# Vierte Nachprüfung PR #127: Referenzvideos, Teil 2

## 1. Merge-Empfehlung

**Nein, noch nicht** für Head `d773aed61eb468dbb9b38a9b96805f96ab44d8aa` (Base `27fa1c4da0f1f76603b3c62118804f5e3c663036`): Die gezielten Gegenbeispiele zeigen keinen verbleibenden Datenverlust, aber Build und vorgeschriebene vollständige Prüfung auf dem exakten Head sind blockiert. Geprüft wurden nur das Delta `73a57bf..d773aed` und die genannten Befunde.

## 2. Befunde

Keine neuen Befunde mit konkretem Gegenbeispiel. **RV2N3-01 (#151):** Ein veralteter Klon mit `status.showUntrackedFiles=no` und ignorierter Kollisionsdatei bricht mit 3 ab; Inhalt bleibt erhalten. **RV2N2-01 (#147):** Auch bei normaler Konfiguration bricht der veraltete Klon mit 3 ab und erhält die Kollisionsdatei. **RV2N2-02 (#148):** Lokale fremde origin und `https://example.invalid/kaaasi/eishockey-manager-referenz.git` werden mit 2 abgewiesen.

Vor `sparse-checkout add` führt der Pfad für einen vorhandenen Klon Herkunftsprüfung, Fetch und Standvergleich aus; in den Gegenproben wurde keine Datei außerhalb von `.git` verändert. Ein erfolgreicher Lauf von `sparse-checkout add` selbst ist damit nicht geprüft. Erwartet offen bestätigt: `CLAUDE.md` nennt die neuen Ladeoptionen weiterhin nicht.

## 3. Ausgeführte Prüfungen und akzeptierte `[APPROX]`

- GitHub-Compare und Dateien mit festen Refs: ein Commit, nur `tools/claude/fetch-reference.sh` geändert. Die separat ausgeführte Skriptkopie hat Blob-SHA `f6652be95e47be9cfa45ba3d680834b6a9ddf88b`, identisch zum Connector. Vorheriges Review aus PR #150 gelesen. Kein vollständiger Checkout: `git fetch origin main pull/127/head` scheiterte am nicht erreichbaren Proxy.
- Synthetische Git-Repositories mit altem Klon und neuerem Remote-Commit mit Kollisionsdatei: beide Kollisionsfälle wie oben. Ein Git-Testdouble leitete nur Fetch auf das lokale Remote um; sonst wurde echtes Git ausgeführt. Beide fremden origin-URLs wurden ohne Fetch abgewiesen. Keine produktiven Daten oder echtes privates Remote verwendet.
- `/workspace/.dotnet/dotnet --version`: `10.0.402`. Build und `tools/pruefen.sh` auf dem exakten Head blockiert, weil dessen Git-Objekte lokal fehlen; keine Testzahlen behauptet.

| Lauf `tools/pruefen.sh` | Ergebnis |
|---|---|
| Mit Originaldaten | blockiert: Head-Checkout fehlt |
| Ohne Originaldaten | blockiert: Head-Checkout fehlt |
| Pflichtlauf ohne Daten | blockiert: Head-Checkout fehlt; erwartetes Scheitern nicht nachgewiesen |

Akzeptierte neue `[APPROX]`: keine. Annahme für die Kollisionsproben: erfolgreicher Fetch eines neueren Commits mit kollidierendem Pfad. Das echte private Remote und der vollständige Head wurden nicht ausgeführt.
