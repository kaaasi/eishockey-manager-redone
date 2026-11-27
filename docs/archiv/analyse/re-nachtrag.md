# Analyse `re-nachtrag` – Analysebedarf Gruppe d (Runde 11)

Runde 11, Spur `re-nachtrag` (nur Doku, Regeln wie Runde 10). Disassembly `ndisasm -b 16` bzw.
`tools/reverse-engineering/disasm.py` von `em.exe`; alle Adressen sind **Dateioffsets** von em.exe (`Datei 0x…`),
DGROUP-Variablen stehen ausdrücklich als „DGROUP“. Kaderplatz = DGROUP 0x5DC3 + 0x618 · Satz + 0x34 · Platz (Satz =
Manager, 4 = Transfermarkt, 5 = Nationalteam), Pool = far DGROUP 0x8253 + 33 · Index, Verein = far DGROUP 0x9657 + 61 · Index.
`random(a, b)` (Datei 0x0C037) liefert a … b einschließlich. Belege aus Spielständen: `emtool hok dump` an
`original/spielstaende/FILE01…FILE14` des Referenz-Repos (nur gelesen, nichts kopiert).

## Ergebnis in Kürze

1. **Ankündigungen (P1 Nr. 9):** Das Original kennt genau zwei Ankündigungen im Tagesablauf: **Ruhestand** (Kaderplatz +0x18
   Bit 0x80, Text „… kündigt an, daß er seinen Vertrag nicht mehr verlängern wird.“) und **Verlängerungsangebot**
   (+0x18 = 100 + n, „… bietet Ihnen an, von 1 auf n Jahre zu verlängern.“). Eine eigene „Absage“ gibt es nicht – der
   Absagetext *ist* die Ruhestandsmeldung. Ruhestand hängt nicht an der Vertragslaufzeit, sondern an Alter, Rolle und
   Technik − Kondition (Feldspieler erst ab 36, Torhüter ab 42). Am Saisonende hören Spieler ohne Managerkader nach
   Alter > random(37, 42) auf und werden im selben Pooleintrag als junger Spieler neu angelegt.
2. **Angebote der Computervereine (A136):** Bit 0x80 und Verein +0x16 schreibt der Tagesablauf **nur in Plätze des
   Transfermarkts** (Satz 4) für Spieler, die der Manager am Zug dort anbietet. Das `[APPROX]` „Original kennt Angebote für
   alle Spieler“ in `Player.Value.cs` ist falsch; die Spieler im eigenen Kader bekommen das getrennte Bit 0x40, das in den
   Spielerwert nicht eingeht.
3. **Automatische Aufstellung:** Datei 0x426BC stellt nur bei gesetzter Einstellung (DGROUP 0x375 + 2 · Satz, Werte 0/1)
   **vollständig neu** auf: 1 Torhüter, drei Abwehrpaare, drei Sturmreihen, danach Ersatz reihum bis 22 Eingesetzte; Wahl
   je Rolle nach (Kondition + Technik + Form) / 3, mit Ausweichrollen. Ohne Einstellung ändert sie nichts.
4. **Liga-Spielplan:** Datei 0x4E7C7 rechnet keine Paarungen aus, sondern liest eine **feste Tabelle** (11 Spieltage für
   12 Vereine als Buchstabenpaare, DGROUP 0x2DC); jede weitere Runde mit vertauschtem Heimrecht.
5. **Preisindex:** DGROUP 0x2BC wird außer von Laden/Speichern nie geschrieben – bleibt 100 (`[ORIGINAL]`).
6. **Kaderplatz +0x19:** Start beider Halbbytes random(3, 12) (Spielstart, Talente, Listenwechsel, Pool-Neuanlage);
   Nationalteam-Kopien 0x77; Training verschiebt sie (0 … 15). Spielstände FILE01/09/10/12 bestätigen 3 … 12.
7. **Europapokal-Gegner, Freundschaftsspiele, Penalty-Zeilen:** Auslosung vergleicht das Mittel der **sechs** Teamwerte
   (Formabschlag −5 wirkt); Freundschaftsspiele laufen durch den Spieltagsablauf mit Code 0xD wie ein WM-Gruppenspiel
   (kein K.-o.), ohne Tabellen- und Serieneintrag; Penaltyschießen nur bei DEB-Pokal, Play-offs und WM-Endrunde.
8. **Verein beim Spielstart:** Der gewählte Verein wird immer in die Start-Liga getauscht (Tausch mit einem Zufallsverein
   der Start-Liga, der kein Managerverein und nicht im Europapokal ist); Sonderfall eine Saison mit einem Manager: Tausch
   auf einen Europapokal-Platz der Bundesliga.
9. **DOSBox-Spielstandpaar:** FILE13/FILE14 taugen nicht für die Zähler (FILE13 ist gepatcht, alle Zähler 0). Ablauf für ein
   echtes Paar in Abschnitt 9; Kauf hängt an den ersten freien Platz an (FILE05 → FILE07).

## Zuordnung der Adressen der Spur

| Adresse | Stelle | Ergebnis | Abschnitt |
|---|---|---|---|
| Datei 0x07169 | Tagesablauf je Manager (Aufrufer Datei 0x54C9F) | Ruhestand, Verlängerungsangebot, Verfall, Marktangebote zugeordnet | 1, 2 |
| Datei 0x07358, Datei 0x0736E | in Datei 0x07169: Marktangebot Bit 0x80 und Verein +0x16 | nur Satz 4 (Transfermarkt) | 2 |
| Datei 0x426BC | Neuaufstellung (Stub 0x108:0x4D) | Regel gelesen | 3 |
| Datei 0x42838 | Spielerwahl je Rolle (Stub 0x108:0x52) | Regel gelesen | 3 |
| Datei 0x4E7C7 | Spielplan (Stub 0x128:0x34) | feste Paarungstabelle | 4 |
| DGROUP 0x2B8, 0x2BC (Preisindex) | Leser Datei 0x3E811, Datei 0x59DBF | kein Schreiber außer Laden/Speichern | 5 |
| Kaderplatz +0x19 | Schreiber Datei 0x06B01, 0x06F23, 0x299F4, 0x3CB2C, 0x49803, 0x5B239 | Startwerte belegt | 6 |
| Datei 0x341AB | Auslosung Europapokal (Stärke über Datei 0x4D316) | Mittel der sechs Teamwerte | 7.1 |
| Datei 0x30365, Datei 0x3C05E | Freundschaftsspiel (Stub 0xE8:0x39, Aufruf im Nationalteam) | Code 0xD, Zeile 3, kein K.-o. | 7.2 |
| Datei 0x555D6 | in Datei 0x54C9F: Zug Nationalteam | Aufruf Datei 0x0952C nach (wirkungsloser) Neuaufstellung | 3.4 |
| Datei 0x32AA3 | Prüfung Unentschieden/Penalty der Zeilen DGROUP 0x5C4B | Zeilen und Wettbewerbe zugeordnet | 7.3 |
| Datei 0x45CD0, Datei 0x29C3B | Verein in Paarungszeile; Vereinsplatz beim Spielstart (in Datei 0x28165) | Regel gelesen | 8 |
| A085, A088, A091, A125 | DOSBox-Spielstände | A091/A125 teilweise, A085/A088 offen (Paar fehlt) | 9 |

## 1. Tagesablauf Datei 0x07169 – Kaderplätze des Managers

Einstieg nur im Zug eines Managers (DGROUP 0xA70E < DGROUP 0x386, Datei 0x07172). Die Schleife über die Kaderplätze
(Datei 0x073FA–0x0795D) läuft nur bis zum Saisonende (32-Bit-Tageszähler DGROUP 0x3B7 < 233, Datei 0x073E3). Je Platz in
dieser Reihenfolge:

1. Verletzung außerhalb des Spiels (Datei 0x07462–0x07548, schon in `spielberechnung.md`, Verletzung 0x05B1A).
2. **Ruhestand** (Datei 0x0754D–0x0760F), Daten Pool +0x14 (Alter), Pool +0x18 (Rollenwert, 0–24 = Torhüter), Kaderplatz
   +0x10/+0x11 (Kondition/Technik):

   ```
   c = 100 − 15 · (Rollenwert == 0)                    // Torhüter 85
   if (Technik − Kondition > 0) c −= (Technik − Kondition) / 2
   v = Alter · c / 10
   if (random(350, 450) < v && !(+0x18 & 0x80) && +0x18 < 100)
       +0x18 |= 0x80; Meldung Art 4 (Zeile 1 Name, Zeile 2 leer: DGROUP 0x4911) über Datei 0x4B2BA
   ```

   Der Wurf fällt **jeden Tag für jeden Platz** (auch geliehene, ohne Rücksicht auf die Laufzeit). Feldspieler mit c = 100:
   unter 36 nie, mit 36 täglich 10/101, mit 40 50/101 – praktisch hört jeder Feldspieler ab 36 binnen weniger Tage auf;
   Torhüter erst ab 42 (42 · 8,5 = 357). Technik über Kondition schiebt das Alter nach oben.
   Meldungsarten (Vorlagen DGROUP 0xCA0 + 4 · Art, far): 0 „$ bietet Ihnen an, $ Jahre zu verlängern.“, 1 „$ ist an $
   interessiert.“, 2 zwei Zeilen, 3 „$ gibt das Eishockeyspielen im Alter von # Jahren endgültig auf.“, 4 „$ kündigt an, daß er
   seinen Vertrag nicht mehr verlängern wird.“, 5 „$ $ $“, 6 „$ ist Sportinvalide. …“.
3. Gesperrter Spieler (+0x0D ≠ 0): +0x0A (eingesetzt) = 0 und Neuaufstellung Datei 0x426BC (Datei 0x07612–0x07621) – das
   Herausnehmen gilt **auch ohne** Einstellung „automatisch“.
4. **Verfall offener Angebote** (Datei 0x07626–0x07673): random(0, 3) = 0, +0x09 > 0x1F und offene Meldung (Pool +0x1E ≠ 0) →
   +0x09 &= 0x1F, Meldung gelöscht (Datei 0x4B270). Dasselbe für die Marktplätze (Datei 0x07215–0x0726A).
5. **Verfall eines Verlängerungsangebots** (Datei 0x0767A–0x076E3): +0x18 > 99 ohne Bit 0x80, random(0, 5) = 0 und offene
   Meldung → +0x18 = random(9, 17) (Verhandlungssperre in Tagen), Meldung gelöscht. Sonst +0x18 in 1 … 99 → −1
   (Datei 0x076F6, Sperre, schon umgesetzt).
6. **Verlängerungsangebot** (Datei 0x076FA–0x07951), nur ohne Leihe (+0x0C = 0):

   ```
   s  = (Kondition + Technik + Form) / 3                       // +0x10..+0x12
   a  = 100 − 100 · min(8, |25 − Alter|) / 8                   // Datei 0x4BDE6 = |a − b|; Alter 25 → 100, ≤ 17 oder ≥ 33 → 0
   e  = min(5, (+0x1C + +0x1E + +0x20) / 36)                   // Spielzähler (Wörter)
   p  = (60 · s + 40 · a) / 100;  p −= p · e / 10
   if (random(0, p) == 0 && random(0, 3) == 0 && Pool +0x1E == 0 && !(+0x18 & 0x80) && +0x0B == 1)
       r = random(0, 100); n = r > 90 ? 4 : r > 70 ? 3 : 2      // 71/101, 20/101, 10/101
       +0x18 = 100 + n; Meldung Art 0 mit „von “ + Laufzeit + „ auf “ + n (DGROUP 0xD78/0xD74); Pool +0x1E = Meldung
   ```

   Tägliche Chance 1/(4 · (p + 1)); Beispiel s = 50, Alter 25, keine Spiele: p = 70 → 1/284 je Tag. Alte und sehr junge
   Spieler melden sich häufiger, Stammspieler (viele Spiele) ebenso. Es gibt **keine Frist**: Wer sich bis zum Saisonende
   nicht meldet, geht in die bekannte Verhandlung am Saisonende (+0x18 = 101, `reverse-engineering.md`, Datei 0x27D05).
7. Unaufgeforderte Angebote Bit 0x40 (Datei 0x07972–0x07AEF), Heilung und Training (Datei 0x07B4E, 0x07B5E), Tendenz
   (Datei 0x07B67–0x07BF8): unverändert wie in `transfer.md`/`spielberechnung.md`. Am Ende Neuaufstellung (Datei 0x07C07).

**Saisonende, Laufbahnende** (Saisonende Datei 0x26F20, Pool-Schleife 1–199 im Zug von Manager 0, Datei 0x2787A–0x279DA):
Pooleintrag ohne Kaderplatz eines Managers und Alter > random(37, 42) – oder mit Kaderplatz, abgelaufenem Vertrag (+0x0B = 0)
und Bit 0x80 – hört auf: Meldung Art 3 (nur bei Kaderplatz; Besitzer Pool +0x1A = 6, Platz frei über Datei 0x425B6), dann
**Neuanlage im selben Pooleintrag**: Alter random(18, 25), s = random(40, 92), Pool +0x15 und +0x16 je random(s − 5, s + 5),
Pool +0x19 (Länderspiele) = 0. Der Verein (Pool +0x1D) bleibt – ein Computerverein bekommt also einen jungen Spieler an
derselben Stelle.

**Abgleich Remake** (`Core/Transfer.Squads.cs`): tägliche Chance 1/60 nur im letzten Vertragsjahr mit Frist, Ruhestand
33–37 (20 % … 100 %) als Teil der Ankündigung, 10 % Absagen (`RefuseChance`), Laufzeiten 2/3/4 mit 80/13/7 %, eigener
Ruhestand der Computervereine 33–37 – alles `[APPROX]` und anders als oben.

## 2. Angebote der Computervereine (Datei 0x07358, Datei 0x0736E; A136)

Schleife über die Plätze des Transfermarkts (Satz 4, DGROUP 0x7623 + 0x34 · Platz, Datei 0x0727C–0x073CE), nur für Spieler,
deren Pool +0x1A = Manager am Zug (selbst angeboten): Heilung, Ruhestand-Bit ausgeschlossen, w = Wert(Preis) / 10000;
random(0, 180) < w, random(0, 2) ≠ 0, keine offene Meldung → **+0x09 |= 0x80 am Marktplatz** (Datei 0x07358), bietender Verein
Datei 0x059B8 → **+0x16 am Marktplatz** (Datei 0x0736E), Meldung Art 1. Der Schalter DGROUP 0x4860 (dritter Wurf
random(0, 3)) ist immer 0. Der Spielerwert Datei 0x49D36 prüft +0x09 Bit 0x80 (Datei 0x4A0C8) und erst dann +0x16 (Verein
> 0x3F → · 1,6, sonst random(95, 100)). Kaderspieler bekommen nur Bit 0x40 mit Verein +0x16 (Datei 0x07A5C/0x07A8F); Bit 0x40
geht nicht in den Wert ein. **Folge:** Das Remake (`PlayerValueOf`: Angebot nur bei Spielern auf der Transferliste) entspricht
dem Original; nur der Kommentar ist zu korrigieren.

## 3. Automatische Aufstellung Datei 0x426BC / Datei 0x42838

### 3.1 Datei 0x426BC (380 Byte)

1. Einstellung `DGROUP 0x375 + 2 · DGROUP 0xA70E` (Byte; der Taktikbildschirm Datei 0x47F2B kippt sie 0 ↔ 1) = 0 → nichts tun.
2. Alle 29 Plätze des Satzes: +0x0A (eingesetzt) = 0.
3. Rolle r = 0 (Torhüter): 1 Block × 1 Spieler; r = 1 (Abwehr): 3 Blöcke × 2; r = 2 (Sturm): 3 Blöcke × 3. Je Spieler
   Datei 0x42838(r) → +0x0A = 1, +0x15 = Block + 1 (+ 0x80 bei Sturm). Findet sich keiner, endet der Block.
4. Ersatz: reihum Sturm, Abwehr, Torhüter je ein weiterer Spieler mit +0x15 = 0, bis 22 eingesetzt sind (Zähler startet bei 1,
   Grenze < 23) oder auch die Torhütersuche (mit Ausweichrollen) leer bleibt.

### 3.2 Datei 0x42838 (350 Byte): bester freier Spieler einer Rolle

- Kandidaten: Platz belegt (+0x0F ≠ 0), +0x0A = 0, +0x09 & 3 = 0 (nicht gesperrt/verletzt). Rolle = min(2, Rollenwert / 25)
  (Datei 0x04180).
- Wert = (Kondition + Technik + Form) / 3; bei Gleichstand gewinnt der spätere Platz (Vergleich `>`).
- Ausweichrollen, solange niemand gefunden ist: Torhüter → Abwehr → Sturm; Abwehr → Sturm → Torhüter; Sturm → Abwehr →
  Torhüter (Schritt −1, weil die Einstellung < 4 ist; der Zweig ≥ 4 ist tot). Als Feldspieler-Ersatz zählt der **erste**
  freie Torhüter nicht (Wert −1), jeder weitere mit Wert 1 – ein Ersatztorhüter bleibt zurück.
- Das Zeigerargument (Stapelvariable des Aufrufers, Startwert −1) wird nie geändert.

### 3.3 Aufrufer

Spielstart (Datei 0x28165), Tagesablauf (Datei 0x07548 nach Verletzung, 0x07621 Gesperrte, 0x07C07 täglich am Ende), Heilung
(Datei 0x07C12), Verletzung im Spiel (Datei 0x53C57), Taktik beim Einschalten (Datei 0x48E62), Transferbildschirm (Datei 0x465F1),
Managerzug (Datei 0x44E22), Nationalteam (Datei 0x3B4C6, Datei 0x54C9F). Die Aufstellung Datei 0x40364 hat **keine** eigene
Auswahl; ein Eingriff dort schaltet die Einstellung ab („Auto-… ist deaktiviert !“, DGROUP 0x124D, Datei 0x40ADE).

### 3.4 Nationalteam (A013)

Satz 5, Einstellung DGROUP 0x37F (= 0x375 + 2 · 5), Sicherung DGROUP 0x380. In der Tagesfunktion (Datei 0x54D84) wird die
Einstellung je Satz aus der Sicherung geholt und Datei 0x426BC gerufen, für Satz 5 nur bei DGROUP 0x9EDA < 0x3F. Im Zug des
Bundestrainers (Datei 0x555B7–0x555D6) wird sie vor dem Aufruf auf 0 gesetzt – dieser Aufruf ist wirkungslos; danach
Datei 0x0952C(5, 0). Die Regel ist damit dieselbe wie beim Verein (3.1/3.2); der Kader „auto“ (Datei 0x3CC01) ist davon getrennt.

**Abgleich Remake:** `Lineup.AutoFill` (nach Spielwert mit Energie, Rückennummern sortiert), `PlayerInjuries.RefillLineup`
(füllt nur Lücken, ohne Einstellung), `CupsNational.AutoFormation` (Kondition + Technik + Form, Blöcke wie Verein),
`StadiumStatistics.IsShortHanded` (prüft nach dem Spiel) – A165/A068/A013 abweichend, A169 (alle zurücksetzen) stimmt.

## 4. Liga-Spielplan Datei 0x4E7C7 (A113, A282)

Aufruf `(Ziel g, Spieltag d, Vereine n, Tabelle far**, Basis)`; Aufrufer Datei 0x05CA0, 0x30427, 0x36B5D, 0x44E22, 0x4E8A3,
0x50B45. Liga: d = DGROUP 0x7A9 + Liga (1-basiert), n = DGROUP 0x7B1 + Liga = 12, Basis = DGROUP 0x7C0 + Liga (0, 12, 24),
Tabelle über DGROUP 0xDD42 + 4 · Liga (alle drei Ligen → DGROUP 0x2DC, gesetzt Datei 0x43AE2).

```
tausch = 0; while (d >= n) { d −= n − 1; tausch ^= 1 }        // Runde 2, 4: Heimrecht getauscht
zeile = Tabelle[d]                                           // 12 Buchstaben 'a'…'l'
für k = 0 … n/2 − 1: h = zeile[2k] − 'a', a = zeile[2k+1] − 'a'; if (tausch) swap
    DGROUP 0x5D7B + 12 · g + 2k = h + Basis; DGROUP 0x5D7C + 12 · g + 2k = a + Basis
```

Tabelle DGROUP 0x2DC (Zeilen 1–11):

| d | Paare | d | Paare |
|---|---|---|---|
| 1 | ab cd ef gh ij kl | 7 | ja be kc gl hd if |
| 2 | da fc he jg li bk | 8 | ak fb cg ld ej hi |
| 3 | ah gb cj id ke fl | 9 | fa bc de lh gi jk |
| 4 | ea bi lc df kg hj | 10 | ag hb kd ic jf el |
| 5 | al jb ce gd fh ik | 11 | ia bd ch fk eg lj |
| 6 | ac lb dj ei fg kh | | |

Geprüft: jede der 66 Paarungen genau einmal, je Zeile jeder Verein genau einmal. Buchstabe = Vereinsindex − Basis (Index
innerhalb der Liga, ändert sich mit dem Vereinstausch beim Auf-/Abstieg). Heimspiele je Hinrunde ungleich (Verein d 4, f 7).
Gruppen: DGROUP 0x30C (6 Vereine, Zeilen „abcdef“, „dafcbe“, „afcebd“, „cafbed“, „eabcdf“) für die WM, DGROUP 0x324
(4 Vereine: „abcd“, „dabc“, „acbd“) in Datei 0x30427 (Aufruf Datei 0x3068B).

**Abgleich Remake:** `NewGame.DoubleRoundRobin` (Kreisverfahren) – andere Paarungen und Reihenfolge; `LeaguePairingsScreen`
zeigt den Plan des Remakes.

## 5. Preisindex (A183)

DGROUP 0x2B8 = 100, DGROUP 0x2BC = 100, DGROUP 0x2C0 = 110 (Startwerte in der EXE). Leser: Datei 0x3E9A8 (Monatsrechnung,
DGROUP 0x2BC), Datei 0x5A25E/0x5A57E/0x5A5B1 (Training Datei 0x59DBF, DGROUP 0x2B8), Datei 0x3FCE8 (DGROUP 0x2C0). Schreiber:
keiner im Spielcode (Querverweise und Bytesuche nach allen Befehlsformen); nur Speichern Datei 0x5BF78 und Laden Datei 0x5C95C
übertragen 4 Byte ab DGROUP 0x2BC. Es gibt keine Teuerung – der Preisindex bleibt 100.

## 6. Kaderplatz +0x19 – Unterzahl (unteres) und Überzahl (oberes Halbbyte) (A185, A186)

| Schreiber | Stelle | Wert |
|---|---|---|
| Datei 0x299F4 | Spielstart Datei 0x28165, Managerkader | (random(3, 12) << 4) \| random(3, 12) (oberes zuerst gezogen) |
| Datei 0x06F23 | Talent Datei 0x06D7F | ebenso |
| Datei 0x49803 | Listenwechsel Datei 0x494EF (Transfermarkt) | ebenso |
| Datei 0x5B239, Datei 0x5B262 | Startwerte Datei 0x5AF50 | ebenso (zwei getrennte Schreibvorgänge) |
| Datei 0x3CB2C | Kader des Nationalteams Datei 0x3CABF | 0x77 (7 \| 7) |
| Datei 0x06B01 | Training Datei 0x0659C | beide Halbbytes + Trainingsänderung, je auf 0 … 15 begrenzt (Datei 0x4BD35) |

Beleg Spielstände: alle Kaderplätze von FILE01, FILE09, FILE10, FILE12 (frischer Start, 120 Plätze) haben beide Halbbytes in
3 … 12; FILE14 (nach einer Saison Training) 0 … 12. **Abgleich Remake:** `HiddenSkill1/2 = 7` für alle (`Squad.Training.cs`).

## 7. Europapokal, Freundschaftsspiele, Penalty-Zeilen

### 7.1 Stärke bei der Europapokal-Auslosung (A004)

Datei 0x341AB vergleicht Datei 0x4D316(Verein) mit 70 (Datei 0x34407): Summe der Vereinsfelder 0x18–0x1D / 6 – also Kondition
und Technik von Abwehr und Sturm **und beide Formwerte** 0x1C/0x1D. Beim Europapokal-Gegner (Index > 64) mindert der Lader die
Form um 5 (`dateiformate.md`), das senkt die Stärke um etwa 1,7 und wirkt so auf die Annahme (< 70 nur zu einem Viertel).
**Abgleich Remake:** `CupsEurope` nimmt nur Datenbankbyte 0 (Abwehr).

### 7.2 Freundschaftsspiel (A008, A110)

Datei 0x30365(Heim, Gast), gerufen aus dem Nationalteam (Datei 0x3C05E): Paarung in Zeile 3 (DGROUP 0x5D5B/0x5D5C, Ende
DGROUP 0x5D5D = 0xFF), Wettbewerbscode DGROUP 0xD0C2 = 0xD (Datei 0x30390), Spieltagsablauf Datei 0x36B5D(0, Maske 8,
K.-o. 0, 0, Zeile 3), dann Datei 0x50283 und Rückgabe (Heimtore << 8) | Gasttore aus DGROUP 0x5CAB/0x5CAC.
Das WM-Gruppenspiel (Datei 0x30F53, Code 8) ruft Datei 0x36B5D mit denselben Argumenten. Unterschiede nur: (a) Minutenweg
bei Code 8 auch über den Anzeigeschalter DGROUP 0x229 (Datei 0x3702E), bei Code 0xD nur, wenn ein Manager seine Partie
verfolgt (DGROUP 0xA729 + Satz, Datei 0x36FEA), sonst schneller Weg Datei 0x3731F; (b) Datei 0x50283 trägt nur Codes 7/8 in
Tabelle und Serien ein (Datei 0x5030D) – beim Freundschaftsspiel nur die Begegnung (Datei 0x4F5A8). Kein K.-o., keine
Verlängerung, Unentschieden möglich. **Abgleich Remake:** `PlayFriendly` rechnet wie ein WM-Gruppenspiel (stimmt bis auf
(a)/(b)); `MatchEngine.Play` (Poisson) nutzt nur noch `KnockoutMatch.Play` (`Playoffs.cs`).

### 7.3 Penalty-Zeilen DGROUP 0x5C4B (A077)

Zeile r (r = 0 … 3, je 32 Byte; Vereine DGROUP 0x5CFB + 32 · r, Stände DGROUP 0x5C4B + 32 · r) läuft in Datei 0x32AA3(Maske,
Grenze, Schalter, Zeile) nur bei gesetztem Bit r; Start der Paarungen DGROUP 0xCFF8 + Zeile. Datei 0x36B5D ruft sie nur mit
K.-o.-Argument ≠ 0: nach 60 Minuten mit Schalter 0 (Unentschieden → Heim + 30, Verlängerung 61–70), danach mit Schalter 1
(noch gleich → Datei 0x3901B). Aufrufe von Datei 0x36B5D (Code, Maske, K.-o., Zeile):

| Aufruf | Wettbewerb | Code | Maske | K.-o. |
|---|---|---|---|---|
| Datei 0x35E91 | DEB-Pokal | 6 | Argument (Zeile 0) | 1 |
| Datei 0x31ACC | Play-offs (+ Aufstiegsrunde) | 1 | 4, bei DGROUP 0xCCA3 ∈ {0, 2} 0xC | 1 |
| Datei 0x3102B | WM-Endrunde | 0xA | 8 | 1 |
| Datei 0x30F65 | WM-Gruppe | 8 | 8 | 0 |
| Datei 0x314B1 | Europapokal | 3 bzw. 5 | 8 | 0 |
| Datei 0x303A2 | Freundschaftsspiel | 0xD | 8 | 0 |
| Datei 0x35ED5 | Liga | 7 | – | 0 |

In Datei 0x32AA3: Maske = 1 (DEB-Pokal) → kein Penalty, wenn beide Bytes der abgelegten Runde (DGROUP 0xD014 + 32 · DGROUP
0xD074 + Paarung) > 99 sind; sonst nur Zeilen 2 und 3: Zeile 2 nicht, wenn beide Serienbytes (DGROUP 0xCCB2 + 14 · DGROUP
0xCCA3 + Paarung) > 99 sind; Zeile 3 bei Maske 0xC nie (Aufstiegsrunde läuft ohne K.-o.). Zeilen 0/1 bei anderer Maske nie.
**Folge:** Penaltyschießen gibt es nur im DEB-Pokal, in Play-off-Spielen einer offenen Serie und in der WM-Endrunde – nicht in
Europapokal, WM-Gruppe, Aufstiegsrunde und Freundschaftsspiel. **Abgleich Remake:** `MatchEngine.Live` schießt in jedem
K.-o.-Spiel des Minutenwegs.

## 8. Vereinsplatz beim Spielstart (Datei 0x29C3B ff.; A112, R-K7)

Je neuem Manager m in Datei 0x28165 (L = Start-Liga DGROUP 0x11A, 0 = Bundesliga; v = Verein des Managers):

1. Behalten (Datei 0x29C3B–0x29C75), wenn L ≠ 0, Liga(v) / 2 = L (Datei 0x34FD5) und v ≤ DGROUP 0x7CB (35).
2. Sonst (Datei 0x29C78–0x29CF7): r = random(Basis L, Basis L + 11); verworfen, wenn r Verein eines früheren Managers ist oder
   Datei 0x45CD0(r, 1) einen Eintrag in Zeile 1 (Europapokal) findet; dann Tausch Datei 0x36169(v, r, 0x80). Der Managerverein
   nimmt den Platz r in der Start-Liga ein, r rückt auf den alten Platz von v (auch ohne Liga). Bei L = 0 wird also immer
   getauscht, notfalls innerhalb der Bundesliga.
3. Sonderfall (Datei 0x29CFA–0x29DA1): neues Spiel (DGROUP 0x386 vorher 0), Spielende DGROUP 0x5A14 = 1994 (eine Saison) und
   ein Manager → DGROUP 0x9744 + 0x1AF · m = 1, r = random(0, 11) ohne Managerverein, **mit** Eintrag in Zeile 1 → Tausch: der
   Manager übernimmt einen Europapokal-Platz der Bundesliga.

Datei 0x45CD0(Verein, Maske): 1, wenn der Verein in Zeile k = 1 … 3 mit Bit k − 1 steht (2 · DGROUP (0x3B1 + DGROUP 0x5CF4)
Einträge). **Abgleich Remake:** `NewGame.AssignClubs` – ligaloser Verein rückt in die Oberliga und verdrängt einen
Zufallsverein; fester Verein bleibt sonst in seiner Liga; kein Europapokal-Filter, kein Sonderfall.

## 9. DOSBox-Spielstände (A085, A088, A091, A125)

**Was die vorhandenen Stände zeigen** (`emtool hok dump <datei> 5DC3 far:8253`):

- FILE13 → FILE14 (Saisonwechsel): FILE13 ist aus FILE12 gepatcht (`hok patch`), alle Zähler sind schon 0 – **kein Beleg für
  A088**. Die Kaderplätze behalten ihre Reihenfolge (Poolindex je Platz gleich), Alter + 1, Laufzeit 2 → 1.
- FILE05 → FILE07 (Kauf DAFFNER): der neue Spieler steht auf dem **ersten freien Platz** (25), die übrigen unverändert (A091).
- Startreihenfolge (FILE01, FILE05, FILE12): Torhüter, Verteidiger, Stürmer, je Art in Poolindex-Reihenfolge – wie die
  Kaderauswahl Datei 0x2993A (A125).
- Verkauf: im Code rückt Datei 0x425B6 die folgenden Plätze auf (`transfer.md`, Teil 5); kein Spielstandpaar.
- Nationalteam (A085): FILE11 hat noch keinen Kader in Satz 5 – offen.

**Ablauf für den Nutzer** (DOSBox, jeweils auf einen neuen Platz sichern, Bild des Kaders dazu):

1. A088: Spiel mit einem Manager bis kurz vor dem 29. April spielen (nach den Play-offs, mindestens 20 Spiele), sichern
   (Stand A); am 29. April bzw. 10. September nach dem Saisonwechsel erneut sichern (Stand B). Auswertung: Kaderplatz
   +0x00–+0x05, +0x1C/+0x1E/+0x20, +0x22 und Pool +0x1B/+0x1F je Poolindex A gegen B.
2. A091: aus Stand A einen Spieler aus der Mitte des Kaders verkaufen, sichern (Stand C); Auswertung: Platzreihenfolge A/C.
3. A085: als Bundestrainer den Kader „auto“ wählen und einen Spieler von Hand hinzufügen, sichern (Stand D); Auswertung:
   Satz 5 (DGROUP 0x5DC3 + 5 · 0x618) gegen `NationalCoach`.

Fehlt ein Paar, bleibt der Punkt begründet offen; die Leitstelle legt die Stände im Referenz-Repo ab.

## Umsetzung

Je Stelle ein Eintrag (Ziel, Dateien, Testidee, Spur dieser Runde). „neu“ = nicht in den Teilen von `arbeitspakete.md`;
Zuordnung als Teil durch die Leitstelle nach den Dateigrenzen.

| # | Stelle | Ziel | Dateien | Testidee | Spur |
|---|---|---|---|---|---|
| U1 | Abschnitt 1, Ruhestand (A231, A232, A238) | Tageswurf random(350, 450) < Alter · c / 10 im Managerkader setzt `Retiring` (Text Art 4) unabhängig von der Laufzeit; Rücktritt bei Vertragsende | `Core/Transfer.Squads.cs` | 35-jähriger Feldspieler nie, 40-jähriger in ≤ 30 Tagen; Torhüter 41 nie; Technik − Kondition = 20 verschiebt die Grenze | `kader-transfer` Teil 7 |
| U2 | Abschnitt 1, Verlängerungsangebot (A229, A239) | Chance 1/(4 · (p + 1)) mit p aus s, Alter, Spielen; n = 2/3/4 mit 71/20/10 von 101; keine Frist | `Core/Transfer.Squads.cs` | p für s = 50, Alter 25, 0 Spiele = 70; Verteilung von n über 10.000 Würfe; Leihspieler nie | `kader-transfer` Teil 7 |
| U3 | Abschnitt 1, Absagen (A230) | `RefuseChance` entfällt; „nicht mehr verlängern“ = Ruhestandsmeldung | `Core/Transfer.Squads.cs` | keine Absage ohne Ruhestand-Bit | `kader-transfer` Teil 7 |
| U4 | Abschnitt 1, Verfall | Angebot +0x18 > 99 verfällt mit 1/6 je Tag bei offener Meldung → Sperre random(9, 17); Bits 0x20–0x80 mit 1/4 | `Core/Transfer.Squads.cs`, `Core/Transfer.Unsolicited.cs` | Sperre in 9 … 17; Meldung gelöscht | `kader-transfer` Teil 7 |
| U5 | Abschnitt 1, Saisonende | Laufbahnende ohne Managerkader bei Alter > random(37, 42), Neuanlage im Pool (18–25, s 40–92) | `Core/Transfer.Squads.cs` (Saisonende-Anschluss nennt `saisonwechsel`) | 4 Saisonwechsel: kein Computerspieler älter als 42 | `kader-transfer` (neu) |
| U6 | Abschnitt 2 (A136) | Kommentar `[APPROX]` → `[ORIGINAL]` (Bit 0x80 nur Marktplatz) | `Core/Player.Value.cs` | bestehender Test bleibt; Kaderspieler mit Bit-0x40-Angebot ohne Wertänderung | `kader-transfer` Teil 7 |
| U7 | Abschnitt 3 (A165, A169, A068, A190) | Einstellung „automatisch“ je Manager; Neuaufstellung nach 3.1/3.2 statt Lückenfüllen; Gesperrte täglich heraus | `Core/Squad.Lineup.cs`, `Core/MatchEngine.Injuries.cs`, Schema-Migration | Gleichstand → späterer Platz; Torhüter-Ausweichregel; ohne Einstellung bleibt die Aufstellung | `wettbewerbe-spiel` (neu) |
| U8 | Abschnitt 3.4 (A013) | Auto-Formation des Nationalteams mit derselben Regel (Satz 5) | `Core/CupsNational.Lineup.cs` | gleiche Auswahl wie U7 bei gleichem Kader | `wettbewerbe-spiel` (neu) |
| U9 | Abschnitt 4 (A113, A282) | Spielplan aus der festen Tabelle, Runden 2/4 getauscht | `Core/NewGame.cs`; Anzeige `UI/Screens/Competitions/LeaguePairingsScreen.cs` | Spieltag 1 = ab cd ef gh ij kl; Spieltag 12 = ba dc …; 66 Paarungen je Hinrunde | `kader-transfer` (neu, `NewGame.cs`); Anzeige `wettbewerbe-spiel` |
| U10 | Abschnitt 5 (A183) | Preisindex 100 als `[ORIGINAL]` | `Core/Squad.Training.cs` (Kommentar) | – | `wettbewerbe-spiel` Teil 7 |
| U11 | Abschnitt 6 (A185, A186) | Startwerte random(3, 12) je Halbbyte (Start, Talent, Markt, Pool), Nationalteam 7/7 | `Core/Squad.Training.cs`, `Core/Player.Factory.cs` (benennen) | frischer Start: alle Werte 3 … 12; Mehrsaisonprobe | `wettbewerbe-spiel` Teil 7 |
| U12 | Abschnitt 7.1 (A004) | Europapokal-Stärke = Mittel der sechs Teamwerte inkl. Form − 5 | `Core/CupsEurope.cs` | Gegner mit Byte 0/1 = 72/70: Stärke < 70 nach Formabzug | `wettbewerbe-spiel` (neu) |
| U13 | Abschnitt 7.2 (A008, A110) | Freundschaftsspiel ohne Tabellen-/Serieneintrag; `[ORIGINAL]` Code 0xD; Kommentar in `MatchEngine.cs` | `Core/CupsNational.GameState.cs`, `Core/MatchEngine.cs` | Unentschieden möglich, keine Verlängerung | `wettbewerbe-spiel` (neu) |
| U14 | Abschnitt 7.3 (A077) | Penaltyschießen nur DEB-Pokal, Play-off-Serie offen, WM-Endrunde | `Core/MatchEngine.Live.cs`, `Core/MatchEngine.Shootout.cs` | Europapokal-Unentschieden bleibt; Aufstiegsrunde ohne Penalty | `wettbewerbe-spiel` (neu) |
| U15 | Abschnitt 8 (A112, R-K7) | Vereinsplatz nach 8.1–8.3; Europapokal-Filter; Sonderfall eine Saison | `Core/NewGame.cs` | fester Zweitligist bei Start-Liga 2. Liga bleibt; bei Bundesliga Tausch; ligaloser Verein in Start-Liga | `kader-transfer` (neu) |
| U16 | Abschnitt 9 | Spielstandpaar nach Ablauf aufnehmen und auswerten | – (Nutzer, Leitstelle) | A088/A091/A085 an den Ständen | Nutzer |

## Offen

- A088 (Zähler zum Saisonwechsel) und A085 (Kaderplätze des Bundestrainers): Spielstandpaar fehlt (Abschnitt 9).
- Bedeutung von DGROUP 0x9744 + 0x1AF · m (Sonderfall 8.3) und die Folgen der Vergleiche mit 1994 in Datei 0x29FB3/0x2A051
  nicht gelesen.
- Datei 0x0952C (Nationalteam nach der Aufstellung) nur als Aufruf benannt.
