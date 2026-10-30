# Unabhängige Nachprüfung – Integration Runde 6, PR #171

## 1. Merge-Empfehlung

**Ja** für Head `47fc83a34072f48c2b4860b7dca38110f42646fe` gegen die angegebene Base `f0938fa62a85516d4f2765b5c9e9a5837db63745`, vorbehaltlich der allgemeinen Merge-Prüfung und aktueller Base: R6-R01 ist behoben; im geprüften Delta `280ceff726dfdfc7483d5521a0efdc2299b20abe..47fc83a34072f48c2b4860b7dca38110f42646fe` kein neuer Befund. Dies ist eine Nachprüfung des [vorigen Reviews](2026-10-30-integration-runde-6.md), kein erneutes Vollreview.

## 2. Befunde

Keine neuen Befunde. **R6-R01 (#173) behoben:** [MenuLamps.cs](https://github.com/kaaasi/eishockey-manager-redone/blob/47fc83a34072f48c2b4860b7dca38110f42646fe/src/EishockeyManager.UI/Screens/MenuLamps.cs#L84) prüft jede Eistafel mit ihrem eigenen Vertrag und ihrer eigenen Freigabe. Beide Gegenbeispiele wurden mit Originaldaten über die Oberfläche ausgeführt: HALLE 2/MEGAFROST 2, Tafel 1 belegt und Tafel 2 frei ergibt rot; Überdachung 0/MEGAFROST 2, Tafel 2 belegt ergibt grün. Beide Tafeln belegt ergibt grün. Die übrigen Werbeflächen waren jeweils belegt.

## 3. Ausgeführte Prüfungen und akzeptierte Näherungen

Git-Fetch scheiterte am Proxy. Der vollständige Head wurde aus dem lokalen Base-Checkout und allen 55 über den GitHub-Connector bezogenen Änderungen rekonstruiert; `git add -A && git write-tree` ergab `a3b4e5ee578e23bfe5d1449900a0030d1a61e929`, gleich dem Tree des Head-Commits. Arbeitsbaum und Index stimmten nach den Tests weiter überein. Das Skript nennt als Commit `f0938fa` mit eingecheckten Indexänderungen: geprüft wurde der anhand des vollständigen Trees verifizierte rekonstruierte Head, kein Checkout des Commitobjekts. SDK `10.0.402`; Original-ZIP aus dem privaten Referenz-Repo, SHA-256 `142fc78e9b286cb47f36baa450f5af311ac0902525564d0c30ab63697ca321f4`, außerhalb des Repos entpackt.

- `dotnet test tests/EishockeyManager.UI.Tests -c Release --no-restore --filter FullyQualifiedName~IntegrationRunde6AdLampTests`: **3 bestanden**, keine übersprungen. In separater Kopie `is null` für den Vertrag von Tafel 2 zu `is not null` mutiert: **alle 3 Tests fehlgeschlagen**; die drei Tests erkennen damit diese konkrete Verfälschung.
- `objdump -D -b binary -m i8086 -M intel --start-address=0x42bd0 --stop-address=0x42c50 em.exe`: Das Original prüft für jede Fläche zunächst den eigenen Vertrag (`0x42BEB–0x42C07`), bei Fläche 9 separat HALLE = 2 (`0x42C09–0x42C1E`), bei Fläche 10 separat MEGAFROST = 2 (`0x42C20–0x42C35`); freigegebene unbelegte Fläche setzt die Warnung (`0x42C37`). Das entspricht der Änderung und den beiden Gegenbeispielen.
- Offline-Restore mit `dotnet restore EishockeyManager.slnx --source /home/agent/.nuget/packages --disable-parallel -p:NuGetAudit=false -m:1`; danach `NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=2 tools/pruefen.sh` mit lokalem Socket-Zugriff. NuGet-Audit nur für den Offline-Lauf deaktiviert; Release-Build mit Warnungen als Fehler bestanden.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 901 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 747 | 0 | 139 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 747 | 154 | 0 |

Das Skript bestätigt beim Pflichtlauf ausschließlich Fehler wegen fehlender Originaldaten. Neue `[APPROX]`: keine; die frühere Ein-Flächen-Näherung entfällt. Die im vorigen Review akzeptierten Näherungen bleiben im dort beschriebenen Umfang akzeptiert. Annahme: Die separate Merge-Prüfung berücksichtigt insbesondere den aktuellen `main` und CI für den genannten Head.
