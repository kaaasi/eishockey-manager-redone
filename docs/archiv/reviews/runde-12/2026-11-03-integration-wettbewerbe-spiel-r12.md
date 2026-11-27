# Review PR #508 – Integration Runde 12, Spur wettbewerbe-spiel

- **PR:** #508 · **Base:** `d5bf0643beb179609dceb5eaf1ef045a7869d78c` · **Head:** `c141fb60a5a82b799c92f6b3f8d046c9590036c6`
- **Reviewer:** Claude Code (geplanter Lauf, eigene Sitzung ohne Implementierungskontext; nicht an #482/#491/#495/#503/#508 beteiligt)
- **Checkout:** Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer; Ausführung am nachgewiesenen Checkout.

## 1. Merge-Empfehlung

**Nein für genau diesen Head – nur wegen R12I-01 (P2).** Die Gegenbeispiele A184, A174 und U8 sind am Base rot und am Head
grün, alle drei Läufe von `tools/pruefen.sh` am Head bestanden. Die eigenen Änderungen der Leitstelle sind korrekt: Werden die
Quelltextänderungen von geld Teil 2 (`6f622f6`, 0x4D316) am Head zurückgenommen, liefern alle neu ermittelten aktiven Abdrücke
und `Mode4Reference` exakt die Werte des Stapelendes `6a831a1`. Die Begründung „nur geld Teil 2 × wettbewerbe-spiel“ ist damit
belegt. Schema 33 und die Migration sind in Ordnung. Ist R12I-01 behoben (Aufrufer umgestellt) oder vom Nutzer ausdrücklich
zurückgestellt und in `docs/arbeitspakete.md` eingetragen, habe ich keine Einwände gegen den Merge.

## 2. Befunde

### R12I-01 (P2): Aufrufer der Belastung nicht umgestellt, Restlücke nicht übertragen

PR #491 (Teil 2) übergibt ausdrücklich „für die Integration (fremde Dateien, nicht geändert)“: neue Kaderplätze sollen
`Strain = random(80, 120)` setzen, die Spalte E soll über `TrainingRules.ExhaustionDisplay`/`ExhaustionWarning` laufen. Die
Integration setzt das nicht um, und weder PR #508 (Restlücken) noch `docs/arbeitspakete.md` führen es auf (Merge-Prüfung §6 Nr. 6).
Seit A184 ist `Strain` das Originalbyte. Die Übergangseigenschaft `Energy` (`set => Strain = 150 - value`,
[Player.Attributes.cs:71](https://github.com/kaaasi/eishockey-manager-redone/blob/c141fb60a5a82b799c92f6b3f8d046c9590036c6/src/EishockeyManager.Core/Player.Attributes.cs#L71))
macht daraus zwei sichtbare Abweichungen:

- **Neue Kaderplätze:** [FinanceTalent.cs:143](https://github.com/kaaasi/eishockey-manager-redone/blob/c141fb60a5a82b799c92f6b3f8d046c9590036c6/src/EishockeyManager.Core/FinanceTalent.cs#L143)
  und [Transfer.Pool.cs:208](https://github.com/kaaasi/eishockey-manager-redone/blob/c141fb60a5a82b799c92f6b3f8d046c9590036c6/src/EishockeyManager.Core/Transfer.Pool.cs#L208)
  setzen `Energy = Clamp(random(80, 120), 1, 99)` und damit `Strain = 150 − min(x, 99)`, also 51 … 70 statt 80 … 120
  (Datei 0x42E4C–0x42E5E). Talente und Spieler vom Markt sind deshalb frischer als im Original; die Spielvorbereitung
  (101 − Belastung/8) und die Trainingswirkung lesen das Byte direkt.
- **Spalte E widersprüchlich:** Nur `NationalLineupScreen` zeigt Belastung · 10 / 15. `LineupScreen.cs:276`,
  `ContractsScreen.cs:275` und `TransferScreen.cs:382` zeigen weiter `Energy` (150 − Belastung), `PlayerHintLine.cs:131`
  und `PlayerInfoPopup.cs:85` rechnen ebenfalls mit `Energy`. Gegenbeispiel: Nach dem ersten Freundschaftsspiel hat ein
  Kaderspieler `Strain = 70`. In der Nationalaufstellung steht E = 46 (DOSBox-Beleg), in der Vereinsaufstellung desselben
  Spielers E = 80. Die Werte laufen in entgegengesetzter Richtung.

Abhilfe: die sieben Stellen aus der Liste in PR #491 umstellen (klein, mechanisch). Alternativ Restlücke mit Issue eintragen
und vom Nutzer zurückstellen lassen.

### R12I-02 (P3): Ausnahme Nationalteam bei Belastung + 1 ungetestet

[MatchEngine.Managed.cs:380](https://github.com/kaaasi/eishockey-manager-redone/blob/c141fb60a5a82b799c92f6b3f8d046c9590036c6/src/EishockeyManager.Core/MatchEngine.Managed.cs#L380):
Mutation `&& !squad.NationalCoach` entfernt → alle 144 Tests der Filter `National|WettbewerbeSpiel12|Friendly|WorldCup|Bundestrainer`
bleiben grün. Das `[APPROX]` (im Original trifft das + 1 nur die Kopie im Satz des Bundestrainers) ist plausibel, aber nicht
abgesichert. Vorschlag: Test „Länderspiel des Bundestrainers ändert die Belastung der Vereinsspieler nicht“.

### Anmerkung (kein Befund)

Ohne die Begrenzung nach dem Spiel (`ClampStrain` in `Credit`) wird nur `SingleManagedMatch_ClampsWholeSquad` rot.
`ManagedMatches_RaiseStrain_ClampAfterMatch` und die Mehrsaisonprobe bleiben grün, weil der Trainingstag ebenfalls auf
50 … 150 begrenzt. Ein Test reicht als Absicherung.

## 3. Ausgeführte Prüfungen

**Einrichtung:** `tools/claude/fetch-reference.sh original`, `tools/claude/setup-workspace.sh ~/referenz/original/gamedata`,
SDK 10.0.402. Zugriff auf das Referenz-Repo per `git ls-remote` ok.

**`tools/pruefen.sh` (alle drei Läufe) am Head `c141fb6`:**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2019 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1706 | 0 | 281 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1707 | 312 | 0 |

Ergebnis: bestanden. CI am Head: `build` success.

**Gegenbeispiele:** eigene Probe-Datei (nur lokal, nicht committet). Sie nutzt nur APIs, die an Base und Head bestehen
(`Energy` statt `Strain`), und lief am Base `d5bf064` und am Head jeweils in einem eigenen Worktree.

| Probe | Ablauf | Base | Head |
|---|---|---|---|
| A184 Grenzen | Managerspiel, zwei Bankspieler mit Belastung 220 und 10 → danach 150 und 50 (0x549A2) | rot (−70 statt 0) | grün |
| A184 + 1 | alle Belastung 100, ein Managerspiel → Belastung mindestens eines Eingesetzten über 100 (0x8C82) | rot | grün |
| A174 | Computerverein, 8 stärkste Aufgestellte entfernt → `ClubFieldValues` unverändert, = Datenbankstärke | rot (21 statt 85) | grün |
| U8 (Seeds 3, 11) | Bundestrainer, unterschiedliche Belastung → `AutoFormation` = `AutoLineupRule.Apply(Rows)` (0x426BC/0x42838) | rot | grün |

**Finanzprobe nach D4 = A:** Die erweiterte Ausnahme in `FinanceCalibrationTests` gilt nur passiv in der Bundesliga. Sie
lässt genau Entlassung nach der Schuldenregel und Strafen als Vielfaches von 200.000 DM bei gesehenem „nicht spielfähig“ zu.
Mindestens ein Seed muss vier Saisons schaffen. Die Entscheidung des Nutzers ist in `docs/arbeitspakete.md` (Spur
wettbewerbe-spiel) und im Kommentar vermerkt. Akzeptiert.

**Eigene Änderungen der Leitstelle:**
- `TeamValue(game, club)` in `StadiumStrengthTests`/`WettbewerbeSpiel4Tests` (und `SpielstaerkeManagerTests`): nur die
  Signatur; für Computervereine `ComputerTeamValue`, wertgleich. Ok.
- Sollwerte: geld Teil 2 (`6f622f6`, nur `src/`) am Head zurückgenommen → `RngTwinProbeTests` aktiv Seed 3 `59E0F208EF82DC89`,
  Originaldatenbank aktiv `2C1300868BBBCBC6`, `SpielberechnungAnschlussTests` aktiv `08AD683AD3B11EAE` und `Mode4` Saison 1
  `309266` = Werte von `6a831a1`. Die passiven Abdrücke und der aktive Abdruck Seed 11 sind ohnehin gleich. Die Begründungen
  stimmen.
- `JugendTeamsTests`: Der Abdruck ist neu. Die Richtungsprüfung (aktiv ≥ +15, passiv ≤ −15) steht unabhängig vom Sollwert
  im Test und ist erfüllt (Endwerte aktiv 81 … 83, passiv 40 … 42).

**Gegenproben (Mutation am Head, temporärer Worktree):**

| Mutation | rot |
|---|---|
| `ClampStrain` nach dem Spiel entfernt | `SingleManagedMatch_ClampsWholeSquad` |
| Migration setzt `Strain = 150` statt `150 − Energy` | `Migration_32To33_StrainFromEnergy` |
| Belastung + 1 entfernt (Zug bleibt) | `ManagedMatches_RaiseStrain_ClampAfterMatch` |
| `TeamValue`/`ClubFieldValues` für Computervereine wieder über die Aufstellung | alle 4 Tests von Teil 3, einschließlich der Mehrsaisonprobe (passiv und aktiv) |
| `!squad.NationalCoach` entfernt | keiner (R12I-02) |

**Schema/Migration:** 32 → 33, Eintrag `[32] = MigrateWettbewerbeSpiel12`, Fingerabdruck neu. Gespeicherte Stände enthalten
kein `Energy` mehr. Die Migration übernimmt 150 − Energie, also die bisherige Ableitung: alte Stände verhalten sich
unverändert, auch nach „WM Vorbereitung“ (alt Energie 70 → Belastung 80). Mehrsaisonprobe A184 und A174: je 4 Wechsel,
passiv und aktiv, Sichern und Laden erhalten die Belastung. Grün in allen Läufen.

**Akzeptierte `[APPROX]`:** `Player.Energy` als abgeleitete Übergangseigenschaft (nur zusammen mit R12I-01), `DefaultStrain`
150, Nationalteam ohne + 1, Sortierung innerhalb der Blöcke nach Rückennummer (U8), Belastung 70 bei „WM Vorbereitung“
nur beim ersten Freundschaftsspiel.

**Annahmen (nicht geprüft):** Die Fundstellen in em.exe (0x8C82, 0x549A2, 0x426BC, 0x977F) habe ich nicht neu disassembliert.
Ich habe sie aus Doku und Code übernommen.
