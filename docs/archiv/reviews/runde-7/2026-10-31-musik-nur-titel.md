# Review: Musik nur im Titelbildschirm, Musikeinstellungen entfernt, Testaufräumen (#223)

- PR: #223 · Base `52dad67d431605748dfc90ee81e1b1cd43f49582` · Head `0002452e66906724a8c60176947ff54005d24401`
- Umfang: #221 (`MusicPolicy`, `SettingsSync`, `GameSettings`, `SettingsScreen`, Ablauf `einstellungen`, Doku),
  #220 (Kommentarreihenfolge `BoerseStartScreenTests`, `MusicPolicy.Decide`)
- Reviewer: Claude (frische Sitzung ohne Implementierungskontext, geplanter Lauf), 31.10.2026
- Checkout: Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer. Build, Tests und Bildschirmfoto am
  nachgewiesenen Checkout mit Originaldaten aus `kaaasi/eishockey-manager-referenz` (SDK 10.0.402).

## 1. Merge-Empfehlung

**Ja, für genau diesen Head.** `tools/pruefen.sh --schnell` grün (1077/0/0), alle drei Gegenbeispiele reproduziert,
die Mutationen an Regel und Dateikompatibilität machen die zugehörigen Tests rot. Musik kann nur noch über
`SettingsSync.Apply` → `MusicPolicy.Decide` starten, und das nur bei `TitleScreen`. Offen sind nur P3-Punkte.

## 2. Befunde

### MT-01 (P3) – Rückkehr zum Titel startet die Musik erneut (Bestätigung, kein Fehler)

Einziger Startpunkt im Code: [`SettingsSync.cs` Z. 42–49](https://github.com/kaaasi/eishockey-manager-redone/blob/0002452e66906724a8c60176947ff54005d24401/src/EishockeyManager.Desktop/SettingsSync.cs#L42), je Frame aus
[`DesktopGame.cs` Z. 71/84](https://github.com/kaaasi/eishockey-manager-redone/blob/0002452e66906724a8c60176947ff54005d24401/src/EishockeyManager.Desktop/DesktopGame.cs#L84) aufgerufen; `grep PlayMusic` findet sonst
nur `AudioMixer`, das Interface `GameAudio` und Audio-Tests. Wege zurück zu `TitleScreen`: Zurück aus der
Spielvorbereitung ([`SetupScreen.cs` Z. 93](https://github.com/kaaasi/eishockey-manager-redone/blob/0002452e66906724a8c60176947ff54005d24401/src/EishockeyManager.UI/Screens/SetupScreen.cs#L93)) und Spielende
([`SeasonEndGameOverScreen.cs` Z. 40](https://github.com/kaaasi/eishockey-manager-redone/blob/0002452e66906724a8c60176947ff54005d24401/src/EishockeyManager.UI/Screens/SeasonEndGameOverScreen.cs#L40)). Dort startet die
Musik von vorn (`AudioMixer.PlayMusic` → `Restart`) – deckt sich mit „nur im Titelbildschirm“. Spieltag, Hauptmenü,
Einstellungen und Spielvorbereitung: `Decide(…, true)` = `Stop` (Probe unten). Wie in TM-01 (#218) bleibt die
Verdrahtung in `Desktop` ohne Test; sie ist jetzt ein reiner `switch` auf `Decide`.

### MT-02 (P3) – Zeilenlage der Einstellungen nur durch Bildschirmfoto belegt

Mutation `FirstRowY = 90 → 107` (alle Zeilen eine Zeile tiefer, letzte überlappt den F8-Hinweis) bleibt grün, weil
Tests Klickfelder aus denselben Konstanten bilden. Bildschirmfoto (`screens … einstellungen`) geprüft: vier Zeilen
lückenlos im Abstand 17, Fenster 24/53/272/138 mittig wie zuvor (Mitte y = 122), F8-Hinweis und „Zurück“ im Rahmen.
Kein Original-Vorbild (Port-Bildschirm), daher kein Pixelvergleich möglich.

### MT-03 (P3) – Umschalten „Automatisch speichern“ per Klick ungetestet (vorbestehend)

Mutation: Klick auf `Row.AutoSave` schaltet `GoalScenes` → alle Tests grün
([`SettingsScreenTests.cs` Z. 155](https://github.com/kaaasi/eishockey-manager-redone/blob/0002452e66906724a8c60176947ff54005d24401/tests/EishockeyManager.UI.Tests/SettingsScreenTests.cs#L155) klickt nur Torszenen und
Vollbild). War schon vor #221 so. Probe des Reviewers (jede Zeile einzeln, s. u.) bestätigt die richtige Zuordnung am Head.

### MT-04 (P3) – Doku

`bildschirme.md` Z. 128 und `zusatzfunktionen.md` Z. 38 nennen keine Musikeinstellung mehr; README nennt nur den
Meilenstein „v0.5 … Sound und Musik“ (Historie, kein Einstellungshinweis) – in Ordnung. Zwei Randnotizen:
`zusatzfunktionen.md` erscheint im Diff als vollständig neu, weil die Zeilenenden von CRLF auf LF umgestellt wurden
(inhaltlich nur die letzte Zeile geändert; Datei weiter ohne Schlusszeilenumbruch). `bildschirme.md` Z. 128 nennt
unter „Einstellungen“ weiter „Cheats“, der Bildschirm hat keine solche Zeile (vorbestehend, nicht Teil von #221).

### MT-05 (P3) – Stil `SettingsScreen`

Nach dem Wegfall der Musikzeile bleibt ein nackter Block `{ var row = Row.EffectsVolume; … }`
([Z. 71](https://github.com/kaaasi/eishockey-manager-redone/blob/0002452e66906724a8c60176947ff54005d24401/src/EishockeyManager.UI/Screens/SettingsScreen.cs#L71)), und `Volume(Row)`/`SetVolume(Row, int)` ignorieren den
Parameter. Funktional richtig, beim nächsten Anfassen vereinfachen.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `tools/pruefen.sh --schnell` (Release, Warnungen = Fehler, `EM_REQUIRE_GAMEDATA=1`) | 1077 bestanden / 0 / 0 |

**Gegenbeispiele**

1. Musik nur bei `TitleScreen`: `MusicPolicyTests` (Titel, Spielvorbereitung, `null`, Hauptmenü, Spieltag) grün;
   zusätzliche Probe (temporäre Kopie, nicht committet): Einstellungen geöffnet → `Wanted` = false, `Decide(…, true)` = `Stop`.
2. Alte Datei mit `MusicVolume`/`MusicInMatch`: `Load_OldFileWithMusicFields_StillLoads` grün (unbekannte Felder
   werden von `System.Text.Json` ignoriert; beim nächsten Speichern fallen sie weg).
3. Einstellungen ohne Lücke: Probe klickt jede Zeile einzeln (Torszenen, Vollbild, Automatisch speichern) – jeweils nur
   der eigene Wert kippt, Effektlautstärke unverändert; Zeilenabstand 17, Zeilen innerhalb Fenster und oberhalb
   „Zurück“. Bildschirmfoto siehe MT-02. (Die Probe war als eigene Klasse parallel zu `SettingsScreenTests` einmal rot
   wegen des statischen `GameSettings.Current` – Artefakt der Probe, allein 3× grün.)

**Mutationen** (temporäre Kopie, je Lauf zurückgesetzt, Filter `MusicPolicy|SettingsScreen`)

| Mutation | Ergebnis |
|---|---|
| `Wanted`: `TitleScreen or MatchDayScreen` | rot: `Music_NotInMatchDay` |
| `Wanted`: `TitleScreen or SetupScreen` | rot: `Music_OnlyInTitleScreen`, `Decide_StartsStopsOrLeaves` |
| `Decide`: `(true,false) → None` | rot: `Decide_StartsStopsOrLeaves` |
| `JsonUnmappedMemberHandling.Disallow` beim Laden | rot: `Load_OldFileWithMusicFields_StillLoads` |
| `FirstRowY 90 → 107` | grün (MT-02) |
| Klick AutoSave schaltet GoalScenes | grün (MT-03) |

#220: Der verschobene Doc-Kommentar in `BoerseStartScreenTests` steht jetzt über `Hover_KeepsOffset_ClickSetsItImmediately`;
vorher hingen zwei `<summary>` an einer Methode. Nur Kommentar, keine Logikänderung.

**Akzeptierte `[APPROX]`:** `MusicPolicy` – ob das Original Musik an weiteren Stellen spielt, ist ungeprüft; Vorgabe
des Nutzers (#221) gilt. **Annahmen:** Desktop-Verdrahtung nur per Codelesen (kein Desktop-Testprojekt); Musikpegel
bleibt beim Mixer-Standard 10 (`AudioMixer._musicVolume`), Stummschalten über F8.
