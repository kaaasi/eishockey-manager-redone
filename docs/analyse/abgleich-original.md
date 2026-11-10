# Abgleich Remake ↔ Original an Spielstandpaaren

Spur `abgleich-original` (Runde 15, Vormerkung Vorschlag 1, R15-D2 = A). Stand: `main` 3af4ec9 + PR #728 (mit Fixlauf zum Review `docs/reviews/2026-11-10-abgleich-original.md`), Saat 1993.

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
   - **zufallsabhängig** – Bereich (Geld: Veränderung seit a, ±10 % der Veränderung des Originals, mindestens ±50.000 DM;
     Spielerwerte ±2 … ±20, Teamwerte ±15) oder nur über Invarianten (Ergebnisse, Spiele je Verein, Zahl und Mittelwerte des
     Pools ±8); unter Markt und Pool ist jedes Feld zufallsabhängig,
   - **Eingriff** – Pfadmuster je Paar aus der README der Spielstände,
   - **nicht im HOK** – Feldkarte N/U und Merker des Remakes (nicht abgedruckt).
4. Tests: `AbgleichOriginalWerkzeugTests` (Klassen decken das Schema, Bewertung, Halt), `AbgleichOriginalPaareTests` (je Paar
   erwartete Liste mit Issue-Nummer; neue Abweichung **und** nicht mehr zutreffendes Muster sind rot; alle Paare zusätzlich mit
   den Saaten 4711, 1, 2, 3, 77). Muster nennen das belegte Feld (Belastung je Spieler, „nur Weltkugel“ Alter/Vertrag mit festem
   Abstand +1/−1), nicht ganze Kader; Muster, die nur bei manchen Saaten treffen, sind als optional markiert.

## Ergebnis je Paar

Alle elf Paare erreichen den Halt von b. Zahlen: Abweichungen insgesamt (davon Teamwerte #729).

| Paar | Art | Abweichungen | außer Teamwerten |
|---|---|---:|---|
| FILE02→03 | 3 Spieltage | 74 (71) | Belastung 3 Spieler #730 |
| FILE13→14 | Saisonwechsel, Oberliga | 129 (119) | Tag 0 #699, Pool #731, Konto #734, Aufstellung #733 |
| FILE16A→17 | Saisonwechsel (Doppelwechsel) | 162 (140) | Tag 0 #699, Pool #731, Belastung 15 #730, Konto #737 |
| FILE20→21 | Saisonwechsel „nur Weltkugel“ | 178 (121) | Managersatz #732, Tag 0 #699, Pool #731 |
| FILE22→23 | Saisonwechsel | 128 (122) | Tag 0 #699, Pool #731 |
| FILE24→25 | Spieltag mit Verletzten | 71 (68) | Belastung #730, Aufstellung #733 |
| FILE30→31 | erstes Heimspiel | 73 (73) | – |
| FILE38→39 | Pokalspiel | 67 (65) | Belastung #730 |
| FILE39→40 | Ligaspiel | 67 (67) | – |
| FILE44→46 | Saisonwechsel, Eingriff Vertrag KJELLBERG | 154 (146) | Tag 0 #699, Pool #731, Form KÖLN #738 (andere Saaten) |
| FILE18→48 | Bundestrainer, Eingriff Kader geleert | 587 (587) | – (Länderwerte DEUTSCHLAND = Eingriff) |

## Abweichungen und Einordnung

| Issue | P | Einordnung | Felder | Fundstelle |
|---|---|---|---|---|
| #729 | P1 | Importlücke | `Club.TeamValues` Kondition/Technik, Folge `Tactics._shownAuto*` | `OriginalImport.cs` `ApplyTeamValues` (absolut) ↔ `MatchEngine.TeamValues.cs` (Abweichung vom Datenbankwert) |
| #730 | P2 | Remake-Fehler oder Importlücke | `Player.Strain` der Managerkader (Original 50) | `Squad.Training.cs` Erholung (Datei 0x6B08–0x6B1A) |
| #731 | P2 | Remake-Fehler | Pool nach Saisonwechsel: Belastung ≈ 87 statt ≈ 146, Technik/Kondition 5–16 tiefer | `Transfer.Pool.cs` `Random16(80, 120)` |
| #734 | P2 | offen (Fehler oder Zufall) | Konto FILE13→14: 364.736 statt 183.787 DM | Finanzbuch Restsaison/Saisonwechsel |
| #737 | P2 | offen (systematisch) | Konto FILE16A→17: Veränderung −2,33 Mio. statt −1,92 Mio. DM, über sechs Saaten ±1.000 DM | Finanzbuch, negativer Kontostand |
| #699 | P3 | Remake-Fehler (bekannt, Punkt 2 Tag 0, Spur `tagesablauf`) | erstes Menü der Saison: `DayOpened`, `FinanceDay`, `TrainedUntil`, `RankSlotsRecorded` | `SeasonFlow.cs` |
| #732 | P3 | offen | „nur Weltkugel“: Managersatz 0 altert, Konto, Teamwerte | Regel 1 der Feldkarte, Saisonwechsel |
| #733 | P3 | offen | `Club._autoLineup` kippt im Original ohne Eingriff; Formwerte und (Saaten 2, 3, 4711) verdeckte Werte KREFELD FILE13→14 | `Squad.AutoLineup.cs`, DGROUP 0x375 |
| #738 | P3 | offen (systematisch) | Teamform KÖLN FILE44→46 im Remake 10–17 höher, bei Saat 1993 knapp im Bereich | Formwerte nach dem Saisonwechsel |

**Zufall, kein Befund:** Ergebnisse, Tabellen, Statistik, Markt, Pool einzeln, Aktienkurse, Nachrichten, Zuschauer.
**Eingriff:** FILE44→46 (KJELLBERG, Kadergröße, Aufstellung), FILE18→48 (Nationalkader, Automatik, Länderwerte der Reihen).

## Grenzen

- Bereiche sind Schätzungen `[APPROX]`, nachgezogen an den Saaten 1993, 4711, 1, 2, 3, 77 (Review P3-2): Pool-Mittel ±8 (größter
  Abstand innerhalb einer Saison 5,7, FILE02→03 Saat 3), Geld wie oben (größter Abstand ohne Befund 33.000 DM bzw. 9 %). Saatabhängig
  und darum als optionale Muster geführt: verdeckte Werte KREFELD (#733), Form/Belastung LANDSHUT (#732), Form KÖLN FILE44→46
  (#738). Eine neue Zufallsreihenfolge kann weitere Grenzfälle kippen; dann an allen sechs Saaten nachziehen.
- Gegenproben (Mutation in einer Kopie, Testlauf `AbgleichOriginalPaareTests`): „Gehälter fehlen“ (`Book` bucht `Gehaelter` nicht)
  → rot in FILE22→23 und FILE44→46 (je sechs Saaten) und im Test `Money_WithoutSalaries_IsDeviation`, der die Mutation nachbildet;
  in FILE13→14 und FILE16A→17 verdeckt das erwartete Muster `Managers[0].Money` (#734, #737) sie. „Alter +2 am Saisonwechsel“ → rot
  in allen fünf Saisonwechsel-Paaren einschließlich FILE20→21. „Erholung halbiert“ → rot in FILE13→14 und FILE16A→17; in den Paaren
  mit 1–3 Tagen bleibt die Belastung innerhalb ±20, und in FILE44→46 verdeckt das optionale Muster #738 die Formänderung.
- Ein Bereich kann einen Fehler verdecken (R15-D2, Option C bitgenau nach v1.0 vorgemerkt).
- Die Abweichung #729 überdeckt alles, was von der Spielstärke abhängt (Ergebnisse, Anzeige „AUTOM.“); nach dem Fix die
  Bereiche nachziehen.
- Der Import von b ist der geladene Stand (mit Ladeeigenheit am Markierungstag), nicht der gesicherte.
- Vor dem Merge (letzte Spur der Runde) die erwarteten Listen gegen den dann aktuellen `main` erneuern: behobene Einträge
  (z. B. #699 nach `tagesablauf`) machen den Test rot.
