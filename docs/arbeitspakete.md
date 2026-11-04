# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 13 – DOSBox-Auswertung, U15-Rest, Restlücken, Bildschirmprüfung (ab 04.11.2026)

Runde 12 (03.–04.11.2026) ist abgeschlossen: [archiv/runde-12.md](archiv/runde-12.md) (Spuren, Integrationen, Reviews,
Entscheidungen E4, S-K6, D1–D10, Funktionskarte: Spielcode 90 %); frühere Runden in [archiv/](archiv/).
**Schreibweise in diesem Abschnitt:** Adressen sind Dateioffsets mit Suffix `h`, damit die Funktionskarte den Plan nicht
als Fundstelle zählt (R10P-01). Im Code und in `docs/analyse/` gilt die normale Schreibweise.

**Ziel der Runde:** alle Kandidaten aus Runde 12 (Archiv-Übergang, Stand `main` @ `a773c1c`) genau einer Spur zuordnen und
abarbeiten: Auswertung der DOSBox-Aufnahme `runde-12` (FILE18–28, Issue #534) mit Umsetzung der belegten Inventur-Punkte,
U15-Rest (D7 = D), Restlücken `ui-meldungen` (#535), R-K5-Reihenfolge (#549), `[APPROX]` S-K6 (#510), alle P3-Issues
(#521–#523, #530–#533, #539–#541, #565), die offenen Prüfbereiche der Gesamt-Nachprüfung (#557) und die offenen Pixel
(G7, R-K2). `livespiel` bleibt zurückgestellt. Entscheidungen D1–D3 und Go des Nutzers am 04.11.2026 (unten).

**Base für alle Spuren:** aktueller `main` (Planstand `a773c1c`); tatsächlichen Base-SHA in den Draft-PR. Regeln wie Runde 12
(Archiv, Absätze „Base für alle Spuren“ und „Zuständigkeit für gemeinsame Dateien“): Befund reproduzieren, roter
Regressionstest mit Fundstelle, dann grün; Mehrsaisonprobe (4 Wechsel, passiv und aktiv) bei Saisonwechsel, Kader oder Geld;
Schema-Änderung mit Migration in neuer Datei `Core/SaveGame.Migration.<Spur>[<n>].cs` (Stand Schema 36, die Leitstelle
nummeriert); `[ORIGINAL]`/`[APPROX]` mit `Datei 0x…`; Befund-Issues mit `Fixes #<nr>`; höchstens drei Fortsetzungen je Spur.
Bei Unklarheit gilt „wie im Original“; bleibt es unklar, Teil blockiert lassen und die Leitstelle entscheiden lassen.
**Entwickler und Reviews sind Claude-Sitzungen** (geplante Läufe); ChatGPT nur für Reviews.
**Abhängigkeit:** Teile mit „nach `re-dosbox`“ beginnen erst, wenn deren PR gemergt ist (sonst blockiert, übrige Teile zuerst).
**Integration** je Spur am Ende, Folge: `kader-start` → `saison` → `nationalteam` → `ui-rest`; danach `pruefung-karte`
(Welle 2). Am Rundenende Gesamtreview nach D3.

### Zuständigkeit für gemeinsame Dateien

Wie Runde 12 (Archiv) mit diesen Änderungen:
- `docs/analyse/re-runde-13.md`: allein `re-dosbox`. `docs/analyse/inventur.md` (Stand je Zeile), `docs/re-abdeckung.md`,
  `tools/reverse-engineering/*`: allein `pruefung-karte` (Welle 2); die anderen Spuren nennen ihre Zeilen im PR. Ausnahme:
  #565 (A324, ein Satz) darf `ui-rest` ändern.
- `UI/Screens/SetupScreen.cs`: allein `kader-start` (U15-Rest, R-K5).
- `Core/CupsNational*.cs`, `Core/MatchEngine.National.cs`, `UI/Screens/Competitions/NationalMenu.cs`, `WorldCupScreen.cs`,
  `NationalLineupScreen.cs`: allein `nationalteam`; `UI/Screens/MainMenuScreen.cs` (nur A300, minimal): `nationalteam`.
- `Core/SeasonFlow*.cs`, `Core/GameState.SeasonEndTurn.cs`, `Core/GameState.Integration.cs` (nur Saisonwechsel im Modus
  „nur Weltkugel“, A058/A163): allein `saison`.
- `Core/Squad*.cs`, `Core/StadiumStatistics.cs`, `Core/MatchEngine.Managed.cs` und `MatchEngine.Live.cs` (nur Kaderreihenfolge
  A085/A091/A125), `Core/Player.Contract.cs` (nur A125): `kader-start`.
- `Core/StatisticsClub.cs` (Vereinsrekorde), `UI/Screens/Manager/*`, `UI/Screens/SaveLoad/*`, `UI/Screens/Competitions/ClubInfoWindow.cs`
  und die Aufrufstelle in „Stärken“: `ui-rest`.
- Tests nur in den Testdateien der eigenen Spur; `tests/**/Support/` allein `pruefung-karte`.

### Spur `re-dosbox` – Auswertung der DOSBox-Aufnahme `runde-12` (zuerst, nur Doku)

- **Inhalt:** Spielstände FILE18–28 und Bilder aus `original/spielstaende/` (Referenz-Repo, README dort mit Beobachtungen des
  Nutzers; `emtool hok dump`, Vergleich gegen Remake-Code und em.exe) je Inventur-ID auswerten: Sitzung 1 Bundestrainer
  (A009, A010, A023, A027, A028, A058, A085, A163, A284, A293, A294, A300), Sitzung 2 Saisonwechsel (A085, A088, A091,
  A125), Sitzung 3 Kader (A166, A167, A189), Sitzung 4 Vereinsplatz beim Start (U15/D7: Start-Liga-Regel bei fest gewähltem
  Verein, Tausch bei Bundesliga, „eine Saison“ nur mit Start-Liga Bundesliga wählbar – Belegstelle in em.exe für die
  Sperre im Spielvorbereitungs-Bildschirm nachlesen, 28030h ff.). Dazu **S-K6-Rest:** Mischzeitpunkt 5645Dh gegenüber dem
  Fenster „Saisonende“ und Zufallsquelle der 55 Tausche; rechnet die Fair-Play-Prämie 27213h über die alte oder neue Liga
  (`re-saisonwechsel.md`, #510)? Und **R-K5:** Reihenfolge der Vereinsauswahl innerhalb der Seite (28030h, A347/F058,
  Video `oberflaeche_0128/_0133/_0136`, Zusammenhang mit der Satzfolge S-K6/A194).
- **Abnahme:** wie Runde 12 `re-pruefung`: Datei `docs/analyse/re-runde-13.md` mit Ergebnis je ID (belegt / widerlegt /
  nicht entscheidbar, Fundstelle: Datei, Byte, Bild) und Abschnitt „Umsetzung“ je Punkt (Ziel, Dateien, Testidee, Spur dieser
  Runde). Selbstprüfung `funktionskarte.py --csv` ohne `--write`. Ein Lauf, keine Teile, keine Fortsetzung.
- **Dateigrenzen:** nur `docs/analyse/re-runde-13.md` und der eigene Statuspunkt. **Modell:** Claude Opus 5.5. **Status:** fertig, PR #567 (04.11.2026): `docs/analyse/re-runde-13.md`, 25 Punkte ausgewertet, 8 offene Fragen an die Leitstelle.

### Spur `kader-start` – U15-Rest, Vereinsauswahl, Kaderbefunde

- **Teile:**
  1. **R12K-01 bis R12K-04 (#530–#533):** gezielter Test der Neuanlage im Poolplatz (U5), Kommentar `Transfer.Pool.cs`,
     Testhelfer `KeepSquad` nur mit Angebot verlängern (Kaderschwund sichtbar), Doku-Stand U15 in `docs/transfer.md`.
     `Fixes #530`, `#531`, `#532`, `#533`.
  2. **U15-Rest (D7 = D, nach `re-dosbox`):** Start-Liga-Regel des Originals beim Spielstart auch bei fest gewähltem Verein
     (bei Bundesliga per Tausch), Sonderfall „eine Saison“ nur mit Start-Liga Bundesliga (Sperre im `SetupScreen` wie im
     Original); `[APPROX]` in `Core/NewGame.cs` auflösen, Mehrsaisonprobe.
  3. **R-K5 (#549, nach `re-dosbox`):** Reihenfolge der Vereinsauswahl innerhalb der Seite nach Befund (A347/F058),
     Marker in `SetupScreen.cs` nachziehen.
  4. **Kaderbefunde aus `re-dosbox`:** A166/A167 (Eingesetzt-Vorgabe, Transfer), A189 (Verletzte/Gesperrte in der Aufstellung),
     A085/A091/A125 (Kaderreihenfolge) – nur die als abweichend belegten Punkte umsetzen, belegte Übereinstimmungen auf
     `[ORIGINAL]`; Mehrsaisonprobe.
- **Dateigrenzen:** `Core/NewGame.cs`, `Core/Transfer*.cs`, `Core/Player*.cs`, `Core/Squad*.cs`, `Core/StadiumStatistics.cs`,
  `UI/Screens/SetupScreen.cs`, `UI/Screens/Team/*` außer `TeamUi.cs`, Tests, `docs/transfer.md`; `MatchEngine.Managed.cs`/`.Live.cs`
  nur die Kaderreihenfolge. **Modell:** Claude Opus 5.5. **Status:** geplant.

### Spur `saison` – Satzfolge, Managerwechsel, Saisonwechsel „nur Weltkugel“

- **Teile:**
  1. **R12S-01 bis R12S-03 (#521–#523):** Mehrsaisonprobe erkennt fehlende Neumischung, Abbruchzweig `AdvanceLeagueSlots`,
     Doku-Rest Schema 33 → 34. `Fixes #521`, `#522`, `#523`.
  2. **`[APPROX]` Zeitpunkt des Managerwechsels am Saisonende** (`GameState.SeasonEndTurn.cs`, offener Bereich aus #557):
     Fundstelle in em.exe lesen, angleichen oder auf `[ORIGINAL]`.
  3. **S-K6-Rest (nach `re-dosbox`):** Mischzeitpunkt (vor dem Fenster „Saisonende“ statt `StartNextSeason`, 5645Dh) und
     Zufallsquelle nach Befund angleichen; Fair-Play-Prämie über die belegte Liga; Schema + Migration nur, falls nötig.
  4. **A058/A088/A163 (nach `re-dosbox`):** Saisonwechsel im Modus „nur Weltkugel“ (Teamwert-Wanderung, Oberliga-Austausch,
     Zurücksetzen der Spielerzähler) nach Befund; Mehrsaisonprobe im Modus „nur Weltkugel“.
- **Dateigrenzen:** siehe Zuständigkeit; Tests, `docs/analyse/re-saisonwechsel.md`, `docs/saison.md`. **Modell:** Claude Opus 5.5.
  **Status:** Teil 1 in Review (PR #569), Teil 2 in Review (PR #571), Teil 3 in Review (PR #579), Teil 4 in Review (A058, A163; A088 wartet auf Entscheidung der Leitstelle).

### Spur `nationalteam` – Bundestrainer nach der DOSBox-Aufnahme (nach `re-dosbox`)

- **Teile:**
  1. **Kader und Werte:** A023 (Spielerfelder 10h–12h als KO/TE/FO), A009 (Länderwerte ohne Aufstellung), A010 (verdrängter
     Spieler), A085 (Kaderplätze des Bundestrainers) nach Befund angleichen oder `[ORIGINAL]`.
  2. **Ablauf der WM:** A027 (Halt vor dem ersten WM-Spiel), A028 (Zug des Bundestrainers nach dem Ausscheiden: jeder WM-Tag
     oder nur Spieltage Deutschlands), A284 (Kachelsperre „Mannschaft“), A300 (Logo-Klick ohne Kaderwarnung, `MainMenuScreen.cs`
     minimal).
  3. **WM-Tabelle:** A293 (Lage der Knöpfe „Gruppe 1/2“, „Endrunde“), A294 (Linksklick außerhalb kehrt zurück) gegen die Bilder
     `file2*_*` (Pixel: Lage/Farbe, Bildschirmfoto-Ablauf).
- **Dateigrenzen:** siehe Zuständigkeit; Tests, `docs/wettbewerbe.md`. **Modell:** Claude Opus 5.5. **Status:** geplant.

### Spur `ui-rest` – Restlücken `ui-meldungen`, Pixel, Hauptmenü-Tests

- **Teile:**
  1. **Restlücken #535:** `ArrowPair` in Werbung (2BA72h) und Stadion (3DD89h/3E0BCh) anschließen (Bremse des Aufrufers
     nachlesen); Vereinsfenster aus „Stärken“ (Aufrufstelle); Umbruchbreite `SaveLoadScreen.Wrap` belegen (158 px `[APPROX]`).
  2. **Vereinsrekorde** aus Pokal-/Play-off-Spielen (`StatisticsClub.cs`, `[APPROX]`): Fundstelle lesen, angleichen; Probe über
     eine Saison mit Pokal und Play-offs.
  3. **Pixel:** G7 Text „(kein Bau...)“ und Wortlaut „(Noch n Wochen)“, Hinweiszeile „… ausbauen“ (x 213, y 201) an 3D45Fh und
     DOSBox-Bildern belegen (`StadiumScreen.cs`); R-K2 Farben 18/12/22 der Saisonende-Zeilen am Video; #565 (A324, Satz in
     `inventur.md`). `Fixes #565`.
  4. **R12U-03 bis R12U-05 (#539–#541):** Testlücke „Zeichnen ändert nichts“ im Hauptmenü, Gehaltsfaktor im ersten Zug eines
     neuen Spiels (Original prüfen: 44E22h), Kommentar `MainMenuEventTests.cs`; Hinweis `NewsBar.Shown` aus Review #546 prüfen.
     `Fixes #539`, `#540`, `#541`.
- **Dateigrenzen:** siehe Zuständigkeit; `UI/Screens/MainMenuScreen.cs` nur für #540 minimal; Tests; `docs/finanzen-rekonstruktion.md`.
  **Modell:** Claude Sonnet 5.5. **Status:** geplant.

### Spur `pruefung-karte` – Welle 2 (nach der Integration der übrigen Spuren)

- **Teile:**
  1. **Freiprüfung aller Bildschirme** (offener Bereich aus #557): ein Test, der jeden `Screen` (über Konstruktoren, Menükacheln
     und indirekte Aufrufe, Reflexion über alle Typen) mit Originaldaten öffnet und zeichnet und den Spielzustand vorher/nachher
     bytegleich vergleicht (Sichern in Speicher); Abweichungen als Befund-Issues (P2), nicht selbst beheben, außer trivial.
  2. **Funktionskarte und Inventur** auf den Rundenstand (`funktionskarte.py --write`, zweiter Lauf ohne Diff), Stand je Zeile in
     `analyse/inventur.md` für alle in Runde 13 belegten IDs.
- **Dateigrenzen:** `tests/**/Support/`, neue Testdatei, `tools/reverse-engineering/*`, `docs/re-abdeckung.md`, `docs/analyse/inventur.md`.
  **Modell:** Claude Sonnet 5.5. **Status:** geplant.

### Integration Runde 13

Folge nach L15 (Nutzer 04.11.2026): `saison` → `nationalteam` → `ui-rest` → `kader-start` → `pruefung-karte`. Entscheidungen der
Leitstelle L1–L12 in #567, L13–L15 in #578/#583.

- **`saison`** – Branch `integration/runde-13-saison` (#569, #571, #579, #583 ohne Konflikt, `main` @ `48e1e67` nachgezogen):
  `tools/pruefen.sh` alle drei Läufe an `07e8a39` bestanden (2178/0/0, 1834/0/308, Pflichtlauf 1835/343). Schema 38 mit
  Leerschritt 37 (L5); A088 bleibt `[APPROX]` (L14, Kandidat Runde 14). Review offen.

### Entscheidungen zum Start (Nutzer, 04.11.2026)

- **D1 = A:** Zuschnitt wie oben – sechs Spuren, `re-dosbox` zuerst, vier Umsetzungsspuren, `pruefung-karte` als Welle 2.
- **D2 = A:** Spieldauer „eine Saison“ nur mit Start-Liga Bundesliga wie im Original (Sperre im `SetupScreen`, `kader-start`
  Teil 2); Lockerung erst nach v1.0 als Einstellung (`anforderungen/zusatzfunktionen.md`, Abschnitt C).
- **D3 = A:** Gesamtreview am Rundenende durch ChatGPT (GPT-6.1 Terra, Codex mit Netzzugang) und Nachprüfung durch Claude,
  wie Runde 12.
- **Go** mit den Modellen je Spur wie oben (Nutzer, 04.11.2026). Umsetzung, Integration und Abschluss durch eine neue
  Leitstelle mit Claude Opus 5.5 (Entscheidung Nutzer 04.11.2026, PR #561).

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
