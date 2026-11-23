# Nachprüfung Review PR #962 (Spur v-rest: VR-01, A961)

Reviewer: Claude Code (Opus 5.5), eigene Sitzung ohne Implementierungskontext, geplanter Lauf. Nachprüfung von
[`2026-11-23-v-rest.md`](2026-11-23-v-rest.md) (bisher geprüfter Head `2b3a649`). Geprüft: Head
`3575a3de20021ddfa5c2f67562931da5dd5dd52d` gegen Base `338b1d4a`, Checkout `git rev-parse HEAD` = Head, `git status --porcelain`
leer, SDK 10.0.402. Umfang: nur Delta `2b3a649..3575a3d` und die Befunde VR-01, A961 (VR-02 fällt im Delta mit an).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. `git range-diff 70a09fc..2b3a649 338b1d4..3575a3d`: die fünf geprüften Commits unverändert (`=`),
neu nur e89815d (VR-01), 5410dff (A961/VR-02) und 3575a3d (Status). VR-01 ist behoben und durch einen Regressionstest
abgesichert; A961 entspricht dem selbst gelesenen Disassembly, beide Mutationen der Anfrage werden rot. `--schnell` grün.

## 2. Befunde

Keine neuen Befunde (P0–P3). Einzeln:

**VR-01 – behoben.** [`Player.Contract.cs:370`](https://github.com/kaaasi/eishockey-manager-redone/blob/3575a3de20021ddfa5c2f67562931da5dd5dd52d/src/EishockeyManager.Core/Player.Contract.cs#L370)
ruft `NewSlotInputs` ohne `fromManager`; [`Player.Value.cs:229`](https://github.com/kaaasi/eishockey-manager-redone/blob/3575a3de20021ddfa5c2f67562931da5dd5dd52d/src/EishockeyManager.Core/Player.Value.cs#L229)
setzt `Penalties5/2 = 0`, Docstring nennt die Kopie 0x47877/0x4787D–0x478CB hinter dem Dialog 0x4786A richtig.
`ApplyNewSlot` (Zustand nach dem Abschluss) unverändert; der neue Test prüft zusätzlich, dass die Strafen nach `Sign` übernommen
sind. Gegenbeispiel: Mutation `Penalties5/2 = Math.Min(player.Penalties5/2, 255)` (Pool) in `NewSlotInputs` →
`PurchaseFromManager_IgnoresSellerSlotPenalties` **rot** (mit ihm die drei übrigen `KaufNeuerKaderplatzTests`, da die
Mutation alle Käufe trifft).

**A961 / VR-02 – behoben, gegen `em.exe` bestätigt.** [`MatchGate.cs:139–176`](https://github.com/kaaasi/eishockey-manager-redone/blob/3575a3de20021ddfa5c2f67562931da5dd5dd52d/src/EishockeyManager.Core/MatchGate.cs#L139-L176).
`ndisasm -b16 -e 0x544C0 -o 0x544C0 em.exe`, 0x5481A–0x549CE selbst gelesen:
- Spiele: 0x54918/0x54922 nur bei +0x0A ≠ 0 und +0x15 ≠ 0 (= `club.Lineup.Players`, wie im gespielten Pfad `CreditLineup`);
  Feld +0x06 bzw. Wort +0x1C mit Versatz Pokal (`[bp+0xA] = 1`) bzw. Europapokal (`= 8`), sonst Liga (0x5494C–0x5499E);
  Pool +0x1C nur bei `[bp+0xA] = 0` (0x5492E–0x54948; = Ligaspiele, im Remake aus `Games − Cup − Europe` abgeleitet,
  `spielstand-original.md:330`). `CountGame(CompetitionSlots.Of(competition))` deckt das ab.
- Belastung 50 … 150 für **alle** Kaderplätze der Schleife (0x549A2–0x549BB, außerhalb der Aufgestellt-Bedingung) =
  `ClampStrain(club.Players)`. Keine Zufallszüge in diesen Schritten; Reihenfolge der Züge (`RollPlayedLines`, 0x548D3) unverändert.
- Nicht spielfähiger Manager und alle späteren: Abbruch vor 0x5481A (0x54589 bzw. 0x547AA) = `break` vor `CountForfeitGames`.
- „Ohne Zustand“ 0x54821: einziger Schreiber von `[bx+0x2D6]` in `em.exe` ist 0x54821 (`mov …,5`; Bytesuche `C6 87 D6 02`,
  `88 87 D6 02`, `FE 87/8F D6 02`), einziger Leser 0x54376; Startwert in DGROUP (Datei 0x16DD0 + 0x2D6) `05 05 05 05 05 05`.
  Der Wert ist also immer 5 – Begründung trägt.
- „Ohne Zustand“ Strafenzähler 0x5483B–0x54897 (Markt, Satz 4 0x7623, Pool +0x1A = Manager) und 0x548EB–0x5490C (Kader):
  Abzug nur bei +0x09 Bit 0 und +0x0D ≠ 0 und `[bp+0xA] = 0`. Bit 0 setzt einzig 0x35C02 (Strafe im Spiel; Bytesuche
  `26 80 4F 09`: sonst nur Bits 0x02/0x80/0x40 bei 0x5C44/0x7358/0x7A5C); gelöscht bei Strafende (0x35795, 0x55163) und nach
  dem Spieltag 0x551D4–0x551E0 bei +0x0D ≠ 0. Vor einer Vorbereitung ist Bit 0 daher 0, der Abzug wirkungslos; gleiche Lage wie
  das bestehende `[APPROX]` in `MatchEngine.PenaltyBoxQuery.cs:16`. Die Umstufung des Markers zu `[ORIGINAL]` mit dieser
  Begründung ist vertretbar; VR-02 (fehlende Fundstellen) ist damit erledigt.
- Gegenbeispiele (temporär, zurückgesetzt), `dotnet test tests/EishockeyManager.Core.Tests -c Release --filter SpielwertungVorbereitungTests`:

| Mutation in `GameState.CountForfeitGames` / `PrepareForfeitGate` | Ergebnis |
|---|---|
| `p.CountGame(slot)` entfernt | `AwayShortHanded_…(League, DebCup)` 2/7 rot |
| `TrainingRules.ClampStrain(club.Players)` entfernt | 2/7 rot (dieselben) |
| `club.Lineup.Players` → `club.Players` (auch Bank zählt) | 2/7 rot |
| Feld immer `CompetitionSlot.League` | 1/7 rot (`DebCup`) |
| Auch der nicht spielfähige Verein zählt (vor dem `break`) | 4/7 rot (beide `LowerIndexShortHanded_…`, beide `AwayShortHanded_…`) |

Hinweis ohne Klasse: Europapokal (Feld 8) ist nur über die gemeinsame Zuordnung `CompetitionSlots.Of` abgedeckt, nicht mit
eigenem Testfall; Play-offs zählen wie im gespielten Pfad im Ligafeld (unverändert seit Base, nicht Teil des Deltas).

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh --schnell` auf dem Head (Nachprüfung, `zusammenarbeit.md` §5):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3802 | 0 | 0 |

CI auf dem Head: `build` success. Weitere Befehle: `git range-diff 70a09fc..2b3a649 338b1d4..3575a3d`,
`git diff 2b3a649 3575a3d -- src tests` (6 Dateien, +144/−12), `ndisasm` wie oben, Bytesuchen per Python in `em.exe`.

**Akzeptierte `[APPROX]`:** keine neuen; der Marker in `MatchGate.cs` entfällt zugunsten der `[ORIGINAL]`-Aufzählung (oben belegt).
**Annahmen:** Zuordnung Kaderplatz +0x0A/+0x15 ↔ `Lineup.Players` aus dem gespielten Pfad (A677) übernommen; Bytesuche erfasst
keine Blockkopien (Spielstand laden) – diese könnten nur gespeicherte Werte zurückschreiben, die nach obiger Lage 5 bzw. Bit 0 = 0 sind.
