# Vorlage: Unabhängiges Review

**Kurzaufruf** (die Leitstelle liefert nur diesen Block, der Nutzer kopiert ihn in einen **neuen** Chat des Reviewers):

> Repo kaaasi/eishockey-manager-redone. Unabhängiges Review nach `docs/vorlagen/review-anfrage.md`.
> PR: #`<nr>` · Base: `<sha>` · Head: `<sha>` · Ablage: `docs/reviews/<JJJJ-MM-TT>-<thema>.md`
> Gegenbeispiele erneut ausführen: `<IDs mit einer Zeile Ablauf>`
> Erwartet offen (nur bestätigen): `<IDs | –>`
> Besonders prüfen: `<1–3 Punkte | –>`
> Erstelle im Repo einen neuen Remote-Branch `review/<thema>` auf Basis von `main`, committe dort ausschließlich
> die Review-Datei mit Agenten-Trailer und öffne einen Review-PR gegen `main`. Diese GitHub-Schreibaktionen sind
> ausdrücklich autorisiert. Nicht mergen. Falls Schreiben nicht möglich ist, stelle die fertige Datei bereit.

Als **geplanter Lauf** zusätzlich „Unbeaufsichtigt, keine Rückfragen.“ und – wenn Originaldaten, Spielstände oder Bilder nötig
sind – der Zugriffsabsatz aus `spur-start.md` (Kurzaufruf „Geplanter Lauf“).

Bei einer **Nachprüfung** zusätzlich: „Nachprüfung des Reviews `<datei>`, bisher geprüfter Head `<sha>`, nur Delta
`<alt>..<neu>` und die genannten Befunde.“ Ablage dann `…-nachpruefung.md`.

## Ablauf für den Reviewer

1. Lies `AGENTS.md` und `docs/prozess/zusammenarbeit.md` (Abschnitte 5 und 6). Hast du an diesem PR selbst
   mitgearbeitet, brich ab und sag es – das Review muss aus einer anderen Sitzung kommen.
2. Richte das SDK gemäß `AGENTS.md` §3 ein und prüfe im Repo-Verzeichnis `dotnet --version` (bei Problemen
   `docs/prozess/umgebung.md`).
   Fehlen Commit oder Bericht im lokalen Checkout und scheitert der Fetch, nutze einen verfügbaren GitHub-Connector
   für Compare, Dateien mit festem Head-Ref, früheren Bericht und PR-Kommentare. Rekonstruiere für Build und Tests
   den Checkout über den Connector, soweit möglich, und verifiziere den vollständigen Git-Tree einschließlich
   Dateimodi und gelöschter Pfade gegen den Zielcommit (siehe `docs/prozess/umgebung.md`). Ist die Verifikation
   nicht erbracht (nicht möglich, unterlassen, Tree-SHA weicht ab oder nur teilweise rekonstruiert), dokumentiere die
   Grenze und die blockierten Prüfungen; Läufe an einem solchen Stand sind höchstens Diagnose, im Bericht als
   „unverifizierter Stand“ gekennzeichnet, und kein Test des exakten Heads. Dokumentiere, ob nur Codelesen oder auch
   eine Ausführung am nachgewiesenen Checkout erfolgte.
   Prüfe genau den genannten Head: bei einem Klon `git rev-parse HEAD` gleich dem Head-SHA und `git status --porcelain`
   leer (sonst unverifizierter Stand), bei einer Rekonstruktion
   Tree-SHA des Checkouts (z. B. `git add -A && git write-tree`) gleich dem Tree-SHA des Zielcommits; Blob-Hashes
   einzelner Dateien genügen nicht. Baue ihn selbst, führe `tools/pruefen.sh` (alle drei Läufe) aus. Aussagen aus der
   PR-Beschreibung nicht ungeprüft übernehmen.
3. Führe zuerst die genannten Gegenbeispiele erneut aus, dann: Diff lesen, betroffene Aufrufer, neue Tests. Eine
   Gegenprobe pro neuem Test, wo es geht (gezielte Mutation in einer temporären Kopie → Test muss rot werden).
4. Bericht mit genau drei Teilen:
   1. **Merge-Empfehlung** ja/nein für genau diesen Head, mit Begründung
   2. **Befunde** mit Priorität P0–P3, Datei/Zeile (Link auf den Head), reproduzierbarem Gegenbeispiel
   3. **Ausgeführte Prüfungen** (Befehle, Tabelle aus `tools/pruefen.sh`) und **akzeptierte `[APPROX]`**, getrennt von Annahmen
   Keine Freigabe anhand der Testanzahl. Knapp: keine Wiederholung der PR-Beschreibung.
   Ohne nachgewiesenen Checkout (nur Codelesen oder unverifizierter Stand) liefert das Review keinen Testnachweis nach
   `zusammenarbeit.md` §6 Nr. 4; die Merge-Empfehlung sagt dann ausdrücklich „nur Codelesen“. Der
   Originaldaten-Nachweis muss dann im PR stehen (auch als Kommentar der Leitstelle).
5. Ablage: nichts am Code oder am geprüften PR ändern, nichts ins Referenz-Repo schreiben. Datei auf neuem Branch `review/<thema>` (Basis `main`), PR mit
   **nur dieser Datei**, Commit-Trailer mit deinem Agentennamen. Ohne Schreibzugriff: Datei an den Nutzer.
6. Antwort an den Nutzer: Link zum Review-PR, Empfehlung in einem Satz.
