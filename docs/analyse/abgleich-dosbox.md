# Auswertung DOSBox-Aufnahme `runde-17` (Spur `abgleich-dosbox`, Runde 18)

Grundlage: Spielstände und Bilder im Referenz-Repo (`original/spielstaende/README.md`, Abschnitte „Runde 17, Sitzung 1–3“),
Werkzeuge `emtool abgleich`, `docs/analyse/belastung-staerke/*.sh|py` (Skripte und `trace.patch` wie dort, `trace.patch` nur
lokal) und `abgleich-dosbox/lampen.py`. Keine Bilder in diesem Repo.

## 1. #730 Belastung – FILE50/FILE51 (Teil 1)

### 1.1 Paare FILE02→FILE50 und FILE02→FILE51

`emtool abgleich FILE02 FILE50|FILE51 --seed s` (s = 1993, 4711, 1, 2, 3, 77): **keine Abweichung**, auch nicht in der Belastung
(`--alle`: alle 25 Kölner innerhalb ±7, z. B. HOCK Original 116/117, Remake 117; SIKORA 120/123, Remake 120). Beide Paare stehen
jetzt mit leerer Erwartungsliste in `AbgleichOriginalPaareTests` (damit auch in `Pairs_ManagerClubStrainMean_LikeOriginal`).
Der Torwartwechsel in FILE51 (Minute 26 CISSEWSKI → HEISS) verschiebt die Belastung nur um 2 (CISSEWSKI 96 → 94, HEISS 91 → 93).

### 1.2 Reihen je Minute (Bilder `file50_02…85`)

Lampen der Anzeigetafel 0x52BD6 (`re-livespiel.md` §4: Lampe k = Reihe auf dem Eis bei (x0 + 8 + 8t | 15 + 7k − 1)), Minute von
der Spieluhr unten links abgelesen; `lampen.py` liest die Lampen, die Folgen stehen als `[ORIGINAL]`-Konstanten in
`AbgleichDosboxLinesTests` (fehlende Minuten 17, 23, 37, 43, 60 vom Vorgänger). Kennzahlen gegen 200 Saaten des Remakes (erstes
Spiel aus FILE02 über `OriginalMinutes.PlayMinute`, Reihen nach dem Reihenwechsel):

| Kennzahl | Original | Remake Bereich | Median | Saaten ≤ / ≥ Original |
|---|---:|---|---:|---|
| Köln Abwehr R1/R2/R3 (Eisminuten) | 19/22/19 | 15–24 / 17–26 / 12–23 | 20/21/20 | 82/188, 170/53, 86/140 |
| Köln Sturm R1/R2/R3 | 18/23/19 | 16–27 / 17–25 / 12–23 | 20/21/20 | 7/198, 188/21, 93/135 |
| Preußen Abwehr R1/R2/R3 | 22/20/18 | 16–30 / 16–29 / 9–23 | 22/22/17 | 121/101, 59/170, 141/80 |
| Preußen Sturm R1/R2/R3 | 20/24/16 | 16–26 / 16–29 / 9–24 | 21/22/17 | 71/149, 175/48, 91/133 |
| Minuten Abwehr ≠ Sturm (Köln/Preußen) | 16/16 | 0–48 | 25/25 | 56/151, 67/142 |
| Reihenwechsel (Köln/Preußen) | 18/21 | 13–26 | 15/16 | 171/38, 192/15 |

**Ergebnis:** Ohne Mausklick wechselt das Original wie das Remake etwa alle vier Minuten (Reihenfolge nicht starr 0-1-2), alle
drei Reihen spielen gleich lang; Köln trennt Abwehr und Sturm erst im letzten Drittel, Preußen auch vorher kurz. Jede Kennzahl liegt in der
Verteilung des Remakes (Test: mindestens 1 % der Saaten auf jeder Seite). Gegenprobe: Schichten erst nach 6 statt 4 Minuten
(`OnIceMinutes > 5` in `ChangeLines`) → rot (Wechsel Köln 18 / Preußen 21 gegen Remake 9–18).

### 1.3 Folgerung für #730

- `OriginalMinutes.ChangeLines` ist für den ersten Spieltag belegt; die Reihenwahl ist nicht die Ursache.
- **Mausweg 0x38D96:** Laut Nutzer (FILE51) wirken Klicks in die Managerzeile nicht, nur das Vereinslogo öffnet die Aufstellung.
  Damit scheidet auch der Mausweg als Ursache für FILE02→03 aus (dort ohne Eingriff gespielt). Der „Knopf unten links“, dessen
  Aussehen beim Klick wechselt, ist die Lampe „Schnelldurchlauf“ (`re-livespiel.md` §10 Schritt 1, x 3…20, y 226…238). Dass
  Klicks in die Felder der Managerzeile ohne Wirkung blieben, ist ein Hinweis für `livespiel-kern` A3 (Eingriffsmenü; ob
  `[0x5A16]` gesetzt war, belegt die Aufnahme nicht) – Leitstelle verknüpft.
- **Quasi-Paar FILE50→FILE03** (zwei verschiedene Durchläufe aus FILE02; FILE50 und FILE51 liegen in der Belastung höchstens 5
  auseinander, FILE50 taugt also als Stand nach Spieltag 1 des FILE03-Laufs): Belastungszuwachs je Reihe Spieltag 2–3
  (`pair.py FILE50 FILE03`, Saaten 1993, 4711, 1, 2, 3, 77):

  | Reihe | Original Σ (gekippt) | Remake Σ Mittel (gekippt) |
  |---|---|---|
  | Abwehr 1 CISSEWSKI, SCHIFFL, MENDE | +13 (1) | +20,8 (0,67) |
  | Abwehr 2 KÜHN, UVIRA | +15 | +8,8 |
  | Abwehr 3 KWASIGROCH, NÖTZEL | +13 | +14,0 |
  | Sturm 1 DOROCHIN, LIEBSCH, CHAIDAROV | +32 | +16,8 |
  | Sturm 2 HOCK, SIKORA, LUPZIG | +5 (2: HOCK, SIKORA) | +13,7 (0) |
  | Sturm 3 BRANDL, STEFAN, HANEMANN | −3 | +10,8 (0,33) |

  Im Original wachsen Sturm 1/2 an den Spieltagen 2/3 stark, Sturm 3 gar nicht; am Spieltag 1 (FILE50) war die Eiszeit
  gleich verteilt. Die Ursache liegt also in den Spieltagen 2/3 (Auswärtsspiel in Ratingen, Heimspiel gegen Kaufbeuren, dazwischen
  Training) und ist mit `runde-17` nicht zu klären.
- **#730 bleibt `[APPROX]`** (Marker an `ChangeLines` neu gefasst), Pflichteinträge unverändert. **Vorschlag DOSBox:** FILE50
  laden, Spieltag 2 und 3 im Live-Spiel ohne Klick mit Bild je Minute, nach jedem Spieltag im Hauptmenü sichern (zwei Stände);
  damit wird FILE50→… ein echtes Paar mit Reihen je Minute.
- **A093** (Technik −10 außerhalb der eigenen Reihe, +0x02): In FILE50/FILE51 spielt kein Spieler außerhalb seiner Reihe (keine
  Umstellung, Klicks ohne Wirkung) – mit `runde-17` nicht belegbar, bleibt `[APPROX]` (Inventur P2).

## 2. #773 Strafminuten – FILE52→FILE53 (Teil 2)

### 2.1 Einsatz nach dem ersten Spieltag

Vereinsfeld 0x26 der Vereinssätze 0–51 (`emtool hok dump … far:9657`, `belastung-staerke/clubs.py`): FILE52 alle 0; FILE53
40 × 20, 60 × 14, 0 × 16 (kein Spiel), Gegner des Managers EC HEDOS MÜNCHEN 0x26 = 60, Grundwert 0x2D = 40, Angriff 4. Das
Remake (`Tactics.MatchEffort` nach `HokCompare.PlayTo`) trifft die Verteilung bei allen Saaten genau; den Gegner setzt 0x939A je
Minute nach dem Spielstand (Remake 55 oder 60). **Der Einsatz ist nicht die Ursache** (`AbgleichDosboxPenaltyTests`).

### 2.2 Strafminuten

| Paar (Spieltage) | Original | Remake (Saaten 1–20) | Mittel | ungerade Summen je Verein (5-Minuten-Strafe) Original / Remake-Mittel |
|---|---:|---|---:|---|
| FILE52→53 (1., neues Spiel) | 456 | 367–483 | 432 | 12 / 8,4 (4–13) |
| FILE02→50 (1.) | 500 | 382–464 | 428 | 12 / 9,6 (5–15) |
| FILE02→51 (1.) | 411 | 382–464 | 428 | 13 / 9,6 |
| FILE02→03 (1.–3.) | 1447 | 1218–1377 | 1302 | – |

Drei unabhängige erste Spieltage liegen alle im oberen Fünftel der ungeraden Summen – das Original verteilt mehr
5-Minuten-Strafen (Ligazähler + Code & 7: 2 bzw. 5).

### 2.3 Ursache: Zusammenhang aufeinanderfolgender lrandom-Züge

`lrandom` (Datei 0xBFD5, ndisasm): x = (16 · x + 167) mod 4.000.000 (0x3D0900), Ergebnis a + x · (b − a + 1) / 4.000.000. Weil
der Faktor 16 gerade ist, stehen die unteren 8 Bit nach zwei Schritten fest; modulo 5^6 ist der Schritt umkehrbar → Zyklus von
**15.625 Zuständen**, und x' ≈ 16 · x. Der Strafwurf Datei 0x358B7–0x3592D zieht d1 = lrandom(0, 12), nur bei d1 ≠ 0 d2 =
lrandom(0, 110 − Einsatz), dann (Platz 0 frei, Computerverein ohne Zug dazwischen) d3 = lrandom(0, 15). Über den ganzen Zyklus:

| Einsatz | Strafe je Wurf (Zyklus / unabhängig) | davon 5 Minuten (Zyklus / unabhängig) | Weg d1 = 0 | Weg d2 = 0 |
|---|---|---|---|---|
| 40 | 8,92 % / 8,99 % | **12,55 %** / 6,25 % | 122 von 1.202 (10,1 %) | 53 von 192 (27,6 %) |
| 60 | 9,41 % / 9,50 % | **11,90 %** / 6,25 % | 122 von 1.202 | 53 von 268 (19,8 %) |

Die Strafwahrscheinlichkeit stimmt, der Anteil der 5-Minuten-Strafen ist im Original doppelt so hoch: Minuten je Strafe 2,38
statt 2,19 (+8,6 %), passend zum Abstand FILE02→03 (+11 %) und zu den ungeraden Summen. Die frühere Probe „lrandom statt
`Random`“ (§8 in `belastung-staerke.md`) ist damit überholt.

### 2.4 Fix

`OriginalMatch.PenaltyMinutes` zieht d1/d2 wie bisher; der 5-Minuten-Zug bleibt **ein** Zug aus `Random` (gleiche Zugzahl), trifft
aber mit der Häufigkeit des Zyklus je Weg (`LRandomCycle.FiveMinuteOdds`, neue Datei `MatchEngine.LRandom.cs`; `[APPROX]` nur die
Lage im Zyklus, gleichverteilt). Ergebnis (Saaten wie oben):

| Paar | Original | vorher | nachher |
|---|---:|---|---|
| FILE02→03 (Saaten 1–12) | 1447 | 1232–1368, Mittel 1301 | 1336–1458, Mittel 1394 |
| FILE52→53 (1–20) | 456 | 367–483, Mittel 432 | 411–542, Mittel 473 |
| FILE05→24 (`AbgleichRestPenaltyTests`, 4 Saaten, ±8 %) | +441 | Mittel ≈ 445 | Mittel 472,5 |

Regressionstests `PenaltyMinutes_FiveMinuteShare_LikeLrandomCycle` (rot vorher: 6,25 %) und
`File02To03_LeaguePenaltyMinutes_WithinRemakeSpread` (rot vorher: 1447 über allen Saaten). Fremde Tests nach R15-E2 = C angepasst
(Liste mit Rückschaltprobe im PR). Nebenwirkung für #730: REZNICEK (FILE52→53) kippt jetzt bei Saat 2 wie im Original,
KUHNEKATH (FILE13→14) weicht nur noch bei 3 von 6 Saaten ab.

**Weitere aufeinanderfolgende lrandom-Züge** (Chancen 0x9F5C, Abschluss, Strafe/Kette) waren hier nicht geprüft; dieselbe Kopplung
kann dort wirken. Vorschlag: eigene Analyse (`LRandomCycle` liefert den Zyklus).

### 2.5 Weitere aufeinanderfolgende lrandom-Züge (#829, Spur `pruefung-karte` Teil 2, Runde 18)

Geprüft mit `LRandomCycle` (Lage beim ersten Zug gleichverteilt über die 15.625 Zustände, wie §2.4); Züge zwischen den
Aufrufen nach `ndisasm`/`disasm.py` (nur `0x99F:0x7E5` bzw. `0x38:0x7E5` → 0xBFD5 zählt, `random` ist ein anderer Generator).

| Stelle | Züge ohne lrandom dazwischen | Zyklus gegen unabhängig |
|---|---|---|
| Chancen 0x9F5C (Datei 0xA387–0xA3EC) | je Punkt von tH, dann von tG lrandom(1, 20) = 1 | Heim-/Gastchance für tH = tG = 1 … 10 höchstens −1,1 % (relativ); beide Chancen in einer Minute ±2 % |
| Abschluss 0xA484 (Datei 0xA58B–0xA82D) | Schwäche lrandom(550, 850), Tür lrandom(0, 2w + 4) bzw. lrandom(0, 3 · WM + 2w + 1), dann lrandom(0, d/4 + w + 2) bzw. lrandom(0, −d/4 + 1), lrandom(0, 6 − w) | Heim: d < 0 −3 … −5 %, d = 0 +1 … +3 %, d > 0 bis +1,5 %; Gast: −3 … +0,3 % (Summe der Stärken 600/700/800, d = −20 … 20) |
| Kette 0x35818 (Datei 0x35921, 0x35A0D, Aufruf 0x35C8B) | d3 = lrandom(0, 15), Kettenzug lrandom(0, 3) = 0, beim Gegner wieder d1/d2 | Kettenzug = 0: 25,8 % statt 25 %; danach Strafe 11,4 % statt 9,0 % (Einsatz 40: 41 von 360) → je Strafe 2,9 statt 2,25 % Kettenstrafen, insgesamt etwa +0,7 % Strafen |

**Folgerung:** Keine der drei Stellen verschiebt eine Häufigkeit annähernd so stark wie der 5-Minuten-Zug (doppelt). Die
Chancen sind praktisch unabhängig. Beim Abschluss wechselt das Vorzeichen mit dem Stärkeabstand (höchstens 5 % je Abschluss; die Wirkung auf
Tore je Spiel ist nicht gemessen, `[APPROX]` bleibt). Die Kette erhöht die Strafminuten im Original um etwa 0,6–0,7 %. Übernähme das Remake sie, läge
FILE05→24 bei etwa +8,3 % statt +7,6 % (Grenze 8 %); FILE02→03 (−3,7 %) und FILE52→53 (+3,7 %) blieben in der Streuung. Das
Remake zieht dort weiter unabhängig (`[APPROX]`, wie §2.4 nur der 5-Minuten-Zug nach dem Zyklus). Vor einer Lockerung der
Grenze in `AbgleichRestPenaltyTests` ist damit keine weitere Kopplung nachzuziehen. Tests: `PruefungKarteRunde18Tests`
(`Chain_PenaltyAfterChainDraw_OverCycle`, `ChanceLoop_CouplingBelowOnePointTwoPercent`).

## 3. Export/Import (Teil 3)

### 3.1 #804 Vereinsfelder 0x26/0x2C

`OUT=… docs/analyse/belastung-staerke/dump.sh FILE03 FILE16A FILE44 FILE53 EXPORT2D && python3 …/clubs.py …` (Computervereine
0–51, Tripel 0x26/0x2D/Angriff 0x2C Bits 4–6):

| Stand | Verteilung |
|---|---|
| FILE03 | 40/40/0 × 23, 60/60/0 × 9, 0 × 16, 40/40/5, 40/40/4, 55/40/6 (Gegner der Managerspiele) |
| FILE16A | 40/40/0 × 23, 60/60/0 × 12, 0 × 16 |
| FILE44 | 40/40/0 × 17, 60/60/4 × 7, 60/60/0 × 7, 40/40/4 × 2, 40/40/5 × 2, 0 × 16 |
| FILE53 | 40/40/0 × 20, 60/60/0 × 14, 60/40/4 (Gegner), 0 × 16 |
| EXPORT2D (Remake, 22 Spieltage) | 0/0/0 × 51 |

Vorher las der Import die Felder nicht (Werte aus dem Nachspielen, je Saat verschieden: FILE03 17–19, FILE16A 33, FILE44 29
Abweichungen) und der Export schrieb 0. **Fix:** `ApplyTeamValues` übernimmt +0x26 und +0x2C Bits 4–6 der Vereine mit Kader
(`Tactics.RecordMatchFields`), `WriteClubRecords` schreibt sie zurück, +0x2D = +0x26 `[APPROX]` (das Remake führt den Grundwert
nicht; im Original nur beim Gegner einer Managerpaarung verschieden). Die übrigen Bits von 0x2C (Modus, Bit 3, Favorit) setzt
0x8365/0x84D7 vor jedem Spiel neu und bleiben 0. Keine Schema-Änderung (die Felder führt der Spielstand seit Schema 48,
`SaveGame.Migration.AbgleichRest.cs`), daher keine Migration. Tests: `AbgleichDosboxExportTests` (Import, Export, Mehrsaisonprobe
Remake → HOK → Remake über vier Saisonwechsel, passiv und aktiv; alle vorher rot).

### 3.2 Exportprobe EXPORT1D/EXPORT2D/EXPORT2C (R758-01)

EXPORT1D/2D sind am `main` 6805ebf erzeugt (nach `97c372a`), EXPORT2C am Fixstand `c09cd45`. Alle drei lädt das Original ohne
Fehler (Bilder `export1d_1…3`, `export2c_1…3`, `export2d_1…3`). Abgleich Bild ↔ `emtool hok dump`:

- Hauptmenü: Datum, Spieltag, Konto (500.000 / 331.691 / 339.011 DM), Kopf mit Sonnenbrille (Kopf 6) wie im Dump.
- Mannschaft EXPORT2D: Namen, Stärken, TO/2m/5m und Energie wie im Dump; die Spalte SP zeigt den Vereinswert (Liga + Pokal +
  Europapokal: HEISS 26 = 22 Liga + 4), wie bei Original-Ständen. Kein „SPIELER 117x“ mehr (EXPORT2C noch mit zwei solchen Namen
  und Energie 100 an allen Stammspielern – beides am Stand `c09cd45`, an 6805ebf behoben).
- Spieler ohne „eingesetzt“ (EXPORT1D STEFAN, DOROCHIN; EXPORT2D STEFAN) zeigen leere Statusspalte, „Eingesetzt: 22“ wie im Dump.

**Ergebnis:** R758-01 erledigt – die Exportstände ab `97c372a` laden im Original und zeigen den Inhalt des Exports. Offen bleibt
nur, was die Probe nicht prüft (Vereinsfelder 0x26/0x2C: §3.1, jetzt exportiert, nicht erneut im Original geladen).

### 3.3 Highscore „nur Weltkugel“ (FILE54, `highscore-weltkugel_1…4`)

- **Saisonwechsel** (Start FILE20, Manager „fsdfsd“, Deutschland in der WM-Gruppe ausgeschieden): Die Liste zeigt vor und nach dem
  Saisonwechsel nur „fsdfsd – DEUTSCHLAND – 1“ (Bilder `_2`, `_4`); der Saisonwechsel trägt denselben Manager nicht doppelt ein.
  Das Remake (`HighscoreTable.Insert` ersetzt den Eintrag mit gleichem Namen und Verein) und `HighscoreRules.Entry` (1 ohne Weltmeistertitel) stimmen
  damit überein.
- **Spielende** (FILE54, Dauer „eine Saison“, Manager „Spieler“): Deutschland steht im Halbfinale (Schweden – Deutschland 5:4,
  Bild `file54_3`), das Spiel um Platz 3 gegen Finnland spielt der Spielende-Lauf. Die Liste zeigt danach „Spieler – DEUTSCHLAND – 5“
  (Bild `file54_4`). Das passt zu „Σ 9 − Platz“ (Platz 4 → 5; Platz 3 gäbe 6), nicht zur Näherung des Remakes
  („Weltmeistertitel + 1“ → 1). **Neuer Befund (Vorschlag P3, Datei `Core/Highscore.cs`, nicht in dieser Spur):** Punkte im Modus
  „nur Weltkugel“ nach der WM-Platzierung; Beleg FILE54/`file54_4`, Fundstelle laut `Highscore.cs`-Kommentar. Die Vereinsspalte
  „DEUTSCHLAND“ stimmt mit dem Remake überein.
- Nach der Liste folgen Schlussbild „Das wars… Carpe diem !“ und das Programmende (`file54_5`, `file54_6`).

### 3.4 A189 „Gesperrte“

Laut Nutzer trat in einer ganzen Saison ab FILE37 kein gesperrter Spieler nach einem Spiel auf (README Runde 17, Sitzung 4); FILE55/56
gibt es nicht. A189 „Gesperrte“ bleibt `[APPROX]` und offen (Inventurzeile für `pruefung-karte`: unverändert, nur Vermerk).
