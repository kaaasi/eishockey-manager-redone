# Auswertung DOSBox-Aufnahme `runde-22` Sitzung 1 (Spur `abgleich-dosbox-22`, Runde 23)

Grundlage: FILE75, FILE76 und die Bilder `file75_*`, `file76_*` im Referenz-Repo (kaaasi/eishockey-manager-referenz#41,
`original/spielstaende/README.md` Abschnitt „Runde 22, Sitzung 1“), `emtool hok dump|import|export`, Disassembly mit
`tools/reverse-engineering/disasm.py`. Keine Bilder in diesem Repo. Tests: `AbgleichDosbox22Tests` (Teil 1, belegte Gleichheit).

**Stand der Aufnahme:** Beide Managervereine (EV LANDSHUT, AUGSBURGER EV) stehen in FILE75 auf den Satzplätzen 12 und 23, also in
der **2. Liga** (README: „Start-Liga Bundesliga“ meint den Regler-Wert 1). Regler: Level 3, Dauer unbegrenzt (3), Start-Stärke
oberes Drittel (0), Modus Fahne + Weltkugel.

## Ergebnis je ID

| ID | Befund | Klasse (Vorschlag) | Teil 2 |
|---|---|---|---|
| A427 | Original führt keine Spieler der Computervereine; Remake-Werte ohne Spielwirkung | K | Markertext |
| A065 | Platzbyte vor dem 1. Spieltag = 0 → Term 25 · (35 − Versatz) statt 0 | V | S: `Highscore.cs` |
| A057 | Zugfolge an drei Tagen Spieler1 → Spieler2 wie im Remake | X | Marker `[ORIGINAL]` |
| A402 | Export: Bankseite Satz 4 +0x18F fehlt (Original +300.000, Export 0) | V | S: `OriginalExport.Managers.cs` |
| A613 | Import Sparbuch und Managerdarlehen belegt | X | Marker `[ORIGINAL]` |
| A614 | Bande +4 = Vertragsnummer (DGROUP 0x17C), +7 = Partner; Import/Export lesen +4 als Partner | V | S: Import/Export Werbung |
| A629 | Leihende weiter offen (Sitzung 2); neu: Export schreibt Pool-Besitzer = Entleiher statt Verleiher | D (Rest) + V | S: `OriginalExport.Squads.cs` |
| A117 | 0 DM → „nicht interessiert“ + Sperre, 60.000 DM angenommen – wie Remake; Dialog zeigt 1 DM statt 0 DM | K | S: `ContractDialog.cs` |

## 1. A427 – Startwerte der Computervereinsspieler

- FILE75 (neues Spiel, erstes Hauptmenü): Pool `far:8253` 149 Plätze mit Besitzer 6, 22 + 22 Managerkader, 7 Markt. Die Plätze
  mit Besitzer 6 tragen zwar einen Verein (`+0x1D`), ihre Grundwerte hängen aber nicht an dessen Teamwerten: Kondition und
  Technik 30–99, Differenz K − T −21 … +23, Form 45–55 – genau die Startwerte 0x5AFB2–0x5B0B1 (`transfer.md`, schon `[ORIGINAL]`
  in `TransferPoolRules.RollStartValues`). Beispiel EV LANDSBERG: Teamwerte `far:9657+0x18…0x1B` = 39/39/41/40, die 18
  Poolplätze des Vereins im Mittel K 70 / T 68.
- Das Original hat also keine Einzelspieler der Computervereine, deren Startwerte man vergleichen könnte; Angabe des Nutzers:
  „Kader der Computervereine im Spiel nicht erreichbar“. Im Remake spielen Computervereine mit den Teamwerten
  (`MatchEngine.ClubFieldValues`), ein Wechsel zu einem Manager übernimmt die Poolwerte (`PlayerFactory.Join` → `AdoptPoolValues`).
  Die Werte ±2 / Form 40–60 / Energie 50–80 (`Player.Factory.cs:42`) wirken nur auf Remake-eigene Anzeigen.
- **Vorschlag K** (ohne Spielwirkung); Markertext um den Beleg FILE75 ergänzen. Keine Codeänderung.

## 2. A065 – Bestenliste vor dem ersten Spieltag

- Die Bestenliste war in dieser Aufnahme **nicht erreichbar**: Highscore-Schlüssel DGROUP 0x256 = 0 in FILE75 und FILE76
  (Start-Stärke oberes Drittel und Dauer ≠ eine Saison → nicht zulässig, Datei 0x289B0/0x289B7). Die sechste Managerkachel ist
  deshalb gesperrt (Datei 0x44FCE) – das ist der „Platzhalter“ aus Bild `file75_2`, kein Fehlen der Kachel. Das Remake sperrt sie
  gleich (`HighscoreRules.Key` = null).
- Den Platz liest die Punkteformel trotzdem belegbar aus dem Stand: [ORIGINAL] Datei 0x5D2CA–0x5D303 rechnet
  36 − (Tabellenfeld DGROUP 0x8288 + 0x50 · Verein + Versatz DGROUP 0x7C0[Manager +0xEB] + 1), Verein = Manager +0x14. In FILE75
  ist das Tabellenfeld bei **allen 36** Ligavereinen 0 (Test `File75_TableFieldBeforeFirstMatchDay_IsZero`); FILE76 (nach dem
  4. Spieltag) trägt die Plätze. Vor dem ersten Spieltag zählt das Original also 25 · (35 − Versatz) – Bundesliga 875, 2. Liga 575
  Rohpunkte (÷ 20: 43 bzw. 28 Punkte) –, das Remake 0 (`Highscore.cs:375`).
- Das Remake führt das Feld bereits (`Club.StoredTableRank`, A046: Spielstart 0, Saisonwechsel Endstand der Vorsaison).
  **Vorschlag V, Teil 2 (S):** in `HighscoreRules.Inputs` ohne gespielten Spieltag `rank0 = StoredTableRank`, Versatz wie bisher.
  Roter Test: Bestenlisteneintrag eines neuen Spiels (Platzterm 875 bzw. 575). Eine Aufnahme mit zulässigem Schlüssel
  (Start-Stärke Mittelfeld) bleibt für die Gegenprobe der Punkte im Bild wünschenswert, ist für die Regel aber nicht nötig.

## 3. A057 – Hot-Seat-Zugfolge

- Angabe des Nutzers: an drei Spieltagen hintereinander erst Spieler1, dann Spieler2, Reihenfolge unverändert. FILE76 ist im Zug
  von Spieler2 gesichert (DGROUP 0xA70E = 1). Das entspricht `GameState.HotSeat.cs:10` (neuer Tag beim ersten aktiven Manager,
  dann Setup-Reihenfolge).
- **Vorschlag X** für den belegten Teil (zwei Manager, keiner entlassen); der Fall eines entlassenen ersten Managers ist nicht
  aufgenommen und bleibt mit `[APPROX]` im Text, falls die Leitstelle ihn als eigene Zeile führen will.
- **Neu, ohne ID:** Der Export schreibt DGROUP 0xA70D = Managerzahl − 1 (`OriginalExport.cs:300`, `[APPROX]`). An allen 72
  Originalständen ist 0xA70D = 0xA70E; FILE73, FILE74 und FILE75 (zwei Manager, Spieler1 am Zug) haben 0 statt 1 (Rundreise
  FILE75: einzige Abweichung außerhalb der Regeln). Die Datei gehört nicht zu den Feldern dieser Spur (A402/A613/A614/A629) →
  Vorschlag an die Leitstelle: 0xA70D = 0xA70E schreiben; damit wird auch der Marker `Transfer.Pool.cs:972` („Original liest
  0xA70D“) zu X.

## 4. A402 / A613 – Sparbuch und Managerdarlehen im HOK

FILE76, Darlehensliste `965B` + 0x1AF · Satz + 0x19B (+4 · i, Drittel-DM):

| Satz | +0x18F Konto | i = 0 | i = 1 | i = 4 (Bank) |
|---|---:|---:|---:|---:|
| 0 Spieler1 | 1.677.000 | 0 | +315.000 | +300.000 |
| 1 Spieler2 | 1.671.000 | −315.000 | 0 | 0 |
| 4 Bank | **+300.000** | −300.000 | 0 | 0 |

- **A613 (Import):** positiver Bankeintrag = Sparbuch 100.000 DM, Gläubiger trägt +, Schuldner −; der Import liefert Sparbuch
  100.000, Kredit 0, ein Darlehen 0 → 1 über 105.000 DM (Test `File76_SavingsAndManagerLoan_ImportLikeOriginal`). Zins 2,5 % im
  Monat (Bild `file76_01`: 2.625 DM) wie `FinanceRules.ManagerLoanRate`. Der Schuldner wird nicht gefragt (Angabe Nutzer) – wie
  `BorrowFromManager` im lokalen Spiel. **Vorschlag X.**
- **A402 (Export):** Darlehen und Bankeintrag des Managers schreibt der Export gleich; die Bankseite Satz 4 +0x19B = −300.000 auch.
  **Abweichung:** Satz 4 +0x18F (Konto der Bank) – Original +300.000, Export 0. FILE08 (Bankkredit) zeigt dasselbe Muster mit
  −2.103.000. Die Rundreise meldet es nicht, weil die Regel „965B +0x18F–0x192 Konto: Rundung“ alle Sätze deckt
  (`OriginalExportRoundTripTests`). **Vorschlag V, Teil 2 (S):** Satz 4 +0x18F = Σ Bankeintrag der Manager (`[APPROX]` für die
  Summe; belegt ist nur je ein Manager mit Bankeintrag), die Rundungsregel auf die Sätze 0–3 und 5 beschränken, FILE08 und FILE76
  in die Rundreise.

## 5. A614 – Werbeverträge (Banden) im HOK

- FILE76, Spieler1 (Bild `file76_03`: zwei Banden JWE SERVICE, eine dritte Bande eines zweiten Partners; Flächen 0–3 leer):

  | Fläche | +0 Betrag (Drittel) | +4 | +5 Monate | +7 | Export +4 / +7 |
  |---|---:|---:|---:|---:|---|
  | 4 | 30.129 | 2 | 24 | 0 | 2 / **2** |
  | 5 | 30.129 | 2 | 24 | 0 | 2 / **2** |
  | 6 | 34.468 | 1 | 24 | 1 | 1 / 1 |

  DGROUP 0x17C: Original 3, Export 1 (an allen früheren Ständen 1 = Spielstart).
- [ORIGINAL] Abschluss einer Bandenwerbung im Werbebildschirm 0x2B0C0: je Bande freie Fläche suchen (Betrag 0 oder „Kündbar.“),
  Angebot (10 Byte) darüber kopieren (0x48:0x2BD4, Datei 0x2BB3E), dann **+4 = [0x17C]** (Datei 0x2BB5B–0x2BB5E); nach allen
  Banden `inc byte [0x17C]` (Datei 0x2BB80). +4 ist bei Banden also eine **Vertragsnummer** (1, 2, …), der **Partner steht in
  +7** (Logo = Partnerindex wie bei den Angeboten, FILE05): 0 = JWE Service GmbH, 1 = CCH (`Sponsor.Companies`). Trikot,
  Fernsehen und Eis (Datei 0x2BC93) kopieren das Angebot ohne Nummer; +4 ist dort die Bandenzahl des Angebots.
- **Abweichung:** Der Import nimmt den Partner aus +4 (FILE76: JWE-Banden als Partner 2), der Export schreibt +4 = +7 = Partner
  und 0x17C = 1. Die Gruppierung „gleicher Partner, Betrag, Laufzeit = ein Vertrag“ (`OriginalImport.Rest.cs:179`) trifft
  zufällig, weil sie über +4 läuft. **Vorschlag V, Teil 2 (S):** Import Partner = +7, Vertrag = Bandenflächen mit gleicher
  Nummer +4; Export Nummern 1 … n in Vertragsfolge (`[APPROX]` Folge) und 0x17C = n + 1; Betrag in Drittel wie bei den Angeboten
  als Rundungsregel (34.468 / 3 = 11.489,33 DM → Export 34.467). Rundreise FILE76.

## 6. A629 – Leihe zwischen Managern

- Ablauf laut Nutzer: Leihe an einen Mitmanager nur über die Transferliste – Spieler1 setzt LOTH auf die Liste, Spieler2 bietet im
  Transferbild 120.000 DM („LOTH leihen“), Spieler1 bestätigt im Popup (Bilder `file76_04…06`); das ist der Weg 0x47546 („Gebot an
  anderen Manager“, `transfer.md`).
- FILE76: Kaderplatz Satz 1 Platz 22 `+0x0B` = 1, `+0x0C` = 0x8C (0x80 | Satz 12 = EV LANDSHUT), Gehalt 7.622 DM (vorher 2 J.,
  7.100 DM); Pool 100 `+0x1A` = **0 (Verleiher)**, `+0x1D` = 23 (Entleiher). Der Import liefert Leihe mit Stammverein EV LANDSHUT,
  1 Jahr, 7.622 DM (Test `File76_LoanBetweenManagers_ImportsLender`).
- **Abweichung (neu):** Der Export schreibt Pool `+0x1A` = Manager des Kaderplatzes, also 1 (`OriginalExport.Squads.cs:47`). Die
  Rückkehr am Saisonende (Datei 0x27547–0x27665) richtet sich nach diesem Besitzer; ein exportierter Stand gäbe LOTH im Original
  dem Entleiher zurück. **Vorschlag V, Teil 2 (S):** Besitzer eines geliehenen Spielers = Manager des Stammvereins.
- Das Leihende selbst (Rückkehr, Laufzeit, Gehalt) bleibt **D** bis zur Sitzung 2 (FILE77/FILE78). Das Gehalt 7.622 DM
  (nicht das alte 7.100 DM) passt zu `Contracts.LoanSalary` (Wert(Gehalt) · random(90, 115) / 100, A118); ohne Zufallszustand im
  HOK nicht nachrechenbar.

## 7. A117 – Vertragsverhandlung mit Gehalt 0 und sehr hohem Gehalt

- Bilder `file76_09…17` (Spieler2, nach FILE76 ohne Stand): A.KRINNER, Gehalt 0 DM eingegeben → Feld zeigt **„0 DM“**, „Mach'
  ich!“ → „A.KRINNER ist nicht an Ihrem Angebot interessiert.“; danach steht KRINNER rot im Kader (Sperre), Gehalt unverändert.
  R.HEIDT, 60.000 DM → keine Meldung, Gehalt 60.000 DM, Laufzeit bleibt 2 Jahre.
- Remake: `ContractDialog.cs:156` macht aus 0 DM 1 DM; `Contracts.Decide` lehnt bei gleicher Laufzeit und weniger Gehalt ab
  (`ContractRules.Accepts`, Datei 0x49A3B: „Laufzeit gleich, weniger → nein“) → `NotInterested` mit Sperre random(10, 18); mehr
  Gehalt → angenommen, Laufzeit unverändert. **Ergebnis gleich.** Die Meldung „unfair“ kommt in der Vertragsverhandlung nicht vor;
  `NegotiationResult.Unfair` ist im Dialog nur für negative Werte erreichbar, die die Eingabe nicht zulässt.
- **Vorschlag K** (nur die Anzeige 1 DM statt 0 DM). Teil 2 (S, optional): Eingabe 0 als „0 DM“ zeigen und als Angebot 0 werten
  (nicht als „nicht eingegeben“), Ergebnis unverändert `NotInterested`; Markertext A117 auf den Beleg umstellen.

## Rundreise FILE75/FILE76 (Probe, nicht eingecheckt)

`OriginalExportRoundTripTests` mit FILE75/FILE76 in der Liste: FILE75 eine Abweichung außerhalb der Regeln (0xA70D, §3), FILE76
fünf (0x17C und C15B +0x2F/+0x39/+0x3C aus §5, Pool 100 +0x1A aus §6). Teil 2 nimmt beide Stände mit den Korrekturen auf.
