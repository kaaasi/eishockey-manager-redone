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

**Offen:** Live-Auslosung mit Dialog „Sind Ihre Nerven stark genug …“ nach dem dritten Termin jeder Runde
(Ablauf `CupDrawStep` liegt vor, die Anzeige im Spieltagsablauf fehlt); Bildschirm „DEB-Pokalspiele“ mit Ergebnissen
(Text in em.exe, Aufbau nicht im Video).

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

**Offen:** Ergebnisse gespielter Begegnungen (Lage aus 0x3248 nicht im Video belegt).

## Weltmeisterschaft (`Core/CupsWorld.cs`)

| Regel | Beleg |
|---|---|
| Zwölf Nationalteams (Datenbank 52–63), zwei Gruppen à sechs, jeder gegen jeden an den Terminen 1–5 (14.–22.4.) | 0x30427 (Wettbewerb 0), Spielplan DGROUP 0x450 ff. |
| Viertelfinale nach Termin 5: A1–B4, A2–B3, A3–B2, A4–B1 (Gruppe 1 mit Heimrecht) | 0x30F8B |
| Weiterverteilung nach jedem K.-o.-Termin (Sieger vorn) nach DGROUP 0x12C: Termin 7 Halbfinale + „SPIELE 5.-8.“, Termin 8 „ENDSPIEL“, „SPIEL UM 3./5./7.PL.“ | 0x31152, Texte DGROUP 0x1D2A ff. |
| Weltmeister = Sieger des Endspiels; Meisterfoto „Weltmeister“ für jedes Land | 0x3136F, 0x31378 |
| Spielwerte der Nationalteams: Tabelle DGROUP 0x4C4E (je Land Kondition Abwehr, Technik Abwehr, Kondition Sturm, Technik Sturm, je −2) | Lader 0x2EFDE; Video „Stärken Länderteams – Gesamt“ 13:18 (Spalten Kondition/Technik = Mittel aus Abwehr und Sturm) |
| `[APPROX]` Gruppeneinteilung: Original nimmt sechs aufeinanderfolgende Nationalteams, das Video zeigt aber eine andere Zusammensetzung → hier jede Saison neu gemischt | Video 13:13 |
| `[APPROX]` Spielstärke = Mittel aus Kondition und Technik; K.-o.-Unentschieden mit Penaltyschießen; Plätze 9–12 nach Gruppenplatz | – |

### Bildschirm WM „Gruppe 1/2“ (`UI/Screens/Competitions/WorldCupScreen.cs`)

Funktion 0x2F5B6: Fenster 12 × 12 Teile ab (16 | 2), Titel Bold Grundlinie 21, Linie (27 … 292 | 24), Zeilen ab Grundlinie 57
im Abstand 28 (Farbe 7/30 im Wechsel), „n.“ Bold x = 25, 44 × „-“ Flow (Grundlinie + 7), rote Doppellinie unter Platz 4,
Flagge = Logo des Nationalteams bei (48 | y − 18) bzw. (104 | y − 18), Punkte und Tore Bold x = 170. Angemeldet unter
Paarungen → WM und Tabellen → Tabelle WM.

Pixelvergleich `emtool screens gamedata ~/work/screens wm`, Bild `00_gruppe1_video_13_13` gegen `oberflaeche_1313`:
mittlere Abweichung 13,1, 9,3 % der Pixel über 40 – an den kontrastreichen Flaggenkanten (Videounschärfe); Zahlen,
Ränge und Linien liegen pixelgenau.

**Offen:** K.-o.-Ansicht (Knopf „Endrunde“) nicht im Video – `[APPROX]`-Liste; „Spieler in WM-Kader“/„WM Vorbereitung“
(Abstellung der Spieler eigener Vereine) fehlt; WM-Statistik folgt mit PR 4.

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

## Freundschaftsspiele – Befund (noch nicht umgesetzt)

Der einzige Freundschaftsspiel-Bildschirm in em.exe (Datei 0x3B8F7) trägt den Titel **„WM Vorbereitung“** (DGROUP
0x68C): Spalten „Heim“/„Auswärts“, „Gegner“, „Kosten“, Meldungen „Nicht mehr als 8 Freundschaftsspiele“ und „Sie haben
nicht genug Geld !“. Freundschaftsspiele sind im Original also Vorbereitungsspiele vor der WM mit Kosten für den Manager
und hängen mit „Spieler in WM-Kader“ zusammen. Die Beschreibung in `docs/anforderungen/bildschirme.md` (Gegner aus Liga
oder Europapokal, Heimspiel an einem freien Termin) stammt aus dem früheren Portierungsversuch und ist damit nicht
belegt. Ein Bildschirm „Paarungen Liga“ ist in den Referenzvideos nicht zu sehen.
