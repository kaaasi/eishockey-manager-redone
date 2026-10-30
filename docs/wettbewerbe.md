# Wettbewerbe (Paket `wettbewerbe`)

Quellen: Disassembly von `em.exe` (Dateioffsets, DGROUP = Datei 0x16DD0), Videos „Die Oberfläche von EM“ (3:30–3:55,
13:08–13:20) und „MEISTER werden in 75 Minuten“ (Infoleisten 12:42–21:48).

Alle Pokalwettbewerbe melden sich über `ISeasonCompetition` in `GameState.Competitions` an
(`Core/Cups.GameState.cs`, aufgerufen aus `GameState.InitializeManagerSystems`). Sie haben eine eigene Zufallsquelle
`GameState.CupRandom`; Liga, Play-Offs und Aufstiegsrunden laufen deshalb mit und ohne Pokal gleich.
Heimspiele gehen über die gemeinsame Spielabrechnung `SettleHomeMatch` (genau einmal je ausgetragenem Spiel).

## DEB-Pokal (`Core/CupsDeb.cs`, `Core/Cups.cs`)

| Regel | Beleg |
|---|---|
| 32 Vereine, Runden mit 32, 16, 8, 4, 2 Vereinen | DGROUP 0x3B1 = `32 16 8 4 2 1` |
| 15 Termine = 5 Runden × 3 (22.9. … 7.4.) | Kalender DGROUP 0x829 |
| Auslosung: zuerst die Vereine aller Manager auf zufällige Plätze 0–31, dann jeden freien Platz mit einem zufälligen Ligaverein (Datenbank 0–35, DGROUP 0x7CB = 35), ohne Doppelte | Datei 0x341AB |
| Paarung = Plätze 2k (links) und 2k+1 (rechts) | 0x33E5E (Anzeige), 0x44D75 (Gegnersuche) |
| Heimrecht: Steht rechts ein Oberligist gegen einen höherklassigen Verein oder ein Zweitligist gegen einen Bundesligisten, tauschen beide die Plätze (nach der Anzeige) | 0x347EF, Spielklasse 0x34FD5 (1 / 2 / 4) |
| Bis zu drei Spiele, wer zuerst zwei gewinnt; drittes Spiel nur bei 1:1 (Markierung 100) | 0x34B76 |
| Nach jedem Spiel tauschen die Vereine die Plätze: Spiele 1 und 3 beim linken, Spiel 2 beim rechten Verein | 0x34B76 (Tausch bei Spiel 0 und 1); Video 1994: 22.9. Heim, 24.9. Auswärts gegen SB Rosenheim |
| Nach dem dritten Termin: Sieger auf Platz 2k, nächste Runde = Sieger der Reihe nach, dann 20 zufällige Vertauschungen | 0x3489D, 0x3494B |
| Pokalsieger nach dem dritten Endspieltermin (Titel „DEB Pokalsieger“, Meisterfoto – im Video 1995: ESV Kaufbeuren nach dem Deutschen Meister) | 0x34ED0, Karriere 17:37 |
| Rundennamen „Erste Runde“, „Achtelfinale“, „Viertelfinale“, „Halbfinale“, „Endspiel“ | DGROUP 0x2576 |
| `[APPROX]` Unentschieden: Original kodiert den Sieger im Ergebnis (+30); hier Penaltyschießen wie in den Play-Offs | – |

### Bildschirm „DEB-Pokal Auslosung“ (`UI/Screens/Competitions/DebCupDrawScreen.cs`)

| Element | Wert | Beleg |
|---|---|---|
| Fenster | 13 × 12 Rahmenteile ab (3 \| 3) = Rect(3, 3, 312, 228) | 0x902:0x335 mit Flag 2 |
| Titel | Bold, zentriert zwischen x = 0 und 321, Grundlinie 26, Farbe 7, Schatten 1 | 0x33C24 |
| Plätze | x = 37 / 164 (DGROUP 0x4DD0), y = 128 − (n/2 · 11)/2 + 11 · Zeile, Streifen kb\25 (120 × 10) | 0x33C92 |
| Namen | Mini, zentriert im Streifen (x 2 … 119), Grundlinie +7, Schatten schwarz, „(1)“–„(3)“ | 0x33E5E |
| Farben | Spielklasse 1/2/3 = 9/10/11 (DGROUP 0x4E17), Manager-Verein 24 | 0x33F22 |
| Hinweis | „TASTE DRÜCKEN“ zentriert zwischen 90 und 237, Grundlinie 221, erst nach der letzten Kugel | 0x34160 |

Textaufruf 0x902:0x885 = (Ziel, Text, x1, x2, Grundlinie, Farbe, Schattenfarbe); Grundlinie = unterste Zeile der
Großbuchstaben (`CompetitionWidgets.Top`).

Pixelvergleich (`emtool screens gamedata ~/work/screens wettbewerbe`, Bild `02_debpokal_video_3_55` mit der Ziehung
aus dem Video gegen `oberflaeche_0355`, eigenes Bild um (−0,25 | +0,5) verschoben und weichgezeichnet, Person und
Mauszeiger ausgeblendet): mittlere Abweichung 8,5 Grauwerte, 3,7 % der Pixel über 40 – nur Kanten der Schrift.
Leere Ziehung (`03_debpokal_leer`) gegen `oberflaeche_0345` ohne Dialog: 9,4 / 2,2 %.

### Live-Auslosung im Spielablauf (`UI/Screens/Competitions/DebCupDrawLive.cs`, Spur `debpokal-auslosung`)

| Regel | Beleg |
|---|---|
| Die Anzeige 0x33B83 gehört zur Auslosung selbst und läuft vor dem Ziehen: erste Runde 0x341AB beim Spielstart (Aufruf 0x43A8C; Video 3:40 direkt nach „Los jetzt!“) und beim Saisonwechsel (0x56609, zwischen WM-Gruppen 0x5644E und Bestenliste 0x56647); Runden 2–5 in 0x3489D nach dem dritten Termin der vorigen Runde (Aufruf 0x34F64) | Datei 0x341AB/0x3420A, 0x3489D/0x348D1 |
| Keine Anzeige ohne DEB-Pokal (Modus „nur Weltkugel“ DGROUP 0xD0B9 = 1, DEB-Pokal aus DGROUP 0x227 = 0) | 0x33BAB |
| Fenster mit leeren Streifen für alle Plätze, dann Frage „An Alle:“ – „Sind Ihre Nerven stark genug, / um die DEB-Pokal-Auslosung / live zu verfolgen ?“, Knöpfe „Aber klar“ / „Kein Gedanke“ | 0x33D67 (Texte DGROUP 0x12F1, 0x12F5, 0x1329, 0x132D, 0x1331); Video 3:40–3:46 |
| „Kein Gedanke“: Fenster zu, keine Übersicht; „Aber klar“: Plätze in zufälliger Reihenfolge einzeln aufdecken, „TASTE DRÜCKEN“, danach „DEB-Pokalspiele“ | 0x33DB1, 0x340C7 (0x34141, 0x34188 → 0x32E23); 0x34521, 0x349E3; Video 3:50–4:00 |
| Weniger als vier Plätze (Endspiel): kein Fenster, keine Frage, nur die Übersicht | 0x33BC3 → 0x33E54 |
| Im Durchlauf ohne Halt (DGROUP 0x1DB) entfällt die Frage – gibt es im Remake nicht | 0x33D60 |
| `[APPROX]` Aufdeckreihenfolge aus eigener Quelle (Original: random(0, n − 1) aus dem gemeinsamen Generator, also andere Folgeergebnisse mit Anzeige); Paarungen und alle Ergebnisse sind hier mit und ohne Anzeige gleich | – |
| `[APPROX]` Ein Platz je 60 Bilder (Video etwa einer je Sekunde), Klick oder Taste deckt alle auf; rollende Kugel fehlt; Enter/Leertaste = „Aber klar“, Escape/rechte Maustaste = „Kein Gedanke“ | – |
| `[APPROX]` Im Endspiel deckt das Original zwei Plätze ohne Fenster auf (0x349E3 mit DGROUP 0xE068 = 1); hier nur die Übersicht | – |

Fragefenster vermessen am Video (`emtool screens gamedata <ziel> debpokal-auslosung`, Bild `01_frage_spielstart` gegen
`oberflaeche_0346`): Rahmen orange x 77–240, y 109–181 wie im Video; „?“-Felder, Überschrift, Zeilen und Knöpfe beste
Verschiebung (0 | 0); mittlere Abweichung im Kasten 12,7 Grauwerte, 8,4 % der Pixel über 40 (Schriftkanten der Zeilen).
Weitere Vorlagebilder seit den Reihen `folge01` … `folge29` und `videos-roh-2` (`docs/referenz-videos.md`): Frage
`folge01_00217`, `folge08_00102`, `duell_s2a_02651`, `eisbaeren_f51_01451`; „DEB-Pokalspiele“ mit Ergebnissen
`folge13_02545`, `duell_s2a_01217` und in allen Runden bis zum Endspiel (`eisbaeren_f60_02758`).

## Europapokal (`Core/CupsEurope.cs`, `Core/CupsGroup.cs`)

| Regel | Beleg |
|---|---|
| Acht Plätze aus den Europapokal-Gegnern (Datenbank 64–149) ohne Doppelte; Gegner mit Stärke unter 70 nur zu einem Viertel angenommen | 0x341AB (Wettbewerb 1) |
| Deutsche Teilnehmer: Deutscher Meister (zu Spielbeginn Verein 2) und Titelverteidiger, wenn deutsch (zu Spielbeginn 64 = kein deutscher Verein); jeder ersetzt einen zufälligen Platz eines ausländischen Gegners | 0x346D5, 0x43A12, DGROUP 0x388 |
| Gruppe 1 = Plätze 0–3, Gruppe 2 = 4–7; Endrunde = Erster und Zweiter von Gruppe 1, dann von Gruppe 2 | 0x30427 |
| Spielplan Vierergruppe „abcd / dabc / acbd“ (Sechsergruppe „abcdef / dafcbe / afcebd / cafbed / eabcdf“) | DGROUP 0x474 ff., 0x450 ff.; Video 13:11 |
| Termine 0–5 Gruppen, 6–11 Endrunde; Sieger nach Termin 11, Meisterfoto nur für einen deutschen Sieger | 0x3139F; Karriere 29:41 |
| `[APPROX]` Rückrunde mit getauschtem Heimrecht; Tabelle wie in der Liga; Gruppenspiele mit Unentschieden | – |

### Bildschirm „Europacup Gruppe 1/2 / Endrunde“ (`UI/Screens/Competitions/EuropeanCupScreen.cs`)

Funktion 0x31CD5: Fenster 13 × 10 Teile ab (4 | 3), Pokale kb\36 bei (13 | 7) und (286 | 7), Titel Bold Grundlinie 23,
Paarungen Mini ab Grundlinie 54 (26 je Runde, 6 je Paarung; Heim x = 25 Farbe 30, „-“ x = 120, Gast x = 137 Farbe 1),
Termine x = 230 / 276, Tabelle Flow ab Grundlinie 135 (10 je Platz), Knöpfe (79 | 196) und (193 | 196).
Zahlen mit Mindestbreite füllen fehlende Stellen mit der Breite der Ziffer 0 auf (`CompetitionWidgets.Number`).

Pixelvergleich `emtool screens gamedata ~/work/screens europapokal`, Bild `00_gruppe1_video_13_11` gegen
`oberflaeche_1311` (Person und Mauszeiger maskiert): mittlere Abweichung 7,2, 1,9 % der Pixel über 40.

**Offen:** Ergebnisse gespielter Begegnungen (Lage aus 0x3248 nicht im Video belegt). Vorlagebilder mit Ergebnissen seit der Reihe `folge04` (`folge04_01241`–`01247`) und `folge20` (`folge20_02316`, `02318`).

## Weltmeisterschaft (`Core/CupsWorld.cs`)

| Regel | Beleg |
|---|---|
| Zwölf Nationalteams (Datenbank 52–63), zwei Gruppen à sechs, jeder gegen jeden an den Terminen 1–5 (14.–22.4.) | 0x30427 (Wettbewerb 0), Spielplan DGROUP 0x450 ff. |
| Viertelfinale nach Termin 5: A1–B4, A2–B3, A3–B2, A4–B1 (Gruppe 1 mit Heimrecht) | 0x30F8B |
| Weiterverteilung nach jedem K.-o.-Termin (Sieger vorn) nach DGROUP 0x12C: Termin 7 Halbfinale + „SPIELE 5.-8.“, Termin 8 „ENDSPIEL“, „SPIEL UM 3./5./7.PL.“ | 0x31152, Texte DGROUP 0x1D2A ff. |
| Weltmeister = Sieger des Endspiels; Meisterfoto „Weltmeister“ für jedes Land | 0x3136F, 0x31378 |
| Spielwerte der Nationalteams: Tabelle DGROUP 0x4C4E (je Land Kondition Abwehr, Technik Abwehr, Kondition Sturm, Technik Sturm, je −2) | Lader 0x2EFDE; Video „Stärken Länderteams – Gesamt“ 13:18 (Spalten Kondition/Technik = Mittel aus Abwehr und Sturm) |
| Gruppeneinteilung: Plätze 1–6 / 7–12 der Länderreihenfolge; beim Spielstart und zu jeder Saison 20 Tausche zweier Plätze random(52, 63), die Tausche wirken fort (`WorldCup.Mix`, `WorldCup.Order`) | 0x35EDD, Aufrufe 0x43A17 / 0x5644E |
| Ohne menschlichen Bundestrainer werden die Werte Deutschlands an den Tagen mit Markierung 255 (13. und 29. April) neu ausgewürfelt: Kondition random(84, 94), Technik random(79, 89), Form random(45, 55) – die WM spielt Deutschland also nie mit den Startwerten (`GameState.NationalRatings`) | 0x554CF |
| `[APPROX]` Spielstärke = Mittel aus Kondition und Technik; K.-o.-Unentschieden mit Penaltyschießen; Plätze 9–12 nach Gruppenplatz | – |

### Bildschirm WM „Gruppe 1/2“ (`UI/Screens/Competitions/WorldCupScreen.cs`)

Funktion 0x2F5B6: Fenster 12 × 12 Teile ab (16 | 2), Titel Bold Grundlinie 21, Linie (27 … 292 | 24), Zeilen ab Grundlinie 57
im Abstand 28 (Farbe 7/30 im Wechsel), „n.“ Bold x = 25, 44 × „-“ Flow (Grundlinie + 7), rote Doppellinie unter Platz 4,
Flagge = Logo des Nationalteams bei (48 | y − 18) bzw. (104 | y − 18), Punkte und Tore Bold x = 170. Angemeldet unter
Paarungen → WM und Tabellen → Tabelle WM.

Pixelvergleich `emtool screens gamedata ~/work/screens wm`, Bild `00_gruppe1_video_13_13` gegen `oberflaeche_1313`:
mittlere Abweichung 13,1, 9,3 % der Pixel über 40 – an den kontrastreichen Flaggenkanten (Videounschärfe); Zahlen,
Ränge und Linien liegen pixelgenau.

**Offen:** K.-o.-Ansicht (Knopf „Endrunde“) nicht im Video – `[APPROX]`-Liste; WM-Statistik wartet auf `livespiel`.

## Stärken (`Core/StatisticsStrength.cs`, `UI/Screens/Statistics/StrengthScreen.cs`)

Funktion 0x51206 (Hauptmenü → Stärken → 1. Liga, 2. Liga, Oberliga, Länderteams):

| Regel | Beleg |
|---|---|
| Spalten Kondition, Technik, Form, Mittel; „Gesamt“ = Mittel aus Abwehr und Sturm, „Abwehr“/„Sturm“ = Einzelwert; jede Spalte auf 0–99 begrenzt; Mittel = (K + T + F) / 3 der ungerundeten Summen | 0x5167F ff., Begrenzung 0x120:0x52; Video 13:18 (Russland 99 99 51 → 85) |
| Sortierung absteigend nach K + T + F der Ansicht | 0x5151D |
| Fenster 10 × 12 Teile ab (30 \| 5), Füllfläche (36 \| 9) … (262 \| 211) Farbe 4 (übermalt die Kopfzeile), Titel Flow zentriert 61 … 248, Grundlinie 19, Farbe 10, Schatten 28, Linie y = 25 | 0x5127C ff. |
| Zeilen ab Grundlinie 34 im Abstand 15, Name x = 45, Werte ab x = 142 (je Zahl + zwölf Leerzeichen), Farbe 30 / Manager 24, Trennlinie y = 43 + 15 n | 0x515B0 ff. |
| Smiley (240 \| 36 + 15 n), Trendbalken von x = 250 bis 204 + Vereinsfeld 0x17, Farbe 12/24/18 | 0x518AB ff. |
| Knöpfe „Gesamt“, „Abwehr“, „Sturm“ bei x = 43 + 80 i, y = 212 | 0x51289 |
| `[APPROX]` Ligavereine: Werte aus der Aufstellung (Verteidiger = Abwehr, Stürmer = Sturm), weil das Remake keine Teamwerte führt; Form = Mittel der Spielerform; Trendbalken aus der Form; Smiley immer gelb (Wertverlauf nicht geführt) | – |

Pixelvergleich `emtool screens gamedata ~/work/screens staerken` (Smiley/Balken und Werte der Ligavereine maskiert, weil
zufallsabhängig): Länderteams gegen `oberflaeche_1318` mittlere Abweichung 8,8, 3,8 % > 40; Oberliga gegen `oberflaeche_1336`
9,5, 4,8 % > 40. Kondition und Technik der Länderteams stimmen mit dem Video überein.

## Statistiken über Saisons (`Core/StatisticsHistory.cs`)

Fortgeschrieben genau einmal je Saison am 29. April (Anschluss in `GameState.Integration.cs`, vor der Titelvergabe).

| Regel | Beleg |
|---|---|
| Ewige Tabelle je Saison: Ligaplatz p (0-basiert) → 12 − p, dazu 40 (Bundesliga) bzw. 20 (2. Liga); ohne Liga 0; Meister +15, als Bundesligist +10; Erster einer unteren Liga +3; DEB-Pokalsieger +25; Europacupsieger +40 | 0x5667A; Video 16:38 (Berliner SC 5, Mannheim 52, Düsseldorf 116 nach einer Saison) |
| Titelzähler Meister, DEB-Pokal, Europapokal je Verein | 0x2E730 (Vereinsstruktur 0x82A3 + 80 · Verein) |
| Ewige Bilanz des Managers: Punkte, Tore, Siege, Niederlagen, Unentschieden aus den Ligaspielen, zu Hause/auswärts | Managerstruktur 0x9796 ff.; Video 16:38 (44 Spiele nach einer Saison) |
| „Früher“: je Saison Liga (Platz bzw. Play-Off-Aus 0x33–0x35 bzw. Meister 0x80), DEB-Pokal-Runde/SIEGER, Europacup Gruppe/Endrunde/SIEGER/Nicht im Wettbewerb | 0x2C099, 0x2C9D6 |
| `[APPROX]` 48 statt 42 Vereine in der Ewigen Tabelle | – |

### Bildschirme

- **„Ewige Tabelle“** (Statistiken → Totale, `UI/Screens/Statistics/EternalTableScreen.cs`): Funktion 0x2E356.
  Fenster (50 \| 23) 9 × 6 und (50 \| 154) 9 × 4, Pokale kb\36, Pfeile kb\4 (20 \| 0) bei (165 \| 30), Liste über Zwischenbild
  (Zeile 6 + 6 n) mit Ausschnitt nach (57 \| 58), unten Bilanz des Managers am Zug. Pixelvergleich gegen `oberflaeche_1638`
  (Listen- und Wertebereiche maskiert): mittlere Abweichung 7,2, 1,2 % > 40.
- **„Früher“** (`HistoryScreen.cs`): Funktion 0x2C1EE. Drei Diagrammfenster mit Achsen (Farbe 6) und Textfeld; rote
  Markierung der angezeigten Saison (Farbe 16). Pixelvergleich gegen `oberflaeche_1652` (Text maskiert): 5,9, 1,1 % > 40.
  `[APPROX]` Balkenhöhen und -farbe.

**Nicht umgesetzt (abhängig vom Live-Spiel):** „Spieler“ (Torschützen, Vorleger, Scorer, Keeper), „Verein“
(Über-/Unterzahl, Serien), „Fairplay“ (Strafen) und „WM-Statistik“ (Spieler im WM-Kader) brauchen Tore, Vorlagen und
Strafen je Spieler. Die heutige Spielberechnung liefert nur das Endergebnis; diese Daten entstehen erst mit dem Paket
`livespiel`.

## Bundestrainer: WM-Kader und „WM Vorbereitung“ (`Core/CupsNational*.cs`)

Freundschaftsspiele gibt es im Original nur für den **Bundestrainer** – im Spielmodus „nur Weltkugel“ und im Modus 4,
wenn ein Manager ernannt wurde (Abschnitt „Bundestrainer im Modus 4“ unten).

**Spielmodus** (Spielvorbereitung, Datei 0x28506/0x2985C): zwei Knöpfe aus kb\1 (57 × 13, Quelle x = 113), links die
deutsche Fahne (Bit 0, y = 76/89), rechts die Weltkugel (Bit 1, y = 102/115), Vorgabe beide an. Daraus DGROUP 0xD0B9:
nur Fahne → 2 (Liga und DEB-Pokal, kein Europapokal/WM), beide → 4, **nur Weltkugel → 1**. Im Modus 1 wird keine Liga
gespielt (z. B. Datei 0x4C759, 0x5D4F6): Der Tageszähler springt bis zum Ereignis 0x10, Manager 0 wird Bundestrainer
(DGROUP 0xD0BA, Datei 0x54D19/0x55809) und bekommt am Saisonende-Abschnitt die Nation DGROUP 0x6AC samt Etat
(DGROUP 0xA055 += 0x29C, Datei 0x554A6). Umgesetzt mit der Spur `wettbewerbe-weltkugel` (Abschnitt unten).

**Ablauf als Bundestrainer:** Der Nationalteam-Zug läuft als „Manager 5“ (DGROUP 0xA70E = 5) mit eigenem Kader,
eigenem Kontostand (DGROUP 0xA055, anfangs 0) und eigener Freischaltung der Kacheln (DGROUP 0x5159 + 42, Datei 0x44FBD):
Mannschaft → Aufstellung, Taktik, „Transfer“ = **„Spieler in WM-Kader“** (Stub 0xF8:0x5C, Datei 0x457C0) und
„Training“ = **„WM Vorbereitung“** (Stub 0xF8:0x57, Datei 0x457D9); dazu WM-Statistik, Tabelle WM, WM, Stärken
Länderteams und Gemischtes. Am ersten WM-Termin wählt das Original den Kader automatisch, wenn DGROUP 0x1DB gesetzt ist
(Datei 0x54D25).

| Regel (umgesetzt in PR 6) | Beleg |
|---|---|
| Etat beim Amtsantritt: DGROUP 0x29C = 1.500.000 Drittel-DM = 500.000 DM; nach jeder WM 100.000 DM, Platz 1–8 (11 − Platz) · 100.000 DM | Datei 0x554A6, 0x31293 ff. |
| Kader: höchstens 29 (Zähler &gt; 28 → abgelehnt); Klick auf Kaderspieler entlässt, Verletzte werden nicht aufgenommen | 0x3CAD5, 0x3C928 |
| „auto“: Kader leeren, dann 3 Torhüter, 10 Verteidiger, 16 Stürmer (DGROUP 0x4EDE) – je Art der Unverletzte mit der höchsten Summe Kondition + Technik + Form | 0x3CA06, 0x3CC01 |
| Am Saisonende kehren alle Kaderspieler zurück, das Amt endet | 0x557B0, 0x5580E |
| Kosten (Drittel-DM, DGROUP 0x26C, je Land 52–63): 300.000, 165.000, 180.000, 135.000, 120.000, 150.000, 90.000, 60.000, 105.000, 75.000, 45.000, 195.000 – im Remake durch 3 (100.000 … 15.000 DM), Heimspiel doppelt | 0x3BE78 |
| Höchstens 8 Spiele je Saison (DGROUP 0xD0C1), „Nicht mehr als / 8 Freundschaftsspiele“; zu wenig Geld: „Sie haben nicht / genug Geld !“ | 0x3BF9B, 0x3C2A3 |
| Spiel sofort (0xE8:0x39), Ergebnis in DGROUP 0xD0C3 ff. | 0x3C05E, 0x3C248 |
| Bonus: x = (Kosten in Drittel-DM / 2000 + random(0, 50)) / 50; b = eigene Tore − Gegentore; auswärts mit x &gt; 0 b += random(1, x), sonst b −= 1; x &lt; 2: b −= 2 − x − (auswärts ? 1 : 0); x &gt; 2: b += x; b auf (x &gt; 2 ? 1 : 0) … (x &lt; 2 ? 1 : 2) begrenzt; x &gt; 3: b + 1 | 0x3BFCC–0x3C183 |
| Bonus auf drei Werte jedes Kaderspielers, einer eins weniger (x &gt; 2: der erste, sonst der zweite), höchstens 99 | 0x3C18C |
| Länderspiele zählen für die Spieler der Aufstellung des Managers 5 | 0x872B |
| `[APPROX]` Kandidaten: alle deutschen Spieler (keine Ausländer) der Ligavereine und des Transfermarkts, nach Summe absteigend – das Original listet seine 200 ausführlich geführten Spieler (Kader der Manager, Transfermarkt) | 0x3C5C9 |
| `[APPROX]` Stärke des Nationalteams = Mittel von (Kondition + Technik) / 2 über Torhüter und Reihen 1–3 der Aufstellung (16 Plätze, Lücken 0) plus Taktikzuschlag; ohne Aufstellung die Automatik (bester Torhüter, je Reihe 2 Verteidiger + 3 Stürmer). Aufstellung und Taktik: Abschnitt „Aufstellung und Taktik des Nationalteams“ | – |
| `[APPROX]` Felder 0x10–0x12 des Kaderplatzes als Kondition, Technik, Form gedeutet (Spaltenfolge „KO TE FO“) | – |
| `[APPROX]` Energie 70 beim Öffnen von „WM Vorbereitung“ (Kaderfeld 0x13) nicht übernommen: Bildschirme ändern beim Öffnen nichts | 0x3BB51 |

Bildschirme (Aufbau aus em.exe; Videobilder seit der Reihe `bundestrainer`, Pixelvergleich steht aus;
Bildschirmfotos `emtool screens gamedata ~/work/screens nationalteam`):
`UI/Screens/Competitions/NationalSquadScreen.cs` („Spieler in WM-Kader“, 0x3C37C) und `FriendliesScreen.cs`
(„WM Vorbereitung“, 0x3B8F7), Bausteine in `NationalWidgets.cs` (Kasten 0x3B700, Mannschaftsfeld 0x3B4C6). Im Spielmodus
„nur Weltkugel“ sind beide über das Menü des Bundestrainers erreichbar (Abschnitt unten).

## Aufstellung und Taktik des Nationalteams (Spur `nationalteam-2`, `Core/CupsNational.Lineup.cs`)

Der Bundestrainer („Manager 5“) benutzt im Original dieselben Bildschirme wie ein Verein: Aufstellung 0x108:0x2a
(Datei 0x40364) und Taktik, beide aus dem Menü über 0x118:0x43 (Datei 0x48E28) mit Kennwert 0. Overlay-Aufrufe
aufgelöst mit Datei = 0x255C0 + Dword im Stub-Kopf (+4) + Funktionsoffset aus dem Stub-Eintrag (`CD 3F <offset>`).
Kaderplatz 52 Byte: DGROUP 0x5DC3 + 0x618 · Manager + 0x34 · Platz, 29 Plätze.

| Regel | Beleg |
|---|---|
| Feld 0x0A „eingesetzt“ (0/1), Feld 0x15 Platz in der Aufstellung; „Eingesetzt: n“ zählt Feld 0x0A ≠ 0 (0x120:0x66 Modus 3) | Datei 0x4BE17, 0x41D20 |
| Aufnahme in den WM-Kader nullt den Platz: neue Kaderspieler sind weder eingesetzt noch aufgestellt; Kader nach Art sortiert | 0x42E2A, 0x42D46 |
| Auf einen Platz setzen macht eingesetzt; bei 22 Eingesetzten wird ein weiterer abgewiesen: „Schon 22 Mann / im Team !“ (DGROUP 0x11D5) | 0x415B2, 0x40E2A–0x40E61 |
| Platz leeren (aus dem Menü, Kennwert 0): auch nicht mehr eingesetzt | 0x40CF8–0x40D0D |
| Linksklick (Mausereignis 1) auf einen eingesetzten, nicht aufgestellten Spieler nimmt ihn heraus; Rechtsklick (Ereignis 3) auf einen nicht eingesetzten setzt ihn ein | 0x40F45–0x40F59, 0x411FE; Ereignisse 0xBBF:0x24DF |
| NR = Zeile im Kader (1–29), nur bei Eingesetzten; sonst NR und STATUS leer | Video 00056 |
| „Eingesetzt: n“ in Farbe 16 (Füllung 4) | 0x41D61–0x41D9D |
| `[APPROX]` Wer von einem besetzten Platz verdrängt wird, bleibt eingesetzt (RESERVE) | – |
| `[APPROX]` Auto-Aufstellung „A“ wie beim Verein (drei Blöcke), dazu die stärksten Übrigen bis 22 eingesetzt; „C“ leert alle Plätze | – |
| `[APPROX]` Linksklick auf nicht eingesetzte oder aufgestellte Spieler wählt sie aus (wie beim Verein); Spielerinfo auf Rechtsklick (0x40872) nicht übernommen | – |
| `[APPROX]` Stärke aus Torhüter und Reihen 1–3 (Reihe 4 zählt nicht), Taktik wie beim Verein (`Tactics.EffortStrengthBonus`); ganz leere Aufstellung → bisherige Automatik | – |

Spielstand Schema 4 (Migration 3 → 4: leere Aufstellung, Grundtaktik, niemand eingesetzt).
Bildschirmfotos: `emtool screens gamedata ~/work/screens nationalteam-2` (Belegung wie im Video).

**Pixelvergleich** (Grauwerte, mittlere Abweichung / Anteil > 40; Sprecher unten links und Mauszeiger maskiert; „pegelbereinigt“
= nach linearer Anpassung Video ≈ 0,67…0,86 · Bild + 27…52, weil das Video Mitteltöne aufhellt und Weiß absenkt):

| Bildschirm / Bereich | roh | pegelbereinigt |
|---|---|---|
| Aufstellung `bundestrainer_00056`, ganz | 18,9 / 11,2 % | 15,4 / 7,7 % |
| – ohne Listeninhalt (andere Spieler und Werte als im Video) | 15,4 / 5,3 % | 11,3 / 4,2 % |
| – Blöcke, TW, „Eingesetzt“, Umschalter (x ≥ 241) | 13,4 / 3,1 % | 8,2 / 2,2 % |
| Taktik `bundestrainer_00058`, ganz | 14,2 / 4,0 % | 9,5 / 4,5 % |
| – ohne Wertefelder Einsatz/Angriff (00058 / 00059) | 14,1 / 4,3 % / 4,2 % | 9,5 / 3,9 % · 9,4 / 4,1 % |

Lage aller Teile stimmt (bester Versatz 0 | 0); Rest sind Schriftkanten, die anderen Spielerdaten und die Wertefelder.
Behoben: Farbe von „Eingesetzt“ (17 → 16).

### Taktikwerte, Vereinsaufstellung (Spur `taktik-verein`, Befunde aus #103)

Taktik von Verein und Nationalteam (`Core/Squad.Tactics.cs`, Belege dort im Klassenkommentar):

| Regel | Beleg |
|---|---|
| Je Block und Spielstand ein Byte: Zeile „Grundwert“ = Grundwert, die fünf übrigen Zeilen = Zuschläge darauf. Je Manager 4 Blöcke × 6 Werte (Einsatz DGROUP 0xCAE9 + 0x30 · Manager, Angriff 0xCB01); der Verein (61-Byte-Struktur) hält eine Arbeitskopie in den Feldern 0x26–0x2B (Einsatz, 0x2D = Grundwert) und 0x35–0x3A (Angriff) | Datei 0x485DD–0x48645, 0x48A9E–0x48B0A |
| Einsatz ist ein Prozentwert bis 100, Angriff eine Stufe bis 7; Zuschlag ≤ Obergrenze − Grundwert, Grundwert ≤ Obergrenze − größter Zuschlag der Zeilen „hohe Führung“ bis „knapper Rückstand“ („hoher Rückstand“ wird nicht geprüft); Schritt 1, untere Grenze 0 | 0x488E9–0x48925, 0x48971–0x48A4F |
| Modi 0 „AUTOM.“, 1 „SEMI“, 2 „MANUELL“ (Einsatz DGROUP 0xA6FB, Angriff 0xA707 je Manager); nur „SEMI“ zeigt die Werte hell und lässt sie ändern | Texte DGROUP 0x5A4; 0x47D9B, 0x488F6, 0x48930 |
| Automatik vor jedem Spiel (je Paarung, auch für Vereine ohne Manager): doppelte Stärken weniger als 25 auseinander → Einsatz 60 mit 0/25/20/30/0, sonst 40 mit 20/20/15/20/0; Angriff 0 mit 5/6/4/4/0 für den Schwächeren, sonst 4/5/6/7/4 | 0x8365–0x84D6, Tabellen DGROUP 0x4912/0x491C; Aufruf 0x36D12 |
| Im Spiel: Grundwert + Zuschlag des Spielstands, „hoch“ ab 3 Toren; Einsatz auf 0…100 begrenzt, Angriff in drei Bit; bei „MANUELL“ setzt das Spiel die Werte nicht | 0x939A–0x9527 |
| `[APPROX]` Ohne Spielvorbereitung je Paarung zeigt „AUTOM.“ die Zeile für ein ausgeglichenes Spiel (wie Video 00058/00059); eigene Werte starten mit derselben Zeile; „MANUELL“ spielt bis zum Live-Spiel mit den eigenen Werten; Energieabzug (Einsatz − (50 − Energie) unter 50 Energie, 0x9463) nicht übernommen | – |
| `[APPROX]` Stärkezuschlag: „AUTOM.“ 0, sonst je Block (Einsatz bei Gleichstand − 80) / 10, begrenzt auf ±3, gewichtet wie die Blöcke (bisheriger Wertebereich) | – |

Vereinsaufstellung (`Core/Squad.Lineup.cs`, `UI/Screens/Team/LineupScreen.cs`): derselbe Code wie beim Nationalteam
(Kennwert 0) – „Eingesetzt: n“ in Farbe 16, höchstens 22 Eingesetzte („Schon 22 Mann / im Team !“), Aufstellen setzt
ein, Platz leeren nimmt heraus, Linksklick nimmt einen Reservisten heraus, Rechtsklick setzt ein (Belege in der Tabelle
oben). `[APPROX]` Ohne Änderung von Hand gelten die Aufgestellten und die stärksten übrigen einsatzfähigen Spieler bis 22
als eingesetzt (Video Aufstellung ~9:00: „Eingesetzt: 20“ bei 20 Spielern); STATUS bleibt bei nicht Eingesetzten leer,
NR zeigt weiter die Rückennummer; „Eingesetzt“ wirkt noch nicht auf die Spielstärke.

Spielstand Schema 5 (Migration 4 → 5, `SaveGame.Migration4.cs`): eigene Taktikwerte aller Vereine und des Bundestrainers
werden auf die Startwerte gesetzt (alte Werte −3…+3 sind nicht umrechenbar), Modi bleiben; Vereine bekommen „Eingesetzt“
mit automatischer Auswahl.

**Pixelvergleich Taktik Nationalteam** (Bildschirmfoto `03_taktik_wie_video_00058` aus `emtool screens gamedata … nationalteam-2`,
Maße wie oben; „Wertefelder“ = die 12 Felder Einsatz/Angriff): ganz 00058 roh 14,1 / 4,1 %, pegelbereinigt 9,4 / 3,9 %
(00059 gleich); Wertefelder roh 13,8 / 1,2 %, pegelbereinigt 6,9 / 2,7 % (vorher mit lauter Nullen 14,0 / 1,2 % bzw.
7,2 / 2,7 %). Sichtprüfung: alle zwölf Werte stimmen mit dem Video überein; der Rest sind Schriftkanten.

## Spielmodus „nur Weltkugel“ (Spur `wettbewerbe-weltkugel`, `Core/CupsNationalMode.cs`)

| Regel | Beleg |
|---|---|
| Knöpfe: Bit 0 Fahne, Bit 1 Weltkugel, Vorgabe 3; Modus = (Bits = 2) + 2 · (Bits = 1), Bits 3 → 4 | Datei 0x28191, 0x29838–0x29862 |
| Klickflächen: x &gt; 190, 45 &lt; y &lt; 61; Fahne bis x &lt; 254, ab 254 Weltkugel; Bild kb\1 (113 \| 76 + 13 · Bit 0) bzw. (113 \| 102 + 13 · Bit 1) bei (193 \| 47) und (259 \| 47) | 0x286F6 ff., 0x28506 |
| Fahne nur mit höchstens einem benannten Manager umschaltbar | 0x28717–0x2873E |
| Ohne Knopf (Bits 0) nimmt „Los jetzt!“ keinen Klick an | 0x295DF |
| Modus 1: Am ersten Tag springt der Tageszähler ohne Tagesverarbeitung bis zum ersten Termin mit Ereignis 0x10 (WM, 14. April) | 0x54CEE–0x54D0E |
| Danach DGROUP 0xD0BA[0] = 1 und Ernennung samt Etat (Konto des Nationalteams) für jeden Manager mit gesetztem Wert; Modus 2 ernennt nie | 0x54D19, 0x55436–0x554BA |
| Am Saisonende setzt Modus 1 DGROUP 0xD0BA[0] wieder: Ernennung in jeder Saison | 0x55802 |
| Automatische Kaderwahl am Sprungtag nur im Durchlauf ohne Halt (DGROUP 0x1DB, versteckter Klick 0x4540E ff.) | 0x54D1E |
| Sperrtabelle DGROUP 0x5159: 7 × 6 Bytes Verein (alle frei) + 7 × 6 Bytes Nationalteam (Manager alles, Mannschaft nur „Verträge“, Statistiken außer WM-Statistik, Tabellen und Paarungen außer WM, Stärken außer Länderteams gesperrt) | 0x44FBD |
| Modus 2 sperrt Platz 6 der Statistiken, Tabellen, Paarungen und Platz 4 der Stärken (WM-Kacheln) | 0x44FE8–0x4501D |
| Gesperrte Kachel: Symbol kb\3 (157 \| 203) 38 × 28, kein Klick | 0x45B63 |
| Menü des Bundestrainers: „Transfer“ = „Spieler in WM-Kader“, „Training“ = „WM Vorbereitung“ | 0x457C0, 0x457D9 |
| `[APPROX]` Halt nach dem Sprung vor dem ersten WM-Spiel, damit der Bundestrainer den Kader wählen kann | – |
| `[APPROX]` Modus 1 nur mit genau einem Manager („Los jetzt!“ mit Meldung gesperrt) | – |
| `[APPROX]` Modus 1 ohne Europapokal, Auf-/Abstieg, Oberliga-Wechsel, Prämien, Ligastatistik, Finanzen, Training, Markt und Vertragsmeldungen (kein Tageslauf vor der WM) | – |
| `[APPROX]` Menü des Bundestrainers schon vor der Ernennung, Mannschaftskacheln bis dahin gesperrt | – |
| Tabellen-Reihe mit sechs Kacheln: 1. Liga, 2. Liga, Oberliga, DEB-Pokal, Europapokal, WM (Symbolstart 17, ab Platz 4 eine Spalte versetzt); „Tabelle WM“ damit auf Platz 6. „Tabelle DEB-Pokal“ und „Tabelle Europapokal“ rufen dieselben Bildschirme auf wie „DEB-Pokal“ und „Europapokal“ bei den Paarungen (0xE8:0x75 bzw. 0xF8:0x3E mit Seitenwert 3, Datei 0x4584A/0x4584C; Spur `tabellen-highscore`, `CupTableMenuEntries`). 0xE8:0x75 ist die Übersicht „DEB-Pokalspiele“ (Datei 0x32E23, Gegner und Ergebnisse), nicht die Auslosung – seit R4-01 (#108) als `DebCupGamesScreen` unter „Tabelle DEB-Pokal“ gebaut (Aufbau, Rahmenformel, Spalten, Farben im Klassenkommentar; [ORIGINAL] Seitentausch nach Termin 1 und 2, Überlauf-Punkt nur bei Verlängerung, Datumszeile ab Tag 212 weg; offen nur Penalty-gegen-Verlängerung und Punktform); die Paarungs-Kachel „DEB-Pokal“ öffnet seit demselben PR ebenfalls die Übersicht (im Original derselbe Stub); die Auslosung ist ohne Kachel und wird nur im Spielablauf gezeigt (Abschnitt „Live-Auslosung im Spielablauf“) | DGROUP 0x5149/0x5151, Datei 0x45B95, 0x45829 ff. |
| Kachelsperren der Spielvorbereitung (Reihe Manager): „Neuer“ gesperrt bei vier Managern (DGROUP 0x386), „Werbung“ gesperrt bei der Spieldauer „eine Saison“ (Spielende DGROUP 0x5A14 = 1994) | Datei 0x45057, 0x45079, 0x2987C; `UI/Screens/TileLocks.cs` |

Das Nationalkonto führt eine Buchungsliste (`GameState.NationalBookings`: Etat, Freundschaftsspiele); Spielstand
Schema 3. Pixelvergleich der Modusknöpfe mit dem Video „Die Oberfläche von EM“ (2:13–2:19 nur Weltkugel, 2:30 beide),
mittlere Abweichung / Anteil > 40: Fahne aus 10,4 / 4,5 % (`oberflaeche_0214`), Weltkugel 12,1 / 6,5 % (`0219`); zum
Vergleich beide an 9,0 / 2,0 % und 11,9 / 6,3 % (`0230`). Das Menü des Bundestrainers zeigt die Reihe `bundestrainer`
(unten); ein Pixelvergleich steht noch aus
(Bildschirmfotos: `emtool screens gamedata ~/work/screens weltkugel`).

### Bundestrainer im Modus 4 (Spur `bundestrainer-modus4`, `Core/CupsNationalMode.cs`)

DGROUP 0xD0BA[Manager] („wird Bundestrainer“) setzt das Original an genau drei Stellen: Modus 1 am Sprungtag (0x54D19)
und zum Saisonende (0x55809) sowie in jedem Modus über die Tabellensuche 0xE8:0x93(0x7F) (Datei 0x34769, Schreibzugriff
`[bx+0xD0B9]` mit bx = Manager + 1). Gespeichert wird es mit 4 Bytes (0x5BED8, 0x5C8BC).

| Regel | Beleg |
|---|---|
| An Tagen mit Markierung 255 (13. und 29. April), solange Deutschland keinen Bundestrainer hat: nach dem Tageswechsel die ersten fünf Plätze der 1. Liga von oben durchsuchen (DGROUP 0x5CCB = Liga · 12 + Platz, Vereinsfeld 0x16 = Manager + 1); der erste Verein mit Manager bestimmt den Bundestrainer | Datei 0x55364–0x553AE, 0x34769–0x347EE |
| Ernennung wie im Modus 1 (Etat aufs Nationalkonto, Name des Managers für „Manager 5“), Modus 2 ernennt nie | 0x55436–0x554BA |
| Gibt es danach weiter keinen Bundestrainer, werden die Werte Deutschlands ausgewürfelt | 0x554CF ff. |
| An WM-Tagen (Ereignis 0x10) folgt nach den Vereinsmanagern der Zug „Manager 5“ (DGROUP 0xA70D = 0xA70E = 5) mit dem Menü des Bundestrainers | 0x55587–0x555D6 |
| Sperrtabelle im Zug „Manager 5“ wie im Modus 1 (DGROUP 0x5159 + 42); „Highscore“ im Modus 4 ausdrücklich gesperrt (Wert = 0x256 = 0 oder Bundestrainer und Modus 4); im Modus 1 über 0x256 = 0 ebenfalls gesperrt | 0x45020–0x4504A, 0x289A3 |
| Am Saisonende kehren die Kaderspieler zurück, das Amt endet (in allen Modi); nur Modus 1 setzt 0xD0BA[0] neu | 0x557A5–0x5580E |
| Infoleiste im Zug des Bundestrainers: WM-Gegner Deutschlands und Nationalkonto (wie Modus 1, Video `bundestrainer_00026`) | – |
| `[APPROX]` Platzierung aus der Abschlusstabelle der Hauptrunde (Gesamt); das Original liest die zuletzt in place sortierte Reihenfolge von 0x5CCB (Sortierung 0x50E60 ff. auch nach Heim/Auswärts) | – |
| `[APPROX]` Zug des Bundestrainers nur an WM-Tagen, an denen Deutschland spielt (wie der Halt im Modus 1); das Original gibt ihn an jedem WM-Tag – Deutschland spielt an allen acht Terminen | 0x555AD |
| `[APPROX]` Reihenfolge: Zug des Bundestrainers vor dem Austragen des Tages, damit Kader und Aufstellung für das WM-Spiel gelten | – |

Unterschiede zu Modus 1: keine Ernennung zu Saisonbeginn und kein Sprung – die Vereinssaison läuft normal; Ernennung
erst am 13. April und nur für einen Manager unter den ersten fünf der 1. Liga; das Vereinsmenü bleibt, der Bundestrainer
bekommt an WM-Tagen einen zusätzlichen Zug; ohne Ernennung spielt Deutschland mit ausgewürfelten Werten. Spielstand
Schema 5 (`_nationalTurn`, Migration `SaveGame.Migration4.cs`). Bildschirmfotos: `emtool screens gamedata ~/work/screens
bundestrainer-modus4` (kein Videobeleg für Modus 4).

Offen (außerhalb dieser Spur): Die Tabellensuche setzt bei jedem Manager unter den ersten fünf zusätzlich Statusbit 0x40
(Manager-Feld 0x974E, außer Modus 2, 0x553BD–0x553F2) → Saisonende-Symbol 8 (`docs/finanzen-rekonstruktion.md`), im
Remake nicht geführt. Der Highscore-Schlüssel 0x7530 („nur Weltkugel“) ist im Original nicht erreichbar (0x28A34 liegt
hinter der Bedingung „Fahne gedrückt“, 0x289A3) – Modus 1 hat dort keine Bestenliste.

**Vorlagebilder (Modus „nur Weltkugel“):** Seit der Reihe `bundestrainer` (WM-Video, `docs/referenz-videos.md`;
Bildnummer = `bundestrainer_<nummer>.png`) **liegen vor:**

- Hauptmenü des Bundestrainers, Infoleiste nach dem Sprung zum 14. April (00026–00041: Gegner Schweiz, Nationalkonto
  500.000 DM; nach den Freundschaftsspielen 400.000 / 290.000 / 160.000 / 100.000 / 0 DM bei 00132, 00211, 00257, 00336,
  00404) und an den WM-Tagen 16.–28. April (00450, 00515, 00545, 00603, 00630, 00700, 00726).
- Kachelreihen und Sperren: Mannschaft = Aufstellung (vom Sprecher verdeckt), Taktik, gesperrt (Verträge), Transfer,
  Training, leer (00053, 00057, 00100); Stärken = drei gesperrt, „WM“ (Länderteams) offen, zwei leer (00108, 00136, 00611);
  Manager = alle sechs gesperrt (00042, 00220, 00418). Passt zur Sperrtabelle DGROUP 0x5159 (Pixelvergleich steht aus).
- „Spieler in WM-Kader“ 00044–00052; Aufstellung 00056 und Taktik 00058–00059 des Nationalteams.
- „WM Vorbereitung“ 00104–00106 (Knopf „Heim“ 00106), 00116–00119, 00141–00156, 00224–00245, 00307–00323,
  00349–00352 („Auswärts“ 00350) u. a.; danach Banner „Freundschaftsspiel“ (00120) und Live-Spiel (00122–00129).
- „Stärken Länderteams – Gesamt“ im Modus „nur Weltkugel“ 00109–00111, 00214–00218, 00605–00607.
- WM-Spiele: Banner „Weltmeisterschaft“ (00422), Live-Spiel mit Anzeigetafel und Reporter (00424–00436), Ergebnisse
  „Gruppe 1/2 60. Min.“ (00437–00445, 00529–00534), Tabelle WM „Gruppe 1/2“ nach jedem Spieltag (00446–00449,
  00535–00544, 00559–00602), „WM-Endrundenspiele“ (00642–00645, 00647–00653, 00719–00725, 00744; dazwischen „Spiele laufen“ 00646), Weltmeister `kb\16` (00745–00809);
  Gegner-Fenster aus der Infoleiste 00417, 00610, 00657–00659.

**Weiterhin fehlen** (Stand nach `folge01` … `folge29` und `videos-roh-2`, `docs/referenz-videos.md`; ✅ = erledigt,
◐ = nur teilweise belegt). `videos-roh-2` zeigt den Modus **Liga mit Weltkugel** (Bundestrainer neben dem Verein), nicht
„nur Weltkugel“:

- ✅ Spielvorbereitung mit den Modusknöpfen – `folge01_00000`–`00151`, `folge08_00000`–`00052`, `speedrun_t1_00056`
  (außerdem „Die Oberfläche von EM“ 2:13–2:30).
- ◐ Infoleiste am Saisonbeginn – „Vor dem ersten Spieltag“ im Vereinsmodus `folge01_00221`; Hauptmenü des Bundestrainers
  am 14. April in Liga mit Weltkugel `eisbaeren_f24_00115`; im Modus „nur Weltkugel“ offen.
- ◐ Kachelreihen Statistiken, Tabellen und Paarungen – Vereinsmodus mit gesperrten WM-Kacheln (`folge01_00526`,
  `folge03_00018`, `folge06_01251`, `duell_s2a_01203`); Liga mit Weltkugel mit **offenen** WM-Kacheln: Tabellen/Paarungen
  `eisbaeren_f01_01431`, Stärken `eisbaeren_f01_01430`, Statistiken `eisbaeren_f01_01011`; Sperren im Modus
  „nur Weltkugel“ weiter nur aus der Reihe `bundestrainer`.
- ◐ „Tabelle WM“ – Gruppentabellen nach dem Spieltag vielfach (`eisbaeren_f35_00531`); ob über die Tabellen-Kachel
  geöffnet, ist im Kapitelindex nicht unterschieden.
- ✅ Die geöffneten Kacheln „Tabelle DEB-Pokal“ (Übersicht „DEB-Pokalspiele“, z. B. `folge13_02545`, `duell_s2a_01217`,
  Endspiel `eisbaeren_f60_02758`) und „Tabelle Europapokal“ (`folge04_01241`–`01247`, `eisbaeren_f51_03346`,
  Endrunde `eisbaeren_f58_01323`).
- ✅ WM-Statistik – `eisbaeren_f07_03843`, `eisbaeren_f56_01126`.
- ✅ WM-Endrunde (K.-o.-Ansicht „WM-Endrundenspiele“) – `eisbaeren_f35_00646`, `eisbaeren_f44_01330`.
- Saisonende im Modus „nur Weltkugel“ – offen (Saisonende im Vereinsmodus vielfach, z. B. `folge03_01807`).
- ✅ Torszenen – `eisbaeren_f01_01149`–`01234`, `duell_s2b_00503`, `duell_s4c_02052`; Schalter in den Einstellungen
  `speedrun_t2_04024`.
- Highscore am Spielende – offen (in keinem Video).

Die Beschreibung in `docs/anforderungen/bildschirme.md` (Gegner aus Liga oder Europapokal, Heimspiel an einem freien
Termin) stammt aus dem früheren Portierungsversuch und ist damit nicht belegt. Bildschirm „Paarungen Liga“: seit Spur
`paarungen-liga` allein aus em.exe gebaut (Abschnitt „Paarungen Liga“ unten); ✅ Videobilder für einen Pixelvergleich gibt
es seit den neuen Reihen: Oberliga `folge06_01256`, `duell_s2a_01138`, Zweite Liga `folge17_02406`, `eisbaeren_f14_01518`,
Bundesliga `folge21_02149`, `folge29_00152`, 60. Minute `eisbaeren_f40_01833`. Handbuchkorrektur im `infofile`: WM-Platzierungsspiele 9–12 (Verlierer/Sieger der
Spiele 5–8).

## Paarungen Liga (Spur `paarungen-liga`, Runde 5)

Kacheln „Paarungen 1. Liga“, „Paarungen 2. Liga“, „Paarungen Oberliga“ → `LeaguePairingsScreen`. **Kein Videobild:**
Der Aufbau stammt allein aus der statischen Disassemblierung von em.exe (kein Emulator verfügbar); ein Pixelvergleich ist
nicht möglich. Aufbau und Farben stehen im Klassenkommentar.

| Befund | Fundstelle |
|---|---|
| Kachel → Seitenwähler 0xF8:0x3E (Seite, Liga, 0) → Seite 7 → Bildschirmfunktion 0x128:0x39 | Datei 0x43150 (Sprungtabelle), 0x4E8A3 |
| Fenster (4 | 15) 312 × 152, drei Knöpfe (48 × 38) bei x = 30 / 140 / 250, y = 180; Pfeile aus kb\4 | 0x4E8A3 ff. |
| Kopfzeile „Liga   n.Spieltag  t.m.“ (Flow, Farbe 10, Schatten 28); n hat Mindestbreite 2, wenn der zuvor gezeichnete Tag gespielt war | 0x4E8A3 ff. |
| sechs Zeilen y = 48 + 20 k: Namen, „n. Platz, Stärke m“ (Mini), Ergebnis „g:g“ bzw. „--:--“ | 0x4E8A3 ff. |
| Start: gespielte Tage + 1 (nach dem Ende: 1); Pfeile blättern mit Umlauf; Rechtsklick/Esc zurück | 0x4E8A3 ff. |

**[APPROX]:** Mittlerer Knopf („Play-Off“ bei der Bundesliga, sonst „Aufstieg“) ohne Wirkung, da die Zielseiten fehlen;
Klick auf eine Zeile (Original: Vereinsinfo) ohne Wirkung; die zuletzt gewählte Seite (DGROUP 0x5125) wird nicht gemerkt;
Spielplan (`Schedule.DoubleRoundRobin`) und Stärkewerte (`StrengthTable`) sind Näherungen des Remakes. Die Trefferfläche der
Pfeilknöpfe ist der Rahmen (48 × 38).
