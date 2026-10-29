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

### Spur `speichern-reihenfolge` – Aufzählungsreihenfolge nach Remove/Add (#80)

- **Ziel:** Befund S71-02 (#80, P3, in Runde 3 zurückgestellt): Nach Entfernen und Wiedereinfügen hängt die
  Reihenfolge von Wörterbüchern/Mengen vom Verlauf ab; nach dem Laden ist sie anders.
- **Abnahme:** (1) Gegenbeispiel aus dem Review (`docs/reviews/2026-10-28-speichern-68-70-71.md`, S71-02) als roter
  Test; (2) Lösung nach Wahl, im PR begründet: reihenfolgerelevante Aufrufer stabil sortieren **oder** Sammlungen beim
  Laden/Speichern in definierter Ordnung führen; (3) alle Zwillings- und Roundtrip-Proben grün, Schemaabdruck nur bei
  echter Formatänderung (dann mit Migration).
- **Dateigrenzen:** `Core/SaveGame*.cs`, betroffene Aufrufer minimal, `docs/formate/spielstand.md`.
- **Modell:** Claude Sonnet 5.5 (klein).

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
