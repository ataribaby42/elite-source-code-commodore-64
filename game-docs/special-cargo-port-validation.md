# Special Cargo port: validation, 2026-09-06

Implemented independently in **main** and **flicker-free**, only for `unbound=yes`.
Ctrl+1 while docked opens the offers or active delivery; W on both charts selects
its destination. Ctrl+2 retains equipment sales and in-flight cargo jettison.

## Final label adjustment

Removed the FEE/CR heading and its rendering code from the offer screen.
Removed the literal space after Cargo: in Inventory, giving e.g.
`Cargo:Riantiat 6553.5 CR`. Both changes apply only to Unbound in both branches.
They save 17 B HICODE and require no LOCODE or RLE changes.

Eight builds passed: the Basic and Full command prefixes documented in the
UI follow-up below, each with `variant=tape-pal unbound=yes encrypt=no` and
`variant=gma86-pal unbound=yes`, in each working copy. Basic builds have
161 / 197 B addable LOCODE / HICODE in main and 81 / 97 B in flicker-free.
Full builds have the current memory reserves listed below. Encrypted GMA86
sector-table verification passed. PAL TAP is the active build in both copies;
NTSC and EasyFlash distribution files remain from the preceding build.

Fresh PAL TAP runs in VICE x64sc verified both screens and successful offer
acceptance in each branch. Screenshots were visually checked. Build commands,
logs, listings and media are under `3-assembled-output/special-cargo-labels/results/`;
screenshots and results are under its `vice/` directory.
Only `elite-special-cargo.asm` and `PROJECT_NOTES.md` changed in each branch,
plus this report in main. Existing work is retained; no commit or push.

## UI follow-up: Inventory and acceptance feedback

The active contract now appears as `Cargo: Lave 535.0 CR` in Inventory's
previously blank row above Fuel. With no contract the row remains blank.
The destination lookup preserves QQ15, chart coordinates QQ9/QQ10, distance QQ8
and system index ZZ. It visits at most 256 systems; invalid destination
coordinates leave the row blank. No persistent state or save-format changes.
An assembler assertion checks that the label, an eight-letter planet name,
the full amount field and currency fit inside the screen.

VALUE and the insufficient-balance Cash? prompt start one character further
right, at column 2. Successful acceptance calls BEEP (effect 5), the same tone
used by the Equip Ship purchase path. Reopening a contract does not beep.

Fresh-TAP monitor-assisted VICE checks passed on main and flicker-free C64
PAL/NTSC, plus flicker-free C128 PAL in C64 mode:

- acceptance reaches BEEP once and dispatches effect 5; reopening adds no beep;
- VALUE and Cash? start in column 2; failure does not charge or accept;
- `Cargo: Lave 535.0 CR` appears on row 3, above Fuel;
- all 17 commodity rows plus Large Cargo Bay remain in place; the bitmap with
  and without a contract differs only on row 3;
- `Cargo: Riantiat 6553.5 CR` fits without wrapping;
- the screen preserves navigation, the contract, cash and commodity quantities,
  both docked and after the real launch path;
- an invalid target finishes its bounded search and changes no text or state.

Screenshots of the alignment, long line and in-flight Inventory were visually
inspected. Sound validation checks the real BEEP/NOISE dispatch; the automated
VICE instances disable host audio. One main NTSC emulator process initially
failed during startup; a fresh process completed the suite.

All 20 branch/configuration builds in this follow-up passed. GMA86 sector-table
validation passed. The 36 actual component/media hashes for each unbound=no
PAL TAP and GMA86 case match the pre-follow-up outputs in both branches; three
unused Unbound-only standalone exports are excluded. The final active outputs
are full PAL TAP builds. Replacing the amount-width literal with a named constant
and adding the width assertion produced byte-identical PAL outputs.

The exact command for every row below is `2-build-files/make.exe` followed by:

```text
BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes realmissiledamage=yes
```

Full configurations additionally use:

```text
scannercolorfix=no renderspeedups=yes planetdatafix=yes bountyhunterfix=yes
```

Run independently from each branch's working directory, then append the options
in the table. Every row passed in both branches:

| Configuration | Additional command options | main LO / HI free | flicker-free LO / HI free |
|---|---|---:|---:|
| Full original mode | variant=tape-pal unbound=no encrypt=no | 263 / 9 B | 60 / 33 B |
| Full original mode | variant=gma86-pal unbound=no | 263 / 9 B | 60 / 33 B |
| Basic | variant=tape-pal unbound=yes encrypt=no | 161 / 180 B | 81 / 80 B |
| Basic | variant=gma86-pal unbound=yes | 161 / 180 B | 81 / 80 B |
| Full | variant=gma86-pal unbound=yes | 95 / 104 B | 15 / 4 B |
| Full | variant=gma85-ntsc unbound=yes | 95 / 104 B | 15 / 4 B |
| Full | variant=easyflash-ntsc unbound=yes encrypt=no | 95 / 104 B | 15 / 4 B |
| Full | variant=easyflash-pal unbound=yes encrypt=no | 95 / 104 B | 15 / 4 B |
| Full | variant=tape-ntsc unbound=yes encrypt=no | 95 / 104 B | 15 / 4 B |
| Full | variant=tape-pal unbound=yes encrypt=no | 95 / 104 B | 15 / 4 B |

Evidence is in `3-assembled-output/special-cargo-feedback/`: exact argument
arrays, build logs and listings in `results/`, screenshots and emulator reports
in `vice/`, the harness in `check.py`, and hash/RLE checks in `final-checks.json`.
This follow-up changes both ASM files and PROJECT_NOTES in each branch, this
report in main, and the English/Czech manual descriptions in flicker-free.
Earlier uncommitted work is retained; no commit or push was performed.

## Source and behaviour

Reference: [Elite-A Special Cargo](https://elite.bbcelite.com/deep_dives/elite-a_special_cargo_missions.html)
and [the documented Elite-A source](https://github.com/markmoxon/elite-a-source-code-bbc-micro/tree/9c7d0450b96ad88e9747c71cc05d506b7c2efa44).
The port retains the original offer, fee, reward and illegality arithmetic.
One active contract is paid once at its destination, halved at any other docking,
and cancelled by galactic hyperspace. Normal hyperspace does not reduce it.

C64 adaptations include the docked Ctrl+1 shortcut, an active-contract screen,
a no-offers message, a separate Cash? error row and a four-byte save extension.
The first 77 commander bytes retain their offsets. New files have 81 data bytes,
83 including the PRG header. Old files load with an empty contract. Default
JAMESON resets all four bytes; declining confirmation preserves them.

## Memory in the full configuration

| Branch | R% | F% | Addable LOCODE | Addable HICODE | Addable RLE |
|---|---|---|---:|---:|---:|
| main | $3FA0 | $CD86 | 95 B | 121 B | 4 B |
| flicker-free | $3FF0 | $CDEA | 15 B | 21 B | 4 B |

These are separate regions and already allow for the strict assembler limits.
Including the final label adjustment, Special Cargo adds 3 B LOCODE and 695 B HICODE
(656 B in the new module and 39 B of screen/dispatch/load/galactic-jump
integration). Live commander RAM grows by
four bytes; saved/default copies reuse existing padding. The 96-byte menu tables
share the idle save/load staging page. The RLE payload is unchanged: 2 usable
bytes before communications data and 2 addable bytes after the hangar
(3 physical bytes there, but the payload limit is strict).

## Emulator validation

Full suites passed on VICE 3.9:

- main: C64 PAL and NTSC (`x64sc`);
- flicker-free: C64 PAL and NTSC (`x64sc`), C128 PAL in C64 mode (`x128 -go64`).

The suites start from the freshly built TAP. The binary monitor prepares test
commander state, injects chords into the game's key logger and ASCII responses
at the keyboard routine's return, and chooses actual routine entry points.
The game's menu renderer, number parser, money arithmetic, docking, hyperspace,
commander copy loops, KERNAL disk operations and raster IRQs execute normally.
This is monitor-assisted emulator testing, not a manual physical-keyboard run or
an entire story-mission playthrough.

Verified in every suite:

- New commander has no delivery; Ctrl+1 is docked-only.
- Zero, one and fifteen offers render within the screen; destinations and fees
  are aligned. Corresponding emulator screenshots were inspected visually.
- Reopening an unaccepted menu preserves its deterministic offers.
- Cancellation, zero/out-of-range input and insufficient cash do not accept
  cargo or change the balance. Selecting 15 or Y uses the final row's fee/target.
- Reopening the active contract cannot replace or charge for it again.
- W selects the delivery destination on both charts while docked and in flight;
  without a contract it leaves the selection unchanged. Distant targets can be
  outside the visible short-range chart.
- Wrong-station docking halves the reward; one expires to zero. Target docking
  pays once, clears the reward, and cannot pay it a second time. Merely opening
  the station screen does neither. Real normal hyperspace retains the contract;
  galactic travel cancels it. Ctrl+2 still opens jettison for an occupied hold.
- Real KERNAL disk SAVE and LOAD preserve all 81 bytes exactly. Extracted saved
  PRGs from all five emulator cases are exactly 83 bytes long. Loading a real
  old 77-byte fixture clears the extension after staging RAM was poisoned.
- The real Save/Load menu's Default JAMESON option plus YESNO handler preserves
  the contract on N; Y clears all four bytes and restores Adder/100 Cr.
- Delivery pays before Constrictor briefing/debriefing and all three Thargoid
  Plans docking events. Their existing entry points are reached.
- Illegal acceptance applies the expected wrapping FIST increment while leaving
  TP and the ordinary commodity hold unchanged.

C128 disk I/O was run from a fresh boot before snapshot-driven scenarios. An
initial test that repeatedly restored C128 snapshots stalled during subsequent
KERNAL setup; the fresh-boot save/reload/legacy-load sequence passed. No game-code
workaround was added for that test-harness state.

Separately, py65 compared 768 offer-generation cases **per branch** against the
original Elite-A cour_buy instructions assembled separately, using the same C64
math helpers and stubbing only output routines. Every offer's coordinates,
illegality, fee and reward matched. Cases include every market seed at Lave and
512 combinations of ship type, galaxy, legal status, position and previous target.

## Initial port build validation, before the UI follow-up

All **24 distinct branch/configuration builds** passed (12 per branch).
Tape and EasyFlash builds explicitly disable encryption; GMA builds retain the
default encryption. GMA86 disks also passed the generated sector-table checks.
The final active outputs in both workspaces are the full tape-pal configuration.
PAL/NTSC TAP, GMA D64 and EasyFlash CRT distribution outputs were refreshed.

For unbound=no, 36 actual game component/media hashes per PAL tape/GMA86 case
match the clean pre-port baseline in both branches. Unbound-only standalone
exports left in the output directory by other builds were excluded because they
are neither assembled nor linked by unbound=no.

## Changed files and evidence

Both workspaces:

- `1-source-files/main-sources/elite-source.asm`: commander extension, hooks and bounds.
- `1-source-files/main-sources/elite-special-cargo.asm`: new HICODE module.
- `PROJECT_NOTES.md`: behaviour, format and current memory measurements.

Main additionally:

- `game-docs/elite-unbound-save-map.src`;
- `game-docs/elite-unbound-missions.md`;
- `game-docs/special-cargo-port-validation.md` (this report).

Flicker-free additionally:

- `docs/instruction-manual.html`;
- `docs/cs/instruction-manual.html`.

English/Czech manual section IDs and internal links passed an HTML parser check.
Generated tracked README content was restored. Existing unrelated files were not
changed. Both workspaces contain the intentional uncommitted changes above;
**no commit or push was performed**.

Local evidence in the main workspace is under
`3-assembled-output/special-cargo-port/`: per-build commands/listings/binaries and
logs in `results/`, original-mode baselines in `baseline/`, compatibility builds
in `defaults/` and `energy-bomb/`, screenshots/snapshots/extracted PRGs and PASS
reports in `vice/`, and `main-logic.json` / `flicker-free-logic.json`.
The monitor harness is `vice_check.py`; the reference comparison is `logic_check.py`.

## Initial port exact commands and results, before the UI follow-up

### main

Working directory: `E:/Development/Elite-C64/elite-source-code-commodore-64`

**easyflash-ntsc-unbound-yes: PASS**; addable LOCODE 95 B, HICODE 202 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=easyflash-ntsc unbound=yes encrypt=no
```

**easyflash-pal-unbound-yes: PASS**; addable LOCODE 95 B, HICODE 202 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=easyflash-pal unbound=yes encrypt=no
```

**gma85-ntsc-unbound-yes: PASS**; addable LOCODE 95 B, HICODE 202 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=gma85-ntsc unbound=yes
```

**gma86-pal-unbound-no: PASS**; addable LOCODE 263 B, HICODE 9 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=gma86-pal unbound=no
```

**gma86-pal-unbound-yes: PASS**; addable LOCODE 95 B, HICODE 202 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=gma86-pal unbound=yes
```

**gma86-pal-unbound-yes-basic: PASS**; addable LOCODE 161 B, HICODE 278 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes realmissiledamage=yes variant=gma86-pal unbound=yes
```

**tape-ntsc-unbound-yes: PASS**; addable LOCODE 95 B, HICODE 202 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=tape-ntsc unbound=yes encrypt=no
```

**tape-pal-unbound-no: PASS**; addable LOCODE 263 B, HICODE 9 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=tape-pal unbound=no encrypt=no
```

**tape-pal-unbound-yes: PASS**; addable LOCODE 95 B, HICODE 202 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=tape-pal unbound=yes encrypt=no
```

**tape-pal-unbound-yes-basic: PASS**; addable LOCODE 161 B, HICODE 278 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes realmissiledamage=yes variant=tape-pal unbound=yes encrypt=no
```

**defaults: tape-pal-unbound-yes: PASS**; addable LOCODE 126 B, HICODE 821 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no variant=tape-pal unbound=yes encrypt=no
```

**iffunit=no: tape-pal-unbound-yes: PASS**; addable LOCODE 95 B, HICODE 634 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=no randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=tape-pal unbound=yes encrypt=no
```

### flicker-free

Working directory: `E:/Development/Elite-C64/elite-source-code-commodore-64-flicker-free`

**easyflash-ntsc-unbound-yes: PASS**; addable LOCODE 15 B, HICODE 102 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=easyflash-ntsc unbound=yes encrypt=no
```

**easyflash-pal-unbound-yes: PASS**; addable LOCODE 15 B, HICODE 102 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=easyflash-pal unbound=yes encrypt=no
```

**gma85-ntsc-unbound-yes: PASS**; addable LOCODE 15 B, HICODE 102 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=gma85-ntsc unbound=yes
```

**gma86-pal-unbound-no: PASS**; addable LOCODE 60 B, HICODE 33 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=gma86-pal unbound=no
```

**gma86-pal-unbound-yes: PASS**; addable LOCODE 15 B, HICODE 102 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=gma86-pal unbound=yes
```

**gma86-pal-unbound-yes-basic: PASS**; addable LOCODE 81 B, HICODE 178 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes realmissiledamage=yes variant=gma86-pal unbound=yes
```

**tape-ntsc-unbound-yes: PASS**; addable LOCODE 15 B, HICODE 102 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=tape-ntsc unbound=yes encrypt=no
```

**tape-pal-unbound-no: PASS**; addable LOCODE 60 B, HICODE 33 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=tape-pal unbound=no encrypt=no
```

**tape-pal-unbound-yes: PASS**; addable LOCODE 15 B, HICODE 102 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=tape-pal unbound=yes encrypt=no
```

**tape-pal-unbound-yes-basic: PASS**; addable LOCODE 81 B, HICODE 178 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes realmissiledamage=yes variant=tape-pal unbound=yes encrypt=no
```

**defaults: tape-pal-unbound-yes: PASS**; addable LOCODE 63 B, HICODE 700 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no variant=tape-pal unbound=yes encrypt=no
```

**iffunit=no: tape-pal-unbound-yes: PASS**; addable LOCODE 32 B, HICODE 513 B.

```text
E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free\2-build-files\make.exe BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=no randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes renderspeedups=yes planetdatafix=yes bountyhunterfix=yes variant=tape-pal unbound=yes encrypt=no
```

