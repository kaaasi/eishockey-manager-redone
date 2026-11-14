# Nachprüfung PR #842 – Spur `nutzer-befunde` (Review `2026-11-14-nutzer-befunde.md`)

- **Geprüft:** Head `1fd7196238b46998b8019a93813d48c789099d85` (Base `4e55e076eeedec61e9656e508515c0dba3eb7cd9`), nur Delta
  `b41507d..1fd7196` und P1-1, P3-1, P3-2. Sauberer Checkout (`git rev-parse HEAD` = Head, `git status --porcelain` leer),
  SDK 10.0.402, Originaldaten aus dem Referenz-Repo.
- **Reviewer:** Claude Opus 5.5, geplanter Lauf ohne Implementierungskontext.

## 1. Merge-Empfehlung

**Ja** für `1fd7196`. P1-1 ist behoben und durch rote→grüne Tests (Kern und UI, mit Originaldaten) abgesichert, das Hinzufügen
ist atomar, P3-1 ist als `[APPROX]` benannt, P3-2 ist geschärft (Mutation ohne `PrepareMatchGate` jetzt rot). Keine neuen Befunde.

## 2. Befunde

Keine neuen. Status der genannten:

| Befund | Probe | Ergebnis |
|---|---|---|
| P1-1 | `AddManager_AfterLoad_ThenMatchDays_SaveLoadIsByteIdentical`, `AddManager_AfterLoad_OriginalData_…` (Probe des Reviews: Verein 2, Seed 5, Neuer Verein 7, 3 Spieltage, sichern/laden bytegleich), UI `NewTile_AfterLoad_AddsManager` („Neuer“ → „Los jetzt!“ nach Laden); Mutation `Growable` → `false`: 6 Kerntests + UI-Test rot | **behoben** |
| P1-1 Atomarität | `AddManager_Rejected_LeavesGameUnchanged` (vergebener/ungültiger Verein, leerer Name im geladenen Spiel: Spielstand bytegleich, `Setup.Managers` = `Managers` = 1), `AddManagers_ChecksAllRowsBeforeAddingAny` (doppelter Verein, fünfter Manager); Mutation „Vereinsprüfung erst nach `Setup.AddManager`“ → 3 Tests rot | **behoben** |
| P3-1 | [`GameState.AddManager.cs` Z. 68–73](https://github.com/kaaasi/eishockey-manager-redone/blob/1fd7196238b46998b8019a93813d48c789099d85/src/EishockeyManager.Core/GameState.AddManager.cs#L68-L73): `[APPROX]` nennt `FinanceRandom`/`TrendRandom` und die Folgeverschiebung; `AddManager_IsDeterministic_ForEqualGames` grün | **behoben** |
| P3-2 | Mutation `MatchEngine.Live.cs` Z. 125 (`PrepareMatchGate` auskommentiert) → `MultiSeason_FourChanges_EveryPreparedGateIsSettled` rot (je Saison vorbereitet = abgerechnet > 0 über `GateCounts`); Geldfixierung entfernt, Sichern/Laden nach jedem Saisonwechsel und Weiterspielen mit dem geladenen Spiel; neue `AddManager_AfterLoad_MultiSeason_OriginalData_WithoutMoneyFixing` (Hinzufügen nach Laden, 4 Wechsel, bytegleich, niemand entlassen) | **behoben** |

**Besonders geprüft:**

- **Leserregel `Growable`** ([`SaveGame.Reader.cs` Z. 255, 341](https://github.com/kaaasi/eishockey-manager-redone/blob/1fd7196238b46998b8019a93813d48c789099d85/src/EishockeyManager.Core/SaveGame.Reader.cs#L255-L341)):
  greift nur bei `declared == typeof(IReadOnlyList<Manager>)`; einziger solcher Typ in `src/` ist `GameState.Managers`
  (`grep -rn "IReadOnlyList<Manager>" src --include=*.cs`). Vorrang `IsArray || (IsReadOnly && !Growable)` korrekt; der Fall
  fällt in den vorhandenen `List`-Zweig. `Kind` hängt am deklarierten Typ, der Schreiber gibt für `ReadOnlySequence` keinen
  `$type` aus (Writer Z. 140) → Datei unverändert; `SaveGame.cs`/`SaveGame.Writer.cs` im Delta unberührt (Schema gleich).
- **Atomarität `AddManagers`** ([Z. 86–118](https://github.com/kaaasi/eishockey-manager-redone/blob/1fd7196238b46998b8019a93813d48c789099d85/src/EishockeyManager.Core/GameState.AddManager.cs#L86-L118)): Liste erweiterbar,
  Platz/Weltkugel, Setup-Länge, Name, Vereinsbereich und Dopplung (gegen vorhandene und untereinander) vor der ersten Änderung
  (Auffüllen `Setup.Managers` Z. 107). `SetupScreen.TryAdd` übergibt alle Zeilen auf einmal und fängt nur
  `InvalidOperationException`/`ArgumentException` ([Z. 233](https://github.com/kaaasi/eishockey-manager-redone/blob/1fd7196238b46998b8019a93813d48c789099d85/src/EishockeyManager.UI/Screens/SetupScreen.cs#L233)) – kein
  Absturz mehr, abgelehnter Klick ohne Wirkung.

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh --schnell` am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3249 | 0 | 0 |

Mutationen (temporärer Worktree, je zurückgesetzt, nicht committet; `EM_REQUIRE_GAMEDATA=1`):

- `Growable` → `false`: `dotnet test tests/EishockeyManager.Core.Tests --filter AddManagerTests` → 6 rot / 8 grün;
  `… UI.Tests --filter AddManagerScreenTests` → `NewTile_AfterLoad_AddsManager` rot.
- Dopplungsprüfung aus der Vorprüfung entfernt, nach `Setup.AddManager` geworfen → `AddManager_Rejected_…`,
  `AddManagers_ChecksAllRowsBeforeAddingAny`, `AddManager_Rejects_TakenClub_…` rot.
- `PrepareMatchGate` entfernt → `MultiSeason_FourChanges_EveryPreparedGateIsSettled` rot.

**Akzeptierte `[APPROX]`:** P3-1 (geteilte `FinanceRandom`/`TrendRandom`, Folge des Originals nicht nachgebildet); übrige wie im
Erstreview.
**Annahmen:** Nach den Prüfungen kann `Seat` theoretisch noch scheitern, wenn eine Zeile ohne Verein keinen freien Verein findet
(`PickRandomClub`); die UI übergibt stets einen Verein, bei höchstens vier Managern nicht erreichbar – nicht weiter verfolgt.
`GateCounts` ist Prüfzähler je Objekt, kein Spielstand. Verhalten des Originals beim Hinzufügen nach Laden weiterhin nicht per
DOSBox geprüft (wie Erstreview).
