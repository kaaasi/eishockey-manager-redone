# Dritte Nachprüfung PR #127: Referenzvideos, Teil 2

## 1. Merge-Empfehlung

**Nein** für Head `73a57bf71323239363d668edf627e779bd7fd640` (Base `27fa1c4da0f1f76603b3c62118804f5e3c663036`). Die Sauberkeitsprüfung kann durch eine Git-Konfiguration ignorierte Dateien übersehen; der Checkout kann sie überschreiben. Geprüft wurden das Delta `da98be2..73a57bf` und die genannten Befunde.

## 2. Befunde

- **RV2N3-01 · P1 – Git-Konfiguration verbirgt ignorierte Dateien vor der Schutzprüfung.** [fetch-reference.sh, Zeilen 47–52](https://github.com/kaaasi/eishockey-manager-redone/blob/73a57bf71323239363d668edf627e779bd7fd640/tools/claude/fetch-reference.sh#L47-L52): `status --porcelain --ignored` respektiert `status.showUntrackedFiles=no` und meldet dann weder unversionierte noch ignorierte Dateien. `checkout` überschreibt ignorierte Dateien standardmäßig. Gegenbeispiel: sauberer alter Klon mit erlaubter origin; `git config status.showUntrackedFiles no`, `collision` in `.git/info/exclude` eintragen, unversionierte `collision` mit `local precious` anlegen. Ein neuer `main` enthält versioniertes `collision` mit `remote`. `git status --porcelain --ignored` ist leer; nach erfolgreichem Fetch und Skriptlauf enthält die Datei `remote`. Im Test leitete ein Git-Testdouble ausschließlich den Fetch auf ein lokales synthetisches Remote um; Status und Checkout führte echtes Git aus. Der spätere Fehler `no sparse-checkout to add to` im nicht-sparsen Testklon tritt erst nach dem Überschreiben auf. Die Konfiguration kann auch global gesetzt sein.

**RV2N2-01 (#147):** Bei normaler Git-Konfiguration bricht das Skript vor Fetch mit Status 2 ab; `collision` bleibt `local precious`. Der oben genannte Fall zeigt eine verbleibende Lücke für ignorierte Dateien. **RV2N2-02 (#148):** Lokale origin `/tmp/…/kaaasi/eishockey-manager-referenz` und `https://example.invalid/kaaasi/eishockey-manager-referenz.git` werden jeweils mit Status 2 abgewiesen. Erwartet offen bestätigt: `CLAUDE.md` nennt die neuen Ladeoptionen weiterhin nicht.

## 3. Ausgeführte Prüfungen und akzeptierte `[APPROX]`

- GitHub-Compare und Dateien mit festen Refs gelesen: ein Commit, nur `tools/claude/fetch-reference.sh` geändert. Die separat ausgeführte Skriptkopie hat Git-Blob `62b75f812b968928e668fd334ca1d9c7f1913e04`, identisch zum Connector. Kein vollständiger Checkout des Heads: `git fetch origin` scheiterte am nicht erreichbaren Proxy. Der frühere Bericht wurde aus PR #145 gelesen.
- Isolierte synthetische Git-Repositories: Kollision und beide fremden origin-URLs wie oben; zusätzliche Probe mit `status.showUntrackedFiles=no` und ignorierter Kollision wie im Befund. Kein Zugriff auf den echten Referenz-Klon; der erfolgreiche Fetch wurde in der zusätzlichen Probe durch ein lokales Remote ersetzt. Kein produktiver Inhalt verwendet.
- SDK `PATH=/workspace/.dotnet:$PATH dotnet --version`: `10.0.402`. Build und `tools/pruefen.sh` auf dem exakten Head blockiert, da die Git-Objekte lokal fehlen und Fetch scheitert; keine Testzahlen behauptet.

| Lauf `tools/pruefen.sh` | Ergebnis |
|---|---|
| Mit Originaldaten | blockiert: Head-Checkout fehlt |
| Ohne Originaldaten | blockiert: Head-Checkout fehlt |
| Pflichtlauf ohne Daten | blockiert: Head-Checkout fehlt; kein erwartetes Scheitern nachgewiesen |

Akzeptierte neue `[APPROX]`: keine. Annahme für RV2N3-01: erfolgreicher Fetch eines Commits mit dem kollidierenden Pfad. Die Funktion mit dem echten privaten Remote bleibt ungeprüft.
