# Review PR #993: Abschluss Runde 24 (nur Doku)

Base `c68e6aa` · Head `83317fa` (geprüft: `git rev-parse pr993` = Head, Checkout ohne Änderungen) · Reviewer: Claude (Sonnet 5.5),
geplanter Lauf, keine Mitarbeit am PR. Kein Build und keine `tools/pruefen.sh`-Läufe (nur Doku, Auftrag „Kein Build nötig“).

## 1. Merge-Empfehlung

**Ja** für `83317fa`. Kein P0–P2. Vier P3 (Klarheit der Doku), die der Merge nicht aufhalten muss. Der Beleg ist Dateilesen und
Skript-Probe, kein Testlauf (nur Doku).

## 2. Befunde

Alle vier P3 sind Beobachtungen am Text; kein Befund verändert Spielverhalten.

- **P3-1 · Nummernkollision K11/K13 (auch K2/K9 gegen K02/K09).** `abweichungen-v1.md` Abschnitt 0 nennt K11 und K13 zweimal:
  Zeile 20–21 als Aufräumbefunde (Rollenwert +0x18, Platzwahl 0x29C3B; `aufraeumen-v1.md` §1.5 Zeilen 122, 124) mit Ziel Runde 25,
  Zeile 22 „ins Aufräumen #936: K04, K11, K13“ als Abweichungsthemen (Transfer-Abläufe, Vertragsdialog Gehalt 0). Beide sind
  gemeint, aber ohne Präfix liest sich das als Widerspruch. Gleiches in `arbeitspakete.md` („aus dem Aufräumkonzept K2, K9, K11,
  K13“) neben den Themen K02/K09 der Abweichungsliste. Beleg: `grep -n 'K11\|K13' docs/analyse/abweichungen-v1.md | head -4`.
  Vorschlag: „Aufräumkonzept K11“ gegen „Thema K11“ ausschreiben.
- **P3-2 · `abweichungen-v1.md` Abschnitt 1 und 3 zeigen die Entscheidung nicht selbst.** Spalte „Entscheidung“ in Abschnitt 1 ist für
  alle 15 Zeilen leer; B11, B31, K07, S01, S05, S07, T03 stehen in Abschnitt 3 weiter mit Vorschlag „bleibt“. Gedeckt nur durch den
  Satz „Was hier nicht steht, gilt nach Vorschlag“ in Abschnitt 0; der Widerspruch ist damit gewollt, aber nur dort aufgelöst.
- **P3-3 · Modellnamen im Archiv nicht belegt.** `archiv/runde-24.md` nennt „GPT-6.2 Astra“ und „Gemini 4 Hydrogen“. Im Repo
  kein Beleg (`grep -rn 'Astra\|Hydrogen' docs` trifft nur `archiv/runde-7.md`; PR #992 nennt nur „ChatGPT“). Verdacht, vom Nutzer
  stammend möglich; Quelle prüfen oder „ChatGPT“/„Gemini“ schreiben.
- **P3-4 · Auftrag Runde 25 verschweigt die sieben „ins Aufräumen“-Themen.** K04, K11, K13, L11, L12, C02, C08 stehen nur in
  Abschnitt 0 (Zeile 22), nicht im Kopf von `arbeitspakete.md`; dort heißt es nur „A1–A6 entschieden, mit #978, #980“. Nichts
  geht verloren (Abschnitt 0 ist verlinkt), aber der Aufräum-Lauf muss dort nachlesen.

## 3. Ausgeführte Prüfungen

| Prüfung | Befehl / Probe | Ergebnis |
|---|---|---|
| Nur Doku | `gh api repos/…/compare/c68e6aa...83317fa` | 6 Dateien, alle `docs/`; kein `src/`, `tests/`, `tools/`, `.github/` → CI entfällt zu Recht |
| (1) Archiv wörtlich | Python: Rundenabschnitt aus `git show c68e6aa:docs/arbeitspakete.md` (Überschrift bis „### Zurückgestellt“, 4589 Zeichen) als Teilstring von `docs/archiv/runde-24.md` | Teilstring **nicht** gefunden; zeilenweise Prüfung: genau eine Zeile weicht ab, die Überschrift („Aktuelle Runde (archiviert): …“), im Kopf der Datei so ausgewiesen → wörtlich bis auf Überschrift |
| (1) PRs #983–#993 | `gh api …/pulls/<nr>` | #983–#992 gemergt, #993 offen; Titel passen zu „Ergebnis“ (Plan #983, Plan-Review #985, Prozess #984/#986/#989, Spuren #987/#988, Erstreviews #991/#990, #992); #990 „Empfehlung nein“, #991 Befunde P2-1…P2-5 laut `docs/reviews/2026-11-23-abweichungsliste.md` (P2-1, P2-2 gelesen) |
| (1) Entscheidungen | Archiv „Entscheidungen“ gegen Abschnitt 0 und `aufraeumen-v1.md` §3 | gleich (B31 = A, B11 = A, B16 = A, F06/F08 = A, A1 = B, A2 = A, A3 = B, A4 = A, A5 = A, A6 = A) |
| (2) Themen-Nummern | Python: Nr.-Spalte der Claude-Tabelle gegen Nr.-Spalte `abweichungen-v1.md` | 109 = 109, keine fehlende, keine überzählige |
| (2) Klassen gegen Abschnitt 0 | Python: Klassen N/S/O der Claude-Tabelle gegen „ändern“ (15 + 7) | Alle 15 Themen aus Abschnitt 1 sind N/S/O; alle 6 N-Themen sind die Zusatzthemen; kein K-Thema steht unter „ändern“; N/S/O außerhalb von „ändern“: F06, F08 (prüfen), K04, K11, K13, L11, L12, C02, C08 (Aufräumen) = genau Abschnitt 0 |
| (2) Zählung | Python: Klassen der Claude-Spalte; Spalten ChatGPT/Gemini | Claude N 6, S 15, O 10, K 71, D 7 = Kopfzeile; ChatGPT 16/33/15/45 und Gemini 9/10/11/79 = deren Gesamtzeilen |
| (2) Prüfungen B16/F06/F08, D06 | Claude-Tabelle gegen Abschnitt 0 | B16 Klasse K mit Hinweis auf Frage 1, Abschnitt „Entscheidungen zu den Fragen“ = Abschnitt 0 (A); F06/F08 Klasse S = „prüfen“; D06 K = „bleibt“ |
| (3) A1–A6 | `aufraeumen-v1.md` §3 gegen die Empfehlungen | „jeweils die Empfehlung“ stimmt: A1 B, A2 A, A3 B, A4 A, A5 A (mit Archiv-Zusatz), A6 A |
| (4) Kopf Runde 25 | `git diff c68e6aa pr993 -- docs/arbeitspakete.md` | Entfernt nur Runde-24-Abschnitt und die R20-D3-Zeile (dort als „entschieden“ ersetzt); „Zurückgestellt“ und alle Vormerkungen (DOSBox-Sitzungen, D 26, #976/#978/#980, #936, #848, N6, nach v1.0) unverändert; Restliste-Stand und Modellwahl erhalten |
| Verweise | Python: relative Links aller sechs Dateien | alle Ziele existieren im Head |

Der Auftrag nennt „Beleg aus em.exe vor der Änderung“ für B12, B13, B16, B28, B29, S02; die Tabelle in Abschnitt 1 verlangt dies
für genau diese Themen (B16 über die Prüfung), B18 und C07 nennen em.exe ebenfalls („klärbar“, „lesbar“) – Abweichung unkritisch.

**Akzeptierte `[APPROX]`:** keine betroffen (kein Code). **Erwartet offen, bestätigt:** D 26 (DOSBox, Vormerkung), #936, #978, #980
(Aufräumen nach Runde 25, „Zurückgestellt“/„Vormerkungen“). **Annahmen, nicht geprüft:** Inhalt der Bewertungen selbst (Klasse je
Thema gegen Code), Modellnamen (P3-3), Inhalt der 109 Themen gegen em.exe (nicht Teil des Auftrags).
