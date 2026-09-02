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
6. Změny provádějte přímo v příslušných pracovních kopiích na disku.
   ZIPy vytvářejte pouze na výslovnou žádost uživatele.

Aktuální zdrojový kód a výstup skutečného buildu mají přednost před údaji
v chatu nebo v PROJECT_NOTES.md. Pokud se poznámky rozcházejí s kódem,
ověřte stav a poznámky aktualizujte.

## Herní dokumentace

Doplňující technická dokumentace je uložena ve větvi `main` v adresáři
`game-docs/`:

- `game-docs/elite-unbound-save-map.src`
  - kompletní mapa commander save souboru;
  - význam jednotlivých bajtů;
  - formát `TP`, `cmdr_type`, checksumů a ostatních uložených hodnot.

- `game-docs/elite-unbound-missions.md`
  - stavy a přechody všech misí;
  - přesné podmínky jejich spuštění;
  - cílové systémy, souřadnice, odměny a spawn podmínky;
  - vysvětlení, které údaje jsou uložené v save a které jsou pevně
    zakódované ve hře.

Před analýzou nebo změnou save formátu, commander dat či misí nejdříve
prostudujte tyto dokumenty. Aktuální zdrojový kód a skutečný build mají při
případném rozporu přednost.

Při změně následujících oblastí aktualizujte také odpovídající dokument:

- struktura commander save nebo checksumy;
- význam bajtu `TP`;
- přidání nebo změna typu hráčovy lodi;
- mission triggery, stavy, cílové systémy nebo odměny;
- Constrictor nebo Thargoid mission spawny;
- Trumble nabídka a její podmínky.

Dokumenty jsou vedeny ve větvi `main`. Při práci ve `flicker-free` je
používejte jako referenci, ale vždy ověřte, že se příslušná implementace mezi
větvemi nerozešla. Nekopírujte je do `flicker-free`, pokud to uživatel výslovně
nepožaduje.

## Zásady úprav zdrojového kódu

- Zachovávejte styl dokumentovaných BeebAsm zdrojů a srozumitelné komentáře.
- Změny pro Elite: Unbound uzavírejte do IF _UNBOUND / ELSE / ENDIF, pokud
  nemají záměrně platit pro původní hru.
- Reálné poškození AI lodí raketami je nezávislá volba
  realmissiledamage=yes a používá IF _REAL_MISSILE_DAMAGE.
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

    make variant=tape-pal encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes realmissiledamage=yes fpslimiter=yes inputfix=yes

Spouštějte ji samostatně v main i flicker-free.

Po změně, která může ovlivnit velikost kódu, checksumy, GMA soubory, loader
nebo tvorbu disku, sestavte také šifrovaný GMA86 PAL disk. Parametr encrypt=no
je zde úmyslně vynechán:

    make variant=gma86-pal match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes realmissiledamage=yes fpslimiter=yes inputfix=yes

Podle rozsahu změny otestujte také:

    make variant=tape-ntsc encrypt=no match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes realmissiledamage=yes fpslimiter=yes inputfix=yes

    make variant=gma85-ntsc match=no verify=no laserbeam=line font=zx dials=new sights=cross warpjunk=yes iffunit=yes randomspawns=yes whitecockpit=yes unbound=yes realmissiledamage=yes fpslimiter=yes inputfix=yes

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

## PDF manuály

Při generování PDF manuálu vytvořte vždy samostatnou anglickou a českou
verzi přímo v adresáři `manual/`:

- `manual/elite-unbound-manual-en.pdf`;
- `manual/elite-unbound-manual-cz.pdf`.

Manuály musí vycházet z aktuální anglické a české verze webu ve větvi
`flicker-free`. Na začátku musí obsahovat také úvodní vyprávění, představení
Elite: Unbound a seznam funkcí; vlastní návod následuje až za nimi.

Na úplný konec obou manuálů vždy připojte aktuální obsah stránky
`docs/credits.html` pro anglickou verzi a `docs/cs/credits.html` pro českou
verzi. Sekce musí mít ve své jazykové verzi nadpis `Credits & Foundations`,
respektive `Poděkování a základy projektu`, a musí být zahrnuta v obsahu,
interních odkazech a PDF záložkách. Externí odkazy v poděkování zachovejte
aktivní.

Používejte bílé pozadí a černý text vhodný pro tisk. Hlavní nadpis na první
straně musí být přesně `Elite: Unbound`, bez podtitulu a bez barevného
podkladu nebo zvýraznění. Zachovejte obrázky, popisky, pořadí sekcí a
jazykovou verzi zdrojového webu.

Popisky obrázků umístěte přímo pod příslušný obrázek a vodorovně je
vystřeďte podle obrázku, nikoli podle stránky.

Vytvořte obsah s čísly stran, klikacími interními odkazy a PDF záložkami.
Externí odkazy musí zůstat aktivní. Po každém vytvoření nebo úpravě
vykreslete obě PDF a vizuálně zkontrolujte všechny stránky, zejména titulní
stranu, zalomení textu, obrázky, popisky a případné přetečení. Ověřte také
počet stran, textovou vrstvu, odkazy a záložky. Vypište také skutečné názvy
PDF záložek a ověřte, že žádný název není zdvojený nebo jinak poškozený.
Dočasné rendery a pomocné soubory po kontrole odstraňte.

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
