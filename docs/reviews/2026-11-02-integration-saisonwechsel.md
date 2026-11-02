# Review: PR #460 – Integration Runde 11, Spur `saisonwechsel`

- PR: #460 · Base `3901e364d5ffaddc7045e5f484099ebc49cd8abf` (`main`) · Head `6410402579bcd22dbb06223bb76e39ff2306076f`
- Umfang: nur die Integration – Merge-Commit `24911cb` (Eltern `3901e36` und `paket/saisonwechsel-5` @ `dbbde55`) und
  Leitstellen-Commit `6410402`. Der Stapel #419 → #428 → #431 → #432 → #449 ist in Review #441 und Nachprüfung #458 geprüft.
- Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext; die Commits stammen aus einer anderen Sitzung)
- Checkout: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer; gebaut und getestet (nicht nur Codelesen).
  Mutationen im selben Checkout, nach jedem Lauf zurückgesetzt (`git status --porcelain` danach leer).

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** Die Konfliktlösung verliert nichts: Ich habe den Merge `3901e36` + `dbbde55` nachgestellt
(5 Konfliktdateien) und das Ergebnis je Datei gegen beide Eltern verglichen. Außerhalb der Konfliktdateien weicht `24911cb`
von der automatischen Zusammenführung nur in zwei gewollten Punkten ab: der Statuszeile in `docs/arbeitspakete.md` und der
Nummer im Kopf von `SaveGame.Migration.Saisonwechsel.cs`. Die einzige automatisch zusammengeführte Datei, die beide Seiten
ändern (`FinanceCalibrationTests.cs`), enthält genau die Änderung der Spur auf dem Stand von `main`. Alle drei Läufe von
`tools/pruefen.sh` sind am Head grün. Die neuen Abdrücke hängen an beiden Spuren (je eine Mutation macht alle 9 rot).
Die Vorbedingungszeile in der Kreditprobe verdeckt keine Regelkollision: em.exe setzt bei S-K1 nur das Konto und lässt
Kredite zwischen Managern unberührt; das Remake macht es genauso (Begründung unter Gegenbeispiel 3).

## 2. Befunde

Keine neuen Befunde P0–P3.

Bestätigt offen (unverändert gegenüber #441/#458, die Spur-Dateien am Head sind byte-gleich mit `dbbde55`):

- **SW-02 (P3):** Ewige Tabelle ohne letzte Saison bei Entlassung aller Manager am 29. April – Restlücke Runde 12, eingetragen
  in `docs/arbeitspakete.md` (Integrationsabschnitt, Zeile 203).
- **SW-05, SW-06 (P3):** Test legt den Partner nicht fest; Paarindex im Fall „doppelter Platz“ – unverändert offen.
- **Doppelfall-Regel `[APPROX]`** (Zwangsabsteiger mit Aufstieg, `ResolveDoubleMoves`): unverändert, akzeptiert wie in #458.

Hinweise (kein Merge-Hindernis):

- Der Statusabschnitt der Spur (`docs/arbeitspakete.md` Zeile 117) nennt Teil 5 noch „Nachprüfung durch die Leitstelle“,
  obwohl #458 vorliegt. Beim Rundenabschluss nachziehen.
- Spielstände, die auf den Spur-Branches mit der vorläufigen Nummer 23 (Zwangsabstieg) gespeichert wurden, würden jetzt als
  geld-Schema 23 gelesen. Die Nummer hat `main` nie erreicht, daher nur Entwicklerstände betroffen.

## 3. Ausgeführte Prüfungen

### Konfliktlösung

`git checkout 3901e36 && git merge --no-commit dbbde55` → Konflikte in `SaveGame.cs`, `SaveGameTests.cs`, `GameModeTests.cs`,
`RngTwinProbeTests.cs`, `SpielberechnungAnschlussTests.cs`. `git diff 24911cb^1 24911cb` je Konfliktdatei:

| Datei | Ergebnis |
|---|---|
| `SaveGame.cs` | Schema 26 → 27, Migrationen `[22]–[25]` (geld) vollständig, `[26] = MigrateSaisonwechsel` angehängt, Kommentarzeile 26 → 27 |
| `SaveGameTests.cs` | 27; geld-Kommentar erhalten („vorläufig“ entfernt), Zeile 27 ergänzt; Fingerabdruck neu (`784125EC8FB8D1F8`) |
| `GameModeTests.cs`, `RngTwinProbeTests.cs`, `SpielberechnungAnschlussTests.cs` | Begründungen beider Seiten vollständig + Integrationszeile; Abdrücke neu |

`Mode4Reference` stimmt auf Plausibilität: Manager 0 Saison 1 = Wert von `main` (612.685), Manager 1 jede Saison 400.000 (S-K1),
R/C = Wert der Spur (geld ändert laut seinen Proben R/C nicht). Alle 26 von der Spur geänderten Dateien: 18 am Head byte-gleich
mit `dbbde55`, die übrigen 8 sind die oben genannten (beide Seiten).

### Gegenbeispiel 1 – Schema

Kleines Konsolenprogramm gegen `EishockeyManager.Core`/`Formats` je Worktree (Originaldatenbank, Manager A Verein 2 Bundesliga,
Manager B Verein 30 Oberliga, Seed 5, 40 × `Continue()` → 4. Nov. 1993, laufende Saison):

| Stand | gespeichert mit | Laden am Head | Rundlauf | weiterspielen (4 Saisonwechsel, Rundlauf nach jedem) |
|---|---|---|---|---|
| Base `3901e36` | Schema 26 | ok, Schema 27 | byte-gleich | durch bis 1997; S-K1 1994/1996 Konto 400.000, 1995 500.000 (= 400.000 + Prämie), Rundläufe gleich |
| vor geld `02c4bd7` | Schema 22 | ok, 22 → 27 | byte-gleich | wie oben |

### Gegenbeispiel 2 – Abdrücke hängen an beiden Spuren

Filter `RngTwinProbeTests | SpielberechnungAnschlussTests.FourSeasonChanges | GameModeTests.Mode4`, `EM_REQUIRE_GAMEDATA=1`:

| Mutation | Ergebnis |
|---|---|
| S-K1: Nachrücker aus allen 12 Plätzen statt `random(36, 41)` (`SeasonFlow.Oberliga.cs:85`, `.Take(OutsideDrawPlaces)` entfernt) | 9/9 Abdrücke rot |
| geld: Jugendzug 0x39DEC aus (`GameState.Manager.cs:142`, `DailyYouth` auskommentiert) | 9/9 Abdrücke rot |
| unverändert | 10/10 grün |

### Gegenbeispiel 3 – `FinanceBedienungTests.MultiSeason_FourChanges_BorrowAndRepayAtOnce`

- Ohne die neue Zeile `active.Money = Math.Max(active.Money, 5_000_000);`: rot, `Expected: 1000000, Actual: 400000`
  (`LendDefault(1, 0)` = min(Schuld, Konto)). Mit der Zeile: grün.
- **Original:** `ndisasm -b16` auf `em.exe` Datei 0x56287–0x56372. Im Zweig S-K1 schreibt em.exe nur
  `mov word [bx-0x69c3],0x12` / `mov word [bx-0x69c5],0x4f80` (Datei 0x562D8/0x562DE, `bx = m · 0x1AF`). Das sind genau
  die 4 Byte des Kontos (DGROUP 0x963B + m · 0x1AF). Danach folgen nur Statusbits (0x959F + m · 0x1AF) und die beiden Tausch-
  aufrufe `0xF0:0x4D` auf Vereinssätze (0x3D Byte). Die Kreditfelder liegen ab Konto + 0xC, 4 Byte je Partner (Manager 1:
  0x97EA → 0x97F6, `spielstand-original.md` FILE08). Sie werden nicht berührt. Offene Kredite zwischen Managern, Bankkredit und
  Sparbuch bleiben also stehen. Der Schuldner hat danach 400.000 DM und weiter die volle Schuld. Das Original würde die volle
  Rückzahlung in der Probe also genauso auf 400.000 begrenzen („Vorgabe = Kreditschuld, höchstens Konto“, `re-geld.md`).
- **Remake:** `SeasonFlow.cs:321` setzt nur `manager.Money`, `ManagerLoans` und Bank bleiben. Die Probe belegt weiterhin, dass
  der Kredit S-K1 übersteht. Mit aufgefülltem Konto liefert `LendDefault` wieder die volle Schuld von 1.000.000, und die
  Rückzahlung leert `ManagerLoans`. **Keine verdeckte Kollision**, die Anpassung ist die richtige Vorbedingung.

### `tools/pruefen.sh` (alle drei Läufe, Head `6410402`, SDK 10.0.402)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1763 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1478 | 0 | 256 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1478 | 285 | 0 |

Ergebnis: bestanden. CI am Head: `build` erfolgreich.

### Akzeptierte `[APPROX]`

Keine neuen durch die Integration. Weiter gelten die in #441/#458 akzeptierten (Doppelfall-Regel; Zug am 13. statt 29. April;
entlassene Manager zählen mit; Platzpaarung bei mehreren Startvereinen ohne Liga).

### Annahmen

- Die Lage der Kreditfelder (Konto + 0xC + 4 · Partner) stammt aus `spielstand-original.md` (FILE08) und wurde hier nur über die
  Adressrechnung abgeglichen, nicht neu in DOSBox gemessen.
- Ob em.exe bei einem anderen Schritt des Saisonwechsels (außerhalb von 0x56287–0x56372) Managerkredite ändert, habe ich nicht
  gelesen. Das betrifft S-K1 nicht und war schon vor der Integration Verhalten von `main`.
