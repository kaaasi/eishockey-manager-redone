> **Herkunft:** aus einem früheren, unvollständigen Portierungsversuch. Dient als Zielbild für die Oberfläche; Koordinaten und Abläufe werden beim Nachbau gegen `docs/referenz-videos.md` geprüft. Pfade wie `src/EishockeyManager.UI` oder `ehm-tools` beziehen sich auf den alten Versuch.

# Bildschirme
 
Die Oberfläche (`src/EishockeyManager.UI`) arbeitet wie das Original auf einem Raster von **320 × 240 Pixeln**.
Alle Koordinaten unten beziehen sich auf dieses Raster. Vorlage waren die Originalgrafiken (`kb\N.vga`) und
Bildschirmfotos aus einem Let's Play des Originals.
 
* **Klassik:** pixelgenau mit den Originalfonts aus `em.exe` (mini 6 px, flow 8 px, newbold 20 px), ganzzahlig skaliert.
* **Modern:** gleiche Anordnung in Fensterauflösung mit TrueType-Schrift und optionalen HD-Grafiken (`assets/hd`).
* Die Glyphbreiten der Originalfonts enthalten den Zeichenabstand bereits (letzte Spalte leer) – daher wird
  **ohne** zusätzlichen Abstand gesetzt. Das war die Ursache für abgeschnittene Texte in Version 0.1.
Bedienung überall: **linke Maustaste** wählt, **rechte Maustaste** oder **Esc** geht zurück ins Hauptmenü,
**Klick aufs Vereinslogo** oben rechts = nächster Tag (im Hauptmenü; die Leertaste hat im Remake keine Weiter-Funktion,
wie im Original, #205), **F2** = Sichern, **F9** = Klassik/Modern, **F11** = Vollbild.
Alle Bildschirme lassen sich ohne Fenster als PNG erzeugen: `ehm-tools screenshots <spielordner> <ausgabe> [de|en]`.
 
## Grafiken des Originals
 
| Bild | Inhalt | Verwendung |
|---|---|---|
| `kb\197` | Titelbild | Titel mit Startmenü (Erweiterung) |
| `kb\150` | Spielvorbereitung | Setup |
| `kb\1` | Setup-Elemente: Level-Figuren (6 × 10 px), Lineale für die Dauer, Liga- und Stärke-Schriftzüge, Spielmodus-Knöpfe (57 × 13), Kästen „Kabel“/„Notizbuch“ (55 × 38) | Setup |
| `kb\0` | 12 Managerköpfe, Zellen 40 × 40 im Raster 41 px (Index 30 = Hintergrund, wird blau eingefärbt) | Setup, Hauptmenü, Highscore |
| `kb\80–143` | Vereinslogos 38 × 38 | überall |
| `kb\20` | Titelleiste 320 × 22 (Index 30/254 färbt das Spiel zur Laufzeit hell) | Hauptmenü |
| `kb\22` | Sternenhintergrund mit Spieler | fast alle Fenster |
| `kb\3` | Menükacheln: groß 58 × 42 bei (1 + 59·Spalte, 1 + 43·Reihe), klein 38 × 28 bei (1 + 39·Spalte, 87 + 29·Reihe); Symbole 232–241; Index 30 = Kachelhintergrund | Hauptmenü |
| `kb\5` | Training mit Wochentagen und Leiste der 9 Trainingsarten (29 × 19 bei x = 26 + 30·i, y = 218) | Training |
| `kb\14` | Stadion (320 × 168) | Stadion |
| `kb\151` | Werbung: Bande, Eisfläche, Trikot, Angebot | Werbung |
| `kb\11` | Firmenlogos 34 × 25 (Spalte 0 Bande, 1 Fernsehen, 2 Trikot) | Werbung |
| `kb\152` | Bank und Börse mit den vier Aktien | Finanzen |
| `kb\195` | Hintergrund des Highscores (eigene Palette); früher hier als „Zeitungsfoto“ geführt – em.exe lädt kb\195 nur in Datei 0x5D90D (Ov 0x140), siehe `highscore.md`, `re-abdeckung.md` | Highscore |
| `kb\19` | Anzeigetafel und Reporter des Live-Spiels | Spiel |
| `kb\38` | Ziffern 8 × 10 der Spieluhr, Mundbewegungen des Reporters | Spiel |
| `kb\13` | Symbole (u. a. Babyflasche „Jugend“) | Jugend |
 
## Ablauf
 
**Titel → Setup → Hauptmenü.** Das Titelbild bekommt ein kleines Menü (Neues Spiel, Laden, Netzwerk, Einstellungen, Ende).
 
### Setup (`kb\150`)
* Links bis zu vier Manager (Hot-Seat): Kopf (5,40+51·i), Name/Verein (44,…), Logo (137,…).
  Kopf anklicken = nächster Kopf (rechts = vorheriger), Namensfeld = Name tippen, Logo = nächster Verein der gewählten
  Liga/Stärke (rechts = Liste aller Vereine), leeres Feld = weiterer Manager, Rechtsklick aufs Namensfeld = entfernen.
* Rechts: **Spielmodus** (DEB-Pokal an/aus, Europapokal + WM an/aus), **Spiel-Level** (Schwierigkeit 0–3),
  **Spiel-Dauer** (1, 3, 5 Saisons oder unbegrenzt), **Start-Liga**, **Start-Stärke** (oberes Drittel, Mittelfeld,
  Abstiegskampf) – jeweils mit ▲▼ (y = 79, 107, 135, 163) oder Klick ins Feld. Level, Liga und Stärke gelten je Manager.
* Unten: Notizbuch = Spielstand laden, „Los jetzt!“ = Start.
### Hauptmenü / Wochenübersicht
* Titelleiste `kb\20`, darunter die Infoleiste (1,21)–(319,75): Datum, Manager, Verein; Kasten (5,33,102×40) mit der
  Tabellenkurve (gelb = eigener Verein, weiß = nächster Gegner) bzw. „Vor dem ersten Spieltag.“; Symbole für Wettbewerb und
  Heim/Auswärts, bisherige Begegnungen („A 0:3,H 1:5“: erst auswärts, dann daheim, je zwei, neueste zuerst, über alle
  Saisons; „ Noch keine“; ohne Gegner „Spielfrei“ – em.exe 0x43E21, `docs/reverse-engineering.md`), Kontostand; sechs Warnlampen des Managers am Zug (Regeln in
  `Screens/MenuLamps.cs`, em.exe 0x42996);
  Kopf (231,33) und Logo (273,33). Klick aufs Logo (Klickfeld x 275–312, y 34–71, em.exe 0x45483) = nächster Tag
  bzw. im Hot-Seat Abgabe an den nächsten Manager.
* Große Kacheln 72 × 58 bei x = 9, 85, 161, 238 und y = 79, 137: Manager, Mannschaft, Statistiken, Tabellen,
  Paarungen, Stärken, Gemischtes, und tschüß… – „und tschüß…“ fragt „Sie haben also wirklich / etwas besseres zu tun ?
  / Sind Sie sicher ??“ (Überschrift Managername, em.exe 0x45C33); „Leider ja“ beendet das Programm, „Kein Gedanke“
  zurück (#205).
* Kleine Kacheln 47 × 41 bei x = 5, 57, 110, 162, 215, 268 und y = 199 – je nach großer Kachel:
| Große Kachel | Kleine Kacheln |
|---|---|
| Manager | Finanzen, Werbung, Stadion, Jugend, Neuer (Posteingang) |
| Mannschaft | Aufstellung, Taktik, Verträge, Transfer, Training |
| Statistiken | Früher, Verein, Spieler, Fairplay, Totale |
| Tabellen | 1. Liga, 2. Liga, Oberliga, WM, Highscore |
| Paarungen | Liga, Playoffs, DEB-Pokal, Europapokal, WM |
| Stärken | 1. Liga, 2. Liga, Oberliga, Nationalteams, Spieler |
| Gemischtes | Laden (im Netzwerk: Chat), Sichern, Einstellungen, Daten (Ex-/Import), Cheats |
 
  Die sechste kleine Kachel ist immer **Weiter** (Fisch, vor Spieltagen; Sterne sonst). Im Netzwerk zeigt eine Ampel,
  dass auf die anderen Manager gewartet wird. Vor eigenen Spielen erscheint das Banner („Ligaspiel“, „Pokalspiel“ …).
* In anderen Sprachen werden die deutschen Beschriftungen der Kacheln überdeckt und übersetzt.
### Mannschaft
* **Verträge:** Fenster (5,15,258×206), Spalten ART NAME SP ST TO STATUS E V.DAUER WERT (Zeilenhöhe 6 px),
  unten „Name: … (… Jahre)“. Klick = **Vertragsverhandlung** (Vertragsdauer, Gehalt / Monat, „Mach' ich!“ / „Abbruch“).
* **Transfer:** links der eigene Kader (Klick = Transferliste/Verkauf), rechts die **Transferliste** mit Kaufpreis (K)
  und Leihgebühr (L), Filter T/A/S; unten Kontostand, Schulden, Anlagen, Saldo und „Talent“. Klick auf (K) bzw. (L) =
  „… kaufen“ / „… leihen“ mit „Ihr Angebot:“.
* **Aufstellung:** links NR ART NAME SP STÄRKEN TO 2m 5m STATUS E, rechts vier Blöcke (3 Stürmer, 2 Verteidiger,
  Rückennummern) und Torhüter (TW). Spieler anklicken, dann Feld (oder umgekehrt); Rechtsklick leert ein Feld.
  Unten A = Auto-Aufstellung, C = leeren, OK = übernehmen (wird beim Verlassen automatisch übernommen).
* **Taktik:** Einsatz und Angriff je Spielstand, sechs Linienwechsel-Modi, Minuten je Reihe.
* **Training** (`kb\5`): 7 Tage × 4 Einheiten (je Tag zwei Kästen mit zwei Einheiten). Art unten wählen (gelb
  unterstrichen), Einheiten anklicken oder mit gedrückter Taste überstreichen; Rechtsklick = zurück. Oben Kosten pro
  Woche und Balken E K T (Energie, Kondition, Technik) je Gruppe Torhüter/Abwehr/Sturm. Nur die erste Art kostet Geld.
  Kosten, Wirkung und Standardplan aus em.exe (siehe `reverse-engineering.md`, Abschnitt Training).
### Manager
* **Finanzen** (`kb\152`): Konto, Kredit, Sparbuch; Betrag mit den Knöpfen links ändern, Stückzahl rechts; die vier
  Aktien mit Kurs, Besitz und Wert, Kursverlauf im schwarzen Feld.
* **Werbung** (`kb\151`): Logos auf Bande und Eisfläche, Trikotsponsor, Angebot mit + (annehmen) / − (ablehnen),
  Einnahmen, Ausgaben, Saldo.
* **Stadion** (`kb\14`): Ausbaustufen (Zeile anklicken = Ausbau), Sitz-/Stehplätze anklicken = Plätze bauen,
  Eintritt mit ▲▼.
* **Jugend:** Talentsuche („Spion“) und junge Spieler bis 21 Jahre.
* **Neuer:** Posteingang mit Angeboten (Transfer, Werbung) und Meldungen.
### Statistiken, Tabellen, Paarungen, Stärken
* **Tabelle:** Fenster (5,38,309×150), „TABELLE Gesamt 38.Spieltag“, Spalten PL. TD VEREIN BILANZ S-U-N PUNKTE TORE
  TORDIFF.; Knöpfe Heim / Gesamt / Auswärts bei y = 162. Punkte nach der 2-Punkte-Regel als Plus:Minus („65:11“),
  Bilanz = letzte acht Spiele (klein = Heim, groß = Auswärts; em.exe 0x50283/0x4F4F4, Spur `tabellen-original`), TD = Tendenz.
* **Verein:** Serien G/H/A (aktuell und Rekord), Über-/Unterzahl, Zuschauer (Schnitt, benötigter Schnitt, Rekord,
  Minuskulisse, Verlauf), Finanzen, Einnahmen/Ausgaben pro Monat.
* **Spieler:** Scorer, Torschützen, Vorlagen, Torhüter, Strafen. **Fairplay**, **Früher** (Chronik und Karriere),
  **Totale** (ewige Tabelle), **Highscore**.
### Spiel
* Anzeigetafel aus `kb\19`: Strafbank mit laufenden Strafen, „2 min.“, Zuschauer, Logos, Uhr aus den Ziffern `kb\38`,
  Drittelergebnisse, Tore und Schüsse senkrecht in den roten/grünen Feldern; unten Energie der Reihen, Reporter, Ticker.
* Geschwindigkeit 1x/4x/16x, Esc oder das kleine Feld unten links springt ans Ende, Enter = weiter.
* **Reporter:** der Mund (`kb\38`, 8 Bilder 12×21 ab x = 80) bewegt sich, solange eine Sprechblase (y = 172) zu sehen ist;
  Kommentare zu Tor, Strafe, Chance, Verletzung, Drittelende und Penalty in mehreren Varianten.
* **Torszene** (abschaltbar unter *Einstellungen*): Ausschnitt (5,44, 310×128) der Halle aus `kb\231/232` + Karte,
  Torschütze läuft an und schießt, Torhüter reagiert, „TOR!“; die Spieluhr hält 2,6 s an, Enter/Klick überspringt (keine Leertaste, #205).
* **Geräusche:** Publikum im Hintergrund, Pfiff bei Strafe/Drittel, Puck und Jubel beim Tor.
### Manager – Finanzen
* Klick auf das Bankbild oder das Kontofeld öffnet das **Finanzbuch**: jede Kategorie (Eintritt, Gastronomie, Sponsoren,
  Transfers, Gehälter, Unterhalt, Zinsen, Training, Bau, Talentsuche, Strafen, Freundschaftsspiele, Versicherung,
  Manager-Kredite) für die laufende und die vorige Saison.
* **Manager-Kredit** (nur mit mehreren Managern): Manager wählen, dann Zins 1–5 % pro Monat; der andere Manager
  bekommt beim nächsten Hauptmenü ein Kreditangebot zum Annehmen/Ablehnen (verfällt nach einer Woche).
  Bestehende Schulden beim Manager werden mit dem eingestellten Betrag getilgt.
### Paarungen
* In der Ligaansicht „Freundschaftsspiel“: Gegner aus Liga oder Europapokal wählen, Heimspiel an einem freien Termin
  der nächsten vier Wochen (höchstens zwei offen).
### Einstellungen
* Vollbild, Automatisch speichern (Torszenen: nur im Optionsbildschirm des Originals, Index 9); Lautstärke Soundeffekte (0–10); `F8` schaltet den Ton
  stumm. Musik: nur im Titelbildschirm, sonst nirgends (Entscheidung Nutzer, #221) – keine Musikeinstellungen. Knopf „Spielstand“ (nur mit laufendem Spiel): Optionsbildschirm des Originals (em.exe 0x2CD1C, DOSBox-Bild 48) mit
  Torszenen (drei Stufen), Spielgeschwindigkeit, Drittelpause/Ergebnisse/Tabelle je Liga, DEB-Pokal, Europapokal, WM-Spiele –
  schaltbar, gespeichert im Spielstand (`docs/reverse-engineering.md`, „Optionsbildschirm“).
### Saisonende
„Saisonende“ in großer Schrift, darunter Weltmeister, DEB-Pokal, Deutscher Meister, Europa-Pokal und je Liga
Aufsteiger (rot) und Absteiger (grün). Nach der gewählten Spieldauer folgt das Spielende mit Highscore.
Wer zu lange zu hohe Schulden hat (unter −1,5 Mio. DM für 8 Wochen, nur lokale Spiele), wird entlassen – Spielende
mit Entlassungstext; im Hot-Seat spielen die übrigen Manager weiter.

## Pixelvergleich Runde 6 (Spur `pixel-verein`)

Abläufe in `src/EishockeyManager.Tools/Scenarios/PixelVerein.cs`; die Bildnamen enden auf das Vorlagebild aus dem
privaten Referenz-Repo (`docs/referenz-videos.md`). `pixel-verein` stellt den Spielbeginn aus Folge 1 nach (Manager
„Kingsday d.2“, Kopf 4, Berliner SC, Spiel-Level 5, Modus „nur Fahne“), `pixel-verein-weltkugel` den Modus mit beiden
Knöpfen, `pixel-verein-bundestrainer` den Zug des Bundestrainers im Modus 4. Kennzahl: mittlere Graudifferenz und Anteil der
Pixel über 40, eigenes Bild je Vorlage um höchstens einen Pixel verschoben und weichgezeichnet (360p-Videos), Mauszeiger,
Moderator und spielstandsabhängige Felder maskiert.

| Bildschirm | Vorlage | Maskiert | Kennzahl |
|---|---|---|---|
| Taktik | `folge10_00300` (auch `_00302`–`_00328`, `duell_s2a_02108`, `eisbaeren_f16_03225` gesichtet) | – | 5,4 / 0,7 % |
| Aufstellung | `folge01_00528` | Werte der Liste (#168), TW-Feld | 5,9 / 2,0 % |
| Finanzen (Bank) | `folge01_00508` | – | 8,3 / 5,1 % – Kurse und Verlauf (#169) |
| Hauptmenü, Reihe Gemischtes | `folge01_00600` | Gegner, Begegnungen, Konto, Zeiger | 7,4 / 1,9 % |
| Reihe Statistiken („nur Fahne“) | `folge01_00526` | dto. | 7,6 / 2,0 % |
| Reihe Tabellen / Gemischtes (Duell) | `duell_s2a_01203`, `_00135` | Infoleiste | 6,0 / 0,2 %; 5,9 / 0,1 % |
| Reihen Stärken / Paarungen / Statistiken (beide Knöpfe) | `eisbaeren_f01_01430`, `_01431`, `_01011` | Infoleiste, Moderator | 7,9–8,1 / 0,6–0,7 % |
| Bundestrainer Modus 4, vier Reihen | `eisbaeren_f24_00049`–`_00057` | dto. | 7,9–8,1 / 0,7 % |
| Spielvorbereitung, Kopfwahl | `speedrun_t1_00056`, `eisbaeren_f01_00204` | Moderator | Text behoben (PV-01) |

Belegt durch die Bilder (bisher `[APPROX]`): Belegung der Kachelreihe Stärken (1. Liga, 2. Liga, Oberliga, Länderteams,
zwei leer); im Modus „nur Fahne“ sind die WM-Kacheln gesperrt, Europapokal bleibt frei; Kachelsperren des Bundestrainers.

| Befund | Original | Stand |
|---|---|---|
| PV-01 Hinweis der Kopfwahl: x 188 … 319, Grundlinien 214 und 222 (bisher 2–3 Pixel tiefer, Abstand 7) | em.exe 0x28CF8; `speedrun_t1_00056` | behoben, `PixelVereinTests` |
| PV-02 Kopfzeile der Aufstellung (auch Nationalteam): Grundlinie 23, Linie 25, eine Zeile Abstand (bisher eine Zeile tiefer, berührte die Linie) | em.exe 0x424DF; `folge01_00528` | behoben |
| PV-03 Lampen: Warnlampen des Managers am Zug (0 Aufstellung unvollständig, 1 Spielerfeld 0x1E, 2 Konto im Minus trotz Sparbuch/Aktien, 3 Trainingskosten, 4 freie Werbefläche, 5 Depot im Minus), y = 33 + 7 · i; bisher Bereitschaft der Manager (R2-06), y = 31 + 7 · i | em.exe 0x42996–0x42CA6; `folge01_00600`, `duell_s2a_00135`/`_01203`, `eisbaeren_f01_01430` | behoben; `[APPROX]` Lampe 1 (Feld 0x1E ungeklärt) bleibt grün, Eisfläche als eine Fläche (#133) |
| PV-04 Infoleiste des Bundestrainers vor WM-Spielen | siehe `docs/wettbewerbe.md` | behoben |
| PV-06 Startwerte der Kaderspieler | `folge01_00528` | offen (#168, Core) |
| PV-07 Börse zu Spielbeginn | `folge01_00508`–`_00510` | offen (#169) |
| PV-08 Nachrichtenleiste nur mit Nachricht | `eisbaeren_f01_00348`, `speedrun_t1_00121` | offen (#170) |

Nicht verglichen (kein Bildschirm im Remake): „Verein“-Statistik (`folge01_00522`, wartet auf `livespiel`).
