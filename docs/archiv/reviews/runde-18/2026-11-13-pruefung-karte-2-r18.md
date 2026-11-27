# Review PR #833 – Spur `pruefung-karte` Teil 2 (Runde 18): Inventur, Funktionskarte, gesammelte P3

Erstreview, unabhängige Sitzung (geplanter Lauf, Claude Opus 5.5, ohne Implementierungskontext). Base `01ed8a5`, Head
`fc9b827812d3699332b1d363ac95773813089388`, Checkout nachgewiesen (`git rev-parse HEAD` = Head, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Ja** für Head `fc9b827`. Alle drei Läufe von `tools/pruefen.sh` grün (3215/0/0 mit Originaldaten). Die drei Gegenbeispiele
der Vorreviews werden jetzt rot. #824 ist eine reine Verschiebung: `LRandom.Next` rechnet über `LRandomCycle.Step/Value` mit
identischer Formel, die Zugfolge bleibt unverändert (Abgleich-Paare, Mehrsaisonproben, #773-Tests und Live-Tests im vollen
Lauf grün). Die Inventur „Stand Runde 18“ ist vollständig: alle 36 neuen bzw. verschobenen Markerzeilen sind erfasst, die
Gegenprobe `209 596` lässt sich nachrechnen. Die Funktionskarte lässt sich ohne Diff erneut erzeugen. Alle 20 neu umgesetzten
Spielcode-Funktionen stehen in der Inventur. Die neuen `[ORIGINAL]` in `reverse-engineering.md` sind mit `ndisasm` belegt.
Keine Befunde über P3.

## 2. Befunde

Keine P0–P2.

**P3-1 (Hinweis, kein Fix nötig): `LRandom_FollowsCycleStepAndValue` ist nach dem Zusammenführen tautologisch.**
[`tests/EishockeyManager.Core.Tests/PruefungKarteRunde18Tests.cs:14`](https://github.com/kaaasi/eishockey-manager-redone/blob/fc9b827812d3699332b1d363ac95773813089388/tests/EishockeyManager.Core.Tests/PruefungKarteRunde18Tests.cs#L14)
vergleicht `LRandom.Next` mit `LRandomCycle.Step/Value`, und `LRandom.Next` ruft genau diese beiden Methoden auf. Beleg
(Mutation in einer Kopie, `Step`: `+ 167` → `+ 169`, Filter LRandom/Penalty/PruefungKarteRunde18/LiveClock/Wink): rot werden
`LivespielKernClockTests.LRandom_FollowsOriginalFormula` und `Chain_PenaltyAfterChainDraw_OverCycle`, der neue Test bleibt grün.
Die Folge selbst ist also durch den älteren Test aus `livespiel-kern` geschützt. Der neue Test belegt nur die Weiterleitung, das
reicht für #824. Keine Aktion nötig.

**Beobachtung (kein Befund):** Die Mutation `Setup.DayActive || Row is not null` → `Row is not null`
([`MatchEngine.LiveStep.cs:72`](https://github.com/kaaasi/eishockey-manager-redone/blob/fc9b827812d3699332b1d363ac95773813089388/src/EishockeyManager.Core/MatchEngine.LiveStep.cs#L72))
bleibt in allen `LivespielKern*`-Tests grün (59/59). In den geprüften Fällen ist sie gleichwertig: Die Szenenwahl braucht eine
Managerpaarung (`MatchEngine.GoalScene.cs:126`), und die hat bei höchstens drei Managern immer eine Reihe. Abweichen würde sie
erst bei mehr als `MaxRows` markierten Paarungen (A445). Das liegt außerhalb von #825.

**Erwartet offen, bestätigt:**
- A053: `[APPROX]` steht weiter in `Core/GameOptions.cs:100` (Datei `livespiel-kern`); im PR als Restlücke für die Leitstelle
  genannt.
- #826 (Highscore „nur Weltkugel“): offen, `Core/Highscore.cs` nicht im Diff.
- Torszenen: F150, F152, F153 in der Inventur weiter „fehlt“ (Runde 19). 0x575C3 zählt nur über die Szenenwahl im Kern
  (`MatchEngine.GoalScene.cs:19/55`) als umgesetzt. Die Karte nennt „umgesetzt“ ausdrücklich eine Obergrenze, A439 bleibt
  `[APPROX]`.

**Dateigrenzen:**
- `LiveClock.cs` und `MatchEngine.LRandom.cs` werden nur verschoben (#824, der Spur zugewiesen).
- `PlayerInfoPopup.cs`: nur Kommentar.
- Fremde Testdateien:
  - `LivespielKernLiveStepTests.cs`: nur ein neuer Test, so wie in #825 verlangt.
  - `KaderTransferTeil4Tests.cs`: Regel statt Saat (#828). Das Verhalten ändert sich nicht. Eine Rückschaltprobe steht im PR
    und wurde hier nachgeprüft (s. u.). Das entspricht R15-E2 = C.
- `abgleich-dosbox.md` §2.5: neuer Abschnitt.
- `arbeitspakete.md`: nur der eigene Status.

## 3. Ausgeführte Prüfungen

SDK 10.0.402 (Release `claude-werkzeuge`). Originaldaten aus `kaaasi/eishockey-manager-referenz` (`ls-remote` erfolgreich).
`gamedata` → `~/referenz/original/gamedata`.

`tools/pruefen.sh` (alle drei Läufe, Head `fc9b827`, Ergebnis bestanden):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3215 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2358 | 0 | 538 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2359 | 856 | 0 |

**Gegenbeispiele und Mutationen** (in einer temporären Kopie des Heads, `EM_REQUIRE_GAMEDATA=1`):

| Mutation | Ergebnis |
|---|---|
| #817: `BarPixelLengths[3]` = `BarPixelsFromPercent(Clamp(Strain − 50, 0, 100))` (alte Formel) | UI 4 rot (alle Fälle von `InfoPopup_DrawnBarLengths_UseExhaustionPixels`), 745 grün |
| #818: `ExhaustionBarPixels` Divisor 99 | UI 2 rot: `FollowsOriginal(66, 31)`, `DrawnBarLengths(66, 31)` |
| #825 (PR-Probe): `match.Play(Minute, new GameOptions())` | 4 rot: alle Fälle mit Darstellung 1, alte LiveStep-Tests grün |
| #825: keine Erholung vor Minute 61 (`minute is 21 or 41 or 66`) | 5 rot: die 4 K.-o.-Fälle der neuen Theorie und `SinglePairing(DebCup)` |
| #825: `DayActive \|\| Row` → `Row` | grün (gleichwertig, s. Beobachtung) |
| #828: Listenwechsel nur bei jedem zweiten erfolgreichen Wurf (`ListChangeTurn`) | rot: „im Mittel nur 10.0 Halte“; ohne Mutation grün |
| #824: `LRandomCycle.Step` `+ 167` → `+ 169` | rot: `LRandom_FollowsOriginalFormula`, `Chain_…OverCycle` (s. P3-1) |

**#819:** `ndisasm -b16` auf `em.exe` ab Datei 0x041AC. Bei 0xA5B steht `mov ax,di`, bei **0xA63 `call 0xac9`**
(Balkenroutine), vorher 0xA45 `call 0x0:0x3ca` / 0xA4C `call 0x0:0x47c` mit 0x5E bzw. 0x64. Die Fundstelle im Kommentar stimmt.

**Hinweiszeile:** Datei 0x41B29–0x41B38: `mov al,[es:bx+0x13]`, `imul 0xa`, `idiv 0xf`, also n = +0x13 · 10 / 15. Damit ist
das neue `[ORIGINAL]` in `reverse-engineering.md` Z. 525 belegt, ebenso Z. 554 (Balken, s. #819).

**Inventur:**
- `git grep -c '\[APPROX\]'` über `src/`: `196 566` bei 81a358c, `209 596` bei 01ed8a5 und Head.
- Diff 81a358c..01ed8a5: 36 Zeilen mit `[APPROX]` hinzugefügt und 6 entfernt.
- Alle 36 Fundstellen stehen in der Inventur, als A438–A459 oder mit „ebenso“. Verschoben sind A401, A409 und A423
  (`Live.cs:141 → 193`).
- Alle 27 im PR genannten Zeilen (A370 … F151) tragen den Vermerk „Stand Runde 18“.

**Funktionskarte:**
- In der Kopie läuft `funktionskarte.py … --write` ohne Diff.
- CSV-Vergleich 81a358c ↔ Head: Im Spielcode gibt es 20 neue Zuordnungen „umgesetzt“: F025, F094, F134, F136–F149, F151,
  F154 und 0x575C3. F018 ist genähert. Das ergibt 268 → 288.
- Alle diese Funktionen stehen in der Inventur (Absatz „Stand Runde 18“ bzw. F-Zeilen).
- Die übrigen Wechsel (0x0228B, 0x105F8, 0x1063E, 0x11283) liegen in Systemsegmenten.

**Akzeptierte `[APPROX]`:** keine neuen. Die Kopplung von Kette und Abschluss bleibt bewusst unabhängig (A441, §2.5). Die
Testzahlen dazu halten die Analyse fest. Die Wirkung auf Tore je Spiel ist laut PR nicht gemessen, das ist eine Annahme und
nicht geprüft.
