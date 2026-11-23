# Review PR #959 – Erfahrungen der Leitstelle aus Runde 22 (nur Doku)

Reviewer: Claude Sonnet 5.5 (geplanter Lauf, unabhängige Sitzung) · Base `f14c4ad` · Head `c2cd855` (Checkout verifiziert,
`git rev-parse HEAD` = `c2cd8558091bd76006334b9ecf23b5ce740f6000`, `git status --porcelain` leer) · Erstreview, **nur Codelesen/Textprüfung**
(kein Build, keine Tests: nur Doku, nichts Ausführbares betroffen).

## 1. Merge-Empfehlung

**Ja** für Head `c2cd855`. Die vier Punkte des Nutzerauftrags stehen vollständig in `anhang.md` F, der Force-Push-Satz steht an den
zwei genannten Stellen, die Kurzaufrufe sind unverändert, `zusammenarbeit.md` §4 ist angeglichen, und die Suche nach dem
alten Merge-Commit-Rückfall findet keinen Rest (Beleg unten). Nur P3-Hinweise, keine P0–P2.

## 2. Befunde

Beleg für alle: Textprüfung am Head, Befehle in Abschnitt 3.

**P3-1 – Wiederholung vorhandener Regel** ([anhang.md:106](https://github.com/kaaasi/eishockey-manager-redone/blob/c2cd8558091bd76006334b9ecf23b5ce740f6000/docs/prozess/anhang.md#L106)).
„Auch ohne Konflikt `--schnell` auf `main` + PR“ steht schon in `zusammenarbeit.md` §6 Nr. 2 und `leitstelle-start.md` Schritt 2.
Neu ist nur „nach jedem Merge die übrigen offenen PRs auf Konflikte prüfen“ und der Verweis #922/#923. Kürzbar auf diesen Teil.

**P3-2 – „Lauf … nur einen Teil nachgeprüft“ unscharf** ([anhang.md:108](https://github.com/kaaasi/eishockey-manager-redone/blob/c2cd8558091bd76006334b9ecf23b5ce740f6000/docs/prozess/anhang.md#L108)).
Eine Nachprüfung prüft nach `zusammenarbeit.md` §5 Punkt 3 immer nur das Delta; §6 Nr. 4 lässt nach einem Fixlauf dessen
`--schnell`-Tabelle genügen. Gelesen als allgemeine Regel widerspricht der Satz beidem. Gemeint ist vermutlich ein Lauf, dessen
Tabelle ohne vollen `--schnell` auf dem Head blieb. Ein Halbsatz („ohne vollen `--schnell` auf dem Head“) räumt das aus.

**P3-3 – Redundanz in `spur-start.md` Schritt 2** ([spur-start.md:72-74](https://github.com/kaaasi/eishockey-manager-redone/blob/c2cd8558091bd76006334b9ecf23b5ce740f6000/docs/vorlagen/spur-start.md#L72)).
„vor dem Push auf `origin/main` rebasen. Base-Wechsel nur per Rebase …“ nennt den Rebase doppelt. Der Satz im Fixlauf-Absatz
([Zeile 40](https://github.com/kaaasi/eishockey-manager-redone/blob/c2cd8558091bd76006334b9ecf23b5ce740f6000/docs/vorlagen/spur-start.md#L40))
ist wortgleich zu Schritt 2; vom Nutzer so verlangt, deshalb nur Hinweis.

**P3-4 – Folge von „Force-Push gesperrt“ offen** (spur-start.md Z. 40 und 73-74). Der Satz nennt Label und Fehlermeldung, aber nicht
wo die Meldung steht (Übergabe/PR-Kommentar), ob die Übergabe geschrieben wird und ob der Lauf endet. Schritt 4 („Teil blockiert“,
Z. 76-78) listet diesen Fall nicht. Das passt zur Tabelle `spur-wartet` in `zusammenarbeit.md` Z. 93 („Zugriff … blockiert“),
ist aber nicht ausdrücklich. Ein Verweis auf Schritt 4 genügt.

## 3. Ausgeführte Prüfungen

| Prüfung | Befehl | Ergebnis |
|---|---|---|
| Head/Delta | `git fetch origin pull/959/head`, `git diff --stat origin/main HEAD` | 3 Dateien, +13/−3, nur `docs/**` |
| CI entfällt | `build.yml` Z. 9/12 `paths-ignore: ['docs/**', '**/*.md']` | bestätigt: kein Lauf, „entfällt, nur Doku“ |
| Whitespace | `git diff --check origin/main HEAD` | sauber |
| Rest des Merge-Rückfalls | `grep -rniI "merge-commit\|git merge\|update_pull_request_branch" docs/prozess docs/vorlagen AGENTS.md CLAUDE.md` | keine Treffer |
| Kurzaufrufe unverändert | Diff `spur-start.md`: nur Z. 40-41 und 72-74 | bestätigt |
| Widerspruch `zusammenarbeit.md` | §4 Z. 47 „(gesperrt: `spur-wartet`, `spur-start.md` „Teile und Fortsetzung“ Schritt 2)“ | passt zu Schritt 2; Tabelle Z. 93 deckt „blockiert“ |
| `leitstelle-start.md` Schritt 2 | Fixlauf ohne Befund bei Head-Änderung | verträglich; Rebase statt Merge-Commit nirgends widersprochen |
| Verweise | #922/#923, A858/A553/A808 in `docs/reviews/` auffindbar | ja |

Vier Punkte des Auftrags in `anhang.md` F: (1) Migrationsspuren nacheinander, Fixlauf der nächsten erst nach Merge der vorherigen;
(2) nach jedem Merge offene PRs prüfen und `--schnell` auf `main` + PR; (3) voller `--schnell` bei nur teilweise nachgeprüften
Läufen; (4) minimaler Kern-Ereignisaufruf im Bildschirm entscheidet die Leitstelle (A858/A553/A808). Alle vier vorhanden;
Punkt (4) vereinbar mit `AGENTS.md` §4 („Bildschirme verändern keinen Spielzustand durch bloßes Öffnen“), da es um Klicken und
Verlassen geht. Das ist eine Lesart; die Regel nennt die Ausnahme nicht ausdrücklich.

**`[APPROX]`:** keine (Doku). **Annahmen:** Wortlaut der vier Punkte aus der PR-Beschreibung und dem Prüfauftrag, der Chat des
Nutzers lag nicht vor. Kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4 nötig (nur Doku, CI entfällt).
