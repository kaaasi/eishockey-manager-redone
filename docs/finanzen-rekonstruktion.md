# Finanzen: Rekonstruktion der Buchungspfade (Spur `finanzen-2`)

Belege aus `em.exe` (Disassembly, Dateioffsets) und den Referenzbildern (Karriere-Video `karriere_hmmss.png`,
UI-Video `oberflaeche_mmss.png`, nur im privaten Referenz-Repo). Alle Beträge im Original intern in **Drittel-DM**
(Anzeige ÷ 3, `docs/reverse-engineering.md`). Hier stehen die Formeln so, wie der Code sie rechnet; die Umsetzung
im Remake rechnet in DM (÷ 3 am Ende).

## Werkzeug

- Overlay-Einsprünge: Segmenttabelle Datei `0x163C0`, Stub-Segment je Overlay, Stub-Kopf `+4` = Dateioffset des
  Codes relativ zu `0x255C0` (nach dem `FBOV`-Kopf). Stub-Eintrag `CD 3F oo oo` = Offset im Overlay. Das Finanz-Overlay
  (Einsprung `0x100`) liegt ab Datei `0x3CFA0`; `0x100:0x39` Einnahmen, `0x100:0x3E` Ausgaben, `0x100:0x48`
  Stadionwert, `0x100:0x4D` Tagesabschluss, `0x100:0x52` Zuschauer, `0x100:0x57` Sponsor-Bewertung, `0x100:0x5C`
  Werbeangebote. Bank-Overlay `0x138` ab `0x57480` (`0x138:0x2A` Schulden/Zinsen).
- Managerstruktur 0x1AF Byte je Manager; Felder (DGROUP-Offset bei Manager 0): `0x973F` Eintrittspreis (DM),
  `0x9746` Liga als Kostenstufe (0 = Bundesliga, 2 = Oberliga; bei Spielstart gesetzt, am Saisonende aus dem
  Vereinsindex neu bestimmt, Datei `0x563C0`),
  `0x97CE` Sponsor-Bewertung (Wort), `0x97E6/E8` Zuschauerrekord-Hilfswerte (99.999 Start), `0x97EA` **Kontostand**,
  `0x97EE` Trainingskosten/Woche, `0x97F2` zuletzt berechnete Einnahmen, `0x97F6+4·i` Darlehen (i = 0–3 Manager,
  4 = Bank; negativ = „ich schulde i“ im eigenen Satz, Zinssätze 2,5 % Manager / 4 % Bankkredit / 1 % Sparbuch),
  Stadion ab `0x9771` (9 Wörter Ausbaustufen, `+0x1D` Sitzplätze, `+0x21` Stehplätze). Spieler 0x34 Byte je Slot ab
  `0x5DC3` (+0x28 Gehalt, +0x30 Versicherung, +0xF belegt), 29 Slots je Manager, Manager 4 = Transfermarkt (12 Slots).
- Global: `[0xED8]` = 5 − Spiel-Level (Spielstart `0x2988D`), `[0x2BC]` = 100 (Preisindex), `[0x2C0]` = 110.

## Spielstart (Datei `0x29BD5` ff.)

- **Konto** = 1.200.000 + 150.000 · (5 − Level) Drittel = **400.000 + 50.000 · (5 − Level) DM**. Belegt: Karriere-Video
  500.000 DM (Level 3, Standard), UI-Video 400.000 DM (Level 5).
- Stadion: Überdachung = 2 − Start-Liga (Oberliga OHNE DACH, 2. Liga HALBOFFEN, Bundesliga HALLE); Anzeigetafel,
  Kühlanlage und Verkehrsanbindung 1 nur in der Bundesliga; Gastronomie 1 (Imbißbude) in Bundesliga und 2. Liga;
  Zustand 2 (MITTEL); Sitzkomfort, Kino, Bowling 0. Plätze: Stehplätze += 2.500 (Sitzplätze je Liga aus Tabelle).
- Jugendteams: 3 Teams, Förderung 0 (Spieler setzt sie im Bildschirm Jugend in 10.000-DM-Schritten, Deckel
  10.000 · 2^Team).

## Monatsabschluss (`0x3F02C` ff., am Monatsletzten)

Läuft, wenn `[0x5A20]` (Tag) = Monatslänge (DGROUP `0x393`) ist, also am **Monatsletzten** (#134; bis Runde 5 im Remake am
Ersten des Folgemonats). `Konto += Einnahmen(0) − Ausgaben(0,0) + Zinskonto[Manager]`, danach Konto auf ±500.000.000 Drittel begrenzt und
Zinskonto = 0. **Keine** Trainingskosten, keine Zinsen in den beiden Funktionen – die laufen über das Zinskonto
(Tagesabschluss) bzw. je Trainingstag.

### Einnahmen (`0x3E657`, Anzeige „Einnahmen (Sponsoren…)“)

```
grund   = 5 · Spielerzahl · (KondAbwehr + KondSturm + TechAbwehr + TechSturm)      [Vereinsfelder 0x18–0x1B]
werbung = Σ Monatsbetrag der 11 Werbeverträge (7 Bande, Trikot, Fernsehen, 2 Eistafeln), auch abgelaufener mit Option
```
Anzeige zusätzlich (Flag 1): Zinsertrag auf Forderungen (2,5 % Manager, 1 % Sparbuch). Gebucht wird davon nichts
direkt (siehe Tagesabschluss). Belegt: ESC Wolfsburg (Oberliga, 20 Spieler) 4.066 DM = 12.200 Drittel → Summe
der vier Teamwerte 122; Berliner SC 3.966 DM → 119.

### Ausgaben (`0x3E811`, Anzeige „Ausgaben (Gehälter…)“)

```
grund    = 130.000 − 10.000 · (5 − Level) − 20 · T[Liga]             T = [2400, 1800, 990] (Bundesliga, 2. Liga, Oberliga)
         + 175.000 · (Preisindex − 25) / 100 · (2 − Liga)             (Oberliga 0)
         − 60.000 falls Liga = 1 (2. Liga)
gehalt   = Σ (Gehalt + Versicherung) aller eigenen Spieler, auch der auf dem Transfermarkt angebotenen
stadion  = Σ Stufe_i · U_i, U = [24000, 7500, 33000, 3000, 4800, 9000, 15000, 15000, 7500]
           (Überdachung, Anzeigetafel, Kühlanlage, Sitzkomfort, Zustand, Verkehrsanb., Gastronomie, Kino, Bowling)
         + 15 · Sitzplätze + 12 · Stehplätze
jugend   = Σ Förderung der 3 Jugendteams
```
Anzeige zusätzlich (Flags 1,1): 8,5 × Kosten je Trainingstag (= 4,25 Wochen; korrigiert in der Nachprüfung, vorher 5/8), Kreditzinsen (Bank 4 %, Manager 2,5 %) und 10 % des
Kontostands, falls negativ (Überziehung). Negative Summe → 0.

Beleg UI-Video Berliner SC (Level 5, Oberliga, 2.000/2.500 Plätze, Zustand 2, Gehälter 23.200 DM aus dem
Verträge-Bildschirm 10:20): Formel 83.133 DM, Anzeige 83.233 DM (Differenz 100 DM = ein abgelesenes Gehalt).
**Wolfsburg (geklärt in der Nachprüfung):** Das Karriere-Video zeigt 87.370 DM, weil vor der Werbeseite alle 20 Verträge
auf 4 Jahre neu verhandelt werden (0:02:24–0:03:52); die Gehälter steigen von 20.710 auf 34.104 DM. Mit den neuen Gehältern
ergibt die Formel exakt 87.370 DM (Abschnitt „Unabhängige Nachprüfung“).

### Tagesabschluss (`0x3ED01`, täglich)

- Je Werbefläche (11) mit Wahrscheinlichkeit 1/11 das Flag `0xCC09 + 11·Manager + Fläche` löschen.
- Zinskonto += (Zinsertrag Forderungen [2 % Manager, 1 % Sparbuch] − Kreditzinsen − 10 % des negativen Kontostands)
  / (Tage im Monat + 1).
- Am Monatsletzten und am Tag ⌊Monatslänge/2⌋ (`0x3EE6B`): Werbeangebote neu (`0x100:0x5C`), dazu mit 50 % eine
  „Prämie“ 60.000 + 10.000 · random(0,10), auf 3.000 gerundet (Feld `0x20D + 4·Manager`; im Remake nicht umgesetzt).
- Am 1. (`0x3EF52`): Werbeverträge: Restmonate −1; bei 0 ohne Option still gelöscht, mit Option bleibt der Vertrag (und
  zahlt) und es kommt einmal die Meldung „Ein Werbepartner mit Verlängerungsoption / bittet Sie an den Verhandlungstisch.“
- Am Monatsletzten der Monatsabschluss (oben). Nach dem Tagesabschluss berechnet `0x5569F` die Werbebewertung neu und
  speichert sie (`0x97CE`).

## Zuschauer und Eintritt (`0x3F185`, Aufruf `0x5464A` je Heimspiel eines Managervereins)

Eingaben: Heimstärke `hs`, Gaststärke `as` (Mittel der sechs Vereinswerte Kond/Tech/Form Abwehr+Sturm, `0x128:0x25`),
Tabellenplatz Heim `hp`, Gast `gp` (0–11; bei Ligaunterschied: Gast aus höherer Liga → `gp` = 0, aus niedrigerer →
`gp` = 11; > 11 → 5), Ligen `hl`, `al` (0 = Bundesliga), Wettbewerbsbonus `x` (0; 1 bei Wettbewerb 1 oder 6; 2 bei 3;
Deckel 3), Eintritt `p`, Stadion.

```
hs' = hs + 17, as' = as + 17
F   = ((5·hs'/4 + 5·as'/4) / 2  +  200 · (22 − hp − gp/2) / 22) / 2
z   = 70 · F + L[hl]                                   L = [4800, 3250, 1200]
    + 3500 · (hl − al) falls hl > al, − 800 · (al − hl) falls hl < al
    + Σ_i Stufe_i · random(G·A_i/60, G·B_i/60),  G = 110 − 2·hp
      A = [350,100,60,30,30,80,50,50,50], B = [450,150,100,55,80,190,80,90,90]
    + 500 · (5 − Level)
    + random(1400, 1800) falls Stehplätze < Sitzplätze + 2000
z  *= (100 + 20·x) / 100
H   = min(90, (hs' + as' + 30) / 2)
K   = 100 − 5 · ((3p)/2);  K < −30 → K += K/2;  −30 ≤ K < 80 → K −= random(K/5, K/2)
z   = z · (100 + K · (100 − 9H/10) / 50) / 100
z  −= 9000
z < random(150,200): bei 16 + 3·(2−hl) > (3p)/2 → z /= random(2,5), sonst solange z < random(200,300): z += 50
hl = 0 und Überdachung < 2: z *= (50 + 25·Überdachung)/100;  hl = 0 und Kühlanlage = 0: z = 2z/3
z  *= (93 + 3·Zustand)/100;  z *= (95 + 5·Sitzkomfort)/100
z < 50 → z = random(50, 100);  z = min(z, Sitz + Steh)
Konto += z · p · 3 (Drittel)   → Eintritt = z · p DM, **keine Gastronomie-Einnahme**
```
Danach: Zuschauerzahl/1000 in die Saisonkurve, Saisonsumme, Rekord/Minuskulisse mit Gegner.

Belege Karriere-Video: erstes Heimspiel ESC Wolfsburg (Oberliga, 4.500 Plätze, 11 DM) ausverkauft, Konto +49.500 DM;
Saison 1 Schnitt 3.049, Minuskulisse 1.046, Rekord 4.500. Statistik „Benötigter Schnitt“ = (Ausgaben − Einnahmen) /
(p · 23/12) (in allen acht Saisons des Videos erfüllt; damit Eintritt je Saison: 11, 11, 12, 12, 13, 12/15, 17, 17 DM).

## Werbung

### Bewertung des Vereins (`0x3F833`, Feld `0x97CE`, 0–100)

Korrigiert in der Spur `finanzen-werbung` (#132):
```
S  = (2 − StartLiga) · (80 · M + 170)
   + [n ≠ 0] · 32 · (P · (n+1) − 2)  + 47 · E · ([0x7AF]+1),   n = [0x5CF3] (DEB-Pokal-Runde)
   + 12 · (11 − Mittel der Ligaplätze der letzten ≤ 5 Saisons, ohne Historie 6)
   + Ø(Spielerwert / 5500)  + random(20,50) · min(4, #Spieler mit Wert/7000 > Ø(Wert/5500) · random(35,55)/10)
   + 3 · W
R  = min(100, (((S mod 2^16) / 12 + 10 · (2 − StartLiga)) · 85 mod 2^16) / 100)
Stadionwert W (0x3EBEA) = [min(50000, (1,5·Sitz + Steh)/2) + Σ adj_i · V_i] / 700,
   V = [4333, 2333, 1166, 2833, 2000, 3333, 2333, 3166, 2250], adj: Überdachung − 1 (falls > 0), Kühlanlage ≥ 1,
   Sitzkomfort fest 2, Zustand fest 3, sonst Stufe.
```
M, P, E sind 0/1 aus `0x110:0x61` (Datei `0x44A48`, Aufruf mit Flag 1): 1, sobald der Verein in den Paarungen der
laufenden Runde steht – Maske 0x40 Meisterrunde (`[0xCCA3]`, Liste `0x5D3B`), 0x08 DEB-Pokal (Runde `[0x5CF3]`, Paarungen
`0x5CFA`), 0x20 Europapokal (Termin `[0x7AF]`, Gruppen bzw. Endrunde). Keine Titelzähler.
Pokalterm (Nachprüfung #75): Der Hilfswert `[bp−0xC]` startet mit **2** (0x3F840) und wird nur bei `[0x5CF3] ≠ 0` zu
`P · ([0x5CF3] + 1)` (0x3F870–0x3F898); addiert wird `32 · (Hilfswert − 2)` (0x3F8EE, `random(32, 32)`).
`[0x5CF3]` ist die Runde des DEB-Pokals: 0 beim Programmstart (BSS) und zu Beginn der Auslosung (0x341CC), 1 nach der
Auslosung (0x346B3), +1 je Runde (0x348DD, höchstens 5).
Star-Spieler (0x3FA24–0x3FAC3): je Spieler ein eigener Zug random(35, 55); Vergleich 32 Bit ohne Vorzeichen.
Ende (0x3FAC5–0x3FB2E): alles in 16 Bit, Division ohne Vorzeichen, nur nach oben gekappt (S = −12, Oberliga → 53).
Gespeichert wird R beim Spielstart (`0x2A03B`), nach jedem Tagesabschluss (`0x5569F`) und beim Klick auf einen Vertrag
mit Option (`0x2B7A6`); die Angebote rechnen mit dem gespeicherten Wert.
[APPROX] im Remake: Abbildung von M/P/E auf die Wettbewerbe des Remakes (`GameState.CurrentParticipation`), Europatermin =
Zahl der gespielten Europapokaltermine; Ø-Rang über die Saisons des Managers (Original: Index `[0x7A9 + Liga] − 5 … − 1`,
Index 0 übersprungen, Herkunft offen); Spielerwert = Kaufpreis × 3.

### Angebote (`0x3FB47`, am Monatsletzten und zur Monatsmitte, beim Spielstart und zu Saisonbeginn)

Je Art k (0 Bande, 1 Trikot, 2 Fernsehen, 3 Eistafel 1, 4 Eistafel 2; #133) und Partner p (0–9), W = 10 · gespeicherte
Bewertung (`0x97CE`, nicht neu berechnet):
```
ohne Flag (Flag nur beim Spielstart eines Spiels über eine Saison, 0x2A051): Partner p wird nur mit Wahrscheinlichkeit
   1/(⌊2p/3⌋ + 1) neu bedient (sonst bleibt das alte Angebot); Fernsehen nur Partner 0–4 (fünf Sender, sonst gelöscht);
   random(0, 5 − W/200) ≠ 0 und random(0,4) ≤ p → Angebot gelöscht, sonst neu:
m      = random(20, 22)
betrag = random(W·m, 13·(W·m/10)) · K[k] / 10 · (65 + 8·(p + 3·[k = Fernsehen])) · 6 / 100 · ([0x2C0] − 20) / 100   (Drittel)
         K = [8, 20, 32, 15, 15]   (Bande, Trikot, Fernsehen, Eis 1, Eis 2)
felder = random(0, m − 15) = 0 ? random(2, 3) : 1          (nur Bande relevant)
laufzeit = 12 · random(1, 2) Monate; Option: random(0,7) = 0 → 12 · random(1, 3 − laufzeit/12) Monate, sonst 0
feld +8 = W/10 (Bewertung beim Angebot, geht in den Vertrag)
```
Aufrufe: Spielstart `0x2A061` (Flag = Spielende 1994), Tagesabschluss `0x3EEA7` (Flag 0), Saisonbeginn `0x56650` (Flag 0).
Eistafel 1 ist im Werbebildschirm nur mit Überdachung HALLE (Stufe 2), Eistafel 2 nur mit Kühlanlage 'MEGAFROST' (Stufe 2)
nutzbar (`0x2B56A–0x2B587`).

**Vertragsende und Verhandlung (#133):** Am 1. Restmonate −1 (`0x3EF52`); bei 0 ohne Option gelöscht, mit Option bleibt
der Vertrag mit Restlaufzeit 0 und zahlt weiter. Erst ein Klick auf die Fläche im Werbebildschirm (`0x2B75F`) verhandelt:
x = random(90, 120) · Bewertung_beim_Angebot / 100, dann Bewertung neu (`0x2B7A6`); bei x < Bewertung_neu die Frage
„Möchten Sie den Vertrag mit … verlängern ?“ (Ja: Restlaufzeit = Option, Nein: gelöscht), sonst „Die Firma … wird den
Vertrag nicht verlängern.“ und gelöscht; die Option ist danach 0.
Beleg UI-Video (Oberliga, Spielbeginn): Bande 3.454 und 5.464 DM/Monat (12 Monate), Fernsehen 16.503 DM (24 Monate)
→ R ≈ 20–25. Karriere-Video: Rushware 13.899 DM/Monat in Saison 2.

## Stadionunterhalt und Gastronomie

Unterhalt siehe Ausgaben (`stadion`). Gastronomie ist nur eine Ausbaustufe (Zuschauer, Unterhalt, Stadionwert); eine
eigene Einnahme „Gastronomie“ gibt es im Original nicht.

## Bank (`0x46432` ff., Bildschirm Finanzen)

Kontostand = Feld `0x97EA`; Schulden = Σ meiner negativen Darlehenssalden; Anlagen = Σ Aktien · Kurs + Forderungen;
Saldo = Kontostand + Anlagen − Schulden. Karriere-Video: Kredit 584.000 DM, Zins 23.360 DM/Monat = 4 % (Kreditlimit
also > 500.000 DM).

## Saisonende (`0x26F20` ff., Aufruf `0x5645D`)

- Aufstieg (Statusbit 0 ohne Bit 1): 1.500.000 Drittel, wenn die neue Liga die Bundesliga ist, sonst 900.000
  (= 500.000 / 300.000 DM); alle 11 Werbeverträge bekommen Flag 0x80.
- Ereignissymbole aus Statusbits 0–6 (Tabelle `0x4B2E` = 2, 1, 0, 5, 4, 6, 8).
- Zwei Kennungen aus dem Saisonwechsel (`0xC8:0x20`, High-/Low-Byte je Manager): Symbol 12 bzw. 3, je 300.000 Drittel
  (100.000 DM) – nicht aufgelöst, im Remake Pokal- und Europapokalsieg.
- `0xE0:0x20(0)` = Erster der Hauptrunde der eigenen Liga (Sortierung nach Punkten, `0x2A29D`): Symbol 13, 100.000 DM –
  **in jeder Liga**, auch Bundesliga.
- Meisterprämie 1.500.000 Drittel im Play-Off-Pfad (`0x55CD3`, schon in #41).

## Nebenpunkte

- Gehaltspauschale bei leerem Kader: im Original Gehälter = Summe der Spieler, bei leerem Kader 0 → Pauschale entfällt.
- Bundestrainer-Etat (`0x554A6`): `[0xA055] += [0x29C]` = Konto des Nationalteam-Datensatzes (Manager-Satz 4, Name per
  strcpy vom Manager), nicht das Vereinskonto. Freundschaftsspiele der WM-Vorbereitung werden von dort bezahlt
  (`0x3BFC4`). Der Rest bleibt für den nächsten Amtsantritt stehen.
- WM-Titel: geht nur an Nationalteams; im Remake jetzt ausdrücklich ohne Vereinsprämie.

## Unabhängige Nachprüfung (Runde 5, Spur `finanzen-nachpruefung`)

Begonnen von ChatGPT (29.10.2026, drei Teilbefunde), abgeschlossen von Claude Code (Opus 5.5, frische Sitzung ohne
Implementierungskontext, 30.10.2026). **Unabhängigkeit:** geprüft nur gegen `em.exe` (SHA-256 `f41eb08a…2294b`,
`ndisasm`) und das Karriere-Video. Die `[ORIGINAL]`-Kommentare und die Abschnitte oben wurden nicht als Quelle genutzt;
jede Routine wurde über Overlay-Einsprungtabelle, Aufrufgraph, Datenfelder oder Texte selbst gefunden und die
Kommentaradresse erst danach verglichen. **Alle Kommentaradressen treffen die richtige Routine**; die Abweichungen
liegen in der Deutung.

Zeilennummern beziehen sich auf den Stand vor dieser Spur (`a09c81e`). „Behoben“ = mit rotem Regressionstest
(`tests/EishockeyManager.Core.Tests/FinanceNachpruefungTests.cs`) korrigiert; „Issue“ = belegt, aber außerhalb der
Dateigrenzen dieser Spur oder zu groß für sie (Folgespur).

| Fundstelle | Ergebnis | Beleg (Dateioffsets) |
|---|---|---|
| `FinanceMonthly.cs:14` Startkonto | bestätigt | `0x29BD5`: 150.000 · [0xED8] + 1.200.000 Drittel, [0xED8] = 5 − Level (`0x2988D`) |
| `FinanceMonthly.cs:21` Sockel | bestätigt | `0x3E819–0x3E868`: 130.000 − 10.000·[0xED8] − 20·Tabelle DGROUP `0x52C2` (2400, 1800, 990) |
| `FinanceMonthly.cs:27` Preisterm | bestätigt | `0x3E9A1–0x3EA07`: 175.000·([0x2BC] − 25)/100·(2 − Stufe), 2. Liga −60.000; [0x2BC] = 100, nur Laden/Speichern schreibt |
| `FinanceMonthly.cs:32/94` Grundausgaben `0x3E811` | bestätigt | Aufrufe: Anzeige `0x2AE94`/`0x2DDF0` mit (1, 1), Monatsabschluss `0x3F0BE` mit (0, 0) |
| `FinanceMonthly.cs:44/87` Grundeinnahmen | bestätigt | `0x3E657–0x3E706`: Σ Vereinsbytes +0x18/+0x19/+0x1A/+0x1B · 15 · Spielerzahl / 3; Werbeverträge `0x3E70C` |
| `FinanceMonthly.cs:67` Kostenstufe | bestätigt | gesetzt `0x29F22` (Start-Liga), Saisonende `0x563C0`: Vereinsindex ≥ [0x7C1] (12) und ≥ [0x7C2] (24) |
| `FinanceMonthly.cs:74/122` Zinskonto | Formel bestätigt, **Zeitpunkt abweichend (#134), behoben in `finanzen-werbung`** | täglich `0x3ED4F–0x3EE67`: (Forderungen 2 %, Sparbuch 1 %, Kredite, 10 % Überziehung)/(Monatslänge+1) nach DGROUP `0x2A0`; gebucht wird bei `0x3F02C` am **Monatsletzten** ([0x5A20] = Monatslänge), im Remake am Ersten |
| `FinanceMonthly.cs:100` Forderungszinsen | bestätigt | `0x3E73B–0x3E7C6` (Flag 1): Manager 25/1000, Bank (Sparbuch) 10/1000 |
| `FinanceMonthly.cs:111` Kreditzinsen | bestätigt | `0x57768` (Modus 1) mit `0x57C64`: Manager 25/1000, Bank 4/100 |
| `Finance.cs:86` Gehälter | bestätigt (auch Teilbefund ChatGPT) | `0x3E8DC–0x3E99F`: Gehalt (+0x28) und Versicherung (+0x30) über eigene Slots und eigene Spieler auf der Transferliste (Besitzer +0x1A) |
| `GameState.Manager.cs` Anzeige „Ausgaben“, Training | **abweichend, behoben** | `0x3E86B–0x3E8B5`: Kosten je Trainingstag X um 3 nach **links** (0:0x52A) + X/2 = 8,5·X; vorher W/8 + W/2 |
| `FinanceStadium.cs:79` Start-Eintritt | bestätigt | `0x29F26–0x29F61`: ((20 − 2·Liga)·2)/3 → 13/12/10 DM |
| `FinanceStadium.cs:64/65` Eintrittsgrenzen (ohne Marker) | **abweichend, behoben** | `0x3DDB8–0x3DDC5`: neuer Preis nur bei 9 < Preis < 31; vorher 5–50 DM |
| `FinanceStadium.cs:96` Startstadion | bestätigt, **Zusatz behoben** | `0x29F77–0x2A027`; zusätzlich `0x29FB3`: Spielende 1994 (Dauer eine Saison, `0x2986E`) → +1.200 Sitz-, +2.500 Stehplätze |
| `FinanceStadium.cs:190/196` Unterhalt | bestätigt | `0x3EA0B–0x3EA8C`: Stufen · DGROUP `0x94` (24000, 7500, 33000, 3000, 4800, 9000, 15000, 15000, 7500) + 15·Sitz + 12·Steh |
| `FinanceStadium.cs:204` Zuschauer | **abweichend in einem Punkt, behoben** | `0x3F63C–0x3F65B`: geteilt durch rnd(2, 5) nur bei 16 + 3·(2 − Liga) **<** Preis·3/2 (teure Karten); Rest der Formel, Tabellen `0x4FD0/0x4FE0/0x4FF2`, Rundung und Buchung bestätigt |
| `FinanceStadium.cs:281` Stärke | bestätigt | `0x4D316` (0x128:0x25): Σ der sechs Teamwerte / 6 |
| Monatsabschluss, Kontogrenze (ohne Marker) | **fehlte, behoben** | `0x3F10E–0x3F165`: Kontostand auf ±500.000.000 Drittel begrenzt |
| `FinancePremiums.cs:6` Meisterprämie | **abweichend, behoben** | `0x55CD7` vergleicht mit [0x387] = **DEB-Pokalsieger** (Titelbild `0x350B6`); Meister [0xD0E6] nur Statusbit (`0x55BE4`). Video: Pokalsieg 600.000 DM, Meister 200.000 DM |
| `FinancePremiums.cs:13` Einzelprämien 100.000 DM | **abweichend**: Pokal/Europapokal behoben, **fehlende Ereignisse #135** | `0x271A9` bester Torhüter, `0x271EF` Topscorer der eigenen Liga im Kader (Rangliste 0xC8:0x20), Europapokalsieg ohne Geld (`0x55D86`) |
| `FinancePremiums.cs:21` Aufstieg | bestätigt | `0x270A8–0x27124`: 500.000 / 300.000 DM; Video 0:18:40, 0:57:50 |
| `FinancePremiums.cs:32` Tabellenprämie | **abweichend**: Tabellenprämie gestrichen, **Fair-Play #135** | 0xE0:0x20 (`0x2A1E0`) ist die Fair-Play-Tabelle (Strafminuten); Video 0:35:54 „1. Platz der Zweite Liga, Prämien: keine“ |
| `FinanceSponsorRating.cs:20` Stadionwert | bestätigt | `0x3EBEA–0x3ED00`, Tabelle DGROUP `0x4F9C` |
| `FinanceSponsorRating.cs:4/57` Bewertung | **abweichend (#132), behoben in `finanzen-werbung`** | `0x3F833–0x3FB46`: Wettbewerbsterme 0/1 für „aktuell im Wettbewerb“ (keine ewigen Titel), Europaterm ·(Runde+1), Star-Schwelle ·Kaderschnitt, Ende in 16 Bit ohne Untergrenze 0 |
| `FinanceSponsors.cs:80` Option | bestätigt | `0x3FD52–0x3FD91`: Feld +6, 1/8, 12·rnd(1, 3 − Jahre) |
| `FinanceSponsors.cs:110/174` Angebote | Betrag und Wahrscheinlichkeiten bestätigt, **Zuordnung der Arten abweichend (#133), behoben in `finanzen-werbung`** | Faktortabelle DGROUP `0x4F05` = 8, 20, 32, 15, 15: Fernsehen 32 (p+3, nur 5 Sender), Trikot 20, Eisfläche 15 (zwei Felder, nur HALLE bzw. MEGAFROST) |
| `FinanceSponsors.cs:169` Startangebote | **abweichend (#133), behoben in `finanzen-werbung`** | Flag nur bei Spieldauer eine Saison (`0x2A051`); sonst auch beim Start gewürfelt; zusätzliche Erneuerung zu Saisonbeginn (`0x56650`) |
| `FinanceSponsors.cs:218` Erneuerung | **abweichend (#133), behoben in `finanzen-werbung`** | `0x3EE6B–0x3EEA2`: am Monatsletzten und am Tag ⌊Monatslänge/2⌋ |
| `FinanceSponsors.cs:265` Monatsbuchung | **abweichend (#133), behoben in `finanzen-werbung`** | Laufzeit −1 am 1. (`0x3EF52`), Buchung am Monatsletzten in Dritteln; Verträge mit Option zahlen bis zur Verhandlung weiter |
| `FinanceInbox.cs:129` Vertragsende | **abweichend (#133), behoben in `finanzen-werbung`** | `0x3EFB2`: mit Option nur Meldung, Frage erst im Werbebildschirm und nur bei rnd(90, 120)·Bewertung_alt/100 < Bewertung_neu; ohne Option still |

**Zufall** 0x38:0x847(a, b): beide Grenzen inklusiv, bei b < a getauscht (Borland-Generator, 15 Bit) – wie
`SquadSetup.Between`.

**Nicht sicher prüfbar:** Herkunft der Rang-Historie für den Ø-Rang der Bewertung (Index 0) und die Einheit des
Spielerwerts (0x118:0x70) gegenüber dem Transferpreis. Nebenbefund ohne Marker, nicht verfolgt: `0x562C6–0x562DE`
setzt beim Oberliga-Austausch den Kontostand eines Managers auf 400.000 DM. Zuschauerstatistik (`0x54664` ff.),
Strafe 200.000 DM bei weniger als neun Spielern (`0x54420`) und Spielbedeutung 0–2 (Wettbewerbscode [0xD0C2]) fehlen im
Remake (#136).

### Wolfsburg-Differenz (Abnahme 1)

Geklärt, kein Formelfehler. Im Karriere-Video werden vor der Werbeseite (0:05:42, „Ausgaben 87.370 DM“) alle 20
Verträge auf 4 Jahre neu verhandelt (0:02:24–0:03:52); die Gehälter steigen von 20.710 auf 34.104 DM (Kaderliste
0:03:52). 90.200 (Sockel Level 3, Oberliga) + 69.600 (Startstadion) + 3 · 34.104 = 262.112 Drittel = **87.370 DM**,
Training 0 DM (Trainingsbildschirm 0:04:00). Test: `MonthlyExpenses_Wolfsburg_AfterRenegotiation_MatchCareerVideo`.

### Mehrsaisonprobe (Abnahme 3)

`FinanceCalibrationTests` (vier Wechsel, passiv und aktiv, drei Seeds, Originaldaten) bleibt in der Sollspanne.
`RngTwinProbeTests` (vier Wechsel, passiv und aktiv): Trainingsanteil, Ein-Saison-Stadion und Kontogrenze lassen alle
Abdrücke unverändert (geprüft je Commit). Zuschauerformel und Prämien verschieben den Verlauf; fünf Abdrücke sind mit
unverändertem Generator neu ermittelt, der passive Lauf mit Originaldaten bleibt gleich.

**Ursache je geändertem Abdruck (FN-01, #139).** Ermittelt mit einer Probe, die die Mehrsaisonprobe auf den Codeständen
`27fa1c4` (Base), `bcf0b0b` (Training, Ein-Saison-Stadion, Kontogrenze), `af1d24e` (Zuschauer, Eintritt) und `e3abc90`
(Prämien) laufen lässt und den Zustand nach jedem Saisonende sowie nach jedem Schritt (`Continue`) samt Zufallsquellen
vergleicht. Jeder Abdruck ändert sich in genau einem Schritt; `bcf0b0b` ändert keinen.

| Fall | ändert sich mit | erste Zustandsdifferenz | Ursache |
|---|---|---|---|
| synthetisch passiv, Seed 3 | `e3abc90` | Saisonende 1993: Konto Manager 0 −100.000 DM, danach unverändert −100.000 DM; sonst kein Feld | Verein 2 ist 1993 Europapokalsieger (Bundesliga Platz 4); die Europapokalprämie entfällt (0x55D86) |
| synthetisch aktiv, Seed 3 | `e3abc90` | wie passiv, Seed 3 | wie oben |
| synthetisch passiv, Seed 11 | `e3abc90` | Saisonende 1993: Konto −100.000 DM, danach gleichbleibend | Verein 2 Europapokalsieger 1993 (Platz 3); Europapokalprämie entfällt |
| synthetisch aktiv, Seed 11 | `e3abc90` | wie passiv, Seed 11 | wie oben |
| Originaldaten aktiv (Verein 42) | `af1d24e` | Saison 1994, Tag 28: Heimspiel-Einnahme +1.620 DM; zugleich anderer Stand von `FinanceRandom` (einzige betroffene Zufallsquelle) | Teilung der Zuschauer durch random(2, 5) nur noch bei teuren Karten (0x3F63C): ein Zug aus `FinanceRandom` entfällt. Die Kontoabweichungen liegen unter der Untergrenze der Probe (10 Mio. DM) und sind an den Saisonenden ausgeglichen; im Abdruck bleibt nur der Stand von `FinanceRandom`. |

Tabellen-, Meister- und Pokalprämie wirken in keinem der fünf Fälle (kein Hauptrunden-Erster, kein Meister, kein
Pokalsieg des Probe-Vereins; Originaldatenfälle durch `e3abc90` unverändert). Der passive Originaldatenfall bleibt mit
allen vier Codeständen gleich.

## Spur `finanzen-werbung` (Runde 6): #132, #133, #134

Umgesetzt nach den Abschnitten oben (Bewertung, Angebote, Vertragsende, Monatsabschluss am Monatsletzten); Regressionstests
in `tests/EishockeyManager.Core.Tests/FinanceWerbungTests.cs` und `tests/EishockeyManager.UI.Tests/FinanceWerbungScreenTests.cs`.
Neuer Zustand (Schema 7, `SaveGame.Migration6.cs`): gespeicherte Bewertung je Manager (−1 in alten Spielständen, beim
nächsten Gebrauch berechnet), Bewertung in Angebot und Vertrag (alte Verträge 0 → Verhandlung endet immer mit der Frage),
zweite Eistafel als eigene Art. Offene Verlängerungsfragen alter Spielstände bleiben im Posteingang beantwortbar.
Nicht umgesetzt: die „Prämie“ der Angebotstage (Feld `0x20D`), Drittel-DM-Summe der Werbeverträge (Remake: DM je Vertrag).

**Mehrsaisonprobe, Ursache je geändertem Referenzwert.** Ermittelt mit einer Probe, die `RngTwinProbeTests.Run` auf
Base `1e4731b` und auf dem Spur-Stand laufen lässt und nach jedem Schritt Zustand, Konto und alle vier Zufallsquellen
vergleicht. In allen Fällen bleiben Haupt-, Pokal- und Vertragszufallsquelle in jedem Schritt gleich – keine
Spielergebnisse, Auslosungen, Titel oder Vertragsentscheidungen verschieben sich.

| Fall | erste Differenz | Ursache |
|---|---|---|
| synthetisch passiv/aktiv, Seed 3 und 11 (4 Abdrücke) | FinanceRandom im ersten Schritt (Tag 3); Konto im Schritt bis Tag 21 (Monatsletzter 30.9.) | Bewertung nach jedem Tagesabschluss zieht aus FinanceRandom (`0x5569F`); Monatsabschluss am 30.9. statt 1.10. (#134), andere Werbeangebote (#132/#133) |
| Originaldaten passiv/aktiv (Verein 42) | FinanceRandom und Konto im ersten Schritt (Tag 3) | wie oben; das Heimspiel an Tag 3 zieht für die Zuschauer aus dem verschobenen FinanceRandom (Einnahme −880 DM) |
| Modus 4 (`GameModeTests`, Vereine 2 und 30) | nur Kontostände beider Manager | wie oben; Titel, `R…` und `C…` unverändert |

