# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 15 – Planung offen (neue Leitstellen-Sitzung, `docs/vorlagen/leitstelle-start.md`)

Runde 14 abgeschlossen am 10.11.2026: [archiv/runde-14.md](archiv/runde-14.md). Grundlage für den Plan: Vormerkungen unten,
offene Issues #620 (Rest), #698, #699, #703, #712, #716.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- DOSBox-Nachaufnahme für A115, A189, A321, A357, A364 (E14-5 = C): später, wenn der Nutzer Lust hat.

- `livespiel` (Torszenen, Anzeigetafel, Reporter; Nr. 14/16): Umsetzung als letzter Schritt vor v1.0; Analyse in
  `re-livespiel`. Noch unbekannt und bei der Umsetzung von `livespiel` mitzuanalysieren (zusammen mit L7, `tore\`-Format):
  575C3h (Ov 138h, Lader `tore\`, einziger Aufrufer 567C8h; R10R-01 #387).
- #349: in Runde 14 (R14-D4 = A, Spur `saison` Teil 4).
- Highscore am Spielende und Saisonende im Modus „nur Weltkugel“ (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).
  Import/Export ist in Runde 14 (E14-1, Spur `hok-import`).
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.
- Restlücke A134 (R688N-02, Review `reviews/2026-11-09-tagestakt-hauptweg-nachpruefung.md`): Die Tendenz je Takt (#688,
  Datei 0x07B64–0x07BFC) wirkt nur auf Kaderplätze mit Tendenzwert und Steuerbyte. Ein neues Spiel legt den Startkader ohne
  beides an (`TestGames.Create(3)`: 0 von 25), also wirkt sie dort erst auf importierte HOK-Stände und über 0x4907A neu angelegte
  Plätze. Kandidat Runde 15: Startwerte des Kaders beim Spielstart nach em.exe (Aufrufer von 0x4907A im Spielstart) lesen und
  übernehmen (Dateien `kader-start`).

### Vormerkungen Runde 15 (Nutzer 10.11.2026, Effizienz)

- **Spur `abgleich-original`** (Vorschlag 1): Werkzeug in `emtool`, das alle Paare aufeinanderfolgender Original-Spielstände
  (`*.HOK`, Referenz-Repo) durchläuft – Remake ab Stand n importieren, bis zum Zeitpunkt von Stand n+1 spielen, je Feld
  vergleichen (Konto, Kader, Zähler, Tabellen) – und jede Abweichung als Tabellenzeile bzw. `[GameDataTheory]` ausgibt.
  Ziel: Befunde entstehen im Test, nicht im Review. Modell: Claude Opus 5.5.
- **Spurmodell ohne Teil-PRs** (Vorschlag 3): Teile einer Spur sind Commits auf **einem** Branch `paket/<spur>` mit **einem** PR;
  Fortsetzungen arbeiten auf demselben Branch weiter. Kein eigener Integrations-PR je Spur, reviewt wird der Spur-PR.
  Umsetzung in `docs/vorlagen/spur-start.md` („Teile und Fortsetzung“) und `leitstelle-start.md` mit dem Plan der Runde 15.
- **Prozessdoku verdichten** (Rest von Vorschlag 5): `AGENTS.md`, `zusammenarbeit.md`, `docs/vorlagen/*` (heute rund 1.200 Zeilen
  Pflichtlektüre je Lauf) auf einen Kern von etwa 300 Zeilen; Sonderfälle in einen Anhang, der nur bei Bedarf gelesen wird.
  Regeln inhaltlich unverändert. Modell: Claude Sonnet 5.5 (mechanisch), Review Doku-Prüfung.
- Offene Issues aus Runde 14: #620 (Rest), #698, #699, #703.

## Paket `hok-format` – Original-Spielstände `*.HOK`

Branch: `paket/hok-format` · Status: Analyse gemergt (#176, Review #178): Verschlüsselung, Prüfsummen, Kopf und Blockfolge
belegt, `emtool hok dump`, Doku `docs/formate/spielstand-original.md` (offene Liste dort). Entscheidung Nutzer:
Sicher-/Ladesperre des Originals nicht übernehmen. Weiter in Runde 7 mit `hok-patch` (Spielstände fürs Original);
Import/Export ins Remake danach.

- Nachtrag (#197, Branch `paket/hok-format-2`): Kaderplätze, Spielerpool, Training und Taktik an FILE05 belegt;
  Kaderausgabe in `emtool hok dump`. Spielstände FILE01–05 und `KAPUTT.HOK` im Referenz-Repo `original/spielstaende/`.

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: PR 1–7 gemergt (Runde 3); Restarbeiten in Runde 4 gemergt; PR 8 in Runde 5 gemergt (`paarungen-liga`, #128). Regeln und Belege: [wettbewerbe.md](wettbewerbe.md)

| PR | Inhalt | Stand |
|---|---|---|
| 1 | DEB-Pokal: Auslosung, Heimrecht, „best of three“, Abrechnung, Bildschirm „DEB-Pokal Auslosung“ | gemergt (Runde 3, #54) |
| 2 | Europapokal (Gruppe + Endrunde), Bildschirm Gruppen/Endrunde | gemergt (Runde 3, #54) |
| 3 | WM (2 Gruppen + K.-o.), Bildschirm Gruppe/Tabelle WM | gemergt (Runde 3, #54) |
| 4 | Stärken (Ligen, Länderteams) | gemergt (Runde 3, #54) |
| 5 | Statistiken: Ewige Tabelle (Totale), Früher | gemergt (Runde 3, #54) |
| 6 | Bundestrainer: WM-Kader, „WM Vorbereitung“ (Freundschaftsspiele), Etat; WM: Werte Deutschlands neu ausgewürfelt, Länder 20-mal getauscht | gemergt (Runde 3, #54) |
| 7 | Spielmodus „nur Weltkugel“: Spielvorbereitung, Vereinssaison überspringen, Menü des Bundestrainers | gemergt (#87 über #89) |
| 8 | Paarungen Liga | gemergt (Runde 5, #128); Pixelvergleich Runde 6 (`pixel-wettbewerbe`) |
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | Runde 8 (`statistiken`, `nationalteam`) |

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: Format, Dialog und automatisches Speichern gemergt (Runde 3, [archiv/runde-3.md](archiv/runde-3.md)); #80 in Runde 4 behoben (#105)

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`) und Export per Vorlage (`hok-format`) – wartet auf Original-Spielstände des Nutzers.

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen auf den Ereignissen aus `spielberechnung` (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
