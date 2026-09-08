# TapD64Transfer

Samostatný projekt pro **PAL C64 + Datasette + Commodore 1541 na adrese 8**.
Z existujícího D64 vytvoří jeden zaváděcí TAP, který přepíše cílovou disketu
a zpětným čtením porovná všech **683 sektorů × 256 bajtů**.

## Hotový TAP

`output/elite-commodore-64-flicker-free-gma86-pal-transfer.tap`

Sestavený TAP má 1 146 026 bajtů. Obsahuje standardní zaváděcí program
`TAPD64` a 35 turbo bloků. Není nutné přepínat více TAPů ani mít celý D64
současně v paměti C64. Úvodní obrazovka zapisovače nese název `ELITE: UNBOUND`.

Zdroj tohoto sestavení:

`../5-compiled-game-disks/elite-commodore-64-flicker-free-gma86-pal.d64`

SHA-256 zdrojového D64 i výsledného D64 z testu ve VICE:

```text
2d46cc6dd35a90cfc2f13179b820cbcdc7464e3a2efa53ead19a308386ec3b90
```

## Použití

1. Připojte 1541 jako zařízení **8** a vložte disketu určenou k přepsání.
2. Přetočte pásku/TAP na začátek a spusťte `SHIFT+RUN/STOP`, případně
   `LOAD"",1` a poté `RUN`.
3. Na obrazovce `ERASE ENTIRE DISK IN DRIVE 8?` stiskněte **Y**.
   Tím povolíte úplné naformátování a přepsání vložené diskety.
4. Nechte Datasette v režimu PLAY. Program sám zastavuje motor při zápisu
   na disketu a spouští ho pro další blok. Během turbo načítání se obraz
   vypne a mění se barva okraje; při zápisu je vidět číslo stopy.
5. Úspěch je oznámen pouze jako `DONE - ALL 683 SECTORS VERIFIED.`
   Zastavte pásku, resetujte C64 a načtěte hru z diskety obvyklým způsobem.

Při chybě program vypíše druh chyby a stopu/sektor a zastaví se. Dvoumístný
kód pod hlášením je poslední přečtený stav DOS; u chyby CRC nebo porovnání
může být `00`. Chybový výsledek není dokončená kopie. Po odstranění příčiny
resetujte C64 a opakujte přenos od začátku. Při chybějících páskových pulzech
zůstává čtečka čekat; přerušení se provádí resetem.

TAP přehrávač či jeho adaptér musí respektovat řízení motoru Datasette.
Pro obyčejné nepřerušované přehrávání zvuku do vstupu bez řízení motoru tento
postup určen není. Ověřeno ve VICE; test na skutečném C64/1541 zatím neproběhl.

## Identická kopie

Program nekopíruje soubory přes DOS, ale zapisuje konkrétní fyzická čísla
stop a sektorů pomocí `U2`. Každý sektor okamžitě přečte přes `U1` a porovná
všech 256 bajtů. Zachová proto rozmístění souborů potřebné pro GMA fast loader,
adresář, BAM i obsah nepoužitých sektorů.

Generátor vynechává pouze sektory, které ve zdroji skutečně obsahují 256 nul.
Nepředpokládá, že volný sektor je nulový. Nulové sektory zapisovač vygeneruje
a také zapíše a ověří. Aktuální D64 má 462 nulových sektorů; páskový náklad
obsahuje 56 576 bajtů ostatních sektorů a 683 příznakových bajtů.

Pořadí stop je 1–17, 19–35, 18. Adresář a BAM se tedy zapisují nakonec.
Před formátováním je převzato ID diskety z BAM zdroje. Po dokončení se provede
pouze inicializace DOS (`I`), nikoli `VALIDATE`, která by obsah mohla změnit.

Identita znamená všech 174 848 datových bajtů standardního D64. D64 neobsahuje
mezery mezi sektory, přesné magnetické časování ani další vlastnosti GCR.
Podporován je 35stopý D64 bez tabulky chyb a s dvoubajtovým tisknutelným ASCII
ID (bez čárky/dvojtečky). Jiné formáty generátor výslovně odmítne.

## Sestavení

Požadavky: Python 3.10+ a BeebAsm. Nejsou potřeba balíčky z pip.
Ve Windows spusťte `build.bat`; lze jej zavolat i z jiného pracovního
adresáře. Vyhledá Python 3.10+ a předá všechny argumenty generátoru:

```bat
build.bat
build.bat d64="C:\obrazy\moje hra.d64" tapename="MOJE HRA" label="MOJE HRA - DISK INSTALLER" tapfile="Moje hra instalace.tap"
```

| Parametr | Význam | Výchozí hodnota |
|---|---|---|
| `d64=` | Cesta a název vstupního D64; relativní cesta se vztahuje k adresáři, odkud voláte BAT | Sestavený Elite flicker-free GMA86 PAL D64 v sousedním adresáři |
| `tapename=` | Název úvodního programu uvnitř kazety, nejvýše **16 znaků** | `TAPD64` |
| `label=` | První řádek obrazovky zapisovače, nejvýše **25 znaků** | `ELITE: UNBOUND` |
| `tapfile=` | Jméno výsledného TAP souboru uvnitř `output/`, bez cesty | Název vstupního D64 + `-transfer.tap` |

Parametry mohou být v libovolném pořadí. Hodnoty s mezerami uzavřete do
uvozovek. Oba názvy se převedou na velká písmena bez diakritiky a přebytečné
znaky se automaticky oříznou. Nepodporované znaky se nahradí otazníkem,
bílé znaky mezerou; prázdný název je chyba. Výsledné hodnoty se vypíší
a uloží do `manifest.json`. Titulek se generuje do `output/title.asm` jako
číselná data, takže ani uvozovky v názvu nemění assemblerový kód.

`tapename` mění jméno programu při `FOUND`. Jméno TAP souboru na PC nastavuje
`tapfile`; zachová velikost písmen a mezery a není omezeno na 16 znaků.
Chybějící přípona `.tap` se automaticky doplní. Například `tapfile="Instalace"`
vytvoří `output/Instalace.tap`. Bez `tapfile` se jméno odvozuje ze vstupního
D64: `moje hra.d64` → `moje hra-transfer.tap`. Skutečný název je v položce
`tap` souboru `manifest.json`.

Případně spusťte Python přímo v tomto adresáři:

```powershell
python build.py
```

Generátor čte aktuální existující D64; samotnou hru znovu nesestavuje.
BeebAsm hledá v PATH a potom v sousedním `../../beebasm/beebasm.exe`.
Lze zadat jiný obraz a cestu assembleru:

```powershell
python build.py d64="C:\obrazy\hra.d64" tapename="HRA" label="DISK TRANSFER" --beebasm "C:\nastroje\beebasm.exe"
```

Zachována je i původní poziční cesta k D64 a přepínače `--d64`, `--tapename`,
`--label` a `--tapfile`. Při vynechání parametrů vznikne dosavadní verze pro Elite.

Výstupní adresář `output/` obsahuje TAP, zaváděcí `transfer.prg`, aktuálně
generované `layout.asm` a `title.asm`, assemblerový výpis `compile.txt` a `manifest.json`
s hashi. Tyto generované soubory jsou ignorované Gitem. Každé sestavení
ověřuje ROM i turbo pulsy a úplnou rekonstrukci zdrojového obrazu.

## Testování

Nejprve spusťte `build.bat` (nebo `python build.py`), aby vznikl aktuální TAP a assemblerový výpis potřebný pro monitor VICE.

```powershell
python -m unittest -v test_transfer
python test_vice.py --timeout 180
python test_vice.py --timeout 180 --fault crc
```

Pro pomalejší počítač zvyšte `--timeout` (výchozí hodnota je 600 sekund).
Přepínač `--vice` umožňuje zadat jinou cestu k VICE 3.9 `x64sc`.

Test VICE načítá skutečné pulsy celého TAPu, používá emulovanou 1541
s true drive emulation a vypnutými virtuálními náhradami zařízení 1 a 8.
Povoluje potvrzení pouze na novém testovacím obrazu vytvořeném v `output/`.
Obrazy `vice-test.d64` a `vice-crc-test.d64` se při příslušném testu přepisují;
test k fyzické mechanice nepřistupuje. Běžný test vyžaduje shodu SHA-256 celého
výsledného D64. Negativní test poškodí první načtený blok v RAM a vyžaduje
chybu CRC ještě před prvním sektorovým zápisem.

Ověření 7. září 2026: všech šest hostitelských testů prošlo; úplný přenos
ve VICE 3.9 skončil shodou SHA-256; poškození bloku bylo zachyceno před zápisem.
Ověření parametrů 8. září 2026: 12 hostitelských testů, sestavení přes BAT
z jiného adresáře, relativní cesta s mezerami, přesné oříznutí názvů na 16/25
znaků a bezpečné vložení titulku s interpunkcí. Kompletní TAP s vlastními
názvy prošel ve VICE porovnáním všech 683 sektorů a SHA-256. Druhý řádek
zapisovače nyní zní `TAP D64 TRANSFER - DRIVE 8`.
Po doplnění `tapfile` prošlo všech 14 testů a build přes BAT s vlastním názvem
s mezerami a automatickou příponou. SHA-256 potvrdil nezměněný obsah TAPu.
Testovací obrazy a protokoly vznikají v ignorovaném `output/`. Po kontrole byly odstraněny spolu s `__pycache__`, `layout.asm`, `title.asm` a `compile.txt`; ponechány jsou pouze hotový TAP, PRG a `manifest.json`. Příští sestavení potřebné pomocné soubory znovu vytvoří.

## Soubory a paměť

- `build.py`: načtení D64, bezeztrátové balení po stopách, tabulky délky/CRC,
  sestavení a kontrola TAPu.
- `build.bat`: sestavení ve Windows s vyhledáním Pythonu a předáním argumentů.
- `transfer.asm`: samostatný zapisovač; nemění herní ASM ani GMA loader.
- `tape_codec.py`: ROM/turbo kodér a kontrolní dekodéry převzaté z projektového
  `2-build-files/elite-tape.py`, bez jeho herního vstupního programu.
- `test_transfer.py`, `test_vice.py`: hostitelské a integrační testy.

S výchozím titulkem program zabírá `$0801–$0EB2`; do bufferu od `$4000`
zbývá 12 621 bajtů. S nejdelším 25znakovým titulkem končí na `$0EBD`
a zbývá 12 610 bajtů. Komprimovaná stopa používá `$4000–$5FFF` (nejvýše 5 397 bajtů)
a sektorový buffer `$6000–$60FF`. Použité pracovní ukazatele jsou `$F9–$FE`.
Počáteční BASIC SYS míří na rezervovaný vstup `$0810`; meze a tabulky hlídá
assembler. Délky a CRC stop vznikají vždy z aktuálního vstupního D64.

Páskové bloky mají kontrolu ID stopy, cílové adresy, přesné délky a XOR;
poté se kontroluje CRC-16/CCITT-FALSE proti rezidentní tabulce. CRC běží až
po zastavení motoru, aby neovlivňovalo časování turbo pulzů.

Reference: [Commodore 1541-II User's Guide, přímý přístup U1/U2](https://oldcrap.org/wp-content/uploads/2024/02/commodore-1541-ii-users-guide.pdf),
[VICE monitor](https://vice-emu.sourceforge.io/vice_12.html).
