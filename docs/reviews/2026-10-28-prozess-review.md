# Prozess-Review – 28.10.2026 (Teilprüfung)

## 1. Merge-Empfehlung

**Nein; keine Freigabe.** Zielstände: #45 `6a6f227cca1ad5f1aab2122ac486d055a3e27a87`, #46 `237c3f66dda95e5d21413d86dbea76743874a2d1`, #50 `d01618a62c955aa5676972a1f270f0ffd5af95fc`, #52 `4af046aa262d3343a57009f6352a9ab2b6fdc776`. Basis laut Auftrag jeweils cc26bde, #50 auf #45. Dieses Dokument ist ausdrücklich kein abgeschlossenes Review aller vier Heads. PR-Diffs wurden über den Connector gelesen; deren vollständige Bindung an die Zielstände und ein gemeinsamer Merge wurden nicht verifiziert.

## 2. Befunde

- **P2 – Pflichtlauf akzeptiert beliebige Infrastrukturfehler.** [tools/pruefen.sh, Zeilen 61–69](https://github.com/kaaasi/eishockey-manager-redone/blob/d01618a62c955aa5676972a1f270f0ffd5af95fc/tools/pruefen.sh#L61): Jeder Exitcode ungleich null zählt als erwarteter Fehler, auch ohne Testergebnis. Reproduziert mit unverändertem Skript und einem dotnet-Testdouble: build exit 0; normaler Testlauf meldet `Passed! - Failed: 0, Passed: 7, Skipped: 2`; Pflichtlauf schreibt nur `Testhost crashed` und endet mit 1. Gesamtskript endet mit 0 und meldet bestanden. Erwartet: Infrastrukturfehler ablehnen und tatsächliche Fehler wegen fehlender Originaldaten nachweisen.
- **P2 – Löschung nicht gegen konkurrierenden Push abgesichert (statische Prüfung).** [aufraeumen.yml, Zeilen 30–42](https://github.com/kaaasi/eishockey-manager-redone/blob/4af046aa262d3343a57009f6352a9ab2b6fdc776/.github/workflows/aufraeumen.yml#L30): Nach Fetch/Ancestor-Prüfung kann ein neuer, ungemergter Commit auf den Branch gepusht werden; `git push origin --delete` löscht trotzdem den aktuellen Remote-Branch. Ebenso wird die Liste offener PRs nur einmal ermittelt. Gegenbeispielablauf: gemergten Branch prüfen, anschließend neuen Commit pushen oder PR öffnen, dann Löschung ausführen. Nicht gegen GitHub ausgeführt. Mindestens Löschung an geprüften SHA binden; verbleibende PR-Race ausdrücklich behandeln.

Übernahme: AGENTS.md und Vorlagen benötigen keinen Chatverlauf, verweisen aber notwendig auf weitere Repository-Dateien, PR-Status und Übergaben. AGENTS.md und zusammenarbeit.md wurden gelesen; vollständige Prüfung sämtlicher Vorlagen ausstehend. Konfliktfreiheit der vier PRs **nicht nachgewiesen**.

## 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

Git-Clone über github.com und git.chatgpt-team.site scheiterte an fehlender Authentifizierung. `command -v dotnet` fand kein SDK. Daher keine echten Builds, drei Testläufe oder Merge-Probe. Dateizugriff über GitHub-Connector möglich. Das Skript von exakt #50-Head wurde als Blob `df898b91a5538e560e0294ff7823846f11dcbdad` gelesen und isoliert mit Testdouble ausgeführt (`PATH=<probe>/bin:$PATH bash tools/pruefen.sh`).

| Testdouble-Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Daten | – | – | nicht vorhanden |
| Ohne Daten | 7 | 0 | 2 |
| Pflichtlauf: Testhost-Absturz | 0 | 0 | 0 |

Dies sind **keine Produkttestzahlen**. Ergebnis des Skripts: Exit 0, fälschlich bestanden. Weitere Zahlenparser-, Umgebungsvariablen- und Fehlermodusprüfungen offen. Keine [APPROX] fachlich akzeptiert. Annahmen und ungeprüfte Punkte sind keine Befunde über nachgewiesenes Laufzeitverhalten.
