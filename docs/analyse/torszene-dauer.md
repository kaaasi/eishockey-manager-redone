# Analyse em.exe: Darstellung 2 „Dauer-Eisfeld“ (Spur `torszene-dauer`, Teil 1, Runde 26)

Befund S02 / A472 ([abweichungen-v1.md](abweichungen-v1.md)). Alle Adressen sind **Dateioffsets** in `em.exe`, Werte im
Datensegment stehen als `DGROUP 0x…` (Schreibweise wie in [re-livespiel.md](re-livespiel.md)). Teil 1 ist nur Beleg, kein
Produktcode.

**Nachbildung:** `python3 tools/reverse-engineering/live/torszene_dauer.py [~/referenz/original/gamedata/em.exe]` lässt die
Originalbytes im Interpreter `tools/reverse-engineering/abweichungen/emu16.py` laufen (alles außerhalb des geprüften Codes
gestubbt und mitgeschrieben, `zusammenarbeit.md` §5 Punkt 7). Die Abschnitte A–G unten nennen den Teil der Ausgabe, der sie
belegt. Die Ausgabe enthält nur Adressen und Zahlen. Gelesen mit `disasm.py` (Ausgabe nur im Scratch): 0x36B5D, 0x37A62,
0x3901B, 0x3A88A, 0x53C57, 0x567C8, 0x544C0 (Ausschnitt 0x54BF7–0x54C9E), 0x38B8F/0x38D96 (Kopf).

**Schalter:** `DGROUP 0x5A56` = Live-Bildschirm an diesem Spieltag (gesetzt in der Abrechnung Datei 0x54C63, wenn ein Verein
zum Zusehen markiert ist, Vereinsfeld 0x34 Bit 7; neu gesetzt in 0x32AA3, Datei 0x32AAD/0x32DD8), `DGROUP 0x5A55` =
Dauer-Eisfeld läuft, `DGROUP 0x226` = Darstellung 0/1/2 (Spielstand-Option 9), `DGROUP 0x386` = Zahl der Manager.

## 1. Ergebnis in Kürze

Bei Darstellung 2 **ersetzt** das Eisfeld den Live-Bildschirm für den ganzen Spieltag: `0x5A56` wird 0, `0x5A55` 1. Damit
entfällt alles, was an `0x5A56` hängt (Live-Bildschirm, Anzeigetafel, Reporter, Spieluhr, Zwinkern, Minutenpausen,
Leertaste/Optionen, Klick nach dem Spiel, Eingriffsmenü, Paarungsseiten), und die Torszenen laufen in Modus 2 auf dem
einmal geladenen Eisfeld. Abweichung zum Remake bestätigt (A472, **ändern**); zusätzlich belegt: das Penaltyschießen zeigt
das Eisfeld schon bei Darstellung 1 (§6), und mit Dauer-Eisfeld gibt es keinen Eingriff nach einer Verletzung (§5).

## 2. Spieltag 0x36B5D (Ausgabe A)

Nur im Minutenweg (Datei 0x37088 ff.: ein Manager mit `DGROUP 0xA729 + m` ≠ 0 oder eine Spielstand-Option des
Wettbewerbs `0x21E…0x229`); der schnelle Weg Datei 0x3731F kennt kein Eisfeld.

1. **Aufbau** (Datei 0x3710B–0x37171): `[0x5A56]` ≠ 0 und `[0x226]` = 2 → `0x5A56` = 0, `0x5A55` = 1, Hintergrund
   `110h:7Ah`, Palette an `38h:DDEh(…, 1)`, dann Abspieler `0x567C8` **Modus 1** mit Heim `DGROUP 0xE2AA`, Gast `0xE2A9`,
   Toren 0:0, Minute 0, Hallenstufe `0xE2A8`, Zuschauertext leer (DGROUP 0x4EA2). Rückgabe 0 (zu wenig Speicher, §4) →
   `0x5A56` = 1, `0x5A55` = 0: normaler Live-Spieltag.
   - `0xE2AA`/`0xE2A9` schreibt die Abrechnung 0x544C0 für **jede** Paarung mit zugesehenem Verein (Datei 0x54C07–0x54C2E):
     die Kopfzeile zeigt also die zuletzt abgerechnete solche Paarung. `0xE2A8` = Kennbyte `DGROUP 0x5D99 + 6 · m` des
     Managers m (Vereinsfeld 0x16) des Heimvereins, ohne Manager 2 (Datei 0x54C31–0x54C5E).
2. **Drittel** 1–20, 21–40, 41–60 (Datei 0x37176–0x371E8): `0x37A62(…, Anzeige = [0x5A56])` – mit Eisfeld also Anzeige 0
   (§3). Danach `0x5A56` = 0 (Datei 0x371EB).
3. **Verlängerung** (K.-o., Datei 0x371F0–0x37295): 0x32AA3(Schalter 0) setzt `0x5A56` neu (zugesehenes Unentschieden).
   `[0x5A56]` ≠ 0 und `[0x226]` = 2 → `0x5A56` = 0, `0x5A55` = 1 **ohne** neuen Aufbau (Datei 0x37220–0x37233): das Eis
   des Spiels bleibt. 61–65 und 66–70 mit Anzeige `[0x5A56]`. 0x32AA3(Schalter 1) mit Penaltyschießen (§6); Rückgabe ≠ 0
   → Ausblenden, Seitenwähler `0x3A88A([0xD0C2], 0, 70)` (§7), Palette an.
4. **Abbau** (Datei 0x372C2–0x372F9): `[0x5A55]` ≠ 0 → `0x541C` = 0, Abspieler **Modus 3**, `0x541C` = 1, `0x5A55` = 0.
   Danach `kb\38` und Hilfsfläche frei, Palette aus (Datei 0x377E4), Darstellung und Geschwindigkeit aus `0xE19C`/`0xE19B`
   zurück (Datei 0x37833–0x3783E).

## 3. Minutenschleife 0x37A62 mit Dauer-Eisfeld (Ausgabe B, B2)

Mit Anzeige-Argument 0 und `[0x5A55]` = 1 je Drittel und Minute (Datei 0x37B35–0x37D8C, 0x3855F–0x38604):

- Drittelanfang: kein Live-Bildschirm, kein „Spiele laufen“, keine Spieluhr (Datei 0x37B77, 0x37C32); der Seitenwechsel
  `0xE19D`/`0xE06A` läuft nur mit Anzeige (hat außerhalb dieser Stelle keinen Leser).
- Je Minute: **keine Pause**, kein Zwinkern, keine Leertaste/Optionen (alles nur mit Anzeige, Datei 0x37C6B, 0x37D02);
  `0xE19E` = Minute, dann Strahlrücklauf `48h:30B5h` und Minutenzeile `0x570E8(Minute)` (Datei 0x37CD5–0x37CEA).
  Ausgabe B: 20 Minuten → 20 Minutenzeilen, 0 Pausen; mit Live-Anzeige 20 Pausen, kein Minutenzeilen-Aufruf.
- Anzeigetafel 0x52BD6 am Minutenende kehrt sofort zurück (Datei 0x52BE3–0x52BF3, Ausgabe F).
- Nach Minute 60/70: kein Klick, kein Ausblenden (nur mit Anzeige, Datei 0x3855F). Ligaseiten `0x3A88A(7, Liga, Minute)`
  nur ohne Dauer-Eisfeld (Datei 0x385E7); die Tabellenrechnung 0x50B45/0x51049/0x052CF läuft in beiden Fällen (Ausgabe B2).
- Die Rechnung selbst (Chancen, Strafen, Verletzungen, Torszenen-Aufrufe aus 0xA484) ist dieselbe; die Zeit des Spieltags
  bestimmen also nur die Torszenen (Pause 30 vorher, Szene, Pause 20; §5).

## 4. Abspieler 0x567C8 (Ausgabe C, C2)

Argumente `(Heim, Gast, ToreH, ToreG, Feldspieler H/G, Farbe H/G, Seite, Art, Minute, Wettbewerb, Flag, Modus,
Hallenstufe, Text)`, Modus = `[bp+0x20]` (14. Argument).

- **Modus 1** (Aufbau): Speicherprüfung wie Modus 0 (frei < 0x412F8 → `[0x226]` = 0, `[0xE19C]` = 0, Live-Bildschirm,
  Meldung `120h:4Dh`, Rückgabe = 13. Argument = 0; Datei 0x56816–0x56871). Hallenstufe: `[0x386]` > 1 und Modus ≠ 0 →
  Stufe 2 (Datei 0x568A9–0x568B6). 13 Bilder `kb\231…247` laden (i = 5: `kb\236`, Stufe 1…4 → `kb\(241 + Stufe)`; Ausgabe C:
  Stufe 1 → `kb\242`, mit zwei Managern `kb\243`, Stufe 0 → `kb\236`). Dann Palette aus, Bildpalette, Kopfzeile
  `0x56EDD(0, …)` (Namen, Tore, Zuschauertext, Minutenzeile), Eisfläche `48h:47F0h` auf den Schirm, Palette an,
  **Rückgabe 1** (Datei 0x56ABC–0x56B4A). Keine Szene.
- **Modus 2** (Szene auf dem Eisfeld, Datei 0x56804 → 0x56B4D): nichts laden; Szenenwahl 0x575C3; Kopfzeile
  `0x56EDD(1, …)` – also mit Namen, Zuschauertext und Minutenzeile **dieser** Szene, aber ohne Palette (Datei
  0x56CB1–0x56CC1); Bildfolge; bei Tor Torkästen `0x56EDD(2, …)`; **keine Pause 6** (nur Modus 0, Datei 0x56E14); nur der
  Szenenpuffer wird frei; Rückgabe 0 (Datei 0x56E37–0x56E3F). Das Eisfeld bleibt mit der Kopfzeile der letzten Szene stehen.
- **Modus 3** (Abbau, Datei 0x56E42–0x56ECF): alle geladenen Bilder frei (Ausgabe C: 13), `DGROUP 0x49A9` = 1, Palette aus, `110h:7Ah` statt
  Live-Bildschirm, Palette an; Rückgabe 0.
- Modus 0 (Einzelszene, Darstellung 1) zum Vergleich: laden, Szene, Pause 6, Bilder frei, Live-Bildschirm neu.

## 5. Torszene 0x53C57 mit Dauer-Eisfeld (Ausgabe E, F)

Aufrufe wie sonst (jeder Abschluss der Managerpaarung, Datei 0xAB90; Verletzung aus 0x38B8F). Mit `0x5A56` = 0:

- keine Reihensuche und **keine Drittelzähler** `0xA839`/`0xA83F` (Datei 0x53C95 → 0x53DB1) und **keine Prüfung**
  `120h:66h` (weniger als 4 fitte Spieler → Rückkehr 0, Datei 0x53D73–0x53D89) – die Szene läuft also auch dann;
- Szene mit `[0x226]` ≠ 0 und Modus `2 · [0x5A55]` = 2 (Datei 0x53F75–0x53FD3); Pause 30 vorher und 20 nachher wie sonst;
  Wiederholung `r`/`w` bis dreimal unverändert;
- Reporter 0x52520 kehrt sofort zurück (Datei 0x5252A), Spielstand 0x51FEA und Zwinkern 0x522EC ebenso (Ausgabe F);
- Verletzung (Art 4): Verletzung `18h:199Ah` wie sonst, aber statt Reporter und Eingriff `0x5434C` die **automatische
  Aufstellung** `108h:4Dh` (Datei 0x54134–0x5419D); keine Anzeigetafel (Datei 0x541A2);
- Eingriffsmenü 0x38D96 kehrt sofort zurück (Datei 0x38DA2–0x38DAA): kein Eingriff per Klick während des Spieltags.

## 6. Penaltyschießen 0x3901B (Ausgabe D)

Anzeigeweg nur, wenn ein Verein der Paarung zugesehen wird (Datei 0x390B8–0x390D6), sonst stiller Weg Datei 0x390D9 ff.

1. Erster Zug `random(0, 1)`; läuft das Eis der Verlängerung (`[0x5A55]` ≠ 0): **Abbau Modus 3**, `0x5A55` = 0
   (Datei 0x39036–0x3906D).
2. Penalty-Bildschirm wie bisher, Klick `38h:118h` (Datei 0x3917F–0x39373).
3. `[0x226]` ≠ 0 – **Darstellung 1 und 2** – → `0x5A55` = 1, `0x5A56` = 0, `38h:10Eh(0xFF)`, Palette aus, eigener Aufbau
   **Modus 1** mit den Vereinen der Paarung, Toren 0:0, **Minute 70**, **Hallenstufe 2**, Text leer (Datei 0x3937F–0x393D8).
   Scheitert er: `0x5A55` = 0, `0x5A56` = 1, Live-Bildschirm. `[0x226]` = 0: Live-Bildschirm (Datei 0x3941A).
4. Je Schuss Torszene 0x53C57 (mit Eisfeld Modus 2), ohne Eisfeld bei Tor Spielstand 0x51FEA (Datei 0x394D3), Pause 80.
5. Ende: `[0x5A55]` ≠ 0 und `[0x226]` ≠ 0 → Abbau Modus 3, `0x5A55` = 0 (Datei 0x3954A–0x39588); Pause 50, Ergebnisbild,
   Klick. `0x5A56` bleibt dann 0 (nur der Rückfall setzt 1).

Folge für den Spieltag: nach einem Penaltyschießen ist `0x5A55` = 0, der Abbau in Datei 0x372C2 entfällt, die Pokalseite
`0x3A88A` danach erscheint normal.

## 7. Seitenwähler 0x3A88A (Ausgabe G)

Mit `[0x5A55]` ≠ 0 kein Ausblenden (Datei 0x3A9D8); Seiten außer 1, 2, 9 kehren sofort zurück (Datei 0x3A9E5–0x3A9FE),
Seite 1 und 2 nach ihrem Datenteil vor der Anzeige (Datei 0x3AA86, 0x3AAFB), Seite 9 nach 0x2FC2F (Datei 0x3AC42; im
Spieltag nicht mit Eisfeld gerufen). Die Gruppentabellen 0x30852 der Seiten 3–5 bei Minute 60 laufen **vor** der Prüfung
(Datei 0x3A92A–0x3A987) – der Spielstand ändert sich mit und ohne Eisfeld gleich. Während eines Dauer-Spieltags erscheint
also keine Paarungsseite.

## 8. Randfälle des Originals

- **Optionen mitten im Spiel auf 2** (Leertaste nur mit Live-Bildschirm): die Verlängerung setzt `0x5A55` = 1 ohne Aufbau
  (Ausgabe A, letzter Fall) – Szenen in Modus 2 und Abbau Modus 3 mit nicht geladenen Bildern. Vorschlag Teil 2:
  `[APPROX]` Aufbau nachholen statt Fehlerbild.
- **Szenenwahl −1** (50 Fehlversuche, nur bei fehlenden `tore\`-Dateien): Modus 2 gibt die Bilder frei und baut den
  Live-Bildschirm auf (Datei 0x56BC7 → 0x56E42), `0x5A55` bleibt 1. Mit vollständigen Daten nicht erreichbar.
- **Zu wenig Speicher** beim Aufbau: Darstellung 0 bis zum Ende des Spieltags und dauerhaft (`0xE19C` = 0 wird zurück
  geschrieben). Im Remake ohne Speichergrenze nicht nötig.

## 9. Wechselwirkung mit `live-verschraenkt` (S01)

Die Minutenschleife rechnet mit Eisfeld dieselben verschränkten Paarungen (gleiche Rechnung, Anzeige-Argument 0). Die
Reihenfolge der Torszenen ist die Reihenfolge der Abschlüsse über alle Managerpaarungen je Minute; jede Szene zeichnet ihre
eigene Kopfzeile. Was S01/S04/S05 am Live-Bildschirm ändern (Paarungsseiten, Reporter, Pausen, Leertaste), gilt mit
Eisfeld gerade **nicht**. Teil 2 setzt auf dem Kern von `live-verschraenkt` auf und braucht von dort: die Liste der
Abschlüsse je Minute in Rechenreihenfolge, die Kennzeichnung „Live-Anzeige an“ (`0x5A56`) getrennt von „Dauer-Eisfeld“
(`0x5A55`) und den Schalter für Eingriff/automatische Aufstellung nach einer Verletzung.

## 10. Vorgaben für Teil 2 (Umsetzung A472)

| Nr. | Vorgabe | Fundstelle |
|---|---|---|
| T1 | Darstellung 2 mit zugesehenem Verein: statt Live-Bildschirm einmal Eisfeld aufbauen (Kopfzeile der zuletzt abgerechneten zugesehenen Paarung, 0:0, Minute 0, Hallenstufe Kennbyte des Heimmanagers bzw. 2, bei mehreren Managern 2) | Datei 0x3710B–0x37171, 0x54C07–0x54C5E, 0x568A9 |
| T2 | Je Minute ohne Pause nur die Minutenzeile „NN.MINUTE“ neu; keine Spieluhr, Anzeigetafel, Reporter, Zwinkern, Leertaste | Datei 0x37C6B–0x37CEA, 0x52BEB, 0x5252A, 0x522EF |
| T3 | Torszenen auf dem stehenden Eis in Modus 2: Kopfzeile der Szene (Modus 1), kein Neuladen, keine Pause 6, danach kein Live-Bildschirm | Datei 0x56B4D–0x56E3F |
| T4 | Torszene ohne Drittelzähler und ohne Fit-Prüfung; nach Verletzung automatische Aufstellung statt Eingriff; kein Eingriffsmenü | Datei 0x53C95, 0x54134–0x5419D, 0x38DA2 |
| T5 | Verlängerung auf demselben Eis; nach Minute 60/70 kein Klick, keine Liga- oder Pokalseiten | Datei 0x37220, 0x3855F, 0x385E7, 0x3A9E5 |
| T6 | Abbau am Spieltagsende ohne Live-Bildschirm (Ausblenden); danach Ablauf wie bisher | Datei 0x372C2, 0x56E42–0x56ECF |
| T7 | Penaltyschießen: Eis der Verlängerung abbauen, Penalty-Bildschirm und Klick, bei Darstellung 1 **und** 2 eigenes Eis (Minute 70, Stufe 2), Schüsse darauf, Abbau | Datei 0x39036–0x39588 |
| T8 | Darstellung 0/1 sonst unverändert | – |

T7 ändert auch Darstellung 1 (Penaltyschießen); falls das außerhalb von A472 liegen soll, entscheidet die Leitstelle
(Überschneidung mit S04 „Penalty-Anzeige“ in `live-verschraenkt`). Prüfung Teil 2: Bildschirmfoto-Ablauf Darstellung 2 über
einen Spieltag mit Tor und Penaltyschießen; ein Videobild mit Darstellung 2 ist im Index `docs/referenz-videos.md` nicht
verzeichnet – Vergleich nur gegen die Einzelszene (gleiches Bild) und gegen diese Lesart; DOSBox-Probe nach R26-D3 = B.
