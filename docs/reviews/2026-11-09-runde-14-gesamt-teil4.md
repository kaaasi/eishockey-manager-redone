# Gesamtreview Runde 14 – Teil 4 (in Arbeit)

## 1. Merge-Empfehlung

**Nein**, solange die geforderte Vollprüfung nicht abgeschlossen ist. Base `401efa7`, Head `b13046422b285404ad6ddbe7b5f1b18e22069eea`. Fortsetzung der Berichte #704, #705 und #706.

## 2. Befunde

Die in den Vorberichten dokumentierten Befunde bestehen fort. R14G3-01 ist mit I10 erneut bestätigt (`Import_RankSlotsMatchTable` bleibt grün, wenn nur `StoreRankSlot` entfällt). R14G3-02 ist mit R05 erneut bestätigt (beide passiven Datenzeilen bleiben bei einem sofortigen `GameOver` ohne Saisonwechsel grün; beide aktiven werden rot). Die als R14G3-03 festgehaltene Testlücke ist erneut reproduziert: H10 senkt die Mindestlänge von `Length` auf `Length - 1`; der vorhandene Test bleibt grün, da der zu kurze Eingabepuffer zusätzlich eine falsche Kennung besitzt.

### R14G4-01 (P3): Vergleich zweier Seitenzahlen erkennt ein konstantes Seitenlabel nicht

`Panel_ShowsNoPageLabel` ([SaveLoadDialogOriginalTests.cs:98](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/tests/EishockeyManager.UI.Tests/SaveLoadDialogOriginalTests.cs#L98)) vergleicht einen Bildausschnitt zwischen einem Verzeichnis mit einer Seite und einem mit zwei Seiten. Ein gleiches Label in beiden Bildern fällt dabei nicht auf.

**Gegenproben:** U16 ergänzt in `DrawPanel` ein sichtbares konstantes `SEITE: 1` im untersuchten Ausschnitt; der bestehende Test bleibt **grün**. Ein zusätzlicher unveränderter und mutierter Kontrolllauf speicherte den Ausschnitt: 102 von 700 Pixeln unterscheiden sich, das Label wird tatsächlich gezeichnet. U15 zeichnet stattdessen eine von der Seitenzahl abhängige Zahl; der Test wird **rot**. Die Lücke betrifft die Aussagekraft des Tests, nicht ein Seitenlabel im unveränderten Produkt. Ein unabhängiger Vergleich mit einem belegten Hintergrund oder die gezielte Prüfung auf den Text würde auch ein konstantes Label erfassen.

### R14G4-02 (P3): Stadionmaskentest übernimmt den Sollwert aus der Produktkonstante

`EveryPartIsDrawnFromItsTableEntryWithMask0xC0` ([StadiumPartsTests.cs:19](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/tests/EishockeyManager.UI.Tests/StadiumPartsTests.cs#L19)) leitet den erwarteten transparenten Pixel aus `StadiumScreen.PartMask` ab, derselben Konstante, die der Produktcode zum Zeichnen verwendet. Der Test prüft den im Namen und Beleg geforderten Wert `0xC0` damit nicht unabhängig.

**Gegenprobe S10:** `PartMask` in einer Prüfkopie auf `0x00` setzen. Der bestehende Test bleibt **grün**, obwohl der Produktcode dann andere Bildpunkte transparent behandelt. Eine zusätzliche Prüfung `Assert.Equal(0xC0, StadiumScreen.PartMask)` besteht am unveränderten Code und wird mit S10 **rot**. Die Originalinstruktion `0x3D92D` (`mov al, 0xC0`) wurde selbst nachgelesen. Der vorhandene Produktwert ist korrekt; der Befund betrifft die unabhängige Absicherung. Ein festes, aus der Originalstelle abgeleitetes Soll für die Maske und die Pixelprüfung würde die Lücke schließen.

### R14G4-03 (P3): Gesamtstand-Prüfung erkennt das Auslassen des Nachsaisonimports nicht

`Import_AllStands_PostseasonMatchesOriginal` ([OriginalImportPostseasonTests.cs:156](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/tests/EishockeyManager.Core.Tests/OriginalImportPostseasonTests.cs#L156)) prüft über alle Originalstände nur die Zahl der Play-off-Runden und Aufstiegsrunden sowie deren Terminzahl. Diese Größen können bereits beim vorherigen Nachspielen übereinstimmen, ohne dass die importierten Teilnehmer und Ergebnisse aus dem HOK stammen.

**Gegenprobe G05:** Den gesamten Aufruf `ApplyPostseason(save, clubOf)` in der Prüfkopie auslassen. Die Gesamtstand-Prüfung bleibt **grün**. Die konkreten Prüfungen für FILE22 und FILE44 zu Aufstiegsrunden, Play-offs und den folgenden Ligen werden dagegen jeweils **rot** (sechs Datenzeilen). Ein Vergleich der Teilnehmer und Ergebnisse pro Stand mit den HOK-Feldern würde die Aussage des Gesamtstandtests absichern. Im Produktcode ist der Importaufruf vorhanden; dies ist eine Testlücke.

### R14G4-04 (P3): Vier-Saison-Probe aller Originalstände akzeptiert vorzeitiges Spielende

`Import_AllStands_FourSeasonChanges` ([OriginalImportPostseasonTests.cs:194](https://github.com/kaaasi/eishockey-manager-redone/blob/b13046422b285404ad6ddbe7b5f1b18e22069eea/tests/EishockeyManager.Core.Tests/OriginalImportPostseasonTests.cs#L194)) akzeptiert am Ende für jeden Stand `Phase == GameOver`, ohne zu prüfen, ob die konfigurierte Spieldauer dieses Ende bereits rechtfertigt. Damit kann die angekündigte Mehrsaisonprüfung bei einem fehlerhaften vorzeitigen Ende bestehen.

**Gegenprobe G06:** `StartNextSeason` bei `SeasonOver` sofort auf `GameOver` setzen und ohne Saisonwechsel zurückkehren lassen. Der Test über alle eingelesenen Originalstände bleibt **grün**. Eine zusätzliche Kontrolle für FILE05 fordert `game.Season == start + 4`: Sie besteht am unveränderten Produktcode und wird mit G06 **rot**. Dies erweitert den verwandten Befund R14G3-02 auf einen anderen, alle Stände umfassenden Test; ein vorzeitiges Ende im unveränderten Produkt ist dadurch nicht nachgewiesen. Der Test sollte zulässiges Ende anhand der tatsächlichen Spieldauer unterscheiden und sonst vier erfolgte Wechsel fordern.

## 3. Prüfungen und offene Prüfbereiche

Testsaat-Rücksetzungen sind in der Regelspalte ausdrücklich als solche bezeichnet; sie sind keine Regelmutationen und zählen nicht als rote Gegenprobe in der Methodenliste. Die Prüfkopie liegt unter `/workspace/r14-review-teil4/copy`; Ausgangsstand ist der unveränderte lokale Head. Mutationen laufen einzeln mit Wiederherstellung, die vollständigen Ausgaben und Regeln werden unter `/workspace/r14-review-teil4/logs` gespeichert. Beim ersten abgebrochenen Versuch wurde eine überlagerte Mutation erkannt; diese Ergebnisse wurden verworfen und anschließend nach Wiederherstellung der Quelldatei erneut ausgeführt. .NET SDK 10.0.402, lokale Testhost-Sockets mit Umgebungsfreigabe, `NuGetAudit=false`.

Die unveränderten Kontrollläufe bestehen: `OriginalSaveHeaderTests` 4/4, `SaveLoadDialogOriginalTests` 16/16 und `NationalteamRunde14Teil4Tests` 18/18.

### Selbst nachgelesene Originalstellen

`em.exe` Datei `0x5B68E–0x5B732`: Der Code übergibt nacheinander die Längen 10, 4, 1, 1 und `0xA0` an denselben Leser; nach den ersten 10 Bytes erfolgt ein Vergleich gegen eine Zeichenkette bei DGROUP `0x55DF`. Damit ist die Struktur des Kopflesers im Originalcode nachvollzogen; die Bedeutung jeder Hilfsfunktion und das Verhalten bei verkürzter Datei bleiben gesondert offen. Selbst aus `FILE46.HOK` gelesen: Kennung `KRON_0.99` mit Nullbyte, Jahr 1994, Managerzahl 1, Spieltag 1, erster Eintrag Liga 0, WM-Flag 0, Name `Joerg` und Vereinsbytes `KLNER EC` (Originalkodierung).

Selbst in `em.exe` nachgelesen: `0x319AC–0x319DB` kopiert in einer Schleife die Plätze `i` und `7 − i` von `0x5CCB` nach `0x5D3B + 2i` und `0x5D3C + 2i`, für vier Werte von `i`. Das bestätigt die Viertelfinalfolge 1–8, 2–7, 3–6, 4–5. Die anschließende Halbfinaltabelle wurde bereits in Teil 2 unabhängig gelesen. S01 setzt die alte Paarungsregel zurück; beide Originalstände FILE44/FILE22 und der Mehrsaisontest werden rot.

Zur Nutzerentscheidung C wurde `em.exe` Datei `0x54D41–0x54D65` erneut gelesen: Der Tagesindex adressiert ein Tagesbyte, Wert 9 wird auf 0 abgebildet, 0 und 255 springen nach `0x55073`. `0x55204–0x55230` erhöht DGROUP `0x3B7` mit Übertrag und ruft nach Setzen von Manager 0 zwei Funktionen je Manager auf. Die Verknüpfung des gesamten Ladewegs und die Zuordnung aller aufgerufenen Funktionen bleiben offen; isolierte Befehle belegen sie nicht allein.

Stadionteile: In `em.exe` Datei `0x3D92D` steht `mov al, 0xC0`, gefolgt von `push ax` bei `0x3D92F`. Der übergebene Maskenwert des niederwertigen Bytes ist damit unmittelbar nachgelesen; die vollständige Kopierroutine und alle Stadionfundstellen sind damit noch nicht geprüft.

Zu L1 selbst in `em.exe` gelesen: `0x29CFA–0x29D7F` prüft `[bp−0x64] = 0`, DGROUP `0x5A14 = 0x7CA` (1994) und Managerzahl `0x386 = 1`, schreibt 1 in das managerindizierte Feld und wiederholt einen Zufallsaufruf bis zur akzeptierten Rückgabe des Stubs `0x110:0x75`. Die Funktion des Stubs und die nachfolgende Tauschfunktion sind durch diese Stelle allein noch nicht zugeordnet; der vollständige Filterbeleg bleibt offen.

### Modus-4-Referenzwert: einzelne geänderte Bestandteile

Die Tabelle vergleicht jedes seit Base geänderte Feld mit den vollständigen tatsächlichen Ausgaben einzelner Regelrückmutationen V01–V09. Ein abweichender Wert belegt die Abhängigkeit von der jeweils genannten Regel unter Head-Bedingungen; er beweist keine alleinige Ursache der Änderung. Die Konto-Vorbedingung des Tests wurde hierbei nicht zurückgesetzt. `offen` bedeutet, dass keine dieser Mutationen den Wert verändert hat.

| Bestandteil | Base | Head | Abweichender Wert unter Rückmutation |
|---|---:|---:|---|
| Saison 1 WorldChampion | 52 | 54 | V08: 52; V09: 52 |
| Saison 1 CupWinner | 11 | 12 | V04: 0; V05: 15; V06: 16; V07: 0; V08: 10 |
| Saison 1 GermanChampion | 1 | 0 | V02: 1; V05: 4; V06: 3; V08: 1 |
| Saison 1 Konto 0 | 96040 | 1100000 | V04: 1168751; V08: 1200000 |
| Saison 2 WorldChampion | 52 | 57 | V02: 52; V03: 52; V04: 63; V05: 52; V06: 53; V07: 52; V09: 52 |
| Saison 2 EuropeanCupWinner | 1 | 0 | V02: 1; V05: 4; V06: 3; V07: 2; V08: 1 |
| Saison 2 Konto 0 | 1402820 | 2429195 | V01: 2443220; V02: 2500446; V03: 2267967; V04: 2273847; V05: 2338465; V06: 2382267; V07: 2357615; V08: 2120952; V09: 2343530 |
| Saison 3 WorldChampion | 52 | 63 | V02: 52; V03: 54; V04: 52; V06: 52; V07: 52; V09: 53 |
| Saison 3 CupWinner | 3 | 0 | V02: 4; V03: 1; V04: 16; V05: 15; V06: 16; V08: 5; V09: 11 |
| Saison 3 EuropeanCupWinner | 1 | 0 | V02: 11; V03: 1; V04: 1; V05: 4; V06: 3; V08: 11 |
| Saison 3 Konto 0 | 2154396 | 2301516 | V01: 2151523; V02: 2564297; V03: 2107624; V04: 2153793; V05: 2412715; V06: 2397123; V07: 3047963; V08: 1995725; V09: 2747853 |
| Saison 4 WorldChampion | 54 | 52 | V02: 54; V05: 54; V06: 54; V07: 53; V08: 55 |
| Saison 4 CupWinner | 3 | 0 | V02: 4; V03: 18; V04: 14; V05: 7; V06: 11; V07: 1; V08: 29; V09: 18 |
| Saison 4 GermanChampion | 13 | 0 | V02: 12; V03: 1; V04: 4; V05: 7; V06: 9; V08: 5; V09: 9 |
| Saison 4 Konto 0 | 2718434 | 1969543 | V01: 1680547; V02: 2609090; V03: 1975466; V04: 2029069; V05: 2275209; V06: 2335622; V07: 2787899; V08: 500000; V09: 2658866 |
| Saison 4 Konto 1 | 400000 | 500000 | V02: 400000; V04: 400000; V05: 400000; V06: 400000; V07: 400000; V08: 400000; V09: 400000 |
| Saison 5 WorldChampion | 54 | 52 | V02: 63; V04: 54; V05: 63; V06: 53; V07: 54; V08: 53; V09: 63 |
| Saison 5 CupWinner | 0 | 18 | V02: 4; V03: 10; V04: 8; V05: 1; V06: 0; V07: 1; V09: 0 |
| Saison 5 GermanChampion | 9 | 4 | V02: 0; V03: 15; V04: 8; V05: 15; V06: 0; V07: 12; V08: 10; V09: 11 |
| Saison 5 EuropeanCupWinner | 13 | 0 | V02: 11; V03: 1; V04: 4; V05: 7; V06: 9; V08: 5; V09: 11 |
| Saison 5 Konto 0 | 2952676 | 1462802 | V01: 1033390; V02: 2102474; V03: 1760179; V04: 400000; V05: 1724952; V06: 400000; V07: 2714316; V08: 400000; V09: 2161046 |
| Saison 5 Konto 1 | -7124085 | 400000 | V02: 500000; V03: 500000; V04: 500000; V05: 500000; V06: 500000 |
| Zufallszustand R | 677985496 | 1179893277 | V02: 1558375281; V03: 218696157; V04: 1047374504; V05: 1140664819; V06: 499199781; V07: 1530654188; V08: 1146779949; V09: 2063705941 |
| Zufallszustand C | 1798453752 | 1809965060 | V02: 893101215; V03: 836845586; V04: 406943591; V05: 1372752275; V06: 1028139438; V07: 1237843630; V08: 167583934; V09: 1067210724 |

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

### Geänderte Saaten: erste Regelgegenprobe

T01 setzt den zentralen Tageszähler von `day + 1` auf `day` zurück. Jede Zeile nennt die Saatänderung seit Base und das tatsächlich beobachtete Ergebnis. Grün belegt hier keine Ursache; weitere gezielte Rückmutationen und weitere Saatänderungen bleiben offen. Bei rot ist erst die Abhängigkeit des Tests unter den aktuellen übrigen Regeln belegt, nicht allein die Notwendigkeit dieser Saatänderung.

| Test | Saat Base → Head | T01 |
|---|---|---|
| `Mode4_TwoManagersInTopFive_HigherPlacedBecomesCoach` | 17 → 71 | rot |
| `Mode4_NoManagerInTopFive_NoCoach` | 4 → 3 | grün |
| `Mode4_Appointment_BeforeReroll_GermanyKeepsBaseRatings` | 2 → 22 | grün |
| `Full_ManagerInTopFive_GetsStatusBit_OtherNot` | 4 → 3 | grün |
| `Full_EveryManagerInTopFive_GetsStatusBit` | 17 → 71 | rot |
| `StatusBit_DoesNotChangeStoredEventsOrPremium` | 3 → 5 | grün |
| `SaveLoad_StatusBitIsDerivedFromSavedData` | 4 → 3 | grün |
| `FourSeasonChanges_ManagedMatchesPlausible` | 3 → 4 | beide Aktivvarianten grün |
| `OtherClubLeads_NoSinglePremium` | 4 → 13 | grün |

### Hauptmenü: Codepfade

| Ereignis | Geprüfter Anschluss | Rest |
|---|---|---|
| Nachrichtenflag | `MainMenuScreen.Enter → BuildMenu → GameState.OnMainMenu → Manager.TakeNewMessage`; der Kern löscht das Flag und liefert seinen alten Wert an den Bildschirm. | Jeden Aufbau- und Ladeweg sowie die Wirkungszahl einzeln gegenprüfen. |
| Vereinsaufstellung | `OnMainMenu` ruft `Club.ApplyAutoLineup` auf; `BeginManagerTurn` ruft die Funktion ebenfalls beim Zugwechsel auf. | Semantik und Wirkungszahl bei Rückkehr, Laden und Hotseat einzeln prüfen. |
| Bundestrainer | `OnMainMenu` gibt `isReturn` an `OnNationalCoachMenu` weiter; `BeginManagerTurn` ruft `BeginNationalCoachTurn`. | Alle Zustands- und Aufrufvarianten offen. |
| Gehaltsfaktor | `BeginFirstTurn` und `BeginManagerTurn` rufen `DrawContractTurnFactor` im Kern auf; `OnMainMenu` selbst zieht ihn nicht. | Alle Einstiegspfade und RNG-Wirkungen offen. |
| Automatisches Speichern | `MainMenuScreen.Enter` ruft nach `BuildMenu` `AutoSave.OnMainMenu`; diese Funktion wurde in dieser Teilprüfung nicht weiter verfolgt. | Datei- und Zustandseffekte sowie doppelte Aufrufe offen. |

Diese statische Zuordnung ergänzt die zwei dynamischen Sonderfallprüfungen und ist keine vollständige Ereignisprüfung.

### Entscheidungen: ergänzende Prüfung

| Entscheidung | Geprüfter Code und Status |
|---|---|
| R14-D2 = B, HOK-Import jetzt, Export später | `SaveLoadScreen` ruft `OriginalImport.TryImport` beim Laden auf; HOK-Einträge werden beim Speichern grau angezeigt (`SaveLoadScreen.cs:329,453,472`). Feldkarte und vollständige Ausschlussprüfung sämtlicher Exportpfade bleiben offen. |
| L1, U15-c | `NewGame.OneSeasonSingleManager` und die anschließende Umplatzierung setzen bei einem Manager und einer Saison den Verein auf Platz 0; `PlaceInStartLeague` schließt Platz 0 aus. Die vollständige Prüfung aller Auswahlpfade und des Originalfilters bleibt offen. |
| L2, Schemafelder im Kader | `Player.Competitions.cs` führt die Wettbewerbszähler, `FinanceSponsorRating.cs` das Tabellenfeld und `Transfer.Listed.cs` eigene Marktspieler; `SaveGame.Migration.KaderStart2.cs` ergänzt alte Stände. B03–B09 und C01–C06 prüfen einzelne Wirkungen; vollständige Feld- und Migrationsprüfung offen. |
| L3, Vereinsaufstellung und Reihenfolge | `Squad.AutoLineup.cs` und `Squad.Lineup.cs` führen die Regeln im Kern, `LineupScreen.cs` die Bildschirmsteuerung. B01/B02 prüfen automatische und manuelle Neuaufstellung, K04 die Sortierung eines Talents; die vollständige Prüfung aller Bedien- und Platzierungsfälle bleibt offen. |
| L4, Taktik 375h im Kern | `Club.ApplyAutoLineup` prüft die Einstellung; `PlayerInjuries.AfterMatch` löst sie nach dem Spiel aus. B01 und B02 machen die automatischen und manuellen Fälle jeweils rot. Alle anderen Aufrufer und Zustände bleiben offen. |
| L8, A009 im Nationalteam | `NationalCoach.BeginTurn`, `OnMenu`, `EndTurn` und `OnEventDay` liegen im Kern; Import übernimmt das gespeicherte Byte in `OriginalImport.National.cs`. Die vollständige Prüfung aller Zug- und Migrationspfade bleibt offen. |
| L9, A362 als Abbild | `ContractsScreen` hält `_picture` und `_pictureManager` als private Bildschirmfelder und baut die Saisonende-Verhandlungen über `ForSeasonEnd` auf. Vollständiger Bild- und Ablaufvergleich sowie die Schemaabgrenzung bleiben offen. |
| L10, A115 nach Code | `Player.Contract.cs` unterscheidet Leihrückkehr zu Computer- und Managervereinen; bei Rückkehr zum Manager bleiben die Leihvertragsfelder erhalten. Alle Originalzweige und die Kopierreihenfolge sind noch nicht vollständig nachgelesen. |
| E14-6 = A, zusätzlicher Lauf Speicherdialog | `docs/arbeitspakete.md:174` ordnet A338/A339 und den zusätzlichen Lauf `hok-import` Teil 5 zu; der Dialogcode ist vorhanden, und 16/16 Kontrollfälle bestehen. Vollständiger Original- und Ereignisabgleich bleibt offen. |

Die Entscheidungsliste und die übrigen Einzelergebnisse aus Teil 1–3 gelten weiter; nicht aufgeführte Entscheidungen sind damit nicht als abgeschlossen bewertet.

### Ergebnisse je Mutation und Theoriezeile

| Mutation | Regeländerung | Ausgeführter Test beziehungsweise Datenzeile | Ergebnis |
|---|---|---|---|
| A01 | HOK-Endung nicht erkennen | `EishockeyManager.UI.Tests.HokImportAccessTests.List_MarksHokFilesAsOriginal` | rot |
| A02 | fremdes Format als Prüfsummenfehler melden | `EishockeyManager.UI.Tests.HokImportAccessTests.TryImport_RejectsNonOriginalData_WithoutChecksumFlag` | rot |
| A03 | getippten HOK nicht laden | `EishockeyManager.UI.Tests.HokImportAccessTests.LoadDialog_ImportsHok_ByTypedName` | rot |
| A04 | wiederholten Klick ignorieren | `EishockeyManager.UI.Tests.HokImportAccessTests.LoadDialog_ImportsHok_ByClickTwice` | rot |
| A05 | auch bei gleichem Namen HOK bevorzugen | `EishockeyManager.UI.Tests.HokImportAccessTests.SameName_TypedLoadsRemakeSave_ClickOnHokImports` | rot |
| A06 | angeklickten Eintrag beim Ausführen vergessen | `EishockeyManager.UI.Tests.HokImportAccessTests.SameName_TypedThenSingleClickOnHok_Imports` | rot |
| A07 | Prüfsummenfehler nicht als Mogelguru anzeigen | `EishockeyManager.UI.Tests.HokImportAccessTests.ChecksumError_ShowsMogelguru_AndKeepsRunningGame` | rot |
| A08 | Prüfsummenfehler nicht kennzeichnen | `EishockeyManager.UI.Tests.HokImportAccessTests.TryImport_FlagsChecksumError_AndImportsValidSave` | rot |
| A09 | HOK beim Speichern auswählbar machen | `EishockeyManager.UI.Tests.HokImportAccessTests.SaveDialog_DoesNotSelectHok` | rot |
| B01 | Neuaufstellung nur bei Verletzung | `EishockeyManager.Core.Tests.KaderStartRunde14Teil2bTests.AutoLineup_AfterMatchWithoutInjury_LinesUpAgain` | rot |
| B02 | immer nach dem Spiel auffüllen | `EishockeyManager.Core.Tests.KaderStartRunde14Teil2bTests.ManualLineup_AfterMatchWithoutInjury_Unchanged` | rot |
| B03 | DEB-Pokal zur Liga zählen | `EishockeyManager.Core.Tests.KaderStartRunde14Teil2bTests.CupMatch_CountsInCupFields` | rot |
| B04 | Europapokal zur Liga zählen | `EishockeyManager.Core.Tests.KaderStartRunde14Teil2bTests.EuropeanCupMatch_CountsInEuropeFields` | rot |
| B05 | Restwettbewerbe als Pokal zählen | `EishockeyManager.Core.Tests.KaderStartRunde14Teil2bTests.Postseason_CountsAsLeague(competition: Relegation)` | rot |
| B05 | Restwettbewerbe als Pokal zählen | `EishockeyManager.Core.Tests.KaderStartRunde14Teil2bTests.Postseason_CountsAsLeague(competition: Playoff)` | rot |
| B06 | Vereinszähler beim Wechsel behalten | `EishockeyManager.Core.Tests.KaderStartRunde14Teil2bTests.Join_ResetsClubCountersOnly` | rot |
| B07 | Starttabellenplatz auf eins setzen | `EishockeyManager.Core.Tests.KaderStartRunde14Teil2bTests.FirstMatchDay_NewGame_UsesRankZero` | rot |
| B08 | Tabellenplatz bei Speicherung um eins verschieben | `EishockeyManager.Core.Tests.KaderStartRunde14Teil2bTests.MultiSeason_TableRankAndCompetitionCounters(active: True)` | rot |
| B08 | Tabellenplatz bei Speicherung um eins verschieben | `EishockeyManager.Core.Tests.KaderStartRunde14Teil2bTests.MultiSeason_TableRankAndCompetitionCounters(active: False)` | rot |
| B09 | migrierten Tabellenplatz um eins verschieben | `EishockeyManager.Core.Tests.KaderStartRunde14Teil2bTests.Migration_OldSave_LeagueOnlyAndTableRankFromRankSlots` | rot |
| C01 | angebotenen Spieler im Kader lassen | `EishockeyManager.Core.Tests.KaderStartTeil2cTests.Offer_PlayerLeavesSquad_SlotsMoveUp_ListingWithSeller` | rot |
| C02 | Rückkehrer hinten einordnen | `EishockeyManager.Core.Tests.KaderStartTeil2cTests.Withdraw_ReturnsPlayerByRole_NotInLineup_ContractUnchanged` | rot |
| C03 | Kadergrenze beim Rückzug erhöhen | `EishockeyManager.Core.Tests.KaderStartTeil2cTests.Withdraw_FullSquad_StaysOnMarket` | rot |
| C04 | Gehalt gelisteter Spieler auslassen | `EishockeyManager.Core.Tests.KaderStartTeil2cTests.ListedPlayer_KeepsCostingSalary` | rot |
| C05 | Verkaufszahlung um eins erhöhen | `EishockeyManager.Core.Tests.KaderStartTeil2cTests.SoldListedPlayer_GoesToBuyer_SellerPaid` | rot |
| C06 | Rückkehrer am Saisonwechsel nicht einsortieren | `EishockeyManager.Core.Tests.KaderStartTeil2cTests.FourSeasonChanges_ListedPlayersReturn_NoDuplicates(active: False)` | grün |
| C06 | Rückkehrer am Saisonwechsel nicht einsortieren | `EishockeyManager.Core.Tests.KaderStartTeil2cTests.FourSeasonChanges_ListedPlayersReturn_NoDuplicates(active: True)` | rot |
| C06 | Rückkehrer am Saisonwechsel nicht einsortieren | `EishockeyManager.Core.Tests.KaderStartTeil2cTests.SeasonChange_UnsoldListedPlayerReturnsByRole_NotInLineup_ContractCountedDown` | rot |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 3)` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 7)` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 5)` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 41)` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds` | rot |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_LoadQuirkBooksAprilMonthEnd_LikeFile46_2` | rot |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.DeepDebts_NoDismissal_GameContinues` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: False)` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_SeasonChange_KeepsListLikeFile46` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SeasonChange_KeepsMarketList_PlayersAgeOneYear` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` | rot |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TwoManagers_NegotiationLockCountsOncePerTick` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File24_ToMenuOfDay17_MarketDecayedOnceLikeFile25` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_CountersFromSeasonEndToNinthOfSeptember` | rot |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TrendControlCountsDownPerTick` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: True)` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 11)` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 3)` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.TrainingDays_AreDayCounterTicks` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.CatchUpManagerDay_TwoManagers_SecondNeitherExpiresNorBidsOnForeignListings` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.Construction_AdvancesOnCounterZeroDays` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.ListChangeDays_OnlyEventDaysOfMainPath` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: False)` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: True)` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_AdvancesConstructionAndHealsSquad` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.AdvanceTrend_ClampsAndRerollsAtZero` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_GastronomyOrderedOnFourteenthOfApril_Loses21WeeksUntilNewSeason` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_ManagerZeroDismissed_MarketStillDecaysPerTick` | grün |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_DayClose_FourMonthEndsBooked` | rot |
| D01 | Sommerpause einen Tag zu früh beginnen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TickOfHaltDay_RunsBeforeHalt` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 3)` | rot |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 7)` | rot |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 5)` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 41)` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_LoadQuirkBooksAprilMonthEnd_LikeFile46_2` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: False)` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.DeepDebts_NoDismissal_GameContinues` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_SeasonChange_KeepsListLikeFile46` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SeasonChange_KeepsMarketList_PlayersAgeOneYear` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TwoManagers_NegotiationLockCountsOncePerTick` | rot |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File24_ToMenuOfDay17_MarketDecayedOnceLikeFile25` | rot |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_CountersFromSeasonEndToNinthOfSeptember` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TrendControlCountsDownPerTick` | rot |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: True)` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 11)` | rot |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 3)` | rot |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.TrainingDays_AreDayCounterTicks` | rot |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero` | rot |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.CatchUpManagerDay_TwoManagers_SecondNeitherExpiresNorBidsOnForeignListings` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.Construction_AdvancesOnCounterZeroDays` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.ListChangeDays_OnlyEventDaysOfMainPath` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: False)` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: True)` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_AdvancesConstructionAndHealsSquad` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.AdvanceTrend_ClampsAndRerollsAtZero` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_GastronomyOrderedOnFourteenthOfApril_Loses21WeeksUntilNewSeason` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_ManagerZeroDismissed_MarketStillDecaysPerTick` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_DayClose_FourMonthEndsBooked` | grün |
| D02 | Hauptwegtakt nach Tag statt Zähler | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TickOfHaltDay_RunsBeforeHalt` | rot |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 3)` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 7)` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_SeasonChange_KeepsListLikeFile46` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SeasonChange_KeepsMarketList_PlayersAgeOneYear` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` | rot |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TwoManagers_NegotiationLockCountsOncePerTick` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File24_ToMenuOfDay17_MarketDecayedOnceLikeFile25` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_CountersFromSeasonEndToNinthOfSeptember` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TrendControlCountsDownPerTick` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: False)` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: False)` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: True)` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 11)` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 3)` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.TrainingDays_AreDayCounterTicks` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.CatchUpManagerDay_TwoManagers_SecondNeitherExpiresNorBidsOnForeignListings` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.Construction_AdvancesOnCounterZeroDays` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.ListChangeDays_OnlyEventDaysOfMainPath` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: True)` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_AdvancesConstructionAndHealsSquad` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.AdvanceTrend_ClampsAndRerollsAtZero` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_GastronomyOrderedOnFourteenthOfApril_Loses21WeeksUntilNewSeason` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_ManagerZeroDismissed_MarketStillDecaysPerTick` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 5)` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_DayClose_FourMonthEndsBooked` | rot |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TickOfHaltDay_RunsBeforeHalt` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 41)` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_LoadQuirkBooksAprilMonthEnd_LikeFile46_2` | rot |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.DeepDebts_NoDismissal_GameContinues` | grün |
| D03 | Tagesabschluss im Sommer auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_SeasonChange_KeepsListLikeFile46` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SeasonChange_KeepsMarketList_PlayersAgeOneYear` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 5)` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TwoManagers_NegotiationLockCountsOncePerTick` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File24_ToMenuOfDay17_MarketDecayedOnceLikeFile25` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_CountersFromSeasonEndToNinthOfSeptember` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TrendControlCountsDownPerTick` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: False)` | rot |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 41)` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_LoadQuirkBooksAprilMonthEnd_LikeFile46_2` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.DeepDebts_NoDismissal_GameContinues` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: True)` | rot |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 3)` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_AdvancesConstructionAndHealsSquad` | rot |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.AdvanceTrend_ClampsAndRerollsAtZero` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 7)` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_GastronomyOrderedOnFourteenthOfApril_Loses21WeeksUntilNewSeason` | rot |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_ManagerZeroDismissed_MarketStillDecaysPerTick` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_DayClose_FourMonthEndsBooked` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TickOfHaltDay_RunsBeforeHalt` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: False)` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: True)` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 11)` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 3)` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.TrainingDays_AreDayCounterTicks` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.CatchUpManagerDay_TwoManagers_SecondNeitherExpiresNorBidsOnForeignListings` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.Construction_AdvancesOnCounterZeroDays` | grün |
| D04 | Sommerbau auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.ListChangeDays_OnlyEventDaysOfMainPath` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 3)` | rot |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 7)` | rot |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_SeasonChange_KeepsListLikeFile46` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SeasonChange_KeepsMarketList_PlayersAgeOneYear` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TwoManagers_NegotiationLockCountsOncePerTick` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File24_ToMenuOfDay17_MarketDecayedOnceLikeFile25` | rot |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_CountersFromSeasonEndToNinthOfSeptember` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TrendControlCountsDownPerTick` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: False)` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: False)` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: True)` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 11)` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 3)` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.TrainingDays_AreDayCounterTicks` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.CatchUpManagerDay_TwoManagers_SecondNeitherExpiresNorBidsOnForeignListings` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.Construction_AdvancesOnCounterZeroDays` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.ListChangeDays_OnlyEventDaysOfMainPath` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: True)` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 5)` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_AdvancesConstructionAndHealsSquad` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.AdvanceTrend_ClampsAndRerollsAtZero` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_GastronomyOrderedOnFourteenthOfApril_Loses21WeeksUntilNewSeason` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_ManagerZeroDismissed_MarketStillDecaysPerTick` | rot |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_DayClose_FourMonthEndsBooked` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TickOfHaltDay_RunsBeforeHalt` | rot |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 41)` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_LoadQuirkBooksAprilMonthEnd_LikeFile46_2` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.DeepDebts_NoDismissal_GameContinues` | grün |
| D05 | Marktbelastung nicht senken | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_SeasonChange_KeepsListLikeFile46` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SeasonChange_KeepsMarketList_PlayersAgeOneYear` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TwoManagers_NegotiationLockCountsOncePerTick` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 5)` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File24_ToMenuOfDay17_MarketDecayedOnceLikeFile25` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_CountersFromSeasonEndToNinthOfSeptember` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TrendControlCountsDownPerTick` | rot |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 41)` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_LoadQuirkBooksAprilMonthEnd_LikeFile46_2` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.DeepDebts_NoDismissal_GameContinues` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 3)` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 7)` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: False)` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: False)` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: True)` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_AdvancesConstructionAndHealsSquad` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.AdvanceTrend_ClampsAndRerollsAtZero` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_GastronomyOrderedOnFourteenthOfApril_Loses21WeeksUntilNewSeason` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_ManagerZeroDismissed_MarketStillDecaysPerTick` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_DayClose_FourMonthEndsBooked` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TickOfHaltDay_RunsBeforeHalt` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: True)` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 11)` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 3)` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.TrainingDays_AreDayCounterTicks` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.CatchUpManagerDay_TwoManagers_SecondNeitherExpiresNorBidsOnForeignListings` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.Construction_AdvancesOnCounterZeroDays` | grün |
| D06 | Tendenzfortschreibung auslassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.ListChangeDays_OnlyEventDaysOfMainPath` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 3)` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 7)` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_SeasonChange_KeepsListLikeFile46` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SeasonChange_KeepsMarketList_PlayersAgeOneYear` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TwoManagers_NegotiationLockCountsOncePerTick` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File24_ToMenuOfDay17_MarketDecayedOnceLikeFile25` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_CountersFromSeasonEndToNinthOfSeptember` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TrendControlCountsDownPerTick` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: False)` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: False)` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: True)` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 11)` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 3)` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.TrainingDays_AreDayCounterTicks` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.CatchUpManagerDay_TwoManagers_SecondNeitherExpiresNorBidsOnForeignListings` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.Construction_AdvancesOnCounterZeroDays` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.ListChangeDays_OnlyEventDaysOfMainPath` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: True)` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_AdvancesConstructionAndHealsSquad` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.AdvanceTrend_ClampsAndRerollsAtZero` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_GastronomyOrderedOnFourteenthOfApril_Loses21WeeksUntilNewSeason` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_ManagerZeroDismissed_MarketStillDecaysPerTick` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 5)` | rot |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_DayClose_FourMonthEndsBooked` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TickOfHaltDay_RunsBeforeHalt` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 41)` | rot |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_LoadQuirkBooksAprilMonthEnd_LikeFile46_2` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.DeepDebts_NoDismissal_GameContinues` | grün |
| D07 | Schwellwert für Zwangsabstieg unerreichbar setzen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 5)` | rot |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_SeasonChange_KeepsListLikeFile46` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead(seed: 41)` | rot |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SeasonChange_KeepsMarketList_PlayersAgeOneYear` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_LoadQuirkBooksAprilMonthEnd_LikeFile46_2` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.DeepDebts_NoDismissal_GameContinues` | rot |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TwoManagers_NegotiationLockCountsOncePerTick` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File24_ToMenuOfDay17_MarketDecayedOnceLikeFile25` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_CountersFromSeasonEndToNinthOfSeptember` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TrendControlCountsDownPerTick` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktSommerpauseReviewTests.File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 3)` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick(seed: 7)` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: False)` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: False)` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks(active: True)` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak(active: True)` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 11)` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks(seed: 3)` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.TrainingDays_AreDayCounterTicks` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_AdvancesConstructionAndHealsSquad` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.AdvanceTrend_ClampsAndRerollsAtZero` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.File44_GastronomyOrderedOnFourteenthOfApril_Loses21WeeksUntilNewSeason` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_ManagerZeroDismissed_MarketStillDecaysPerTick` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.SummerBreak_DayClose_FourMonthEndsBooked` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.CatchUpManagerDay_TwoManagers_SecondNeitherExpiresNorBidsOnForeignListings` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegReviewTests.MainPath_TickOfHaltDay_RunsBeforeHalt` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.Construction_AdvancesOnCounterZeroDays` | grün |
| D08 | Manager bei hohen Schulden entlassen | `EishockeyManager.Core.Tests.TagestaktHauptwegTests.ListChangeDays_OnlyEventDaysOfMainPath` | grün |
| E01 | auch bei ungleichem Index Markierung annehmen | `EishockeyManager.Core.Tests.LoadQuirkTests.OnlyMarkerDaySaves_GetTheOffset` | rot |
| E02 | zweiten Tagesabschluss nach Laden auslassen | `EishockeyManager.Core.Tests.LoadQuirkTests.File44_AfterImport_SecondDayChange_FifteenthOfApril` | rot |
| E03 | Ladeversatz am Saisonende nicht löschen | `EishockeyManager.Core.Tests.LoadQuirkTests.File44_Offset_SurvivesSave_EndsAtSeasonChange_FourSeasons(active: False)` | rot |
| E03 | Ladeversatz am Saisonende nicht löschen | `EishockeyManager.Core.Tests.LoadQuirkTests.File44_Offset_SurvivesSave_EndsAtSeasonChange_FourSeasons(active: True)` | rot |
| E04 | Ladeversatz beim Neuspiel auf eins setzen | `EishockeyManager.Core.Tests.LoadQuirkTests.NewGame_HasNoOffset` | rot |
| E05 | Ladeversatz bei Migration auf eins setzen | `EishockeyManager.Core.Tests.LoadQuirkTests.Schema46_WithoutOffset_LoadsWithZero` | rot |
| E06 | Manager-Tagesmerker unterdrücken | `EishockeyManager.Core.Tests.SaisonManagerTagTests.MultiSeason_FourSeasonChanges_BothSettings(active: False, option: True)` | grün |
| E06 | Manager-Tagesmerker unterdrücken | `EishockeyManager.Core.Tests.SaisonManagerTagTests.MultiSeason_FourSeasonChanges_BothSettings(active: True, option: True)` | grün |
| E06 | Manager-Tagesmerker unterdrücken | `EishockeyManager.Core.Tests.SaisonManagerTagTests.MultiSeason_FourSeasonChanges_BothSettings(active: True, option: False)` | grün |
| E06 | Manager-Tagesmerker unterdrücken | `EishockeyManager.Core.Tests.SaisonManagerTagTests.MultiSeason_FourSeasonChanges_BothSettings(active: False, option: False)` | grün |
| E06 | Manager-Tagesmerker unterdrücken | `EishockeyManager.Core.Tests.SaisonManagerTagTests.ManagerPlaysCupDay_OptionOn_AllPairingsUseMinuteLoop_RandomDiffers` | rot |
| E07 | Manager-Tagesmerker ohne Paarungsprüfung setzen | `EishockeyManager.Core.Tests.SaisonManagerTagTests.NoManagerInvolved_OptionDoesNothing_SameRandom` | rot |
| E08 | Option standardmäßig aktivieren | `EishockeyManager.Core.Tests.SaisonManagerTagTests.Default_IsOff_AndSaveRoundTripKeepsIt` | rot |
| E09 | Option bei Migration aktivieren | `EishockeyManager.Core.Tests.SaisonManagerTagTests.Schema39Save_Migrates_OptionOff` | rot |
| F01 | Feldkartenversion auf 46 setzen | `EishockeyManager.Core.Tests.OriginalImportFieldMapTests.FieldMap_NamesSchemaVersion` | rot |
| F02 | ein Schemafeld aus der Karte entfernen | `EishockeyManager.Core.Tests.OriginalImportFieldMapTests.FieldMap_CoversEverySchemaFieldExactlyOnce` | rot |
| F03 | ungültigen Feldkartenstatus setzen | `EishockeyManager.Core.Tests.OriginalImportFieldMapTests.FieldMap_RowsHaveValidStatusAndSource` | rot |
| F04 | nicht gelesenen Feldversatz als Quelle behaupten | `EishockeyManager.Core.Tests.OriginalImportFieldMapTests.FieldMap_ImportedRows_HaveTheirBlocksAndOffsetsInTheImportCode` | rot |
| F05 | Zeilenkommentare in der Testhilfe nicht entfernen | `EishockeyManager.Core.Tests.OriginalImportFieldMapTests.StripComments_RemovesLineAndBlockCommentsButKeepsStrings` | rot |
| F06 | Vereinsnamen im eingelesenen HOK-Objekt verfälschen | `EishockeyManager.Core.Tests.OriginalImportFieldMapDataTests.ClubRecords_MapToDatabaseByName(file: "FILE11.HOK", onlyWorld: True)` | rot |
| F06 | Vereinsnamen im eingelesenen HOK-Objekt verfälschen | `EishockeyManager.Core.Tests.OriginalImportFieldMapDataTests.ClubRecords_MapToDatabaseByName(file: "FILE29.HOK", onlyWorld: False)` | rot |
| F06 | Vereinsnamen im eingelesenen HOK-Objekt verfälschen | `EishockeyManager.Core.Tests.OriginalImportFieldMapDataTests.ClubRecords_MapToDatabaseByName(file: "FILE16A.HOK", onlyWorld: False)` | rot |
| F06 | Vereinsnamen im eingelesenen HOK-Objekt verfälschen | `EishockeyManager.Core.Tests.OriginalImportFieldMapDataTests.ClubRecords_MapToDatabaseByName(file: "FILE05.HOK", onlyWorld: False)` | rot |
| F07 | Tabellenplätze im eingelesenen HOK-Objekt verdoppeln | `EishockeyManager.Core.Tests.OriginalImportFieldMapDataTests.LeagueTable_UsesSlotsOfItsLeague(file: "FILE05.HOK")` | rot |
| F07 | Tabellenplätze im eingelesenen HOK-Objekt verdoppeln | `EishockeyManager.Core.Tests.OriginalImportFieldMapDataTests.LeagueTable_UsesSlotsOfItsLeague(file: "FILE29.HOK")` | rot |
| F07 | Tabellenplätze im eingelesenen HOK-Objekt verdoppeln | `EishockeyManager.Core.Tests.OriginalImportFieldMapDataTests.LeagueTable_UsesSlotsOfItsLeague(file: "FILE16A.HOK")` | rot |
| F08 | Konto im eingelesenen HOK-Objekt um ein Bit verändern | `EishockeyManager.Core.Tests.OriginalImportFieldMapDataTests.ManagerRecordOffsets_MatchDocumentedValues(file: "FILE08.HOK", account: 4315062, bankLoan: -2103000, ticket: 17, seats: 3000, standing: 4500)` | rot |
| F08 | Konto im eingelesenen HOK-Objekt um ein Bit verändern | `EishockeyManager.Core.Tests.OriginalImportFieldMapDataTests.ManagerRecordOffsets_MatchDocumentedValues(file: "FILE09.HOK", account: 1200000, bankLoan: 0, ticket: 13, seats: 4200, standing: 7000)` | rot |
| F08 | Konto im eingelesenen HOK-Objekt um ein Bit verändern | `EishockeyManager.Core.Tests.OriginalImportFieldMapDataTests.ManagerRecordOffsets_MatchDocumentedValues(file: "FILE01.HOK", account: 1500000, bankLoan: 0, ticket: 13, seats: 3000, standing: 4500)` | rot |
| G01 | DEB-Pokal nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupFinalWithMarks` | rot |
| G01 | DEB-Pokal nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_EuropeanCupFromOriginal` | grün |
| G01 | DEB-Pokal nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupQuarterFinal` | rot |
| G01 | DEB-Pokal nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupContinues` | grün |
| G01 | DEB-Pokal nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_WorldCupFromOriginal` | grün |
| G01 | DEB-Pokal nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupAllStands` | rot |
| G02 | Europapokal nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupFinalWithMarks` | grün |
| G02 | Europapokal nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_EuropeanCupFromOriginal` | rot |
| G02 | Europapokal nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupQuarterFinal` | grün |
| G02 | Europapokal nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupContinues` | grün |
| G02 | Europapokal nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_WorldCupFromOriginal` | grün |
| G02 | Europapokal nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupAllStands` | grün |
| G03 | WM nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupFinalWithMarks` | grün |
| G03 | WM nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_EuropeanCupFromOriginal` | grün |
| G03 | WM nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupQuarterFinal` | grün |
| G03 | WM nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupContinues` | grün |
| G03 | WM nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_WorldCupFromOriginal` | rot |
| G03 | WM nicht importieren | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupAllStands` | grün |
| G04 | nächste Pokalrunde nicht auslosen | `EishockeyManager.Core.Tests.OriginalImportCupTests.Import_DebCupContinues` | rot |
| G05 | Nachsaison nicht aus HOK übernehmen | `EishockeyManager.Core.Tests.OriginalImportPostseasonTests.Import_RelegationGroups_FromOriginal(file: "FILE44.HOK")` | rot |
| G05 | Nachsaison nicht aus HOK übernehmen | `EishockeyManager.Core.Tests.OriginalImportPostseasonTests.Import_RelegationGroups_FromOriginal(file: "FILE22.HOK")` | rot |
| G05 | Nachsaison nicht aus HOK übernehmen | `EishockeyManager.Core.Tests.OriginalImportPostseasonTests.Import_Playoffs_FromOriginal(file: "FILE22.HOK")` | rot |
| G05 | Nachsaison nicht aus HOK übernehmen | `EishockeyManager.Core.Tests.OriginalImportPostseasonTests.Import_Playoffs_FromOriginal(file: "FILE44.HOK")` | rot |
| G05 | Nachsaison nicht aus HOK übernehmen | `EishockeyManager.Core.Tests.OriginalImportPostseasonTests.Import_SeasonChange_LeaguesLikeNextOriginalStand(file: "FILE44.HOK", next: "FILE46.HOK")` | rot |
| G05 | Nachsaison nicht aus HOK übernehmen | `EishockeyManager.Core.Tests.OriginalImportPostseasonTests.Import_SeasonChange_LeaguesLikeNextOriginalStand(file: "FILE22.HOK", next: "FILE23.HOK")` | rot |
| G05 | Nachsaison nicht aus HOK übernehmen | `EishockeyManager.Core.Tests.OriginalImportPostseasonTests.Import_AllStands_PostseasonMatchesOriginal` | grün |
| G06 | Saisonwechsel endet sofort als GameOver | `EishockeyManager.Core.Tests.OriginalImportPostseasonTests.Import_AllStands_FourSeasonChanges` | grün |
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
| J01 | laufende Bauten nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_StadiumConstruction_File44A` | rot |
| J01 | laufende Bauten nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_StadiumConstruction_File43` | rot |
| J02 | Trendwert nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_PlayerTrendFromSlot` | rot |
| J03 | Vertragsstatus nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_ContractNoticeFromSlot` | rot |
| J04 | Heimspielzahl erhöhen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_StadiumStatisticsFromOriginal(file: "FILE46.HOK")` | rot |
| J04 | Heimspielzahl erhöhen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_StadiumStatisticsFromOriginal(file: "FILE44.HOK")` | grün |
| J04 | Heimspielzahl erhöhen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_StadiumStatisticsFromOriginal(file: "FILE05.HOK")` | rot |
| J05 | kein Freundschaftsspiel übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_FriendliesFromOriginal(file: "FILE18.HOK", count: 0)` | grün |
| J05 | kein Freundschaftsspiel übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_FriendliesFromOriginal(file: "FILE19.HOK", count: 1)` | rot |
| J05 | kein Freundschaftsspiel übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_FriendliesFromOriginal(file: "FILE20.HOK", count: 1)` | rot |
| J06 | mindestens ein Freundschaftsspiel übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_FriendliesFromOriginal(file: "FILE18.HOK", count: 0)` | rot |
| J06 | mindestens ein Freundschaftsspiel übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_FriendliesFromOriginal(file: "FILE19.HOK", count: 1)` | grün |
| J06 | mindestens ein Freundschaftsspiel übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_FriendliesFromOriginal(file: "FILE20.HOK", count: 1)` | grün |
| J07 | Bundestraineramt nicht übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoach_WhenOnlyTheOriginalHasOne` | rot |
| J08 | Bundestrainer-Amtsflag invertieren | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord(file: "FILE11.HOK", inOffice: True)` | rot |
| J08 | Bundestrainer-Amtsflag invertieren | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord(file: "FILE47.HOK", inOffice: True)` | rot |
| J08 | Bundestrainer-Amtsflag invertieren | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord(file: "FILE18.HOK", inOffice: True)` | rot |
| J08 | Bundestrainer-Amtsflag invertieren | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord(file: "FILE05.HOK", inOffice: False)` | grün |
| J09 | Automatikbyte umkehren | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_AutoLineup_ReadsCurrentByteNotTheSavedOne(file: "FILE32.HOK", enabled: False)` | rot |
| J09 | Automatikbyte umkehren | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_AutoLineup_ReadsCurrentByteNotTheSavedOne(file: "FILE33.HOK", enabled: False)` | rot |
| J09 | Automatikbyte umkehren | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_AutoLineup_ReadsCurrentByteNotTheSavedOne(file: "FILE05.HOK", enabled: True)` | rot |
| J10 | Heimspielzahl um eins verringern | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_StadiumStatisticsFromOriginal(file: "FILE46.HOK")` | grün |
| J10 | Heimspielzahl um eins verringern | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_StadiumStatisticsFromOriginal(file: "FILE44.HOK")` | rot |
| J10 | Heimspielzahl um eins verringern | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_StadiumStatisticsFromOriginal(file: "FILE05.HOK")` | rot |
| J11 | Bundestraineramt immer vorhanden | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord(file: "FILE11.HOK", inOffice: True)` | grün |
| J11 | Bundestraineramt immer vorhanden | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord(file: "FILE47.HOK", inOffice: True)` | grün |
| J11 | Bundestraineramt immer vorhanden | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord(file: "FILE18.HOK", inOffice: True)` | grün |
| J11 | Bundestraineramt immer vorhanden | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord(file: "FILE05.HOK", inOffice: False)` | grün |
| J12 | auch ohne Amtsflag einen Bundestrainer einsetzen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord(file: "FILE11.HOK", inOffice: True)` | grün |
| J12 | auch ohne Amtsflag einen Bundestrainer einsetzen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord(file: "FILE47.HOK", inOffice: True)` | grün |
| J12 | auch ohne Amtsflag einen Bundestrainer einsetzen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord(file: "FILE18.HOK", inOffice: True)` | grün |
| J12 | auch ohne Amtsflag einen Bundestrainer einsetzen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord(file: "FILE05.HOK", inOffice: False)` | rot |
| J13 | DEB-Pokalsieger nie übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_CupWinnersFromOriginal(file: "FILE05.HOK", debFinished: False, europeFinished: False)` | grün |
| J13 | DEB-Pokalsieger nie übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_CupWinnersFromOriginal(file: "FILE38.HOK", debFinished: False, europeFinished: True)` | grün |
| J13 | DEB-Pokalsieger nie übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_CupWinnersFromOriginal(file: "FILE44.HOK", debFinished: True, europeFinished: True)` | rot |
| J14 | Europapokalsieger nie übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_CupWinnersFromOriginal(file: "FILE05.HOK", debFinished: False, europeFinished: False)` | grün |
| J14 | Europapokalsieger nie übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_CupWinnersFromOriginal(file: "FILE38.HOK", debFinished: False, europeFinished: True)` | rot |
| J14 | Europapokalsieger nie übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_CupWinnersFromOriginal(file: "FILE44.HOK", debFinished: True, europeFinished: True)` | rot |
| J15 | Vorjahressieger fälschlich als Sieger übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_CupWinnersFromOriginal(file: "FILE05.HOK", debFinished: False, europeFinished: False)` | rot |
| J15 | Vorjahressieger fälschlich als Sieger übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_CupWinnersFromOriginal(file: "FILE38.HOK", debFinished: False, europeFinished: True)` | rot |
| J15 | Vorjahressieger fälschlich als Sieger übernehmen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_CupWinnersFromOriginal(file: "FILE44.HOK", debFinished: True, europeFinished: True)` | grün |
| J16 | Baustellen auch ohne Restwochen anlegen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NoConstruction_WhenOriginalHasNone(file: "FILE41.HOK")` | grün |
| J16 | Baustellen auch ohne Restwochen anlegen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NoConstruction_WhenOriginalHasNone(file: "FILE44.HOK")` | grün |
| J17 | monatlichen Werbebetrag um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.TryImport_SponsorContractAmountNotDivisibleByThree_Loads` | rot |
| J18 | Importfehler als Prüfsummenfehler markieren | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.TryImport_ImportError_IsReportedWithoutChecksumFlag` | rot |
| J19 | Anzeige für automatische Einsatzwerte verschieben | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalTacticsShownAutoLine(file: "FILE05.HOK")` | rot |
| J19 | Anzeige für automatische Einsatzwerte verschieben | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalTacticsShownAutoLine(file: "FILE19.HOK")` | rot |
| J19 | Anzeige für automatische Einsatzwerte verschieben | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NationalTacticsShownAutoLine(file: "FILE20.HOK")` | rot |
| J20 | Landes- und Managersatz vertauschen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.ClubIndices_WorldOnly_NationKeepsItsIndex_ManagerSlotGetsTheMissingClub(file: "FILE11.HOK")` | rot |
| J20 | Landes- und Managersatz vertauschen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.ClubIndices_WorldOnly_NationKeepsItsIndex_ManagerSlotGetsTheMissingClub(file: "FILE18.HOK")` | rot |
| J21 | Baustelle auch bei Nullwert anlegen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NoConstruction_WhenOriginalHasNone(file: "FILE41.HOK")` | rot |
| J21 | Baustelle auch bei Nullwert anlegen | `EishockeyManager.Core.Tests.OriginalImportReviewR14Tests.Import_NoConstruction_WhenOriginalHasNone(file: "FILE44.HOK")` | rot |
| K01 | globale Liga ohne Manager nicht speichern | `EishockeyManager.Core.Tests.KaderStartRunde14Tests.StartLeague_WithoutManagers_StepReportsOnlyRealChanges` | rot |
| K01 | globale Liga ohne Manager nicht speichern | `EishockeyManager.Core.Tests.KaderStartRunde14Tests.StartLeague_SurvivesRemovalOfLastManager` | rot |
| K01 | globale Liga ohne Manager nicht speichern | `EishockeyManager.Core.Tests.KaderStartRunde14Tests.StartLeague_WithoutManagers_IsKept_AndAdoptedByNewManager` | rot |
| K02 | Manager übernimmt eingestellte Liga nicht | `EishockeyManager.Core.Tests.KaderStartRunde14Tests.StartLeague_WithoutManagers_IsKept_AndAdoptedByNewManager` | rot |
| K03 | Schritt am Reglerende als Änderung melden | `EishockeyManager.Core.Tests.KaderStartRunde14Tests.StartLeague_WithoutManagers_StepReportsOnlyRealChanges` | rot |
| K04 | Talente hinten anhängen | `EishockeyManager.Core.Tests.KaderStartRunde14Tests.JoinScoutedTalent_GoalkeeperOrDefender_SortedIn` | rot |
| K05 | alte Startliga nicht migrieren | `EishockeyManager.Core.Tests.KaderStartRunde14Tests.Migration_41To42_TakesStartLeagueOfFirstManager` | rot |
| N11 | Neuaufstellung des Bundestrainers auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation(handMade: True)` | grün |
| N11 | Neuaufstellung des Bundestrainers auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation(handMade: False)` | rot |
| N11 | Neuaufstellung des Bundestrainers auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Click_SwitchesOff_EmptySlotDoesNot` | grün |
| N11 | Neuaufstellung des Bundestrainers auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.MenuBuild_WithDefaultSetting_LinesUpTheSquad` | rot |
| N11 | Neuaufstellung des Bundestrainers auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FullSquad_ClickDoesNothing_KeeperSwapTakesRowThree` | rot |
| N11 | Neuaufstellung des Bundestrainers auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.ClearedLineup_SavedInTurn_LoadLinesUpAgain` | rot |
| N11 | Neuaufstellung des Bundestrainers auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver(active: False)` | rot |
| N11 | Neuaufstellung des Bundestrainers auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver(active: True)` | rot |
| N11 | Neuaufstellung des Bundestrainers auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.ClearedLineup_SwitchesOff_ValuesZero_PlaysEmpty` | rot |
| N11 | Neuaufstellung des Bundestrainers auslassen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_Chain39To41_AddsBothSettings` | grün |
| N12 | manueller Eingriff schaltet Automatik nicht ab | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation(handMade: True)` | grün |
| N12 | manueller Eingriff schaltet Automatik nicht ab | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation(handMade: False)` | grün |
| N12 | manueller Eingriff schaltet Automatik nicht ab | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Click_SwitchesOff_EmptySlotDoesNot` | rot |
| N12 | manueller Eingriff schaltet Automatik nicht ab | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.MenuBuild_WithDefaultSetting_LinesUpTheSquad` | grün |
| N12 | manueller Eingriff schaltet Automatik nicht ab | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FullSquad_ClickDoesNothing_KeeperSwapTakesRowThree` | grün |
| N12 | manueller Eingriff schaltet Automatik nicht ab | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.ClearedLineup_SavedInTurn_LoadLinesUpAgain` | grün |
| N12 | manueller Eingriff schaltet Automatik nicht ab | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver(active: False)` | grün |
| N12 | manueller Eingriff schaltet Automatik nicht ab | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver(active: True)` | rot |
| N12 | manueller Eingriff schaltet Automatik nicht ab | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.ClearedLineup_SwitchesOff_ValuesZero_PlaysEmpty` | rot |
| N12 | manueller Eingriff schaltet Automatik nicht ab | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_Chain39To41_AddsBothSettings` | grün |
| N13 | beim Zuganfang immer Automatik anschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation(handMade: True)` | rot |
| N13 | beim Zuganfang immer Automatik anschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation(handMade: False)` | grün |
| N13 | beim Zuganfang immer Automatik anschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Click_SwitchesOff_EmptySlotDoesNot` | grün |
| N13 | beim Zuganfang immer Automatik anschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.MenuBuild_WithDefaultSetting_LinesUpTheSquad` | grün |
| N13 | beim Zuganfang immer Automatik anschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FullSquad_ClickDoesNothing_KeeperSwapTakesRowThree` | grün |
| N13 | beim Zuganfang immer Automatik anschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.ClearedLineup_SavedInTurn_LoadLinesUpAgain` | grün |
| N13 | beim Zuganfang immer Automatik anschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver(active: False)` | grün |
| N13 | beim Zuganfang immer Automatik anschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver(active: True)` | rot |
| N13 | beim Zuganfang immer Automatik anschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.ClearedLineup_SwitchesOff_ValuesZero_PlaysEmpty` | rot |
| N13 | beim Zuganfang immer Automatik anschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_Chain39To41_AddsBothSettings` | grün |
| N14 | beim Zugende Automatik immer speichern | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation(handMade: True)` | grün |
| N14 | beim Zugende Automatik immer speichern | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation(handMade: False)` | grün |
| N14 | beim Zugende Automatik immer speichern | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Click_SwitchesOff_EmptySlotDoesNot` | grün |
| N14 | beim Zugende Automatik immer speichern | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.MenuBuild_WithDefaultSetting_LinesUpTheSquad` | grün |
| N14 | beim Zugende Automatik immer speichern | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FullSquad_ClickDoesNothing_KeeperSwapTakesRowThree` | grün |
| N14 | beim Zugende Automatik immer speichern | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.ClearedLineup_SavedInTurn_LoadLinesUpAgain` | grün |
| N14 | beim Zugende Automatik immer speichern | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver(active: False)` | grün |
| N14 | beim Zugende Automatik immer speichern | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver(active: True)` | rot |
| N14 | beim Zugende Automatik immer speichern | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.ClearedLineup_SwitchesOff_ValuesZero_PlaysEmpty` | rot |
| N14 | beim Zugende Automatik immer speichern | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_Chain39To41_AddsBothSettings` | grün |
| N15 | Einstellung bei Migration immer einschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation(handMade: True)` | rot |
| N15 | Einstellung bei Migration immer einschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation(handMade: False)` | grün |
| N15 | Einstellung bei Migration immer einschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Click_SwitchesOff_EmptySlotDoesNot` | grün |
| N15 | Einstellung bei Migration immer einschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.MenuBuild_WithDefaultSetting_LinesUpTheSquad` | grün |
| N15 | Einstellung bei Migration immer einschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FullSquad_ClickDoesNothing_KeeperSwapTakesRowThree` | grün |
| N15 | Einstellung bei Migration immer einschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.ClearedLineup_SavedInTurn_LoadLinesUpAgain` | grün |
| N15 | Einstellung bei Migration immer einschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver(active: False)` | grün |
| N15 | Einstellung bei Migration immer einschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver(active: True)` | grün |
| N15 | Einstellung bei Migration immer einschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.ClearedLineup_SwitchesOff_ValuesZero_PlaysEmpty` | grün |
| N15 | Einstellung bei Migration immer einschalten | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_Chain39To41_AddsBothSettings` | grün |
| N16 | vollen Nationalkader nicht begrenzen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation(handMade: True)` | grün |
| N16 | vollen Nationalkader nicht begrenzen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation(handMade: False)` | grün |
| N16 | vollen Nationalkader nicht begrenzen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Click_SwitchesOff_EmptySlotDoesNot` | grün |
| N16 | vollen Nationalkader nicht begrenzen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.MenuBuild_WithDefaultSetting_LinesUpTheSquad` | grün |
| N16 | vollen Nationalkader nicht begrenzen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FullSquad_ClickDoesNothing_KeeperSwapTakesRowThree` | rot |
| N16 | vollen Nationalkader nicht begrenzen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.ClearedLineup_SavedInTurn_LoadLinesUpAgain` | grün |
| N16 | vollen Nationalkader nicht begrenzen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver(active: False)` | grün |
| N16 | vollen Nationalkader nicht begrenzen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver(active: True)` | grün |
| N16 | vollen Nationalkader nicht begrenzen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.ClearedLineup_SwitchesOff_ValuesZero_PlaysEmpty` | grün |
| N16 | vollen Nationalkader nicht begrenzen | `EishockeyManager.Core.Tests.NationalteamRunde14Teil3Tests.Migration_Chain39To41_AddsBothSettings` | grün |
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
| R01 | Importdatum nach dem Einlesen um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_SetsDateManagersAndClubs(file: "FILE29.HOK")` | rot |
| R01 | Importdatum nach dem Einlesen um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_SetsDateManagersAndClubs(file: "FILE16A.HOK")` | rot |
| R01 | Importdatum nach dem Einlesen um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_SetsDateManagersAndClubs(file: "FILE05.HOK")` | rot |
| R01 | Importdatum nach dem Einlesen um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_SetsDateManagersAndClubs(file: "FILE11.HOK")` | rot |
| R01 | Importdatum nach dem Einlesen um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_AllOriginalStands_AreValid` | rot |
| R02 | Heimtore aus dem Ergebnisblock um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_LeagueTableMatchesOriginalTableRecords(file: "FILE31.HOK")` | rot |
| R02 | Heimtore aus dem Ergebnisblock um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_LeagueTableMatchesOriginalTableRecords(file: "FILE24.HOK")` | rot |
| R02 | Heimtore aus dem Ergebnisblock um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_LeagueTableMatchesOriginalTableRecords(file: "FILE40.HOK")` | rot |
| R02 | Heimtore aus dem Ergebnisblock um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_LeagueTableMatchesOriginalTableRecords(file: "FILE37A.HOK")` | rot |
| R02 | Heimtore aus dem Ergebnisblock um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_LeagueTableMatchesOriginalTableRecords(file: "FILE05.HOK")` | rot |
| R02 | Heimtore aus dem Ergebnisblock um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_LeagueTableMatchesOriginalTableRecords(file: "FILE16A.HOK")` | rot |
| R02 | Heimtore aus dem Ergebnisblock um eins erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_LeagueTableMatchesOriginalTableRecords(file: "FILE36.HOK")` | rot |
| R03 | importiertes Gehalt durch zwei statt drei teilen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_ManagerSquadFromSquadSlots` | rot |
| R04 | Spieltag nach Speichern/Laden erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_RoundTripAndContinue(file: "FILE11.HOK")` | rot |
| R04 | Spieltag nach Speichern/Laden erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_RoundTripAndContinue(file: "FILE29.HOK")` | rot |
| R04 | Spieltag nach Speichern/Laden erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_RoundTripAndContinue(file: "FILE16A.HOK")` | rot |
| R04 | Spieltag nach Speichern/Laden erhöhen | `EishockeyManager.Core.Tests.OriginalImportTests.Import_RoundTripAndContinue(file: "FILE05.HOK")` | rot |
| R05 | Saisonwechsel endet sofort als GameOver | `EishockeyManager.Core.Tests.OriginalImportTests.Import_FourSeasonChanges(file: "FILE05.HOK", active: True)` | rot |
| R05 | Saisonwechsel endet sofort als GameOver | `EishockeyManager.Core.Tests.OriginalImportTests.Import_FourSeasonChanges(file: "FILE16A.HOK", active: False)` | grün |
| R05 | Saisonwechsel endet sofort als GameOver | `EishockeyManager.Core.Tests.OriginalImportTests.Import_FourSeasonChanges(file: "FILE05.HOK", active: False)` | grün |
| R05 | Saisonwechsel endet sofort als GameOver | `EishockeyManager.Core.Tests.OriginalImportTests.Import_FourSeasonChanges(file: "FILE11.HOK", active: True)` | rot |
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
| S10 | Maskenindex auf null setzen | `EishockeyManager.UI.Tests.StadiumPartsTests.EveryPartIsDrawnFromItsTableEntryWithMask0xC0` | grün |
| S11 | Zeichenreihenfolge von Halle und Restaurant vertauschen | `EishockeyManager.UI.Tests.StadiumPartsTests.HallCoversRestaurantInDrawOrder` | rot |
| S12 | Hotspotgröße ohne inklusive obere Grenzen | `EishockeyManager.UI.Tests.StadiumPartsTests.Hotspot_EdgesAreInclusive_LikeOriginal(feature: Gastronomie, x: 25, y: 112, w: 51, h: 31)` | rot |
| S12 | Hotspotgröße ohne inklusive obere Grenzen | `EishockeyManager.UI.Tests.StadiumPartsTests.Hotspot_EdgesAreInclusive_LikeOriginal(feature: Kino, x: 29, y: 44, w: 53, h: 22)` | rot |
| S12 | Hotspotgröße ohne inklusive obere Grenzen | `EishockeyManager.UI.Tests.StadiumPartsTests.Hotspot_EdgesAreInclusive_LikeOriginal(feature: Ueberdachung, x: 79, y: 57, w: 145, h: 61)` | rot |
| S12 | Hotspotgröße ohne inklusive obere Grenzen | `EishockeyManager.UI.Tests.StadiumPartsTests.Hotspot_EdgesAreInclusive_LikeOriginal(feature: Verkehrsanbindung, x: 244, y: 50, w: 67, h: 119)` | rot |
| S12 | Hotspotgröße ohne inklusive obere Grenzen | `EishockeyManager.UI.Tests.StadiumPartsTests.Hotspot_EdgesAreInclusive_LikeOriginal(feature: Bowlingbahn, x: 201, y: 94, w: 69, h: 56)` | rot |
| S13 | Hotspots in umgekehrter Reihenfolge prüfen | `EishockeyManager.UI.Tests.StadiumPartsTests.Hotspot_FirstHitWins_InOrderOfTheOriginal` | rot |
| S14 | Bauzeittext ändern | `EishockeyManager.UI.Tests.StadiumPartsTests.ConstructionQuestion_TextRowsAndButtons_LikeDosbox` | rot |
| S15 | Rechtsklick schließt Ausbaufrage | `EishockeyManager.UI.Tests.StadiumPartsTests.ProjectUnderConstructionShowsOnlyHintAndQuestionUsesOriginalDialog` | rot |
| T01 | zentralen Tageszähler auf Remake-Tag zurücksetzen | `EishockeyManager.Core.Tests.NationalCoachMode4Tests.Mode4_NoManagerInTopFive_NoCoach` | grün |
| T01 | zentralen Tageszähler auf Remake-Tag zurücksetzen | `EishockeyManager.Core.Tests.SeasonEndStatusTests.SaveLoad_StatusBitIsDerivedFromSavedData` | grün |
| T01 | zentralen Tageszähler auf Remake-Tag zurücksetzen | `EishockeyManager.Core.Tests.NationalCoachMode4Tests.Mode4_TwoManagersInTopFive_HigherPlacedBecomesCoach` | rot |
| T01 | zentralen Tageszähler auf Remake-Tag zurücksetzen | `EishockeyManager.Core.Tests.SeasonEndStatusTests.Full_ManagerInTopFive_GetsStatusBit_OtherNot` | grün |
| T01 | zentralen Tageszähler auf Remake-Tag zurücksetzen | `EishockeyManager.Core.Tests.NationalCoachMode4Tests.Mode4_Appointment_BeforeReroll_GermanyKeepsBaseRatings` | grün |
| T01 | zentralen Tageszähler auf Remake-Tag zurücksetzen | `EishockeyManager.Core.Tests.SeasonEndStatusTests.StatusBit_DoesNotChangeStoredEventsOrPremium` | grün |
| T01 | zentralen Tageszähler auf Remake-Tag zurücksetzen | `EishockeyManager.Core.Tests.SeasonEndStatusTests.Full_EveryManagerInTopFive_GetsStatusBit` | rot |
| T01 | zentralen Tageszähler auf Remake-Tag zurücksetzen | `EishockeyManager.Core.Tests.StatistikenPraemienTests.OtherClubLeads_NoSinglePremium` | grün |
| T01 | zentralen Tageszähler auf Remake-Tag zurücksetzen | `EishockeyManager.Core.Tests.SpielstaerkeManager2Tests.FourSeasonChanges_ManagedMatchesPlausible(active: True)` | grün |
| T01 | zentralen Tageszähler auf Remake-Tag zurücksetzen | `EishockeyManager.Core.Tests.SpielstaerkeManager2Tests.FourSeasonChanges_ManagedMatchesPlausible(active: False)` | grün |
| T02 | Testsaat auf Base zurücksetzen | `EishockeyManager.Core.Tests.NationalCoachMode4Tests.Mode4_TwoManagersInTopFive_HigherPlacedBecomesCoach` | rot |
| T03 | Testsaat auf Base zurücksetzen | `EishockeyManager.Core.Tests.NationalCoachMode4Tests.Mode4_NoManagerInTopFive_NoCoach` | grün |
| T04 | Testsaat auf Base zurücksetzen | `EishockeyManager.Core.Tests.NationalCoachMode4Tests.Mode4_Appointment_BeforeReroll_GermanyKeepsBaseRatings` | grün |
| T05 | Testsaat auf Base zurücksetzen | `EishockeyManager.Core.Tests.SeasonEndStatusTests.Full_ManagerInTopFive_GetsStatusBit_OtherNot` | grün |
| T06 | Testsaat auf Base zurücksetzen | `EishockeyManager.Core.Tests.SeasonEndStatusTests.Full_EveryManagerInTopFive_GetsStatusBit` | rot |
| T07 | Testsaat auf Base zurücksetzen | `EishockeyManager.Core.Tests.SeasonEndStatusTests.StatusBit_DoesNotChangeStoredEventsOrPremium` | rot |
| T08 | Testsaat auf Base zurücksetzen | `EishockeyManager.Core.Tests.SeasonEndStatusTests.SaveLoad_StatusBitIsDerivedFromSavedData` | grün |
| T09 | Testsaat auf Base zurücksetzen | `EishockeyManager.Core.Tests.SpielstaerkeManager2Tests.FourSeasonChanges_ManagedMatchesPlausible(active: True)` | grün |
| T09 | Testsaat auf Base zurücksetzen | `EishockeyManager.Core.Tests.SpielstaerkeManager2Tests.FourSeasonChanges_ManagedMatchesPlausible(active: False)` | grün |
| T10 | Testsaat auf Base zurücksetzen | `EishockeyManager.Core.Tests.StatistikenPraemienTests.OtherClubLeads_NoSinglePremium` | grün |
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
| U07 | auch auf leeren Zeilen Marker anzeigen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Marker_NotOnEmptyRows` | rot |
| U08 | Info bei Loslassen nicht schließen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Info_OfARemakeSave_ShowsOnlyWhileTheRightButtonIsHeld` | rot |
| U09 | Spieltag des Originalkopfs erhöhen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Info_OfAnOriginalSave_ComesFromTheHeader_AsInFile46` | rot |
| U10 | Liga im Weltkugelmodus anzeigen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Info_ShowsNoLeagueInWorldOnlyMode` | rot |
| U11 | Warnung bei Fremddatei ändern | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Info_OfAForeignFile_ShowsTheNoSaveGameMessage` | rot |
| U12 | Taste PageDown blättert | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Paging_WheelAndKeys_DoNothing` | rot |
| U13 | Seitenzahl im Panel anzeigen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Panel_ShowsNoPageLabel` | grün |
| U14 | Pfeilgrafik nicht zeichnen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Arrows_AreTheKb4Graphics_AtTheOriginalCallSite` | rot |
| U15 | seitenzahlabhängiges Label zeichnen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Panel_ShowsNoPageLabel` | rot |
| U16 | konstantes Seitenlabel zeichnen | `EishockeyManager.UI.Tests.SaveLoadDialogOriginalTests.Panel_ShowsNoPageLabel` | grün |
| V01 | Sommerbeginn einen Tag vorziehen | `EishockeyManager.Core.Tests.GameModeTests.Mode4_ReferenceValues_Unchanged` | rot |
| V02 | alte Halbfinalpaarung | `EishockeyManager.Core.Tests.GameModeTests.Mode4_ReferenceValues_Unchanged` | rot |
| V03 | WM-Tageslauf auslassen | `EishockeyManager.Core.Tests.GameModeTests.Mode4_ReferenceValues_Unchanged` | rot |
| V04 | Hauptwegtakt mit Tag statt Zähler | `EishockeyManager.Core.Tests.GameModeTests.Mode4_ReferenceValues_Unchanged` | rot |
| V05 | Wanderung mit Tag statt Zähler | `EishockeyManager.Core.Tests.GameModeTests.Mode4_ReferenceValues_Unchanged` | rot |
| V06 | zentralen Tageszähler auf Remake-Tag zurücksetzen | `EishockeyManager.Core.Tests.GameModeTests.Mode4_ReferenceValues_Unchanged` | rot |
| V07 | Listenwechsel im Hauptweg auslassen | `EishockeyManager.Core.Tests.GameModeTests.Mode4_ReferenceValues_Unchanged` | rot |
| V08 | Tageslauf jeden Tag ausführen | `EishockeyManager.Core.Tests.GameModeTests.Mode4_ReferenceValues_Unchanged` | rot |
| V09 | WM-Schleife einen Tag früher beginnen | `EishockeyManager.Core.Tests.GameModeTests.Mode4_ReferenceValues_Unchanged` | rot |
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

Grundlage ist die Roslyn-Inventur aus Teil 2 mit 334 Methoden. `rot` bedeutet eine in Teil 4 erneut ausgeführte gezielte Mutation einer Produktregel, Feldkarte, Testhilfe oder Eingabedaten, die mindestens eine Zeile der Methode rot macht; die vollständige Einzelzeilentabelle darüber ist maßgebend. `offen` bedeutet, dass diese erneute Gegenprobe in Teil 4 noch aussteht. Bereits in Teil 2 oder 3 dokumentierte Ergebnisse bleiben dort gültig, ersetzen aber nicht die hier neu ausstehende Protokollierung. Änderungen an Testhelfern, Datenquellen und gelöschte Tests bleiben zusätzlich zu prüfen.

| Testdatei / Methode | Delta | Erneute Gegenprobe |
|---|---|---|
| `AutoLineupTeil8Tests.cs · AutoLineupTeil8Tests.Migration_31To32_KeepsHandMadeLineup` | geändert | offen |
| `DeadlineRegressionTests.cs · DeadlineRegressionTests.R208_AnswerChecksDeadlineAtPausedUnprocessedDay` | geändert | offen |
| `FinanceCalibrationTests.cs · FinanceCalibrationTests.PassiveBundesligaManager_WithoutCare_NotDismissed_ForcedRelegation` | neu | offen |
| `GameModeTests.cs · GameModeTests.Mode4_ReferenceValues_Unchanged` | geändert | rot: V01, V02, V03, V04, V05, V06, V07, V08, V09 |
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
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.AutoLineup_AfterMatchWithoutInjury_LinesUpAgain` | neu | rot: B01 |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.ManualLineup_AfterMatchWithoutInjury_Unchanged` | neu | rot: B02 |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.CupMatch_CountsInCupFields` | neu | rot: B03 |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.EuropeanCupMatch_CountsInEuropeFields` | neu | rot: B04 |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.Postseason_CountsAsLeague` | neu | rot: B05 |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.Join_ResetsClubCountersOnly` | neu | rot: B06 |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.FirstMatchDay_NewGame_UsesRankZero` | neu | rot: B07 |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.MultiSeason_TableRankAndCompetitionCounters` | neu | rot: B08 |
| `KaderStartRunde14Teil2bTests.cs · KaderStartRunde14Teil2bTests.Migration_OldSave_LeagueOnlyAndTableRankFromRankSlots` | neu | rot: B09 |
| `KaderStartRunde14Teil6Tests.cs · KaderStartRunde14Teil6Tests.LeftClick_WithoutLine_NoLineSelected_NothingChanges` | neu | offen |
| `KaderStartRunde14Teil6Tests.cs · KaderStartRunde14Teil6Tests.LeftClick_FirstFreeSlotOfLine_FullLineNoRoom_GoalkeeperWrongLine` | neu | offen |
| `KaderStartRunde14Teil6Tests.cs · KaderStartRunde14Teil6Tests.ClickOnPlaced_TakesOut_SelectsHisLine_LeftUndresses_RightKeeps` | neu | offen |
| `KaderStartRunde14Teil6Tests.cs · KaderStartRunde14Teil6Tests.RightClick_TogglesDressed_TwentyThirdRefused_AutoLineupOff` | neu | offen |
| `KaderStartRunde14Tests.cs · KaderStartRunde14Tests.StartLeague_WithoutManagers_IsKept_AndAdoptedByNewManager` | neu | rot: K01, K02 |
| `KaderStartRunde14Tests.cs · KaderStartRunde14Tests.StartLeague_WithoutManagers_StepReportsOnlyRealChanges` | neu | rot: K01, K03 |
| `KaderStartRunde14Tests.cs · KaderStartRunde14Tests.StartLeague_SurvivesRemovalOfLastManager` | neu | rot: K01 |
| `KaderStartRunde14Tests.cs · KaderStartRunde14Tests.JoinScoutedTalent_GoalkeeperOrDefender_SortedIn` | neu | rot: K04 |
| `KaderStartRunde14Tests.cs · KaderStartRunde14Tests.Migration_41To42_TakesStartLeagueOfFirstManager` | neu | rot: K05 |
| `KaderStartStartLigaTests.cs · KaderStartStartLigaTests.Bundesliga_AlwaysDraws_NeverTheEuropeanCupPlace` | geändert | offen |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.Offer_PlayerLeavesSquad_SlotsMoveUp_ListingWithSeller` | neu | rot: C01 |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.Withdraw_ReturnsPlayerByRole_NotInLineup_ContractUnchanged` | neu | rot: C02 |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.Withdraw_FullSquad_StaysOnMarket` | neu | rot: C03 |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.ListedPlayer_KeepsCostingSalary` | neu | rot: C04 |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.SoldListedPlayer_GoesToBuyer_SellerPaid` | neu | rot: C05 |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.SeasonChange_UnsoldListedPlayerReturnsByRole_NotInLineup_ContractCountedDown` | neu | rot: C06 |
| `KaderStartTeil2cTests.cs · KaderStartTeil2cTests.FourSeasonChanges_ListedPlayersReturn_NoDuplicates` | neu | rot: C06 |
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
| `LoadQuirkTests.cs · LoadQuirkTests.OnlyMarkerDaySaves_GetTheOffset` | neu | rot: E01 |
| `LoadQuirkTests.cs · LoadQuirkTests.File44_AfterImport_SecondDayChange_FifteenthOfApril` | neu | rot: E02 |
| `LoadQuirkTests.cs · LoadQuirkTests.File44_Offset_SurvivesSave_EndsAtSeasonChange_FourSeasons` | neu | rot: E03 |
| `LoadQuirkTests.cs · LoadQuirkTests.NewGame_HasNoOffset` | neu | rot: E04 |
| `LoadQuirkTests.cs · LoadQuirkTests.Schema46_WithoutOffset_LoadsWithZero` | neu | rot: E05 |
| `NationalCoachMode4Tests.cs · NationalCoachMode4Tests.Mode4_TwoManagersInTopFive_HigherPlacedBecomesCoach` | geändert | rot: T01 |
| `NationalCoachMode4Tests.cs · NationalCoachMode4Tests.Mode4_NoManagerInTopFive_NoCoach` | geändert | offen |
| `NationalCoachMode4Tests.cs · NationalCoachMode4Tests.Mode4_Appointment_BeforeReroll_GermanyKeepsBaseRatings` | geändert | offen |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.MenuBuild_WithDefaultSetting_LinesUpTheSquad` | neu | rot: N11 |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.ClearedLineup_SwitchesOff_ValuesZero_PlaysEmpty` | neu | rot: N11, N12, N13, N14 |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.ClearedLineup_SavedInTurn_LoadLinesUpAgain` | neu | rot: N11 |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.Click_SwitchesOff_EmptySlotDoesNot` | neu | rot: N12 |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.FourSeasons_SettingCarriesOver` | neu | rot: N11, N12, N13, N14 |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.Migration_SetsSettingFromFormation` | neu | rot: N11, N13, N15 |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.Migration_Chain39To41_AddsBothSettings` | neu | offen |
| `NationalteamRunde14Teil3Tests.cs · NationalteamRunde14Teil3Tests.FullSquad_ClickDoesNothing_KeeperSwapTakesRowThree` | neu | rot: N11, N16 |
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
| `OriginalImportCupTests.cs · OriginalImportCupTests.Import_DebCupQuarterFinal` | neu | rot: G01 |
| `OriginalImportCupTests.cs · OriginalImportCupTests.Import_DebCupFinalWithMarks` | neu | rot: G01 |
| `OriginalImportCupTests.cs · OriginalImportCupTests.Import_DebCupAllStands` | neu | rot: G01 |
| `OriginalImportCupTests.cs · OriginalImportCupTests.Import_DebCupContinues` | neu | rot: G04 |
| `OriginalImportCupTests.cs · OriginalImportCupTests.Import_EuropeanCupFromOriginal` | neu | rot: G02 |
| `OriginalImportCupTests.cs · OriginalImportCupTests.Import_WorldCupFromOriginal` | neu | rot: G03 |
| `OriginalImportFieldMapDataTests.cs · OriginalImportFieldMapDataTests.ClubRecords_MapToDatabaseByName` | neu | rot: F06 |
| `OriginalImportFieldMapDataTests.cs · OriginalImportFieldMapDataTests.LeagueTable_UsesSlotsOfItsLeague` | neu | rot: F07 |
| `OriginalImportFieldMapDataTests.cs · OriginalImportFieldMapDataTests.ManagerRecordOffsets_MatchDocumentedValues` | neu | rot: F08 |
| `OriginalImportFieldMapTests.cs · OriginalImportFieldMapTests.FieldMap_NamesSchemaVersion` | neu | rot: F01 |
| `OriginalImportFieldMapTests.cs · OriginalImportFieldMapTests.FieldMap_CoversEverySchemaFieldExactlyOnce` | neu | rot: F02 |
| `OriginalImportFieldMapTests.cs · OriginalImportFieldMapTests.FieldMap_RowsHaveValidStatusAndSource` | neu | rot: F03 |
| `OriginalImportFieldMapTests.cs · OriginalImportFieldMapTests.FieldMap_ImportedRows_HaveTheirBlocksAndOffsetsInTheImportCode` | neu | rot: F04 |
| `OriginalImportFieldMapTests.cs · OriginalImportFieldMapTests.StripComments_RemovesLineAndBlockCommentsButKeepsStrings` | neu | rot: F05 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_PoolFromOriginal` | neu | rot: P01 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_MarketFromOriginal` | neu | rot: P02 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_MarketListingOfManager` | neu | rot: P03 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_PoolNamesUnique_AndContinue` | neu | rot: P04 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_NationalSquadFromOriginal` | neu | rot: P05 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_MessagesFromOriginal` | neu | rot: P06 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_MessageAboutPoolPlayer` | neu | rot: P07 |
| `OriginalImportPoolTests.cs · OriginalImportPoolTests.Import_RoundTripWithPoolMarketNational` | neu | rot: P08 |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_RelegationGroups_FromOriginal` | neu | rot: G05 |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_Playoffs_FromOriginal` | neu | rot: G05 |
| `OriginalImportPostseasonTests.cs · OriginalImportPostseasonTests.Import_SeasonChange_LeaguesLikeNextOriginalStand` | neu | rot: G05 |
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
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_StadiumConstruction_File43` | neu | rot: J01 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_StadiumConstruction_File44A` | neu | rot: J01 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_NoConstruction_WhenOriginalHasNone` | neu | rot: J21 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.TryImport_SponsorContractAmountNotDivisibleByThree_Loads` | neu | rot: J17 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.TryImport_ImportError_IsReportedWithoutChecksumFlag` | neu | rot: J18 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_FriendliesFromOriginal` | neu | rot: J05, J06 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_StadiumStatisticsFromOriginal` | neu | rot: J04, J10 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_PlayerTrendFromSlot` | neu | rot: J02 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_ContractNoticeFromSlot` | neu | rot: J03 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_NationalCoachFromClubRecord` | neu | rot: J08, J12 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_NationalCoach_WhenOnlyTheOriginalHasOne` | neu | rot: J07 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_NationalTacticsShownAutoLine` | neu | rot: J19 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_CupWinnersFromOriginal` | neu | rot: J13, J14, J15 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.ClubIndices_WorldOnly_NationKeepsItsIndex_ManagerSlotGetsTheMissingClub` | neu | rot: J20 |
| `OriginalImportReviewR14Tests.cs · OriginalImportReviewR14Tests.Import_AutoLineup_ReadsCurrentByteNotTheSavedOne` | neu | rot: J09 |
| `OriginalImportRunde14Tests.cs · OriginalImportRunde14Tests.Import_TakesNationalCoachAutoLineupFromBlock0375` | neu | offen |
| `OriginalImportRunde14Tests.cs · OriginalImportRunde14Tests.Import_AllStands_RoundTripIsStable` | neu | offen |
| `OriginalImportTests.cs · OriginalImportTests.Import_SetsDateManagersAndClubs` | neu | rot: R01 |
| `OriginalImportTests.cs · OriginalImportTests.Import_LeagueTableMatchesOriginalTableRecords` | neu | rot: R02 |
| `OriginalImportTests.cs · OriginalImportTests.Import_AllOriginalStands_AreValid` | neu | rot: R01 |
| `OriginalImportTests.cs · OriginalImportTests.Import_ManagerSquadFromSquadSlots` | neu | rot: R03 |
| `OriginalImportTests.cs · OriginalImportTests.Import_RoundTripAndContinue` | neu | rot: R04 |
| `OriginalImportTests.cs · OriginalImportTests.Import_FourSeasonChanges` | neu | rot: R05 |
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
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.ManagerPlaysCupDay_OptionOn_AllPairingsUseMinuteLoop_RandomDiffers` | neu | rot: E06 |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.NoManagerInvolved_OptionDoesNothing_SameRandom` | neu | rot: E07 |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.Default_IsOff_AndSaveRoundTripKeepsIt` | neu | rot: E08 |
| `SaisonManagerTagTests.cs · SaisonManagerTagTests.Schema39Save_Migrates_OptionOff` | neu | rot: E09 |
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
| `SeasonEndStatusTests.cs · SeasonEndStatusTests.Full_EveryManagerInTopFive_GetsStatusBit` | geändert | rot: T01 |
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
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.MainPath_TwoManagers_NegotiationLockCountsOncePerTick` | neu | rot: D02 |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.SummerBreak_CountersFromSeasonEndToNinthOfSeptember` | neu | rot: D01 |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.SummerBreak_AdvancesConstructionAndHealsSquad` | neu | rot: D04 |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.SummerBreak_DayClose_FourMonthEndsBooked` | neu | rot: D01, D03 |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` | neu | rot: D01, D03 |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.File44_GastronomyOrderedOnFourteenthOfApril_Loses21WeeksUntilNewSeason` | neu | rot: D04 |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.MainPath_TickOfHaltDay_RunsBeforeHalt` | neu | rot: D02, D05 |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.File24_ToMenuOfDay17_MarketDecayedOnceLikeFile25` | neu | rot: D02, D05 |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.SeasonChange_KeepsMarketList_PlayersAgeOneYear` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.File44_SeasonChange_KeepsListLikeFile46` | neu | offen |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak` | neu | rot: D04 |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.MainPath_ManagerZeroDismissed_MarketStillDecaysPerTick` | neu | rot: D05 |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.MainPath_TrendControlCountsDownPerTick` | neu | rot: D02, D06 |
| `TagestaktHauptwegReviewTests.cs · TagestaktHauptwegReviewTests.AdvanceTrend_ClampsAndRerollsAtZero` | neu | offen |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.TrainingDays_AreDayCounterTicks` | neu | rot: D02 |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.MainPath_TwoManagers_TrainOnlyOnTicks` | neu | rot: D02 |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.MainPath_MarketDecay_OncePerTick` | neu | rot: D02, D05 |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.ListChangeDays_OnlyEventDaysOfMainPath` | neu | offen |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.CatchUpManagerDay_TwoManagers_SecondNeitherExpiresNorBidsOnForeignListings` | neu | offen |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero` | neu | rot: D02 |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.FourSeasonChanges_TwoManagers_TrainingOnTicks` | neu | offen |
| `TagestaktHauptwegTests.cs · TagestaktHauptwegTests.Construction_AdvancesOnCounterZeroDays` | neu | offen |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.SummerBreakCounters_FirstOfMayToNinthOfSeptember_FourMonthEnds` | neu | rot: D01 |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_LoadQuirkBooksAprilMonthEnd_LikeFile46_2` | neu | rot: D01, D03 |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.File44_Valliere_ImportedAsLoan_EndsFreeWithoutCompensation` | neu | offen |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.PassiveBundesligaManager_NeverDismissed_ForcedRelegationInstead` | neu | rot: D07, D08 |
| `TagestaktSommerpauseReviewTests.cs · TagestaktSommerpauseReviewTests.DeepDebts_NoDismissal_GameContinues` | neu | rot: D08 |
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
| `HokImportAccessTests.cs · HokImportAccessTests.List_MarksHokFilesAsOriginal` | neu | rot: A01 |
| `HokImportAccessTests.cs · HokImportAccessTests.TryImport_RejectsNonOriginalData_WithoutChecksumFlag` | neu | rot: A02 |
| `HokImportAccessTests.cs · HokImportAccessTests.LoadDialog_ImportsHok_ByTypedName` | neu | rot: A03 |
| `HokImportAccessTests.cs · HokImportAccessTests.LoadDialog_ImportsHok_ByClickTwice` | neu | rot: A04 |
| `HokImportAccessTests.cs · HokImportAccessTests.SameName_TypedLoadsRemakeSave_ClickOnHokImports` | neu | rot: A05 |
| `HokImportAccessTests.cs · HokImportAccessTests.SameName_TypedThenSingleClickOnHok_Imports` | neu | rot: A06 |
| `HokImportAccessTests.cs · HokImportAccessTests.ChecksumError_ShowsMogelguru_AndKeepsRunningGame` | neu | rot: A07 |
| `HokImportAccessTests.cs · HokImportAccessTests.TryImport_FlagsChecksumError_AndImportsValidSave` | neu | rot: A08 |
| `HokImportAccessTests.cs · HokImportAccessTests.SaveDialog_DoesNotSelectHok` | neu | rot: A09 |
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
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Paging_WheelAndKeys_DoNothing` | neu | rot: U12 |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Panel_ShowsNoPageLabel` | neu | rot: U15 |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Arrows_AreTheKb4Graphics_AtTheOriginalCallSite` | neu | rot: U14 |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Marker_AppearsLeftOfTheEntryUnderTheMouse_AndIsRemovedWhenLeaving` | neu | rot: U06 |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Marker_ColumnBoundaries_LikeTheOriginal` | neu | rot: U02, U03, U04, U05 |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Marker_NotOnEmptyRows` | neu | rot: U07 |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Info_OfARemakeSave_ShowsOnlyWhileTheRightButtonIsHeld` | neu | rot: U08 |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Info_OfAnOriginalSave_ComesFromTheHeader_AsInFile46` | neu | rot: U09 |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Info_ShowsNoLeagueInWorldOnlyMode` | neu | rot: U10 |
| `SaveLoadDialogOriginalTests.cs · SaveLoadDialogOriginalTests.Info_OfAForeignFile_ShowsTheNoSaveGameMessage` | neu | rot: U11 |
| `SeasonEndColorsTests.cs · SeasonEndColorsTests.Palette18_12_22_MatchDosBoxPictures` | geändert | offen |
| `StadiumArrowTests.cs · StadiumArrowTests.TicketArrows_AreTenPixelsWide_FromKb4BaseGraphic` | neu | offen |
| `StadiumPartsTests.cs · StadiumPartsTests.EveryPartIsDrawnFromItsTableEntryWithMask0xC0` | neu | offen |
| `StadiumPartsTests.cs · StadiumPartsTests.HallCoversRestaurantInDrawOrder` | neu | rot: S11 |
| `StadiumPartsTests.cs · StadiumPartsTests.ProjectUnderConstructionShowsOnlyHintAndQuestionUsesOriginalDialog` | neu | rot: S15 |
| `StadiumPartsTests.cs · StadiumPartsTests.ConstructionQuestion_TextRowsAndButtons_LikeDosbox` | neu | rot: S14 |
| `StadiumPartsTests.cs · StadiumPartsTests.Hotspot_EdgesAreInclusive_LikeOriginal` | neu | rot: S12 |
| `StadiumPartsTests.cs · StadiumPartsTests.Hotspot_FirstHitWins_InOrderOfTheOriginal` | neu | rot: S13 |
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

### Base-Saat bei unverändertem Head

Die Saat wurde ausschließlich in der Testkopie auf den Base-Wert gesetzt; dies ist keine Produktregelmutation. Fehlende Vorbedingungen und grüne Fälle sind getrennt zu bewerten.

| Test | Probelauf | Ergebnis |
|---|---|---|
| `Mode4_TwoManagersInTopFive_HigherPlacedBecomesCoach` | T02 | rot |
| `Mode4_NoManagerInTopFive_NoCoach` | T03 | grün |
| `Mode4_Appointment_BeforeReroll_GermanyKeepsBaseRatings` | T04 | grün |
| `Full_ManagerInTopFive_GetsStatusBit_OtherNot` | T05 | grün |
| `Full_EveryManagerInTopFive_GetsStatusBit` | T06 | rot |
| `StatusBit_DoesNotChangeStoredEventsOrPremium` | T07 | rot |
| `SaveLoad_StatusBitIsDerivedFromSavedData` | T08 | grün |
| `FourSeasonChanges_ManagedMatchesPlausible` | T09 | grün (active: True), grün (active: False) |
| `OtherClubLeads_NoSinglePremium` | T10 | grün |

T02 und T06 scheitern an der Vorbedingung, dass beide Managervereine in den ersten fünf stehen; T07 am erwarteten Statusbit. Die übrigen Base-Saaten bestanden unter dem heutigen Head, ohne dass damit ihre historischen Ursachen widerlegt sind.
