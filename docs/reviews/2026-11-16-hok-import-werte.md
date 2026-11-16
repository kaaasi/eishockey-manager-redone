# Review PR #892 – Spur `hok-import-werte` (Erstreview)

Base `40f67fcabb0a7b0649f3771d9f00a4ce8d186bea` · Head `62761a3d8ba5e1fd9d72248a3f3a238a8e539573` · Reviewer: Claude Opus 5.5
(frische Sitzung, nicht an #892 beteiligt).

## 1. Merge-Empfehlung

**Ja für `62761a3`.** A607 und A624 sind an `em.exe` nachvollzogen, die Gegenbeispiele bestätigt, die neuen Tests werden
unter Rückschaltung rot, die Rundreise-Regeln sind enger als vorher (keine Abschwächung), die Dateigrenzen sind eingehalten. Offen
sind nur zwei P3 (Wortlaut/Status), die nicht vor dem Merge behoben werden müssen.

Zur Frage „trägt die Rundreise ‚Import verliert nichts‘ (A624)?“: **Nein, die Rundreise allein nicht** – die Rückschaltung auf die
alte Summe +0x03–0x05 lässt alle 166 Rundreise-/Import-Tests grün außer dem synthetischen Test (Probe 2), weil +0x04/+0x05 bei
Torhütern an keinem Stand ≠ 0 sind; Marktplätze (Satz 4) sind für +0x03–0x08 ohnehin per Regel `UnusedSlotOrMarket`
ausgenommen. Getragen wird die Aussage von (a) dem Rohdatenscan `Import_Goalkeeper_GoalsAgainstOnlyInLeagueField` (Sätze 0–4,
alle 52 Stände) und (b) der Disassembly (unten). Das reicht für `[ORIGINAL]`; die Rundreise ist hier nur Rahmen, kein Beleg.

## 2. Befunde

Keine P0–P2.

- **P3-1 Wortlaut „einziger Schreiber“** ([`OriginalImport.cs` Z. 572–576](https://github.com/kaaasi/eishockey-manager-redone/blob/62761a3d8ba5e1fd9d72248a3f3a238a8e539573/src/EishockeyManager.Core/OriginalImport.cs#L572-L576),
  ebenso PR-Text). Beleg: `ndisasm -b16 -e 0x42E8D -o 0x42E8D em.exe` → `0x42E93 mov al,[es:bx+0x1b]` (Pool) … `0x42E9A mov
  [es:bx+0x3],al` (Kaderplatz) und `0x42EB5 mov [es:bx+0x6],al` (Pool +0x1C) im neuen Kaderplatz 0x42CB1. +0x03 hat damit einen
  zweiten Schreiber (Kopie aus Pool +0x1B beim Anlegen, für alle Rollen), dazu Nullsetzen +0x04/+0x05 bei der Leihe (0x47939/0x4793D).
  Die Folgerung (+0x04/+0x05/+0x22–0x27 bei Torhütern nie erhöht, Gegentore = +0x03) bleibt richtig; vorschlagsweise „einziger
  zählender Schreiber; 0x42E9A kopiert beim Anlegen Pool +0x1B“. Kein Code-Fehler.
- **P3-2 Status veraltet** ([`docs/arbeitspakete.md` Z. 74](https://github.com/kaaasi/eishockey-manager-redone/blob/62761a3d8ba5e1fd9d72248a3f3a238a8e539573/docs/arbeitspakete.md#L74),
  PR-Text „Teile“): „A612 blockiert – Entscheidung Nutzer“; entschieden ist R21-E2 = A, ausgelagert nach #896. Nachziehen durch
  die Leitstelle beim Merge genügt.

Erwartet offen – bestätigt: **A612** ausgelagert (#896, R21-E2 = A), nicht geprüft. **#897**: `MatchEngine.Live.CreditLineup`
(Z. 428–430) zählt `goalie.GoalsAgainst += against` ohne Wettbewerbsprüfung, Original nur bei `[0xD0C2] = 7` (0xA3FD);
`TransferMarket.ChangeList` setzt `MonthlySalary = 0` (`Transfer.Pool.cs:828`) statt Wert(Gehalt) (0x497A1).
Ohne Spielwirkung bestätigt: `PlayerLoan.MonthlySalary` wird nirgends gelesen (nur `Loan.Origin`), Kauf setzt das Gehalt neu
(`Transfer.Purchase.cs:57`). **A822** (Rest-`[APPROX]` Verein +0x1D außerhalb der 150 Sätze → 0, `OriginalImport.Market.cs:79`)
als E (Formatgrenze) plausibel.

## 3. Ausgeführte Prüfungen

- Checkout `git rev-parse HEAD` = Head, `git status --porcelain` leer; SDK 10.0.402 (Release `claude-werkzeuge`);
  Originaldaten und -stände aus `kaaasi/eishockey-manager-referenz` (`ls-remote` HEAD `11c3095`). CI auf dem Head: `build` success.
- `tools/pruefen.sh` (alle drei Läufe, Erstreview):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3479 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2428 | 0 | 578 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2429 | 1050 | 0 |

- **Gegenbeispiel Poolangebote** (temporärer Probetest, nicht committet; Import Seed 4711, Satz 4, Pool +0x1A = Markt):
  FILE14 8/8, FILE23 8/8, FILE44A 7/7, FILE46 7/7 Angebote mit +0x0B = 0xFF, alle Gehalt = +0x28 / 3 ≠ 0 (Rest 0), Vertrag 0;
  z. B. FILE14 DENISIUK 2.400 → 800, FILE44A N.SCHOLZ 109.200 → 36.400. FILE05: 7/7 mit +0x0B = 0.
- **Gegenbeispiel Rundreise 52 Stände** (Gegentore +0x03): grün im Volllauf; Rückschaltung siehe Probe 2.
- **Disassembly** (`ndisasm -b16`, Dateiadressen):
  - A607: 0x4970A `call 0x108:0x5C` (= 0x42CB1) mit Argumenten +0x0B = 0, +0x0C = 0, Preis 0, Grenze 0x0A → neuer Platz Vertrag 0;
    0x497A1 `call 0x9D36` (Art 1 = Gehalt) → 0x497BE/0x497C3 `mov [bx+0x5DED/0x5DEB]` = Satz [0xA70E], +0x28/+0x2A;
    0x27500 `dec byte [es:bx+0xB]` (nur bei `[0x1DB] = 0`, `[0xD0B9] ≠ 1`) → 0 − 1 = 0xFF. Lesart „Gehalt +0x28 / 3, Vertrag 0“ trägt.
  - A624: 0xA3FD `cmp byte [0xD0C2],7`/`jnz` → nur Liga; 0xA442/0xA449 +0x0A, +0x15 ≠ 0; 0xA461 Pool +0x18 = 0 (Torhüter);
    0xA46E/0xA475 `inc byte [es:bx+0x3]` bis 0xFF. Torschütze 0x542F4–0x54333: +0x22 + 2·[0xD0BF] (Wort), +0x03 + [0xD0BF].
    Suche aller `inc/add/mov … [es:bx+0x3|0x4|0x5|0x22|0x24|0x26]` im Volllisting: keine weitere Erhöhung in Kaderplätzen (siehe P3-1).
- **Gegenproben** (temporärer Worktree, `--filter HokImportWerteTests|OriginalExportRoundTripTests`, Originaldaten):
  1. Markt-Gehalt nach `ApplySlot` wieder 0 → rot: `Import_MarketListing_KeepsSalaryOfSlot` 52, `Import_File05_Daffner_…` 1,
     `OriginalStand_ExportMatchesOriginal_ExceptAllowedFields` 50.
  2. `GoalsAgainst = goals` (alte Summe) → rot nur `Import_Goalkeeper_IgnoresCupFieldAsGoalsAgainst` (1 von 166).
  `Import_Goalkeeper_GoalsAgainstOnlyInLeagueField` prüft Rohdaten (keine Code-Mutation möglich); als Wächter für künftige Stände ok.
- **Rundreise-Regeln:** alt +0x22–0x27 und +0x28–0x2F ohne Bedingung; neu +0x22–0x27 nur `UnusedSlot` oder `NationalSlots`,
  +0x28–0x2F für Satz 4 nur noch ungenutzte Plätze → strikt enger; `AllowedRules_EachCoversADifference` grün (keine tote Regel).
- **Dateigrenzen:** Code nur `Core/OriginalImport.cs`, `Core/OriginalImport.Market.cs`; neuer Test eigene Datei; fremder Test
  `OriginalExportRoundTripTests.cs` nur zwei Regeln + Hilfsfunktion, im PR begründet; `docs/arbeitspakete.md` nur eigener Status.
- **Marker:** `[ORIGINAL]` A607/A624 mit Fundstellen, oben bestätigt; Rest `[APPROX]` Verein +0x1D (A822) bleibt markiert.

**Akzeptierte `[APPROX]`:** A822 (Verein +0x1D außerhalb der 150 Sätze → 0, an keinem Stand beobachtet).
**Annahmen (nicht geprüft):** Bedeutung von Pool +0x1B/+0x1C nicht nachvollzogen (nicht Teil von A624); 0x27500 nicht über alle
Aufrufer verfolgt (0xFF an allen vier Ständen beobachtet); Mehrsaisonprobe nur über die bestehende Rundreise Teil (2).
