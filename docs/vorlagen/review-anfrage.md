# Vorlage: Unabhängiges Review

**Kurzaufruf** (die Leitstelle liefert nur diesen Block, der Nutzer kopiert ihn in einen **neuen** Chat des Reviewers):

> Repo kaaasi/eishockey-manager-redone. Unabhängiges Review nach `docs/vorlagen/review-anfrage.md`.
> PR: #`<nr>` · Base: `<sha>` · Head: `<sha>` · Ablage: `docs/reviews/<JJJJ-MM-TT>-<thema>.md`
> Gegenbeispiele erneut ausführen: `<IDs mit einer Zeile Ablauf>`
> Erwartet offen (nur bestätigen): `<IDs | –>`
> Besonders prüfen: `<1–3 Punkte | –>`

Bei einer **Nachprüfung** zusätzlich: „Nachprüfung des Reviews `<datei>`, bisher geprüfter Head `<sha>`, nur Delta
`<alt>..<neu>` und die genannten Befunde.“ Ablage dann `…-nachpruefung.md`.

## Ablauf für den Reviewer

1. Lies `AGENTS.md` und `docs/prozess/zusammenarbeit.md` (Abschnitte 5 und 6). Hast du an diesem PR selbst
   mitgearbeitet, brich ab und sag es – das Review muss aus einer anderen Sitzung kommen.
2. Prüfe genau den genannten Head (Blob-Hashes oder `git rev-parse`), baue ihn selbst, führe `tools/pruefen.sh`
   (alle drei Läufe) aus. Aussagen aus der PR-Beschreibung nicht ungeprüft übernehmen.
3. Führe zuerst die genannten Gegenbeispiele erneut aus, dann: Diff lesen, betroffene Aufrufer, neue Tests. Eine
   Gegenprobe pro neuem Test, wo es geht (gezielte Mutation in einer temporären Kopie → Test muss rot werden).
4. Bericht mit genau drei Teilen:
   1. **Merge-Empfehlung** ja/nein für genau diesen Head, mit Begründung
   2. **Befunde** mit Priorität P0–P3, Datei/Zeile (Link auf den Head), reproduzierbarem Gegenbeispiel
   3. **Ausgeführte Prüfungen** (Befehle, Tabelle aus `tools/pruefen.sh`) und **akzeptierte `[APPROX]`**, getrennt von Annahmen
   Keine Freigabe anhand der Testanzahl. Knapp: keine Wiederholung der PR-Beschreibung.
5. Ablage: nichts am Code oder am geprüften PR ändern. Datei auf neuem Branch `review/<thema>` (Basis `main`), PR mit
   **nur dieser Datei**, Commit-Trailer mit deinem Agentennamen. Ohne Schreibzugriff: Datei an den Nutzer.
6. Antwort an den Nutzer: Link zum Review-PR, Empfehlung in einem Satz.
