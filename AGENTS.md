# Regeln für alle Agenten

Dieses Dokument gilt für **jeden** Agenten, der an diesem Repository arbeitet (Claude Code, ChatGPT, andere KI-Agenten)
und für Menschen. Es setzt keinen Chatverlauf und kein bestimmtes Werkzeug voraus. Werkzeugspezifische Hinweise stehen
getrennt (Claude Code: [CLAUDE.md](CLAUDE.md)) und dürfen diesen Regeln nicht widersprechen.

Zusammenarbeit, Rollen, Übergabe und Merge-Prüfung: [docs/prozess/zusammenarbeit.md](docs/prozess/zusammenarbeit.md).
Aktuelle Arbeitspakete: [docs/arbeitspakete.md](docs/arbeitspakete.md) (Abschnitt „Aktuelle Runde“).

## 1. Projekt

Portierung des **Eishockey Managers** (Software 2000, DOS 1993) nach C#/.NET 10. Ziel: zuerst originalgetreu
(Pixel, Regeln, Abläufe), danach Erweiterungen. **Sprache:** Deutsch in Doku, Kommentaren und Commit-Nachrichten;
Code-Bezeichner englisch.

## 2. Harte Regeln

1. **Originaldateien, Videos und Ausschnitte daraus niemals committen** – auch nicht in Tests, Doku oder PR-Texten
   als Bild. `.gitignore` schützt `gamedata/`, `*.vga` usw.; Referenzbilder liegen nur im privaten Repo
   `kaaasi/eishockey-manager-referenz` und werden nie in dieses Repo kopiert.
2. **Nie direkt auf `main` schreiben.** Jede Änderung über einen Branch und einen Pull Request.
3. **Ein Branch – ein Agent.** Wer an einem Branch arbeitet, öffnet sofort einen Draft-PR und trägt sich dort als
   Bearbeiter ein. Fremde Branches nur nach dokumentierter Übergabe weiterführen (siehe Zusammenarbeit).
4. **Keine Freigabe der eigenen Arbeit.** Wer etwas umgesetzt hat, gibt es nicht selbst zum Merge frei; dafür gibt es
   ein unabhängiges Review (anderer Agent oder frische Sitzung ohne Implementierungskontext).
5. **Nur die eigenen Dateien ändern** (Dateigrenzen der Spur in `docs/arbeitspakete.md`). Gemeinsame Dateien
   (`GameState.cs`, `GameState.Integration.cs`, `MainMenuScreen.cs`, `Widgets.cs`, `MenuTiles.cs`,
   `EishockeyManager.slnx`, `README.md`) höchstens minimal und mit Begründung im PR. Neue Bausteine für alle →
   eigene Datei in `src/EishockeyManager.UI/` und im PR erwähnen.
6. **Erst reproduzieren, dann reparieren:** Befund nachstellen → Regressionstest schreiben, der rot ist → beheben.
7. **Was nicht aus dem Original belegt ist, bekommt im Code `[APPROX]`** mit kurzer Begründung; Belegtes `[ORIGINAL]`
   mit Fundstelle. Keine Originaltreue behaupten, die nicht geprüft wurde.

## 3. Einrichtung

| Was | Woher |
|---|---|
| .NET-SDK | Version aus `global.json` (10.0.402, `latestPatch`). Jede Quelle ist recht (Microsoft, Paketverwaltung). Ohne Zugang zu Microsoft-Servern: Release `claude-werkzeuge` dieses Repos (siehe `tools/claude/setup-workspace.sh`, braucht `gh`). |
| NuGet-Pakete | `dotnet restore EishockeyManager.slnx` über nuget.org. Ohne Zugang: Offline-Spiegel im selben Release (`nuget-packages.tar.gz`). Paketversionen sind fest (`.github/scripts/versionen_pruefen.py`). |
| Originaldateien | Eigene Kopie der DOS-Version (ZIP in den Projektdateien des Nutzers; sonst beim Nutzer anfragen). Inhalt nach `gamedata/` im Repo-Stamm, sodass `gamedata/em.exe` und `gamedata/kb/` existieren. |
| Referenzbilder | Privates Repo `kaaasi/eishockey-manager-referenz` (nur lesen), Laden mit `tools/claude/fetch-reference.sh`; Kapitelindex `docs/referenz-videos.md`. |

Prüfen, ob alles da ist: `tools/pruefen.sh --schnell` (Build + Tests).

## 4. Aufbau

```
Desktop (MonoGame) → UI (Bildschirme, 320×240-Palettenpuffer) → Core (Spielzustand, Regeln) → Formats (Originaldateien)
```

- `src/EishockeyManager.Formats` – Dateiformate (Bilder, Paletten, Datenbank, Fonts, Halle, Figuren). Doku: `docs/formate/dateiformate.md`
- `src/EishockeyManager.Core` – Spielzustand und Regeln. `GameState`, `Club`, `Manager`, `Player` sind `partial`:
  **neue Daten eines Pakets in eine eigene Datei** (`GameState.Finanzen.cs` o. ä.).
- `src/EishockeyManager.UI` – ein `Screen` je Bildschirm, zeichnet in `Surface` (320 × 240 Byte + Palette).
  Bausteine des Originals in `Widgets.cs` (Rahmen aus kb\2, Knöpfe, Lampen, Smileys …).
- `src/EishockeyManager.Audio`, `src/EishockeyManager.Tools` (`emtool`: Export, Datenbank, Bildschirmfotos), `src/EishockeyManager.Viewer`.
- `tests/` – xUnit, je Schicht ein Projekt.

### Verbindliche Architekturregeln

- **Zeit vergeht nur über `GameState.Continue()`.** Was je Tag, Woche oder Saison passiert, wird in
  `src/EishockeyManager.Core/GameState.Integration.cs` angeschlossen – keine eigenen Nachhol-Zähler.
- **Echte Einnahmen und Ausgaben nur über `GameState.Book(manager, kategorie, betrag)`.** Neutrale Umbuchungen
  (Bank, Sparbuch, Aktien zum Kurswert, Kredite zwischen Managern) sind kein Aufwand und bleiben getrennt.
- **Bildschirme verändern keinen Spielzustand durch bloßes Öffnen** (keine Zeit, kein Training, kein Markt, kein Zufall).
- Neue Bildschirme für eine Menükachel ohne fremde Dateien anmelden (Name exakt wie in `Screens/MenuTiles.cs`):

  ```csharp
  internal static class FinanzenMenuEntries
  {
      [MenuEntry("Finanzen")]
      private static Screen Open(ScreenHost host, Screen back) => new FinanzenScreen(host, back);
  }
  ```

- Bildschirmfoto-Abläufe: eigene Datei in `src/EishockeyManager.Tools/Scenarios/`, Methode mit `[Scenario("name")]`
  und `ScreenDriver` (`StartNewGame()`, `PlayMatchDay()`, `OpenTile(…)`, `Click`, `Type`, `Press`, `Save`).
  Ausführen: `dotnet run --project src/EishockeyManager.Tools -- screens gamedata <ziel> <ablauf>`.

### Originaltreue

1. **Vorlage:** Referenzbilder (1 Bild/s, schon auf 320 × 240 zugeschnitten: 720p-Video 960 × 720 ab x = 320,
   360p-Video 480 × 360 ab x = 0), Originalgrafiken (`emtool export`), Disassembly von `em.exe` (`ndisasm`,
   `docs/reverse-engineering.md`), Anforderungen in `docs/anforderungen/bildschirme.md`.
2. **Vermessen:** Grafikteile per Template-Matching suchen (z. B. Python mit numpy/scipy/PIL). Videobilder sind um
   ca. (−0,25 | +0,5) Pixel verschoben und leicht unscharf (`docs/portierung.md`).
3. **Farben:** Paletten auf 6 Bit maskieren (`Palette.DacToRgb`); häufige Indizes in `Colors` (`Assets.cs`).
4. **Markieren:** `[ORIGINAL]` mit Fundstelle bzw. `[APPROX]` mit Begründung (Regel 2.7).
5. **Prüfen:** Bildschirmfoto rendern, mit dem Videobild vergleichen, Test schreiben.

## 5. Tests

- **Einheitliche Prüfung vor jedem PR: `tools/pruefen.sh`.** Baut mit Warnungen als Fehler und führt drei Läufe aus:
  mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`), ohne Originaldaten (`EM_SKIP_GAMEDATA=1`, wie die CI) und den
  Pflichtlauf ohne Daten, der scheitern **muss**. Die ausgegebene Tabelle gehört in die PR-Beschreibung.
- Tests mit Originaldaten **nur** mit `[GameDataFact]`/`[GameDataTheory]` und `TestGameData.Load()`
  (`tests/EishockeyManager.UI.Tests/Support/GameDataFact.cs`). Nie „ohne Daten einfach `return`“.
- Zeit in Tests nur über die Zeitleiste vorspulen: `TestSeason.PlayMatchDay(game)` / `PlayRegularSeason(game)`.
- **Mehrsaisonprobe:** Wer Saisonwechsel, Kader oder Geld berührt, liefert einen Test über mindestens 4 Saisonwechsel
  (passiver und aktiver Manager).
- Die CI hat **keine** Originaldaten. Ein grüner CI-Lauf belegt nur den synthetischen Teil; der Lauf mit
  Originaldaten wird lokal ausgeführt und im PR mit Zahlen nachgewiesen. Testanzahl ist keine Abnahme.

## 6. Git und Pull Requests

- **Branches:** `paket/<spur>` für Arbeitspakete, `fix/<thema>`, `prozess/<thema>`, `integration/<runde>`,
  `review/<pr>` (nur die Review-Datei). Vor jedem Push: `git fetch origin && git rebase origin/main`, `tools/pruefen.sh` grün.
- **Commits** auf Deutsch, aussagekräftig. Jeder Commit nennt den Agenten in einem Trailer, z. B.
  `Co-Authored-By: ChatGPT <noreply@openai.com>` – alle Agenten schreiben über dasselbe GitHub-Konto, nur so bleibt
  nachvollziehbar, wer was gemacht hat.
- **Kleine PRs gegen `main`**, Beschreibung nach `.github/pull_request_template.md` (Pflichtangaben: Base-/Head-SHA,
  Fehler vorher, Regressionstest rot→grün, Testtabelle, Restlücken, `[APPROX]`). PR per Weboberfläche oder REST
  (`POST /repos/kaaasi/eishockey-manager-redone/pulls`); GraphQL ist nicht in jeder Umgebung erreichbar.
- **Status** einer Spur nur im eigenen Abschnitt von `docs/arbeitspakete.md`; Übergaben als PR-Kommentar
  (Vorlage `docs/handoffs/VORLAGE.md`).
- **Gemergt wird nur von der Leitstelle oder dem Nutzer** nach der Merge-Prüfung in `docs/prozess/zusammenarbeit.md`.

## 7. Wo was steht

| Thema | Datei |
|---|---|
| Zusammenarbeit, Rollen, Übergabe, Merge-Prüfung, GitHub-Free-Grenzen | `docs/prozess/zusammenarbeit.md` |
| Arbeitspakete, Dateigrenzen, Abnahmekriterien, Status | `docs/arbeitspakete.md` |
| Unabhängige Reviews | `docs/reviews/` |
| Dateiformate, Reverse Engineering, Portierung | `docs/formate/`, `docs/reverse-engineering.md`, `docs/portierung.md` |
| Saisonablauf, Tagesablauf | `docs/saison.md`, `docs/tagesablauf.md` |
