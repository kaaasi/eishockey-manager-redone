# Review PR #708 – Prozess: Effizienz (Doku-Prüfung)

Reviewer: Claude Code (geplanter Lauf, eigene Sitzung, nicht an #708 beteiligt) · Base `b13046422b285404ad6ddbe7b5f1b18e22069eea` ·
Head `28de8b771b38ef52ff08303c1c08dd520a0f9de4` · Art: reine Doku-Prüfung, nur Codelesen (kein Build, CI entfällt)

## 1. Merge-Empfehlung

**Nein** für genau diesen Head – zwei P2-Lücken bei den neuen Labels (R-01, R-02); beide mit wenigen Zeilen behebbar.
Inhaltlich entspricht der Diff der in der PR-Beschreibung genannten Entscheidung (2, 4, 5, 6 umgesetzt; 1 und 3 sowie das
Verdichten als Vormerkung Runde 15). Die Entscheidung selbst steht nur im Chat, nicht in GitHub; geprüft ist daher nur die
Übereinstimmung Diff ↔ PR-Beschreibung. Widersprüche zu `AGENTS.md`, §5 „Sparsame Kette“, §6 Merge-Prüfung,
Unabhängigkeit des Reviews und „Teile und Fortsetzung“ habe ich nicht gefunden: Punkt 7 (ohne Beleg ≤ P3) passt zu §6 Nr. 5,
die vom Fixlauf angelegte Nachprüfung läuft in einer neuen Sitzung (Unabhängigkeit gewahrt), `spur-wartet` wird im selben
Sinn wie in `spur-start.md` Schritt 5 verwendet. Das Spurmodell ohne Teil-PRs (Vorschlag 3) widerspräche §4 und der
Stapelregel, ist aber ausdrücklich nur vorgemerkt – das ist bei der Umsetzung in Runde 15 mitzuändern.

**Erwartet offen bestätigt:** Verdichten der Prozessdoku ist als Vormerkung Runde 15 eingetragen
([arbeitspakete.md:247](https://github.com/kaaasi/eishockey-manager-redone/blob/28de8b771b38ef52ff08303c1c08dd520a0f9de4/docs/arbeitspakete.md#L247)), nicht umgesetzt.

## 2. Befunde

**R-01 (P2) – Standard-Reviewer ChatGPT/Codex kann das Label nicht setzen, die Leitstelle wartet aber nur noch auf Labels.**
[review-anfrage.md:54](https://github.com/kaaasi/eishockey-manager-redone/blob/28de8b771b38ef52ff08303c1c08dd520a0f9de4/docs/vorlagen/review-anfrage.md#L54) verlangt das Label vom Reviewer, „ohne Schreibzugriff setzt es
die Leitstelle“. Codex arbeitet ohne Netz ([review-anfrage.md:87](https://github.com/kaaasi/eishockey-manager-redone/blob/28de8b771b38ef52ff08303c1c08dd520a0f9de4/docs/vorlagen/review-anfrage.md#L87) und Abschnitt
„Aufrufe für ChatGPT/Codex“, unverändert) und setzt also kein Label; die Warteschleife fragt nach
[CLAUDE.md:64](https://github.com/kaaasi/eishockey-manager-redone/blob/28de8b771b38ef52ff08303c1c08dd520a0f9de4/CLAUDE.md#L64) und [leitstelle-start.md:45](https://github.com/kaaasi/eishockey-manager-redone/blob/28de8b771b38ef52ff08303c1c08dd520a0f9de4/docs/vorlagen/leitstelle-start.md#L45) aber nur noch
Labels ab („früher: auf den Review-Branch“). Folge: Bei ChatGPT-Reviews (auch Gesamtreview) läuft die Schleife bis zur
Obergrenze, ohne den Review-PR zu sehen. Beleg: `git grep -n 'issues?labels' 28de8b7 -- CLAUDE.md docs/vorlagen` → nur
Labelabfragen; `git grep -n -i 'codex.*label\|label.*codex' 28de8b7` → kein Treffer.
Vorschlag: Schleife fragt zusätzlich offene PRs mit Head `review/*` ab (wie bisher), oder im Abschnitt „Aufrufe für
ChatGPT/Codex“ festlegen, dass die Leitstelle das Label beim Auswerten setzt und auf den Review-Branch wartet.

**R-02 (P2) – Lebenszyklus der Labels nicht beschrieben.** Nur `review-ja`/`review-nein` haben eine Entfernregel
(„das jeweils andere entfernen“). Wer `leitstelle-delta`, `beleg-strittig` und `spur-wartet` nach Erledigung entfernt,
und ob der Fixlauf `review-nein` beim Push eines neuen Heads entfernt, steht nirgends. Da die Schleife
`issues?labels=<label>&state=open` abfragt, meldet sie bereits erledigte PRs sofort wieder; ein Lauf ohne Chatverlauf kann
alt und neu nicht unterscheiden (z. B. PR mit `review-nein` aus dem Erstreview und `leitstelle-delta` aus dem Fixlauf
gleichzeitig). Beleg: `git grep -n -i 'entfern' 28de8b7 -- docs/prozess docs/vorlagen CLAUDE.md` → nur
[zusammenarbeit.md:106](https://github.com/kaaasi/eishockey-manager-redone/blob/28de8b771b38ef52ff08303c1c08dd520a0f9de4/docs/prozess/zusammenarbeit.md#L106) und review-anfrage.md:54, beide zu `review-*`.
Vorschlag: ein Satz in §5 Punkt 8: „Wer ein Label abarbeitet, entfernt es (Leitstelle: `leitstelle-delta`,
`beleg-strittig`, `spur-wartet`); der Fixlauf entfernt `review-nein` mit dem Push des neuen Heads.“ Die Schleife meldet
dann nur, was gerade ansteht.

**R-03 (P3) – Label-Recht des Reviewers nicht an allen Stellen nachgezogen.** Rollentabelle
[zusammenarbeit.md:14](https://github.com/kaaasi/eishockey-manager-redone/blob/28de8b771b38ef52ff08303c1c08dd520a0f9de4/docs/prozess/zusammenarbeit.md#L14) („Darf nicht: … den geprüften PR ändern“) und Schritt 5 von
review-anfrage.md nennen die Ausnahme nicht; der Kurzaufruf-Block oben in `review-anfrage.md` autorisiert nur Branch, Commit
und PR – dieser Lauf brauchte die Label-Erlaubnis als Zusatz im Prompt. Vorschlag: „…, Label `review-ja`/`review-nein`
am PR #<nr> setzen“ in den Kurzaufruf, „außer Label“ in die Tabelle.

**R-04 (P3) – Mehrere Labels in einer Abfrage sind UND-verknüpft.** `issues?labels=a,b` liefert nur Issues mit **allen**
Labels (GitHub-REST). [leitstelle-start.md:45](https://github.com/kaaasi/eishockey-manager-redone/blob/28de8b771b38ef52ff08303c1c08dd520a0f9de4/docs/vorlagen/leitstelle-start.md#L45) („fragt nur diese Labels ab
(`…labels=…`)“) lädt zur Komma-Liste ein. Vorschlag: „je Label eine Abfrage“.

**R-05 (P3) – Fixlauf legt Nachprüfung ohne Dublettenprüfung und fest mit Opus an.**
[spur-start.md:32](https://github.com/kaaasi/eishockey-manager-redone/blob/28de8b771b38ef52ff08303c1c08dd520a0f9de4/docs/vorlagen/spur-start.md#L32) / [zusammenarbeit.md:104](https://github.com/kaaasi/eishockey-manager-redone/blob/28de8b771b38ef52ff08303c1c08dd520a0f9de4/docs/prozess/zusammenarbeit.md#L104):
Anders als bei Fortsetzungen (spur-start „Teile und Fortsetzung“ Schritt 5) fehlt „vorher `list_triggers` prüfen“; ein
wiederholter Fixlauf legt so zwei Nachprüfungen an. „Modell Claude Opus 5.5“ deckt sich mit dem Standardmodell, widerspricht
aber für Doku-PRs der Modellwahl (Sonnet für Nachprüfungen reiner Doku-PRs); zudem kann der Lauf das Modell nicht setzen
(spur-start Schritt 5). Vorschlag: Dublettenprüfung übernehmen, Modell „nach §5 Modellwahl, ggf. ‚Modell prüfen‘ in der
Übergabe“.

**R-06 (P3) – „und merged“ ohne Verweis auf §6.** [zusammenarbeit.md:106](https://github.com/kaaasi/eishockey-manager-redone/blob/28de8b771b38ef52ff08303c1c08dd520a0f9de4/docs/prozess/zusammenarbeit.md#L106) liest sich
wie Merge auf `review-ja` hin; `leitstelle-start.md` Schritt 4 hält §6 fest, hier fehlt nur „nach §6“.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `git rev-parse pr708` = Head, Merge-Base = Base | ✓ `28de8b7…`, `b130464…` |
| `git diff --stat origin/main...pr708` | 7 Dateien, nur `*.md` (CI entfällt zu Recht) |
| Diff vollständig gelesen; Kontext §1, §3–§6 `zusammenarbeit.md`, `leitstelle-start.md`, `spur-start.md`, `review-anfrage.md`, `CLAUDE.md` | gelesen |
| Labels im Repo (`gh api …/labels`) | `review-ja`, `review-nein`, `leitstelle-delta`, `beleg-strittig`, `spur-wartet`, `P3` vorhanden |
| Abgleich PR-Beschreibung ↔ Diff (Punkte 1–6) | vollständig, nichts Zusätzliches |
| Build, `tools/pruefen.sh` | nicht ausgeführt – reine Doku, laut Auftrag nicht nötig |

Akzeptierte `[APPROX]`: keine (Doku-PR). Annahme: Codex kann ohne Netz keine Labels setzen (aus review-anfrage.md
„Aufrufe für ChatGPT/Codex“ abgeleitet, nicht in Codex erprobt).
