# Review PR #966 – `pruefen-sh` (Fixes #951, Review #950 P3-1…3)

Reviewer: Claude (Sonnet 5.5), frische Sitzung ohne Implementierungskontext. Erstreview (geplanter Lauf, unbeaufsichtigt).
Geprüft: Base `70a09fc47787f9792362c16c64a3e7a3a58e6d86`, Head `277f262577cef5a0462ef67b3777495a428609ab`
(`git rev-parse HEAD` = Head, `git status --porcelain` leer, SDK 10.0.402, Originaldaten aus dem Referenz-Repo verknüpft).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Alle drei Läufe und der Selbsttest sind grün, die Zahlen decken sich mit der PR-Beschreibung, die
Rückschaltproben des PR sind reproduziert, die Änderung bleibt in den Dateigrenzen. #951 ist umgesetzt; offen bleibt nur eine
Testlücke (P3-1 unten, kein Blocker).

## 2. Befunde

**P3-1 – Fehlervermerk der Zeile „Mit Originaldaten“ ist vom Selbsttest nicht abgedeckt (P3-2 nur halb belegt).**
[`tools/pruefen-selbsttest.sh` Z. 90](https://github.com/kaaasi/eishockey-manager-redone/blob/277f262577cef5a0462ef67b3777495a428609ab/tools/pruefen-selbsttest.sh#L90)
prüft mit `grep -qF` irgendeine Zeile des Protokolls; im Modus `haenger` trägt aber auch die Zeile „Ohne Originaldaten“ den Vermerk
(wenn `gamedata/kb` da ist, laufen beide Zeilen). Beleg (Mutation nur an der ersten Fundstelle in
[`tools/pruefen.sh` Z. 60](https://github.com/kaaasi/eishockey-manager-redone/blob/277f262577cef5a0462ef67b3777495a428609ab/tools/pruefen.sh#L60)):
`sed -i '0,/ || vermerk=" – \*\*FEHLER: unvollständig\*\*"/s// || vermerk=""/' tools/pruefen.sh` → `tools/pruefen-selbsttest.sh` rc 0
(unmutiert rc 0; Vermerk nur der Zeile „Ohne“ entfernt → rc 1). Auswirkung gering: die echte Ausgabe stimmt (Hänger-Probe unten),
nur eine künftige Regression an dieser Zeile bliebe unbemerkt. Vorschlag: `zeile` auf die Zeilenkennung beschränken
(`"Mit Originaldaten (\`EM_REQUIRE_GAMEDATA=1\`) | … – **FEHLER: unvollständig**"`).

**P3-2 – Verdacht, kein Beleg nötig:** Der Zeilentest „Lauf mit Daten“ ist an `[ -d "$ROOT/gamedata/kb" ]` gebunden und läuft damit in der CI
(keine Originaldaten) nicht; dort deckt nur „ohne Daten“ die Summen ab. Folgerichtig, nur der Vollständigkeit halber genannt.

Keine P0–P2.

## 3. Ausgeführte Prüfungen

**Gegenbeispiele des PR erneut ausgeführt** (Mutation in der Arbeitskopie, danach `git checkout`; jeweils `tools/pruefen-selbsttest.sh`):

| Mutation | rc | Meldung (Auszug) |
|---|---:|---|
| `{s+=$2}` → `{s+=0}` (P3-1 Summe) | 1 | Tabelle „ohne Daten“: erwartet `… | 20 | 0 | 8 |` |
| `{p+=$2}` → `{p+=2*$2}` | 1 | Tabelle „mit Daten“: erwartet `… | 28 | 0 | 0 |` (8 FEHLER) |
| `{f+=$2}` → `{f+=0}` | 1 | Tabelle „Pflichtlauf“: erwartet `… | 24 | 4 | 0 |` |
| Vermerk „FEHLER: unvollständig“ entfernt (P3-2, beide Zeilen) | 1 | Tabelle „haenger“: erwartet `15 | 0 | 6 – **FEHLER: unvollständig**` |
| Standardlimit `20m` → `21m` | 1 | „Hängeschutz (20m): 0 von 3 Testaufrufen“ |
| unverändert | 0 | alle Fälle ok |

**Selbsttest** `tools/pruefen-selbsttest.sh` (Head): 14 Fälle ok, rc 0, 2 s.

**`tools/pruefen.sh` (alle drei Läufe, Head, 24 min 58 s):** Build ohne Warnungen, Ergebnis „bestanden“, Commit-Zeile = Head.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3787 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2673 | 0 | 612 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2674 | 1113 | 0 |

**Hängeschutz-Probe** `EM_HANG_TIMEOUT=1s tools/pruefen.sh --schnell` (echter Hänger statt Double; 30 s wären für die Probe zu lang,
Mechanik identisch): rc 1, Meldung „Lauf mit Originaldaten lieferte Ergebnisse für 1 von 4 Testprojekten“, „inactivity time … elapsed“,
Tabellenzeile `| Mit Originaldaten (…) | 31 | 0 | 0 – **FEHLER: unvollständig** |`, „Ergebnis: NICHT bestanden“. Die Summe enthält nur
die fertigen Projekte. Die 30s-Variante prüft der Selbsttest über die Testaufrufe (`--blame-hang-timeout 30s … ok`). Nach den Läufen
`git status --porcelain` leer (keine Testartefakte im Baum).

**#951 / Review #950 vollständig?**
- P3-1 (Summen): Double liefert je Lauf verschiedene Zahlen (7/0/0, 5/0/2, 6/1/0), Selbsttest prüft die vier Tabellenzeilen – ja
  (bis auf P3-1 oben für den Vermerk).
- P3-2 (Vermerk bei Hänger): umgesetzt in beiden Läufen, Probe oben – ja.
- P3-3 (Kleinigkeiten): `unset` durch `env -u` ersetzt (wirksam: Mutation „21m“ wird erkannt; Aufruf mit gesetzter Umgebungsvariable
  nicht gesondert geprüft), Hinweis zu „Collecting hang dumps“ im Skriptkopf statt `umgebung.md` – vom Review als „ggf.“ freigestellt, ja.

**Dateigrenzen:** `git diff --stat 70a09fc 277f262`: `tools/pruefen.sh`, `tools/pruefen-selbsttest.sh` und die Statuszeile der Spur in
`docs/arbeitspakete.md` (eigener Abschnitt, nach `AGENTS.md` §6 zulässig). Keine gemeinsamen Dateien, keine Originaldaten im Diff.

**Akzeptierte `[APPROX]`:** keine (Prozesswerkzeug). **Annahmen/nicht geprüft:** Verhalten unter GitHub-CI (kein Lauf dort);
`pruefen-selbsttest.sh` läuft in der CI nur, soweit `build.yml` es aufruft – nicht nachgesehen.
