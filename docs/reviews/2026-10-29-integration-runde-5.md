# Unabhängiges Review: Integration Runde 5 (#128)

## Merge-Empfehlung

**Nein** für Head `6f3f0ff7b45da6bfddcb18cb28c5a534959e2797` (Base `a09c81e7d2e63830148ebfb1e023e9379f6b2b7b`): Ein Schema-5-Spielstand aus der parallel entwickelten Modus-4-Spur kann nicht geladen werden (R5-01). Ob produktiv genutzte Spielstände dieses ungemergten Spurstands existieren, ist nicht festgestellt; die angefragte Kompatibilität für Schema 5 ist dennoch nicht allgemein erfüllt. Originaltreue und Gegenproben sind nicht vollständig geprüft, siehe unten.

## Befunde

- **R5-01 (P2): Zwei unterschiedliche Schema-5-Formen, nur eine wird migriert.** [Migration 5 → 6](https://github.com/kaaasi/eishockey-manager-redone/blob/6f3f0ff7b45da6bfddcb18cb28c5a534959e2797/src/EishockeyManager.Core/SaveGame.Migration5.cs#L12) ergänzt nur `_nationalTurn`; der [Reader](https://github.com/kaaasi/eishockey-manager-redone/blob/6f3f0ff7b45da6bfddcb18cb28c5a534959e2797/src/EishockeyManager.Core/SaveGame.Reader.cs#L143) verlangt alle neuen Vereinsfelder. Schema 5 aus #123 enthält `_nationalTurn`, aber noch keine Vereinsfelder `_dressed` und `_dressedByHand`; Schema 5 aus #121 hat umgekehrt die Vereinsfelder. Gegenbeispiel: Einen aktuellen synthetischen Spielstand auf `schema = 5` setzen, bei allen Vereinen beide Felder entfernen und `_nationalTurn` behalten (Form von #123); `SaveGame.Load` meldet „Der Spielstand ist beschädigt: Verein 0: Feld _dressed in Club fehlt.“ Der gezielte Test in einer temporären Kopie schlug so fehl. Es wurde kein tatsächlich mit #123 gespeicherter Spielstand eingesetzt. Migration muss die Varianten anhand ihrer Felder unterscheiden und für die #123-Form auch die Taktikmigration ausführen; ein vorhandener offener Nationalzug darf dabei nicht verloren gehen. Die vorhandenen Tests bilden Schema 4 und das Schema 5 von #121 ab, nicht diese zweite Form.

## Ausgeführte Prüfungen und akzeptierte Näherungen

- Prüfstand: vollständiger Tree aus Base und 50 fest auf Head abgerufenen Dateien rekonstruiert; `git write-tree` = `77f4fc85e3874ca80d7e364c4f22b5679a081a39`, identisch mit dem GitHub-Tree des Head-Commits. SDK 10.0.402; Restore aus lokalem NuGet-Cache mit `NuGetAudit=false`. Original-ZIP aus dem privaten Referenzrepo anhand Blob `0d9da281bde4db3babd9a73b49800e138c9cbe13` geprüft. Keine Originaldaten im Review-Branch.
- H2-01: Szenario `highscore` gerendert und `oberflaeche_0848` vermessen: bei RGB-Summe > 600, x 100–219 und y 3–17 jeweils x = 117–202. H2-02: `HighscoreTests.Key_FromSetup` unverändert grün; Mutation `!(duration == 0)` in separater Kopie: 2 fehlgeschlagen, 8 bestanden. H2-03: `DebCupDrawFlowTests` und Titeltest mit Originaldaten: 7 bestanden; darunter Anzeige nach drittem Termin und Vergleich mit/ohne Anzeige über mehrere Saisons.
- `dotnet test tests/EishockeyManager.Core.Tests -c Release --no-build --filter 'FullyQualifiedName~SchemaFourSave_IsMigrated|FullyQualifiedName~TacticsOriginalTests|FullyQualifiedName~Key_FromSetup' -m:1`: 28 bestanden. Schema-4-Kette und Schema-5-Form von #121 werden in den vorhandenen Tests geladen; siehe R5-01 zur zweiten Form.
- `em.exe`: `objdump -D -b binary -m i8086 -M intel` an 0x488e9–0x48925 bestätigt Grenzen 100/7 und SEMI-Sperre; 0x553a2–0x554e0 bestätigt den Aufruf der Auswahl, die Modus-2-Sperre und das Statusbit aus #125. Die Bilder `bundestrainer_00058/00059` und das erzeugte Taktikbild sowie `paarungen-liga` wurden gesichtet; für Paarungen fehlt das Vergleichsvideo. Das allein belegt nicht alle Werte und Abläufe gegen das Original.
- Grenzen dieser Prüfung: Keine vollständige unabhängige Nachdisassembly von Taktikstärkewirkung, Modus-4-Zeitpunkten und Live-Auslosung; keine Gegenmutation je neuem Test und keine unabhängige Bestätigung aller Mehrsaison-Referenzwerte. Die im Code ausdrücklich als Näherung markierte Stärkewirkung ist keine em.exe-gleiche Formel. Kein Anspruch auf vollständige Originaltreue aus grünen Tests.
- Erwartet offen, nicht als neue Befunde gewertet: `[APPROX]` aus #119–#123 einschließlich Anzeige-Zufall/Animation, Highscore-Zentrierung und Punkte, Taktikvorbereitung/Stärke/Eingesetzt, Liga-Spielplan und Bildschirmfunktionen, Modus-4-Tabellenauswahl und Zugzeitpunkt; #125 und #126 sind offen. Diese Aufzählung übernimmt die Scope-Entscheidung der Anfrage, keine eigene Freigabe der Näherungen.

- `PATH=/workspace/.dotnet:$PATH NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=1 tools/pruefen.sh`, nach Offline-Restore und mit für VSTest freigegebenen lokalen Sockets:

  | Lauf | bestanden | fehlgeschlagen | übersprungen |
  |---|---:|---:|---:|
  | Mit Originaldaten | 807 | 0 | 0 |
  | Ohne Originaldaten | 673 | 0 | 119 |
  | Pflichtlauf ohne Daten (erwartet fehlgeschlagen) | 673 | 134 | 0 |

  Das Skript meldet „bestanden“; die Commit-Ausgabe `HEAD` ist wegen des rekonstruierten Arbeitsbaums ohne Commit nicht aussagekräftig. Der Tree-Nachweis oben bindet die Tests an den angefragten Head. Der erste Versuch ohne Socket-Freigabe brach ab und zählt nicht als Testergebnis.
