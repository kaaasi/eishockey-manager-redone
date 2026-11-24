# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 25 – Abweichungen vor v1.0 umsetzen (Plan 24.11.2026, Go 24.11.2026)

Runde 24 abgeschlossen am 24.11.2026: [archiv/runde-24.md](archiv/runde-24.md). Stand: Restliste
[analyse/restliste-v1.md](analyse/restliste-v1.md) **V = 0, X = 0**, D 26, K 224, E 323; `main` `1daa223`, Schema 63.
**Ziel der Runde:** Die Abweichungen nach [analyse/abweichungen-v1.md](analyse/abweichungen-v1.md) **Abschnitt 0** sind „wie im
Original“ umgesetzt oder mit Beleg als „keine Abweichung“ geklärt: die 15 Themen aus Abschnitt 1, dazu B31, B11, K07, S05, S07,
T03, S01; Prüfungen B16, F06, F08; aus dem Aufräumkonzept (`aufraeumen-v1.md` §1.5) die Befunde **Aufräumkonzept K2, K9 (= B17),
K11, K13**. Klassen je Thema: [analyse/abweichungen-bewertung-claude.md](analyse/abweichungen-bewertung-claude.md). **Nicht in
dieser Runde:** Live-Themen S01, S04, S05, S02 (Runde 26, R25-D1 = D); die Themen K04, K11, K13, L11, L12, C02, C08 der
Abweichungsliste (gehen ins Aufräumen #936); D-Themen (DOSBox nach Lust). **Danach:** Runde 26 Live-Spieltag (Vormerkung unten), Aufräumen #936 nach [analyse/aufraeumen-v1.md](analyse/aufraeumen-v1.md) (A1–A6, mit #978, #980), dann
Abschlussprüfung.
**Entscheidungen Nutzer 24.11.2026** (Optionen im Plan-PR #996): R25-D1 = D (Zuschnitt nach Bereich, Live erst in Runde 26),
R25-D2 = A (S01 in Runde 26 als eine Spur: Nachbildung, Umbau, dann S04/S05; S02 danach), R25-D3 = B (gemeinsame RE-Spur
`re-abweichungen` als Welle 0, umsetzende Spuren warten auf deren Merge), R25-D4 = A (risikoorientiertes Gesamtreview durch
ChatGPT). **Go Nutzer 24.11.2026** (alle Spuren samt Folgeläufen, Modelle wie unten; Start nach Merge des Plans).

**Regeln wie Runde 23** (Archiv): Base = aktueller `main` beim Start der Spur, tatsächlicher Base-SHA in den Draft-PR; ein Branch
`paket/<spur>`, ein PR, Teile als Commits, höchstens drei Fortsetzungen; Befund reproduzieren, roter Regressionstest mit Fundstelle
`Datei 0x…`, dann grün; Mehrsaisonprobe (4 Wechsel, passiv/aktiv) bei Saisonwechsel, Kader oder Geld; `[ORIGINAL]`/`[APPROX]`;
fremde Tests nach R15-E2 = C, erwartete Werte nach R21-D2 = A; keine geplanten Aufgaben aus Läufen (R15-E4). N2 = B, N6 = A
gelten. **Je Thema:** (1) Beleg aus em.exe aus `re-abweichungen` (R25-D3 = B) – zeigt er **keine** Abweichung, nur Marker mit Beleg
nachziehen, nichts ändern; (2) sonst roter Test → Änderung „wie im Original“ → `[APPROX]` wird `[ORIGINAL]` mit Fundstelle; bei
Bildschirmen Bildschirmfoto mit Videobild bzw. DOSBox-Bild vergleichen. Bleibt ein Teil unklar: „wie im Original“, sonst
`[APPROX]` mit Begründung, Leitstelle bündelt. **Markerliste:** kein `markerliste.py --write` in den Spuren; die Spur trägt ihre
IDs mit neuem Stand im PR ein, den einzigen Generatorlauf macht `pruefung-karte`. **Migrationen** (Schema 64 ff.): `statistik-anzeige` bekommt 64, `saison-geld` (nur falls F06
Felder braucht) die nächste freie Nummer; Spuren mit Migration nacheinander mergen, Rebase-Fixlauf der nächsten erst nach dem Merge (`prozess/anhang.md` F). **Gemeinsame Dateien:**
`MainMenuScreen.cs` nur `bedienung`; `GameState.DailyRun.cs` `aufstellung` (B31) und `saison-geld` (T03, F08) je minimal;
`ClubScheduleWindow.cs` `bedienung` (B11) und `wettbewerbe-anzeige` (B16), je minimal.

**Folge:** Welle 0 `re-abweichungen`; nach deren Merge Welle 1 fünf Spuren parallel; Welle 2 `pruefung-karte` nach allen; dann
Gesamtreview (R25-D4 = A). Integration in der Reihenfolge der Fertigstellung, Migrationsspuren nacheinander.

| Spur | Welle | Themen | Modell | Grund |
|---|---|---|---|---|
| `re-abweichungen` | 0 | Belege für B12, B13, B15/B16, B17, B18, B28, B29, C07, K07, T03, F06, F08, Aufräumkonzept K2, K11, K13 | Opus 5.5 | Disassembly und Nachbildung; Lesefehler würden alle Spuren in die Irre führen |
| `bedienung` | 1 | B01, B07, B11, B12, B18 | Opus 5.5 | B12/B18 brauchen em.exe (Hauptmenü 0x45483 ff., Kurve des Bundestrainers); B11 über neun Bildschirme |
| `wettbewerbe-anzeige` | 1 | B10, B15, B16 (prüfen), B17 (= K9), C07 | Opus 5.5 | Wettbewerbscode 0xD0C2, 0x977F, Marken vor 0x38402 aus em.exe |
| `statistik-anzeige` | 1 | B25, B28, B29, B33 | Opus 5.5 | zwei neue Spielstandfelder (Migration), Balkenformel und Wertverlauf erst lesen |
| `aufstellung` | 1 | B31, K07, B13, S07, Aufräumkonzept K2 | Opus 5.5 | Spielkern Aufstellung (Kern-Ereignis beim Verlassen, Automatik je Manager, Strafbank) |
| `saison-geld` | 1 | T03, F06 (prüfen), F08 (prüfen), Aufräumkonzept K11, K13 | Opus 5.5 | Reihenfolge der Würfe, Geld, Platzwahl 0x29C3B |
| `pruefung-karte` | 2 | P3 der Runde, Generatorlauf, Abweichungsliste nachziehen | Sonnet 5.5 | mechanisch |

Erstreviews: Opus 5.5 (Code und Belege), für `pruefung-karte` Sonnet 5.5. Fixläufe im Modell der Spur. Plan-Review Sonnet 5.5.
Gesamtreview ChatGPT (GPT-6.2 Astra) mit Claude-Auswertung.

### Spur `re-abweichungen` – Belege aus em.exe vor der Umsetzung (Welle 0, R25-D3 = B)

- **Teile:** 1. Bedienung/Anzeige: B12 (Datei 0x45483–0x4551E, auch Rechtsklick außerhalb des Logos), B13 (Spielerinfo per
  Rechtsklick in der Nationalaufstellung, eigene Stelle neben 0x40872?), B15/B16 (Gesicht 0x110:0x5C, Tore/Spiel und Schnitt
  0xE0:0x70, Wettbewerbscode 0xD0C2; Widerspruch A495/A753), B17 (Werte von 0x977F), B18 (Hauptmenüaufbau: Markierung, Kopf,
  Kurve und Lampe des Bundestrainers), B28 (12er-Kodierung A356, Pokalbalken A806), B29 (Wertverlauf je Verein A361), C07 (Marken
  vor 0x38402, DGROUP 0xA0BF); 2. Kern: K07 (Auswahlregel der Automatik A165, Verletzte, sechster Feldspieler), Aufräumkonzept K2
  (Strafbank-Abfrage 0x40364 beim Bundestrainer), K11 (Rollenwert +0x18), K13 (Platzwahl 0x29C3B inkl. A463), T03 (Zeitpunkt und
  Reihenfolge der Saisonwechsel-Würfe A661, A829, A843, A935), F06 (A888, Bande je Teilvertrag), F08 (A468, A509, A512, A526, A695).
- **Ergebnis je Thema:** Beleg als Nachbildung des Originalablaufs (Skript über die Bytes von em.exe mit Ausgabe, `zusammenarbeit.md`
  §5 Punkt 7), Urteil „Abweichung bestätigt“ bzw. „keine Abweichung“, Vorgabe für die umsetzende Spur (Regel, Fundstellen,
  erwartete Werte für den roten Test). Kein Produktcode.
- **Dateien:** `docs/analyse/re-abweichungen.md` (neu), `tools/reverse-engineering/abweichungen/*.py` (neu). **Prüfung:** Skripte
  laufen mit `~/referenz/original/gamedata/EM.EXE`, Ausgaben im Bericht; `--schnell` entfällt (kein Code unter `src/`).
  **Erstreview:** Opus 5.5 (Skripte ausführen, Stichprobe der Lesarten).
- **Status:** Teil 1 und Teil 2 belegt (B12–B29, C07, K07 mit A971/A972, K2, K11, K13, T03, F06, F08) – PR #998; Erstreview #1001 (Nein wegen RA-01 P2, RA-02 P3), Fixlauf 24.11.2026 behebt beide, Delta-Prüfung der Leitstelle ausstehend.

### Spur `bedienung` – Bedienung und Hauptmenü (Welle 1)

- **Teile:** 1. B01 (A002, Titelmusik einmal), B07 (A778–A780, Trennung an „^“, Hinweis und Zähler wie Original), B11 (A273,
  A348, A351, A354, A359, A479, A762, A783, A869: „jede Taste schließt“ wie im Original, Escape und Rechtsklick bleiben – B11 = A);
  2. B12 (A296: nach Beleg aus `re-abweichungen` Abfragen in `MainMenuScreen.cs` umordnen), B18 (A297, A301, A302, A331: Kachelmarkierung, Managerkopf, Kurve und Lampe des Bundestrainers aus dem Hauptmenüaufbau).
- **Dateien:** `Audio/AudioMixer.cs`, `UI/Screens/Manager/NewsBar.cs`, `UI/Screens/MainMenuScreen.cs`, `UI/Screens/MenuLamps.cs`,
  die Bildschirme von B11 (`Competitions/ClubScheduleWindow.cs`, `DebCupDrawLive.cs`, `WorldCupScreen.cs`, `QuitQuestion.cs`,
  `SetupScreen.cs`, `Statistics/ClubStatisticsScreen.cs`, `FairplayScreen.cs`, `HighscoreScreen.cs`, `PlayerStatisticsScreen.cs`),
  zugehörige Tests. **Prüfung:** `--schnell`; Bildschirmfotos Hauptmenü (Manager, Bundestrainer).
- **Status:** Teil 1 (B01, B07, B11) und Teil 2 (B12, B18) umgesetzt – PR #1002, Erstreview ausstehend.

### Spur `wettbewerbe-anzeige` – Paarungs-, Gegner- und Vereinsfenster (Welle 1)

- **Teile:** 1. B10 (A268, A269: gemerkte Seite, DGROUP 0x5123–0x5127, Datei 0x45862), B17 (A285, A419, A961: Anzeige über
  `NationalPanelLineValue` 0x977F, Aufräumkonzept K9); 2. B15 (A017, A286–A290: Gesicht 0x110:0x5C, Tore/Spiel und Schnitt
  0xE0:0x70) mit B16 prüfen (A433, A495, A753–A755, A867, A868: Wettbewerbscode 0xD0C2 lesen, ändern nur bei belegter Abweichung,
  B16 = A; Widerspruch A495/A753 auflösen); 3. C07 (A098, A277, A435, A482, A759, A924, A925: Marken am Ergebnis, Klick ins
  Vereinsfenster, Stand vor 0x38402 DGROUP 0xA0BF).
- **Dateien:** `UI/Screens/Competitions/CupPairingsScreen.cs`, `CupTableMenuEntries.cs`, `NationalWidgets.cs`,
  `TeamInfoWindow.cs`, `ClubInfoWindow.cs`, `ClubScheduleWindow.cs`, `LeaguePairingsScreen.cs`, `DebCupGamesScreen.cs`,
  `PlayoffGamesScreen.cs`, `WorldCupKnockoutScreen.cs`; `Core/CupsNational.TeamInfo.cs`, `CupsNational.cs`, `CupsNational.Lineup.cs` (A961, minimal), `Cups.Tiebreak.cs`,
  `Playoffs.cs`, Marken in `MatchEngine.Minutes.cs` (minimal); Tests. **Prüfung:** `--schnell`; Pixelvergleich der Fenster.
- **Status:** Teile 1–3 fertig (Claude Code, 24.11.2026) – PR #1003, Base `0c1811f`; wartet auf Erstreview.

### Spur `statistik-anzeige` – Saisonende, Historie, Stärken, Spielerfenster (Welle 1)

- **Teile:** 1. B25 (A342, A343: Symbol 5, Pokal aus kb\16), B28 (A356, A805, A806: 12er-Kodierung und Pokalbalken nach Beleg aus
  `re-abweichungen`); 2. B29 (A361, A699, A700: Wertverlauf je Verein nach Beleg, Feld im Spielstand), B33 (A372, A957, A958: Gegentore beim Verein,
  Feld im Spielstand). Migration(en) Schema 64 ff. in einem Schritt.
- **Dateien:** `UI/Screens/SeasonEndSummaryScreen.cs`, `SeasonEndTrophyScreen.cs`, `Statistics/HistoryScreen.cs`,
  `StrengthScreen.cs`, `Team/PlayerDataWindow.cs`, `PlayerHintLine.cs`, `PlayerInfoPopup.cs`; `Core/StatisticsStrength.cs`,
  neue Datei(en) für die Felder, Zählstellen der Gegentore `MatchEngine.Live.cs`, `MatchEngine.National.cs`,
  `MatchEngine.Statistics.cs` (je minimal; `MatchEngine.Statistics.cs` auch `saison-geld` K11 – wer später merged, rebast), `SaveGame.Migration.<thema>.cs`; Tests. **Prüfung:**
  `--schnell`; Mehrsaisonprobe (Felder über 4 Wechsel); Laden Schema 63.
- **Status:** Teil 1 und Teil 2 fertig – PR #1005 (24.11.2026). B25, B28, A361 (Bilanzzeichen und Gesicht), A372 umgesetzt;
  ohne neues Spielstandfeld: die Bilanzzeichen der Vereine sind die Ligaergebnisse der Saison (gleiche Menge wie 0x50283 bei
  Code 7, geleert im selben Saisonwechselschritt), A957 ist keine Abweichung (Torhüter-Vereinswert +0x22 schreibt das Original
  nie), A700 ist schon erfüllt (Lader in `NewGame.Create`). Keine Migration, Schema 64 bleibt frei.

### Spur `aufstellung` – Aufstellung, Taktik, Nationalaufstellung (Welle 1)

- **Teile:** 1. B31 (A892, A893, A956, A959, A960: Verlassen von Aufstellung/Taktik schreibt wie im Original 0x952C, als
  Kern-Ereignis, B31 = A; AGENTS.md §4 bleibt: Öffnen ändert nichts); 2. K07 (A165, A807, A936, A939, A971, A972: Automatik je
  Manager, Verletzte, sechster Feldspieler wie im Original); 3. Aufräumkonzept K2 (Strafbank beim Bundestrainer im Spiel, 0x40364),
  S07 (A874, A898: Einsätze beim Aufstellen im Spiel zählen, niedrige Priorität), B13 (A757: nur wenn `re-abweichungen` die Spielerinfo
  per Rechtsklick auch beim Nationalteam belegt).
- **Dateien:** `Core/Squad.Lineup*.cs`, `Squad.AutoLineup.cs`, `Squad.SlotValues.cs`, `CupsNational.Lineup.InMatch.cs`,
  `MatchEngine.LiveLineup.cs`, `GameState.TacticsLeft.cs`, `GameState.DailyRun.cs` (minimal); `UI/Screens/Team/LineupScreen.cs`,
  `TacticsScreen.cs`, `Competitions/NationalLineupScreen.cs`; Tests. **Prüfung:** `--schnell`; Mehrsaisonprobe (Kader).
- **Status:** PR #1004, Base `3d6f792`, Schema 64 (Entscheidung Leitstelle 24.11.2026). Alle Teile fertig (Fortsetzung 2): Teil 1 B31; Teil 2 K07 (A165, A807, A936, A939, A971/A972 – Kennzeichen sechster Platz als Bildschirmzustand `LineupGrid`); Teil 3 K2, B13, S07 (A898, A874 – Länderspiele beim Spielbeginn und beim Aufstellen im Spiel). Bereit für das Erstreview.

### Spur `saison-geld` – Saisonwechsel, Geld, Platzwahl (Welle 1)

- **Teile:** 1. T03 (A661, A829, A843, A935: Zeitpunkt und Reihenfolge von Wanderung, Ligamischung, Oberliga-Zug, Markt-Verfall wie
  im Original, Oberliga-Zug am 29. April); 2. F06 (A888) und F08 (A468, A509, A512, A526, A695) prüfen, belegte Abweichung ohne
  Rückfrage wie im Original ändern (F06/F08 = A); 3. Aufräumkonzept K13 (Platzwahl 0x29C3B: `NewGame.PlaceInStartLeague`/
  `SwapStartSlot` gegen `AddManager.Swap.DrawStartLeaguePlace` – eine Umsetzung wie im Original, inkl. A463), K11 (Rollenwert +0x18:
  ein belegter Wert für Export und Torwürfe).
- **Dateien:** `Core/SeasonFlow.*.cs`, `MatchEngine.TeamValues.cs`, `GameState.DailyRun.cs` (minimal), `FinanceSponsors.cs`,
  `FinanceStadium.cs`, `MatchGate.cs`, `StadiumStatistics.cs`, `NewGame.cs`, `GameState.AddManager*.cs`,
  `OriginalExport.Squads.cs`, `MatchEngine.Statistics.cs`; Migration nur falls F06 Felder je Bandenteil braucht; Tests.
  **Prüfung:** `--schnell`; Mehrsaisonprobe (passiv/aktiv, Geld).
- **Status:** Teil 1 (T03: A661, A829, A843, A935), Teil 2 (F06: A888; F08: A509, A512, A526 umgesetzt, A468, A695 nur Marker) und Teil 3 (K11: Rollenwert als Feld, Schema 65; K13: eine Platzwahl, Randfall widerlegt; A463 nur Marker) fertig – PR #1006 (Base `90d8394`), Spur fertig – Review (24.11.2026).

### Spur `pruefung-karte` – Abschluss der Runde (Welle 2)

- **Teile:** 1. gesammelte P3 der Runde; 2. einziger Lauf `markerliste.py --write`, Klassenzeilen der geänderten IDs in
  `restliste-v1-klassen.csv`, Status je Thema in `abweichungen-v1.md` Abschnitt 0 („umgesetzt“/„keine Abweichung“ mit PR).
- **Dateien:** `docs/analyse/*`, Kommentare der P3. **Prüfung:** `--schnell`. **Erstreview:** Sonnet 5.5.
- **Status:** Go 24.11.2026, Start nach Merge aller Welle-1-Spuren.

**Modellwahl (Nutzer 17.11.2026):** je Lauf möglichst wenig Tokens für ein brauchbares Ergebnis – Haiku, Sonnet, Opus; Fable nur
in Ausnahmefällen. **Läufe melden ihr Ende** per Push-Benachrichtigung (Nutzer 22.11.2026, `prozess/anhang.md` F).

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen

- **Runde 26 – Live-Spieltag (R25-D1 = D, R25-D2 = A):** Spur `live-verschraenkt` (Opus 5.5): S01 (A442, A450, A454, A541, A542,
  A547, A554, A555, A766, A909, A954; je Minute alle Paarungen, 0x37A62) erst nachbilden, dann Live-Kern umbauen, dann S04 (A292,
  A436, F068, F076; Quellen 0x3183C, 0x35CBE) und S05 (A448, A449, A453, A456, A457, A475); danach Spur `torszene-dauer` (S02,
  A472; Spieltagsablauf 0x36B5D/0x3901B, Moduswechsel 0x568A9–0x568B6).

- **DOSBox-Aufnahme `runde-22` Sitzungen 2–6** und **Sitzung 7** (neue D aus Runde 22: A671, A713/A949, A896, A899, A910, A912,
  A918, A945; Liste nach R23-D3 in `vorlagen/dosbox-aufnahme.md`): Aufnahme nach Lust, Auswertung in der Runde danach.
- **Abschlussprüfung:** 24 X-Funktionszeilen sind im Generator ausgeblendet (#976) – dort bestätigen. P3 #978, #980 im Aufräumen
  (R24-D2 = B).
- **Vor v1.0 (R20-D3-Zusatz):** Abweichungsliste vorgelegt und entschieden (24.11.2026, Runde 24).
- **Letzter Schritt vor v1.0 (Nutzer 17.11.2026, #936):** Aufräumen des gesamten Repos, danach nur noch die Abschlussprüfung.
- **Testlücke Gesamtreview #848:** Speichern/Laden mitten in einer angehaltenen Live-Paarung (über die Oberfläche nicht
  erreichbar; prüfen, sobald ein Speicherweg im Live-Spiel dazukommt).
- **Mit v1.0 und dem bitgenauen Zufallsmodus (N6 = A, Nutzer 16.11.2026):** eigene Zufallsquellen der Darstellung (A061, A271,
  A274, A438, A452) erneut prüfen.
- **Nach v1.0:** Einstellungen aus R15-E3, bitgenauer Zufallsmodus (`anforderungen/zusatzfunktionen.md` C), Sprachen und
  Währungen (N1 = B, `anforderungen/zusatzfunktionen.md` A 2).

## Paket `hok-format` – Original-Spielstände `*.HOK`

Branch: `paket/hok-format` · Status: Analyse gemergt (#176, Review #178): Verschlüsselung, Prüfsummen, Kopf und Blockfolge
belegt, `emtool hok dump`, Doku `docs/formate/spielstand-original.md` (offene Liste dort). Entscheidung Nutzer:
Sicher-/Ladesperre des Originals nicht übernehmen. Weiter in Runde 7 mit `hok-patch` (Spielstände fürs Original);
Import/Export ins Remake danach.

- Nachtrag (#197, Branch `paket/hok-format-2`): Kaderplätze, Spielerpool, Training und Taktik an FILE05 belegt;
  Kaderausgabe in `emtool hok dump`. Spielstände FILE01–05 und `KAPUTT.HOK` im Referenz-Repo `original/spielstaende/`.

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: PR 1–7 gemergt (Runde 3); Restarbeiten in Runde 4 gemergt; PR 8 in Runde 5 gemergt (`paarungen-liga`, #128). Regeln und Belege: [wettbewerbe.md](wettbewerbe.md)

| PR | Inhalt | Stand |
|---|---|---|
| 1 | DEB-Pokal: Auslosung, Heimrecht, „best of three“, Abrechnung, Bildschirm „DEB-Pokal Auslosung“ | gemergt (Runde 3, #54) |
| 2 | Europapokal (Gruppe + Endrunde), Bildschirm Gruppen/Endrunde | gemergt (Runde 3, #54) |
| 3 | WM (2 Gruppen + K.-o.), Bildschirm Gruppe/Tabelle WM | gemergt (Runde 3, #54) |
| 4 | Stärken (Ligen, Länderteams) | gemergt (Runde 3, #54) |
| 5 | Statistiken: Ewige Tabelle (Totale), Früher | gemergt (Runde 3, #54) |
| 6 | Bundestrainer: WM-Kader, „WM Vorbereitung“ (Freundschaftsspiele), Etat; WM: Werte Deutschlands neu ausgewürfelt, Länder 20-mal getauscht | gemergt (Runde 3, #54) |
| 7 | Spielmodus „nur Weltkugel“: Spielvorbereitung, Vereinssaison überspringen, Menü des Bundestrainers | gemergt (#87 über #89) |
| 8 | Paarungen Liga | gemergt (Runde 5, #128); Pixelvergleich Runde 6 (`pixel-wettbewerbe`) |
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | Runde 8 (`statistiken`, `nationalteam`) |

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: Format, Dialog und automatisches Speichern gemergt (Runde 3, [archiv/runde-3.md](archiv/runde-3.md)); #80 in Runde 4 behoben (#105)

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`) und Export per Vorlage (`hok-format`) – wartet auf Original-Spielstände des Nutzers.

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen auf den Ereignissen aus `spielberechnung` (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
