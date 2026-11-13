# Review PR #807 – Abschluss Runde 17

**Geprüft:** Base `6805ebf`, Head `9acfff9` (`git rev-parse pr807` = `9acfff9666a06f7c4ed20dbf025bb71afc7f6267`, Head per
`git fetch origin pull/807/head`; `git merge-base pr807 origin/main` = Base). Reine Doku-Prüfung:
`git diff --stat 6805ebf 9acfff9` → `docs/arbeitspakete.md` (+8/−151), `docs/archiv/runde-17.md` (+169). **Nur Codelesen, Grep
und GitHub-REST:** kein SDK, kein `tools/pruefen.sh` (Kurzaufruf: CI entfällt, kein Build nötig). Der Prüfer hat am PR nicht
mitgearbeitet (frische Sitzung, geplanter Lauf). Gegenbeispiele: keine angegeben.

## 1. Merge-Empfehlung

**Ja** für `9acfff9`. Der Rundenabschnitt ist unverändert archiviert, Ergebnis und Abschluss stimmen mit den gemergten PRs, der
Inventur am Base und den offenen Issues überein, der Kopf für Runde 18 folgt R17-D2 = B. Ein P3-Hinweis ohne Auflage.

## 2. Befunde

### R807-01 (P3, Hinweis): Abschluss nennt als Rest nur A037
`Abschluss` führt neben den Issues nur „A037 bleibt `[APPROX]`“. Weitere Restpunkte der Runde stehen nur in der Inventur, z. B.
A390 „Symbol 7 (EC-Teilnehmer, Core) offen“ (`docs/analyse/inventur.md` Z. 623 am Base), A251 „erledigt bis auf die Gebotshöhe
(0x47347)“, A226 „Rest Klasse b“ sowie die neuen Zeilen A418–A437. Die Statuszeile von `ui-rest` im Archiv nennt Symbol 7
zwar, die Vormerkungen in `arbeitspakete.md` aber nicht.

```
git show 6805ebf:docs/analyse/inventur.md | grep -m1 "^| A390 |" | grep -o "Symbol 7 (EC-Teilnehmer, Core) offen"   → Treffer
git show 9acfff9:docs/arbeitspakete.md | grep -c "Symbol 7"                                                          → 0
```

Kein Fehler (die Inventur ist maßgeblich, Vorbild Archiv Runde 16 nennt ebenfalls nur Issues), nur ein möglicher Satz
„Restzeilen der Runde: Inventur ‚Stand Runde 17‘“ im Abschluss. Kann beim Plan Runde 18 mitgenommen werden.

**Besonders geprüft (ohne Befund):**

1. **Archiv unverändert.** Abschnitt am Base von `## Aktuelle Runde: Runde 17` bis vor `### Zurückgestellt` (146 Zeilen) gegen
   `docs/archiv/runde-17.md`:
   ```
   awk '/^## Aktuelle Runde: Runde 17/{f=1} /^### Zurückgestellt/{f=0} f' base.md > sect.md; diff sect.md arch.md
   ```
   → Unterschied nur im Kopf (Archivüberschrift, Hinweisabsatz, Ergebnis, Abschluss, Entscheidungen während der Runde, `---`), in
   der Rundenüberschrift (`## Aktuelle Runde: Runde 17 …` → `## Runde 17 …`) und der entfallenen Leerzeile am Dateiende. Aufbau
   wie `archiv/runde-16.md`. „Zurückgestellt“ und „Vormerkungen“ fehlen im Archiv und stehen weiter in `arbeitspakete.md`.
2. **Ergebnis/Abschluss gegen GitHub.** `gh api repos/…/pulls/<nr>`: #790–#794, #801 und Reviews #795–#799, #802, #806 alle
   `merged=true`; Merge-Folge im Log `3a8070c..6805ebf` #794 → #793 → #790 → #791 → #792 → #801 wie im Ergebnis. Merge-Commits
   `eeb2dba` (wettbewerbe-rest) und `636b417` (ui-rest) belegen den Rückfallweg aus `spur-start.md` Z. 71 f. Offene Issues
   (`issues?state=open`): genau #730, #773, #803, #804, #805 (plus PR #807); #784, #785 `completed`, #800 `not_planned`
   („kein Befund“). Gesamtreview `2026-11-13-runde-17-gesamt.md`: „Ja für Head `20e739c…`“, keine neuen Befunde. Inventur am
   Base: alle im Ergebnis genannten IDs mit „Stand Runde 17 … erledigt“ (A130/A066 „erledigt für Managerkader“, Rest A427),
   A037 „offen, präzisiert“; Schema 50 (`SaveGame.cs:48`), `SeasonEvent.YouthJoined` vorhanden; `re-abdeckung.md` Z. 77
   „0x575C3 … seit Runde 17 dokumentiert“; `re-livespiel.md` §10–§12 vorhanden (§12 = Umsetzungsplan Runde 18).
   Keine Nutzerentscheidung `R17-E…` in `docs/` (`git grep "R17-E[0-9]"` leer) → „Entscheidungen (Leitstelle)“ stimmig.
3. **Runde 18 und `livespiel`.** Kopf „Runde 18 – Planung offen“ verweist auf `analyse/re-livespiel.md` §10–§12 und R17-D2 = B;
   der Eintrag `livespiel` in „Zurückgestellt“ ist zu Recht gestrichen (Umsetzung jetzt Runde 18, 575C3h nicht mehr unbekannt),
   neu in „Vormerkungen (nach Runde 17)“ als „Live-Spiel (Runde 18)“. Erledigte Testlücken #784/#785 aus den Vormerkungen
   entfernt, P3 #803–#805 ergänzt. Der allgemeine Fahrplaneintrag „Später: `livespiel` … letzter Schritt vor v1.0“ bleibt und
   widerspricht dem nicht.

**Erwartet offen, bestätigt:** #730 (P2), #773 (P3) – DOSBox-Aufnahme `runde-17`; #803, #804, #805 (P3) – alle GitHub `open`.

## 3. Ausgeführte Prüfungen und akzeptierte `[APPROX]`

- `git fetch origin main pull/807/head:pr807`, `git rev-parse pr807`, `git merge-base pr807 origin/main`, `git diff --stat`,
  `git diff 6805ebf pr807 -- docs/arbeitspakete.md`, Abschnittsvergleich per `awk`/`diff` (oben).
- `gh api repos/kaaasi/eishockey-manager-redone/pulls/<nr>` für 790–799, 801, 802, 806, 807; `issues?state=open`; `issues/784`,
  `785`, `800`.
- `git grep` am Base: Inventurzeilen aller Ergebnis-IDs, `SchemaVersion`, `YouthJoined`, `575C3`, `R17-E`, `Symbol 7`.
- **Nicht ausgeführt:** Build, Tests, `tools/pruefen.sh` (reine Doku, laut Kurzaufruf entfallen). Kein Testnachweis.
- **`[APPROX]`:** keine neuen; A037 bleibt `[APPROX]` wie im Gesamtreview bestätigt.
