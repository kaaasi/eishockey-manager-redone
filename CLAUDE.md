# Leitfaden für Claude

Dieses Repository portiert den **Eishockey Manager** (Software 2000, DOS 1993) nach C#/.NET 10.
Ziel: zuerst originalgetreu (Pixel, Regeln, Abläufe), danach Erweiterungen. Sprache im Projekt: **Deutsch**
(Code-Bezeichner englisch, Kommentare, Doku und Commit-Nachrichten deutsch).

Mehrere Chats arbeiten parallel an verschiedenen **Arbeitspaketen** → [docs/arbeitspakete.md](docs/arbeitspakete.md).

## 1. Arbeitsumgebung einrichten (jede neue Sitzung)

```bash
git clone https://github.com/kaaasi/eishockey-manager-redone   # bzw. vorhandenen Klon nutzen
cd eishockey-manager-redone
```

**Originaldateien** (nie ins Repo!): Die DOS-Version liegt als ZIP in den Dateien des claude.ai-Projekts
„Eishockey Manager Redone“. Mit dem Projects-Werkzeug laden (`project_info` → Dateiname, dann `project_read`),
in einen eigenen Ordner entpacken, z. B. `~/orig/dos` (enthält `em.exe`, `kb/`, `tore/`).
Fehlt die Datei im Projekt, den Nutzer bitten, sie im Chat anzuhängen.

**.NET-SDK und NuGet-Pakete** (Microsoft- und NuGet-Server sind aus der Arbeitsumgebung gesperrt, GitHub nicht):

```bash
tools/claude/setup-workspace.sh ~/orig/dos   # lädt SDK + Pakete aus dem Release "claude-werkzeuge", verlinkt gamedata/
source ~/claude-tools/env.sh
dotnet build EishockeyManager.slnx && dotnet test EishockeyManager.slnx
```

Fehlt das Release oder sind Pakete veraltet: Workflow „Werkzeuge für Claude“ starten
(`gh workflow run werkzeuge.yml -R kaaasi/eishockey-manager-redone`). Er läuft auch automatisch, wenn sich
`*.csproj` auf `main` ändern. GitHub GraphQL ist gesperrt → `gh api` (REST) statt `gh pr …`-Befehlen, die GraphQL nutzen.

## 2. Aufbau

```
Desktop (MonoGame) → UI (Bildschirme, 320×240-Palettenpuffer) → Core (Spielzustand, Regeln) → Formats (Originaldateien)
```

- `src/EishockeyManager.Formats` – Dateiformate (Bilder, Paletten, Datenbank, Fonts, Halle, Figuren). Doku: `docs/formate/dateiformate.md`
- `src/EishockeyManager.Core` – Spielzustand und Regeln. `GameState`, `Club`, `Manager`, `Player` sind `partial`:
  **neue Daten eines Pakets in eine eigene Datei** (`GameState.Finanzen.cs` o. ä.).
- `src/EishockeyManager.UI` – ein `Screen` je Bildschirm, zeichnet in `Surface` (320 × 240 Byte + Palette).
  Bausteine des Originals in `Widgets.cs` (Rahmen aus kb\2, Knöpfe, Lampen, Smileys …).
- `src/EishockeyManager.Tools` – `emtool` (Export, Datenbank, Bildschirmfotos).
- `tests/` – xUnit. `EishockeyManager.UI.Tests` spielt mit den echten Dateien Abläufe durch (auf GitHub übersprungen).

### Zeit und Geld (verbindlich seit Integration Runde 1)

- **Zeit vergeht nur über `GameState.Continue()`** (Zeitleiste des Pakets `saison`). Was je Tag, Woche oder Saison
  passieren soll, wird in `src/EishockeyManager.Core/GameState.Integration.cs` angeschlossen – keine eigenen
  Nachhol-Zähler. Tests spulen mit `TestSeason.PlayMatchDay(game)` / `PlayRegularSeason(game)` vor.
- **Geld nur über `GameState.Book(manager, kategorie, betrag)`** buchen (Finanzbuch), nicht `Money` direkt ändern.
- Vor dem PR **auch gegen die anderen Pakete testen**: `git fetch origin && git rebase origin/main`, alle Tests grün.

### Neuen Bildschirm für eine Menükachel anmelden (ohne fremde Dateien zu ändern)

```csharp
internal static class FinanzenMenuEntries
{
    [MenuEntry("Finanzen")]  // Name exakt wie in Screens/MenuTiles.cs
    private static Screen Open(ScreenHost host, Screen back) => new FinanzenScreen(host, back);
}
```

### Bildschirmfotos-Ablauf anlegen

Eigene Datei in `src/EishockeyManager.Tools/Scenarios/`, Methode mit `[Scenario("finanzen")]` und `ScreenDriver`
(`StartNewGame()`, `PlayMatchDay()`, `OpenTile("Finanzen")`, `Click`, `Type`, `Press`, `Save`). Ausführen:
`dotnet run --project src/EishockeyManager.Tools -- screens gamedata ~/work/screens finanzen`

## 3. Originaltreue – so wird gearbeitet

1. **Vorlage**: Videos des Originals (vom Nutzer im Chat hochgeladen; Index in `docs/referenz-videos.md`),
   Originalgrafiken (`emtool export`), Disassembly von `em.exe` (`ndisasm`, siehe `docs/reverse-engineering.md`),
   Anforderungen in `docs/anforderungen/bildschirme.md`.
2. **Vermessen**: Videobild auf 320 × 240 bringen (720p-Video: Ausschnitt 960 × 720 ab x = 320; 360p-Video:
   480 × 360 ab x = 0). Grafikteile per Template-Matching suchen (Python mit numpy/scipy/PIL ist vorhanden).
   Videobilder sind um ca. (−0,25 | +0,5) Pixel verschoben und leicht unscharf. Details: `docs/portierung.md`.
3. **Farben**: Paletten werden auf 6 Bit maskiert (`Palette.DacToRgb`). Häufige Indizes in `Colors` (Assets.cs).
4. **Markieren**: Was nicht aus dem Original belegt ist, bekommt im Code `[APPROX]` mit kurzer Begründung.
5. **Prüfen**: Bildschirmfoto rendern, mit Videobild vergleichen, Test schreiben.

## 4. Regeln für parallele Arbeit

- Jedes Paket arbeitet auf einem eigenen Branch `paket/<name>` und liefert **kleine Pull Requests** gegen `main`.
- Vor jedem Push: `git fetch origin main && git rebase origin/main`, dann bauen und **alle** Tests lokal grün.
- Pull Request per REST: `gh api repos/kaaasi/eishockey-manager-redone/pulls -f title=… -f head=paket/<name> -f base=main -f body=…`.
  Gemergt wird vom Koordinations-Chat (oder vom Nutzer), wenn CI grün ist.
- **Nur die eigenen Dateien ändern.** Gemeinsame Dateien (`GameState.cs`, `MainMenuScreen.cs`, `Widgets.cs`,
  `MenuTiles.cs`, `EishockeyManager.slnx`, `README.md`) höchstens minimal und nur wenn unvermeidbar – lieber neue Dateien.
  Neue Bausteine für alle → eigene Datei in `src/EishockeyManager.UI/` und im PR erwähnen.
- Den Paketstatus in `docs/arbeitspakete.md` nur im eigenen Abschnitt pflegen.
- Commit-Nachrichten auf Deutsch, aussagekräftig.
- **Originaldateien, Videos oder Ausschnitte daraus niemals committen** (`.gitignore` schützt `gamedata`, `*.vga` usw.).
