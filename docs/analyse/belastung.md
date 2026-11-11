# Belastung der Managerspieler (#730 Rest) – Ursachensuche

Spur `abgleich-rest` Teil 1 (Runde 16), Stand 11.11.2026. Ausgangsbefund: `reviews/2026-11-11-abgleich-fix.md` B1 – im
Original kippen Vielspieler der Reihen 1/2 vor dem Training über 127 (→ 50, `cbw` in Datei 0x4BD56), im Remake fast nie
(FILE02→03 HOCK/SIKORA, FILE16A→17). **Ergebnis: Ursache nicht belegt** – alle Stellen, die die Belastung schreiben, und die
Reihenwahl stimmen mit em.exe überein; die Abweichung liegt in der Verteilung der Eiszeit auf die Sturmreihen, deren Ursache
offen bleibt (siehe „Offene Spur“). Teil blockiert, Teil 2 hängt daran.

## 1. Wer schreibt die Belastung (Kaderplatz +0x13)

Suche nach allen Schreibbefehlen auf `[es:bx+0x13]` in em.exe (Byte-Muster `26 fe 47 13`, `26 88 47 13`, `26 c6 47 13`,
`26 80 47/6f 13`, ALU-Formen): genau sechs Stellen, alle im Remake vorhanden.

| Datei | Wirkung | Remake |
|---|---|---|
| 0x8C82 | Spielminute, Spieler auf dem Eis: `lrandom(0, 4 + (Kondition > Technik)) = 0` → + 1 | `OriginalMinutes.DrainEnergy` |
| 0x6B0F | Training: Belastung − Erholung (Byte), dann 0x4BD56 (50 … 150, `cbw`) | `TrainingRules.Apply` |
| 0x549AC/0x549BB | Spielabrechnung: 50 … 150 **ohne** Vorzeichen (`cmp …,0x32; jnc` / `cmp …,0x96; jna`) | `TrainingRules.ClampStrain` |
| 0x71C8 | Markt-Verfall (Satz 0x7623) | `DailyRunMarket` |
| 0x42E5E | neuer Spieler `random(80, 120)` | `Transfer.Pool`, `FinanceTalent` |

0x4BD56 (vorzeichenbehaftete Begrenzung) hat als Belastungsschreiber nur den Aufruf 0x6B25 (Training); die Spielabrechnung
begrenzt ohne Vorzeichen. Ein Kippen auf 50 gibt es also nur im Training, und nur wenn Belastung − Erholung ≥ 128 ist.

## 2. Nachgeprüfte Abläufe (alle wie im Remake)

- **Energie und Belastung je Minute** 0x87F7 (Managerteil ab 0x8B31): Schleife über eingesetzte Kaderplätze (+0x0A), Teil =
  +0x15 ≫ 7, „auf dem Eis“ = (+0x15 & 0x7F) − 1 = Reihe des Teils (Vereinsfeld 0x2E + 2 · Teil), Minutenzähler 0x2F + 2 · Teil
  einmal je Teil, Zug 0x8C53–0x8C82, Abzug `(lrandom(0, Einsatz) + 75) / 40 + 1 + 4·[m ≥ lrandom(0, 5)] + 8·[m > lrandom(5, 15)]`,
  Begrenzung 1 … 99 (0x4BD35), Reihenenergie = Summe / 3 mit 90 je fehlendem Platz, Torhüter (Pool +0x18 = 0) nicht in der Summe.
- **Spielvorbereitung** 0x84D7: Energie = 101 − (Belastung ≫ 3), dann `min(…, 91) − 1`; Drittelpause + 20 (gleiche Grenze).
  Bei Spielbeginn setzt 0x84D7 den Modus (Vereinsfeld 0x2C Bits 0–2) **nur bei Computervereinen** auf 0 und die Minuten auf dem
  Eis auf 0; die Reihe auf dem Eis bleibt aus dem Vorspiel stehen. Wirkt höchstens auf die ersten Minuten (siehe „Offene Spur“).
- **Modus der Reihenwechsel** 0x80CB (ungerade Minuten): (2, 2); (2, 1) bei Führung von a nach Minute 40 oder weniger
  Feldspielern von a; (1, 2) bei Rückstand bzw. mehr Feldspielern, dann ohne Stärkevergleich; sonst 2 · Stärke (8 Bit):
  +20/+15 → (3, 2)/(2, 3). Manager schreiben den Modus nur bei DGROUP 0xA701 + Manager = 0 („Automatisch“).
- **Reihenwechsel** 0x8EF2 mit 0x91FF/0x8E5F: Sprungtabelle Datei 0x91F7 (Modus 1/2 → 0x9052, 3 → 0x907F, 4 → 0x90D9);
  Modus 1/2 wechseln nach mehr als 3 Minuten zur energiestärksten Reihe (bzw. zur zweiten), Modus 1 betrachtet nur zwei Reihen;
  der Vergleich „Energie + 4 < beste Reihe“ vergleicht mit der Reihennummer (greift nie). Sortierung 0x91FF tauscht nur bei
  kleinerer Energie, Rückgabe hoch = beste, tief = zweite Reihe.
- **Aufrufreihenfolge je Minute** Datei 0x37F8B–0x38073: 0x874D (Torhüterminuten), 0x87F7 für beide (nur Managerpaarung,
  Merker `[bp-0xF]` aus 0x52B91), Seitentausch `random(0, 1)`, 0x80CB (ungerade Minute), 0x8EF2 (a, b), 0x8EF2 (b, a), 0x939A
  (gerade Minute) – wie `OriginalMinutes.PlayMinute`.
- **Training** 0x659C–0x6B2A: Belastung unsigniert gelesen (0x6623), Erholung `Round(Energie · 200 / Norm, Energie-Modus)`;
  Takt Mittwoch/Samstag (Zähler 0x3B7 ≡ 0/4 mod 7) – FILE02→03 im Remake drei Takte (Tage 3, 6, 10) mit Erholung 1 (Sturm) bzw.
  0 (Abwehr, Torhüter), passend zu den Spielern ohne Einsatz im Original (Sturm −3 über drei Takte, Abwehr ±0).

## 3. Was abweicht: Eiszeit der Sturmreihen

Zuwachs je Spieler = Δ Belastung + Erholung · Takte, geteilt durch die Zugwahrscheinlichkeit 1/5 bzw. 1/6
(Kondition > Technik), ergibt die Minuten auf dem Eis. Original aus `emtool hok dump` (E · 1,5, ±1,5 Rundung), Remake aus
`emtool abgleich FILE02 FILE03 --seed 1…16` (Mittel) und einer Zählung der Reihe auf dem Eis je Minute (16 Saaten, 48 Spiele):

| FILE02→03 (3 Spiele) | Sturm 1 | Sturm 2 | Sturm 3 | Abwehr 1 | Abwehr 2 | Abwehr 3 |
|---|---:|---:|---:|---:|---:|---:|
| Original, Minuten je Spiel (geschätzt) | ~28 | ~23 (HOCK ≥ 32) | **~9** | ~15 | ~18 | ~17 |
| Remake, gezählt | 20,8 | 21,1 | 18,2 | 20,5 | 21,3 | 18,1 |

- Im Original spielen die Sturmreihen 1/2 deutlich mehr, Sturmreihe 3 kaum (BRANDL +4,5, STEFAN +6 über drei Spiele; DOROCHIN
  +19, LIEBSCH +15, CHAIDAROV +16); die Abwehrreihen sind ausgeglichen. HOCK braucht für das Kippen ≥ 16 Züge in drei Spielen
  (115 + Zuwachs − 2 ≥ 129), bei 1/6 also rund 32 Minuten je Spiel – mit der Verteilung des Remakes (Mittel +6) fast nie.
- Ergebnisabhängigkeit erklärt es nicht: Auch in den 20 verlorenen Remake-Spielen bleibt die Verteilung 20,9/22,1/17,0
  (Modus 1 im Mittel 14 Minuten). Im Original verlor Köln 2:6, dann 3:3, 4:4; im Remake gewinnt Köln das erste Spiel bei 13 von
  16 Saaten.
- Gegenprobe FILE38→39 (Pokal 1:8): Sturm 1/2 ~30/27, Sturm 3 ~11, Abwehr 2 ~33 – DOROCHIN und LIEBSCH (123) kippen.
  FILE39→40 (Liga): Sturm 3 spielt viel (LALA +7, STEFAN +5,5). Die Verteilung hängt im Original stark vom Spiel ab.

## 4. Offene Spur (für die Entscheidung)

Die Asymmetrie Sturm/Abwehr kann nicht aus Modus 1 kommen (wirkt auf beide Teile). Kandidaten, im Remake nicht geprüft:

1. **Modus 3 „Nach Gegnerreihe“**: Der Sturm folgt der Abwehrreihe des Gegners (Vereinsfeld 0x2E des Gegners), die Abwehr dessen
   Sturmreihe. Spielt die Abwehr des Computervereins selten Reihe 3, folgt der eigene Sturm. Dafür müssten Stärke (0x368B1,
   Gewichtung 1) oder Reihenwerte der Computervereine anders sein als im Remake – passend zu den Ergebnissen (Original 0-2-1,
   Remake meist Sieg).
2. **Spielstärke des Managervereins** (0x98F8/0x368B1): Köln ist im Remake stärker als im Original (Ergebnisse oben); das
   verschiebt den Modus über 0x80CB.
3. **Startzustand je Spiel** (0x84D7): Reihe auf dem Eis und Modus des Managers aus dem Vorspiel, `SkatersPerPart` aus der
   letzten Neuzählung (`[APPROX]` im Remake). Wirkt nur am Spielanfang, reicht allein nicht.

Ohne Mitschnitt der Reihen je Minute im Original (DOSBox, Live-Spiel mit Anzeige „Reihe“) lässt sich das nicht belegen; die
Nachrechnung über die Belastung hat ±2–3 Züge Streuung je Spieler. Vorschlag für die Leitstelle: (a) eine DOSBox-Aufnahme eines
Spiels FILE02 mit Bildern der Aufstellung je Drittel, oder (b) eigener Prüfauftrag „Spielstärke Managerverein gegen Original“
(Kandidaten 1/2), danach Teil 2.

Werkzeuge der Untersuchung (nicht im Repo): temporäre Ausgabe der Belastung in `TrainManager`/`MatchEngine.Live` und der
Reihe/des Modus je Minute in `OriginalMinutes.PlayMinute`; `disasm.py` für 0x6590–0x6B40, 0x80CB–0x8370, 0x84D7–0x87F7,
0x87F7–0x8E5F, 0x8E5F–0x93A0, 0x37A62–0x386D1, 0x54880–0x549D0.
