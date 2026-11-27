# Review PR #791 – Spur kader-rest (Runde 17), Erstreview

- PR: #791 · Base `e1476e65e721a2266752949d798e2d1539194e61` (Merge von `main` `a62ab21` im Branch) · Head
  `d72afd7b95dbd6c6aaa0e631a576c3cf43b71715` · Reviewer: Claude Opus 5.5, geplanter Lauf, eigene Sitzung (nicht an #791 beteiligt).
- Geprüft: `git rev-parse HEAD` = Head, `git status --porcelain` leer; Diff gegen die Merge-Basis `a62ab21` (48 Dateien).

## 1. Merge-Empfehlung

**Ja** für genau `d72afd7`, unter dem schon vorgesehenen Vorbehalt: vor dem Merge Rebase per Fixlauf. Der Branch enthält einen
Merge-Commit von `main` (`731813a`) statt Rebase (nur vermerkt) und ist gegen den aktuellen `main` `3cd9169` **nicht konfliktfrei**:
`git merge-tree --write-tree origin/main d72afd7` → `CONFLICT (content): Merge conflict in docs/reverse-engineering.md` (nur Doku,
#793 re-livespiel-2). Alle Gegenbeispiele der Spur sind per Rückmutation rot, die drei Läufe grün, die angepassten fremden Tests
sind begründet und per Rückschaltprobe belegt. Offen bleiben nur zwei P3.

## 2. Befunde

**P3-1 – Abweichende Poolform überschreibt die frisch gewürfelte Form (latent).** `Player.AdoptPoolForm` läuft in
[`PlayerFactory.Join`](https://github.com/kaaasi/eishockey-manager-redone/blob/d72afd7b95dbd6c6aaa0e631a576c3cf43b71715/src/EishockeyManager.Core/Player.Factory.cs#L193)
und
[`TransferPoolRules.RollSlotFields`](https://github.com/kaaasi/eishockey-manager-redone/blob/d72afd7b95dbd6c6aaa0e631a576c3cf43b71715/src/EishockeyManager.Core/Transfer.Pool.cs#L220)
**nach** dem Würfeln Form random(45, 55) in
[`ApplyScoutedTalentValues`](https://github.com/kaaasi/eishockey-manager-redone/blob/d72afd7b95dbd6c6aaa0e631a576c3cf43b71715/src/EishockeyManager.Core/FinanceTalent.cs#L150)
(Talent, Jugend am Saisonende) bzw. im Listenwechsel
[Transfer.Pool.cs#L809](https://github.com/kaaasi/eishockey-manager-redone/blob/d72afd7b95dbd6c6aaa0e631a576c3cf43b71715/src/EishockeyManager.Core/Transfer.Pool.cs#L809).
Im Original ist die gewürfelte Form die Poolform (+0x17), die 0x42CB1 dann kopiert (`docs/transfer.md` Zeile „Listenwechsel,
Füllen“, `docs/finanzen-rekonstruktion.md` Talent). Ein Spieler mit gesetzter `PoolForm` bekäme im Remake die alte statt der neuen
Form. Beleg (Probe in temporärer Kopie, nicht committet): Spieler mit `PoolForm = 21`, `ApplyScoutedTalentValues` + `Join` →
`Assert.Equal(46, Form)` scheitert mit `Actual: 21`; Form random(45, 55) + `RollSlotFields` → `Expected 47, Actual 21`.
Erreichbarkeit: Mehrsaisonprobe (`TestGames.Create(5)`, `RngTwinProbeTests.Run`, passiv/aktiv, 4 Wechsel) fand 10 Spieler mit
abweichender Poolform außerhalb der Kader, aber **keinen** mit Besitzer `Free` und keinen, der später wieder in einen Kader kam
→ im normalen Lauf derzeit nicht beobachtet, daher P3. Vorschlag: `PoolForm = null` dort setzen, wo die Form neu gewürfelt wird
(oder Form in die Poolform würfeln), mit Test.

**P3-2 – Testlücke `IsSquadFull` in Vertrag und Antwort des Verkäufers.** Rückmutation von
[Transfer.Purchase.cs#L44](https://github.com/kaaasi/eishockey-manager-redone/blob/d72afd7b95dbd6c6aaa0e631a576c3cf43b71715/src/EishockeyManager.Core/Transfer.Purchase.cs#L44)
bzw.
[Transfer.SellerQuestion.cs#L37](https://github.com/kaaasi/eishockey-manager-redone/blob/d72afd7b95dbd6c6aaa0e631a576c3cf43b71715/src/EishockeyManager.Core/Transfer.SellerQuestion.cs#L37)
auf `Players.Count >= MaxSquad` → `--filter "FullyQualifiedName~KaderRest|FullyQualifiedName~Transfer"` 223/223 grün. Nur der
Bietweg (`Bid`) ist abgedeckt (`Bid_SquadFull_CountsOwnListingsAndLoanedOut`). Verhalten selbst plausibel (gleiche Funktion).

**Hinweise (kein Befund):**
- Erwartet offen bestätigt: A251 (Gebotshöhe 0x47347) und A226 (Buchung Klasse b) nur belegt, Restpunkte im PR; Schema 50
  vorläufig (Leitstelle nummeriert); #730, #773 unberührt. Anschluss `SeasonEndSummaryScreen.YouthJoined`: bis zum Merge mit
  `ui-rest` fällt `SeasonEvent.YouthJoined` in
  [`EventIcon`](https://github.com/kaaasi/eishockey-manager-redone/blob/d72afd7b95dbd6c6aaa0e631a576c3cf43b71715/src/EishockeyManager.UI/Screens/SeasonEndSummaryScreen.cs#L58)
  auf `_ => Symbol(6)` – auf `main` zeigt das Fenster je Neuzugang also das Europapokal-Symbol, bis die Leitstelle anschließt.
- Konflikt mit `main` nur in `docs/reverse-engineering.md` (siehe 1.); Rebase im Fixlauf.

**Besonders geprüft:**
1. *Saisonende:* `FinishSeason` ruft `SeasonEndYouthTeams()` (alle Manager, auch im Modus „nur Weltkugel“ – richtig, 0x55889
   liegt in der Saisonwechsel-Schleife, nicht in 0x26F20) vor Zwangsabstieg/Fenster und je Manager nach den Prämien
   `SeasonEndYouthEvents` (nur innerhalb der Schleife, die bei `WorldOnly` und ausgeschiedenen Managern nicht läuft – wie zuvor
   `ActiveManagers`/`!WorldOnly`). Jahr `Season + 1` = alter Aufruf `Season` nach `Season++`. Der Jugendspieltag des 29. April
   (`OnDayManager`) läuft vor `FinishSeason`. Alter/Herunterzählen weiter in `StartSquadSeason`. Migration: Stand vor 50 in
   `SeasonOver` → `_seasonEndYouthPending = true`, `CatchUpSeasonEndYouth` vor `Season++`; andere Phasen laufen den neuen Weg.
   Rückmutationen (Symbol, Reihenfolge, Nachholen, Migration) rot; Mehrsaisonprobe 4 Wechsel passiv/aktiv vorhanden.
2. *Transfer:* Prüffolge von `Offer` (Ruhestand → schon gelistet → 3 eigene → geliehen → Markt voll) entspricht der genannten
   Folge 0x46DD8/0x47027/0x47040/0x42CB1; `CanLeave` bleibt nur für Computervereine (`IsCurrentListing`). Besitzgrenze:
   `OwnedPlayers` zählt eigene Markt-Plätze außerhalb des Kaders, Ausnahme Spieler des eigenen Vereins. A196: Export schreibt je
   Paar `Thirds(ManagerLoanNet(a, b))`, gespiegelt; `TotalDebts`/Kontoleiste nur Doku `[APPROX]` → `[ORIGINAL]`.
3. *Fremde Tests (R15-E2 = C):* je Stelle Begründung mit Fundstelle im Test. Rückschaltprobe (Squad.Setup `managed: true` und
   `Inflict` → `player.Form`, sonst Head): `RngTwinProbeTests` (6), `SpielberechnungAnschlussTests.FourSeasonChanges_*` (2),
   `JugendTeamsTests.FourSeasonChanges_*` (2), `GameModeTests.Mode4_ReferenceValues_Unchanged` rot; reine Saatwechsel
   (`KaderTransferTeil4/7`, `SeasonFlowTests`, `TagestaktHauptwegTests`) und `InboxYouthTests` (Halt für Halt) bleiben dabei grün
   – erwartbar, ihre Vorbedingung gilt mit beiden Saaten; Begründung mit Saatenliste nachvollziehbar, Grenzen unverändert.

## 3. Ausgeführte Prüfungen

- Zugriff: `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD` → `a529b60…` (ok);
  `tools/claude/fetch-reference.sh original`, `tools/claude/setup-workspace.sh`, `dotnet --version` = 10.0.402.
- `tools/pruefen.sh` (alle drei Läufe, Erstreview) auf `d72afd7`:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2987 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2193 | 0 | 507 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2194 | 793 | 0 |

- CI auf dem Head: `build` success.
- Rückmutationen (temporärer Worktree, je eine Änderung, `EM_REQUIRE_GAMEDATA=1 dotnet test … --filter`):

| ID | Mutation | rot |
|---|---|---|
| A130 | `Squad.Setup`: `managed: true` entfernt | `KaderStartTests.StartStrain_Random80To120` (2), `File01_KoelnerEc_StartStrain80To120` |
| A066 | `Inflict` senkt `Form` statt `PoolForm` | `KaderRestTests.Inflict_LowersPoolForm_SlotFormStays`, `SpielberechnungKomplett3Tests.Inflict_FollowsOriginal` |
| A066 | `AdoptPoolForm` in `RollSlotFields` entfernt | – (nur `Join` getestet; siehe P3-1) |
| A253 | `Offer` wieder mit `CanLeave` | `Offer_AllGoalkeepers_NoMinimumPerRole_MatchDayStillRuns`, `TransferTests.Offer_LimitsAndWithdraw`, `WaveBReviewRegressionTests.PR3502_*` |
| A253 | `Retiring` bzw. `OnlyLoaned` → `Impossible` | `Offer_Retiring_RefusedBeforeOwnListingLimit` bzw. `Offer_LoanedIn_OnlyLoaned_AfterOwnListingLimit` |
| F089 | `IsSquadFull` ohne Besitzzählung / ohne Ausnahme eigener Verein | `Bid_SquadFull_CountsOwnListingsAndLoanedOut` |
| F089 | `OwnedPlayers` ohne eigene Markt-Plätze | `OwnedPlayers_CountsOwnListingsOutsideSquad`, `Bid_SquadFull_…`, `MultiSeason_…(active: True)` |
| F089 | Purchase / SellerQuestion ohne `IsSquadFull` | – (P3-2) |
| A244/A245 | Startmeldung wieder gesetzt | `TalentSearch_Started_NoMessage` |
| A196 | Export je Kredit statt Saldo | `KaderRestGeldTests.Export_NetFieldPerPartner_OriginalSumsMatchRemake` |
| Symbol 9 | Ereignisse nicht übernommen | `SeasonEnd_YouthJoinedBeforeWindow_Symbol9PerNewcomer` (passiv/aktiv) |
| Symbol 9 | `SeasonEndYouthTeams` in `FinishSeason` entfernt | dieselben + `JugendTeamsTests` (3) |
| Symbol 9 | `CatchUpSeasonEndYouth` entfernt / Migration setzt nie `true` | `SaveGame_Before50_AtSeasonEnd_CatchesUpYouth` |

- Konflikt-Probe: `git merge-tree --write-tree --name-only origin/main d72afd7` (main `3cd9169`) → Konflikt
  `docs/reverse-engineering.md`.

**Akzeptierte `[APPROX]`:** Computervereine Energie 50–80 und ±2 (keine Kaderplätze im Original); Mindestbesetzung 2/4/6 nur
Computervereine; Gebotshöhe beim Gebot (0x47347); Stände vor 50 vom Bildschirm „Saisonende“ holen Jugend ohne Symbol nach;
Poolwerte Kondition/Technik nicht getrennt geführt.
**Annahmen (nicht selbst in em.exe nachgelesen):** die Fundstellen 0x2728C–0x2730F, 0x55889, 0x46DD8/0x47027/0x47040,
0x3CCEB/0x42D1E, 0x591E2–0x5932B sind aus PR und `docs/reverse-engineering.md` übernommen und nur auf Stimmigkeit mit Code und
Tests geprüft.
