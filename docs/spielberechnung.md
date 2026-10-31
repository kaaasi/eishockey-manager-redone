# Spielberechnung des Originals (Overlay „Live-Spiel“, Spur `spielberechnung`)

Geprüft an `em.exe` mit `ndisasm`; alle Adressen sind **Dateioffsets** (Basissegmente: 0x1E00 + Segment · 16 + Offset;
Overlays: 0x255C0 + Dateiversatz aus dem Stub, siehe `reverse-engineering.md`, Abschnitt „Overlays“). Das in
`reverse-engineering.md` „5E73“ genannte Overlay „Live-Spiel“ ist Stub 0x130 (Code ab Datei 0x51E90, Einsprung 0x93 =
Spielabrechnung 0x544C0). Die Rechenfunktionen liegen großteils im Basissegment 0x62C (Datei 0x80C0 ff.) und im Overlay des
Spieltagsablaufs (Stub 0xF0, Datei 0x35540 ff.).

Zufall: `random(a, b)` = C-`rand` (Datei 0xC037, `0x99F:0x847`), `lrandom(a, b)` = eigene Folge
x = (16 · x + 167) mod 4.000.000, Ergebnis a + x · (b − a + 1) / 4.000.000 (Datei 0xBFD5, `0x99F:0x7E5`). Beide liefern
a … b einschließlich.

## 1. Ergebnis von Schritt 1: Ereignisse unabhängig von der Anzeige

Ja. Belege:

- **Spieltagsablauf 0x36B5D.** Ligaspieltage (Wettbewerbscode `[0xD0C2]` = 7) gehen immer in die Minutenschleife
  (0x3703C → 0x37088), gleich welche Anzeige eingestellt ist. Sie läuft drittelweise: `0x37A62(…, 1, 20)`, `(21, 40)`,
  `(41, 60)`; bei K.-o.-Unentschieden (Prüfung `0xE8:0x70`) `(61, 65)` und `(66, 70)`. Pokalspiele ohne Managerverein und
  mit ausgeschalteter Anzeige des Wettbewerbs (Schalter DGROUP 0x21E/0x221/0x224, 0x227 DEB-Pokal, 0x228 Europapokal,
  0x229 WM) nehmen den schnellen Weg 0x3731F (Abschnitt 3).
- **Minutenschleife 0x37A62.** Minute `[bp−0xC]` = Drittelanfang … Drittelende, je Minute alle Paarungen. Anzeige
  (Spielstände, Uhr, Reporter `0x48:0x2E1E`, Torszene) nur bei gesetztem Anzeigeschalter `[bp+0x10]` bzw. `[0x5A55]`;
  die Rechnung ab 0x37D8C (Liga) bzw. 0x380D0 (Pokale) läuft immer.
- **Nach dem Spieltag 0x52CF** verteilt das Original für Computervereine die Tore des Endstands auf Spieler
  (Abschnitt 5) – reine Rechnung aus dem gespeicherten Ergebnis.
- **Video** (`docs/dosbox-befunde.md`): Freundschaftsspiele mit Torszenen aus, ohne Live-Spiel – danach Tore und
  Strafminuten im Kader gefüllt.

Folge: Der Rechenteil wird ohne Darstellung nachgebaut (`OriginalMatch`, `OriginalMinutes`, `MatchStatistics` in
`src/EishockeyManager.Core/MatchEngine.*.cs`, Anschluss `MatchEngine.PlayWithEvents`, Abschnitt 11); die Anzeige folgt in
`livespiel`.

## 2. Ablauf einer Spielminute (Liga, 0x37DCF–0x380A4)

Je Paarung (Heim h, Gast g), übersprungen bei gewerteten Spielen (Ergebnisbyte 0x5A):

1. Managerpaarung? (`0x130:0x4D`: Verein in der Liste DGROUP 0x233, gesetzt beim Spieltagsbeginn) – dann bei Mausklick
   das Eingriffsmenü `0x38D96` (Anzeige).
2. Hat einer einen Manager (Vereinsfeld 0x16 ≠ 0): Aufstellung/Reihenfolge `0x38B8F`.
3. Torchancen `0x9F5C(h, g, Minute, Drittelende)` (Abschnitt 3, Minutenform) → Bit 0 Heim, Bit 1 Gast; je gesetztem Bit
   Abschluss `0xA484(Liga, Paarung, h, g, Seite, Minute, …)` (Abschnitt 4). Steht im Ergebnisbyte schon 0x1E, wird nicht
   neu gezogen.
4. Torhüterminuten der Managervereine `0x874D` für beide (Abschnitt 8; die Strafbank zählt erst 0x35818 herunter).
5. Nur Managerpaarungen: Energie `0x87F7`, Seiten zufällig tauschen (random(0, 1)), in ungeraden Minuten Taktik der
   Computervereine `0x80CB`, Reihenwechsel `0x8EF2` (je Seite), in geraden Minuten Spielstandtaktik `0x939A`.
6. Außer in Minute 60 und 70: **Strafen** `0x35818(h, g, g)` und `0x35818(g, h, h)` – für jede Paarung (Abschnitt 6).

Pokale (0x380D0 ff.) laufen gleich, mit Paarungen aus DGROUP 0x5CFB und Ergebnissen in 0x5B0B/0x5C4B.

## 3. Torbudget und Torchancen 0x9F5C (`OriginalMatch`)

Eingaben je Mannschaft: Stärke S = `0x368B1(Verein, Modus 1, Minute, Feldspielerdifferenz, neu)` (Abschnitt 7),
Feldspieler auf dem Eis F (Vereinsfeld 0x32: 5; bei Strafe weniger, ohne Torhüter 6), Angriff A (Vereinsfeld 0x2C,
Bits 4–6), Torhüter draußen (Bit 3). WM = `[0x12B]` (nur während WM-Spielen 1, Datei 0x30F4E/0x31012).

```
bH = max(0, lrandom(A_h, 7) − 5);  bG = max(0, lrandom(A_g, 7) − 5)        ; Angriff = untere Grenze (R7-02, #200)
d  = S_h/3 − S_g/3
tG = (WM und d < 19) ? lrandom(1, 2) : 0
tH = (d > −19) ? lrandom(1, 2) : 0
d > 0:  g = d > 19 ? 5 : T[d]                                               ; T = DGROUP 0x4948
        x, y = lrandom(1, g) ×2
        tH += (d > 8 und lrandom(0, 2) = 0) ? besser(x, y) : x              ; besser = 0x3688F
        d < 25: tG += lrandom(1, 2)
sonst:  d = −d;  g = d > 19 ? d/5 : T[d]
        d < 25: tH += lrandom(1, 3 − WM)
        x, y = lrandom(1, g) ×2
        tG += (d > 8 und lrandom(0, 3 − WM) = 0) ? besser(x, y) : x
(lrandom(0,1) = 0 oder lrandom(0, bH) = 0) ? tH++ : tG++
(lrandom(0,1) = 0 oder lrandom(0, bG) = 0) ? tG++ : tH++
nicht (0, 60): solange tH > 10 oder tG > 10: beide − 1
beide ≥ 0
S_h = 0: tH = 0, S_g > 20 → tG = 9;   S_g = 0: entsprechend
Code 1 (Play-off/Aufstieg) oder Minute > 60: S ≠ 0 → t = t/2 + 1
F_h < F_g: tH >>= 2;  F_g < F_h: tG >>= 2
Minute < 58 und ein Torhüter draußen: tH = tG = 9
(0, 60): Ergebnis = (tH, tG)                                                 ; schneller Weg
sonst:  (Code 1 oder Drittelende > 60): beide + 1
        je Punkt von tH: lrandom(1, 20) = 1 → Bit 0;  je Punkt von tG: Zähler += 2 (zwei Treffer = 4, Bit 1 aus)
```

T = 2, 2, 2, 2, 2, 2, 3 × 9, 4 × 5 (Index 0 … 19); besser(x, y) = (random(0, 1) ≠ 0 und x > y) ? x : y.
Der Heimvorteil steckt im Zweig d ≤ 0: Bei gleicher Stärke 4,5 : 2,5 im schnellen Weg.

**Schneller Weg (0x3731F–0x377A3, nur Pokale ohne Manager und ohne Anzeige; `OriginalMinutes.FastCupResult`):**
s = `0x368B1(…, 1, 0, 0, 1)` + 6 für Vereine (Index < 0x40), k = min(4, |s_H/3 − s_G/3|) (0x374AF, `0x120:0x61`).
Endstand (h, g) aus `0x9F5C(h, g, 0, 60)`. Glättung (lrandom): ohne WM mit 1/4 – sonst bei stärkerem Gast mit 2/3 –
h −= lrandom(0, h); dann mit 1/4 – sonst bei stärkerem Gast mit 1/2 – g += lrandom(0, 4). In der WM (`[0x12B]`) nur der
Heimwert: h −= lrandom(0, h), h += lrandom(0, h) (gleiche Bedingung) bzw. h += lrandom(0, h), h −= lrandom(0, h) (mit 1/4,
sonst bei stärkerem Heim mit 1/2). Dann lrandom(0, k) ≠ 0 und h ≤ g bei stärkerem Heim bzw. h ≥ g bei stärkerem Gast →
alles neu ab 0x9F5C (0x37665–0x37696). K.-o.-Gleichstand (`0xE8:0x70`): random(0, 2) je Seite dazu, bis ungleich,
Heim + 30 als Penaltymarke (0x376F0–0x377A3).

## 4. Abschluss 0xA484 (`OriginalMinutes.Shot`)

`0xA484(Wettbewerb, Paarung, h, g, Seite, Minute, Anzeige)`, je gesetztem Chancenbit einmal (Heim zuerst):

1. Stärken aus dem Puffer der Minute (`0x368B1(…, Modus, Minute, 0, 0)`): H0/H2 Heim in Modus 0 (85/15) und 2 (25/75),
   G0/G2 Gast. dH = (H2 − G0) / 3, dG = (G2 − H0) / 3 (C-Division).
2. **Schwäche** w = 1, wenn lrandom(550, 850) > H0 + G2 + G0 + H2, sonst 0 (Zug immer, Datei 0xA57F–0xA5AB).
3. Spielstand aus dem Ergebnisbyte (Liga DGROUP 0xA0BF + 0x210 · Wettbewerb + 0xC · Spieltag + Paarung, Pokale 0x5B0B).
   Minute > 60 und Heimbyte < 10 → kein Tor (Datei 0xA642). Gedeutet als Marke der Spiele in der Verlängerung: die
   Pokale tragen bei Gleichstand + 0x1E ein (0x32AA3); nur markierte Spiele treffen in der Verlängerung. Das Remake
   rechnet die Verlängerung nur für unentschiedene Spiele und setzt die Marke intern (`[APPROX]` Deutung).
4. Kein Tor, wenn die Schützenseite schon 29 oder ≥ 59 Tore im Byte hat (0xA6DA, 0xA78F).
5. **Heim:** lrandom(0, 2w + 4) ≤ 1 → kein Tor. **Gast:** lrandom(0, 3 · WM + 2w + 1) ≤ WM → kein Tor.
6. d = dH bzw. dG. d ≥ 0: Tor, wenn lrandom(0, d / 4 + w + 2) ≠ 0. d < 0: Tor, wenn lrandom(0, −d / 4 + 1) = 0, sonst
   wenn lrandom(0, 6 − w) = 0.
7. Hat die verteidigende Mannschaft einen Manager und ist DGROUP 0xDE15 + Manager = 0, ist **jeder** Abschluss ein Tor
   (0xA83E–0xA883; Bedeutung des Bytes offen, vermutlich „kein Torhüter aufgestellt“; Remake `LiveTeam.OpenNet`).
8. Tor (0xA88C ff.): bei Unterzahl der verteidigenden Mannschaft (`0x35540(…, 0)` > 0) `0x35691(…, 0, 0)` – eine Strafe
   endet (Abschnitt 6). Danach Manager-Zähler (Über-/Unterzahltore +0x177 + 8/0xA, Tore nach Feldspielerlage +0x177 + 2·k,
   ausgenommen 5 gegen 6), Gegentor des Torhüters auf dem Eis `0xA3F7` (nur Managervereine), Ergebnisbyte + 1.
9. Torschütze: in der Managerpaarung über die Torszene `0x53C57` (Anzeige-Overlay), sonst für Managervereine
   `0x541D9(Verein, 1, 0)`: Manager mit mehr als 3 fitten Spielern (`0x4BE17`); Torschütze `0x386D1(Manager, 0, 1)`,
   Vorlage `0x386D1(Manager, 1, 1)` bis zu einem anderen Spieler (ohne Grenze); Vorlage: Spielerfeld 0x1F + 1 (jedes
   Spiel); Tore nur bei `[0xD0BF]` = 0: Spielerfeld 0x1B + 1 (bis 255), Kaderplatz +0x22 + 2·`[0xD0BF]` (Wort) und
   +0x3 + `[0xD0BF]` (Byte). Computervereine bekommen ihre Tore erst nach dem Spieltag (Abschnitt 5).

Noch offen: Auswahl `0x386D1` im Einzelnen, Bedeutung von `[0xD0BF]` (vermutlich Wettbewerbsart 0 = Liga).

## 5. Statistik der Computervereine nach dem Spieltag 0x52CF (`MatchStatistics`)

Für jede Paarung `0x5028(Verein, Tore, Gegentore)` für Heim und Gast; nur Vereine (Index < 0x40) ohne Manager.

- Liga: Torhüter `0x4DB6(Verein, Gegentore)` – auch bei 0 Gegentoren: die ersten ≤ 3 Torhüter des Vereins im Pool;
  bei mehreren mit 1/9 ein Wechsel (Anteil random(0, 100) %, Minuten random(2, 100) · 60 / 100, Rest an den zweiten),
  sonst alles und 60 Minuten für den ersten. Gegentore im Spielerfeld 0x1B (Byte, Grenze 255), Minuten 0x1F (Wort).
- Ohne eigenes Tor sonst nichts (auch keine Spiele). Mit Tor: Spiele + 1 (Spielerfeld 0x1C) für **alle** Spieler des
  Vereins; Kandidaten = Feldspieler mit Rollenwert + random(5, 25) > random(10, 99), höchstens 50; unter 2 Kandidaten
  Ende. Je Tor: random(0, 4) = 0 → ohne Torschützen; sonst Grenze g = (n > 9 ? 10 : n − 1), über 2 noch − 1; Zug
  random(0, g): ≠ 0 → Kandidat random(0, n − 1) (Vorlage: anderer Kandidat, ≤ 250 Versuche), = 0 → neuer Versuch, wenn
  g ≠ 0 und n > random(0, 1) + 2. Erster Treffer Tore + 1 (Feld 0x1B), zweiter Vorlage + 1 (Feld 0x1F). Höchstens eine
  Vorlage je Tor.

## 6. Strafen 0x35818 und Strafbank 0x35540/0x35691 (`OriginalMinutes.Penalty`, `PenaltyBox`)

**Strafbank** (Vereinsfeld 0x33): Platz 0 in Bits 0–2 (Restminuten), Marke Bit 3; Platz 1 in Bits 4–6, Marke Bit 7.
Markierte Strafen (5 Minuten, Code 13 = 8 + 5) beendet kein Gegentor. Feldspieler auf dem Eis: Vereinsfeld 0x32.

- `0x35540(Verein, 1)` zählt herunter und liefert die belegten Plätze: Platz 0 − 1 (Marke bleibt); von 1 auf 0 →
  `0x35691(Verein, 0, 1)`. Dann Platz 1 − 1; von 1 auf 0 → `0x35691(Verein, 1, 1)`; sonst rückt Platz 1 nach Platz 0
  (Marke Bit 7 → Bit 3), wenn Platz 0 frei ist. Mit Schalter 0 nur zählen.
- `0x35691(Verein, Platz, Zwang)`: Platz 0 ohne Zwang: ist Platz 0 markiert oder Platz 1 belegt, wird Platz 1 frei
  (falls nicht markiert) – auch wenn Platz 0 eine gewöhnliche 2-Minuten-Strafe ist (übernommen); sonst Platz 0 frei
  (Platz 1 rückt mit Marke nach). Feldspieler + 1; beim Managerverein Spieler zurück in die Reihe (DGROUP 0xD096 ff.)
  und `0x38A36`.
- Das Herunterzählen geschieht **nur** am Anfang von 0x35818, also je Minute (außer 60/70) und Mannschaft einmal und
  bei einer Kettenstrafe (unten) für den Gegner ein zweites Mal. Nach dem Spiel räumt 0x377F7 die Strafbänke der
  Managervereine (Herunterzählen bis leer, ≤ 2000 Runden).

`0x35818(Verein, Gegner, Gegner)` jede Minute für beide Seiten (außer Minute 60/70):

- Herunterzählen (oben); sind danach 2 Plätze belegt: keine Strafe (kein Zug).
- Einsatz E = Vereinsfeld 0x26 (Automatik 0x8365: 60 oder 40 + Zuschlag), beim Managerverein + Byte DGROUP
  0x974B + 0x1AF · (Vereinsfeld 0x16) − 20 (Vereinsfeld 0x16 = Manager + 1; Bedeutung offen), begrenzt 0 … 100. **Strafe, wenn lrandom(0, 12) = 0 oder lrandom(0, 110 − E) = 0** (je Minute 1/13 + 12/13 · 1/(111 − E)).
- Managerverein mit weniger als 8 fitten Spielern (`0x120:0x66`): keine Strafe (nach dem Zug).
- Art: auf Platz 0 (Strafbank leer) mit lrandom(0, 15) = 0 fünf Minuten (Code 13), sonst zwei (Code 2).
- Feldspieler − 1, Code auf Platz „belegte Plätze“ (0x35941–0x35963). Liga: Strafminuten des Vereins
  DGROUP 0x82A1 + 0x50 · Verein (Wort). Managerpaarung: Spielstrafminuten DGROUP 0xA845 ff. für die Anzeige.
- Managerverein: Spieler `0x378C5(Manager)` (100 = keiner); Feldspieler: Kaderplatz +0 (5 min) bzw. +1 (2 min) + 1,
  Platz auf der Strafbank DGROUP 0xD095 ff., Kaderplatz +0xA = 0 (vom Eis), +0xD = Art. Über-/Unterzahlzähler des
  Managers (+0x177 + 8 bzw. 0x97DC), Reporter `0x130:0x48`.
- Computervereine: keine Strafen je Spieler (nur Vereinsminuten).
- Kette: lrandom(0, 3) (Zug nur, wenn Verein ≠ Gegner) = 0 und Strafbank vorher leer → `0x35818(Gegner, Gegner,
  Verein)`; dort Verein = Gegner, also keine weitere Kette. Bleibt die Strafe aus, wird der Überzahlzähler des Managers
  zurückgenommen.

## 7. Stärke im Spiel 0x368B1 / 0x977F (`OriginalMatch.TeamStrength`, `LineValues`)

`0x368B1(Verein, Modus, Minute, Feldspielerdifferenz, neu)`: je Teil (0 Abwehr, 1 Sturm) Werte der Reihe auf dem Eis
(Vereinsfeld 0x2E + 2 · Teil) aus `0x977F` (bei „neu“ = 0 aus dem Puffer DGROUP 0xE06E):
Technik − Minute · 10 / (Technik + 1) (≥ 0) + Kondition + Form, höchstens 255; gewichtet DGROUP 0x4E99
(85/15, 50/50, 25/75 %); ist die Summe ≠ 0, + (Vereinsfeld 0x17 − 44) / 2.

`0x977F` für Computervereine (bzw. Managervereine außerhalb der Managerpaarung): Teamwert + 4 − 4 · Reihe für
Kondition (0x18/0x19), Technik (0x1A/0x1B), Form (0x1C/0x1D); Unterzahl: Abwehr-Technik >> fehlende Spieler, Überzahl:
Sturm-Technik + Technik >> (3 − Überzahl); Technik · (Energie 0x1E + 4 · Teil + Reihe / 4 + 75) / 100 (ohne Spiel
100 %); im Spiel Kondition − (100 − Einsatz · Kondition / 100) / 4; alles 0 … 100. Für Managervereine in der
Managerpaarung werden die Werte aus den eingesetzten Spielern der Reihe gebildet (0x98F8–0x9C63, Teil 2).

## 8. Umsetzung im Remake

| Teil | Stand | Code |
|---|---|---|
| Schritt 1 (Anzeige unabhängig) | belegt | – |
| Torbudget/Chancen 0x9F5C | `[ORIGINAL]` | `OriginalMatch.FullResult`, `MinuteChances` |
| Stärke 0x368B1, Reihenwerte 0x977F (Computerteil) | `[ORIGINAL]` | `OriginalMatch.TeamStrength`, `LineValues` |
| Abschluss 0xA484 (ohne Manager-Zähler, Torschütze) | `[ORIGINAL]`, Verlängerungsmarke gedeutet | `OriginalMinutes.Shot` |
| Strafbank 0x35540/0x35691 | `[ORIGINAL]` | `PenaltyBox` |
| Strafen 0x35818 (Vereine ohne Manager, mit Kette) | `[ORIGINAL]` | `OriginalMinutes.Penalty`, `OriginalMatch.PenaltyMinutes` |
| Minutenablauf einer Paarung ohne Manager | `[ORIGINAL]`, Verlängerung ohne Marken `[APPROX]` | `OriginalMinutes.Play`, `PlayMinute` |
| Statistik Computervereine 0x5028/0x4DB6 | `[ORIGINAL]`, Rollenwert und Spielerreihenfolge `[APPROX]` | `MatchStatistics` |
| Zähler Torhüter (Gegentore, Minuten) | Schema 10 | `Player.GoalsAgainst`, `Player.GoalieMinutes` |
| Managervereine: Torhüterminuten 0x874D, Gegentor 0xA3F7, keine Strafen unter 8 fitten Spielern | `[ORIGINAL]` (Torhüter der Aufstellung) | `MatchEngine.CreditPlayers`, `LiveTeam.FewFitPlayers` |
| Managervereine: Torschütze 0x386D1, Strafspieler 0x378C5, Reihenwerte aus Spielern 0x98F8, Einsatzzuschlag 0x974B, Energie 0x87F7, Reihenwechsel 0x8EF2 | `[APPROX]` (nicht bzw. nur teilweise gelesen, Abschnitt 11) | `MatchEngine.LiveTeamOf`, `CreditPlayers` |
| Taktik im Spiel 0x939A (Manager, gerade Minuten) / 0x80CB (Computer, ungerade, nur Managerpaarungen) | Takt `[ORIGINAL]`, Werte `[APPROX]` über `Tactics.Effective*` | `LiveTeam.Tactics` |
| Schneller Weg der Pokale mit Glättung 0x3731F | `[ORIGINAL]` | `OriginalMinutes.FastCupResult` |
| Verletzungen im Spiel | nicht gesucht (offen) | – |
| Anschluss an Spieltage, Zähler je Saison, Mehrsaisonprobe | erledigt (Teil 3), Sollwerte auf dem Branch | `MatchEngine.PlayWithEvents`, Abschnitt 11 |

Rollenwert (Spielerfeld 0x18) führt das Remake nicht: `[APPROX]` Verteidiger 33, Stürmer 83 (Mitte der Spannen).
Verletzungen im Spiel sind noch nicht gesucht (Kandidat: Kaderplatz-Felder in 0x548A8 ff.); das Remake erzeugt im Spiel
keine Verletzungen.

`0x874D` ist nicht das Herunterzählen der Strafbank (so in Teil 1 vermutet), sondern zählt je Minute für die
Managervereine die Minuten der Spieler auf dem Eis mit Spielerfeld 0x18 = 0 (Torhüter): Spielerfeld 0x1F + 1.

## 9. Minutenablauf ohne Manager (`OriginalMinutes.Play`) und Größenordnungen

Je Minute: Stärken beider Mannschaften (Reihenwerte mit Feldspielerdifferenz, Modus 1/0/2) → `MinuteChances` →
Abschluss Heim, dann Gast → `Penalty(Heim, Gast)`, `Penalty(Gast, Heim)` außer in Minute 60/70. Drittel 1–20, 21–40,
41–60; K.-o. bei Gleichstand 61–65, 66–70 (danach Penaltyschießen beim Aufrufer).

Probe mit gleichen Teamwerten (`LiveTeam`, je 5000 Spiele; die Abbildung der Vereine des Remakes auf die Teamwerte
folgt in Teil 3, die Zahlen sind deshalb nur Größenordnungen):

| Teamwerte / Einsatz | Stärke | Tore je Spiel (Heim) | Strafminuten je Spiel (beide) | 5-Minuten-Strafen | Unentschieden |
|---|---:|---:|---:|---:|---:|
| 60 / 60 | 175 | 8,1 (5,2) | 24,9 | 0,65 | 11 % |
| 50 / 40 | 141 | 9,4 (5,9) | 23,6 | 0,63 | 10 % |
| 70 / 80 | 211 | 6,7 (4,5) | 28,0 | 0,73 | 10 % |
| 40 gegen 80 / 60 | 111 : … | 7,2 (0,4) | 24,9 | 0,66 | 0,3 % |

Auffällig: schwache Mannschaften treffen öfter (Schwäche w), der Heimvorteil ist groß. Vor dem Anschluss gegen die
Videos prüfen (Tore je Spiel, Strafminuten).

## 10. Felder für Folgearbeiten

- **Länderwerte nach Freundschaftsspielen (#193):** braucht nur das Endergebnis – `MatchReport.HomeGoals/AwayGoals`
  (und `Overtime`), wie bisher `MatchEngine.Play`.
- **Energieabzug je Spiel:** die Rechnung liefert je Mannschaft `LiveTeam.Effort` (Einsatz), die Reihe auf dem Eis
  (`DefenseLine`, `AttackLine`) und je Minute die Lage (`PenaltyBox.Skaters`); je Spieler gibt es im Remake noch keine
  Eiszeit. Das Original zieht die Energie in `0x87F7` je Minute nur in Managerpaarungen ab (noch nicht gelesen).
- **Statistik je Spieler:** `MatchReport.Events` (Tor: Minute, Seite; Strafe: Minute, Seite, 2/5) und
  `LiveTeam.PenaltyMinutes`; Computervereine verteilen Tore weiter über `MatchStatistics` aus dem Endstand.

## 11. Anschluss an die Spieltage (Teil 3, `MatchEngine.Live.cs`)

`MatchEngine.PlayWithEvents(game, heim, gast, Wettbewerb, K.-o., Zufall)` ersetzt `MatchEngine.Play` in der Liga
(`GameState.PlayNextMatchDay`), den Play-offs und der Aufstiegsrunde (`Playoffs.cs`), im DEB-Pokal (`CupsDeb.cs`), im
Europapokal (`CupsGroup.cs`/`CupsEurope.cs`) und in der WM (`CupsWorld.cs`, Nationalteams über `RatedNation`).
`MatchEngine.Play(Club, Club, Random)` bleibt unverändert für die Freundschaftsspiele in `CupsNational*.cs` (Spur
weltkugel-dosbox; Umstellung durch die Leitstelle in der Integration).

Das Nationalteam des Bundestrainers (`RatedNation`, ohne eigenen Kader) zählt in Freundschafts- und WM-Spielen über
die Aufstellung des Bundestrainers (`NationalCoach.Lineup()`) wie ein Managerverein: Spiele, Tore/Vorlagen, Strafen,
Gegentore und Minuten des Torhüters (Review-Befund R7-03, #201; Test `IntegrationRunde7NationalCountersTests`).

**Ablauf je Spiel**

1. Spielwertung (#165, 0x36DC5): nicht spielfähiger Managerverein → 0:2 bzw. 2:0 ohne Rechnung (`MatchReport.Forfeit`).
2. Pokale (DEB-Pokal, Europapokal, WM) ohne Managerverein: schneller Weg 0x3731F (Abschnitt 3). Das Original nimmt ihn nur
   bei ausgeschalteter Anzeige des Wettbewerbs; das Remake hat keine Anzeige (`[APPROX]`: Schalter gelten als aus).
   K.-o.-Gleichstand: der Sieger des Penaltyzugs (0x376F0) entscheidet, gespeichert bleibt der Gleichstand mit
   `PenaltyWinner` (`[APPROX]` Darstellung; bisher „n.P.“ ebenso).
3. Sonst Minutenablauf (Abschnitte 2, 4, 6, 9); K.-o. mit Verlängerung 61–70; danach weiter unentschieden → das bisherige
   Penaltyschießen von `KnockoutMatch.Play` (`[APPROX]`).
4. Zähler der Spieler (`CreditPlayers`).

Wettbewerbscode 1 (Torbudget halbiert, Abschnitt 3) gilt für Play-offs und Aufstiegsrunde; WM-Regeln (`[0x12B]`) für die WM.
K.-o.: Play-offs, DEB-Pokal, WM-Endrunde; Gruppenspiele (Europapokal, WM, Aufstiegsrunde) dürfen unentschieden enden.

**Abbildung `Club` → `LiveTeam`** (`MatchEngine.LiveTeamOf`)

| Vereinsfeld | Original | Remake |
|---|---|---|
| 0x18–0x1B Kondition/Technik Abwehr/Sturm | Lader 0x2EF1B: Datenbankbyte 0 (Abwehr) bzw. 1 (Sturm) ± random(0, Wert/10); Nationalteams aus DGROUP 0x4C4E | `TeamValue`: Datenbankstärke + Veränderung der Aufstellung gegenüber dem Spielbeginn (wie `MatchStrength`, ohne dessen Einsatzzuschlag), 0 … 100. `[APPROX]` ohne Streuung, Byte 1 nicht geführt; Nationalteams mit `NationalMatchStrength` |
| 0x1C/0x1D Form | random(45, 55), Europapokal-Gegner (Index > 64) − 5 | `[APPROX]` 50 bzw. 45 |
| 0x17 Tagesform | random(47, 53) | `[APPROX]` 50 |
| 0x26 Einsatz, 0x2C Angriff | Automatik 0x8365 bei der Spielvorbereitung | `Tactics.EffectiveEffort/EffectiveAttack` (Block 0, Stand 0:0); in Managerpaarungen nach dem Spielstand nachgeführt (Manager gerade, Computer ungerade Minuten) |
| 0x1E Energie | 0x5B (0x84D7) | `[ORIGINAL]` 91 |
| 0x2E/0x30 Reihe | Wechsel 0x8EF2 nur in Managerpaarungen | Reihe 0 (`[ORIGINAL]` ohne Manager, `[APPROX]` mit) |

Managervereine bilden im Original ihre Reihenwerte aus den eingesetzten Spielern (0x98F8–0x9C63, gelesen bis 0x9B99):
Mittel über drei Kaderplätze (DGROUP 0x5DC3 + 0x618 · Manager + 0x34 · Platz; Felder +0x10/+0x11/+0x12), Technik ·
(Energie +0x1B / 2 + 50) %, Abzüge −10/−15 je nach Schwierigkeit `[0xED8]` für Spieler auf fremder Position, Zuschlag
+0x19 >> 3 beim Torhüter. Der Maßstab der Spielerwerte im Remake (Basis 20 + Stärke/2, Spur `kader-start`) passt noch
nicht zu den Teamwerten; deshalb `[APPROX]` Teamwerte auch für Managervereine.

**Zähler** (`Player.Games/Goals/Assists/Penalties2/Penalties5/GoalsAgainst/GoalieMinutes`)

- Vereine ohne Manager (Index < 0x40, mit Kader): `[ORIGINAL]` 0x5028 aus dem Endstand, Torhüter 0x4DB6 nur in der
  Liga; auch nach einer Spielwertung (das Original wertet das gespeicherte Ergebnis). Keine Strafen je Spieler.
- Managervereine: Spiele + 1 für die Aufstellung; Gegentore und Minuten (60/70) für den aufgestellten Torhüter
  (`[ORIGINAL]` 0xA3F7/0x874D, ohne Torhüterwechsel im Spiel); Torschützen und Vorlagen nach 0x5028 unter den
  Feldspielern der Aufstellung, jede Strafe für einen zufälligen Feldspieler der Aufstellung (`[APPROX]`, 0x386D1 und
  0x378C5 nicht gelesen). Nach einer Spielwertung nichts.
- Saisonwechsel: alle Zähler auf 0 (`GameState.ResetMatchCounters`, `[APPROX]`: das Zurücksetzen im Original ist nicht
  gesucht); `Caps` bleiben.

**Größenordnung gegen das Video** (`SpielberechnungAnschlussTests.OriginalDatabase_GoalsPerGame_InVideoRange`):
Video „MEISTER werden in 75 Minuten“, Bild 01019 (Oberliga nach dem 36. Spieltag): 1732 Tore in 216 Spielen = 8,0 je
Spiel, 7,9 % Unentschieden. Remake mit Originaldaten nach 36 Spieltagen (Seed 7): Oberliga 7,96 Tore je Spiel (13,4 %
Unentschieden), 2. Liga 7,81, Bundesliga 6,17. Strafminuten im Video noch nicht vermessen (Fair-Play-Tabelle,
Folgespur).

**Mehrsaisonprobe** (`SpielberechnungAnschlussTests`): vier Saisonwechsel mit Originaldaten, passiv und aktiv (Modus 4),
und Modus 1 („nur Weltkugel“); Zähler je Saison gefüllt und konsistent (Vorlagen ≤ Tore, Spielertore ≥ halbe Ligatore),
nach jedem Wechsel 0. Alle bisherigen Referenzwerte neu (Begründung je Test im Kommentar): `RngTwinProbeTests` (6),
`GameModeTests.Mode4_ReferenceValues_Unchanged`, `DebCupDrawFlowTests` (Referenz; mit/ohne Anzeige weiter gleich).
Zufallsabhängige Testaufbauten angepasst: `EuropeanCupTests` (Gegnerstärke 95 statt 90), `SeasonPaymentTests` (bis 40
Seeds), `LifecycleRegressionTests.R204` (Seed 2: Markt nicht voll), `WaveBReviewRegressionTests.PR3501` (Spielerzahl
darf durch Nachbesetzung steigen).
