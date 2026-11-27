# Nachprüfung: Finanzen, PR #67

## 1. Merge-Empfehlung

**Nein – der verlangte Gesamtnachweis ist in dieser Umgebung blockiert.** Geprüfter Head:
`9f51e14913f6c27ddc2fa05e842c53333d3a3398`, vorgegebene Base:
`6b8dc0077289837ef89c10d5b732b2a4965ff4a8`. Unabhängige neue ChatGPT-Sitzung ohne Beteiligung an der Umsetzung.
Nachprüfung des [ersten Reviews](2026-10-28-finanzen-2-67.md), bisheriger Prüfstand
`da3e6a4cdad72591bad67fc5f2004bf3cf4a8cfd`.

**F67-01 bis F67-04 sind in den ausgeführten Gegenproben behoben. Keine neuen Codebefunde.** Schema 1 lädt und
spielt weiter; die Berührungen der Speicherdateien und gemeinsamen Dateien sind begründet und minimal.
Release-Build und ergänzende Direktaufrufe der relevanten Tests gelingen. Die regulären drei Testläufe können
wegen der Socket-Sperre der Ausführungssandbox jedoch keine Testergebnisse liefern. Sie sind vor Freigabe auf
genau diesem Head nachzuholen. Das ist ein fehlender Prüfungsnachweis, kein nachgewiesener Defekt des PR.

## 2. Befunde

Keine neuen P0–P3-Codebefunde im geprüften Delta. Erwartet offen laut Anfrage: **–**.

| Alter Befund | Eigene Nachprüfung |
|---|---|
| **F67-01** | Behoben. Verein 24, Level 3, Seed 3, leerer Kader: mit ausgelostem DEB-Pokal Bewertung **4**, minimales Bandenangebot **748 DM**; ohne Pokal Bewertung **8**. Eigene Disassembly bestätigt den mit 2 initialisierten Hilfswert, dessen bedingte Überschreibung bei `[0x5CF3] != 0` und `32 × (Hilfswert − 2)`. Der frühere Bericht hatte die Bedingung nicht berücksichtigt. Mutation zurück zum fehlenden Abzug: Bewertungsfall rot, Fall ohne Pokal weiter grün. |
| **F67-02** | Behoben. Ausschließlich die Monatsbuchung auf `100 * MonthlyBaseIncome(manager)` mutiert: **alle acht Varianten rot**, jeweils an der Sponsoren-Obergrenze in Saison 1. Unveränderter Head: alle acht grün; jeweils Oberliga/Bundesliga, aktiv/passiv, Seeds 3/17/41, vier Saisonwechsel. Beispielsweise Originaldaten/passiv Oberliga: 2.723.100 statt höchstens 92.400 DM; synthetisch/passiv Oberliga: 5.886.400 statt höchstens 92.400 DM. |
| **F67-03** | Behoben. Die unveränderte Methode `Offers_RefreshOnFirstAndMidMonth_NotEveryDay` mit ihren Snapshot-/Datumshelfern isoliert gegen Core von `5f8e02a`: **rot, erwartet 3 Monatsmitten, tatsächlich 0**. Gegen Head grün. Nur Testattribut/Projektumgebung für den Direktaufruf angepasst; keine Assertion oder Produktionsfunktion zurückportiert. |
| **F67-04** | Behoben. Neue Spiele setzen das Konto über `FinanceRules.StartMoney(Level)`; alle sechs Levels unabhängig gegen `400.000 + 50.000 × (5 − Level)` geprüft: **650.000/600.000/550.000/500.000/450.000/400.000 DM**. Eigene Disassembly bei `0x29BD5` bestätigt 150.000 und 1.200.000 Drittel-DM. |

Die Nachprüfung betrifft insbesondere:
[SponsorRating](https://github.com/kaaasi/eishockey-manager-redone/blob/9f51e14913f6c27ddc2fa05e842c53333d3a3398/src/EishockeyManager.Core/FinanceSponsorRating.cs#L54),
[Kontokorridor](https://github.com/kaaasi/eishockey-manager-redone/blob/9f51e14913f6c27ddc2fa05e842c53333d3a3398/tests/EishockeyManager.Core.Tests/FinanceCalibrationTests.cs#L87),
[Monatsmitten-Test](https://github.com/kaaasi/eishockey-manager-redone/blob/9f51e14913f6c27ddc2fa05e842c53333d3a3398/tests/EishockeyManager.Core.Tests/SponsorOfferTests.cs#L122),
[Startkonto](https://github.com/kaaasi/eishockey-manager-redone/blob/9f51e14913f6c27ddc2fa05e842c53333d3a3398/src/EishockeyManager.Core/NewGame.cs#L105).

## 3. Ausgeführte Prüfungen und akzeptierte `[APPROX]`

**Exakter Stand und Rebase.** Git-Netzzugriffe konnten im Freigabeschritt nicht ausgeführt werden. Dateien und
Git-Objekte deshalb über den GitHub-Connector gelesen, lokal rekonstruiert und sämtliche Blob-, Baum- und
Commit-Hashes gegen GitHub geprüft. `git rev-parse HEAD` im unveränderten Prüf-Worktree ergibt den angefragten
Head; `git status --short` ist leer. Keine Änderungen am geprüften Branch oder dessen Code.

```sh
git range-diff 5f8e02a..da3e6a4 6b8dc00..9f51e14
git diff 4e0e104..9f51e14
```

Die ersten sechs alten Commits sind patchgleich (`=`). `da3e6a4` entspricht `4e0e104`; dessen Abweichung im
Range-Diff betrifft den Dokumentkontext in `docs/arbeitspakete.md`. Danach sechs zusätzliche Commits:
`aa9ea44` (Speichern), `f729187` (Pokalterm), `30e8b95` (Startgeld), `ebbb233` (Monatsmitte), `a5e1180`
(Kontokorridor), `9f51e14` (Status). Dieses Delta mit 24 Dateien wurde gelesen. Die Rebase-Integration mit den
inzwischen eingeführten RNG-/Speicherfunktionen wurde anhand der Aufrufer und Zwillingsproben geprüft.

**Originaldateien.** ZIP aus dem privaten Referenz-Repository über den Connector geladen; SHA-256
`142fc78e9b286cb47f36baa450f5af311ac0902525564d0c30ab63697ca321f4` stimmt. Nur lokal entpackt/verlinkt.
Eigene Disassembly mit `objdump -D -b binary -m i8086 --start-address=… --stop-address=… gamedata/em.exe`:
Pokalterm `0x3F833–0x3F913`, Rundenzustand `0x341CC`, `0x346B3`, `0x348DB`, Startkonto `0x29BD5–0x29C02`,
Grundausgaben ab `0x3E819`. Keine erneute vollständige Originalprüfung aller bereits im ersten Review
behandelten Formeln oder Videovermessung; die alten Evidenzgrenzen werden dadurch nicht pauschal aufgehoben.

**Mehrsaison-Sollspanne.** Grundausgaben und Stadionunterhalt werden im Test aus separat notierten Konstanten
berechnet; keine Aufrufe von `MonthlyBaseExpenses`, `MonthlyBaseIncome` oder `Stadium.MonthlyUpkeep` zur
Erzeugung dieser Sollwerte. Sponsorgrenze aus Faktor 5, maximal vier Teamwerten zu je 99, Kadergröße, sieben
Monatsabschlüssen und tatsächlich vorhandenen Vertragssummen; Prämien aus festen Einzelbeträgen. Der Test nutzt
noch die gemeinsame Drittel-DM-Umrechnung, `Player.MaxValue` sowie das Startgeld-Helper; deren Grundwerte werden
durch feste Erwartungen separat geprüft. Die Grenze ist ein breiter Formel-Korridor, keine empirisch vermessene
Original-Kontokurve. Die verlangte 100-fach-Mutation wird zuverlässig erkannt.

**Speichern und Dateigrenzen.**
[SaveGame.Migration1.cs](https://github.com/kaaasi/eishockey-manager-redone/blob/9f51e14913f6c27ddc2fa05e842c53333d3a3398/src/EishockeyManager.Core/SaveGame.Migration1.cs#L15)
ergänzt die neuen Finanzfelder, setzt alte Optionen auf 0 und führt Gastronomie in beiden Finanzbuchperioden
mit Eintritt zusammen. Die übrigen Kategorieindizes rücken korrekt nach; Kontobewegungssummen bleiben erhalten.
`SaveGame.cs` ändert nur Schemaversion und Migrationsregistrierung; `SaveGameTests.cs` nur Versions-/Schemaabdruck.
`RngTwinProbeTests.cs` ändert Referenzabdrücke und Erklärung, nicht den Generator oder die Assertions.
`NewGame.cs` setzt das Level-Startkonto; `GameState.cs` präzisiert nur dessen Dokumentation.
Die Sortierung der Werbeangebote ist eine begrenzte Korrektur für deterministisches Speichern. Entfernen dieser
Sortierung in einer temporären Kopie macht `SponsorOffers_StayOrderedByPartner_AfterRefreshes` rot.

Zusätzlich zum vorhandenen künstlich zurückgebauten Migrationsfall einen **echten Schema-1-Spielstand** mit
Core von Base `6b8dc00` erzeugt: Originaldaten, Verein 24, Seed 3, 40 `Continue()`-Schritte, angenommener
Trikotvertrag, Bundestrainer und Gastronomiebuchung 1.234 DM. Auf Head geladen: Konto **1.035.824 DM** und
Finanzbuchsumme **635.824 DM** unverändert, Nationalkonto 0. Über `Continue()` bis Saisonende, dann
`StartNextSeason()` weitergespielt: Saison 1994, Konto **874.326 DM**; erneutes Sichern/Laden erfolgreich.

**Build und reguläre Prüfung.** SDK 10.0.402. Vollständiger Release-Build, keine Warnungen/Fehler:

```sh
dotnet build EishockeyManager.slnx -c Release -warnaserror -nologo -v q \
  -m:1 -p:UseSharedCompilation=false -p:NuGetAudit=false
tools/pruefen.sh
```

Der Build benötigt `-m:1`/keinen Compiler-Server wegen gesperrter lokaler Sockets und `NuGetAudit=false` wegen
des blockierten NuGet-Netzzugriffs. **Das unveränderte `tools/pruefen.sh` scheitert bereits beim Build**; es
liefert keine Ergebnistabelle. Auch separate Versuche der drei `dotnet test … --no-build`-Läufe nach dem
erfolgreichen angepassten Build brechen vor Testausführung ab (`SocketException (13): Permission denied`,
`SocketServer.Start`). Der Pflichtlauf scheitert damit nicht nachweislich aus dem verlangten Grund.

| Regulärer Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | – | – | –; Testhost blockiert |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | – | – | –; Testhost blockiert |
| Pflichtlauf ohne Daten (beide Variablen) | – | – | –; kein gültiger Pflichtlauf |

**Ergänzende Direktaufrufe, ausdrücklich kein Ersatz für obige Tabelle.** Temporäres Konsolenprojekt außerhalb
des Repositories referenziert das unveränderte Core-Testprojekt. Methoden samt Inline-/MemberData direkt per
Reflection aufgerufen, `ITestOutputHelper` bereitgestellt und optionale Parameter berücksichtigt; xUnit-Assertions
unverändert. Damit wird kein Testhost-Socket benötigt. Auswahl:

```text
SponsorOfferTests | StartMoney | FinanceCalibrationTests.FourSeasonChanges |
FinanceSaveGameTests | SaveGameTests | RngTwinProbeTests
```

Mit Originaldaten: **64 bestanden, 0 fehlgeschlagen, 0 übersprungen**; ohne Originaldaten: **56 bestanden,
0 fehlgeschlagen, 8 übersprungen**. Im Datenlauf enthalten: acht Finanz-Mehrsaisonfälle,
sechs RNG-Mehrsaisonabdrücke, Schema-/Feldabdeckung, Roundtrips und sämtliche Speicher-Zwillingsfälle. Separate
temporäre Kopien für die 100-fach-, Pokalterm- und Sortierungs-Mutationen; Befehlsmuster
`dotnet <temporäres Konsolenprojekt>/bin/Release/net10.0/harness.dll <Methodenfilter>`.
Die PR-Zahl „614“ wird nicht als eigener vollständiger Testnachweis übernommen.

**Akzeptierte `[APPROX]`:** Zeitpunkt der DEB-Pokal-Auslosung im Remake und Zuordnung der Titelzähler;
Migration alter Nationalkonten auf 0 sowie Umbuchung alter Gastronomieerlöse zu Eintritt. Die schon im ersten
Review benannten sonstigen Näherungen bleiben als solche bestehen; keine neue Originaltreuebehauptung dazu.

**Annahmen und offene Nachweise:** Drei reguläre Skriptläufe auf genau dem Prüf-Head fehlen. Die Startgeld- und
Pokalterm-Gegenproben sowie der reale Schema-1-Lauf sind eigene Ergebnisse; die Gesamtfreigabe folgt daraus
ohne die verlangte Prüfung nicht. `main` war beim Abschluss bereits weiter als die vorgegebene Base; ein
späterer Rebase des PR benötigt erneut die Prüfung seines Deltas und der Integration.
