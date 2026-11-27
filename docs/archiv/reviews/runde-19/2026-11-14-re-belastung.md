# Erstreview `re-belastung` (PR #855)

**PR:** #855 · **Base:** `9f1994e` · **Head:** `ae23d29` · **Reviewer:** Claude Opus 5.5 (geplanter Lauf, frische Sitzung, nicht an
#855 beteiligt). Checkout genau auf dem Head (`git rev-parse HEAD` = `ae23d2913c8851ed1dbeadc5efe59927650643cf`, `git status
--porcelain` leer). `git diff --stat 9f1994e ae23d29`: nur `docs/analyse/belastung-staerke.md`, `docs/analyse/belastung-staerke/*`
(Analyse-Skripte) und `docs/arbeitspakete.md` → `tools/pruefen.sh --schnell` genügt.

## 1. Merge-Empfehlung

**Nein** für `ae23d29` (Label `review-nein`) – wegen eines P2 an der Kernaussage, Behebung nur Text. Alles Nachgerechnete stimmt
zahlengenau (Auswertefehler KÜHN, FILE02→03, FILE50→03, sieben Ein-Spieltag-Paare, Kipp-Häufigkeiten, `lrsim.py`); die gelesenen
em.exe-Stellen stimmen mit dem Remake. Aber die Überschrift „**kein belegter Unterschied mehr**“ (Z. 260) und die Folgerung „kleiner
als die Streuung eines Spieltags“ (Z. 342) trägt der Beleg nicht: zwei Größen, die der Bericht selbst ausgibt bzw. die direkt daraus
folgen, liegen außerhalb aller Saaten (P2-1). Das ist die Grundlage der Nutzerentscheidung A/B (§9.5). Ist §9 berichtigt, genügt nach
`zusammenarbeit.md` §5 Nr. 4 die Delta-Prüfung der Leitstelle.

**#730 bleibt offen** (bestätigt; Entscheidung Nutzer A/B nach §9.5). Mit P2-1 spricht der Befund eher für **A** (echtes Paar per
DOSBox), weil gerade FILE02→03 – das Paar von #730 – die deutlichste Abweichung zeigt.

## 2. Befunde

**P2-1 – „Kein belegter Unterschied mehr“ zu stark: Paarsummen und Eiszeitverteilung liegen außerhalb aller Saaten.**
[belastung-staerke.md Z. 260](https://github.com/kaaasi/eishockey-manager-redone/blob/ae23d2913c8851ed1dbeadc5efe59927650643cf/docs/analyse/belastung-staerke.md#L260),
[Z. 286–289](https://github.com/kaaasi/eishockey-manager-redone/blob/ae23d2913c8851ed1dbeadc5efe59927650643cf/docs/analyse/belastung-staerke.md#L286-L289),
[Z. 338–347](https://github.com/kaaasi/eishockey-manager-redone/blob/ae23d2913c8851ed1dbeadc5efe59927650643cf/docs/analyse/belastung-staerke.md#L338-L347).
Der Bericht prüft nur jede Reihe einzeln (Σ z² über Reihen). Das ist gegen eine Verlagerung der Eiszeit zwischen Reihen und gegen
eine Abweichung der Gesamtzahl schwach; beides zeigt sich mit denselben Daten:

- **Paarsummen** (Treffer aller Spieler, Zahlen aus Z. 287–289, `hits.py`, 40 Saaten bzw. FILE52→53 60 Saaten): z = −2,82 / −0,65 /
  +0,55 / −1,15 / +0,04 / −1,90 / +1,70 → **Σ z² = 16,5 bei 7 Paaren, p ≈ 0,02**. FILE02→50: Original 44, Remake 60,9 ± 6,0
  (**48–77, keine der 40 Saaten ≤ 44**). Der Bericht nennt die Zahlen, bewertet sie aber nicht („± 5–8“). Die Richtungen sind gemischt
  (−, +): eher größere Streuung der Paarsummen im Original als eine Verschiebung.
- **FILE02→03, Sturm 1 gegen Sturm 3** (Differenz der Reihensummen je Saat, 200 Saaten): Original 48 − 13 = **35**, Remake
  15,5 ± 7,5, **keine der 200 Saaten ≥ 35** (z +2,61); S1 + S2 − 2·S3: 60 gegen 26,7 ± 12,3, 1 von 200. FILE50→03 ebenso (31 gegen
  11,6 ± 8,1, 0 von 40; kein unabhängiges Paar). Die Ein-Spieltag-Paare zeigen keine feste Richtung (FILE24→25 −1 gegen 4,1 ± 3,8,
  FILE52→53 −3 gegen 4,8 ± 4,7, FILE02→50/51 +2/−4 gegen 1,0 ± 4,6) – eine allgemeine Regel „Original lässt Sturm 3 ruhen“ ist
  damit nicht belegt, wohl aber, dass FILE02→03 die Eiszeit anders verteilt als jede der 200 Saaten.

Beide Kontraste sind nachträglich gewählt (bei etwa zehn naheliegenden Kontrasten bleibt 0/200 mit Bonferroni unter 5 %); sie
belegen keine Ursache, widerlegen aber „kein belegter Unterschied“ und „kleiner als die Streuung eines Spieltags“ (2,6–2,8 s).
Probe (Vorbereitung §0, `OUT` gesetzt, Saaten wie §9.2):

```bash
W=docs/analyse/belastung-staerke; for k in 0 1 2 3; do $W/strain.sh FILE02 FILE03 "KÖLNER EC" $(seq $((k*50+1)) $((k*50+50))); done > $OUT/s_FILE02_FILE03.txt
sed -e "s#os.path.dirname(__file__)#'$W'#" -e 's#^        rs = .*lines\[g\]) for v in rem.values()\]#&; R[g]=(osum,rs)#' -e 's#^Z = \[\]; P = \[\]#Z=[];P=[];R={}#' $W/hits.py > $OUT/h.py
printf '%s\n' "o=R['S1'][0]-R['S3'][0]; d=[x-y for x,y in zip(R['S1'][1],R['S3'][1])]" "print(o, st.mean(d), st.pstdev(d), sum(x>=o for x in d), len(d))" >> $OUT/h.py
python3 $OUT/h.py "FILE02 FILE03"    # → 35 15.5 7.5 0 200
```

Auflage: Überschrift Z. 260 und §9.5 auf „in den Einzelreihen kein Unterschied; Paarsumme FILE02→50 und die Verteilung Sturm 1/3 in
FILE02→03 liegen außerhalb aller Saaten, Ursache offen“ ändern; „kleiner als die Streuung eines Spieltags“ streichen; Empfehlung
§9.5 entsprechend (B ist dann eine Zurückstellung, kein „kein Befund“).

**P3-1 – §9.1: `strain.sh` war nicht fehlerhaft.** [Z. 267](https://github.com/kaaasi/eishockey-manager-redone/blob/ae23d2913c8851ed1dbeadc5efe59927650643cf/docs/analyse/belastung-staerke.md#L267)
`emtool abgleich` gibt `KÜHN` aus (kein `]`); alter und neuer `sed`-Ausdruck liefern für FILE02→03 Saat 1 dieselben 25 Zeilen
(`diff` leer). Der Fehler lag allein in `pair.py` (Dumpname `K]HN`); das „bzw.“ sollte das sagen. Änderung an `strain.sh` harmlos.

**P3-2 – §9.3 Zeile 0x80CB verkürzt.** [Z. 322](https://github.com/kaaasi/eishockey-manager-redone/blob/ae23d2913c8851ed1dbeadc5efe59927650643cf/docs/analyse/belastung-staerke.md#L322)
Die Bedingungen enthalten auch den Feldspielervergleich (Vereinsfeld 0x32, Datei 0x8169/0x8195: weniger Feldspieler → (2, 1), mehr
→ (1, 2) ohne Stärkevergleich); `ComputerLineModes` (`MatchEngine.Managed.cs:450–455`) setzt das richtig um. Nur Text.

**P3-3 – Σ z² mit χ²-p-Werten setzt unabhängige Reihen voraus.** Reihen eines Vereins teilen sich die Eiszeit (Korrelation S1/S3
in FILE02→03 −0,09, FILE50→03 −0,23, FILE24→25 −0,23); die Rangzahlen („Saaten ≤/≥“) sind die tragfähigere Angabe. Kein Einfluss
auf die Zahlen, nur auf deren Lesart.

## 3. Ausgeführte Prüfungen

| Prüfung | Befehl | Ergebnis |
|---|---|---|
| Auswertefehler KÜHN | `pair.py FILE50 FILE03 x.txt b` (Saaten 1993, 4711, 1, 2, 3, 77), alt (Base) gegen neu | A2 Remake 7,0 → 12,7, Σ≤Orig 5/6 – wie Z. 270 ✓ |
| FILE02→03 | `strain.sh` 200 Saaten, `hits.py` | Tabelle Z. 293–299 zahlengenau, Σ z² 10,4, Summe 164 gegen 167,4 ± 12,7 ✓ |
| FILE50→03 | 40 Saaten | S1 38 (0/40), S2 31 (0/40), S3 7, Σ z² 13,2 ✓ |
| Ein-Spieltag-Paare | FILE02→50/51, 38→39, 39→40, 05→24, 24→25 je 40, 52→53 **60** Saaten | Σ z² 50,7 bei 42 (Bericht 50,6 mit 200), je Spieler 142,7 bei 110 (140,0), Summen wie Z. 287 ✓ |
| Kippen | Zählung Remake ≤ 60 | HOCK 5, SIKORA 68, MENDE 194, BRANDL 47 von 200; REZNICEK 4 von 60 ✓; FILE16A→17 nicht nachgerechnet |
| lrandom | `lrsim.py`; zusätzlich feste Abstände 0/1/5/37/100 Züge (300 Spiele je) | 61,8 ± 7,44 / 60,9 ± 7,45; fest 60,6–61,1, je Spieler gleich → Ausschluss für den Managerteil von 0x87F7 getragen ✓ |
| em.exe | `disasm.py` 0x8B31–0x8DF8, 0xBFD5, 0x80CB–0x81C0, 0x86A0–0x8700, 0x9205/0x9350–0x9399 | lrandom 16x+167 mod 0x3D0900 ✓; Bank (0,4)→(0,100) gegen +0x10, Eis Treffer (0, 4+[K>T]), Einsatz (r+75)/40+1, +4 (0,5), +8 (5,15) ✓; Erholung min(e+20, 91)−1 ✓; Modus 1 sortiert nur Reihen 0/1 ✓; 0x80CB s. P3-2 |
| Prüfung | `tools/pruefen.sh --schnell` | s. u. |

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3249 | 0 | 0 |

Commit `ae23d2913c8851ed1dbeadc5efe59927650643cf`, SDK 10.0.402, „Ergebnis: bestanden“.

**Nicht nachgestellt:** Gegenprobe „LRandom in `OriginalMatch.Rnd`“ (Produktcode-Patch), Trace §9.4, 0x388F6, 0x37E77, 0x52B91,
0x54840. **Akzeptierte `[APPROX]`:** keine neuen. **Annahmen:** Mindestzahl „k“ (128 + Trainings − Start) als untere Schranke ist
für Original und Remake gleich behandelt – fair; `lrsim.py` vereinfacht die Reihenwahl (Modus 2), deckt die Kopplung also nur im
Managerteil ab, nicht über Chancen/Strafen (dort `abgleich-dosbox.md` §2.5).

Hinweis: `hits.py` ohne `python3 -I` legt `docs/analyse/belastung-staerke/__pycache__/` an (nicht in `.gitignore`).
