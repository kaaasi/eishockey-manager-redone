# Archiv: Runde 19 – Torszenen, neuer Manager, Belastung, Auswertung DOSBox `runde-19` (14.–15.11.2026)

Aus `docs/arbeitspakete.md` ausgelagert am 15.11.2026. Inhalt unverändert bis auf Überschrift, „Ergebnis“, „Abschluss“ und
„Entscheidungen während der Runde“, ohne „Zurückgestellt“ und „Vormerkungen“ (bleiben in `arbeitspakete.md`). Die Statuszeilen
der Spuren geben den Stand vor dem jeweiligen Merge wieder. Reviews: `docs/reviews/` (Dateien vom 14.–15.11.2026). Nur bei Bedarf
lesen. Relative Links beziehen sich auf `docs/`.

**Ergebnis:** Spuren `neuer-manager` (#854), `torszenen` (#856), `re-belastung` (#855), `pruefung-karte` (#863) und
`abgleich-dosbox-19` (#866) gemergt; Plan #851 (Plan-Review #852), Entscheidungseinträge #853, #865, #868. Live-Spiel vollständig
(L1–L8): Torszenen mit Figuren und Eisfläche, Abspieler 0x567C8, Anzeigeteil 0x53C57 im Live-Bildschirm, Wiederholung ohne neuen
Zug (R19-E1 = A). Behoben bzw. geschlossen: #847 (Platztausch des neuen Managers in die Start-Liga, Teamwerte aus der Aufstellung),
#826 (Highscore „nur Weltkugel“), #857, #859, #862, #838 (Messwert bestätigt ⅓ s je Minute), #730 („im Bereich“ nach
DOSBox-Gegenprobe FILE66–70; Auswertefehler Runde 18 berichtigt). Gesamtreview (#873, ChatGPT/Codex) **Ja** für `c4fe55a`:
keine neuen Produktfehler, Testlücke Bestenliste am Spielende (#874).

**Abschluss (15.11.2026):** offen #871 (P3 aus Review #870: pyc-Artefakt, Drift-Wächter ZIMMERMANN, doppelte Teamwert-Rechnung,
Bezugsbasis), #872 (Reihe auf dem Eis bei Spielbeginn), #874 (Testlücke Bestenliste), A093 (mit `runde-19` nicht belegbar).
Funktionskarte neu erzeugt (R867 P3-1).

**Entscheidungen während der Runde:** Nutzer: R19-D1 = A, R19-D2 = C, R19-D3 = B mit Claude Fable 5.2 (Fehlschlag → A),
R19-E1 = A (Wiederholung der Torszene ohne neuen Zug), R19-E2 = A (DOSBox-Liste `runde-19` statt Schließen), R19-E3 = A
(Auswertung `runde-19` noch in Runde 19 als Spur `abgleich-dosbox-19`), R19-E4 = A (DOSBox-Gegenprobe FILE66–70). Leitstelle
nach Prozess: Mergefolge `torszenen` vor `re-belastung` (nur Doku, konfliktfrei, kein zusätzlicher Integrationstest); Läufe vom
14.11. nach Nutzungslimit am 15.11. neu (Nutzer startete zwei davon von Hand); Entscheidungseinträge #853/#865/#868 ohne eigenes
Review, Prüfung im Gesamtreview.

---

## Aktuelle Runde: Runde 19 – Torszenen, neuer Manager, Belastung (Plan 14.11.2026, Go offen)

Runde 18 abgeschlossen am 14.11.2026: [archiv/runde-18.md](archiv/runde-18.md). Grundlage: Spur C `torszenen` nach
[analyse/re-livespiel.md](analyse/re-livespiel.md) §12 (L7-Anzeige; Kern-Zug 0x575C3 und Leser `tore\` aus `livespiel-kern` A4
vorhanden, §11 Format, §6 Anzeigeteil), offene Issues #847 (P2), #826 (P3), #730 (P2, Rest). Ziel: Live-Spiel vollständig (L1–L8),
#847, #826 und #730 belegt geschlossen. **Entscheidungen Nutzer 14.11.2026** (Optionen im Plan-PR #851): R19-D1 = A (eine Spur
`torszenen`, C1 → C2 → C3), R19-D2 = C (#847 eigene Spur, #826 an `pruefung-karte`), R19-D3 = B mit Claude Fable 5.2, bei
Fehlschlag A (Prüfauftrag `re-belastung` in em.exe; findet er die Ursache nicht, DOSBox-Liste `runde-19`). **Go Nutzer 14.11.2026**
(alle Spuren und Folgeläufe, Modelle wie unten). Plan-Review #852 Ja (P2-1 → R19-E1 = A, P2-2 und P3-1…5 eingearbeitet).

**Base für alle Spuren:** aktueller `main` (Planstand `926aeab`); tatsächlichen Base-SHA in den Draft-PR. Regeln wie Runde 18
(Archiv): ein Branch `paket/<spur>`, ein PR, Teile als Commits, höchstens drei Fortsetzungen; je ID zuerst prüfen, ob sie noch
offen ist; Befund reproduzieren, roter Regressionstest mit Fundstelle, dann grün; Mehrsaisonprobe bei Saisonwechsel, Kader oder
Geld; Schema-Änderung mit Migration in neuer Datei `Core/SaveGame.Migration.<Spur>.cs` (Leitstelle nummeriert beim Merge);
`[ORIGINAL]`/`[APPROX]` mit `Datei 0x…`; fremde Tests nach R15-E2 = C; keine geplanten Aufgaben aus Läufen (R15-E4). Bei
Unklarheit „wie im Original“, sonst `[APPROX]` mit Begründung, die Leitstelle bündelt die Fragen. **Entwickler und Erstreviews
sind Claude-Sitzungen;** ChatGPT nur fürs Gesamtreview.

**Grundsätze Torszenen:** Die Szene ist reine Anzeige. Alle Zufallszüge (Szenenwahl 0x575C3) zieht weiter der Kern nach
R18-D3 = A; der Abspieler zieht nichts und ändert keinen Spielwert (`AGENTS.md` §4). Ein Spieltag mit Torszenen ergibt dasselbe
wie ohne. Zeit vergeht weiter nur über `GameState.Continue()`; Bilder je Szene sind ein Modell (wie `LiveClock`), die Wanduhr
macht nur der Bildschirm.

**Folge (R19-D1 = A, R19-D2 = C, R19-D3 = B):** Welle 1 parallel `torszenen` (C1 → C2 → C3 nacheinander), `neuer-manager` und
`re-belastung`. Merge `neuer-manager` → `re-belastung` → `torszenen`. Welle 2: `pruefung-karte` (`paket/pruefung-karte`), am Rundenende Gesamtreview (ChatGPT).

### Zuständigkeit für gemeinsame Dateien

- `Formats/GoalScene.cs`, `Core/MatchEngine.GoalScene.cs`, neu `UI/Screens/Live/GoalScene*.cs`, `UI/LiveWidgets.cs`,
  `UI/Screens/Live/LiveMatchScreen.cs`, `UI/Screens/Live/LiveSession.cs`, `Tools/Scenarios/Live.cs`: allein `torszenen`.
  `Core/MatchEngine.Minutes.cs`, `MatchEngine.Live*.cs`, `LiveClock.cs`: nur lesen; braucht C3 dort einen Anschluss
  (z. B. Kennzeichen „Szene anzeigen“ am Torereignis), minimal und im PR mit Fundstelle begründet.
- `Audio/**`: nur `torszenen`, nur Abspielen der Effekte je Bild (Byte 3), keine Änderung bestehender Töne.
- `Core/GameState.AddManager.cs`, neu `Core/GameState.AddManager.Swap.cs` (o. ä.), Vereinstausch (Datei 0x36169, Umfang laut
  Teil 1): allein `neuer-manager`; `GameState.Integration.cs`, `GameState.cs` höchstens minimal, im PR begründet.
- `Core/Squad.Training*.cs`, `Core/MatchEngine.Managed.cs`, Reihenwechsel in `Core/MatchEngine.Minutes.cs`/`OriginalMinutes`,
  `tests/**/AbgleichOriginalPaareTests.cs`, `docs/analyse/belastung*.md`: allein `re-belastung`.
- `Core/Highscore.cs` (#826), `docs/analyse/inventur.md`, `docs/re-abdeckung.md`, `tools/reverse-engineering/*`,
  `tests/**/Support/`: allein `pruefung-karte`; die anderen Spuren nennen ihre Inventurzeilen im PR.
- `docs/analyse/re-livespiel.md`: neuer Abschnitt §16 nur `torszenen`; `docs/reverse-engineering.md`: je Spur ein eigener
  Abschnitt, die Leitstelle löst beim Merge auf.

### Spur `torszenen` – Abspieler und Anzeigeteil der Torszene (Spur C, L7)

- **Teile:**
  1. **C1 Figuren und Eisfläche (Analyse + Leser):** Zeichnen `48h:4ECFh` ff. und Bildtafel der Figurnummern 0 … 93 lesen
     (Kandidat `kb\233…235` 32 × 32 über eine Tabelle in `kb\246`, `re-livespiel.md` §11); Größe und Aufbau der Eisfläche,
     Zuordnung zur Halle `kb\241…244` (Review #795 P2-1), Scrollen der Kamera. Ergebnis in `re-livespiel.md` §16 und im Leser
     (`Formats/GoalScene.cs`: Figur → Bildquelle). Abnahme: `[GameDataFact]` Figur → Bild für alle in den 48 Dateien
     vorkommenden Nummern; Eisfläche als Bild gerendert, Ausschnitt gegen `eisbaeren_f01_01150` vermessen.
  2. **C2 Abspieler 0x567C8 (Modus 0–3):** Bilder `kb\231 + i` mit Hallenstufe (i = 5 → `kb\241…244`, 12 → `kb\246`, 13 →
     `kb\247`), Kopfzeile 0x56EDD/Minutenzeile 0x570E8, Trikotfarben 0x53BF9 und Palette 0xFE/0xFD, Bildschleife (Kamera,
     Puck, 12 Objekte, Bewegungsbyte), Töne je Bild (Byte 3), Torzahl + 1 und Kopfzeile Modus 2, Pause 6 – vorher den
     Widerspruch §6/§11 zu „Pause 6“ und den Modusnamen an Datei 0x56E14–0x56E29 auflösen (R852 P3-1); neu
     `UI/Screens/Live/GoalSceneScreen.cs` (bzw. `GoalScenePlayer.cs`). Speichermangel-Zweig (Modus 0) entfällt mit Begründung
     `[APPROX]`. Abnahme: zuerst Szene und Bildindex von `eisbaeren_f01_01150` bestimmen (R852 P3-2; ist es nicht `2.ht`, die
     gezeigte Szene nehmen), dann abspielen und Pixelvergleich mit Fundstelle; Kopfzeile
     pixelgenau (Textpositionen §6); Einheitstest Entzerrung (Summe < 6 → XOR 0xFFF); Ton je Bild wie Byte 3.
  3. **C3 Einbindung Anzeigeteil 0x53C57 (Datei 0x53DCA–0x5406A):** Aufruf nur für Art 0/1 und Torszenen an (`[0x226]`),
     Dauer-Eisfeld `0x5A55` (Modus 2), Lampe/Schnelldurchlauf schaltet Torszenen aus (A2); Pause 30/20, Zuschauerzeile
     („ZUSCHAUER“/„AUSWÄRTS“/Länderteam leer), Spielstand aus dem Ergebnisbyte (bei Tor − 1), Wiederholung `r`/`w` bis dreimal
     (`DGROUP 0xCC35`; R19-E1 = A, Nutzer 14.11.2026: die Wiederholung zeigt dieselbe Szene noch einmal ohne neuen Zug, `[APPROX]`
     mit Fundstelle – das Original zieht neu; originalgetreu erst mit dem bitgenauen Zufallsmodus nach v1.0),
     danach Torschütze, Reporter und Anzeigetafel wie bisher. Abnahme: Ablauftest ganzer Spieltag mit Managerpaarung, Torszenen
     an gegen Torszenen aus bei gleicher Darstellung ≠ 0 (der Kern zieht nur bei `LiveMode ≠ 0`) = gleiches Ergebnis; Bildschirmfoto-Ablauf `live-torszene`;
     Mehrsaisonprobe unverändert grün.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** C1, C2 und C3 fertig (PR #856, 15.11.2026), Erstreview offen.

### Spur `neuer-manager` – Platztausch in die Start-Liga mitten in der Saison (#847, R18-E5 = A)

- **Ziel:** Beim Hinzufügen eines Managers über „Neuer“ mitten in der Saison den Verein wie im Original in die gewählte Start-Liga
  tauschen (Datei 0x29C3B–0x29CF2, Tausch 0x36169). Ausgangslage `spielberechnung.md` „Vereinstausch 0x36169“ (Z. 460 ff.): mit
  Schalter 0x80 nur Kennung (Bytes 0x00–0x16, 0x34, Wort 0x3B), Spieler und Managerzuordnung, Teamwerte, Tabellensatz und Serien
  bleiben am Platz (R852 P2-2); ob mitten in der Saison weiteres (Tabelle, Spielplan, Pokale) betroffen ist, nur mit Beleg.
  `[APPROX]` in `GameState.AddManager.cs` entfernen.
- **Teile:** 1. Umfang belegen: 0x29C3B–0x29CF2 im Hinzufügen-Modus und 0x36169 (Schalter, Bedingungen, Gleichstand), Vorhandenes
  aus `spielberechnung.md` nur prüfen und ergänzen; 2. Umsetzung im Kern mit Regressionstest (rot vor Fix) und Proben: Tausch in jede Start-Liga, Verein
  schon in der Start-Liga (kein Tausch), Bundesliga/2. Liga/Oberliga, nach dem Laden (atomar wie #842 P1-1); 3. Mehrsaisonprobe
  (4 Saisonwechsel, passiver und aktiver Manager, mit einem mitten in Saison 1 hinzugefügten Manager).
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Teile 1–3 fertig (PR #854), wartet auf Erstreview.

### Spur `re-belastung` – Ursache #730 Spieltag 2/3 in em.exe (R19-D3 = B)

- **Ziel:** Ursache der Belastung an Spieltag 2/3 in em.exe finden und beheben; Ansatz ist die Tabelle in
  `analyse/abgleich-dosbox.md` §1.3 (Abweichungen je Reihe in beide Richtungen: Sturm 1 +32 gegen +16,8, Sturm 2 +5 gegen +13,7,
  Abwehr 2 +15 gegen +8,8, Sturm 3 −3 gegen +10,8), nicht die Textfolgerung (R852 P3-3). Kandidaten: Eiszeit/Reihenwahl je Minute bei Auswärts- und
  Heimspiel, Training und Erholung zwischen den Spieltagen (Datei 0x6B08–0x6B1A, `EnergyNorm`), Torwart/Reihen nach
  Taktik-Minutenvorgabe; Vorarbeit `analyse/belastung-staerke.md`, `belastung.md` (Runde 16) nicht wiederholen.
- **Teile:** 1. Analyse mit ausführbarem Beleg (Quasi-Paar FILE50→FILE03, Reihensummen wie Bericht §1.3; roter Test oder Probe
  mit Fundstelle), Abschnitt in `belastung-staerke.md`; 2. Fix mit Regressionstest (Abnahme #730: FILE02 → 3 Spieltage, HOCK ≤ 70,
  Eintrag aus der erwarteten Liste entfernen), Abgleich-Paare und Mehrsaisonprobe (4 Wechsel) grün.
- **Fehlschlag (R19-D3 → A):** findet Teil 1 keine belegte Ursache, Bericht mit geprüften Stellen, Label `spur-wartet`, kein
  Teil 2; Bericht-PR wird nach Erstreview gemergt, #730 bleibt offen, die Leitstelle gibt die DOSBox-Liste `runde-19`
  (`vorlagen/dosbox-aufnahme.md`) frei (R852 P3-4).
- **Modell:** Claude Fable 5.2 (Nutzer 14.11.2026). **Erstreview:** Claude Opus 5.5. **Status:** Teil 1 fertig ohne belegte Ursache (PR #855, `analyse/belastung-staerke.md` §9: Auswertefehler der Tabelle, Einzelreihen im Bereich des Remakes, Paarsumme FILE02→50 und Verteilung Sturm 1/3 in FILE02→03 außerhalb aller Saaten), kein Teil 2; Fixlauf nach Erstreview #860 (P2-1, P3-1 bis P3-3) erledigt. **R19-E2 = A** (Nutzer 15.11.2026): #730 bleibt offen, DOSBox-Liste `runde-19` freigegeben.

### Spur `pruefung-karte` (Welle 2)

- **Inhalt:** #826 (Highscore „nur Weltkugel“: Punkte Σ 9 − Platz nach FILE54, roter Test mit Beleg aus dem Bericht
  `abgleich-dosbox` §3); Inventur „Stand Runde 19“, Funktionskarte (L7 umgesetzt), gesammelte P3 der Runde.
- **Voraussetzung:** `neuer-manager`, `re-belastung` (auch als Bericht im Fehlschlag) und `torszenen` gemergt.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Teile 1–3 fertig (PR #863): #826 Highscore „nur Weltkugel“ (Datei 0x5D59B), #857 Kennung nach Platztausch (`ClubIdentity`, Export und Trikotfarbe), #859/#862 Tests, `abgleich-dosbox.md` §1.3 → §9; Inventur „Stand Runde 19“ (A460–A481), Funktionskarte 7988 Zuordnungen. Wartet auf Erstreview.

### Spur `abgleich-dosbox-19` – Auswertung Aufnahme `runde-19` (Welle 3, R19-E3 = A)

- **Entscheidung Nutzer 15.11.2026:** R19-E3 = A – Aufnahme `runde-19` liegt vor (Referenz-Repo #35–#38), Auswertung noch in
  Runde 19, Gesamtreview danach.
- **Teile:**
  1. **#730 und A093:** FILE50 → FILE61 (Spieltag 2) → FILE63 (Spieltag 3) mit Reihenbildern je Minute gegen das Remake
     (`hits.py`, Kontrast Sturm 1/3 und Paarsumme nach `belastung-staerke.md` §9); Ursache mit ausführbarem Beleg finden und
     beheben (roter Test → grün, Abnahme #730) oder mit Beleg „im Bereich“ schließen; A093 (Technik −10) mitbelegen.
  2. **#838 Echtzeit „Spiele laufen“:** Messwert der Aufnahme (Geschwindigkeit 0/60, DOSBox-Zyklen) statt ⅓ s je Minute; Takt
     belegt oder `[APPROX]` mit Messwert.
  3. **#847 Gegenprobe:** FILE64 → FILE65 gegen den Platztausch aus #854 (Tabellen, Kennung, Spieler); Abweichung → roter Test
     und Fix, sonst Abgleich-Paar ergänzen.
- **Dateigrenzen:** `Core/Squad.Training*.cs`, `Core/MatchEngine.Managed.cs`, Reihenwechsel in `MatchEngine.Minutes.cs`/
  `OriginalMinutes`, `Core/LiveClock.cs` und Desktop-Bildtakt (nur #838), `Core/GameState.AddManager*.cs` (nur #847),
  `tests/**/AbgleichOriginalPaareTests.cs`, `docs/analyse/abgleich-dosbox.md` (neuer Abschnitt Runde 19),
  `docs/analyse/belastung-staerke.md`; `docs/analyse/inventur.md` nicht (Zeilen im PR, Leitstelle trägt nach). Mehrsaisonprobe,
  wenn Kader oder Saisonwechsel berührt sind.
- **Voraussetzung:** keine (parallel zum Erstreview von `pruefung-karte`); Merge nach `pruefung-karte`.
- **R19-E4 = A (Nutzer 15.11.2026):** Teil 1 (#730) wartet auf die DOSBox-Gegenprobe `runde-19` Sitzung 4 (FILE66–70); danach
  Auswertung als Fortsetzung der Spur, Erstreview erst danach.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Go 15.11.2026.
  **Stand 15.11.2026 (PR #866, Draft):** Teil 2 (#838: ⅓ s je Minute mit Messwert 15,9–20 s belegt, `[APPROX]`) und Teil 3
  (#847: Gegenprobe FILE64→65, Teamwerte des Neuen aus der Aufstellung 0x952C, Datei 0x2A073, behoben) fertig. Teil 1 (#730)
  nach R19-E4 = A (Fortsetzung 1): Reihenwahl für Spieltag 1–3 belegt; fünf Läufe von Spieltag 2 (FILE66–70) im
  Mittel 58,4 gegen 60,9 ± 6,7 im Remake → #730 im Bereich geschlossen (`analyse/abgleich-dosbox.md` §4.5); A093 nicht belegbar.
  Alle Teile fertig, wartet auf Erstreview.

### Gesamtreview am Rundenende

ChatGPT GPT-6.1 Terra (Codex, mit Netzzugang) nach `anhang.md` C; besonders: Torszenen ändern keinen Spielwert, Platztausch
mitten in der Saison (Tabelle, Spielplan, Laden/Speichern), Belastung (#730).
