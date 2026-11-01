# Nachprüfung: Plan Runde 9 (PR #275)

- Base `b3a38df7dbdc1372140fc23823478a75d85a0c22` · Head `7638871bff5dc66953db782de3c2903dc3093596` · Vorreview
  `docs/reviews/2026-11-01-plan-runde-9.md` (PR #276, Head `f60797a`) · Reviewer: Claude (geplanter Lauf, eigene
  Sitzung, an PR #275 nicht beteiligt).
- Geprüft: nur Delta `f60797a..7638871` (4 Dateien) und R9P-01 bis -04. **Nur Codelesen** (Doku-PR, Auftrag).
  Checkout per `git fetch origin pull/275/head`, `git rev-parse HEAD` = Head, `git status --porcelain` leer.

## 1. Merge-Empfehlung

**Ja für genau diesen Head (nur Codelesen).** R9P-01 bis -04 sind sachlich umgesetzt. Offen bleiben drei P3. R9N-01
sollte vor dem Start von `optionen-wirkung` klargestellt werden, in einem Folge-PR oder unter „Besonderheiten“ im
Startaufruf. Sonst schreibt ein unbeaufsichtigter Lauf in `Season.cs` hinein oder läuft absehbar in „blockiert“.

## 2. Befunde

### Status der Vorbefunde

- **R9P-01 – erledigt.** Die Muster bleiben überschneidungsfrei gegen `git ls-files src`:
  - `StatisticsClub.cs`, `StatisticsPlayers.cs` und `UI/Screens/Statistics/*` gehören zu spielberechnung, ohne
    `HighscoreScreen.cs`. Dieser bleibt wie `Core/Highscore.cs` ohne Spur, passend zu „Zurückgestellt“.
  - `SeasonEnd*.cs` (5 Dateien, auch Trophäe, Spielende und Zusammenfassung) gehören zu transfer.
  - `StatisticsHistory.cs` gehört zu wettbewerbe, `GameOptions.cs` (neu) zu optionen.
  - R8W3I-06 liegt jetzt bei spielberechnung Teil 2. Die Ausnahme für `optionen-wirkung` steht unter R9N-01.
- **R9P-02 – erledigt.** `spur-start.md` hat jetzt den Abschnitt „Teile und Fortsetzung“ und einen eigenen Startaufruf
  mit Autorisierung. Geregelt sind:
  - Stapelregel mit PR-Base und Rebase,
  - Zähler (der höhere gilt),
  - Prüfung mit `list_triggers`,
  - blockierter Teil,
  - Kettenende mit Label `spur-wartet`. Das Label existiert im Repo (per REST geprüft).

  Restpunkte stehen unter R9N-02.
- **R9P-03 – erledigt.** Die IDs sind so zugeordnet:

  | ID | Zuordnung |
  |---|---|
  | R8-D02 | wettbewerbe 2 |
  | R8-D03 | optionen 1 |
  | W51-02 | transfer 4 |
  | Verdacht 0x952C | spielberechnung 2 |
  | Tore mod 30 | wettbewerbe 1 |
  | R8W3I-10 | aufgeteilt auf spielberechnung 2, wettbewerbe 3 und transfer 4 |
  | 0x445E0 | im Remake vorhanden (`MainMenuScreen.cs` Z. 278, „Vor dem ersten Spieltag.“) |

  Als entfallen geführt: R8-D04, R8-D05 und R8-A03. Der Beleg für R8-A03 ist PR #263: gemergt, der Body nennt A03.
  W51-03 ist behoben, die Leerzeile vor „## C. Ideen“ ist da.
- **R9P-04 – teilweise.** Der Kopf nennt jetzt den fehlenden Abschnitt „Zurückgestellt“, siehe R9N-03.

### R9N-01 (P3) – Ausnahme für `optionen-wirkung` nennt die falsche Datei und lässt den Zeitpunkt offen

Stellen: [`arbeitspakete.md` Z. 50–52](https://github.com/kaaasi/eishockey-manager-redone/blob/7638871bff5dc66953db782de3c2903dc3093596/docs/arbeitspakete.md#L50-L52), [Z. 115–116](https://github.com/kaaasi/eishockey-manager-redone/blob/7638871bff5dc66953db782de3c2903dc3093596/docs/arbeitspakete.md#L115-L116).

- **`Season.cs` passt nicht.** Die Tagestabelle ist statisch: `static readonly DayEvents[] Table` und
  `public static DayEvents Events(int day)` (Z. 74), ohne Zugriff auf den Spielstand. Eine Abfrage der Schalter dort
  bräuchte eine neue Signatur. Die wiederum trifft die Aufrufer in `CupsDeb.cs`, `CupsEurope.cs`, `CupsWorld.cs` und
  `Competitions/*`, und die darf optionen nicht ändern.
  Gegenbeispiel: `grep -rn "DaysOf(DayEvents\.\|SeasonCalendar.Events(" src`.
- **`SeasonFlow.cs` trägt die Wirkung tatsächlich.** Die Pokale laufen über `Competitions`. Ein Schalter „aus“ lässt
  sich als Filter in den Schleifen `MatchOn` (Z. 112–114) und `ProcessDay` (Z. 205–207) umsetzen.
- **Einschalten geht nicht ohne Zusatzarbeit.** Ein Pokal, der beim Spielstart aus war, existiert nicht, denn
  `InitializeCompetitions` in `Cups.GameState.cs` meldet nur einmal an. Er müsste in `StartNextSeason` (Z. 398–399)
  zur `Competitions`-Liste hinzugefügt werden. Ob das unter „Moduswechsel einfügen“ fällt, sagt der Plan nicht.
- **Ausschalten mitten in der Saison** lässt Auslosung und Bildschirme (`DebCupGamesScreen`, `EuropeanCupScreen`) sowie
  die Europapokal-Runden in `FinanceSponsorRating.cs` Z. 165 in einem halben Zustand. Laut `reverse-engineering.md`
  Z. 615 und 627 ist die Folgewirkung im Original nicht belegt.
- **Integrationsfolge:** wettbewerbe vor optionen ist in sich stimmig, weil die kleinen Einfügungen zuletzt kommen.

**Vorschlag:** „in `Season.cs` (Tagestabelle)“ streichen und als erlaubte Stellen benennen: `SeasonFlow.cs`
`MatchOn`/`ProcessDay` (Filter) und `StartNextSeason` (Pokale an- und abmelden). Dazu als `[APPROX]` festlegen:
„Wirkung ab dem nächsten Saisonstart, solange 0x2CD1C nichts anderes belegt“. Braucht es `Cups*.cs`, gilt Schritt 4
(blockiert).

### R9N-02 (P3) – Restpunkte im Fortsetzungsablauf

Stelle: [`spur-start.md` Z. 18–21, 33–57](https://github.com/kaaasi/eishockey-manager-redone/blob/7638871bff5dc66953db782de3c2903dc3093596/docs/vorlagen/spur-start.md#L18-L57).

Ohne Chatverlauf ist der Ablauf ausführbar:

- Der Stand steht im Übergabekommentar.
- Der Zähler ist durch „der höhere gilt“ robust gegen einen Neustart ohne Zähler.
- Die Kette ist auf 3 begrenzt.
- Das Ende ist über Label und Leitstelle sichtbar.

Drei Kleinigkeiten bleiben:

1. **Schritt 3 des Hauptablaufs** (`main`, „sonst den Nutzer fragen“) und **Schritt 5** (`rebase origin/main`) sind
   unverändert. Der neue Abschnitt geht zwar sinngemäß vor, das steht aber nirgends. Ungeregelt bleibt der Fall
   „offener PR der Spur mit Bearbeiter ≠ frei“, etwa nach einem abgebrochenen Lauf ohne Übergabe. Unbeaufsichtigt kann
   niemand gefragt werden.
   Vorschlag: in Schritt 3 „bei geplanten Läufen: siehe Teile und Fortsetzung“, und im Abschnitt Schritt 1: „Bearbeiter
   ≠ frei → wie blockiert (Schritt 4), nichts ändern“.
2. **Doppelstart:** `list_triggers` zeigt nur ausstehende Aufgaben. Eine Fortsetzung, die gerade läuft, ist
   ausgelöst und nicht mehr gelistet. Sie fällt nur über den Bearbeiter-Eintrag im Draft-PR auf. Mit Punkt 1 ist auch
   das abgedeckt.
3. **Modell:** `update_trigger` hat ein Modellfeld, das Werkzeug erlaubt eine Änderung aber nur auf ausdrückliche
   Bitte eines Menschen. In der Praxis greift also immer der Rückfall „Modell der Fortsetzung prüfen“. Das ist harmlos,
   der Satz könnte gleich so lauten.

### R9N-03 (P3) – Archivkopf weiter nicht genau

[`archiv/runde-8.md` Z. 4–12](https://github.com/kaaasi/eishockey-manager-redone/blob/7638871bff5dc66953db782de3c2903dc3093596/docs/archiv/runde-8.md#L4-L12): Zusätzlich zu Überschrift und „Zurückgestellt“
ist der Absatz **„Ergebnis:“** (Z. 6–12) neu. Den hatte das Vorreview übersehen.
Gegenbeispiel: `diff <(git show b3a38df:docs/arbeitspakete.md | sed -n 12,336p) <(tail -n +6 docs/archiv/runde-8.md)` →
`0a1,7`, dazu eine Leerzeile. Der übrige Rumpf ist zeilengleich. Vorschlag: „… und mit ergänztem Ergebnisabsatz“.

## 3. Ausgeführte Prüfungen

- `git diff --stat f60797a..7638871` (4 Dateien, +71/−20) und das ganze Delta gelesen.
- `git ls-files src` gegen die neuen Zuständigkeitsmuster geprüft. Code der Ausnahme gelesen: `Season.cs` Z. 44–74,
  `SeasonFlow.cs` Z. 88–207 und 390–402, `Cups.GameState.cs` Z. 20–45, `CupsNationalMode.cs` Z. 53, `GameSetup.cs`
  Z. 97–136.
- `grep` nach `DaysOf(DayEvents.`, `0x4F986`, „Vor dem ersten Spieltag“ und R8-A03/D04.
- Archiv gegen `b3a38df` per `diff` verglichen.
- `gh api`: Labels (`spur-wartet` vorhanden) und PR #263 (gemergt 01.11., Body nennt A03).
- Kein Build und keine Tests (Doku-PR). Damit kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4.
- Akzeptierte `[APPROX]`: keine. Annahme: Mit „kleine Stellen“ in der Ausnahme sind Einfügungen ohne Signaturänderung
  gemeint.
