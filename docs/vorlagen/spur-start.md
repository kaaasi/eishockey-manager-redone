# Vorlage: Start einer Spur

**Kurzaufruf** (das ist alles, was der Nutzer in den neuen Chat schreibt):

> Repo kaaasi/eishockey-manager-redone. Arbeite nach `docs/vorlagen/spur-start.md`. Spur: `<name>`. Agent: `<Claude Code | ChatGPT | …>`.
> Besonderheiten: `<nur falls nötig, sonst weglassen>`

## Ablauf für den Agenten

1. Lies `AGENTS.md`, `docs/prozess/zusammenarbeit.md` (Abschnitte 2–4) und in `docs/arbeitspakete.md` **nur** die aktuelle
   Runde und den Abschnitt deiner Spur. Archiv und alte Reviews nur bei konkretem Bedarf.
2. Einrichten nach `AGENTS.md` Abschnitt 3 (Claude Code: zusätzlich `CLAUDE.md`). Originaldaten nach `gamedata/`.
3. Gibt es für die Spur schon einen offenen PR mit Bearbeiter? Dann nur nach Übergabekommentar weiterarbeiten,
   sonst den Nutzer fragen. Neu: Branch `paket/<spur>[-<teil>]` von aktuellem `main`, sofort Draft-PR mit
   `Bearbeiter: <Agent>` und Base-SHA.
4. Arbeiten nach den harten Regeln: reproduzieren → roter Regressionstest → Fix; Dateigrenzen; `[APPROX]`/`[ORIGINAL]`;
   Mehrsaisonprobe, wenn Kader, Geld oder Saisonwechsel berührt sind.
5. Vor jedem Push: `git fetch origin && git rebase origin/main`, `tools/pruefen.sh --schnell` (siehe `AGENTS.md` §5).
6. Fertig: PR-Beschreibung nach `.github/pull_request_template.md`, Draft aufheben, Status im eigenen Abschnitt von
   `docs/arbeitspakete.md`. Mehrere kleine PRs sind besser als ein großer.
7. **Immer zum Schluss – nach erledigter Abarbeitung wie bei Unterbrechung:** Übergabekommentar im PR nach
   `docs/handoffs/VORLAGE.md` (Head-SHA, erledigt/offen/nächster Schritt, Testtabelle, Risiken, `Bearbeiter: frei`),
   damit ein neuer Chat ohne Verlauf übernehmen kann.
8. Antwort an den Nutzer **knapp** (`AGENTS.md` §7): PR-Links, Ergebnis in einem Satz je PR, offene Fragen.
