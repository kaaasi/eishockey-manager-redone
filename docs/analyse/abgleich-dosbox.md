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
