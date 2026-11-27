# Review: `pruefung-karte` Runde 13 – PR #607 und #608 (gestapelt)

Unabhängiges Review (geplanter Lauf, frische Sitzung ohne Implementierungskontext) nach `docs/vorlagen/review-anfrage.md`.
Base `5309690e38a2e8ad36c37e55e314de75578943f6` (`main`), Head #607 `558b7f602c9390d40c960d945080678d85d6a6e7`,
Head #608 `0728c178122a08feceff7db78f789c4e33aced12` (oberster Stand, Tree `8b21c45`), gegen den Base geprüft.
Checkout: Klon, `git rev-parse HEAD` = `0728c17…`, `git status --porcelain` leer; Ausführung am nachgewiesenen Checkout.

## 1. Merge-Empfehlung

**Ja für #607 `558b7f6` und #608 `0728c17`** (in dieser Reihenfolge; #608 hat die Base `paket/pruefung-karte-1`). Alle drei Läufe von
`tools/pruefen.sh` am obersten Head sind grün. Die Freiprüfung erkennt Zustandsänderungen in `Draw`, `Enter` und Konstruktor, auch
bei einem ausgenommenen Bildschirm (drei Mutationen, jeweils 3 von 3 Zuständen rot). Die Funktionskarte ist reproduzierbar
(257/2/104/1, zweiter Lauf ohne Diff). Die HOK-Regel wirkt wie beschrieben. Die Inventur deckt alle Runde-13-IDs aus
`re-runde-13.md` und den vier Integrations-PRs ab. Alle Befunde sind P3, keiner blockiert. R13PK-01 (drei Tabellenzeilen
zerbrochen) lässt sich vor dem Merge mit `\|` beheben.

## 2. Befunde

**R13PK-01 (P3) – Drei Inventurzeilen zerbrechen die Tabelle.** In [`inventur.md:589`](https://github.com/kaaasi/eishockey-manager-redone/blob/0728c178122a08feceff7db78f789c4e33aced12/docs/analyse/inventur.md#L589)
(A293), [`:601`](https://github.com/kaaasi/eishockey-manager-redone/blob/0728c178122a08feceff7db78f789c4e33aced12/docs/analyse/inventur.md#L601) (A305) und
[`:623`](https://github.com/kaaasi/eishockey-manager-redone/blob/0728c178122a08feceff7db78f789c4e33aced12/docs/analyse/inventur.md#L623) (A327)
stehen Koordinaten als „(237 | 211)“, „(138 | 183)/(138 | 192)“ usw. mit unmaskiertem `|`. Die Zeilen haben so 10, 11 und 13
statt 9 Zellen. Auf GitHub endet die Spalte „Beleg“ bei „(237“, die Spalten „Kand.“ und „Prio“ zeigen Textreste, und die echten
Werte (z. B. A305 `M-K4`/`P3`) fallen weg.
Gegenbeispiel: Zellen je Tabellenzeile zählen (Trennzeichen `|` ohne vorangestelltes `\`). Am Base sind A375 und F099
betroffen (beide schon vorher), am Head zusätzlich A293, A305 und A327. Abhilfe: `\|` oder „(237, 211)“.

**R13PK-02 (P3) – Zwei Zusagen zur Freiprüfung sind nicht umgesetzt.**
- [`BildschirmeFreipruefungTests.cs:18`](https://github.com/kaaasi/eishockey-manager-redone/blob/0728c178122a08feceff7db78f789c4e33aced12/tests/EishockeyManager.UI.Tests/BildschirmeFreipruefungTests.cs#L18):
  „scheitert … bei jeder nicht mehr auftretenden bekannten Abweichung“. Der Test prüft nur neue Abweichungen
  (Z. 193–194). Das wirkt heute nicht, weil `Known` leer ist.
- [`ScreenReflection.cs:15`](https://github.com/kaaasi/eishockey-manager-redone/blob/0728c178122a08feceff7db78f789c4e33aced12/tests/EishockeyManager.UI.Tests/Support/ScreenReflection.cs#L15):
  Nicht erzeugbare Konstruktoren werden „gemeldet, damit die Abdeckung … nicht still schrumpft“. Sie erscheinen aber nur, wenn
  `EM_FREIPRUEFUNG_BERICHT` gesetzt ist; `notBuilt` wird nicht geprüft. Bekommt ein neuer Bildschirm einen Parametertyp, den
  `TryResolve` nicht kennt, bleibt er ungeprüft und der Test grün.
  Heute sind 0 Konstruktoren unauflösbar (Bericht unten). Abhilfe: eine erwartete Liste der nicht erzeugbaren Konstruktoren
  festschreiben oder die Kommentare berichtigen.

**R13PK-03 (P3) – Falscher Grund für die Ausnahme `NationalMenu.Open`.**
[`BildschirmeFreipruefungTests.cs:37`](https://github.com/kaaasi/eishockey-manager-redone/blob/0728c178122a08feceff7db78f789c4e33aced12/tests/EishockeyManager.UI.Tests/BildschirmeFreipruefungTests.cs#L37)
verweist auf den `MenuEntries`-Test. `NationalMenu.Open` läuft aber nicht über `MenuEntries`, sondern über `MainMenuScreen.cs:253`.
Mit dem Kachelnamen "" liefert es `null`, und `Host.Show(null)` wirft. Die Ausnahme ist trotzdem in der Sache gedeckt: Alle vier
Bildschirme, die `Open` erzeugen kann, prüft die Freiprüfung direkt (`NationalSquadScreen`, `FriendliesScreen` und
`NationalLineupScreen` in allen Zuständen, `NationalLineupScreen.Tactics` im Zustand `NationalCoach`). Abhilfe: den Grund
berichtigen oder `Open` mit den vier Kachelnamen im Zustand `NationalCoach` aufrufen.

**R13PK-04 (P3, Hinweis) – Zwei Lücken der HOK-Regel.**
`HOK_PREFIX` ([`funktionskarte.py:514`](https://github.com/kaaasi/eishockey-manager-redone/blob/0728c178122a08feceff7db78f789c4e33aced12/tools/reverse-engineering/funktionskarte.py#L514))
gilt nur für die Adresse direkt nach „HOK“. In der Scratch-Kopie zählen „HOK 0x00844, 0x575C3“ und „HOK-Offset 0x575C3“
weiter als Fundstelle (0x575C3 wird von „unbekannt“ zu dokumentiert/umgesetzt).
Am Head kommt das nur einmal vor ([`spielstand-original.md:332`](https://github.com/kaaasi/eishockey-manager-redone/blob/0728c178122a08feceff7db78f789c4e33aced12/docs/formate/spielstand-original.md#L332),
„HOK `0x08EED` / `0x08EF7`“). Ohne Wirkung: Ein zweites „HOK“ ändert die Karte nicht. Für den README genügt der Satz „je
Adresse ein HOK“.

**Bestätigt offen (erwartet):** Die Freiprüfung deckt nur Öffnen und Zeichnen ab (keine Eingaben, kein `Update`). Die Statuszeile
`re-dosbox` in [`arbeitspakete.md:66`](https://github.com/kaaasi/eishockey-manager-redone/blob/0728c178122a08feceff7db78f789c4e33aced12/docs/arbeitspakete.md#L66)
nennt weiter „8 offene Fragen an die Leitstelle“. Außerdem gibt es keinen Prüfzustand mit abgeschlossener Saison (`History` leer).
Die Saisonende-Bildschirme deckt deshalb nur `BildschirmeOeffnenZustandTests.SeasonEndScreens_…` ab. Bildschirme mit
Saisonchronik zeichnen in der Freiprüfung nur den leeren Fall (Kandidat Runde 14, kein Befund).

**Außerhalb dieses PRs (nur Hinweis):** `src/EishockeyManager.Core/SeasonFlow.LeagueSlots.cs:9` hat noch `random(b, b + 11)`
(Code-Kommentar, nach L12 Spur `saison`). `MatchEngine.Live.cs:367` begründet `[APPROX]` noch mit „im Original nicht gesucht“,
das gilt nach `re-runde-13.md` (A088, L14) nicht mehr.

## 3. Ausgeführte Prüfungen

**Gegenbeispiele Teil 1 (`BildschirmeFreipruefungTests`)** – Mutation in einem temporären Worktree des Heads, Filter
`BildschirmeFreipruefung`, mit Originaldaten:

| Mutation | Ergebnis |
|---|---|
| keine | 4/4 bestanden (`Detector_SeesStateChange` und alle drei Zustände; die `private`-Theorie läuft unter xUnit 2.9.2) |
| `StadiumScreen.Draw`: `Game.Random.Next()` | rot in `Start`, `MidSeason`, `NationalCoach` |
| `SeasonEndSummaryScreen`-Konstruktor (ausgenommen): `host.Game!.Random.Next()` | rot in allen drei Zuständen; die Ausnahme verdeckt die Zustandsprüfung nicht |
| `NationalLineupScreen.Enter` (neu): `Clubs[0].Players[0].Goals++` | rot in allen drei Zuständen |

Bericht (`EM_FREIPRUEFUNG_BERICHT`): `Start` 112 geöffnet / 0 Abweichungen / 5 nicht prüfbar, `MidSeason` 112/0/5,
`NationalCoach` 113/0/4. Nicht prüfbar sind nur die genannten Ausnahmen; unauflösbare Parametertypen: 0.

Vollständigkeit der Liste: `AllScreenTypes` sieht nur die UI-Assembly. Außerhalb davon leitet nur
`Tools/Scenarios/Meldungen.cs` `DialogScreen` ab, eine Hilfe für Bildschirmfotos und kein Bildschirm des Spiels. Ausnahmen:
- `SeasonEndScreen` und `SeasonEndSummaryScreen` (Konstruktor und `Open`) zu Recht: `History[^1]` wirft ohne Saisonende, und
  `SeasonEndScreens_OpeningAndDrawingDoNotChangeGameState` prüft beide im echten Saisonende.
- `NationalLineupScreen.Tactics` zu Recht: im Zustand `NationalCoach` geöffnet.
- `NationalMenu.Open` in der Sache zu Recht, mit falschem Grund (R13PK-03).

**Gegenbeispiele Teil 2:**
- `python3 tools/reverse-engineering/funktionskarte.py gamedata/em.exe --repo . --write` zweimal: beide Läufe ohne Diff gegen
  den Head. Spielcode umgesetzt/genähert/dokumentiert/unbekannt = **257/2/104/1** (gesamt 268/2/152/257).
- HOK-Regel in einer Scratch-Kopie, Testzeile in `docs/saison.md` an der unbekannten Funktion 0x575C3:
  - „HOK 0x575C3“, „HOK \`0x575C3\`“, „(HOK 0x575C3)“: bleibt unbekannt.
  - „Datei 0x575C3“ und bloß „0x575C3“: zählen als Fundstelle.
  - Lücken siehe R13PK-04.

**Inventur „Stand Runde 13“:** 32 Zeilen und der Sammelabsatz (Z. 271) gezählt.
- Alle IDs aus `re-runde-13.md` (22) und aus den PR-Texten #586 (A058, A088, A163), #587 (10 IDs), #588 (A324) und #600
  (7 IDs) tragen einen Eintrag „Stand Runde 13“. Die übrigen Zeilen gehen auf die Teil-PRs #570/#572/#573/#579/#589 zurück.
- Spalten: Geändert ist nur die Belegspalte, und nur durch Anhängen. Ausnahmen: A324 (eigener Runde-13-Vermerk neu gefasst) und
  R13PK-01.
- Stichprobe gegen Code und `em.exe` am Head:
  - **A009:** `0x41506` in `CupsNational.GameState.cs`/`NationalLineupScreen.cs`, `NationalTeamValues` angeschlossen.
  - **A091:** `RenumberPlayers`, Datei 0x42CB1/0x425B6, `KaderStartRunde13Teil4Tests` vorhanden; `SortedPlayers` in `TeamUi.cs`
    weiter benutzt, also zu Recht „offen“.
  - **A166:** `AutoLineupRule.FillReserves` in `Squad.AutoLineup.cs`/`Squad.Lineup.cs`.
  - **A088:** `ResetMatchCounters` setzt auch `TransferMarket.FreeAgents` zurück, `[APPROX]` bleibt; der Status „bleibt offen
    nach L14“ stimmt.
  - **A058:** `SaisonWeltkugelTests`.
  - **A028:** `NationalteamRunde13Teil2Tests`.
  - **A016:** `ClubRecordsSeasonTests`, Datei 0x5030D.
  - **A305:** `ArrowPair`, Datei 0x2BA72.
  - **A284:** Datei 0x54D19 in `NationalMenu.cs`.
  - **A347:** `NewGame.StartOrder`, `SetupScreen.ClubAt`.
  - **A112:** `PlaceInStartLeague`.
  - **F128** (aus #573) ist zu Recht nicht neu vermerkt, weil schon „erledigt“.

**Doku nach L12:**
- `spielberechnung.md` §14 (Z. 476 ff., innerhalb von §14): In `em.exe` kommt der Far-Call `9A 43 00 F0 00` genau zweimal vor,
  an Datei 0x43A17 und 0x5644E. Datei 0x439BB ruft ein anderes Ziel (`9A 75 00 E0 00`). Die Aussage „nur zwei Aufrufer, keiner
  nach dem Lader“ ist damit bestätigt.
- `re-runde-12.md` S-K6: `lrandom` Datei 0xBFD5 passt zum Code-Kommentar `SeasonFlow.LeagueSlots.cs:15–16`.
- `spielstand-original.md`: Die Befehlsadressen 0x5BE5 (`mov [es:bx+0xd],al`), 0x5C44 (`or byte [es:bx+0x9],0x2`),
  0x5C4C (`+0x17`), 0x5C50 (`+0x0A = 0`), 0x54300 (`cmp word [es:bx+0x22],0xff`) und 0x54314 (`inc word [es:bx+0x22]`) per
  `ndisasm` geprüft, alle wie beschrieben.

**R13RDN-01:**
- Übersicht A088 jetzt „(L4)“: erledigt.
- Z. 732/778 mit Vermerk L12: erledigt.
- `arbeitspakete.md:66`: bleibt offen (erwartet).

**Dateigrenzen:**
- Geändert sind `tests/EishockeyManager.UI.Tests/Support/ScreenReflection.cs` (Support), eine neue Testdatei,
  `tools/reverse-engineering/*`, `re-abdeckung.md` (nur der generierte Block) und `inventur.md`.
- Nach L12 und R13RDN-01 kommen hinzu: `spielberechnung.md` (nur §14), `re-runde-12.md` (S-K6), `spielstand-original.md`
  (Feldbelege) und `re-runde-13.md`. Dazu der eigene Statusabschnitt in `arbeitspakete.md`.
- Kein `src/`, keine gemeinsamen Dateien. Zwischen `558b7f6` und `0728c17` gibt es keine Änderung an `src`, `tests`,
  `tools/pruefen.sh` oder `.github`.
- Alle 4 Commits tragen den Trailer `Claude Sonnet 5.5`.

**CI:** `build` success auf `558b7f6` und auf `0728c17`.

**`tools/pruefen.sh` (alle drei Läufe) am Head `0728c17`:**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2261 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1896 | 0 | 327 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1897 | 364 | 0 |

SDK 10.0.402, Ergebnis: bestanden.

**Akzeptierte `[APPROX]`:** keine neuen in diesen PRs (reine Test- und Doku-Änderungen). Annahmen, nicht geprüft: Die
Kennzeichnung „Stand Runde 13“ gibt die Teil-PRs sinngemäß wieder; nur die Stichprobe oben wurde gegen Code und Binärdatei
gehalten.
