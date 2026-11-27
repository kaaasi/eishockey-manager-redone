# Auswertung DOSBox-Aufnahmen `runde-17` (Spur `abgleich-dosbox`, Runde 18) und `runde-19` (Spur `abgleich-dosbox-19`, §4 ff.)

Grundlage: Spielstände und Bilder im Referenz-Repo (`original/spielstaende/README.md`, Abschnitte „Runde 17, Sitzung 1–3“),
Werkzeuge `emtool abgleich`, `docs/archiv/analyse/belastung-staerke/*.sh|py` (Skripte und `trace.patch` wie dort, `trace.patch` nur
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
  | Abwehr 2 KÜHN, UVIRA | +15 | +8,8 ¹ |
  | Abwehr 3 KWASIGROCH, NÖTZEL | +13 | +14,0 |
  | Sturm 1 DOROCHIN, LIEBSCH, CHAIDAROV | +32 | +16,8 |
  | Sturm 2 HOCK, SIKORA, LUPZIG | +5 (2: HOCK, SIKORA) | +13,7 (0) |
  | Sturm 3 BRANDL, STEFAN, HANEMANN | −3 | +10,8 (0,33) |

  ¹ **Berichtigt (Runde 19):** Auswertefehler – KÜHN fehlte in der Remake-Summe (Namensabgleich `K]HN`/`KÜHN`), der Wert
  zählt nur UVIRA; berichtigt 12,7 (5 von 6 Saaten unter +15). Die Abweichung „Abwehr 2“ entfällt. Berichtigte Auswertung in
  Treffern und mit 40–200 Saaten: [`belastung-staerke.md` §9](belastung-staerke.md)
  (§9.1 Fehler, §9.2 Proben, §9.5 Folgerung). Die übrigen Zeilen sind Nettozuwächse aus 6 Saaten; maßgeblich ist §9.

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

`OUT=… docs/archiv/analyse/belastung-staerke/dump.sh FILE03 FILE16A FILE44 FILE53 EXPORT2D && python3 …/clubs.py …` (Computervereine
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
  „DEUTSCHLAND“ stimmt mit dem Remake überein. **Umgesetzt (Runde 19, Spur `pruefung-karte`, #826):** Datei 0x5D59B–0x5D607
  summiert 9 − Byte der Platzierungsfolge (Plätze ab 9 als 11 → −2), `HighscoreRules.WorldOnlyScore`, Test `HighscoreWeltkugelTests`.
- Nach der Liste folgen Schlussbild „Das wars… Carpe diem !“ und das Programmende (`file54_5`, `file54_6`).

### 3.4 A189 „Gesperrte“

Laut Nutzer trat in einer ganzen Saison ab FILE37 kein gesperrter Spieler nach einem Spiel auf (README Runde 17, Sitzung 4); FILE55/56
gibt es nicht. A189 „Gesperrte“ bleibt `[APPROX]` und offen (Inventurzeile für `pruefung-karte`: unverändert, nur Vermerk).

## 4. Runde 19: #730 Spieltag 2/3 und A093 (Spur `abgleich-dosbox-19`, Teil 1)

Grundlage: Aufnahme `runde-19` Sitzung 1 (README Referenz-Repo „Runde 19, Sitzung 1“): FILE50 → Spieltag 2 (EC RATINGEN –
KÖLNER EC) → FILE61 → Spieltag 3 (KÖLNER EC – ESV KAUFBEUREN) → FILE63, beide ohne Mausklick, Bilder `file61_03…77` und
`file63_009…104`. Werkzeuge: `abgleich-dosbox/reihen19.py` (Lampen wie `lampen.py`, Minute jetzt per Siebensegment-Leser aus dem
Managerkasten, an `file50` gegen die Handlesung 84/84 gleich), `abgleich-dosbox/treffer19.py`, `belastung-staerke/hits.py`.
Zusammen mit FILE02→50 (§1) ist der Lauf FILE02→50→61→63 ein Original-Lauf über drei Spieltage mit Reihen je Minute.

### 4.1 Reihen je Minute (Eiszeit)

`AbgleichDosboxLinesTests.File61_…`/`File63_…` (Kennzahlen wie §1.2, je 1000 Saaten, Grenze eine Saat je Seite): alle 32
Kennzahlen in der Verteilung des Remakes. Am Rand: Spieltag 2 Köln Abwehr R2 27 Minuten (3 von 1000 ≥), Ratingen Abwehr ≠ Sturm
50 Minuten (5 von 1000 ≥; Ratingen spielt Modus 3 und folgt den Kölner Reihen); Spieltag 3 Kaufbeuren Sturm R1 24 (19 von 2000 ≥),
Kaufbeuren Abwehr ≠ Sturm 0 (150 von 2000 ≤). Bei 48 Kennzahlen aus drei Spielen ist das Zufall in erwartbarem Umfang; mit 200
Saaten und Grenze 1 % (wie §1.2) fiele Spieltag 2 durch, deshalb die feinere Grenze (Begründung am Test).

**Eiszeit Köln (Minuten je Reihe, Abwehr / Sturm):** Spieltag 1 19-22-19 / 18-23-19, Spieltag 2 21-27-12 / 23-21-16, Spieltag 3
21-18-21 / 23-16-21. Sturm 1 und Sturm 3 spielen zusammen 59 bzw. 56 Minuten. Die Auffälligkeit aus FILE02→03 (`belastung-staerke.md`
§9.2: Sturm 1 − Sturm 3 = 35 Treffer, keine von 200 Saaten) **wiederholt sich im zweiten Original-Lauf nicht**: dort 27 − 27 = 0
(Remake 15,5 ± 7,5 für FILE02→03). Eine Regel „Sturm 3 ruht“ gibt es damit nicht; `ChangeLines` ist für drei Spieltage belegt.

Nebenbefund (ohne Wirkung auf #730): Köln beginnt Spieltag 2 im Sturm mit Reihe 3 aus dem Vorspiel und wechselt erst in Minute 4
(Datei 0x84D7 setzt nur die Minuten auf dem Eis zurück, `belastung.md` §2); das Remake beginnt jedes Spiel mit Reihe 1. Die Abwehr
beginnt dagegen mit Reihe 1, ebenso beide Teile an Spieltag 3 (erstes Bild noch Reihe 2/2 aus dem Vorspiel, im selben
Minutenbild Reihe 1/1). Warum die Abwehr zurückgesetzt wird, ist nicht gelesen; Vorschlag Inventur P3 (Wirkung: höchstens drei
Minuten einer Reihe je Spiel).

**Aufgelöst Runde 20 (#872, Spur `pruefung-karte` Teil 2):** Die Abwehr wird nicht zurückgesetzt. Der Spielstand hält die Reihen
im Vereinssatz (far:9657 + 0x3D · Verein, Felder 0x2E/0x30): Köln steht in FILE50 bei Abwehr 0 / Sturm 2 (der Wechsel der Abwehr
fiel in die fehlende Minute 60 von Spieltag 1), in FILE61 bei 1/1, in FILE63 bei 0/0; Kaufbeuren nach Spieltag 3 bei 1/1, alle
Vereine ohne Managerspiel bei 0/0. 0x84D7 setzt nur die Minuten 0x2F/0x31 (Datei 0x853F) und den Modus der Computervereine
(Datei 0x8521). Spieltag 2 beginnt also mit 0/2, der Sturm wechselt im Modus 1/2 in Minute 4; an Spieltag 3 folgt Köln im Modus 3
sofort den Reihen Kaufbeurens (0/0). Das Remake übernimmt die Reihen seither aus dem Vorspiel (`Tactics.MatchDefenseLine`,
Import/Export 0x2E/0x30, Schema 51; `ReiheBeiSpielbeginnTests`).

### 4.2 Belastung: Paare und Treffer je Spiel

`emtool abgleich FILE50 FILE61` und `FILE61 FILE63` (Saaten 1993, 4711, 1, 2, 3, 77): nur Sägezahn (MENDE 123 → 127 im Original,
im Remake bei 3 von 6 Saaten über 127 → 50; SIKORA, BRANDL je einmal) – beide Paare stehen mit `StrainSawtooth` in
`AbgleichOriginalPaareTests`. `hits.py` (140 Saaten):

| Paar | Original Treffer | Remake Mittel ± s (Bereich) | z |
|---|---:|---|---:|
| FILE50→61 (Spieltag 2) | 49 | 60,4 ± 6,4 (49–75) | −1,78 |
| FILE61→63 (Spieltag 3) | 46 (MENDE gekippt, untere Schranke) | 57,4 ± 6,2 (43–73) | −1,84 |
| FILE02→63 (Spieltage 1–3) | 139 | 173,6 ± 13,1 (145–200), **keine Saat ≤ 139** | −2,64 |

Jede einzelne Reihe liegt in der Streuung (|z| ≤ 1,9, Σ z² = 25,9 bei 24 Reihen). Zu niedrig ist die Summe – in allen drei
Spielen dieses Laufs.

**Eiszeit oder Trefferrate?** `treffer19.py` rechnet mit den Minuten auf dem Eis aus den Bildern die nach em.exe erwarteten Treffer
(Datei 0x8C53–0x8C82: `lrandom(0, 4 + (Kondition > Technik)) = 0` je Spieler und Minute; Simulation des Managerteils von 0x87F7 mit
den Reihen der Bilder, unabhängige Züge bzw. echte lrandom-Folge):

| Spiel | Original (Abwehr + Sturm) | erwartet aus den Bildern (Abwehr + Sturm) | Simulation lrandom-Folge | z |
|---|---:|---:|---:|---:|
| 1 (FILE02→50) | 44 (19 + 25) | 62,3 (27,1 + 35,2) | 60,0 ± 6,8 | −2,36 |
| 2 (FILE50→61) | 49 (23 + 26) | 62,6 (27,3 + 35,3) | 60,9 ± 7,0 | −1,69 |
| 3 (FILE61→63) | 46 (16 + 30) | 63,1 (27,6 + 35,5) | 60,9 ± 7,3 | −2,04 |
| Lauf | 139 | 188,0 | 181,8 ± 12,2 | **−3,51** |

Die Eiszeit ist also nicht die Ursache (jeder Spieler stand rund ein Drittel der Minuten auf dem Eis), sondern die Zahl der Treffer
je Minute auf dem Eis: im Lauf etwa 0,15 statt 0,19. Die Abwehr ist von der Erholungsannahme frei (Erholung 0 im Training, alle
Abwehrspieler ohne Einsatz ±0): 58 gegen 82,0 erwartet. Eine Verschiebung um eine Minute (0x87F7 läuft vor dem Reihenwechsel) ändert
die Erwartung um höchstens 0,3.

**Gegenrechnung mit den anderen Läufen aus FILE02** (gleicher Start, gleiche Gegner): FILE02→03 (drei Spieltage, anderer Lauf)
164 Treffer gegen Remake 167,4 ± 12,7, FILE02→51 (Spieltag 1, mit Klicks) 57 gegen 60,9 ± 6,0. Zusammen mit dem Lauf oben 360
gegen 410 ± 19 (z ≈ −2,7); alle Ein-Spieltag-Paare anderer Vereine (`belastung-staerke.md` §9.2) gemischt (FILE38→39 +0,6, FILE52→53
+1,7, FILE24→25 −1,9). Der Unterschied zwischen dem Lauf hier und FILE03 (139 gegen 164) liegt bei 1,4 s.

### 4.3 Geprüft in em.exe (zusätzlich zu `belastung-staerke.md` §9.3)

| Datei | Befund |
|---|---|
| 0x8B31–0x8C82 | Kaderplatz = DGROUP 0x5DC3 + 0x618 · (Vereinsfeld 0x16 − 1) + 0x34 · Platz; „auf dem Eis“ = (+0x15 & 0x7F) − 1 gleich Vereinsfeld 0x2E + 2 · (+0x15 ≫ 7); Zug `lrandom(0L, 4 + (+0x10 > +0x11))` (Argumente als `long`, Reihenfolge geprüft), Ergebnis 0 → `inc byte [es:bx+0x13]` auf denselben Kaderplatz |
| 0x8DF8–0x8E04 | Schleife über alle Kaderplätze bis zur Kadergröße (0x4BE17), keine Grenze bei 20 o. ä. |
| 0x37E03–0x37FB7 | Merker `[bp-0xF]` je Paarung neu, gesetzt über 0x52B91 (Liste 0x233, beide Vereine der Paarung); 0x87F7 für a und b in **jeder** Minute der Managerpaarung |
| 0xBFD5–0xC036 | lrandom: x = (x ≪ 4) + 0xA7 mod 0x3D0900, Wert = a + x · (b − a + 1) / 0x3D0900; einziger Schreiber von 0xE017/0xE019 (kein Neusetzen) |
| lrandom-Folge | ein einziger Zyklus (15.625 Zustände, alle ≡ 7 mod 16), Anteil Wert 0 bei `(0, 4)` genau 0,2, bei `(0, 5)` 0,1667 |
| 0x659C–0x6B2A | Training liest von den Spielerfeldern nur +0x13 (Belastung), weder Energie +0x1B noch Einsatz – die Erholung hängt nicht vom Spiel ab |

Alles wie im Remake (`OriginalMinutes.DrainEnergy`). Ein Mechanismus, der die Trefferrate im Original senkt, ist nicht gefunden.

### 4.4 Folgerung für #730 und A093

- **Erledigt:** Die Auffälligkeit „Sturm 1 gegen Sturm 3“ (FILE02→03) ist kein Muster des Originals (§4.1); die Reihenwahl ist für
  drei Spieltage belegt (Marker an `ChangeLines` neu gefasst: Beleg statt `[APPROX]`).
- **Offen:** Die Belastungstreffer je Minute auf dem Eis liegen im Lauf FILE02→63 bei allen drei Spielen unter der Regel aus
  em.exe (z −3,5 gegen die Simulation mit den echten Reihen; über alle Läufe aus FILE02 z ≈ −2,7). Weder „im Bereich“ (der Lauf
  allein liegt außerhalb jeder Saat) noch eine Ursache ist belegt. Hinweis an `DrainEnergy`; Pflichteinträge #730 unverändert.
  **Nachtrag (§4.5):** Gegenprobe nach A ausgewertet, #730 im Bereich geschlossen.
- **Entscheidung Nutzer (Vorschlag):** (A) Gegenprobe DOSBox: FILE50 fünfmal laden, Spieltag 2 ohne Klick spielen (ohne Bilder),
  jeweils im Hauptmenü sichern (FILE66–70) – fünf unabhängige Treffersummen desselben Spiels: Mittel um 49 belegt eine
  systematisch kleinere Rate (dann Suche im Live-Ablauf), um 60 macht den Lauf zum Zufall (#730 „im Bereich“ schließen);
  (B) #730 schließen „Code wie em.exe, Restabweichung eines Laufs“ und die Pflichteinträge in den Sägezahn überführen;
  (C) offen lassen.
- **A093** (Technik −10 außerhalb der eigenen Reihe): auch in `runde-19` spielt kein Spieler außerhalb seiner Reihe (ohne Klick, keine
  Umstellung) – nicht belegbar, bleibt offen.
- **Inventur (Leitstelle trägt nach):** A409 „Stand Runde 19 (Spur `abgleich-dosbox-19`, Teil 1): Reihenwahl für Spieltag 1–3 belegt
  (`AbgleichDosboxLinesTests`), Sturm 1/3 aus FILE02→03 nicht wiederholt; offen nur die Trefferrate (§4.2), #730 offen.“ A093 „Stand
  Runde 19: mit `runde-19` nicht belegbar, offen.“ Neu (P3): Reihe auf dem Eis bei Spielbeginn (Sturm aus dem Vorspiel, Abwehr
  Reihe 1), Remake beginnt mit Reihe 1 (§4.1). Runde 20: aufgelöst, beide Reihen aus dem Vorspiel (§4.1, #872).

### 4.5 Gegenprobe Sitzung 4: fünf Läufe von Spieltag 2 (Fortsetzung 1, R19-E4 = A)

Grundlage: Aufnahme `runde-19` Sitzung 4 (README Referenz-Repo „Runde 19, Sitzung 4“): FILE50 fünfmal geladen, Spieltag 2 (EC
RATINGEN – KÖLNER EC) ohne Mausklick gespielt, im Hauptmenü gesichert als FILE66 … FILE70 (keine Bilder). Zusammen mit FILE61
(Sitzung 1) sechs unabhängige Läufe desselben Spiels.

**Paare:** `emtool abgleich FILE50 FILE66…70` (Saaten 1993, 4711, 1, 2, 3, 77): nur Sägezahn (MENDE 123 kippt in FILE66–68 auf
50, in FILE69/70 nicht; im Remake je nach Saat; SIKORA bei Saat 2) – alle fünf Paare mit `StrainSawtooth` in
`AbgleichOriginalPaareTests`.

**Treffer „Belastung + 1“** (`hits.py`, 400 Saaten des Remakes ab FILE50; gekippte Spieler mit der Mindestzahl bis 128, „k“):

| Lauf | Original | A1 | A2 | A3 | S1 | S2 | S3 |
|---|---:|---:|---:|---:|---:|---:|---:|
| FILE61 (Sitzung 1) | 49 | 11 | 6 | 6 | 10 | 8 | 8 |
| FILE66 | 64 (k) | 19 | 6 | 3 | 14 | 14 | 8 |
| FILE67 | 58 (k) | 16 | 6 | 3 | 17 | 6 | 10 |
| FILE68 | 57 (k) | 15 | 5 | 3 | 14 | 8 | 12 |
| FILE69 | 60 | 11 | 9 | 5 | 15 | 13 | 7 |
| FILE70 | 53 | 11 | 3 | 5 | 9 | 8 | 17 |
| Remake (400 Saaten) | 60,9 ± 6,7 (40–84) | 12,3 | 7,5 | 6,6 | 12,8 | 11,5 | 10,2 |

- **FILE66–70:** Mittel 58,4 (s 4,0), Abstand zum Remake −2,5 bei einem Standardfehler von 3,0 (z −0,8); jeder Lauf in der
  Streuung (z −1,2 … +0,5), je Reihe Σ z² = 38,6 bei 36 Reihen. Mit FILE61: sechs Läufe 56,8 (s 5,2), z −1,5.
- **Folgerung:** Das Mittel liegt um 60, nicht um 49 (Kriterium §4.4 A). Eine systematisch kleinere Trefferrate des Originals
  ist damit nicht belegt; FILE61 (49) ist ein Lauf am unteren Rand, die Spannweite der sechs Läufe (49–64) entspricht der des
  Remakes. **#730 im Bereich geschlossen** (Code wie em.exe, §4.3). Abnahme: `AbgleichDosboxStrainHitsTests` (Mittel der fünf
  Läufe gegen 30 Saaten des Remakes, Grenze zwei Standardfehler; Gegenprobe Trefferrate · ¾ im Remake → rot, 30,0 gegen 45,4
  ohne Erholung).
- **Rest (zur Einordnung):** Über alle Läufe aus FILE02/FILE50 (FILE02→03, →51, →50→61→63 und FILE50→66…70) liegt die Summe bei
  652 gegen 706,3 ± 24,7 (z −2,2), durchgehend mit `hits.py` (FILE02→63 173,6 ± 13,1 aus §4.2 statt 181,8 ± 12,2 der
  Eiszeit-Simulation: 714,5 − 181,8 + 173,6; Varianz 24,2² − 12,2² + 13,1²; Runde 20, Review #870 P3-4); getragen wird das
  vom Lauf FILE02→50→61→63 (z −2,6, mit der Eiszeit-Simulation −3,5), nicht von der Gegenprobe. Ein weiterer Mechanismus ist aus em.exe nicht gelesen; ohne neuen Beleg kein Anlass für eine Änderung.
- **Pflichteinträge #730** (`StrainOf` ZIMMERMANN FILE16A→17, `Strain730_DeviatesAtMostSeeds`): seit Runde 20 (Review #870
  P3-2) ZIMMERMANN in `Expected` nur im Sägezahn, der Bereich über die Saaten als Drift-Wächter
  `Strain_DeviatesAtMostSeeds_DriftGuard` (ehemals #730).
- **A093** weiter nicht belegbar (kein Spieler außerhalb seiner Reihe).

## 5. Runde 19: #838 Echtzeit „Spiele laufen“ (Teil 2)

Grundlage: README Referenz-Repo „Runde 19, Sitzung 2“ (DOSBox 0.74-3, `dosbox-0.74-3.conf`: `cycles=fixed 3000`, `core=auto`,
`frameskip=0`; FILE05 geladen, Spieltag im Live-Spiel, Minute 1 bis 60 handgestoppt; Bilder `file05_r19_01…15`). Die
Geschwindigkeit setzt der Regler in Zehnern, Pinguine = Wert / 9 + 1 (`GameOptionsScreen`): ein Pinguin = 0, acht Pinguine = 70.

| Einstellung | Messung Nutzer | Pfad im Original | Remake |
|---|---|---|---|
| acht Pinguine (70) | 15,9 s für 60 Minuten | Schnelldurchlauf (≥ 70, Datei 0x37C7A): keine Pause, kein Reporter – reine Rechen-/Zeichenzeit | „Spiele laufen“ 20 s (20 Bilder je Minute) |
| ein Pinguin + Lampe (Lampe setzt 70) | 20 s | wie oben | wie oben |
| ein Pinguin (0) | 3:20 min mit Reporter-Unterbrechungen; dazwischen 1 s je Minute | Pause 45 bzw. 30 (Zwinkern) / 3 Ticks ≈ 0,77 s im Mittel + Rechenzeit | Live-Bildschirm: nur die Pause (≈ 0,77 s) |

**Ergebnis:** Ohne Pause braucht das Original bei 3000 Zyklen 0,27–0,33 s je Minute; die bisherige Näherung des Remakes für
„Spiele laufen“ (1/3 s je Minute, `MatchDayScreen.FramesPerMinute`, 20,35 s je Spieltag) liegt am oberen Rand und bleibt
(`[APPROX]` mit Messwert; im Original hängt die Dauer von der Rechenleistung ab). Bei Geschwindigkeit 0 erklärt Pause des Modells
(`LiveClock`, 0,77 s) plus diese Rechenzeit (0,33 s) die gemessene Sekunde je Minute; das Pausenmodell ist damit bestätigt. Tests:
`AbgleichDosboxEchtzeitTests` (Bereich 15,4–20,5 s, Geschwindigkeit 70 ohne Pause, Pause + Rechenzeit = 1 s ± 0,15).

Einschränkung: Gemessen wurde der Live-Bildschirm ohne Pause (Lampe bzw. 70), nicht „Spiele laufen“ ohne Live-Anzeige; beide
Wege rechnen dieselbe Minutenschleife 0x37A62 (ohne Anzeige ohne Tafel und Reporter), die Rechenzeit ist also eine obere Schranke.

**Neuer Befund (Vorschlag P2, Datei `UI/Screens/Live/LiveMatchScreen.cs`, Spur `torszenen`, nicht in dieser Spur):** Der
Live-Bildschirm des Remakes kennt keine Rechenzeit je Minute. Im Schnelldurchlauf (Lampe oder Geschwindigkeit 70) läuft ein Spiel
damit in rund 60–120 Bildern (1–2 s) statt 16–20 s, bei Geschwindigkeit 0 eine Minute in ≈ 0,77 s statt ≈ 1,1 s. Vorschlag: je
Minute zusätzlich `MatchDayScreen.FramesPerMinute` Bilder (wie „Spiele laufen“).

## 6. Runde 19: #847 Gegenprobe Neuer Manager (Teil 3)

Grundlage: README Referenz-Repo „Runde 19, Sitzung 3“: FILE64 (Stand FILE05, 20.09.1993, ein Manager) → „Neuer“: „Spieler2“ mit
EC HANNOVER (2. Liga), Start-Liga Bundesliga → FILE65 (Hauptmenü von Spieler2). Bilder `file65_1/2` (vorher) und `file65_5/6`
(nachher). Test: `AbgleichDosboxNeuerManagerTests` (Remake ab FILE64 mit dem Platz, den das Original gezogen hat, gegen FILE65 je
Vereinsname).

- **Satzvergleich FILE64 → FILE65** (`dump.sh`, Vereinssätze far:9657): nur Satz 4 und Satz 18 ändern sich. Satz 4 (Bundesliga,
  vorher EISBÄREN BERLIN) heißt jetzt EC HANNOVER, Satz 18 (2. Liga, vorher EC HANNOVER) EISBÄREN BERLIN; mit dem Namen wandern
  Manager 0x16 (Satz 4 = 2), 0x34 (Logo/Markierung, Satz 4 mit Bit 7) und Farbcode 0x3B/0x3C – wie 0x36169(v, r, 0x80) aus #854.
  Tabellensatz, Liga und Teamwerte von Satz 18 bleiben am Platz. **Satz 4 bekommt neue Teamwerte** 0x18–0x1D = 81/81/79/80/56/51
  (vorher 75/71/71/75/54/47).
- **Remake vorher:** Tabellen (Platz, Spiele, Punkte, Tore), Ligen, Vereine der Manager, Konto (500.000 DM), Eintritt und Kadergröße
  wie im Original; abweichend nur die Teamwerte des neuen Managerplatzes (blieben 75/71/71/75/54/47).
- **Ursache:** Die Managerschleife der Spielvorbereitung ruft nach der Aufstellung 0x426BC die Teamwerte aus der Aufstellung
  0x952C([0xA70E], 0) (Datei 0x2A067–0x2A078) – auch im Hinzufügen-Modus. Im Original stehen die Werte von Satz 4 genau auf
  `ClubLineupTeamValues` des FILE65-Kaders (Test „Aufstellung“). Das Remake rief die Rechnung in `AddManager` nicht.
- **Fix:** `GameState.WriteLineupTeamValues` nach der Aufstellung des Neuen (`GameState.AddManager.cs`). Regressionstest
  `File64ToFile65_SwapLikeOriginal` rot → grün; fremder Test `NeuerManagerTests.AddManager_BundesligaStart_…` angepasst (R15-E2 = C:
  Teamwerte des Platzes r jetzt aus der Aufstellung, Rest unverändert am Platz; Rückschaltprobe alter Code + neue Tests → beide rot).
  Die Spieler des Neuen zieht der Zufall (im Original andere Namen), sie sind nicht verglichen.
- Für die Leitstelle: `docs/reverse-engineering.md` Abschnitt „Neuer Manager mitten in der Saison“, Punkt „Folge mitten in der
  Saison“ nennt „Teamwerten 0x17–0x33“ am Platz – 0x18–0x1D schreibt danach 0x952C neu (Datei außerhalb der Spur).
