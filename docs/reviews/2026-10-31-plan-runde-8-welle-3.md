# Review PR #255 – Plan Runde 8, Welle 3

PR #255 (`plan/runde-8-welle-3`) · Base `9fab85493372587d7c936ac906544ac974ffba16` · Head `c60d0ccc27f356c20c35e1f141931587d5835927`
Reviewer: Claude (geplante Sitzung `…9DW`; der Plan-Commit stammt aus Sitzung `…6cc`, an #255 nicht beteiligt).
Diff: ein Commit, nur `docs/arbeitspakete.md` (+30 Zeilen, Z. 253–281).

Links: `A` = `https://github.com/kaaasi/eishockey-manager-redone/blob/c60d0ccc27f356c20c35e1f141931587d5835927/docs/arbeitspakete.md`,
`S` = `https://github.com/kaaasi/eishockey-manager-redone/blob/c60d0ccc27f356c20c35e1f141931587d5835927/src/EishockeyManager.UI/Screens`.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head – **nur Codelesen** (reiner Doku-PR, kein Build, kein `tools/pruefen.sh`, kein
Testnachweis nach `zusammenarbeit.md` §6 Nr. 4). Die Dateigrenzen der sechs Spuren sind bis auf eine Lücke
überschneidungsfrei, die übernommenen Aufrufstellen gibt es in den genannten Dateien. Zwei P2 verhindern die Freigabe:
eine Folgearbeit aus der Integration fällt durch (R8W3-01), und `nationalteam-2` kann den Europapokal-Teil von #194
innerhalb seiner Dateigrenzen nicht anmelden (R8W3-02). Beide sind mit je einem Halbsatz in der Tabelle zu beheben;
danach genügt eine Nachprüfung der Zeilen A#L271–L277.

## 2. Befunde

### R8W3-01 · P2 – Spielerdaten-Fenster aus der Aufstellung ist keiner Stelle zugeordnet

[A#L237-L238](https://github.com/kaaasi/eishockey-manager-redone/blob/c60d0ccc27f356c20c35e1f141931587d5835927/docs/arbeitspakete.md#L237-L238),
[A#L269-L277](https://github.com/kaaasi/eishockey-manager-redone/blob/c60d0ccc27f356c20c35e1f141931587d5835927/docs/arbeitspakete.md#L269-L277).

- Die Folgearbeiten der Integration nennen „Öffnen aus Aufstellung und Ranglisten“ (#249, Aufrufstelle 3: 0x041AC wird
  im Original auch aus der Aufstellung 0x3FFE0/0x40364 gerufen).
- Ranglisten → `statistiken-verein` ✓. Die Aufstellung steht in keiner Zeile der Welle-3-Tabelle.
- Gegenbeispiel: `LineupScreen.cs` nutzt am Head nur `PlayerHintLine` (Z. 268–273), kein `PlayerInfoPopup`/
  `PlayerDataWindow`. Die Datei gehört der Spur `spielerinfo` („nur Aufruf“), die in Welle 3 keine Folgespur hat; nach
  A#L260 baut die Leitstelle keine Aufrufe in Spurdateien ein, und `leitstelle-r8` führt `LineupScreen.cs` nicht.
  Die Arbeit hat damit keinen Bearbeiter.
- Abhilfe: einer Spur zuordnen (naheliegend `statistiken-verein`, die denselben Aufruf mit `PlayerDataContext.Create`
  für die Ranglisten baut, mit `UI/Screens/Team/LineupScreen.cs` „nur Aufruf“) oder ausdrücklich `leitstelle-r8` mit
  dieser Datei.

### R8W3-02 · P2 – `nationalteam-2` kann die Europapokal-Paarungsansicht (#194) nicht anmelden

[A#L274](https://github.com/kaaasi/eishockey-manager-redone/blob/c60d0ccc27f356c20c35e1f141931587d5835927/docs/arbeitspakete.md#L274),
[EuropeanCupScreen.cs Z. 173](https://github.com/kaaasi/eishockey-manager-redone/blob/c60d0ccc27f356c20c35e1f141931587d5835927/src/EishockeyManager.UI/Screens/Competitions/EuropeanCupScreen.cs#L173),
[CupTableMenuEntries.cs Z. 27](https://github.com/kaaasi/eishockey-manager-redone/blob/c60d0ccc27f356c20c35e1f141931587d5835927/src/EishockeyManager.UI/Screens/Competitions/CupTableMenuEntries.cs#L27).

- Ziel der Spur: „#194 Paarungsansicht WM/Europapokal (`CupPairingsScreen.cs`)“ (0x2FC2F laut `docs/re-abdeckung.md`
  Z. 104 für beide).
- WM ist abgedeckt: Kachel „WM“ in `WorldCupScreen.cs` (`WorldCup*.cs` gehört der Spur).
- Die Kachel „Europapokal“ (Paarungen) meldet `EuropeanCupScreen.cs` an; „Tabelle Europapokal“ öffnet denselben
  Bildschirm aus `CupTableMenuEntries.cs` (Spur `tabellen-original-2`). `EuropeanCupScreen.cs` gehört keiner Spur.
- Gegenbeispiel: Eine zweite Anmeldung `[MenuEntry("Europapokal")]` in `CupPairingsScreen.cs` wirft beim Start
  („Kachel … ist doppelt angemeldet“, `UI/MenuEntries.cs` Z. 45). Ohne Änderung an `EuropeanCupScreen.cs` (bzw. einem
  Knopf darin, falls 0x2FC2F aus dem Gruppenbildschirm 0x31CD5 erreicht wird – Aufrufer 0x32E23) ist der Europapokal-Teil
  nicht erreichbar.
- Abhilfe: `UI/Screens/Competitions/EuropeanCupScreen.cs` (nur Anmeldung bzw. Aufruf) in die Dateigrenzen von
  `nationalteam-2`, oder den Europapokal-Teil ausdrücklich aus #194 nehmen (das Issue nennt nur die WM).

### R8W3-03 · P3 – Weitere Restlücken ohne Zuordnung

- **R8-A03** (Integrationsreview, Teamwerte: Tagesform beim kampflosen Spiel, Zusatzzug – Mutationen fallen nur in
  Abdrucktests auf) steht weder in A#L245–L251 noch in der Welle-3-Tabelle. Passt zu `spielstaerke-manager-2`.
- **Goldtest aus Original-Spielständen** (A#L86, „→ `transfer-original`“) ist in A#L275 nicht übernommen.
- **F46-01, W51-01** (A#L251) sind nicht zugeordnet; W51-01 ist mit #250 gegenstandslos, F46-01 nicht nachträglich
  behebbar – ein Satz („erledigt“/„entfällt“) neben R8-A01 (A#L281) schließt beide.

### R8W3-04 · P3 – SW-03 liegt außerhalb der Dateigrenzen von `leitstelle-r8`

[A#L277](https://github.com/kaaasi/eishockey-manager-redone/blob/c60d0ccc27f356c20c35e1f141931587d5835927/docs/arbeitspakete.md#L277).
Der falsche Verweis `PlayerValueTests` steht in `src/EishockeyManager.Core/Player.Value.cs` Z. 74 (Spur `spielerwert`,
abgeschlossen). Die Dateispalte von `leitstelle-r8` nennt die Datei nicht. Abhilfe: `Core/Player.Value.cs` (nur
Kommentar) ergänzen.

### R8W3-05 · P3 – „übernimmt“ greift über die Dateigrenzen; eine Zeilenangabe ist falsch

- `tabellen-original-2`: „`docs/wettbewerbe.md` nur Z. ~331 (Sortierung 0x5CCB)“ – Z. 331 ist die Sperrtabelle im Abschnitt
  „nur Weltkugel“; die Vermutung zur Sortierung steht in Z. 366 und **Z. 373** (Abschnitt „Bundestrainer im Modus 4“).
- `tabellen-original-2` übernimmt `docs/saison.md`, darf dort aber nur den eigenen Abschnitt (Z. 93 ff.) ändern; die
  Vermutung steht in Z. 90 (Abschnitt „Statusbyte“). Für `wettbewerbe.md` gibt es die Ausnahme, für `saison.md` nicht.
- `statistiken-verein` übernimmt den Hinweis „Nicht umgesetzt …“ (`docs/wettbewerbe.md` Z. 161, Abschnitt „Statistiken über
  Saisons“), die Dateigrenze „wie `statistiken`“ deckt nur den eigenen Abschnitt (Z. 166 ff.).
- Abhilfe: Ausnahmen mit Zeile in die Dateispalte (`wettbewerbe.md` Z. 373, `saison.md` Z. 90, `wettbewerbe.md` Z. 161).

### R8W3-06 · P3 – `NationalLineupScreen.cs`: Hinweiszeile, nicht Spielerdaten-Fenster

[A#L274](https://github.com/kaaasi/eishockey-manager-redone/blob/c60d0ccc27f356c20c35e1f141931587d5835927/docs/arbeitspakete.md#L274).
#249 (Aufrufstelle 2) verlangt für `ContractsScreen.cs` und `NationalLineupScreen.cs` die Umstellung der genäherten
Hinweiszeile auf `PlayerHintLine` ([NationalLineupScreen.cs Z. 248–261](https://github.com/kaaasi/eishockey-manager-redone/blob/c60d0ccc27f356c20c35e1f141931587d5835927/src/EishockeyManager.UI/Screens/Competitions/NationalLineupScreen.cs#L248-L261),
`< 18 => "Rückennummer …"`), kein Fenster. Die Zeile von `nationalteam-2` sagt „Aufruf Spielerdaten-Fenster“; dass das
Fenster aus der Nationalaufstellung gerufen wird, ist nicht belegt. Abhilfe: „Hinweiszeile auf `PlayerHintLine`“ (Fenster
nur mit Beleg).

### R8W3-07 · P3 – Widerspruch beim Anschluss #193 und mögliche Überschneidung bei Seed-Proben

- A#L260: „die Leitstelle baut keine Aufrufe in diese Dateien ein“; A#L272: #193 „Anschluss nach Freundschaftsspielen
  nennt die Spur, die Leitstelle schließt an“ – der Anschluss liegt in `Core/CupsNational*.cs` (Datei von `nationalteam-2`).
  Wegen der Integrationsfolge (`nationalteam-2` vor `spielstaerke-manager-2`) ist das sachlich richtig, braucht aber die
  Ausnahme in A#L260.
- `leitstelle-r8` baut `BoerseStartTests`/`FinanceWerbungTests` um (R8-A02), parallel dazu dürfen
  `spielstaerke-manager-2` und `transfer-original-vertrag` nach A#L59–L62 Sollwerte bei geändertem Zufallsverbrauch
  anpassen – in der Integration war genau das bei diesen beiden Dateien nötig (Seeds 7 → 8, 15 → 16). Abhilfe: Spuren
  ändern diese zwei Dateien nicht; Anpassung nur durch die Leitstelle in der Integration.
- R8-C01 betrifft `TransferTests.cs`, `PlayerFactoryTests.cs`, `LifecycleRegressionTests.cs`, `WaveBReviewRegressionTests.cs`
  (fremde, ältere Testdateien); die Dateispalte von `transfer-original-vertrag` sollte sie nennen.

### Geprüft ohne Befund

- **(1) Dateigrenzen:** Abgleich der Spalte „Dateigrenzen“ samt Welle-1-Abschnitten (A#L66–L222) gegen `git ls-files`.
  Eindeutig je Spur: `Core/NewGame.cs` (nur `spielstaerke-manager-2`), `UI/Screens/SetupScreen.cs` (nur `nationalteam-2`;
  die Ursache von #195 liegt laut Übergabe in #243 dort, Z. 200–202), `CupPairingsScreen.cs`, `WorldCupStatisticsScreen.cs`,
  `ClubStatisticsScreen.cs`, `CalendarScreen.cs` (neu, je eine Spur), `Core/SeasonFlow.cs` (nur Kommentar,
  `leitstelle-r8`; `optionen-kalender-2` nur Vorschlag), `docs/re-abdeckung.md` und `tools/reverse-engineering/`
  (nur `leitstelle-r8`, `befunde-p3` hat keine Folgespur). `Squad.Tactics.cs` bleibt nach Mitgliedern geteilt wie in
  Welle 1; der vorläufige Eintrag in `Core/SaveGame.cs` ist eine bewusste, von der Leitstelle aufgelöste Überschneidung.
  Ausnahmen: R8W3-02, R8W3-07.
- **(2) Zuordnung:** Funktionskarte, `bildschirme.md` Z. 103 (am Head: „Bilanz = letzte neun Spiele (groß = Heim …)“),
  Kommentar `SeasonFlow.cs` (Z. 359) → `leitstelle-r8`; Sortierung in `CupsNationalMode.cs` (Z. 125–126) →
  `nationalteam-2`; `docs/saison.md`/`docs/wettbewerbe.md` → `tabellen-original-2`; „Nicht umgesetzt …“, Ranglisten →
  `statistiken-verein`; Lader nach `NewGame.Create` → `spielstaerke-manager-2`; `TransferScreen`/`ContractsScreen` →
  `transfer-original-vertrag`. Alle offenen Teile der Spuren (A#L239–L243) stehen je genau einmal in der Spalte „Ziel“.
  R8-A01 (entfällt), R8-A02, R8-B01 … R8-B04, R8-C01, R8-C02, SW-01 … SW-03 je genau einmal. Ausnahmen: R8W3-01, R8W3-03.
- **(3) Aufrufstellen:** `TransferScreen.cs` Z. 121 `_info.Show(l.Player, l.Price)` (ohne `PlayerDataContext`);
  `ContractsScreen.cs` Z. 280–291 genäherte Hinweiszeile; `PlayerStatisticsScreen.cs` Z. 41 (Fundstelle 0x26B13, noch
  kein Aufruf); `NationalLineupScreen.cs` Z. 248–261 (siehe R8W3-06); `CupsNationalMode.cs` Z. 125–126.
  `PlayerDataContext.Create(GameState, Player, Club?, Club, int)` und `PlayerInfoPopup.Show(Player, long, PlayerDataContext)`
  gibt es (`PlayerDataWindow.cs` Z. 31, `PlayerInfoPopup.cs` Z. 54).

## 3. Ausgeführte Prüfungen

Nur Codelesen am exakten Head: `git rev-parse HEAD` = `c60d0cc…`, `git status --porcelain` leer.
`git diff --stat 9fab854 c60d0cc` (1 Datei), `git ls-files`, `grep` nach Aufrufstellen, `[MenuEntry(` und
Sortierungskommentaren; PR-Texte/Übergaben #243 und #249 sowie die Reviews `2026-10-31-integration-runde-8.md` und
`2026-10-31-spielerwert-239.md` über `gh api` (REST) gelesen. Kein Build, kein `tools/pruefen.sh` (reiner Doku-PR).

**Akzeptierte `[APPROX]`:** keine (Doku-PR). **Annahmen:** „genau einer Stelle“ heißt einer Spurzeile oder einem
ausdrücklichen Vermerk (wie bei R8-A01); die Spalte „Dateigrenzen“ gilt zusammen mit dem Welle-1-Abschnitt, die Spalte
„übernimmt“ erweitert sie nicht stillschweigend.
