# Unabhängiges Review: PR #69 Markerbericht

## 1. Merge-Empfehlung

**Ja** für genau Head `6328b1fe9de490113e630b18ebe1d21005638227`, Base
`6b8dc0077289837ef89c10d5b732b2a4965ff4a8`. Die geforderte Zählung stimmt mit der grep-Pipeline überein,
die Ausgabe ist deterministisch und die CI hängt sie nach dem Testbericht an die Step Summary an. Der CI-Lauf
dieses Heads ist auf GitHub erfolgreich. Übergabe, Übernahme und Abschluss sind im PR-Kommentarverlauf
vollständig.

## 2. Befunde

Keine Befunde.

Geprüfte Gegenproben:

- `python3 tools/markerbericht.py` zweimal ausgeführt und byteweise verglichen; beide Ausgaben identisch.
  Gesamt: **128 `[ORIGINAL]`, 174 `[APPROX]`**.
- `grep -rho "\[APPROX\]\|\[ORIGINAL\]" src | sort | uniq -c` ergab dieselben Summen und dieselbe
  sortierte Zeilenfolge.
- Isolierte Fixtures: mehrere Marker je Zeile, UTF-8, Sortierung, Pfad mit Pipe, leeres und fehlendes
  Quellverzeichnis sowie Aufruf aus einem anderen Arbeitsverzeichnis. Ausgabe wie erwartet; fehlender Ordner
  endet mit argparse-Fehlercode 2.
- Keine neuen Abhängigkeiten. Das Skript verwendet ausschließlich Python-Standardbibliothek.
- YAML geparst: Schritt `Markerbericht` folgt auf `Testbericht`, steht am Ende der Schritte des Jobs `build`
  und verwendet wie der bestehende Bericht `if: success() || failure()`.
- Übergabekette geprüft: ChatGPT übergibt `62ba50b` mit Bearbeiter frei; Claude Code bestätigt die Übernahme,
  rebasiert auf die vorgegebene Base, ergänzt den CI-Schritt und schließt mit Head `6328b1f` und Bearbeiter frei.
  Eine zusätzliche Übergabedatei fehlt nicht; die Vorlage verlangt den Kommentar am PR.
- Exakter Head ausgecheckt; `git status --short` sauber. Der Diff enthält ausschließlich die vorgegebenen
  zwei Dateien: `.github/workflows/build.yml` und `tools/markerbericht.py`.

## 3. Ausgeführte Prüfungen und akzeptierte `[APPROX]`

`tools/pruefen.sh` (drei Läufe) auf dem exakten Head, SDK 10.0.402, Release-Build mit Warnungen als Fehler:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 580 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 496 | 0 | 69 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 496 | 84 | 0 |

Das Skript meldet insgesamt bestanden. Der Pflichtlauf validiert die erwarteten Fehlschläge wegen fehlender
Originaldaten. GitHub Actions Lauf 138 für Commit `6328b1f` ist abgeschlossen und erfolgreich.

Akzeptierte `[APPROX]`: keine neuen; dieser PR ändert weder Spielregeln noch Marker im Quellcode. Die Zählung
ist Bestandsinformation und keine Aussage über Originaltreue. Annahmen oder ungeprüfte Originalformeln
stehen nicht im Umfang dieses Werkzeug- und CI-PRs.
