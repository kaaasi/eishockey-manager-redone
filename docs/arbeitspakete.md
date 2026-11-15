# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 20 – Restliste bis v1.0, P3 aus Runde 19 (Plan 15.11.2026, Go offen)

Runde 19 abgeschlossen am 15.11.2026: [archiv/runde-19.md](archiv/runde-19.md). Live-Spiel vollständig (L1–L8); offen nur
#871, #872, #874 (P3/Testlücke) und A093. **Lage:** Die Inventur (`analyse/inventur.md`, Runde 10) ist der einzige Überblick
über die Annahmen, aber nicht mehr verlässlich: seit Runde 10 von 382 auf **616 `[APPROX]` in 215 Dateien** gewachsen
(Stand `482d2db`), Nachträge nur angehängt („Stand Runde n“), Prio-Spalte veraltet (z. B. A033, A041, A162 als P1 geführt,
aber seit Runde 11 umgesetzt). Ohne verlässliche Restliste lässt sich weder der Weg zu v1.0 schneiden noch der Umfang der
Abschlussprüfung (`prozess/anhang.md` C) festlegen. **Ziel der Runde:** belegte Restliste bis v1.0 als Grundlage für Runde 21 ff.
und die Abschlussprüfung; #871, #872, #874 geschlossen. Entscheidungen R20-D1…D4 im Plan-PR (Vorschlag: D1 = A, D2 = A, D3 = A,
D4 = C; unten so eingeplant).

**Base für alle Spuren:** aktueller `main` (Planstand); tatsächlichen Base-SHA in den Draft-PR. Regeln wie Runde 19 (Archiv): ein
Branch `paket/<spur>`, ein PR, Teile als Commits, höchstens drei Fortsetzungen; Befund reproduzieren, roter Regressionstest mit
Fundstelle, dann grün; Mehrsaisonprobe bei Saisonwechsel, Kader oder Geld; `[ORIGINAL]`/`[APPROX]` mit `Datei 0x…`; fremde Tests
nach R15-E2 = C; keine geplanten Aufgaben aus Läufen (R15-E4). Bei Unklarheit „wie im Original“, sonst `[APPROX]` mit Begründung,
die Leitstelle bündelt die Fragen. **Entwickler und Erstreviews sind Claude-Sitzungen;** ChatGPT nur fürs Gesamtreview.

**Folge (R20-D1 = A, R20-D2 = A):** Welle 1 parallel `inventur-v1` (Teile 1–2) und `pruefung-karte`. Merge `pruefung-karte` →
`inventur-v1` (Teil 3 erzeugt die Liste nach diesem Merge neu). Danach Gesamtreview nach R20-D4.

### Zuständigkeit für gemeinsame Dateien

- Neu `docs/analyse/restliste-v1.md`, neu `tools/reverse-engineering/markerliste.py`, Kopf von `docs/analyse/inventur.md`
  (Verweis „abgelöst durch restliste-v1.md“, Tabellen bleiben als Archiv), Fahrplan in `README.md` (nur Haken/Stand v0.4 und
  v0.9, minimal): allein `inventur-v1`. `inventur-v1` ändert **keinen** Code und keinen Marker in `src/`.
- `Core/MatchEngine.Minutes.cs`/`OriginalMinutes` (Reihe bei Spielbeginn, #872), `Core/Highscore*.cs` und Tests der Bestenliste
  (#874), `tests/**/AbgleichOriginalPaareTests.cs`, `AbgleichDosboxStrainHitsTests`, `docs/analyse/abgleich-dosbox.md`,
  `docs/analyse/belastung-staerke/`, `.gitignore` (#871), `docs/re-abdeckung.md`, `tests/**/Support/`: allein `pruefung-karte`.
  `pruefung-karte` nennt die geänderten Markerzeilen im PR (Eingabe für `inventur-v1` Teil 3), trägt aber nichts in `inventur.md` ein.

### Spur `inventur-v1` – Restliste bis v1.0 (R20-D1 = A, R20-D3 = A)

- **Teile:**
  1. **Generator:** `tools/reverse-engineering/markerliste.py` liest alle `[APPROX]`-Zeilen aus `src/` und ordnet sie den IDs
     der Tabelle 1 in `inventur.md` zu (Datei + Wortlaut, nicht Zeilennummer; neue Stellen ab A482, entfallene IDs als
     „entfallen“ mit letztem Stand); Ausgabe Markdown-Tabelle zwischen Markern in `restliste-v1.md` (`--write`, wie
     `funktionskarte.py`). Abnahme: Zählung = `grep -rc '\[APPROX\]' src/ | awk -F: '$2>0{n++;s+=$2}END{print n,s}'`, zweiter
     Lauf ohne Änderung (deterministisch), Probe mit einer verschobenen und einer umformulierten Markerzeile (ID bleibt bzw.
     wird als neu erkannt) im PR.
  2. **Einordnung:** jede Stelle genau eine Klasse – **V** v1.0-Blocker (spielwirksam, aus em.exe belegbar, fehlt oder weicht
     ab; Fundstelle `Datei 0x…`), **D** nur mit DOSBox/Video belegbar (→ Aufnahmeliste, inkl. A093), **E** bewusst
     (Entscheidung Nutzer mit Kennung, Plattformersatz, nach v1.0), **K** ohne Spielwirkung bzw. kosmetisch (bleibt `[APPROX]`
     bis zur Abschlussprüfung), **X** Marker veraltet (Code belegt → Umstellung auf `[ORIGINAL]`, gesammelt). Dazu die
     Funktionen „dokumentiert“ und „genähert“ der Funktionskarte (5 % bzw. 3 Funktionen): je Funktion „fehlt im Remake“
     (→ V/D), „vorhanden ohne Marker“ (→ X) oder „nicht nötig“ mit Grund. Klasse V und „fehlt“ nur mit ausführbarem Beleg
     (`zusammenarbeit.md` §5 Punkt 7: Fundstelle und erfolglose Codesuche), sonst höchstens „Verdacht“ (eigene Spalte).
  3. **Restliste (nach Merge `pruefung-karte`):** Liste neu erzeugen, `restliste-v1.md` mit Zusammenfassung je Bereich
     (Laden/Speichern, Tages-/Saisonwechsel, Finanzen, Wettbewerbe, Kader/Transfer, Bedienung/Pixel, Live-Spiel) und Klasse,
     Aufwand je V-Eintrag (S/M/L), **Vorschlag Zuschnitt Runde 21 ff.** (Spuren nach Bereichen), DOSBox-Sammelliste (Klasse D,
     Vorlage `vorlagen/dosbox-aufnahme.md`), **Vorschlag Umfang der Abschlussprüfung** (was dort geprüft wird, was Klasse K/E
     bewusst offen lässt). Kopf in `inventur.md`, Fahrplan `README.md` (v0.9 erledigt; v0.4 „Spielerstatistiken“ prüfen).
  Abnahme gesamt: jede der Stellen genau eine Klasse (Gegenprobe im PR per Skript), je Klasse mindestens drei Einträge mit
  nachvollziehbarem Beleg als Stichprobe im PR; keine Änderung unter `src/`, `tests/`.
- **Modell:** Claude Opus 5.5 (Alternative: Claude Fable 5.2 für Teil 2). **Erstreview:** Claude Opus 5.5 (Einordnung gegen
  Code und em.exe, keine reine Doku-Prüfung). **Status:** wartet auf Go.

### Spur `pruefung-karte` – P3 und Testlücke aus Runde 19 (R20-D2 = A)

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
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** wartet auf Go.

### Gesamtreview Runde 20 (R20-D4 = C)

ChatGPT (Codex, GPT-6.1 Terra) nach Merge beider Spuren, eng: Stichprobe der Einordnung in `restliste-v1.md` (je Klasse, besonders
V gegen K/E und „nicht nötig“) und der Fix #872; Absatz `anhang.md` C mit diesen beiden Punkten.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- A093 (Technik −10 außerhalb der eigenen Reihe): mit `runde-17` und `runde-19` nicht belegbar (ohne Klick spielt niemand
  außerhalb seiner Reihe); braucht eine Aufnahme mit Umstellung im Spiel – wenn der Nutzer Lust hat.
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen (nach Runde 19)

- **#871, #872, #874:** P3 und Testlücke aus Runde 19 → Runde 20 `pruefung-karte`.
- **Testlücke Gesamtreview #848:** Speichern/Laden mitten in einer angehaltenen Live-Paarung (über die Oberfläche nicht
  erreichbar; prüfen, sobald ein Speicherweg im Live-Spiel dazukommt).
- **Nach v1.0:** Einstellungen aus R15-E3 und bitgenauer Zufallsmodus (`anforderungen/zusatzfunktionen.md` C).

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
