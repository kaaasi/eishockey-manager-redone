# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 4 – Lücken bis v0.9 ohne Live-Spiel (ab 29.10.2026)

Runde 3 (Kader, Wettbewerbe, Finanzen, Speichern, Spielmodus „nur Weltkugel“):
[archiv/runde-3.md](archiv/runde-3.md); Runden 1–2: [archiv/runden-1-2.md](archiv/runden-1-2.md).
**Base für alle Spuren:** aktueller `main` (≥ diesem Plan); den tatsächlichen Base-SHA trägt die Spur in ihren Draft-PR ein.
Weiterhin verbindlich: Mehrsaisonprobe für jede Spur, die Saisonwechsel, Kader oder Geld berührt; Spielstand-Schema
mit Version und Migration für jeden neuen Zustand. Die Spuren sind unabhängig und laufen parallel; danach eine
Integration wie in Runde 3, Welle C.

```
Runde 4 (parallel):  nationalteam-2   tabellen-highscore   speichern-reihenfolge
```

### Spur `nationalteam-2` – Aufstellung und Taktik des Nationalteams, Vergleich mit dem WM-Video

- **Ziel:** Die Platzhalter „Aufstellung“ und „Taktik“ im Menü des Bundestrainers durch die Bildschirme des Originals
  ersetzen; die Bundestrainer-Bildschirme aus Runde 3 erstmals gegen Videobilder vermessen.
- **Belege:** Reihe `bundestrainer` (`docs/referenz-videos.md`): Aufstellung Nationalteam 00056, Taktik 00058–00059,
  Hauptmenü des Bundestrainers 00026–00041 und Kachelreihen 00053/00100/00108/00042, „Spieler in WM-Kader“ 00044–00052,
  „WM Vorbereitung“ 00104–00106 u. a., „Stärken Länderteams“ 00109–00111; em.exe (Aufstellung/Taktik des Vereins als
  Ausgangspunkt, Unterschiede beim Nationalteam suchen). Verdeckung unten links beachten.
- **Abnahme:** (1) Aufstellung und Taktik des Nationalteams über die Kacheln erreichbar, Änderungen wirken auf die
  WM-Spiele (Test: andere Aufstellung → andere Stärke im nächsten WM-Spiel), Öffnen ohne Änderung ist zustandsrein;
  (2) Spielstand: neuer Zustand mit Schemaversion und Migration, Roundtrip; (3) Pixelvergleich je Bildschirm mit den
  genannten Videobildern (Kennzahl wie bei den Modusknöpfen in `wettbewerbe.md`, verdeckte Fläche ausgenommen),
  Abweichungen begründet oder behoben; (4) Mehrsaisonprobe Modus 1 (4 Wechsel) mit geänderter Aufstellung.
- **Dateigrenzen:** `UI/Screens/Competitions/**` (inkl. `NationalMenu.cs` – nur die Weiterleitung `Open`), `Core/CupsNational*.cs`,
  `Core/SaveGame.Migration*.cs` (neue Datei) und Eintrag in `SaveGame.cs`, `Tools/Scenarios/Wettbewerbe.cs`,
  `docs/wettbewerbe.md`. Vereinsbildschirme Aufstellung/Taktik nur lesen bzw. Bausteine in eine eigene Datei auslagern
  (im PR begründen).
- **Modell:** Claude Opus 5.5.
- **Status:** PR #103 bereit zum Review (Base `cbc650c`). Abnahme 1–4 umgesetzt; Regeln, Kennzahlen und offene Befunde
  (Taktik-Vorbelegung DGROUP 0x4911 ff., 22er-Grenze und Farbe beim Verein) in `docs/wettbewerbe.md`, Abschnitt
  „Aufstellung und Taktik des Nationalteams“.

### Spur `tabellen-highscore` – „Tabelle DEB-Pokal“, „Tabelle Europapokal“, Highscore, Kachelsperren

- **Ziel:** die drei verbliebenen Platzhalter ohne Abhängigkeit vom Live-Spiel schließen und die dynamischen
  Kachelsperren des Originals (Werbung, Neuer, Highscore) umsetzen.
- **Belege:** em.exe (Tabellen-Kacheln Symbolstart DGROUP 0x5149/0x5151, Datei 0x45B95; Sperren ab Datei 0x44FBD,
  siehe `NationalMenu.cs`); Highscore im Video „Die Oberfläche von EM“ 8:40 (`oberflaeche_0840`, `kb\195`);
  Spielende-Bildschirm verweist auf die Bestenliste (`SeasonEndGameOverScreen.cs`). Für die beiden Pokaltabellen gibt es
  kein Videobild (`wettbewerbe.md`, „Weiterhin fehlen“) → Aufbau aus em.exe, Pixelvergleich entfällt, `[APPROX]` wo nötig.
- **Abnahme:** (1) beide Pokaltabellen zeigen den aktuellen Stand aus `Cups*` (Test gegen einen nachgestellten
  Pokalverlauf), zustandsrein; (2) Highscore: Eintrag zum belegten Zeitpunkt, Sortierung und Länge wie im Original,
  dauerhaft gespeichert wie im Original (Datei oder Spielstand – belegen) und Pixelvergleich mit `oberflaeche_0840`;
  (3) dynamische Sperren je Kachel mit Test gegen die em.exe-Bedingung; (4) Bildschirmfoto-Ablauf.
- **Dateigrenzen:** neue Bildschirme in `UI/Screens/Competitions/` bzw. `UI/Screens/Statistics/` (eigene Dateien,
  Anmeldung per `[MenuEntry]`), `Core/Statistics*.cs` bzw. neue `Core/Highscore*.cs`, `Tools/Scenarios/` (eigene Datei);
  minimal und begründet: `NationalMenu.IsLocked` (Sperren) bzw. eine eigene Sperrdatei, `SeasonEndGameOverScreen.cs`.
  **Konflikt mit `nationalteam-2`** nur in `NationalMenu.cs` möglich (getrennte Methoden) – der Zweite rebased.
- **Modell:** Claude Sonnet 5.5.
- **Status (Claude Code, 29.10.2026):** PR #104 `paket/tabellen-highscore`: Pokaltabellen + Sperren „Werbung“/„Neuer“
  (em.exe: beide Tabellen-Kacheln öffnen dieselben Bildschirme wie die Paarungen). Highscore folgt in eigenem PR
  (`paket/tabellen-highscore-bestenliste`).
- **Status Teil 2 (Claude Code, 29.10.2026):** PR #111 `paket/tabellen-highscore-bestenliste`, Base `caf9700`, fertig zur
  Prüfung: Highscore ([highscore.md](highscore.md)), Übersicht „DEB-Pokalspiele“ (#108), Hinweis „(Neuer)“ (#109).
  Offen: [APPROX] Spielerwerte (0x49D36), Europa-Stufe/Pokalbytes 2–3, Penalty-vs-Verlängerung in der DEB-Übersicht (siehe PR).

### Spur `speichern-reihenfolge` – Aufzählungsreihenfolge nach Remove/Add (#80)

- **Ziel:** Befund S71-02 (#80, P3, in Runde 3 zurückgestellt): Nach Entfernen und Wiedereinfügen hängt die
  Reihenfolge von Wörterbüchern/Mengen vom Verlauf ab; nach dem Laden ist sie anders.
- **Abnahme:** (1) Gegenbeispiel aus dem Review (`docs/reviews/2026-10-28-speichern-68-70-71.md`, S71-02) als roter
  Test; (2) Lösung nach Wahl, im PR begründet: reihenfolgerelevante Aufrufer stabil sortieren **oder** Sammlungen beim
  Laden/Speichern in definierter Ordnung führen; (3) alle Zwillings- und Roundtrip-Proben grün, Schemaabdruck nur bei
  echter Formatänderung (dann mit Migration).
- **Dateigrenzen:** `Core/SaveGame*.cs`, betroffene Aufrufer minimal, `docs/formate/spielstand.md`.
- **Modell:** Claude Sonnet 5.5 (klein).
- **Status (29.10.2026):** erledigt, PR offen (Lösung: stabil sortieren in `GameState.PlayNextMatchDay`; Format und Schema unverändert).

### Integration Runde 4 (29.10.2026)

Branch `integration/runde-4` von `main` `cbc650c`, Leitstelle Claude Code. Zusammengeführt: #103 (`nationalteam-2`,
Head `38f0e70`), #104 (`tabellen-highscore` **Teil 1**: Pokaltabellen, Sperren Werbung/Neuer, Head `65446d5`; „Tabelle DEB-Pokal“ nach R4-01 zurückgestellt),
#105 (`speichern-reihenfolge`, Head `f4ef3ab`).

- Konflikt: `docs/wettbewerbe.md` (#103 × #104, Tabelle der Belege) – beide Zeilen übernommen. `NationalMenu.cs`
  ohne Konflikt (#103 `Open`, #104 `IsLocked`).
- Zusammenspiel neu getestet (`IntegrationRunde4Tests`, Originaldaten): eigene Aufstellung und Taktik des
  Nationalteams überstehen Sichern/Laden über die Kacheln im Menü des Bundestrainers (Schema 4), Aufstellung/Taktik
  öffnen danach zustandsrein; die neuen Sperren der Spielvorbereitung ändern das Menü des Bundestrainers nicht. Grün,
  kein Integrationsfehler.
- #105 lief in seiner Spur ohne Originaldaten; auf dem Integrationsstand mit Originaldaten grün (alle
  Mehrsaison-Referenzwerte unverändert).
- Doku `spielstand.md`: Migration 3 → 4 (aus #103) in der Tabelle ergänzt; Hinweis aus #105 auf Schema 4 angepasst.
- Bildschirmfotos `wettbewerbe wm nationalteam weltkugel nationalteam-2 tabellen-pokale speichern`: ohne Fehler;
  alle außer `speichern` (zufälliges Verzeichnis, bekannt) bytegleich mit dem Stand der jeweiligen Spur.
- Review [integration-runde-4](reviews/2026-10-29-integration-runde-4.md): **R4-01 (P2, #108)** – „Tabelle DEB-Pokal“
  führt im Original zur Übersicht „DEB-Pokalspiele“ (Datei 0x32E23), nicht zur Auslosung. Entscheidung des Nutzers
  (29.10.2026): Kachel bis dahin wieder **Platzhalter** (Integrations-Branch), die Übersicht baut `tabellen-highscore`
  Teil 2 mit. **R4-02 (P3, #109)** – Hinweis „(Neuer)“ bei vier Managern auf gesperrte Kachel → ebenfalls Teil 2.
- **Offen:** `tabellen-highscore` Teil 2 (Highscore: Bestenliste, Sperre, Speicherung, Pixelvergleich) – noch nicht
  begonnen; eigener PR nach dieser Integration. Befunde aus #103 für andere Spuren (Taktik-Vorbelegung DGROUP
  0x4911–0x4925, Vereinsaufstellung ohne 22er-Grenze, Farbe 17) stehen in `docs/wettbewerbe.md`.

### Integration Runde 4, Teil 2 (29.10.2026)

Branch `integration/runde-4-teil-2` von `main` `caf9700`, Leitstelle Claude Code. Zusammengeführt: #111
(`tabellen-highscore` **Teil 2**: Highscore, Übersicht „DEB-Pokalspiele“ für #108, Hinweis „(Neuer)“ für #109, Head `25ed38f`).

- #111 ist direkt von `caf9700` abgezweigt: kein Konflikt, Baum des Merge-Commits gleich dem Spur-Head.
- `tools/pruefen.sh` (alle drei Läufe) auf dem Integrationsstand grün; Bildschirmfotos `tabellen-pokale highscore
  wettbewerbe nationalteam-2 weltkugel statistik` ohne Fehler.
- Dateigrenzen-Ausnahmen der Spur (im Review prüfen): `DebCupDrawScreen.cs` (MenuEntry „DEB-Pokal“ → Übersicht),
  `FinanceSponsorRating.cs` (optionaler Zufallsstrom, damit die Highscore-Anzeige `FinanceRandom` nicht verbraucht).
- Offen aus der Spur (`[APPROX]`, `docs/highscore.md`): Spielerwerte 0x49D36, Europa-Stufe/Pokalbytes 2–3, Penalty vs.
  Verlängerung in der DEB-Übersicht; Pixelvergleich Highscore nur gegen `oberflaeche_0848` (Abnahme nannte 0840 bzw. 0844–0855).

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- Statistiken Spieler, Verein, Fairplay, WM-Statistik: warten auf `livespiel` (Tore, Vorlagen, Strafen je Spieler).
- Paarungen Liga (Paket `wettbewerbe` PR 8): kein Bildschirm im Referenzvideo nachgewiesen.
- Import `*.HOK`: kein Original-Spielstand zum Nachprüfen des Formats vorhanden (`docs/formate/dateiformate.md`, „Offen“).
- `finanzen`: Monatsausgaben ESC Wolfsburg (87.370 statt 73.977 DM), unabhängige Nachdisassembly der übrigen
  `[ORIGINAL]`-Fundstellen.
- Bundestrainer im Modus 4 (Liga + Weltkugel): Verhalten des Originals noch nicht belegt.

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: PR 1–7 gemergt (Runde 3); Restarbeiten in Runde 4 (`nationalteam-2`, `tabellen-highscore`), PR 8 zurückgestellt. Regeln und Belege: [wettbewerbe.md](wettbewerbe.md)

| PR | Inhalt | Stand |
|---|---|---|
| 1 | DEB-Pokal: Auslosung, Heimrecht, „best of three“, Abrechnung, Bildschirm „DEB-Pokal Auslosung“ | gemergt (Runde 3, #54) |
| 2 | Europapokal (Gruppe + Endrunde), Bildschirm Gruppen/Endrunde | gemergt (Runde 3, #54) |
| 3 | WM (2 Gruppen + K.-o.), Bildschirm Gruppe/Tabelle WM | gemergt (Runde 3, #54) |
| 4 | Stärken (Ligen, Länderteams) | gemergt (Runde 3, #54) |
| 5 | Statistiken: Ewige Tabelle (Totale), Früher | gemergt (Runde 3, #54) |
| 6 | Bundestrainer: WM-Kader, „WM Vorbereitung“ (Freundschaftsspiele), Etat; WM: Werte Deutschlands neu ausgewürfelt, Länder 20-mal getauscht | gemergt (Runde 3, #54) |
| 7 | Spielmodus „nur Weltkugel“: Spielvorbereitung, Vereinssaison überspringen, Menü des Bundestrainers | gemergt (#87 über #89) |
| 8 | Paarungen Liga (kein Videobeleg) | offen |
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | **wartet auf `livespiel`** (Tore, Vorlagen, Strafen je Spieler) |

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: Format, Dialog und automatisches Speichern gemergt (Runde 3, [archiv/runde-3.md](archiv/runde-3.md)); #80 in Runde 4

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`) – zurückgestellt, kein Original-Spielstand vorhanden.

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
