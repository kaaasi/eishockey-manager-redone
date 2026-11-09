# Nachprüfung #688 – Tagesabschluss der Sommerpause (Delta `f98f22f..a25f251`)

- **PR:** #688 · **Base:** `f98f22fff2018efba32dd81e934839c48ddf61b6` (Nachprüfung `2026-11-09-tagestakt-hauptweg-nachpruefung.md`: Ja)
  · **Head:** `a25f251293fc8ee664302d622593e0701e90d97e` (ein Commit)
- **Reviewer:** Claude Code (geplanter Lauf, frische Sitzung ohne Implementierungskontext), 09.11.2026
- **Checkout:** Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer; Build und Tests am Head selbst ausgeführt.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head – wegen R688S-01 (P2). Der Tagesabschluss der Sommerpause ist richtig angeschlossen
(Ablauf je Tag, Buchungen über `GameState.Book`, Werbeangebote erst nach der Schleife). Die Lage des **30. April** ist aber
nicht wie im Original. In em.exe läuft der Tagesabschluss des 30.04. samt April-Monatsabschluss noch in der letzten Runde der
WM-Schleife. Das ist vor dem Saisonende und vor dem Kaderwechsel. Die Sommerschleife umfasst nur 01.05.–09.09. mit vier
Monatsabschlüssen. Das Remake bucht den April nach dem Kaderwechsel mit dem neuen Kader im Buch der neuen Saison. Die
`[ORIGINAL]`-Angabe „30. April bis 9. September“ ist damit falsch belegt (AGENTS.md 2.7). Die Behebung ist klein (siehe
R688S-01). Alles andere im Delta ist bestätigt. Die Rückmutation führt alle Sollwerte zurück.

Zur Frage „passiver Bundesliga-Manager ohne Werbung scheitert in Saison 2“: Der Geldfluss der Sommerpause entspricht dem
Original. FILE44 → FILE46 stimmt nach Abzug eines Importfehlers auf rund 1 %. Eine Einnahme fehlt nicht: Die Sommerschleife hat
keine Spiele, keine Zuschauer und keine Turniere. Werbung kommt nur über 0x3ED01, und das ist jetzt angeschlossen. Die
**Folge** ist dagegen nicht belegt. Die Entlassung nach 8 Wochen unter −1,5 Mio. DM ist eine Remake-Regel aus den Anforderungen
und steht nicht in em.exe (R688S-03).

## 2. Befunde

### R688S-01 (P2): 30. April gehört zur WM-Schleife, die Sommerpause beginnt am 1. Mai

- **Stelle:** [`GameState.DailyRun.cs` Z. 241/258](https://github.com/kaaasi/eishockey-manager-redone/blob/a25f251293fc8ee664302d622593e0701e90d97e/src/EishockeyManager.Core/GameState.DailyRun.cs#L241-L258)
  (`SummerBreakCounters` ab Zähler 233 = 30.04.),
  [`GameState.Integration.cs` Z. 154–158](https://github.com/kaaasi/eishockey-manager-redone/blob/a25f251293fc8ee664302d622593e0701e90d97e/src/EishockeyManager.Core/GameState.Integration.cs#L154-L158),
  [`docs/tagesablauf.md` Z. 34](https://github.com/kaaasi/eishockey-manager-redone/blob/a25f251293fc8ee664302d622593e0701e90d97e/docs/tagesablauf.md#L34),
  Tests `SummerBreak_DayClose_FiveMonthEndsBooked` und `FinanceCalibrationTests` („sieben Monatsabschlüsse der Spielzeit“).
- **em.exe (selbst gelesen, `disasm.py`):**
  - **WM-Schleife, Datei 0x55570–0x55740.** Jeder Tag endet mit `inc [0x3B7]` (0x55670), dem Datum 0x367B1 und je Manager
    0x3ED01, 0x3F833, Stadionbau (Zähler ≡ 0) und Heilung. Erst danach prüft 0x55724, ob der nächste Spielplaneintrag noch ein
    WM-Tag ist. Sonst geht es zum Saisonende (0x55743 …).
  - **Folge:** Nach dem letzten WM-Spieltag (Zähler 232 = 29.04.) läuft der Tagesabschluss für **Zähler 233 = 30.04.** noch in
    der WM-Schleife. Der Saisonwechsel zählt dann vor der Sommerschleife weiter (`inc [0x3B7]`, Datei 0x564BC). Der erste
    Sommertag ist also Zähler 234 = **01.05.**
  - **Ende:** Die Schleife endet nach dem Tag mit Monat 8 (0-basiert) und Tag 9 = 09.09. (Datei 0x5656B–0x56593).
  - **Datum (0x367B1):** Tag im Jahr = Zähler + 0xFC, Monatstabelle DGROUP 0x393. Probe: Zähler 217 = 14.04. (FILE44),
    Zähler 1 = 10.09. (FILE46).
  - **Ergebnis:** Die Sommerpause hat 132 Tage mit 4 Monatsabschlüssen (Mai–August). Der April-Abschluss gehört zur
    Spielzeit, die damit 8 Monatsabschlüsse hat.
- **Beleg im Spielstand:** `file46_2` (Ereignisse „Saisonende“, Montag 30. April 1994) zeigt **17.523.876 DM**. Das Konto steht
  dort vor Verhandlung, Ablöse und Kaderwechsel. FILE44 hat 18.027.183 DM, dazu kommt die Prämie von 100.000 DM. Also sind bis
  zu diesem Bildschirm schon **−603.307 DM** gebucht, und das kann nur der April-Abschluss sein. Im Remake ändert sich das Konto
  von FILE44 bis zum Saisonende nur um die Prämie (Probe: 18.027.183 → 18.127.183 DM, Tag 231).
- **Folgen im Remake:**
  - Die April-Gehälter werden mit dem neuen Kader gerechnet: 24 statt 25 Spieler, ohne KJELLBERG 18.800 DM und VALLIERE
    9.999 DM.
  - Der April-Abschluss landet im Buch der neuen Saison statt im alten.
  - Die Werbebewertung 0x3F833 des 30.04. fehlt.
  - Die Tests prüfen 5 Monatsabschlüsse im Sommer und 7 in der Spielzeit statt 4 und 8.
- **Gegenprobe:** In einer Kopie des Heads läuft der erste Sommertag (30.04.) vor `StartSquadSeason`, die Schleife danach ab
  dem 01.05. FILE44 → neue Saison: −1.956.130 → **−1.983.676 DM**, also −27.546 DM, etwa die Gehälter der beiden Abgänge.
- **Behebung (Vorschlag):**
  - Den letzten Tagesabschluss der WM-Schleife (Zähler 233, 30.04., mit 0x3F833) vor dem Saisonende anschließen, in der
    Zeitleiste über `Continue()`.
  - `SummerBreakCounters` ab Zähler 234 laufen lassen.
  - Doku, `[ORIGINAL]`-Angaben und die Zählungen 5/7 → 4/8 in den Tests anpassen.
  - Regressionstest: Konto am Saisonende-Bildschirm FILE44 → wie `file46_2`, abzüglich der Remake-Abweichung im Monatsabschluss.

### R688S-02 (P3, nicht durch das Delta verursacht): Leihe VALLIERE aus dem HOK-Import ohne Leihkennzeichen → Ablöse

- **Stelle:** `OriginalImport` (Kaderplätze). Probe: Nach `Import("FILE44.HOK")` gilt für VALLIERE `OnLoan = false` (Vertrag 1 J.).
  Am Saisonende geht er deshalb über `Leave()` ([`Transfer.SeasonEnd.cs`](https://github.com/kaaasi/eishockey-manager-redone/blob/a25f251293fc8ee664302d622593e0701e90d97e/src/EishockeyManager.Core/Transfer.SeasonEnd.cs#L136-L142))
  gegen **130.500 DM Ablöse**. Im Original endet die Leihe frei: A115, Pool +0x1A 4 → 6, nur die Ablöse für KJELLBERG.
- **Wirkung:** Das erklärt den größten Teil der Restabweichung von FILE44 → FILE46 (unten). Der Test `File44_ToNewSeason_…`
  verdeckt den Fehler mit seiner Spanne von ±15 %. Gehört zum Import (A115), eigener Befund.

### R688S-03 (P2, nicht durch das Delta verursacht, durch das Delta verschärft): Folge der Sommerpause ist eine unbelegte Entlassungsregel

- **Stelle:** [`SeasonDismissal.cs` Z. 7](https://github.com/kaaasi/eishockey-manager-redone/blob/a25f251293fc8ee664302d622593e0701e90d97e/src/EishockeyManager.Core/SeasonDismissal.cs#L7)
  und `SeasonFlow.CheckDebts`. Die Grenze von 8 Wochen unter −1,5 Mio. DM stammt aus `docs/anforderungen/bildschirme.md`.
  `docs/analyse/re-saisonwechsel.md` nennt sie „ohnehin nur `[APPROX]`, im Original nicht belegt“. Belegt ist im Original
  dagegen der **Zwangsabstieg** beim Saisonwechsel (Konto < −2.000.000 Drittel, Datei 0x55F87 ff.), und den hat das Remake nicht.
- **Wirkung:** Mit 12 statt 7 Monatsabschlüssen je Jahr scheitert der passive Bundesliga-Manager ohne Werbung jetzt in
  Saison 2 an dieser Remake-Regel. Das gilt für die Saaten 1–60 laut PR und für alle drei Saaten der Hüllkurve.
  - Der Geldfluss stimmt mit dem Original überein (FILE44 → FILE46 unten): Ohne Werbung deckt das Eintrittsgeld einer
    Bundesliga-Saison nicht zwölf Monatsgehälter.
  - Ob das Original den Manager ebenso entlässt, ist nicht belegt.
  - Die PR-Tests halten das Konto in sieben Proben künstlich hoch (`Money = Math.Max(…)` ohne Buchung). Das ist für diese
    Proben vertretbar, verdeckt aber die Frage.
- **Vorschlag:** Kein Fix in #688. Entscheidung des Nutzers: entweder die Regel im Original belegen (Disassembly der
  Entlassung bzw. ein DOSBox-Lauf mit einem passiven Bundesliga-Manager über einen Sommer) oder die Regel als `[APPROX]`
  bestätigen. Den Zwangsabstieg als eigenes Paket aufnehmen.

### Bestätigt (ohne Befund)

- **Sommerschleife Datei 0x564D2–0x56593 je Kalendertag und je Manager:** Stadionbau 0x3D316 bei Zähler ≡ 0 (0x564F5–0x5650D),
  dann 0x3ED01 (0x56512), dann Heilung 0x07C12 je Kaderplatz (0x5653C). Dazu gibt es keine Werbebewertung 0x3F833, keine
  Talentsuche, keine Jugendteams, keine Spiele und kein Training. `SummerDayClose` deckt sich mit dem Tagesabschluss des
  Hauptwegs (`AdvanceFinance`) ohne die Teile außerhalb von 0x3ED01.
- **Reihenfolge:** Saisonende der Kader 0x26F20 (Datei 0x5645D), dann die Schleife, dann je Manager 0x5D90D(1) und 0x3FB47
  (Datei 0x56650). `StartSponsorSeason` läuft jetzt nach der Schleife. Der Saisonzähler wird vorher erhöht (Datei 0x56476), die
  Bücher der neuen Saison sind damit ein zulässiges `[APPROX]`.
- **AGENTS.md §4:** Gebucht wird nur über vorhandene Wege mit `Book` (`MonthlyFinances`, `AccrueDailyInterest`,
  Werbeverträge, Stadion). Es gibt keine eigenen Nachhol-Zähler, die Schleife ist die des Originals. Die Börse rechnet einmal
  je Tag (Entscheidung #188). Bildschirme sind unverändert.

### Beleg FILE44 → FILE46 nachgerechnet

Datenbasis:

- `emtool hok dump`: FILE44 hat 14.04.1994, Zähler 217, 18.027.183 DM und 25 Spieler. FILE46 hat 10.09.1994, Zähler 1,
  15.887.669 DM und 24 Spieler (KJELLBERG und VALLIERE fehlen, HOHENADL ist von der Liste zurück).
- Der HOK enthält keine Saisonsummen je Kategorie. Die Aufteilung im Original stammt deshalb aus den Bildern `file46_2`
  (30.04.: 17.523.876 DM) und `file46_5` (Ablöse 155.500 DM).

| Posten | Original | Remake (Head) | Differenz |
|---|---:|---:|---:|
| Prämie 10. Platz | +100.000 | +100.000 | 0 |
| Ablöse (Transfers) | +155.500 (KJELLBERG) | +288.500 (KJELLBERG 158.000, VALLIERE 130.500) | +133.000 (R688S-02, Zufall +2.500) |
| April-Abschluss (bis Saisonende-Bildschirm) | −603.307 | im Sommer, s. u. | |
| Sommer (Original Mai–August, 4 Abschlüsse) | −1.791.707 | | |
| Monatsabschlüsse zusammen | **−2.395.014** | **−2.344.630** (5×: Gehälter −1.455.500, Unterhalt −964.330, Sponsoren +75.200) | +50.384 |
| **Summe** | **−2.139.514** | **−1.956.130** | **+183.384** |

Die Restabweichung von 183.384 DM setzt sich so zusammen:

- **130.500 DM** Ablöse VALLIERE (R688S-02),
- **2.500 DM** Zufall der Ablöse KJELLBERG,
- **rund 27.500 DM** April-Gehälter mit dem neuen Kader (R688S-01, Gegenprobe −27.546),
- **rund 23.000 DM** übrige Monatsposten.

Die übrigen Monatsposten lassen sich ohne Kategoriesummen im HOK nicht weiter aufteilen. Auffällig: Der April des Originals ist
rund 126.000 DM teurer als ein Sommermonat, nach Abzug der beiden Gehälter. Das bleibt offen und wäre mit einem Finanzbild am
30.04. zu klären.

Ohne R688S-01/-02 läge das Remake bei rund −2.114.000 DM, also bei 98,8 % des Originals.

## 3. Ausgeführte Prüfungen

- **Disassembly:** `python3 tools/reverse-engineering/disasm.py gamedata/em.exe` für die Bereiche 0x56440–0x56680
  (Sommerschleife, Saisonwechsel), 0x55570–0x55760 (WM-Schleife), 0x3ED01–0x3F185 (Tagesabschluss) und 0x367B1 (Datum).
- **Spielstände:** `emtool hok dump` FILE44/FILE46 sowie die Bilder `file46_1`/`file46_2`.
- **Proben** (temporärer Test, nicht committet): FILE44 → Saisonende → `StartNextSeason`, Konto je Tag, Buch je Kategorie,
  Abgänge, Ablöse je Spieler, Leihkennzeichen nach dem Import.
- **Rückmutation 1** (nur `SummerDayClose` entfernt, Head-Tests): 21 rot. Darunter sind beide neuen Tests, alle 8
  `FinanceCalibrationTests`-Hüllkurven, RngTwin ×6, Spielberechnung ×2, JugendTeams ×2 und GameMode.
  - RngTwin synthetisch ×4 und Spielberechnung ×2 liefern dabei genau die Werte von `f98f22f`.
  - `File44_…` liefert +388.500 DM.
- **Rückmutation 2** (zusätzlich die alte Lage aus `f98f22f`: Sommerpause in `OnNewSeasonManager`, Werbeangebote in
  `StartFinanceSeason`; Tests JugendTeams, RngTwin, GameMode und Spielberechnung mit den Sollwerten von `f98f22f`): **46/46
  grün**. Jeder geänderte Abdruck geht damit auf Tagesabschluss und Lage zurück, auch RngTwin Originaldatenbank aktiv und das
  Konto in GameMode.
- **Gegenprobe R688S-01:** siehe oben, −1.983.676 DM.
- **Mehrsaisonprobe am Head** (4 Wechsel, passiv und aktiv): `FinanceCalibrationTests.FourSeasonChanges_*` (Bundesliga und
  Oberliga, synthetisch und Originaldatenbank), `RngTwinProbeTests.FourSeasonChanges_*`, `JugendTeamsTests.FourSeasonChanges_*`
  und `SpielberechnungAnschlussTests.FourSeasonChanges_*` sind grün, enthalten im Lauf unten.
- **`tools/pruefen.sh --schnell`** am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2604 | 0 | 0 |
| Ohne Originaldaten | CI `build` success (Head `a25f251`) | | |
| Pflichtlauf ohne Daten | nicht verlangt (sparsame Kette) | | |

**Akzeptierte `[APPROX]`:**

- Die Sommerbuchungen stehen im Buch der neuen Saison. Das gilt außer für den April (R688S-01).
- Bau, Tagesabschluss und Heilung laufen je Tag für alle Manager nacheinander statt verschränkt.
- Die Börse rechnet einmal je Tag (#188).
- Im Modus „nur Weltkugel“ gibt es keinen Tagesabschluss.

**Annahmen:**

- Die Datumsanzeige des Saisonende-Bildschirms ist das zuletzt von 0x367B1 berechnete Datum (Zähler 233). Das ist durch
  `file46_2` (30. April) und den dort schon niedrigeren Kontostand gestützt.
- Den Ursprung der zusätzlichen rund 126.000 DM im April-Abschluss des Originals habe ich nicht geprüft.
