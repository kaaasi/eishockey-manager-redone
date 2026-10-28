# Unabhängiges Prozess-Review vom 28.10.2026

## 1. Merge-Empfehlung

**Nein für das gemeinsame Paket in den angefragten Ständen.** Insbesondere kann #52 neue, ungemergte Arbeit löschen; der Prüflauf aus #45/#50 kann ohne wirksamen Negativtest grün werden. Frische ChatGPT/Codex-Sitzung, keine Mitarbeit an den Implementierungen.

| PR | Base | geprüfter Head | Empfehlung |
|---|---|---|---|
| #45 | `cc26bde939f6a94485bb2ee7f4dc3053741a8d3a` | `6a6f227cca1ad5f1aab2122ac486d055a3e27a87` | Nein: PROZ-02/03 |
| #46 | `cc26bde939f6a94485bb2ee7f4dc3053741a8d3a` | `237c3f66dda95e5d21413d86dbea76743874a2d1` | Noch nein: kein eigener Sachbefund, aber Originaldaten-Nachweis der Merge-Prüfung fehlt hier |
| #50 | #45, `6a6f227cca1ad5f1aab2122ac486d055a3e27a87` | `d01618a62c955aa5676972a1f270f0ffd5af95fc` | Nein: PROZ-02 bleibt; PROZ-03 ist hier behoben |
| #52 | `cc26bde939f6a94485bb2ee7f4dc3053741a8d3a` | `4af046aa262d3343a57009f6352a9ab2b6fdc776` | Nein: PROZ-01 |

**Übernahme ohne Chatverlauf: grundsätzlich ja.** `AGENTS.md` und die Vorlagen verweisen auf Rollen, aktuelle Arbeitspakete, Übergabekommentare, feste SHAs, Einrichtung und Merge-Prüfung. Die verlinkten Repository-Dateien und PR-Kommentare sind dafür erforderlich; nur die beiden Verzeichnisse isoliert reichen ausdrücklich nicht. Die drei Kurzaufrufe beschreiben einen ausreichend konkreten Einstieg. Die nachfolgend belegten technischen Sicherungslücken bleiben davon unabhängig.

**Konfliktfreie gemeinsame Integration: ja**, bezogen auf `main` = `ee7ea0ed58570312e564a3568cb1fff5bcb9d712`. Inhaltsgetreue Git-Dreifachmerge-Proben: #45 → #50 → #46 → #52 → main, jeweils Exit 0, keine Konfliktmarker. Auch #46/#52 und #50/#46 bzw. #50/#52 paarweise konfliktfrei. Im gemeinsamen Workflow bleiben sowohl `-warnaserror` als auch Downloads nur per manueller Anforderung/Versions-Tag und sieben Tage Aufbewahrung erhalten. Das ist keine Aussage über spätere main-Stände und keine Sicherheitsfreigabe für den Löschjob.

## 2. Befunde

**PROZ-01 · P1 · Branch-Löschung ist nicht an die geprüfte Spitze gebunden.**

[aufraeumen.yml:34–42 am Head von #52](https://github.com/kaaasi/eishockey-manager-redone/blob/4af046aa262d3343a57009f6352a9ab2b6fdc776/.github/workflows/aufraeumen.yml#L34-L42) prüft die beim Checkout geladene `origin/<branch>`-Ref, löscht anschließend aber die dann aktuelle Remote-Ref ohne SHA-Bindung.

Reproduziert mit einem lokalen Bare-Remote und dem unveränderten Shell-Block des Workflows; nur `gh` lieferte eine kontrolliert leere PR-Liste:

1. Remote `main=A`, `feature=A`; Workflow-Klon mit vollständigem Fetch erstellen.
2. In einem zweiten Klon `feature` um Commit B erweitern und pushen. B ist kein Vorfahr von main. Der Workflow-Klon hält weiterhin `origin/feature=A`.
3. Workflow-Block mit `NUR_ANZEIGEN=false` ausführen.
4. `merge-base --is-ancestor origin/feature origin/main` akzeptiert A; `git push origin --delete feature` löscht B. Workflow Exit 0, Zusammenfassung: „vollständig in main“; `git ls-remote ... refs/heads/feature` ist leer.

Damit ist die geforderte Eigenschaft „löscht nur gemergte Branches“ widerlegt. Die PR-Liste wird ebenfalls nur einmal vor der Schleife gelesen; ein danach eröffneter PR ist ungeschützt. Abhilfe: unmittelbar vor der Aktion PR-Zustand erneut prüfen und die Löschung mit dem konkret geprüften Remote-SHA absichern, etwa über eine explizite `--force-with-lease=refs/heads/<branch>:<sha>`-Lösch-Refspec. Ein zusätzlicher Fetch allein schließt das Rennen nicht. Kein echter GitHub-Branch wurde für diese Probe gelöscht.

**PROZ-02 · P2 · Fehlender/defekter Testlauf kann als erfolgreicher Nachweis erscheinen.**

[pruefen.sh:22–24](https://github.com/kaaasi/eishockey-manager-redone/blob/d01618a62c955aa5676972a1f270f0ffd5af95fc/tools/pruefen.sh#L22-L24) gibt bei fehlender Testzusammenfassung `0 0 0` zurück. [Zeilen 58–64](https://github.com/kaaasi/eishockey-manager-redone/blob/d01618a62c955aa5676972a1f270f0ffd5af95fc/tools/pruefen.sh#L58-L64) werten jeden von null verschiedenen Exitcode als gewünschten Pflichtfehler. Beide Probleme bestehen schon in #45.

Reproduzierbare Gegenprobe in einer temporären Kopie des Skripts: einen `dotnet`-Stub vorne in `PATH` setzen. Build und `--version` liefern Exit 0; für normale Tests liefert er entweder eine bekannte Zusammenfassung oder gar keine Ausgabe. Für `EM_REQUIRE_GAMEDATA=1` liefert er nur „Testhost konnte nicht gestartet werden“, Exit 1:

```bash
#!/usr/bin/env bash
case "$1" in
  build) exit 0 ;;
  --version) echo mock; exit 0 ;;
  test)
    if [ "${EM_REQUIRE_GAMEDATA:-}" = 1 ]; then
      echo 'Testhost konnte nicht gestartet werden'; exit 1
    fi
    # Diese Zeile für die zweite Gegenprobe entfernen:
    echo 'Passed!  - Failed: 0, Passed: 12, Skipped: 4, Total: 16'
    exit 0 ;;
esac
```

Beobachtet: Skript Exit 0, „Ergebnis: bestanden“, Pflichtlauf `0/0/0` als „scheitert wie gewollt“. Ohne die Echo-Zeile wird sogar der normale Lauf mit `0/0/0` akzeptiert. Erforderlich sind nachweislich ausgeführte Tests sowie ein Negativnachweis des **erwarteten Originaldatenfehlers**, nicht bloß irgendein Prozessfehler. Strukturierte Ergebnisse pro erwartetem Testprojekt sind dafür belastbarer als allein die Konsolensumme.

**PROZ-03 · P2 · #45 überspringt im Schnellmodus ohne Originaldaten sämtliche Tests; durch #50 behoben.**

[pruefen.sh:48 am Head von #45](https://github.com/kaaasi/eishockey-manager-redone/blob/6a6f227cca1ad5f1aab2122ac486d055a3e27a87/tools/pruefen.sh#L48) führt den synthetischen Lauf nur bei `QUICK=0` aus. Reproduziert real: ohne `gamedata/kb`, `bash tools/pruefen.sh --schnell` → Build, ausschließlich die Tabellenzeile „Mit Originaldaten – keine Daten vorhanden“, Exit 0; kein `dotnet test`. #50 ergänzt den Fall `HAVE_DATA=0` korrekt. #45 deshalb nicht allein als fertige Agenten-Grundlage freigeben; #50 behebt diesen konkreten Befund im gemeinsamen Stand.

## 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

Vorlage: `docs/vorlagen/review-anfrage.md` aus #50; außerdem AGENTS, Zusammenarbeit §§5/6, alle drei Startvorlagen, Handoff-/PR-Vorlagen und die Diffs der vier Prozess-PRs gelesen. Sämtliche angefragten Heads über GitHub-Metadaten, Blob-Hashes und die vollständigen Git-Trees verifiziert; die signierten Head-Commitobjekte lokal SHA-identisch rekonstruiert. Für Merge-Proben wurden temporäre Commits mit exakt diesen Trees und den angegebenen Base-Beziehungen benutzt, nicht die echten PR-Branches verändert.

SDK **10.0.402**, Linux. Der direkte Clone hatte keine Anmeldung; Dateistände kamen über den authentifizierten GitHub-Zugriff. Paralleles MSBuild scheiterte in dieser Umgebung ohne verwertbare Diagnose. Ein transparenter lokaler `dotnet`-Wrapper ergänzte bei `build`/`test` ausschließlich `-m:1 -p:UseSharedCompilation=false`, zusätzlich `DOTNET_PROCESSOR_COUNT=2`. `-c Release`, `-warnaserror` und die Testfilter des Skripts blieben erhalten. Release-Builds damit ohne Warnungen/Fehler.

Ausgeführt: `bash tools/pruefen.sh` jeweils auf #45, #46, #50, #52 und dem gemeinsamen Stand. Bei #46/#52 wurde das dort noch fehlende Skript aus #50 als ungetracktes Prüfwerkzeug bereitgestellt; deren geprüfter Anwendungscode/Tests blieben unverändert.

| Stand | Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---|---:|---:|---:|
| #45 | Mit Originaldaten | – | – | nicht ausführbar, Daten fehlen |
| #45 | Ohne Originaldaten | 297 | 0 | 47 |
| #45 | Pflichtlauf ohne Daten | 297 | 54 | 0 |
| #46 | Mit Originaldaten | – | – | nicht ausführbar, Daten fehlen |
| #46 | Ohne Originaldaten | 297 | 0 | 47 |
| #46 | Pflichtlauf ohne Daten | 297 | 54 | 0 |
| #50 | Mit Originaldaten | – | – | nicht ausführbar, Daten fehlen |
| #50 | Ohne Originaldaten | 297 | 0 | 47 |
| #50 | Pflichtlauf ohne Daten | 297 | 54 | 0 |
| #52 | Mit Originaldaten | – | – | nicht ausführbar, Daten fehlen |
| #52 | Ohne Originaldaten | 297 | 0 | 47 |
| #52 | Pflichtlauf ohne Daten | 297 | 54 | 0 |
| gemeinsam + main | Mit Originaldaten | – | – | nicht ausführbar, Daten fehlen |
| gemeinsam + main | Ohne Originaldaten | 297 | 0 | 47 |
| gemeinsam + main | Pflichtlauf ohne Daten | 297 | 54 | 0 |

Die Pflichtläufe endeten mit Testfehlern wegen fehlender Originaldaten; die positive Zusammenfassung des Skripts ist dessen erwartete Gesamtauswertung. Unterschiedliche Gesamtzahlen bei Skips versus Pflichtlauf entstehen durch erst bei Ausführung aufgelöste Theory-Datenzeilen. Die Tabelle ist kein Ersatz für den hier fehlenden echten Originaldatenlauf.

Zusätzlich: echter Schnelllauf von #45 (PROZ-03); kontrollierte Summen-/Fehler-Stub-Proben (PROZ-02); realer lokaler Remote-Löschversuch (PROZ-01); TRX-Fixture für `testbericht.py` aus #46 mit 2 synthetisch ausgeführten Fällen, davon 1 Fehler, und 1 übersprungenem Originaldatenfall: Zahlen und Hinweis korrekt. Die echte Ausgabe für #54 wurde ergänzend unabhängig aus TRX-Ergebniszeilen nachgezählt (siehe separates Integrationsreview); dabei stimmen die Konsolensummen.

**Akzeptierte [APPROX]:** keine neuen fachlichen Näherungen in diesem Prozesspaket.

**Annahmen/Grenzen:** Repository-Verweise und dokumentierte Übergaben müssen gepflegt bleiben. Originaldaten, echte GitHub-Actions-Ausführung der neuen Workflows und der Originaldaten-Nachweis der Merge-Prüfung wurden hier nicht nachgewiesen. Lokale Merge-Konfliktfreiheit gilt ausschließlich für die angegebenen Trees; keine PR-Beschreibung wurde als Testnachweis übernommen.
