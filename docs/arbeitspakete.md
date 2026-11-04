# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 13 – Planung durch die nächste Leitstelle

Runde 12 (03.–04.11.2026) ist abgeschlossen: [archiv/runde-12.md](archiv/runde-12.md) (Spuren, Integrationen, Reviews,
Entscheidungen E4, S-K6, D1–D10, Funktionskarte: Spielcode 90 % umgesetzt); frühere Runden in [archiv/](archiv/). Die Planung
von Runde 13 steht aus; Ausgangspunkt sind die folgenden Kandidaten und der Abschnitt „Zurückgestellt“.
**Schreibweise in diesem Abschnitt:** Adressen sind Dateioffsets mit Suffix `h`, damit die Funktionskarte den Plan nicht
als Fundstelle zählt (R10P-01). Im Code und in `docs/analyse/` gilt die normale Schreibweise.

### Kandidaten Runde 13

Nur Liste, keine Spurplanung; je Zeile die Fundstelle.

- **U15-Rest (D7 = D):** Start-Liga-Regel des Originals (auch bei fest gewähltem Verein, bei Bundesliga per Tausch) und
  Sonderfall „eine Saison“ (`Core/NewGame.cs`, `docs/transfer.md`; #493, #533). Beleg: DOSBox-Aufnahme Sitzung 4 (FILE27/28, liegt vor).
- **DOSBox-Auswertung:** Aufnahme `runde-12` nach `vorlagen/dosbox-aufnahme.md` vollständig abgelegt (Issue #534):
  Sitzung 1 Bundestrainer FILE18–21, 78 Bilder (Referenz-PR #11; A009, A010, A023, A027, A028, A058, A085, A163, A284, A293, A294,
  A300), Sitzung 2 Saisonwechsel FILE22–23 (#12; A085, A088, A091, A125), Sitzung 3 Kader FILE24–26 (#13; A166, A167, A189),
  Sitzung 4 Vereinsplatz beim Start FILE27–28 (#14; U15/D7 – Befund: „eine Saison“ nur mit Start-Liga Bundesliga wählbar).
  Noch nicht ausgewertet – Analysespur „Auswertung DOSBox“.
- **Restlücken `ui-meldungen` (#535):** `ArrowPair` in Werbung (2BA72h) und Stadion (3DD89h/3E0BCh) – Bremse des Aufrufers;
  Vereinsfenster aus „Stärken“ (Aufrufstelle); Vereinsrekorde aus Pokal-/Play-off-Spielen; Umbruchbreite `SaveLoadScreen.Wrap`.
- **R-K5 (#549):** Reihenfolge der Vereinsauswahl innerhalb der Seite (Video `oberflaeche_0128/_0133/_0136`; Zusammenhang mit
  S-K6/A194 prüfen), A347/F058 offen.
- **`[APPROX]` S-K6:** Mischzeitpunkt (`StartNextSeason` statt vor dem Fenster „Saisonende“, 5645Dh) und Zufallsquelle (#510).
- **P3-Issues:** R12S-01…03 (#521–#523), R12K-01…04 (#530–#533), R12U-03…05 (#539–#541); Hinweis `NewsBar.Shown` (Review #546).
- **Offene Prüfbereiche der Gesamt-Nachprüfung (#557):** Freiprüfung aller Bildschirme über Konstruktoren und indirekte Aufrufe
  (bisher Menükacheln, Saisonende, Spieltag); `[APPROX]` Zeitpunkt des Managerwechsels am Saisonende (`GameState.SeasonEndTurn.cs`).
- **Pixel offen:** G7 Text „(kein Bau...)“ fehlt im Code, Wortlaut von „(Noch n Wochen)“ (`[APPROX]`, `StadiumScreen.cs`) und
  Hinweiszeile „… ausbauen“ (x 213, y 201) belegen (3D45Fh); Farben 18/12/22 der Saisonende-Zeilen am Video (R-K2). G8 ist erledigt
  (Nachmessung mit DOSBox-Bild, `finanzen-rekonstruktion.md`).
- `livespiel` bleibt zurückgestellt (siehe „Zurückgestellt“).

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- `livespiel` (Torszenen, Anzeigetafel, Reporter; Nr. 14/16): Umsetzung als letzter Schritt vor v1.0; Analyse in
  `re-livespiel`. Noch unbekannt und bei der Umsetzung von `livespiel` mitzuanalysieren (zusammen mit L7, `tore\`-Format):
  575C3h (Ov 138h, Lader `tore\`, einziger Aufrufer 567C8h; R10R-01 #387).
- #349 Spieltag mit Managerverein ganz in der Minutenrechnung: Remake-Verhalten bleibt; Original als Einstellung nach
  v1.0 (`docs/anforderungen/zusatzfunktionen.md`, Abschnitt C).
- Import/Export von Original-Spielständen ins Remake; Highscore am Spielende und Saisonende im Modus „nur Weltkugel“
  (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

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
