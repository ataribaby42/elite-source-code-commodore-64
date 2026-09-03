# ELITE: Unbound — mission state, targets, and triggers

This document describes the mission logic in the Commodore 64 source and how it is represented in the commander save.

Commander-byte numbers are relative to `DataStart`; add 2 for the physical offset in a C64 PRG file.

## What the save stores

The commander save does **not** contain a separate target-star field. Mission destinations are constants in the program. The save only contains enough state for the game to decide what happens next:

| Commander byte | Variable | Meaning used by missions |
|---:|---|---|
| `#00` / `$00` | `TP` | Bitmapped state of all three missions |
| `#01` / `$01` | `QQ0` | Current system's galactic X coordinate |
| `#02` / `$02` | `QQ1` | Current system's galactic Y coordinate |
| `#03`–`#08` / `$03`–`$08` | `QQ21` | Current galaxy seed |
| `#09`–`#12` / `$09`–`$0C` | `CASH` | Cash, 32-bit big-endian, in tenths of a credit |
| `#15` / `$0F` | `GCNT` | Galaxy number, zero-based (`0`–`7`) |
| `#43` / `$2B` | `ENGY` | Energy-unit type; mission 2 can set Naval Energy Unit (`2`) |
| `#48`–`#49` / `$30`–`$31` | `TRIBBLE` | Number of Trumbles, 16-bit little-endian |
| `#71`–`#72` / `$47`–`$48` | `TALLY` | Integer combat kill-point tally, 16-bit little-endian |

The system names are generated from the galaxy data. Mission code recognizes its destinations by `GCNT`, `QQ0`, and `QQ1`, not by a stored name, system number, or mission target.

## Mission byte `TP`

`TP` is commander byte `#00` (`$00`).

| Bits | Mission | Value | Meaning |
|---|---|---:|---|
| 0–1 | Constrictor | `%00` | Not started |
| 0–1 | Constrictor | `%01` | In progress; hunting the Constrictor |
| 0–1 | Constrictor | `%11` | Constrictor destroyed; debrief still pending |
| 0–1 | Constrictor | `%10` | Mission and debrief complete |
| 2–3 | Thargoid Plans | `%00` | Not started |
| 2–3 | Thargoid Plans | `%01` | Started; plans not yet collected |
| 2–3 | Thargoid Plans | `%10` | Plans collected and being transported |
| 2–3 | Thargoid Plans | `%11` | Mission complete |
| 4 | Trumbles | `0` | Offer has not been answered |
| 4 | Trumbles | `1` | Offer was accepted or declined |
| 5–7 | — | — | Unused |

The normal progression of the low nibble is:

| `TP & $0F` | State |
|---:|---|
| `$00` | No story mission started |
| `$01` | Constrictor hunt active |
| `$03` | Constrictor killed, awaiting debrief |
| `$02` | Constrictor mission complete |
| `$06` | Plans mission started; travel to Ceerdi |
| `$0A` | Plans collected; travel to Birera |
| `$0E` | Both story missions complete |

Bit 4 is independent, so `$10` may be added to any of these values after the Trumble offer has been answered.

## When mission checks run

Mission progression is checked by `DOENTRY` when the player docks at a station. The order matters:

1. Start or debrief the Constrictor mission.
2. Start the Thargoid Plans mission, collect the plans, or deliver them.
3. If no preceding event took over, consider the Trumble offer.
4. Otherwise display the normal docking-bay/status screen.

Only one briefing or debriefing is therefore processed during a single docking event.

## Mission 1: Constrictor

### Start trigger

The mission starts on docking when all of these conditions are true:

- `(TP & $03) == $00`: mission 1 has not started.
- `TALLY+1 != 0`: integer kill-point tally is at least `$0100` (256).
- `GCNT` is `0` or `1`: the player is in in-game Galaxy 1 or Galaxy 2.

`BRIEF` then sets bit 0 of `TP`, changing the mission-1 state to `%01`.

The opening clue depends on the galaxy where the mission starts: in Galaxy 1 the briefing says the ship was last seen at Reesdice; in Galaxy 2 it says the Constrictor is believed to have jumped to that galaxy.

#### Starting the mission in Galaxy 2

If the commander is already in in-game Galaxy 2 when the eligibility conditions are met, the mission is still announced automatically. On the next docking, `DOENTRY` calls `BRIEF`, which sets bit 0 of `TP` before displaying an `INCOMING MESSAGE`, the rotating Constrictor model, and the full Navy briefing.

The briefing's location hint is selected according to `GCNT`:

- in Galaxy 1 it uses extended token 220: `WAS LAST SEEN AT REESDICE`;
- in Galaxy 2 it uses extended token 221: `IS BELIEVED TO HAVE JUMPED TO THIS GALAXY`.

The Galaxy 2 briefing therefore tells the player that the stolen Constrictor must be found and destroyed somewhere in the current galaxy, but it does not name Errius or another first destination. The intended discovery route is to encounter one of the Galaxy 2 rumour systems described below, whose mission description points to Errius, and then follow the main trail:

`rumour system -> Errius -> Inbibe -> Ausar -> Usleri -> Orarra`

Despite the briefing saying `SHOULD YOU DECIDE TO ACCEPT IT`, there is no yes/no prompt. The mission has already been activated in `TP` and cannot be declined.

### Clue systems

There is no saved clue index or route position. While mission 1 is active, static `RUPLA`, `RUGAL`, and `RUTOK` tables replace the normal descriptions of specific systems with mission text. The descriptions form a logical trail, but the game does not enforce its order and reading a clue does not modify `TP`.

The lookup is limited to the 26 entries by `NRU% = 26`, fixing the original zero-counter overflow. Table addresses are exported from the current data build by `elite-token-layout.py`, because the Unbound text changes move the tables away from their original fixed offsets. This prevents invalid description lookups without changing mission state or trigger conditions.

`PDESC` shows a mission description only when all of these conditions are true:

- the player is docked;
- the system selected on the Data on System screen is the current system, not a remote system on the chart;
- bit 0 of `TP` is set, so mission 1 is in progress; and
- the current system number and `GCNT` match an entry in `RUPLA` and `RUGAL`.

#### Galaxy 1 trail

| System | Mission description |
|---|---|
| Xeer | Reports that the Constrictor was last seen at Reesdice. This is an alternative pointer to the same destination given in the Galaxy 1 briefing. |
| Reesdice | Reports that the ship left for Arexe. |
| Arexe | Reports that the ship had a galactic hyperdrive fitted and used it, indicating that the trail continues in another galaxy. |

#### Galaxy 2 main trail

| System | Mission description points to |
|---|---|
| Errius | Inbibe |
| Inbibe | Ausar |
| Ausar | Usleri |
| Usleri | Orarra |
| Orarra | The target system; its description warns that a real pirate is out there. |

The main Galaxy 2 route is therefore:

`Errius -> Inbibe -> Ausar -> Usleri -> Orarra`

#### Galaxy 2 rumour systems

The following systems are not additional sequential steps:

- Bebege, Cearso, Dicela, Eringe, Gexein, Isarin, Letibema;
- Maisso, Onen, Ramaza, Sosole, Tivere, and Veriar.

Their `RUTOK` entries 10-22 all contain `ERND 25`, which randomly selects one of extended tokens 106-110. The wording varies, but every version directs the player to Errius, for example `TRY ERRIUS` or `GET YOUR IRON ASS OVER TO ERRIUS`. These systems act as multiple entry points into the main Galaxy 2 trail for a player searching for information.

#### Galaxy 3 warning

Xeveon's mission description says `BOY ARE YOU IN THE WRONG GALAXY!`, warning a player who has followed the trail too far.

Because no clue progression is saved or checked, the player may ignore the whole trail and fly directly to Orarra. The Constrictor spawn depends only on the mission bits and the current location described below.

### Target and spawn trigger

The Constrictor target is hard-coded:

| Target | `GCNT` | In-game galaxy | Coordinates | System number |
|---|---:|---:|---:|---:|
| Orarra | `1` | 2 | `(144, 33)` | 193 |

`THERE` checks `GCNT == 1`, `QQ0 == 144`, and `QQ1 == 33`. During the extra-vessel spawn pass, the Constrictor is spawned if:

- the player is at Orarra;
- mission-1 bit 0 is set;
- mission-1 bit 1 is clear, so it has not been destroyed; and
- no Constrictor is already present in the local ship bubble (`MANY+CON == 0`).

The target is therefore reconstructed entirely from constants and the saved current location.

### Kill and completion triggers

When `KILLSHP` removes a ship of type `CON`:

- bit 1 of `TP` is set, changing mission 1 from `%01` to `%11`;
- `TALLY+1` is incremented, awarding 256 kill points.

On the next docking, `DOENTRY` sees `(TP & $03) == $03` and calls `DEBRIEF`:

- bit 0 is cleared, leaving mission-1 state `%10`;
- 5,000.0 Cr is added to `CASH`.

## Mission 2: Thargoid Plans

### Start trigger

The mission starts on docking when all of these conditions are true:

- `(TP & $0F) == $02`: the Constrictor mission is complete and debriefed, while mission 2 has not started.
- `GCNT == 2`: the player is in in-game Galaxy 3.
- `TALLY+1 >= 5`: integer kill-point tally is at least `$0500` (1280).

`BRIEF2` sets bit 2, producing `(TP & $0F) == $06`, and tells the player to travel to Ceerdi.

### Collecting the plans

The plans are collected on docking when:

- `(TP & $0F) == $06`;
- `GCNT == 2` (Galaxy 3); and
- the current coordinates are `(QQ0, QQ1) == (215, 84)`, which is Ceerdi.

`BRIEF3` changes the low nibble to `$0A`, meaning the plans are now being carried to Birera.

### Thargoid attacks while carrying the plans

While `(TP & $0C) == $08`, the extra-vessel spawn code makes an additional random check whenever its spawn delay expires. A random byte of 200–255 spawns a Thargoid and a Thargon companion:

- exact probability: `56 / 256`;
- percentage: `21.875%`, rounded to 22% in the source comments.

This is a chance per eligible extra-vessel spawn check, not per frame and not a saved timer.

### Delivery and completion trigger

The mission is completed on docking when:

- `(TP & $0F) == $0A`;
- `GCNT == 2` (Galaxy 3); and
- the current coordinates are `(QQ0, QQ1) == (63, 72)`, which is Birera.

`DEBRIEF2` then:

- sets bit 2, producing `(TP & $0F) == $0E`;
- sets `ENGY = 2`, installing the Naval Energy Unit;
- increments `TALLY+1`, awarding 256 kill points.

### Mission-2 destinations

| Stage | Destination | `GCNT` | In-game galaxy | Coordinates | Stored as target? |
|---|---|---:|---:|---:|---|
| Collect plans | Ceerdi | `2` | 3 | `(215, 84)` | No; hard-coded in `DOENTRY` |
| Deliver plans | Birera | `2` | 3 | `(63, 72)` | No; hard-coded in `DOENTRY` |

## Mission 3: Trumbles

### Offer trigger

The offer is considered on docking, after the story-mission checks, when:

- `(TP & $10) == 0`: the offer has never been answered; and
- the source-code test `CASH+2 >= $C4` succeeds.

The cash test is deliberately described as the exact byte comparison because it is **not** a full 32-bit `CASH >= value` comparison. `CASH` is big-endian and measured in tenths of a credit. When the two most significant bytes are zero, the first passing value is `$0000C400`, or 5,017.6 Cr. Larger balances do not necessarily pass because only `CASH+2` is tested. Nearby source comments give conflicting prose thresholds; the machine instruction above is authoritative.

### Accepting or declining

`TBRIEF` sets bit 4 of `TP` **before** asking the yes/no question. Therefore:

- accepting subtracts 50,000 internal cash units, i.e. 5,000.0 Cr, and increments `TRIBBLE` from 0 to 1 so breeding starts;
- declining changes no cash or Trumble count;
- either answer leaves bit 4 set, so the offer is never made again.

There is no Trumble target system and no separate accepted/completed mission state in `TP`. The infestation itself is represented by the saved `TRIBBLE` count.

## Compact docking logic

```text
on docking:
    m1 = TP & $03

    if m1 == $00 and TALLY >= $0100 and GCNT < 2:
        start Constrictor mission
    else if m1 == $03:
        debrief Constrictor mission
    else if GCNT == 2:
        state = TP & $0F
        if state == $02 and TALLY >= $0500:
            start Plans mission
        else if state == $06 and location == (215, 84):
            collect plans at Ceerdi
        else if state == $0A and location == (63, 72):
            deliver plans at Birera

    if no mission event occurred and CASH+2 >= $C4 and (TP & $10) == 0:
        offer Trumbles
```

## Relevant source labels

| Area | Labels or tables |
|---|---|
| Docking trigger order | `DOENTRY`, `EN1`–`EN6` |
| Constrictor | `BRIEF`, `THERE`, `KILLSHP`, `DEBRIEF` |
| Constrictor clues | `PDESC`, `RUPLA`, `RUGAL`, `RUTOK` |
| Thargoid Plans | `BRIEF2`, `BRIEF3`, `DEBRIEF2`, extra-vessel spawning code |
| Trumbles | `TBRIEF`, `LCASH` |

The save-byte layout is documented separately in [`elite-unbound-save-map.src`](elite-unbound-save-map.src).
