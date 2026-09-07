"""Host-side format and pulse integrity tests; no emulator required."""
import binascii
import random
import unittest
import build
import tape_codec as tape

class TransferTests(unittest.TestCase):
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
