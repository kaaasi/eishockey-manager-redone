# Vorlage: Leitstelle (neuer Chat je Runde oder Vertretung)

**Kurzaufruf:**

> Repo kaaasi/eishockey-manager-redone. Du bist die Leitstelle nach `docs/vorlagen/leitstelle-start.md`.
> Auftrag: `<z. B. Integration Runde 3 Welle A | Review #<nr> auswerten und mergen | Runde 4 planen>`.
> `<Vertretung: ja, ernannt vom Nutzer am <datum> | –>`

## Ablauf für die Leitstelle

0. **Zugriff (Vorgabe Nutzer 01.11.2026):** Die Leitstelle hat immer Lese- und Schreibzugriff auf beide Repos. Zuerst
   `add_repo` für `kaaasi/eishockey-manager-redone` und `kaaasi/eishockey-manager-referenz`, jeweils access `push` (ggf. erst
   per `ToolSearch` mit `select:mcp__claude-code-remote__add_repo` laden). Lehnt die Freigabe das ab, dem Nutzer melden.
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
6. **Immer zum Schluss:** Übergabekommentar nach `docs/handoffs/VORLAGE.md` in jedem PR, an dem die Sitzung gearbeitet hat
   (Integration, Plan); ohne PR als `docs/handoffs/<datum>-<thema>.md` auf eigenem Branch.
7. **Geplante Läufe** (Spuren, Fortsetzungen, Reviews): Geplante Läufe erhalten nur die Repos, die die **anlegende Sitzung**
   eingebunden hat (beobachtet 01.11.2026: zwei Spurläufe ohne Referenz-Repo blockiert, #291, #295); `add_repo` steht im Lauf nicht verlässlich bereit. Deshalb vor dem ersten `create_trigger` der Sitzung:
   Schritt 0 erledigt (beide Repos `push`) und
   `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD` erfolgreich – sonst keinen Lauf anlegen. Jeder Startaufruf enthält den
   Zugriffsabsatz aus `spur-start.md` (Kurzaufruf „Geplanter Lauf“). Modell nach dem Anlegen mit `update_trigger` setzen,
   wenn es von Opus abweicht – nur auf ausdrücklichen Wunsch des Nutzers (z. B. vom Nutzer bestätigter Plan bzw. Go mit
   Modell). Offene PRs mit `spur-wartet` durchsehen: „Fortsetzung anlegen – Leitstelle“ heißt, die Aufgabe mit dem Prompt aus
   der Übergabe anlegen – vorher gegen den Kurzaufruf „Geplanter Lauf mit Teilen“ prüfen; abweichen dürfen nur Spur, Modell,
   Zähler und PR-Nummer (R9F-03). Draft-Status, den ein Lauf nicht aufheben konnte, setzt der Nutzer; die Leitstelle nur,
   wenn ihr ein Werkzeug dafür zur Verfügung steht.
8. Antwort an den Nutzer **knapp** (`AGENTS.md` §7): Ergebnis, Entscheidungsbedarf, nächster Schritt, Kurzaufrufe.
