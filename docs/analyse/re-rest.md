# Analyse em.exe: übrige unbekannte Funktionen (Spur `re-rest`, Runde 10)

Alle Adressen sind **Dateioffsets** von `em.exe` (Basissegmente: 0x1E00 + Segment · 16 + Offset; Overlays: 0x255C0 +
Versatz aus dem Stub, siehe `docs/reverse-engineering.md`). DGROUP-Variablen stehen immer mit dem Wort „DGROUP“ davor.
Fernaufrufe heißen wie im Original `Selektor:Offset`. Beleg ist, wo nicht anders gesagt, die Disassembly (`ndisasm -b16`
über die Bereiche der Funktionskarte, Base `664ff18`; Hilfsskripte und Ausgaben nur im Scratch). Spielstände und
DOSBox-Bilder wurden in diesem Lauf nicht benutzt. Strukturen: Managersatz DGROUP 0x965B + 0x1AF · Manager, Kaderplatz
DGROUP 0x5DC3 + 0x618 · Satz + 0x34 · Platz, Spielerpool `far:8253` (0x21 je Spieler), Vereinssatz `far:9657` (0x3D je
Verein), Nachrichten DGROUP 0xD294 (24 × 0x71). Hilfen der C-Laufzeit: 0x0390B `strcpy`, 0x0389C `strcat`, 0x03934
`strlen`, 0x0398B `strncpy`, 0x038DB `strcmp`, 0x02658 `time`, 0x024CE `srand`; random(a, b) ist Datei 0x0C037.

## Übersicht

53 Funktionen, 6.989 Byte. „Remake“ nennt die Entsprechung, „–“ heißt: keine eigene nötig.

| Funktion | Rolle | Remake | Kandidat |
|---|---|---|---|
| Datei 0x064C9 | n-ter Kalendertag mit Bitmaske | `Season.DaysOf`, `LeagueMatchDays` | – |
| Datei 0x06C9C | setzt DGROUP 0x175 = 1; ohne Aufrufer | – (toter Code) | – |
| Datei 0x06CDF | Smiley mit Rahmen aus einem Bildstreifen | `Widgets`-Smileys | – |
| Datei 0x06FA5 | Talentsuche: Ablauf und Ergebnis | `FinanceYouth.AdvanceTalentSearch` [APPROX] | **K1 (P2)** |
| Datei 0x0AD7A | Summen der Aufstellung; ohne Aufrufer | – (toter Code) | – |
| Datei 0x255C0 (Stub 0xC0 Einsprung 0x20) | Sound Blaster digital erkennen | Audio (MonoGame) | – |
| Datei 0x255FF | auf Ende der digitalen Ausgabe warten; ohne Aufrufer | – | – |
| Datei 0x2592F | EMS-Bildcache: Eintrag freigeben | – (alles im Speicher) | – |
| Datei 0x25A0E | Schriften laden (newbold, flow, mini) | `Assets`-Fonts | – |
| Datei 0x25AF9 | eigene Zufallsfolge vorspulen | – | – |
| Datei 0x25B31 | `srand(time(0))` | – | – |
| Datei 0x28030 | Vereinsauswahl: 12 Logos einer Seite | `SetupScreen.DrawClubPicker` | K5 (P3) |
| Datei 0x280DA | Markierung eines Managerfelds (Spalte, Zeile) | `SetupScreen` | – |
| Datei 0x28111 | zwei Striche oben/unten (36 breit, 39 auseinander) | `SetupScreen` | – |
| Datei 0x2A5F5 | „:“ und rechtsbündiger DM-Betrag | Finanz-/Stadionbildschirme | – |
| Datei 0x2A6A2 | Werbeflächen zeichnen (Maske) | `AdvertisingScreen` | K4 (P3) |
| Datei 0x2B008 | Rahmenlinien aus einem Bild zurückkopieren | – (Neuzeichnen) | – |
| Datei 0x3183C | Live-Tore der Pokal-/WM-Seiten ablegen | – (Livespiel) | → `livespiel` |
| Datei 0x34872 | Verein in der Liste der deutschen EC-Teilnehmer? | `Cups` (Europapokal) | – |
| Datei 0x34B3E | Ergebniszeile einer DEB-Runde sichern | `CupsDeb` | – |
| Datei 0x34F7D | Ergebniszeilen und Pokalablage leeren | `Cups` | – |
| Datei 0x34FF9 | Überschrift „BUNDESLIGA GEGEN …:“ | `DebCupGamesScreen.GroupTitle` | – |
| Datei 0x35CBE | Hintergrund in drei Farbstreifen (Penaltyschießen) | – (Livespiel) | → `livespiel` |
| Datei 0x35D5D | Banner „Ligaspiel“ usw. | `SeasonInfo.DrawBanner` | K3 (P3) |
| Datei 0x36146 | Wert a ↔ b in einem Byte tauschen | – | – |
| Datei 0x367B1 | Tageszähler → Tag, Monat, Jahr | `Season.LayoutDate`/`Format` | – |
| Datei 0x369C7 | gewertete Spiele in die Ergebnisse schreiben | `MatchEngine.Live` (#165) | – |
| Datei 0x36AAA | Spiel werten (Marke 0x5A, Wertung merken) | `MatchEngine.Live` (#165) | – |
| Datei 0x37A4A | „.“ anhängen | – | – |
| Datei 0x3CA71 | WM-Kader: Spieler entlassen | `NationalCoach.Remove` | – |
| Datei 0x3CCEB | Spieler im Besitz eines Managers zählen | – | K1 (P2) |
| Datei 0x40290 | Kopfzeile Verein links, Manager rechts | fehlt (nur im Eingriff) | → `livespiel` |
| Datei 0x431CB | kb\248 laden und Ton „Aus d. letzten Spielen:“ starten | Audio | – |
| Datei 0x432C4 | Ton beenden, Puffer freigeben | Audio | – |
| Datei 0x435CD | Grafik beim Programmstart einrichten | Desktop/`Surface` | – |
| Datei 0x436DD | Platzierungsfolgen (WM-Statistik) leeren | `WorldCupStatistics` | – |
| Datei 0x45CD0 | Verein in den DEB-Paarungen der Runde? | – | K7 (P3) |
| Datei 0x47BCD | Textspalte als Bildpuffer | `TacticsScreen` | – |
| Datei 0x47CA0 | Zeiger auf Taktik-Einstellung k | `Squad.Tactics` | – |
| Datei 0x4B0F0 | DGROUP 0xE25E = 10.000 (nur geschrieben) | – | – |
| Datei 0x4B101 | freier Nachrichtenplatz | `Inbox` (unbegrenzt) | K6 (P3) |
| Datei 0x4B127 | Nachrichten sortieren | `Inbox` | K6 (P3) |
| Datei 0x4B270 | Nachricht eines Spielers löschen | `Inbox` | K6 (P3) |
| Datei 0x4B2BA | Nachricht mit zwei Zeilen an den Manager am Zug | `GameState.Notify` | – |
| Datei 0x4B9CD | nächste/vorige Nachricht des Managers | Posteingang | – |
| Datei 0x4BD7E | Großbuchstaben-Kopie mit Umlauten | `ToUpperInvariant` | – |
| Datei 0x4C6B6 | Kaderplatz eines Poolspielers suchen | Objektverweise | – |
| Datei 0x4CCB4 | Zeile Auf-/Absteiger/Meister am Saisonende | `SeasonEndScreen.DrawRows` | **K2 (P3)** |
| Datei 0x59D71 | Trainingsbildchen k | `TrainingScreen.KindIcon` | – |
| Datei 0x5A820 | Text mit XOR entschlüsseln (Passwortabfrage) | – (kein Kopierschutz) | – |
| Datei 0x5AA34 | Spielername aus der Namensdatenbank | `Transfer.Pool` | – |
| Datei 0x5AC9B | leere Funktion | – | – |
| Datei 0x5E8B0 | Text auf Höchstbreite kürzen und zeichnen | `SaveLoadScreen` | – |

## A. Basissegmente

### Basis 0x238 und 0x62C

- **Datei 0x064C9** (33 B, `0x238:0x...`, Aufrufer 0x05CA0, 0x4E8A3). Argumente (Maske, n). Zählt i ab 0 über die
  Kalendertabelle DGROUP 0x828 + i und liefert den Index des n-ten Tages mit `Tabelle & Maske ≠ 0` (Maske 7 = Ligaspieltag
  usw., Werte in `docs/saison.md`). Index 1 ist der 10. September (DGROUP 0x829 = Datei 0x175F9). Remake:
  `Season.DaysOf`/`LeagueMatchDays` (Tag 0 = 10. September). Kein Kandidat.
- **Datei 0x0AD7A** (177 B). Kein Aufrufer (weder Fern- noch Nahaufruf noch Zeiger `0x2CBA:0x062C` im Bild). Für den
  Manager DGROUP 0xA70E: über die belegten Kaderplätze (Zahl aus 0x4BE17 Modus 0) mit `+0x0A` in 1 … 11 Summen von
  `+0x10`, `+0x11`, `+0x13` in drei Ausgabewörter, Mittel von `+0x13` (E) in ein viertes. Toter Code, kein Kandidat.

### Basis 0x46E – Smiley, Talentsuche

- **Datei 0x06C9C** (10 B): `DGROUP 0x175 = 1`. Kein Aufrufer, DGROUP 0x175 wird nirgends gelesen. Toter Code.
- **Datei 0x06CDF** (160 B, Aufrufer 0x06CA6, 0x26F20; ruft Kopie 0x139C9). Argumente (Bild far, x, y, k): Rahmen 30 × 27
  aus der Quelle (144 | 0) nach (x | y), dann Gesicht 24 × 21 aus Spalte k mod 6 (x = 24 · Spalte) und Zeile k div 6
  (y = 21 · Zeile) nach (x + 3 | y + 3), Maske 0xC0. Die Smiley-Bausteine des Remakes (`Widgets`) zeichnen dasselbe.
- **Datei 0x06FA5 – Talentsuche** (228 B, Aufrufer Tagesfunktion 0x54C9F an Datei 0x5528A; ruft random 0x0C037,
  Talent anlegen 0x06D7F, Nachricht 0x4B2BA).
  - **Takt:** 0x54C9F ruft die Funktion, wenn der Tageszähler DGROUP 0x3B7 (32 Bit) mod 7 = 0 **oder** (Zähler + 3) mod 7 = 0
    ist (Datei 0x55253–0x5528A, Rest über 0x0228B) – also zweimal je Woche.
  - **Daten:** Zähler je Manager im Managersatz `+0xEC` (DGROUP 0x9747 + 0x1AF · Manager). Einziger Schreiber ist die
    Transferliste 0x465F1 (Datei 0x46A10): Zähler = random(3, 10), mit random(0, 3) = 0 zusätzlich + 6; dort wird auch
    der Preis vom Konto abgezogen (Datei 0x469CA).
  - **Regel** je Manager m < DGROUP 0x386 (DGROUP 0xA70D/0xA70E = m): Zähler 0 → nichts. Sonst Zähler − 1; ist er danach
    ≠ 0 → weiter (der Schalter DGROUP 0x4860, der das sofortige Ende erlaubte, ist immer 0, `docs/transfer.md`).
    Bei 0: Erfolg, wenn random(0, 5 − ⌊DGROUP 0xED8 / 2⌋) = 0 (DGROUP 0xED8 = 5 − Level; Chance 1/4 bei Level 0–1, 1/5
    bei 2–3, 1/6 bei 4–5).
    - Misserfolg: Nachricht „Ihr Talentspäher gibt die“ / „Suche ohne Erfolg auf... Pech...“ (Zeiger DGROUP 0xC98/0xC9C,
      Art 2).
    - Erfolg: 0x06D7F(m, random(40, 90), 80, 0) legt das Talent an; liefert es 0 (der Manager besitzt schon 28 Spieler,
      Zählung 0x3CCEB), gibt es **keine** Nachricht. Sonst „Ihre Talentsuche war ein Erfolg.“ / „Ein junger Spieler
      stößt zu Ihrem Kader.“ (DGROUP 0xC90/0xC94).
  - **Talent (0x06D7F, nur gelesen):** zufälliger Poolspieler mit Besitzer `+0x1A` = 6, Alter random(18, 21), `+0x17` =
    random(45, 55), `+0x16` und `+0x15` je random(s − 5, s + 1) mit s = random(40, 90).
  - **Remake:** `FinanceYouth.AdvanceTalentSearch` – wöchentlich, 4 Wochen (`[OFFEN]`), Chance 40–85 % nach Jugendplatz
    (`[APPROX]`), Grenze 30 Spieler im Kader (`[OFFEN]`), Talentwerte aus der Vereinsstärke. Alle vier Punkte weichen ab
    → **K1**.

## B. Overlay 0xC0 – Programmrahmen

- **Datei 0x255C0** (Stub 0xC0 Einsprung 0x20, Anfang des Overlays; 45 B, Aufrufer Programmstart 0x437D5): Sound Blaster digital. 0x13FEE (DSP-Port DGROUP 0x59E8,
  Rückgabe Bitmaske, Bit 2 = Karte) → 0x13EB4 (IRQ 2/3/5/7 durch Testinterrupt ermitteln) → 0x1416F (Rückgabe 0). Dann
  DGROUP 0x4A44 = 1 und Rückgabe 1, sonst 0. Der Tonteil 0x567C8 fragt DGROUP 0x4A44 ab.
- **Datei 0x255FF** (20 B, kein Aufrufer gefunden): bei DGROUP 0x4A44 ≠ 0 warten, bis DGROUP 0x59E4 (laufende Ausgabe)
  0 ist.
- **Datei 0x2592F** (96 B, Aufrufer 0x2598F, Räumen des Bildcaches): Eintrag (far, 20 Byte) ≠ 0 → Puffer `+0x0A`
  freigeben (0x0C789), DGROUP 0xDE2B = `+0x0E`, DGROUP 0xDE31 = `+0x06` (32 Bit), Eintrag freigeben; Rückgabe 1. Ergänzt
  den EMS-Bildcache in `re-meldungen` (0x25764).
- **Datei 0x25A0E** (97 B, Aufrufer 0x435CD): für i = 1 … 3 Dateiname aus der Zeigertabelle DGROUP 0x49BA + 4 · i
  („newbold.font“, „flow.font“, „mini.font“) nach DGROUP 0xDE51, laden (0x13124, Rückgabe far-Zeiger), Zeiger nach DGROUP 0xDE65 +
  4 · i; fehlt eine, Rückgabe 0 (0x435CD meldet dann Fehler 3 über 0x25B4A).
- **Datei 0x25AF9** (56 B, Aufrufer 0x37A62 „Spiele laufen“, 0x437D5): random(100, 300)-mal die eigene Zufallsfolge
  0x0BFD5(0, 1) aufrufen. **Datei 0x25B31** (25 B, Aufrufer 0x437D5): `srand(time(0))`. Remake: eigene Zufallsquellen
  (`MatchEngine.Original`), Vorspulen ohne Wirkung auf die Regeln. Kein Kandidat.

## C. Overlay 0xD8 – Spielstart (Aufrufer 0x28165)

- **Datei 0x28030** (170 B, Aufrufe Datei 0x282D7, 0x28E6E): Argumente (Seite p, Ziel far). Für i = 0 … 11 Vereinssatz
  `far:9657` + 0x3D · (12p + i): Logo kb\(80 + (Feld 0x34 & 0x3F)) laden (0x0C17D), 38 × 38 nach (40 · (i mod 3) |
  40 · (i div 3)) ins Ziel kopieren, Puffer freigeben (0x0BEFA). Der Aufrufer blättert p zyklisch 0 … 3 (Datei
  0x28E4E–0x28E64). Die Seiten zeigen also die Sätze 0–47 **in Satzreihenfolge**. Remake: `SetupScreen.ClubCell`
  (Raster 40, 38 × 38) stimmt; `ClubAt` trägt `[APPROX]` „im Original nicht nach Index sortiert“ → **K5**.
- **Datei 0x280DA** (55 B, Aufruf Datei 0x28AE8): (Farbe, Spalte s, Zeile z) → bei s > 0 und z ≠ −1 0x28111((s − 1) ·
  132 + 7, 51 · z + 40, Farbe). **Datei 0x28111** (84 B, auch direkt Datei 0x28B0B/0x28B3C): (x, y, Farbe) → Linien
  (x | y)–(x + 35 | y) und (x | y + 39)–(x + 35 | y + 39) über 0x0B7B5. Markierung von Kopf (Spalte 1, x = 7) bzw. Logo
  (Spalte 2, x = 139) des Managers in Zeile z (Abstand 51 = `SetupScreen.SlotPitch`). Kein Kandidat.

## D. Overlay 0xE0 – Finanzen, Werbung

- **Datei 0x2A5F5** (173 B, Aufrufer 0x2AE8A Finanzübersicht, 0x3D45F Stadionbau): (Betrag 32 Bit, x₁, x₂, y, Farbe):
  „:“ (DGROUP 0x4CF6) bei x₁, dann Betrag als Geldtext (0x0BAC8) + „ DM“ (Zeiger DGROUP 0x14C9) rechtsbündig bis x₂
  (Breite 0x13208), ohne Schatten. Die Bildschirme des Remakes richten ihre Beträge schon so aus.
- **Datei 0x2A6A2** (779 B, Aufrufer Werbebildschirm 0x2B0C0): (Maske, Logos far, Hintergrund far). Satz DGROUP 0xC15B +
  0x6E · Manager, 10 Byte je Fläche, `+0` 32 Bit ≠ 0 = belegt, `+7` Bildnummer. Je gesetztem Bit, Kopie Maske 0xC0:
  - Bit 0 Banden 0–6: belegt 33 × 23 aus (1 | 25 · Bild + 1) nach (40 + 35 · i | 35), frei derselbe Ausschnitt aus dem
    Hintergrund.
  - Bit 1 Trikot (Fläche 7, `+0x4D`): belegt **31 × 26** aus einer eigenen Spalte – Bild ≤ 7: (70 | 26 · Bild), sonst
    (39 | 130 + 26 · (Bild − 8)) – nach **(30 | 120)**; frei 33 × 23 bei (30 | 122) aus dem Hintergrund.
  - Bit 2 Fernsehen (Fläche 8, `+0x57`): 33 × 23 aus (36 | 25 · Bild + 1) nach (11 | 173).
  - Bit 3 Eistafeln 9/10: 33 × 23 aus (1 | 25 · Bild + 1) nach (268 | 92 + 36 · i).
  Remake: Banden, Fernsehen (11 | 173) und Tafeln (268 | 92/128) stimmen; das Trikot zeichnet das Firmenlogo bei
  (31 | 116) → **K4**.
- **Datei 0x2B008** (184 B, Aufrufer 0x2B0C0): (Quelle far, x₁, y₁, x₂, y₂, Warten, Ziel far): auf den Strahlrücklauf
  warten (0x0B8FE), dann die vier 1-Pixel-Kanten des Rechtecks aus der Quelle zurückkopieren – löscht einen
  Auswahlrahmen. Remake zeichnet neu, kein Kandidat.

## E. Overlay 0xE8 – Pokale

- **Datei 0x3183C** (248 B, Aufrufe im Seitenwähler 0x3A88A, Datei 0x3A96F, 0x3AAF3, 0x3AC21; nur bei Minute ≠ 0
  und ≠ 60): (Wettbewerb w, Start b, Gruppenabstand c, Spieltag t ≤ 5). Für Gruppe g < 2 (w = 3: nur eine) und k = 0, 2, 4
  Heim- und Gasttor aus der Ergebniszeile 3 (DGROUP 0x5CAB/0x5CAC + b + k + c · g) nach DGROUP 0xCDB8 + 0x48 · w +
  0x24 · g + 6 · t + k. Aufrufe mit (0, 0, 6, DGROUP 0x7AE) = WM-Gruppen, (1, DGROUP 0xCFFA, 4, …) und (2/3, 0, 4, …).
  Damit zeigen die Paarungsseiten während der Spiele den laufenden Stand. Gehört zur Live-Anzeige → `livespiel`.
- **Datei 0x34872** (43 B, Aufrufer 0x26F20, 0x346D5): 1, wenn der Verein in der Liste DGROUP 0xA776 (Länge DGROUP 0xA775,
  gefüllt von 0x346D5 mit Meister und Titelverteidiger, `docs/wettbewerbe.md`) steht.
- **Datei 0x34B3E** (56 B, Aufruf Datei 0x34B91 im DEB-Pokal 0x34B76): 32 Byte der Ergebniszeile DGROUP 0x5C4B nach DGROUP
  0xD014 + 32 · DGROUP 0xD074 (Ablage der Runde). **Datei 0x34F7D** (88 B, Aufrufer 0x34B76 bei Runde < 5, 0x437D5,
  0x54C9F an Datei 0x565EE): DGROUP 0x5C4B (4 × 32) und DGROUP 0xD014 (3 × 32) auf 0. Remake: Pokalobjekte, kein Kandidat.
- **Datei 0x34FF9** (189 B, Aufruf Datei 0x33491 in 0x32E23 „DEB-Pokalspiele“): (Gruppe k, Ziel far) →
  „<A> GEGEN <B>:“ in Großbuchstaben (0x4BD7E), A = Bundesliga bei k < 4, Zweite Liga bei k < 6, sonst Oberliga;
  B = Bundesliga bei k = 1, Zweite Liga bei k = 2 oder 4, sonst Oberliga (Namen DGROUP 0x9E8, „ GEGEN “ DGROUP 0xCCC).
  Remake `DebCupGamesScreen.GroupTitle`/`GroupHeading` stimmt.

## F. Overlay 0xF0 – Spielablauf

- **Datei 0x35CBE** (159 B, Aufrufer Penaltyschießen 0x3901B): 0x134C2(Bildschirm, 11 · DGROUP 0x6AE); bei DGROUP 0x6AE ≠ 0
  (immer 1, `re-meldungen`) drei Flächen über 0x0B620: x 1 … 318, y 1 … 79 Farbe 0, y 80 … 159 Farbe 18, y 160 … 238
  Farbe 24; dann Schrift 1. Hintergrund der Penalty-Anzeige → `livespiel`.
- **Datei 0x35D5D – Banner** (239 B, Aufrufer 0x30365 Freundschaftsspiel, 0x30E7B WM, 0x3139F Europacup, 0x31934
  Play-Off, 0x35E4C DEB-Pokal, 0x35EA4 Ligaspiel, 0x4C747): Bildschirm aus, Fenster 0x0B155(x 4, y 92, 13 × 3 Teile,
  Art 2), Schrift 1, Text zentriert 0 … 319 zuerst in Farbe 0 auf Grundlinie 127, dann in Farbe 11 auf 126 (kein
  seitlicher Schatten), Bildschirm ein; ohne Durchlauf (DGROUP 0x1DB = 0) 60 Takte warten (0x130EE, ≈ 3,3 s), Bildschirm
  aus, alle Fenster zu (127), 0x45D46. Remake `SeasonInfo.DrawBanner`: Schatten (+1 | +1) in Farbe 198,
  `MatchDayScreen.BannerFrames` = 75 → **K3**.
- **Datei 0x36146** (35 B, Aufrufer 0x36169): `*p == a → b, *p == b → a`. Hilfe des Platztauschs in 0x36169.
- **Datei 0x367B1** (222 B, Aufrufer 0x05CA0, 0x31CD5, 0x32E23, 0x44E22, 0x4E8A3, 0x54C9F): Argument 0 = Tageszähler DGROUP
  0x3B7; Wert 2000 → 0. Tag + 252, Jahr = DGROUP 0x3BB; solange > 365: − 365, Jahr + 1; dann Monatslängen DGROUP 0x393
  (zyklisch ab Januar) abziehen. Ergebnis DGROUP 0x5A20 Tag, 0x5A1C Monat (0-basiert), 0x5A18 Jahr. Kein 29. Februar –
  wie `Season.LayoutDate`/`Format` (`docs/saison.md`).
- **Datei 0x36AAA – Spiel werten** (179 B, Aufrufer 0x36B5D an Datei 0x36DE9/0x36FC8 mit Wertung 0 : 2 bzw. 2 : 0,
  0x37A62 an Datei 0x38412): (Art, Liga/Zeile l, Paarung p, Heim, Gast). Ergebnisbyte = 0x5A – Art 0 in der Ligatabelle
  DGROUP 0xA0BF + 0x210 · l + 12 · DGROUP (0x7A9 + l) + p, sonst in der Zeile DGROUP 0x5C4B + 32 · l + p; Eintrag in die
  Liste (höchstens 16, Länge DGROUP 0xA968): Heim DGROUP 0xA928, Gast 0xA918, Liga 0xA958, Spieltag − 1 0xA948, Paarung
  0xA938. **Datei 0x369C7** (227 B, Aufrufer 0x37A62 an Datei 0x37B30, 0x385A0, 0x3861F): (v, Pokal) schreibt für jeden
  Listeneintrag beide Bytes = v bei v = 0x5A, sonst die gemerkte Wertung, in die Ligatabelle (DGROUP 0xA0CB, Pokal = 0)
  bzw. die Zeile DGROUP 0x5C4B. Die Live-Anzeige liest die Liste in 0x51FEA (`re-livespiel`). Remake: Wertung 0 : 2/2 : 0
  ohne Berechnung in `MatchEngine.Live` (#165). Kein Kandidat.
- **Datei 0x37A4A** (24 B, Aufrufer 0x32E23, 0x4D385, 0x4E8A3, 0x4F1F1, 0x4F78D): `strcat(Ziel, ".")` (DGROUP 0x4EA4) –
  Platznummern „1.“ der Tabellen.

## G. Overlays 0xF8, 0x108, 0x110, 0x118

- **Datei 0x3CA71** (78 B, Aufrufer „Spieler in WM-Kader“ 0x3C37C, „auto“ 0x3CA06): (Pool, Satz, Platz) → Verein des
  Poolspielers `+0x1D` = Kaderplatz `+0x0C` & 0x7F, Platz entfernen (0x108:0x48 = 0x425B6, Grenze 29). Remake:
  `NationalCoach.Remove`/`ClearSquad`.
- **Datei 0x3CCEB** (135 B, Aufrufer 0x06D7F, 0x42CB1): Zahl der Spieler im Besitz von Manager m – alle belegten Plätze
  der Sätze 0 … DGROUP 0x386 − 1 und 4 (Markt): eigener Satz zählt ganz, fremde Plätze, wenn Pool `+0x1A` = m (verliehene
  Spieler). Grenze 28 der Talentsuche (K1).
- **Datei 0x40290** (212 B, Aufrufe Aufstellung 0x40364 an Datei 0x40530, Taktik 0x47F2B an Datei 0x480D2, nur bei
  Bildschirm-Argument ≠ 0): Schrift 2, Vereinsname (`far:9657` + 0x3D · Managersatz `+0x14`) bei x 1, Managername
  (DGROUP 0x965B + 0x1AF · m) rechtsbündig bis x 317, Grundlinie 10, Farbe 30, Schatten 0; danach Schrift 3. Das Argument
  ≠ 0 kommt nur aus dem Eingriff im Spiel 0x5434C (über 0x118:0x43 = 0x48E28); aus dem Hauptmenü (0x44E22, Datei
  0x4579C) ist es 0. → `livespiel` (Eingriff).
- **Datei 0x431CB** (249 B, Aufrufer 0x437D5): mit Sound Blaster (0x13FC0, 0x14260) „kb\248.vga“ öffnen (fehlt → Fehler 2),
  36.000 Byte anfordern (fehlt → Fehler 4), lesen, Ausgabe starten (0x142DB … 0x142B2); Rückgabe der Puffer. Der
  vermeintliche Textzeiger „Aus d. letzten Spielen:“ ist der PIT-Takt 0x1234DC (1.193.180), geteilt durch die Abtastrate
  im Kopf (`+0x0C`). **Datei 0x432C4** (40 B):
  Ausgabe beenden (0x14308, 0x1435E), Puffer freigeben. Remake: `EishockeyManager.Audio`, kein Kandidat.
- **Datei 0x435CD** (272 B, Aufrufer 0x437D5): Speicher/Grafik einrichten (0x0D085, 0x0D24F, 0x1093B), Bildschirmpuffer
  DGROUP 0xC157/0xC143/0xC12B, Palette 0xC153, Maus DGROUP 0xC147, Schriften 0x25A0E, Palette schwarz (0x1358F),
  Fenstertabelle DGROUP 0x5A57 (50 Plätze) frei, dann 0x43150. Fehler 0 bzw. 3 über 0x25B4A.
- **Datei 0x436DD** (55 B, Aufrufer Tagesfunktion 0x54C9F, Zweig Argument 0, Datei 0x54CE9): je Satz 0 … 63 die 22 Byte ab
  DGROUP 0x828B + 0x50 · Satz (Feld 0x34 des Satzes 0x8257, Platzierungsfolge der WM-Statistik) auf 0. **Nebenbefund:** der
  in `reverse-engineering.md` („WM-Statistik 0x3ADE1“) als nicht gefunden genannte Schreiber der Platzierungen steht in
  der WM 0x30E7B (Datei 0x311CF–0x31351): erste freie Stelle im Satz des Siegers DGROUP 0x5D5B, bei mehr als 15 alle zwölf
  WM-Sätze um eins verschieben; dann für die zwölf Teams Wert 11 an diese Stelle, danach die Plätze aus der Endrunde.
- **Datei 0x45CD0** (118 B, Aufrufer Spielstart, Datei 0x29CCC und 0x29D76): (Verein, Maske) → 1, wenn der Verein in einer
  der Paarungszeilen k = 1 … 3 mit Bit k − 1 in der Maske steht (DGROUP 0x5CFB + 32 · k, 2 · DGROUP (0x3B1 + DGROUP 0x5CF4)
  Einträge, Tabelle 32, 16, 8, 4, 2, 1 je DEB-Runde). Bei der Wahl des Zufallsvereins verwirft der eine Zweig (Datei
  0x29C78) Vereine **mit**, der andere (Datei 0x29D28) Vereine **ohne** Eintrag in Zeile 1 → K7.
- **Datei 0x47BCD** (211 B, Aufrufer Taktik 0x47F2B an Datei 0x47F91, 0x48299, 0x48BC8): (n, Breite b, Abstand a, Texte
  far[], Start, Farbe, Hintergrund, Schrift) → leerer Bildpuffer b × n · a (0x0C17D Bild 0), Farbe (0x134C2), Schrift;
  n Texte aus der Tabelle ab Start, zentriert 0 … b auf Grundlinie (i + 1) · a − ((a − h) / 2 + 2) mit h = 6 (Schrift 3)
  bzw. 8; Rückgabe des Puffers. **Datei 0x47CA0** (78 B): Zeiger auf Einstellung k des Managers – 0 DGROUP 0x375 + 2m,
  1 DGROUP 0xA701 + m, 2 DGROUP 0xA6FB + m (Einsatz), 3 DGROUP 0xA707 + m (Angriff); sonst 0. Remake `TacticsScreen`,
  `Squad.Tactics`.

## H. Overlay 0x120 – Nachrichten, Texte

Nachrichtensatz (DGROUP 0xD294 + 0x71 · k, k < 24): `+0` Art (Bits 0–2 Manager, Bit 3 = ohne Spieler, 0 = frei),
`+1` Sortierschlüssel, `+2` Poolspieler; Pool `+0x1E` = k + 1 („offene Meldung“, `docs/transfer.md`). Angelegt in
0x4B2F6 (dokumentiert).

- **Datei 0x4B0F0** (17 B, Aufrufer 0x437D5): DGROUP 0xE25E = 10.000 (32 Bit). Kein Leser gefunden.
- **Datei 0x4B101** (38 B, Aufrufer 0x4B2F6): erster freier Satz (`+0` = 0) oder 0xFF.
- **Datei 0x4B127** (329 B, Aufruf Datei 0x54EA9 in der Tagesfunktion): Tauschsortierung, Schlüssel `+1` absteigend; nach
  jedem Tausch zeigt Pool `+0x1E` der beiden Spieler (Art ≠ 0, Bit 3 frei) auf den neuen Platz. Die äußere Schleife läuft
  bis 38, die innere endet bei 24 – ohne Wirkung.
- **Datei 0x4B270** (74 B, Aufrufer 0x07169, 0x26F20, 0x425B6, 0x465F1, 0x4A1DB): (Zeiger auf Pool `+0x1E`) → Wert 1 … 24:
  Satz Wert − 1 leeren (`+0`, `+1/+2`), Zeiger 0.
- **Datei 0x4B2BA** (60 B, Aufrufer 0x06FA5, 0x07089, 0x07169, 0x3CFA0, 0x3D316, 0x3ED01): (Zeile 1, Zeile 2, Art) →
  0x4B2F6 an den Manager DGROUP 0xA70E (setzt DGROUP 0xA70D). Remake `GameState.Notify`.
- **Datei 0x4B9CD** (139 B, Aufrufer Nachrichtenleiste 0x4B5F4): (i, vorwärts) → nächster bzw. voriger Satz mit
  `+0 & 7` = DGROUP 0xA70D, sonst 0xFF.
- **Datei 0x4BD7E** (104 B, sechs Aufrufer): Kopie in Großbuchstaben, a–z und ä/ö/ü (0x84/0x94/0x81 → 0x8E/0x99/0x9A);
  Rückgabe Ziel.
- **Datei 0x4C6B6** (145 B, Aufrufer Saisonende 0x26F20): sucht den Kaderplatz mit `+0x0F` = Pool in den Sätzen
  0 … DGROUP 0x386 − 1, 4 und 5; Rückgabe Zeiger, Satz und Platz, sonst Satz 0xFF.
- **Datei 0x4CCB4** (146 B, Aufrufe im Saisonende-Bild 0x4C747, Datei 0x4CBFD, 0x4CC44, 0x4CC66): (Verein, Art, Spalte s,
  y). Zeichen DGROUP 0x5355[Art] („&“, „@“, „*“) in Farbe DGROUP 0x5358[Art] = **18, 12, 22** bei x = 100 · s + 20,
  Vereinsname in Farbe 1 bei x = 100 · s + 28, beide Schatten 0. Art 0 („&“, Absteiger) auf Grundlinie 156 + 8 · Zeilenzähler
  des Aufrufers, Art 2 („*“, Meister DGROUP 0xD0E6) auf 156, Art 1 („@“, Aufsteiger) auf 176 + 8 · Zeile. Remake `SeasonEndScreen`: Farben 238/13/151, Name
  9 Pixel hinter dem Zeichen → **K2**.

## I. Overlays 0x138, 0x140

- **Datei 0x59D71** (78 B, Aufrufer Training 0x59DBF): (k, x, y) → Bildchen 29 × 20 aus dem zweiten Puffer DGROUP 0xC12B
  bei (26 + 30 · k | 218). Remake `TrainingScreen.KindIcon` stimmt.
- **Datei 0x5A820** (66 B, Aufrufer Passwortabfrage 0x4CD46): Text XOR Schlüssel nach DGROUP 0xE2F8, Rückgabe Zeiger
  (`reverse-engineering.md`, „PASSWORTABFRAGE“). Das Remake hat keinen Kopierschutz; kein Kandidat.
- **Datei 0x5AA34** (108 B, Aufrufer 0x5AAA0, 0x5ACA0, 0x5AF50): (Ziel far = Namensdatenbank, Verein, i) → Satz zu 0x20B
  Byte suchen, dessen Name (`strcmp`) gleich dem Vereinsnamen ist (höchstens 64), Rückgabe Satz + 0x17 + 20 · i (i-ter
  Spielername). Remake: `Transfer.Pool` mit den Datenbanknamen.
- **Datei 0x5AC9B** (5 B, drei Aufrufe über 0x140:0x43 in 0x36B5D): leer (`push bp; mov bp,sp; pop bp; retf`).
- **Datei 0x5E8B0** (131 B, Aufrufer Laden/Sichern 0x5DBE5): Text höchstens 78 Zeichen kopieren, vom Ende kürzen, bis die
  Breite (0x13208) ≤ Höchstbreite ist, dann über 0x0B651 zeichnen. Remake `SaveLoadScreen` (Dateinamen), kein Kandidat.

## Restlücken

- Datei 0x13FEE, 0x13EB4, 0x1416F (Sound-Blaster-Erkennung, Basis 0x11FD) nur so weit gelesen, wie 0x255C0 sie braucht.
- Datei 0x06D7F (Talent anlegen) und 0x465F1 (Start der Talentsuche, Preis) nur für K1 gelesen.
- Sortierschlüssel `+1` der Nachrichten: Bedeutung (Datum oder Vorrang) offen; Schreiber 0x4B2F6 nicht gelesen.
- Zeile 1 der Paarungen beim Spielstart (K7): was dort beim Spielstart steht, nicht verfolgt.

## Kandidaten Runde 11

- **K1 (P2) Talentsuche nach 0x06FA5.** Ziel: Takt zweimal je Woche (Tageszähler mod 7 ∈ {0, 4}), Dauer random(3, 10)
  (+ 6 mit 1/4), Erfolg 1/(6 − ⌊(5 − Level)/2⌋), Grenze 28 eigene plus verliehene Spieler, ohne Platz keine Nachricht,
  Talentwerte wie 0x06D7F. Dateien: `src/EishockeyManager.Core/FinanceYouth.cs`, `Transfer.Talent.cs`,
  `Player.Factory.cs`; Mehrsaisonprobe (Kader). Test: feste Zufallsfolge, Zählerablauf über die Zeitleiste,
  Chance je Level über viele Läufe, voller Kader → keine Nachricht.
- **K2 (P3) Saisonende-Zeilen nach 0x4CCB4.** Ziel: Farben 18/12/22, Name 8 Pixel hinter dem Zeichen, Lage aus den
  Grundlinien 156/164/176/184 (vorher prüfen, ob 238/13/151 im Spiel dieselben RGB-Werte haben). Datei:
  `src/EishockeyManager.UI/Screens/SeasonEndScreen.cs`. Test: Bildschirmfoto, Pixelfarben der Zeichen.
- **K3 (P3) Banner nach 0x35D5D.** Ziel: Farbe 11 auf Grundlinie 126, darunter Farbe 0 eine Zeile tiefer (kein
  seitlicher Versatz), Dauer 60 Takte zu 18,2 Hz. Dateien: `SeasonEndWidgets.cs` (`DrawBanner`), `MatchDayScreen.cs`.
  Test: gerendertes Banner, Pixel unter und neben dem Text; Dauer in Bildern.
- **K4 (P3) Trikotwerbung nach 0x2A6A2.** Ziel: Trikotbild 31 × 26 aus der eigenen Spalte (70 | 26 · Bild) bzw.
  (39 | 130 + 26 · (Bild − 8)) bei (30 | 120), frei 33 × 23 bei (30 | 122). Datei: `AdvertisingScreen.cs`. Test:
  Bildschirmfoto mit Trikotvertrag gegen Video.
- **K5 (P3) Vereinsauswahl nach 0x28030.** Ziel: Seite p zeigt die Sätze 12p … 12p + 11 in Satzreihenfolge (4 Seiten,
  zyklisch); `[APPROX]` in `SetupScreen.ClubAt` auflösen. Test: Reihenfolge gegen die Datenbankreihenfolge.
- **K6 (P3, Entscheidung Nutzer) Nachrichtenspeicher nach 0x4B101/0x4B127/0x4B270.** Ziel: 24 gemeinsame Plätze, voll →
  Nachricht entfällt, Sortierung nach Schlüssel. Datei: `FinanceInbox.cs`. Test: 25. Nachricht, Reihenfolge. Nur, wenn
  das begrenzte Verhalten gewünscht ist; vorher Schlüssel `+1` klären.
- **K7 (P3) Zufallsverein beim Spielstart nach 0x45CD0.** Ziel: Bedingung der Zweige Datei 0x29C78/0x29D28 klären und mit
  dem offenen Punkt `NewGame.PickRandomClub` (`reverse-engineering.md`, „Offen“) zusammen umsetzen. Datei:
  `NewGame*.cs`. Test: Spielstart mit festem Zufall.
- **An `livespiel` übergeben (kein eigener Kandidat):** Datei 0x3183C (laufende Stände der Paarungsseiten), 0x35CBE
  (Penalty-Hintergrund), 0x40290 (Kopfzeile im Eingriff 0x5434C).
