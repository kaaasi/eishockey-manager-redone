# Belastung (#730 Rest) – Prüfauftrag Spielstärke und Reihenwahl

Spur `abgleich-rest`, Teil 1b (Prüfauftrag, R16-E1 = B, 11.11.2026), Base `main` d6fa442. Ausgang: `belastung.md` §4 auf
`paket/abgleich-rest` (Kandidaten 1–3). **Ergebnis: Keiner der drei Kandidaten erklärt die Abweichung.** Spielstärke des
Managervereins und der Computervereine, Reihenwahl (Modus 1–3) und Startzustand stimmen mit em.exe überein; Zwangsversuche
mit jedem Modus und mit festem Einsatz erzeugen das Muster des Originals nicht. Die Ergebnisse der Paare liegen im Bereich des
Remakes. Neu gefunden und als #772 (P2) gemeldet: Einsatz und Angriff der Spielvorbereitung 0x8365 (wirkt auf Computerspiele,
nicht auf #730). **Teil 2 startbar: nein** (Abschnitt 6).

**Nachtrag Fortsetzung 2 (R16-E2 = B):** #772 behoben (§7.1, Belastungsmuster unverändert); Teil 1c (§7.2) findet keinen weiteren
Schreiber der Reihenwahl im Spiel, die Minuten-Vorgabe ist in FILE02/FILE16A nicht aktiv. **Ursache #730 weiter nicht belegt,
Teil 2 blockiert** (Fallback R16-E2: C).

## 0. Vorbereitung (alle Befehle aus dem Repo-Wurzelordner)

```bash
source ~/claude-tools/env.sh && dotnet build src/EishockeyManager.Tools -c Release
export OUT=/tmp/belastung-staerke                         # Ausgaben, nie ins Repo
W=docs/analyse/belastung-staerke                          # Skripte, README dort
$W/dump.sh FILE02 FILE03 FILE16A FILE17 FILE24 FILE25 FILE38 FILE39 FILE40
git apply $W/trace.patch && dotnet build src/EishockeyManager.Tools -c Release   # nur für §2.4/§3, danach git checkout
```

`trace.patch` schreibt je Minute der Managerpaarung Stärken, Modi, Reihen, Energien, Einsatz (`EM_TRACE=<datei>`) und erlaubt
`EM_FORCE_MODE=1…3` bzw. `EM_FORCE_EFFORT=<n>` für die Managervereine. Es ist keine Produktänderung und wird nicht committet.

## 1. Spielstärke (Kandidat 2 und Stärke der Computervereine in Kandidat 1)

### 1.1 Managerverein – 0x977F/0x98F8 gegen `MatchEngine.ManagedLineValues`

Nachgeprüft mit `python3 tools/reverse-engineering/disasm.py gamedata/em.exe 0x977F 0x9D1B`:

| em.exe | Inhalt | Remake |
|---|---|---|
| 0x97CF–0x97F0 | Managerverein im Spiel nur bei 0x52B91 ≠ 0 (Managerpaarung), ohne Spiel immer 0x98F8 | `Strengths` nur mit `Squad` (Managerpaarung) ✓ |
| 0x9958–0x99DC | eingesetzt (+0x0A); Torhüter = +0x15 = 1, Pool +0x18 = 0, Abwehr; Reihe ((+0x15 − 1) & 0x7F, Teil +0x15 ≫ 7) | ✓ |
| 0x99DF–0x9A01 | höchstens DGROUP 0xCC37 + 2·Manager + Teil Spieler (im Spiel), auch für den Torhüter geprüft | ✓ |
| 0x9A04–0x9A27 | Faktor +0x1B / 2 + 50, ohne Spiel und Torhüter 100 | ✓ |
| 0x9A2F–0x9A48 | Halbbytes +0x19: hoch → Überzahl, tief → Unterzahl | ✓ (`HiddenSkills.ForMatch`) |
| 0x9A65–0x9A8A | `[0xED8]` < 2 (Level 4/5): Technik − 10 außerhalb der Reihe +0x02 | `[APPROX]` fehlt; FILE02/16A: `[0xED8]` = 2 (Level 3) → wirkt nicht |
| 0x9A8D–0x9AC2 | `[0xED8]` < 5: Technik − 15 auf fremder Position (0x4180) | ✓ |
| 0x9AF9–0x9B17 | Abwehr · DGROUP 0x4946 = 80 / 140 % (Bytes `50 8C` in em.exe) | ✓ |
| 0x9B42–0x9B7B | DGROUP 0xCC37 + 2·Manager = 4 Verteidiger: Abwehr 0, Sturm × 1,5 | fehlt; 0x38A36 zählt nur die Reihe auf dem Eis (≤ 2 Verteidiger) → tot |
| 0x9B7E–0x9C63 | Summen / 3, Unterzahl/ohne Torhüter, Überzahl, Einsatz auf Kondition | ✓ |
| 0x368B1 | je Teil Technik − Minute·10/(Technik + 1), + Kondition + Form ≤ 255, Puffer DGROUP 0xE06E, Gewichte DGROUP 0x4E99 (`55 0F 00 32 32 00`), + (0x17 − 44) / 2 | ✓ (`OriginalMatch.TeamStrength`) |

**Zahlenprobe FILE02, 1. Spieltag Köln – Preussen (Minute 1)**, von Hand aus dem Dump (`python3 -c` mit `$W/hok.py`,
Kaderplätze +0x10/+0x11/+0x12/+0x13): Energie 101 − Belastung ≫ 3, dann min(…, 91) − 1 (0x84D7). Abwehr 1 (CISSEWSKI TW 80/83/45,
SCHIFFL 79/83/56 E 87, MENDE 80/82/60 E 85): K 79, T (116 + 61 + 60)/3 = 79, F 53, K nach Einsatz 80 → 70 → 202. Sturm 1
(DOROCHIN E 88, LIEBSCH E 87, CHAIDAROV E 90): K 82 → 74, T 77, F 55 → 206. Gewichtung 1: 101 + 103 + (52 − 44)/2 = **208**.
Preussen (Vereinsfelder aus dem Dump: Abwehr 80/74/53, Sturm 71/74/50, 0x17 = 53, Energie 90): 208 / 194 → **205**. Das Remake
(`trace.patch`, `$W/run.sh FILE02 FILE03 f02 1`, Zeile `M 1` des vierten `END`-Blocks): `hS=208 aS=205` – gleich.

### 1.2 Computervereine

Teamwerte des Imports = Vereinsfelder 0x18–0x1D des Spielstands (FILE02 Preussen 80/74/53 und 71/74/50 im Trace `aDef/aAtt`
des ersten eigenen Spiels); Energie 90 (0x84D7: 0x5B, dann herunterzählen) ✓; Form 0x17 ✓. Wanderung 0x9D1B zweimal je Woche
wie dokumentiert (`MatchEngine.DriftTeamValues`). **Ergebnis:** Die Stärke beider Seiten wird wie im Original gebildet;
Köln ist im Remake nicht stärker gerechnet als im Original.

## 2. Reihenwahl (Kandidat 1) und Startzustand (Kandidat 3)

### 2.1 Code

| em.exe | Befund | Remake |
|---|---|---|
| 0x80CB | (2,2); (2,1)/(1,2) nach Spielstand ab Minute 41 bzw. Feldspielern; sonst 2·Stärke als Byte (`shl al,1`) aus dem Puffer (0x368B1 mit „neu“ = 0): **der Schwächere** bekommt Modus 3 (s_a + 20 < s_b → (3,2); s_b + 15 < s_a → (2,3)); Manager nur bei DGROUP 0xA700 + Manager = 0 | `ComputerLineModes` ✓ |
| 0x8EF2 / 0x91FF | Modus 1/2: Wechsel nach > 3 Minuten zur besten (bzw. zweiten) Reihe, Vergleich „Energie + 4 < beste“ mit der Reihennummer (tot); Modus 3: Reihe des gegnerischen anderen Teils, „+ 8“ ebenso tot, vierte Reihe → beste; Sortierung tauscht bei kleinerer Energie, Modus 1 nur zwei Reihen | `ChangeLines`, `LinesByEnergy` ✓ |
| 0x87F7 | setzt `[0xA70E]` = Vereinsfeld 0x16 − 1; Zähler je Reihe = DGROUP 0xCC37 (Neuzählung der Reihe auf dem Eis), Summe (3 − Zähler)·90, Minutenzähler einmal je Teil | `DrainEnergy` ✓ |
| 0x37E77–0x380A1 | Merker = 0x52B91; 0x38D96 nur bei Maustaste (`[0x5A16]` = 1/3); 0x87F7 ×2, Seitentausch, 0x80CB (ungerade), 0x8EF2 ×2, 0x939A (gerade), Strafen | `PlayMinute` ✓ |

### 2.2 Startzustand 0x84D7

Datei 0x84EB–0x857D (nur Spielbeginn, Argument 0): Modus (0x2C Bits 0–2) nur bei Computervereinen auf 0, Minuten 0x2F/0x31 = 0,
Reihen 0x2E/0x30 bleiben aus dem Vorspiel; ist 0x2C Bit 3 gesetzt, Abwehrreihe = 2. Bei ungerader Minute 1 setzt 0x80CB den Modus
sofort; Modus 1/2 wechseln spätestens nach 4 Minuten, Modus 3 folgt sofort. Die übernommene Reihe verschiebt also höchstens die
ersten vier Minuten eines Spiels (Remake beginnt mit Reihe 0) – zu wenig für Abschnitt 4 (dort fehlen der Reihe 3 rund 40
Minuten in drei Spielen).

### 2.3 Modi im Remake (FILE02→03, 16 Saaten)

`$W/run.sh FILE02 FILE03 f02 $(seq 1 16) && python3 $W/lines.py f02 HAH 3`: Köln spielt Modus 2 in Spiel 1 (49,5 von 60
Minuten), gemischt in Spiel 2, Modus 3 in Spiel 3 gegen Kaufbeuren (36,2; Kaufbeuren 247 gegen Köln 207, also Köln der
Schwächere). Das Original endet FILE03 ebenso mit Köln in Modus 3 (Vereinsfeld 0x2C = 0x43) und Kaufbeuren in Modus 2 (0xE2),
Kölns Reihen folgen den Kaufbeurer Reihen über Kreuz (Köln 0x2E/0x30 = 1/1, Kaufbeuren 1/1). Eiszeit je Reihe im Remake in allen
drei Spielen 17–22 Minuten.

### 2.4 Zwangsversuche (schließen die Kandidaten aus)

`EM_FORCE_MODE=m $W/strain.sh FILE02 FILE03 "KÖLNER EC" $(seq 1 16) > $OUT/m$m.txt && python3 $W/pair.py FILE02 FILE03 $OUT/m$m.txt b`
(Reihen wie in FILE03, Σ = Belastungszuwachs der nicht gekippten Spieler, „k“ = Zahl der auf 50 gekippten):

| FILE02→03 | A1 | A2 | A3 | S1 | S2 | S3 |
|---|---:|---:|---:|---:|---:|---:|
| **Original** | +18, k 1 | +20 | +19 | **+39** | +5, **k 2** | **+4** |
| Remake (24 Saaten) | 25,8 ± 5,5 | 13,2 ± 2,9 | 19,2 ± 4,3 | 28,0 ± 5,8 | 21,9, k 0,4 | 12,8 ± 5,1 |
| Modus 1 erzwungen | 14,3 | 15,4 | **0,0** | 42,2 | 20,9, k 1,1 | **−8,5** |
| Modus 2 erzwungen | 24,7 | 13,5 | 22,2 | 26,4 | 19,9, k 0,2 | 12,4 |
| Modus 3 erzwungen | 23,7 | 12,1 | 18,4 | 28,1 | 19,9, k 0,6 | 11,0 |
| Einsatz 40 (`EM_FORCE_EFFORT`) | 25,9 | 12,9 | 19,2 | 27,9 | 18,9, k 0,5 | 10,9 |
| Einsatz 100 | 24,4 | 12,6 | 16,2 | 30,8 | 19,3, k 0,3 | 11,3 |

Im Original spielen Sturm 1/2 viel, Sturm 3 kaum, die Abwehrreihen ausgeglichen. Modus 1 (zwei Reihen) streicht die dritte Reihe
in **beiden** Teilen; Modus 2, Modus 3 und der Einsatz ändern die Verteilung kaum. Da 0x80CB und 0x8EF2 für Abwehr und Sturm
denselben Modus nutzen und Modus 3 über Kreuz folgt, kann keine Stärke- oder Moduswahl Sturm und Abwehr verschieden behandeln.
**Kandidaten 1–3 erklären das Muster nicht.**

## 3. Ergebnisse der Paare (Kandidat „Köln im Remake stärker“)

`$W/run.sh FILE02 FILE03 f02 $(seq 1 16) && python3 $W/res.py f02 HAH` (nur die letzten drei Managerspiele; die ersten drei
`END`-Blöcke stammen aus dem Import von FILE03): Köln im Mittel 11,2 : 10,2 Tore und 3,25 Punkte (24 Siege, 4 Remis, 20
Niederlagen in 48 Spielen); ≤ 2 Punkte bei 6 von 16 Saaten. Original 9 : 13 Tore, 2 Punkte (2:6, 3:3, 4:4). Das erste Spiel
gewinnt Köln bei 13 von 16 Saaten, das Auswärtsspiel in Ratingen verliert es bei 13 von 16. Ein einzelnes Spiel trägt keinen
Stärkebefund; die Summe der drei Spiele liegt im Bereich des Remakes. FILE16A→17 (Saisonrest und Aufstiegsrunde): die Belastung
weicht wie bekannt ab (A.RAUBAL, ZIMMERMANN, TRAVNICEK, 4 Saaten `emtool abgleich … --seed 1…4`); die Spiele lassen sich im
Trace nicht vom Nachspielen beim Import trennen, Ergebnisse dort nicht ausgewertet.

## 4. Was das Original tatsächlich zeigt

- **Aufstellung wechselt zwischen den Spieltagen:** „automatisch aufstellen“ (0x426BC, Tageslauf) tauscht nach dem 1. Spiel
  HANEMANN (K 82 → 81, Wert 69 → 68) gegen LALA (Wert 68, späterer Platz). Kaderplatz +0x06 zählt die Spiele (FILE03:
  HANEMANN 1, LALA 2, Stammspieler 3). Das Remake tauscht ebenso (FILE03-Aufstellung im Abgleich gleich). Reihen deshalb nach
  b gruppieren (`pair.py … b`); die Schätzung in `belastung.md` §3 (Sturm 3 ~9 Minuten) bleibt gültig.
- **Andere Paare, je ein Spiel** (`$W/strain.sh A B "KÖLNER EC" $(seq 1 24)`, `pair.py … b`; Anteil der Remake-Saaten mit Σ ≤
  Original): FILE38→39 (Pokal 1:8) A2 +13 (24/24), S2 +13 (23/24), S1 zwei gekippt; FILE39→40 S3 +13 (23/24), S1 +4 (3/24);
  FILE05→24 A2 +14 (24/24), A3 +11 und ein gekippter (23/24). Es ist also nicht immer Sturm 3: **im Original verteilt sich die
  Eiszeit je Spiel viel ungleicher auf die Reihen**, mal in der Abwehr, mal im Sturm; im Remake liegen alle Reihen bei 17–22
  Minuten. Die Energien am Spielende stützen das: FILE03 Köln Abwehr 3 = 89, Sturm 3 = 87 (lange nicht gespielt), im Remake am
  Ende von Spiel 3 in keiner der 16 Saaten eine Kölner Reihe über 81 (Trace, Zeile `M 60` des sechsten `END`-Blocks).

## 5. Neuer Befund #772 (P2)

0x8365 (Datei 0x839D–0x84BD) setzt Vereinsfeld 0x26 = Grundwert 60/40 (ohne Zuschlag „Unentschieden“), schreibt keine Angriffsbits
und vergleicht 2·Stärke 0x368B1; das Remake nimmt Grundwert + Zuschlag (80/55), Angriff 4/6 und den Teamwert. In Spielen ohne
Manager bleibt das im Original das ganze Spiel so (`python3 $W/clubs.py FILE02 FILE03 FILE16A`: in FILE16A 35 Computervereine mit 0x26 =
0x2D ∈ {40, 60} und Angriff 0). Für #730 ohne Wirkung (Managerpaarung: 0x939A ab Minute 2; Einsatz-Zwangsversuch §2.4).

## 6. Folgerung für Teil 2

Eine Fix-Anweisung lässt sich nicht geben: Alle Stellen, die Belastung, Energie, Modus und Reihe im Spiel schreiben, sind
nachgeprüft (`belastung.md` §1–2 und hier §1–2), und keine zulässige Moduswahl erzeugt die ungleiche Verteilung. **Teil 2 ist
nicht startbar.** Offen bleibt eine Stelle außerhalb der geprüften Funktionen, die im Original Reihen während des Spiels
umstellt oder festhält. Vorschlag für die Entscheidung:

- **A – DOSBox-Aufnahme** (`docs/vorlagen/`, Kurzaufruf DOSBox): FILE02 laden, 1. Spieltag Köln – Preussen ansehen, je Minute
  ein Bild der Live-Anzeige (Reihe auf dem Eis beider Teile), danach sichern. Belegt in einem Spiel, wann und wohin das
  Original wechselt; mit dem Trace (`trace.patch`) Minute für Minute vergleichbar.
- **B – weitere Suche in em.exe:** alle Schreiber von Vereinsfeld 0x2E/0x30 und Kaderplatz +0x15 während des Spiels (außer
  0x8EF2, 0x84D7, 0x38A2A) auflisten, insbesondere 0x38D96 (Live-Spiel) und die Verletzungen 0x38B8F; ein eigener Prüfauftrag.
- **C – #730 als `[APPROX]` zurückstellen** und die Pflichteinträge belassen, bis A oder B vorliegt.

Empfehlung: A (ein Spiel genügt, liefert den direkten Beleg).

## 7. Fortsetzung 2 (R16-E2 = B, 11.11.2026)

### 7.1 #772 behoben – Wirkung auf #730

`MatchEngine.PrepareMatch` (Spielvorbereitung 0x8365 wie in §5), Regressionstest `AbgleichRestPreparationTests`. Danach
`emtool abgleich` erneut (Skripte wie oben; `mean.sh`-Rechnung = Mittel von `strain.sh` je Saat):

| Paar | Größe | vor #772 | nach #772 | Original |
|---|---|---:|---:|---:|
| FILE02→03 (24 Saaten, `pair.py … b`) | Σ A1 / A2 / A3 | 25,8 / 13,2 / 19,2 | 26,5 / 12,4 / 20,5 | +18 / +20 / +19 |
| | Σ S1 / S2 (gekippt) / S3 | 28,0 / 21,9 (0,4) / 12,8 | 28,6 / 19,0 (0,38) / 10,0 | +39 / +5 (2) / +4 |
| FILE02→03 (Saaten 1993, 1, 2, 3, 77) | Mittel Belastung Köln | 100,3 … 104,8 | 100,8 … 104,1 | 97,2 |
| FILE16A→17 (Saaten 4–15) | Mittel Belastung Bayreuth | 91,7 | 90,5 | 83,9 |

**Das Belastungsmuster ändert sich nicht** (Abwehr ausgeglichen, S2 kippt kaum, S3 zu viel). Pflichteinträge #730 neu eingeordnet:
PYLE, A.RAUBAL (FILE16A→17) und J.MEYER (FILE13→14) weichen nicht mehr bei allen Prüfsaaten ab → Sägezahn.

Nebenbefund (kein Fix in diesem Lauf): Liga-Strafminuten aller Vereine FILE02→03 (`emtool abgleich --alle`, Zeilen
`LeaguePenaltyMinutes`, Saaten 1–15): Original 1447; vor #772 Mittel 1454 (1324–1583), nach #772 1308 (1218–1377). Die Strafen
lesen Vereinsfeld 0x26 (Datei 0x3586A); mit dem Einsatz des Originals (40/60) liegt das Remake also rund 10 % unter dem Original –
vorher glich der zu hohe Einsatz (55/80) eine andere Abweichung der Strafrechnung aus (0x35818/0x35540 nicht neu geprüft).

### 7.2 Teil 1c – Schreiber der Reihenwahl und Minuten-Vorgabe

`OUT=… docs/analyse/belastung-staerke/writers.sh` (ndisasm über die ganze em.exe, schreibende Befehle auf die Vereinsfelder
0x2C/0x2E–0x31 und Kaderplatz +0x15, dazu die Blöcke A701/CC43 der Stände). Zuordnung (Spalte „Funktion“ des Skripts an
Regionsgrenzen unzuverlässig, hier von Hand):

| Schreiber | Funktion | im Spiel? | Remake |
|---|---|---|---|
| 0x8272/0x8305 (Modus), 0x82AF–0x8358 (Bit 3 = Torhüter raus, nur Computer, Min. 58/59/68/69) | 0x80CB | ja, ungerade Minuten | `ComputerLineModes`, `GoaliePulled` |
| 0x83DD/0x83EC (Bit 7), 0x844E/0x84C7 (Bit 3) | 0x8365 | Vorbereitung | §7.1 |
| 0x8521 (Modus 0), 0x853F (Min. 0), 0x857D (Abwehrreihe 2 bei Bit 3) | 0x84D7 | Spielbeginn | §2.2 |
| 0x8924/0x8C3C (Minuten 0x2F/0x31) | 0x87F7 | ja | `DrainEnergy` |
| 0x8F7F (Modus = A701 bei 1–4), 0x91CF/0x91E3 (Reihe, Minuten) | 0x8EF2 | ja | `ChangeLines` |
| 0x9506 (Angriffsbits) | 0x939A | ja, gerade Minuten | `FollowTactics` |
| 0x38A2A (Abwehrreihe 0) | 0x388F6 | ja (aus 0x38A36) | `RecountSkaters` |
| 0x38B6F/0x38B86 (Bit 3) | 0x38A36 | ja (0x35691, 0x38B8F) | `RecountSkaters` |
| **0x38F40 (Angriffsbits 4–6; berichtigt Runde 17, `re-livespiel.md` §10, vorher „Modus“), 0x38FAA (Reihe), 0x38EFE (Einsatz)** | **0x38D96** | **nur bei Mausklick im Live-Spiel** (`[0x5A16]`, Felder der Managerzeile über 0x534BC) | **fehlt** (kein Live-Spiel, Taktik-Klasse `[APPROX]`) |
| 0x6F70/0x6F74, 0x29EBF/0x29EC3, 0x4781E–0x47833, 0x4AABF–0x4AD61 | 0x6D7F (Tageslauf/Saisonwechsel), Spielstart, andere Sätze | nein | – |
| Kaderplatz +0x15: 0x6E7D, 0x2799C, 0x29E5B, 0x40CF8, 0x41210, 0x4158A/0x415AB, 0x4776B, 0x4988F, 0x5B084 | Kaderaufbau, Aufstellung, Markt, Laden | nein (keiner im Spiel) | – |

0x38B8F (Verletzungen) schreibt weder Reihe noch +0x15; es ruft nur 0x38A36 (Neuzählung). Außer dem Mausweg 0x38D96 setzt also
keine Stelle die Reihen anders als das Remake.

**Minuten-Vorgabe (Hinweis Nutzer):** Reihenwechsel-Modus DGROUP 0xA701 + Manager und Minutenfelder 0xCC43 + 8 · Manager:

| Stand | A701 (Manager 0–5) | CC43 Manager 0 (Abwehr 1–4, Sturm 1–4) |
|---|---|---|
| FILE02, FILE03, FILE16A, FILE17 (auch FILE01/05/13/24) | 0 0 0 0 0 0 = „Automatisch“ | 0 0 0 0 / 0 0 0 0 |
| FILE38, FILE39, FILE40, FILE44 | 0 0 0 0 0 0 = „Automatisch“ | 2 1 3 1 / 1 4 2 2 (Vorgabe aus einer früheren Einstellung) |

Mit Modus 0 schreibt 0x8EF2 keinen Modus (Datei 0x8F60–0x8F65), der Modus kommt aus 0x80CB wie beim Computerverein (Datei
0x8255–0x8264: nur bei A700 + Vereinsfeld 0x16 = 0); die Minutenfelder liest nur Modus 4 (0x9140/0x91AE, DGROUP 0xCC73). Die
Minuten-Vorgabe ist also in keinem der Belastungspaare aktiv; wie das Original Vorgabe 0 im Modus 4 behandelt, ist für #730 ohne
Belang (Remake: `Tactics.MinuteQuotas`, Ringausgleich auf Summe 8 wie Datei 0x48EEA–0x4901D).

### 7.3 Folgerung

Kein Beleg für die Ursache. Einziger nicht nachgebauter Schreiber ist der Mausweg 0x38D96: Klickt der Spieler im Live-Spiel in die
Managerzeile (Einsatz, Angriff, Modus, Reihe), stellt das Original Reihen und Modus um; ob das bei FILE02→03 / FILE16A→17
geschah, belegt nur eine Aufnahme. **Teil 2 bleibt blockiert; Fallback R16-E2: C** (#730 als `[APPROX]` zurückstellen,
Pflichteinträge bleiben, DOSBox-Liste: FILE02 laden, 1. Spieltag ohne Mausklick im Live-Spiel, je Minute ein Bild der Reihen
beider Teile, danach sichern – zeigt die Verteilung ohne Eingriff; dieselbe Aufnahme mit Klicks auf die Reihenknöpfe als Gegenprobe).

### 7.4 Abschluss (Fortsetzung 3, R16-E2 = C)

#730 zurückgestellt: `[APPROX]` an der Reihenwahl (`OriginalMinutes.ChangeLines`, Verweis auf §7), die Pflichteinträge #730 in
`AbgleichOriginalPaareTests` bleiben, Issue #730 bleibt offen bis zur DOSBox-Aufnahme nach §7.3. Keine weitere Ursachensuche.

## 8. #773 – Liga-Strafminuten nach #772 (Fortsetzung 3)

**Befund:** FILE02→03 Summe `LeaguePenaltyMinutes` aller Vereine Original 1447, Remake (Saaten 1–16) 1218–1377, Mittel 1302.

**Code (ndisasm, Datei 0x35818–0x35CBD, Aufrufe 0x3808E/0x3809E bzw. 0x3850E/0x3851E):** Strafbank 0x35540(Verein, 1), bei zwei
belegten Plätzen keine Strafe; Einsatz = Vereinsfeld 0x26 (Datei 0x3586A), bei Managervereinen + Managerwert − 20 und auf
0 … 100 begrenzt (0x35873–0x358B4); Strafe, wenn lrandom(0, 12) = 0 oder lrandom(0, 110 − Einsatz) = 0 (0x358B7–0x358E9);
5 Minuten (Code 13) nur auf Platz 0 mit lrandom(0, 15) = 0; Ligazähler + (Code & 7) nur bei Wettbewerb 7 (0x35967–0x35986);
Kettenstrafe des Gegners mit lrandom(0, 3) = 0 bei leerem Platz 0, Folgeaufruf mit Verein = zweitem Argument (keine weitere
Kette). Alles wie `OriginalMinutes.Penalty` / `OriginalMatch.PenaltyMinutes` / `PenaltyBox.Tick`.

**Einsatz im Spiel:** Schreiber von 0x26 in em.exe nur 0x8479 (0x8365, vor jedem Spiel), 0x9523 (0x939A, nur Managerpaarung,
Aufruf hinter `[bp-0xf]` = Live-Paarung, Datei 0x37E03/0x37F9B), 0x38EFE (Mausweg) und die Taktik-Bildschirme 0x48007/0x48ED1/
0x4903E. Computerpaarungen spielen also mit dem Grundwert 40/60; FILE03 (`clubs.py`): 40 × 25, 60 × 9, 0 × 16 (kein Spiel) –
wie das Remake nach #772. Erwartung aus der Regel bei Einsatz 40–60: 11,7–12,1 Strafminuten je Verein und Spiel; Remake 12,1,
Original FILE02→03 13,4.

**Weitere Paare (Zuwachs der Summe, Original-Ausgangswert aus dem Vorpaar):**

| Paar | Spieltage | Original | Remake (Saaten) | Mittel |
|---|---|---:|---|---:|
| FILE02→03 | 1.–3. | 1447 | 1218–1377 (1–16) | 1302 |
| FILE05→24 (FILE05 = FILE03) | 4. | +441 | +390 … +497 (1–8) | +445 |
| FILE16A→22 (FILE16A Original 894) | Rest der Saison | +430 | +399 … +467 (1–6) | +437 |

**Ergebnis:** kein Fehler der Strafrechnung. Die Regel und der Einsatz entsprechen dem Original; zwei unabhängige Paare liegen
genau im Bereich, nur die ersten drei Spieltage FILE02→03 darüber (Original-Streuung je Verein 12,6 zu 8,4 im Remake – Zufall
der einen Aufnahme bzw. der Zufallsquelle lrandom, deren Folge nicht nachbildbar ist, `MatchEngine.Original.cs` `[APPROX]`).
Vor #772 glich der zu hohe Einsatz (80/55) diese eine Abweichung zufällig aus. Probe: lrandom (16 · x + 167 mod 4.000.000)
statt `Random` in `OriginalMatch.Rnd` ändert FILE02→03 nicht (Saaten 1–12: 1233–1363, Mittel 1286) – das Remake spielt die
Paarungen nacheinander, das Original minutenweise verschränkt (0x37DCF), daher keine zugexakte Probe.
Wächtertest `AbgleichRestPenaltyTests` (FILE05→24, Mittel über vier Saaten ±8 % um +441; mit Einsatz + 20/15 wie vor #772 rot:
Mittel 482,5).
