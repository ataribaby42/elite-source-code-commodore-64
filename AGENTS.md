# Elite C64 / Elite: Unbound – pokyny pro práci

## Rozsah projektu

Tento repozitář je fork dokumentovaných zdrojových kódů Commodore 64 Elite
od Marka Moxona a obsahuje rozšíření Elite: Unbound.

GitHub:

    https://github.com/ataribaby42/elite-source-code-commodore-64/

Reference:

    https://elite.bbcelite.com/
    https://github.com/markmoxon/elite-source-code-commodore-64/
    https://github.com/markmoxon/elite-a-source-code-bbc-micro/

Projekt se udržuje ve dvou samostatných pracovních kopiích:

| Větev | Pracovní kopie ve Windows |
|---|---|
| main | E:\Development\Elite-C64\elite-source-code-commodore-64 |
| flicker-free | E:\Development\Elite-C64\elite-source-code-commodore-64-flicker-free |

Tento soubor je záměrně stejný v obou větvích. Skutečnou větev vždy ověř
příkazem git branch --show-current a podle cesty pracovní kopie.

## Povinný postup před úpravou

1. Nejdřív určete, zda se změna týká větve main, flicker-free, nebo obou.
   Pokud to ze zadání není jednoznačné, zeptejte se uživatele před editací.
2. Spusťte git status --short --branch v každé dotčené pracovní kopii.
3. Zachovejte všechny existující uživatelské úpravy. Nesouvisející změny
   neupravujte, nemažte ani nevracejte.
4. Při změně pro obě větve ji implementujte a otestujte samostatně v každé
   pracovní kopii. Nekopírujte celý ASM soubor přes druhou větev; flicker-free
   obsahuje vlastní odlišnosti.
5. Neprovádějte commit ani push bez výslovného pokynu uživatele.
6. Pokud uživatel neurčí jinak, předávejte výsledné soubory v jednom ZIPu
   s oddělenými adresáři main/ a flicker-free/.

Aktuální zdrojový kód a výstup skutečného buildu mají přednost před údaji
v chatu nebo v PROJECT_NOTES.md. Pokud se poznámky rozcházejí s kódem,
ověřte stav a poznámky aktualizujte.

## Zásady úprav zdrojového kódu

- Zachovávejte styl dokumentovaných BeebAsm zdrojů a srozumitelné komentáře.
- Změny pro Elite: Unbound uzavírejte do IF _UNBOUND / ELSE / ENDIF, pokud
  nemají záměrně platit pro původní hru.
- Frame limiter a paralelní vstup jsou nezávislé volby. Jejich kód uzavírejte
  do IF _FPS_LIMITER a IF _INPUT_FIX, nikoli do IF _UNBOUND.
- Oprava původní palety skeneru je volba scannercolorfix=yes. Používejte
  IF _SCANNER_COLOR_FIX pouze ve větvi, kde je _WHITE_COCKPIT vypnutý;
  whitecockpit=yes má vždy přednost a používá vlastní opravenou paletu.
- Při unbound=no musí zůstat původní chování. Pokud je to součástí zadání,
  ověřte také shodu původních binárek.
- Upřednostňujte hodnoty odvozené z aktuálního buildu před pevnými adresami,
  velikostmi a sektory.
- Automaticky generovaný soubor
  1-source-files/main-sources/elite-build-options.asm neupravujte ručně.
- Při přesunu nebo přidání kódu sledujte zvlášť limity LOCODE a HICODE.
  Jejich volnou paměť nelze sčítat.
- Flicker-free LOCODE je při běžné konfiguraci téměř plný. Nové větší rutiny
  umisťujte přednostně do HICODE nebo tam přesuňte vhodný existující kód.
- Po významné změně aktualizujte PROJECT_NOTES.md v obou větvích.

## Běžný testovací build

Nejčastěji používaná konfigurace:

    make variant=tape-pal encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes fpslimiter=yes inputfix=yes

Spouštějte ji samostatně v main i flicker-free.

Po změně, která může ovlivnit velikost kódu, checksumy, GMA soubory, loader
nebo tvorbu disku, sestavte také šifrovaný GMA86 PAL disk. Parametr encrypt=no
je zde úmyslně vynechán:

    make variant=gma86-pal match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes fpslimiter=yes inputfix=yes

Podle rozsahu změny otestujte také:

    make variant=tape-ntsc encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes fpslimiter=yes inputfix=yes

    make variant=gma85-ntsc match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes fpslimiter=yes inputfix=yes

Build vyžaduje BeebAsm, Python 3 a pro diskové varianty c1541 z VICE.
Výstupy jsou v adresářích 3-assembled-output,
5-compiled-game-disks a 5-compiled-game-tapes.

## GMA fast loader

Automatická tabulka sektorů se týká pouze varianty gma86-pal.

Makefile provede dva průchody:

1. vytvoří první D64;
2. 2-build-files/elite-gma-sectors.py přečte skutečné pozice GMA2 až GMA6;
3. opraví tabulku v 3-assembled-output/gma1.unprot.bin;
4. vytvoří finální D64;
5. ověří tabulku v binárce i v GMA1 uloženém na disku.

Nevracejte pevné podmínky sektorů podle UNBOUND, typu laseru ani velikosti
GMA5. Při neshodě musí build skončit chybou.

GMA85 NTSC nepoužívá stejnou tabulku trackSector. Tape varianty nepoužívají
D64 ani GMA fast loader, takže se jich sektorový problém netýká.

Varování c1541 o chybějící opencbm.dll je při práci pouze s D64 neškodné,
pokud následné vytvoření obrazu a ověření sektorové tabulky úspěšně projde.

## Checksumy a šifrování

2-build-files/elite-checksum.py čte z aktuálního
3-assembled-output/compile.txt adresy B%, G%, NA2%, W%, X% a U%. Běžný posun
herních rutin proto nevyžaduje ruční změnu adres v tomto skriptu.

Skript znovu podrobně prověřte, pokud se mění:

- rozložení GMA1 a pozice patchů diskové ochrany;
- formát commander dat nebo algoritmus jeho checksumu;
- PRG hlavičky, padding nebo skladba ELTA až ELTK;
- názvy či formát symbolů zapisovaných do compile.txt;
- hranice nebo algoritmus šifrování LOCODE, HICODE či COMLOD.

## Paměťové limity

Assembler hlídá dva nezávislé konce:

- R% < $4000 pro LOCODE;
- F% < $CE00 pro HICODE.

Protože jde o striktní nerovnost, při rozdílu 16 bajtů lze skutečně přidat
nejvýše 15 bajtů. Poslední naměřené hodnoty jsou v PROJECT_NOTES.md a po
každé větší úpravě je nutné je znovu odečíst z compile.txt.

## Důležitá místa

- Hlavní hra:
  1-source-files/main-sources/elite-source.asm
- GMA86 loader a provizorní tabulka:
  1-source-files/main-sources/elite-gma1.asm
- Automatické sektory:
  2-build-files/elite-gma-sectors.py
- Checksumy a šifrování:
  2-build-files/elite-checksum.py
- Build:
  Makefile
- Verze na úvodní obrazovce:
  návěští .TitleScreenVersion v elite-source.asm

Čísla řádků neukládejte jako autoritativní údaj, protože se po úpravách mění.

## Předání výsledku

V závěru vždy uveďte:

- které větve byly změněny;
- které soubory byly změněny;
- přesné provedené buildy a jejich výsledek;
- aktuální volnou paměť, pokud změna ovlivnila ASM kód;
- zda je pracovní kopie čistá nebo jaké změny zůstávají;
- že commit ani push nebyl proveden, pokud si je uživatel výslovně nevyžádal.
