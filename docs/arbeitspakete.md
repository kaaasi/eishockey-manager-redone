# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 12 – Planung durch die nächste Leitstelle

Runde 11 (Umsetzung der Inventur, 02.–03.11.2026) ist abgeschlossen: [archiv/runde-11.md](archiv/runde-11.md) (Spuren,
Integrationen, Reviews, Funktionskarte, „Entscheidungen Rundenende“); frühere Runden in [archiv/](archiv/). Die Planung
von Runde 12 steht aus; Ausgangspunkt sind die folgenden Kandidaten sowie die Abschnitte „Nicht in Runde 11“ und „Zurückgestellt“.
**Schreibweise in diesem Abschnitt:** Adressen sind Dateioffsets mit Suffix `h`, damit die Funktionskarte den Plan nicht
als Fundstelle zählt (R10P-01). Im Code und in `docs/analyse/` gilt die normale Schreibweise.

### Kandidaten Runde 12

Nur Liste, keine Spurplanung; je Zeile die Fundstelle.

- **E2 (Entscheidung Nutzer 03.11.2026, A):** Text „noch gesperrt !“ – im Original Zeile 1 „Spieler ist“ (DGROUP 1255h), im
  Remake der Spielername: erst die Bedingung am Aufruf 40B55h in em.exe prüfen, dann angleichen (`LineupScreen.cs`).
- **E3 (C):** Schließen des Meldungsfensters per Taste/Rechtsklick (`TeamUi.MessageBox`, [APPROX]) zurückbauen, bis v1.0 nur
  Linksklick wie im Original; danach abschaltbare Einstellung (`anforderungen/zusatzfunktionen.md` Abschnitt C).
- **E4 (A):** Das Hauptmenü setzt beim bloßen Anzeigen ein Merkmal im Spielzustand (vermutlich „Nachricht gelesen“,
  Hinweis aus Review #443): Original untersuchen, danach angleichen oder Regel in `AGENTS.md`/Doku ergänzen.
- **E5 (B):** S-K1 – Konto am 29. April auf 400.000 DM gesetzt, auch bei hohem Guthaben (562D8h): gezielt in em.exe nachprüfen
  (`SeasonFlow.Oberliga.cs`, `docs/analyse/re-saisonwechsel.md`).
- **WS-04:** Neuzählung 38A36h – Grenze 5 und Abzug vom Sturm ungetestet (Review #468,
  `reviews/2026-11-02-integration-wettbewerbe-spiel.md`).
- **A184, A174:** Belastung +13h als eigenes Feld statt `Energy`; Computervereine ohne Kaderbezug (Entscheidung Nutzer
  02.11.2026, Review #440; `analyse/inventur.md`).
- **SW-02, SW-05, SW-06:** Ewige Tabelle ohne die letzte Saison bei Entlassung aller Manager; Partner im Doppelwechsel nicht
  festgelegt; Paarindex im Fall „doppelter Platz“ (Reviews #441, #458, `reviews/2026-11-02-saisonwechsel*.md`).
- **R11G-01 bis R11G-06 (#456):** P3-Befunde der geld-Integration (`reviews/2026-11-02-integration-geld.md`).
- **U8:** Nationalteam Satz 5 mit der Regel der automatischen Aufstellung (426BCh/42838h, `CupsNational.Lineup.cs`);
  **U4, U5, U9, U15** aus `analyse/re-nachtrag.md`.
- **UM-2:** Vereinsinfo-Kurve 4D385h (Kurve x + 30, y − 30); das Vereinsfenster fehlt im Remake (`TeamInfoWindow` ist nur das
  Länderfenster) – Review #469.
- **UM-4:** Aufrufer von 0C0B8h ohne Entsprechung im Remake: 46DEFh, 40EF7h, 414C6h, 3DE9Fh, 5C559h/5C59Ah (Review #469).
- **UM-3, Rest:** übrige [APPROX] der `MessageBox` (Knopf „Na gut.“, alter Rahmen bei mehr als vier Zeilen).
- **`FinanceStadium.Strength`** mit Spielerwerten statt Vereinsfeldern (4D316h, PR #438) – Geld ändert sich.
- **Beobachtung WS-05/Gesamtreview:** der Rückfallweg von `TestSquadKeeper` (Spieler eines Computerkaders umsetzen) verdeckt, ob
  zu Saisonbeginn Vertragslose verpflichtbar sind – Spur `kader-transfer` prüfen.
- **Gesamtreview #474 („vorläufig nein“, Prüfung nicht vollständig, kein neuer Produktfehler):** offene Prüfbereiche – echter
  Schema-22-Spielstand mit Mehrsaisonfortsetzung, Doppelwechsel per DOSBox (`reviews/2026-11-03-runde-11-gesamt.md`).
- **P3 Nr. 2, 4, 6 aus Runde 11:** siehe „Nicht in Runde 11“ unten.
- `livespiel` bleibt zurückgestellt (siehe „Zurückgestellt“).

### Nicht in Runde 11

- `wettbewerbe-spiel` Teil 5, Rest (Entscheidung Nutzer 02.11.2026): A184 (Belastung +13h statt Energy, fremde Dateien) und
  A174 (Computervereine ohne Kaderbezug) – Runde 12.
- SW-02 (P3, Review #441): Ewige Tabelle ohne die letzte Saison, wenn am 29. April alle Manager entlassen werden – Runde 12.

- P3 Nr. 2 (Pixel nach Analyse: G7, G8, M-K4, M-K9, R-K2…R-K5, L8), Nr. 4 (M-K10, R-K6 – Entscheidung Nutzer) und Nr. 6
  (S-K6, nur Analyse): Kandidaten für Runde 12.

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
