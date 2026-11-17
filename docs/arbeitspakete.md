# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 22 – Planung offen (neue Leitstellen-Sitzung, `docs/vorlagen/leitstelle-start.md`)

Runde 21 abgeschlossen am 17.11.2026: [archiv/runde-21.md](archiv/runde-21.md). Grundlage für den Plan: Restliste
[analyse/restliste-v1.md](analyse/restliste-v1.md) („Klasse V nach Themen“, „Vorschlag Zuschnitt Runde 21 ff.“: übrige Spuren
`bundestrainer-satz`, `kaderplatz-werte`, `kader-regeln`, `transfer-pool`, `wettbewerbe-regeln`, `einzelpunkte`; danach
`marker-x`), neue V aus [analyse/re-vor-dosbox.md](analyse/re-vor-dosbox.md) („Zuschnittsvorschlag Runde 22“, u. a. A093 in
`kaderplatz-werte`), offene Issues #896, #897, #899, #908, #912, #914, Vormerkungen unten. **Modellwahl (Nutzer 17.11.2026):** je
Lauf möglichst wenig Tokens für ein brauchbares Ergebnis – Haiku, Sonnet, Opus; Fable nur in Ausnahmefällen.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen (nach Runde 21)

- **Runde 22:** #896 (A612 Play-off-Marken, vorher RE 0x32AA3/0x38402/0x376F0), #897 (Finanzübersicht Zeilen 3/4 und 6/7,
  Gegentore des Managertorhüters in Pokalen, `ChangeList` Gehalt 0, Verletzung außerhalb des Spiels 0x07462, Poolplatz 179,
  `SlotSeed`, Folgeverdacht A059), #899 (Präzisierungen für A093/A206/A059), #908 (HOK-Import Reihentausch, 0x952C beim Verlassen
  der Vereinsaufstellung); P3/Testlücken #912, #914 für die nächste `pruefung-karte`.
- **Testlücke Gesamtreview #848:** Speichern/Laden mitten in einer angehaltenen Live-Paarung (über die Oberfläche nicht
  erreichbar; prüfen, sobald ein Speicherweg im Live-Spiel dazukommt).
- **Vor v1.0 (R20-D3-Zusatz, Nutzer 15.11.2026):** Liste aller noch abweichenden Bereiche und Funktionen an den Nutzer
  (`python3 tools/reverse-engineering/markerliste.py --abweichungen`), der entscheidet, was noch geändert wird.
- **Mit v1.0 und dem bitgenauen Zufallsmodus (N6 = A, Nutzer 16.11.2026):** eigene Zufallsquellen der Darstellung (A061, A271,
  A274, A438, A452) erneut prüfen.
- **Nach v1.0:** Einstellungen aus R15-E3, bitgenauer Zufallsmodus (`anforderungen/zusatzfunktionen.md` C), Sprachen und
  Währungen (N1 = B, `anforderungen/zusatzfunktionen.md` A 2).

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
