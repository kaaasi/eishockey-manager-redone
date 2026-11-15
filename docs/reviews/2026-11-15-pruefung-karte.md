# Review PR #863 – Spur `pruefung-karte` (Runde 19, Welle 2)

Erstreview, unabhängige Sitzung (keine Mitarbeit am PR), unbeaufsichtigter Lauf. Base `8b1dbd3b17fc5a752ebf207ae1333e7aa31c6307`,
Head `f3a1f1f7594c45b467349c800afc6a0ce6166e7d` (Checkout nachgewiesen: `git rev-parse HEAD` = Head, `git status --porcelain` leer).
`main` steht inzwischen auf `6011ac8` (#865, nur `docs/arbeitspakete.md`); `git merge-tree origin/main f3a1f1f` konfliktfrei.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Alle drei Läufe von `tools/pruefen.sh` grün; alle vier Gegenbeispiele lassen sich nachstellen und
sind behoben bzw. durch Tests abgesichert, die bei Rücknahme oder Mutation rot werden. Die Fundstellen zu #826 (0x5D59B,
0x31266) und #862 P3-3 (0x5680A, 0x568A9) stimmen mit dem Disassembly überein. Die Dateien außerhalb der Spur sind im PR
begründet und wirken nur wie beschrieben. Ein P3 (Zählerstand der Funktionskarte um eins veraltet) blockiert nicht.

## 2. Befunde

**P3-1 Funktionskarte am Head nicht mehr frisch.** [re-abdeckung.md:155](https://github.com/kaaasi/eishockey-manager-redone/blob/f3a1f1f7594c45b467349c800afc6a0ce6166e7d/docs/re-abdeckung.md#L155)
Beleg: `python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --write` am Head ändert
„7988 Zuordnungen“ → „7989“ (sonst kein Diff). Ursache: Commit f3a1f1f trägt nach der Erzeugung (86fb081) die Fundstelle
`0x5D59B` in `docs/arbeitspakete.md` ein; mit `arbeitspakete.md` aus 86fb081 erzeugt das Skript keinen Diff. Die Aussage
„zweiter `--write` ohne Diff“ gilt also für 86fb081, nicht für den Head. Vorschlag: bei der Integration bzw. der nächsten
Zählung neu erzeugen (auch `main` 6011ac8 ergänzt `arbeitspakete.md`); kein eigener Fixlauf nötig.

**Hinweis (kein Befund):** `GoalSceneCallKennungTests` steht als zweite Klasse in `GoalSceneCallZweiterTagTests.cs`; die
PR-Tabelle nennt die Klasse, aber keine Datei – beim Suchen auffällig, sonst ohne Wirkung.

### Gegenbeispiele

| ID | Ablauf | Ergebnis |
|---|---|---|
| #826 | `HighscoreWeltkugelTests` am Head grün; mit `Highscore.cs` aus Base 4 von 8 rot; Mutation `_ => 9 - 11` → `_ => 0` 3 rot. Disassembly 0x5D59B–0x5D5FE: Σ (9 − `[0x828B + 0x50·[0x6AC] + i]`) bis zur ersten 0; 0x31266–0x31291 schreibt 0x0B für alle 12 Länder; 0x311F8 rückt ab 16 WMs nach vorn (= `MaxWorldCupRankings`). Bild `file54_4`: „Spieler DEUTSCHLAND 5“. | behoben |
| #857 | Probe (temporärer Test, nicht committet): neues Spiel Saat 2, Joerg Verein 2, 3 Spieltage, `AddManager(„Neu“, Verein 7)` → Platz r = 8; `.ems` gespeichert, `emtool hok export … --force`, Dump (`emtool hok dump`: „Manager 1: Neu, Verein 7 EV LANDSHUT“) und Vereinssätze gelesen: Satz 7 „EV LANDSHUT“, Manager 2, Logo 0x8A (10 \| 0x80), Farbcode 15; Satz 4 „MANNHEIMER ERC“, Manager 0, Logo 0x0D, Farbcode 0 – Kennung wandert mit. Rücknahme von `OriginalExport.League.cs` → `NeuerManagerExportTests` rot (15 erwartet, 0 erhalten); Rücknahme von `GoalSceneCall.cs` → `GoalSceneCallKennungTests` rot (0 erwartet, 3840 erhalten). | behoben |
| #859 | Mutation `\|\| europe.Contains(c)` in `GameState.AddManager.Swap.cs:35` entfernt → `NeuerManagerEuropaTests` rot (1/1). | abgesichert |
| #862 P3-1/P3-2 | Mutationen aus `2026-11-15-torszenen.md`: `Number(spectators, 1, false)` → `true` (`GoalSceneCall.cs:82`) und `score = score;` (`LiveMatchScreen.cs:306`) → `GoalSceneCallZweiterTagTests` jeweils rot (1/1). | abgesichert |
| #862 P3-4 | `screens gamedata … live-torszene`: `02_szene_ende` zeigt „SC RIESSERSEE 1 : BERLINER SC 0“, 4. Minute. | behoben |

### Erwartet offen

- **#730:** offen, bestätigt (letzter Kommentar: Aufnahme `runde-19` liegt vor, Auswertung offen; jetzt Spur
  `abgleich-dosbox-19`, #866 Punkt 1).
- **#838:** Issue ist **geschlossen** (14.11.2026 durch Commit ddf6b91 „1/3 s je Spielminute“, Schätzung). Offen bleibt nur der
  Messwert aus `runde-19` – geführt in #866 Punkt 2. Inhaltlich wie erwartet, nur nicht als offenes Issue.

### Besonders geprüft

- **Dateien außerhalb der Spur:** `OriginalExport.League.cs` (#857: Name, Logo, Farbcode aus dem Satz der Kennung; die
  Namensbedingung ist gleichwertig zur alten, das Managerbit 0x80 bleibt am Platz), `GoalSceneCall.cs` (#857 Farbcode, #862
  P3-3 nur Kommentar), `Scenarios/Live.cs` (#862 P3-4, nur Szenario). Alle im PR je einem zugewiesenen Befund zugeordnet; keine
  gemeinsame Datei aus AGENTS.md §2.5.
- **`ClubIdentity`:** Probe über `kb\23`: 150 Sätze, keine doppelten Namen; `Club.Name` ist `init`. Weitere Stellen mit
  `database[club.Index]` für Name/Logo/Farbe: Codesuche (`grep -rnE "(Database|database|db)\[[a-zA-Z.]*(Index|club|Club)…\]" src`)
  findet nur die beiden umgestellten.
- **A466:** `[0x6AC]` = Deutschland als `[APPROX]` akzeptiert (Remake kennt keinen Bundestrainer eines anderen Landes).
- **Inventur „Stand Runde 19“:** alle 22 Zeilen A460–A481 zeigen am Head auf eine `[APPROX]`-Zeile; Zählung
  `grep -rc '\[APPROX\]' src/ | awk …` → `215 616` am Head (Base `215 615`, 926aeab `212 608`, 01ed8a5 `209 596`) wie
  angegeben. Zuordnung geprüft per `git blame`: A460/A461/A463 aus #854 (351dceb), A462/A465 und A464 aus #842 (1e43648,
  1fd7196, beide nach 01ed8a5); #855 (A409) und #856 (L7, A470–A474, F150/F152/F153) gemergt, wie eingetragen.

## 3. Ausgeführte Prüfungen

SDK 10.0.402 (Release `claude-werkzeuge`), Originaldaten aus dem Referenz-Repo. `tools/pruefen.sh` (alle drei Läufe) am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3307 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2421 | 0 | 567 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2422 | 885 | 0 |

Ergebnis: bestanden. Weitere Befehle: `ndisasm -b16 -e 0x5D590|0x311E0|0x56800|0x5689C gamedata/em.exe`; Mutationen und
Rücknahmen in einer temporären Arbeitskopie (`dotnet test <projekt> --filter …`, `EM_REQUIRE_GAMEDATA=1`), danach verworfen;
`funktionskarte.py --write` am Head und mit `arbeitspakete.md` aus 86fb081.

**Akzeptierte `[APPROX]`:** A466 (Land der Platzierungsfolge = Deutschland). Erweiterung A472 (Hallenstufe 2 entfällt mit Modus 2)
stimmt mit 0x5680A/0x568A9 überein.

**Annahmen (nicht geprüft):** Zeitpunkt, zu dem das Original die Folge 0x828B je WM schreibt (vor oder nach der Endrunde) – das
Remake zählt erst abgeschlossene WMs; FILE54 (Spielende nach der WM) ist damit vereinbar.
