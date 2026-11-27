# Review: Integration Runde 14, Spur `hok-import` (PR #670)

Base `8436a9bd4f958c270a99cdfa79bdfaf2429ccae3` · Head `38ebbad7ca78101bd726e3d6172c9b420456af4e` · Reviewer: Claude (geplanter
Lauf, frische Sitzung ohne Implementierungskontext) · 09.11.2026 · Checkout: Klon, `git rev-parse HEAD` = Head, `git status
--porcelain` leer; Mutationen nur in einem eigenen Worktree desselben Heads. Codelesen **und** Ausführung am nachgewiesenen
Checkout mit Originaldaten (`kaaasi/eishockey-manager-referenz`, Stand `779a1c8`, 52 Stände `original/spielstaende/FILE*.HOK`
und `KAPUTT.HOK`). Eigener Entschlüssler (Python, unabhängig vom C#-Leser, nur die Blockfolge `OriginalSaveGame.Layout`
übernommen), eigenes Prüfprogramm gegen `Core`/`Formats` (Import aller Stände, Fehlerfälle, Fuzzing, Saisonwechsel).
Pixelvergleich: DOSBox-Bilder 642 × 507 → Spielpixel (1 + 2x | 26 + 2y), Abweichung > 24 je Kanal.

H = `https://github.com/kaaasi/eishockey-manager-redone/blob/38ebbad7ca78101bd726e3d6172c9b420456af4e`

## 1. Merge-Empfehlung

**Nein** für Head `38ebbad`. Gründe:

- **R14HI-01 (P1):** Der Import übernimmt Play-offs, Play-downs und Aufstiegsrunden nicht aus dem HOK. Er rechnet sie beim
  Nachspielen neu. Bei Ständen nach der Hauptrunde ist das Ergebnis falsch: Nach dem Import von FILE44 und einem Saisonwechsel
  steigen andere Vereine auf und ab als im Original (FILE46). Die Feldkarte führt diese Felder als belegt bzw. abgeleitet.
- **R14HI-02 (P1):** Laufende Stadionausbauten (Ziel und Restwochen) gehen verloren. Bei FILE43 ist der bezahlte Ausbau
  (370.000 DM) nach dem Import weg.
- **R14HI-03 (P2):** Ein Spielstand mit gültigen Prüfsummen bringt das Programm zum Absturz. `TryImport` lässt eine
  `KeyNotFoundException` durch, und der Laden-Dialog fängt sie nicht ab.
- **R14HI-04 (P2):** Die Feldkarte widerspricht dem Code. Weitere Zeilen mit Status B/A werden nicht importiert, ohne
  `[APPROX]`. Das verletzt E14-4 = A.

Alles Übrige trägt: Teil 5 (Speichern-Dialog) stimmt am Bild und in em.exe, die Fehlerfälle (3) sind sauber, und alle 52 Stände
überstehen 4 Saisonwechsel ohne Ausnahme. Offene Prüfbereiche stehen in Abschnitt 3.

## 2. Befunde

### R14HI-01 (P1) – Play-offs, Play-downs und Aufstiegsrunden kommen aus dem Nachspielen, nicht aus dem HOK

[`OriginalImport.cs` `ReplayTo`](H/src/EishockeyManager.Core/OriginalImport.cs#L243) ersetzt nur Ligaergebnisse (`A0CB`).
`CED8`/`CDB8`/`CCA4`/`CD88`/`CD58` (Play-off-Serien, Gruppen der Aufstiegsrunde, Siege, Platzlisten) liest kein Importcode.
Die Feldkarte sagt das Gegenteil: `Playoffs._rounds` B, `RelegationGroup.Results`/`.Clubs`/`.Rounds` B, `GameState.PlayoffResults` A
([Zeilen 391, 725–731](H/docs/formate/spielstand-original.md#L725)). Unter „Noch nicht übernommen“
([Zeile 806](H/docs/formate/spielstand-original.md#L806)) fehlen sie.

Gegenbeispiele (selbst ausgeführt):

| Stand | Original (HOK) | Import |
|---|---|---|
| FILE44 (14.04.1994, Runde fertig: alle 6 Termine in `CDB8+0x48`) | Aufstiegsrunde Bundesliga: Teilnehmer Freiburg, Preussen Berlin, Weißwasser, Kassel; Platzliste `CD88+12` = 3 2 0 1 → **Weißwasser und Kassel** vorn | dieselben Teilnehmer (Tabelle stimmt mit `5CCB` überein), aber Preussen 12:0, Freiburg 6:6, Weißwasser 6:6, Kassel 0:12 |
| FILE44 → Saisonwechsel, Vergleich mit FILE46 | Bundesliga mit EC Kassel, ES Weißwasser | Bundesliga mit EHC Freiburg und SC Preussen Berlin. Zweite Liga und Oberliga weichen entsprechend ab |
| FILE22 (14.04.1994) | Aufstiegsrunde Bundesliga/2. Liga: EC Ratingen, EV Landshut, ECD Sauerland, ERC Schwenningen; 2. Liga/Oberliga: EV Landsberg, Augsburger EV, SC Riessersee, ESC Wedemark | DEG, EV Landshut, ERC Schwenningen, ES Weißwasser bzw. Augsburger EV, EHC Essen-West, TSV Erding, ESC Wolfsburg. Die Abschlusstabellen stimmen mit `5CCB` überein; erst die neu gespielten Play-downs ergeben andere Teilnehmer |
| FILE22 → Saisonwechsel, Vergleich mit FILE23 | Bundesliga mit Schwenningen | Bundesliga mit Landshut, Schwenningen bleibt in der 2. Liga |

Betroffen sind alle Stände ab dem Ende der Hauptrunde: FILE11, FILE18–FILE22, FILE44, FILE47–FILE49A. Teil 4 (E14-4 = A) verlangt,
alle Bereiche zu importieren oder sie als `[APPROX]` mit Grund in der Karte zu führen. Hier geschieht keins von beiden.

### R14HI-02 (P1) – Laufender Stadionausbau geht beim Import verloren

[`ApplyManager`](H/src/EishockeyManager.Core/OriginalImport.cs#L357) setzt je Bereich nur die Stufe (`965B+0x116+2i`). Ziel
(`+0x117+2i`) und Restwochen (`+0x128+p`) übernimmt der Import nicht. Die Karte führt beide als B
([Zeilen 501–502](H/docs/formate/spielstand-original.md#L501)).

- FILE43: Das HOK enthält Sitzkomfort Stufe 0 mit Ziel 1 (Wort 256) und 8 Wochen Rest; die 370.000 DM sind schon vom Konto
  abgebucht. Nach dem Import ist `Stadium.Constructions` leer. Der Ausbau ist weg, das Geld auch.
- FILE44A: Bei der Gastronomie steht Stufe 2 mit Ziel 3 (Wort 770), also das Restaurant im Bau. Auch dieser Ausbau geht verloren.

Regressionstest-Vorschlag: FILE43 importieren und den Ausbau Sitzkomfort → 1 mit Restwochen erwarten.

### R14HI-03 (P2) – Absturz bei einem Werbevertrag, dessen Betrag nicht durch 3 teilbar ist

[`ApplySponsoring`](H/src/EishockeyManager.Core/OriginalImport.Rest.cs#L194) bildet den Schlüssel neu als
`MonthlyAmount * 3`. Bei einem Betrag wie 1.000.001 Drittel-DM ergibt das 999.999, und der Wörterbuchzugriff wirft eine
`KeyNotFoundException`. Der Filter in [`TryImport`](H/src/EishockeyManager.Core/OriginalImport.Access.cs#L46) fängt diesen Typ
nicht, und [`SaveLoadScreen.Import`](H/src/EishockeyManager.UI/Screens/SaveLoad/SaveLoadScreen.cs#L472) ruft `TryImport` ohne
eigenen Schutz auf. Die Ausnahme beendet also das Programm. Die Doku verspricht dagegen „keine Ausnahme nach außen“.

Nachstellen: FILE05 nehmen, `C15B` Fläche 0 auf Betrag 1.000.001, Partner 0, 6 Monate setzen und mit `OriginalSavePatcher.Encode`
neu verschlüsseln. Die Prüfsummen sind dann gültig, `TryImport` wirft `KeyNotFoundException: '(0, 999999, 6)'`. Das zufällige
Fuzzing (2.412 Läufe) fand dieselbe Stelle zweimal. Der gezielte Lauf (12.076 Einzelbyte-Änderungen in 20 Blöcken, Kader, Pool,
Nachrichten, `CED8`) fand keine weitere.

Abhilfe: Den Schlüssel aus dem Rohbetrag bilden und in `TryImport` jede Ausnahme des Imports als Ladefehler melden, nicht nur
eine feste Liste von Typen.

### R14HI-04 (P2) – Feldkarte nennt Felder belegt oder abgeleitet, die der Import nicht übernimmt

Außer den Feldern aus R14HI-01 und R14HI-02 übernimmt der Code (`OriginalImport*.cs`) auch diese Zeilen mit Status B/A nicht. Sie
kommen aus dem Nachspielen oder bleiben auf der Vorgabe. Dabei steht kein `[APPROX]` und keine Zeile unter „Noch nicht übernommen“:

| Zeile der Karte | Gegenbeispiel |
|---|---|
| `NationalCoach._friendlies` B (`D0C1`, `D0C3`) | FILE19: `D0C1` = 1 (Schweden 2:1); Import: 0 Freundschaftsspiele. FILE20 ebenso |
| `ManagerFinance._homeStatistics`, `StadiumStatistics.*` B (`965B+0xED` ff.) | FILE44: Heimspiele `+0xED` = 22, kommt nur aus dem Nachspielen. In der Prosa unter „Weiter offen“ genannt, in der Tabelle als B geführt |
| `Player.Notice` A (`far:8253+0x1E`) | FILE44: ein Poolspieler mit `+0x1E` ≠ 0, nicht übernommen |
| `Player.Trend` A (`5DC3+0x14`) | Der Import setzt nur `TrendControl`, `Trend` nicht |
| `GameState._vacantNationalTactics`, `Tactics._shownAutoEffort/_shownAutoAttack` A (`far:9657+0x26`/`+0x35`) | kein Importcode |
| `NationalCoach.Manager` B (`D0BA`) | [`OriginalImport.National.cs`](H/src/EishockeyManager.Core/OriginalImport.National.cs#L12) sagt selbst: `D0BA` ist an allen Ständen 0, das Amt kommt aus dem Nachspielen |
| `DebCup.Winner` B (`0387`), `EuropeanCup.Winner` B (`0388`) | Beide werden nicht gelesen, sondern aus Paarung bzw. Tabelle gerechnet. Vor dem Endspiel setzt der Import `null`, obwohl `0387` den Vorjahressieger hält (FILE05: 11) |

`OriginalImportFieldMapTests` prüft nur Form und Vollständigkeit der Zeilen, nicht ob der Status zum Code passt. Deshalb bleibt
das unbemerkt. Abhilfe: übernehmen, oder die Zeilen auf U/N mit `[APPROX]` und Grund setzen und unter „Noch nicht übernommen“
aufführen.

### R14HI-05 (P3) – Tests ohne Wirkung auf einzelne Regeln

| Mutation | Test | Ergebnis | Lücke |
|---|---|---|---|
| M03: „nur Weltkugel“, Satz von Land und Managerverein vertauscht | `Import_SetsDateManagersAndClubs` (FILE11), `ClubRecords_MapToDatabaseByName` | grün | Kein Test prüft, welcher Satz das Land und welcher der Managerverein ist |
| M09: `0375[2m]` → `[2m+1]` (0x376 statt 0x375) | `Import_TacticsAndAutoLineup` | grün | Der Test nutzt FILE40 (0/0) und FILE05 (1/1). FILE32/FILE33 (0/1) würden unterscheiden |
| M45: `_selected = index` beim Ausführen entfernt | `SameName_TypedLoadsRemakeSave_ClickOnHokImports` | grün | Fall „Name getippt, dann ein Klick auf das gleichnamige HOK“ fehlt |

### R14HI-06 (P3) – Marker unter der Maus: Fundstelle belegt mehr als angegeben, Spaltengrenze um 1 Pixel versetzt

[`DrawMarker`](H/src/EishockeyManager.UI/Screens/SaveLoad/SaveLoadScreen.cs#L613) markiert als `[APPROX]`, ob das Original leere
Zeilen auslässt, und schreibt dazu „nicht gelesen“. In em.exe steht es: Datei 0x5E2C7–0x5E2DD vergleicht
Spalte · 16 + Zeile + Seitenanfang (`[bp−0x18]`) mit der Zahl der Einträge (`[bp−0x10] + [bp−0x12]`). Nur bei einem
belegten Eintrag wird gezeichnet. Das kann also `[ORIGINAL]` werden.

Die Spaltenwahl weicht leicht ab. Das Original zählt in 0x5E252–0x5E264 die Spalte hoch, solange 0x7A + 80 · k < x gilt. Damit ist
x = 122 noch Spalte 0 und x = 202 noch Spalte 1. Das Remake rechnet `(x − 42) / 80` ([Zeile 622](H/src/EishockeyManager.UI/Screens/SaveLoad/SaveLoadScreen.cs#L622))
und kommt an diesen beiden x-Werten auf die nächste Spalte.

### R14HI-07 (P3) – Dateien außerhalb der Spur im PR nicht genannt

Laut PR: „Keine gemeinsamen Dateien. Außerhalb der Spur: `docs/arbeitspakete.md`.“ Geändert sind aber auch
`UI/Screen.cs` (`InputState.RightDown`), `Desktop/DesktopGame.cs`, `UI/Testing/ScreenDriver.cs`, `Core/CupsDeb.cs`,
`CupsEurope.cs`, `CupsWorld.cs` (je `partial`) und `Tools/Program.cs` (Hilfetext). Die Änderungen sind klein und nötig. AGENTS.md §2.5
verlangt trotzdem, sie im PR zu nennen und zu begründen.

### Erwartet offen – bestätigt

- **#669 (`kader-start`, Schema 42/43):** Die Felder Start-Liga, A046 (Tabellenfeld `8257`+0x2E) und A135 (Tore/Spiele je
  Wettbewerb), Leihe und Transferliste sowie Taktik 375h bildet die Karte nicht auf Schema 43 ab. Das steht im Abschnitt „Stand
  Integration Runde 14“. Head und `main` haben Schema 41, #669 ist nicht gemergt.
- **Inventurzeilen** (Feldkarte/Import A009) → `pruefung-karte`, so im PR genannt.

## 3. Ausgeführte Prüfungen

### `tools/pruefen.sh` (alle drei Läufe, Head `38ebbad`, mit Originaldaten)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2418 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1961 | 0 | 391 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1962 | 456 | 0 |

Ergebnis „bestanden“, SDK 10.0.402. Die Zahlen stimmen mit der PR-Beschreibung überein.

### (1) Gegenprobe je neuem Test

Am Base kompiliert keiner der neuen Tests, weil `OriginalImport`, `OriginalSaveHeader`, `SaveInfo`, `MogelguruScreen` und
`InputState.RightDown` dort fehlen. Deshalb gibt es zusätzlich je Test eine gezielte Mutation im Worktree
(`dotnet test -c Release --filter`, `EM_REQUIRE_GAMEDATA=1`):

| Test | Mutation | rot |
|---|---|---|
| `OriginalImportTests.Import_SetsDateManagersAndClubs` | M01 Tag `03B7 − 0`; M05 Konto ohne /3 | ja |
| `…Import_LeagueTableMatchesOriginalTableRecords` | M02 Ligaergebnisse nicht ersetzt | ja |
| `…Import_AllOriginalStands_AreValid`, `…Import_RoundTripAndContinue`, `…Import_FourSeasonChanges` | M30 Poolspieler ohne ID | ja |
| `…Import_ManagerSquadFromSquadSlots` | M04 Gehalt ohne /3 | ja |
| `OriginalImportRestTests.Import_StocksFromOriginal` | M06 Kurs ohne /3 | ja |
| `…Import_StockHoldings` | M07 Stückzahl +0 statt +4 | ja |
| `…Import_TrainingPlan` | M08 Tag/Einheit vertauscht | ja |
| `…Import_TacticsAndAutoLineup` | M64 Kennzeichen umgekehrt (M09 Versatz: grün, R14HI-05) | ja |
| `…Import_TacticModes` | M10 A6FB statt A707 | ja |
| `…Import_SeriesRecordsAndTableFields` | M11 Serie +1 | ja |
| `…Import_RankSlotsMatchTable` | M12 Halbbytes vertauscht | ja |
| `…Import_Sponsoring` | M13 Angebotsbetrag ohne /3 | ja |
| `…Import_BankAndManagerLoans` | M14 Vorzeichen Kredit | ja |
| `…Import_NationalAccountAndTalentSearch` | M15 Satz 4 statt 5 | ja |
| `…Import_NewMessageFlag` | M16 Kennzeichen immer aus | ja |
| `OriginalImportPoolTests.Import_PoolFromOriginal` | M17 Besitzer 4 → frei | ja |
| `…Import_MarketFromOriginal` | M18 Preis ohne /3 | ja |
| `…Import_MarketListingOfManager` | M19 Verkäufer null | ja |
| `…Import_PoolNamesUnique_AndContinue` | M20 Namensausgleich aus; M30 | ja |
| `…Import_NationalSquadFromOriginal` | M21 Nominierte nicht aufgenommen | ja |
| `…Import_MessagesFromOriginal` | M23 Tag = Schlüssel | ja |
| `…Import_MessageAboutPoolPlayer` | M24 Spieler null | ja |
| `…Import_RoundTripWithPoolMarketNational` | M30 | ja |
| `OriginalImportCupTests.Import_DebCupQuarterFinal`, `…Import_DebCupAllStands` | M25 Tauschregel umgekehrt | ja |
| `…Import_DebCupFinalWithMarks` | M26 Marken 0; M27 Sieger null | ja |
| `…Import_DebCupContinues` | M63 alte Runde nicht entfernt (M25, M27, M58: grün) | ja |
| `…Import_EuropeanCupFromOriginal` | M57 keine deutschen Teilnehmer (M28 `Take(1)`: grün) | ja |
| `…Import_WorldCupFromOriginal` | M29 Viertelfinale `b[k]` statt `b[3−k]` | ja |
| `OriginalImportRunde14Tests.Import_TakesNationalCoachAutoLineupFromBlock0375` | M22 0x37F statt 0x380 (FILE47: 0/1) | ja |
| `…Import_AllStands_RoundTripIsStable` | M30 | ja |
| `OriginalImportFieldMapTests.FieldMap_NamesSchemaVersion` | M35 Überschrift „Schema 40“ | ja |
| `…FieldMap_CoversEverySchemaFieldExactlyOnce` | M33 Feldname geändert | ja |
| `…FieldMap_RowsHaveValidStatusAndSource` | M34 Status „X“ | ja |
| `OriginalImportFieldMapDataTests.ClubRecords_MapToDatabaseByName` | M56 Schlüsselschritt 0xD8 (M03, M31 Namenslänge 8: grün; reiner Datenbeleg) | ja |
| `…LeagueTable_UsesSlotsOfItsLeague`, `…ManagerRecordOffsets_MatchDocumentedValues` | M32 Schlüsselschritt 0xD8 (Datenbeleg, kein Importcode) | ja |
| `OriginalSaveHeaderTests.TryRead_ReturnsYearMatchDayAndManagers` | M39 Weltkugel-Bit umgekehrt | ja |
| `…TryRead_ReadsOnlyTheHeader_NoLengthOrChecksumCheck` | M38 volle Länge verlangt | ja |
| `…TryRead_LimitsManagersToFour` | M36 ohne `Min(…, 4)` | ja |
| `…TryRead_WrongMagicOrTooShort_ReturnsNull` | M37 Kennung nicht geprüft | ja |
| `HokImportAccessTests.List_MarksHokFilesAsOriginal` | M40 HOK als „Other“ | ja |
| `…TryImport_RejectsNonOriginalData_WithoutChecksumFlag` | M42 falscher Fangtyp | ja |
| `…LoadDialog_ImportsHok_ByTypedName` | M43 getippter HOK-Weg aus | ja |
| `…LoadDialog_ImportsHok_ByClickTwice` | M60 HOK beim Laden nicht wählbar (M44: grün) | ja |
| `…SameName_TypedLoadsRemakeSave_ClickOnHokImports` | M44 Auswahlprüfung auf Verzeichnis (M45: grün, R14HI-05) | ja |
| `…ChecksumError_ShowsMogelguru_AndKeepsRunningGame`, `…TryImport_FlagsChecksumError_AndImportsValidSave` | M41 Prüfsummen-Kennzeichen aus | ja |
| `…SaveDialog_DoesNotSelectHok` | M46 HOK auch beim Sichern wählbar | ja |
| `SaveLoadDialogOriginalTests.Paging_OnlyThroughArrowPair_InStepsOfFortyEight_NoEffectAtTheEnds` | M61 ohne Obergrenze; M62 Richtung umgekehrt (M47 doppelter Schritt: grün, durch die Klammer verdeckt) | ja |
| `…Paging_WheelAndKeys_DoNothing` | M48 Mausrad und Bild ab wieder an | ja |
| `…Panel_ShowsNoPageLabel` | M49 „SEITE:“ wieder gezeichnet | ja |
| `…Arrows_AreTheKb4Graphics_AtTheOriginalCallSite` | M50 Pfeil bei (146 \| 166) | ja |
| `…Marker_AppearsLeftOfTheEntryUnderTheMouse_AndIsRemovedWhenLeaving` | M51 Marker x + 1 | ja |
| `…Info_OfARemakeSave_ShowsOnlyWhileTheRightButtonIsHeld` | M52 Info bleibt stehen | ja |
| `…Info_OfAnOriginalSave_ComesFromTheHeader_AsInFile46` | M53 Jahr + 1 | ja |
| `…Info_ShowsNoLeagueInWorldOnlyMode` | M54 Liga immer | ja |
| `…Info_OfAForeignFile_ShowsTheNoSaveGameMessage` | M55 andere Meldung | ja |

Jeder neue Test ist durch mindestens eine Mutation rot. Die grünen Mutationen zeigen die Lücken aus R14HI-05.

### (2) Feldkarte an Original-Ständen, Import aller Stände gegen die DOSBox-Bilder

Eigene Entschlüsselung, Feld für Feld gegen die Notizen und Bilder im Referenz-Repo. Alle Werte stimmen, keine Abweichung:

| Bereich | Feld (Versatz, Breite) | Stände |
|---|---|---|
| Konto | `965B+0x18F` i32 / 3 | FILE01, 03, 07, 08, 12, 31, 44, 46 |
| Eintritt | `965B+0xE4` u8 | FILE01, 02, 12, 15, 29 |
| Sitz-/Stehplätze | `965B+0x133/0x137` i16 | FILE01, 12 |
| Bank, Gegenbuchung | `965B+0x1AB` i32 (−2.103.000), Satz 4 `+0x19B` (+2.103.000) | FILE08, FILE05 (0) |
| Liga-Stufe | `965B+0xEB` | FILE01, 12, 15, 17, 22, 23 |
| Managerverein → Name | `965B+0x14` → `far:9657` | FILE12 (27), 14 (24), 15 (23), 17 (0), 23 (35), 27 (18), 18/21 (Deutschland) |
| Saison-/Tageszähler, Spieltag | `03BD` i16, `03B7` i32, `07A9[0]` | FILE05, 14, 21, 46; FILE01, 13; FILE22, 24, 26, 31, 36, 40 |
| Datum | `5A20`/`5A1C`+1/`5A18` | FILE03, 24, 36, 44, 46 |
| Kader- und Marktgröße | `5DC3` Satz 0 belegt, Satz 4 Poolindex ≠ 0 | FILE05, 12, 15, 26, 35, 37A, 46; FILE05, 12, 26 |
| automatisch aufstellen | `0375[0]` | FILE05 (1), 32 (0), 34 (0) |
| Regler, Spielmodus | `0118`, `D0B9` | FILE01, 09, 12; FILE10, 11, 15, 29 |
| Spielerwerte | `5DC3+0x10…0x12`, `+0x0B`, `+0x28`/3, `+0x2C`/3, Pool `+0x14` | FILE35 KJELLBERG 91 99 47, 32 J., 1 J., 18.800, 400.001; FILE41 VALLIERE 9.999, Platz 25 |
| Aktien | `D0F4+0x48i+0x46` / 3 | FILE01: 84 / 119 / 135 / 147 |
| Nationalkonto | Satz 5 `+0x18F` / 3 | FILE19 1.390.000 = Anzeige nach dem Freundschaftsspiel (Notiz FILE19); FILE18/FILE47 1.000.000 gegen Anzeige 1.500.000 – Unterschied nicht nachgeprüft (Budget DGROUP 0x29C liegt laut Karte in keinem Block) |
| Nachrichten | `D294+1` | FILE20: 217, 221 |
| Stadion | `965B+0x116+2i` u16 | FILE41, 43 (256), 44, 44A (770) |
| Über-/Unterzahl | `965B+0x177` 6 × i16 | FILE36: 12 1 1 24 71 81 |

Import aller 52 Stände über `TryImport` (Saat 1993): Alle laden. Datum, Manager am Zug, Verein, Liga, Konto und Kadergröße
stimmen mit den Notizen und Bildern überein. Die Tabelle stimmt bei FILE03 (Platz 9, 2:4, 9:13) und FILE16A (Platz 1, 4:0, 11:1).
FILE05 Kader und Transferliste gegen `file05_5_transfer.png`: alle 25 Spieler (SP, K/T/F) und alle 7 Marktspieler (Werte, Alter,
Preis) gleich. Kader 26 statt 25 bei FILE26, FILE41 und FILE44: Der vom Manager gelistete Spieler bleibt im Remake-Kader. Das ist
die belegte Entscheidung L7, kein Befund. `KAPUTT.HOK`: „Prüfsumme falsch“.

`[ORIGINAL]`-Fundstellen in em.exe selbst gelesen (`disasm.py`):

- 0x5B661 (Kopf: 10 Byte gegen DGROUP 0x55DF, dann 4/1/1/0xA0)
- Fenster 0x5B73C (71 | 71), 8 × 3 Kacheln, Stil 3
- Textlagen x 72–261, y 81, Linie bis 254, Zeile y 90, Manager x 76/133/212, Liga 0x9EC + 4 · Liga
- Schleife bis zum Mausereignis 2/4 (0x5B9A8)
- Marker 0x5E2C7 ff. (siehe R14HI-06)
- Pfeilpaar 0x5E3B5 (150, 169, 174, 169, 1), Schritt 0x30
- DEB-Pokal 0x387 bei 0x34ED9, `D074` = 2 bei 0x34EFC
- 0x375/0x376 (0x54FCE–0x54FE8, 0x552F9), 0x37F/0x380 (0x55599–0x555C5)
- „SEITE:“ DGROUP 0xBC0: kein Leser

### (3) Fehlerfälle

`emtool hok import` mit leerer, abgeschnittener (5.000 Byte), fremder (Zufall) und um 1 Byte zu langer Datei: Rückgabe 1,
klare Meldung („Spielstand zu kurz“, „Länge … erwartet 47949“, „Keine Kennung „KRON_0.99““), keine Zieldatei. Bei `KAPUTT.HOK` gibt
es Rückgabe 3 mit der Mogelguru-Meldung. Bei fehlender Quelle oder fehlendem Zielordner Rückgabe 2. Wird ein Ziel ohne `--force`
überschrieben: Rückgabe 1.

`TryImport` mit leer, 1 Byte, nur Kopf, abgeschnitten (4 Längen), +1 Byte, Text, Zufall, Remake-`.ems` und kaputter Prüfsumme:
immer `Game = null` mit Meldung.

Fuzzing mit gültigen Prüfsummen: 2.412 zufällige Mehrbyte-Änderungen und 12.076 gezielte Einzelbyte-Änderungen. Bis auf
R14HI-03 keine Ausnahme nach außen. Der Dialog setzt `Host.Game` nur bei Erfolg (Test `ChecksumError_…_KeepsRunningGame`, Code
gelesen). Ein halb geladener Zustand ist nicht möglich, weil der Import immer einen neuen `GameState` baut.

### (4) Pixelvergleich

Abläufe `speichern-original` und `hok-import` am Head gerendert.

| Bild | abweichende Pixel | davon erklärt |
|---|---:|---|
| `a338_2` (Seite 2) | 1.546 (2,0 %) | Einträge grau statt gelb (HOK beim Sichern, im PR genannt), Pfad (Ordner statt `C:\EM`), Titel „Speichern“ (Kanten, unverändert seit Base) |
| `a338_3` (Infofeld) | 1.969 (2,6 %) | wie oben, Pfad in der Infozeile, Mauszeiger; Fenster, Linie, Datumszeile und Managerzeile decken sich |
| `a338_1` (Seite 1) | 5.159 (6,7 %) | die Liste weicht stärker ab (andere Dateien und Farben). Pfeile 2 von 660 Pixeln, Panel bis auf Pfad und Eingabemarke gleich |
| `file05_1` (Hauptmenü FILE05) | 15.183 | Managerkopf (Karte U) und das im Original geöffnete Untermenü Mannschaft. Datum, Gegner, Spieltag, Kurve, Konto und Lampen gleich |
| `kaputt_mogelguru` | 5.900 | `[APPROX]` Lage nach Augenmaß (im Code genannt) |

### (5) Saisonwechsel nach dem Import

Alle 52 Stände, Saat 7. Passiv heißt ohne Eingriff, aktiv heißt Konto je Tag auf mindestens 2 Mio. DM; danach jeweils
`ValidatePlayerIdentity` und Rundreise `SaveGame.Save`/`Load`. Ergebnis: keine Ausnahme, keine Identitätsfehler, jede Rundreise
in Ordnung.

- Aktiv: 4 Saisonwechsel, außer bei Spielende laut Dauer (FILE09/29–31 Dauer 0, FILE28 Dauer 1).
- Passiv: oft „GameOver“ nach 0–3 Wechseln (Entlassung).
- `Import_FourSeasonChanges` (FILE05 passiv/aktiv, FILE16A, FILE11) ist grün.

### AGENTS.md §4 und Dateigrenzen

- Das Öffnen des Laden-Dialogs liest nur die Dateiliste.
- Rechtsklick liest nur den Kopf bzw. den `.ems`-Stand.
- Der Import läuft erst nach dem zweiten Klick oder nach Enter.
- `ReplayTo` ruft `ProcessDay` auf einem neuen Zustand. Das ist kein Vorspulen des laufenden Spiels; als Bauweise des Imports
  akzeptiert.
- Keine Originaldaten im Repo: Tests nur mit `[GameDataFact]`/`TestGameData`, Spielstände aus dem Referenzordner, Abläufe kopieren
  nur in `Path.GetTempPath()`.
- Schema bleibt 41.
- Dateigrenzen: siehe R14HI-07.

### Akzeptierte `[APPROX]`

- Nachspielen statt Zustand für nicht geführte Bereiche (bis auf R14HI-01/-02/-04)
- Saat 1993 im Werkzeug
- Bundestrainer-Automatik aus 0x380 (FILE47 0/1 passt zu „beim Laden wieder eingesetzt“)
- WM-K.-o. 1:0
- Gelistete Managerspieler bleiben im Kader (L7)
- HOK beim Sichern grau
- Mogelguru-Lage
- Managerkopf 0

### Offene Prüfbereiche

- Die Feldkarte hat über 300 Zeilen. Nicht jede Zeile mit Status B/A ist an zwei Ständen einzeln semantisch nachgeprüft. Geprüft
  sind die Bereiche der Tabelle oben. Jugend, Werbeangebote, Kopf-an-Kopf, Rekorde, Platzfelder und Taktikwerte wurden nur gegen den
  Code und die vorhandenen Tests gelesen; dazu gibt es keine DOSBox-Bilder mit Zahlen.
- Ein Bildvergleich von Kader und Tabelle für jeden der 52 Stände fehlt. Verglichen sind die Notizen aller Stände und die Bilder
  von FILE05.
