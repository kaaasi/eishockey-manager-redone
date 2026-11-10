# Vorlage: Leitstelle (neuer Chat je Runde oder Vertretung)

**Kurzaufruf:**

> Repo kaaasi/eishockey-manager-redone. Du bist die Leitstelle nach `docs/vorlagen/leitstelle-start.md`.
> Auftrag: `<z. B. Integration Runde 3 Welle A | Review #<nr> auswerten und mergen | Runde 4 planen>`.
> `<Vertretung: ja, ernannt vom Nutzer am <datum> | –>`

## Ablauf für die Leitstelle

0. **Zugriff (Nutzer 01.11.2026):** immer Lese- und Schreibzugriff auf beide Repos: zuerst `add_repo` für
   `kaaasi/eishockey-manager-redone` und `kaaasi/eishockey-manager-referenz`, jeweils access `push` (ggf. erst per `ToolSearch`
   mit `select:mcp__claude-code-remote__add_repo` laden). Lehnt die Freigabe ab, dem Nutzer melden.
1. Lies `AGENTS.md`, `docs/prozess/zusammenarbeit.md` und `docs/prozess/anhang.md` (vollständig), in `docs/arbeitspakete.md` die
   aktuelle Runde, die offenen PRs (Titel, Bearbeiter, letzte Übergabe) und die Reviews der Runde. Kein Vorwissen aus Chats nötig.
2. **Integration:** Spur-PRs (einer je Spur) nach Erstreview und Merge-Prüfung in der Integrationsfolge des Plans mergen. Ist
   `main` seit dem Review neu und der PR **ohne Konflikt**, wird **nicht** rebased (der freigegebene Head bleibt): vorher lokal
   `tools/pruefen.sh --schnell` mit Originaldaten auf `main` + PR (Merge-Ergebnis), Tabelle im Merge-Kommentar (`zusammenarbeit.md`
   §6 Nr. 2). Muss sich der Head trotzdem ändern (z. B. erwartete Listen von `abgleich-original` gegen den neuen `main`), Fixlauf
   ohne Befund anlegen (`spur-start.md` „Fixlauf“); sein Delta prüft die Leitstelle nach §5 Punkt 4, wenn es nur Rebase und diese
   Anpassung enthält (`git range-diff`). `integration/<runde>` nur bei Konflikten zwischen Spuren: von `main`, PRs zusammenführen,
   Konflikte lösen, `tools/pruefen.sh` (alle drei Läufe), Mehrsaisonprobe, Bildschirmfoto-Abläufe, Integrationsfehler mit rotem
   Regressionstest beheben; Integrations-PR nach Vorlage, Abschnitt „Integration …“ in `docs/arbeitspakete.md`.
3. **Review anfordern:** dem Nutzer nur den ausgefüllten Kurzaufruf aus `review-anfrage.md` geben (Gesamtreview, Codex:
   `anhang.md` C, D).
4. **Mergen:** nur nach der Merge-Prüfung (`zusammenarbeit.md` §6), mit SHA-Bindung. Eigene Integrations-Commits brauchen das
   Review wie jede andere Änderung.
5. **Planen:** neue Spuren in `docs/arbeitspakete.md` mit Ziel, Befunden, Branch, Base-SHA, Dateigrenzen, Abnahme; Startaufrufe nur
   als Kurzaufruf aus `spur-start.md`. Abgeschlossene Runden nach `docs/archiv/`.
6. **Immer zum Schluss:** Übergabekommentar nach `docs/handoffs/VORLAGE.md` in jedem PR, an dem die Sitzung gearbeitet hat
   (Integration, Plan); ohne PR als `docs/handoffs/<datum>-<thema>.md` auf eigenem Branch.
7. **Geplante Läufe** (Spuren, Fortsetzungen, Fixläufe, Reviews): Zugriff, Modell, `spur-wartet`, Draft-Status nach `anhang.md` F.
8. Antwort an den Nutzer **knapp** (`AGENTS.md` §7): Ergebnis, Entscheidungsbedarf, nächster Schritt, Kurzaufrufe.
9. **Sitzung je Runde (Nutzer 10.11.2026):** höchstens eine Runde je Sitzung; die nächste beginnt in einem neuen Chat mit dem
   Kurzaufruf oben. Stand nur in GitHub (Status in `docs/arbeitspakete.md`, Labels, Übergaben). Dem Nutzer meldet sie sich nur bei
   Entscheidungsbedarf, am Ende einer Spur (Merge) und am Rundenende – nicht nach jedem Teil.
10. **Warten über Labels** (`zusammenarbeit.md` §5 Punkt 8, Tabelle): Warteschleife je Label eine Abfrage, bei ChatGPT/Codex
    zusätzlich `review/*` (`anhang.md` F). Erledigte Labels entfernt die Leitstelle.
