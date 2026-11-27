# Review PR #753 – Spur pruefung-karte Teil 1 (Testlücken, Restpunkte Welle 2)

Geprüft: Head `8e738e4eaf6e41a0030b03c21a26972d6572cd3e` (`git rev-parse` am PR-Head), Base `007fcbf8b076e5c42b51563d254fbfff8b671354`
(= Merge-Basis mit `main`), Arbeitsbaum sauber. Reviewer: Claude Code (Opus 5.5), geplanter Lauf, eigene Sitzung ohne
Implementierungskontext. Mutationen nur in einem temporären Worktree, danach verworfen.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Alle drei Läufe von `tools/pruefen.sh` grün, jede der zwölf erneut ausgeführten Gegenproben wird rot
(R725-03 als toter Zweig bestätigt), der einzige Produktfix `TransferScreen.cs` stimmt mit em.exe Datei 0x46693 und mit dem
DOSBox-Bild `file40_03`–`05` überein, die Kommentare in `GameState.LoadQuirk.cs` und `Season.cs` (neues `[APPROX]` R701-04)
rechnen nach. Einziger Befund P3 (Sollwert-Abdruck im Sponsortest, Aussage der PR-Beschreibung zu `SponsorBewertungTests` trifft
nicht zu) blockiert nicht.

## 2. Befunde

**R753-01 (P3) – `SponsorOfferTests.Rating_FreshClub_…`: (14, 69) ist ein Abdruck des Heads, nicht unabhängig hergeleitet.**
[SponsorOfferTests.cs:73](https://github.com/kaaasi/eishockey-manager-redone/blob/8e738e4eaf6e41a0030b03c21a26972d6572cd3e/tests/EishockeyManager.Core.Tests/SponsorOfferTests.cs#L73).
Die PR-Beschreibung sagt, „die Herleitung je Summand prüft `SponsorBewertungTests`“. Beleg dagegen: Mutation
`10 * (2 - tier)` → `10 * tier` in `FinanceSponsorRating.cs:125`, dann
`dotnet test tests/EishockeyManager.Core.Tests --filter FullyQualifiedName~SponsorBewertungTests` → **11/11 grün**; rot werden nur
drei `SponsorOfferTests` (darunter der neue Abdruck). Der Schlussterm selbst ist richtig: em.exe Datei 0x3FAC5 ff. rechnet
`s / 12` (Divisor 0x0C, 0x3FADF) und `10 · (2 − Liga)` (Liga aus `[bx−0x68BA]`, `mov ax,2; sub ax,dx`, Faktor 0x0A, 0x3FAFE–0x3FB0E).
Die Lücke ist also nur schwach geschlossen: ein Abdruck, der bei jeder legitimen Änderung eines anderen Summanden mitgezogen
werden muss. Vorschlag (nicht blockierend): in `SponsorBewertungTests` einen Fall für `Finish(score, tier)` mit Hand-Sollwerten
je Liga (z. B. S = −12 → Oberliga 53 laut Doc-Kommentar) ergänzen und die PR-Aussage berichtigen.

**Hinweis (kein Befund):** Mit dem toten Zweig in `Day_MainPathOnly_…_EventDayHaltShowsLineupForm`
([NationalteamRunde14Tests.cs:88](https://github.com/kaaasi/eishockey-manager-redone/blob/8e738e4eaf6e41a0030b03c21a26972d6572cd3e/tests/EishockeyManager.Core.Tests/NationalteamRunde14Tests.cs#L88))
fällt auch die Prüfung #688 R688-08 (Training im Takt vor 0x952C) weg; sie lief nie (Zweig tot, `Assert.Fail` dort grün) und
`grep -rn R688-08 tests/` findet sonst nichts. Ob R688-08 anderswo wirksam abgesichert ist, wäre in Teil 2 (Inventur) zu notieren.

## 3. Ausgeführte Prüfungen

Einrichtung: `tools/claude/setup-workspace.sh ~/referenz/original/gamedata`, `dotnet --version` = 10.0.402,
`git ls-remote …/eishockey-manager-referenz HEAD` erreichbar. `tools/pruefen.sh` (alle drei Läufe) am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2831 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2124 | 0 | 477 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2125 | 706 | 0 |

Gegenproben (je eine Ersetzung im Worktree, `EM_REQUIRE_GAMEDATA=1 dotnet test <projekt> --filter …`, danach zurückgesetzt):

| Befund | Mutation | Ergebnis |
|---|---|---|
| R725-03 | – (Zweig enthält am Head `Assert.Fail`) | Test grün → Zweig tot, bestätigt |
| N33 | `DriftTeamValues(…Weekly)` in `OpenClubSeasonDay` aus | `Day_MainPath_SmallDrift_…` 2/2 rot |
| R14G4-06 (#716, N25) | `UpdateNationalTeamValues()` in `WorldCupLoopStepA` aus | `Mode4_LaterWorldCupDay_StepA_…` 2/2 rot |
| #620 (R13GN-03) | `MixLeagueSlots()` im Rückfallweg aus | `Schema37Save_…StartNextSeasonFormsSlots` rot |
| #620 | `10 * (2 - tier)` → `10 * tier` | `Rating_FreshClub_…` rot (s. R753-01) |
| R701-02 (#620) | `NationalCoachPlaysOn … && MatchOn(day, Nation) is not null` | `CoachWarning_Boundary(10, False)` rot |
| R701-01 | Fix zurück (`EntryLine(s, 36, …)`) | `TransferList_HeaderLine_…` rot |
| R701-03 M07 | Schritt (c) mit `DayCounter.Of(previous)` | `File44_WorldCupLoop_…` rot |
| R701-03 M08 | `HealManagerSquads` mit `DayCounter.Of` | grün – erwartet offen, bestätigt |
| R14G2-02 (#716) | `Managers[0].Money++` im Konstruktor `SeasonEndScreen` | `SeasonEndScreens_…` rot |
| R14G3-01 (#716) | `StoreRankSlot` im Import aus | `Import_RankSlotTakenFromHok` rot |
| R14G3-02 (#716) | `StartNextSeason` → sofort `GameOver` | `Import_FourSeasonChanges` 4/4 rot (auch passiv) |
| R14G4-08 (#716) | `MaxDressed` 23 | `Lineup_RefusesTwentyThirdDressed` rot |
| R14G5-01 (#716) | `CarryGamesForClub()` aus | `SeasonEnd_Compensation_KeepsGamesForClub` rot |

Abgleich mit dem Original:

- **R701-01:** `ndisasm -b16 -e 0x46670 em.exe`: Datei 0x46693 `push 0x1F, 0x24, 0x139, 0x24, 0xB4; call 0x30:0x995` → Linie
  x 180–313, y 36, Farbe 0x1F; Datei 0x46AFE (Entmarkieren des alten Eintrags) `0xB4 … 0x13A`, y = 16·i + 0x24 → 180–314. Bilder
  `original/spielstaende/bilder/file40_03`, `_04`, `_05` (642 × 507, Spiel doppelt ab (1 | 26)): Zeile 36 gelb von x 180 bis 313,
  Eintragslinie Zeile 52 von 181 bis 313. Code ([TransferScreen.cs:423](https://github.com/kaaasi/eishockey-manager-redone/blob/8e738e4eaf6e41a0030b03c21a26972d6572cd3e/src/EishockeyManager.UI/Screens/Team/TransferScreen.cs#L423))
  `HLine(180, 36, 134)` bzw. nach Markierung `EntryLine(…, repainted)` 180–314: stimmt.
- **R14G4-08:** Datei 0x40E4D `cmp al,0x16` → fester Sollwert 22 belegt.
- **#620 Sponsor:** Datei 0x3FAC5–0x3FB0E wie unter R753-01.
- **R701-03 (`GameState.LoadQuirk.cs`):** `SavedOnMarkerDay` lässt nur Tag 216 (= `ClubSeasonEnd` + 1) zu; `Counter(216)` =
  217 + 1 = 218, 218 mod 7 = 1 → Zweig nicht erreichbar, Kommentar stimmt.
- **R701-04 (`Season.cs`):** `DateText` = `Format(Day + LoadCounterOffset)`; Halt Tag 230 mit Versatz → `Format(231 = SeasonEnd)`
  → Wochentag + 1 („Montag“), Original (Zähler 232 + 5) mod 7 = 6 = Sonntag (Tabelle ab Montag, da 233 → 0 = Montag laut A158).
  Abweichung richtig beschrieben, `[APPROX]` angemessen.

Erwartet offen, bestätigt: #699 Punkt 2 im Modus „nur Weltkugel“ (`AbgleichOriginalPaareTests` `DayZero = 699`, FILE20→21);
#703 R701-06 (nur Hinweis); R701-03 M08 (Mutation oben grün); Teil 2 (Inventur/Karte) folgt.

**Akzeptierte `[APPROX]`:** R701-04 (Wochentag am Saisonende am Tag statt am Zähler; mit FILE22/FILE44 nicht erreichbar).
**Annahmen (nicht selbst geprüft):** dass FILE22 nach dem Import ebenfalls erst am Saisonende hält (für FILE44 durch den neuen
Test belegt); die übrigen Tabellenzeilen des PR (R14G2-01, R14G3-03, R14G4-01/-02/-03/-04/-05/-07) nicht erneut mutiert.
