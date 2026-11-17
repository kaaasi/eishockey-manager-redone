# Review PR #931 – Spur transfer-pool (A391, A713, #897, A864, A731)

Geprüft: Base `c58e738`, Head `9587ab8` (`git rev-parse HEAD` = `9587ab82f69021eb14806df3a61887abc7496b11`, `git status
--porcelain` leer). Reviewer: Claude (geplanter Lauf, Sitzung ohne Implementierungskontext). Erstreview, alle drei Läufe.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head, wegen P2-1. A391, #897 `ChangeList`, A864 und A731 halten der Nachprüfung am Original stand. Jede
gezielte Mutation macht die neuen Tests rot, und die drei Prüfläufe sind grün. A713 stimmt nur bis zum ersten Oberliga-Austausch.
Danach schrumpft Gruppe 3 im Remake, und die Abweichung ist nicht als `[APPROX]` markiert. Die Behebung ist klein (Fixlauf, nur
P2/P3 → Delta-Prüfung der Leitstelle). Hinweis: `main` steht inzwischen auf `41ad124` mit Schema 57, ein Rebase ist vor dem Merge
nötig.

## 2. Befunde

**P2-1 – A713: Gruppe 3 = Index 36 … 42 „ohne Liga“ gilt nach dem Oberliga-Austausch nicht mehr und ist nicht markiert.**
[Transfer.PoolDistribution.cs:260](https://github.com/kaaasi/eishockey-manager-redone/blob/9587ab82f69021eb14806df3a61887abc7496b11/src/EishockeyManager.Core/Transfer.PoolDistribution.cs#L260)
- Das Original rechnet mit DGROUP 0x7C6/0x7C7 = `0,11,12,23,24,35,36,42`. Das habe ich nachgelesen: Datei 0x16DD0 + 0x7C6, Vergleich
  bei 0x07D1A/0x07D36.
- Die Gruppen sind also **Satzpositionen** nach der Umordnung 0x35EDD. Die Positionen 0 … 35 sind die Ligavereine, und die Positionen
  36 … 42 halten immer **7** Vereine ohne Liga.
- Das Remake ordnet die Vereine nicht um (A715). Es nimmt den festen Vereinsindex 36 … 42 und davon nur die Vereine ohne Liga.
- Ein Verein, der die Oberliga verlässt und einen Index < 36 hat, fällt so in keine Gruppe (−1). Ein aufgestiegener Verein mit
  Index 36 … 42 verlässt Gruppe 3, und niemand rückt nach.
- Der Kommentar markiert als `[APPROX]` nur die Gruppen 0 … 2. Gruppe 3 gilt dort als Indexbereich wie im Original.
- Probe: Testdatei `ReviewProbeTests` im temporären Worktree, `RngTwinProbeTests.Run(game, false, …)`, je Saisonwechsel
  `PoolGroup` aller Vereine < 48 mit `League == null` ausgewertet. Befehl:
  `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.Core.Tests -c Release --filter FullyQualifiedName~ReviewProbeTests`.
  - Saat 3: 1994 Gruppe 3 = `[36,39,40,41,42]`, ohne Gruppe `[25,27,43…47]`; 1995 Gruppe 3 = `[39,40,42]`; 1997 Gruppe 3 =
    `[38,40,42]`, ohne Gruppe `[25,26,27,29,43…47]` mit **10 Poolplätzen ≤ 179**. Die Verteilung 0x07C7E vergibt diese Plätze nie
    mehr.
  - Saat 11: 1997 Gruppe 3 = `[37,39,40,42]`, ohne Gruppe `[23,27,33,43…47]`.
- Der Mehrsaisontest prüft das nicht: Er zählt nur das Altern und die Verletzungen.
- Vorschlag: Gruppe 3 nimmt die ersten 7 Vereine ohne Liga (Index < 48) in einer festgelegten Reihenfolge. Das wird als
  `[APPROX]` markiert, weil die Umordnung 0x35EDD fehlt (A715). Dazu kommt ein Test über ≥ 2 Austausche: Gruppe 3 hat immer 7
  Vereine, und kein Poolplatz ≤ 179 steht bei einem Verein ohne Liga und ohne Gruppe. Alternativ entscheidet die Leitstelle, die
  Abweichung nur zu markieren, mit Verweis auf A715.

**P3-1 – Saatwechsel ohne Begründung in `AbgleichFixLineupTests`.**
[AbgleichFixLineupTests.cs:98](https://github.com/kaaasi/eishockey-manager-redone/blob/9587ab82f69021eb14806df3a61887abc7496b11/tests/EishockeyManager.Core.Tests/AbgleichFixLineupTests.cs#L98)
- Die Saat 2 → 4 wurde als einzige der angepassten Stellen ohne Kommentar geändert.
- Probe: Saat 2 am Head wieder eingesetzt → `File44To46_HandLineup_KeepsGaps_FormLikeOriginal(seed: 2)` ist rot mit „Expected 11,
  Actual 10“. Die Verletzung außerhalb des Spiels hinterlässt eine Lücke, die das Original in FILE46 nicht hat.
- Das ist ein Zufallsfeld und passt zu R21-D2 = A. Es fehlt aber die Herkunftszeile wie bei den anderen Tests.

**P3-2 – Ergänzung zur Lockerung in `AbgleichOriginalPaareTests`.** Das neue optionale Feld
`Clubs[18 SC MEMMINGEN].TeamValues.AttackCondition` (Chance) ist begründet und betrifft nur die Saat 1993. Die Leitstelle sollte es
in die Liste der Zufallsfelder aufnehmen und am Rundenende prüfen, ob es bei weiteren Stromänderungen fest wird. Das ist kein
Mangel des PRs.

Geprüft ohne Befund:
- **A391:** 0x274A7 `inc [+0x14]` in der Schleife über alle Poolplätze, Sprung nur bei `[0xA70E] ≠ 0` (0x2748E–0x27494).
- **#897 `ChangeList`:**
  - 0x494EF setzt `[0xA70E] := 4`; der Markt ist Kader 4, 0x5DC3 + 4 · 0x618 = 0x7623.
  - 0x497A1 schreibt +0x28 = Wert(Gehalt) in Drittel-DM; `/3` ist die Umrechnung in DM wie bei `Contracts.ValueSalary`.
  - Der Gehaltsweg zieht keinen Zufall (`PlayerValue.Compute`, nur Preis mit Angebot).
- **A864:** 0x07421–0x07548 gelesen: x vor der Prüfung, +0x0D = 0 und +0x09 = 0 (ganzes Byte), `random(0, 30·[0xED8] + x + 100)`.
  Bit 0x80 sitzt am Marktplatz (0x07358, Platz 0x7623 + i · 0x34), und Marktspieler sind im Remake aus dem Kader entfernt
  (`Offer` → `RemovePlayer`). Die Prüfung von `UnsolicitedOffer` reicht deshalb.
- **A731:**
  - Der Wurf liegt im Klickzweig: 0x47334 testet Bit 0x80, dann folgen 0x47347 `call 0x9D36` und 0x47353–0x47379.
  - `TransferScreen.Enter` ist entfernt. Das Öffnen zeigt kein Fenster und würfelt nicht, der Wurf läuft nur über
    `TransferMarket.RollBid` (Kern).
  - Die UI-Änderung ist minimal (−`ShowNextBid`, +`OpenBid`).
  - `Accept` würfelt für Aufrufer ohne Klick nach, das ist als `[APPROX]` markiert.
  - Gebote werden nicht gespeichert (unverändert), deshalb braucht es kein neues Schemafeld.

## 3. Ausgeführte Prüfungen

Gegenbeispiele erneut ausgeführt (Disassembly `ndisasm -b16` an `em.exe`, Tests am Head): A391 0x274A7 ✓, A713 0x7C6/0x7C7 = 36 … 42
✓ (Index; siehe P2-1), #897 0x497A1 ✓, A864 0x07462–0x07548 ✓, A731 0x47347 ✓ (ein Wurf je Angebot:
`A731_RollBid_IsOncePerOffer_AndConsumesRandomOnlyOnce` grün).

`tools/pruefen.sh` (alle drei Läufe, SDK 10.0.402, Commit `9587ab82`):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3646 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2547 | 0 | 600 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2548 | 1098 | 0 |

Gegenproben (temporärer Worktree am Head; `TransferPoolSpurTests`, UI: `TransferVertraegeKomplettTeil5ScreenTests` +
`WaveBReviewScreenTests`):

| Mutation | Ergebnis |
|---|---|
| M1 `AgeLostPlayers`-Aufruf entfernt | rot 3/11 (A391 + Mehrsaisonprobe beide Fälle) |
| M2 `NoLeagueLast = 47` | rot 1/11 (A713) |
| M3 `ChangeList` Gehalt = 0 | rot 1/11 |
| M4 A864 ohne `UnsolicitedOffer`-Prüfung | rot 1/11 |
| M5 Höhe wieder beim Entstehen würfeln | rot 2/11 (A731) |
| M6 `OpenBid` ohne `RollBid` | UI rot 2/3 |
| M7 `Enter` öffnet wieder das erste Angebot | UI rot 3/3 |

**Erwartet offen, bestätigt:**
- Poolplatz 179 ist kein Befund. 0x05521 zieht `random(1, 0xB3)`, und `PoolSlot179_IsReachedByQuotaDraw_Not180Up` ist grün.
- Datenbankplätze im Pool (`CaptureDatabaseNames`) bleiben `[APPROX]` (Transfer.Pool.cs, Kommentar bei 366).
- Die Schemanummer ist vorläufig. Dieser PR ändert kein Speicherformat, `main` hat inzwischen Schema 57, die Nummer ist also
  beim Rebase abzugleichen.

**Akzeptierte `[APPROX]`:**
- Level für `ChangeList` ist der Manager am Zug statt `[0xA70D]`.
- Neuanlage bei Alter > random(37, 42) trifft den verlorenen Platz nicht.
- Text der Verletzungsart in Zeile 2.
- Im Modus „nur Weltkugel“ gibt es keinen Tageslauf.
- Nachwurf in `Accept`.
- Gruppen 0 … 2 nach `Club.League`.

**Annahmen:** DGROUP 0x828 = „Tag ohne Ereignis“ ist aus dem PR übernommen (`SeasonCalendar.Events`), nicht gegen einen
Spielstand geprüft. Die Positionsdeutung von 0x7C6/0x7C7 nach 0x35EDD stützt sich auf den eigenen Kommentar des PRs und auf
`NextPoolClub` (Ligaanfang 0x7C0).
