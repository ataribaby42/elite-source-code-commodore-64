#!/usr/bin/env python3
#
# ******************************************************************************
#
# COMMODORE 64 ELITE GMA FAST-LOADER SECTOR SCRIPT
#
# Read the actual file locations from a GMA86 PAL D64 image and patch the
# fast-loader track/sector table in gma1.unprot.bin. The disk is then rebuilt
# with the patched GMA1 file and this script is run again with --verify.
#
# ******************************************************************************

import argparse
import os
from pathlib import Path
import tempfile


DIRECTORY_TRACK = 18
DIRECTORY_SECTOR = 1
GMA_TABLE_OFFSET = 5
GMA_TABLE_FILES = {
    2: "BYEBYEJULIE",  # GMA2 on the original GMA86 disk
    3: "GMA3",
    4: "GMA4",
    5: "GMA5",
    6: "GMA6",
}


def sectors_per_track(track):
    if 1 <= track <= 17:
        return 21
    if 18 <= track <= 24:
        return 19
    if 25 <= track <= 30:
        return 18
    if 31 <= track <= 40:
        return 17
    raise RuntimeError("Invalid D64 track: {}".format(track))


def sector_offset(image, track, sector):
    sector_count = sectors_per_track(track)
    if not 0 <= sector < sector_count:
        raise RuntimeError("Invalid D64 track/sector: {}/{}".format(track,
                                                                   sector))

    offset = sum(sectors_per_track(t) for t in range(1, track)) * 256
    offset += sector * 256
    if offset + 256 > len(image):
        raise RuntimeError("Track/sector {}/{} is outside the D64 image".format(
            track, sector))
    return offset


def decode_filename(raw_name):
    name = bytes(value for value in raw_name if value != 0xA0)
    return name.decode("latin-1").rstrip("\x00 ").upper()


def read_directory(image):
    files = {}
    visited = set()
    track = DIRECTORY_TRACK
    sector = DIRECTORY_SECTOR

    while track:
        position = (track, sector)
        if position in visited:
            raise RuntimeError("Cyclic D64 directory chain at {}/{}".format(
                track, sector))
        visited.add(position)

        offset = sector_offset(image, track, sector)
        block = image[offset:offset + 256]
        track, sector = block[0], block[1]

        for entry_number in range(8):
            entry = 2 + entry_number * 32
            if block[entry] == 0:
                continue

            name = decode_filename(block[entry + 3:entry + 19])
            if not name:
                continue
            if name in files:
                raise RuntimeError("Duplicate D64 filename: {}".format(name))

            files[name] = (block[entry + 1], block[entry + 2])

    return files


def expected_table(files):
    table = {}
    for index, filename in GMA_TABLE_FILES.items():
        if filename not in files:
            raise RuntimeError("File {} is missing from the D64 image".format(
                filename))
        table[index] = files[filename]
    return table


def validate_gma1(data):
    required_size = GMA_TABLE_OFFSET + (max(GMA_TABLE_FILES) + 1) * 2
    if len(data) < required_size:
        raise RuntimeError("GMA1 file is too short")
    if data[0:3] != b"\x34\x03\x4C":
        raise RuntimeError("GMA1 does not have the expected PRG header and JMP")


def table_entry_offset(index):
    return GMA_TABLE_OFFSET + index * 2


def patch_gma1(path, table):
    data = bytearray(path.read_bytes())
    validate_gma1(data)

    for index, (track, sector) in table.items():
        offset = table_entry_offset(index)
        data[offset] = track
        data[offset + 1] = sector

    with tempfile.NamedTemporaryFile(dir=str(path.parent), delete=False) as temp:
        temp.write(data)
        temp_path = Path(temp.name)
    os.replace(str(temp_path), str(path))


def verify_table(data, table, source):
    validate_gma1(data)
    for index, expected in table.items():
        offset = table_entry_offset(index)
        actual = (data[offset], data[offset + 1])
        if actual != expected:
            raise RuntimeError(
                "{} has ${:02X}/${:02X} for GMA{}, expected ${:02X}/${:02X}"
                .format(source, actual[0], actual[1], index,
                        expected[0], expected[1]))


def verify_disk_gma1(image, files, table):
    if "GMA1" not in files:
        raise RuntimeError("File GMA1 is missing from the D64 image")

    track, sector = files["GMA1"]
    offset = sector_offset(image, track, sector)
    first_file_block = image[offset + 2:offset + 256]
    verify_table(first_file_block, table, "GMA1 in the D64 image")


def table_summary(table):
    return ", ".join("GMA{}=${:02X}/${:02X}".format(index, track, sector)
                     for index, (track, sector) in sorted(table.items()))


def main():
    parser = argparse.ArgumentParser(
        description="Patch or verify the GMA86 fast-loader sector table")
    parser.add_argument("--disk", required=True, type=Path,
                        help="GMA86 PAL D64 image")
    parser.add_argument("--gma1", required=True, type=Path,
                        help="gma1.unprot.bin to patch or verify")
    parser.add_argument("--verify", action="store_true",
                        help="verify the final binary and D64 instead of patching")
    args = parser.parse_args()

    image = args.disk.read_bytes()
    files = read_directory(image)
    table = expected_table(files)

    if args.verify:
        verify_table(args.gma1.read_bytes(), table, str(args.gma1))
        verify_disk_gma1(image, files, table)
        print("GMA fast-loader sector table verified: {}".format(
            table_summary(table)))
    else:
        patch_gma1(args.gma1, table)
        verify_table(args.gma1.read_bytes(), table, str(args.gma1))
        print("GMA fast-loader sector table updated: {}".format(
            table_summary(table)))


if __name__ == "__main__":
    main()
