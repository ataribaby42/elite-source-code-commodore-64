# Elite C64 Commander Save Editor

An English-language .NET 8 console editor and creator for Commodore 64
*Elite* and *Elite: Unbound* commander positions stored in TAP images.

The editor starts with the original JAMESON position from Commodore 64
*Elite*: Cobra Mk III at Lave, 100.0 Cr, 7.0 LY of fuel and three missiles.
It can also load an existing TAP from the menu or directly from the command
line. When a TAP contains more than one commander file, a selection menu is
shown.

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
- original *Elite* and *Elite: Unbound* 77-byte commander blocks;
- credits, fuel, galaxy, current system and raw galaxy seed;
- all mission states, kill points with the calculated C64 combat rating, legal
  status and Trumble count;
- cargo, local market availability, the market price-randomization byte and
  current prices calculated with the original C64 market routine;
- lasers, missiles and all installed equipment, including Extra and Naval
  Energy Units;
- all thirteen *Elite: Unbound* player ships;
- *Elite: Unbound* registration ID and Scrambled ID state;
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

The tests cover original commander checksums, galaxy mission systems, C64
market prices, mission presets, ship-change clearing, single- and
multi-position TAP round trips, recovery from a damaged primary tape copy,
and the supplied FLINT TAP when it is available at its original path. Set
`ELITE_TEST_TAP` to test another copy of that file. The supplied two-position
`test-scramble.tap` is also checked when available, including both
independently loaded commander positions. Set `ELITE_TEST_MULTI_TAP` to use
the same test with another path.
