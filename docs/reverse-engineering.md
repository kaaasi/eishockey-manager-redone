> **Herkunft:** aus einem früheren, unvollständigen Portierungsversuch. Die Adressen (Segment:Offset) beziehen sich auf die Ghidra-Analyse von `em.exe`. Bisher an den Dateien bestätigt: Datenbank, Fonts, Hallenkacheln/-karten, Figuren (siehe `formate/dateiformate.md`). Die Spielregeln sind noch nicht nachgeprüft.

# Reverse Engineering – Stand der Rekonstruktion
 
Ansatz „Hybrid“: Datenstrukturen und alle bereits entschlüsselten Formeln werden 1:1 übernommen,
noch nicht rekonstruierte Teile werden plausibel nachgebaut und im Code mit `[APPROX]` markiert
(verifizierte Teile mit `[ORIGINAL]`). So ist das Spiel sofort spielbar und kann schrittweise originalgetreuer werden.
 
Werkzeuge: siehe `tools/reverse-engineering/README.md`.
 
## Programmaufbau
 
* Borland C++ (1991), Large Model, Real Mode, DGROUP = Segment 0x14FD.
* 12 Basissegmente (C-Laufzeit, Spiellogik, Fenster, Bildlader, Speicher, Grafik/Maus, Datei, Timer, Sound) und
  17 VROOMM-Overlays; Rollen der Basissegmente in [re-abdeckung.md](re-abdeckung.md).
* Overlay-Zuordnung (berichtigt durch Spur `re-abdeckung`, #210: alle 17 Overlays, Größe = Codegröße aus dem Stub-Kopf,
  Thema nach benutzten Texten und belegten Funktionen; Dateibereiche und Anteile in [re-abdeckung.md](re-abdeckung.md)).
  Der Ghidra-Name ist das Ladesegment der alten Analyse (Adressen wie `3810:4D3B` = Overlay 0xE0, Offset 0x4D3B):

| Stub-Selektor | Ghidra | Größe | Thema |
|---|---|---|---|
| 0xC0 | 337B | 2,0 KB | Fehlermeldungen („LIB-Error“, „GRAFIK-Error“, „Alles klar!“) – früher „Sponsoren-/Werbetexte“ |
| 0xC8 | 33FB | 4,1 KB | Saisonende: Ranglisten („Die besten“, bester Torhüter), „Ereignisse“ |
| 0xD0 | 34FF | 4,1 KB | Saisonende: Prämien („Erhaltene Prämien“) |
| 0xD8 | 3606 | 8,1 KB | Spielvorbereitung und Spielstart (Regler, Kopfwahl, Kader, Startkonto) |
| 0xE0 | 3810 | 20,0 KB | Teamwerte (Lader), Ewige Tabelle/Historie, Bandenwerbung, Spielmodus-Optionen |
| 0xE8 | 3D11 | 23,4 KB | Wettbewerbe: DEB-Pokal (Auslosung), Europapokal, WM, Jugendansichten, K.-o.-Spiel |
| 0xF0 | 42E9 | 16,6 KB | Spieltagsablauf: Spielminuten, Strafen, „Spiele laufen“, Penaltyschießen |
| 0xF8 | 4711 | 12,9 KB | Nationalteam und WM (Vorbereitung, WM-Kader, WM-Statistik), Jugendteams, Credits-Anzeige |
| 0x100 | 4A4C | 11,6 KB | Stadion und Finanzen (Ausbau, Eintritt, Grund- und Werbeeinnahmen, Sponsor) |
| 0x108 | 4D30 | 12,0 KB | Aufstellung, Reihen, Spielerinfo, Vertragsende |
| 0x110 | 502F | 11,1 KB | Hauptmenü-/Spieltagsschleife (autosave.hok), Kalender, Infofile – **keine Zeitung** (früher „Zeitung, Einnahmen/Ausgaben“, 20 KB) |
| 0x118 | 52F7 | 19,5 KB | Transfermarkt: Transferliste, Angebote, Vertragsverhandlung |
| 0x120 | 57D6 | 8,2 KB | Meldungen/Posteingang, Handbuchfrage, kleine Rechenhilfen |
| 0x128 | 59E3 | 18,2 KB | Tabellen und Statistikansichten (Tabelle, Sortierung, Vereinsinfo) |
| 0x130 | 5E73 | 20,7 KB | Live-Spiel (Anzeige, Torszenen, Spielabrechnung), Saisonwechsel |
| 0x138 | 63A2 | 12,5 KB | Finanzen-Bildschirm (Konto, Kursverlauf), Training |
| 0x140 | 66C3 | 16,3 KB | Laden/Speichern, Datenbank und Spielergenerierung, Highscore |

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
  2. Schleife mit Zähler gegen DGROUP 0x386 (Datei 0x34DB7; Zweck der Schleife nicht geklärt): wenn das Endjahr
     (DGROUP 0x5A14, s. u.) < 2000 ist, also ein endliches Spiel, die DEB-Pokal-Runde (Byte DGROUP 0x5CF3) == 4 und
     `random(0, 7) == 0`. Das Ergebnis wird an der Stelle nicht ausgewertet.
  3. Funktion bei Datei 0x5667A (Datei 0x566AC): wenn der Saisonzähler (DGROUP 0x3BD) > 10 und `random(0, 5) == 0`, oder
     genau 9. Das Ergebnis wird an der Stelle nicht ausgewertet.
* **Globale der Auslöser** (inzwischen erklärt, Spur `re-abdeckung`, [re-abdeckung.md](re-abdeckung.md) „Einordnung“):
  * DGROUP **0x5A14** = **Spielende (Jahr)**: der Spielstart setzt das gewählte Endjahr oder **22222** (Datei 0x2986E) für
    ein endloses Spiel; Vergleiche mit 1994 (0x7CA) und 2000 (0x7D0). So auch in `OriginalSaveGame.cs` und `TileLocks.cs`.
  * DGROUP **0x5CF3** = **DEB-Pokal-Runde** (`finanzen-rekonstruktion.md`, Sponsorbewertung).
  * DGROUP **0x3BD** = **Saisonzähler** (erhöht in Datei 0x56476; `highscore.md`).
* **Nicht geklärt:** ob ein falsches Ergebnis bei Auslöser 2 oder 3 je Folgen hat. Für die Spur nicht nötig, da nichts
  davon portiert wird. Spielstart (1.) ist durch Bildfolge und Code belegt.
* Test, dass das Remake nicht fragt: `MenueBefundeTests` (`NewGame_AndFirstMatchDays_RunWithoutManualQuestion`,
  `NoScreenImplementsTheManualQuestion`).

## Credits (Titelleiste im Hauptmenü)

Spur `credits`, #207. Die Texte stehen nicht in `em.exe` (auch nicht verschleiert – geprüft: alle XOR- und Additionsschlüssel
für „Noeske“, „Testspieler“, „Kandemiroglu“), sondern fertig gezeichnet im Bild **`kb\15`** (216 × 151).

* **Auslöser** (Hauptmenüschleife, Datei 0x4544E–0x45488): Mausereignis 1 oder 3 (DGROUP 0x5A16, linke/rechte Taste), dann
  zehnmal `0x120:0x75` (Datei 0x4C196, gibt Schaltflächenplätze frei – Verwaltung, nicht portiert), dann bei Maus-y `si < 0x15`
  Fernaufruf `0xF8:0x20`; die x-Lage wird nicht geprüft.
* **Fenster** = Overlay 0xF8 (Segmenttabelle Eintrag 0xF8, Overlaybeginn Datei 0x399D0, Funktion am Anfang): Puffer
  216 × 153 anlegen, `kb\15` laden (Bildladefunktion `0x38:0x98D`, Datei 0xC17D), Bildschirm ab (0x33 | 0x3D) sichern, Bild
  nach (51 | 61) kopieren, warten, Hintergrund zurückschreiben, Puffer freigeben.
* **Warten** `0x38:0x118` (Datei 0xB908): erst bis kein Mausereignis mehr ansteht (0xC097), dann bis Mausereignis 1 oder 3
  (Abfrage 0xC068 → `0xBBF:0x24DF`) oder DGROUP 0x1DB (Durchlauf ohne Halt); dazwischen Pause `0x1115:0x19E` mit 30.
  Tasten werden dort nicht ausgewertet.
* **Bildtexte** sind pixelgenau der Mini-Font mit Schatten (Farbe 0, +1/+1): linke Spalte x 11, Farbe 30; rechte Spalte x 100,
  Farbe 7; Zeilen-Oberkanten 22, 30, 43, 51, 64, 76, 88, 100, 111, 119, 127, 138 (Ausnahme: der lange Strich in
  „Ogan – Celal Kandemiroglu“ ist kein Zeichen des Fonts).

## Ligatabelle (Spur `tabellen-original`)

Overlay 0x128 (Ghidra 59E3). Fundstellen als Dateioffset von em.exe.

* **Vereinssatz der Tabelle** DGROUP 0x825A + 0x50 · Verein (im Spielstand Block 0x8257, 0x1400 Byte), je Feld erst Heim,
  dann Auswärts: Punkte +0/+1, Bilanz +4 (8 Zeichen + 0), Tore +0x16/+0x17, Gegentore +0x1A/+0x1B, Spiele +0x1E/+0x1F,
  Siege +0x26/+0x27, Niederlagen +0x2A/+0x2B; dahinter je eine Sicherung (+2/+3, Bilanz +0x0D, +0x18/+0x19, +0x1C/+0x1D,
  +0x28/+0x29, +0x2C/+0x2D). Unentschieden zählt das Original nicht, die Anzeige rechnet Spiele − Siege − Niederlagen.
* **Ergebnis eintragen** Datei 0x50283 (Heim, Gast, Heimtore, Gasttore): Tabellenfelder nur bei DGROUP 0xD0C2 = 7 oder 8;
  Sieg DGROUP 0x7C5 (= 2) Punkte, Unentschieden 1; Bilanz über 0x4F4F4 (8 Zeichen nach links schieben, neues hinten) mit
  den Zeichen DGROUP 0x7CE „sSnNuU“: **Heimverein klein** (s/u/n), **Gast groß** (S/U/N). Außerdem Serien
  DGROUP 0xB57B + 0x15 · Verein und Zähler über 0x51133/0x4F5A8 (nicht Teil dieser Spur).
* **Spieltag und Sortierung** Datei 0x50B45 (Liga, Modus, Ansicht 0 Heim / 1 Gesamt / 2 Auswärts): Modus 1 sichert die
  Felder (0x4F325: aktuell → Sicherung), Modus 0 stellt sie aus der Sicherung her und trägt die Ergebnisse des Spieltags
  (DGROUP 0xA0CB, Paarungen 0x5D7B) ein. Danach Tauschsortierung der Platzliste DGROUP 0x5CCB (Liga · 12 + Platz,
  Datei 0x50D6D–0x51011): für jeden Platz i alle späteren j; j verdrängt i bei mehr Punkten, bei gleichen Punkten weniger
  Spielen (abschaltbar mit DGROUP 0x6B0 – kein Code schreibt die Stelle), dann höherer Tordifferenz, dann mehr Toren; sonst
  bleibt die Reihenfolge (keine weitere Regel, nicht stabil). Am letzten Spieltag der Oberliga folgt `0xE8:0x66`.
* **Aufrufe:** Tagesbeginn 0x36CD3 (Liga, 1, Gesamt) nach dem Spielplan 0x4E7C7; Spielende 0x385C7 (Liga, 0, Gesamt) bei
  Minute 60; Hauptmenü 0x44EFD (Liga, 1, Gesamt); Tabellenansicht 0x4FA8D (Liga, 1, gewählte Ansicht) und beim Verlassen
  0x50256 (Liga, 1, Gesamt). Die Platzliste behält also nach dem Ansehen wieder die Gesamtordnung; nur die Reihenfolge
  vollständig gleicher Vereine kann sich durch eine angesehene Heim-/Auswärtstabelle ändern.
* **Startordnung** Datei 0x2F1A2: beim Spielstart Vereinsindex aufsteigend; danach tauscht 0x437D5 (Datei 0x43A08/0x43A0D)
  in der 1. Liga die Plätze 1 und 3 (DGROUP 0x5CCB = 2, 0x5CCD = 0; Deutscher Meister DGROUP 0xD0E6 = 2). Video 13:07. Andere Schreiber von 0x5CCB: nur die
  Sortierung und 0x35EDD (WM-Reihenfolge). Über den Saisonwechsel bleibt die Liste stehen.
* **Tabellenansicht** Datei 0x4F78D: Titel aus DGROUP 0x1011 („   TABELLE “) + Ansicht (Zeiger 0x1001: „Heim“, „Gesamt“,
  „Auswärts“) + Spieltag; Name x 0x21, Bilanz x 0x75 (immer die Gesamtbilanz +4, gleich in jeder Ansicht), Tendenz über
  `0x110:0x4D`, Zahlenzeile ab x 0xA6 als ein Text: „*“, Siege, Unentschieden, Niederlagen (Mindestbreite 2,
  DGROUP 0x374), Punkte „P:M“ mit M = 2 · Spiele − P (bei DGROUP 0x7C5 ≥ 3 statt M ein Leerzeichen), Tore „T:G“
  (Breite 3), Vorzeichen („`“ bei 0, „+“, „−“) und Betrag der Tordifferenz (Breite 3). Trennzeichen DGROUP 0x53AA ff.
  Berichtigt in Teil 2: Titel und „*“ werden vom nächsten Kopieren (0x390B) überschrieben – Titel = „   TABELLE “ + Ansicht +
  „ “ + DGROUP 0x7A9 + „.Spieltag“ (zentriert x 5 … 313, Grundlinie 54, Farbe 10, Schatten 28; 0x7A9 = 1 beim Spielstart,
  Datei 0x43A7E), die Zahlenzeile beginnt mit den Siegen. Kopfzeile DGROUP 0x100D ab (10 | 64), Farbe 1; Linien y 66 und 153
  (x 12 … 308). Zeile i: Grundlinie 73 + 7 i, Platz „n.“ (Breite 2) ab x 10 in Farbe 8, Smiley (24 | Grundlinie − 4), Farbe
  30 bzw. 24 für Managervereine. Markierung Datei 0x4FAA7 ff.: 1. Liga nur Platz 1, sonst die ersten DGROUP 0x7B8 + Liga (2)
  in Farbe 15, die letzten DGROUP 0x7B9 + Liga (2) in Farbe 18, x 10 … 20. Bilanz beim Spielstart „--------“.
  **Pixelvergleich** (`emtool screens gamedata <ziel> tabelle-original`, Bild `00_tabelle_1liga_video_13_07` gegen
  `oberflaeche_1307`, Fenster (4 | 38) … (316 | 190), eigenes Bild um (−0,25 | +0,5) verschoben und weichgezeichnet):
  mittlere Abweichung 9,5, 3,6 % der Pixel über 40 (vorher 12,3 / 7,9 %). Bilanz „Heim klein“ am Video: folge21_02638
  (Hauptmenü vor dem 3. Spieltag, Heimsymbol, Gegner EV Landshut) und folge21_02711 (danach: Berliner SC „-----sSs“,
  EV Landshut „-----UnN“).

## Gruppentabellen (Spur `tabellen-original-2`)

Overlay 0xE8 (Ghidra 3D11). Tabellen der Gruppen führt nicht 0x50283, sondern 0x30852 (Datei 0x30852–0x30E7A,
`f(Wettbewerb, Paarungsbasis, Paarungen je Gruppe, Runde)`):

* **Wettbewerb** 0 = WM (zwei Sechsergruppen, Platz = Land − DGROUP 0x6AD − 6 · Gruppe), 1 = Aufstiegsrunden (zwei Vierergruppen,
  Aufruf 0x38691), 2 = Europapokal-Gruppen, 3 = Endrunde (eine Gruppe; Aufrufe 0x3A982 bei Minute 60, DGROUP 0xD0C2 = 3 bzw. 5).
  Gruppengröße DGROUP 0x4DE0 (6/4/4/4), Runden 0x4DD8 (5/3/3/3, Vierergruppen doppelt). Plätze der Vierergruppen in
  DGROUP 0xCFF4 + 8 · Wettbewerb + 4 · Gruppe (0x30427: Europapokal aus der Auslosung 0x5D1B, Aufstiegsrunde aus 0x5CCB + 10/11 und
  + 12/13 je Liga, Endrunde aus Erstem und Zweitem jeder Gruppe 0x305D4).
* **Je Runde:** für Wettbewerb 0 und 1 zuerst 0x50283 (Serien, Tagesform; die Tabellenfelder 0x825A nur bei Code 7/8), dann
  0x303D9: Tore/Gegentore DGROUP 0xCD28/0xCCF8 und Punkte 0xCD58 (Sieg 2, Unentschieden je 1) je Platz (Index Wettbewerb · 12 +
  Gruppe · 6 + Platz). Ergebnisse ≥ 30 (Sieger im Ergebnis kodiert) werden vorher um 30 vermindert.
* **Sortierung** Datei 0x30C36–0x30E73 nach jeder Runde, an Ort und Stelle in der Platzliste DGROUP 0xCD88 (Anfang: Plätze
  0, 1, 2, …, 0x30831): für jeden Platz i alle späteren j; j verdrängt i bei mehr Punkten, bei gleichen Punkten bei höherer
  Tordifferenz (vorzeichenbehaftet), bei gleicher Differenz, wenn i nicht mehr Tore hat (`jna`, Datei 0x30D8F) – voller
  Gleichstand tauscht also. Keine Regel „weniger Spiele“. Umgesetzt in `CupGroup.Standings` (auch `RelegationGroup.Table`).
* **Leser:** WM-Gruppe 0x2F5B6 (Platzliste, Punkte, Tore), Europapokal 0x31CD5, Viertelfinale WM 0x30F8B, Endrunde 0x305D4,
  Europapokalsieger 0x314D6 (Platz 1 der Endrunde).
* **Weitere Aufrufer von 0x50283** ohne Tabelle: Freundschaftsspiel 0x30365 (Code 0xD), DEB-Pokal 0x34B76 (Code 6), Play-offs
  0x315E9 (Code 1; Siege der Serie DGROUP 0xCCA4). Play-off-Setzung 0x31934 aus 0x5CCB (1–8, 2–7, 3–6, 4–5), gerufen von
  0x50B45 nach dem letzten Spieltag – also aus der Gesamtordnung wie im Remake.

## Teamwerte und Tagesform (Spur `spielstaerke-manager`)

Einzelheiten und Messung: [spielberechnung.md](spielberechnung.md) §12.

| Fundstelle | Inhalt | Code |
|---|---|---|
| `0x2EF1B` (Ov 0xE0) | Lader der Teamwerte, einmal beim Programmstart (Aufruf nur aus `0x437D5`) | `MatchEngine.LoadTeamValues` |
| `0x4F527` (Ov 0x128, Stub 0x4D) | Tagesform 0x17 ± random(0, 2·\|Tordifferenz\|), 47 … 53; aus `0x50283` bei Code 7/8 | `MatchEngine.UpdateDayForm` |
| `0x4BDE6` (Ov 0x120, Stub 0x61) | Betrag der Differenz zweier `long` (\|a − b\|) | – |
| `0x4BD35` (Ov 0x120, Stub 0x52) | Wert über Zeiger auf min … max begrenzen | – |
| `0x55506`–`0x55560` (in `0x54C9F`) | Länderwerte Deutschlands neu: Kondition random(84, 94), Technik random(79, 89), Form random(45, 55) je Teil bei `[0x9EDA]` = 0xFD | – (#193) |

## Managerteil der Spielberechnung (Spur `spielstaerke-manager-2`)

Einzelheiten: [spielberechnung.md](spielberechnung.md) §13.

| Fundstelle | Inhalt | Code |
|---|---|---|
| `0x98F8`–`0x9C63` (in `0x977F`) | Reihenwerte der Managervereine aus den eingesetzten Spielern | `MatchEngine.ManagedLineValues` |
| `0x952C` | Teamwerte 0x18–0x1D eines Managervereins aus Reihe 0–2 ohne Spiel, Managerwert DGROUP 0x974B (100 bei < 9 Spielern) | `MatchEngine.ManagedTeamValues`, `LineupTeamValues` (#193) |
| `0x84D7` | Spielvorbereitung (0) und Drittelpause (1): Energie der Reihen und Kaderplätze | `PreparedEnergy`, `PreparedPlayerEnergy`, `RecoveredEnergy` |
| `0x87F7` | Energie je Minute (nur Managerpaarungen) | `OriginalMinutes.DrainEnergy` |
| `0x8E5F` / `0x91FF` / `0x8EF2` | vierte Reihe besetzt? / Reihen nach Energie / Reihenwechsel | `LiveSquad.HasFourthLine`, `LinesByEnergy`, `ChangeLines` |
| `0x80CB` | Linienwechsel-Modus und Torhüter der Computervereine (ungerade Minuten) | `ComputerLineModes` |
| `0x939A` | Einsatz/Angriff nach Spielstand mit Energieabzug (gerade Minuten, beide Vereine) | `OriginalMinutes.FollowTactics` |
| `0x378C5` | Spielerwahl für Strafe und Verletzung | `OriginalMinutes.PenalizedPlayer` |
| `0x38B8F` | **Verletzung im Spiel** (bisher als „Aufstellung/Reihenfolge“ geführt): je Minute je Managerverein mit ≥ 4 fitten Spielern random(0, 20·clamp(60 − M, 0, 40) + 200·`[0xED8]` + 200) = 0 → Spieler 0x378C5, Ereignis 4 (0x130:0x7F), Versicherung 0x20:0xBA9 | – (offen, Teil 3) |
| `0x386D1` | Torschütze/Vorlage der Managervereine | – (offen, Teil 3) |

## Funktionskarte und Abdeckung (Spur `re-abdeckung`)

Spur `re-abdeckung`, #210. Karte aller 678 Funktionen (12 Basissegmente, 17 Overlays) mit Abdeckungsstatus
(umgesetzt / genähert / dokumentiert / unbekannt) aus den Fundstellen in `src/` und `docs/`, die 20 größten Lücken mit
Spurvorschlag und die Einordnung bekannter offener Punkte: **[re-abdeckung.md](re-abdeckung.md)**. Erzeugt mit
`tools/reverse-engineering/funktionskarte.py` (Aufruf dort und in `tools/reverse-engineering/README.md`); nach neuen
Fundstellen neu erzeugen. Neue Fundstellen bitte als Dateioffset mit Kennwort schreiben (`Datei 0x…`, `em.exe 0x…`),
damit der Abgleich sie sicher erkennt.


## Spielerwert 0x49D36 (Spur `spielerwert`)

Spur `spielerwert`, Runde 8. Funktion Datei 0x49D36–0x4A1DA (Overlay 0x118 + 0x3D36, Einsprung `0x118:0x70`, 1189 Byte),
umgesetzt in `Core/Player.Value.cs` (`PlayerValue.Compute`, Formel dort mit Fundstellen). Aufruf
`f(Kaderplatz far*, Art)`; Ergebnis in Drittel-DM (32 Bit, vorzeichenbehaftet).

* **Art** (Bit 0): 0 = Wert/Preis (Kaderplatz +0x2C), 1 = Gehalt (+0x28). Bit 1 (Preis / 3, Datei 0x4A0A7) setzt kein Aufrufer.
* **Eingänge:** Kaderplatz +0x00/+0x01 (5-/2-Minuten-Strafen), +0x03–+0x05 (Tore je Wettbewerb, Gewichte 30/30/40),
  +0x09 Bit 0x80 und +0x16 (Angebot und anbietender Verein, gesetzt im Tagesablauf Datei 0x7358/0x736E), **+0x0E Tendenz**
  (30–68; Anzeige (Wert − 30) / 13 → „Fallend./Konstant./Steigend.“, DGROUP 0xB8C, Datei 0x41AE0; Start random(35, 65) Datei
  0x4907A, täglich auf 30–68 begrenzt Datei 0x7BEA), +0x0F Poolindex, +0x10/+0x11 (Kondition, Technik – die Form +0x12 geht
  nicht ein), +0x1C/+0x1E/+0x20 (Wörter, Gewichte 50/20/30); Spielerpool +0x14 Alter, +0x18 Rollenwert (0 = Torhüter ·1,6),
  +0x1D Verein → Spielklasse 0x34FD5; DGROUP 0xED8 (5 − Level, nur Gehalt). Tabellen DGROUP 0x5312 (Gewichte 58, 4, 4, 5, 4, 25
  bzw. 70, 8, 7, 5, 8, 2) und 0x52C2 (Grundgehalt 2400 / 1800 / 990 Drittel nach Spielklasse 1 / 2 / 4).
* **Zufall:** nur Preis mit Angebot eines Vereins ≤ 63: random(95, 100) (Datei 0x4A0F9); Verein > 63 → · 1,6.
* **Beleg:** eigener 8086-Emulator (nicht im Repo) lief die Originalfunktion samt LXMUL 0x21CA, LDIV 0x227C, LXLSH 0x232A,
  clamp 0x4BD35 und 0x34FD5; 2.000 Zufallsfälle gleich, 30 davon als Goldtest (`SpielerwertTests.Compute_MatchesOriginalFunction`).
* **Aufrufer (#225, RA-01):** 12 Funktionen mit 21 Aufrufstellen (die „13“ in `re-abdeckung.md` ist falsch):

| Funktion | Aufrufstellen (Datei) | Art | Zweck |
|---|---|---|---|
| 0x041AC Spielerdaten-Fenster | 0x48F1 | 0 | Anzeige Wert/Kaufpreis |
| 0x06D7F | 0x6F35, 0x6F65 | 1, 0 | Gehalt (· x / 100 nach +0x28) und Preis eines Kaderplatzes |
| 0x07169 Tagesablauf | 0x72EB, 0x79EC | 0 | Wert / 10000 gegen random(0, 180): Angebot eines Vereins |
| 0x26F20 Saisonende | 0x27D2D, 0x27DF3 | 1, 0 | Gehaltsrechnung mit 147 − Alter; Versicherung zahlt Wert / 2 („Sie zahlt Ihnen … DM“) |
| 0x28165 Spielstart | 0x29EB4, 0x29EE4 | 0, 1 | Startpreis und Startgehalt der Kaderplätze |
| 0x3F833 Werbebewertung | 0x3F9E7, 0x3FA56 | 0 | Ø Wert / 5500, Stars Wert / 7000 (umgesetzt) |
| 0x42CB1 Aufstellung | 0x42F11 | 1 | Gehalt, nachdem die Form aus dem Pool (+0x17) in +0x12 kopiert ist |
| 0x465F1 Transferliste | 0x46E17, 0x47347, 0x47558 | 0 | Preise (Bit 0x40 bzw. Angebot 0x80; Markt = Satz 4, DGROUP 0x7623) |
| 0x494EF | 0x4975C, 0x497A1 | 0, 1 | Preis und Gehalt nach +0x2C/+0x28 eines neuen Kaderplatzes (Kauf) |
| 0x49A3B | 0x49C0F | 1 | Gehalt (Angebot) |
| 0x4A1DB Vertragsverhandlung | 0x4A82C, 0x4AAAB | 1 | Gehaltsforderung; bei +0x30 = 0 zusätzlich Gehalt / 2 (Bedeutung offen) |
| 0x5D04F Highscore | 0x5D26C | 0 | Vermögen + Wert / 2 je Kaderplatz (umgesetzt) |

Einsatz im Remake: Highscore und Werbebewertung (diese Spur); Transfers, Verträge, Saisonende und Aufstellung folgen in
`transfer-original` bzw. `spielerinfo`.

## Statistiken: Ranglisten, Fair-Play, Einzelprämien (Spur `statistiken`, Runde 8)

Bisher in der Funktionskarte „dokumentiert“ bzw. ohne Abdeckung; jetzt `[ORIGINAL]` in `Core/StatisticsPlayers.cs`,
`Core/StatisticsClub.cs` und den Bildschirmen unter `UI/Screens/Statistics/`.

* **Menüsprung** (Ov 0x110, Datei 0x457E1): Index 0–5 über cs:0x2927 → 0x2D5AE, 0x2E356, 0x2C1EE, `0xC8:0x20(0)`,
  `0xE0:0x20(1)`, `0xF8:0x48`. 0x2E356 (Totale) und 0x2C1EE (Früher) sind am Video belegt; „Verein“ ist damit 0x2D5AE.
* **0x25E30** (Ov 0xC8 + 0, Einsprung `0xC8:0x20`, Argument 0 Bildschirm / 1 Kennungen): kopiert zuerst die Tore der
  Managerkader aus Kaderplatz +3 in Spielerfeld 0x1B (0x25E6E); Spielerpool DGROUP `[0x8253]` (33 Byte je Spieler,
  Index 1–199). Schlüssel 0x25F64–0x260B6 (Torhüter: Minuten 0x1F > 399 → −LDIV(LXMUL(Gegentore 0x1B, 6000), Minuten),
  sonst 0xFFFE7961; Feldspieler: 0x1B und/oder 0x1F nach Modus `[bp−0x14]` 0/1/2 = TORSCH./VORLEGER/SCORER),
  Austauschsortierung mit Nebenschlüssel Spiele 0x1C aufsteigend. Ligagrenzen DGROUP 0x7C6/0x7C7 + 2 · Liga (Liga =
  Managerfeld 0x9746). Zeilen: Grundlinie 0x36/0xA1 + 7 n, Farbe 0x18/0x1E nach Vereinsfeld 0x16, Spalten x = 0x26,
  0x72, 0xC8, 0xFC − 15 · Art; Mindestbreite über DGROUP 0x374. Rückgabe `[bp−0x2A] | [bp−0x29] << 8` (Topscorer,
  Torhüter des Managers am Zug) – Aufruf je Manager in 0x5576E, Auswertung in 0x26FE2–0x27007.
* **0x2A1E0** (Ov 0xE0 + 0, `0xE0:0x20`): Vereine ab DGROUP 0x7C0 + Liga, Strafminuten DGROUP 0x82A1 + 0x50 · Verein
  (Wort), Spiele = Bytes 0x8278 + 0x8279 (Heim/Auswärts, wie Tabelle 0x4FD58). Argument 0: 1, wenn Vereinsfeld 0x16 des
  Ersten = Manager + 1 (0x2A29D); Argument 1: Bildschirm.
* **0x26F20** (Ov 0xD0, Saisonende): je Einzelprämie Symbol über 0x20:0x7C6, `+0x493E0` auf DGROUP 0xE289 + 4 · Manager
  (Prämienzähler) und auf das Konto 0x97EA + 0x1AF · Manager.
* **Strafminuten** 0x35967–0x35986: `[0xD0C2] = 7` (Ligaspieltag) → Code & 7 auf 0x82A1 + 0x50 · Verein; 0x5647A löscht
  die Werte der Vereine 0–63 nach `0xD0:0x20`.
* **Knöpfe** 0x4C196 (`0x120:0x75`, x, y, Text, Farbe, 1): belegt einen von zehn Plätzen und zeichnet über 0x4BFF2 das
  Knopfbild 57 × 12 (kb\2 ab (17 | 179 + 12 · Zustand), Zustand immer 0) mit Text Mini zentriert x … x + 59, Grundlinie
  y + 7, Farbe 29, Schatten 1. Die übergebene Farbe wird nicht verwendet.
* **Erledigt in Welle 3:** 0x2D5AE („Verein“) und das Spielerdaten-Fenster aus der Rangliste – Abschnitt „Statistik Verein“.

## Statistik Verein 0x2D5AE (Spur `statistiken-verein`, Runde 8, Welle 3)

Umsetzung: `Core/StatisticsClub.cs`, `UI/Screens/Statistics/ClubStatisticsScreen.cs`. Zuordnung über den Menüsprung 0x457E1
(Index 0) und am Video (`folge01_00522`, `folge01_01022`: Serien, Über-/Unterzahl, Zuschauer, Finanzen, Monat).

* **Texte** DGROUP 0xADC: 30 Fernzeiger (Klartext, nicht verschlüsselt) „!Serien“ … „AUSGABEN(GEHÄLTER...)“; „!“ = Überschrift.
  „(noch keiner...)“ / „(noch keine...)“ über die Zeiger 0xEDD / 0xED9 (0x319B / 0x318B), „G       H       A“ über 0xEE1.
  Der Hinweis „XOR 0xD9/0xC9“ im Arbeitspaket trifft für diese Texte nicht zu.
* **Aufbau** 0x2D5AE: drei Fenster (0x30:0x335, Stil 0, Merker 2): (12 \| 0) 8 × 10, (12 \| 201) 8 × 2, (210 \| 0) 4 × 12;
  Farbverlauf in die Paletteneinträge 0x4C3E[12 − i] (16, 17, 18, 10, 11, 8, 22–28) mit RGB 0x4C24[i] (Grün 0x0C0 → Rot
  0xD00, je 4 Bit · 4) über 0x50:0x63F; Schleife 0x2D6CC–0x2DE7C über die Texte mit x/y-Sprüngen bei 19, 23, 24, 26, 27;
  Überschriften Schrift 2 (0x38:0x6DD) mit Doppellinie, Text Schrift 3 Farbe 1, Werte Farbe 30. Danach (0x2DE7F–0x2E2B5)
  Spaltenköpfe, Rahmen Farbe 6, Streifen, Balken. Kein Warten: die Funktion kehrt nach 0x38:0xDDE / 0x38:0x878 zurück.
* **Zahlenausgabe 0x0B954** (über 0x38:0x2C2 bzw. 0x2D8 mit Drittel → DM): Mindestbreite DGROUP 0x374, fehlende Stellen
  „%“ (Ziffernabstand), bei 0 zuerst „0“; Tausenderpunkt nur bei DGROUP 0x38D = 0 (Komma, wenn [0x79D] = 4 und
  [0x79F] = 0); negativ: „-“ ohne Auffüllen. Der Bildschirm setzt 0x374 = 2 (Serien), 3 (ab Über-/Unterzahl), 8 (Monat)
  und 0x38D = 0 für Zuschauer, Finanzen und Monat. 0x2D57A: Wert 0 → „%-“ (DGROUP 0x4D9F).
* **Serien** (laufend je Verein DGROUP 0xB57B + 0x15 · Verein, Rekord je Manager 0xBABB + 0x15 · Manager; Zeile · 3 +
  Spalte G/H/A): geschrieben nur in 0x50283 (Datei 0x50443–0x50AB4, Wettbewerbscode 7/8), 0x50D4B (Rekord = Maximum nach
  dem Spieltag, Schleife 0x50CDB über alle Manager mit Verein aus 0x966F) und beim Tausch der Vereinssätze 0x36554–0x36593
  – kein Rücksetzen beim Saisonwechsel. Anzeige laufend „(“ Rekord „)“ (0x2D8A3, 0x2D95A).
* **Torquoten 0x2ECA0** (Puffer, Verein, Spaltencode 0x4C4B = 2/0/1, Modus 0 Tore / 1 Gegentore): Tabelle 0x8270 +
  0x50 · Verein (+0/+1 Tore, +4/+5 Gegentore, +8/+9 Spiele Heim/Auswärts), Wert = Tore · 10 / Spiele, „E.Z“.
* **Über-/Unterzahl** (Managerfelder 0x97D2 + 2 k): k 0–3 Tore/Gegentore in Über- bzw. Unterzahl, gezählt nach jedem Treffer
  in 0xA484 (Datei 0xAA2C–0xAAEF, Feldspieler [bp−0x11]/[bp−0x12], Schütze [bp+0xE], Manager [bp−0x13]/[bp−0x14]);
  k 4/5 Zahl der Über-/Unterzahlspiele: Strafwurf 0x35818 (Datei 0x35A84–0x35B18, Differenz der Feldspieler = 1 über
  0x120:0x61), Rücknahme 0x35C7B–0x35CB2, nach einem Tor in Überzahl 0xA939–0xA9A2. Rücksetzen 0x5588E–0x558B2.
  Anzeige 0x2D9D9–0x2DB22: Werte auf 0 … 100 begrenzt, Erfolg k0 · 100 / k4 bzw. 100 − k3 · 100 / k5 (ohne Teiler 0 bzw.
  100), „#“ (DGROUP 0x4DA9) als Prozentzeichen. Die Zählung liegt in der Minutenrechnung (`MatchEngine*.cs`, Spur
  `spielstaerke-manager`); das Remake hat Regeln, Zustand und Anzeige, der Anschluss fehlt noch.
* **Zuschauer** (0x2DB30–0x2DC87): Schnitt 0x97DE / 0x9748, Rekord 0x97E2 mit Gegner 0x976F, Minuskulisse 0x97E6 mit
  Gegner 0x9770 (99.999 bzw. 0 → „(noch keine...)“), Wert auf y + 6, „(Gegner)“ auf y + 12. **0x3EB30** Benötigter Schnitt:
  (0x3E811(1, 1) − 0x3E657(1)) · 12 / 23 · [0x7A1] / [0x79D] / Eintritt 0x973F, negativ → 0. Probe am Video `folge01_00522`:
  (71.733 − 42.697) DM · 3 · 12 / 23 / 3 / 10 DM = 1.514 – wie angezeigt. **0x2E2CA**: ganzzahlige
  Wurzel (Schritte 100, 10, 1). Balken 0x2DFD2–0x2E162: k = Benötigter Schnitt / 1000, Streuung √(Σ |v − k|² / n) ≥ 10,
  Halbbreite aus Streuung, Min und Max, Höhe (v − k) · 19 / r + 19 auf 0 … 37, Linie Farbe 4 bei x = 227 + 3 i.
* **Finanzen** (0x2DC8A–0x2DD13, 0x2E162–0x2E286): Kontostand 0x97EA; Schulden 0x57768(0, 0x63) = Summe der negativen
  Salden gegen die Manager 0 … [0x386] − 1 und die Bank (4); Balken je Pixel 223 … 293: Konto > −2.449.488 + 70.000 · i
  (Farbe 0x4C3E[i / 6]), Schulden + negatives Konto > 25.000 · i (Farbe 0x4C3E[12 − i / 6]).
* **Spielerdaten-Fenster aus Rangliste und Aufstellung:** 0x26B13–0x26BF5 (Linksklick, Zeile (y − Oberkante + 4) / 7, Suche
  der Poolnummer in den Kaderplätzen +0xF aller Manager 0 … [0x386] − 1 und 4, DGROUP 0xA70E vorübergehend auf diesen
  Manager); Aufstellung 0x400E9–0x4010D und 0x4084E–0x40872 (Ereignis 3 = rechte Maustaste, 0x22 < x < 0x60, Zeile aus
  0x41683).


## Spielerdaten-Fenster 0x041AC und Hinweiszeile 0x41683 (Spur `spielerinfo`)

Spur `spielerinfo`, Runde 8. **Zuordnung (Abnahme 1):** Die Funktionskarte nennt beide „Fenster“; am Code und am Video ist es
nur eines:

| Funktion | Datei | Was es ist | Remake | Bildname |
|---|---|---|---|---|
| **0x041AC** (2761 Byte, Basissegment 0x238 + 0x2C) | `PlayerInfoPopup` / `PlayerDataWindow` | modales Fenster „Spielerdaten“ (Ursprung 89 \| 55, Knopf „Aha !“), Argument: Kaderplatz des aktiven Managers (bp+6) | zeichnet `PlayerInfoPopup`, Regeln in `PlayerDataWindow` | `oberflaeche_1115` („HANFT“, Transferliste; Bilder 1113–1119 gleich) |
| **0x41683** (1693 Byte, Overlay 0x108 + 0x16A3, Stub 0x108:0x34) | `PlayerHintLine` | **kein Fenster**: Hinweiszeile unter den Spielerlisten, Zeile und Spalte aus der Mausstelle | `LineupScreen.HoverText` → `PlayerHintLine` | Aufstellung (Zeile unter der Liste, y 213) |

Aufrufer von 0x41683: Aufstellung 0x40077 und 0x407ED (Löschaufruf mit Spalte −1), 0x400E0 (Rand 0xFF, Maske 0x3D146 – andere
Spaltenauswahl, nicht untersucht) und 0x40845 (Ursprung 7 \| 29, Zeilen 0 … Zahl − 1, Rand 0xE9, Maske 0xF0C7); Transferliste 0x4688B
(Löschaufruf) und 0x46A83 (Ursprung 7 \| 29, Rand 155, Maske 0x90C6). Alle Adressen in den folgenden Absätzen sind, wenn nicht anders
bezeichnet, Versätze im Funktionsrumpf (Funktionsanfang = 0). 0x041AC wird nach der Zuordnung der Funktionskarte aus Ranglisten 0x25E30,
Aufstellung 0x3FFE0/0x40364 und Transferliste 0x465F1 gerufen; im Remake öffnet bisher nur die Transferliste es.

### Hinweiszeile 0x41683 (`Screens/Team/PlayerHintLine.cs`)

* **Eingänge:** `(Ursprung x, Ursprung y, erste Zeile, letzte Zeile, Rand, Maske)`; Maus liegt in `[0xC147]` (+0 = y, +2 = x).
  Zeile = `(y − Ursprung y) / 6` (idiv, zur Null hin); Spalte: Versatz `x − Ursprung x`, Summe der Breiten (DGROUP 0x39F: 11, 18,
  57, 13, 12, 12, 14, 36, 12, 14, 12, 12, 13, 27, 40, 16, 35, 38) über die in der Maske gesetzten Bits, erste Spalte mit
  Versatz < Summe. Außerhalb (x < 0 oder > Rand, Zeile außerhalb) wird die Hinweiszeile gelöscht (Versätze 0x8D–0x14B).
  Der Spieler ist Kaderplatz `[0xA70E] · 0x618 + Zeile · 0x34 + 0x5DC3` (0xA70E = Satz des Managers, 0x34 Byte je Platz).
* **Texte je Spalte** (Zeiger DGROUP 0x984 + 4 · Spalte): 0 „Rückennummer des Spielers“ (+ „ (Reihe: n)“, n = +0x15 & 0x7F, wenn
  +0x15 ≠ 0); 1 Art (DGROUP 0x9CC: „Torwart“, „Abwehrspieler“, „Stürmer“, **ohne** Vorsatz; + „ (ausgeliehen)“ bei +0x0C ≠ 0);
  2 „Name: n (a Jahre)“ (Spielerpool +0x00 / +0x14); 6 „Spiele gesamt (Liga und Pokal)“; 7 „Kondition, Technik, Form (Ø)“ mit
  Ø = (+0x10 + +0x11 + +0x12) / 3; 8 „Durchschnittsstärke“; 12 „Tore gesamt (Liga und Pokal)“; 13 „2/5 Min.-Strafzeiten“;
  14 „Status: …“; 15 „Tendenz des Spielers: Fallend./Konstant./Steigend.“ (Index (+0x0E − 30) / 13), nur mit Rand 0xE9
  (Aufstellung) „ (Erschöpfung:n)“, n = +0x13 · 10 / 15; 16 „Dauer des Vertrages mit dem Spieler (in Saisons)“; 17 „Monatliches
  Gehalt des Spielers“; Spalten 3–5 und 9–11 leer.
* **Status (Spalte 14):** +0x0A ≠ 0 (eingesetzt) und +0x15 & 0x7F = 0 → „RESERVE“ (DGROUP 0x964); sonst Kaderplatz +0x09 & 3:
  0 → eingesetzt „IM TEAM“ (0x960), sonst „Hängt halt so rum...“ (0xD40); 1 → „Noch n Minuten Gesperrt.“ (n = +0x0D, DGROUP
  0xC48 / 0x11C1 / 0xB88); 2 → „Verletzt.“ (0xB84) + „(Art)“ (+0x17, DGROUP 0xA00: Schädelbruch, Kahnbeinbruch, Gehirnerschütterung,
  Fingerbruch, Kieferbruch, Meniskuskorbhenkelriß).
* **Zeichnen:** Text mit 0x30:0x885, Auswahllinien um Zeile und Spalte mit 0x30:0x995 (nur wenn die Maustaste nicht gedrückt ist,
  `[0x5A16]` ≠ 1, 3); gemerkt in `[0x5094]` (Spalte) und `[0x5096]` (Zeile).
* **Bisher falsch im Remake:** Spaltengrenzen um 1–4 Pixel verschoben (Aufstellung jetzt 18, 36, 93, 107, 143, 156, 183, 223, 233);
  „Art des Spielers: “ vor der Rolle; „Verletzt. (Noch n Spiele)“; „Fallend./…“ ohne Erschöpfung; Spalte 7 ohne Durchschnitt;
  Spalte 0 ohne Reihe.
* **[APPROX]:** Verletzungsart (+0x17), Sperre in Minuten (+0x0D; das Remake zählt Spiele), Belastung +0x13 (aus der Energie) und die
  Zeilenschritte der Aufstellung (`LineupScreen.RowAt` klickt bei y = 28 + 6 k, die Hinweiszeile wie das Original bei 29 + 6 k;
  Klickfläche nicht Teil dieser Spur).

### Spielerdaten-Fenster 0x041AC (`PlayerDataWindow.cs`, `PlayerInfoPopup.cs`)

* **Kopf:** Name (Spielerpool), „Länderspiele:“ (+0x19), „Alter:“ (+0x14), „Stärke:“ ((+0x10 + +0x11 + +0x12) / 3), Gesicht aus kb\10.
  **Gesicht:** Quelle Spalte `(3 − min((Alter − 18) / 5, 3)) · 17`, Zeile `Talent · 21` (Spielerpool +0x12), Größe 17 × 21, Ziel
  (Ursprung + 0xA7 \| + 0x0F) = (256 \| 70). Beleg: Template-Matching des Gesichts in `oberflaeche_1115` ergibt Spalte 3 (Alter 18)
  und Zeile 8 mit Fehler 12 (nächstbester 23); bisher stand ein Namens-Hash und die Blitzstelle eine Zeile zu hoch.
* **Tore/Spiele:** Zeile „Tore:“ bei Torhütern „GTore:“ (Rollenwert 0, Versatz 0x45D), Spalten Liga/DEB-Pokal/Europacup (DGROUP 0xC0C);
  Torhüter nur die Spalte Liga (Schleife `3 − 2 · [Torhüter]`). Je Zelle `Zahl( n)`: Byte +0x03 + i / +0x06 + i, in Klammern das
  Wort +0x22 + 2 i bzw. +0x1C + 2 i; die Zahlenfunktion 0x1488:0x5C schreibt für 0 „-“. [APPROX] Zähler in Klammern fehlen im
  Remake („( -)“).
* **Daten (6 Felder, DGROUP 0xC18, Sprungtabelle im Funktionsrumpf, Versatz 0xABD):** 0 Kaufpreis = Kaderplatz +0x2C (Bit 15 der oberen Hälfte gelöscht); 1 Marktwert
  = Spielerwert 0x118:0x70 Art 0 (über 0x14C7:0x70); 2 Versicherung = +0x30 (0 → „keine“, sonst Betrag + „ DM“); 3 Sperren
  in Min. = +0x00 · 5 + +0x01 · 2; 4 Gehalt/M. = +0x28; 5 Vertrag = +0x0B Jahre („ Jahr“, bei > 1 „e“), bei +0x0C ≠ 0 „GELIEHEN“.
  Beträge der Felder 0, 1, 2, 4 werden aus Drittel-DM durch 3 geteilt (0x3CA/0x47C mit DGROUP 0x79D = 3) und mit „ DM“ angehängt,
  0 wird zu „-“. **Ausblenden** (Versätze 0x7D6–0x82B): gehört der Spieler (Spielerpool +0x1D = Verein) nicht zum Verein des Managers
  (`[0xA70E · 0x1AF − 0x6991]` = Manager +0x14 = Verein; Vergleich der Vereinsindizes, Befund R8-D01 #253), ist das Gehalt immer 0 und der Marktwert 0, wenn „5 − Level“ (0xED8) < 3. Das erklärt „-“ bei
  Marktwert, Gehalt und Vertrag für fremde Spieler im Videobild.
* **Balken** (Funktion 0xAC9 im Fenster, Wert 1–94 Pixel): Unterzahl = (+0x19 & 0xF) · 94 / 15, Überzahl = (+0x19 >> 4) · 94 / 15,
  Tendenz = (+0x0E − 30) · 100 / 38, Erschöpfung = (100 − +0x13) · 94 / 100. Rahmen: Rechteck Farbe 1, innen Farbe 0, Farbband mit acht
  Abschnitten à 12 Pixel (Farben 16–19, 15–12, DGROUP 0x482A), **Zeiger** senkrecht über die ganze Innenhöhe (Oberkante + 4 bis
  + 12) bei Balkenanfang + 1 + Länge in der Farbe des Abschnitts (Beleg: Zeilen 177 und 184 in `oberflaeche_1115`). Position links
  Ursprung + 7, rechts **+ 0x6C** (bisher + 110), Zeilenabstand 0x13.
* **[APPROX]:** Unterzahl/Überzahl (Kaderplatz-Halbbytes +0x19, im Remake nicht geführt), Belastung, Tendenz auf drei Stufen
  (15 / 50 / 84 statt 30 … 68 → 0 … 100), Versicherung („ja“ statt der versicherten Summe, +0x30), Zähler in Klammern bei Tore/Spiele.
  Das Remake zeigt den Marktwert nur mit Zusammenhang (`PlayerInfoPopup.Show(Player, long, PlayerDataContext)`); der alte Aufruf
  `Show(Player, long)` bleibt (Marktwert „-“, Gehalt sichtbar).

### Pixelvergleich (Abnahme 2)

Ablauf `spielerinfo` (`Tools/Scenarios/Spielerinfo.cs`), Spieler wie im Videobild (HANFT, Alter 18, 16/20/53, Talent 8, 150.000 DM).
Kennzahl wie bei `pixel-verein`: mittlere Graudifferenz / Anteil der Pixel über 40 (beide Bilder leicht weichgezeichnet), Zeiger
im Videobild nicht maskiert; Vorlage `oberflaeche_1115`.

| Bereich | Vorher | Nachher |
|---|---|---|
| ganzes Fenster (95 … 300 \| 60 … 222) | 9,2 / 5,3 % (Versatz 0 \| 0) | 8,6 / 4,8 % (0 \| 0) |
| Gesicht (250 … 284 \| 66 … 98) | 16,3 / 10,8 % (Versatz 0 \| +1) | 8,3 / 4,5 % (0 \| 0) |
| rechte Balken (198 … 304 \| 172 … 212) | 12,1 / 8,0 % (Versatz −2 \| 0) | 11,4 / 8,0 % und 9,9 / 6,6 % (0 \| 0) |

Die Hinweiszeile hat kein Videobild mit lesbarem Text (Zeile in Mini, Aufstellung ohne Maus im Bild); sie ist nur über Disassembly und
Tests belegt (Abnahme 2 für 0x41683: Pixelvergleich nicht möglich, siehe PR).

## Transfermarkt Ov 0x118 (Spur `transfer-original`)

Spur `transfer-original`, Runde 8, Teil 1. Belegt und umgesetzt in `Core/Transfer.Original.cs` (`TransferRules`); Übersicht,
Formeln und offene Punkte in [transfer.md](transfer.md).

* Listenpreis = Spielerwert (Kaderplatz +0x2C, Satz 4 = Markt), Anzeige „(K)“ = +0x2C / 3, „(L)“ = (+0x2C / 3) / 3 (Datei 0x46000).
* Annahme vom Computermarkt **0x4991D** (bisher „unbekannt“): random(75, 85) / random(110, 130) / random(70, 120) um den Preis.
* Unfair-Grenze 60–140 % des frischen Werts beim Gebot an einen anderen Manager (0x47546–0x475DB, in 0x465F1).
* Angebote für eigene Spieler auf dem Markt: Tageswurf 0x072DD (in 0x07169), Höhe 0x47347 (in 0x465F1).
* random(a, b) 0xC037: 16-Bit-Grenzen, Tausch bei b &lt; a. DGROUP 0x4860 wird nie beschrieben (0).

**Teil 2 (Welle 3, Spur `transfer-original-vertrag`):** Fundstellen und Formeln in [transfer.md](transfer.md), „Teil 2“.

* Stubeinträge Ov 0x118: 0x5C → **0x494EF** = Wechsel der Transferliste (nicht „Kaufablauf“), 0x75 → **0x4A1DB**
  Vertragsverhandlung (Aufrufer 0x27D91 Saisonende, 0x40208 Bildschirm „Verträge“ **0x3FFE0**, 0x4786A Kauf).
* **0x49A3B** Annahme eines Vertragsangebots (Laufzeit, Gehalt) – bisher „unbekannt“.
* **0x3FFE0** (Ov 0x108) ist der Bildschirm „Verträge“ (Hinweiszeile 0x400E0 mit Maske 0x3D146), nicht die Aufstellung.
* DGROUP 0xE1A4 = random(85, 115) je Managerzug (0x44E3F); 0x3B7 = 233 nach dem Saisonende (0x26F55); 0x7B6–0x7B8 = 44.
* Kaderplatz +0x18: 1–99 Verhandlungssperre in Tagen, &gt; 99 Verlängerungsangebot (Jahre + 100), Bit 0x80 Ruhestand;
  +0x1A: Gebotssperre (Bit 0x80 \| 1 &lt;&lt; Manager); +0x30 Versicherungsprämie (Wert(Gehalt) / 2); +0x2C Bit 31 Versicherung
  im Dialog abgeschlossen.

## Spielmodus-Optionen 0x2CD1C (Spur `optionen-kalender`, Teil 1: Belege; #228)

Nur Belege aus `em.exe` (Disassembly mit `ndisasm -b 16`); **keine Code-Änderung**. DGROUP beginnt in der Datei bei
**0x16DD0** (Tabelle der Optionstexte: DGROUP 0xE9C liegt in der Datei bei 0x17C6C und zeigt auf „Drittelpause“,
Datei 0x19EB1). Umsetzung in Core samt Migration bleibt Folgepaket `optionen-wirkung`.

**Aufbau der Funktion** (Datei 0x2CD1C, 2142 Bytes, Overlay 0xE0, Hintergrund kb\33): zeichnet drei Rahmen, die
Beschriftungen aus der Tabelle DGROUP 0xE98–0xED4 (Reihenfolge: `Nie.`, Drittelpause, Ergebnisse, Tabelle, Torszenen,
Europapokal, WM-Spiele, Länderspiele:, Zeitung, Hintergrundgrafik, 1.Liga, 2.Liga, 3.Liga, Spielgeschwindigkeit,
„V1.00 - Ende: “, „ (Level “) und 15 Schalter (Index 0–14). Schleife mit Mausereignis `DGROUP 0x5A16`: 1 = Klick
auswerten, 3 = Verlassen (Rückkehr nach Freigabe des Puffers 0x38:0x70A).

**Schalter** (Tabellen in DGROUP, je 15 Einträge): Breite in 8-Pixel-Schritten `0x4BCA` (alle 1 außer Index 9, 13, 14 = 0 bzw.
9: 0), x `0x4BE8` = 130, 144, 158 (dreimal), 24, 188, 204, 218, 39, 54; y `0x4C06` = 222, 222, 222, 252, 252, 252, 282, 282, 282,
200, 184, 214, 200, 244, 220 (Bildschirm-/Fensterkoordinaten, Trefferprüfung `y ≥ Tabelle`, `y < Tabelle + 0x13 − Breite·8`;
`x < Tabelle + 0x17 − Breite·10`). Index 0–8 bilden eine 3 × 3-Matrix (Zeile · 3 + Spalte), Index 9 ist dreistufig,
10–12 sind Schalter, 13–14 zählen über 0xE (`≥ 0xE` wirkt auf die Tabelle `DGROUP 0x120 + Manager` statt auf 0x21D).

**Zustand** (Anfangswerte aus der Datei): `DGROUP 0x21D…0x22B` = 0,0,0,0,0,0,0,0,0, **1**, 0,0,0,0,0 (Index 9 = 1);
`DGROUP 0x120…` = 1,1,1,1,1,69,94,39,73,15,43,0,9,13,10,14,11,15,12,16 (je Manager, vom Schalter ≥ 0xE per `xor 1` umgeschaltet);
`DGROUP 0x262` = 70 (Regler „Spielgeschwindigkeit“: x-Klick 0xBE–0x11D, y 0x54–0x60: `(x − 0xBE) / 12 · 10`, also 0–80 in Zehnerschritten).

**Klickverhalten** (Datei 0x2D32A–0x2D3D0 bzw. Kopie 0x5F7–0x6AE im Overlay):
* Index 9 („Neuer Spielmodus:“, Wert 0/1/2): `inc`; bei 3 → 0; bei 2 → 0, falls `DGROUP 0x5A56 ≠ 0`. Bei Wert 2 außerdem Aufruf
  `0x120:0x4D` (Spielmodus-Abfrage) und `DGROUP 0x226 = 0` (Datei 0x2D39F) – `0x226` ist der Spielmodus (== 2 zeigt die Zusatzzeile
  mit fünf Schaltern, Datei 0x32C).
* Index 0–8 und 10–12: `xor 1` (an/aus); Index 13/14 wie oben für den aktiven Manager (`DGROUP 0xA70E`).
* Nach Index 12: Vergleich mit `DGROUP 0x6AC` (Manager-Statusbyte `0x974E + 0x1AF · Manager`), weitere Folgewirkung nicht verfolgt.

**Leser der Matrix** `DGROUP 0x21D + 3 · Zeile + Spalte`: Datei 0x27AC0–0x27B2F (Spielstart: Summen je Zeile, Schalter ohne
passende Zeilensumme werden auf 0 gesetzt – Konsistenzprüfung), **0x3A8EC** (Ereignisfilter: Zeile aus Parameter, Spalte 0/1 je nach
`Minute == 60` bzw. `> 60 und Parameter 6 == 1`; Wert 0 → Ereignis wird übersprungen; dazu `DGROUP 0x229`, `0x21D`).
Das ist die Auswertung für Meldungen im Live-Ablauf (Drittelpause/Ergebnisse/Tabelle/Torszenen), also Folgepaket `livespiel`.

**Zeitung (DGROUP 0xEB8):** nur Text; kein Code liest den Eintrag (Befund `re-abdeckung`, bestätigt: die Hotspot-Tabellen haben
keinen Schalter mit dem Text). Nicht benutzt.

**Offen / [APPROX]:** Zuordnung der 9 Matrix-Felder zu den Beschriftungen (Zeilen vermutlich Drittelpause · Ergebnisse · Tabelle,
Spalten 1./2./3. Liga) ist aus den Koordinaten geschlossen, **nicht** am Video bestätigt; Wirkung von WM-Spiele/Länderspiele/Europapokal
(Index 10–12 oder Zeile 4–6) und die Folge nach Index 12 nicht belegt. Die Kalender-Teile siehe unten (Teil 2).

### Teil 2 (Spur `optionen-kalender-2`)

**Bildschirm = Einstellungen des Originals.** DOSBox-Bilder 48/49 (`dosbox/nur-weltkugel`, `docs/dosbox-befunde.md`) zeigen genau den
Bildschirm von 0x2CD1C: oben Fenster „Torszenen“, „Hintergrundgrafik“, „Scrolling“, „Spielgeschwindigkeit“ (acht Pinguine), Mitte
3 × 3-Matrix **Zeilen Drittelpause · Ergebnisse · Tabelle, Spalten 1.Liga · 2.Liga · 3.Liga**, unten **DEB-Pokal · Europapokal · WM-Spiele**
(je Pinguin = an, Loch = aus). Damit ist die in Teil 1 offene Matrix-Zuordnung **[ORIGINAL] am Bild bestätigt** (Bild 49: Ergebnisse
1. Liga, Tabelle 1./2. Liga eingeschaltet). Index 10–12 = DEB-Pokal, Europapokal, WM-Spiele ist aus Beschriftung und Bild geschlossen
(**[APPROX]**, Wirkung nicht belegt). Der im Remake bisher angenommene Satz „Original hat keinen Einstellungen-Bildschirm“ war falsch.

**Remake:** `SettingsScreen` behält die Geräteeinstellungen (Lautstärke, Torszenen, Vollbild, automatisch speichern – [APPROX], im
Original nicht vorhanden) und bekommt den Knopf „Spielstand“ mit einer **lesenden** Seite (`GameOptionsView`): Spielmodus aus dem
Spielstand, Matrix immer aus (das Remake kennt die Meldungsfilter nicht; Live-Ablauf: `livespiel`), Pokale aus dem Modus abgeleitet.
Pixelvergleich mit Bild 48 **nicht** gemacht: das Remake zeichnet in seinem Layout (Rahmen, Lampen statt Pinguine), die
Pinguin-/Loch-Grafiken und die Fenster des Originals sind nicht nachgebaut; Bildschirmfoto `einstellungen/03_spielstand_optionen`.
Wirkung der Spielstand-Optionen in Core samt Migration: Folgepaket `optionen-wirkung`.

**Kalender (Einordnung, Disassembly `ndisasm -b 16`).**

| Adresse | Befund |
|---|---|
| 0x43B1F (206 B) | **Datumstext**: Wochentag (Tabelle DGROUP 0xAA0, Index `(Tag + 5) mod 7`), Trenntext (DGROUP 0x521F), Tag als Zahl (0x38:0x2C2), Trenntext (0x5222), Monatsname (Tabelle 0xA70, Index DGROUP 0x5A1C), dann Jahr (DGROUP 0x5A18); Rückgabe Zeiger auf den Puffer. Kein Bildschirm. Das Remake formatiert Datumstexte in Core (`docs/saison.md`, „Kalender“). |
| 0x43C38 (489 B) | Fensterzeichner (Aufrufe 0x30:0x800, 0x50:0x3B0/0x545, 0x30:0x995 je Zeile); gerufen von 0x0B155. Gehört zum Terminfenster 0x43E21; nicht im Video belegt. |
| 0x43E21 (1983 B) | Terminübersicht („Saisonende“, „Spielfrei“, „ Noch keine“, „ DM“). **Kein Referenzbild** gefunden (`docs/referenz-videos.md` führt keinen Kalenderbildschirm; Suche nach „Kalender“/„Terminübersicht“ in der Doku ohne Treffer) → **kein Bildschirm gebaut**. |
| 0x445E0 (1128 B) | Text „Vor dem ersten Spieltag.“ und „.Spieltag“: Infoleiste (Tabellenkurve), im Remake vorhanden (`docs/anforderungen/bildschirme.md`). |
| 0x36169 (1476 B) | ohne Texte, Aufrufe aus Spielstart, WM und Pokal: Termin-/Paarungserzeugung vermutet; **nicht weiter verfolgt** (Folgepaket, falls gebraucht). |

Die Tiefe bei 0x43C38, 0x43E21 und 0x36169 ist bewusst gering (nur Funktionsrumpf und Aufrufe angesehen); die Zuordnung ist
**[APPROX]**. Änderungen am Tagesablauf: keine vorgeschlagen.

(Index 10–12 oder Zeile 4–6) und die Folge nach Index 12 nicht belegt. Die Bildschirm- und Kalender-Teile (0x43E21 mit 0x43B1F,
0x43C38, 0x445E0, 0x36169; Pixelvergleich) sind **nicht begonnen**.

## Nationalteam, Welle 3 (Spur `nationalteam-2`, Runde 8)

Dateioffsets in `em.exe`, DGROUP = Datei 0x16DD0. Umsetzung und Belege im Bild: `docs/wettbewerbe.md`, Abschnitt
„Nationalteam, Welle 3“.

* **Tagesfunktion 0x54C9F** (Ov 0x130, Aufrufer 0x437D5): Modus 1 (DGROUP 0xD0B9 = 1) springt am Anfang (0x54CEE–0x54D0E)
  bis zum Termin mit Ereignis 0x10, setzt DGROUP 0xD0BA = 1 (0x54D19) und geht ans Tagesende 0x553FF. Dort setzt es
  Vereinsfeld 0x16 Deutschlands (Satz 0x3D Byte ab far DGROUP 0x9657, Land DGROUP 0x6AC) auf 0 (0x5540E), ernennt
  (0x55436 ff., Feld 0x16 = 6 bei 0x55494) und ruft für Manager 5 an einem WM-Tag das Hauptmenü 0x110:0x66 = 0x44E22
  (0x555A3). Das Hauptmenü wird also aus der Tagesfunktion gerufen; nach „Los jetzt!“ gibt es kein Menü vor dem ersten Tag
  (#195). 0x55752 setzt Feld 0x16 wieder auf 0.
* **Spielvorbereitung 0x8365**: Vereinsfeld 0x16 = 0 → Einsatz- und Angriffszeile ohne Blick auf die Modi (0x842B–0x8443,
  0x8481–0x8496); sonst Modus des Managers DGROUP 0xA6FA/0xA706 + Feld 0x16 (R8-B04).
* **Seitenwähler 0x3A88A** (Stub 0xF8:0x3E, Argumente Seite, Wettbewerb, Minute): Sprungtabelle cs:0x12FB (Datei
  0x3ACCB) für die Seiten 1–12 – 1 und 6 Übersicht 0x32E23 (0xE8:0x75 mit Wettbewerb 2 bzw. 0), 2–5 Europapokal (0xE8:0x6B; Zuordnung
  im Einzelnen nicht geprüft), 7 Ligapaarungen (0x128:0x39), 8/9 WM-Paarungen Gruppe 1/2 (0xE8:0x34 = 0x2FC2F, Spieltag DGROUP 0x7AE), 10 WM-Endrunde
  (0xE8:0x75 = 0x32E23, Wettbewerb 3; Rückgabe ≠ 0 → vorige Seite, 0x3AC75), 11/12 WM-Tabelle Gruppe 1/2 (0xE8:0x25 =
  0x2F5B6). Startseiten je Kachel DGROUP 0x511F … 0x5127 (Paarungen → WM: 8, Tabellen → WM: 11); die Rückgabe wird dort
  gemerkt (0x45862, 0x458BB).
* **Paarungsansicht 0x2FC2F** (Argumente Gruppe, Minute, Spieltag): Aufbau im Klassenkommentar von `CupPairingsScreen`.
  Teams DGROUP 0xCED8 + 0x24 · Gruppe + 6 · Spieltag + 2 · Begegnung + Seite, Tore DGROUP 0xCDB8 (gleicher Index). Knöpfe
  über 0x30:0x2C6, Pfeile über 0x2FB31 (Hit-Test 46 × 38, gedrückt Quelle + 10). Klick auf eine Flagge: 0x128:0x2A.
  Im Spielablauf (Minute ≠ 0) ruft 0x32E23 die Ansicht für beide Gruppen (0x32E52–0x32E70).
* **WM-Statistik 0x3ADE1** (kb\12 kleine Flaggen 9 × 6, Index Satzfeld 0x4C; Flaggenleiste 0x2F530): Aufbau im
  Klassenkommentar von `WorldCupStatisticsScreen`. Die Platzierungen stehen im Satz DGROUP 0x8257 + 0x50 · Verein ab Feld
  0x34 (Bytefolge, 0 beendet sie; 0x8257 ist der Block „Vereinssatz der Tabelle“ im Spielstand). Bei der WM-Auslosung
  0x35EDD werden die Sätze 0x8257, die Vereinssätze 0x9657 und die Serien DGROUP 0xB57B zweier Plätze getauscht
  (0x36467–0x3653A) – darum führt jeder Satz die Flaggennummer selbst. Nicht gefunden: die Stelle, die die Platzierungen
  nach einer WM schreibt.

## Spielberechnung abschließen (Spur `spielberechnung-komplett`, Runde 9)

Teil 1 (R8W3I-15, #204): Vereinstausch 0x36169 (Schalter 0x80 = nur Kennung, sonst ganzer Vereinssatz mit Tabellensatz
und Serien), Ligamischung 0x35EDD (je Liga 55 Tausche), Auf-/Abstieg 0x562F2/0x5636D, Spielstart 0x29C78–0x29D9C
(Managerverein auf einen Platz der Start-Liga), Play-offs 0x31934/0x315E9/0x31505 („best of five“, Heimtausch, drei
Runden), Spielerschreiber 0x570E/0x49723 (keine Teamwerte). Einzelheiten: [spielberechnung.md](spielberechnung.md) §14.
Ursache von #204: **Wanderung der Teamwerte 0x9D1B** (Basis 0x62C:0x1C5B; Aufrufe Spielstart 0x43A73 und Saisonwechsel
0x56448 mit Modus 10, Tageslauf 0x55292 mit Modus 1 an Tageszähler 0x3B7 ≡ 0, 4 mod 7), Liga 0x34FD5, Grenzen DGROUP 0x125,
Stärke-Anzeige 0x4F1F1 ((0x18 … 0x1D) / 6). Weiterer Schreiber 0x58DC (Basis 0x238:0x175C, Transferbildschirm 0x46FB3/0x47483:
Käufer gewinnt Kondition/Technik, nicht umgesetzt). Die Suche in §12 von spielberechnung.md hatte 0x9E46–0x9F31 0x98F8
zugeordnet; sie liegen in 0x9D1B. Einzelheiten: [spielberechnung.md](spielberechnung.md) §15.
Teil 2 (R8W3I-03 bis -06): 0x378C5 höchstens 150 Züge (Prüfung 0x37A30 vor dem Zug 0x378FC); kein Spieler (100) →
0x35B33 → 0x3583B ohne Anzeige und Kette; Managerwert 0x974B nur von 0x952C mit Schalter 1 (Tageslauf 0x54F9C–0x54FBF),
100 unter neun Spielern (0x9698); Nationalteam (Feld 0x16 = 6) zieht über 0x35AB8–0x35B2C ebenfalls 0x378C5. **0x38A36**
(aus 0x952C an 0x9567, Stub 0x149D:0x8E): Spieler auf dem Eis je Teil (0x388F6), DGROUP 0xCC35, Vereinsfeld 0x32 und
0x2C Bit 3 – keine automatische Aufstellung. Einzelheiten: [spielberechnung.md](spielberechnung.md) §16.
Teil 3: **Verletzungen 0x38B8F** (je Minute der Managerpaarung vor den Torchancen, 0x37F06/0x38304) mit Szene 0x53C57
Ereignis 4 → **Verletzung 0x05B1A** (Tabellen DGROUP 0x181 Wochen, 0x1B9 Häufigkeit, 0x483B schwere Arten; auch aus dem
Tageslauf 0x07169 an 0x74A9), **Heilung 0x7C12** (wöchentlich), Versicherung 0x07089 (Zahlung +0x30 · 4, nicht umgesetzt);
**Torschütze 0x541D9 → 0x386D1** je Abschluss der Managerpaarung; Zählung 0x4BE17 (Modus 0 belegt, 1 Aufstellung, 2
Reserve); Gruppe 0x4180 (Rollenwert / 25 ≤ 2). Einzelheiten: [spielberechnung.md](spielberechnung.md) §17.
