# Nachprüfung: Integration Runde 8, Welle 3 (#268)

- PR: #268 · Base `99fa995b941d9e85234cebb0ca4c7dc00507a100` · Head `d61ef21f28df7ac7163b2d92826444bc6b5e6893`
- Vorheriges Review: `docs/reviews/2026-11-01-integration-runde-8-welle-3.md` (PR #271), Head `643488cd`. Geprüft wurde nur
  das Delta `643488c..d61ef21` (5bc0c07, d61ef21) sowie die genannten Befunde.
- Reviewer: Claude, frische Sitzung ohne Implementierungskontext, geplanter Lauf, unbeaufsichtigt, 01.11.2026.
- Checkout: Klon mit `git rev-parse HEAD` = Head-SHA und leerem `git status --porcelain`. Originaldaten aus
  `kaaasi/eishockey-manager-referenz` (`tools/claude/fetch-reference.sh original`, 215 Dateien), SDK 10.0.402 aus dem
  Release `claude-werkzeuge`. Die Proben liefen in einem eigenen Worktree und sind nicht im Repo.
- **Annahmen** (unbeaufsichtigter Lauf):
  - Neue Befunde werden wie R8W3I-02 nach ihrer Wirkung im Core eingestuft. Die Erreichbarkeit im UI hängt an der noch
    offenen Verhandlung am Saisonende (0x27D05).
  - Die IDs setzen die Zählung des Reviews fort (R8W3I-12 ff.).
  - Als Messwerte für „Stand 643488c“ dienen Läufe am Head, bei denen nur R8W3I-01 per Umgebungsvariable zurückgedreht
    ist. Die übrigen Delta-Änderungen betreffen nur Verträge nach dem Saisonende.

## 1. Merge-Empfehlung

**Nein für genau diesen Head, solange R8W3I-12 (#272, P2) offen ist.** Die Behebung von R8W3I-02 greift auf Level 5 nicht.
Die Korrektur ist klein (Forderung unabhängig vom Level berechnen). Alternativ kann der Nutzer sie als Folgearbeit annehmen.
Ohne R8W3I-12 wäre es ein Ja.

Bestätigt ist Folgendes:

- **R8W3I-01 (#269) ist behoben.** Der Spielstand wird vor den Toren gelesen (`homeBefore/awayBefore`) und an 0x80CB
  sowie 0x939A übergeben, wie in 0x37E2D/0x37E62 → 0x38011, 0x3805A/0x3806E. Der Gegenbeispieltest
  `ManagerMatch_ComputerModes_UseScoreBeforeMinute` (1:2 vor Minute 59, Ausgleich, Torhüter heraus) ist grün. Mit dem
  Lesen nach den Toren wird er rot.
- **R8W3I-02 (#270) ist für die Level 0–4 behoben.** `DemandThirds` hat nach dem Saisonende keine Untergrenze mehr
  (9.000/100/100/2/Alter 27/bisher 20.000 → 14.400). `Negotiate` vergleicht mit der eingegebenen Laufzeit und der
  Forderung: Eine kürzere Laufzeit ist dann nicht „zu kurz“, und ein Gehalt unter der Forderung wird ohne Zufallszug
  abgelehnt, nur die Sperre wird gezogen. Beide neuen Tests werden bei Mutation rot. Ausnahme: Level 5, siehe R8W3I-12.
- **`tools/pruefen.sh`:** Alle drei Läufe sind am exakten Head grün (3.1).

## 2. Befunde

Links auf den Head: `https://github.com/kaaasi/eishockey-manager-redone/blob/d61ef21f28df7ac7163b2d92826444bc6b5e6893/…`

### P2

**R8W3I-12 (#272): Vertrag nach dem Saisonende auf Level 5 – Laufzeit und Forderung werden nicht zurückgeschrieben**

- **Stelle:** [`Player.Contract.cs` Z. 292](https://github.com/kaaasi/eishockey-manager-redone/blob/d61ef21f28df7ac7163b2d92826444bc6b5e6893/src/EishockeyManager.Core/Player.Contract.cs#L292).
  Die Übernahme hängt an `DemandAfterYears(...) is { } demand`, und `DemandAfterYears` liefert ab Level 5 `null` (Z. 254).
- **Original:**
  - Nach der Laufzeiteingabe (`0x4A7F1 call 0x38:0x30b`) rechnet es ohne Levelabfrage bis 0x4A9A1 und schreibt dort +0x28/+0x2A
    und +0x0B.
  - Erst `0x4A9DB cmp byte [0xed8],0` entscheidet, ob das Gehaltsfeld gefüllt wird. Das ist im gesamten Block
    0x4A81B–0x4A9F2 die einzige Abfrage von 0xED8.
  - „Mach' ich!“ liest danach die zurückgeschriebenen Werte (0x4ABD1–0x4ABEC) und vergleicht sie (0x4AC1A, 0x4AC75).
- **Gegenbeispiel** (Probe: synthetische Datenbank, Seed 3, Verein 2, `LifecycleRegressionTests.FinishSeason`, Stürmer mit
  `ContractYears = 3` und 2.300 DM, Eingabe 2 Jahre und 23.000 DM, `demandFilled: false`):
  - Level 3 → `Accepted` (Forderung 18.066 DM).
  - Level 5 → `TooShort`.
  - Das Original nimmt an: Laufzeit = +0x0B, Gehalt ≥ +0x28 (17.281 DM), Kurzweg `si = 3` bei 0x4AC3C.
  - Umgekehrt nimmt das Remake auf Level 5 bei gleicher Laufzeit schon ab dem alten Gehalt an, das Original erst ab der
    Forderung oder über 0x49A3B.
- **Wirkung:** Vertragsergebnisse und Gehälter auf Level 5 nach dem Saisonende. Es ist derselbe Weg wie bei R8W3I-02, die
  Erreichbarkeit im UI hängt ebenso an 0x27D05.

### P3

- **R8W3I-13:** Das `[APPROX]` in `Negotiate` ist in Wahrheit `[ORIGINAL]`.
  - Stellen: [`Player.Contract.cs` Z. 289–291](https://github.com/kaaasi/eishockey-manager-redone/blob/d61ef21f28df7ac7163b2d92826444bc6b5e6893/src/EishockeyManager.Core/Player.Contract.cs#L289-L291),
    [`Transfer.Contract.cs` Z. 42](https://github.com/kaaasi/eishockey-manager-redone/blob/d61ef21f28df7ac7163b2d92826444bc6b5e6893/src/EishockeyManager.Core/Transfer.Contract.cs#L42).
  - Bei einer Ablehnung (`si = 2`) zieht das Original die Sperre (0x4AD3E–0x4AD50) und läuft dann in 0x4AD54 weiter. Dort
    stellt es +0x30/+0x32, +0x0B und +0x28/+0x2A aus den Werten zurück, die beim Öffnen gesichert wurden (0x4A239–0x4A25F).
  - Nach einer gescheiterten Verhandlung bleiben also im Original wie im Remake die alten Werte. Die Annahme (Punkt 4 der
    Anfrage) stimmt damit inhaltlich.
  - Die Markierung sollte aber `[ORIGINAL] 0x4AD3E→0x4AD54` lauten. Auch der Satz „Abbruch stellt beides zurück“ in
    `Transfer.Contract.cs` ist unvollständig, denn es gilt für Abbruch und Ablehnung.
- **R8W3I-14:** Die Begründung zur Grenze in `OriginalDatabase_GoalsPerGame_InVideoRange` ist ungenau.
  - Stelle: [`SpielberechnungAnschlussTests.cs` Z. 221–227](https://github.com/kaaasi/eishockey-manager-redone/blob/d61ef21f28df7ac7163b2d92826444bc6b5e6893/tests/EishockeyManager.Core.Tests/SpielberechnungAnschlussTests.cs#L221-L227).
  - In diesem Test spielt der Manager in der Oberliga, die Bundesliga hat **keine** Managerpaarung (Probe: 0 von 0).
    6,09 → 5,97 ist also keine Folge des korrekten Spielstands, sondern Streuung über den gemeinsamen Zufallsstrom.
  - Seeds 1–12 am Head: Bundesliga 5,97–6,69 (Mittel 6,31); mit zurückgedrehtem R8W3I-01: 6,06–6,65 (Mittel 6,38).
    Oberliga 7,53–8,32 bzw. 7,72–8,27.
  - Die geänderte Grenze selbst ist vertretbar: Die Oberliga bleibt bei 6–10 wie belegt, die übrigen Ligen sind ohne
    Videobeleg. Nur der Kommentar sollte die Ursache richtig nennen.
- **R8W3I-15 (Verdacht, nicht bestätigt, Altbestand aus #263):** Managervereine spielen mit Spielerwerten deutlich schwächer
  als mit Datenbankstärke.
  - Probe: Originaldaten, 40 Seeds, 36 Spieltage, Platz, Punkte und Tordifferenz aus `game.Results`:

    | Verein | Spielerwerte (Head) | dito, R8W3I-01 zurück | `PlayAtDatabaseStrength` |
    |---|---|---|---|
    | 2 (Düsseldorf) | Platz 8,95 · TD −24,2 | Platz 9,18 · TD −27,4 | Platz 3,40 · TD +36,4 |
    | 30 (Oberliga) | Platz 8,22 · TD −25,0 | Platz 8,53 · TD −27,3 | Platz 2,73 · TD +58,5 |

  - Das bestätigt die Messung der Leitstelle: Der Fix bringt keinen Stärkeverlust, sondern eher einen leichten Gewinn.
  - Zugleich zeigt es, warum die Vorbedingung „Bundestrainer nach Saison 1“ am Zufall hängt. Ein starker Verein landet unter
    Managerführung im Mittel auf Platz 9.
  - Die vier Ablauftests mit `ManagedSquadsUi.PlayAtDatabaseStrength` (Punkt 2 der Anfrage) verdecken keinen Fehler aus dem
    Delta. Sie prüfen UI-Abläufe des Bundestrainers, nicht die Spielstärke. Sie umgehen aber, wie schon 23 Aufrufe in Core-Tests,
    genau diesen Abstand.
  - Ob das Original Managervereine ebenso schwächt, ist nicht belegt. Empfehlung: in Runde 9 (spielstaerke-manager Teil 3)
    gegen ein Video messen.
- **R8W3I-11 (aus dem Review):** nur teilweise erledigt. Der Hinweis „im Plan als Ausnahme nachgetragen“ steht nur in der
  Review-Zeile (`arbeitspakete.md` Z. 323). Die Regel in Z. 269–271 nennt weiter #193 als „Einzige Ausnahme“.

## 3. Ausgeführte Prüfungen

### 3.1 `tools/pruefen.sh` (alle drei Läufe, exakter Head, Release, Warnungen = Fehler)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1406 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1175 | 0 | 207 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1175 | 231 | 0 |

Commit `d61ef21f28df7ac7163b2d92826444bc6b5e6893`, SDK 10.0.402, Ergebnis „bestanden“.

### 3.2 Gegenbeispiele und Mutationen (Worktree, Filter `IntegrationRunde8Welle3Tests|TransferOriginalVertrag`)

| Mutation | rot |
|---|---|
| R8W3I-01 zurück (Stand nach den Toren an 0x80CB/0x939A) | `ManagerMatch_ComputerModes_UseScoreBeforeMinute` |
| `DemandThirds` wieder mit `Math.Max` nach dem Saisonende | `Demand_AfterSeason_HasNoFloor` |
| Übernahme von Laufzeit/Forderung in `Negotiate` entfernt | `Negotiate_AfterSeason_UsesWrittenDemandAndYears` |

- Bei 0x80CB wendet `Apply` die Torhüterregel auf beide Seiten an. Der Test ist also unabhängig vom Seitentausch gültig.
- R8W3I-12: siehe Gegenbeispiel in 2.

### 3.3 Referenzwerte und Begründungen (Punkt 1 der Anfrage)

- **Mode4 −6,96 Mio. bestätigt.** Probe nach `Mode4_ReferenceValues_Unchanged`:
  - Kader von Manager 0 je Saison 20/20/19/16/12, mit zurückgedrehtem R8W3I-01 20/20/19/16/13.
  - Saison 5 am Head: Strafen −9.200.000, Eintritt 0, Zinsen −1.601.228; Kontostand −6.956.210 wie in der Referenz.
  - Mit 13 Spielern: Strafen 0, Kontostand 4.826.927.
  - Es ist kein neuer Geldfehler. Die Referenz hält in Saison 5 aber einen nicht spielfähigen Kader fest und taugt dort als
    Geldprobe wenig. Das steht als Restlücke im Plan und ist bestätigt.
- **RngTwin, Zähler, WorldOnly, DEB-Pokal:** Die Werte sind deterministisch und in 3.1 grün. Die Ursache (andere Ergebnisse
  der Managerpaarungen über den gemeinsamen Zufallsstrom) ist plausibel. Eine Gegenprobe, die den alten Verlauf erhält, gibt
  es weiterhin nicht.
- **„Kein systematischer Stärkeverlust“ bestätigt** mit eigener Probe über 40 Seeds und zwei Vereine (Tabelle bei R8W3I-15).
- **„Düsseldorf 2/8 → 0/8 → 7/8 Seeds“** habe ich nicht nachgemessen.

### 3.4 Disassembly (`ndisasm -b16`, Dateiadressen)

- **0x4A7F1–0x4A9F2:** Laufzeiteingabe, Forderung, 0x4A963 Tageszähler > 0xE8, Rückschreiben 0x4A9A1 vor der
  Untergrenze 0x4A9B3, Level erst 0x4A9DB.
- **0x4A22D–0x4A262:** Sicherung von +0x30/+0x32, +0x0B, +0x28/+0x2A beim Öffnen.
- **0x4AB66–0x4AD7A:** TooLong 0x4AB93, Kurzweg 0x4AC1A–0x4AC3C (Laufzeit gleich und Gehalt ≥ Platzwert), Ergänzen
  0x4AC3F–0x4AC6D, TooShort 0x4AC75, 0x49A3B bei 0x4ACDA, Annahme 0x4ACE4, Sperre 0x4AD3E, Rückstellen 0x4AD54 bei Abbruch
  und Ablehnung.

### 3.5 Erwartet offen – bestätigt

- **R8W3I-03 bis -10:** unverändert offen. Das Delta berührt `MatchEngine.Managed.cs`, `MatchEngine.Live.cs` und das UI
  nicht. `NegotiationLock = 0` steht weiter in `Player.Contract.cs` Z. 95.
- **R8W3I-08** (ganze DM) wirkt jetzt auch auf die Vergleichsforderung nach dem Saisonende: `currentSalary` = Forderung/3,
  abgerundet.
- **R8W3I-11:** siehe 2.
- Alle sind im Plan als Restlücken für Runde 9 geführt.

### 3.6 Akzeptierte `[APPROX]`

- Keine neuen. Das `[APPROX]` aus Punkt 4 ist inhaltlich richtig, gehört aber als `[ORIGINAL]` markiert (R8W3I-13).
