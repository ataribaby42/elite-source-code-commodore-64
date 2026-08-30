#!/usr/bin/env python3
"""Generate the PackBits-compressed dashboard binaries used by Elite: Unbound."""

from __future__ import annotations

import argparse
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
IMAGE_DIR = ROOT / "1-source-files" / "images"
DASHBOARDS = (
    ("C.CODIALS.bin", "C.CODIALS.RLE.bin"),
    ("C.CODIALSNEW.bin", "C.CODIALSNEW.RLE.bin"),
)


def encode_packbits(data: bytes) -> bytes:
    """Return an optimal standard PackBits encoding of data."""

    size = len(data)
    best_size = [0] * (size + 1)
    best_packets = [0] * (size + 1)
    choices: list[tuple[str, int] | None] = [None] * (size + 1)

    for offset in range(size - 1, -1, -1):
        best_key: tuple[int, int, int, int] | None = None
        best_choice: tuple[str, int] | None = None
        remaining = min(128, size - offset)

        for length in range(1, remaining + 1):
            next_offset = offset + length
            key = (
                1 + length + best_size[next_offset],
                1 + best_packets[next_offset],
                1,
                -length,
            )
            if best_key is None or key < best_key:
                best_key = key
                best_choice = ("literal", length)

        run_length = 1
        while (
            run_length < remaining
            and data[offset + run_length] == data[offset]
        ):
            run_length += 1

        for length in range(2, run_length + 1):
            next_offset = offset + length
            key = (
                2 + best_size[next_offset],
                1 + best_packets[next_offset],
                0,
                -length,
            )
            if best_key is None or key < best_key:
                best_key = key
                best_choice = ("repeat", length)

        assert best_key is not None
        assert best_choice is not None
        best_size[offset] = best_key[0]
        best_packets[offset] = best_key[1]
        choices[offset] = best_choice

    packed = bytearray()
    offset = 0

    while offset < size:
        choice = choices[offset]
        assert choice is not None
        packet_type, length = choice

        if packet_type == "literal":
            packed.append(length - 1)
            packed.extend(data[offset : offset + length])
        else:
            packed.append(257 - length)
            packed.append(data[offset])

        offset += length

    return bytes(packed)


def decode_packbits(data: bytes) -> bytes:
    """Decode a standard PackBits byte stream."""

    unpacked = bytearray()
    offset = 0

    while offset < len(data):
        control = data[offset]
        offset += 1

        if control <= 127:
            length = control + 1
            end = offset + length
            if end > len(data):
                raise ValueError("Truncated PackBits literal packet")
            unpacked.extend(data[offset:end])
            offset = end
        elif control >= 129:
            if offset >= len(data):
                raise ValueError("Truncated PackBits repeat packet")
            unpacked.extend([data[offset]] * (257 - control))
            offset += 1
        # Control byte 128 is the standard PackBits no-op.

    return bytes(unpacked)


def process_dashboard(source_name: str, target_name: str, check: bool) -> None:
    source = IMAGE_DIR / source_name
    target = IMAGE_DIR / target_name
    original = source.read_bytes()
    packed = encode_packbits(original)

    if decode_packbits(packed) != original:
        raise RuntimeError(f"PackBits round-trip failed for {source_name}")

    if check:
        if not target.exists() or target.read_bytes() != packed:
            raise SystemExit(
                f"{target.relative_to(ROOT)} is missing or out of date; "
                "run 2-build-files/elite-packbits.py"
            )
    elif not target.exists() or target.read_bytes() != packed:
        target.write_bytes(packed)

    saved = len(original) - len(packed)
    print(
        f"{source_name}: {len(original)} -> {len(packed)} bytes "
        f"({saved} bytes saved)"
    )


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Generate PackBits dashboard binaries"
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="verify that the checked-in compressed files are up to date",
    )
    args = parser.parse_args()

    for source_name, target_name in DASHBOARDS:
        process_dashboard(source_name, target_name, args.check)


if __name__ == "__main__":
    main()

