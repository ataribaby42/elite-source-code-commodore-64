#!/usr/bin/env python3
"""Exercise assembled docking/police code after an unbound=yes build.

Requires py65 (python -m pip install py65). Run from the repository root:
    python 2-build-files/test-scrambled-docking.py

Addresses and loaded code come from the current assembler listing and binaries.
The CPU executes the real routines. Hooks substitute only unrelated geometry,
sound, random inputs, and the final ship-allocation operation; no game bytes are
patched. --police-only also works with a pre-change build for comparison.
"""

import argparse
import hashlib
import json
import re
from pathlib import Path

from py65.devices.mpu6502 import MPU


class Game:
    def __init__(self, build_dir):
        listing = (build_dir / "compile.txt").read_text(encoding="utf-8")
        self.symbols = {}
        pending = []
        for line in listing.splitlines():
            label = re.fullmatch(r"\.([\w%]+)", line.strip())
            if label:
                pending.append(label[1])
            address = re.match(r"^\s+([0-9A-F]{4})(?:\s|$)", line)
            if address:
                for name in pending:
                    self.symbols.setdefault(name, int(address[1], 16))
                pending.clear()

        self.ram = [0] * 65536
        for name, address in re.findall(r"^S\.(ELT[A-K]) &([0-9A-F]+)", listing, re.M):
            self.load(build_dir / (name + ".bin"), int(address, 16))
        self.load(build_dir / "HANGAR.bin", self.symbols["HANGAR_CODE"])
        self.load(build_dir / "C128-RASTER.bin", self.symbols["C128RasterStart"])
        self.count = 0

    def load(self, path, address):
        data = path.read_bytes()
        self.ram[address:address + len(data)] = data

    def cpu(self, **values):
        cpu = MPU(memory=self.ram.copy())
        for name, value in values.items():
            cpu.memory[self.symbols[name]] = value
        return cpu

    def run(self, cpu, entry, stops=(), hooks=None):
        hooks = {self.symbols[k]: v for k, v in (hooks or {}).items()}
        stops = {self.symbols[k]: k for k in stops}
        cpu.pc = self.symbols[entry]
        old_sp = cpu.sp
        # The return address is an artificial test boundary, outside game code.
        cpu.stPushWord(0x01FF)
        for _ in range(10000):
            if cpu.pc == 0x0200 or cpu.pc in stops:
                result = stops.get(cpu.pc, "return")
                cpu.sp = old_sp
                self.count += 1
                return result
            if cpu.pc in hooks:
                hooks[cpu.pc](cpu)
            else:
                cpu.step()
        raise AssertionError((entry, "CPU timeout", hex(cpu.pc)))


def rts(cpu):
    cpu.pc = (cpu.stPopWord() + 1) & 0xFFFF


def random_byte(value):
    def hook(cpu):
        cpu.a = cpu.x = value
        cpu.p = (cpu.p & ~(cpu.NEGATIVE | cpu.ZERO)) | (value & 0x80)
        if value == 0:
            cpu.p |= cpu.ZERO
        rts(cpu)
    return hook


def registration_tests(game, station_flags):
    s = game.symbols
    for government in range(8):
        for scrambled in (0, 255):
            for record in range(256):
                cpu = game.cpu(SSPR=1, gov=government, regplate_scrambled=scrambled,
                               FIST=record)
                cpu.x, cpu.y = 73, 219
                cpu.memory[station_flags] = 0xA1
                game.run(cpu, "ScrambleRegistrationDockingDenied")
                denied = scrambled == 255 and government not in (0, 1, 3)
                assert cpu.a == (255 if denied else 0)
                assert bool(cpu.p & cpu.ZERO) == (not denied)
                assert (cpu.x, cpu.y) == (73, 219)
                assert cpu.memory[s["FIST"]] == record
                assert cpu.memory[station_flags] == 0xA1

    # A cleared safe-zone flag always allows the registration check.
    cpu = game.cpu(SSPR=0, gov=7, regplate_scrambled=255, FIST=17)
    game.run(cpu, "ScrambleRegistrationDockingDenied")
    assert cpu.a == 0 and cpu.memory[s["FIST"]] == 17

    def aligned_station(cpu):
        cpu.memory[s["XX15"] + 2] = 127
        rts(cpu)

    # Exercise the actual docking gate, including the normal alignment checks.
    for government in range(8):
        for scrambled in (0, 255):
            for hostile in (0, 4):
                for autopilot in (0, 255):
                    cpu = game.cpu(SSPR=1, gov=government, regplate_scrambled=scrambled,
                                   FIST=0, auto=autopilot)
                    cpu.memory[station_flags] = hostile
                    cpu.memory[s["INWK"] + 14] = 255
                    cpu.memory[s["INWK"] + 16] = 127
                    result = game.run(cpu, "ISDK", ("MA62", "GOIN"),
                                      {"SPS1": aligned_station})
                    denied = hostile or (scrambled and government not in (0, 1, 3))
                    assert result == ("MA62" if denied else "GOIN")
                    assert cpu.memory[s["FIST"]] == 0
                    assert cpu.memory[station_flags] == hostile


def warning_tests(game, station_flags):
    s = game.symbols
    for government in range(8):
        for record in (0, 39, 40, 49, 50, 99, 100, 255):
            cpu = game.cpu(SSPR=1, gov=government, regplate_scrambled=255, FIST=record)
            cpu.memory[station_flags] = 0xA1
            denied = government not in (0, 1, 3)
            result = game.run(cpu, "ScrambleRegistrationSafeZone",
                              ("ScrambleRegistrationPirateMessage",))
            assert (result == "ScrambleRegistrationPirateMessage") == denied
            assert cpu.memory[s["ScrambleRegistrationBlocked"]] == (255 if denied else 0)
            # The following frames must neither repeat the message nor change FIST.
            for _ in range(3):
                assert game.run(cpu, "ScrambleRegistrationSafeZone",
                                ("ScrambleRegistrationPirateMessage",)) == "return"
            assert cpu.memory[s["FIST"]] == record
            assert cpu.memory[station_flags] == 0xA1

    # Leaving/re-entering and restoring/scrambling the ID rearm the warning.
    for reset_name, clear, restore in (("SSPR", 0, 1), ("regplate_scrambled", 0, 255)):
        cpu = game.cpu(SSPR=1, gov=7, regplate_scrambled=255, FIST=23)
        for values, expected in (({}, True), ({reset_name: clear}, False),
                                 ({reset_name: restore}, True)):
            for key, value in values.items():
                cpu.memory[s[key]] = value
            result = game.run(cpu, "ScrambleRegistrationSafeZone",
                              ("ScrambleRegistrationPirateMessage",))
            assert (result == "ScrambleRegistrationPirateMessage") == expected
            assert cpu.memory[s["FIST"]] == 23

    # Real communication preparation/queueing; only the sound device is omitted.
    kinds = set()
    for seed in range(32):
        for screen in (0, 1):
            cpu = game.cpu(SSPR=1, gov=7, regplate_scrambled=255, FIST=0,
                           QQ11=screen, regplate_1=74, regplate_2=83, regplate_3=42)
            cpu.memory[s["RAND"]:s["RAND"] + 4] = [seed, seed * 3, 47, 83]
            beeps = []

            def beep(cpu):
                beeps.append(True)
                rts(cpu)

            result = game.run(cpu, "ScrambleRegistrationSafeZone", ("MESS",), {"BEEP": beep})
            kinds.add(cpu.memory[s["CommMessageRequestedKind"]])
            assert len(beeps) == 1
            assert cpu.memory[s["FIST"]] == 0
            assert cpu.memory[station_flags] == 0
            if screen:
                assert result == "return"
                assert cpu.memory[s["CommMessagePending"]] == 0x80
                assert cpu.memory[s["CommMessageRecipientHidden"]] == 255
            else:
                assert result == "MESS" and cpu.a == 2
                assert cpu.memory[s["CommMessagePending"]] == 0
    assert kinds == {1, 2, 3}, kinds

    cpu = game.cpu(SSPR=0, ScrambleRegistrationBlocked=255, BOMB=0)
    game.run(cpu, "RES2", ("WPSHPS",))
    assert cpu.memory[s["ScrambleRegistrationBlocked"]] == 0


def police_tests(game, types):
    s = game.symbols
    outcomes = []
    records = (0, 39, 40, 49, 50, 99, 100, 255)
    # Station police retain their hostile-station, random-chance and count gates.
    # A blocked launch corridor must not prevent an otherwise valid Viper launch.
    for record in records:
        for scrambled in (0, 255):
            for hostile in (0, 4):
                for cops in (0, 3, 4, 7):
                    for roll in range(256):
                        cpu = game.cpu(FIST=record, regplate_scrambled=scrambled,
                                       TYPE=types["SST"], NEWB=hostile)
                        cpu.x = types["SST"]
                        cpu.memory[s["MANY"] + types["COPS"]] = cops
                        cpu.memory[s["MANY"] + types["SHU"] + 1] = 1
                        result = game.run(cpu, "TACTICS", ("SFS1", "UnboundStationLaunchClear"),
                                          {"DORND": random_byte(roll)})
                        expected = bool(hostile and cops < 4 and roll >= 240)
                        assert (result == "SFS1") == expected
                        assert result != "UnboundStationLaunchClear"
                        if expected:
                            assert cpu.x == types["COPS"] and cpu.a == 0xF1
                        assert cpu.memory[s["FIST"]] == record
                        outcomes.append(expected)

    # Existing police/bounty-hunter legal-status threshold is exactly 40.
    for record in range(256):
        for scrambled in (0, 255):
            cpu = game.cpu(FIST=record, regplate_scrambled=scrambled, NEWB=2)
            cpu.a = 1  # TN1 receives NEWB shifted once by TA14.
            game.run(cpu, "TN1", ("TN2",))
            flags = cpu.memory[s["NEWB"]]
            assert flags == (6 if record >= 40 else 2)
            assert cpu.memory[s["FIST"]] == record
            outcomes.append(flags)

    # Random police outside the zone still depend on cargo/legal record and RNG.
    for record in records:
        for scrambled in (0, 255):
            for safe_zone in (0, 1):
                for existing in (0, 1):
                    for cargo in (0, 20):
                        for roll in (0, 39, 40, 49, 50, 99, 100, 135, 136, 137, 254, 255):
                            cpu = game.cpu(FIST=record, regplate_scrambled=scrambled,
                                           SSPR=safe_zone)
                            cpu.memory[s["MANY"] + types["COPS"]] = existing
                            spawned = []

                            def allocate(cpu):
                                spawned.append(cpu.a)
                                rts(cpu)

                            result = game.run(cpu, "MTT1", ("MLOOP", "randomPoliceDone", "fothg"),
                                              {"BAD": random_byte(cargo), "Ze": random_byte(roll),
                                               "NWSHP": allocate, "RandomPoliceSpawnComplete": rts})
                            badness = (2 * cargo) | (record if existing else 0)
                            expected = not safe_zone and roll != 136 and roll < badness
                            assert spawned == ([types["COPS"]] if expected else [])
                            assert cpu.memory[s["FIST"]] == record
                            outcomes.append((result, spawned))
    digest = hashlib.sha256(json.dumps(outcomes).encode()).hexdigest()
    print("Police behavior fingerprint:", digest)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-dir", type=Path, default=Path("3-assembled-output"))
    parser.add_argument("--police-only", action="store_true")
    args = parser.parse_args()
    source = Path("1-source-files/main-sources/elite-source.asm").read_text(encoding="utf-8")
    types = {name: int(re.search(r"^ " + re.escape(name) + r" = (\d+)", source, re.M)[1])
             for name in ("SST", "COPS", "SHU", "NI%")}
    game = Game(args.build_dir)
    station_flags = game.symbols["K%"] + types["NI%"] + 36
    if not args.police_only:
        registration_tests(game, station_flags)
        warning_tests(game, station_flags)
    police_tests(game, types)
    print("PASS:", game.count, "assembled-code scenarios")
    for name, limit in (("R%", 0x4000), ("F%", 0xCE00)):
        end = game.symbols[name]
        print(f"{name}=${end:04X}; usable bytes before strict limit: {limit - end - 1}")


if __name__ == "__main__":
    main()
