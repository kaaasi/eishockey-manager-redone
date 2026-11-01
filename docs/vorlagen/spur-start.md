# Vorlage: Start einer Spur

**Kurzaufruf** (das ist alles, was der Nutzer in den neuen Chat schreibt):

> Repo kaaasi/eishockey-manager-redone. Arbeite nach `docs/vorlagen/spur-start.md`. Spur: `<name>`. Agent: `<Claude Code | ChatGPT | …>`.
> Besonderheiten: `<nur falls nötig, sonst weglassen>`

**Geplanter Lauf mit Teilen** (Startaufruf der Leitstelle; die Fortsetzung hängt `Fortsetzung <n> von 3, Übergabe in PR #<nr>.` an):

> Repo kaaasi/eishockey-manager-redone. Arbeite nach `docs/vorlagen/spur-start.md`. Spur: `<name>`. Agent: Claude Code.
> Unbeaufsichtigt, keine Rückfragen. Teile und Selbstfortsetzung nach Abschnitt „Teile und Fortsetzung“; das Anlegen
> einer Fortsetzung per `create_trigger` ist autorisiert (höchstens 3 je Spur). Modell der Spur: `<modell>`.

## Ablauf für den Agenten

1. Lies `AGENTS.md`, `docs/prozess/zusammenarbeit.md` (Abschnitte 2–4) und in `docs/arbeitspakete.md` **nur** die aktuelle
   Runde und den Abschnitt deiner Spur. Archiv und alte Reviews nur bei konkretem Bedarf.
2. Einrichten nach `AGENTS.md` Abschnitt 3 (Claude Code: zusätzlich `CLAUDE.md`). Originaldaten nach `gamedata/`.
3. Gibt es für die Spur schon einen offenen PR mit Bearbeiter? Dann nur nach Übergabekommentar weiterarbeiten,
   sonst den Nutzer fragen. Neu: Branch `paket/<spur>[-<teil>]` von aktuellem `main`, sofort Draft-PR mit
   `Bearbeiter: <Agent>` und Base-SHA. **Geplante Läufe mit Teilen:** statt dieses Schritts und Schritt 5 gilt der Abschnitt
   „Teile und Fortsetzung“ unten.
4. Arbeiten nach den harten Regeln: reproduzieren → roter Regressionstest → Fix; Dateigrenzen; `[APPROX]`/`[ORIGINAL]`;
   Mehrsaisonprobe, wenn Kader, Geld oder Saisonwechsel berührt sind.
5. Vor jedem Push: `git fetch origin && git rebase origin/main`, `tools/pruefen.sh --schnell` (siehe `AGENTS.md` §5).
6. Fertig: PR-Beschreibung nach `.github/pull_request_template.md`, Draft aufheben, Status im eigenen Abschnitt von
   `docs/arbeitspakete.md`. Mehrere kleine PRs sind besser als ein großer.
7. **Immer zum Schluss – nach erledigter Abarbeitung wie bei Unterbrechung:** Übergabekommentar im PR nach
   `docs/handoffs/VORLAGE.md` (Head-SHA, erledigt/offen/nächster Schritt, Testtabelle, Risiken, `Bearbeiter: frei`),
   damit ein neuer Chat ohne Verlauf übernehmen kann.
8. Antwort an den Nutzer **knapp** (`AGENTS.md` §7): PR-Links, Ergebnis in einem Satz je PR, offene Fragen.

## Teile und Fortsetzung (geplante Läufe)

Gilt, wenn die Spur in `docs/arbeitspakete.md` eine geordnete Liste von Teilen hat und der Startaufruf die Fortsetzung erlaubt.

1. **Stand ermitteln:** offene PRs der Spur (`paket/<spur>-<n>`) und den letzten Übergabekommentar lesen. Dort steht
   „Fortsetzungen verbraucht: n von 3“. Nennt der Startaufruf einen anderen Zähler, gilt der höhere. Steht in einem offenen
   PR der Spur ein Bearbeiter ≠ `frei` ohne Übergabekommentar (abgebrochener oder noch laufender Lauf), nichts ändern und
   wie blockiert verfahren (Schritt 4, ohne Code-Änderung).
2. **Stapelregel:** Teil 1 von aktuellem `main`, PR-Base `main`. Teil n > 1: Ist der PR des vorigen Teils noch offen, Branch
   von `paket/<spur>-<n-1>` und PR-Base = dieser Branch; ist er gemergt, von `main`. Rebase vor dem Push (Schritt 5): nur der
   unterste offene Teil auf `origin/main`, jeder höhere auf seinen Base-Branch (`git rebase origin/paket/<spur>-<n-1>`);
   danach `git push --force-with-lease` nur auf eigene Branches.
3. **Teil abschließen:** grünes `tools/pruefen.sh --schnell`, PR „Ready for review“, Übergabekommentar mit „Fortsetzungen
   verbraucht: n von 3“. Dann den nächsten Teil im selben Lauf beginnen, solange Zeit bleibt.
4. **Teil blockiert** (kein grünes `--schnell`, Beleg fehlt, fremde Datei nötig): Teil als Draft-PR mit Übergabe lassen,
   Label `spur-wartet` setzen, im Übergabekommentar „blockiert – Entscheidung Nutzer“ und den Grund. **Keine Fortsetzung**
   anlegen und den Teil nicht erneut versuchen.
5. **Fortsetzung anlegen** (nur wenn Teile offen sind, nichts blockiert ist und n < 3):
   - vorher mit `list_triggers` prüfen, ob für diese Spur schon eine geplante Aufgabe aussteht; dann keine weitere anlegen;
   - `create_trigger` mit `run_once_at` etwa 10 Minuten später, Name `Spur <name> – Fortsetzung <n+1>`, Prompt = Startaufruf
     oben mit `Fortsetzung <n+1> von 3, Übergabe in PR #<oberster PR>.`;
   - das Modell lässt sich beim Anlegen nicht wählen und darf vom Lauf nicht umgestellt werden: im Übergabekommentar
     „Modell der Fortsetzung prüfen (Soll: <modell>)“ vermerken – Leitstelle bzw. Nutzer stellen es um;
   - die Aufgabe (Name, Zeitpunkt) im Übergabekommentar nennen.
6. **Kettenende:** Sind alle Teile fertig, oder ist n = 3 und Teile bleiben offen: keine Fortsetzung; Label `spur-wartet`
   auf den obersten PR und im Übergabekommentar „Spur fertig – Integration“ bzw. „Fortsetzungen erschöpft – Entscheidung
   Nutzer“. Die Leitstelle sieht offene PRs mit `spur-wartet` durch.
7. Nie eine fremde Spur fortsetzen oder starten.
