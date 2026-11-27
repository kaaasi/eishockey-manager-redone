# Nachprüfung: Integration der Spur `wettbewerbe-spiel` (PR #465)

Unabhängige Nachprüfung (Claude Code, geplanter Lauf, 02.11.2026) des Reviews
[`2026-11-02-integration-wettbewerbe-spiel.md`](2026-11-02-integration-wettbewerbe-spiel.md) (PR #468), nach
`docs/vorlagen/review-anfrage.md`. Geprüft wurde nur das Delta `08b7b68..7860bb1` und die genannten Befunde. Das Delta ist
ein einziger Commit (`7860bb1`, Befunde Review #468). Geprüft am nachgewiesenen Checkout: Worktree mit
`git rev-parse HEAD` = Head, `git status --porcelain` leer, SDK 10.0.402, Originaldaten aus
`kaaasi/eishockey-manager-referenz` (`original/`). Ich habe nicht an PR #465 mitgearbeitet.

| PR | Base | Geprüft bisher | Head |
|---|---|---|---|
| #465 | `4e71df745b91b5ceb31f0a20a20ad2e63c32c0cc` | `08b7b683949b9229d3fad424b48eea6354764bb2` | `7860bb1f7fda1061e7dcd1351dc5da43f7309eb8` |

## 1. Merge-Empfehlung

**Ja, für Head `7860bb1`** (Ausführung am nachgewiesenen Checkout, nicht nur Codelesen). WS-01, WS-03 und WS-05 sind behoben,
WS-02 ist wie gewünscht eingeengt; alle drei Läufe von `tools/pruefen.sh` sind grün, die geforderten Mutationen werden rot.
Das Delta ändert an Produktionscode nur `SaveGame.Migration.WettbewerbeSpiel8.cs`. Kein neuer Befund P0–P2.
Offen bleiben nur die erwarteten Punkte: WS-02 (Entscheidung Nutzer) und WS-04 (Runde 12).

## 2. Befunde

| ID | Stand | Ergebnis der Nachprüfung |
|---|---|---|
| WS-01 (P2) | **behoben** | Siehe unten. |
| WS-02 (P2) | **offen, Entscheidung Nutzer** (vorläufig Alternative 1 umgesetzt) | Siehe unten. |
| WS-03 (P3) | **behoben** (Kommentar) | Siehe unten. |
| WS-04 (P3) | **offen → Runde 12** | Bestätigt: [`docs/arbeitspakete.md`](https://github.com/kaaasi/eishockey-manager-redone/blob/7860bb1f7fda1061e7dcd1351dc5da43f7309eb8/docs/arbeitspakete.md) nennt „WS-04 (Neuzählung Grenze 5) Runde 12“. Delta berührt `MatchEngine.RecountSkaters` und seine Tests nicht. |
| WS-05 (P3) | **behoben** (S-K1); übrige Punkte waren Beobachtungen | Siehe unten. |

**WS-01 – behoben.** [`SaveGame.Migration.WettbewerbeSpiel8.cs:21`](https://github.com/kaaasi/eishockey-manager-redone/blob/7860bb1f7fda1061e7dcd1351dc5da43f7309eb8/src/EishockeyManager.Core/SaveGame.Migration.WettbewerbeSpiel8.cs#L22)
setzt `_autoLineup ??= !byHand` mit `byHand` aus `_dressedByHand`; fehlt oder ist das Feld kein Bool, gilt `false`, also
Einstellung an (Startwert des Originals). Das ist die im Review vorgeschlagene Form. Der neue Test
`Migration_31To32_KeepsHandMadeLineup` baut das Gegenbeispiel nach (Stürmer 0/0 per `AssignToLineup`, `schema = 31`,
`_autoLineup` entfernt, laden, `Continue()`): Einstellung bleibt aus, die Wahl bleibt auf dem Platz. Gegenprobe siehe §3.
Der bestehende Test `Migration_31To32_SetsSettingOn` setzt jetzt `_dressedByHand = false` je Verein, weil der Start seit
Teil 8 den Managerverein von Hand aufstellt (sonst gilt der neue Zweig). Er vergleicht statt mit dem Ausgangsstand nur noch,
dass Speichern → Laden → Speichern stabil ist; das ist schwächer, aber begründet, da der geladene Stand bewusst vom
Ausgangsspiel abweicht (Beobachtung, kein Befund).

**WS-02 – offen (Entscheidung Nutzer).** Umgesetzt ist Alternative 1: Ausnahme nur mit `seed == 17`
([`FinanceCalibrationTests.cs:298`](https://github.com/kaaasi/eishockey-manager-redone/blob/7860bb1f7fda1061e7dcd1351dc5da43f7309eb8/tests/EishockeyManager.Core.Tests/FinanceCalibrationTests.cs#L298)),
dazu `dismissedSeeds <= 1` am Ende (Zeile 386). Jeder andere Seed, der entlassen wird, läuft in die `SeasonOver`-Prüfung
und wird rot; die Mutation bestätigt das (§3). Bewertung der Regel selbst unverändert wie im Review: sachlich vertretbar,
aber Seed-Auswahl nach Ergebnis – die Entscheidung bleibt beim Nutzer. Restschwäche: Die Zählprüfung `dismissedSeeds <= 1`
war nicht mehr eigenständig auslösbar, weil `seed == 17` im selben Zweig bereits jeden zweiten entlassenen Seed ausschließt;
sie ist redundant, aber harmlos.

**WS-03 – behoben (Kommentar).** [`WettbewerbeSpiel2Tests.cs:41`](https://github.com/kaaasi/eishockey-manager-redone/blob/7860bb1f7fda1061e7dcd1351dc5da43f7309eb8/tests/EishockeyManager.Core.Tests/WettbewerbeSpiel2Tests.cs#L41)
sagt jetzt ausdrücklich, dass „Europapokal nein“ nur strukturell belegt ist (Gegner ≥ 64, `MeetingHistory.Record` verwirft
sie). Eine echte Prüfung gibt es weiterhin nicht; das war der Vorschlag „Kommentar anpassen“ und ist damit erfüllt.

**WS-05 – behoben.** [`StadiumAusbauTests.cs:247`](https://github.com/kaaasi/eishockey-manager-redone/blob/7860bb1f7fda1061e7dcd1351dc5da43f7309eb8/tests/EishockeyManager.Core.Tests/StadiumAusbauTests.cs#L247)
prüft `Konto = OberligaKeptMoney + Prämien` vor der Aufnahme der neutralen Differenz in `cumulative`.

## 3. Ausgeführte Prüfungen

### `tools/pruefen.sh` (alle drei Läufe) am Head `7860bb1`

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1925 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1630 | 0 | 263 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1630 | 295 | 0 |

Gegenüber `08b7b68` (1924 / 1629 / 1629 + 295) je ein Test mehr (`Migration_31To32_KeepsHandMadeLineup`); sonst gleich.
Build mit Warnungen als Fehler grün. `git diff --stat 08b7b68..7860bb1`: Produktionscode nur
`SaveGame.Migration.WettbewerbeSpiel8.cs` (+5/−2); sonst vier Testdateien und `docs/arbeitspakete.md`.

### Gegenproben (Mutation im Worktree, danach `git checkout`; `dotnet test --filter`, mit Originaldaten)

| Befund | Mutation | Ergebnis |
|---|---|---|
| WS-01 | Migration `club["_autoLineup"] ??= true` (alter Stand) | `AutoLineupTeil8Tests`: **rot** (`Migration_31To32_KeepsHandMadeLineup`, 1 von 17); am Head grün |
| WS-02 | `Manager.DebtLimit` künstlich von −1,5 Mio. auf −1,0 Mio. DM | **rot**: `FourSeasonChanges_…(Bundesliga, active: False)` echt und synthetisch, Zeile 306 (Seed 3 entlassen ohne Ausnahme); Basislauf ohne Mutation 8 von 8 grün |
| WS-05 | `manager.Money = OberligaKeptMoney + 1000` in `SeasonFlow.cs:326` | `StadiumAusbauTests`: **rot** (2 von 17, erwartet 600000, tatsächlich 601000) |

Nicht gegengeprüft: die Zählprüfung `dismissedSeeds <= 1` (siehe WS-02, vom `seed == 17`-Zweig verdeckt) und WS-03
(nur Kommentar).

### Akzeptierte `[APPROX]`

Keine neuen im Delta. Die Begründung der Migration (Datei 0x40ADE/0x40DE4) stammt aus dem Review #468 und ist unverändert
als Annahme übernommen, nicht neu gegen `em.exe` geprüft (außerhalb des Deltas).
