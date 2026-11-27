# Review: Integration der Spur `wettbewerbe-spiel` (PR #465)

Unabhängiges Review (Claude Code, geplanter Lauf, 02.11.2026; frische Sitzung ohne Implementierungskontext) nach
`docs/vorlagen/review-anfrage.md`. **Hinweis zur Unabhängigkeit:** Dieser Lauf trägt dieselbe Sitzungskennung
(`session_01GhACvTU96qXCfvVts2ihWL`) wie die Leitstellen-Commits `8a27341`, `a2eeba0`, `08b7b68`; er startete jedoch als
geplanter Lauf ohne deren Verlauf im Kontext (keine Kenntnis der Umsetzung außer Repo und PR). Ob das als „frische
Sitzung“ nach AGENTS.md 2.4 genügt, entscheidet der Nutzer; die Prüfungen unten sind unabhängig nachvollziehbar. Geprüft am nachgewiesenen Checkout (Klon, `git rev-parse HEAD` = Head,
`git status --porcelain` leer), SDK 10.0.402, Originaldaten aus `kaaasi/eishockey-manager-referenz` (`original/`).

| PR | Base | Head |
|---|---|---|
| #465 (Stapel #420 → … → #454 @ `cb02f41`, dazu `8a27341`, `a2eeba0`, `08b7b68`) | `4e71df745b91b5ceb31f0a20a20ad2e63c32c0cc` | `08b7b683949b9229d3fad424b48eea6354764bb2` |

## 1. Merge-Empfehlung

**Ja, für Head `08b7b68`.** Alle drei Läufe von `tools/pruefen.sh` grün mit den Zahlen der PR-Beschreibung; alle
Regressionstests der Spur und alle fünf Anschlusstests der Leitstelle werden unter gezielter Mutation rot; beide
Belegstellen in `em.exe` bestätigt; Konfliktlösung ohne stillen Verlust. Kein P0/P1. Offen bleiben ein P2 in der
Migration 31 → 32 (WS-01, als kleiner Nachtrag lösbar) und der vorläufige Ausnahmezweig der Finanzprobe (WS-02,
Entscheidung Nutzer), dazu P3-Lücken in Proben.

## 2. Befunde

### WS-01 (P2) – Migration 31 → 32 überschreibt eine von Hand gesetzte Aufstellung alter Spielstände

[`SaveGame.Migration.WettbewerbeSpiel8.cs:19`](https://github.com/kaaasi/eishockey-manager-redone/blob/08b7b683949b9229d3fad424b48eea6354764bb2/src/EishockeyManager.Core/SaveGame.Migration.WettbewerbeSpiel8.cs#L19)
setzt `_autoLineup = true` für alle Vereine. Ein Stand vor Teil 8, in dem der Spieler die Aufstellung von Hand gebaut hat
(`_dressedByHand = true`), wird damit am nächsten Tag von 0x426BC neu aufgestellt – die Entscheidung des Spielers geht
ohne Meldung verloren. Im Original schaltet jeder Eingriff von Hand die Einstellung ab (Datei 0x40ADE/0x40DE4); der
treue Übertrag ist daher `_autoLineup = !_dressedByHand` (alte Stände setzen `_dressedByHand` nur durch Eingriffe von
Hand, `AutoLineup()` setzte es zurück).
Gegenbeispiel (Probe am Head, nicht committet): `TestGames.Create(3)`, im Managerverein Stürmer Block 0/2 per
`AssignToLineup` tauschen (Einstellung aus), speichern, `schema = 31`, `_autoLineup` entfernen, laden →
`AutoLineupEnabled = true`, nach `Continue()` steht auf Sturm 0/0 wieder `SPIELER 61` statt der Wahl `SPIELER 55`.
`AutoLineupTeil8Tests.Migration_31To32_SetsSettingOn` prüft nur einen Stand, in dem die Einstellung ohnehin an war.

### WS-02 (P2, Entscheidung Nutzer) – Finanzprobe: Ausnahme „passive Bundesliga entlassen“ gilt für jeden Seed

[`FinanceCalibrationTests.cs:296`](https://github.com/kaaasi/eishockey-manager-redone/blob/08b7b683949b9229d3fad424b48eea6354764bb2/tests/EishockeyManager.Core.Tests/FinanceCalibrationTests.cs#L296)
Bewertung der Entlassung (Originaldaten, Seed 17): **sachlich vertretbar.** Die Probe zeigt am Head und an `main` dieselbe
Lage – ein passiver Bundesligist mit 25 Spielern und ohne Werbung verliert je Saison rund 1,0–1,6 Mio. DM:

| Seed | `main` (4e71df7) tiefster Stand | Head tiefster Stand | Head |
|---|---:|---:|---|
| 3 | −2.125.073 | −1.915.432 | 4 Saisons, nicht entlassen |
| 17 | −1.575.272 | −2.247.111 | Saison 2 entlassen (Regel 8 Wochen) |
| 41 | −1.301.419 | −1.322.581 | 4 Saisons, nicht entlassen |

Schon `main` liegt bei Seed 3 und 17 unter −1,5 Mio. DM, nur kürzer als acht Wochen. Am Head hat Seed 17 in Saison 1 zwei
Heimspiele weniger und keine Prämie (Ende −515.079 statt −245.317) und kippt in Saison 2 über die Regel. Das ist ein
Grenzfall des Seeds, kein Hinweis auf einen Rechenfehler der Integration; die Bedingung prüft die Ursache richtig
(Schulden, Tage, keine Strafen).
Schwäche: Der Zweig greift für **alle** Seeds. Eine spätere Änderung, die auch Seed 3 oder 41 entlässt, bliebe grün, und
die Mehrsaisonprobe (AGENTS.md §5) wäre für „passiv Bundesliga“ dann nicht mehr erbracht.
Alternativen:
1. **(empfohlen)** Regel annehmen, die Ausnahme fest auf Seed 17 setzen (`seed == 17`, sonst Entlassung = rot) und
   zusätzlich verlangen, dass mindestens zwei Seeds vier Saisons durchlaufen.
2. Seed 17 durch einen Seed ersetzen, der vier Saisons schafft, und die Entlassung nur in
   `PassiveBundesligaManager_WithoutCare_IsDismissed` prüfen – sauber, aber Seed-Auswahl nach Ergebnis.
3. Die Hüllkurvenprobe für „passiv Bundesliga“ von vornherein spielfähig **und** zahlungsfähig halten (z. B. Kader auf
   die Mindestgröße abbauen, wie es ein Spieler täte) – ändert die Bedeutung von „passiv“, braucht eine eigene Begründung.

### WS-03 (P3) – „Europapokal nein“ ist im Test leer

[`WettbewerbeSpiel2Tests.cs:40`](https://github.com/kaaasi/eishockey-manager-redone/blob/08b7b683949b9229d3fad424b48eea6354764bb2/tests/EishockeyManager.Core.Tests/WettbewerbeSpiel2Tests.cs#L40)
Gegenprobe: `game.RecordMeetings(played)` zusätzlich in `EuropeanCup` (nach `SettleCupMatches`) → Test bleibt grün.
Ursache: Europapokalgegner haben Index ≥ 64, `MeetingHistory.Record` verwirft sie (`Opponents = 64`), und die Schleife
prüft ohnehin nur Gegner < 52. Die Aussage stimmt also strukturell, der Test belegt sie aber nicht. Vorschlag: Kommentar
anpassen oder einen deutschen Europapokalgegner konstruieren.

### WS-04 (P3) – Neuzählung 0x38A36: Grenze 5 und Abzug vom Sturm ungetestet

[`AutoLineupTeil8Tests.cs:268`](https://github.com/kaaasi/eishockey-manager-redone/blob/08b7b683949b9229d3fad424b48eea6354764bb2/tests/EishockeyManager.Core.Tests/AutoLineupTeil8Tests.cs#L268)
Mutationen in `MatchEngine.RecountSkaters`: `limit = 6` statt 5 (ohne Torhüter) und `forwards − Überschuss` statt
`3 − Überschuss` bleiben grün. `Recount_NoGoalie_DefenseLineReset_LimitFive` hat 0 + 2 + 3 = 5 auf dem Eis, die Grenze
greift nie. Der Rücksetzer der Abwehrreihe (Datei 0x38A2A) ist abgedeckt (Mutation rot). Vorschlag: ohne Torhüter mit
Strafbankspieler (2 + 3 + 1 = 6 > 5) und eine Reihe mit zwei Stürmern plus Strafbank.

### WS-05 (P3) – Nachgeführte Proben: Ursachen nachvollzogen, zwei Prüfungen schwächer

Am Head mit den Testständen von `8a27341` nachgestellt (alle vier rot, Ursache wie angegeben):
- `ClubDressedTests.SchemaFourSave_IsMigrated`: Reservisten verschieden (Position 13) – der Managerverein setzt seit Teil 8
  beim Start von Hand ein, das kann ein Schema-4-Stand nicht tragen. Wechsel auf einen Computerverein vertretbar; die
  Vorgaberegel am Managerverein ist damit nicht mehr geprüft (danach stellt ohnehin 0x426BC auf).
- `WettbewerbeSpiel2Tests` Seed 3: `PlayoffResults` enthält Tag 149 ohne den Manager – `Continue` läuft bis zum nächsten
  Halt; Lockerung richtig. Seed 5: Byte-Überlauf durch DEB-Pokal 19:1 (Tag 124, Verein 0 Stärke 100 gegen 88, ohne
  Penaltyschießen). Hohe Ergebnisse gibt es mit synthetischen Daten auch an `main` und `cb02f41` (7:3, 8:2, 6:2); kein
  Hinweis auf einen Integrationsfehler, die Maske wie 0x4F5A8 ist richtig.
- `StatistikenVereinTests(active: False)`: Saisonprobe scheitert ohne Nachkauf. `TestSquadKeeper`-Rückfallweg (Spieler
  eines Computerkaders umsetzen) ist nur Probe, ohne Buchung und Zufall; vertretbar, aber er verdeckt, ob zu Saisonbeginn
  wirklich kein Vertragsloser auf der Liste steht – das gehört als Beobachtung zur Spur `kader-transfer`.
- `StadiumAusbauTests` S-K1 ([Zeile 247](https://github.com/kaaasi/eishockey-manager-redone/blob/08b7b683949b9229d3fad424b48eea6354764bb2/tests/EishockeyManager.Core.Tests/StadiumAusbauTests.cs#L247)):
  nimmt die neutrale Differenz ungeprüft in `cumulative` auf; `FinanceCalibrationTests` prüft dabei
  `Konto = 400.000 + Prämien`. Vorschlag: dieselbe Gleichheit hier.
- `KaderTransferTeil4Tests` Grenze 22 statt 30 (Seed 5: genau 30): trennt den täglichen Takt weiter vom alten (Mittel 11),
  aber knapper; vertretbar. Ebenso der Seedwechsel 7 → 8 in `SpielberechnungAnschlussTests` (Mittel über 16 Seeds belegt).

## 3. Ausgeführte Prüfungen

### `tools/pruefen.sh` (alle drei Läufe) am Head `08b7b68`

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1924 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1629 | 0 | 263 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1629 | 295 | 0 |

### Gegenproben (Mutation in einer Arbeitskopie des Heads, `dotnet test --filter`, mit Originaldaten)

| Teil / Anschluss | Mutation | Ergebnis |
|---|---|---|
| Begegnungen DEB-Pokal (0x34C5C) | `RecordMeetings(played)` im DEB-Pokal entfernt | rot (2 von 6) |
| Begegnungen Europapokal nein | Eintrag im Europapokal ergänzt | **grün** (WS-03) |
| Tiebreak Pokale (Teil 1) | Penaltytore nicht im Endstand | rot (5 von 7) |
| Länderspiele / Nationalteam-Stärke (Teil 3) | Kondition Abwehr +5 | rot (2 von 7) |
| Startwert +0x19 (0x299F4) | random(3, 13) | rot (1 von 10) |
| 0x426BC Gleichstand → späterer Platz | `>=` statt `>` | rot (3 von 16) |
| Torhüter-Ausweichregel | Wert immer 1 | rot (1 von 16) |
| Ohne Einstellung bleibt die Aufstellung | Prüfung `_autoLineup` entfernt | rot (1 von 16) |
| Neuzählung 0x38A36 | Abwehrreihe nicht zurückgesetzt | rot; Grenze 5 / Sturmabzug **grün** (WS-04) |
| Migration 30 → 31 | Marke immer 0 | rot |
| Migration 31 → 32 | Startwert `false` | rot |
| Anschluss Verletzungsart +0x17 | Zuweisung entfernt | rot (`MatchEngineInjuryKindTests` 3 von 3) |
| Anschluss Play-offs / Aufstiegsrunde | je Eintrag entfernt | beide rot |
| Anschluss Managerzug 0x450C6 | `BeginManagerTurn` leer | rot (`AutoLineupTurnTests`) |
| Anschluss Transferliste 0x47BB5 | Neuaufstellung entfernt | rot (`TransferAutoLineupTests`) |
| Anschluss Meldung 0x40ADE | Meldung entfernt | rot (`LineupAutoDisabledMessageTests`) |
| `a2eeba0` Bezugsbewertung | Neubewertung entfernt | rot (3 von 85 in Squad/KaderStart/Finanz/Stärke) |

### Belegstellen in `em.exe` (`ndisasm`)

- Datei 0x450C6 und 0x47BB5: je `call 0x108:0x4d`; Segmenttabelle (Datei 0x163C0, Eintrag 0x108/8) → Stub-Segment 0x14B7,
  Eintrag bei Stub +0x4D = `CD 3F DC 26` → Overlay 0x108 (ab Datei 0x3FFE0) + 0x26DC = **0x426BC**. Beide Aufrufe liegen in
  den Funktionen ab 0x44E22 bzw. 0x465F1 (kein Prolog dazwischen).
- 0x299B7–0x299F4: zweimal random(3, 12), erster Wert `shl 4`, Speicher `[bx+0x5DDC]` = Kaderplatz +0x19.
- 0x4292F: `cmp [bp-6], si; jg` – übersprungen nur bei größerem Bestwert, Gleichstand nimmt den späteren Platz; erster
  freier Torhüter −1, weitere 1 (0x42913–0x4292C).

### (1) `a2eeba0` – Bezugsbewertung nach der Startaufstellung

Vertretbar, verdeckt keinen Fehler. `ReferenceRating` ist ein `[APPROX]`-Bezugspunkt („Aufstellung bei Spielbeginn“);
nach der Neuaufstellung 0x426BC gilt die Definition nur mit Neubewertung. Der Unterschied ist klein: Rating nach Regel
gegen `AutoFill` 64,33/64,67 (Seed 3), 65,57/65,74 (Seed 5), 66,08/66,42 (Seed 17) → unter 1 Stärkepunkt. Nur
Managervereine betroffen; Manager wechseln den Verein im Spiel nicht, ein späterer Bezugsbruch tritt also nicht auf.

### (4) Konfliktlösung

`git merge-tree 4e71df7 cb02f41` nachgestellt: Konflikte in `MatchEngine.Injuries.cs`, `SaveGame.cs`, `Squad.Lineup.cs`,
`Transfer.Pool.cs` und Tests. Je Datei gegen beide Eltern:
- `Transfer.Pool.cs`: `RollTrend(trendRandom ?? random)` (kader-transfer) vor `HiddenSkills.Roll` (zwei random(3, 12) aus
  `random`, wie die vorher verworfenen Züge) – Reihenfolge und Zufallsverbrauch beider Seiten erhalten.
- `MatchEngine.Injuries.cs`: `RefillLineup` delegiert an `Club.RefillLineup` (main), Doku der Spur; Verletzungsart neu.
- `Squad.Lineup.cs`: Stand der Spur (enthält `RefillLineup` mit Grenze 22 aus main) plus `if (ApplyAutoLineup()) return;`;
  `RefillLineup` wird nur für Managervereine aufgerufen (Heilung, Verletzung im Spiel).
- `SaveGame.cs`: Kette 22 → 30 aus main, 30 → 31 → 32 angehängt. `SeasonFlow.cs` ohne Konflikt, saisonwechsel-Stand erhalten.

### (5) Abdrücke

`RngTwinProbeTests`, `GameModeTests`, `SpielberechnungAnschlussTests`, `DebCupDrawFlowTests` am Head neu und mit Ursache
kommentiert; grün in allen Läufen.

### Erwartet offen (bestätigt)

U8 (Nationalteam Satz 5), Serien der Pokal-/Play-off-Spiele, Kurzmeldung K2 für die Aufstellungsmeldung, Inventur
A190/A111 mit `marker-werkzeug`.

### Akzeptierte `[APPROX]`

Bezugsbewertung nach der Startaufstellung; Meldung „Auto-Aufstellung“ unterliegt einer gleichzeitigen anderen Meldung;
Einstellung je Verein statt je Manager; Kaderplätze in der Reihenfolge `SortedPlayers`; Feldspieler aus der Torhütersuche
nur eingesetzt.

### Annahmen

Overlaybeginn 0x3FFE0 für Overlay 0x108 aus `docs/re-abdeckung.md` übernommen. Die Probe zu WS-01 und die Vergleichsläufe an
`main`/`cb02f41` liefen in temporären Arbeitskopien und sind nicht committet.
