# Gesamtreview Runde 13 – Nachprüfung (Vollprüfung)

Claude-Lauf statt ChatGPT (Entscheidung Nutzer 05.11.2026, Codex ohne Netz), geplanter Lauf ohne Implementierungskontext.
Head `b634032433e389623d413b14441ceb65fabdb6a5` (Branch `fix/r13-nummerierung`, PR #615, enthält `main` @ `b0688af`), Gesamt-Base `8afd750bfc0cc57948041ca8906db6924caa7eb5`.
Ersetzt den Zwischenstand dieser Datei. Gegenproben teils durch Hilfs-Agenten derselben Sitzung (Anhänge A–E); jede Tabellenzeile belegt einen ausgeführten Lauf.

## 1. Merge-Empfehlung

**Nein** für `b634032433e389623d413b14441ceb65fabdb6a5`. Grund: R13GN-01 (P2) – die mit `[ORIGINAL]` markierte Regel „Kauf hinten an“ (A091) widerspricht em.exe; Nr., Kaderfolge und Gleichstandsregel der Auto-Aufstellung weichen für gekaufte Torhüter und Verteidiger ab. Dazu P3-Befunde zu Tests, die ihre Regel nicht prüfen (R13GN-04), und zu nicht belegten Saat-Änderungen (R13GN-05). Der Fix #615 selbst ist in Ordnung: R13G-01 und #606 sind am Head behoben (Teil 3.2). Alle drei Läufe von `tools/pruefen.sh` am Head mit Originaldaten bestanden. Offene Prüfbereiche: siehe Ende von Teil 3.

## 2. Befunde

### R13GN-01 (P2): Kauf hängt jeden Spieler hinten an, das Original sortiert nach Spielerart ein

[`Player.Factory.cs:176`](https://github.com/kaaasi/eishockey-manager-redone/blob/b634032433e389623d413b14441ceb65fabdb6a5/src/EishockeyManager.Core/Player.Factory.cs#L176) (`[ORIGINAL] A091: neuer Kaderplatz hinten an (Datei 0x42CB1 …)`) hängt mit `club.Players.Add(p)` immer ans Ende. em.exe Datei 0x42D55–0x42DA1 sucht ab Platz 0 den ersten Platz, der leer ist oder dessen Art (0x4180: 0 Torhüter, 1 Verteidiger, 2 Stürmer) größer als die des Neuen ist, und schiebt die Folge danach per memmove nach hinten (0x42DC3 ff.). Nachgelesen mit `python3 tools/reverse-engineering/disasm.py gamedata/em.exe 0x42D40 0x42E30` (`cmp byte [bx+0x5dd2],0`/`jz`, `call 0x04180`, `cmp al,[bp-2]`/`ja`). FILE07 (DAFFNER, Stürmer, Platz 25) ist damit kein Beleg für Torhüter/Verteidiger. Gegenbeispiel (Probe am Head): `TestGames.Create(3)`, Torhüter eines anderen Vereins per `RemovePlayer` lösen, `PlayerFactory.Join(club, keeper, 2, false)` → Platz 25 (Nr. 26); laut 0x42CB1 Platz 2 hinter dem letzten Torhüter (Probe: erwartet 2, ist 25). Folgen: sichtbare Nr. (A091), Folge der Live-Mannschaft und „späterer Platz gewinnt“ in [`Squad.AutoLineup.cs:21`](https://github.com/kaaasi/eishockey-manager-redone/blob/b634032433e389623d413b14441ceb65fabdb6a5/src/EishockeyManager.Core/Squad.AutoLineup.cs#L21). Gleiche Aussage auch in [`MatchEngine.Managed.cs:79`](https://github.com/kaaasi/eishockey-manager-redone/blob/b634032433e389623d413b14441ceb65fabdb6a5/src/EishockeyManager.Core/MatchEngine.Managed.cs#L79), [`Player.Contract.cs:396`](https://github.com/kaaasi/eishockey-manager-redone/blob/b634032433e389623d413b14441ceb65fabdb6a5/src/EishockeyManager.Core/Player.Contract.cs#L396), [`SaveGame.Migration.Nummern.cs:7`](https://github.com/kaaasi/eishockey-manager-redone/blob/b634032433e389623d413b14441ceb65fabdb6a5/src/EishockeyManager.Core/SaveGame.Migration.Nummern.cs#L7), [`Squad.Strength.cs:34`](https://github.com/kaaasi/eishockey-manager-redone/blob/b634032433e389623d413b14441ceb65fabdb6a5/src/EishockeyManager.Core/Squad.Strength.cs#L34). Die Regressionstests in `KaderStartRunde13Teil4Tests` kaufen nur Stürmer und decken den Fall nicht ab.

### R13GN-02 (P3): Weitere ungenaue `[ORIGINAL]`-Fundstellen

Nachgelesen in em.exe (Anhang A, Widersprüche 2–8): [`GameState.SeasonEndTurn.cs:10`](https://github.com/kaaasi/eishockey-manager-redone/blob/b634032433e389623d413b14441ceb65fabdb6a5/src/EishockeyManager.Core/GameState.SeasonEndTurn.cs#L10) (0x27BC6 prüft „keine belegten Kaderplätze“, die Vertragsprüfung steht erst bei 0x27BCF–0x27BD8); [`ClubInfoWindow.cs:39`](https://github.com/kaaasi/eishockey-manager-redone/blob/b634032433e389623d413b14441ceb65fabdb6a5/src/EishockeyManager.UI/Screens/Competitions/ClubInfoWindow.cs#L39) („nur Code 7“, 0x50283 lässt 7 und 8 durch); [`Squad.Lineup.cs:233`](https://github.com/kaaasi/eishockey-manager-redone/blob/b634032433e389623d413b14441ceb65fabdb6a5/src/EishockeyManager.Core/Squad.Lineup.cs#L233) (Ersatzrunde „ohne Einstellung“: 0x426BC endet ohne Einstellung sofort bei 0x426C4–0x426E4 – betrifft Verhalten, Test `Dressed_WithoutAutoLineupRun_UsesReserveRound`); [`CupsNational.Lineup.cs:186`](https://github.com/kaaasi/eishockey-manager-redone/blob/b634032433e389623d413b14441ceb65fabdb6a5/src/EishockeyManager.Core/CupsNational.Lineup.cs#L186) (bei +0x09 & 3 = 3 keine Meldung); [`WorldCupScreen.cs:29`](https://github.com/kaaasi/eishockey-manager-redone/blob/b634032433e389623d413b14441ceb65fabdb6a5/src/EishockeyManager.UI/Screens/Competitions/WorldCupScreen.cs#L29) (auch DGROUP 0x1DB ≠ 0 verlässt die Seite); [`GameState.MainMenu.cs:58`](https://github.com/kaaasi/eishockey-manager-redone/blob/b634032433e389623d413b14441ceb65fabdb6a5/src/EishockeyManager.Core/GameState.MainMenu.cs#L58) (Adresse, schon #598). Fehlender `[APPROX]`-Marker für U15-c im Code (L2; `grep -rn "29CFA\|U15-c" src` → 0 Treffer, nur `docs/transfer.md:493`).

### R13GN-03 (P3): Tests, die ihre Regel nicht oder nur indirekt prüfen

Gegenproben (Anhänge C, D1, D2, E): `SaisonSatzfolgeSaisonendeTests.Preparing_NextSlots_DoesNotAdvanceGameRandom` lässt sich nicht rot machen (zwei gleich gesäte Spiele; Mischung aus `GameState.Random` bleibt grün, macht aber alle vier synthetischen RngTwin-Abdrücke rot). `SeasonEndColorsTests.Palette18_12_22_MatchDosBoxPictures` prüft nur Paletteneinträge, nicht die verwendeten Indizes (alte Indizes 238/13/151 bleiben grün). `StatistikenPraemienTests.MultiSeason_FourChanges_PassiveAndActiveManager` bildet die Erwartung mit `FairPlayPremiumWinner` selbst (Mutation nur der Siegerregel bleibt grün). `WettbewerbeNationalteamTests.CoachWarning_Boundary`: geänderter Zweig bleibt bei zurückgenommenem A028 grün. `NummerierungMigrationTests.CurrentSave_RoundTrip_Unchanged` erkennt eine auf aktuelle Stände angewandte Migration nicht. `Schema37Save_…StartNextSeasonFormsSlots` prüft das Mischen im Rückfallweg nicht. `BildschirmeFreipruefungTests` erkennt `OnMainMenu` im Zeichnen nicht (deckt `MainMenuDrawTests` ab) und prüft kein `Update` ohne Eingabe (`Game.Random`-Zug in `StadiumScreen.Update` bleibt in allen 7 Freiprüfungs-Tests grün; ergänzt #610). `SponsorOfferTests.Rating_FreshClub_…` nur über eine zweite, gröbere Mutation rot. Kein Produktfehler, aber Abnahmetests ohne Wirkung.

### R13GN-04 (P3): Saat-Änderungen und Kommentare zu Fingerabdrücken nicht belegt

Saaten (Anhang D1): Bei `SeasonEndStatusTests.Full_ManagerInTopFive_GetsStatusBit_OtherNot` und `…SaveLoad_StatusBitIsDerivedFromSavedData` ist die alte Saat 3 am Head grün, keine Kombination der Rücknahmen macht sie rot – die Begründung „Vorbedingung nach dem Starttausch“ trifft nicht zu. Bei `NationalCoachMode4Tests.Mode4_NoManagerInTopFive_NoCoach` und `WettbewerbeSpiel1Tests.DebCup_NoMatchEndsLevel_…` ist die alte Saat am Head grün (nur im Zwischenstand Teil 2 nötig). Saat-Änderungen in 9 fremden Testdateien sind von L13 („StartLeague passend zum festen Verein“) nicht gedeckt (in #578/#589 genannt). Fingerabdrücke (Anhang E): S-K6-a allein ändert keinen der acht Abdrücke (gleicher `SlotSeed`), ursächlich ist S-K6-c; „Originaldatenbank von U15-a unverändert“ in `RngTwinProbeTests.cs` und „aktiv unverändert“ in `SpielberechnungAnschlussTests.cs` stimmen am Head nicht. Ergänzt #604.

### R13GN-05 (P3): Veraltete Doku-Stellen

D3-Text in `docs/arbeitspakete.md:170` nicht an #614 angepasst; Platzhalter-Kommentar „schließt die Leitstelle bei der Integration“ in `SaveGame.cs:148`/`SaveGame.Migration.Saison.cs:28–31`; Begründung „nicht gesucht“ beim `[APPROX]` A088 (`MatchEngine.Live.cs:366`) überholt; Kommentarzeile für Schema 38 in `SaveGameTests` fehlt.

## 3. Ausgeführte Prüfungen und akzeptierte Annäherungen

**Umgebung:** Checkout `git rev-parse HEAD` = `b634032433e389623d413b14441ceb65fabdb6a5`, `git status --porcelain` leer; SDK 10.0.402 (Release `claude-werkzeuge`); Originaldaten aus `kaaasi/eishockey-manager-referenz` @ `36aeca95` (`git ls-remote` erfolgreich), `sha256sum -c original/SHA256SUMS` OK, `gamedata` verlinkt, `spielstaende/` daneben.

**`tools/pruefen.sh` am Head (alle drei Läufe), Ergebnis „bestanden“, Build ohne Warnungen:**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2266 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1901 | 0 | 327 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1902 | 364 | 0 |

### 3.1 Vorgegebene Gegenbeispiele

| Gegenbeispiel | Ausführung | `b0688af` | Head |
|---|---|---|---|
| R13G-01 (#613) | `FlowTests.CreateDriver()`, `StartNewGame()`, Automatik aus, `Players[0].Number = 77`, `Host.Show(new MainMenuScreen(Host))`, `Frame()` (Draw) | rot (erwartet 77, ist 1) | grün (77) |
| #606 | Echte Schema-36-Stände am Base `8afd750` erzeugt (`SaveGame.Save(TestGames.Create(21))`, Automatik aus; freie Nummern 99−3·i beim Managerverein 2 und Computerverein 0; einmal Spielbeginn, einmal nach 10 Spieltagen), am Head geladen | – | grün: in allen Vereinen Nr. = Kaderplatz + 1 (2/2); `OnMainMenu` danach ändert nur `_newMessage` |
| Migration 36 → 39 | beide Stände, je passiv/aktiv vier Saisonwechsel (`RngTwinProbeTests.Run`), Vergleich mit gespeicherter/geladener Kopie, danach Nr. = Platz + 1 | – | grün 4/4, Abdrücke und End-Spielstände bytegleich |

### 3.2 Teil 1 – jeder neue oder geänderte Test rot gemacht

Inventur: Methoden mit `[Fact]/[Theory]/[GameDataFact]/[GameDataTheory]`, deren Text sich zwischen Base und Head unterscheidet (Skript über `git show`): 75 neu, 40 geändert, 3 entfernt (ersetzt durch umbenannte Nachfolger: `Friendly_SetsGermanyRatingsFromCoachLineup` → `Friendly_KeepsGermanyRatings_…`, `WorldOnly_TwentySeasonChanges_NoExchange…` → `…_OnlyOberligaExchange…`, `Schema32Save_…_LoadsWithIndexOrder…` → `…_MigratesTo34…`). Tests mit unverändertem Text, aber geändertem Hilfscode (`Create` in Saisonwechsel-Konto/-Oberliga/-Zwangsabstieg, Saat in `WeltkugelDosboxTests`, Standard von `TestSeason.KeepSquad`/`KeepSquadHelper`) sind über Teil 2 (Saaten, R12K-03) und L13 abgedeckt.

Schritt 1: Head-Tests auf Base-Code (Kopie von `8afd750` mit `tests/` vom Head; neutrale Ersatzschnittstellen ohne Head-Verhalten: `NewGame.DatabaseLeague`, ignoriertes `GameSetup.StartLeague`, leeres `OnNationalLineupLeft`/`RenumberPlayers`, `RemovePlayer` ohne Umnummerieren, zusätzliche `LineupResult`-Werte); Methoden mit echten neuen Schnittstellen (`StartOrder`, `GameSetup.Step`, `ClickPlayer`, `NextLeagueSlots`, `FairPlayPremiumWinner`, neue UI-Eigenschaften) ausgeschlossen. Läufe `EM_REQUIRE_GAMEDATA=1 dotnet test … -c Release --no-build`. **41 Tests am Base rot:**

| Test | Art | Base | Meldung (gekürzt) |
|---|---|---|---|
| GameModeTests.WorldOnly_SeasonRunsThroughWorldCup_WithoutLeagueOrClubBookings | geändert | rot 1/1 | Assert.Equal() Failure: Values differ Expected: 2 Actual:   0 |
| IntegrationRunde8Welle3Tests.Friendly_KeepsGermanyRatings_LeavingLineupSetsThem | neu | rot 1/1 | Assert.Equal() Failure: Values differ Expected: Tuple (84, 91, 93, 91) Actual:   Tuple (80, 84, 82, 83) |
| JugendTeamsTests.FourSeasonChanges_StrengthFollowsFunding | geändert | rot 2/2 | Assert.Equal() Failure: Strings differ                            ↓ (pos 16) Expected: "0:Schueler:2/11/58;0:J |
| KaderStartRunde13Teil4Tests.AutoLineup_File05Values_GiveFile05Lineup | neu | rot 1/1 | Assert.Equal() Failure: Values differ Expected: 2 Actual:   0 |
| KaderStartRunde13Teil4Tests.AutoLineup_AfterPurchase_GivesFile07 | neu | rot 1/1 | Assert.Equal() Failure: Values differ Expected: 26 Actual:   1 |
| KaderStartRunde13Teil4Tests.AutoLineup_AfterSale_GivesFile26_PlacesMoveUp | neu | rot 1/1 | Assert.Equal() Failure: Collections differ                                            ↓ (pos 0) Expected: Rang |
| KaderStartRunde13Teil4Tests.SaleThenPurchase_NewPlayerLast_WinsTie | neu | rot 1/1 | Assert.Equal() Failure: Values differ Expected: 25 Actual:   1 |
| KaderStartRunde13Teil4Tests.LiveSquad_FollowsSquadSlots_NotRoleAndNumber | neu | rot 1/1 | Assert.Equal() Failure: Collections differ                                                                     |
| KaderStartRunde13Teil4Tests.Dressed_WithoutAutoLineupRun_UsesReserveRound | neu | rot 1/1 | Assert.True() Failure Expected: True Actual:   False |
| KaderStartRunde13Teil4Tests.MultiSeason_FourChanges_SlotOrderKept | neu | rot 2/2 | Assert.All() Failure: 36 out of 36 items in the collection did not pass. [0]:  Item:  Club { AutoLineupEnabled |
| KaderStartStartLigaTests.FixedBundesligaClub_SecondLeague_SwapsPlacesStrengthStays | neu | rot 3/3 | Assert.Equal() Failure: Values differ Expected: ZweiteLiga Actual:   Bundesliga |
| KaderStartStartLigaTests.NoLeagueClub_LandsInStartLeague_DisplacedLosesLeague | neu | rot 3/3 | Assert.Equal() Failure: Values differ Expected: ZweiteLiga Actual:   Oberliga |
| KaderStartStartLigaTests.Bundesliga_AlwaysDraws_NeverTheEuropeanCupPlace | neu | rot 1/1 | Assert.NotEqual() Failure: Values are equal Expected: Not 90 Actual:       90 |
| KaderStartStartLigaTests.TwoManagers_LaterNeverDrawsEarlierManagersClub | neu | rot 1/1 | Assert.Equal() Failure: Values differ Expected: Bundesliga Actual:   ZweiteLiga |
| KaderStartStartLigaTests.OriginalDatabase_KoelnSecondLeague_LikeFile27 | neu | rot 1/1 | Assert.Equal() Failure: Values differ Expected: ZweiteLiga Actual:   Bundesliga |
| KaderStartStartLigaTests.MultiSeason_FourChanges_AfterStartSwap | neu | rot 2/2 | System.InvalidOperationException : Sequence contains no matching element |
| FirstTurnFactorTests.NewGame_DrawsFactorFromTurnRandom_NotTheDerivedFallback | neu | rot 3/3 | Assert.Equal() Failure: Values differ Expected: 99 Actual:   91 |
| NationalteamRunde13Teil2Tests.A028_CoachHaltsOnEveryWorldCupDay | neu | rot 1/2 | Assert.Equal() Failure: Collections differ                                     ↓ (pos 5) Expected: int[]     [ |
| NationalteamRunde13Tests.A085_AutoNominate_TieKeepsCandidateOrder_NotName | neu | rot 1/1 | Assert.Equal() Failure: Collections differ                                          ↓ (pos 0) Expected: <gener |
| NationalteamRunde13Tests.A009_Friendly_DoesNotRecomputeNationalValues | neu | rot 1/1 | Assert.Equal() Failure: Values differ Expected: Tuple (84, 91, 93, 91) Actual:   Tuple (81, 84, 81, 83) |
| NationalteamRunde13Tests.A009_SingleForward_GivesFile20Values | neu | rot 1/1 | Assert.Equal() Failure: Values differ Expected: Tuple (0, 0, 9, 10) Actual:   Tuple (84, 91, 93, 91) |
| NationalteamRunde13Tests.A009_ValuesFollowLineupOnlyAtDayOrLeaving | neu | rot 1/1 | Assert.Equal() Failure: Values differ Expected: Tuple (PartValues { Condition = 79, Technique = 81, Form = 49  |
| NummerierungMigrationTests.OldSave_WithFreeNumbers_RenumbersAllClubsOnLoad | neu | rot 2/2 | Der Spielstand stammt aus einer neueren Version (Schema 38, unterstützt bis 36). |
| RngTwinProbeTests.FourSeasonChanges_FingerprintUnchangedBySerializableGenerator | geändert | rot 4/4 | Assert.Equal() Failure: Strings differ            ↓ (pos 0) Expected: "377126FABD83B898" Actual:   "946F98B1C2 |
| RngTwinProbeTests.FourSeasonChanges_OriginalDatabase_FingerprintUnchanged | geändert | rot 2/2 | Assert.Equal() Failure: Strings differ            ↓ (pos 0) Expected: "C2A2F88417587DD1" Actual:   "8911A65C5D |
| SaisonWeltkugelTests.WorldOnly_MultiSeason_OberligaExchangesLastTwoOfStartOrder_LeaguesOneTwoUnchanged | neu | rot 2/2 | Assert.Equal() Failure: Collections differ            ↓ (pos 0) Expected: [35, 34] Actual:   [] |
| SaisonWeltkugelTests.WorldOnly_MultiSeason_SeasonDriftMovesLeagueClubsWithinBounds_NationalTeamsKeepConditionAndTechnique | neu | rot 2/2 | Saisonwechsel 1: nur 0 Vereine gewandert |
| SaisonwechselProbenTests.WorldOnly_TwentySeasonChanges_OnlyOberligaExchange_NoForcedRelegation | neu | rot 2/2 | Assert.Equal() Failure: Values differ Expected: 2 Actual:   0 |
| SaisonwechselSatzfolgeTests.UnusualLeagueSize_AtSeasonChange_SlotsFallBackToIndexOrder | neu | rot 1/1 | Assert.Equal() Failure: Collections differ                      ↓ (pos 0) Expected: int[]     [0, 1, 2, 3, 4,  |
| SaveGameTests.SchemaFingerprint_MatchesSchemaVersion | geändert | rot 1/1 | Assert.Equal() Failure: Values differ Expected: 39 Actual:   36 |
| SpielberechnungAnschlussTests.FourSeasonChanges_OriginalDatabase_CountersPerSeason | geändert | rot 2/2 | Assert.Equal() Failure: Strings differ            ↓ (pos 0) Expected: "98AD5EF36A0277C8" Actual:   "75F5165C1D |
| SpielberechnungKomplett4Tests.StrengthTable_CoachNation_ShowsLineupValues | geändert | rot 1/1 |  |
| StatistikenPraemienTests.OtherClubLeads_NoSinglePremium | geändert | rot 1/1 | Assert.Contains() Failure: Item not found in collection Collection: [] Not found:  FairPlay |
| AdvertisingArrowTests.HitAreas_Are8x9_AtOriginalPositions | neu | rot 1/1 | Assert.Equal() Failure: Values differ Expected: Rect { X = 138, Y = 183, W = 8, H = 9, Right = 146, Bottom = 1 |
| AdvertisingArrowTests.PressedArrow_IsDrawnFromKb4 | neu | rot 1/1 | Assert.NotEqual() Failure: Collections are equal Expected: Not [29, 29, 29, 29, 29, ···] Actual:       [29, 29 |
| DebCupDrawFlowTests.MultiSeason_CupResultsUnchanged_WithAndWithoutDisplay | geändert | rot 1/1 | Assert.Equal() Failure: Strings differ                      ↓ (pos 10) Expected: "1993: DEB 21, DM 6, Tab 1537 |
| KaderStartSetupScreenTests.OneSeason_LocksStartLeague_ForAllManagers | neu | rot 1/1 | Assert.All() Failure: 1 out of 2 items in the collection did not pass. [1]: Item:  ManagerSetup { ClubIndex =  |
| NationalteamRunde13UiTests.A293_A294_LeftClickOutsideStays_GroupButtonExact_RightClickReturns | neu | rot 1/1 | Assert.Same() Failure: Values are not the same instance Expected: WorldCupScreen { Group = CupGroup { Number = |
| NationalteamRunde13UiTests.A293_NoKnockoutButton_KnockoutViaPairings | neu | rot 1/1 | Assert.Same() Failure: Values are not the same instance Expected: WorldCupScreen { Group = CupGroup { Number = |
| NewsBarShownTests.Shown_ResetsToNewest_WhenOneMessageDropsAndAnotherArrives | neu | rot 1/1 | Assert.Equal() Failure: Values differ Expected: 2 Actual:   1 |
| StadiumArrowTests.TicketArrows_UseOriginalAreas_AndStepOnlyOnPress | neu | rot 1/1 | Assert.Equal() Failure: Values differ Expected: Rect { X = 192, Y = 189, W = 8, H = 9, Right = 200, Bottom = 1 |

Schritt 2: Die übrigen 74 Tests (45 am Base grün, 29 am Base ohne Schnittstelle) per gezielter Mutation am Head (Umgebungsschalter `RVMUT`, Kopie des Heads, jeweils ohne Schalter grün bestätigt): Tabellen in Anhang C (UI, 19), D1 (kader-start, 26) und D2 (saison, nationalteam, übrige, 29). Ergebnis: 73 rot, 1 nicht rot zu machen (`Preparing_NextSlots_DoesNotAdvanceGameRandom`, R13GN-03); weitere Schwächen R13GN-03.

### 3.3 Teil 2 – Sollwerte, Fingerabdrücke, Saaten

Anhang E (Fingerabdrücke RngTwin 6, Spielberechnung 2, geänderte Sollwerte in geänderten Tests, fest eingetragene Sollwerte neuer Tests) und Anhang D1 (14 Saaten). Jeder Wert fällt bei Rückmutation seiner Regel, außer den in R13GN-04 genannten Begründungen und Saaten; SeasonEndColors siehe R13GN-03.

### 3.4 Teil 3 – Entscheidungen D1–D3, L1–L15

Anhang B1: 19 Zeilen (D1–D3, L1–L15, L5 gegen L14), 17 ok, 2 Abweichungen (L2: Marker fehlt im Code, R13GN-02; L13: Saat statt Start-Liga, R13GN-04). L5/L14 kein Widerspruch (Schema 38 vor L14 entstanden, L14 betrifft nur A088; Schema 39 aus #615).

### 3.5 Teil 4 – neue `[ORIGINAL]`-Fundstellen

Anhang A: 70 Fundstellen, 57 bestätigt, 12 teilweise, 1 widerlegt (R13GN-01), 0 offen; Widersprüche in R13GN-01/-02.

### 3.6 Teil 5 – Bildschirme und Hauptmenü-Ereignisse (AGENTS.md §4)

Anhang B2: kein geänderter Bildschirm ändert Zustand durch Öffnen, Zeichnen oder Frames ohne Eingabe (eigene Probe mit Spielstand-Bytes und `StateFingerprint`, drei Zustände); `OnMainMenu` ändert nur `_newMessage` und – nur mit Automatik – die Aufstellung, genau ein Aufruf je Aufbau; Zuganfang nur Gehaltsfaktor/Zugzufall/Manager am Zug; Saisonende-Seiten ändern `CurrentManager` nur auf Eingabe. Testlücken siehe R13GN-03.

### 3.7 Erwartet offen (bestätigt)

#591, #592, #594, #595, #597, #598, #603–#606, #610, #611 offen (P3 laut Titel, keine Labels gesetzt); #606 und #613 werden von #615 behoben (Teil 3.1). A088 bleibt `[APPROX]` (`MatchEngine.Live.cs:366`, L14); U15-c bleibt Annäherung, aber ohne Code-Marker (R13GN-02). #601 offen (Aufnahme `runde-13` für Runde 14).

### 3.8 Akzeptierte `[APPROX]` und Annahmen

Akzeptiert: Migration 38 → 39 als Remake-Anpassung ohne Gegenstück im Original (`SaveGame.Migration.Nummern.cs`); A088, A125 (Rückkehr hinten an), getrennte Ersatz-Zufallsquellen für Start-/Saisonmischung. Annahme: Ersatzschnittstellen am Base (Teil 3.2) bilden Base-Verhalten ab; Hilfs-Agenten-Ergebnisse wurden nicht alle einzeln wiederholt, R13GN-01 selbst nachgelesen und reproduziert.

**Offene Prüfbereiche:** (1) Nicht erzeugbare Bildschirme (SeasonEndScreen, SeasonEndSummaryScreen, `NationalMenu.Open`, `NationalLineupScreen.Tactics` ohne Bundestrainer) – im Testtreiber nicht erreichbar, nur am Code gelesen (#610). Sonst keine.

## Anhang A – `[ORIGINAL]`-Fundstellen

#### Review A – neue `[ORIGINAL]`-Fundstellen (Runde 8afd750 → b634032, `src/`)

Grundlage: `git -C /home/claude/wt-h2 diff -U0 8afd750 b634032 -- src` → 70 hinzugefügte Zeilen mit `[ORIGINAL]`
(Kommentare vollständig im Code nachgelesen). Checkout `wt-h2` nur gelesen (HEAD = b634032), nichts gebaut.

**Werkzeuge (ausgeführt):**

- Disassembly: `python3 tools/reverse-engineering/disasm.py em.exe <lo> <hi> --cache <scratch>/an.pickle` für alle 29
  Code-Regionen (Basissegmente 0x1E00–0x143A6, Overlays 0xC0–0x140) in eine Scratch-Datei; Auszüge mit
  `L <von> <bis>` (awk-Filter auf die Adressspalte). In der Tabelle steht `L a–b` für diesen Auszug.
- DGROUP-Querverweise: `disasm.py em.exe --xref <lo> <hi> [--imm]` (`xref`).
- DGROUP-Werte/Texte: Python, Datei-Offset = 0x16DD0 + DGROUP (`dg`), Fernzeiger Segment 0x14FD.
- Spielstände: eigener Entschlüssler nach `docs/formate/spielstand-original.md` (`k` ab Byte 0xB9, `klar = ((b−k)&0xFF)^k`,
  `k += 0xD7`, Prüfsummenbytes 0x8F09/Ende übersprungen; Blocktabelle der Doku für DGROUP → HOK), Kaderplatz
  HOK 0x06A27 + 0x618·Satz + 0x34·Platz, Pool HOK 0x0505F + 33·i, Vereine HOK 0x018A1 + 0x3D·i (`hok`).
  Kontrolle: FILE01 Regler 3 3 0 1, Manager „Joerg“, Verein 2 „KÖLNER EC“ wie in der Doku.
- Bilder (Referenz-Repo `original/spielstaende/bilder`): angesehen nur `file18_01`, `file11_03`; alle anderen Bildbelege
  sind „Bild nicht angesehen“.

B = bestätigt, T = teilweise, W = widerlegt/nicht gefunden, O = offen.

| Code (Datei:Zeile) | Fundstelle | Behauptung | Nachgelesen (Befehl → Inhalt kurz) | Ergebnis |
|---|---|---|---|---|
| Core/CupsNational.GameState.cs:70 | 0x41506–0x4151A, 0x54F9C–0x54FBF, 0x555D6 | 0x952C (Länderwerte) wird beim Verlassen der Aufstellung, im Tageslauf und nach dem Menü an WM-Tagen gerufen, nicht nach dem Freundschaftsspiel | `L 0x41506 0x4151A` → push 0, [0xa70e], `call 0x28:0x146c` (→0x952C); `L 0x54F9C 0x54FBF` → `mov [0xa70e],0` … push 1, call 0x952C; `L 0x555D0 0x555E0` → call 0x952C bei 0x555D6; `grep 0x0952c` → 12 Aufrufer, keiner in Ov 0xF8 (0x399D0–0x3CD72) | B |
| Core/CupsNational.GameState.cs:164 | 0x3CC01, 0x3CCBA–0x3CCC4; FILE18 Satz 5 | Auto-Kader: Plätze 0–2/3–12/13–28, je Art absteigend nach Summe, Gleichstand → zuerst durchsuchter Pool 1…0xB3 | `L 0x3CC01 0x3CCEA` → Zähler [bp-1]=1, `cmp [bp-1],0xb4`/jnc; Summe Pool +0x15+0x16+0x17; 0x3CCBA cmp/jl/jg, 0x3CCC4 `jna` (strikt); `dg 0x4EDE` = 3,10,16; `hok FILE18` Satz 5: Platz 17 LEDLIN p129 228, 18 DEISENBERGER p169 228, 27 LEE p114 220, 28 KUHNKE p166 220 | B |
| Core/CupsNational.GameState.cs:229 | Ov 0xF8, 0x3BF9B/0x3C05E; FILE18→FILE19 | Freundschaftsspiel schreibt die Länderwerte nicht neu | kein Aufrufer von 0x952C in 0x399D0–0x3CD72 (s. o.); `L 0x3BF9B` → `cmp [0xd0c1],8`; `L 0x3C05E` → call 0xE8:0x39; `hok` Verein 54 +0x18–0x1D FILE18 = FILE19 (58 5A 53 57 33 33), [0xD0C1] 0→1; +0x1E ff. ändern sich | B |
| Core/CupsNational.Lineup.cs:63 | 0x40F35–0x40F59, 0x411FE, 0x40F1E–0x40F32 → 0x41273 | Rechtsklick nimmt eingesetzten Nicht-Aufgestellten heraus, setzt Nicht-Eingesetzten ein; Linksklick auf Nicht-Aufgestellten geht in die Reihe | `L 0x40F0D 0x40F80` → Ereignis 3 → 0x40F35; [bp+6]=0 (Menüaufruf 0x48E3B) → 0x40F45: +0x0A≠0 ∧ +0x15=0 → 0x40F59 `mov [+0xa],0`; sonst 0x4115F → 0x411FE `cmp [0x5a16],3` → 0x41208 `mov [+0xa],1`; Ereignis 1: 0x40F1E/0x40F2B → 0x41273 → Platzsuche 0x41287. Bild file19_08 nicht angesehen | B |
| Core/CupsNational.Lineup.cs:69 | 0x40890–0x40943, 0x41287–0x414C6, 0x40F7C–0x411FE | Block wählt Reihe; Spieler auf ersten freien Platz seiner Art, sonst „Kein Platz mehr !“; Aufgestellter heraus + Reihe gewählt | `L 0x40880 0x40950` → Schleife 0..4 mit Treffertest 0x0B0E6, 0x4093D `[bp-9]=Block`; `L 0x412AE 0x41620` → Suche in [bp-0x46], 0x414BC–0x414C6 Meldung DGROUP 0x50A7/0x50AC; `L 0x40F7C 0x41210` → Platz leeren, 0x411F8 [bp-9]=Reihe; kein Verdrängen. Bilder file19_06/07/08 nicht angesehen | B |
| Core/CupsNational.Lineup.cs:186 | 0x40E01–0x40E27, 0x40E2A–0x40E61 | +0x09 & 3 ≠ 0: nur Meldung, beide Tasten; danach „Schon 22 Mann“ | `L 0x40D8D 0x40E61` → [bp-0x1a] = +0x09 & 3; nur **=1** („Gesperrt.“, DGROUP 0xB88) und **=2** („Verletzt.“, 0xB84) → Meldung, vor jeder Tastenprüfung; Wert 3 fällt durch. 0x40E2A–0x40E61: [bp+6]=0, +0x0A=0, 0x4BE17(m,3)=0x16 → „Schon 22 Mann / im Team !“ (0x11D5/0x11D9) | T (Wert 3 ohne Meldung) |
| Core/CupsNational.Lineup.cs:189 | 0x40F7C–0x411C1, 0x4115F–0x4116F, 0x411FE–0x41208 | Aufgestellter: Platz leeren, Reihe gewählt; links +0x0A=0, rechts wieder eingesetzt | `L 0x40F7C 0x41210` → Platzschleife bis 0x41153; 0x4116F `mov [+0xa],al` mit al = ([bp+6]≠0) = 0; 0x411C1–0x411FB [bp-9]=Reihe; 0x41208 `mov [+0xa],1` bei Ereignis 3; 0x41210 +0x15=0 | B |
| Core/CupsNational.Lineup.cs:192 | 0x40F59, 0x41208 | Rechtsklick: Reservist heraus, Nicht-Eingesetzter ein | `L 0x40F59` → `mov byte [es:bx+0xa],0`; `L 0x41208` → `mov byte [es:bx+0xa],1` | B |
| Core/CupsNational.Lineup.cs:193 | 0x40E77–0x40EB9, 0x412A1–0x41381, 0x415B2 ([APPROX]-Teil 0x414D3–0x414ED, 0x413E8–0x414B4) | Keine Reihe → Meldung; Torhüter nur in Torhüterplatz; erster freier Platz Sturm 0–2/Abwehr 3–4; eingesetzt | `L 0x40E77 0x40EB9` → [bp-9]=0xFF → „Keine Reihe / ausgew{hlt !“ (0x1221/0x1225); Reihe 4 ↔ Art 0 sonst „Der darf da / nicht rein !“; `L 0x412A1 0x41381` → si-Start (Art<2)·3, Ende +2; Art = `0x4180` = min(Pool+0x18/25,2); `L 0x41533 0x415B2` → +0x15 = Reihe+1 (+0x80 bei si<3), `mov [+0xa],1`; 0x414D3 [bp-0x1a]=3−[bp-0x1a] und 0x413E8 Platz 5 bestätigen den [APPROX]-Text | B |
| Core/CupsNational.Strength.cs:30 | DGROUP 0x4C4E − 2 | Kondition/Technik der Länder aus Tabelle − 2 | `xref 0x4C4C 0x4C50 --imm` → nur 0x2F0E5/0x2F109; `L 0x2F0CC 0x2F124` → `mov al,[bx+0x4c4e]; add al,0xfe` → [+0x18]/[+0x1A] | T (Deutschland-Würfel, Form/Tagesform nicht nachgelesen) |
| Core/CupsNational.Strength.cs:33 | 0x952C mit Manager 5 | dieselben Felder, die 0x952C aus der Aufstellung schreibt | `L 0x952C 0x977F` → `cmp [bp+6],5`; Verein = Manager+0x14 (0x96F2); 0x9721/0x9749/0x9771 schreiben +0x18/+0x1A/+0x1C je k=0,1 | B |
| Core/CupsNational.TeamInfo.cs:126 | 0x50283, 0x5030D–0x5031B, 0x35E7C, 0x31AB7 | Pokal/Play-off schreiben keine Rekorde: 0x50283 prüft Code 0xD0C2 nur auf 7 und 8 | `L 0x5030D 0x5031B` → `cmp [0xd0c2],7`/`cmp …,8`/`jmp 0x50B41` (retf); 0x35E7C `mov [0xd0c2],6`; 0x31AB7 `mov [0xd0c2],1`. Vor der Prüfung ruft 0x50283 aber 0x4F5A8 (schreibt DGROUP 0xAB7B ff.) für jeden Wettbewerb | T (ob das 0x4F5A8-Geschriebene „Rekorde“ sind, offen) |
| Core/CupsNational.cs:158 | Spielerfelder 0x10–0x12; FILE18/FILE19 | KO/TE/FO = +0x10/+0x11/+0x12; Freundschaftsspiel +1/+2/+2, 99 bleibt | `hok FILE18` Satz 5 Platz 2 KÜNAST 79 81 53, Platz 1 MERK 84 81 50; Bild file18_01 zeigt KO/TE/FO genau so; FILE18→FILE19: 27 Plätze +1/+2/+2, ZERWESZ (13) und SCHINKO (16) +0x10 99→99 | B |
| Core/CupsNationalMode.cs:84 | 0x54D19, DGROUP 0x1DB, 0x54D1E–0x54D2D, 0x3CA06; FILE11 | nur Ernennung; „auto“ und Satz 5 nur im Durchlauf | `L 0x54D10 0x54D38` → `mov [0xd0ba],1`; `cmp [0x1db],0`/jz; `[0xa70e]=5`; `call 0xf8:0x61` (→0x3CA06); `hok FILE11` 14.04.1994, Satz 5 alle Plätze 0, 0xA70E=5 (0xD0BA im Stand 0: 0x554BA löscht das Kennzeichen nach der Anzeige). Bild file11_12 nicht angesehen | B |
| Core/CupsNationalMode.cs:101 | 0x5557F–0x555A3, 0x44E22, DGROUP 0x9EDA | Menü an jedem WM-Tag bei 0x9EDA≠0xFD und Tagesereignis≠0, ohne Abfrage des eigenen Spiels | `L 0x5557F 0x555A3` → [0xa70d]=[0xa70e]=5; `cmp [0x9eda],0xfd`/jz; `cmp [bx+0x828],0`/jz; `call 0x110:0x66` (→0x44E22); keine Spielabfrage. Bilder file19_28/file20_02/04/file21_02/03 nicht angesehen | B |
| Core/GameSetup.cs:54 | DGROUP 0x11A | Start-Liga ein Wert für alle Manager | `xref 0x11A 0x11A` → 8 Leser (0x28915, 0x289BE, 0x28A52, 0x29A3F, 0x29B76, 0x29BBB, 0x29BCE, 0x5B289), alle `[0x11a]` ohne Managerindex; Stand-Block 0x118 (4 Byte) einmal; README FILE09 „Start-Liga gilt im Original für alle Manager“ | B |
| Core/GameSetup.cs:125 | DGROUP 0x11A, 0x118–0x11B | Regler 2 der Spielvorbereitung | `L 0x2890A` → `add [bx+0x118],al` mit bx = Reglernummer → Regler 2 = 0x11A; `dg 0x118` = 3 3 0 1 | B |
| Core/GameSetup.cs:128 | Vorgabe 0 | ohne Manager Bundesliga | `dg 0x11A` = 0 | B |
| Core/GameSetup.cs:181 | 0x2890A, 0x2890E–0x2891C, 0x2899D, 0x28925–0x28944 | Dauer „eine Saison“ + Start-Liga ≠ BL → Schritt zurück, vor der Begrenzung, für alle Regler | `L 0x288F0 0x2899D` → add; `cmp [0x119],0`/jnz; `cmp [0x11a],0`/jz; `sub [bx+0x118],al`; `jmp 0x2899D`; danach 0x28925–0x28944 Klemmen 0…[bx+0x11c]. Bilder file28_3–5 nicht angesehen | B |
| Core/GameState.Integration.cs:101 | 0x56448, 0x9D1B; FILE20→FILE21 | Teamwert-Wanderung (Modus 10) auch im Modus „nur Weltkugel“ | `L 0x56445 0x56448` → `mov al,0xa; call 0x28:0x1c5b` (→0x9D1B); `L 0x55810 0x56448 \| grep d0b9` → keine Modusprüfung; `hok` (Modus 1): ESV KAUFBEUREN +0x18–0x1B 80 78 79 75 → 92 91 90 87, DÜSSELDORFER EG 94×4 → 94×4; 12 Länder +0x18–0x1B gleich, +0x1C–0x1E alle geändert | B |
| Core/GameState.MainMenu.cs:58 | 0x44E3F | erster Menüaufbau zieht den Gehaltsfaktor wie jeder Zug | `L 0x44E22 0x44E46` → Funktion beginnt **0x44E22**; 0x44E37–0x44E46 `random(0x55,0x73)` (Aufruf bei 0x44E3F) → `[0xe1a4]` ohne Bedingung; `xref 0xE1A4` → Leser 0x4A832 (Gehaltsforderung) | T (0x44E3F ist der Aufruf, nicht der Funktionsanfang) |
| Core/GameState.SeasonEndTurn.cs:10 | 0x26F20, 0x26FB0–0x26FB3, 0x279ED–0x279FA, 0x26F2F–0x26FAA, 0x2756B, 0x27636, 0x279DA–0x279DD, 0x27B64–0x27B76, 0x27BC6, 0x564E1–0x564F2 | Zeiger 0xA70E/0xA70D im Fenster „Saisonende“ | `L 0x26F2F 0x26FB3` → Start = [0x386]·([0xd0b9]=1), `mov [0xa70d],al`; `L 0x279D5 0x27A00` → zurück, `inc/cmp [0x386]`; 0x2756B = Pool+0x1A; 0x27636 zurück; `L 0x27B64 0x27B76` je Manager; `L 0x564E1 0x564F2` → 0. **Aber** `L 0x27B79 0x27BD8`: 0x27BC6 prüft 0x4BE17(m,0) = Zahl belegter Kaderplätze; „kein auslaufender Vertrag“ (+0x0B=0) ist [bp-0x18] bei 0x27BCF–0x27BD8 | T (Fundstelle/Deutung 0x27BC6 falsch) |
| Core/MatchEngine.Managed.cs:79 | 0x42CB1, 0x425B6; FILE07, FILE26, FILE22/FILE23 | Kaderplätze: Start TW/V/S, Kauf hinten an, Verkauf rückt auf, Saisonwechsel unverändert | `L 0x42CF0 0x42E48` → Kauf **fügt am Ende der eigenen Art ein** (s. Widerspruch 1); `L 0x42670 0x42691` Verkauf memmove 0x5DF7→0x5DC3; `hok FILE22/FILE23` Satz 0: alle 22 +0x0F gleich | T („Kauf hinten an“ nur für Stürmer) |
| Core/MatchEngine.National.cs:58 | FILE18 Satz 5 | nach „auto“ 0–2 TW, 3–12 V, 13–28 S, absteigend | `hok FILE18` Satz 5: TW 216/215/213, V 234…219, S 239…220 absteigend, Rollen passend; Code s. Zeile 164 | B |
| Core/NewGame.cs:93 | DGROUP 0x11A | ein Wert für alle Manager | wie GameSetup.cs:54 | B |
| Core/NewGame.cs:136 | 0x29C3B–0x29CF2, 0x7CB, 0x29C78, 0x29C9B–0x29CC3, 0x29CCC, 0x36169; FILE27/28/15 | Behalten nur bei Start-Liga ≠ BL, Verein in der Liga, Platz ≤ 35; sonst Zufallsplatz, neu gezogen bei früherem Manager/Europapokal; Tausch | `L 0x29C3B 0x29CF7` → `cmp [bp-0x5e],0`; 0x34FD5/2 = Liga; `cmp al,[0x7cb]`/ja; `random([0x7c0+L],[0x7c0+L]+[0x7b1+L]−1)`; Schleife über Manager < [0xa70e]; `0x45CD0(r,1)`; `0x36169(v,r,0x80)`; `hok` FILE27/28 KÖLNER EC in 2. Liga, in der BL ES WEIßWASSER bzw. EC HANNOVER; FILE15 SV BAYREUTH (23) in 2. Liga | T („mit Kölns Teamwerten“ und was 0x36169 tauscht nicht nachgelesen) |
| Core/NewGame.cs:174 | DGROUP 0x7B1 | 12 Plätze je Liga | `dg 0x7B1` = 12 12 12 12 | B |
| Core/NewGame.cs:182 | 0x43A17 (0x35EDD), 0x43A61 (0x28165), 0x43A66–0x43B40, 0x35FD2–0x3613F, 0x439F5 | Startmischung vor der Spielvorbereitung, danach keine zweite | `L 0x439F0 0x43A66 \| grep call` → 0x439F5 →0x25B31 (= srand(time(0)), `L 0x25B31`), 0x43A17 →0x35EDD, 0x43A61 →0x28165; `L 0x43A66 0x43B40 \| grep call` → kein 0x35EDD; 0x35FD2–0x3613F ruft 0x36169. Bilder oberflaeche_0128/_0133/_0136 nicht angesehen | B |
| Core/NewGame.cs:209 | DGROUP 0x7C0, 0x7B1, 0x29C78 | erster Platz 0/12/24, 12 Plätze | `dg 0x7C0` = 0 12 24; `L 0x29C78` → liest [bx+0x7c0], [bx+0x7b1] | B |
| Core/NewGame.cs:220 | DGROUP 0x7CB | = 35 | `dg 0x7CB` = 35 | B |
| Core/Player.Contract.cs:396 | 0x4A93F, 0x425B6; FILE22/FILE23 | Alter aus Kaderplatz 3; Platzfolge Start/Kauf hinten/Verkauf/Saisonwechsel | `L 0x4A3D3 0x4A407` → di 0..2, endet mit 3; `L 0x4A935 0x4A95A` → Pool[Platz di +0x0F]+0x14; `hok` FILE22/FILE23 Platz 3 = Pool 23 DOLL | T (Teil „Kauf hinten“, s. Widerspruch 1) |
| Core/Player.Factory.cs:176 | 0x42CB1; FILE05→FILE07 | neuer Kaderplatz hinten an | `L 0x42D46 0x42E48` → erster Platz, der leer ist oder dessen Art (0x4180) **größer** ist (0x42D96 `ja`), belegte Folge per memmove (0x0B830) nach hinten; `hok` FILE07 DAFFNER (Rolle 86 = Stürmer) auf Platz 25 = Ende, daher kein Gegenbeleg; Remake `club.Players.Add(p)` | W (gilt nur für Stürmer) |
| Core/SaveGame.Migration.Nummern.cs:7 | 0x42CB1, 0x425B6; FILE07, FILE05→FILE26 | Nr. = Kaderplatz + 1; Kauf hinten, Verkauf rückt auf | Verkauf: `L 0x42670 0x42691` memmove; `hok` FILE26 HOCK (13) fehlt, 14–24 → 13–23. Kauf: s. Widerspruch 1 | T |
| Core/SeasonFlow.FairPlay.cs:8 | 0x5645D→0x26F20, 0x27213, 0x5644E, 0x2A1E0/0x2A1F2, 0x9746, 0x56409, 0x2A21D–0x2A292, +0x4A, 0x5647A–0x56497, 0x2A29D, 0x5576B | Fair-Play nach der Mischung, Liga der nächsten Saison, aufsteigend nach Strafminuten, Tausch nur bei echt mehr | `L 0x5644E 0x5645D` → 0x35EDD, 0x346D5, 0x26F20; `L 0x27213` → `call 0xe0:0x20` mit 0; `L 0x2A1E0 0x2A2C1` → Liga [0x9746+0x1AF·m], Sätze [0x7c0+L]+i, Wort 0x82A1 (= 0x8257+0x4A), `cmp`/`jna` strikt; 0x56409 schreibt 0x9746; 0x5647A–0x56497 löscht Wort 0x82A1 für 0x40 Sätze; 0x5576B → 0x25E30 | B |
| Core/SeasonFlow.LeagueSlots.cs:21 | 0x5644E vor 0x5645D | Mischung nach Auf-/Abstieg, Ewiger Tabelle und Wanderung, vor dem Fenster | `L 0x56238 0x5645D` → Absteiger 0x56238 ff., 0x56448 Wanderung, 0x5644E Mischung, 0x5645D Fenster | T (Ewige Tabelle nicht nachgelesen) |
| Core/SeasonFlow.LeagueSlots.cs:26 | 0x346D5, 0x56453, 0x43A17, DGROUP 0xA763/0xA776/0xD0E6/0x388, 0x36146, 0x36270/0x36284; FILE22→FILE23 | Europapokal-Teilnehmer; Tausch führt Meister und Titelverteidiger mit | `L 0x56453`, `L 0x43A1C` → call 0x346D5; `L 0x346D5 0x34900` → [0xd0e6], [0x388] nach 0xA763/0xA776; `L 0x36265 0x3628A` → 0x36146(&0xD0E6), (&0x388); `L 0x36146 0x36168` tauscht Index; `hok` FILE22 0xD0E6 = 4 KÖLNER EC → FILE23 8 KÖLNER EC, 0xA776 = 8 | B |
| Core/SeasonFlow.cs:292 | 0x56238–0x56284, DGROUP 0x5CCB; FILE20→FILE21 | Auf-/Abstieg auch in Modus 1 über die ungespielte Platzliste | `L 0x56230 0x56290` → [0x5cca+12L+[0x7b1+L]−k], `random([0x7c2]+[0x7b3], +5)`; keine `d0b9`-Prüfung; `hok` (nach Namen): Oberliga raus HEC HEILBRONN, EC BAD TÖLZ (Tabellenplätze 34/35), rein EHC STRAUBING (Satz 37), ESV KÖNIGSBRUNN (Satz 41); Ligen 1 und 2 namensgleich | B |
| Core/SeasonFlow.cs:318 | 0x5644E vor 0x5645D | Mischung nach Austausch und Ewiger Tabelle, kein Zufall | wie oben; `L 0x35EDD 0x36145 \| grep call` → nur 0x0BFD5 und 0x36169, kein `random` (0x0C037) und kein `rand` (0x24DF) | T (Ewige Tabelle nicht nachgelesen) |
| Core/Squad.AutoLineup.cs:21 | 0x42CB1, 0x425B6, 0x42838; FILE05/07/26, FILE22/23 | Platzfolge entscheidet Gleichstand | `L 0x42838 0x42995` → Kandidaten +0x0A=0, +0x09&3=0, Wert (+0x10+0x11+0x12)/3, 0x4292F `cmp [bp-6],si`/`jg` → späterer Platz gewinnt; `hok` FILE05 HANEMANN 18, H.STEIGER 23, LALA 24 je 68, LALA 0x83; FILE07 DAFFNER 25 (68) 0x83, LALA 0. Kauf s. Widerspruch 1 | T |
| Core/Squad.Lineup.cs:233 | 0x28165, 0x47BB5, 0x7C07, 0x54D9E/0x5530B, 0x426BC, 0x427C0–0x4282C | Aufrufe von 0x426BC; ohne Einstellung gilt die Ersatzrunde von 0x426BC | `grep 0x426bc\[` → 0x2A067 (in 0x28165), 0x47BB5, 0x7C07, 0x54D9E, 0x5530B (+8 weitere); `L 0x426BC 0x426E4` → `[0x375+2m]−1 = 0xFF` → sofort Ende, d. h. ohne Einstellung **keine** Ersatzrunde; 0x426FD löscht +0x0A aller Plätze | T („oder ohne Einstellung … Ersatzrunde“ trifft nicht zu) |
| Core/Squad.Lineup.cs:239 | 0x42E3A, 0x47BB5, 0x415B2, 0x5C50; FILE07, FILE06, FILE24 | neuer Platz leer; Aufstellen setzt ein; Verletzter behält +0x15 | `L 0x42E2A 0x42E48` → memset(Platz,0,0x34); 0x415B2 `mov [+0xa],1`; 0x5C50 `mov [+0xa],0`; `hok` FILE07 DAFFNER +0x0A=1; FILE05→FILE06 BRANDL 1→0/0x83→0, DOBRYZINSKI 0→1/0→0x83; FILE24 STEFAN +0x15 0x83 | B |
| Core/Squad.Lineup.cs:361 | 0x427C0/0x4282C, 0x426BC, 0x54D8C–0x54D9E, 0x552F9–0x5530B; FILE24 | Zähler [bp-9] ab 1 bis 0x17; nach dem Spieltag Neuaufstellung | `L 0x426DA` [bp-9]=1; 0x427C0/0x4282C `cmp [bp-9],0x17`; `L 0x54D84 0x54D9E`, `L 0x552F1 0x5530B` → [0x375+2m]=[0x376+2m], call 0x426BC; `hok` FILE24 22 × +0x0A=1 | B |
| Core/Squad.Strength.cs:34 | 0x42CB1, 0x425B6; FILE26, FILE07, FILE22/23 | Nr. = Kaderplatz + 1; Kauf hinten, Verkauf rückt auf | wie Migration.Nummern.cs:7; FILE22/23 alle 22 Plätze gleicher Poolindex | T (Kauf) |
| Core/StadiumStatistics.cs:107 | 0x5C50; FILE05→FILE24 | Verletzte: +0x15 bleibt, +0x0A = 0 | `L 0x5C3C 0x5C55` → `or [+0x9],2`, `[+0x17]`, `mov [+0xa],0`; `hok` STEFAN (15) 0x83, CHAIDAROV (20) 0x81 bleiben, +0x0A 1→0, +0x09 0→2 | B |
| Core/StatisticsStrength.cs:83 | 0x952C, 0x5149A–0x514E3 | Stärken zeigt gespeicherte Felder 0x18–0x1D | `L 0x5149A 0x514E3` → [+0x18], [+0x1A], [+0x1C] über 0x9657/0x7C0; kein 0x5DC3 in 0x51480–0x51600 | B |
| Core/Transfer.Pool.cs:535 | 0x274A7, 0x2787A, 0x56467/0x56470 | Alter aller Poolplätze +1; neue Namen Art 0, dann 1 | `L 0x2748E 0x274A7` → `inc [Pool+0x14]` bei [0xa70e]=0, Schleife bis 0xC8 (0x2766D); `L 0x56464 0x56475` → 0x5ACA0(0), (1); 0x2787A in derselben Schleife (Inhalt nicht im Einzelnen gelesen) | B |
| UI/…/ClubInfoWindow.cs:39 | 0x50283 | nur Wettbewerbscode 7 | `L 0x5030D 0x5031B` → `cmp …,7` **und** `cmp …,8` | T (Code 8 fehlt) |
| UI/…/NationalLineupScreen.cs:35 | 0x4090A, 0x408D3 | gewählte Reihe Farbe 0x10, sonst 2 | `L 0x408D3` `mov ax,0x2`, `L 0x4090A` `mov ax,0x10` → 0x0B651 | B |
| UI/…/NationalLineupScreen.cs:95 | 0x40890–0x40943 | Klick in Block wählt Reihe | `L 0x40886 0x40948` → `cmp [0x5a16],1`, Schleife, 0x4093D [bp-9] | B |
| UI/…/NationalMenu.cs:69 | 0x54D19, 0x55587; file11_03 | Kacheln nach Ernennung offen, kein Menü davor | Bild file11_03 (14.04.1994, DEUTSCHLAND, Kacheln offen); 0x55587 `cmp [0x9eda],0xfd` | B |
| UI/…/WorldCupScreen.cs:18 | 0x2F625–0x2F690, 0x4C196, DGROUP 0x65C, 0x2FA4A–0x2FA6C, 0x3A88A, 0x3AC84–0x3AC8E, 0x30FF2–0x3100D | Gruppenknopf (237\|211) 57×12, nur aus dem Menü | `L 0x2F625 0x2F690` → [bp+8]=0, Text [0x65C] „Gruppe“ + (2−g), 0x4C196(0xED,0xD3,…); `L 0x2FA4A 0x2FA6C` → y 0xD3–0xDE, x 0xED–0x125, 12−g; `L 0x3AC84 0x3AC8E` → 0x2F5B6(code−11); `L 0x30FF2 0x3100D` → 0x2F5B6(g,1) | B (Klickfläche ist auch ohne Knopf aktiv) |
| UI/…/WorldCupScreen.cs:23 | 0x2F5B6, 0x2FC2F, 0x3AC10–0x3AC3C, 0x3AC67–0x3AC75, 0x3ACCB | kein Knopf „Endrunde“; Endrunde über Paarungen → WM | `L 0x2FA2D 0x2FADC` nur Knopf + Zeilen; `L 0x3AA0F 0x3AA1C` `jmp [cs:0x12FB]` = Datei 0x3ACCB; Einträge 8/9 → 0x3AC10 (0x2FC2F), 10 → 0x3AC67 (0x32E23 mit 3), 11/12 → 0x3AC84 | B |
| UI/…/WorldCupScreen.cs:29 | 0x2FA2D–0x2FADC, DGROUP 0x5A16 | Ausgang nur bei Ereignis 3, sonst zurück nach 0x2FA1E | `L 0x2FACC 0x2FADC` → `cmp [0x5a16],3` → Ausgang; sonst `[0x1db]≠0` → ebenfalls Ausgang; sonst `jmp 0x2FA1E` | T (Ausgang auch im Durchlauf 0x1DB≠0) |
| UI/Screens/MainMenuScreen.cs:186 | 0x454C2–0x454F2, 0x54D19, 0x5557F–0x55587 | Original prüft nur 0xA70E = 5 | `L 0x454C2 0x454F5` → `cmp [0xa70e],5`, 0x4BE17(5,0) < 0xA → „Wollen Sie die WM wirklich / mit SO wenigen Spielern bestreiten ?“ (0x1241/0x1245); Zug 5: 0x5557F (Wächter 0x9EDA≠0xFD), 0x54D25 nur im Durchlauf. Bild file11_11 nicht angesehen | B |
| UI/…/AdvertisingScreen.cs:53 | 0x2BA72, 0x2BFF7, 0x120:0x3E | Pfeile 8×9 bei (138\|183, 138\|192), Halten 0; −1/+1 | `L 0x2BA5F 0x2BA72`, `L 0x2BFE4 0x2BFF7` → push 0,0xC0,0x8A,0xB7,0x8A → 0x4BA58; `L 0x4BA7D 0x4BABA` → x..x+7, y..y+8, Fläche 1 −1, Fläche 2 +1 | B |
| UI/…/AdvertisingScreen.cs:170 | 0x2BE0A | Schritt bei jedem Durchlauf mit Rückgabe ≠ 0 | `L 0x2BE0A 0x2BE4A` → `add [bp-0x28],[bp-0x31]` ohne Ereignisprüfung; 0x4BA58 liefert bei gehaltener Taste (Ereignis 2, 0x4BAC9/0x4BB64) den gemerkten Wert | B |
| UI/…/AdvertisingScreen.cs:238 | 0x2BE0A–0x2BE3C | Index klemmt bei 0 und 9 (Sender 4), kein Umbruch | `L 0x2BE10 0x2BE3C` → Index 0 + Schritt −1 → nichts; Grenze 9 − 5·([bp-0x2d]=8); kein Umbruch | B |
| UI/…/StadiumScreen.cs:24 | 0x3DB62 | `cmp si,0xA7`, „größer“ | `L 0x3DB58 0x3DB66` → si = Mausfeld +0 (y), `cmp si,0xa7`/`jng` | B |
| UI/…/StadiumScreen.cs:28 | DOSBox-Bild file01_3_stadion | Hinweiszeile x 213, y 201 | Messung nicht wiederholt | O (Bildvermessung nicht durchgeführt) |
| UI/…/StadiumScreen.cs:37 | 0x3D4FE–0x3D5E9, DGROUP 0xDD0 | „(keine Bautätigkeiten)“ bzw. Stufe + „ (Noch “ + n + „ Wochen“ (bei 1 ohne letztes Zeichen) + „)“ | `L 0x3D4FE 0x3D5F8` → [0xDD0] „(keine Baut{tigkeiten)“, [0xC4C] „ (Noch “, [0xDD8] „ Wochen“, +0x12=1 → strlen−1 = 0, ds:0x501F „)“ | B |
| UI/…/StadiumScreen.cs:52 | 0x3D63D–0x3D76A, DGROUP 0xDD4 | „(kein Bau...)“ bzw. Plätze + „ (“ + n + „)“ | `L 0x3D68C 0x3D72F` → [0xDD4] „(kein Bau...)“, ds:0x5024 „ (“, ds:0x5027 „)“ | B |
| UI/…/StadiumScreen.cs:68 | 0x3DD89, 0x3E0BC, 0x3DDB3, 0x3E0D7, 0x3DD93/0x3E0CD | Pfeilpaare, links „mehr“, nur im Klick-Frame | `L 0x3DD76 0x3DDB5` → 0x4BA58(192,189,202,189,0), `cmp [0x5a16],1`, `sub al,cl`; `L 0x3E0A9 0x3E0D7` → (192,210,202,210,0), `cmp [0x5a16],1`, `sub [bp-0xb],cl` | B |
| UI/…/StadiumScreen.cs:123 | 0x3D5FB–0x3D607, 0x3D737–0x3D743 | Wertfarbe 31 − 30·Modus | beide: `imul 0x1e`, `mov dx,0x1f`, `sub dx,ax` | B |
| UI/…/StadiumScreen.cs:139 | 0x3DB5B–0x3DB7D, 0x3E4D4 | Umschalten rechts bei y > 167, sonst verlassen | `L 0x3DB5B 0x3DB7D` → `cmp [0x5a16],3`, `cmp si,0xa7`, `xor [bp-0xf],1`, call 0x3D45F; `L 0x3E4D4` `cmp [0x5a16],3` → Ausgang | B |
| UI/…/StadiumScreen.cs:402 | 0x4BAD3–0x4BBFE | Pfeilgrafik 8×7 (22\|1+9j) bzw. (22\|19+9j) nach (x+2\|y+1) | `L 0x4BAD3 0x4BBFE` → push 0x7, 0x8, y+1, x+2, Quelle y 9·j+1 bzw. 9·j+0x13, x 0x16 | B |
| UI/…/SaveLoadScreen.cs:389 | 0x4C246 | Linie 80…237 = 158 px | `L 0x4C592 0x4C5AB` → 0x0B7B5(0x50, 0xA3, 0xED, 0xA3, 0) | B |
| UI/Screens/SetupScreen.cs:159 | 0x2890E–0x2891C | Rücknahme des Schritts | wie GameSetup.cs:181 | B |
| UI/Screens/SetupScreen.cs:284 | 0x28030, 0x28E4B, 0x43A17/0x43A61 | Seite p = Sätze 12p…12p+11; zyklisch | `L 0x28030 0x280CD` → [bp+6]·12, Grenze +12; `L 0x28E47 0x28E6E` → `add [bp-0x46],ax`, <0 → 3, =4 → 0, call 0x28030; Reihenfolge Start s. NewGame.cs:182. Bilder nicht angesehen | B |
| UI/Screens/Statistics/StrengthScreen.cs:70 | 0x51A02–0x51A59, 0x4D385, DGROUP 0x7B1 | nur links, 27 < y < 213, Zeile (y−27)/15, < [0x7B1], kein x | `L 0x519FD 0x51A5C` → `cmp [0x5a16],1`; si = [es:bx] (0x519AA) `cmp 0xd5`/`cmp 0x1b`; `add ax,0xffe5; idiv 0xf`; `cmp [0x7b1+L]`; 0x4D385(Satz, 2, 0, 0) | B |
| UI/ShortMessageTexts.cs:49 | 0x414C6, DGROUP 0x50A7/0x50AC | „Kein / Platz mehr !“ wenn beide Hälften voll | `L 0x414BC 0x414C6` → push ds:0x50AC „Platz mehr !“, ds:0x50A7 „Kein“, call 0x0C0B8; vorher zweiter Versuch andere Hälfte 0x414D3 | B |

**Zählung (vor dem Nachtrag):** 70 Fundstellen – 52 bestätigt, 16 teilweise, 1 widerlegt, 1 offen.
**Zählung nach dem Nachtrag (maßgeblich):** 70 Fundstellen – 57 bestätigt, 12 teilweise, 1 widerlegt, 0 offen.

#### Widersprüche und offene Punkte

1. **Kauf „hinten an“ (0x42CB1) ist so nicht richtig** (Player.Factory.cs:176 widerlegt; teilweise in
   MatchEngine.Managed.cs:79, Player.Contract.cs:396, SaveGame.Migration.Nummern.cs:7, Squad.AutoLineup.cs:21,
   Squad.Strength.cs:34): 0x42CB1 sucht ab Platz 0 den ersten Platz, der leer ist oder dessen Spielerart
   (0x4180 = min(Pool+0x18 / 25, 2): 0 TW, 1 V, 2 S) größer als die des Neuen ist (0x42D55–0x42DA1), schiebt die Folge
   per memmove um einen Platz nach hinten (0x42DC6–0x42E22) und setzt den Neuen dort ein. Ein gekaufter Torhüter oder
   Verteidiger kommt also hinter den letzten seiner Art, nicht ans Ende. FILE07 (DAFFNER, Stürmer) widerspricht dem nicht.
   Das Remake hängt immer an (`club.Players.Add(p)`), seine „Nr.“ weicht dann ab. Dieselbe Routine ruft auch der
   Spielstart (0x28165) und die WM-Kaderwahl (0x3CABF).
2. **GameState.SeasonEndTurn.cs:10:** 0x27BC6 prüft `0x4BE17(m,0) = 0`, also „kein belegter Kaderplatz“. Ob ein
   auslaufender Vertrag da ist (+0x0B = 0), prüft [bp-0x18] bei 0x27BCF–0x27BD8.
3. **ClubInfoWindow.cs:39** („nur Wettbewerbscode 7“) widerspricht CupsNational.TeamInfo.cs:126 und dem Code: 0x50283
   lässt 7 **und** 8 durch (0x5030D–0x5031B).
4. **Squad.Lineup.cs:233:** Ohne „automatisch aufstellen“ endet 0x426BC sofort (0x426C4–0x426E4). Eine Ersatzrunde
   „ohne Einstellung“ gibt es im Original nicht.
5. **CupsNational.Lineup.cs:186:** Die Meldung kommt nur bei +0x09 & 3 = 1 („Gesperrt.“) oder 2 („Verletzt.“). Bei 3
   (beide Bits) kommt keine Meldung, und es geht mit der normalen Behandlung weiter.
6. **WorldCupScreen.cs:29:** Bei DGROUP 0x1DB ≠ 0 (Durchlauf, 0x2FAD3) verlässt das Original die Seite auch ohne
   Rechtsklick. Im Remake folgenlos, als Aussage aber ungenau.
7. **GameState.MainMenu.cs:58:** Das Hauptmenü beginnt bei 0x44E22. 0x44E3F ist der Aufruf `random(85,115)` gleich
   nach dem Funktionsanfang. Inhaltlich stimmt die Aussage.
8. **CupsNational.TeamInfo.cs:126:** Vor der 7/8-Prüfung ruft 0x50283 für jeden Wettbewerb 0x4F5A8 (schreibt
   DGROUP 0xAB7B ff.). Ob das zu den „Rekorden“ gehört, ist offen.
9. **Offen:** StadiumScreen.cs:28 (Bildvermessung x 213 / y 201 nicht wiederholt); NewGame.cs:136 („Teamwerte bleiben
   am Platz“, Inhalt des Tauschs 0x36169); SeasonFlow.LeagueSlots.cs:21 und SeasonFlow.cs:318 (Lage der Ewigen
   Tabelle); CupsNational.Strength.cs:30 (ausgewürfelte Werte Deutschlands, Herkunft Form/Tagesform);
   Transfer.Pool.cs:535 (Inhalt 0x2787A). Außer `file18_01` und `file11_03` wurde kein DOSBox-Bild angesehen.
10. Am Rande (nicht als Fundstelle gezählt): FILE11 hat DGROUP 0xD0BA = 0 0 0 0. 0x54D19 setzt das Kennzeichen,
    0x554BA löscht es nach der Anzeige. Die Bezeichnung „Bundestrainer-Ernennung je Manager“ in der Formatdoku meint
    also ein vorübergehendes Kennzeichen.

#### Nachtrag – offene Punkte aus Abschnitt 9 nachgelesen

Dieselben Regeln, nur lesen. Ein Bild aus dem Remake habe ich nicht gerendert: Ich habe nur das Referenzbild vermessen
und mit den Konstanten im Code verglichen.

| Code (Datei:Zeile) | Fundstelle | Behauptung | Nachgelesen (Befehl → Inhalt kurz) | Ergebnis |
|---|---|---|---|---|
| UI/…/StadiumScreen.cs:28 | DOSBox-Bild `original/spielstaende/bilder/file01_3_stadion.png` | Hinweiszeile „… ausbauen“: x = 213, Oberkante y = 201 | Python/PIL: Fenster 642 × 507, Spielfläche 640 × 480 ab (1 \| 26) (Titelleiste bis y 25, Rahmen x 0/641), Mittelwert je 2 × 2 → 320 × 240; Pixel ≠ Hintergrund (81,113,130): „Gastronomie ausbauen“ Rahmen x 213–304, y 201–205, das „G“ beginnt in Zeile 201 bei x 213. Gegenprobe Kopfzeile = Zeichenposition: „Konto:“ (Code y 232) hat oberste Zeile 232, „K“ bei x 213 (= `RightLabelX`); Ausstattungsliste (Code `FeatureRow.Y + 1` = 173) ab Zeile 173. Code: `HintY = 201`, `RightLabelX = 213` | B (Remake-Bild nicht gerendert) |
| Core/NewGame.cs:136 | 0x36169 mit Schalter 0x80 (Aufruf 0x29CF2) | Name, Logo, Manager und Spieler wandern, die Teamwerte bleiben am Platz | `L 0x36170 0x3628F` → Pool +0x1D (Verein) aller 200 Spieler v↔r (0x361D7), Manager +0x14 (0x3623D), Titel 0x387/0xD0E6/0x388 über 0x36146; `L 0x3628F 0x363F6` → bei 0x80 nur Vereinsbytes 0x00–0x15 (Name, `rep movsb` 0x105C4 mit 0x16), 0x16 (Manager), 0x34 (Logo laut reverse-engineering.md) und Wort 0x3B (Farbcode) getauscht, danach `jmp 0x3649C`; `L 0x3649C 0x364B4` → bei 0x80 Sprung ans Ende 0x36728, also kein Tausch des Tabellensatzes 0x8257 (0x50 Byte); ohne 0x80 ganzer Satz 0x3D (0x363F9 ff.). Bleiben also 0x17–0x33 (Tagesform, Teamwerte, Form …) und 0x35–0x3A. `hok` FILE27: ES WEIßWASSER (Satz 10, BL) +0x18–0x1B 91 94 91 94, KÖLNER EC (Satz 18, 2. Liga) 55 55 53 55 | B |
| Core/SeasonFlow.LeagueSlots.cs:21 | 0x5644E vor 0x5645D | Mischung nach Auf-/Abstieg, Ewiger Tabelle und Wanderung | `grep 0x5667a\[` → einziger Aufruf der Ewigen Tabelle 0x5667A bei 0x563AF; `L 0x563A8 0x563B2` → nach der Ligaschleife (Auf-/Abstieg ab 0x56137/0x56238, `jnc` bei 0x563A8 beendet sie), vor der Managerschleife 0x563B2–0x56418 (0x56409 Liga der nächsten Saison), dann 0x56448 Wanderung, 0x5644E Mischung, 0x5645D Fenster | B |
| Core/SeasonFlow.cs:318 | 0x5644E vor 0x5645D | Mischung nach Austausch und Ewiger Tabelle | wie oben: 0x563AF (Ewige Tabelle) < 0x5644E (Mischung) im selben geradlinigen Ablauf | B |
| Core/CupsNational.Strength.cs:30 | DGROUP 0x4C4E − 2, ausgewürfelte Werte Deutschlands, Form/Tagesform aus dem Vereinsplatz | K/T aus der Ländertabelle bzw. den Würfelwerten, Form und Tagesform aus Lader bzw. Wanderung | `L 0x554CF 0x5556F` → nur bei `[0x9eda] = 0xFD` (kein Bundestrainer), Verein `[0x6ac]` (eigenes Land), je k = 0,1: +0x18+k = random(0x54,0x5E) = 84–94, +0x1A+k = random(0x4F,0x59) = 79–89, +0x1C+k = random(0x2D,0x37) = 45–55; `L 0x0C037 0x0C067` → random(a,b) einschließlich b (`idiv (b+1−a)`). Lader `L 0x2EFE0 0x2F0C8` → +0x17 = random(0x2F,0x35) = 47–53, +0x1C = random(0x2D,0x37) = 45–55 (Abzug nur bei Satz > 0x40), danach nur +0x18/+0x1A für die Länder überschrieben (0x2F0E5–0x2F120), die Form bleibt. Wanderung: FILE20→FILE21 alle 12 Länder +0x1C–0x1E neu (Abschnitt oben). Remake `RerollNationalRatings`: Next(84,95)/Next(79,90)/Next(45,56) in derselben Reihenfolge | B |
| Core/Transfer.Pool.cs:535 | 0x2787A | Laufbahnende und Neuanlage im selben Platz, ohne Umbenennen | `L 0x2787A 0x279D9` → Ende, wenn Alter +0x14 > random(37,42) (0x2788F–0x278A1); steht der Spieler in einem Kader, nur bei Vertrag +0x0B = 0 bzw. Pool +0x18 & 0x80; dann Meldung 0x4B2F6, Abgang 0x425B6, Besitzer +0x1A = 6 (0x27942). Neuanlage im selben Poolplatz: Alter random(18,25) (0x27947–0x27966), s = random(40,92), +0x15/+0x16 (und +0x17) = random(s−5,s+5), +0x19 = 0; kein Schreiben des Namens (+0x00) in 0x2787A–0x279D9. Die Namen der freien Plätze vergibt danach 0x5ACA0 (0x56467/0x56470) | B |

Damit sind alle Punkte aus Abschnitt 9 geschlossen. Gegenüber der Tabelle oben ändern sich StadiumScreen.cs:28
(O → B), NewGame.cs:136 (T → B), SeasonFlow.LeagueSlots.cs:21 und SeasonFlow.cs:318 (T → B) sowie
CupsNational.Strength.cs:30 (T → B); Transfer.Pool.cs:535 war schon B und ist jetzt vollständig nachgelesen.

**Zählung nach dem Nachtrag:** 70 Fundstellen – **57 bestätigt, 12 teilweise, 1 widerlegt, 0 offen.** Die Widersprüche
1–8 bleiben unverändert.

## Anhang B1 – Entscheidungen

#### Reviewer B – Entscheidungen D1–D3, L1–L15 gegen den Code (Head b634032, Base 8afd750)

Bau/Test: `source /root/claude-tools/env.sh`, `dotnet test … -c Release`, Originaldaten über `gamedata/`. Mutationen in /home/claude/wt-B, danach `git checkout -- src` (am Ende `git checkout -- . && git clean -fd src tests`, Status leer). Merge-Folge: `git log --first-parent 8afd750..b634032`; Dateien je Integration: `git diff --name-only <M>^1 <M>`.

| Entscheidung | verlangt | umgesetzt (Datei:Zeile) | ausgeführte Prüfung → Ergebnis | Urteil |
|---|---|---|---|---|
| D1 | Sechs Spuren, `re-dosbox` zuerst, vier Umsetzungsspuren, `pruefung-karte` als Welle 2 | Merge-Folge main: 4afcdef (#567 re-dosbox, 17:19 CET) → ad9bf5c saison → 22979d7 nationalteam → 784f3b8 ui-rest → b951c31 kader-start → 698fb83/1243451 pruefung-karte | `git log --first-parent`; Autor-Zeitpunkte der ersten „nach re-dosbox“-Commits: saison T3 8e72b7a 16:33 UTC, nationalteam b805262 16:34 UTC, kader-start T2 db1d2ba (`%ai`) 17:02 UTC – alle nach 16:19 UTC (Merge #567). pruefung-karte-Parent 558b7f6 enthält alle vier Integrationen (`merge-base --is-ancestor` je ja) | ok |
| D2 | „eine Saison“ nur mit Start-Liga Bundesliga, Sperre im SetupScreen (kader-start T2) | `Core/GameSetup.cs:191` `Step`, Sperre `:204`; `UI/Screens/SetupScreen.cs:161` ruft `_setup.Step` | `KaderStartRunde13Teil2Tests`+`KaderStartStartLigaTests` 19/0/0, `KaderStartSetupScreenTests` 2/0/0. Mutation `:204` → `if (false)`: 3 Core-Tests rot (`Duration_OtherStartLeague_StopsAtThreeSeasons` ×2, `StartLeague_OneSeason_StaysBundesliga`) + UI `OneSeason_LocksStartLeague_ForAllManagers` rot | ok |
| D3 | Gesamtreview am Rundenende durch ChatGPT (Codex) + Nachprüfung durch Claude | `docs/reviews/2026-11-05-runde-13-gesamt.md` (Commit 6c8fdfe, Trailer Codex); `docs/vorlagen/review-anfrage.md:53–67` „Gesamtreview am Rundenende (Vollprüfung, Vorgabe Nutzer 05.11.2026)“ – „Damit entfällt die frühere Nachprüfung durch Claude“ | `git diff 57a793a b0688af -- docs/vorlagen/review-anfrage.md` → nur dieser Abschnitt neu (#614). D3 ist für den Nachprüfungsteil durch eine spätere Nutzervorgabe ersetzt. Anmerkung: `docs/arbeitspakete.md:170` nennt D3 weiter mit „Nachprüfung durch Claude“, ohne Hinweis auf #614 | ok (ersetzt; Text D3 nicht nachgeführt) |
| L1 | Start-Liga global, ein Wert für alle Manager; Sperre U15-b prüft diesen Wert | `Core/GameSetup.cs:130–138` (`GameSetup.StartLeague`, Setter für alle Manager), `:204` prüft `StartLeague`; `SetupScreen.cs:126` neuer Manager erbt den Wert, Anzeige `SetupScreen.cs:357` aus `_setup.StartLeague` | Mutation Setter nur `Managers[0]` → `StartLeague_SharedByAllManagers` und UI `OneSeason_LocksStartLeague_ForAllManagers` rot. Anmerkung: Datenmodell führt den Wert weiter je Manager und `NewGame.cs:93–103` liest ihn je Manager (Aufbauten im Code können verschiedene Ligen setzen); über die Spielvorbereitung ist er stets einheitlich | ok |
| L2 | U15-c bleibt `[APPROX]` mit Verweis auf L7, nicht umgesetzt | Nur Doku: `docs/transfer.md:493`, `docs/analyse/re-runde-13.md:798`. Im Code kein Marker | `grep -rn "29CFA\|U15-c" src` → 0 Treffer; `NewGame.PlaceInStartLeague` (`NewGame.cs:135–150`) und `AssignClubs` erwähnen den Sonderfall (Datei 0x29CFA–0x29DA1, Europapokal-Platz bei 1 Manager/1 Saison) nicht | **Abweichung** (Regel 2.7: `[APPROX]` gehört in den Code) |
| L3 | `SeasonFlow.LeagueSlots.cs`: kader-start nur Belegung beim Spielstart, saison nur Saisonwechsel; Methoden im PR nennen | kader-start: nur `InitializeLeagueSlots(places)` (`SeasonFlow.LeagueSlots.cs:80`); saison: `_nextLeagueSlots`, `PrepareNextLeagueSlots`, `SuccessorSlots`, `Mix`, `Segment` | `git diff cc8b0a0 b951c31 -- …LeagueSlots.cs` → eine Methode (Start); `git diff 48e1e67 ad9bf5c` → nur Saisonwechsel-Methoden | ok |
| L4 | A088 bei `saison` mit kleinen Änderungen in `MatchEngine.Live.cs`/`Player.Factory.cs`; kader-start dort nur Kaderreihenfolge | A088 nach L14 nicht umgesetzt: `MatchEngine.Live.cs` im Rundendiff unverändert; `Player.Factory.cs:179` (kader-start, A091 `RenumberPlayers` = Kaderplatzfolge), `MatchEngine.Managed.cs` (`club.Players` statt `SortedPlayers`) | `git diff 8afd750 b634032 -- MatchEngine.Live.cs` leer; `git diff --name-only 48e1e67 ad9bf5c` ohne `Player.Factory.cs`/`MatchEngine.Live.cs` | ok |
| L5 | S-K6-a/c wie im Original (neue Liga ab 29.04.) mit Migration; Schema 37 kader-start (nur bei Bedarf), 38 saison in `SaveGame.Migration.Saison.cs`; Lücke schließt die Leitstelle | `SaveGame.cs:48` (39), `:148–150` (`[36]` Leerschritt, `[37] = MigrateSaison`, `[38] = MigrateNummern`); `SaveGame.Migration.Saison.cs:14–25`; `SeasonFlow.cs:320` `PrepareNextLeagueSlots` | `SaveGameTests|SaisonSatzfolgeSaisonendeTests|SaisonwechselSatzfolgeTests|NummerierungMigrationTests|SaisonWeltkugelTests` 69/0/0. Mutation `[37] = MigrateSaisonPlatzhalter37` → `Schema37Save_InSeasonOver_WithoutNextSlots_Migrates_StartNextSeasonFormsSlots` rot. Anmerkung: Kommentare `SaveGame.Migration.Saison.cs:28–31` und `SaveGame.cs:148` („bis zur Integration … schließt die Leitstelle bei der Integration“) sind nach der Integration veraltet; Lücke funktional als Leerschritt geschlossen (kader-start brauchte kein Schema) | ok |
| L5 vs. L14 | Widerspruch „saison Schema 38“ ↔ „Kein Schema 38 in dieser Runde“ | Schema 38 = S-K6 (saison Teil 3), Schema 39 = Fix #615 (Nummern) | `git log --format='%h %ci' -- SaveGame.Migration.Saison.cs` → b807274 2026-11-04 17:05 UTC (= 18:05 CET), also **vor** L14 (18:34 CET); Handoff #583 fragte für A088 nach einem *weiteren* Feld „Schema 38 in `SaveGame.Migration.Saison.cs`“. `docs/arbeitspakete.md:150–151` (Leitstelle): „Schema 38 mit Leerschritt 37 (L5); A088 bleibt `[APPROX]` (L14)“. L14 betrifft nur A088, hebt L5 nicht auf | ok (nur Wortlaut von L14 missverständlich) |
| L6 | A293: nationalteam liest Datei 0x3A88A; unklar → `[APPROX]` | `UI/Screens/Competitions/WorldCupScreen.cs:18–25` `[ORIGINAL]` (kein Knopf „Endrunde“, Weg über 0x3A88A → Paarungen); `Tools/Scenarios/Wettbewerbe.cs:123` | `NationalteamRunde13UiTests` (A293_NoKnockoutButton_KnockoutViaPairings, A293_A294_…) in Lauf 14/0/0 grün | ok |
| L7 | Fehlende Aufnahmen entscheidet der Nutzer; betroffene Punkte bleiben `[APPROX]` | `docs/vorlagen/dosbox-aufnahme.md:85` (FILE29 für U15-c u. a.); A088 `[APPROX]` `MatchEngine.Live.cs:366`; ui-rest-Reste `[APPROX]` (`docs/arbeitspakete.md:157–158`) | `grep` wie oben; für U15-c fehlt der Code-Marker (siehe L2) | ok (U15-c-Marker unter L2 gezählt) |
| L8 | saison Teil 3 liest 0x346D5 mit und ordnet zu | `Core/SeasonFlow.LeagueSlots.cs:26` `[ORIGINAL] Datei 0x346D5`; `docs/analyse/re-saisonwechsel.md:412` | `grep -rn 346D5` → beide Stellen | ok |
| L9 | nationalteam ändert `GameState.Integration.cs` nur im Tageslauf des Bundestrainers; Saisonwechsel „nur Weltkugel“ bleibt saison | nationalteam: `GameState.Integration.cs:51` `UpdateNationalTeamValues()` (+3 Kommentarzeilen); saison: `:105` `DriftTeamValues` ohne `WorldOnly`-Wächter (A058) | `git diff 795413c 22979d7 -- GameState.Integration.cs` → nur Tageslauf; `git diff 48e1e67 ad9bf5c` → nur Saisonwechsel | ok |
| L10 | `StatisticsStrength.cs` → nationalteam | Änderung nur in #587 (A009) | `git diff --name-only` je Integration: nur 22979d7 | ok |
| L11 | `GameSetup.cs` → kader-start | Änderung nur in #600 (U15-b, L1) | wie oben: nur b951c31 | ok |
| L12 | `docs/spielberechnung.md` §14, `docs/analyse/re-runde-12.md`, Feldbelege `spielstand-original.md` → pruefung-karte; saison/kader-start nicht | einzige Commits: 474f878/25bbcf6 (pruefung-karte T2) | `git log --no-merges 8afd750..b634032 -- <je Datei>` → nur pruefung-karte | ok |
| L13 | kader-start ändert in fremden Testdateien je Aufbau **eine Zeile: `StartLeague`** passend zum Verein; `ScreenDriver.cs` minimal; neue Fingerabdrücke mit Begründung im PR | 26 fremde Testdateien mit `StartLeague`-Zeile bzw. Fingerabdruck+Kommentar; `UI/Testing/ScreenDriver.cs:86–95` (Seite/Zelle über `CellOf`, Start-Liga per Pfeil) | `git diff --numstat cc8b0a0 b951c31 -- tests` + `-U0` je Datei: je Aufbau höchstens eine Zeile. **Aber** in 9 fremden Dateien ist statt/zusätzlich zur Start-Liga die **Saat** geändert, z. B. `NationalCoachMode4Tests.cs:60` 3→17, `:71` 3→4, `:82` 3→22, `:93` 3→2; `SeasonEndStatusTests.cs` 4×; `AutoLineupTeil8Tests`, `IntegrationRunde9Tests`, `SaisonwechselProbenTests`, `WettbewerbeSpiel1Tests`, `StatistikenPraemienTests`, `WeltkugelDosboxTests`. Im PR #578/#589 aufgeführt und begründet („Vorbedingung nach dem Starttausch“), aber nicht von L13 gedeckt. `KeepSquad(…, extendExpired: true)`-Zeilen gehören zu Teil 1 (R12K-03, eigene Aufgabe) | **Abweichung** (gering, dokumentiert) |
| L14 | A088 bleibt `[APPROX]`, Kandidat Runde 14; kein (weiteres) Schema dafür | `MatchEngine.Live.cs:366` `[APPROX]`; `docs/analyse/inventur.md:369` Stand Runde 13 | `grep -rn A088 src` → 0 Treffer, Marker ohne ID; Rundendiff für `MatchEngine.Live.cs`, `Transfer*.cs` (saison) leer. Anmerkung: Begründung im Marker („Das Zurücksetzen im Original ist nicht gesucht“) ist durch `re-saisonwechsel.md:431–439` überholt; Inventur verweist auf `:382` statt `:366–370` | ok (Markertext veraltet) |
| L15 | Integrationsfolge saison → nationalteam → ui-rest → kader-start → pruefung-karte; kader-start auf integriertem Stand, Fingerabdrücke einmal | ad9bf5c → 22979d7 → 784f3b8 → b951c31 → 698fb83 → 1243451 | `git merge-base` je Merge: nationalteam-Parent 2 enthält saison, ui-rest enthält beide, kader-start-Parent d92078c hat Merge-Base = cc8b0a0 (main nach ui-rest), pruefung-karte alle vier. Fingerabdrücke von kader-start in a76aaea „auf dem integrierten Stand (main + Teile 1–3) neu“ | ok |

**Zählung:** 19 Zeilen (inkl. L5-vs-L14): ok 17, Abweichung 2 (L2, L13), offen 0.

Nebenbefund Dateigrenzen (nicht eigene Entscheidung): ui-rest änderte `Core/NewGame.cs` (1 Zeile, `BeginFirstTurn`), `Core/Transfer.Contract.cs` (Kommentar) – beide kader-start – und `Core/CupsNational.TeamInfo.cs` (Kommentar, nationalteam). In PR #572/#574 unter „Gemeinsame Dateien und Dateigrenzen“ begründet → nach AGENTS.md 2.5 zulässig.

## Anhang B2 – Bildschirme und Hauptmenü

#### Reviewer B – AGENTS.md §4 je Bildschirm und Hauptmenü-Ereignis (Head b634032)

Geänderte UI-Dateien: `git diff --name-only 8afd750 b634032 -- src/EishockeyManager.UI` → ArrowPair.cs, ClubInfoWindow.cs, NationalLineupScreen.cs, NationalMenu.cs, WorldCupScreen.cs, MainMenuScreen.cs, AdvertisingScreen.cs, NewsBar.cs, StadiumScreen.cs, SaveLoadScreen.cs, SetupScreen.cs, StrengthScreen.cs, ShortMessageTexts.cs (nur Texte), Testing/ScreenDriver.cs (Teststeuerung).

Eigene Probe (Kopie: `/home/claude/rv/ZzBProbeTests.cs`, im Checkout wieder entfernt): 2 Manager (Verein 2, Verein 14/2. Liga), Saat 5, Zustände Start / Bundestrainer ernannt / nach 8 `Advance`. Je Bildschirm: `SaveGame.Save` + `StateFingerprint.Of` → `Host.Show` (Enter) → 3× `Draw` → 5–30 `Frames()` **ohne Eingabe** (Update + Draw) → Bytes und Abdruck vergleichen. Lauf `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.UI.Tests -c Release --filter ZzBProbeTests` → 6/0/0. Detektor-Gegenprobe: `Host.Game?.Random.Next(0,10)` in `StadiumScreen.Draw` bzw. `.Update` → Probe in allen 3 Zuständen rot (bytesGleich=False, 35 Abdruckzeilen).

| Bildschirm/Ereignis | geprüft wie (Befehl/Probe) | Ergebnis | Urteil §4 |
|---|---|---|---|
| SetupScreen (Ctor: Seed + `NewGame.StartOrder`) | Probe, 3 Zustände, 5 Frames; Code `SetupScreen.cs:73–77` | Bytes gleich, Abdruck 0 Zeilen; Seed nur aus `Host.Seed`/`TickCount`, `StartOrder` zieht nicht aus `Game.Random` | ok |
| NationalLineupScreen / `.Tactics` | Probe (ohne Bundestrainer springt der Bildschirm zurück ins Menü; mit Bundestrainer offen bzw. TacticsScreen) | Bytes gleich, 0 Zeilen in allen Zuständen | ok |
| NationalMenu (`Open`, `IsLocked`) | Code `NationalMenu.cs:60–86` (nur Kommentar geändert, reine Abfragen); Freiprüfung erzeugt die Zielbildschirme über ihre Konstruktoren | keine Zustandszugriffe; Zielbildschirme ohne Abweichung | ok |
| WorldCupScreen | Probe, 5 Frames | Bytes gleich, 0 Zeilen | ok |
| AdvertisingScreen (ArrowPair) | Probe, 30 Frames (Pfeil-Wiederholung ohne Eingabe) | Bytes gleich, 0 Zeilen | ok |
| StadiumScreen (ArrowPair, Bau-Tabelle) | Probe, 30 Frames; Mutationsprobe s. o. | Bytes gleich, 0 Zeilen; Mutation erkannt | ok |
| SaveLoadScreen (Sichern/Laden) | Probe, je 5 Frames | Bytes gleich, 0 Zeilen | ok |
| StrengthScreen + Vereinsfenster (ClubInfoWindow, neuer Klick 0x51A02) | Probe: öffnen, Linksklick (100,45) → `InfoWindows.Visible = True`, 5 Frames, Rechtsklick | Bytes gleich, 0 Zeilen (auch das per Eingabe geöffnete Fenster ändert nichts) | ok |
| NewsBar (`Shown` mit Newest/Oldest) | Code `NewsBar.cs:28–55`: Cursor in `ConditionalWeakTable` (UI-Zwischenspeicher, nicht im Spielstand); Probe Hauptmenü b) | kein Spielzustand, Bytes gleich | ok |
| MainMenuScreen – Draw/Update ohne Eingabe | Probe: Kennzeichen „neue Nachricht“ nach dem Aufbau neu gesetzt, 10× `Draw` + 10 `Frames()` → Kennzeichen bleibt, Bytes gleich. Mutation `OnMainMenu` in `Draw` → `MainMenuDrawTests.Draw_DoesNotConsumeNewMessageOrRepairLineup` und Probe rot | Ereignis nicht in Draw/Update | ok |
| MainMenuScreen – genau ein Aufruf je Aufbau | Code `MainMenuScreen.cs:91–109` (`Enter` → `BuildMenu` → einmal `OnMainMenu`), `:240` (Zugwechsel ohne Bildschirmwechsel → einmal `BuildMenu`); `grep -rn "OnMainMenu(" src` → nur diese Aufrufer (+ `AutoSave.OnMainMenu`, Host-Zähler, kein Spielzustand) | ein Aufruf je Aufbau | ok |
| `GameState.OnMainMenu` – Zustandsänderungen | Code `GameState.MainMenu.cs:76–82`: nur `ApplyAutoLineup()` + `TakeNewMessage()`. Probe a) Automatik aus, Nr. 77, neue Nachricht → einzige Abdruckzeile `_newMessage True→False`, Nr. 77 bleibt; c) Automatik an → Nummern unverändert, `Game.Random`-Felder vorher = nachher. Mutation `RenumberPlayers()` am Anfang von `ApplyAutoLineup` → `NummerierungMigrationTests.MainMenu_WithAutoLineupOff_KeepsNumbers`, `…On_DoesNotRenumber` und Probe rot | nur erlaubte Ereignisse; seit #615 kein Umnummerieren (`Squad.AutoLineup.cs:184–188`, `RenumberPlayers` nur noch in `Squad.Setup.cs:79`, `Player.Factory.cs:179`, `Player.Contract.cs:196`, `Squad.Strength.cs:52`) | ok |
| Zuganfang `GameState.HotSeat.cs:67–71` (`BeginManagerTurn`) | Probe: 2 Manager, Automatik aus, `game.Continue()` (nur Zugwechsel); Mengen-Diff der Abdruckzeilen | neu nur: `CurrentManager 0→1`, `_readyManagers`, `_contractTurnFactor = 94`, Zustand `_turnRandom` – Gehaltsfaktor je Zug, keine Nummern, kein Hauptzufall | ok |
| Erster Zug `NewGame.cs:64` → `BeginFirstTurn` (`GameState.MainMenu.cs:63`, ui-rest #540) | Code: zieht nur `TurnRandom` beim Spielstart (Kern, kein Bildschirm); `MainMenuEventTests` grün (Core-Lauf 19/0/0) | Gehaltsfaktor je Managerzug, im Kern | ok |
| Saisonende-Seiten je Manager (`GameState.SeasonEndTurn.cs:29–43`, `SeasonEndSummaryScreen.cs:26–29, 82`) | Probe bis `SeasonEndScreen`: 5 Frames ohne Eingabe → Bytes gleich; Klick → `SeasonEndSummaryScreen.Open` → einzige Abdruckzeile `CurrentManager 1→0`; 5 Frames ohne Eingabe → Bytes gleich; Klick → Seite 1: nur `CurrentManager 0→1`. Vorhandener Test `BildschirmeOeffnenZustandTests.SeasonEndScreens_…` grün | Wechsel nur durch Eingabe, nur Zeiger auf den Manager am Zug (Original 0x26FB0); Ligamischung 29.04. im Kern (`SeasonFlow.cs:320`) | ok |
| Freiprüfung `BildschirmeFreipruefungTests` (pruefung-karte T1) – Abdeckung | `EM_FREIPRUEFUNG_BERICHT=… dotnet test --filter BildschirmeFreipruefungTests|BildschirmeOeffnenZustandTests` → 7/0/0; Bericht: Start 112 / MidSeason 112 / NationalCoach 113 geöffnet, 0 Abweichungen. Deckt alle oben genannten Screen-Typen per Konstruktor-Reflexion ab. Mutation in `StadiumScreen.Draw` → Freiprüfung (3 Zustände) + `MenuTileScreens_…` rot; Mutation in `StadiumScreen.Update` → **alle 7 grün**, nur meine Probe rot. Mutation `OnMainMenu` in `MainMenuScreen.Draw` bzw. Umnummerieren im Aufbau → Freiprüfung grün (nur Spezialtests rot) | Freiprüfung prüft nur Enter + ein `Draw`, nicht „Update ohne Eingabe“ (§4 „bloßes Öffnen“ umfasst laufende Frames); Lücke im Prüfmittel, kein Verstoß im Code | offen (Testlücke) |
| Ausnahmen der Freiprüfung, Issue #610 | `gh api …/issues/610` → open; Code `BildschirmeFreipruefungTests.cs:19` (Remark: „scheitert … bei jeder nicht mehr auftretenden bekannten Abweichung“) vs. `:193` (prüft nur neue); `Known` leer; nicht erzeugbar nur im Bericht: `SeasonEndScreen`, `SeasonEndSummaryScreen` (Ctor, `Open`), `NationalMenu.Open`, `NationalLineupScreen.Tactics` (ohne Bundestrainer) | Ausnahmen bestehen unverändert; Saisonende über `BildschirmeOeffnenZustandTests` nur für `SeasonEndSummaryScreen`-Ctor, `SeasonEndScreen` selbst wird dort nicht vor/nach verglichen (meine Probe: ok) | offen (#610 bestätigt) |

**Zählung:** 17 Zeilen: ok 15, Abweichung 0, offen 2.

## Anhang C – Mutationen UI-Tests

#### Gegenproben UI-Tests (Fork C, Arbeitskopie `wt-C` am Head `b634032`, Schalter `RVMUT`, je Lauf `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.UI.Tests -c Release --no-build --filter …`)

Ohne Schalter alle 19 Tests grün (Läufe vor den Mutationen). Mutationen nur in der Arbeitskopie, nicht committet.

| Test | Art | Mutation (Datei → Änderung, Schalter) | ohne Mutation | mit Mutation (Meldung kurz) |
|---|---|---|---|---|
| AdvertisingArrowTests.HeldArrow_RepeatsOnlyAfterBrake_AndClampsAtTheEnd | neu | `AdvertisingScreen.Browse`: Umbruch `(Cursor+delta+count)%count` statt Klemmen 0x2BE0A (`ADV_WRAP`) | grün | rot: erwartet 9, ist 1 |
| ″ | | `AdvertisingScreen.Update`: Bremse aus, jeder gehaltene Frame blättert (`ADV_NOBRAKE`) | grün | rot: erwartet 0 (Bremse), ist 9 |
| BildschirmeFreipruefungTests.Detector_SeesStateChange | neu | `SeasonFlow.Continue`: sofort `return []` (`DET_NOCONT`) | grün | rot: `Assert.False` (Bytes gleich) |
| BildschirmeFreipruefungTests.AllScreens_OpeningAndDrawingDoNotChangeGameState | neu | `StrengthScreen.Draw`: `Game.Random.Next()` (`FP_DRAW`) | grün 3/3 | rot 3/3 (neue Abweichungen) |
| ″ | | `MainMenuScreen.Draw`: `OnMainMenu(…, isReturn: true)` (`MMD_EVENT`) | grün 3/3 | grün 3/3 – in den Ausgangszuständen der Freiprüfung ändert ein zusätzliches `OnMainMenu` nichts (Kennzeichen schon gelöscht, Aufstellung voll); abgedeckt durch `MainMenuDrawTests` (unten) |
| KaderStartSetupScreenTests.ClubPicker_ShowsStartOrder_PageThreeInDatabaseOrder | neu | `SetupScreen`-Konstruktor: `_places` = Datenbankfolge statt `NewGame.StartOrder` (`SP_DBORDER`) | grün | rot: erwartet 4, ist 0 |
| LifecycleScreenRegressionTests.R204_OriginalDataLoanAndContractEndAreConsistentAfterContinueThroughSeason | geändert (Kommentar, Ausgang) | `NewGame.StartOrder`: 0 statt 55 Tausche je Liga, R-K5 (`RK5_NOMIX`) | grün | rot: Spieler im Pool (zweiter Ausgang, wie im Kommentar für Teil 2) |
| ″ | | `NewGame.AssignClubs`: kein Starttausch U15-a (`U15A_NOSWAP`); zusätzlich `RK5_NOMIX,U15A_NOSWAP` | grün | grün bzw. grün (beides aus = Base-Verlauf, erster Ausgang) – stimmig mit der Kommentar-Kette |
| MainMenuDrawTests.Draw_DoesNotConsumeNewMessageOrRepairLineup | neu | `MainMenuScreen.Draw`: `OnMainMenu` im Zeichnen (`MMD_EVENT`) | grün | rot: Kennzeichen „neue Nachricht“ gelöscht |
| NationalLineupScreenTests.A010_FullLine_NoRoom_ClickOnPlacedTakesOut | neu | `NationalCoach.ClickPlayer`: volle Reihe → ersten Platz überschreiben statt `NoRoom` (`NL_NOROOM`) | grün | rot: Formation geändert |
| ″ | | `ClickPlayer`: Herausnehmen behält „eingesetzt“ auch bei Linksklick (`NL_TAKEOUT`) | grün | rot: `IsDressed(placed)` true |
| NationalLineupScreenTests.Lineup_Clicks_FollowOriginalRules_AndTacticsBelongToNationalTeam | geändert | `ClickPlayer`: zweiter Rechtsklick nimmt nicht heraus (0x40F59) (`NL_RIGHTUNDRESS`) | grün | rot: `Assert.False(IsDressed)` |
| PixelWettbewerbeTests.WorldCupKnockout_IsEndrundenspieleScreen | geändert | `CupPairingsScreen`: Knopf 1 öffnet `WorldCupScreen` statt Endrunde (`WK_ROUTE`) | grün | rot: falscher Bildschirmtyp |
| SaveLoadScreenTests.AutoSave_OverFiveSeasons_LoadedStateContinuesIdentically | geändert (Helfer `extendExpired: true`, R12K-03) | `KeepSquadHelper`: `extendExpired` wirkungslos (`KS_NOEXTEND`, Testhelfer) | grün | rot: `InvalidOperationException` (Saison/Spiel endet, Entlassung) – Anpassung nötig |
| SeasonEndColorsTests.Palette18_12_22_MatchDosBoxPictures | neu | `SeasonEndScreen.Draw`: Palette mit Eintrag 18 = (238,0,0) (`SE_PAL`) | grün | rot: [243,0,0] erwartet, [238,0,0] |
| ″ | | `SeasonEndScreen`: Zeichenfarbe Absteiger 238 statt 18 (`SE_CONST`) | grün | **grün – nicht rot zu machen**: der Test prüft nur die Palette, nicht die Farbindizes der gezeichneten Zeichen (0x4CCB4) |
| StadiumBuildTableTests.RightClickBelowY167_TogglesBuildTable_OtherwiseLeaves | neu | `StadiumScreen.Update`: `y >= 167` statt `> 167` (0x3DB62) (`ST_Y`) | grün | rot: bleibt im Stadion bei y = 167 |
| StadiumBuildTableTests.BuildTexts_FollowTheOriginalStrings | neu | `StadiumScreen.FeatureValue`: „(noch“ statt „(Noch“ (`ST_TEXT`) | grün | rot: Strings differ |
| StadiumScreenTests.RightBlockColumnsMatchDosBoxImage | geändert (211 → 212) | `ArrowPair.DrawReleased`: Pfeil bei x + 3 statt x + 2 (kb\4) (`ST_ARROWX`) | grün | rot: erwartet 213, ist 212 |
| StrengthInfoWindowTests.RowAt_FollowsOriginalHitTest | neu | `StrengthScreen.RowAt`: `(y − 28)/15` (`SI_PITCH`) | grün 5/5 | rot 1/5 (y = 42) |
| StrengthInfoWindowTests.RowAt_OutsideIsNull | neu | `RowAt`: `y < 27` statt `y <= 27` (`SI_BOUND`) | grün 4/4 | rot 1/4 (y = 27) |
| StrengthInfoWindowTests.ClickOnClubRow_OpensClubWindow_ClosesWithoutChangingState | neu | Klick öffnet kein Fenster (`SI_NOCLICK`); Klick verbraucht `Game.Random` (`SI_STATE`) | grün | rot / rot (Fingerabdruck) |
| StrengthInfoWindowTests.ClickOnCountryRow_OpensNationWindow | neu | `SI_NOCLICK`; `InfoBarWindows.Show` immer Vereinsfenster (`SI_CLUBONLY`) | grün | rot / rot |
| WettbewerbeNationalteamTests.CoachWarning_Boundary | geändert (A028) | `MainMenuScreen`: Warnschwelle `CoachMinPlayers − 1` (`CW_MIN`) | grün 2/2 | rot 1/2 (9 Spieler ohne Warnung) |
| ″ | | `CupsNationalMode`: Halt nur an Spieltagen Deutschlands wie vor A028 (`CW_A028`) | grün 2/2 | **grün – geänderter Zweig unspezifisch**: `CurrentDay > day` gilt mit und ohne A028; A028 selbst prüft `NationalteamRunde13Teil2Tests.A028_CoachHaltsOnEveryWorldCupDay` (am Base rot) |

## Anhang D1 – Mutationen kader-start und Saaten

#### Fork D1 – Gegenproben kader-start (Core) und Saat-Rückführung

Arbeitskopie `/home/claude/wt-D1` = Head `b634032`, Mutationen über Umgebungsschalter `RVMUT` (Datei `src/EishockeyManager.Core/ZzRvMut.cs`, Schalter an den genannten Stellen), eine Build, `dotnet test -c Release --no-build`, `EM_REQUIRE_GAMEDATA=1`. „ohne“ = gleicher Build ohne Schalter. Rohausgabe Saaten: `/home/claude/rv/d1_seeds.txt`.

#### Tests

| Test | Art | Mutation (Regel zurückgenommen) | ohne | mit |
|---|---|---|---|---|
| KaderStartReihenfolgeTests.StartOrder_PagesHoldTheirLeague_PageThreeUnmixed (3 Fälle) | neu | `RK5`: `NewGame.StartOrder` ohne 55 Tausche (Indexfolge) | grün | rot 3/3 (NotEqual: Seite ungemischt) |
| KaderStartReihenfolgeTests.NewGame_LeagueSlotsAreTheStartOrder (2) | neu | `SLOTREMIX`: `InitializeLeagueSlots` mischt nach Übernahme der Startmischung erneut (Stand vor R-K5) | grün | rot 2/2 |
| KaderStartReihenfolgeTests.StartSwap_ExchangesSlots_OthersKeepStartOrder (3) | neu | `NOPLACESWAP`: `PlaceInStartLeague` tauscht die Satzplätze nicht | grün | rot 3/3 |
| KaderStartRunde13Teil2Tests.Duration_OtherStartLeague_StopsAtThreeSeasons (2) | neu | `NOLOCK`: Sperre „eine Saison ⇒ Bundesliga“ in `GameSetup.Step` entfernt | grün | rot 2/2 |
| KaderStartRunde13Teil2Tests.StartLeague_OneSeason_StaysBundesliga | neu | `NOLOCK` | grün | rot |
| KaderStartRunde13Teil2Tests.Bundesliga_OneSeasonAllowed_LongerDurationAnyLeague | neu | `LOCKALL`: Sperre auch für Bundesliga | grün | rot (Dauer bleibt DreiSaisons) |
| KaderStartRunde13Teil2Tests.LevelAndStrength_UnaffectedByLock_NeedManager | neu | `LOCKALL` (Sperre wirkt auf alle Regler) | grün | rot |
| KaderStartRunde13Teil2Tests.StartLeague_SharedByAllManagers | neu | `NOSHARE`: Setter `GameSetup.StartLeague` setzt nur Manager 0 (L1 zurück) | grün | rot (2 von 3 Managern) |
| KaderStartRunde13Tests.FreePoolSlotAtAge60_RetiresAndGetsYoungSuccessorInSameSlot (2) | neu | `NOPOOLRENEW`: `RenewAgedPoolPlayer` im Saisonwechsel des Pools nicht aufgerufen | grün | rot 2/2 |
| KaderStartRunde13Tests.KeepSquad_DefaultExtendsOnlyOfferedContracts | neu | `KEEPDEF`: Testhelfer `TestSeason.KeepSquad` Standard wieder „auch ohne Angebot verlängern“ (R12K-03 zurück) | grün | rot (2 statt 1) |
| KaderStartStartLigaTests.FixedClubAlreadyInStartLeague_Kept (2) | neu | `KEEPRULE`: Behalten-Bedingung (Datei 0x29C3B) entfernt, immer ziehen | grün | rot 2/2 |
| ClubAssignmentTests.RandomThenFixed_SameBand_NeverShared | geändert (Aufbau) | `NORESERVED`: Zufallswahl schließt vergebene Vereine nicht aus | grün | rot |
| ClubAssignmentTests.NoLeagueClubFirst_DoesNotDisplaceLaterFixedOberligaClub | geändert (Aufbau) | `NOEARLIER`: Zufallsplatz schließt Vereine früherer Manager nicht aus (0x29C9B) | grün | rot (Managerverein ohne Liga) |
| AutoLineupTeil8Tests.Migration_31To32_KeepsHandMadeLineup | geändert (Saat) | `MIG31`: Migration 31→32 setzt `_autoLineup` immer true | grün | rot |
| FinanceCalibrationTests.Premium_DependsOnTitlesAndPromotion_NotEstimatedTableAwards (7) | geändert (Aufbau) | `NOPROMO`: Aufstiegsprämie in `SeasonPremium` entfällt | grün 7/7 | rot 3/7 (alle Aufstiegsfälle) |
| FinanceWerbungTests.Rating_IgnoresEternalChampionships | geändert (Aufbau) | `ETERNAL`: Werbebewertung +50 je ewiger Meisterschaft | grün | rot (29 → 40) |
| SponsorOfferTests.Rating_FreshClub_IsModerate_AndBundesligaHigher | geändert (Aufbau) | `TIERFLIP` (nur `Finish` mit 2−tier): **bleibt grün**; `TIERCONST` (Kostenliga 0 für alle) | grün | `TIERFLIP` grün, `TIERCONST` rot (56 außerhalb 10–45) |
| KaderStartTests.UiVideo_BerlinerSc_Mittelfeld_Values29To33 | geändert (Aufbau) | `STARTBASE`: Startbasis +6 (Tabelle DGROUP 0x4B42) | grün | rot (20/20 Spieler außerhalb) |
| KaderTransferTeil5PoolTests.Start_TwoLeagues_PlayerCountsForBothAndEndsInSecond | geändert (Aufbau) | `POOLBREAK`: Ligaschleife 0x057E3 bricht nach erster Zuteilung ab | grün | rot (40/40) |
| NewGameTests.Create_ClubWithoutLeagueMovesIntoOberliga | geändert (Aufbau) | `NOLEAGUE`: Verein ohne Liga wird nicht gesetzt | grün | rot (null) |
| SeasonFlowTests.Dismissal_OtherManagersContinue | geändert (Aufbau) | `DISMISSALL`: Spielende schon bei einer Entlassung | grün | rot |
| NummerierungMigrationTests.MainMenu_WithAutoLineupOff_KeepsNumbers | neu (Fix #615) | `MMRENUM`: `RenumberPlayers()` am Anfang von `ApplyAutoLineup` wieder eingesetzt | grün | rot (77 → 1) |
| NummerierungMigrationTests.MainMenu_WithAutoLineupOn_DoesNotRenumber | neu (Fix #615) | `MMRENUM` | grün | rot (77 → 1) |
| NummerierungMigrationTests.CurrentSave_RoundTrip_Unchanged | neu (Fix #615) | `MIGALL` (Migration 38→39 auf jeden Stand): **bleibt grün** (Nummern sind bereits Platz+1); `MIGALL,RVOFF` (dieselbe Migration mit Nr. = Platz+2) | grün | `MIGALL` grün, `MIGALL,RVOFF` rot |

#### Saaten (Teil 2)

Schalter `OLDSEED` setzt die alte Saat (`RvMut.S(neu, alt)` an der geänderten Zeile); `U15A` = Starttausch zurück (Base-Logik: nur Verein ohne Liga rückt in die Oberliga, kein Zug, kein Tausch); `RK5` = Startmischung zurück (`StartOrder` = Indexfolge, Satzfolge wie Base: Indexfolge + Mischung).

| Test | Saat alt→neu | alte Saat am Head | alte Saat + Rücknahme U15-a | + Rücknahme R-K5 (nur) | beide zurück | Ursache |
|---|---|---|---|---|---|---|
| AutoLineupTeil8Tests.Migration_31To32_KeepsHandMadeLineup | 3→4 | rot (60 vs 61) | grün | grün | grün | Zusammenspiel U15-a + R-K5 (jede Rücknahme allein heilt) |
| IntegrationRunde9Tests.PlayoffWithoutManager_FastUnlessResultsReported (2) | 3→5 | rot 2/2 | grün | rot 2/2 | grün | U15-a |
| NationalCoachMode4Tests.Mode4_TwoManagersInTopFive_HigherPlacedBecomesCoach | 3→17 | rot | grün | rot | grün | U15-a |
| NationalCoachMode4Tests.Mode4_NoManagerInTopFive_NoCoach | 3→4 | **grün** | grün | rot | grün | am Head nicht nötig; nötig nur mit U15-a ohne R-K5 (Zwischenstand Teil 2) |
| NationalCoachMode4Tests.Mode2_ManagerInTopFive_NeverCoach | 3→22 | rot | grün | grün | grün | Zusammenspiel U15-a + R-K5 |
| NationalCoachMode4Tests.Mode4_Appointment_BeforeReroll_GermanyKeepsBaseRatings | 3→2 | rot | grün | grün | grün | Zusammenspiel U15-a + R-K5 |
| SaisonwechselProbenTests.PlayoffWithoutManager_EachResultsFieldAloneSwitchesToMinuteLoop (6) | 3→5 | rot 6/6 | grün | rot 6/6 | grün | U15-a |
| SeasonEndStatusTests.Full_ManagerInTopFive_GetsStatusBit_OtherNot | 3→4 | **grün** | grün | grün | grün | am Head nicht rückführbar (keine Kombination macht alte Saat rot) |
| SeasonEndStatusTests.Full_EveryManagerInTopFive_GetsStatusBit | 3→17 | rot | grün | rot | grün | U15-a |
| SeasonEndStatusTests.ClubOnly_ManagerInTopFive_NoStatusBit | 3→22 | rot | grün | grün | grün | Zusammenspiel U15-a + R-K5 |
| SeasonEndStatusTests.SaveLoad_StatusBitIsDerivedFromSavedData | 3→4 | **grün** | grün | grün | grün | am Head nicht rückführbar |
| StatistikenPraemienTests.OtherClubLeads_NoSinglePremium | 3→4 | rot | grün | grün | grün | Zusammenspiel U15-a + R-K5 (Kommentar nennt zusätzlich S-K6-c, hier nicht getrennt geprüft) |
| WettbewerbeSpiel1Tests.DebCup_NoMatchEndsLevel_ShootoutInFinalScore (Fall 4/false) | 5→4 | **grün** (3/3) | grün | rot 1/3 | grün | am Head nicht nötig; nötig nur mit U15-a ohne R-K5 |
| WeltkugelDosboxTests (UI, Helfer `Synthetic`, 14 Tests) | 3→22 | rot 1/14 (InRange, 0) | grün | grün | grün | Zusammenspiel U15-a + R-K5 |

Hinweis „Zusammenspiel“: Die Rücknahme jeder der beiden Regeln allein stellt die alte Saat wieder her; die Saat fällt erst mit beiden Regeln zusammen (Stand ab Teil 3).

## Anhang D2 – Mutationen saison, nationalteam, übrige

#### Fork D2 – Gegenproben per Mutation (Head b634032, Kopie /home/claude/wt-D2)

Methode: Schalter `RVMUT` (Datei `Core/ZzMut.cs`, nur in der Kopie), eine Build, `dotnet test tests/EishockeyManager.Core.Tests -c Release --no-build --filter …` mit `EM_REQUIRE_GAMEDATA=1`. Ohne Mutation: alle 44 Fälle der Auswahl grün (1 min 8 s).

| Test | Art | Mutation (Datei → Änderung, Schalter) | ohne | mit (Meldung kurz) |
|---|---|---|---|---|
| ClubRecordsSeasonTests.Season_WithCupAndPlayoffs_RecordsComeOnlyFromLeagueMatches (5, 6) | neu | `MeetingHistory.cs` `RecordMeetings` (Play-offs/Pokal) trägt zusätzlich `EnterLeagueRecords` ein (REC) | grün 2/2 | rot 2/2 (Rekordtupel weicht ab, Expected (2304, 1)) |
| IntegrationRunde9Tests.PlayoffWithoutManager_FastUnlessResultsReported (2 Fälle) | geändert (Saat) | `GameOptions.cs` `CupMatchUsesMinuteLoop`: Play-off/Aufstiegsrunde immer schneller Weg (PO) | grün 2/2 | rot 2/2 (NotEqual: Werte gleich) |
| SaisonwechselProbenTests.PlayoffWithoutManager_EachResultsFieldAloneSwitchesToMinuteLoop (6 Fälle) | geändert (Saat) | wie oben (PO) | grün 6/6 | rot 6/6 (NotEqual: Werte gleich) |
| MainMenuEventTests.NewMessageAndTurnFactor_RoundTripAndMigrate | geändert | `Transfer.Contract.cs` `ContractTurnFactor`: kein abgeleiteter Ersatzwert bei 0 (TF) | grün | rot (InRange 85–115: 0) |
| NationalCoachMode4Tests.Mode4_TwoManagersInTopFive_HigherPlacedBecomesCoach | geändert (Saat) | `CupsNationalMode.cs` `TopFiveManager` von unten durchsuchen (TOPLAST) | grün | rot (Expected 1) |
| NationalCoachMode4Tests.Mode4_NoManagerInTopFive_NoCoach | geändert (Saat) | `TopFiveManager` über alle 12 Plätze (TOPALL) | grün | rot (NationalCoach nicht null) |
| NationalCoachMode4Tests.Mode2_ManagerInTopFive_NeverCoach | geändert (Saat) | `OnNationalMarkerDay` ohne Prüfung `Mode == Full` (MODE) | grün | rot (NationalCoach nicht null) |
| NationalCoachMode4Tests.Mode4_Appointment_BeforeReroll_GermanyKeepsBaseRatings | geändert (Saat) | `OnNationalMarkerDay`: Auswürfeln vor der Ernennung (ORDER) | grün | rot (Länderwerte ≠ (84, 91, 93, 91)) |
| NationalteamRunde13Teil2Tests.A027_WorldOnly_HaltsAtFirstWorldCupDay_WithEmptySquad | neu | `CupsNationalMode.cs` `SkipToWorldCup`: nach der Ernennung `AutoNominate()` (A027) | grün | rot (Kader nicht leer) |
| NationalteamRunde13Tests.A023_FriendlyBonus_AsFile18ToFile19 | neu | `CupsNational.cs` `ApplyBonus`: Abzug −1 je nach x entfernt (BONUS) | grün | rot (Expected (80, 83, 55)) |
| NationalteamRunde13Tests.A010_FullLine_NoRoom_NothingChanges | neu | `CupsNational.Lineup.cs` `ClickPlayer`: bei voller Reihe freien Platz in anderer Reihe nehmen statt `NoRoom` (NOROOM) | grün | rot (Expected NoRoom) |
| NationalteamRunde13Tests.A010_ClickOnPlacedPlayer_TakesOutAndSelectsHisLine | neu | `ClickPlayer`: Linksklick-Herausnehmen lässt eingesetzt (`keepDressed` immer true, TAKE) | grün | rot (IsDressed true) |
| NationalteamRunde13Tests.A010_LineRulesAndRightClick | neu | `ClickPlayer`: ohne gewählte Reihe Reihe 0 annehmen (NOLINE) | grün | rot (Expected NoLineSelected) |
| SaisonSatzfolgeSaisonendeTests.MultiSeason_NextSlotsFormedOn29April_StartNextSeasonAdoptsThemUnchanged (passiv, aktiv) | neu | `SeasonFlow.LeagueSlots.cs` `PrepareNextLeagueSlots` bildet nichts (alter Zeitpunkt StartNextSeason, PREP) | grün 2/2 | rot 2/2 |
| SaisonSatzfolgeSaisonendeTests.Preparing_NextSlots_DoesNotAdvanceGameRandom | neu | `Mix` zieht aus `GameState.Random` statt aus abgeleiteter Quelle (MIXRAND); Wirksamkeit belegt: alle 4 synthetischen RngTwin-Abdrücke rot | grün | **grün – nicht rot zu machen**: Der Test vergleicht zwei gleich gesäte Spiele; jede deterministische Änderung trifft beide gleich. Er prüft nicht, dass die Hauptquelle unverändert bleibt (möglicher Befund P3, Test ohne Aussage) |
| SaisonSatzfolgeSaisonendeTests.FairPlay_Promoted_ComparedWithNewLeague_NotOldLeague (true, false) | neu | `SeasonFlow.FairPlay.cs`: Sieger und Prämie über die alte Liga in laufender Folge (FAIRWIN+FAIREV); ebenso PREP | grün 2/2 | rot 2/2 (beide Mutationen) |
| SaisonSatzfolgeSaisonendeTests.FairPlay_OberligaManagerWithZeroMinutes_WinsOnlyIfSlotBeforeBothNewcomers | neu | wie oben (FAIRWIN+FAIREV); ebenso PREP | grün | rot (beide Mutationen) |
| SaisonSatzfolgeSaisonendeTests.SaveAndLoad_InSeasonOver_KeepsNextSlots_ByteIdentical_SameSeasonAfterwards | neu | `SaveGame.Reader.cs` nach dem Lesen `_nextLeagueSlots` leeren (LOADNEXT); zusätzlich Feld aus der Feldliste entfernt (SAVENEXT) | grün | rot (LOADNEXT: Bytes ab Pos. 438499 verschieden; SAVENEXT: NullReference) |
| SaisonSatzfolgeSaisonendeTests.Schema37Save_InSeasonOver_WithoutNextSlots_Migrates_StartNextSeasonFormsSlots | neu | `AdvanceLeagueSlots` Rückfallweg bildet keine Folge (NOFALLBACK2). Nur das Mischen im Rückfallweg wegzulassen (NOFALLBACK) bleibt grün: die Folge erbt die gemischte Startfolge, der Test prüft also das Bilden, nicht das erneute Mischen | grün | rot (NOFALLBACK2: keine Liga außer Indexfolge) |
| SaisonSatzfolgeSaisonendeTests.NextLeagueSlots_AndFairPlayWinner_AreReadOnly | neu | `FairPlayPremiumWinner` zieht `Random.Next(2)` (RO) | grün | rot (Spielstand verschieden ab Pos. 92791) |
| SaisonendeManagerZugTests.SeasonEndPages_EachManagerOnTurnForOwnPage_ThenTurnOrderStartsAtFirst | neu | `GameState.SeasonEndTurn.cs` `ShowSeasonEndManager` ohne Wirkung (SEPAGE) | grün | rot (Expected 1) |
| SaisonendeManagerZugTests.SeasonEndPages_DismissedManagerHasNoPage_FirstActiveManagerOnTurnAfterwards | neu | `ReturnToFirstActiveManager` setzt immer Manager 0 (SEFIRST) | grün | rot (Expected 1) |
| SaisonwechselSatzfolgeTests.MultiSeason_SlotsRemixedEachSeasonChange_MembersFollowPromotion (passiv, aktiv) | geändert | `SeasonFlow.LeagueSlots.cs` `Mix`: keine 55 Tausche (NOMIX) | grün 2/2 | rot 2/2 |
| SaisonwechselSatzfolgeTests.Schema32Save_WithoutSlots_MigratesTo34_LoadsWithIndexOrder_MixedAtNextSeasonChange | neu (ersetzt …LoadsWithIndexOrder…) | wie oben (NOMIX) | grün | rot (keine Liga gemischt) |
| SeasonEndStatusTests.Full_ManagerInTopFive_GetsStatusBit_OtherNot | geändert (Saat) | `SeasonFlow.cs` `HasTopFiveStatus` ohne Platzprüfung (STALL) | grün | rot (Assert.False) |
| SeasonEndStatusTests.SaveLoad_StatusBitIsDerivedFromSavedData | geändert (Saat) | wie oben (STALL) | grün | rot (Assert.False) |
| SeasonEndStatusTests.ClubOnly_ManagerInTopFive_NoStatusBit | geändert (Saat) | `HasTopFiveStatus` ohne Modusprüfung (STMODE) | grün | rot (Assert.False) |
| SeasonEndStatusTests.Full_EveryManagerInTopFive_GetsStatusBit | geändert (Saat) | `HasTopFiveStatus` nur für den höchstplatzierten Manager (STONE) | grün | rot (Assert.True) |
| StatistikenPraemienTests.MultiSeason_FourChanges_PassiveAndActiveManager | geändert | `SeasonEndSinglePremiums` wieder über die alte Liga (FAIREV) | grün | rot (Ereignisse ≠ [TopScorer]). Hinweis: Mutation nur von `FairPlayPremiumWinner` (FAIRWIN) bleibt grün – der Test bildet die Erwartung mit derselben Funktion (tautologisch für die Siegerregel; geprüft wird nur der Anschluss) |
| WettbewerbeSpiel12Teil3Tests.MultiSeason_ComputerClubsPlayWithoutSquad (passiv, aktiv) | geändert (Helfer) | `MatchEngine.Live.cs` `TeamValue`: Computervereine wie Managervereine aus der Aufstellung (TV) | grün 2/2 | rot 2/2 (Expected 95) |
| WettbewerbeSpiel1Tests.DebCup_NoMatchEndsLevel_ShootoutInFinalScore (5/true, 11/true, 4/false) | geändert (Saat 5 → 4) | `Cups.Tiebreak.cs` `FromReport`: Penaltytore nicht in den Endstand (SHOOT) | grün 3/3 | rot 3/3 (Gleichstand 4:4) |

Auffällig (möglicher Befund P3): `Preparing_NextSlots_DoesNotAdvanceGameRandom` ist durch keine deterministische Mutation rot zu machen; `StatistikenPraemienTests.MultiSeason_…` prüft die Fair-Play-Siegerregel nicht selbst (nur über die gezielten Tests in `SaisonSatzfolgeSaisonendeTests`).

## Anhang E – Sollwerte und Fingerabdrücke

#### Teil 2: Sollwerte, Fingerabdrücke und Saaten auf ihre Regel zurückgeführt (Fork E, Head `b634032`)

Methode: In einer Kopie des Heads (`wt-E`) wurde jede Regel einzeln über einen Umgebungsschalter `RVMUT` zurückgenommen. Je Projekt gab es eine Build, die Läufe liefen mit `EM_REQUIRE_GAMEDATA=1`. Ohne Schalter waren alle betroffenen Tests grün (13 + 52 + 11 Fälle). Die Schalter:
`RK5` = `NewGame.StartOrder` ohne die 55 Tausche (NewGame.cs:190).
`U15A` = kein `PlaceInStartLeague` (NewGame.cs:99).
`U15A2` = kein Starttausch nur für Ligavereine.
`U15NL` = ligaloser Verein wie am Base (zufälliger Oberliga-Verdrängter, kein Stärketausch).
`SK6A` = in `AdvanceLeagueSlots` die Folge vom 29. April verwerfen und neu bilden (SeasonFlow.LeagueSlots.cs:120).
`SK6C` = alte Prämienregel `SinglePremiumEvents` (SeasonFlow.cs:366).
`A091` = `RenumberPlayers` ohne Wirkung (Squad.Strength.cs:39).
`A166` = `SortedPlayers` in Auto-Aufstellung (Squad.AutoLineup.cs:188) und `LiveSquad` (MatchEngine.Managed.cs:88).
`A058`/`A163` = Saisonwanderung bzw. Oberliga-Austausch im Modus „nur Weltkugel“ aus (GameState.Integration.cs:105, SeasonFlow.cs:299).
`A009F` = Länderwerte wieder nach dem Freundschaftsspiel schreiben.
`A009L` = `OnNationalLineupLeft` ohne Wirkung.
`A009D` = Tageslauf ohne `UpdateNationalTeamValues`.
`A085` = Gleichstand wieder nach dem Namen.
`A023` = Felder 0x10/0x11 vertauscht.
`FIRST` = `BeginFirstTurn` ohne Wirkung.
`SLOTFB` = kein Rückfall auf die Indexfolge.
`ADVRECT`/`STADRECT` = Pfeilflächen wie am Base.
`STADARR` = Stadionpfeile nicht zeichnen.
`NEWSBAR` = ohne den Vergleich der neuesten und ältesten Meldung.
`SECOL` = Saisonende-Farben 238/13/151 wie am Base.
`RVKEEP=flip` = Hilfsdefault `extendExpired` umkehren (R12K-03).

| Test/Fall | Sollwert | genannte Regel | Rückmutation | Ergebnis (Wert fällt?) |
|---|---|---|---|---|
| RngTwin synthetisch Saat 3 passiv/aktiv | `1501663F…`/`377126FA…` | S-K6-a/c, U15-a, R-K5, A091/A166 | SK6A / SK6C / U15A / U15A2 / RK5 / A091 / A166 | SK6A **nein** (beide grün); SK6C ja/ja; U15A ja/ja; U15A2 ja/ja; RK5 ja/ja; A091 ja/ja; A166 nein/ja |
| RngTwin synthetisch Saat 11 passiv/aktiv | `6B6DAAD4…`/`C429365C…` | U15-a, R-K5, A091/A166 (S-K6 laut Kommentar nur Saat 3) | wie oben | SK6A nein/nein; SK6C nein/ja; U15A ja/ja; U15A2 ja/ja; RK5 ja/ja; A091 ja/ja; A166 nein/ja |
| RngTwin Originaldatenbank passiv/aktiv | `C2A2F884…`/`FD69401E…` | S-K6-a/c, R-K5, A091/A166; U15-a laut Kommentar „unverändert“ | SK6A / SK6C / U15A2 / U15NL / RK5 / A091 / A166 | SK6A nein/nein; SK6C ja/ja; U15A2 nein/nein (wie behauptet); **U15NL ja/ja** (neu `C462C689…`/`010B15F1…`); RK5 ja/ja; A091 ja/ja; A166 nein/ja |
| Spielberechnung CountersPerSeason passiv/aktiv | `6C2D3078…`/`98AD5EF3…` | S-K6 „passiv neu, aktiv unverändert“, U15-a (Verein 2), R-K5, A091/A166 | SK6A / SK6C / U15A2 / RK5 / A091 / A166 | SK6A nein/nein; SK6C ja/**ja** (aktiv hängt am Head auch von S-K6-c ab); U15A2 ja/ja; RK5 ja/ja; A091 ja/ja; A166 nein/ja |
| Spielberechnung `WorldOnlyReference` | `…R982137502 C211999367` | A058, A163, U15-a, R-K5 | A058 / A163 / U15A / RK5 | alle vier ja; SK6*, A091, A166 nein |
| GameModeTests `Mode4Reference` | Konten/Titel 5 Saisons | S-K6-c, U15-a, R-K5 | SK6C / U15A / U15A2 / RK5 | alle ja; SK6A, A091, A166 nein |
| GameModeTests.WorldOnly_SeasonRuns… | `LeftOberliga.Count = 2` | A163 | A163 | ja (0) |
| JugendTeams passiv/aktiv | `ExpectedPassive`/`ExpectedActive` | U15-a, R-K5; aktiv mit `extendExpired: true` (R12K-03) | U15A / U15A2 / RK5 / RVKEEP=flip | U15A ja/ja; U15A2 ja/ja; RK5 ja/ja; flip –/ja (passiv ruft KeepSquad nicht) |
| SaveGameTests.SchemaFingerprint | `39`, `8F2FBF53475D516B` | 38: `_nextLeagueSlots` (S-K6-a); 39 ohne Feld | Schemabeschreibung an Base 36 / b0688af 38 / Head 39 ausgegeben | 36 = `263E9784…`, 38 und 39 = `8F2FBF53…`; einzige Zeile mehr: `GameState._nextLeagueSlots: List<Int32>`. Ohne das Feld wäre der Abdruck wieder `263E…`. |
| StadiumScreenTests.RightBlockColumns | Spalte 212 statt 211 | kb\4-Pfeile enden bei 211 | RVCOL=211 / RVCOL=211 + STADARR | 211 am Head rot; 211 mit STADARR grün → 212 folgt aus den neuen Pfeilen |
| DebCupDrawFlow `Reference` | `1993: DEB 21, DM 6, Tab 15379 | …` | U15-a (Verein 42 → Oberliga), R-K5, R12K-03 | U15NL / U15A2 / RK5 / flip | U15NL ja (gibt genau den alten Base-Wert `1993: DEB 4, DM 6, Tab 15341 …`); U15A2 nein (42 ist ligalos, passt); RK5 ja; flip ja |
| SaveLoadScreen AutoSave 5 Saisons | kein Festwert; `extendExpired: true` | R12K-03 | flip | rot (Entlassung/Saisonende), true wird gebraucht |
| LifecycleScreen R204 | Verein 59 (unverändert eingetragen) | Teil 2 U15-a → anderer Ausgang, Teil 3 R-K5 → wieder 59 | U15NL / RK5 | U15NL grün; RK5 rot → 59 hängt am Head an R-K5 |
| WettbewerbeSpiel12Teil3 MultiSeason | `extendExpired: true` | R12K-03 | flip | passiv rot (2), aktiv grün |
| KaderStartRunde13Teil4 File05/FILE07/FILE26, SaleThenPurchase, MultiSeason SlotOrder | Nr. 26/25, Platzfolgen | A091 | A091 | alle 6 ja (Nr. 0); A166 nur LiveSquad_FollowsSquadSlots ja |
| KaderStartStartLiga Köln FILE27, FixedBundesliga (3 Saaten), Bundesliga_AlwaysDraws (≠ 90), TwoManagers, MultiSeason | Liga 2/Werte | U15-a | U15A2 | alle ja; NoLeagueClub (3) nur unter U15A/U15NL |
| NationalteamRunde13 A009_Friendly | (84,91,93,91) | A009 (kein Schreiben im Freundschaftsspiel) | A009F / A009L | ja (80,83,80,81) / ja |
| NationalteamRunde13 A009_SingleForward | (0,0,9,10) | A009 beim Verlassen | A009L | ja (84,91,…) |
| NationalteamRunde13 A009_ValuesFollowLineupOnlyAtDayOrLeaving | Teilwerte | A009 Tageslauf | A009D | ja |
| IntegrationRunde8Welle3.Friendly_KeepsGermanyRatings | (84,91,93,91) | A009 | A009F / A009L | ja (79,83,81,82) / ja |
| SpielberechnungKomplett4.StrengthTable_CoachNation, WettbewerbeSpiel3.InfoStrength_WithCoach | Länderwerte | A009 beim Verlassen | A009L | ja / ja |
| NationalteamRunde13 A023 | (80,83,55), (99,92,56) | Felder 0x10–0x12 = KO/TE/FO | A023 | ja (81,82,55) |
| NationalteamRunde13 A085 | [early, late] | Gleichstand nach Id statt Name | A085 | ja |
| FirstTurnFactorTests (Saat 3/4/5) | Faktor aus `_turnRandom` (z. B. 99) | R12U-04 | FIRST | 3/3 ja (91) |
| SaisonWeltkugel Oberliga [35,34] | Plätze 35/34 | A163 | A163 | 2/2 ja ([]) |
| SaisonWeltkugel Drift | Vereine gewandert | A058 | A058 | 2/2 ja („nur 0 Vereine“) |
| SaisonwechselProben WorldOnly 20 Wechsel | 2 je Wechsel | A163 | A163 | 2/2 ja (0) |
| SaisonwechselSatzfolge UnusualLeagueSize | Indexfolge | R12S-02 Rückfall | SLOTFB | ja ([2,3,11,…]) |
| NewsBarShown | 2 | Vergleich neueste/älteste Meldung | NEWSBAR | ja (1) |
| AdvertisingArrow HitAreas | (138,183,8,9) | kb\4-Flächen | ADVRECT | ja (137,181,13,11) |
| StadiumArrow TicketArrows | (192,189,8,9) | Flächen des Originals | STADRECT | ja (194,188,7,6) |
| SeasonEndColors Palette18_12_22 | RGB von 18/12/22 | Zeichenfarben 18/12/22 (0x4CCB4) | SECOL | **nein**: bleibt grün |

**Auffälligkeiten:**

- **E-1, unwirksame Gegenprobe:** `SeasonEndColorsTests.Palette18_12_22_MatchDosBoxPictures` prüft nur die RGB-Werte der Paletteneinträge 18/12/22 aus kb\21. Ob `SeasonEndScreen` diese Indizes benutzt, prüft der Test nicht. Mit den alten Indizes 238/13/151 bleibt er grün; die Regel `[ORIGINAL] 0x4CCB4` ist damit nicht abgesichert.
- **E-2, Begründung ungenau:** S-K6-a allein (Bildung am 29. April statt in `StartNextSeason`) ändert keinen der acht Abdrücke. `SlotSeed` liegt zwischen 29. April und `StartNextSeason` gleich. Die Abdruckänderung entsteht über S-K6-c, das die vorgebildete Folge nutzt. Die Kommentare nennen S-K6-a/c gemeinsam; als eigenständige Ursache trifft S-K6-a nicht zu.
- **E-3, Kommentar am Head veraltet:**
  - RngTwin: Am Head hängen die Originaldatenbank-Abdrücke am Starttausch des ligalosen Vereins 42 (U15NL rot). Der Kommentar nennt sie für Teil 2 „unverändert“; das gilt nur für den Tausch von Ligavereinen (U15A2).
  - SpielberechnungAnschluss: „aktiv unverändert“ (S-K6) gilt am integrierten Stand nicht mehr, SK6C macht auch den aktiven Abdruck rot.
- **E-4, Dokumentationslücke:** In `SaveGameTests` fehlt die Kommentarzeile für Schema 38 (`_nextLeagueSlots`, S-K6-a). Der Abdruck selbst ist richtig zugeordnet.
