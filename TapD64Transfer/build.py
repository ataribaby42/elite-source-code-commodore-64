"""Build a PAL C64 cassette that writes every sector of a 35-track D64."""
from pathlib import Path, PureWindowsPath
import argparse
import binascii
import hashlib
import json
import ntpath
import os
import shutil
import subprocess
import sys
import unicodedata
import tape_codec as tape

ROOT = Path(__file__).resolve().parent
BUFFER = 0x4000
TRACKS = tuple(range(1, 18)) + tuple(range(19, 36)) + (18,)
TAPE_NAME_LIMIT = 16
LABEL_LIMIT = 25

def tap_filename(value):
    """A filename within output/, preserving case and adding .tap if omitted."""
    # Python 3.13+ moved this check; keep Python 3.10-3.12 support as well.
    is_reserved = getattr(ntpath, "isreserved", lambda name: PureWindowsPath(name).is_reserved())
    if (not value or value in (".", "..") or value[-1] in " ."
            or any(ord(c) < 32 or c in '<>:"/\\|?*' for c in value)
            or is_reserved(value)):
        raise ValueError("tapfile must be a valid Windows filename without a directory")
    return value if value.lower().endswith(".tap") else value + ".tap"

def c64_text(value, limit):
    """Printable uppercase C64 text, stripped of accents and clipped in bytes."""
    value = unicodedata.normalize("NFKD", value)
    value = "".join(c for c in value if not unicodedata.combining(c)).upper()
    value = "".join(" " if c.isspace() else c if 32 <= ord(c) <= 95 else "?" for c in value)
    return value.strip()[:limit]

def parse_args(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, epilog='Also accepts: d64="path to image.d64" tapename="TAPE NAME" label="SCREEN TITLE" tapfile="output name.tap"')
    parser.add_argument("image", nargs="?", type=Path, help="D64 image (legacy positional form)")
    parser.add_argument("--d64", type=Path, help="Source 35-track D64 image")
    parser.add_argument("--tapename", default="TAPD64", help="Cassette program name, clipped to 16 characters")
    parser.add_argument("--label", default="ELITE: UNBOUND", help="C64 title, clipped to 25 characters")
    parser.add_argument("--tapfile", help="TAP filename inside output/; .tap is added if omitted")
    parser.add_argument("--beebasm", default=shutil.which("beebasm") or str(ROOT.parents[1] / "beebasm/beebasm.exe"))
    normalized = []
    for argument in sys.argv[1:] if argv is None else argv:
        key, separator, value = argument.partition("=")
        if separator and key.lower() in ("d64", "tapename", "label", "tapfile"):
            argument = "--" + key.lower() + "=" + value
        normalized.append(argument)
    args = parser.parse_args(normalized)
    if args.tapfile is not None:
        try:
            args.tapfile = tap_filename(args.tapfile)
        except ValueError as error:
            parser.error(str(error))
    if args.d64 is not None and args.image is not None:
        parser.error("Specify either d64= or a positional image, not both")
    if args.d64 == Path(".") or args.image == Path("."):
        parser.error("D64 path must name an image file")
    source = args.d64 if args.d64 is not None else args.image
    if source is None:
        source = ROOT.parent / "5-compiled-game-disks/elite-commodore-64-flicker-free-gma86-pal.d64"
    if not source.is_absolute():
        # build.bat runs in the tool directory; relative input paths still
        # belong to the directory from which the user invoked the batch file.
        source = Path(os.environ.get("ELITE_TAPD64_CALLER_DIR", os.getcwd())) / source
    args.d64 = source.resolve()
    for field, limit in (("tapename", TAPE_NAME_LIMIT), ("label", LABEL_LIMIT)):
        value = c64_text(getattr(args, field), limit)
        if not value:
            parser.error(field + " must contain at least one printable character")
        setattr(args, field, value)
    return args

def title_assembly(label):
    # Encode data numerically: quotes or punctuation in a title must never
    # become assembler syntax. The surrounding Welcome string adds CR.
    values = ",".join(str(b) for b in label.encode("ascii"))
    return (".TransferTitle\n EQUB " + values + "\n.TransferTitleEnd\n"
            "ASSERT TransferTitleEnd - TransferTitle <= " + str(LABEL_LIMIT) + "\n")

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
    args = parse_args()
    source = args.d64
    data = source.read_bytes()
    if len(data) != 174848:
        raise ValueError("Only plain 35-track, 174848-byte D64 supported (no error table)")
    disk_id = data[offset(18)+162:offset(18)+164]
    if any(value < 32 or value > 126 or value in (44, 58) for value in disk_id):
        raise ValueError("Disk ID must be two printable ASCII bytes, excluding comma/colon, for the DOS format command")
    out = ROOT / "output"
    out.mkdir(exist_ok=True)
    (out / "title.asm").write_text(title_assembly(args.label), encoding="ascii")
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
    tape.append_standard_prg(pulses, boot, args.tapename, tape.PAL_CLOCK)
    tape.verify_standard_section(pulses, boot, args.tapename)
    tape.emit_long_delay(pulses, 2*tape.PAL_CLOCK)
    for t, payload in blocks:
        start = len(pulses)
        tape.turbo_block(pulses, t, BUFFER, payload, pilot_bytes=2048)
        end = tape.verify_turbo_block(pulses, start, 2048, t, BUFFER, payload)
        if end != len(pulses):
            raise ValueError("Unexpected tape data")
    header = tape.TAP_MAGIC + bytes((1, 0, 0, 0)) + len(pulses).to_bytes(4, "little")
    target = out / (args.tapfile if args.tapfile is not None else source.stem + "-transfer.tap")
    target.write_bytes(header + pulses)
    manifest = {"source": source.name, "source_sha256": hashlib.sha256(data).hexdigest(),
                "tapename": args.tapename, "label": args.label,
                "source_bytes": len(data), "sectors": 683, "track_order": TRACKS,
                "payload_bytes": sum(len(b) for _, b in blocks), "prg_bytes": len(boot),
                "tap": target.name, "tap_sha256": hashlib.sha256(header+pulses).hexdigest(),
                "tap_bytes": target.stat().st_size,
                "verification": "ROM and turbo pulse round-trip; exact 683-sector reconstruction"}
    (out / "manifest.json").write_text(json.dumps(manifest, indent=2)+"\n")
    print(json.dumps(manifest, indent=2))

if __name__ == "__main__":
    main()
