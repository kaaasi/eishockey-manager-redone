# Zweite Nachprüfung PR #127: Referenzvideos, Teil 2

## 1. Merge-Empfehlung

**Nein** für Head `da98be25fbad7eafb6dc2257ac39bb2af18b4838` (Base `27fa1c4da0f1f76603b3c62118804f5e3c663036`). Der Schutz gegen Datenverlust ist noch umgehbar: Ein Reset kann unversionierte Dateien überschreiben; auch eine fremde origin mit passendem Pfadsuffix wird akzeptiert. Geprüft wurden nur das Delta `908b27e..da98be2` und die genannten Befunde.

## 2. Befunde

- **RV2N2-01 · P1 – Unversionierte Dateien werden vor dem Reset nicht geschützt.** [fetch-reference.sh, Zeilen 37–42](https://github.com/kaaasi/eishockey-manager-redone/blob/da98be25fbad7eafb6dc2257ac39bb2af18b4838/tools/claude/fetch-reference.sh#L37-L42): `status --untracked-files=no` übersieht unversionierte und ignorierte Dateien. Wenn der geholte Commit einen gleichnamigen versionierten Pfad enthält, überschreibt `reset --hard` diese Dateien. Reproduktion mit lokalem bare Remote, dessen Pfad auf `kaaasi/eishockey-manager-referenz` endet: alten Klon erstellen, dort unversioniertes `collision` mit `local precious` anlegen, im Remote auf `main` `collision` mit `remote` committen und das Skript mit `REFERENZ=<klon>` ausführen. Danach enthält `collision` `remote`; die lokale Datei ist verloren. Auch ein Symlink auf den Klon wird als Ziel akzeptiert. Der spätere Fehler `no sparse-checkout to add to` im nicht-sparsen Testklon kommt erst nach dem Datenverlust.
- **RV2N2-02 · P1 – Origin-Prüfung akzeptiert fremde Repositories.** [fetch-reference.sh, Zeilen 32–36](https://github.com/kaaasi/eishockey-manager-redone/blob/da98be25fbad7eafb6dc2257ac39bb2af18b4838/tools/claude/fetch-reference.sh#L32-L36): Der reguläre Ausdruck prüft nur das Suffix, weder Host noch URL-Grenze. Ein lokales fremdes Remote `/tmp/rv2-review/kaaasi/eishockey-manager-referenz` bestand die Prüfung; ein sauberer Klon wurde auf dessen neues `main` zurückgesetzt. Ebenso würde `https://example.invalid/kaaasi/eishockey-manager-referenz.git` die Prüfung passieren; hier stoppte dessen Fetch am Proxy. Ein sauberer fremder Arbeitsbranch kann dadurch auf einen anderen Commit verschoben und sein Inhalt ersetzt werden. Verifizierbare Herkunft und Schutz des bestehenden Zustands sind vor dem Reset nötig.

**RV2N-01 (#144):** Teilweise behoben: Fremdes Repo mit abweichender origin und Änderung sowie `REFERENZ=.` im verschmutzten Code-Klon brechen mit 2 ab und erhalten die Änderung. Die obigen Fälle zeigen, dass der Schutz noch nicht vollständig ist. Unterverzeichnis ohne eigenes `.git`: Klonversuch schlägt bei nichtleerem Ziel bzw. gesperrtem Fetch fehl, kein Reset des Eltern-Repos. Erwartet offen: `CLAUDE.md` nennt die neuen Ladeoptionen weiterhin nicht.

## 3. Ausgeführte Prüfungen und akzeptierte `[APPROX]`

- GitHub-Compare und Dateien mit festen SHAs gelesen; Delta umfasst nur `tools/claude/fetch-reference.sh`. Das separat ausgeführte Head-Skript hat Git-Blob-SHA `5fd3131c9f3d92f44354eccc472572e362e8309f`, identisch zum Connector. Kein vollständiger Checkout des Heads: `git fetch origin main pull/127/head` scheiterte am nicht erreichbaren Proxy. Der frühere Bericht wurde aus PR #142 gelesen.
- Isolierte Git-Gegenproben mit `REFERENZ=… bash /tmp/rv2-review/fetch.sh speedrun_t1`: fremdes Repo mit geänderter versionierter Datei und abweichender origin → 2, Inhalt erhalten; verschmutzter lokaler Code-Klon mit `REFERENZ=.` → 2, Datei-Hash unverändert. Sauberer alter synthetischer Sparse-Klon mit lokalem Remote → 0, neuer Commit und Testdatei geladen. Kein Zugriff auf den echten privaten Referenz-Klon; dessen Aktualisierung bleibt unabhängig unbestätigt. Zusätzliche Unterverzeichnis-, Symlink-, Suffix- und Kollisionsproben wie oben; keine produktiven Daten verwendet.
- SDK `/workspace/.dotnet/dotnet --version`: `10.0.402`. Build und `tools/pruefen.sh` auf dem exakten Head nicht ausführbar, da die benötigten Git-Objekte lokal fehlen und Fetch scheitert; keine Testzahlen behauptet.

| Lauf `tools/pruefen.sh` | Ergebnis |
|---|---|
| Mit Originaldaten | blockiert: Head-Checkout fehlt |
| Ohne Originaldaten | blockiert: Head-Checkout fehlt |
| Pflichtlauf ohne Daten | blockiert: Head-Checkout fehlt; kein erwartetes Scheitern nachgewiesen |

- Akzeptierte neue `[APPROX]`: keine. Annahmen: Die Datenverlust-Gegenbeispiele setzen einen erfolgreichen Fetch und einen kollidierenden Pfad beziehungsweise ein fremdes `main` voraus. Netzwerk und echte Referenzdaten wurden nicht simuliert.
