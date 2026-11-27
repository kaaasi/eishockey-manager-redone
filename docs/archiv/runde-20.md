# Archiv: Runde 20 – Restliste bis v1.0, P3 aus Runde 19 (15.–16.11.2026)

Aus `docs/arbeitspakete.md` ausgelagert am 16.11.2026. Inhalt unverändert bis auf Überschrift, „Ergebnis“, „Abschluss“ und
„Entscheidungen während der Runde“, ohne „Zurückgestellt“ und „Vormerkungen“ (bleiben in `arbeitspakete.md`). Die Statuszeilen
der Spuren geben den Stand vor dem jeweiligen Merge wieder. Reviews: `docs/archiv/reviews/runde-20/` (Dateien vom 15.–16.11.2026). Nur bei Bedarf
lesen. Relative Links beziehen sich auf `docs/`.

**Ergebnis:** Spuren `pruefung-karte` (#880, Erstreview #881 Ja) und `inventur-v1` (#883, Erstreview #884 Nein → Fixlauf,
Delta-Prüfung der Leitstelle) gemergt; Plan #878 (Plan-Review #879). Geschlossen: #871, #872 (Reihen auf dem Eis bleiben aus dem
Vorspiel stehen, Datei 0x84D7, Felder 0x2E/0x30, Schema 51), #874. Neu: belegte Restliste bis v1.0
([analyse/restliste-v1.md](analyse/restliste-v1.md), Einordnung `analyse/restliste-v1-klassen.csv`, Generator
`tools/reverse-engineering/markerliste.py`): 689 Einträge, **V 85, D 45, E 295, K 182, X 82**, Zuschnitt Runde 21 ff.,
DOSBox-Sammelliste, Umfang der Abschlussprüfung, Abweichungsliste vor v1.0 (`markerliste.py --abweichungen`). Gesamtreview
(#885, ChatGPT/Codex) **Ja** für `a509f5c`: keine neuen Befunde.

**Abschluss (16.11.2026):** offen #882 (P3 aus #881), A093 (D), Vormerkungen in `arbeitspakete.md`.

**Entscheidungen während der Runde:** Nutzer 15.11.2026: R20-D1 = A, R20-D2 = B, R20-D3 = A mit Zusatz (Abweichungsliste vor
v1.0), R20-D4 = C, Go. Nutzer 16.11.2026 (Fragen der Restliste, Kommentare in #883): **N1 = B** (ganze DM bleiben; nach v1.0
Umschaltung auf andere Sprachen und Währungen als mögliche Ergänzung, `anforderungen/zusatzfunktionen.md` A 2), **N2 = B**
(Fehler des Originals A247, A701, A715, A775 bleiben korrigiert, E), **N3 = B** (eigene Zufallsquellen auf `GameState.Random`,
Spur `zufallsstrom`), **N4 = C** (HOK-Import zählt für die übernommenen Werte, Ablauf-Feinheiten wie A397 bleiben E), **N5 = C**
(Buchung bleibt, Original-Finanzübersicht prüfen und ggf. ohne diese Posten), **N6 = A** (Zufall der Darstellung A061, A271,
A274, A438, A452 behält eigene Quelle; mit v1.0 und dem bitgenauen Modus erneut prüfen). Leitstelle nach Prozess: #872 nach
versehentlichem Schließen beim Plan-Merge wieder geöffnet; Fixlauf #883 nur P2/P3 → Delta-Prüfung statt Nachprüfung.

---

## Aktuelle Runde: Runde 20 – Restliste bis v1.0, P3 aus Runde 19 (Plan 15.11.2026, Go 15.11.2026)

Runde 19 abgeschlossen am 15.11.2026: [archiv/runde-19.md](archiv/runde-19.md). Live-Spiel vollständig (L1–L8); offen nur
#871, #872, #874 (P3/Testlücke) und A093. **Lage:** Die Inventur (`analyse/inventur.md`, Runde 10) ist der einzige Überblick
über die Annahmen, aber nicht mehr verlässlich: seit Runde 10 von 382 auf **616 `[APPROX]` in 215 Dateien** gewachsen
(Stand `482d2db`), Nachträge nur angehängt („Stand Runde n“), Prio-Spalte veraltet (z. B. A033, A041, A162 als P1 geführt,
aber seit Runde 11 umgesetzt). Ohne verlässliche Restliste lässt sich weder der Weg zu v1.0 schneiden noch der Umfang der
Abschlussprüfung (`prozess/anhang.md` C) festlegen. **Ziel der Runde:** belegte Restliste bis v1.0 als Grundlage für Runde 21 ff.
und die Abschlussprüfung; #871, #872, #874 geschlossen. **Entscheidungen Nutzer 15.11.2026** (Optionen im Plan-PR #878): R20-D1 = A, R20-D2 = B
(`pruefung-karte` zuerst, `inventur-v1` danach), R20-D3 = A mit Zusatz (unten), R20-D4 = C. **Go Nutzer 15.11.2026** (alle Spuren samt Folgeläufen, Modelle wie unten). Plan-Review #879 Ja (P2-1 und
P3-1…3 eingearbeitet).

**R20-D3 Zusatz (Nutzer 15.11.2026):** Unmittelbar vor v1.0 bekommt der Nutzer eine Liste, welche Bereiche und Funktionen
noch vom Original abweichen (alle Klassen außer „belegt gleich“, also auch K und E); er entscheidet dann, was doch noch
geändert wird. `restliste-v1.md` ist so aufzubauen, dass diese Liste daraus erzeugt werden kann (Bereich, Funktion bzw.
Bildschirm, Abweichung in einem Satz, Klasse).

**Base:** aktueller `main` beim Start der Spur (`inventur-v1`: nach Merge von `pruefung-karte`); tatsächlichen Base-SHA in den Draft-PR. Regeln wie Runde 19 (Archiv): ein
Branch `paket/<spur>`, ein PR, Teile als Commits, höchstens drei Fortsetzungen; Befund reproduzieren, roter Regressionstest mit
Fundstelle, dann grün; Mehrsaisonprobe bei Saisonwechsel, Kader oder Geld; `[ORIGINAL]`/`[APPROX]` mit `Datei 0x…`; fremde Tests
nach R15-E2 = C; keine geplanten Aufgaben aus Läufen (R15-E4). Bei Unklarheit „wie im Original“, sonst `[APPROX]` mit Begründung,
die Leitstelle bündelt die Fragen. **Entwickler und Erstreviews sind Claude-Sitzungen;** ChatGPT nur fürs Gesamtreview.

**Folge (R20-D1 = A, R20-D2 = B):** Welle 1 `pruefung-karte`; nach deren Merge Welle 2 `inventur-v1` auf festem Stand.
Danach Gesamtreview nach R20-D4 = C.

### Zuständigkeit für gemeinsame Dateien

- Neu `docs/analyse/restliste-v1.md`, neu `docs/analyse/restliste-v1-klassen.csv`, neu `tools/reverse-engineering/markerliste.py`, Kopf von `docs/analyse/inventur.md`
  (Verweis „abgelöst durch restliste-v1.md“, Tabellen bleiben als Archiv), Fahrplan in `README.md` (nur Haken/Stand v0.4 und
  v0.9, minimal): allein `inventur-v1`. `inventur-v1` ändert **keinen** Code und keinen Marker in `src/`.
- `Core/MatchEngine.Minutes.cs`, `Core/MatchEngine.Managed.cs` (`ChangeLines`, Reihe bei Spielbeginn, #872), `OriginalMinutes`,
  `Core/GameState.DailyRun.cs` (nur `UpdateManagerTeamValues`, #871 P3-3, minimal), `Core/Highscore*.cs` und Tests der Bestenliste
  (#874), `tests/**/AbgleichOriginalPaareTests.cs`, `AbgleichDosboxStrainHitsTests`, `docs/archiv/analyse/abgleich-dosbox.md`,
  `docs/archiv/analyse/belastung-staerke/`, `.gitignore` (#871), `docs/re-abdeckung.md`, `tests/**/Support/`: allein `pruefung-karte`.
  `pruefung-karte` nennt die geänderten Markerzeilen im PR (Eingabe für `inventur-v1`), trägt aber nichts in `inventur.md` ein.

### Spur `inventur-v1` – Restliste bis v1.0 (Welle 2, R20-D1 = A, R20-D3 = A)

- **Teile:**
  1. **Generator:** `tools/reverse-engineering/markerliste.py` liest alle `[APPROX]`-Zeilen aus `src/` und ordnet sie den IDs
     der Tabelle 1 in `inventur.md` zu (Datei + Wortlaut, nicht Zeilennummer; neue Stellen ab A482, entfallene IDs als
     „entfallen“ mit letztem Stand); Ausgabe Markdown-Tabelle zwischen Markern in `restliste-v1.md` (`--write`, wie
     `funktionskarte.py`). **Einordnung getrennt vom generierten Block (R879 P2-1):** handgepflegt in
     `restliste-v1-klassen.csv`, Schlüssel ID, Spalten ID, Klasse, Bereich, Funktion/Bildschirm, Abweichung (ein Satz), Beleg,
     Verdacht, Aufwand; der Generator liest sie, schreibt sie in den Block, meldet Marker ohne Klasse („unklassifiziert“,
     Rückgabe ≠ 0) und Klassen ohne Marker („entfallen“). Abnahme: Zählung = `grep -rc '\[APPROX\]' src/ | awk -F: '$2>0{n++;s+=$2}END{print n,s}'`, zweiter
     Lauf ohne Änderung (deterministisch), Probe mit einer verschobenen und einer umformulierten Markerzeile (ID bleibt bzw.
     wird als neu erkannt) im PR.
  2. **Einordnung:** jede Stelle genau eine Klasse – **V** v1.0-Blocker (spielwirksam, aus em.exe belegbar, fehlt oder weicht
     ab; Fundstelle `Datei 0x…`), **D** nur mit DOSBox/Video belegbar (→ Aufnahmeliste, inkl. A093), **E** bewusst
     (Entscheidung Nutzer mit Kennung, Plattformersatz, nach v1.0), **K** ohne Spielwirkung bzw. kosmetisch (bleibt `[APPROX]`
     bis zur Abschlussprüfung), **X** Marker veraltet (Code belegt → Umstellung auf `[ORIGINAL]`, gesammelt). Dazu die
     Funktionen „dokumentiert“ und „genähert“ der Funktionskarte (5 % bzw. 3 Funktionen): je Funktion „fehlt im Remake“
     (→ V/D), „vorhanden ohne Marker“ (→ X) oder „nicht nötig“ mit Grund. Klasse V und „fehlt“ nur mit ausführbarem Beleg
     (`zusammenarbeit.md` §5 Punkt 7: Fundstelle und erfolglose Codesuche), sonst höchstens „Verdacht“ (eigene Spalte).
  3. **Restliste:** `restliste-v1.md` mit Zusammenfassung je Bereich
     (Laden/Speichern, Tages-/Saisonwechsel, Finanzen, Wettbewerbe, Kader/Transfer, Bedienung/Pixel, Live-Spiel) und Klasse,
     Aufwand je V-Eintrag (S/M/L), **Vorschlag Zuschnitt Runde 21 ff.** (Spuren nach Bereichen), DOSBox-Sammelliste (Klasse D,
     Vorlage `vorlagen/dosbox-aufnahme.md`), **Vorschlag Umfang der Abschlussprüfung** (was dort geprüft wird, was Klasse K/E
     bewusst offen lässt). Abweichungsliste vor v1.0
     nach R20-D3-Zusatz (Erzeugung beschrieben, Probelauf am Stand der Spur). Kopf in `inventur.md`, Fahrplan `README.md`
     (v0.9 erledigt; v0.4 „Spielerstatistiken“ prüfen).
  **Abgrenzung (R879 P3-1):** V wirkt auf Spielwerte, Ergebnisse, Geld, Abläufe oder Inhalte von Texten; K nur auf
  Darstellung ohne Spielwert (Pixel, Farbe, Lage, Wortlaut ohne neuen Inhalt). Aufwand: S = Teil einer Spur ohne
  Mehrsaisonprobe, M = ein Teil mit Mehrsaisonprobe oder Migration, L = mehrere Teile bzw. eigene Spur.
  Abnahme gesamt: Generatorlauf ohne „unklassifiziert“ (jede Stelle genau eine Klasse), je Klasse mindestens drei Einträge
  (aus verschiedenen Bereichen, soweit vorhanden) mit Beleg nach §5 Punkt 7 als Stichprobe im PR; keine Änderung unter `src/`, `tests/`.
- **Voraussetzung:** `pruefung-karte` gemergt.
- **Modell:** Claude Opus 5.5 (Alternative: Claude Fable 5.2 für Teil 2). **Erstreview:** Claude Opus 5.5 (Einordnung gegen
  Code und em.exe, keine reine Doku-Prüfung). **Status:** Teile 1–3 fertig in PR #883 (16.11.2026), wartet auf Erstreview; Fragen N1–N5 an den Nutzer in `analyse/restliste-v1.md`.

### Spur `pruefung-karte` – P3 und Testlücke aus Runde 19 (Welle 1, R20-D2 = B)

- **Teile:**
  1. **#871 (P3-1…4):** `__pycache__/hok.cpython-313.pyc` löschen, `__pycache__/` in `.gitignore`; ZIMMERMANN in den Sägezahn
     von `AbgleichOriginalPaareTests` mit Drift-Wächter `Strain_DeviatesAtMostSeeds_DriftGuard` (Review #870 §1);
     `UpdateManagerTeamValues` über `WriteLineupTeamValues` mit eigenem `[APPROX]`; Bezugsbasis `abgleich-dosbox.md` Z. 331 und
     Remarks `AbgleichDosboxStrainHitsTests`.
  2. **#872 Reihe bei Spielbeginn:** Sturm beginnt mit der Reihe aus dem Vorspiel, Abwehr mit Reihe 1 (Beleg `File61[3]`: Köln
     Sturm Reihe 2, Abwehr Reihe 0; `abgleich-dosbox.md` §4.1); Fundstelle in em.exe suchen (Speicherort der letzten Reihe je
     Verein, Laden/Speichern); roter Test → Fix, `[APPROX]` an `ChangeLines` auflösen. Wirkt auf Belastung und Ergebnisse:
     Abgleich-Paare, Mehrsaisonprobe (4 Wechsel, passiv/aktiv) und erwartete Listen nachziehen (R15-E2 = C); ist das Feld nicht
     im Spielstand, Migration in neuer Datei `Core/SaveGame.Migration.<Spur>.cs`. Ohne Beleg der Fundstelle: Bericht, `spur-wartet`,
     #872 bleibt offen (→ Restliste).
  3. **#874 Testlücke Bestenliste:** Darstellung und Eintragung der Bestenliste am Spielende „nur Weltkugel“ (FILE54: Platz,
     Name, Punkte; Bildschirmfoto-Ablauf oder Zustandstest), Mutation zeigt rot.
  4. **Abschluss:** Funktionskarte neu (`funktionskarte.py --write`), Markerzeilen der Spur im PR für `inventur-v1`.
- **Tests:** `--schnell` mit Originaldaten (Abgleich-Paare, FILE54), Mehrsaisonprobe für #872.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Teile 1–4 fertig in PR #880 (15.11.2026), wartet auf Erstreview.

### Gesamtreview Runde 20 (R20-D4 = C)

ChatGPT (Codex, GPT-6.1 Terra) nach Merge von `inventur-v1`, eng: Stichprobe der Einordnung in `restliste-v1.md` (je Klasse, besonders
V gegen K/E und „nicht nötig“) und der Fix #872; Absatz `anhang.md` C mit diesen beiden Punkten.
