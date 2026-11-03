# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 12 – Restpunkte Runde 11, Prüfaufträge E2–E5 (Umsetzung läuft, Start 03.11.2026)

Runde 11 (Umsetzung der Inventur, 02.–03.11.2026) ist abgeschlossen: [archiv/runde-11.md](archiv/runde-11.md) (Spuren,
Integrationen, Reviews, Funktionskarte, „Entscheidungen Rundenende“ E1–E6); frühere Runden in [archiv/](archiv/).
**Schreibweise in diesem Abschnitt:** Adressen sind Dateioffsets mit Suffix `h`, damit die Funktionskarte den Plan nicht
als Fundstelle zählt (R10P-01). Im Code und in `docs/analyse/` gilt die normale Schreibweise.

**Ziel der Runde:** alle Kandidaten aus Runde 11 (Liste im Archiv-Übergang, Stand `main` @ `e401dbd`) genau einer Spur
zuordnen und abarbeiten: Prüfaufträge E2, E4, E5 und S-K6 in em.exe, Rückbau E3, Restbefunde der Reviews (R11G, SW, WS, UM),
Umsetzungen U4/U5/U8/U9/U15 aus `analyse/re-nachtrag.md`, A174/A184, die zwei offenen Prüfbereiche des Gesamtreviews #474
und P3 Nr. 2 (Pixel). `livespiel` bleibt zurückgestellt. Entscheidungen D1–D3 und Go des Nutzers am 03.11.2026 (unten).

**Base für alle Spuren:** aktueller `main` (Planstand `e401dbd`); tatsächlichen Base-SHA in den Draft-PR. Regeln wie Runde 11
(Archiv, Absatz „Base für alle Spuren“ und „Ablauf: Teile und Selbstfortsetzung“): Befund reproduzieren, roter
Regressionstest mit Fundstelle, dann grün; Mehrsaisonprobe (4 Wechsel, passiv und aktiv) bei Saisonwechsel, Kader oder Geld;
Schema-Änderung mit Migration in neuer Datei `Core/SaveGame.Migration.<Spur>[<n>].cs` (Stand Schema 32, die Leitstelle
nummeriert); `[ORIGINAL]`/`[APPROX]` mit `Datei 0x…`; Befund-Issues mit `Fixes #<nr>`; höchstens drei Fortsetzungen je Spur.
**Entwickler und Reviews sind Claude-Sitzungen** (geplante Läufe); ChatGPT nur für Reviews.
**Abhängigkeit:** Teile mit „nach `re-pruefung`“ beginnen erst, wenn deren PR gemergt ist (sonst blockiert, übrige Teile zuerst).
**Integration** je Spur am Ende, Folge: `geld` → `saisonwechsel` → `kader-transfer` → `wettbewerbe-spiel` → `ui-meldungen`;
danach `pixel-marker` (Welle 2).

### Zuständigkeit für gemeinsame Dateien

Wie Runde 11 (Archiv, „Zuständigkeit für gemeinsame Dateien“) mit diesen Änderungen:
- `UI/Screens/Team/TeamUi.cs` (nur `MessageBox`) und `UI/Screens/MainMenuScreen.cs` (nur E4, minimal): `ui-meldungen`;
  `kader-transfer` ändert `TeamUi.cs` nicht.
- `Core/SeasonFlow*.cs` einschließlich `SeasonFlow.Oberliga.cs` (E5): allein `saisonwechsel`.
- `Core/Player.Attributes.cs` (nur das Feld Belastung, A184): `wettbewerbe-spiel`.
- `docs/analyse/re-runde-12.md`: allein `re-pruefung`. `tools/reverse-engineering/*`, `docs/re-abdeckung.md`,
  `docs/analyse/inventur.md` (Stand je Zeile): allein `pixel-marker` (Welle 2); die anderen Spuren nennen ihre Zeilen im PR.

### Spur `re-pruefung` – Prüfaufträge in em.exe (zuerst, nur Doku)

- **Inhalt:** E2 – Bedingung am Aufruf 40B55h („noch gesperrt !“, Original Zeile 1 „Spieler ist“, DGROUP 1255h); E4 – was
  das Original beim Anzeigen des Hauptmenüs am Spielzustand ändert (Hinweis Review #443, „Nachricht gelesen“); E5 – S-K1:
  Konto am 29. April auf 400.000 DM (562D8h), Bedingung und Fälle (hohes Guthaben, Schulden, Oberliga); S-K6 – Umordnung der
  Vereinssätze zwischen 29.04. und 10.09. (A194; FILE13/FILE14 mit `emtool hok` vergleichen); UM-4 – die Aufrufer von 0C0B8h
  (46DEFh, 40EF7h, 414C6h, 3DE9Fh, 5C559h/5C59Ah): Anlass, Text, Zeitpunkt; UM-2 – Aufbau des Vereinsfensters und der Kurve
  4D385h (x + 30, y − 30).
- **Abnahme:** wie Runde 10 „Abnahme je Spur“ (Selbstprüfung `funktionskarte.py --csv` ohne `--write`); Datei
  `docs/analyse/re-runde-12.md` mit Abschnitt „Umsetzung“ je Punkt (Ziel, Dateien, Testidee, Spur dieser Runde). Ein Lauf,
  keine Teile, keine Fortsetzung.
- **Dateigrenzen:** nur `docs/analyse/re-runde-12.md` und der eigene Statuspunkt. **Modell:** Claude Opus 5.5. **Status:** geplant.

### Spur `geld` – Testlücken, Stadionstärke, Pixel Finanzen

- **Teile:**
  1. **R11G-01 bis R11G-04, R11G-06 (#456):** Bauende-Woche, alle 11 Zeilen der Bauzeittabelle, Jugend-Platzverlauf mit
     festem Zufall als Sollwert, Doku `AnswerConstruction`, Vermerk Tagesanschlüsse. `Fixes #456`.
  2. **`FinanceStadium.Strength`** mit Spielerwerten statt Vereinsfeldern (4D316h, PR #438) – Geldwirkung, Mehrsaisonprobe.
  3. **Pixel G7, G8:** Stadiontabelle 3D45Fh, Finanzübersicht 57CFBh/57ECFh/581C2h gegen Video (`boerse-start`).
- **Dateigrenzen:** `Core/Finance*.cs`, `UI/Screens/Manager/*`, Tests, `docs/finanzen-rekonstruktion.md`. **Modell:** Claude
  Sonnet 5.5. **Status:** geplant.

### Spur `saisonwechsel` – Restbefunde, S-K1, Schema-22-Probe

- **Teile:**
  1. **SW-02:** Ewige Tabelle ohne die letzte Saison, wenn am 29. April alle Manager entlassen werden (Review #441).
  2. **SW-05, SW-06:** Partner im Doppelwechsel im Test festlegen (`down[k]`); Paarindex im Fall „doppelter Platz“ (Review #458).
  3. **E5/S-K1** (nach `re-pruefung`): Konto am 29. April nach dem Befund angleichen oder Marker auf `[ORIGINAL]`
     (`SeasonFlow.Oberliga.cs`, `docs/analyse/re-saisonwechsel.md`); **S-K6** umsetzen, falls die Analyse eine Abweichung zeigt.
  4. **Gesamtreview #474, Bereich 1:** echter Schema-22-Spielstand (vom Code am letzten Schema-22-Commit erzeugt, kein
     Originalmaterial) als Testdatei, laden, migrieren, 4 Saisonwechsel weiterspielen.
  5. **Gesamtreview #474, Bereich 2 (D3):** Spielstand für den Doppelwechsel per `hok-patch` vorbereiten, Aufnahmeablauf
     für den Nutzer im PR; Auswertung nach der Aufnahme (Teil bleibt bis dahin begründet offen).
- **Dateigrenzen:** siehe Zuständigkeit; Tests (Testdateien unter `tests/**/TestData/`). **Modell:** Claude Opus 5.5. **Status:** geplant.

### Spur `kader-transfer` – Nachträge U4/U5/U9/U15, E2

- **Teile:**
  1. **U4** Verfall der Angebote (Sperre random(9, 17), Bits 20h–80h mit 1/4); **U5** Laufbahnende ohne Managerkader
     (Alter > random(37, 42), Neuanlage im Pool) – Mehrsaisonprobe.
  2. **U9** Spielplan aus der festen Tabelle (Runden 2/4 getauscht); **U15** Vereinsplatz beim Start (8.1–8.3,
     Europapokal-Filter) – `Core/NewGame.cs`; Anzeige `LeaguePairingsScreen.cs` nur prüfen, Änderungen dort als Anschluss melden.
  3. **WS-05:** Rückfallweg von `TestSquadKeeper` so einschränken, dass sichtbar wird, ob zu Saisonbeginn Vertragslose
     verpflichtbar sind; Befund im PR.
  4. **E2** (nach `re-pruefung`): Text „noch gesperrt !“ in `LineupScreen.cs` angleichen.
- **Dateigrenzen:** `Core/Transfer*.cs`, `Core/Player*.cs` (außer Belastung), `Core/Squad.Setup.cs`, `Core/NewGame.cs`,
  `UI/Screens/Team/*` außer `TeamUi.cs`, Tests, `docs/transfer.md`. **Modell:** Claude Sonnet 5.5. **Status:** geplant.

### Spur `wettbewerbe-spiel` – Serien, Belastung, Computervereine, Nationalteam

- **Teile:**
  1. **Serien Pokal/Play-offs:** `CountSeries` (`StatisticsClub.cs`) für DEB-Pokal, Play-offs und Aufstiegsrunde anschließen
     (R11A-01, Reviews #468/#478); **WS-04:** Neuzählung 38A36h, Grenze 5 und Abzug vom Sturm testen.
  2. **A184:** Belastung (Spielerplatz +13h) als eigenes Feld statt `Energy` (Schema + Migration); Mehrsaisonprobe.
  3. **A174:** Computervereine ohne Kaderbezug (Entscheidung Nutzer 02.11.2026, Review #440); Mehrsaisonprobe.
  4. **U8:** Nationalteam Satz 5 mit der Regel der automatischen Aufstellung (426BCh/42838h, `CupsNational.Lineup.cs`).
- **Dateigrenzen:** siehe Zuständigkeit Runde 11. **Modell:** Claude Opus 5.5. **Status:** geplant.

### Spur `ui-meldungen` – Meldungsfenster, Vereinsfenster, Hauptmenü

- **Teile:**
  1. **E3:** Schließen per Taste/Rechtsklick in `TeamUi.MessageBox` zurückbauen (nur Linksklick wie im Original); Eintrag
     in `anforderungen/zusatzfunktionen.md` Abschnitt C (abschaltbar nach v1.0). **UM-3, Rest:** Knopf „Na gut.“, alter
     Rahmen bei mehr als vier Zeilen.
  2. **UM-2** (nach `re-pruefung`): Vereinsfenster mit Kurve 4D385h (neue Datei; `TeamInfoWindow` bleibt das Länderfenster).
  3. **UM-4** (nach `re-pruefung`): Meldungen der Aufrufer von 0C0B8h als Bausteine; Aufrufer in fremden Dateien im PR
     listen, die Leitstelle schließt sie in der Integration an.
  4. **E4** (nach `re-pruefung`): Hauptmenü angleichen oder Ausnahme in `AGENTS.md` §4 begründet vorschlagen (Leitstelle).
  5. **Pixel M-K4, M-K9, L8:** 4BA58h; 0C5CEh/45D46h; Spielende-Bildschirm 51F3Dh (`SeasonEndGameOverScreen`).
- **Dateigrenzen:** siehe Zuständigkeit Runde 11 und oben. **Modell:** Claude Opus 5.5. **Status:** Teil 1 fertig (PR #483); Teil 5 in Arbeit (Branch `paket/ui-meldungen-5`); Teile 2–4 warten auf `re-pruefung` (#480).

### Spur `pixel-marker` – Welle 2 (nach der Integration der übrigen Spuren)

- **Teile:** 1. **Pixel R-K2…R-K5** (4CCB4h, 35D5Dh, 2A6A2h, 28030h) gegen Video, Marker nachziehen. 2. **Funktionskarte und
  Inventur** auf den Rundenstand (`funktionskarte.py --write`, zweiter Lauf ohne Diff), Stand je Zeile in `analyse/inventur.md`.
  3. **R-K6 (D2):** Nachrichtenspeicher 4B101h–4B270h wie im Original (24 Plätze, Sortierung, Löschen je Spieler); Schema +
  Migration, Mehrsaisonprobe. Marker M-K10 auf „nicht nötig“.
- **Dateigrenzen:** `tools/reverse-engineering/*`, `docs/re-abdeckung.md`, `docs/analyse/inventur.md`, Pixel-Fundstellen in
  den betroffenen Bildschirmen (nur Lage/Farbe), Tests; für Teil 3 `Core/FinanceInbox.cs`. **Modell:** Claude Sonnet 5.5.
  **Status:** geplant.

### Entscheidungen zum Start (Nutzer, 03.11.2026)

- **D1 = A:** Zuschnitt wie oben – sechs Spuren, `re-pruefung` zuerst, Pixel in den zuständigen Spuren, R-K2…R-K5 und
  Funktionskarte in Welle 2 (`pixel-marker`).
- **D2 = C:** R-K6 (24 gemeinsame Nachrichtenplätze 4B101h, Sortierung 4B127h, Löschen 4B270h) wie im Original –
  `pixel-marker` Teil 3, Schema + Migration, Mehrsaisonprobe. M-K10 (Fehlerhalt 25B4Ah, ERROR.HOK) wird **nicht** übernommen
  (Remake: Ausnahmen + AutoSave); Marker in `analyse/inventur.md` auf „nicht nötig“ (Welle 2).
- **D3 = A:** Doppelwechsel per DOSBox – `saisonwechsel` Teil 5: passenden Spielstand mit `hok-patch` erzeugen (nicht
  committen, Pfad und Erzeugung im PR), Ablauf der Aufnahme für den Nutzer im PR beschreiben; die Leitstelle legt den
  Spielstand im Referenz-Repo ab (`original/spielstaende/`), der Nutzer nimmt den Wechsel auf; Auswertung danach.
- **Go** mit den Modellen je Spur wie oben.

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
