# Nachprüfung: Integration Runde 3, Welle A (#54)

## 1. Merge-Empfehlung

**Ja: #54 ist am Head `3e44a8412c14a3dcf0addc653c224f48f99c8e85` mergebereit. R3A-01, R3A-02 und R3A-03 sind behoben.** Geprüfter Head: `3e44a8412c14a3dcf0addc653c224f48f99c8e85`, aktueller main/Base: `c84cafe20633a5b2c2d4e8b9d530df537c40e131`. Nachprüfung der Reviews [Integration Runde 3a](2026-10-28-integration-runde-3a.md) und [Nachprüfung](2026-10-28-nachpruefung.md), nur Delta `c622b0f..3e44a84` und R3A-01/02/03. Keine Mitarbeit an der Implementierung.

Die PR-Beschreibung enthält einen ausdrücklich dem neuen vollständigen Head zugeordneten Nachtrag der Leitstelle: Release-Build mit `-warnaserror`, 0 Warnungen, und `tools/pruefen.sh` mit Originaldaten **472/0/0**, ohne Daten **394/0/66**, Pflichtlauf **394/78/0**. Dieser Nachtrag gilt als externer Originaldaten-Nachweis nach Zusammenarbeit §6.4; er wird nicht als eigener Lauf des Reviewers ausgegeben. Im Gegensatz zum vorigen Nachtrag werden die gemeinsamen Läufe jetzt als ausgeführt dokumentiert. Roh-TRX wurden nicht bereitgestellt; der positive Lauf wird hier nicht unabhängig nachgezählt. Die Entscheidung beruht zusätzlich auf den eigenen Gegenproben und Anwendungsläufen, nicht auf der Testanzahl.

## 2. Befunde

**R3A-01 · bisher P2 · behoben.** [GameDataFact.cs:29–35](https://github.com/kaaasi/eishockey-manager-redone/blob/3e44a8412c14a3dcf0addc653c224f48f99c8e85/tests/EishockeyManager.UI.Tests/Support/GameDataFact.cs#L29-L35) berücksichtigt `EM_SKIP_GAMEDATA=1` vor dem Locator. Der unveränderte `TestGameData`-Klassenkörper wurde mit der echten Formats-Bibliothek isoliert ausgeführt, je Modus in einem frischen Prozess. Fixture: ausschließlich ein leeres `gamedata/kb`, keine Originaldateien. Alter Helfer aus `c622b0f` trotz `EM_SKIP_GAMEDATA=1`: **Available=true**; neuer Helfer mit demselben Ordner: **Available=false**. Ohne Skip liefert der neue Helfer wieder **true**. Mit Skip und `EM_REQUIRE_GAMEDATA=1` bleibt **Required=true**, `Load()` wirft den erwarteten Fehler „Originaldaten fehlen“. Damit wird der Ordner tatsächlich ignoriert; die Probe simuliert kein positives Laden echter Originaldateien.

**R3A-02 · bisher P3 / #61 · behoben.** [DebCupTests.cs:126](https://github.com/kaaasi/eishockey-manager-redone/blob/3e44a8412c14a3dcf0addc653c224f48f99c8e85/tests/EishockeyManager.Core.Tests/DebCupTests.cs#L126) erwartet Spiel 1/3 beim ersten und Spiel 2 beim zweiten Teilnehmer unabhängig von `CupTie.Pairing`. Der neue [direkte Regressionstest:12–19](https://github.com/kaaasi/eishockey-manager-redone/blob/3e44a8412c14a3dcf0addc653c224f48f99c8e85/tests/EishockeyManager.Core.Tests/IntegrationRound3ReviewTests.cs#L12-L19) prüft die drei Paarungen mit festen Teilnehmern 7 und 11. Beide Tests am unveränderten neuen Head grün. Mutation ausschließlich in temporärem Worktree: `Cups.cs`, `game % 2 == 0` → `true`. **Beide Tests werden rot (0 bestanden, 2 fehlgeschlagen)**, jeweils durch Wertassertionen statt Buildfehler: direkter Test erwartet `(11, 7)`, erhält `(7, 11)`; Saison-/Kalendertest erwartet `(2, 12)`, erhält `(12, 2)`. Damit überlebt die Mutation aus dem Ausgangsreview nicht mehr.

**R3A-03 · bisher P3 / #62 · behoben.** [CupsNational.GameState.cs:59–71](https://github.com/kaaasi/eishockey-manager-redone/blob/3e44a8412c14a3dcf0addc653c224f48f99c8e85/src/EishockeyManager.Core/CupsNational.GameState.cs#L59-L71) gibt beim selben Manager das bestehende Trainerobjekt zurück, bevor ein neues Objekt angelegt oder Etat gebucht wird. Ein anderer Manager wird bei bestehendem Amt abgelehnt. Der [neue Regressionstest:24–40](https://github.com/kaaasi/eishockey-manager-redone/blob/3e44a8412c14a3dcf0addc653c224f48f99c8e85/tests/EishockeyManager.Core.Tests/IntegrationRound3ReviewTests.cs#L24-L40) nominiert zunächst einen nichtleeren Kader und prüft bei der zweiten Ernennung Objektidentität, unveränderten Kontostand und unveränderte Kaderfolge. Mutation durch Rücknahme ausschließlich des neuen Schutzblocks: **0 bestanden, 1 fehlgeschlagen**, `Assert.Same` erkennt das ersetzte Trainerobjekt; die Fehlerausgabe zeigt den zuvor gefüllten und danach leeren Kader. Am unveränderten Head besteht der Test alle drei Assertions. Der ursprüngliche Fehler ist damit reproduziert und die Regression wirksam. `StartNationalSeason()` beendet das Amt weiterhin, sodass die Sperre nicht über Saisonwechsel hinweg bestehen bleibt.

**Keine neuen P0–P3-Befunde im geprüften Delta.** Die bereits dokumentierten Grenzen (Gehaltspauschale bei leerem Kader, Etat unter „Prämien“, Finanzbalance ohne Endabnahme) sind durch dieses Delta unverändert; kein neuer Originaltreue- oder Balance-Abschluss.

## 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

Review-Vorlage, AGENTS und Zusammenarbeit §§5/6 aus dem Prozessstand gelten unverändert. Vollständige Heads lokal und über GitHub verifiziert. `6d3b748192366017be724175a560a6f244cb5aa5` ist der Merge mit Eltern `c622b0f` und aktuellem main `c84cafe`; der folgende Commit ist `3e44a84`. Anwendungscode/-tests ändern sich durch den main-Merge ausschließlich am gemeinsamen Datenhelfer; danach nur Ernennungsschutz, unabhängige Heimrechterwartung und zwei Regressionstests. Die übernommenen Prüfsystem-Dateien sind identisch mit dem bereits freigegebenen #50-Head `ad26a99`; keine erneute Vollprüfung aller acht Einzel-PRs.

```bash
git diff c622b0f..3e44a84 --stat
git diff 3e44a84^..3e44a84
git merge-base --is-ancestor origin/main 3e44a84
dotnet test tests/EishockeyManager.Core.Tests -c Release -nologo \
  --filter 'FullyQualifiedName~IntegrationRound3ReviewTests|FullyQualifiedName~EveryTie_IsBestOfThree_WithAlternatingHomeRight_OnCalendarDays'
```

Mutationsbefehle: derselbe `dotnet test`-Aufruf mit Filter `FullyQualifiedName~R3A02_DebCupHomeRight|FullyQualifiedName~EveryTie_IsBestOfThree_WithAlternatingHomeRight_OnCalendarDays` nach genau der Heimrecht-Mutation, anschließend `FullyQualifiedName~R3A03_SecondAppointment` nach genau der Rücknahme des Ernennungsschutzes.

Gezielte unveränderte Head-Prüfung: **3 bestanden, 0 fehlgeschlagen, 0 übersprungen**. Locator-Probe: eigenständiges .NET-Programm mit unverändertem Klassenkörper und Referenz auf die echte Formats-Bibliothek, frische Prozesse für alt/neu und Skip/Require-Modi. Keine echte Originaldatei erzeugt oder kopiert.

Mutationen und zusätzliche Proben liegen ausschließlich in temporären Kopien. Der für den vollständigen Anwendungslauf verwendete Worktree bleibt unverändert. SDK 10.0.402; Aufruf wie in den vorigen Nachprüfungen:

```bash
XDG_DATA_HOME=/workspace/xdg PATH=/workspace/sdk:$PATH \
DOTNET_CLI_HOME=/workspace/dotnet-home NUGET_PACKAGES=/workspace/nuget \
DOTNET_CLI_TELEMETRY_OPTOUT=1 bash tools/pruefen.sh
```

| Lauf am exakten Head | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten, externer Nachweis der Leitstelle | 472 | 0 | 0 |
| Ohne Originaldaten, eigener Lauf | 394 | 0 | 66 |
| Pflichtlauf ohne Daten, eigener Lauf | 394 | 78 | 0 |

Eigener positiver Originaldatenlauf mangels Originaldateien nicht ausführbar. Eigener Gesamtlauf Exit 0, Release-Build mit Warnungen als Fehler erfolgreich; alle Pflichtfehler wurden vom Prüfsystem als fehlende Originaldaten bestätigt. Die Tabelle trennt den externen positiven Lauf ausdrücklich von den eigenen Läufen. Zusätzlich `bash tools/pruefen-selbsttest.sh`: alle sechs Fälle korrekt, Exit 0. Mutationen anschließend wiederhergestellt; der unveränderte Anwendungs-Worktree blieb durchgehend sauber. CI `build` auf genau `3e44a84` erfolgreich; `publish` erwartungsgemäß übersprungen, [Run 40691919149](https://github.com/kaaasi/eishockey-manager-redone/actions/runs/40691919149). Aktueller main ist bereits Vorfahr des geprüften Heads, keine offene Zusammenführung.

**Akzeptierte [APPROX]:** keine neuen fachlichen Näherungen im geprüften Delta; die ausdrücklich akzeptierten Näherungen aus dem Ausgangsreview gelten unverändert. **Annahmen/Grenzen:** Positiver Originaldatenlauf als externer Head-Nachweis; keine eigene Rohdaten-Nachzählung, Pixelabnahme oder DOS-Balanceprüfung. Die Locator-Fixture prüft nur die Schalterwirkung, nicht den Inhalt echter Spieldaten.
