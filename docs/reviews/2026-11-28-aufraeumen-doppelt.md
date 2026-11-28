# Review PR #1067 – Spur aufraeumen-doppelt (Erstreview)

Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext, Opus 5.5) · Base `270bd1e` · Head `10d2c07` ·
28.11.2026

## 1. Merge-Empfehlung

**Ja** für genau `10d2c07`. Verhalten unverändert belegt: Zustandsabdruck (Harness #1059) Base = Head bytegleich in allen
8 Abdrücken, Bildschirmfotos aller 79 Abläufe bis auf die schon auf Base nicht reproduzierbaren Temp-Ordnerzeilen gleich,
Markerprobe nur verschobene Fundorte, Testliste nur +2 Fälle (#1019). Alle drei Läufe von `tools/pruefen.sh` grün. Die
Zusammenlegungen K8/K10/K12/K19/U1/U3/U10 sind je Stelle gleichwertig (s. 3.), die neue Testnaht misst T03 (zwei Mutationen rot).
Einziger Befund P3 (Doku-Fundorte), kein Merge-Hindernis.

## 2. Befunde

**P3-1 – Doku nennt die aufgeteilten Dateien noch** (nur Doku, kein Verhalten)
- `docs/analyse/restliste-v1.md` (generiert; z. B. Z. 277 A895 `Manager.Einzelpunkte.cs:8`, Z. 349–352, 382–387
  `OriginalExport.Rest.cs`/`OriginalImport.Rest.cs`), `docs/analyse/abweichungen-bewertung-chatgpt.md:76/126/129`,
  `docs/analyse/aufraeumen-verweise.md:79–80`.
- Beleg: `grep -rnE "OriginalImport\.Rest|OriginalExport\.Rest|Manager\.Einzelpunkte" --include=*.md docs | grep -v "archiv\|reviews"`
  auf Head → diese Zeilen; die Dateien gibt es auf Head nicht mehr. `markerliste.py` ohne `--write` zeigt die neuen Fundorte.
- Vorschlag: in `aufraeumen-doku` mit `markerliste.py --write` und Nachziehen der beiden Analyse-Dateien (zusammen mit den im
  PR schon genannten `KeepSquadHelper`-Verweisen).

Keine weiteren Befunde.

**Erwartet offen – bestätigt:**
- K1 liegen gelassen: Begründung im PR nachvollziehbar (Verein vs. Bundestrainer verschieden), kein Code dazu im Diff.
- U12/U13 liegen: `SoundExport.cs` und die `Continue()`-Schleife in `HokExport.cs` nicht berührt.
- U10 im Import ohne Konstanten: `OriginalImport.cs:406–411` liest weiter `record[0x18F..]`, `0xEB`, `0xE4`, `0x116` als Literal
  (Feldkarten-Test sucht Literale); `HokDump`, `OriginalSaveGamePatcher`, `OriginalExport.Managers` nutzen die Konstanten.

## 3. Ausgeführte Prüfungen

Checkout: `git rev-parse HEAD` = `10d2c07bdec68d799f56f3eff88a8e135058e4db`, `git status --porcelain` leer; SDK 10.0.402;
Originaldaten aus `kaaasi/eishockey-manager-referenz` (`ls-remote` erreichbar). Base als Worktree auf `270bd1e`.

`tools/pruefen.sh` (alle drei Läufe, auf Head):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4004 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2812 | 0 | 673 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2813 | 1191 | 0 |

**Gegenbeispiele:**
- Zustandsabdruck: Harness aus der PR-Beschreibung #1059 unverändert in `tests/EishockeyManager.UI.Tests/` (Base- und
  Head-Worktree, nicht eingecheckt), `EM_REQUIRE_GAMEDATA=1 dotnet test … --filter Zz_MigrationHarness`; `diff -r` der 8
  Abdruckdateien Base/Head leer. Passiv `427150EB075036B9 D8DC90CA9CBDE043 19881337F32AFDA7 1FD1AA5409D04D4E`, aktiv
  `D9D7F4C45B5A87CB F77B150CD583D032 389D013F6D9496BA BBCD733F833309A6`, Rundreise je Saison „gleich“.
- Bildschirmfotos: `emtool screens gamedata <ziel> <ablauf>` für alle 79 Abläufe einzeln, Base und Head (Release-Build je
  Worktree). Je 342 Bilder, gleiche Dateiliste; 331 bytegleich. 11 verschieden (`speichern` 01–07, `speichern-original` 01–03,
  `hok-import/01`): nur Zeilen y 188–193 (`03_infofeld` zusätzlich ab y 77); zweiter Base-Lauf gegen ersten Base-Lauf zeigt
  dieselben Zeilen verschieden (Temp-Ordnername). `pixel-verein-bundestrainer` und `transfer-vertrag-kauf-saisonende` brechen
  auf Base und Head gleich ab (Rückgabe 134, U14).
- Markerprobe: `markerliste.py --repo <base|head>` ohne `--write`: 487 → 487 Markerzeilen, 1034 Tabellenzeilen; nach Entfernen
  der Fundort-Spalte Base = Head (`diff` leer) → keine ID verloren/neu, Wortlaut gleich, nur Fundorte (A895, A397, A403, A583,
  A584, A613, A615, A616 in die neuen Dateien, übrige Zeilenverschiebungen). `grep -c '\[APPROX\]'` über `src/` 487 = 487.
- Testliste (`dotnet test --list-tests`): Audio 32 → 32, Formats 171 → 171, UI 920 → 920, Core 2866 → 2868; `diff` nur
  `SaisonGeldT03Tests.SeasonEnd_DriftBeforeTheFortyNationThrows(mode: ClubOnly|Full)`.

**Schwerpunkte:**
- K8: `NationalTeamValuesOf(Club)` liefert `(cd, td, DefenseForm)`/`(ca, ta, AttackForm)` aus `NationalRatings(team.Index)` –
  dieselben Felder in derselben Folge wie der alte Zweig in `StrengthTable` (Tupel `(kd, td, ks, ts)`); `Of` setzt nur nach
  `double` um.
- K10: `TableRow.AddResult` wörtlich gleich den beiden entfernten `Add` (Liga, `CupGroup`), Aufrufe mit gleichen Argumenten.
- K12: `OpenPairingStillTied` – Pokal Grenze 2 wie `computed >= DebCupExtraRollPairings`; Play-offs Grenze 1 ⇔ alte
  `Any(Games.Count > played)`. `t != pairing` im generischen Helfer ist Referenzvergleich; `CupTie` und `PlayoffSeries` sind
  `sealed class` ohne `operator ==` (`grep -rn "operator ==" src` leer) → wie bisher. Mutation Play-off-Grenze 1 → 2:
  `WettbewerbeRegelnTests.FirstPlayoffPairingOfDate_OnlyBeforeAnyGameOfTheDate` rot.
- K19: jede ersetzte Stelle behält ihre Menge – `Managers` → `IsClubManaged` (Integration, TeamValues, MatchGate ×2 mit
  `!a && !b` = `!Any(a ∨ b)`, OriginalImport, AutoLineup, Lineup, Unsolicited ×2 inkl. `All(!=)`, Market `All(!=)`,
  Transfer), `ActiveManagers` → `IsClubActivelyManaged` (DailyRun, TacticsLeft, Live, Preparation, StatisticsStrength);
  `MatchEngine.IsManaged` behält den Bundestrainer-Zweig. Rest-Inline-Stelle nur `Tools/Scenarios/TransferAngebot.cs:20`
  (Welle 4).
- U1: `GameState.FormatMoney(long)` hat dieselbe Formel (`"#,0"`, de-DE, „ DM“) wie beide entfernten Fassungen; `int`-Aufrufer
  erweitern verlustfrei. Keine Restverweise (`grep` auf `TransferScreen.Money|ContractsScreen.FormatMoney` leer).
- U2/U3: `SeasonInfo.LeagueName` gleiche drei Texte; `SaveInfo` klemmt auf 0–2 wie zuvor. `NationalWidgets.FillInclusive` ordnet
  jetzt Ecken; alle 15 National-Aufrufer haben konstante x2 ≥ x1, y2 ≥ y1 → gleich (Fotos bestätigen).
- U10: Konstanten = alte Ausdrücke (0x97EA−0x965B = 0x18F, 0x973F−0x965B = 0xE4, 0x9746−0x965B = 0xEB, 0x9771−0x965B = 0x116,
  0x116+0x1D = 0x133, 0x116+0x21 = 0x137); `Rec16` jetzt mit Feldversatz statt DGROUP, Aufrufe entsprechend umgerechnet. Export-
  Bytes über `OriginalExportRoundTripTests` (Lauf mit Daten grün) und Abdruck-Rundreise.
- Teil 5: `git diff -M` zeigt `Manager.Einzelpunkte.cs → Manager.Season.cs` mit 100 % Ähnlichkeit. Zeilen-Multimenge
  `OriginalImport.Rest.cs` (Base) = Vereinigung der vier neuen Import-Dateien bis auf Gerüst (`using`, `namespace`, `partial
  class`, Klammern, Leerzeilen); Export ebenso plus ein Kommentarverweis (`OriginalImport.ManagerAreas.cs ff.`). `[APPROX]`-
  Wortlaut daher unverändert (Markerprobe oben).
- #1019: `SeasonEndObserver` ist `[ThreadStatic] internal static`, im Produkt nie gesetzt (`grep` nur Test), Aufruf `?.Invoke`
  ohne Wirkung; kein Instanzfeld → Abdruck/Spielstand unberührt. Test setzt/räumt im `finally`, läuft synchron (kein
  Threadwechsel). Mutationen in temporärer Kopie: (a) `DriftSeasonEnd` hinter die Ländermischung → beide Fälle rot („vor den
  Länderwürfen erst 0 Vereine gewandert“); (b) `Core.WorldCup.MixThrows(Random)` im Vereinsmodus entfernt → `ClubOnly` rot.
  Kein Endlos-Muster (Zeitleiste mit `PlayUntil`).
- U7/U8: `ManagedSquads` (Support) = alte Core-Fassung aus `SpielstaerkeManager2Tests.cs` (Klassenrumpf `diff` leer), UI-Fassung
  war gleich; `SquadKeeper.KeepSquads` = alte `KeepSquadHelper.KeepSquads` (gleiche Filter, gleiche `Negotiate`-Argumente).
  UI-Tests binden beide als `<Compile Include … Link>` ein; UI-Testdiff nur Namensersatz (9× `KeepSquads`, 10× `ManagedSquads`).
- #1066/Crefs: `CatchUpTraining`/`TrainOneDay` nicht mehr in `src/`. Da CS1574 aus ist, alle 27 im Diff neuen `cref`-Ziele per
  `grep` gegen Definitionen geprüft: alle vorhanden.
- Dateigrenzen der Spur eingehalten (`docs/arbeitspakete.md` Spur `aufraeumen-doppelt`); `Widgets.cs`, `GameState.cs`,
  `MenuTiles.cs`, `MainMenuScreen.cs` nicht berührt.

**Akzeptierte `[APPROX]`:** keine neuen (Markerprobe).
**Annahmen:** `Clubs[i].Index == i` (für `NationalTeamValues(int)` → `NationalTeamValuesOf(Clubs[nation])`) aus dem Aufbau
angenommen, nicht gesondert geprüft; der bytegleiche Abdruck mit WM-Modus deckt den Pfad praktisch ab.
