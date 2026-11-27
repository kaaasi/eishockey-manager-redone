# Review: Analyse `re-nachtrag` (#424)

Reviewer: Claude Code (Claude Opus 5.5), geplanter Lauf, frische Sitzung ohne Beteiligung am PR.
Geprüft: #424 Base `7332a0fd04628e1a63acde30d9131ecb2f2d2afb` · Head `e962958fc2ac267ad7260a7a1a6cc716d6ac6cb1`.
Art der Prüfung: **Ausführung an nachgewiesenem Checkout** (`git rev-parse HEAD` = Head, `git status --porcelain` leer),
Originaldateien aus `kaaasi/eishockey-manager-referenz` (`original/gamedata/em.exe`, `original/spielstaende/`).

## 1. Merge-Empfehlung

**Ja** für genau `e962958`. Nur Doku (`docs/analyse/re-nachtrag.md`, eigener Statuspunkt in `docs/arbeitspakete.md`).
Die Abnahme der Spur ist erfüllt: jede Funktion der Spur nennt `re-nachtrag.md` als Fundstelle, alle Stichproben stimmen
mit dem Disassembly überein. Die Abschnitte „Umsetzung“ für `kader-transfer` Teil 7 und `wettbewerbe-spiel` Teil 7 sind
umsetzbar; drei Präzisierungen (P3) sollten die Spuren beim Start beachten, sie blockieren den Merge nicht.

## 2. Befunde

| ID | Prio | Befund |
|---|---|---|
| RN-1 | P3 | U11: Nationalteam 0x77 und Platz- statt Spielerbindung nicht auf das Remake-Modell abgebildet: [Z. 322](https://github.com/kaaasi/eishockey-manager-redone/blob/e962958fc2ac267ad7260a7a1a6cc716d6ac6cb1/docs/analyse/re-nachtrag.md#L322) |
| RN-2 | P3 | U2: Bedingung „Restlaufzeit +0x0B = 1“ fehlt in der Zeile, „keine Frist“ missverständlich: [Z. 101](https://github.com/kaaasi/eishockey-manager-redone/blob/e962958fc2ac267ad7260a7a1a6cc716d6ac6cb1/docs/analyse/re-nachtrag.md#L101), [Z. 313](https://github.com/kaaasi/eishockey-manager-redone/blob/e962958fc2ac267ad7260a7a1a6cc716d6ac6cb1/docs/analyse/re-nachtrag.md#L313) |
| RN-3 | P3 | Schreibertabelle +0x19: Blockkopie beim Kauf fehlt, 0x5B239 als „Pool-Neuanlage“ unscharf: [Z. 28](https://github.com/kaaasi/eishockey-manager-redone/blob/e962958fc2ac267ad7260a7a1a6cc716d6ac6cb1/docs/analyse/re-nachtrag.md#L28), [Z. 213](https://github.com/kaaasi/eishockey-manager-redone/blob/e962958fc2ac267ad7260a7a1a6cc716d6ac6cb1/docs/analyse/re-nachtrag.md#L213) |

**RN-1 (P3).** Im Original liegt +0x19 im **Kaderplatz**, im Remake als `HiddenSkill1/2` am **Spieler**
(`Squad.Training.cs` Z. 370/373). Das Nationalteam (Satz 5) bekommt 0x77 (Datei 0x3CB2C, nachvollzogen); das Remake
spielt Länderspiele aber mit denselben `Player`-Objekten (`MatchEngine.Managed.cs` Z. 159/160 summiert
`p.Player.HiddenSkill1/2`, Zweig `nationalCoach` Z. 165). Eine Umsetzung nur in `Squad.Training.cs`/`Player.Factory.cs`
lässt Länderspiele mit den Vereinswerten laufen. Vorschlag für U11: `MatchEngine.Managed.cs` ergänzen (im Länderspiel 7/7)
und festhalten, dass das Würfeln beim Eintritt in Managerkader bzw. Markt geschieht (Computerspieler haben im Original
keinen Wert; ein Spieler, der den Managerkader verlässt und über den Markt zurückkommt, würfelt neu) – sonst `[APPROX]`.

**RN-2 (P3).** Der Pseudocode in Abschnitt 1 nennt `+0x0B == 1` (Datei 0x07865 `cmp byte [es:bx+0xb],0x1`,
nachvollzogen), die Umsetzungszeile U2 nicht, und „Es gibt keine Frist“ neben dem Abgleich „Remake: nur im letzten
Vertragsjahr mit Frist“ lädt dazu ein, die Restlaufzeit-Bedingung mit der Frist zu streichen. Gemeint ist: Angebot nur bei
Restlaufzeit 1, aber ohne Stichtag. In U2 die Bedingungen ausschreiben (+0x0B = 1, keine Leihe, keine offene Meldung,
kein Bit 0x80, nur bis Tag 233); Testidee „Restlaufzeit 2 nie“ ergänzen.

**RN-3 (P3).** (a) Beim Kauf vom Markt kopiert Datei 0x478DB–0x47907 (`0x48:0x2BD4`, 0x34 Byte von DGROUP 0x7623 + 0x34 ·
Platz in den Managerplatz) den ganzen Kaderplatz – +0x19 wandert mit. Die Tabelle nennt nur Byte-Schreiber; für die
Umsetzung ist die Kopie relevant (Kauf behält die Werte des Marktplatzes). (b) Datei 0x5B239/0x5B262 liegt hinter dem
Pool-Startteil 0x5AF50–0x5B0B1 (`transfer.md` Z. 174: dort Pool +0x19 = 0, das sind Länderspiele) in einem Zweig, der
Pool +0x1A = Manager am Zug und Alter random(19, 31) setzt und in den **Managerkader** schreibt. „Pool-Neuanlage“
(Ergebnis Nr. 6, U11) ist dafür irreführend; besser den Zweig benennen oder „Managerkader-Neuanlage in 0x5AF50“.

## 3. Ausgeführte Prüfungen

**Gegenbeispiel Abnahme (`funktionskarte.py --csv`, ohne `--write`)** an Head und Base:

- `python3 tools/reverse-engineering/funktionskarte.py em.exe --csv head.csv` (ebenso an `7332a0f`): 680 Funktionen,
  **0 Statuswechsel** Base → Head (nur Doku, `docs/re-abdeckung.md` unverändert). 58 Funktionen nennen am Head
  `re-nachtrag.md` (51 umgesetzt, 7 dokumentiert).
- Alle Adressen der Spur (Inhalt Runde 11 und Zuordnungstabelle der Analyse) auf ihre Funktion abgebildet: 0x07169
  (inkl. 0x07358/0x0736E), 0x426BC, 0x42838, 0x4E7C7, 0x3E811, 0x59DBF, 0x0659C, 0x06D7F, 0x28165 (0x299F4, 0x29C3B,
  0x2993A), 0x3CABF, 0x494EF, 0x5AF50, 0x341AB, 0x4D316, 0x30365, 0x3B8F7 (0x3C05E), 0x54C9F (0x555D6), 0x32AA3, 0x45CD0,
  0x36B5D, 0x0952C, 0x425B6, 0x4B2BA, 0x4B270, 0x36169, 0x34FD5, 0x50283, 0x26F20, 0x3901B, 0x49D36, 0x059B8 –
  **alle** mit Fundstelle `re-nachtrag.md`. Begründet offen: A085/A088 (Abschnitt 9, „Offen“), 0x9744 + 0x1AF · m,
  0x0952C nur als Aufruf.

**Stichproben `ndisasm -b16 -e <off> -o <off> em.exe`** (je drei):

| Stelle | Geprüft | Ergebnis |
|---|---|---|
| Tagesablauf 0x07169 | Einstieg 0x07172 `cmp al,[0x386]` mit DGROUP 0xA70E; Marktzeiger 0x07292–0x0729D = 0x7623 + 0x34 · Platz (Satz 4); 0x07358 `or byte [es:bx+0x9],0x80` und 0x0736E `mov [es:bx+0x16],al` am Marktplatz | stimmt (A136) |
| Tagesablauf Ruhestand/Verlängerung | 0x0754D–0x075E7: c = 100 − 15 · (Pool +0x18 = 0), − (Technik − Kondition)/2 bei > 0, v = Alter · c / 10, random(350, 450) < v, Bit 0x80, +0x18 < 100, Meldung 4; 0x076FA–0x078A7: s, a mit min(8, \|25 − Alter\|), e = Summe / 36 ≤ 5, p = (60s + 40a)/100 − p·e/10, random(0, p) = 0, random(0, 3) = 0, Pool +0x1E = 0, kein 0x80, +0x0B = 1, n aus random(0, 100) > 90/70 | stimmt; Rechenbeispiele U1 (35/36/40 Jahre, Torhüter 41/42, T − K = 20 → Grenze 39) und U2 (p = 70) nachgerechnet |
| Aufstellung 0x426BC | Einstellung `[0x375 + 2 · [0xA70E]]` − 1 = −1 → Ende; 29 Plätze +0x0A = 0; Blöcke 3 − 2 · (r = 0), Spieler r + 1; +0x15 = Block + 1 + 0x80 · (r = 2); Ersatz ab Rolle 2, Zähler ab 1, Grenze < 0x17; kein Spieler gefunden → Sprung 0x4272E → 0x42787 (nächster Block); Torhüter leer → Ende | stimmt |
| Startwert +0x19 | 0x299B7–0x299F4: (random(3, 12) << 4) \| random(3, 12) → DGROUP 0x5DDC (= 0x5DC3 + 0x19); 0x5B210–0x5B262 ebenso in zwei Schreibvorgängen; 0x3CB2C `mov byte [bx+0x5ddc],0x77` | stimmt (RN-3 b zur Benennung) |

**Spielstände** (`emtool hok dump <FILE> 5DC3`, Halbbytes von +0x19 aller belegten Plätze, Satz 0–4): FILE01, FILE09,
FILE10, FILE12 je 3 … 12 (FILE10 oben 3 … 11); FILE14 0 … 12 – wie Abschnitt 6.

**Erwartet offen bestätigt:** A085 (FILE11 ohne Satz 5) und A088 (FILE13 gepatcht, Zähler 0) begründet offen; A091 teilweise
(Kauf FILE05 → FILE07, Verkauf nur Code), A125 aus den Startständen belegt. Ablauf für das Spielstandpaar in Abschnitt 9
ist ausführbar.

**Umsetzbarkeit Teil 7:** `kader-transfer` – A136 (U6), A229/A239 (U2), A230 (U3), A231/A232/A238 (U1) haben Regel, Daten
(Adressen, Felder, Meldungsarten) und Testidee; Ergänzung RN-2. `wettbewerbe-spiel` – A185/A186 (U11) Regel und Daten
vollständig, Dateiliste um RN-1 ergänzen.

**`tools/pruefen.sh`** (alle drei Läufe, SDK 10.0.402, Commit `e962958`):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1631 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1351 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1351 | 280 | 0 |

Ergebnis: bestanden (kein Code geändert; Lauf belegt nur, dass der Stand baut).

**Akzeptierte `[APPROX]`:** keine (PR ohne Code). **Annahmen:** `0x48:0x2BD4` als Blockkopie (Quelle Marktplatz, Ziel
Managerplatz) aus der Argumentfolge gelesen, Bibliotheksfunktion nicht disassembliert; Bedeutung des Zweigs ab 0x5B0B1
nicht weiter verfolgt.
