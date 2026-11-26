# Live-Spieltag: Minutenschleife 0x37A62 über alle Paarungen (Spur `live-verschraenkt`, Runde 26)

Teil 1 der Spur (Beleg vor Umbau, `docs/arbeitspakete.md` Runde 26): Nachbildung der Minutenschleife Datei 0x37A62 (Ov 0xF0,
`0xF0:0x7F`, 3183 B, einziger Aufrufer der Spieltagsablauf 0x36B5D) über alle Paarungen eines Spieltags. Skript
`tools/reverse-engineering/live/minutenschleife.py` führt den Originalcode von 0x37A62 mit dem Emulator
`tools/reverse-engineering/abweichungen/emu16.py` aus und protokolliert die Aufrufe der Minutenrechnung in der Reihenfolge, in der
das Original sie ausführt; alles außerhalb von 0x37A62 ist ein Stub (Aufruf: `python3 tools/reverse-engineering/live/minutenschleife.py`).
Gelesen mit `disasm.py em.exe 0x36B5D 0x3901B`. Ausgaben enthalten nur Adressen und Zahlen.

## 1. Aufruf aus dem Spieltagsablauf

[ORIGINAL] 0x36B5D bereitet erst alle Paarungen vor (0x544C0 Datei 0x36DBD/0x36F9C, 0x8365 Datei 0x36D12/0x36D49/0x36F27/0x36F5A)
und ruft dann 0x37A62 **einmal je Drittel für alle Paarungen des Tages** (Datei 0x37192, 0x371BE, 0x371E5: Minuten 1–20, 21–40,
41–60; Verlängerung Datei 0x37254/0x37276: 61–65, 66–70 nach 0x32AA3 Schalter 0, Datei 0x37211; danach 0x32AA3 Schalter 1 mit
Penaltyschießen 0x3901B, Datei 0x3728B). Argumente (cdecl, von links): Ligamaske, Pokalmaske (Zeilenbits), –, Drittelanfang,
Drittelende, Anzeige (DGROUP 0x5A56), Pokalzeile. Pokalmaske ≠ 0 nimmt den Pokalzweig (Datei 0x37D8C → 0x380D0), sonst den
Ligazweig (Datei 0x37D98).

## 2. Ablauf einer Minute (Nachbildung, Fälle 1–3)

Je Minute (Schleife Datei 0x37C6B–0x3855C):

1. Mit Anzeige, `[0x1DB]` = 0 und Geschwindigkeit < 70: Zwinkerwurf lrandom(0, 4) und Pause (Datei 0x37C6B–0x37CBD, wie
   `LiveClock`), dann `DGROUP 0xE19E` = Minute (Datei 0x37CD2), Taste lesen (Datei 0x37CFA).
2. Leertaste (bzw. Fensterobjekt 0xC147 meldet 0/0 bei `DGROUP 0x5A16` = 1): Spielmodus-Optionen 0x2CD1C, Live-Bildschirm neu,
   Lampe (10|228) aus, dann **Sprung an das Minutenende ohne Rechnung** (Datei 0x37D89 `jmp 0x38551`): in dieser Minute rechnet
   keine Paarung, auch die Tafel 0x52BD6 wird nicht gezeichnet (Fall 2: Minute 7 ohne Chancen und ohne Tafel; Minute 8 normal).
3. **Alle Paarungen nacheinander** in fester Reihenfolge:
   - **Liga** (Datei 0x37DB7–0x380CD): Ligen 0, 1, 2 (Bit der Ligamaske), je Liga die Paarungen des Spieltags in Tabellenreihenfolge
     (Heim DGROUP 0x5D7B + 12 l + 2 p, Gast + 1, Anzahl 0x7B1[l]). Spielwertung (Ergebnisbyte 0x5A) wird übersprungen.
   - **Pokal** (Datei 0x380D0–0x3853E): Zeilen 0 (DEB-Pokal), 1, 2 (Play-off), 3 (WM-Endrunde) nach der Pokalmaske, je Zeile die
     Paarungen ab 0xCFF8[Zeilenargument] bis 0xFF; übersprungen werden Spielwertung (0x5A), entschiedene Runde/Serie (≥ 100 in
     DGROUP 0xD014 + 32 · 0xD074 bzw. 0xCCB2 + 14 · 0xCCA3) und **in der Verlängerung jede Paarung ohne Marke** (Ergebnisbyte
     < 30, Datei 0x3825F–0x3826C); bei Maske 0xC (Play-off und WM zugleich) rechnet die WM-Zeile keine Verlängerung (Datei 0x381C6).
4. Je Paarung (Fall 1, Liga; Pokal gleich ohne 0x874D):
   1. nur Paarung in der Reihenliste (0x52B91, Datei 0x37E7B bzw. 0x38277) und `DGROUP 0x5A16` ∈ {1, 3}: **Eingriffsmenü 0x38D96**
      (Datei 0x37EB5 bzw. 0x382B3) – also je Managerpaarung einmal je Minute, unmittelbar vor deren Rechnung;
   2. Vereinsfeld +0x16 von Heim oder Gast ≠ 0: Verletzungen 0x38B8F (Datei 0x37F06 bzw. 0x38304);
   3. Chancen 0x9F5C, Abschluss Heim, dann Gast 0xA484 (Torszene/Tormeldung im Abschluss, A832 Datei 0xABB4–0xAD71);
      Pokal, Verlängerung: ein Tor schreibt 0x36AAA und beendet die Minute **dieser** Paarung (Datei 0x383DF–0x38418);
   4. Liga: 0x874D Heim, Gast;
   5. nur Reihenpaarung: 0x87F7 Heim, Gast, random(0, 1) Seitentausch, ungerade Minute 0x80CB, 0x8EF2 ×2, gerade Minute 0x939A ×2;
   6. außer Minute 60/70: Strafen 0x35818 (Reporter) für beide Seiten.
5. Minutenende: Anzeigetafel aller Reihen 0x52BD6(0xFF) (Datei 0x3854A), auch ohne Anzeige (zeichnet dann nur die Uhr).

Ausgabe Fall 1 (3 Ligen × 6 Paarungen, Reihen Verein 2 und 41): Reihenfolge in jeder Minute 1–20 gleich
`[0, 2, 4, 6, 8, 10, 20, 22, … 30, 40, … 50]`; je Minute zwei Eingriffsabfragen und zwei Würfe random(0, 1), je an der Stelle der
Reihenpaarung. Fall 4 (Verlängerung, DEB-Pokal): in 61–65 rechnen nur die Paarungen mit Marke (`[100, 104]`), die entschiedene
Reihenpaarung (Verein 102) bekommt **keine** Eingriffsabfrage. Fall 6: treffen in derselben Verlängerungsminute beide Seiten,
schreibt 0x36AAA nur das Gasttor (`[bp-0x4]` = 2 überschreibt 1; Nebenbefund außerhalb dieser Spur, siehe §5).

## 3. Abweichungen des Remakes (vor dem Umbau)

| Befund | Remake | Original (Beleg oben) |
|---|---|---|
| A442, A547 | `GameState.PlayNextMatchDay` und die Pokal-/Play-off-Tage rechnen Paarung für Paarung alle Minuten (`PlayWithEvents`); der Live-Treiber bekommt je Managerpaarung einen eigenen `LiveMatchDay` mit einer Paarung | eine Minutenschleife je Drittel für alle Paarungen, Minute für Minute in der Reihenfolge §2 Punkt 3, gemeinsamer Zufall |
| A450, A555 | spätere Managerpaarungen erscheinen erst mit ihrem Spiel | alle Reihen von Minute 1 an (0x544C0 vor den Minuten, Tafel 0x52BD6 je Minute für alle Reihen) |
| A454 | Maus je Bild, Befehle wirken ab der nächsten Rechnung | Abfrage 0x38D96 je Managerpaarung und Minute vor deren Rechnung |
| A766 | fertig gerechnete Paarung (nacheinander) nimmt keine Befehle | in der Verlängerung rechnet nur die Paarung mit Marke; die entschiedene bekommt keine Abfrage (Fall 4) – mit verschränkter Rechnung also wie im Original |
| A909, A954 | Tormeldungen der Paarungen ohne Reihe vor dem Halt an der Reihenpaarung | Meldung im Abschluss 0xA484 an der Stelle der Paarung in der Minute |
| A541/A542 | unverändert (R26-D2 = A) | – |

## 4. Vorgabe und erwartete Werte für die Teile 2–4

**Teil 2 (S01):** Ein Spieltag mit Minutenweg rechnet alle Paarungen eines Termins in einem `LiveMatchDay` (Reihenfolge Liga 0 → 2,
je Liga die Paarungen des Spielplans; Pokal je Wettbewerb in Paarungsreihenfolge) aus dem gemeinsamen Zufallsstrom; mit
Live-Treiber bekommt der Treiber **einen** `LiveMatchDay` je Termin. Rote Tests (erwartete Werte):

- **E1:** Liga-Spieltag mit zwei Managerpaarungen in verschiedenen Ligen, Treiber eingesetzt: genau ein `Run` je Spieltag; der
  `LiveMatchDay` enthält alle Paarungen des Tages in obiger Reihenfolge; nach jedem `Step` haben beide Reihenpaarungen dieselbe
  `LastMinute` (heute: zwei `Run`, je eine Paarung).
- **E2:** Ergebnis des Spieltags ohne Treiber = Ergebnis mit Treiber ohne Eingriff = `LiveMatchDay.Begin(alle Paarungen, gemeinsamer
  Zufall)` mit `Finish` (gleicher Startwert; heute ungleich, sobald zwei Paarungen den Minutenweg nehmen).
- **E3 (A454):** Eingriffsabfrage je Minute einmal je Reihenpaarung, unmittelbar vor deren Minutenrechnung, in Paarungsreihenfolge;
  Paarungen ohne Reihe nie.
- **E4 (A766):** In 61–70 rechnen nur die Paarungen mit Gleichstand nach 60; entschiedene Paarungen bekommen weder Rechnung noch
  Abfrage. Penaltyschießen erst nach Minute 70 aller Paarungen, in Paarungsreihenfolge (0x32AA3 Schalter 1).
- **Mehrsaisonprobe** (4 Wechsel, zwei Manager, Live an): Ergebnisse und Geld plausibel.

`[APPROX]` bleibt: bitgenaue Zufallsfolge (A442/A547 nur Verteilung der Würfe je Minute, Folge nicht bitgenau – R26-D2 = A), der
Zwinkerwurf aus eigenem Generator (`LiveClock`, Review #795), Termine mit Paarungen verschiedener Wettbewerbe (Maske 0xC) laufen
je Wettbewerb, wenn der Ablauf des Remakes sie getrennt aufruft.

**Teil 3 (S04):** unabhängig von dieser Nachbildung (Fundstellen 0x3183C, 0x35CBE).

**Teil 4 (S05):**
- **E5 (A456):** Reihenfolge der Meldungen einer Paarung in der Minute: Verletzung (0x38B8F, vor den Chancen) → Chancen/Tor
  (0xA484) → Strafe Heim, Strafe Gast (0x35818); über die Paarungen in Paarungsreihenfolge.
- **E6 (Leertaste, Teil von A449):** Leertaste in Minute n: Zwinkerwurf und Pause der Minute n, dann Spielmodus-Optionen; Minute n
  wird für **keine** Paarung gerechnet, die Tafel nicht gezeichnet; weiter mit Minute n + 1 (Fall 2). Enter/Escape bleiben (#205).

## 5. Nebenbefund (nicht Teil der Spur)

Pokal-Verlängerung, beide Seiten treffen in derselben Minute: 0x36AAA bekommt nur das Gasttor (Datei 0x383D7–0x383DB `[bp-0x4]` = 2
überschreibt 1, Datei 0x383EB–0x38412), der Rest der Minute dieser Paarung entfällt (Datei 0x38418). Ob das Remake das so rechnet,
ist nicht geprüft – Meldung an die Leitstelle.

## 6. Umsetzung Teil 2 (S01)

- **Kern:** `MatchEngine.PlayMatchDay` (`Core/MatchEngine.LiveDriver.cs`) rechnet einen Ligaspieltag als einen `LiveMatchDay`
  über alle Paarungen aller Ligen (Reihenfolge §2 Punkt 3) aus dem gemeinsamen Zufall; `GameState.PlayNextMatchDay` ruft es
  einmal je Spieltag (gemeinsame Datei, minimal). Mit Live-Treiber bekommt der Treiber den ganzen Spieltag (ein `Run`).
  `LiveMatchDay.Step` schließt Paarungen (Reihe, Penaltyschießen) erst nach der Minute aller Paarungen ab (0x32AA3 nach
  0x37A62); das Ziel der Tormeldung steht nach der Vorbereitung aller Paarungen fest (A909).
- **Oberfläche:** `LiveSession.Shown` = alle Paarungen mit Reihe des Termins ab Minute 1; `LiveMatchScreen` zeigt je Minute die
  Meldungen der Reihen und die Tormeldungen der Paarungen ohne Reihe in Paarungsreihenfolge.
- **A454:** Klicks wirken zwischen zwei Minuten statt an der Abfragestelle je Paarung; gleiche Wirkung, weil 0x38D96 keinen
  Zufall zieht und nur die Felder der eigenen Paarung schreibt (Aufrufe 0x53453, 0x534BC, 0x53508, 0x5434C, 0x08E5F, 0x0C097).
- **Tests:** `LiveVerschraenktTests` (E1, E2, E4, Mehrsaisonprobe 4 Wechsel passiv/aktiv), `LiveVerschraenktUiTests`;
  Szenarien `live-verschraenkt-2`, `live-verschraenkt-3` (drei Reihen in Minute 21 gleichzeitig).

## 7. Umsetzung Teil 2b (S01, Pokaltermine)

Entscheidung Leitstelle 25.11.2026 (PR #1032): Pokaltermine wie im Original verschränken, Dateigrenzen um `Playoffs.cs`,
`CupsDeb.cs`, `CupsGroup.cs`, `CupsWorld.cs` erweitert (je minimal).

- **Kern:** `MatchEngine.PlayDate` (`Core/MatchEngine.LiveDriver.cs`): Paarungen eines Termins mit Minutenweg laufen über
  `PlayMatchDay` (je Minute alle Paarungen der Pokalzeile in Paarungsreihenfolge, Datei 0x380D0–0x3853E; ein Treiberaufruf je
  Termin); Paarungen des schnellen Wegs (`MatchEngine.FastCupPath`, aus `BeginMatch` herausgelöst) und Spielwertungen einzeln über
  `PlayWithEvents`. Jede Paarung wird gemeldet, sobald sie gerechnet ist, damit die Zusatzwürfe des schnellen Wegs die schon
  gerechneten Paarungen zählen (`DebCupExtraRoll`, `FirstPlayoffPairingOfDate`).
- **Anschluss:** DEB-Pokal (`DebCup.PlayDay`, Zeile 0), Play-offs (`Playoffs.PlayDate`, Zeile 2), Aufstiegsrunde
  (`RelegationGroup.PlayDate`), Europapokal je Gruppe (`CupGroup.Play`, Zeile 1), WM-Gruppen beider Gruppen und WM-Endrunde
  (`WorldCup.PlayDay`, Zeile 3).
- **[APPROX]:** (1) Der Weg (Minutenschleife oder schneller Weg) gilt im Remake je Paarung (Nutzer 01.11.2026), im Original je Tag
  (Merker 0x36FEA); an einem Termin mit beiden Wegen rechnet die Minutenschleife zuerst, danach die Paarungen des schnellen Wegs in
  ihrer Reihenfolge (sie zählen die Paarungen der Minutenschleife als gerechnet, wie die Markierung 0x37211 nach Minute 60).
  Mit `GameOptions.ManagerDayMinuteLoop` laufen alle Paarungen des Termins durch die Minutenschleife wie im Original.
  (2) Termine mit Paarungen verschiedener Wettbewerbe in einer Schleife (Maske 0xC: Play-off und Aufstiegsrunde; zwei
  Europapokalgruppen) rechnet das Remake je Wettbewerb bzw. Gruppe, weil der Ablauf sie getrennt aufruft.
- **Tests:** `LiveVerschraenktPokalTests` (DEB-Pokaltermin mit zwei Managerpaarungen: ein Treiberaufruf, rot vorher; erste Saison
  mit = ohne Treiber; Reihenfolge der Wege in `PlayDate`), Mehrsaisonprobe aus Teil 2 deckt die Pokaltermine mit ab.

## 8. Teil 3 (S04): Seiten nach dem Drittel

Gelesen mit `disasm.py em.exe 0x36B5D 0x3901B` und `0x3A88A 0x3ACE3`, **nachgebildet** mit
`tools/reverse-engineering/live/drittelseiten.py` (Ende von 0x37A62 Datei 0x3855F–0x386CD und Seitenwähler 0x3A88A aus den
Bytes, Seitenfunktionen als Stubs) und `tools/reverse-engineering/live/pokalseiten.py` (0x32E23 mit Minute 0/20/60, Texte und
Lage aus den Zeichenaufrufen 0x0B6A5/0x0B651).

- **Aufrufe** (auch ohne Anzeige): am Ende jedes Aufrufs von 0x37A62, also nach Minute 20, 40, 60 und in der Verlängerung 65, 70
  (Datei 0x37192–0x37276), sowie nach dem Penaltyschießen, wenn 0x32AA3(Schalter 1) eines meldet (Datei 0x37297–0x372AC, Minute
  70, nur die Seite des Wettbewerbscodes). Liga: vorher 0x369C7(2, 0) (laufende Stände), bei Minute 60 die Tabellen 0x50B45,
  0x51049, 0x052CF; je Liga der Maske Seite 7 (0x4E8A3(Liga, 2, Minute, 1)). Pokal: 0x369C7(2, 1), dann nach Code DGROUP 0xD0C2:
  1 → Seite 1 (Play-off 0x32E23(2, Minute)) und bei Spiel DGROUP 0xCCA3 ∈ {0, 2} Seite 2 (Aufstiegsrunde 0x31CD5, bei Minute 60
  vorher Tabelle 0x30852); sonst Seite = Code (6 DEB-Pokal 0x32E23(0, m), 3 Europapokal 0x31CD5, 8 WM-Gruppen 0x2FC2F beider
  Gruppen, 10 WM-Endrunde 0x32E23(3, m), 13 Freundschaftsspiel: keine).
- **Filter** (Datei 0x3A8A3–0x3AA01, Ausgabe Fall 1–4): Merker „Drittelpause“ = Minute 20 oder 40.

  | Seite | nach 20/40 | nach 60 (65, 70) |
  |---|---|---|
  | 7 Liga l | Matrix 3l + 0 („Drittelpause“) | Matrix 3l + 1 („Ergebnisse“) |
  | 1 Play-Off Spiele | Index 0 | Index 1 |
  | 2 Aufstiegsrunde | Index 3 | Index 4 bei 60, Index 3 bei 65/70 |
  | 6 DEB-Pokal | Index 0 | Schalter DEB-Pokal (Index 10) |
  | 3 Europapokal | Schalter 11 und Index 0 | Schalter 11 |
  | 8 WM-Gruppen, 10 WM-Endrunde | Schalter 12 und Index 0 | Schalter 12 |

  „Tabelle“ (Zeile 2) zeigt im Spiel keine Seite. Mit dauerhaftem Eisfeld (DGROUP 0x5A55 = 1: Darstellung 2 mit Anzeige, Datei
  0x37112/0x37227) erscheint keine Seite (die Live-Tore werden trotzdem abgelegt). Mit den Anfangswerten (nur DEB-Pokal an)
  erscheint also genau „DEB-Pokalspiele“ ab Minute 60.
- **Live-Tore** (F068): Play-off kopiert DGROUP 0x5C8B/0x5C8C → 0xCCB2 + 14 · Spiel (Datei 0x3AA30–0x3AA84), Aufstiegsrunde,
  Europapokal und WM-Gruppen 0x3183C (bei Minute 60 statt dessen die Tabelle 0x30852); die Seiten zeigen damit den laufenden Stand
  wie ein gespieltes Ergebnis.
- **0x32E23 mit Minute** (A292, A436; `pokalseiten.py`): statt Rundenname und Datum „       n.Minute“ (sieben Leerzeichen DGROUP
  0x4E5F, Minute, 0x37A4A) auf derselben Grundlinie, ohne Knopf (Play-off/WM; Datei 0x33A70); DEB-Pokal: Spalten nach den
  vergangenen Terminen wie ohne Minute (x = 207 − 10 · (n − 1)), der laufende Termin als Spalte n dahinter (2. Termin: 207 und
  243; 1. Termin: 217); Play-off: laufendes Spiel in seiner Spalte (Spiel 1: x = 213); Bedienung wie ohne Minute (Rechtsklick
  oder Taste weiter, Linksklick auf einen Verein öffnet sein Fenster).
- **Marken:** die erste setzt 0x32AA3(Schalter 0) erst nach den Seiten der Minute 60 (Datei 0x37211) – ein Punkt ab Minute 65,
  nach dem Penaltyschießen zwei Punkte mit Treffern (C07).

**Umsetzung:** `LivePages.After` (`Core/MatchEngine.LivePages.cs`, Filter wie oben), `LivePageScreens`
(`UI/Screens/Live/LivePageScreens.cs`, Seiten mit Minute und laufendem Stand), `LiveMatchScreen` zeigt sie nach dem Drittel bzw.
nach dem Penaltyschießen; „DEB-Pokalspiele“ und „Play-Off Spiele“ haben eine Ansicht mit Minute, „WM-Endrundenspiele“ und die
WM-Gruppenpaarungen nutzen ihre vorhandene. Tests: `LivePagesTests` (Filter, Werte aus `drittelseiten.py`), `LiveSeitenUiTests`
(DEB-Pokaltag mit Anfangswerten und mit Drittelpause; rot vorher: keine Seite).

**[APPROX]/offen:** (1) Ligapaarungen (Seite 7), Aufstiegsrunde (Seite 2) und Europapokal (Seite 3) haben keine Ansicht im
Spielablauf – die Bildschirme (`LeaguePairingsScreen`, `PromotionRoundScreen`, `EuropeanCupScreen`) liegen außerhalb der
Dateigrenzen; die Seiten entfallen. (2) Ohne Live-Anzeige („Spiele laufen“) rechnet das Remake den Tag vorab in
`GameState.Continue`; die Seiten erscheinen nur im Live-Bildschirm. (3) Penalty-Anzeige 0x35CBE (F076) noch offen.

## 9. Teil 4 (S05): Reporter und Tasten

- **A456 (E5):** Reporter in der Reihenfolge der Rechnung je Paarung: Verletzungen 0x38B8F (Datei 0x37F06 bzw. 0x38304, vor den
  Chancen), Chancen/Tore 0x9F5C/0xA484, Strafen 0x35818 der beiden Seiten in der Reihenfolge des Kerns (nach dem Seitentausch
  random(0, 1), Datei 0x37F9B–0x38073). Umsetzung `LiveMatchScreen.Messages`; Test `LiveTastenUiTests.ReporterOrder_…` (rot
  vorher: Verletzungen kamen zuletzt).
- **Leertaste (E6, Teil von A449):** Datei 0x37CFA liest die Taste in jeder Minute nach Zwinkern und Pause, ausgewertet nur mit
  Anzeige (Datei 0x37D02 prüft nur `[bp+0x10]`, also auch im Schnelldurchlauf). 0x20 → Sicherung DGROUP 0xE19B/0xE19C nach
  0x262/0x226 (Datei 0x37D29–0x37D34), 0x45D46, Optionen 0x2CD1C, neu sichern (Datei 0x37D41–0x37D4A), Neuaufbau 0x5353C, Lampe
  aus (0xE19A = 0, 0x53453), dann `jmp 0x38551` – die Minute rechnet für keine Paarung, die Schleife zählt weiter. Drittelpause
  0x37A86 steht vor der Schleife, das Ende 0x32AA3 nach ihr; beide bleiben. Umsetzung `LiveMatchDay.Skip`, `LiveSession.Skip`,
  `LiveMatchScreen` (`GameOptionsScreen`); Tests `LiveTastenTests` (kein Zufallszug, Minute 60 übersprungen: Gleichstand geht in
  die Verlängerung), `LiveTastenUiTests.Space_…` (rot vorher: keine Optionen). `[APPROX]`: die Tafel bleibt in der Minute sichtbar
  (das Original zeichnet nach dem Neuaufbau bis zum nächsten Minutenende keine).
- **A448:** Pause 0x130EE: n / 3 mit Vorzeichen als vorzeichenloses Wort Ticks (n = −1, −2 → 0; −3 … −5 → 0xFFFF Ticks) – jetzt
  wie im Original (`LiveReporter.PauseFrames`); erreichbar weder dort noch im Remake (Regler nur Zehner, `SetMatchSpeed`).
- **A457:** Reporterbild Datei 0x53658–0x5367E über 50h:BD3h (0x13B23) ohne Maskenargument; 0x13B23 springt bei Quelle im Speicher
  nach 0x13C0B → 0x11283, deren Pixelschleifen Farbe 0 auslassen (Speicher → Speicher Datei 0x1134C, Speicher → Bildschirm Datei
  0x11469: `or al, al` / `jz`). Also durchsichtig wie bisher, jetzt `[ORIGINAL]`; Test `ReporterImage_Color0LeavesBackground`.
- **A475:** Mindestbreite DGROUP 0x374 an der Zuschauerzeile (Datei 0x53AA7) = 1: 0x5353C ruft vorher den Grafiklader 0xC17D
  (Datei 0x535DB, 0x537E7, 0x53992), der als Erstes `mov byte [0x374], 1` schreibt (Datei 0x0C1A1); dazwischen rufen nur 0x139C9,
  0x0BEFA, 0x0B6A5, 0x021CA (keine Schreiber, `disasm.py --xref 0x374 0x374`). Wert 1 wie bisher, jetzt `[ORIGINAL]`.
- **A453:** bestätigt (Plattformersatz 70 Hz → 60 Bilder/s, E).

## 10. Teil 3b (S04 Rest): Ligapaarungen, Penalty-Bildschirm; offen Aufstiegsrunde und Europapokal

Entscheidung Leitstelle 26.11.2026 (PR #1032): (a) wie im Original, Dateigrenzen um `LeaguePairingsScreen.cs`,
`PromotionRoundScreen.cs`, `EuropeanCupScreen.cs` erweitert; (b) Penalty-Bildschirm mit den drei Farbstreifen (F076) hier, das eigene
Eis mit den Schüssen (T7) in `torszene-dauer` Teil 2.

- **Seite 7 Ligapaarungen** (0x4E8A3(Liga, 2, Minute, 1), Datei 0x3ABEA–0x3AC05): Modus 2 zeichnet Fenster, Linien und Zeilen wie
  aus dem Hauptmenü, aber keine Knöpfe und Pfeile (Datei 0x4E90A: nur Modus 0) und wertet sie nicht aus (Datei 0x4EFE0, 0x4F01D).
  Kopfzeile (Datei 0x4EA9F–0x4EC1F): Liga, „   “, Spieltag, „.Spieltag  “ ohne Datum, „  “, Minute, „.MINUTE“ – z. B.
  „Bundesliga   5.Spieltag    60.MINUTE“. Ergebnisse (Datei 0x4EE89: gespielte Tage + 1 &gt; Tag) für jede Paarung des Tages aus
  der Ergebnistabelle DGROUP 0xA0BF, in die 0x369C7(2, 0) vorher die laufenden Stände schreibt (Datei 0x38598). Breite des
  Spieltags: 2 nach einer vorigen Ligaseite der Folge (Datei 0x4EEA4), sonst `[APPROX]` 1. Bedienung: Rechtsklick oder Taste
  weiter, Linksklick auf einen Verein öffnet sein Fenster. Umsetzung `LeaguePairingsScreen` (Ansicht mit Minute),
  `LivePageScreens`; Test `LiveSeitenUiTests.LeagueDay_Reports_PairingsPageWithMinute` (rot vorher: keine Seite).
- **Penalty-Bildschirm** (F076, Datei 0x35CBE, 0x3917F–0x39373, 0x3958D–0x39769): Grund Farbe 11, Streifen y 1–79 Farbe 0,
  80–159 Farbe 0x12, 160–238 Farbe 0x18 (x 1–318), Schrift 1; Ankündigung „!    Penaltyschiessen    !“ (Grundlinie 32, Farbe
  0x1E), Heim (99, 0x18), „gegen“ (123, 0x1E), Gast (152, 0x18), Klick; nach den Schüssen Pause 50 und „Endergebnis:“ mit
  „h : g   n.P.“ (212, 0x10) aus den Treffern des Penaltyschießens, Klick. Nur im Anzeigeweg (ein Verein der Paarung live
  zugesehen, Datei 0x390B8–0x390D6), nach den Seiten der Minute 70 (Datei 0x3728B), je Paarung in Paarungsreihenfolge, danach die
  Seiten nach dem Penaltyschießen. Umsetzung `LivePenaltyScreen`, `LiveMatchScreen.Shootouts`; Tests `LivePenaltyUiTests`.
  `[APPROX]` die Schüsse dazwischen (Datei 0x3941A–0x39588) zeigt das Remake noch nicht (T7, `torszene-dauer` Teil 2).
- **Seite 2 Aufstiegsrunde und Seite 3 Europapokal:** umgesetzt in Spur `live-seiten-rest`, siehe §11.

## 11. Spur `live-seiten-rest` (S04 Rest): Aufstiegsrunde und Europapokal im Spielablauf

Gelesen mit `disasm.py em.exe 0x31CD5 0x32A80` und `0x3A88A 0x3ACE3`, **nachgebildet** mit
`tools/reverse-engineering/live/aufstieg_europapokal.py` (Seitenwähler aus den Bytes mit 0x31CD5 als Stub; 0x31CD5 aus den Bytes mit
vorbereiteten Paarungen DGROUP 0xCED8 und Ergebnissen 0xCDB8 + 0x48 · Wettbewerb + 0x24 · Gruppe + 6 · Spiel; Seitenwähler wie aus
dem Hauptmenü zusammen mit 0x31CD5).

- **Argumente** von 0x31CD5 (Datei 0x3AB05–0x3ABCE, sieben Wörter): Wettbewerb (1 Aufstiegsrunde, 2 Europapokal-Gruppen, 3
  Endrunde), Gruppe (Seite 2: [bp+8] − 1; Seiten 3/4: Seite − 3; Seite 5: 0), Merker „im Spiel“ [bp+0xA] = (Minute ≠ 0),
  Terminzahl [bp+0xC], 0 ([bp+0xE], nur die Gegnersuche 0x44A48 setzt ihn), Kalendermaske [bp+0x10] (0x40 bzw. 0x20), Merker
  „Drittelpause“ [bp+0x12] (Minute 20/40, Seitenwähler [bp−4]). Terminzahl = Basis + (Minute ∈ {0, 60}) − 1 mit Basis
  Aufstiegsrunde min(2 · DGROUP 0x7B0 + (0xCCA3 &gt; 0) + (0xCCA3 &gt; 2), 7), Europapokal-Gruppen min(0x7AF, 6), Endrunde 0x7AF − 6
  (bei 0x7AF &gt; 5, sonst 0x7AF). Ergebnis der Nachbildung (Gruppe 1, 0x7AF = 2): Minute 0 → (Zahl 2, Pause 0), 20/40 → (1, 1),
  60 → (2, 0), 65/70 → (1, 0). Die Basis ist während des Termins die Zahl der Termine davor (0x7AF zählt erst nach dem Termin
  hoch, Datei 0x314FC).
- **Regel in 0x31CD5** mit c = Terminzahl + Pause: Ergebnis für Spiel g = Runde + 3 · Rückspiel, wenn g &lt; c oder (g = c und „im
  Spiel“) (Datei 0x324C7–0x32515); Termin nur in der ersten Zeile einer Runde, ohne „im Spiel“ und für g ≥ c (Datei
  0x3228D–0x322C6). Im Spielablauf also nie ein Termin, nach 20/40/60 der laufende Termin mit seinem Stand (Live-Tore 0x3183C
  nach 20/40/65/70, Datei 0x3AAD9–0x3AAF8 bzw. aus 0x37A62), nach 65/70 ohne den laufenden Termin. Tabelle: erst bei Minute 60
  mit dem Termin (0x30852, Datei 0x38691).
- **Folge im Spielablauf:** Play-off-Tag mit Code 1 und Spiel 0xCCA3 ∈ {0, 2}: nach „Play-Off Spiele“ Seite 2 zweimal
  (Datei 0x38699–0x386C5: 0x3A88A(2, 1, m) und 0x3A88A(2, 2, m)), gefiltert mit Index 3 · Gruppe + (Minute = 60) – Gruppe 0
  „Drittelpause/Ergebnisse 2.Liga“ (3/4), Gruppe 1 „… Oberliga“ (6/7). Europapokal: Code 3 → Seite 3 (Gruppe 1) in den
  Gruppenterminen, Code 5 → Seite 5 (Endrunde) ab dem siebten Termin (Datei 0x31490–0x314A1). Die Knöpfe geben die nächste
  Seite an den Seitenwähler zurück, der sie ohne Filter mit derselben Minute ruft (Datei 0x3ACBB–0x3ACC1).
- **PW-04 aufgelöst:** Die Videobilder ohne Termine nach den ersten Spielen sind Seiten im Spielablauf – `eisbaeren_f20_02950`/
  `02951` (davor das Live-Spiel 02930), `eisbaeren_f51_03311`/`03314` (Minute 60 des zweiten Termins nach dem Live-Spiel 03300),
  `eisbaeren_f51_03345`/`03346` (Minute 60 des dritten Termins: drei Spiele in der Tabelle, keine Termine), `eisbaeren_f58_01323`
  (Endrunde, vier Spiele in der Tabelle, danach das Eis 01332). Im Hauptmenü zeichnet em.exe dagegen die Termine offener Spiele
  (Nachbildung Teil 3: 0x7AF = 3 → Termine der drei Rückspiele), wie die Aufstiegsrunde in `folge06_01257`.

**Umsetzung:** `EuropeanCupScreen` (`ShownGames`, `ShowsResult`, `ShowsDate`, `Cell`, `Standings`, Ansicht mit `GroupLive`),
`PromotionRoundPage.Of(game, group, live)`, `LivePageScreens` (Fall Aufstiegsrunde und Europapokal, zweite Aufstiegsgruppe in
`For`, Ansicht der Europapokalseite). Tests `LiveSeitenRestTests` (Regeln aus der Nachbildung, Aufstiegstag mit beiden Gruppen,
Gruppe mit laufendem Termin, Endrunde ab Termin 7, Europapokal-Termin im Live-Spiel) und `PixelWettbewerbeTests` (PW-04).
Bildschirmfotos: Ablauf `live-aufstieg-europapokal` (`Tools/Scenarios/LiveSeitenRest.cs`), Vergleich mit `eisbaeren_f51_03346` und
`eisbaeren_f58_01323` (Fenster nach Verschiebung um (−0,25 | +0,5) und Unschärfe: mittlere Abweichung 7,3 bzw. 8,0 Graustufen,
5 bzw. 0 Pixel über 60).

**[APPROX]/offen:** (1) Das Remake rechnet die zwei Gruppen eines Termins (Aufstiegsrunde, Europapokal) je Gruppe nacheinander
(Teil 2b); die Gruppe ohne Live-Spiel zeigt daher den Endstand des Tages bzw. noch nichts statt des laufenden Stands. (2) Die Knöpfe
„Liga“ der Aufstiegsseite öffnen die Ligapaarungen ohne Minute (Original: Seite 7 mit Minute). (3) Der Bildschirmfoto-Ablauf
`pixel-europapokal` (`WettbewerbeVideo.cs`, außerhalb der Dateigrenzen) zeichnet `eisbaeren_f51_03346` und `eisbaeren_f58_01323`
weiter als Hauptmenü und zeigt nun Termine; der Vergleich steht im neuen Ablauf.
