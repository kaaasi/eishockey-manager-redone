# Vorlage: Leitstelle (neuer Chat je Runde oder Vertretung)

**Kurzaufruf:**

> Repo kaaasi/eishockey-manager-redone. Du bist die Leitstelle nach `docs/vorlagen/leitstelle-start.md`.
> Auftrag: `<z. B. Integration Runde 3 Welle A | Review #<nr> auswerten und mergen | Runde 4 planen>`.
> `<Vertretung: ja, ernannt vom Nutzer am <datum> | –>`

## Ablauf für die Leitstelle

1. Lies `AGENTS.md`, `docs/prozess/zusammenarbeit.md` (vollständig), in `docs/arbeitspakete.md` die aktuelle Runde,
   die offenen PRs (Titel, Bearbeiter, letzter Übergabekommentar) und die Reviews der aktuellen Runde in `docs/reviews/`.
   Kein Vorwissen aus früheren Chats nötig oder vorausgesetzt.
2. **Integration:** Branch `integration/<runde>` von `main`, PRs zusammenführen, Konflikte lösen, `tools/pruefen.sh`
   (alle drei Läufe), Mehrsaisonprobe, Bildschirmfoto-Abläufe. Integrationsfehler mit rotem Regressionstest beheben.
   Integrations-PR nach Vorlage; Abschnitt „Integration …“ in `docs/arbeitspakete.md`.
3. **Review anfordern:** dem Nutzer nur den Kurzaufruf aus `docs/vorlagen/review-anfrage.md` ausgefüllt geben.
4. **Mergen:** nur nach der Merge-Prüfung (`zusammenarbeit.md` §6), mit SHA-Bindung. Eigene Integrations-Commits
   brauchen das Review wie jede andere Änderung.
5. **Planen:** neue Spuren in `docs/arbeitspakete.md` mit Ziel, Befunden, Branch, Base-SHA, Dateigrenzen, Abnahme;
   Startaufrufe nur als Kurzaufruf aus `docs/vorlagen/spur-start.md`. Abgeschlossene Runden nach `docs/archiv/` verschieben.
6. Antwort an den Nutzer **knapp** (`AGENTS.md` §7): Ergebnis, Entscheidungsbedarf, nächster Schritt, Kurzaufrufe.
