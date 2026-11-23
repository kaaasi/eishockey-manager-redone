# Spur `re-reste` – Reste ohne Beleg per em.exe (Runde 23)

Bericht der Spur `re-reste` (R23-D2 = D, Teil 1, 23.11.2026). Alle Adressen sind Dateioffsets in `em.exe` (DOS-Version,
`~/referenz/original/gamedata/em.exe`); Disassembly mit `python3 tools/reverse-engineering/disasm.py <em.exe> <lo> <hi>` bzw. je
Beleg ein `ndisasm`-Einzeiler, der die entscheidende Anweisung zeigt (im Spielordner ausführen). Spielstände aus
`~/referenz/original/spielstaende/` mit `emtool hok dump`.

Ergebnis in einem Satz je Punkt: #925 (b) gilt auch im DEB-Pokal und weicht im Remake ab (umsetzen, Teil 2); #938 P3-3 ist nicht
belegt, der Zug 0x8C53 stimmt mit dem Original überein, die Messung ist durch die Energie-Rücksetzung des Bildschirms „WM
Vorbereitung“ erklärbar (keine Änderung); A876 K bestätigt (Saisonwechsel setzt nur die Sätze 0 … [0x386] − 1 zurück); A917 K
bestätigt (das Original schreibt an Ereignistagen die Felder aller Sätze neu, das Nachbilden bräuchte das Eingesetzt-Byte).

## 1. #925 (b) – Gleichstand nach dem Zusatzwurf im DEB-Pokal (0x3166E, 0x32B5F)

**Frage:** Bleibt im schnellen Weg der Pokale ein eigener Gleichstand nach dem Wurf x = y stehen auch im DEB-Pokal, und für wen zählt
er dort? Das Remake (`CupMatch`, `OriginalMinutes.FastCupResultWithTie`) würfelt im DEB-Pokal „bis ungleich“ mit einer Marke (A003).

**Lesung.** Der schnelle Weg 0x3731F trägt Heim- und Gasttore nach DGROUP 0x5C4B/0x5C4C (Zeile · 32 + Paarung) ein und ruft bei
K.-o. (`[bp+0xA]` ≠ 0) die Markierung `0x32AA3(Wettbewerb, Zahl, 0, Tabelle)` (Datei 0x376D2–0x376E6). Diese behandelt den
DEB-Pokal ausdrücklich (0x32AF4 `cmp byte [bp+0x6],0x1`, Wettbewerbsbit 0 = Zeile 0): Für jede Paarung mit Heimbyte = Gastbyte
(0x32B23) oder Heimbyte − 30 = Gastbyte (0x32B3F–0x32B5A), deren gesichertes Ergebnis in DGROUP 0xD014 + 32 · [0xD074] noch nicht
100 („nicht ausgetragen“, 0x32B71/0x32B88) ist, setzt sie **Heimbyte + 30** (0x32B9D–0x32BAA) und meldet „Gleichstand gefunden“
(`[bp−2]` = 1, Rückgabe 0x32E1C). Nur mit Modus ≠ 0 (Minutenweg mit Manager) folgt das Penaltyschießen 0x3901B (0x32DE3–0x32DFA);
der schnelle Weg übergibt 0.

Danach würfelt 0x376F0 x = random(0, 2), y = random(0, 2) und vergleicht **Heimbyte + x** mit **Gastbyte + y** (0x37724–0x37748).
Weil das Heimbyte der eben gerechneten Paarung schon die Marke trägt, ist der Vergleich nie gleich: **genau ein Wurf**. Dann
Heimbyte := Heimbyte + x + 30 (0x3775D–0x37774, zweite Marke), Gastbyte := Gastbyte + y (0x37788–0x3779F). Bei x = y (Wahrscheinlichkeit
1/3: `random(0, 2)` ist inklusiv, drei Werte je Seite; RR-02) bleibt der Gleichstand mit zwei Marken stehen – wie bei den Play-offs
(#925 (b)).

**Wertung im DEB-Pokal** (anders als 0x3166E der Play-offs): Der Pokaltermin 0x34B76 zieht Marken ab, solange das Heimbyte > 29 ist
(0x34C06–0x34C2A), trägt das Spiel über 0x50283 ein (0x34C5C) und zählt den Sieg: `cmp Heimtore, Gasttore; jnc` → **Heimtore ≥
Gasttore zählt für den Platz 2k** (0x34C76–0x34C83, Siege DGROUP 0xD075 + Paarung), sonst Platz 2k + 1 (0x34C90). Platz 2k ist der
Heimverein des Spiels (erster Parameter von 0x50283 = DGROUP 0x5CFB + Paarung; Tausch nach den Spielen 1 und 2). **Ein stehender
Gleichstand zählt im DEB-Pokal also für den Heimverein**, bei den Play-offs für den Gast.

**Folge für das Remake.** Original bei Gleichstand im schnellen Weg: Heim gewinnt mit 2/3 (x = y 3/9 oder x > y 3/9), Gast mit 1/3
(x < y; RR-02); Endstand
Gleichstand + x : + y mit zwei Marken (Anzeige zwei Punkte, 0x338AB). Remake: bis ungleich, je 1/2, eine Marke, nie ein Gleichstand.
Der HOK-Import (`OriginalImport.Cups.cs`, `KnockoutMatch.Winner` → `PenaltyWinner ?? Home`) wertet ein importiertes 4:4 mit zwei Marken
schon für den Heimverein – stimmt mit 0x34C7A überein. Abweichung spielwirksam, Größe S, Klasse **V**; Umsetzung in Teil 2
(`FastCupResultWithTie` für den DEB-Pokal wie `playoff`, aber Rückfall auf den Heimverein; `CupMatch.FromReport` Marken 2).
Europapokal und WM-Endrunde laufen in 0x32AA3 nicht über Zeile 0 (Zeilen 2/3 nur für `[bp+0x6]` ≠ 1, 0x32C7F–0x32C8B) – hier nicht
gelesen, unverändert lassen.

**Zusatzwurf für ungleiche Paarungen (RR-01, Review #965, Fixlauf 23.11.2026).** Die Markierung 0x32AA3 läuft über **alle** Plätze
der Zeile 0, nicht nur über die eben gerechnete Paarung. Noch nicht gerechnete Paarungen des Termins stehen auf 0:0 (0x34C94–0x34CAA
setzt 5C4B/5C4C nach jedem gewerteten Spiel zurück, 0x34F7D zu Rundenbeginn; D014-Byte 0 ≤ 0x63 → offen), erfüllen also „Heim = Gast“
(0x32B23) und werden mit markiert (30:0); der Rückgabewert 1 löst den Wurf für die **eben gerechnete** Paarung aus, auch wenn ihr
Ergebnis ungleich ist (0x376E9 `or al,al; jnz 0x376F0`). Ihr Heimbyte trägt dann keine Marke, 0x37748 schleift bis Heim + x ≠ Gast + y,
0x3775D setzt **eine** Marke; der Sieger folgt der Summe (0x34C76 nach Abzug der Marken 0x34C06–0x34C2A) und kann kippen (Abstand 1:
x = 0, y = 2, einer von sieben ungleichen Würfen → 1/7). Beim nächsten Durchlauf stehen die offenen Paarungen auf 30:0 und erfüllen „Heim − 30 = Gast“ (0x32B3F–0x32B5A,
`add ax,0xffe2` einmal) → 60:0. **Beleg strittig** gegenüber RR-01 („jede Paarung, hinter der eine offene aussteht“): Bei 60:0 greift
keiner der beiden Vergleiche mehr (60 ≠ 0, 60 − 30 = 30 ≠ 0; es gibt keine Schleife über mehrere Marken, anders als bei der Wertung
0x34C06), die Markierung meldet also nur nach der **ersten und zweiten** gerechneten Paarung eines Termins einen fremden „Gleichstand“.
Remake: `MatchEngine.DebCupExtraRoll` (ersten zwei gerechnete Paarungen in Platzfolge mit offener Paarung dahinter; eine Paarung der
Minutenschleife zählt als gerechnet, Markierung 0x37211), `FastCupResultWithTie(debExtraRoll)` würfelt bis ungleich, `CupMatch.FromReport`
trägt Summe und eine Marke (`ReResteTests`). Spielstände unterscheiden beide Lesarten nicht (alle Mehrpaarungstermine in FILE01–FILE76
sind Managertage → Minutenweg); ein Videobild eines DEB-Ergebnisbildschirms ohne Managerverein mit drei und mehr Paarungen (dritte
Paarung ohne Punkt bei ungleichem Ergebnis) würde entscheiden.

```bash
ndisasm -b16 -o 0x32B3F -e 0x32B3F em.exe | head -1   # add ax,0xffe2   (einmal − 30, keine Schleife)
ndisasm -b16 -o 0x32B58 -e 0x32B58 em.exe | head -3   # cmp ax,dx / jz 0x2b5f / jmp 0x2dfd   (sonst nächster Platz, keine Marke)
ndisasm -b16 -o 0x376E9 -e 0x376E9 em.exe | head -2   # or al,al / jnz 0x76f0   (jede Meldung würfelt für die eben gerechnete Paarung)
```

**Beleg Spielstand:** FILE44 (14.04.1994, nach dem Endspiel), Block D014: Spiel 1 `40 04` = 4:4 mit zwei Marken (64 = 4 + 60),
Spiel 2 `03 3F` = 3:3 mit zwei Marken beim Heimverein Platz 1, Spiel 3 `01 04`; Plätze 5CFB `01 09`, Runde 5CF3[0] = 5, Zähler
D074 = 2. Zwei stehende Gleichstände in einem Endspiel ohne Managerverein (Köln = Verein 2 ist nicht beteiligt) – der schnelle Weg
erzeugt sie im DEB-Pokal. (Die Siegerzuordnung ist hier nicht unterscheidbar: Spiel 3 entscheidet bei beiden Lesarten für Platz 2k + 1.)

**Ausführbar:**

```bash
ndisasm -b16 -o 0x32AF4 -e 0x32AF4 em.exe | head -1   # cmp byte [bp+0x6],0x1   (DEB-Pokal-Zweig der Markierung)
ndisasm -b16 -o 0x32B9D -e 0x32B9D em.exe | head -2   # mov al,[bx+0x5c4b] / add al,0x1e   (Marke vor dem Wurf)
ndisasm -b16 -o 0x37746 -e 0x37746 em.exe | head -2   # cmp ax,dx / jz 0x76f0   (Vergleich mit markiertem Heimbyte)
ndisasm -b16 -o 0x3775D -e 0x3775D em.exe | head -2   # add dl,[bx+0x5c4b] / add dl,0x1e   (zweite Marke)
ndisasm -b16 -o 0x34C76 -e 0x34C76 em.exe | head -2   # cmp al,[bx+0x5c4c] / jnc   (Heim ≥ Gast → Sieg Platz 2k)
emtool hok dump ~/referenz/original/spielstaende/FILE44.HOK D014 5CFB 5CF3 D074
```

## 2. #938 P3-3 – Belastungszug 0x8C53 im Länderspiel

**Frage:** Trifft der Belastungszug im Länderspiel des Bundestrainers zu oft? Review #937: FILE19 → FILE20 (5 WM-Spiele) JOORIS
+0x13 = 76, Remake bei acht Saaten 76 … 86.

**Lesung.** Der Managerteil der Minute (0x87F7) geht je Kaderplatz des Satzes `[0xA70E]` (0x8B31–0x8B54: 0x5DC3 + 0x618 · Satz +
0x34 · Platz; der Bundestrainer zieht als Satz 5) über die eingesetzten Spieler (+0x0A ≠ 0, 0x8B57). Ein Spieler auf dem Eis
(Position & 0x7F − 1 = aktuelle Reihe des Teils, 0x8B96–0x8BA7) bekommt mit `lrandom(0, 4 + [Kondition > Technik]) = 0` Belastung
+0x13 + 1 (0x8C53–0x8C82); sonst, unter 90 Energie, Energie + 1 + Bonus (0x8BA9–0x8C06). Das entspricht Zeile für Zeile
`MatchEngine.Managed.cs` (Zug, Bedingung, Byte-Zähler). **Im Zug selbst gibt es keine Abweichung.**

**Erklärung der Messung.** Der Bildschirm „WM Vorbereitung“ (0x3B8F7, Aufrufer Hauptmenü 0x44E22) setzt bei jedem vollständigen
Aufbau (`[bp−5]` = 1 beim Eintritt, 0x3B8FF; erneut nach einem Freundschaftsspiel, 0x3C291) die Belastung **aller belegten Plätze des
Satzes 5 auf 70** (Schleife 0x3BB20–0x3BB5A über 29 Plätze: +0x0F ≠ 0 → +0x13 := 0x46). Jedes Öffnen des Bildschirms zwischen zwei
WM-Spielen löscht also die Züge der vorangegangenen Spiele; FILE20 zeigt dann nur die Treffer seit dem letzten Öffnen (76 = sechs
Treffer, plausibel für ein Spiel mit ≈ 20–25 Minuten Eiszeit bei 1/4 … 1/5). Ob und wann der Bildschirm in der Aufnahme geöffnet wurde,
ist nicht dokumentiert; die Probe des Reviews nimmt fünf Spiele ohne Rücksetzung an und vergleicht ohne gleichen Zufallsstrom.

**Folge.** P3-3 bleibt **nicht belegt**; keine Änderung am Zug. Die Rücksetzung beim Öffnen ist A883 (Klasse E, „Öffnen ändert
nichts“, Nutzer E4 = B) – das Remake setzt nur nach dem Freundschaftsspiel zurück (`CupsNational.GameState.cs`, A551). Teil 2 ergänzt
den Kommentar an 0x8C53 in `MatchEngine.Managed.cs` um diesen Zusammenhang (eigene Datei der Spur); der Kommentar in
`CupsNational.GameState.cs` Z. 321 (nur nach Merge von `v-rest`) braucht keine Änderung. Wer den Zufallsstrom später angleicht, muss
für die Probe FILE19 → FILE20 wissen, wann „WM Vorbereitung“ offen war.

**Ausführbar:**

```bash
ndisasm -b16 -o 0x8C53 -e 0x8C53 em.exe | head -6     # dl=0; al=[+0x10] Kondition; cmp [+0x11] Technik; jna; dl=1; … add ax,4
ndisasm -b16 -o 0x8C7F -e 0x8C7F em.exe | head -2     # les bx,[bp-0x10] / inc byte [es:bx+0x13]   (Belastung + 1)
ndisasm -b16 -o 0x3B8FF -e 0x3B8FF em.exe | head -1   # mov byte [bp-0x5],0x1   (Vollaufbau beim Eintritt)
ndisasm -b16 -o 0x3BB51 -e 0x3BB51 em.exe | head -3   # mov byte [bx+0x5dd6],0x46 / inc cx / cmp cx,0x1d   (29 Plätze auf 70)
```

## 3. A876 – Zähler Über-/Unterzahl des Satzes 5 über den Saisonwechsel

**Frage:** Überleben die sechs Wörter DGROUP 0x97D2 + 0x1AF · Satz + 2 · k (Manager +0x177) des Satzes 5 den Saisonwechsel und
eine neue Ernennung? Beleg bisher nur Spielstände (FILE19 → FILE20 → FILE21).

**Lesung.** Alle Schreiber der Zähler (Querverweis `disasm.py --xref 0x97D2 0x97DD`): 0x35B18 `inc` (Zahl der Spiele beim Strafwurf),
0x35CB2 `dec`, und genau ein Rücksetzer 0x558A5 `mov word [bx+0x97D2],0` in der Schleife 0x55893–0x558B2 (k = 0 … 5) des
Saisonwechsels (Funktion 0x54C9F, Block 0x55861–0x55DC1). Diese Schleife läuft über `[0xA70E]` = 0 … **[0x386] − 1** (0x55850–0x5585C,
`cmp al,[0x386]; jc`), also nur über die Vereinsmanager; Satz 5 wird nie erreicht. Einen weiteren Rücksetzer gibt es nicht; die
Ernennung schreibt die Zähler nicht. Das bestätigt FILE20 `1,0,0,90,3,5` → FILE21 (neues Amt) unverändert.

**Folge.** Klasse **K** bestätigt: Das Remake legt je Saison ein neues `NationalCoach` an (`CupsNational.GameState.cs:369`, Datei nur
`v-rest`), die Zähler des Satzes 5 werden nirgends gelesen und nicht exportiert. Vor einem Export der Zähler: Übertrag beim
Saisonwechsel und bei der Ernennung (Spielstandfeld, Schema), Entscheidung Leitstelle; nicht Teil 2 dieser Spur (fremde Datei).

**Ausführbar:**

```bash
ndisasm -b16 -o 0x55850 -e 0x55850 em.exe | head -4   # mov byte [0xa70e],0 / mov al,[0xa70e] / cmp al,[0x386] / jc
ndisasm -b16 -o 0x558A5 -e 0x558A5 em.exe | head -3   # mov word [bx-0x682e],0x0 (= 0x97D2) / inc word [bp-0xa] / cmp …,6
python3 tools/reverse-engineering/disasm.py em.exe 0x97D2 0x97DD --xref --cache <scratch>/an.pickle   # fünf Treffer, ein Rücksetzer
```

## 4. A917 – HOK-Import: Vereinsfelder 0x18–0x1D nach der Neuaufstellung (0x54D84–0x54DAA)

**Frage:** Für welche Sätze schreibt das Original nach dem Laden die Vereinsfelder 0x18–0x1D neu? Der Import
(`OriginalImport.Rest.cs`, `ApplyLoadLineupValues`) tut es nur für den Manager am Zug mit 0x375 ≠ 0x376.

**Lesung.** Der Tageslauf 0x54C9F prüft am Schleifenkopf das Ereignis des Tages (0x54D41–0x54D65: Kalender `[0xA969] + 0x828`,
9 → 0; 0 oder 0xFF → kein Ereignis, Sprung 0x55073). An einem Ereignistag läuft 0x54D68–0x54DBA über **alle Sätze `[0xA70E]` = 0 … 5**:
Satz < [0x386] (0x54D70) oder Satz 5 mit Bundestrainer (`[0x9EDA]` < 0x3F, 0x54D76–0x54D82). Je Satz: 0x375 := 0x376 (0x54D84–0x54D9A),
Neuaufstellung 0x426BC (kehrt sofort zurück, wenn 0x375 = 0 ist: 0x426CC–0x426E4), dann **0x952C(Satz, 0)** (0x54DA3–0x54DAA) –
die Vereinsfelder 0x18–0x1D aus der aktuellen Aufstellung. Nach dem Laden (Fortsetzung am Schleifenkopf, #733) geschieht das für
jeden Satz, nicht nur für den Manager am Zug, und unabhängig von 0x375 ≠ 0x376.

**Folge.** Die Beschränkung des Imports auf den Manager am Zug mit Umschaltung ist eine Näherung; das Original rechnet die Felder
aller Sätze (auch Satz 5) am Ereignistag neu, und zwar aus der Aufstellung, die nach der Neuaufstellung 0x426BC gilt. Die Abweichung
ist nur sichtbar, wenn sich seit dem letzten Schreiben Werte bewegt haben (Wanderung 0x9D1B, Verletzungen, Transfers) oder die
Automatik anders aufstellt. Klasse **K** bestätigt mit der schon vermerkten Begründung: Das Nachbilden für alle Sätze bräuchte die
Eingesetzten-Bytes (+0x0A) im Export (FILE06 `OriginalExportRoundTripTests`), sonst ändert der Export der Felder. Belegt ist jetzt,
**dass** das Original alle Sätze schreibt; ob es sich lohnt, entscheidet die Leitstelle (Ausgabestände, Vergleichstests). Nicht Teil 2.

**Ausführbar:**

```bash
ndisasm -b16 -o 0x54D70 -e 0x54D70 em.exe | head -6   # cmp al,[0x386]; jc / cmp byte [0xa70e],5 / cmp byte [0x9eda],0x3f / jnc
ndisasm -b16 -o 0x54D9E -e 0x54D9E em.exe | head -5   # call 0x108:0x4d (0x426BC) / push 0 / push [0xa70e] / call 0x28:0x146c (0x952C)
ndisasm -b16 -o 0x54DB1 -e 0x54DB1 em.exe | head -3   # inc byte [0xa70e] / cmp byte [0xa70e],0x6 / jl
ndisasm -b16 -o 0x426CC -e 0x426CC em.exe | head -3   # mov al,[bx+0x375] / dec al … (0 → Ende ohne Neuaufstellung)
```

## Teil 2 – Umsetzung (23.11.2026)

- **#925 (b) umgesetzt:** `OriginalMinutes.FastCupResultWithTie(…, debCup: true)` würfelt im DEB-Pokal genau einmal (Vergleich mit
  markiertem Heimbyte); `MatchReport.FastCupPath` kennzeichnet den schnellen Weg; `CupMatch.FromReport(…, debCup: true)` setzt zwei
  Marken und wertet den stehenden Gleichstand für den Heimverein (0x34C76). Anschluss in `MatchEngine.Live.cs` (nur die Übergabe
  `debCup`/`FastCupPath`, minimal). Regressionstests `ReResteTests` (rot vorher: 3/3). Angepasste fremde Tests (R15-E2 = C):
  `WettbewerbeSpiel1Tests.DebCup_NoMatchEndsLevel_ShootoutInFinalScore` (schneller Weg: Marken 0/2, Gleichstand für Heim; Rückschaltprobe
  alter Code → rot), `FastPath_ReportCarriesExtraGoals` (x = y erlaubt; Rückschaltprobe grün, nur gelockert). WM-Endrunde und
  Europapokal unverändert (Zeilen 2/3 in 0x32AA3 nicht gelesen – Restlücke).
- **#938 P3-3:** nicht belegt, keine Änderung am Zug; Kommentar an 0x8C53 in `MatchEngine.Managed.cs` ergänzt. Vermerk E (A883).
- **A876, A917:** Vermerk K bestätigt (siehe §3, §4), keine Code-Änderung; Folgearbeiten (Übertrag Satz 5, Eingesetzt-Byte im
  Export) Entscheidung Leitstelle.

## Übersicht für `pruefung-karte`

| Punkt | Ergebnis | Klasse | Teil 2 |
|---|---|---|---|
| #925 (b) DEB-Pokal | gilt: eine Wurfrunde, zwei Marken, Gleichstand zählt für den Heimverein (0x34C7A) | V (S) → umgesetzt | `MatchEngine.Minutes.cs`, `Cups.Tiebreak.cs`, `MatchEngine.Live.cs`, `ReResteTests` |
| #938 P3-3 | Zug stimmt; Messung durch Rücksetzung in „WM Vorbereitung“ (0x3BB51, A883 E) erklärbar | nicht belegt | Kommentar 0x8C53 ergänzt |
| A876 | Saisonwechsel setzt nur Sätze 0 … [0x386] − 1 zurück (0x55858); Satz 5 bleibt | K bestätigt | nein (Datei `v-rest`) |
| A917 | Original schreibt an Ereignistagen alle Sätze neu (0x54D68–0x54DBA) | K bestätigt | nein (Eingesetzt-Byte fehlt) |
