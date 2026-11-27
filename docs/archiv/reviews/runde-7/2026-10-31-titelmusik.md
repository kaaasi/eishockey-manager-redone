# Review: Titelmusik nur im Titelbildschirm, Test Bankauswahl, Kommentar 0xA70E (#218)

- PR: #218 · Base `7b484eabe99e208a4c7279850daae3cbf0ec4b36` · Head `eb5424cfe2892771f36903508c3836e4bc645128`
- Umfang: #212 (`MusicPolicy.cs`, `SettingsSync.cs`), #214 (Test Bankauswahl), #215 (Kommentar 0xA70E)
- Reviewer: Claude (frische Sitzung ohne Implementierungskontext, geplanter Lauf), 31.10.2026
- Checkout: Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer; Base = `origin/main`. Build und Tests
  am nachgewiesenen Checkout mit Originaldaten aus `kaaasi/eishockey-manager-referenz` (SDK 10.0.402).

## 1. Merge-Empfehlung

**Ja, für genau diesen Head.** Kleiner PR, `tools/pruefen.sh --schnell` grün, alle Gegenbeispiele reproduziert, jede
Mutation macht den zugehörigen Test rot. Der Musikwechsel Titel → Spielvorbereitung funktioniert im Desktop-Spiel (per
Codelesen belegt, siehe TM-01). Offen sind nur P3-Punkte.

## 2. Befunde

### TM-01 (P3) – Keine Prüfung der Verdrahtung `SettingsSync.Apply` ↔ Bildschirmwechsel

Frage „stoppt die Musik beim Wechsel Titel → Spielvorbereitung?“: **ja**, Kette per Codelesen belegt (kein Test, das Projekt
`Desktop` hat keinen):

1. `TitleScreen.Update` ruft bei Klick/Taste `Host.Show(new SetupScreen(Host))`; `ScreenHost.Show` setzt `Current`
   sofort ([`Screen.cs` Z. 93–97](https://github.com/kaaasi/eishockey-manager-redone/blob/eb5424cfe2892771f36903508c3836e4bc645128/src/EishockeyManager.UI/Screen.cs#L93)).
2. `DesktopGame.Update` ruft je Frame `SettingsSync.Apply(…, _host.Current)`
   ([`DesktopGame.cs` Z. 84](https://github.com/kaaasi/eishockey-manager-redone/blob/eb5424cfe2892771f36903508c3836e4bc645128/src/EishockeyManager.Desktop/DesktopGame.cs#L84)).
3. `MusicPolicy.Wanted(SetupScreen, …)` = false → `mixer.StopMusic()`; `AudioMixer.Read` mischt nur bei `_musicPlaying`
   ([`AudioMixer.cs` Z. 126](https://github.com/kaaasi/eishockey-manager-redone/blob/eb5424cfe2892771f36903508c3836e4bc645128/src/EishockeyManager.Audio/AudioMixer.cs#L126)).

Die Tests prüfen nur `MusicPolicy.Wanted`; eine Rückkehr der alten Logik in `SettingsSync` (z. B. wieder `current is MatchDayScreen`)
bliebe grün. Kein Merge-Blocker, die Methode ist jetzt ein Einzeiler auf die Richtlinie.

### TM-02 (P3) – Musik startet bei jeder Rückkehr von vorn; „Musik auch im Spiel“ nicht mehr durchlaufend

`AudioMixer.PlayMusic` ruft `_music.Restart()`. Folge: (a) Rückkehr zum Titel (Spielvorbereitung „Zurück“,
[`SetupScreen.cs` Z. 93](https://github.com/kaaasi/eishockey-manager-redone/blob/eb5424cfe2892771f36903508c3836e4bc645128/src/EishockeyManager.UI/Screens/SetupScreen.cs#L93);
Spielende ohne/mit Bestenliste) startet das Stück neu; (b) mit `MusicInMatch = true` (Standard) läuft Musik nur noch auf
`MatchDayScreen` und beginnt bei jedem Spieltag von vorn, in allen anderen Bildschirmen dazwischen ist Ruhe (vorher lief sie
durchgehend). Das ist als `[APPROX]` für die Spieltage deklariert; der Neustart je Spieltag ist darin nicht ausdrücklich
genannt. Ob das Original Musik im Spieltag überhaupt spielt, bleibt offen (siehe Abschnitt 3).

### TM-03 (P3) – Einstellungen: Musiklautstärke nicht mehr hörbar

`SettingsScreen` ist nur über die Hauptmenükachel erreichbar
([`SettingsScreen.cs` Z. 179](https://github.com/kaaasi/eishockey-manager-redone/blob/eb5424cfe2892771f36903508c3836e4bc645128/src/EishockeyManager.UI/Screens/SettingsScreen.cs#L179)),
also nie im Titel. Dort läuft jetzt keine Musik, die Zeile „Musiklautstärke“ lässt sich nicht mehr probehören (Effekte
schon). Nur relevant, falls die Einstellung erhalten bleiben soll; wenn das Original dort ebenfalls still ist, ist es
Absicht. Zur Kenntnis, ggf. in `[APPROX]` aufnehmen.

### TM-04 (P3) – Verwaiste Doku-Kommentare in `BoerseStartScreenTests.cs`

Der neue `<summary>`-Block steht zwischen dem bestehenden Block von `Hover_KeepsOffset_ClickSetsItImmediately`
(„… Ersetzt `Hover_SetsOffsetImmediately` aus #188.“) und dessen Methode. `OnlyLeftClickOnLogoColumn_SelectsStock` trägt
nun zwei `<summary>`-Blöcke, `Hover_KeepsOffset_ClickSetsItImmediately` keinen; die Beschreibung des Hover-Tests gehört
nicht mehr zu ihm
([`BoerseStartScreenTests.cs` Z. 133–163](https://github.com/kaaasi/eishockey-manager-redone/blob/eb5424cfe2892771f36903508c3836e4bc645128/tests/EishockeyManager.UI.Tests/BoerseStartScreenTests.cs#L133)).
Reine Kommentarordnung: den neuen Test oberhalb des alten Blocks einfügen.

## 3. Ausgeführte Prüfungen

Befehle: `tools/pruefen.sh --schnell` (nach `tools/claude/setup-workspace.sh`, `source ~/claude-tools/env.sh`);
Einzelläufe `dotnet test tests/EishockeyManager.UI.Tests --filter <Test> -v q` mit `EM_REQUIRE_GAMEDATA=1`.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`--schnell`, `EM_REQUIRE_GAMEDATA=1`) | 1074 | 0 | 0 |

Ohne-Daten-Lauf und Pflichtlauf entfallen (kleiner PR, `AGENTS.md` §5); CI nicht ausgewertet.

Gegenbeispiele, jeweils in temporärer Änderung am Head, danach `git checkout`, Arbeitsbaum wieder sauber:

| Gegenbeispiel / Mutation | Erwartung | Ergebnis |
|---|---|---|
| `MusicPolicy`: `_ => false` → `_ => true` (Spielvorbereitung/Hauptmenü bekäme Musik) | `TitleMusic_StopsInSetup_AndMainMenu` rot | rot |
| `MusicPolicy`: `TitleScreen => true` → `false` (Titel ohne Musik) | `TitleMusic_StopsInSetup_AndMainMenu` rot | rot |
| `MusicPolicy`: `MatchDayScreen => settings.MusicInMatch` → `true` (Einstellung ignoriert) | `MatchDay_FollowsMusicInMatchSetting` rot | rot |
| `FinanceScreen` Z. 245 `StockSelectArea(i)` → `StockRow(i)` | `OnlyLeftClickOnLogoColumn_SelectsStock` rot | rot |
| `FinanceScreen` Z. 208 `input.LeftClick &&` → `(input.LeftClick \|\| input.RightClick) &&` | `OnlyLeftClickOnLogoColumn_SelectsStock` rot | rot |

Der Basislauf der drei Tests ist grün. Alle Bildschirme, in denen Musik laufen könnte: vor dem Titel gibt es keinen
(`DesktopGame.LoadContent` zeigt `TitleScreen` zuerst und startet die Musik sofort); um den Titel herum nur
`SetupScreen` (hat keine Musik, deckt sich mit dem Nutzerbefund), `HighscoreScreen`/`SeasonEndGameOverScreen`
(stumm, danach Titel mit Neustart, TM-02) und `CreditsScreen` aus dem Hauptmenü (stumm).

**Nicht nachgeprüft (Annahmen):** der Kommentar zu 0xA70E in `QuitQuestion.cs` (#215) – nur Kommentar, die Fundstelle in
`em.exe` habe ich nicht erneut disassembliert; das Verhalten des Originals zu Musik in Spielvorbereitung und Spieltag
(Nutzerbefund per DOSBox, nicht von mir wiederholt).

**Akzeptierte `[APPROX]`:** keine neue angenommen. **Erwartet offen, bestätigt:** `[APPROX]` Musik während der
Spieltage nach Einstellung (`MusicPolicy.cs`, Zeitpunkt im Original ungeprüft) bleibt offen; #216 und #217 nicht Teil
dieses Heads und unberührt.
