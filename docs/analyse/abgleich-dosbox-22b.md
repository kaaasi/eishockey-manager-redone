# Auswertung DOSBox-Aufnahme `runde-22` Sitzungen 4, 6, 7 Nr. 4 und A707 (Spur `abgleich-dosbox-22b`, Runde 27)

Grundlage: FILE80/FILE81, FILE89/FILE90, KAPUTT.HOK und die Bilder `file80_*`, `file81_*`, `a899_*`, `file89_*`, `file90_*`,
`kaputt_*`, `a707_*` im Referenz-Repo (kaaasi/eishockey-manager-referenz#41, #42; `original/spielstaende/README.md`
Abschnitte „Runde 22, Sitzungen 4 und 6“ und „Runde 22, Sitzung 7 Nr. 4“), zum Vergleich FILE18–FILE21, FILE48, FILE54 (Modus
„nur Weltkugel“). Ohne Aufnahme: A259 und A707 als Nachbildung über die Bytes von `em.exe`
(`tools/reverse-engineering/abweichungen/emu16.py`). Keine Bilder in diesem Repo; Proben gegen das Remake liefen als nicht
eingecheckte Testläufe bzw. Konsolenprojekte (Aufruf und Ausgabe je Abschnitt). Entscheidung A22-D1 = C: Abweichungen nur als
Issues für Runde 28, kein Produktcode in dieser Spur.

**Skripte** (`tools/reverse-engineering/abgleich/`, Ausgaben ohne Originalbytes): `hokraw.py` (Entschlüsselung wie
`OriginalSaveGame.Read`, Hilfsmodul), `kaderdiff.py` (Kaderfelder zweier Stände je Satz), `a408_tageslauf_modus1.py`,
`a411_pool_satz0.py`, `a899_tor.py`, `a899_zaehler.py`, `a599_mogelguru.py`, `a259_annahme.py`, `a707_grundstaerken.py`.
Aufruf jeweils `python3 -I <skript> [em.exe bzw. Stände]` (Docstring), Standardpfade `~/referenz/original/…`.

## Ergebnis je ID

| ID | Befund | Klasse (Vorschlag) | Teil 2 / Folge |
|---|---|---|---|
| A408 | Original rechnet im Modus 1 für Satz 0 Verträge, Heilung, Training und Aufstellung (0x07169 ohne Modusabfrage); Remake nur den Markt | V (M) | Klassentext; Issue #1084 |
| A411 | Original führt den Kader von Satz 0 im Modus 1 im Pool (Besitzer 0, Plätze 2–3, 22–31, 98–110); Remake lässt die Plätze frei, der Markt kann sie ziehen | V (S) | Klassentext; Issue #1085 |
| A918 | Freundschaftsspiel zählt im Original Spiele in Feld 2 (+0x08/+0x20), Tore in der Liga; Remake Spiele in der Liga – ohne Wirkung | K | Klassentext |
| A899 | Spiel zählt für den Feldspieler im Tor – wie im Remake | X | Klassentext (ohne Marker) |
| A678 (neu) | Rechtsklick im Live-Spiel stellt im Original Verteidiger ins freie Tor und Torhüter in die Abwehr; Remake weist ab | V (M, bisher K) | Klassentext; Issue #1086 |
| A599 | Original hängt nach dem Mogelguru-Bildschirm in einer Endlosschleife ohne Eingabe; Remake kehrt zum Laden-Dialog zurück | E (Entscheidung Nutzer) | Klassentext; A786 ergänzen |
| A259 | Original prüft beim Annehmen nur Besitz und Gebotsbit; „Bieterkader voll“ gibt es dort nicht | K | Klassentext; Sitzung 3 Nr. 3 streichen |
| A707 | 0x4981E liest nur Vereinsfeld 0x19/0x1B; Remake gleich, Länderwerte der Aufstellung gehen über `NationalRatings` ein | X | Klassentext; Sitzung 4 Nr. 2 streichen, keine Aufnahme nötig |

**Produktcode-Marker:** Die Marker von A259 (`Transfer.cs`), A707 (`Transfer.Pool.cs`) und A918 (`Player.Competitions.cs`) bleiben
in dieser Spur unverändert (Dateigrenze nur `docs/**`, Aufräumen verhaltensneutral); Wortlaute für Runde 28 stehen in den
Abschnitten. Hinweis an die Leitstelle: `docs/formate/spielstand-original.md` „Offene Punkte“ (Ablauf nach dem
Mogelguru-Bildschirm) ist mit A599 beantwortet, gehört aber nicht zu dieser Spur.

## A408, A411, A918 – Modus „nur Weltkugel“, FILE80/FILE81 (Sitzung 6)

**Stand der Aufnahme:** FILE80 und FILE81 sind ein Spiel im Modus 1 („nur Weltkugel“, DGROUP 0xD0B9 = 1), ein Manager,
Bundestrainer von DEUTSCHLAND (`06AC` = 54), 14.04.1994, Tageszähler 217 in **beiden** Ständen. Zwischen den Ständen liegt nur
das Freundschaftsspiel DEUTSCHLAND – ITALIEN 5:0 (`D0C1` 0 → 1, Heim `D0DB` = 54, Gast `D0D3` = 62, Tore `D0CB` = 5, `D0C3` = 0);
es vergeht kein Tag. Werkzeuge: `tools/reverse-engineering/abgleich/hokraw.py` (Entschlüsselung wie `OriginalSaveGame.Read`,
Blockfolge aus `OriginalSaveGame.Layout`), `kaderdiff.py`, `a408_tageslauf_modus1.py`, `a411_pool_satz0.py`.

| ID | Befund | Klasse (Vorschlag) | Teil 2 |
|---|---|---|---|
| A408 | Original rechnet im Modus 1 für Satz 0 Verträge, Heilung, Training und Aufstellung (0x07169 ohne Modusabfrage); Remake nur den Markt | V | Issue (M), Klassentext |
| A411 | Original führt den Kader von Satz 0 im Modus 1 im Pool (Besitzer 0, Plätze 2–3, 22–31, 98–110); Remake lässt die Plätze frei, der Markt kann sie ziehen | V | Issue (S), Klassentext |
| A918 | Freundschaftsspiel zählt im Original Spiele im Feld 2 (+0x08/+0x20), Tore in der Liga (+0x03/+0x22); Remake Spiele in der Liga – ohne Wirkung | K | Klassen- und Markertext |

### A408 – Tageslauf des Kaders von Satz 0 im Modus „nur Weltkugel“

- **FILE80/FILE81 decken A408 nicht ab:** gleicher Tag (Tageszähler 217), der Tageslauf 0x07169 lief dazwischen nicht. Satz 0
  ändert sich nur in `+0x1B` (alle 25 Plätze 0 → 90, Feld offen; dasselbe Muster steht in allen 150 Vereinssätzen `far:9657`
  `+0x1E`…`+0x25` → 90): `python3 tools/reverse-engineering/abgleich/kaderdiff.py FILE80 FILE81 0`. Die Angabe des Nutzers
  („keine Verträge, Training nur über ein Freundschaftsspiel“) betrifft den sichtbaren Nationalkader (Satz 5), nicht Satz 0: Satz 0
  ist im Modus 1 unsichtbar, führt aber Verträge (alle 25 `+0x0B` = 2) und ist Quelle von Nationalspielern (FILE80 Satz 5 Plätze
  2, 11, 12, 21–23 = Pool 3, 28, 29, 106, 109, 110 aus Satz 0).
- **Nachbildung über die Bytes von em.exe:** `python3 tools/reverse-engineering/abgleich/a408_tageslauf_modus1.py` lädt den
  ganzen Stand FILE19 (Modus 1, Tag 217) in DGROUP und die Fernblöcke, setzt DGROUP 0xA70E = 0 (Schritt (c) der WM-Schleife) und
  führt 0x07169 aus (echter Code 0x07169–0x07C12 und Platzzähler 0x4BE17, alles andere als Stub protokolliert), einmal mit Modus 1,
  einmal mit erzwungenem Modus 0. Ausgabe: in **beiden** Läufen dieselben 683 Aufrufe, darunter je Platz Aufrufe aus dem Vertragsteil
  Datei 0x073E3–0x07AEF (0x05B1A, 0x4B2BA, 0x49D36 je 25×), die Heilung 0x07C12 (25×, Datei 0x07B4E), das Training 0x0659C
  (25×, Datei 0x07B5E) und 0x426BC (Datei 0x07548 im Vertragsteil, Datei 0x07C07 Aufstellung); „Aufruffolge gleich: True“. 0x07169 liest DGROUP 0xD0B9 nicht.
- **Gegenprobe in den Ständen:** `kaderdiff.py FILE18 FILE48 0` (Tag 217 → 219, eine Ausführung laut Markerbeleg): Platz 17
  neu verletzt (`+0x09` 0 → 2, `+0x0D` 0 → 2, `+0x17` 0 → 18, `+0x0A` 1 → 0), dafür Platz 12 eingesetzt (`+0x0A` 0 → 1) und
  Platz 16 in Reihe 3 Sturm (`+0x15` 0 → 131), Belastung `+0x13` bei 13 Plätzen −1, Kondition `+0x10`/Technik `+0x11` je ein Platz −1.
  `kaderdiff.py FILE19 FILE20 0` (Tag 217 → 231): zwei neue Verletzte (Plätze 21, 24), vier Aufstellungswechsel (`+0x0A`/`+0x15`),
  `+0x10` bei 7 Plätzen −1, `+0x13` bei 13 Plätzen −2. Die Begründung des Markers („Kader von Satz 0 bis auf Form und Belastung
  unverändert“, #732) trifft also nicht zu: Verletzung, Heilung, Training und Aufstellungsautomatik laufen.
- **Remake:** `CupsNational.WorldCupLoop.cs` `WorldCupLoopDailyRun` endet bei `WorldOnly` nach `DailyRunMarket`
  (`if (WorldOnly) return;`), also ohne `AnnounceContractEnds` und `ManagerSquadRun`.
- **Vorschlag V** (Aufwand M): Schritt (c) im Modus 1 wie in den anderen Modi (Bedingung 0xA70E = 0 bleibt, A498). Zu prüfen bei der
  Umsetzung: Trainingskosten und Vertragsmeldungen des unsichtbaren Satzes 0 (Konto, Posteingang im Modus 1). Issue unten.
- **Teil 2, Klassentext (Beleg):** „FILE18 → FILE48 und FILE19 → FILE20: Satz 0 mit neuen Verletzten, Aufstellungswechseln,
  Kondition/Belastung (kaderdiff.py); Nachbildung a408_tageslauf_modus1.py: 0x07169 ruft im Modus 1 wie im Modus 0 Vertragsteil,
  Heilung 0x07C12, Training 0x0659C und Aufstellung 0x426BC für alle 25 Plätze; FILE80/81 ohne Tageswechsel (abgleich-dosbox-22b)“.
  Markertext erst mit der Umsetzung (Runde 28) ändern.

**Issue #1084 (Label `befund`, Runde 28):**

> **Titel:** A408: Modus „nur Weltkugel“ – Tageslauf von Satz 0 ohne Verträge, Heilung, Training und Aufstellung
>
> Aus `docs/analyse/abgleich-dosbox-22b.md` (Spur `abgleich-dosbox-22b`, A22-D1 = C). Im Modus 1 rechnet das Remake in Schritt (c)
> der WM-Schleife nur den Markt (`CupsNational.WorldCupLoop.cs`, `WorldCupLoopDailyRun`: `if (WorldOnly) return;`). Das Original
> führt 0x07169 ohne Modusabfrage aus. Beleg: `python3 tools/reverse-engineering/abgleich/a408_tageslauf_modus1.py` (FILE19,
> Modus 1 und erzwungen 0: gleiche 683 Aufrufe, je Platz Vertragsteil, Heilung 0x07C12, Training 0x0659C, Aufstellung 0x426BC);
> `kaderdiff.py FILE18 FILE48 0` und `kaderdiff.py FILE19 FILE20 0` (Satz 0: neue Verletzte, Aufstellungswechsel, Kondition und
> Belastung). Erwartung: Schritt (c) im Modus 1 wie sonst (Bedingung 0xA70E = 0 nach A498 unverändert); Trainingskosten und
> Vertragsmeldungen des Satzes 0 im Modus 1 dabei gegen das Original prüfen. Roter Test: Stand FILE18 importieren, Tageslauf bis
> Tag 219 → Satz 0 mit Heilung/Training/Aufstellung. Aufwand M.

### A411 – Kader von Satz 0 im Pool (Modus „nur Weltkugel“)

- **Original:** `python3 tools/reverse-engineering/abgleich/a411_pool_satz0.py` – in allen vier Ständen des Modus 1 (FILE18,
  FILE19, FILE54, FILE80) verweisen die 25 Plätze von Satz 0 auf Pool 2–3, 22–31, 98–110, alle mit Besitzer 0; Markt 6–7
  Plätze, davon in FILE18/19/80 je zwei im WM-Kader (FILE80: MERK, KUHNKE, Besitzer 4). Das Original setzt den Kader also wie in
  den anderen Modi in den Pool (Plätze wie FILE01, Startkader Datei 0x5B0BB–0x5B2BE); die offene Frage des Markers ist damit
  beantwortet. Bild `file80_3` (WM-Kader mit Vereinsspielern) passt dazu, wurde aber nicht pixelweise ausgewertet.
- **Remake:** `Squad.Setup.cs` (Zweig `game.WorldOnly`: Kader aus eigener Quelle, „kein Poolplatz der Manager“) und
  `Transfer.cs` `Initialize` (`if (!game.WorldOnly) SeatManagerSquads(game)`) lassen die Plätze frei. Probe (Harness nicht
  eingecheckt, `NewGame.Create` wie `SquadLossLoanReturnTests.WorldOnly_NewGame_Export_SquadOfRecord0OnFile01Slots`, Saaten 1–8):
  die 25 Plätze sind `Free` (Besitzer 6), bei 7 von 8 Saaten steht danach mindestens einer auf dem Markt (Saat 7: zwei). Der Export
  gleicht das über `AddUnseatedSquadPlayers` aus (Test #780), außer wenn ein Platz auf dem Markt liegt (A848).
- **Wirkung:** Im Remake kann der Listenwechsel im Modus 1 aus 25 Plätzen ziehen, die das Original nie auf den Markt gibt; Marktspieler
  sind im Modus 1 Kandidaten des WM-Kaders (FILE80). Die Abweichung ist also nicht nur ein Exportdetail.
- **Vorschlag V** (Aufwand S): Startkader auch im Modus 1 in den Pool setzen (`SeatManagerSquads` ohne Ausnahme, Pool mit
  Managerplätzen); danach entfällt der Ersatzweg `AddUnseatedSquadPlayers` samt Restlücke A848. Issue unten.
- **Teil 2, Klassentext (Beleg):** „FILE18/19/54/80 (a411_pool_satz0.py): Satz 0 auf Pool 2–3, 22–31, 98–110, Besitzer 0; Remake
  setzt im Modus 1 nicht in den Pool (Squad.Setup.cs, Transfer.cs Initialize), Plätze frei und marktfähig (Probe Saaten 1–8,
  abgleich-dosbox-22b)“. A848 (E) bleibt bis zur Umsetzung; mit ihr entfällt der Marker.

**Issue #1085 (Label `befund`, Runde 28):**

> **Titel:** A411: Modus „nur Weltkugel“ – Startkader von Satz 0 nicht im Pool
>
> Aus `docs/analyse/abgleich-dosbox-22b.md`. Das Original führt den Kader von Satz 0 auch im Modus 1 im Pool: FILE18, FILE19,
> FILE54, FILE80 alle 25 Plätze auf Pool 2–3, 22–31, 98–110, Besitzer 0 (`python3 tools/reverse-engineering/abgleich/a411_pool_satz0.py`).
> Das Remake lässt die Plätze frei (`Squad.Setup.cs` Zweig `WorldOnly`, `Transfer.cs` `Initialize`: `if (!game.WorldOnly)
> SeatManagerSquads`); bei 7 von 8 Saaten zieht der Listenwechsel einen davon auf den Markt, aus dem der WM-Kader nominiert werden
> kann. Erwartung: Startkader im Modus 1 wie sonst in den Pool; Export ohne `AddUnseatedSquadPlayers` (A848 entfällt), Test #780
> bleibt grün. Roter Test: neues Spiel Modus 1 → Poolplätze 2–3, 22–31, 98–110 Besitzer Manager 0. Aufwand S.

### A918 – Zählfeld des Freundschaftsspiels

- **Original:** `python3 tools/reverse-engineering/abgleich/kaderdiff.py FILE80 FILE81 5` – die 16 Gespielten (Satz 5 Plätze 0,
  3–8, 13–20, 25) bekommen `+0x08` +1 und `+0x20` +1 (Spiele Feld 2, wie die WM nach A628), `+0x1A` = 128 (gespielt, A677); die
  Tore stehen in `+0x03`/`+0x22` (Liga), Strafen in `+0x00`/`+0x01`; `+0x06`/`+0x1C` bleiben 0. Dazu passt der Aufruf
  0x30365 → 0x36B5D mit Wettbewerb 8 (Datei 0x3039C `mov al,0x8`, weiter an 0x544C0, Datei 0x36F6C–0x36F9C).
- **Remake:** `MatchEngine.Live.cs` `Credit` → `CreditLineup(…, league: false)` mit Standardfeld `CompetitionSlot.League`,
  `MatchEngine.LiveCredit.cs` `NationalSlot` nur für die WM Feld 2. Probe (Harness nicht eingecheckt): FILE80 importieren,
  `PlayFriendly(62, home: true)` → 16 Spieler `Games` +1, `EuropeGames` 0 (dieselben 16 Plätze wie im Original).
- **Wirkung:** keine. Angezeigt wird nur die Summe (`NationalLineupScreen` Spalte SP = `Games`); `EuropeGames` liest nur der Export,
  und der Export schreibt im Satz 5 keine Zähler (Probe: nach dem Freundschaftsspiel `+0x06`, `+0x08`, `+0x1C`, `+0x20` von Satz 5
  im Export alle 0, `OriginalExport.Cups.cs` schreibt nur `+0x0A`, `+0x0F`–`+0x13`, `+0x15`).
- **Vorschlag K.** **Teil 2, Klassentext:** „Original FILE80 → FILE81: Spiele Feld 2 (+0x08/+0x20), Tore Liga (+0x03/+0x22), wie
  die WM (A628); Remake zählt Spiele in der Liga, ohne Wirkung (nur Summe sichtbar, Export ohne Zähler in Satz 5;
  abgleich-dosbox-22b)“. Markertext dabei auf den Befund umstellen (Wortlaut ändert die ID – im Generatorlauf von Teil 2 einplanen)
  oder den Code angleichen (`NationalSlot` → Feld 2 auch für `Friendly`), dann X.

**Weitere Beobachtungen ohne ID (FILE80 → FILE81):**

- Wertebonus des Freundschaftsspiels wie `NationalCoach.ApplyBonus` mit Bonus 1, x ≤ 2: alle 29 Plätze `+0x10` +1, `+0x11` ±0,
  `+0x12` +1, bei `+0x10` = 99 (Plätze 13, 16) kein Zuwachs; Belastung `+0x13` aller 29 Plätze = 70 (`FriendlyEnergy`). Keine
  Vertragsfelder im Satz 5 geändert (Angabe Nutzer „keine Verträge“).
- Export Satz 5 ohne Spiel-, Tor- und Strafzähler (siehe A918): Verdacht P3, eigenes Issue nur, wenn der Export bitgenau werden soll.
- `+0x1B` (Kaderplatz) und Vereinssatz `+0x1E`…`+0x25` werden beim Freundschaftsspiel für alle nicht beteiligten auf 90, für die
  Gespielten auf 46–76 gesetzt – Feld offen, nicht ausgewertet.

## A899 – Feldspieler im Tor (FILE89/FILE90, Sitzung 7 Nr. 4; Bezug #939)

**Stand der Aufnahme:** KÖLNER EC (Manager 0 „Joerg“), 1. Saison. FILE89 am 22.09.1993 nach dem 4. Spieltag (Reihe 1 ohne
Torhüter, Paarung 20.9. EV LANDSHUT – KÖLNER EC 23:0, Bild `file89_3`), FILE90 am 24.09.1993 nach dem nächsten Spiel – einem
**Pokalspiel** (alle Aufgestellten zählen Pokal +0x07 und Verein Pokal +0x1E, nicht Liga +0x06). Vor dem Spiel hat der Nutzer
HEISS ausgesetzt (+0x0A 1 → 0, also kein Torhüter mehr eingesetzt) und im Spiel MAYR (Platz 4, Verteidiger, Rollenwert 35) per
Rechtsklick ins Tor gestellt („TW 4“, Bild `file90_3`).

**Ergebnis je Teilfrage:**

| Teilfrage | Original (Beleg) | Remake | Gleich? |
|---|---|---|---|
| Linksklick Feldspieler → Torreihe (vor und im Spiel) | „Der darf da nicht rein !“ (`a899_tor.py`, Datei 0x40E63–0x40EB9, Texte DGROUP 0x1229 → 0x3FDD/0x3FE9; Bilder `a899_3/4`, `file89_2`) | `LineupResult.WrongRole`, dieselbe Meldung (`LineupScreen.cs:275`) | ja |
| Rechtsklick vor dem Spiel | Ein-/Aussetzen (+0x0A = 0), keine Art-Prüfung | `Undressed` | ja |
| Spiel ohne Torhüter | spielbar, 0:23 (FILE89, Bild `file89_3`) | Spielrechnung ohne Torhüter vorhanden (`MatchEngine.GoalScene.cs:115`, `MatchEngine.Managed.cs:212`) | qualitativ ja (Ergebnis nicht nachgerechnet) |
| **Rechtsklick im Spiel**, Verteidiger → Torreihe, Torzelle frei | **aufgestellt** in Torzelle (Reihe 4, Zelle 4), Kennwert an 0x41533 | `WrongRole` + „Der darf da nicht rein !“ (`Squad.Lineup.InMatch.cs` Rollenprüfung vor der Platzsuche) | **nein** |
| Rechtsklick im Spiel, Verteidiger, Torzelle belegt | „Kein Platz mehr !“ | `WrongRole` | nein |
| Rechtsklick im Spiel, Stürmer → Torreihe | nichts (still abgewiesen, Datei 0x412B6–0x412C2) | `WrongRole` + Meldung | nein (nur Meldung) |
| Rechtsklick im Spiel, Torhüter → Abwehr Reihe 1 | **aufgestellt** (Reihe 0, Zelle 3) | `WrongRole` | **nein** |
| Spiel zählt für den Feldspieler im Tor | ja: MAYR Pokal 0 → 1, Verein Pokal 0 → 1, +0x15 0 → 1, „hat gespielt“ +0x1A Bit 7 0 → 1 (`a899_zaehler.py`, FILE89 → FILE90) | zählt jeden Aufgestellten mit +0x15 ≠ 0, auch den sechsten (`MatchEngine.Managed.cs:111`, `CountPlacedInMatch`); Import FILE90: MAYR `CupGames` 1 | ja (soweit im Remake erreichbar) |
| Nach dem Spiel | +0x15 = 1 bleibt; Bild `file90_4`: Torplatz leer, MAYR als Abwehr Reihe 1 (Reihe 1 hat damit drei Verteidiger mit +0x15 = 1) | Import FILE90: kein Torhüter, MAYR und SCHIFFL Abwehr Reihe 1, MENDE sechster Feldspieler | ja |

**Lesart der Rechtsklick-Abweichung (Nachbildung):** Die Art-Prüfung mit der Meldung „Der darf da nicht rein !“ läuft nur bei
Mausereignis DGROUP 0x5A16 = 1 (linke Taste, Datei 0x40E63). Im Spiel (Kennwert `[bp+0x6]` ≠ 0) geht die rechte Taste (Ereignis 3)
direkt in den Aufstellweg 0x41273 (Datei 0x40F35–0x40F42). Dort entscheidet nur die Rollengruppe aus 0x04180 (Pool +0x18 / 25,
höchstens 2): Torhüter und Verteidiger suchen ab Zelle 3 (Abwehr) bzw. in der Torreihe nur Zelle 4 (`[bp-0x2A]`), Stürmer werden in
der Torreihe still abgewiesen (Datei 0x412A1–0x412C2). Die Bedingung des Nutzers „kein Torwart und kein Reservetorwart“ ist in
diesem Weg nur die freie Torzelle; ob ein eingesetzter Ersatztorhüter auf der Bank anderswo (Spielbeginn, Live-Ablauf) nachrückt,
ist nicht gelesen. Welche Werte die Spielrechnung für einen Verteidiger in der Torzelle (+0x15 = 1) nimmt, ist ebenfalls nicht
nachgebildet; die Neuzählung 0x38A36 zählt +0x15 = 1 eines Feldspielers als dritten Verteidiger der Reihe 1
(`Squad.Lineup.InMatch.cs`, #939), das spräche für „sechs Feldspieler ohne Torhüter“ – Verdacht, kein Beleg.

**Beleg-Befehle:**

```
python3 tools/reverse-engineering/abgleich/a899_tor.py ~/referenz/original/gamedata/em.exe
python3 tools/reverse-engineering/abgleich/a899_zaehler.py ~/referenz/original/spielstaende/FILE89.HOK ~/referenz/original/spielstaende/FILE90.HOK
dotnet run --project src/EishockeyManager.Tools -- hok import ~/referenz/original/spielstaende/FILE90.HOK <scratch>/f90.emsave --gamedata gamedata --seed 1
```

Ausgabe `a899_tor.py` (Auszug): „vor dem Spiel links Verteidiger Tor leer: Meldung „Der darf da nicht rein !““, „im Spiel rechts
Verteidiger Tor leer: aufgestellt: Reihe 4 Zelle 4 (Kennwert 1)“, „im Spiel rechts Stürmer Tor leer: nichts“, „im Spiel rechts
Torhüter Reihe 1 (Abwehr): aufgestellt: Reihe 0 Zelle 3“. Ausgabe `a899_zaehler.py`: „Platz 4 MAYR Rolle 35: Pokal 0→1, Verein
Pokal 0→1, +0x15 0→1, Bit7 0→1“, die 16 übrigen Aufgestellten nur Pokal/Verein Pokal 0→1, HEISS nur +0x0A 1→0. Gegenprobe Remake:
Probe (nicht eingecheckt) über `Club.ClickPlayer`/`ClickPlayerInMatch` mit leerem Torplatz: links vor/im Spiel `WrongRole`,
rechts vor dem Spiel `Undressed`, **rechts im Spiel Verteidiger und Stürmer `WrongRole`**, Torhüter → Reihe 1 `WrongRole`.

**Vorschlag:**

- **A899 → X** (Klasse ohne Marker, entfallen seit Runde 23): Das Spiel zählt für den Feldspieler im Tor, wie
  `MatchEngine.Managed.cs:111` (`[ORIGINAL]` Datei 0x54918–0x54927) es tut. Teil 2: Zeile in `restliste-v1-klassen.csv` auf X,
  Abweichung „keine“, Beleg „FILE89 → FILE90 (Runde 22 S7 Nr. 4): MAYR im Tor Pokal 0→1, +0x1A Bit 7 (`abgleich/a899_zaehler.py`);
  Import FILE90 `CupGames` 1“, Verdacht leer. Der alte Text („das Remake zählt ihn nicht“) ist überholt.
- **A678 → V** (bisher K „Rechtsklick ohne Reihe im Spiel“): Der Markertext in `Squad.Lineup.InMatch.cs:29` behauptet „Art-Prüfung
  und Plätze wie beim Linksklick“; die Nachbildung widerlegt das für die rechte Taste im Spiel. Teil 2: Klasse V, Abweichung
  „Im Spiel stellt der Rechtsklick Verteidiger ins freie Tor und Torhüter in die Abwehr; das Remake weist mit „Der darf da nicht
  rein !“ ab“, Beleg „`abgleich/a899_tor.py`; FILE90 (MAYR „TW 4“, Bild `file90_3`)“, Verdacht „Spielrechnung mit Verteidiger in
  der Torzelle nicht gelesen (0x38A36)“, Aufwand M. Issue unten (Runde 28). Derselbe Code gilt für das Nationalteam
  (`CupsNational.Lineup.InMatch.cs:56`, gleiche Funktion 0x40364).
- Der Teil „ohne gewählte Reihe meldet der Rechtsklick nichts“ von A678 ist durch die Nachbildung bestätigt (Datei 0x4127E–0x41284).

**Issue #1086 (Label `befund`, Runde 28):**

> **Titel:** A678/A899: Rechtsklick im Live-Spiel stellt Feldspieler ins Tor und Torhüter in die Abwehr (Original), Remake weist ab
>
> Aus `docs/analyse/abgleich-dosbox-22b.md` (Spur `abgleich-dosbox-22b`, Teil 1). **Original:** Im Spiel (Kennwert DGROUP 0x2D6 +
> Manager ≠ 0) überspringt die rechte Maustaste (DGROUP 0x5A16 = 3) die Art-Prüfung „Der darf da / nicht rein !“ (Datei
> 0x40E63–0x40EB9, nur Ereignis 1) und geht in den Aufstellweg 0x41273 (Datei 0x40F35–0x40F42). Dort gilt nur die Rollengruppe
> 0x04180: Verteidiger in der Torreihe → Torzelle `[bp-0x2A]`, falls frei (sonst „Kein Platz mehr !“); Torhüter in einer
> Abwehrreihe → Abwehrzellen; Stürmer in der Torreihe → still nichts (Datei 0x412A1–0x412C2). Der Aufgestellte bekommt +0x15 = 1,
> „hat gespielt“ und sofort ein Spiel im laufenden Wettbewerb (0x41533, Datei 0x415B7–0x41622). **Beleg:**
> `python3 tools/reverse-engineering/abgleich/a899_tor.py ~/referenz/original/gamedata/em.exe` („im Spiel rechts Verteidiger Tor leer:
> aufgestellt: Reihe 4 Zelle 4 (Kennwert 1)“); DOSBox Runde 22 S7 Nr. 4: FILE89 → FILE90, MAYR per Rechtsklick „TW 4“ (Bild
> `file90_3`), `a899_zaehler.py`: MAYR Pokal 0→1, +0x15 0→1. **Remake:** `Club.ClickPlayerInMatch` (`Squad.Lineup.InMatch.cs`) und
> `NationalCoach.ClickPlayerInMatch` (`CupsNational.Lineup.InMatch.cs:56`) prüfen die Rolle vor der Platzsuche für beide Tasten →
> `LineupResult.WrongRole` („Der darf da nicht rein !“). **Offen vor dem Umbau:** Spielrechnung mit einem Feldspieler in der Torzelle
> (Neuzählung 0x38A36 zählt +0x15 = 1 als Verteidiger der Reihe 1; Torhüterwerte?) und Torhüter in der Abwehr nachbilden; Anzeige
> „TW n“ im Gitter. **Abnahme:** roter Test `ClickPlayerInMatch(verteidiger, Torreihe, right: true)` → Torzelle; Stürmer → `Unchanged`
> ohne Meldung; Torhüter → Abwehr Reihe 1. Marker A678 auf `[ORIGINAL]` mit Fundstelle.

## A599 – nach „Der kleine Mogelguru“ (KAPUTT.HOK, Sitzungen 4 und 6)

**Aufnahme:** Bilder `kaputt_1_mogelguru_ohne_rand`, `kaputt_2_mogelguru_roter_rand`; Nutzer: um den oberen Text blinkt der Rand
etwa im Sekundentakt, weder Mausklick noch Leertaste, Enter oder Esc führen weiter, der Bildschirm bleibt stehen.

**Original (Nachbildung):** Der Lader 0x5C3E3 (Ov 0x140) setzt bei falscher Prüfsumme den Fehlercode `[bp-0x1]` = 5 (Datei
0x5CBD8–0x5CBE1, 0x5CDAF–0x5CDB8). Ab Datei 0x5CEFA zeichnet er bei Code ≠ 0 den Mogelguru-Bildschirm und läuft dann in die
Schleife Datei 0x5CFFE–0x5D03A: Farbe `[bp-0x2]` XOR 0x12 (Start 0, Datei 0x5C3F0), drei Rahmen 0x0B5A7 (0,0)–(319,53),
(1,1)–(318,52), (2,2)–(317,51), Pause 0x130EE(20) = 6 BIOS-Takte (≈ 0,33 s), `jmp short` zurück. Kein Aufruf liest Maus oder
Tastatur, kein Zweig verlässt die Schleife; der normale Ausgang Datei 0x5D03C (Merker löschen, Rückgabe 1) wird nur bei Code 0
erreicht. Das Original **hängt also endlos**; der Rand ist ein 3 Pixel breiter Rahmen um die oberen 54 Zeilen und wechselt alle
6 Takte zwischen Farbe 0x12 (`Colors.Red`) und 0 (Periode ≈ 0,66 s – „etwa im Sekundentakt“).

```
python3 tools/reverse-engineering/abgleich/a599_mogelguru.py ~/referenz/original/gamedata/em.exe 4
```

Ausgabe: je Durchlauf „Rahmen (0,0)–(319,53) Farbe 0x12“ … „Pause 0x130EE(20) = 6 BIOS-Takte“, dann Farbe 0x00; „nach der ersten
Pause erreicht: Ausgang Datei 0x5d03c nein; Adressen außerhalb der Schleife 0x5cffe–0x5d03c: 0“; „Schleifenende Datei 0x5D03A:
Befehl 0xeb 0xc2 (eb = jmp short), Ziel Datei 0x5cffe“.

**Remake:** `OriginalImport.TryImport` lädt nichts (`OriginalImport.Access.cs:25`, A599), `MogelguruScreen` zeichnet einen festen
roten Winkel (VLine x = 2, HLine y = 50) und kehrt bei Klick oder Taste in den Laden-Dialog zurück (Test
`HokImportAccessTests.ChecksumError_ShowsMogelguru_AndKeepsRunningGame`).

**Vorschlag:**

- **A599 → E** (bisher D): Was das Original danach tut, ist jetzt belegt – nichts, es bleibt in einer Endlosschleife (Neustart
  nötig, ungesicherter Stand verloren). Das Remake hängt bewusst nicht und behält den laufenden Stand; ein Nachbau des Hängers
  hätte keinen Nutzen. **E setzt eine Entscheidung des Nutzers voraus** – bis dahin in Teil 2 als E mit Vermerk „Entscheidung
  Nutzer offen (Teil 1 #1083)“ führen; Alternative V (Hänger nachbauen) wäre wörtlich originaltreu. Teil 2: Beleg „Datei
  0x5CFFE–0x5D03A Endlosschleife ohne Eingabe (`abgleich/a599_mogelguru.py`); Nutzer Runde 22 S4/S6“, Abweichung „Original hängt
  nach dem Bildschirm; Remake kehrt zum Laden-Dialog zurück“.
- **A786 (K, Bildschirm) ergänzen:** Rand im Original ein blinkender Rahmen (0,0)–(319,53), 3 Pixel, Farbe 0x12 ↔ 0 alle 6 Takte;
  das Remake zeichnet einen festen Winkel. Bleibt K (kosmetisch); Beleg um `a599_mogelguru.py` und `kaputt_1/2` ergänzen,
  „Rückkehr zum Laden-Dialog angenommen“ ersetzen durch „Rückkehr statt Endlosschleife (A599)“.
- `docs/formate/spielstand-original.md` „Offene Punkte“ (Ablauf nach dem Mogelguru-Bildschirm) ist damit beantwortet – Datei
  gehört nicht zu dieser Spur; Hinweis an die Leitstelle.

## A259 – Annahme eines nicht mehr ausführbaren Gebots

**Zweig:** Die Plan-Adresse 0x46E04–0x46FCF ist der Klick auf einen eigenen Kaderplatz mit **unaufgefordertem** Angebot
(+0x09 Bit 0x40, `Transfer.Unsolicited.cs`). Der Marker A259 sitzt aber in `TransferMarket.Accept` (`Transfer.cs:642`), also
beim Gebot auf einen **eigenen Markt-Platz** (Satz 4, DGROUP 0x7623 + 0x34 · Platz, +0x09 Bit 0x80). Das ist Datei
0x4710E–0x474E8 im selben Klickhandler. Ausgewertet sind beide Zweige.

**Befund (Nachbildung über die Bytes von em.exe):**

- Markt-Zweig: Vor dem Angebotsfenster prüft das Original nur zwei Dinge. Der Pool-Satz gehört noch dem Manager (Pool +0x1A
  = Manager, Datei 0x47324–0x4732E; die Vorauswahl 0x47140–0x471AB lenkt nur dorthin). Dazu kommt das Gebotsbit (Datei
  0x47334). Bei „verkaufen“ setzt es ohne jede weitere
  Prüfung Pool +0x1D = Bieter (+0x16), ruft 0x058DC (Teamwerte des Bieters), schreibt dem Konto gut (0x4749A), entfernt den
  Markt-Platz (0x425B6, Art 10) und setzt Pool +0x1A = 6.
- Angebots-Zweig (0x46D8F–0x46FCF): gleiches Bild ohne Prüfung des Bieters. 0x058DC kommt nur bei Verein < 0x40
  (Datei 0x46F89); danach Pool +0x1A = 6 (Datei 0x46FCA).
- Weder Kader noch Kadergröße noch der Status des bietenden Vereins werden gelesen. Computervereine haben im Original keinen
  Kader, ihre Spieler stehen nur im Pool mit Besitzer 6 und Verein +0x1D (A923, FILE75 in `archiv/analyse/abgleich-dosbox-22.md`).
  Der Bieter eines Markt-Gebots kommt aus 0x059B8 mit Schalter 0, also Verein 0 … 42 (Gebot Datei 0x07358–0x0736E).
  Ein Verein mit Vereinssatz +0x16 ≠ 0 wird dort neu gewürfelt (Datei 0x05A5B–0x05A65); im Remake ist das `BidderStrength` =
  null für Managervereine.
- **Ergebnis:** „Bieterkader inzwischen voll“ gibt es im Original nicht. Der Ablauf der Aufnahme (Sitzung 3 Nr. 3) ist damit
  nicht herstellbar, eine Aufnahme ist nicht nötig. Das Original verkauft immer, sobald Besitz und Gebotsbit stimmen.

**Beleg:**

```
python3 tools/reverse-engineering/abgleich/a259_annahme.py ~/referenz/original/gamedata/em.exe
markt: Bieter 5, Pool +0x1A vorher 0, Antwort 0
    0x49D36 Spielerwert -> 100000 (fest)
    0x0C037 random(5000, 10000) -> 5000
    0x0C037 random(85, 115) -> 85
    0x491F9 Angebotsfenster -> 0 (verkaufen)
    0x4B270 Verfall Gebot/Meldung()
    0x058DC Teamwerte Bieter (Verein, Wert)(5, 63)
    0x425B6 Satz entfernen (Platz, 1, Art)(2, 1, 10)
    Ende Datei 0x474e8; Pool +0x1A = 6, +0x1D = 5; Konto Manager = 83400; Satz +0x09 = 0x00
markt: Bieter 5, Pool +0x1A vorher 0, Antwort 1
    … 0x491F9 Angebotsfenster -> 1 (behalten) / 0x4B270 …
    Ende Datei 0x474e8; Pool +0x1A = 0, +0x1D = 12; Konto Manager = 0; Satz +0x09 = 0x00
markt: Bieter 5, Pool +0x1A vorher 4, Antwort 0
    Ende Datei 0x474e8; Pool +0x1A = 4, +0x1D = 12; Konto Manager = 0; Satz +0x09 = 0x80
angebot: Bieter 5, Pool +0x1A vorher 0, Antwort 0
    0x49D36 Spielerwert -> 100000 (fest)
    0x0C037 random(87, 125) -> 87
    0x491F9 Angebotsfenster -> 0 (verkaufen)
    0x4B270 Verfall Gebot/Meldung()
    0x425B6 Satz entfernen (Platz, 1, Art)(2, 1, 29)
    0x058DC Teamwerte Bieter (Verein, Wert)(5, 63)
    Ende Datei 0x46fcf; Pool +0x1A = 6, +0x1D = 5; Konto Manager = 87000; Satz +0x09 = 0x00
angebot: Bieter 70, Pool +0x1A vorher 0, Antwort 0
    … (kein 0x058DC) … Ende Datei 0x46fcf; Pool +0x1A = 6, +0x1D = 70; Konto Manager = 111900; …
```

Außer den aufgezeichneten Aufrufen verlässt keiner der beiden Zweige den nachgebildeten Bereich (`allowed`). Ein Zugriff auf
Kader- oder Vereinsdaten des Bieters fehlt also. Der Zahlenteil (Höhe nach 0x47347 bzw. 0x46E0E) stimmt mit den
`[ORIGINAL]`-Stellen `Transfer.cs` `RollBid` und `UnsolicitedOfferRules.AmountThirds` überein. Nicht geprüft: was das
Angebotsfenster 0x491F9 über den Zeiger am Betrag ändert (hier durch die Antwort ersetzt). Ablöse/Restlaufzeit
(`TransferFee`, Datei 0x49332) ist nicht Gegenstand von A259.

**Vergleich Remake:** `Accept` verwirft das Gebot in vier Fällen. (1) Bieterkader ≥ 99 (`PlayerFactory.MaxNumber`): nur im
Remake-Modell denkbar, weil dort Computervereine Kader führen; im normalen Spiel praktisch nicht erreichbar. (2) Bieter ist
inzwischen Managerverein. (3) Bieter = abgebender Verein. (4) Verkäufer nicht mehr Manager dieses Vereins (Test
`LoanReturnInvalidBidTests.PR3502_BidThatBecameInvalid_IsRejectedWithResultAndRemoved`). (2) und (3) schließt das Original bei
der Gebotsvergabe aus (0x059B8). Nur der Sonderfall, dass ein Bieterverein **nach** dem Gebot einen Manager bekommt (neuer
Manager mitten in der Saison, #847), läuft auseinander: Das Original würde verkaufen (Pool +0x1A = 6, +0x1D = jetzt
Managerverein), das Remake verwirft. Für (4) ist nicht gelesen, ob das Original die Markt-Plätze beim Vereinswechsel leert;
die Prüfung des Klicks hängt am Manager, nicht am Verein. Beides ist nicht aufgenommen und ohne Spielwirkung im üblichen
Ablauf: **P3 „Verdacht“**, kein Issue.

**Vorschlag K.** Die Remake-Schutzprüfung greift nur in Zuständen, die das Original nicht kennt oder nicht prüft. Im
erreichbaren Ablauf verkauft das Remake wie das Original. Keine Codeänderung.

**Teil 2:**

- `restliste-v1-klassen.csv` A259: Klasse `K`; Abweichung: „Das Remake verwirft ein Gebot, das in seinem Modell nicht mehr
  ausführbar ist (Bieterkader 99, Bieter inzwischen Managerverein, Verkäufer nicht mehr beim Verein); das Original prüft beim
  Annehmen nur Besitz (Pool +0x1A = Manager) und Gebotsbit und verkauft immer.“ Beleg: „abgleich-dosbox-22b (#1083):
  `tools/reverse-engineering/abgleich/a259_annahme.py`, Datei 0x4710E–0x474E8 (Markt) und 0x46D8F–0x46FCF (Angebot); Bieter
  nur Computervereine 0 … 42 ohne Manager (0x059B8, Datei 0x05A5B); Computervereine ohne Kader (A923)“. Verdacht: „Bieterverein
  bekommt nach dem Gebot einen Manager: Original verkauft, Remake verwirft (nicht aufgenommen)“.
- Markertext `Transfer.cs:642` ist Produktcode. In Teil 2 bleibt er unverändert (Dateigrenze: nur `docs/**`), die ID bleibt.
  Wortlaut für eine spätere Runde: „[APPROX] Das Original prüft beim Annehmen nur Besitz und Gebotsbit (Datei
  0x47191–0x47339) und verkauft immer; ein voller Bieterkader kommt dort nicht vor (Computervereine ohne Kader).“
- `vorlagen/dosbox-aufnahme.md`: Sitzung 3 Nr. 3 (A259) streichen – im Original nicht herstellbar (siehe oben).

## A707 – Grundstärken neuer Marktspieler eines Nationalteams

Grundlage: Nachbildung über die Bytes von em.exe (`tools/reverse-engineering/abgleich/a707_grundstaerken.py`), Remake-Probe
(Import FILE01/FILE21, nicht eingecheckt), Bild `a707_1_hauptmenue_18041994_wm` und README „Runde 22, Sitzungen 4 und 6“
(Nutzer: im Modus „nur Weltkugel“ kein Transfermarkt für den Bundestrainer, FILE82/FILE83 nicht aufgenommen).

- **Was das Original rechnet.** 0x4981E (Stub 0x118:0x61) bekommt Poolindex und Vereinssatz und liest nur die Vereinsfelder
  **0x19** (Kondition Sturm) und **0x1B** (Technik Sturm) dieses Satzes: +0x15 = 0x19 + random(10 · t, 14) − 10, +0x16 = 0x1B +
  random(10 · t, 14) − 10 (+ random(12, 20) bei random(0, 4) = 0), beide 10 … 99 (0x4BD35); t = 1 nur für Sätze 0x6AD … 0x6AD + 11
  (0x4D2F0, 0x6AD = 52). Eine Aufstellung liest die Routine nicht; Länderwerte gehen nur über das, was im Satz steht.
- **Ersatzverein.** Vor dem Aufruf (Datei 0x49656–0x496ED) ersetzt 0x494EF den Poolverein durch einen Verein ohne Manager, wenn
  Satz +0x16 ≠ 0 ist – das gilt auch für den Satz des Landes mit Bundestrainer (+0x16 = 6, einziger Schreiber Datei 0x55494; an
  allen Originalständen mit Bundestrainer gesetzt, z. B. FILE80/FILE81 Satz 54). Dann geht ein Oberligaverein (random(24, 35))
  mit t = 0 ein, **nicht** das Land. Das Remake prüft nur `IsClubManaged` (Managervereine) und nähme das Land.
- **Erreichbarkeit.** 0x494EF hat nur die Aufrufer Datei 0x43A9F (Spielstart, vor der Hauptschleife 0x43B09) und 0x54E7C
  (Hauptweg). Modus 1 überspringt den Hauptweg (Datei 0x54CEE–0x54D38 → 0x553FF), Modus 4 ernennt erst am 13. April in der
  WM-Schleife ohne Listenwechsel (`CupsNational.WorldCupLoop.cs`, [ORIGINAL] Datei 0x54E71) und setzt +0x16 zum Saisonstart
  zurück (Datei 0x55753). Ein Listenwechsel mit +0x16 = 6 beim Land kommt also nicht vor; der Unterschied beim Ersatzverein ist
  ohne Spielwirkung. Das deckt sich mit der Angabe des Nutzers (kein Markt für den Bundestrainer in Modus 1).
- **Länderwerte im Remake.** `RollMarketStrengths` liest für Nationalteams `game.NationalRatings` = Felder 0x19/0x1B, für
  Deutschland `_germanyRatings` aus der Aufstellung (`UpdateNationalTeamValues`, CupsNational.GameState.cs:109), dem Würfeln ohne
  Bundestrainer (0x554CF) bzw. dem Import (OriginalImport.National.cs:110). Nach einer Saison mit Bundestrainer bleiben die Werte
  der Aufstellung in beiden stehen (Original FILE20 → FILE21: 0x18–0x1B bleiben). Der Satz im Marker „Länderwerte aus der
  Aufstellung (0x952C) gehen nicht ein“ ist damit **veraltet** (Verdacht der Klassenliste bestätigt). Vereine: Remake
  `Strength` + Abweichung = Feld 0x19/0x1B des Remakes (Speicherform A842, K; Stärken 20 … 95, also gleich `ComputerTeamValue`).

Beleg (em.exe):

```text
$ python3 tools/reverse-engineering/abgleich/a707_grundstaerken.py ~/referenz/original/gamedata/em.exe
Teil A – Listenwechsel 0x494EF, erster Zug, Poolspieler mit +0x1D = Satz des Landes:
  Satz 54 +0x16 = 6: stand-in random(24, 35) = 24; 0x4981E(pool 2, club record 24)
  Satz 54 +0x16 = 0: 0x4981E(pool 2, club record 54)
  Ligagrenzen im Abbild: 0x7C0[l] = 0, 0x7B1[l] = 12, 0x7C6/0x7C7[2l] = 0…11 | … 12…23 | … 24…35
Teil B – Grundstärken 0x4981E (Satz 54: 0x19 = 81, 0x1B = 80; Satz 30: 0x19 = 43, 0x1B = 47):
  Satz 54, Zufall unten: random(10, 14) = 10; random(10, 14) = 10; random(0, 4) = 0; random(12, 20) = 12 -> +0x15 = 81, +0x16 = 92
  Satz 54, Zufall oben: random(10, 14) = 14; random(10, 14) = 14; random(0, 4) = 4 -> +0x15 = 85, +0x16 = 84
  Satz 30, Zufall unten: random(0, 14) = 0; random(0, 14) = 0; random(0, 4) = 0; random(12, 20) = 12 -> +0x15 = 33, +0x16 = 49
  Satz 30, Zufall oben: random(0, 14) = 14; random(0, 14) = 14; random(0, 4) = 4 -> +0x15 = 47, +0x16 = 51
```

Beleg (Remake, Probe: `OriginalImport.Load`, `TransferPoolRules.RollMarketStrengths(p, club, Random mit unterer/oberer Grenze,
game)`; Konsolenprojekt im Scratch mit Verweis auf Core/Formats):

```text
FILE01: NationalRatings(56) = (84, 91, 93, 91), IsClubManaged(56) = False, NationalCoach = -
  Deutschland, Zufall unten: +0x15 = 93, +0x16 = 99      (Original-Formel: 93 + 10 − 10; 91 + 10 − 10 + 12 → 99)
  Verein 30 (Feld 0x19 = 40, 0x1B = 39), Zufall unten: +0x15 = 30, +0x16 = 41
  Deutschland, Zufall oben: +0x15 = 97, +0x16 = 95
  Verein 30 (Feld 0x19 = 40, 0x1B = 39), Zufall oben: +0x15 = 44, +0x16 = 43
FILE21: NationalRatings(56) = (0, 0, 9, 10) (Aufstellung des Bundestrainers, Satz 58 0x18–0x1B = 00 09 00 0A)
  Deutschland, Zufall unten: +0x15 = 10, +0x16 = 22
```

Gleiche Formel, gleiche Felder (Sturm 0x19/0x1B), gleiche Begrenzung; die Länderwerte der Aufstellung gehen im Remake ein.

- **Vorschlag X** (Code belegt). Eine Aufnahme mit Verein und Bundestrainer ist **nicht nötig**: Die Nachbildung zeigt, dass
  0x4981E nur den Vereinssatz liest, und mit Bundestrainer läuft kein Listenwechsel.
- **Neu, ohne ID (K, kein Issue):** Ersatzverein bei +0x16 = 6 (Land des Bundestrainers) fehlt im Remake (`TransferMarket.IsManaged`
  nur Managervereine); im Original nicht erreichbar (siehe oben). Nur vermerken.

**Teil 2:**

- `restliste-v1-klassen.csv`, A707: Klasse **X**, Abweichung „Keine: Grundstärken aus Vereinsfeld 0x19/0x1B des Satzes wie
  0x4981E; für Nationalteams `NationalRatings` (Deutschland mit den Länderwerten der Aufstellung, 0x952C). Vereinsfelder in der
  Speicherform von A842.“; Beleg „`tools/reverse-engineering/abgleich/a707_grundstaerken.py` (0x494EF/0x4981E nachgebildet), Probe
  FILE01/FILE21 (`docs/analyse/abgleich-dosbox-22b.md` A707); Ersatzverein bei Bundestrainer (+0x16 = 6) im Original ohne
  Listenwechsel nicht erreichbar (Aufrufer 0x43A9F, 0x54E7C; Modus 1 Datei 0x54CEE)“; Verdacht leer.
- Markertext `Transfer.Pool.cs` (Summary von `RollMarketStrengths`): `[APPROX]`-Satz ersetzen durch „[ORIGINAL] Vereinsfelder der
  Vereine: Datenbankstärke + Abweichung (`MatchEngine.TeamValuesOf`, Speicherform A842) = Feld 0x19/0x1B; Länderwerte der
  Aufstellung über `GameState.NationalRatings` (A707, `abgleich-dosbox-22b`).“ – das ist Produktcode (Kommentar) und gehört
  nicht in Teil 2 dieser Spur (Dateigrenze nur Doku); bis dahin führt die CSV die ID als X wie die 24 ausgeblendeten Funktionen,
  oder die Leitstelle gibt die Markerzeile an `aufraeumen`/Runde 28 (Wortlaut ändert die ID).
- `vorlagen/dosbox-aufnahme.md` Sitzung 4 Nr. 2 (FILE82/FILE83, A707) **streichen**: Aufnahme nicht nötig (Nachbildung reicht;
  Listenwechsel mit Bundestrainer im Original nicht erreichbar).

## Abnahme Teil 1

- `python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --csv <scratch>` vor (Base `920a5c2`)
  und nach diesem Bericht: Statusverteilung gleich (umgesetzt 339, dokumentiert 110, genähert 3, unbekannt 227; keine Funktion
  mit geändertem Status).
- Alle Skripte laufen mit `~/referenz/original/gamedata/em.exe` (Pfad im Klon klein geschrieben) bzw. den Ständen unter
  `~/referenz/original/spielstaende/` mit Rückgabe 0.
