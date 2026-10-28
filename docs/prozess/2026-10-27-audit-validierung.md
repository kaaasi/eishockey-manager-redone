# Validierung des Repository-Audits (PR #42) und Maßnahmenplan

**Datum:** 27.10.2026 · **Leitstelle:** Claude Code · **Geprüfter Stand:** `main` @ `cc26bde939f6a94485bb2ee7f4dc3053741a8d3a`
**Grundlage:** [Audit in PR #42](https://github.com/kaaasi/eishockey-manager-redone/pull/42)
(`docs/reviews/2026-10-27-repository-audit-github-free.md`, Head `046f461`), `AGENTS.md`/`CLAUDE.md`,
`docs/arbeitspakete.md`, Reviews in `docs/reviews/`, Workflows, Repo-Einstellungen über die REST-API, Build und Tests lokal.

**Kennzeichnung:** *bestätigt* = selbst nachgeprüft; *teilweise* = Befund stimmt nur zum Teil oder ist schon zum Teil
umgesetzt; *nicht bestätigt* = trifft so nicht zu; *Annahme* = nicht prüfbar aus dieser Umgebung.

## 1. Eigene Prüfungen (Evidenz)

| Prüfung | Ergebnis |
|---|---|
| Branch-Schutz und Rulesets (`GET …/branches/main/protection`, `GET …/rulesets`) | beide HTTP 403 „Upgrade to GitHub Pro or make this repository public“ – **bestätigt nicht verfügbar** |
| Repo-Einstellungen | privat; Issues und Projects an; Wiki aus; `delete_branch_on_merge=false`; alle drei Merge-Arten erlaubt; Auto-Merge aus |
| Draft-PRs im privaten Free-Repo | verfügbar (#41, #42 sind Drafts) |
| Issues, Labels, Milestones | 0 Issues, nur Standard-Labels, 0 Milestones |
| Branches | 45 im Remote; **37 vollständig in `main` enthalten**, 5 mit offenen Commits (Audit, `paket/finanzen`, `paket/kader`, `paket/wettbewerbe`, `paket/wettbewerbe-europapokal`) |
| **Artefakt-Speicher** | 39 nicht abgelaufene Artefakte, **zusammen ca. 1,43 GB** (je Push auf `main` 3 × 35–37 MB, Aufbewahrung 14 Tage). GitHub Free enthält für private Repos **500 MB**, stündlich gemessen; ohne Zahlungsmittel wird die Nutzung nach Erreichen des Kontingents blockiert ([GitHub-Doku](https://docs.github.com/en/billing/concepts/product-billing/github-actions)) |
| Actions-Minuten | 71 Läufe seit 24.10., 159 Jobs, ca. **126 abrechenbare Minuten** (aufgerundet je Job, alle auf Linux) → bei gleichem Tempo ca. 1.300 von 2.000 Minuten im Monat |
| Abrechnungsseite | aus dieser Umgebung nicht abrufbar (nur repo-bezogene Endpunkte) – *Annahme*: noch keine Sperre, da Uploads am 26.10. erfolgreich waren |
| Warnungen | `main` und die Heads der offenen PRs #39, #40, #41 bauen mit **0 Warnungen** |
| `dotnet format --verify-no-changes` | 140 Meldungen (keine `.editorconfig`) – Einführung bedeutet Massenformatierung |
| Marker im Code | 131 × `[APPROX]`, 40 × `[ORIGINAL]` in `src/` |
| `publish` in denselben Ordner `out` | Tools hat keine eigenen NuGet-Pakete; gemeinsame Assemblies stammen aus demselben Build; `deps.json`/`runtimeconfig.json` sind je Programm getrennt benannt |
| Lauf „ohne Originaldaten“ lokal | bisher nur durch Verschieben von `gamedata/` möglich (fehleranfällig) |
| `tools/claude/setup-workspace.sh` | braucht `gh`; ChatGPT konnte es in Runde 2 nicht ausführen (laut PR #33/#34) |

## 2. Validierungsmatrix G01–G16

Aufwand: S = unter 1 Stunde, M = halber Tag, L = mehrere Tage. Konfliktrisiko bezieht sich auf die laufende Runde 3.

| ID | Befund | Umsetzung bisher | Free-tauglich | Nutzen | Risiko / Konflikte | Aufwand | Priorität | Entscheidung |
|---|---|---|---|---|---|---|---|---|
| **G01** Merge-Prüfliste | bestätigt (kein Branch-Schutz) | teilweise: Ablauf je PR in `arbeitspakete.md`, Merges der Leitstelle schon mit `sha` | ja | hoch: ersetzt fehlende Sperren | keins | S | hoch | **übernommen, erweitert** um die SHA-Bindung beim REST-Merge (einzige serverseitige Bindung); agentenneutral in `docs/prozess/zusammenarbeit.md` §6 statt in CLAUDE.md |
| **G02** PR-Vorlage | bestätigt (keine Vorlage) | Pflichtangaben nur als Text | ja | hoch | keins; Hinweis: GitHub füllt die Vorlage nur bei PRs aus der Weboberfläche, nicht bei REST | S | hoch | **übernommen** (`.github/pull_request_template.md`), Pflicht auch für REST-PRs in AGENTS.md |
| **G03** Issues | bestätigt (0 Issues) | – | ja | mittel: konfliktfreie Befundliste | weitere Statusquelle neben `arbeitspakete.md`; Zugriff der Agenten auf Issues je Werkzeug unterschiedlich | M | mittel | **angepasst, zurückgestellt:** ab Integration Runde 3 nur für Review-Befunde (eine Issue je Befund, Labels `befund`, `P0`–`P3`); Pakete bleiben in `arbeitspakete.md`. Freigabe nötig |
| **G04** Invarianten-Tests | bestätigt als Daueraufgabe | teilweise: Mehrsaisonprobe Pflicht (Runde 3), `MultiSeasonSquadTests`, Lebenszyklus- und Ansichtstests mit Zustandsabdruck | ja | hoch | `#39` ändert gerade `MultiSeasonSquadTests` | M | hoch | **übernommen, nach Integration Runde 3 Welle A:** gemeinsamer Prüfer `GameInvariants` (Spieler eindeutig, Nummern, Verträge, Leihen, Buchungen) für alle Mehrsaisontests |
| **G05** Originaltreue-Register | teilweise: Marker sind vorhanden und konsequent genutzt | Marker im Code (131/40) | ja | mittel | ein handgepflegtes Register veraltet sofort | S–M | mittel | **angepasst:** kein Handregister, sondern ein **erzeugter Markerbericht** (Skript, CI-Zusammenfassung). Pilotpaket für G16 |
| **G06** CI-Aussage trennen | teilweise: Testbericht trennt synthetisch/Originaldaten/Skips und warnt bei Skips | weitgehend | ja | mittel | keins | S | mittel | **übernommen als Ergänzung:** ausdrücklicher Satz „Originaldaten in der CI nicht geprüft“ im Testbericht (#46) |
| **G07** getrennte `publish`-Ordner | formal zutreffend, **kein Schaden nachweisbar** (siehe §1) | – | ja | gering | Trennung ändert den Inhalt der Downloads (emtool liegt bewusst neben dem Spiel, beide nutzen `gamedata/`) | S | niedrig | **verworfen** in dieser Form. Das echte Problem des Publish-Jobs ist der Speicher (N1) |
| **G08** Branch-Bereinigung | bestätigt (45 Branches, 37 gemergt) | – | ja (inkl. „Automatically delete head branches“) | mittel | Löschen ist endgültig; Paket-Branches wurden bisher für mehrere PRs wiederverwendet | S | mittel | **übernommen, Freigabe nötig:** die 37 gemergten Branches löschen und automatisches Löschen nach Merge einschalten; künftig je PR ein frischer Branch |
| **G09** GitHub Project | – | – | ja | gering bei einem Konto | dritte Statusquelle; Projects-Endpunkte sind aus Claudes Sitzung nicht erreichbar | M | niedrig | **verworfen** für jetzt; neu prüfen, falls Issues (G03) sich bewähren |
| **G10** Architekturregeln, Entkopplung | teilweise: Abhängigkeitsrichtung ist schon durch Projektverweise erzwungen (Formats ← Core ← UI ← Desktop/Tools) | – | ja | mittel langfristig | hohes Konfliktrisiko mit allen Spuren | L | niedrig jetzt | **zurückgestellt** bis nach `speichern`; dann als Entscheidungsvorlage (ADR) |
| **G11** Persistenz-Schema | zutreffend | teilweise: `speichern` verlangt Zufallszustand, Objektidentität, offene Vorgänge | ja | hoch | keins, Paket noch nicht begonnen | – (Teil von `speichern`) | **hoch** | **übernommen, vorgezogen:** Schemaversion, stabile IDs, Roundtrip- und Mehrsaison-Roundtrip-Test werden Abnahmekriterien von `speichern` (Runde 3 Welle B), nicht erst vor v1.0 |
| **G12** Format/Analyzer | bestätigt (`TreatWarningsAsErrors=false`, 140 Format-Meldungen) | – | ja | mittel | Massenformatierung kollidiert mit Runde 3 | S (Warnungen), M (Format) | mittel | **geteilt:** Warnungen als Fehler **jetzt** in CI und `tools/pruefen.sh` (0 Warnungen auf `main` und allen offenen PR-Heads); `.editorconfig` + `dotnet format` **nach Runde 3** |
| **G13** Releases, ADRs | zutreffend | – | ja (Release-Dateien zählen nicht zum Artefakt-Speicher) | mittel | keins | S–M | mittel | **übernommen, angepasst:** Downloads künftig über getaggte Releases statt Artefakte je Push (zusammen mit N1); ADRs erst mit G10 |
| **G14** CODEOWNERS | zutreffend, dass es nicht erzwingt | – | Datei ja, Wirkung nein | keiner (ein Konto) | falsche Sicherheit | S | – | **verworfen** |
| **G15** Conventional Commits | – | – | ja | gering | widerspricht der Regel „deutsche, aussagekräftige Commit-Nachrichten“ | S | – | **verworfen**; stattdessen Pflicht-Trailer für den Agenten (AGENTS.md §6) |
| **G16** Agentenunabhängigkeit | bestätigt: Regeln standen in `CLAUDE.md`, Setup brauchte Claude-Werkzeuge, Übergaben liefen über den Chat | teilweise: Pflichtangaben, Reviews als Dateien | ja | **hoch** | gering; `CLAUDE.md` wird umgebaut, keine offene PR ändert sie | M | **hoch** | **übernommen, umgesetzt** (#45), Pilot geplant (§5) |

### Zusätzliche Befunde (nicht im Audit)

| ID | Befund | Priorität | Maßnahme |
|---|---|---|---|
| **N1** | Artefakt-Speicher ca. 1,43 GB bei 500 MB Kontingent; bei gleichem Tempo wächst er im November auf ein Vielfaches. Folge ohne Zahlungsmittel: Nutzung blockiert, mindestens die Artefakt-Uploads (GitHub meldet das nicht vorab) | **hoch** | #47: Publish nur noch manuell und bei Tags, Aufbewahrung 7 Tage; alte Artefakte löschen. **Freigabe nötig** |
| **N2** | Actions-Minuten ca. 1.300/Monat bei jetzigem Tempo | niedrig | beobachten; bei mehr Parallelarbeit Testläufe auf geänderte Pfade begrenzen |
| **N3** | Lauf „ohne Originaldaten“ lokal nur durch Verschieben von `gamedata/` | mittel | `EM_SKIP_GAMEDATA=1` + `tools/pruefen.sh` (#45) |
| **N4** | Alle Agenten schreiben über das Konto `kaaasi`; ChatGPT-Commits aus Runde 2 tragen keinen Hinweis auf den Urheber | mittel | Pflicht-Trailer und `Bearbeiter:` im PR (AGENTS.md, Vorlage) |
| **N5** | `setup-workspace.sh` setzt `gh` voraus | niedrig | AGENTS.md beschreibt den werkzeugneutralen Weg (SDK nach `global.json`, nuget.org oder Spiegel) |

## 3. Umsetzungsplan

| # | Maßnahme | Ziel / Nutzen | Dateien | Abhängig von | Aufwand / Risiko | Abnahme und Tests | Stand |
|---|---|---|---|---|---|---|---|
| M1 | **Agenten-Grundlage** (G16, G01, G02, N3, N4, N5) | jeder Agent kann ohne Chatverlauf arbeiten, übergeben, prüfen | `AGENTS.md` (neu), `CLAUDE.md`, `docs/prozess/zusammenarbeit.md`, `docs/handoffs/VORLAGE.md`, `.github/pull_request_template.md`, `tools/pruefen.sh`, `tests/…/Support/GameDataFact.cs` | – | M / gering | `tools/pruefen.sh` liefert dieselben Zahlen wie die bisherige Handprüfung; alle Regeln aus dem alten `CLAUDE.md` sind in `AGENTS.md` enthalten | [#45](https://github.com/kaaasi/eishockey-manager-redone/pull/45) |
| M2 | **CI-Qualität** (G06, G12 Teil 1) | Warnungen können nicht mehr unbemerkt entstehen; CI-Aussage eindeutig | `.github/workflows/build.yml` (Build-Schritt), `.github/scripts/testbericht.py` | – | S / gering | CI auf dem PR grün; Testbericht enthält den Hinweis; Build mit `-warnaserror` auf `main` und #39–#41 ohne Warnung (geprüft) | [#46](https://github.com/kaaasi/eishockey-manager-redone/pull/46) |
| M3 | **Artefakt-Speicher** (N1, G13 Teil 1) | Kontingent einhalten | `.github/workflows/build.yml` (Publish-Job) | Freigabe | S / mittel (Downloads ändern sich) | Publish nur bei `workflow_dispatch` und Tags `v*`, Aufbewahrung 7 Tage; danach alte Artefakte löschen | [#47](https://github.com/kaaasi/eishockey-manager-redone/pull/47) (Draft), Freigabe |
| M4 | **Branch-Bereinigung** (G08) | Übersicht, weniger Altlasten | Repo-Einstellungen | Freigabe | S / gering (nur gemergte Branches) | Liste der 37 Branches vor dem Löschen erneut mit `merge-base --is-ancestor` prüfen | Freigabe |
| M5 | **Pilot Übergabe** (G16.7) mit Paket **Markerbericht** (G05) | Nachweis, dass ein anderer Agent nur mit Repo-Informationen arbeiten kann | `tools/markerbericht.py`, CI-Zusammenfassung | M1, M2 | S–M / gering | siehe §5 | nach M1 |
| M6 | **Abnahme `speichern` schärfen** (G11) | robustes Speicherformat | `docs/arbeitspakete.md` (Plan) | Integration Runde 3 Welle A | S / keins | Schemaversion, stabile IDs, Roundtrip, Mehrsaison-Roundtrip als Abnahme | bei Paketzuschnitt |
| M7 | **Invarianten-Prüfer** (G04) | ein Prüfer für alle Mehrsaisontests | `tests/EishockeyManager.Core.Tests/Support/GameInvariants.cs` | Integration Runde 3 Welle A | M / gering | Prüfer schlägt bei absichtlich doppeltem Spieler, doppelter Nummer, verwaister Leihe und doppelter Buchung an | nach Runde 3 A |
| M8 | **Issues für Befunde** (G03) | konfliktfreie Befundliste | Labels, Issues | Freigabe | S / gering | Befunde des nächsten Reviews als Issues, Verweis in `arbeitspakete.md` | Freigabe |
| M9 | **Format** (G12 Teil 2) | einheitlicher Stil | `.editorconfig`, alle Quellen | Runde 3 abgeschlossen | M / mittel (Konflikte) | eine Formatierungs-PR ohne inhaltliche Änderung, danach `dotnet format --verify-no-changes` in CI | nach Runde 3 |
| M10 | **ADRs, Entkopplung** (G10, G13 Teil 2) | Entscheidungen nachvollziehbar | `docs/adr/` | `speichern` | M / gering | erste ADRs: Zeitleiste, Finanzbuch, zustandsreine Ansichten | später |

**Reihenfolge:** M1 → M2 → (Freigabe) M3, M4 → M5 Pilot → Runde-3-Integration mit M6/M8 → M7 → nach Runde 3 M9 → M10.
Keine Maßnahme ändert Dateien der offenen PRs #39–#41, #43, #44 (geprüft anhand ihrer Dateilisten).

## 4. Multi-Agenten-Konzept (Kurzfassung)

Ausgeführt in [zusammenarbeit.md](zusammenarbeit.md):

- **Gemeinsame Regeln:** `AGENTS.md` ist der werkzeugneutrale Einstieg; `CLAUDE.md` importiert ihn (`@AGENTS.md`) und
  enthält nur noch Claude-spezifische Bedienhinweise. Keine Regel steht doppelt.
- **Rollen:** Nutzer, Leitstelle (vom Nutzer ernannt, aktuell Claude Code), Implementierer, Reviewer. Niemand gibt die
  eigene Arbeit frei; ein Review in derselben Sitzung wie die Umsetzung zählt nicht.
- **Branch-Sperre:** Draft-PR mit `Bearbeiter:` sofort beim Start; ein Branch, ein Bearbeiter.
- **Übergabe:** Kommentar im PR nach `docs/handoffs/VORLAGE.md` mit Head-SHA, Testtabelle, Offenem und nächstem Schritt;
  Übernahme mit Gegenkommentar nach eigener Prüfung.
- **Einheitliche Prüfung:** `tools/pruefen.sh` für alle Agenten, Tabelle direkt in PR und Übergabe.
- **Vertretung der Leitstelle:** Ernennung durch den Nutzer, gleiche Merge-Prüfung, Review durch eine andere Sitzung;
  ohne zweiten Agenten entscheidet der Nutzer. Keine automatische Integration.
- **GitHub Free:** Regeln sind Prozess, nicht Technik – mit zwei echten Stützen: REST-Merge mit `sha` und Draft-PRs.

## 5. Pilot: Übergabe an einen anderen Agenten

**Paket:** Markerbericht (G05). Klein, ohne Bezug zu Runde 3, gut prüfbar.

- **Ziel:** `tools/markerbericht.py` zählt `[ORIGINAL]`- und `[APPROX]`-Marker je Datei und Projekt in `src/` und
  schreibt eine Markdown-Tabelle; die CI hängt sie an die Zusammenfassung an.
- **Branch:** `paket/markerbericht` von aktuellem `main` (nach M1 und M2). **Bearbeiter:** ChatGPT.
- **Dateigrenzen:** `tools/markerbericht.py` (neu), ein zusätzlicher Schritt am Ende des Jobs `build` in `build.yml`.
- **Abnahme:** Zahlen stimmen mit `grep -rho "\[APPROX\]\|\[ORIGINAL\]" src | sort | uniq -c` überein; Ausgabe
  deterministisch sortiert; keine neuen Abhängigkeiten; CI grün; PR nach Vorlage mit `tools/pruefen.sh`-Tabelle und Trailer.
- **Review:** Claude Code (andere Sitzung als jede Umsetzung) – Rollentausch gegenüber Runde 2.
- **Bewertung des Piloten:** (1) Musste der Agent Informationen außerhalb des Repos erfragen? Welche? (2) Wurden Draft-PR,
  `Bearbeiter:`, Trailer, Vorlage und `tools/pruefen.sh` ohne Hinweis genutzt? (3) Eine Übergabe mitten im Paket:
  Der Agent schreibt nach dem ersten Commit einen Übergabekommentar, eine **neue** Sitzung übernimmt und beendet das Paket.
  Erfolg = Abschluss ohne Rückfrage an den Nutzer zum Inhalt. Fehlendes wird in `AGENTS.md`/`zusammenarbeit.md` nachgetragen.

## 6. Entscheidungen des Nutzers (28.10.2026: alle fünf freigegeben)

| # | Entscheidung | Umsetzung |
|---|---|---|
| 1 | M3 Downloads nur auf Anforderung, alte Artefakte löschen | 39 Artefakte (ca. 1,43 GB) gelöscht; Workflow-Änderung in #52 (ersetzt Draft #47) |
| 2 | M4 gemergte Branches löschen, automatisches Löschen einschalten | aus Claudes Sitzung gesperrt (HTTP 403) → Workflow „Branches aufräumen“ in #52 (Start von Hand, mit Vorschau); „Automatically delete head branches“ schaltet der Nutzer unter Settings → General ein |
| 3 | M8 Befunde als Issues | Labels `befund`, `P0`–`P3` angelegt; Regel in `zusammenarbeit.md` §5 |
| 4 | Audit als Dokument mergen | #53 (ersetzt Draft #42) gemergt |
| 5 | M5 Pilot | Paket in `docs/arbeitspakete.md` („Pilot: Markerbericht“), Start nach Merge von #45 und #50 |

## 7. Verworfene Empfehlungen und Alternativen

| ID | Grund | Alternative |
|---|---|---|
| G07 | kein nachweisbarer Schaden, Trennung verschlechtert die Downloads | Speicherproblem lösen (M3) |
| G09 | dritte Statusquelle, ein Konto, Endpunkte nicht für alle Agenten erreichbar | `arbeitspakete.md` + Draft-PRs; ggf. Issues (M8) |
| G14 | ohne Branch-Schutz wirkungslos | Reviewer-Regel in `zusammenarbeit.md` |
| G15 | widerspricht der Commit-Regel, kein Changelog-Bedarf | Agent-Trailer |
