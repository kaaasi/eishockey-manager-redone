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
2. Hat einer einen Manager (Vereinsfeld 0x16 ≠ 0): Verletzungen im Spiel `0x38B8F` (§13; früher als Aufstellung gedeutet).
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
Heim + 30 als Penaltymarke (0x376F0–0x377A3). Im Remake (Runde 11, Spur wettbewerbe-spiel Teil 1) bilden DEB-Pokal und
WM-Endrunde daraus den Endstand mit Marke 1 (`CupMatch.FromReport`, `CupTie.Marks`, `WorldCup.MarksOf`); der Bericht trägt die
Zusatztore als `ShootoutGoals`, sein Spielstand bleibt der Gleichstand. Die Play-offs (`Playoffs.cs`) bleiben bei Gleichstand + Sieger.

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

Auswahl `0x386D1` und Aufruf je Abschluss: §17. Bedeutung von `[0xD0BF]` (vermutlich Wettbewerbsart 0 = Liga) offen.

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
| Managervereine: Reihenwerte aus Spielern 0x98F8, Managerwert 0x952C/0x974B, Energie 0x84D7/0x87F7, Reihenwechsel 0x8EF2/0x91FF/0x80CB, Taktik 0x939A, Strafspieler 0x378C5 | `[ORIGINAL]` (§13), Kaderreihenfolge und Feldspieler je Teil `[APPROX]` | `MatchEngine.Managed.cs` |
| Managervereine: Torschütze 0x541D9/0x386D1, Verletzungen 0x38B8F/0x05B1A, Heilung 0x7C12 | `[ORIGINAL]` (§17), Neuzählung 0x38A36 und Versicherung 0x07089 offen | `MatchEngine.Injuries.cs`, `CreditPlayers` |
| Taktik im Spiel 0x939A (Manager, gerade Minuten) / 0x80CB (Computer, ungerade, nur Managerpaarungen) | Takt `[ORIGINAL]`, Werte `[APPROX]` über `Tactics.Effective*` | `LiveTeam.Tactics` |
| Schneller Weg der Pokale mit Glättung 0x3731F | `[ORIGINAL]` | `OriginalMinutes.FastCupResult` |
| Lader der Teamwerte 0x2EF1B (Streuung, Form, Tagesform), Tagesform nach Ligaspielen 0x4F527 | `[ORIGINAL]`, Zeitpunkt des Ziehens `[APPROX]` (Abschnitt 12) | `MatchEngine.LoadTeamValues`, `UpdateDayForm`, `Club.TeamValues` |
| Verletzungen im Spiel | umgesetzt (§17) | `OriginalMinutes.Injuries`, `PlayerInjuries` |
| Anschluss an Spieltage, Zähler je Saison, Mehrsaisonprobe | erledigt (Teil 3), Sollwerte auf dem Branch | `MatchEngine.PlayWithEvents`, Abschnitt 11 |
| Computervereine ohne Kaderbezug (A174, Runde 12 Teil 3): Teamwert = Datenbankstärke, Aufstellung wirkt nur bei Managervereinen (`[APPROX]`) | `[ORIGINAL]` (0x977F liest nur die Vereinsfelder) | `MatchEngine.TeamValue(GameState, Club)`, `ComputerTeamValue` |

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
`MatchEngine.Play(Club, Club, Random)` hat keinen Aufrufer im Spielablauf mehr: Die Freundschaftsspiele rechnen seit der
Integration Runde 7 mit `PlayWithEvents`, ab Runde 17 mit Wettbewerbscode 0xD (`MatchCompetition.Friendly`: Regeln ohne WM,
Minutenschleife über den Bundestrainer; A008/A110, `docs/reverse-engineering.md` „Spur wettbewerbe-rest“).

Das Nationalteam des Bundestrainers (`RatedNation`, ohne eigenen Kader) zählt in Freundschafts- und WM-Spielen über
die Aufstellung des Bundestrainers (`NationalCoach.Lineup()`) wie ein Managerverein: Spiele, Tore/Vorlagen, Strafen,
Gegentore und Minuten des Torhüters (Review-Befund R7-03, #201; Test `IntegrationRunde7NationalCountersTests`).

**Ablauf je Spiel**

1. Spielwertung (#165, 0x36DC5): nicht spielfähiger Managerverein → 0:2 bzw. 2:0 ohne Rechnung (`MatchReport.Forfeit`).
2. Pokale (DEB-Pokal, Europapokal, WM) ohne Managerverein: schneller Weg 0x3731F (Abschnitt 3). Das Original nimmt ihn nur
   bei ausgeschalteter Anzeige des Wettbewerbs (Spielstand-Optionen DGROUP 0x227–0x229, `GameOptions.CupMatchUsesMinuteLoop`,
   Datei 0x3702E–0x37085); sonst Minutenschleife (Integration Runde 9). Anfangswerte: DEB-Pokal an, Europapokal und WM aus.
   Die WM-Endrunde (Code 10) prüft keinen Schalter und nimmt immer den schnellen Weg (#350; der Vergleich 0x3702E–0x37085
   ist belegt, das Erreichen des Spieltagsablaufs 0x36B5D aus 0x31017 ist aus dem Aufrufmuster geschlossen – der
   Überlagerungssprung über 0xF0:0x70 ist nicht aufgelöst, R9S-07 #358). Play-offs und Aufstiegsrunde
   (Code 1) ohne Managerverein nehmen den schnellen Weg, außer ein Feld „Ergebnisse“ der Meldungsmatrix (0x21E/0x221/0x224)
   ist an (0x37058–0x37085; voreingestellt aus; #351, Entscheidung Nutzer 01.11.2026). `[APPROX]` Das Original entscheidet
   einmal je Spieltag (Merker 0x36FEA: spielt ein Managerverein, laufen alle Paarungen durch die Minutenschleife), das Remake
   je Paarung – bleibt so (Entscheidung Nutzer 01.11.2026); das Verhalten des Originals wird nach v1.0 eine Einstellung (#349).
   `[APPROX]` Der WM-Schalter, den der Bundestrainer-Abschnitt setzt (0x5541F/0x5575A, Sicherung 0x22C), ist nicht nachgebaut.
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
| 0x18–0x1B Kondition/Technik Abwehr/Sturm | Lader 0x2EF1B: Datenbankbyte 0 (Abwehr) bzw. 1 (Sturm) ± random(0, Wert/10); Nationalteams aus DGROUP 0x4C4E | `TeamValue`: Datenbankstärke + Veränderung der Aufstellung gegenüber dem Spielbeginn (wie `MatchStrength`, ohne dessen Einsatzzuschlag) + Streuung des Laders (`Club.TeamValues`, Abschnitt 12), 0 … 100. `[APPROX]` Byte 1 nicht geführt; Managervereine ohne Streuung; Nationalteams mit `NationalMatchStrength` |
| 0x1C/0x1D Form | random(45, 55), Europapokal-Gegner (Index > 64) − 5 | `[ORIGINAL]` aus dem Lader (Abschnitt 12); Managervereine `[APPROX]` 50 |
| 0x17 Tagesform | random(47, 53), nach Ligaspielen 0x4F527 | `[ORIGINAL]` aus dem Lader, nachgeführt (Abschnitt 12) |
| 0x26 Einsatz, 0x2C Angriff | Automatik 0x8365 bei der Spielvorbereitung | `Tactics.EffectiveEffort/EffectiveAttack` (Block 0, Stand 0:0); in Managerpaarungen nach dem Spielstand nachgeführt (Manager gerade, Computer ungerade Minuten) |
| 0x1E Energie | 0x5B (0x84D7) | `[ORIGINAL]` 91 |
| 0x2E/0x30 Reihe | Wechsel 0x8EF2 nur in Managerpaarungen | Reihe 0 (`[ORIGINAL]` ohne Manager, `[APPROX]` mit) |

Managervereine bilden im Original ihre Reihenwerte aus den eingesetzten Spielern (0x98F8–0x9C63, gelesen bis 0x9B99):
Mittel über drei Kaderplätze (DGROUP 0x5DC3 + 0x618 · Manager + 0x34 · Platz; Felder +0x10/+0x11/+0x12), Technik ·
(Energie +0x1B / 2 + 50) %, Abzüge −10/−15 je nach Schwierigkeit `[0xED8]` für Spieler auf fremder Position, Zuschlag
+0x19 >> 3 beim Torhüter. Der Maßstab der Spielerwerte im Remake (Basis 20 + Stärke/2, Spur `kader-start`) passt noch
nicht zu den Teamwerten; deshalb `[APPROX]` Teamwerte auch für Managervereine. **Überholt:** seit Spur
spielstaerke-manager-2 vollständig gelesen und umgesetzt (§13); die Startwerte der Kaderspieler (Spur `kader-start`,
DGROUP 0x4B42) haben den Maßstab der Teamwerte.

**Zähler** (`Player.Games/Goals/Assists/Penalties2/Penalties5/GoalsAgainst/GoalieMinutes`)

- Vereine ohne Manager (Index < 0x40, mit Kader): `[ORIGINAL]` 0x5028 aus dem Endstand, Torhüter 0x4DB6 nur in der
  Liga; auch nach einer Spielwertung (das Original wertet das gespeicherte Ergebnis). Keine Strafen je Spieler.
- Managervereine: Spiele + 1 für die Aufstellung; Gegentore und Minuten (60/70) für den aufgestellten Torhüter
  (`[ORIGINAL]` 0xA3F7/0x874D, ohne Torhüterwechsel im Spiel); Torschützen und Vorlagen nach 0x5028 unter den
  Feldspielern der Aufstellung aus 0x386D1 im Spiel (§17); die Strafe zählt beim Spieler aus 0x378C5 (§13, §16).
  Nach einer Spielwertung nichts.
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

## 12. Teamwerte des Laders und Meisterverteilung (Spur spielstaerke-manager, #204)

**Lader 0x2EF1B** (Ov 0xE0, Code 0x2EFDE–0x2F158, gerufen nur aus 0x437D5 beim Programmstart, vor der
Spielvorbereitung 0xD8:0x2F): je Verein i = 0 … 149 in dieser Reihenfolge 0x17 = random(47, 53); je Teil k = 0, 1 mit
v = Datenbankbyte k, t = v / 10: Kondition 0x18 + k = v − random(0, t) + random(0, t), Technik 0x1A + k ebenso, Form
0x1C + k = random(45, 55) − 5 · (i > 64). Nationalteams 52 … 63: Kondition/Technik aus DGROUP 0x4C4E (ohne Streuung).

**Schreibzugriffe auf die Vereinsfelder 0x17–0x1D** (Suche über die ganze `em.exe`: Schreibbefehle auf
`[es:…+0x17…0x1D]` mit Vereinszeiger DGROUP 0x9657 und Faktor 0x3D in den 40 Befehlen davor): nur der Lader,
0x4F527 (0x17), 0x55506–0x55560 (Länderwerte Deutschlands `[0x6AC]` neu gewürfelt bei `[0x9EDA]` = 0xFD), 0x9721–0x9771
(0x952C, Länderwerte aus der Aufstellung, #193), 0x9E46–0x9F31 (0x98F8, Managervereine aus den Spielern), 0x570E (0x1D,
nicht gelesen), 0x49723 (0x1A = 4, Ov 0x118, nicht gelesen). **Die Teamwerte der Computervereine bleiben also im Original
das ganze Spiel über gleich** – nur die Tagesform wandert.
**Berichtigt in §15:** Die Suche hat die Wanderung 0x9D1B übersehen (0x9E46–0x9F31 liegen in 0x9D1B, nicht in 0x98F8) und
0x58DC (Verkauf an einen Computerverein) nicht erfasst – die Teamwerte wandern im Original ständig (0x58DC seit Runde 11
umgesetzt, siehe §15).

**Tagesform 0x4F527** (Ov 0x128, aus der Tabellenrechnung 0x50283 mit d = eigene − gegnerische Tore, nur bei
Wettbewerbscode `[0xD0C2]` = 7 Liga bzw. 8 WM-Gruppe): r = random(0, 2 · |d|) (`0x120:0x61` = |a − b|), 0x17 ± r je
nach Vorzeichen, begrenzt auf 47 … 53 (`0x120:0x52`). Sie wirkt in 0x368B1 mit (0x17 − 44) / 2 = 1 … 4 auf die Stärke.

**Remake** (`MatchEngine.TeamValues.cs`): `Club.TeamValues` (Abweichung von Kondition/Technik, Form, Tagesform; Schema
12, Migration `SaveGame.Migration.SpielstaerkeManager.cs`). `[APPROX]` gezogen beim ersten Spiel eines Spielstands aus
`GameState.Random` (Spielstart und Kaderaufbau bleiben unverändert; ältere Spielstände ebenso beim nächsten Spiel).
Computervereine spielen mit Teamwert + Streuung und Form des Laders, alle Vereine mit der Tagesform; Managervereine
ohne Streuung und mit Form 50 (im Original aus den Spielern, 0x98F8, noch `[APPROX]`). Tagesform nach jedem Ligaspiel,
WM ohne Nachführung (`RatedNation` hat keine eigenen Werte).

**Meisterverteilung** (`SpielstaerkeManagerTests.SeasonDistribution_OriginalDatabase`, Originaldaten, Managerverein 42
aktiv, Seeds 1–12, je vier Saisonwechsel = 48 Titel; Datenbankstärken Düsseldorf 92, Köln 90, Krefeld 88, Hedos
München 86, Mannheim 84):

| Meister | vor der Spur (Base `10eb6c5`) | mit Lader und Tagesform |
|---|---:|---:|
| Düsseldorf (2) | 21 | 18 |
| Köln (5) | 11 | 19 |
| Krefeld (6) | 8 | 5 |
| Hedos München (9) | 4 | 3 |
| Mannheim (8) | 2 | 2 |
| Kaufbeuren (4) | 1 | 1 |
| Preussen Berlin (1) | 1 | 0 |
| Bundesliga Tore je Spiel / Unentschieden | 6,27 / 11,6 % | 6,28 / 11,7 % |

Einordnung #204: Mehrjährige Dominanz desselben Vereins **innerhalb eines Spiels** ist im Original angelegt, weil die
Teamwerte nach dem Laden fest bleiben; die Streuung entscheidet je Spiel, welcher der starken Vereine vorn liegt (jetzt
Düsseldorf und Köln etwa gleich oft statt Düsseldorf allein). Kein weiterer Befund in der Abbildung der Computervereine.
**Offen:** Vergleich mit den Saisonenden der Videos (Reihe `folgen`, „Saisonende“) – in diesem Lauf kein Zugriff auf
das Referenz-Repo; Strafminuten je Spiel nicht vermessen (die Ligaergebnisse führen keine Strafen).

## 13. Managerteil (Spur spielstaerke-manager-2)

**Managerpaarung** (0x130:0x4D, im Remake jedes Spiel mit Managerverein): nach den Abschlüssen und den Torhüterminuten
(0x874D) je Minute (Datei 0x37F9B–0x38073) Energie `0x87F7` für Heim und Gast, Seiten zufällig tauschen (random(0, 1); die
Reihenfolge gilt auch für die Strafen dieser Minute), ungerade Minute `0x80CB(a, b, Tore a, Tore b, Minute)`, Reihenwechsel
`0x8EF2(a, b)` und `0x8EF2(b, a)`, gerade Minute `0x939A` für beide. Außerhalb der Managerpaarungen ändert sich nichts.

- **Spielvorbereitung 0x84D7(0)** (Datei 0x36C4F): Reihenenergie aller Vereine 0x5B und einmal herunterzählen = 90
  (bisher 91, gleicher Technikfaktor); Kaderplätze der Manager 101 − Belastung/8, höchstens 91, minus 1. In jeder
  Drittelpause (Datei 0x37A86, auch vor der Verlängerung) **0x84D7(1)**: + 20, ebenso begrenzt.
- **Energie 0x87F7**: Computerverein je Teil und Reihe 0 … 3 – nicht auf dem Eis und < 90: + 1, mit 1/5 und
  Kondition > lrandom(0, 100) + 2; auf dem Eis: Minuten m + 1, − ((lrandom(0, Einsatz) + 75)/40 + 1) − 4 (m ≥ lrandom(0, 5))
  − 8 (m > lrandom(5, 15)), 1 … 99. Managerverein ebenso je eingesetztem Kaderplatz (Kondition des Spielers; vorher
  lrandom(0, 4 + (Kondition > Technik)) = 0 → Belastung + 1), danach Reihenenergie = Summe der Feldspieler / 3 (Abwehr mit
  90 für den fehlenden dritten Platz). Seit Runde 12 (A184) führt das Remake die Belastung als eigenes Feld (`Player.Strain`):
  + 1 wie oben (Datei 0x8C82), nach dem Spiel eines Managervereins 50 … 150 für alle Kaderplätze (Spielabrechnung 0x544C0,
  Datei 0x549A2–0x549BB). Mit dem Standardplan erholt das Training nicht (Erholung 0), die Belastung der Stammspieler steigt
  daher über die Saison bis 150 (Spalte E 100) – nur freie Einheiten senken sie.
- **0x80CB** (nur Computervereine bzw. „Automatisch“): Modi (2, 2); (2, 1) bei Führung von a nach Minute 40 oder weniger
  Feldspielern; (1, 2) bei Rückstand nach Minute 40 oder mehr Feldspielern, sonst Stärkevergleich 2·S (8 Bit):
  +20 → (3, 2), +15 → (2, 3). Computer ziehen in Minute 58/59/68/69 bei einem Tor Rückstand den Torhüter (wirksam 59/69).
- **0x8EF2** mit 0x91FF (Reihen nach Energie, 3 bzw. mit vierter Reihe 4, Modus 1 nur 2): Modus 1/2 nach > 3 Minuten zur
  energiestärksten Reihe (bzw. zur zweiten); Modus 3 Reihe des gegnerischen anderen Teils; Modus 4 Minuten-Vorgabe
  (Zähler DGROUP 0x4926); „Voll manuell“ kein Wechsel. Die Energieprüfungen „+ 4“ / „+ 8“ vergleichen im Original die
  Energie mit der Reihennummer und greifen nie (übernommen).
- **0x98F8** (Reihenwerte aus Spielern): Abwehr = (2 Verteidiger · 80 % + Torhüter · 140 %) / 3, Sturm = 3 Stürmer / 3;
  Technik · (Energie/2 + 50) %; Abzüge fremde Position (−15, Level < 5) und fremde Reihe (−10, Level 4/5; `[APPROX]`
  entfällt, Kaderplatz +0x02 nicht geführt); Unterzahl Technik − (45 − Unterzahl-Halbbytes + 16·(−1 − Differenz)),
  Überzahl + Überzahl-Halbbytes + 16·(Differenz − 1); ohne Torhüter (DGROUP 0xDE16) bricht die Abwehr zusammen und jeder
  Abschluss ist ein Tor (0xA83E, `LiveTeam.OpenNet`). Bundestrainer + Länderspiele/8.
  Die Halbbytes (Kaderplatz +0x19, unten Unterzahl, oben Überzahl) starten mit random(3, 12) je Halbbyte (Spielstart
  Datei 0x299F4, Talent Datei 0x06F23, Marktplatz Datei 0x49803), im Nationalteam 7/7 (Datei 0x3CB2C); Training verschiebt
  sie in 0 … 15 (Teil 7, A185/A186, `HiddenSkills`; `[APPROX]` am Spieler statt am Kaderplatz, Computerspieler 7/7).
- **0x952C**: Teamwerte des Managervereins aus Reihe 0–2 ohne Spiel und Managerwert M = clamp(d/5 + 20, 0, 40) mit
  d = Σ laufende Summen (Kondition − Technik). Strafen (0x35890) lesen M über Vereinsfeld 0x16 = Manager + 1, also den
  **Satz des nächsten Managers** (übernommen; `[APPROX]` ohne nächsten Manager 0). Für #193 liefert
  `MatchEngine.LineupTeamValues` die Werte aus der Aufstellung des Bundestrainers; den Anschluss nach
  Freundschaftsspielen (0x555D6) in `Core/CupsNational*.cs` setzt die Leitstelle.
- **0x378C5** (Strafspieler): höchstens 150 Züge (§16) random(0, Kaderplätze − 1); nur Spieler der Reihe auf dem Eis; Technik <
  Kondition → random(0, 10) > 3, sonst > 7; Torhüter mit 2/3 verschont. Feldspieler gehen vom Eis bis zum Freiwerden
  (DGROUP 0xD095, `PenaltyBox.Occupants`); ohne Spieler (100) keine Kettenstrafe. Die Strafe zählt beim gewählten
  Spieler (Kaderplatz +0/+1), beim Torhüter bei keinem.
- **0x38B8F = Verletzungen im Spiel** (umgesetzt, §17): je Minute je Managerverein mit ≥ 4 fitten Spielern (Schalter DGROUP
  0x4E80 aus) random(0, 20·clamp(60 − M, 0, 40) + 200·(5 − Level) + 200) = 0 → Spieler 0x378C5, Ereignis 4
  (0x130:0x7F, Dauer dort), Versicherung 0x20:0xBA9. Torschütze 0x386D1: §17.

**Wirkung** (Probe, Originaldaten, Seed 7, 400 Spiele je Paar, Liga): Managerverein Düsseldorf (Datenbank 92, Startkader
Bundesliga/Mittelfeld 81 ± 2) gegen Köln (90): vorher 197 Siege/154 Niederlagen (3,21 : 2,63 Tore), jetzt 90/280
(2,35 : 4,23) – der Managerverein spielt mit seinen Spielern statt mit der Datenbankstärke. Oberliga Verein 42 (31 ± 2)
gegen Verein 40 (20): vorher 192/165, jetzt 276/83. Strafminuten je Spiel Managerverein 12,8 statt 14,4 (Managerwert
des nicht belegten Satzes, oben).

**Meisterverteilung** (`SpielstaerkeManagerTests.SeasonDistribution_OriginalDatabase`, Verein 42 aktiv, 48 Titel):
Köln 21, Düsseldorf 12, Hedos München 7, Krefeld 5, Preussen Berlin 2, Mannheim 1; Bundesliga 6,28 Tore je Spiel,
11,6 % Unentschieden (Managerverein 42 spielt in der Oberliga, die Bundesliga ändert sich nur über Pokal- und
Europapokalspiele).

**Videovergleich #204** (Reihe `folgen`, Meisterfotos „Deutscher Meister“, 18 Saisonenden einer Laufbahn): Düsseldorf
(92) F2, F9; Preussen Berlin (82) F3, F12, F13; Freiburg (72) F4, F6; Schwenningen (72, 2. Liga) F7; Nürnberg (63,
2. Liga) F10, F23, F25; Köln (90) F14, F17, F19, F21; Landshut (76) F16; Ratingen (78) F27, F29. **Befund:** im Original
acht Meister in 18 Saisons, die beiden stärksten Vereine nur 6 von 18; im Remake die beiden stärksten 33 von 48 und nie
ein Verein unter 82. Mit festen Teamwerten der Computervereine (§12) erklärt das weder der Lader noch der Managerteil.
Offene Kandidaten: Play-off-Format (`[APPROX]`), weitere Schreibzugriffe auf Teamwerte (0x570E auf 0x1D, 0x49723 auf
0x1A, nicht gelesen), Kaderwechsel durch den Video-Manager (Berliner SC). #204 bleibt offen. **Berichtigt in §15:** F19 zeigt Freiburg, nicht Köln; Folge 8
beginnt ein zweites Spiel. Ursache: Wanderung der Teamwerte 0x9D1B.


## 14. Spielstärke der Managervereine gegen das Original (Spur spielberechnung-komplett, Teil 1; R8W3I-15, #204)

Geprüft an `em.exe` (`ndisasm`). Kein Zugriff auf das Referenz-Repo in diesem Lauf (Videos und DOSBox-Spielstände nicht
nachgesehen); Originaldaten aus der ZIP des Projekts.

**R8W3I-15 – kein Fehler.** Ein Managerverein spielt in der Managerpaarung mit seinen Spielern (0x98F8, §13). Deren
Startwerte hängen nur von Start-Liga und Start-Stärke ab (DGROUP 0x4B42, Datei 0x29DA4; `reverse-engineering.md`
„Startwerte der Kaderspieler“), nicht von der Datenbankstärke. Wer Düsseldorf (92) mit „Mittelfeld“ übernimmt, bekommt einen
Bundesliga-Mittelfeldkader (81 ± 2) und landet im Mittelfeld – so ist es im Original angelegt. `PlayAtDatabaseStrength`
setzt dagegen alle Spieler auf die Datenbankstärke; das gibt es im Original nicht und ist nur ein Testaufbau für Abläufe,
die einen starken Managerverein als Vorbedingung brauchen. Probe (`SpielberechnungKomplett1Tests`, Originaldaten,
Seeds 1–12, Hauptrunde, passiver Manager), mittlerer Platz / Tordifferenz:

| Verein (Datenbank) | Oberes Drittel (87) | Mittelfeld (81) | Abstiegskampf (70) |
|---|---|---|---|
| Düsseldorf (92) | 6,42 / +4,8 | 8,67 / −31,9 | 11,83 / −115,2 |
| Rosenheim (70) | 5,92 / +3,7 | 9,50 / −45,2 | 11,75 / −126,5 |

Die Datenbankstärke spielt keine Rolle (Abstand je Spalte < 1), die Start-Stärke ordnet die Plätze.
Im Original wählt die Spielvorbereitung außerdem bei einem Verein außerhalb der Start-Liga einen zufälligen Platz der
Start-Liga (`random([0x7C0 + Liga], …)`, Datei 0x29C78) und tauscht nur die Kennung (0x36169 mit Schalter 0x80, Datei
0x29CF2/0x29D9C, unten) – die Teamwerte bleiben am Platz. Das Remake bildet das beim Spielstart nach (`NewGame.PlaceInStartLeague`,
U15-a) und seit Runde 19 auch beim Hinzufügen eines Managers mitten in der Saison (`GameState.SwapClubPlaces`, Spur
`neuer-manager`, #847; Belege `reverse-engineering.md` „Neuer Manager mitten in der Saison“): dann bleiben auch Tabellenstand,
Spielplan und Ergebnisse am Platz.

**Kandidaten für die Meisterstreuung (#204), geprüft:**

- **0x570E und 0x49723 schreiben keine Teamwerte.** Beide adressieren mit Faktor 0x21 den Spielersatz (DGROUP 0x8253),
  nicht den Vereinssatz (0x3D, 0x9657): 0x570E setzt Spielerfeld 0x1D (Verein) beim Verteilen des Transferpools
  (Schleife 0x564C–0x571D über random(1, 0xB3)), 0x49723 setzt Spielerfeld 0x1A (Besitzer) = 4 beim Wechsel eines Spielers (Ov 0x118).
  Damit bleibt §12: Die Teamwerte der Computervereine ändern sich nach dem Lader nicht.
- **Vereinstausch 0x36169 (Ov 0xF0 + 0xC29, Stub 0xF0:0x4D)** `f(a, b, Schalter)`: tauscht alle Spieler (Feld 0x1D) und
  Managerzuordnungen (DGROUP 0x966F + 0x1AF · m) von a und b, bei Schalter & 0x7F zusätzlich DGROUP 0x387, 0xD0E6
  (Meister), 0x388. **Schalter 0x80:** nur Bytes 0x00–0x15, 0x16 (Manager), 0x34 und das Wort 0x3B des Vereinssatzes –
  die Teamwerte 0x17–0x1D bleiben am Platz (nur Spielstart 0x29CF2/0x29D9C, Managerverein in die Start-Liga). **Sonst:**
  ganzer Vereinssatz (0x3D Byte), bei Vereinen < 0x40 auch Tabellensatz 0x8257 (0x50 Byte) und Serien 0xB57B (0x15 Byte).
  Aufrufe mit vollem Tausch: 0x35EDD (WM-Länder 20-mal, danach je Liga 55-mal zwei zufällige Plätze der Liga, Platzliste
  0x5CCB mitgetauscht; Zufall lrandom Datei 0xBFD5, nicht `random` 0xC037). **Aufrufer von 0x35EDD** sind nur zwei: der
  Spielstart (Datei 0x43A17 in 0x437D5, vor der Spielvorbereitung 0x28030) und der Saisonwechsel am 29. April (Datei 0x5644E in
  0x54C9F, nach Auf-/Abstieg und Ewiger Tabelle, vor dem Fenster „Saisonende“). Ein Aufruf „nach dem Lader“ besteht nicht:
  Datei 0x439BB ruft nur den Lader 0x2EF1B (Runde 13, `re-runde-13.md` S-K6-a, R-K5). Außerdem tauscht voll der
  Saisonwechsel 0x562F2/0x5636D (Auf-/Abstieg,
  Oberliga ↔ ligalose Vereine). Die Teamwerte wandern also immer mit dem Verein – keine Quelle der Streuung.
- **Play-off-Format 0x31934/0x315E9 (Ov 0xE8)** belegt das Remake-Format: Setzung 1–8, 2–7, 3–6, 4–5 aus 0x5CCB; „best of
  five“ (Spiel `[0xCCA3]` 0 … 4, Siege DGROUP 0xCCA4 + Paarung, nach drei Siegen Ergebnis 100 = nicht ausgetragen);
  nach jedem Spiel tauschen Heim und Gast (0x31505); drei Runden (8 → 4 → 2, Paarungszahl 8 − 4·(Runde > 0) − 2·(Runde > 1),
  0x31BB0); Ergebnisse ≥ 30 = Penaltysieg; Meister DGROUP 0xD0E6 (0x31CBF). Damit ist auch das Format keine Erklärung;
  die Marke `[APPROX]` in `Playoffs.cs` kann die Spur `wettbewerbe-komplett` auf `[ORIGINAL]` stellen. Halbfinale und Endspiel
  nach Tabelle DGROUP 0x144 (0x31BED–0x31C07: 1–8 gegen 3–6, 2–7 gegen 4–5, Heimrecht beim kleineren Platz) seit #676 `[ORIGINAL]`.

**Stand #204:** Mit Lader, festen Teamwerten, Vereinstausch und Play-off-Format stimmt die Abbildung der Computervereine
mit dem Original überein; die stärkere Streuung der Meister im Video (§13) bleibt unerklärt. Vor weiteren Änderungen die
Meisterfotos der Reihe `folgen` neu zuordnen: Die Fundliste „Meisterfoto“ (`referenz-videos.md`) mischt „Deutscher
Meister“, „DEB Pokalsieger“ und „Europacupsieger“, und Folge 8 beginnt ein neues Spiel (zwei Laufbahnen statt einer).
Erst wenn das Video nach der Neuzuordnung weiter streut, ist die Minutenrechnung (§3/§4) der nächste Kandidat.
**Nachtrag:** Ursache gefunden und umgesetzt, siehe §15.

## 15. Wanderung der Teamwerte 0x9D1B (Spur spielberechnung-komplett, Teil 1; #204)

**Videobefund** (Reihe `folgen`, Referenz-Repo): Die Spielstärke der Computervereine ändert sich über die Saisons stark und in
beide Richtungen. Paarungen und Vereinsinfo zeigen „Stärke“ = (Kondition + Technik + Form beider Teile) / 6 aus den
Vereinsfeldern 0x18–0x1D (Datei 0x4F1F1), der Bildschirm „Stärken“ die Felder selbst (0x51206). Beispiele Bundesliga
(Kondition/Technik „Gesamt“): Nürnberg (Datenbank 63) 87/85 (F18), 80/92 (F20), 89/89 (F24), 64/80 (F28); Krefeld (88)
73/68 (F18), 98/84 (F24); Kempten (36) und Straubing (20) als Aufsteiger 92/86 und 90/80 (F28). Paarungen F21 1. Spieltag:
Hedos München Stärke 60, Düsseldorf 65, Nürnberg 77. Schon am Spielstart weichen die Oberligavereine weiter als ± 10 % von der
Datenbank ab (F1: Heilbronn 24 → 33/31; F8: Timmendorf 34 → 17/18). Zum Vergleich: In `FILE01.HOK` (neues Spiel, sofort
gesichert) liegen die Werte nahe der Datenbank (Düsseldorf 94/94 82/89, Nürnberg 57/58 58/62) – der Spielstand entsteht vor
dem ersten Spieltag.

**Meister im Video** (Meisterfotos „Deutscher Meister“ und Bildschirm „Saisonende“; die Fundliste mischt Meister, DEB-Pokal-
und Europacupsieger): Spiel 1 (Folgen 1–7, fünf Saisons) Düsseldorf F2, Preussen Berlin F3, Freiburg F4, F6, Schwenningen F7;
Spiel 2 (ab Folge 8, 13 Saisons) Düsseldorf F9, Nürnberg F10, Preussen Berlin F12, F13, Köln F14, Landshut F16, Köln F17,
Freiburg F19 (§13 nannte Köln), Köln F21, Nürnberg F23, F25, Ratingen F27, F29. Abstiege starker Vereine: Köln (90) nach
Saison 3 in Spiel 1, Mannheim (84) nach Saison 4, Hedos München (86) nach Saison 2 in Spiel 2, Düsseldorf (92) und Hedos
München nach Saison 12. Zusammen: acht Meister in 18 Saisons, Düsseldorf und Köln 5 von 18, Meister mit Datenbank ≤ 72
8 von 18.

**Fundstelle 0x9D1B** (Basis 0x62C:0x1C5B, Argument Modus, jedes Argument ≠ 1 gilt als 3), je Vereinsplatz
i = 0 … [0x6AD] + 11 in dieser Reihenfolge:

- Vereine (i < [0x6AD] = 52): Liga L = 0x34FD5(i) / 2 (1 Bundesliga, 2 Zweite Liga, 4 sonst → 0, 1, 2); Grenzen DGROUP
  0x125 + 2L: (69, 94), (39, 73), (15, 43). r = lrandom(0, 2·Modus); b = 3 bei Modus 3, sonst 1; Schritt
  s = lrandom(0, 2r + 2b) − (Modus + b), also −6 … 2r bzw. −2 … 2r, im Mittel 0.
- Kondition 0x18 + k (k = 0, 1): + lrandom(s − 1, s + 1). Technik 0x1A + k: lrandom(0, 2) ziehen, bei 0 oder Modus 3
  + lrandom(s − 1, s + 1). Beide mit 0x120:0x52 begrenzt: Modus 3 auf die Liga-Grenzen, Modus 1 auf 1 … 99.
- Alle Plätze einschließlich Nationalteams: Form 0x1C + k = lrandom(45, 55) für k = 0, 1, 2 (Schleife bis k < 3, überschreibt
  auch Feld 0x1E).

**Aufrufe:** Spielstart 0x43A73 Modus 10 (nach Ligamischung 0x43A17 und Spielvorbereitung 0xD8:0x2F); Saisonwechsel 0x56448
Modus 10 (nach Auf-/Abstieg 0x562F2/0x5636D); Tageslauf 0x54C9F Modus 1, wenn der Tageszähler 0x3B7 ≡ 0 oder 4 (mod 7) ist
(0x5525A–0x55292, davor 0x20:0xAC5). Europapokal-Gegner (Index > 64) wandern nicht.

**Weiterer Schreiber 0x58DC** (Basis 0x238:0x175C, gerufen aus dem Transferbildschirm 0x46FB3/0x47483, wenn ein Spieler an einen
Computerverein < 0x40 geht): Mittel a = (Kondition + Technik eines Teils) / 2 (der Teil kommt aus dem Register `si` des
Aufrufers), d = clamp((Spielerstärke − a + 10) / 3, 1, 10), Teil random(0, 1), Kondition und Technik dieses Teils
+ random(d − 1, d + 1). **Umgesetzt in Runde 11** (Spur `kader-transfer` Teil 3, PR #434; `Core/Transfer.ComputerClub.cs`
`ComputerClubTransferRules.StrengthenBuyer`, Anschlüsse in `AcceptUnsolicitedOffer` – Aufruf 0x46FB3, nur Verein < 0x40 –
und `TransferMarket.Accept` – Aufruf 0x47483; Tests `KaderTransferTeil3Tests`): Verkäufe des Managers an Computervereine
stärken den Käufer. `[APPROX]` Das Mittel a liest das Original über das nicht gesetzte Register `si` (Mauszeile des
Transferbildschirms, beliebige Bytes); das Remake nimmt (Kondition + Technik beider Teile) / 4, ohne Zufallszug. Ohne
gezogene Teamwerte (vor dem ersten Spiel) keine Wirkung.

**Remake** (`MatchEngine.DriftTeamValues`, `TeamValueDrift`): Spielstart beim ersten Spiel nach dem Lader (`EnsureTeamValues`),
Saisonwechsel in `OnNewSeasonManager` (nicht im Modus „nur Weltkugel“, `[APPROX]`), Tageswechsel in `OpenClubSeasonDay` (vor dem Menü,
#688) an Tagen mit Zähler `DayCounter.Of(Tag)` ≡ 0, 4 (mod 7) (#675). `[APPROX]` Reihenfolge nach
Vereinsindex; Kondition und Technik bleiben als Abweichung vom Datenbankwert gespeichert, die Grenzen gelten für
Datenbankwert + Abweichung (die Kaderkorrektur `TeamValue` kommt im Spiel hinzu). Kein neues Schemafeld.

**Wirkung** (`TeamValueDriftTests.ChampionSpread_LikeVideo`, Originaldaten, Managerverein 42 aktiv, 12 Seeds × 4 Saisons;
Tore aus `SpielstaerkeManagerTests.SeasonDistribution_OriginalDatabase`). Spalte „Runde 11“: Stand der Spur
wettbewerbe-spiel bis Teil 5 (Branch `paket/wettbewerbe-spiel-5`, auf dem Integrationsstand von Runde 9/10; R9S-06, #354).

| | vorher (§13) | mit Wanderung | Runde 11 | Video (18 Saisons) |
|---|---:|---:|---:|---:|
| verschiedene Meister | 6 | 14 | 16 | 8 |
| Düsseldorf + Köln | 33 von 48 | 10 von 48 | 5 von 48 | 5 von 18 |
| Meister mit Datenbank < 80 | 0 | 19 von 48 | 26 von 48 | 8 von 18 (≤ 72) |
| Bundesliga Tore je Spiel / Unentschieden | 6,28 / 11,6 % | 6,35 / 10,7 % | 6,24 / 11,0 % | – / 11,4 % (F21, 44. Spieltag) |

Einordnung: Das Remake streut die Meister eher stärker als das Video; in 18 Saisons erwartet man aus der Verteilung der 48
Titel etwa 10 verschiedene Meister (Review R9S-06). Die Abnahmegrenzen des Tests (D + K ≤ 23, schwache ≥ 4, ≥ 6 Meister)
halten. Diagnostisch aus dem Review (Integration Runde 9, 6 Seeds × 2 Saisons): 2. Liga 7,44 Tore / 8,8 %, Oberliga
8,11 / 8,7 % (Video Oberliga 8,0 / 7,9 %); Strafminuten 26–27 je Spiel ohne Videowert.

## 16. Strafen in der Managerpaarung (Spur spielberechnung-komplett, Teil 2; R8W3I-03 bis -06)

Geprüft an `em.exe` (`ndisasm -b 16`), Tests `SpielberechnungKomplett2Tests`.

- **R8W3I-03, Zahl der Züge 0x378C5.** Der Zähler [bp-0xb] beginnt bei 0; 0x378ED springt vor dem ersten Zug zur Prüfung
  0x37A2A (`mov al,[bp-0xb]; inc byte [bp-0xb]; cmp al,0x96; jnc`), erst dann folgt der Zug 0x378FC. Also höchstens
  **150** Züge random(0, n); das Remake zog 151-mal (`MaxPenaltyDraws`).
- **R8W3I-04, kein Spieler (100).** 0x35B33 `cmp [bp-0x1],0x64` → 0x3583B: Rückgabe 0 ohne Anzeige (0x130:0x48 an
  0x35C73) und ohne Kettenstrafe. Strafbank, Feldspieler (0x35941/0x35963) und Strafminuten des Vereins (0x35986) sind da
  schon gesetzt. Remake: kein `MatchEvent(Penalty)` mehr, Vereinsminuten und Strafbank bleiben.
- **R8W3I-05, Managerwert.** 0x35897 liest das Byte DGROUP 0x974B + 0x1AF · (Vereinsfeld 0x16), geschrieben nur von 0x952C
  mit Schalter ≠ 0 (0x9672): clamp(d/5 + 20, 0, 40) bzw. **100 bei weniger als neun** aufgestellten und eingesetzten
  Spielern (Zählung 0x388F6 über Stub 0x149D:0x89, 0x9698–0x96A8). Schalter 1 nur im Tageslauf 0x54C9F, Schleife über die
  Manager 0 … [0x386] − 1 und den Bundestrainer (5) bis Tag 0x3F (0x54F9C–0x54FBF), also vor den Spielen des Tages; die
  Aufrufe 0x54DAA und 0x555D6 haben Schalter 0 (nur Teamwerte). `[APPROX]` Das Remake rechnet den Wert beim Spiel aus
  demselben Stand (`LiveTeamOf`, „weniger als neun“ = `GameState.IsShortHanded`); der Zweig 100 war vorher toter Code.
- **R8W3I-06, Nationalteam des Bundestrainers.** 0x35AB8–0x35B2C ruft 0x378C5 für jeden Verein mit Vereinsfeld 0x16 ≠ 0;
  das Nationalteam hat 0x16 = 6 und zieht aus den Kaderplätzen von Manager 5 (DGROUP 0x5DC3 + 5 · 0x618). Unter 8 fitten
  Kaderspielern keine Strafe (0x358FF). Remake: Strafspieler aus dem Kader des Bundestrainers, die Strafe zählt beim
  gezogenen Spieler (die Gutschrift zog vorher einen Zufallsspieler). Seit §20 spielt das Land ganz mit diesem Kader
  (`LiveTeam.Squad` aus `MatchEngine.NationalSquad`). `[APPROX]` Kaderreihenfolge; Managerwert 20 (das Original liest
  0x974B + 6 · 0x1AF hinter den sechs Managersätzen, nicht geprüft).
- **Verdacht 0x952C → 0x9567 widerlegt.** 0x9567 ruft über Stub 0x149D:0x8E die Funktion **0x38A36** (Verein), keine
  automatische Aufstellung: sie zählt mit 0x388F6 die Spieler der Reihen auf dem Eis (Torhüter, Abwehr, Sturm), zieht die
  Strafbank ab (0x35540 mit Schalter 0), schreibt DGROUP 0xCC35 + 2 · (Feld 0x16) und +1 sowie Vereinsfeld 0x32
  (Feldspieler) und setzt Bit 3 von Feld 0x2C (Torhüter draußen), wenn sechs Feldspieler auf dem Eis sind. Das `[APPROX]`
  in `PlayFriendly` (Automatik ohne Länderwerte) bleibt: 0x952C bildet keine Aufstellung (Datei der Spur
  wettbewerbe-komplett, nicht geändert).
- **Testlücke R8W3I-10** (`Forfeit_LeagueMatch_UpdatesDayForm`): `Forfeit_LeagueMatch_LoserDayFormDrops` prüft die
  Tagesform des Verlierers (0x4F527).

**Wirkung:** Paarungen ohne Manager unverändert. Neue Abdrücke (Länderspiele, ein Zug weniger ohne passenden Spieler):
`RngTwinProbeTests` (3 von 6), `GameModeTests.Mode4_ReferenceValues_Unchanged` (ab Saison 5),
`SpielberechnungAnschlussTests.FourSeasonChanges_WorldOnly` (WM-Ergebnisse, Pokalquelle).


## 17. Verletzungen im Spiel und Torschütze (Spur spielberechnung-komplett, Teil 3)

Geprüft an `em.exe` (`ndisasm -b 16`), umgesetzt in `Core/MatchEngine.Injuries.cs`, Tests `SpielberechnungKomplett3Tests`.

**Verletzungen 0x38B8F** (Stub 0x149D:0x93; gerufen je Minute der Liga 0x37F06 bzw. der Pokale 0x38304 vor den
Torchancen, wenn einer der beiden Vereine Vereinsfeld 0x16 ≠ 0 hat; Argumente (…, Heim, Gast, 0, 1)):

1. 0x38A36 für Heim und Gast (Feldspieler neu zählen, §16). `[APPROX]` nicht umgesetzt: das Remake führt die Feldspieler
   über die Strafbank; ein verletzter Spieler fehlt nur in den Reihenwerten (0x98F8) und den Spielerwahlen.
2. Schleife m = 0 … 5: Satz aktiv (DGROUP 0xA729 + m), m < Zahl der Manager [0x386] oder m = 5 (Bundestrainer, nur wenn
   dessen Verein DGROUP 0x9EDA ≤ 0x3F – also nie mit dem Nationalteam); Verein des Managers (0x966F + 0x1AF · m) ist Heim
   oder Gast.
3. Managerwert des Managers am Zug (0x974B + 0x1AF · [0xA70E]) = 100 → Ende der Funktion; ebenso unter 4 eingesetzten
   Spielern der Aufstellung (0x120:0x66(m, 1) = 0x4BE17 Modus 1: +0x0A ≠ 0 und +0x15 ≠ 0). `[APPROX]` Manager am Zug =
   der jeweilige Manager.
4. v = clamp(60 − M, 0, 40) mit M = 0x974B + 0x1AF · m (clamp 0x4BD35); ohne Schalter DGROUP 0x4E80 weiter nur bei
   random(0, 20 · v + 200 · [0xED8] + 200) = 0 (0xED8 = 5 − Level).
5. Spieler 0x378C5(m) (Reihe auf dem Eis, §13); keiner (100) → nächster Manager.
6. Szene 0x130:0x7F (= 0x53C57) mit Ereignis 4 → 0x5411E: Verletzung **0x05B1A**(Kaderplatz, Text, 1), Anzeige nur mit
   Live-Anzeige ([0x5A56]). Danach Versicherung **0x07089**, wenn Kaderplatz +0x30/+0x32 ≠ 0: Zahlung (+0x30) · 4 auf
   DGROUP 0x97EA + 0x1AF · Manager („Ein Fall für die Versicherung“) – Anschluss Spur transfer-vertraege-komplett (Teil 2).

**Verletzung 0x05B1A** (Basis 0x238 + 0x199A; auch aus dem Tageslauf 0x07169 an 0x74A9 mit Schalter 0, Wurf
random(0, 30 · [0xED8] + x + 100) = 0 an 0x7479 – außerhalb des Spiels, nicht Teil dieser Spur):

- Form im Pool (Spielerfeld 0x17) > 20 → − random(10, 15).
- Art t = random(0, 27 − 6 · Schalter), wiederholt, bis random(0, 15) ≤ DGROUP 0x1B9[t] (Häufigkeit 1 … 7).
- Dauer +0x0D = random(DGROUP 0x181[2t], 0x182[2t]) **Wochen**; Art in DGROUP 0x483B (0, 1, 4, 7, 9, 10, 11, 14, 19) mit
  random(0, 2 · [0xED8] + 10 − min/4) = 0 → 0xFF (heilt nie).
- +0x09 | 2 (verletzt), +0x17 = Art (Namen DGROUP 0xA00), +0x0A = 0 (vom Eis), Text „Verletzt: …“.
- **Heilung 0x7C12** (Tageslauf 0x07169 an 0x72CB für Marktplätze der Manager und an 0x7B4E für ihre Kaderplätze):
  verletzt, +0x0D ∉ {0, 0xFF} und (Tageszähler 0x3B7 − [0x6AF]) mod 7 = 0 → +0x0D − 1; Status frei bei +0x0D = 0.
  `[ORIGINAL]` [0x6AF] = 0 (DGROUP-Startwert, kein Schreiber; Spiel- und Saisonstart setzen 0x3B7 := [0x6AF], Datei 0x43A93/
  0x565A7). Der Zähler steht an den Spielen von Tag d auf d + 1 (`DayCounter`, Review #674/#675). Remake: `Player.InjuredGames`
  zählt die Wochen, Art nicht geführt.

**Torschütze 0x541D9** (Ov 0x130; Stub 0x14E1:0x84). In der Managerpaarung (Byte [bp−9] ≠ 0x1E in 0xA484) ruft **jeder
Abschluss** – Tor ([bp−4] = 1) oder nicht (0), nur der Abbruch in der Verlängerung 0xA64E nicht – die Szene 0x53C57; ohne
Torszenen ([0x226] = 0) geht sie gleich nach 0x5406C → 0x541D9(schießender Verein, Art, 0):

- erster Manager m = 0 … 5 mit diesem Verein (0x541D9 prüft die Zahl der Manager nicht; der Bundestrainer m = 5 mit
  dem Nationalteam, wenn DGROUP 0x9EDA das Land hält – gedeutet aus 0x38BE9); mehr als 3 eingesetzte
  Spieler der Aufstellung, sonst nichts;
- Schütze = 0x386D1(m, 0, Art), Vorlage = 0x386D1(m, 1, 1) wiederholt, bis ungleich (ohne Grenze);
- nur bei Art 1: Vorlage Spielerfeld 0x1F + 1; Tor Kaderplatz +0x22 + 2 · [0xD0BF] (Wort, < 0xFF) und +0x03 + [0xD0BF]
  (Byte), bei [0xD0BF] = 0 auch Spielerfeld 0x1B (< 0xFF). [0xD0BF] setzen 0x30EA6/0x31A1E (0), 0x313C5 (2), 0x35E72 (1).

**Spielerwahl 0x386D1**(m, v, s) (Stub 0x149D:0x84): 0xFE ohne eingesetzte Spieler der Aufstellung; n = belegte Plätze − 1
(Modus 0); höchstens 250 Züge Platz random(0, n): Feldspieler (Pool +0x18 ≠ 0), eingesetzt (+0x0A, < 12), in der
Managerpaarung (0x130:0x4D) in der Reihe auf dem Eis ((+0x15 & 0x7F) − 1 = Vereinsfeld 0x2E + 2 · Sturm). Gewicht
w = (v oder s) ? Technik · 3 / (5 − 3v) + Kondition / 5 + Form / 5 : random(120, 170); + Gruppe (0x238:0 = Rollenwert / 25,
höchstens 2) · (72 − 17v) + Tore (+0x03) · 46 + 700 im Sturm; Treffer bei random(0, 3000) < w. Ohne Treffer der erste
Feldspieler der Plätze 0 … n − 1.

**Remake:** `OriginalMinutes.Injuries` (vor den Torchancen jeder Managerpaarung), `PlayerInjuries.Inflict/Heal`,
`GameState.HealInjuries` (Tageslauf), `OriginalMinutes.ScoringPlayers/ScorerIndex` (nach jedem Abschluss der
Managerpaarung); Schütze und Vorlage stehen im Torereignis, `CreditLineup` zählt sie (statt 0x5028 nach dem Spiel).
`[APPROX]`: Tore aller Wettbewerbe in `Player.Goals` (Gewicht +0x03 = Saisontore samt Spiel), Vorlagenschleife mit Grenze
1000, Nationalteam zieht aus dem Kader des Bundestrainers (`LiveTeam.PenaltySquad`, §16).

## 18. Über-/Unterzahl-Zähler und Länderwerte im Stärkevergleich (Spur spielberechnung-komplett, Teil 4)

Geprüft an `em.exe` (`ndisasm -b 16`), umgesetzt in `Core/MatchEngine.PowerPlay.cs` und `Core/StatisticsStrength.cs`,
Tests `SpielberechnungKomplett4Tests`. Regeln, Zustand, Saisonwechsel und Anzeige der Zähler: Spur statistiken-verein
(`Core/StatisticsClub.cs`, `PowerPlay`).

**Zähler** (Managerfelder DGROUP 0x97D2 + 2 · k, Satz 0x94AC + 0x1AF · Vereinsfeld 0x16 = Manager + 1):

- **Tor** (Abschluss 0xA484): Feldspieler beider Vereine zu Beginn des Abschlusses ([bp−0x11]/[bp−0x12], 0xA491–0xA4BC).
  Nach dem Treffer (0xA8AC): hat die verteidigende Mannschaft Spieler auf der Strafbank, merkt sich 0xA8F9–0xA91D, ob
  einer der Vereine einen Manager hat und beide gleich viele Feldspieler haben; dann endet eine Strafe (0x35691(…, 0, 0)).
  Steht die verteidigende Mannschaft danach in Überzahl, zählt der Manager des Schützen k = 5 und der der verteidigenden
  Mannschaft k = 4 (0xA939–0xA9A2). Danach die Torzähler k = 0 … 3 für Heim- und Gastmanager (0xAA2C–0xAAEF,
  `PowerPlay.Goal`), außer bei zusammen 11 Feldspielern (5 gegen 6, 0xAA18–0xAA29).
- **Strafe** (0x35818, nach Strafbank, Vereinsminuten und Kettenzug, vor der Spielerwahl 0x378C5): ist
  |eigene − gegnerische Feldspieler| = 1 (`0x120:0x61`), zählt der Gegnermanager k = 4 (0x35A84) und der eigene k = 5
  (0x35B18). Übernommen: auch 4 gegen 3 nach einer Strafe der Mannschaft in Überzahl zählt. Liefert die Kettenstrafe 0
  (keine Strafe), wird nur der zuletzt gezählte Wert ([bp−0xa]/[bp−0xb]) zurückgenommen (0x35C7B–0x35CB2).
- Kein Zufallszug: Ergebnisse, Zufallsverbrauch und Abdrücke ändern sich nicht. Gezählt wird in jedem Spiel der
  Minutenrechnung mit Manager (Liga, Play-offs, Pokale; 0xA484 prüft keinen Wettbewerb).
- `[APPROX]` Nationalteam: das Original zählt für Vereinsfeld 0x16 = 6 in den Satz des Bundestrainers (Manager 5); das
  Remake führt ihn nicht (`LiveTeam.PowerPlayCounts` = null), die Rücknahme trifft wie im Original den zuletzt gezählten
  Verein.

**Länderwerte (#193) im Stärkevergleich.** 0x952C mit Manager 5 schreibt die Teamwerte des Landes (Vereinsfelder
0x18–0x1D, Kondition, Technik, Form) aus der Aufstellung des Bundestrainers: im Tageslauf 0x54F9C–0x54FBF (Schalter 1)
jeden Tag, solange es einen Bundestrainer gibt (`[0x9EDA]` < 0x3F), und nach jedem Freundschaftsspiel (0x555D6,
Schalter 0). Der Stärkevergleich 0x51206 liest diese Felder. Beleg: DOSBox 27–29 → 41–43, Deutschland +3/+3/+5 nach
drei Freundschaftsspielen (`docs/dosbox-befunde.md`). Remake: `StrengthTable` zeigt für das Land des Bundestrainers mit
eigener Aufstellung `MatchEngine.LineupTeamValues(coach.Formation)` samt Form. `[APPROX]` aus der aktuellen Aufstellung
statt dem Stand des letzten Tageslaufs; ohne eigene Aufstellung bleiben die Länderwerte (wie in `PlayFriendly`).

## 19. Penaltyschießen nach der Verlängerung 0x3901B (Spur spielberechnung-komplett, Teil 5)

Geprüft an `em.exe` (`ndisasm -b 16`), umgesetzt in `Core/MatchEngine.Shootout.cs`, Tests `SpielberechnungKomplett5Tests`.

- **Aufruf:** 0x3901B ist Stub 0xF0:0x9D (Stubsegment 0x149D, Overlay 0xF0 ab Datei 0x35540 + 0x3ADB). 0x32AA3 geht die
  Paarungen der Zeilen DGROUP 0x5C4B (32 Byte je Zeile, Heim-/Gastbyte, Vereine 0x5CFB/0x5CFC) durch: Zeile 1 markiert
  unentschiedene Spiele mit Heim + 0x1E für die Verlängerung, Zeilen 2/3 vergleichen Heim − 30 · Schalter mit Gast; mit
  Schalter ≠ 0 folgt für ein noch unentschiedenes Spiel 0x3901B(Zeile, Paarung, …) (0x32DE3–0x32DF5). Ist einer der
  Vereine zum Zusehen markiert (Vereinsfeld 0x34 Bit 7, gesetzt über 0xAE2B), gilt die Live-Anzeige ([0x5A56] = 1).
- **0x3901B:** zuerst random(0, 1) (0x39028, nur die Anzeige wertet ihn aus). Ohne Bit 7 bei beiden Vereinen
  (0x390CB/0x390FA): Heim random(2, 5), Gast random(2, 5), wiederholt bis ungleich (0x39105–0x39179). Mit Bit 7 die
  Anzeige „Penaltyschiessen“ mit eigenen Zügen (0x3917E–0x3976F, nicht gelesen). Am Ende (0x39770–0x397B3) kommen die
  Treffer auf den Spielstand: Heim = (Heimbyte − 30) + Treffer + 30, Gast + Treffer – der Endstand ist Gleichstand +
  Penaltytore, die Marke + 30 bleibt („n.P.“).
- **Remake:** `MatchEngine.PlayWithEvents` ruft `OriginalMinutes.Shootout` für jedes K.-o.-Spiel des Minutenwegs, das nach
  der Verlängerung unentschieden ist, und setzt `MatchReport.ShootoutGoals`/`ShootoutHomeWins`; `KnockoutMatch.Play`
  übernimmt den Sieger (vorher `[APPROX]` `NextDouble` nach Vereinsstärke). Der schnelle Weg der Pokale bleibt bei
  0x376F0 (§3, random(0, 2) je Seite).
- `[APPROX]` Keine Live-Anzeige (Zweig mit Bit 7 nicht umgesetzt); der Spielstand bleibt der Gleichstand, die Penaltytore
  stehen getrennt im Bericht.
- **Zeilen 0x5C4B (A077, Runde 17):** 0x32AA3 läuft nur mit dem K.-o.-Merker von 0x36B5D (Argument 3, Datei 0x371F0); Maske 1 =
  Zeile 0 DEB-Pokal (Datei 0x5504A), Maske 4/0xC = Zeile 2 Play-offs (0x31A28/0x31A3C; Zeile 3 der Aufstiegsrunde in Maske 0xC
  übersprungen, 0x32D13), Maske 8 = Zeile 3 WM-Endrunde (0x3101C). Europapokal, WM-Gruppe und Freundschaftsspiel rufen ohne
  K.-o.-Merker – `[ORIGINAL]` wie `knockOut` im Remake (`WettbewerbeRestTests`).
- **Endstand und Marken (Runde 11, Spur wettbewerbe-spiel Teil 1):** 0x39088 nimmt die Marke ab (Heimbyte − 30), 0x39789
  setzt sie wieder – Endstand Gleichstand + Treffer mit **einer** Marke. Ein Tor in der Verlängerung schreibt 0x38402 Heim + 30
  auf den Stand; im Video stehen danach **zwei** Punkte (`eisbaeren_f44_01330` „4: 3..“ neben „4: 3.“). Die Anzeige 0x32E23 zieht
  je 30 ab und setzt einen Punkt (0x338AB–0x338E3, Pixel über 0x50:0x3B0 bei x + 22 + 2 · i, 0x339BD). DEB-Pokal und WM-Endrunde
  übernehmen das über `CupMatch` (Marken 0/1/2). `[APPROX]` Herkunft des Stands vor 0x38402 (Tabelle DGROUP 0xA0BF, 0x37E2D)
  nicht verfolgt; ob ein Tor in der Verlängerung das Spiel sofort beendet (0x38418 → 0x38524), ist nicht geprüft – das Remake
  spielt beide Abschnitte (§9).

## 20. Stärke der Nationalteams (Runde 11, Spur wettbewerbe-spiel Teil 3; Inventur P2 Nr. 2)

Geprüft an `em.exe` (`ndisasm -b 16`); umgesetzt in `Core/CupsNational.Strength.cs` und `Core/MatchEngine.National.cs`,
Tests `WettbewerbeSpiel3Tests`.

- **Keine eigene Formel.** Nationalteams sind Vereinsplätze 52–63; 0x368B1 (Stärke) und 0x977F (Reihenwerte) lesen ihre
  Vereinsfelder wie bei jedem Verein. Der Lader (Datei 0x2F0CC–0x2F120) überschreibt für die Plätze [0x6AD] … [0x6AD] + 11
  Kondition 0x18 + k und Technik 0x1A + k mit DGROUP 0x4C4E − 2 (Reihenfolge K Abwehr, T Abwehr, K Sturm, T Sturm), ohne
  Streuung; Form 0x1C + k = random(45, 55) und Tagesform 0x17 = random(47, 53) wie bei den Vereinen. Remake:
  `GameState.NationalTeamValues` → `MatchEngine.NationalLiveTeam` (vorher: ein Wert (K + T) / 4 für alle vier Felder,
  Form und Tagesform 50).
- **Land ohne Manager** (auch Deutschland ohne Bundestrainer, Vereinsfeld 0x16 = 0): Einsatz und Angriff aus der Automatik
  der Spielvorbereitung 0x8365, unabhängig von den Modi (R8-B04).
- **Land des Bundestrainers** (0x16 = 6): Managerverein mit den Kaderplätzen von Manager 5 – Reihenwerte 0x98F8 aus den
  eingesetzten Spielern aller vier Blöcke (Reihenwechsel 0x8EF2/0x91FF mit vierter Reihe), Technik + Länderspiele / 8
  (höchstens 99), Taktik des Nationalteams (Einsatz in 0x98F8, Taktik 0x939A, Reihenwechsel), Energie 0x87F7, Strafspieler
  0x378C5. Vorher: Mittel der 16 Plätze (K + T) / 2 plus Taktikzuschlag `EffortStrengthBonus`, ohne Kader gespielt.
- **Automatik 0x8365:** doppelte Stärken 0x368B1(Verein, 1, 0, 0, 1) (Datei 0x83AD/0x83CA), Grenze 0x19 (0x840E) –
  `GameState.NationalMatchStrength` (Reihe 0, Energie der Spielvorbereitung, Minute 0, gleich viele Feldspieler).
- **„Stärke:“ und Stärketabelle:** 0x952C mit Manager 5 schreibt die Felder 0x18–0x1D des Landes aus der Aufstellung des
  Bundestrainers (Tageslauf jeden Tag, nach Freundschaftsspielen); `NationalInfoStrength` nimmt sie (A019).
- `[APPROX]` Einsatz 0x26 in `NationalMatchStrength`: Taktik beim Stand 0:0 gegen gleich starken Gegner (Original: zuletzt
  gesetzter Wert). Länderwerte mit Bundestrainer aus der aktuellen Aufstellung statt dem Stand des letzten Tageslaufs.
  Ohne eigene Aufstellung (passiver Bundestrainer) spielt die Automatik `NationalCoach.Lineup` mit drei Reihen (Original:
  ohne Aufstellung kein Spieler auf dem Eis, nicht nachgebaut). „5 − Level“ aus dem Level des Managers, der Bundestrainer
  ist. Die von 0x554CF für Deutschland ausgewürfelte Form verwirft das Remake (die Wanderung zieht sie zweimal je Woche neu).
  Managerwert der Strafen 20 (A084, DOSBox-Probe offen); keine Verletzungen und Über-/Unterzahl-Zähler des Bundestrainers.
  Tagesform nach WM-Gruppenspielen (0x4F527 auch bei Code 8) weiter nicht nachgeführt.

## 21. Teamwerte in der Stärketabelle 0x51206 (Runde 11, Spur wettbewerbe-spiel Teil 4; Inventur P1 Nr. 5, P2 Nr. 16)

Geprüft an `em.exe` (`ndisasm -b 16`), umgesetzt in `Core/StatisticsStrength.cs`, Tests `WettbewerbeSpiel4Tests`.

- **Gelesen** werden je Teil k = 0 (Abwehr), 1 (Sturm) nur Vereinsfelder: Kondition 0x18 + k (Datei 0x5149A), Technik
  0x1A + k (0x514BD), Form 0x1C + k (0x514E3), Vereinszeiger DGROUP 0x9657 mit Faktor 0x3D; der Trendbalken liest
  Vereinsfeld 0x17 (Tagesform, 0x51900). Spielerwerte gehen nicht ein (A200, A201).
- **Remake:** Computervereine `MatchEngine.ClubFieldValues` (Teamwert + Streuung des Laders bzw. Stand der Wanderung §15,
  Form des Laders; dieselben Werte wie `LiveTeamOf`). Managervereine aus der Aufstellung (`LineupTeamValues`, 0x952C im
  Tageslauf 0x54F9C–0x54FBF für jeden Manager). Länderteams: Kondition/Technik aus 0x4C4E (`NationalRatings`), Form und
  Tagesform aus den Feldern des Vereinsplatzes; Land des Bundestrainers aus dessen Aufstellung (§18). Trend =
  Tagesform (47 … 53).
- `[APPROX]` Managervereine aus der aktuellen Aufstellung statt dem Stand des letzten Tageslaufs; vor dem ersten Spiel
  eines Spielstands (Teamwerte noch nicht gezogen) Streuung 0, Form und Tagesform 50.
- **Finanzen (Stand Runde 12, Spur geld Teil 2):** `StrengthTable.TeamValues` liest jetzt dieselben Vereinsfelder wie die
  Stärketabelle; die Zuschauerstärke (`SpectatorRules.Strength`, Datei 0x4D316) ist das Mittel der sechs Felder 0x18–0x1D, die
  Monatseinnahme (`FinanceMonthly.TeamValueSum`, 0x3E657) las sie schon. Kein Zufallszug mehr oder weniger; Meister und
  Zufallszustände bleiben gleich, nur die Konten ändern sich (Abdrücke `GameModeTests.Mode4Reference`, `RngTwinProbeTests`).
- **Über-/Unterzahl in der Vereinsstatistik (A349):** Der Anschluss der Zähler aus §18 an „Statistiken → Verein“ war
  schon vorhanden (`Manager.PowerPlayCounts`); geprüft über die Zeitleiste (`WettbewerbeSpiel4ScreenTests`), der
  veraltete `[APPROX]`-Kommentar in `ClubStatisticsScreen` ist ersetzt.

## 22. Taktik und Feldspieler (Runde 11, Spur wettbewerbe-spiel Teil 5; Inventur P2 Nr. 1, G6)

Geprüft an `em.exe` (`tools/reverse-engineering/disasm.py`), Tests `TaktikTeil5Tests`.

- **Minuten-Vorgabe (A177, G6):** je Teil zusammen höchstens 20 Minuten (Pfeilgrenze Datei 0x4889F–0x488CE, 0x489C3:
  Feld < 20 − übrige Reihen). Beim Verlassen des Taktikbildschirms rechnet 0x48E62 (Datei 0x48EEA–0x4901D) um:
  Reihe 1–3 ⌊Min/2⌋ | (Min & 1), Reihe 4 ⌊Min/2⌋, dann von Reihe 4 rückwärts im Ring bis Summe 8 (Summe > 8: −1 nur bei
  Werten > 1; sonst +1, bei Werten ≤ 1 nicht in Reihe 4). Modus 4 von 0x8EF2 zählt gegen diese Werte (Datei 0x09140).
  Remake: `Tactics.MinutesLimit`, `Tactics.MinuteQuota`; `ChangeLines` Modus 4. Beispiele 7/7/6/0 → 3/3/2/0,
  5/5/5/5 → 3/2/2/1, 0/0/0/0 → 2/3/3/0.
- **Minutenzähler DGROUP 0x4926 (A090):** nur 0x8EF2 liest und schreibt ihn (Datei 0x90F9, 0x911C, 0x9166); kein
  weiterer Zugriff, kein Blockzugriff. Er läuft im Original über das Spielende hinaus weiter. `[APPROX]` Das Remake
  beginnt jedes Spiel bei 0.
- **„MANUELL“ (A176):** 0x939A schreibt Vereinsfeld 0x26 (Einsatz) nur, wenn der Modus nicht 2 ist (Datei 0x9516), den
  Angriff in 0x2C nur bei Modus < 2 (Datei 0x94E2). Bei „MANUELL“ bleibt im Spiel der Wert vor dem Spiel (Grundwert +
  „Unentschieden“, 0x48E62 Schritt 4) – ohne Nachführen nach dem Spielstand und ohne Energieabzug. `FollowTactics`.
- **Kein Einsatz-Zuschlag (A178, A182):** der Einsatz wirkt nur über Vereinsfeld 0x26 in 0x368B1/0x977F und 0x87F7;
  `Tactics.EffortStrengthBonus` (±3 um 80) entfällt, `MatchStrength` (nur noch `MatchEngine.Play`) ohne Zuschlag.
- **Eingesetzt (A168):** Der Kader der Managerpaarung (`LiveSquad.Create`) setzt +0x0A wie der Verein (`Club.IsDressed`
  und spielfähig). Nicht Eingesetzte zählen weder in den Reihenwerten (0x98F8) noch in der Energie (0x87F7). Ohne
  Zufallszug-Änderung bei der automatischen Auswahl (alle Abdrücke unverändert).
- **Feldspieler 0x38A36 (A089, A070):** 0x388F6(Verein, Manager, Modus, Teil) zählt
  eingesetzte Kaderplätze des Teils (+0x15 Bit 7): Modus 0 Torhüter (Gruppe 0x04180 = 0, Reihe beliebig ≠ 0; keiner →
  Vereinsfeld 0x2E = 0, Datei 0x38A2A), Modus 1 Verteidiger und 2 Stürmer der Reihe auf dem Eis (0x2E/0x30). 0x38A36:
  Grenze 6 (ohne Torhüter und mit < 6 Feldspielern 5); Torhüter + Abwehr + Sturm + belegte Strafbankplätze (0x35540
  Schalter 0) über der Grenze → Sturm = 3 − Überschuss; DGROUP 0xCC37 + 2·m = Abwehr, +1 = Sturm; Vereinsfeld 0x32 =
  Abwehr + Sturm; 0x2C Bit 3 bei genau 6, sonst gelöscht. Aufrufe im Spiel: je Minute zu Beginn von 0x38B8F für beide
  Vereine (Datei 0x38BA9–0x38BBA) und beim Freiwerden 0x35691 (Datei 0x35810); dazu 0x952C (Datei 0x9567).
  Angeschlossen in Teil 8 (`MatchEngine.RecountSkaters`, `CountOnIce`): zu Beginn von `OriginalMinutes.Injuries` für
  beide Vereine (nur Managervereine mit Kader) und über `PenaltyBox.Released` beim Freiwerden; schreibt
  `SkatersPerPart`, `PenaltyBox.Skaters` (0x32) und `GoaliePulled` (0x2C Bit 3). `[APPROX]` Startwert 2/3 je Spiel,
  Sturm nicht unter 0. Vorher kippten mit dem Anschluss 19 Tests, weil Managervereine ohne automatische Aufstellung mit
  Lücken spielten; mit §23 bleiben nur die Abdrücke (siehe PR Teil 8).

## 23. Automatische Aufstellung 0x426BC/0x42838 (Runde 11, Spur wettbewerbe-spiel Teil 8; A068, A165, A169)

Regel nach `docs/analyse/re-nachtrag.md` §3 (Review #446), Remake `Core/Squad.AutoLineup.cs`, Tests `AutoLineupTeil8Tests`.

- **Einstellung „automatisch aufstellen“** (DGROUP 0x375 + 2 · Manager, Startwert 1): `Club.AutoLineupEnabled`
  (`[APPROX]` je Verein statt je Manager; Schema 23 → 24). Ein Eingriff in der Aufstellung (Platz setzen/leeren,
  ein-/aussetzen) schaltet sie ab (Datei 0x40ADE/0x40DE4); „A“ schaltet sie ein und stellt sofort auf (`[APPROX]`,
  im Original kippt sie der Taktikbildschirm 0x47F4E und stellt dann auf).
- **0x426BC:** ohne Einstellung nichts. Sonst alle nicht eingesetzt, dann Torhüter (1 × 1), Abwehr (3 × 2), Sturm
  (3 × 3) mit 0x42838, Block endet ohne Kandidat; danach Ersatz reihum Sturm, Abwehr, Torhüter bis 22 Eingesetzte oder
  leere Torhütersuche. Ein Feldspieler aus der Ausweichrolle steht auf dem Platz der gesuchten Rolle (`Lineup.Place`);
  aus der Torhütersuche wird er nur eingesetzt (`[APPROX]`, im Original +0x15 = 1).
- **0x42838:** freie, spielfähige Spieler der Rolle; Wert (K + T + F) / 3, Gleichstand → späterer Platz
  (`[APPROX]` Platzfolge = `Club.SortedPlayers`); Ausweichrollen T → A → S, A → S → T, S → A → T; erster freier Torhüter
  als Feldspieler −1, jeder weitere 1.
- **Aufrufe im Remake:** Spielstart (`InitializeManagerSystems`, 0x28165), am Ende von 0x07169 je Manager (`ManagerSquadRun`,
  0x07C07), an Tagen mit Ereignis vor den Zügen (`MainPathEventDayLineups`, 0x54D9E; #688, R688-07), Zuganfang 0x44E22,
  Verletzung im Spiel und Heilung (`Club.RefillLineup`). Nicht angeschlossen: Transferbildschirm 0x465F1,
  Nationalteam (U8, Satz 5) und die Meldung DGROUP 0x124D
  (Oberfläche, Spur kader-transfer).
