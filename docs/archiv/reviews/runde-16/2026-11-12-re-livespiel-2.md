# Review PR #793 – Analyse-Nachtrag Live-Spiel (Spur `re-livespiel-2`, L6/L7)

Geprüft: Head `fac7ef1d0578da8b10c5e085852383298712faa4` gegen Base `e1476e65e721a2266752949d798e2d1539194e61` (= `main`).
Reviewer: Claude Opus 5.5, frische Sitzung (geplanter Lauf), nicht an der Spur beteiligt. Checkout nachgewiesen
(`git rev-parse HEAD` = Head, `git status --porcelain` leer), em.exe und `tore\` aus dem Referenz-Repo (`original/`).

## 1. Merge-Empfehlung

**Nein für diesen Head** – allein wegen P2-1 (`zusammenarbeit.md` §6 Nr. 5). Alle Stichproben der Lesarten §10–§11 stimmen
Byte für Byte mit em.exe überein, das Format `tore\` hält über alle 48 Dateien, die Funktionskarte führt 0x575C3 als
„dokumentiert“, der Plan L1–L8 benennt vorhandene Bausteine und prüfbare Abnahmen. Falsch ist nur die Angabe zum
Koordinatenbereich der Torszenen (Grundlage für Spur C). Fix: eine Zeile; danach genügt die Delta-Prüfung der Leitstelle.

## 2. Befunde

**P2-1 Koordinatenbereich `tore\` falsch („bis rund 540 × 320“)** –
[`re-livespiel.md` Z. 562](https://github.com/kaaasi/eishockey-manager-redone/blob/fac7ef1d0578da8b10c5e085852383298712faa4/docs/analyse/re-livespiel.md#L562).
Die Kopfwerte (Big-Endian, Lesart des PR) reichen weit darüber; das Eisfeld der Szenen ist hoch, nicht breit.
Beleg (Skript im Scratch, liest Kopf +0x04/+0x06, +0x08/+0x0A und Objekte +0x0C + 6k mit Figur ≠ 0 aus allen 48 Dateien):
`python3 -I tore2.py ~/referenz/original/gamedata/tore` →
`cam [304, 880] puck [639, 1051] aktiv [791, 1205]`; Beispiele `6.at`: Kamera (304\|768), Puck (639\|1024), Objekt-y bis
1205; `8.at`: Kamera-y 880; `.h*`-Szenen kleiner (y bis 905, `9.ht`), `.a*`-Szenen liegen im unteren Teil. Startwerte allein
widerlegen „540 × 320“; die Bewegungen kommen noch hinzu. Vorschlag: „Koordinaten bis etwa 800 × 1 210 (Startwerte;
Gastszenen unten, Heimszenen oben) – Größe der Eisfläche und Zuordnung zur Halle `kb\241…244` in Spur C prüfen“.

**P3-1 `lrandom` (`38h:7E5h`) hat einen eigenen Generator – die offene Frage aus §12 ist beantwortbar** –
[`re-livespiel.md` Z. 576](https://github.com/kaaasi/eishockey-manager-redone/blob/fac7ef1d0578da8b10c5e085852383298712faa4/docs/analyse/re-livespiel.md#L576).
`ndisasm -b16 -e 0xBFD5 -o 0xBFD5 em.exe | head -32` (Datei 0xBFD5 = Basis 0x99F + 0x7E5): Zustand `DGROUP 0xE017/0xE019`
(Langwort), `x = ((x << 4) + 0xA7)` mit `0:48Bh` (aus der Benutzung: Langwort-Rest) gegen 0x3D0900 (4 000 000), danach Skalierung auf `(b − a + 1)`.
`random` (`38h:847h` = Datei 0xC037) ruft dagegen `0:6DFh` = Datei 0x24DF (Borland-`rand`, Zustand `DGROUP 0x5984/0x5986`,
Faktor 0x015A4E35). Die Zwinker-Züge verschieben die Spielzufallsfolge also nicht; nur Strafgrund und Szenenwahl tun es.
Hinweis für A4/A2 (eigener Generator im Kern bzw. in `LiveClock`), kein Fehler der Analyse.

**P3-2 `random(2, 2)` verbraucht trotzdem einen Zug** – §11 Schritt 2 / Plan A4. 0xC037 hat keinen Sonderfall `a = b`
(`ndisasm -b16 -e 0xC037 -o 0xC037 em.exe | head -20`: Grenzentausch, dann immer `call 0x0:0x6df`). Mit `MAXSZENE:11|2|2`
ergibt die Penalty- bzw. Sechser-Wahl stets n = 2, zieht aber jedes Mal; die A4-Abnahme „Züge in genau dieser Reihenfolge“
sollte das ausdrücklich prüfen (bei Penalty/sechs **drei** Züge, sonst zwei).

**P3-3 Kleinigkeit §10 Neuaufbau:** 0x5434C ruft vor `118h:43h` bereits `110h:7Ah` (Hintergrund) und `38h:8A7h` (Datei
0x5435E/0x54363); die Aufzählung „Danach (0x5438D–…)“ beginnt erst dahinter. Für B3 ergänzen, keine falsche Lesart.

Erwartet offen – bestätigt (unverändert offen, korrekt als `[APPROX]` geführt): Aufstellung im Spiel Datei 0x41298, 0x4135E,
0x413E8 (`cmp byte [bp+0x6]` bzw. `mov al,[bp+0x6]` vorhanden, Folgecode nicht gedeutet); Taktik 0x486F1; Rücksetzen
`DGROUP 0x2D6` (Bytesuche `C6 87 D6 02` nur 0x54821, `8A 87 D6 02` nur 0x54376, keine `88 87`/`FE 87`/`FE 8F`/`80 BF`-Treffer;
übrige `D6 02`-Funde sind `cs:`-Zugriffe bzw. Daten); Deutung „Kamera“ (0x12D06 schreibt Byte 0 nach `cs:0x1A/0x1C`, ersetzt
statt addiert – Lesart passt, Benennung bleibt Deutung); Bildtafel der Figurnummern (Probe: 86 verschiedene Werte 0…93).

## 3. Ausgeführte Prüfungen

| Prüfung | Befehl / Probe | Ergebnis |
|---|---|---|
| Zugriff Referenz-Repo | `git ls-remote …/eishockey-manager-referenz HEAD` | `a529b60…` |
| §10 Lampe, Reihen, Felder | `ndisasm -b16 -e 0x38D96 -o 0x38D96 em.exe` bis 0x3901B; Bytesuche im Bereich | alle genannten Bytes an den genannten Stellen (`83 FE 02` 0x38DBB … `26 88 47 2E` 0x38FAA); `si` = `[es:bx+2]` gegen 2/0x15 (x), `[es:bx]` gegen 0xE1/0xEF (y); Aufrufe 0x5434C(Mgr, Pokal, 0, 1/2), 4. Reihe über `0x28:0xD9F(Mgr, t)` |
| §10 Aufrufer | Suche `0E E8` mit Ziel 0x38D96 in 0x37A62… | 0x37EB5, 0x382B3 (genau zwei) |
| §10 Spieltagssicherung | `ndisasm` 0x36B65, 0x37833, 0x37D28–0x37D5B | wie beschrieben (Sicherung, Rückschreiben, neue Sicherung, Lampe = 0) |
| §10 Treffer-Tests | `ndisasm` 0x534BC, 0x53508 | Grenzen einschließend, `+6` bzw. `+5`; `(mx − x)·n / breite + 1` |
| §10 0x5434C/0x48E28 | `ndisasm` beider Funktionen | Argumente und Schleife wie beschrieben; `[bp+0xA]` ungelesen |
| §10 Aufstellung | `ndisasm` 0x40364… `grep '\[bp+0x6\]'` | genau acht Stellen; 0x40CFD, 0x40E2A, 0x40F35, 0x4115F gelesen wie beschrieben |
| §11 0x57480 | `ndisasm` 0x57574–0x575C2 | Zähler 0 → 0xE2F4, 1 → 0xE2F2, Rest → 0xE2F6; Prüfung < 1 nur 0xE2F6/0xE2F2 |
| §11 0x575C3 | `ndisasm` vollständig | Argumente, Zugreihenfolge, 255/210, Suffixe, 50 Fehlversuche, Rückgabe wie beschrieben |
| §11 Texte | DGROUP 0x16DD0 + 0x554A … 0x5568 | `tore\anzahl`, `tore\`, `.h`, `.a`, `v`, `t`, `e`, `j` |
| §11 Aufrufer | `ndisasm` 0x56B4D–0x56BCB | `0x138:0x25(Art, Penalty, Seite, sechs)`, nur −1 ausgewertet |
| §11 Format | Skript über 48 Dateien | Länge = 0x54 + 28·n + 2, Ende 0xAAAA, 6 600 Bilder, Byte 2 = 0, Objekt-Byte 0 = 0, Effekte 3:149 4:53 1:25, Kopf „TOR!“ |
| §11 Bewegungsbyte | `ndisasm` 0x12B9D (`48h:51ADh`), 0x12D06 (`48h:5316h`) | Bits 5–3/2–0, Vorzeichen Bit 6/7; Byte 0 Kamera (ersetzt), Byte 1 Puck (addiert), Objekte nur bei Figur oder Bewegung ≠ 0 |
| Funktionskarte | `funktionskarte.py em.exe --csv` an Base (`--repo` Worktree) und Head, Vergleich der Spalte `status` | einzige Änderung `0x575C3 unbekannt → dokumentiert`; Spielcode „unbekannt“ 0 % |
| Plan L1–L8 | Codesuche `LiveTeam`, `LiveSquad.HasFourthLine`, `RecountSkaters`, `ChangeLines`, `MatchEngine.Live.cs`, `MatchDayScreen.cs`, `GameOptionsScreen.cs`, `SeasonEndGameOverScreen.cs`, `SpriteSheet.cs` | alle vorhanden; neue Dateien (`LiveClock.cs`, `GoalScene.cs`, `LiveMatchScreen.cs` …) existieren noch nicht – keine Kollision; Abhängigkeiten schlüssig |
| Diff-Umfang | `git diff --stat` | nur `re-livespiel.md`, `reverse-engineering.md` (eigener Abschnitt), Statuszeile `arbeitspakete.md` – Dateigrenzen eingehalten |

`tools/pruefen.sh --schnell` (reine Doku, laut Auftrag genügt `--schnell`), SDK 10.0.402, Commit `fac7ef1`:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2961 | 0 | 0 |

**Akzeptierte `[APPROX]`:** Farbwert 7 statt 0x1E bei 0x47F5E; „≠ 0 = im Spiel“ für `DGROUP 0x2D6`; die drei ungelesenen
Stellen der Aufstellung (Vorgabe „in B3 zuerst lesen“ steht im Plan); Kamera-Deutung; Figurtafel offen.
**Annahmen (nicht geprüft):** Deutung „sechs = ohne Torhüter“ aus Feld 0x32 + Strafbank = 6 (Code bestätigt, Bedeutung
nicht per Spielstand); Toneffekt-Zuordnung 1/3/4 aus `dateiformate.md` übernommen.
