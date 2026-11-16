# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 21 – Umsetzung Restliste: Lader, Zufallsstrom, HOK-Werte, Finanzübersicht, Zusehen, RE vor DOSBox (Plan 16.11.2026, Go 16.11.2026)

Runde 20 abgeschlossen am 16.11.2026: [archiv/runde-20.md](archiv/runde-20.md). Grundlage: Restliste
[analyse/restliste-v1.md](analyse/restliste-v1.md) (V 85, D 45; Entscheidungen N1–N6, „Vorschlag Zuschnitt Runde 21 ff.“), #882,
DOSBox-Aufnahme `a093` (#888, Referenz-PR kaaasi/eishockey-manager-referenz#40 gemergt). **Ziel der Runde:** die V-Themen mit
gemeinsamer Ursache im Spielkern (Lader beim Programmstart, gemeinsamer Zufallsstrom, Teamwert) und die kleinen, unabhängigen
V-Themen (HOK-Werte, Finanzübersicht, Zusehen) umsetzen; die 22 D „RE zuerst“ und A093 als V/X/K einordnen. Übrige V-Themen
(`bundestrainer-satz`, `kaderplatz-werte`, `kader-regeln`, `transfer-pool`, `wettbewerbe-regeln`, `einzelpunkte`) und neue V aus
`re-vor-dosbox` → Runde 22; `marker-x` und Abschlussprüfung danach (Vorschlag). **Entscheidungen Nutzer 16.11.2026** (Optionen
im Plan-PR #889): R21-D1 = A, R21-D2 = A, R21-D3 = A, R21-D4 = A; Modell `re-vor-dosbox` Claude Fable 5.2, übrige wie unten. **Go Nutzer 16.11.2026** (alle Spuren samt Folgeläufen). Plan-Review #890 Ja (P3-1…4 eingearbeitet).

**Base:** aktueller `main` beim Start der Spur (Welle 2 nach Merge von `start-lader`, Welle 3 nach Merge von `zufallsstrom`);
tatsächlichen Base-SHA in den Draft-PR. Regeln wie Runde 20 (Archiv): ein Branch `paket/<spur>`, ein PR, Teile als Commits,
höchstens drei Fortsetzungen; Befund reproduzieren, roter Regressionstest mit Fundstelle `Datei 0x…`, dann grün; Mehrsaisonprobe
(4 Wechsel, passiv/aktiv) bei Saisonwechsel, Kader oder Geld; `[ORIGINAL]`/`[APPROX]`; fremde Tests nach R15-E2 = C bzw. R21-D2;
keine geplanten Aufgaben aus Läufen (R15-E4). Bei Unklarheit „wie im Original“, sonst `[APPROX]` mit Begründung, die Leitstelle
bündelt die Fragen. N2 = B (korrigierte Originalfehler bleiben) und N6 = A (Zufall der Darstellung: A061, A271, A274, A438, A452
unverändert) gelten. **Entwickler und Erstreviews sind Claude-Sitzungen;** ChatGPT nur fürs Gesamtreview.

**Folge (R21-D1 = A):** Welle 1 parallel `re-vor-dosbox`, `hok-import-werte`, `finanzuebersicht`, `zusehen`, `start-lader`
(Integration in dieser Folge). Welle 2 nach Merge von `start-lader` parallel `teamwert-aufstellung`, `zufallsstrom`
(Integration in dieser Folge; `zufallsstrom` rebased und zieht die erwarteten Werte nach). Welle 3 `pruefung-karte` nach Merge
von `zufallsstrom`. Danach Gesamtreview (R21-D4).

### Zuständigkeit für gemeinsame Dateien

- **Markerliste (R21-D3 = A):** `docs/analyse/restliste-v1.md` (generierter Block, Zahlen) und der Generatorlauf
  `markerliste.py --write` allein `pruefung-karte` (Welle 3). Alle anderen Spuren führen **kein** `--write` aus, sondern nennen im
  PR jede geänderte, neue oder entfallene Markerzeile (Datei, alte ID, neue Klasse nach eigener Sicht). In
  `docs/analyse/restliste-v1-klassen.csv` ändert `re-vor-dosbox` nur die Zeilen ihrer 23 IDs (Welle 1), den Rest `pruefung-karte`.
- **Erwartete Werte (R21-D2 = A):** Verschiebt eine Spur den Zufallsverbrauch oder Teamwerte (`start-lader`,
  `teamwert-aufstellung`, `zufallsstrom`), passt sie Fingerabdrücke, Saaten und erwartete Listen fremder Tests an – Begründung
  und Rückschaltprobe **je Testdatei** (Ursache mit Fundstelle, alter Code + neue Werte → rot). Fachliche Sollwerte aus
  Originalständen (Abgleich „exakt“ nach R15-D2, DOSBox-Belege) werden nie bloß neu eingetragen: weicht einer ab, ist das ein
  Befund (roter Test bleibt, `spur-wartet`).
- `Core/MatchEngine.TeamValues.cs`: `start-lader` (Welle 1), danach `zufallsstrom` nur A107. `Core/MatchEngine.Live.cs`: `zusehen`
  nur A541, `start-lader` nur A080, `teamwert-aufstellung` nur A544. `Core/Transfer.ComputerClub.cs`: `start-lader` nur A704,
  `finanzuebersicht` nur A703. `Core/FinanceMonthly.cs`: allein `start-lader`. `Core/FinanceStadium.cs`, `Core/FinanceTalent.cs`: `finanzuebersicht` nur die Buchungsstellen
  (A511, A513), `zufallsstrom` nur die Zufallsstellen (A510, A388). `Core/GameState.AddManager.cs`: `start-lader` nur A519,
  `zufallsstrom` nur A464. Spielstandfelder neu: Migration je Spur in eigener Datei `Core/SaveGame.Migration.<Spur>.cs`.

### Spur `re-vor-dosbox` – D „RE zuerst“ und A093 einordnen (Welle 1)

- **Teile:**
  1. **em.exe lesen** für die 22 D mit „RE zuerst“ (A002, A038, A059, A080, A101, A206, A421, A425, A455, A480, A492, A494, A497,
     A500, A553, A628, A670, A671, A672, A694, A698, A801): je Eintrag Fundstelle, Lesart, Remake-Stelle; neue Klasse **V** (mit
     Aufwand S/M/L und Beleg nach `zusammenarbeit.md` §5 Punkt 7), **X**, **K** oder bleibt **D** (dann konkreter Aufnahmeauftrag).
  2. **A093 aus `a093`** (FILE71–FILE74, #888; Notizen dort): Technikabzug außerhalb der eigenen Reihe (Kaderplatz +0x02,
     Datei 0x98F8) gegen die Stände prüfen; Einordnung wie Teil 1 (bei V mit Testvorschlag). Fixes #888.
  3. **Ergebnis:** `docs/analyse/re-vor-dosbox.md` (neu, höchstens 150 Zeilen: Tabelle ID, Klasse alt→neu, Fundstelle, Beleg),
     Klassen in `restliste-v1-klassen.csv` (nur diese 23 Zeilen), Handtext der DOSBox-Sammelliste in `restliste-v1.md` (übrige D),
     Liste der neuen V als Zuschnittsvorschlag für Runde 22.
- **Abgrenzung:** keine Änderung unter `src/`, `tests/`; kein `markerliste.py --write`.
- **Modell:** Claude Fable 5.2 (Nutzer 16.11.2026). **Erstreview:** Claude Opus 5.5 (Stichprobe je neuer Klasse gegen
  em.exe). **Status:** fertig – Review (PR #891, 16.11.2026): Teile 1–3 erledigt, [analyse/re-vor-dosbox.md](analyse/re-vor-dosbox.md);
  V 15, X 5, K 1, E-Vorschlag 2 (A059, A480), kein D mehr.

### Spur `hok-import-werte` – im HOK geführte Werte übernehmen (Welle 1, N4 = C)

- **Teile:** A607 (`Core/OriginalImport.Market.cs`), A612 (`Core/OriginalImport.Playoffs.cs`, Marken der Play-offs), A624
  (`Core/OriginalImport.cs`): je Wert Feld im HOK (Feldkarte), roter Test mit Original-Spielstand (`original/spielstaende/`), dann
  Übernahme; Ablauf-Feinheiten bleiben E (N4).
- **Dateien:** `Core/OriginalImport*.cs`, zugehörige Tests. **Tests:** `--schnell` mit Originaldaten; Abgleich gegen die
  Original-Stände, deren Werte betroffen sind.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** offen.

### Spur `finanzuebersicht` – Prüfauftrag Finanzübersicht (Welle 1, N5 = C)

- **Teile:**
  1. **Prüfen:** Kategorien und Summen der Original-Finanzübersicht in em.exe lesen; für A192, A207, A511, A513, A703, A716
     feststellen, ob das Original den Betrag in der Übersicht zeigt (Fundstelle), Remake-Kategorie dagegen.
  2. **Umsetzen:** wo die Übersicht abweicht, zeigt sie diese Posten nicht (nur das Finanzbuch, Wunsch 13.11.2026); Buchung über
     `GameState.Book` bleibt (N5); roter Test je Posten (Übersicht vor/nach Buchung). A703: ggf. neutrale Kategorie.
- **Dateien:** `UI/Screens/Manager/FinanceScreen.cs`, Kategorien der Übersicht (`Core/Finance.cs`;
  nicht `Core/FinanceMonthly.cs`, das gehört `start-lader`), die sechs Buchungsstellen nur Markerzeile/Kategorie. **Tests:** `--schnell`, Mehrsaisonprobe nur, wenn sich Beträge ändern.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** offen.

### Spur `zusehen` – Verein zum Zusehen markieren (Welle 1)

- **Teile:**
  1. **Schalter 0x0AE2B** (F018, A445): Verein zum Zusehen markieren, Knöpfe im Gegner- und Vereinsfenster (A286
     `TeamInfoWindow.cs`, A752 `ClubInfoWindow.cs`), Liste DGROUP 0x233, Wirkung auf die Anzeige (`MatchEngine.Scoreboard.cs`)
     und Spielplan des Gegners; Speicherung im Spielstand (Migration).
  2. **Teil-V:** A295 (Option 12, `GameOptionsScreen.cs`), A541 (WM-Schalter Datei 0x5541F/0x5575A, Sicherung 0x22C,
     `MatchEngine.Live.cs` nur diese Stelle).
- **Dateien:** wie oben, neu `Core/GameState.Watch.cs`, `Core/SaveGame.Migration.Zusehen.cs`; Bildschirmfoto-Ablauf in
  `src/EishockeyManager.Tools/Scenarios/`. **Tests:** `--schnell`, Pixelvergleich der Knöpfe gegen Videobild, Laden/Speichern.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** offen.

### Spur `start-lader` – Lader 0x2EF1B beim Programmstart (Welle 1)

- **Teile:**
  1. **Zeitpunkt:** Teamwerte wie im Original beim Programmstart bzw. Spielstart ziehen (Datei 0x439BB call 0xE0:0x75,
     `spielberechnung.md` §12) statt `MatchEngine.EnsureTeamValues` beim ersten Spiel; Verhalten beim Laden eines Spielstands aus
     em.exe lesen (läuft der Lader erneut?). Behebt A106, A108, A418 (Europapokal-Auslosung), A485 (Länderwerte), A505
     (Grundeinnahme), A704 (Käufer). Roter Test je Folge vor dem ersten Spiel.
  2. **Neuer Managerverein** (A519, `GameState.AddManager.cs`) und Folge **A080** prüfen (Teamwerte der Managervereine; ggf. mit
     `re-vor-dosbox` abstimmen, Ergebnis im PR).
  3. **Abschluss:** Mehrsaisonprobe, erwartete Werte nach R21-D2, Markerzeilen im PR.
- **Dateien:** `Core/MatchEngine.TeamValues.cs`, `Core/CupsEurope.cs`, `Core/CupsNational.GameState.cs` (nur A485),
  `Core/FinanceMonthly.cs`, `Core/Transfer.ComputerClub.cs` (nur A704), `Core/GameState.AddManager.cs` (nur A519),
  `Core/MatchEngine.Live.cs` (nur A080), Aufrufstelle in
  `Core/NewGame.cs`/`Core/GameSetup.cs` minimal, ggf. Migration.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Teile 1–3 fertig, wartet auf Erstreview (#895).

### Spur `teamwert-aufstellung` – Teamwert der Managervereine aus den Spielern (Welle 2)

- **Teile:** A174 (`Core/Squad.Strength.cs`), A544 (`Core/MatchEngine.Live.cs` nur diese Stelle): Formel Datei 0x368B1/0x977F
  lesen, Teamwert aus der Aufstellung wie im Original, auch für die Automatik des Gegners; `UpdateManagerTeamValues`
  (`GameState.DailyRun.cs`) minimal. Abgleich-Paare und Mehrsaisonprobe; erwartete Werte nach R21-D2.
- **Voraussetzung:** `start-lader` gemergt. **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** offen.

### Spur `zufallsstrom` – gemeinsamer Zufallsstrom `GameState.Random` (Welle 2, N3 = B)

- **Teile:** je Stelle Zugzeitpunkt und -reihenfolge wie im Original (Fundstelle), eigene Quelle entfernen, roter Test (Folge
  der Züge bzw. Zustand nach fester Saat).
  1. **Transfer und Pool:** A212, A708 (`Transfer.Pool.cs`), A248, A726 (`Transfer.Unsolicited.cs`), A720, A721
     (`Transfer.Squads.cs`), A722 (`Transfer.StartSalary.cs`), A725 (`Transfer.Trend.cs`).
  2. **Finanzen und Kader:** A388 (`FinanceTalent.cs`), A510 (`FinanceStadium.cs`), A675, A676 (`Squad.HiddenSkills.cs`), A107
     (`MatchEngine.TeamValues.cs`).
  3. **Tag und Start:** A464 (`GameState.AddManager.cs`), A521, A524 (`GameState.DailyRun.cs`), A533 (`GameState.MainMenu.cs`,
     nur im Ereignis `OnMainMenu`), A558 (`NewGame.cs`); Probe: Öffnen aller Bildschirme zieht nichts aus dem Strom (`AGENTS.md`
     §4, N6). Mehrsaisonprobe; erwartete Werte nach R21-D2 (nach Rebase auf `teamwert-aufstellung`).
- **Voraussetzung:** `start-lader` gemergt; Integration nach `teamwert-aufstellung`. **Modell:** Claude Opus 5.5. **Erstreview:**
  Claude Opus 5.5. **Status:** offen.

### Spur `pruefung-karte` – P3 und Markerliste (Welle 3, R21-D3 = A)

- **Teile:**
  1. **#882 (P3-1…5):** Bereich Drift-Wächter ZIMMERMANN (≈ 90–110), Rundlauf Export → Import mit Reihen 0x2E/0x30,
     Fundstelle 0x38FAA und `[APPROX]` Bit 3, Zeilenfarbe der Bestenliste am Spielende (Farbe 10, DGROUP 0x2C4) samt Test-Doku,
     Angaben Pool-Probe/Saaten.
  2. **Markerliste:** einziger Lauf `markerliste.py --write`; alle in Runde 21 neuen/geänderten Marker nach den PR-Angaben der
     Spuren einordnen (CSV), Generator ohne „unklassifiziert“; Zusammenfassung und Klasse-V-Themen in `restliste-v1.md` nachziehen.
  3. **Funktionskarte** neu (`funktionskarte.py --write`).
  P3 aus Erstreviews der Runde (Label `P3`) nimmt die Spur mit.
- **Voraussetzung:** `zufallsstrom` gemergt. **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** offen.

### Gesamtreview Runde 21 (R21-D4 = A)

ChatGPT (Codex, GPT-6.1 Terra) nach Merge von `pruefung-karte`, risikoorientiert (`anhang.md` C), besonders: Zufallsfolge und
Teamwerte bei Spielstart und Laden, Mehrsaisonverlauf, angepasste erwartete Werte (R21-D2), Finanzübersicht, Laden/Speichern
der neuen Felder (Zusehen).

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- A093 (Technik −10 außerhalb der eigenen Reihe): Aufnahme `a093` liegt vor (#888), Auswertung in `re-vor-dosbox` Teil 2.
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen (nach Runde 20)

- **#882:** P3 aus Erstreview #881 (gesammelt, nächste `pruefung-karte`).
- **Testlücke Gesamtreview #848:** Speichern/Laden mitten in einer angehaltenen Live-Paarung (über die Oberfläche nicht
  erreichbar; prüfen, sobald ein Speicherweg im Live-Spiel dazukommt).
- **Vor v1.0 (R20-D3-Zusatz, Nutzer 15.11.2026):** Liste aller noch abweichenden Bereiche und Funktionen an den Nutzer
  (`python3 tools/reverse-engineering/markerliste.py --abweichungen`), der entscheidet, was noch geändert wird.
- **Mit v1.0 und dem bitgenauen Zufallsmodus (N6 = A, Nutzer 16.11.2026):** eigene Zufallsquellen der Darstellung (A061, A271,
  A274, A438, A452) erneut prüfen.
- **Nach v1.0:** Einstellungen aus R15-E3, bitgenauer Zufallsmodus (`anforderungen/zusatzfunktionen.md` C), Sprachen und
  Währungen (N1 = B, `anforderungen/zusatzfunktionen.md` A 2).

## Paket `hok-format` – Original-Spielstände `*.HOK`

Branch: `paket/hok-format` · Status: Analyse gemergt (#176, Review #178): Verschlüsselung, Prüfsummen, Kopf und Blockfolge
belegt, `emtool hok dump`, Doku `docs/formate/spielstand-original.md` (offene Liste dort). Entscheidung Nutzer:
Sicher-/Ladesperre des Originals nicht übernehmen. Weiter in Runde 7 mit `hok-patch` (Spielstände fürs Original);
Import/Export ins Remake danach.

- Nachtrag (#197, Branch `paket/hok-format-2`): Kaderplätze, Spielerpool, Training und Taktik an FILE05 belegt;
  Kaderausgabe in `emtool hok dump`. Spielstände FILE01–05 und `KAPUTT.HOK` im Referenz-Repo `original/spielstaende/`.

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: PR 1–7 gemergt (Runde 3); Restarbeiten in Runde 4 gemergt; PR 8 in Runde 5 gemergt (`paarungen-liga`, #128). Regeln und Belege: [wettbewerbe.md](wettbewerbe.md)

| PR | Inhalt | Stand |
|---|---|---|
| 1 | DEB-Pokal: Auslosung, Heimrecht, „best of three“, Abrechnung, Bildschirm „DEB-Pokal Auslosung“ | gemergt (Runde 3, #54) |
| 2 | Europapokal (Gruppe + Endrunde), Bildschirm Gruppen/Endrunde | gemergt (Runde 3, #54) |
| 3 | WM (2 Gruppen + K.-o.), Bildschirm Gruppe/Tabelle WM | gemergt (Runde 3, #54) |
| 4 | Stärken (Ligen, Länderteams) | gemergt (Runde 3, #54) |
| 5 | Statistiken: Ewige Tabelle (Totale), Früher | gemergt (Runde 3, #54) |
| 6 | Bundestrainer: WM-Kader, „WM Vorbereitung“ (Freundschaftsspiele), Etat; WM: Werte Deutschlands neu ausgewürfelt, Länder 20-mal getauscht | gemergt (Runde 3, #54) |
| 7 | Spielmodus „nur Weltkugel“: Spielvorbereitung, Vereinssaison überspringen, Menü des Bundestrainers | gemergt (#87 über #89) |
| 8 | Paarungen Liga | gemergt (Runde 5, #128); Pixelvergleich Runde 6 (`pixel-wettbewerbe`) |
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | Runde 8 (`statistiken`, `nationalteam`) |

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: Format, Dialog und automatisches Speichern gemergt (Runde 3, [archiv/runde-3.md](archiv/runde-3.md)); #80 in Runde 4 behoben (#105)

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`) und Export per Vorlage (`hok-format`) – wartet auf Original-Spielstände des Nutzers.

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen auf den Ereignissen aus `spielberechnung` (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
