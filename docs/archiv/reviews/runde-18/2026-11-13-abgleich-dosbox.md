# Review PR #812 – Spur `abgleich-dosbox` (Runde 18)

Erstreview, unabhängige Sitzung (geplanter Lauf, Claude Opus 5.5, ohne Implementierungskontext). Geprüft: Head
[`6c25f4e`](https://github.com/kaaasi/eishockey-manager-redone/tree/6c25f4ee5f46076c428173535d1fd8e6c08993fe) gegen Base `c68b71b`;
`git rev-parse HEAD` = Head, `git status --porcelain` leer.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Alle drei Läufe von `tools/pruefen.sh` grün (3096/0/0 mit Originaldaten). Die Gegenbeispiele
#773, #804 und R758-01 sind nachgestellt: die Zyklusrechnung stimmt mit em.exe überein (unabhängig nachgerechnet), der Rundlauf
Original → Remake → HOK hält 0x26 und die Angriffsbits aller 52 Computervereine in FILE03/16A/44/53, der Remake-Export ist
im Rundlauf byte-gleich, EXPORT1D wird am Head byte-gleich erzeugt. Jede Gegenprobe macht die neuen Tests rot. Die Rückschaltproben
der angepassten fremden Tests liefern genau die alten Werte. Ein P0–P2 wurde nicht gefunden.

## 2. Befunde

**P3-1 (nach Merge von #814): lrandom zweimal nachgebaut.**
[`MatchEngine.LRandom.cs:19-23`](https://github.com/kaaasi/eishockey-manager-redone/blob/6c25f4ee5f46076c428173535d1fd8e6c08993fe/src/EishockeyManager.Core/MatchEngine.LRandom.cs#L19-L23)
(`LRandomCycle.Step/Value`) und `LRandom` in `LiveClock.cs` von #814 (Head `57be39b`, Z. 141 ff.) rechnen dieselbe Folge
(16·x + 0xA7 mod 0x3D0900). Beleg: `git show pr814:src/EishockeyManager.Core/LiveClock.cs | grep -n 0x3D0900`. Die Namen kollidieren
nicht, `git merge-tree pr812 pr814` meldet keine Konflikte, und der Testlauf der Zusammenführung ist grün (§3). Vorschlag: nach
beiden Merges eine Stelle (z. B. `LRandomCycle` über `LRandom`), Aufgabe für Leitstelle/`livespiel-kern`.

**P3-2: `KaderTransferTeil4Tests.Season_MarketRunsEveryCalendarDay` prüft die Saat, nicht die Regel.**
[`KaderTransferTeil4Tests.cs:59`](https://github.com/kaaasi/eishockey-manager-redone/blob/6c25f4ee5f46076c428173535d1fd8e6c08993fe/tests/EishockeyManager.Core.Tests/KaderTransferTeil4Tests.cs#L59):
Laut eigener Begründung liegen nur 3 von 40 Saaten über der Grenze 22. Die Rückschaltprobe (alter 5-Minuten-Zug, Saat 26) ist
rot: „nur 21 Halte mit Listenwechsel“. Die Anpassung ist nach R15-E2 = C begründet und war schon in vier Runden üblich. Der Test
sollte aber wie in `AbgleichRestPenaltyTests` über ein Mittel mehrerer Saaten prüfen. Das ist kein Mangel dieser Spur.

**P3-3 (Hinweis): Strafminuten FILE05→24 knapp an der Grenze.** Die Erhöhung in
[`AbgleichRestPenaltyTests.cs:33`](https://github.com/kaaasi/eishockey-manager-redone/blob/6c25f4ee5f46076c428173535d1fd8e6c08993fe/tests/EishockeyManager.Core.Tests/AbgleichRestPenaltyTests.cs#L33)
von 4 auf 16 Saaten ist begründet: Mittel 474,4 gegen +441 im Original, also +7,6 % bei einer Grenze von 8 %. Das Remake liegt
damit jetzt über dem Original, nachdem es früher darunter lag. Zusammen mit den Proben im Bericht §2.4 (FILE52→53 Mittel 473
gegen 456, FILE02→03 1394 gegen 1447) ist das kein Widerspruch. Den Vorschlag im Bericht, weitere aufeinanderfolgende
lrandom-Züge zu prüfen (0x9F5C, Abschluss, Kette), stütze ich: er sollte vor der nächsten Lockerung kommen.

Ohne Befund geprüft:

- `lrandom` 0xBFD5: `ndisasm` zeigt x<<4 (0x52A, cl = 4), +0xA7, Rest durch 0x3D0900, dann x·(b−a+1)/0x3D0900 + a.
- Strafwurf 0x358B7–0x3592D: d1 = lrandom(0,12), d2 nur bei d1 ≠ 0, d3 = lrandom(0,15) nur bei freiem Platz 0
  (`[bp-1]`). Beim Managerverein liegt dazwischen nur `0x120:0x66` (Zählung fitter Spieler, kein lrandom; `docs/spielberechnung.md:160`).
  Die Zyklushäufigkeiten gelten also für beide Vereinsarten.
- Zyklus selbst nachgerechnet (Python): 15.625 Zustände, Einsatz 40 → 1.202/192 Treffer, 122/53 mit 5 Min. = 12,55 %, Einsatz 60 → 11,90 %.
  `Total` ist für alle Einsätze 0–100 > 0 (Weg 2 mindestens 123), `Rnd(random, 0, total − 1)` ist also nie leer. Die Zahl der Züge bleibt gleich.
- #730 Teil 1: `lampen.py` aus den Bildern `file50_*` erneut ausgeführt; die vier Ziffernfolgen sind gleich den `[ORIGINAL]`-Konstanten.
  Stichprobe Bild `file50_21`: Uhr 16, Lampen Reihe 1 beide Vereine. `PlayFirstMatch` entspricht `OriginalMinutes.Play`
  (Erholung vor Minute 21/41). Zu Spieltag 2/3 behauptet der PR nichts ohne Beleg; der `[APPROX]`-Marker an `ChangeLines` ist sauber neu gefasst.
- #804: Import und Export sind symmetrisch (Nationalteams und „nur Weltkugel“-Managerverein jeweils ausgenommen). Es gibt keine
  Schema-Änderung: `_matchEffort/_matchAttack` stehen seit `SaveGame.Migration.AbgleichRest.cs`. Die Dateien liegen in der Spur;
  `OriginalExport.League.cs`/`OriginalImport.cs` gehören zu Teil 3 laut `arbeitspakete.md`, gemeinsame Dateien sind unberührt.
- #814: Die Zugzahl in `PenaltyMinutes` bleibt gleich. Die Einfügung `PenaltyReason` (nach der Strafe, vor dem Kettenzug)
  verschiebt keinen Wurf dieser Spur.

## 3. Ausgeführte Prüfungen

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3096 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2254 | 0 | 523 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2255 | 841 | 0 |

`tools/pruefen.sh` (alle drei Läufe), Commit 6c25f4e, SDK 10.0.402, „Ergebnis: bestanden“.

**Gegenbeispiele** (Werkzeug: `emtool` aus dem Release-Build des Heads):

- **#773:** Mutation `Rnd(random, 0, 15) == 0` (alter 5-Minuten-Zug) macht 3 Fälle rot:
  `PenaltyMinutes_FiveMinuteShare_LikeLrandomCycle` (40, 60) und `File02To03_LeaguePenaltyMinutes_WithinRemakeSpread`.
  `File52To53_*` bleibt grün; der Einsatz ist nicht die Ursache, wie behauptet.
- **#804:**
  - `emtool hok import FILE03|16A|44|53` → `hok export`, verglichen über `hok dump far:9657` und die Vereine 0–51. Feld 0x26
    und 0x2C Bits 4–6 sind in allen vier Ständen gleich. 0x2D weicht je Stand an 1–2 Sätzen ab: Gegner der Managerpaarung,
    `[APPROX]` 0x2D = 0x26. Die übrigen Bits von 0x2C werden 0 (Favorit 0x80 und Modi), wie erwartet.
  - Remake `hok export --neu Joerg 5 … --kopf 6 --spieltage 22` → import → export ist byte-gleich. Zwei Läufe sind byte-gleich.
    Die Vereinsfelder sind jetzt belegt: 40/40/0 × 13, 60/60/0 × 12, Angriff 4–7 an 10 Vereinen.
  - Mutationen: Import-Zeile `RecordMatchFields` weg → alle 11 Fälle von `AbgleichDosboxExportTests` rot, auch die
    Mehrsaisonprobe passiv/aktiv. Export ohne `r[0x2C]` → 6 rot (Export und Mehrsaison).
- **R758-01:** `hok export --neu Joerg 5 E1.HOK --kopf 6` am Head ist byte-gleich mit `EXPORT1D.HOK` (SHA-256 wie README
  Referenz-Repo, d2b3cc93…). Bild `export1d_2_mannschaft` gesichtet: 25 Spieler, „Eingesetzt: 22“, STEFAN/DOROCHIN ohne Status,
  keine „SPIELER 117x“. EXPORT2D lässt sich am Head nicht mehr byte-gleich erzeugen, weil sich die Spielrechnung durch #773
  ändert. Erledigt bleibt R758-01 für die geladenen Stände.
- **#730 Teil 1:** Mutation `OnIceMinutes[part] > 5` in `ChangeLines` macht `File50_LinesWithoutClick_WithinRemakeSpread` rot
  („Gast Wechsel: Original 21, Remake 9…17“).

**Fremde Tests (R15-E2 = C), Rückschaltprobe** mit dem alten 5-Minuten-Zug in einer temporären Kopie:

- `JugendTeamsTests` (beide), `GameModeTests.Mode4_ReferenceValues_Unchanged` und `RngTwinProbeTests` (alle 6 Abdrücke)
  liefern genau die alten Werte aus dem Diff.
- `WorldCupTests` (Saat 8), `KaderStartRunde13Tests` (Saat 13) und `AbgleichFixStrainTests` (±17/±11) sind auch alt grün.
- `KaderTransferTeil4Tests` (Saat 26) ist alt rot, wie im Kommentar angegeben (P3-2).
- Die übrigen Anpassungen (`SpielberechnungKomplett4/Minuten/Tests`, `ScriptedRandom` 1 → 1000 bzw. 200) folgen nur aus der neuen
  Bedeutung des Zugs. Die Begründung (≥ 122 von 1.202 bzw. ≥ 53 von 268) ist nachgerechnet.
- **Mehrsaisonprobe:** `AbgleichDosboxExportTests.MultiSeason_*` (passiv/aktiv) und die Vier-Saison-Abdrücke grün.

**Verträglichkeit #814:** Zusammenführung 6c25f4e + 57be39b ohne Konflikt. Gezielt LivespielKern, AbgleichDosbox,
Spielberechnung, RngTwin und GameMode: 161/0. Ganze Lösung mit Originaldaten (`EM_REQUIRE_GAMEDATA=1 dotnet test EishockeyManager.slnx`):
Audio 31, Formats 157, Core 2259, UI 714 – 0 fehlgeschlagen. Das ist ein Probelauf, keine Abnahme von #814.

**Akzeptierte `[APPROX]`** (erwartet offen, bestätigt):

- #730 Spieltag 2/3 (Marker an `ChangeLines`, DOSBox-Vorschlag Bericht §1.3);
- A093 (`MatchEngine.Managed.cs:188`, unverändert `[APPROX]`);
- Lage im lrandom-Zyklus gleichverteilt;
- Export +0x2D = +0x26, übrige Bits von +0x2C = 0;
- Highscore „nur Weltkugel“ nach WM-Platz (`Highscore.cs:236` unverändert „Weltmeistertitel + 1“, eigenes Issue).

**Annahmen:** Die von Hand abgelesenen Spieluhr-Minuten (`MINUTEN` in `lampen.py`) sind nur an einem Bild geprüft.
