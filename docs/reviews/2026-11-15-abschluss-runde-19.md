# Review PR #875: Abschluss Runde 19 (Archiv, Kopf Runde 20, Funktionskarte, Inventur)

Claude (Sonnet 5.5), unabhängige, unbeaufsichtigte Sitzung ohne Mitarbeit an PR #875 · Base `cde015cae049a7ababc8108ef0ee28b57b6da27d` ·
Head `2f04db7fe5a6f17f0f69deefd5c1f8902ad35c45` · 15.11.2026 · Reine Doku-Prüfung, kein Build (CI startet nicht, nur `docs/**`).

## 1. Merge-Empfehlung

**Ja für genau `2f04db7`.** Der Archivinhalt ist wörtlich, Ergebnis, Abschluss und Entscheidungen stimmen mit GitHub und den
Reviews überein; offen sind nur Doku-P3 (Zählerzeile der Funktionskarte, Klammerfehler in A093, #847 noch offen). Keine davon
blockiert; Aussage gilt als „nur Doku/Codelesen“, kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4 (nicht nötig, kein Code).

## 2. Befunde

**P3-1 – Zählerzeile der Funktionskarte ist nicht der Generatorwert des Heads** ([`docs/re-abdeckung.md` Z. 155](https://github.com/kaaasi/eishockey-manager-redone/blob/2f04db7fe5a6f17f0f69deefd5c1f8902ad35c45/docs/re-abdeckung.md#L155)).
Im Head steht „8013 Zuordnungen“, die PR-Beschreibung nennt „7988 → 7989“, der Generator liefert am Head **7995**. Beleg:
`python3 -I tools/reverse-engineering/funktionskarte.py <em.exe aus Referenz-Repo> --repo .` am ausgecheckten Head → 7995
(sonst nur die Marker-Zeile `END generiert` verschieden; Tabellen unverändert). Am Base-Stand erzeugt derselbe Aufruf 8013, die
Base trägt 7988: der Wert im Head ist also der Base-Stand, erzeugt **vor** dem Verschieben des Rundenabschnitts in
`docs/archiv/` (der von der Zählung ausgenommen ist). Folge: Zahl in der Datei, in der PR-Beschreibung und im Generator
passen nicht zusammen; Prozentwerte und Funktionszahlen sind richtig. Vorschlag: `funktionskarte.py --write` nach dem
Verschieben erneut ausführen (→ 7995) und die Zahl bei der nächsten Inventur („7988 → …“, `inventur.md` Z. 387) nachziehen.

**P3-2 – Klammerfehler in Inventur A093** ([`docs/analyse/inventur.md` Z. 495](https://github.com/kaaasi/eishockey-manager-redone/blob/2f04db7fe5a6f17f0f69deefd5c1f8902ad35c45/docs/analyse/inventur.md#L495)).
Aus `…offen.**` (Stand Runde 18) wurde `…offen. **Stand Runde 19 …offen.**`: das schließende `**` des ersten Stands fehlt, die
Hervorhebung bricht in der Darstellung. Beleg: `git diff cde015c 2f04db7 -- docs/analyse/inventur.md` (Zeile 495; Vergleich A409 Z. 685,
dort korrekt `.** **Stand …`). Inhalt stimmt mit `abgleich-dosbox.md` §4.4/§4.5 („auch in `runde-19` … nicht belegbar, bleibt offen“) überein.

**P3-3 – #847 steht auf GitHub noch offen, das Archiv nennt es „behoben bzw. geschlossen“** ([`docs/archiv/runde-19.md` Z. 13–14](https://github.com/kaaasi/eishockey-manager-redone/blob/2f04db7fe5a6f17f0f69deefd5c1f8902ad35c45/docs/archiv/runde-19.md#L13)).
Beleg: GitHub `list_issues`: #847 OPEN (zuletzt 14.11.2026); #826, #838, #857, #859, #862, #730 CLOSED. Kopf Runde 20 und
„Vormerkungen“ nennen #847 korrekt nicht als offen. Vorschlag: #847 mit Verweis auf #854/#866 schließen (Leitstelle). Die
Bewertung „behoben“ trägt die Gesamtreview (#873: FILE64→65 unter `AbgleichDosboxNeuerManagerTests`).

## 3. Geprüft (alle ohne weiteren Befund)

- **Archivprobe (Gegenbeispiel):** Abschnitt „## Aktuelle Runde: Runde 19“ bis vor „### Zurückgestellt“ aus `git show cde015c:docs/arbeitspakete.md`
  (134 Zeilen) ist wörtlich in `docs/archiv/runde-19.md` enthalten (Python-`in`-Vergleich: **True**). Neu sind nur Überschrift, Ergebnis,
  Abschluss, Entscheidungen. `git diff --stat cde015c 2f04db7`: 4 Dateien (`docs/analyse/inventur.md` 4, `docs/arbeitspakete.md` 150,
  `docs/archiv/runde-19.md` 162 neu, `docs/re-abdeckung.md` 2), 174 +, 144 −; nichts außerhalb `docs/`.
- **Ergebnis gegen GitHub:** #854 (14.11.), #855, #856 (15.11. 15:24), #863, #866 gemergt; Plan #851 mit Plan-Review #852 (Ja);
  Entscheidungseinträge #853 (R19-E1), #865 (E3), #868 (E4) gemergt; Mergefolge `torszenen` (15:23:40) vor `re-belastung` (15:24:11)
  wie im Archiv, abweichend vom Plan (`neuer-manager` → `re-belastung` → `torszenen`) und dort begründet. Gesamtreview #873 gemergt,
  Datei `docs/reviews/2026-11-15-runde-19-gesamt.md`: „Ja für `c4fe55a`“, nur Testlücke Bestenliste → #874. Reviews vom 14.–15.11. in
  `docs/reviews/` (11 Dateien) widersprechen dem Archiv nicht. #838 (⅓ s bestätigt) und #730 („im Bereich“, `abgleich-dosbox.md` §4.5) CLOSED.
- **Entscheidungen** R19-D1 = A, D2 = C, D3 = B (→ A bei Fehlschlag), E1 = A (Wiederholung ohne neuen Zug), E2 = A (Liste `runde-19`),
  E3 = A (Spur `abgleich-dosbox-19`), E4 = A (Gegenprobe FILE66–70): deckungsgleich mit dem wörtlich übernommenen Rundenabschnitt,
  den PR-Titeln #853/#865/#868 und dem Gesamtreview. „Läufe vom 14.11. nach Nutzungslimit neu“ ist aus GitHub nicht prüfbar (Annahme).
- **Erwartet offen (bestätigt):** #871 OPEN (P3 aus R870: pyc, Drift-Wächter ZIMMERMANN, Teamwert-Rechnung, Bezugsbasis), #872 OPEN
  (Reihe auf dem Eis bei Spielbeginn), #874 OPEN (Testlücke Bestenliste), A093 offen (`abgleich-dosbox.md` §4.4 und §4.5 Schluss). Die
  offenen Aufnahme-Issues #864/#869 sind reine Ablagevermerke und stehen nicht im Kopf (unkritisch).
- **Kopf Runde 20 / „Zurückgestellt“ / „Vormerkungen“:** Kopf verweist auf `archiv/runde-19.md` (Datei vorhanden); „Zurückgestellt“ nennt A093
  mit Grund (§4.4: ohne Klick spielt niemand außerhalb seiner Reihe, Marker `MatchEngine.Managed.cs` Z. 189 weiter `[APPROX]`) und R8-A01;
  der alte Punkt #838/#730-Aufnahme entfällt zu Recht (beide erledigt); Vormerkung „Testlücke Gesamtreview #848“ und „Nach v1.0“ bleiben.
- **Inventur A409** („Stand Runde 19“ #866): deckt sich mit `abgleich-dosbox.md` §4.4/§4.5 (Reihenwahl Spieltag 1–3 belegt, Test
  `AbgleichDosboxLinesTests` existiert; Trefferrate nach FILE66–70 im Bereich, #730 geschlossen). Der Bericht-Vorschlagstext in §4.4
  („#730 offen“) ist durch den Nachtrag §4.5 überholt; die Inventurzeile übernimmt den späteren Stand zu Recht.

## 4. Ausgeführte Prüfungen und Annahmen

- `git fetch`, `git rev-parse FETCH_HEAD` = `2f04db7fe5a6f17f0f69deefd5c1f8902ad35c45`, `git status --porcelain` leer; `git diff --stat cde015c FETCH_HEAD` (s. o.).
- Archivprobe und Generatorlauf wie oben (`ndisasm` vorhanden, `em.exe` aus dem Referenz-Repo); GitHub-Stand über PR- und Issue-Listen.
- Kein Build und kein `tools/pruefen.sh` (nur Doku, Auftrag). **Akzeptierte `[APPROX]`:** keine neuen. Annahme: Zählung der Funktionskarte
  ist durch den Generator bestimmt (nicht durch eine Testschwelle; Test auf die Zahl nicht gesucht).
