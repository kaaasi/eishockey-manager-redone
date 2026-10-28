# Nachprüfung zu #59 und #60 vom 28.10.2026

## 1. Merge-Empfehlung

**Noch nein für das gemeinsame Prozesspaket: PROZ-02 bleibt teilweise offen (P2).** PROZ-01 und PROZ-03 sind behoben. #45 wird ausschließlich zusammen mit #50 beurteilt. Frische ChatGPT/Codex-Sitzung ohne Mitarbeit an den Implementierungen; geprüft wurden die angefragten Deltas und Befunde, keine erneute Vollprüfung aller acht Integrations-PRs.

| PR | genau geprüfter Head | Ergebnis der Nachprüfung |
|---|---|---|
| #45 + #50 | `6a6f227cca1ad5f1aab2122ac486d055a3e27a87` + `73b299c753092dec033ae8013f7b53648bb5fe63` | Nein: Teilabsturz im Pflichtlauf weiterhin als Erfolg akzeptiert; PROZ-03 behoben. #45 ist Vorfahr von #50. |
| #46 | `237c3f66dda95e5d21413d86dbea76743874a2d1` | Unverändert seit #59; kein zusätzlicher Sachbefund. |
| #52 | `a45d0690f44e830caf1b485171f8f307f123cd0e` | PROZ-01 behoben; fachlich freigabefähig hinsichtlich dieses Befunds. |
| #54 | `c622b0fe06b0000b74fe1f1f6ce91dbf365c068a` | Noch keine uneingeschränkte Mergefreigabe: R3A-01 bleibt an diesem Head offen. Die beiden offenen P3-Befunde blockieren für sich allein nicht. |

**Originaldaten-Nachweis der Leitstelle:** Der [Kommentar in #54](https://github.com/kaaasi/eishockey-manager-redone/pull/54#issuecomment-6468067237) benennt den vollständigen, unveränderten Head, Release-Build mit `-warnaserror` (0 Warnungen/Fehler) und `EM_REQUIRE_GAMEDATA=1` mit 470/0/0. Das ist als **externer, dem Head zugeordneter Originaldaten-Nachweis** verwertbar; die Prozessregel verlangt die Tabelle in PR oder Review, nicht zwingend einen zweiten Originaldatenlauf durch denselben Reviewer. Der bisher fehlende positive Lauf aus #60 ist damit durch eine ausdrücklich zugeordnete Fremdprüfung ergänzt. Rohprotokoll/TRX liegen dem Kommentar nicht bei; die Zahlen wurden hier nicht unabhängig nachgezählt und sind kein Originaltreuebeweis.

Der Kommentar kündigt die beiden weiteren Läufe nach Merge von #45/#50 und Rebase von #54 ausdrücklich erst an. Er schließt **R3A-01 nicht**: Der Datenhelfer an `c622b0f` ignoriert weiterhin `EM_SKIP_GAMEDATA`. Die früheren Ohne-Daten-Läufe aus #60 liefen mangels Daten tatsächlich ohne Daten und bleiben gültig, ersetzen aber keinen Nachweis für einen künftig veränderten Integrationshead. Vor regulärer Freigabe #45/#50 einschließlich des noch nötigen PROZ-02-Abschlusses übernehmen, neuen Head nachprüfen und alle drei Läufe dort dokumentieren. Alternativ ist am unveränderten Head ein nachgewiesener tatsächlicher Datenentzug für die beiden Negativläufe möglich; der neue Kommentar liefert diesen Nachweis nicht.

**#61/#62:** Beide Issues sind offen und mit `befund` sowie `P3` („nicht blockierend“) erfasst. Nach `zusammenarbeit.md` §6.5 müssen offene P0/P1/P2 behoben oder vom Nutzer zurückgestellt sein; offene P3 verlangen keine solche Ausnahme. Deshalb: **Ja, #54 kann grundsätzlich mit diesen beiden offenen P3-Befunden freigegeben werden; nein, die gesamte Merge-Prüfliste für den angefragten Head ist damit noch nicht erfüllt.** Voraussetzung für die spätere Freigabe bleibt insbesondere der Abschluss von R3A-01. Der Bundestrainer-Modus ist weiterhin nur im Szenario, nicht im normalen Menü erreichbar; die P3-Einstufung aus #60 wird dadurch nicht verschärft.

## 2. Befunde

**PROZ-01 · bisher P1 · behoben in `a45d069`.** [Workflow:31–54](https://github.com/kaaasi/eishockey-manager-redone/blob/a45d0690f44e830caf1b485171f8f307f123cd0e/.github/workflows/aufraeumen.yml#L31-L54): SHA vor der Vorfahrenprüfung festhalten, offene PRs je Branch unmittelbar vor der Aktion abfragen, Lösch-Refspec mit explizitem `--force-with-lease=refs/heads/<branch>:<sha>`. Die Gegenprobe aus #59 löscht am alten Head B und bewahrt am neuen Head exakt B. Ein unveränderter gemergter Branch wird weiterhin gelöscht; offener PR, Vorschau und fehlgeschlagene API-Abfrage bewahren ihn. Das verbleibende Rennen „PR öffnet nach Abfrage, Branch unverändert“ ist im Workflow ausdrücklich dokumentiert: Es betrifft dann bereits in main enthaltenen Inhalt, nicht die zuvor nachgewiesene Löschung neuer ungemergter Commits. Keine echte GitHub-Ref gelöscht.

**PROZ-02 · P2 · teilweise behoben, weiterhin offen: Teilabsturz eines Testprojekts im Pflichtlauf wird übersehen.** [pruefen.sh:70–85](https://github.com/kaaasi/eishockey-manager-redone/blob/73b299c753092dec033ae8013f7b53648bb5fe63/tools/pruefen.sh#L70-L85) zählt ausschließlich `UnitTestResult outcome="Failed"`. Ein zusätzliches abgebrochenes Projekt ohne solche Ergebniszeilen wird nicht geprüft; `ResultSummary outcome="Aborted"` und `RunInfo outcome="Error"` werden ignoriert. Die Konsolenprüfung verlangt nur irgendeine Zusammenfassung, nicht die vollständigen erwarteten Projekte.

Reproduziert mit unverändertem Skript in einer temporären Kopie und `dotnet`-Double wie in #59: normaler Lauf liefert 12/0/4 und Exit 0. Pflichtlauf liefert 12/1/0, Exit 1, eine fehlgeschlagene Ergebniszeile „Originaldaten fehlen“ in `core.trx` sowie einen Testhost-Absturz in `ui.trx` und auf der Konsole. Ergebnis des Skripts: **Exit 0, „Ergebnis: bestanden“, Pflichtlauf „scheitert wie gewollt“**. Dies ist eine kontrollierte Ergebnis-Fixture, kein behaupteter realer Absturz der Anwendungstests.

Minimales Double für diese Gegenprobe (vorne in `PATH`; Skriptkopie ohne `gamedata/kb`):

```python
#!/usr/bin/env python3
import os, pathlib, sys
if sys.argv[1] in ('build', '--version'):
    print('double'); sys.exit(0)
if os.getenv('EM_REQUIRE_GAMEDATA') != '1':
    print('Passed!  - Failed: 0, Passed: 12, Skipped: 4, Total: 16')
    sys.exit(0)
p = pathlib.Path(sys.argv[sys.argv.index('--results-directory') + 1])
p.mkdir(parents=True)
ns = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'
(p / 'core.trx').write_text(f'''<TestRun xmlns="{ns}"><Results>
<UnitTestResult outcome="Failed"><Output><ErrorInfo>
<Message>Originaldaten fehlen</Message></ErrorInfo></Output></UnitTestResult>
</Results></TestRun>''')
(p / 'ui.trx').write_text(f'''<TestRun xmlns="{ns}">
<ResultSummary outcome="Aborted"><RunInfos><RunInfo outcome="Error">
<Text>Test host process crashed</Text></RunInfo></RunInfos></ResultSummary>
</TestRun>''')
print('Failed!  - Failed: 1, Passed: 12, Skipped: 0, Total: 13')
print('The active test run was aborted. Reason: Test host process crashed')
sys.exit(1)
```

Abhilfe: vollständige strukturierte Ergebnisse für jedes erwartete Testprojekt verlangen und Abbrüche/Lauf-Fehler ausdrücklich ablehnen; danach zusätzlich sicherstellen, dass alle erwarteten Testfehler ausschließlich fehlende Originaldaten melden. Regression gegen den gemischten Fall ergänzen. Der neue Selbsttest deckt nur vollständigen Absturz und unerwartet grünen Pflichtlauf ab; beide sind jetzt korrekt rot. Auch leere normale Ausgabe und ein fremder Testfehler werden nun korrekt abgelehnt. Die ursprünglichen Gegenbeispiele sind damit repariert, die weitergehende Aussage „Absturz gilt als NICHT bestanden“ noch nicht.

**PROZ-03 · bisher P2 · im gemeinsamen Stand behoben.** [pruefen.sh:55–60](https://github.com/kaaasi/eishockey-manager-redone/blob/73b299c753092dec033ae8013f7b53648bb5fe63/tools/pruefen.sh#L55-L60): `--schnell` ohne Daten führt genau einen Testaufruf mit `EM_SKIP_GAMEDATA=1` aus. Das Double protokolliert den Aufruf; damit ist ein bloßer erfolgreicher Build als Nachweis ausgeschlossen. Keine Einzelfreigabe für #45.

**R3A-01 · P2 · offen am unveränderten Head.** [Datenhelfer:22–25](https://github.com/kaaasi/eishockey-manager-redone/blob/c622b0fe06b0000b74fe1f1f6ce91dbf365c068a/tests/EishockeyManager.UI.Tests/Support/GameDataFact.cs#L22-L25). Kein Code-Delta seit #60, daher keine Behebung durch den neuen Kommentar. Gegenbeispiel und Abhilfe aus #60 gelten weiter.

**R3A-02/03 · P3 · erwartet offen, erfasst als [#61](https://github.com/kaaasi/eishockey-manager-redone/issues/61)/[#62](https://github.com/kaaasi/eishockey-manager-redone/issues/62).** Produktionsabhängiger Sollwert weiterhin in [DebCupTests:126](https://github.com/kaaasi/eishockey-manager-redone/blob/c622b0fe06b0000b74fe1f1f6ce91dbf365c068a/tests/EishockeyManager.Core.Tests/DebCupTests.cs#L126); ungeschützte erneute Ernennung mit Etatbuchung weiterhin in [CupsNational.GameState:58–66](https://github.com/kaaasi/eishockey-manager-redone/blob/c622b0fe06b0000b74fe1f1f6ce91dbf365c068a/src/EishockeyManager.Core/CupsNational.GameState.cs#L58-L66). Keine erneute Mutation/Spielprobe behauptet, da Head unverändert und hier nur Bewertung des Nachweises beauftragt.

## 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

`AGENTS.md`, Review-Vorlage und Zusammenarbeit §§5/6 vom Branch `prozess/sparsam` sowie beide Review-Dateien an ihren Review-Heads gelesen. GitHub-Metadaten und lokaler Clone bestätigen alle vollständigen Heads. Delta #50: `d01618a..73b299c`, drei Dateien (AGENTS, Prüfsystem, Selbsttest); Delta #52: `4af046a..a45d069`, nur Aufräumworkflow. #46 und #54 unverändert. Blob des Prüfsystems `1f8f42dc1a0117a418d6eb222b116c107ca25b97`, des Selbsttests `9f1d05f3e3967ae531e3bfa7e582d67060a7433e`.

Ausgeführte Befehle in isolierten Worktrees/Kopien:

```bash
git merge-base --is-ancestor 6a6f227 73b299c
git diff d01618a..73b299c -- tools AGENTS.md
git diff 4af046a..a45d069 -- .github
bash tools/pruefen-selbsttest.sh
python3 /tmp/nachpruefung-probes.py
python3 /tmp/nachpruefung-cleanup.py
```

| Kontrollierte Prüfsystem-Probe | Exit des Prüfsystems | Bewertung |
|---|---:|---|
| Vollständiger Pflichtlauf-Absturz, keine Ergebnisse | 1 | korrekt abgelehnt |
| Leere normale Ausgabe | 1 | korrekt abgelehnt |
| Grüner Pflichtlauf | 1 | korrekt abgelehnt |
| Pflichtlauf mit fremdem Fehler in TRX | 1 | korrekt abgelehnt |
| Pflichtlauf nur mit „Originaldaten fehlen“ | 0 | Positivkontrolle korrekt |
| Originaldatenfehler plus weiteres abgebrochenes Testprojekt | 0 | weiterhin fehlerhaft, PROZ-02 |
| Schnelllauf ohne Daten | 0 | genau ein synthetischer Testaufruf |

Löschprobe: lokales Bare-Remote, zwei Klone, A auf main und feature, nach Checkout B nur auf Remote-feature pushen, unveränderten Shell-Block des jeweiligen Workflows ausführen (Repository-Platzhalter lokal ersetzt; `gh` kontrolliert). Alter Head löscht B, neuer Head erhält B durch Lease-Ablehnung. Neue Kontrollfälle: gemergter unveränderter Branch gelöscht; offener PR, API-Fehler und Vorschau jeweils erhalten. Diese Proben brauchen kein .NET-SDK.

Gemeinsame Merge-Probe gegen `main=ee7ea0ed58570312e564a3568cb1fff5bcb9d712`: von #50 (enthält #45) aus #46/#52/main gemergt, danach #54; alle ohne Konflikte. Gemeinsamer temporärer Commit `175b27753058562de8eae09fac68bc8e811ede7f`; kein veröffentlichter Implementierungshead. Das ersetzt keinen Test auf einem künftigen Rebase-Head. CI von #54: Check `build` auf exakt `c622b0f` erfolgreich, `publish` übersprungen, [Run 40668103674](https://github.com/kaaasi/eishockey-manager-redone/actions/runs/40668103674).

**Eigene Anwendungsläufe dieser Nachprüfung:** SDK 10.0.402. Der erste Aufruf scheiterte vor den Tests an `NuGet-Migrations` (schreibgeschütztes Benutzerverzeichnis); mit separaten CLI-/Paket-/XDG-Verzeichnissen und vorbereitetem NuGet-Migrationsmarker liefen die Prüfungen. Befehlspräfix: `XDG_DATA_HOME=/workspace/xdg PATH=/workspace/sdk:$PATH DOTNET_CLI_HOME=/workspace/dotnet-home NUGET_PACKAGES=/workspace/nuget DOTNET_CLI_TELEMETRY_OPTOUT=1 bash tools/pruefen.sh`. Keine zusätzlichen Build-/Testfilter, kein dotnet-Double in diesen Anwendungsläufen. Release-Builds mit Warnungen als Fehler erfolgreich. Originaldaten sind nicht vorhanden. Vorherige Review-Zahlen und Leitstellenzahlen werden nicht als eigene neue Läufe ausgegeben.

| Stand | mit Daten | ohne Daten | Pflichtlauf ohne Daten |
|---|---|---|---|
| #45/#50, exakt `73b299c` | nicht ausgeführt, Daten fehlen | 297/0/47 | 297/54/0 |
| gemeinsame Prüfkopie einschließlich #54, `175b277` | nicht ausgeführt, Daten fehlen | 392/0/66 | 392/78/0 |
| #54, exakt `c622b0f` | nicht ausgeführt, Daten fehlen | 392/0/66 | 392/78/0 |
| #54, externer Nachweis Leitstelle zum Head | 470/0/0 | im Kommentar angekündigt | im Kommentar angekündigt |

Zahlenfolge: bestanden/fehlgeschlagen/übersprungen; alle drei Prüfsystem-Aufrufe Exit 0. Pflichtfehler wurden durch die TRX-Auswertung als fehlende Originaldaten bestätigt. Das Skript aus #50 wurde auf #54 ausschließlich als ungetracktes Prüfwerkzeug bereitgestellt; Produktionscode und Datenhelfer blieben unverändert. Ohne tatsächlich vorhandene Originaldaten kann dieser Lauf R3A-01 weder auslösen noch schließen.

**Akzeptierte [APPROX]:** keine neuen Näherungen im Prozessdelta. Die in #60 ausdrücklich akzeptierten Näherungen und fachlichen Grenzen bleiben unverändert, einschließlich fehlender Endabnahme der Finanzbalance. Keine neue Originaltreue- oder Pixelabnahme. **Annahmen/Grenzen:** Externer Originaldatenlauf wird als Aussage der benannten Leitstelle gewertet; keine Rohdaten/Nachzählung verfügbar. Die isolierten TRX-Fixtures prüfen die Auswertung, nicht das Verhalten von xUnit bei einem hier tatsächlich aufgetretenen Absturz.
