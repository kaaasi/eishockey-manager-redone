# Review PR #713 – Gesamtreview risikoorientiert (Prozessdoku)

Reviewer: Claude Code (Claude Opus 5.5), geplanter Lauf 10.11.2026, unbeaufsichtigt. Reine Doku-Prüfung (Vorgabe Kurzaufruf):
kein Build, CI entfällt. Base `d0dd488c34a135410528bc621e27cecf2441e806` · Head `cbf8c603fc5c0bb824a5d8fe6c861eabd49631c8`.

## 1. Merge-Empfehlung

**Ja** für Head `cbf8c60`. Der neue Abschnitt deckt alle Vorgaben der PR-Beschreibung ab; keine Widersprüche zu
`zusammenarbeit.md` §5/§6. Verbleibende Stellen mit „Vollprüfung“ sind historisch (Runde 14) bzw. betreffen das Erstreview;
zwei Klarstellungen als P3 (nicht mergeblockierend). `main` ist seit Base nur um eine Review-Datei gewachsen (#710) – kein Konflikt.

## 2. Befunde

**P3-1 (Lesbarkeit, `docs/arbeitspakete.md` Z. 189):** „Vollprüfung durch ChatGPT … nach `vorlagen/review-anfrage.md`, Abschnitt
„Gesamtreview am Rundenende““ verweist nun auf einen Abschnitt, der ausdrücklich *keine* Vollprüfung mehr beschreibt. Die Folgezeilen
(Stand/Entscheidung C) lösen das auf, der erste Satz liest sich aber widersprüchlich. Vorschlag: „Ursprünglich Vollprüfung (E14-3) …“.
Beleg: `git grep -n -i vollprüfung cbf8c60 -- docs/arbeitspakete.md` → Z. 189, 191, 193, 221.

**P3-2 (Lesbarkeit, `docs/arbeitspakete.md` Z. 221):** „E14-3 = A: Gesamtreview … als Vollprüfung“ ist durch Entscheidung C
(10.11.2026) abgelöst, aber nicht als abgelöst markiert. Vorschlag: Zusatz „(abgelöst durch Entscheidung C, 10.11.2026)“.

**P3-3 (Abgrenzung, `docs/vorlagen/review-anfrage.md` Schritt 3, Z. 41–42):** Der allgemeine Ablauf verlangt weiter „eine Gegenprobe
pro neuem Test, wo es geht“; der Gesamtreview-Absatz sagt „keine Mutation jedes Tests“. Da der Absatz hinter „Besonders prüfen:“
eingesetzt wird und Schritt 3 „wo es geht“ einschränkt, ist die Lesart eindeutig genug; ein Halbsatz „(Gesamtreview: siehe unten)“
in Schritt 3 würde Missverständnisse beim Reviewer vermeiden. Ebenso nennt Schritt 2 für das Gesamtreview keinen Prüflauf
(nur Erstreview/Nachprüfung) – vorbestehend, nicht durch #713 verursacht.

Keine P0–P2.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `git diff --stat d0dd488 cbf8c60` | nur `docs/arbeitspakete.md`, `docs/prozess/zusammenarbeit.md`, `docs/vorlagen/review-anfrage.md` → CI entfällt, nur Doku |
| `git merge-base cbf8c60 origin/main` | = Base; `d0dd488..origin/main` ändert nur `docs/reviews/2026-11-09-runde-14-gesamt-teil5.md` |
| Vorgaben (1) gegen neuen Abschnitt | Ziel Originaltreue + konkrete Fehler ✓; Laden/Speichern, Tages-/Saisonwechsel, Finanzen, Wettbewerbe, Kader, Bedienung ✓; Originalstände/Bilder/em.exe gezielt ✓; Gegenbeispiele erneut ✓; keine Vollmutation/Einzelrückführung ✓; Testlücke getrennt von Produktfehler ✓; `[APPROX]`/erwartete Issues nur bestätigen ✓; Merge-Empfehlung nach Befunden, Ungeprüftes nicht automatisch „Nein“ ✓; Vollprüfung einmal als Abschlussprüfung (vor v1.0) ✓ |
| `git grep -n -i vollprüfung cbf8c60` (ohne `docs/archiv`, `docs/reviews`) | `arbeitspakete.md` 189/191/193/221 (Runde 14, historisch → P3-1/2); `zusammenarbeit.md` 90 (Nachprüfung, „keine erneute Vollprüfung“ – passt), 97 (neu, passt) |
| Reste der alten Regel (`offener Prüfbereich`, `Rückmutation`, `Stichprobe`) in `docs/vorlagen`, `docs/prozess`, `AGENTS.md`, `CLAUDE.md` | nur `zusammenarbeit.md` 90 (Nachprüfung, unabhängig); keine Regel „offener Prüfbereich = Nein“ mehr |
| `zusammenarbeit.md` §5 Nr. 1–9 und §6 | kein Widerspruch: Erstreview bleibt vollständig (§5.1), Gesamtreview risikoorientiert (§5.6), Länge 120 Zeilen je Teil (§5.9) = Absatz; §6 Nr. 5 (P0–P2 behoben) unabhängig vom Zuschnitt |
| `leitstelle-start.md` | keine Vollprüfungs-Vorgabe für das Gesamtreview (nur Z. 42 Zeitpunkt Rundenende) |
| Erwartet offen #712 | bestätigt offen (Label `P3`), nicht als neuer Befund gezählt |

Akzeptiert ohne eigenen Befund: Für die „Abschlussprüfung“ gibt es noch keinen Ablauf/Kurzaufruf; laut PR erst vor v1.0 nötig.
Annahme: Wortlaut der Nutzervorgabe nur aus der PR-Beschreibung bekannt (Nachricht aus ChatGPT liegt nicht im Repo).
