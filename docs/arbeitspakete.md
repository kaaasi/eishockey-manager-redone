# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 5 – Restbefunde und Zurückgestelltes vor dem Live-Spiel (ab 29.10.2026)

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
- **Status:** offen.

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
- **Status:** offen.

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
- **Status:** offen.

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

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- Statistiken Spieler, Verein, Fairplay, WM-Statistik: warten auf `livespiel` (Tore, Vorlagen, Strafen je Spieler).
- Import `*.HOK`: kein Original-Spielstand zum Nachprüfen des Formats vorhanden (`docs/formate/dateiformate.md`, „Offen“).
- Inaktive Branches `paket/speichern-ids`, `paket/wettbewerbe-weltkugel-2` (ohne PR): Löschen nur nach Entscheidung des Nutzers.

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: PR 1–7 gemergt (Runde 3); Restarbeiten in Runde 4 gemergt; PR 8 in Runde 5 (`paarungen-liga`). Regeln und Belege: [wettbewerbe.md](wettbewerbe.md)

| PR | Inhalt | Stand |
|---|---|---|
| 1 | DEB-Pokal: Auslosung, Heimrecht, „best of three“, Abrechnung, Bildschirm „DEB-Pokal Auslosung“ | gemergt (Runde 3, #54) |
| 2 | Europapokal (Gruppe + Endrunde), Bildschirm Gruppen/Endrunde | gemergt (Runde 3, #54) |
| 3 | WM (2 Gruppen + K.-o.), Bildschirm Gruppe/Tabelle WM | gemergt (Runde 3, #54) |
| 4 | Stärken (Ligen, Länderteams) | gemergt (Runde 3, #54) |
| 5 | Statistiken: Ewige Tabelle (Totale), Früher | gemergt (Runde 3, #54) |
| 6 | Bundestrainer: WM-Kader, „WM Vorbereitung“ (Freundschaftsspiele), Etat; WM: Werte Deutschlands neu ausgewürfelt, Länder 20-mal getauscht | gemergt (Runde 3, #54) |
| 7 | Spielmodus „nur Weltkugel“: Spielvorbereitung, Vereinssaison überspringen, Menü des Bundestrainers | gemergt (#87 über #89) |
| 8 | Paarungen Liga (kein Videobeleg) | Runde 5, Spur `paarungen-liga` |
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | **wartet auf `livespiel`** (Tore, Vorlagen, Strafen je Spieler) |

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: Format, Dialog und automatisches Speichern gemergt (Runde 3, [archiv/runde-3.md](archiv/runde-3.md)); #80 in Runde 4 behoben (#105)

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`) – zurückgestellt, kein Original-Spielstand vorhanden.

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
