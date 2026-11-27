# Review PR #1006 (Spur saison-geld: T03, F06, F08, K11, K13)

Reviewer: Claude Code (Opus 5.5), eigene Sitzung ohne Implementierungskontext, geplanter Lauf (Sitzung nicht unter den drei im PR
genannten). Geprüft: Head `2be69963d1d02e7288301cf5d9635df0c8ea27e4` gegen Base `90d8394` (= aktueller `main`, Merge-Base gleich),
Checkout `git rev-parse HEAD` = Head, `git status --porcelain` leer. Originaldaten aus `kaaasi/eishockey-manager-referenz`.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Kein Befund über P3:

Alle drei Läufe von `tools/pruefen.sh` sind grün, die Rückschaltprobe bestätigt die PR-Beschreibung, und jede Vorgabe aus
`re-abweichungen.md` (T03, F06 mit RA-01 und Klickprüfung, F08, K11, K13) ist so umgesetzt, wie sie dort steht. Die Migration
64 → 65 lädt Stände mit Schema 63 und 64; ein Stand vom 13. April aus dem alten Code zieht nicht doppelt.

Der K13-Randfall ist **nicht** `beleg-strittig`: Die Widerlegung der Spur stimmt mit der Disassembly überein (B4). Die vier P3
sind Testlücken, ein Kommentar und eine Folgeaufgabe außerhalb der Vorgabe. Sie blockieren den Merge nicht.

## 2. Befunde

**SG-01 (P3, Folgeaufgabe außerhalb der Vorgabe) – Hauptweg: Tagesabschluss nicht je Manager verschränkt.** Das Original ruft
0x3ED01 im Hauptweg je Manager in einer Schleife auf: `[0xA70E]` läuft von 0 bis `[0x386]`, mit `call 0x100:0x4D` bei Datei
0x5522B. Pro Manager laufen also Baufirmen (11 Würfe), Zins und Werbeangebote, erst dann kommt der nächste Manager. Das Remake
rechnet erst die Baufirmen aller Manager, dann die Werbeangebote aller Manager
([`GameState.Manager.cs:161–163`](https://github.com/kaaasi/eishockey-manager-redone/blob/2be69963d1d02e7288301cf5d9635df0c8ea27e4/src/EishockeyManager.Core/GameState.Manager.cs#L161-L163)).
Seit A512 würfelt jeder Manager jeden Tag elfmal. Mit zwei oder mehr Managern ist der Strom deshalb an jedem Angebotstag anders
als im Original. Die Würfe sind gleich verteilt, nur die Reihenfolge weicht ab. Die Stelle hat keinen Marker.
Beleg: `ndisasm -b16 -o 0x55200 -e 0x55200 <(head -c 0x55250 gamedata/em.exe)`. Die Abweichung gab es schon vor dem PR (die
Angebote waren schon damals nicht verschränkt). Die Vorgabe F08 verlangt die Verschränkung nur für die Sommerpause (A526).
Vorschlag: nach `pruefung-karte` bzw. als Vorgabe für die nächste Runde (wie `RunSummerDay` ein `DayClose(date, i)` je Manager).
Bis dahin `[APPROX]` an der Aufrufstelle.

**SG-02 (P3, Testlücke) – T03 (3): Die Stromlage „Wanderung vor den 40 Würfen der Länder“ prüft kein eigener Test.** Probe in
einer temporären Kopie: `DriftSeasonEnd(record)` hinter `worldCup.MixForNextSeason`/`MixThrows` verschoben
([`SeasonFlow.cs:424`](https://github.com/kaaasi/eishockey-manager-redone/blob/2be69963d1d02e7288301cf5d9635df0c8ea27e4/src/EishockeyManager.Core/SeasonFlow.cs#L424)).
Ergebnis: alle 7 `SaisonGeldT03Tests` grün, nur die 6 Abdrücke in `RngTwinProbeTests` rot. Die Vorgabe nennt genau diesen roten
Test („Stromstand“). Ergänzen: Stromstand nach `FinishSeason` = Stand vor der Wanderung + Wanderung + 40 Länderwürfe (`Copy`
wie in `MarketPart_…`).

**SG-03 (P3, Kommentar) – Migration: „ein neuer Poolaufbau würfelt ihn neu“ tritt im laufenden Spiel nicht ein.**
[`SaveGame.Migration.SaisonGeld.cs:8`](https://github.com/kaaasi/eishockey-manager-redone/blob/2be69963d1d02e7288301cf5d9635df0c8ea27e4/src/EishockeyManager.Core/SaveGame.Migration.SaisonGeld.cs#L8).
`BuildPool` läuft nur beim Spielstart oder bei einem leeren Pool (`Transfer.cs:262`, `Transfer.Pool.cs:937–942`). Der
`PoolSlot`-Setter reicht „keinen Wert“ an den nächsten Spieler weiter. Probe (B3): Ein Stand mit Schema 64 hat nach 4
Saisonwechseln noch keinen gewürfelten Rollenwert. Das ist durch das erwartete `[APPROX]` gedeckt (Mitte 0/33/83 für alte Stände,
dauerhaft). Nur der Satz im Kommentar führt in die Irre.

**SG-04 (P3, Doku) – `re-abweichungen.md` K13 „Randfall“ ist überholt** (B4); Nachtrag durch Leitstelle bzw. `pruefung-karte`.

Keine weiteren Befunde. Was ausdrücklich geprüft wurde:

- **Dateien außerhalb der Grenzen:** `FinanceMonthly.cs` (+8), `GameState.Manager.cs` (+6), `AdvertisingScreen.cs` (+7),
  `SaveGame.cs` (+3), `Transfer.Pool.cs` (+36, K11 nötig), `OriginalImport*.cs` (je +1), `HokCompareClasses.cs` (1 Zeile),
  `Transfer.cs`/`Transfer.Listed.cs` (Helfer, `expire: false`). Alles minimal und im PR begründet.
- **Fremde saatabhängige Tests** (`RngTwinProbeTests`, `KaderplatzWerteFixTests`, `AbgleichFixPoolTests`,
  `AbgleichOriginalPaareTests`, …) im Diff Stück für Stück geprüft: Jede Änderung hat eine Begründung mit Fundstelle, Saatwechsel
  tragen die Liste der geprüften Saaten, Abdrücke sind als „kein Originalsollwert“ markiert, kein neuer Originalsollwert ist
  eingetragen. Die neuen optionalen Einträge in `AbgleichOriginalPaareTests` (HEDOS, RATINGEN, FRANKFURT, KÖLN) sind als
  Zufallsfelder begründet, die Prüfsaat ist grün. R15-E2 = C und R21-D2 = A sind erfüllt.

## 3. Ausgeführte Prüfungen

**B1 – `tools/pruefen.sh` (alle drei Läufe, Head `2be6996`, SDK 10.0.402): bestanden.**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3970 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2805 | 0 | 649 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2806 | 1164 | 0 |

**B2 – Rückschaltprobe.** Aufbau: `src/` von Base `90d8394` mit den neuen Testdateien `SaisonGeldT03Tests` (ohne
`SeasonDriftOrder_…`, das ohne die neue Methode nicht kompiliert) und `SaisonGeldTeil2Tests`. Ergebnis: 13 rot, 2 grün.
Grün bleiben `BoardOffer_TooFewFreeFields_Rejected` und die passive Mehrsaisonprobe aus Teil 2; das deckt sich mit der
PR-Beschreibung. `SaisonGeldTeil3Tests` kompiliert gegen die Base nicht (neue API), deshalb Mutationen in einer Kopie des Heads
(Filter `SaisonGeld*`, gegebenenfalls plus Nachbartests):

| Mutation | rot |
|---|---|
| M1 `RoleValue`-Getter immer Mitte | PoolBuild (2), PoolSlot, CreditSkaters, Export, RoundTrip (2) |
| M2 keine Weitergabe am Poolplatz | PoolSlot, PoolBuild (2), Mehrsaisonprobe K11 (passiv/aktiv) |
| M3 Migrationseintrag `[64]` entfernt | `SaveLoad_…Schema64Migrated`, `SchemaOneSave_…`, `SchemaTwoSave_…` (2) |
| M4 Wanderungsgrenzen nach alter Liga | Mehrsaisonprobe T03 (passiv/aktiv) |
| M5 Wanderfolge = `ClubRecordOrder()` | `SeasonDriftOrder_PromotedTakeSlotOfRelegatedPartner` |
| M6 Satz 0 nie gesperrt (K13) | `Draw_AsInOriginal` (3), `AddManager_ExcludesCupPlacesAndRecordZero`, `AddManager_DrawnPlace_…` |
| M7 Verfallswurf nur bei Gebot (A829) | beide `MarketPart_…`, Mehrsaisonprobe Teil 2 (aktiv) |
| M8 Schutz „schon gezogen“ entfernt | keiner der 130 Tests, aber Probe B3 rot |
| M9 Wanderung hinter Ländermischung | nur `RngTwinProbeTests` (6), siehe SG-02 |

**B3 – Spielstände über die Versionen (eigene Probe, nicht committet).** Der alte Code (Base) schreibt zwei Stände mit Schema 64:
einen am 13. April (`ClubSeasonOver`, Saat 5, zwei Manager) und einen am Bildschirm „Saisonende“. Den Oberliga-Zug hatte der alte
Code schon gezogen (26, 32 → 40, 38). Der Head lädt beide Stände. Am 29. April zieht er nicht erneut, die Paare bleiben gleich.
Speichern(Laden(Speichern)) ist bytegleich. Es folgen 4 weitere Saisonwechsel ohne Fehler, das Geld bleibt positiv, ein gewürfelter
Rollenwert entsteht nicht. Ohne den Schutz `LeftOberliga.Count == 0` (M8) wird die Probe rot.

Schema 63 → 65 lädt über `AufstellungK07Tests` (Zeile 183, Kette 63 → 64 → 65) und die Migrationsketten in `GameModeTests`.

**B4 – K13-Randfall, Gegenprobe der Disassembly.**

Die Bytes `F4 5C` stehen nur beim Lesen in 0x45CD0 (Datei 0x45CD7). Alle Schreiber nutzen die Tabelle `[bx+0x5CF3]` mit dem
Wettbewerb `[bp+6]` als Index: `mov …,0` bei Datei 0x341CC, `mov …,1` bei 0x346B3, `inc` bei 0x348DB und 0x34AE3, `dec` bei
0x349E9 (alle in 0x341AB bzw. 0x3489D); dazu die Ein- und Ausgabe des Spielstands (Datei 0x5C224, 0x5CC15). Einziger naher Aufrufer
von 0x3489D ist Datei 0x34F64; die Schleife dort (Datei 0x34F10–0x34F71) läuft mit `[bp−1]` = 0, `[bp−2]` = 1, also nur für
Wettbewerb 0. 0x45CD0 zählt bis `[0x3B1 + [0x5CF4]]`. Ergebnis: DGROUP 0x5CF4 ist im Spiel nur 0 oder 1. Die Lesart des Berichts
(„ab Runde 3 …“) ist widerlegt, die Umsetzung der Spur passt dazu.

**B5 – Skripte.** `f06_bande.py`, `t03_markt.py` und `k13_platzwahl.py` laufen fehlerfrei. Ihre Ausgabe stimmt mit dem Bericht und
den Testfällen überein. Disassembly der Annahme bei Datei 0x2BAD9: Die Feldsuche beginnt für jede Bande wieder bei Feld 0
(`[bp−0x2A]` = 0). Frei heißt Betrag 0 oder +6 Bit 0x80. Das passt zu `FreeBoards().Take(n)`, weil neue Verträge nicht kündbar sind.

**Code gelesen:** T03 `DailyRunMarket` (Verfall, Sperre, `Random16(0, 3)` je Platz, dann Löschen) und `ExchangeOberliga`
(`Random16(36, 41)`); Reihenfolge am 29. April wie im Original (Jugend, Zwangsabstieg, Oberliga, Ewige Tabelle, Wanderung, Länder,
Ligen). F06 `AcceptOffer`/`CurrentOffer` und Einnahme je Feld; F08 `ForgiveBuilders`, `RunSummerDay`, Bau ohne Ziel; K11 Feld,
Weitergabe, Export, Import, Statistik.

**Akzeptierte `[APPROX]`** (erwartet offen, bestätigt): `Player.RoleValue` – Spieler ohne Poolplatz und Stände vor Schema 65
bekommen die Mitte 0/33/83 ohne Wurf, dauerhaft (SG-03). A463 – die Zufallszuteilung ist eine Erweiterung des Remakes (Datei
0x2964C: im Original wählt jeder Manager selbst). Ebenfalls akzeptiert: alte Stände vom 13. bis 29. April behalten ihren Zug (B3),
der Saisonende-Stand verliert eine Wanderung, `AdvanceBids` nur noch für Werkzeuge, Börse einmal je Tag (#188).

**Annahmen (nicht geprüft):** Fernaufrufe (`call far`) auf 0x3489D oder 0x341AB aus anderen Segmenten habe ich nur über das Muster
`E8` gesucht. Videobilder wurden nicht verglichen, weil der PR keinen Bildschirm ändert. Die Bedingung in `AdvertisingScreen`
habe ich nur gelesen.
