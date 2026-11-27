# Nachprüfung: Plan Runde 10 – Sonderrunde Analyse em.exe (PR #366)

- Nachprüfung von [2026-11-01-plan-runde-10.md](2026-11-01-plan-runde-10.md) (#367, geprüfter Head `89e53c6`). Delta
  `89e53c6..d6810bc` (ein Commit) und die Befunde R10P-01 bis -05 (#368–#372).
- Base `29f306267093c688b064bae5c997b27ce2ad4534` · Head `d6810bcc52315dbafa6f7ab3c3e5d6ebc107258c` · Reviewer: Claude
  Opus 5.5 (eigene Sitzung, am PR und am Review #367 nicht beteiligt).
- Checkout per Klon und `git fetch origin pull/366/head`; `git rev-parse HEAD` = Head, `git status --porcelain` leer. Base,
  alter und neuer Head zusätzlich als Worktrees für die Funktionskarte.

Links beziehen sich auf den Head:
`https://github.com/kaaasi/eishockey-manager-redone/blob/d6810bcc52315dbafa6f7ab3c3e5d6ebc107258c/<pfad>#L<n>`.

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** Das Gegenbeispiel R10P-01 ist behoben: Die Karte ist an Base und Head identisch (0 Statuswechsel,
vorher 15). Die neue Abnahme greift nachweislich (Probedatei unter `docs/analyse/` erscheint in der Spalte `fundstellen`).
R10P-02 bis -04 sind behoben, R10P-05 für den genannten Link; ein weiterer defekter Link im selben Archiv bleibt (P3,
R10PN-01, kein Merge-Hindernis). CI am Head grün, `tools/pruefen.sh` (alle drei Läufe) grün.

## 2. Befunde

### Stand der Befunde aus #367

| ID | Issue | Stand am Head | Nachweis |
|---|---|---|---|
| R10P-01 (P1) | #368 | behoben | Gegenbeispiel wiederholt (unten); Schreibweise `…h` ([L18](https://github.com/kaaasi/eishockey-manager-redone/blob/d6810bcc52315dbafa6f7ab3c3e5d6ebc107258c/docs/arbeitspakete.md#L18)), Abnahme über die Spalte `fundstellen` ([L46](https://github.com/kaaasi/eishockey-manager-redone/blob/d6810bcc52315dbafa6f7ab3c3e5d6ebc107258c/docs/arbeitspakete.md#L46)), Kartenstand als Base-Stand gekennzeichnet |
| R10P-02 (P2) | #369 | behoben | wörtlicher Startaufruf ([L29–37](https://github.com/kaaasi/eishockey-manager-redone/blob/d6810bcc52315dbafa6f7ab3c3e5d6ebc107258c/docs/arbeitspakete.md#L29-L37)): ohne Autorisierung für `create_trigger`, Branch `paket/<name>`, Selbstprüfung statt `--schnell`, „Teile und Fortsetzung“ entfällt, Rebase (Hauptablauf Schritt 5) bleibt – passt zu `spur-start.md` |
| R10P-03 (P3) | #370 | behoben | Fensterbasis als Liste: die 16 Adressen stimmen exakt mit den unbekannten Funktionen von Basis 0x902 (5) und 0x99F (11) an Base überein; 0x562C6 als Stelle in 0x54C9F; Sonderfall „innerhalb einer Funktion / genähert“ in der Abnahme |
| R10P-04 (P3) | #371 | behoben | die 19 Adressen stimmen exakt mit den unbekannten Funktionen von Ov 0x130 an Base überein; 0x54C9F/0x562C6 bei `re-saisonwechsel` |
| R10P-05 (P3) | #372 | behoben (genannter Link) | `runde-8.md` und `runde-9.md` L14–15 zeigen auf existierende Dateien; siehe R10PN-01 |

**Gegenbeispiel R10P-01 wiederholt** (`--csv`, Spalten `status` und `fundstellen` verglichen):

- Base `29f3062` gegen alten Head `89e53c6`: 15 Funktionen unbekannt → dokumentiert (dieselben 15 wie in #367) – das
  Gegenbeispiel ist reproduzierbar.
- Base `29f3062` gegen Head `d6810bc`: **0 Statuswechsel**; Bytes je Status identisch. `docs/arbeitspakete.md` kommt am Head
  in keiner Fundstelle vor (an Base 30 Funktionen mit Fundstelle aus dem Runde-9-Plan, ohne Statuswirkung).
- Probe der neuen Abnahme (temporäre Kopie des Heads, nicht im Repo): `docs/analyse/re-geld.md` mit „Dateioffsets“-Kopf und
  Listenpunkten. 0x3D45F → dokumentiert mit Fundstelle `docs/analyse/re-geld.md`; 0x562C6 erscheint als Fundstelle bei
  0x54C9F (Status bleibt „umgesetzt“); 0x39DEC erhält die Fundstelle, bleibt „genähert“ – genau wie die Abnahme es beschreibt.

### R10PN-01 (P3) – Weiterer defekter relativer Link im Archiv

[`docs/archiv/runde-9.md` L31](https://github.com/kaaasi/eishockey-manager-redone/blob/d6810bcc52315dbafa6f7ab3c3e5d6ebc107258c/docs/archiv/runde-9.md#L31):
`[vorlagen/spur-start.md](vorlagen/spur-start.md)` zeigt aus `docs/archiv/` auf `docs/archiv/vorlagen/spur-start.md` (fehlt).
Schon am alten Head vorhanden, in #367 übersehen. Gegenbeispiel: alle relativen Links in `docs/arbeitspakete.md` und
`docs/archiv/runde-{8,9}.md` gegen das Dateisystem auflösen – einziger Fehltreffer ist dieser. Vorschlag:
`../vorlagen/spur-start.md`. Dasselbe Muster `](archiv/…)` steht außerhalb des PR noch in `runde-4.md` L14 und `runde-5.md`
L13–14 (bei Gelegenheit mitziehen).

### Hinweis (kein Befund)

Die Karte erkennt eine Adresse nicht, auf die direkt ein Doppelpunkt folgt (`0x57ECF:` – Muster `BARE` schließt `:`
dahinter aus, um `Segment:Offset` nicht zu zerreißen). In der Probe blieb `- Kopfzeile 0x57ECF: …` ohne Fundstelle. Die
Selbstprüfung der Abnahme deckt das auf; ein Satz im Startaufruf oder in „Inhalt je Fundstelle“ („nach einer Adresse kein
Doppelpunkt“) würde den Spuren einen Korrekturlauf ersparen.

## 3. Ausgeführte Prüfungen

```
git clone … && git fetch origin pull/366/head:pr366        # rev-parse = d6810bc, status leer
git worktree add <scratch>/{base,r367,head} {29f3062,89e53c6,d6810bc}
tools/claude/fetch-reference.sh original
python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --repo <worktree> --csv <scratch>/<stand>.csv
tools/claude/setup-workspace.sh ~/referenz/original/gamedata
tools/pruefen.sh                                            # am Head d6810bc
```

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1631 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1351 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1351 | 280 | 0 |

Commit d6810bcc52315dbafa6f7ab3c3e5d6ebc107258c, SDK 10.0.402, Ergebnis: bestanden.

CI am Head: `build` success (`publish` übersprungen). PR-Beschreibung verknüpft #368–#372 mit `Fixes`.

**Akzeptierte `[APPROX]`:** keine (reiner Doku-PR). **Annahmen:** Status- und Byte-Vergleich über die CSV-Spalten `status`,
`groesse`, `fundstellen`; die Prozentwerte der Karte folgen daraus.
