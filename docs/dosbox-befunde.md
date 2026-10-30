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
