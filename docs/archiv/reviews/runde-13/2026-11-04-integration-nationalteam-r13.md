# Review Integration Runde 13 – Spur `nationalteam` (PR #587)

Unabhängiges Review (Claude Opus 5.5, geplanter Lauf, frische Sitzung ohne Mitarbeit an #580/#581/#584/#587).
Base `0a34f8761a324cf3db5b6c40161ea0e6de68d2db` (`integration/runde-13-saison`, #586), Head
`c4e557f539f8c343978bc0f64a632084ccb63c63`. Geprüft wurde nur das Delta `0a34f87..c4e557f` (25 Dateien).

## 1. Merge-Empfehlung

**Ja für `c4e557f539f8c343978bc0f64a632084ccb63c63`** (nach #586, auf dem dieser Branch aufbaut). Geprüft am Klon:
`git rev-parse HEAD` = Head, `git status --porcelain` leer. Mit Ausführung. `tools/pruefen.sh` hat am Head alle drei Läufe
bestanden, mit denselben Zahlen wie im PR. Alle verlangten Gegenbeispiele sind am Base rot und am Head grün. Die Fundstellen
von A009, A023, A027, A085, A293, A294 und A300 stimmen mit em.exe und den Spielständen FILE18–FILE20 überein.
Es gibt keinen P0–P2-Befund. Die drei P3-Befunde blockieren nicht.

## 2. Befunde

- **P3 – R13IN-01: Reihenfolge von 0x952C und der kleinen Wanderung im Modus 4 nicht beschrieben.**
  Stelle: [`OnDayManager`](https://github.com/kaaasi/eishockey-manager-redone/blob/c4e557f539f8c343978bc0f64a632084ccb63c63/src/EishockeyManager.Core/GameState.Integration.cs#L51)
  ruft `UpdateNationalTeamValues()` nach `OnDayClubSeason` auf, also nach
  [`DriftTeamValues(…, Weekly)`](https://github.com/kaaasi/eishockey-manager-redone/blob/c4e557f539f8c343978bc0f64a632084ccb63c63/src/EishockeyManager.Core/GameState.Integration.cs#L82).
  Die Wanderung würfelt auch die Form 0x1C/0x1D der Nationalteams neu. Danach überschreibt 0x952C sie mit der Form der
  Aufstellung. Im Original liegt der Aufruf 0x952C(5, 1) im Tageslauf bei Datei 0x54F9C–0x54FBF, die Wanderung bei Datei
  0x5525A–0x55292. Beide liegen in derselben Tagesfunktion 0x54C9F, die Wanderung kommt dort also nach 0x952C. Das ist nur
  aus den Adressen gelesen, die Sprünge dazwischen habe ich nicht verfolgt.
  Wenn die Reihenfolge stimmt: Im Modus 4 zeigt das Remake an einem Wandertag ohne WM-Spiel für Deutschland die Form der
  Aufstellung. Das Original zeigt dort die gewürfelte Form. An WM-Tagen rechnet 0x555D6 nach dem Menü ohnehin neu, für die
  Spiele gibt es also keinen Unterschied, nur für „Stärke:“ und die Stärketabelle. Im Modus „nur Weltkugel“ gibt es keinen
  Unterschied, weil es dort keinen Tageslauf gibt.
  Vorschlag: den `[APPROX]`-Punkt in `UpdateNationalTeamValues` um die Wanderung ergänzen oder die Reihenfolge nachlesen.
  Nach L9 ist ein Umzug des Aufrufs vor die Wanderung erlaubt („Tageslauf des Bundestrainers“). Kandidat für Runde 14.
- **P3 – R13IN-02: Kein Regressionstest für R13RD-04 (Länderwerte über den Saisonwechsel).**
  `docs/arbeitspakete.md` meldet R13RD-04 als erledigt. Der Kommentar in
  [`UpdateNationalTeamValues`](https://github.com/kaaasi/eishockey-manager-redone/blob/c4e557f539f8c343978bc0f64a632084ccb63c63/src/EishockeyManager.Core/CupsNational.GameState.cs#L83)
  nennt FILE20 → FILE21. Einen Test dazu gibt es nicht (`grep R13RD-04 tests` leer).
  Mein Gegenbeispiel (siehe 3.) ist am Head grün: Modus 1, nur ein Stürmer mit 88/96/57, Werte (0, 0, 9, 10). Über
  `StartNextSeason` bleiben die Werte (0, 0, 9, 10), die Form liegt danach in 45…55. Auch der erste Tag nach der neuen Ernennung
  (leerer Kader) schreibt nichts.
  Vorschlag: diesen Ablauf als Test in `NationalteamRunde13Tests` übernehmen (Spur `nationalteam`, Runde 14).
- **P3 – R13IN-03: Testdateien anderer Spuren geändert (wie R13IS-03).**
  Betroffen sind `IntegrationRunde8Welle3Tests`, `SpielberechnungKomplett4Tests`, `WettbewerbeSpiel3Tests`,
  `WettbewerbeNationalteamTests`, `PixelWettbewerbeTests` und der Ablauf `Scenarios/Wettbewerbe.cs`. Die Regel ist „Tests nur
  in den Testdateien der eigenen Spur“. Jede Änderung ist klein, folgt zwingend aus A009, A028 oder A293 und ist in
  #580/#581/#584 einzeln begründet. `UI/ShortMessageTexts.cs` ändert nur einen Kommentar.
  Vorschlag: Die Leitstelle bestätigt das in einem Satz.

Sonst habe ich nichts gefunden. Geprüft wurden auch diese Punkte:

- **L9:** Das Delta ändert `GameState.Integration.cs` nur um den Aufruf `UpdateNationalTeamValues()`.
- **Reihenfolge zur Ernennung:** 0x952C kommt vor `OnNationalMarkerDay`. Das passt zum Original: Der Tageslauf ruft 0x952C
  bei Datei 0x54F9C, der Tageszähler steigt erst bei Datei 0x553A2, die Ernennung folgt danach. Am Ernennungstag gibt es
  vorher keinen Bundestrainer, und `UpdateNationalTeamValues` kehrt zurück.
- **L10:** `StatisticsStrength.cs` liest nur noch die gespeicherten Felder.
- **Dateigrenzen:** Alle übrigen Dateien gehören der Spur.
- **Spielstand:** `_germanyRatings` ist ein Feld von `GameState`. Der Writer schreibt Felder per Reflexion, also bleibt der
  Wert beim Speichern erhalten.

## 3. Ausgeführte Prüfungen

**Umgebung:** `tools/claude/fetch-reference.sh original`, `tools/claude/setup-workspace.sh`, SDK 10.0.402. Klon am Head,
`git status --porcelain` leer. Für die Gegenbeispiele gab es getrennte Worktrees an Base und Head. In beide wurden dieselben
Testdateien kopiert, aber nicht committet.

**`tools/pruefen.sh` (alle drei Läufe) am Head `c4e557f`:**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2192 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1845 | 0 | 311 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1846 | 346 | 0 |

**Gegenbeispiele.** Die eigenen Tests kommen ohne neue API aus, damit sie auch am Base kompilieren. Gelaufen sind sie mit
`EM_REQUIRE_GAMEDATA=1 dotnet test … --filter …`.

| ID | Ablauf | Base | Head |
|---|---|---|---|
| A009 Freundschaftsspiel | „auto“, `AutoFormation`, `PlayFriendly(52)` → `NationalRatings(Germany)` unverändert | rot | grün |
| A009 Tageslauf | `AutoFormation`, `Continue()` → Länderwerte = `LineupTeamValues(Formation)` | rot | grün |
| A009 leere Aufstellung | `ClearFormation`, `Continue()` → Werte aus der Automatik (`[APPROX]`, L7) | rot (Werte bleiben) | grün |
| A010 verdrängter Spieler | Bildschirm: Platz Abwehr Reihe 1 anklicken, dann Linksklick auf einen RESERVE-Verteidiger → Formation unverändert, er bleibt eingesetzt | rot (Verdrängung) | grün |
| A028 | „nur Weltkugel“, `AutoNominate`, Halte bis Saisonende = alle WM-Tage (Seeds 7, 11) | Seed 11 rot, Seed 7 grün | grün |
| A294 | Tabelle WM: Linksklick (100 \| 30) und (300 \| 30) → bleibt; Rechtsklick → Hauptmenü | rot | grün |
| A085 Gleichstand | zwei Stürmer mit Id-Reihenfolge ≠ Namensreihenfolge auf 99/99/99 → kleinere Id zuerst | rot | grün |
| A293/A294 (Tests des PR) | `NationalteamRunde13UiTests`, am Base eingespielt | 2 rot | 2 grün |
| R13RD-04 | siehe R13IN-02 | – | grün |

**Gegenproben (Mutation am Head-Worktree, danach zurückgesetzt):**

| Nr. | Mutation | Ergebnis |
|---|---|---|
| M1 | Aufruf `UpdateNationalTeamValues()` in `OnDayManager` entfernt | `A009_ValuesFollowLineupOnlyAtDayOrLeaving` rot (1/11) |
| M2 | `.ThenBy(c => c.Player.Id)` in `AutoNominate` entfernt | `A085_AutoNominate_TieKeepsCandidateOrder_NotName` rot |
| M3 | `OnNationalLineupLeft()` in `NationalLineupScreen.Leave` entfernt | `A010_FullLine_NoRoom_ClickOnPlacedTakesOut` rot |

**`[ORIGINAL]`-Fundstellen gegen `docs/analyse/re-runde-13.md`, Sitzung 1:**

- **Spielstände** (`emtool hok dump FILE18/19/20.HOK 5DC3 far:9657 far:8253`, Satz 5 = 5 · 0x618, Platz 0x34; Verein 54 Felder
  0x18–0x1D):
  - **A023:** KÜNAST 79/81/53 → 80/83/55. MERK 84/81/50. ZERWESZ und SCHINKO bleiben bei +0x10 = 99. Alle anderen Felder
    +1/+2/+2.
  - **A085:** Plätze 0–2 Torhüter, 3–12 Verteidiger, 13–28 Stürmer, je Art absteigend. LEDLIN (Pool 129) vor DEISENBERGER
    (169), je 228. LEE (114) vor KUHNKE (166), je 220.
  - **A009:** FILE18 und FILE19 beide `58 5A 53 57 33 33` (88 90 83 87 51 51). FILE20 `00 09 00 0A 00 06`, nur JOORIS 88/96/57
    in Reihe 1 Sturm (+0x15 = 0x81).
- **em.exe** (`ndisasm`):
  - **A009:** 0x54F9C–0x54FBF ruft 0x28:0x146C(m, 1) für Manager 0…n−1, für Manager 5 bei DGROUP 0x9EDA < 0x3F.
    0x555D6 ruft 0x28:0x146C(5, 0) nach dem Menü, wenn Ereignis 0x10.
  - **A028:** 0x5557F–0x555A3: Menü bei 0x9EDA ≠ 0xFD und Tagesereignis ≠ 0, ohne Abfrage des eigenen Spiels.
  - **A027:** 0x54D19 setzt 0xD0BA = 1. „auto“ (0xF8:0x61) läuft nur bei DGROUP 0x1DB ≠ 0.
  - **A300:** 0x454C2 prüft nur 0xA70E = 5 und einen Zähler < 10.
  - **A293:** 0x2FA4A–0x2FA6C: Klickfläche y 211–222, x 237–293 → 12 − Gruppe.
  - **A294:** 0x2FA2D: Bei Ereignis 1 werden Knopf und Zeilen geprüft. 0x2FACC: Ausgang nur bei Ereignis 3 oder
    DGROUP 0x1DB ≠ 0, sonst zurück nach 0x2FA1E.
  - Endrunde über die Paarungen: 0x3AC67 ruft 0xE8:0x75 mit Wettbewerb 3.
- **A284:** Nur Codelesen in `NationalMenu.cs` gegen die Fundstellen 0x54D19/0x55587. Das Bild `file11_03` habe ich nicht
  verglichen.
- **Pixel A293:** Bildschirmfoto `emtool screens … wm` (`03_gruppe1_gespielt`) neben `file19_19_gruppe1_tabelle_ende`. Die
  Bilder `file19_*`/`file2*_*` stammen alle aus dem Tagesablauf und zeigen keinen Knopf. Das passt zu 0x2F625 (Knopf nur aus
  dem Menü). Die Lage (237 | 211) ist deshalb nur aus em.exe belegt (0x4C196 bei 0xED | 0xD3), nicht aus einem Bild. Der
  frühere Knopf „Endrunde“ bei x = 173 fehlt am Head.

**Erwartet offen, bestätigt:**

- `[APPROX]` Escape schließt die WM-Tabelle: `WorldCupScreen.Update`, Kommentar vorhanden.
- Nebenbefunde Reihenlogik der Vereinsaufstellung (wie A010) und Sperre Transfer/Training ab der Gruppenphase: Sie stehen im
  Integrationsabschnitt von `docs/arbeitspakete.md` als Kandidaten für Runde 14.

**Akzeptierte `[APPROX]`:**

- Leere Formation → Automatik statt 0 (`TeamValueFormation`, L7).
- Die Werte werden beim Verlassen nur nach einer Änderung neu gerechnet (E4).
- Die Rechnung läuft an jedem Tag vor den Spielen.
- Gleichstand nach `Player.Id` statt Poolindex.
- Feste Plätze je Art statt Ausweichen in die andere Hälfte (0x413E8–0x414ED).
- Spielerinfo auf Rechtsklick fehlt.
- Escape in Aufstellung und WM-Tabelle.

**Annahmen (nicht geprüft):**

- Die Reihenfolge innerhalb von 0x54C9F (R13IN-01).
- Die Werte 0 bei einer im Original ganz leeren Aufstellung (keine Aufnahme, L7).
