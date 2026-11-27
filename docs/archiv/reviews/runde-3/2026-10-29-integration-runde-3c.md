## 1. Merge-Empfehlung

**Nein für Head `1d52a63dd02c266053b75ce4adb399d9e4b6f9f8`**, Base
`f642ef80ef450b0e79c3513429a04dc91b50078f`, PR [#89](https://github.com/kaaasi/eishockey-manager-redone/pull/89).

Unabhängiges Erstreview von #87 (`ceb3afc`) und #88 (`88a149c`) auf dem Integrationsstand gegen Welle C.
R3C-01 verletzt den dokumentierten Autosave-Zeitpunkt im Hot-Seat. Außerdem bleibt der reguläre
Drei-Läufe-Nachweis wegen einer Sandbox-Sperre offen; die unten ausgewiesene direkte xUnit-Gegenprüfung ist kein
erfolgreicher Lauf von `tools/pruefen.sh`. Keine vorgegebenen Gegenbeispiele oder ausdrücklich zurückgestellten Befunde.

## 2. Befunde

### R3C-01 · P2 · Autosave beim Managerwechsel hängt vom Kachelbesuch ab

Stelle: [MainMenuScreen.cs:99–102](https://github.com/kaaasi/eishockey-manager-redone/blob/1d52a63dd02c266053b75ce4adb399d9e4b6f9f8/src/EishockeyManager.UI/Screens/MainMenuScreen.cs#L99-L102),
zusammen mit [AutoSave.cs:46–65](https://github.com/kaaasi/eishockey-manager-redone/blob/1d52a63dd02c266053b75ce4adb399d9e4b6f9f8/src/EishockeyManager.UI/Screens/SaveLoad/AutoSave.cs#L46-L65).

Beim bloßen Weitergeben an den nächsten Manager bleibt derselbe Bildschirm aktiv. Nach `Game.Continue()`
fehlt deshalb der Aufruf von `AutoSave.OnMainMenu`. Das ist relevant, obwohl nur der erste aktive Manager den Zähler
**erhöht**: Die Prüfung auf 10 und das Schreiben stehen vor dieser Bedingung. Genau diese Reihenfolge bestätigt
`em.exe` 0x44E49–0x44E69. Auch die dokumentierte Remake-Abbildung zählt jeden neuen Zug (Spiel, Saison, Tag, Manager).

**Nachgestellt auf dem geprüften Head:** temporäres Konsolenprogramm mit Verweisen auf Core.Tests und UI;
`ScreenDriver.Create(new GameData(...))`, `TestGames.CreateDatabase()`, Seed 3, Manager A/Kopf 0/Verein 2 und
B/Kopf 1/Verein 30, eigener leerer Spielstandordner, `AutoSaveOverride = true`.
Spiel in `Host.Game` setzen, `Host.Show(new MainMenuScreen(host))`; danach:

1. 16-mal `driver.PlayMatchDay()`: Tag 24, Manager 0, Zähler 10, noch keine Datei.
2. Ein weiteres `PlayMatchDay()`: Manager 1 am selben Tag, weiterhin Zähler 10, keine Datei.
3. `OpenTile("Laden")`, danach Escape: **erst jetzt** entsteht AUTOSAVE.ems, Zähler 0.
4. Gegenprobe ohne Schritt 3: Erst das nächste `PlayMatchDay()` schreibt, nun am Tag 26 für Manager 0.

| Stelle im Ablauf | Ohne Kachelbesuch: Zähler / Schreibvorgänge | Mit Kachelbesuch: Zähler / Schreibvorgänge |
|---|---:|---:|
| Manager 0, Tag 24 | 10 / 0 | 10 / 0 |
| Übergabe an Manager 1, Tag 24 | 10 / 0 | 10 / 0 |
| Laden öffnen und abbrechen | entfällt | 0 / 1 |
| Nächster Tag, Manager 0, Tag 26 | 1 / 1 | 1 / 1 |

Damit enthält der automatische Spielstand je nach Bedienung einen anderen Tag und eine andere
Hot-Seat-Bereitschaft. Die bekannte Angabe „Hot-Seat-Zählung ohne Test“ beschreibt hier einen tatsächlichen Fehler.
Der Zustandsabdruck des laufenden Spiels wird durch das Speichern selbst weiterhin nicht verändert.

**Abhilfe/Regression:** Den neuen Zug auch beim direkten Managerwechsel am vollständigen Haltepunkt an den
Autosave-Hook melden. Zwei ansonsten identische UI-Abläufe mit und ohne Kachelbesuch müssen zum selben Zeitpunkt
dieselben Autosave-Bytes ergeben; der Kachelbesuch darf keinen weiteren Schreibvorgang auslösen.

Keine weiteren bestätigten P0–P2-Codebefunde.

## 3. Ausgeführte Prüfungen und akzeptierte `[APPROX]`

### Prüfstand und Abnahme

- Head zunächst über den GitHub-Connector geladen, alle 32 geänderten Dateien per Blob-SHA geprüft.
  Anschließend Original-Commitobjekt einschließlich Signatur rekonstruiert und ausgecheckt:
  `git rev-parse HEAD = 1d52a63dd02c266053b75ce4adb399d9e4b6f9f8`,
  Tree `82ce87ac49adab49ec9d33d0ce05819e4d8b2f95`; Arbeitsbaum sauber.
  Keine Implementierungsänderung am geprüften PR.
- Original-ZIP aus dem privaten Referenzrepo, SHA-256
  `142fc78e9b286cb47f36baa450f5af311ac0902525564d0c30ab63697ca321f4` bestätigt.
  Originaldateien und Referenzbilder bleiben außerhalb des Review-Commits.
- SDK 10.0.402; `dotnet build EishockeyManager.slnx -c Release --no-restore -m:1 -p:UseSharedCompilation=false -warnaserror -v q`:
  **0 Warnungen, 0 Fehler**. Offline-Restore aus vorhandenem Paketcache; `NuGetAudit=false` wegen fehlender
  Netzwerkverbindung. Ein Prozessor und abgeschalteter Compiler-Server vermeiden weitere Sandbox-Probleme.
- **Echte Migration 2 → 3:** Schema-2-Writer des lokalen `51c110a` verwendet; GitHub-Compare bestätigt, dass bis
  Base `f642ef8` ausschließlich `docs/arbeitspakete.md` geändert wurde, also identischer alter Laufzeitcode.
  Vier Ausgangsstände mit Originaldaten: Saisonstart und laufende Saison mit ernanntem Bundestrainer,
  automatischer Nominierung und Freundschaftsspiel, jeweils DEB-Pokal an/aus; Nationalkonto 0 bzw. 300.000 DM.
  Acht unveränderte Schema-2-Dateien einschließlich der vier um drei `Continue()` fortgesetzten Stände geladen.
  Fahne an, Pokalschalter erhalten, Modus 4, Nationalkonto = Summe der Übertragsbuchungen, Schema-3-Roundtrip erfolgreich.
  Für alle vier Ausgangsstände: drei Schritte nach Migration ergeben **bytegleich** das Ergebnis des alten
  Weiterlaufs, anschließend migriert. Das ergänzt den vorhandenen Test, der Schema 2 aus Schema 3 nachbildet.
- **Modusbits und Sperren unabhängig aus em.exe:** `objdump -D -b binary -m i8086`, Bereiche
  0x28717–0x28754 (Fahne/Managerzahl), 0x29838–0x29867 (Bits 1→2, 2→1, 3→4),
  0x54CEE–0x54D38 (Sprung zur WM), 0x44FBD–0x45020 (Manager-5-Tabelle und Modus-2-Sperren).
  84 Tabellenbytes ab Datei `0x16DD0 + 0x5159` gelesen: passen zur Übertragung in `NationalMenu`;
  Modus 2 sperrt die angegebenen vier WM-Kacheln.
- **Autosave:** 0x44E22–0x44E6D nachdisassembliert: Prüfung auf 10 vor der Erhöhung nur für Manager 0 bestätigt.
  Callchain `MainMenu.Enter → AutoSave → SaveGame.Save` geprüft; kein Core-Aufruf des Autosaves aus
  `Continue()`. Roundtrip-, Zustandsreinheits- und Mehrsaisontests grün in der direkten Gegenprüfung.
  UI-Hot-Seat-Gegenbeispiel siehe R3C-01.
- **Abnahme Welle C:** Modus 1 bis durch die WM ohne Vereinsbuchungen, vier Saisonwechsel mit passivem/aktivem
  Bundestrainer und Modus-4-Referenzwerte durch die selbst ausgeführten Tests bestätigt.
  Öffnen/Abbrechen, Dialog-Roundtrip, falsche Datei/höhere Version, Überschreiben/Löschen und Integration
  im Bundestrainer-Menü ebenfalls grün. Die Autosave-Abnahme bleibt wegen R3C-01 offen.
- **Gemeinsame Dateien:** Begründungen in #87/#88 mit Diff abgeglichen. `MainMenuScreen.cs`: Sperren,
  National-Weiterleitung/Infoleiste und ein Autosave-Enter-Hook; `SetupScreen.cs`: Modusknöpfe und Laden-Notizbuch;
  `MenuTiles.cs`: sechs Tabellenplätze; `GameState.Integration.cs`: unverändert geordneter Vereins-Tageslauf
  hinter Modusprüfung; `SeasonFlow.cs`: WM-Sprung/-Halte, keine Vereinsfolgen/Prämien in Modus 1.
  Minimal und funktional begründet. Die Abweichung von der ursprünglichen Autosave-Anbindung am Tagesabschluss
  ist wegen des belegten Hauptmenü-Zeitpunkts sachgerecht; ihre Hot-Seat-Anbindung ist unvollständig.
- **Bildschirmfotos:** `dotnet emtool.dll screens gamedata <ziel> speichern weltkugel wettbewerbe wm nationalteam`:
  34 PNGs ohne Fehler. Laden-Bild gegen privates `oberflaeche_0102` angesehen: Rahmen, Spalten, Balken und Knöpfe
  passen; Dateiliste/Pfad unterscheiden sich erwartungsgemäß. Pixelvergleich der 57×13-Modusknöpfe, RGB-MAE /
  Pixelanteil mit maximaler Kanalabweichung >40: Fahne aus zu 0214 **6,72 / 4,45 %**, Weltkugel zu 0219
  **7,12 / 6,48 %**; beide an zu 0230 **5,79 / 2,02 %** und **6,97 / 6,34 %**.
  Keine neue Behauptung eines pixelgleichen Bundestrainer-Menüs ohne Videobeleg.

### Standardprüfung und zusätzliche Testgegenprüfung

Befehl auf dem exakten, sauberen Head:

```sh
NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=1 \
PATH=/workspace/.dotnet:$PATH tools/pruefen.sh
```

Build erfolgreich, **alle drei Teststarts durch VSTest abgebrochen**:
`System.Net.Sockets.SocketException (13): Permission denied` beim lokalen `TcpListener`.
Das ist ein Umgebungsfehler, kein Nachweis eines Testfehlers im Produkt. Die angeforderte zusätzliche
Netzwerkberechtigung kam nicht zur Ausführung; keine erfolgreiche Standardprüfung behauptet.

Tabelle aus diesem Skriptlauf (Exit 1; Nullwerte bedeuten hier **keine Testergebnisse**):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 0 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 0 | 0 | 0 |
| Pflichtlauf ohne Daten | 0 | 0 | 0 – FEHLER: keine Ergebnisse, Exit 1 |

Zusätzlich die gebauten vier Testassemblies direkt mit dem vorhandenen xUnit-Runner 2.8.2
(`AssemblyRunner.WithoutAppDomain`, seriell, ein Thread) ausgeführt. Separater Prozess je Umgebung,
gleiche Variablen wie im Skript, keine VSTest-TCP-Verbindung. Befehle im temporären Review-Harness:
`dotnet review89-harness.dll tests`, jeweils mit `EM_REQUIRE_GAMEDATA=1`,
`EM_SKIP_GAMEDATA=1` bzw. beiden Variablen:

| Direkte xUnit-Gegenprüfung | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten | 660 | 0 | 0 |
| Ohne Originaldaten | 561 | 0 | 84 |
| Pflichtlauf ohne Daten | 561 | 99 | 0 |

Alle 99 Pflichtlauf-Fehler enthalten „Originaldaten fehlen“; keine anderen Fehler. Die unterschiedlichen
Gesamtzahlen entstehen aus der xUnit-Theorieerkennung beim Überspringen. Nicht als `pruefen.sh`-Erfolg gewertet.

**Mutationsgegenproben in separater temporärer Quellkopie:** Ernennung beim WM-Sprung entfernt und Migration mit
Fahne aus statt an: `GameMode*` 7/17 rot (Saisonlauf, Mehrsaisonprobe, Migration, Coach-Roundtrip).
Autosave-Schreiben entfernt: `SaveLoadScreenTests` 3/23 rot (Intervall, UI-Weg, Mehrsaisonprobe).
„WM“ aus Freischaltungstabellen entfernt: `WorldOnlyMenuTests` mit Tabellenprüfung rot.
Gezielte Gruppenproben, keine vollständige Mutation jedes einzelnen neuen Tests.

### Akzeptierte `[APPROX]`

Für diesen Reviewumfang nachvollziehbar: Halt vor dem ersten WM-Spiel zur Kaderwahl, genau ein Manager im Modus 1,
Bundestrainer-Menü bereits vor Ernennung mit Mannschaftssperren, Auslassen der Vereinsfolgen im Modus 1;
Nationalaufstellung/-taktik als ausgewiesene Platzhalter. Dialogbedienung ohne Videobeleg, gleicher Aufbau
für Sichern/Laden, nachgezeichnete Pfeile, eigener EMS-Ordner und Dateinamenregeln, wirkungslose Laufwerksknöpfe
und fehlende „.“/„..“-Einträge. Autosave-Einstellung, Unterdrückung am Spielende, erster aktiver Manager und
Zählung je neuem Zug sind dokumentierte Abbildungen. **R3C-01 ist davon nicht gedeckt.**

### Annahmen und verbleibende Prüfgrenzen

Die fehlenden DEB-/Europapokal-Tabellen und Nationalaufstellung/-taktik bleiben die ausdrücklich dokumentierten
Platzhalter; Bundestrainer im Modus 4 ist nicht enthalten. Unabhängig gemessene Knopfabweichungen begründen keine
vollständige Pixelgleichheit des Dialogs. Der erfolgreiche reguläre Drei-Läufe-Nachweis muss in einer Umgebung
mit erlaubtem VSTest-Kanal nachgeholt werden.
