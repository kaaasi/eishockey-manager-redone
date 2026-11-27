# Review PR #1047 – Spur `pruefung-karte` (P3 der Runde 26, Teil 1 und 2)

Prüfer: Claude Sonnet 5.5 (frische Sitzung, geplanter Lauf; keine Mitarbeit am PR) · Base `67b851f` · Head `9890f9e34521ddfaf94acb8d163837266b50ce67`
Stand verifiziert: `git rev-parse HEAD` = Head, `git status --porcelain` leer, SDK 10.0.402, Originaldaten aus `kaaasi/eishockey-manager-referenz`.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Kein P0–P2. Zwei P3 (falsche PR-Zuordnung in zwei Klassenzeilen), nicht blockierend.

## 2. Befunde

| ID | P | Befund | Beleg |
|---|---|---|---|
| R1047-01 | P3 | `restliste-v1-klassen.csv` A1010: Beleg „live-seiten-rest (PR #1042, Teil 2b)“, der Marker `Core/CupsGroup.cs:126` stammt aber aus `8b1b92e` (live-verschraenkt Teil 2b, **#1032**). | `git blame -L126,126 src/EishockeyManager.Core/CupsGroup.cs` → `8b1b92e5f`; `git log --merges --ancestry-path 8b1b92e..origin/main` → #1032 |
| R1047-02 | P3 | A1029 nennt „live-verschraenkt (#1032, Teil 3), torszene-dauer (#1033)“; der Marker `UI/Screens/Live/LivePageScreens.cs:41` stammt aus `3556c26` (live-seiten-rest, **#1042**). | `git blame -L41,41 …/LivePageScreens.cs` → `3556c2684` |

Nur Dokumentation (Beleg-Spalte); Klasse bleibt jeweils K, kein Einfluss auf Zählung oder Generatorblock. Behebung beim nächsten
Generatorlauf oder von der Leitstelle.

## 3. Ausgeführte Prüfungen

**`tools/pruefen.sh` (Erstreview, alle drei Läufe), Commit `9890f9e`, Ergebnis: bestanden**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4074 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2885 | 0 | 670 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2886 | 1188 | 0 |

**Gegenbeispiele / Mutationsproben** (Mutation im Arbeitsbaum, `dotnet test --filter`, danach `git checkout -- src`, Baum wieder sauber)

| Befund | Mutation | Ergebnis |
|---|---|---|
| #1036 `WorldCupLoopBuildDay_BuildPerManagerAfterItsRating` | `WeeklyManagerFinances` in der WM-Schleife (`GameState.Manager.cs`) → `WeeklyFinances()` hinter der Schleife | rot (Failed 1) |
| #1036 `SummerDay_BuildBeforeDayClosePerManager` | Bau nach `DayClose` in `RunSummerDay` (`GameState.DailyRun.cs`) | rot (Failed 1) |
| #1041 `KnockOutDay_ShootoutsOnlyAfterMinute70OfAllPairings` | `completed.Add(match)` → `match.Complete()` in `MatchEngine.LiveStep.cs` (Schritt mit Reporter) | rot (Failed 1) |
| #1045 `PromotionDay_BothGroups_RunningScore_NoDates` | `&& game.Options.Reports[6 + …]` aus `LivePageScreens.For` entfernt | rot (Failed 1) |
| #1037 | nur Kopf von `stadionschaeden.py`; `TrainingRules.LevelValue(level) => 5 - level` (`Squad.Training.cs:218`) stimmt mit „d = 5 − Spielstufe“ überein; `OriginalExport.cs:247` schreibt 0xED8 = 5 − level | bestätigt |

Hängeschutz/Endlosfall: Die neuen Tests laufen mit begrenzter Saatschleife (400) und `Assert.True(found, …)`; kein „Original zieht endlos“.

**Weitere Punkte des Auftrags**
- `python3 tools/reverse-engineering/markerliste.py` (ohne `--write`) auf dem Head: Rückgabe 0, `git status --porcelain` leer → Generatorblock unverändert.
- Kein Produktcode: `git diff --stat 67b851f HEAD -- src/EishockeyManager.Core src/EishockeyManager.UI` leer; geändert nur Tests, `WettbewerbeVideo.cs` (Tools-Szenario), Kopf von `stadionschaeden.py`, Doku.
- 15 X-Zeilen (A278, A436, A442, A448, A450, A454, A456, A457, A472, A475, A547, A766, A909, A910, A954): Beleg nennt PR (#1032, #1033, #1042) und Abschnitt in `live-spieltag.md`/`torszene-dauer.md`; alle vier PRs (#1032, #1033, #1034, #1042) sind auf `main` gemergt.
- 23 neue IDs A1010–A1032: Ursprungs-PR je Marker per `git blame` ermittelt. 21 stimmen mit dem Beleg überein (A1011/A1012 #1034; A1013–A1014, A1017–A1018, A1022, A1025–A1027, A1031 #1033; A1015–A1016, A1019–A1020, A1023–A1024, A1028, A1030, A1032 #1032; A1021 #1042); Abweichungen siehe R1047-01/-02.
- Status in `abweichungen-v1.md` Abschnitt 0: S01 (#1032), S02 (#1033), S04 (#1032, #1042), S05 (#1032), SG-01 (#1034, #1018 geschlossen) stimmen mit den Merges überein; die Reste verweisen auf vorhandene IDs.
- `pixel-europapokal` rendert (`dotnet run --project src/EishockeyManager.Tools -- screens gamedata <ziel> pixel-europapokal`); `eisbaeren_f51_03346.png` und `eisbaeren_f58_01323.png` sind byte-gleich (md5) mit denen aus `live-aufstieg-europapokal`, wie in der PR-Beschreibung behauptet.

**Erwartet offen (bestätigt, nicht geprüft):** #1019, #1024, #978, #980 (Aufräumen #936). **A1027:** als K eingeordnet, sichtbar, aber selten
(Penaltyschießen einer zugesehenen Paarung mit Darstellung 0, Marker `LiveMatchScreen.cs:282`); ob K genügt, entscheidet der Nutzer
über die Leitstelle.

**Akzeptierte `[APPROX]`:** keine neuen im PR. **Annahmen/ungeprüft:** inhaltliche Richtigkeit der Beleg-Texte der Klassenzeilen gegen em.exe
(nur PR-Zuordnung und Marker-Herkunft geprüft); CI-Lauf auf dem Head nicht eingesehen (nur Doku/Tests/Tools-Szenario → Prüfung lokal).
