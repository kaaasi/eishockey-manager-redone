# Review: Integration Runde 9 – spielberechnung-komplett (PR #338)

- **Reviewer:** Claude Code (geplanter Lauf, frische Sitzung ohne Implementierungskontext; Autorensitzung des PR ist eine andere)
- **Geprüft:** Base `34eeec85ed69daeb49e67b7fc2198ea775839cb5` .. Head `37267474a77749c0db26dcf2f6fb7ec53321e690` (nur Delta)
- **Checkout:** Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer; gebaut und getestet mit Originaldaten
  (`kaaasi/eishockey-manager-referenz`, `original/`). Disassembly: `ndisasm -b 16 em.exe` (Datei-Offsets).

## 1. Merge-Empfehlung

**Ja, für genau diesen Head** – mit Folgeauftrag zu R9S-01 bis R9S-03. Build und alle drei Läufe sind grün und stimmen mit der
PR-Tabelle überein, beide Gegenbeispiele sind nachvollzogen, die Stichproben-Mutationen aller fünf Teile werden von ihren
Regressionstests gefangen, und die Probenänderungen verdecken keinen Buchungs- oder Spielfehler (Abschnitt 2, R9S-04/-05).
Die P2-Befunde betreffen die Fundstelle 0x3702E–0x37085: Der Anschluss bildet nur den Pokalteil nach, der Kommentar
`[ORIGINAL]` behauptet mehr. Zwei davon gab es schon vor dem PR, R9S-02 ist neu, wirkt aber nur bei einer Option, die
nicht voreingestellt ist. Keiner blockiert den Merge, aber die Kennzeichnung muss vor der nächsten Runde stimmen.

## 2. Befunde

### R9S-01 (P2) – Merker „Managerverein spielt“ gilt im Original für den ganzen Spieltag, im Remake je Paarung

[`MatchEngine.Live.cs` Z. 64/78](https://github.com/kaaasi/eishockey-manager-redone/blob/37267474a77749c0db26dcf2f6fb7ec53321e690/src/EishockeyManager.Core/MatchEngine.Live.cs#L64-L78)

Datei 0x36FEA–0x3700B: `[bp-5] = 0`, dann für m = 0…5 `cmp byte [0xA729+m],0` → `[bp-5] = 1`. DGROUP 0xA729 + m ist
„Satz aktiv“ eines Managers (gesetzt in der Spielvorbereitung 0x545A6/0x547C7, gelöscht 0x54FF3). 0x37028 `cmp [bp-5],0;
jnz 0x37088` prüft das **einmal für den Spieltag**; die Minutenschleife 0x37A62 rechnet dann alle Paarungen
(`docs/spielberechnung.md` §1). `docs/reverse-engineering.md` Z. 671 sagt selbst „Spieltag ohne Managerverein“.
Das Remake entscheidet je Spiel (`managerMatch = homeManaged || awayManaged`).

**Gegenbeispiel:** Europapokalrunde (Schalter 0x228 aus, Voreinstellung) mit einem Managerverein: Im Original laufen
alle Paarungen der Runde durch die Minutenschleife, im Remake nehmen die Paarungen ohne Manager den schnellen Weg 0x3731F.
Mit DEB-Pokal-Schalter aus gilt das Gleiche für den DEB-Pokal. Das gab es schon vor dem PR (vorher immer schneller Weg),
ist aber jetzt als `[ORIGINAL]` gekennzeichnet. **Vorschlag:** einen Merker „Spieltag mit Managerverein“ vom Aufrufer
(Runde) übergeben oder `[APPROX]` mit Begründung setzen.

### R9S-02 (P2) – WM-Endrunde (Code 10) folgt im Remake dem WM-Schalter, im Original nie

[`MatchEngine.Live.cs` Z. 77–78](https://github.com/kaaasi/eishockey-manager-redone/blob/37267474a77749c0db26dcf2f6fb7ec53321e690/src/EishockeyManager.Core/MatchEngine.Live.cs#L77-L78),
Aufrufer [`CupsWorld.cs` Z. 210](https://github.com/kaaasi/eishockey-manager-redone/blob/37267474a77749c0db26dcf2f6fb7ec53321e690/src/EishockeyManager.Core/CupsWorld.cs#L210)

0x3702E prüft 0x229 nur bei Code `[0xD0C2]` = 8 (WM-Gruppe, gesetzt 0x30F53). Die WM-Endrunde setzt Code 10 (0x31017).
Code 10 fällt durch alle Vergleiche bis 0x37074 (`cmp 6`) → `jmp 0x731F`: also **immer der schnelle Weg**. Das Remake
nutzt `MatchCompetition.WorldCup` für Gruppe und Endrunde, also auch in der Endrunde den Schalter.

**Gegenbeispiel (diagnostisch, nicht committet):** `PlayWithEvents(…, MatchCompetition.WorldCup, knockOut: true, new Random(7))`
mit zwei Vereinen ohne Manager, danach `random.Next()`: Schalter aus 2043921309, Schalter an 676624765 – der Zufall wird
also verschieden verbraucht. Original: in beiden Fällen der schnelle Weg. Vor dem PR passte das (immer schnell); neu
abweichend, aber nur bei eingeschaltetem WM-Schalter (voreingestellt aus).
`[PLAUSIBEL]`: dass 0x31017 über 0xF0:0x70 denselben Spieltagsablauf 0x36B5D erreicht, ist aus dem Aufrufmuster
geschlossen (wie Code 1, 3/5, 8), der Überlagerungssprung ist nicht aufgelöst.

### R9S-03 (P2) – Play-offs/Aufstiegsrunde (Code 1) nehmen im Original voreingestellt den schnellen Weg

[`GameOptions.cs` Z. 109–114](https://github.com/kaaasi/eishockey-manager-redone/blob/37267474a77749c0db26dcf2f6fb7ec53321e690/src/EishockeyManager.Core/GameOptions.cs#L109-L114)

0x37058–0x37085: Code 1 (Play-off-/Aufstiegsrunden, 0x31AB7; `MatchEngine.Original.cs` Z. 191) → Minutenschleife nur,
wenn eines der Felder „Ergebnisse“ der Meldungsmatrix (0x21E/0x221/0x224) an ist; sonst `jmp 0x731F` (der schnelle Weg
prüft den Code nicht). Die Matrix ist voreingestellt ganz aus (Datei 0x16FED). Das Remake rechnet `Playoff`/`Relegation`
ohne Manager **immer** Minute für Minute; `ShowsCup` liefert dafür `null`, der Kommentar sagt „für Liga und Play-offs ohne Bedeutung“.

**Gegenbeispiel (diagnostisch):** `PlayWithEvents(…, Relegation, false, new Random(7))` bzw. `(…, Playoff, true, …)`
mit allen `Options.Reports` aus und an: `random.Next()` danach ist beide Male 227950298 – der Schalter wirkt nicht.
Im Original wäre der Weg bei „aus“ der schnelle. Das gab es schon vor dem PR, berührt aber die Meisterverteilung, weil
die Play-offs ohne Managerverein den Meister bestimmen. **Vorschlag:** `CupMatchUsesMinuteLoop` um Code 1 erweitern
(`Reports[ReportIndex(Results, l)]` für l = 0…2) oder `[APPROX]`. In beiden Fällen die Kommentare in `GameOptions.cs`
und `docs/spielberechnung.md` §11 Nr. 2 richtigstellen.

### R9S-04 (P3) – Ausweg der Finanzprobe greift am Head nicht mehr und schwächt die Probe für später

[`FinanceCalibrationTests.cs` Z. 231](https://github.com/kaaasi/eishockey-manager-redone/blob/37267474a77749c0db26dcf2f6fb7ec53321e690/tests/EishockeyManager.Core.Tests/FinanceCalibrationTests.cs#L231)

Gegenbeispiel nachvollzogen: An `645da78` mit der Probe aus `645da78^1` ist `…Synthetic(Bundesliga, passiv)` rot
(`Expected SeasonOver, Actual GameOver`). Die Ursache stimmt mit der Begründung überein: Seed 3 steigt nach Saison 2 ab,
der Kader schrumpft auf 1/6/8 (Aufstellung 9), in Saison 4 bucht `Strafen` −4.200.000 (21 Spielwertungen × 200.000) →
Entlassung. Das ist eine Spielregel und kein Buchungsfehler (die Verletzungen aus Teil 3 machen die dünne Aufstellung des
passiven Managers unspielbar). **Am Head (`58e6df3` und später) kommt aber Seed 3 durch alle vier Saisons** (lowest −547.251,
end 1.187.469; Seeds 17/41 ebenso), der neue Zweig ist also toter Code. Er würde künftig jede Entlassung eines passiven
Managers ab Saison 3 durchlassen und die Kontoprüfung `lowest > DebtLimit` abschalten. **Vorschlag:** Zweig entfernen
oder auf den Seed beschränken und dort die Ursache prüfen (`Strafen` ≠ 0, Kader < 9).

### R9S-05 (P3) – Proben-Lockerungen: Geld in TransferOriginalVertragTests, R204 akzeptiert beide Ausgänge

- [`TransferOriginalVertragTests.cs` Z. 419](https://github.com/kaaasi/eishockey-manager-redone/blob/37267474a77749c0db26dcf2f6fb7ec53321e690/tests/EishockeyManager.Core.Tests/TransferOriginalVertragTests.cs#L419):
  Ohne die Geldsperre wird die aktive Probe am Head rot (`Values differ`). Der Verlauf zeigt keinen Fehler: Die Probe kauft
  jeden Tag zu 80 %, Abstieg nach Saison 2, Gehälter 1,70 Mio. bei 1,61 Mio. Eintritt, dann Dispozinsen (10 %/Monat,
  0x3ED4F) −847.374 in Saison 4 → Konto −2,46 Mio. Die Lockerung ist vertretbar, aber der Doc-Kommentar Z. 398 („Kader
  und Geld bleiben gültig“) stimmt nicht mehr.
- [`LifecycleScreenRegressionTests.cs` Z. 39](https://github.com/kaaasi/eishockey-manager-redone/blob/37267474a77749c0db26dcf2f6fb7ec53321e690/tests/EishockeyManager.UI.Tests/LifecycleScreenRegressionTests.cs#L39):
  Der Ablauf ist deterministisch; am Head nimmt er immer den Poolzweig (`inOrigin=False inPool=True`, Verein 9).
  Der Vereinszweig wird nie ausgeführt. Besser genau den beobachteten Ausgang prüfen.

### R9S-06 (P3) – Verteilungstabellen in `docs/spielberechnung.md` §15 nicht auf dem integrierten Stand

Am Head (`TeamValueDriftTests.ChampionSpread_LikeVideo`, 12 Seeds × 4 Saisons): **19** verschiedene Meister,
Düsseldorf + Köln 10/48, Meister unter 80 **24**/48 – das Dokument nennt 14 und 19/48. Gegen das Video (8 Meister in
18 Saisons, D+K 5/18, schwache 8/18) streut das Remake eher stärker. Bei der Verteilung aus
`SeasonDistribution_OriginalDatabase` erwartet man in 18 Saisons etwa 10,3 verschiedene Meister (Stichprobe aus den 48 Titeln).
Die Abnahmegrenzen des Tests halten. Tore am Head (diagnostisch, 6 Seeds × 2 Saisons, Originaldaten, Verein 42 aktiv):
Bundesliga 6,27/Spiel, 11,6 % Unentschieden; 2. Liga 7,44 / 8,8 %; Oberliga 8,11 / 8,7 % – das passt zum Video (Oberliga
8,0 / 7,9 %, Bundesliga 11,4 % Unentschieden). Strafminuten (Fair-Play-Tabelle) 13,2–13,5 je Verein und Spiel, also 26–27 je
Spiel; einen Videowert gibt es nicht (§11), nicht abnehmbar. Tabelle §15 bitte mit den Werten am Head nachführen.

### Erwartet offen – bestätigt

- WM-Schalter im Bundestrainer-Abschnitt: 0x5541F `mov al,[0x229]; mov [0x22c],al`, 0x5545F `mov byte [0x229],1`,
  0x5575A Rückschreiben – nicht nachgebaut, `[APPROX]` in `MatchEngine.Live.cs` Z. 75–76 und §11 vorhanden.
- 0x58DC (Verkauf an Computerverein stärkt den Käufer): kein Vorkommen in `src/`, als Restlücke in §15 geführt.

### Geprüft ohne Befund

- **Anschluss 0x3702E–0x37085 (Pokalteil):** Code 8 → 0x229, 3/5 → 0x228 (Code 4 kommt nicht vor: 0x31490 setzt 3, +2 bei
  `[0x7AF] > 5`), 6 → 0x227, 7 immer – stimmt mit `ShowsCup` überein. Vorrang in Z. 77–78 korrekt
  (`!managerMatch && (competition is A or B or C) && !…`).
- **Teil-PRs (Mutation → Regressionstest rot, danach zurückgesetzt):** T1 Driftschritt `−(Modus+b)+1` → 2 rot
  (`TeamValueDriftTests`/`…Komplett1`); T2 `MaxPenaltyDraws` 151 → 1 rot (0x37A30 `cmp al,0x96` bestätigt); T3
  Verletzungsschwelle 60 → 50 → 1 rot; T4 Unterzahl-/Überzahlzähler vertauscht → 1 rot; T5 Penaltytreffer
  random(2, 6) → 1 rot (0x39116 `push 5; push 2` bestätigt).

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `tools/pruefen.sh` (alle drei Läufe) am Head | bestanden |
| Gegenbeispiel `IntegrationRunde9Tests.CupMatchWithoutManager_FollowsDisplaySwitch` an `645da78` (Test eingefügt) | 3 rot („Values are equal“) |
| dasselbe am Head | grün (im vollen Lauf) |
| `FinanceCalibrationTests.FourSeasonChanges_Synthetic…(Bundesliga, passiv)` an `645da78`, Probe aus `645da78^1` | rot, `GameOver` statt `SeasonOver` (Ursache siehe R9S-04) |
| Mutationen T1–T5 (siehe oben) | jeweils rot |
| Diagnoseläufe R9S-02/-03/-06, Verläufe R9S-04/-05 | nur lokal, nicht committet |

`tools/pruefen.sh` am Head (SDK 10.0.402):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1626 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1346 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1346 | 280 | 0 |

**Akzeptierte `[APPROX]`:** WM-Schalter im Bundestrainer-Abschnitt (Restlücke), Reihenfolge der Wanderung nach Vereinsindex und
Tag 0 = Tageszähler 0 (§15), Feldspieler-Neuzählung 0x38A36 über die Strafbank (§17 Nr. 1), Darstellung des Penaltysiegers.

**Annahmen (nicht geprüft):** R9S-02 Erreichbarkeit von 0x36B5D für Code 10 (siehe dort); Code 1 umfasst auch die
Bundesliga-Play-offs (laut `MatchEngine.Original.cs`, nicht neu belegt); Strafminuten gegen das Video nicht abnehmbar.
