# Review PR #786: Abschluss Runde 16 (Doku)

Prüfer: Claude (frische geplante Sitzung, `claude-sonnet-5-5`; keine Mitarbeit an #786) · Base `ee595f1c0116ca16fc059c77e6d4fd7b4b4c9afb` · Head `14bbea4fbc386898a30b2192ef59f79ebe374a77`
Checkout am Head: `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer. Reine Doku-Prüfung, kein Build.

## 1. Merge-Empfehlung

**Ja** für `14bbea4`, mit einem P3 (R786-01): die Zählzeile der Funktionskarte (6826) stimmt am Head nicht mehr; `--write` ergibt 6821.
Inhaltlich (Archiv, Ergebnis, Abschluss, Namensregel, CI-Abschnitt) keine Beanstandung. Nachziehen in diesem PR oder beim
nächsten `--write` genügt; kein P0–P2.

## 2. Befunde

**R786-01 (P3) – R781-P3-1 nur scheinbar behoben: `funktionskarte.py --write` hat am Head einen Diff.**
[`docs/re-abdeckung.md:152`](https://github.com/kaaasi/eishockey-manager-redone/blob/14bbea4fbc386898a30b2192ef59f79ebe374a77/docs/re-abdeckung.md#L152) und
[`docs/analyse/inventur.md:310`](https://github.com/kaaasi/eishockey-manager-redone/blob/14bbea4fbc386898a30b2192ef59f79ebe374a77/docs/analyse/inventur.md#L310) nennen 6826.
Beleg (em.exe aus dem Referenz-Repo, `tools/claude/fetch-reference.sh original`):
- Am Head: `python3 tools/reverse-engineering/funktionskarte.py --write ~/referenz/original/gamedata/em.exe` → `re-abdeckung.md` Zeile 152 wird `6821 Zuordnungen` (zweiter Lauf gleich, 6821). Sonst keine Zeile ändert sich (Statustabellen gleich).
- Ursache: Der Lauf zählt `docs/**/*.md` ohne `archiv/` und `reviews/` (`scan_docs`, Zeilen 592–596). Der verschobene Rundenabschnitt liegt jetzt im Archiv und fällt aus der Zählung. Gegenprobe: Head mit `docs/arbeitspakete.md` von der Base → `--write` ohne Diff (6826). Die 6826 gehören also zum Stand vor der Verschiebung.
- Folge: Das Kriterium „`--write` ohne Diff am Head“ ist verfehlt; der nächste `--write` ändert die Datei. Fix: 6821 in `re-abdeckung.md:152` und `inventur.md:310`, danach `--write` zweimal ohne Diff. Auswirkung auf Statusanteile: keine.

**R781-P3-2 (A404-Text): behoben.** `inventur.md` A404 liest jetzt „(PR #765, Spur `pool` Teil 3, #743): Ersatzregel …“; die Klammer- und Fettschrift-Reste („#743):, Teil 3):“) sind weg, übrige Zelle unverändert (`git diff ee595f1..14bbea4 -- docs/analyse/inventur.md`).

Keine weiteren Befunde.

## 3. Geprüftes (Besonders prüfen) und bestätigte Offene

1. **Archiv `docs/archiv/runde-16.md`:** Base-Abschnitt (`ee595f1:docs/arbeitspakete.md` Zeilen 11–127, Überschrift „Aktuelle Runde“ bis Ende „R16-D2“) gegen Archiv Zeilen 25–141: `diff` zeigt genau eine Zeile (Überschrift „## Aktuelle Runde: Runde 16 …“ → „## Runde 16 …“), 117 von 117 Zeilen sonst gleich. Neu sind nur Kopf, Ergebnis, Abschluss, Entscheidungen während der Runde. „Zurückgestellt“ und „Vormerkungen“ bleiben in `arbeitspakete.md` (um #730/#773 bzw. #784/#785 ergänzt); Abschnitte `hok-format`, `wettbewerbe`, `speichern`, `Später` unberührt.
   Ergebnis/Abschluss gegen GitHub: #765, #766, #767, #781, #783 gemergt; #771 geschlossen (Draft, nicht gemergt – so im Archiv als „Prüfauftrag“ von #766 gefasst, vertretbar); #743, #732, #699, #772, #757, #734, #703, #768, #770, #775, #780, #776, #778 geschlossen; offen genau #730 (P2), #773, #784, #785 (P3). #783: Base `d233989`, Empfehlung ja, zwei Testlücken – stimmt mit „Ja für `d233989`“.
2. **Namensregel:** `anhang.md` F („`EM R<runde> <Name>`“, Beispiele `EM R16 Spur pool – Fortsetzung 1`, `EM R16 Erstreview #765 pool`) und `spur-start.md` Schritt 5 (`EM R<runde> Spur <name> – Fortsetzung <n+1>`) widerspruchsfrei; keine weitere Stelle in `docs/vorlagen`, `docs/prozess`, `AGENTS.md`, `CLAUDE.md` nennt ein altes Namensschema. Die Kurzaufrufe (Platzhalter in `spur-start.md`, `review-anfrage.md`) sind unverändert wörtlich verwendbar. Kosmetik: Zeile 79 in `spur-start.md` ist länger als die übrigen (kein Befund).
   **„CI hängt“:** `build.yml` hat `workflow_dispatch` (Kommentar „baut zusätzlich die Downloads (Job publish)“), `pull_request` mit `paths-ignore: docs/**, **/*.md`; `gh workflow run build.yml --ref <branch>` ist der passende Aufruf; `zusammenarbeit.md` §6 Nr. 3 (CI auf genau diesem Head) wird dadurch erfüllt. Aussage stimmt; Nebenwirkung (publish-Job läuft mit) ist in `build.yml` dokumentiert, im Abschnitt nicht erwähnt – kein Befund.
3. **R781-P3-1:** siehe R786-01. **R781-P3-2:** behoben (oben).
4. **Erwartet offen (nur bestätigt):** #730, #773, #784, #785 offen; in „Zurückgestellt“ (#730, #773) bzw. „Vormerkungen“ (#784, #785) eingetragen.

## 4. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `git diff --stat ee595f1..14bbea4` | 6 Dateien, nur `docs/**`; kein Code |
| Archivabschnitt gegen Base (`diff`) | 1 Zeile (Überschrift) |
| `funktionskarte.py --write` am Head (2×) | 6821 statt 6826 → R786-01 |
| dasselbe, `arbeitspakete.md` von Base | kein Diff (Ursache belegt) |
| `funktionskarte.py --write` an der Base | committet 6824, Lauf 6826 (= ursprünglicher Befund R781-P3-1) |
| GitHub: PR-/Issue-Stände | siehe 1. |

`tools/pruefen.sh` nicht ausgeführt (Doku-PR, CI entfällt nach `build.yml` `paths-ignore`; Auftrag: kein Build nötig). Kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4 beansprucht; Originaldaten für `em.exe` genutzt. Alle Änderungen sind Doku, keine `[APPROX]` berührt. Arbeitsbaum nach den Proben zurückgesetzt.
