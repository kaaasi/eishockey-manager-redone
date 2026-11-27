# Review: Integration Runde 8, Welle 3 (#268)

- PR: #268 · Base `99fa995b941d9e85234cebb0ca4c7dc00507a100` · Head `643488cd66d8820932e14209e86e458c534a60ef`
- Enthalten: #262 (leitstelle-r8), #267 (optionen-kalender-2), #261 (tabellen-original-2), #264 (statistiken-verein),
  #265 (nationalteam-2), #266 (transfer-original-vertrag), #263 (spielstaerke-manager-2). Keiner davon war einzeln
  reviewt, ihr Inhalt wurde mitgeprüft.
- Reviewer: Claude (frische Sitzung ohne Implementierungskontext, geplanter Lauf, unbeaufsichtigt), 31.10./01.11.2026.
  Die Prüfung war auf drei Teilprüfungen in eigenen Worktrees verteilt: A Spielstärke Managerteil, B Vertrag/Transfer,
  C Tabellen/Verein/Nationalteam/Einstellungen. Schema, Migration, R8-A02, Anschlüsse der Leitstelle und Dateigrenzen
  hat die Hauptsitzung selbst geprüft. Die beiden P2-Befunde sind dort zusätzlich am Disassembly nachgelesen.
- Checkout: Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer. Originaldaten aus
  `kaaasi/eishockey-manager-referenz` (`original/gamedata`, `sha256sum -c SHA256SUMS`: 167 OK, 0 Fehler). SDK 10.0.402 aus
  dem Release `claude-werkzeuge`.
- **Annahmen** (unbeaufsichtigter Lauf):
  - Priorität nach Wirkung auf Ergebnisse und Geld; Darstellung (Pixelvergleiche) nur über die vorhandenen Tests, nicht
    eigens nachgemessen.
  - Die als offen geführten Teile (Plan, Abschnitt „Integration Runde 8, Welle 3“) sind keine Befunde, nur bestätigt (3.4).
  - Befunde, die nur bei Erreichbarkeit im UI Wirkung haben, sind nach ihrer Wirkung im Core eingestuft (R8W3I-02).

## 1. Merge-Empfehlung

**Nein für genau diesen Head, solange R8W3I-01 (#269) und R8W3I-02 (#270) offen sind** (beide P2,
`zusammenarbeit.md` §6 Nr. 5). Beide lassen sich klein beheben. Alternativ kann der Nutzer sie als Folgearbeit für Runde 9
annehmen. Ohne diese beiden wäre es ein Ja.

Folgendes ist bestätigt:

- **`tools/pruefen.sh`:** Alle drei Läufe sind am exakten Head grün, inklusive `OriginalSavePatcherTests` (25/25 mit
  `EM_REQUIRE_GAMEDATA=1`).
- **Schema 15–17:** Echte Schema-14-Spielstände, auf der Base erzeugt, laden am Head. Geprüft wurden vier Stände: früh,
  Saisonmitte mit zehn abgelehnten Verträgen, vor der WM und Saison 3. Danach waren Sichern und Laden byte-gleich, und
  jeder Stand lief vier Saisonwechsel weiter.
- **Disassembly:** Gruppentabellen 0x30852, Reihenwerte 0x98F8–0x9C63, 0x952C, 0x87F7, 0x8EF2 sowie Forderung, Annahme und
  Sperren der Vertragsverhandlung stimmen an den geprüften Stellen mit dem Original überein. Ausnahmen sind die beiden
  P2-Befunde.
- **Anschlüsse der Leitstelle:** #193 und #195 wirken nur, wo sie sollen. Beide Tests werden bei Mutation rot.
- **R8-A02 / #260:** Die Eingrenzung ist tragfähig. Eigene Messung über 40 Seeds: Base 55,6 %, Head 60,0 %.

## 2. Befunde

Links auf den Head: `https://github.com/kaaasi/eishockey-manager-redone/blob/643488cd66d8820932e14209e86e458c534a60ef/…`

### P2

**R8W3I-01 (#269): Managerpaarung, Spielstand für 0x80CB/0x939A nach den Toren der Minute**

- **Stelle:** [`MatchEngine.Minutes.cs` Z. 431–432](https://github.com/kaaasi/eishockey-manager-redone/blob/643488cd66d8820932e14209e86e458c534a60ef/src/EishockeyManager.Core/MatchEngine.Minutes.cs#L431-L432)
- **Fehler:** Das Remake liest `report.HomeGoals/AwayGoals`. Diese Werte hat `Score` in derselben Minute schon erhöht
  (Z. 418–421, 476/478).
- **Original:** Es lädt die Ergebnisbytes einmal je Paarung in lokale Variablen:
  - `0x37E2D mov al,[bx-0x5f41]; mov [bp-0xd],al` und `0x37E62 mov [bp-0xe],al`.
  - Bis 0x380A1 schreibt es `[bp-0xd]/[bp-0xe]` nur noch beim Seitentausch (0x37FDC–0x37FEB). Nach den Abschlüssen
    `0x37F54`/`0x37F7F call 0x28:0x23c4` geschieht das nicht.
  - Übergeben werden diese Werte an `0x38011 call 0x28:0xb` (0x80CB) und `0x3805A/0x3806E call 0x28:0x12da` (0x939A).
- **Gegenbeispiel:** Ein Computerverein liegt vor Minute 59 mit 1:2 zurück und gleicht in Minute 59 aus. Das Original nimmt
  den Torhüter trotzdem heraus (own+1 == other mit dem alten Stand), das Remake nicht. Ebenso betroffen sind die Modi
  (2,1)/(1,2) ab Minute 40 und Einsatz/Angriff in 0x939A.
- **Wirkung:** Ergebnisse jeder Managerpaarung.

**R8W3I-02 (#270): Vertragsforderung nach dem Saisonende – Untergrenze statt Rückschreiben**

- **Stellen:**
  - [`Transfer.Contract.cs` `DemandThirds`](https://github.com/kaaasi/eishockey-manager-redone/blob/643488cd66d8820932e14209e86e458c534a60ef/src/EishockeyManager.Core/Transfer.Contract.cs#L50-L60)
    (`Math.Max(s, currentSalaryThirds)`)
  - Folge in [`Player.Contract.cs` `Decide`](https://github.com/kaaasi/eishockey-manager-redone/blob/643488cd66d8820932e14209e86e458c534a60ef/src/EishockeyManager.Core/Player.Contract.cs#L310-L330)
- **Original:** Nach dem Saisonende schreibt es Forderung und Laufzeit in den Kaderplatz, bevor es vergleicht
  (`0x4A9A1 les bx,[bp+6]; mov [es:bx+0x2A],dx; mov [es:bx+0x28],ax; … mov [es:bx+0xB],al`). Erst danach folgt
  `0x4A9B3 … cmp ax,[bp-0x16]`. Damit vergleicht es die Forderung mit sich selbst, und die Untergrenze wirkt nicht.
- **Gegenbeispiel:** Fall aus `Demand_FollowsOriginal` (9.000, Faktor 100, Rest 100, 2 Jahre, nach dem Saisonende,
  Alter 27, bisher 20.000 Drittel). Das Remake ergibt 20.000, das Original 14.400. Der Test schreibt also den falschen Wert
  fest.
- **Folgeabweichung:**
  - Eine kürzere Laufzeit gilt nicht mehr als „kürzer“ (0x4AC75).
  - Ein Gehalt unter der Forderung lehnt 0x49A3B ohne Zufallszug ab.
  - Das Remake meldet dagegen `TooShort` bzw. würfelt über `Accepts`.
- **Erreichbarkeit:** Im Core ist der Fall über `AfterSeasonEnd` (Phase `SeasonOver`) erreichbar. Ob die Verträge im UI in
  dieser Phase offen sind, habe ich nicht geprüft.

### P3

- **R8W3I-03:** Strafe in der Managerpaarung, `MatchEngine.Managed.cs` `PenalizedPlayer` (Z. 525, Doku Z. 498 „bis zu 151“).
  - Das Remake zieht bis zu 151-mal. Das Original prüft den Zähler vor dem Zug (`0x378ED jmp`; `cmp al,0x96; jnc` vor
    `0x378FC call 0x38:0x847`) und zieht höchstens 150-mal.
  - Nachgestellt mit zählendem `Random`: 151 Aufrufe.
  - Folge: ein Zufallszug mehr, wenn kein Spieler passt.
- **R8W3I-04:** Strafe ohne passenden Spieler (Rückgabe 100), `MatchEngine.Minutes.cs` Z. 354.
  - Das Remake legt trotzdem ein `MatchEvent(Penalty)` an.
  - Das Original überspringt Anzeige und Kettenstrafe (`0x35B33 cmp [bp-0x1],0x64` → `jmp 0x583b`); der Kommentar nennt
    nur die Kettenstrafe.
- **R8W3I-05:** Managerwert für Strafen, `MatchEngine.Live.cs` Z. 157–163.
  - Das Remake berechnet den Wert je Spiel frisch aus der Aufstellung. Das Original liest den gespeicherten Wert 0x974B, den
    nur 0x952C schreibt (0x9672–0x96E2).
  - Der Zweig „< 9 Spieler → 100“ ist toter Code (`nineOrMore` wird nie false).
  - Die Abweichung ist nicht als `[APPROX]` markiert.
- **R8W3I-06:** Nationalteam des Bundestrainers.
  - Das Original ruft 0x378C5 für jeden Verein mit Feld 0x16 ≠ 0 auf (0x35AB8–0x35B2C).
  - Das Remake hat für das Nationalteam keinen Kader, die Strafzüge fehlen dort.
  - Das `[APPROX]` in `LiveTeamOf` nennt nur Energie und Reihen.
- **R8W3I-07:** Versicherung im Vertragsdialog, `ContractsScreen.cs` Z. 152 (`_insured = !_insured`).
  - Das Original schaltet nur um, wenn +0x2C Bit 31 frei ist und der Spieler nicht verletzt ist (`0x4AA74 … and ax,0x8000;
    jnz`).
  - Derzeit gibt es keinen Ausnutzungsweg, weil keine Leistung ausgezahlt wird. Der Fall fehlt aber unter „Noch offen“ in
    `docs/transfer.md`.
- **R8W3I-08:** Gehalt in ganzen DM (`Player.Contract.cs` Z. 252–262, 292).
  - Die Forderung in Drittel-DM wird abgerundet, bis zu 2/3 DM je Monat.
  - Das liegt am Datenmodell, ist aber nicht als `[APPROX]` markiert.
- **R8W3I-09:** `Player.Contract.cs` Z. 95 setzt `NegotiationLock = 0` beim Saisonwechsel.
  - Dieses Rücksetzen habe ich im Original nicht gefunden (Verdacht, nicht bestätigt). Es ist nicht als `[APPROX]` markiert.
- **R8W3I-10:** Testlücken.
  - `NegotiationResult.Unfair` (`Player.Contract.cs` Z. 283) ist in keinem Test mehr abgedeckt, seit `ContractTests` aus
    `SquadTests.cs` entfallen sind.
  - Der gesperrte Menüweg vor der Ernennung im Modus 1 bei alten Spielständen von Tag 0 ist seit dem Diff in
    `WorldOnlyMenuTests.cs` ungetestet.
  - `Forfeit_LeagueMatch_UpdatesDayForm` prüft nur den Sieger: Beim Verlierer (47–50) bliebe ein unveränderter Wert 50 grün.
- **R8W3I-11 (Prozess):** Plan und Code passen beim #195-Anschluss nicht zusammen.
  - Der Plan nennt als einzige Ausnahme für Aufrufe der Leitstelle den Anschluss #193.
  - Der #195-Anschluss in `SeasonEndSummaryScreen.cs` kam laut Kommentar aus einem Vorschlag in PR #265 und liegt in
    keiner Spurdatei. Sachlich ist er richtig (siehe 3.2). Im Plan sollte er als zweite Ausnahme nachgetragen werden.

**Verdacht, nicht bestätigt:**

- 0x952C ruft zuerst `0x9567 call 0x149d:0x8e`. Ist das eine automatische Aufstellung, schreibt das Original die
  Länderwerte auch ohne eigene Aufstellung neu. Dann wäre das `[APPROX]` in `PlayFriendly` („bleiben stehen“) eine
  echte Abweichung.
- 0x30852 nimmt die Tore vor dem Eintragen modulo 30 (0x30906/0x30916). Ob das Remake das für die Marke +30
  (Verlängerung) braucht, ist offen.

## 3. Ausgeführte Prüfungen

### 3.1 `tools/pruefen.sh` (alle drei Läufe, exakter Head, Release, Warnungen = Fehler)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1403 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1172 | 0 | 207 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1172 | 231 | 0 |

- Commit `643488cd66d8820932e14209e86e458c534a60ef`, SDK 10.0.402, Ergebnis „bestanden“.
- Zusätzlich lief `dotnet test tests/EishockeyManager.Formats.Tests --filter OriginalSavePatcher` mit
  `EM_REQUIRE_GAMEDATA=1`: 25/25, 0 übersprungen.

### 3.2 Gezielte Proben (eigene Worktrees, Probedateien nicht im Repo)

- **Schema 14 → 17:**
  - Auf der Base (`SchemaVersion` = 14) habe ich mit Originaldatenbank, Verein 42 und Seed 3 vier Stände gesichert:
    Tag 10; Tag 66 mit zehn `RejectedOfferDay = 18`; Tag 216 `ClubSeasonOver` vor der WM; Saison 1995.
  - Am Head habe ich jeden Stand geladen (Erfolg), sofort erneut gesichert und geladen (byte-gleich), dann
    `Continue`/`StartNextSeason` über vier Saisonwechsel gespielt, jeweils mit `KeepSquad`. Danach wieder gesichert und
    geladen. Alle vier grün, Manager nicht entlassen.
  - Alte Ablehnungen werden zu Sperre 0. Das ist so dokumentiert (`[APPROX]` in der Migration) und akzeptiert.
- **#193 `PlayFriendly`:**
  - Mutation, Zuweisung entfernt: `Friendly_SetsGermanyRatingsFromCoachLineup` wird rot.
  - `_germanyRatings` wird nur gelesen, wenn Deutschland **keinen** Bundestrainer hat, sowie in der Anzeige. Wettbewerbsspiele
    mit Bundestrainer nutzen `coach.Lineup()`, #193 ändert sie also nicht.
- **#195 `SeasonEndSummaryScreen`:**
  - Mutation, `if (Game.WorldOnly) Game.Continue();` entfernt: `Mode1_AfterSeasonChange_FirstHaltIsFirstWorldCupDay`
    wird rot.
  - Der Anschluss greift nur bei `Mode == WorldOnly`. `SkipToWorldCup` springt nur vor dem ersten WM-Tag.
- **R8-A02 / #260:** Eigene Probe nach dem Muster von `RunStockProbe` über die Seeds 1–40 (4 Aktien, 4 Saisonwechsel):

  | Stand | Basiswerte gewachsen |
  |---|---:|
  | Base | 89/160 = 55,6 % |
  | Head | 96/160 = 60,0 % |

  - Die Abweichung zur Messung der Leitstelle (64 % → 56 %) liegt bei n = 160 innerhalb von etwa 1,5 Standardfehlern. Die
    Schlussfolgerung „Streuung, kein Fehler“ ist damit tragfähig.
  - Die neue Schwelle (≥ 40 % über zwölf Seeds, also 48 Werte) liegt etwa 2,5 Standardfehler unter dem Erwartungswert. Ein
    künftiger Fehlalarm nach einer Änderung des Zufallsverbrauchs ist selten, aber möglich.
  - Ein Totalausfall des Wachstums (#260) würde sicher erkannt.
  - `FinanceWerbungTests.ActiveManager_NegotiatesOptions_AcrossSeeds` (einer aus sechs Seeds) ist ebenfalls robust. Je Seed
    prüft die Probe weiter die Regeln.
- **Mutationen Transfer (B):** Jede Mutation macht Tests rot (Filter `TransferOriginalVertrag`):

  | Mutation | rote Tests |
  |---|---:|
  | 122 → 121 in der Forderung | 4 |
  | 110 → 111 in der Annahme | 2 |
  | Sperrlösung random(0,2) → (0,3) | 1 |
  | `Math.Max` entfernt | 1 |

- **Gezielte Läufe (C):**
  - Core `TabellenGruppen|StatistikenVerein|Nationalteam2`: 44/44.
  - UI `Nationalteam2|IntegrationRunde8Welle3Ui|TableScreenOriginal|StatistikenVereinScreen|SettingsScreen|WorldOnlyMenu`:
    53/53, 0 übersprungen.
- **Spielstärke (A):** `SpielstaerkeManager2Tests` und `IntegrationRunde8Welle3Tests` grün. 151 Züge in `PenalizedPlayer`
  mit zählendem `Random` nachgestellt.

### 3.3 Am Disassembly bestätigt (`ndisasm -b16`, Dateiadressen)

- **Gruppentabellen 0x30852** (vollständig):
  - Punkte 0x303D9: Sieger +2, sonst beide +1.
  - Vergleich 0x30CB6–0x30D91: Punkte vorzeichenlos, Tordifferenz mit Vorzeichen, dann Tore; bei vollem Gleichstand
    wird getauscht.
  - Sortierschleife 0x30E1E–0x30E51 wie `LeagueTable.ExchangeSort`.
  - Ausgangsreihenfolge Endrunde 0x305D4 und Aufstiegsrunde 0x30511.
  - Weiterkommen in Europapokal, WM und Aufstiegsrunde ist damit konsistent. Die frühere `[APPROX]`-Regel „obere Liga
    hat Vorrang“ entfällt zu Recht.
- **Serien 0x50283:** nur Liga (7) und WM-Gruppe (8), Prüfung bei 0x5030D. Stichprobe: Spalten „ohne Gegentor/ohne
  Torerfolg“.
- **WM-Punkte 0x3B216:** 9 − Platz, nur für Platz < 9.
- **Reihenwerte 0x98F8–0x9C63:**
  - Energiefaktor e/2 + 50 und Halbbytes von +0x19.
  - −15 bei t > 15, Länderspiele/8 (höchstens 99).
  - Faktoren 80/140, `idiv` durch 3.
  - Unterzahl, ohne Torhüter, Minute ≥ 58, Begrenzung auf 0…100.
- **0x952C:** d/5 mit Vorzeichen, +20, Begrenzung auf 0…40.
- **0x87F7:** Reihenfolge der Züge (0,Einsatz) → (0,5) → (5,15), Begrenzung auf 1…99.
- **0x8E5F/0x8EF2/0x91FF:** Reihenwechsel, Ring mit 250 Schritten.
- **0x80CB:** Modi, Schwellen 20/15, Torhüter in den Minuten 58/59/68/69. Der übergebene Spielstand weicht ab, siehe R8W3I-01.
- **Ablauf 0x37F9B–0x380A1:** Seitentausch random(0,1).
- **Forderung 0x4A81B–0x4A9F2** (bis auf R8W3I-02):
  - S = Wert · Faktor / 100.
  - W = (U/6 + 122) · 8/10, dann S · (W + 4)/100.
  - Faktor nach dem Saisonende: (152 − Alter)/100.
- **Annahme 0x49A3B:**
  - Kurzwege 120 % / 105 %.
  - Genau drei Zufallszüge in Bereich und Reihenfolge des Codes.
  - Grenze 0x186A0.
- **„Mach' ich!“ 0x4AB66–0x4AD7A:**
  - Reihenfolge TooLong → Annahmemerker → TooShort → 0x49A3B.
  - Sperre random(10,18) bei jeder Ablehnung.
- **Sperre +0x18:** Abfrage 0x4A704, Abbau täglich 0x76F6.
- **Gebotssperre +0x1A:** gesetzt bei 0x472F4, geprüft bei 0x47140, gelöst bei 0x71CC (nur im Zug von Manager 0).
- **Geld:** `Buy` bucht nur über `GameState.Book`; keine Doppelbuchung gefunden.

### 3.4 Erwartet offen – bestätigt

Laut Plan sind die folgenden Teile offen, und sie sind es tatsächlich:

- **Über-/Unterzahl-Zähler:**
  - `StatisticsClub.CountPowerPlayGoal` wird nur aus Tests gerufen, für k = 4/5 fehlt die Schnittstelle.
  - Der Bildschirm „Verein“ zeigt in echten Spielen 0 bzw. „UNTERZAHL ERFOLG 100 %“.
  - Das ist ehrlich als `[APPROX]` „Anschluss offen“ markiert.
- **spielstaerke-manager Teil 3:** 0x386D1 (Torschütze) und Verletzungen 0x38B8F sind nicht umgesetzt, 0x386D1 wurde nicht
  geprüft.
- **transfer-original:**
  - Verhandlung am Saisonende 0x27D05, Versicherungsprämie und unaufgeforderte Angebote sind nicht angeschlossen.
  - Damit hängt die Wirkung von R8W3I-02 im UI.
- **nationalteam:** Die Schreibstelle der WM-Platzierungen ist offen. Die Migration beginnt mit einer leeren Liste
  (`[APPROX]`).
- **tabellen-original:** Startordnung ab Saison 2 ist offen. `StartOrder` (Tausch 1/3) ist nur an der Doku zu 0x43A08
  gelesen.
- **optionen-kalender:**
  - Die Einstellungen sind rein lesend, und beim Öffnen ändert sich nichts.
  - Der Pixelvergleich (Bild 48) ist offen.

### 3.5 Dateigrenzen (Plan Welle 3)

Je Spur-Merge-Commit `git diff --name-only <c>^1 <c>`: Die Spuren halten ihre Grenzen ein.

| Datei | Änderung | Bewertung |
|---|---|---|
| `GameState.Integration.cs`, `GameState.cs` (statistiken-verein) | je ein Aufruf | gemeinsame Dateien, minimal |
| `Player.Value.cs` (statistiken-verein, SW-02) | nur Kommentar | in Ordnung |
| `CupsWorld.cs` | ein Aufruf | in Ordnung |
| `Squad.Tactics.cs` (nationalteam-2, R8-B04) | optionaler Parameter, mit Fundstelle | in Ordnung |

`BoerseStartTests`/`FinanceWerbungTests` wurden nur von der Leitstelle geändert. Abweichung: R8W3I-11.

### 3.6 Nur stichprobenartig oder nicht geprüft

- **Nicht nachgelesen:**
  - 0x939A nur in Stichproben.
  - 0x386D1, die Paarungsansicht 0x2FC2F und die Automatik ohne Bundestrainer (16f262d) nicht gegen das Disassembly.
  - Bildschirm „Verein“ 0x2D5AE: nur Zustandsreinheit und die Serien geprüft, nicht Layout und Texte.
  - Die Rekordserien 0x50CDB.
  - Prüfung beim Klick in die Liste 0x4015D, Regel „Geld vor unfair“ 0x474F0.
- **Nur über die vorhandenen Tests geprüft, nicht nachgemessen:** Pixelvergleiche aller Bildschirme.
- **Neu gesetzte Referenzabdrücke** (RngTwin, SpielberechnungAnschluss, GameMode, SeasonFlow, DebCupDrawFlow):
  - Plausibel durch den geänderten Zufallsverbrauch der Managerpaarungen. Paarungen ohne Manager sind unverändert
    (`SpielstaerkeManagerTests`, `SpielberechnungMinutenTests` nicht im Diff).
  - Eine Gegenprobe, die den alten Verlauf erhält, gibt es nicht.
  - `ManagedSquads.PlayAtDatabaseStrength` ersetzt in mehreren Tests die echten Kader. Diese Tests prüfen das Zusammenspiel
    mit echten Kadern nicht mehr. Eine Abschwächung, die einen Fehler verdeckt, habe ich nicht gefunden.

### 3.7 Akzeptierte `[APPROX]`

- Migration: alte Ablehnung → Sperre 0; WM-Platzierungen leer; Serien und Zähler 0.
- `PlayFriendly` ohne Aufstellung: Länderwerte bleiben stehen (unter Vorbehalt des Verdachts in 2).
- `ContractTurnFactor`: ein Faktor je Tag für alle Manager statt je Managerzug (0x44E3F).
- Uninitialisierter Zähler bei der vierten Reihe (0x8EF2), „MANUELL“ in 0x939A.
- Pokal-Schalter in den Einstellungen.
- Über-/Unterzahl „Anschluss offen“.
