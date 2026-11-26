# Erstreview PR #1049 – A1027 Penaltyschießen mit Darstellung 0

Reviewer: Claude Code (Opus 5.5), frische Sitzung ohne Implementierungskontext (geplanter Lauf, unbeaufsichtigt).
Geprüft: Base `67b851f`, Head `22186296c12af119395df54b646fb8fca8e9a5c7` (`git rev-parse HEAD` = Head, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Nein** für genau diesen Head – nur wegen R1 (P2): Der Regressionstest prüft die sichtbare Kernaussage von A1027 (Tafel zeigt
die Treffer des Penaltyschießens) nicht; eine Mutation, die `DrawBoard` den Penaltystand ignorieren lässt, bleibt grün. Ablauf,
Pausen und Zufallsfolge stimmen mit der Nachbildung (Teil H) überein; R26-E3 = A ist für Darstellung 0 korrekt umgesetzt.
Nach Behebung von R1 (nur Test) genügt die Delta-Prüfung der Leitstelle (`zusammenarbeit.md` §5 Punkt 4).

## 2. Befunde

**R1 (P2) – Bildprüfung des Tests ist selbstbezüglich.**
[`PenaltyDarstellung0UiTests.cs:112`](https://github.com/kaaasi/eishockey-manager-redone/blob/22186296c12af119395df54b646fb8fca8e9a5c7/tests/EishockeyManager.UI.Tests/PenaltyDarstellung0UiTests.cs#L112)
erzeugt das Erwartungsbild mit demselben `LiveMatchScreen.DrawBoard(…, frameScore)`, das geprüft werden soll. Die Folge
`between` prüft nur die Eigenschaft `ShootoutScore`, nicht das Bild.
Beleg (Mutation in temporärer Kopie): [`LiveMatchScreen.cs:823`](https://github.com/kaaasi/eishockey-manager-redone/blob/22186296c12af119395df54b646fb8fca8e9a5c7/src/EishockeyManager.UI/Screens/Live/LiveMatchScreen.cs#L823)
→ `var (homeGoals, awayGoals) = (report.HomeGoals, report.AwayGoals);` (Parameter `score` ignoriert, Tafel zeigt wieder den
Spielstand) – `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.UI.Tests -c Release --filter
"FullyQualifiedName~PenaltyDarstellung0UiTests"` → **Passed 1/1**; auch alle 72 Tests mit `Live|Penalty|Torszene|Shootout` grün.
Behebung: Erwartung unabhängig von `DrawBoard` bilden, z. B. Ziffern mit `LiveWidgets.Digit(…, frameScore.Home/Away …)` bei
x = 139/148/160/169 zeichnen, oder zusätzlich prüfen, dass das Bild von `DrawBoard(…, score: null)` abweicht (Spielstand ≠
Penaltystand ist im Aufbau gegeben, da erst nach dem ersten Treffer abgegriffen wird).

**R2 (P3) – Weg „Aufbau scheitert“ als `[ORIGINAL]` markiert, weicht aber ab.**
[`LiveMatchScreen.cs:283`](https://github.com/kaaasi/eishockey-manager-redone/blob/22186296c12af119395df54b646fb8fca8e9a5c7/src/EishockeyManager.UI/Screens/Live/LiveMatchScreen.cs#L283)
und der `else`-Zweig ([Z. 312](https://github.com/kaaasi/eishockey-manager-redone/blob/22186296c12af119395df54b646fb8fca8e9a5c7/src/EishockeyManager.UI/Screens/Live/LiveMatchScreen.cs#L312))
behandeln Darstellung ≠ 0 ohne `tore\` wie Darstellung 0. Beleg: `python3 tools/reverse-engineering/live/torszene_dauer.py
gamedata/em.exe`, Teil H „Darstellung 2, Aufbau scheitert“: je Schuss `Pause 30`, `Abspieler 0x567C8 Modus 0`, `Pause 20`, dann
ggf. `0x51FEA`, `Pause 80`; Aufbau `0x5353C(1, 0)` statt `(1, 1)`. Das Remake zeigt dort nur Pause 80 je Schuss. Das passt zur
bestehenden Vereinfachung ohne `tore\` (`GoalScene`, Z. 684, keine Pausen 30/20), ist aber nicht belegt original → Marker für
diesen Weg auf `[APPROX]` (ohne `tore\` keine Szenenpausen) setzen. Mit R1 im selben Fixlauf möglich, sonst Issue `P3`.

**Gegenbeispiel A1027 erneut ausgeführt:** Teil H „Darstellung 0“: `random(0, 1)`, Klick, `0x5353C(1, 1)` mit Stand 0:0, je Schuss
nur `random(0, 10)`, bei Treffer `0x51FEA(Reihe 0, 1)` mit Penaltystand 1:0 … 5:0, `Pause 80` je Schuss, danach `Pause 50`, Klick.
Remake (`LiveShootoutShots`): ein Bild Aufbau 0:0, je Schuss `PauseFrames(80)` mit Stand nach dem Schuss, `PauseFrames(50)` –
deckt sich; Test prüft die Folge exakt (Mutation „`else`-Zweig leer“ → rot, „Tafel ohne `_shootoutMatch`“ → rot).

**Kein Zufallszug in 0x53C57 mit Darstellung 0 bestätigt:** In Teil H sind die einzigen Aufrufe von `random` (0x0C037) der
Anstoß `random(0, 1)` und `random(0, 10)` je Schuss; unter den sonstigen gerufenen Zielen (0x389c/0x390b Zeichenketten, 0xbab2,
0xbecd, 0x35cbe, Text, Palette, 0x52BD6, 0x541d9) ist kein direkter Aufruf des Generatorkerns (0x0C037 ruft ihn über `call
0:06DF`). `MatchEngine.Shootout.cs` unverändert (`git diff --stat 67b851f..HEAD`), `LiveShootoutShots` zieht keinen Zufall →
Zufallsfolge, Ergebnisse und Statistik gleich.

**Darstellung 1 und 2 (T7 aus #1033) unverändert:** Zweig `LiveMode != 0 && GoalSceneCall.Available` unverändert; `DrawBoard`
erhält dort `null` (`_shootoutMatch` nur in `LiveShootoutShots` gesetzt, am Ende zurückgesetzt); alle T7-Tests im vollen Lauf grün.

**Dateigrenzen:** `LiveMatchScreen.cs` (+62/−11, nur Penalty-Zweig und optionaler Parameter `DrawBoard`), `LivePenaltyScreen.cs`
(nur Kommentar), `MatchEngine.Shootout.cs` nicht berührt, keine gemeinsamen Dateien; Doku/Skript in Spurdateien.
**Hängeschutz:** Test hat Abbruchzähler (`guard < 20000`, `i < 400000`, 16 Startwerte, `Assert.Fail` am Ende).

## 3. Ausgeführte Prüfungen

| Lauf (`tools/pruefen.sh`, alle drei) | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4072 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2882 | 0 | 671 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2883 | 1189 | 0 |

Commit `22186296c12a…`, SDK 10.0.402, Ergebnis: bestanden. Nachbildung: `torszene_dauer.py gamedata/em.exe` (Teile D und H).
Mutationen M1–M3 (s. o.) per `--filter PenaltyDarstellung0UiTests` mit Originaldaten.

**Erwartet offen – bestätigt:** Palettenüberblendung des Neuaufbaus (0x5353C mit Argument 1) nicht nachgebildet (ein Bild);
Weg ohne `tore\` ungetestet (Testdaten haben `tore\`; Abweichung dort siehe R2).
**Akzeptierte `[APPROX]`:** Torschütze 0x541D9 im Penaltyschießen zieht weiterhin nicht (bestehend).
**Annahmen:** Rückkehr der Tafel zum Spielstand nach dem Endergebnis (`ShootoutScore = null`) nicht gegen das Original
nachgebildet; Teil H endet mit `Stand Heim/Gast 38/3` (Ergebnisbyte nach dem Schießen) wie Teil D – bestehendes Verhalten, nicht
Gegenstand dieses PRs.
