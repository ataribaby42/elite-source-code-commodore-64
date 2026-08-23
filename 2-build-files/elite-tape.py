#!/usr/bin/env python3
"""Build a complete bootable C64 TAP image for Elite.

The first file is a normal Commodore ROM-tape BASIC PRG so SHIFT+RUN/STOP works.
After that file, COMLOD/LOCODE/HICODE are appended using the small custom turbo
format decoded by elite-tape-loader.asm.

No external PRG->TAP utility is required.
"""

from __future__ import annotations

import argparse
from pathlib import Path

# -----------------------------------------------------------------------------
# TAP container
# -----------------------------------------------------------------------------

TAP_MAGIC = b"C64-TAPE-RAW"

PAL_CLOCK = 985_248
NTSC_CLOCK = 1_022_727

# Clean, conservative ROM-tape pulse lengths in TAP units (8 CPU cycles/unit).
# They sit near the centre of the standard short/medium/long recognition bands.
# Use the exact integer values used by the reference c64_tap_tool PRG->TAP
# writer: 360 / 524 / 687 PAL cycles, shifted to TAP units of 8 cycles.
ROM_SHORT = 0x2D   # 360 >> 3 = 45 TAP units (reference ROM-tape writer)
ROM_MEDIUM = 0x41  # 524 >> 3 = 65 TAP units
ROM_LONG = 0x55    # 687 >> 3 = 85 TAP units

# Turbo pulses. CIA2 threshold in the C64 loader is $00FE = 254 cycles.
TURBO_SHORT = 0x1A  # 208 cycles -> 0
TURBO_LONG = 0x28   # 320 cycles -> 1


def emit_long_delay(out: bytearray, cycles: int) -> None:
    """Emit one TAP v1 pulse/delay of an arbitrary cycle count."""
    if cycles <= 0:
        return
    if cycles <= 0xFF * 8 and cycles % 8 == 0:
        value = cycles // 8
        if value:
            out.append(value)
            return
    if cycles > 0xFFFFFF:
        raise ValueError("TAP v1 delay exceeds 24-bit cycle count")
    out.append(0)
    out.extend(cycles.to_bytes(3, "little"))


# -----------------------------------------------------------------------------
# Commodore KERNAL tape encoding
# -----------------------------------------------------------------------------


def rom_bit(out: bytearray, bit: int) -> None:
    if bit:
        out.extend((ROM_MEDIUM, ROM_SHORT))
    else:
        out.extend((ROM_SHORT, ROM_MEDIUM))


def rom_byte(out: bytearray, value: int) -> None:
    # Byte marker.
    out.extend((ROM_LONG, ROM_MEDIUM))

    # Data is LSB first. Parity is odd.
    parity = 1
    for bit_number in range(8):
        bit = (value >> bit_number) & 1
        rom_bit(out, bit)
        parity ^= bit

    rom_bit(out, parity)


def rom_end_marker(out: bytearray) -> None:
    out.extend((ROM_LONG, ROM_SHORT))


def xor8(data: bytes | bytearray) -> int:
    value = 0
    for b in data:
        value ^= b
    return value


def rom_record(out: bytearray, payload: bytes, first_copy: bool, end_marker: bool) -> None:
    """Write one KERNAL-format copy of a block."""
    countdown = range(0x89, 0x80, -1) if first_copy else range(0x09, 0x00, -1)

    for b in countdown:
        rom_byte(out, b)

    for b in payload:
        rom_byte(out, b)

    rom_byte(out, xor8(payload))

    if end_marker:
        rom_end_marker(out)


def append_standard_prg(out: bytearray, prg: bytes, name: str, clock: int) -> None:
    if len(prg) < 3:
        raise ValueError("Bootstrap PRG is too short")

    start = int.from_bytes(prg[0:2], "little")
    data = prg[2:]
    end = start + len(data)

    # Basic structural sanity check for a C64 BASIC PRG.
    # V5 deliberately uses a harmless PRINT line instead of SYS, so do not
    # hard-code the exact BASIC token stream here.
    if start == 0x0801:
        if len(data) < 8:
            raise ValueError("Bootstrap BASIC PRG is too short")
        next_line = int.from_bytes(data[0:2], "little")
        line_number = int.from_bytes(data[2:4], "little")
        if not (0x0801 < next_line <= 0x9FFF):
            raise ValueError("Bootstrap BASIC next-line pointer is invalid")
        if line_number != 10:
            raise ValueError("Bootstrap BASIC first line is not line 10")
        # The first line's next-line pointer points at the two-byte $0000
        # BASIC terminator. Diagnostic builds may deliberately contain binary
        # payload after that terminator, so do not require $0000 at EOF.
        terminator_offset = next_line - start
        if terminator_offset < 0 or terminator_offset + 2 > len(data):
            raise ValueError("Bootstrap BASIC next-line pointer is outside the PRG")
        if data[terminator_offset:terminator_offset + 2] != b"\x00\x00":
            raise ValueError("Bootstrap BASIC linked-list terminator is not $0000")

    if end > 0x10000:
        raise ValueError("Bootstrap PRG crosses $FFFF")

    # The bootstrap is a normal BASIC program beginning at $0801.
    # Header type $01 is the standard relocatable BASIC type used by
    # SHIFT+RUN/STOP.
    file_type = 0x01 if start == 0x0801 else 0x03

    header = bytearray([0x20] * 192)
    header[0] = file_type
    header[1:3] = start.to_bytes(2, "little")
    header[3:5] = end.to_bytes(2, "little")

    filename = name.upper().encode("ascii", "replace")[:16]
    header[5:21] = filename.ljust(16, b" ")

    # Match the native C64 KERNAL tape layout closely:
    #
    #   27135 short pulses
    #   $89..$81 + header + xor + EOD
    #   79 short pulses
    #   $09..$01 + header copy + xor
    #   5671 short pulses
    #   $89..$81 + data + xor + EOD
    #   79 short pulses
    #   $09..$01 + data copy + xor
    #
    # In particular there is no artificial long pause between the duplicate
    # header and the data leader. The KERNAL uses the continuous short leader
    # to recalibrate its pulse timing.

    out.extend([ROM_SHORT] * 27135)
    rom_record(out, bytes(header), first_copy=True, end_marker=True)

    out.extend([ROM_SHORT] * 79)
    rom_record(out, bytes(header), first_copy=False, end_marker=False)

    out.extend([ROM_SHORT] * 5671)
    rom_record(out, data, first_copy=True, end_marker=True)

    out.extend([ROM_SHORT] * 79)
    # When another custom stream follows the KERNAL file, terminate the
    # duplicate data copy explicitly. At end-of-file VICE/KERNAL tolerated
    # the previous truncated ending, but appended turbo pulses exposed it.
    rom_record(out, data, first_copy=False, end_marker=True)



# -----------------------------------------------------------------------------
# Self-verification of the generated ROM section
# -----------------------------------------------------------------------------


def _rom_classify(value: int) -> str:
    if 0x24 <= value <= 0x36:
        return "S"
    if 0x37 <= value <= 0x49:
        return "M"
    if 0x4A <= value <= 0x64:
        return "L"
    return "?"


def _decode_rom_byte(pulses: bytes | bytearray, pos: int) -> tuple[int, int]:
    if pos + 20 > len(pulses):
        raise ValueError("ROM section ended in the middle of a byte")
    kinds = [_rom_classify(v) for v in pulses[pos:pos + 20]]
    if kinds[:2] != ["L", "M"]:
        raise ValueError(f"Missing ROM byte marker at pulse {pos}")
    value = 0
    parity = 1
    p = 2
    for bit_number in range(8):
        pair = kinds[p:p + 2]
        p += 2
        if pair == ["S", "M"]:
            bit = 0
        elif pair == ["M", "S"]:
            bit = 1
        else:
            raise ValueError(f"Invalid ROM bit pulse pair at pulse {pos + p - 2}")
        value |= bit << bit_number
        parity ^= bit
    pair = kinds[p:p + 2]
    if pair == ["S", "M"]:
        parity_on_tape = 0
    elif pair == ["M", "S"]:
        parity_on_tape = 1
    else:
        raise ValueError(f"Invalid ROM parity pulse pair at pulse {pos + p}")
    if parity_on_tape != parity:
        raise ValueError(f"ROM parity error at pulse {pos}")
    return value, pos + 20


def _decode_rom_bytes(pulses: bytes | bytearray, pos: int, count: int) -> tuple[bytes, int]:
    out = bytearray()
    for _ in range(count):
        value, pos = _decode_rom_byte(pulses, pos)
        out.append(value)
    return bytes(out), pos


def verify_standard_section(pulses: bytes | bytearray, prg: bytes, name: str) -> None:
    """Round-trip the generated KERNAL section and compare it to the PRG."""
    pos = 0
    while pos < len(pulses) and _rom_classify(pulses[pos]) == "S":
        pos += 1
    if pos < 20000:
        raise ValueError("ROM leader is unexpectedly short")

    header1, pos = _decode_rom_bytes(pulses, pos, 9 + 192 + 1)
    if header1[:9] != bytes(range(0x89, 0x80, -1)):
        raise ValueError("Bad primary header countdown")
    if xor8(header1[9:-1]) != header1[-1]:
        raise ValueError("Bad primary header checksum")
    if _rom_classify(pulses[pos]) != "L" or _rom_classify(pulses[pos + 1]) != "S":
        raise ValueError("Missing primary header end marker")
    pos += 2
    while _rom_classify(pulses[pos]) == "S":
        pos += 1

    header2, pos = _decode_rom_bytes(pulses, pos, 9 + 192 + 1)
    if header2[:9] != bytes(range(0x09, 0x00, -1)):
        raise ValueError("Bad backup header countdown")
    if header2[9:] != header1[9:]:
        raise ValueError("Header copies differ")
    while _rom_classify(pulses[pos]) == "S":
        pos += 1

    data_len = len(prg) - 2
    data1, pos = _decode_rom_bytes(pulses, pos, 9 + data_len + 1)
    if data1[:9] != bytes(range(0x89, 0x80, -1)):
        raise ValueError("Bad primary data countdown")
    if data1[9:-1] != prg[2:]:
        raise ValueError("Primary ROM data does not round-trip to bootstrap PRG")
    if xor8(data1[9:-1]) != data1[-1]:
        raise ValueError("Bad primary data checksum")
    if _rom_classify(pulses[pos]) != "L" or _rom_classify(pulses[pos + 1]) != "S":
        raise ValueError("Missing primary data end marker")
    pos += 2
    while pos < len(pulses) and _rom_classify(pulses[pos]) == "S":
        pos += 1

    data2, pos = _decode_rom_bytes(pulses, pos, 9 + data_len + 1)
    if data2[:9] != bytes(range(0x09, 0x00, -1)):
        raise ValueError("Bad backup data countdown")
    if data2[9:] != data1[9:]:
        raise ValueError("Data copies differ")
    if pos + 2 > len(pulses):
        raise ValueError("Missing final backup-data EOD marker")
    if _rom_classify(pulses[pos]) != "L" or _rom_classify(pulses[pos + 1]) != "S":
        raise ValueError("Missing final backup-data EOD marker")
    pos += 2
    if pos != len(pulses):
        raise ValueError("Unexpected pulses after verified ROM section")

# -----------------------------------------------------------------------------
# Elite turbo encoding
# -----------------------------------------------------------------------------


def turbo_bit(out: bytearray, bit: int) -> None:
    out.append(TURBO_LONG if bit else TURBO_SHORT)


def turbo_byte(out: bytearray, value: int) -> None:
    # The C64 turbo decoder reconstructs bytes MSB first.
    for bit_number in range(7, -1, -1):
        turbo_bit(out, (value >> bit_number) & 1)


def turbo_sync(out: bytearray, pilot_bytes: int) -> None:
    for _ in range(pilot_bytes):
        turbo_byte(out, 0x02)

    for value in range(9, 0, -1):
        turbo_byte(out, value)


def turbo_block(
    out: bytearray,
    block_id: int,
    destination: int,
    data: bytes,
    pilot_bytes: int,
) -> None:
    if not 0 <= block_id <= 0xFF:
        raise ValueError("Invalid block id")
    if not 0 <= destination <= 0xFFFF:
        raise ValueError("Invalid destination")
    if len(data) > 0xFFFF:
        raise ValueError("Turbo block exceeds 65535 bytes")

    turbo_sync(out, pilot_bytes)

    header = bytes(
        (
            block_id,
            destination & 0xFF,
            (destination >> 8) & 0xFF,
            len(data) & 0xFF,
            (len(data) >> 8) & 0xFF,
        )
    )

    for b in header:
        turbo_byte(out, b)
    turbo_byte(out, xor8(header))

    for b in data:
        turbo_byte(out, b)
    turbo_byte(out, xor8(data))



# -----------------------------------------------------------------------------
# Self-verification of custom turbo blocks
# -----------------------------------------------------------------------------


def _decode_turbo_byte(pulses: bytes | bytearray, pos: int) -> tuple[int, int]:
    if pos + 8 > len(pulses):
        raise ValueError("Turbo section ended in the middle of a byte")
    value = 0
    for pulse in pulses[pos:pos + 8]:
        cycles = pulse * 8
        value = (value << 1) | (1 if cycles > 0xFE else 0)
    return value, pos + 8


def verify_turbo_block(
    pulses: bytes | bytearray,
    pos: int,
    pilot_bytes: int,
    block_id: int,
    destination: int,
    expected_data: bytes,
) -> int:
    for _ in range(pilot_bytes):
        value, pos = _decode_turbo_byte(pulses, pos)
        if value != 0x02:
            raise ValueError(f"Bad turbo pilot in block {block_id}")
    for expected in range(9, 0, -1):
        value, pos = _decode_turbo_byte(pulses, pos)
        if value != expected:
            raise ValueError(f"Bad turbo countdown in block {block_id}")
    header = bytearray()
    for _ in range(6):
        value, pos = _decode_turbo_byte(pulses, pos)
        header.append(value)
    raw_header = header[:5]
    if header[5] != xor8(raw_header):
        raise ValueError(f"Bad turbo header checksum in block {block_id}")
    got_id = raw_header[0]
    got_destination = raw_header[1] | (raw_header[2] << 8)
    got_length = raw_header[3] | (raw_header[4] << 8)
    if (got_id, got_destination, got_length) != (block_id, destination, len(expected_data)):
        raise ValueError(f"Turbo header mismatch in block {block_id}")
    decoded = bytearray()
    for _ in range(got_length):
        value, pos = _decode_turbo_byte(pulses, pos)
        decoded.append(value)
    checksum, pos = _decode_turbo_byte(pulses, pos)
    if bytes(decoded) != expected_data:
        raise ValueError(f"Turbo payload mismatch in block {block_id}")
    if checksum != xor8(decoded):
        raise ValueError(f"Bad turbo payload checksum in block {block_id}")
    return pos

# -----------------------------------------------------------------------------
# TAP writer
# -----------------------------------------------------------------------------


def make_tap(
    boot_prg: bytes,
    comlod: bytes,
    locode: bytes,
    hicode: bytes,
    name: str,
    video: str,
    rom_only: bool = False,
    turbo_test: bool = False,
    comlod_test: bool = False,
    full_boot: bool = False,
) -> bytes:
    video = video.lower()
    if video == "pal":
        video_code = 0
        clock = PAL_CLOCK
    elif video == "ntsc":
        video_code = 1
        clock = NTSC_CLOCK
    else:
        raise ValueError("video must be pal or ntsc")

    pulses = bytearray()
    append_standard_prg(pulses, boot_prg, name, clock)
    rom_end = len(pulses)
    verify_standard_section(pulses[:rom_end], boot_prg, name)

    if full_boot:
        # V11: proven KERNAL boundary, then the three game components.
        #
        # COMLOD is followed directly by the LOCODE pilot. The C64 stops the
        # Datasette motor after COMLOD, so that long pilot remains stationary
        # while COMLOD executes and absorbs real-drive motor inertia.
        gap_cycles = int(round(clock * 2.0))
        emit_long_delay(pulses, gap_cycles)
        turbo_start = len(pulses)

        turbo_block(pulses, 4, 0x4000, comlod, pilot_bytes=2048)
        turbo_block(pulses, 5, 0x1D00, locode, pilot_bytes=2048)
        turbo_block(pulses, 6, 0x6A00, hicode, pilot_bytes=256)

        pos = turbo_start
        pos = verify_turbo_block(pulses, pos, 2048, 4, 0x4000, comlod)
        pos = verify_turbo_block(pulses, pos, 2048, 5, 0x1D00, locode)
        pos = verify_turbo_block(pulses, pos, 256, 6, 0x6A00, hicode)

        if pos != len(pulses):
            raise ValueError("Unexpected data after V13C HICODE turbo block")

    elif comlod_test:
        # V9 keeps the proven V8B boundary unchanged:
        # final KERNAL EOD -> true 2 second gap -> turbo pilot.
        gap_cycles = int(round(clock * 2.0))
        emit_long_delay(pulses, gap_cycles)
        turbo_start = len(pulses)

        turbo_block(pulses, 9, 0x4000, comlod, pilot_bytes=2048)

        pos = turbo_start
        pos = verify_turbo_block(pulses, pos, 2048, 9, 0x4000, comlod)
        if pos != len(pulses):
            raise ValueError("Unexpected data after V10A COMLOD turbo block")

    elif turbo_test:
        gap_cycles = int(round(clock * 2.0))
        emit_long_delay(pulses, gap_cycles)
        turbo_start = len(pulses)

        test_data = b"ELITEV8B TURBO OK\x00"
        turbo_block(pulses, 8, 0xC000, test_data, pilot_bytes=2048)

        pos = turbo_start
        pos = verify_turbo_block(pulses, pos, 2048, 8, 0xC000, test_data)
        if pos != len(pulses):
            raise ValueError("Unexpected data after V8B turbo test block")

    elif not rom_only:
        # Full Elite mode (kept for later versions).
        turbo_block(pulses, 1, 0x4000, comlod, pilot_bytes=1024)
        turbo_block(pulses, 2, 0x1D00, locode, pilot_bytes=1024)
        turbo_block(pulses, 3, 0x6A00, hicode, pilot_bytes=192)

        pos = rom_end
        pos = verify_turbo_block(pulses, pos, 1024, 1, 0x4000, comlod)
        pos = verify_turbo_block(pulses, pos, 1024, 2, 0x1D00, locode)
        pos = verify_turbo_block(pulses, pos, 192, 3, 0x6A00, hicode)
        if pos != len(pulses):
            raise ValueError("Unexpected data after turbo block 3")

    tap_header = bytearray(TAP_MAGIC)
    # TAP v1 is required for the true multi-second no-transition gap encoded
    # by emit_long_delay(). Bytes $0D-$0F remain zero for C64/PAL-compatible
    # classic TAP readers; the actual pulse timing is already machine-clocked.
    tap_header.extend((1, 0, 0, 0))
    tap_header.extend(len(pulses).to_bytes(4, "little"))

    return bytes(tap_header + pulses)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build bootable Elite C64 cassette TAP")
    parser.add_argument("--boot", required=True, type=Path)
    parser.add_argument("--comlod", required=True, type=Path)
    parser.add_argument("--locode", required=True, type=Path)
    parser.add_argument("--hicode", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--video", choices=("pal", "ntsc"), required=True)
    parser.add_argument("--name", default="ELITE")
    parser.add_argument(
        "--rom-only",
        action="store_true",
        help="write only the standard KERNAL tape file (diagnostic mode)",
    )
    parser.add_argument(
        "--turbo-test",
        action="store_true",
        help="append only the small one-block turbo transport test",
    )
    parser.add_argument(
        "--comlod-test",
        action="store_true",
        help="append only the COMLOD diagnostic transport",
    )
    parser.add_argument(
        "--full-boot",
        action="store_true",
        help="build the V11 complete COMLOD/LOCODE/HICODE cassette boot",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    boot = args.boot.read_bytes()
    comlod = args.comlod.read_bytes()
    locode = args.locode.read_bytes()
    hicode = args.hicode.read_bytes()

    selected_modes = sum(
        1
        for enabled in (
            args.rom_only,
            args.turbo_test,
            args.comlod_test,
            args.full_boot,
        )
        if enabled
    )
    if selected_modes > 1:
        raise ValueError(
            "--rom-only, --turbo-test, --comlod-test and --full-boot are mutually exclusive"
        )

    tap = make_tap(
        boot,
        comlod,
        locode,
        hicode,
        args.name,
        args.video,
        rom_only=args.rom_only,
        turbo_test=args.turbo_test,
        comlod_test=args.comlod_test,
        full_boot=args.full_boot,
    )

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(tap)

    print("Elite C64 cassette image")
    print("  video :", args.video.upper())
    print("  boot  :", len(boot), "bytes PRG")
    print("  COMLOD:", len(comlod), "bytes -> $4000")
    print("  LOCODE:", len(locode), "bytes -> $1D00")
    print("  HICODE:", len(hicode), "bytes -> $6A00")
    print("  TAP   :", len(tap), "bytes")
    if args.full_boot:
        print("  mode  : V13C cassette turbo boot - filename ELITE")
        print(f"  COMLOD: {len(comlod)} bytes -> $4000")
        print(f"  LOCODE: {len(locode)} bytes -> $1D00")
        print(f"  HICODE: {len(hicode)} bytes -> $6A00")
        print("  gap   : 2.0 seconds before first turbo block")
        print("  verify: ROM + COMLOD + LOCODE + HICODE round-trip OK")
    elif args.comlod_test:
        print("  mode  : COMLOD diagnostic transport")
        print(f"  test  : {len(comlod)} bytes -> $4000")
        print("  gap   : 2.0 seconds between KERNAL file and turbo stream")
        print("  verify: ROM + final EOD + complete COMLOD turbo block OK")
    elif args.turbo_test:
        print("  mode  : small one-block turbo transport test")
        print("  gap   : 2.0 seconds between KERNAL file and turbo stream")
        print("  verify: ROM + small turbo block round-trip OK")
    elif args.rom_only:
        print("  mode  : ROM/KERNAL diagnostic only")
        print("  verify: ROM block round-trip OK")
    else:
        print("  verify: ROM + all turbo blocks round-trip OK")
    print("  output:", args.output)


if __name__ == "__main__":
    main()
