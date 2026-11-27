# Nachprüfung: Integration Runde 11, Spur `kader-transfer` (PR #462)

Unabhängige Nachprüfung (Claude Code, geplanter Lauf, 02.11.2026; nicht an Spur oder Integration beteiligt) des Reviews
`docs/reviews/2026-11-02-integration-kader-transfer.md` (PR #463) nach `docs/vorlagen/review-anfrage.md`. Nur Delta
`ff75934..bbece11` und die Befunde KT-01 bis KT-05 (Entscheidung Nutzer B: KT-01 annehmen und dokumentieren, KT-02 bis
KT-05 beheben). Geprüft am nachgewiesenen Checkout (Klon, `git rev-parse HEAD` = `bbece11d9bb5dcc209ac32b6117c4c948179091b`,
`git status --porcelain` leer) mit Originaldaten aus `kaaasi/eishockey-manager-referenz` (`original/`). Mutationen nur in
einem getrennten Worktree, danach verworfen.

| PR | Base | bisher geprüft | Head |
|---|---|---|---|
| #462 | `606403065b72f7098edd1d57afa0cebda4ad1020` | `ff7593437b121db31b24570b854c8661bb787308` | `bbece11d9bb5dcc209ac32b6117c4c948179091b` |

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** Das Delta (ein Commit, 7 Dateien) ändert nur `tests/` und `docs/`, keinen Produktionscode.
KT-01 ist wie entschieden angenommen und an allen drei verlangten Stellen genannt, die Probe ist sonst unverändert streng.
KT-03 hat jetzt einen eigenen Test, der bei der Mutation rot wird. Die Kommentare zu KT-02, KT-04 und KT-05 stimmen mit dem
Code. `tools/pruefen.sh` ist in allen drei Läufen grün. Einziger neuer Befund ist eine überzogene Zahl in einem
Testkommentar (KTN-01, P3, nicht merge-hindernd).

## 2. Befunde

### Alte Befunde

| ID | Stand | Nachweis |
|---|---|---|
| KT-01 | angenommen, dokumentiert | [`FinanceCalibrationTests.cs` Z. 171–188](https://github.com/kaaasi/eishockey-manager-redone/blob/bbece11d9bb5dcc209ac32b6117c4c948179091b/tests/EishockeyManager.Core.Tests/FinanceCalibrationTests.cs#L171): Zusammenfassung von `AssertEnvelope` nennt die Ausnahme (Seed 41, −1.586.947 DM, Acht-Wochen-Regel); [Z. 360](https://github.com/kaaasi/eishockey-manager-redone/blob/bbece11d9bb5dcc209ac32b6117c4c948179091b/tests/EishockeyManager.Core.Tests/FinanceCalibrationTests.cs#L360) Kommentar am Assert; `docs/arbeitspakete.md` Z. 205–206. Bedingung `active \|\| league != League.Bundesliga` und alle übrigen Asserts unverändert (Diff nur Kommentare). |
| KT-02 | behoben | [`TestSquadKeeper.cs` Z. 11–17](https://github.com/kaaasi/eishockey-manager-redone/blob/bbece11d9bb5dcc209ac32b6117c4c948179091b/tests/EishockeyManager.Core.Tests/TestSquadKeeper.cs#L11) und `FinanceCalibrationTests.cs` Z. 343–344: Sponsorzuschuss-Wurf beim Kauf (random(0, 6), Hauptquelle, 0x063BE), Angebot verworfen, keine Buchung. Kein „kein Zufallszug“/„zieht keine Zahl“ mehr in beiden Dateien. |
| KT-03 | behoben | Neuer Test [`KaderTransferTeil7Tests.Offer_NeverWithTwoYearsLeft_WholeSquad`](https://github.com/kaaasi/eishockey-manager-redone/blob/bbece11d9bb5dcc209ac32b6117c4c948179091b/tests/EishockeyManager.Core.Tests/KaderTransferTeil7Tests.cs#L160) (synthetisch, `[Fact]`). Mutation `\|\| p.ContractYears != 1` in `Transfer.Squads.cs` Z. 161 entfernt → rot (8 von 25 Spielern mit Angebot), übrige 7 Tests der Klasse grün; ohne Mutation grün. |
| KT-04 | behoben | [`IntegrationRunde8Welle3Tests.cs` Z. 154–158](https://github.com/kaaasi/eishockey-manager-redone/blob/bbece11d9bb5dcc209ac32b6117c4c948179091b/tests/EishockeyManager.Core.Tests/IntegrationRunde8Welle3Tests.cs#L154): Annahme über `ContractRules.Accepts`, Zweig `years == current && …` nicht nötig. Mutation `Player.Contract.cs` Z. 480 → `\|\| false && years == current …`: Klasse 7/7 grün – stimmt mit dem neuen Kommentar. |
| KT-05 | behoben | `GameModeTests.cs` Z. 318–320: Zusammenfassung mit kader-transfer-Zeile (Oberliga-Manager 400.000 DM nach S-K1, Saison 1 500.000), Teil-7-Kommentar als Spurstand gekennzeichnet (Z. 351); passt zum Abdruck `Mode4Reference`. Doc-Kommentar steht wieder an `AssertEnvelope`, der Entlassungstest hat nur noch seinen eigenen. `KaderTransferTeil2Tests.cs` Z. 99: „Migration 27 → 28 (Spur vorläufig 22 → 23)“. |

### Neuer Befund

**KTN-01 (P3).** [`KaderTransferTeil7Tests.cs` Z. 163–164](https://github.com/kaaasi/eishockey-manager-redone/blob/bbece11d9bb5dcc209ac32b6117c4c948179091b/tests/EishockeyManager.Core.Tests/KaderTransferTeil7Tests.cs#L163):
„ohne diese Bedingung käme eine zweistellige Zahl Angebote zusammen“. Gegenbeispiel: Mutation wie KT-03 → 8 von 25
Spielern mit je einem Angebot (`Assert.All` meldet 8 Fehlschläge); mehr als ein Angebot je Spieler ist ausgeschlossen, weil
`Notice` danach `Offered` ist. Die Probe ist trotzdem scharf; nur die Zahl im Kommentar stimmt nicht („mehrere“ bzw. „rund
acht“). Bei nächster Gelegenheit berichtigen, kein Merge-Hindernis.

### Erwartet offen – bestätigt

Wie im Review #463: Doku-Anschlüsse (re-meldungen.md, re-geld.md, reverse-engineering.md, spielberechnung.md §15, Inventur)
offen in `arbeitspakete.md`; Verletzungsart in `MatchEngine.Injuries.cs` → Integration `wettbewerbe-spiel`;
U4/U5/U9/U15 → Runde 12. Das Delta berührt keinen dieser Punkte.

## 3. Ausgeführte Prüfungen

Setup: `tools/claude/fetch-reference.sh original`, `tools/claude/setup-workspace.sh ~/referenz/original/gamedata`, SDK 10.0.402.

- `git diff --stat ff75934 bbece11` und `git diff --name-only … | grep -v -E '^(tests|docs)/'` → leer: kein Produktionscode.
- Delta vollständig gelesen.

`tools/pruefen.sh` (alle drei Läufe) am Head `bbece11`:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1854 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1561 | 0 | 261 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1561 | 293 | 0 |

Gegenüber `ff75934` je +1 (neuer synthetischer Test KT-03).

Mutationen (`EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.Core.Tests --filter …`):

| Befund | Mutation | Ergebnis |
|---|---|---|
| KT-03 | `Transfer.Squads.cs` Z. 161: `\|\| p.ContractYears != 1` entfernt | rot: `Offer_NeverWithTwoYearsLeft_WholeSquad` (1/8 rot); ohne Mutation grün |
| KT-04 | `Player.Contract.cs` Z. 480: Zweig `years == current && …` aus | grün (7/7) – wie der neue Kommentar sagt |

KT-01 nicht erneut mutiert: Der Code der Probe ist gegenüber `ff75934` unverändert (Diff nur Kommentare); die Notwendigkeit der
Ausnahme ist im Review #463 belegt.

**Akzeptierte `[APPROX]`:** keine neuen im Delta.

**Annahmen (nicht geprüft):** Die Zahlen in den berichtigten Kommentaren (Seed 41 −1.586.947 DM, Mode4 500.000 in Saison 1)
nur gegen Review #463 und den Abdruck abgeglichen, nicht neu ausgerechnet.
