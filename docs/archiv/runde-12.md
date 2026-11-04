# Archiv: Runde 12 – Restpunkte Runde 11, Prüfaufträge E2–E5 (03.–04.11.2026)

Aus `docs/arbeitspakete.md` ausgelagert am 04.11.2026. Inhalt unverändert bis auf die Überschrift, den Absatz „Ergebnis“, die nachgeführten Status der Spuren (Review #562, P3-1), die
relativen Links und die Abschnitte „Entscheidungen während der Runde“, „Abschluss“ und „Funktionskarte“ am Ende; der Abschnitt
„Zurückgestellt“ bleibt als Übergang für Runde 13 in `arbeitspakete.md`. Reviews: `docs/reviews/` (Dateien vom 03.–04.11.2026).
Nur bei Bedarf lesen.

**Ergebnis:** Analyse `re-pruefung` (#480, Review #494, Nachprüfung #507) und sechs Umsetzungsspuren, je Spur ein
Integrations-PR: `geld` (#505, Review #506), `wettbewerbe-spiel` (#508, Review #514, Nachprüfung #518), `saisonwechsel` (#519,
Review #520), `kader-transfer` (#526 mit den Integrationsfehlern #524 Doppelwechsel und #525 Kaderprobe, Review #529),
`ui-meldungen` (#535 mit den Anschlüssen #527, Review #536), `pixel-marker` (#545 mit Korrektur #551, Review #546, Nachprüfung
#552). **Gesamtreview durch ChatGPT** (#555, „Nein“: P2 #538) und **Nachprüfung** (#557, alle Rot-Grün-Gegenproben nachgeholt,
Entscheidungsabgleich; neuer P2 #558); beide P2 behoben (#556 Bildschirme ändern beim Öffnen keinen Spielzustand, #559 Spieltag
ohne Zeit beim Öffnen; Nachprüfung #560 „Ja“). Schema 32 → 36. DOSBox: Doppelwechsel FILE15–17 (Referenz-PRs #9, #10),
Vorlage `docs/vorlagen/dosbox-aufnahme.md` (#528), Aufnahme `runde-12` vollständig (FILE18–28, Referenz-PRs #11–#14, Issue #534; Auswertung Runde 13).
Runde 11: [runde-11.md](runde-11.md); ältere Runden in [./](./).


Runde 11 (Umsetzung der Inventur, 02.–03.11.2026) ist abgeschlossen: [archiv/runde-11.md](runde-11.md) (Spuren,
Integrationen, Reviews, Funktionskarte, „Entscheidungen Rundenende“ E1–E6); frühere Runden in [archiv/](./).
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
- **Dateigrenzen:** nur `docs/analyse/re-runde-12.md` und der eigene Statuspunkt. **Modell:** Claude Opus 5.5. **Status:** fertig,
  PR #480; Befunde Review #494 (R12RP-01 bis -06) behoben, Nachprüfung #507 („Ja“), gemergt; Ergebnis und Umsetzung je Punkt in
  `analyse/re-runde-12.md`.

### Spur `geld` – Testlücken, Stadionstärke, Pixel Finanzen

- **Teile:**
  1. **R11G-01 bis R11G-04, R11G-06 (#456):** Bauende-Woche, alle 11 Zeilen der Bauzeittabelle, Jugend-Platzverlauf mit
     festem Zufall als Sollwert, Doku `AnswerConstruction`, Vermerk Tagesanschlüsse. `Fixes #456`.
  2. **`FinanceStadium.Strength`** mit Spielerwerten statt Vereinsfeldern (4D316h, PR #438) – Geldwirkung, Mehrsaisonprobe.
  3. **Pixel G7, G8:** Stadiontabelle 3D45Fh, Finanzübersicht 57CFBh/57ECFh/581C2h gegen Video (`boerse-start`).
- **Dateigrenzen:** `Core/Finance*.cs`, `UI/Screens/Manager/*`, Tests, `docs/finanzen-rekonstruktion.md`. **Modell:** Claude
  Sonnet 5.5. **Status:** Teil 1 fertig (PR #484), Teil 2 fertig (PR #487), Teil 3 fertig (PR #490); integriert #505: G7 rechter Stadionblock nach DOSBox-Bild korrigiert, G8 ohne Abweichung; offen nur die ▲▼-Grafik (`[APPROX]`).

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
- **Dateigrenzen:** siehe Zuständigkeit; Tests (Testdateien unter `tests/**/TestData/`). **Modell:** Claude Opus 5.5. **Status:** Teile 1, 2, 4, 5 fertig (PR #481, #486, #488, #489); Teil 3 (E5, S-K6 = A) fertig (PR #510); Teil 6 (D5 = A: Saisonende-Fenster und Startordnung beim Doppelwechsel) fertig (PR #512). Integriert #519 (Review #520).

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
  `UI/Screens/Team/*` außer `TeamUi.cs`, Tests, `docs/transfer.md`. **Modell:** Claude Sonnet 5.5.
  **Status:** Teil 1 (#485), Teil 2 (#493), Teil 3 (WS-05, #504) fertig; Teil 4 (E2, `LineupScreen.cs`) fertig (PR #509); integriert #526 mit #524/#525 (Review #529); U15-Rest nach D7 = D in Runde 13.

### Spur `wettbewerbe-spiel` – Serien, Belastung, Computervereine, Nationalteam

- **Teile:**
  1. **Serien Pokal/Play-offs:** `CountSeries` (`StatisticsClub.cs`) für DEB-Pokal, Play-offs und Aufstiegsrunde anschließen
     (R11A-01, Reviews #468/#478); **WS-04:** Neuzählung 38A36h, Grenze 5 und Abzug vom Sturm testen.
  2. **A184:** Belastung (Spielerplatz +13h) als eigenes Feld statt `Energy` (Schema + Migration); Mehrsaisonprobe.
  3. **A174:** Computervereine ohne Kaderbezug (Entscheidung Nutzer 02.11.2026, Review #440); Mehrsaisonprobe.
  4. **U8:** Nationalteam Satz 5 mit der Regel der automatischen Aufstellung (426BCh/42838h, `CupsNational.Lineup.cs`).
- **Dateigrenzen:** siehe Zuständigkeit Runde 11. **Modell:** Claude Opus 5.5. **Status:** integriert #508 (Review #514, Nachprüfung #518); Teil 1 fertig zum Review
  (PR #482: Serien im Original nur bei Code 7/8 – kein Anschluss nötig, Probe und Doku; WS-04 getestet). Teil 2 fertig zum
  Review (PR #491: A184 umgesetzt; Hüllkurvenprobe nach D4 = A: passiver Bundesliga-Manager darf nach Schuldenregel bzw.
  „Weniger als neun Spieler!“ ausscheiden, Ursache geprüft); Teil 3 fertig zum Review (PR #495: A174, Computervereine mit der
  Datenbankstärke ohne Aufstellungsbezug); Teil 4 fertig zum Review (PR #503: U8, Auto-Aufstellung des Nationalteams nach
  0x426BC/0x42838). Spur fertig – Integration.

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
- **Dateigrenzen:** siehe Zuständigkeit Runde 11 und oben. **Modell:** Claude Opus 5.5. **Status:** Teil 1 fertig (PR #483); Teil 5 fertig (PR #492, Bausteine M-K4/M-K9 zum Anschluss in der Integration); Teil 2 (UM-2, Vereinsfenster) fertig (PR #511); Teil 3 (UM-4, Bausteine Kurzmeldungen) fertig (PR #513, Anschluss in der Integration); Teil 4 (E4 = B, Hauptmenü als Ereignis `GameState.OnMainMenu`, Schema 33 vorläufig) fertig (PR #515, Anschlüsse in der Integration).

### Spur `pixel-marker` – Welle 2 (nach der Integration der übrigen Spuren)

- **Teile:** 1. **Pixel R-K2…R-K5** (4CCB4h, 35D5Dh, 2A6A2h, 28030h) gegen Video, Marker nachziehen. 2. **Funktionskarte und
  Inventur** auf den Rundenstand (`funktionskarte.py --write`, zweiter Lauf ohne Diff), Stand je Zeile in `analyse/inventur.md`.
  3. **R-K6 (D2):** Nachrichtenspeicher 4B101h–4B270h wie im Original (24 Plätze, Sortierung, Löschen je Spieler); Schema +
  Migration, Mehrsaisonprobe. Marker M-K10 auf „nicht nötig“.
- **Dateigrenzen:** `tools/reverse-engineering/*`, `docs/re-abdeckung.md`, `docs/analyse/inventur.md`, Pixel-Fundstellen in
  den betroffenen Bildschirmen (nur Lage/Farbe), Tests; für Teil 3 `Core/FinanceInbox.cs`. **Modell:** Claude Sonnet 5.5.
  **Status:** Teil 3 (R-K6 + D8 = A) fertig (PR #542, Branch `paket/pixel-marker-3`, Schema 36, Migration
  `SaveGame.Migration.PixelMarker3.cs`); Teil 1 (R-K2…R-K5) fertig (PR #543); Teil 2 (Karte/Inventur, Branch
  `paket/pixel-marker-2`) fertig (PR #544); integriert #545 mit Korrektur #551 (Review #546, Nachprüfung #552). Dateigrenzen von Teil 3 erweitert um `UI/Screens/Manager/InboxScreen.cs` und
  `NewsBar.cs` (D8 = A, #537).

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

### Entscheidungen während der Runde (Nutzer, 03.–04.11.2026)

- **E4 = B:** Hauptmenü-Verhalten wie im Original als Ereignis im Spielkern (`GameState.OnMainMenu`), `AGENTS.md` §4 präzisiert.
- **S-K6 = A:** Satzfolge je Liga wie 0x35EDD (`saisonwechsel` Teil 3).
- **D4 = A:** Ausnahme der Hüllkurvenprobe für den passiven Bundesliga-Manager (A184).
- **D5 = A:** Saisonende-Fenster beim Doppelwechsel wie im Original (`saisonwechsel` Teil 6).
- **D6 = A:** Zusatzlauf `kader-transfer` Teil 4 (E2) über die Fortsetzungsgrenze.
- **D7 = D:** U15-Rest (Start-Liga-Regel, Sonderfall „eine Saison“) nach Runde 13.
- **D8 = A:** „gelesen“ je Nachricht entfällt wie im Original (`pixel-marker` Teil 3, #537).
- **D9 = A:** Gesamtreview durch ChatGPT in Codex mit Netzzugang (erster Versuch #554 enthielt das Review von Runde 11, verworfen).
- **D10 = A:** P2 aus dem Gesamtreview vor dem Abschluss beheben, Nachprüfung holt die fehlenden Prüfungen nach.

### Abschluss (04.11.2026)

Alle Spuren integriert, alle P0–P2 behoben. Offen und in `arbeitspakete.md` unter „Kandidaten Runde 13“: P3-Issues, U15-Rest,
Restlücken `ui-meldungen`, R-K5-Reihenfolge, DOSBox-Auswertung. Erfahrungen der Leitstelle: geplante Läufe melden ihr Ende
nicht – Laufstatus mit `list_triggers` prüfen, nicht nur den PR; Reviews durch ChatGPT brauchen Codex mit Netzzugang und eine
Prüfung von Titel und Head der Review-Datei; Integrationen gestapelt (Spur auf Spur) sparen doppelte Sollwertpflege, zeigen
aber Fehler erst spät (Doppelwechsel #524).

### Funktionskarte (Stand Ende Runde 12)

Aus `docs/re-abdeckung.md` am Abschlussstand (`funktionskarte.py gamedata/em.exe --repo . --write`, zweiter Lauf ohne Diff):
679 Funktionen, 309525 Byte; Spielcode 364 Funktionen (260901 Byte): **umgesetzt 254 (90 %), genähert 3 (1 %), dokumentiert 106
(10 %), unbekannt 1 (0x575C3, bei `livespiel` zurückgestellt)**. Vor Runde 12: 238 (88 %) / 4 / 121 / 1. Zuordnungen von
Fundstellen zu Funktionen: 5097.
