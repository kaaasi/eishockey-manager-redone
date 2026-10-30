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

Folge: Der Rechenteil wird ohne Darstellung nachgebaut (`OriginalMatch`, `MatchStatistics` in
`src/EishockeyManager.Core/MatchEngine.*.cs`); die Anzeige folgt in `livespiel`.

## 2. Ablauf einer Spielminute (Liga, 0x37DCF–0x380A4)

Je Paarung (Heim h, Gast g), übersprungen bei gewerteten Spielen (Ergebnisbyte 0x5A):

1. Managerpaarung? (`0x130:0x4D`: Verein in der Liste DGROUP 0x233, gesetzt beim Spieltagsbeginn) – dann bei Mausklick
   das Eingriffsmenü `0x38D96` (Anzeige).
2. Hat einer einen Manager (Vereinsfeld 0x16 ≠ 0): Aufstellung/Reihenfolge `0x38B8F`.
3. Torchancen `0x9F5C(h, g, Minute, Drittelende)` (Abschnitt 3, Minutenform) → Bit 0 Heim, Bit 1 Gast; je gesetztem Bit
   Abschluss `0xA484(Liga, Paarung, h, g, Seite, Minute, …)` (Abschnitt 4). Steht im Ergebnisbyte schon 0x1E, wird nicht
   neu gezogen.
4. Strafbank herunterzählen `0x874D` für beide.
5. Nur Managerpaarungen: Energie `0x87F7`, Seiten zufällig tauschen (random(0, 1)), in ungeraden Minuten Taktik der
   Computervereine `0x80CB`, Reihenwechsel `0x8EF2` (je Seite), in geraden Minuten Spielstandtaktik `0x939A`.
6. Außer in Minute 60 und 70: **Strafen** `0x35818(h, g, g)` und `0x35818(g, h, h)` – für jede Paarung (Abschnitt 6).

Pokale (0x380D0 ff.) laufen gleich, mit Paarungen aus DGROUP 0x5CFB und Ergebnissen in 0x5B0B/0x5C4B.

## 3. Torbudget und Torchancen 0x9F5C (`OriginalMatch`)

Eingaben je Mannschaft: Stärke S = `0x368B1(Verein, Modus 1, Minute, Feldspielerdifferenz, neu)` (Abschnitt 7),
Feldspieler auf dem Eis F (Vereinsfeld 0x32: 5; bei Strafe weniger, ohne Torhüter 6), Angriff A (Vereinsfeld 0x2C,
Bits 4–6), Torhüter draußen (Bit 3). WM = `[0x12B]` (nur während WM-Spielen 1, Datei 0x30F4E/0x31012).

```
bH = max(0, random(0, A_h) − 5);  bG = max(0, random(0, A_g) − 5)          ; lrandom
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

**Schneller Weg (0x3731F–0x377A3, nur Pokale ohne Manager und ohne Anzeige):** S nach `0x368B1(…, 1, 0, 0, 1)`, +6 für
Vereine (Index < 0x40); Differenz der Drittel |S_h/3 − S_g/3|, höchstens 4; Endstand aus `0x9F5C(h, g, 0, 60)`, dann
Glättung mit lrandom (Datei 0x374EF–0x37696: Tore des Schwächeren verringern/erhöhen, bis Stärke- und Torreihenfolge
passen) und bei K.-o.-Gleichstand Zusatztore random(0, 2) je Seite + 30 als Penaltymarke (0x376F0–0x377A3).
Noch nicht übernommen.

## 4. Abschluss 0xA484 (Torhüter, Tor, Torschütze) – noch nicht übernommen

Teilweise gelesen: Stärke Angreifer `0x368B1(…, Modus 0/2)` gegen Verteidiger, Vergleich mit lrandom(550, 850),
Spielstand-Abhängigkeit (0x1D … 0x3B), WM-Zuschlag `[0x12B]`; danach Ergebnisbyte + 1 (Liga 0xA0BF ff., Pokale 0x5B0B),
Managerverein: Torschütze und Vorlage über `0x541D9` (je Tor genau ein Torschütze und eine Vorlage, Auswahl
`0x386D1`: aufgestellter Feldspieler, in der Managerpaarung nur aus der Reihe auf dem Eis, Gewicht aus Spielerwerten,
Saisontoren · 46, Sturm + 700, Annahme bei random(0, 3000) < Gewicht), Gegentor des Torhüters auf dem Eis `0xA3F7`
(nur Liga). Rest offen (Teil 2).

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

## 6. Strafen 0x35818 (`OriginalMatch.PenaltyMinutes`, Rest Teil 2)

`0x35818(Verein, Gegner, Gegner)` jede Minute für beide Seiten (außer Minute 60/70):

- Strafbankplatz `0x35540(Verein, 1)`: 0 oder 1 frei, sonst keine Strafe.
- Einsatz E = Vereinsfeld 0x26 (Automatik 0x8365: 60 oder 40 + Zuschlag), beim Managerverein + Byte DGROUP
  0x974B + 0x1AF · (Vereinsfeld 0x16) − 20 (Vereinsfeld 0x16 = Manager + 1; Bedeutung offen), begrenzt 0 … 100. **Strafe, wenn random(0, 12) = 0 oder random(0, 110 − E) = 0** (je Minute 1/13 + 12/13 · 1/(111 − E)).
- Managerverein mit weniger als 8 fitten Spielern (`0x120:0x66`): keine Strafe.
- Art: auf Platz 0 mit random(0, 15) = 0 fünf Minuten (Code 13), sonst zwei (Code 2).
- Vereinsfeld 0x32 − 1 (Feldspieler), Strafuhr in 0x33 (4 Bit je Platz). Liga: Strafminuten des Vereins
  DGROUP 0x82A1 + 0x50 · Verein (Wort). Managerpaarung: Spielstrafminuten DGROUP 0xA845 ff. für die Anzeige.
- Managerverein: Spieler `0x378C5(Manager)` (100 = keiner); Feldspieler: Kaderplatz +0 (5 min) bzw. +1 (2 min) + 1,
  Platz auf der Strafbank DGROUP 0xD095 ff., Kaderplatz +0xA = 0 (vom Eis), +0xD = Art. Über-/Unterzahlzähler des
  Managers (+0x177 + 8 bzw. 0x97DC), Reporter `0x130:0x48`.
- Computervereine: keine Strafen je Spieler (nur Vereinsminuten).
- Mit 1/4 (random(0, 3) = 0, nur auf Platz 0, nicht gegen sich selbst) zusätzlich eine Strafe für den Gegner
  (rekursiv; bleibt sie aus, wird der Überzahlzähler zurückgenommen).

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
| Strafwurf 0x35818 | `[ORIGINAL]` (nur der Wurf) | `OriginalMatch.PenaltyMinutes` |
| Statistik Computervereine 0x5028/0x4DB6 | `[ORIGINAL]`, Rollenwert und Spielerreihenfolge `[APPROX]` | `MatchStatistics` |
| Zähler Torhüter (Gegentore, Minuten) | Schema 10 | `Player.GoalsAgainst`, `Player.GoalieMinutes` |
| Abschluss 0xA484, Torschütze 0x541D9/0x386D1, Strafbank, Energie, Reihenwechsel, Verletzungen, schneller Weg der Pokale | offen | Teil 2 |
| Anschluss an Spieltage, Sollwerte | offen | Teil 3 (auf integriertem Stand, `docs/arbeitspakete.md`) |

Rollenwert (Spielerfeld 0x18) führt das Remake nicht: `[APPROX]` Verteidiger 33, Stürmer 83 (Mitte der Spannen).
Verletzungen im Spiel sind noch nicht gesucht (Kandidat: Kaderplatz-Felder in 0x548A8 ff.).
