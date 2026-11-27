# Zweite Nachprüfung PR #727 – Spur hok-export (Fixlauf N1/N2)

Reviewer: Claude Opus 5.5 (geplanter Lauf, frische Sitzung ohne Implementierungskontext) · Vorreview
`docs/reviews/2026-11-10-hok-export-nachpruefung.md` (#742, Head `5e24636`) · geprüft: Delta `5e24636..c09cd45` (ein Commit,
6 Dateien) · Head `c09cd45` (`git rev-parse HEAD` = Head, `git status --porcelain` leer) · Originaldaten und Spielstände aus
`kaaasi/eishockey-manager-referenz` (`ls-remote` erreichbar, HEAD `79ae66c`).

## 1. Merge-Empfehlung

**Ja** für Head `c09cd45`, vorbehaltlich der dritten DOSBox-Probe EXPORT1C/2C. Die Probe ist die vereinbarte Merge-Bedingung
(`arbeitspakete.md`). N1 (= R727P-04) und N2 sind behoben, Gegenbeispiele und Rückmutationen sind belegt, und
`tools/pruefen.sh --schnell` ist grün. Der Verdacht „zwei Spieler auf demselben Poolindex“ ist geklärt und behoben. Es bleibt
ein P3 (Testlücke), der den Merge nicht blockiert.

## 2. Befunde

**N1 = R727P-04 (P1): behoben.**
- `emtool hok export --neu Joerg 5 EXPORT01.HOK --kopf 6` und `… --spieltage 22` neu erzeugt. SHA-256 `2917ef54…3b6136` bzw.
  `1c350912…a23637d`. Beide sind gleich mit dem Kommentar „dritter Durchgang“ und mit `EXPORT1C`/`EXPORT2C` im Referenz-Repo.
- Eigener Decoder über far:8253 +0x00–0x11 aller 200 Sätze: In beiden Exporten stehen 0 Felder mit Bytes ≠ 0 hinter der
  Endnull, und es gibt keinen doppelten Namen. Die Namen sind sauber: J.SCHULZ, MAYR, KÜHN, MENDE, NÖTZEL (0x99), HOCK, SIKORA …
  Vorher (EXPORT1B) waren es J.SCHULZV, MAYRRSSON, MENDENZ, K]HNLD, HOCKAGGIA usw.
- Zum Vergleich FILE01: Reste nur in 2 Sätzen (31, 110). Das ist Originalverhalten und von `BehindPoolName` abgedeckt.
- **Verdacht „zwei Spieler auf einem Index“: bestätigt für `5e24636`, behoben in `c09cd45`.**
  - Vorher bekamen die 25 Kaderspieler die Plätze 174–199. Dort standen Nationalspieler, die die erste Schleife schon geschrieben
    hatte. 25 Nationalspieler gingen also verloren, dazu kamen Namensreste.
  - Jetzt ist die Platzfolge **2, 3, 22–31, 98–110**. Sie ist identisch mit FILE01, Besitzer 0, die Kaderplätze sind verschieden.
    Die Plätze 180–199 sind unverändert, und die Marktsätze 8, 114 und 187 (Besitzer 4) bleiben.
  - Zusatzprobe mit drei Managern (Vereine 5/2/9, temporärer Test, 0 und 22 Spieltage): 75 von 75 Indizes sind verschieden, keiner
    ist 0. Die Plätze folgen je Art hintereinander (M1 4, 5, 32 …; M2 6, 7, 43 …), die Besitzer sind 0/1/2, es gibt 0 Namensreste
    und Markt = Listings (7).
- **Zwilling (FILE41 Pool 179):** Import → Export über alle FILE*.HOK, Besitzerbyte +0x1A Original gegen Export.
  - Auf `c09cd45` ist in FILE41/43/43A/44 Pool 179 Besitzer 4 → 4, der Markt bleibt bei 10 bzw. 8 Spielern.
  - Auf `5e24636` war dort `199: 6 → 0` (Nationalspieler verdrängt).
- **Rückmutationen** (temporäre Kopie, danach `git checkout`):
  - Ohne `e[..0x12].Clear()` → `PoolNames_WholeField_OnePlayerPerIndex` 3/3 rot (z. B. „2 HEISS: 484549535345…“ = HEISSE). Die
    Rundreise bleibt grün, wie erwartet.
  - Alte Platzwahl (`Stack` der höchsten freien Plätze) → 3/3 rot. Zusätzlich sind dann die Rundreisen FILE41, FILE43 und
    FILE43A rot, weil die Regel „Poolname geändert“ fehlt.
  - Zwillingszweig aus (`twin is not null && false`) → FILE41/43/43A rot.
  - Art egal (`FindIndex(_ => true)`) → 3/3 rot.
- **Rundreise-Regel „Poolname geändert“ entfernt: berechtigt.** Mit dem Zwillingszweig hat sie keine Treffer mehr (die Rundreise
  ist grün, `AllowedRules_EachCoversADifference` ist grün). Ohne ihn werden die Rundreisen rot, die Regel deckt also nichts mehr
  zu.

**N2 (P1): behoben, Anpassung nach R15-E2 = C nachvollziehbar.**
- `File44_SeasonChange_KeepsListLikeFile46` (Saaten 4705, 4706, 4711) ist grün.
- Rückschaltprobe mit dem Import `OriginalImport.cs` wieder absolut (`r[0x18]` … ohne `- value`): grün.
- Mutation `TransferMarket.ChangeList(this, Random)` in `StartNextSeason` nach `OnNewSeasonTeam()`: 3/3 rot.
- Gegenprobe zur Saat-Erklärung: Die Testform von `5e24636` (`StartNextSeason` mit Tag 0) ist auf `c09cd45` bei 4705 und 4711
  rot und bei 4706 grün. Die Theorie deckt also beide Ausgänge des Wurfs 0x54E65 ab.
- R688-05 (kein Listenwechsel durch den Saisonwechsel selbst) bleibt geprüft, vor und nach dem Wechsel.
- Die fremde Datei ist im PR begründet (Abschnitt „Fremd geändert“).

**R727N2-01 (P3) Besitzerbyte des Pools in der Rundreise pauschal freigegeben, Marktzwilling ohne Test.**
- Ort: [`OriginalExportRoundTripTests.cs:114`](https://github.com/kaaasi/eishockey-manager-redone/blob/c09cd450a04774b525bf7bfcb15cf2cc379a40ba/tests/EishockeyManager.Core.Tests/OriginalExportRoundTripTests.cs#L114).
  Die Regel `far:8253 +0x19–0x1C „ungedeutet (Statistik der Poolspieler?)“` umfasst +0x1A. Das ist laut
  `OriginalSaveGame.cs:281` der Besitzer (4 = Markt), und die Regel gilt für alle Sätze.
- Beleg, Mutation in [`OriginalExport.Squads.cs:67`](https://github.com/kaaasi/eishockey-manager-redone/blob/c09cd450a04774b525bf7bfcb15cf2cc379a40ba/src/EishockeyManager.Core/OriginalExport.Squads.cs#L67):
  `if (!marketTwins.Contains(index))` wird zu `if (true)`.
  - Alle Tests in `OriginalExport*` bleiben grün.
  - Der Export verliert dabei den Marktsatz FILE41/43/43A/44 Pool 179 (`4 → 0`, Markt 10 → 9 bzw. 8 → 7).
  - Ebenso unbefestigt: Die Reihenfolge „ab dem zweiten Platz der Art“
    ([Zeile 143](https://github.com/kaaasi/eishockey-manager-redone/blob/c09cd450a04774b525bf7bfcb15cf2cc379a40ba/src/EishockeyManager.Core/OriginalExport.Squads.cs#L143))
    zu `OrderBy(i => i)` geändert → grün, obwohl HEISS dann auf Platz 1 statt 2 steht (gegen FILE01).
- Der Code des Heads ist richtig. Es fehlt nur die Regressionssicherung.
- Vorschlag: Rundreise-Regel für +0x1A auf Bytes ≠ Besitzer einengen oder einen eigenen Test „Besitzer +0x1A wie Original“
  schreiben, und in `PoolNames_WholeField_OnePlayerPerIndex` die Platzfolge 2, 3, 22… gegen FILE01 prüfen.
- Außerhalb des Deltas verdeckt dieselbe Regel weitere Besitzerabweichungen: FILE11/18–21/47–49A haben 7 Sätze `0 → 6`
  (29–31, 98–100, 110), FILE46 hat `196: 0 → 6`. Beides ist auf `5e24636` unverändert, also kein Befund dieses Fixlaufs.
  Es sollte aber beim Einengen der Regel gedeutet werden.

## 3. Ausgeführte Prüfungen, akzeptierte `[APPROX]`

`tools/pruefen.sh --schnell` (Head `c09cd45`, SDK 10.0.402):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2724 | 0 | 0 |

Das stimmt mit der Übergabe überein. Den Lauf ohne Daten übernimmt die CI.

**Weitere Befehle:**
- `emtool hok export --neu Joerg 5 … --kopf 6 [--spieltage 22]` und `emtool hok dump`.
- Eigenes Prüfprogramm auf `OriginalSaveGame.Read`: Namensfelder, Kader-Poolindizes 5DC3 +0x0F, Besitzer +0x1A.
- Temporäre `[GameDataFact]`-Proben: Import→Export-Besitzer über alle FILE*.HOK auf `c09cd45`, `5e24636` und Mutante; drei
  Manager.
- `dotnet test tests/EishockeyManager.Core.Tests --filter …` für jede Mutation.
- Alle Mutationen und Proben liefen im Arbeitsbaum und wurden mit `git checkout`/`rm` zurückgesetzt. Der Baum war danach leer.

**Erwartet offen – bestätigt:**
- `[APPROX]` Je Kaderspieler ohne Poolplatz entfällt ein freier Ligaspieler gleicher Art im Export, im neuen Spiel 25. Belegt:
  Die Plätze 2, 3, 22–31 und 98–110 tragen in EXPORT1B andere Spieler (FR]TEL, K.FRIESEN, GENZE …), die in EXPORT1C fehlen.
  Markt-, vergebene und Nationalspieler sind nie betroffen. Ob das Remake die Managerkader wie das Original aus dem Pool zieht
  (fremde Spur), entscheidet die Leitstelle.
- Dritte DOSBox-Probe EXPORT1C/2C (Nutzer, nur Mannschaft): steht aus. Die Dateien liegen schon im Referenz-Repo, SHA wie oben.

**Akzeptierte `[APPROX]`:** Spieler ohne freien Ligaplatz seiner Art entfallen (`continue`). Bei drei Managern ist der Fall nicht
eingetreten. Die übrigen `[APPROX]` aus den Vorreviews bleiben akzeptiert.

**Annahmen:** Den Wortlaut der Nutzerentscheidung R15-E2 = C habe ich im Repo nicht gefunden. Ich übernehme ihn aus PR-Text und
Aufruf (fremden Test im #727 mit Begründung anpassen). Bildinhalte der dritten Probe sind nicht Teil dieses Laufs.
