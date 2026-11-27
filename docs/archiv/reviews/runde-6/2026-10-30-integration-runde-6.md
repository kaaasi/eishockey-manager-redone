# Unabhängiges Review – Integration Runde 6, PR #171

## 1. Merge-Empfehlung

**Nein** für Head `280ceff726dfdfc7483d5521a0efdc2299b20abe` gegen Base `f0938fa62a85516d4f2765b5c9e9a5837db63745`: Die Integration der zwei Eiswerbetafeln mit der neuen Warnlampe ist fehlerhaft (R6-R01, P2). Nach Korrektur ist eine Nachprüfung des Deltas nötig. Die ausdrücklich zurückgestellten Punkte sind kein zusätzlicher Ablehnungsgrund.

Reviewer: ChatGPT/Codex, frische Sitzung ohne Beteiligung an der Implementierung. Eigenständige Ausführung am vollständig verifizierten Tree, nicht nur Codelesen. Änderungen und Mutationen ausschließlich in separaten lokalen Prüfkopien; der geprüfte PR bleibt unverändert.

## 2. Befunde

**R6-R01 · P2 – Werbewarnlampe berücksichtigt die zweite Eistafel nicht.**

Fundstelle: [MenuLamps.cs, Zeilen 83–84](https://github.com/kaaasi/eishockey-manager-redone/blob/280ceff726dfdfc7483d5521a0efdc2299b20abe/src/EishockeyManager.UI/Screens/MenuLamps.cs#L83).

`FreeAdSpace` verbindet HALLE und MEGAFROST mit ODER, prüft anschließend aber ausschließlich den Vertrag für `Eisflaeche`. Seit #133 existiert `Eisflaeche2` mit eigener Verfügbarkeit und eigenem Vertrag. Damit kann Lampe 4 freie, nutzbare Werbung übersehen oder dauerhaft vor einer gesperrten Fläche warnen.

Reproduktion mit Originaldaten: Neues Spiel; alle sieben Banden, Trikot und Fernsehen belegen. Angebote für die folgenden Eisverträge einstellen und über `AcceptOffer` annehmen:

| Stadion und Eisverträge | Erwartet, Lampe 4 | Tatsächlich |
|---|---|---|
| HALLE = 2, MEGAFROST = 2; Tafel 1 belegt, Tafel 2 frei | rot | grün |
| Überdachung = 0, MEGAFROST = 2; Tafel 2 belegt, Tafel 1 gesperrt | grün | rot |

Eigene parametrisierte Gegenprobe `IndependentR6ReviewTests.AdLamp_TracksEachAvailableIceBoard`: **beide Fälle rot**. In einer temporären Kopie beide Tafeln jeweils mit ihrer eigenen Freigabebedingung und ihrem eigenen Vertrag prüfen: **beide grün**. Zusammen mit dem unabhängigen Hot-Seat-Test: vorher 1 bestanden / 2 fehlgeschlagen, danach 3 / 0. Der Originalcode bestätigt die getrennten Prüfungen: `0x42C09–0x42C1E` für Fläche 9/Überdachung, `0x42C20–0x42C35` für Fläche 10/Kühlanlage. Der `[APPROX]`-Kommentar in Zeile 73 beschreibt einen inzwischen überholten Modellmangel; er begründet die Abweichung im integrierten Stand nicht mehr. Empfohlen: beide Bedingungen getrennt prüfen und beide Gegenbeispiele als Regressionstests aufnehmen.

## 3. Ausgeführte Prüfungen und akzeptierte Näherungen

**Prüfstand und Umgebung.** Git-Fetch scheiterte an der Proxy-Verbindung. Deshalb vollständige Rekonstruktion über den GitHub-Connector: unveränderte Blobs aus dem vorhandenen Checkout, geänderte/neue Dateien am festen Head geladen, Pfadbestand und Dateimodi abgeglichen. `git add -A && git write-tree` ergab **`d6aab75303c38b209e0abacb57ec0d1e9494cbbe`**, identisch mit dem Tree des angefragten Commits; Arbeitsbaum vor/nach der Gesamtprüfung unverändert. Der lokale Hilfscommit hat eine andere Commit-ID und ist kein Remote-Commit. Die Vergleichs-Base wurde ebenso verifiziert: Tree **`c7e074299639ab6eb58d0785cda79dd0e24d2e71`**. Instrumentierte Vergleichs- und Mutationskopien sind ausdrücklich zusätzliche Diagnoseausführungen.

SDK im Repo: `10.0.402`. Original-ZIP aus dem privaten Referenz-Repo geladen, SHA-256 **`142fc78e9b286cb47f36baa450f5af311ac0902525564d0c30ab63697ca321f4`** geprüft, außerhalb des Code-Repos entpackt und als ignoriertes `gamedata` verlinkt. Offline-Restore:

```sh
export DOTNET_ROOT=/workspace/.dotnet PATH=/workspace/.dotnet:$PATH
dotnet restore EishockeyManager.slnx --source /home/agent/.nuget/packages --disable-parallel -p:NuGetAudit=false -m:1
NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=2 tools/pruefen.sh
```

NuGet-Audit nur für diese Offline-Ausführung deaktiviert. VSTest benötigte genehmigte lokale Socket-Rechte. Ein erster Gesamtlauf wurde während der Nutzungsunterbrechung im Pflichtlauf abgebrochen; dessen unvollständige Ausgabe gilt nicht als bestandener Gesamtlauf. Die folgende Tabelle stammt aus der vollständigen Wiederholung:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 898 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 747 | 0 | 136 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 747 | 151 | 0 |

Release-Build mit Warnungen als Fehler und Gesamtskript bestanden. Die 151 Pflichtfehler wurden vom Skript ausschließlich als fehlende Originaldaten validiert.

**Gegenbeispiele zuerst:** `dotnet test tests/EishockeyManager.Core.Tests -c Release --no-restore --filter FullyQualifiedName~IntegrationRunde6MigrationTests`: **2 bestanden**. Schema 7 ohne `SponsorRating`, `Rating`, `SignedRating`, aber mit Statistik lädt; zwei Spiele, Minimum und Rekord bleiben erhalten. Schema 6 ohne Bewertungsfelder und `_homeStatistics` lädt über 6 → 7 → 8, bekommt leere Statistik und übersteht erneutes Sichern/Laden bytegleich. Mutation nur des Nachholaufrufs in `MigrateFrom7`: Schema-7-Test rot mit **„Feld SignedRating in SponsorContract fehlt“**, Schema-6-Test weiter grün. Mutation der Migrationstabelle `[7] = root => root`: beide rot; Schema 6 scheitert am fehlenden `_homeStatistics`.

**Weitere Gegenproben:** In zwei getrennten Mutationsläufen wurden Bewertungs-/Angebotsformeln, Monatsdatum, Eisfreigaben, Verlängerung, Statistikfortschreibung/-reset, Top-5-Prädikat, Highscore-Schlüssel sowie UI-Koordinaten, Ergebnisrichtung, Datumsanzeige, WM-Navigation und Symbolzeichnung gezielt verfälscht. Filter: `FinanceWerbungTests|HeimspielStatistikTests|HighscoreKeyOriginalTests|SeasonEndStatusTests|IntegrationRunde6MigrationTests` (Core) und `FinanceWerbungScreenTests|PixelVereinTests|PixelWettbewerbeTests|SeasonEndStatusUiTests|IntegrationRunde6Tests|HotSeatUiTests` (UI). Ergebnisse: **51/51 Core-Fälle und 25/25 UI-Fälle rot**, jeweils nach erfolgreicher Kompilierung, TRX ausgewertet. Mehrere Mutationen je Lauf: Das belegt Fehlersensitivität, keine isolierte Zuordnung jeder Assertion zu genau einer Mutation; einzelne Fälle scheitern bereits an veränderten Vorbedingungen.

**PV-03 / R2-06:** Disassemblierung mit `objdump -D -b binary -m i8086 -M intel` an `0x42996–0x42CA6` bestätigt Warnlampen des aktuellen Managers, nicht Bereitschaft je Manager; Zielposition `226 / (33 + 7·i)`. `HotSeatUiTests` behält die Zugwechsel-/Einmal-pro-Tag-Prüfung. Eigener Test mit Trainingskosten nur bei Manager A: Lampe 3 bei A rot, nach Weitergabe an B grün, weiterhin null gespielte Tage. Die Bedeutungsumkehr ist richtig; R6-R01 betrifft ihre Integration mit #133.

**#126:** Direkt an der geprüften `em.exe` bestätigt: `0x2899D cmp byte [bp-0x57],2`, `0x289A1 je 0x289C5`, dort `ax=1`; danach `0x28A34` Vergleich mit 2 und `0x28A3A` Schlüssel `0x7530`. „Im Original unerreichbar“ trifft nicht zu. Die neuen Kombinationstests prüfen 864 Konfigurationen. Die schon in #160 erwähnte falsche Erklärung in `NationalMenu.cs` („ohne Fahne keine Bestenliste“) bleibt bestehen; die Freigabe dieser Schlüsselbewertung ist keine Bestätigung sämtlicher dortiger Kachelsperren.

**Finanz-Abdrücke:** Beide verifizierten Ausgangsstände mit derselben zusätzlichen Trace-Probe ausgeführt, sechs Fälle über je vier Saisonwechsel: synthetisch aktiv/passiv, Seeds 3/11; Originaldaten aktiv/passiv, Verein 42, Seed 3. Nach jedem Schritt vollständige Zustände der vier Zufallsquellen und den unveränderten `RngTwinProbeTests.Dump` verglichen, Geld separat. Über insgesamt **1.426 Zustände einschließlich Startzuständen**: Haupt-, Pokal- und Vertragszufall sowie Dump ohne Kontostand identisch. Alle sechs alten und neuen Referenzabdrücke reproduziert. Erste Kontodifferenz synthetisch Tag 21 (Monatsletzter), Originaldaten Tag 3 **10.006.880 → 10.006.000 DM**, also −880 DM. FinanceRandom unterscheidet sich bereits beim Spielstart durch die geänderten Angebote; die Dokumentation beschreibt den ersten nachfolgenden Schritt. `Mode4_ReferenceValues_Unchanged` auf beiden Ständen bestanden: Titel und abschließende R-/C-Werte gleich, Kontostände geändert. Die Änderungen in `docs/finanzen-rekonstruktion.md` sind damit nachvollziehbar, keine bloße Übernahme neuer Sollwerte.

**Erwartet offen bestätigt:** #167 ist nicht enthalten (`SettleMatchGate` berechnet weiterhin Eintritt ohne Neun-Spieler-Prüfung). #165 Spielwertung, #166 Oberliga-Austausch, #168 Kaderstartwerte, #169 Börsenstart und #170 Nachrichtenleiste bleiben offen; zugehörige GitHub-Einträge und betroffene Codepfade geprüft. Meister-Symbol bleibt wie verlangt `[APPROX]` (Puck statt Originalsymbol 5, #160).

**Akzeptierte `[APPROX]` im Umfang dieses Reviews:** Meister-Symbol und Gesamt-Abschlusstabelle für Top-5-Status; Lampe 1 weiterhin grün bei ungeklärtem Spielerfeld 0x1E, Bundestrainer vor Ernennung ohne vollständigen Kader; Wettbewerbsabbildung, Historienmittel und Spielerwerteinheit der Werbebewertung; unbekannte alte Vertragsbewertung als 0, DM-Rundung statt Drittel-DM; dokumentierte Werbedialog-/Grafikdetails; DEB-Rundenname geladener erster Saison, Europapokal-Datumsbedingung und Penalty-Punkte der WM-Anzeige. Die überholte Ein-Flächen-Annahme von R6-R01 ist ausdrücklich **nicht** akzeptiert.

**Grenzen/Annahmen:** Die 22 dokumentierten Video-Pixelkennzahlen wurden nicht vollständig neu vermessen; ausgeführt wurden die UI-/Pixeltests mit Originalgrafiken und die genannten unabhängigen Proben. Der Trace-Vergleich belegt die erfassten sechs Abläufe, nicht beliebige Seeds. Näherungen bleiben Näherungen; aus grünen Testzahlen allein folgt keine Originaltreue.
