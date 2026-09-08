"""Host-side format and pulse integrity tests; no emulator required."""
import binascii
import random
import unittest
from unittest.mock import patch
from contextlib import redirect_stderr
import io
import build
import tape_codec as tape

class TransferTests(unittest.TestCase):
    def test_tapfile_name_and_extension(self):
        self.assertIsNone(build.parse_args([]).tapfile)
        for value, expected in (('Moje hra', 'Moje hra.tap'), ('Moje hra.tap', 'Moje hra.tap'), ('GAME.TAP', 'GAME.TAP')):
            with self.subTest(value=value):
                self.assertEqual(build.parse_args(['tapfile=' + value]).tapfile, expected)
        self.assertEqual(build.parse_args(['--tapfile', 'Test']).tapfile, 'Test.tap')

    def test_reject_invalid_tapfile_names(self):
        for value in ('', '../escape', 'dir/file.tap', 'dir\\file.tap', 'C:\\file.tap', 'bad:name', 'NUL', 'CON.tap', 'bad*name', 'bad.', 'bad '):
            with self.subTest(value=value), redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
                build.parse_args(['tapfile=' + value])

    def test_named_parameters_and_limits(self):
        args = build.parse_args(['d64=C:/images/test disk.d64', 'tapename=abcdefghijklmnopqrstuvwxyz', 'label=123456789012345678901234567890'])
        self.assertEqual(args.d64.name, 'test disk.d64')
        self.assertEqual(args.tapename, 'ABCDEFGHIJKLMNOP')
        self.assertEqual(args.label, '1234567890123456789012345')

    def test_relative_path_from_batch_caller(self):
        with patch.dict('os.environ', {'ELITE_TAPD64_CALLER_DIR': str(build.ROOT)}):
            args = build.parse_args(['d64=sub dir/test.d64'])
        self.assertEqual(args.d64, (build.ROOT / 'sub dir/test.d64').resolve())

    def test_defaults_and_legacy_arguments(self):
        args = build.parse_args([])
        self.assertEqual((args.tapename, args.label), ('TAPD64', 'ELITE: UNBOUND'))
        self.assertEqual(build.parse_args(['test.d64']).d64.name, 'test.d64')
        self.assertEqual(build.parse_args(['--tapename', 'name', '--label', 'Title']).label, 'TITLE')

    def test_c64_text_and_safe_assembler_data(self):
        self.assertEqual(build.c64_text('Žluťoučký kůň', 25), 'ZLUTOUCKY KUN')
        self.assertEqual(build.c64_text('A\nB😀', 25), 'A B?')
        title = 'A "B"; & C!'
        assembly = build.title_assembly(title)
        self.assertNotIn(title, assembly)
        encoded = assembly.split(' EQUB ')[1].splitlines()[0]
        self.assertEqual(bytes(map(int, encoded.split(','))), title.encode('ascii'))

    def test_reject_empty_and_conflicting_parameters(self):
        for argv in (['label='], ['tapename=   '], ['d64='], ['d64=a.d64', 'b.d64']):
            with self.subTest(argv=argv), redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
                build.parse_args(argv)

    def test_named_tape_header_contains_exactly_16_characters(self):
        name = build.parse_args(['tapename=abcdefghijklmnopTOO LONG']).tapename
        pulses = bytearray()
        prg = b'\x00\xc0\x60'
        tape.append_standard_prg(pulses, prg, name, tape.PAL_CLOCK)
        pos = 27135
        countdown, pos = tape._decode_rom_bytes(pulses, pos, 9)
        header, pos = tape._decode_rom_bytes(pulses, pos, 192)
        self.assertEqual(header[5:21], b'ABCDEFGHIJKLMNOP')
        tape.verify_standard_section(pulses, prg, name)

    def test_geometry(self):
        self.assertEqual(sum(map(build.sectors, range(1, 36))), 683)
        self.assertEqual(set(build.TRACKS), set(range(1, 36)))
        self.assertEqual(build.TRACKS[-1], 18)

    def test_all_track_shapes(self):
        rng = random.Random(1541)
        for track in range(1, 36):
            for pattern in (bytes(build.sectors(track)*256),
                            bytes([0xff])*build.sectors(track)*256,
                            rng.randbytes(build.sectors(track)*256)):
                encoded = build.encode_track(pattern)
                self.assertEqual(build.decode_track(encoded, track), pattern)
                self.assertLessEqual(len(encoded), 21*257)
                self.assertLess(build.BUFFER+len(encoded), 0x6000)

    def test_no_assumption_about_free_sector_contents(self):
        # Nonzero data in unallocated sectors must survive too.
        raw = bytes(256) + bytes(range(256)) + bytes([0xa5])*256 + bytes(18*256)
        encoded = build.encode_track(raw)
        self.assertEqual(build.decode_track(encoded, 1), raw)
        self.assertEqual(len(encoded), 21+512)

    def test_reject_malformed_sparse_tracks(self):
        for encoded in (b"", bytes([2])*21, b"\x01", bytes(22)):
            with self.assertRaises(ValueError):
                build.decode_track(encoded, 1)

    def test_crc_reference(self):
        self.assertEqual(binascii.crc_hqx(b"123456789", 0xffff), 0x29b1)

    def test_turbo_corruption(self):
        payload = build.encode_track(bytes(range(256))*21)
        pulses = bytearray()
        tape.turbo_block(pulses, 1, build.BUFFER, payload, pilot_bytes=2048)
        self.assertEqual(tape.verify_turbo_block(pulses, 0, 2048, 1, build.BUFFER, payload), len(pulses))
        pulses[-20] = tape.TURBO_LONG if pulses[-20] == tape.TURBO_SHORT else tape.TURBO_SHORT
        with self.assertRaises(ValueError):
            tape.verify_turbo_block(pulses, 0, 2048, 1, build.BUFFER, payload)

if __name__ == "__main__":
    unittest.main()
