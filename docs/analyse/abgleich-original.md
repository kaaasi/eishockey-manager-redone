# Abgleich Remake ↔ Original an Spielstandpaaren

Spur `abgleich-original` (Runde 15, Vormerkung Vorschlag 1, R15-D2 = A). Stand: `main` 3af4ec9 + PR #728, Saat 1993.

## Verfahren

`emtool abgleich <a.hok> <b.hok> [--gamedata ordner] [--seed n] [--alle]` (`src/EishockeyManager.Tools/HokCompare*.cs`):

1. Stand a importieren (`OriginalImport.TryImport`), ohne Eingriff bis zum Halt von b spielen: je Halt einmal
   `GameState.OnMainMenu` (Aufbau des Hauptmenüs), dann `Continue()`; am Saisonende `StartNextSeason()` (ausgelaufene Verträge
   enden wie „Abbruch“), erster aktiver Manager am Zug, im Modus „nur Weltkugel“ ein weiterer Tag. Halt = Saison, Tag, Phase,
   Manager am Zug, Zug des Bundestrainers. `[APPROX]` Fragen und Angebote bleiben unbeantwortet.
2. Abdruck beider Stände über alle Instanzfelder wie im eigenen Spielstandformat (Schemafeld `Typ.Feld`), Vereine nach Index,
   Spieler nach Namen. Kader der Computervereine fehlen im Abdruck (der Import erzeugt sie, Feldkarte Regel 3).
3. Vergleich je Feld nach Klasse (`HokCompareClasses.cs`, eine Datei):
   - **deterministisch** – exakt (Datum, Zähler, Spielplan, Verträge, Alter, Bau, Einstellungen, Kredit, Sponsorverträge;
     Liga und Spielplan nur innerhalb einer Saison),
   - **zufallsabhängig** – Bereich (Geld ±25 %, Spielerwerte ±2 … ±20, Teamwerte ±15) oder nur über Invarianten (Ergebnisse,
     Spiele je Verein, Zahl und Mittelwerte des Pools ±5); unter Markt und Pool ist jedes Feld zufallsabhängig,
   - **Eingriff** – Pfadmuster je Paar aus der README der Spielstände,
   - **nicht im HOK** – Feldkarte N/U und Merker des Remakes (nicht abgedruckt).
4. Tests: `AbgleichOriginalWerkzeugTests` (Klassen decken das Schema, Bewertung, Halt), `AbgleichOriginalPaareTests` (je Paar
   erwartete Liste mit Issue-Nummer; neue Abweichung **und** nicht mehr zutreffendes Muster sind rot; zwei Paare zusätzlich mit
   Saat 4711).

## Ergebnis je Paar

Alle elf Paare erreichen den Halt von b. Zahlen: Abweichungen insgesamt (davon Teamwerte #729).

| Paar | Art | Abweichungen | außer Teamwerten |
|---|---|---:|---|
| FILE02→03 | 3 Spieltage | 74 (71) | Belastung 3 Spieler #730 |
| FILE13→14 | Saisonwechsel, Oberliga | 130 (119) | Tag 0 #699, Pool #731, Konto #734, Aufstellung #733 |
| FILE16A→17 | Saisonwechsel (Doppelwechsel) | 163 (140) | Tag 0 #699, Pool #731, Belastung 15 #730 |
| FILE20→21 | Saisonwechsel „nur Weltkugel“ | 178 (121) | Managersatz #732, Tag 0 #699, Pool #731 |
| FILE22→23 | Saisonwechsel | 130 (122) | Tag 0 #699, Pool #731 |
| FILE24→25 | Spieltag mit Verletzten | 71 (68) | Belastung #730, Aufstellung #733 |
| FILE30→31 | erstes Heimspiel | 73 (73) | – |
| FILE38→39 | Pokalspiel | 67 (65) | Belastung #730 |
| FILE39→40 | Ligaspiel | 67 (67) | – |
| FILE44→46 | Saisonwechsel, Eingriff Vertrag KJELLBERG | 154 (146) | Tag 0 #699, Pool #731 |
| FILE18→48 | Bundestrainer, Eingriff Kader geleert | 587 (587) | – (Länderwerte DEUTSCHLAND = Eingriff) |

## Abweichungen und Einordnung

| Issue | P | Einordnung | Felder | Fundstelle |
|---|---|---|---|---|
| #729 | P1 | Importlücke | `Club.TeamValues` Kondition/Technik, Folge `Tactics._shownAuto*` | `OriginalImport.cs` `ApplyTeamValues` (absolut) ↔ `MatchEngine.TeamValues.cs` (Abweichung vom Datenbankwert) |
| #730 | P2 | Remake-Fehler oder Importlücke | `Player.Strain` der Managerkader (Original 50) | `Squad.Training.cs` Erholung (Datei 0x6B08–0x6B1A) |
| #731 | P2 | Remake-Fehler | Pool nach Saisonwechsel: Belastung ≈ 87 statt ≈ 146, Technik/Kondition 5–16 tiefer | `Transfer.Pool.cs` `Random16(80, 120)` |
| #734 | P2 | offen (Fehler oder Zufall) | Konto FILE13→14: 364.736 statt 183.787 DM | Finanzbuch Restsaison/Saisonwechsel |
| #699 | P3 | Remake-Fehler (bekannt, Punkt 2 Tag 0, Spur `tagesablauf`) | erstes Menü der Saison: `DayOpened`, `FinanceDay`, `TrainedUntil`, `RankSlotsRecorded` | `SeasonFlow.cs` |
| #732 | P3 | offen | „nur Weltkugel“: Managersatz 0 altert, Konto, Teamwerte | Regel 1 der Feldkarte, Saisonwechsel |
| #733 | P3 | offen | `Club._autoLineup` kippt im Original ohne Eingriff; Formwerte FILE13→14 | `Squad.AutoLineup.cs`, DGROUP 0x375 |

**Zufall, kein Befund:** Ergebnisse, Tabellen, Statistik, Markt, Pool einzeln, Aktienkurse, Nachrichten, Zuschauer.
**Eingriff:** FILE44→46 (KJELLBERG, Kadergröße, Aufstellung), FILE18→48 (Nationalkader, Automatik, Länderwerte der Reihen).

## Grenzen

- Bereiche sind erste Schätzungen `[APPROX]`; die Probe mit Saat 4711 zeigt dieselbe Liste. Ein Bereich kann einen Fehler
  verdecken (R15-D2, Option C bitgenau nach v1.0 vorgemerkt).
- Die Abweichung #729 überdeckt alles, was von der Spielstärke abhängt (Ergebnisse, Anzeige „AUTOM.“); nach dem Fix die
  Bereiche nachziehen.
- Der Import von b ist der geladene Stand (mit Ladeeigenheit am Markierungstag), nicht der gesicherte.
- Vor dem Merge (letzte Spur der Runde) die erwarteten Listen gegen den dann aktuellen `main` erneuern: behobene Einträge
  (z. B. #699 nach `tagesablauf`) machen den Test rot.
