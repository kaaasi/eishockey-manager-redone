# Tagesablauf (verbindlich seit Runde 2, Spur `fundament`)

Festgelegt mit Befund R2-02 aus dem [Folgereview](reviews/2026-10-26-folgereview.md). Zeit vergeht **nur** über
`GameState.Continue()` („Tag weiter“ per Klick aufs Vereinslogo der Infoleiste; „und tschüß…“ beendet seit #205 wie
im Original das Programm); Bildschirme schreiben beim Öffnen nichts fort.

## Zustand im Hauptmenü

Das Hauptmenü zeigt den Tag **T** des nächsten eigenen Spiels.

- Alle Tage **vor T** sind genau einmal verarbeitet (`TrainedUntil == (Saison, T − 1)`).
- Tag **T selbst ist noch nicht verarbeitet** – kein Training, keine Finanzen, keine Spiele dieses Tages.
- Jetzt bedienen die Manager ihre Bildschirme. Öffnen und Schließen ändert nichts; nur ausdrückliche Handlungen
  (Plan zeichnen, Kauf, Vertrag, Bank …) wirken – und zwar sofort.

## „Tag weiter“ – Reihenfolge je Tag

`Continue()` verarbeitet ab Tag T jeden Tag einzeln (`SeasonFlow.ProcessDay`) und hält am nächsten eigenen
Spieltag wieder an:

| Schritt | Was | Wo |
|---|---|---|
| 1 | Finanzen bis einschließlich heute: Tages-, Wochen-, Monatswechsel (Werbung, Gehälter, Aktien …) | `GameState.Integration.cs` → `AdvanceFinanceTo(day)` |
| 2 | **Tagesablauf 0x07169** nur am Takttag (Zähler 0x3B7 ≡ 0/4 mod 7, Mittwoch und Samstag; #678) je Manager: Markt (Verfall, Gebote, Angebote), Verträge, **Training** mit dem **jetzt gültigen Plan** | `GameState.Integration.cs` → `MainPathDailyRun(day)` (`GameState.DailyRun.cs`) |
| 3 | Listenwechsel des Transfermarkts: an Tagen mit Ereignis ein Wurf je Managerzug (0x54E71) | `GameState.Integration.cs` → `MainPathListChanges(day)` |
| 4 | Schulden prüfen, ggf. Entlassung | `SeasonFlow.CheckDebts` |
| 5 | Ende der Vereinssaison / Saisonende | `SeasonFlow.ProcessDay` |
| 6 | Spiele des Tages: Liga (mit Spieltagsabrechnung), Play-Offs/Aufstiegsrunde, weitere Wettbewerbe | `SeasonFlow.ProcessDay` |

Daraus folgt:

- **Plan ändern → Training des Tages:** Ein am angezeigten Tag T geänderter Trainingsplan gilt schon für das Training
  von Tag T (abgerechnet beim Weiterspielen). Test: `PassiveScreenTests.PlanChangedOnPausedDay_AppliesToThatDaysTraining`.
- Der Transfermarkt reagiert auf einen Ligaspieltag am **nächsten verarbeiteten Takttag** (Schritt 2 läuft vor den Spielen).
- Der Tageslauf wird **nur** aus `GameState.Integration.cs` aufgerufen (Hauptweg) bzw. aus der WM-Schleife
  (`CupsNational.WorldCupLoop.cs`).
  Neue Tages-, Wochen- oder Saisonereignisse dort anschließen, nie in einem `Screen.Enter()`.

## Abnahme

`tests/EishockeyManager.UI.Tests/PassiveScreenTests.cs` (mit Originaldaten):

- `OpeningEveryTile_OnPausedDay_ChangesNoGameState` – an einem angehaltenen Trainingstag alle Kacheln öffnen und
  schließen: vollständiger Zustandsabdruck (alle Felder: Geld, Finanzbuch, Spielerwerte, Markt, innerer Zustand der
  Zufallsgeneratoren …) unverändert, Tag T weiterhin unverarbeitet.
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
