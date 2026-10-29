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

## Monatsabschluss (`0x3F02C` ff., am Monatsersten)

`Konto += Einnahmen(0) − Ausgaben(0,0) + Zinskonto[Manager]`, danach Konto auf ±500.000.000 Drittel begrenzt und
Zinskonto = 0. **Keine** Trainingskosten, keine Zinsen in den beiden Funktionen – die laufen über das Zinskonto
(Tagesabschluss) bzw. je Trainingstag.

### Einnahmen (`0x3E657`, Anzeige „Einnahmen (Sponsoren…)“)

```
grund   = 5 · Spielerzahl · (KondAbwehr + KondSturm + TechAbwehr + TechSturm)      [Vereinsfelder 0x18–0x1B]
werbung = Σ Monatsbetrag der 11 Werbeverträge (7 Bande, 2 Eis, Trikot, Fernsehen)
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
Anzeige zusätzlich (Flags 1,1): 5/8 der Trainingskosten/Woche, Kreditzinsen (Bank 4 %, Manager 2,5 %) und 10 % des
Kontostands, falls negativ (Überziehung). Negative Summe → 0.

Beleg UI-Video Berliner SC (Level 5, Oberliga, 2.000/2.500 Plätze, Zustand 2, Gehälter 23.200 DM aus dem
Verträge-Bildschirm 10:20): Formel 83.133 DM, Anzeige 83.233 DM (Differenz 100 DM = ein abgelesenes Gehalt).
**Restlücke:** Karriere-Video ESC Wolfsburg (Level 3, Gehälter 20.710 DM, gleiches Stadion) zeigt 87.370 DM, die
Formel ergibt 73.977 DM; die Differenz von 13.393 DM (40.180 Drittel) ist nicht erklärt (Trainingskosten 0,
keine Versicherung, Konto positiv, keine Kredite). Möglich: andere Programmversion oder nicht sichtbarer Zustand.

### Tagesabschluss (`0x3ED01`, täglich)

- Je Werbefläche (11) mit Wahrscheinlichkeit 1/11 das Flag `0xCC09 + 11·Manager + Fläche` löschen.
- Zinskonto += (Zinsertrag Forderungen [2 % Manager, 1 % Sparbuch] − Kreditzinsen − 10 % des negativen Kontostands)
  / (Tage im Monat + 1).
- Am 1. und in der Monatsmitte: Werbeangebote neu (`0x100:0x5C`), dazu mit 50 % eine „Prämie“
  60.000 + 10.000 · random(0,10), auf 3.000 gerundet (Feld `0x20D + 4·Manager`).
- Am 1.: Werbeverträge: Restmonate −1; bei 0 ohne Option gelöscht, mit Option Nachricht „Werbepartner mit
  Verlängerungsoption“.

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

```
S  = (2 − StartLiga) · (80 · Titel(0x40) + 170)
   + [n ≠ 0] · 32 · (Titel(0x08) · (n+1) − 2)  + 47 · Titel(0x20) · ([0x7AF]+1),   n = [0x5CF3] (DEB-Pokal-Runde)
   + 12 · (11 − Mittel der Ligaplätze der letzten ≤ 5 Saisons, ohne Historie 6)
   + Mittel(Spielerwert / 5500)  + random(20,50) · min(4, #Spieler mit Wert/7000 > random(35,55)/10)
R  = min(100, ((S + 3 · W) / 12 + 10 · (2 − StartLiga)) · 85 / 100)
Stadionwert W (0x3EBEA) = [min(50000, (1,5·Sitz + Steh)/2) + Σ adj_i · V_i] / 700,
   V = [4333, 2333, 1166, 2833, 2000, 3333, 2333, 3166, 2250], adj: Überdachung − 1 (falls > 0), Kühlanlage ≥ 1,
   Sitzkomfort fest 2, Zustand fest 3, sonst Stufe.
```
Pokalterm (Nachprüfung #75): Der Hilfswert `[bp−0xC]` startet mit **2** (0x3F840) und wird nur bei `[0x5CF3] ≠ 0` zu
`Titel(0x08) · ([0x5CF3] + 1)` (0x3F870–0x3F898); addiert wird `32 · (Hilfswert − 2)` (0x3F8EE, `random(32, 32)`).
`[0x5CF3]` ist die Runde des DEB-Pokals (Wettbewerb 0, Feld `[Wettbewerb + 0x5CF3]`): 0 beim Programmstart (BSS) und
zu Beginn der Auslosung (0x341CC), 1 nach der Auslosung (0x346B3), +1 je Runde (0x348DD, höchstens 5). Ohne
ausgelosten Pokal ist der Term also 0, danach ohne Titel −64. `n` ist damit aufgelöst; offen bleiben die Titelzähler
`0x110:0x61` (Masken 0x40/0x08/0x20) → im Remake `[APPROX]` über Meister/Pokal/Europapokal-Zähler, und `[0x7AF]`.

### Angebote (`0x3FB47`, am 1. und zur Monatsmitte, sowie beim Spielstart)

Je Art k (0 Bande, 1 Eis, 2 Trikot, 3 Fernsehen, 4 unbenutzt) und Partner p (0–9), W = 10·R:
```
nicht am Spielstart: Partner p wird nur mit Wahrscheinlichkeit 1/(⌊2p/3⌋ + 1) neu bedient (sonst bleibt das alte Angebot);
   Trikot nur Partner 0–4 (sonst Angebot gelöscht);
   random(0, 5 − W/200) ≠ 0 und random(0,4) ≤ p → Angebot gelöscht, sonst neu:
m      = random(20, 22)
betrag = random(W·m, 13·(W·m/10)) · K[k] / 10 · (65 + 8·(p + 3·[k = Trikot])) · 6 / 100 · ([0x2C0] − 20) / 100   (Drittel)
         K = [8, 20, 32, 15, 15]
felder = random(0, m − 15) = 0 ? random(2, 3) : 1          (nur Bande relevant)
laufzeit = 12 · random(1, 2) Monate; Option: random(0,7) = 0 → 12 · random(1, 3 − laufzeit/12) Monate, sonst 0
```
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

## Unabhängige Nachprüfung (ChatGPT, begonnen 29.10.2026)

Prüfstand: `em.exe` aus dem privaten Referenz-Repository, SHA-256
`f41eb08aa3bc3119bd83c44856ccc550d7a450d6ee55e0447378700fefb2294b`;
16-Bit-Disassembly mit `objdump -D -b binary -m i8086 -M intel`. Diese Tabelle ist **unvollständig**;
alle nicht genannten `[ORIGINAL]`-Stellen und die Wolfsburg-Differenz sind noch offen.

| Fundstelle | Ergebnis der Teilprüfung | Beleg |
|---|---|---|
| `FinanceMonthly.cs`, Sockel `0x3E819` | Konstanten und Rechenoperationen bestätigt; Tabellenwerte selbst noch nicht unabhängig geprüft | `0x3E820` lädt 10.000; `0x3E82C–0x3E835` subtrahiert das Produkt von 130.000; `0x3E853–0x3E868` zieht 20 × Tabellenwert ab. |
| `FinanceMonthly.cs`, Preisterm `0x3E9A1` | Rechenoperationen bestätigt; Vorbelegung des Preisindex noch nicht unabhängig geprüft | `0x3E9A8–0x3E9EA`: Index minus 25, mal 175.000, geteilt durch 100, mal 2 minus Liga; `0x3E9FB–0x3EA07` zieht für Liga 1 weitere 60.000 ab. |
| `Finance.cs`, Gehaltsanteil `0x3E8DC` | Schleifen über eigene Spieler und zugeordnete Transfermarktspieler bestätigt; Feldbedeutungen noch nicht unabhängig nachverfolgt | `0x3E8DC–0x3E92D` summiert zwei Felder je eigenem Spieler; `0x3E92F–0x3E99F` summiert dieselben Felder zugeordneter Marktspieler. |
