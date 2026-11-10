# Vorlage: Start einer Spur

**Kurzaufruf** (das ist alles, was der Nutzer in den neuen Chat schreibt):

> Repo kaaasi/eishockey-manager-redone. Arbeite nach `docs/vorlagen/spur-start.md`. Spur: `<name>`. Agent: `<Claude Code | ChatGPT | …>`.
> Besonderheiten: `<nur falls nötig, sonst weglassen>`

**Geplanter Lauf mit Teilen** (Startaufruf der Leitstelle; die Fortsetzung hängt `Fortsetzung <n> von 3, Übergabe in PR #<nr>.` an):

> Repo kaaasi/eishockey-manager-redone. Arbeite nach `docs/vorlagen/spur-start.md`. Spur: `<name>`. Agent: Claude Code.
> Unbeaufsichtigt, keine Rückfragen. Teile und Selbstfortsetzung nach Abschnitt „Teile und Fortsetzung“; das Anlegen
> einer Fortsetzung per `create_trigger` ist autorisiert (höchstens 3 je Spur). Modell der Spur: `<modell>`.
> Zugriff zuerst prüfen: `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD`. Scheitert das,
> `add_repo` (ggf. erst per `ToolSearch` mit `select:mcp__claude-code-remote__add_repo` laden; meist nicht vorhanden) für
> `kaaasi/eishockey-manager-referenz` (access read) und erneut prüfen; bleibt es unerreichbar, sofort blockiert melden (genaue
> Fehlermeldungen von `ls-remote` und `ToolSearch`), keine Code-Arbeit. Meldung: Benachrichtigung an den Nutzer; gibt es einen
> PR der Spur, dort Kommentar und Label `spur-wartet`; sonst keinen PR anlegen.

Der Zugriffsabsatz steht in **jedem** Startaufruf eines geplanten Spurlaufs (Spur, Fortsetzung) wörtlich; eine Fortsetzung
übernimmt ihn unverändert. Reviews: nach `review-anfrage.md` (nur wenn Daten nötig; „keine Code-Arbeit“ gilt dort als
„kein Review“). Voraussetzung auf Seiten der Leitstelle: [leitstelle-start.md](leitstelle-start.md), „Geplante Läufe“.
Geplante Läufe erben von der Leitstelle auch Schreibrecht auf `kaaasi/eishockey-manager-referenz`; Spuren und Reviews
schreiben dort trotzdem nie (nur lesen). Fehlen Daten oder Bilder, im PR melden – ablegen nur Leitstelle oder Nutzer.

**Einzelner Folgeteil ohne Fortsetzung:** Kurzaufruf „Geplanter Lauf mit Teilen“, aber statt „Teile und Selbstfortsetzung …
(höchstens 3 je Spur).“ der Satz „Teile nach Abschnitt ‚Teile und Fortsetzung‘; nur Teil <n>, keine Fortsetzung anlegen.“
Es gilt der Abschnitt „Teile und Fortsetzung“ ohne Schritt 5 (R9F-02).

**Fixlauf nach einem Review** (geplanter Lauf, Nutzer 10.11.2026): Startaufruf wie oben mit Auftrag „Befunde `<IDs>` aus
`<review-datei>` zu PR #`<nr>` beheben, Head vorher `<sha>`“. Am Ende nach `docs/prozess/zusammenarbeit.md` §5 Punkt 8: bei
behobenem P0/P1 die Nachprüfung selbst per `create_trigger` anlegen (Kurzaufruf aus `review-anfrage.md`, Delta alter..neuer Head,
nur diese Befunde, Modell Claude Opus 5.5; das Anlegen ist autorisiert), sonst Label `leitstelle-delta`. Widerspricht der Lauf einem
Befund mit eigenem Beleg: Label `beleg-strittig`, beide Belege im Übergabekommentar, für diesen Punkt keine Nachprüfung.

## Ablauf für den Agenten

1. Lies `AGENTS.md`, `docs/prozess/zusammenarbeit.md` (Abschnitte 2–4) und in `docs/arbeitspakete.md` **nur** die aktuelle
   Runde und den Abschnitt deiner Spur. Archiv und alte Reviews nur bei konkretem Bedarf.
2. Einrichten nach `AGENTS.md` Abschnitt 3 (Claude Code: zusätzlich `CLAUDE.md`). Originaldaten nach `gamedata/`.
3. Gibt es für die Spur schon einen offenen PR mit Bearbeiter? Dann nur nach Übergabekommentar weiterarbeiten,
   sonst den Nutzer fragen. Neu: Branch `paket/<spur>[-<teil>]` von aktuellem `main`, sofort Draft-PR mit
   `Bearbeiter: <Agent>` und Base-SHA. **Geplante Läufe mit Teilen:** Für die Prüfung auf vorhandene PRs und die Wahl von
   Branch und PR-Base gilt der Abschnitt „Teile und Fortsetzung“ unten; den Draft-PR mit `Bearbeiter:` und Base-SHA legst du
   trotzdem sofort an.
4. Arbeiten nach den harten Regeln: reproduzieren → roter Regressionstest → Fix; Dateigrenzen; `[APPROX]`/`[ORIGINAL]`;
   Mehrsaisonprobe, wenn Kader, Geld oder Saisonwechsel berührt sind.
5. Vor jedem Push: `git fetch origin && git rebase origin/main` (geplante Läufe mit Teilen: Rebase-Ziel nach „Teile und
   Fortsetzung“, Schritt 2), `tools/pruefen.sh --schnell` (siehe `AGENTS.md` §5).
6. Fertig: PR-Beschreibung nach `.github/pull_request_template.md`, Draft aufheben, Status im eigenen Abschnitt von
   `docs/arbeitspakete.md`. Mehrere kleine PRs sind besser als ein großer.
7. **Immer zum Schluss – nach erledigter Abarbeitung wie bei Unterbrechung:** Übergabekommentar im PR nach
   `docs/handoffs/VORLAGE.md` (Head-SHA, erledigt/offen/nächster Schritt, Testtabelle, Risiken, `Bearbeiter: frei`),
   damit ein neuer Chat ohne Verlauf übernehmen kann.
8. Antwort an den Nutzer **knapp** (`AGENTS.md` §7): PR-Links, Ergebnis in einem Satz je PR, offene Fragen.

## Teile und Fortsetzung (geplante Läufe)

Gilt, wenn die Spur in `docs/arbeitspakete.md` eine geordnete Liste von Teilen hat und der Startaufruf die Fortsetzung erlaubt
oder einen einzelnen Folgeteil mit Base nennt (dann ohne Schritt 5).

1. **Stand ermitteln:** offene PRs der Spur (`paket/<spur>-<n>`) und den letzten Übergabekommentar lesen. Dort steht
   „Fortsetzungen verbraucht: n von 3“. Nennt der Startaufruf einen anderen Zähler, gilt der höhere. Steht in einem offenen
   PR der Spur ein Bearbeiter ≠ `frei` ohne Übergabekommentar **nach** dessen letztem Bearbeiter- oder Übernahme-Eintrag
   (abgebrochener oder noch laufender Lauf): nichts am Branch ändern, nur Label `spur-wartet` und einen Hinweiskommentar setzen –
   **ohne** Übergabe und **ohne** `Bearbeiter: frei` – keine Fortsetzung anlegen und den Lauf beenden; der Hinweiskommentar
   ersetzt die Übergabe (Hauptablauf Schritt 7 entfällt).
2. **Stapelregel:** Teil 1 von aktuellem `main`, PR-Base `main`. Teil n > 1: Ist der PR des vorigen Teils noch offen, Branch
   von `paket/<spur>-<n-1>` und PR-Base = dieser Branch; ist er gemergt, von `main`. Rebase vor dem Push (Hauptablauf Schritt 5): nur der
   unterste offene Teil auf `origin/main`, jeder höhere auf seinen Base-Branch (`git rebase origin/paket/<spur>-<n-1>`);
   danach `git push --force-with-lease` nur auf eigene Branches.
3. **Teil abschließen:** grünes `tools/pruefen.sh --schnell`, PR „Ready for review“ setzen:
   `gh api -X POST repos/kaaasi/eishockey-manager-redone/pulls/<nr>/ccr/ready_for_review` (Antwort `{"draft":false}`; erprobt
   04.11.2026). Scheitert der Aufruf, Fehlermeldung im Übergabekommentar vermerken; dann setzt es die Leitstelle mit demselben Aufruf,
   Übergabekommentar mit „Fortsetzungen verbraucht: n von 3“. Dann den nächsten Teil im selben Lauf beginnen, solange Zeit bleibt.
4. **Teil blockiert** (kein grünes `--schnell`, Beleg fehlt, Referenz-Repo unerreichbar, fremde Datei nötig): Teil als Draft-PR mit Übergabe lassen,
   Label `spur-wartet` setzen, im Übergabekommentar „blockiert – Entscheidung Nutzer“ und den Grund. **Keine Fortsetzung**
   anlegen und den Teil nicht erneut versuchen.
5. **Fortsetzung anlegen** (nur wenn Teile offen sind, nichts blockiert ist und n < 3):
   - vorher mit `list_triggers` prüfen, ob für diese Spur schon eine geplante Aufgabe aussteht; dann keine weitere anlegen;
   - `create_trigger` mit `run_once_at` etwa 10 Minuten später, Name `Spur <name> – Fortsetzung <n+1>`, Prompt = Startaufruf
     oben mit `Fortsetzung <n+1> von 3, Übergabe in PR #<oberster PR>.`;
   - das Modell lässt sich beim Anlegen nicht wählen und darf vom Lauf nicht umgestellt werden: im Übergabekommentar
     „Modell der Fortsetzung prüfen (Soll: <modell>)“ vermerken – Leitstelle bzw. Nutzer stellen es um;
   - die Aufgabe (Name, Zeitpunkt) im Übergabekommentar nennen;
   - lehnt die Freigabe `create_trigger` ab (beobachtet 01.11.2026, häufig): Label `spur-wartet` setzen und im Übergabekommentar
     „Fortsetzung anlegen – Leitstelle“ mit dem fertigen Prompt (Name, Startaufruf mit Zähler) vermerken; die Leitstelle legt
     die Aufgabe an und setzt das Modell laut vom Nutzer bestätigtem Plan bzw. nach `docs/prozess/zusammenarbeit.md` §5
     „Modellwahl“.
6. **Kettenende:** Sind alle Teile fertig, oder ist n = 3 und Teile bleiben offen: keine Fortsetzung; Label `spur-wartet`
   auf den obersten PR und im Übergabekommentar „Spur fertig – Integration“ bzw. „Fortsetzungen erschöpft – Entscheidung
   Nutzer“. Die Leitstelle sieht offene PRs mit `spur-wartet` durch.
7. Nie eine fremde Spur fortsetzen oder starten.
