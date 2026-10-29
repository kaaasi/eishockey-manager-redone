# Highscore (Bestenliste)

Spur `tabellen-highscore`, Teil 2. Code: `Core/Highscore.cs`, `UI/Screens/Statistics/HighscoreScreen.cs`.
Belege aus `em.exe` (Datei-Offsets; Overlay-Stub Highscore = 0x140:0x7A).

## [ORIGINAL]
- **Datei:** je Schlüssel eine Datei `h.<Schlüssel>`, 8 Einträge × 58 Byte = 464 Byte (Datei 0x5D617). Eintrag: Name 26 Byte
  (0-terminiert), Verein ab 26, Titel A/B/C ab 49, Punkte (Int16) ab 52, Stempel (UInt32) ab 54.
- **Einfügen** (0x5D73D): absteigend nach Punkten, bei Gleichstand hinter dem Vorhandenen; gleicher Name und Verein
  ersetzt den alten Eintrag; zu schwache Einträge fallen heraus. Eigenheit bei 0x5D7BB: Ein gleichnamiger Eintrag wird auch
  mit weniger Punkten überschrieben (Test `Insert_SameNameWithFewerPoints_OverwritesBetterEntry`).
- **Schlüssel** (0x28A2C): Level + 4 · Dauer + 32 · Startliga + 128 · Startstärke − 2; keine Liste bei Level ≤ 2 (Kachel
  gesperrt, 0x44FCE). Nur Weltkugel: Schlüssel 30000.
- **Eintragen:** beim Öffnen der Kachel und bei jedem Saisonwechsel; am Spielende erscheint die Liste vor dem Titel (0x5642E).
- **Layout:** Hintergrund kb\195, Titel Grundlinie 15, Zeilen ab 154 im Abstand 10, x = 56 / 115 / 207 / 240; am Bild
  oberflaeche_0848 pixelgenau geprüft.
- **Punkte** (Funktion 0x5D04F, `HighscoreRules.Points`), durch 20 geteilt, mindestens 1:
  Σ Bewertung·100 (Werbebewertung 0x100:0x57, `SponsorRating`); Stadion (Stufe·Preis/1000, Plätze·540000 und
  Stehplätze·405000 als **vorzeichenlose 32-Bit-Zahl**/10⁶ – das Produkt läuft über, 0x483 ist unsigned LDIV);
  Vermögen (Drittel) min(·, 50 Mio)/2000; 25·max(0, 36−Platz−Versatz); 150·v je Pokalbyte 1–6; Mittel der Saisoncodes
  ((36−Ligacode)·7, DEB-Code: Sieger 80 sonst 10·(Code&7); Europa nur bei DEB-Sieger bzw. ≥ 8: Sieger 130 sonst 20·(Code&7)),
  dazu 8·Meister + 6·Pokal + 10·Europa.
- **Beleg:** eigener 8086-Emulator lief die Originalfunktion gegen das Modell, 14 Fälle als Goldtest
  (`HighscoreTests.Points_MatchOriginalFunction`), 500 Zufallsfälle zusätzlich gleich. Die Emulator-Skripte liegen nicht im Repo.

## [APPROX]
- Spielerwerte (Funktion 0x49D36) als Kaufpreis × 3/2 nachgebildet.
- Europapokal-Stufe auf Pokalbyte 1 abgebildet, Pokalbytes 2–3 und Meisterschafts-Bracketcode 0x35 nicht belegt.
- Stempel = Sitzungsstempel; Level/Liga/Stärke des Schlüssels vom ersten Manager.
- Nur Weltkugel (Schlüssel 30000) wird mit den vereinfachten Werten gewertet.
- Speicherort: Ordner „Bestenliste“ neben dem Benutzerordner der Speicherstände.
