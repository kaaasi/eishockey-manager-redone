# Auswertung DOSBox-Aufnahme `runde-13` (Spur `re-dosbox`, Runde 14)

Stand: Teil 1 abgeschlossen (geplanter Lauf Claude Opus 5.5, PR #628): Sitzungen 1 und 2, Spielstände FILE29–41 und FILE37A.
Teil 2 (Sitzungen 3–6, FILE43–49) folgt in einem eigenen PR in derselben Datei.

Quellen: Spielstände und Bilder aus `kaaasi/eishockey-manager-referenz` (`original/spielstaende/`, README mit den Angaben des
Nutzers, Stand `36aeca9`), `emtool hok dump`, der entschlüsselte Klartext (Verfahren `docs/formate/spielstand-original.md`)
und das Disassembly von `em.exe` (`tools/reverse-engineering/disasm.py`). Schreibweise wie in `re-runde-13.md`: Adressen in
`em.exe` als `Datei 0x…`, DGROUP-Adressen als `DGROUP 0x…`, Offsets im entschlüsselten Spielstand als `HOK 0x…`.
Bildkoordinaten sind Spielkoordinaten (320 × 240): Die DOSBox-Bilder (642 × 507) zeigen das Spiel doppelt ab Pixel (1 | 26).
Die Farben der Bilder stimmen exakt mit der Palette `kb\21.vga` überein (6 Bit), Indizes sind daher direkt ablesbar.

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
| A373 | belegt | Pfeil oben = nächster Modus, unten = voriger (Datei 0x487A8–0x487B8, 0x48C07–0x48C25) | `ui-rest` |
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
| A115 | nicht entscheidbar | FILE42 (Leihende) nicht erzeugt (Angabe Nutzer) | – |
| A088, A009, A085 | kein neuer Beleg | Sitzungen 1–2 enthalten keinen Saisonwechsel und keinen Bundestrainer; Teil 2 | – |

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
    Deutschen Meister. Nach dem Füllen bleiben die Einträge 8–31 der Zeile 0 (Füllwerte), Platz 0 gilt also auch danach als
    „Teilnehmer“.
  - Folgen: (1) U15-c: Der Managerverein landet immer auf Platz 0. FILE29: ESV KAUFBEUREN auf Platz 0, Feld 0x9744 = 1. Am
    echten Europapokal nimmt er nicht teil (Zeile 1 nach dem Füllen: 148 75 9 110 138 125 95 114 – Platz 9 = DÜSSELDORFER EG
    = Meister DGROUP 0xD0E6 = 9). Die Wirkung von 0x9744 = 1 außerhalb des Highscores ist nicht gelesen. (2) U15-a: Der
    Zufallsplatz r der Start-Liga ist nie Platz 0 (statt „nie der Meister, Verein 2“). Verträglich mit allen Ständen:
    Managervereine auf Platz 2 (FILE01), 1 (FILE10), 3/8 (FILE09), keiner auf Platz 0.
- **Fundstelle:** em.exe Datei 0x29CFA–0x29D23 (Bedingung, `mov byte [bx-0x68bc],1` = DGROUP 0x9744 + 0x1AF · m),
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
- **Fundstelle:** em.exe Datei 0x54608–0x5464A (Argumente `[bx−0x7D78]` = DGROUP 0x8288 + 0x50 · Heim/Gast an 0x3F185).
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

- **Ergebnis:** belegt. Pfeil oben = nächster Wert, unten = voriger, für alle vier Modusknöpfe; Reihenfolge der Reihen-Modi
  Automatisch → Spiel nur mit 2 Reihen → Energiestärkster Block → Nach Gegnerreihe → Minuten-Vorgabe → Voll manuell (wie
  `LineChangeMode`).
- **Fundstelle:** em.exe Datei 0x487A8–0x487B8 (`[bp−0xC]` = 1, wenn y über der Mitte des Pfeilpaars), 0x48C07–0x48C25
  (Schritt +1 bei `[bp−0xC]` = 1, −1 bei 0), Grenzen 0x48BD2–0x48BFF (0 … Anzahl − 1, kein Umlauf). Bilder `file33_02…08`.
- **Umsetzung:** `[APPROX] Richtung der Pfeile` in `TacticsScreen.cs` → `[ORIGINAL]`. Prüfen, ob `Cycle` umläuft (Original:
  bleibt an den Enden stehen). Spur: `ui-rest`.

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
- **Umsetzung:** `[APPROX]` in `CupsNational.Lineup.cs` → `[ORIGINAL]` mit diesen Bildern. Spur: `kader-start` (Kommentar
  in fremder Datei nur mit Freigabe; sonst an `nationalteam` melden).

### A167 – Neue Kaderspieler nicht eingesetzt (auch Handmodus)

- **Ergebnis:** belegt, jetzt auch bei ausgeschalteter Einstellung (bisher offen, L7). KJELLBERG (Kauf, FILE35) und VALLIERE
  (Leihe, FILE41) stehen ohne „eingesetzt“ und ohne Reihe im Kader; Einstellung 0x375 = 0.
- **Fundstelle:** FILE35 Platz 26 KJELLBERG, FILE41 Platz 25 VALLIERE; Bilder `file35_5`, `file40_10`.
- **Umsetzung:** `[APPROX]` (Kauf bei ausgeschalteter Einstellung) in `Squad.Lineup.cs` → `[ORIGINAL]`. Spur: `kader-start`.

### A091 – Kaderplätze bei Kauf, Leihe, Verkauf und Transferliste

- **Ergebnis:** belegt. Kauf KJELLBERG (STU) → Nr. 26 hinten (Gruppe STU ist die letzte); Verkauf SCHIFFL (Nr. 7, FILE37A) →
  MENDE 8 → 7, alle folgenden rücken auf, KJELLBERG 26 → 25; HOHENADL (Nr. 9) auf die Transferliste → Plätze rücken auf
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
  Eintrag 7): Zeilen 148 und 164. Übrige Trennlinien `file40_04`: Zeilen 52 + 16 · k Index 31, Zeile darunter Index 4.
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

- **A115** (Versicherung bei Rückkehr aus Leihe): nicht entscheidbar, FILE42 fehlt (Angabe Nutzer: optional später).
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
- **`pruefung-karte`:** Feldbelege für `docs/formate/spielstand-original.md` (0x9744, 0x8288, 0x375/0x376, +0x03–0x08,
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

## Teil 2 – Sitzungen 3–7 (FILE43–49A)

In Arbeit (geplanter Lauf Claude Opus 5.5, PR folgt).

## Selbstprüfung

`python3 tools/reverse-engineering/funktionskarte.py gamedata/em.exe --repo . --csv <scratch>` ohne `--write`, Base `59f98cf`
gegen diesen Branch: je 679 Funktionen, Status unverändert (umgesetzt 268, dokumentiert 152, genähert 2, unbekannt 257); diese
Datei ist Fundstelle in 20 Funktionen und ändert keinen Status. Den Stand in `re-abdeckung.md` und die Zeilen in `inventur.md`
schreibt `pruefung-karte` (Welle 2). Keine Originaldaten oder Bilder im Repo; Bildnamen verweisen nur auf das Referenz-Repo.
