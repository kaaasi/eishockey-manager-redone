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
 
* **Spielberechnung** (Overlay „Live-Spiel“ = Ghidra 5E73, Stub 0x130): Minutenschleife mit Torchancen, Abschluss,
  Strafen und Statistik; Rekonstruktion, Fundstellen und Stand der Umsetzung in [spielberechnung.md](spielberechnung.md).
  Die frühere Beschreibung einer 10-Sekunden-Schrittsimulation galt nicht für den Code. Liga, Play-offs, Aufstiegsrunde,
  Pokale und WM rechnen mit `MatchEngine.PlayWithEvents`; nur die Freundschaftsspiele des Nationalteams noch mit
  `MatchEngine.Play` (`[APPROX]`, Poisson aus der Stärkedifferenz).
* **Finanzen** (Zuschauer, Eintritt, Gehälter, Sponsoren, Stadionkosten, Börse) – Größenordnungen nach den Texten des Originals.
* **Saisonmodus**: Playoff-Format, Relegation, DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
* **Torszenen** (`tore\`): Format teilweise erkannt (Kennung `TOR!`, Positionslisten), Darstellung fehlt noch.
* Spielfeld-Kacheln (`kb\231–236, 242–246`).
## Startwerte der Kaderspieler

Spur `kader-start` (#168). Geprüft an `em.exe` (`ndisasm`), Overlay „Spielstart“; im Remake `SquadSetup.StartBase`
(`Core/Squad.Setup.cs`), Tests `KaderStartTests`.

* **Kaderauswahl** (Datei `0x2993A–0x29A5E`): je Art (Torhüter, Verteidiger, Stürmer) so viele Spieler wie die
  Kadertabelle DGROUP `0x169` nach Start-Liga angibt, jeweils ein zufälliger Spieler des Pools (Besitzer `0x1A` = 6) aus
  dem Indexbereich DGROUP `0x381` ff. (`1, 21, 97, 200`); Besitzer = Manager, Verein `0x1D` = Managerverein, Alter
  `0x14` = random(19, 31).
* **Startwerte** (Datei `0x29DA4–0x29EF7`): Basis = Byte DGROUP `0x4B42` [Start-Liga · 3 + Start-Stärke]
  (Setup-Werte `0x11A`/`0x11B`). Für jeden Kaderplatz: Technik `0x15` = random(Basis − 2, Basis + 2), Kondition `0x16`
  = random(Basis − 2, Basis + 2), Form `0x17` = random(40, 60) – für alle Arten gleich, Torhüter eingeschlossen.

  | Start-Liga | Oberes Drittel | Mittelfeld | Abstiegskampf |
  |---|---:|---:|---:|
  | Bundesliga | 87 | 81 | 70 |
  | 2. Liga | 68 | 55 | 42 |
  | Oberliga | 39 | 31 | 21 |

* **Nicht beteiligt:** Spiel-Level (`0x118` wird hier nicht gelesen; `0xED8` = 5 − Level wirkt nur auf das Startkonto
  `0x29BD5`), Datenbankstärke des Vereins (sie bestimmt nur die Teamwerte der Computervereine, Lader `0x2EF1B`).
* **Belege:** `folge01` (Berliner SC, Oberliga, Abstiegskampf – Spielvorbereitung `folge01_00150`): Aufstellung
  `folge01_00528` alle Werte 19–23 = 21 ± 2. UI-Video (`oberflaeche_0900`/`_0910`): Werte 29–33 = 31 ± 2, also Oberliga,
  Mittelfeld. Die Regler der Spielvorbereitung zeigen dort zwar „Bundesliga“ (`oberflaeche_0230`–`_0310`), der Verein
  spielt aber Oberliga (DEB-Pokal `oberflaeche_0400`: „Oberliga gegen Oberliga: Berliner SC“). Der frühere
  `[APPROX]`-Ansatz 20 + Stärke/2 traf das UI-Video nur zufällig (Stärke 22 → 31).
* **Remake** (`[APPROX]`): Start-Liga und Start-Stärke sind je Manager eingestellt, ein fest gewählter Verein bleibt in
  seiner Liga (das Original setzt ihn in die Start-Liga, Datei `0x29C3B` ff.). Daher zählt die Liga des Vereins bei
  Spielbeginn (ligalose Vereine sind dann Oberliga) und die Start-Stärke des Managers. Die Teamwerte der
  Manager-Vereine werden weiterhin gezogen und verworfen, damit Computervereine und Transfermarkt dieselbe Zufallsfolge
  behalten. Computervereine und Talente bleiben bei `SquadSetup.BaseValue` (20 + Stärke/2, `[APPROX]`).
* **Offen:** Das Original wählt den zufälligen Verein aus der ganzen Start-Liga (`random([0x7C0 + Liga], …)`, Datei
  `0x29C78`), nicht aus dem Drittel der Start-Stärke wie `NewGame.PickRandomClub`; nicht Teil dieser Spur.

**Mehrsaisonprobe, Ursache je geändertem Abdruck.** Vergleichslauf (Probe außerhalb des Repos) auf Base `15d0733` und
auf dem Spur-Stand, Zustand (Manager, Manager-Vereine, Computervereine, Pool, Laufbahnende) und alle vier
Zufallsquellen nach jedem Schritt. In allen Fällen unterscheiden sich nach dem Spielstart **nur die Spieler der
Manager-Vereine** (Werte, Gehälter); Computervereine, Pool und alle vier Zufallsquellen sind gleich.

| Fall | Basis alt → neu | erste Differenzen danach | Ursache |
|---|---|---|---|
| `RngTwinProbeTests` synthetisch passiv/aktiv, Seed 3 (Verein 2) | ≈ 65 → 81 (Bundesliga, Mittelfeld) | Konto Tag 21; Spielstärke ±1 ab Tag 35; Pokalquelle Tag 56; FinanceRandom Tag 84; Hauptquelle Tag 153 | Gehälter +5.800 DM/Monat und Grundeinnahmen aus Teamwerten (Monatsabschluss); Energie wirkt auf größere Spielerwerte stärker → andere Pokal- und Ligaergebnisse |
| dito Seed 11 | ≈ 65 → 81 | Konto Tag 21; Spielstärke ±1 ab Tag 10; Pokalquelle Tag 45; FinanceRandom Tag 84; Hauptquelle Tag 163 | wie oben (+4.600 DM/Monat) |
| `RngTwinProbeTests` Originaldaten passiv/aktiv (Verein 42) | 31 ± 3 je Art → 31 (Oberliga, Mittelfeld) | Konto Tag 21; FinanceRandom 1994 Tag 28; Spielstärke ±1 ab Tag 136; Kader/Pool ab Saisonwechsel 1995 (passiv: Pokalquelle 1996 Tag 21, Hauptquelle 1996 Tag 70, Titel 1996) | Gehälter +500 DM/Monat, Teamwerte leicht anders (Zuschauer, Grundeinnahmen) |
| `GameModeTests.Mode4_ReferenceValues_Unchanged` (Vereine 2 und 30) | 65 → 81, 55 → 31 (Verein 30 Oberliga) | Konto Manager 1 Tag 3; Spielstärke Verein 2 ab Tag 40; Pokalquelle Tag 45; Hauptquelle Tag 91 | Zuschauer aus den Teamwerten (`0x4D316`) beim ersten Heimspiel; danach wie oben → 1993 andere Pokal- und WM-Sieger |
| `DebCupDrawFlowTests.MultiSeason_…` (Verein 42, Seed 1993) | 31 ± 3 → 31 | Gehälter +200 DM; Konto Tag 7; Spielstärke ±1 ab Tag 17; Hauptquelle 1994 Tag 122 | 1993 unverändert, ab 1994 andere Meister und Tabellen |

`FinanceCalibrationTests` (Originaldaten, Sollspannen) bleibt grün. `ClubDressedTests.TwentyThird_…` setzte voraus, dass
nach dem Zukauf ein eigener Stürmer nicht eingesetzt ist; mit dem stärkeren Startkader bleibt ein zugekaufter
Verteidiger draußen – der Test wählt den Platz im vierten Block jetzt nach der Spielerart (Prüfung unverändert).

## Kopierschutz (Handbuchfrage)

Spur `menue-befunde`, #185. **Das Remake übernimmt die Abfrage nicht** (Entscheidung Nutzer, 30.10.2026; bewusste
Abweichung in [portierung.md](portierung.md)). Hier steht nur, was das Original tut. Alle Dateioffsets in `em.exe`;
DGROUP liegt bei Datei 0x16DD0 (Segmenttabelle 0x163C0, Eintrag 0x148).

* **Bildschirm „PASSWORTABFRAGE“** (Bildfolge `folge23_02308`, `_02314`, `_02315`, Fundliste `docs/referenz-videos.md`):
  Überschrift, „Bitte ziehen Sie nun Ihre Anleitung zu Rate:“, darunter „SEITE: 16 – ZEILE: 1 – WORT: 1“ und acht rote
  Wortknöpfe (z. B. Regel, Legen, Bereiche, Rechts, Seine, Probieren, Kopie, Bild). Gesucht ist das Wort an der
  genannten Stelle im gedruckten Handbuch (hier „Rechts“).
* **Funktion** ab Datei **0x4CD46** (Overlay, Segmenttabelle 0x120, Stub-Offset 0x8E; Parameter: Zeiger auf ein Dword).
  Ablauf: Position der richtigen Antwort = `random(0, 7)`, Eintrag = `random(0, 0x1B)` (28 Einträge); die übrigen sieben
  Knöpfe bekommen verschiedene andere Wörter (Wiederholung wird ausgeschlossen). Ausgabe: Rückgabe 1 = richtig;
  das Dword erhält 0 bei richtiger, 1 bei falscher Antwort.
* **Tabellen** (DGROUP): Wortliste 0x14F5, 28 Wörter zu je 13 Byte; Seite 0x7D4, Zeile 0x7F0, Wort 0x80C (je 28 Byte, gleicher
  Index). Wörter und Texte sind mit **XOR 0xD9** (Wörter) bzw. **0xC9** (Titel 0x1661, Frage 0x1671, „SEITE: “ 0x169E,
  „ – ZEILE: “ 0x16A6, „ – WORT: “ 0x16B1) verschleiert; das Byte 0 bleibt 0. Deshalb findet die Textsuche nach
  „Handbuch“ oder „Passwort“ nichts. Die Zeichenkodierung ist die übliche des Spiels (`}` = ü, `{` = ä).

  | Nr | Seite | Zeile | Wort | Lösung | | Nr | Seite | Zeile | Wort | Lösung |
  |---|---|---|---|---|---|---|---|---|---|---|
  | 0 | 3 | 5 | 3 | Fan | | 14 | 8 | 3 | 8 | Legen |
  | 1 | 7 | 14 | 2 | Kopie | | 15 | 10 | 12 | 7 | Bereiche |
  | 2 | 10 | 9 | 4 | Regel | | 16 | 11 | 4 | 4 | Damen |
  | 3 | 11 | 6 | 6 | Reihe | | 17 | 13 | 2 | 1 | Pingi |
  | 4 | 13 | 21 | 7 | Stärke | | 18 | 13 | 4 | 7 | Baby |
  | 5 | 15 | 16 | 12 | Saison | | 19 | 13 | 10 | 5 | Üben |
  | 6 | 21 | 13 | 12 | Logo | | 20 | 13 | 16 | 12 | Verfügung |
  | 7 | 27 | 22 | 6 | Bild | | 21 | 15 | 2 | 3 | Auslosung |
  | 8 | 43 | 5 | 3 | Spiel | | 22 | 15 | 19 | 9 | Smiley |
  | 9 | 44 | 12 | 10 | Knopf | | 23 | 14 | 2 | 5 | Probieren |
  | 10 | 3 | 2 | 10 | Seine | | 24 | 14 | 9 | 7 | Exkursion |
  | 11 | 3 | 14 | 8 | Herz | | 25 | 16 | 1 | 1 | Rechts |
  | 12 | 7 | 7 | 1 | Spieler | | 26 | 16 | 6 | 7 | Kamera |
  | 13 | 7 | 18 | 9 | Diskette | | 27 | 17 | 2 | 7 | Datum |

  Das Bild zeigt `Seite 16 – Zeile 1 – Wort 1` = Eintrag 25, „Rechts“ ✔ (Wort im Bild hervorgehoben).
* **Auslöser** – drei Aufrufstellen (Fernaufruf `9A 8E 00 20 01`):
  1. **Spielstart** (Datei 0x4387B, in der Funktion, die Spielwerte vorbelegt und damit zum Spielstart gehört – [APPROX] Zuordnung): nur wenn `random(0, 2) == 0` (Wahrscheinlichkeit 1/3).
     Höchstens zwei Versuche; wer beide verfehlt, bleibt in einer Endlosschleife hängen (`si` bleibt 2, Sprung
     0x4389A ↔ 0x43871) – das ist der eigentliche Kopierschutz (aus dem Code gelesen, im Original nicht ausprobiert).
  2. Schleife mit Zähler gegen DGROUP 0x386 (Datei 0x34DB7; Zweck der Schleife nicht geklärt): wenn Zähler DGROUP 0x5A14 < 2000, Byte 0x5CF3 == 4
     und `random(0, 7) == 0`. Das Ergebnis wird an der Stelle nicht ausgewertet.
  3. Funktion bei Datei 0x5667A (Datei 0x566AC): wenn Zähler DGROUP 0x3BD > 10 und `random(0, 5) == 0`, oder genau 9.
     Das Ergebnis wird an der Stelle nicht ausgewertet.
* **Nicht geklärt:** Bedeutung der Globalen 0x5A14, 0x5CF3, 0x3BD (Auslöser 2 und 3) und ob je ein falsches Ergebnis dort
  Folgen hat. Für die Spur nicht nötig, da nichts davon portiert wird. Spielstart (1.) ist durch Bildfolge und Code belegt.
* Test, dass das Remake nicht fragt: `MenueBefundeTests` (`NewGame_AndFirstMatchDays_RunWithoutManualQuestion`,
  `NoScreenImplementsTheManualQuestion`).
