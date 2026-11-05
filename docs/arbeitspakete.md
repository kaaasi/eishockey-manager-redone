# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Übergang zu Runde 14 (ab 05.11.2026)

Runde 13 (04.–05.11.2026) ist abgeschlossen: [archiv/runde-13.md](archiv/runde-13.md) (Spuren, Integrationen, Reviews,
Entscheidungen L1–L15, Funktionskarte: Spielcode 90 %); frühere Runden in [archiv/](archiv/). Plan Runde 14 folgt durch die
Leitstelle nach den Vorentscheidungen des Nutzers (Issue #601).

### Vorentscheidungen Runde 14 (Nutzer, 04.11.2026, Issue #601)

- **E14-1 = A + C:** Restpunkte (unten) und zusätzlich Import/Export von Original-Spielständen `*.HOK` (Grundlage `hok-format`,
  `formate/spielstand-original.md`). `livespiel` bleibt zurückgestellt.
- **E14-2 = A:** Start ohne Warten auf alle Aufnahmen; bereits abgelegte Sitzungen der Aufnahme `runde-13` sofort auswerten,
  spätere in einem Folgeteil derselben Analysespur.
- **E14-3 = A:** Gesamtreview durch ChatGPT als Vollprüfung (`vorlagen/review-anfrage.md`, #614/#617); keine Nachprüfung durch
  Claude außer dem Delta von Fixes.

### Kandidaten Runde 14

- **DOSBox-Aufnahme `runde-13`** (Plan `vorlagen/dosbox-aufnahme.md`, FILE29–FILE49): Auswertung je Sitzung, sobald im
  Referenz-Repo abgelegt; daran hängen A088 (L14), U15-c (L2), A009 leer, A085 Handaufnahme, A167/A189 Gesperrte, Taktik-Befund
  DGROUP 375h und die übrigen IDs der Sitzungen.
- **P3-Issues aus den Reviews:** #591, #592 (Satzfolge, Fair-Play), #594, #595 (Länderwerte), #597, #598 (Stadionpfeile,
  Adresse), #603–#605 (Saaten, Vorbedingungen, Start-Liga ohne Manager), #610, #611 (Freiprüfung, HOK-Regel), #619–#621
  (Fundstellen, Tests ohne Wirkung, Saaten/Doku), #625 (Talentsuche, Aufruferliste).
- **Nebenbefunde `nationalteam`** (#580): Reihenlogik der Vereinsaufstellung wie A010; Sperre Transfer/Training ab der
  WM-Gruppenphase ohne Inventur-ID.
- **Import/Export `*.HOK`** (E14-1 C).
- WM-Kader-Reihenfolge beim Einsortieren (3CABFh, Restlücke #623); `OutsidePlaces` in `SeasonFlow.Oberliga.cs` paart nach Index,
  Kommentar A112 veraltet (#600).

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- `livespiel` (Torszenen, Anzeigetafel, Reporter; Nr. 14/16): Umsetzung als letzter Schritt vor v1.0; Analyse in
  `re-livespiel`. Noch unbekannt und bei der Umsetzung von `livespiel` mitzuanalysieren (zusammen mit L7, `tore\`-Format):
  575C3h (Ov 138h, Lader `tore\`, einziger Aufrufer 567C8h; R10R-01 #387).
- #349 Spieltag mit Managerverein ganz in der Minutenrechnung: Remake-Verhalten bleibt; Original als Einstellung nach
  v1.0 (`docs/anforderungen/zusatzfunktionen.md`, Abschnitt C).
- Highscore am Spielende und Saisonende im Modus „nur Weltkugel“ (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).
  Import/Export ist Kandidat Runde 14 (E14-1).
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
