"""End-to-end TAP test in VICE; writes only output/vice-test.d64."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import socket
import subprocess
import time
import build

ROOT = Path(__file__).resolve().parent

def symbols():
    listing = (ROOT / "output/compile.txt").read_text()
    found = {}
    pending = []
    for line in listing.splitlines():
        if re.fullmatch(r"\.[A-Za-z][A-Za-z0-9]*", line.strip()):
            pending.append(line.strip()[1:])
        match = re.match(r"\s+([0-9A-F]{4})\s+[0-9A-F]{2}", line)
        if match:
            for label in pending:
                found[label] = int(match[1], 16)
            pending.clear()
    return found

class Monitor:
    def __init__(self, port):
        self.sock = socket.create_connection(("127.0.0.1", port), timeout=5)
        self.sock.settimeout(.3)
    def command(self, text, timeout=10):
        # A breakpoint may have emitted an unsolicited prompt since last call.
        self.sock.settimeout(.02)
        try:
            while self.sock.recv(65536):
                pass
        except socket.timeout:
            pass
        self.sock.settimeout(.3)
        self.sock.sendall((text + "\n").encode())
        result = bytearray()
        until = time.monotonic()+timeout
        while time.monotonic() < until:
            try:
                part = self.sock.recv(65536)
                if not part:
                    break
                result.extend(part)
                if re.search(rb"\(C:\$?[0-9a-fA-F]+\)\s*$", result):
                    break
            except socket.timeout:
                if text in ("x", "quit"):
                    break
        return result.decode(errors="replace")

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--vice", default=str(ROOT.parents[1]/"vice/bin/x64sc.exe"))
    parser.add_argument("--timeout", type=int, default=600)
    parser.add_argument("--fault", choices=("crc",), help="Corrupt loaded RAM before CRC; expect failure")
    args = parser.parse_args()
    out = ROOT / "output"
    manifest = json.loads((out / "manifest.json").read_text())
    run_name = "vice-crc-test" if args.fault else "vice-test"
    target = out / (run_name + ".d64")
    target.write_bytes(bytes([0xA5])*174848)
    syms = symbols()
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        port = sock.getsockname()[1]
    startup = out / "vice-start.mon"
    commands = f'break ${syms["Confirm"]:04x}\nbreak ${syms["SuccessHalt"]:04x}\nbreak ${syms["FailureHalt"]:04x}\n'
    if args.fault:
        commands += f'break ${syms["CheckCrc"]:04x}\n'
    startup.write_text(commands + 'x\n')
    log = (out / "vice.log").open("w")
    process = subprocess.Popen([args.vice, "-default", "-console", "+sound", "-warp",
        "-drive8type", "1541", "-drive8truedrive", "+virtualdev8", "+virtualdev1", "-8", str(target),
        "-remotemonitor", "-remotemonitoraddress", f"ip4://127.0.0.1:{port}",
        "-initbreak", "reset", "-moncommands", str(startup),
        "-autostart", str(out / manifest["tap"])], stdout=log, stderr=log,
        creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
    mon = None
    transcript = []
    try:
        for _ in range(100):
            if process.poll() is not None:
                raise RuntimeError((out / "vice.log").read_text())
            try:
                mon = Monitor(port)
                break
            except OSError:
                time.sleep(.1)
        if mon is None:
            raise RuntimeError("Monitor unavailable")
        started = time.monotonic()
        confirmed = False
        injected = False
        last_progress = ""
        while time.monotonic()-started < args.timeout:
            state = mon.command("r")
            transcript.append(state)
            screen = mon.command("screen")
            transcript.append(screen)
            screen = screen.upper()
            lines = [line[9:].strip() for line in screen.splitlines() if line.startswith("*C:") and line[9:].strip()]
            progress = lines[-1] if lines else "Starting"
            if progress != last_progress:
                print(progress, flush=True)
                last_progress = progress
            if "ERASE ENTIRE DISK" in screen and not confirmed:
                transcript.append(mon.command("delete 1"))
                transcript.append(mon.command('keybuf "y"'))
                confirmed = True
            if args.fault and not injected and f'.;{syms["CheckCrc"]:04x}' in state.lower():
                transcript.append(mon.command("delete 4"))
                # First byte is a generated tag (0 or 1). $80 must differ.
                transcript.append(mon.command("> 4000 80"))
                injected = True
            if "ERROR AT" in screen or "MISMATCH AT" in screen:
                if args.fault == "crc" and injected and "TAPE ERROR AT" in screen and "WRITE / VERIFY" not in screen:
                    report = {"result": "PASS", "test": "corrupt track rejected before sector writes", "tap_sha256": manifest["tap_sha256"]}
                    (out / (run_name + ".json")).write_text(json.dumps(report, indent=2)+"\n")
                    print("PASS: corrupted track rejected before sector writes", flush=True)
                    return
                raise RuntimeError("C64 writer stopped with error")
            if "ALL 683 SECTORS VERIFIED" in screen:
                transcript.append(mon.command("quit"))
                process.wait(timeout=10)
                actual = hashlib.sha256(target.read_bytes()).hexdigest()
                if actual != manifest["source_sha256"]:
                    raise RuntimeError(f"Final D64 differs: {actual}")
                report = {"result": "PASS", "test": "complete TAP -> true 1541 -> D64", "sectors": 683,
                          "source_sha256": manifest["source_sha256"], "result_sha256": actual,
                          "tap_sha256": manifest["tap_sha256"], "wall_seconds": round(time.monotonic()-started, 2)}
                (out / (run_name + ".json")).write_text(json.dumps(report, indent=2)+"\n")
                print("PASS: complete TAP -> 1541 -> D64, SHA256", actual, flush=True)
                return
            transcript.append(mon.command("x"))
            time.sleep(3)
        raise TimeoutError("VICE test timeout")
    finally:
        (out / (run_name + "-monitor.txt")).write_text("\n".join(transcript), encoding="utf-8")
        if mon:
            try:
                mon.command("quit", timeout=1)
            except OSError:
                pass
            mon.sock.close()
        if process.poll() is None:
            process.terminate()
            process.wait(timeout=10)
        log.close()

if __name__ == "__main__":
    main()
