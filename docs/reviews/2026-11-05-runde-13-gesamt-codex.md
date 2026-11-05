# Gesamtreview Runde 13 – Zwischenstand Codex

## 1. Merge-Empfehlung

**Nein** für Base `8afd750bfc0cc57948041ca8906db6924caa7eb5` bis Head `b634032433e389623d413b14441ceb65fabdb6a5`. Dies ist ein **unvollständiges Review, nur Codelesen**, kein Testnachweis. Die nachstehend offenen Prüfbereiche verhindern nach der Vorlage eine Freigabe. Eine Fortsetzung in einer neuen Sitzung ist erforderlich; ein Originaldaten-Nachweis für genau diesen Head muss im PR stehen.

## 2. Befunde

Keine neuen, unabhängig reproduzierten P0–P3-Befunde in diesem Zwischenstand. Das bedeutet keine Aussage über die Fehlerfreiheit des Deltas.

Der GitHub-Connector liefert bei Suche nach offenen Issues die erwarteten #591, #592, #594, #595, #597, #598, #603–#605, #610 und #611 sowie #601. Das bestätigt ihren offenen Status zum Abfragezeitpunkt, nicht die erneute technische Prüfung der Befunde. #601 nennt die Auswertung der Aufnahme `runde-13` für Runde 14 und A088; A088 und U15-c als `[APPROX]` sind hier noch nicht unabhängig bestätigt.

## 3. Ausgeführte Prüfungen und offene Prüfbereiche

- Vorlage, AGENTS.md und Zusammenarbeit §§5–6 gelesen; unabhängige Sitzung ohne Implementierungskontext.
- Connector-Compare `8afd750...b634032`: 99 Commits voraus; Compare `b0688af...b634032`: fünf geänderte beziehungsweise hinzugefügte Dateien. Der Fix entfernt laut Head-Commit-Diff den Aufruf `RenumberPlayers()` vor der Abfrage der Automatik und ergänzt Migration 38 → 39. Dies ist eine begrenzte Codeeinsicht, keine Gegenprobe.
- Lokaler Checkout sauber auf `b0688af89c2cafe701f714259c02f20bf552f442`, Tree `9e23096f9fefdc961cce5ddefb339374cb30146e`, also **nicht** der angeforderte Head. `git cat-file -t b634032` scheitert; `git fetch origin` scheitert an der Verbindung zum konfigurierten Proxy. `gh auth status` meldet ungültige Authentifizierung. Der Connector liefert Compare und Dateien, aber ein vollständiger rekonstruierter und gegen den Ziel-Tree geprüfter Checkout wurde in dieser Sitzung nicht hergestellt.
- `/workspace/.dotnet/dotnet --version`: `10.0.402`. Original-`em.exe` und Spielstände liegen im lokalen Referenzverzeichnis; sie wurden für dieses Review noch nicht ausgewertet.

| Geforderte Prüfung | Ergebnis |
|---|---|
| `tools/pruefen.sh`: mit Originaldaten | Nicht ausgeführt; exakter Head nicht nachgewiesen. |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | Nicht ausgeführt; exakter Head nicht nachgewiesen. |
| Pflichtlauf ohne Originaldaten | Nicht ausgeführt; erwartetes Scheitern nicht nachgewiesen. |
| Gegenbeispiele S-K6-a, A009, Stadion rechte Taste, U15-a, R-K5, Freiprüfung per Mutation, R13G-01 | Offen: rot vor der jeweiligen Spur und grün am Head nicht erneut ausgeführt. |
| Jeder neue/geänderte Test: Gegenprobe und Tabelle je Test | Offen: Umfang des Deltas noch nicht vollständig inventarisiert und mutiert. |
| Jeder Sollwert, Fingerabdruck und jede Saat: Rückführung und Rückmutation | Offen: noch nicht vollständig inventarisiert oder rückmutiert. |
| D1–D3 und L1–L15 jeweils gegen Code | Offen: Entscheidungen und Kommentare noch nicht vollständig ausgewertet. |
| Jede neue `[ORIGINAL]`-Fundstelle gegen em.exe/Spielstand | Offen: Fundstellen noch nicht vollständig inventarisiert und nachgelesen. |
| Alle Bildschirme und Hauptmenü-Ereignisse gegen AGENTS.md §4 | Offen: vollständige Prüfung und Mutationen fehlen. |
| Schemakette 36 → 39 mit echtem Altstand und vier Saisonwechseln | Offen: echter Altstand und Ablauf noch nicht geprüft. |
| Erwartete `[APPROX]` A088 und U15-c | Offen: nicht unabhängig bestätigt; keine zusätzlichen `[APPROX]` akzeptiert. |

**Nächster Schritt:** Exakten Head mit vollständigem Tree-Nachweis bereitstellen; zuerst die genannten Gegenbeispiele rot/grün ausführen, dann sämtliche Zeilen der offenen Vollprüfung abarbeiten und je Test, Entscheidung, Wert und Fundstelle Ergebnisse dokumentieren. Danach alle drei Standardläufe auf dem nachgewiesenen Head durchführen. Dieser Zwischenstand ist keine Freigabe und ersetzt keine der genannten Prüfungen.
