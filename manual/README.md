# PDF manuály: schválený vzhled a postup generování

Uživatel dne 6. září 2026 výslovně schválil aktuální vzhled obou PDF
a požádal o jeho zachování při příštím generování. Před dalším exportem
si přečtěte tento soubor a prohlédněte stávající PDF jako vizuální vzor.
Obsah aktualizujte z webu; vzhled bez požadavku uživatele neměňte.

## Zdroje a výstupy

Používejte aktuální soubory ve větvi `flicker-free`, včetně dosud
necommitnutých úprav:

- EN: `docs/index.html`, `docs/instruction-manual.html`, `docs/credits.html`.
- CZ: `docs/cs/index.html`, `docs/cs/instruction-manual.html`,
  `docs/cs/credits.html`.
- Obrázky: skutečné soubory odkazované z příslušných HTML stránek.

Výstupy ukládejte přímo do této složky:

- `elite-unbound-manual-en.pdf`;
- `elite-unbound-manual-cz.pdf`.

Schválené vydání mělo v každém jazyce 17 stran, 25 výskytů obrázků
a 18 PDF záložek. Jde o kontrolní údaje tohoto vydání, nikoli pevné limity
pro budoucí obsah. Nesnažte se udržet počet stran zmenšováním písma.

## Barvy a základní sazba

- A4 na výšku: přibližně 595,276 × 841,890 bodu.
- Bílé pozadí; **veškerý sázený text je čistě černý (`#000000`)**.
- Černé jsou také nadpisy, popisky, odrážky, obsah, čísla stran a odkazy.
  Nevracejte oranžové či jiné barevné odkazy ze starších PDF ani barevnost webu.
- Zachovejte původní barvy uvnitř obrázků. Obrázky nepřevádějte do odstínů šedi.
- Levý a pravý okraj: 48 bodů; horní a dolní okraj textového rámce: 45 bodů.
- Šířka textového rámce: přibližně 499,276 bodu, bez vnitřního odsazení.
- Odstavce zarovnávejte vlevo, bez odsazení prvního řádku.
- Žádné barevné panely, podklady nadpisů, rámečky kolem upozornění ani záhlaví.

## Písmo a mezery

Schválené PDF bylo vytvořeno pomocí ReportLabu s vloženými TrueType fonty
Arial z Windows: `arial.ttf`, `arialbd.ttf`, `ariali.ttf`, `arialbi.ttf`.
Zachovejte českou diakritiku a správné mapování tučného písma a kurzívy.
Všechny rozměry v následující tabulce jsou v typografických bodech (pt).

| Prvek | Písmo | Velikost | Řádkování | Mezery |
|---|---|---:|---:|---|
| Titulek první strany | Arial Bold | 26 | 32 | před 0, za 27 |
| Hlavní nadpisy, Contents / Obsah | Arial Bold | 16 | 20 | před 15, za 10 |
| Nadpisy sekcí vlastního návodu | Arial Bold | 13 | 17 | před 15, za 10 |
| Běžný text | Arial | 10 | 14 | za odstavcem 8 |
| Zvýraznění / kurzíva | Arial Bold / Italic | 10 | 14 | podle odstavce |
| Odrážkové seznamy | Arial | 10 | 14 | za položkou 4, za seznamem 4 |
| Popisky obrázků | Arial | 8,5 | 11 | před 4, za 9 |
| Obsah: hlavní položky | Arial Bold | 10 | 15 | před 4 |
| Obsah: podřízené položky | Arial | 9,5 | 14 | před 2, levé odsazení 15 |
| Patička a číslo stránky | Arial | 7 | — | účaří 25 bodů od dolního okraje |

Položky seznamu mají levé odsazení 12 bodů; odrážka leží na levém okraji
textového rámce. Klávesy a krátké úseky HTML `code` se v PDF sází tučným
Arialem, bez barevného pozadí. Běžné odkazy jsou černé a podtržené.

Vlevo v patičce je `Elite: Unbound - Commodore 64 - Ataribaby 2026`.
Vpravo je `Page N` v EN nebo `Strana N` v CZ. Číslování začíná první stranou.

## První strana, obsah a pořadí

První strana obsahuje pouze hlavní titulek **Elite: Unbound**, pod ním
`Contents` / `Obsah`, klikací obsah s tečkovanými vodiči a čísly stran
a níže černý podtržený odkaz `Elite: Unbound website` / `Web Elite: Unbound`.
Titulek nemá podtitulek ani barevný podklad.

Za první stranou následují v pořadí zdrojového webu:

1. Úvodní ilustrace Leaving Lave s původním jazykovým popiskem a vyprávění
   The Beginning / Začátky.
2. What is Elite: Unbound? / Co je Elite: Unbound? od nové strany.
3. Features / Funkce, plynule za představením projektu.
4. Instruction Manual / Návod k použití od nové strany, včetně úvodní poznámky
   a seznamu kláves, poté všechny sekce v pořadí aktuálního HTML.
5. Credits & Foundations / Poděkování a základy projektu od nové strany,
   úplně na konci, s celým aktuálním obsahem příslušné stránky credits.

Speciální přepravní zakázky patří před postup při dokování. Jejich tři
obrázky zachovávají umístění z HTML: nabídka před popisem Ctrl + 1,
detail před popisem VALUE, Inventory před popisem W.

Zachovejte hierarchii PDF záložek: `Elite: Unbound` na první straně,
samostatné úvodní sekce, `Instruction Manual` / `Návod k použití`
s podřízenými sekcemi návodu a závěrečné poděkování. Názvy odvozujte
z aktuálních HTML nadpisů, nikdy je neskládejte z fragmentů vykresleného textu.
Čísla stran v obsahu se musí přepočítat podle skutečné sazby (víceprůchodový
export, např. `multiBuild` v ReportLabu).

## Obrázky a popisky

- Obrázky vodorovně vystřeďte a zachovejte poměr stran bez ořezu.
- Běžné herní screenshoty mají šířku 240 bodů (asi 84,7 mm).
- Úvodní ilustrace Leaving Lave má šířku 350 bodů.
- Malé logo Elite má šířku 115 bodů.
- Úzký detail nepřátelského kontaktu na skeneru má šířku 182 bodů.
- Malé obrázky nezvětšujte nad jejich přirozenou čitelnost. Schválený
  generátor omezoval šířku na nejvýše 0,75 násobku počtu zdrojových pixelů
  vyjádřeného v bodech, se speciálním poměrem 0,65 pro úzký detail skeneru.
- Před obrázkem je mezera 4 body. Za obrázkem bez popisku je 10 bodů.
- Původní popisek patří přímo pod obrázek, do rámce přesně stejné šířky;
  text je vystředěný vůči obrázku. Rámec popisku nemá vnitřní odsazení.
- Obrázek a jeho popisek musí zůstat na stejné straně.
- Nevymýšlejte nové popisky pro screenshoty, které je na webu nemají.
- Z úvodní dvojice artwork-primary / artwork-hover tiskněte pouze hlavní
  ilustraci. Hover varianta s `aria-hidden="true"` do PDF nepatří.
- Zachovejte i klikací odkaz na autorovu galerii připojený k úvodní ilustraci.

## Zalomení a kontrola

Nadpis návodové sekce držte pohromadě s úvodním odstavcem a prvním obrázkem,
pokud se tento celek vejde na jednu stranu. Nenechávejte nadpis osamocený
na konci stránky. Krátká upozornění a jejich tučné názvy držte pohromadě.
Nevynucujte novou stranu pro každou malou sekci ani pro každou ilustraci.

Při použití ReportLabu nevnořujte `KeepTogether` obrázku přímo do dalšího
`KeepTogether` pro úvod sekce: rozbalte jeho obsah do jednoho společného
bloku. Vnoření při zkušební sazbě způsobovalo zbytečné přechody na nové strany.

Po každém finálním exportu:

- Vykreslete **všechny stránky obou PDF** a vizuálně je zkontrolujte,
  včetně titulní strany, diakritiky, popisků, zalomení a spodních okrajů.
- Ověřte textovou vrstvu proti všem odstavcům, položkám seznamů a popiskům
  zdrojových HTML. Kontrolujte také počet skutečně vložených obrázků.
- Ověřte černou barvu všech sázených textů a zachování vložených fontů.
- Ověřte obsah, skutečná čísla stran, interní odkazy i všechny externí
  odkazy, zejména odkazy v poděkování. Relativní odkazy na web převádějte
  na absolutní veřejné adresy, nikoli na lokální cesty v počítači.
- Vypište skutečné názvy záložek a zkontrolujte jejich úplnost, pořadí,
  hierarchii, cílové stránky a nepřítomnost duplicit či poškození.
- Odstraňte dočasné rendery a pomocné soubory; zachovejte tuto specifikaci.

Tento soubor doplňuje pravidla pro PDF v kořenovém `AGENTS.md`.
Samotná regenerace manuálu nevyžaduje herní build ani změnu ASM.
