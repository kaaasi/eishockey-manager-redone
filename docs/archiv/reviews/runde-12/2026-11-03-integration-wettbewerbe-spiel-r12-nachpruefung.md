# Nachprüfung Review PR #508 – Integration Runde 12, Spur wettbewerbe-spiel

- **PR:** #508 · **Base:** `d5bf0643beb179609dceb5eaf1ef045a7869d78c` (= aktueller `main`) · **Head:** `50cac4f42a1092a4babcdd12a3e904f9636121e0`
- **Nachprüfung von:** `docs/reviews/2026-11-03-integration-wettbewerbe-spiel-r12.md` (PR #514), bisher geprüfter Head
  `c141fb60a5a82b799c92f6b3f8d046c9590036c6`; geprüft nur das Delta `c141fb6..50cac4f` (`80944d3`, `50cac4f`) und die Befunde.
- **Reviewer:** Claude Code (geplanter Lauf, eigene Sitzung ohne Implementierungskontext; nicht an #491/#508 beteiligt).
- **Checkout:** Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer; Ausführung am nachgewiesenen Checkout.

## 1. Merge-Empfehlung

**Ja für genau diesen Head `50cac4f`.** R12I-01 ist behoben: beide Gegenbeispiele sind am alten Head rot und am neuen Head grün.
Die neuen Abdrücke der Originaldatenbank sind allein durch R12I-01 erklärt. Werden nur die zwei Zeilen `Strain = random(80, 120)`
zurückgenommen, liefern die vier Tests wieder genau die alten Werte, und kein anderer Core-Test ändert sich. Alle drei Läufe von
`tools/pruefen.sh` am Head sind bestanden, CI `build` ist grün.

R12I-02 bestätige ich **nicht**. Der Befund war falsch und wird zurückgezogen (siehe unten). Ich empfehle, #517 zu schließen.

Die neuen Befunde R12N-01 und R12N-02 haben P3 und blockieren den Merge nicht. Zur Merge-Prüfung §6 Nr. 6: Die Restliste aus
R12N-02 gehört in die Restlücken von `docs/arbeitspakete.md`.

## 2. Befunde

### R12I-01 (P2, #516): behoben

- **Neue Kaderplätze:** [FinanceTalent.cs:144](https://github.com/kaaasi/eishockey-manager-redone/blob/50cac4f42a1092a4babcdd12a3e904f9636121e0/src/EishockeyManager.Core/FinanceTalent.cs#L144)
  und [Transfer.Pool.cs:209](https://github.com/kaaasi/eishockey-manager-redone/blob/50cac4f42a1092a4babcdd12a3e904f9636121e0/src/EishockeyManager.Core/Transfer.Pool.cs#L209)
  setzen jetzt `Strain = Random16(80, 120)`. Die Folge der Zufallszahlen ist unverändert (ein Zug wie vorher).
- **Spalte E:** [LineupScreen.cs:278](https://github.com/kaaasi/eishockey-manager-redone/blob/50cac4f42a1092a4babcdd12a3e904f9636121e0/src/EishockeyManager.UI/Screens/Team/LineupScreen.cs#L278),
  [ContractsScreen.cs:277](https://github.com/kaaasi/eishockey-manager-redone/blob/50cac4f42a1092a4babcdd12a3e904f9636121e0/src/EishockeyManager.UI/Screens/Team/ContractsScreen.cs#L277)
  und [TransferScreen.cs:384](https://github.com/kaaasi/eishockey-manager-redone/blob/50cac4f42a1092a4babcdd12a3e904f9636121e0/src/EishockeyManager.UI/Screens/Team/TransferScreen.cs#L384)
  verwenden denselben Ausdruck wie `NationalLineupScreen.cs:219`: `ExhaustionDisplay` = Belastung · 10 / 15 und Farbe `0x10` bei
  `ExhaustionWarning` (Belastung > 130).
- **Wertgleich umgestellt:** `PlayerHintLine.Exhaustion` und der Balken in `PlayerInfoPopup` rechnen jetzt
  `clamp(Strain − 50, 0, 100)`. Das ist algebraisch gleich `100 − clamp(Energy, 0, 100)`, weil `Energy = clamp(150 − Strain, 0, 100)`.
  `Transfer.Pool.cs:511` prüft jetzt `Strain ≥ 150`, das ist gleichbedeutend mit dem alten `Energy ≤ 0`. `Transfer.Pool.cs:529`
  kopiert jetzt `Strain` ohne die frühere Begrenzung auf 50 … 150. Da die Belastung nach Spiel und Training ohnehin auf 50 … 150
  begrenzt wird, hat das keine sichtbare Wirkung.

### R12I-02 (P3, #517): zurückgezogen, kein Befund

Erneut ausgeführt: Mutation `&& !squad.NationalCoach` in [MatchEngine.Managed.cs:380](https://github.com/kaaasi/eishockey-manager-redone/blob/50cac4f42a1092a4babcdd12a3e904f9636121e0/src/EishockeyManager.Core/MatchEngine.Managed.cs#L380)
entfernt, Filter wie im Review (`National|WettbewerbeSpiel12|Friendly|WorldCup|Bundestrainer`). Ergebnis:

- Core: 142/142 grün.
- UI: **4 rot**: `WeltkugelDosboxTests.FirstFriendly_SetsSquadEnergyTo70`, `MultiSeason_ActiveCoach_FirstFriendlySetsEnergy`
  (Full, WorldOnly) und `OpeningFriendlies_KeepsState_FirstFriendlySetsEnergy`.

Am alten Head `c141fb6` ist es genauso (4 von 14 `WeltkugelDosbox` rot). Diese Tests prüfen nach dem Länderspiel `Strain = 70` für
alle Spieler des Aufgebots, also für die Vereinsspieler selbst. Damit decken sie genau den vorgeschlagenen Test ab. `FirstFriendly_SetsSquadEnergyTo70`
ist ein synthetischer `[Fact]` und läuft auch in der CI. Mein Befund im ersten Review beruhte auf einem Filterlauf ohne
diese UI-Tests. Die Ausnahme ist abgesichert, daher kein Handlungsbedarf.

### R12N-01 (P3): R12I-01 ohne gezielten Regressionstest

AGENTS.md §2.6 verlangt einen Regressionstest, der vor der Behebung rot ist. Das Delta enthält keinen. Gegenproben am Head:

- **Core:** Die zwei Zeilen werden auf `Energy = Clamp(random(80, 120))` zurückgesetzt. Danach sind von 1360 Core-Tests nur die vier
  Abdrücke der Originaldatenbank rot. Die CI ohne Originaldaten bemerkt den Rückfall also nicht, und die synthetischen Abdrücke
  (Seed 3/11) bleiben gleich.
- **UI:** Die drei Spalten E werden auf den Stand von `c141fb6` zurückgesetzt (`p.Energy`). Danach sind alle 496 UI-Tests grün.

Vorschlag (Runde 13): ein synthetischer Test „`RollSlotFields` und `JoinScoutedTalent` liefern Belastung 80 … 120“ und ein
Bildschirmtest „Spalte E in Aufstellung, Verträgen und Transfer = Belastung · 10 / 15, Farbe 0x10 über 130“. Meine Proben unten
lassen sich dafür direkt übernehmen.

### R12N-02 (P3): Liste aus #491 nur teilweise umgesetzt, Rest nicht als Restlücke geführt

Laut PR-Text ist die „Liste aus #491“ umgesetzt. Die Übergabe in #491 nennt aber zusätzlich folgende Stellen, die am Head noch offen sind:

- [Player.Factory.cs:45](https://github.com/kaaasi/eishockey-manager-redone/blob/50cac4f42a1092a4babcdd12a3e904f9636121e0/src/EishockeyManager.Core/Player.Factory.cs#L45):
  Spielstart mit `Energy = 50 … 80`, also Belastung 70 … 100. Dass das nicht belegt ist, steht in #491 selbst.
- [Player.Factory.cs:83](https://github.com/kaaasi/eishockey-manager-redone/blob/50cac4f42a1092a4babcdd12a3e904f9636121e0/src/EishockeyManager.Core/Player.Factory.cs#L83):
  `Energy = TalentEnergy`, also Belastung 70. Das gilt für Talente, mit denen Computervereine ihren Kader auffüllen
  (`Transfer.Squads.cs:256`), und für den Rückgriff in `Transfer.Pool.cs:512`. Beides läuft weiter über die Energie.
- Hinweiszeile (`PlayerHintLine.cs:131`) und Balken (`PlayerInfoPopup.cs:85`) behalten das bisherige `[APPROX]` und rechnen nicht
  nach den Formeln aus #491 (Belastung · 10 / 15 bzw. (100 − Belastung) · 94 / 100). Das ist als `[APPROX]` korrekt markiert.
- `Player.Energy` ist nicht entfernt, und `Squad.Strength.PlayerValue` (`Squad.Strength.cs:45`) liest weiter `Energy`.

Keiner dieser Punkte gehörte zu den Gegenbeispielen von R12I-01, und an den sichtbaren Spalten ändert keiner etwas. Sie müssen
aber als Restlücke in PR #508 und in `docs/arbeitspakete.md` stehen. Abhilfe: eine Zeile in den Restlücken, etwa als Kandidat für
Runde 13.

## 3. Ausgeführte Prüfungen

**Zugriff/Einrichtung:** `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD` ok (`b509b32`).
Danach `tools/claude/fetch-reference.sh original` und `tools/claude/setup-workspace.sh ~/referenz/original/gamedata`, SDK 10.0.402.

**`tools/pruefen.sh` (alle drei Läufe) am Head `50cac4f`:**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2019 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1706 | 0 | 281 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1707 | 312 | 0 |

Ergebnis: bestanden. Die Werte decken sich mit der Angabe im PR. CI am Head: `build` success.

**Gegenbeispiele R12I-01:** Eigene Probe-Dateien, nur lokal und nicht committet, je in einem Worktree am alten und am neuen Head.

| Probe | Ablauf | `c141fb6` | `50cac4f` |
|---|---|---|---|
| Pool | `RollSlotFields`, 3000 Seeds → Belastung genau 80 … 120, alle 41 Werte | rot (min 51) | grün |
| Talentsuche | `JoinScoutedTalent(me, 60)`, `TestGames.Create(1…60)` → Belastung in 80 … 120, Maximum > 99 | rot (51) | grün |
| Spalte E (Aufstellung, Verträge, Transfer; je eine Theorie) | `StartNewGame`, Zeilen mit Belastung 70/140/131 → jede Zeile pixelgenau = `TextRight(Mini, x, y, Belastung·10/15)` in Artfarbe bzw. `0x10` über 130 (also 46, 93 rot, 87 rot) | rot (Zeile 0: „80“ statt „46“) | grün |

**Abdrücke allein durch R12I-01 erklärt** (Worktree am Head):

| Rücknahme | Ergebnis |
|---|---|
| `src/` komplett auf `c141fb6` | 4 Abdrücke rot, „Actual“ = alte Sollwerte (`842BCF1E4E4DB320`, `735F117A722426AD`, `0971EB079BD2E088`, `D21A19FFA304AAE8`) |
| nur die zwei Zeilen `Strain = Random16(80, 120)` (übrige Delta-Änderungen bleiben) | dieselben 4 rot mit denselben alten Werten; die übrigen 1356 Core-Tests grün |

Die übrigen Core-Änderungen im Delta (`Transfer.Pool.cs:511/529`) wirken sich also auf die Abdrücke nicht aus. Die synthetischen Abdrücke
bleiben unverändert, wie im PR begründet.

**Gegenproben (Mutation am Head):**

| Mutation | rot |
|---|---|
| zwei Zeilen zurück auf `Energy = Clamp(…)` | nur 4 Abdrücke der Originaldatenbank (R12N-01) |
| Spalte E in den drei Bildschirmen zurück auf `Energy` | keiner (R12N-01) |
| `!squad.NationalCoach` entfernt | 4 `WeltkugelDosboxTests` (R12I-02 zurückgezogen) |

**Akzeptierte `[APPROX]`:** Hinweiszeile und Balken mit `clamp(Belastung − 50, 0, 100)`. Der Wert ist gleich dem bisherigen,
die Formel aus #491 ist noch nicht übernommen (R12N-02). Rückgriff `Transfer.Pool.cs:512` mit Belastung 150 − 80 = 70 wie bei einem Talent.

**Annahmen (nicht geprüft):** Die Fundstellen 0x42E4C–0x42E5E und 0x42335–0x4236D in em.exe habe ich nicht neu disassembliert.
Sie stammen aus Code und #491. Die Spalte E der Nationalaufstellung ist durch den bestehenden DOSBox-Test (E = 46 bei Belastung 70)
belegt.
