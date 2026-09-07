"""Build a PAL C64 cassette that writes every sector of a 35-track D64."""
from pathlib import Path
import argparse
import binascii
import hashlib
import json
import shutil
import subprocess
import tape_codec as tape

ROOT = Path(__file__).resolve().parent
BUFFER = 0x4000
TRACKS = tuple(range(1, 18)) + tuple(range(19, 36)) + (18,)

def sectors(track):
    if not 1 <= track <= 35:
        raise ValueError("Expected track 1..35")
    return 21 if track <= 17 else 19 if track <= 24 else 18 if track <= 30 else 17

def offset(track):
    return sum(sectors(t) for t in range(1, track)) * 256

def encode_track(raw):
    if not raw or len(raw) % 256:
        raise ValueError("Incomplete track")
    result = bytearray()
    for start in range(0, len(raw), 256):
        sector = raw[start:start + 256]
        result.append(int(any(sector)))
        if any(sector):
            result.extend(sector)
    return bytes(result)

def decode_track(payload, track):
    result = bytearray()
    pos = 0
    for _ in range(sectors(track)):
        if pos >= len(payload) or payload[pos] not in (0, 1):
            raise ValueError("Invalid sector tag")
        tag = payload[pos]
        pos += 1
        if tag:
            if pos + 256 > len(payload):
                raise ValueError("Truncated sector")
            result.extend(payload[pos:pos + 256])
            pos += 256
        else:
            result.extend(bytes(256))
    if pos != len(payload):
        raise ValueError("Trailing bytes")
    return bytes(result)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("d64", nargs="?", type=Path, default=ROOT.parent / "5-compiled-game-disks/elite-commodore-64-flicker-free-gma86-pal.d64")
    parser.add_argument("--beebasm", default=shutil.which("beebasm") or str(ROOT.parents[1] / "beebasm/beebasm.exe"))
    args = parser.parse_args()
    source = args.d64.resolve()
    data = source.read_bytes()
    if len(data) != 174848:
        raise ValueError("Only plain 35-track, 174848-byte D64 supported (no error table)")
    disk_id = data[offset(18)+162:offset(18)+164]
    if any(value < 32 or value > 126 or value in (44, 58) for value in disk_id):
        raise ValueError("Disk ID must be two printable ASCII bytes, excluding comma/colon, for the DOS format command")
    out = ROOT / "output"
    out.mkdir(exist_ok=True)
    blocks = [(t, encode_track(data[offset(t):offset(t) + sectors(t)*256])) for t in TRACKS]
    restored = bytearray(len(data))
    for t, payload in blocks:
        restored[offset(t):offset(t)+sectors(t)*256] = decode_track(payload, t)
    if restored != data:
        raise ValueError("Sector reconstruction differs from source")
    def table(name, values):
        return "." + name + "\n EQUB " + ",".join(str(v) for v in values) + "\n"
    layout = table("TrackOrder", TRACKS)
    layout += table("SectorCounts", [sectors(t) for t in TRACKS])
    layout += table("LengthLo", [len(b) & 255 for _, b in blocks])
    layout += table("LengthHi", [len(b) >> 8 for _, b in blocks])
    crcs = [binascii.crc_hqx(b, 0xffff) for _, b in blocks]
    layout += table("CrcLo", [c & 255 for c in crcs])
    layout += table("CrcHi", [c >> 8 for c in crcs])
    layout += '.FormatCommand\n EQUS "N0:TAPD64,"\n EQUB ' + ','.join(str(v) for v in disk_id) + '\n.FormatEnd\n'
    layout += 'ASSERT FormatEnd - FormatCommand < 256\n'
    (out / "layout.asm").write_text(layout, encoding="ascii")
    build = subprocess.run([args.beebasm, "-i", "transfer.asm", "-v"], cwd=ROOT, capture_output=True, text=True)
    (out / "compile.txt").write_text(build.stdout + build.stderr, encoding="utf-8")
    if build.returncode:
        raise RuntimeError(build.stdout + build.stderr)
    boot = (out / "transfer.prg").read_bytes()
    pulses = bytearray()
    tape.append_standard_prg(pulses, boot, "TAPD64", tape.PAL_CLOCK)
    tape.verify_standard_section(pulses, boot, "TAPD64")
    tape.emit_long_delay(pulses, 2*tape.PAL_CLOCK)
    for t, payload in blocks:
        start = len(pulses)
        tape.turbo_block(pulses, t, BUFFER, payload, pilot_bytes=2048)
        end = tape.verify_turbo_block(pulses, start, 2048, t, BUFFER, payload)
        if end != len(pulses):
            raise ValueError("Unexpected tape data")
    header = tape.TAP_MAGIC + bytes((1, 0, 0, 0)) + len(pulses).to_bytes(4, "little")
    target = out / (source.stem + "-transfer.tap")
    target.write_bytes(header + pulses)
    manifest = {"source": source.name, "source_sha256": hashlib.sha256(data).hexdigest(),
                "source_bytes": len(data), "sectors": 683, "track_order": TRACKS,
                "payload_bytes": sum(len(b) for _, b in blocks), "prg_bytes": len(boot),
                "tap": target.name, "tap_sha256": hashlib.sha256(header+pulses).hexdigest(),
                "tap_bytes": target.stat().st_size,
                "verification": "ROM and turbo pulse round-trip; exact 683-sector reconstruction"}
    (out / "manifest.json").write_text(json.dumps(manifest, indent=2)+"\n")
    print(json.dumps(manifest, indent=2))

if __name__ == "__main__":
    main()
