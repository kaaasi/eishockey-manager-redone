# Nachprüfung Review #355 – Abschluss Runde 9

- **PR:** #355 (`prozess/runde-9-abschluss`) · Nachprüfung von `docs/reviews/2026-11-01-abschluss-runde-9.md`
- **Base:** `acc01a3404fc40845f8391ae31a169084e7f4121` (laut REST) · **Head:** `e2619f75c1bafeb1d3b0c2a24e8ed3bd02236779`
  (= `refs/heads/prozess/runde-9-abschluss` per `git ls-remote`) · **bisher geprüft:** `328c932`
- **Delta:** `328c932..e2619f7` = ein Merge-Commit (Eltern `328c932`, `acc01a3`) mit Konfliktauflösung und den
  Änderungen zu AR9-01/-03/-04
- **Reviewer:** Claude Code (Claude Opus 5.5), geplanter Lauf, eigene Sitzung ohne Mitarbeit an #355
- **Art:** nur Codelesen (reiner Doku-PR; kein Build, keine Tests)

## 1. Merge-Empfehlung

**Ja, für genau diesen Head – nur Codelesen.** AR9-01 bis AR9-04 sind erledigt. Gegen das aktuelle `main` (`8316263`, nach
#360/#361) lässt sich der Head konfliktfrei zusammenführen. Das Ergebnis unterscheidet sich von `main` nur in den vier
Doku-Dateien des PRs (+23/−5). Die Konfliktauflösung übernimmt den Stand von `main` vollständig. Neue Befunde gibt es nicht.

## 2. Befunde

### AR9-01 (P2) – Konflikt mit `main`: **erledigt**

Gegenbeispiel erneut ausgeführt: `git merge-tree --write-tree origin/main e2619f7` → Tree `7327fe9…`, Rückgabe 0, kein
`CONFLICT` (`origin/main` = `8316263`). `git diff acc01a3 e2619f7 -- docs/anforderungen/zusatzfunktionen.md` zeigt nur fünf
angehängte Zeilen am Ende von Abschnitt C. „Verjüngung des Spielerpools“ und die Neufassung von „Annahmewahrscheinlichkeit“
von `main` sind unverändert enthalten. `git diff origin/main 7327fe9 --stat` ergibt genau die vier Dateien des PRs.

### AR9-02 (P3) – `Fixes #349`: **erledigt**

Die PR-Beschreibung nennt jetzt `Refs #349 (bleibt offen, Label „nach-v1.0“; Review AR9-02)`. `closed_by_pull_requests` von
#349 ist leer. **Erwartet offen, bestätigt:** #349 ist offen und trägt die Labels `befund`, `P2` und `nach-v1.0`. Die
Head-SHA-Zeile der PR-Beschreibung nennt noch `328c932`. Das ist kein Befund, die Merge-Prüfung nutzt den Head aus REST.

### AR9-03 (P3) – Liste für Runde 11: **erledigt**

[`arbeitspakete.md` Z. 169–170](https://github.com/kaaasi/eishockey-manager-redone/blob/e2619f75c1bafeb1d3b0c2a24e8ed3bd02236779/docs/arbeitspakete.md#L169-L170):
#358 steht bei den P3 der Integration, #357 bei der Probenrobustheit. Zusätzlich aufgenommen sind #362, #363 und #364 (PSW-01
bis -03 aus dem Review zu #360). Alle drei sind offen, tragen `befund` und `P3` und nennen im Text „Runde 11 (P3)“.

### AR9-04 (P3) – Wortlaut: **erledigt**

- [`spur-start.md` Z. 23–25](https://github.com/kaaasi/eishockey-manager-redone/blob/e2619f75c1bafeb1d3b0c2a24e8ed3bd02236779/docs/vorlagen/spur-start.md#L23-L25):
  Ersetzt wird jetzt der ganze Satz „Teile und Selbstfortsetzung … (höchstens 3 je Spur).“ Das passt zum Kurzaufruf in
  Z. 11–12. „Selbstfortsetzung“ bleibt damit nicht mehr stehen.
- [`zusatzfunktionen.md` Z. 71–75](https://github.com/kaaasi/eishockey-manager-redone/blob/e2619f75c1bafeb1d3b0c2a24e8ed3bd02236779/docs/anforderungen/zusatzfunktionen.md#L71-L75):
  Dort steht jetzt „Spieltag von Pokal, Play-offs oder Aufstiegsrunde“ und „sonst je nach Anzeigeschalter des Wettbewerbs
  (0x3702E–0x37085)“. Statt „Pokalspiele ohne Manager“ heißt es „Spiele ohne Manager“. Das ist wörtlich der Vorschlag.

Die übrigen Teile des PRs (R9F-02 bis -04, `leitstelle-start.md`) sind im Delta unverändert, nur neu gemischt. Die Bewertung
aus dem ersten Review gilt weiter.

## 3. Ausgeführte Prüfungen

```
git ls-remote origin | grep runde-9-abschluss          → e2619f75… refs/heads/prozess/runde-9-abschluss
git log -1 --format='%P' e2619f7                       → 328c932…  acc01a3…
git merge-base --is-ancestor acc01a3 origin/main       → ja (main = 8316263)
git merge-tree --write-tree origin/main e2619f7        → 7327fe920d58…, rc=0 (AR9-01)
git diff origin/main 7327fe9 --stat                    → 4 Dateien, +23/−5
git diff 328c932 e2619f7 -- <4 Doku-Dateien>; git diff acc01a3 e2619f7
Zeilenlängen der hinzugefügten Zeilen (Zeichen, nicht Byte): höchstens 126
GitHub-Connector: PR #355 (Beschreibung, Head, mergeable_state clean); Issues #349, #362, #363, #364 (Status, Labels)
```

Kein Build und keine Tests (reiner Doku-PR, nur Codelesen). Das Review liefert keinen Testnachweis nach `zusammenarbeit.md`
§6 Nr. 4. Für Doku ist er nicht nötig.

**Akzeptierte `[APPROX]`:** unverändert #349 (je Paarung statt je Spieltag), Entscheidung des Nutzers, Einstellung nach v1.0.
**Annahmen:** #357 und #358 aus dem ersten Review übernommen (offen, `P3`). Hier wurde nur ihre Aufnahme in die Liste
geprüft.
