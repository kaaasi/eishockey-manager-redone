# Gesamtreview Runde 14 – Teil 4 (in Arbeit)

## 1. Merge-Empfehlung

**Nein**, solange die geforderte Vollprüfung nicht abgeschlossen ist. Base `401efa7`, Head `b13046422b285404ad6ddbe7b5f1b18e22069eea`. Fortsetzung der Berichte #704, #705 und #706.

## 2. Befunde

Die in den Vorberichten dokumentierten Befunde bestehen fort. Die als R14G3-03 festgehaltene Testlücke ist erneut reproduziert: H10 senkt die Mindestlänge von `Length` auf `Length - 1`; der vorhandene Test bleibt grün, da der zu kurze Eingabepuffer zusätzlich eine falsche Kennung besitzt.

## 3. Prüfungen und offene Prüfbereiche

Die Prüfkopie liegt unter `/workspace/r14-review-teil4/copy`; Ausgangsstand ist der unveränderte lokale Head. Mutationen laufen einzeln mit Wiederherstellung, die vollständigen Ausgaben und Regeln werden unter `/workspace/r14-review-teil4/logs` gespeichert. Beim ersten abgebrochenen Versuch wurde eine überlagerte Mutation erkannt; diese Ergebnisse wurden verworfen und anschließend nach Wiederherstellung der Quelldatei erneut ausgeführt. .NET SDK 10.0.402, lokale Testhost-Sockets mit Umgebungsfreigabe, `NuGetAudit=false`.

Die unveränderten Kontrollläufe bestehen: `OriginalSaveHeaderTests` 4/4, `SaveLoadDialogOriginalTests` 16/16 und `NationalteamRunde14Teil4Tests` 18/18.

### Selbst nachgelesene Originalstellen

`em.exe` Datei `0x5B68E–0x5B732`: Der Code übergibt nacheinander die Längen 10, 4, 1, 1 und `0xA0` an denselben Leser; nach den ersten 10 Bytes erfolgt ein Vergleich gegen eine Zeichenkette bei DGROUP `0x55DF`. Damit ist die Struktur des Kopflesers im Originalcode nachvollzogen; die Bedeutung jeder Hilfsfunktion und das Verhalten bei verkürzter Datei bleiben gesondert offen. Selbst aus `FILE46.HOK` gelesen: Kennung `KRON_0.99` mit Nullbyte, Jahr 1994, Managerzahl 1, Spieltag 1, erster Eintrag Liga 0, WM-Flag 0, Name `Joerg` und Vereinsbytes `KLNER EC` (Originalkodierung).

Selbst in `em.exe` nachgelesen: `0x319AC–0x319DB` kopiert in einer Schleife die Plätze `i` und `7 − i` von `0x5CCB` nach `0x5D3B + 2i` und `0x5D3C + 2i`, für vier Werte von `i`. Das bestätigt die Viertelfinalfolge 1–8, 2–7, 3–6, 4–5. Die anschließende Halbfinaltabelle wurde bereits in Teil 2 unabhängig gelesen. S01 setzt die alte Paarungsregel zurück; beide Originalstände FILE44/FILE22 und der Mehrsaisontest werden rot.

Zur Nutzerentscheidung C wurde `em.exe` Datei `0x54D41–0x54D65` erneut gelesen: Der Tagesindex adressiert ein Tagesbyte, Wert 9 wird auf 0 abgebildet, 0 und 255 springen nach `0x55073`. `0x55204–0x55230` erhöht DGROUP `0x3B7` mit Übertrag und ruft nach Setzen von Manager 0 zwei Funktionen je Manager auf. Die Verknüpfung des gesamten Ladewegs und die Zuordnung aller aufgerufenen Funktionen bleiben offen; isolierte Befehle belegen sie nicht allein.

### Einzelne Kopfleser-Sollwerte

Der neue Test `TryRead_ReturnsYearMatchDayAndManagers` hat bei unverändertem Code bestanden. Die folgenden Werte wurden jeweils unabhängig durch eine Änderung der lesenden Regel rot gemacht; Einzelprotokolle und Regeländerungen stehen in der Mutationstabelle.

| Sollwert | Mutation | Ergebnis |
|---|---|---|
| Jahr 1994 | H01 | rot |
| Spieltag 7 | H05 | rot |
| Managerzahl 2 | H11 | rot |
| Manager 0: Liga 0 | H12 | rot |
| Manager 0: WM-Flag false | H13 | rot |
| Manager 0: Name Anna | H14 | rot |
| Manager 0: Verein KOELNER EC | H15 | rot |
| Manager 1: Liga 2 | H16 | rot |
| Manager 1: WM-Flag true | H17 | rot |
| Manager 1: Name Bernd | H18 | rot |
| Manager 1: Verein EV LANDSHUT | H19 | rot |

### Zusätzliche Bildschirmkontrollen

In einer Prüfkopie wurde der Vergleich vor den Fabrikaufruf `NationalMenu.Open` verschoben, auch für Kacheln, die `null` liefern; Öffnen, Zeichnen und drei eingabelose Frames bestanden (1/1). Für `SeasonEndScreen` wurde nach Erreichen eines gültigen Saisonendzustands der Spielstand vor Konstruktion, `Host.Show`, Zeichnen und drei eingabelosen Frames gesichert und danach verglichen (1/1 bestanden). Die Testlücken R14G2-01 und R14G2-02 im unveränderten Testsatz bleiben bestehen; diese zusätzlichen Kontrollen belegen nur die ausgeführten Zustände.

### Entscheidungen: ergänzende Prüfung

| Entscheidung | Geprüfter Code und Status |
|---|---|
| R14-D2 = B, HOK-Import jetzt, Export später | `SaveLoadScreen` ruft `OriginalImport.TryImport` beim Laden auf; HOK-Einträge werden beim Speichern grau angezeigt (`SaveLoadScreen.cs:329,453,472`). Feldkarte und vollständige Ausschlussprüfung sämtlicher Exportpfade bleiben offen. |
| E14-6 = A, zusätzlicher Lauf Speicherdialog | `docs/arbeitspakete.md:174` ordnet A338/A339 und den zusätzlichen Lauf `hok-import` Teil 5 zu; der Dialogcode ist vorhanden, und 16/16 Kontrollfälle bestehen. Vollständiger Original- und Ereignisabgleich bleibt offen. |

Die Entscheidungsliste und die übrigen Einzelergebnisse aus Teil 1–3 gelten weiter; nicht aufgeführte Entscheidungen sind damit nicht als abgeschlossen bewertet.

### Ergebnisse je Mutation und Theoriezeile

| Mutation | Regeländerung | Ausgeführter Test beziehungsweise Datenzeile | Ergebnis |
|---|---|---|---|
| H01 | Jahr um eins erhöhen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H02 | vollen Kopf ablehnen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReadsOnlyTheHeader_NoLengthOrChecksumCheck` | rot |
| H03 | maximale Managerzahl auf drei setzen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_LimitsManagersToFour` | rot |
| H04 | Kennung nicht vergleichen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_WrongMagicOrTooShort_ReturnsNull` | rot |
| H05 | Spieltag um eins erhöhen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H06 | Ligafeld erstes Managers ändern | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H07 | WM-Flag invertieren | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H08 | Name aus Vereinsfeld lesen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H09 | Verein aus Namensfeld lesen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H10 | Mindestlänge um eins verkürzen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_WrongMagicOrTooShort_ReturnsNull` | grün |
| H11 | Managerzahl auf eins begrenzen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H12 | Manager 0: Liga um eins erhöhen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H13 | Manager 0: WM-Flag invertieren | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H14 | Manager 0: Namen vertauschen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H15 | Manager 0: Verein vertauschen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H16 | Manager 1: Liga um eins erhöhen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H17 | Manager 1: WM-Flag invertieren | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H18 | Manager 1: Namen vertauschen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| H19 | Manager 1: Verein vertauschen | `EishockeyManager.Formats.Tests.OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | rot |
| I01 | Aktienkurse nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_StocksFromOriginal` | rot |
| I02 | Aktienbesitz nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_StockHoldings` | rot |
| I03 | Trainingsplan nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_TrainingPlan(file: "FILE09.HOK")` | grün |
| I03 | Trainingsplan nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_TrainingPlan(file: "FILE08.HOK")` | rot |
| I04 | Darlehen nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_BankAndManagerLoans` | rot |
| I05 | Werbung nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_Sponsoring` | rot |
| I06 | Vereinstaktik nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_TacticModes` | rot |
| I06 | Vereinstaktik nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_TacticsAndAutoLineup` | rot |
| I07 | Nachrichtenanzeige immer falsch | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_NewMessageFlag(file: "FILE05.HOK", expected: False)` | grün |
| I07 | Nachrichtenanzeige immer falsch | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_NewMessageFlag(file: "FILE20.HOK", expected: True)` | rot |
| I08 | Nachrichtenanzeige immer wahr | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_NewMessageFlag(file: "FILE05.HOK", expected: False)` | rot |
| I08 | Nachrichtenanzeige immer wahr | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_NewMessageFlag(file: "FILE20.HOK", expected: True)` | grün |
| I09 | Platzhalbbytes vertauschen | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_RankSlotsMatchTable` | rot |
| I10 | Ranghistorie nicht zuweisen | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_RankSlotsMatchTable` | grün |
| I11 | Unterzahl- und Überzahlspiele löschen | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_SeriesRecordsAndTableFields` | rot |
| I12 | Trainingseinheit um eins verschieben | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_TrainingPlan(file: "FILE09.HOK")` | rot |
| I12 | Trainingseinheit um eins verschieben | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_TrainingPlan(file: "FILE08.HOK")` | rot |
| I13 | Nationalkonto nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportRestTests.Import_NationalAccountAndTalentSearch` | rot |
| P01 | Poolalter um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportPoolTests.Import_PoolFromOriginal` | rot |
| P02 | Marktpreis durch zwei statt drei | `EishockeyManager.Core.Tests.OriginalImportPoolTests.Import_MarketFromOriginal` | rot |
| P03 | Verkäufer nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportPoolTests.Import_MarketListingOfManager` | rot |
| P04 | doppelte Poolspieler stehenlassen | `EishockeyManager.Core.Tests.OriginalImportPoolTests.Import_PoolNamesUnique_AndContinue(file: "FILE05.HOK")` | rot |
| P04 | doppelte Poolspieler stehenlassen | `EishockeyManager.Core.Tests.OriginalImportPoolTests.Import_PoolNamesUnique_AndContinue(file: "FILE41.HOK")` | rot |
| P04 | doppelte Poolspieler stehenlassen | `EishockeyManager.Core.Tests.OriginalImportPoolTests.Import_PoolNamesUnique_AndContinue(file: "FILE36.HOK")` | rot |
| P05 | Nationalkader rückwärts einlesen | `EishockeyManager.Core.Tests.OriginalImportPoolTests.Import_NationalSquadFromOriginal` | rot |
| P06 | Nachrichten nicht importieren | `EishockeyManager.Core.Tests.OriginalImportPoolTests.Import_MessagesFromOriginal` | rot |
| P07 | Spielerverweis in Nachricht auslassen | `EishockeyManager.Core.Tests.OriginalImportPoolTests.Import_MessageAboutPoolPlayer` | rot |
| P08 | Preis erster Liste nach Laden erhöhen | `EishockeyManager.Core.Tests.OriginalImportPoolTests.Import_RoundTripWithPoolMarketNational(file: "FILE20.HOK")` | rot |
| P08 | Preis erster Liste nach Laden erhöhen | `EishockeyManager.Core.Tests.OriginalImportPoolTests.Import_RoundTripWithPoolMarketNational(file: "FILE18.HOK")` | rot |
| P08 | Preis erster Liste nach Laden erhöhen | `EishockeyManager.Core.Tests.OriginalImportPoolTests.Import_RoundTripWithPoolMarketNational(file: "FILE41.HOK")` | rot |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Playoffs_OwnRounds_PairLikeTheOriginal(file: "FILE44.HOK")` | rot |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Playoffs_OwnRounds_PairLikeTheOriginal(file: "FILE22.HOK")` | rot |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.MultiSeason_FourChanges_PlayoffsPairByTable0x144` | rot |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_RunningQuarterFinal_SwapsAtOddGameCount(played: 2)` | grün |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_RunningQuarterFinal_SwapsAtOddGameCount(played: 4)` | grün |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_RunningQuarterFinal_SwapsAtOddGameCount(played: 3)` | grün |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_RunningQuarterFinal_SwapsAtOddGameCount(played: 1)` | grün |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_CarriesPerCompetition_AllStands` | grün |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_OwnListedPlayer_OnlyOnMarket(file: "FILE26.HOK", name: "HOCK")` | grün |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_OwnListedPlayer_OnlyOnMarket(file: "FILE41.HOK", name: "HOHENADL")` | grün |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Load_Schema43_RunningQuarterFinal_SemifinalByTable0x144` | rot |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_RelegationGroup_ThreeOfSixDates` | grün |
| S01 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Playoffs_NextRound_PairsByTable0x144_HomeAtSmallerSlot` | rot |
| S02 | gerade und ungerade Spielzahlen vertauschen | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_RunningQuarterFinal_SwapsAtOddGameCount(played: 2)` | rot |
| S02 | gerade und ungerade Spielzahlen vertauschen | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_RunningQuarterFinal_SwapsAtOddGameCount(played: 4)` | rot |
| S02 | gerade und ungerade Spielzahlen vertauschen | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_RunningQuarterFinal_SwapsAtOddGameCount(played: 3)` | rot |
| S02 | gerade und ungerade Spielzahlen vertauschen | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_RunningQuarterFinal_SwapsAtOddGameCount(played: 1)` | rot |
| S03 | einen Gruppentermin zusätzlich annehmen | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_RelegationGroup_ThreeOfSixDates` | rot |
| S04 | Pokalspielübertrag weglassen | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Import_CarriesPerCompetition_AllStands` | rot |
| S05 | alte Reihenfolge nicht sortieren | `EishockeyManager.Core.Tests.PlayoffSemifinalTests.Load_Schema43_RunningQuarterFinal_SemifinalByTable0x144` | rot |
| U01 | Blätterpfeil nicht auf Loslassen warten | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Paging_OnlyThroughArrowPair_InStepsOfFortyEight_NoEffectAtTheEnds` | rot |
| U02 | Spaltenwechsel bei Grenzwert vorziehen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 202, column: 1)` | rot |
| U02 | Spaltenwechsel bei Grenzwert vorziehen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 123, column: 1)` | grün |
| U02 | Spaltenwechsel bei Grenzwert vorziehen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 265, column: 2)` | grün |
| U02 | Spaltenwechsel bei Grenzwert vorziehen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 122, column: 0)` | rot |
| U02 | Spaltenwechsel bei Grenzwert vorziehen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 203, column: 2)` | grün |
| U02 | Spaltenwechsel bei Grenzwert vorziehen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 43, column: 0)` | grün |
| U03 | Spaltenschwelle um eins erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 202, column: 1)` | grün |
| U03 | Spaltenschwelle um eins erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 123, column: 1)` | rot |
| U03 | Spaltenschwelle um eins erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 265, column: 2)` | grün |
| U03 | Spaltenschwelle um eins erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 122, column: 0)` | grün |
| U03 | Spaltenschwelle um eins erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 203, column: 2)` | rot |
| U03 | Spaltenschwelle um eins erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 43, column: 0)` | grün |
| U04 | linke Treffergrenze um eins erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 202, column: 1)` | grün |
| U04 | linke Treffergrenze um eins erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 123, column: 1)` | grün |
| U04 | linke Treffergrenze um eins erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 265, column: 2)` | grün |
| U04 | linke Treffergrenze um eins erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 122, column: 0)` | grün |
| U04 | linke Treffergrenze um eins erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 203, column: 2)` | grün |
| U04 | linke Treffergrenze um eins erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 43, column: 0)` | rot |
| U05 | rechte Treffergrenze um eins senken | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 202, column: 1)` | grün |
| U05 | rechte Treffergrenze um eins senken | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 123, column: 1)` | grün |
| U05 | rechte Treffergrenze um eins senken | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 265, column: 2)` | rot |
| U05 | rechte Treffergrenze um eins senken | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 122, column: 0)` | grün |
| U05 | rechte Treffergrenze um eins senken | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 203, column: 2)` | grün |
| U05 | rechte Treffergrenze um eins senken | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal(mouseX: 43, column: 0)` | grün |
| U06 | Marker nicht zeichnen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_AppearsLeftOfTheEntryUnderTheMouse_AndIsRemovedWhenLeaving` | rot |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_NoCoach_HealingFollowsDayCounter` | rot |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.ClubOnly_WorldCupPeriod_NoTrainingNoMarketNoDrift(seed: 3)` | grün |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.ClubOnly_WorldCupPeriod_NoTrainingNoMarketNoDrift(seed: 11)` | grün |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_WorldCupLoop_TalentSearchStands(coach: False)` | grün |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_WorldCupLoop_TalentSearchStands(coach: True)` | grün |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldOnly_OneWorldCup_AutoOnOff(auto: False)` | grün |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldOnly_OneWorldCup_AutoOnOff(auto: True)` | grün |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_Coach_TrainingOnlyAfterWorldCupDriftDays(seed: 11)` | rot |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_Coach_TrainingOnlyAfterWorldCupDriftDays(seed: 3)` | rot |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldOnly_InjuredPlayer_HealsTwiceOverWorldCup` | grün |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_NoCoach_TrainingOnlyManagerZeroAfterWorldCupDriftDays(seed: 11)` | rot |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_NoCoach_TrainingOnlyManagerZeroAfterWorldCupDriftDays(seed: 3)` | rot |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldCupLoop_MarketDecay_LikeFile18To48AndFile19To20(worldOnly: False)` | rot |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldCupLoop_MarketDecay_LikeFile18To48AndFile19To20(worldOnly: True)` | rot |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldCupLoop_MarketDecay_FloorAtMinStrain` | rot |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_FourSeasonChanges_WorldCupLoopWithAndWithoutCoach(active: True)` | rot |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_FourSeasonChanges_WorldCupLoopWithAndWithoutCoach(active: False)` | rot |
| W01 | WM-Tageslauf nach WM-Spielen auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.MainPath_TalentSearchTicks_FollowDayCounter` | grün |
| W02 | WM-Spiele auch im Vereinsmodus unterstellen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.ClubOnly_WorldCupPeriod_NoTrainingNoMarketNoDrift(seed: 3)` | rot |
| W02 | WM-Spiele auch im Vereinsmodus unterstellen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.ClubOnly_WorldCupPeriod_NoTrainingNoMarketNoDrift(seed: 11)` | rot |
| W03 | Heilung im Weltkugelmodus auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldOnly_InjuredPlayer_HealsTwiceOverWorldCup` | rot |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_NoCoach_HealingFollowsDayCounter` | rot |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.ClubOnly_WorldCupPeriod_NoTrainingNoMarketNoDrift(seed: 3)` | rot |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.ClubOnly_WorldCupPeriod_NoTrainingNoMarketNoDrift(seed: 11)` | rot |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_WorldCupLoop_TalentSearchStands(coach: False)` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_WorldCupLoop_TalentSearchStands(coach: True)` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldOnly_OneWorldCup_AutoOnOff(auto: False)` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldOnly_OneWorldCup_AutoOnOff(auto: True)` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_Coach_TrainingOnlyAfterWorldCupDriftDays(seed: 11)` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_Coach_TrainingOnlyAfterWorldCupDriftDays(seed: 3)` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldOnly_InjuredPlayer_HealsTwiceOverWorldCup` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_NoCoach_TrainingOnlyManagerZeroAfterWorldCupDriftDays(seed: 11)` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_NoCoach_TrainingOnlyManagerZeroAfterWorldCupDriftDays(seed: 3)` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldCupLoop_MarketDecay_LikeFile18To48AndFile19To20(worldOnly: False)` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldCupLoop_MarketDecay_LikeFile18To48AndFile19To20(worldOnly: True)` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.WorldCupLoop_MarketDecay_FloorAtMinStrain` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_FourSeasonChanges_WorldCupLoopWithAndWithoutCoach(active: True)` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_FourSeasonChanges_WorldCupLoopWithAndWithoutCoach(active: False)` | grün |
| W04 | Heilung der Managerkader auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.MainPath_TalentSearchTicks_FollowDayCounter` | grün |
| W05 | WM-Tagesabschluss mit Talentsuche rechnen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_WorldCupLoop_TalentSearchStands(coach: False)` | rot |
| W05 | WM-Tagesabschluss mit Talentsuche rechnen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil4Tests.Mode4_WorldCupLoop_TalentSearchStands(coach: True)` | rot |

### Inventur je geändertem oder neuem Test

Grundlage ist die Roslyn-Inventur aus Teil 2 mit 334 Methoden. `rot` bedeutet eine in Teil 4 erneut ausgeführte Regelmutation, die mindestens eine Zeile der Methode rot macht; die vollständige Einzelzeilentabelle darüber ist maßgebend. `offen` bedeutet, dass diese erneute Gegenprobe in Teil 4 noch aussteht. Bereits in Teil 2 oder 3 dokumentierte Ergebnisse bleiben dort gültig, ersetzen aber nicht die hier neu ausstehende Protokollierung. Änderungen an Testhelfern, Datenquellen und gelöschte Tests bleiben zusätzlich zu prüfen.

| Testdatei / Methode | Delta | Erneute Gegenprobe |
|---|---|---|
| `AutoLineupTeil8Tests.cs · AutoLineupTeil8Tests.Migration_31To32_KeepsHandMadeLineup` | geändert | offen |
| `DeadlineRegressionTests.cs · DeadlineRegressionTests.R208_AnswerChecksDeadlineAtPausedUnprocessedDay` | geändert | offen |
| `FinanceCalibrationTests.cs · FinanceCalibrationTests.PassiveBundesligaManager_WithoutCare_NotDismissed_ForcedRelegation` | neu | offen |
| `GameModeTests.cs · GameModeTests.Mode4_ReferenceValues_Unchanged` | geändert | offen |
| `HeimspielStatistikTests.cs · HeimspielStatistikTests.MultiSeason_FourChanges_PassiveAndActiveManager` | geändert | offen |
| `HotSeatTests.cs · HotSeatTests.DismissalDuringDay_ExcludedFromNextDay` | geändert | offen |
| `IntegrationRunde9Tests.cs · IntegrationRunde9Tests.PlayoffWithoutManager_FastUnlessResultsReported` | geändert | offen |
| `KaderStartRunde14FixTests.cs · KaderStartRunde14FixTests.ListedOwnPlayer_SaveLoad_ListingKept_WithdrawAfterLoad` | neu | offen |
| `KaderStartRunde14FixTests.cs · KaderStartRunde14FixTests.ListedOverSeasonEnd_ReturnsWithSeasonCountersReset_ClubGamesKept` | neu | offen |
| `KaderStartRunde14FixTests.cs · KaderStartRunde14FixTests.ListedOwnPlayer_InjuryHealsLikeSquad` | neu | offen |
| `KaderStartRunde14FixTests.cs · KaderStartRunde14FixTests.FourSeasonChanges_WithListedPlayers_SaveAndLoadEachSeason` | neu | offen |
| `KaderStartRunde14FixTests.cs · KaderStartRunde14FixTests.CatchUpManagerDay_NoListChange_BidsOnlyOnOwnListings` | neu | offen |
| `KaderStartRunde14ImportTests.cs · KaderStartRunde14ImportTests.Import_CountersPerCompetition_FromSquadSlots` | neu | offen |
| `KaderStartRunde14ImportTests.cs · KaderStartRunde14ImportTests.Import_TableRankAndStartLeague` | neu | offen |
| `KaderStartRunde14Teil2Tests.cs · KaderStartRunde14Teil2Tests.WorldOnly_EveryManagerRowMapsToRowZero` | neu | offen |
| `KaderStartRunde14Teil2Tests.cs · KaderStartRunde14Teil2Tests.OneSeason_SingleManager_ClubOnPlaceZero` | neu | offen |
| `KaderStartRunde14Teil2Tests.cs · KaderStartRunde14Teil2Tests.NoSpecialCase_PlaceZeroUntouched` | neu | offen |
| `KaderStartRunde14Teil2Tests.cs · KaderStartRunde14Teil2Tests.StartLeagueDraw_NeverPlaceZero_ChampionPossible` | neu | offen |
| `KaderStartRunde14Teil2Tests.cs · KaderStartRunde14Teil2Tests.ClubTactics_StartAtZero_AutoLineAfterFirstMatchDay` | neu | offen |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.AutoLineup_AfterMatchWithoutInjury_LinesUpAgain` | neu | offen |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.ManualLineup_AfterMatchWithoutInjury_Unchanged` | neu | offen |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.CupMatch_CountsInCupFields` | neu | offen |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.EuropeanCupMatch_CountsInEuropeFields` | neu | offen |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.Postseason_CountsAsLeague` | neu | offen |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.Join_ResetsClubCountersOnly` | neu | offen |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.FirstMatchDay_NewGame_UsesRankZero` | neu | offen |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.MultiSeason_TableRankAndCompetitionCounters` | neu | offen |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.Migration_OldSave_LeagueOnlyAndTableRankFromRankSlots` | neu | offen |
| `KaderStartRunde14Teil6Tests.cs · KaderStartRunde14Teil6Tests.LeftClick_WithoutLine_NoLineSelected_NothingChanges` | neu | offen |
| `KaderStartRunde14Teil6Tests.cs · KaderStartRunde14Teil6Tests.LeftClick_FirstFreeSlotOfLine_FullLineNoRoom_GoalkeeperWrongLine` | neu | offen |
| `KaderStartRunde14Teil6Tests.cs · KaderStartRunde14Teil6Tests.ClickOnPlaced_TakesOut_SelectsHisLine_LeftUndresses_RightKeeps` | neu | offen |
| `KaderStartRunde14Teil6Tests.cs · KaderStartRunde14Teil6Tests.RightClick_TogglesDressed_TwentyThirdRefused_AutoLineupOff` | neu | offen |
| `KaderStartRunde14Tests.cs · KaderStartRunde14Tests.StartLeague_WithoutManagers_IsKept_AndAdoptedByNewManager` | neu | offen |
| `KaderStartRunde14Tests.cs · KaderStartRunde14Tests.StartLeague_WithoutManagers_StepReportsOnlyRealChanges` | neu | offen |
| `KaderStartRunde14Tests.cs · KaderStartRunde14Tests.StartLeague_SurvivesRemovalOfLastManager` | neu | offen |
| `KaderStartRunde14Tests.cs · KaderStartRunde14Tests.JoinScoutedTalent_GoalkeeperOrDefender_SortedIn` | neu | offen |
| `KaderStartRunde14Tests.cs · KaderStartRunde14Tests.Migration_41To42_TakesStartLeagueOfFirstManager` | neu | offen |
| `KaderStartStartLigaTests.cs · KaderStartStartLigaTests.Bundesliga_AlwaysDraws_NeverTheEuropeanCupPlace` | geändert | offen |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.Offer_PlayerLeavesSquad_SlotsMoveUp_ListingWithSeller` | neu | offen |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.Withdraw_ReturnsPlayerByRole_NotInLineup_ContractUnchanged` | neu | offen |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.Withdraw_FullSquad_StaysOnMarket` | neu | offen |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.ListedPlayer_KeepsCostingSalary` | neu | offen |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.SoldListedPlayer_GoesToBuyer_SellerPaid` | neu | offen |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.SeasonChange_UnsoldListedPlayerReturnsByRole_NotInLineup_ContractCountedDown` | neu | offen |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.FourSeasonChanges_ListedPlayersReturn_NoDuplicates` | neu | offen |
| `KaderStartTeil4Tests.cs · KaderStartTeil4Tests.MarketLoan_EndsFree_NotBackToComputerClub` | neu | offen |
| `KaderStartTeil4Tests.cs · KaderStartTeil4Tests.ManagerLoan_ReturnsWithLoanSlotContract_SortedIn` | neu | offen |
| `KaderStartTeil4Tests.cs · KaderStartTeil4Tests.ManagerLoan_ExpiringLoanSlot_NegotiatedAtOrigin` | neu | offen |
| `KaderStartTeil4Tests.cs · KaderStartTeil4Tests.ManagerLoan_ExpiringLoanSlot_WithoutScreen_LeavesOrigin` | neu | offen |
| `KaderStartTeil4Tests.cs · KaderStartTeil4Tests.SeasonEndNumbers_File44To46` | neu | offen |
| `KaderStartTeil4Tests.cs · KaderStartTeil4Tests.RenewalSalary_UsesAgeAfterAgeing` | neu | offen |
| `KaderStartTeil4Tests.cs · KaderStartTeil4Tests.SeasonChange_ResetsSeasonCounters_KeepsClubCounters` | neu | offen |
| `KaderStartTeil4Tests.cs · KaderStartTeil4Tests.Offer_ListPriceIsSlotPrice_LoanFeeThird` | neu | offen |
| `KaderStartTeil4Tests.cs · KaderStartTeil4Tests.FourSeasonChanges_LoansEnd_NoDuplicates` | neu | offen |
| `KaderTransferTeil5Tests.cs · KaderTransferTeil5Tests.SeasonEnd_OfferSalary_KeepsGamesForClub` | geändert | offen |
| `KaderTransferTeil5Tests.cs · KaderTransferTeil5Tests.SeasonEnd_Compensation_KeepsGamesForClub` | geändert | offen |
| `KaderTransferTeil5Tests.cs · KaderTransferTeil5Tests.GamesForClub_AccumulateOverFourSeasonChanges` | geändert | offen |
| `KaufEinsortierenTests.cs · KaufEinsortierenTests.Buy_Goalkeeper_FromTransferList_SortedIn` | geändert | offen |
| `KaufEinsortierenTests.cs · KaufEinsortierenTests.LoanReturn_ToManagerClub_SortedIn` | geändert | offen |
| `LifecycleRegressionTests.cs · LifecycleRegressionTests.R204_MarketLoanEndsFree_NotBackWithItsRemainingContract` | neu | offen |
| `LoadQuirkTests.cs · LoadQuirkTests.OnlyMarkerDaySaves_GetTheOffset` | neu | offen |
| `LoadQuirkTests.cs · LoadQuirkTests.File44_AfterImport_SecondDayChange_FifteenthOfApril` | neu | offen |
| `LoadQuirkTests.cs · LoadQuirkTests.File44_Offset_SurvivesSave_EndsAtSeasonChange_FourSeasons` | neu | offen |
| `LoadQuirkTests.cs · LoadQuirkTests.NewGame_HasNoOffset` | neu | offen |
| `LoadQuirkTests.cs · LoadQuirkTests.Schema46_WithoutOffset_LoadsWithZero` | neu | offen |
| `NationalCoachMode4Tests.cs · NationalCoachMode4Tests.Mode4_TwoManagersInTopFive_HigherPlacedBecomesCoach` | geändert | offen |
| `NationalCoachMode4Tests.cs · NationalCoachMode4Tests.Mode4_NoManagerInTopFive_NoCoach` | geändert | offen |
| `NationalCoachMode4Tests.cs · NationalCoachMode4Tests.Mode4_Appointment_BeforeReroll_GermanyKeepsBaseRatings` | geändert | offen |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.MenuBuild_WithDefaultSetting_LinesUpTheSquad` | neu | offen |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.ClearedLineup_SwitchesOff_ValuesZero_PlaysEmpty` | neu | offen |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.ClearedLineup_SavedInTurn_LoadLinesUpAgain` | neu | offen |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.Click_SwitchesOff_EmptySlotDoesNot` | neu | offen |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver` | neu | offen |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation` | neu | offen |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.Migration_Chain39To41_AddsBothSettings` | neu | offen |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.FullSquad_ClickDoesNothing_KeeperSwapTakesRowThree` | neu | offen |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.ClubOnly_WorldCupPeriod_NoTrainingNoMarketNoDrift` | neu | rot: W02, W04 |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.Mode4_NoCoach_TrainingOnlyManagerZeroAfterWorldCupDriftDays` | neu | rot: W01 |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.Mode4_Coach_TrainingOnlyAfterWorldCupDriftDays` | neu | rot: W01 |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.Mode4_FourSeasonChanges_WorldCupLoopWithAndWithoutCoach` | neu | rot: W01 |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.WorldOnly_OneWorldCup_AutoOnOff` | neu | offen |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.WorldCupLoop_MarketDecay_LikeFile18To48AndFile19To20` | neu | rot: W01 |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.WorldCupLoop_MarketDecay_FloorAtMinStrain` | neu | rot: W01 |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.WorldOnly_InjuredPlayer_HealsTwiceOverWorldCup` | neu | rot: W03 |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.Mode4_NoCoach_HealingFollowsDayCounter` | neu | rot: W01, W04 |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.Mode4_WorldCupLoop_TalentSearchStands` | neu | rot: W05 |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.MainPath_TalentSearchTicks_FollowDayCounter` | neu | offen |
| `NationalteamRunde14Tests.cs · NationalteamRunde14Tests.Day_MainPathOnly_NationalValuesBeforeDrift_DriftDayShowsRolledForm` | neu | offen |
| `NationalteamRunde14Tests.cs · NationalteamRunde14Tests.Mode4_AfterRealAppointment_DriftDayWorldCupDate_UsesLineupForm` | neu | offen |
| `NationalteamRunde14Tests.cs · NationalteamRunde14Tests.Mode4_FourSeasonChanges_WorldCupLoopEverySeason` | neu | offen |
| `NationalteamRunde14Tests.cs · NationalteamRunde14Tests.Mode4_WorldCupDay_PlaysLineupSeenInCoachTurn` | neu | offen |
| `NationalteamRunde14Tests.cs · NationalteamRunde14Tests.SeasonChange_GermanyConditionTechniqueKept_FormRolled` | neu | offen |
| `NationalteamRunde14Tests.cs · NationalteamRunde14Tests.HandNomination_InsertsByRole` | neu | offen |
| `NationalteamRunde14Tests.cs · NationalteamRunde14Tests.WorldCupStage_LocksNationalSquadFromFirstDate` | neu | offen |
| `NationalteamTests.cs · NationalteamTests.Club_AutoDisplay_Unchanged` | geändert | offen |
| `NationalteamTests.cs · NationalteamTests.Club_AutoDisplay_KeepsLastLine` | neu | offen |
| `NationalteamTests.cs · NationalteamTests.Schema11_WithoutCoach_IsMigrated` | geändert | offen |
| `NewGameTests.cs · NewGameTests.Create_RandomBundesligaClub_NeverTakesEuropeanCupParticipant` | geändert | offen |
| `NummerierungMigrationTests.cs · NummerierungMigrationTests.CurrentSave_RoundTrip_Unchanged` | geändert | offen |
| `OriginalImportCupTests.cs · OriginalImportCupTests.Import_DebCupQuarterFinal` | neu | offen |
| `OriginalImportCupTests.cs · OriginalImportCupTests.Import_DebCupFinalWithMarks` | neu | offen |
| `OriginalImportCupTests.cs · OriginalImportCupTests.Import_DebCupAllStands` | neu | offen |
| `OriginalImportCupTests.cs · OriginalImportCupTests.Import_DebCupContinues` | neu | offen |
| `OriginalImportCupTests.cs · OriginalImportCupTests.Import_EuropeanCupFromOriginal` | neu | offen |
| `OriginalImportCupTests.cs · OriginalImportCupTests.Import_WorldCupFromOriginal` | neu | offen |
| `OriginalImportFieldMapDataTests.cs · OriginalImportFieldMapDataTests.ClubRecords_MapToDatabaseByName` | neu | offen |
| `OriginalImportFieldMapDataTests.cs · OriginalImportFieldMapDataTests.LeagueTable_UsesSlotsOfItsLeague` | neu | offen |
| `OriginalImportFieldMapDataTests.cs · OriginalImportFieldMapDataTests.ManagerRecordOffsets_MatchDocumentedValues` | neu | offen |
| `OriginalImportFieldMapTests.cs · OriginalImportFieldMapTests.FieldMap_NamesSchemaVersion` | neu | offen |
| `OriginalImportFieldMapTests.cs · OriginalImportFieldMapTests.FieldMap_CoversEverySchemaFieldExactlyOnce` | neu | offen |
| `OriginalImportFieldMapTests.cs · OriginalImportFieldMapTests.FieldMap_RowsHaveValidStatusAndSource` | neu | offen |
| `OriginalImportFieldMapTests.cs · OriginalImportFieldMapTests.FieldMap_ImportedRows_HaveTheirBlocksAndOffsetsInTheImportCode` | neu | offen |
| `OriginalImportFieldMapTests.cs · OriginalImportFieldMapTests.StripComments_RemovesLineAndBlockCommentsButKeepsStrings` | neu | offen |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_PoolFromOriginal` | neu | rot: P01 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_MarketFromOriginal` | neu | rot: P02 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_MarketListingOfManager` | neu | rot: P03 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_PoolNamesUnique_AndContinue` | neu | rot: P04 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_NationalSquadFromOriginal` | neu | rot: P05 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_MessagesFromOriginal` | neu | rot: P06 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_MessageAboutPoolPlayer` | neu | rot: P07 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_RoundTripWithPoolMarketNational` | neu | rot: P08 |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_RelegationGroups_FromOriginal` | neu | offen |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_Playoffs_FromOriginal` | neu | offen |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_SeasonChange_LeaguesLikeNextOriginalStand` | neu | offen |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_AllStands_PostseasonMatchesOriginal` | neu | offen |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_AllStands_FourSeasonChanges` | neu | offen |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_StocksFromOriginal` | neu | rot: I01 |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_StockHoldings` | neu | rot: I02 |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_TrainingPlan` | neu | rot: I03, I12 |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_TacticsAndAutoLineup` | neu | rot: I06 |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_TacticModes` | neu | rot: I06 |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_SeriesRecordsAndTableFields` | neu | rot: I11 |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_RankSlotsMatchTable` | neu | rot: I09 |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_Sponsoring` | neu | rot: I05 |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_BankAndManagerLoans` | neu | rot: I04 |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_NationalAccountAndTalentSearch` | neu | rot: I13 |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_NewMessageFlag` | neu | rot: I07, I08 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_StadiumConstruction_File43` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_StadiumConstruction_File44A` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_NoConstruction_WhenOriginalHasNone` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.TryImport_SponsorContractAmountNotDivisibleByThree_Loads` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.TryImport_ImportError_IsReportedWithoutChecksumFlag` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_FriendliesFromOriginal` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_StadiumStatisticsFromOriginal` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_PlayerTrendFromSlot` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_ContractNoticeFromSlot` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_NationalCoach_WhenOnlyTheOriginalHasOne` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_NationalTacticsShownAutoLine` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_CupWinnersFromOriginal` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.ClubIndices_WorldOnly_NationKeepsItsIndex_ManagerSlotGetsTheMissingClub` | neu | offen |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_AutoLineup_ReadsCurrentByteNotTheSavedOne` | neu | offen |
| `OriginalImportRunde14Tests.cs · OriginalImportRunde14Tests.Import_TakesNationalCoachAutoLineupFromBlock0375` | neu | offen |
| `OriginalImportRunde14Tests.cs · OriginalImportRunde14Tests.Import_AllStands_RoundTripIsStable` | neu | offen |
| `OriginalImportTests.cs · OriginalImportTests.Import_SetsDateManagersAndClubs` | neu | offen |
| `OriginalImportTests.cs · OriginalImportTests.Import_LeagueTableMatchesOriginalTableRecords` | neu | offen |
| `OriginalImportTests.cs · OriginalImportTests.Import_AllOriginalStands_AreValid` | neu | offen |
| `OriginalImportTests.cs · OriginalImportTests.Import_ManagerSquadFromSquadSlots` | neu | offen |
| `OriginalImportTests.cs · OriginalImportTests.Import_RoundTripAndContinue` | neu | offen |
| `OriginalImportTests.cs · OriginalImportTests.Import_FourSeasonChanges` | neu | offen |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Playoffs_NextRound_PairsByTable0x144_HomeAtSmallerSlot` | neu | rot: S01 |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.MultiSeason_FourChanges_PlayoffsPairByTable0x144` | neu | rot: S01 |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Playoffs_OwnRounds_PairLikeTheOriginal` | neu | rot: S01 |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Import_RunningQuarterFinal_SwapsAtOddGameCount` | neu | rot: S02 |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Import_RelegationGroup_ThreeOfSixDates` | neu | rot: S03 |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Import_OwnListedPlayer_OnlyOnMarket` | neu | offen |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Import_CarriesPerCompetition_AllStands` | neu | rot: S04 |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Load_Schema43_RunningQuarterFinal_SemifinalByTable0x144` | neu | rot: S01, S05 |
| `PruefungKarteRunde14Tests.cs · PruefungKarteRunde14Tests.Schema45_WithDebtDaysAndDismissed_LoadsKeepsDismissed_EndsGame` | neu | offen |
| `PruefungKarteRunde14Tests.cs · PruefungKarteRunde14Tests.ListedInjuredOwnPlayer_HealsOnTheHealingTick` | neu | offen |
| `PruefungKarteRunde14Tests.cs · PruefungKarteRunde14Tests.AutoLineup_OnlyOnTickOrEventDays` | neu | offen |
| `RngTwinProbeTests.cs · RngTwinProbeTests.FourSeasonChanges_FingerprintUnchangedBySerializableGenerator` | geändert | offen |
| `RngTwinProbeTests.cs · RngTwinProbeTests.FourSeasonChanges_OriginalDatabase_FingerprintUnchanged` | geändert | offen |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.ManagerPlaysCupDay_OptionOn_AllPairingsUseMinuteLoop_RandomDiffers` | neu | offen |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.NoManagerInvolved_OptionDoesNothing_SameRandom` | neu | offen |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.Default_IsOff_AndSaveRoundTripKeepsIt` | neu | offen |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.Schema39Save_Migrates_OptionOff` | neu | offen |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.MultiSeason_FourSeasonChanges_BothSettings` | neu | offen |
| `SaisonSatzfolgeSaisonendeTests.cs · SaisonSatzfolgeSaisonendeTests.MultiSeason_NextSlotsFormedOn29April_StartNextSeasonAdoptsThemUnchanged` | geändert | offen |
| `SaisonSatzfolgeSaisonendeTests.cs · SaisonSatzfolgeSaisonendeTests.Preparing_NextSlots_DoesNotAdvanceGameRandom` | geändert | offen |
| `SaisonSatzfolgeSaisonendeTests.cs · SaisonSatzfolgeSaisonendeTests.FairPlayWinner_WithoutFormedSlots_FallsBackToIndexOrder` | neu | offen |
| `SaisonwechselEntlassungTests.cs · SaisonwechselEntlassungTests.OneManagerDismissed_OtherFinishesSeason_RecordWithoutDismissed` | neu | offen |
| `SaisonwechselEntlassungTests.cs · SaisonwechselEntlassungTests.AllManagersDismissed_GameOverAtNextDay` | neu | offen |
| `SaisonwechselOberligaTests.cs · SaisonwechselOberligaTests.StartSwap_ChosenClubWithoutLeague_DisplacedIsFromStartLeague` | neu | offen |
| `SaisonwechselOberligaTests.cs · SaisonwechselOberligaTests.StartSwap_TwoChosenClubsWithoutLeague_BothDisplacedOutside_MultiSeason` | neu | offen |
| `SaisonwechselProbenTests.cs · SaisonwechselProbenTests.PlayoffWithoutManager_EachResultsFieldAloneSwitchesToMinuteLoop` | geändert | offen |
| `SaveGameTests.cs · SaveGameTests.SchemaFingerprint_MatchesSchemaVersion` | geändert | offen |
| `SeasonEndStatusTests.cs · SeasonEndStatusTests.Full_ManagerInTopFive_GetsStatusBit_OtherNot` | geändert | offen |
| `SeasonEndStatusTests.cs · SeasonEndStatusTests.Full_EveryManagerInTopFive_GetsStatusBit` | geändert | offen |
| `SeasonEndStatusTests.cs · SeasonEndStatusTests.StatusBit_DoesNotChangeStoredEventsOrPremium` | geändert | offen |
| `SeasonEndStatusTests.cs · SeasonEndStatusTests.SaveLoad_StatusBitIsDerivedFromSavedData` | geändert | offen |
| `SeasonFlowTests.cs · SeasonFlowTests.Debts_DoNotDismiss` | neu | offen |
| `SeasonFlowTests.cs · SeasonFlowTests.Dismissal_OtherManagersContinue` | geändert | offen |
| `SpielberechnungAnschlussTests.cs · SpielberechnungAnschlussTests.FourSeasonChanges_OriginalDatabase_CountersPerSeason` | geändert | offen |
| `SpielberechnungKomplett3Tests.cs · SpielberechnungKomplett3Tests.Heal_Weekly_PermanentStays` | geändert | offen |
| `SpielstaerkeManager2Tests.cs · SpielstaerkeManager2Tests.FourSeasonChanges_ManagedMatchesPlausible` | geändert | offen |
| `SponsorBewertungTests.cs · SponsorBewertungTests.FourSeasonChanges_RatingAndIncomeStayConsistent` | geändert | offen |
| `SquadLifecycleTests.cs · SquadLifecycleTests.R211_PassiveManager_IsWarnedOnceForEveryExpiringPlayer_BeforeTheDeadline` | geändert | offen |
| `StatistikenPraemienTests.cs · StatistikenPraemienTests.OtherClubLeads_NoSinglePremium` | geändert | offen |
| `StatistikenPraemienTests.cs · StatistikenPraemienTests.MultiSeason_FourChanges_PassiveAndActiveManager` | geändert | offen |
| `TacticsOriginalTests.cs · TacticsOriginalTests.Defaults_ShowEvenMatchRowOfOriginal` | geändert | offen |
| `TacticsOriginalTests.cs · TacticsOriginalTests.EffortIsPercent_AttackUpToSeven_BaseAndSurchargeShareTheLimit` | geändert | offen |
| `TacticsOriginalTests.cs · TacticsOriginalTests.EffectiveEffort_IsBasePlusSituation` | geändert | offen |
| `TacticsOriginalTests.cs · TacticsOriginalTests.EffectiveAttack_KeepsThreeBits` | geändert | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.MainPath_TwoManagers_NegotiationLockCountsOncePerTick` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.SummerBreak_CountersFromSeasonEndToNinthOfSeptember` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.SummerBreak_AdvancesConstructionAndHealsSquad` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.SummerBreak_DayClose_FourMonthEndsBooked` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.File44_GastronomyOrderedOnFourteenthOfApril_Loses21WeeksUntilNewSeason` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.MainPath_TickOfHaltDay_RunsBeforeHalt` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.File24_ToMenuOfDay17_MarketDecayedOnceLikeFile25` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.SeasonChange_KeepsMarketList_PlayersAgeOneYear` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.File44_SeasonChange_KeepsListLikeFile46` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.MainPath_ManagerZeroDismissed_MarketStillDecaysPerTick` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.MainPath_TrendControlCountsDownPerTick` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.AdvanceTrend_ClampsAndRerollsAtZero` | neu | offen |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.TrainingDays_AreDayCounterTicks` | neu | offen |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks` | neu | offen |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick` | neu | offen |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.ListChangeDays_OnlyEventDaysOfMainPath` | neu | offen |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.CatchUpManagerDay_TwoManagers_SecondNeitherExpiresNorBidsOnForeignListings` | neu | offen |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero` | neu | offen |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks` | neu | offen |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.Construction_AdvancesOnCounterZeroDays` | neu | offen |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds` | neu | offen |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_LoadQuirkBooksAprilMonthEnd_LikeFile46_2` | neu | offen |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation` | neu | offen |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead` | neu | offen |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.DeepDebts_NoDismissal_GameContinues` | neu | offen |
| `TaktikTeil5Tests.cs · TaktikTeil5Tests.Manual_KeepsEffortAndAttack_DuringMatch` | geändert | offen |
| `TeamValueDriftTests.cs · TeamValueDriftTests.GameStart_DriftsIntoLeagueBounds` | geändert | offen |
| `TransferOriginalTests.cs · TransferOriginalTests.MultiSeason_Transfers_StayConsistent` | geändert | offen |
| `TransferOriginalVertragTests.cs · TransferOriginalVertragTests.Lock_RefusesInput_AndCountsDownDaily` | geändert | offen |
| `TransferTransactionRegressionTests.cs · TransferTransactionRegressionTests.R209_InvalidBidCannotBookOrMove` | geändert | offen |
| `TransferVertraegeKomplettTeil4Tests.cs · TransferVertraegeKomplettTeil4Tests.SponsorRating_PlayerWithBid_DrawsValueAgainBeforeStarCheck` | geändert | offen |
| `WaveBReviewRegressionTests.cs · WaveBReviewRegressionTests.PR3502_BidThatBecameInvalid_IsRejectedWithResultAndRemoved` | geändert | offen |
| `WettbewerbeSpiel12Teil3Tests.cs · WettbewerbeSpiel12Teil3Tests.MultiSeason_ComputerClubsPlayWithoutSquad` | geändert | offen |
| `WettbewerbeSpiel1Tests.cs · WettbewerbeSpiel1Tests.DebCup_NoMatchEndsLevel_ShootoutInFinalScore` | geändert | offen |
| `OriginalSaveHeaderTests.cs · OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | neu | rot: H01, H05, H06, H07, H08, H09, H11, H12, H13, H14, H15, H16, H17, H18, H19 |
| `OriginalSaveHeaderTests.cs · OriginalSaveHeaderTests.TryRead_ReadsOnlyTheHeader_NoLengthOrChecksumCheck` | neu | rot: H02 |
| `OriginalSaveHeaderTests.cs · OriginalSaveHeaderTests.TryRead_LimitsManagersToFour` | neu | rot: H03 |
| `OriginalSaveHeaderTests.cs · OriginalSaveHeaderTests.TryRead_WrongMagicOrTooShort_ReturnsNull` | neu | rot: H04 |
| `BildschirmeFreipruefungTests.cs · BildschirmeFreipruefungTests.AllScreens_OpeningAndDrawingDoNotChangeGameState` | geändert | offen |
| `BildschirmeFreipruefungTests.cs · BildschirmeFreipruefungTests.NationalMenu_Open_AllCoachTiles_DoNotChangeGameState` | neu | offen |
| `FinanceBedienungTests.cs · FinanceBedienungTests.HeldButton_Repeats_AndDrawsBar` | geändert | offen |
| `FinanceBedienungTests.cs · FinanceBedienungTests.Hint_ShownForActiveSide` | geändert | offen |
| `FinanceBedienungTests.cs · FinanceBedienungTests.AmountBar_DisappearsWhenMouseLeavesStrip` | neu | offen |
| `GameOptionsScreenTests.cs · GameOptionsScreenTests.ManagerDaySwitch_TogglesOption` | neu | offen |
| `HistoryBarsDosboxTests.cs · HistoryBarsDosboxTests.Bars_ColorsHeightsAndMarker_LikeOriginal` | neu | offen |
| `HokImportAccessTests.cs · HokImportAccessTests.List_MarksHokFilesAsOriginal` | neu | offen |
| `HokImportAccessTests.cs · HokImportAccessTests.TryImport_RejectsNonOriginalData_WithoutChecksumFlag` | neu | offen |
| `HokImportAccessTests.cs · HokImportAccessTests.LoadDialog_ImportsHok_ByTypedName` | neu | offen |
| `HokImportAccessTests.cs · HokImportAccessTests.LoadDialog_ImportsHok_ByClickTwice` | neu | offen |
| `HokImportAccessTests.cs · HokImportAccessTests.SameName_TypedLoadsRemakeSave_ClickOnHokImports` | neu | offen |
| `HokImportAccessTests.cs · HokImportAccessTests.SameName_TypedThenSingleClickOnHok_Imports` | neu | offen |
| `HokImportAccessTests.cs · HokImportAccessTests.ChecksumError_ShowsMogelguru_AndKeepsRunningGame` | neu | offen |
| `HokImportAccessTests.cs · HokImportAccessTests.TryImport_FlagsChecksumError_AndImportsValidSave` | neu | offen |
| `HokImportAccessTests.cs · HokImportAccessTests.SaveDialog_DoesNotSelectHok` | neu | offen |
| `IntegrationRunde4Tests.cs · IntegrationRunde4Tests.NationalLineup_SurvivesSaveLoadThroughDialog_InCoachMenu` | geändert | offen |
| `IntegrationRunde6Tests.cs · IntegrationRunde6Tests.TwoSeasons_Mode4_StatisticsRatingStatusAndSaves` | geändert | offen |
| `KaderStartRunde14FixUiTests.cs · KaderStartRunde14FixUiTests.DialogFrame_ContractWindow_OuterShadowCornersInFillColor` | neu | offen |
| `KaderStartRunde14FixUiTests.cs · KaderStartRunde14FixUiTests.ContractDialog_SeparatorSixTo163_ColonsAt76` | neu | offen |
| `KaderStartRunde14FixUiTests.cs · KaderStartRunde14FixUiTests.ContractDialog_PressedButton_LabelAtSamePlace` | neu | offen |
| `KaderStartRunde14FixUiTests.cs · KaderStartRunde14FixUiTests.OfferWindow_AtXTwo_Width165_LineSixTo163` | neu | offen |
| `KaderStartRunde14FixUiTests.cs · KaderStartRunde14FixUiTests.TransferScreen_AccountBar_LikeDosBox` | neu | offen |
| `KaderStartRunde14FixUiTests.cs · KaderStartRunde14FixUiTests.TransferScreen_SquadList_NoStarForListedPlayer_EvenInLegacyState` | neu | offen |
| `KaderStartRunde14Teil6UiTests.cs · KaderStartRunde14Teil6UiTests.A364_NumberOnlyForDressedPlayers` | neu | offen |
| `KaderStartRunde14Teil6UiTests.cs · KaderStartRunde14Teil6UiTests.LeftClickOnReserve_WithoutLine_ShowsNoLineSelected` | neu | offen |
| `KaderStartRunde14Teil6UiTests.cs · KaderStartRunde14Teil6UiTests.SelectLine_ThenLeftClick_PlacesInFirstFreeSlotOfLine` | neu | offen |
| `KaderStartRunde14Teil6UiTests.cs · KaderStartRunde14Teil6UiTests.FullLine_ShowsNoRoom_AndChangesNothing` | neu | offen |
| `KaderStartRunde14Teil6UiTests.cs · KaderStartRunde14Teil6UiTests.GoalkeeperIntoFieldLine_IsRejected` | neu | offen |
| `KaderStartRunde14Teil6UiTests.cs · KaderStartRunde14Teil6UiTests.ClickOnPlacedPlayer_TakesOut_LeftUndresses_RightKeepsDressed` | neu | offen |
| `KaderStartRunde14Teil6UiTests.cs · KaderStartRunde14Teil6UiTests.RightClick_TogglesDressedForUnplaced` | neu | offen |
| `KaderStartRunde14Teil6UiTests.cs · KaderStartRunde14Teil6UiTests.Slot_LeftClickClears_RightClickLeaves` | neu | offen |
| `KaderStartSetupScreenTests.cs · KaderStartSetupScreenTests.StartLeague_BeforeFirstManager_IsAdopted` | neu | offen |
| `KaderStartTeil2SetupTests.cs · KaderStartTeil2SetupTests.WorldOnly_ClickOnSecondRow_EditsFirstManager_NoSecondManager` | neu | offen |
| `KaderStartTeil2cScreenTests.cs · KaderStartTeil2cScreenTests.OwnListingClick_WithoutBid_ReturnsPlayerToSquad` | neu | offen |
| `KaderStartTeil2cScreenTests.cs · KaderStartTeil2cScreenTests.OwnListingClick_WithBid_OpensBidWindow` | neu | offen |
| `KaderStartTeil4ScreenTests.cs · KaderStartTeil4ScreenTests.SeasonEnd_ListIsPictureBeforeNegotiation_RedrawnAfterDeparture` | neu | offen |
| `KaderStartTeil4ScreenTests.cs · KaderStartTeil4ScreenTests.ContractsScreen_HeaderAndDialogFrame_MatchDosBoxPictures` | neu | offen |
| `LifecycleScreenRegressionTests.cs · LifecycleScreenRegressionTests.R204_OriginalDataLoanAndContractEndAreConsistentAfterContinueThroughSeason` | geändert | offen |
| `LineupAutoDisabledMessageTests.cs · LineupAutoDisabledMessageTests.ManualChange_DisablesAutoLineup_AndShowsMessageOnce` | geändert | offen |
| `LineupShortMessagesE2Tests.cs · LineupShortMessagesE2Tests.List_Suspended_ShowsSpielerIstGesperrt` | geändert | offen |
| `LineupShortMessagesE2Tests.cs · LineupShortMessagesE2Tests.List_Injured_ShowsSpielerIstVerletzt` | geändert | offen |
| `NationalLineupScreenTests.cs · NationalLineupScreenTests.CoachMenu_OpensNationalLineupAndTactics_WithoutChangingState` | geändert | offen |
| `NationalLineupScreenTests.cs · NationalLineupScreenTests.A010_FullLine_NoRoom_ClickOnPlacedTakesOut` | geändert | offen |
| `NationalLineupScreenTests.cs · NationalLineupScreenTests.Lineup_Clicks_FollowOriginalRules_AndTacticsBelongToNationalTeam` | geändert | offen |
| `NationalteamRunde14UiTests.cs · NationalteamRunde14UiTests.CoachMenu_TransferTraining_LockedFromFirstWorldCupDate` | neu | offen |
| `NationalteamRunde14UiTests.cs · NationalteamRunde14UiTests.CoachMenu_WorldCupPairings_OpensKnockoutFromStageFive` | neu | offen |
| `NationalteamRunde14UiTests.cs · NationalteamRunde14UiTests.CoachMenu_WorldCupPairings_ReturnInSameTurn_KeepsRememberedGroup` | neu | offen |
| `NationalteamRunde14UiTests.cs · NationalteamRunde14UiTests.CoachMenu_WorldCupPairings_SeasonChange_ResetsRememberedGroup` | neu | offen |
| `NationalteamRunde14UiTests.cs · NationalteamRunde14UiTests.NationalLineup_RightClickOnSlotLeaves_LeftClickClears` | neu | offen |
| `NationalteamRunde14UiTests.cs · NationalteamRunde14UiTests.NationalLineup_LeftClickOnInjuredOrSuspended_StaysWithMessage` | neu | offen |
| `NewsBarDosboxTests.cs · NewsBarDosboxTests.Message_ShowsDateAndTwoCenteredLines` | neu | offen |
| `NewsBarDosboxTests.cs · NewsBarDosboxTests.LongMessage_ShowsOnlyTwoLines_NothingOnTheBottomEdge` | neu | offen |
| `OriginalDialogCallersTests.cs · OriginalDialogCallersTests.Lineup_AutoDisabledMessage_IsShortMessageWith45Ticks` | geändert | offen |
| `OriginalDialogCallersTests.cs · OriginalDialogCallersTests.Lineup_WrongRole_IsShortMessage_AndIgnoresClicks` | geändert | offen |
| `PassiveScreenTests.cs · PassiveScreenTests.PlanChangedOnPausedTrainingDay_AppliesFromNextTick` | neu | offen |
| `PruefungKarteRunde14UiTests.cs · PruefungKarteRunde14UiTests.TransferList_Separators_181To313_LongerAfterHover` | neu | offen |
| `PruefungKarteRunde14UiTests.cs · PruefungKarteRunde14UiTests.TransferScreen_AccountLabels_TopRow225_AnlagenAt133` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Paging_OnlyThroughArrowPair_InStepsOfFortyEight_NoEffectAtTheEnds` | neu | rot: U01 |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Paging_WheelAndKeys_DoNothing` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Panel_ShowsNoPageLabel` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Arrows_AreTheKb4Graphics_AtTheOriginalCallSite` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Marker_AppearsLeftOfTheEntryUnderTheMouse_AndIsRemovedWhenLeaving` | neu | rot: U06 |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal` | neu | rot: U02, U03, U04, U05 |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Marker_NotOnEmptyRows` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Info_OfARemakeSave_ShowsOnlyWhileTheRightButtonIsHeld` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Info_OfAnOriginalSave_ComesFromTheHeader_AsInFile46` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Info_ShowsNoLeagueInWorldOnlyMode` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Info_OfAForeignFile_ShowsTheNoSaveGameMessage` | neu | offen |
| `SeasonEndColorsTests.cs · SeasonEndColorsTests.Palette18_12_22_MatchDosBoxPictures` | geändert | offen |
| `StadiumArrowTests.cs · StadiumArrowTests.TicketArrows_AreTenPixelsWide_FromKb4BaseGraphic` | neu | offen |
| `StadiumPartsTests.cs · StadiumPartsTests.EveryPartIsDrawnFromItsTableEntryWithMask0xC0` | neu | offen |
| `StadiumPartsTests.cs · StadiumPartsTests.HallCoversRestaurantInDrawOrder` | neu | offen |
| `StadiumPartsTests.cs · StadiumPartsTests.ProjectUnderConstructionShowsOnlyHintAndQuestionUsesOriginalDialog` | neu | offen |
| `StadiumPartsTests.cs · StadiumPartsTests.ConstructionQuestion_TextRowsAndButtons_LikeDosbox` | neu | offen |
| `StadiumPartsTests.cs · StadiumPartsTests.Hotspot_EdgesAreInclusive_LikeOriginal` | neu | offen |
| `StadiumPartsTests.cs · StadiumPartsTests.Hotspot_FirstHitWins_InOrderOfTheOriginal` | neu | offen |
| `TacticsClubScreenTests.cs · TacticsClubScreenTests.Lineup_RefusesTwentyThirdDressed` | geändert | offen |
| `TacticsClubScreenTests.cs · TacticsClubScreenTests.Tactics_OnlySemiEditable` | geändert | offen |
| `TacticsClubScreenTests.cs · TacticsClubScreenTests.Tactics_ModeArrows_LowerHalfIsNext_NoWrap` | neu | offen |
| `TacticsClubScreenTests.cs · TacticsClubScreenTests.Tactics_GoalieArrows_LowerHalfIsNext_NoWrap` | neu | offen |
| `TeamScreenTests.cs · TeamScreenTests.Lineup_ClickPlayerThenSlot_ChangesGoalkeeper` | geändert | offen |
| `TeamScreenTests.cs · TeamScreenTests.Tactics_ModesBlocksAndSwitchToLineup` | geändert | offen |
| `TransferBuyWindowDosboxTests.cs · TransferBuyWindowDosboxTests.BuyWindow_IsLeftOverSquadList_WithoutButtons_AndSolidShadow` | neu | offen |
| `TransferVertraegeKomplettTeil3ScreenTests.cs · TransferVertraegeKomplettTeil3ScreenTests.ClickOnOfferedPlayer_OpensOfferWindow_SellMovesPlayer` | geändert | offen |
| `TransferVertraegeKomplettTeil4ScreenTests.cs · TransferVertraegeKomplettTeil4ScreenTests.HoveredEntry_RecolorsSeparatorsAboveAndBelow` | geändert | offen |
| `TransferVertraegeKomplettTeil5ScreenTests.cs · TransferVertraegeKomplettTeil5ScreenTests.MarketBid_OpensOfferWindow_SellCreditsFee` | geändert | offen |
| `TransferVertraegeKomplettTeil5ScreenTests.cs · TransferVertraegeKomplettTeil5ScreenTests.MarketBid_KeepDeclines` | geändert | offen |
| `WaveBReviewScreenTests.cs · WaveBReviewScreenTests.PR3502_SecondBidBecameInvalid_ShowsMessageAndDoesNotReopenSameBid` | geändert | offen |
| `WettbewerbeNationalteamTests.cs · WettbewerbeNationalteamTests.CoachWarning_Boundary` | geändert | offen |

### Offene Prüfbereiche

- Die Inventur enthält noch `offen` markierte Testmethoden; alle Theoriezeilen, geänderten Testhilfen, Datenquellen und entfernten Tests bedürfen vollständiger Einzelprüfung.
- Geänderte Sollwerte, Fingerabdrücke und Saaten sind noch nicht vollständig einzeln auf ihre Regel zurückgeführt.
- Die Entscheidungen der Runde einschließlich PR-Kommentaren und der Nutzerentscheidungen vom 09.11.2026 sind noch nicht vollständig einzeln gegen den Code geprüft.
- Nicht alle neuen `[ORIGINAL]`-Fundstellen sind selbst in `em.exe` beziehungsweise den Originalständen nachgelesen.
- Nicht jeder Bildschirm und jedes Hauptmenüereignis ist vollständig gegen AGENTS.md § 4 geprüft; die in Teil 2 genannten Lücken bleiben offen.

Jeder offene Prüfbereich bedeutet Merge-Empfehlung **Nein**.
