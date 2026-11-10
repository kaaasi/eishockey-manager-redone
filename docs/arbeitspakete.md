# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 15 – Abgleich mit dem Original, Export `*.HOK`, Restpunkte (Plan 10.11.2026, Go 10.11.2026)

Runde 14 abgeschlossen am 10.11.2026: [archiv/runde-14.md](archiv/runde-14.md). Grundlage: Vormerkungen des Nutzers vom
10.11.2026 (Effizienz), R14-D2 = B (Export `*.HOK` in Runde 15), offene Issues #620 (Rest), #698, #699, #703, #712, #716,
Restlücke A134. #639 (Ablage Aufnahme `runde-13`) ist mit Runde 14 ausgewertet und geschlossen.

**Base für alle Spuren:** aktueller `main` (Planstand `892ca85`); tatsächlichen Base-SHA in den Draft-PR. Regeln wie Runde 14
(Archiv): Befund reproduzieren, roter Regressionstest mit Fundstelle, dann grün; Mehrsaisonprobe (4 Wechsel, passiv und aktiv)
bei Saisonwechsel, Kader oder Geld; Schema-Änderung mit Migration in neuer Datei `Core/SaveGame.Migration.<Spur>[<n>].cs`
(Stand Schema 47, die Leitstelle nummeriert beim Merge); `[ORIGINAL]`/`[APPROX]` mit `Datei 0x…`; `Fixes #<nr>`; höchstens drei
Fortsetzungen je Spur. Bei Unklarheit „wie im Original“; bleibt es unklar, Teil blockiert lassen, die Leitstelle bündelt die
Fragen für den Nutzer. **Entwickler und Erstreviews sind Claude-Sitzungen** (geplante Läufe); ChatGPT nur für das Gesamtreview.

**Spurmodell ab Runde 15 (Vormerkung Vorschlag 3, umgesetzt mit diesem Plan):** je Spur **ein** Branch `paket/<spur>` mit **einem**
PR; Teile sind Commits, Fortsetzungen arbeiten nach dem Übergabekommentar auf demselben Branch weiter
(`vorlagen/spur-start.md`, „Teile und Fortsetzung“). Der Spur-PR wird einmal reviewt (Erstreview Claude Opus 5.5; Doku-Spuren:
Claude Sonnet 5.5 nach „Modellwahl“) und in der Integrationsfolge gemergt, ohne Rebase, wenn konfliktfrei
(`vorlagen/leitstelle-start.md` Schritt 2); kein eigener Integrations-PR je Spur. **Folge:** Welle 0 `prozess-kern` (R15-D3 = A; die übrigen
Spuren starten nach dessen Merge) → Welle 1 parallel, Merge in der Folge `tagesablauf` → `kader` → `hok-export` →
`abgleich-original` (steht die DOSBox-Probe von `hok-export` noch aus, darf `abgleich-original` vorher gemergt werden); danach
Welle 2 `pruefung-karte`, am Rundenende Gesamtreview.

### Zuständigkeit für gemeinsame Dateien

- `AGENTS.md`, `CLAUDE.md`, `docs/prozess/*`, `docs/vorlagen/*`, `docs/handoffs/VORLAGE.md`: allein `prozess-kern`.
- `Core/GameState.DailyRun.cs`, `Core/GameState.HotSeat.cs`, `Core/SeasonFlow*.cs`, `Core/DayCounter.cs`; für Tag 0 (#699)
  zusätzlich `Core/GameState.Manager.cs` (nur `AdvanceFinance`), `Core/Finance.cs`, `Core/FinanceBank.cs`,
  `Core/FinanceInbox.cs`, `Core/FinanceSponsorRating.cs`: allein `tagesablauf`.
- `Core/NewGame.cs`, `Core/GameSetup.cs`, `Core/Transfer*.cs`, `Core/Player*.cs`, `Core/Squad*.cs`, `Core/FinanceTalent.cs`,
  `Core/FinanceYouth.cs`: allein `kader`. Braucht eine Spur eine weitere `Core/Finance*.cs`, nennt sie sie im PR; die Leitstelle
  ordnet zu.
- `src/EishockeyManager.Formats/OriginalSave*.cs`, neue Dateien `Core/OriginalExport*.cs`, `Tools/HokExport.cs`,
  `docs/formate/spielstand-original.md`: allein `hok-export`. `Core/OriginalImport*.cs`: `hok-export` (nur was die Rundreise
  verlangt, im PR genannt); `abgleich-original` liest nur und meldet Importlücken als Issue.
- Neue Dateien `Tools/HokCompare*.cs`, `docs/analyse/abgleich-original.md`, Tests `Abgleich*`: allein `abgleich-original`.
- `src/EishockeyManager.Tools/Program.cs`: je Spur eine Registrierungszeile (`hok-export`, `abgleich-original`).
- `Core/GameState.Integration.cs`: höchstens minimal, im PR begründet.
- `docs/analyse/inventur.md`, `docs/re-abdeckung.md`, `tools/reverse-engineering/*`, `tests/**/Support/`, fremde Testdateien:
  allein `pruefung-karte` (Welle 2); die anderen Spuren nennen ihre Inventurzeilen im PR.

### Spur `prozess-kern` – Prozessdoku verdichten (Vormerkung Vorschlag 5, Welle 0)

- **Inhalt:** `AGENTS.md`, `docs/prozess/zusammenarbeit.md`, `docs/vorlagen/*` (heute rund 1.200 Zeilen Pflichtlektüre je Lauf)
  auf einen Kern von etwa 300 Zeilen; Sonderfälle in `docs/prozess/anhang.md` (nur bei Bedarf gelesen). Regeln inhaltlich
  unverändert. Pfade und Namen der Kurzaufrufe bleiben (`spur-start.md`, `review-anfrage.md`, `leitstelle-start.md`), damit
  laufende Spuren nicht brechen. Dazu #712 (R-06, N-01, N-02, P3-1…3).
- **Abnahme:** Gegenliste im PR: jede Regel der alten Fassung → Fundstelle in Kern oder Anhang (keine fehlt, keine neu – außer
  den Klärungen aus #712, je mit Verweis); Zeilenzählung vorher/nachher. Dazu R719-02 aus `reviews/2026-11-10-plan-runde-15.md`
  (Reste des Stapelmodells), soweit nicht schon erledigt.
- **Modell:** Claude Sonnet 5.5 (mechanisch). **Review:** Doku-Prüfung, Claude Sonnet 5.5. **Status:** fertig, Review offen (PR #722).

### Spur `tagesablauf` – Restlücken Tagestakt (#699 Punkte 1–3)

- **Teile:**
  1. **Tag 0:** erster Tageswechsel vor dem ersten Menü mit Tagesabschluss je Manager (Datei 0x565A1h, 0x54D41h → 0x55073h →
     0x55204h, 0x3ED01h/0x3F833h); Zinskonto/Werbebewertung an Tag 0; Listenwechsel-Wurf nach dem ersten Menü. Abnahme gegen
     FILE01 (Zähler 1) und FILE02→FILE03 (Konto 737.354 DM), Mehrsaisonprobe.
  2. **Hot-Seat-Würfe:** Wurf von Manager i nach dem Menü von Manager i − 1 (0x54E3Fh–0x54EF4h); Test mit zwei Managern.
  3. **Bundestrainer am Ereignistag:** Neuaufstellung Satz 5 und 952Ch(5, 0) vor den Menüs (0x54DAAh); nur Testweg.
- **Modell:** Claude Opus 5.5. **Status:** Teile 1–3 fertig, Review offen (PR #725; R15-E1 = A: 29 fremde Tests angepasst, Rückschaltprobe im PR).

### Spur `kader` – Kader beim Spielstart und Saisonwechsel (#698, A134, #699 Punkt 4)

- **Teile:**
  1. **#698:** passiver Manager nach FILE44-Import ohne Spieler nach 4 Saisonwechseln reproduzieren; Fundstelle der
     Kaderergänzung bzw. des Abgangs im Original (Jugend, Pool, Zwangsergänzung); wie im Original beheben oder belegt schließen.
  2. **A134:** Startwerte Tendenz und Steuerbyte je Kaderplatz beim Spielstart (Aufrufer von 4907Ah im Spielstart) übernehmen;
     Gegenprobe FILE01/FILE29 (Dump), Mehrsaisonprobe.
  3. **R204 (#699 Punkt 4, R15-D4 = A):** wie im Original – Vertragslose aus Managervereinen werden freie Poolspieler
     (Besitzer 6) und kommen nur über den Listenwechsel zurück; keine Einstellung.
- **Modell:** Claude Opus 5.5. **Status:** PR #726; Teil 1 (Jugend am Saisonende 0x26F20, #698) und Teil 2
  (Tendenz der Startkader 0x4907A, A134) und Teil 3 (R204: Vertragslose nicht auf der Liste, Datei 0x27DE3–0x27E9D; FILE46
  KJELLBERG Pool +0x1A = 0, also nicht „Besitzer 6“) fertig; Erstreview #741 (ja, P3-1…4). Fixlauf: Rebase auf #725, P3-1…4,
  R15-E3 = C (Vertragslose aus Managervereinen ganz verloren); Nachprüfung ausstehend.

### Spur `hok-export` – Remake-Spielstand als `*.HOK` (R14-D2 = B)

- **Teile:**
  1. **Export Kern:** `Core/OriginalExport*.cs` und Schreiber in `Formats` nach der Feldkarte (`formate/spielstand-original.md`):
     Klartext, Verschlüsselung, Prüfsummen, Spielstand-ID. Nicht abbildbare Felder nach Feldkarte, sonst `[APPROX]`.
  2. **Rundreise:** FILE01–49A importieren → exportieren → Klartext vergleichen; erlaubt sind nur Abweichungen in Feldern, die
     die Feldkarte als „neu berechnet“ führt (Liste im Test). Zweite Rundreise Remake → HOK → Remake ohne Abweichung.
  3. **Zugang (R15-D5 = D):** nur `emtool hok export <spielstand> <ziel.hok>`, keine Option im Speichern-Dialog; `hok dump`
     liest das Ergebnis.
- **Abnahme zusätzlich (R15-D5 = D, Pflicht):** Die Spur legt im PR mindestens zwei exportierte Stände fest (Spielstart aus
  einem neuen Remake-Spiel, Saisonmitte nach Remake-Spieltagen); die Leitstelle legt sie im Referenz-Repo ab
  (`original/spielstaende/`, Branch `daten/…`), der Nutzer lädt sie im Original (DOSBox) und hält Hauptmenü, Kader und Finanzen
  im Bild fest. Merge erst nach dieser Probe; Abweichungen werden Befunde des Spur-PR.
- **Modell:** Claude Opus 5.5. **Status:** offen.

### Spur `abgleich-original` – Remake gegen Spielstandpaare des Originals (Vormerkung Vorschlag 1)

- **Ziel:** Befunde entstehen im Test, nicht im Review. Das HOK hat keinen Zufallszustand (`formate/spielstand-original.md`,
  „Zufallszustand“) – Vergleichsart nach R15-D2 = A (deterministische Felder exakt, zufallsabhängige über Invarianten und
  Bereiche).
- **Teile:**
  1. **Werkzeug:** `emtool abgleich <a.hok> <b.hok>`: Stand a importieren, ohne Eingriff bis Datum/Zähler von b spielen
     (`Continue()`), je Feld mit dem Import von b vergleichen; Ausgabe als Tabelle (Feld, Original, Remake, Klasse). Feldklassen
     in einer Datei: deterministisch (exakt), zufallsabhängig (Invariante/Bereich), durch Nutzereingriff geändert (aus der README).
  2. **Paare:** ohne Eingriff FILE02→03, FILE13→14, FILE16A→17, FILE20→21, FILE22→23, FILE24→25, FILE30→31, FILE38→39→40;
     mit bekanntem Eingriff (nur übrige Felder) FILE44→46, FILE18→48. Je Paar ein `[GameDataTheory]`-Fall; bekannte Abweichungen
     als erwartete Liste mit Issue-Nummer, jede neue Abweichung macht den Test rot.
  3. **Bericht:** `docs/analyse/abgleich-original.md` (je Abweichung: Einordnung Remake-Fehler mit Fundstelle / Importlücke /
     Zufall / Eingriff); je Remake-Fehler ein Issue (Label `befund`, `P0`–`P3`). Behoben wird in Welle 2 bzw. Runde 16
     (Leitstelle bündelt), nicht in dieser Spur.
- **Hinweis:** wird als letzte Spur gemergt; vor dem Merge erwartete Listen gegen den dann aktuellen `main` erneuern.
- **Modell:** Claude Opus 5.5. **Status:** offen.

### Spur `pruefung-karte` – Welle 2 (nach dem Merge aller Spuren)

- **Inhalt:** #620 (Rest), #703, #716 (Testlücken), P3 aus den Reviews dieser Runde, Abgleich-Befunde P3 (nach Zuweisung der
  Leitstelle); Inventur und Funktionskarte nachziehen (`--write`, zweiter Lauf ohne Diff).
- **Modell:** Claude Opus 5.5 (Testlücken in riskanten Übergängen; Sonnet nur, wenn der Nutzer es wählt). **Status:** offen.

### Gesamtreview am Rundenende

ChatGPT (Codex mit Netzzugang) nach `vorlagen/review-anfrage.md`, risikoorientiert (Nutzer 10.11.2026). **Modell:** GPT-6.1 Terra.

### Entscheidungen zum Plan (Nutzer, Go 10.11.2026, PR #719)

**Ergebnis:** R15-D1 = A, R15-D2 = A, R15-D3 = A, R15-D4 = A, R15-D5 = D; Modelle wie in den Spurabschnitten. Vorgemerkt nach
v1.0: bitgenauer Modus (eine Borland-`rand`-Quelle mit belegter Aufrufreihenfolge, Voraussetzung für einen exakten Abgleich;
Rückfrage Nutzer zu R15-D2, 10.11.2026).

**R15-D1 – Zuschnitt.** Problem: fünf Vormerkungen/Pflichten (Abgleich, Export, Doku, Restlücken, Issues) konkurrieren um
Läufe; der Abgleich kann neue Arbeit erzeugen.
- **A** Sechs Spuren wie oben, fünf parallel (nach R15-D3 = A: `prozess-kern` als Welle 0, vier parallel), `pruefung-karte` als Welle 2. + alle Vormerkungen in einer Runde; − Abgleich-Befunde
  werden erst in Welle 2/Runde 16 behoben.
- **B** Wie A ohne `hok-export` (Runde 16). + weniger Läufe; − bricht R14-D2 = B.
- **C** Erst `abgleich-original` allein, danach Fachspuren auf Basis seiner Befunde. + Fixes zielgerichtet; − Runde deutlich länger,
  Fachspuren warten.
- **D** Nur `abgleich-original`, `prozess-kern`, `pruefung-karte`. + kleinste Runde; − #698/#699/Export bleiben liegen.
- **Empfehlung A:** Die Spuren berühren getrennte Dateien; der Abgleich findet ohnehin erst im Test, Fixes lassen sich bündeln.

**R15-D2 – Vergleichsart im Abgleich.** Problem: Das HOK speichert keinen Zufallszustand; Ergebnisse, Verletzungen, Markt
weichen deshalb immer ab, ein exakter Vergleich aller Felder ist unmöglich.
- **A** Feldklassen: deterministische Felder exakt (Datum, Zähler, Spielplan, Verträge, Alter, Bau, Zinsen, feste Buchungen),
  zufallsabhängige über Invarianten und Bereiche (Summen, Kaderzahl, Tabellenkonsistenz). + deckt viel ab; − Bereiche können
  Fehler verdecken.
- **B** Nur deterministische Felder. + keine Fehlalarme; − weniger Abdeckung.
- **C** Zusätzlich Teil 0: Zufallszustand des Originals rekonstruieren (Saat suchen, Borland `rand`), dann alles exakt.
  + vollständig; − unsicher, ob die Saat findbar ist, hoher Aufwand.
- **D** A plus neue DOSBox-Paare mit wenig Zufall (z. B. Tage ohne Spiel). + bessere Paare; − Arbeit für den Nutzer.
- **Empfehlung A:** größter Nutzen ohne Abhängigkeit vom Nutzer; C als Kandidat für später, falls A zu viele „Zufall“-Fälle zeigt.

**R15-D3 – Zeitpunkt `prozess-kern`.** Problem: Verdichten spart Tokens in jedem Lauf, ändert aber die Doku, die laufende Spuren lesen.
- **A** Zuerst (Welle 0), Fachspuren starten danach. + alle Läufe der Runde profitieren; − Start um Lauf + Review verzögert.
- **B** Parallel, Pfade und Kurzaufrufe bleiben stabil, Merge als erstes. + kein Verzug; − Läufe vor dem Merge lesen noch die lange Fassung.
- **C** Am Rundenende. + keine Störung; − Ersparnis erst in Runde 16.
- **D** Leitstelle verdichtet selbst im Plan-PR. + sofort; − Leitstelle setzt eigene Doku um, Plan-PR wird groß.
- **Empfehlung B.**

**R15-D4 – R204 (#699 Punkt 4).** Problem: Vertragslose aus Managervereinen kommen im Remake beim Saisonwechsel zusätzlich auf
freie Listenplätze; im Original werden sie freie Poolspieler und kommen nur über den Listenwechsel zurück.
- **A** Wie im Original. + Regel „wie im Original“; − Markt in der ersten Woche kleiner.
- **B** Remake-Verhalten behalten, als Abweichung kennzeichnen. + kein Aufwand; − Abweichung bleibt.
- **C** Wie im Original, Remake-Verhalten nach v1.0 als Einstellung vormerken. + wie A, Wahl bleibt; − Eintrag in der Liste nach v1.0.
- **Empfehlung C** (Muster wie #349 und R13-D2).

**R15-D5 – Zugang zum Export.** Problem: Das Original kennt keinen Export; ein Knopf im Speichern-Dialog wäre eine Erweiterung.
- **A** Nur `emtool hok export` und Rundreisetests. + kein Eingriff in die Original-Oberfläche; − nur über die Kommandozeile.
- **B** Zusätzlich Option im Speichern-Dialog. + bequem; − weicht vom Original-Dialog ab (vor v1.0).
- **C** A jetzt, Dialog-Option nach v1.0 als Einstellung. + wie A, Wahl bleibt.
- **D** A plus Pflichtprobe im Original durch den Nutzer. + Beleg im Original; − Abnahme hängt am Nutzer.
- **Empfehlung C.**

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- DOSBox-Nachaufnahme für A115, A189, A321, A357, A364 (E14-5 = C): später, wenn der Nutzer Lust hat.
- `livespiel` (Torszenen, Anzeigetafel, Reporter; Nr. 14/16): Umsetzung als letzter Schritt vor v1.0; Analyse in
  `re-livespiel`. Noch unbekannt und bei der Umsetzung von `livespiel` mitzuanalysieren (zusammen mit L7, `tore\`-Format):
  575C3h (Ov 138h, Lader `tore\`, einziger Aufrufer 567C8h; R10R-01 #387).
- Highscore am Spielende und Saisonende im Modus „nur Weltkugel“ (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

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
