# Review: Integration Runde 12 – Spur saisonwechsel (PR #519)

- **Reviewer:** Claude (Opus 5.5), geplanter Lauf, frische Sitzung ohne Implementierungskontext
- **Geprüft:** Delta `50cac4f42a1092a4babcdd12a3e904f9636121e0..0c1d64494e4e0f6165b27d25ca9ca170de8b11bd` (Teile 1–6 der Spur und Merge-Commit `0c1d644` der Leitstelle)
- **Checkout:** Klon, `git rev-parse HEAD` = `0c1d644…`, `git status --porcelain` leer; PR-Head auf GitHub = `0c1d644`. Originaldaten aus `kaaasi/eishockey-manager-referenz` (`original/`), SDK 10.0.402.

## 1. Merge-Empfehlung

**Ja** für Head `0c1d644`. Alle Gegenbeispiele sind am Base-Verhalten rot und am Head grün (bzw. als Belegtest erwartungsgemäß schon am Base grün, siehe 3.2), die Schema-Nummerierung der Leitstelle ist schlüssig (A184 32 → 33, S-K6 33 → 34; echter Schema-33-Stand vom Base und der Schema-22-Fixture migrieren und laufen 4 Saisonwechsel), alle drei Läufe von `tools/pruefen.sh` bestanden mit denselben Zahlen wie im PR. Offen sind nur P3-Punkte, kein Merge-Hindernis.

## 2. Befunde

**P3-1 – Mehrsaisonprobe der Satzfolge erkennt fehlende Neumischung nicht.**
[`SaisonwechselSatzfolgeTests.cs:106`](https://github.com/kaaasi/eishockey-manager-redone/blob/0c1d64494e4e0f6165b27d25ca9ca170de8b11bd/tests/EishockeyManager.Core.Tests/SaisonwechselSatzfolgeTests.cs#L106)
`MultiSeason_SlotsRemixedEachSeasonChange_MembersFollowPromotion` zählt „neu gemischt“, sobald sich die Folge ändert – das tut sie durch Auf-/Abstieg in jeder Liga ohnehin. Gegenprobe: Mutation `for (var n = 0; n < 0; n++)` in `MixLeagueSlots` → beide Fälle (passiv/aktiv) bleiben grün. Die Neumischung im Saisonwechsel deckt nur `Schema32Save_WithoutSlots_…` ab. Vorschlag: zusätzlich prüfen, dass die Folge nicht gleich der Partnerersetzung ohne Mischung ist (oder Satzplätze der nicht gewechselten Vereine vergleichen).

**P3-2 – Abbruchzweig in `AdvanceLeagueSlots` lässt alte Folge stehen.**
[`SeasonFlow.LeagueSlots.cs:86`](https://github.com/kaaasi/eishockey-manager-redone/blob/0c1d64494e4e0f6165b27d25ca9ca170de8b11bd/src/EishockeyManager.Core/SeasonFlow.LeagueSlots.cs#L86)
Kommentar „Folge nicht führen, Indexfolge gilt“, aber `return` lässt `_leagueSlots` (36 Einträge) unverändert; `LeagueSlots` nutzt dann die veraltete Folge für die verbliebenen Vereine statt der Indexfolge. Nur bei ungewöhnlichen Ligagrößen (synthetisch), Wirkung nur auf die Fair-Play-Reihenfolge. Vorschlag: `_leagueSlots.Clear()` vor `return` oder Kommentar anpassen.

**P3-3 – Doku-Rest der Umnummerierung.**
[`SeasonFlow.LeagueSlots.cs:29`](https://github.com/kaaasi/eishockey-manager-redone/blob/0c1d64494e4e0f6165b27d25ca9ca170de8b11bd/src/EishockeyManager.Core/SeasonFlow.LeagueSlots.cs#L29): „Leer in Spielständen vor Schema 33“ – richtig ist jetzt Schema 34 (Merge-Commit hat nur `SaveGame.cs`/Migration angepasst). Ebenso heißt der Test `Schema32Save_WithoutSlots_…`, prüft aber den Weg 32 → 33 → 34 (funktional korrekt).

Hinweis ohne Befund: Spielstände, die auf dem Spur-Branch mit dem vorläufigen Schema 33 (= Satzfolge, ohne A184) geschrieben wurden, wären nach der Umnummerierung nicht ladbar; das betrifft nur Branch-Stände, keine veröffentlichten.

## 3. Ausgeführte Prüfungen

### 3.1 `tools/pruefen.sh` (alle drei Läufe) am Head

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2049 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1734 | 0 | 283 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1735 | 314 | 0 |

Commit `0c1d644`, SDK 10.0.402, Ergebnis: bestanden.

### 3.2 Gegenbeispiele (rot am Base, grün am Head)

Am Base kompilieren nur die Tests ohne neue API; für die übrigen wurde das Base-Verhalten im Head-Checkout (temporäre Worktree-Kopie) nachgebildet. Alle Läufe mit `EM_REQUIRE_GAMEDATA=1`.

| ID | Ablauf | Base | Head |
|---|---|---|---|
| SW-02 | `SaisonwechselEntlassungTests` (alle Manager am 29. April entlassen, Ewige Tabelle) direkt im Base-Worktree; zusätzlich Mutation `if (day == SeasonCalendar.SeasonEnd)` → `if (false)` | 2/2 rot (beide Wege) | grün |
| SW-05 | `DoubleMove_PartnerHasSamePairIndex`; alte Regel (Base-`ResolveDoubleMoves` über Listenindex) ist hier korrekt, Test pinnt den Paarindex – Mutation „immer erster Absteiger“ | rot unter Mutation; unter Base-Regel grün (erwartet, Commit `2b67607`) | grün |
| SW-06 | `DoubleMove_DoublePlace_*` (doppelter Platz); Base-`ResolveDoubleMoves` eingesetzt | 2/2 rot | grün |
| S-K6 | `SaisonwechselSatzfolgeTests` (Permutation beim Spielstart, Mischung im Saisonwechsel, Determinismus ohne Weiterschalten von `Random`, Fair-Play-Gleichstand nach Satzplatz); Mutation ohne die 55 Tausche (Base = Indexfolge) | 4 rot (Spielstart, Saisonwechsel nach Schema-32-Stand, Fair-Play, Determinismus); Mehrsaisonprobe grün → P3-1 | grün |
| E5/S-K1 | `SaisonwechselKontoTests` (Guthaben gekürzt, Schulden gehoben, 2. Liga unverändert) direkt im Base-Worktree | grün – erwartet: Teil 3 belegt bestehendes Verhalten (nur Kommentare im Code), kein Fehler vorher | grün |
| D5 | `SaisonwechselDoppelwechselAnzeigeTests`; `ShownRelegatedInto`/`FirstExchangeRelegatedInto` auf `RelegatedInto[league]` (Base-Anzeige und -Startordnung) | 4 rot (Spalte „Zweite Liga“, Startordnung 2. Liga und Bundesliga, ohne Ereignis) | grün |
| D3 | `SaisonwechselDoppelwechselOriginalTests` (FILE16A/FILE17) grün am Head und unter Base-Regel (Fall ohne doppelten Platz war schon richtig, Test ist Beleg); roter Teil ist Symbol 0: `EventIcon_ForcedRelegation_IsSymbolZero` mit entfernter Zeile `ForcedRelegation => Symbol(0)` | Symbol-Test rot | grün |

D5 gegen das Referenzbild `original/spielstaende/bilder/file16_8_saisonende.png` abgeglichen: Bundesliga blau Krefelder EV, grün SV Bayreuth, kein Rot; Zweite Liga rot SC Riessersee, grün SC Riessersee und ESC Wolfsburg; Oberliga rot EV Landshut und SC Memmingen. Das entspricht `SeasonEndWindow_SecondLeagueColumn_ShowsClubOnSwappedPlace` (Riessersee rot in der 2. Liga, Landshut/Memmingen rot in der Oberliga, Bundesliga leer) und der einzigen Änderung in `SeasonEndScreen.cs` (`ShownRelegatedInto`). Kein gerendertes Bildschirmfoto verglichen (Datenabgleich, Lage/Farben unverändert).

### 3.3 Besonders geprüft

- **Schema-Nummerierung:** `SaveGame.cs` Migrationen `[32] = MigrateWettbewerbeSpiel12`, `[33] = MigrateSaisonwechsel3`, `SchemaVersion = 34`, Fingerabdruck in `SaveGameTests` angepasst; Merge-Commit ändert gegenüber `bfb1fe2` nur Nummerierung und Kommentare dieser Dateien. Echter **Schema-33-Spielstand vom Base** (`50cac4f`, Seed 11, ein Spieltag; temporärer Test, nicht committet) am Head geladen: Erfolg, Satzfolge = Indexfolge, Speichern/Laden byte-gleich, danach 4 Saisonwechsel (passiv) mit 12 Plätzen je Liga, Folge danach gemischt. **Schema 22** (`SaisonwechselSchema22Tests`, Fixture aus Teil 4) läuft über die ganze Kette inkl. 33 → 34 und 4 Saisonwechsel: grün.
- **Mehrsaisonprobe:** 4 Saisonwechsel passiv und aktiv in `SaisonwechselSatzfolgeTests.MultiSeason_…` (Schwäche P3-1), Doppelwechsel über mehrere Saisons mit Ligen 12/12/12 in `SaisonwechselDoppelwechselTests.MultiSeason_DoubleMoveRecurs_LeaguesStay12` (mit/ohne Europa), Schema-22-Probe – alle grün.
- **Bildschirme lesen nur:** `FairPlayTable_IsReadOnly` (auch `LeagueSlots`) grün; `SlotSeed` liest `Random.Capture()` ohne Weiterschalten (`Mixing_IsDeterministic_AndDoesNotAdvanceGameRandom`).

### 3.4 Akzeptierte `[APPROX]`

- Zufallsquelle der Ligamischung aus dem Zustand von `GameState.Random` abgeleitet, ohne ihn weiterzuschalten (sonst verschöben sich alle übrigen Verläufe).
- Zeitpunkt der Mischung in `StartNextSeason` statt vor dem Fenster „Saisonende“ (Wirkung nur Fair-Play-Gleichstand um Platz 1).
- Migration älterer Stände: Satzfolge leer, Indexfolge bis zum nächsten Saisonwechsel.
- SW-02: Abschluss der Saison bei Entlassung aller Manager am 29. April (Entlassung selbst nicht aus em.exe belegt).
- Doppelwechsel mit doppeltem Platz und Paarindex ohne Ereignis (synthetisch) – ohne DOSBox-Beleg, nachvollziehbar aus dem Disassembly begründet.

**Annahmen (nicht geprüft):** Fundstellen im Disassembly (0x35EDD, 0x5629D–0x562DE usw.) nicht selbst nachgelesen; FILE16A/FILE17-Auswertung über die Tests übernommen.
