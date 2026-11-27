# Review: Integration Runde 12 – Spur kader-transfer (PR #526)

- **Reviewer:** Claude Code (geplanter Lauf, frische Sitzung ohne Implementierungskontext, Claude Opus 5.5)
- **PR:** #526 · **Base:** `f52ffa7186c13e6d5275d3951b3212e77a6ae83a` · **Head:** `1a1a80f19dd57db5c1d8494d8758b544deb2c51b`
- **Checkout:** Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer (vor und nach dem Lauf). Gebaut und ausgeführt,
  nicht nur Codelesen. Originaldaten aus `kaaasi/eishockey-manager-referenz` (`original/`, Zugriff per `ls-remote` geprüft).

## 1. Merge-Empfehlung

**Ja, für genau diesen Head.** Alle fünf Gegenbeispiele sind am Stand vor der Spur bzw. vor #524 rot und am Head grün; die
neu ermittelten Sollwerte der Leitstelle sind vollständig durch die Produktänderungen der Spur erklärt (Gegenprobe unten);
#525 weicht die Mindestkader-Prüfung nicht auf; alle drei Läufe von `tools/pruefen.sh` am Head bestanden. Befunde nur P3.

## 2. Befunde

Keine P0–P2.

- **P3-1 Neuanlage im Poolplatz ohne gezielten Test.** [`Transfer.Squads.cs` Z. 311](https://github.com/kaaasi/eishockey-manager-redone/blob/1a1a80f19dd57db5c1d8494d8758b544deb2c51b/src/EishockeyManager.Core/Transfer.Squads.cs#L311)
  (`RenewAgedPoolPlayer`, Aufruf `Transfer.Pool.cs` Z. 559). Gegenprobe: Aufruf auskommentiert → alle Tests der Spur
  (`KaderTransfer*`, `SquadLifecycle*`, `*Pool*`) grün, rot werden nur die vier synthetischen Abdrücke `RngTwinProbeTests`.
  Das Verhalten selbst stimmt (Ad-hoc-Probe: freier Poolplatz außerhalb jedes Kaders, Alter 60 → nach `StartNextSeason`
  neuer Spieler 18–25 im selben Platz, alter in `RetiredPlayers`; mit Mutation rot `Assert.NotSame`). Vorschlag: diese Probe
  als Test übernehmen (Runde 13, Spur kader-transfer).
- **P3-2 Veralteter Kommentar.** [`Transfer.Pool.cs` Z. 537](https://github.com/kaaasi/eishockey-manager-redone/blob/1a1a80f19dd57db5c1d8494d8758b544deb2c51b/src/EishockeyManager.Core/Transfer.Pool.cs#L537)
  sagt „Alter + 1 (ohne Laufbahnende …)“, drei Zeilen unter dem Aufruf von `RenewAgedPoolPlayer`. Die Nutzerentscheidung betrifft die
  Verjüngung beim Umbenennen, nicht das Laufbahnende (0x2787A); Satz anpassen.
- **P3-3 Testhelfer verlängern auch ohne Angebot.** [`TestSeason.cs` Z. 35](https://github.com/kaaasi/eishockey-manager-redone/blob/1a1a80f19dd57db5c1d8494d8758b544deb2c51b/tests/EishockeyManager.Core.Tests/TestSeason.cs#L35)
  und [`KeepSquadHelper.cs` Z. 23](https://github.com/kaaasi/eishockey-manager-redone/blob/1a1a80f19dd57db5c1d8494d8758b544deb2c51b/tests/EishockeyManager.UI.Tests/KeepSquadHelper.cs#L23):
  Seit U4 verlängert `KeepSquad` jeden Spieler im letzten Vertragsjahr ohne Sperre um zwei Jahre (Standard `extendExpired = true`).
  Begründet (Angebote verfallen jetzt), aber die „passiven“ Mehrsaisonproben, die `KeepSquad` nutzen, halten den Kader damit
  stärker fest als vorher; Kaderschwund des Managers zeigen sie kaum noch. Kein Fehler, nur beim Lesen der Proben beachten.
- **P3-4 Doku-Stand U15.** [`docs/transfer.md` Z. 481](https://github.com/kaaasi/eishockey-manager-redone/blob/1a1a80f19dd57db5c1d8494d8758b544deb2c51b/docs/transfer.md#L481)
  nennt den U15-Rest „Entscheidung nötig“; laut PR ist D7 = D gefallen (Umsetzung Runde 13). Beim Nachziehen in Runde 13 angleichen.

Erwartet offen, bestätigt: **U15-Rest** (Start-Liga-Regel, D7 = D, Runde 13) – im Code nicht umgesetzt, fest gewählter Verein
bleibt (`Create_FixedClubKeepsChosenEuropeanCupParticipant`).

Geprüft ohne Befund: Spielplantabelle in `NewGame.cs` byte-gleich mit em.exe (Datei 0x17190, 11 Zeilen „abcdefghijkl“ …
„iabdchfkeglj“); `src/`-Diff von #526 inhaltsgleich mit `af907c5..3e9b937` (Spur) plus #524; ein Gebot je Marktplatz
(`_bids.Any(b => b.Listing == listing)`), daher entspricht der Wurf je Gebot dem Wurf je Platz; neues Angebot direkt nach dem
Verfall am selben Tag ist wie im Original möglich (Bedingung dort nur Pool +0x1E = 0, `re-nachtrag.md` §1 Nr. 6). Die
Ablageänderung #524 (`if (counts.Count > 0)`) schreibt alle Gruppen nur bei einem Zwangsabsteiger neu – schlüssig, weil sich
die Tabellen vom 13./29. April sonst nicht unterscheiden.

## 3. Ausgeführte Prüfungen

Umgebung: `tools/claude/fetch-reference.sh original`, `tools/claude/setup-workspace.sh`, SDK 10.0.402.

**`tools/pruefen.sh` (alle drei Läufe) am Head `1a1a80f`:** Ergebnis bestanden.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2080 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1755 | 0 | 289 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1756 | 324 | 0 |

**Gegenbeispiele** (rot: Worktree `0c1d644` = Integrationsstand vor der Spur, mit den Testdateien des Heads; bei
`KaderTransferNachtraegeTests` ohne die zwei Tests auf neue APIs (`CareerEnds`, `CreateRenewal`) und mit `internal` statt
`private` für drei Helfer in `SquadLifecycleTests` – sonst nicht übersetzbar; grün: Head, voller Lauf oben):

| ID | Tests | vorher | Head |
|---|---|---|---|
| U4 | `KaderTransferNachtraegeTests.OfferedExtension_Expires_…`, `UnsolicitedOffer_Expires`, `MarketBid_Expires_…` | 3 rot | grün |
| U5 | `…ComputerPlayerAtAge60_Retires_…`, `MultiSeason_FourChanges_ComputerPlayersRetireAfterAge37To42` (4 Fälle) | 5 rot | grün |
| U9 | `ScheduleTests.Rounds_TwelveClubs_UseFixedOriginalTable`, `…FirstHalfHas66DistinctPairings…` | 2 rot | grün |
| U15 | `NewGameTests.Create_RandomBundesligaClub_NeverTakesEuropeanCupParticipant` (Wächter `Create_FixedClub…` grün) | rot | grün |
| E2 | `LineupShortMessagesE2Tests` (5 von 6; `Field_ClickOnHealthyPlayer…` ist Wächter) | 5 rot, u. a. `["MAC MILLAN","noch gesperrt !"]` statt `["Spieler ist","Gesperrt."]` | grün |
| #524 | `SaisonwechselDoppelwechselTests.MultiSeason_DoubleMoveRecurs_LeaguesStay12(europe: True)` (Ligen 11/13), `SaisonwechselAustauschStandTests.ForcedPromotedClub_…` (`[12]` statt `[12, 16]`) am Worktree `574cc9a` | 2 rot | grün |

**Sollwerte der Leitstelle (`9d98111`)**: Am Head `src/` und die drei Testhelfer (`TestSeason.cs`, `TestSquadKeeper.cs`,
`KeepSquadHelper.cs`) auf `0c1d644` zurückgesetzt (plus `TransferOriginalVertragTests.cs`, nutzt `extendExpired`; neue
Testdateien der Spur entfernt): alle betroffenen Tests liefern **exakt die alten Sollwerte** – `RngTwinProbeTests` (4 synthetisch
`8D3B…`, `0928…`, `D052…`, `A887…`; 2 Originaldatenbank `3A0F…`, `96F3…`), `SpielberechnungAnschlussTests` (`34A3…`, `1B38…`,
WorldOnly `R540662232`), `JugendTeamsTests` (beide), `GameModeTests.Mode4Reference`, `DebCupDrawFlowTests.Reference`. Die
Testdateien selbst ändern sich nur in Kommentaren und Sollwerten. Nur #524 zurückgesetzt: alle 12 Core-Sollwerttests bleiben
grün – die neuen Werte kommen allein aus kader-transfer.

**#525 (Kaderprobe):** nur `SquadLifecycleTests.cs`, Grenze `>= 18` unverändert; der aktive Testmanager spielt höchstens 60
Schritte über `game.Continue()` weiter und kauft nach. Begründung (Markt zum Saisonstart leer, Listenwechsel erst im
Tagesablauf 0x54E65; kleiner Kader im Original erlaubt bis „Weniger als neun Spieler!“) schlüssig, kein Produktfehler
verdeckt. Gegenprobe zur Absicherung: ohne Sperrrücksetzung beim neuen Angebot (`Transfer.Squads.cs`, `NegotiationLock = 0`)
werden vier aktive Mehrsaisonproben rot, darunter `SquadLifecycleTests.MultiSeason_SixChanges_OriginalDatabase(active: True)`.

**Mehrsaisonprobe:** `MultiSeason_FourChanges_ComputerPlayersRetireAfterAge37To42` (4 Wechsel, passiv/aktiv, Seed 3/11),
`KaderStartTests`, `KaderTransferWs05Tests` (synthetisch und Originaldaten), `SaisonwechselDoppelwechselTests` – grün.

**Akzeptierte `[APPROX]`:** Mischung vor dem festen Spielplan nur für den Zufallsverbrauch (`NewGame.cs`); Kreisverfahren als
Rückfall für ≠ 12 Vereine und Play-offs; Europapokal-Filter nur mit dem Meister (Verein 2); Nachfolger mit neuem Namen/ID statt
Umbenennung des Pooleintrags, Form/Talent/Energie ergänzt (`CreateRenewal`); Zufall der Verträge statt Hauptquelle beim Laufbahnende;
E2 nur Linksklick.

**Annahmen (nicht selbst geprüft):** Fundstellen der Bedingungen in em.exe (0x0767A, 0x07626, 0x07215, 0x2787A, 0x40B37–0x40B55,
0x560EB–0x563A4) nicht nachdisassembliert; die Texte „Spieler ist“, „noch gesperrt !“, „Gesperrt.“, „Verletzt.“, „ist deaktiviert !“
stehen in em.exe (Datei 0x1ACD4, 0x1AE6E, 0x193D3, 0x193C9, 0x1AE5C).
