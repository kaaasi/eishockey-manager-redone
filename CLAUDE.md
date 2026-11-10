# Hinweise für Claude Code

Die Projektregeln stehen werkzeugneutral in AGENTS.md und gelten vollständig:

@AGENTS.md

Hier stehen nur Hinweise, die Claudes Arbeitsumgebung betreffen. Bei Widerspruch gilt AGENTS.md.

## Umgebung einrichten (jede neue Sitzung)

```bash
git clone https://github.com/kaaasi/eishockey-manager-redone   # bzw. vorhandenen Klon nutzen
cd eishockey-manager-redone
```

- **Originaldateien:** Repo `kaaasi/eishockey-manager-referenz` per `add_repo` (access `read`, Leitstelle immer `push`) einbinden und nur
  `original/` laden (Sparse, nicht die ~650 MB Videobilder). Details: [docs/originaldateien.md](docs/originaldateien.md).
  Rückfallweg ohne Repo-Zugriff: ZIP aus den Dateien des claude.ai-Projekts „Eishockey Manager Redone“ (Projects-Werkzeug
  `project_info` → `project_read`) in einen eigenen Ordner entpacken; fehlt auch die, den Nutzer bitten, sie anzuhängen.
- **SDK und NuGet-Pakete:** Microsoft- und NuGet-Server sind aus der Arbeitsumgebung gesperrt, GitHub nicht.

  ```bash
  tools/claude/fetch-reference.sh original                    # Originaldateien nach ~/referenz/original/
  tools/claude/setup-workspace.sh ~/referenz/original/gamedata # SDK + Pakete aus dem Release "claude-werkzeuge", verlinkt gamedata/
  source ~/claude-tools/env.sh
  tools/pruefen.sh
  ```

  Fehlt das Release oder sind Pakete veraltet: `gh workflow run werkzeuge.yml -R kaaasi/eishockey-manager-redone`
  (läuft auch automatisch, wenn sich `*.csproj` auf `main` ändern).
- **Referenzbilder:** Repo `kaaasi/eishockey-manager-referenz` per `add_repo` (access `read`, Leitstelle immer `push`) einbinden, dann
  `tools/claude/fetch-reference.sh [oberflaeche] [karriere] [bundestrainer] [folgen|folgeNN] [eisbaeren|eisbaeren_fNN]
  [duell|duell_…] [speedrun] [uebersicht]` → `~/referenz/` (Reihen und Fundlisten: `docs/referenz-videos.md`). Ein
  vorhandener Klon wird ergänzt; ist er veraltet, bricht das Skript mit Rückgabe 3 ab – dann `~/referenz` löschen und
  neu laden.

## GitHub aus der Sitzung

- GraphQL ist gesperrt → `gh api` (REST) statt `gh pr …`. PR anlegen:
  `gh api repos/kaaasi/eishockey-manager-redone/pulls -f title=… -f head=<branch> -f base=main -f body=…`.
- Draft-Status: „Ready for review“ mit `gh api -X POST repos/kaaasi/eishockey-manager-redone/pulls/<nr>/ccr/ready_for_review`,
  zurück mit `…/ccr/convert_to_draft` (GraphQL-Ersatz der Sitzung, erprobt 04.11.2026).
- Nur repository-bezogene Endpunkte sind erreichbar (keine Nutzer-, Billing- oder Projects-Endpunkte).
- Releases lassen sich aus der Sitzung nicht anlegen, nur über Workflows.
- Mergen (nur als Leitstelle, nach der Merge-Prüfung in `docs/prozess/zusammenarbeit.md`) immer mit dem geprüften
  Head-SHA: `gh api -X PUT repos/kaaasi/eishockey-manager-redone/pulls/<nr>/merge -f sha=<head> -f merge_method=merge`.
- **Leitstelle:** immer Lese- und Schreibzugriff auf beide Repos (`add_repo` für `eishockey-manager-redone` und
  `eishockey-manager-referenz`, jeweils access `push`; Vorgabe Nutzer 01.11.2026), z. B. um Spielstände und Bilder im
  Referenz-Repo abzulegen (dort per PR, Branch `daten/…`).
- **Geplante Läufe anlegen** (Leitstelle): vorher beide Repos per `add_repo` in der eigenen Sitzung einbinden – der Lauf
  erbt nur diese; Ablauf `docs/vorlagen/leitstelle-start.md` Schritt 7. Im Lauf selbst ist `add_repo` meist nicht
  verfügbar (auch nicht per `ToolSearch`); maßgeblich ist das Einbinden durch die anlegende Sitzung.
- Commit-Trailer für Claude: `Co-Authored-By: Claude <noreply@anthropic.com>` (bzw. die Zeilen aus der Sitzungsvorgabe).
- **Git im flachen Klon (Erfahrung Leitstelle Runde 11):** Fetch-Regeln für einzelne Branches (`remote.origin.fetch`) brechen
  `git fetch` ab, sobald der Branch auf GitHub gelöscht ist („couldn't find remote ref“) – dann
  `git config --unset-all remote.origin.fetch 'refs/heads/<b>:'` (zweites Argument ist ein regulärer Ausdruck: ohne führendes `+`;
  Treffer vorher mit `git config --get-all remote.origin.fetch` prüfen). Meldet der Stop-Hinweis „unpushed commits / no remote branch“, obwohl
  gepusht ist, fehlt nur das Tracking: `git config branch.<b>.remote origin`, `branch.<b>.merge refs/heads/<b>` und eine
  Fetch-Regel `+refs/heads/<b>:refs/remotes/origin/<b>` eintragen, dann `git fetch`.
- **Ende geplanter Läufe (Erfahrung Leitstelle Runde 12):** Läufe melden ihr Ende nicht. Vor jeder Statusauskunft
  `list_triggers` (Feld `last_run.status`/`finished_at`) prüfen, nicht nur den PR; ein Draft-PR heißt nicht „läuft noch“.
  Reviews durch ChatGPT brauchen Codex mit Netzzugang; Titel und Head-SHA der Review-Datei vor der Auswertung prüfen.
- **Auf geplante Läufe warten:** `send_later` stand nicht verlässlich bereit; bewährt hat sich eine Warteschleife im
  Hintergrund (`run_in_background`), die per `gh api …/issues?labels=<label>&state=open` auf die Labels aus
  `leitstelle-start.md` Schritt 10 wartet (früher: auf den Review-Branch) – Intervall 60 s,
  Obergrenze rund zwei Stunden (z. B. `for i in $(seq 1 115)`), danach abbrechen und den Lauf prüfen (`list_triggers`). Nach einem Neustart
  der Arbeitsumgebung sind solche Schleifen verloren und neu anzulegen.

## Rolle in diesem Projekt

Claude Code ist derzeit die **Leitstelle** (siehe `docs/prozess/zusammenarbeit.md`, Ablauf `docs/vorlagen/leitstelle-start.md`):
verteilt Pakete, integriert, gibt dem Nutzer die Review-Anfrage für ChatGPT als ausgefüllten Kurzaufruf und wertet die
Reviews aus `docs/reviews/` aus. Antworten knapp nach `AGENTS.md` §7.
