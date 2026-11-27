# Nachprüfung: Integration Runde 5 (#128)

## Merge-Empfehlung

**Nein** für Head `ebf602aa00c05507ed120eff2a88317ae2754f87` (Base `a09c81e7d2e63830148ebfb1e023e9379f6b2b7b`): R5-01 ist im angefragten synthetischen Gegenbeispiel behoben und im Delta wurde kein neuer Befund festgestellt. Der eigene Pflichtlauf mit Originaldaten fehlt jedoch; vor einer Freigabe muss er auf genau diesem Head nachgeholt werden. Umfang: nur Delta `6f3f0ff..ebf602a` und die angefragten Befunde; übrige Grenzen des [vorigen Reviews](https://github.com/kaaasi/eishockey-manager-redone/pull/129) gelten weiter.

## Befunde

Keine neuen Befunde im geprüften Umfang. **R5-01 (P2, #130)**: Das Gegenbeispiel lädt, der offene Zug bleibt und die alten Taktikwerte werden auf Startwerte gesetzt. Der Test verwendet einen synthetisch umgeformten Spielstand, keinen tatsächlich mit #123 gespeicherten Spielstand.

Die [Bedingung](https://github.com/kaaasi/eishockey-manager-redone/blob/ebf602aa00c05507ed120eff2a88317ae2754f87/src/EishockeyManager.Core/SaveGame.Migration5.cs#L20) ruft für die Form von #121 mit vorhandenen `_dressed`-Feldern `MigrateFrom4` nicht auf; ein ergänzter Test mit verändertem Taktikwert bestätigt dessen Erhalt. Die Schema-4-Kette ergänzt die Felder vor dem Schritt 5 → 6 und löst damit keinen erneuten Nachholschritt aus.

## Ausgeführte Prüfungen und akzeptierte Näherungen

- Prüfstand: Base lokal, 51 anhand des festen Head abgerufene Dateien rekonstruiert. Für den Vorgänger wurden die vier Delta-Dateien zurückgesetzt: `git write-tree` = `77f4fc85e3874ca80d7e364c4f22b5679a081a39`, identisch mit dem im ersten Review nachgewiesenen Tree. Danach die vier Delta-Dateien erneut fest auf Head abgerufen; resultierender Tree `651d7c793828b7255602316ee1f50509dde1c9b4`. Der Checkout ist rekonstruiert, kein ausgecheckter Head-Commit. Git-Fetch scheiterte am Proxy. SDK 10.0.402, Offline-Restore aus lokalem NuGet-Cache mit `NuGetAudit=false`; Release-Build mit Warnungen als Fehler bestanden.
- `dotnet test tests/EishockeyManager.Core.Tests -c Release --no-build --filter 'FullyQualifiedName~SchemaFive_FromMode4Track|FullyQualifiedName~SchemaFourSave_IsMigrated' -m:1`: 3 bestanden. Dies umfasst Schema 4 → 5 → 6 und beide Schema-5-Formen. Neuer Test: offener Zug, fehlende Vereinsfelder, alter Taktikwert und bytegleicher Rückvergleich. In separater Kopie Aufruf von `MigrateFrom4` entfernt: Test rot mit „Verein 0: Feld _dressed in Club fehlt“. In separater Kopie den #121-Test um einen Taktikwert 42 ergänzt: Wert nach Laden erhalten, 1 bestanden.
- `PATH=/workspace/.dotnet:$PATH NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=1 tools/pruefen.sh`, mit freigegebenen lokalen Test-Sockets:

  | Lauf | bestanden | fehlgeschlagen | übersprungen |
  |---|---:|---:|---:|
  | Mit Originaldaten | – | – | – (keine Daten vorhanden) |
  | Ohne Originaldaten | 674 | 0 | 119 |
  | Pflichtlauf ohne Daten (erwartet fehlgeschlagen) | 674 | 134 | 0 |

  Das Skript meldet bestanden; seine Commit-Ausgabe nennt wegen des rekonstruierten Arbeitsbaums die Base und bindet die Prüfung nicht an den Head. Originaldaten wurden in dieser Sitzung nicht geprüft.
- Erwartet offen: die im vorigen Review aufgeführten `[APPROX]` aus #119–#123 sowie #125 und #126. Diese Scope-Entscheidung wird übernommen, nicht als eigenständige Prüfung oder Freigabe der Originaltreue gewertet.
