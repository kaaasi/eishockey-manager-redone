# Tagesablauf (verbindlich seit Runde 2, Spur `fundament`)

Festgelegt mit Befund R2-02 aus dem [Folgereview](reviews/2026-10-26-folgereview.md). Zeit vergeht **nur** über
`GameState.Continue()` („Tag weiter“ per Klick aufs Vereinslogo der Infoleiste; „und tschüß…“ beendet seit #205 wie
im Original das Programm); Bildschirme schreiben beim Öffnen nichts fort.

## Zustand im Hauptmenü

Das Hauptmenü zeigt den Tag **T** des nächsten eigenen Spiels.

- Alle Tage **vor T** sind genau einmal verarbeitet, dazu der **Tageswechsel zu T** (#688, R688-04; `DayOpened`,
  `TrainedUntil == (Saison, T)`, `FinanceDay == T`): Tagesabschluss, Takt 0x07169 mit Training und Markt, Neuaufstellung und
  Listenwechsel der Tage mit Ereignis. `[ORIGINAL]` Datei 0x55204–0x552D3 (nach den Spielen von T − 1) und 0x54D68–0x54E7C liegen
  vor dem Menü 0x44E22 (Datei 0x54EB2); die Spielstände des Originals sind dort gesichert (Zähler 0x3B7 = T + 1).
- Das gilt auch für Tag 0 eines neuen Spiels und jeder Saison (#699; FILE01: Zähler 1). Im Modus „nur Weltkugel“ ist der erste Halt
  der erste WM-Tag (Sprung Datei 0x54CEE–0x54D38 ohne Tageslauf); sein Tageswechsel gilt als gerechnet und rechnet nichts (FILE11, FILE21).
- **Nicht** gerechnet sind 0x952C(5, 1) des Bundestrainers (Datei 0x54F9C, nach den Menüs), die Listenwechsel-Würfe der
  Manager nach dem ersten (je beim Weitergeben des Zugs), Schuldenprüfung und die Spiele von T.
- Jetzt bedienen die Manager ihre Bildschirme. Öffnen und Schließen ändert nichts; nur ausdrückliche Handlungen
  (Plan zeichnen, Kauf, Vertrag, Bank …) wirken – und zwar sofort.

## „Tag weiter“ – Reihenfolge je Tag

`Continue()` spielt Tag T (`SeasonFlow.ProcessDay`), rechnet den Tageswechsel zum nächsten Tag (`SeasonFlow.OpenDay` →
`OnDayOpen`) und hält am nächsten eigenen Spieltag wieder an:

| Schritt | Was | Wo |
|---|---|---|
| 1 | Nach den Menüs: Länderwerte 0x952C (Hauptweg) bzw. Schritt (a) der WM-Schleife, Ernennung am Tag mit Markierung 255 | `GameState.Integration.cs` → `OnDayManager(day)` |
| 2 | Ausgeschiedene Manager überspringen (keine Entlassung wegen Schulden, #688 R688S-03) | `SeasonFlow.CheckDismissed` |
| 3 | Ende der Vereinssaison / Saisonende | `SeasonFlow.ProcessDay` |
| 4 | Spiele des Tages: Liga (mit Spieltagsabrechnung), Play-Offs/Aufstiegsrunde, weitere Wettbewerbe | `SeasonFlow.ProcessDay` |
| 5 | Tageswechsel zum nächsten Tag: Finanzen (Tages-, Wochen-, Monatswechsel; Stadionbau am Zähler ≡ 0), Wanderung | `GameState.Integration.cs` → `OnDayOpen(day)` |
| 6 | **Tagesablauf 0x07169** nur am Takttag (Zähler 0x3B7 ≡ 0/4 mod 7, Mittwoch und Samstag; #678) je Manager: Markt (Verfall, Gebote, Angebote), Verträge, Heilung, **Training**, Tendenz, Aufstellung | `MainPathDailyRun(day)` (`GameState.DailyRun.cs`) |
| 7 | Tage mit Ereignis: Neuaufstellung je Satz (0x54D9E); Listenwechsel-Wurf bis zum ersten Managerzug (0x54E71), die weiteren beim Weitergeben des Zugs bzw. vor den Spielen (#699) | `MainPathEventDayLineups`, `MainPathListChanges`, `PassTurn` |

**Sommerpause** (Saisonwechsel, `GameState.RunSummerBreak`; `[ORIGINAL]` Datei 0x564D2–0x56593, #688): nach dem Kaderwechsel je
Kalendertag vom 1. Mai bis 9. September (Zähler 234–365: Datei 0x26F55 setzt 233, 0x564BC zählt weiter) Stadionbau (Zähler ≡ 0),
Tagesabschluss 0x3ED01 (Zinsen, Werbung, Börse, Monatsabschlüsse Ende Mai bis August mit Gehältern und Unterhalt) und Heilung;
danach die Werbeangebote des Saisonbeginns (0x56650). Gebucht wird im Buch der neuen Saison. Der 30. April hat im Original keinen
Tagesabschluss (die WM-Schleife schließt zuletzt den 29. April ab, Zähler 232; R688S-01): elf Monatsabschlüsse je Jahr, kein April.

Nach dem 13. April ersetzt die WM-Schleife die Schritte 5–7 (`CupsNational.WorldCupLoop.cs`: (c) des Vortags, (d)). **Tag 0**
(#699, `[ORIGINAL]` Datei 0x565A7/0x43A93 → 0x54D5C → 0x55073 → 0x55204): Ein neues Spiel (`BeginFirstTurn`) und jeder
Saisonwechsel (`StartNextSeason`, mit Verhandlungen nach diesen in `ReturnToFirstActiveManager`) öffnen Tag 0 vor dem ersten Menü
(`OpenStartDay`: Zähler 1 = 10. September, Tagesabschluss je Manager, Neuaufstellung, Listenwechsel-Wurf von Manager 0). Die
Jugendteams von Tag 0 (Datei 0x55008) rechnet das Remake nach dem Menü vor den Spielen (FILE01/FILE10/FILE27 zeigen im ersten
Menü die Startwerte); an den übrigen Tagen `[APPROX]` im Tageswechsel davor. Nur Spielstände vor Schema 45 holen den Tageswechsel
vor den Spielen des Tages nach. Satz 5 (Bundestrainer) wird an Tagen mit Ereignis ebenfalls vor den Menüs neu aufgestellt
(0x952C(5, 0), Datei 0x54DAA); nach den Menüs folgt nur 0x952C(5, 1).

Daraus folgt:

- **Training → Plan ändern:** Am angezeigten Takttag T ist dessen Training schon gerechnet; ein im Menü geänderter Plan gilt ab
  dem nächsten Takt (wie im Original). Test: `PassiveScreenTests.PlanChangedOnPausedTrainingDay_AppliesFromNextTick`.
- Der Transfermarkt reagiert auf einen Ligaspieltag am **nächsten Takttag**, vor dessen Menü.
- Im Hot-Seat fällt der Listenwechsel-Wurf von Manager i nach dem Menü von Manager i − 1 (`[ORIGINAL]` Datei 0x54E3F–0x54EF4,
  #699); der Markt kann sich zwischen zwei Zügen ändern.
- Der Tageslauf wird **nur** aus `GameState.Integration.cs` aufgerufen (Hauptweg) bzw. aus der WM-Schleife
  (`CupsNational.WorldCupLoop.cs`).
  Neue Tages-, Wochen- oder Saisonereignisse dort anschließen, nie in einem `Screen.Enter()`.

## Abnahme

`tests/EishockeyManager.UI.Tests/PassiveScreenTests.cs` (mit Originaldaten):

- `OpeningEveryTile_OnPausedDay_ChangesNoGameState` – an einem angehaltenen Trainingstag alle Kacheln öffnen und
  schließen: vollständiger Zustandsabdruck (alle Felder: Geld, Finanzbuch, Spielerwerte, Markt, innerer Zustand der
  Zufallsgeneratoren …) unverändert, Tag T weiterhin ungespielt (Tageswechsel gerechnet).
- `AfterBrowsing_EachDayIsProcessedExactlyOnce` – zwei gleiche Spiele, nur in einem werden alle Kacheln geöffnet;
  nach zwei Spieltagen ist der Zustand identisch.

## Offene Punkte

- **R2-02b (Spur `wirtschaft`, Core/FinanceSponsors.cs):** Das erste Öffnen von „Werbung“ würfelt die ersten Angebote
  aus (`GameState.Ads` → `RenewOffers`) und verbraucht den Finanz-Zufall. Folge: Geldstand hängt davon ab, ob/wann
  „Werbung“ geöffnet wurde (Probe: nach ca. 40 Tagen 2.920 DM Unterschied). Bis zur Behebung ist „Werbung“ aus den
  Abnahmetests ausgenommen; der strenge Test `OpeningEveryTile_IncludingAdvertising_FromFirstDay_ChangesNoGameState`
  ist sichtbar übersprungen und wird mit dem Fix eingeschaltet.
- Faule Anlage ohne Zufall/Geld: Der Trainingsbildschirm legt den Standardplan an (`PrepareTraining`), „Finanzen“
  legt am ersten Tag den Aktienmarkt an (`Stocks`). Beides passiert sonst am ersten Trainings- bzw. Finanztag mit
  demselben Ergebnis; es verschiebt keine Zufallsfolge. Sauberer wäre die Anlage beim Spielstart.
- `LineupScreen.Enter()` entfernt nicht verfügbare Spieler aus der Aufstellung (`Lineup.RemoveUnavailable`) –
  Bereinigung beim Öffnen; gehört eigentlich in den Tagesablauf (Paket `mannschaft`).
- Beobachtung (Paket `mannschaft`): Ein Plan, der **nur** aus „Special“ besteht, gilt als leer (`TrainingKind.Special = 0`,
  `TrainingPlan.IsEmpty`) und wird am nächsten Trainingstag durch den Standardplan ersetzt. Das entspricht vermutlich
  dem Original (Funktion 46E:0653 prüft auf 0) – bitte gegen die EXE bestätigen.
