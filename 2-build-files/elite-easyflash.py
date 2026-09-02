#!/usr/bin/env python3
"""Build a native EasyFlash CRT image for Commodore 64 Elite."""

from __future__ import annotations

import argparse
import struct
from pathlib import Path


BANK_SIZE = 0x2000
CART_TYPE_EASYFLASH = 32
CHIP_TYPE_FLASH_ROM = 2


def xor_checksum(data: bytes) -> int:
    result = 0
    for value in data:
        result ^= value
    return result


def pad_bank(data: bytes) -> bytes:
    if len(data) > BANK_SIZE:
        raise ValueError(f"8 KiB EasyFlash bank overflow: {len(data)} bytes")
    return data.ljust(BANK_SIZE, b"\xff")


def chip_packet(bank: int, load_address: int, data: bytes) -> bytes:
    image = pad_bank(data)
    return struct.pack(
        ">4sIHHHH", b"CHIP", 0x10 + len(image), CHIP_TYPE_FLASH_ROM,
        bank, load_address, len(image)
    ) + image


def crt_header(name: str) -> bytes:
    encoded_name = name.encode("ascii", errors="strict")
    if len(encoded_name) > 32:
        raise ValueError("CRT cartridge name must be at most 32 ASCII characters")

    return (
        b"C64 CARTRIDGE   "
        + struct.pack(">IHHBB", 0x40, 0x0100, CART_TYPE_EASYFLASH, 1, 0)
        + b"\x00" * 6
        + encoded_name.ljust(32, b"\x00")
    )


def verify_crt(image: bytes, expected_packets: int) -> None:
    if len(image) < 0x40 or image[:16] != b"C64 CARTRIDGE   ":
        raise RuntimeError("internal CRT verification failed: invalid header")

    position = 0x40
    packets = 0
    while position < len(image):
        if position + 0x10 > len(image):
            raise RuntimeError("internal CRT verification failed: truncated CHIP header")

        magic, packet_size, chip_type, _, load_address, image_size = struct.unpack_from(
            ">4sIHHHH", image, position
        )
        if (
            magic != b"CHIP"
            or chip_type != CHIP_TYPE_FLASH_ROM
            or load_address not in (0x8000, 0xA000)
            or image_size != BANK_SIZE
            or packet_size != 0x10 + image_size
            or position + packet_size > len(image)
        ):
            raise RuntimeError("internal CRT verification failed: invalid CHIP packet")

        position += packet_size
        packets += 1

    if position != len(image) or packets != expected_packets:
        raise RuntimeError("internal CRT verification failed: incorrect packet count")


def build_image(
    boot_low: bytes,
    boot_high: bytes,
    comlod: bytes,
    locode: bytes,
    hicode: bytes,
    name: str,
) -> tuple[bytes, int]:
    if len(boot_high) > 0x3FA:
        raise ValueError("ROMH reset stub exceeds the available $FC00-$FFF9 area")

    high_bank = bytearray(b"\xff" * BANK_SIZE)
    high_bank[0x1C00:0x1C00 + len(boot_high)] = boot_high
    struct.pack_into("<HHH", high_bank, 0x1FFA, 0xFC03, 0xFC00, 0xFC03)

    segments = ((0x4000, comlod), (0x1D00, locode), (0x6A00, hicode))
    manifest = bytearray(b"ECRT\x01\x03")
    for destination, segment in segments:
        if len(segment) > 0xFFFF:
            raise ValueError(f"segment at ${destination:04X} exceeds 65535 bytes")
        manifest.extend(struct.pack("<HHB", destination, len(segment), xor_checksum(segment)))

    stream = bytes(manifest) + b"".join(segment for _, segment in segments)
    stream_banks = max(1, (len(stream) + BANK_SIZE - 1) // BANK_SIZE)
    if stream_banks > 63:
        raise ValueError("payload exceeds the available EasyFlash ROML banks")

    image = bytearray(crt_header(name))
    image.extend(chip_packet(0, 0x8000, boot_low))
    image.extend(chip_packet(0, 0xA000, bytes(high_bank)))

    for index in range(stream_banks):
        start = index * BANK_SIZE
        image.extend(chip_packet(index + 1, 0x8000, stream[start:start + BANK_SIZE]))

    return bytes(image), stream_banks


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--boot-low", required=True, type=Path)
    parser.add_argument("--boot-high", required=True, type=Path)
    parser.add_argument("--comlod", required=True, type=Path)
    parser.add_argument("--locode", required=True, type=Path)
    parser.add_argument("--hicode", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--name", default="Elite C64 EasyFlash")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    result, stream_banks = build_image(
        args.boot_low.read_bytes(),
        args.boot_high.read_bytes(),
        args.comlod.read_bytes(),
        args.locode.read_bytes(),
        args.hicode.read_bytes(),
        args.name,
    )

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(result)

    expected_packets = stream_banks + 2
    verify_crt(result, expected_packets)

    print(
        f"Created {args.output} ({len(result)} bytes, "
        f"{stream_banks} payload ROML banks)"
    )


if __name__ == "__main__":
    main()
