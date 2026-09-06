# Elite C64 Commander Save Editor

An English-language .NET 8 console editor and creator for Commodore 64
*Elite* and *Elite: Unbound* commander positions stored in TAP images.

The editor starts with the original JAMESON position from Commodore 64
*Elite*: Cobra Mk III at Lave, 100.0 Cr, 7.0 LY of fuel and three missiles.
It can also load an existing TAP from the menu or directly from the command
line. When a TAP contains more than one commander file, a selection menu is
shown. The format is detected automatically for each selected position.
The format selected under **Commander identity and format** determines how
the next TAP is saved; it is not a filter on loading.

## Run

From this directory:

```powershell
dotnet run --project .\EliteSaveEditor\EliteSaveEditor.csproj
```

To open a TAP immediately:

```powershell
dotnet run --project .\EliteSaveEditor\EliteSaveEditor.csproj -- "C:\path\save.tap"
```

## Standalone Windows executable

Run:

```powershell
.\publish-win-x64.cmd
```

The output is `publish\win-x64\EliteSaveEditor.exe`. It is a self-contained
Windows x64 single-file executable and does not require .NET to be installed
on the destination computer. The publish profile enables full .NET trimming
and single-file compression, so unused managed framework code is removed and
the remaining runtime is bundled into the executable.

The executable can open a TAP directly:

```powershell
.\publish\win-x64\EliteSaveEditor.exe "C:\path\save.tap"
```

The interface uses Up/Down, Page Up/Page Down, Home and End to move through
menus, Enter to select, and Esc to return or cancel. In text and numeric
fields, Ctrl+A clears the current value. Repainted menus retain the current
cursor position after editing or toggling a value, and the main menu retains
the selected section when returning from it.

## Supported data

- commander tape filename (1-7 characters, matching the game's seven-character
  commander-name limit);
- original *Elite* 77-byte and current *Elite: Unbound* 81-byte commander blocks,
  including older 77-byte Unbound saves;
- credits, fuel, galaxy, current system and raw galaxy seed;
- all mission states, kill points with the calculated C64 combat rating, legal
  status and Trumble count;
- cargo, local market availability, the market price-randomization byte and
  current prices calculated with the original C64 market routine;
- lasers, missiles and all installed equipment, including Extra and Naval
  Energy Units;
- all thirteen *Elite: Unbound* player ships;
- *Elite: Unbound* registration ID and Scrambled ID state;
- *Elite: Unbound* Special Cargo destination and remaining reward;
- unused commander bytes and the TAP load address in the Advanced menu.

Competition/version flags are displayed but deliberately read-only. Original
commander checksums and all KERNAL tape XOR checksums are recalculated when a
TAP is saved. In *Elite: Unbound*, bytes that held the original internal
checksums remain registration data and are not overwritten.

Changing the *Elite: Unbound* player ship clears cargo, Trumbles, equipment,
lasers and missiles, as requested by the save format's editing rules. Fuel is
retained but clamped to the new hull's capacity. Cargo and installed-equipment
weight, laser mounts, missile capacity and fuel capacity are validated against
the current source-code tables.

For original *Elite*, the Cobra Mk III holds 20 tonnes, or 35 tonnes with
Large Cargo Bay. These actual capacities are used in the interface and cargo
validation; the corresponding CRGO values stored in TAP files remain 22 and
37. *Elite: Unbound* uses its separate per-ship capacity tables.

## Special Cargo and save formats

The **Special Cargo** main-menu section appears only for **Elite: Unbound**.
Choose **Add delivery...** or **Edit delivery...**, select a planet from the
current saved galaxy, and enter the remaining reward from **0.1 to 6553.5 Cr**
with at most one decimal place. Escape during destination or reward entry
leaves the existing delivery unchanged. **Clear delivery** removes the active
contract. As in the game, clearing retains the previous target coordinates,
which participate in generating later offers.

Editing a delivery does not charge an entry fee, change legal or story-mission
status, or occupy ordinary cargo space. Changing the player ship preserves it.
Changing the galaxy or its raw seed cancels it, matching galactic travel in
the game. Destination names are generated from the saved seed, including
custom seeds edited in Advanced values.

An 81-byte commander is detected as Unbound. For 77-byte files, the original
commander checksums and Unbound registration fields determine the format.
Only an ambiguous or damaged older file requires manual interpretation.
One TAP may contain a mixture of all supported lengths and types.

Older Unbound saves load with an empty four-byte extension and are saved in
the current 81-byte format. Original Elite always saves 77 bytes with its
internal checksums; Unbound saves 81 bytes and preserves registration data.
Converting to Original removes Special Cargo. Converting back to Unbound
starts with an empty delivery, rather than restoring a discarded one.

These lengths describe commander data inside the TAP, not the TAP image's
total size. The Advanced menu displays the actual commander block length.

## Mission presets

- **Mission 1 - Constrictor, Galaxy 1:** places the commander at Xeer in
  Galaxy 1, resets both story-mission state fields, and ensures at least 256
  integer kill points.
- **Mission 1 - Constrictor, Galaxy 2:** places the commander at Errius in
  Galaxy 2, resets both story-mission state fields, and ensures at least 256
  integer kill points.
- **Mission 2 - Thargoid Plans:** marks mission 1 complete, places the
  commander at Ceerdi in Galaxy 3, and ensures at least 1280 integer kill
  points.
- **Mission 3 - Trumbles:** marks both story missions complete, resets the
  Trumble state, and prepares the exact `CASH+2` condition used by the game so
  the offer is the next docking event.

The first three presets leave the mission unstarted and prepare the conditions
that cause its briefing on the next docking. The system coordinates and all
eight galaxy seeds are generated with the original Elite seed algorithm.

## TAP handling

The reader recognizes standard Commodore KERNAL tape headers and validates
ROM byte parity and block XOR checksums. It accepts primary or backup copies
and returns one position for each commander file in the TAP, rather than
mistaking the two KERNAL copies for separate saves.

Saving creates a new TAP containing the current position. The destination is
never silently replaced: an existing file requires an explicit overwrite
answer. The writer emits a C64 TAP v1 image with duplicate KERNAL header and
data blocks using the same conservative ROM pulse lengths as the repository's
game-tape builder.

## Build and test

```powershell
dotnet build .\EliteSaveEditor.sln -c Release
dotnet run --project .\EliteSaveEditor.Tests\EliteSaveEditor.Tests.csproj -c Release
```

The tests cover original cargo limits and TAP encoding, commander checksums,
galaxy mission systems, C64 market prices, mission presets, ship-change
clearing, single- and
multi-position TAP round trips, recovery from a damaged primary tape copy,
and the supplied FLINT TAP when it is available at its original path. Set
`ELITE_TEST_TAP` to test another copy of that file. The supplied two-position
`test-scramble.tap` is also checked when available, including both
independently loaded commander positions. Set `ELITE_TEST_MULTI_TAP` to use
the same test with another path. Special Cargo tests cover format conversion,
automatic detection, legacy migration, exact extension bytes, reward limits,
all eight galaxies and custom seeds, invalid destinations, independent clones,
mixed-length TAPs with identical filenames, and checksum/backup handling of
the final extension byte. Set `ELITE_TEST_OUTPUT` to a scratch directory to
write TAP fixtures for interactive checks.

### Special Cargo update validation (2026-09-06)

The Release solution build and win-x64 publish completed with no warnings or
errors. All 17 tests passed, including the available FLINT and two-position
TAP fixtures. Exact commands, run from this directory:

```powershell
dotnet build EliteSaveEditor.sln -c Release --nologo
dotnet run --project EliteSaveEditor.Tests/EliteSaveEditor.Tests.csproj -c Release --no-build
dotnet publish EliteSaveEditor/EliteSaveEditor.csproj -p:PublishProfile=win-x64 --nologo
```

Interactive console checks exercised automatic loading of Original 77-byte,
legacy Unbound 77-byte and extended Unbound 81-byte saves; the Unbound-only
menu; adding, clearing and cancelling delivery edits; and rejection of
6553.6 Cr while accepting 6553.5 Cr. Three files saved through the normal menu
were re-read and checked: Original remained 77 bytes with valid checksums,
legacy Unbound became 81 bytes with no delivery, and the edited Unbound file
retained Lave / 535.0 Cr and registration ZZ-255.

The standalone executable in `release/EliteSaveEditor.exe` has been refreshed
from the publish output and was also launched for interactive load/save checks.
Scratch fixtures and the saved-file results are under the repository's
`3-assembled-output/save-editor-special-cargo/`.
