# Regeln für alle Agenten

Gilt für **jeden** Agenten (Claude Code, ChatGPT, andere) und für Menschen; setzt keinen Chatverlauf und kein Werkzeug voraus.
Werkzeugspezifisches steht getrennt (Claude Code: [CLAUDE.md](CLAUDE.md)) und darf nicht widersprechen. Zusammenarbeit, Rollen,
Übergabe, Merge-Prüfung: [docs/prozess/zusammenarbeit.md](docs/prozess/zusammenarbeit.md); Sonderfälle:
[docs/prozess/anhang.md](docs/prozess/anhang.md). Aktuelle Pakete: [docs/arbeitspakete.md](docs/arbeitspakete.md) („Aktuelle Runde“).

## 1. Projekt

Portierung des **Eishockey Managers** (Software 2000, DOS 1993) nach C#/.NET 10: zuerst originalgetreu (Pixel, Regeln, Abläufe),
danach Erweiterungen. **Sprache:** Deutsch in Doku, Kommentaren und Commit-Nachrichten; Code-Bezeichner englisch.

## 2. Harte Regeln

1. **Originaldateien, Videos und Ausschnitte daraus niemals committen** – auch nicht in Tests, Doku oder PR-Texten als Bild.
   `.gitignore` schützt `gamedata/`, `*.vga` usw.; Referenzbilder liegen nur im privaten Repo `kaaasi/eishockey-manager-referenz`
   und werden nie in dieses Repo kopiert.
2. **Nie direkt auf `main` schreiben.** Jede Änderung über Branch und Pull Request.
3. **Ein Branch – ein Agent.** Wer an einem Branch arbeitet, öffnet sofort einen Draft-PR und trägt sich als Bearbeiter ein.
   Fremde Branches nur nach dokumentierter Übergabe weiterführen.
4. **Keine Freigabe der eigenen Arbeit;** dafür gibt es ein unabhängiges Review (anderer Agent oder frische Sitzung ohne
   Implementierungskontext).
5. **Nur die eigenen Dateien ändern** (Dateigrenzen der Spur in `docs/arbeitspakete.md`). Gemeinsame Dateien (`GameState.cs`,
   `GameState.Integration.cs`, `MainMenuScreen.cs`, `Widgets.cs`, `MenuTiles.cs`, `EishockeyManager.slnx`, `README.md`) höchstens
   minimal und mit Begründung im PR. Neue Bausteine für alle → eigene Datei in `src/EishockeyManager.UI/`, im PR erwähnen.
6. **Erst reproduzieren, dann reparieren:** Befund nachstellen → roter Regressionstest → beheben.
7. **Nicht aus dem Original Belegtes bekommt im Code `[APPROX]`** mit kurzer Begründung, Belegtes `[ORIGINAL]` mit Fundstelle.
   Keine ungeprüfte Originaltreue behaupten.

## 3. Einrichtung

| Was | Woher |
|---|---|
| .NET-SDK | Version aus `global.json` (10.0.402, `latestPatch`), jede Quelle; ohne Microsoft-Server: Release `claude-werkzeuge` (`tools/claude/setup-workspace.sh`, braucht `gh`). |
| NuGet-Pakete | `dotnet restore EishockeyManager.slnx` über nuget.org; ohne Zugang Offline-Spiegel im selben Release (`nuget-packages.tar.gz`). Versionen fest (`.github/scripts/versionen_pruefen.py`). |
| Originaldateien | Privates Repo [`kaaasi/eishockey-manager-referenz`](https://github.com/kaaasi/eishockey-manager-referenz): DOS-ZIP `original/Eishockey-Manager_DOS_DE.zip`, entpackt `original/gamedata/`; Verknüpfung nach `gamedata/`: [docs/originaldateien.md](docs/originaldateien.md). Ohne Leserechte eigene Kopie der DOS-Version (sonst beim Nutzer anfragen). |
| Referenzbilder | Referenz-Repo (Spuren und Reviews nur lesen; ablegen nur Leitstelle und Nutzer per PR), `tools/claude/fetch-reference.sh`, Index `docs/referenz-videos.md`. |

Prüfen: `tools/pruefen.sh --schnell` (Build + Tests). Scheitern Build, Tests, Fetch oder GitHub-Schreibaktionen an der Umgebung
(kein `dotnet`, kein Netz, VSTest-Socket gesperrt, Freigabe abgelehnt): [docs/prozess/umgebung.md](docs/prozess/umgebung.md).
Blockierte Läufe sind keine Testergebnisse und werden als blockiert dokumentiert; Proxy, Sandbox und Freigaben nicht umgehen.

## 4. Aufbau

`Desktop (MonoGame) → UI (Bildschirme, 320×240-Palettenpuffer) → Core (Spielzustand, Regeln) → Formats (Originaldateien)`

- `src/EishockeyManager.Formats` – Dateiformate (Bilder, Paletten, Datenbank, Fonts, Halle, Figuren); `docs/formate/dateiformate.md`.
- `src/EishockeyManager.Core` – Spielzustand und Regeln. `GameState`, `Club`, `Manager`, `Player` sind `partial`: **neue Daten eines
  Pakets in eine eigene Datei** (`GameState.Finanzen.cs` o. ä.).
- `src/EishockeyManager.UI` – ein `Screen` je Bildschirm, zeichnet in `Surface` (320 × 240 Byte + Palette); Bausteine des Originals
  in `Widgets.cs` (Rahmen aus kb\2, Knöpfe, Lampen, Smileys …).
- `src/EishockeyManager.Audio`, `src/EishockeyManager.Tools` (`emtool`: Export, Datenbank, Bildschirmfotos), `src/EishockeyManager.Viewer`;
  `tests/` – xUnit, je Schicht ein Projekt.

**Verbindliche Architekturregeln:**

- **Zeit vergeht nur über `GameState.Continue()`.** Was je Tag, Woche oder Saison passiert, wird in
  `src/EishockeyManager.Core/GameState.Integration.cs` angeschlossen – keine eigenen Nachhol-Zähler.
- **Echte Einnahmen und Ausgaben nur über `GameState.Book(manager, kategorie, betrag)`.** Neutrale Umbuchungen (Bank, Sparbuch,
  Aktien zum Kurswert, Kredite zwischen Managern) sind kein Aufwand und bleiben getrennt.
- **Bildschirme verändern keinen Spielzustand durch bloßes Öffnen** (keine Zeit, kein Training, kein Markt, kein Zufall). Was das
  Original beim Aufbau des Hauptmenüs ändert (Kennzeichen „neue Nachricht“, Neuaufstellung mit „automatisch aufstellen“,
  Gehaltsfaktor je Managerzug; em.exe Datei 0x44E22), ist ein Ereignis im Spielkern: `GameState.OnMainMenu(manager, isReturn)` bzw.
  der Zuganfang in `GameState.HotSeat.cs`. Das Hauptmenü ruft es genau einmal je Aufbau auf und zeichnet nur (Nutzer E4 = B,
  03.11.2026); keine weiteren Ausnahmen.
- Neue Bildschirme für eine Menükachel ohne fremde Dateien anmelden (Name exakt wie in `Screens/MenuTiles.cs`):

  ```csharp
  internal static class FinanzenMenuEntries
  {
      [MenuEntry("Finanzen")]
      private static Screen Open(ScreenHost host, Screen back) => new FinanzenScreen(host, back);
  }
  ```

- Bildschirmfoto-Abläufe: eigene Datei in `src/EishockeyManager.Tools/Scenarios/`, Methode mit `[Scenario("name")]` und
  `ScreenDriver` (`StartNewGame()`, `PlayMatchDay()`, `OpenTile(…)`, `Click`, `Type`, `Press`, `Save`); ausführen:
  `dotnet run --project src/EishockeyManager.Tools -- screens gamedata <ziel> <ablauf>`.

**Originaltreue:** (1) **Vorlage:** Referenzbilder (1 Bild/s, auf 320 × 240 zugeschnitten: 720p 960 × 720 ab x = 320, 360p
480 × 360 ab x = 0), Originalgrafiken (`emtool export`), Disassembly von `em.exe` (`ndisasm`, `docs/reverse-engineering.md`),
`docs/anforderungen/bildschirme.md`. (2) **Vermessen:** Template-Matching (z. B. Python mit numpy/scipy/PIL); Videobilder sind um
ca. (−0,25 | +0,5) Pixel verschoben und leicht unscharf (`docs/portierung.md`). (3) **Farben:** Paletten auf 6 Bit maskieren
(`Palette.DacToRgb`), häufige Indizes in `Colors` (`Assets.cs`). (4) **Markieren** nach Regel 2.7. (5) **Prüfen:** Bildschirmfoto
rendern, mit dem Videobild vergleichen, Test schreiben.

## 5. Tests

- **Einheitliche Prüfung `tools/pruefen.sh`** (Warnungen als Fehler, gibt die Tabelle für den PR aus):
  - **Normaler PR:** `--schnell` – ein Lauf mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`, enthält alle synthetischen Tests); den Lauf
    ohne Daten übernimmt die CI. Ohne Originaldaten läuft `--schnell` wie die CI → im PR „Originaldaten nicht geprüft“.
  - **Alle drei Läufe** (ohne Option: mit Daten, ohne Daten `EM_SKIP_GAMEDATA=1`, Pflichtlauf ohne Daten, der scheitern **muss**) nur
    bei Integrations-PRs, Erstreviews und Änderungen an der Test-Infrastruktur (`tests/**/Support/`, Testprojekte,
    `tools/pruefen.sh`, `.github/`). Fixläufe und Nachprüfungen: `--schnell` (`zusammenarbeit.md` §5).
  - Wer `tools/pruefen.sh` ändert, führt zusätzlich `tools/pruefen-selbsttest.sh` aus (Testdouble, kein SDK nötig).
  - Während der Arbeit nur betroffene Tests (`dotnet test <projekt> --filter …`), die volle Prüfung erst vor dem Push.
- Tests mit Originaldaten **nur** mit `[GameDataFact]`/`[GameDataTheory]` und `TestGameData.Load()`
  (`tests/EishockeyManager.UI.Tests/Support/GameDataFact.cs`). Nie „ohne Daten einfach `return`“.
- Zeit in Tests nur über die Zeitleiste: `TestSeason.PlayMatchDay(game)` / `PlayRegularSeason(game)`.
- **Mehrsaisonprobe:** Wer Saisonwechsel, Kader oder Geld berührt, liefert einen Test über mindestens 4 Saisonwechsel (passiver und
  aktiver Manager).
- Die CI hat **keine** Originaldaten: grün belegt nur den synthetischen Teil; den Lauf mit Originaldaten lokal ausführen und im PR
  mit Zahlen nachweisen. Testanzahl ist keine Abnahme.

## 6. Git und Pull Requests

- **Branches:** `paket/<spur>`, `fix/<thema>`, `prozess/<thema>`, `integration/<runde>`, `review/<pr>` (nur die Review-Datei). Vor
  jedem Push `git fetch origin && git rebase origin/main` und `tools/pruefen.sh` grün.
- **Commits** auf Deutsch, aussagekräftig, mit Agenten-Trailer (z. B. `Co-Authored-By: ChatGPT <noreply@openai.com>`) – alle
  Agenten schreiben über dasselbe GitHub-Konto.
- **Kleine PRs gegen `main`**, Beschreibung nach `.github/pull_request_template.md` (Base-/Head-SHA, Fehler vorher, Regressionstest
  rot→grün, Testtabelle, Restlücken, `[APPROX]`); per Weboberfläche oder REST (`POST /repos/kaaasi/eishockey-manager-redone/pulls`),
  GraphQL ist nicht überall erreichbar.
- **Status** einer Spur nur im eigenen Abschnitt von `docs/arbeitspakete.md`.
- **Übergabe immer:** Jede Sitzung endet – fertig wie bei Abbruch – mit einem Übergabekommentar im PR (`docs/handoffs/VORLAGE.md`,
  `zusammenarbeit.md` §3).
- **Gemergt wird nur von Leitstelle oder Nutzer** nach der Merge-Prüfung (`zusammenarbeit.md` §6).

## 7. Sparsam arbeiten

Jede Nachricht und jede gelesene Datei kostet Tokens – bei allen Agenten:

- **Antworten an den Nutzer knapp:** Ergebnis, Entscheidungsbedarf, nächster Schritt, mit Links. Keine Zusammenfassung des
  Arbeitswegs, keine Wiederholung dessen, was in PR, Review oder Repo steht, keine Tabelle, wo ein Satz reicht; ausführlich nur auf
  Wunsch.
- **Vorlagen nicht ausschreiben:** Spurstart, Review-Anfrage und Leitstelle laufen über die **Kurzaufrufe** in `docs/vorlagen/`; die
  Leitstelle gibt dem Nutzer nur den ausgefüllten Kurzaufruf.
- **Gezielt lesen:** in `docs/arbeitspakete.md` nur die aktuelle Runde und die eigene Spur; `docs/archiv/`, ältere Reviews und
  `docs/prozess/anhang.md` nur bei Bedarf. Diffs zuerst mit `--stat`, Logs gefiltert (`tail`, `grep`).
- **Lange Inhalte ins Repo** (Bericht, Plan, Review), im Chat nur der Verweis.
- **Tests nach Bedarf** (§5).

## 8. Wo was steht

| Thema | Datei |
|---|---|
| Zusammenarbeit, Rollen, Übergabe, Review, Merge-Prüfung | `docs/prozess/zusammenarbeit.md` |
| Sonderfälle: Vertretung, GitHub Free, Gesamtreview, Codex, geplante Läufe der Leitstelle | `docs/prozess/anhang.md` |
| Eingeschränkte Arbeitsumgebungen (SDK, Netz, VSTest, Freigaben) | `docs/prozess/umgebung.md` |
| Arbeitspakete, Dateigrenzen, Abnahme, Status | `docs/arbeitspakete.md` |
| Kurzaufrufe: Spurstart, Review-Anfrage, Leitstelle, DOSBox-Aufnahme | `docs/vorlagen/` |
| Unabhängige Reviews / abgeschlossene Runden | `docs/reviews/` / `docs/archiv/` |
| Dateiformate, Reverse Engineering, Portierung | `docs/formate/`, `docs/reverse-engineering.md`, `docs/portierung.md` |
| Saisonablauf, Tagesablauf | `docs/saison.md`, `docs/tagesablauf.md` |
