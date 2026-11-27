# Nachprüfung: Integration Runde 14, Spur `saison` (PR #652)

Nachprüfung des Reviews `docs/reviews/2026-11-08-integration-saison-r14.md`, bisher geprüfter Head `194f387e60fd0220f570c5867c2d1a55b21f5c0b`,
Delta `194f387..9f49b7e` (1 Commit, 5 Dateien, +105/−25) und Befunde R14S-01 bis -03.
Head `9f49b7e785578e2cf190948b59bc20939630dcbd` · Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext) · 08.11.2026 ·
Checkout: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer. Codelesen **und** Ausführung am nachgewiesenen Checkout
mit Originaldaten.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. R14S-01 ist behoben: Der Merker wird einmal je Tag gebildet, am Pokaltag mit Managerverein rechnet keine
Paarung mehr im schnellen Weg, über 4 Saisons kippt er an keinem Tag. R14S-02 ist behoben: Beide Mutationen des Aufrufs machen je einen
Test rot. R14S-03 ist behoben. Bei „aus“ ist der gesamte Spielstand nach jeder von 4 Saisons gleich Base `b7e1eb2`. Der statische
Merker je Thread ist für alle geprüften Fälle sicher. Die nicht statischen Alternativen sind nicht besser (siehe R14N-01, nur Hinweis).

## 2. Befunde

### R14S-01 (P1) – behoben

[`SeasonFlow.cs` Z. 182–193](https://github.com/kaaasi/eishockey-manager-redone/blob/9f49b7e785578e2cf190948b59bc20939630dcbd/src/EishockeyManager.Core/SeasonFlow.cs#L182-L193) setzt den Merker zu Tagesbeginn auf `false`, bildet
ihn in [Z. 215](https://github.com/kaaasi/eishockey-manager-redone/blob/9f49b7e785578e2cf190948b59bc20939630dcbd/src/EishockeyManager.Core/SeasonFlow.cs#L215) **einmal** nach `CheckDebts`/`MarkShortHandedClubs` und vor allen Spielen
des Tages und setzt ihn im `finally` zurück. [`MatchEngine.Live.cs` Z. 91](https://github.com/kaaasi/eishockey-manager-redone/blob/9f49b7e785578e2cf190948b59bc20939630dcbd/src/EishockeyManager.Core/MatchEngine.Live.cs#L91) liest
nur noch diesen Wert. Gegenbeispiel erneut ausgeführt (Kopie mit Protokoll je `PlayWithEvents`-Aufruf, Werte unten):

- Seed 5, Köln (5), „Ganzer Tag“ an, Tag 12 (DEB-Pokal): 16 Paarungen, davon 1 mit Manager. Im schnellen Weg: **0** (vorher 13).
  Merker je Paarung `1111111111111111`. Die alte Bildung je Paarung hätte `1110000000000000` ergeben.
- 4 Saisons, nur `Continue`, Einstellung an, 6 Szenarien (Seed/Verein 5/5, 9/30, 3/1, 21/14, 7/0, 12/18): je 778–792 Aufrufe außerhalb
  der Liga. **0 Tage**, an denen der Merker innerhalb eines Tages verschiedene Werte hat. **0 Aufrufe** im schnellen Weg bei gesetztem
  Merker. Die alte Bildung je Paarung wiche in 62–95 Aufrufen je Szenario ab. Die Probe deckt die Fälle von R14S-01 also ab.

`[ORIGINAL]` an der Merkerzeile ist damit akzeptiert. Die Fundstellen im Kommentar `SeasonFlow.cs` Z. 172–178 stimmen mit dem Review
überein.

### R14S-02 (P2) – behoben

- [`ManagerPlaysCupDay_OptionOn_AllPairingsUseMinuteLoop_RandomDiffers`](https://github.com/kaaasi/eishockey-manager-redone/blob/9f49b7e785578e2cf190948b59bc20939630dcbd/tests/EishockeyManager.Core.Tests/SaisonManagerTagTests.cs#L80)
  prüft jetzt `fastOn == 0` und `loopOn >= fastOff + loopOff`. Mutation „Merker je Paarung neu“ (Z. 91 wie am alten Head
  `game.Options.ManagerDayMinuteLoop && game.ManagerPlaysOn(game.CurrentDay)`): **rot** ✔.
- [`NoManagerInvolved_OptionDoesNothing_SameRandom`](https://github.com/kaaasi/eishockey-manager-redone/blob/9f49b7e785578e2cf190948b59bc20939630dcbd/tests/EishockeyManager.Core.Tests/SaisonManagerTagTests.cs#L97)
  vergleicht zwei Klone desselben Stands über einen `Continue` mit Pokaltagen ohne Manager. Mutation M4 (`ManagerDayFlag =
  Options.ManagerDayMinuteLoop;`, Bedingung „Managerverein spielt“ weggelassen): **rot** ✔.
- Nur Schönheit, kein Befund: Die Variablen `kind`/`checkedKinds` sind überflüssig. Wegen `Count < 1` wird nur ein Aufruf geprüft.

### R14S-03 (P3) – behoben

`GameOptions.cs` Z. 78–79 und `MatchEngine.Live.cs` Z. 81–84 kennzeichnen den Teil „auch die WM-Endrunde“ jetzt als `[APPROX]` mit
Verweis auf R9S-07/#358. Akzeptiert.

### R14N-01 (P3, Hinweis) – Merker statisch je Thread: sicher; Alternativen nicht besser

Geprüft am Code und durch Läufe:

- **Mehrere `GameState`-Instanzen im selben Thread:** Der Merker gilt nur zwischen Anfang und Ende von `ProcessDay`. Der Tag läuft
  synchron. `ProcessDay` hat genau einen Aufrufer (`RunDays`, Z. 149) und ruft sich nicht selbst auf. Eine zweite Instanz kann
  also während des Tages einer anderen nicht rechnen. Die Tests wechseln Instanzen nur zwischen den Tagen (`off`/`on`, Klone `a`/`b`
  aus `SaveGame.Load`). Jeder Tag bildet den Merker neu, Zurücksetzen beim Start und im `finally`.
- **Hot-Seat:** `Continue` eines Managers, der nicht zuletzt am Zug ist, endet in `PassTurn` ohne `ProcessDay`. Kein Spiel, kein Merker.
- **Aufrufer außerhalb des Tages:** Der einzige ist `PlayFriendly` (`CupsNational.GameState.cs` Z. 218, aus dem UI). Dort ist der Merker
  `false`. Ohne Folgen: Die Nation des Bundestrainers zählt als `IsManaged` (`MatchEngine.Live.cs` Z. 143–145), das Spiel läuft also
  ohnehin Minute für Minute. Ohne das Zurücksetzen bekäme ein späterer direkter Aufruf von `PlayWithEvents` im selben Thread, etwa in
  einem Test, den Wert des letzten Tages. Das Zurücksetzen ist also nötig und vorhanden.
- **Ausnahme mitten im Tag:** `finally` setzt zurück (Z. 189–192). Der halbe Tag bleibt wie vorher halb gerechnet. Das ist nicht neu.
- **Parallelität:** In Core und UI gibt es weder `Task.Run`, `Parallel` noch `async`. Der Tag wechselt also nicht den Thread. xUnit 2.9
  läuft mit der Vorgabe Parallelisierung zwischen Klassen, ohne `xunit.runner.json` oder `CollectionBehavior`. `[ThreadStatic]`
  trennt die Threads. Belegt: Probe und `SaisonManagerTagTests` liefen parallel im selben Prozess grün, ebenso alle drei Läufe von
  `tools/pruefen.sh`. Vorbild im Repo: `Transfer.SeasonEnd.cs` Z. 160 `s_deferRenewals`, dasselbe Muster.
- **Alternativen:** Ein von `SaveGame` ausgenommenes Instanzfeld gibt es nicht. `SaveGame.Fields` nimmt alle Instanzfelder,
  `CheckSchemaCoverage` gilt ausdrücklich für „ausgenommen ist nichts“ (`SaveGame.cs` Z. 222–243, 289–292). Ein Ausnahmemechanismus
  wäre eine Änderung an gemeinsamer Schema-Infrastruktur samt Fingerabdruck und für einen Tageswert unverhältnismäßig. Ein Parameter
  müsste durch `PlayNextMatchDay`, `Playoffs.PlayDate`, `RelegationGroup.PlayDate`, `Competition.PlayDay` → `Cups.Tiebreak`,
  `CupsGroup` und `CupsWorld` laufen: sieben Aufrufstellen in Dateien anderer Spuren. Das ist nicht besser.
- **Hinweis, keine Bedingung:** Wer den Merker zusätzlich an die Instanz binden will, kann `[ThreadStatic] static GameState?
  s_managerDay` mit `game == s_managerDay` in `PlayWithEvents` verwenden statt eines `bool`. Dann sieht eine fremde Instanz nie den
  Merker einer anderen. Das ist heute nicht erreichbar, daher P3.

### Testhaken `OriginalMinutes.FastCupCalls`/`MinuteLoopCalls` – ohne Befund

`[ThreadStatic] public static int` ([`MatchEngine.Minutes.cs` Z. 586–592](https://github.com/kaaasi/eishockey-manager-redone/blob/9f49b7e785578e2cf190948b59bc20939630dcbd/src/EishockeyManager.Core/MatchEngine.Minutes.cs#L586-L592)),
nur erhöht und nie gelesen außer in Tests. Kein Spielzustand. Statisch, also nicht im Spielstand, das Schema bleibt unverändert. Je Thread,
also unter xUnit-Parallelität ohne Übersprechen. Die Tests setzen die Zähler vor jeder Messung auf 0. Überlauf ist ohne
`CheckForOverflowUnderflow` unkritisch. Kein Einfluss auf das Spiel: Bei „aus“ ist der Spielstand gleich Base, die die Zähler nicht hat
(unten).

## 3. Ausgeführte Prüfungen

`tools/claude/fetch-reference.sh original`, `tools/claude/setup-workspace.sh`, SDK 10.0.402, Originaldaten aus
`kaaasi/eishockey-manager-referenz` (Zugriff per `git ls-remote` geprüft). `tools/pruefen.sh` (alle drei Läufe) am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2288 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1922 | 0 | 328 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1923 | 365 | 0 |

Commit `9f49b7e…`, Ergebnis: bestanden. Die Testzahl ist unverändert gegenüber `194f387`: Das Delta ändert zwei Tests und fügt keine hinzu.

**Gegenproben** (Mutation in einer Kopie, `dotnet test --filter SaisonManagerTagTests`):

| Nr. | Mutation | Ergebnis |
|---|---|---|
| M4 | `SeasonFlow.cs` Z. 215: `ManagerDayFlag = Options.ManagerDayMinuteLoop;` | `NoManagerInvolved_…` **rot** ✔ (1/8 rot) |
| M-Paarung | `MatchEngine.Live.cs` Z. 91: Bildung je Paarung wie `194f387` | `ManagerPlaysCupDay_…` **rot** ✔ (1/8 rot) |

**Einstellung aus gleich Base `b7e1eb2`:** Derselbe Ablauf an Base und Head (synthetische Datenbank wie in den PR-Tests,
Pokal-/Europapokal-Anzeige aus). Nach jeder von 4 Saisons wird SHA-256 über den ganzen Spielstand gebildet, ohne `schema` und ohne das
neue Optionsfeld. Szenarien: Seed 5/Köln passiv und aktiv, Seed 21/(5, 30) passiv und aktiv, Seed 9/30 passiv. **Alle 20 Prüfwerte
gleich**, z. B. Seed 21 aktiv: `692BC486…`, `91FDF1B1…`, `EE5E740C…`, `EA18613A…`. Damit sind Zufallsfolge und Ergebnisse gleich,
und darüber hinaus der gesamte Zustand.

**Schemafingerabdruck:** `ExpectedSchemaFingerprint` = `E3546517EB3E0A02` (`SaveGameTests.cs` Z. 54). Das Delta berührt weder
`SaveGame*.cs` noch Zustandsfelder, der neue Merker und die Zähler sind statisch. Der Test ist im Lauf mit Originaldaten grün, der
Fingerabdruck ist also unverändert gegenüber `194f387`.

**Erwartet offen, bestätigt:** Inventur A112 und #349 → `pruefung-karte` (Welle 2, `docs/arbeitspakete.md` Z. 35/161 auf `main`).
Platz des Schalters „Ganzer Tag“ `[APPROX]` (`GameOptionsScreen.cs` Z. 46–47, kein Schalter des Originals): akzeptiert.

**Akzeptierte `[APPROX]`:** Platz des Zusatzschalters. Teil „auch die WM-Endrunde“ (R9S-07/#358). Ansonsten unverändert gegenüber
dem Review zu `194f387`.

**Annahmen:** Wie im Review zu `194f387`: Dass 0xA729+m zwischen Spielvorbereitung und 0x54FF3 durchgehend gesetzt bleibt, folgt aus
den Querverweisen. Nicht in DOSBox beobachtet. Die Proben liefen mit der synthetischen Datenbank der PR-Tests, nicht mit `emdb` des
Originals.
