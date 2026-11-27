# Nachprüfung: Integration Runde 14, Spur `nationalteam` (PR #661)

Nachprüfung des Reviews `docs/reviews/2026-11-08-integration-nationalteam-r14.md`. Bisher geprüfter Head `b09c755`, jetzt nur das
Delta `b09c755715173457b8b8ab79f5fce6cbf5776ee3..3ed31653a96165026760b0c5663bb62226b52a0a` und die Befunde R14NT-01 bis -06.
Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext) · 09.11.2026. Checkout: Klon,
`git rev-parse HEAD` = `3ed3165…`, `git status --porcelain` leer. Codelesen **und** Ausführung am nachgewiesenen Checkout mit
Originaldaten. Mutationen in einem eigenen Worktree desselben Commits. Fundstellen selbst mit `tools/reverse-engineering/disasm.py`
nachgelesen.

H = `https://github.com/kaaasi/eishockey-manager-redone/blob/3ed31653a96165026760b0c5663bb62226b52a0a`

## 1. Merge-Empfehlung

**Ja** für Head `3ed3165`. R14NT-01 bis -06 sind behoben. Das Gegenbeispiel aus R14NT-01 zeigt jetzt an jedem WM-Termin die Form der
Aufstellung, auch an Tag 216. `OnDayWorldCupLoop` entspricht der Schleife Datei 0x55570–0x55740. Die drei verlangten Mutationen wurden
ausgeführt. Zwei davon machen Tests rot. Die dritte („gemerkte Seite nicht zurückgesetzt“) bleibt in der ganzen UI-Suite grün. Das
Verhalten am Head stimmt aber, das zeigt eine eigene Probe (R14NT-N1, P3). Diese Testlücke kann ein Folgeauftrag schließen; sie
blockiert den Merge nicht. Alle drei Läufe von `tools/pruefen.sh` sind grün. Der Merge von `main` (`6b7ef91`) bringt nur die
Review-Datei.

## 2. Befunde

### R14NT-N1 (P3) – Rücksetzen der gemerkten WM-Seite beim Saisonwechsel ist nicht getestet

[`WorldCupPairingsPages.cs` Z. 82](https://github.com/kaaasi/eishockey-manager-redone/blob/3ed31653a96165026760b0c5663bb62226b52a0a/src/EishockeyManager.UI/Screens/Competitions/WorldCupPairingsPages.cs#L82)
setzt die Seite bei neuer Saison auf 8 zurück. Nachgelesen: Datei 0x5649D–0x564BA kopiert 0x5128 … 0x512C nach 0x5123 … 0x5127. Die
Mutation M5a (Zeile auskommentiert) bleibt in der ganzen UI-Suite grün (606/606). Damit ist die Vorgabe „jede Mutation macht einen
Test rot“ für diesen Punkt nicht erfüllt.

Am Head ist das Verhalten richtig. Das zeigt eine eigene Probe, die nicht committet ist. Ablauf: „nur Weltkugel“, Kachel „WM“, dann
„Gruppe“ → Gruppe 2. Die Kachel zeigt danach erneut Gruppe 2. Dann Saisonende, `StartNextSeason`, `Continue` und die Kachel noch
einmal: Am Head erscheint Gruppe 1, die Probe ist grün. Mit M5a erscheint Gruppe 2, die Probe ist rot. **Vorschlag:** diese Probe als
Test in `NationalteamRunde14UiTests` aufnehmen.

### R14NT-N2 (P3, nur Hinweis) – Bedingung „Tagesbyte ≠ 0“ für 0x952C(5, 0) ist ohne Test, aber praktisch gleichwertig

[`CupsNational.AutoLineup.cs` Z. 152](https://github.com/kaaasi/eishockey-manager-redone/blob/3ed31653a96165026760b0c5663bb62226b52a0a/src/EishockeyManager.Core/CupsNational.AutoLineup.cs#L152)
rechnet die Länderwerte nur an WM-Tagen (`NationalCoachPlaysOn`). Das entspricht Datei 0x55587–0x55597: `cmp [0x9eda],0xfd` und
`cmp [bx+0x828],0`, sonst Sprung hinter 0x555D6. Ohne die Bedingung (M2a, Rechnung an jedem Tag der Schleife) bleiben alle
Nationalteam-Tests grün. Eine Wirkung habe ich nicht gefunden. An Tagen ohne WM-Termin hält die Zeitleiste nicht (`RunDays` hält nur
bei eigenem Spiel oder Zug des Bundestrainers). Der nächste WM-Tag schreibt die Werte vor den Spielen ohnehin neu. Kein
Handlungsbedarf.

### R14NT-N3 (P3, nur Hinweis) – Heilung in der WM-Schleife vor statt nach den WM-Spielen

Im Original läuft 0x07169 bei Datei 0x5565C **nach** 0x30E7B(1) (0x555F3). Das Remake heilt in `OnDayWorldCupLoop` vor den Spielen
des Tages. Die Länderwerte stehen dann schon fest, weil `UpdateNationalTeamValues` davor läuft, und Vereinsspiele gibt es im
WM-Zeitraum nicht. Eine Wirkung habe ich nicht gefunden. Der Kommentar könnte die Reihenfolge als `[APPROX]` nennen. Training und
Markt im WM-Zeitraum gehören zu Issue #664.

## 3. Ausgeführte Prüfungen

**`tools/pruefen.sh`** (alle drei Läufe, Head `3ed3165`, Originaldaten aus dem Referenz-Repo, SDK 10.0.402):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2312 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1946 | 0 | 328 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1947 | 365 | 0 |

Ergebnis: bestanden.

**Merge von `main`:** `git diff --stat 60539d9 6b7ef91` zeigt nur `docs/reviews/2026-11-08-integration-nationalteam-r14.md`
(+234). Der zweite Elternteil ist `ea318cf` = `origin/main`.

**Gegenbeispiel R14NT-01** (gleicher Aufbau wie im Review #662, aber mit echter Ernennung). Modus 4 mit Seeds 3 und 11. Verein 2
spielt mit Höchstwerten bis zum 13. April. Ernannt wird Manager 0 am 13. April. Danach `AutoNominate` und `AutoFormation`; vor jedem
`Continue` bekommt der Kader Form 1. Gemessen wird die Form Deutschlands vor den WM-Spielen:

| Seed | 216 | 218 | 220 | 222 | **224** | 226 | **228** | 230 |
|---|---|---|---|---|---|---|---|---|
| 3 | 1/1 | 1/1 | 1/1 | 1/1 | **1/1** | 1/1 | **1/1** | 1/1 |
| 11 | 1/1 | 1/1 | 1/1 | 1/1 | **1/1** | 1/1 | **1/1** | 1/1 |

Mit der Mutation M1 (Wanderung in der Schleife) ergibt Seed 3 an Tag 224 den Wert 50/53 und an Tag 228 den Wert 48/45. Die Probe
erkennt die Abweichung also.

**Gegenbeispiel (4) aus Review #649 bzw. R14NT-05:** Ablauf: Stufe 5, erster Aufbau, Endrunde, „Gruppe“ → „Gruppe 1 5.Spieltag“,
dann `OnMainMenu(isReturn: true)` und die Kachel erneut. Am Head erscheint die gemerkte Gruppenseite, und der Spielstand bleibt
bytegleich. Das deckt
[`CoachMenu_WorldCupPairings_ReturnInSameTurn_KeepsRememberedGroup`](https://github.com/kaaasi/eishockey-manager-redone/blob/3ed31653a96165026760b0c5663bb62226b52a0a/tests/EishockeyManager.UI.Tests/NationalteamRunde14UiTests.cs#L110)
ab. Der Test ist grün und wird mit M5b und M5d rot. Bei Stufe 6 erscheint die zuletzt gezeigte Gruppe
([Z. 76](https://github.com/kaaasi/eishockey-manager-redone/blob/3ed31653a96165026760b0c5663bb62226b52a0a/tests/EishockeyManager.UI.Tests/NationalteamRunde14UiTests.cs#L76)).

**Mutationen** (im Worktree des Heads, Filter auf Nationalteam-, WM- und Jugendtests, danach zurückgesetzt):

| Mutation | rot |
|---|---|
| M1 Wanderung in der WM-Schleife wieder an (`DriftTeamValues` in `OnDayWorldCupLoop`) | `Mode4_AfterRealAppointment_…` (2/2), `Mode4_FourSeasonChanges_…` (passiv und aktiv), `Mode4_WorldCupDay_PlaysLineupSeenInCoachTurn` (2/2), `JugendTeamsTests.FourSeasonChanges_StrengthFollowsFunding(active: False)` |
| M1b `InWorldCupLoop` immer false (Hauptweg wie Base) | dieselben 7 |
| M2b 0x952C in der Schleife entfernt | `Mode4_AfterRealAppointment_…` (2), `Mode4_FourSeasonChanges_…(active)`, `Mode4_WorldCupDay_…` (2) |
| M2c Neuaufstellung „Tag mit Ereignis“ vor 0x952C (0x54D41 in der Schleife) | `Mode4_WorldCupDay_PlaysLineupSeenInCoachTurn` (2) |
| M2d Hauptweg `OnDayNationalTeam` statt Schleife (0x952C(5, 1) an falscher Stelle) | `Mode4_WorldCupDay_…` (2) |
| M2a 0x952C an jedem Tag der Schleife (ohne „Tagesbyte ≠ 0“) | **grün** (R14NT-N2) |
| M4 Stufe ohne Obergrenze (8 nach dem Finale) | `WorldCupStage_LocksNationalSquadFromFirstDate`, `Mode4_FourSeasonChanges_…` (2) |
| M6a Wächter `Squad.Count > 0` entfernt (M2b aus #662) | `SeasonChange_GermanyConditionTechniqueKept_FormRolled` |
| M6b WM-Kader hinten anhängen (M3 aus #662) | `FullSquad_ClickDoesNothing_KeeperSwapTakesRowThree` |
| M5a gemerkte Seite beim Saisonwechsel nicht zurückgesetzt | **grün**, auch in der ganzen UI-Suite (R14NT-N1) |
| M5b „erster Aufbau eines Zugs“ nicht geprüft | `…ReturnInSameTurn_KeepsRememberedGroup` |
| M5c Stufe `>= 5` statt `== 5` | `CoachMenu_WorldCupPairings_OpensKnockoutFromStageFive` |
| M5d Seite nicht gemerkt | beide UI-Tests zu A293 |
| M5e Endrunde auch außerhalb des Bundestrainer-Menüs | grün (ohne Wirkung: Vereinsmanager haben im WM-Zeitraum keinen eigenen Halt, der die Kachel vor dem Bundestrainer öffnet) |

**Sollwert `JugendTeamsTests.ExpectedPassive`:** Rückmutation mit M1 und mit M1b, den alten Sollwert von `b09c755` eingesetzt: beide
Male grün (2/2). Der neue Wert stammt also allein aus dem Wegfall der Wanderung in der WM-Schleife.

**Mehrsaisonprobe `Mode4_FourSeasonChanges_WorldCupLoopEverySeason`** (passiv und aktiv): grün. Sie prüft je Saison die echte
Ernennung, eine vollständige WM bis zur Stufe 7 und Speichern/Laden. M1, M1b, M2b und M4 machen sie rot.

**Fundstellen, selbst nachgelesen:**

- 0x55482/0x55485: 0x9EDA := 0x6AC. Bei 0x554CF/0x554D6 geht es mit Bundestrainer direkt nach 0x55570, ohne Auswürfeln.
- Schleife 0x55570–0x55740:
  - Menü und 0x952C(5, 0) nur bei 0x9EDA ≠ 0xFD und Tagesbyte ≠ 0 (0x55587–0x55597 → 0x555DD).
  - 0x37F/0x380 und 0x426BC bei 0x55599–0x555CA.
  - WM-Spiele 0x30E7B(1) bei 0x555F3.
  - 0x07169 nur bei Tag mod 7 = 0 oder (Tag + 3) mod 7 = 0, also bei Tagen ≡ 0/4 (0x55634–0x5565C).
  - Tageswechsel und Managerschleife 0x55670–0x55721.
  - Rücksprung bei Byte 0x10 oder 0 (0x5572C/0x55740).
  - Kein Aufruf von 0x9D1B und kein 0x952C(5, 1) in der Schleife. Auch 0x07169 (Größe 2729) ruft weder 0x9D1B noch 0x952C. ✓
- 0x31152/0x31157 (`cmp [0x7ae],7` / `jnl 0x311CF`) vor dem einzigen Zähler 0x311C8 ✓
- 0x44B15–0x44B1C (Stufe = 5 → 0x5127 := 10) ✓
- 0x5649D–0x564BA (Rücksetzen der Seiten) ✓

**AGENTS.md §4 und Spielstand:** `WorldCupPairingsPages` hält die Seite in einer `ConditionalWeakTable<GameState, …>` der
UI-Schicht. Sie liegt nicht in Core und nicht im Spielstand. Das Delta ändert weder `SaveGame` noch den Schemafingerabdruck,
und `SchemaFingerprint_MatchesSchemaVersion` ist im Lauf grün. Nach dem Öffnen der Kachel bleibt der Spielstand bytegleich (Test
Z. 110). Das Öffnen ändert keinen Spielzustand.

**Bildschirmfoto-Ablauf `bundestrainer-modus4`:** läuft jetzt durch (am Base brach er ab, R14NT-03). Er erzeugt 6 Bilder
(`01_verein_erster_wm_tag` … `06_verein_naechster_wm_tag`), die ich angesehen habe:

- Vereinsmenü am Samstag, 14. April.
- Zug des Bundestrainers mit „DEUTSCHLAND“, Fahne und 500.000 DM.
- Gesperrte Manager-Reihe.
- Mannschaftsreihe mit „Aufst.“, „Taktik“, „Transfer“, „Training“.
- „Spieler in WM-Kader“ mit 16 Spielern.
- Vereinsmenü am Montag, 16. April.

Die Ernennung im Ablauf ist als `[APPROX]` vermerkt (R14NT-03).

**Erwartet offen, bestätigt:** Issue #664 ist offen (WM-Schleife ohne Bundestrainer, Training und Markt im WM-Zeitraum;
`[APPROX]` in `OnDayWorldCupLoop` und `InWorldCupLoop`). Im Bild 06 erscheint eine Marktmeldung im WM-Zeitraum, auch das gehört zu
#664. R14NT-07 ist von der Leitstelle akzeptiert (Kommentar in #661 vom 08.11.2026).

**Akzeptierte `[APPROX]`:**

- WM-Schleife erst ab der Ernennung (#664).
- Finanzen, Training und Markt vor der Schleife (#664).
- Gemerkte Seite nach dem Laden auf dem Startwert.
- „Erster Aufbau“ wird beim ersten Öffnen der Kachel im Zug erkannt.
- Ernennung im Ablauf `bundestrainer-modus4`.

**Annahmen:** Der Startwert 8 in DGROUP 0x512C ist aus `docs/reverse-engineering.md` übernommen und nicht selbst nachgelesen. Ein
DOSBox-Vergleich der Form an Tag 224/228 liegt weiter nicht vor.
