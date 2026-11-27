# Review PR #814 – Spur `livespiel-kern` (A1–A5)

Reviewer: Claude Opus 5.5 (geplanter Lauf, frische Sitzung ohne Implementierungskontext) · 13.11.2026
Geprüft: Head `57be39b16e6ef67a1b9a6027342324d174c0db85` (`git rev-parse HEAD`, `git status --porcelain` leer) gegen Base
`5367ba84614e9c57aefc85f0cab348aaebc6a1fd`. SDK 10.0.402, Originaldaten aus `kaaasi/eishockey-manager-referenz` (Zugriff ok).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head – in der Reihenfolge des Rundenplans **nach** #812 (`abgleich-dosbox`). Probemerge
`57be39b` + #812 (`584f318`) + `main` (`c68b71b`) ist konfliktfrei, betroffene Tests dort grün (siehe 3). Keine P0–P2.

- (1) A5: `LiveMatchDay` ohne Eingriff ist ergebnisgleich zu `PlayWithEvents` – auch über die Tests des PR hinaus mit
  mehreren Managern, Play-off/DEB-Pokal (Verlängerung, Penaltyschießen, schneller Weg) und mit Anzeigezügen (Darstellung 1,
  Geschwindigkeit 40): ganzer Spielstand (`SaveGame.Save`), Berichte, Szenen und Strafgründe gleich (Probe P-1). Die
  Herauslösung `BeginMatch`/`FinishMinuteLoop`/`EndMatch` ist eine reine Verschiebung (Spielwertung: dieselben Aufrufe
  `CreditPlayers`/`AfterMatch` ohne `KeepMatchFields`; Penaltyschießen vor den Vereinsfeldern wie vorher); alle
  bisherigen Tests inkl. Abgleich-Paare und Mehrsaisonproben grün. Eingriff nach Minute m wirkt ab m + 1 (Test des PR prüft
  Minuten 1 … m auf Gleichheit und die Stärken der Minute m + 1).
- (2) A4: Zugfolge 0x575C3 (Datei 0x575E9–0x57643: `random(2,[0xE2F6])`, `random(0, Art ? 0xD2 : 0xFF)`, dann Penalty
  bzw. sechs) und Bedingung der Torszene (Aufruf aus 0xAB90 nur mit Reihe – `[bp-9]` ≠ 0x1E, Abbruch 0xA642 springt
  vorher weg; `[0x226]` ≠ 0 bei 0x53DDC; 0x5A56/0x5A55 bei 0x53F75) nachgeprüft; im Abspieler 0x567C8 bis 0x56BBC kein
  weiterer `0x38:0x847`. Strafgrund: Aufruf 0x35C1C–0x35C73 nur nach gewähltem Spieler (keiner → 0x35C19 `jmp 0x583B`),
  Bedingung Vereinsfeld 0x16 von Verein oder Gegner, im Reporter `[0x5A56]` und `[0x262]` < 0x46 (0x5252A–0x5253B),
  5-Minuten-Strafe (Typ 3) Index 5 ohne Zug (0x52A8E–0x52A94), sonst `random(0, 12)` (0x52AA1). Code entspricht dem
  ([Minutes.cs:424](https://github.com/kaaasi/eishockey-manager-redone/blob/57be39b16e6ef67a1b9a6027342324d174c0db85/src/EishockeyManager.Core/MatchEngine.Minutes.cs#L424),
  [Minutes.cs:562](https://github.com/kaaasi/eishockey-manager-redone/blob/57be39b16e6ef67a1b9a6027342324d174c0db85/src/EishockeyManager.Core/MatchEngine.Minutes.cs#L562)).
  `MatchEngine.Minutes.cs` hat genau diese zwei Einfügungen (+5 Zeilen inkl. Kommentar), sonst nichts – benannte
  Ausnahme R809 P2-1 eingehalten. #812 ändert in `PenaltyMinutes` nur die Verteilung bei gleicher Zahl der Züge; der
  Strafgrund liegt danach, also verträglich (Probemerge grün).
- (3) A1–A3: Minute 0x37C6B–0x37CBD (lrandom(0,4) über `0x38:0x7E5`, 30/45 − `[0x262]` sar 1), Pause 0x130EE (Langwort / 3),
  Eingriff 0x38ECD–0x38FBE (Einsatz = Treffer − 1 nach 0x26, Angriff Bits 4–6 mit Maske 0x8F, vierte Reihe nur über
  `0x28:0xD9F`) nachgeprüft und stimmig. Kern ohne Bildschirm vollständig, kein Zeitfortschritt außerhalb
  `GameState.Continue()` (Minutenschritt rechnet nur auf Befehl, kein Kalender), keine fremden Dateien außer den
  benannten Ausnahmen (`MatchEngine.Live.cs` A5 nach R18-E1, `Minutes.cs` R809 P2-1); `docs/arbeitspakete.md` nur eigener
  Status.

## 2. Befunde

**P3-1 – `LiveClock.Minute` liefert negative Pausen.**
[LiveClock.cs:119](https://github.com/kaaasi/eishockey-manager-redone/blob/57be39b16e6ef67a1b9a6027342324d174c0db85/src/EishockeyManager.Core/LiveClock.cs#L119):
mit Zwinkern ist `30 − g/2` für g = 62 … 69 negativ. Beleg (Probe P-2, `LiveClock(new GameOptions { MatchSpeed = g })
.Minute(true)` bis zum ersten Zwinkern): g = 62 → Einheiten −1, Ticks 0; g = 68/69 → Einheiten −4, **Ticks −1**. Das
Original übergibt denselben vorzeichenbehafteten Wert (0x37CB2–0x37CB7, `cwd`) und teilt in 0x130EE durch 3 → −1 an
`0xBBF:0x2C08`; was der Zeitgeber damit tut, ist nicht belegt. Kein Spielwert betroffen. Vorschlag: im Doc-Kommentar
festhalten und für B2 entweder „≤ 0 = keine Pause“ als `[APPROX]` oder den Zeitgeber nachprüfen.

**P3-2 – lrandom zweimal nachgebaut (nach Merge mit #812).**
[LiveClock.cs:145](https://github.com/kaaasi/eishockey-manager-redone/blob/57be39b16e6ef67a1b9a6027342324d174c0db85/src/EishockeyManager.Core/LiveClock.cs#L145)
`LRandom.Next` und `LRandomCycle.Step/Value` aus #812 (`MatchEngine.LRandom.cs`) rechnen dieselbe Folge
(`(16x + 0xA7) mod 4 000 000`, `x·(b−a+1)/4 000 000 + a`). Beleg: `git show pr812:src/EishockeyManager.Core/MatchEngine.LRandom.cs`
Zeilen 18–24 gegen LiveClock.cs 145–157. Kein Namenskonflikt, Build des Probemerges grün. Hinweis an die Leitstelle:
beim Integrieren auf eine Stelle zusammenführen (oder gegenseitig verweisen), sonst laufen künftige Korrekturen auseinander.

**P3-3 – Gleichheitsprobe A5 nur mit einem Manager im PR.** Die Tests des PR decken einen Manager, Liga/DEB-Pokal und
Darstellung 0 ab; die im Auftrag genannten Fälle (mehrere Manager, Play-off, Anzeigezüge) belegt erst Probe P-1 (alle grün).
Empfehlung: Probe P-1 (oder eine reduzierte Theorie) in `LivespielKernLiveStepTests` übernehmen, da B2/B3 sich auf die
Gleichheit stützen. Das `[APPROX]` „Vorbereitung aller Paarungen vor Minute 1 kann bei mehreren Managern abweichen“ trat in
270 Spieltagen der Probe nicht auf; es bleibt als theoretischer Fall offen.

## 3. Ausgeführte Prüfungen

| Lauf (`tools/pruefen.sh`, ohne Option, Head `57be39b`) | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3109 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2304 | 0 | 518 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2305 | 804 | 0 |

Ergebnis: bestanden, SDK 10.0.402.

- **P-1** (temporäre Datei `ReviewProbeLiveStepTests.cs` in einer Kopie, nicht committet): 18 Fälle = Manager {2}, {2,3},
  {2,5,7} × Liga / Play-off (K.-o.) / DEB-Pokal (K.-o.) × (Darstellung 0, g 70) / (Darstellung 1, g 40), je 15 Seeds;
  ganzer Spieltag `LiveMatchDay.Begin(…).Finish()` mit eigenem `GameRandom` je Paarung gegen Paarung für Paarung
  `PlayWithEvents`; verglichen Berichte (inkl. Torschütze/Vorlage), `SaveGame.Save`, Reihenzahl, `Scenes`,
  `PenaltyReasons`. `dotnet test tests/EishockeyManager.Core.Tests --filter FullyQualifiedName~ReviewProbe` → 19/19 grün;
  mit Anzeige z. B. {2,5,7} Liga: 2 Reihen, 33 Szenen, 22 Strafgründe gezogen.
- **P-2** (gleiche Datei): Pausen bei g = 60/62/68/69 mit Zwinkern → 0/0, −1/0, −4/−1, −4/−1 (P3-1).
- **Gegenproben (Mutation in Kopie, `--filter FullyQualifiedName~LivespielKern`, je 53 Tests):** Zugfolge in `Choose`
  getauscht → 3 rot; Strafgrund `>= 70` → `> 70` → 1 rot (`RandomSequence_WithAndWithoutDisplay`); Prüfung vierte Reihe
  entfernt → 1 rot (`FourthLine_WithoutPlayer_NoChange`); Drittelzähler bis Minute 70 → 2 rot (`PeriodOf_Minute`).
- **Rebase-Verträglichkeit:** `git merge pr812` und `origin/main` auf `57be39b` konfliktfrei; dort mit Originaldaten
  `--filter Livespiel|AbgleichDosbox|SpielberechnungMinuten|RngTwin|AbgleichOriginalPaare` → 169/169 grün (kein voller Lauf).
- **Disassembly:** `ndisasm -b16` an den in 1 genannten Dateiadressen.

**Akzeptierte `[APPROX]`** (laut PR „Restlücken“, bestätigt): Markierung = Managerverein/Land des Bundestrainers;
Reihenliste ohne gemeinsamen Spieltagseinstieg, höchstens 4 Reihen; Zwinkerzufall getrennt von der Spielrechnung; Strafe
ohne wählbaren Spieler fehlt in der Strafenliste „bis Minute m“; Szenenwiederholung „r/w“ und Rückkehr der Torszene bei
< 4 fitten Spielern (0x53D7A `120h:66h`, nachgesehen – überspringt im Original auch den Torschützen 0x541D9); A5 Zufall je
Paarung und Vorbereitung aller Paarungen vor Minute 1. Offen nach Plan: Live-Bildschirm B2/B3 (Welle 2).

**Annahmen (nicht geprüft):** `0x138:0x25` = 0x575C3 und `0x14E1:0x7F` = 0x53C57 (Thunks, aus der Funktionskarte
übernommen); Vereinsfeld 0x16 ≙ `LiveTeam.Managed` auch für das Land des Bundestrainers; Leser `Formats/GoalScene.cs`
nur über die `[GameDataFact]`-Tests (48 Dateien) geprüft, nicht gegen das Disassembly.
