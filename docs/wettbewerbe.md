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

## Europapokal, WM (Vorarbeit für die nächsten PRs)

- Auslosung 0x341AB mit Wettbewerb 1: acht Plätze aus den Europapokal-Gegnern (Datenbank 64–149); Vereine mit
  Stärke unter 70 nur zu einem Viertel angenommen; danach ersetzen die deutschen Teilnehmer (Liste DGROUP 0xA75D,
  Ende 0x80) zufällige Plätze.
- Video 13:11: „Europacup Gruppe 1“ – vier Vereine, je zwei Spiele an 4.11./11.11., 7.11./14.11., 9.11./16.11.,
  Knöpfe „Gruppe 2“ und „Endrunde“. Video 13:13: WM „Gruppe 1“ mit sechs Ländern.
