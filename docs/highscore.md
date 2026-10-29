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
- **Punkte:** Stadionstufen × Originalpreise/1000, Plätze, Vermögen/2000 (≤ 50 Mio), Tabellenplatz 25·(36−Platz−12·Liga),
  Saisonhistorie, Titel 8/6/10.

## [APPROX]
- Nicht zugeordnete Formelterme (0x100:0x57, 9741-Bytes-Feld, 0x138:0x2A, Kaderwerte) fehlen – Punkte weichen ab.
- Stempel = Sitzungsstempel; Level/Liga/Stärke des Schlüssels vom ersten Manager.
- Der Strafterm (36−0x80)·7 beim Meistercode ist wörtlich übernommen; Sinn ungeprüft.
- Speicherort: Ordner „Bestenliste“ neben dem Benutzerordner der Speicherstände.
