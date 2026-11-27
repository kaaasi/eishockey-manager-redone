# Nachprüfung PR #127: Referenzvideos, Teil 2

## Merge-Empfehlung

**Nein** für Head `908b27e94cbff5bb431422c7704d7183ff2fdbe9` (Base `27fa1c4da0f1f76603b3c62118804f5e3c663036`). Das Delta behebt die falsche Bildbeschreibung; der neue harte Reset kann jedoch ein anderes Repository einschließlich des Code-Repos beschädigen. Der tatsächliche Download aus dem privaten Referenz-Repo konnte hier nicht wiederholt werden.

## Befunde

- **RV2N-01 · P1 – Zielpfad wird vor hartem Reset nicht als Referenz-Klon geprüft.** [`tools/claude/fetch-reference.sh`, Zeilen 28–35](https://github.com/kaaasi/eishockey-manager-redone/blob/908b27e94cbff5bb431422c7704d7183ff2fdbe9/tools/claude/fetch-reference.sh#L28-L35): Ein beliebiges Verzeichnis mit `.git` wird als Referenz-Klon behandelt; `origin main` wird geholt und `reset --hard FETCH_HEAD` verwirft dort Änderungen an versionierten Dateien und verschiebt den ausgecheckten Branch auf den geholten Stand. Gegenbeispiel: `REFERENZ=.` im Code-Repo ausführen, nachdem dort eine versionierte Datei geändert wurde; bei erfolgreichem Fetch ist die Änderung weg, und ein abweichender Branch wird zurückgesetzt. Das wurde sicher in einem separaten synthetischen Git-Klon nachgestellt: `tracked.txt` mit Inhalt `unsaved work` wurde auf `old` zurückgesetzt. Auch andere fremde Git-Repositories sind betroffen. Vor dem Reset müssen Ziel und Herkunft sicher geprüft werden; das Code-Repo darf nicht als Ziel akzeptiert werden.

## Ausgeführte Prüfungen und akzeptierte `[APPROX]`

- Nur Delta `f720e10aaa18ec88b2511c9259dde30292cbb4cf..908b27e94cbff5bb431422c7704d7183ff2fdbe9` und Befunde aus [Review #137](https://github.com/kaaasi/eishockey-manager-redone/pull/137) geprüft. Dateien und Diff über GitHub-Connector mit festem SHA gelesen; kein vollständiger Checkout des Heads.
- **RV2-01:** Das Head-Skript in `/tmp` mit lokalem bare Remote und einem alten Sparse-Klon ausgeführt: `REFERENZ=<alter-klon> bash /tmp/review-fetch-908.sh speedrun_t1` aktualisierte den Klon und stellte die synthetische Datei bereit. `does_not_exist` gab 1 zurück. Gegenprobe RV2N-01 wie oben. Dies belegt die Git-Abläufe mit Testdaten, nicht den Download der echten Bilder: direkter Git-Zugriff war in dieser Umgebung zuvor durch die Socket-/Proxy-Sperre blockiert. RV2-01 ist damit für den realen Referenz-Klon nicht vollständig unabhängig bestätigt.
- **RV2-02:** Vier Bilder im privaten Referenz-Repo bei `2bfe462` visuell geprüft: `folge13_02545` zeigt Ergebnisse beider Termine; `duell_s2a_01217`, `duell_s6a_03100` und `eisbaeren_f60_02758` zeigen die angegebenen Paarungen ohne Ergebnisse. Die korrigierten Beschreibungen stimmen; RV2-02 ist behoben.
- `CLAUDE.md` nennt die neuen Ladeoptionen weiterhin nicht, wie erwartet offen.
- [CI Build #228](https://github.com/kaaasi/eishockey-manager-redone/actions/runs/41015000154) auf dem genannten Head erfolgreich, einschließlich synthetischem Test; Publish übersprungen. CI hat keine Originaldaten. Weitere Build- und Testläufe entfallen gemäß ursprünglichem Auftrag für Doku und Ladeskript; keine lokale Prüftabelle.
- Akzeptierte neue `[APPROX]`: keine. Annahmen: Der Git-Fetch des Ziel-Repos ist erfolgreich und `origin` besitzt `main`; bei Fehlschlag stoppt das Skript vor dem Reset.
