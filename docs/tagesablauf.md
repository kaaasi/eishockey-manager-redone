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
- **Nicht** gerechnet sind 0x952C des Bundestrainers (Datei 0x54F9C, nach den Menüs), Schuldenprüfung und die Spiele von T.
- Jetzt bedienen die Manager ihre Bildschirme. Öffnen und Schließen ändert nichts; nur ausdrückliche Handlungen
  (Plan zeichnen, Kauf, Vertrag, Bank …) wirken – und zwar sofort.

## „Tag weiter“ – Reihenfolge je Tag

`Continue()` spielt Tag T (`SeasonFlow.ProcessDay`), rechnet den Tageswechsel zum nächsten Tag (`SeasonFlow.OpenDay` →
`OnDayOpen`) und hält am nächsten eigenen Spieltag wieder an:

| Schritt | Was | Wo |
|---|---|---|
| 1 | Nach den Menüs: Länderwerte 0x952C (Hauptweg) bzw. Schritt (a) der WM-Schleife, Ernennung am Tag mit Markierung 255 | `GameState.Integration.cs` → `OnDayManager(day)` |
| 2 | Schulden prüfen, ggf. Entlassung | `SeasonFlow.CheckDebts` |
| 3 | Ende der Vereinssaison / Saisonende | `SeasonFlow.ProcessDay` |
| 4 | Spiele des Tages: Liga (mit Spieltagsabrechnung), Play-Offs/Aufstiegsrunde, weitere Wettbewerbe | `SeasonFlow.ProcessDay` |
| 5 | Tageswechsel zum nächsten Tag: Finanzen (Tages-, Wochen-, Monatswechsel; Stadionbau am Zähler ≡ 0), Wanderung | `GameState.Integration.cs` → `OnDayOpen(day)` |
| 6 | **Tagesablauf 0x07169** nur am Takttag (Zähler 0x3B7 ≡ 0/4 mod 7, Mittwoch und Samstag; #678) je Manager: Markt (Verfall, Gebote, Angebote), Verträge, Heilung, **Training**, Tendenz, Aufstellung | `MainPathDailyRun(day)` (`GameState.DailyRun.cs`) |
| 7 | Tage mit Ereignis: Neuaufstellung je Satz (0x54D9E), Listenwechsel ein Wurf je Managerzug (0x54E71) | `MainPathEventDayLineups`, `MainPathListChanges` |

Nach dem 13. April ersetzt die WM-Schleife die Schritte 5–7 (`CupsNational.WorldCupLoop.cs`: (c) des Vortags, (d)). Ein neues
Spiel, der erste Tag einer Saison und Spielstände vor Schema 45 holen den Tageswechsel vor den Spielen des Tages nach.

Daraus folgt:

- **Training → Plan ändern:** Am angezeigten Takttag T ist dessen Training schon gerechnet; ein im Menü geänderter Plan gilt ab
  dem nächsten Takt (wie im Original). Test: `PassiveScreenTests.PlanChangedOnPausedTrainingDay_AppliesFromNextTick`.
- Der Transfermarkt reagiert auf einen Ligaspieltag am **nächsten Takttag**, vor dessen Menü.
- `[APPROX]` Im Hot-Seat rechnet das Remake die Listenwechsel-Würfe aller Manager vor dem ersten Menü des Tages (Original: Wurf
  von Manager i nach dem Menü von Manager i − 1).
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
