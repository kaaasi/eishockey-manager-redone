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
**Leertaste** = Weiter (im Hauptmenü), **F2** = Sichern, **F9** = Klassik/Modern, **F11** = Vollbild.
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
| `kb\195` | Zeitungsfoto mit acht Textzeilen | Posteingang („Neuer“) |
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
  Heim/Auswärts, bisherige Begegnungen („A 0:3,H 1:5“), Kontostand; sechs Lampen (Manager: rot = am Zug, grün = bereit);
  Kopf (231,33) und Logo (273,33).
* Große Kacheln 72 × 58 bei x = 9, 85, 161, 238 und y = 79, 137: Manager, Mannschaft, Statistiken, Tabellen,
  Paarungen, Stärken, Gemischtes, und tschüß…
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
* **Training** (`kb\5`): 7 Tage × 2 Blöcke (Woche A/B im Wechsel) × 2 Einheiten. Art unten wählen, Einheiten anklicken
  oder überstreichen; Rechtsklick = frei. Oben Kosten pro Woche und Balken E K T (Energie, Kondition, Technik).
  *[APPROX]* Wirkungen und Kosten sind angenähert.
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
  Bilanz = letzte neun Spiele (groß = Heim, klein = Auswärts), TD = Tendenz.
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
  Torschütze läuft an und schießt, Torhüter reagiert, „TOR!“; die Spieluhr hält 2,6 s an, Leertaste/Klick überspringt.
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
* Vollbild, Torszenen zeigen, Cheats, Automatisch speichern; Lautstärke Soundeffekte und Musik (0–10),
  Musik „auch im Spiel“ (Standard: an); `F8` schaltet den Ton stumm.
### Saisonende
„Saisonende“ in großer Schrift, darunter Weltmeister, DEB-Pokal, Deutscher Meister, Europa-Pokal und je Liga
Aufsteiger (rot) und Absteiger (grün). Nach der gewählten Spieldauer folgt das Spielende mit Highscore.
Wer zu lange zu hohe Schulden hat (unter −1,5 Mio. DM für 8 Wochen, nur lokale Spiele), wird entlassen – Spielende
mit Entlassungstext; im Hot-Seat spielen die übrigen Manager weiter.