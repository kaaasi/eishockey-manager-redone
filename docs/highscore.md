# Highscore (Bestenliste)

Spur `tabellen-highscore`, Teil 2. Code: `Core/Highscore.cs`, `UI/Screens/Statistics/HighscoreScreen.cs`.
Belege aus `em.exe` (Datei-Offsets; Overlay-Stub Highscore = 0x140:0x7A).

## [ORIGINAL]
- **Datei:** je Schlüssel eine Datei `h.<Schlüssel>`, 8 Einträge × 58 Byte = 464 Byte (Datei 0x5D617). Eintrag: Name 26 Byte
  (0-terminiert), Verein ab 26, Titel A/B/C ab 49, Punkte (Int16) ab 52, Stempel (UInt32) ab 54.
- **Einfügen** (0x5D73D): absteigend nach Punkten, bei Gleichstand hinter dem Vorhandenen; gleicher Name und Verein
  ersetzt den alten Eintrag; zu schwache Einträge fallen heraus. Eigenheit bei 0x5D7BB: Ein gleichnamiger Eintrag wird auch
  mit weniger Punkten überschrieben (Test `Insert_SameNameWithFewerPoints_OverwritesBetterEntry`).
- **Schlüssel** (Datei 0x2899D–0x28A69, Variable `[bp-0x57]` = Knopfbits, Bit 0 Fahne, Bit 1 Weltkugel, dieselbe wie beim Modus
  0x29838): Zuerst die Zulässigkeit `[bp-0x42]` (0x2899D–0x289CC):
  1. Knopfbits = 2 („nur Weltkugel“) → **zulässig**, ohne Blick auf Level, Dauer, Liga oder Stärke (0x2899D `cmp [bp-0x57],2`,
     0x289A1 `jz 0x289C5`);
  2. sonst ohne Fahne (Bit 0) → nicht zulässig (0x289A3);
  3. Level ≤ 2 → nicht zulässig (0x289A9);
  4. Startstärke = Mittelfeld → zulässig (0x289B0), sonst nur bei Dauer = eine Saison **und** Startliga = Bundesliga (0x289B7).

  Danach `[0x256]` (DGROUP): nicht zulässig → 0, keine Liste (0x28A2C; Kachel gesperrt, 0x44FCE); Knopfbits = 2 → **0x7530 = 30000**
  (0x28A34/0x28A3A); sonst Level + 4 · Dauer + 32 · Startliga + 128 · Startstärke − 2 (0x28A42). Test `Key_FromSetup` prüft jede Hälfte der
  Bedingung einzeln, `HighscoreKeyOriginalTests` alle Kombinationen gegen diesen Ablauf.
- **Befund #126 trifft nicht zu.** Das Issue las die Bedingung ab 0x289A3 („verlangt Bit 0“) und übersah den Vergleich mit 2 bei 0x2899D
  davor: Der Zweig `Knopfbits = 2 → 0x7530` ist erreichbar, im Modus „nur Weltkugel“ gibt es also eine Bestenliste (Datei `h.30000`).
  Eintragen (0x5D4C2), Einfügen (0x5D618) und Anzeigen (0x5D90D) kehren nur bei `[0x256] = 0` sofort zurück (Vergleiche 0x5D4CE,
  0x5D622, 0x5D915), bei 0x7530 arbeiten sie normal; der Aufruf am Spielende (0x5642E) prüft den Schlüssel nicht selbst. Dass die Kachel „Highscore“ im Video `bundestrainer_00042/00220/00418` gesperrt ist, erklärt
  die Sperrtabelle des Bundestrainers (Reihe Manager komplett gesperrt, zweiter Teil der Tabelle DGROUP 0x5159, siehe `NationalMenu`),
  nicht der Schlüssel 0. Das Remake war von Anfang an richtig (`HighscoreRules.Key`, `Key_WorldOnly_Is30000`), es wurde nichts geändert.
- **Eintragen:** beim Öffnen der Kachel und bei jedem Saisonwechsel; am Spielende erscheint die Liste vor dem Titel (0x5642E).
- **Layout:** Hintergrund kb\195, Titel Grundlinie 15, Zeilen ab 154 im Abstand 10, x = 56 / 115 / 207 / 240. Zeilen am Bild
  oberflaeche_0848 geprüft (Abweichung im Rahmen des Videoversatzes von etwa −0,25 px). Titel: Ausdehnung der Titelpixel
  x = 117 … 202 wie im Video (oberflaeche_0844–0855), Rundung der Zentrierung siehe [APPROX]; automatisch geprüft in
  `HighscoreScreenTests.Title_PixelExtent_MatchesVideo`.
- **Punkte** (Funktion 0x5D04F, `HighscoreRules.Points`), durch 20 geteilt, mindestens 1:
  Σ Bewertung·100 (Werbebewertung 0x100:0x57, `SponsorRating`); Stadion (Stufe·Preis/1000, Plätze·540000 und
  Stehplätze·405000 als **vorzeichenlose 32-Bit-Zahl**/10⁶ – das Produkt läuft über, 0x483 ist unsigned LDIV);
  Vermögen (Drittel) min(·, 50 Mio)/2000; 25·max(0, 36−Platz−Versatz); 150·v je Pokalbyte 1–6; Mittel der Saisoncodes
  ((36−Ligacode)·7, DEB-Code: Sieger 80 sonst 10·(Code&7); Europa nur bei DEB-Sieger: Sieger 130 sonst 20·(Code&7); ein
  Zweig „≥ 8“ ist nach der Maskierung `& 7` bei 0x5D41D unerreichbar),
  dazu 8·Meister + 6·Pokal + 10·Europa.
- **Beleg:** eigener 8086-Emulator lief die Originalfunktion gegen das Modell, 14 Fälle als Goldtest
  (`HighscoreTests.Points_MatchOriginalFunction`), 500 Zufallsfälle zusätzlich gleich. Die Emulator-Skripte liegen nicht im Repo.

## [APPROX]
- Spielerwerte (Funktion 0x49D36) als Kaufpreis × 3/2 nachgebildet.
- Europapokal-Stufe auf Pokalbyte 1 abgebildet, Pokalbytes 2–3 und Meisterschafts-Bracketcode 0x35 nicht belegt.
- Stempel = Sitzungsstempel; Level/Liga/Stärke des Schlüssels vom ersten Manager.
- Nur Weltkugel (Schlüssel 30000) wird mit den vereinfachten Werten gewertet.
- Speicherort: Ordner „Bestenliste“ neben dem Benutzerordner der Speicherstände.
- Titel „Highscore“: Der Originalaufruf (0x5D9B8–0x5D9D6) zentriert zwischen 0 und 0x140 = 320. `CompetitionWidgets.Centered` setzt
  den Titel damit 1 Pixel zu weit links (x = 116 … 201 statt 117 … 202). Korrektur nur für diesen Aufruf mit rechter Grenze 321
  (wie `DebCupDrawScreen`); die Rundungsregel des Originals bei ungerader Textbreite ist nicht belegt, `Centered` bleibt unverändert.
- Punkteformel, drei Abweichungen von `Inputs`/`Entry` (Review Runde 4 Teil 2, H2-04):
  1. **Saisonzahl:** Das Original zählt den globalen Saisonzähler (DGROUP 0x3BD, höchstens 50, Teiler + 1, 0x5D3A3), das Remake die
     Länge des Verlaufs des Managers. Gleich, außer bei entlassenen Managern (deren Verlauf endet früher).
  2. **Platzierungsterm vor dem ersten Spieltag:** 0 statt des Platzbytes des Vereins.
  3. **Titelzähler und Gesamtpunkte:** Titel aus dem Saisonverlauf statt aus den Bytes Manager + 0x15–0x17 (0x5D545–0x5D56D); die
     Gesamtpunkte sind auf 1 … 32767 begrenzt, im Original laufen sie als 16-Bit-Wert über. Die Emulator-Fälle liegen bei etwa
     3000–4000 Punkten; ob 32767 im Spiel erreichbar ist, wurde nicht geprüft.
