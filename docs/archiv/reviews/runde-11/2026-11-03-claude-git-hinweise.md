# Unabhängiges Review: PR #476 `CLAUDE.md` – Hinweise zu Git-Tracking und Warten auf geplante Läufe

Base `121c99344a30130a41a055781ab3358d9f8e804e` · Head `d1fc6aa18cd2bb0808b8e4db46ab254e3ba4d1db` · Reviewer: Claude
(neue Sitzung, geplanter Lauf, ohne Beteiligung am PR). Stand: Klon, Head per `git fetch` geholt, Diff gelesen
(`git diff --stat`: nur `CLAUDE.md`, +8/−0). Kein Build, kein `tools/pruefen.sh` (Doku-Änderung, laut Auftrag nicht nötig).
Die Git-Befehle wurden in einem frischen Wegwerf-Repo nachvollzogen. Keine Prüfung der Aussage zu `send_later`/Hintergrundschleife
in der Umgebung der Leitstelle (nur Codelesen/Plausibilität).

## 1. Merge-Empfehlung

**Ja** für genau Head `d1fc6aa18cd2bb0808b8e4db46ab254e3ba4d1db` (Stand: Codelesen plus Nachvollzug der Befehle, kein Testnachweis
nötig). Nur `CLAUDE.md` ist geändert, kein Widerspruch zu `AGENTS.md` oder `docs/prozess/`. Ein P2 (Befehl mit wörtlichem Muster
schlägt fehl) ist leicht korrigierbar und nicht merge-hindernd, sollte aber vor oder kurz nach dem Merge behoben werden.
Merge nur durch Leitstelle/Nutzer nach `zusammenarbeit.md` §6.

## 2. Befunde

| ID | Prio | Ort | Kurz |
|---|---|---|---|
| R476-01 | P2 | [`CLAUDE.md` Z. 53–54](https://github.com/kaaasi/eishockey-manager-redone/blob/d1fc6aa18cd2bb0808b8e4db46ab254e3ba4d1db/CLAUDE.md#L53-L54) | `--unset-all … '<muster>'` nimmt einen regulären Ausdruck; die wörtliche Fetch-Regel `+refs/heads/…` wird abgelehnt |
| R476-02 | P3 | [`CLAUDE.md` Z. 59–61](https://github.com/kaaasi/eishockey-manager-redone/blob/d1fc6aa18cd2bb0808b8e4db46ab254e3ba4d1db/CLAUDE.md#L59-L61) | Warteschleife ohne Hinweis auf Abbruchbedingung/Intervall |

**R476-01 (P2).** Gegenbeispiel (frisches Repo, `git config --add remote.origin.fetch '+refs/heads/foo:refs/remotes/origin/foo'`):

```
git config --unset-all remote.origin.fetch '+refs/heads/foo:refs/remotes/origin/foo'
→ error: invalid pattern: +refs/heads/foo:refs/remotes/origin/foo   (Rückgabe 6, Regel bleibt stehen)
git config --unset-all remote.origin.fetch 'refs/heads/foo'
→ Rückgabe 0, Regel entfernt, Standardregel `+refs/heads/*:refs/remotes/origin/*` bleibt
```

Der zweite Parameter von `--unset-all` ist ein Regex (führendes `+` ungültig, `*` und `.` wirken als Metazeichen). Wer die
Regel aus der Fehlermeldung wörtlich als „Muster“ einsetzt, scheitert. Vorschlag: „`<muster>` ist ein Regex, z. B. nur
`refs/heads/<b>:` ohne führendes `+`; Substring-Treffer prüfen mit `git config --get-all remote.origin.fetch`“.

**R476-02 (P3).** Die Schleife wird nur beschrieben; ein Hinweis auf sparsames Intervall (AGENTS §7) und eine Obergrenze
fehlen. Nicht merge-hindernd; der Satz „Nach einem Neustart … verloren“ ist korrekt und nützlich.

Bestätigt (kein Befund): `git config branch.<b>.remote origin` und `branch.<b>.merge refs/heads/<b>` setzen das Tracking
(nachgestellt, `git config --get-regexp '^branch'` zeigt beides); die Fetch-Regel `+refs/heads/<b>:refs/remotes/origin/<b>`
ist die übliche Form für Einzelbranch-Klone. Das Abbrechen von `git fetch` bei gelöschtem Remote-Ref auf einer expliziten
Fetch-Regel ist bekanntes Git-Verhalten. Verträglich mit CLAUDE.md-Abschnitt „GitHub aus der Sitzung“ (GraphQL gesperrt,
`add_repo` im Lauf), `AGENTS.md` §2/§3/§7 und `docs/prozess/umgebung.md` (kein Umgehen von Proxy/Sandbox: nicht berührt;
`grep` in `docs/prozess`, `docs/vorlagen` findet keine widersprechenden Aussagen zu `send_later`/Warteschleifen).

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `gh api …/pulls/476 --jq .head.sha` | `d1fc6aa…` (= genannter Head), Base `121c993…`, 1 Datei geändert |
| `git diff --stat 121c993 d1fc6aa` | nur `CLAUDE.md`, +8 |
| `git config --unset-all` mit wörtlicher Regel / Teilmuster | Rückgabe 6 bzw. 0 (siehe R476-01) |
| `git config branch.b.remote/merge` | wie beschrieben |
| `tools/pruefen.sh` | nicht ausgeführt (laut Auftrag nicht nötig; CI genügt) |

Akzeptierte `[APPROX]`: keine (kein Code). Annahme, nicht geprüft: Aussage zur Unzuverlässigkeit von `send_later` in der
Leitstellen-Umgebung.
