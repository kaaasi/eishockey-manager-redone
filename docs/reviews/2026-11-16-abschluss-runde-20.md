# Review PR #886: Abschluss Runde 20 (Archiv, Kopf Runde 21, Vormerkungen, Funktionskarte)

Geprüft: Base `72d3a9897395bdf29e0179d8ef4bb0ad1844e652` (= `main`), Head `83bb87b1dba2b04e4539038622de118e333cf5ce`
(Checkout nachgewiesen: `git rev-parse HEAD` = Head, `git status --porcelain` leer, `git merge-base` = Base). Reiner Doku-PR, kein
Build, keine CI (nach Auftrag). Reviewer: Claude (Sonnet 5.5), eigene Sitzung, an #886 nicht beteiligt.

`git diff --stat 72d3a98 83bb87b`:

```
 docs/anforderungen/zusatzfunktionen.md |   4 ++
 docs/arbeitspakete.md                  | 113 ++++--------------
 docs/archiv/runde-20.md                | 123 +++++++++++++++++++++
 docs/re-abdeckung.md                   |   8 +--
 4 files changed, 144 insertions(+), 104 deletions(-)
```

`git diff --stat 72d3a98 83bb87b -- src tests tools .github` ist leer: kein Code, keine Tests, keine Werkzeuge berührt.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head (Lesart: nur Doku-Prüfung, kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4 nötig, da keine
Änderung unter `src/`, `tests/`, `tools/`). Archivprobe bestanden (mit der unten genannten Einschränkung), alle geprüften Angaben zu
PRs, Reviews, Issues und Entscheidungen stimmen mit GitHub überein, Währungseintrag und Funktionskarte sind am Original
nachvollzogen. Zwei P3 (Doku), keiner blockiert. Erwartet offen bestätigt: #882, A093.

## 2. Befunde

**P3-1 – Zwei weitere offene Issues fehlen im „Abschluss“.** Archiv (`docs/archiv/runde-20.md`, Zeile „Abschluss (16.11.2026)“)
und Kopf Runde 21 nennen als offen nur #882 und A093. Offen sind aber auch
[#864](https://github.com/kaaasi/eishockey-manager-redone/issues/864) und
[#869](https://github.com/kaaasi/eishockey-manager-redone/issues/869) („DOSBox-Aufnahme runde-19: abgelegt“, ohne Label).
Beleg: `gh api repos/kaaasi/eishockey-manager-redone/issues?state=open` → #886 (PR), #882, #869, #864. Die zugehörigen
Referenz-PRs sind gemergt: `gh api repos/kaaasi/eishockey-manager-referenz/pulls/{35,36,37,39}` → je `merged=true`. Die Issues sind
damit reine Ablage-Quittungen und vermutlich zu schließen; solange sie offen sind, stimmt „offen: #882“ nicht wörtlich. Vorschlag:
Leitstelle schließt sie oder ergänzt „(#864, #869 Ablage-Quittungen, erledigt)“. Kein Einfluss auf den Merge.

**P3-2 – Kopfzeile des Archivs ungenau.** `docs/archiv/runde-20.md` Zeile 3–4: „Inhalt unverändert bis auf Überschrift, ‚Ergebnis‘,
‚Abschluss‘ und ‚Entscheidungen …‘“. Die Überschrift ist unverändert (Zeile 29 = Zeile 11 an `a509f5c`); neu sind nur Titel und Kopfblock
(Zeilen 1–28). Ob der Auftrag „wörtlich enthalten“ so zu lesen ist, zeigt die Archivprobe unten. Rein sprachlich, kein Fehler im
Inhalt.

Keine P0–P2. Keine Befunde zu Währungen, Funktionskarte, Entscheidungen N1–N6, R20-D1…D4.

## 3. Ausgeführte Prüfungen

**Gegenbeispiel Archivprobe** (Abschnitt „Aktuelle Runde: Runde 20“ aus `docs/arbeitspakete.md` an `a509f5c` wörtlich in
`docs/archiv/runde-20.md`): `git show a509f5c:docs/arbeitspakete.md | sed -n 11,121p` (Abschnitt bis vor „## Paket `hok-format`“,
111 Zeilen) gegen `sed -n '29,$p' docs/archiv/runde-20.md` (95 Zeilen), Ergebnis `diff`: Zeilen 1–95 des Abschnitts
(Überschrift, Lage, Zuständigkeit, Spuren `inventur-v1` und `pruefung-karte`, Gesamtreview) **Byte für Byte gleich**. Nur die
Unterabschnitte „Zurückgestellt“ und „Vormerkungen (nach Runde 19)“ (Zeilen 96–111) fehlen im Archiv. Das ist beabsichtigt und in der
Archivkopfzeile so beschrieben („ohne ‚Zurückgestellt‘ und ‚Vormerkungen‘, bleiben in `arbeitspakete.md`“); „Zurückgestellt“ steht
dort unverändert (A093, R8-A01), „Vormerkungen“ wurde zu „nach Runde 20“ fortgeschrieben. Nichts Inhaltliches ging verloren.
Der Archivlink `archiv/runde-20.md` und alle Links im Archivkopf (`analyse/restliste-v1.md`, `…-klassen.csv`,
`markerliste.py`) lösen auf.

**Erwartet offen:** #882 offen (P3 aus #881, `gh api …/issues/882`); A093 steht in `arbeitspakete.md` „Zurückgestellt“ und in
`restliste-v1-klassen.csv` als D (Review #885 bestätigt es ebenfalls).

**Ergebnis/Abschluss gegen GitHub** (`gh api …/pulls/<n>`, `…/issues/<n>`, Events):

- #878 Plan, gemergt (Label `review-ja`); #879 Plan-Review, gemergt.
- #880 `pruefung-karte`, gemergt; #881 Erstreview, gemergt, Datei `2026-11-16-pruefung-karte-r20.md`: „Ja“, fünf P3 → #882. ✓
- #883 `inventur-v1`, gemergt; #884 Erstreview, Datei `2026-11-16-inventur-v1.md`: Nein (P2-1…3, P3-1…3); Fixlauf (Head `1fdb332`)
  mit Delta-Prüfung der Leitstelle in der Merge-Prüfung von #883 dokumentiert. ✓
- #885 Gesamtreview, gemergt (= `main` `72d3a98`), Empfehlung **Ja** für `a509f5c`, keine neuen Befunde; Autor laut Commit-Trailer
  `Co-Authored-By: Codex` (Archiv: „ChatGPT/Codex“ ✓).
- Geschlossen: #871, #872, #874 (completed). #872 am 15.11. 21:38 geschlossen und 13 s später wieder geöffnet, am 23:51
  geschlossen – deckt „nach versehentlichem Schließen beim Plan-Merge wieder geöffnet“ (Zeitfolge plausibel, Plan-Merge gleichzeitig
  nicht einzeln geprüft). Angaben „Datei 0x84D7, Felder 0x2E/0x30, Schema 51“ stimmen mit Review #881 und
  `docs/formate/spielstand-original.md` Z. 1074.
- Restliste: `restliste-v1-klassen.csv` per Python gezählt: **689** Einträge, V 85, D 45, E 295, K 182, X 82 → wie im Archiv. ✓
  (Die 568 in `restliste-v1.md` Z. 151 sind die Abweichungsliste, ein anderer Zähler.)
- Entscheidungen: R20-D1 = A, D2 = B, D3 = A + Zusatz, D4 = C (Archiv; der Plan-Review `2026-11-15-plan-runde-20.md` bestätigt D1…D4 gegen die PR-Beschreibung von #878; Optionstexte nicht neu geprüft). N1–N5 stimmen wörtlich mit dem Nutzerkommentar in #883 vom 16.11. 07:51Z überein
  (N1 = B, N2 = B, N3 = B, N4 = C, N5 = C), N6 = A mit dem Kommentar 08:22Z (A061 E; A271, A274, A438, A452 K). Im Archiv ist N5
  als „Buchung bleibt, Original-Finanzübersicht prüfen und ggf. ohne diese Posten“ korrekt verkürzt.
- Kopf Runde 21 und „Vormerkungen (nach Runde 20)“: #882, Testlücke #848, „Vor v1.0“ (R20-D3-Zusatz, Befehl
  `markerliste.py --abweichungen` existiert), N6-Wiedervorlage, „Nach v1.0“ (R15-E3, bitgenauer Zufallsmodus, Sprachen und
  Währungen N1 = B) – alle in der Quelle belegt; nichts aus „Vormerkungen (nach Runde 19)“ unbegründet entfallen (#871/872/874
  erledigt, Rest übernommen).

**Währungen** (`docs/anforderungen/zusatzfunktionen.md` A 2 gegen `docs/reverse-engineering.md` „Geldbeträge“ Z. 97–101 und
Z. 453): Eintrag „Betrag × DGROUP [0x7A1] / [0x79D] (deutsch 1/3)“ ist wörtlich die Aussage von „Geldbeträge“ (Ausgabefunktion
99F:02D8). Zusatz „Zahlenausgabe Datei 0x0B954 kennt einen zweiten Zweig mit Komma als Tausendertrenner bei [0x79D] = 4“ am Original
nachvollzogen: `dd if=em.exe bs=1 skip=$((0xB954)) count=130 | ndisasm -b 16 -o 0 -` zeigt bei +0x2F: `[0x38d]` = 0 und
Stelle 3/7/11 → `cmp [0x79f],0` (jnz) und `cmp [0x79d],4`; Treffer → AX = 1, `shl al,1`, `mov dl,0x2E; sub dl,al` = 0x2C = „,“,
sonst „.“. (Die Doku Z. 454 ergänzt korrekt „[0x79F] = 0“; der Eintrag A 2 lässt diese Bedingung weg – Kürzung, kein Widerspruch.)
Aussage „Remake führt bis v1.0 ganze DM (A040, A122, A124, A227, A501)“ deckt sich mit N1 und `restliste-v1.md` Z. 75. ✓

**Funktionskarte** (`docs/re-abdeckung.md`, nur Zählerzeilen): Generator am Head gegen die Original-`em.exe` (387968 Byte) neu
ausgeführt: `python3 -I tools/reverse-engineering/funktionskarte.py <em.exe> --repo .` (ohne `--write`, Rückgabe 0). Ergebnis
identisch zum PR: „679 Funktionen … 8614 Zuordnungen“; dokumentiert 121 / 22088 B, unbekannt 235 / 25681 B; Basis 0x0 dokumentiert
14 %, unbekannt 70 %. Rechenprobe: 318 + 5 + 121 + 235 = 679; 259856 + 1900 + 22088 + 25681 = 309525. ✓ Der Block ist damit vom
Generator erzeugt, nicht von Hand geändert.

**Nicht ausgeführt:** `tools/pruefen.sh` (kein Code im Diff, nach Auftrag), kein Referenzbildvergleich. `[APPROX]`: keine neue
oder geänderte Markierung im Code; die 8 Treffer von `git diff 72d3a98 83bb87b | grep '^+.*APPROX'` stehen alle im verschobenen
Archivtext (`docs/archiv/runde-20.md`, per `awk` nach Datei gezählt). Akzeptierte `[APPROX]`: keine neu.

**Annahmen (nicht geprüft):** Inhalt der Optionen R20-D1…D4 ist nur im PR-Text von #878 belegt; hier nur die Entscheidungsbuchstaben
gegen Plan-Review und Archiv abgeglichen. Dass #872 beim Plan-Merge geschlossen wurde, ist aus den Event-Zeiten abgeleitet.
