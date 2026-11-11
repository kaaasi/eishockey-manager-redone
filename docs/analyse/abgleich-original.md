# Abgleich Remake ↔ Original an Spielstandpaaren

Spur `abgleich-original` (Runde 15, Vormerkung Vorschlag 1, R15-D2 = A). Stand: `main` 62e4927 (nach `tagesablauf` #725, `kader` #726, `hok-export` #727) + PR #728 (Fixlauf zum Review
`docs/reviews/2026-11-10-abgleich-original.md`, Listen erneuert), Saat 1993.

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
     Spielerwerte ±2 … ±20, Teamwerte ±15, Kondition/Technik über einen Saisonwechsel ±35) oder nur über Invarianten (Ergebnisse, Spiele je Verein, Zahl und Mittelwerte des
     Pools ±8); unter Markt und Pool ist jedes Feld zufallsabhängig,
   - **Eingriff** – Pfadmuster je Paar aus der README der Spielstände,
   - **nicht im HOK** – Feldkarte N/U und Merker des Remakes (nicht abgedruckt).
4. Tests: `AbgleichOriginalWerkzeugTests` (Klassen decken das Schema, Bewertung, Halt), `AbgleichOriginalPaareTests` (je Paar
   erwartete Liste mit Issue-Nummer; neue Abweichung **und** nicht mehr zutreffendes Muster sind rot; alle Paare zusätzlich mit
   den Saaten 4711, 1, 2, 3, 77). Muster nennen das belegte Feld (Belastung je Spieler, „nur Weltkugel“ Alter/Vertrag mit festem
   Abstand +1/−1), nicht ganze Kader; Muster, die nur bei manchen Saaten treffen, sind als optional markiert.

## Ergebnis je Paar

Alle elf Paare erreichen den Halt von b. Zahlen: Abweichungen bei Saat 1993 vor der Erneuerung (Basis 3af4ec9, davon Teamwerte
#729) und danach (Basis 62e4927).

| Paar | Art | vorher | nachher | nachher erwartet |
|---|---|---:|---:|---|
| FILE02→03 | 3 Spieltage | 74 (71) | 3 | Belastung 3 Spieler #730 |
| FILE13→14 | Saisonwechsel, Oberliga | 129 (119) | 22 | Pool #731, Konto #734, Aufstellung #733, Teamwerte Managerverein #748 (optional), Belastung (Zufall, optional) |
| FILE16A→17 | Saisonwechsel (Doppelwechsel) | 162 (140) | 16 | Pool #731, Belastung 9 (+6 optional) #730, Konto #737, Teamwerte LANDSHUT (Zufall, optional) |
| FILE20→21 | Saisonwechsel „nur Weltkugel“ | 178 (121) | 83 | Managersatz #732 (jetzt auch Gehalt/Preis), Tag 0 #699, Pool #731 |
| FILE22→23 | Saisonwechsel | 128 (122) | 1 | Pool #731 |
| FILE24→25 | Spieltag mit Verletzten | 71 (68) | 3 | Belastung #730, Aufstellung #733 |
| FILE30→31 | erstes Heimspiel | 73 (73) | 0 | – |
| FILE38→39 | Pokalspiel | 67 (65) | 2 | Belastung #730 |
| FILE39→40 | Ligaspiel | 67 (67) | 0 | – |
| FILE44→46 | Saisonwechsel, Eingriff Vertrag KJELLBERG | 154 (146) | 5 | Pool #731, Poolplatz KJELLBERG #749 (optional), Form KÖLN #738 (optional) |
| FILE18→48 | Bundestrainer, Eingriff Kader geleert | 587 (587) | 7 | Teamwerte neu gewürfelt #747 (Länderwerte DEUTSCHLAND = Eingriff) |

## Abweichungen und Einordnung

| Issue | P | Einordnung | Felder | Fundstelle |
|---|---|---|---|---|
| #729 | P1 | **behoben durch #727** | `Club.TeamValues` Kondition/Technik, Folge `Tactics._shownAuto*` | `OriginalImport.cs` `ApplyTeamValues` (jetzt Abweichung) |
| #699 | P3 | **behoben durch #725** (Punkt 2 Tag 0), außer „nur Weltkugel“ (FILE20→21: `DayOpened`) | erstes Menü der Saison: `DayOpened`, `FinanceDay`, `TrainedUntil`, `RankSlotsRecorded` | `SeasonFlow.cs` `OpenStartDay` (ohne `WorldOnly`) |
| #730 | P2 | **behoben durch #752** (Training begrenzt die Belastung wie Datei 0x4BD56 vorzeichenbehaftet: ab 128 → 50; je Spieler seither Sägezahn mit zufälliger Phase, geprüft über das Vereinsmittel ±10) | `Player.Strain` der Managerkader (Original 50) | `Squad.Training.cs` Erholung (Datei 0x6B08–0x6B1A) |
| #731 | P2 | Kondition/Technik **behoben durch #752** (Tausch Pool ↔ Kader lässt die Werte am Platz); Belastung kein Befund (Pool hat im Original keine, Import setzt 150) | Pool nach Saisonwechsel: Belastung ≈ 87 statt ≈ 146, Technik/Kondition 5–16 tiefer | `Transfer.Pool.cs` `Random16(80, 120)` |
| #734 | P2 | offen, **kein Finanzfehler** (#752): Original nach dem Laden mit 5 Eingesetzten (`file13_2`) → Strafen, Platz 12, Konto auf 400.000 DM; mit dieser Aufstellung im Bereich (`AbgleichFixMoneyTests`); Ursache der Aufstellung offen (#733) | Konto FILE13→14: 364.736 statt 183.787 DM | Finanzbuch Restsaison/Saisonwechsel |
| #737 | P2 | **Zufall** (#752): 200.000 DM Einzelprämien im Original (`file16_9`; gleicher Start ohne Prämien: `file23_1`), mit ihnen im Bereich; Muster jetzt `Chance` | Konto FILE16A→17: Veränderung −2,33 Mio. statt −1,92 Mio. DM, über sechs Saaten ±1.000 DM | Finanzbuch, negativer Kontostand |
| #747 | P2 | **behoben durch #752** (`EnsureTeamValues` übergeht den Managerverein „nur Weltkugel“, Import leert ihn) | „nur Weltkugel“: alle Teamwerte nach dem Import neu gewürfelt (FILE18→48 16–20, Nationalteams 0) | `OriginalImport.cs` `ApplyTeamValues` (Managerverein `null`) → `MatchEngine.EnsureTeamValues`/`LoadTeamValues` (alle Vereine) |
| #732 | P3 | **teilweise behoben durch #752**: Teamwerte (#747); Alter, Laufzeit, Pool-Alter (0x26F20 läuft im Modus 1 nicht, `AbgleichFixWorldOnlyTests`); Gehalt/Preis kein Befund (Importbewertung). Offen: Konto (−2.030.327 DM; Original ruft den Tagesabschluss 0x100:0x4D für Satz 0 in Sommerpause 0x56512 und WM-Schleife 0x5569A, Remake im Modus 1 ohne Vereinsfinanzen) | „nur Weltkugel“: Managersatz 0 altert, Konto, Teamwerte; Original bewertet Gehalt/Preis neu (neue Tendenz) | Regel 1 der Feldkarte, Saisonwechsel |
| #733 | P3 | FILE24→25 **behoben durch #752** (Import liest das gesicherte Byte 0x376 + 2 · m: Laden setzt den Tageslauf am Schleifenkopf fort, Datei 0x54CC2 → 0x54CEE, und holt am Ereignistag 0x375 := 0x376 zurück, 0x54D84–0x54D9E); FILE13→14 **Eingriff** (kein Schreiber einer 0 ohne Nutzerhandlung: Aufstellung von Hand 0x40ADE/0x40DE4, Taktikknopf 0x47F4E/0x49058; Laden/Sichern kopieren den Block 0x5C3E3/0x5B9DD) | `Club._autoLineup`; Formwerte und (Saaten 2, 3, 4711) verdeckte Werte KREFELD FILE13→14 als Folge der Handaufstellung | `OriginalImport.Rest.cs` `ApplyTactics`, DGROUP 0x375/0x376 |
| #738 | P3 | **behoben durch #752**: Import ohne Reihenspieler ohne „eingesetzt“; im Handbetrieb rückt nach Verletzungen niemand nach (0x426BC endet ohne Einstellung, Datei 0x426C4–0x426E4). FILE44→46 an den Saaten 1, 2, 3, 77: Form KÖLN 35/38 wie im Original (`AbgleichFixLineupTests`) | Teamform KÖLN FILE44→46 im Remake 10–17 höher | `Squad.Lineup.cs` `RefillLineup`, Saisonwechsel |
| #748 | P3 | **behoben durch #752** (0x952C(m, 0/1) schreibt die Felder 0x18–0x1D der Managervereine an Tagen mit Ereignis aus der Aufstellung, Datei 0x54DAA/0x54FBF; Beleg: alle Stände mit „automatisch aufstellen“ stehen genau auf diesen Werten). FILE13→14 bleibt als Folge der Handaufstellung (Eingriff, #733) | Kondition/Technik KREFELD (Managerverein) FILE13→14: Original absolut 0–13, Remake jetzt aus der automatischen Aufstellung | `GameState.DailyRun.cs` `UpdateManagerTeamValues` |
| #749 | P3 | **behoben durch #752** (Import: Besitzer Manager ohne Kaderplatz = vergeben) | Poolplatz 196 KJELLBERG FILE44→46: Import frei, Remake vergeben (R15-E3 = C); `Invariante.PoolSpieler` 175 ↔ 174 (Saaten 1993, 1) | `OriginalImport.Market.cs` (Besitzer Manager ohne Kaderspieler) ↔ `Transfer.Pool.cs` `StartPoolSeason` |

**Zufall, kein Befund:** Ergebnisse, Tabellen, Statistik, Markt, Pool einzeln, Aktienkurse, Nachrichten, Zuschauer; Teamwerte
Kondition/Technik über einen Saisonwechsel bis ±31 (große Wanderung je Verein, Bereich ±35); LANDSHUT FILE16A→17 (Original in die
Oberliga abgestiegen, Werte auf deren Obergrenze −33, Remake nicht; Liga über den Saisonwechsel zufallsabhängig); Belastung KREFELD
FILE13→14 bei Saaten 1993 und 77 (Endrunde mit 9 statt 3 Spielen). Im Test als optionale Muster mit „Zufall“ (`Chance`).
**Eingriff:** FILE44→46 (KJELLBERG, Kadergröße, Aufstellung), FILE18→48 (Nationalkader, Automatik, Länderwerte der Reihen:
DEUTSCHLAND alle sechs Teamwerte, Original 0 absolut bei leerem Kader, Remake `[APPROX]` unverändert).
**Nicht in den Listen:** #698 (behoben durch #726) betrifft vier Saisonwechsel nach FILE44, kein Paar.

## Grenzen

- Bereiche sind Schätzungen `[APPROX]`, nachgezogen an den Saaten 1993, 4711, 1, 2, 3, 77 (Review P3-2): Pool-Mittel ±8 (größter
  Abstand innerhalb einer Saison 5,7, FILE02→03 Saat 3), Geld wie oben (größter Abstand ohne Befund 33.000 DM bzw. 9 %). Saatabhängig
  und darum als optionale Muster geführt: verdeckte Werte KREFELD (#733), Form/Belastung LANDSHUT (#732), Form KÖLN FILE44→46
  (#738), Teamwerte KREFELD (#748), Poolzahl FILE44→46 (#749), Belastung BAYREUTH (6 Spieler, #730) und die Zufallsfälle oben. Eine neue Zufallsreihenfolge kann weitere Grenzfälle kippen; dann an allen sechs Saaten nachziehen.
- Gegenproben (Basis 3af4ec9; Mutation in einer Kopie, Testlauf `AbgleichOriginalPaareTests`): „Gehälter fehlen“ (`Book` bucht `Gehaelter` nicht)
  → rot in FILE22→23 und FILE44→46 (je sechs Saaten) und im Test `Money_WithoutSalaries_IsDeviation`, der die Mutation nachbildet;
  in FILE13→14 und FILE16A→17 verdeckt das erwartete Muster `Managers[0].Money` (#734, #737) sie. „Alter +2 am Saisonwechsel“ → rot
  in allen fünf Saisonwechsel-Paaren einschließlich FILE20→21. „Erholung halbiert“ → rot in FILE13→14 und FILE16A→17; in den Paaren
  mit 1–3 Tagen bleibt die Belastung innerhalb ±20, und in FILE44→46 verdeckt das optionale Muster #738 die Formänderung.
- Ein Bereich kann einen Fehler verdecken (R15-D2, Option C bitgenau nach v1.0 vorgemerkt).
- Nach #727 sind die Teamwerte vergleichbar; die Bereiche sind an allen sechs Saaten nachgezogen (Saisonwechsel ±35 statt ±15,
  `HokCompareClasses.TeamStrength`). Der Bereich ±35 verdeckt #747 in FILE20→21 (Saisonwechsel); FILE18→48 zeigt ihn.
- Der Import von b ist der geladene Stand (mit Ladeeigenheit am Markierungstag), nicht der gesicherte.
- Erneuert gegen `main` 62e4927 (Fixlauf 10.11.2026). Jede weitere Änderung am Spielkern kann Einträge kippen: behobene
  Einträge und neue Abweichungen machen den Test rot.
