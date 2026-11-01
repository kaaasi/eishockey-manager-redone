# Review: PR #322 – Integration Runde 9, wettbewerbe-komplett

- PR: #322 · Base `5729e695aa6f9fd7f7a0a9c0ffefc921727e1098` (gestapelt auf #320) · Head `3f4fceac296b1da0e0e3a765c73801163d56dc53`
- Geprüft: nur das Delta Base..Head (34 Dateien, +1694/−39)
- Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext)
- Checkout: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer; gebaut und getestet (nicht nur Codelesen)

## 1. Merge-Empfehlung

**Nein für genau diesen Head – nach Behebung von R9W-D01 (P2) ja**, sofern der Nutzer R9W-D01 nicht ausdrücklich
zurückstellt. Startordnung ab Saison 2, Titelzahl, WM-Kappung und Migration 17 → 18 sind gegen Disassembly und Video
stimmig, alle drei Läufe grün, Gegenproben greifen, die Mehrsaisonproben bestehen auch mit fremden Seeds. Offen ist ein
als originalgetreu ausgewiesenes, aber widerlegtes Verhalten der Länder-Bilanz im Gegner-Fenster.

## 2. Befunde

### R9W-D01 (P2) – Bilanz der Länder wird beim Saisonwechsel nicht geleert; Kommentar und RE-Doku behaupten „wie im Original“

- Stellen: [CupsNational.TeamInfo.cs:166–167](https://github.com/kaaasi/eishockey-manager-redone/blob/3f4fceac296b1da0e0e3a765c73801163d56dc53/src/EishockeyManager.Core/CupsNational.TeamInfo.cs#L166-L167)
  („Wie im Original setzt der Saisonwechsel sie nicht zurück“), [reverse-engineering.md:781](https://github.com/kaaasi/eishockey-manager-redone/blob/3f4fceac296b1da0e0e3a765c73801163d56dc53/docs/reverse-engineering.md#L781)
  („… nicht die Bilanzzeichen“). Widerspricht dem eigenen Teil-1-Eintrag derselben Datei (Zeile ~705: „Bilanz 0x825E aus DGROUP 0x5503“).
- Beleg em.exe: Datei 0x55EBA–0x55EE0 im Saisonwechsel-Schleifenkörper (alle 64 Teams, [bp−0xA] = 0, 1):
  `push ds; push 0x5503; push ds; push 0x825E + 0x50·Team + 9·Hälfte; call 0:0x1B0B`. 0:0x1B0B (Datei 0x390B) ist
  `strcpy(dest=[bp+6], src=[bp+0xA])`, DGROUP 0x5503 (Datei 0x1C2D3) = `"--------"`. 0x825E ist genau die Bilanz, die
  0x4F4F4 je Spiel schiebt und das Gegner-Fenster zeigt. Die Bilanzzeichen **aller** Teams (Vereine und Länder) werden also
  geleert.
- Videobeleg: `folge06_02238` (2. Saison, 1. Spieltag) zeigt in der Spalte „Bilanz“ `--------S/U/N` – nur das eine Spiel
  der neuen Saison, nichts aus der Vorsaison.
- Gegenbeispiel (Head): `GameModeTests.Create(7, true, true, 2, 30)`, 5 Spieltage, bis Saisonende spielen,
  `StartNextSeason()` → `NationalForm(56)` = `0,0,0,2,3,3,1,1` vor **und** nach dem Wechsel (Original: leer). In der
  folgenden WM zeigt „Aus d. letzten Spielen“ dann drei Zeichen der Vorjahres-WM statt Strichen.
- Behebung (Vorschlag): `_nationalForm` im Saisonwechsel (über `GameState.Integration.cs`/`StartNextSeason`) auf 0
  setzen, Kommentar `[ORIGINAL]` mit Fundstelle 0x55EBA, RE-Doku Zeile 781 korrigieren; Regressionstest „Bilanz nach
  `StartNextSeason` leer, Rekorde und Kopf-an-Kopf bleiben“ (Rekorde 0xBD0F und Kopf-an-Kopf 0xAB7B schreibt die Schleife
  0x55E39–0x55EF8 nicht – das ist richtig umgesetzt).

### R9W-D02 (P3) – Laufende Spielstände ab Saison 2 sortieren rückwirkend anders

`LeagueTable.StartOrder` ([Season.cs:244](https://github.com/kaaasi/eishockey-manager-redone/blob/3f4fceac296b1da0e0e3a765c73801163d56dc53/src/EishockeyManager.Core/Season.cs#L244)) gilt auch für geladene
Spielstände von Schema ≤ 17 mitten in Saison ≥ 2: Die Tabelle der laufenden Saison (und die Tendenz-Smileys) wird mit der
neuen Startordnung neu berechnet. Deren `PromotedInto`/`RelegatedInto` stammen noch aus der alten Reihenfolge
`group.Clubs` (nicht Tabellenreihenfolge), die Paarung kann daher vom Original abweichen. Wirkt nur bei vollem Gleichstand.
Kein Handlungsbedarf außer einem Satz im `[APPROX]` von `SuccessorOrder` bzw. in der Migration.

### R9W-D03 (P3) – Veralteter Kommentar

[SaveGameTests.cs:36](https://github.com/kaaasi/eishockey-manager-redone/blob/3f4fceac296b1da0e0e3a765c73801163d56dc53/tests/EishockeyManager.Core.Tests/SaveGameTests.cs#L36) nennt Schema 18 noch „vorläufig“; laut
Head-Commit ist es endgültig.

### Erwartet offen – bestätigt

- **#166** Saison eines Managers ohne Liga: weiter nur Beleglage (Teil 4, RE-Doku), `FinishClubSeason` nimmt Managervereine
  vom Oberliga-Austausch aus (`[APPROX]`). Bleibt für Runde 10.
- **Leeres Gegner-Fenster nach Migration** (`[APPROX]`): Spielstände von Schema 17 laden mit Rekorden 0, Kopf-an-Kopf 255,
  Bilanz 0 (unten, Prüfung 3) und füllen sich ab dem nächsten Länderspiel.

## 3. Ausgeführte Prüfungen

### Umgebung und Gesamtlauf

`tools/claude/fetch-reference.sh original`, `tools/claude/setup-workspace.sh`, SDK 10.0.402, `tools/pruefen.sh` (alle drei Läufe) am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1477 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1227 | 0 | 225 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1227 | 250 | 0 |

Commit 3f4fceac296b1da0e0e3a765c73801163d56dc53, Ergebnis: bestanden.

### (1) Startordnung ab Saison 2 und Titelzahl

- `ndisasm` 0x56137–0x56290: je Liga [bp−4] ab Platz 0 sucht den nächsten Verein mit Index ≥ 0x7C1[Liga] unter den Plätzen
  1–2, [bp−5] ab Platz 2 den nächsten mit Index < 0x7C1[Liga] unter 3–4; Tausch paarweise in Tabellenreihenfolge. Oberliga
  (0x56238): `0x5CCA + 12·Liga + [0x7B1+Liga] − k` = Letzter, dann Vorletzter; Partner `0x38:0x847(a, a+5)` mit
  Wiederholungssperre. Deckt sich mit `SuccessorOrder` und der neuen Schleife über `group.Table(Clubs)` in `SeasonFlow`.
  0x55E39–0x55EF8: Tabellenfelder 0x825A/0x8270/0x8274/0x8278/0x8280/0x8284 beider Hälften auf 0; 0x5CCB nicht berührt;
  0x5613E setzt 0x7A9 = 1. Erwartungswerte in `SuccessorOrder_PartnerTakesTheListPlace` von Hand nachgerechnet.
- Video: `folge06_01257` (Aufstieg 2. Liga: Bad Nauheim 1., Berliner SC 2., Weißwasser 3., Erding 4.) ergibt die Paare
  Bad Nauheim↔Weißwasser, Berliner SC↔Erding; `folge06_02238` zeigt die Gleichstandsgruppe Köln, Augsburg, Kempten,
  Bad Nauheim auf 5–8 mit Bad Nauheim zuletzt – mit der Platzliste der Vorsaison vereinbar. Die Gegenrechnung „Vereinsreihenfolge
  ergäbe Augsburg vor Köln“ habe ich nicht selbst nachgerechnet (Annahme aus der RE-Doku).
- Titelzahl 0x4F986–0x4F9DE: `cmp [bp+8],1; jnz; cmp 0x7A9[Liga],1; jng` → Abzug 1, sonst 0 – wie dokumentiert;
  `TableScreen.Title` zeigt damit max(1, gespielte Spieltage), Video `folge06_02238` „1.Spieltag“ nach einem Spiel stimmt.
  Der Code in `TableScreen` ändert sich nur im Kommentar.

### (2) Migration 17 → 18 mit echtem Schema-17-Spielstand

Am **Base**-Commit (Schema 17) mit einem temporären Test drei Spielstände geschrieben (`GameModeTests.Create(7, true, true,
2, 30)`: Saison 1 nach 5 Spieltagen, Saison 1 nach Saisonende, Saison 2 nach 7 Spieltagen). Am **Head** geladen: alle drei
`Success`, Roundtrip byte-gleich, Länder-Bilanz 0, Kopf-an-Kopf 255; danach je zwei Saisons weitergespielt (inkl. WM,
Saisonwechsel, Sichern/Laden nach jeder Saison) ohne Fehler. (Der Saison-2-Stand endet 1995 mit `GameOver`, weil beide
passiven Manager wegen Schulden entlassen werden – Testaufbau, kein Befund.) Temporäre Tests nicht committet.

### (3) Mehrsaisonproben ohne Seed-Abhängigkeit

Seeds in einer temporären Kopie über Umgebungsvariable ersetzt (RSEED = 1, 13, 42) für `DebCupTests`/`EuropeanCupTests`/
`WorldCupTests.MultiSeasonProbe_…` (Modus 4), `GegnerFensterTests.MultiSeason_NationalData_GrowsPerWorldCup` und
`Nationalteam2Tests.MultiSeason_WorldCupRankings_OnePerWorldCup` (Modus 1, 2, 4, passiv/aktiv),
`GameModeTests.WorldOnly_MultiSeason_…` (Modus 1), `Nationalteam2Tests.VacantNationalTeam_…`: je 17/17 bestanden.
`Mode4_ReferenceValues_Unchanged` ist als Abdruck naturgemäß seedgebunden; seine Vorbedingung (Manager 0 im Amt und
spielfähig) wird jetzt täglich geprüft. `TestSquadKeeper` zieht keine Zufallszahlen (nur vertragslose Spieler zum Preis 0,
Auswahl nach Name).

### Gegenproben (Mutation in temporärer Kopie, danach verworfen)

| Mutation | betroffene Tests |
|---|---|
| `StartOrder` ignoriert die Vorsaison | `WettbewerbeStartordnungTests` 3 rot |
| `FinishClubSeason` wieder über `group.Clubs` statt Gruppentabelle | `WettbewerbeStartordnungTests` 2 rot |
| Kappung `MaxWorldCupRankings` entfernt | `WettbewerbeNationalteamTests` 1 rot |
| Migration `[17]` ausgetragen | `GegnerFensterTests` 1 rot (`Schema17_IsMigrated_…`) |

### Akzeptierte `[APPROX]` (getrennt von Annahmen)

- `SuccessorOrder` aus der Abschlusstabelle statt eigener Platzliste (Unterschied nur bei angesehenen Heim-/Auswärtstabellen
  und vollem Gleichstand); Vereine ohne Partner in Vereinsreihenfolge.
- Gegner-Fenster nur für Länder; Vereinsspiele (Code 7) werden nicht eingetragen; dritter Knopf nicht umgesetzt.
- Länder ohne Manager: Kopf-an-Kopf nur für Deutschland.
- Migration 17 → 18 legt die neuen Felder leer an.
- Managervereine vom Oberliga-Austausch ausgenommen (#166, Runde 10).

**Annahmen (nicht selbst geprüft):** Pixelvergleichstabelle der RE-Doku; Gegenrechnung der Gleichstandsreihenfolge aus der
reinen Vereinsreihenfolge; Teil-2-Fundstellen des Fensteraufbaus (`TeamInfoWindow`) außer 0x825E/0x55E39.
