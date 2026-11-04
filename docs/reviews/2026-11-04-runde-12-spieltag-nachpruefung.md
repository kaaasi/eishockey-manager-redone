# Review: PR #559 – Spieltag lässt beim Öffnen keine Zeit vergehen (Nachprüfung zu #557, Befund #558)

Geprüft: Head `f597a7a3db20cf903c875ac8b30fbf936ab4f929`, Base `824094f4e41c76af279941ff4296a473bd802ca6` (Head von #556).
Nachprüfung von `docs/reviews/2026-11-04-runde-12-gesamt-nachpruefung.md` (Branch `review/runde-12-gesamt-nachpruefung`, PR #557,
bisher geprüfter Head `824094f`), nur Delta `824094f..f597a7a` (2 Commits, 3 Dateien) und R12G-01 (#558).
Unabhängige Sitzung (geplanter Lauf, Claude Opus 5.5), nicht an der Umsetzung beteiligt.

## 1. Merge-Empfehlung

**Ja für `f597a7a3db20cf903c875ac8b30fbf936ab4f929`.** R12G-01 ist behoben: `Game.Continue()` läuft in der Eingabe „Tag weiter“
([`MainMenuScreen.cs:246`](https://github.com/kaaasi/eishockey-manager-redone/blob/f597a7a3db20cf903c875ac8b30fbf936ab4f929/src/EishockeyManager.UI/Screens/MainMenuScreen.cs#L246)),
`MatchDayScreen` erhält die Schritte über den Konstruktor und ruft in `Enter` kein `Continue()` mehr
([`MatchDayScreen.cs:46–65`](https://github.com/kaaasi/eishockey-manager-redone/blob/f597a7a3db20cf903c875ac8b30fbf936ab4f929/src/EishockeyManager.UI/Screens/MatchDayScreen.cs#L46-L65)).
Gegenbeispiel am Base rot, am Head grün; „Tag weiter“ führt genau ein `Continue()` aus; alle drei Läufe grün. Keine neuen Befunde.
Hinweis: #559 zielt auf `fix/r12-bildschirme-zustand` (#556), nicht auf `main` – Merge-Reihenfolge #556 → #559 einhalten.

## 2. Befunde

| ID | Status | Nachweis |
|---|---|---|
| R12G-01 (#558, P2) | **behoben** | siehe Gegenbeispiele unten |

Keine neuen Befunde (P0–P3).

Beobachtungen ohne Befund:

- `MatchDayScreen(host, menu, steps = null)`: Ein künftiger Aufrufer ohne Schritte bekommt still einen leeren Spieltag (sofort zurück
  ins Hauptmenü). Derzeit einziger Aufrufer ist das Hauptmenü; die Voreinstellung hält das Gegenbeispiel am Base kompilierbar.
  Unkritisch.
- `HighscoreScreen`-Konstruktor ruft `HighscoreStore.Record()` und schreibt die Bestenlisten-Datei – kein Spielzustand (Spielstand-Bytes
  unverändert, durch `MenuTileScreens_OpeningAndDrawingDoNotChangeGameState` abgedeckt) und als `[ORIGINAL]` belegt (Eintrag vor dem
  Anzeigen, Datei 0x45773/0x5642E). Außerhalb des Deltas, kein Verstoß gegen AGENTS.md §4.

## 3. Ausgeführte Prüfungen

Checkout: Klon, `git rev-parse HEAD` = `f597a7a3db20cf903c875ac8b30fbf936ab4f929`, `git status --porcelain` leer; SDK 10.0.402;
Originaldaten aus `kaaasi/eishockey-manager-referenz` (`tools/claude/fetch-reference.sh original`). Ausführung, nicht nur Codelesen.

**Gegenbeispiele** (temporäre Testdatei, nicht committet, je mit `EM_REQUIRE_GAMEDATA=1`):

| Gegenbeispiel | `824094f` (Base) | `4232e37` (roter Test) | `f597a7a` (Head) |
|---|---|---|---|
| #558: zwei Manager, `CurrentManager = 1`, Hauptmenü offen; `SaveGame.Save` vor/nach `host.Show(new MatchDayScreen(host, menu))` + `Draw` gleich | rot („Zeit vergeht beim Öffnen“) | rot | grün |
| Regressionstest `MatchDayScreen_OpeningAndDrawingDoNotChangeGameState` | – (nicht vorhanden) | rot | grün |
| „Tag weiter“ = genau ein `Continue()`: ein Manager, Spielstand-Klon (`SaveGame.Load`) + ein `clone.Continue()` byte-gleich mit dem Spiel nach Klick aufs Logo; Durchlauf bis zum Hauptmenü ändert Saison/Tag nicht mehr (1993/0 → 1993/3, 1 Schritt) | grün | grün | grün |

Gegenprobe (Mutation in temporärer Kopie): zusätzliches `Game.Continue()` vor `Host.Show(new MatchDayScreen(…))` im Hauptmenü →
u. a. `SeasonUiTests.ThreeSeasons_ThroughTheUserInterface`, `DebCupDrawFlowTests` (4), `IntegrationRunde3cTests` (2),
`StatisticsScreenTests.AfterFirstSeason_ScreensShowRecordedSeason` und das Gegenbeispiel „genau ein Continue“ rot. Ein doppelter Tag
fiele also auch ohne den neuen Test auf. Mutation zurückgenommen.

**Besonders geprüft:**

- *Ablauf, Banner, Abdrücke:* Die Schrittverarbeitung in `Enter`/`Next` ist unverändert, nur die Quelle der Schritte wechselt. `ScreenHost.Show`
  setzt nur `Current` und ruft `Enter` – kein `Leave`/Autospeichern dazwischen, die Reihenfolge `Continue` → Bildschirm ist damit
  gleichwertig. Keine Abdruckdatei im Delta; alle Pixel-/Abdrucktests grün.
- *Aufrufer:* `new MatchDayScreen(` in `src/` und `tests/`: nur `MainMenuScreen.cs:246` und der neue Test. `ScreenDriver.PlayMatchDay`/
  `NextDay`, Szenarien (`Manager`, `Speichern`, `Statistiken`, `StatistikenVerein`, `Mannschaft`, `Nationalteam`) und Desktop gehen
  über den Logo-Klick im Hauptmenü; keine Umstellung nötig. Weitere `Continue()`-Aufrufe in der UI: `MainMenuScreen.cs:238` (Hot-Seat,
  Eingabe), `SetupScreen.cs:204` („Los jetzt!“), `SeasonEndSummaryScreen.cs:103` (Rückruf nach Verhandlungen) – alle Eingabe-/Rückrufpfade.
- *Eigene Stichprobe `Enter`/`Draw`/Konstruktoren:* alle 7 `Enter`-Überschreibungen in `src/EishockeyManager.UI` gelesen (Training, Transfer,
  Laden/Speichern, Finanzen, Spieltag, Hauptmenü – sanktionierte Ausnahme E4 = B –, Spielende): nur Bildschirmzustand. Von `MatchDayScreen`
  erreichte Bildschirme (`SeasonEndTrophyScreen`, `DebCupDrawFlow.Create` → `DebCupDrawLiveScreen`/`DebCupGamesScreen`, `SeasonEndScreen`,
  `SeasonEndGameOverScreen` → `HighscoreScreen`): Konstruktoren speichern nur Argumente (Bestenliste siehe oben). Suche in allen
  `Draw`-Methoden nach Zuweisungen an `Game.*`, `Book(`, `Continue(`, Zufall: nur lokale Listen. Kein Fund.

**`tools/pruefen.sh` (alle drei Läufe) am Head:**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2162 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1818 | 0 | 308 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1819 | 343 | 0 |

Commit `f597a7a3db20cf903c875ac8b30fbf936ab4f929`, SDK 10.0.402, Ergebnis: bestanden. Build Release mit Warnungen als Fehler grün.

**Akzeptierte `[APPROX]`:** keine neuen im Delta. **Annahmen:** keine.
