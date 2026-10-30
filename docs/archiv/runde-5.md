# Archiv: Runde 5 – Restbefunde und Zurückgestelltes vor dem Live-Spiel (29.–30.10.2026)

Aus `docs/arbeitspakete.md` ausgelagert am 30.10.2026, damit Agenten beim Einstieg nur die aktuelle Runde lesen.
Inhalt unverändert bis auf die Überschrift. Ergebnisse und Reviews: `docs/reviews/`. Nur bei Bedarf lesen.

**Ergebnis:** Integration #128 (`highscore-nacharbeit`, `paarungen-liga`, `debpokal-auslosung`, `taktik-verein`,
`bundestrainer-modus4`; Spielstand Schema 6; R5-01 #130 behoben), einzeln #124 (`finanzen-nachpruefung`, FN-01 #139),
#127 (Referenzvideos `videos-roh-1`/`-2`, Ladeskript nach vier Nachprüfungen sicher), #146 (Review-Vorlage: Head-Nachweis
über Tree-SHA). Offen in Runde 6: #125, #126, #132–#134, #136, #157, #158; #135 wartet auf `livespiel`.

## Runde 5 – Restbefunde und Zurückgestelltes vor dem Live-Spiel (ab 29.10.2026)

Runde 4 (Nationalteam-Aufstellung/-Taktik, Pokaltabellen, Highscore, Kachelsperren, #80): [archiv/runde-4.md](archiv/runde-4.md);
Runde 3: [archiv/runde-3.md](archiv/runde-3.md); Runden 1–2: [archiv/runden-1-2.md](archiv/runden-1-2.md).
**Ziel der Runde (Entscheidung Nutzer, 29.10.2026):** offene Befunde schließen und die zurückgestellten Punkte klären,
soweit sie ohne Live-Spiel belegbar sind. Danach bleiben bis v1.0 nur `livespiel` und die davon abhängigen Statistiken.
**Base für alle Spuren:** aktueller `main` (≥ diesem Plan); den tatsächlichen Base-SHA trägt die Spur in ihren Draft-PR ein.
Weiterhin verbindlich: Mehrsaisonprobe für jede Spur, die Saisonwechsel, Kader oder Geld berührt; Spielstand-Schema mit
Version und Migration für jeden neuen Zustand; Befund-Issues mit `Fixes #<nr>` schließen.

```
Runde 5 (parallel):  highscore-nacharbeit   debpokal-auslosung   taktik-verein
                     finanzen-nachpruefung  bundestrainer-modus4  paarungen-liga
```

Gemeinsame Berührungen: `debpokal-auslosung` und `paarungen-liga` arbeiten beide in `UI/Screens/Competitions/`, aber in
getrennten Dateien; `taktik-verein` und `bundestrainer-modus4` können sich nur in `Core/CupsNational*.cs` treffen
(getrennte Dateien vorgeben, der Zweite rebased). Konflikte löst die Leitstelle in der Integration.

### Spur `highscore-nacharbeit` – P3-Befunde der Bestenliste (#114, #115, #117)

- **Ziel:** die Befunde H2-01, H2-02, H2-04 aus [Review Runde 4 Teil 2](reviews/2026-10-29-integration-runde-4-teil-2.md) schließen.
- **Abnahme:** (1) H2-01: Titel „Highscore“ pixelgleich mit `oberflaeche_0844–0855` (Titelpixel x 117 … 202) – Korrektur nur
  für diesen Aufruf oder in `CompetitionWidgets.Centered` mit Nachweis, dass alle übrigen Bildschirmfotos bytegleich
  bleiben – oder `[APPROX]` mit Begründung; automatischer Pixeltest des Titels; Aussage „pixelgenau“ in
  `docs/highscore.md` berichtigt. (2) H2-02: Test für die Bedingung „Dauer = eine Saison und Startliga = Bundesliga“
  (Mutation `!(duration == 0)` muss rot werden). (3) H2-04: die drei Abweichungen der Punkteformel (Saisonzahl DGROUP
  0x3BD, Platzierungsterm vor Spieltag 1, Titelzähler/16-Bit-Überlauf) umsetzen oder als `[APPROX]` in
  `docs/highscore.md` führen; Wortlaut „Europa nur bei DEB-Sieger“.
- **Dateigrenzen:** `Core/Highscore.cs`, `UI/Screens/Statistics/HighscoreScreen.cs`, `docs/highscore.md`, Tests
  `Highscore*`; `CompetitionWidgets.cs` nur minimal und begründet.
- **Modell:** Claude Sonnet 5.5 (klein).
- **Status:** umgesetzt, wartet auf Review (PR #120, Base `a09c81e`); H2-01 behoben, H2-02 getestet, H2-04 als `[APPROX]` geführt.

### Spur `debpokal-auslosung` – Live-Auslosung im Spieltagsablauf (#116)

- **Ziel:** Die DEB-Pokal-Auslosung wie im Original im Tagesablauf zeigen; seit #111 ist `DebCupDrawScreen` im Spiel nicht
  mehr erreichbar, `CupDrawStep` hat keinen UI-Verbraucher.
- **Belege:** `docs/wettbewerbe.md` („Offen: Live-Auslosung mit Dialog ‚Sind Ihre Nerven stark genug …‘ nach dem dritten
  Termin jeder Runde“); Zeitpunkt der Auslosung im Original nicht geprüft (Remake: Saisonbeginn, Restlücke Runde 3);
  em.exe (Dialogtext, Aufrufer von 0xE8:0x75 und der Auslosung); Referenzvideos (`docs/referenz-videos.md`).
- **Abnahme:** (1) Zeitpunkt und Ablauf der Auslosung aus em.exe belegt (`[ORIGINAL]` mit Fundstelle) und umgesetzt;
  (2) Dialog und Auslosungsbildschirm im Spieltagsablauf, Antwort „nein“ überspringt die Anzeige ohne anderes Ergebnis;
  (3) Regressionstest: Auslosung nach dem dritten Termin erscheint genau einmal je Runde, Paarungen gleich mit und ohne
  Anzeige; (4) Mehrsaisonprobe (4 Wechsel) mit unveränderten Pokal-Referenzwerten bzw. begründeter Änderung;
  (5) Bildschirmfoto-Ablauf.
- **Dateigrenzen:** `Core/CupsDeb.cs`, `UI/Screens/Competitions/DebCupDraw*.cs`, `UI/Screens/MatchDayScreen.cs` bzw.
  der Tagesablauf (minimal, begründet), `Core/GameState.Integration.cs` nur falls nötig, `Tools/Scenarios/` (eigene Datei),
  `docs/wettbewerbe.md` (Abschnitt DEB-Pokal).
- **Modell:** Claude Opus 5.5.
- **Status:** PR #119 zum Review (Claude Code, 29.10.2026): Auslosung beim Spielstart, Saisonwechsel und nach dem dritten
  Termin mit Frage; Endspiel nur Übersicht. Restlücken im PR (Kugel-Animation, Tastenbelegung der Frage, Endspiel-Aufdecken).

### Spur `taktik-verein` – Taktik-Vorbelegung, 22er-Grenze, Farbe „Eingesetzt“ (Befunde aus #103)

- **Ziel:** die in `docs/wettbewerbe.md` („Aufstellung und Taktik des Nationalteams“) festgehaltenen Befunde für Verein und
  Nationalteam gemeinsam klären.
- **Befunde:** (a) Taktik-Werte im Video (Block 1 Einsatz 60/0/25/20/30/0, Angriff 0/4/5/6/7/4) stehen in DGROUP
  0x4911–0x4925 und widersprechen dem angenommenen Wertebereich −3 … +3 der Vereinstaktik; das Remake zeigt beim
  Nationalteam alles 0. (b) Vereinsaufstellung zeichnet „Eingesetzt“ in Farbe 17 statt 16. (c) Vereinsaufstellung kennt
  die Grenze von 22 Eingesetzten nicht.
- **Abnahme:** (1) Bedeutung, Wertebereich und Vorbelegung der Taktikwerte aus em.exe belegt; Verein und Nationalteam
  zeigen die Vorbelegung des Originals, Pixelvergleich Taktik Nationalteam mit Videobild 00058–00059 bzw. ein
  Vereinsbild aus `oberflaeche`; (2) Farbe und 22er-Grenze mit Regressionstest (rot → grün) und Fundstelle, oder
  begründet als Original-Verhalten belegt; (3) Auswirkung auf die Stärke (Test) und Spielstand (Schema/Migration nur bei
  Formatänderung); (4) Mehrsaisonprobe, falls die Taktik die Spielstärke ändert.
- **Dateigrenzen:** `Core/Squad.Tactics.cs`, `Core/Squad.Lineup.cs`, `Core/CupsNational.Lineup.cs`,
  `UI/Screens/Team/TacticsScreen.cs`, `UI/Screens/Team/LineupScreen.cs`, `UI/Screens/Competitions/NationalLineupScreen.cs`,
  `Core/SaveGame.Migration*.cs` (neue Datei), `docs/wettbewerbe.md` (Abschnitt Aufstellung/Taktik).
- **Modell:** Claude Opus 5.5.
- **Status:** PR #121 (zur Prüfung): Taktikwerte nach em.exe (Einsatz 0–100 %, Angriff 0–7, Vorbelegung der Automatik), Vereinsaufstellung mit 22 Eingesetzten und Farbe 16, Schema 5. Offen: Spielvorbereitung je Paarung (Automatik gegen den echten Gegner) erst mit `livespiel`.

### Spur `finanzen-nachpruefung` – Monatsausgaben Wolfsburg, unabhängige Nachdisassembly

- **Ziel:** die Restlücke aus Runde 3 klären und die `[ORIGINAL]`-Fundstellen der Finanzen von einem anderen Agenten als
  dem ursprünglichen Bearbeiter (Claude) nachprüfen lassen.
- **Befunde:** Karriere-Video ESC Wolfsburg (Level 3, Gehälter 20.710 DM) zeigt 87.370 DM Monatsausgaben, die Formel
  (`0x3E811`) ergibt 73.977 DM; Differenz 13.393 DM (40.180 Drittel) ungeklärt (`docs/finanzen-rekonstruktion.md`).
- **Abnahme:** (1) Differenz erklärt und behoben (roter Test aus dem Videowert) **oder** begründet als nicht belegbar
  (z. B. andere Programmversion) dokumentiert; (2) jede `[ORIGINAL]`-Fundstelle in `Core/Finance*.cs` gegen em.exe
  nachgeprüft, Ergebnis als Tabelle in `docs/finanzen-rekonstruktion.md` (bestätigt / abweichend / nicht prüfbar);
  Abweichungen mit rotem Test behoben; (3) Mehrsaisonprobe (4 Wechsel, passiv und aktiv) – Referenzwerte unverändert
  oder jede Änderung begründet.
- **Dateigrenzen:** `Core/Finance*.cs` (außer `FinanceSponsorRating.cs` nur lesen), `docs/finanzen-rekonstruktion.md`,
  Tests `Finance*`.
- **Modell:** ChatGPT **GPT-6.1 Terra** (Codex) – Unabhängigkeit vom ursprünglichen Bearbeiter; ersatzweise Claude Opus 5.5
  in einer frischen Sitzung.
- **Status:** PR #124 bereit zum Review (Claude Code, übernommen von ChatGPT). Abnahme (1) Wolfsburg erklärt
  (Gehälter nach Vertragsverhandlung), (2) Tabelle vollständig, sechs Abweichungen mit roten Tests behoben,
  Rest als #132–#136 für eine Folgespur, (3) Mehrsaisonprobe grün, geänderte Referenzwerte begründet.

### Spur `bundestrainer-modus4` – Bundestrainer im Modus 4 (Liga + Weltkugel)

- **Ziel:** das Verhalten des Bundestrainers im Spielmodus 4 aus dem Original belegen und umsetzen (bisher
  Referenzwerte Modus 4 nur „unverändert“ gesichert, `Mode4_ReferenceValues_Unchanged`).
- **Belege:** em.exe (Ernennung, Menü des Bundestrainers, Kachelsperren in Modus 1 vs. 4), Reihe `bundestrainer`
  (`docs/referenz-videos.md`), `docs/wettbewerbe.md` (Spielmodus „nur Weltkugel“).
- **Abnahme:** (1) Ernennung, Menü und WM-Ablauf im Modus 4 aus em.exe belegt und umgesetzt, Unterschiede zu Modus 1
  dokumentiert; (2) Regressionstest je belegtem Unterschied; (3) Mehrsaisonprobe Modus 4 (4 Wechsel, passiv/aktiv),
  Änderungen der Referenzwerte begründet; (4) Spielstand: neuer Zustand mit Schema/Migration; (5) Bildschirmfoto-Ablauf.
- **Dateigrenzen:** `Core/CupsNationalMode.cs`, `Core/CupsNational.GameState.cs`, `UI/Screens/Competitions/NationalMenu.cs`
  (`Open`/`IsLocked`), `UI/Screens/Competitions/NationalSquadScreen.cs`, `Core/SaveGame.Migration*.cs` (neue Datei),
  `Tools/Scenarios/` (eigene Datei), `docs/wettbewerbe.md` (Abschnitt Weltkugel/Bundestrainer).
- **Modell:** Claude Opus 5.5.
- **Status:** zum Review (#123): Ernennung am 13. April aus den ersten fünf der 1. Liga, Zug „Manager 5“ an WM-Tagen, Schema 5; Referenzwerte Modus 4 begründet neu aufgenommen.

### Spur `paarungen-liga` – Kacheln „Paarungen 1. Liga/2. Liga/Oberliga“ (Paket `wettbewerbe` PR 8)

- **Ziel:** die drei Platzhalter-Kacheln schließen. Kein Videobild vorhanden → Aufbau aus em.exe, `[APPROX]` wo nötig.
- **Belege:** em.exe (Aufrufer der drei Kacheln, Textbausteine); die Beschreibung in `docs/anforderungen/bildschirme.md`
  stammt aus dem früheren Portierungsversuch und gilt nicht als Beleg (`docs/wettbewerbe.md`).
- **Abnahme:** (1) Kachel → Bildschirm aus em.exe belegt (Stub, Layout, Inhalt); (2) Inhalt aus dem Spielplan
  (`Season*`), Test gegen einen nachgestellten Spieltag; (3) Öffnen zustandsrein (Test); (4) Bildschirmfoto-Ablauf,
  Pixelvergleich entfällt mangels Videobild.
- **Dateigrenzen:** neue Datei(en) `UI/Screens/Competitions/LeaguePairings*.cs` mit `[MenuEntry]`, `Tools/Scenarios/`
  (eigene Datei), `docs/wettbewerbe.md` (Abschnitt Paarungen), Tests `LeaguePairings*`; `Core/Season*.cs` nur lesen.
- **Modell:** Claude Sonnet 5.5.
- **Status:** umgesetzt, wartet auf Review (PR #122, Branch `paket/paarungen-liga`, Base `a09c81e`). Restlücken: Play-Off/Aufstieg-Seiten, Vereinsinfo beim Klick, gemerkte Seite; Spielplan und Stärken [APPROX].

### Integration Runde 5 (29.10.2026)

Branch `integration/runde-5` von `main` `a09c81e`, Leitstelle Claude Code. Zusammengeführt: #120 (`highscore-nacharbeit`,
Head `4432bcc`), #122 (`paarungen-liga`, `9b3b13b`), #119 (`debpokal-auslosung`, `7246952`), #121 (`taktik-verein`,
`9cf2c79`), #123 (`bundestrainer-modus4`, `853a077`). **Nicht enthalten:** #124 (`finanzen-nachpruefung`, Draft, laut
Übergabe unvollständig).

- Konflikt #121 × #123: beide reichten Migration 4 → 5 als `SaveGame.Migration4.cs` ein. Gelöst: `taktik-verein` bleibt
  4 → 5, `bundestrainer-modus4` wird 5 → 6 (`SaveGame.Migration5.cs`), `SchemaVersion` = 6, Schemaabdruck nachgeführt;
  `NationalCoachMode4Tests.SchemaFourSave_IsMigrated` prüft nun Schema 5 → 6, `ClubDressedTests.SchemaFourSave_IsMigrated`
  die Kette 4 → 5 → 6 (ohne `_nationalTurn`). Tabelle in `docs/formate/spielstand.md` ergänzt.
- Übrige Berührungen (`docs/arbeitspakete.md`, `docs/wettbewerbe.md`, `RngTwinProbeTests.cs`) ohne Konflikt.
- Zusammenspiel neu getestet (`IntegrationRunde5Tests`, Originaldaten): eine Saison im Modus 4 über die Oberfläche mit
  Live-Auslosung (12 Anzeigen), Zug des Bundestrainers mit Startwerten der Taktik nach em.exe, zustandsreinem Öffnen der
  Taktik und bytegleichem Sichern/Laden im Zug und nach dem Saisonwechsel. Grün, kein weiterer Integrationsfehler.
- Bildschirmfotos `debpokal-auslosung paarungen-liga bundestrainer-modus4 highscore nationalteam-2 tabellen-pokale
  wettbewerbe weltkugel`: ohne Fehler; alle 36 Bilder der Spurabläufe bytegleich mit dem Stand der jeweiligen Spur.
- Review [integration-runde-5](reviews/2026-10-29-integration-runde-5.md): **R5-01 (P2, #130)** – Schema 5 aus der Spur
  `bundestrainer-modus4` (mit `_nationalTurn`, ohne `_dressed`) ließ sich nicht laden. Behoben: Migration 5 → 6 holt
  für diese Form den Schritt 4 → 5 nach, offener Zug bleibt; Regressionstest `IntegrationRunde5MigrationTests` (rot → grün).
- Neue Befunde aus #123: #125 (Statusbit 0x40 am Saisonende), #126 (Highscore-Schlüssel 0x7530 im Original unerreichbar),
  beide P3.
