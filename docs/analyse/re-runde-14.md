# Auswertung DOSBox-Aufnahme `runde-13` (Spur `re-dosbox`, Runde 14)

Stand: Teil 1 abgeschlossen (geplanter Lauf Claude Opus 5.5, PR #628): Sitzungen 1 und 2, Spielstände FILE29–41 und FILE37A.
Korrekturen nach Review #643 (R14RD1-01 bis -05) durch die Leitstelle am 08.11.2026.
Teil 2 (PR #642): Sitzungen 3–7, FILE43–49A ohne FILE45, dazu FILE46 für das Leihende A115 (Abschnitt „Teil 2“).

Quellen: Spielstände und Bilder aus `kaaasi/eishockey-manager-referenz` (`original/spielstaende/`, README mit den Angaben des
Nutzers, Stand `36aeca9`), `emtool hok dump`, der entschlüsselte Klartext (Verfahren `docs/formate/spielstand-original.md`)
und das Disassembly von `em.exe` (`tools/reverse-engineering/disasm.py`). Schreibweise wie in `re-runde-13.md`: Adressen in
`em.exe` als `Datei 0x…`, DGROUP-Adressen als `DGROUP 0x…`, Offsets im entschlüsselten Spielstand als `HOK 0x…`.
Bildkoordinaten sind Spielkoordinaten (320 × 240): Die DOSBox-Bilder (642 × 507) zeigen das Spiel doppelt ab Pixel (1 | 26).
Die Farben der Bilder stimmen exakt mit der Palette `kb\21.vga` überein (6 Bit). Indizes sind daraus ablesbar, eindeutig bis auf
doppelte Farben der Palette: (97 | 0 | 16) = 16 oder 96, (243 | 195 | 113) = 31 oder 63, (32 | 48 | 81) = 29 oder 147. Gewählt ist
jeweils der übliche Index; für Pixeltests unerheblich, als `[ORIGINAL]`-Index „nach Farbe“ zu schreiben (R14RD1-04).

„Ergebnis“ bezieht sich auf die Remake-Annahme der jeweiligen `[APPROX]`-Stelle (`docs/analyse/inventur.md`): **belegt** =
Remake stimmt mit dem Original, **widerlegt** = Remake weicht ab (Umsetzung nötig), **nicht entscheidbar** = Beleg fehlt.

## Übersicht

| ID | Ergebnis | Kern | Spur |
|---|---|---|---|
| A056 | widerlegt | Bei „nur Weltkugel“ führt jeder Klick in die Managerzeilen auf Zeile 0 (Datei 0x28ACF): kein zweiter Manager, keine Meldung | `kader-start` |
| U15-c | belegt (Sonderfall) / widerlegt (Europapokal-Filter) | Ein Manager, eine Saison: Feld DGROUP 0x9744 = 1, Managerverein auf Platz 0. Der Filter 0x45CD0(r, 1) trifft beim Start nur Platz 0, nicht den Meister | `kader-start` |
| A180 | widerlegt | Eigene Taktikwerte und Vereinsfelder 0x26–0x3A beim Start 0, nicht die ausgeglichene Zeile | `kader-start` / `ui-rest` |
| A166 | belegt | Start: 16 Aufgestellte + 6 Stärkste eingesetzt (FILE29); Handmodus: kein Auffüllen (22 → 18) | `kader-start` |
| A082 | belegt | Satz des nicht belegten Managers 1: DGROUP 0x974B + 0x1AF bleibt 0 | `kader-start` |
| A046 | widerlegt | Tabellenfeld DGROUP 0x8288 + 0x50 · Verein vor dem 1. Spieltag 0 (Spielstart) bzw. Endstand der Vorsaison | `kader-start` |
| A373 | widerlegt | Pfeil **unten** = nächster Modus, oben = voriger, an den Enden kein Schritt (Datei 0x487A8–0x487B8, 0x48BD2–0x48C25); Remake umgekehrt und mit Umlauf (R14RD1-01) | `ui-rest` |
| Taktik 375h | widerlegt | Taktik oben links ist „automatisch aufstellen“ (DGROUP 0x375 + 2 · m), kein Torwartwechsel; zweites Byte 0x376 + 2 · m sichert die Einstellung über den Spieltag | `ui-rest` / `kader-start` |
| A364 | teils | Rechtsklick auf den Namen → Spielerdaten (belegt); STATUS leer bei nicht Eingesetzten (belegt); NR leer bei nicht Eingesetzten (widerlegt); Linksklick nicht aufgenommen | `ui-rest` |
| A012 | belegt | VERL./GESP. auch bei nicht eingesetzten Spielern | `kader-start` |
| A167 | belegt (auch Handmodus) | Kauf und Leihe: neuer Platz nicht eingesetzt, Reihe leer, auch bei ausgeschalteter Einstellung | `kader-start` |
| A091 | belegt | Kauf/Leihe STU hinten an (Art-Gruppe), Verkauf und Transferliste rücken auf | `kader-start` |
| A135 | widerlegt | Tore und Spiele je Wettbewerb (+0x03–0x05, +0x06–0x08, +0x1C/+0x1E/+0x20, +0x22/+0x24/+0x26); SP und TO zeigen die Summe | `kader-start` |
| A189 | belegt (Verletzte, auch Handmodus) / nicht entscheidbar (Gesperrte) | Verletzung löscht +0x0A in und außerhalb des Spiels, Reihe bleibt | `kader-start` |
| A380 | widerlegt | Kein „*“: Ein Spieler auf der Transferliste verlässt den Kader; in der Liste blaues „A“ und Statuszeile „von JOERG“ | `kader-start` / `ui-rest` |
| A382 | widerlegt | Markierung Index 16 (97 \| 0 \| 16), Trennlinie einzeilig Index 31 (243 \| 195 \| 113) | `ui-rest` |
| A377 | widerlegt | Kauf-/Leihfenster nur Titel, Linie und „Ihr Angebot:“; Rahmen (2 \| 29)–(166 \| 71) | `ui-rest` |
| A374 | belegt | Dialogschatten einfarbig Index 6 über beliebigem Hintergrund | `ui-rest` |
| A313 | nicht entscheidbar (Kursregler) / widerlegt (Betragsbalken) | Balken verschwindet, sobald die Maus die Zeilen 73–100 verlässt (Datei 0x59531–0x5955F), nicht erst beim Klick | `ui-rest` |
| A321 | teils | Kopffeld = Datum, weißes Feld = Text (zwei Zeilen), kein Zähler bei einer Nachricht | `ui-rest` |
| A357 | widerlegt | Balken Liga Index 29, DEB-Pokal Index 27 (nicht 200); Marke 2/4/6 + Stiel 2 Pixel breit | `ui-rest` |
| A115 | nicht entscheidbar (Teil 1) | Leihende liegt in FILE46 (ersetzt FILE42); Auswertung in Teil 2 | – |
| A088, A009, A085 | kein neuer Beleg | Sitzungen 1–2 enthalten keinen Saisonwechsel und keinen Bundestrainer; siehe Teil 2 | – |

## Sitzung 1 – Spielvorbereitung und Spielstart (FILE29–31)

Grundlage: FILE29 (neues Spiel, ein Manager „dsfs“, ESV KAUFBEUREN, nur Fahne, Level 3, Dauer 0 = eine Saison, Start-Liga
Bundesliga, Start-Stärke 1, sofort gesichert), FILE30 (gleicher Klartext, erster Heimspieltag noch nicht gespielt), FILE31
(nach dem 1. Spieltag, 13.09.1993). Bilder `file29_1…4`, `file30_1…3`, `file31_1`. Zum Vergleich FILE01/05/09/10/12/15/27/28.
Offsets: Managersatz m = HOK 0x00E87 + 0x1AF · m (DGROUP 0x965B), Vereinssatz i = HOK 0x018A1 + 0x3D · i, Pokal-/Europapokal-
Paarungen DGROUP 0x5CFB = HOK 0x08F0C (Zeile r = + 0x20 · r), Tabellenblock DGROUP 0x8257 = HOK 0x03C5F (0x50 je Verein),
Taktikwerte DGROUP 0xCAE9 = HOK 0x0BA26.

### A056 – „nur Weltkugel“ nur mit genau einem Manager

- **Ergebnis:** widerlegt. Das Original lässt einen zweiten Manager bei „nur Weltkugel“ gar nicht erst zu: Jeder Klick in die
  Managerzeilen wird auf Zeile 0 umgelenkt (Bearbeiten des ersten Managers). Umgekehrt sperrt ein zweiter Manager die Fahne
  (schon umgesetzt). Eine Meldung beim Start gibt es nicht.
- **Fundstelle:**
  - em.exe Datei 0x28AA9–0x28AD5: Zeile `di = (y − 0x26) / 0x33`, höchstens 3; bei Spielmodus-Bits `[bp−0x57] = 2`
    (nur Weltkugel) `di = 0`. Fahne gesperrt bei mehr als einem benannten Manager: Datei 0x28717–0x2873E.
  - Bilder `file29_1` (nur Weltkugel, ein Manager „sads“) und `file29_2` (Fahne + Weltkugel, zweiter Manager „sadas“);
    Angabe Nutzer: „Wenn „nur Weltkugel“ ausgewählt wird, kann man keinen zweiten Manager hinzufügen.“
- **Umsetzung:**
  - Ziel: Im `SetupScreen` bei `Mode == WorldOnly` einen Klick auf eine Managerzeile ≥ 1 wie einen Klick auf Zeile 0 behandeln
    (kein Hinzufügen). Die Meldung „Nur Weltkugel: nur ein Manager.“ und die Sonderbedingung in `GameSetup.CanStart` entfallen
    bzw. werden zur reinen Absicherung; `[APPROX]` in `GameSetup.cs` (CanStart) durch `[ORIGINAL] Datei 0x28ACF` ersetzen.
  - Dateien: `src/EishockeyManager.UI/Screens/SetupScreen.cs`, `src/EishockeyManager.Core/GameSetup.cs` (Regel testbar im Kern,
    z. B. `GameSetup.ManagerRowFor(row)`).
  - Testidee: Weltkugel an, Fahne aus, Klick auf Zeile 1 → kein zweiter Manager, Zeile 0 im Bearbeiten.
  - Spur: `kader-start`.

### U15-c – Sonderfall „eine Saison, ein Manager“

- **Ergebnis:** Sonderfall belegt; dabei widerlegt die Remake-Annahme zum Europapokal-Filter.
  - FILE29 erfüllt die Bedingung (neues Spiel, Spielende DGROUP 0x5A14 = 1994, DGROUP 0x386 = 1 Manager). Das Original setzt
    Managerfeld DGROUP 0x9744 + 0x1AF · m = 1 (in allen anderen Ständen 30 = 0x1E „nicht im Wettbewerb“, vgl. Highscore) und
    tauscht den Managerverein auf einen Platz r der Bundesliga mit 0x45CD0(r, 1) ≠ 0.
  - 0x45CD0(r, 1) durchsucht Zeile 1 der Tabelle DGROUP 0x5CFB mit 2 · [0x3B1 + [0x5CF4]] Einträgen. Beim Spielstart ist die
    Tabelle noch leer (Startwert 0 in em.exe, DGROUP 0x5CF4 = 0 → 64 Einträge); gefüllt wird sie erst nach der Spielvorbereitung
    (0x341AB über Datei 0x43A8C, Spielvorbereitung Datei 0x43A61). Damit trifft der Filter genau **Platz 0**, nicht den
    Deutschen Meister. Nach dem Füllen ist DGROUP 0x5CF4 = 1 (32 Einträge); in **Zeile 1** sind 8 belegt (FILE29 HOK 0x08F2C:
    148 75 9 110 138 125 95 114), die Einträge 8–31 bleiben 0 – Platz 0 gilt also auch danach als „Teilnehmer“. Zeile 2 ist 0,
    Zeile 3 = 0xFF (R14RD1-03).
  - Geltungsbereich: Das gilt für den Einstieg Datei 0x43A61 (einmal je Programmlauf). Der zweite Aufrufer Datei 0x45755 (aus
    0x44E22, Argument 0) sieht eine gefüllte Tabelle; dort ist der Sonderfall wegen `[bp−0x64]` = `[0x386]` ≠ 0 aus (Datei
    0x2819C–0x281A1, 0x29CFA), der Filter von U15-a (Datei 0x29CC5–0x29CD5) schlösse aber die Europapokal-Teilnehmer und Platz 0
    aus. Ob dieser Zweig dort erreicht wird, ist nicht gelesen; die Umsetzung „`partnerPlace == 0`“ gilt nur für „neues Spiel“.
  - Folgen: (1) U15-c: Der Managerverein landet immer auf Platz 0. FILE29: ESV KAUFBEUREN auf Platz 0, Feld 0x9744 = 1. Am
    echten Europapokal nimmt er nicht teil (Zeile 1 nach dem Füllen: 148 75 9 110 138 125 95 114 – Platz 9 = DÜSSELDORFER EG
    = Meister DGROUP 0xD0E6 = 9). Die Wirkung von 0x9744 = 1 außerhalb des Highscores ist nicht gelesen. (2) U15-a: Der
    Zufallsplatz r der Start-Liga ist nie Platz 0 (statt „nie der Meister, Verein 2“). Verträglich mit allen Ständen:
    Managervereine auf Platz 2 (FILE01), 1 (FILE10), 3/8 (FILE09), keiner auf Platz 0.
- **Fundstelle:** em.exe Datei 0x29CFA–0x29D23 (Bedingung, setzt DGROUP 0x9744 + 0x1AF · m auf 1),
  0x29D28–0x29D7F (Zufallsplatz der Bundesliga, Ausschluss früherer Manager, 0x45CD0(r, 1)), 0x29D9C `0x36169(v, r, 0x80)`;
  0x45CD0–0x45D42 (Suche in DGROUP 0x5CFB + 0x20 · Zeile); Schreiber der Tabelle 0x341AB (Datei 0x3435D, 0x343E3), Aufruf
  Datei 0x43A8C nach 0x43A61. FILE29/30/31: HOK 0x00F70 = 1; FILE01/05/09/10/27/28: 30. Bild `file29_3` (Spielvorbereitung).
- **Umsetzung:**
  - Ziel: In `NewGame` den Sonderfall nach U15-a anhängen: ein Manager und eine Saison → Tausch des Managervereins auf Platz 0
    der Bundesliga (Teamwerte bleiben am Platz, wie U15-a); `EuropeanCup.InitialChampion` als Ausschluss in
    `PlaceInStartLeague` durch „Platz 0“ ersetzen (`places[partnerPlace]` → `partnerPlace == 0`). Das Feld 0x9744 nur abbilden,
    wenn der Highscore es braucht (dort bisher `[APPROX]`, `Highscore.cs`).
  - Dateien: `src/EishockeyManager.Core/NewGame.cs`, ggf. `src/EishockeyManager.Core/Highscore.cs` (nur Kommentar, Datei der
    Spur prüfen), Doku-Bedarf `docs/transfer.md` (U15-c nicht mehr `[APPROX]`).
  - Testidee: ein Manager, `Duration = EineSaison`, fester Verein → Verein auf Satzplatz 0, Teamwerte von Platz 0; zwei
    Manager → kein Sonderfall. U15-a: Start-Liga Bundesliga, 1000 Saaten → Zufallsplatz nie 0.
  - Spur: `kader-start` (ersetzt L2 „U15-c bleibt `[APPROX]`“, Entscheidung Leitstelle).

### A180 – Startwerte der eigenen Taktik

- **Ergebnis:** widerlegt. Beim Spielstart stehen die eigenen Werte des Managers (DGROUP 0xCAE9 Einsatz, 0xCB01 Angriff, je
  0x30 · m) auf 0, ebenso die Vereinsfelder 0x26–0x3A aller Vereine (Werte der Automatik). Die Automatik schreibt erst vor dem
  ersten Spiel. Das Remake startet Vereine mit der ausgeglichenen Zeile 60/0/25/20/30/0 und 0/4/5/6/7/4.
- **Fundstelle:**
  - FILE29, FILE30, FILE01, FILE12, FILE15, FILE27: Vereinsfelder 0x26–0x2B und 0x35–0x3A aller Vereine 0 (vorhanden sind nur
    0x32 = 5 und 0x34); HOK 0x0BA26 ff. (DGROUP 0xCAE9, 288 Byte) ganz 0.
  - FILE31 (nach dem 1. Spieltag) Kaufbeuren: 0x26–0x2B 60 0 25 20 30 0, 0x2D = 60, 0x35–0x3A 0 4 5 6 7 4 (ausgeglichen,
    Favorit); Gegner-Beispiel 30 20 20 15 20 0 / 0x2D = 40 / 0 5 6 4 4 0. Die eigenen Werte DGROUP 0xCAE9 bleiben 0.
  - FILE33 nach SEMI-Eingaben: Block 1 Einsatz 36 21 22 18 23 4, Angriff 1 4 6 6 2 1, Blöcke 2–4 = 0 (Bild `file33_15`: Reiter 2
    bei SEMI/SEMI lauter Nullen); die Werte bleiben, als die Modi wieder auf AUTOM. stehen. Die Vereinsfelder Köln 0x26–0x2B,
    0x2D und 0x35–0x3A stehen in FILE33 auf 0 (FILE32: 48 20 20 15 20 0 / 40 / 0 5 6 4 4 0); wer sie beim Umschalten löscht, ist
    nicht gelesen. Nach dem nächsten Spieltag (FILE34) schreibt die Automatik wieder (0x2D = 60).
  - Nebenbefund Anzeige „AUTOM.“: `file32_1`/`file33_01` zeigen 40 20 20 15 20 0 und 0 5 6 4 4 0, FILE32 Vereinsfelder Köln
    0x26 = 48, 0x2D = 40. Der Grundwert der Anzeige kommt also aus Feld 0x2D (0x26 ist der Spielwert), die Zuschläge aus
    0x27–0x2B bzw. 0x35–0x3A.
- **Umsetzung:**
  - Ziel: `Tactics()` für Vereine wie `Tactics.National()`: eigene Werte 0, Anzeige „AUTOM.“ 0 bis zum ersten Spiel, danach die
    Werte der letzten Automatik (`RecordAutoLine` auch für Vereine). `[APPROX]` in `Squad.Tactics.cs` (Klassenkommentar, Konstruktor)
    durch `[ORIGINAL]` mit FILE29/FILE31 ersetzen. Alte Spielstände: nur neue Spiele betroffen (keine Migration nötig, sonst
    eigene Migrationsdatei).
  - Dateien: `src/EishockeyManager.Core/Squad.Tactics.cs` (`kader-start`), Anzeige `src/EishockeyManager.UI/Screens/Team/TacticsScreen.cs`
    (`ui-rest`).
  - Testidee: neues Spiel → `Tactics` des Managervereins Einsatz/Angriff aller Blöcke 0; nach einem Spieltag Anzeige 60/0/25/20/30/0
    bei ausgeglichenem Spiel.
  - Spur: `kader-start` (Kern), `ui-rest` (Anzeige).

### A166 – Eingesetzt-Vorgabe beim Start

- **Ergebnis:** belegt (verträglich mit `AutoLineupRule`). FILE29: 16 Aufgestellte (je drei Reihen Abwehr/Sturm) + Torhüter FOUS
  + die fünf stärksten übrigen Feldspieler = 22 eingesetzt; nicht eingesetzt KUNCE (82 83 40), RAU (82 79 41), JORDE (79 79 41),
  die schwächsten nach Summe. Handmodus siehe A167/A189: ohne Einstellung wird nichts aufgefüllt.
- **Fundstelle:** `emtool hok dump FILE29.HOK` (Kader Manager 0, 25 Spieler, Spalte „eingesetzt“).
- **Umsetzung:** keine Codeänderung; FILE29 als weiterer Testfall der bestehenden Regressionstests möglich. Spur: `kader-start`.

### A082 – Strafen-Managerwert ohne nächsten Manager

- **Ergebnis:** belegt. Der Satz des nächsten, nicht belegten Managers behält DGROUP 0x974B + 0x1AF · m = 0 über alle Spieltage;
  gelesen wird also 0.
- **Fundstelle:** HOK 0x00E87 + 0xF0 + 0x1AF · m. Ein-Manager-Stände FILE03/05/32/36 (Manager 0 = 17), FILE31 (20), FILE41 (13):
  Manager 1–5 jeweils 0. Vor dem ersten Spieltag (FILE29/30/01) auch Manager 0 = 0.
- **Umsetzung:** `[APPROX]` in `MatchEngine.Live.cs` („Ohne nächsten Manager 0 (nicht belegter Satz, nicht geprüft)“) zu
  `[ORIGINAL]` mit diesen Ständen. Keine Verhaltensänderung. Spur: `kader-start` (nur Kommentar).

### A046 – Tabellenplatz vor dem ersten Spieltag (Zuschauerformel)

- **Ergebnis:** widerlegt. Die Zuschauerformel 0x3F185 bekommt die Plätze aus dem Tabellenfeld DGROUP 0x8288 + 0x50 · Verein
  (Datei 0x54608–0x5462D), nicht aus einer Vortabelle. Vor dem ersten Spieltag des Spiels ist das Feld für alle Vereine 0
  (= Platz 1). In späteren Saisons steht dort vor dem 1. Spieltag der Wert vom Ende der Vorsaison (nicht neu berechnet, nach
  Auf-/Abstieg am Satzplatz). Das Remake nimmt die Reihenfolge der Liga.
- **Fundstelle:** em.exe Datei 0x54608–0x5464A (Argumente DGROUP 0x8288 + 0x50 · Heim/Gast an 0x3F185).
  HOK 0x03C5F + 0x31 + 0x50 · i: FILE29/30/01/12 alle 0; FILE31 (nach dem 1. Spieltag) Plätze 0–11 gesetzt; FILE14/17/23
  (10.09., vor dem 1. Spieltag der zweiten Saison) Werte der Vorsaison (Doppelte innerhalb einer Liga nach dem Tausch).
- **Umsetzung:**
  - Ziel: `TableRank` liest ein je Verein gespeichertes Tabellenfeld (nach jedem Ligaspieltag geschrieben, beim Spielstart 0,
    beim Saisonwechsel unverändert). Wo das Original 0x8288 schreibt, ist nicht gelesen; Annahme: mit dem Platzverlauf
    (`RecordRankSlots`) – dann reicht dort ein zusätzliches Feld. Neues Feld → Schema und Migration in eigener Datei
    (`Core/SaveGame.Migration.KaderStart*.cs`, Altstände: aktueller Tabellenplatz).
  - Dateien: `src/EishockeyManager.Core/FinanceStadium.cs` (`TableRank`), `src/EishockeyManager.Core/FinanceSponsorRating.cs`
    (Feld neben `_rankSlots`).
  - Testidee: neues Spiel, Heimspiel am 1. Spieltag → Zuschauer mit Plätzen 0/0; zweite Saison 1. Spieltag → Plätze der Vorsaison.
    Mehrsaisonprobe.
  - Spur: `kader-start`.

## Sitzung 2 – Kader und Taktik im Handbetrieb (FILE32–41, FILE37A)

Grundlage: Start FILE05 (KÖLNER EC, Manager „Joerg“), danach im selben Spiel weitergespielt. Bilder `file32_1`, `file33_01…15`,
`file34_1…3`, `file35_1…5`, `file36_1…2`, `file37_1…2`, `file37a_1…4`, `file38_1…2`, `file39_1…3`, `file40_01…13`,
`file41_1…12`, `file14_4…5`. Ab FILE34 steht die Aufstellung im Handmodus (DGROUP 0x375/0x376 = 0/0 in FILE34–41).

### A373 – Pfeilrichtung bei den Modi

- **Ergebnis:** widerlegt (korrigiert nach Review #643, R14RD1-01). Pfeil **unten** = nächster Wert, oben = voriger, für alle
  vier Modusknöpfe; an den Enden bleibt der Wert stehen (kein Umlauf). Maus: `di` = y (Datei 0x484A1–0x484AC); `[bp−0xC]` = 1
  genau dann, wenn y **unter** der Mitte liegt (Datei 0x48783–0x487B8, Bildschirm-y wächst nach unten); die gedrückte Darstellung
  liegt dann 7 bzw. 9 Zeilen tiefer (Datei 0x487BB–0x487D0). Reihenfolge der Reihen-Modi
  Automatisch → Spiel nur mit 2 Reihen → Energiestärkster Block → Nach Gegnerreihe → Minuten-Vorgabe → Voll manuell (wie
  `LineChangeMode`).
- **Fundstelle:** em.exe Datei 0x487A8–0x487B8 (`[bp−0xC]` = 1, wenn y unter der Mitte des Pfeilpaars), 0x48C07–0x48C25
  (Schritt +1 bei `[bp−0xC]` = 1, −1 bei 0), Grenzen 0x48BD2–0x48BFF (0 … Anzahl − 1, kein Umlauf). Die Bilder `file33_02…08`
  und die Angabe des Nutzers entscheiden die Richtung nicht.
- **Umsetzung:** `TacticsScreen.Step` umdrehen (+1 für die untere Hälfte), `Cycle` durch Begrenzen auf 0 … Anzahl − 1 ersetzen,
  `[ORIGINAL] Datei 0x487A8–0x487B8, 0x48BD2–0x48C25`. Testidee: untere Hälfte der Reihen-Pfeile auf „Automatisch“ → „Spiel nur
  mit 2 Reihen“; obere Hälfte auf „Automatisch“ → unverändert. Zahlenpfeile (Datei 0x489AB ff., gleiche Variable) mitprüfen.
  Spur: `ui-rest`.

### Taktik-Befund DGROUP 375h – Knopf oben links und Spieltag

- **Ergebnis:** widerlegt.
  - Der Knopf oben links im Taktikbildschirm (Symbol zwei Spieler, AUTOM./MANUELL) ist die Einstellung „automatisch aufstellen“
    DGROUP 0x375 + 2 · m, kein Torwartwechsel. Das Remake führt dort `Tactics.Goalie` (ohne Wirkung) und die Einstellung getrennt
    (`Club.AutoLineupEnabled`, umschaltbar mit „A“ in der Aufstellung).
  - Je Manager gibt es zwei Bytes: 0x375 + 2 · m (aktuell) und 0x376 + 2 · m (gesichert). Im Tageslauf vor den Spielen:
    gesichert := aktuell, aktuell := 0. Nach den Spielen: aktuell := gesichert, dann Neuaufstellung 0x426BC. Während der Spiele
    des Tages ist die Einstellung also aus; ob deshalb die Neuaufstellung nach einer Verletzung im Spiel (0x53C57 → 0x426BC)
    entfällt, ist nicht nachgelesen.
  - Der Taktikknopf schreibt nur das aktuelle Byte: FILE32/33 0x375 = 0, 0x376 = 1 (umgestellt, noch kein Spieltag); FILE34
    nach dem Spieltag 0/0 (Angabe Nutzer: „Nach dem Spieltag steht sie weiterhin auf Manuell“).
- **Fundstelle:**
  - em.exe Datei 0x47CA0–0x47CE5: Zeiger je Knopf 0 → DGROUP 0x375 + 2 · m, 1 → 0xA701 + m (Reihen-Modus), 2 → 0xA6FB + m
    (Einsatz), 3 → 0xA707 + m (Angriff). Lage der Knöpfe DGROUP 0x52D2/0x52E2 (Knopf 4: (71 | 42) = oben links, 5: (13 | 116),
    6/7: (152 | 80)/(248 | 80)), Werteanzahl DGROUP 0x5306: 2, 6, 3, 3.
  - Tageslauf: Datei 0x54FCE–0x54FE8 (sichern, auf 0 setzen), 0x552F9–0x5530B und 0x54D8C–0x54D9E (zurückholen, 0x426BC).
  - Bilder `file32_1` (oben links MANUELL), `file33_01` (AUTOM.); HOK 0x00844 (Block DGROUP 0x375, 12 Byte): FILE29–31/05 alle 1,
    FILE32/33 `00 01`, FILE34–41 `00 00`.
- **Umsetzung:**
  - Ziel Anzeige: Knopf oben links zeigt und schaltet `AutoLineupEnabled` des Managervereins (AUTOM./MANUELL); `Tactics.Goalie`
    entfällt (Migration nur zum Entfernen des Felds, falls gespeichert). Schalten von MANUELL auf AUTOM. stellt nicht sofort auf.
  - Ziel Kern: Tageslauf mit den zwei Bytes (gesichert/aktuell) oder gleichwertig: während der Spiele des Tages keine
    Neuaufstellung durch die Einstellung; nach den Spielen Neuaufstellung, wenn eingeschaltet. Vorher 0x53C57 lesen.
  - Dateien: `src/EishockeyManager.UI/Screens/Team/TacticsScreen.cs` (`ui-rest`); `src/EishockeyManager.Core/Squad.AutoLineup.cs`,
    `src/EishockeyManager.Core/Squad.Tactics.cs`, `src/EishockeyManager.Core/MatchEngine.Injuries.cs` (`kader-start`;
    Anschluss im Tageslauf `GameState.Integration.cs` minimal, im PR begründet).
  - Testidee: Taktik oben links auf MANUELL → `AutoLineupEnabled == false`; Spieltag mit Verletzung bei eingeschalteter
    Einstellung → Neuaufstellung erst nach den Spielen.
  - Spur: `ui-rest` (Anzeige), `kader-start` (Kern).

### A364 – Bedienung der Aufstellung, Spalten NR und STATUS

- **Ergebnis:** teils.
  - Rechtsklick auf einen Spielernamen öffnet die Spielerdaten („Aha !“), für nicht eingesetzte (SANDNER, `file34_1`) wie für
    aufgestellte Spieler (LALA, `file34_2`): belegt (Remake schon `[ORIGINAL]` 0x400E9).
  - STATUS bei nicht Eingesetzten leer: belegt (`file34_3`).
  - NR bei nicht Eingesetzten leer: widerlegt. `file34_3` zeigt keine Nummer bei J.SCHULZ (Platz 3), DOBRYZINSKI (20) und
    SANDNER (23), alle laut FILE34 nicht eingesetzt; `file36_1`/`file39_3` ebenso (MENDE, HOCK im Spiel, HEISS, KJELLBERG).
    Das Remake zeigt die Nummer immer.
  - Linksklick auf nicht eingesetzte oder aufgestellte Spieler: nicht aufgenommen, bleibt `[APPROX]`.
- **Fundstelle:** Bilder `file34_1…3`, `file36_1`, `file39_3`; `emtool hok dump FILE34.HOK` (Spalte „eingesetzt“).
- **Umsetzung:** `LineupScreen.Draw`: NR nur für eingesetzte Spieler; `[APPROX]` „NR zeigt weiter die Rückennummer“ und „STATUS
  bleibt leer“ in `[ORIGINAL]` (Bilder) bzw. berichtigen. Testidee: Pixeltest gegen `file34_3`, Zeile J.SCHULZ ohne Ziffer. Datei
  `src/EishockeyManager.UI/Screens/Team/LineupScreen.cs` (Dateigrenze `kader-start`, Darstellung laut Plan `ui-rest`; siehe
  offene Fragen). Prüfen, ob die Nationalaufstellung (gleicher Code 0x40364) dieselbe Regel braucht.

### A012 – VERL./GESP. auch bei nicht eingesetzten Spielern

- **Ergebnis:** belegt. `file36_1`/`file36_2`: MENDE (FILE36 nicht eingesetzt, Reihe 1A) mit „VERL.“; `file36_1` HOCK im Spiel
  ohne NR mit „GESP.(2)“; `file37_2`/`file39_3` HEISS (FILE37 nicht eingesetzt) mit „VERL.“. Vereins- und Nationalaufstellung
  teilen den Code (Datei 0x40364).
- **Umsetzung:** `[APPROX]` in `CupsNational.Lineup.cs` → `[ORIGINAL]` mit diesen Bildern. Spur: `nationalteam` (Datei
  `CupsNational.Lineup.cs` gehört `nationalteam`, L7).

### A167 – Neue Kaderspieler nicht eingesetzt (auch Handmodus)

- **Ergebnis:** belegt, jetzt auch bei ausgeschalteter Einstellung (bisher offen, L7). KJELLBERG (Kauf, FILE35) und VALLIERE
  (Leihe, FILE41) stehen ohne „eingesetzt“ und ohne Reihe im Kader; Einstellung 0x375 = 0.
- **Fundstelle:** FILE35 Platz 26 KJELLBERG, FILE41 Platz 25 VALLIERE; Bilder `file35_5`, `file40_10`.
- **Umsetzung:** `[APPROX]` (Kauf bei ausgeschalteter Einstellung) in `Squad.Lineup.cs` → `[ORIGINAL]`. Spur: `kader-start`.

### A091 – Kaderplätze bei Kauf, Leihe, Verkauf und Transferliste

- **Ergebnis:** belegt. Kauf KJELLBERG (STU) → Nr. 26 hinten (Gruppe STU ist die letzte); Verkauf SCHIFFL (Nr. 7, FILE37A) →
  MENDE 8 → 7, alle folgenden rücken auf, KJELLBERG 26 → 25; HOHENADL (Nr. 8 seit FILE37A, vorher 9) auf die Transferliste → Plätze rücken auf
  (FILE41: KJELLBERG 24), Leihe VALLIERE (STU) → Nr. 25.
- **Fundstelle:** `emtool hok dump` FILE35/37/37A/40/41, Bilder `file35_5`, `file37a_4`, `file40_10`.
- **Umsetzung:** keine; Einsortieren nach Art bei TOR/ABW nicht aufgenommen. Spur: `kader-start`.

### A135 – Tore und Spiele je Wettbewerb

- **Ergebnis:** widerlegt. Das Original zählt je Wettbewerb (Liga/DEB-Pokal/Europapokal): Tore +0x03/+0x04/+0x05, Spiele
  +0x06/+0x07/+0x08, beim jetzigen Verein Spiele +0x1C/+0x1E/+0x20 und Tore +0x22/+0x24/+0x26 (Wörter). Die Spalten SP und TO der
  Kaderliste zeigen die Summe. Das Remake zählt alles in +0x03 bzw. +0x1C.
- **Fundstelle:**
  - FILE38 → FILE39 (Pokal ESV KAUFBEUREN – KÖLNER EC 8:1, `file39_1`): +0x07/+0x1E 5 → 6 bei allen Spielern des Spiels, +0x06
    unverändert 26; SIKORA +0x04/+0x24 2 → 3 (das 1 Tor), +0x03 unverändert.
  - FILE39 → FILE40 (Liga): +0x06/+0x1C 26 → 27; KWASIGROCH, STEFAN, LALA je +1 in +0x03/+0x22.
  - `file39_3`: SP 32 = 26 Liga + 6 Pokal; `emtool hok dump` zeigt unter SP nur +0x06, unter TO die Summe.
- **Umsetzung:**
  - Ziel: getrennte Zähler je Wettbewerb am `Player` (Tore, Spiele; beim Verein Tore, Spiele), Spielwertung bucht nach Wettbewerb;
    `PlayerValue.Inputs` liest Liga-Tore (+0x03) und Spiele beim Verein (+0x1C) wie das Original 0x49D36; Anzeige SP/TO = Summe;
    Spielerdaten (Tore/Spiele je Wettbewerb) aus den Zählern. Schema und Migration (Altstände: alles als Liga).
  - Dateien: `src/EishockeyManager.Core/Player*.cs`, Buchung der Spiele (Pokale: `Cups*.cs` nur Aufruf, im PR nennen),
    `src/EishockeyManager.UI/Screens/Team/PlayerDataWindow`-Anschluss.
  - Testidee: Pokalspiel mit Tor → +0x04-Zähler +1, Liga-Tore unverändert, Spielerwert unverändert. Mehrsaisonprobe (Rücksetzen
    zum Saisonwechsel wie A088: +0x03–0x08 auf 0, Vereinszähler bleiben).
  - Spur: `kader-start`.

### A189 – Verletzte und gesperrte Aufgestellte

- **Ergebnis:** Verletzte belegt, jetzt auch im Handmodus und außerhalb eines Spiels; Gesperrte nicht entscheidbar.
  - MENDE (Verletzung im Spiel, FILE36): +0x0A gelöscht, Reihe 1A bleibt; im Block nicht mehr angezeigt (`file36_1`: Block 1
    „7 _“), „Eingesetzt: 21“ nach dem Spiel.
  - HEISS (Verletzung außerhalb des Spiels, Meldung 27.11., FILE37): +0x0A gelöscht, „Eingesetzt: 20“.
  - Handmodus füllt nicht auf: eingesetzt 22 (FILE34/35) → 21 → 20 → 19 (Verkauf) → 18 (Transferliste, FILE41).
  - „GESP.(2)“ bei HOCK gilt nur im Spiel (`file36_1`), nach dem Spiel „IM TEAM“, eingesetzt, Reihe 2S (FILE36); eine Sperre über
    Spiele kommt in keinem Stand vor (Angabe Nutzer zu FILE37).
- **Fundstelle:** `emtool hok dump` FILE34–41, Bilder `file36_1…2`, `file37_1…2`.
- **Umsetzung:** Kommentar in `StadiumStatistics.cs` ergänzen (Handmodus, außerhalb des Spiels belegt); Gesperrte bleiben
  `[APPROX]`. Spur: `kader-start`.

### A380 – Eigener Spieler auf dem Transfermarkt

- **Ergebnis:** widerlegt. Ein auf die Transferliste gesetzter Spieler verlässt den Kader: Sein Kaderplatz wandert in den
  Marktsatz (Manager 4), die Kaderplätze rücken auf; der Pool-Satz behält Besitzer = Manager und Verein. In der Transferliste
  steht er mit blauem Art-Buchstaben, die Statuszeile zeigt „von JOERG“. Ein „*“ gibt es nicht. Das Remake lässt ihn im Kader
  mit „NAME *“.
- **Fundstelle:** FILE40 → FILE41: HOHENADL aus dem Kader (25 → 24 + VALLIERE), im Markt Platz 7; Pool-Satz 28 `+0x1A` = 0
  (Manager), `+0x1D` = 2 (Köln) in beiden Ständen; VALLIERE (Leihe) Pool `+0x1A` = 4, `+0x1D` 57 → 2. Bilder `file40_04`
  (HOHENADL auf der Liste, Kaderliste links ohne ihn), `file40_05` („von JOERG“), `file40_06` („von EV LANDSHUT“), `file40_07`
  („von FRANKREICH“).
- **Umsetzung:**
  - Ziel Kern: Auf die Liste setzen = Spieler aus dem Kader nehmen (Platz frei, eingesetzt/Reihe weg, Nummern rücken auf), Angebot
    am Markt mit Verkäufer = Manager; Gehalt und Rückkehr beim Zurückziehen nachlesen (Code der Transferliste, Datei 0x46704 ff.).
  - Ziel Anzeige: Art-Buchstabe des eigenen Angebots blau (Index aus `file40_04` messen), Statuszeile „von <Manager>“ bzw.
    „von <Verein>“; „*“ in der Kaderliste entfällt.
  - Dateien: `src/EishockeyManager.Core/Transfer*.cs` (`kader-start`), `src/EishockeyManager.UI/Screens/Team/TransferScreen.cs`
    (`ui-rest`).
  - Testidee: Spieler auf die Liste → nicht mehr in `Club.Players`, Markt enthält ihn mit Verkäufer; Kaderplätze rücken auf.
    Mehrsaisonprobe (Kader).
  - Spur: `kader-start` (Kern), `ui-rest` (Anzeige).

### A382 – Farbe der Markierung in der Transferliste

- **Ergebnis:** widerlegt. Die Trennlinien über und unter dem Eintrag unter der Maus werden Index 16 (97 | 0 | 16), nicht 201
  (65 | 0 | 0). Index 96 hat dieselbe Farbe; 16 ist der übliche Index. Die normale Trennlinie ist **eine** Zeile Index 31
  (243 | 195 | 113), darunter Hintergrund 4 – nicht 93 mit 198 darunter.
- **Fundstelle:** `file40_05` (Maus auf HOHENADL, Eintrag 6): Zeilen 132 und 148, x 180–314 Index 16; `file40_06` (VOELKLE,
  Eintrag 8 ab 0): Zeilen 164 und 180, x 180–314 Index 16 (Zeile 148 ist die normale Linie, Index 31). Übrige Trennlinien `file40_04`: Zeilen 52 + 16 · k Index 31, Zeile darunter Index 4.
- **Umsetzung:** `HoverEntryColor = 16`, `EntryLine` einzeilig Index 31; `[APPROX]` → `[ORIGINAL]` (DOSBox-Bild, exakt). Pixeltest
  gegen `file40_05`. Datei `src/EishockeyManager.UI/Screens/Team/TransferScreen.cs`. Spur: `ui-rest`.

### A377 – Kauf- und Leihfenster

- **Ergebnis:** widerlegt. Das Original zeigt nur Titel „<NAME> kaufen“ bzw. „<NAME> leihen“, Linie und „Ihr Angebot:“ mit
  Eingabemarke in der großen Schrift; keine Zeilen „…, n Jahre alt“, „ABLÖSESUMME“/„LEIHGEBÜHR“. Rahmen (2 | 29)–(166 | 71),
  Schatten 2 Pixel Index 6 bis (168 | 73), also über der Kaderliste links, nicht mittig.
- **Fundstelle:** `file40_08` (VALLIERE kaufen), `file40_09` (VALLIERE leihen), `file35_2` (KJELLBERG kaufen); Differenz zu
  `file40_07`: x 2–168, y 29–73.
- **Umsetzung:** `TransferDialog` (Buy/Loan) nach dem Bild; Lage gegen Code Datei 0x491F9 prüfen. Pixeltest gegen `file40_08`.
  Datei `src/EishockeyManager.UI/Screens/Team/TransferDialog.cs`. Spur: `ui-rest`.

### A374 – Schatten der Dialoge

- **Ergebnis:** belegt. Der Schatten der Vertragsverhandlung ist einfarbig Index 6 (460 von 464 Pixeln; die übrigen 4 sind die
  versetzten Ecken), auch über Listentext und Linien (darunter Indizes 4, 9, 24, 30, 5). Kein Abdunkeln.
- **Fundstelle:** `file40_12` gegen `file40_13`: Rahmen bis (166 | 95), Schatten bis (168 | 97). Angabe Nutzer zum „bunten
  Hintergrund“: „Wahrscheinlich.“
- **Umsetzung:** `[APPROX]` in `TeamUi.cs` (`DialogShadow`) → `[ORIGINAL]`. Spur: `ui-rest`.

### A313 – Streifen der Finanzen

- **Ergebnis:**
  - Kursregler (Ziehen im Kursfeld): nicht aufgenommen, nicht entscheidbar.
  - Betragsbalken (Bank, Mitmanager, Aktien): widerlegt. Der Balken entsteht erst mit dem Klick (nicht beim Bewegen) und
    verschwindet, sobald die Maus die Zeilen des Streifens samt Beschriftung verlässt – ohne Klick. Das Remake löscht ihn erst
    bei einem Klick außerhalb.
- **Fundstelle:** em.exe Datei 0x59531–0x5955F: Maus-y außerhalb 0x49–0x64 (73–100) oder keine Seite → 0x59ADD (Hintergrund
  zurück, `[bp−0x1C]` = 0); erst danach die Tastenprüfung Datei 0x59B2D (`DGROUP 0x5A16 == 1`). Angabe Nutzer zu `file41_2…8`:
  „Wenn man einen Betrag eingestellt hat und mit der Maus den Bereich des Balkens und der darüberliegenden Beschriftung verlässt,
  verschwindet der Balken wieder.“ Bild `file41_5` (Balken links 883.000 DM, Hinweis „Geld von der Bank abheben/leihen (4%).“).
- **Umsetzung:** `FinanceScreen.Deactivate` bei Maus außerhalb y 73–100 in jedem Bild aufrufen (nicht nur beim Klick);
  Kommentar „Klick außerhalb des Streifens (Datei 0x59ADD–0x59B28)“ berichtigen. Ob der Betrag stehen bleibt, bleibt wie
  beschrieben (`[APPROX]` 0x5911D). Datei `src/EishockeyManager.UI/Screens/Manager/FinanceScreen.cs`. Spur: `ui-rest`.

### A321 – Nachrichtenleiste

- **Ergebnis:** teils. Die Leiste erscheint nach Klick auf das Managerbild (Angabe Nutzer). Ohne Nachricht steht „Keine Nachricht
  für Sie vorhanden.“ im oberen Feld (wie Remake). Mit Nachricht steht oben das **Datum** („6. Dez. 1993“), im weißen Feld der Text
  in zwei zentrierten Zeilen („EC HEDOS MÜNCHEN ist an SCHIFFL“ / „interessiert.“); ein Zähler „1/1“ ist nicht zu sehen.
- **Fundstelle:** `file37a_1`: Text (Index 0) oben Zeilen 208–212 (x 135–181), weiß Zeilen 220–224 (x 95–221) und 227–231
  (x 133–183), Mitte x ≈ 158; `file41_1` (keine Nachricht). Aufteilung längerer Texte (mehr als zwei Zeilen, Umbruch) nicht
  aufgenommen.
- **Umsetzung:** `NewsBar.Draw`: oberes Feld = Datum der Nachricht, Textzeilen ab dem weißen Feld (7 Zeilen Abstand); Zähler nur,
  falls belegt (sonst weglassen oder `[APPROX]`). Pixeltest gegen `file37a_1`. Datei
  `src/EishockeyManager.UI/Screens/Manager/NewsBar.cs`. Spur: `ui-rest`.

### A357 – Balken der Vereinshistorie

- **Ergebnis:** widerlegt. Die Balken haben je Grafik eine eigene Farbe: Liga Index 29 (32 | 48 | 81), DEB-Pokal Index 27
  (97 | 65 | 48); Europapokal nicht belegt (nicht im Wettbewerb). Remake: 200 (32 | 48 | 65). Balken 2 Pixel breit; Höhe für
  „12. Platz der Oberliga“ 2 Pixel, „DEB-Pokal: Erste Runde“ 3 Pixel (Remake 4 bzw. 4). Die Marke unter der Achse ist 2/4/6 Pixel
  breit, darunter ein Stiel 2 Pixel × 3 Zeilen (Remake 1/3/5 + 1 Pixel).
- **Fundstelle:** `file14_5`: Liga-Balken x 117–118, y 171–172, Achse y 173 (Index 6), Marke y 174–179; DEB-Balken x 77–78,
  y 73–75, Marke y 77–82; `file41_12` (keine Einträge). Formel der Höhe nicht gelesen.
- **Umsetzung:** `BarColor` je Grafik (Liga 29, DEB 27, Europa offen), Marke nach dem Bild; Höhenformel aus em.exe lesen (sonst
  `[APPROX]` mit diesen zwei Messpunkten). Pixeltest gegen `file14_5`. Datei
  `src/EishockeyManager.UI/Screens/Statistics/HistoryScreen.cs`. Spur: `ui-rest`.

### Weitere IDs aus dem Plan

- **A115** (Versicherung bei Rückkehr aus Leihe): in Teil 1 nicht entscheidbar; das Leihende liegt in FILE46 (ersetzt FILE42),
  Auswertung in Teil 2.
- **A088** (Zähler zum Saisonwechsel, L14): kein Saisonwechsel in Sitzung 1–2. Nebenbei belegt A135 die Zählerfelder je
  Wettbewerb, die A088 betrifft.
- **A009, A085** (Bundestrainer): kein Bundestrainer in diesen Sitzungen → Teil 2.

## Umsetzung

Je Punkt stehen Ziel, Dateien und Testidee oben. Zusammenfassung nach Spuren (Plan Runde 14):

- **`kader-start` Teil 2:** A056, U15-c (mit Korrektur des Europapokal-Filters in U15-a), A180 (Kern), A046 (Schema), A135
  (Schema), A380 (Kern), Taktik 375h (Kern: zwei Bytes im Tageslauf); nur Kommentare: A082, A166, A167, A189, A091.
- **`ui-rest` Teil 2:** A373, Taktik 375h (Knopf oben links), A364 (NR), A380 (Anzeige), A382, A377, A374, A313, A321, A357,
  A180 (Anzeige „AUTOM.“).
- **`nationalteam`:** A012 nur Kommentar in `CupsNational.Lineup.cs`; Hinweis: Anzeige „AUTOM.“ Grundwert aus Feld 0x2D.
- **`hok-import`** (L7, Datei allein `hok-import`): Feldbelege für `docs/formate/spielstand-original.md` (0x9744, 0x8288, 0x375/0x376, +0x03–0x08,
  +0x22–0x26, Markt-Kaderplatz bei Transferliste).

## Offene Fragen an die Leitstelle

1. **U15-c:** L2 („bleibt `[APPROX]`“) aufheben? Der Beleg FILE29 liegt jetzt vor; Umsetzung samt Filter „Platz 0“ in U15-a.
2. **Schema:** A046, A135 und A380 brauchen neue Felder bzw. Umbau (Zähler je Wettbewerb, Tabellenfeld, Markt-Kaderplatz). In
   `kader-start` Teil 2 oder als eigener Teil?
3. **Dateigrenze `LineupScreen.cs`** (A364, NR leer): Datei gehört `kader-start`, Darstellung laut Plan `ui-rest`.
4. **Taktik 375h:** Die Kernänderung (zwei Bytes, Neuaufstellung erst nach den Spielen) berührt `MatchEngine.Injuries.cs` und den
   Tageslauf; vorher 0x53C57 nachlesen.
5. **Fehlende Aufnahmen** (Kandidaten): Leihende (A115, FILE42), Spieler mit Sperre über Spiele (A189), Nachricht mit mehr als zwei
   Zeilen bzw. mehreren Nachrichten (A321), Europapokal-Balken (A357), Linksklick in der Aufstellung (A364).

6. **Dateigrenzen der Umsetzungsvorschläge (Review #643, R14RD1-02)** – entschieden als L7 (Leitstelle 08.11.2026):
   `kader-start` darf für A046 `Core/FinanceStadium.cs` (`TableRank`) und `Core/FinanceSponsorRating.cs` (nur das Tabellenfeld),
   für A082 `Core/MatchEngine.Live.cs` (nur Kommentar), für A189 `Core/StadiumStatistics.cs` (nur Kommentar), für A135 die
   Buchung von Spielen und Toren je Wettbewerb (`Core/Cups*.cs` außer `CupsNational*`, `Core/MatchEngine*.cs`, nur diese Stelle)
   und für U15-c `Core/Highscore.cs` ändern; `ui-rest` für A357 `UI/Screens/Statistics/HistoryScreen.cs`; A012 → `nationalteam`;
   Feldbelege in `docs/formate/spielstand-original.md` → `hok-import`.

## Teil 2 – Sitzungen 3–7 (FILE43–49A)

Geplanter Lauf Claude Opus 5.5, PR #642 (gestapelt auf #628). Material laut Issue #639: Sitzung 3 Stadion (FILE43, FILE43A,
FILE44), Sitzung 4 Saisonende (FILE46, Start FILE44; FILE45 entfällt), Sitzung 5 Bundestrainer (FILE47–49, FILE49A), Sitzung 6
Speichern-Dialog (`a338_1…3`, ohne Spielstand), Sitzung 7 Stadion Stufe 2/3 (FILE44A, FILE44B). Referenz-Repo Stand `779a1c8`.
Verfahren wie Teil 1. Das Stadionbild nutzt die Palette `kb\14` (6 Bit), die übrigen Bilder `kb\21`.

### Übersicht Teil 2

| ID | Ergebnis | Kern | Spur |
|---|---|---|---|
| A323/A325 | widerlegt (Lage) / belegt (Stufenfolge) | Teile aus Tabelle DGROUP 0x4F3D/0x4F5B/0x4F79, Folge DGROUP 0x4F97 (Kino zuletzt), Maske 0xC0 (Datei 0x3D904–0x3D9EB); alle fünf Stadionbilder damit pixelgleich | `ui-rest` |
| A325 (Rest) | nicht entscheidbar | Kein Bild von Kino MITTEL mit Restaurant, Autobahn, Halboffen, Bowlingbahn (Lage folgt aus derselben Tabelle); Stufe unter Kneipen = IMBIßBUDE (belegt) | `ui-rest` |
| A326 | teils | Bau-Tabelle Farbe 1 und Ist-Ansicht im Bau belegt; rechts kein „(Noch n Wochen)“; Ausbau-Frage ist das Fragefenster Datei 0x4C246 („Ja.“/„Nein.“) | `ui-rest` |
| A362 | teils | Liste = Verträge-Liste (belegt), aber einmal vor den Verhandlungen gezeichnet (Datei 0x27C9F): WERT altes Gehalt, „Kein V.“; Fenster x = 2 | `kader-start` |
| A091/A380 | belegt (neu) | Nicht verkaufter Transferlisten-Spieler kehrt am Saisonende zurück, nach Art einsortiert, nicht eingesetzt (Datei 0x27547–0x27665) | `kader-start` |
| A085 (Verein) | belegt | Platzfolge über den Saisonwechsel unverändert, Abgänge fallen weg, Rückkehrer einsortiert | – |
| A115 | teils / nicht entscheidbar (Versicherung) | Marktleihe endet frei (Pool 6); Rückkehr zum Manager kopiert den Leih-Kaderplatz (nur nach Code) | `kader-start` |
| Saisonende | belegt | 100.000 DM = eine Einzelprämie; Gehalt 19.152 = Wert · 114/100, Ablöse 155.500 = Wert(Preis)/2 | `kader-start` (Test) |
| A088 | teils belegt | Kaderplatz: Saisonzähler `+0x03`–`+0x08` → 0, Gesamtzähler `+0x1C`–`+0x26` bleiben; Pool/Markt nicht verglichen | `kader-start` |
| A321 | teils (neu) | Datum rot (Index 17), wenn eine weitere Nachricht folgt (Datei 0x4B71E, 0x4B9CD) | `ui-rest` |
| A189 | nicht entscheidbar | Kein Gesperrter in FILE43–49A (kein `+0x09` Bit 0x01) | – |
| A009 | widerlegt | Bundestrainer hat „automatisch aufstellen“ (DGROUP 0x37F/0x380, Vorgabe an, Neuaufstellung beim Menüaufbau); Herausnehmen schaltet ab; leere Aufstellung spielt leer (0:17), Länderwerte 0 | `nationalteam` |
| A085 (WM-Kader) | belegt | Voller Kader: Klick wirkungslos; Herausnehmen rückt auf, Aufnehmen nach Art (Datei 0x3CA71, 0x3CABF), Herkunft `+0x0C` | `nationalteam` |
| A293 | belegt / widerlegt (Startseite) | Kein „Endrunde“ in der Tabelle; „Paarungen → WM“ springt ab WM-Stufe 5 in die Endrunde (Datei 0x44B1C) | `nationalteam` |
| A338 | Bild belegt / Bedienung widerlegt | Kein „SEITE:“ (DGROUP 0xBC0 nie gelesen); Blättern nur über `ArrowPair` (150 \| 169)/(174 \| 169), Schritt 48, an den Enden wirkungslos (Datei 0x5E386–0x5E42B); keine Tasten, kein Mausrad; roter Marker unter der Maus | `hok-import` |
| A339 | widerlegt | Infofeld Datei 0x5B661 aus dem unverschlüsselten Kopf (Kennung, Jahr, Managerzahl, Spieltag, 4 × 0x28); Fenster (71 \| 71) 8 × 3; nur solange die rechte Taste gehalten wird | `hok-import` |

## Sitzung 3 und 7 – Stadion ausbauen (FILE43–44, FILE44A, FILE44B)

Grundlage: FILE41 (vorher), FILE43 (Sitzkomfort im Bau, 20.12.1993), FILE43A (Konto 20 Mio., sonst FILE43), FILE44 (Kino KLEIN
und KNEIPEN fertig, 14.04.1994), FILE44A (Kino MITTEL fertig, RESTAURANT im Bau, 10.09.1994), FILE44B (RESTAURANT und Kino GROß
fertig, 17.09.1994); Bilder `file43_1…7`, `file44_1…8`, `file44a_1…10`, `file44b_1…7`. Stadionsatz des Managers 0 =
DGROUP 0x9771 (HOK 0x00F9D): je Bereich i Stufe (+2·i) und Zielstufe (+2·i+1), Restwochen +0x12 + p. Die Bilder sind
pixelgleich mit kb\14 (Palette kb\14, 6 Bit); Teile aus kb\8.

### A323/A325 – Teile des Stadionbilds (kb\8)

- **Ergebnis:** widerlegt (Lage, Quellrechtecke, Reihenfolge, Transparenz). Das Original zeichnet die Teile aus einer Tabelle;
  mit ihr ist das Stadionbild in `file43_2`, `file43_7`, `file44_7`, `file44a_10` und `file44b_6` im Bildbereich (0–167)
  **pixelgleich** (0 abweichende Pixel). Das Remake weicht ab: Kinos 15 Zeilen zu tief, Imbiß 6 Zeilen, Quellrechtecke mit
  Trennlinien von kb\8 (sichtbare Fremdzeilen/-spalten), falsche Reihenfolge, Transparenz 0 statt 0xC0.
- **Tabelle** (DGROUP, je Eintrag 2 Byte, Index 6 · k + 2 · (Stufe − 1)): Bereichsfolge DGROUP 0x4F97 = 5, 6, 8, 0, 7
  (Verkehrsanbindung, Gastronomie, Bowlingbahn, Überdachung, Kino – Kino zuletzt), Ziel (x | y) DGROUP 0x4F3D, Größe (w × h)
  DGROUP 0x4F5B, Quelle (x | y) in kb\8 DGROUP 0x4F79; Stufe 0 zeichnet nichts.

  | Teil | Ziel | Quelle | Größe | Remake (Quelle → Ziel) | Bildbeleg |
  |---|---|---|---|---|---|
  | Straße | (244 \| 50) | (0 \| 0) | 66 × 118 | (0 \| 0) 68 × 118 → (244 \| 50) | alle |
  | Autobahn | (244 \| 50) | (66 \| 0) | 76 × 118 | (68 \| 0) 75 × 118 → (246 \| 50) | – |
  | Imbißbude | (25 \| 112) | (211 \| 109) | 50 × 30 | (211 \| 110) 50 × 28 → (23 \| 118) | `file43_2`, `file43_7` |
  | Kneipen | (21 \| 105) | (211 \| 139) | 56 × 39 | (211 \| 138) 56 × 40 → (20 \| 108) | `file44_7`, `file44a_10` |
  | Restaurant | (13 \| 91) | (143 \| 115) | 68 × 58 | (143 \| 115) 67 × 56 → (14 \| 92) | `file44b_6` |
  | Bowlingbahn | (201 \| 94) | (142 \| 60) | 68 × 55 | (143 \| 59) 64 × 57 → (202 \| 93) | – |
  | Halboffen | (79 \| 57) | (142 \| 0) | 144 × 60 | (143 \| 0) 143 × 59 → (81 \| 54) | – |
  | Halle | (80 \| 57) | (0 \| 118) | 143 × 57 | (0 \| 117) 143 × 58 → (80 \| 56) | alle |
  | Kino klein | (29 \| 44) | (144 \| 173) | 52 × 21 | (143 \| 171) 55 × 23 → (27 \| 57) | `file44_7` |
  | Kino mittel | (27 \| 42) | (210 \| 86) | 53 × 23 | (209 \| 87) 54 × 23 → (27 \| 57) | `file44a_10` |
  | Kino groß | (24 \| 39) | (210 \| 60) | 58 × 26 | (209 \| 59) 61 × 28 → (24 \| 54) | `file44b_6` |

- **Reihenfolge:** Mit der Remake-Folge (Verkehr, Überdachung, Gastronomie, Kino, Bowling) weicht `file44b_6` in 10 Pixeln
  (Spalte 80, Zeilen 91–112: Restaurant über der Halle) ab; mit der Originalfolge 0.
- **Transparenz:** Maske 0xC0 (Index 192, Datei 0x3D92D `mov al, 0xC0` als letztes Argument der Kopie 0x50:0xA79 = Datei
  0x139C9). Mit Transparenz 0 weicht `file44a_10` in 16 Pixeln ab (Kino mittel, x 74–79, y 42–44: Index 0 ist dort deckend).
- **Stufenfolge (A325):** belegt. Gastronomie OHNE/IMBIßBUDE/KNEIPEN/RESTAURANT, Kino OHNE/KLEIN/MITTEL/GROß wie
  `FinanceStadium.cs`; Stufen-Bytes FILE44 `2 1 1 1 2 1 2 1 0`, FILE44A Gastronomie 2 → Ziel 3, Kino 2, FILE44B `… 3 3 0`. Die
  Stufe unter den Kneipen ist die IMBIßBUDE (Startstufe Bundesliga, im Bild `file43_2` mit Schriftzug, Lage oben belegt).
  Kino KLEIN → GROß in einem Auftrag (`file44b_2`: 1.040.000 DM = 2 · 520.000) und KNEIPEN → RESTAURANT 580.000 DM stimmen mit
  `Stadium.Cost` (linear je Stufe) und der Stufenwahl bis zur Höchststufe.
- **Nicht entscheidbar (Bild):** Kino MITTEL zusammen mit Restaurant, Autobahn, Halboffen, Bowlingbahn – kein Bild. Die Lage
  folgt aber aus derselben Tabelle; Überdeckung nur über die Reihenfolge.
- **Fundstelle:** em.exe Datei 0x3D904–0x3D9EB (Schleife über 5 Bereiche, Tabellen DGROUP 0x4F97, 0x4F3D, 0x4F5B, 0x4F79),
  Datei 0x3D92D (Maske 0xC0); Template-Abgleich der Bilder (Spielkoordinaten, Bild 642 × 507 doppelt ab (1 | 26)).
- **Umsetzung:** `DrawBuildings` auf die Tabelle umstellen (Ziel, Quelle, Größe je Bereich und Stufe, Folge 5, 6, 8, 0, 7,
  Transparenz 0xC0; `Part` mit Maskenindex statt 0), `[APPROX]` an Zeile 103/329 durch `[ORIGINAL]` mit Fundstelle ersetzen.
  Test: Stadionbild je Stufe rendern, Teilrechtecke gegen die Tabelle prüfen; Pixeltest `file44b_6`-Stand (Restaurant über
  Halle, Spalte 80) synthetisch über Reihenfolge. Hinweis: Die Hotspots (`Hotspots`, Kino 18 | 53) liegen nach der Korrektur
  teils neben den Gebäuden (Kino jetzt y 39–65) – nicht aus dem Original belegt, eigene Prüfung. Dateien:
  `UI/Screens/Manager/StadiumScreen.cs`. Spur: `ui-rest`.

### A326 – Bau-Tabelle und Baustellenanzeige

- **Ergebnis:** teils.
  - **Bau-Tabelle belegt:** Werte und „(kein Bau...)“ in Farbe 1 (`file43_3`, `file43_6`, `file44_6`; Ist-Ansicht Farbe 31,
    `file43_7`), Beschriftung wie Ist. Im Bau „SITZE MIT KISSEN (Noch 8 Wochen)“, übrige Zeilen „(keine Bautätigkeiten)“,
    rechts „(kein Bau...)“; mehrere Bauten zugleich (`file44_6`: Sitzkomfort 8, KNEIPEN 16, KLEIN 17 Wochen). Restwochen im
    Stand: FILE43 HOK 0x00FB2 (Projekt 3) = 8; FILE44A Projekt 6 = 1 (Restaurant am 10.09.1994 noch 1 Woche). Nach der
    Fertigstellung leer (`file44_8`).
  - **Ist-Ansicht im Bau belegt:** zeigt die alte Stufe (PLASTIKSCHALEN, `file43_7`; KNEIPEN in `file44a_10` während RESTAURANT
    im Bau).
  - **Baustellenanzeige rechts widerlegt:** Überfahren eines Bereichs im Bau zeigt nur „Gastronomie ausbauen“ (Maus (177 | 218)
    in der Zeile Gastronomie, KNEIPEN im Bau, `file44_6`) – kein „(Noch n Wochen)“, keine Stufenwahl, keine Kostenzeile. Das
    Remake schreibt dort Bereich ohne „ausbauen“ und „(Noch n Wochen)“ (`StadiumScreen.cs` `[APPROX]` „Laufende Baustelle“).
  - Ausbau-Frage **widerlegt:** Das Original nutzt das Fragefenster Datei 0x4C246 (Aufruf Datei 0x3E37A, Titel = Managername,
    Zeichen „?“, Zeilen „Möchten Sie Ihr Stadion ausbauen ?“, „Rabatt: r%“, „Bauzeit: ca. w Wochen“, Knöpfe „Ja.“/„Nein.“;
    `file43_5`, `file44_3`, `file44_5`). Das Remake zeichnet ein eigenes Fenster (Kostenzeile, „Aber klar“/„Kein Gedanke“,
    jeder andere Klick = Nein).
- **Fundstelle:** Bilder oben; Datei 0x3D5CA–0x3D5F8 (Text „ (Noch “ DGROUP 0xC4C, Zahl 0x0BAB2 ohne Auffüllen, „ Wochen“
  DGROUP 0xDD8, „)“ DGROUP 0x501F), Datei 0x3D5FB–0x3D607 (Farbe 31 − 30 · Modus), Datei 0x3E2EF–0x3E37A (Frage).
- **Umsetzung:** Zweig „Laufende Baustelle“ in `DrawRight` entfernen (auch im Bau „<Bereich> ausbauen“, ohne Stufenwahl);
  `DrawConfirm`/Eingabe durch `OriginalDialog` (Frage, drei Zeilen, Knöpfe „Ja.“/„Nein.“, nur linke Maustaste) ersetzen.
  Test: Bereich im Bau überfahren → Hinweis „… ausbauen“, keine Zeile 221; Frage über `OriginalDialog` mit den drei Zeilen.
  Dateien: `UI/Screens/Manager/StadiumScreen.cs`. Spur: `ui-rest`.

## Sitzung 4 – Saisonende mit Kaderabgang (FILE44 → FILE46)

Grundlage: FILE44 (14.04.1994, Byte 0x7A9 = 45, Kader 25, KJELLBERG Platz 24 mit +0x0B = 1, VALLIERE Platz 25 geliehen,
HOHENADL im Markt-Satz 4 Platz 5 mit +0x09 = 0x80), FILE46 (10.09.1994, Saisonzähler 1, Kader 24), zum Vergleich FILE41;
Bilder `file46_1…6`. Klartext wie in Teil 1 entschlüsselt (Kaderplatz HOK 0x06A27 + 0x618 · Satz + 0x34 · Platz, Spielerpool
HOK 0x0505F, 33 Byte; Nachrichten DGROUP 0xD294 = HOK 0x0AF72, 24 × 0x71).

### A362 – Liste hinter den Saisonende-Verhandlungen

- **Ergebnis:** teils. Die Liste ist die Kaderliste mit den Spalten von „Verträge“ (ART NAME SP ST TO STATUS E V.DAUER WERT),
  wie im Remake (belegt). Widerlegt ist ihr Inhalt während der Verhandlung: Das Original zeichnet die Liste **einmal vor** der
  Schleife (Datei 0x27C9F, `0x108:0x43`) und erst nach einem Abgang neu (Datei 0x27F31); `+0x0B = 1` und das neue Gehalt
  `+0x28` (Datei 0x27D15–0x27D70) schreibt es danach. Bei KJELLBERG steht daher in der Liste „Kein V.“ und WERT **18.800 DM**
  (altes Gehalt), im Fenster „1 Jahr“, „19.152 DM“. Das Remake setzt `MonthlySalary` schon im Kaderwechsel (`BeginRenewal`) und
  zeichnet die Liste live – WERT zeigte 19.152.
  - Lage: Kopfzeile y 19–23, Zeilen ab y 29 im Abstand 6 (Remake `FirstRowY` 29, `RowPitch` 6 belegt; `HeaderY` 20 – 1 Pixel
    zu prüfen). Fenster (KJELLBERG Zeile 23 → oben): Rahmen hell (2 | 29) bis dunkel (166 | 95), Schatten 2 Pixel (167–168,
    96–97); Remake `DialogRect` x = 1, Breite 166 → 1 Pixel links zu weit (gleiche Abweichung wie A377).
  - Vor der Verhandlung sind schon alle Saisonschritte gelaufen: SP/TO 0, alle Laufzeiten 2 → 1 Jahr, VALLIERE fehlt, HOHENADL
    steht wieder auf Platz 11 (siehe A091).
- **Fundstelle:** `file46_3` (Fensterrahmen (146 | 178 | 195) oben/links, (32 | 48 | 81) rechts/unten);
  em.exe Datei 0x27C9F, 0x27D15–0x27D70, 0x27D91 (`0x118:0x75`), 0x27F31; `emtool hok dump` FILE44/FILE46.
- **Umsetzung:** Ziel: Liste beim Öffnen der Saisonende-Verhandlungen als Abbild (Gehalt vor `BeginRenewal`, Laufzeit „Kein V.“)
  zeichnen und erst nach einem Abgang neu aufbauen; `DialogRect` x = 2, Breite 165 prüfen (gemeinsam mit A377), `HeaderY` am
  Bild prüfen. Dateien `UI/Screens/Team/ContractsScreen.cs` (das alte Gehalt hält der Bildschirm selbst oder der Kern als
  `Player.PreviousSalary` – nur falls ohne Schemaänderung möglich, sonst Abbild beim Aufbau). Testidee: Saisonende mit einem
  auslaufenden Vertrag, Pixel der WERT-Zeile = altes Gehalt, Fenster = neues; Pixelprobe gegen `file46_3`. Spur: `kader-start` Teil 4 (`UI/Screens/Team/*` gehört `kader-start`).

### A091 / A380 – Rückkehr von der Transferliste am Saisonende

- **Ergebnis:** belegt (neu): Ein eigener Spieler, der auf der Transferliste nicht verkauft wurde, kehrt am Saisonende in den
  Kader zurück und wird **nach Spielerart einsortiert**: HOHENADL (ABW) aus Markt-Satz 4 Platz 5 → Kader Platz 11 (vor BRANDL,
  dem ersten STU), alle folgenden rücken um eins nach hinten. Im Kaderplatz: `+0x0A` = 0 (nicht eingesetzt), `+0x15` = 0,
  `+0x09` 0x80 → 0, Laufzeit 2 → 1 (heruntergezählt, noch im Markt-Satz). Pool `+0x1A` = 0 (Manager) bleibt.
  - Mechanik: Die Saisonschleife über alle Sätze (auch 4 = Markt) vergleicht Pool `+0x1A` (Besitzer) mit dem Satz, in dem der
    Spieler steht (Datei 0x27547). Weicht er ab und ist Besitzer < 4, holt `0x108:0x5C` (Datei 0x42CB1, Einsortieren) einen
    Platz, der Kaderplatz wird kopiert (0x34 Byte, Datei 0x275D2), danach `+0x0C` = 0 und `+0x18` &= 0x7F (Datei 0x2764B,
    0x27665) und der alte Platz frei (`0x108:0x48`, Datei 0x27628). Vorher `+0x09` &= 0x3F (Datei 0x274EB). Gleicher Weg wie die
    Leihrückkehr (A115).
  - Das Remake behält den Spieler auf der Transferliste im Kader (A380, Teil 1); erst mit dem Umbau A380 braucht es diese
    Rückkehr.
- **Fundstelle:** `emtool hok dump` FILE44 (Markt Platz 5, `+0x09` = 0x80, `+0x0B` = 2) / FILE46 (Kader Platz 11); Bild `file46_3`
  (Zeile 11 mit 11.000 DM bereits am 30.04.); em.exe wie oben.
- **Umsetzung:** Mit A380 (`kader-start` Teil 2): Saisonende gibt nicht verkaufte eigene Transferlisten-Spieler an den Kader
  zurück, über `PlayerFactory.InsertSlot` (nach Art), nicht eingesetzt, Reihe leer, Laufzeit wie alle heruntergezählt, vor den
  Verhandlungen. Datei `Core/Player.Contract.cs` (Kaderwechsel). Testidee: Spieler auf die Liste, Saisonwechsel ohne Verkauf →
  Platz nach Art, `IsInLineup` false, Laufzeit − 1; Mehrsaisonprobe. Spur: `kader-start` Teil 4.

### A085 – Platzfolge des Vereins über den Saisonwechsel

- **Ergebnis:** belegt. Die Folge der 22 Verbleibenden ist unverändert; nur die Abgänge (KJELLBERG Platz 24, VALLIERE Platz 25)
  fallen weg und der Rückkehrer wird einsortiert (A091). Eingesetzt `+0x0A` und Reihe `+0x15` bleiben bei allen stehen.
- **Fundstelle:** `emtool hok dump` FILE44/FILE46 (Plätze 1–23).
- **Umsetzung:** keine (Remake `LiveSquad`/`RenumberPlayers`, `[ORIGINAL]` Runde 13). Spur: –.

### A115 – Leihende

- **Ergebnis:** teils.
  - Leihe vom Markt endet mit dem Saisonwechsel: VALLIERE (Pool `+0x1A` = 4, „von FRANKREICH“) ist in FILE46 weder im Kader
    noch im Markt; Pool `+0x1A` = 6 (frei). Code: Besitzer ≥ 4 → `+0x1A` = 6, Platz frei (Datei 0x27571–0x275FF, 0x27628).
    Schon vor der Verhandlung (Liste `file46_3` ohne VALLIERE). Belegt am Stand.
  - Versicherungsprämie: **nicht entscheidbar** am Stand (VALLIERE `+0x30`–`+0x33` = 0 in FILE41/FILE44). Nach dem Code kehrt ein
    an einen Manager zurückgehender Spieler mit einer **Kopie des Leih-Kaderplatzes** zurück (0x34 Byte, nur `+0x0C` und
    `+0x18`-Bit 0x80 gelöscht), also mit Laufzeit, Gehalt und Versicherung aus der Leihe; das Remake setzt dagegen Vertrag,
    Gehalt und `Insured` aus `PlayerLoan` (Stand beim Verleiher) zurück – nach Code widerlegt, am Stand nicht geprüft.
  - Das Leih-Kennzeichen ist `+0x0C` (VALLIERE 0xB9 in FILE41/44).
- **Fundstelle:** `emtool hok dump` FILE41/FILE44/FILE46 (Pool 179 VALLIERE `+0x1A` 4 → 6); em.exe Datei 0x27547–0x27665.
- **Umsetzung:** Ziel: Leihe vom Markt → am Saisonende frei (Pool frei), nicht zurück zu einem Verein; Leihe zwischen Managern →
  Rückkehr mit dem Kaderplatz des Entleihers (Laufzeit/Gehalt/Versicherung aus der Leihe) – erst nach Lesen von
  `0x48:0x2BD4` (Kopierrichtung) umsetzen, sonst `[APPROX]` lassen. Datei `Core/Player.Contract.cs` (Saisonwechsel,
  `PlayerLoan`). Testidee: Marktleihe → nach `StartNextSeason` frei und nicht im Kader des Herkunftsvereins. Spur: `kader-start`.

### Saisonende-Ablauf, Prämie, Ablöse (ohne eigene ID)

- **Ergebnis:** belegt, soweit am Bild prüfbar.
  - Ereignisse 30.04.1994: ein Symbol, „10. Platz der Bundesliga“, „Erhaltene Prämien: 100.000 DM“ = eine Einzelprämie, keine
    Tabellenprämie (Remake `SeasonPremium`, `Premium(...) = 0`). Welches Symbol (Topscorer/Torhüter/Fair-Play), nicht bestimmt.
  - Verhandlung: Alter 33 = Pool `+0x14` nach dem Altern (FILE44 32 → FILE46 33), Laufzeit 1 Jahr, Gehalt 19.152 DM =
    Wert(Gehalt) · (147 − 33) / 100 ⇒ Wert(Gehalt) = 16.800 DM; „Abbruch“ → „kehrt Ihrem Verein den Rücken...“, Ablöse
    155.500 DM = Wert(Preis) / 2 ⇒ Wert(Preis) = 311.000 DM (Remake-Formel `RenewalSalaryThirds`, `Compensation`).
  - Saisonzähler im Kaderplatz: `+0x00`, `+0x01`, `+0x03`–`+0x08` → 0; `+0x02`, `+0x1C`–`+0x26` bleiben (wie Runde 13, A226);
    Verletzungen (MENDE 14, BRANDL 7, LIEBSCH 2 Spiele) sind bis zum 10.09. ausgeheilt (`+0x09` Bit 0x02, `+0x0D` = 0) – Ort
    im Code nicht gelesen.
- **Fundstelle:** `file46_1…5`, `emtool hok dump` FILE44/FILE46.
- **Umsetzung:** Zahlenprobe als Test, sobald `hok-import` Stände laden kann: KJELLBERG aus FILE44 → `RenewalSalaryThirds` = 3 ·
  19.152, Ablöse 155.500 DM. Spur: `kader-start` (Test), sonst keine.

### A321 – Nachrichtenleiste (Ergänzung)

- **Ergebnis:** teils (neu): Das Datum oben ist **rot (Index 17)**, wenn es eine weitere Nachricht des Managers gibt, sonst
  schwarz (Index 0): Farbe = erste Zeile · (`0x4B9CD`(Platz, 1) ≠ 0xFF) · 0x11 (Datei 0x4B71E–0x4B73C); `0x4B9CD` sucht ab dem
  Platz vorwärts im Nachrichtenspeicher DGROUP 0xD294 (24 × 0x71) die nächste Nachricht desselben Managers (`& 7` = DGROUP
  0xA70D). FILE46 hat zwei Nachrichten (Plätze 0 und 1), `file46_6` zeigt die zum 13.04.1994 mit rotem Datum; `file37a_1` (eine
  Nachricht) schwarz. Die zwei Pfeile rechts stehen in beiden Bildern. Zeilen: erste Zeile y = 212 (Unterkante), weitere
  + 7 + 5, dann + 7 (Datei 0x4B6F0, 0x4B75D–0x4B769).
- **Fundstelle:** `file46_6` (Datum x 133–185, y 208–213, Farbe (178 | 16 | 16) = Index 17), `file37a_1`; em.exe Datei
  0x4B6F0–0x4B76D, 0x4B9CD–0x4BA56; HOK 0x0AF72 FILE46.
- **Umsetzung:** `NewsBar.Draw`: Datum in Index 17, wenn es nach der angezeigten noch eine Nachricht gibt, sonst Index 0
  (`[ORIGINAL]` mit Fundstelle). Datei `UI/Screens/Manager/NewsBar.cs`. Testidee: zwei Nachrichten → Kopfzeile Index 17, eine →
  Index 0. Spur: `ui-rest`.

### A088 – Spielerzähler zum Saisonwechsel (L14)

- **Ergebnis:** teils belegt (Vereinskader): Im Kaderplatz gehen `+0x00`, `+0x01` und `+0x03`–`+0x08` (Zähler je Wettbewerb,
  siehe A135) auf 0; `+0x02` und `+0x1C`–`+0x26` (Gesamtzähler) bleiben stehen. Pool `+0x1B`/`+0x1F` und Transfermarkt-Spieler
  nicht verglichen.
- **Fundstelle:** `emtool hok dump` FILE44/FILE46 (Kaderplätze Satz 0).
- **Umsetzung:** mit A135 (`kader-start` Teil 2, Zähler je Wettbewerb): Rücksetzung nur der Saisonzähler, Gesamtzähler
  behalten; Testidee in der Mehrsaisonprobe. `[APPROX]` für Pool und Markt bleibt (L14). Spur: `kader-start`.

### A189 – Gesperrte (Sitzungen 3–7)

- **Ergebnis:** nicht entscheidbar. In FILE43–FILE49A hat kein Kaderplatz (alle sechs Sätze) `+0x09` Bit 0x01; nur Verletzte
  (Bit 0x02: FILE43 HEISS, FILE44 MENDE/BRANDL/LIEBSCH, FILE44B MAYR, FILE48 HANTSCHKE).
- **Fundstelle:** `emtool hok dump`/Klartext FILE43, 43A, 44, 44A, 44B, 46–49, 49A.
- **Umsetzung:** keine; `[APPROX]` in `StadiumStatistics.cs` bleibt. Spur: –.

## Sitzung 5 – Bundestrainer (FILE47–49, FILE49A)

Grundlage: FILE47 (FILE18 geladen, Aufstellung des Bundestrainers bis auf die Reserve geleert, im Hauptmenü gesichert; nach dem
Laden wieder „Eingesetzt: 22“), FILE48 (FILE18 geladen, geleert, einen Tag weiter: DEUTSCHLAND – TSCHECHEI 0:17, Aufstellung
bleibt leer), FILE49 (FILE11 geladen, WM-Kader mit „auto“), FILE49A (dazu KÜNAST heraus, ENGLBRECHT von Hand hinein). Bilder
`file47_1…4`, `file48_1…6`, `file49_1…8`, `file49a_1…2`. Vergleich mit FILE11/FILE18. Offsets: Kaderplatz des WM-Kaders =
Satz 5, DGROUP 0x5DC3 + 5 · 0x618 + 0x34 · Platz (HOK 0x06A27 + 0x1E78 + …), Einstellung „automatisch aufstellen“ des
Bundestrainers DGROUP 0x37F (aktuell) / 0x380 (gesichert) = Block DGROUP 0x375 Byte 10/11 (HOK 0x0084E/0x0084F), Länderwerte
Vereinssatz 54 (DEUTSCHLAND) Felder 0x18–0x1D.

### A009 – Leere Aufstellung des Bundestrainers

- **Ergebnis:** widerlegt (Remake-Automatik bei leerer Aufstellung, keine Einstellung „automatisch“ für Satz 5, Länderwerte).
  - Das Leeren löscht bei allen Aufgestellten +0x0A und +0x15; die sechs Reservespieler (MERK, KÜNAST, DOLAK, BUKOWSKI, PURVES,
    R.REUTER) bleiben eingesetzt („Eingesetzt: 6“, `file47_3`). Dabei geht DGROUP 0x37F von 1 auf 0 (Herausnehmen schaltet
    „automatisch aufstellen“ ab wie beim Verein, FILE06); 0x380 bleibt 1 (FILE47: Block 0x375 `… 00 01`).
  - Der Bundestrainer hat „automatisch aufstellen“ von Anfang an eingeschaltet (FILE11/18/49: 0x37F = 0x380 = 1). Sein Zug:
    0x37F := 0x380, Hauptmenü 0x44E22 (dort bei jedem Aufbau Neuaufstellung 0x426BC, Datei 0x450C6), nach dem Menü
    0x380 := 0x37F, 0x37F := 0, 0x426BC (wirkungslos), Länderwerte 0x952C. Deshalb steht nach dem Laden von FILE47 wieder die
    volle Aufstellung (0x37F := 0x380 = 1, dann 0x426BC → 22), während in FILE48 das Weiterschalten 0x380 := 0 gesichert hat:
    Die Aufstellung bleibt über den Spieltag leer (FILE48 Block `… 00 00`, alle +0x0A außer der Reserve 0).
  - Die leere Aufstellung spielt: 0:17 gegen TSCHECHEI, kein Spieler bekommt ein Spiel (SP überall 0, auch die Reserve,
    `file48_6`); keine Warnung (die Warnung A300 betrifft den leeren WM-Kader). Das Remake setzt bei leerer Formation die
    Automatik ein (`NationalCoach.Lineup`, `MatchEngine.NationalSquad`, `TeamValueFormation`).
  - Länderwerte: Schon das Verlassen der leeren Aufstellung schreibt 0 in die Felder 0x18–0x1D (FILE47: `00 00 00 00 00 00`,
    FILE18: `58 5A 53 57 33 33`); das Remake schreibt bei leerer Formation nichts (`[APPROX]` in `UpdateNationalTeamValues`).
  - Statuszeile „Hängt halt so rum...“ beim nicht eingesetzten LEE (`file48_6`): belegt (`PlayerHintLine`, Status 0, nicht
    eingesetzt). Spalte NR nur bei Eingesetzten (2, 3, 11, 12, 24, 25 – wie A364 Teil 1).
- **Fundstelle:**
  - em.exe Datei 0x40ADE/0x40DE4 (Aufstellung: Herausnehmen setzt 0x375 + 2 · m = 0), 0x426CC–0x426E4 (Neuaufstellung nur bei
    Einstellung ≠ 0), 0x55599–0x555D6 (Zug des Bundestrainers: zurückholen, Menü, sichern, 0x426BC, 0x952C), 0x450C6 (Menü).
  - HOK 0x0084E/0x0084F: FILE18 `01 01`, FILE47 `00 01`, FILE48 `00 00`; Satz 5 +0x0A/+0x15 FILE18 → FILE47/48; Vereinssatz 54
    0x18–0x1D FILE18/47/48. Bilder `file47_2…4`, `file48_1…6`.
- **Umsetzung:**
  - Ziel: Einstellung „automatisch aufstellen“ für den Bundestrainer (Vorgabe an, gesichert/aktuell wie Teil 1 Taktik 375h);
    Neuaufstellung beim Aufbau seines Hauptmenüs, wenn an; Herausnehmen in der Aufstellung schaltet sie ab. Leere Formation
    ohne Einstellung = leere Aufstellung im Spiel (keine Remake-Automatik), Länderwerte 0.
  - Dateien: `Core/CupsNational.cs` (`Lineup`), `Core/CupsNational.Lineup.cs` (`TeamValueFormation`, Einstellung, Herausnehmen),
    `Core/CupsNational.GameState.cs` (`UpdateNationalTeamValues`), `Core/MatchEngine.National.cs` (`NationalSquad`); Anschluss
    Menüaufbau über `GameState.OnMainMenu`/`GameState.HotSeat.cs` (minimal, im PR begründet); ggf. Schemafeld mit Migration.
  - Testidee: FILE18-artiger Stand, Aufstellung leeren und Tag weiter → Länderspiel ohne aufgestellte Spieler (keine Spiele
    gezählt), Länderwerte 0; leeren ohne Weiterschalten, Menü neu aufbauen nach Laden → 22 eingesetzt.
  - Spur: `nationalteam`.

### A085 – WM-Kader von Hand

- **Ergebnis:** belegt.
  - Bei vollem Kader (29) bewirkt ein Klick auf einen weiteren Spieler nichts (Angabe Nutzer); Remake `ToggleNomination` →
    `SquadFull` ohne Meldung.
  - Herausnehmen rückt auf (0x3CA71 → 0x425B6, wie der Verkauf), Aufnehmen sortiert nach Art ein (0x3CABF → 0x42CB1/0x42D55,
    wie der Kauf): ENGLBRECHT (TW) steht in FILE49A auf Platz 3, wo KÜNAST stand. Remake: `Rows` stabil nach Art → ebenfalls
    Platz 3.
  - Herkunft: Aufnahme setzt Pool +0x1D auf das Land (ENGLBRECHT 7 → 54) und merkt den Verein in Kaderplatz +0x0C (Verein +
    0x80); Herausnehmen schreibt ihn zurück (KÜNAST 54 → 7). Dazu +0x0B = 1, +0x19 = 0x77. Remake: `NationalSquadMember.Club`.
  - ENGLBRECHT ist in FILE49A eingesetzt (Reserve): nicht durch die Aufnahme (0x3CABF setzt +0x0A nicht), sondern durch die
    Neuaufstellung beim nächsten Menüaufbau (Einstellung an, siehe A009). Im Remake bleibt er nicht eingesetzt – Folge von A009.
  - Die Bytes +0x0E, +0x13 (E), +0x14 und +0x28/+0x29 aller 29 Plätze unterscheiden sich schon zwischen FILE18 und FILE49
    (beide „auto“ aus FILE11): je Aufbau neu gewürfelt, kein Befund zur Handaufnahme.
- **Fundstelle:** em.exe Datei 0x3C928–0x3C9C0 (Klick, Platzsuche über Satz 5 +0x0F = DGROUP 0x7C4A), 0x3CA71–0x3CABE,
  0x3CABF–0x3CB8C; HOK Satz 5 und Pool far:8253 FILE49 → FILE49A; Bilder `file49_2…3`, `file49a_1…2`.
- **Umsetzung:**
  - Ziel: keine Änderung an der Aufnahme; Kommentar `[ORIGINAL]` mit Fundstelle an `ToggleNomination` (rückt auf, nach Art,
    Herkunft +0x0C). Eingesetzt nach Aufnahme kommt mit A009.
  - Dateien: `Core/CupsNational.GameState.cs` (nur Kommentar). Testidee: voller Kader + Klick → `SquadFull`, Kader unverändert;
    TW heraus und TW hinein → Zeile 3.
  - Spur: `nationalteam`.

### A293 – „Endrunde“ nach der Gruppenphase

- **Ergebnis:** belegt (Knöpfe) / widerlegt (Startseite „Paarungen → WM“).
  - Tabellen → WM zeigt auch nach der Gruppenphase nur die Gruppentabelle mit Knopf „Gruppe 2“ bei (237 | 211) (`file49_6`);
    keinen Knopf „Endrunde“ – Aussage Runde 13 (L6) bestätigt.
  - Paarungen → WM öffnet am 24.04.1994 direkt „WM-Endrundenspiele“ (Viertelfinale, Knopf „Gruppe“ bei (136 | 201),
    `file49_7`); „Gruppe“ führt zu „Gruppe 1   5.Spieltag“ mit den vier Knöpfen (Pfeil, „Endrunde“, „Gruppe 2“, Pfeil) ab
    x = 39 + 65 i, y = 180 (`file49_8`). Lagen wie im Remake (`WorldCupKnockoutScreen`, `CupPairingsScreen`).
  - Startseite: Der Menüaufbau setzt die gemerkte Seite von „Paarungen → WM“ (DGROUP 0x5127) auf 10 (Endrunde), sobald die
    WM-Stufe DGROUP 0x7AE = 5 ist (nach den fünf Gruppenspieltagen). Das Remake beginnt immer mit Gruppe 1 (Seite 8) und merkt
    keine Seite (A268). Der Kommentar in `WorldCupKnockoutScreen` („nach der Gruppenphase über die Gruppentabelle“) ist falsch.
- **Fundstelle:** em.exe Datei 0x44A5A–0x44A6A und 0x44B15–0x44B1C (0x44A48, Aufruf aus 0x44E22: `mov byte [0x5127],0xa`
  bei [0x7AE] = 5), 0x458A2–0x458BB (Kachel mit gemerkter Seite); Bilder `file49_5…8`.
- **Umsetzung:**
  - Ziel: „Paarungen → WM“ öffnet ab WM-Stufe 5 „WM-Endrundenspiele“ (Rückweg „Gruppe“ → Gruppe 1, Spieltag 5); Kommentar in
    `WorldCupKnockoutScreen.cs` berichtigen. Gemerkte Seite allgemein bleibt A268.
  - Dateien: `UI/Screens/Competitions/WorldCupScreen.cs` (`WorldCupMenuEntries.Pairings`), `WorldCupKnockoutScreen.cs`
    (Kommentar), ggf. `CupPairingsScreen.cs`.
  - Testidee: Stand nach fünf Gruppenspieltagen → Kachel „WM“ (Paarungen) öffnet `WorldCupKnockoutScreen`; davor
    `CupPairingsScreen`.
  - Spur: `nationalteam`.

## Sitzung 6 – Speichern-Dialog mit mehr als 48 Einträgen (A338, A339)

Grundlage: Aufnahme des Nutzers in DOSBox 0.74-3 (Verzeichnis C:\EM, 53 Einträge: 5 Verzeichnisse, AUTOSAVE.HOK, EM.EXE,
FILE01…, INFOFILE, KOPIE01–KOPIE30 als Kopien von FILE46), Bilder `a338_1…3`, Angaben des Nutzers in der README des
Referenz-Repos. Disassembly Ov 0x140: Dialog Datei 0x5DBE5 (Stub 0x140:0x7F), Spielstand-Info Datei 0x5B661 (Stub 0x140:0x57).
Kopf von FILE46 (unverschlüsselt) zum Abgleich mit dem Infofeld (KOPIE30 = FILE46).

### A338 – Bild und Bedienung des Speichern-Dialogs, „SEITE:“, Blättern

- **Ergebnis:** Bild belegt (gleicher Aufbau wie „Laden“, Titel „Speichern“; 3 Spalten × 16 = 48 Einträge je Seite,
  Verzeichnisse vor den Dateien); Bedienung des Remakes widerlegt:
  - **„SEITE:“ gibt es nicht.** Der Text DGROUP 0x270C liegt nur in der Zeigertabelle (DGROUP 0xBC0); kein Befehl in em.exe
    liest 0xBC0/0xBC2 (Bytefolge über die ganze Datei gesucht), dagegen werden die Nachbarn „VERZEICHNIS:“ (0xBB8),
    „FILENAME      :“ (0xBBC) und „LÖSCHEN“ (0xBC4) gelesen (Datei 0x5DFD3, 0x5E086, 0x5DC45). Bilder: kein Seitentext.
  - **Blättern nur über das Pfeilpaar** 0x4BA58 (Datei 0x5E3B5: Flächen (150 | 169) = −1, (174 | 169) = +1, je 8 × 9,
    Halten = 1 → Wirkung beim Loslassen), nur bei linker Taste (Ereignis DGROUP 0x5A16 = 1) und Maus-y 169–177 (Datei
    0x5E386–0x5E3A0). −1: Versatz [bp−0x18] − 48 nur, wenn Versatz > 47; +1: Versatz + 48 nur, wenn kleiner als Verzeichnisse
    + Dateien ([bp−0x10] + [bp−0x12]) (Datei 0x5E3C2–0x5E3EC); am Anfang bzw. Ende ohne Wirkung, dann die drei Spalten leeren
    und neu füllen (Datei 0x5E3F2–0x5E42B). Bild: oberer Pfeil etwa (152–157 | 172–175), unterer (177–181 | 172–174).
  - **Keine Tasten, kein Mausrad:** Die Dialogschleife (Datei 0x5E200–0x5E20C, 0x38:0x878) liest nur Mausereignisse; Tastatur
    gibt es nur im Eingabefeld 0x0BAFB, das ein Linksklick auf VERZEICHNIS/FILENAME öffnet (Datei 0x5E42E–0x5E530). Angabe
    Nutzer: Mausrad und Tastatur wirkungslos.
  - **Markierung unter der Maus:** Beim Überfahren eines Eintrags zeichnet das Original links neben der Zeile einen roten
    Marker (Datei 0x5E2C7–0x5E329, Aufruf 0xF8:0x25, Fläche (43 + 80 · Spalte | 55 + 7 · Zeile)–(45 + 80 · Spalte | 60 + 7 ·
    Zeile)) und löscht ihn beim Verlassen mit Farbe 3 (Datei 0x5E334–0x5E377). Trefferfeld der Liste: 42 < x < 266,
    53 < y < 166, Zeile = (y − 54) / 7, Spalte über 0x7A + 80 · Spalte (Datei 0x5E221–0x5E266). Bild `a338_3`.
- **Remake-Abweichungen** (`UI/Screens/SaveLoad/SaveLoadScreen.cs`):
  - Z. 167–168: Mausrad blättert – entfällt.
  - Z. 169–172: Bild↑/Bild↓ blättern – entfällt; Pfeilflächen `PageUpRect`/`PageDownRect` (Z. 61–62: (146 | 166) 16 × 14,
    (170 | 166) 18 × 14) statt 8 × 9 bei (150 | 169)/(174 | 169), Wirkung beim Drücken statt beim Loslassen.
  - Z. 454–455, 473–497: Pfeile nachgezeichnet (`DrawArrow`, `[APPROX]`) statt Baustein `ArrowPair` (kb\4, Halten = 1;
    Aufrufstelle 0x5E3B5 steht schon in `ArrowPair.cs`).
  - Z. 507–508: „SEITE: n“ bei mehr als einer Seite – entfällt; Klassenkommentar Z. 22 und Z. 31–32 berichtigen.
  - Roter Marker unter der Maus fehlt.
- **Fundstelle:** em.exe Datei 0x5DBE5 (0x5E200–0x5E42B), 0x4BA58; DGROUP 0x270C, 0xBB8–0xBC7; Bilder `a338_1` (Seite 1,
  48 Einträge), `a338_2` (Seite 2, KOPIE26–30), `a338_3`.
- **Umsetzung:** Ziel: Blättern nur über `ArrowPair(150, 169, 174, 169, hold: true)`, Schritt 48 mit den Grenzen oben, kein
  Seitentext, keine Tasten/Mausrad, roter Marker unter der Maus. Dateien: `UI/Screens/SaveLoad/SaveLoadScreen.cs`.
  Testidee: 53 Einträge → Seite 2 nur über Klick und Loslassen auf (174 | 169), weiterer Klick bleibt auf Seite 2, (150 | 169)
  zurück, Seite 1 bleibt; Mausrad und Bild↓ ändern nichts; Bildprobe ohne „SEITE:“. Spur: `hok-import` Teil 4 (L6).

### A339 – Spielstand-Info beim Rechtsklick (Datei 0x5B661)

- **Ergebnis:** widerlegt (Remake zeigt nichts). Rechtsklick (Ereignis 3) auf einen Dateieintrag baut Verzeichnis + „\“ +
  Name (Datei 0x5E5ED–0x5E659) und ruft 0x5B661; das Namensfeld bleibt unberührt (schon `[ORIGINAL]` im Remake). 0x5B661:
  - Datei öffnen (0x50:0x17); scheitert das, Fehlerweg 0x5A862 und Rückgabe 0. Kopf lesen: 10 Byte Kennung, mit DGROUP
    0x55DF verglichen; falsch → Meldung „Kein / Eishockey Manager / Spielstand. / !!!“ (DGROUP 0x45D7–0x45FA, 0x4BCCC) (Datei
    0x5B68E–0x5B6EA). Dann 4 Byte Jahr, 1 Byte Managerzahl, 1 Byte Spieltag, 0xA0 Byte = 4 Managereinträge zu 0x28 (Datei
    0x5B6EC–0x5B736). Abgleich FILE46: Kennung „KRON_0.99“, Jahr 1994, 1 Manager, Spieltag 1, Eintrag 0: Liga 0, Byte +1 = 0,
    „Joerg“ (+2), „KÖLNER EC“ (+0x15) – genau der Inhalt von `a338_3`. Der Kopf steht unverschlüsselt vor dem Datenteil.
  - Fenster 0x0B155 bei (71 | 71), 8 × 3 Kacheln, Stil 3 (Datei 0x5B73C–0x5B764); Bild: Rahmen (71 | 71)–(262 | 127), orange
    wie die übrigen Rahmen.
  - Zeile 1: „SPIELSTAND“ (DGROUP 0xBAC „spielstand“ über 0x4BD7E in Großbuchstaben) + „:“ + Pfad, mittig zwischen x 72 und
    261, y 81, Farbe 7, darunter Linie (77 | 83)–(254 | 83) Farbe 7 (Datei 0x5B767–0x5B7EA).
  - Zeile 2: „ Jahr“ (DGROUP 0x2852) + Jahr + „, “ + Spieltag + „.Spieltag“ (DGROUP 0x1B2D), mittig, y 90, Farbe 1 (Datei
    0x5B7ED–0x5B8A0); Bild „Jahr 1994, 1.Spieltag“ (Leerzeichen vor der Zahl im Bild; Zahlformat 0x0BAB2 nicht gelesen).
  - Je Manager i < Managerzahl, y = 99 + 7 · i, Farbe 1: Name bei x 76, Verein bei x 133, Liga (DGROUP 0x9EC + 4 · Liga:
    Bundesliga/Zweite Liga/Oberliga) bei x 212 nur, wenn Byte +1 = 0 (Datei 0x5B8A3–0x5B9A5; Bedeutung von +1 nicht gelesen).
  - **Halten:** Schleife 0x38:0x878 bis Ereignis 2 oder 4 (Loslassen), dann Fenster zurück (0x0B447) und weiter im Dialog
    (Datei 0x5B9A8–0x5B9D4). Das Feld bleibt also nur bei gehaltener Taste stehen (Angabe Nutzer bestätigt).
  - FILENAME leer in `a338_3`: Die Rechtsklick-Strecke schreibt das Namensfeld nicht; das leere Feld stammt aus der Bedienung
    davor (kein Befund).
- **Remake-Abweichung:** `TryRightClickEntry` (Z. 220–232) und der Klassenkommentar Z. 26–28 (`[APPROX]` #326): kein Infofeld.
- **Fundstelle:** em.exe Datei 0x5B661–0x5B9DC, 0x5E5ED–0x5E667; DGROUP 0x2852, 0xBAC, 0x1B2D (über 0x670), 0x9EC; Kopf
  FILE46; Bild `a338_3`.
- **Umsetzung:** Ziel: Infofeld solange die rechte Taste gehalten wird (ausblenden beim Loslassen beider Tasten), Inhalt
  Pfad, Jahr/Spieltag, je Manager Name/Verein/Liga mit den Lagen oben; bei Original-Dateien `*.HOK` aus dem Kopf (176 Byte,
  unverschlüsselt), bei Remake-Ständen aus den gleichen Feldern des eigenen Formats; falsche Kennung → Meldung „Kein
  Eishockey Manager Spielstand. !!!“. Dateien: `UI/Screens/SaveLoad/SaveLoadScreen.cs`, Kopf-Leser in
  `Formats/OriginalSaveGame*.cs`. Testidee: Kopf von FILE46 → „Jahr 1994, 1.Spieltag“, „Joerg“, „KÖLNER EC“, „Bundesliga“
  (`[GameDataFact]`); Rechts gedrückt → Feld sichtbar, Loslassen → weg, Namensfeld und Auswahl unverändert. Spur:
  `hok-import` Teil 4 (L6).

## Umsetzung Teil 2

Je Punkt stehen Ziel, Dateien und Testidee oben. Zusammenfassung nach Spuren:

- **`ui-rest` Teil 3** (Plan Punkt 3): A323/A325 (Tabelle, Folge, Maske 0xC0), A326 (rechts ohne „(Noch n Wochen)“,
  Fragefenster 0x4C246) in `StadiumScreen.cs`; dazu A321 (rotes Datum, `NewsBar.cs`) mit Teil 2.
- **`kader-start` Teil 2:** A115 (Marktleihe endet frei; Rückkehr zum Manager erst nach Lesen von `0x48:0x2BD4`), A088 (mit
  A135).
- **`kader-start` Teil 4** (Plan Punkt 4): A362 (Abbild der Liste, Fenster x = 2, `ContractsScreen.cs`), A091/A380 (Rückkehr
  von der Transferliste am Saisonende; setzt den Umbau A380 aus Teil 2 voraus), Zahlenprobe Saisonende als Test; A085 (Verein)
  ohne Änderung.
- **`nationalteam`** (Folgeteil, Plan Punkt 3): A009 (Einstellung „automatisch aufstellen“ für den Bundestrainer, leere
  Aufstellung spielt leer, Länderwerte 0), A085 (nur Kommentar), A293 (Startseite „Paarungen → WM“ ab WM-Stufe 5, Kommentar in
  `WorldCupKnockoutScreen.cs`).
- **`hok-import` Teil 4** (L6): A338, A339 in `SaveLoadScreen.cs`, Kopf-Leser in `Formats/OriginalSaveGame*.cs`.
- **`pruefung-karte`:** Feldbelege für `docs/formate/spielstand-original.md` (Stadion DGROUP 0x9771, Restwochen, 0x37F/0x380,
  Kopf 176 Byte, Kaderplatz `+0x0C` Leihe/Herkunft) und Inventur-Stand der IDs oben.

## Offene Fragen Teil 2 an die Leitstelle

1. **A009, Schema:** Die Einstellung „automatisch aufstellen“ des Bundestrainers braucht zwei Bytes im Spielstand (aktuell und
   gesichert, wie Taktik 375h beim Verein) und einen Anschluss an `GameState.OnMainMenu`. `nationalteam` oder zusammen mit
   Taktik 375h in `kader-start` Teil 2?
2. **A362:** Das alte Gehalt in der Liste braucht entweder ein Abbild im Bildschirm oder ein Kernfeld; Vorschlag Abbild ohne
   Schemaänderung (`kader-start` Teil 4).
3. **A115:** Leihe zwischen Managern ist am Stand nicht belegt (nur nach Code); umsetzen nach Code oder `[APPROX]` lassen?
4. **Fehlende Aufnahmen** (Runde 15): Kino MITTEL mit Restaurant und die Bereiche Autobahn, Halboffen, Bowlingbahn im
   Stadionbild (A325), Spieler mit Sperre über Spiele (A189), Rückkehr aus einer Leihe zwischen Managern mit Versicherung
   (A115).

## Selbstprüfung

`python3 tools/reverse-engineering/funktionskarte.py gamedata/em.exe --repo . --csv <scratch>` ohne `--write`, Base `59f98cf`
gegen diesen Branch: je 679 Funktionen, Status unverändert (umgesetzt 268, dokumentiert 152, genähert 2, unbekannt 257); diese
Datei ist Fundstelle in 20 Funktionen und ändert keinen Status. Den Stand in `re-abdeckung.md` und die Zeilen in `inventur.md`
schreibt `pruefung-karte` (Welle 2).
Teil 2: derselbe Aufruf, Base `6568de7` (Teil 1) gegen diesen Branch: je 679 Funktionen, Status unverändert (268/152/2/257);
die Datei ist jetzt Fundstelle in 46 Funktionen (vorher 20). Keine Originaldaten oder Bilder im Repo; Bildnamen verweisen nur auf das Referenz-Repo.
