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
* **Startordnung** Datei 0x2F1A2: beim Spielstart Vereinsindex aufsteigend. Andere Schreiber von 0x5CCB: nur die
  Sortierung und 0x35EDD (WM-Reihenfolge). Über den Saisonwechsel bleibt die Liste stehen.
* **Tabellenansicht** Datei 0x4F78D: Titel aus DGROUP 0x1011 („   TABELLE “) + Ansicht (Zeiger 0x1001: „Heim“, „Gesamt“,
  „Auswärts“) + Spieltag; Name x 0x21, Bilanz x 0x75 (immer die Gesamtbilanz +4, gleich in jeder Ansicht), Tendenz über
  `0x110:0x4D`, Zahlenzeile ab x 0xA6 als ein Text: „*“, Siege, Unentschieden, Niederlagen (Mindestbreite 2,
  DGROUP 0x374), Punkte „P:M“ mit M = 2 · Spiele − P (bei DGROUP 0x7C5 ≥ 3 statt M ein Leerzeichen), Tore „T:G“
  (Breite 3), Vorzeichen („`“ bei 0, „+“, „−“) und Betrag der Tordifferenz (Breite 3). Trennzeichen DGROUP 0x53AA ff.

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
* **Offen:** 0x2D5AE („Verein“, 3356 Byte), Spielerdaten-Fenster aus der Rangliste (0x26B13 → 0x041AC).
