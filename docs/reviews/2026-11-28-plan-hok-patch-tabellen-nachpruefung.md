# Nachprüfung Plan-PR #1073 (hok-patch-tabellen, abgleich-dosbox-22b)

Reviewer: Claude Sonnet 5.5 (frische Sitzung, geplanter Lauf, keine Mitarbeit am PR) · Vorreview:
`docs/reviews/2026-11-28-plan-hok-patch-tabellen.md` (geprüft `e230199`) · Base `8fa9aaf` · Head `7e2fb55`
Prüfumfang: `git diff 735eb06 7e2fb55` (nur `docs/arbeitspakete.md`, +36/−6) und die genannten Befunde.

## 1. Merge-Empfehlung

**Ja** für Head `7e2fb55` (nur Doku). P2-1, P2-2, P3-1 sind behoben. Neu nur P3, kein Blocker. Checkout: `git rev-parse HEAD` =
`7e2fb55d22d0…`, `git status --porcelain` leer.

## 2. Gegenbeispiele erneut ausgeführt

| ID | Ablauf | Ergebnis |
|---|---|---|
| P2-1 | `git diff 735eb06 7e2fb55`, Teil 1 und Teil 3 von `hok-patch-tabellen` lesen | **behoben.** Teil 1 legt die Vorlage fest (Stand um 13.–29. April mit Aufstiegsrunde und zwei Bundesliga-Managern, sonst Spielverlauf), nennt „FILE75 ist ein Spielstart ohne Ergebnisse“ und die Pflichtblöcke `A0CB`, `07A9`, `D0E6`, Statusbits 0x28; Teil 3 sagt „Vorlage aus Teil 1“. |
| P2-2 | `git merge-base --is-ancestor 411c9e8 7e2fb55` → Rückgabe 0 | **behoben.** Folge: „alle Teile erst nach Merge von `aufraeumen-doku`“ (gemergt, #1076, in Base enthalten); Teil 1 vermerkt `funktionskarte.py <em.exe> --csv` vor/nach. |
| P3-1 | `grep -n 55F17 docs/arbeitspakete.md docs/reverse-engineering.md` | **behoben:** Plan Z. 173 nennt 0x55F17–0x55F87, wie `reverse-engineering.md` Z. 946. Berichtigung zu meinem Vorreview: „0x55F22 trifft nur den Plan“ war falsch – 0x55F22 ist die Vergleichsstelle (Archiv `re-saisonwechsel.md` Z. 66, `restliste-v1-klassen.csv` A662); kein Widerspruch. Gegen `em.exe` nicht neu gelesen (keine Originaldaten in diesem Lauf). |

## 3. Besonders geprüft: Spur `abgleich-dosbox-22b` gegen A22-D1 = C

Passt zum PR-Text (Option C: zweiteilig, Bericht, dann Klassen nach `aufraeumen-doku`; Abweichungen nur als Issues für Runde 28).

| Punkt | Befund |
|---|---|
| Teile / Folge | Teil 1 ohne Klassenliste und Produktcode, V nur als Issue (`befund`, Runde 28); Teil 2 nach Teil 1 und vor `hok-patch-tabellen` Teil 3. Keine Zyklen. Länge 25 Zeilen (Grenze 60). |
| IDs und Stände | Gegen README des Referenz-Repos (`original/spielstaende/README.md` Z. 118–119, 462–479) und Referenz-PR #41/#42: FILE80/81 → A408, A411, A918 (Nutzerangabe „FILE91 entfällt“); FILE89/90 → A899 (Bezug #939); A599 Bilder; A707 ohne Stand (FILE82/83 nicht aufgenommen); A259 und A671 ohne Aufnahme. Stimmt mit dem Plan. `dosbox-aufnahme.md` nennt dagegen noch FILE90/91 für A918 – genau das zieht Teil 2 nach. |
| Klassen Sitzung 1 | Plan: A057, A065, A117, A402, A427, A613, A614, A629-Export. `archiv/analyse/abgleich-dosbox-22.md` Z. 15–22: A427 K, A065 V, A057 X, A402 V, A613 X, A614 V, A629 D+V, A117 K. Alle acht stehen in `restliste-v1-klassen.csv` noch als `D` (Z. 18, 25, 47, 191, 213, 381, 382, 396) → „noch als D geführt“ stimmt. Nachtragszeilen A962–A973 existieren schon als K, die Ursprungszeilen nicht. |
| Vorlage-Streichungen | A259 (Sitzung 3 Nr. 3) D → bleibt Teil 1 (Lesung 0x46E04–0x46FCF); A910 steht als `X` (CSV Z. 667) → Streichung Sitzung 7 Nr. 5 gedeckt; S04-Fotos in Live-Zusatz Sitzung 2 (Nr. 3, Runde 26) ist vorhanden. |
| A671 in `hok-patch-tabellen` Teil 3 | FILE92/93 und „Sitzung 7 Nr. 6 Teil 2“ stimmen mit `dosbox-aufnahme.md` Z. 267–269; Bedingung „wenn Teil 1 die Felder belegt“ ist richtig gesetzt. Stand der Restliste: A671 `D`, Rest nur Modus 1 (CSV Z. 438). |

## 4. Befunde (alle P3, beheben nicht blockierend)

**P3-1 – Dateigrenze `dosbox-aufnahme.md` nicht bereinigt.** `hok-patch-tabellen` „Dateien“ erlaubt dort „nur Sitzung 5“
(`arbeitspakete.md` Z. 200), Teil 3 ändert aber auch Sitzung 7 Nr. 6 Teil 2 (A671, Z. 194). `abgleich-dosbox-22b` Teil 2 nimmt dieselbe
Nr. 6 (A918/FILE91 entfällt) mit. Reihenfolge ist festgelegt, die Grenze nicht. Beleg: `grep -n "Sitzung 7 Nr. 6\|nur Sitzung 5" docs/arbeitspakete.md`.
Vorschlag: bei `hok-patch-tabellen` „Sitzung 5 und Sitzung 7 Nr. 6 Teil 2“, bei `abgleich-dosbox-22b` „außer Sitzung 5 und Nr. 6 Teil 2“.

**P3-2 – `markerliste.py --write` gegen geltende Regel.** Plan Z. 51–52: „Kein `markerliste.py --write` außer in `aufraeumen-doku`“.
`abgleich-dosbox-22b` Teil 2 verlangt es einmal; die Ausnahme steht nur im Spurabschnitt. Vorschlag: Regel Z. 51 um
„und `abgleich-dosbox-22b` Teil 2 (A22-D1 = C)“ ergänzen.

**P3-3 – Pfad `EM.EXE`.** Teil 1 nennt `~/referenz/original/gamedata/EM.EXE`; die Doku schreibt `em.exe` (`docs/originaldateien.md` Z. 10,
44). Auf Linux unterscheidet das Dateisystem Groß-/Kleinschreibung. Vorschlag: `em.exe`.

**P3-4 – Parallelität der beiden Teil-1-Läufe.** Beide neuen Analysedateien enthalten em.exe-Adressen und ändern die
`funktionskarte.py`-Statusverteilung (Anlass von P2-2). „Parallel“ ohne Reihenfolge lässt das „vor“ der zweiten Spur veralten.
Vorschlag: je Spur „vor/nach gegen den Stand beim Merge“ oder Reihenfolge festlegen.

**P3-5 – A671-Vorlage unbenannt.** Der Stand braucht Modus „nur Weltkugel“; `hok patch` hat dafür kein Feld (`OriginalSaveGamePatcher.cs`
kennt `konto` …). Teil 3 nennt keine Vorlage (FILE18-Reihe naheliegend). Teil 1 soll das mit festlegen.

## 5. Ausgeführte Prüfungen

- `git diff 735eb06 7e2fb55`, `git diff --stat 8fa9aaf 7e2fb55` (1 Datei), Greps und Ancestor-Probe wie oben; README des
  Referenz-Repos und PR #41/#42 per `gh api` gelesen.
- `tools/pruefen.sh` nicht ausgeführt: Plan nennt „Tests: entfällt, nur Doku“, Diff berührt nur `docs/arbeitspakete.md`; kein Originaldaten-Lauf.
- Akzeptierte `[APPROX]`: keine. Annahmen (nicht geprüft): Inhalt von em.exe (Tabellenblöcke, 0x55F17), Lage der Dateien im
  Referenz-Repo über die README hinaus, Ablauf der Wellen.
