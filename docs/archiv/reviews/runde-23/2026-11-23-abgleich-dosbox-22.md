# Review PR #964 (Spur abgleich-dosbox-22: A427, A065, A057, A402, A613, A614, A629, A117, 0xA70D)

Reviewer: Claude Code (Opus 5.5), eigene Sitzung ohne Implementierungskontext, geplanter Lauf. Geprüft: Head
`bd4c45a7270d294f55429cb0315d568f3fe5286d` gegen Base `f147211` (= aktueller `main`), Checkout `git rev-parse HEAD` = Head,
`git status --porcelain` leer, SDK 10.0.402. Referenz-Repo erreichbar (`git ls-remote` → `7412a501`).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Alle Klassenvorschläge des Berichts sind an FILE75/FILE76 und em.exe nachvollzogen, jede
Korrektur hat einen Regressionstest, der mit dem alten Code rot wird (Rückschaltprobe je Punkt, unten), alle drei Läufe von
`tools/pruefen.sh` sind grün. Keine Spielzustandsänderung durch bloßes Öffnen, keine neuen Buchungen (`Book`) im Diff. Offen
bleiben nur drei P3 (zwei Testlücken, eine Markerbegründung) – kein eigener Lauf nötig (`zusammenarbeit.md` §5 Punkt 5).

## 2. Befunde

**AD22-01 (P3) – A065: Test deckt nur Tabellenfeld 0 ab.** Mutation
[`Highscore.cs:385`](https://github.com/kaaasi/eishockey-manager-redone/blob/bd4c45a7270d294f55429cb0315d568f3fe5286d/src/EishockeyManager.Core/Highscore.cs#L385)
`: club.StoredTableRank` → `: 0` bleibt grün (Filter `AbgleichDosbox22Tests|OriginalExportRoundTripTests|OriginalImportRestTests|Highscore`:
143/143 bestanden). FILE75 hat überall 0, der Fall „Endstand der Vorsaison vor dem 1. Spieltag“ ist ungetestet. Vorschlag:
synthetischer Test mit `StoreTableRank(k ≠ 0)` und `PlayedMatchDays = 0` → `Rank0 = k`.

**AD22-02 (P3) – A614: Vertragsnummer im Gruppierschlüssel ungetestet.** Mutation
[`OriginalImport.Rest.cs:201`](https://github.com/kaaasi/eishockey-manager-redone/blob/bd4c45a7270d294f55429cb0315d568f3fe5286d/src/EishockeyManager.Core/OriginalImport.Rest.cs#L201)
`c[4]` → `-1` (Nummer aus dem Schlüssel) bleibt grün (143/143). Zwei Bandenverträge desselben Partners mit gleichem Betrag und
gleicher Restlaufzeit, aber verschiedenen Nummern, würden wieder zu einem Vertrag verschmolzen – genau der Fall, den die neue
Nummer unterscheiden soll. Vorschlag: `Import_Sponsoring`-Variante mit zwei solchen Verträgen (Nummern 1 und 2). Die
Sortierung nach Nummer ist abgedeckt (Mutation „ohne `OrderBy`“ → 2 rot).

**AD22-03 (P3, Markerbegründung) – `Transfer.Pool.cs:972` `[ORIGINAL]` für das Level aus 0xA70D.**
[`Transfer.Pool.cs:972`](https://github.com/kaaasi/eishockey-manager-redone/blob/bd4c45a7270d294f55429cb0315d568f3fe5286d/src/EishockeyManager.Core/Transfer.Pool.cs#L972)
begründet die Gleichheit zur Laufzeit mit 0xA70D = 0xA70E an den Spielständen. Beleg, dass das nicht reicht:
`python3 tools/reverse-engineering/disasm.py em.exe --xref 0xA70D 0xA70D` → rund 25 Schreiber (u. a. 0x2756E, 0x298D1,
0x29B69, 0x54E5D–0x564F2); das Hauptmenü setzt 0x44EB1 `mov al,[0xa70e]` / `mov [0xa70d],al` – das erklärt die Gleichheit an
allen Ständen (gesichert wird aus dem Hauptmenü). Für den Leser 0x49812 ist damit nur belegt, dass er im Managerzug nach dem
Hauptmenü und beim Spielstart (0x5B0CF, Schleife über 0xA70E) gleich ist; Aufrufe aus anderen Abläufen sind nicht geprüft.
Keine Verhaltensabweichung belegt; Vorschlag: Begründung auf „0x44EB1 setzt 0xA70D = 0xA70E je Hauptmenü“ umstellen. Die
Export-Entscheidung 0xA70D = 0xA70E selbst ist damit zusätzlich gestützt (nach dem Laden überschreibt das Hauptmenü den Wert).

Bestätigt (erwartet offen): `[APPROX]` Bankseite als Summe (Probe: an keinem Stand mehr als ein Manager mit Bankeintrag),
Vertragsnummern-Folge 1 … n, +4 = Partner bei Nicht-Banden, Leihe von Computerverein (Besitzer = Entleiher), A117 Kern 1 DM;
A629 Leihende und A065-Gegenprobe im Bild brauchen weitere Aufnahmen; 0xA70D = 0xA70E ist Entscheidung der Leitstelle
([Kommentar](https://github.com/kaaasi/eishockey-manager-redone/pull/964#issuecomment-7096940685)).

## 3. Ausgeführte Prüfungen

**Gegenbeispiele / Rückschaltproben** (Arbeitskopie des Heads, Datei(en) auf Base zurückgesetzt, `EM_REQUIRE_GAMEDATA=1`,
Filter `AbgleichDosbox22Tests|OriginalExportRoundTripTests|OriginalImportRestTests.Import_Sponsoring`, 72 Tests):

| Punkt | zurückgesetzt | rot |
|---|---|---|
| alles | `src/` = Base | 11 (6 eigene Fälle inkl. Theorie, `Import_Sponsoring`, Rundreise FILE75/FILE08/FILE76, `AllowedRules_EachCoversADifference`) |
| A065 | `Highscore.cs` | `File75_HighscoreBeforeFirstMatchDay_UsesTableField` |
| A402 | `OriginalExport.Managers.cs` | `Export_BankRecordAccount_LikeOriginal` (FILE08, FILE76), Rundreise FILE08/FILE76 |
| A614 Import | `OriginalImport.Rest.cs` | `Import_Sponsoring` (fremder Test), `File76_BoardContracts…`, Rundreise FILE76 |
| A614 Export | `OriginalExport.Rest.cs` (+ alte 0x17C-Zeile) | `File76_BoardContracts…`, Rundreise FILE76 |
| A629 | `OriginalExport.Squads.cs` | `File76_LoanedPlayer_PoolOwnerIsLender`, Rundreise FILE76 |
| 0xA70D | Zeile auf `Managerzahl − 1` | `File75_Export_TurnByteA70D_EqualsA70E`, Rundreise FILE75, `AllowedRules…` (Regel A70D ungenutzt) |
| A117 | `ContractDialog.cs` | `AbgleichDosbox22UiTests.ContractDialog_SalaryZero_ShowsZeroDm` |

Mit Head alle grün; Rundreise FILE75/FILE76 (`OriginalExportRoundTripTests`) ohne Abweichung außerhalb der Regeln.

**Bericht `docs/analyse/abgleich-dosbox-22.md` gegen Quellen** (eigene Probe über `OriginalSaveGame.Read`, alle 81 `.HOK` in
`original/spielstaende/`):
- 0xA70D ≠ 0xA70E an **keinem** Stand; FILE75 0/0.
- Satz 4 +0x18F = Σ Satz m +0x19B + 16 an allen Ständen; ≠ 0 nur FILE08 (−2.103.000) und FILE76 (+300.000), je ein Manager.
- C15B-Verträge nur in FILE76: Flächen 4/5 `+4 = 2, +7 = 0`, Fläche 6 `+4 = 1, +7 = 1`, 0x17C = 3 (sonst überall 1).
- FILE76 Pool 100 `+0x1A = 0`, `+0x1D = 23`; FILE75 Poolbesitzer 0/1/4/6 = 22/22/7/148 (Bericht: 149 – Zählweise, ohne Belang).
- em.exe: 0x2BB5B `mov al,[0x17c]` → `mov [bx+0xC15F],al` (+4 der Fläche), 0x2BB80 `inc byte [0x17c]` nach der Bandenschleife;
  0x5D2CA–0x5D303: Verein = Manager +0x14, Feld `[0x8288 + 0x50·Verein]`, Versatz `[0x7C0 + Manager +0xEB]`, `0x24 − (… + 1)` ✓.
- Bilder: `file76_11` zeigt „Gehalt / Monat : 0 DM“, `FormatMoney(0)` = „0 DM“ ✓; `file76_03` zwei JWE-Banden + dritte Bande ✓.

**Finanzen:** Diff ohne `Book`, `Random`, `Continue`; Sparbuch/Darlehen nur im Import (A613, unverändert, Test
`File76_SavingsAndManagerLoan_ImportLikeOriginal` grün). `ContractDialog`: `_salaryZero` nur Anzeige, Konstruktor ändert
nichts am Spielzustand.

**`tools/pruefen.sh`** (alle drei Läufe, Commit `bd4c45a`, SDK 10.0.402, Ergebnis: bestanden):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3844 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2709 | 0 | 625 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2710 | 1134 | 0 |

CI auf dem Head: `build` success.

**Akzeptierte `[APPROX]`:** Bankseite als Summe; Vertragsnummern 1 … n in Vertragsfolge; +4 bei Trikot/Fernsehen/Eis = Partner;
Leihe von Computerverein → Besitzer = Entleiher; A117 Kern 1 DM (Ergebnis „nicht interessiert“ gleich); A057 entlassener erster
Manager; A065 Weltmodus Platzterm 0. **Annahmen:** Bilder nur gesichtet, kein Pixelvergleich (Änderung A117 ist reiner Text,
UI-Test vergleicht mit dem Fenster „Gehalt 0“).
