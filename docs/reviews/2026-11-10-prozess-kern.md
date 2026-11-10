# Review PR #722 – Spur `prozess-kern` (reine Doku-Prüfung)

Reviewer: Claude Sonnet 5.5 (geplanter Lauf, Sitzung ohne Mitarbeit an #722) · Base `7ea49a50eb3e50dc70442fbd97bceb1605a8d7ba` ·
Head `3a32c5f9934383d8b8a51e4a5539fe4b3e0a92eb` · 10.11.2026 · Art: nur `*.md` geändert (`git diff --name-only 7ea49a5 3a32c5f | grep -v '\.md$'`
→ leer), CI entfällt, kein Build, `tools/pruefen.sh` entfällt. Klon am Head: `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer.

## 1. Merge-Empfehlung

**Ja für genau diesen Head** (Doku-Prüfung, kein Testnachweis nötig, §6 Nr. 3 „entfällt, nur Doku“). Keine Regel der alten Fassung
geht verloren, keine ist neu oder inhaltlich verändert außer den ausgewiesenen Klärungen; Spurmodell und Merge ohne Rebase sind
erhalten. Fünf P3-Unschärfen (N1–N5), kein Merge-Hindernis. Erwartet offen bestätigt: `gegenliste-verdichtung.md` ist nach dem
Merge entbehrlich (Prüfhilfe, keine Regel; keine Datei am Head verweist auf sie – `git grep -l gegenliste-verdichtung 3a32c5f` → leer).

## 2. Prüfpunkt 1 – Gegenliste gegen `7ea49a5`

Alte Fassungen aller sieben Kerndateien (`git show 7ea49a5:<datei>`) Zeile für Zeile mit dem Head und der Gegenliste verglichen.

| Bereich | Ergebnis | Beleg |
|---|---|---|
| Zeilenzählung (7 Dateien, Pflichtlektüre, Leitstelle) | stimmt | `wc -l` alt/neu: 163/144, 182/128, 95/84, 93/45, 53/38, 48/46, 73/58; `anhang.md` 109; Spur 144+31+84+46 = 305 (alt 163+55+95+48 = 361), Review 144+72+45 = 261 (alt 334), Leitstelle 144+128+38+109 = 419 (+175) – Z §2–4/§5–6 per `awk` nach `## n.` |
| `AGENTS.md` §1–§8 | vollständig | §2 Nr. 1–7, §3 (inkl. „Eingeschränkte Arbeitsumgebungen“ als Absatz, blockiert ≠ Ergebnis), §4 Architekturregeln (`Continue()`, `Book()`, `OnMainMenu`/E4 = B, `MenuEntry`, Szenarien), Originaltreue (1)–(5), §5, §6, §7 nur gekürzt |
| `zusammenarbeit.md` §1–§8 | vollständig | Rollentabelle, §2 (Übernahme), §3 (Übergabe, 15 Zeilen), §4, §5 Punkte 1–9 + Modellwahl + Issues, §6 Nr. 1–8; §7/§8 als Verweisüberschriften auf `anhang.md` A/B |
| Vorlagen, `VORLAGE.md`, `CLAUDE.md` | vollständig | Ablauf `review-anfrage` 1–7, `leitstelle-start` 0–10, `spur-start` 1–8 und Teile 1–7 inhaltsgleich; Auslagerungen nach `anhang.md` C–G (Gesamtreview-Absatz bis auf den Verweis „siehe D“ wortgleich; Codex-Regeln 1–4; Rekonstruktion/Tree-SHA in E; Zugriff vor `create_trigger`, `spur-wartet`, Warteschleife, Laufende in F; Git-Klon in G) |
| Klärungen aus #712 | je vorhanden und verwiesen | R-06 `zusammenarbeit.md` §5 Punkt 8 („merged nach §6 (#712/R-06)“); N-01 ebd. Tabelle + `anhang.md` F; N-02 `anhang.md` D; P3-1/P3-2 `archiv/runde-14.md` („Ursprünglich …“, „abgelöst durch Entscheidung C“); P3-3 `review-anfrage.md` Schritt 2/3 + `anhang.md` C; N2 Spiegelstrich 4 + Punkt 4; N3 §5 Tabelle, `spur-start.md` „Fixlauf“, `leitstelle-start.md` Schritt 2 |
| R719-02 | erledigt | `git grep -niE 'stapel|PR-Base|Rebase-Ziel|gestapelt|Folgeteil mit Base' -- docs/vorlagen docs/prozess/zusammenarbeit.md docs/prozess/anhang.md AGENTS.md CLAUDE.md docs/handoffs` → kein Treffer |

Nicht in der Gegenliste verzeichnet, inhaltlich aber nicht neu: die Labeltabelle in §5 Punkt 8 fasst nur Regeln zusammen, die vorher
in §5 Punkt 8 und `spur-start.md` Teile 1/4/5/6 standen (Zeile `spur-wartet`: Zugriff, Teil blockiert, Kettenende, Freigabe abgelehnt,
laufender Fremdlauf – jeweils alt belegt). Gekürzt ohne Bedeutungsverlust u. a.: „nicht eines älteren Laufs“ (§6 Nr. 3), „nie den
ganzen Ablauf“ (`AGENTS.md` §7), „Ablagestand“ (§3 Tabelle).

## 3. Prüfpunkt 2 – Kurzaufrufe und Nummern

- **Kurzaufrufe wörtlich verwendbar:** Kurzaufruf `review-anfrage.md` (alle Zeilen inkl. Schreibaktions-Absatz), `leitstelle-start.md`,
  `spur-start.md` (Kurzaufruf, „Geplanter Lauf mit Teilen“ samt Zugriffsabsatz) sind im Diff **nicht** berührt
  (`git diff 7ea49a5 3a32c5f -- docs/vorlagen/spur-start.md` beginnt erst nach dem Zugriffsabsatz bei Zeile 16; Kurzaufruf
  `review-anfrage.md`: Zeilen 1–14 identisch, `diff` alt/neu leer; geändert erst die Zusätze ab Z. 15). Zusätze „Geplanter Lauf“/„Nachprüfung“ stehen wortgleich zur Verfügung.
- **Fixlauf:** Auftragssatz alt wortgleich, neu ergänzt um den Fall ohne Befund („PR #<nr> auf `main` rebasen, `<Anpassung laut Plan>`,
  Head vorher `<sha>`“, #712/N3); Label-Regeln decken sich mit der Tabelle (§5 Punkt 8).
- **Nummern stabil:** `git grep` nach Verweisen aus Code, Tests, `.github`, `tools`, `umgebung.md`, `dosbox-aufnahme.md`, Plan:
  `AGENTS.md` §2.1/§3/§4/§5/§7, `zusammenarbeit.md` §3/§5 Punkt 4/8/§6 Nr. 1–4, `leitstelle-start.md` Schritt 2/7/10, `review-anfrage.md`
  Schritt 2, „Teile und Fortsetzung“ – alle Ziele vorhanden und gleich nummeriert. `AGENTS.md` §3-Satz „ablegen nur Leitstelle und
  Nutzer per PR“ (Verweis aus `dosbox-aufnahme.md`) steht weiter in der Tabelle. `tools/pruefen.sh` zitiert „Abschnitt Tests“ = §5.
  Ausnahme: N2 unten (Archiv).
- Einzige entfallene Überschriften: `review-anfrage.md` „Gesamtreview …“/„Aufrufe für ChatGPT/Codex“ (jetzt `anhang.md` C/D, im
  Kopf der Vorlage und in `leitstelle-start.md` Schritt 3 verlinkt) und `AGENTS.md` Unterüberschriften „Originaltreue“/„Eingeschränkte
  Arbeitsumgebungen“ (nur Run-in; Verweise gehen auf §3/§4).

## 4. Prüfpunkt 3 – Spurmodell und Merge ohne Rebase

Erhalten. `zusammenarbeit.md` §3 („genau einen PR (ab Runde 15)“), §4 (`paket/<spur>`, Fortsetzungen nacheinander,
`--force-with-lease`), `spur-start.md` Teile 2 („Ein Branch, ein PR (ab Runde 15)“, Draft-PR Base `main`, Rebase vor Push), Teile 6
(kein Integrations-PR je Spur). `leitstelle-start.md` Schritt 2: „Ist `main` seit dem Review neu und der PR **ohne Konflikt**, wird
**nicht** rebased (der freigegebene Head bleibt)“, lokales `--schnell` auf `main`+PR, Tabelle im Merge-Kommentar, Fixlauf nur wenn der Head
sich ändern muss, Delta-Prüfung per `git range-diff`, `integration/<runde>` nur bei Konflikten – inhaltsgleich zu alt. `zusammenarbeit.md`
§6 Nr. 1 (Rebase-Ausnahme) unverändert, Nr. 2 nennt zusätzlich das Vorgehen aus Schritt 2 (`--schnell`, Tabelle) statt es nur vorauszusetzen.
Der Zusatz „ab Runde 15“ in Schritt 2 entfällt zugunsten „(einer je Spur)“ – akzeptiert.

## 5. Befunde (alle P3, ohne Auswirkung auf Regeln)

- **N1 – Zeichenzahl der Gegenliste.** Sie nennt 43.057 Zeichen für die sieben Kerndateien; gemessen 43.092 (alt 56.823 stimmt).
  Beleg: `for f in AGENTS.md docs/prozess/zusammenarbeit.md docs/vorlagen/{spur-start,review-anfrage,leitstelle-start}.md docs/handoffs/VORLAGE.md CLAUDE.md; do git show 3a32c5f:$f | wc -m; done | paste -sd+ | bc` → 43092 (−24 % bleibt). Gegenliste wird archiviert; ggf. ignorieren.
- **N2 – Verweis im Archiv.** `docs/archiv/runde-14.md` Z. 196 sagt jetzt „Ursprünglich Vollprüfung … nach `vorlagen/review-anfrage.md`, Abschnitt
  „Gesamtreview am Rundenende““; dieser Abschnitt liegt nach dem PR in `prozess/anhang.md` C. Beleg: `grep -n 'Gesamtreview am Rundenende' docs/vorlagen/review-anfrage.md` → nur der Verweiszeile-Treffer (Z. 18), keine Überschrift.
  Vorschlag: „(jetzt `prozess/anhang.md` C)“ ergänzen; Archiv, nur historisch relevant.
- **N3 – Nicht verzeichnete Ergänzung.** `AGENTS.md` §8, Zeile „Kurzaufrufe“, nennt neu „DOSBox-Aufnahme“ (Datei `docs/vorlagen/dosbox-aufnahme.md`
  existiert, `ls docs/vorlagen`). Die Gegenliste führt es nicht auf („keine neu“); harmlos, nur Auffindbarkeit.
- **N4 – „wie oben“ statt Name.** `spur-start.md` „Einzelner Folgeteil ohne Fortsetzung“ sagt nur „wie oben, aber statt …“; alt stand dort „Kurzaufruf
  ‚Geplanter Lauf mit Teilen‘“. Direkt darüber steht der Zugriffsabsatz, nicht der Kurzaufruf. Wörtlich verwendbar bleibt es (Ersetzungssatz
  genau benannt), aber für einen neuen Agenten leicht vieldeutig. Vorschlag: „wie ‚Geplanter Lauf mit Teilen‘, aber …“ (gleiches bei „Fixlauf“:
  „Startaufruf wie oben“, alt ebenso).
- **N5 – Codex: Netz vs. Netzbefehle.** `anhang.md` D legt fest, ChatGPT-Reviews liefen mit Netzzugang (N-02), Punkt 3 verbietet aber weiter
  „Netzbefehle im Aufruf“ (`git fetch`, `git ls-remote`, `add_repo`); die frühere Begründung („ohne Netz“) ist entfallen. Kein Widerspruch (Labels
  brauchen Netz, der Aufruf soll trotzdem keine Vorprüfung enthalten), aber ein Halbsatz „weil nach dem Klonen nicht sicher Netz besteht“ würde es klären.

## 6. Ausgeführte Prüfungen und Grenzen

`git diff --stat/--name-only 7ea49a5 3a32c5f`, `git show 7ea49a5:<datei>` je Kerndatei gegen Head, `wc -l`/`wc -m`, `awk`-Abschnittszählung,
`git grep` auf Reststapelmodell, Abschnittsverweise (Code, Tests, `.github`, `tools`, Doku), Gegenliste Zeile für Zeile. Kein Build, keine Tests
(nur Doku). Keine `[APPROX]` betroffen. Nicht geprüft: Sachrichtigkeit der alten Regeln selbst (Auftrag: Erhalt, nicht Inhalt).
