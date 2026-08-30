# Elite C64 / Elite: Unbound – projektové poznámky

Stav poznámek: 29. srpna 2026.

Tyto poznámky popisují obě dlouhodobě udržované větve. Údaje o adresách,
velikostech a commitech jsou kontrolní body, ne náhrada za aktuální git log
a nový build.

## Repozitář a pracovní kopie

GitHub:

    https://github.com/ataribaby42/elite-source-code-commodore-64/

| Větev | Windows pracovní kopie | Známý kontrolní bod |
|---|---|---|
| main | E:\Development\Elite-C64\elite-source-code-commodore-64 | a94b04a |
| flicker-free | E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free | e23a2a7 |

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

    make variant=tape-pal encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes realmissiledamage=yes fpslimiter=yes inputfix=yes renderspeedups=yes

Použité volby projektu zahrnují:

- laserbeam=line: jediná laserová čára v ose přídě a ve stylu AI laserů;
- font=zx: font ze ZX Spectrum Elite 48K;
- dials=new: vyčištěný panel bez nápisu ELITE pod scannerem;
- sights=cross: vždy křížový zaměřovač;
- warpjunk=yes: odstranění junk objektů před warpem;
- iffunit=yes: I.F.F. Unit místo Energy Bomb;
- randomspawns=yes: opravené náhodné pozice lodí ve stylu Elite-A;
- whitecockpit=yes: bílé okraje kokpitu, kompas a žlutý kanál scanneru;
- unbound=yes: Elite: Unbound včetně kupovatelných a létatelných lodí;
- realmissiledamage=yes: rakety ubírají AI lodím skutečnou energii;
- fpslimiter=yes: limiter herní logiky odvozený z Elite 128;
- inputfix=yes: současné používání klávesnice a joysticku jako v Elite 128;
- renderspeedups=yes: předvýpočet a zrcadlení bodů kružnic.

Volba scannercolorfix=yes opravuje původní paletu jediné buňky skeneru vlevo
od kompasu z $67 na $27, takže se červený blip nezobrazuje s modrým čtvercem.
Je aktivní pouze při whitecockpit=no. Při whitecockpit=yes zůstává vždy použita
stávající opravená červeno-bílá paleta $21 bez ohledu na scannercolorfix.
Volba mění jediný datový bajt a nezabírá žádnou další LOCODE ani HICODE paměť.

Pokud má hráč alespoň jeden Trumble, nákup nové lodi je vždy odmítnut zprávou
`CARGO?`. Hráč se musí Trumbles nejprve zbavit; do nové lodi se nepřenášejí.
Kontrola zabírá 8 bajtů HICODE a žádný LOCODE.

Při unbound=yes používá společný slot Energy Bomb / I.F.F. Unit ceny
vynásobené 6,25 a zaokrouhlené na celé kredity: 2500 Cr pro Cobra Mk III,
1250 Cr pro Adder/Gecko/Cobra Mk I, 2500 Cr pro Fer-de-Lance/Asp Mk II,
1875 Cr pro Python/Boa/Anaconda a 938 Cr pro
Moray/Sidewinder/Krait/Mamba. Přepínač iffunit mění název a funkci položky,
nikoli její cenu. Hodnoty při unbound=no zůstávají nezměněné.

Ve flicker-free kombinaci unbound=yes, iffunit=no je původní 20bajtová
obsluha klávesy Energy Bomb přesunuta z LOCODE do 21bajtové HICODE rutiny.
V LOCODE zůstává pouze tříbajtové JSR, což šetří 17 bajtů. Přesun je pod
oběma podmínkami, takže ostatní tři kombinace unbound/iffunit zůstávají
binárně shodné. Opravená kombinace končí na R%=$3FF3 a F%=$CCAF, takže lze
prakticky přidat dalších 12 bajtů LOCODE a 336 bajtů HICODE.

Historická úplná matice osmi nezávislých yes/no voleb (warpjunk, iffunit,
unbound, fpslimiter, inputfix, randomspawns, whitecockpit a scannercolorfix)
obsahuje 256 kombinací na větev. Všech 512 sestavení prošlo. Nejvyšší jednotlivě
naměřené konce byly main R%=$3FB4 a F%=$CD8F, flicker-free R%=$3FF3 a
F%=$CD77; maxima R% a F% nemusejí pocházet ze stejné kombinace. Tato dřívější
matice ještě nezahrnovala novější volbu renderspeedups.

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
     EQUS "v0.30"
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
| main | 857d254 v0.20 dynamic GMA D64 sectors computation and FU and SP dial bars calibration for all ships from Unbound expansion |
| flicker-free | 61d2bde v0.20 dynamic GMA D64 sectors computation and FU and SP dial bars calibration for all ships from Unbound expansion |

## Frame limiter z Elite 128

Limiter je v main i flicker-free řízen samostatnou volbou fpslimiter=yes,
nezávisle na unbound. Výchozí hodnota je no, takže build bez nové volby
zachovává původní neomezené časování. Raster IRQ zvýší FrameCounter jednou za
kompletní videosnímek a rutina FrameLimit čeká, dokud neuplynou čtyři snímky.
Potom čítač vynuluje.

Limiter se volá:

- na vstupu NOMVETR do hlavního letového cyklu;
- v TLL2 po vykreslení rotující lodi na titulní obrazovce.

Konstanta FRAME_LIMIT je nastavena na 4, což omezuje herní logiku na 12,5 FPS
u PAL a 15 FPS u NTSC. Na pomalém stock C64 čekání nastane jen tehdy, pokud by
herní cyklus skončil dříve než za čtyři snímky. Volba pouze rozhoduje při
sestavení; přepínač za běhu hry se nepřidával. Kód je uzavřen v
IF _FPS_LIMITER a funguje i při unbound=no.

Flicker-free implementace byla ověřena v emulátoru: diagnostická hodnota 25
vytvořila očekávané velmi nízké stabilní FPS a finální hodnota 4 stabilizovala
FPS v jednoduchých scénách.

Kontrolní buildy:

- tape-pal a tape-ntsc s fpslimiter=yes prošly včetně TAP round-trip ověření
  v obou větvích;
- fpslimiter=no inputfix=no má binárně shodné LOCODE, HICODE a COMLOD jako
  původní build;
- fpslimiter=yes inputfix=no má binárně shodné binárky jako dřívější
  implementace limiteru svázaná s unbound=yes;
- šifrovaný gma86-pal prošel assemblerem, checksumem a šifrováním, ale tvorbu
  D64 nebylo možné dokončit v kontrolním prostředí bez c1541.

Kontrolní commity původní implementace limiteru:

| Větev | Commit |
|---|---|
| main | 88c6e66 Added frame limiter FRAME_LIMIT = 4: 12,5 FPS PAL / 15 FPS NTSC |
| flicker-free | b02bbe9 Added frame limiter FRAME_LIMIT = 4: 12,5 FPS PAL / 15 FPS NTSC |

## Paralelní klávesnice a joystick z Elite 128

Volba inputfix=yes portuje změnu obsluhy vstupu z binárky Elite 128 1.0.
Původní C64 rutina RDKEY při zvoleném joysticku po jeho načtení přeskočí celý
sken klávesnice. Elite 128 po zpracování směrů, fire a reverzace os pokračuje
na scanmatrix, takže hodnoty joysticku v KY3 až KY7 zůstanou nastavené a ve
stejném průchodu se doplní klávesy z celé matice.

V současném dokumentovaném zdroji je stejné chování implementováno podmíněným
JMP scanmatrix za návěštím noswapxs. Není nutné přesouvat celou joystickovou
část jako v binárce Elite 128. Změna zabírá 3 bajty HICODE a žádný LOCODE.

inputfix=yes nemění volbu režimu JSTK: joystick musí být stále vybrán obvyklým
způsobem. Jakmile je vybrán, lze současně řídit joystickem a používat klávesy
pro rychlost, střely, ECM, mapy a další příkazy. Volba je nezávislá na unbound,
výchozí hodnota je no a při inputfix=no zůstávají původní binárky beze změny.

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
| main | 857d254 v0.20 dynamic GMA D64 sectors computation and FU and SP dial bars calibration for all ships from Unbound expansion |
| flicker-free | 61d2bde v0.20 dynamic GMA D64 sectors computation and FU and SP dial bars calibration for all ships from Unbound expansion |

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

## Vyvážení štítů hráčských lodí

Pouze při `unbound=yes` se hodnoty Elite-A `new_shields` nově chápou jako
relativní síla štítu, nikoli jako pevná absorpce odečtená od každého zásahu.
Cobra Mk III s hodnotou 7 je reference 100 % a proto dostává přesně stejné
poškození jako původní C64 Elite:

    scaledDamage = (incomingDamage * 7 + shieldStrength / 2) / shieldStrength

Výpočet používá celočíselné dělení se zaokrouhlením na nejbližší hodnotu.
Zbytek se mezi zásahy neukládá, takže není potřeba žádný nový trvalý bajt.
Hodnoty nad 255 se ve stejném zásahu posílají do `OOPS` po částech; menší část
se zpracuje první a poškození alespoň 510 se zpracuje jako dvě části po 255.

Tabulka `ShipShieldAbsorption` byla přejmenována na `ShipShieldStrength` a
helper `PlayerShieldAbsorption` na `PlayerShieldStrength`.

Kontrolní výsledky proti policejnímu Viperu (vstupní poškození 8, plný štít a
energie, bez regenerace):

| Loď | Síla | Poškození | Smrt při zásahu |
|---|---:|---:|---:|
| Cobra Mk III | 7 | 8 | 64 |
| Adder | 4 | 14 | 37 |
| Gecko | 5 | 11 | 47 |
| Moray | 6 | 9 | 57 |
| Cobra Mk I | 5 | 11 | 47 |
| Fer-de-Lance | 8 | 7 | 73 |
| Python | 11 | 5 | 102 |
| Boa | 10 | 6 | 85 |
| Anaconda | 13 | 4 | 128 |
| Asp Mk II | 10 | 6 | 85 |
| Sidewinder | 2 | 28 | 19 |
| Krait | 3 | 19 | 27 |
| Mamba | 4 | 14 | 37 |

Změna je celá v HICODE. Pro `unbound=no` zůstávají výsledné bloky LOCODE,
HICODE a COMLOD binárně shodné s verzí před změnou.

## Reálné poškození AI lodí raketami

Volba `realmissiledamage=yes` je nezávislá na `unbound`. Ve výchozím stavu je
vypnutá a rakety ničí AI lodě okamžitě jako dosud. Po zapnutí každý zásah
odečte 81 z bajtu #35, tedy z aktuální energie cílové AI lodě.

Pokud měla loď před zásahem alespoň 81 energie, uloží se zbytek a loď přežije;
to zahrnuje i přesný výsledek nula. Pokud měla méně než 81, použije se původní
logika označení lodi ke zničení. Cobra Mk III se 150 body proto bez regenerace
přežije první raketu se 69 body a druhá ji zničí. Regenerace energie AI může v
reálné hře potřebný počet raket zvýšit.

Stanice zůstávají imunní, zasažená raketa se vždy zničí a původní účinek její
exploze na blízkého hráče zůstává beze změny. Implementace nepřidává žádný
stavový bajt ani tabulku. Vlastní 30bajtová rutina `AIRealMissileDamage` je v
HICODE; v LOCODE nahrazuje původní 13bajtový blok tříbajtový skok, takže LOCODE
se při zapnuté volbě zmenší o 10 bajtů. Při `realmissiledamage=no` jsou LOCODE,
HICODE a COMLOD bitově shodné se stavem před změnou.

## Přesun rutiny DIALS do HICODE

Při `unbound=yes` se 205bajtová rutina `DIALS` již nesestavuje v LOCODE, ale
na konci HICODE. Společné tělo rutiny je definováno makrem `ASSEMBLE_DIALS`,
takže obě umístění používají jediný zdroj. V HICODE má rutina 206 bajtů:
původní relativní návrat přes `dec27` v LOCODE nahrazuje lokální `RTS`.

Při `unbound=no` zůstává `DIALS` na původním místě v LOCODE. Kontrolní build
potvrdil bitovou shodu bloků LOCODE, HICODE i COMLOD se stavem před přesunem.

## PackBits komprese dashboardu

Při `unbound=yes` loader podle volby `dials` načte soubor
`C.CODIALS.RLE.bin` nebo `C.CODIALSNEW.RLE.bin` a ponechá jej komprimovaný na
adrese `DSTORE%`. HICODE rutina `UnpackDials` jej rozbalí přímo do obrazovkové
paměti až při skutečném zobrazení dashboardu; nevytváří trvalou rozbalenou
kopii v `DSTORE%`.

Použitý formát je standardní PackBits. Původní soubory mají po 2248 bajtech,
komprimovaný starý dashboard 1228 bajtů a nový dashboard 1199 bajtů.
Konzervativně je tedy možné počítat s uvolněním nejméně 1020 bajtů paměti
(2248 - 1228). Skript `2-build-files/elite-packbits.py` vytváří oba soubory a
build v režimu `--check` ověřuje jejich přesný obsah i zpětné rozbalení. Při
`unbound=no` zůstává původní nekomprimovaná cesta beze změny.

## Rychlejší vykreslování kružnic

Volba `renderspeedups=yes` je nezávislá na `unbound` a ve výchozím stavu je
vypnutá. Rutina `BuildCircleSinCache` před každým voláním `CIRCLE2` vypočítá
jen jedinečné součiny `K * sin(angle)` pro úhly 0 až 16 a díky přesné symetrii
tabulky `SNE` je zrcadlí do vlastní 32bajtové cache. Smyčka `CIRCLE2` dál
prochází původní hodnoty `CNT` ve stejném pořadí a volá původní `BLINE`, takže
se nemění pořadí bodů, clipping ani ball-line heap. Lookup obnovuje i carry
vracené rutinou `FMLTU2`, které původní kód používá při změně znaménka.

Počet volání `FMLTU2` na jednu kružnici se mění takto:

| Krok STP | Původně | S cache |
|---:|---:|---:|
| 8 | 18 | 3 |
| 4 | 34 | 5 |
| 2 | 66 | 9 |

Optimalizace se uplatní na planetě a také na dalších uživatelích společné
rutiny `CIRCLE2`, tedy na mapových kružnicích a tunelu při startu či
hyperspace. Zabírá 66 bajtů LOCODE pro cache a její naplnění a 13 bajtů
HICODE pro lookupy. Při `renderspeedups=no` zůstaly LOCODE, HICODE i COMLOD
bitově shodné s předchozím stavem.

Kontrolní PAL tape build s `renderspeedups=yes unbound=no` prošel v obou
větvích. Naměřené konce byly main R%=$3EE0, F%=$CDAD a flicker-free R%=$3FAB,
F%=$CD95. Optimalizace se tedy vejde i s původní hudbou a DIALS v LOCODE.
Stejná konfigurace prošla i jako šifrovaný `variant=gma86-pal`; dvouprůchodová
tvorba D64 v obou větvích úspěšně ověřila fast-loader sektorovou tabulku.

## Bezpečný koridor pro vypouštění lodí ze stanice

Při `unbound=yes` stanice před vypuštěním Shuttle nebo Transporteru kontroluje
prostor před dokovacím otvorem. Policejní Viper (`COPS`) má výjimku a může být
vypuštěn vždy.

Kontrolovaný prostor je válec vedený od stanice směrem k planetě. Používá
poloměr 200 jednotek a délku 2240 jednotek, tedy sedm průměrů Coriolisu.
Válec začíná na dokovací stěně 160 jednotek od středu stanice a končí ve
vzdálenosti 2400 jednotek od jejího středu.

Hráč se kontroluje samostatně, protože nemá vlastní slot v tabulce `FRIN`.
Potom se projdou mobilní sloty 2 až 9. Start blokují skutečné lodě od Shuttle
výše, zatímco rakety, kontejnery, únikové moduly, kameny, asteroidy, splintery
a rock hermit se ignorují. Testuje se poloha středu lodě. Zablokovaný náhodný
pokus o start se zahodí; stanice nemá frontu a další pokus proběhne standardní
náhodnou cestou.

Každý zablokovaný pokus o start zobrazí ve stavovém řádku dynamickou zprávu ve
formátu `C1-007:AB-123, DOCK OR LEAVE`. První registrace patří aktuální
stanici a druhá přesně tomu hráči nebo AI slotu, který test koridoru našel.
Pirátská AI loď a AI loď se stejným registračním číslem jako hráč se zobrazí
jako `??-???`. Zpráva používá standardní mechanismus `MESS`, stejné centrování
a EOR mazání jako ostatní zprávy, ale privátní token 2 má `DLY=100`, tedy
pětinásobnou dobu proti běžné hodnotě 20.

V LOCODE je pouze krátká kontrola před voláním `SFS1`; výpočet válce je v
HICODE. Předchozí varianta s poloměrem 160 prošla běžným PAL tape buildem v
obou větvích. Po zvětšení poloměru na 200 a přidání stavové zprávy nebyl podle
výslovného požadavku proveden nový build ani test; paměťové hodnoty níže jsou
poslední naměřené hodnoty před touto změnou.

## Registrace hráčovy lodě

Při `unbound=yes` jsou bývalé checksumové bajty commander save `#74-#76`
přejmenované na `regplate_1`, `regplate_2` a `regplate_3`. Ukládají dvě ASCII
písmena `A-Z` a číslo `1-255`; velikost 77bajtového commander bloku se nemění.
Výchozí commander Jameson má registraci `JS-042`.

Výchozí blok posledního commandera obsahuje stejnou registraci a rutina
`DFAULT` při `unbound=yes` kopíruje všech 77 bajtů včetně čísla v bajtu #76.
Tím zůstává `JS-042` zachováno při prvním spuštění i po volbě Default JAMESON.

Po načtení se obě písmena a nenulové číslo validují. Původní save, jehož
checksumové hodnoty validaci nesplní, dostane novou náhodnou registraci. Nová
registrace se generuje také po každém úspěšném nákupu jiné lodi a po použití
Escape Podu. Status Mode ji zobrazuje za názvem hráčovy lodě. Jeho titulek se
centruje pouze podle `COMMANDER` a aktuální délky jména; při necelém středu se
volí levější sloupec. Checksumový postprocesor při `unbound=yes` poslední tři
bajty nepřepisuje; při `unbound=no` zachovává původní checksumové chování.

Registrace zaměřené AI lodě se maskuje jako `??-???` nejen u pirátů, ale také
tehdy, když se její číselná část shoduje s registračním číslem hráčovy lodě.

## Scramble Ship Registration

Při `unbound=yes` používá dřívější save-count bajt `#73` příznak
`regplate_scrambled`: `0` znamená viditelnou registraci a `$FF` skrytou.
Výchozí commander i Default JAMESON mají hodnotu 0. Při načtení se přijímají
jen hodnoty 0 a `$FF`; libovolná jiná hodnota se normalizuje na 0.

V Anarchy systému je na konci obrazovky Equip Ship položka
`Scramble Ship ID` za `200.0 Cr` na stejném řádku. Po zakoupení se bajt #73 nastaví
na `$FF`, položka z nabídky zmizí a hráčova registrační rutina i zpráva o blokovaném startu stanice
zobrazí `??-???`. Nová loď nebo Escape Pod vygenerují novou registraci a
současně příznak vrátí na 0.

Pokud hráč se skrytou registrací vstoupí do bezpečné zóny stanice v systému,
jehož vláda není Anarchy, Feudal ani Dictatorship, zvýší se `FIST` nejméně na
100. Kontrola probíhá po každém letovém snímku bezprostředně po aktualizaci
`SSPR`, takže se stav změní hned při vstupu do zóny. Vyšší hodnota se nesnižuje
a uvedené tři vlády příznak ignorují.

Při aktivním scramble a `FIST >= 50` mohou lidské pirátské lodě vzniknout bez
hostile bitu. Testovací hodnota `NEUTRAL_PIRATE_CHANCE = 128` znamená šanci
128/256, tedy 50 %. Pirát si ponechá pirate bit, AI i původní agresivitu, dostane
počáteční rychlost 16 až 31 a letí rovně aktuálním kurzem. Při
`randomspawns=yes` dostane každý pirát ve skupině vlastní pozici, takže pasivní
lodě nevznikají přes sebe. Před každým `NWSHP` se rychlost pracovního bloku
vynuluje, aby ji další člen skupiny nezdědil po předchozí pasivní lodi. Po zásahu
hráčem rutina `ANGRY` hostile bit obnoví a loď se začne normálně bránit. Bez
scramble nebo při `FIST < 50` zůstává volba hostility beze změny. Thargoidi a
mise jsou z této změny vyloučeni.

Zkušební PAL tape build s běžnou konfigurací včetně `unbound=yes`,
`iffunit=yes` a `renderspeedups=yes` prošel v obou větvích včetně TAP
round-trip ověření.

## Volná paměť

Naměřeno pro běžnou konfiguraci projektu uvedenou výše:

| Větev | Konec LOCODE R% | Rozdíl do $4000 | Prakticky přidat | Konec HICODE F% | Rozdíl do $CE00 | Prakticky přidat |
|---|---:|---:|---:|---:|---:|---:|
| main | $3F40 | 192 B | 191 B | $CB6E | 658 B | 657 B |
| flicker-free | $3F90 | 112 B | 111 B | $CBD2 | 558 B | 557 B |

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
6. zkontrolovat unbound=no a také fpslimiter=no inputfix=no, pokud změna
   zasahuje společnou cestu;
7. necommitovat ani nepushovat bez výslovného pokynu;
8. při předání vytvořit ZIP s adresáři main/ a flicker-free/.
