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

Freundschaftsspiele gibt es im Original nur für den **Bundestrainer** im Spielmodus „nur Weltkugel“ (WM-Modus).

**Spielmodus** (Spielvorbereitung, Datei 0x28506/0x2985C): zwei Knöpfe aus kb\1 (57 × 13, Quelle x = 113), links die
deutsche Fahne (Bit 0, y = 76/89), rechts die Weltkugel (Bit 1, y = 102/115), Vorgabe beide an. Daraus DGROUP 0xD0B9:
nur Fahne → 2 (Liga und DEB-Pokal, kein Europapokal/WM), beide → 4, **nur Weltkugel → 1**. Im Modus 1 wird keine Liga
gespielt (z. B. Datei 0x4C759, 0x5D4F6): Der Tageszähler springt bis zum Ereignis 0x10, Manager 0 wird Bundestrainer
(DGROUP 0xD0BA, Datei 0x54D19/0x55809) und bekommt am Saisonende-Abschnitt die Nation DGROUP 0x6AC samt Etat
(DGROUP 0xA055 += 0x29C, Datei 0x554A6). Das Remake deutet den linken Knopf bisher als „DEB-Pokal an/aus“ – das ist
nicht belegt (Paket Spielvorbereitung).

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
| `[APPROX]` Aufstellung des Nationalteams: bester Torhüter, drei Reihen à 2 Verteidiger + 3 Stürmer; Stärke = Mittel von (Kondition + Technik) / 2 über 16 Plätze (Lücken 0) | – |
| `[APPROX]` Felder 0x10–0x12 des Kaderplatzes als Kondition, Technik, Form gedeutet (Spaltenfolge „KO TE FO“) | – |
| `[APPROX]` Energie 70 beim Öffnen von „WM Vorbereitung“ (Kaderfeld 0x13) nicht übernommen: Bildschirme ändern beim Öffnen nichts | 0x3BB51 |

Bildschirme (kein Videobeleg, Aufbau aus em.exe, Bildschirmfotos `emtool screens gamedata ~/work/screens nationalteam`):
`UI/Screens/Competitions/NationalSquadScreen.cs` („Spieler in WM-Kader“, 0x3C37C) und `FriendliesScreen.cs`
(„WM Vorbereitung“, 0x3B8F7), Bausteine in `NationalWidgets.cs` (Kasten 0x3B700, Mannschaftsfeld 0x3B4C6). Beide sind
noch nicht im Hauptmenü erreichbar: Das Menü des Bundestrainers (Manager 5) folgt mit dem Spielmodus.

**Noch offen (eigener PR, berührt gemeinsame Dateien):** Spielmodus „nur Weltkugel“ in der Spielvorbereitung (heute
deutet das Remake den linken Knopf als „DEB-Pokal an/aus“), Überspringen der Vereinssaison bis zum ersten WM-Termin,
Ernennung des Managers 0 zu jeder Saison, Hauptmenü des Bundestrainers mit der Kachelfreischaltung oben.

**Fehlende Vorlagebilder (Modus „nur Weltkugel“):** Seit der Reihe `bundestrainer` (WM-Video, `docs/referenz-videos.md`;
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

**Weiterhin fehlen:** Spielvorbereitung mit den Modusknöpfen (nur „Die Oberfläche von EM“ 2:13–2:30), Infoleiste am
Saisonbeginn, die Kachelreihen Statistiken, Tabellen und Paarungen (im Video nicht geöffnet), „Tabelle WM“ über die
Tabellen-Kachel (nur die Gruppentabellen nach dem Spieltag), die geöffneten Kacheln „Tabelle DEB-Pokal“ und
„Tabelle EuroCup“, WM-Statistik, Saisonende im Modus „nur Weltkugel“ (das Video endet mit dem Weltmeister-Bild) und
Torszenen.

Die Beschreibung in `docs/anforderungen/bildschirme.md` (Gegner aus Liga oder Europapokal, Heimspiel an einem freien
Termin) stammt aus dem früheren Portierungsversuch und ist damit nicht belegt. Ein Bildschirm „Paarungen Liga“ ist in
den Referenzvideos nicht zu sehen. Handbuchkorrektur im `infofile`: WM-Platzierungsspiele 9–12 (Verlierer/Sieger der
Spiele 5–8).
