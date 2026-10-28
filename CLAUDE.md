# Hinweise für Claude Code

Die Projektregeln stehen werkzeugneutral in AGENTS.md und gelten vollständig:

@AGENTS.md

Hier stehen nur Hinweise, die Claudes Arbeitsumgebung betreffen. Bei Widerspruch gilt AGENTS.md.

## Umgebung einrichten (jede neue Sitzung)

```bash
git clone https://github.com/kaaasi/eishockey-manager-redone   # bzw. vorhandenen Klon nutzen
cd eishockey-manager-redone
```

- **Originaldateien:** Zuerst das private Repo `kaaasi/eishockey-manager-referenz` lesend einbinden.
  Dort liegen `original/Eishockey-Manager_DOS_DE.zip` und die entpackten Dateien unter `original/gamedata/`.
  Ablagestand und Abruf: [docs/originaldateien.md](docs/originaldateien.md). Den geladenen `gamedata`-Ordner
  direkt verwenden; ein erneutes Hochladen im Chat ist bei vorhandenem Repo-Zugriff nicht nötig.
- **SDK und NuGet-Pakete:** Microsoft- und NuGet-Server sind aus der Arbeitsumgebung gesperrt, GitHub nicht.

  ```bash
  tools/claude/setup-workspace.sh /workspace/shared/eishockey-manager-referenz/original/gamedata
  # SDK + Pakete aus dem Release "claude-werkzeuge", verlinkt gamedata/
  source ~/claude-tools/env.sh
  tools/pruefen.sh
  ```

  Fehlt das Release oder sind Pakete veraltet: `gh workflow run werkzeuge.yml -R kaaasi/eishockey-manager-redone`
  (läuft auch automatisch, wenn sich `*.csproj` auf `main` ändern).
- **Referenzbilder:** Repo `kaaasi/eishockey-manager-referenz` per `add_repo` (access `read`) einbinden, dann
  `tools/claude/fetch-reference.sh [oberflaeche] [karriere] [uebersicht]` → `~/referenz/`.

## GitHub aus der Sitzung

- GraphQL ist gesperrt → `gh api` (REST) statt `gh pr …`. PR anlegen:
  `gh api repos/kaaasi/eishockey-manager-redone/pulls -f title=… -f head=<branch> -f base=main -f body=…`.
- Nur repository-bezogene Endpunkte sind erreichbar (keine Nutzer-, Billing- oder Projects-Endpunkte).
- Releases lassen sich aus der Sitzung nicht anlegen, nur über Workflows.
- Mergen (nur als Leitstelle, nach der Merge-Prüfung in `docs/prozess/zusammenarbeit.md`) immer mit dem geprüften
  Head-SHA: `gh api -X PUT repos/kaaasi/eishockey-manager-redone/pulls/<nr>/merge -f sha=<head> -f merge_method=merge`.
- Commit-Trailer für Claude: `Co-Authored-By: Claude <noreply@anthropic.com>` (bzw. die Zeilen aus der Sitzungsvorgabe).

## Rolle in diesem Projekt

Claude Code ist derzeit die **Leitstelle** (siehe `docs/prozess/zusammenarbeit.md`, Ablauf `docs/vorlagen/leitstelle-start.md`):
verteilt Pakete, integriert, gibt dem Nutzer die Review-Anfrage für ChatGPT als ausgefüllten Kurzaufruf und wertet die
Reviews aus `docs/reviews/` aus. Antworten knapp nach `AGENTS.md` §7.
