# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

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
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Plan, Start nach Go.

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
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** in Arbeit (Teil 1), Branch `paket/neuer-manager`, Claude Code.

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
- **Modell:** Claude Fable 5.2 (Nutzer 14.11.2026). **Erstreview:** Claude Opus 5.5. **Status:** Plan, Start nach Go.

### Spur `pruefung-karte` (Welle 2)

- **Inhalt:** #826 (Highscore „nur Weltkugel“: Punkte Σ 9 − Platz nach FILE54, roter Test mit Beleg aus dem Bericht
  `abgleich-dosbox` §3); Inventur „Stand Runde 19“, Funktionskarte (L7 umgesetzt), gesammelte P3 der Runde.
- **Voraussetzung:** `neuer-manager`, `re-belastung` (auch als Bericht im Fehlschlag) und `torszenen` gemergt.
- **Modell:** Claude Opus 5.5. **Erstreview:** Claude Opus 5.5. **Status:** Plan, Start nach Go.

### Gesamtreview am Rundenende

ChatGPT GPT-6.1 Terra (Codex, mit Netzzugang) nach `anhang.md` C; besonders: Torszenen ändern keinen Spielwert, Platztausch
mitten in der Saison (Tabelle, Spielplan, Laden/Speichern), Belastung (#730).
### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- DOSBox-Aufnahme `runde-17` ausgewertet in Runde 18 (`abgleich-dosbox`, #812). Offen daraus: #730 Spieltag 2/3 und A093
  (Vorschlag für eine weitere Aufnahme im Bericht §1.3) – wenn der Nutzer Lust hat.
- DOSBox-Messung der Echtzeit „Spiele laufen“ (#838, derzeit ⅓ s je Minute `[APPROX]` nach Schätzung des Nutzers).
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen (nach Runde 18)

- Torszenen, #847, #826: in Runde 19 eingeplant (Spuren oben).
- **DOSBox-Aufnahme `runde-19`** (R19-D3: nur falls `re-belastung` die Ursache nicht findet; #838 auch sonst, nach Lust): #730 Spieltag 2/3, A093, #838 Echtzeit;
  Liste in `vorlagen/dosbox-aufnahme.md`, Auswertung in der Runde nach Eintreffen.
- **Testlücke Gesamtreview #848:** Speichern/Laden mitten in einer angehaltenen Live-Paarung (über die Oberfläche nicht
  erreichbar; prüfen, sobald ein Speicherweg im Live-Spiel dazukommt).
- **Nach v1.0:** Einstellungen aus R15-E3 und bitgenauer Zufallsmodus (`anforderungen/zusatzfunktionen.md` C).

## Paket `hok-format` – Original-Spielstände `*.HOK`

Branch: `paket/hok-format` · Status: Analyse gemergt (#176, Review #178): Verschlüsselung, Prüfsummen, Kopf und Blockfolge
belegt, `emtool hok dump`, Doku `docs/formate/spielstand-original.md` (offene Liste dort). Entscheidung Nutzer:
Sicher-/Ladesperre des Originals nicht übernehmen. Weiter in Runde 7 mit `hok-patch` (Spielstände fürs Original);
Import/Export ins Remake danach.

- Nachtrag (#197, Branch `paket/hok-format-2`): Kaderplätze, Spielerpool, Training und Taktik an FILE05 belegt;
  Kaderausgabe in `emtool hok dump`. Spielstände FILE01–05 und `KAPUTT.HOK` im Referenz-Repo `original/spielstaende/`.

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: PR 1–7 gemergt (Runde 3); Restarbeiten in Runde 4 gemergt; PR 8 in Runde 5 gemergt (`paarungen-liga`, #128). Regeln und Belege: [wettbewerbe.md](wettbewerbe.md)

| PR | Inhalt | Stand |
|---|---|---|
| 1 | DEB-Pokal: Auslosung, Heimrecht, „best of three“, Abrechnung, Bildschirm „DEB-Pokal Auslosung“ | gemergt (Runde 3, #54) |
| 2 | Europapokal (Gruppe + Endrunde), Bildschirm Gruppen/Endrunde | gemergt (Runde 3, #54) |
| 3 | WM (2 Gruppen + K.-o.), Bildschirm Gruppe/Tabelle WM | gemergt (Runde 3, #54) |
| 4 | Stärken (Ligen, Länderteams) | gemergt (Runde 3, #54) |
| 5 | Statistiken: Ewige Tabelle (Totale), Früher | gemergt (Runde 3, #54) |
| 6 | Bundestrainer: WM-Kader, „WM Vorbereitung“ (Freundschaftsspiele), Etat; WM: Werte Deutschlands neu ausgewürfelt, Länder 20-mal getauscht | gemergt (Runde 3, #54) |
| 7 | Spielmodus „nur Weltkugel“: Spielvorbereitung, Vereinssaison überspringen, Menü des Bundestrainers | gemergt (#87 über #89) |
| 8 | Paarungen Liga | gemergt (Runde 5, #128); Pixelvergleich Runde 6 (`pixel-wettbewerbe`) |
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | Runde 8 (`statistiken`, `nationalteam`) |

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: Format, Dialog und automatisches Speichern gemergt (Runde 3, [archiv/runde-3.md](archiv/runde-3.md)); #80 in Runde 4 behoben (#105)

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`) und Export per Vorlage (`hok-format`) – wartet auf Original-Spielstände des Nutzers.

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen auf den Ereignissen aus `spielberechnung` (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
