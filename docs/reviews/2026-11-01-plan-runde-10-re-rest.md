# Review: Nachtrag Spur `re-rest` (Plan Runde 10, PR #379)

- PR [#379](https://github.com/kaaasi/eishockey-manager-redone/pull/379) · Base `664ff18f8675c12d63c5f6ad4075309d5975b829` ·
  Head `94b2a841ca4e0d73eafd73fdd8b3972863df05ba` · Reviewer: Claude Opus 5.5 (geplanter Lauf, eigene Sitzung, an #379
  und an den Spur-PRs #375–#378 nicht beteiligt).
- Checkout per Klon, `git fetch origin pull/379/head`; Worktree am Head: `git rev-parse HEAD` = Head, `git status --porcelain`
  leer. Weitere Worktrees: Base; Base + `paket/re-geld` (d695cda), `paket/re-meldungen` (cfb727a), `paket/re-saisonwechsel`
  (a13c536) zusammengeführt („Stand 3“); Stand 3 + `paket/re-livespiel` (46dbfe6) („Stand 4“); `integration/runde-10` (ceee82c).
  Alle vier Spurbranches haben Merge-Base 664ff18 und entsprechen den Heads von #375–#378.
- `em.exe` aus dem Referenz-Repo (SHA-256 laut `original/SHA256SUMS` geprüft).

Links: `https://github.com/kaaasi/eishockey-manager-redone/blob/94b2a841ca4e0d73eafd73fdd8b3972863df05ba/docs/arbeitspakete.md#L<n>`.

## 1. Merge-Empfehlung

**Nein für genau diesen Head – nach einer kleinen Textänderung Ja (Nachprüfung des Deltas).** Die Adressliste ist exakt
richtig (53 Funktionen, 6989 Byte, keine fehlt, keine zu viel), R10P-01 bleibt behoben (0 Statuswechsel), der PR ist ein
reiner Doku-PR, CI und `tools/pruefen.sh` (alle drei Läufe) sind grün. Aber 575C3h wird mit „bleibt bei `re-livespiel`“
einer Spur zugewiesen, deren Adressliste und PR (#378) die Funktion nicht enthalten – sie bliebe nach Runde 10 ohne
Zuständigkeit unbekannt (R10R-01, P2). Dazu ein falscher Issue-Verweis (R10R-02, P3).

## 2. Befunde

### R10R-01 (P2) – 575C3h ist keiner Spur zugeordnet

[L98](https://github.com/kaaasi/eishockey-manager-redone/blob/94b2a841ca4e0d73eafd73fdd8b3972863df05ba/docs/arbeitspakete.md#L98)
nimmt 575C3h aus `re-rest` heraus („vermutlich Livespiel, bleibt bei `re-livespiel`“). Die Spur `re-livespiel` hat aber nur
die 19 unbekannten Funktionen von Ov 130h
([L86–88](https://github.com/kaaasi/eishockey-manager-redone/blob/94b2a841ca4e0d73eafd73fdd8b3972863df05ba/docs/arbeitspakete.md#L86-L88));
575C3h liegt in **Ov 138h** (421 Byte, Text „toreÖ“, einziger Aufrufer 0x567C8 = Torszenen-Abspieler).

Gegenbeispiel: Funktionskarte `--csv` an Stand 4 (alle vier Spurbranches) → 0x575C3 `unbekannt`, Spalte `fundstellen`
leer; `docs/analyse/re-livespiel.md` (#378) nennt nur 0x567C8 und das offene `tore\`-Format, nicht 0x575C3. `git grep 575C3`
am Head findet außer L98 nur `re-abdeckung.md` und ein altes Review. Mit #378 abgeschlossen hat die Funktion damit keinen
Bearbeiter, entgegen dem Rundenziel und der Abnahme der Integration.

Vorschlag (eine Zeile): 575C3h in die Liste von `re-rest` aufnehmen (Ov 138h: 575C3h, 59D71h; dann 54 Funktionen,
7410 Byte) **oder** unter „Zurückgestellt“ bei `livespiel` als offen eintragen („575C3h, Ov 138h, Lader `tore\`, Aufrufer
567C8h – Analyse mit `livespiel`“). Ersteres schließt die Lücke in Runde 10.

### R10R-02 (P3) – Falscher Verweis „#374“ beim Doppelpunkt-Hinweis

[L109](https://github.com/kaaasi/eishockey-manager-redone/blob/94b2a841ca4e0d73eafd73fdd8b3972863df05ba/docs/arbeitspakete.md#L109):
„Nach einer Adresse kein Doppelpunkt (sonst erkennt die Karte sie nicht, #374).“ Issue #374 ist R10PN-01 (defekter Link im
Archiv). Der Doppelpunkt-Fall ist der „Hinweis (kein Befund)“ in `docs/reviews/2026-11-01-plan-runde-10-nachpruefung.md`
(Review-PR #373). Vorschlag: „(Hinweis in `reviews/2026-11-01-plan-runde-10-nachpruefung.md`)“ statt „#374“.

### Hinweis (kein Befund) – 435CDh erhält eine Fundstelle aus `re-livespiel.md`

Keine Überschneidung im Umfang: Ov 130h ist ausgenommen, die Liste enthält keine der 19 Adressen von `re-livespiel`. In der
Karte wird an Stand 4 aber 0x435CD (Ov 110h, in der `re-rest`-Liste) durch eine Stelle innerhalb der Funktion
(`re-livespiel.md:125`, 0x4365B/0x4367E) „dokumentiert“. Für die Abnahme von `re-rest` ändert das nichts (die Spalte
`fundstellen` muss trotzdem `re-rest.md` nennen); nach der Integration ist der Statuswechsel von 435CDh nur kein Beleg für
die Arbeit von `re-rest`. Ein Halbsatz („435CDh: Arbeitsfläche schon in `re-livespiel.md` erwähnt, nur lesen“) wäre
genug, ist aber nicht nötig.

### Erwartet offen

- **#374** (R10PN-01): bestätigt offen (Issue `open`); am Head nicht berührt, Behebung liegt in `integration/runde-10`
  (#380, „Fixes #374“).

### Gegenbeispiel R10P-01 wiederholt

Funktionskarte `--csv` an Base und Head, Spalten `status` und `fundstellen` verglichen: **0 Statuswechsel, 0 geänderte
Fundstellen**; `docs/arbeitspakete.md` kommt an keinem der beiden Stände in `fundstellen` vor. Die h-Schreibweise der
54 neuen Adressen wird wie vorgesehen nicht gezählt.

### Adressliste (Besonders prüfen)

Unbekannte Funktionen im Spielcode (ohne Systemsegmente nach `BASE_KIND` in `funktionskarte.py`) an Stand 3: 73 Funktionen,
14 014 Byte; davon Ov 130h 19 und 575C3h 1 → **53 Funktionen, 6989 Byte**. Menge identisch mit den 53 Adressen in
L100–106 (keine fehlt, keine zusätzlich, keine doppelt); alle 53 waren schon an Base unbekannt. Gegen `integration/runde-10`
(ceee82c, drei Spuren ohne `re-livespiel`) dieselbe Menge. Bereichszuordnung je Zeile stimmt. Die Angabe „etwa 7 KB“ passt.

Reiner Doku-PR: `git diff --stat` = nur `docs/arbeitspakete.md` (+17), `git diff --check` sauber.

## 3. Ausgeführte Prüfungen

```
git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD      # erreichbar
git fetch origin '+refs/heads/*:refs/remotes/origin/*' pull/379/head
git worktree add <scratch>/{base,head,combo,combolive,integ} …               # combo: merge der drei Spurbranches
python3 tools/reverse-engineering/funktionskarte.py <em.exe> --repo <worktree> --csv <scratch>/<stand>.csv
python3 <scratch>/ana.py                                                     # Vergleich, Mengenabgleich mit L100–106
gh api …/commits/94b2a84…/check-runs                                         # build: success, publish: skipped
tools/claude/setup-workspace.sh <referenz>/original/gamedata && source ~/claude-tools/env.sh
tools/pruefen.sh                                                             # am Head 94b2a84
```

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1631 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1351 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1351 | 280 | 0 |

Commit 94b2a841ca4e0d73eafd73fdd8b3972863df05ba, SDK 10.0.402, Ergebnis: bestanden. Ausführung am nachgewiesenen Checkout.

**Akzeptierte `[APPROX]`:** keine (Doku-PR ohne Code).
**Annahmen:** „Spielcode“ wie in `funktionskarte.py` (`is_system`); Zuordnung „vermutlich Livespiel“ von 575C3h nicht
geprüft (nur Aufrufer und Text aus der Karte).
