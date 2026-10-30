# Nachprüfung PR #146 – „Review: Checkout bei blockiertem Fetch rekonstruieren“

**Reviewer:** Claude (frische Sitzung, nicht an PR #146 beteiligt) · **Datum:** 30.10.2026
**PR:** [#146](https://github.com/kaaasi/eishockey-manager-redone/pull/146) · **Base:** `0a7257498f0a871aacfe41a1a6b2ab437a77d5fb` · **Head:** `c6618735f2a517943db19b146693413b24ed383c`
**Nachprüfung von:** `docs/reviews/2026-10-30-prozess-146.md` (PR #149), bisher geprüfter Head `349ba3107270ce5586cff280f76c31a759eeac46`; geprüft nur das Delta `349ba31..c661873` und B1–B3.
**Umfang:** reine Dokumentationsänderung. Codelesen an einem Klon mit nachgewiesenem Commit; keine .NET-Läufe (laut Anfrage entfallen sie bis auf die CI).

## 1. Merge-Empfehlung

**Ja für genau diesen Head (`c661873`).** B1 (P2), B2 und B3 (P3) sind behoben: Die Gegenbeispiele laufen gegen den neuen
Wortlaut ins Leere, und die geänderten Zeilen sind widerspruchsfrei zu `docs/prozess/umgebung.md`. Das Delta berührt nur
die beiden Dateien, die der PR ohnehin ändert. Zwei neue P3-Hinweise (N1, N2) sind nicht blockierend (`zusammenarbeit.md`
§6 Nr. 5 verlangt Behebung nur bei P0–P2) und können mit dem Merge oder danach erledigt werden.

Einschränkung nach der Regel, die dieser PR einführt: Dieses Review liefert **keinen Testnachweis** nach §6 Nr. 4 (keine
.NET-Läufe). Ob Nr. 4 bei einem reinen Doku-PR greift, entscheidet die Leitstelle; die CI des Heads ist grün (s. u.).
Base ist unverändert aktuell (`main` = `0a72574`), Nachprüfung also ohne Rebase-Folgen.

## 2. Befunde

### Status der Vorbefunde

| ID | Issue | Stand | Beleg |
|---|---|---|---|
| B1 (P2) | [#152](https://github.com/kaaasi/eishockey-manager-redone/issues/152) | **behoben** | Klausel „(Blob-Hashes oder `git rev-parse`)“ entfernt; [Z. 31–33](https://github.com/kaaasi/eishockey-manager-redone/blob/c6618735f2a517943db19b146693413b24ed383c/docs/vorlagen/review-anfrage.md?plain=1#L31-L33) verlangen Tree-SHA, „Blob-Hashes einzelner Dateien genügen nicht“ |
| B2 (P3) | [#153](https://github.com/kaaasi/eishockey-manager-redone/issues/153) | **behoben** | [Z. 26–30](https://github.com/kaaasi/eishockey-manager-redone/blob/c6618735f2a517943db19b146693413b24ed383c/docs/vorlagen/review-anfrage.md?plain=1#L26-L30) decken nicht möglich, unterlassen, Abweichung und teilweise ab und regeln gelaufene Läufe |
| B3 (P3) | [#154](https://github.com/kaaasi/eishockey-manager-redone/issues/154) | **behoben** (Rest: N2) | [Z. 42–44](https://github.com/kaaasi/eishockey-manager-redone/blob/c6618735f2a517943db19b146693413b24ed383c/docs/vorlagen/review-anfrage.md?plain=1#L42-L44) und [`zusammenarbeit.md` Z. 96–97](https://github.com/kaaasi/eishockey-manager-redone/blob/c6618735f2a517943db19b146693413b24ed383c/docs/prozess/zusammenarbeit.md?plain=1#L96-L97) |

**Gegenbeispiel B1 erneut ausgeführt** (Kopie des Head-Trees mit `git archive`, je `git init` und `git add -A && git write-tree`):

| Stand | Tree-SHA | gilt nach neuem Wortlaut als Head? |
|---|---|---|
| exakter Head | `443adf9dba475875741277fe1995201c0fb7c6e3` | ja (= `c661873^{tree}`) |
| Blob-gleich, dazu eine übrig gebliebene Datei | `b793749429ce8ff157d70ee9973bde5833e05ab8` | nein |
| Blob-gleich, dazu Datei `docs/ALT-geloescht.md` noch da | `23344780eac376ff92fd17aea3c867d1eadb0232` | nein |
| Blob-gleich, Modus 100755 statt 100644 | `e9e23fdd810f220a799f111efe844c42207ceb5b` | nein |

Bei dem Stand „Blob-gleich + übrig gebliebene Datei“ stimmen die Blob-Hashes beider im Delta geänderten Dateien mit dem Head
überein (`8d7a8e2…`, `4ada47d…`) – nach dem alten Wortlaut wäre er als Head durchgegangen, nach dem neuen scheitert er am Tree.

**Gegenbeispiel B2:** Die vier Fälle „nicht möglich“, „unterlassen“, „Tree-SHA weicht ab“, „nur teilweise rekonstruiert“
stehen jetzt ausdrücklich in Z. 26–28. Ein Reviewer, der rekonstruiert, `tools/pruefen.sh` laufen lässt und den Tree nicht
vergleicht, fällt unter „unterlassen“; seine Läufe sind laut Z. 28–29 „höchstens Diagnose“, als „unverifizierter Stand“
zu kennzeichnen und „kein Test des exakten Heads“. Das deckt sich wörtlich mit `umgebung.md` Z. 34–36 und 65 („höchstens
… Diagnose“).

**Gegenbeispiel B3:** Ein Review nur mit Codelesen oder unverifiziertem Stand: Z. 42–44 verlangt „nur Codelesen“ in der
Merge-Empfehlung und schließt den Nachweis nach §6 Nr. 4 aus; Nr. 4 verweist rückwärts auf Schritt 2 der Vorlage
(existiert, definiert „unverifizierter Stand“). Kein Zirkelbezug.

### N1 (P3) – Klon-Fall: `git rev-parse HEAD` erkennt keine veränderte Arbeitskopie

[`review-anfrage.md` Z. 31](https://github.com/kaaasi/eishockey-manager-redone/blob/c6618735f2a517943db19b146693413b24ed383c/docs/vorlagen/review-anfrage.md?plain=1#L31)
nennt für einen Klon nur `git rev-parse HEAD` gleich dem Head-SHA. Das belegt den Commit, nicht den Inhalt des Verzeichnisses,
in dem gebaut wird – dieselbe Lücke wie B1, nur für den Klon-Weg.

Gegenbeispiel: Arbeitskopie nach `echo veraendert >> docs/vorlagen/review-anfrage.md` (im Nachbau): `git status --porcelain`
zeigt ` M docs/vorlagen/review-anfrage.md`, der Tree nach `git add -A && git write-tree` ist `f59604eef1e4e1fffe8fb6e8f1c0f4a72ea67e06`
statt `443adf9d…`. In einem echten Klon bleibt `git rev-parse HEAD` dabei unverändert (folgt aus der Git-Semantik, nicht
gesondert ausgeführt). Gemildert wird das durch Schritt 3 (Mutationen nur „in einer temporären Kopie“), daher P3.

Vorschlag: Z. 31 ergänzen: „bei einem Klon `git rev-parse HEAD` gleich dem Head-SHA und `git status --porcelain` leer“.

### N2 (P3) – Satz Z. 42–44: Grammatik und Quellenliste

[Z. 43–44](https://github.com/kaaasi/eishockey-manager-redone/blob/c6618735f2a517943db19b146693413b24ed383c/docs/vorlagen/review-anfrage.md?plain=1#L43-L44)
lautet „…, und **den** Originaldaten-Nachweis muss dann aus dem PR oder von der Leitstelle kommen“ – Akkusativ statt
Nominativ, der Satz ist so nicht lesbar. Außerdem nennt er „von der Leitstelle“ als Quelle, während
[`zusammenarbeit.md` Z. 94](https://github.com/kaaasi/eishockey-manager-redone/blob/c6618735f2a517943db19b146693413b24ed383c/docs/prozess/zusammenarbeit.md?plain=1#L94)
den Nachweis nur „in PR oder Review“ zulässt. Das ist vermutlich kein Widerspruch (ein Leitstellen-Kommentar im PR ist „im
PR“), aber ungenau.

Vorschlag: „…, und der Originaldaten-Nachweis muss dann aus dem PR oder aus einem Leitstellen-Kommentar kommen.“ und in
§6 Nr. 4 „(in PR, Review oder Leitstellen-Kommentar)“.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| Klon `main`: `git rev-parse HEAD`; `gh api …/git/ref/heads/main` | beide `0a7257498f0a871aacfe41a1a6b2ab437a77d5fb` = Base |
| `git fetch origin <sha>` für `349ba31…` und `c661873…`; `git cat-file -t` | beide `commit`; `c661873^` = `349ba31…` (ein Commit Delta) |
| `gh api …/pulls/146` | offen, kein Draft, Base `0a72574` (`main`), Head `c661873` = genannter Head |
| `git diff --raw 349ba31 c661873` und `0a72574 c661873` | genau zwei Dateien, beide `M`, Modus 100644 unverändert, keine Löschungen oder Neuanlagen |
| Delta gelesen (`git diff 349ba31 c661873`, +14/−5) | `review-anfrage.md` Z. 25–34 und 42–44, `zusammenarbeit.md` Z. 96–97 |
| `git grep` im Head nach Blob/unverifiziert/nur Codelesen/Prüfstand | keine Altformulierung mit Blob-Hashes als Nachweis mehr; Treffer nur in den neuen Zeilen, `umgebung.md` Z. 34 und harmlosen Stellen (`--filter=blob:none`) |
| Abgleich mit `umgebung.md` Z. 15–16, 34–36, 61–66 | übereinstimmend: Codelesen bleibt Codelesen; Tree und Modi; keine Behauptung des exakten Heads ohne Nachweis; „blockiert“ und „höchstens Diagnose“ |
| Gegenbeispiele B1–B3 (siehe oben) | B1 vier Stände per Tree-SHA; B2 und B3 am Wortlaut |
| CI-Check-Runs des Head-SHA `c661873` | `build`: success · `publish`: skipped |
| Issues #152–#154 | offen, Labels `befund` + `P2`/`P3`; PR-Beschreibung enthält `Fixes #152`, `#153`, `#154` |
| `tools/pruefen.sh`, `dotnet`-Läufe | **nicht ausgeführt** (reine Doku, laut Anfrage nur CI) |

**Beobachtung ohne Befund:** Der Tree-Vergleich per `git add -A` lässt Pfade aus `.gitignore` unberücksichtigt (`bin/`, `obj/`,
`*.user`, `gamedata`, `*.vga` …). Das ist für diesen Zweck vertretbar. Das Beispiel setzt außerdem ein Git-Verzeichnis
voraus (im Test `git init` vorweg); als „z. B.“ formuliert, daher kein Befund.

**Akzeptierte `[APPROX]`:** keine (Doku).
**Annahmen:** Die CI-Aussage gilt nur für den Build-Check des Head-SHA ohne Originaldaten. Dass `git rev-parse HEAD` in einem
echten Klon bei veränderter Arbeitskopie unverändert bleibt (N1), ist aus der Git-Semantik abgeleitet, nicht in einem Klon
des Heads nachgestellt.
