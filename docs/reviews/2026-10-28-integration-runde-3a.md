# Integration Runde 3, Welle A – Prüfstatus 28.10.2026

## 1. Merge-Empfehlung

**Nein – Prüfung unvollständig, keine Freigabe.** Auftrag: PR #54, Base `ee7ea0ed58570312e564a3568cb1fff5bcb9d712`, Head `c622b0fe06b0000b74fe1f1f6ce91dbf365c068a`. Dieses Dokument hält einen blockierten Prüfstand fest und ersetzt das angeforderte unabhängige Inhaltsreview nicht.

## 2. Befunde

Keine neuen Produktfehler belastbar nachgewiesen. Das ist keine Fehlerfreiheitserklärung.

Erwartet offen: Etat als „Prämien“ statisch am exakten Head bestätigt: [CupsNational.GameState.cs, AppointNationalCoach, Zeile 64](https://github.com/kaaasi/eishockey-manager-redone/blob/c622b0fe06b0000b74fe1f1f6ce91dbf365c068a/src/EishockeyManager.Core/CupsNational.GameState.cs#L64) bucht `FinanceCategory.Praemien`. Gehaltspauschale bei leerem Kader und ausstehende Finanzbalance-Endabnahme sind im abgerufenen PR-Diff dokumentiert, jedoch hier nicht unabhängig im vollständigen Code bzw. durch Simulation bestätigt.

Offen bleiben Inhaltsreviews der acht Einzel-PRs #39, #40, #43, #44, #48, #49, #51, #41 einschließlich Aufrufern, neuer Tests und Mutationsgegenproben. Ebenso offen: genau einmalige Abrechnung sämtlicher Pokal-Heimspiele sowie Spielererhaltung über Kader-KI, WM-Kader und Transfermarkt. Der gelesene Nationalteam-Code verwendet Spielerreferenzen; dies allein beweist keine globale Erhaltungsinvariante.

## 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

Review-Vorgabe auf prozess/sparsam, AGENTS.md und Zusammenarbeit gelesen. #54-PR-Diff abgerufen; wegen Umfang nicht vollständig ausgewertet. CupsNational.GameState.cs am exakten Ziel-Head gelesen (Blob `06076f0db5a93f4d163ae397bc217db06cc9875e`). Keine eigene Implementierung in dieser Sitzung.

`git clone https://github.com/kaaasi/eishockey-manager-redone.git repo` sowie Clone über git.chatgpt-team.site scheiterten an fehlender Authentifizierung. `command -v dotnet` fand kein SDK. Kein vollständiger Checkout hergestellt; daher keine Build-, Test- oder Mutationsausführung.

| Geforderte Prüfung | Ergebnis |
|---|---|
| 88b5615: Build mit -warnaserror | Nicht ausgeführt |
| 9fb4a3b: SeasonPaymentTests.SeasonPremium_IsBookedOnce_InOwnCategory | Nicht ausgeführt |
| Ziel-Head: eigener Build | Nicht ausgeführt |
| tools/pruefen.sh: mit Originaldaten | Nicht ausgeführt, keine Zahlen |
| tools/pruefen.sh: ohne Originaldaten | Nicht ausgeführt, keine Zahlen |
| tools/pruefen.sh: Pflichtlauf ohne Daten | Nicht ausgeführt, keine Zahlen |

Im separaten Prozess-Teilreview ist ein Testdouble-Gegenbeispiel gegen die Auswertung des Pflichtlaufs dokumentiert; es ist kein Test von #54. Vor Verwendung des Skripts als Nachweis muss ein Infrastrukturabsturz von erwarteten Datenfehlern unterschieden werden. Keine [APPROX] fachlich akzeptiert. PR-Beschreibungen, dortige Testzahlen und Originalbelege wurden nicht als eigene Prüfergebnisse übernommen.
