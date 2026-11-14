# Review PR #842 – Spur `nutzer-befunde` (#834–#841)

- **Geprüft:** Head `b41507db74f5035401c4b6cd2b47c13a9a3398b5` gegen Base `4e55e076eeedec61e9656e508515c0dba3eb7cd9` (= `main`),
  sauberer Checkout (`git rev-parse HEAD` = Head, `git status --porcelain` leer), SDK 10.0.402, Originaldaten aus dem Referenz-Repo.
- **Reviewer:** Claude Opus 5.5, geplanter Lauf ohne Implementierungskontext (andere Sitzung als die drei im PR).

## 1. Merge-Empfehlung

**Nein** für `b41507d`. Ein P1: Nach dem Laden eines Spielstands stürzt „Neuer“ → „Los jetzt!“ mit einer Ausnahme ab
(`AddManager` verlangt eine `List<Manager>`, der Spielstandleser liefert ein Array). Laden ist der Normalfall; der Kernpunkt
von #841 („Laden/Speichern mit neuem Manager“) ist damit nur für den Weg „neu starten → hinzufügen → sichern“ belegt. Alle übrigen
Gegenbeispiele sind behoben; #837 verschiebt keine Werte (Probe unten). Nach Behebung von P1-1 genügt eine Nachprüfung des Deltas.

## 2. Befunde

### P1-1 – Hinzufügen nach dem Laden wirft, Spielvorbereitung bleibt halb geändert

[`GameState.AddManager.cs` Z. 92, 99, 116](https://github.com/kaaasi/eishockey-manager-redone/blob/b41507db74f5035401c4b6cd2b47c13a9a3398b5/src/EishockeyManager.Core/GameState.AddManager.cs#L92-L116),
Aufruf ohne Abfang [`SetupScreen.cs` Z. 228](https://github.com/kaaasi/eishockey-manager-redone/blob/b41507db74f5035401c4b6cd2b47c13a9a3398b5/src/EishockeyManager.UI/Screens/SetupScreen.cs#L228).

- `SaveGame.Load` legt `GameState.Managers` als `Manager[]` an (Reader füllt `IReadOnlyList<>` mit einem Array); `ManagerList`
  wirft dann „Die Managerliste dieses Spielstands ist nicht erweiterbar.“ Die PR-Aussage „beim Spielstart und Laden eine
  `List<Manager>`“ stimmt für das Laden nicht.
- Vor dem Wurf ist `Setup.AddManager(setup)` (Z. 92) schon gelaufen: `Setup.Managers` = 2 bei `Managers` = 1 (kein atomarer Ablauf).
- **Beleg (Kern, Originaldaten):** neues Spiel (1 Manager, Verein 2, Seed 5) → `SaveGame.Load(SaveGame.Save(game)).Game!.AddManager(new
  ManagerSetup { Name = "N", Head = 3, ClubIndex = 7 })` → `InvalidOperationException`; Ausgabe der Probe:
  `new List`1[Manager] loaded Manager[] setupcount 1` / `after failed add: Managers 1, Setup.Managers 2`.
- **Beleg (UI, Originaldaten):** wie `AddManagerScreenTests.NewTile_AddsManager_WhoTakesTheNextTurn`, aber nach `d.StartNewGame()`
  `d.Host.Game = SaveGame.Load(SaveGame.Save(d.Host.Game!)).Game!` → beim Klick „Los jetzt!“ `InvalidOperationException` aus
  `SetupScreen.TryAdd` (Z. 228) über `ScreenHost.Update` – in der Desktop-Fassung ein Absturz.
- `AddManager_SurvivesSaveAndLoad` fügt nur **vor** dem Sichern hinzu und deckt den Fall nicht ab. Erwartet: roter Test „laden →
  hinzufügen → Spieltage → sichern/laden bytegleich“, dann Behebung (z. B. Reader/Kern liefert eine erweiterbare Liste; Prüfungen vor
  jeder Änderung). `GameState.cs` ist gemeinsame Datei – ggf. minimal mit Begründung.

### P3-1 – Doku „eigene Zufallsquelle“ unvollständig: `FinanceRandom` und `TrendRandom` werden verbraucht

[`GameState.AddManager.cs` Z. 65–67, 105, 174](https://github.com/kaaasi/eishockey-manager-redone/blob/b41507db74f5035401c4b6cd2b47c13a9a3398b5/src/EishockeyManager.Core/GameState.AddManager.cs#L65-L67):
Hauptquelle `Random` bleibt unberührt (Test grün, Mutation `var random = Random;` → `AddManager_DoesNotConsumeMainRandom` rot).
Aber `RenewOffers` zieht aus `FinanceRandom`, `RollTrend` aus `TrendRandom`. Probe (zwei gleiche Spiele, Originaldaten, 3 Spieltage,
in einem `AddManager`): `FinanceRandom same after add: False`, `TrendRandom same after add: False` – Zuschauer, Werbung usw. der
vorhandenen Manager folgen danach einer anderen Folge. Im Original (ein Generator) ebenso unvermeidlich; bitte als `[APPROX]`
benennen statt „verbraucht keinen Zufall“ allgemein, oder `RenewOffers` für den Neuen aus der eigenen Quelle ziehen.

### P3-2 – Mehrsaisonproben schwach

- `NutzerBefundeMatchGateTests.MultiSeason_FourChanges_EveryPreparedGateIsSettled` bleibt grün, wenn der Aufruf
  `game.PrepareMatchGate(...)` in `MatchEngine.Live.cs` entfernt wird (Mutation: nur `HomeGate_KnownDuringMatch…` und
  `NutzerBefundeLiveSpectatorTests` rot) – „0 offene Vorbereitungen“ ist ohne Vorbereitung trivial wahr. Eine Zählung „vorbereitet =
  abgerechnet > 0“ je Saison würde es abdecken.
- `AddManager_MultiSeason_…` ist synthetisch und setzt das Geld jedes Managers je Zug auf 10 Mio. (Geld ungeprüft). Ergänzend
  von mir mit Originaldaten ohne Geldfixierung gelaufen (s. 3.) – unauffällig; bitte die Probe mit Laden (P1-1) verbinden.

### Gegenbeispiele (erneut ausgeführt)

| Befund | Probe | Ergebnis |
|---|---|---|
| #834 | Ablauf `werbung` (Bild `08_eisflaeche`: Logo „JWE Service“, Angebot angenommen); `ndisasm -e 0x2B56A em.exe`: Tafel 9 nur bei `[es:bx]` = 2 (Überdachung), Tafel 10 nur bei `[es:bx+4]` = 2 (Kühlanlage), sonst Sprung 0x2B589 (leeren) | behoben; Sperre belegt (Oberliga-Start ohne Halle → im Original ebenso nicht wählbar) |
| #835 | `neuer-manager`/Hauptmenü neben `oberflaeche_0410` (4-fach vergrößert) | kein dunkler Zellrand, deckungsgleich |
| #836 | Ablauf `einstellungen` (`00_optionen`): Kachel → Optionsfenster, Knopf „Gerät“ unten links im kb\2-Stil | behoben |
| #837 | `NutzerBefundeLiveSpectatorTests`, Mutation ohne `PrepareMatchGate` → rot; Ablauf `live-spiel` (Auswärtsspiel „(ausw.)“) | behoben |
| #838 | `MatchDayScreen.FramesPerMinute` = 20; `DesktopGame.Update` treibt den Host, MonoGame-Vorgabe `IsFixedTimeStep` (60 Hz, kein Override) → Echtzeit unabhängig von der Zeichenrate; `ScreenDriver.PlayMatchDay` überspringt per Taste | behoben (`[APPROX]` ⅓ s offen) |
| #839 | `finanzen/01` neben `oberflaeche_0600` | Schrift/Lage rechte Spalte deckungsgleich |
| #840 | `finanzen/09_finanzbuch` über Knopf „Buch“; Bankbild ohne Wirkung (`FinanceScreenTests`) | behoben |
| #841 | Ablauf `neuer-manager` (3 Bilder), `AddManagerScreenTests`; Hot-Seat-Probe 2 Manager, Manager 1 am Zug fügt 3. hinzu → `Continue` gibt Manager 2 denselben Tag | behoben für neue Spiele, **nicht nach Laden (P1-1)** |

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh` (alle drei Läufe) am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3242 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2373 | 0 | 550 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2374 | 868 | 0 |

Weitere Proben (temporäre Testdateien in eigenem Worktree, nicht committet; Originaldaten, Seed 5):

- **#837 keine Werteverschiebung:** zwei Manager (Vereine 2/7), zwei Saisons, beide halten den Kader; Base `4e55e07` und Head je
  ohne und mit Live-Treiber (Treiber ohne Wirkung) → SHA-256 des Spielstands `D7DF9D6E6864F9E8…` und Konten `-5300070/-7287801` in
  allen vier Läufen gleich (Base = Head, Live = ohne Anzeige).
- **#837 Sichern zwischen Spiel und Abrechnung:** nicht erreichbar – `Continue` läuft während des Live-Spiels auf eigenem Thread,
  die Live-Bildschirme haben keinen Sicherungsweg (`grep SaveGame src/EishockeyManager.UI/Screens/Live` leer); Vorbereitungen sind
  kein Spielstand und verfallen am Tagesanfang (`MarkShortHandedClubs`).
- **#841 Kader/Pool:** Computerkader von SB Rosenheim (20 Spieler) liegt nicht im Pool → Entfernen hinterlässt keine verwaisten
  Plätze; neuer Kader 25 Spieler, alle Poolplätze `Taken` mit Verein. Mutationen: ohne `RemoveListingsOfAddedClub` →
  `AddManager_RemovesMarketSlotsOfTheClub` rot; ohne Nationalspieler-Grenze 179 → `AddManager_MidSeason_…` rot.
- **#841 Mehrsaison mit Originaldaten, ohne Geldfixierung:** hinzugefügt nach Spieltag 3, vier Saisonwechsel, Neuer hält Kader →
  beide nicht entlassen, Kader 25–26, Sichern/Laden bytegleich. Poolplätze ohne Verein-Gegenstück treten nur beim passiven
  Manager 0 auf und genauso ohne `AddManager` (Vergleichslauf) – nicht Teil dieses PRs.
- Dateigrenzen: `inventur.md`, `MenuTiles.cs`, `GameState.cs`, `Widgets.cs` unverändert; `MainMenuScreen.cs` nur Kopf-Blit (#835).

**Akzeptierte `[APPROX]`:** #838 ⅓ s je Minute (Schätzung Nutzer, DOSBox-Messung offen); #841 kein Platztausch in die
Start-Liga (0x29C3B–0x29CF2), erster freier Poolplatz nach 1000 Fehlzügen, Computerkader verlässt das Spiel, Escape verlässt den
Modus, Level/Start-Stärke von Manager 0; Knöpfe „Buch“/„Gerät“ und `SmallButton`; Kontostand zentriert; Mindestbreite 0x374.
**Bestätigt offen (Leitstelle):** Inventur A318–A320 und `anforderungen/bildschirme.md` Z. 67/100 (und Z. 119) veraltet.
**Annahmen:** Das Verhalten nach dem Laden im Original (Manager hinzufügen in geladenem Spiel) ist nicht per DOSBox geprüft; die
Kachel ist laut 0x45057 nur bei vier Managern gesperrt, also auch nach dem Laden erreichbar.
