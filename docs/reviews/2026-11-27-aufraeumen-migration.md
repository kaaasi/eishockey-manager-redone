# Review PR #1059 – Spur aufraeumen-migration (nur Schema 65 ladbar, Migrationen entfernt)

Reviewer: Claude Opus 5.5 (frische Sitzung, geplanter Lauf, keine Mitarbeit am PR). Geprüft: Base `b0a548d`, Head `29eb97d`
(`git rev-parse HEAD` = `29eb97d353ca7db4fefccfffc2d5a0db7f5c5a61`, `git status --porcelain` leer, SDK 10.0.402, Originaldaten
aus `kaaasi/eishockey-manager-referenz` `original/`). Erstreview: alle drei Läufe von `tools/pruefen.sh`.

## 1. Merge-Empfehlung

**Ja für `29eb97d`.** Der Ladeweg lehnt jedes Schema < 65 mit der geforderten Meldung ab, Schema 65 lädt unverändert. Der
Zustandsabdruck über 4 Saisonwechsel (passiv/aktiv) ist auf Base und Head bytegleich, ohne Ausnahmefelder. Die Markerprobe zeigt
nur die erwarteten 48 entfallenen IDs. Die Testliste ändert sich nur bei Migrationsfällen, den 8 Umbenennungen und dem neuen
Lader-Test. Außerhalb des Ladewegs ändert sich Code nur an A221/A222/A392; deren Rückfälle erreicht kein Weg einer neuen Partie
(Mutationsprobe unten). Es bleibt ein P3 zur Begründungstabelle, kein Merge-Hindernis.

## 2. Befunde

### P3-1 – A416/A710 `SeatManagerSquads`: Begründung „auch Managerwechsel zu einem Computerverein“ ohne Aufrufer

Die Tabelle im PR behält A416/A710 mit „auch Managerwechsel zu einem Computerverein“. Auf dem Head hat
[`TransferMarket.SeatManagerSquads`](https://github.com/kaaasi/eishockey-manager-redone/blob/29eb97d353ca7db4fefccfffc2d5a0db7f5c5a61/src/EishockeyManager.Core/Transfer.Pool.cs#L568)
(samt `SeatLoosePlayers`, Marker A416 Zeile 562, A710 Zeile 625) **keinen Aufrufer im Produktcode** mehr. Die beiden einzigen
Aufrufer auf Base waren `AfterLoadPool` (Migration) und der entfernte Rückfall A221/A222 in `ChangeList`.
`GameState.AddManager.cs:234` (Managerwechsel) setzt nur `slot.Manager = -1` und ruft die Methode nicht.

Beleg: `git grep -n 'SeatManagerSquads(' 29eb97d -- src tests | grep -v '///'` → nur die Definition
`Transfer.Pool.cs:568` und der Test `TransferVertraegeKomplettTeil5Tests.cs:329` (`Layout_IsFlexible_OriginalAnd400`). Auf Base:
zusätzlich `SaveGame.Migration.Pool.cs:41` und `Transfer.Pool.cs:943`.

Wirkung: keine Verhaltensänderung. Das Behalten deckt Kriterium (b) (ein Testaufbau braucht es). Die Begründung in der Tabelle
und der `[APPROX]`-Text („Spielstände vor dieser Spur, Managerwechsel …“) beschreiben aber einen Weg, den es nicht gibt.
Vorschlag: Begründung im PR auf „nur Testaufbau“ berichtigen und A416/A710 an `aufraeumen-tot` weitergeben (Test über
`SeatStartSquads` bzw. Spielstart umstellen, dann Methode und Marker entfernen). Kein Fix in diesem PR nötig.

### Stichproben ohne Befund

- **A221/A222/A392 wirken nur auf Altstände.** `ChangeList` laufen nur `Initialize` (baut den Pool vorher, falls leer),
  `OpenStartMarket` (nach `ReservePool` in `NewGame.Create`) und der Import (`ImportOriginal` übernimmt die 200 Sätze aus
  `far:8253`) voraus. `IsFreeAgent = true` entsteht nur in `Release` (`Transfer.cs:236`) und landet nur in `_freeAgents`. Alle
  drei `_listings.Add` (`Transfer.cs:213/604`, `Transfer.Pool.cs:989`) setzen es nicht. Mutationsprobe M2/M3 unten: kein Treffer.
- **`AfterLoadPool` ohne Wirkung für Schema 65.** Base `SaveGame.Migration.Pool.cs:39`: `if (loadedSchema >= PoolSeatedSchema /* 49 */) return;`.
  `BindLineupOwners` bleibt im Reader; die Methode liegt wie im neuen Kommentar in `Squad.AutoLineup.Manager.cs:58`.
- **Behaltene Rückfälle (Stichproben):** A429/`_youthOfDayDone` (Zustandsfelder, Schema-Abdruck bliebe sonst nicht 65),
  A213 (`CaptureDatabaseNames` aus `ReservePool`/Import, `Transfer.Pool.cs:326/341`), A884 (Import nach dem 29. April),
  Satzfolge `NextLeagueOrder`. Die Satzfolge wird von synthetischen Aufbauten erreicht: Mutation M1 macht 7 Fälle von
  `FinanceCalibrationTests.Premium_DependsOnTitlesAndPromotion_NotEstimatedTableAwards` rot. Der entfallene Test
  `FairPlayWinner_WithoutFormedSlots_FallsBackToIndexOrder` lud Schema 37; der Rückfall bleibt also abgedeckt (ohne
  Assertion auf die Gleichstandsfolge, vertretbar).
- **Keine Verhaltensänderung außerhalb des Ladewegs.** Der Codediff in `src/` außerhalb von `SaveGame.Migration*` besteht nur aus
  Kommentaren, `SaveGame.Reader.cs`, `SaveGame.cs` (Wörterbuch) und den drei Stellen in `Transfer.Pool.cs`. Die 60 gelöschten
  Dateien enthielten nur `private` Member.

### Erwartet offen (bestätigt)

- **R27-D2 = B:** Schema < 65 lädt nicht. Probe auf dem Head: Schema 64, 22 und 1 → `Success=False`, `WrongVersion=True`,
  „Spielstände mit Schema n können nicht mehr geladen werden.“
- **A6 = A:** `docs/` nennt noch alte Dateinamen, z. B. `docs/analyse/restliste-v1.md`, `inventur.md`,
  `abweichungen-bewertung-chatgpt.md`: 197 Treffer `SaveGame.Migration` in `docs/`, Bereinigung in `aufraeumen-doku`.

## 3. Ausgeführte Prüfungen

**`tools/pruefen.sh`** (ohne Option, Head `29eb97d`, Ergebnis: bestanden):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4005 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2813 | 0 | 673 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2814 | 1191 | 0 |

**Gegenbeispiele** (je eigener Worktree für Base und Head, Originaldaten):

1. **Zustandsabdruck:** Harness aus der PR-Beschreibung unverändert (`Zz_MigrationHarness`, nur Ausgabepfad). Ergebnis: alle 8
   Abdruckdateien `fp-{passiv,aktiv}-{0..3}.txt` Base = Head per `cmp`. Hashes passiv `427150EB075036B9 D8DC90CA9CBDE043
   19881337F32AFDA7 1FD1AA5409D04D4E`, aktiv `D9D7F4C45B5A87CB F77B150CD583D032 389D013F6D9496BA BBCD733F833309A6`, wie im PR.
   Es gibt keine abweichenden Felder.
2. **Rundreise Speichern → Laden, Schema 65:** im aktiven Lauf je Saisonwechsel „rundreise gleich“ (Base und Head).
3. **Schema 64:** eigene Probe (Stand von `TestGames.Create()`, nur `"schema"` ersetzt). Head: 64 → Meldung „Spielstände mit
   Schema 64 können nicht mehr geladen werden.“ (`WrongVersion`), 65 lädt. Base: 64, 22 und 1 laden (`Success=True`). Damit ist
   `SaveGameTests.Schema64_CanNoLongerBeLoaded` auf Base rot (Gegenprobe zum neuen Test).
4. **`markerliste.py` ohne `--write`:** 543 → 495 Markerzeilen, 491 → 539 entfallene IDs (+48), keine neue ID. Die Fundstelle laut
   Base-Liste: 45 IDs in `SaveGame.Migration*.cs`, dazu A221/A222/A392 in `Transfer.Pool.cs:931/939/1014`. Die Liste stimmt mit
   dem PR überein.

**Testliste** (`dotnet test EishockeyManager.slnx --list-tests`): 4066 → 3992, 83 weg, 9 neu. Neu sind die 8 Umbenennungen aus
dem PR und `SaveGameTests.Schema64_CanNoLongerBeLoaded`. Alle 83 entfallenen Fälle laden laut Quelltext ein Schema < 65 oder eine
Fixture. Die beiden ohne Schema im Namen (`KaderRestSaisonendeTests.SaveGame_Before50_…`, `SaisonSatzfolgeSaisonendeTests.FairPlayWinner_…`)
setzen `schema` 49 bzw. 37. `PruefungKarteRunde16Tests.TwoManagers_SquadLoss_OnlyExpiringContracts` baut den Aufbau der Fixture
neu. Die R25-G1-Proben laufen jetzt mit Schema 65 durch den Reader; je eine Probe mit 64 endet in der Versionsmeldung.

**Mutationsprobe** (temporär auf dem Head, danach verworfen; `EM_REQUIRE_GAMEDATA=1 dotnet test EishockeyManager.slnx`):

- M1: Indexfolge in `NextLeagueOrder` durch `throw` ersetzt → 7 rot (siehe oben).
- M2: `if (_pool.Count == 0) throw` am Anfang von `ChangeList`.
- M3: `IsHeld` wirft bei `IsFreeAgent`.

Für M2 und M3 gab es **0 Treffer** in Core (2882) und UI (920): Kein Testaufbau und keine neue Partie erreicht die entfernten
Rückfälle.

**Akzeptierte `[APPROX]`:** keine neuen. Entfallen sind nur Marker der gelöschten Migrationen und A221/A222/A392.
**Annahmen** (nicht geprüft): Schema-65-Stände, die per Migration aus Partien vor Teil 5 bzw. Runde 15 entstanden und seither
keinen Listenwechsel hatten, verlieren die Rückfälle (Restlücke im PR). Echte Altstände der Nutzer wurden nicht geladen. Auch
der Laden-Bildschirm wurde nicht geprüft, nur `SaveGame.Load`.
