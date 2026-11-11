# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 16 – Planung offen (neue Leitstellen-Sitzung, `docs/vorlagen/leitstelle-start.md`)

Runde 15 abgeschlossen am 11.11.2026: [archiv/runde-15.md](archiv/runde-15.md). Grundlage für den Plan: Vormerkungen unten,
offene Issues #699, #703, #730 (Rest), #732, #734, #743, #757.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- DOSBox-Nachaufnahme für A115, A189, A321, A357, A364 (E14-5 = C): später, wenn der Nutzer Lust hat.
- `livespiel` (Torszenen, Anzeigetafel, Reporter; Nr. 14/16): Umsetzung als letzter Schritt vor v1.0; Analyse in
  `re-livespiel`. Noch unbekannt und bei der Umsetzung von `livespiel` mitzuanalysieren (zusammen mit L7, `tore\`-Format):
  575C3h (Ov 138h, Lader `tore\`, einziger Aufrufer 567C8h; R10R-01 #387).
- Highscore am Spielende und Saisonende im Modus „nur Weltkugel“ (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen Runde 16 (R15-E6 = A, Nutzer 11.11.2026)

- **#730 (Rest):** Belastungszuwachs je Reihe/Einsatz der Managerspieler (Original 50); Sägezahn bisher nur für die übrigen.
- **#732:** Konto im Modus „nur Weltkugel“ (Tagesabschluss für Satz 0); Altern/Laufzeit sind behoben.
- **#743:** Pool-Ziehung beim Saisonwechsel (DGROUP 0x381).
- **#757:** Aktienkurs 0 → Division durch null.
- **#734:** FILE13→14 als Eingriff/Sitzungszustand einordnen (Probe 11.11.2026: frisch geladen 20 Eingesetzte, die fünf stammten
  aus der DOSBox-Sitzung), danach schließen.
- **#699 (Rest), #703 (R701-06)** aus `pruefung-karte` Teil 1.
- **Testlücke R727N2-01** (Gesamtreview Runde 15): Rundreisetest prüft den exportierten Besitzer des Marktzwillings nicht
  (Mutation `marketTwins` → FILE41/43/43A/44 verlieren Poolplatz 179, Tests grün).
- **DOSBox-Aufnahme** (nach Lust des Nutzers, `vorlagen/dosbox-aufnahme.md`): Punkte unter „Zurückgestellt“, dazu EXPORT2C und
  die Exportstände ab Head `97c372a` (Restlücke R758-01).
- **Nach v1.0:** Einstellungen aus R15-E3 und bitgenauer Zufallsmodus (`anforderungen/zusatzfunktionen.md` C).

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
