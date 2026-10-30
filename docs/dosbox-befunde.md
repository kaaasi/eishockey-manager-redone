# Befunde aus dem Original unter DOSBox

Der Nutzer kann das Original (deutsche DOS-Version, DOSBox 0.74-3, 3000 Zyklen) jetzt selbst ausführen. Die
Bildschirmfotos liegen im privaten Referenz-Repo unter `dosbox/nur-weltkugel/` (laden mit
`tools/claude/fetch-reference.sh dosbox`), Dateiname `NN_<inhalt>.png`. Es sind **Fensterfotos** (ca. 640 × 505 mit
DOSBox-Titelleiste, skaliert) – gut für Inhalt und Abläufe, für Pixelvergleiche nur eingeschränkt; verlustfreie
320 × 240-Bilder entstehen mit Strg+F5 (Ordner `capture`).

## Spiel vom 30.10.2026: Modus „nur Weltkugel“ bis vor das erste WM-Spiel

Ein Manager („Joerg“), Kopf und Verein Düsseldorfer EG gewählt, Start-Liga Bundesliga, Start-Stärke oberes Drittel
(Level und Dauer wie auf 04/05). Bildnummern = Dateien in `dosbox/nur-weltkugel/`.

### Spielvorbereitung (01–05)

- „nur Weltkugel“ = Fahnenknopf durchgestrichen, Weltkugel an (01, 02, 04, 05).
- 03: beide Modusknöpfe aus; statt des Notizbuchs (Laden) erscheint unten links ein Schläger mit Puck. Passt zu „ohne
  Knopf nimmt ‚Los jetzt!‘ keinen Klick an“ (0x295DF); was das Symbol bedeutet, ist nicht geklärt.

### Nach „Los jetzt!“ (06–18)

- **Halt am Samstag, 14. April 1994** vor dem ersten WM-Spiel, Hauptmenü des Bundestrainers mit Fahne statt Wappen,
  Gegner Russland, „Noch keine“, Konto 500.000 DM (06). Bestätigt das bisherige `[APPROX]` „Halt nach dem Sprung“.
- **Menü des Bundestrainers schon vor der Ernennung:** Statistiken, Tabellen, Paarungen je fünf gesperrt + „WM“ offen
  (08–10); Gemischtes Laden/Sichern/Einst. + drei leere Plätze (11). Mannschaft und Stärken sind vor der Ernennung
  **gesperrt** (Angabe des Nutzers, kein Bild). Bestätigt das `[APPROX]` „Menü des Bundestrainers schon vor der
  Ernennung, Mannschaftskacheln bis dahin gesperrt“.
- **Abweichung:** In der Reihe Manager ist vor der Ernennung **„Highscore“ offen** (07), die übrigen fünf gesperrt. Laut
  Sperrtabelle DGROUP 0x5159 (Teil Nationalteam) und Video `bundestrainer_00042` ist die ganze Reihe gesperrt. Offen: ob
  die Sperrtabelle erst ab der Ernennung gilt (DGROUP 0xD0BA) – Reihe Manager nach der Ernennung noch aufnehmen.
- Highscore (19): Liste mit „Joerg – DEUTSCHLAND – 1“; bestätigt die Bestenliste mit Schlüssel 30000 im Modus
  „nur Weltkugel“ (`docs/highscore.md`).
- Tabellen → WM: „Gruppe 1/2“ mit 0:0-Werten (12, 13); Klick auf ein Land öffnet das Gegner-Fenster (14–16, Knopf
  unten rechts mit wechselndem Symbol je nach Mausposition, „Reicht mir“).
- Paarungen → WM: „Gruppe 1 1.Spieltag“ mit 0:0 (20, 21), Knöpfe ←, „Endrunde“, „Gruppe 2“, →; Pfeile blättern die
  Spieltage. **„Endrunde“ vor den Spielen:** Fenster „WM-Endrundenspiele – Die Paarungen stehen noch nicht fest…“ mit
  Knopf „Gruppe“ (24) – bisher nur als `[APPROX]` gebaut.
- Statistiken → WM-Statistik vor dem ersten Spiel: acht Zeilen „0 P.“, darunter alle zwölf Flaggen (17).
- „Spieler in WM-Kader“ vor (18) und nach „auto“ (22, 23): Gewählte stehen mit Verein „DEUTSCHLAND“ in heller Schrift;
  „−“/„+“ blättern; Kasten „DEUTSCHLAND“ zeigt danach Abwehr 91/88/88, Sturm 94/88/87.
- Nach „auto“ werden die Lampen in der Infoleiste alle grün (30; vorher die erste rot).

### Nationalteam nach „auto“ (25–29)

- Aufstellung: 22 eingesetzt, drei Reihen und TW belegt, Energie E 56–79 (25).
- **Taktik: alle Werte 0**, Einsatz/Angriff „AUTOM.“, Wechsel „MANUELL“, Reihen „Automatisch“ (26). Die Werte
  60/0/25/20/30/0 bzw. 0/4/5/6/7/4 aus `bundestrainer_00058` stehen zu Spielbeginn also noch nicht – die Grundtaktik des
  Remakes (alles 0) passt zum Anfangszustand.
- Stärken Länderteams Gesamt/Abwehr/Sturm (27–29): Deutschland 88/90/52 → 77 (Platz 6).

### „WM Vorbereitung“ und Freundschaftsspiele (31–45)

- Leer mit Knopf „Heim“ (31); Einfachklick auf ein Land zeigt Gegner, Kosten, Kontostand (USA 100.000, Kanada 120.000,
  Schweden 110.000 DM; 32–34) und darunter den Reihenvergleich beider Teams (35); **Doppelklick startet das Spiel**.
- Ablauf: Banner „Freundschaftsspiel“ (36), „Spiele laufen“ mit Kasten unten links (37), **kein Live-Spiel** (Einstellung
  „WM-Spiele“ an, Torszenen aus); Ergebnis in der Liste rechts, Heimteam rot (38: 3:1 gegen Schweden).
- Nach drei Spielen (3:1 Schweden, 1:3 Finnland, 14:1 Österreich; 39):
  - Konto 500.000 → 260.000 DM (40), **Datum bleibt 14. April** – Freundschaftsspiele lassen keine Zeit vergehen.
  - Deutschland in den Stärken 88/90/52 → **91/93/57** (Mittel 77 → 80, Platz 3), Abwehr und Sturm ebenso (+3 Kondition,
    +3 Technik, +5 Form; 41–43). Andere Länder unverändert.
  - Kader: SP 3, Tore und Strafminuten gefüllt, Spielerwerte gestiegen, **Energie E bei allen 46** (45; vorher 56–79).
    Zu vergleichen mit `[APPROX]` „Energie 70 beim Öffnen von ‚WM Vorbereitung‘ nicht übernommen“ (`docs/wettbewerbe.md`).
- Nach der Ernennung Reihe Mannschaft: Aufst., Taktik, gesperrt, Transfer, Training, leer (40) – wie im WM-Video.
- Info-Fenster aus den Stärken (44) hat nur „Reicht mir“, das Gegner-Fenster der Infoleiste zusätzlich den Knopf rechts.

### Gemischtes (46–51)

- Laden/Speichern (46, 47): Dateiliste in drei Spalten („.“, „..“, HIGH, KB, TORE, EM.EXE, INFOFILE), „VERZEICHNIS:
  C:\EM“, „FILENAME :“, Laufwerksknöpfe A:–G:, „LÖSCHEN“.
- **Einstellungen** (48–51): Kopfzeile „V1.00 – Ende: Nie. (Level 1)“ und „RAM: 307872 Bytes frei.“; Torszenen,
  Hintergrundgrafik, Scrolling (Pinguin = an, Loch = aus), Spielgeschwindigkeit (acht Pinguine); je Liga (1./2./3. Liga)
  Drittelpause, Ergebnisse, Tabelle; DEB-Pokal, Europapokal, WM-Spiele. **Torszenen hat drei Stufen:** kein Pinguin,
  Pinguin, Pinguin mit rotem Punkt (50, 51; laut Nutzer keine Animation). Bedeutung der dritten Stufe offen (em.exe).
  Das Remake hat einen eigenen Einstellungsbildschirm (`docs/anforderungen/bildschirme.md`), Torszenen nur an/aus.

### Offen

- Zeitpunkt und Auslöser der Ernennung (Meldung?); Reihe Manager und Stärken nach der Ernennung.
- Erster WM-Spieltag: Banner „Weltmeisterschaft“, Live-Spiel mit Torszene, „Gruppe 60. Min.“, Endstand, Tabelle,
  Hauptmenü am Folgetag.
- Bedeutung der Torszenen-Stufe 3; Symbol bei Spielvorbereitung ohne Modus (03).

## Status der Folgeaufgaben (Spur `weltkugel-dosbox`, Runde 7)

Tests: `tests/EishockeyManager.UI.Tests/WeltkugelDosboxTests.cs` (Bildnamen im Testkommentar).

| Befund | Ergebnis | Stand |
|---|---|---|
| „Highscore“ vor der Ernennung offen (07) | em.exe Datei 0x45020–0x4504A: Platz 6 der Reihe Manager wird beim Aufbau mit „DGROUP 0x256 = 0 oder (DGROUP 0xA70E = 5 und Modus 4)“ überschrieben; die Ernennung (0xD0BA) fragt der Aufbau nicht ab. Modus 1 hat immer Schlüssel 30000 → offen vor und nach der Ernennung; Modus 4 gesperrt. Der Kommentar zu 0x289A3 in `NationalMenu.cs` war falsch (0x2899D prüft vorher „nur Weltkugel“) | behoben (`NationalMenu.CoachHighscoreLocked`) |
| Video `bundestrainer_00042` zeigt „Highscore“ gesperrt (Saison 2008) | mit em.exe nur bei DGROUP 0x256 = 0 erklärbar (z. B. andere Programmversion oder ein Spielstand, der 0x256 = 0 trägt – 0x256 wird mitgesichert, 0x5BF64/0x5C948); nicht prüfbar | offen |
| „WM-Endrundenspiele“ vor den Spielen (24) | em.exe Datei 0x3337B–0x333C8: Wettbewerb 3 und WM-Stufe 0x7AE &lt; 5 → „Die Paarungen stehen noch nicht fest...“ (Zeiger DGROUP 0x924), Flow, Grundlinie Oberkante + 34, Farbe 30; Rahmen 5 Teile ab y = 70 | behoben (`WorldCupKnockoutScreen`) |
| „Paarungen → WM“ = eigene Ansicht „Gruppe n  m.Spieltag“ mit Flaggen und Knöpfen ←, „Endrunde“, „Gruppe 2“, → (20, 21) | im Remake öffnet die Kachel die Gruppentabelle (wie „Tabellen → WM“, 12, 13); dadurch ist das Fenster aus 24 vor der Endrunde nicht erreichbar. Neuer Bildschirm außerhalb der Dateigrenzen (`WorldCupScreen.cs`) | offen – Vorschlag eigenes Paket |
| Freundschaftsspiele: kein Zeitfortschritt, Kosten (32–34, 39, 40) | Datum bleibt, Konto sinkt um die Kosten (Heim: USA 100.000, Kanada 120.000, Schweden 110.000 DM) | war schon richtig; Regressionstest |
| Deutschland in den Stärken +3/+3/+5 nach drei Spielen, andere Länder unverändert (27 → 41) | Die Werte nach „auto“ (88/90/52) liegen außerhalb der Würfelbereiche (Technik 79–89) → das Original rechnet die Teamwerte Deutschlands aus der Aufstellung. Kandidat: Datei 0x952C (Teamwerte je Manager-Verein: Mittel der drei Reihen, Felder 0x18–0x1D, Reihenwert 0x977F); Aufrufer über Overlay-Stubs nicht aufgelöst. Der Bonus (0x3C18C) wirkt auf die Kaderplätze, daraus folgt die Änderung der Teamwerte. Im Remake zeigen die Stärken für Deutschland weiter Tabelle/Würfelwerte | offen – braucht die Reihenwerte (0x977F), Überschneidung mit `spielberechnung` |
| Energie aller Eingesetzten 46 nach drei Spielen (25 → 45) | Öffnen von „WM Vorbereitung“ setzt Kaderfeld 0x13 aller belegten Plätze auf 70 (Datei 0x3BB51, `mov byte [bx+0x5DD6],0x46`); 70 − 3 · 8 = 46 passt zu einem festen Abzug je Spiel in der Spielberechnung (0xE8:0x39, nicht gelesen). Setzen beim Öffnen verbietet die Architekturregel („Bildschirme ändern beim Öffnen nichts“) | offen – Entscheidung nötig, Abzug gehört zu `spielberechnung` |
| Taktik des Nationalteams zu Beginn alle 0 (26) | Die Automatik (Datei 0x8365) schreibt Einsatz/Angriff nur **vor einem Spiel** in den Vereinssatz (Felder 0x26–0x2D), wenn der Verein keinen Manager hat oder dessen Modus „AUTOM.“ ist; Aufrufe nur aus dem Spieltagsablauf je Paarung (Datei 0x36D12/0x36D49 Liga, 0x36F27/0x36F5A Wettbewerbe, Tabelle DGROUP 0x5CFB). Es gibt keinen Start- oder Ernennungswert und keine Unterscheidung nach Modus: Bis zum ersten Spiel des Teams stehen 0 – im Modus 1 wie im Modus 4. Das Video `bundestrainer_00058` (60/0/25/20/30/0) stammt aus einer späteren Saison (Werte des letzten Spiels bleiben stehen). Umsetzung braucht eine Änderung in `Squad.Tactics.cs` (Anzeige „AUTOM.“, Spur `taktik-verein`) und Zustand über den Saisonwechsel (Schema) | offen – Entscheidung Leitstelle |
| Torszenen-Stufe 3, Symbol ohne Modus (03, 50, 51) | nicht untersucht | offen |
