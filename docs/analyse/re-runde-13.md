# Auswertung DOSBox-Aufnahme `runde-12` (Spur `re-dosbox`, Runde 13)

Stand: abgeschlossen (04.11.2026, geplanter Lauf Claude Opus 5.5, PR #567; Issue #534). Quellen: Spielstände FILE05–FILE28 und
Bilder aus `kaaasi/eishockey-manager-referenz` (`original/spielstaende/`, README mit den Beobachtungen des Nutzers, Stand
`e3a591e`), `emtool hok dump` und der entschlüsselte Klartext (Verfahren `docs/formate/spielstand-original.md`), Disassembly
von `em.exe` mit `tools/reverse-engineering/disasm.py`. Adressen in `em.exe` stehen als `Datei 0x…`, DGROUP = Segment
0x14FD (Datei 0x16DD0 + Versatz). Offsets im entschlüsselten Spielstand (`hok.load`) stehen als `HOK 0x…`, nicht als
`Datei 0x…` (Review R13RD-01). „Ergebnis“ bezieht sich auf die Remake-Annahme der jeweiligen `[APPROX]`-Stelle
(`docs/analyse/inventur.md`): **belegt** = Remake stimmt mit dem Original, **widerlegt** = Remake weicht ab (Umsetzung nötig),
**nicht entscheidbar** = Beleg fehlt (genannt).

Stichproben der Leitstelle dieser Spur nachgerechnet: Oberliga-Austausch FILE20 → FILE21 (Plätze 34/35 Heilbronn, Bad Tölz →
Straubing, Königsbrunn), Bundesliga-Platz von ES Weißwasser (FILE27) bzw. EC Hannover (FILE28), Sperre Datei 0x2890E–0x2891C,
Aufruffolge Datei 0x56448/0x5644E/0x5645D.

## Übersicht

| ID | Ergebnis | Kern | Spur |
|---|---|---|---|
| A009 | widerlegt | Länderwerte immer aus der Aufstellung (0x952C), nicht im Freundschaftsspiel; leere Plätze zählen 0 | `nationalteam` |
| A010 | widerlegt | keine Verdrängung: volle Reihe → „Kein Platz mehr !“; Klick auf Spieler der Reihe nimmt ihn heraus | `nationalteam` |
| A023 | belegt | KO/TE/FO = +0x10/+0x11/+0x12 | `nationalteam` |
| A027 | belegt | Halt am 14.04. mit leerem Kader; „auto“ nur im Durchlauf | `nationalteam` |
| A028 | widerlegt | Zug des Bundestrainers an jedem WM-Tag, auch nach dem Ausscheiden („Spielfrei“) | `nationalteam` |
| A058 | widerlegt | Teamwert-Wanderung läuft auch im Modus „nur Weltkugel“ | `saison` |
| A085 | belegt (Abweichung) | Bundestrainer: „auto“ nach Art, dann Summe; Gleichstand nach Poolindex (Remake: Name). Verein: Platzfolge über den Saisonwechsel unverändert | `nationalteam` / `kader-start` |
| A088 | belegt (Abweichung) | Zähler-Rücksetzung wie Remake, aber Transfermarkt-Spieler behalten ihre Zähler | `saison` (L4) |
| A091 | widerlegt | „Nr.“ = Kaderplatz + 1; Verkauf: Plätze rücken auf, Kauf: hinten an | `kader-start` |
| A125 | belegt | `Players[3]` folgt der Platzfolge des Originals | `kader-start` |
| A163 | widerlegt | Oberliga-Austausch auch im Modus „nur Weltkugel“; Ligen 1/2 unverändert | `saison` |
| A166 | belegt | `AutoLineupRule` gibt alle 11 Stände wieder; Rückfall `Club.AutoDressed` angleichen | `kader-start` |
| A167 | belegt | neuer Platz leer, eingesetzt nur durch Auto-Aufstellung | `kader-start` |
| A189 | belegt (Verletzte) / nicht entscheidbar (Gesperrte, Handmodus) | Verletzung löscht +0x0A, Reihe +0x15 bleibt | `kader-start` |
| A284 | belegt | Fall „vor der Ernennung“ gibt es im Original nicht | `nationalteam` |
| A293 | teils | Gruppenknopf (237 \| 211) belegt; Knopf „Endrunde“ gibt es nicht, Menüweg offen | `nationalteam` |
| A294 | widerlegt | Linksklick außerhalb bewirkt nichts; zurück nur mit Rechtsklick | `nationalteam` |
| A300 | belegt | gleichwertig (Menü mit Zug 5 erst nach der Ernennung) | `nationalteam` |
| U15-a | widerlegt | fester Verein tauscht mit Zufallsplatz der Start-Liga (Datei 0x29CF2), Teamwerte bleiben am Platz | `kader-start` |
| U15-b | belegt | Dauer 0 × Start-Liga ≠ Bundesliga: Reglerschritt wird stumm zurückgenommen (Datei 0x2890E) | `kader-start` |
| U15-c | nicht entscheidbar | Sonderfall „eine Saison, ein Manager“ (Europapokal-Platz, Datei 0x29CFA); Spielstand fehlt | `kader-start` |
| R-K5 | widerlegt | Vereinsauswahl zeigt die beim Start schon gemischte Satzfolge (Datei 0x43A17 vor 0x43A61) | `kader-start` |
| S-K6-a | widerlegt | Mischung am 29.04. (Datei 0x5644E) vor dem Prämienfenster, nicht in `StartNextSeason` | `saison` |
| S-K6-b | nicht entscheidbar (`[APPROX]` bleibt) | Zufallsquelle lrandom (Datei 0xBFD5), Zustand nicht im Spielstand → nicht nachbildbar; Begründung berichtigen | `saison` |
| S-K6-c | widerlegt | Fair-Play-Prämie über die neue Liga in neuer Satzfolge | `saison` (Anschluss `ui-rest`) |

## Sitzung 1 – Bundestrainer (FILE11, FILE18–21)

Grundlage: Spielstände `FILE11/18/19/20/21.HOK` (entschlüsselt mit `hok.load`), Bilder `file11_*`, `file18_*`, `file19_*`,
`file20_*`, `file21_*`, README „Beobachtungen des Nutzers, Sitzung 1“, em.exe (disasm.py). Offsets: Kaderplätze Satz 5 =
HOK 0x06A27 + 5 · 0x618 = HOK 0x0889F (DGROUP 0x5DC3 + 5 · 0x618), Vereine far:9657 ab HOK 0x018A1 (0x3D je Verein,
Feldversatz im Satz), Platzliste DGROUP 0x5CCB = HOK 0x08F97. Manager 5 führt Verein 54 (HOK 0x00E87 + 5 · 0x1AF + 0x14 = 54
in FILE11–20, 58 in FILE21). Das Land DEUTSCHLAND ist Verein 54 (FILE21: 58); Verein 7 (FILE21: 5) ist der gleichnamige
Bundesliga-Platz des Managers.

### A009 – Länderwerte ohne Bundestrainer-Aufstellung

- **Ergebnis:** widerlegt. 0x952C schreibt die Länderwerte (Vereinsfelder 0x18–0x1D) immer aus der Aufstellung und hat
  keinen Zweig „ohne Aufstellung Werte behalten“. Bei nur einem aufgestellten Spieler (Lauf B) fallen sie auf 0/9/0/10/0/6.
  Ein Freundschaftsspiel berechnet sie nicht neu. Neu berechnet wird beim Verlassen der Aufstellung, im täglichen Tageslauf
  und am Ende jedes WM-Tags des Bundestrainers.
- **Fundstelle:**
  - Verein 54, Felder 0x18–0x1D:
    - FILE11: 84 93 91 91 48 50 (Datenbank, Kader leer).
    - FILE18, nach „auto“ und Besuch der Aufstellung am selben Tag (`file18_13`): 88 90 83 87 51 51.
    - FILE19, nach dem Freundschaftsspiel 2:1: unverändert 88 90 83 87 51 51, obwohl alle Kaderwerte gestiegen sind
      (+0x10/+0x11/+0x12 um +1/+2/+2) und sich die Aufstellung geändert hat (Satz 5: Reihen von FUNK/LEHNER,
      DRAISAITL/SCHINKO, B.CRAWFORD, DAFFNER, BLEICHER, Torhüter HEISS→MERK).
    - FILE20, nur JOORIS in Reihe 1 Sturm (`file19_25`, „Eingesetzt: 8“): 00 09 00 0A 00 06. Das passt genau zu
      Σ Reihe / 3, dann / 3: 88/9 = 9, 96/9 = 10, 57/9 = 6.
  - em.exe:
    - Datei 0x0952C–0x0977E: Zielverein = DGROUP 0x966F + 0x1AF · m. Die Felder 0x18 + k, 0x1A + k und 0x1C + k werden
      ohne Bedingung geschrieben (Datei 0x09718–0x09771). Der Schalter (Argument 2) betrifft nur DGROUP 0x974B
      (Datei 0x09672–0x096E2).
    - Aufrufer von 0x952C: Datei 0x28165, 0x2CD1C, 0x40364, 0x465F1, 0x4D385, 0x4E8A3, 0x51206 und 0x54C9F. Keiner liegt
      im Overlay 0xF8 (Freundschaftsspiel Datei 0x3BF9B/0x3C05E).
    - Aufstellung: einziger Ausgang Datei 0x41506–0x4151A ruft 0x952C(DGROUP 0xA70E, 0).
    - Zug des Bundestrainers: Datei 0x555D6 ruft 0x952C(5, 0) nach dem Menü, wenn der Tag ein WM-Tag ist.
- **Umsetzung:**
  - Ziel:
    - In `PlayFriendly` keine Neuberechnung von `_germanyRatings` mehr.
    - Neu berechnen beim Verlassen der Aufstellung des Bundestrainers, nach dem Zug jedes WM-Tags und im Tageslauf.
    - Fallback „leere Formation → Werte behalten“ streichen: leere Plätze zählen 0.
    - Kommentar: [ORIGINAL] mit den Fundstellen oben. Die leere Aufstellung selbst ist nicht aufgenommen, folgt aber aus dem
      Code ohne Sonderzweig.
  - Dateien: `src/EishockeyManager.Core/CupsNational.GameState.cs` (PlayFriendly), `src/EishockeyManager.Core/StatisticsStrength.cs`
    (A203, Anzeige), Anschluss im Tageslauf `GameState.Integration.cs`, Verlassen der Aufstellung in
    `src/EishockeyManager.UI/Screens/Competitions/NationalLineupScreen.cs`. Das Verlassen ist ein Ereignis im Kern, kein
    Zustandswechsel durch bloßes Öffnen.
  - Testidee: Kader „auto“, ein Freundschaftsspiel; die Länderwerte bleiben bis zum Verlassen der Aufstellung bzw. bis zum
    nächsten Tag stehen. Nur ein Stürmer in Reihe 1 ergibt 0/9/0/10/0/6.
  - Spur: `nationalteam`.

### A010 – Verdrängter Spieler bleibt eingesetzt

- **Ergebnis:** widerlegt, denn das Original verdrängt keinen Spieler.
  - Man wählt eine Reihe (Block) und klickt einen Spieler an. Ist die Reihe für seine Art voll, kommt die Kurzmeldung
    „Kein / Platz mehr !“ und nichts ändert sich.
  - Klickt man einen schon in dieser Reihe stehenden Spieler an, wird er herausgenommen. Er hinterlässt eine Lücke und ist
    danach nicht mehr eingesetzt („Eingesetzt“ 22 → 21, Status „Hängt halt so rum…“).
- **Fundstelle:**
  - Bilder: `file19_06` (Reihe 3 ZERWESZ), `file19_07` (ZERWESZ ohne NR/Status, „Eingesetzt: 21“, Reihe 1 links leer),
    `file19_08` (RESERVE-Verteidiger RYAN bei voller Reihe 1 → „Kein Platz mehr !“), `file19_09` („Schon 22 Mann im Team !“).
  - em.exe: Datei 0x414BC–0x414C6 Kurzmeldung 0x0C0B8 mit DGROUP 0x50A7 „Kein“ / 0x50AC „Platz mehr !“ in der Aufstellung
    0x40364.
- **Umsetzung:**
  - Ziel:
    - Die Auswahl im Aufstellungsbildschirm des Nationalteams geht nach Reihe, nicht nach Platz.
    - Ist der Block voll → `NoRoom` (`ShortMessageTexts.NoRoom`), keine Verdrängung.
    - Klick auf einen Spieler, der schon in der gewählten Reihe steht → Platz leeren und nicht mehr eingesetzt (wie `ClearSlot`).
    - `[APPROX]`-Punkt in `CupsNational.Lineup.cs` ersetzen.
  - Dateien: `src/EishockeyManager.Core/CupsNational.Lineup.cs`, `src/EishockeyManager.UI/Screens/Competitions/NationalLineupScreen.cs`.
  - Testidee: volle Reihe 1 (2 Verteidiger), weiteren Verteidiger zuweisen → Ergebnis „kein Platz“, Formation und
    Eingesetzt-Zahl unverändert.
  - Spur: `nationalteam`.
  - Ob der Vereinsbildschirm dieselbe Regel hat, ist nicht Teil dieser Sitzung.

### A023 – Spielerfelder 0x10–0x12 als Kondition/Technik/Form

- **Ergebnis:** belegt. „Spieler in WM-Kader“ zeigt in den Spalten KO, TE und FO der Reihe nach die Felder +0x10, +0x11 und
  +0x12. Der Bonus des Freundschaftsspiels gibt dem ersten Feld einen Punkt weniger (x > 2), Obergrenze 99.
- **Fundstelle:**
  - `file18_01`: KÜNAST 79/81/53 und MERK 84/81/50 = FILE18, Satz 5, Platz 2 bzw. 1, +0x10/+0x11/+0x12. `file19_05`
    (STÄRKEN) zeigt HEISS 85 82 54 = FILE19, Platz 0.
  - FILE18→FILE19: alle 29 Plätze +1/+2/+2. ZERWESZ und SCHINKO bleiben bei +0x10 = 99.
- **Umsetzung:**
  - Ziel: keine Codeänderung. In `CupsNational.cs` (ApplyBonus) [APPROX] → [ORIGINAL] mit Fundstelle `file18_01`/FILE18→19.
  - Dateien: `src/EishockeyManager.Core/CupsNational.cs`.
  - Testidee: bestehender Bonustest. Zusätzlich: 99 bleibt 99, die übrigen Felder +2.
  - Spur: `nationalteam`.

### A027 – Halt vor dem ersten WM-Spiel zur Kaderwahl

- **Ergebnis:** belegt.
  - Im normalen Spiel (ohne Durchlauf) setzt das Original nach dem Sprung nur die Ernennung und zeigt das Hauptmenü des
    Bundestrainers am 14.04. mit leerem Kader.
  - Der Kader „auto“ läuft dort nur im Durchlauf (DGROUP 0x1DB ≠ 0).
  - Weiterschalten mit leerem Kader blockiert die Warnung.
- **Fundstelle:**
  - FILE11: Satz 5 leer, Datum 14.04.1994, DGROUP 0xA70E = 5. Bilder `file11_01` (Gegner TSCHECHEI), `file11_11` (Warnung),
    `file11_12` (Aufstellung leer, „Eingesetzt: 0“).
  - em.exe: Datei 0x54D19 DGROUP 0xD0BA = 1. Datei 0x54D1E–0x54D2D: nur bei DGROUP 0x1DB ≠ 0 folgen DGROUP 0xA70E = 5 und
    0x3CA06(5) („auto“).
- **Umsetzung:**
  - Ziel: keine. In `CupsNationalMode.cs` (SkipToWorldCup) [APPROX] → [ORIGINAL] mit Fundstelle Datei 0x54D1E und FILE11.
  - Dateien: `src/EishockeyManager.Core/CupsNationalMode.cs`.
  - Testidee: neues Spiel „nur Weltkugel“, Continue bis zum Halt → Datum erster WM-Tag, Kader leer, kein WM-Spiel gespielt.
  - Spur: `nationalteam`.

### A028 – Zug des Bundestrainers nur an Spieltagen Deutschlands

- **Ergebnis:** widerlegt. Der Bundestrainer bekommt sein Hauptmenü an jedem Tag mit Kalenderereignis, also an jedem WM-Tag,
  auch wenn Deutschland schon ausgeschieden ist. Nach dem Ausscheiden zeigt das Menü „Spielfrei“, und jeder Klick auf die
  Flagge spielt eine Endrundenrunde.
- **Fundstelle:**
  - Beobachtung Lauf B und Bilder: `file19_28` (24.04.1994 „Spielfrei“), `file20_02` (26.04.), `file20_04` (28.04.).
  - FILE20: 28.04.1994, Deutschland Gruppe 1 Platz 6.
  - em.exe: Datei 0x5557F–0x555A3 setzt DGROUP 0xA70E = 5. Wenn DGROUP 0x9EDA ≠ 0xFD und das Tagesereignis (DGROUP 0x828
    + Tag) ≠ 0 ist, folgt das Hauptmenü 0x44E22. Erst danach (Datei 0x555AD) die Prüfung auf Ereignis 0x10. Das Spielen der
    eigenen Mannschaft fragt das Original nicht ab.
- **Umsetzung:**
  - Ziel: `NationalCoachPlaysOn` → der Halt gilt an jedem WM-Tag (Ereignis 0x10, im Modus 1 jeder Ereignistag), solange es
    einen Bundestrainer gibt.
  - Das Menü zeigt dann „Spielfrei“. Nach dem letzten WM-Tag folgt der Saisonwechsel ohne Hauptmenü (Beobachtung: Weltmeister,
    dann direkt die neue Saison, `file21_02` → `file21_03`).
  - Dateien: `src/EishockeyManager.Core/CupsNationalMode.cs`, Anschluss in `GameState.Integration.cs`.
  - Testidee: Deutschland in der Gruppe ausgeschieden (Platz 6) → Continue hält am 24., 26. und 28.04.; Gegner = keiner.
  - Spur: `nationalteam`.

### A058 – Teamwert-Wanderung im Modus „nur Weltkugel“

- **Ergebnis:** widerlegt. Die große Wanderung beim Saisonwechsel läuft auch im Modus 1.
  - Ligavereine: Kondition und Technik (Felder 0x18–0x1B) ändern sich um bis zu ±13, begrenzt auf die Ligagrenzen (Modus ≠ 1).
  - Nationalteams: nur die Form (0x1C–0x1E) wird neu gewürfelt.
- **Fundstelle:** FILE20 → FILE21, Abgleich über Namen (Indizes umsortiert), jeweils Felder 0x18–0x1B:
  - Kaufbeuren 80 78 79 75 → 92 91 90 87.
  - Bayreuth 57 56 55 59 → 67 68 66 71.
  - Bonn 24 23 23 24 → 36 34 35 36.
  - Ratingen 84 91 80 82 → 78 85 73 76.
  - An der Grenze bleiben: Düsseldorf 94 × 4 (Obergrenze 1. Liga 94), Schwenningen 73 × 4 (Obergrenze 2. Liga 73),
    Freiburg 69 69 (Untergrenze 1. Liga 69), Bad Nauheim 43 × 4 (Obergrenze Oberliga 43). Grenzen DGROUP 0x125 laut
    `docs/spielberechnung.md`.
  - Alle 12 Nationalteams: 0x18–0x1B unverändert.
  - Bei allen Plätzen wird 0x1C–0x1E neu gesetzt (45…55; Feld 0x1E vorher überall 90).
  - Passt zu 0x9D1B mit Modus 10 aus Datei 0x56448.
- **Umsetzung:**
  - Ziel: Bedingung `if (!WorldOnly)` vor `DriftTeamValues(Season)` streichen; [APPROX] → [ORIGINAL] mit Fundstelle
    FILE20/21.
  - Dateien: `src/EishockeyManager.Core/GameState.Integration.cs`.
  - Testidee: Modus „nur Weltkugel“, Saisonwechsel. Mindestens ein Ligaverein ändert seine Werte, alle bleiben in den
    Ligagrenzen, Nationalteams behalten 0x18–0x1B. Mehrsaisonprobe über 4 Wechsel.
  - Spur: `saison`.

### A085 – Reihenfolge der Kaderplätze des Bundestrainers (Teil Bundestrainer)

- **Ergebnis:** belegt mit Abweichung bei Gleichstand.
  - Satz 5 nach „auto“: Plätze 0–2 Torhüter, 3–12 Verteidiger, 13–28 Stürmer. Innerhalb jeder Art absteigend nach Summe
    der Grundstärken (Pool +0x15…+0x17).
  - Bei Gleichstand steht der niedrigere Poolindex vorn (strikter Vergleich beim Durchlauf über Pool 1…179).
  - Das Remake bricht Gleichstände nach Namen.
- **Fundstelle:**
  - FILE18, Satz 5: HEISS 216, MERK 215, KÜNAST 213 | DOLL 234 … PODLAHA 219 | ZERWESZ 239 … KUHNKE 220.
  - Gleichstände: LEDLIN (Pool 129) vor DEISENBERGER (169), je 228. PURVES (110), R.REUTER (148), BROCKMANN (176), je 222.
    LEE (114) vor KUHNKE (166), je 220. AMANN (38) vor J.MEYER (51), je 226. Das Remake (Name) ergäbe DEISENBERGER, LEDLIN
    bzw. BROCKMANN, PURVES, R.REUTER bzw. KUHNKE, LEE.
  - Pool +0x1D = 54 für alle 29.
  - em.exe:
    - Datei 0x3CA06: Kader leeren, dann je Art 0x3CC01 so oft wie in DGROUP 0x4EDE angegeben.
    - Datei 0x3CC01: Schleife Pool 1 … 0xB3. Übersprungen werden Pool +0x13 ≠ 0, falsche Art (0x04180) und Pool +0x1D =
      DGROUP 0x9EDA. Vergleich `jl`/`jna` (Datei 0x3CCBA–0x3CCC4), daher gewinnt bei Gleichstand der erste.
  - Handaufnahme (Einfügen hinter den letzten gleicher Art, Datei 0x42D46) ist in dieser Aufnahme nicht geprüft: kein Spieler
    von Hand hinzugefügt.
- **Umsetzung:**
  - Ziel: In `AutoNominate` bei gleicher Summe die Reihenfolge der Kandidatenliste des Originals nehmen statt des Namens.
    Ein Poolindex fehlt im Remake, deshalb [APPROX] „Spieler-Id bzw. Datenbankreihenfolge statt Poolindex“.
  - Kommentar in `MatchEngine.National.cs` („Reihenfolge der Kaderplätze wie der WM-Kader (nicht gelesen)“) → [ORIGINAL]
    mit FILE18.
  - Dateien: `src/EishockeyManager.Core/CupsNational.GameState.cs`, `src/EishockeyManager.Core/MatchEngine.National.cs`.
  - Testidee: zwei Stürmer mit gleicher Summe; der zuerst geführte steht nach „auto“ vorn, unabhängig vom Namen.
  - Spur: `nationalteam`.

### A163 – Auf-/Abstieg und Oberliga-Austausch im Modus „nur Weltkugel“

- **Ergebnis:** widerlegt.
  - Der Oberliga-Austausch läuft auch im Modus 1: die letzten beiden der (ungespielten) Oberliga-Platzliste gehen, zwei
    Vereine von den Plätzen 36–41 rücken nach.
  - Zwischen 1. Liga, 2. Liga und Oberliga wechselt niemand. Die Mengen bleiben gleich, nur die Reihenfolge innerhalb der
    Ligen ändert sich.
- **Fundstelle:** Platzliste DGROUP 0x5CCB (HOK 0x08F97), Oberliga-Teil Plätze 24–35:
  - FILE20 (gleich FILE11): …, TSV ERDING, HEC HEILBRONN, EC BAD TÖLZ.
  - FILE21: …, TSV ERDING, EHC STRAUBING, ESV KÖNIGSBRUNN.
  - HEILBRONN (Platz 34) und BAD TÖLZ (Platz 35) stehen in FILE21 auf Index 37 bzw. 41, außerhalb. STRAUBING (FILE20 Index 37)
    und KÖNIGSBRUNN (Index 41) stehen jetzt auf 24 bzw. 29.
  - 1. Liga und 2. Liga: dieselben 12 Namen in FILE20 und FILE21.
  - Passt zur Regel Datei 0x56238–0x56284 (aus = Platzliste + 24 + 11 − k, ein = Verein random(36, 41)).
- **Umsetzung:**
  - Ziel: `ExchangeOberliga` auch bei `WorldOnly` aufrufen. Grundlage ist die Platzliste ohne gespielte Spiele, also die
    Startordnung. Auf- und Abstieg der Ligen 1/2 bleibt wie bisher ohne Spiele aus; prüfen, ob die Remake-Relegation ohne
    Spiele niemanden bewegt.
  - [APPROX] → [ORIGINAL] mit FILE20/21.
  - Dateien: `src/EishockeyManager.Core/SeasonFlow.cs`, `src/EishockeyManager.Core/SeasonFlow.Oberliga.cs`.
  - Testidee: Modus „nur Weltkugel“, Saisonwechsel. Genau zwei Oberligavereine (die letzten der Startordnung) werden gegen
    zwei Vereine der Plätze 36–41 getauscht, Ligen 1/2 unverändert. Mehrsaisonprobe über 4 Wechsel.
  - Spur: `saison`.

### A284 – Mannschaft vor der Ernennung gesperrt

- **Ergebnis:** belegt (Fall tritt im Original nicht auf).
  - Im Modus 1 erscheint das Menü des Bundestrainers erst nach Sprung und Ernennung; im Modus 4 nur im Zug „Manager 5“ mit
    Bundestrainer. Ein Menü „vor der Ernennung“ gibt es also nicht.
  - Nach der Ernennung und vor der Kaderwahl ist die Mannschaft offen: Aufst., Taktik, Transfer, Training; eine Kachel
    gesperrt, ein Fisch. Das entspricht dem Remake mit `NationalCoach ≠ null`.
  - Ab dem ersten Gruppenbildschirm sind Transfer und Training gesperrt, siehe Offene Fragen.
- **Fundstelle:**
  - Bild `file11_03` und Beobachtung des Nutzers „Kacheln vor der Kaderwahl“.
  - FILE11: Satz 5 leer, DGROUP 0xD0BA gesetzt (Datei 0x54D19).
  - em.exe: Datei 0x5557F–0x555A3 (Zug 5 nur bei DGROUP 0x9EDA ≠ 0xFD).
- **Umsetzung:**
  - Ziel: keine Verhaltensänderung. In `NationalMenu.cs` [APPROX] → [ORIGINAL]: nach der Ernennung offen (`file11_03`),
    vor der Ernennung kein Menü (Datei 0x54D19, 0x55587).
  - Dateien: `src/EishockeyManager.UI/Screens/Competitions/NationalMenu.cs`.
  - Testidee: FILE11-Lage (ernannt, Kader leer) → die vier Mannschaftskacheln offen, Verträge gesperrt.
  - Spur: `nationalteam`.

### A293 – Lage der Knöpfe „Gruppe 1/2“ und „Endrunde“ der WM-Tabelle

- **Ergebnis:** teils belegt, teils widerlegt.
  - Der Knopf „Gruppe 2“/„Gruppe 1“ liegt im Original genau bei (237 | 211), 57 × 12, wie im Remake. Er wird nur gezeichnet,
    wenn die Tabelle aus dem Menü kommt; in den WM-Tagesbildern fehlt er.
  - Einen Knopf „Endrunde“ hat 0x2F5B6 nicht. Der Remake-Knopf bei x = 173 hat dort keine Entsprechung.
  - Wie das Original die Endrundenspiele aus dem Menü erreicht, ist nicht entscheidbar: Verteiler Datei 0x3A88A nicht gelesen.
- **Fundstelle:**
  - em.exe:
    - Datei 0x2F625–0x2F690: nur bei Argument 2 = 0 zeichnet 0x4C196 bei (0xED | 0xD3) den Text DGROUP 0x65C („Gruppe“) +
      0x4E30 + (2 − Gruppe).
    - Datei 0x2FA4A–0x2FA6C: Klickfläche y 211–222, x 237–293 → Rückgabe 12 − Gruppe, Ereignis 3.
    - Menüaufruf Datei 0x3AC84–0x3AC8E mit Wert − 11 (Werte 11/12).
    - Tageslauf Datei 0x30FF2–0x3100D mit Argument 2 = 1, beide Gruppen nacheinander, nur bei DGROUP 0x229 ≠ 0.
  - Bilder `file19_15`, `file19_19`, `file19_20`: kein Knopf (Tagesablauf).
- **Umsetzung:**
  - Ziel: Gruppenknopf → [ORIGINAL] mit Datei 0x2F688/0x2FA4A. Der Knopf erscheint nur bei Aufruf aus dem Menü. Zeigt das
    Remake die Tabelle im Tagesablauf, dann ohne Knopf.
  - Knopf „Endrunde“ als [APPROX] lassen, bis der Menüweg gelesen ist.
  - Dateien: `src/EishockeyManager.UI/Screens/Competitions/WorldCupScreen.cs`.
  - Testidee: Klick bei (240 | 215) wechselt die Gruppe, bei (236 | 215) nicht.
  - Spur: `nationalteam`.

### A294 – Linksklick außerhalb der WM-Tabelle kehrt zurück

- **Ergebnis:** widerlegt.
  - Ein Linksklick wirkt nur auf den Gruppenknopf und die Zeilen (y > 42, x wird nicht geprüft); sonst wartet die Tabelle
    weiter.
  - Zurück geht es nur mit Ereignis 3 (Rechtsklick) oder über den Knopf; im Durchlauf (DGROUP 0x1DB ≠ 0) sofort.
  - Beobachtung: Klick in, links oder rechts neben der Zeile und auf den Rand öffnet den Infoscreen.
- **Fundstelle:**
  - em.exe: Datei 0x2FA2D–0x2FADC. Bei DGROUP 0x5A16 = 1 Knopf- und Zeilenprüfung, Ausgang nur bei 0x5A16 = 3 oder
    DGROUP 0x1DB ≠ 0, sonst Sprung nach Datei 0x2FA1E.
  - README Sitzung 1 „WM-Tabellen“.
- **Umsetzung:**
  - Ziel: In `WorldCupScreen.Update` den Zweig `input.LeftClick || input.Keys.Count > 0 → zurück` auf Rechtsklick beschränken.
    Tasten: im Original nicht geprüft, Escape als Remake-Komfort kennzeichnen.
  - Dateien: `src/EishockeyManager.UI/Screens/Competitions/WorldCupScreen.cs`.
  - Testidee: Linksklick bei (100 | 30) → Bildschirm bleibt; Rechtsklick → zurück.
  - Spur: `nationalteam`.

### A300 – Bundestrainer ohne Ernennung: keine Kaderwarnung

- **Ergebnis:** belegt (gleichwertig).
  - Das Original prüft beim Klick aufs Logo nur DGROUP 0xA70E = 5 und weniger als 10 Kaderspieler.
  - Ein Menü mit 0xA70E = 5 gibt es nur nach der Ernennung, daher ist der Remake-Wächter `NationalCoach is {}` wirkungslos.
  - Mit leerem Kader nach der Ernennung erscheint die Warnung, der Tag läuft nicht weiter.
- **Fundstelle:**
  - Bild `file11_11`, FILE11 (Satz 5 leer).
  - em.exe: Datei 0x454C2–0x454F2, Datei 0x54D19, Datei 0x5557F–0x55587.
  - Lauf B: 29 im Kader, 8 eingesetzt → keine Warnung, weil 0x4BE17 Modus 0 Kaderplätze zählt.
- **Umsetzung:**
  - Ziel: keine. In `MainMenuScreen.cs` beim Logo-Klick [APPROX] → [ORIGINAL] mit Fundstelle.
  - Dateien: `src/EishockeyManager.UI/Screens/MainMenuScreen.cs`.
  - Testidee: ernannt, Kader 9 → Warnung; Kader 10 → Tag läuft.
  - Spur: `nationalteam`.

### Nebenbefunde (nicht in den IDs)

- **Aufstellung nach dem Freundschaftsspiel:** FILE18→FILE19 ändert die Aufstellung von Satz 5 (siehe A009); `file19_05`
  zeigt den neuen Stand. Ob das Spiel (0x977F hat den Aufrufer Datei 0x3B4C6) oder der Nutzer umgestellt hat, ist aus den
  Bildern nicht zu trennen. Betrifft A013.
- **Länderwerte über den Saisonwechsel:** In FILE21 hat Deutschland (Verein 58) in den Feldern 0x18–0x1B weiter 0/9/0/10,
  und Feld 0x16 = 6 bleibt (Bundestrainer bleibt im Modus 1). Die Felder 0x1C/0x1D werden dagegen neu gewürfelt (FILE20
  `00 06` → FILE21 `33 2E`, wie bei allen Plätzen; A058: Form 0x1C–0x1E). Nur 0x18–0x1B bleiben also im Modus 1 erhalten.
  Folgerung für A009/A058: Der gespeicherte Länderwert (`_germanyRatings`) führt nur die Werte aus 0x18–0x1B über den
  Saisonwechsel, die Form (0x1C/0x1D) würfelt der Saisonwechsel neu. Spur `saison`, sofern `RerollNationalRatings` dort
  greift; `nationalteam` beachtet es bei A009.
- **Dauer der Warnung:** Der Nutzer schätzt sie auf ca. 2 s, das Remake rechnet ≈ 1,4 s (A298). Eine Schätzung, kein Messwert.
- **Kandidaten für „auto“:** Pool 1…179 ohne Ausländerprüfung in 0x3CC01. Die Liste „Spieler in WM-Kader“ zeigt 179 Spieler;
  das Remake filtert Ausländer. BIAKIN und GARDNER sind grau, Grund offen.

### Offene Fragen dieses Abschnitts

1. A293: Weg zu den WM-Endrundenspielen aus dem Menü (Verteiler Datei 0x3A88A, Sprungtabelle cs:0x12FB, 0x2FC2F) lesen
   oder als DOSBox-Bild nachfordern (Tabellen → WM nach der Gruppenphase, aus dem Menü).
2. A085: Handaufnahme eines Spielers nach „auto“ ist weiter nicht aufgenommen (Stand D aus `re-nachtrag.md` Abschnitt 9).
3. A009: Eine ganz leere Aufstellung mit Tageswechsel ist nicht aufgenommen; das Ergebnis 0 folgt nur aus dem Code.
4. Sperre von Transfer und Training ab dem ersten Gruppenbildschirm (`file19_26`): keiner ID zugeordnet, Regel im Remake prüfen.

## Sitzung 2 und 3 – Saisonwechsel und Kader (FILE16A, FILE22–26)

Grundlage: FILE16A, FILE22, FILE23 (Sitzung 2), FILE05, FILE24, FILE25, FILE26 (Sitzung 3), zum Vergleich FILE06/FILE07 und
die Startstände FILE01, FILE27, FILE28. Rohstrom per `hok.load`, Kaderplätze HOK 0x06A27 + 0x618 · Satz + 0x34 · Platz
(DGROUP 0x5DC3), Spielerpool HOK 0x0505F (`far:8253`, 33 Byte). Auswerteskripte (Scratch, nicht im Repo):
`/root/s/py/s23.py` (Rohdump), `s23diff.py` (Feldvergleich zweier Stände), `s23cols.py` (Zählerspalten),
`s23auto.py` (Nachbau von Datei 0x426BC gegen die Stände).

Kurzbefund vorweg: Die Kaderplätze behalten über den Saisonwechsel ihre Reihenfolge. Beim Verkauf rücken die folgenden
Plätze auf, beim Kauf kommt der neue Spieler hinten dazu. Die angezeigte „Nr.“ ist **Kaderplatz + 1**. Die automatische
Aufstellung 0x426BC, wie sie `AutoLineupRule` nachbaut, erklärt „eingesetzt“ und „Reihe“ in **allen 11 Ständen mit
eingeschalteter Einstellung** ohne Abweichung. Nur FILE06 weicht ab, und genau um die Änderung von Hand.

### Zusätzliche Feldbelege (für `docs/formate/spielstand-original.md`)

| Feld | Befund | Beleg |
|---|---|---|
| Kaderplatz `+0x1C`/`+0x1E`/`+0x20` (Wörter) | Spiele beim jetzigen Verein je Wettbewerb 0/1/2; gleich `+0x06`/`+0x07`/`+0x08` bis zum Saisonwechsel, danach **nicht** zurückgesetzt | FILE16A/FILE22/FILE05/FILE24 Spiegel ohne Ausnahme (außer gekauftem DAFFNER, FILE07 Platz 25); FILE23 `+0x1C` = 3, `+0x1E` = 2 bei `+0x06`/`+0x07` = 0 |
| Kaderplatz `+0x22`/`+0x24`/`+0x26` (Wörter) | Tore beim jetzigen Verein je Wettbewerb; gleich `+0x03`/`+0x04`/`+0x05` (Feldspieler), beim Saisonwechsel **nicht** zurückgesetzt; Torhüter 0 (sein `+0x03` sind Gegentore) | wie oben, z. B. HADRASCHEK FILE22 `+0x04` = 4, `+0x24` = 4 → FILE23 `+0x04` = 0, `+0x24` = 4; em.exe `Datei 0x54300`/`0x54314` (`cmp word [+0x22],0xFF`, `inc word`) |
| Kaderplatz `+0x09` Bit 0x02, `+0x0D`, `+0x17` | verletzt, Restdauer, Verletzungsart | em.exe `Datei 0x5C44` (`or +0x09,2`), `0x5BE5` (`+0x0D`), `0x5C4C` (`+0x17`), `0x5C50` (`+0x0A = 0`); FILE24 STEFAN `+0x09` 00→02, `+0x0D` 0→10, `+0x17` 00→10 |
| Kaderplatz `+0x02` | offen; steigt in der Saison (TRAVNICEK, MORRISON, LANGLOIS 0→2 von FILE16A nach FILE22), wird beim Saisonwechsel **nicht** zurückgesetzt | FILE22→FILE23 unverändert |
| Pool `+0x1C` | Ligaspiele der Saison (gleich Kaderplatz `+0x06`), Saisonwechsel → 0 | FILE22→FILE23, 16 von 16 → 0 |
| DGROUP 0x375 + 2 · m | „automatisch aufstellen“ liegt in HOK 0x00844 | FILE05 01, FILE06 00, FILE24 00 (Byte 0x376 = 01), FILE25 01 |

### A085 – Reihenfolge der Kaderplätze über den Saisonwechsel (Vereinsteil)

- **Ergebnis:** belegt (für den Vereinskader). Alle 22 Kaderplätze von SV BAYREUTH behalten von FILE22 nach FILE23 ihren
  Poolindex. Weder sortiert das Original neu, noch schiebt es Plätze zusammen. Von FILE16A nach FILE22 gilt dasselbe. Das
  Remake ordnet `Club.Players` am Saisonwechsel ebenfalls nicht um. Nicht geprüft ist ein Abgang am Saisonende (Vertrag
  ausgelaufen), denn in diesem Paar gehen alle Verträge von 2 auf 1. Der Teil zum Bundestrainer (Satz 5,
  `MatchEngine.National.cs:58`) gehört zu Sitzung 1.
- **Fundstelle:** FILE22/FILE23 HOK 0x06A27 + 0x34 · Platz, `+0x0F` je Platz gleich (Platz 0 HABNITT p2 … Platz 21 NOACK p108);
  Bild `file23_3_kader.png`.
- **Umsetzung:** Ziel: keine für den Verein. Den Befund in der Doku der Kaderreihenfolge (A091) als [ORIGINAL] mit Fundstelle
  FILE22/FILE23 vermerken. Dateien: `src/EishockeyManager.Core/MatchEngine.Managed.cs` (Kommentar zu `LiveSquad.Create`),
  `Squad.AutoLineup.cs`. Testidee: In der Mehrsaisonprobe bleibt die Reihenfolge der Spieler-IDs über vier Saisonwechsel
  gleich, solange niemand kommt oder geht. Spur: `kader-start`.

### A088 – Zurücksetzen der Spielerzähler zum Saisonwechsel

- **Ergebnis:** belegt mit einer Abweichung beim Transfermarkt. Das Original löscht für jeden Manager (Sätze 0 … [0x386] − 1)
  in allen 29 Kaderplätzen `+0x00`, `+0x01`, `+0x03`–`+0x05` und `+0x06`–`+0x08`. Im Spielerpool löscht es für alle 200
  Spieler `+0x1B` (Ligatore), `+0x1C` (Ligaspiele) und `+0x1F` (Wort: Vorlagen bzw. Torhüterminuten). Stehen bleiben
  `+0x02`, `+0x1C`–`+0x20` (Spiele beim Verein) und `+0x22`–`+0x26` (Tore beim Verein). Die Zähler der Kaderplätze des
  **Transfermarkts (Satz 4)** löscht das Original nicht. Das Remake setzt in `ResetMatchCounters` auch
  `TransferMarket.FreeAgents` zurück. Ein Kauf übernimmt im Original die Poolzähler, also nach dem Wechsel 0, in den neuen Platz.
- **Fundstelle:** em.exe `Datei 0x27A0C–0x27AA8` (in Datei 0x26F20, Fenster „Ereignisse“): Schleife über Manager bis [0x386],
  29 Plätze, schreibt 0 nach DGROUP 0x5DC3/0x5DC4 sowie je drei Bytes ab 0x5DC6 und 0x5DC9. em.exe `Datei 0x2769E–0x276E7`:
  Pool `+0x1C`, `+0x1B` (Byte) und `+0x1F` (Wort) = 0, nur wenn [0xA70E] + 1 = [0x386], also einmal beim letzten Manager.
  Spielstände FILE22 (14.04.1994) → FILE23 (10.09.1994), Satz 0: `+0x01` 10 Plätze → 0, `+0x03` 9 → 0, `+0x04` 10 → 0,
  `+0x06` 16 → 0, `+0x07` 17 → 0, PROKES `+0x00` 1 → 0. Pool: `+0x1B` 8 → 0, `+0x1C` 16 → 0, `+0x1F` 13 → 0 (M.SCHMIDT 180 → 0).
  Unverändert bleiben z. B. HORSTMANN `+0x02` = 2, `+0x1C` = 3, `+0x1E` = 2, `+0x22` = 2, `+0x24` = 1. Satz 4 (Markt):
  KAMMERER `+0x03` = 1, `+0x06` = 3 in beiden Ständen, sein Pool `+0x1B`/`+0x1C` 1/3 → 0/0. Kauf: `Datei 0x42E93`
  (neuer Platz `+0x03` = Pool `+0x1B`) und `0x42EAE` (`+0x06` = Pool `+0x1C`). Bild `file23_1_ereignisse_30apr.png`.
- **Umsetzung:** Ziel: Die Spieler der Vereine bleiben wie bisher. Für Transfermarktspieler bleiben Spiele, Tore und Strafen
  der alten Saison für Anzeige und Spielerwert stehen (`+0x03`–`+0x05` gehen mit Gewicht 30/30/40 in den Wert ein). Beim Kauf
  nach dem Wechsel beginnt der Spieler mit 0. Bis dahin `[APPROX]` in `MatchEngine.Live.cs:367` → `[ORIGINAL]` mit
  Datei 0x27A0C/0x2769E. Dateien: `src/EishockeyManager.Core/MatchEngine.Live.cs` (`ResetMatchCounters`, nur der Teil
  FreeAgents), `src/EishockeyManager.Core/Player.Factory.cs` (`Join`: Saisonzähler aus dem „Pool“). Testidee: Ein Marktspieler
  mit 2 Toren behält nach `StartNextSeason` Tore = 2 und den Listenpreis. Nach dem Kauf hat er Tore = 0 und Spiele = 0. Ein
  Kaderspieler hat Spiele = 0, `GamesForClub` bleibt. Spur: `saison`. Die Änderung liegt in `MatchEngine.Live.cs` und
  `Player.Factory.cs`; `saison` ändert dort wenig, mit Begründung im PR (Entscheidung L4).

### A091 – Kaderreihenfolge nach Rückennummer statt Kaderplätzen

- **Ergebnis:** widerlegt. Das Original kennt keine eigene Rückennummer: Die „Nr.“ ist Kaderplatz + 1 und erscheint nur bei
  `+0x0A` ≠ 0. Beim Verkauf rücken die folgenden Plätze auf (Datei 0x425B6). Beim Kauf kommt der neue Platz hinten dazu,
  unabhängig von der Rolle. Am Saisonwechsel bleibt alles, wie es ist. Das Remake vergibt dagegen die kleinste freie Nummer
  (`PlayerFactory.FreeNumber`) und behält sie. `LiveSquad.Create` und `AutoLineupRule` sortieren nach Rolle und Nummer
  (`Club.SortedPlayers`). Nach einem Verkauf mit anschließendem Kauf weichen deshalb ab:
  - die angezeigten Nummern (Remake 15–25 statt 14–24);
  - die Platzfolge. Der neue Spieler bekommt die Lücke statt des letzten Platzes.
  Die Platzfolge entscheidet in 0x42838 bei Gleichstand (späterer Platz gewinnt). In FILE05 hängt Reihe 3 genau daran:
  LALA, HANEMANN und H.STEIGER haben alle ST 68.
- **Fundstelle:** FILE05 → FILE26 (HOCK, Platz 13, auf die Transferliste): Plätze 14–24 werden 13–23, Kader 25 → 24.
  Bilder `file26_1_aufstellung_vorher.png` (Reihe 3: 13/16/25) und `file26_3_aufstellung_nachher.png` (13/23/24, „Nr.“ = Platz + 1).
  HOCK steht danach als letzter Markteintrag (Satz 4, Platz 7) und behält `+0x15` = 2S. em.exe `Datei 0x47066–0x470D6`:
  Marktplatz über 0x42CB1, eigener Platz `+0x0A` = 0, 0x34 Byte kopieren, dann 0x425B6. FILE05 → FILE07: DAFFNER kommt auf
  Platz 25 (Nr. 26) hinter LALA.
- **Umsetzung:** Ziel: Die Kaderreihenfolge ist die Reihenfolge von `Club.Players`, wie sie heute schon gepflegt wird
  (`Add` hinten, `Remove` rückt auf, Saisonwechsel unverändert). Die Nr. wird als Listenplatz + 1 abgeleitet oder nach
  jeder Änderung neu vergeben. `LiveSquad.Create` und `ApplyAutoLineup` übergeben `Players` statt `SortedPlayers`. Falls
  `Number` gespeichert wird, braucht es Schema und Migration. Ob das Original höchstens 29 Plätze hat, ist abzugleichen
  (Remake: 99 Nummern). Dateien: `src/EishockeyManager.Core/MatchEngine.Managed.cs` (`LiveSquad.Create`),
  `Squad.AutoLineup.cs`, `Squad.Strength.cs` (`SortedPlayers`), `Player.Factory.cs` (`FreeNumber`/`Join`), `Squad.Setup.cs`,
  `src/EishockeyManager.UI/Screens/Team/LineupScreen.cs` (Anzeige der Nr.). Testidee: Kader wie FILE05; Nr. 14 verkaufen →
  die bisherigen Nr. 15–25 heißen 14–24. Danach einen Stürmer kaufen → er ist Nr. 25 und gewinnt den Gleichstand in der
  Auto-Aufstellung gegen einen gleich starken Spieler mit kleinerer Nummer. Spur: `kader-start`.

### A125 – Alter aus Kaderplatz 3 mit Remake-Reihenfolge

- **Ergebnis:** belegt. `ComputedDemand` liest `club.Players[3]` und damit die Listenreihenfolge, nicht `SortedPlayers`. Diese
  Reihenfolge stimmt mit den Kaderplätzen des Originals überein:
  - Start in der Folge Torhüter, Abwehr, Sturm in Poolfolge (FILE01, FILE05, FILE27, FILE28);
  - Kauf hinten (FILE07);
  - Verkauf mit Aufrücken (FILE26);
  - Saisonwechsel unverändert (FILE22/FILE23: Platz 3 = DOLL p23 in beiden).
  Es bleibt richtig, solange die Umsetzung von A091 die Liste nicht umsortiert. Nicht gegen das Original geprüft ist die
  Rückkehr eines Leihspielers (`Player.Contract.cs:199`, `Add` hinten).
- **Fundstelle:** FILE22/FILE23 HOK 0x06A27 + 3 · 0x34 (`+0x0F` = 23, beide Stände). FILE05/FILE26/FILE07 Platz 3 = MAYR p23
  in allen drei. em.exe `Datei 0x4A93F` (Fundstelle laut Code). Platzverschiebung `Datei 0x425B6`.
- **Umsetzung:** Ziel: keine Änderung am Verhalten. `[APPROX]` in `Player.Contract.cs:399` → `[ORIGINAL]` mit den Fundstellen
  FILE07/FILE26/FILE23. Dateien: `src/EishockeyManager.Core/Player.Contract.cs`. Testidee: Kader mit fünf Spielern; Platz 1
  verkaufen → die Forderung nimmt das Alter des bisherigen Platzes 4 (jetzt Index 3). Spur: `kader-start`.

### A166 – Eingesetzt-Vorgabe bei Start, Transfer und Computervereinen

- **Ergebnis:** belegt für `AutoLineupRule` (0x426BC/0x42838). Nachgebaut auf den gespeicherten Stärken in
  Kaderplatzfolge, liefert die Regel `+0x0A` und `+0x15` aller eingesetzten Spieler ohne Abweichung in FILE01, FILE27, FILE28
  (Spielstart), FILE05, FILE07 (Kauf), FILE26 (Verkauf), FILE24 und FILE25 (Verletzte), FILE16A, FILE22 und FILE23
  (Saisonwechsel). Nur FILE06 weicht ab, und zwar nur um die Änderung von Hand. Die Rückfallregel in `Club.AutoDressed`
  (Summe K + T + F, dann Rolle und Nummer, ohne Reihum-Auffüllung) gibt es im Original nicht.
  - FILE05: Reihe 3 = STEFAN, BRANDL und LALA (Gleichstand ST 68 → späterer Platz). Ersatz reihum Sturm, Abwehr, Torhüter:
    H.STEIGER, A.POKORNY, HEISS, HANEMANN, MAYR, dann HOHENADL als Abwehr statt eines zweiten Torhüters. Nicht eingesetzt
    sind J.SCHULZ, SANDNER und DOBRYZINSKI.
  - FILE07: DAFFNER (ST 68, letzter Platz) verdrängt LALA aus Reihe 3. HANEMANN fällt heraus, weil H.STEIGER als späterer
    Platz den Gleichstand gewinnt.
  - Computervereine haben im Original keine Kaderplätze, also auch kein `+0x0A`.
- **Fundstelle:** `/root/s/py/s23auto.py` gegen die genannten Stände (alle „keine Abweichung“). em.exe `Datei 0x426E7–0x42706`
  (alle `+0x0A` = 0), `0x42744`/`0x42777` (eingesetzt, Reihe), `0x427C0–0x4282C` (Ersatz bis 22), `0x428B1–0x4292F`
  (Kandidat `+0x0A` = 0, `+0x09 & 3` = 0, (K+T+F)/3, Gleichstand → späterer Platz). Aufrufe: Spielstart `0x28165`, Ende
  Transferliste `0x47BB5`, Tageslauf Datei `0x7C07`, Spieltag `0x54D9E`/`0x5530B`. Bilder `file05_2_aufstellung.png`,
  `file26_3_aufstellung_nachher.png`.
- **Umsetzung:** Ziel: `Club.AutoDressed` an `AutoLineupRule` angleichen: Eingesetzte vor der ersten Auto-Aufstellung
  ebenfalls nach 0x426BC, oder sicherstellen, dass jeder Managerverein vor der ersten Anzeige `ApplyAutoLineup` durchläuft.
  `[APPROX]` in `Squad.Lineup.cs:233` → `[ORIGINAL]` mit den Fundstellen oben. Den Teil „Computervereine“ als „im Original ohne
  Kaderplätze“ vermerken. Die verbleibende Abweichung ist die Platzfolge (A091). Dateien: `src/EishockeyManager.Core/Squad.Lineup.cs`,
  `Squad.AutoLineup.cs`. Testidee: Synthetischer Kader mit den Stärken aus FILE05 (25 Spieler, Platzfolge wie dort) →
  `ApplyAutoLineup` ergibt genau die Reihen und Eingesetzten aus FILE05. Mit DAFFNER hinten ergibt er FILE07, ohne HOCK
  ergibt er FILE26. Spur: `kader-start`. A166 wechselt erst mit diesem Regressionstest von `kader-start` auf `[ORIGINAL]`:
  Die Auswerteskripte liegen nicht im Repo, nachgerechnet hat das Review nur eine Stichprobe (Review R13RD-06).

### A167 – Aufgestellte immer eingesetzt, neue Kaderspieler nicht eingesetzt

- **Ergebnis:** belegt, mit einer Ausnahme im Datenmodell, die man nicht sieht. Ein neuer Kaderplatz entsteht leer: 0x42CB1
  setzt die 0x34 Byte auf 0, also `+0x0A` = 0 und `+0x15` = 0. Eingesetzt wird der neue Spieler nur durch die
  Auto-Aufstellung beim Verlassen der Transferliste (FILE07: DAFFNER `+0x0A` = 1, Reihe 3S, Einstellung an). Wer von Hand
  aufstellt, setzt ein (0x415B2). Wer einen Platz leert, nimmt heraus (FILE06: BRANDL `+0x0A` 1→0 mit `+0x15` → 0,
  DOBRYZINSKI 0→1 mit 0x83). Ausnahme: Ein Verletzter behält im Original `+0x15` (Reihe), verliert aber `+0x0A`. Die
  Anzeige zeigt ihn in keiner Reihe und ohne Nr. Das Remake nimmt ihn stattdessen aus der Aufstellung. Gleichwertig ist
  das, solange kein Leser `+0x15` ohne `+0x0A` auswertet. Gesucht wurde ein solcher Leser nicht. Den Fall ohne
  Auto-Aufstellung zeigt kein Stand: Kauf bei ausgeschalteter Einstellung.
- **Fundstelle:** em.exe `Datei 0x42E3A` (Löschen 0x34 Byte im neuen Platz), `0x42E48` (`+0x0F`), kein Schreiben von `+0x0A`
  in 0x42CB1. em.exe `Datei 0x5C50` (Verletzung: `+0x0A` = 0, `+0x15` unberührt). FILE07 Platz 25 DAFFNER; FILE06 Plätze 12/19;
  FILE22 PESKA/PROKES (`+0x09` = 2, `+0x0A` = 0, `+0x15` = 1S, dazu fünf Plätze mit 1S); FILE24 STEFAN 3S/CHAIDAROV 1S mit
  `+0x0A` = 0 (vier Plätze 1S bzw. 3S). Bild `file24_5_aufstellung_nach_spieltag.png` („VERL.“ ohne Nr. und Reihe).
- **Umsetzung:** Ziel: keine Verhaltensänderung. `[APPROX]` in `Squad.Lineup.cs:236` → `[ORIGINAL]` mit 0x42E3A/0x47BB5/FILE07.
  Die Abweichung „Verletzter behält `+0x15`“ als bewusste Modellabweichung vermerken; sie betrifft auch den Export und
  Import von Spielständen. Dateien: `src/EishockeyManager.Core/Squad.Lineup.cs`. Testidee: Einstellung aus, Spieler kaufen →
  „Eingesetzt“ unverändert, neuer Spieler nicht eingesetzt. Einstellung an, Transferliste verlassen → Auto-Aufstellung
  setzt ihn nach Wert ein. Spur: `kader-start`.

### A189 – Verletzte oder gesperrte Aufgestellte und „spielfähig“

- **Ergebnis:** belegt für Verletzte, nicht entscheidbar für Gesperrte. Das Original zählt nur Plätze mit `+0x15` ≠ 0 **und**
  `+0x0A` ≠ 0. Die Verletzung löscht `+0x0A` sofort und ohne Abfrage der Einstellung. Der Verletzte zählt also nicht, obwohl
  `+0x15` stehen bleibt. So rechnet auch `IsShortHanded`.
  - Im Spiel bleibt die Lücke: `file24_2`, Reihe 3 = 13/25/leer, „Eingesetzt: 21“. Das Original schaltet die Einstellung
    für die Dauer der Spiele ab (DGROUP 0x375 → 0x376, 0x375 = 0).
  - Nach dem Spieltag stellt es sie wieder her und stellt über 0x426BC neu auf (`file24_5`, „Eingesetzt: 22“). Das entspricht
    `RefillLineup` nach der Abrechnung (`MatchEngine.Live.cs:317`).
  - Ohne Auto-Aufstellung schreibt im Verletzungsweg niemand `+0x15`. Schreibstellen gibt es nur in 0x426BC, der Oberfläche,
    Transfer und Saisonwechsel. Das spricht dafür, dass bei Aufstellung von Hand die Lücke bleibt. Das Remake füllt sie dann
    über `RefillLineup` mit Reservisten (`[APPROX]`). Belegt ist das nicht, weil kein Stand im Handmodus vorliegt.
  - Für Gesperrte gibt es in den Ständen keinen Fall. Die Auswahl 0x42838 schließt `+0x09 & 3` aus, beide Bits.
- **Fundstelle:** FILE05 → FILE24: STEFAN `+0x09` 00→02, `+0x0A` 1→0, `+0x0D` 0→10, `+0x17` 00→10, `+0x15` 3S bleibt;
  CHAIDAROV ebenso mit `+0x17` = 0x0C, `+0x15` 1S. FILE25 unverändert verletzt, Reihen neu (LALA 3S→2S, BRANDL 2S→3S
  nach ST 69→68). em.exe `Datei 0x5C44`/`0x5C50` (Verletzung), `0x388F6` (Zählung laut Code), `0x54FCE–0x54FE8`
  (0x375 → 0x376, 0x375 = 0), `0x54D8C–0x54D9E` und `0x552F9–0x5530B` (zurück, dann 0x426BC). `+0x15`-Schreiber: `0x2799C`,
  `0x29E5B`, `0x40CF8`, `0x41210`, `0x4158A`, `0x415AB`, `0x42777`, `0x42816`, `0x4776B`, `0x4988F`, `0x5B084`. Bilder
  `file24_2_aufstellung_im_spiel_stefan_verl.png`, `file24_3_popup_spieler_verletzt_im_spiel.png`, `file24_5…`, `file25_2_aufstellung.png`.
- **Umsetzung:** Ziel: `[APPROX]` in `StadiumStatistics.cs:107` → `[ORIGINAL]` für Verletzte (Fundstellen oben). Den Satz „ob das
  Original sie vorher aus der Aufstellung nimmt“ ersetzen durch: „`+0x15` bleibt, `+0x0A` = 0 (Datei 0x5C50); gezählt wird beides“.
  Gesperrte bleiben `[APPROX]`, ebenso die Neuaufstellung von Hand in `Squad.Lineup.cs:355`. Der Code-Hinweis „keine
  Neuaufstellung ohne Einstellung“ wird dort eingetragen. Dateien: `src/EishockeyManager.Core/StadiumStatistics.cs`,
  `Squad.Lineup.cs`. Testidee: Auto-Aufstellung an, Verletzung im Spiel → im Spiel „Eingesetzt“ 21 und Lücke, nach der
  Abrechnung 22 nach `AutoLineupRule`. Mit acht gesunden Aufgestellten und einem verletzten ist der Verein nicht
  spielfähig. Spur: `kader-start`.

### Offene Fragen dieses Abschnitts

1. **A088, Spur und Dateigrenze:** Die Abweichung betrifft `ResetMatchCounters` in `MatchEngine.Live.cs` und `Join` in
   `Player.Factory.cs`, nicht `SeasonFlow*`. Übernimmt das `saison` mit minimaler Änderung, oder `kader-start`, der
   `Player*.cs` gehört? Ist der Modus „nur Weltkugel“ gemeint, ist zu prüfen, ob 0x26F20 dort läuft. Bei [0x386] = 0 hängt
   die Poolschleife an [0xA70E] + 1 = [0x386]; nicht geprüft.
2. **Einstellung „automatisch aufstellen“ von FILE24 nach FILE25:** In FILE24 ist 0x375 = 0 bei 0x376 = 1, in FILE25 wieder 1.
   Den Nutzer fragen, ob er dazwischen die Taktik besucht hat (Umschalter Datei 0x47F4E/0x49058). Hat er das nicht, schaltet
   die Wiederherstellung 0x54D8C (0x376 → 0x375) die von Hand abgeschaltete Einstellung am nächsten Spieltag wieder ein.
   Das wäre ein eigener Befund für `kader-start`; die Ablaufreihenfolge in 0x54C9F ist nicht gelesen.
3. **Fehlende Aufnahme für A167/A189 (Handmodus):** FILE05 laden, einen Spieler von Hand umstellen (Einstellung aus), einen
   Spieler kaufen und sichern, dann einen Spieltag mit Verletzung spielen und sichern. Damit ließe sich klären, ob der Kauf
   ohne Einstellung uneingesetzt bleibt und ob die Lücke nach einer Verletzung bleibt.
4. **Abgang am Saisonende** (A085/A091): Ein Paar mit auslaufendem Vertrag oder Karriereende fehlt. Geprüft ist das
   Aufrücken nur beim Verkauf.

## Sitzung 4 – Vereinsplatz beim Start (U15/D7) und R-K5 (FILE27, FILE28)

Werkzeuge: `hok.load` (Vereinssätze far:9657 ab HOK 0x018A1, 48 × 0x3D; Tabelle DGROUP 0x5CCB ab HOK 0x08F97; Regler
DGROUP 0x118–0x11B ab HOK 0x000E2; Meister DGROUP 0xD0E6 ab HOK 0x00E82), Skripte `/root/s/py/s4clubs.py`, `/root/s/py/s4tv.py`;
`emtool vereine` (Datenbankreihenfolge); Disassembly Datei 0x28165 ff., 0x29B60–0x29DA4, 0x43960–0x43B40.

Datenbankreihenfolge (Bezug für „Remake-Index“): 0 Eisbären … 2 Düsseldorf, 5 Kölner EC, 8 Mannheim, 9 München; 12–23
2. Liga (16 EC Hannover, 23 ES Weißwasser); 24–35 Oberliga; 36–47 ohne Liga.

### U15-a – Start-Liga bei fest gewähltem Verein (Tausch)

- **Ergebnis:** widerlegt (Remake-Annahme „fester Verein bleibt in seiner Liga“; die Original-Regel ist gelesen). Das Original setzt
  jeden Managerverein in die gewählte Start-Liga: Er tauscht mit einem Zufallsplatz r der Start-Liga (ohne Vereine früherer
  Manager, ohne Europapokal-Eintrag Zeile 1) per Datei 0x36169 mit Schalter 0x80 – Name, Logo (Feld 0x34), Manager (0x16),
  Wort 0x3B, Spieler (+0x1D) und Managerzuordnung wandern, die **Teamwerte 0x17–0x1D bleiben am Platz**. Der verdrängte Verein
  übernimmt den alten Platz (und damit die alte Liga) des Managervereins. Behalten wird nur, wenn Start-Liga ≠ Bundesliga, der
  Verein schon in ihr steht und Index ≤ DGROUP 0x7CB (35); bei Start-Liga Bundesliga wird immer getauscht (notfalls innerhalb
  der Bundesliga), ein ligaloser Verein (36–47) landet direkt in der Start-Liga (auch Bundesliga/2. Liga), nicht pauschal in
  der Oberliga.
- **Fundstelle:**
  - em.exe: L = DGROUP 0x11A → [bp−0x5E] (Datei 0x29BCE/0x29BD2); Behalten-Prüfung Datei 0x29C3B–0x29C75 (L ≠ 0,
    Datei 0x34FD5(v)/2 = L, v ≤ [0x7CB]); Zufallsplatz Datei 0x29C78–0x29C91 `random([0x7C0+L], [0x7C0+L]+[0x7B1+L]−1)`;
    Ausschluss früherer Manager Datei 0x29C9B–0x29CC3 (Schleife si < DGROUP 0xA70E); Europapokal-Filter Datei 0x29CCC
    (0x45CD0(r, 1)); Tausch Datei 0x29CF2 `0x36169(v, r, 0x80)`. Ablauf: Ligamischung Datei 0x43A17 (0x35EDD) läuft **vor** der
    Spielvorbereitung Datei 0x43A61 (0x28165); nach dem Tausch keine weitere Mischung (0x43A66–0x43B40: 0x9D1B, 0x57E3, 0x341AB …).
  - FILE27 (Köln, L = 1, Regler 118–11B = 3/3/1/1): KÖLNER EC Satz 18 (2. Liga), **ES WEIßWASSER** (DB 23, 2. Liga) Satz 10 in
    der Bundesliga. Teamwerte 0x17–0x1D: Weißwasser [50, 91, 94, 91, 94, 50, 53] (Bundesliga-Niveau), Köln [49, 55, 55, 53, 55,
    51, 49] (2.-Liga-Niveau) – die Werte sind am Platz geblieben, Name/Logo/Spieler gewandert.
  - FILE28 (Köln, L = 1, Dauer 1): KÖLNER EC Satz 12, **EC HANNOVER** (DB 16) Satz 3 in der Bundesliga, Teamwerte Hannover
    [51, 90, 94, 93, 90, 52, 51], Köln [47, 55, 54, 55, 54, 53, 53].
  - FILE15 (Bayreuth, L = 1): SV Bayreuth bleibt in der 2. Liga (Satz 23), kein Bundesligafremder in der Bundesliga – Behalten-Zweig.
  - FILE01 (Köln, L = 0): Köln Satz 2, FILE10 (Mannheim, L = 0): Satz 1; Bundesliga unverändert besetzt – Tausch innerhalb der
    Bundesliga nicht vom Mischen unterscheidbar (verträglich, nicht einzeln belegbar). Meister DGROUP 0xD0E6 = 8 bzw. 6 (Düsseldorf),
    nicht der Managerplatz (Europapokal-Filter verträglich).
  - Bilder `file27_1_spielvorbereitung_zweite_liga_dauer3.png`, `file28_1_…_dauer1.png` (Köln gewählt, Start-Liga Zweite Liga).
- **Umsetzung:** Ziel: `NewGame.AssignClubs` – fester Verein v mit Start-Liga L: behalten nur bei L ≠ Bundesliga, Liga(v) = L
  und v ≤ 35; sonst Zufallsplatz r der Start-Liga (ohne Vereine früherer Manager, ohne Europapokal-Teilnehmer), Liga von v und r
  tauschen, r erhält den alten Platz von v (ligalos möglich), in `LeagueSlots` die Plätze tauschen, die Teamwerte (Remake-Felder
  zu 0x17–0x1D) **beim Platz lassen**, also zwischen v und r tauschen. `[APPROX]` „Verein ohne Liga rückt in die Oberliga“
  (A112) entfällt. Hinweis: Das Original prüft nur Vereine **früherer** Manager; ein späterer Manager, dessen Verein als r
  gezogen wird, wandert mit (0x36169 tauscht die Managerzuordnung mit). Dateien: `src/EishockeyManager.Core/NewGame.cs`,
  `src/EishockeyManager.Core/SeasonFlow.LeagueSlots.cs` (Anschluss Startpermutation, ggf. über `GameState`-Teildatei), Doku
  `docs/transfer.md`/`spielberechnung.md` §14. Testidee: Köln (DB 5) fest, Start-Liga 2. Liga → Köln in 2. Liga, genau ein
  früherer Zweitligist in der Bundesliga mit Kölns alten Teamwerten; Mehrsaisonprobe (4 Wechsel, passiver und aktiver Manager)
  laut D7. Spur: `kader-start`.

### U15-b – „Eine Saison“ nur mit Start-Liga Bundesliga (Sperre im Spielvorbereitungsbildschirm)

- **Ergebnis:** belegt. Nach jedem Reglerschritt (alle vier Regler, gleiche Routine) prüft das Original: Dauer (DGROUP 0x119)
  = 0 **und** Start-Liga (DGROUP 0x11A) ≠ 0 → der Schritt wird **zurückgenommen** (Wert − Schritt), ohne Meldung. Es wird weder
  die Dauer hochgesetzt noch die Liga zurückgestellt: Mit Dauer 0 bleibt der Start-Liga-Regler auf Bundesliga stehen, mit 2. Liga
  oder Oberliga bleibt der Dauer-Regler auf 1 (drei Saisons) stehen. Gesperrt sind genau Dauer 0 × Start-Liga 1/2; danach
  Begrenzung 0 … DGROUP 0x11C + i (5, 3, 2, 2).
- **Fundstelle:** em.exe Datei 0x2890A `add [bx+0x118], al` (al = Schritt ±1 aus [bp−0x58], bx = Regler 0–3); Datei 0x2890E
  `cmp byte [0x119],0` / 0x28915 `cmp byte [0x11a],0` / 0x2891C `sub [bx+0x118], al` und Sprung über Neuzeichnen (0x2899D);
  sonst Untergrenze Datei 0x28925–0x2892C, Obergrenze 0x28936–0x28944, Neuzeichnen 0x2894A–0x28987. Bilder
  `file28_3_spielvorbereitung_bundesliga_kuerzeste_dauer.png` (Bundesliga, Regler ganz links = eine Saison),
  `file28_4_…_zweite_liga_kuerzeste_dauer.png`, `file28_5_…_oberliga_kuerzeste_dauer.png` (Regler auf drei Saisons stehen
  geblieben); FILE28 Regler 3/1/1/1 (Spielende 1996), FILE09 Dauer 0 mit Liga 0 (Spielende 1994). Nicht ausgewertet: die
  Bedingung Datei 0x289A9–0x28A69 (Level > 2, Stärke, Dauer/Liga → Wert DGROUP 0x256), gehört nicht zur Sperre.
- **Umsetzung:** Ziel: `SetupScreen.Step` nach dem Schritt: wenn `Duration == EineSaison` und Start-Liga ≠ Bundesliga, alten
  Wert behalten (kein Ersatz-Umstellen). Da das Remake die Start-Liga je Manager führt, das Original aber einen gemeinsamen
  Wert DGROUP 0x11A hat, die Prüfung für Dauer gegen **alle** Manager bzw. für die Liga gegen die Dauer führen (oder Start-Liga
  global machen, siehe offene Frage). Kommentar `[ORIGINAL] Datei 0x2890E–0x2891C`. Dateien:
  `src/EishockeyManager.UI/Screens/SetupScreen.cs`, ggf. `src/EishockeyManager.Core/GameSetup.cs` (`SetupSliders`, Regel in
  Core testbar). Testidee: Dauer auf 0, Start-Liga hoch → bleibt Bundesliga; Liga 2. Liga, Dauer runter von 1 → bleibt 1.
  Spur: `kader-start`.

### U15-c – Sonderfall „eine Saison, ein Manager“

- **Ergebnis:** nicht entscheidbar per Spielstand (Code gelesen). Bei neuem Spiel (DGROUP 0x386 beim Eintritt 0, [bp−0x64]),
  Spielende DGROUP 0x5A14 = 1994 und DGROUP 0x386 = 1 setzt das Original nach dem Tausch Managerfeld DGROUP 0x9744 + 0x1AF · m
  = 1 und tauscht den Managerverein nochmals auf einen Zufallsplatz 0–11 **mit** Europapokal-Eintrag (Zeile 1). Wegen U15-b ist
  dabei die Start-Liga immer Bundesliga.
- **Fundstelle:** em.exe Datei 0x281A1 ([bp−0x64] = [0x386]), Datei 0x29CFA–0x29D23, Zufallsplatz 0x29D28–0x29D7F,
  Tausch Datei 0x29D9C `0x36169(v, r, 0x80)`. FILE09 hat Dauer 0, aber zwei Manager (Sonderfall greift nicht).
- **Umsetzung:** Ziel: im selben Umbau wie U15-a nachziehen oder als `[APPROX]` mit Fundstelle markieren; Beleg fehlt:
  DOSBox-Spielstand „neues Spiel, ein Manager, Dauer eine Saison, sofort gesichert“ (Managerverein auf einem Europapokal-Platz,
  Feld +0x9744−0x965B = Managersatz +0xE9 = 1). Dateien: `src/EishockeyManager.Core/NewGame.cs`. Testidee: ein Manager,
  EineSaison → Managerverein ist Europapokal-Teilnehmer. Spur: `kader-start`.

### R-K5 – Reihenfolge der Vereinsauswahl innerhalb einer Seite

- **Ergebnis:** widerlegt (Remake-Annahme „Datenbankindex“; Ursache im Original gefunden). Die Vereinsauswahl Datei 0x28030 zeigt
  die Vereinssätze 12p … 12p + 11 in Satzreihenfolge, und die Sätze sind beim Öffnen der Spielvorbereitung **schon gemischt**:
  Beim Spielstart läuft die Ligamischung 0x35EDD (55 Tausche je Liga 0–2, voller Satztausch) vor dem Aufruf der
  Spielvorbereitung. Die beobachtete Reihenfolge ist also eine zufällige Permutation je Programmstart (srand(time) Datei
  0x439F5), Seiten 0–2 gemischt innerhalb der Liga, Seite 3 (36–47, ohne Liga) in Datenbankreihenfolge.
- **Fundstelle:**
  - em.exe Startablauf: Datei 0x439BB Lader 0x2EF1B, 0x439F5 0x25B31 (srand(time)), 0x439FA 0x25AF9, 0x439FF DGROUP 0x386 = 0,
    0x43A08–0x43A12 [0x5CCB] = 2, [0xD0E6] = 2, **Datei 0x43A17 `call 0xF0:0x43` → 0x35EDD**, danach 0x346D5, 0x34F7D und
    erst **Datei 0x43A61 `call 0xD8:0x2F` → 0x28165** (Spielvorbereitung mit Vereinsauswahl 0x28030, Aufrufe 0x282D7/0x28E6E).
    Kein weiterer Aufruf von 0x35EDD bis zum Hauptmenü (0x43A66–0x43B40).
  - Video `oberflaeche_0136.png` (Seite 3): TUS Geretsried, EHC Straubing, 1. EV Weiden … Zelle 8 „HEC BONN“ (= DB 44 = 36 + 8)
    – Datenbankreihenfolge, wie bei ungemischter Seite 3 erwartet. `oberflaeche_0128.png` (Seite 0): Plätze 5, 3, 1, 4, 9, 0, 8,
    6, 2, 7, 10, 11 – dieselben 12 Bundesligisten permutiert (Mischung nur innerhalb der Liga), `_0133` Seite 1 ebenso.
  - Spielstände: Vereinsindizes je neuem Spiel neu verteilt, Seite 3 immer gleich: Bundesliga-Satzfolge FILE01 Freiburg,
    Krefeld, Köln, Preussen …; FILE10 Freiburg, Mannheim, Köln, Preussen …; FILE27 Kaufbeuren, Eisbären, Preussen, Rosenheim …;
    FILE28 Landshut, Preussen, Düsseldorf, Hannover …; Sätze 36–47 in allen vier Ständen = DB 36–47. Logo-Feld 0x34 wandert
    mit dem Satz (FILE27/28 f34 je Platz verschieden, 36–47 gleich: 60, 44, 27, …).
  - Remake: `SetupScreen.ClubAt` = `_clubPage * 12 + cell` (DB-Index), `DrawClubPicker` liest `db[ClubAt(k)]`;
    `GameState.InitializeLeagueSlots` mischt erst in `NewGame.Create` nach der Vereinsvergabe.
- **Umsetzung:** Ziel: Startpermutation (0x35EDD-Mischung der Ligen 0–2) **vor** der Spielvorbereitung im Core erzeugen (z. B.
  beim Anlegen des `GameSetup` aus dem Spielstart-Seed, nicht durch das Öffnen des Bildschirms), `ClubAt(cell)` = Satz
  12p + cell dieser Permutation (Seite 3 unverändert), und dieselbe Permutation in `NewGame` als Anfangs-`LeagueSlots`
  übernehmen; danach der Managertausch aus U15-a. `[APPROX]` in `ClubAt` → `[ORIGINAL] Datei 0x43A17 → 0x28030`;
  Zufallsquelle bleibt `[APPROX]` (Original srand(time)). Dateien: `src/EishockeyManager.UI/Screens/SetupScreen.cs`,
  `src/EishockeyManager.Core/GameSetup.cs`, `src/EishockeyManager.Core/NewGame.cs`,
  `src/EishockeyManager.Core/SeasonFlow.LeagueSlots.cs` (Anfangspermutation annehmen). Testidee: Seiten 0–2 enthalten je die
  12 Vereine der Liga in der Reihenfolge der Startpermutation, Seite 3 = DB 36–47, und `LeagueSlots` des neuen Spiels beginnt
  mit genau dieser Reihenfolge (bis auf den Managertausch). Spur: `kader-start` (Anschluss `SeasonFlow.LeagueSlots.cs` mit der
  Spur `saison` abstimmen).

### Offene Fragen dieses Abschnitts

1. Start-Liga, Level und Start-Stärke liegen im Original als **ein** Satz DGROUP 0x118–0x11B (README FILE09: „Start-Liga gilt
   für alle Manager“), das Remake führt sie je Manager. Soll `kader-start` mit U15-b die Start-Liga global machen, oder nur
   die Sperre gegen alle Manager prüfen? (Ob das Original beim Managerwechsel Werte je Manager umkopiert, ist nicht geprüft.)
2. U15-c (Europapokal-Platz bei einer Saison und einem Manager): umsetzen oder als `[APPROX]` lassen; Beleg wäre ein
   DOSBox-Stand „ein Manager, Dauer eine Saison“.
3. R-K5 berührt `SeasonFlow.LeagueSlots.cs` (Spur `saison` in Runde 13): Dateigrenze für `kader-start` freigeben?

## S-K6-Rest – Mischzeitpunkt, Zufallsquelle, Fair-Play-Prämie

Grundlage: Disassembly `em.exe` (Datei 0x54C9F-Saisonwechsel, 0x35EDD, 0x36169, 0x2A1E0, 0x26F20, 0xBFD5, 0x5667A).
Spielstände FILE13/14, FILE16A/17, FILE22/23 (Strafminuten = Tabellensatz DGROUP 0x8257 + 0x50 · v, Feld +0x4A, HOK
0x03C5F + 0x50 · v + 0x4A; Vereinsnamen far:9657), Bilder `file13_15`, `file16_8`, `file16_9`, `file23_1`.

**Ablauf am 29. April (Datei 0x54C9F, gelesen):**

| Schritt | em.exe | Bemerkung |
|---|---|---|
| Ranglisten je Manager (Topscorer, Torhüter) | Datei 0x5576B `0xC8:0x20(1)` | **alte** Liga |
| Tabellenfelder auf 0 | Datei 0x55DD1–0x55EF8 | Strafminuten (+0x4A) werden **nicht** gelöscht (Schreibzugriffe dort auf +0x03, +0x19, +0x1D, +0x21, +0x29, +0x2D u. a., kein +0x4A) |
| Zwangsabstieg, Austausch (Auf-/Abstieg) | Datei 0x55F17–0x563A4 (0x562F2, 0x5636D) | Tausch 0x36169 Schalter 1 |
| Ewige Tabelle **und Bildschirm „Saisonende“ (Spalten je Liga)** | Datei 0x563AF → 0x5667A → Datei 0x56682 `0x120:0x84` = 0x4C747 | `file13_14`, `file16_8` |
| Liga-Stufe je Manager neu | Datei 0x563B2–0x56418 (schreibt DGROUP 0x9746 in Datei 0x56409) | aus dem neuen Index |
| letzte Saison → Highscore/Ende | Datei 0x5641A–0x56444 | |
| Teamwert-Wanderung Modus 10 | Datei 0x56448 `0x28:0x1C5B` = 0x09D1B | zieht lrandom |
| **Ligamischung** | **Datei 0x5644E** `0xF0:0x43` = 0x35EDD | |
| 0x346D5 | Datei 0x56453 `0xE8:0x8E` | nicht gelesen |
| **Fenster „Saisonende/Ereignisse“, Prämien** | **Datei 0x5645D** `0xD0:0x20` = 0x26F20 (Fair-Play Datei 0x27213) | `file13_15`, `file16_9`, `file23_1` |
| `0x140:0x48(0)`, `(1)`, Saisonzähler DGROUP 0x3BD + 1 | Datei 0x56467–0x56476 | |
| Strafminuten aller 64 Vereine auf 0 | Datei 0x5647A–0x56497 | |

### S-K6-a – Mischzeitpunkt

- **Ergebnis:** widerlegt (das Remake mischt in `StartNextSeason`, das Original am 29. April vor dem Fenster 0x5645D; die
  Beschreibung im Remake-`[APPROX]` stimmt, das Verhalten weicht ab).
  Die Mischung 0x35EDD läuft am 29. April nach Auf-/Abstieg, Ewiger Tabelle, dem Spalten-Bildschirm „Saisonende“ 0x4C747,
  der Liga-Stufe und der Teamwert-Wanderung, aber **vor** dem Ereignis-/Prämienfenster 0x26F20 und vor dem Löschen der
  Strafminuten. Die zwei „Saisonende“-Bilder liegen also auf verschiedenen Seiten der Mischung: Die Spalten (0x4C747, liest
  die Auf-/Absteigerlisten DGROUP 0xD0E7 ff. über Indizes, Datei 0x4CBDB) sehen die ungemischten Plätze, die Prämien die
  gemischten. Weitere Aufrufer von 0x35EDD gibt es nur beim Spielstart (Datei 0x43A17 in 0x437D5); der in
  `docs/spielberechnung.md` §14 genannte Aufruf „nach dem Lader 0x439BB“ ist falsch (Datei 0x439BB ruft 0x2EF1B).
- **Fundstelle:** em.exe Datei 0x56445–0x5647A (Folge oben), Datei 0x563AF/0x56682 (0x4C747 aus 0x5667A), Aufruferliste
  0x35EDD = {0x437D5, 0x54C9F}. Spielstände bestätigen die Mischung je Liga (Zeitpunkt nur aus dem Code, die Stände liegen
  am 14.04. bzw. 10.09.): FILE13 → FILE14 Krefelder EV 27 → 24 (der Doppeltausch lässt ihn auf 27, also hat die Mischung
  ihn bewegt), TEV Miesbach (Nachrücker) 40 → 35 (Tauschpartner Wedemark) → 32; FILE16A → FILE17 SV Bayreuth 23 → 0
  (Bundesliga); FILE22 → FILE23 SV Bayreuth 23 → 35 (Oberliga), Miesbach 40 → 24, Königsbrunn 41 → 30, in allen drei Ligen
  wechseln die übrigen Mitglieder ihre Plätze (z. B. Bundesliga Mannheim 1 → 2, Freiburg 10 → 4; 2. Liga Hannover 21 → 13).
- **Umsetzung:** Ziel: Satzfolge der neuen Saison schon in `FinishSeason` (29. April) bilden – Partnerersatz wie
  `AdvanceLeagueSlots` aus `record` (Liga der nächsten Saison über `record.NextLeague`), dann mischen – und speichern;
  `StartNextSeason` übernimmt sie nur noch (nicht erneut mischen). Der Spalten-Bildschirm „Saisonende“ braucht nichts (er
  zeigt im Remake ohnehin Vereine, keine Plätze). Achtung: Zwischen 29.04. und `StartNextSeason` gehört die gespeicherte
  Folge schon zur neuen Mitgliedschaft; `LeagueSlots(league)` filtert nach `LeagueClubs` (alte Ligen) – dort entweder
  alte und neue Folge getrennt führen oder die Abfrage in dieser Phase auf die neue Folge umstellen (Fair-Play-Tabelle im
  Statistikmenü zeigt im Original ab 29.04. die neue Liga des Managers mit 0 Minuten in Satzfolge). Schema nur, wenn eine
  zweite Liste gespeichert werden muss. Dateien: `src/EishockeyManager.Core/SeasonFlow.LeagueSlots.cs`,
  `src/EishockeyManager.Core/SeasonFlow.cs` (`FinishSeason`, `StartNextSeason`). Testidee: nach `FinishSeason` ist die Folge
  der nächsten Saison gesetzt und `StartNextSeason` ändert sie nicht mehr; Mehrsaisonprobe (4 Wechsel) bleibt grün.
  Spur: `saison`.

### S-K6-b – Zufallsquelle der 55 Tausche

- **Ergebnis:** nicht entscheidbar (Zustand des Generators nicht im Spielstand, `[APPROX]` bleibt); die Begründung ist
  falsch (Code-Kommentar und `re-runde-12.md` nennen `random(b, b + 11)`): Länder- und
  Ligatausche ziehen aus **lrandom** Datei 0xBFD5 (`0x38:0x7E5`), nicht aus C-`rand`/`random` Datei 0xC037. lrandom ist ein
  eigener Generator x = (16 · x + 167) mod 4.000.000, Ergebnis a + x · (b − a + 1) / 4.000.000, Zustand 32 Bit DGROUP
  0xE017/0xE019; ihn benutzen auch Spielberechnung (0x87F7, 0x9F5C, 0xA484, 0x36B5D, 0x37A62), Teamwert-Wanderung 0x09D1B
  und weitere (0x25AF9, 0x35818, 0x52430, 0x52520). Der Zustand wird nirgends gesetzt außer in 0xBFD5 selbst, startet mit 0
  (EXE-Abbild) und steht **nicht** im Spielstand – die Folge hängt von allen lrandom-Zügen seit Programmstart ab. Eine
  zugexakte Nachbildung ist daher nicht möglich und nicht sinnvoll; die Verteilung (55 Tausche zweier gleichverteilter Plätze,
  auch derselbe) bildet das Remake schon nach. Das `[APPROX]` der Zufallsquelle bleibt, nur die Begründung ist zu berichtigen.
- **Fundstelle:** em.exe Datei 0x35F02/0x35F1D (Länder, 20-mal), Datei 0x36013/0x3602E (je Liga, Zähler bis 0x37 = 55, Datei
  0x3612D), Rumpf Datei 0xBFD5–0xC036 (Konstanten 0x3D0900 = 4.000.000, ×16 über 0x0:0x52A, +0xA7); `--xref` DGROUP
  0xE017–0xE01A: sechs Zugriffe, alle in 0xBFD5. EXE Datei 0x24DE7 (= DGROUP 0x16DD0 + 0xE017, im Ladeabbild bis 0x255B0):
  `00 00 00 00`. Blocktabelle `docs/formate/spielstand-original.md` endet bei DGROUP 0xD294 + 2712 = 0xDD2C, 0xE017 fehlt.
  Nebenbefund: ab x = 0 durchläuft lrandom nach 2 Vorschritten einen Zyklus von nur 15.625 Werten (x ≡ 7 mod 16).
  Reihenfolge im Saisonwechsel: Teamwert-Wanderung (Datei 0x56448) zieht unmittelbar vor der Mischung aus derselben Folge.
- **Umsetzung:** keine Verhaltensänderung; `[APPROX]` bleibt mit berichtigter Begründung („lrandom Datei 0xBFD5, gemeinsame
  Folge mit Spielberechnung und Teamwert-Wanderung, Zustand DGROUP 0xE017 nicht im Spielstand → nicht nachbildbar“), dazu
  `random(b, b + 11)` → `lrandom(b, b + 11)` im Kommentar `src/EishockeyManager.Core/SeasonFlow.LeagueSlots.cs`; Doku
  `docs/analyse/re-runde-12.md` (S-K6, Punkte 1 und 2) und `docs/spielberechnung.md` §14 (Aufruf 0x439BB streichen) –
  **nicht `saison`, sondern `pruefung-karte` (L12); erledigt in PR #608 (Teil 2).**
  Testidee: keine (reine Begründung); bestehende Verteilungs-/Mehrsaisonprobe genügt. Spur: `saison` (Code-Kommentar), Doku `pruefung-karte` (L12).

### S-K6-c – Fair-Play-Prämie alte/neue Liga

- **Ergebnis:** widerlegt (Remake rechnet über die alte Liga und die alte Satzfolge). Das Original wertet die Fair-Play-Prämie
  über die **neue** Liga des Managers in der **neuen, gemischten** Satzfolge, mit den Strafminuten der abgelaufenen Saison, die
  mit dem Vereinssatz gewandert sind. Erster ist der Verein mit den wenigsten Minuten, bei Gleichstand der kleinste Satzplatz.
  Folge: Die beiden Nachrücker aus den ligalosen Vereinen 36–47 haben immer 0 Minuten – ein Oberliga-Manager (auch ein in
  die Oberliga abgestiegener) bekommt die Prämie nur mit 0 Minuten und einem Satzplatz vor beiden; ein Aufsteiger wird mit
  den Minuten der höheren Liga verglichen. Topscorer und Torhüter (Datei 0x5576B) bleiben bei der alten Liga.
- **Fundstelle:** 0x2A1E0 liest die Liga aus dem Managerfeld DGROUP 0x9746 (Datei 0x2A1F2), Basis `[0x7C0 + Liga]`; 0x9746
  wird erst in Datei 0x56409 aus dem Index nach dem Austausch gesetzt (Vergleich mit DGROUP 0x7C1/0x7C2). Die Satzfolge ist
  zu diesem Zeitpunkt schon gemischt (0x5644E vor 0x5645D). Strafminuten DGROUP 0x82A1 + 0x50 · v = Tabellensatz 0x8257
  + 0x4A, den 0x36169 ganz mittauscht (Datei 0x364BC–0x364FA, 0x50 Byte ab 0x8257); gelöscht erst Datei 0x5648A nach dem
  Fenster, nicht in Datei 0x55DD1–0x55EF8. Sortierung Datei 0x2A21D–0x2A292 tauscht nur bei echt mehr Minuten; Prämie, wenn
  Vereinsfeld +0x16 des Ersten = Manager + 1 (Datei 0x2A29D). Spielstände (widerspruchsfrei, aber nicht trennscharf):
  - **FILE13 → FILE14** (Krefelder EV, Prämie 100.000 DM, `file13_15`): Kandidaten mit 0 Minuten in der neuen Oberliga sind
    Krefeld und der Nachrücker TEV Miesbach; Krefeld steht in FILE14 auf Satz **24** (erster Platz der Oberliga), Miesbach
    auf 32 → Krefeld gewinnt den Gleichstand, passt zur neuen Folge. Die Minuten vom 29.04. stehen in keinem Stand (FILE13
    ist gepatcht, alle 0).
  - **FILE22 → FILE23** (Bayreuth, Zwangsabstieg, „Prämien: keine“, `file23_1`): FILE22 (14.04.) hat die endgültigen Minuten
    (danach keine Ligaspiele): Bayreuth 27; alte 2. Liga Erster EC Hannover 22; neue Oberliga: Miesbach (Satz 24) und
    Königsbrunn (30) mit 0, ESC Wolfsburg 4 → Miesbach. Beide Lesarten ergeben „keine“.
  - **FILE16A → FILE17** (Bayreuth, Aufstieg, 200.000 DM = Topscorer + Fair-Play, `file16_9`): FILE16A nur nach dem 43.
    Spieltag (2 Spiele): Bayreuth 19; in der 2. Liga darunter Weißwasser 14, Frankfurt 16, Hannover 16, Augsburg 18 (Bayreuth
    dort Satz 23, verliert jeden Gleichstand); in der Bundesliga Krefeld 18, sonst ≥ 19 (Bayreuth in FILE17 Satz **0**,
    gewinnt jeden Gleichstand). Spricht eher für die neue Liga, entscheidet aber nicht (44. Spieltag fehlt).
- **Umsetzung:** Ziel: In `FinishSeason` die Fair-Play-Prämie über die Liga der nächsten Saison (`record.NextLeague` des
  Managervereins) und die nach S-K6-a vorab gebildete neue Satzfolge werten; Strafminuten wie am Saisonende (wandern im
  Remake ohnehin mit dem `Club`), Nachrücker mit 0. Torhüter/Topscorer unverändert alte Liga. Dateien:
  `src/EishockeyManager.Core/SeasonFlow.cs` (Aufruf `SinglePremiumEvents` trennen: Fair-Play mit neuer Liga/Folge),
  `src/EishockeyManager.Core/SeasonFlow.LeagueSlots.cs`; `src/EishockeyManager.Core/StatisticsClub.cs` (`FairPlayTable` mit
  übergebener Folge bzw. Liga, `SinglePremiumEvents`) gehört `ui-rest` – kleinsten Anschluss mit `ui-rest` abstimmen oder
  den Fair-Play-Ersten für die Prämie in einer `saison`-Datei berechnen. Testidee: Aufsteiger mit weniger Minuten als jeder
  Verein der höheren, aber mehr als ein Verein der alten Liga bekommt die Prämie (und umgekehrt nicht); Oberliga-Manager
  mit 0 Minuten gewinnt nur, wenn sein Satzplatz vor dem der Nachrücker liegt. Spur: `saison` (Anschluss `ui-rest`).
  Optionaler DOSBox-Gegenbeweis: FILE22 mit Konto über −666.667 DM (kein Zwangsabstieg, Bayreuth bleibt 2. Liga) und
  gepatchten Strafminuten Bayreuth 1, SC Riessersee (steigt in die 2. Liga auf) 0 → alte Liga: Prämie, neue Liga: keine
  (braucht ein `hok patch` für HOK 0x03C5F + 0x50 · v + 0x4A, gibt es so noch nicht).

### Offene Fragen dieses Abschnitts

1. Phase zwischen 29.04. und `StartNextSeason`: Soll die Fair-Play-Tabelle im Statistikmenü dort schon die neue Liga (Minuten
   0, neue Satzfolge) zeigen wie das Original, oder bleibt die Anzeige `[APPROX]`? Das bestimmt, ob eine zweite Satzfolge
   gespeichert werden muss (Schema).
2. Doku-Berichtigungen (lrandom statt random in `re-runde-12.md`/Code-Kommentar; Aufruf „nach dem Lader 0x439BB“ in
   `spielberechnung.md` §14 streichen) – an Spur `saison` mitgeben oder Leitstelle? **Entschieden (L12): `pruefung-karte`** (Doku), `saison` nur den Code-Kommentar.
3. 0x346D5 (`0xE8:0x8E`, direkt nach der Mischung, auch beim Spielstart) nicht gelesen – vermutlich Neuaufbau von Tabelle
   oder Spielplan; ggf. Prüfauftrag.

## Umsetzung

Je Punkt stehen Ziel, Dateien und Testidee im jeweiligen Abschnitt oben. Zuordnung zu den Spuren dieser Runde nach den
Entscheidungen der Leitstelle L1–L8 ([Kommentar in #567](https://github.com/kaaasi/eishockey-manager-redone/pull/567#issuecomment-6633257714)) und L9–L12 ([Ergänzung](https://github.com/kaaasi/eishockey-manager-redone/pull/567#issuecomment-6633831366)); bei Widerspruch zu den
Abschnitten oben gelten diese Entscheidungen:

- **`nationalteam`** (nach dem Merge dieses PRs): A009, A010, A028, A294 (Verhaltensänderung); A085 Gleichstand (Kandidatenfolge
  statt Name); A293 (Knopf nur aus dem Menü; „Endrunde“: Datei 0x3A88A lesen, sonst `[APPROX]`, L6); A023, A027, A284, A300 nur
  `[APPROX]` → `[ORIGINAL]`. Nebenbefunde Sitzung 1 (Sperre Transfer/Training ab der Gruppenphase, Kandidaten ohne
  Ausländerfilter, Länderwerte 0x18–0x1B über den Saisonwechsel) prüfen. Dateien außerhalb der eigenen Spur:
  `GameState.Integration.cs` nur für den Tageslauf des Bundestrainers (A009, A028), wenig und mit Begründung im PR (L9);
  `StatisticsStrength.cs` gehört `nationalteam` (L10).
- **`kader-start`:** U15-a (Tausch in die Start-Liga, `NewGame`), U15-b (Sperre im `SetupScreen`, D2; Start-Liga global wie im
  Original, L1), R-K5 (Startpermutation vor der Spielvorbereitung), A091 (Kaderplatzfolge statt Rückennummer), A166 (Rückfall
  `Club.AutoDressed`; `[ORIGINAL]` erst mit dem Regressionstest), A125/A167/A189 `[ORIGINAL]` mit Fundstelle; U15-c bleibt
  `[APPROX]` (L2). Mehrsaisonprobe für U15-a und A091. `GameSetup.cs` gehört `kader-start` (L11). `SeasonFlow.LeagueSlots.cs`
  nur für die Belegung beim Spielstart (U15-a, R-K5), kleine Änderungen, geänderte Methoden im PR nennen (L3). In
  `MatchEngine.Live.cs`/`Player.Factory.cs` nur die Kaderreihenfolge (L4). Schema 37 nur, falls L1 ein neues Feld braucht (L5).
- **`saison`:** A058 und A163 (Modus „nur Weltkugel“), S-K6-a (Satzfolge am 29.04. bilden, neue Liga ab 29.04. mit Migration,
  Schema 38, `Core/SaveGame.Migration.Saison.cs`, L5), S-K6-c (Fair-Play über die neue Liga), S-K6-b (Begründung des
  `[APPROX]` im Code-Kommentar von `SeasonFlow.LeagueSlots.cs`), A088 (Transfermarkt-Zähler, wenig in `MatchEngine.Live.cs`
  `ResetMatchCounters` und `Player.Factory.cs`, Begründung im PR, L4); 0x346D5 mitlesen und zuordnen (L8).
  `SeasonFlow.LeagueSlots.cs` für den Saisonwechsel (S-K6-a), Konflikte mit `kader-start` löst die Leitstelle bei der
  Integration (Folge `kader-start` → `saison`, L3). Mehrsaisonprobe Pflicht.
- **`ui-rest`:** nur der Anschluss `FairPlayTable` für S-K6-c (`StatisticsClub.cs`), mit `saison` abstimmen.
- **`pruefung-karte`** (Welle 2, L12): `docs/spielberechnung.md` §14 – Aufruf von 0x35EDD „nach dem Lader 0x439BB“ ist falsch
  (Aufrufer 0x437D5 Datei 0x43A17 und 0x54C9F Datei 0x5644E); `docs/analyse/re-runde-12.md` S-K6: `lrandom` statt `random`;
  neue Feldbelege aus Sitzung 2/3 für `docs/formate/spielstand-original.md` (Abschnitt Sitzung 2 und 3 oben). `saison` und
  `kader-start` ändern diese drei Dateien nicht, auch wenn die Abschnitte oben sie nennen (U15-a, S-K6-b), sondern nennen den
  Bedarf im PR.
- **Fehlende Aufnahmen** (Frage 7): entscheidet der Nutzer; bis dahin bleiben die betroffenen Punkte `[APPROX]` (L7).

## Offene Fragen an die Leitstelle (gesammelt)

Entschieden als L1–L8 im Kommentar der Leitstelle in #567 (siehe „Umsetzung“).

1. **Start-Liga global?** Original: ein Satz DGROUP 0x118–0x11B für alle Manager; Remake je Manager. `kader-start` die Start-Liga
   global machen lassen oder die Sperre U15-b gegen alle Manager prüfen?
2. **U15-c** umsetzen oder `[APPROX]`? Beleg wäre ein DOSBox-Stand „ein Manager, Dauer eine Saison, sofort gesichert“.
3. **Dateigrenze `SeasonFlow.LeagueSlots.cs`:** gehört `saison`, wird aber von R-K5/U15-a (`kader-start`) und S-K6-a (`saison`)
   berührt – Reihenfolge oder Freigabe festlegen.
4. **A088:** Änderung liegt in `MatchEngine.Live.cs` (`ResetMatchCounters`) und `Player.Factory.cs`, nicht in `SeasonFlow*` –
   `saison` mit minimaler Änderung oder `kader-start`?
5. **S-K6-a/c Schema:** Soll die Fair-Play-Anzeige zwischen 29.04. und `StartNextSeason` schon die neue Liga zeigen (zweite
   Satzfolge speichern, Migration) oder bleibt das `[APPROX]`?
6. **A293:** Weg zu den WM-Endrundenspielen aus dem Menü (Datei 0x3A88A) lesen oder als DOSBox-Bild nachfordern.
7. **Fehlende Aufnahmen** (Kandidaten für eine weitere DOSBox-Sitzung): Handaufnahme im WM-Kader nach „auto“ (A085); ganz leere
   Bundestrainer-Aufstellung mit Tageswechsel (A009); Handmodus Kader mit Kauf und Verletzung (A167/A189); Abgang am Saisonende
   (A085/A091); U15-c; Taktik-Besuch zwischen FILE24 und FILE25 beim Nutzer erfragen (DGROUP 0x375 0 → 1, möglicher Befund
   „Auto-Aufstellung schaltet sich wieder ein“, Datei 0x54D8C).
8. 0x346D5 (direkt nach der Mischung, auch beim Spielstart) ist nicht gelesen.

## Selbstprüfung

`python3 tools/reverse-engineering/funktionskarte.py gamedata/em.exe --repo . --csv <scratch>` ohne `--write` (Stand dieses
Branches): 679 Funktionen, 5349 Zuordnungen (Base `8afd750`: 5097); Spielcode umgesetzt 254, genähert 3, dokumentiert 106,
unbekannt 1 (Lader `tore\`, zurückgestellt bei `livespiel`), umgesetzt 90 % der Bytes; Gesamtzeile wie am Base: dokumentiert
154, unbekannt 257. Diese Datei ist Fundstelle in 69 Funktionen und ändert keinen Status gegenüber dem Base. Nach R13RD-01
(Spielstand-Offsets als `HOK 0x…`, Kopf ohne das Wort „Dateioffsets“, sonst liest die Karte jede Hexzahl als em.exe-Offset)
entfallen die im Review genannten falschen Zuordnungen zu vier Funktionen; die Funktion, die nur über den Strafminuten-Offset
im Spielstand „dokumentiert“ war, ist wieder unbekannt (Tabelle Base/Head im Übergabekommentar in #567). Den Stand in
`re-abdeckung.md` und die Zeilen in `inventur.md` schreibt `pruefung-karte` (Welle 2). Keine Originaldaten oder Bilder im Repo;
Bildnamen verweisen nur auf das Referenz-Repo.
