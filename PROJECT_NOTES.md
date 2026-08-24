# Elite C64 / Elite: Unbound – projektové poznámky

Stav poznámek: 24. srpna 2026.

Tyto poznámky popisují obě dlouhodobě udržované větve. Údaje o adresách,
velikostech a commitech jsou kontrolní body, ne náhrada za aktuální git log
a nový build.

## Repozitář a pracovní kopie

GitHub:

    https://github.com/ataribaby42/elite-source-code-commodore-64/

| Větev | Windows pracovní kopie | Známý kontrolní bod |
|---|---|---|
| main | E:\Development\Elite-C64\elite-source-code-commodore-64 | 1e4328d |
| flicker-free | E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free | d7cb9f4 |

Obě pracovní kopie používají stejný remote. Větev flicker-free navíc obsahuje
vlastní vykreslovací úpravy, proto se zdrojové soubory mezi větvemi nesmějí
přepisovat jako celek.

Hlavní reference:

- Mark Moxon, dokumentované C64 zdroje:
  https://github.com/markmoxon/elite-source-code-commodore-64/
- Mark Moxon, Elite-A pro BBC Micro:
  https://github.com/markmoxon/elite-a-source-code-bbc-micro/
- Komentovaný web a deep dives:
  https://elite.bbcelite.com/

## Běžná konfigurace projektu

    make variant=tape-pal encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes

Použité volby projektu zahrnují:

- laserbeam=line: jediná laserová čára v ose přídě a ve stylu AI laserů;
- font=zx: font ze ZX Spectrum Elite 48K;
- dials=new: vyčištěný panel bez nápisu ELITE pod scannerem;
- sights=cross: vždy křížový zaměřovač;
- warpjunk=yes: odstranění junk objektů před warpem;
- iffunit=yes: I.F.F. Unit místo Energy Bomb;
- randomspawns=yes: opravené náhodné pozice lodí ve stylu Elite-A;
- whitecockpit=yes: bílé okraje kokpitu, kompas a žlutý kanál scanneru;
- unbound=yes: Elite: Unbound včetně kupovatelných a létatelných lodí.

Tape PAL používá PAL herní variantu GMA86, ale výsledkem je TAP a nepoužívá
diskovou sektorovou tabulku. Tape NTSC analogicky používá NTSC herní variantu
GMA85.

## Elite: Unbound

Rozšíření portuje myšlenku kupovatelných a létatelných lodí z Elite-A.

Známé ovládání:

- Ctrl+3: nákup nové lodi;
- Ctrl+2: prodej vybavení.

Před koupí nové lodi je nutné prodat vybavení, které na ni nelze přenést,
a nadbytečné střely, které se do nové lodi nevejdou. Naval Energy Unit je
z tohoto požadavku vyňata.

Vlastnosti aktuální hráčovy lodi jsou uloženy v tabulkách u návěští, jako jsou:

- ShipMaxSpeed;
- ShipFuelCapacity;
- další tabulky pro roll, pitch, energii, náklad a vybavení.

Původní commander data zůstávají kompatibilní: dříve nepoužívaný bajt s nulou
odpovídá původní Cobře Mk III.

Při unbound=yes se z důvodu místa nezahrnuje 2931bajtová titulní skladba
C.THEME. Vstup startat je nečinný; Blue Danube / C.COMUDAT zůstává. Makefile
zakazuje unbound=yes pro source-disk-build a source-disk-files, protože by
překročily původní limit HICODE.

Verze zobrazená na titulní obrazovce je v:

    1-source-files/main-sources/elite-source.asm

pod návěštím:

    .TitleScreenVersion
     EQUS "v0.10"
     EQUB 0

Při změně délky textu zkontrolovat centrování. Číslo řádku se mezi větvemi
a po úpravách mění.

## Dynamické ukazatele FU a SP

Při unbound=yes se ukazatele:

- SP škáluje podle ShipMaxSpeed aktuální lodi;
- FU škáluje podle ShipFuelCapacity aktuální lodi.

Použitý převod je:

    floor(current_value * 15 / ship_maximum)

Výsledek se omezuje na 15, takže maximum lodi vždy zaplní celý 16bodový
sloupec a přetečená hodnota ze starého save souboru se zobrazí jako plná.

Implementace je v elite-source.asm v rutinách:

- ShipNormalizeSpeedBar;
- ShipNormalizeFuelBar;
- ShipNormalizeBarDial.

Při unbound=no zůstává původní kalibrace C64 Elite. Ukazatele RL a DC už byly
před touto opravou řízené maximy roll/pitch aktuální lodi.

Kontrolní commity:

| Větev | Commit |
|---|---|
| main | 3b2fe7a Scale Unbound speed and fuel dials by ship |
| flicker-free | 6566002 Scale Unbound speed and fuel dials by ship |

## Automatické sektory GMA86 fast loaderu

Dřívější pevný začátek GMA6 byl citlivý na velikost GMA5 a build volby.
V main se podle konfigurace objevovaly sektory $14/$07, $14/$08 a $14/$09;
ve flicker-free $14/$08 a $14/$09.

Pevné podmínky byly nahrazeny dvouprůchodovým diskovým buildem:

1. Makefile vytvoří provizorní GMA86 PAL D64.
2. elite-gma-sectors.py přečte adresář D64.
3. Zjistí skutečné počáteční track/sektor pro GMA2 až GMA6.
4. Opraví tabulku v gma1.unprot.bin.
5. Makefile vytvoří finální D64.
6. Skript ověří tabulku v binárce i uvnitř GMA1 na finálním disku.

Mapování GMA2 používá soubor BYEBYEJULIE. Herní loader aktivně používá
zejména položky GMA3 až GMA6, ale skript bezpečně odvozuje a kontroluje celou
tabulku GMA2 až GMA6.

Důležité soubory:

- Makefile;
- 1-source-files/main-sources/elite-gma1.asm;
- 2-build-files/elite-gma-sectors.py.

Automatika platí pouze pro gma86-pal:

- gma85-ntsc stejnou trackSector tabulku nepoužívá;
- tape-pal a tape-ntsc nemají D64 ani GMA fast loader;
- source-disk varianty mají vlastní způsob sestavení.

Fail-safe chování je záměrné: chybějící soubor, neplatný D64, duplicitní jméno
nebo neshoda tabulky musí ukončit build chybou.

Automatika byla následně úspěšně otestována uživatelem se skutečným Windows
c1541. Varování OPENCBM o chybějící opencbm.dll bylo neškodné, protože práce
s D64 i závěrečné ověření proběhly správně.

Kontrolní commity:

| Větev | Commit |
|---|---|
| main | 1e4328d Calculate GMA86 fast-loader sectors from disk |
| flicker-free | d7cb9f4 Calculate GMA86 fast-loader sectors from disk |

Main commit 21b0ef6 obsahoval přechodné pevné podmínky podle buildu. Současná
automatika v 1e4328d je nahrazuje; tyto podmínky neobnovovat.

## elite-checksum.py

Skript 2-build-files/elite-checksum.py už nemá pevné adresy herních symbolů.
Z aktuálního 3-assembled-output/compile.txt načítá:

- B%;
- G%;
- NA2%;
- W%;
- X%;
- U%.

Z nich dynamicky určuje hranice a offsety pro commander checksum a šifrování
LOCODE, HICODE a COMLOD. Běžná změna velikosti nebo posun herní rutiny proto
nevyžaduje změnu tohoto skriptu.

Pevné zůstávají vlastnosti formátu vydání:

- PRG hlavičky;
- padding;
- skladba ELTA až ELTK;
- pozice instrukcí diskové ochrany v GMA1;
- algoritmus a formát commander checksumu.

Při změně některé z těchto oblastí je nutná nová nezávislá kontrola skriptu.
Při normálním buildu se elite-checksum.py stále spouští automaticky.

Skript byl ověřen pro:

- GMA85 a GMA86;
- source-disk varianty;
- main a flicker-free;
- šifrovaný i nešifrovaný režim;
- oba commander checksumy;
- zpětné rozšifrování LOCODE, HICODE a COMLOD;
- PRG hlavičky a patche diskové ochrany;
- syntaxi přes py_compile.

Kontrolní commity s dynamickými adresami:

| Větev | Commit |
|---|---|
| main | 5627fd0 checksum update |
| flicker-free | d038ece checksum update |

## Volná paměť

Naměřeno pro běžnou konfiguraci projektu uvedenou výše:

| Větev | Konec LOCODE R% | Rozdíl do $4000 | Prakticky přidat | Konec HICODE F% | Rozdíl do $CE00 | Prakticky přidat |
|---|---:|---:|---:|---:|---:|---:|
| main | $3FA0 | 96 B | 95 B | $CC64 | 412 B | 411 B |
| flicker-free | $3FF0 | 16 B | 15 B | $CCC8 | 312 B | 311 B |

Praktická hodnota je o jeden bajt nižší kvůli assemblerovým podmínkám:

    ASSERT R% < $4000
    ASSERT F% < $CE00

LOCODE a HICODE jsou samostatné oblasti a jejich rezervy se nesčítají.
Nejkritičtější je flicker-free LOCODE. Větší nové rutiny umisťovat přednostně
do HICODE a po každé změně znovu odečíst R% a F% z compile.txt.

## Doporučená kontrola po dalších změnách

Pro každou dotčenou větev:

1. ověřit git status a aktivní větev;
2. sestavit běžnou tape-pal konfiguraci;
3. pokud se mění kód nebo jeho velikost, odečíst R% a F% z compile.txt;
4. pokud se mění GMA, loader nebo velikost souborů, sestavit šifrovaný
   gma86-pal a zkontrolovat hlášku o úspěšném ověření sektorové tabulky;
5. podle povahy změny sestavit také tape-ntsc a gma85-ntsc;
6. zkontrolovat unbound=no, pokud změna zasahuje společnou cestu;
7. necommitovat ani nepushovat bez výslovného pokynu;
8. při předání vytvořit ZIP s adresáři main/ a flicker-free/.

