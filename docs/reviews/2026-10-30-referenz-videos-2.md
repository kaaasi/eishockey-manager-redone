# Review PR #127: Referenzvideos, Teil 2

## Merge-Empfehlung

**Nein** für Head `f720e10aaa18ec88b2511c9259dde30292cbb4cf` (Base `27fa1c4da0f1f76603b3c62118804f5e3c663036`): Eine Fundstelle ist falsch beschrieben und vorhandene Referenz-Klone werden für die neuen Gruppen nicht aktualisiert. Die tatsächlichen Gruppendownloads konnten in dieser Umgebung nicht ausgeführt werden.

## Befunde

- **RV2-01 · P2 – Vorhandener Klon bleibt auf altem Stand.** [`fetch-reference.sh`, Zeilen 28–31](https://github.com/kaaasi/eishockey-manager-redone/blob/f720e10aaa18ec88b2511c9259dde30292cbb4cf/tools/claude/fetch-reference.sh#L28-L31): Existiert `$REFERENZ/.git`, wird weder gefetcht noch der ausgecheckte Commit aktualisiert; `sparse-checkout add` ergänzt nur Pfade des bestehenden Commits. Gegenbeispiel: Ein vor `videos-roh-2` angelegter Referenz-Klon und anschließend `fetch-reference.sh speedrun` ergeben keine neuen Bilder, obwohl das Skript laut `CLAUDE.md` einen vorhandenen Klon ergänzt. Nach dem dokumentierten Zurücksetzen und Neuschreiben der Referenz-Historie genügt ein blindes Fast-forward nicht unbedingt. Die anschließende `echo`-Ausgabe kann trotz fehlendem Verzeichnis null Dateien melden. Frischer Klon ist davon nicht betroffen.
- **RV2-02 · P3 – Pokalbild enthält keine Ergebnisse.** [`docs/referenz-videos.md`, Zeile 309](https://github.com/kaaasi/eishockey-manager-redone/blob/f720e10aaa18ec88b2511c9259dde30292cbb4cf/docs/referenz-videos.md#L309): `duell_s2a_01217` wird als „22.9. mit Ergebnissen“ bezeichnet. Das Bild zeigt die Paarungen ohne Ergebniszahlen; wer Ergebnisse sucht, findet dort keine. Auch der Verweis in `docs/wettbewerbe.md` belegt damit nur die Übersicht, keine Ergebnisse.

## Ausgeführte Prüfungen und akzeptierte `[APPROX]`

- GitHub-Compare der angegebenen SHAs: drei geänderte Dateien (`docs/referenz-videos.md`, `docs/wettbewerbe.md`, `tools/claude/fetch-reference.sh`), keine Bilder oder Ausschnitte im PR-Diff. Head-Dateien und Skript per Connector unter festem SHA gelesen; kein vollständiger Head-Checkout. Der direkte `git fetch origin` scheiterte an der lokalen Socket-/Proxy-Sperre.
- Zwölf Bilder aus dem privaten Referenz-Repo bei `2bfe462` außerhalb des Code-Repos visuell mit der Fundliste verglichen: `eisbaeren_f01_01431`, `eisbaeren_f24_01048`, `eisbaeren_f35_00646`, `duell_s2b_00503`, `eisbaeren_f07_03843`, `duell_s2a_02651`, `duell_s2a_01217`, `duell_s2a_01138`, `duell_s2a_02108`, `speedrun_t1_00456`, `eisbaeren_f51_03346`, `duell_s2a_01126`. Elf entsprechen den jeweils geprüften Angaben; Ausnahme RV2-02.
- Gruppenerweiterung im Skript gelesen: `eisbaeren` umfasst 01–65 außer 41, `duell` die elf angegebenen Reihen, `speedrun` beide Teile. Ein tatsächlicher Download aller Gruppen wurde nicht ausgeführt; die Korrektheit des Ladens ist daher nicht vollständig bestätigt (siehe RV2-01).
- CI auf dem angegebenen Head: [Build #223](https://github.com/kaaasi/eishockey-manager-redone/actions/runs/40998121276) erfolgreich; Build-Job und synthetischer Test erfolgreich, Publish übersprungen. CI hat keine Originaldaten. Lokaler Build, `tools/pruefen.sh` und weitere Prüfläufe entfielen auf ausdrücklichen Auftrag; daher keine lokale Prüftabelle. `dotnet --version` schlug fehl (`dotnet` nicht im PATH).
- Erwartet offen bestätigt als Dokumentationsstand: Highscore am Spielende und Saisonende im Modus „nur Weltkugel“ bleiben offen; `CLAUDE.md` nennt die neuen Ladeoptionen nicht. Die Abwesenheit in sämtlichen Videos wurde nicht unabhängig durch vollständige Sichtung verifiziert.
- Akzeptierte neue `[APPROX]`: keine. Annahme für RV2-01: ein bestehender Klon steht noch auf einem Commit vor den neuen Verzeichnissen; dies folgt aus dem Skriptablauf, nicht aus einem ausgeführten Download.
