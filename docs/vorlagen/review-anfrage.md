# Vorlage: Unabhängiges Review

**Kurzaufruf** (die Leitstelle liefert nur diesen Block, der Nutzer kopiert ihn in einen **neuen** Chat des Reviewers):

> Repo kaaasi/eishockey-manager-redone. Unabhängiges Review nach `docs/vorlagen/review-anfrage.md`.
> PR: #`<nr>` · Base: `<sha>` · Head: `<sha>` · Ablage: `docs/reviews/<JJJJ-MM-TT>-<thema>.md`
> Gegenbeispiele erneut ausführen: `<IDs mit einer Zeile Ablauf>`
> Erwartet offen (nur bestätigen): `<IDs | –>`
> Besonders prüfen: `<1–3 Punkte | –>`
> Erstelle im Repo einen neuen Remote-Branch `review/<thema>` auf Basis von `main`, committe dort ausschließlich
> die Review-Datei mit Agenten-Trailer und öffne einen Review-PR gegen `main`; setze am PR #`<nr>` das Label `review-ja` bzw.
> `review-nein` (mit Netzzugang). Diese GitHub-Schreibaktionen sind
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
   einzelner Dateien genügen nicht. Baue ihn selbst, führe `tools/pruefen.sh` aus (Erstreview: alle drei Läufe; Nachprüfung: `--schnell`,
   `docs/prozess/zusammenarbeit.md` §5 „Sparsame Kette“). Aussagen aus der
   PR-Beschreibung nicht ungeprüft übernehmen.
3. Führe zuerst die genannten Gegenbeispiele erneut aus, dann: Diff lesen, betroffene Aufrufer, neue Tests. Eine
   Gegenprobe pro neuem Test, wo es geht (gezielte Mutation in einer temporären Kopie → Test muss rot werden).
4. Bericht mit genau drei Teilen:
   1. **Merge-Empfehlung** ja/nein für genau diesen Head, mit Begründung
   2. **Befunde** mit Priorität P0–P3, Datei/Zeile (Link auf den Head), reproduzierbarem Gegenbeispiel
   3. **Ausgeführte Prüfungen** (Befehle, Tabelle aus `tools/pruefen.sh`) und **akzeptierte `[APPROX]`**, getrennt von Annahmen
   Keine Freigabe anhand der Testanzahl. Knapp: keine Wiederholung der PR-Beschreibung, **höchstens 120 Zeilen**.
   **Jeder Befund mit ausführbarem Beleg** (roter Test oder Probe mit Befehl und Ergebnis; „fehlt im Remake“ zusätzlich mit der
   erfolglosen Codesuche). Ohne Beleg höchstens P3 „Verdacht“ (`zusammenarbeit.md` §5 Punkt 7).
   Ohne nachgewiesenen Checkout (nur Codelesen oder unverifizierter Stand) liefert das Review keinen Testnachweis nach
   `zusammenarbeit.md` §6 Nr. 4; die Merge-Empfehlung sagt dann ausdrücklich „nur Codelesen“. Der
   Originaldaten-Nachweis muss dann im PR stehen (auch als Kommentar der Leitstelle).
5. Ablage: nichts am Code oder am geprüften PR ändern (außer dem Label in Schritt 6), nichts ins Referenz-Repo schreiben. Datei auf neuem Branch `review/<thema>` (Basis `main`), PR mit
   **nur dieser Datei**, Commit-Trailer mit deinem Agentennamen. Ohne Schreibzugriff: Datei an den Nutzer.
6. Am geprüften PR das Label `review-ja` bzw. `review-nein` setzen (das jeweils andere entfernen), ohne den PR sonst zu ändern.
   Ohne Schreibzugriff setzt es die Leitstelle.
7. Antwort an den Nutzer: Link zum Review-PR, Empfehlung in einem Satz.

## Gesamtreview am Rundenende (risikoorientiert, Vorgabe Nutzer 10.11.2026)

Ziel: Entspricht das Remake dem Original hinreichend, und enthalten die Änderungen der Runde konkrete Fehler? Nachgeprüft wird nur
das Delta von Fixes, die aus dem Gesamtreview folgen (nur bei P0/P1, sonst prüft die Leitstelle nach
`docs/prozess/zusammenarbeit.md` §5 „Sparsame Kette“). Die Leitstelle setzt im Kurzaufruf hinter „Besonders prüfen:“ diesen Absatz
ein und ergänzt die riskantesten Änderungen der Runde:

> Risikoorientiert, keine Vollinventur: Prüfe bevorzugt riskante Änderungen und Übergänge – Laden und Speichern, Tages- und
> Saisonwechsel, Finanzen, Wettbewerbe, Kader, Bedienung – und die Entscheidungen der Runde. Nutze Originalstände, Bildschirmfotos und
> `em.exe` gezielt dort, wo sie eine relevante Frage klären. Führe die genannten Gegenbeispiele erneut aus. Tests und Mutationen dienen
> der Untersuchung konkreter Risiken oder vermuteter Fehler; keine Mutation jedes Tests und keine Einzelrückführung jedes Sollwerts,
> Fingerabdrucks oder jeder Saat. Melde Testlücken als solche (Kennzeichen „Testlücke“), getrennt von nachgewiesenen Produktfehlern.
> Bekannte `[APPROX]`-Punkte und die erwartet offenen Issues nur bestätigen, nicht als neue Befunde melden. Die Merge-Empfehlung
> richtet sich nach den konkreten Befunden, ihrer Auswirkung und den verbleibenden relevanten Unsicherheiten; nicht Geprüftes nennst
> du kurz, es bedeutet nicht von selbst „Nein“. Höchstens 120 Zeilen; reicht eine Sitzung nicht, Bericht mit Stand ablegen, Fortsetzung
> als neue Datei `<datei>-teil<n>.md` auf neuem Branch (siehe „Aufrufe für ChatGPT/Codex“).

Die umfassende Prüfung aller Tests, Sollwerte, Originalfundstellen, Bildschirmzustände und Entscheidungen erfolgt einmal als
gesonderte **Abschlussprüfung**, wenn alles umgesetzt ist (vor v1.0), nicht in den Runden.

## Aufrufe für ChatGPT/Codex (Erfahrung Leitstelle Runde 13, 05.11.2026)

Codex klont das Repo beim Start der Aufgabe mit dem dort gewählten Branch. Der Agent arbeitet danach **ohne Netz**
(`git fetch`/`clone` scheitern mit „Failed to connect to proxy port 8080“). Den PR legt Codex selbst an. Daraus folgt
für jeden Kurzaufruf an ChatGPT:

1. **Head nur auf dem gewählten Branch:** Der Head-SHA muss der Stand des Branches sein, den der Nutzer in Codex auswählt
   (meist `main`). Liegt er auf einem anderen Branch (z. B. Fix-PR), nennt die Leitstelle dem Nutzer diesen Branch. Base-SHA
   und ältere Stände müssen in der Historie dieses Branches liegen.
2. **Ablage immer neu:** neuer Branch `review/<thema>`, neue Datei, neuer PR. Nie einen bestehenden Branch fortschreiben,
   auch nicht bei einer Fortsetzung (dann `…-teil<n>.md`).
3. **Keine Netzbefehle im Aufruf:** keine Vorprüfung mit `git fetch`, `git ls-remote` oder `add_repo`. Originaldaten und
   Spielstände kommen über den GitHub-Connector oder das Setup der Codex-Umgebung. Fehlen sie, gehört das als offener
   Prüfbereich in den Bericht.
4. Der Zugriffsabsatz aus `spur-start.md` gilt nur für Claude-Läufe.
