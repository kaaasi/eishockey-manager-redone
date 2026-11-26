# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 26 – Live-Spieltag und Tagesabschluss je Manager (Plan 25.11.2026, Go 25.11.2026)

Runde 25 abgeschlossen am 25.11.2026: [archiv/runde-25.md](archiv/runde-25.md). Stand: Restliste
[analyse/restliste-v1.md](analyse/restliste-v1.md) **V = 0**, D 26, E 319, K 190; `main` `c59f7d5`, Schema 65. Abweichungsliste
[analyse/abweichungen-v1.md](analyse/abweichungen-v1.md) Abschnitt 0: alle Themen außer Live umgesetzt bzw. geklärt.
**Ziel der Runde:** Die Live-Themen S01, S04, S05, S02 der Abweichungsliste (R25-D1 = D, R25-D2 = A) und #1018 (SG-01 = A,
Nutzer 25.11.2026) sind „wie im Original“ umgesetzt oder mit Beleg als „keine Abweichung“ geklärt. **Nicht in dieser Runde:**
Aufräumen #936 (mit #978, #980, #1019, #1024 Rest P3-2), D-Themen (DOSBox nach Lust), Abschlussprüfung. **Danach:** Aufräumen #936
nach [analyse/aufraeumen-v1.md](analyse/aufraeumen-v1.md) (A1–A6), dann Abschlussprüfung.
**Entscheidungen Nutzer 25.11.2026** (Optionen im Plan-PR #1030): R26-D1 = B (`torszene-dauer` Teil 1 nur Beleg parallel,
Umsetzung nach Merge von `live-verschraenkt`), R26-D2 = A (frühere Entscheidungen A449, A453, A541/A542, A442/A547 bleiben),
R26-D3 = B (DOSBox nach Lust, keine eigene Aufnahme `runde-26`: Zusätze in `runde-22` Sitzung 2 und Sitzung 7 Nr. 5,
`vorlagen/dosbox-aufnahme.md`; Auswertung spätestens in der Abschlussprüfung), R26-D4 = B (enges Gesamtreview Live-Spieltag und
Tagesabschluss, ChatGPT GPT-6.2 Astra). **Go Nutzer 25.11.2026** (alle Spuren samt Folgeläufen, Modelle wie unten; Start nach Merge
des Plans).
**Entscheidungen während der Runde:** Nutzer: R26-E1 = B (Stadionschäden 0x3CFA0 als Teil 2 von `tagesabschluss`), R26-E2 = C
(Rest S04 als eigene Spur `live-seiten-rest`). Leitstelle: T7 (Eis des Penaltyschießens, Darstellung 1 und 2) an
`torszene-dauer` Teil 2, Penalty-Bildschirm F076 an `live-verschraenkt`; Pokaltermine verschränkt, Dateigrenzen von
`live-verschraenkt` um `Playoffs.cs`, `Cups*.cs`, `LeaguePairingsScreen.cs` (je minimal) erweitert; `PromotionRoundScreen.cs` und
`EuropeanCupScreen.cs` gehen mit dem Rest an `live-seiten-rest`.

**Regeln wie Runde 25** (Archiv): Base = aktueller `main` beim Start der Spur, tatsächlicher Base-SHA in den Draft-PR; ein Branch
`paket/<spur>`, ein PR, Teile als Commits, höchstens drei Fortsetzungen; Befund reproduzieren, roter Regressionstest mit Fundstelle
`Datei 0x…`, dann grün; Mehrsaisonprobe (4 Wechsel, passiv/aktiv) bei Saisonwechsel, Kader oder Geld; `[ORIGINAL]`/`[APPROX]`;
fremde Tests nach R15-E2 = C, erwartete Werte nach R21-D2 = A; keine geplanten Aufgaben aus Läufen (R15-E4). N2 = B, N6 = A
gelten. **Beleg vor Umbau:** jede Lesart von em.exe als Nachbildung (Skript über die Bytes, `zusammenarbeit.md` §5 Punkt 7) in
`tools/reverse-engineering/live/*.py` bzw. `…/tagesabschluss/*.py`, Ergebnis in `docs/analyse/live-spieltag.md` bzw. im PR. Zeigt
der Beleg **keine** Abweichung: nur Marker mit Beleg nachziehen. **Bestehende Entscheidungen bleiben** (R26-D2 = A):
Enter/Escape (A449, #205), Strahlrücklauf (A453, Plattform), Einstellung `ManagerDayMinuteLoop` mit Remake-Vorgabe (A541/A542,
R14-D4 = A); bitgenaue Zufallsfolge erst mit dem Zufallsmodus nach v1.0 (A442/A547: Verteilung der Würfe je Minute wie im Original,
Folge nicht bitgenau). **Markerliste:** kein `markerliste.py --write` in den Spuren; einziger Generatorlauf in `pruefung-karte`.
**Migration:** keine erwartet (Live-Zustand wird nicht gespeichert, Testlücke #848); braucht eine Spur doch eine, nächste freie
Nummer 66, Spuren mit Migration nacheinander mergen (`prozess/anhang.md` F). **Gemeinsame Dateien:** `GameState.DailyRun.cs`
`live-verschraenkt` und `tagesabschluss` je minimal; `GameState.Manager.cs` nur `tagesabschluss`.

**Folge (R26-D1 = B):** Welle 1 `live-verschraenkt`, `tagesabschluss` und `torszene-dauer` Teil 1 (nur Beleg) parallel;
`torszene-dauer` Teil 2 und `live-seiten-rest` (R26-E2 = C) nach Merge von `live-verschraenkt` (Teil 2 als Fortsetzung auf
demselben Branch, vorher Rebase); Welle 2
`pruefung-karte` nach allen; dann Gesamtreview (R26-D4 = B). Integration in der Reihenfolge der Fertigstellung.

| Spur | Welle | Themen | Modell | Grund |
|---|---|---|---|---|
| `live-verschraenkt` | 1 | S01, S04, S05 | Opus 5.5 | Umbau des Live-Kerns (Minutenschleife 0x37A62 über alle Paarungen), Disassembly; Fehler wirken auf jeden Live-Spieltag |
| `tagesabschluss` | 1 | #1018 (SG-01) | Opus 5.5 | Zufallsreihenfolge im Tageslauf, Geld; klein, aber Spielkern |
| `torszene-dauer` | 1 (Teil 1), 1b (Teil 2) | S02 | Opus 5.5 | Spieltagsablauf 0x36B5D/0x3901B ungelesen, Umbau des Ablaufs der Darstellung 2 |
| `live-seiten-rest` | 1b | Rest S04 (Aufstiegsrunde, Europapokal; R26-E2 = C) | Opus 5.5 | Disassembly 0x31CD5, nach Merge von `live-verschraenkt` |
| `pruefung-karte` | 2 | P3 der Runde, Generatorlauf, Abweichungsliste nachziehen | Sonnet 5.5 | mechanisch |

Erstreviews: Opus 5.5, für `pruefung-karte` Sonnet 5.5. Fixläufe im Modell der Spur. Plan-Review Sonnet 5.5. Gesamtreview eng nach
R26-D4 = B (ChatGPT GPT-6.2 Astra) mit Claude-Auswertung.

### Spur `live-verschraenkt` – Paarungen je Minute verschränkt, Paarungsseiten, Reporter und Tasten (Welle 1)

- **Teile:** 1. **Nachbildung** der Minutenschleife 0x37A62 über alle Paarungen eines Live-Spieltags (Reihenfolge der Paarungen und
  der Würfe je Minute, Eingriffsabfrage je Managerpaarung A454, fertige Paarung A766, Anzeigereihen A554/A555, Tormeldungen
  A909/A954), Skript mit Ausgabe, Vorgabe und erwartete Werte für die roten Tests in `docs/analyse/live-spieltag.md`, kein
  Produktcode; 2. **Umbau S01** (A442, A450, A454, A541, A542, A547, A554, A555, A766, A909, A954): Live-Kern rechnet je Minute
  alle Paarungen des Spieltags in der Reihenfolge des Originals, der Live-Bildschirm zeigt die Reihen gleichzeitig; Ergebnisse,
  Statistik, Strafen, Verletzungen und Finanzen unverändert über den gemeinsamen Zufallsstrom; 3. **S04** (A292, A436, F068, F076:
  „n.Minute“ auf den Paarungsseiten, Zwischenstände Datei 0x3183C, Penalty-Anzeige mit drei Farbstreifen Datei 0x35CBE);
  4. **S05** (A448 Pause bei Geschwindigkeit 62–69, A456 Reihenfolge der Reportermeldungen in der Minute, A457 Farbe 0 des
  Reporterbilds, A475 Mindestbreite Zuschauerzahl, Leertaste = Spielmodus-Optionen Datei 0x2CD1C als offener Teil von A449; Enter/Escape aus A449 bleiben nach #205, A453 nur
  bestätigen).
- **Dateien:** `Core/MatchEngine.Live*.cs`, `LiveClock.cs`, `MatchEngine.Intervention.cs`, `MatchEngine.Scoreboard.cs`,
  `MatchEngine.Shootout.cs`, `MatchEngine.Minutes.cs`, `MatchEngine.MinuteCounters.cs` (je minimal), `GameState.DailyRun.cs`
  (minimal); `UI/Screens/Live/*` außer `GoalScene*`, `UI/LiveWidgets.cs`, `UI/Screens/MatchDayScreen.cs`,
  `Competitions/DebCupGamesScreen.cs`, `PlayoffGamesScreen.cs`, `WorldCupKnockoutScreen.cs` (S04, minimal);
  `tools/reverse-engineering/live/*.py`, `docs/analyse/live-spieltag.md` (neu); Tests (Livespiel-Tests dürfen nach R15-E2 = C
  angepasst werden). **Prüfung:** `--schnell`; Live-Spieltag mit zwei und drei Managerpaarungen (Liga, Play-off, Pokal mit
  Penaltyschießen) als Bildschirmfoto-Ablauf, Vergleich mit Videobild bzw. DOSBox-Bild; Mehrsaisonprobe (4 Wechsel, zwei Manager,
  Live an) – Ergebnisse und Geld plausibel.
- **Status:** Review offen (PR #1032, Claude Code): Teil 1, 2, 2b, 3, 4 fertig; Teil 3b: Ligapaarungen mit Minute und Penalty-Bildschirm F076 fertig. Rest Teil 3b (Aufstiegsrunde und Europapokal im Spielablauf, 0x31CD5, `docs/analyse/live-spieltag.md` §10) ist eigene Spur `live-seiten-rest` (R26-E2 = C). Fixlauf 26.11.2026: Rebase auf `main` 9d9ae39 (nach `tagesabschluss` #1034), erwartete Werte fremder Tests neu begründet (R21-D2 = A).

### Spur `tagesabschluss` – Tagesabschluss im Hauptweg je Manager (#1018, Welle 1)

- **Teile:** 1. Nachbildung des Hauptwegs Datei 0x5522B (`call 0x100:0x4D`, Tagesabschluss 0x3ED01 je Manager in einer Schleife:
  Baufirmen 0x3ED19 mit 11 Würfen, Zins, Werbeangebote) mit Ausgabe, roter Test mit zwei Managern an einem Angebotstag
  (Zufallsfolge je Manager verschränkt); Umbau zu `DayClose(date, manager)` je Manager wie `RunSummerDay`; WM-Schleife Datei
  0x556B5 ff. mitprüfen (gleiches Muster → mitnehmen, sonst Marker). Börsentag bleibt einmal je Tag (Nutzer 30.10.2026, #188).
- **Dateien:** `Core/GameState.Manager.cs`, `GameState.DailyRun.cs` (minimal), `FinanceSponsors.cs`, `FinanceStadium.cs`,
  `FinanceMonthly.cs` (Zinsrechnung `AccrueDailyInterest`), `tools/reverse-engineering/tagesabschluss/*.py` (neu); Tests.
  **Prüfung:** `--schnell`; Mehrsaisonprobe (4 Wechsel, passiv/aktiv, zwei Manager, Geld).
- **Status:** Teil 1 fertig (Lauf 25.11.2026, Claude Code), Review offen. Beleg `tagesabschluss/hauptweg.py`: Hauptweg und WM-Schleife
  je Manager 0x3ED01 → 0x3F833; Talentsuche, Wanderung, 0x07169 und Bau danach. Umgesetzt: `DayClose(date, i)` für alle drei Wege,
  Talentsuche nach der Schleife, Monatsabschluss vor dem Bau, WM-Bau je Manager. Neu gefunden: Stadionschäden 0x3CFA0 fehlen im Remake.
  Teil 2 (R26-E1 = B) fertig (Fortsetzung 1, 25.11.2026): Beleg `tagesabschluss/stadionschaeden.py`, Stadionschäden 0x3CFA0 zu Beginn
  jedes Stadionbaus, Hauptweg-Bau je Manager nach 0x07169; Review offen.

### Spur `torszene-dauer` – Darstellung 2 „Dauer-Eisfeld“ (S02, Welle 1/1b)

- **Teile:** 1. **Beleg** (parallel zu `live-verschraenkt`, kein Produktcode): Spieltagsablauf 0x36B5D und Penaltyschießen 0x3901B
  bei Darstellung 2 lesen und nachbilden (wann das Eisfeld aufgebaut, zwischen Szenen gehalten und abgebaut wird; Moduswechsel
  und Hallenstufe Datei 0x568A9–0x568B6; Wechselwirkung mit den verschränkten Paarungen aus `live-verschraenkt` Teil 1), Ergebnis
  in `docs/analyse/torszene-dauer.md` (eigene Datei, keine Kollision mit `live-spieltag.md`), Skripte
  `tools/reverse-engineering/live/torszene_*.py`; 2. **Umsetzung A472** nach Merge von
  `live-verschraenkt` (Rebase, Fortsetzung): bei Darstellung 2 zeigt der Spieltag durchgehend das Eisfeld wie im Original,
  Torszenen darauf; Darstellung 0/1 unverändert.
- **Dateien:** Teil 1 `docs/analyse/torszene-dauer.md` (neu), `tools/reverse-engineering/live/torszene_*.py`; Teil 2
  `UI/Screens/Live/GoalScene*.cs`, `Core/MatchEngine.GoalScene.cs`, `Formats/GoalSceneRink.cs`, `UI/Screens/Live/LiveSession.cs`
  und `LiveMatchScreen.cs` (je minimal, nach `live-verschraenkt`); Tests. **Prüfung:** Teil 1 Skripte mit
  `~/referenz/original/gamedata/EM.EXE`; Teil 2 `--schnell`, Bildschirmfoto-Ablauf Darstellung 2 über einen Spieltag mit Tor und
  Penaltyschießen, Vergleich mit Videobild.
- **Status:** Teil 1 fertig (PR #1033; Beleg `docs/analyse/torszene-dauer.md`, Abweichung bestätigt, Vorgaben T1–T8). Teil 2
  fertig (Fortsetzung 1, 26.11.2026): Dauer-Eisfeld T1–T6, T8, Penaltyschießen auf eigenem Eis bei Darstellung 1/2 (T7, Leitstelle
  25.11.2026); Umsetzung `docs/analyse/torszene-dauer.md` §11. Erstreview (#1044): R1 (P2) Sperre der Darstellung 2 im
  Live-Spieltag (`GameOptionsScreen.cs` eine Stelle, Leitstelle), R2/R3 (P3) im Fixlauf 26.11.2026 behoben; Delta-Prüfung offen.

### Spur `live-seiten-rest` – Aufstiegsrunde und Europapokal im Spielablauf (R26-E2 = C, Welle 1b)

- **Teile:** 1. Rest von S04 aus `live-verschraenkt` (#1032, Übergabe 26.11.2026): Nachbildung von Datei 0x31CD5 mit den Argumenten
  aus dem Seitenwähler Datei 0x3AB43–0x3ABCE (Wettbewerb 2/3, Gruppe, Merker „im Spiel“ [bp+0xA] = Minute ≠ 0, Terminzahl
  min(DGROUP 0x7AF, 6 bzw. − 6) + (Minute ∈ {0, 60}) − 1; Wirkung bei Datei 0x32294 und 0x32515, Stände aus 0x3183C), Skript wie
  `pokalseiten.py`; dann je eine Ansicht mit Minute in Aufstiegsrunde (Seite 2) und Europapokal (Seite 3) und der Fall in
  `LivePageScreens.Create`; löst PW-04 (`[APPROX]` zu [bp+0xA] in `EuropeanCupScreen`, Stand in `docs/wettbewerbe.md` und
  `PixelWettbewerbeTests.cs` nachziehen).
- **Dateien:** `UI/Screens/Competitions/PromotionRoundScreen.cs`, `EuropeanCupScreen.cs`, `UI/Screens/Live/LivePageScreens.cs`
  (minimal), `tools/reverse-engineering/live/aufstieg_europapokal.py` (neu), Abschnitt in `docs/analyse/live-spieltag.md`,
  `docs/wettbewerbe.md` (PW-04, minimal); Tests (auch `PixelWettbewerbeTests.cs`, PW-04).
  Start nach Merge von #1032, parallel zu `torszene-dauer` Teil 2 (keine gemeinsamen Dateien). **Prüfung:** `--schnell`; Bildschirmfoto Aufstiegsrunde und Europapokal im Spielablauf. **Modell:** Opus 5.5,
  Erstreview Opus 5.5.
- **Status:** Teil 1 fertig (PR #1042, Claude Code): Nachbildung `aufstieg_europapokal.py`, Aufstiegsrunde (beide Gruppen) und
  Europapokal im Spielablauf, PW-04 aufgelöst (Hauptmenü zeigt Termine offener Spiele). Review offen.

### Spur `pruefung-karte` – Abschluss der Runde (Welle 2)

- **Teile:** 1. gesammelte P3 der Runde; 2. einziger Lauf `markerliste.py --write`, Klassenzeilen der geänderten IDs in
  `restliste-v1-klassen.csv`, Status S01, S02, S04, S05 in `abweichungen-v1.md` Abschnitt 0 („umgesetzt“/„keine Abweichung“ mit PR),
  #1018 schließen.
- **Dateien:** `docs/analyse/*` (auch die in Welle 1 neuen `live-spieltag.md`, `torszene-dauer.md`), Kommentare der P3 in den
  Dateien der Spuren dieser Runde. **Prüfung:** `--schnell`. **Erstreview:** Sonnet 5.5.
- **Status:** offen (Go 25.11.2026, Start nach Merge des Plans).

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen

- **DOSBox-Aufnahme `runde-22` Sitzungen 2–6** und **Sitzung 7** (neue D aus Runde 22: A671, A713/A949, A896, A899, A910, A912,
  A918, A945; Liste nach R23-D3 in `vorlagen/dosbox-aufnahme.md`; Zusätze Runde 26 nach R26-D3 = B): Aufnahme nach Lust, Auswertung
  in der Runde danach, spätestens in der Abschlussprüfung.
- **Abschlussprüfung:** 24 X-Funktionszeilen sind im Generator ausgeblendet (#976) – dort bestätigen. P3 #978, #980 im Aufräumen
  (R24-D2 = B).
- **Vor v1.0 (R20-D3-Zusatz):** Abweichungsliste vorgelegt und entschieden (24.11.2026, Runde 24).
- **Letzter Schritt vor v1.0 (Nutzer 17.11.2026, #936):** Aufräumen des gesamten Repos, danach nur noch die Abschlussprüfung.
- **Testlücke Gesamtreview #848:** Speichern/Laden mitten in einer angehaltenen Live-Paarung (über die Oberfläche nicht
  erreichbar; prüfen, sobald ein Speicherweg im Live-Spiel dazukommt).
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
