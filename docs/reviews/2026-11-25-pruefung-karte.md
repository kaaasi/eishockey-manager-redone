# Review PR #1022 – pruefung-karte (P3 der Runde 25, Generatorlauf, Abweichungsliste)

Erstreview, Claude Opus 5.5 (frische Sitzung ohne Implementierungskontext, geplanter Lauf). Base `075dc2c`, Head `9150295`
(`git rev-parse HEAD` = `91502957bfcb3a070b4dd8ff9640c50d4155e6b7`, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Ja** für genau Head `9150295`. Die Regeländerung #1007 ist durch eine Nachbildung über die Bytes von em.exe belegt und durch
einen roten Test abgesichert; alle sechs Rückschaltproben der PR-Beschreibung machen ihre Tests rot; der Generatorlauf ist
reproduzierbar (ohne `--write` identisch zum Block, Exit 0); alle 69 X-Zeilen betreffen nur IDs, deren Marker in Runde 25
entfallen ist. Die drei Läufe von `tools/pruefen.sh` sind grün. Offen sind nur P3 der Doku (Abschnitt 2), keine davon wirkt auf
Code oder Tests.

## 2. Befunde

Kein P0–P2.

**#1007 (Regeländerung) geprüft, kein Befund.** `python3 tools/reverse-engineering/abweichungen/h1_highscore_liga.py
~/referenz/original/gamedata/em.exe` → `DGROUP 0x7C0 = [0, 12, 24]`; Bundesliga Platz 1/5/12 → 1/5/12, Zweite Liga → 13/17/24,
Oberliga → 25/29/36 (Exit 0). `ndisasm` Datei 0x55C0E–0x55C52: `mov [bx-0x698d],dl` (Platz), dann `mov al,[bx-0x68ba]`
(= 0x9746 + 0x1AF · m, Liga des Managers, vgl. Review 2026-11-04-integration-saison-r13 Z. 84/92), `mov al,[bx+0x7c0]`,
`add [bx-0x698d],al` – passt zur Nachbildung. Rückschaltprobe: [`Highscore.cs:161`](https://github.com/kaaasi/eishockey-manager-redone/blob/91502957bfcb3a070b4dd8ff9640c50d4155e6b7/src/EishockeyManager.Core/Highscore.cs#L161)
`h.Rank + 12 * (int)h.League` → `h.Rank`: `SeasonCodes_LeagueByte_AddsLeagueOffset` **4 rot, 2 grün** (Bundesliga). Gegenprobe
doppelter Versatz in [`HistoryScreen.cs:188`](https://github.com/kaaasi/eishockey-manager-redone/blob/91502957bfcb3a070b4dd8ff9640c50d4155e6b7/src/EishockeyManager.UI/Screens/Statistics/HistoryScreen.cs#L188)
(alte Rechnung zusätzlich): `FullyQualifiedName~History` 1 rot. **Gespeicherte Bestenlisten:** `high\h.<Schlüssel>` speichert
nur Punkte je Eintrag (`HighscoreTable`, 58 Byte); alte Einträge behalten ihren Wert, bis derselbe Manager/Verein neu eingetragen
wird (Ersetzen bei gleichem Namen und Verein, Datei 0x5D73D). Spielstände führen Liga und Platz in `ManagerSeasonHistory`, der
Code wird abgeleitet → keine Migration nötig; Dateiformat unverändert. Play-off- und Meistercodes (0x33–0x35, 0xB5) unberührt.

**P3-1 Klassenzeilen: vier entfallene IDs umgesetzter Themen nicht auf X.** In Runde 25 sind 74 IDs entfallen (Marker-Tabelle
`restliste-v1.md` Base gegen Head), 69 davon auf X; ohne X bleiben A102 (K), A404 (E), A460 (K), A486 (E), A907 (K). A460 ist
umformuliert (Nachfolger A976, Thema K13 „bleibt“) – Klasse bleiben ist vertretbar. Die anderen vier gehören zu umgesetzten
Themen: A102 und A404 (Rollenwert; K11, #1006: [`OriginalExport.Squads.cs:220`](https://github.com/kaaasi/eishockey-manager-redone/blob/91502957bfcb3a070b4dd8ff9640c50d4155e6b7/src/EishockeyManager.Core/OriginalExport.Squads.cs#L220)
schreibt `p.RoleValue` als `[ORIGINAL]`), A486 (B31, #1004: Verlassen ohne Bedingung, `CupsNational.GameState.cs:135`
`[ORIGINAL]`), A907 (K2, #1004: Nationalaufstellung fragt ab, `MatchEngine.PenaltyBoxQuery.cs:16`; Rest in A980). Abschnitt 3 von
`abweichungen-v1.md` führt sie weiter als „bleibt“ (C09, L03, C03, S08). Beleg: Abgleich-Skript unten (Prüfung 4), Ausgabe
`gone 74 … gone not X ['A102', 'A404', 'A460', 'A486', 'A907']`.

**P3-2 Abweichungsliste: 36 neue IDs ohne Thema.** A974–A1009 stehen in keinem Thema von `abweichungen-v1.md` Abschnitt 1/3/4
(Suche je ID über die Themenzeilen: 0 Treffer); Anhang „Vollliste“ steht noch auf `e0b0ea5`, die Probe in Abschnitt 5 („jede ID
der Vollliste steht in genau einem Thema“) gilt damit nicht mehr. Der Plan verlangte nur Abschnitt 0 – daher P3; nachziehen
zusammen mit P3-1 (z. B. Aufräumen #936 oder nächste `pruefung-karte`).

**P3-3 Abschnitt 0: zwei Formulierungen passen nicht zur Klassenliste.** K13: „A463 bleibt `[APPROX]`“ – A463 ist entfallen und
X, der Marker lebt als A977 (E) weiter; B29: „Managervereine (A699) keine Abweichung“ – A699 hat weiter Marker und Klasse K.
Nur Wortlaut (z. B. „A463 → A977“, „A699 Marker bleibt“).

**Erwartet offen bestätigt:** #1019 (offen, P3, T03-Stromlage; Begründung im PR, zurückgestellt) und #1018 (offen, P3,
Runde 26). Gegenbeispiele der PR-Beschreibung bestätigt (Prüfung 3).

## 3. Ausgeführte Prüfungen

1. Einrichtung: `tools/claude/fetch-reference.sh original`, `tools/claude/setup-workspace.sh ~/referenz/original/gamedata`,
   `dotnet --version` = 10.0.402; Zugriff Referenz-Repo: `git ls-remote … HEAD` → `7412a50`.
2. `tools/pruefen.sh` (alle drei Läufe) auf `9150295`, Ergebnis „bestanden“:

   | Lauf | bestanden | fehlgeschlagen | übersprungen |
   |---|---:|---:|---:|
   | Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3982 | 0 | 0 |
   | Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2814 | 0 | 652 |
   | Pflichtlauf ohne Daten (scheitert wie gewollt) | 2815 | 1167 | 0 |

3. Rückschaltproben (Worktree des Heads, `EM_REQUIRE_GAMEDATA=1 dotnet test <projekt> --filter …`), alle **rot**:
   - #1007 `Highscore.cs:161` ohne Versatz → 4/6 rot; doppelter Versatz `HistoryScreen.cs:188` → 1/4 rot.
   - #1010 `ClubInfoWindow.cs:309` `FormMood(form)` → `Widgets.Mood.Same` → 1/1 rot; `:321` `IsMainMenu` → `true` → 1/1 rot;
     `TeamInfoWindow.cs:315` `IsMainMenu` → `true` → 1/1 rot.
   - #1012 `SeasonFlow.cs:540` `Results.Clear();` → `;` → `TeamFormMultiSeasonTests` 2/2 rot.
   - #1014 `InfoBarWindows.cs:44/49` `Clicked` → `Clicked || RightClicked` → 1/1 rot.
   - #1016 `GameState.AddManager.cs:163` `BindLineupOwners();` → `;` → 1/1 rot.
4. Markerliste: `python3 tools/reverse-engineering/markerliste.py` → Exit 0, Ausgabe identisch mit dem Block in
   `restliste-v1.md` (Python-Vergleich `True`); `--write` in einer Kopie → `git status --porcelain` leer. Abgleich Base/Head
   (Python über `git show 075dc2c|9150295:` von `restliste-v1-klassen.csv` und der Marker-Tabelle): 69 neue X, davon 0 mit Marker
   im Head und 0 ohne Marker in der Base; 36 neue IDs A974–A1009 (K 24, E 12), alle klassifiziert; keine andere Klassenänderung.
   Stichprobe 15 X (A098, A165, A277, A285, A331, A348, A435, A695, A757, A829, A892, A936, A959, A961, A972): genannter PR
   ändert jeweils die Datei des Base-Markers, Thema laut Abschnitt 0 umgesetzt bzw. „nur Marker mit Beleg“. Klassen der neuen IDs
   gesichtet: E nur für Nutzerentscheidungen (B11 = A, N3 = B, #188) und Remake-Erweiterungen; Nachfolger übernehmen die Klasse
   der Vorgänger (A976 ← A460, A980 ← A907, A982 ← A102).
5. Abschnitt 0 gegen gemergte PRs (`gh api …/pulls/<nr>`): #998 (re-abweichungen, als Belegquelle genannt), #1002 B01/B07/B11/
   B12/B18, #1003 B10/B15/B16/B17/C07, #1004 B31/K07/K2/S07/B13, #1005 B25/B28/B29/B33, #1006 T03/F06/F08/K11/K13 – alle gemergt,
   Zuordnung stimmt.
6. CI auf `9150295`: `build` success.

**Akzeptierte `[APPROX]`:** keine neuen. **Annahme (nicht geprüft):** `ManagerSeasonHistory.League` ist die Liga der beendeten
Saison (wie 0x9746 vor dem Schreiben in Datei 0x56409); bestehende Tests der Historie stützen das.
