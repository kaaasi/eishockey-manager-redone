# Review PR #1033 – Spur torszene-dauer (A472, Teil 1 + 2)

Erstreview (Claude Opus 5.5, frische Sitzung, geplanter Lauf). Base `ad7a7d9`, Head `e79bffe` (`git rev-parse HEAD` =
`e79bffe65e193335931a9d37a8f19e65c462ffe5`, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Nein** für genau diesen Head – wegen eines P2 (R1). Der Rest trägt: Gegenbeispiele A472 und T1–T8 nachgestellt, alle drei Läufe
von `tools/pruefen.sh` grün, drei Kern-Mutationen werden rot, der Spielkern ändert sich nur an den drei vorgesehenen Stellen
(Strafgrund-Zug, Merker „Manager am Zug“, Szenenwahl je Schuss). R1 ist klein: ein Fixlauf mit `--schnell`; danach reicht die
Delta-Prüfung der Leitstelle (nur P2/P3).

## 2. Befunde

### R1 (P2) – Darstellung 2 lässt sich mitten im Live-Spieltag einschalten; der Spielkern zieht dann ab sofort keinen Strafgrund mehr

- Fundstellen: `OriginalMinutes.PermanentRink` liest die **aktuelle** Option
  ([MatchEngine.GoalScene.cs:151](https://github.com/kaaasi/eishockey-manager-redone/blob/e79bffe65e193335931a9d37a8f19e65c462ffe5/src/EishockeyManager.Core/MatchEngine.GoalScene.cs#L151),
  Gate [Z. 161](https://github.com/kaaasi/eishockey-manager-redone/blob/e79bffe65e193335931a9d37a8f19e65c462ffe5/src/EishockeyManager.Core/MatchEngine.GoalScene.cs#L161));
  die Spielmodus-Optionen (Leertaste im Live-Bildschirm, Darstellung 0/1) schalten ohne Sperre weiter
  ([GameOptionsScreen.cs:162](https://github.com/kaaasi/eishockey-manager-redone/blob/e79bffe65e193335931a9d37a8f19e65c462ffe5/src/EishockeyManager.UI/Screens/GameOptionsScreen.cs#L162):
  `options.NextLiveMode()` ohne `permanentRinkBlocked`).
- Original: `GameOptions.NextLiveMode` dokumentiert selbst als `[ORIGINAL]`, dass Wert 2 bei `DGROUP 0x5A56 ≠ 0` sofort 0 wird
  (Datei 0x2D349–0x2D35D: `cmp byte [bx+0x21d],0x2` / `cmp byte [0x5a56],0x0` / `mov byte [bx+0x21d],0x0`, ndisasm über
  `em.exe`). Während eines Live-Spieltags ist 0x5A56 = 1, Darstellung 2 ist dort also nicht erreichbar.
- Folge im Remake (neu durch diesen PR): Nach dem Umschalten bleibt der Live-Bildschirm (`PermanentRink` der UI ist im Konstruktor
  festgelegt), der Spielkern hält aber ab der nächsten Minute „Dauer-Eisfeld“ und zieht keinen Strafgrund mehr – geänderte
  Zufallsfolge, also andere Ergebnisse an einem Darstellung-1-Spieltag (gegen T8). Am Spieltagsende bleibt Darstellung 2 gesichert.
- Beleg (Probe, temporär `tests/EishockeyManager.UI.Tests/ReviewProbeTests.cs`, Ablauf wie `LiveTastenUiTests.FirstLeagueDay`,
  Seed 5, Darstellung 1, Geschwindigkeit 60; Minute 5 Leertaste, Klick in `GameOptionsScreen.SwitchRect(GoalScenesSwitch)`):

  ```
  Assert.Equal(GameOptions.PermanentRinkLiveMode, game.Options.LiveMode); // Original: 0
  Assert.False(live.PermanentRink);
  Assert.True(OriginalMinutes.PermanentRink(game.Options));
  ```

  `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.UI.Tests -c Release --filter ReviewProbeTests` → bestanden, Ausgabe
  `PROBE LiveMode nach Klick mitten im Spieltag: 2; PermanentRink(UI)=False`. Dass der Kern dann nicht zieht, belegt der
  PR-eigene Test `PenaltyReason_NotDrawnOnPermanentRink` (Darstellung 2 → 0 Züge).
- Vorschlag: im Optionen-Bildschirm aus dem Live-Spieltag `NextLiveMode(permanentRinkBlocked: true)` (Original-Sperre; Datei
  außerhalb der Spur → Leitstelle) **oder** in der Spur den Zustand „Dauer-Eisfeld“ einmal zu Spieltagsbeginn festhalten
  (0x5A56/0x5A55 statt der Option); die Probe als Regressionstest übernehmen. Nebenbei: `docs/analyse/torszene-dauer.md` §8
  („Optionen mitten im Spiel auf 2“) beschreibt damit einen im Original nicht erreichbaren Fall – Satz anpassen.
- Bestand vor dem PR (nicht neu): Mit Darstellung 2 spielt `GoalSceneCall.Request` die Szene auch ohne Eis in Modus 2
  (`Continuous`); mit der Sperre entfällt auch das.

### R2 (P3) – Drei UI-Mutationen bleiben grün

Gegenproben in temporärer Kopie (`--filter TorszeneDauerUiTests`, alle 5 Tests bestanden statt rot):
- M4 `LiveMatchScreen.cs:157` `&& !PermanentRink` entfernt (Klick/Eingriff auf dem Eis): grün – die Klicks des Tests auf
  (100 | 10) öffnen offenbar auch ohne Sperre kein Eingriffsmenü; `Interventions == 0` prüft nichts.
- M5 `LiveMatchScreen.cs:415` `permanentRink: false`: grün – der Ligaspieltag hat kein Penaltyschießen, `RinkScript` ruft
  `Pages` sonst nie.
- M7 `LiveMatchScreen.cs:649` ohne `HallLevel = rink.HallLevel`: grün – mit einem Manager gleich; die Stufe 2 bei zwei Managern
  ist nur an `BuildHallLevel` geprüft, nicht am Auftrag der Szene.
Vorschlag: Klick auf eine Stelle, die bei Darstellung 1 sicher das Eingriffsmenü öffnet (Gegenprobe), und ein Szenenauftrag mit
zwei Managern.

### R3 (P3, Verdacht) – Rückfall ohne `tore\` nur in der UI

`LiveMatchScreen.PermanentRink` = Option **und** `GoalSceneCall.Available` (Z. 97), der Spielkern prüft nur die Option. Ohne
`tore\` zeigt das Remake also Live-Bildschirm samt Reporter und Eingriffsmenü, zieht aber keinen Strafgrund und setzt den Merker
„Manager am Zug“ wie ohne Live-Anzeige. Das Original setzt beim gescheiterten Aufbau 0x5A56 = 1 (Ausgabe A „Aufbau scheitert“).
Nur Codelesen; mit vollständigen Originaldaten nicht erreichbar. Im `[APPROX]` ergänzen genügt.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `tools/pruefen.sh` (alle drei Läufe, SDK 10.0.402, Commit `e79bffe`) | bestanden |
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4055 / 0 / 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2872 / 0 / 665 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2873 / 1182 / 0 |

- Gegenbeispiel A472/T1–T6, T8: `python3 tools/reverse-engineering/live/torszene_dauer.py ~/referenz/original/gamedata/em.exe`
  (Ausgaben A–G, 164 Zeilen): Aufbau Modus 1 einmal vor den Minuten, Minutenschleifen mit Anzeige-Argument 0 und `[0x5A55]` = 1,
  je Minute nur Minutenzeile ohne Pause/Klick, Verlängerung ohne neuen Aufbau, Abbau Modus 3; Torszene auf dem Eis ohne
  Drittelzähler, Reporter kehrt sofort zurück, Verletzung → automatische Aufstellung; Seitenwähler mit `[0x5A55]` = 1 ohne Seite.
  Deckt sich mit `GoalSceneRinkView`/`RinkScript` und den Tests.
- T7: Ausgabe D – Darstellung 1 **und** 2: Abbau des Verlängerungseises (nur 2), Klick, eigenes Eis Minute 70 Stufe 2,
  Torszene je Schuss, Abbau, Klick; Darstellung 0 Live-Bildschirm mit Spielstand 0x51FEA. Remake: `Shootouts`/`ShootoutShots`.
- Bildschirmfoto-Ablauf `dotnet run -c Release --project src/EishockeyManager.Tools -- screens gamedata <ziel> torszene-dauer`:
  fünf Bilder (Aufbau 0 : 0 Minute 1, stehendes Eis nach der Szene, Ankündigung, Penalty-Eis Minute 70, Endergebnis „1:2 n.P.“)
  angesehen, plausibel; kein Videobild mit Darstellung 2 zum Pixelvergleich.
- Mutationen Spielkern (`--filter TorszeneDauerTests`, je 1 von 6 rot): M1 Strafgrund-Gate entfernt, M2 `&& !PermanentRink`
  in `MatchEngine.Live.cs` entfernt, M3 Szenenwahl je Schuss aus. UI: M6 kein Abbau vor dem Penaltyschießen → 1 von 5 rot;
  M4/M5/M7 siehe R2.
- Zufallsfolge/Geld: Kern-Diff `--stat` nur `MatchEngine.GoalScene.cs`, `MatchEngine.Live.cs` (eine Zeile), `MatchEngine.Shootout.cs`;
  Darstellung 0 zieht im Penaltyschießen wie vorher (`ShootoutDisplayZero_NoScenes_SameAsBefore`), keine Buchung, keine
  Statistik berührt. Dateigrenzen eingehalten (`MatchEngine.Live.cs`, `LivePageScreens.cs` je eine Stelle); Kollision
  `LivePageScreens.cs` mit #1042 nicht bewertet.

**Erwartet offen, bestätigt (akzeptierte `[APPROX]`):** ohne `tore\` Live-Bildschirm statt Eis (siehe R3); Kopfzeile nur aus
höchstens vier Reihen; Torschütze 0x541D9 im Penaltyschießen ohne Zug; Schüsse bei Darstellung 0 nicht gezeigt; kein Videobild mit
Darstellung 2 (DOSBox nach R26-D3 = B). Weiter akzeptiert: kein Rückfall bei zu wenig Speicher.

**Annahmen (nicht geprüft):** Szenenwahl im Penaltyschießen nur mit `[0x226]` ≠ 0 (Datei 0x53DD2–0x53DE3) aus der Lesart des
PR übernommen; ob der Reporter 0x52520 bei Darstellung 0 im Penaltyschießen zieht, bleibt wie vor dem PR offen.
