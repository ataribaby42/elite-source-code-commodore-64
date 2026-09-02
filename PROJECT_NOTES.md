# Elite C64 / Elite: Unbound – projektové poznámky

Stav poznámek: 2. září 2026.

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
Pirátská AI loď a AI loď s celou registrací shodnou s hráčem se zobrazí jako
`??-???`; samotná shoda číselné části registraci neskrývá. Zpráva používá
standardní mechanismus `MESS`, stejné centrování
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

Registrace zaměřené AI lodě se maskuje jako `??-???` u pirátů a při shodě celé
registrace AI lodě s hráčovou. Samotná shoda číselné části identitu neskrývá.

## Scramble Ship Registration

Při `unbound=yes` používá dřívější save-count bajt `#73` příznak
`regplate_scrambled`: `0` znamená viditelnou registraci a `$FF` skrytou.
Výchozí commander i Default JAMESON mají hodnotu 0. Při načtení se přijímají
jen hodnoty 0 a `$FF`; libovolná jiná hodnota se normalizuje na 0.

V Anarchy systému je na konci obrazovky Equip Ship položka
`Scramble Ship ID` za `5000.0 Cr` na stejném řádku. Po zakoupení se bajt #73 nastaví
na `$FF`, položka z nabídky zmizí a hráčova registrační rutina i zpráva o blokovaném startu stanice
zobrazí `??-???`. Nová loď nebo Escape Pod vygenerují novou registraci a
současně příznak vrátí na 0.

Pokud hráč se skrytou registrací vstoupí do bezpečné zóny stanice v systému,
jehož vláda není Anarchy, Feudal ani Dictatorship, zvýší se `FIST` nejméně na
100. Kontrola probíhá po každém letovém snímku bezprostředně po aktualizaci
`SSPR`, takže se stav změní hned při vstupu do zóny. Vyšší hodnota se nesnižuje
a uvedené tři vlády příznak ignorují.

V okamžiku, kdy tato kontrola nastaví `FIST=100`, zobrazí se dynamická zpráva
s registrací aktuální stanice a registrací hráčovy lodě. Protože je scramble
aktivní, hráčova běžná registrační rutina vypíše `??-???`. Koncovka se pro
každou zprávu rovnoměrně náhodně vybere z `, SCRAM PIRATE!`, `, RUN PIRATE!`
a `, DIE PIRATE!`. Výběr nyní používá společnou rutinu
`CommMessageRandomThree` v prostoru za RLE dashboardem. Zpráva používá stejný privátní token jako
`DOCK OR LEAVE`, včetně `DLY=100`, a zvolený text zůstává stabilní pro EOR
překreslení. Texty jsou uloženy v rezidentním prostoru za RLE dashboardem;
HICODE obsahuje tiskovou rutinu a tabulku offsetů. LOCODE se nemění.

Při aktivním scramble a `FIST >= 50` mohou lidské pirátské lodě vzniknout bez
hostile bitu. Testovací hodnota `NEUTRAL_PIRATE_CHANCE = 128` znamená šanci
128/256, tedy 50 %. Pirát si ponechá pirate bit, AI i původní agresivitu, dostane
počáteční rychlost 16 až 31 a letí rovně aktuálním kurzem. Při
`unbound=yes` sdílejí všichni piráti ve skupině hrubou pozici určenou rutinou
`Ze`, ale každý dostane vlastní spodní bajty souřadnic x, y a z. Zůstanou proto
pohromadě jako pack, aniž by pasivní lodě vznikaly přesně přes sebe. Platí to
nezávisle na `randomspawns`; tato volba určuje pouze způsob výběru společné hrubé
pozice packu. Před každým `NWSHP` se rychlost pracovního bloku vynuluje, aby ji
další člen skupiny nezdědil po předchozí pasivní lodi. Po zásahu
hráčem rutina `ANGRY` hostile bit obnoví a loď se začne normálně bránit. Bez
scramble nebo při `FIST < 50` zůstává volba hostility beze změny. Thargoidi a
mise jsou z této změny vyloučeni.

Zkušební PAL tape build s běžnou konfigurací včetně `unbound=yes`,
`iffunit=yes` a `renderspeedups=yes` prošel v obou větvích včetně TAP
round-trip ověření.

Po zúžení rozptylu packu prošly v obou větvích kontrolní PAL tape buildy i
šifrované GMA86 PAL buildy s `randomspawns=yes` i `randomspawns=no`; diskové
buildy ověřily automatickou fast-loader sektorovou tabulku. Pro kombinaci
`unbound=yes randomspawns=no` používá vzdálený skok na `me2` dlouhou větev,
zatímco `unbound=no` zachovává původní krátkou větev a úspěšně se sestaví.

Nová `AMBPOS` je v běžné konfiguraci o 3 bajty HICODE větší a LOCODE nemění.
Při `randomspawns=yes` končí main na `R%=$3F19`, `F%=$CCDD` (prakticky volných
230 bajtů LOCODE a 290 bajtů HICODE) a flicker-free na `R%=$3F69`, `F%=$CD41`
(150 bajtů LOCODE a 190 bajtů HICODE). Při `randomspawns=no` je `F%=$CCED`
v main a `F%=$CD51` ve flicker-free.

## Aktivní I.F.F. interrogator

Při `unbound=yes iffunit=yes` obsluhuje letová fáze také klávesu `I`, ale jen
pokud má hráč I.F.F. jednotku skutečně nainstalovanou. Dokud zbývá alespoň
jedna raketa, `I` používá stejnou cílovací cestu jako `T`.

Bez raket zapne `I` jednorázový I.F.F. interrogator a přehraje krátké vysoké
pípnutí z rutiny `BEEP`, stejné jako při získání cíle raketou. Interrogator nemá
žádný stavový indikátor. Jakmile zaměří objekt v hledáčku, znovu přehraje
stejné pípnutí, zobrazí jeho identifikaci přes běžnou registrační rutinu a
potom se bez dalšího zvuku vypne. Klávesa `U` aktivní interrogator zruší a
přehraje stejné `sfxboop` jako odzbrojení rakety.

Stav používá existující `MSAR`; hodnota `MSTG=$FE` pouze čeká na uvolnění
klávesy `I`, aby se funkce po zaměření nebo zrušení okamžitě znovu nezapnula.
Nevznikl nový stavový bajt. Změna přidává 13 bajtů do LOCODE a 104 bajtů do
HICODE. Build s `unbound=no` zůstal bitově shodný ve všech blocích a
kombinace `unbound=yes iffunit=no` se také úspěšně sestavila.

## Bezpečné přepsání commander save na disku

Při `unbound=yes` se před uložením commanderu na disk otevře CBM-DOS command
channel 15 a odešle se příkaz `S:<jméno>`. Mechanika nejprve vyhledá odpovídající
soubor; pokud existuje, smaže jej, a pokud neexistuje, disk ponechá beze změny.
Potom se obnoví běžné parametry souboru a commander se uloží obyčejným jménem.
Není tedy nutné používat nebezpečný save-with-replace prefix `@`.

Změna se netýká ukládání na kazetu. Při chybě otevření command channelu se
provede stávající obsluha `DISK ERROR`. Celá cesta je podmíněna `unbound=yes`;
kontrolní PAL tape build potvrdil, že při `unbound=no` zůstávají `LOCODE`,
`HICODE` i `COMLOD` bitově shodné se stavem před změnou. Implementace přidává
76 bajtů HICODE a žádný LOCODE.

## BBC-style hangár po dokování

Při `unbound=yes` volá dokovací rutina po průletu staničním tunelem nový
`HALL`. Na dobu stávající dokovací prodlevy zobrazí perspektivní hangár ve
stylu BBC Elite. Náhodně vybere jednu ze čtyř připravených skupin lodí, nebo
jednu klasickou hangárovou loď; možný je také prázdný hangár. Podlahové a
stěnové čáry končí na již nakreslených pixelech lodí, takže jimi neprocházejí.

Při kreslení lodí `UNWISE` dočasně přepne všech 22 použitých instrukcí
`EOR (SC),Y` na `ORA (SC),Y`. Překrývající se hrany se proto navzájem nemažou.
Před kreslením pozadí se instrukce vrátí na původní EOR režim. Kontrola
s `renderspeedups=no` i `renderspeedups=yes` potvrdila, že všech 22 adres stále
ukazuje na opcode `$51`.

Samotný hangárový kód zabírá 579 bajtů a sestavuje se v `elite-hangar.asm` na
`DSTORE%+$500`; loader jej ukládá za PackBits dashboard. Soubor nyní obsahuje
také komunikační rutiny a texty popsané níže, takže celý blok má 1002 bajtů a končí na
`$F87A`. Celý RLE, hangárový a komunikační payload tak končí na offsetu `$8EA`
a na konci původní devítistránkové oblasti zbývá 22 bajtů. Komprimovaný nový
dashboard navíc ponechává 81 bajtů před pevnou adresou hangáru; celkem je tedy
volných 103 bajtů ve dvou nesouvislých blocích. Nový i starý dashboard
používají stejnou pevnou adresu hangáru. Původní přidání hangáru zvětšilo
LOCODE pouze o tříbajtové `JSR` a HICODE nezměnilo.

V obou větvích prošly PAL i NTSC tape buildy včetně TAP round-trip kontroly,
šifrované GMA86 PAL buildy včetně ověření fast-loader sektorové tabulky a
šifrované GMA85 NTSC buildy. Úspěšně se sestavil také starý dashboard společně
s `renderspeedups=yes`.

## Nadpis nákupu lodí

Při `unbound=yes` má obrazovka CTRL+3 vlastní vystředěný nadpis `BUY SHIP`.
Běžná obrazovka pod klávesou 3 si ponechává `EQUIP SHIP`. Změna je pouze
v tisku nadpisu, přidává 27 bajtů HICODE a nemění velikost LOCODE.

V obou větvích prošel běžný nešifrovaný tape-pal build a šifrovaný gma86-pal
build, včetně TAP round-trip kontroly a ověření sektorové tabulky GMA loaderu.
Kontrolní tape-pal build s `unbound=no` má LOCODE, HICODE a COMLOD bitově
shodné se stavem před touto úpravou. Vizuální kontrola ve VICE nebyla provedena.

## Plynulé škálování rychloměru

Při `unbound=yes` původní normalizace rychlosti počítala
`floor(DELTA * 15 / maximum)`. Hodnota těsně pod maximální rychlostí proto u
všech třinácti typů lodí končila na 14 pixelech, zatímco zvláštní obsluha
přesného maxima nastavila rovnou 16 pixelů. To způsobovalo viditelný skok o dva
pixely na konci ukazatele.

`ShipNormalizeSpeedBar` nyní používá přímo rozsah 0..16, tedy
`floor(DELTA * 16 / maximum)`, zatímco `ShipNormalizeFuelBar` zůstává na
rozsahu 0..15. Pro všechny maximální rychlosti v `ShipMaxSpeed` je největší
krok ukazatele jeden pixel a hodnota těsně pod maximem se zobrazuje jako 15.
Změna zmenšuje HICODE o 3 bajty a nemění LOCODE.

V obou větvích prošel běžný nešifrovaný tape-pal build s `unbound=yes` a
šifrovaný gma86-pal build včetně TAP round-trip kontroly a ověření sektorové
tabulky GMA loaderu. Kontrolní tape-pal build s `unbound=no` má LOCODE, HICODE
i COMLOD bitově shodné se stavem před změnou. Vizuální kontrola ve VICE nebyla
provedena.

## Oprava artefaktů I.F.F. značek na skeneru

I.F.F. rozšiřuje původní EOR kresbu blipu o pravou část hlavy T. Při mazání se
proto musí použít stejný hostile stav jako při předchozím vykreslení. Artefakty
mohly vzniknout ve třech situacích: `WARPJUNK` kopíroval do INWK pouze bajty
0–31 a ponechal starou hodnotu NEWB v bajtu 36, `ANGRY` mohl změnit L na T mezi
dvěma voláními `SCAN` a `TACTICS` měnil hostile stav ještě před smazáním staré
značky.

Při `iffunit=yes` nyní `WARPJUNK` kopíruje celý 37bajtový blok. Změna hostility
přes zásah laserem nebo vypuštěnou raketu nejprve smaže starou značku a potom
ji překreslí s novým stavem. V `MVEIT` se I.F.F. značka maže před voláním
`TACTICS`. Cesty pro `iffunit=no` zůstávají v podmíněných větvích původní.

Pomocné rutiny jsou v HICODE; oproti bezprostřednímu stavu před opravou se
LOCODE zmenšil o 5 bajtů a HICODE narostl o 78 bajtů. V obou větvích prošel
běžný nešifrovaný tape-pal build včetně TAP round-trip kontroly a šifrovaný
gma86-pal build včetně ověření fast-loader sektorové tabulky. Vizuální kontrola
ve VICE nebyla provedena. Kontrolní tape-pal build s `iffunit=no` se v obou
větvích také úspěšně sestavil.

## Dostupnost Mamby podle ekonomiky

Mamba je v anarchických systémech nabízena pouze pro ekonomiky 0 až 2.
Sidewinder zůstává dostupný pro ekonomiky 0 až 6 a Krait pro 0 až 5. Pro
ekonomiku 3 se proto používá cenově seřazený seznam se Sidewinderem a Kraitem,
jehož poslední položka Cobra Mk III zachovává správný celkový počet nabídek.

Změna přidává 1 bajt do HICODE a nemění LOCODE. V obou větvích prošel běžný
nešifrovaný tape-pal build včetně TAP round-trip kontroly a šifrovaný
gma86-pal build včetně ověření fast-loader sektorové tabulky.

## Fronta komunikačních zpráv a testovací pirátská hláška

Soukromý letový token 2 nyní obsluhuje obecné zprávy od stanic i AI lodí.
Každá přijatá zpráva jednou krátce pípne. Ve 3D pohledu se zobrazí okamžitě;
na mapě, Statusu a ostatních obrazovkách se uloží do jediné čekající pozice a
zobrazí se po návratu do 3D pohledu bez druhého pípnutí. Novější komunikace
vždy nahradí starší čekající komunikaci a neexistují žádné priority zpráv.
Dynamické registrace odesílatele a příjemce se při odložení ihned zkopírují,
takže zpráva zůstane správná i po zániku nebo opětovném použití AI slotu.
Stanice i AI komunikace připravují registraci hráče společnou rutinou
`CommMessagePreparePlayerRecipient`; při aktivním scrambled ID uloží příjemce
jako `??-???`.
Běžný reset letového stavu v `RES2` zároveň nuluje `CommMessagePending`, takže
odložená komunikace nepřežije smrt, hyperspace, mis-jump, zadokování, start ze
stanice ani použití záchranného modulu.
Běžné zprávy přes ostatní tokeny, například bounty nebo ENERGY LOW, se nemění.

Jako první AI test vybírají pirátské spawny rovnoměrně z textů
`, BOO YOU DEAD!`, `, PREPARE DIE!` a `, SCUMBAG!`. Společná rutina
`CommMessageRandomThree` vrací index 0 až 2; přičtení začátku souvislé skupiny
druhů zpráv ji používá jak pro tyto tři hlášky, tak pro staniční trojici
`SCRAM/RUN/DIE PIRATE!`. Stejným způsobem lze přidat další třířádkové skupiny
pro obchodníky, policii nebo bounty huntery. Před
celým packem se vynuluje uložený odesílatel a po každém `NWSHP` se zkontroluje
úspěšné vytvoření i finální bity `NEWB`. Odesílatelem se stane první úspěšně
vytvořený pirát, který si po případné neutralizaci stále ponechal současně
pirate a hostile bit. Pravděpodobnost se vyhodnotí až po zpracování celého
packu a zpráva se proto odešle nejvýše jednou. Pokud se žádný člen nevytvoří
nebo jsou všichni pasivní, zpráva se neodešle.

Stejná kontrola následuje také za samostatným spawnem v cestě `focoug`.
Touto cestou mohou vznikat i bounty hunters, Thargoid, Cougar a Constrictor,
ale komunikaci může vyvolat pouze úspěšně vytvořená loď s finální kombinací
pirate+hostile. Každý takový samostatný pirát má právě jeden pokus.

`PIRATE_COMM_CHANCE_PERCENT` určuje společnou pravděpodobnost v rozsahu
0 až 100 %. Nula kód zcela vypne, 100 zprávu vždy povolí a mezilehlá hodnota
se převede na osmibitový práh a spotřebuje jedinou hodnotu z `DORND` za celý
pack nebo samostatného piráta. Aktuální hodnota je 50 %. Pro přesných 50 % se
používá přímo horní bit náhodného bajtu, který rozděluje všech 256 hodnot na
128 povolených a 128 zamítnutých; ostatní mezilehlé hodnoty nadále používají
osmibitový práh.

Všechny čtyři konstanty `PIRATE_COMM_CHANCE_PERCENT`,
`TRADER_COMM_CHANCE_PERCENT`, `BOUNTY_HUNTER_COMM_CHANCE_PERCENT` a
`POLICE_COMM_CHANCE_PERCENT` lze nezávisle přepsat na libovolné celé číslo
od 0 do 100 bez další změny kódu. Assembler tento rozsah kontroluje pomocí
`ASSERT`. Hodnoty 0, 50 a 100 mají zkrácené překladové větve; ostatní hodnoty
1 až 99 používají obecný osmibitový práh.

Samostatná větev `MTT4` po úspěšném `NWSHP` nyní umožňuje komunikaci obchodníka,
tedy lodě typu Cobra Mk III, Python, Boa nebo Anaconda vytvořené touto větví. Přímé
napojení na `MTT4` je záměrné: blueprinty lodí Cobra Mk III, Python a Boa nemají
v tabulce `E%` nastavený trader bit, přestože je tato spawn větev považuje za
obchodníky. Neúspěšný spawn zprávu neodešle. Odesílatelem je nová loď v posledním
obsazeném slotu `FRIN` a rovnoměrně se vybírá z `, HAVE NICE TRIP`, `, HELLO`
a `, JUST PASSING`.

`TRADER_COMM_CHANCE_PERCENT` je samostatná pravděpodobnost v rozsahu 0 až 100 %
se stejnou logikou jako pirátská volba. Vyhodnocuje se
jednou za úspěšně vytvořeného obchodníka a aktuálně je nastavena na 50 %.

Samostatný spawn v cestě `focoug` po úspěšném `NWSHP` rozlišuje finální příznaky
role. Finální kombinace pirate+hostile používá pirátskou sadu, zatímco kombinace
bounty-hunter+hostile používá sadu `, WRONG PLACE!`, `, HERE WE GO!`
a `, MY BOUNTY!`. Odesílatelem je opět skutečný nově vytvořený slot. Tím se
zabrání tomu, aby pirátské varianty Cobra Mk III, Asp Mk II a Python ze stejné
spawn větve dostaly zprávu lovce odměn. Jde pouze o klasifikaci role lodi;
nezavádí se žádná priorita zpráv a jediná čekající pozice stále obsahuje poslední
přijatou komunikaci.

`BOUNTY_HUNTER_COMM_CHANCE_PERCENT` je třetí nezávislá pravděpodobnost v rozsahu
0 až 100 %, vyhodnocovaná jednou pro samostatně vytvořeného hostile bounty
huntera. Aktuálně je nastavena na 50 %.

Hostile police Viper může komunikovat ze dvou spawn cest. Viper vypuštěná
nepřátelskou stanicí dědí hostile příznak stanice. Náhodně vytvořená Viper dostane
hostile příznak v `TACTICS` jen při `FIST >= 40`, takže stejná podmínka rozhoduje
o zprávě bez změny herního chování. Po úspěšném spawnu se rovnoměrně vybírá
z `, STOP NOW!`, `, WE FOUND YOU!` a `, SURRENDER!`.

`POLICE_COMM_CHANCE_PERCENT` je čtvrtá nezávislá pravděpodobnost v rozsahu
0 až 100 %, vyhodnocovaná jednou pro každou takto vytvořenou hostile police
Viperu. Aktuálně je nastavena na 50 %.

Původní krátká větev `BCS P%+7` v náhodné policejní spawn cestě byla po
přidání komunikačního hooku příliš krátká: při zamítnutém spawnu přeskočila
`NWSHP`, ale dopadla přímo do `RandomPoliceSpawnComplete`. Carry z porovnání
pak vypadal jako úspěšný spawn a jako odesílatel se použil poslední existující
slot, typicky Slunce v slotu 1. Pojmenované návěští `randomPoliceDone` nyní
přeskakuje spawn i komunikační hook, takže policejní zprávu může vyvolat pouze
skutečně vytvořená Viper. Změna nemění velikost kódu.

Společný prefix `, ` se nyní tiskne jednou v `CommMessagePrint` a není opakován
v každém uloženém textu. Původních deset textových zakončení proto zabírá 122
bajtů od `$F7F0` do `$F869`; policejní zakončení navazují od `$F86A` do `$F88C`
při aktuálním nastavení všech šancí na 50 %.
Desetibajtová
`CommMessageOffsets` zůstává v HICODE; její jednobajtové offsety nadále ukazují
relativně k `CommMessageText`, i když text překračuje hranici stránky `$F7FF`.
Tři zakončení lovce odměn mají 36 bajtů v samostatném bloku `$F466` až `$F489`
těsně před hangárem. S `dials=new` zbývá 39 bajtů před tímto blokem, 6 bajtů
za ním a při šanci 50 % 3 bajty za hangárem; celkem 48 nesouvislých bajtů.
Obecná mezilehlá pirátská šance spotřebuje poslední 2 bajty této rezervy, takže
v nejhorším případě zbývá 39 + 6 + 1 = 46 bajtů. Delší `dials=old` ponechává
ve stejných třech blocích 19 bajtů při 50 % a 17 bajtů v nejhorším případě.

Komunikační rutiny a texty ponechané v prostoru RLE dashboardu zabírají při
aktuálních 50 % 478 bajtů a v největší obecné mezilehlé variantě 480 bajtů.
Proti stavu před zavedením komunikace se LOCODE v obou větvích
zmenšil o 2 bajty a HICODE se zmenšil o 166 bajtů. Kontrola zároveň odhalila starší
chybějící podmínku u `IFFAngryCurrentShip` a `IFFAngryMissileTarget`:
`unbound=no iffunit=yes` odkazovalo na rutiny sestavované pouze pro Unbound.
Mimo Unbound nyní tyto dvě cesty používají původní `ANGRY` a kontrolní build
znovu prochází.

V obou větvích prošel nešifrovaný tape-pal build s `unbound=yes`, nešifrovaný
i šifrovaný gma86-pal build včetně ověření fast-loader sektorové tabulky a kontrolní
tape-pal build s `unbound=no iffunit=yes`. Oba tape buildy prošly TAP
round-trip kontrolou. Úspěšně prošel také tape-pal build s `unbound=yes`,
`iffunit=no` a `randomspawns=no`. Vizuální kontrola ve VICE nebyla provedena.
Po přidání komunikace lovců odměn byly znovu úspěšně provedeny nešifrovaný
gma86-pal pro `dials=new` i `dials=old`, šifrovaný gma86-pal s `dials=new`
a nešifrovaný tape-pal s `dials=new`; ve všech případech samostatně v obou
větvích. GMA buildy ověřily fast-loader tabulku a tape buildy prošly úplným
round-trip ověřením. Vizuální kontrola nové zprávy ve VICE zatím provedena nebyla.
Po doplnění policejní komunikace znovu prošly v obou větvích nešifrované
gma86-pal buildy s `dials=new` i `dials=old`, šifrovaný gma86-pal, běžný
nešifrovaný tape-pal a kontrolní tape-pal s `unbound=no`. GMA buildy ověřily
fast-loader tabulku a tape buildy úplný round-trip. Hodnota policejní šance 50 %
byla rovněž sestavena; v `main` prošel celý tape build, ve flicker-free prošly
herní zdroj, HICODE a loader, ale následný pomocný zápis `README.txt` zablokoval
přechodný zámek souboru. Finální 100% buildy obou větví prošly celé. Vizuální
kontrola policejních zpráv ve VICE zatím provedena nebyla.

Po finálním nastavení všech čtyř komunikačních šancí na 50 % prošel v obou
větvích běžný nešifrovaný tape-pal build včetně úplného TAP round-trip ověření,
šifrovaný gma86-pal build a přesná běžná nešifrovaná gma86-pal konfigurace
s `renderspeedups=yes`. Oba GMA buildy ověřily automatickou fast-loader
sektorovou tabulku. Optimalizovaná 50% větev a odstranění nadbytečných instrukcí
udržely `HANGAR.bin` na 1021 bajtech (`$3FD`). Kontrolní plné tape-pal buildy
se společnými hodnotami 0, 1, 50, 99 a 100 prošly v obou větvích; hodnoty 1 a
99 představují největší obecnou variantu a mají 1023 bajtů (`$3FF`), stále pod
přísným limitem `$900` celé oblasti dashboardu.

Níže uvedené hodnoty paměti byly znovu odečteny 2. září 2026 z compile.txt
pro oba PAL buildy; starší tabulka již neodpovídala aktuálnímu zdrojovému kódu.

## Poškození při kolizi podle velikosti lodí

Pouze při `unbound=yes` se běžné kolize s AI objekty škálují podle pěti
velikostních tříd: debris, very small, small, medium a large. Sidewinder je
samostatně ve třídě very small; Adder, Gecko, Mamba, Krait a Worm jsou small.
Python, Boa a Anaconda jsou large. Asteroid a rock hermit jsou large, boulder
je small a splinter patří mezi debris. Stanice nadále používají svou původní
zvláštní kolizní větev.

Stejná třída zachovává původní poškození `128 + current AI energy / 2`. AI
objekt o jednu třídu menší způsobí polovinu a o dvě či více tříd menší čtvrtinu
původního poškození. AI objekt o jednu třídu větší přidá 64 bodů se saturací na
255; rozdíl dvou nebo více tříd hráče okamžitě zničí. Kolidující AI objekt se
nadále označí ke zničení, aby se kolize neopakovala v následujícím snímku.

Třídy nezabírají samostatné tabulky. AI třída je v bitech 4 až 6 tabulky
`KWH%`; `EXNO2` je před započtením combat points maskuje. Hráčova třída je ve
stejných bitech `ShipShieldStrength`; `PlayerShieldStrength` vrací pouze spodní
čtyři bity skutečné síly štítu. Změna přidává 79 bajtů LOCODE a 4 bajty HICODE.
Kontrolní `unbound=no` PAL tape build byl v obou větvích porovnán proti čistému
HEAD: všech 34 BIN/PRG/TAP souborů bylo bitově shodných. Vizuální test kolizí ve
VICE zatím proveden nebyl.

## Volná paměť

Naměřeno pro běžnou konfiguraci projektu uvedenou výše:

| Větev | Konec LOCODE R% | Rozdíl do $4000 | Prakticky přidat | Konec HICODE F% | Rozdíl do $CE00 | Prakticky přidat |
|---|---:|---:|---:|---:|---:|---:|
| main | $3FA0 | 96 B | 95 B | $CD8F | 113 B | 112 B |
| flicker-free | $3FF0 | 16 B | 15 B | $CDF3 | 13 B | 12 B |

Praktická hodnota je o jeden bajt nižší kvůli assemblerovým podmínkám:

    ASSERT R% < $4000
    ASSERT F% < $CE00

LOCODE a HICODE jsou samostatné oblasti a jejich rezervy se nesčítají.
Ve flicker-free jsou nyní těsné obě oblasti, přičemž menší rezervu má HICODE.
Po každé změně znovu odečíst R% a F% z compile.txt.

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
8. ZIP vytvořit pouze na výslovnou žádost uživatele.

## Nativní EasyFlash varianty

Makefile podporuje nové varianty `easyflash-pal` a `easyflash-ntsc`.
PAL používá stejný herní build jako GMA86, NTSC stejný build jako GMA85;
samotné herní bloky COMLOD, LOCODE a HICODE se kvůli cartridge nemění.
Výstupy jsou nativní CRT obrazy typu EasyFlash v adresáři
`5-compiled-game-cartridges/`.

Bank 0 obsahuje ROMH reset stub a CBM80 bootstrap v ROML. Reset stub přepne
EasyFlash z ultimax do osmikipobajtového režimu a předá řízení standardnímu
KERNAL resetu. Bootstrap provede `IOINIT`, `RAMTAS`, `RESTOR` a `SCINIT`, poté
zkopíruje 544bajtový rezidentní loader do `$0334-$0553`. Pořadí je důležité:
`RAMTAS` musí proběhnout před kopírováním loaderu, jinak jeho pracovní prostor
vymaže a pozdější kazetové či diskové commander I/O nemá správně inicializované
KERNAL hodnoty.

Python skript `2-build-files/elite-easyflash.py` ukládá do ROML bank od banky 1
manifest `ECRT`, tři deskriptory s cílovou adresou, délkou a XOR kontrolou a za
ně bloky COMLOD (`$4000`), LOCODE (`$1D00`) a HICODE (`$6A00`). Loader ověřuje
manifest, cílové adresy i všechny tři XOR hodnoty. Pro zápis pod ROML cartridge
dočasně skryje, po zavedení všech bloků ji vypne a vstoupí do hry na `$1D22`.
Standardní ukládání a načítání commanderů z kazety i disku tak zůstává
dostupné. Aktuální obrazy mají sedm payload ROML bank a velikost 73936 bajtů;
bootstrap má 719 bajtů a ROMH reset stub 38 bajtů.

V obou větvích prošly úplné `easyflash-pal` i `easyflash-ntsc` buildy s běžnou
konfigurací Elite: Unbound včetně `renderspeedups=yes`. Všechny čtyři CRT obrazy
prošly `cartconv -c` a ve VICE se v odpovídajícím PAL nebo NTSC režimu dostaly
na titulní obrazovku. Uživatel před integrací ověřil stejnou opravenou bootovací
architekturu při skutečném načítání i ukládání commanderů na kazetu a disk.
