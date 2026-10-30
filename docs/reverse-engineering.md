> **Herkunft:** aus einem früheren, unvollständigen Portierungsversuch. Die Adressen (Segment:Offset) beziehen sich auf die Ghidra-Analyse von `em.exe`. Bisher an den Dateien bestätigt: Datenbank, Fonts, Hallenkacheln/-karten, Figuren (siehe `formate/dateiformate.md`). Die Spielregeln sind noch nicht nachgeprüft.

# Reverse Engineering – Stand der Rekonstruktion
 
Ansatz „Hybrid“: Datenstrukturen und alle bereits entschlüsselten Formeln werden 1:1 übernommen,
noch nicht rekonstruierte Teile werden plausibel nachgebaut und im Code mit `[APPROX]` markiert
(verifizierte Teile mit `[ORIGINAL]`). So ist das Spiel sofort spielbar und kann schrittweise originalgetreuer werden.
 
Werkzeuge: siehe `tools/reverse-engineering/README.md`.
 
## Programmaufbau
 
* Borland C++ (1991), Large Model, Real Mode, DGROUP = Segment 0x14FD.
* 12 Basissegmente (C-Laufzeit, Grafik/Fonts, Fenster, Datei-I/O, Sound) und 17 VROOMM-Overlays.
* Overlay-Zuordnung (Stub → Funktion, grob nach verwendeten Texten):
| Overlay (Ghidra) | Größe | Thema |
|---|---|---|
| 337B | 2 KB | Sponsoren-/Werbetexte |
| 3810 | 20 KB | Spielstart, Vereinsinitialisierung, Spielmodus-Optionen |
| 3D11 | 24 KB | Vereinslogos, Verletzungen außerhalb des Spiels |
| 42E9 | 17 KB | Bildaufbau, Fonts, Fehlerbehandlung |
| 4A4C | 12 KB | Sponsoren, Werbepartner |
| 4D30 | 12 KB | Aufstellung, Reihen, Transfers |
| 502F | 20 KB | Zeitung, Infofile, Einnahmen/Ausgaben |
| 52F7 | 18 KB | Statistik, Ewige Tabelle, DEB-Pokal-Auslosung, Spielerwerte |
| 5E73 | 21 KB | Live-Spiel, Torszenen, Stadion-Ausbau |
| 63A2 | 13 KB | Transferangebote, Börse |
| 66C3 | 16 KB | Datenbank, Spielergenerierung, Laden/Speichern |
 
## Verifizierte Regeln (im Code übernommen)
 
| Regel | Quelle |
|---|---|
| `random(a, b)` mit beiden Grenzen inklusive | `1058:0647` |
| Teamwerte: Kondition/Technik Abwehr/Sturm = Basis ± random(0, Basis/10) | `3810:4D3B` |
| Form der Vereine 45–55, Europapokal-Teams 5 schwächer | `3810:4D3B` |
| Stärken der Nationalteams (Tabelle in DGROUP) | `3810:4D3B` |
| 3 Ligen à 12 Vereine, Indizes 0–35 | DGROUP `0x7B1` |
| Kader je Schwierigkeit: 2/10/13, 2/9/11, 2/8/10 | DGROUP `0x169` |
| Transferpool: Alter 18–33, Basis 30–89 (unter 45 zu 50 % neu 40–89), Wert = Basis+10−random(0..20), Form 45–55 | `66C3:0730` |
| Kaderspieler: Alter 19–31 | `66C3:0730` |
| Platzhalter-Slots bekommen zufällige, eindeutige Datenbanknamen | `66C3:0280` |
| Spielerfeld 0x12 = random(0..9) (Talent) | `66C3:0730` |
| Spielstand-Verschlüsselung | `66C3:0074/018E` |
| Datei- und Grafikformate, Fonts | siehe `Dateiformate.md` |
 
## Spielerstruktur im Original (33 Byte)
 
| Offset | Inhalt |
|---|---|
| 0x00 | Name (17 Zeichen + 0) |
| 0x12 | Talent 0–9 |
| 0x14 | Alter |
| 0x15 | Technik (aus Vereinsfeld 0x19 abgeleitet) |
| 0x16 | Kondition (aus Vereinsfeld 0x1B abgeleitet, 1/5 Chance auf +12..20) |
| 0x17 | Form |
| 0x18 | Rollenwert (Torhüter 0, Verteidiger 25–41, Stürmer 75–91) |
| 0x1A | Besitzer: Manager 0–3, **4 = Transfermarkt** (belegt an Original-Spielstand FILE05, `formate/spielstand-original.md`; früher „6“) |
| 0x1D | Verein / Nationalteam (0xFF = keiner) |
 
Nur 200 Spieler werden detailliert verwaltet (Kader der menschlichen Manager + Transfermarkt).
Computervereine spielen ausschließlich über ihre Teamwerte. Das Remake erzeugt dagegen für alle Vereine
vollständige Kader (aus den Datenbanknamen) und leitet deren Werte aus den Teamwerten ab – damit funktionieren
Transfers zwischen allen Vereinen, Statistiken und Ex-/Import einheitlich.
 
## Vereinsstruktur im Original (61 Byte, 150 Einträge)
 
| Offset | Inhalt |
|---|---|
| 0x00 | Name |
| 0x16 | Status/Liga-Flag (häufigster Zugriff) |
| 0x17 | random(47..53) |
| 0x18/0x19 | Kondition Abwehr/Sturm |
| 0x1A/0x1B | Technik Abwehr/Sturm |
| 0x1C/0x1D | Form Abwehr/Sturm |
| 0x34 | Logo |
| 0x3B | Farbcode (16 Bit) |
 
## Overlays: Segmente in Fernaufrufen

Fernaufrufe in Overlay-Code (`call 0x38:0x2d8`) nennen nicht das Segment, sondern einen Eintrag der Segmenttabelle
(Datei 0x163C0, 8 Byte je Eintrag, Index = Wert / 8; Verweis im `FBOV`-Kopf bei Datei 0x255B0). Beispiel: 0x38 →
Eintrag 7 → Segment 0x99F → Datei 0x1E00 + 0x99F0 + Offset. Segment 0x0000 ist die C-Laufzeit (0x3CA = LXMUL,
0x47C = LDIV). Einträge mit Flag 3 sind Overlay-Stubs (`INT 3Fh`).

## Geldbeträge

Das Original rechnet intern in **Drittel-DM**: die Ausgabefunktion 99F:02D8 rechnet Betrag × [0x7A1] / [0x79D]
= Betrag × 1 / 3, bevor sie den Text bildet. Belegt am Training (siehe unten). Für die Pakete `manager` und
`speichern` wichtig: Rohwerte aus em.exe oder Spielständen sind durch 3 zu teilen.

## Spiel-Level

Setup-Werte DGROUP 0x118–0x11B = Level, Dauer, Start-Liga, Start-Stärke (Vorgaben 3, 3, 0, 1),
Höchstwerte 0x11C–0x11F = **5, 3, 2, 2**. Belegt im Overlay „Spielstart“ (Datei 0x28570 ff.):

* Erstzeichnung (Datei 0x28571): Schleife über die vier Regler, Streifen = Wert + Tabelle DGROUP 0x4B3E
  (`0, 6, 10, 13`), Bild kb\1, Streifenhöhe 10, Ziel (195 | 81 + 28 · Regler).
* Verstellen (Datei 0x2890A): Wert ± 1; unter 0 → 0, über dem Höchstwert → Höchstwert (**kein Umlauf**).
  Zusätzlich: Ist danach Dauer = 0 (eine Saison) und Start-Liga ≠ 0, wird der Schritt zurückgenommen.
* Level umfasst damit **0–5** (sechs Figuren in kb\1, Streifen 0–5), Dauer 0–3 (Streifen 6–9), Start-Liga 0–2
  (10–12), Start-Stärke 0–2 (13–15). Im Remake: `SetupSliders` in `Core/GameSetup.cs`.
* Spielstart (Datei 0x2988D): 0xED8 = 5 − [0x118]. Weitere Lesezugriffe auf [0x118] gibt es nicht; der Level ist im
  Original **ein Wert für alle Manager** (Remake: je Manager, `[APPROX]`).
* Die Kadertabelle DGROUP 0x169 wird beim Spielstart mit `[0x11A] · 4 + Gruppe` gelesen (Datei 0x29A3F), also
  nach **Start-Liga**, nicht nach Level.

## Training (Manager-Feld 0xA8A1, 28 Byte je Manager) – `[ORIGINAL]` in `Squad.Training.cs`

* Bildschirm: Overlay-Funktion ab Datei 0x59DBF. Einheiten bei x = 14 + 44·Tag, y = 103/125/168/190, Bildchen
  29 × 20 aus der Leiste (x = 26 + 30·Art, y = 218). Unterstreichung y = 238 (Farbe 24). Balken E/K/T bei
  x = 148/157/166 + 2·Gruppe, Unterkante y = 93, höchstens 36 px, Farben DGROUP 0x5544.
* Kosten: Art 0 kostet je Einheit (12.000 − 1.200·Stufe)·Preisindex/100, dazu einmal 21.000 − 6.000·Stufe
  (Drittel-DM). Stufe = Liga bei Spielbeginn (Manager-Feld 0x9746). Abzug je Trainingstag, Anzeige ×2 pro Woche.
* Trainingstage: zweimal pro Woche (Tag mod 7 = 0 bzw. = 4), Funktion 46E:0C89, für jeden Spieler 46E:00BC.
* Wirkung (46E:00BC): Belastung je Art/Wert/Gruppe aus DGROUP 0x4861, Norm 0x48CD (= Belastung des Standardplans),
  Erholung 0x48E5. Wochenbonus in %: Alter, Wiederholungen (−3), Training am Spieltag (−6/−20), fehlende Arten (−5),
  Art 0 (+7). Ergebnis je Wert ±1 mit Wahrscheinlichkeit Abweichung/101 (Rundung 46E:000A). Spielerfeld 0x13 =
  Belastung 50–150, Feld 0x19 = zwei versteckte Werte (Halbbytes).
* Standardplan (46E:0653): Arten 2–8 reihum, frei an Mi/So nachmittags und Sa nachmittags; bei Level 5 immer frei.

## Noch nicht rekonstruiert (`[APPROX]` im Code)
 
* **Spielsimulation** (Overlay 5E73): Das Remake simuliert in 10-Sekunden-Schritten mit den im Original sichtbaren
  Parametern (3 Reihen, Linienwechsel-Modi, Einsatz/Angriff je Spielstand, 2/5-Minuten-Strafen mit den 13 Strafarten,
  Über-/Unterzahl, Verletzungen, Penaltyschießen). Kalibriert auf ~6,5 Tore/Spiel.
* **Finanzen** (Zuschauer, Eintritt, Gehälter, Sponsoren, Stadionkosten, Börse) – Größenordnungen nach den Texten des Originals.
* **Saisonmodus**: Playoff-Format, Relegation, DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
* **Torszenen** (`tore\`): Format teilweise erkannt (Kennung `TOR!`, Positionslisten), Darstellung fehlt noch.
* Spielfeld-Kacheln (`kb\231–236, 242–246`).