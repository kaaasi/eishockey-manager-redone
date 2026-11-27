# Review PR #728 – Abgleich Remake ↔ Original (Spur `abgleich-original`)

Geprüft: Head `c44d529e2426932c200d751518f61a95a9b82ec5` gegen Base `3af4ec9ba3b530ee2efb81ab4849e4d81eaa9b8e` (= `main`).
Reviewer: Claude Opus 5.5, frische Sitzung (geplanter Lauf), nicht an der Spur beteiligt. Checkout nachgewiesen
(`git rev-parse HEAD` = Head, `git status --porcelain` leer), Originaldaten und Spielstände aus dem Referenz-Repo.

## 1. Merge-Empfehlung

**Nein für diesen Head** – wegen P2-1 (nach `zusammenarbeit.md` §6 Nr. 5 vor dem Merge beheben oder vom Nutzer zurückstellen).
Werkzeug, Abdruck und Halt-Logik arbeiten; die Gegenbeispiele und die Belege von #729/#734 sind nachgestellt; alle
„deterministisch“ eingestuften Felder sind über fünf Saaten stabil. Aber der Geldbereich ist so weit, dass ein grober
Buchungsfehler (alle Gehälter fehlen) in keinem der elf Paare rot wird – das Netz hält für Geld nicht, was Bericht und
Testkommentar versprechen. Fix klein (siehe P2-1); danach Delta-Prüfung der Leitstelle genügt (nur P2/P3).

## 2. Befunde

**P2-1 Geldbereich ±25 % auf den Kontostand verdeckt grobe Fehler** –
[`HokCompareClasses.cs` Z. 159](https://github.com/kaaasi/eishockey-manager-redone/blob/c44d529e2426932c200d751518f61a95a9b82ec5/src/EishockeyManager.Tools/HokCompareClasses.cs#L159)
(`Set(Range(0, 0.25), "Manager.Money")`). Der Bereich bezieht sich auf den Absolutstand von b, nicht auf die Veränderung seit a;
bei Konten in Millionenhöhe sind ±25 % mehr als eine ganze Saison Gehälter.
Beleg (Mutation in temporärer Kopie, `GameState.Book`: `if (category == FinanceCategory.Gehaelter) return;`):
`EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.Core.Tests --filter FullyQualifiedName~AbgleichOriginalPaareTests`
→ **Passed 13/13** (erwartet: rot). Konten mit Mutation (`emtool abgleich … --alle`, Original → Remake):
FILE22→23 −3.475.588 → −2.679.063 (+23 %), FILE16A→17 −2.850.819 → −2.236.007 (+22 %), FILE44→46 15.887.669 → 17.402.971
(+10 %), FILE02→03 737.354 → 720.796. Vorschlag: Veränderung vergleichen (Remake b−a gegen Original b−a, Bereich auf die
Veränderung bzw. fester Deckel in DM), Gegenprobe „Gehälter fehlen“ als Test oder im Bericht.

**P3-1 Breite Pfadmuster der erwarteten Listen verdecken neue Abweichungen** –
[`AbgleichOriginalPaareTests.cs` Z. 38–51](https://github.com/kaaasi/eishockey-manager-redone/blob/c44d529e2426932c200d751518f61a95a9b82ec5/tests/EishockeyManager.Core.Tests/AbgleichOriginalPaareTests.cs#L38-L51).
`Clubs[7 EV LANDSHUT].Players*` (FILE20→21) nimmt den ganzen Managerkader samt Alter/Vertrag aus,
`Clubs[*].Players[*].Strain` alle Kader; „trifft nicht mehr“ verlangt nur einen Treffer je Muster.
Beleg: Rückmutation `Player.Contract.cs` Z. 136 `player.Age += 2` → rot in FILE13→14 (20 neu), FILE16A→17 (22), FILE22→23 (22,
auch Saat 4711), FILE44→46 (24) – **grün in FILE20→21**. Rückmutation `Squad.Training.cs` Z. 345 Erholung halbiert →
rot nur FILE13→14 (11) und FILE44→46 (1); FILE16A→17, FILE24→25, FILE38→39 (Muster `Strain`) bleiben grün.
Vorschlag: Muster auf die belegten Felder einengen (FILE20→21 z. B. `…Players[*].Age|Strain|…` statt `Players*`) oder je Muster
die erwartete Trefferzahl festhalten.

**P3-2 Bereiche saatempfindlich** – Bericht „Probe mit Saat 4711 zeigt dieselbe Liste“ gilt nur für die zwei getesteten Paare.
Beleg: `emtool abgleich` je Paar mit `--seed 1 2 3 77`, Abweichungen gegen die erwarteten Muster abgeglichen
(Python-Abgleich der `ABWEICHUNG`-Zeilen): neue Abweichungen in 5 von 35 Fällen – FILE02→03 s3 `Invariante.PoolMittel.Strain`;
FILE13→14 s2/s3 `KREFELDER EV.Players[J.REIL|THOMSON].HiddenSkill1/2`; FILE44→46 s1 `KÖLNER EC.TeamValues.DefenseForm`,
s77 `…AttackForm`. Kein CI-Flattern (Saat fest), aber jede Änderung der Zufallsreihenfolge kann die Tests ohne Fehler rot
machen. Vorschlag: Bereiche an mehreren Saaten nachziehen oder diese Fälle im Bericht als bekannte Grenze nennen.

**Bestätigt offen (wie erwartet, nicht in dieser Spur zu beheben):**

- **#729 (P1)** nachgestellt: `emtool abgleich FILE02.HOK FILE03.HOK` → `KÖLNER EC.AttackCondition 80 → 9`,
  `DÜSSELDORFER EG.AttackCondition 93 → 3`, `EISBÄREN BERLIN.AttackTechnique 75 → 25` (genau die Issue-Werte);
  `OriginalImport.cs` `ApplyTeamValues` schreibt die Bytes 0x18–0x1B absolut, `MatchEngine.TeamValues.cs` Z. 15/107 führt
  Abweichungen – Einordnung „Importlücke“ plausibel.
- **#734 (P2)** nachgestellt: FILE13→14 Konto 183.787 → 364.736. Zusatz: mit Saat 1/2/3/77 340.239 / 347.066 / 346.652 / 348.332 –
  die Abweichung (+156 … +181 Tsd. DM) ist saatstabil, spricht eher für einen systematischen Fehler als für Zufall.
- #699, #730–#733 erscheinen in den Ausgaben wie in den erwarteten Listen (FILE13→14: `DayOpened`, `FinanceDay`, `TrainedUntil`,
  `RankSlotsRecorded`, `PoolMittel.Strain/Technique`, `KREFELDER EV._autoLineup`; FILE02→03: Belastung HOCK/MENDE/SIKORA 50 → 122…132).
- Erneuerung der erwarteten Listen gegen den dann aktuellen `main` vor dem Merge: bestätigt als offen (`main` = Base, noch nichts zu tun).

## 3. Ausgeführte Prüfungen

**Gegenbeispiele** (`dotnet run -c Release --project src/EishockeyManager.Tools -- abgleich <a> <b> --gamedata gamedata`):

```
FILE02→FILE03  rc=4  Zeilen 45989: deterministisch 12588, zufallsabhängig 33401, Eingriff 0, nicht eingeordnet 0; Abweichungen 74
FILE13→FILE14  rc=4  Zeilen 45623: deterministisch 10252, zufallsabhängig 35371, Eingriff 0, nicht eingeordnet 0; Abweichungen 130
```

Abweichungen außer Teamwerten FILE13→14: `KREFELDER EV._autoLineup False→True`, `DayOpened True→False`, `FinanceDay 0→-1`,
`PoolMittel.Strain 145.2→86.8`, `PoolMittel.Technique 58.3→53.2`, `Managers[0].Money 183787→364736`, `RankSlotsRecorded 0→44`,
`TrainedUntil (1994,0)→(1993,231)` – deckt sich mit dem Bericht (74 bzw. 130 Abweichungen wie angegeben).

**Besonders prüfen:**

1. *Feldklassen:* Alle elf Paare mit fünf Saaten (1993, 1, 2, 3, 77): keine Abweichung eines `deterministisch` eingestuften
   Feldes ändert sich mit der Saat (Unterschiede nur in Bereichsfeldern, P3-2) – die Einstufung hält empirisch. Bereiche:
   Geld verdeckt grobe Fehler (P2-1); Spielerwerte/Pool siehe P3-2.
2. *Rückmutation:* `Age += 2` am Saisonwechsel → 5 Fälle rot (4 Paare + Saat 4711); Erholung halbiert → 2 rot;
   Gehälter weg → 0 rot. Die Theorie wird also rot, aber nicht flächendeckend (P2-1, P3-1).
3. *`Core.Tests.csproj`:* `Compile Include="..\..\src\EishockeyManager.Tools\HokCompare*.cs"` baut sauber (nur Core/Formats als
   Abhängigkeit, kein Typkonflikt, da Core.Tests das Tools-Projekt nicht referenziert); künftige `HokCompare*`-Dateien landen
   automatisch mit drin – im Kommentar steht es. Alle drei Läufe:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2647 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2084 | 0 | 455 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2085 | 562 | 0 |

`tools/pruefen.sh` (ohne Option): Commit c44d529, SDK 10.0.402, **bestanden**. CI des PR: `build` success.

**Akzeptierte `[APPROX]`:** Ablauf „ohne Eingriff“ (`HokCompare.cs` Z. 29–33, Fragen/Angebote unbeantwortet), Bereiche als erste
Schätzung (außer Geld, P2-1), Import von b als geladener statt gesicherter Stand.
**Annahmen (nicht geprüft):** Pfadmuster der Eingriffe FILE44→46/FILE18→48 gegen die README der Spielstände nur gelesen, nicht
einzeln nachgestellt; Mutationen nur in temporärer Kopie (`git worktree`), am PR nichts geändert.
