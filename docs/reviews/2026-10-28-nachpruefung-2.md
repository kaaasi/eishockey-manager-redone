# Zweite Nachprüfung: PROZ-02

## 1. Merge-Empfehlung

**Ja für #50 am Head `ad26a99dc3388347f089bbce267c4af4215bde83`, ausschließlich zusammen mit #45. PROZ-02 ist behoben. #45+#50, #46 und #52 sind mergebereit.** Diese Aussage gilt für die folgenden unveränderten bzw. nachgeprüften Heads und den Umfang des Prozessreviews; #54 wird hier nicht freigegeben.

| PR | freigegebener Head |
|---|---|
| #45, nur zusammen mit #50 | `6a6f227cca1ad5f1aab2122ac486d055a3e27a87` |
| #50 | `ad26a99dc3388347f089bbce267c4af4215bde83` |
| #46 | `237c3f66dda95e5d21413d86dbea76743874a2d1` |
| #52 | `a45d0690f44e830caf1b485171f8f307f123cd0e` |

Nachprüfung von [2026-10-28-nachpruefung.md](2026-10-28-nachpruefung.md), nur Delta `73b299c..ad26a99` und PROZ-02. Keine Mitarbeit an der Implementierung. Die Freigaben zu PROZ-01/03 und der unveränderte Befundstand zu #46 werden aus dem vorigen Review übernommen. Keine Freigabe allein anhand von Testzahlen.

## 2. Befunde

**PROZ-02 · bisher P2 · behoben.** [pruefen.sh:21–28](https://github.com/kaaasi/eishockey-manager-redone/blob/ad26a99dc3388347f089bbce267c4af4215bde83/tools/pruefen.sh#L21-L28) verlangt für jedes der vier Testprojekte eine Konsolenzusammenfassung. [Zeilen 71–99](https://github.com/kaaasi/eishockey-manager-redone/blob/ad26a99dc3388347f089bbce267c4af4215bde83/tools/pruefen.sh#L71-L99) verlangen zusätzlich vier TRX-Dateien und lehnen `ResultSummary` mit `Aborted`/`Error` sowie Abbruch-/Absturzmeldungen in `RunInfo` ab. Erst danach werden ausschließlich erwartete Originaldatenfehler akzeptiert.

Die ursprüngliche Gegenprobe wurde unverändert erneut ausgeführt: ein erwarteter Originaldatenfehler in `core.trx` plus abgebrochenes UI-Projekt in `ui.trx`, Prozess-Exit 1. Alter Head `73b299c`: Prüfsystem **Exit 0**, „bestanden“. Neuer Head `ad26a99`: **Exit 1**, „NICHT bestanden“; Konsolen- und TRX-Anzahl sind unvollständig. Für die neue Anzahlprüfung wurden ausschließlich vier leere `.csproj`-Pfade in der temporären Fixture ergänzt; das Double und seine Ergebnisse blieben unverändert.

Zusätzliche isolierte Gegenproben schließen aus, dass nur die Anzahlprüfung die Reparatur vortäuscht: Vier Projektpfade, vier Konsolenzeilen und vier TRX-Dateien, alle Testfehler melden „Originaldaten fehlen“, aber die letzte TRX hat `ResultSummary outcome="Aborted"`. Alter Head: **Exit 0**; neuer Head: **Exit 1**. Gleiches Ergebnis bei normaler Summary und zusätzlichem `RunInfo` mit „Test host process crashed“. Mit vier regulären Ergebnissen ausschließlich wegen fehlender Originaldaten bleibt die Positivkontrolle **Exit 0**. Fehlende einzelne TRX bzw. Konsolenzeile werden jetzt ebenfalls abgelehnt.

Keine neuen P0–P3-Befunde im geprüften Delta. Das Anzahlschema passt zum aktuellen Repository: vier Testprojekte in `tests/*/*.csproj`, jeweils ein Targetframework. Diese Prüfung behauptet keine allgemein verwendbare Lösung für beliebige mehrzielige Solutions.

## 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

Review-Vorlage, AGENTS und Zusammenarbeit §§5/6 aus `prozess/sparsam` gelten unverändert gegenüber der vorigen Nachprüfung. Lokaler Fetch und GitHub-Metadaten bestätigen den vollständigen neuen Head. Delta: ausschließlich `tools/pruefen.sh` und `tools/pruefen-selbsttest.sh`, kein Produktionscode, keine Anwendungstests und keine Datenhelfer geändert. Blob-Hashes: Prüfsystem `99b21bda86b8b504a278ed1e4eb9048877e5d474`, Selbsttest `e308cc809a2bbae5642fbe47e56cd4c8070d68c2`.

```bash
git diff 73b299c..ad26a99 --stat
git diff 73b299c..ad26a99 -- tools
bash -n tools/pruefen.sh tools/pruefen-selbsttest.sh
bash tools/pruefen-selbsttest.sh
python3 /tmp/nachpruefung-probes.py
python3 /tmp/nachpruefung2-originalprobe.py
python3 /tmp/nachpruefung2-isoliert.py
```

**Selbsttest auf dem neuen Head: Exit 0, alle sechs Fälle korrekt.** Positivkontrolle bestanden; grüner Pflichtlauf, vollständiger Absturz, fehlende Projektzusammenfassung, abgebrochene TRX und fremder Testfehler jeweils abgelehnt. Gegenprobe des neuen Selbsttests gegen das alte Prüfsystem in temporärer Kopie: **Exit 1**, Teilabsturz ohne Projektzusammenfassung wird fälschlich akzeptiert. Die zusätzlichen eigenen isolierten Fixtures werden ebenfalls am alten Head grün und am neuen rot; es handelt sich um Auswertungsproben, keinen behaupteten tatsächlichen Absturz der Anwendungstests.

| Eigene isolierte Fixture | `73b299c` | `ad26a99` |
|---|---:|---:|
| Vollständige erwartete Originaldatenfehler | Exit 0 | Exit 0 |
| Alle Ergebnisse vorhanden, eine Summary `Aborted` | Exit 0 | Exit 1 |
| Alle Ergebnisse vorhanden, eine `RunInfo` mit Absturz | Exit 0 | Exit 1 |
| Eine TRX fehlt | Exit 0 | Exit 1 |
| Eine Konsolenzusammenfassung fehlt | Exit 0 | Exit 1 |

**Gemeinsamer Stand:** Konfliktfreie Merge-Probe von #50 (enthält #45) mit #46, #52 und aktuellem `main=49bc2cee8a2c5831c007ff17af4f34529cc52922`. Temporärer gemeinsamer Commit `1f92a49c7339290995d9e21e17ad824c6e4a0196`. Seit dem letzten Review enthält main ausschließlich die drei Review-Dateien, keinen geänderten Anwendungscode. GitHub-Check `build` auf jedem der vier freigegebenen Heads erfolgreich; `publish` jeweils erwartungsgemäß übersprungen, insbesondere [#50-Run 40689766987](https://github.com/kaaasi/eishockey-manager-redone/actions/runs/40689766987).

**Eigene Anwendungsläufe:** SDK 10.0.402, jeweils unverändertes `tools/pruefen.sh`, Release-Build mit `-warnaserror`. Aufruf mit den bereits eingerichteten Arbeitsverzeichnissen:

```bash
XDG_DATA_HOME=/workspace/xdg PATH=/workspace/sdk:$PATH \
DOTNET_CLI_HOME=/workspace/dotnet-home NUGET_PACKAGES=/workspace/nuget \
DOTNET_CLI_TELEMETRY_OPTOUT=1 bash tools/pruefen.sh
```

| Stand | Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---|---:|---:|---:|
| exakt `ad26a99` | Mit Originaldaten | – | – | keine Daten vorhanden |
| exakt `ad26a99` | Ohne Originaldaten | ausstehend | – | – |
| exakt `ad26a99` | Pflichtlauf ohne Daten | ausstehend | – | – |
| gemeinsam `1f92a49` | Mit Originaldaten | – | – | keine Daten vorhanden |
| gemeinsam `1f92a49` | Ohne Originaldaten | ausstehend | – | – |
| gemeinsam `1f92a49` | Pflichtlauf ohne Daten | ausstehend | – | – |

**Akzeptierte [APPROX]:** keine neuen fachlichen Näherungen in diesem Delta. **Grenzen/Annahmen:** Kein eigener positiver Originaldatenlauf mangels Originaldateien. Die vorigen externen Originaldaten-Nachweise der unveränderten Anwendung werden nicht als neuer eigener Lauf ausgegeben; die positive Auswertung des geänderten Prüfsystems ist zusätzlich durch den Selbsttest kontrolliert. Die fachliche Prozessfreigabe entbindet die Leitstelle nicht von der SHA-Bindung beim Merge und der Prüfung des jeweils aktuellen Integrationsstands.
