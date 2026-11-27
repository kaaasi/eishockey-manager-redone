# Unabhängiges Review: PR #467 Integration `marker-werkzeug` (Teile 1–2)

Base `f03410f8f06d2d9043788862bb2a2f54c0fff185` (`integration/runde-11-ui-meldungen`, #466) · Head
`924d6e1b6f8ce51ebac3cfc9a19d847eca4018c3` · Reviewer: Claude (neue Sitzung, ohne Beteiligung an #422/#423/#467).
Stand: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer; `gamedata/` verknüpft mit `~/referenz/original/gamedata`
(Referenz-Repo erreichbar); SDK 10.0.402. Ausgeführt, nicht nur gelesen.

## 1. Merge-Empfehlung

**Ja** für genau Head `924d6e1b6f8ce51ebac3cfc9a19d847eca4018c3`. Es ist kein Produktionscode geändert (nur `///`-Kommentare in
`GameOptions.cs`, `MatchEngine.Live.cs` und ein Testkommentar), `tools/pruefen.sh` ist in allen drei Läufen wie erwartet,
alle Gegenbeispiele sind behoben, die nachgetragenen Inventur-Status stützen sich in den Stichproben auf vorhandenen Code
und Tests. Offen sind nur P3-Formulierungen (P3-1 bis P3-3), die mit Teil 3 oder dem Rundenabschluss erledigt werden können.
Merge nur durch Leitstelle/Nutzer nach `zusammenarbeit.md` §6.

## 2. Befunde

Keine P0–P2. Drei P3 (Wortlaut, nicht merge-hindernd):

| ID | Prio | Ort | Kurz |
|---|---|---|---|
| R11I-01 | P3 | `inventur.md` A231/A232/A238 | „umgesetzt“ gilt nur für Managerkader; die Zeilen verweisen auf Konstanten, die für Computervereine `[APPROX]` bleiben |
| R11I-02 | P3 | `arbeitspakete.md` (Abschnitt `marker-werkzeug`) | Vergleichswerte „210 → 246“ sind der Stand der Paketzweige, nicht der Base |
| R11I-03 | P3 | `inventur.md` A111 | „Europapokal nicht“ nur gegen Code und Test, nicht gegen `em.exe` nachgeprüft (Anmerkung, kein Handlungsbedarf) |

**R11I-01 (P3).** [`Transfer.Squads.cs` Z. 23–31](https://github.com/kaaasi/eishockey-manager-redone/blob/924d6e1b6f8ce51ebac3cfc9a19d847eca4018c3/src/EishockeyManager.Core/Transfer.Squads.cs#L23-L31):
`RetireMinAge = 33` und `RetireMaxAge = 37` (die Konstanten, auf die A231/A232 in der Inventur zeigen) sind weiter
`[APPROX]` für die Kader-KI der Computervereine; der ORIGINAL-Wurf ist `RetirementRoll` für Managerkader. Die Statuszeilen
„Stand Runde 11: umgesetzt (PR #459 …)“ sind für Managerkader richtig, lesen sich für die Zeilenkonstanten aber
vollständiger, als der Code ist. Der Code-Kommentar selbst ist ehrlich (Z. 24–27). Vorschlag: „(für Managerkader; Konstanten
der Computervereine bleiben `[APPROX]`)“ ergänzen. Keine Originaltreue wird fälschlich behauptet.

**R11I-02 (P3).** Der Abschnitt nennt „Funktionen umgesetzt 210 → 246, dokumentiert 204 → 172“. `docs/re-abdeckung.md` auf der
Base weist 202/212 aus (210/204 sind die Werte der Zweige #422/#423). Die Endwerte stimmen mit dem erzeugten Dokument und
meiner CSV überein (246/4/172/257). Vorschlag: „(Paketzweige 210; Base 202)“ oder die Base-Werte nennen.

**R11I-03 (P3, Anmerkung).** Die Aussage „Europapokal nicht“ ist im Code und Test belegt
(`MeetingHistory.cs` Z. 126, `WettbewerbeSpiel2Tests.MeetingTable_HoldsLeagueAndDebCup_NotEuropeanCup`); gegen `em.exe`
(0x30852 → 0x50283 nur für Wettbewerb 0 und 1) habe ich es nicht neu disassembliert. Kein Handlungsbedarf.

### Gegenbeispiele erneut ausgeführt

| Befund | Ablauf | Ergebnis |
|---|---|---|
| R11M-01 | `disasm.py gamedata/em.exe 0x3688F 0x36890 --repo . --cache c.pickle` mit Head-Werkzeug → `Status umgesetzt`; Kopie der Werkzeuge mit alter `BARE`-Regel (`(?![\w:])`), Kopie des Caches samt `.key` → `Status dokumentiert` (Schlüssel geändert, Cache neu erzeugt); alte Regel ohne Cache → `dokumentiert` | **behoben**: Cache folgt dem Werkzeugstand (SHA-256 beider Skripte im Schlüssel, [`disasm.py` Z. 35–44](https://github.com/kaaasi/eishockey-manager-redone/blob/924d6e1b6f8ce51ebac3cfc9a19d847eca4018c3/tools/reverse-engineering/disasm.py#L35-L44)) |
| R11M-02 | README Z. 15–17 gelesen | **behoben**: „Beispiel `0xA839 0xA846` und Einzelwerte wie `0xA845 0xA845`“ |
| R11M-03 | `grep` in `inventur.md` nach „Karte erkennt ihn nicht“ / „ohne Datei-Präfix“ | **behoben**: keine Treffer; F016/F030/F069/F070/F080/F093/F157 tragen „Karte erkannte ihn bis Runde 11 Teil 1 nicht, R10N-01“ |
| R10N-01 | `perl -pi -e 's/(0x[0-9A-Fa-f]{4,5}):(?=[^0-9A-Fa-fx]\|$)/$1 :/g'` auf alle `src/**/*.cs` einer Kopie (97 Dateien geändert), `funktionskarte.py gamedata/em.exe --repo <Kopie> --csv` gegen unveränderte Kopie | **0 Zeilen Diff** (680 Zeilen). Gegenprobe mit dem Werkzeug der Base: 95 Diff-Zeilen → die Probe ist scharf |
| `--write` | `funktionskarte.py gamedata/em.exe --repo . --write` am Head | `git status --porcelain` leer: kein Diff |

### Besonders geprüft

1. **Nachgetragene Inventur-Status** (Stichproben gegen Code/Tests, soweit Namen und Aussagen prüfbar):
   A111 (Test `MeetingTable_HoldsPlayoffAndRelegationGames_AfterIntegration`, `SeasonFlow.cs` Z. 248 `RecordMeetings(playoffGames)`;
   Europapokal bewusst nicht, durch Test belegt), A136 (`Player.Value.cs` Z. 212: „[ORIGINAL] Bit 0x80 setzt nur …“), A170
   (`SquadSetup.ManagedSquadSize`, `KaderLigaGroesseTests`), A190 (`Club.ApplyAutoLineup` in `Squad.AutoLineup.cs`, `[APPROX]`
   für den Abrechnungszeitpunkt bleibt ausdrücklich stehen), A209 (`MarketStrengths_NationalTeamFromCountryTable`), A226
   (`Player.ClubGamesCarry`/`GamesForClub`, `KaderTransferTeil5Tests`), A229–A232/A238/A239 (`RetirementRoll`: `random(350, 450) <
   Alter · c / 10`, Torhüter c = 85 stimmt mit [Z. 62–75](https://github.com/kaaasi/eishockey-manager-redone/blob/924d6e1b6f8ce51ebac3cfc9a19d847eca4018c3/src/EishockeyManager.Core/Transfer.Squads.cs#L62-L75);
   `OfferedYears` 71/20/10 von 101 stimmt; `RefuseChance` ist entfallen, Kommentar „keine eigene Absage“ steht
   Z. 135), A256–A258 (`SellerAsked`, `TransferDialog.ForSeller`, `AnswerSeller`, Test `BidOnManagerListing_AsksSeller`), A360/A368/A369
   (`PlayerStatisticsPurchasePriceTests`, `KaderTransferTeil2UiTests`; „Verletzt.(Art)“ in `PlayerHintLine.cs` Z. 143; Strafbank-
   Einordnung von „Gesperrt, n Minuten“ als Nichtumsetzung ehrlich benannt), A381/F109 (`Transfer.AccountBar.cs`,
   `TransferScreen.DrawAccountBar`; „Lage nicht neu vermessen“ steht dabei, keine Pixeltreue behauptet), F002–F004/F013–F015
   (`Transfer.PoolDistribution.cs`, `DistributePoolAtStart`, Goldtest `Start_OriginalDatabase_MatchesFile05Distribution`,
   Mehrsaisonprobe `SeasonChange_RefillsManagersLeague_FourChanges`; F015 ausdrücklich „bewusst nicht nachgebildet“), F005
   (`StrengthenBuyer`; `[APPROX]` für das Mittel a über Register `si` steht im Code Z. 19–23 wie in der Inventur), F006 (25–65 %,
   `random(0, 6) = 0` = Chance 1/7: stimmt mit `SponsorSubsidy` Z. 52–66), F113 (`TrendScore`, `TrendControl`,
   `SaveGame.Migration.KaderTransfer4.cs`). Keine Originaltreue behauptet, die der Code nicht selbst mit Marker trägt.
   Nicht geprüft: die Disassembly-Adressen selbst (0x07169 usw.) – Gegenstand der Fachreviews der Spuren.
2. **Doku-Anschlüsse:** `re-meldungen.md` (Bausteine `OriginalDialog`, `ShortMessage`, `MessageBox.ShowShort`, `InfoFileScreen`,
   `RankCurve`, `RenewalDialog`, Test `OriginalDialogCallersTests`, Zähler DGROUP 0x48FB in `Transfer.PoolDistribution.cs`/Test; K4, K9, K10
   bleiben offen und sind so benannt), `re-geld.md` (G5 `GameState.TransferAccount`/`DrawAccountBar`; G6 → `spielberechnung.md`
   §22 existiert), `reverse-engineering.md` und `spielberechnung.md` §15 (`ComputerClubTransferRules.StrengthenBuyer`;
   Aufruf 0x46FB3 nur bei Verein < 0x40 = `Transfer.Unsolicited.cs` Z. 167–168; Aufruf 0x47483 = `Transfer.cs` Z. 515–516; „ohne
   Zufallszug“ gilt für das Mittel a, die drei Würfe bleiben – so im Code Z. 19–23). Alles stimmt mit dem Code.
3. **KTN-01:** Der Testkommentar nennt jetzt „mehrere Angebote … gemessen bei der Mutation: 8 von 25 Spielern, je eines“. Mutation
   `|| p.ContractYears != 1` in `Transfer.Squads.cs` Z. 161 entfernt (Kopie): `Offer_NeverWithTwoYearsLeft_WholeSquad` rot,
   „8 out of 25 items … did not pass“. Ohne Mutation grün. Kommentar stimmt.
4. **Kein Produktionscode:** `git diff f03410f..HEAD -- src tests` enthält ausschließlich `///`-Zeilen (`GameOptions.cs`,
   `MatchEngine.Live.cs`, `KaderTransferTeil7Tests.cs`). Die Kommentare stimmen mit `CupMatchUsesMinuteLoop`
   (Play-offs/Aufstiegsrunde über `ReportKind.Results`, Index 1/4/7; Pokale über `ShowsCup`) und der WM-Endrunde (`ShowsCup`
   liefert für sie `null`, Rumpf nimmt den schnellen Weg) überein. Dateigrenzen laut Teil 1/2 eingehalten; keine Originaldaten
   oder Auszüge im Diff (`docs/re-abdeckung.md` ist erzeugt und enthält nur Adressen und Zahlen).

### Erwartet offen – bestätigt

Inventur der übrigen Spurbefunde (`geld`, `saisonwechsel`, `wettbewerbe-spiel`, `ui-meldungen`) steht in `inventur.md` ausdrücklich
als „noch nicht Zeile für Zeile übertragen (Rundenabschluss)“; Teil 3 (Marker ohne Codeänderung) ist als eigener Lauf angekündigt.
Beides ist so dokumentiert, nichts wird als erledigt dargestellt.

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh` (alle drei Läufe, Build mit Warnungen als Fehler) am Head `924d6e1`, Ergebnis „bestanden“:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1973 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1662 | 0 | 279 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1663 | 310 | 0 |

Weitere Befehle: `funktionskarte.py gamedata/em.exe --repo . --csv` (680 Zeilen; Status umgesetzt 246, dokumentiert 172, genähert 4,
unbekannt 257 = Tabelle in `re-abdeckung.md`), `--write` ohne Diff, Mutation `perl -pi` (R10N-01), Werkzeugkopie mit alter
`BARE`-Regel (R11M-01), Mutation `ContractYears != 1` (KTN-01) – jeweils in Kopien/Worktrees, der Checkout blieb sauber.

**Akzeptierte `[APPROX]`:** keine neuen; die in den Inventurzeilen genannten (F005 Register `si`, A190 Abrechnungszeitpunkt, A381 Lage
der Kontoleiste, Verteilung der Computervereinskader) sind im Code bereits markiert und werden unverändert akzeptiert.
**Annahmen:** Disassembly-Adressen der nachgetragenen Zeilen nicht neu geprüft; von den ~40 Inventurzeilen sind etwa 30 gegen
Code/Tests geprüft (Namen, Aussagen, Marker), der Rest (F109, A238/A239-Details, A368-Anzeige) nur gegen die Verweise auf PR/Test.
