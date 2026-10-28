<!-- Regeln: AGENTS.md und docs/prozess/zusammenarbeit.md. Nicht Zutreffendes mit „–“ beantworten, nicht löschen. -->

**Bearbeiter:** <Agent> · **Spur/Paket:** <name> · **Befunde:** <IDs>

## Prüfstand
- Base: `<sha>`
- Head: `<sha>`

## Was und warum
<!-- Ziel, Ausgangsbefund, Änderungsumfang -->

## Fehler vorher (reproduziert)
<!-- Ablauf und beobachteter Wert vor dem Fix -->

## Regressionstest
<!-- Name, was er prüft; rot vor dem Fix (wie gezeigt?), grün danach -->

## Tests (`tools/pruefen.sh` zum Head)
| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | | | |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | | | |
| Pflichtlauf ohne Daten (muss scheitern) | | | |

Mehrsaisonprobe (falls Kader, Geld oder Saisonwechsel berührt): <Test / –>

## Gemeinsame Dateien und Dateigrenzen
<!-- Berührte Dateien außerhalb der eigenen Spur mit Begründung, sonst „keine“ -->

## Restlücken, neue [APPROX]
- …

## Auswirkungen auf GitHub-Kontingente
<!-- neue/geänderte Workflows, Artefakte, Laufzeit – sonst „keine“ -->
