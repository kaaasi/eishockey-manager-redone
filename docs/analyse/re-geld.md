# Analyse em.exe: Geld, Börse, Stadion (Spur `re-geld`, Runde 10)

Alle Adressen in dieser Datei sind **Dateioffsets** in `em.exe` (Schreibweise und Umrechnung wie in
[re-abdeckung.md](../re-abdeckung.md)); Werte im Datensegment stehen ausdrücklich als `DGROUP 0x…`. Beträge sind wie im
Original in **Drittel-DM** angegeben, wenn nicht „DM“ dabeisteht ([reverse-engineering.md](../reverse-engineering.md),
„Geldbeträge“). Hilfsfunktionen der Laufzeit und der Fensterbasis (Spur `re-meldungen`) stehen nur mit ihrem Fernaufruf
in der Schreibweise des Plans (`38h:847h`), damit die Funktionskarte sie nicht dieser Datei zuordnet:

| Aufruf | Bedeutung (aus der Benutzung hier) |
|---|---|
| `38h:847h` | `random(a, b)` mit beiden Grenzen |
| `120h:57h` | Byte über Zeiger auf `[lo, hi]` begrenzen (vorzeichenbehaftet) |
| `120h:61h` | Betrag `|a − b|` (32 Bit) |
| `0:3CAh`, `0:47Ch`, `0:483h`, `0:48Bh` | 32-Bit-Multiplikation, -Division (zwei Varianten), -Rest |
| `0:1A9Ch`, `0:1B0Bh`, `0:1B34h` | `strcat`, `strcpy`, `strlen` |
| `38h:2C2h`, `38h:2D8h` | Zahl als Text; Betrag in Drittel-DM als DM-Text (÷ 3) |
| `30h:800h`, `30h:831h`, `30h:885h` | Rechteck füllen `(x1, y1, x2, y2, Farbe)`; Text `(x, Grundlinie, Farbe)`; Text zentriert zwischen `x1` und `x2` |
| `120h:3Eh`, `120h:7Ah`, `120h:34h` | Pfeilknöpfe/Wertabfrage, Ja/Nein-Frage, Meldung in den Posteingang (Spur `re-meldungen`) |

Belege: Disassembly mit `ndisasm` über die Funktionskarte (`tools/reverse-engineering/funktionskarte.py`, Grenzen und
Aufrufer aus der CSV), DGROUP-Inhalte aus em.exe (Datei 0x163C0 ff., `DGROUP_FILE` im Skript). Spielstände aus dem
Referenz-Repo nur dort, wo sie schon ausgewertet sind ([spielstand-original.md](../formate/spielstand-original.md)).
Es wurden keine neuen DOSBox-Bilder gebraucht.

## Übersicht

| Adresse (Plan) | Bereich | Rolle laut Code | Karte vorher | Ergebnis |
|---|---|---|---|---|
| Datei 0x3D45F | Ov 0x100 + 0x4BF | Stadion: Tabelle „Ist“ bzw. „Bau“ (9 Ausbauteile, 2 Platzarten, Konto) | unbekannt | zugeordnet, P1 mit 0x3D7F8/0x3D316 |
| Datei 0x57CFB | Ov 0x138 + 0x87B | Finanzen: Kästen Bank/Mitmanager (Saldo und Zins je Monat) | unbekannt | zugeordnet |
| Datei 0x57ECF | Ov 0x138 + 0xA4F | Finanzen: Übersicht rechts (Konto, Untereinander, Bank, Aktien) | unbekannt | zugeordnet |
| Datei 0x581C2 | Ov 0x138 + 0xD42 | Finanzen: Infofeld je Aktie (Kurs, Zahl, Kauf, Wert) | unbekannt | zugeordnet |
| Datei 0x58409 | Ov 0x138 + 0xF89 | Finanzen: Betragsbalken (Schieberegler) für Bank, Mitmanager, Aktien | unbekannt | zugeordnet, P1 mit Buchung in 0x58A57 |
| Datei 0x463CC | Ov 0x118 + 0x3CC | Transfermarkt: Leiste Kontostand/Schulden/Anlagen/Saldo | unbekannt | zugeordnet, P2 |
| Datei 0x48E62 | Ov 0x118 + 0x2E62 | **Taktik**-Bildschirm, Abschluss (kein Geld; „Grundwert“ ist die Taktikzeile) | unbekannt | zugeordnet, P2 |
| Datei 0x39DEC | Ov 0xF8 + 0x41C | Jugend: Stärke und Tabellenplatz je Ligaspieltag | genähert | zugeordnet, P1 |
| Datei 0x3A13B | Ov 0xF8 + 0x76B | Jugend: Saisonwechsel (Auf-/Abstieg, Stärke) | genähert | zugeordnet, P1 |

Nebenbei zugeordnet (gleicher Ablauf, bisher unbekannt): Datei 0x3D316 (wöchentlicher Baufortschritt) und Datei 0x3E52B
(Kostenzeile im Ausbaudialog). Der Ausbaudialog selbst liegt im umgesetzten Stadionbildschirm Datei 0x3D7F8.

## Stadion (Ov 0x100)

### Datensatz

Stadion je Manager ab `DGROUP 0x9771 + 0x1AF · Manager` (Zeiger in 0x3D316 und 0x3D7F8), Struktur aus den Zugriffen:

| Feld | Inhalt |
|---|---|
| `+2·i`, `+2·i+1` (i = 0–8) | Ausbaustufe jetzt / Zielstufe im Bau (0 = kein Bau) für Überdachung, Anzeigetafel, Kühlanlage, Sitzkomfort, Zustand, Verkehrsanbindung, Gastronomie, Kino, Bowlingbahn (Namen `DGROUP 0x484`) |
| `+0x12 + i` (i = 0–10) | Restwochen je Projekt (0 = keine Baustelle); 9 = Sitzplätze, 10 = Stehplätze |
| `+0x1D + 4·j`, `+0x1F + 4·j` (j = 0 Sitz-, 1 Stehplätze) | Plätze jetzt / Plätze nach dem Bau (0 = kein Bau) |

Stufenzahl je Teil `DGROUP 0x102` (Wörter): 3, 3, 3, 3, 5, 3, 4, 4, 2 (Stufennamen `DGROUP 0x4B4 + 0x14 · i + 4 · Stufe`,
z. B. „OHNE DACH / HALBOFFEN / HALLE“). Das passt zu den Stufenlisten des Remakes.

### Datei 0x3D45F – Tabelle „Ist“/„Bau“

Aufrufer nur der Stadionbildschirm (Datei 0x3D7F8, dreimal: Aufbau, nach Umschalten, nach einem Bau). Argumente: Zeiger auf
den Datensatz, Modus (0 = jetziger Stand, 1 = Baustellen). Zeichnet in Schrift `DGROUP 0x374`:

- Fläche (0 | 169)–(188 | 239) und (262 | 169)–(318 | 187) in Farbe 4.
- Je Teil i = 0–8 Name bei (1 | 177 + 7·i) Farbe 30, Wert bei x = 55: „: “ + Stufenname (Modus 0: Stufe `+2·i`, Modus 1:
  Zielstufe `+2·i+1`). Modus 1 ohne Bau: „(keine Bautätigkeiten)“ (`DGROUP 0xDD0`); mit Bau dahinter „ (Noch n Wochen)“,
  bei n = 1 ohne das letzte Zeichen („Woche“, Datei 0x3D5CA–0x3D5E5). Wertfarbe `31 − 30 · Modus` (31 bzw. 1).
- Sitz- und Stehplätze bei (213 | 177 + 7·j) Farbe 30, Wert bei x = 267: Plätze jetzt bzw. nach dem Bau; Modus 1 ohne Bau
  „(kein Bau...)“, mit Bau „ (n)“ (nur die Zahl der Restwochen).
- Fläche (213 | 230)–(318 | 236), „Konto“ bei (213 | 236) Farbe 11, Kontostand `DGROUP 0x97EA` mit Feldbreite 10 bis x 312
  (Hilfsfunktion des Overlays 0xE0, `E0h:25h`).

### Ausbau im Stadionbildschirm (Datei 0x3D7F8, innerhalb der Funktion)

Ablauf ab Auswahl eines Projekts `p` (0–10), Datei 0x3DE71–0x3E490:

1. **Baufirma beleidigt:** Ist `DGROUP 0xCC09 + 11 · Manager + p` gesetzt, kommt „Die Baufirma / ist noch beleidigt...“
   (`DGROUP 0x1239`) und keine Auswahl (Datei 0x3DE77–0x3DEA7).
2. **Obergrenze** (Datei 0x3DEB4–0x3DF1E): Teile: höchste Stufe = Stufenzahl − 1. Plätze: `(25.000 − Σ_j Plätze_j) / 500`
   Schritte, wobei für eine Platzart im Bau die Zielzahl zählt – **Sitz- und Stehplätze zusammen höchstens 25.000**.
3. **Gesperrt** (Datei 0x3DF1F–0x3DF81): Projekt schon im Bau (Zielstufe bzw. Zielplätze ≠ 0), Platzschritte 0, oder Teil
   schon auf der höchsten Stufe. Andere Projekte bleiben frei: **mehrere Baustellen gleichzeitig**, je Projekt höchstens eine.
4. **Wahl der Stufe** mit Pfeilen (`120h:3Eh`), Start = jetzige Stufe + 1 (Plätze: 1 Schritt = +500). Kosten
   `(Ziel − jetzt) · K[p]` mit `K = DGROUP 0xC0` (11 Langwörter, Drittel-DM je Stufe bzw. je 500 Plätze):

   | p | Projekt | K (Drittel) | = DM je Schritt | Bauzeit `random(lo, hi)` Wochen (`DGROUP 0xEC + 2·p`) |
   |---|---|---|---|---|
   | 0 | Überdachung | 3.600.000 | 1.200.000 | 20–30 |
   | 1 | Anzeigetafel | 1.350.000 | 450.000 | 12–17 |
   | 2 | Kühlanlage | 1.920.000 | 640.000 | 16–25 |
   | 3 | Sitzkomfort | 1.110.000 | 370.000 | 6–9 |
   | 4 | Zustand | 990.000 | 330.000 | 5–8 |
   | 5 | Verkehrsanbindung | 3.900.000 | 1.300.000 | 40–60 |
   | 6 | Gastronomie | 1.740.000 | 580.000 | 17–24 |
   | 7 | Kino | 1.560.000 | 520.000 | 14–18 |
   | 8 | Bowlingbahn | 1.350.000 | 450.000 | 10–15 |
   | 9 | Sitzplätze (je 500) | 540.000 | 180.000 | 12–17 |
   | 10 | Stehplätze (je 500) | 405.000 | 135.000 | 10–14 |

   Eine höhere Stufe wird nur angenommen, wenn der Kontostand die Kosten deckt (Datei 0x3E107–0x3E137). Die Kostenzeile
   zeichnet Datei 0x3E52B (unten).
5. **Klick auf „ausbauen“** (Datei 0x3E1FE): nur bei Ziel ≠ jetzt und Konto ≥ Kosten. Bauzeit
   `w = random(lo, hi)` wird **einmal** gezogen (nicht je Stufe), Text „Bauzeit: ca. w Wochen“ (`DGROUP 0xDE0`).
   `DGROUP 0xCC09 + 11·m + p` = 1. Rabatt `r = random(0, 10)`, mit `random(0, 2) = 0` dagegen 0 (Reihenfolge der
   Ziehungen so). Frage „Möchten Sie Ihr Stadion ausbauen ?“ mit „Rabatt: r%“ (`120h:7Ah`, Ja/Nein).
6. **Ja** (Datei 0x3E389–0x3E47F): Flag wieder 0; Kosten −= `Kosten · r / 100` (ganzzahlig); **Konto −= Kosten direkt**
   (`DGROUP 0x97EA`, keine Kategorie, keine Rate); Restwochen = `w + random(0, 4) − 2` (die angezeigte Zeit ist „ca.“);
   Zielstufe bzw. Zielplätze = jetzt + 500 · Schritte eintragen; Tabelle neu (0x3D45F, Modus 1).
   **Nein** (Datei 0x3E482): Restwochen 0, das Flag bleibt 1 → „Baufirma beleidigt“.
7. Das Flag löscht der Tagesabschluss Datei 0x3ED01 (Datei 0x3ED19–0x3ED48): je Projekt mit `random(0, 10) = 0`, also im
   Mittel nach 11 Tagen.

### Datei 0x3D316 – Baufortschritt (wöchentlich)

Aufruf aus dem Tagesablauf Datei 0x54C9F je menschlichem Manager, wenn Tageszähler `DGROUP 0x3B7` durch 7 teilbar ist
(Datei 0x552B5–0x552C3, 0x556B5–0x556CD, 0x564F5–0x5650D). Je Projekt p = 0–10 mit Restwochen > 0: −1; erreicht es 0:
Stufe = Zielstufe, Zielstufe = 0 (Teile) bzw. Plätze = Zielplätze, Zielplätze = 0. Dann Meldung „Der Stadion-Ausbau (Name)
ist abgeschlossen.“ (`DGROUP 0xDE8`, `0xDEC`) über `120h:34h`. Vorher ruft sie Datei 0x3CFA0 (umgesetzt).

### Datei 0x3E52B – Kostenzeile

Argumente: Projekt, Zielstufe, Zielplätze, Kosten. Text „ Kosten: x DM“ bei (213 | 225); Farbe 8, wenn das Konto reicht,
sonst 17 (Datei 0x3E59A–0x3E5C7). Darunter zentriert zwischen x 213 und 315 auf Grundlinie 215 der Stufenname bzw. die
Platzzahl, Farbe 24.

### Abgleich mit dem Remake

`Core/FinanceStadium.cs` (`FirstStepCost`, `WeeksPerStep`, `Offer`, `PlacesOffer`, `StartConstruction`,
`AdvanceConstruction`, alle `[APPROX]`) und `UI/Screens/Manager/StadiumScreen.cs`:

| Punkt | Original | Remake |
|---|---|---|
| Baustellen | je Projekt eine, alle 11 parallel | höchstens eine insgesamt |
| Kosten | `(Ziel − jetzt) · K` (linear) | Stufe n kostet n · erster Schritt; Überdachung 1,5 Mio., Anzeigetafel 250.000, Sitzkomfort/Zustand aus der Kapazität, Verkehr 1,2 Mio. |
| Plätze | 500 je Schritt, Sitz 180.000 DM, Steh 135.000 DM, zusammen ≤ 25.000 | 500 Sitz für 110.000 DM, 1.000 Steh für 60.000 DM, je ≤ 15.000 |
| Bauzeit | `random(lo, hi)` einmal, tatsächlich ± 2 | Wochen je Stufe fest |
| Rabatt / Ablehnung | Rabatt 0–10 % (in 1/3 der Fälle sicher 0), „Nein“ sperrt das Projekt bis zum Zufall im Tagesabschluss | fehlt |
| Buchung | Konto direkt | `Book(…, Bau, …)` |
| Meldung | je fertigem Projekt | ja |

Die Videowerte im Kommentar zu `FirstStepCost` (Kühlanlage 640.000, Imbißbude 580.000, Kino 520.000, Bowlingbahn 450.000)
bestätigen K; „Straße 1.200.000 DM“ widerspricht K = 1.300.000 DM – die Videostelle ist nachzusehen (Rabatt wird erst nach
der Frage abgezogen, kann es also nicht sein).

## Finanzen-Bildschirm (Ov 0x138)

Alle vier Funktionen ruft nur der Finanzen-Bildschirm Datei 0x58A57 (Aufbau Datei 0x58CED–0x58D04, Balken Datei 0x5900E,
0x5910E, 0x59A5D). Datensätze wie in [finanzen-rekonstruktion.md](../finanzen-rekonstruktion.md) („Börse“, „Werkzeug“):
Konto `DGROUP 0x97EA`, Darlehen `DGROUP 0x97F6 + 4·i` (negativ = ich schulde i; i = 4 Bank), Bestand
`DGROUP 0xD214 + 32·m + 8·i` (Kaufpreis, `+4` Stückzahl), Kurs `DGROUP 0xD13A + 0x48·i`.

### Datei 0x57CFB – Kästen Bank und Mitmanager

Argumente: Hintergrundbild, Zeiger auf die drei Mitmanager-Nummern des Bildschirms (`DGROUP 0x553B`, 0xFF = leer). Je Kasten
k = 0–3 (k = 0 Bank = Satz 4, sonst Mitmanager): zwei Zeilen zentriert zwischen `DGROUP 0x5520 + 2·k` und dem nächsten
Wert (0, 112, 181, 253, 315) auf Grundlinie 62 bzw. 70: eigenes Darlehen mit i und der Monatszins aus Datei 0x57C64 („ DM“ bzw. „ DM/M.“). Schrift 9, Farbe 16
bei negativem, 12 bei positivem, 30 bei Null-Betrag (Datei 0x57E0A–0x57E68, gleiche Regel in 0x57ECF).

### Datei 0x57ECF – Übersicht rechts

15 Zeilen ab Grundlinie 107 (Schrift 9), Text bei x = 249 (Farbe 30, Schatten 6), Werte bei x = 246 (Aufruf `30h:885h`
mit zweitem x = −1); Zeilen mit Überschrift
(`DGROUP 0xDF0 + 4·z`) rücken 10, Wertzeilen 8 Pixel vor. Sprungtabelle Datei 0x581A6:

| Zeile | Inhalt |
|---|---|
| 0 / 1 | „Kontostand:“ / Konto |
| 2 / 3 / 4 | „Untereinander:“ / Σ Darlehen mit den anderen menschlichen Managern / Σ deren Monatszinsen |
| 5 / 6 / 7 | „Bank:“ / Darlehen Bank / Monatszins Bank |
| 8, 9 / 10 | „Aktien:“, „Kaufpreis:“ / Σ Kaufpreise |
| 11 / 12 | „Depot-Wert:“ / Σ Stückzahl · Kurs |
| 13 / 14 | „Depot-Saldo:“ / Depotwert − Σ Kaufpreise |

Hintergrund vorher mit Hilfsfunktion `48h:4675h` (Bereich x 246–…, y 103–132).

### Datei 0x581C2 – Infofeld je Aktie

Je Aktie i = 0–3: Fläche (47 | 104 + 34·i, +1 ab i = 2)–(127 | 132 + 34·i, +1 bei i = 3) Farbe 8, dann vier Zeilen „Kurs:“, „Zahl:“, „Kauf:“, „Wert:“
(`DGROUP 0xE2C`) bei x = 48, Werte bei x = 70, Grundlinie 110 + 34·i (+1 ab i = 2) + 7·Zeile, Farbe `DGROUP 0x552A + i` (14, 22, 17, 9). Werte: Kurs,
Stückzahl mit „ St.“, Kaufpreis, Stückzahl · Kurs. Setzt am Ende `DGROUP 0x552E` = 1.

### Datei 0x58409 – Betragsbalken

Argumente: x der Beschriftung (36 links, 167 rechts), Balkenlänge `s` in Pixeln (−1 = aus dem Vorgabewert), Schrittwert
`q` (32 Bit), Höchstwert `max`, Vorgabewert, Art (Bit 0x80 = Aktie, Stück statt DM). Regel:

```
v = s · q;  if q == 0: q = 1
if s == -1: v = Vorgabe; s = v / q
else if not Aktie: v = (v / 3000) · 3000        // ganze 1.000 DM
v = min(v, max)
zeichnen (nur v ≠ 0): Text „v DM“ bzw. „v St.“ zentriert zwischen x und x + 115 auf Grundlinie 95, Farbe 30;
Balken y 89–96: Fläche x 4–313 Farbe 0, dann rechts (x > 159) 160 … 159 + s, links 158 − s … 158 Farbe 9
return v   (auch in DGROUP 0x552F, dort nie gelesen)
```

Der Aufrufer setzt `q = max / 154 + 1` (Datei 0x59A15–0x59A30), Mausklicks auf den Balken geben `s` direkt (Datei
0x590AD–0x5911A), die Pfeile ändern den Betrag um ±1 Stück bzw. ±3.000 Drittel (Datei 0x58F80–0x59017).

**Höchstwerte im Aufrufer** (Datei 0x596C2–0x59A12; links = „holen/leihen/kaufen“, rechts = „einzahlen/verleihen/verkaufen“,
Texte `DGROUP 0xE3C ff.`):

| Auswahl | links | rechts |
|---|---|---|
| Aktie i | Konto / Kurs (0, wenn Konto ≤ 0) | Stückzahl |
| Bank | 6.000.000 − (was ich der Bank schulde); Vorgabe und Mindest-Höchstwert = Sparbuch | eigenes Konto (nur > 0); Vorgabe = Kreditschuld, höchstens Konto |
| Mitmanager j | nur wenn sein Konto > 0: min(3.000.000 − was ich j schulde, Konto von j); Vorgabe = was j mir schuldet | min(3.000.000 − was j mir schuldet, eigenes Konto); Vorgabe = was ich j schulde |

Also **Bankkredit bis 2.000.000 DM**, zwischen Managern bis 1.000.000 DM je Richtung. Spielstand FILE08
(Bankkredit 701.000 DM, [spielstand-original.md](../formate/spielstand-original.md)) liegt über dem Video-Wert 584.000 DM
und im Rahmen.

**Buchung im Aufrufer** (Datei 0x5915F–0x5952D, Betrag v):

- Geld (Bank = Satz 4 wie ein Manager): rechts Konto −= v, Konto j += v, eigenes Darlehen[j] += v, Darlehen von j[ich] −= v;
  links umgekehrt. Ein Mitmanager wird **nicht gefragt**, der Betrag fließt sofort.
- Aktie kaufen: `w = v · Kurs`, Kosten `w + w · 15 / 1000`; Konto −= Kosten, Kaufpreis += Kosten, Stückzahl += v.
- Aktie verkaufen: Konto += `w − w · 15 / 1000`; Kaufpreis = `Kaufpreis · ⌊(Zahl − v) · 100 / Zahl⌋ / 100`; Zahl −= v.

**Gebühr 1,5 %** in beiden Richtungen; der Kaufpreis enthält sie.

### Abgleich mit dem Remake

`Core/FinanceStocks.cs` (`BuyStock`, `SellStock`), `Core/FinanceBank.cs`, `Core/Finance.cs` (`CreditLimit = 1.000.000`
`[APPROX]`), `UI/Screens/Manager/FinanceScreen.cs` (`Step` 10.000 DM/10 Stück `[APPROX]`, Bedienung `[APPROX]`):

- Keine Gebühr beim Kauf und Verkauf; Kaufpreis beim Verkauf anteilig genau statt über ganze Prozent.
- Kreditrahmen 1 Mio. DM statt 2 Mio.; Grenze zwischen Managern fehlt (nur Konto des Partners).
- Leihen bei einem Mitmanager geht im Remake als Anfrage in den Posteingang, im Original sofort.
- Betrag über ±-Knöpfe in festen Schritten statt Balken (Klick = Pixel · `q`, Rundung auf 1.000 DM).
- Übersicht rechts und Aktienfelder: Inhalt stimmt (Zeilen wie oben), Lage und Farben nicht nachgemessen.

## Transfermarkt: Kontoleiste (Datei 0x463CC)

Aufrufer nur Transferliste Datei 0x465F1. Fenster (0 | 222)–(319 | 239) mit Rahmen über `30h:335h`; vier Felder
k = 0–3 bei x = 3 + 65·k, Text (`DGROUP 0x11C5`) auf Grundlinie 229, Wert auf 236, Farbe 7 bzw. 240 („Saldo:“):

| k | Text | Wert |
|---|---|---|
| 0 | Kontostand: | Konto |
| 1 | Schulden: | Datei 0x57768 (Modus 0, alle): Σ eigener negativer Darlehen (Manager und Bank) |
| 2 | Anlagen: | Σ Stückzahl · Kurs + Σ_{i ≠ ich} (−Darlehen von i[ich], wenn negativ) – Forderungen samt Sparbuch |
| 3 | Saldo: | Konto + Anlagen − Schulden |

Das ist die Regel aus [finanzen-rekonstruktion.md](../finanzen-rekonstruktion.md), Abschnitt „Bank (`0x46432` ff.)“ – die
Stelle liegt aber im Transfermarkt-Overlay, nicht im Finanzen-Bildschirm. **Remake:** `TransferScreen.DrawAccountBar`
zeigt Schulden und Anlagen fest 0 (`[APPROX]`), Saldo = Konto; x der Anlagen 132 statt 133, Zeilen 226/232 statt
Grundlinien 229/236 (Lage nicht nachgemessen).

## Taktik-Abschluss (Datei 0x48E62)

Der Plan führt die Funktion als „Grundwert“ unter Geld; das Wort stammt aus dem Aufrufer, dem Taktikbildschirm
Datei 0x47F2B (Zeile „Grundwert“ von Einsatz/Angriff, `Core/Squad.Tactics.cs`). Mit Geld hat sie nichts zu tun. Aufruf beim
Verlassen (Datei 0x48E19) mit Vereinssatz, Merker und den vier Bildzeigern des Bildschirms:

1. Bilder freigeben (`38h:70Ah`).
2. Einsatz des Vereins `+0x26` (Grundwert) und `+0x27…+0x2B` (Zuschläge): jeder Zuschlag wird auf `100 − Grundwert` gekürzt
   (wie die Pfeilgrenze Datei 0x488E9 ff.).
3. **Minuten-Vorgabe:** Für Abwehr und Sturm (t = 0, 1) und Reihe k = 0–3:
   `DGROUP 0xCC73 + 8·m + 4·t + k = ⌊Min / 2⌋ | (Min & 1)` (bitweises Oder, k < 3) bzw. `⌊Min / 2⌋` (k = 3) aus den Minutenfeldern
   `DGROUP 0xCC43 + 8·m + 4·t + k` (je Teil zusammen höchstens 20, Datei 0x4889F–0x488CE). Dann auf Summe 8 bringen:
   bei Summe > 8 von Reihe 4 rückwärts −1 (nur Werte > 1), sonst +1 (nicht Reihe 4), reihum bis die Summe 8 ist.
4. `+0x2D` = Grundwert, `+0x26` = Grundwert + Zuschlag „Unentschieden“ (`+0x29`); beim Öffnen holt Datei 0x48003 den
   Grundwert aus `+0x2D` zurück. Spielstand FILE06 (Köln `+0x26` 48 → 55) passt dazu.
5. Einstellung „automatisch aufstellen“ `DGROUP 0x375 + 2·m`: der Bildschirm arbeitet mit dem umgekehrten Wert (Datei
   0x47F38), hier zurückgedreht; wurde sie im Bildschirm eingeschaltet, folgt sofort die Neuaufstellung Datei 0x426BC.

**Remake:** Die Spielberechnung (Datei 0x8EF2, Modus 4) vergleicht den Minutenzähler der Reihe mit `DGROUP 0xCC73`
(Datei 0x09140), also mit den Werten aus Schritt 3 (Summe 8). `MatchEngine.Managed.cs` (Reihenwechsel, Modus 4) nimmt
`Tactics.Minutes` direkt (0–60, `MaxMinutes [APPROX]`); Schritt 3 und die Grenze 20 je Teil fehlen. Schritt 2 und 4 sind
im Remake schon so (`Squad.Tactics.cs`).

## Jugendteams (Ov 0xF8)

Managersatz `DGROUP 0xA075 + 0x15·m`: Stärke je Team `+0…2`, Platz `+3…5` (0–11), Liga `+6…8` (0 = höchste), Förderung
`+9` (3 Langwörter, Drittel-DM) – das ist der bekannte Satz `DGROUP 0xA078` um drei Byte nach vorn erweitert. Ligabezug
`DGROUP 0x4EBA` = 85, 70, 57.

### Datei 0x39DEC – Ligaspieltag

Aufruf aus dem Tagesablauf Datei 0x54C9F je menschlichem Manager an Tagen, deren Kalenderbyte `DGROUP 0x828 + Tag` ein Bit
0–2 hat (Datei 0x55008–0x5501C; Deutung: Ligaspieltag) – **nicht monatlich**. Ablauf:

```
T[0..43]: a = 4; je k: T[k] = a; if a > 0 and random(0, 8) == 0: a -= 1      // neu bei jedem Aufruf
für Team t = 0..2:
  F = Förderung[t] / 3                                   // DM
  s = Stärke[t]; p = Platz[t]; ref = DGROUP_4EBA[Liga[t]] − p
  X = (F · 100 / (10000 · 2^t)) / 2 + 40                  // 40 … 90
  Y = X · 100 / s
  d = sign(Y − 100); if random(0, 4) == 0: d = random(0, 2) − 1
  if random(0, 30) < |Y − 100|: e = d else e = 0
  if random(0, 2) == 0: s += e
  s = clamp(s, 10, 85); Stärke[t] = s
  g = s − ref; d = sign(g); if random(0, 4) == 0: d = random(0, 2) − 1
  if random(0, 3) != 0 and random(1, 18) > 2 · |g|: d = 0
  p −= random(1, T[DGROUP_7A9]) · d                       // 7A9 = nächster Spieltag 1. Liga
  Platz[t] = clamp(p, 0, 11)
```

Die Stärke strebt also gegen X (volle Förderung 90, keine 40), der Platz gegen den Bezug der Liga; die Schrittweite nimmt
über die Saison zufällig ab. Alle Ganzzahl-Divisionen vorzeichenbehaftet, Reihenfolge der Zufallszahlen wie oben.

### Datei 0x3A13B – Saisonwechsel

Aufruf je menschlichem Manager im Saisonwechsel (Datei 0x55889). Für t = 2, 1, 0 (absteigend):

```
p = Platz[t]; neu = 5; l = Liga[t]
if p < 2 and l > 0:  l -= 1; neu = p + 10      // Platz 1–2 steigt auf, startet als 11./12.
if p > 10 and l < 2: l += 1; neu = p − 10      // Platz 12 steigt ab, startet als 2.
if t > 0: Stärke[t] = (2 · Stärke[t] + Stärke[t − 1]) / 3   // jüngeres Team rückt nach
Platz[t] = neu; Liga[t] = l
```

Sonst startet jedes Team auf Platz 6 (Byte 5).

### Abgleich mit dem Remake

`Core/FinanceYouth.cs`: `SettleYouth` (monatlich, Zufall nach Förderanteil, `[APPROX]`) ersetzt beide Funktionen; es
gibt keine Jugendstärke, keine Auf- und Absteiger und keinen Neustart auf Platz 6. Die Monatsbuchung der Förderung gehört
nicht zu diesen Funktionen (Ausgaben Datei 0x3E811, schon umgesetzt). Ob die Stärke weiter wirkt (Talentsuche,
Jugendspieler), zeigen die Zugriffe auf `DGROUP 0xA075` außerhalb dieser Funktionen – nicht Teil der Spur, offen.

## Offen

- Kalenderbyte `DGROUP 0x828`: Bit 0–2 als Ligaspieltag ist Deutung aus 0x54E96/0x55008; zu belegen mit `docs/tagesablauf.md`
  bzw. der Kalenderanalyse (Spur `re-meldungen`).
- Videostelle „Straße 1.200.000 DM“ gegen K = 1.300.000 DM.
- Weitere Leser von `DGROUP 0xA075` (Jugendstärke) und `DGROUP 0xCC73` außerhalb der Spielberechnung (Laden/Speichern
  Datei 0x5BDC0, 0x5C7A4 schreiben nur).

## Kandidaten Runde 11

| Nr. | Prio | Ziel | Dateien | Testidee |
|---|---|---|---|---|
| G1 | P1 | Stadionausbau wie Datei 0x3D7F8/0x3D316: Kostentabelle K, Bauzeit `random(lo, hi)` + `random(0, 4) − 2`, parallele Baustellen je Projekt, Platzgrenze 25.000 zusammen, Schritt 500, Rabatt und „beleidigt“-Sperre (Tageszufall 1/11), Konto direkt belasten (Entscheidung Nutzer: Finanzbuch „Bau“ behalten?) | `Core/FinanceStadium.cs`, `UI/Screens/Manager/StadiumScreen.cs`, Spielstand-Migration (Baustellenliste) | Kosten je Projekt/Stufenzahl gegen Tabelle; zwei Baustellen zugleich; Nein → Sperre bis Zufall; Restwochen in `[lo−2, hi+2]`; Mehrsaisonprobe mit Ausbau |
| G2 | P1 | Jugend wie Datei 0x39DEC/0x3A13B: Stärke je Team, Platzlauf je Ligaspieltag, Auf-/Abstieg und Neustart im Saisonwechsel | `Core/FinanceYouth.cs`, `GameState.Integration.cs` (Anschluss Spieltag/Saisonwechsel), Migration (Stärke) | Mit festem Zufall: volle Förderung → Stärke steigt Richtung 90 (Deckel 85); Platz 1 → Aufstieg auf Platz 11; Platz 12 in Liga 2 bleibt; Mehrsaisonprobe 4 Saisonwechsel |
| G3 | P1 | Börse/Bank: Gebühr 1,5 % beim Kauf und Verkauf, Kaufpreis beim Verkauf über ganze Prozent, Kreditrahmen Bank 2 Mio. DM, Grenze 1 Mio. DM zwischen Managern | `Core/FinanceStocks.cs`, `Core/FinanceBank.cs`, `Core/Finance.cs` | Kauf 10 Stück zu 100 DM kostet 1.015 DM; Verkauf mit Restanteil; Kredit bis 2 Mio. möglich, darüber nicht |
| G4 | P2 | Betragsbalken Datei 0x58409 statt ±-Schritten (Pixel · `max/154+1`, 1.000-DM-Rundung, Vorgaben je Auswahl); Leihen bei Mitmanagern sofort (Entscheidung Nutzer, heute Anfrage) | `UI/Screens/Manager/FinanceScreen.cs` | Klick auf Pixel s ergibt `min(⌊s·q/3000⌋·3000, max)`; Vorgabe = Sparbuch bzw. Forderung |
| G5 | P2 | Kontoleiste im Transfermarkt mit Schulden, Anlagen, Saldo (Regel Datei 0x463CC) | `UI/Screens/Team/TransferScreen.cs` (Werte aus vorhandenen Finanzdaten) | Mit Kredit und Aktien: Schulden/Anlagen/Saldo wie im Finanzen-Bildschirm |
| G6 | P2 | Minuten-Vorgabe wie Datei 0x48E62/0x8EF2: Minuten je Teil ≤ 20, Werte `⌊Min/2⌋ \| (Min & 1)` auf Summe 8 gebracht, Spielberechnung zählt gegen diese Werte | `Core/Squad.Tactics.cs`, `Core/MatchEngine.Managed.cs`, Taktikbildschirm | 7/7/6/0 → 3/3/3/0 → 3/3/2/0; 5/5/5/5 → 3/3/3/2 → 3/2/2/1; Spiel: Wechsel nach so vielen Zählerschritten |
| G7 | P3 | Stadiontabelle Datei 0x3D45F pixelgenau („(Noch n Wochen)“, „(kein Bau...)“, Farben 31/1, Lagen) | `UI/Screens/Manager/StadiumScreen.cs` | Bildschirmfoto gegen Video, sobald G1 die Daten liefert |
| G8 | P3 | Lage/Farben der Finanzübersicht Datei 0x57ECF, 0x57CFB, 0x581C2 nachmessen | `UI/Screens/Manager/FinanceScreen.cs` | Pixelvergleich `boerse-start` |
