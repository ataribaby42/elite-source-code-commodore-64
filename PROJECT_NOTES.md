# Elite C64 / Elite: Unbound – projektové poznámky

Stav poznámek: 3. září 2026.

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

Níže uvedené hodnoty paměti byly znovu odečteny 3. září 2026 z compile.txt
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

## Oprava Data on System a adresování popisů systémů

Dne 3. září 2026 byla do obou větví přenesena oprava Marka Moxona z commitu
`d796cbadb3d336b8ef02db6dc902216dac9b3945`:

https://github.com/markmoxon/elite-source-code-commodore-64/commit/d796cbadb3d336b8ef02db6dc902216dac9b3945

`NRU%` je nyní 26 místo 0. Smyčka `PDESC` tak prochází pouze platné
záznamy tabulek `RUPLA` a `RUGAL`; původní nulový čítač podtekl a dovolil
čtení mimo tabulky, například se zamrznutím na Biarge během mise Constrictor.
Jde o opravu společné chyby původní hry, proto platí i při `unbound=no`.

Kontrola skutečného buildu odhalila také starší související chybu Unbound:
upravené tokenizované texty posunuly všechny tři tabulky o 12 bajtů, zatímco
herní kód stále používal původní pevné adresy. Pouhá změna `NRU%` proto
nestačila. Aktuální rozložení je:

| Režim | RUPLA | RUGAL | RUTOK |
|---|---:|---:|---:|
| unbound=no | $1A28 | $1A42 | $1A5C |
| unbound=yes | $1A1C | $1A36 | $1A50 |

Makefile nyní bezprostředně po sestavení `elite-data.asm` spouští
`2-build-files/elite-token-layout.py`. Ten přečte skutečné symboly z právě
vytvořeného `compile.txt` a zapíše `3-assembled-output/elite-token-layout.asm`.
Hlavní ASM tento generovaný soubor načte místo pevných adres. Generátor
kontroluje úplnost a platnost rozložení; assembler navíc ověřuje, že obě
tabulky mají přesně `NRU%` položek. Generovaný soubor se neupravuje ručně
a je spolu s ostatními build výstupy ignorován Gitem.

Oprava nezvětšuje herní kód ani data. Pro běžný Unbound PAL tape build se
oproti stavu před opravou změnilo přesně pět bajtů LOCODE: hodnota čítače,
tři dolní bajty adres ve vyhledávání a dolní bajt adresy tokenů v `DETOK3`.
HICODE a COMLOD zůstaly bitově shodné. V původním režimu generované adresy
odpovídají původním konstantám; výsledné bloky jsou shodné se samostatně
přenesenou upstream opravou. Stav, podmínky spuštění a odměny misí se nemění.

### Ověření

V každé větvi samostatně prošlo všech devět následujících příkazů
(celkem 18 finálních buildů):

```text
make variant=tape-pal encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-pal encrypt=no match=no verify=no unbound=no
make variant=gma86-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma85-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
```

TAP buildy prošly kontrolou ROM + COMLOD + LOCODE + HICODE round-trip.
GMA86 buildy včetně šifrované varianty ověřily automatickou sektorovou
tabulku. Pro všechny buildy byly zkontrolovány skutečně vygenerované volby
a adresy tabulek.

Izolovaná kontrola sestavených 6502 instrukcí `PDESC` prověřila všech
256 systémů ve všech osmi galaxiích, mission byte 0, 1 a 3, a přeskakování
override tabulek za letu nebo při zobrazení vzdáleného systému. Celkem
šlo o 10 240 případů na build, tedy 184 320 případů. Čtení zůstala v mezích
tabulek, smyčka prošla nejvýše 26 položek a výsledné tokeny odpovídaly
skutečným datům. Tiskové rutiny byly při této kontrole nahrazeny kontrolními
body; nejde o vizuální test celé obrazovky ve VICE.

Pro šest distribučních variant s `renderspeedups=yes` zůstává rezerva
beze změny: main 95 B LOCODE a 112 B HICODE, flicker-free 15 B LOCODE
a 12 B HICODE. RLE oblast má v obou větvích oddělené mezery 39 + 6 + 3 B
(fyzicky 48 B); kvůli přísné koncové podmínce lze z poslední mezery využít
jen 2 B, tedy dohromady prakticky 47 B v nesouvislých blocích.

## Automatické offsety názvů lodí a meze textových oblastí

Dne 3. září 2026 byly v obou větvích nahrazeny ruční hodnoty
`ShipNameOffsets` rozdíly návěští jednotlivých názvů vůči `ShipNames`.
Po změně délky názvu tak assembler sám přepočítá následující offsety.
Pořadí 13 lodí, jejich názvy i výsledné bajty tabulky zůstávají stejné.

Assembler navíc hlídá:

- `ShipNames - ShipNameOffsets = 13`: počet offsetů odpovídá typům lodí;
- `ShipNamesEnd - ShipNames <= $100`: celá tabulka včetně nulových
  zakončení se vejde do rozsahu 8bitového indexu v `ShipPrintName`;
- `endian <= $1D00` v `elite-data.asm`: všechny tokenové texty včetně
  posledního zakončení leží nejvýše na `$1CFF`, kam sahá kopírování
  nízkých dat loaderem. Na `$1D00` už začíná LOCODE. Kontrola se vztahuje
  na skutečná tokenová data před původními nepoužívanými bajty.

Tyto změny přidávají pouze návěští, výrazy a kontroly při sestavení.
Nezabírají žádné další bajty RAM ani nemění chování původní hry.

Znovu prošlo všech devět přesných build příkazů uvedených v předchozí sekci,
samostatně v každé větvi (18 buildů celkem): běžný PAL tape, šifrovaný
GMA86 PAL, původní PAL tape s `unbound=no` a všech šest distribučních
PAL/NTSC variant s `renderspeedups=yes`. Bloky LOCODE, HICODE a COMLOD
v nešifrované podobě, IANTOK a ELTC jsou u každé konfigurace bitově shodné
se stavem bez těchto preventivních úprav; ověřeny byly i skutečné build volby.
Přesné příkazy se oproti předchozí sekci nezměnily.

V dočasných oddělených sestaveních bylo navíc ověřeno, že prodloužení
názvu ADDER správně posune offsety všech dalších lodí, velikost názvů
256 bajtů projde a 257 bajtů vyvolá ASSERT. Konec tokenů přesně na `$1D00`
(exkluzivní konec) projde, `$1D01` vyvolá ASSERT. Všech 12 těchto kontrol
v obou větvích dopadlo podle očekávání; testovací názvy ani výplně
nebyly přeneseny do projektu.

Pro distribuční konfigurace zůstává skutečná rezerva main 95 B LOCODE
a 112 B HICODE, flicker-free 15 B LOCODE a 12 B HICODE. RLE mezery
zůstávají 39 + 6 + 3 B fyzicky, z nichž poslední dovoluje využít jen 2 B.

## Automatické vstupy COMLOD/LOCODE a adresy patchů hangáru

Dne 3. září 2026 byly v obou větvích opraveny další adresy závislé
na rozložení kódu:

- COMLOD a hlavní hra exportují skutečná návěští `ENTRY` a `S%` jako
  `COMLOD_ENTRY` a `GAME_ENTRY` do aktuálního výpisu assembleru.
- Nový `2-build-files/elite-loader-layout.py` z nich vytvoří ignorovaný
  `3-assembled-output/elite-loader-layout.asm`. Kontroluje jednoznačnost
  exportů, rozsah adres, umístění v čerstvě sestavených binárkách a vstupní
  instrukci `CLD`. Chybějící či neplatný export zastaví build.
- GMA, TAP a EasyFlash loadery načítají tento společný výstup místo
  ručních vstupních adres. Makefile proto sestavuje hlavní hru a COMLOD
  před GMA loaderem. Generovaný soubor se nikdy neupravuje ručně.
- Hangár používá návěští přímo na všech 22 přepisovaných instrukcích.
  Dřívější výrazy jako `LI81+2` tak nejsou závislé na délce předchozí
  instrukce. Počet položek smyčky se odvozuje z tabulky; `ASSERT` hlídá
  její neprázdnost, 8bitový rozsah a stejnou délku dolních i horních bajtů.
  Oba skoky `P%+4` jsou nahrazeny pojmenovanými návěštími.

V aktuálních Unbound buildech je vstup COMLOD na `$758A`, zatímco původní
ručně zadané `$7596` ukazovalo dovnitř instrukce. Nejde o nahrazení jedné
pevné adresy jinou: každý build nyní získá vlastní skutečné adresy.
Úprava hangáru je preventivní; jeho dosavadní výsledné adresy byly správné.

Změněné soubory této opravy v každé větvi: `Makefile`,
`2-build-files/elite-loader-layout.py`, `PROJECT_NOTES.md` a soubory
`elite-source.asm`, `elite-loader.asm`, `elite-gma1.asm`,
`elite-tape-loader.asm`, `elite-easyflash-loader.asm` a `elite-hangar.asm`
v `1-source-files/main-sources/`. Dřívější úpravy tokenových tabulek,
názvů lodí a projektových pokynů zůstaly zachovány.

### Ověření opravy COMLOD a hangáru

Každý následující příkaz prošel samostatně v main i flicker-free:
16 konfigurací na větev, celkem 32 úspěšných finálních sestavení.

```text
make variant=tape-pal encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma85-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-pal encrypt=no match=no verify=no unbound=no
make variant=tape-ntsc encrypt=no match=no verify=no unbound=no
make variant=gma86-pal encrypt=no match=no verify=no unbound=no
make variant=gma85-ntsc encrypt=no match=no verify=no unbound=no
make variant=easyflash-pal encrypt=no match=no verify=no unbound=no
make variant=easyflash-ntsc encrypt=no match=no verify=no unbound=no
make variant=source-disk-build encrypt=no match=no verify=no unbound=no
make variant=source-disk-files encrypt=no match=no verify=no unbound=no
```

U všech 32 konfigurací jsou herní bloky `LOCODE.unprot.bin`,
`HICODE.unprot.bin`, `COMLOD.unprot.bin`, `ELTC.bin` a `IANTOK.bin`
bitově shodné se stavem před touto opravou; beze změny jsou také build
volby. Porovnané původní obrazy TAP, D64 a CRT s `unbound=no` jsou rovněž
bitově shodné. U Unbound se opravují operandy skoků v mediálních loaderech,
nikoli herní obsah. TAP kontroly round-trip a GMA86 kontrola skutečné
sektorové tabulky včetně šifrované varianty prošly.

Ve všech sestaveních byly ověřeny exportované vstupy a operandy skoků.
V 16 Unbound konfiguracích bylo zkontrolováno všech 352 adres patchů
hangáru: každá míří na správný opcode. V izolovaných sestaveních pro obě
větve bylo ověřeno automatické sledování posunutých vstupů, odmítnutí
neplatných exportů, posun adresy patchované instrukce po vložení bajtu
a selhání assembleru při rozdílné délce obou částí tabulky.
Testovací výplně ani úmyslné chyby nejsou součástí projektu.

VICE ověřil načtení všech šesti distribučních Unbound PAL/NTSC obrazů
a šifrovaného GMA86 PAL v každé větvi: celkem 14 úspěšných startů až
na skutečný vstup hlavní hry. Diskové testy odpovídají `Y` na otázku
fast loaderu; čekání na tuto otázku samo o sobě není selhání loaderu.
Jde o test načítání, nikoli o úplný herní nebo vizuální test hangáru.

### Paměť po opravě COMLOD a hangáru

Oprava nepřidává žádné bajty do herních bloků ani RLE oblasti.

| Konfigurace | Větev | LOCODE volné | HICODE volné |
|---|---|---:|---:|
| Šest distribučních variant, renderspeedups=yes | main | 95 B | 112 B |
| Šest distribučních variant, renderspeedups=yes | flicker-free | 15 B | 12 B |
| Běžný tape-pal a šifrovaný gma86-pal výše, bez renderspeedups | main | 161 B | 125 B |
| Běžný tape-pal a šifrovaný gma86-pal výše, bez renderspeedups | flicker-free | 81 B | 25 B |

RLE oblast má stále oddělené mezery 39 + 6 + 3 B, fyzicky 48 B.
Kvůli přísné koncové podmínce lze v poslední mezeře přidat pouze 2 B;
prakticky je tedy využitelných 47 B v nesouvislých blocích. Rezervy
LOCODE a HICODE již zohledňují přísné nerovnosti koncových kontrol.

## Odstranění čtyř zbytečných zápisů v MVS4

Dne 3. září 2026 byla v obou větvích převzata optimalizace z upstream commitu:

https://github.com/markmoxon/elite-source-code-commodore-64/commit/7aedfeb6f45af3b9ae71d90e6fcaf8fd1826f371

Při `unbound=yes` se vynechávají přesně čtyři instrukce `STX P`
v rutině `MVS4`, které odstraňuje tento commit. Každý z těchto zápisů
je přepsán voláním `MAD -> MULT1` dříve, než se jeho hodnota použije.
První dvojice `LDX INWK,Y / STX P` ani další instrukce se nemění.

Zápisy jsou zachovány pod `IF NOT(_UNBOUND)`, takže `unbound=no`
zůstává binárně beze změny. Optimalizace šetří 8 B HICODE a 12 CPU cyklů
na jedno volání `MVS4`, tedy 36 cyklů při rotaci všech tří orientačních
vektorů lodi. Nejde o opravu chybného výsledku výpočtu.

Změněny byly pouze `1-source-files/main-sources/elite-source.asm`
a `PROJECT_NOTES.md` v obou větvích. Dřívější necommitované změny
zůstaly zachovány.

### Ověření optimalizace MVS4

V každé větvi prošlo všech devět následujících příkazů, celkem 18 buildů:

```text
make variant=tape-pal encrypt=no match=no verify=no unbound=no
make variant=tape-pal encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma85-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
```

Skutečně sestavená rutina je v každém Unbound buildu kratší přesně o osm
bajtů a liší se pouze vynecháním uvedených čtyř instrukcí. Po přesunu
následujícího kódu byly ověřeny vstupy loaderů a všech 352 adres patchů
hangáru v 16 Unbound sestaveních. Změny COMLOD odpovídají výhradně novým
adresám uvnitř vloženého bloku hangáru; jeho velikost se nemění.
TAP round-trip i kontroly GMA86 sektorové tabulky včetně šifrované varianty
prošly.

V emulátoru instrukcí 6502 byly porovnány skutečné sestavené rutiny před
a po úpravě: 3 840 případů v každé větvi, celkem 7 680 porovnání.
Test zahrnoval krajní i náhodné úhly, náhodná data vektorů, všechny tři
volby vektoru a různé počáteční registry a příznaky při vypnutém
desítkovém režimu. Výsledné registry, příznaky a celá zero page byly
vždy shodné; každý průchod ušetřil přesně 12 CPU cyklů. Jde o izolovaný
test rutiny, nikoli o nové úplné herní testování ve VICE.

Původní PAL tape buildy s `unbound=no` mají stejné herní bloky i celý
výsledný TAP jako před úpravou.

### Paměť po optimalizaci MVS4

| Konfigurace | Větev | LOCODE volné | HICODE volné |
|---|---|---:|---:|
| Šest distribučních variant, renderspeedups=yes | main | 95 B | 120 B |
| Šest distribučních variant, renderspeedups=yes | flicker-free | 15 B | 20 B |
| Běžný tape-pal a šifrovaný gma86-pal výše, bez renderspeedups | main | 161 B | 133 B |
| Běžný tape-pal a šifrovaný gma86-pal výše, bez renderspeedups | flicker-free | 81 B | 33 B |

LOCODE ani RLE oblast se velikostí nemění. RLE mezery zůstávají fyzicky
39 + 6 + 3 B (48 B), prakticky využitelných je kvůli přísné koncové
podmínce 47 B v nesouvislých blocích.

## Další nepoužívaný kód: DKS2, Checksum a newosrdch

Dne 3. září 2026 byla v obou větvích převzata optimalizace z upstream commitu:

https://github.com/markmoxon/elite-source-code-commodore-64/commit/aac5bcaebd95cba7a724e6c0338fb6fe91b8adad

Všech šest změn platí pouze pro `unbound=yes`. Původní instrukce a
návěští jsou zachovány pod `IF NOT(_UNBOUND)`; při `unbound=no`
se sestavují jako před úpravou. Opraveny byly také dva překlepy
`650s` na `6502` v komentářích.

| Vynechaný kód | Úspora HICODE |
|---|---:|
| Nepoužívaná joysticková rutina DKS2 | 7 B |
| Nepoužívaná rutina Checksum včetně CHKLoop | 34 B |
| Nepoužívaná rutina newosrdch včetně badkey a coolkey | 25 B |
| Zbytečné CLC před LDY/CPY v LIlog6 | 1 B |
| BIT vzniklé z EQUB $2C a přeskočeného STA SC+1 v RR2 | 3 B |
| Druhé, nedosažitelné RTS za BDexitirq | 1 B |
| Celkem | 71 B |

Ve zdrojích nejsou aktivní volání odstraněných rutin ani odkazy na jejich
vnitřní návěští zvenčí. Před těmito bloky jsou návraty nebo nepodmíněný
skok, takže do nich řízení nepropadá. `Checksum` je starý nepoužívaný
kód z verze pro 6502 Second Processor; není to kontrola commander dat
ani současný build skript pro checksumy a šifrování. Ty zůstávají zachovány.

Změněny byly pouze `1-source-files/main-sources/elite-source.asm`
a `PROJECT_NOTES.md` v main i flicker-free. Ostatní rozpracované změny
zůstaly zachovány.

### Ověření odstranění nepoužívaného kódu

V každé větvi prošlo všech devět následujících příkazů, celkem 18 konfigurací:

```text
make variant=tape-pal encrypt=no match=no verify=no unbound=no
make variant=tape-pal encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma85-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
```

Assembler potvrdil nepřítomnost všech šesti vynechaných návěští v Unbound,
nezměněný konec LOCODE a zkrácení HICODE přesně o 71 B. Velikost RLE
payloadu se nemění; změny vloženého hangáru odpovídají novým adresám
přesunutého kódu. Ověřeny byly skutečné vstupy loaderů a všech 352 adres
patchů hangáru v 16 Unbound sestaveních. TAP round-trip a GMA86 kontroly
sektorové tabulky včetně šifrované varianty prošly.

U původních PAL tape buildů s `unbound=no` byly porovnány herní bloky
i kompletní výsledné TAP soubory; jsou bitově shodné s předchozím stavem.

Na emulovaném 6502 byly porovnány skutečné instrukce obou změněných
aktivních částí před a po úpravě:

- Úsek `LIlog6` do první větve za `CPY`: všech 65 536 kombinací
  Y1/Y2 s oběma vstupními hodnotami carry, 131 072 případů na větev,
  262 144 celkem. Stav registrů, příznaků a zero page je shodný;
  úsek potřebuje o 2 CPU cykly méně.
- Tiskový úsek `RR2` až do návratu: všechny řádky 0–23 a sloupce
  0–30, různé vstupní bitmapy a čtyři kombinace bitů N/V v paměti čtené
  původní instrukcí `BIT`. Celkem 2 976 případů na větev, 5 952 celkem.
  Výsledné pixely, barvy, zero page a návratové registry jsou shodné,
  carry zůstává vynulované a úsek ušetří 4 CPU cykly.

V příznaku V po tiskové rutině může být rozdíl: odstraněné `BIT` ho
dříve přepisovalo. V není součástí návratového kontraktu `CHPR`;
jeho použití v okolním kódu si stav nastavuje samostatně. Test tisku
proto ověřuje návratový kontrakt a ostatní příznaky, nikoli shodu V.
Jde o izolované regresní kontroly, nikoli o nové úplné hraní ve VICE.

### Paměť po odstranění nepoužívaného kódu

| Konfigurace | Větev | LOCODE volné | HICODE volné |
|---|---|---:|---:|
| Šest distribučních variant, renderspeedups=yes | main | 95 B | 191 B |
| Šest distribučních variant, renderspeedups=yes | flicker-free | 15 B | 91 B |
| Běžný tape-pal a šifrovaný gma86-pal výše, bez renderspeedups | main | 161 B | 204 B |
| Běžný tape-pal a šifrovaný gma86-pal výše, bez renderspeedups | flicker-free | 81 B | 104 B |

RLE mezery se nemění: fyzicky 39 + 6 + 3 B (48 B), prakticky využitelných
47 B v nesouvislých blocích. Rezervy LOCODE a HICODE již zohledňují
přísné nerovnosti koncových kontrol.

## Oprava náhodného výběru Moraye

Dne 3. září 2026 byla v obou větvích opravena nabídka typů při spawnu
samostatného bounty huntera/piráta, pouze pro `unbound=yes`.
Podnětem byl upstream commit:

https://github.com/markmoxon/elite-source-code-commodore-64/commit/0a76066b9866611c14fd28e9c6338dab616aa9ee

Upstream vložil `LSR A` až za `AND #3`. Takové pořadí však po
`ADC #CYL2` dává jen typy 24–26: Moray by se stále nespawnoval
a z tohoto výběru by navíc vypadl Fer-de-Lance. Proto není patch převzat
doslova: u nás je `LSR A` před maskováním. Carry dostane původní bit 0
a maskovaný posunutý registr dodá offset 0–3. Tři nejnižší vstupní bity
pak vybírají typy 24, 25, 25, 26, 26, 27, 27, 28.

Výběr tedy zahrnuje Cobra Mk III (pirate), Asp Mk II, Python (pirate),
Fer-de-Lance a Moray. Nejde o rovnoměrný výběr pěti typů.
Podmínka `A < 100` pro tuto větev, odbočka na pirátský pack,
odklad dalšího spawnu a případné nahrazení Constrictorem se nemění.

Přidaný bajt překročil dosah existujícího `BEQ fothg` o jeden bajt.
Pro Unbound je proto nahrazen dvojicí `BNE randomPoliceCheck` /
`JMP fothg`. Vzácné setkání stále nastává při stejné hodnotě 136;
vlastní volba Thargoida/Cougara je zachována. Všechny cíle skoků
počítá assembler z návěští.

Celkový náklad je 4 B HICODE: 1 B za `LSR A` a 3 B za delší skok.
Při `unbound=no` zůstávají původní instrukce. Změněny byly pouze
`1-source-files/main-sources/elite-source.asm` a `PROJECT_NOTES.md`
v každé větvi; ostatní rozpracované soubory zůstaly zachovány.

### Ověření opravy spawnu Moraye

V každé větvi úspěšně prošlo všech devět příkazů, celkem 18 buildů:

```text
make variant=tape-pal encrypt=no match=no verify=no unbound=no
make variant=tape-pal encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma85-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
```

Původní PAL tape buildy s `unbound=no` mají bitově shodné herní
bloky i celý TAP proti stavu před touto opravou. Kontroly TAP round-trip
a sektorů GMA86 včetně šifrované varianty prošly. Ověřeny byly vstupy
loaderů ve všech 18 sestaveních a 352 adres patchů vloženého hangáru
v 16 Unbound sestaveních. Velikost hangáru a komunikačních dat se nemění.

Skutečně sestavené instrukce byly prověřeny v emulátoru 6502:

- 36 864 průchodů výběrem: všech 256 vstupních hodnot, obě hodnoty carry
  a čtyři počáteční hodnoty odkladu EV v každém buildu. Moray je dostupný,
  všechny dosavadní typy zůstávají dostupné a odbočka pro pack je zachována.
- 9 216 porovnání původní a nové odbočky na vzácné setkání: všechny
  vstupní hodnoty a obě carry. Cíl větve, registry i příznaky jsou shodné.
- 57 600 porovnání podmínky pro Constrictora před a po úpravě: skutečná
  rutina THERE, platná i chybná galaxie/souřadnice, všechny čtyři stavy
  mise 1, horní bity TP, přítomný/nepřítomný Constrictor a všech 100
  vstupních hodnot pro samostatnou loď. Podmínky mise se nemění.
- Samostatně bylo emulací potvrzeno, že upstream pořadí pro 100 hodnot
  této větve vybírá pouze typy 24–26, zatímco opravené pořadí dává
  četnosti 13, 26, 25, 24 a 12 pro typy 24–28. Jde o četnosti při
  rovnoměrném vyčerpání vstupů 0–99, nikoli o naměřené četnosti ve hře.

Jde o izolované testy sestaveného kódu, nikoli o nové hraní ve VICE.

### Paměť po opravě spawnu Moraye

| Konfigurace | Větev | LOCODE volné | HICODE volné |
|---|---|---:|---:|
| Šest distribučních variant, renderspeedups=yes | main | 95 B | 187 B |
| Šest distribučních variant, renderspeedups=yes | flicker-free | 15 B | 87 B |
| Běžný tape-pal a šifrovaný gma86-pal výše, bez renderspeedups | main | 161 B | 200 B |
| Běžný tape-pal a šifrovaný gma86-pal výše, bez renderspeedups | flicker-free | 81 B | 100 B |

RLE mezery se nemění: fyzicky 39 + 6 + 3 B (48 B), prakticky využitelných
47 B v nesouvislých blocích. Rezervy LOCODE a HICODE zohledňují přísné
nerovnosti koncových kontrol. Commit ani push nebyl proveden.

## Oprava chybějících hvězd po startu z CRT

Dne 3. září 2026 byla v obou větvích opravena inicializace generátoru
náhodných čísel při startu EasyFlash, pouze pro `unbound=yes`.
Při nativním startu z cartridge zůstával čtyřbajtový `RAND` nulový.
Po prvním výletu proto vzniklo všech 12 hvězd ve stejném bodě a jejich
XOR vykreslení se navzájem vyrušilo. Teprve další běh hry a obnovení
hvězd je postupně rozptýlily. U běžného TAP a diskového startu byl
naměřen funkční počáteční stav `00 AA B1 91`.

EasyFlash loader nyní nastaví tento stav bezprostředně před předáním
řízení hře, až po obnovení KERNAL zero page, vektorů a uzavření kanálů.
Mění pouze živý herní `RAND`; záloha KERNAL zero page pro pozdější
načítání a ukládání commander dat zůstává nedotčená.

Adresu generátoru exportuje assembler jako `GAME_RAND` z návěští
`RAND`. Skript `elite-loader-layout.py` ji načítá z aktuálního
`compile.txt`, ověřuje jednoznačný export a uložení všech čtyř bajtů
v zero page mimo CPU port. Loader také obsahuje assemblerové kontroly.
Adresa není ručně zapsaná v loaderu ani v build skriptu.

Změněné soubory v každé větvi:

- `1-source-files/main-sources/elite-easyflash-loader.asm`;
- `1-source-files/main-sources/elite-source.asm` (pouze export symbolu);
- `2-build-files/elite-loader-layout.py`;
- `PROJECT_NOTES.md`.

### Ověření opravy CRT hvězd

V každé větvi prošlo následujících osm buildů před i po změně:
16 ověřovacích buildů a dalších 16 sestavení srovnávacího výchozího stavu.

```text
make variant=easyflash-pal encrypt=no match=no verify=no unbound=no
make variant=easyflash-ntsc encrypt=no match=no verify=no unbound=no
make variant=tape-pal encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-ntsc encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
```

Herní bloky LOCODE, HICODE, COMLOD, ELTA, IANTOK, vložený hangár
a komunikační data jsou bitově shodné s výchozím stavem.
Všechny porovnávané TAP a D64 obrazy zůstaly bitově shodné.
Pro `unbound=no` jsou bitově shodné také celé PAL/NTSC CRT obrazy
a jejich tři loaderové binárky. Kontroly TAP round-trip a sektorové
tabulky GMA86 včetně šifrovaného sestavení prošly.

Ve VICE 3.9 byly samostatně spuštěny PAL a NTSC CRT v obou větvích,
plus kontrolní PAL TAP a GMA86 D64 v obou větvích (osm spuštění).
Před spuštěním hry byl ve všech případech seed `00 AA B1 91`;
po inicializaci hvězd i prvním letovém snímku mělo všech 12 hvězd
různé souřadnice. Čtyři snímky z CRT byly také vizuálně zkontrolovány:
hvězdy jsou viditelné ihned po výletu. Testované CRT před a po
inicializaci zachovaly beze změny zálohu KERNAL zero page i ostatní
živé bajty zero page mimo RAND.

Automatizovaný test ve VICE simuloval odpovědi na úvodní otázky
a klávesu pro výlet přes návratové registry klávesové rutiny.
Nepřepisoval herní kód, RNG ani data hvězd. Nejde o úplný herní
test ani o nové ověření všech operací ukládání a načítání.

Při ručním testování spustit nový CRT od resetu. Načtení starého
VICE snapshotu obnoví starý obsah RAM a obejde opravený boot loader.

### Paměť po opravě CRT hvězd

Oprava přidává pouze 16 B do rezidentního EasyFlash loaderu
(544 -> 560 B), mimo LOCODE, HICODE a RLE mezery.
Herní paměťové rezervy se nezměnily:

| Konfigurace | Větev | LOCODE volné | HICODE volné |
|---|---|---:|---:|
| Testované Unbound CRT/TAP/D64, renderspeedups=yes | main | 95 B | 187 B |
| Testované Unbound CRT/TAP/D64, renderspeedups=yes | flicker-free | 15 B | 87 B |
| Běžný tape-pal a šifrovaný gma86-pal výše, bez renderspeedups | main | 161 B | 200 B |
| Běžný tape-pal a šifrovaný gma86-pal výše, bez renderspeedups | flicker-free | 81 B | 100 B |

RLE mezery zůstávají fyzicky 39 + 6 + 3 B (48 B), prakticky
47 B v nesouvislých blocích. Rezervy zohledňují přísné nerovnosti
assemblerových kontrol. Commit ani push nebyl proveden.

## Samostatné příznaky členů pirátského packu

Dne 3. září 2026 bylo v obou větvích odstraněno dědění příznaků
`NEWB` mezi postupně vytvářenými členy náhodného pirátského packu,
pouze pro `unbound=yes`.

Ve smyčce `mt3` se po vynulování rychlosti nyní stejnou nulovou
hodnotou nastaví také `NEWB`. Každé následující volání `NWSHP`
proto vychází z vlastních výchozích příznaků daného typu. Například
Worm již nepředává příznak trader následující Mambě či Kraitu a sám
nepřebírá pirate bit od předchozího člena.

Wormovy výchozí příznaky `$05` (hostile + trader) zůstávají stejné.
Stále se může v packu objevit, má vlastní pomalejší rozhodování AI
a není záměrně přeřazen mezi piráty. Sdílené rutiny `NWSHP`,
`SFS1`, doprovod Anacondy, trosky, stanice, mise ani tabulka `E%`
se nemění. Výběr typů, společná poloha packu, rozestupy, agresivita,
vybavení a pravidla neutralizace jednotlivých pirátů zůstávají stejné.
Úprava platí pro `randomspawns=yes` i `randomspawns=no`.

Změněné soubory v každé větvi:

- `1-source-files/main-sources/elite-source.asm`;
- `PROJECT_NOTES.md`.

### Ověření příznaků packu

Před úpravou byl v každé větvi sestaven referenční původní TAP:

```text
make variant=tape-pal encrypt=no match=no verify=no unbound=no
```

Po úpravě prošlo v každé větvi těchto sedm buildů:

```text
make variant=tape-pal encrypt=no match=no verify=no unbound=no
make variant=tape-pal encrypt=no randomspawns=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal randomspawns=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-pal encrypt=no randomspawns=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=gma86-pal encrypt=no randomspawns=yes renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=easyflash-pal encrypt=no randomspawns=yes renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
make variant=tape-pal encrypt=no randomspawns=yes renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes whitecockpit=yes fpslimiter=yes inputfix=yes scannercolorfix=no realmissiledamage=yes unbound=yes
```

Celkem 14 buildů po změně a 2 referenční buildy před ní. Kontroly
sestavení, TAP round-trip a automatických sektorů šifrovaného
i nešifrovaného GMA86 prošly. Při `unbound=no` zůstaly porovnávané
herní binárky a výsledný PAL TAP bitově shodné s referencí.

Ve VICE 3.9 byly samostatně spuštěny opravené PAL CRT obou větví.
Pro každou větev prošly čtyři scénáře s reálným provedením smyčky
packu, `NWSHP`, neutralizace a komunikačního hooku:

| Scénář | Typy v pořadí | Výsledné NEWB | Počáteční rychlosti |
|---|---|---|---|
| Worm jako první | Worm, Mamba, Krait | 05, 0C, 0C | 0, 0, 0 |
| Worm po pirátovi | Sidewinder, Worm, Krait | 0C, 05, 0C | 0, 0, 0 |
| Scrambled Fugitive, neutralizace uspěje | Sidewinder, Worm, Krait | 08, 05, 08 | 31, 0, 31 |
| Scrambled Fugitive, neutralizace neuspěje | Worm, Mamba, Krait | 05, 0C, 0C | 0, 0, 0 |

Příznaky byly ověřeny v uložených datových blocích lodí i pracovním
`NEWB`. Kontrolována byla také zachovaná společná hrubá poloha
a hodnota AI a správný výběr prvního skutečně hostile piráta jako
odesílatele zprávy. Ve scénáři s pasivními piráty Worm se svým
hostile + trader není považován za pirátského odesílatele, takže
samotný pack pirátskou zprávu neodešle.

Testovací monitor pouze nastavoval zvolené typy ve výběrovém místě,
hráčovy podmínky a v příslušných scénářích stav RNG před neutralizací.
Spouštěl skutečné binárky a nepřepisoval testovaný herní kód.
Jde o cílené regresní ověření, nikoli úplný průchod hrou.

Při ručním ověření spustit opravený build a nechat vzniknout nový pack.
Starý VICE snapshot obnovuje také původní kód a již existující příznaky
lodí; oprava neprovádí zpětnou úpravu již vytvořených packů.

### Paměť po odstranění dědění

Přibyla jediná dvoubajtová instrukce `STA NEWB` v HICODE.
LOCODE a velikosti rezidentních RLE bloků se nezměnily.

| Konfigurace z výše uvedených buildů | Větev | LOCODE volné | HICODE volné |
|---|---|---:|---:|
| TAP/GMA86/EasyFlash, renderspeedups=yes, randomspawns=yes | main | 95 B | 185 B |
| TAP/GMA86/EasyFlash, renderspeedups=yes, randomspawns=yes | flicker-free | 15 B | 85 B |
| Běžný TAP a šifrovaný GMA86 bez renderspeedups | main | 161 B | 198 B |
| Běžný TAP a šifrovaný GMA86 bez renderspeedups | flicker-free | 81 B | 98 B |
| TAP, renderspeedups=yes, randomspawns=no | main | 95 B | 169 B |
| TAP, renderspeedups=yes, randomspawns=no | flicker-free | 15 B | 69 B |

Pro aktuální TAP s renderspeedups=yes končí LOCODE na `$3FA0`
v main a `$3FF0` ve flicker-free; HICODE na `$CD46`, respektive
`$CDAA`. Rezervy výše zohledňují přísné nerovnosti assembleru.
RLE mezery zůstávají fyzicky 39 + 6 + 3 B (48 B), prakticky
47 B v nesouvislých blocích.

Generovaný sledovaný `3-assembled-output/README.txt` byl po testech
obnoven ze zálohy před buildy. Commit ani push nebyl proveden.

## Koridor omezený na starty ze stanice

Dne 3. září 2026 byl v obou větvích opraven společný vstup `TN6`,
pouze pro `unbound=yes`. Před kontrolou koridoru nyní porovná typ
rodiče `TYPE` s `SST`. Registr X obsahuje typ vypouštěné lodi,
nikoli rodiče. Anaconda a rock hermit proto již nekontrolují koridor
stanice a neodesílají nesouvisející zprávu DOCK OR LEAVE.

Stanice nadále blokuje civilní start při obsazeném koridoru;
policejní Viper může startovat i tehdy. Thargoid vypouští Thargony
přímo přes `SFS1`, takže touto chybou nebyl postižen a jeho cesta
se nemění. Oprava nemění výběr typů, pravděpodobnosti, příznaky
ani vlastní vytvoření potomka. Dřívější oprava dědění `NEWB`
mezi členy pirátského packu je zachována.

Změněny byly `1-source-files/main-sources/elite-source.asm`
a tyto poznámky v obou pracovních kopiích.

### Buildy a regresní ověření koridoru

V každé větvi úspěšně proběhlo těchto šest přesných příkazů:

```text
make variant=tape-pal encrypt=no match=no verify=no unbound=no
make variant=tape-pal encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes realmissiledamage=yes fpslimiter=yes inputfix=yes scannercolorfix=no
make variant=gma86-pal match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes realmissiledamage=yes fpslimiter=yes inputfix=yes scannercolorfix=no
make variant=gma86-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes realmissiledamage=yes fpslimiter=yes inputfix=yes scannercolorfix=no
make variant=easyflash-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes realmissiledamage=yes fpslimiter=yes inputfix=yes scannercolorfix=no
make variant=tape-pal encrypt=no renderspeedups=yes match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes realmissiledamage=yes fpslimiter=yes inputfix=yes scannercolorfix=no
```

Celkem 12 úspěšných buildů. Ve třetím příkazu je `encrypt=no`
záměrně vynecháno: byl ověřen i šifrovaný GMA86 PAL podle AGENTS.md.
Kontroly TAP round-trip a automatických sektorů GMA86 prošly.
Při `unbound=no` zůstaly LOCODE, HICODE, COMLOD, SHIPS, WORDS,
IANTOK a výsledný TAP bitově shodné s archivovanou referencí.

Ve VICE 3.9 prošlo 30 cílených scénářů, samostatně v obou větvích:

- Před opravou: při obsazeném koridoru se zablokuje vypuštění
  Wormu z Anacondy a Mamby z hermita; objeví se varování stanice.
  Thargoid svůj Thargon vypustí již před opravou.
- Po opravě: Anaconda vypustí Worm i Sidewinder a hermit Mambu
  bez ohledu na obsazený koridor. Thargoid vypouští Thargon dál.
- Volný koridor: Anaconda/Worm, hermit/Mamba a Thargoid/Thargon
  projdou bez falešného varování stanice.
- Stanice: Shuttle i Transporter při obsazení čekají, při volném
  koridoru startují; policejní Viper startuje i při obsazení.

Monitor nastavoval kontrolované počáteční podmínky a výsledky RNG
v příslušných rozhodovacích místech skutečného `TACTICS`. Samotné
vypuštění provedl neupravený běžící herní kód. Kontrolovány byly
`TYPE`, rodičovský blok, počty `MANY`, typ potomka v `FRIN`, aktivní
AI, varování, spotřeba Thargonu i jednorázová deaktivace AI hermita.
Žádné pravděpodobnosti ani jiné testovací zásahy nezůstaly ve zdrojích.

Pro obrázky navíc prošly tři scénáře ve flicker-free: 192 snímků
přirozeného letu po vypuštění Wormu, Mamby a Thargonu. Následné
rozhodování AI již nebylo vynucováno. Snímky jsou přímo z VICE,
bez úprav obrazových dat. U Thargoidu byl po 128 snímcích přesunut
pozorovatel společným posunem všech objektů, aby byly obě lodě
vidět; jejich vzájemné polohy, rychlosti a AI zůstaly zachovány.
Jde o cílené regresní testy, nikoli úplný průchod hrou.

Skripty, build logy, výpisy assembleru, výpisy RAM, výsledky a PNG
jsou v ignorovaném adresáři `3-assembled-output/corridor-parent-check/`
pracovní kopie main. Vybrané obrázky a jejich přehled jsou v
`3-assembled-output/corridor-parent-check/screenshots/index.html`.

### Paměť po opravě koridoru

Přibylo 6 B v LOCODE; HICODE a velikosti RLE bloků se nezměnily.

| Testovaná konfigurace | Větev | LOCODE volné | HICODE volné |
|---|---|---:|---:|
| TAP/GMA86/EasyFlash, renderspeedups=yes | main | 89 B | 185 B |
| TAP/GMA86/EasyFlash, renderspeedups=yes | flicker-free | 9 B | 85 B |
| Běžný TAP a šifrovaný GMA86 bez renderspeedups | main | 155 B | 198 B |
| Běžný TAP a šifrovaný GMA86 bez renderspeedups | flicker-free | 75 B | 98 B |

Pro konfiguraci s renderspeedups=yes: R% = $3FA6 / $3FF6,
F% = $CD46 / $CDAA (main / flicker-free). Rezervy zohledňují
přísné nerovnosti assemblerových kontrol. RLE mezery mají stále
39 + 6 + 3 B, tedy fyzicky 48 B, prakticky 47 B v oddělených blocích.

Generovaný sledovaný `3-assembled-output/README.txt` byl obnoven
ze zálohy před testy. Pracovní kopie obsahují uvedené změny a dřívější
necommitovanou opravu packu. Commit ani push nebyl proveden.

## 2026-09-04 — Neblokující stránkování údajů o planetách

V obou větvích je nezávislá volba `planetdatafix=yes`, mapovaná
Makefilem na `_PLANET_DATA_FIX`; výchozí stav je vypnuto. Funguje
s původní hrou i plným Unbound, včetně renderspeedups, fpslimiter a inputfix.

### Konečné chování

Pouze přeplněná první stránka čeká na uvolnění otevírací klávesy,
nový stisk libovolné herní klávesy a její uvolnění. Poté zobrazí pokračování.
Poslední stránka zůstává otevřená jako běžná obrazovka 6 (QQ11 = 1):
žádné automatické zavírání, návrat do kokpitu ani na Status.
Krátké popisy rovnou používají původní obsluhu obrazovky 6.

Původní pokus s PAUSE2/RDKEY v clss blokoval hlavní herní smyčku a byl
nahrazen. Nyní TT25 dokončí render bez čekání; clss odloží přetékající
znaky do TAP% (256bajtová staging oblast save/load před XX21). Počet a řádek
používají dosud nevyužité bajty XP/YP, bez posunu workspace. ASSERT ověřuje
velikost oblasti a runtime kontrola brání přetečení indexu do blueprintů.
Save/load není v čekající stránce dostupný; po odstránkování buffer není potřeba.

PlanetDataFrame je volán z TT102 jen pro dočasné stavy QQ11 = 17..19.
Každý snímek se vrací do hlavní smyčky a volá TT107 pro živý hyperspace
countdown. Dočasné letové zprávy MESS během čekání nemažou první stránku;
poslední stránka má opět zcela běžné chování. Stisk i uvolnění potvrzovací
klávesy se spotřebují, takže držená 6 okamžitě neotevře popis znovu.

LOCODE nenarostl. Veškerý nový kód je v HICODE; jen při zapnuté volbě se
vynechá původní assembly noise před log a nepoužité kopie pixelových masek
DTWOS/TWOS2/TWFL/TWFR. Čistý přírůstek HICODE v GMA/TAP/EasyFlash je 56 B
proti vypnuté volbě. Při vypnuté volbě se původní kód i nepoužívaná data zachovají.

### Ověření ve VICE 3.9

Samostatné instance načetly finální PAL TAP každé větve s plným Unbound.
Adresy rutin a workspace byly získány z aktuálního compile.txt.

- Ceused, galaxie 5, systém 98 (indexováno od nuly), souřadnice (202, 56):
  jako vzdálený systém přetéká slovem `shyness.`.
- Test probíhal v inicializované hře po odletu, mezi skutečnými průchody
  TT102/hlavní smyčky. Klávesy byly řízeny monitorem na hranici vstupu.
- Na první stránce proběhlo 10 herních snímků; na druhé 12. MCNT se změnil
  o odpovídající počet, pohyb lodí pokračoval a QQ22+1 klesl 99 -> 97 -> 94.
- Držená 6 neodstránkuje před uvolněním. Po uvolnění zůstává QQ11 = 1,
  další obyčejná klávesa stránku nezavře a 8 normálně otevře Status.
- Ve stanici pokračování rovněž zůstane běžnou obrazovkou 6.
  Edreered, jehož popis se vejde, nemá dodatečné čekání.
- Regresní průchod skutečným TT24/TT25 přes všech 2048 procedurálních planet
  s nenulovou vzdáleností našel jen tyto pokračující popisy:
  Ceused (G5/98, 8 znaků), Maesqua (G5/113, 12),
  Tiregees (G7/225, 9), Laorlaza (G8/98, 9).
  Nejdelší pokračování tedy využívá 12 bajtů. Capture kód se při následném
  zjednodušení chování poslední stránky nezměnil.
- Ověřeno také 25 dosažitelných systémových override položek RUPLA/RUGAL
  (aktivní Constrictor, nulová vzdálenost, ve stanici); žádná nepřetekla.
  Poslední položka tabulky patří nedosažitelné galaxii 16.

### Přesné testované buildy

Následující příkazy byly provedeny samostatně v main i flicker-free;
všech deset konfigurací v každé větvi prošlo:

```powershell
$common = @('match=no', 'verify=no', 'laserbeam=line', 'font=zx',
  'dials=new', 'sights=cross', 'warpjunk=yes', 'iffunit=yes',
  'randomspawns=yes', 'whitecockpit=yes', 'unbound=yes',
  'realmissiledamage=yes', 'fpslimiter=yes', 'inputfix=yes')

make variant=tape-pal encrypt=no planetdatafix=no renderspeedups=yes @common
make variant=tape-pal encrypt=no match=no verify=no planetdatafix=yes
make variant=tape-pal encrypt=no planetdatafix=yes renderspeedups=no @common
make variant=gma86-pal planetdatafix=yes renderspeedups=no @common
make variant=tape-ntsc encrypt=no planetdatafix=yes renderspeedups=yes @common
make variant=gma85-ntsc planetdatafix=yes renderspeedups=yes @common
make variant=easyflash-pal encrypt=no planetdatafix=yes renderspeedups=yes @common
make variant=easyflash-ntsc encrypt=no planetdatafix=yes renderspeedups=yes @common
make variant=gma86-pal planetdatafix=yes renderspeedups=yes @common
make variant=tape-pal encrypt=no planetdatafix=yes renderspeedups=yes @common
```

Použito `2-build-files/make.exe`, Python 3.13 a další proměnné make
`BEEBASM=E:/Development/Elite-C64/beebasm/beebasm.exe`,
`C1541=G:/Emulace/C64/GTK3VICE-3.9-win64/bin/c1541.exe`.
TAP round-trip kontroly prošly; šifrované GMA86 disky prošly oběma průchody
i kontrolou automatické sektorové tabulky. Finální distribuční TAP/D64/CRT
obsahují plný Unbound a novou opravu.

Při planetdatafix=no se všech 34 binárek v každé větvi shoduje s uloženým
stavem před opravou po normalizaci samostatné změny titulní verze
v0.73 -> v0.80. Bez normalizace se liší pouze tyto dva znaky v ELTD a jeho
navazujících kontejnerech; změna titulní verze byla zachována, nikoli vrácena.

### Aktuální paměť

Skutečně přidatelné bajty respektují striktní limity R% < $4000 a F% < $CE00:

| Konfigurace s planetdatafix=yes | Větev | R% | F% | LOCODE volné | HICODE volné |
|---|---|---|---|---:|---:|
| Plný Unbound, renderspeedups=yes, TAP/GMA/EasyFlash PAL/NTSC | main | $3FA6 | $CD7E | 89 B | 129 B |
| Plný Unbound, renderspeedups=yes, TAP/GMA/EasyFlash PAL/NTSC | flicker-free | $3FF6 | $CDE2 | 9 B | 29 B |
| Běžný Unbound TAP/GMA86 PAL, renderspeedups=no | main | $3F64 | $CD71 | 155 B | 142 B |
| Běžný Unbound TAP/GMA86 PAL, renderspeedups=no | flicker-free | $3FB4 | $CDD5 | 75 B | 42 B |

K celé volbě patří změny Makefile, elite-source.asm, README.md a tohoto
souboru v obou větvích. V této navazující opravě se Makefile již neměnil.
Generovaný elite-build-options.asm nebyl upravován ručně. Uživatelská změna
build_tape.bat ve flicker-free zůstala zachována. Pracovní kopie nejsou čisté;
commit ani push nebyl proveden.
