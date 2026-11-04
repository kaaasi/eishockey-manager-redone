# Unabhängiges Gesamtreview Runde 11

Base `7332a0f` · Head `fd55b89e311a9762d2079df4c624de380d30b445` · Integrationen #452, #460, #462, #465, #466, #467, #472. Reviewer: ChatGPT, neue Sitzung ohne Implementierungsbeteiligung.

## 1. Merge-Empfehlung

**Vorläufig nein für `fd55b89`:** Die Gesamtprüfung ist nicht vollständig abgeschlossen. Die genannten Gegenbeispiele und die verifizierten Stichproben sind unten dokumentiert; daraus allein folgt keine Freigabe des gesamten Integrationsbereichs. Offene, ausdrücklich zurückgestellte Punkte benötigen die bereits vorgesehene Entscheidung beziehungsweise Runde 12.

## 2. Befunde

In den unten genannten Stichproben kein neuer reproduzierter Produktfehler. Das ist keine Aussage über alle 235 geänderten Dateien. Die angeforderten bereits offenen Punkte sind nicht als neue Befunde gezählt:

- WS-02: Der Ausnahmezweig der Finanzprobe ist auf Seed 17 beschränkt und verlangt mindestens zwei vollständige Seeds; die Entscheidung zum Ausnahmefall bleibt offen. WS-04 (Grenze der Neuzählung), U8, A184 und A174 bleiben als Lücken erkennbar.
- SW-02/05/06 bleiben laut den Saisonwechsel-Reviews offen; R11T3-01/02 sind am Head weiterhin sichtbar: die Adressangabe in [`FinanceStadium.cs:359`](https://github.com/kaaasi/eishockey-manager-redone/blob/fd55b89e311a9762d2079df4c624de380d30b445/src/EishockeyManager.Core/FinanceStadium.cs#L359) und der Widerspruch zwischen Standvermerk und F066/F067 in [`inventur.md:742`](https://github.com/kaaasi/eishockey-manager-redone/blob/fd55b89e311a9762d2079df4c624de380d30b445/docs/analyse/inventur.md#L742).
- UM-2/UM-4: Die IDs ließen sich im geprüften Repository nicht zuordnen; ihr Status wurde nicht unabhängig bestätigt.

Die nachgeführten Proben sind begrenzt aussagekräftig: [`TestSquadKeeper.cs:40`](https://github.com/kaaasi/eishockey-manager-redone/blob/fd55b89e311a9762d2079df4c624de380d30b445/tests/EishockeyManager.Core.Tests/TestSquadKeeper.cs#L40) verschiebt bei leerem Markt Spieler ohne Spieltransaktion. Solche Mehrsaisonproben belegen daher nicht die Verfügbarkeit regulär verpflichtbarer Spieler oder unverfälschte Computer-Kader. Die Finanzprobe prüft Konto und Buch getrennt von der neutralen Oberliga-Setzung; der Ausnahmeweg für Seed 17 bleibt die bekannte WS-02-Entscheidung. [`StadiumAusbauTests.cs:247`](https://github.com/kaaasi/eishockey-manager-redone/blob/fd55b89e311a9762d2079df4c624de380d30b445/tests/EishockeyManager.Core.Tests/StadiumAusbauTests.cs#L247) prüft die Oberliga-Setzung vor Übernahme der Differenz, sodass die frühere WS-05-Schwäche dort nicht mehr vorliegt. Gegenprobe in einer Kopie: `manager.Money = OberligaKeptMoney + 1_000` macht die Stadionprobe aktiv und passiv sowie die synthetische Finanzprobe Oberliga aktiv und passiv rot. Daraus folgt kein Nachweis, dass andere Änderungen keine Fehler verdecken.

## 3. Ausgeführte Prüfungen und akzeptierte `[APPROX]`

Der Checkout wurde aus `c3eb337` und acht über den GitHub-Connector geladenen Dateien rekonstruiert. `git add -A && git write-tree` ergab `c699bb0ca18a20f239a3eac6b126e2d6a22fb513`, identisch mit dem Tree des Head-Commits. Das ist ein verifizierter vollständiger Tree einschließlich Dateimodi; der lokale `HEAD` blieb `c3eb337`. SDK `10.0.402`. Originaldaten aus dem privaten Referenz-Repo: alle 166 Spieldateien und 14 DOS-Spielstände anhand ihrer Git-Blob-Hashes geprüft; Originaldateien nicht eingecheckt. Standardaufrufe mit `NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=1`; Testhost-Sockets außerhalb der Sandbox.

Fünf Gegenbeispiele: `dotnet test` mit Filter auf die folgenden Methoden am verifizierten Tree (synthetische Daten): alle fünf grün. Jeweils danach die genannte Mutation in einer getrennten Kopie: jeweils genau der gefilterte Test rot.

| Spur | Test | Mutation |
|---|---|---|
| Geld | `Bank_CreditUpToTwoMillion` | Kreditlimit 2 Mio. → 1 Mio. |
| Saisonwechsel | `ForcedFromSecondLeague_PromotedInRelegationRound_StaysUp_PartnerDropsTwoLeagues` | beim Doppelwechsel `oberliga[at] = up[k]` statt Partner |
| Kader | `Offer_NeverWithTwoYearsLeft_WholeSquad` | Prüfung `ContractYears != 1` entfernen |
| Wettbewerbe | `Migration_31To32_KeepsHandMadeLineup` | Migration setzt `_autoLineup` immer auf `true` |
| UI | `MessageWithFourthLine_LeftButtonIsLineFive` | linke Beschriftung auch bei Meldung aus Zeile 4 |

`StadiumAusbauMigrationTests.Schema22_OldConstruction_IsMigrated`: vier Fälle grün; der Loader durchläuft dabei die Kette bis Schema 32. Die Probe erzeugt einen künstlich zurückgesetzten Stand und prüft insbesondere die Baustelle. Zusätzlich ist `Migration_RealSchema19Save_LoadsAndPlaysOn` mit einem tatsächlich mit Schema 19 erzeugten synthetischen Stand grün: Migration bis 32 und ein Saisonwechsel; die Probe setzt das Konto dabei mindestens auf 10 Mio. DM. Ein tatsächlicher Schema-22-Stand und eine mehrjährige Fortsetzung nach dieser Migration wurden nicht geprüft.

`tools/pruefen.sh` wurde zunächst ohne Daten ausgeführt; anschließend mit vollständigen Daten und `EM_SAVEGAMES=/tmp/spielstaende` als `tools/pruefen.sh --schnell` wiederholt. Beide verwendeten denselben oben verifizierten Tree. Der zuerst mit unvollständigen Spielständen gestartete Originallauf wurde abgebrochen und zählt nicht als Ergebnis. Das Skript meldet wegen der Rekonstruktion den alten lokalen Commit und gestagte Änderungen; der Tree-Nachweis oben ist für den geprüften Inhalt maßgeblich.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten | 1982 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1670 | 0 | 280 |
| Pflichtlauf ohne Daten (wie gewollt wegen fehlender Originaldaten) | 1671 | 311 | 0 |

Beide abgeschlossenen Skriptaufrufe endeten mit Exit 0; der Pflichtlauf wurde vom Skript auf den Fehlergrund geprüft.

Originalstichproben: `objdump -D -b binary -mi8086 -M intel` auf der hashgeprüften `em.exe`. Bei Datei `0xC037` tauschen `cmp di,si` und der folgende Zweig die Grenzen; `FinanceStadium.Rnd` bildet dies ab. Bei `0x317D3`/`0x31800` lösen mehr als zwei Siege die Markierung 100 aus, passend zu `PlayoffSeries.WinsNeeded = 3`. Bei `0x3DDB8`/`0x3DDC1` ist der Eintrittspreis auf 10–30 begrenzt. Diese Stichproben belegen nicht die gesamten umgebenden Abläufe.

Akzeptierte `[APPROX]`: Spiel 1 der Play-off-Serie beim besser Platzierten und die Halbfinalpaarung sind ausdrücklich als ungesichert markiert; die tatsächlichen Originalpaarungen wurden nicht selbst verifiziert. Annahmen: Insbesondere die Originaltreue des Doppelwechsels und die Spielbarkeit eines vollständig historischen Schema-22-Stands wurden nicht durch DOSBox beziehungsweise einen solchen Spielstand belegt.
