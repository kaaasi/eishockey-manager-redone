# Tabellen und Aufstiegsrunde im Original-Spielstand (Spur `hok-patch-tabellen`, Teil 1)

Runde 27, Nachtrag (H1 = D). Beleg ohne Produktcode: wer die Tabellenblöcke in `em.exe` schreibt und liest, was zueinander passen
muss, welche Konstellation den Sonderfall von A667 auslöst und welche Vorlage sich eignet; dazu die Vorlage für A671.
Alle Belege sind Nachbildungen über die Bytes von `em.exe` (`zusammenarbeit.md` §5 Punkt 7) mit den Skripten in
`tools/reverse-engineering/hok/` (Emulator `abweichungen/emu16.py`); Ausgaben nennen nur Satz- und Platznummern.
Satzbezeichnung: „Satz“ = Vereinsplatz (Index in `far:9657`, 0–11 Bundesliga, 12–23 2. Liga, 24–35 Oberliga). Offsets im
Spielstand als `HOK 0x…`, sonst DGROUP-Adressen bzw. „Datei 0x…“ (em.exe).

## Ergebnis in Kürze

1. **Nichts wird beim Laden neu berechnet.** Das Laden (Datei 0x5C3E3, Blöcke über 0x5A9AE) ruft keine Tabellenfunktion
   (`bloecke.py`: „davon Tabellenfunktionen: keine“). Liga- und Gruppentabellen werden **aufaddiert**, nicht aus den Ergebnissen
   neu gerechnet: Liga je Spieltag über 0x50B45 → 0x50283, Gruppen je Termin über 0x30852 → 0x303D9.
2. **Gruppentabellen der Aufstiegsrunde** (Wettbewerb 1) an FILE22 und FILE44 **exakt** aus den gespeicherten Gruppentoren
   nachgerechnet (`gruppentabelle.py`, Punkte, Tore, Gegentore und Platzliste je Gruppe gleich). Punkte/Tore stehen **je
   Teilnehmer**, nicht je Platz (Korrektur zur Blocktabelle in `formate/spielstand-original.md`, Teil 2).
3. **Ligaplatzliste `5CCB`** wird nur bis zum 44. Spieltag umsortiert (0x50B45 kehrt ab DGROUP 0x7A9 = 45 sofort zurück,
   Datei 0x50B4D–0x50B71); das Hauptmenü sortiert vorher bei jedem Aufbau (Datei 0x44EFD). Nach der Saison bleibt eine gepatchte
   Platzliste also stehen; davor muss sie ein Fixpunkt der Sortierung sein (`ligatabelle.py`).
4. **Der 29. April liest nur** Platzliste `CD94`, erste Paarungszeile `CF20` der Aufstiegsrunden, die Ligaplatzliste `5CCB`,
   Konto/Liga/Status/Verein der Manager und Vereinsfeld +0x16 – nicht Punkte, Tore, Ergebnisse oder Tabellensätze.
   Nachbildung `zwangsabstieg.py` (Datei 0x55F00–0x563AD samt Tausch 0x36169 aus den Bytes) **reproduziert FILE17** (DOSBox-Lauf
   FILE16A → FILE17): gespeicherte erste Paarungszeile gleich, Satz 23 → Bundesliga, Satz 9 → Oberliga.
5. **A667 braucht einen Bundesliga- und einen Zweitliga-Manager**, beide unter der Grenze – nicht zwei Bundesliga-Manager. Mit
   zwei Bundesliga-Managern (Fälle 5–7) gibt es keinen doppelten Platz über beide Gruppen. Das Remake gibt in den
   Fällen 0–7, für FILE17 und für die Prognose der Vorlage dieselben Ligen wie em.exe (Gegenprobe mit `GameState.InsertForcedRelegation`/`RelegationExchange`/
   `ResolveDoubleMoves`, nicht eingecheckt); offen bleibt nur die Pflichtprobe im Original.
6. **Vorlage A667: FILE44** (14.04.1994, Aufstiegsrunde fertig, Tabellen original und stimmig) **ohne Tabellenpatch**: zwei Manager
   über die Kachel „Neuer“ (Bundesliga-Manager auf Satz 3, Zweitliga-Manager auf Satz 16), dann nur Konten patchen. Tabellen
   patchen ist für A667 nicht nötig; nötig ist höchstens ein Feld für die Start-Liga (DGROUP 0x11A). Abschnitt „Vorlage A667“.
7. **A671:** Die Nachbildung zeigt bei leeren Gruppentabellen (Modus „nur Weltkugel“, FILE18) für einen Managerverein auf dem
   letzten Oberligaplatz Konto 1.200.000 Drittel = 400.000 DM und Status 0x03. Im Modus 1 steht der Managerverein in allen
   vorhandenen Ständen auf einem **Bundesligasatz** (FILE11/18/20: Satz 7, FILE54: Satz 0); Vorlage nur über ein neues Spiel
   „nur Weltkugel“ mit Start-Liga Oberliga, falls die Spielvorbereitung das zulässt (Abschnitt „Vorlage A671“).
8. **Plan-Angabe berichtigt:** FILE75 hat zwei **Zweitliga**-Manager (Satz 12 und 23, Spielstart 10.09.1993), keine
   Bundesliga-Manager.

## Ausführen

```
python3 -I tools/reverse-engineering/hok/bloecke.py ~/referenz/original/gamedata/em.exe --cache <scratch>/an.pickle
python3 -I tools/reverse-engineering/hok/gruppentabelle.py ~/referenz/original/spielstaende/FILE22.HOK   # ebenso FILE44
python3 -I tools/reverse-engineering/hok/ligatabelle.py ~/referenz/original/spielstaende/FILE22.HOK      # ebenso FILE18, FILE05
python3 -I tools/reverse-engineering/hok/zwangsabstieg.py                 # Fälle 0–11
python3 -I tools/reverse-engineering/hok/zwangsabstieg.py --check17       # Gegenprobe FILE17
python3 -I tools/reverse-engineering/hok/zwangsabstieg.py --hok ~/referenz/original/spielstaende/FILE44.HOK \
        --add 3:0:-2000003 --add 16:1:-2000003                            # Prognose Vorlage A667
```

`em.exe` liegt im Klon als `original/gamedata/em.exe` (klein geschrieben); `emu16.load` nimmt ohne Pfad
`~/referenz/original/gamedata/em.exe` und fällt auf `EM.EXE` zurück.

## Schreiber und Leser je Block

Aus `bloecke.py` (W = schreibt, R = liest, P = Zeiger als Direktwert, Zugriff in der gerufenen Funktion). Laden/Sichern
(0x5C3E3, 0x5B9DD) nur als P. Deutung der Funktionen aus `reverse-engineering.md`, `formate/spielstand-original.md` und dieser
Analyse.

| Block | Schreiber | Leser (Auswahl) | Neu berechnet? |
|---|---|---|---|
| `8257` Tabellensätze (DGROUP 0x825A + 0x50 · Satz) | u. a. 0x50283 (Ergebnis buchen, nur bei DGROUP 0xD0C2 = 7/8), 0x51049 (Platzfeld 0x8288), Start 0x2EF1B/0x436DD, Tausch 0x36169 (Schalter ohne 0x80), 0x5667A; der Block reicht bis DGROUP 0x9656, nicht jeder Treffer betrifft die Tabellensätze | 0x50B45 (Sortierung), 0x4F78D (Ansicht), 0x5667A (Ewige Tabelle) | nein, aufaddiert je Ligaspieltag |
| `5CCB` Platzliste der Ligen | 0x50B45 (Sortierung, nur 0x7A9 ≤ 44), Ligamischung 0x35EDD (Saisonwechsel), Start 0x2EF1B/0x437D5 | Auslosung 0x30427 (Plätze 11, 12 oben, 1, 2 unten), Play-offs 0x31934, 29. April 0x54C9F | nur Sortierung aus `8257` |
| `A0CB` Ergebnisse | 0x369C7 (Spieltag) | 0x50B45 (buchen), Anzeige | nein |
| `CD88` Gruppen-Platzliste (+12 · Wettbewerb + 6 · Gruppe + Platz → Teilnehmer) | Auslosung 0x30427, Gruppenspieltag 0x30852 (Tauschsortierung) | 0x54C9F (Zwangsabstieg und Austausch), Anzeigen 0x2F5B6/0x31CD5 | Sortierung je Termin |
| `CD58`/`CD28`/`CCF8` Punkte/Tore/Gegentore (Index Teilnehmer) | 0x30427 (Null), 0x30852 über 0x303D9 (aufaddiert) | 0x30852 (Sortierung), Anzeigen | nein |
| `CED8` Gruppenpaarungen (Satznummern; Zeile 0 = Teilnehmer 0–3) | 0x30427, 29. April 0x54C9F (Datei 0x56043/0x560B4, Einschub in Zeile 0) | 0x30852, 0x54C9F, Anzeigen | nein |
| `CDB8` Gruppentore (+30 je Marke) | 0x30852, 0x3183C, 0x30427 (Null) | Anzeigen 0x2FC2F/0x31CD5 | nein |
| `CFF4`/`CFFC` Gruppenteilnehmer | 0x30427, 0x3139F, 0x31934, 0x54C9F | 0x30852 (Teilnehmer suchen), Anzeigen | nein |
| `07A9` Spieltage/Termine | 0x54C9F (u. a. 0x7A9 = 1 im Saisonwechsel, Datei 0x5613E), 0x31934 (0x7B0), 0x30E7B/0x3139F (0x7AE/0x7AF), Start 0x437D5, Ansicht 0x4F78D | 0x50B45 (Abbruch ab 45), Aufstiegstermin 0x38652 | nein |
| `D0E6` Meister | Play-offs 0x31934 (Datei 0x31CBF), Start 0x437D5 | Statusbit 0x08 im Saisonende, 0x5667A | nein |

## Was zueinander passen muss

**Aufstiegsrunde (Wettbewerb 1, Gruppe g = 0 Bundesliga/2. Liga, 1 = 2. Liga/Oberliga).**

- Teilnehmer DGROUP 0xCFFC + 4 · g = Plätze 11, 12 der oberen und 1, 2 der unteren Liga laut `5CCB` am Tag der Auslosung
  (0x30427 liest DGROUP 0x5CD5–0x5CD8, Gruppe 1 entsprechend). Erste Paarungszeile 0xCF20 + 0x24 · g = Teilnehmer 0–3 in
  dieser Reihenfolge (FILE22, FILE44; der Zwangsabstieg überschreibt nur diese Zeile, `CFFC` bleibt – FILE17 Gruppe 1:
  `CFFC` 14, 15, 30, 27, Zeile 0 14, 23, 30, 27).
- Gespielte Termine T = min(2 · DGROUP 0x7B0 + (0xCCA3 > 0) + (0xCCA3 > 2), 6) (Datei 0x38652–0x38691, Auswahl 0x3AAA4). Einen
  eigenen Terminzähler gibt es nicht; nach dem Endspiel (0x7B0 = 2, 0xCCA3 = 5) ist T = 6.
- Punkte, Tore, Gegentore je Teilnehmer = Summe über die Termine 0 … T − 1 aus Paarung (`CED8`) und Toren (`CDB8`, Byte
  modulo 30): 2/1/0 Punkte; damit Punktsumme der Gruppe = 4 · T, Torsumme = Gegentorsumme. Nachbildung `gruppentabelle.py`:
  FILE22 und FILE44 beide Gruppen gleich (Punkte z. B. FILE22 Gruppe 0: 12, 4, 0, 8; Summe 24).
- Platzliste = Ergebnis der Tauschsortierung nach jedem Termin (Punkte, dann Tordifferenz, dann Tore; voller Gleichstand
  tauscht). Für einen Patch ohne vollen Gleichstand ist sie eindeutig (absteigend); mit vollem Gleichstand hängt sie vom Verlauf
  ab – ein Patcher sollte vollen Gleichstand abweisen.

**Ligen.**

- `5CCB` je Liga = Sortierung von 0x50B45 über die Tabellensätze `8257` (Fixpunkt; `ligatabelle.py`: FILE05, FILE13, FILE22,
  FILE44 bleiben stehen, umgekehrt eingesetzt kommt die gespeicherte Folge zurück). Im Modus „nur Weltkugel“ (FILE18) bleibt
  jede Folge stehen (Gegenprobe umgekehrt bleibt umgekehrt): dort sind die Sortierschlüssel gleich.
- Tabellensätze `8257` = Summe der Ergebnisse `A0CB` nach Spielplan (belegt im Import, `formate/spielstand-original.md`
  „Import im Spielkern“, FILE05/16A/24/31/36); 0x50283 bucht Tabellenfelder nur an Ligatagen (DGROUP 0xD0C2 = 7/8), die
  Aufstiegsrunde ändert `8257` also nicht.
- Die Teilnehmer der Aufstiegsrunde müssen zu `5CCB` passen (siehe oben); der 29. April prüft beim Zwangsabstieg nur, dass der
  Managerverein irgendwo in der Zeile seiner Liga steht (Datei 0x55F93–0x560D8).

**Weitere Pflichtblöcke.**

- `07A9`: nach der Saison 0x7A9–0x7AB = 45 (FILE22, FILE44); kleiner heißt, das Hauptmenü sortiert `5CCB` um. 0x7B0/0xCCA3
  bestimmen T (oben). Patchen nicht nötig, wenn die Vorlage nach dem 29. März liegt.
- `D0E6` und Statusbits 0x28: Deutscher Meister und Europacupsieger sind ausgenommen (Datei 0x55F59; Bit 0x08 setzt Datei 0x55C09,
  0x20 Datei 0x55DAD, beide in 0x54C9F vor Datei 0x55F00 – die Reihenfolge im Tagesablauf ist nicht nachgebildet). Ein Managerverein darf also nicht `D0E6` sein
  (FILE44: `D0E6` = Satz 9). Fall 9: Meister wird nicht zwangsweise versetzt, steigt aber über die Gruppentabelle ab (Status 0x0A).
- `A0CB`: am 29. April ungelesen; nur Anzeige und Remake-Import.
- Konto: Grenze −2.000.000 Drittel, „gleich“ zählt nicht (Fall 8). Bis zum 29. April laufen noch Tagesbuchungen; das Konto daher
  mit Abstand darunter setzen (z. B. −1.000.000 DM).

## Konstellationen am 29. April (Nachbildung `zwangsabstieg.py`)

Gruppe 0 synthetisch 12, 13 (2. Liga) auf Platz 1–2, 10, 11 (Bundesliga) auf 3–4; Gruppe 1 24, 25 (Oberliga) auf 1–2, 22, 23
(2. Liga) auf 3–4. „→“ = Liga in der neuen Saison (vor der Ligamischung).

| Fall | Manager unter der Grenze | Gruppe 0 nach Einschub | Ergebnis (nur Abweichungen vom Normalfall) |
|---|---|---|---|
| 0 | keiner | 12, 13, 10, 11 | 10, 11 → 2. Liga; 12, 13 → BL; 22, 23 → OL |
| 1 | BL Satz 5 (außerhalb) | 12, 13, 5, 11 | 5 → 2. Liga, 10 bleibt BL |
| 2 | BL Satz 11 (Platz 4, BL 10 auf 3) | 12, 13, **11, 11** | nur 13 steigt auf, 12 bleibt 2. Liga auf dem Platz von 13 (A665), 10 bleibt BL |
| **3 (A667)** | wie 2 und 2. Liga Satz 13 (Platz 2) | 12, 13, 11, 11 | **12 (Erster der Runde) → OL**, 11 (Zwangsabsteiger BL) → 2. Liga, 13 (Zwangsabsteiger 2. Liga) → BL, 22 bleibt 2. Liga |
| 4 | wie 2 und 2. Liga Satz 12 (Platz 1) | 12, 13, 11, 11 | 11 → **OL** (zwei Ligen), 12 bleibt 2. Liga, 13 → BL, 22 bleibt |
| 5 | BL Satz 4 und 5 (außerhalb) | 12, 13, 4, 5 | 4, 5 → 2. Liga, 10, 11 bleiben |
| 6 | BL Satz 11 (Platz 4) und Satz 5 | 12, 13, 11, 5 | 11, 5 → 2. Liga, 10 bleibt |
| 7 | BL Satz 10 (Platz 1) und Satz 5 | 10, 12, 13, 5 | ein Tausch: 5 ↔ 12; 10 bleibt BL (Zwangsabsteiger oben!) |
| 8 | wie 3, Konto genau −2.000.000 Drittel | unverändert | Normalfall (Grenze echt kleiner) |
| 9 | wie 3, BL-Manager Meister | 12, 13, 10, 11 | 11 → OL über 13s Platz (Status 0x0A) |
| 10 | Modus-1-Lage, Manager Satz 35 (OL-Letzter) | leer | Konto 1.200.000 Drittel, Status 0x03, Satz 35 bleibt OL (zwei Tausche) |
| 11 | wie 10, Satz 33 (Zehnter) | leer | keine Änderung |

Der Sonderfall der Restliste (A667, `SeasonFlow.Zwangsabstieg.cs` `ResolveDoubleMoves`) ist **Fall 3**: Der doppelte Platz
(Zwangsabsteiger auf Platz 3 und 4) entsteht schon mit **einem** Bundesliga-Manager auf Platz 4 hinter einem Bundesligisten auf
Platz 3; den Zweitligisten, der „nur den Platz wechselt“ und dann über Gruppe 1 in die Oberliga geht, gibt es nur, wenn ein
**Zweitliga-Manager** aus Platz 1–2 der Gruppe 0 zwangsweise in Gruppe 1 eingeschoben wird. Zwei Bundesliga-Manager (Fälle
5–7) erzeugen ihn nicht. Remake-Gegenprobe (Wegwerf-Harness über die öffentlichen statischen Funktionen, gleiche
Gruppen): Fälle 0–7, FILE17 und beide Prognosen unten mit denselben Ligen wie die Nachbildung.

## Vorlage A667 (Aufnahme `runde-22` Sitzung 5)

Kein vorhandener Stand hat Manager in zwei Ligen am Saisonende (alle Stände mit Datum April haben einen Manager; FILE09/65/71–76
mit zwei Managern liegen im September 1993). Eine eigene Saison mit zwei Ligen ist in DOSBox zu lang. Vorschlag:

1. **FILE44** (14.04.1994, KÖLNER EC Satz 2, Bundesliga, Start-Liga 0; laut Nutzer folgt als nächster Spieltag der 30. April,
   README Runde 13 Sitzung 4; T = 6,
   `CD94` Gruppe 0 = Satz 15, 16, 0, 3). Prüfsummen gültig, Gruppentabellen = Nachbildung, `5CCB` Fixpunkt.
2. Kachel „Neuer“, beliebiger Verein: Der neue Manager kommt bei Start-Liga Bundesliga auf einen **Zufallssatz** der Bundesliga
   (Datei 0x29C78–0x29CCC; ausgeschlossen Satz 0, der Europapokal-Teilnehmer Satz 8 und Kölns Satz 2 → 9 Kandidaten). Gebraucht
   wird **Satz 3** (Platz 4 der Gruppe 0, hinter Satz 0 auf Platz 3): wiederholen, bis der neue Verein in der Tabelle auf Platz
   12 der Bundesliga steht (im Mittel neun Versuche), dann sichern.
3. Start-Liga auf 2. Liga (DGROUP 0x11A = 1): in der Spielvorbereitung, falls dort einstellbar, sonst per Patch (neues Feld,
   Teil 2; 0x11A lesen nur Spielvorbereitung 0x28165 und Startkader 0x5AF50, `disasm.py --xref 0x11A 0x11A`). Dann „Neuer“ mit
   **Satz 16** der 2. Liga (Platz 2 der Gruppe 0): Er bleibt auf seinem Satz (Datei 0x29C3B–0x29C75: Start-Liga ≠ Bundesliga, Liga
   des Vereins = Start-Liga, Satz ≤ 35). Sichern.
4. Patch: Konto der Manager 1 und 2 auf −1.000.000 DM (`--manager 1 konto=-1000000`, `--manager 2 …`), Start-Liga zurück auf 0
   (Ordnung, ohne Wirkung). Kein Tabellenpatch.
5. Erwartet (Nachbildung, `--add 3:0:… --add 16:1:…`): Satz 15 (Erster der Aufstiegsrunde, 2. Liga) → **Oberliga**, Satz 3
   (Zwangsabsteiger BL) → 2. Liga, Satz 16 (Zwangsabsteiger 2. Liga) → Bundesliga, Satz 0 bleibt BL, Satz 17 bleibt 2. Liga,
   Satz 23 → Oberliga; Status 0x04 bei beiden. Variante mit Satz 15 statt 16 (Platz 1): Satz 3 → Oberliga (Fall 4).

Alternative ohne Zufall (Entscheidung Leitstelle/Nutzer): ein Patchfeld, das den Managerverein wie „Neuer“ auf einen anderen
Satz tauscht (0x36169 mit Schalter 0x80, `reverse-engineering.md` „Neuer Manager mitten in der Saison“) – größerer Eingriff, im
Plan nicht vorgesehen. Die geplanten Tabellenfelder (Teil 2) sind für A667 **nicht nötig**; ihre Regeln stehen oben, falls sie
trotzdem kommen.

## Vorlage A671 (Sitzung 7 Nr. 6 Teil 2)

- Im Modus 1 bekommt der Managersatz den Namen des Nationalteams und Feld +0x16 = 1 zweimal: FILE18 Satz 7 und Satz 54
  (FILE54: 0 und 53). Damit ist die Frage der Restliste („ob em.exe dort Vereinsfeld +0x16 setzt“) für den Ligasatz beantwortet:
  ja. Gruppentabellen bleiben im Modus 1 leer (FILE18: 0x7B0 = 0, 0xCCA3 = 0, `CFFC` Null), Gruppen 0/1 tauschen nichts
  (`--hok FILE18`); die Oberliga tauscht wie immer `5CCB` + 24 + 11, + 10 (FILE18: Satz 31, 32).
- Fall 10: Steht der Managersatz dort, setzt em.exe das Konto auf 400.000 DM und beide Pfeile; das Remake lässt es (A671).
  Dass Modus 1 am 29. April denselben Weg Datei 0x55F00–0x563AD nimmt, ist nicht nachgebildet: Datei 0x54ED9 springt im Modus 1 nur
  im Tagesteil nach 0x553FF; der Saisonwechsel FILE20 → FILE21 (Modus 1) hat stattgefunden.
- **Vorlage:** In allen Modus-1-Ständen steht der Managersatz in der Bundesliga (Start-Liga 0). Ein Bundesligasatz kann nicht in
  die Oberligazeile von `5CCB` (er stünde doppelt). Nötig ist ein neues Spiel „nur Weltkugel“ mit **Start-Liga Oberliga**, gleich
  nach dem Sprung (14.04.1994) gesichert – falls die Spielvorbereitung die Start-Liga im Modus 1 zulässt (nicht nachgebildet;
  erster Schritt der Aufnahme). Dann Patch der Oberligazeile von `5CCB`, so dass der Managersatz auf Platz 12 steht (im Modus 1
  Fixpunkt, vorher mit `ligatabelle.py` prüfen); FILE92 vor, FILE93 nach dem Wechsel. Lässt die Spielvorbereitung das nicht zu,
  kann der Fall im Original nicht eintreten (Einordnung dann in `abgleich-dosbox-22b`/Leitstelle).

## Restlücken

- Pflichtprobe im Original (DOSBox) für A667 und A671 steht aus; „Neuer“ am 14. April (nach Saisonende) ist nicht erprobt.
- Ob die Spielvorbereitung im Hinzufügen-Modus die Start-Liga ändern lässt und ob Modus 1 eine andere Start-Liga als Bundesliga
  erlaubt, ist nicht nachgebildet. Gelesen ist nur Datei 0x2890E–0x28920: eine Änderung wird zurückgenommen, wenn die Dauer
  „eine Saison“ ist und die Start-Liga nicht 0; eine Modusprüfung steht dort nicht.
- Die Ansichten der Aufstiegsrunde (0x31CD5) sind nicht gegen einen gepatchten Stand geprüft (nur nötig, wenn Tabellen gepatcht
  werden).
