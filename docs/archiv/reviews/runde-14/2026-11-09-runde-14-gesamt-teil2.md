# Gesamtreview Runde 14 – Fortsetzung, Teil 2

## 1. Merge-Empfehlung

**Nein** für den geprüften Head `b13046422b285404ad6ddbe7b5f1b18e22069eea` (Rundenbase `401efa7`). Es bestehen offene Prüfbereiche der ausdrücklich verlangten Vollprüfung sowie zwei reproduzierte P3-Lücken der Bildschirmtests. Dieser Bericht ergänzt [Teil 1](https://github.com/kaaasi/eishockey-manager-redone/blob/a9ef4ebc07f5165f85df084fef3dc084eed7ff8b/docs/reviews/2026-11-09-runde-14-gesamt.md); er ersetzt dessen Prüfungen nicht. Auch ein grüner Testlauf gibt keine Freigabe bei offenen Prüfbereichen.

## 2. Befunde

### R14G2-01 (P3): Zustand der Bundestrainer-Fabrik wird erst nach ihrem Aufruf gesichert

In [BildschirmeFreipruefungTests.cs:229](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/tests/EishockeyManager.UI.Tests/BildschirmeFreipruefungTests.cs#L229) ruft der neue Test `NationalMenu.Open` auf, bevor er `SaveGame.Save` und `StateFingerprint` als Ausgangszustand erhebt. Die allgemeine Reflexionsprüfung verwendet für den Kachelnamen nur `""` und stuft `NationalMenu.Open` als erwartete Vorbedingung ein; sie prüft daher die echten Kacheln an dieser Stelle nicht.

**Gegenbeispiel UI-B:** In einer isolierten Kopie direkt vor `return tile switch` in `NationalMenu.Open` für `tile == "Aufstellung"` `game.ActiveManager.Money++` ergänzen. Der neue Kacheltest und alle drei `AllScreens_OpeningAndDrawingDoNotChangeGameState`-Varianten bleiben **4/4 grün**, obwohl der Fabrikaufruf den Spielzustand ändert. Den Testvergleich vor `Open` verschieben: der Kacheltest wird **rot** mit `Kachel Aufstellung: Bytes weichen ab`. Das belegt eine Lücke der Prüfung; im unveränderten Produkt wurde hier keine solche Mutation festgestellt. Vorschlag: vor dem Fabrikaufruf Bytes und Fingerabdruck sichern, auch null liefernde Kacheln auf unerwartete Änderungen prüfen.

### R14G2-02 (P3): Saisonende-Hauptbildschirm wird ohne Zustandsvergleich geöffnet

Die in Runde 14 geänderte Freiprüfung verweist bei [BildschirmeFreipruefungTests.cs:35](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/tests/EishockeyManager.UI.Tests/BildschirmeFreipruefungTests.cs#L35) für `SeasonEndScreen` auf den Sondertest. Dieser erreicht den Bildschirm jedoch bereits durch `d.Advance()` und sichert erst danach den Zustand; er vergleicht das Öffnen von `SeasonEndSummaryScreen` und `ContractsScreen`, nicht von `SeasonEndScreen` selbst ([BildschirmeOeffnenZustandTests.cs:73](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/tests/EishockeyManager.UI.Tests/BildschirmeOeffnenZustandTests.cs#L73)). Die Testlücke bestand bereits vor der Runde; die geänderte Freiprüfung weist sie nicht als offen aus.

**Gegenbeispiel UI-A:** In der Kopie `SeasonEndScreen.Enter` ergänzen, sodass nur bei vorhandenem Saisonrückblick `game.Managers[0].Money++` ausgeführt wird. Der Saisonende-Sondertest und alle drei allgemeinen Freiprüfungen bleiben **4/4 grün**. Einen zusätzlichen Vergleich direkt vor und nach `Host.Show(new SeasonEndScreen(...))` im erreichten Saisonende einfügen: **rot**, gespeicherte Bytes unterscheiden sich. Im unveränderten `SeasonEndScreen` ist diese Mutation nicht vorhanden. Vorschlag: validen Saisonendzustand herstellen und Konstruktor, `Show`, Zeichnen und eingabelose Frames mit davor erhobenem Zustand prüfen.

## 3. Ausgeführte Prüfungen und offene Prüfbereiche

### Prüfstand und Originalstellen

`git fetch origin` ergab `main` = `b13046422b285404ad6ddbe7b5f1b18e22069eea`. Die Prüfkopien wurden aus `git archive` des genannten Heads erzeugt; nur dort wurden Produktcode oder Tests für Mutationen geändert. Das Repository selbst behielt den ungeänderten Produktstand. `/workspace/.dotnet/dotnet` ist SDK 10.0.402; Originaldaten und die Spielstände des Referenzstands `779a1c8af8597adf51a8538d87cfb5a228179f79` wie in Teil 1. Die Ausführung der 14 Testfälle aus `LoadQuirkTests` und `SaisonManagerTagTests` an der nicht mutierten Kopie war grün. `AllScreens_OpeningAndDrawingDoNotChangeGameState` lief auch direkt am geprüften Head mit **3/3 bestanden**. `tools/pruefen.sh` wurde für diesen unveränderten Head in Teil 1 mit allen drei Läufen abgeschlossen; hier nicht erneut wiederholt.

Selbst nachgelesen in `em.exe` mit `objdump -D -b binary -m i8086 -Mintel`: Datei `0x31BED–0x31C0A` liest die Sieger aus `0x5D3B` und die Zielindizes aus `0x144`, schreibt nach `0x5D3A + Index`; die Bytes ab `0x16DD0 + 0x144 = 0x16F14` lauten `09 18 0B 18 0A 18 0C 18 0D 18 0E 18` und ergeben die in Teil 1 geprüfte Halbfinalpaarung. `0x55F22–0x55F36` vergleicht das 32-Bit-Konto mit `FFE1:7B80` = **−2.000.000 Drittel**, mit strengem Unterlauf. Dies stützt die Schwelle des Zwangsabstiegs; die gesamte weitere Originalkontrolle und die Abwesenheit sämtlicher Entlassungszweige sind damit nicht bewiesen.

### Gegenmutationen dieser Fortsetzung

Alle Mutationen einzeln in `/tmp/r14-teil2`, jeweils nach vorherigem Zurücksetzen; `dotnet test ... --filter ...` mit `EM_REQUIRE_GAMEDATA=1`, `EM_SAVEGAMES=/tmp/r14-referenz/original/spielstaende`, SDK 10.0.402. Die zwei UI-Lücken wurden getrennt in `/tmp/r14-ui-teil2` mutiert und mit je einem verschärften Kontrolltest bestätigt. Mit unverändertem Produktcode bestanden beide verschärften Kontrollen (2/2). Grüne Tests am Head beziehungsweise in der unveränderten Kopie sind durch Teil 1 und die oben genannten Läufe belegt.

| ID | Regelmutation | Ergebnis |
|---|---|---|
| M01 | `SavedOnMarkerDay`: bei ungleichem Index trotzdem `true` | `OnlyMarkerDaySaves_GetTheOffset` rot: statt FILE22/FILE44 weitere Stände |
| M02 | `EndLoadCounterOffset` setzt 1 statt 0 | `File44_Offset_SurvivesSave_EndsAtSeasonChange_FourSeasons` rot, beide `active`-Werte |
| M03 | Initialwert von `LoadCounterOffset` = 1 | `NewGame_HasNoOffset` rot: 1 statt 0 |
| M04 | Migration 46 → 47 ergänzt 1 statt 0 | `Schema46_WithoutOffset_LoadsWithZero` rot: 1 statt 0 |
| M05 | `ManagerDayFlag` immer `false` | `ManagerPlaysCupDay_…` rot (15 schnelle Paarungen statt 0); `MultiSeason_…` bleibt in allen vier Varianten grün. Letzterer Test belegt damit nicht die Wirkung der Option. |
| M06 | Tagesmerker allein aus Option, ohne `ManagerPlaysOn` | `NoManagerInvolved_…` rot wegen abweichendem Zufallszustand |
| M07 | Optionsvorgabe `ManagerDayMinuteLoop = true` | `Default_IsOff_AndSaveRoundTripKeepsIt` rot |
| M08 | Migration 39 → 40 ergänzt Option `true` | `Schema39Save_Migrates_OptionOff` rot |
| M10 | Beim Saisonwechsel Absteiger in ihrer alten Liga belassen; Aufsteiger wechseln weiter | `MultiSeason_…` rot in vier Varianten: Ligabelegung 14 statt 12. Dies prüft die Ligagröße, nicht die Funktionsweise der Option. |
| M11 | Ab Tag 56 bei Konto unter −1.500.000 DM `Dismissed = true` | `DeepDebts_NoDismissal_GameContinues` und beide Saaten von `PassiveBundesligaManager_…` rot |
| M12 | Zwangsabstiegsgrenze auf −1.000.000.000.000 Drittel setzen | `PassiveBundesligaManager_…` rot für beide Saaten: kein Ereignis `ForcedRelegation` |
| UI-A | gültiges `SeasonEndScreen.Enter` verändert Konto | Allgemeine Prüfung 3/3 und Saisonende-Sondertest grün; mit erweitertem Vorher-nachher-Vergleich rot (R14G2-02) |
| UI-B | `NationalMenu.Open` verändert Konto bei „Aufstellung“ | Allgemeine Prüfung 3/3 und neuer Kacheltest grün; mit Vergleich vor Fabrikaufruf rot (R14G2-01) |
| UI-C | `StrengthScreen.Draw` verändert Konto | Allgemeine Freiprüfung rot in allen drei Zuständen |
| UI-D | `StrengthScreen.Update` verändert Konto ohne Eingabe | Allgemeine Freiprüfung rot in allen drei Zuständen; je drei Update-Aufrufe sichtbar |

M09 (früherer Versuch mit einem nicht-null-sicheren Vergleich) erzeugte eine Ausnahme vor der Sollwertprüfung und wird nicht als Regelgegenprobe gewertet; M10 ersetzt ihn.

### Inventur der neuen und geänderten Testmethoden

Ein temporäres Roslyn-Programm verglich sämtliche per `git diff --name-only 401efa7 b130464 -- tests/**/*.cs` erfassten Dateien: Methoden mit `Fact`, `Theory`, `GameDataFact` oder `GameDataTheory` anhand ihrer normalisierten Syntax einschließlich Attribute und Methodenkörper. Ergebnis **334 Kandidaten** (259 neu, 75 geändert; Core 240, Formats 4, UI 90). Die Tabelle nennt je erkannter Methode die bisherige Gegenprobe; `offen` bedeutet, dass **keine** erfolgreiche Einzelgegenprobe durch dieses Gesamtreview vorliegt. Die grünen Standardläufe aus Teil 1 sind keine Ersatz-Gegenproben. Eine Methodenliste erfasst geänderte gemeinsame Helfer, Datenquellen von `MemberData`, gelöschte Tests und Abhängigkeiten nicht automatisch: diese müssen zusätzlich anhand des Diffs geprüft werden. Theoriezeilen sind nur dort einzeln abgedeckt, wo das Ergebnis es ausdrücklich sagt.

| Testdatei / Methode | Delta | Gegenprobe |
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
| `LoadQuirkTests.cs · LoadQuirkTests.OnlyMarkerDaySaves_GetTheOffset` | neu | M01: rot |
| `LoadQuirkTests.cs · LoadQuirkTests.File44_AfterImport_SecondDayChange_FifteenthOfApril` | neu | Teil 1: rot bei Versatz 0 |
| `LoadQuirkTests.cs · LoadQuirkTests.File44_Offset_SurvivesSave_EndsAtSeasonChange_FourSeasons` | neu | M02: rot, beide Varianten |
| `LoadQuirkTests.cs · LoadQuirkTests.NewGame_HasNoOffset` | neu | M03: rot |
| `LoadQuirkTests.cs · LoadQuirkTests.Schema46_WithoutOffset_LoadsWithZero` | neu | M04: rot |
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
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.ClubOnly_WorldCupPeriod_NoTrainingNoMarketNoDrift` | neu | offen |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.Mode4_NoCoach_TrainingOnlyManagerZeroAfterWorldCupDriftDays` | neu | offen |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.Mode4_Coach_TrainingOnlyAfterWorldCupDriftDays` | neu | offen |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.Mode4_FourSeasonChanges_WorldCupLoopWithAndWithoutCoach` | neu | offen |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.WorldOnly_OneWorldCup_AutoOnOff` | neu | offen |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.WorldCupLoop_MarketDecay_LikeFile18To48AndFile19To20` | neu | offen |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.WorldCupLoop_MarketDecay_FloorAtMinStrain` | neu | offen |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.WorldOnly_InjuredPlayer_HealsTwiceOverWorldCup` | neu | offen |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.Mode4_NoCoach_HealingFollowsDayCounter` | neu | offen |
| `NationalteamRunde14Teil4Tests.cs · NationalteamRunde14Teil4Tests.Mode4_WorldCupLoop_TalentSearchStands` | neu | offen |
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
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_PoolFromOriginal` | neu | offen |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_MarketFromOriginal` | neu | offen |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_MarketListingOfManager` | neu | offen |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_PoolNamesUnique_AndContinue` | neu | offen |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_NationalSquadFromOriginal` | neu | offen |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_MessagesFromOriginal` | neu | offen |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_MessageAboutPoolPlayer` | neu | offen |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_RoundTripWithPoolMarketNational` | neu | offen |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_RelegationGroups_FromOriginal` | neu | offen |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_Playoffs_FromOriginal` | neu | offen |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_SeasonChange_LeaguesLikeNextOriginalStand` | neu | offen |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_AllStands_PostseasonMatchesOriginal` | neu | offen |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_AllStands_FourSeasonChanges` | neu | offen |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_StocksFromOriginal` | neu | offen |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_StockHoldings` | neu | offen |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_TrainingPlan` | neu | offen |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_TacticsAndAutoLineup` | neu | offen |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_TacticModes` | neu | offen |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_SeriesRecordsAndTableFields` | neu | offen |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_RankSlotsMatchTable` | neu | offen |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_Sponsoring` | neu | offen |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_BankAndManagerLoans` | neu | offen |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_NationalAccountAndTalentSearch` | neu | offen |
| `OriginalImportRestTests.cs · OriginalImportRestTests.Import_NewMessageFlag` | neu | offen |
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
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Playoffs_NextRound_PairsByTable0x144_HomeAtSmallerSlot` | neu | Teil 1: rot bei anderer Halbfinalpaarung |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.MultiSeason_FourChanges_PlayoffsPairByTable0x144` | neu | offen |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Playoffs_OwnRounds_PairLikeTheOriginal` | neu | offen |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Import_RunningQuarterFinal_SwapsAtOddGameCount` | neu | offen |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Import_RelegationGroup_ThreeOfSixDates` | neu | offen |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Import_OwnListedPlayer_OnlyOnMarket` | neu | offen |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Import_CarriesPerCompetition_AllStands` | neu | offen |
| `PlayoffSemifinalTests.cs · PlayoffSemifinalTests.Load_Schema43_RunningQuarterFinal_SemifinalByTable0x144` | neu | offen |
| `PruefungKarteRunde14Tests.cs · PruefungKarteRunde14Tests.Schema45_WithDebtDaysAndDismissed_LoadsKeepsDismissed_EndsGame` | neu | offen |
| `PruefungKarteRunde14Tests.cs · PruefungKarteRunde14Tests.ListedInjuredOwnPlayer_HealsOnTheHealingTick` | neu | offen |
| `PruefungKarteRunde14Tests.cs · PruefungKarteRunde14Tests.AutoLineup_OnlyOnTickOrEventDays` | neu | offen |
| `RngTwinProbeTests.cs · RngTwinProbeTests.FourSeasonChanges_FingerprintUnchangedBySerializableGenerator` | geändert | offen |
| `RngTwinProbeTests.cs · RngTwinProbeTests.FourSeasonChanges_OriginalDatabase_FingerprintUnchanged` | geändert | offen |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.ManagerPlaysCupDay_OptionOn_AllPairingsUseMinuteLoop_RandomDiffers` | neu | M05: rot |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.NoManagerInvolved_OptionDoesNothing_SameRandom` | neu | M06: rot |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.Default_IsOff_AndSaveRoundTripKeepsIt` | neu | M07: rot |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.Schema39Save_Migrates_OptionOff` | neu | M08: rot |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.MultiSeason_FourSeasonChanges_BothSettings` | neu | M05: grün trotz abgeschalteter Option; M10: rot, alle vier Varianten |
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
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` | neu | Teil 1: rot bei Versatz 0; mit exaktem Sollwert geprüft |
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
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds` | neu | Teil 1: rot bei Sommerbeginn 30. April |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_LoadQuirkBooksAprilMonthEnd_LikeFile46_2` | neu | offen |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation` | neu | offen |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead` | neu | M11 und M12: rot, beide Saaten |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.DeepDebts_NoDismissal_GameContinues` | neu | M11: rot |
| `TaktikTeil5Tests.cs · TaktikTeil5Tests.Manual_KeepsEffortAndAttack_DuringMatch` | geändert | offen |
| `TeamValueDriftTests.cs · TeamValueDriftTests.GameStart_DriftsIntoLeagueBounds` | geändert | offen |
| `TransferOriginalTests.cs · TransferOriginalTests.MultiSeason_Transfers_StayConsistent` | geändert | offen |
| `TransferOriginalVertragTests.cs · TransferOriginalVertragTests.Lock_RefusesInput_AndCountsDownDaily` | geändert | offen |
| `TransferTransactionRegressionTests.cs · TransferTransactionRegressionTests.R209_InvalidBidCannotBookOrMove` | geändert | offen |
| `TransferVertraegeKomplettTeil4Tests.cs · TransferVertraegeKomplettTeil4Tests.SponsorRating_PlayerWithBid_DrawsValueAgainBeforeStarCheck` | geändert | offen |
| `WaveBReviewRegressionTests.cs · WaveBReviewRegressionTests.PR3502_BidThatBecameInvalid_IsRejectedWithResultAndRemoved` | geändert | offen |
| `WettbewerbeSpiel12Teil3Tests.cs · WettbewerbeSpiel12Teil3Tests.MultiSeason_ComputerClubsPlayWithoutSquad` | geändert | offen |
| `WettbewerbeSpiel1Tests.cs · WettbewerbeSpiel1Tests.DebCup_NoMatchEndsLevel_ShootoutInFinalScore` | geändert | offen |
| `OriginalSaveHeaderTests.cs · OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | neu | offen |
| `OriginalSaveHeaderTests.cs · OriginalSaveHeaderTests.TryRead_ReadsOnlyTheHeader_NoLengthOrChecksumCheck` | neu | offen |
| `OriginalSaveHeaderTests.cs · OriginalSaveHeaderTests.TryRead_LimitsManagersToFour` | neu | offen |
| `OriginalSaveHeaderTests.cs · OriginalSaveHeaderTests.TryRead_WrongMagicOrTooShort_ReturnsNull` | neu | offen |
| `BildschirmeFreipruefungTests.cs · BildschirmeFreipruefungTests.AllScreens_OpeningAndDrawingDoNotChangeGameState` | geändert | UI-C und UI-D: rot 3/3; UI-A und UI-B: grün trotz Mutation (Befunde) |
| `BildschirmeFreipruefungTests.cs · BildschirmeFreipruefungTests.NationalMenu_Open_AllCoachTiles_DoNotChangeGameState` | neu | UI-B: grün trotz Mutation; mit Vergleich vor Open rot (Befund R14G2-01) |
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
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Paging_OnlyThroughArrowPair_InStepsOfFortyEight_NoEffectAtTheEnds` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Paging_WheelAndKeys_DoNothing` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Panel_ShowsNoPageLabel` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Arrows_AreTheKb4Graphics_AtTheOriginalCallSite` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Marker_AppearsLeftOfTheEntryUnderTheMouse_AndIsRemovedWhenLeaving` | neu | offen |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal` | neu | offen |
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

**Offener Prüfbereich – Tests:** Alle Zeilen `offen`, weitere möglicherweise durch gemeinsame Hilfen und Testdaten geänderte Tests sowie nicht einzeln nachgewiesene Theoriezeilen müssen anhand von Diff und Baseline vollständig erfasst und pro Test gezielt rot gemacht werden. Die 334 Kandidaten sind eine Arbeitsliste, kein Vollständigkeitsbeweis. Grund: Das Delta enthält 104 geänderte Testdateien und zahlreiche produktspezifische Mutationen; die verbleibenden Gegenproben sind in dieser Sitzung nicht ausgeführt.

**Offener Prüfbereich – Sollwerte, Fingerabdrücke, Saaten:** Die vollständige Einzelinventur und ursächliche Rückmutation jedes neuen/geänderten Werts fehlen weiterhin. Die oben nachgewiesenen Werte und Regeln decken nur einzelne Fälle ab. Grund: noch nicht abgearbeiteter Umfang.

### Bildschirmprotokoll

Die unveränderte Produktkopie lief mit dem vorhandenen Reflexionstest, ergänzt ausschließlich um eine Protokollzeile nach erfolgreichem Öffnen. Drei Zustände (Start, Mitte, Bundestrainer): **117 / 117 / 118 Öffnungen**, **0 / 0 / 0 Abweichungen**, darunter Konstruktorvarianten und Fabriken. Der Vergleich umfasst Speicherdaten und Fingerabdruck, `Draw` und drei Frames ohne Eingabe. Die Tabelle zählt erfolgreiche Konstruktorvarianten je Typ und Zustand (Start / Mitte / Bundestrainer), keine unabhängige Prüfung aller Spielzustände oder Benutzereingaben. `MainMenuScreen` hat ausdrücklich erlaubte Kernereignisse; der Vergleich im bereits aufgebauten Zustand beweist nicht, dass alle Hauptmenü-Ereignisse richtig und genau einmal ausgelöst werden.

| Bildschirmtyp | erfolgreiche Konstruktorvarianten S/M/B | Ergebnis / Grenze |
|---|---:|---|
| `Competitions.CupPairingsScreen` | 6/6/6 | keine Abweichung in diesen Zuständen |
| `Competitions.DebCupDrawLiveScreen` | 3/3/3 | keine Abweichung in diesen Zuständen |
| `Competitions.DebCupDrawScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Competitions.DebCupGamesScreen` | 2/2/2 | keine Abweichung in diesen Zuständen |
| `Competitions.EuropeanCupScreen` | 3/3/3 | keine Abweichung in diesen Zuständen |
| `Competitions.FriendliesScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Competitions.LeaguePairingsScreen` | 6/6/6 | keine Abweichung in diesen Zuständen |
| `Competitions.NationalLineupScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Competitions.NationalSquadScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Competitions.WorldCupKnockoutScreen` | 3/3/3 | keine Abweichung in diesen Zuständen |
| `Competitions.WorldCupScreen` | 3/3/3 | keine Abweichung in diesen Zuständen |
| `Competitions.WorldCupStatisticsScreen` | 2/2/2 | keine Abweichung in diesen Zuständen |
| `CreditsScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `GameOptionsScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `InfoFileScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `MainMenuScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `ManagerArea.AdvertisingScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `ManagerArea.FinanceBookScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `ManagerArea.FinanceScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `ManagerArea.InboxScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `ManagerArea.StadiumScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `ManagerArea.YouthScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `MatchDayScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `PlaceholderScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `SaveLoad.MogelguruScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `SaveLoad.SaveLoadScreen` | 3/3/3 | keine Abweichung in diesen Zuständen |
| `SeasonEndGameOverScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `SeasonEndTrophyScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `SettingsScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `SetupScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Statistics.ClubStatisticsScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Statistics.EternalTableScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Statistics.FairplayScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Statistics.HighscoreScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Statistics.HistoryScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Statistics.PlayerStatisticsScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Statistics.StrengthScreen` | 3/3/3 | keine Abweichung in diesen Zuständen |
| `TableScreen` | 3/3/3 | keine Abweichung in diesen Zuständen |
| `Team.ContractsScreen` | 2/2/2 | keine Abweichung in diesen Zuständen |
| `Team.LineupScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Team.TacticsScreen` | 2/2/2 | keine Abweichung in diesen Zuständen |
| `Team.TrainingScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `Team.TransferScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `TitleScreen` | 1/1/1 | keine Abweichung in diesen Zuständen |
| `SeasonEndScreen` | 0/0/0 | Voraussetzung fehlt; eigener Sondertest ohne Vergleich des Öffnens: R14G2-02 |
| `SeasonEndSummaryScreen` | 0/0/0 | Voraussetzung fehlt; Sondertest vergleicht Öffnen/Zeichnen im Saisonende, Update-Frames dort nicht gesondert nachgewiesen |

Nicht erzeugt: in allen drei Zuständen die Konstruktoren der beiden Saisonendbildschirme, `SeasonEndSummaryScreen.Open` und `NationalMenu.Open` mit leerem Namen; in Start/Mitte zusätzlich `NationalLineupScreen.Tactics` ohne Bundestrainer. Die separat geprüften echten Bundestrainer-Kacheln haben die Lücke R14G2-01. Nur ein Satz ausgewählter Argumentvarianten wurde für Konstruktoren mit Parametern getestet; diese Ergebnistabelle allein ist keine vollständige Prüfung aller Bildschirmzustände und Hauptmenüereignisse.

**Offener Prüfbereich – Bildschirme und Hauptmenü:** R14G2-01/-02 schließen die behauptete Vollabdeckung aus. Die Sonderfälle, die übrigen gültigen Spielzustände, sämtliche Fabrikwege einschließlich vor dem Fabrikaufruf sowie jeder Aufbau- und Zugwechselpfad (`OnMainMenu`, Nachrichten, Aufstellung, Bundestrainer, Gehaltsfaktor, Laden/Hotseat) sind weiter einzeln zu prüfen. Grund: Reflexion und ausgewählte Zustände reichen nicht für alle Pfade.

**Offener Prüfbereich – Entscheidungen:** Die Einträge aus Teil 1 bleiben offen, soweit dort nicht belegt. Für R14-D4 zeigen M05/M06 die Wirkung des Tagesmerkers und die Gegenprobe ohne Manager; Vollprüfung aller Wettbewerbspfade fehlt. Für die Entscheidung keine Entlassung wegen Schulden zeigen M11/M12 die Wirksamkeit der Testfälle und die Originalschwelle; alle Original- und Codezweige bleiben offen. Für Entscheidung C zum Markierungstag prüfen M01–M04 Erkennung, Versatz, Zurücksetzen und Migration, weitere Import-/Menüpfade bleiben offen. Alle weiteren Entscheidungen und genannten PR-Kommentare müssen noch einzeln gegen den Code abgeglichen werden.

**Offener Prüfbereich – Originalfundstellen:** Der Diff enthält 415 hinzugefügte Zeilen mit `[ORIGINAL]` in `src`, `tests` und `docs`; Zeilen sind weder gleich Fundstellen noch vollständig für geänderte mehrzeilige Belege. Außer den hier und in Teil 1 ausdrücklich genannten Stellen sind sie nicht vollständig einzeln in `em.exe` bzw. den Spielständen nachgelesen. Grund: Umfang; `objdump` ist für bekannte Instruktionsgrenzen verfügbar, `ndisasm` fehlt.

### Akzeptierte `[APPROX]` und Fortsetzung

Keine weitere `[APPROX]`-Abnahme. Erwartet offen bleiben #620 (Rest), #698, #699, #703 und die April-Abweichung 115.000 DM wie in Teil 1; ihr Status wurde dort bestätigt und hier nicht erneut recherchiert.

Fortsetzung in neuem Chat: `Fortsetzung Gesamtreview, Bericht docs/reviews/2026-11-09-runde-14-gesamt-teil2.md`. Nächste Datei `docs/reviews/2026-11-09-runde-14-gesamt-teil3.md` auf neuem Branch; zuerst die offenen Testzeilen und gemeinsame Helfer systematisch bearbeiten, dann Sollwerte/Saaten, Entscheidungen, Originalstellen und verbleibende Bildschirmwege vollständig prüfen. Solange ein Bereich offen ist: Merge-Empfehlung **Nein**.
