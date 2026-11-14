# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 18 – Live-Spiel und Auswertung DOSBox `runde-17` (Plan 13.11.2026, Go 13.11.2026)

Runde 17 abgeschlossen am 13.11.2026: [archiv/runde-17.md](archiv/runde-17.md). Grundlage: Umsetzungsplan Live-Spiel L1–L8 in
[analyse/re-livespiel.md](analyse/re-livespiel.md) §12 (Spuren A/B/C), DOSBox-Aufnahme `runde-17` (Referenz-Repo #29–#34) für
#730 und #773, offene P3 #803, #804, #805. Ziel: Live-Spiel ohne Torszenen spielbar (L1–L6, L8), offene Abgleich-Issues belegt
geschlossen oder mit Beleg zurückgestellt. **Entscheidungen Nutzer 13.11.2026** (Optionen im Plan-PR #809): R18-D1 = A
(Zuschnitt zwei Wellen, Torszenen Runde 19), R18-D2 = A (eigene Spur `abgleich-dosbox`), R18-D3 = A (Zufallszüge der Anzeige im
Kern wie im Original, Bedingung des Originals, unabhängig vom Remake-Bildschirm). Plan-Review #810 (R809 P2-1, P3-1…3
eingearbeitet). **Go Nutzer 13.11.2026**, alle Spuren und Erstreviews Claude Opus 5.5.

**Base für alle Spuren:** aktueller `main` (Planstand `81a358c`); tatsächlichen Base-SHA in den Draft-PR. Regeln wie Runde 17
(Archiv): ein Branch `paket/<spur>`, ein PR, Teile als Commits, höchstens drei Fortsetzungen; je ID zuerst prüfen, ob sie noch
offen ist; Befund reproduzieren, roter Regressionstest mit Fundstelle, dann grün; Mehrsaisonprobe bei Saisonwechsel, Kader oder
Geld; Schema-Änderung mit Migration in neuer Datei `Core/SaveGame.Migration.<Spur>.cs` (Leitstelle nummeriert beim Merge);
`[ORIGINAL]`/`[APPROX]` mit `Datei 0x…`; fremde Tests nach R15-E2 = C; keine geplanten Aufgaben aus Läufen (R15-E4). Bei
Unklarheit „wie im Original“, sonst `[APPROX]` mit Begründung, die Leitstelle bündelt die Fragen. **Entwickler und Erstreviews
sind Claude-Sitzungen;** ChatGPT nur fürs Gesamtreview.

**Grundsätze Live-Spiel** (`re-livespiel.md` §12): Der Spielkern bleibt ohne Bildschirm vollständig; der Live-Bildschirm treibt
die Minuten nur über Kern-Befehle, Zeit vergeht weiter nur über `GameState.Continue()`, Bildschirme ändern durch Öffnen nichts
(`AGENTS.md` §4). Zufallszüge, die das Original nur mit Anzeige macht (Strafgrund, Szenenwahl), zieht nach R18-D3 = A der Kern unter der
Bedingung des Originals (Managerpaarung, Live-Modus der Optionen), auch ohne offenen Bildschirm. `lrandom` (Zwinkern)
ist ein eigener Generator getrennt vom Spielzufall (Review #795 P3-1).

**Folge (R18-D1 = A):** Welle 1 parallel `livespiel-kern`, `livespiel-ui` (Teile B1, B4), `abgleich-dosbox`,
`pruefung-karte` Teil 1. Merge `pruefung-karte` Teil 1 → `abgleich-dosbox` → `livespiel-kern` → `livespiel-ui`. Welle 2 nach dem
Merge von `livespiel-kern`: `livespiel-ui-2` (B2, B3, eigener Branch `paket/livespiel-ui-2`); zuletzt `pruefung-karte` Teil 2
(`paket/pruefung-karte-2`), am Rundenende Gesamtreview (ChatGPT). Torszenen (L7-Anzeige, Spur C `torszenen`) in Runde 19.

### Zuständigkeit für gemeinsame Dateien

- `Core/MatchEngine.Live.cs`: allein `livespiel-kern` (nur Anschlüsse der neuen Dateien); braucht `abgleich-dosbox` dort eine
  Stelle, nennt es sie im PR, die Leitstelle löst beim Merge auf.
- Alle übrigen `Core/MatchEngine.*.cs` (u. a. `Minutes`, `PowerPlay`, `Managed`, `Preparation`, `Original`, `Injuries`,
  `TeamValues`, `Statistics`, `National`, `Shootout`, `MatchEngine.cs`; Reihenwechsel, Strafen, Einsatz 0x26): allein
  `abgleich-dosbox`; `livespiel-kern` legt nur neue Dateien an und ruft dort nur auf (z. B. `RecountSkaters`) (R809 P3-1).
  **Benannte Ausnahme (R809 P2-1):** `livespiel-kern` A4 darf in `MatchEngine.Minutes.cs` genau die Anzeigezüge einfügen –
  Strafgrund in `Penalty` (nach der Strafe, vor dem Kettenzug) und Szenenwahl am Torereignis –, minimal, je Stelle im PR mit
  Fundstelle begründet; sonst nichts in dieser Datei. `abgleich-dosbox` wird zuerst gemergt, `livespiel-kern` setzt beim
  Rebase darauf auf; ändert `abgleich-dosbox` die Zugfolge in `Penalty` (#773), nennt es das im PR.
- `Core/GameOptions.cs` (Geschwindigkeit, Lampe, Sicherung `0xE19B`/`0xE19C`): allein `livespiel-kern`;
  `UI/Screens/GameOptionsScreen.cs`: allein `livespiel-ui`.
- `UI/Screens/MatchDayScreen.cs`, `UI/Screens/SeasonEndGameOverScreen.cs`, neu `UI/LiveWidgets.cs`, `UI/Screens/Live/**`,
  `Tools/Scenarios/Live.cs`: allein `livespiel-ui` bzw. danach `livespiel-ui-2`.
- `UI/Screens/Team/LineupScreen.cs`, `TacticsScreen.cs`: nur `livespiel-ui-2`, nur Kennzeichen „im Spiel“ (minimal, im PR
  begründet); `PlayerHintLine.cs`, `PlayerInfoPopup.cs`: allein `pruefung-karte` Teil 1 (#805).
- `Core/OriginalImport*.cs`, `Core/OriginalExport*.cs`, `Formats/OriginalSave*.cs`, `tests/**/AbgleichOriginalPaareTests.cs`:
  allein `abgleich-dosbox`; andere Spuren nur die erwarteten Einträge ihrer IDs, im PR genannt.
- `Core/GameState.Integration.cs`, `GameState.cs`: höchstens minimal, im PR begründet.
- `docs/reverse-engineering.md`, `docs/analyse/re-livespiel.md`: je Spur ein eigener Abschnitt, die Leitstelle löst beim Merge auf.
- `docs/analyse/inventur.md`, `docs/re-abdeckung.md`, `tools/reverse-engineering/*`, `tests/**/Support/`: allein
  `pruefung-karte` (Teil 2); die anderen Spuren nennen ihre Inventurzeilen im PR.

### Spur `livespiel-kern` – Anzeigezähler, Ablaufmodell, Eingriff, Zufall der Anzeige (Spur A)

- **Teile** (Inhalt, Dateien und Abnahme je Teil wie `re-livespiel.md` §12 Tabelle „Spur A“):
  1. **A1 Anzeigezähler (L1):** Reihenliste bis 4 Paarungen (`0xA75C`/`0x233`), Chancen/Tore je Drittel und Seite
     (`0xA839`/`0xA83F`, Minute ≤ 60), Strafminuten (`0xA845`); neu `Core/MatchEngine.Scoreboard.cs`.
  2. **A2 Ablaufmodell (L5):** Geschwindigkeit 0–80, Pausen je Minute, Zwinkern 1/5 über `lrandom`, Schnelldurchlauf ≥ 70,
     Warten nach Minute 60/70, Spieltagssicherung, Lampe §10 Schritt 1; nur Modell (Bilder je Minute), keine Wanduhr; neu
     `Core/LiveClock.cs`.
  3. **A3 Eingriff als Befehle (L6):** Einsatz 0, 4 … 100, Angriff 0–7, Reihe auf dem Eis mit Prüfung 4. Reihe (0x08E5F),
     Kennzeichen „im Spiel“, danach `RecountSkaters`; neu `Core/MatchEngine.Intervention.cs`.
  4. **A4 Zufall und Format (L4/L7-Kern):** Strafgrund, Szenenwahl §11 (Zugfolge exakt) nach R18-D3; Leser `tore\` (neu
     `Formats/GoalScene.cs`, `Core/MatchEngine.GoalScene.cs`).
  5. **A5 Minutenschritt im Kern (R18-E1 = A):** Paarungen des Spieltags vorbereiten, je Minute alle Paarungen (0x37A62),
     Abschluss wie `PlayWithEvents`, Eingriff ab der nächsten Minute; neu `Core/MatchEngine.LiveStep.cs`, Herauslösung in
     `MatchEngine.Live.cs` ohne Verhaltensänderung.
- **Abnahme:** wie §12; zusätzlich Mehrsaisonprobe unverändert grün und Abgleich-Paare unverändert (die Anzeige ändert ohne
  Eingriff keinen Spielwert außer den Zügen nach R18-D3; dafür erwartete Einträge begründet).
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** A1–A4 fertig in #814 (13.11.2026); A5 Minutenschritt im Kern (R18-E1 = A, Nutzer 13.11.2026) fertig in #814 (13.11.2026), wartet auf Erstreview über A1–A5.

### Spur `livespiel-ui` – Bausteine und Spielende (Spur B, Teile B1, B4)

- **Teile:**
  1. **B1 Bausteine (L3):** Spieluhr 0x51E90, Balken 0x53307/0x533D0, Lampe 0x53453, Ziffern 0x53234 in neu `UI/LiveWidgets.cs`;
     „Spiele laufen“ in `MatchDayScreen` darauf umstellen (Balken zweizeilig, 4 px je Minute, Ziffernabstand 10, Grundlinie 80).
  2. **B4 Spielende (L8):** 0x51F3D in `SeasonEndGameOverScreen` (schwarz, „Das wars...“ 100, „Carpe diem !“ 140, Klick).
- **Abnahme:** `karriere_02315` (Minute 08: oberer Balken 32 px gelb, die anderen rot, „08“ ab x = 96), Bildschirmfoto-Abläufe, Pixelvergleich mit
  Fundstelle; Textpositionen Spielende.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** B1 und B4 fertig (PR #811, 13.11.2026), wartet
  auf Erstreview; Abnahme `karriere_02315` korrigiert (Minute 08, `re-livespiel.md` §13).

### Spur `livespiel-ui-2` – Live-Bildschirm, Reporter, Klickwege (Welle 2, Teile B2, B3)

- **Voraussetzung:** `livespiel-kern` gemergt (B2 braucht A1/A2 und für den Reporter A4, B3 braucht A3).
- **Teile:**
  1. **B2 Live-Bildschirm und Reporter (L2/L4):** Aufbau 0x5353C, Anzeigetafel 0x52BD6, Meldung 0x52520, Mund/Augen; neu
     `Screens/Live/LiveMatchScreen.cs`, `Screens/Live/LiveReporter.cs`.
  2. **B3 Klickwege (L6):** Treffer-Tests 0x534BC/0x53508, Lampe, Kopf/Logo links = Aufstellung, rechts = Taktik, Neuaufbau
     0x5434C; vorher die drei offenen Stellen §10 lesen. Mausweg „Logo = Aufstellung“ samt Torwartwechsel wie FILE51 (Beleg aus
     `abgleich-dosbox` Teil 1).
  3. **Regler** der Geschwindigkeit in `GameOptionsScreen`, nur falls nach A2 nötig (aus `livespiel-ui` verlegt, R809 P3-3).
- **Abnahme:** Bildschirmfoto gegen `bundestrainer_00200` und `duell_s2a_01126`; Mundfolge „Chance:“ = [0, 9 × 8, 0xFF];
  Klickproben wie §12 B3; Ablauftest eines ganzen Spieltags mit Managerpaarung über den Live-Bildschirm = Ergebnis ohne Anzeige.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** PR #830 (Draft): B2 fertig (13.11.2026); B3 fertig (Treffer-Tests, Schieber, Reihenlampen, Lampe; Kopf/Logo → Aufstellung/Taktik im Spiel mit Neuaufbau in Fortsetzung 1, 13.11.2026); Regler nicht nötig (`GameOptionsScreen` hat ihn). Erstreview #831: Nein (R830-01 P2, R830-02/-03 P3); Fixlauf 13.11.2026 behebt alle drei, wartet auf Delta-Prüfung der Leitstelle.

### Spur `abgleich-dosbox` – Auswertung Aufnahme `runde-17` (R18-D2 = A)

- **Teile:**
  1. **#730 Belastung:** FILE50 (ohne Klick) und FILE51 (Torwartwechsel in Minute 26 über das Vereinslogo, Referenz-PR #30)
     gegen das Remake ab FILE02, Reihenbilder je Minute gegen
     `OriginalMinutes.ChangeLines`; A093 (Technik −10) mitbelegen. Liegt die Ursache im Mausweg des Eingriffs, nur Beleg und
     Testaufbau liefern – der Befehl gehört zu `livespiel-kern` A3, die Leitstelle verknüpft.
  2. **#773 Strafminuten:** FILE52→FILE53 (erster Spieltag): Einsatz 0x26 und Strafminuten je Verein; Ursache trennen (Einsatz oder
     Zahl der Strafwürfe), beheben oder mit Beleg `[APPROX]`.
  3. **Export/Import:** #804 (Vereinsfelder 0x26/0x2C mit Migration, fremde Abdrücke nach R15-E2), Exportprobe EXPORT1D/2D/2C
     (R758-01); FILE54 und Bilder `highscore-weltkugel_1…4` (Start FILE20, Referenz-PR #32) Highscore „nur Weltkugel“ und A189
     („Gesperrte“ trat nicht auf) nur auswerten und vermerken. A115/A321/A357/A364 sind seit Runde 13 belegt (Referenz-PR #34)
     und gehören nicht in diese Spur.
- **Abnahme:** je Issue roter Test → grün mit Fundstelle oder begründetes `[APPROX]` mit Beleg aus den neuen Ständen;
  `AbgleichOriginalPaareTests` um die Paare FILE02→50/51 und FILE52→53 erweitert; Mehrsaisonprobe (4 Wechsel) für Teile 1–3.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Go 13.11.2026; Teile 1–3 fertig (PR #812): #804 behoben,
  R758-01 erledigt, Highscore „nur Weltkugel“ ausgewertet (neuer Befund Punkte nach WM-Platz), A189 vermerkt.

### Spur `pruefung-karte`

- **Teil 1 (sofort, kleiner PR):** #803 (Börsentest-Fenster erkennt gesenkte Wachstumsrate), #805 (Belastungsanzeige A370:
  Formel in `PlayerHintLine.cs`/`PlayerInfoPopup.cs` nach Datei-Fundstelle, Bildschirmfoto). Modell Claude Opus 5.5 (Sonnet 5.5
  möglich), Erstreview Claude Opus 5.5.
- **Teil 2 (Welle 2, `paket/pruefung-karte-2`):** Inventur „Stand Runde 18“, Funktionskarte (L-Funktionen umgesetzt), gesammelte
  P3 der Runde. Modell Claude Opus 5.5, Erstreview Claude Opus 5.5.
- **Status:** Go 13.11.2026 (Teil 1 Welle 1, Teil 2 Welle 2). Teil 1 fertig 13.11.2026, PR #813 (#803, #805), wartet auf
  Erstreview; Inventurzeile A370 und `reverse-engineering.md` (Hinweiszeile/0x041AC, [APPROX] Belastung) für Teil 2. Teil 2 fertig 13.11.2026, PR #833 (Inventur „Stand Runde 18“, Funktionskarte 268 → 288 umgesetzt, `reverse-engineering.md` Belastung, P3 #817–#819, #824, #825, #828, #829), wartet auf Erstreview.

### Spur `nutzer-befunde` – Befunde der Nutzerprobe `01ed8a5` (Welle 3)

- **Ziel:** Befunde der Nutzerprobe 13.11.2026 (main `01ed8a5`) wie im Original beheben; Entscheidungen Nutzer 13.11.2026:
  R18-E2 = A, R18-E3 = A, R18-E4 = wie im Original.
- **Befunde:** #834 (P1) Werbung Eisfläche: Anbieterwahl, Logo, Angebotstext; #838 (P2) Echtzeit „Spiele laufen“ (0x130EE,
  Bildtakt); #837 (P2) Zuschauer im Live-Heimspiel (0x53A11–0x53B37, Kern rechnet vor dem Spiel); #841 (P2) Kachel „Neuer“ =
  Spielvorbereitung, Posteingang über die Nachrichtenleiste; #836 (P3) Kachel „Einstellungen“ = Optionsfenster 0x2CD1C,
  Geräteeinstellungen über Zusatzknopf/F8/F11; #840 (P3) Finanzbuch über eigenen Knopf, Bank-Klick ohne Wirkung; #839 (P3)
  Schrift rechte Spalte Finanzfenster; #835 (P3) Rand um das Managerbild im Hauptmenü.
- **Dateigrenzen:** betroffene Bildschirme unter `src/EishockeyManager.UI/Screens/**` (Werbung, Finanzen,
  Einstellungen/Optionen, Posteingang, Spielvorbereitung, Live); `LiveClock.cs`/Desktop-Bildtakt nur für #838; für #837 eine
  neue Kern-Datei; `MenuTiles.cs`/`MainMenuScreen.cs` minimal mit Begründung; `docs/analyse/inventur.md` nicht (Zeilen im PR).
- **Teile:** 1. #834, #839, #840, #835, #836, #838 (Bildschirme); 2. #837 (Zuschauer vor dem Spiel im Kern, neue Kern-Datei,
  Mehrsaisonprobe); 3. #841 (Kachel „Neuer“ = Spielvorbereitung für weitere Manager im laufenden Spiel).
- **Modell:** Claude Opus 5.5. **Status:** PR #842 (Draft, Base `01ed8a5`, rebased auf `4e55e07`): Teil 1 fertig
  (13.11.2026); Teil 2 fertig (14.11.2026, Fortsetzung 1: Zuschauer aus der Vorbereitung vor dem Spiel, `Core/MatchGate.cs`);
  Teil 3 fertig (14.11.2026, Fortsetzung 2: `GameState.AddManager.cs`, Spielvorbereitung im Hinzufügen-Modus); Spur fertig – Review (`b41507d`: Nein, P1-1). Fixlauf 14.11.2026: P1-1
  (Hinzufügen nach dem Laden, atomar), P3-1 (`[APPROX]` Zufall), P3-2 (Proben geschärft) behoben – Nachprüfung.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- DOSBox-Aufnahme `runde-17` **liegt vor** (13.11.2026, Referenz-Repo PRs #29–#34): FILE50/FILE51 (#730, Sitzung 1),
  FILE52/FILE53 (#773), FILE54 (Highscore „nur Weltkugel“), Exportprobe EXPORT1D/2D und EXPORT2C (R758-01); A189 „Gesperrte“
  trat in einer ganzen Saison nicht auf, A115/A321/A357/A364 aus Runde 13 belegt. **Auswertung in Runde 18.**
- #730 (Belastung der Managerspieler, R16-E2 = C) und #773 (Strafminuten der ersten Spieltage): DOSBox-Aufnahmen nach
  `analyse/belastung-staerke.md` §7.3 bzw. Stand nach Spieltag 1 eines neuen Spiels (Befund im Issue).
- Highscore am Spielende und Saisonende im Modus „nur Weltkugel“ (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen (nach Runde 17)

- **Torszenen (Runde 19):** L7-Anzeige, Spur C `torszenen` nach `analyse/re-livespiel.md` §12 (nach `livespiel-kern` A4).
- **P3 aus Runde 17:** #803 (Börsentest-Fenster), #804 (Import/Export 0x26/0x2C), #805 (Belastungsanzeige A370).
  Übrige Restzeilen der Runde (u. a. A390 Symbol 7, A251 Gebotshöhe 0x47347, A226, A418–A437): Inventur „Stand Runde 17“ (R807-01).
- **DOSBox-Aufnahme** (nach Lust des Nutzers, `vorlagen/dosbox-aufnahme.md`): Punkte unter „Zurückgestellt“, dazu EXPORT2C und
  die Exportstände ab Head `97c372a` (Restlücke R758-01).
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
