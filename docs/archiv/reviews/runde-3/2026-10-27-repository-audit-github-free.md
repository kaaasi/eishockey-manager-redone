# Repository-Audit: Organisation, Codearchitektur und Git-Workflow (GitHub Free)

Stand: 2026-10-27. Basis: `main` @ `cc26bde939f6a94485bb2ee7f4dc3053741a8d3a`.

## Verbindliche Randbedingungen
- Privates Repository `kaaasi/eishockey-manager-redone`.
- **GitHub Free bleibt bestehen; kein Upgrade auf Pro oder Team empfehlen.**
- Keine Originaldateien, Videoframes oder proprietären Assets committen.
- Laufende Runde 3 (kader, wettbewerbe, finanzen; danach speichern) nicht durch organisatorische Großrefaktorierung stören.
- Claude-Leitstelle koordiniert und integriert, ChatGPT prüft unabhängig. Alle Änderungen nach Prüfung über kleine PRs.

## Untersuchungsumfang und Evidenz
Über authentifizierten GitHub-Zugriff gelesen: Repository-Baum auf main (184 Dateien, davon 51 in tests/; sieben Produktions- und vier Testprojekte), README.md, CLAUDE.md, docs/arbeitspakete.md (Auszug), Directory.Build.props, .github/workflows/build.yml, .github/workflows/werkzeuge.yml, Branch-Liste, jüngere Commits und offene PR-Metadaten (#39, #40, #41). Ruleset-API antwortete mit HTTP 403 und Hinweis auf Pro-Upgrade. Keine lokale Testausführung, keine vollständige zeilenweise Codeprüfung, keine umfassende historische Commit-Analyse und keine vollständige Prüfung aller CI-Runs. Die folgenden Punkte sind teils bestätigte Beobachtungen, teils explizit als Vorschläge markiert.

## Befunde
1. Die Projektaufteilung in Desktop, UI, Core, Formats, Audio, Tools und Viewer ist sinnvoll und sollte erhalten bleiben. `partial`-Dateien ermöglichen parallele KI-Arbeit, beseitigen aber keine fachliche Kopplung.
2. Zentrale Invarianten bei Transfers, Leihen, Saisonwechsel und Buchungen sind besonders kritisch; die vorhandenen Reviews und Regressionstests adressieren dies bereits.
3. `docs/arbeitspakete.md` (~35 KB) enthält Planung, Historie und laufenden Status in einer gemeinsamen Datei. Das erschwert langfristig die parallele Statuspflege.
4. Zahlreiche Paket- und Integrationsbranches bleiben bestehen; eine Bereinigung ist erst nach Prüfung ihrer Integration sinnvoll.
5. `build.yml` baut und testet synthetisch und meldet Originaldaten-Tests ohne proprietäre Daten als Skips. Ein grüner synthetischer CI-Lauf ist kein Nachweis der Originaltreue.
6. `build.yml` veröffentlicht Desktop und Tools nacheinander in dasselbe Verzeichnis `out`; getrennte Ausgabeordner sind robuster.
7. `werkzeuge.yml` spiegelt ein versioniertes .NET-SDK mit Prüfsummenprüfung und NuGet-Pakete für Claude. Dieses Verfahren ist nützlich.
8. `Directory.Build.props` setzt `TreatWarningsAsErrors=false`; schrittweise statische Analyse ist sinnvoller als sofortige globale Verschärfung.
9. PR #41 dokumentiert rot/grün-Regression, Originalbelege und Mehrsaisonproben vorbildlich. Dies sollte als PR-Standard gelten.

## GitHub-Free-Grenzen und Ersatzmaßnahmen
Bei privaten Repositories unter GitHub Free sind die benötigten Branch-Protection-/Ruleset-Funktionen nicht wie bei bezahlten Plänen verfügbar; der konkrete Ruleset-Leseversuch lieferte 403. **Keine Schutzfunktion als aktiv oder erzwingbar behaupten, bevor sie in diesem Repository erfolgreich geprüft wurde.**
- PR-only, Review-Pflicht, CI-Pflicht und kein Force-Push sind deshalb zunächst **Prozessregeln**, keine technische Merge-Sperre.
- Die Claude-Leitstelle prüft unmittelbar vor jedem Merge: aktueller main- und PR-Head-SHA, CI-Ergebnis auf genau diesem Head, Review-Freigabe für genau diesen Head, offene Befunde und Teststatus. Bei Head-Änderung Delta-Review.
- `main` nur durch die Leitstelle bzw. den Nutzer verändern; keine direkten Commits der Arbeitspakete.
- GitHub Actions, Issues, Projects, Labels, Milestones, Templates und Draft-PRs verwenden, soweit innerhalb der tatsächlichen Free-Kontingente verfügbar. Limits und konkrete Projektberechtigungen vor Automatisierung prüfen.
- Ein optionaler lokaler `pre-push`-Hook kann Fehler verhindern, ist aber **umgehbar und keine serverseitige Sperre**.
- GitHub-Actions-Minuten und Artifact-Speicherverbrauch beobachten, besonders bei Multi-RID-Publish und SDK-Mirror; kostenpflichtige Zusatzkontingente nicht voraussetzen.

## Empfehlungen und Prioritäten

| ID | Empfehlung | Grad | Begründung / Abnahme |
|---|---|---|---|
| G01 | Schriftliche Merge-Gate-Checkliste in CLAUDE.md; SHAs, CI, Review, Restbefunde vor Merge prüfen | Hoch | Kompensiert fehlende serverseitige Branch-Sperren; an einem Test-PR nachvollziehen |
| G02 | PR-Template mit Issue, Base/Head, Fehler vorher, Regression rot/grün, Tests synthetisch/Originaldaten/Skips, Restlücken | Hoch | Einheitliche Prüfbarkeit; Muster-PR ausfüllen |
| G03 | GitHub Issues für neue Aufgaben und Review-Befunde, bestehende Dokumentation zunächst belassen | Hoch | Eindeutige Zuständigkeit, weniger konkurrierende Statusänderungen; keine sofortige Massenmigration |
| G04 | Zentrale GameState-Invarianten in synthetischen Mehrsaison- und Übergangstests | Hoch | Verhindert doppelte Spieler/Buchungen und Teilmutationen; mindestens vier Saisonwechsel passiv/aktiv |
| G05 | Originaltreue-Register mit [ORIGINAL]/[APPROX], Beleg, Implementierung, Test und offener Unsicherheit | Hoch | Verhindert unbelegte Paritätsbehauptungen; zunächst kritische Finanz- und Saisonpfade |
| G06 | Getrennte CI-Aussagen: technisch grün vs. Originaldaten tatsächlich verifiziert | Hoch | Skips nicht als Originaltreue-Nachweis interpretieren |
| G07 | `publish` für Desktop und Tools in getrennte Verzeichnisse | Mittel | Keine Dateivermischung; Artefaktinhalt prüfen |
| G08 | Kurzlebige PR-Branches pro Änderung, alte Branches nur nach Merge-/Reachability-Prüfung löschen | Mittel | Weniger Altlasten und Konflikte; keine laufenden Paketbranches löschen |
| G09 | GitHub Project als Übersicht, Issues als Aufgaben, docs/arbeitspakete.md als Plan/Archiv | Mittel | Bessere Koordination; erst nach Runde 3 schrittweise migrieren |
| G10 | Architektur-Invarianten/Abhängigkeitsregeln automatisieren, dann GameState/SeasonFlow schrittweise entkoppeln | Mittel | Fachliche Schnittstellen statt reiner partial-Dateitrennung; keine Big-Bang-Refaktorierung |
| G11 | Persistenz-Schema versionieren, stabile IDs, Roundtrip- und Migrationstests | Mittel | Robustheit über spätere Erweiterungen |
| G12 | `dotnet format --verify-no-changes`, Analyzer schrittweise, gezielte Warnungen als Fehler | Mittel | Qualität ohne disruptive Massenänderung |
| G13 | Versionierte Releases und ADRs für zentrale Architekturentscheidungen | Mittel | Reproduzierbarkeit und Entscheidungsnachvollziehbarkeit |
| G14 | CODEOWNERS zur Orientierung, sofern nützlich; **nicht** als erzwingbares Approval-Gate annehmen | Niedrig | Reviewer-Hinweise ohne falsche Schutzannahmen |
| G15 | Conventional Commits / Changelog-Automatisierung | Nice to have | Komfort; aktuell kein Engpass |

## Reihenfolge
**Sofort, kleine PRs:** G01, G02, G06, G07. G03 nur für neue Aufgaben starten.
**Nach Runde 3:** G04/G05 weiter ausbauen, G08/G09, G12; erst nach fachlicher Bewertung G10.
**Vor v1.0:** G11, G13. G14/G15 optional.


## G16 – Agentenunabhängige Entwicklung und Ausfallsicherheit (Hoch)

**Auslöser:** Wenn Claude Code sein Nutzungskontingent erreicht, soll ChatGPT oder ein anderer Agent Entwicklung, Review und bei Bedarf Integration kontrolliert fortsetzen können. Die bisherige Rollenverteilung „Claude entwickelt, ChatGPT reviewt“ ist eine Präferenz, keine technische Voraussetzung.

**Maßnahmen:**
1. Ein kurzes, toolneutrales `AGENTS.md` als verbindlichen Einstiegspunkt erstellen: Architektur, Setup, Tests, Invarianten, Branch-/PR-Regeln, Originaldaten-Schutz, Dokumentationsorte. `CLAUDE.md` behält nur Claude-spezifische Bedienhinweise und verweist auf die gemeinsamen Regeln. Widersprüche vermeiden; bestehende Detailregeln nicht unkontrolliert duplizieren.
2. Arbeitspakete agentenunabhängig beschreiben: Issue-ID, Ziel und Akzeptanzkriterien, Base-SHA, zugewiesener Branch, erlaubte Dateigrenzen, Abhängigkeiten, Regressionstest und Originaldatenbedarf. GitHub Issues sind die kanonische Quelle für neue Pakete; laufende Runde 3 zunächst nicht umorganisieren.
3. Standardisiertes `docs/handoffs/<issue>-<branch>.md` oder einen strukturierten Issue-Kommentar als Übergabe nutzen: Stand/Head-SHA, geänderte Dateien, Tests mit exakten Befehlen und Ergebnissen, offene Befunde, uncommittete Änderungen (müssen vor Übergabe gesichert oder explizit gemeldet sein), nächste konkrete Schritte. Keine Secrets, keine Originalassets.
4. Übernahmeprotokoll: Übernehmender Agent liest Regeln, Issue, PR und Handoff, prüft aktuellen Head und Branchzustand, führt Basistests aus und arbeitet erst danach weiter. Kein gleichzeitiger Schreibzugriff auf denselben Branch. Wenn derselbe Branch weiterverwendet wird, erfolgt eine ausdrückliche Übergabe; sonst eigener Branch und neuer PR.
5. Integrationsrolle nicht an Claude binden: Ein Agent kann nach Nutzerfreigabe die Leitstelle übernehmen. Unmittelbar vor Merge müssen **aktueller** Head/Base, CI auf dem Head, unabhängiges Review, Originaldatenstatus und Restlücken geprüft werden. Bei geänderten SHAs erneutes Review. GitHub Free erzwingt diese Regeln nicht serverseitig.
6. Rollenprinzip: Implementierer soll nicht alleiniger finaler Freigeber des eigenen PR sein. Bei fehlendem zweitem Agenten entscheidet der Nutzer nach dokumentiertem Review-/Teststatus. Keine automatische Integration bei erschöpftem Kontingent.
7. **Pilot:** Ein kleines, nichtkritisches Arbeitspaket von Claude an ChatGPT übergeben und mit dokumentierter Handoff-Checkliste, grünem CI und unabhängigem Review abschließen. Danach Format anpassen.

**Abnahme:** Ein neuer Agent kann ohne Zugriff auf den bisherigen Chatverlauf binnen eines Arbeitspakets aus GitHub-Daten reproduzierbar übernehmen. Kein exklusiver Claude-Zugang, keine unbekannten lokalen Zustände, keine parallelen Branch-Schreibzugriffe.

**Reihenfolge:** G16 vor bzw. gemeinsam mit G01–G03 umsetzen. Nur `AGENTS.md`, Handoff-Vorlage und Pilot zunächst; keine laufenden Branches oder Paketdateien ohne Abstimmung verändern.

## Validierungsauftrag an Claude
1. Lies CLAUDE.md, docs/arbeitspakete.md, die aktuellen Reviews, diese Datei sowie relevante Workflow- und Core-Dateien.
2. Verifiziere jede Beobachtung gegen den aktuellen main-Head; kennzeichne `bestätigt`, `teilweise`, `nicht bestätigt` oder `überholt`. Prüfe GitHub-Free-Funktionalität ohne Upgrade-Annahme.
3. Prüfe ausdrücklich G16 auf die Übernahme durch ChatGPT bei erschöpftem Claude-Kontingent. Prüfe bestehende Implementierungen, um Dopplungen zu vermeiden. Bewerte Nutzen, Risiko, Aufwand, Dateikonflikte und Abhängigkeiten jeder Empfehlung.
4. Lege einen konkreten gestaffelten Umsetzungsplan mit kleinen PRs, Akzeptanzkriterien und Tests vor. Nur risikoarme, eindeutig bestätigte Quick Wins nach den normalen Projektregeln implementieren; keine großen Refactorings, kein Löschen von Branches und keine Änderungen an laufenden Paketdateien ohne explizite Freigabe.
5. Jeder Implementierungs-PR enthält Base-/Head-SHA, Fehler oder Motiv, Tests, Restlücken und Auswirkungen auf GitHub-Free-Kontingente.
