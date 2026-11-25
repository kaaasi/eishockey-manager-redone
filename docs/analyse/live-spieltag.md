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
