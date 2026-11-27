# Review Integration Runde 13 – Spur `ui-rest` (PR #588)

Unabhängiges Review (Claude Opus 5.5, geplanter Lauf, frische Sitzung ohne Mitarbeit an #570/#572/#573/#574/#588).
Base `c4e557f539f8c343978bc0f64a632084ccb63c63` (`integration/runde-13-nationalteam`, #587, inzwischen in `main`), Head
`10656c9dbacb96112cdb2407a28da992c66c08fd`. Geprüft wurde nur das Delta `c4e557f..10656c9` (22 Dateien).

## 1. Merge-Empfehlung

**Ja für `10656c9dbacb96112cdb2407a28da992c66c08fd`.** Geprüft am Klon: `git rev-parse HEAD` = Head, `git status --porcelain`
leer. Mit Ausführung. `tools/pruefen.sh` hat am Head alle drei Läufe bestanden, mit denselben Zahlen wie im PR. Die Gegenbeispiele
mit Verhaltensänderung sind am Base rot und am Head grün. Die übrigen Punkte (Vereinsrekorde, R12U-03, R-K2, Umbruchbreite) ändern
nur Doku oder Tests. Für sie habe ich per Mutation am Head gezeigt, dass die neuen Tests rot werden. Die Fundstellen in em.exe
stimmen. Die Hinweiszeile G7 deckt sich pixelgenau mit dem DOSBox-Bild. Es gibt keinen P0–P2-Befund. Die drei P3-Befunde
blockieren nicht. CI `build` am Head: success. PR mergebar (`clean`) gegen `main` `0aae331`; `main` hat seit dem Base nur die
beiden Review-Dateien dazubekommen.

## 2. Befunde

- **P3 – R13IU-01: Stadionpfeile 2 Spalten zu schmal gegenüber dem Original.**
  Stelle: [`StadiumScreen.Arrows`](https://github.com/kaaasi/eishockey-manager-redone/blob/10656c9dbacb96112cdb2407a28da992c66c08fd/src/EishockeyManager.UI/Screens/Manager/StadiumScreen.cs#L405)
  zeichnet nur `ArrowPair.DrawReleased`, also kb\4 (22 | 1 + 9 · j), 8 × 7 nach (x + 2 | y + 1). In den DOSBox-Bildern
  `file01_3_stadion` und `file02_1_stadion_eintritt17` (Referenz-Repo, 642 × 507, auf 320 × 240 gebracht mit Schritt 2 ab (1 | 26))
  ist jeder Eintrittspfeil 10 Pixel breit: Kante und Schatten liegen auch in den Spalten x und x + 1. Der linke Pfeil hat dort
  Pixel bei (192–193 | 193–195), der rechte bei (202–203 | 190–192). Gegenüber dem Remake (neues Spiel, Maus auf Gastronomie)
  unterscheiden sich im Feld (190…211 | 189…197) genau diese 10 Pixel, in beiden Bildern gleich. Die Werbung hat das Problem
  nicht: sie blittet zuerst die 10 breite Grundgrafik kb\4 (20 | 0) (0x2B2CE).
  Vorschlag: im Stadion die Grundgrafik der Pfeile 10 breit wie im Original zeichnen. Den Aufruf im Aufbau 0x3D7F8 ff.
  nachlesen, dann einen Pixeltest gegen `file01_3` schreiben. Für Runde 14.
- **P3 – R13IU-02: Falsche Adresse im Kommentar.**
  [`GameState.MainMenu.cs` Z. 58](https://github.com/kaaasi/eishockey-manager-redone/blob/10656c9dbacb96112cdb2407a28da992c66c08fd/src/EishockeyManager.Core/GameState.MainMenu.cs#L58)
  nennt „Funktionsanfang 0x44E3F“. Der Funktionsanfang ist 0x44E22 (`push bp`). Bei 0x44E3F steht der Aufruf `0x38:0x847(0x55, 0x73)`,
  danach `mov [0xE1A4], al` (0x44E46). Inhaltlich ist der Kommentar richtig: Jeder Eintritt in 0x44E22 zieht den Faktor, also auch der
  erste. Zu korrigieren ist nur die Adresse; das gleiche steht in der Beschreibung von #574.
- **P3 – R13IU-03: Datei einer anderen Spur geändert (nur Kommentar).**
  [`Core/CupsNational.TeamInfo.cs`](https://github.com/kaaasi/eishockey-manager-redone/blob/10656c9dbacb96112cdb2407a28da992c66c08fd/src/EishockeyManager.Core/CupsNational.TeamInfo.cs#L126)
  gehört laut `docs/arbeitspakete.md` (Runde 13, Zuständigkeit) allein `nationalteam`. Geändert sind nur Kommentarzeilen, und #572
  begründet das. Ich nehme es hin. Die Leitstelle sollte es aber beim Abhaken der Dateigrenzen erwähnen. Die übrigen Dateien
  liegen in den Grenzen der Spur oder sind in #570/#574 begründet: `UI/ArrowPair.cs` (Baustein), `Core/NewGame.cs` (eine Zeile),
  `Core/GameState.MainMenu.cs`, `Core/Transfer.Contract.cs` (Kommentar) für #540. Die Ausnahme #565 deckt die Zeile A324 in
  `inventur.md` ab.

**Besonders geprüft:**

- **Mausblock x/y (5da4804).** Die Korrektur betrifft nur `StrengthScreen.RowAt`. Andere Aufrufer gibt es nicht. Der Block
  [0xC147] ist an allen gelesenen Stellen gleich belegt: +0 = y, +2 = x. Belege:
  - Werbung 0x2B9B2/0x2B9B9: +2 wird gegen 36…282 geprüft, +0 gegen 28…54.
  - `ArrowPair` 0x4BA6F–0x4BABA: dx = +2 gegen x1 + 8, bx = +0 gegen y1 + 9. Das passt zum Baustein.
  - Stadion 0x3DB58: si = +0, `cmp si, 0xA7`.
  - Stärken 0x519AA: si = +0, 0x51A04–0x51A37: 27 < y < 213, `idiv` durch 15, Zeile < [0x7B1 + Liga].

  Im Remake gibt es keine weitere Zeilenprüfung der Form `(MouseX − n) / …` (grep leer).
- **`MainMenuScreen.cs`** ist im Delta unverändert, ebenso `GameState.cs` und `GameState.Integration.cs`. Das Zeichnen ändert nichts
  (E4 = B). Probe: Ein Aufruf von `OnMainMenu` in `Draw` macht `MainMenuDrawTests` rot.
- **Dateigrenzen:** siehe R13IU-03.
- **Alle drei Läufe** am Head: siehe 3.

**Erwartet offen, bestätigt:**

- `[APPROX]` Bremse der Werbepfeile, 20/6 Frames (`AdvertisingScreen.RepeatDelayFrames/RepeatEveryFrames`). Das Original blättert
  ungebremst je Schleifendurchlauf (0x2BE0A ff.).
- Farbe 1 der Bau-Tabelle und „(Noch n Wochen)“ sind nicht am Bild geprüft. Die Farbe ist aus em.exe belegt: 31 − 30 · Modus bei
  0x3D5FB und 0x3D737. Ein DOSBox-Bild im Bau-Modus fehlt, Sitzung 3 von `runde-13` steht aus.
- Die Inventurzeilen außer A324 trägt `pruefung-karte` nach (A305, A326, A327, F091, F106, A016 unverändert).

## 3. Ausgeführte Prüfungen

Einrichtung nach `CLAUDE.md`: `fetch-reference.sh original`, `setup-workspace.sh`, SDK 10.0.402. Klon detached auf `10656c9`.

`tools/pruefen.sh` (alle drei Läufe) am Head, Ergebnis `bestanden`:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2217 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1859 | 0 | 322 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1860 | 357 | 0 |

**Gegenbeispiele am Base.** Worktree `c4e557f`, darin die Testdateien des Heads, mit Originaldaten. Für neue Schnittstellen habe ich
die Tests minimal angepasst:
- Konstanten 20/6 statt `RepeatDelayFrames`/`RepeatEveryFrames`.
- `ShowsBuildTable`-Asserts entfernt; `Assert.Same(screen, Host.Current)` bleibt.
- `BuildTexts` (nur neue API) entfernt.
- Stärken: eigene Probe, die am Base und am Head übersetzt. Sie klickt auf (100 | 62) und (300 | 62) und prüft, ob sich das Bild
  ändert und der Bildschirm bleibt.

| Gegenbeispiel | Base | Head |
|---|---|---|
| #535 Pfeile Werbung 0x2BA72: Flächen 8 × 9, Halten mit Bremse, Klemmen, gedrückt aus kb\4 (`AdvertisingArrowTests`, 3) | rot (3) | grün |
| #535 Pfeile Stadion 0x3DD89/0x3E0BC: Flächen, links = mehr (0x3DDB3), Halten wiederholt nichts (`StadiumArrowTests`) | rot | grün |
| Info-Fenster aus „Stärken“, Prüfung auf y, x egal (Probe, 2 Fälle; am Head zusätzlich `StrengthInfoWindowTests`, 11) | rot (2) | grün |
| Stadion rechte Taste: y > 167 schaltet um (0x3DB5B), sonst Verlassen (0x3E4D4) (`StadiumBuildTableTests`) | rot | grün |
| R12U-04 Faktor im ersten Zug 0x44E22/0x44E3F (`FirstTurnFactorTests`, 3 Seeds) | rot (3) | grün |
| `NewsBar.Shown` bei gleicher Anzahl (`NewsBarShownTests`) | rot | grün |
| Vereinsrekorde nur Code 7 (0x50283), Saison mit Pokal und Play-offs (`ClubRecordsSeasonTests`, 2 Seeds) | grün (keine Codeänderung, erwartet) | grün |
| R12U-03 Zeichnen des Hauptmenüs (`MainMenuDrawTests`) | grün (Testlücke, erwartet) | grün |
| R-K2 Farben 18/12/22 (`SeasonEndColorsTests`) | grün (nur Test, erwartet) | grün |
| R12U-05 (Kommentar `MainMenuEventTests`) und `MainMenuEventTests` | grün | grün |

**Mutationen am Head.** Je eine Mutation, danach zurückgesetzt; `git status` am Ende leer.

| Mutation | betroffene Tests |
|---|---|
| `Draw` ruft `OnMainMenu(…, false)` | `MainMenuDrawTests` rot (1/1) |
| `Cups.Tiebreak.Play` trägt Rekorde ein (Pokal) | `ClubRecordsSeasonTests` rot (2/2) |
| `Playoffs.Play` trägt Rekorde ein (Play-offs) | `ClubRecordsSeasonTests` rot (2/2) |
| `RowAt(input.MouseX, …)` statt Maus-y | `StrengthInfoWindowTests` rot (2/11) |
| Stadion `MouseY >= 167` statt `> 167` | `StadiumBuildTableTests` rot (1/2) |
| `BeginFirstTurn()` in `NewGame.Create` auskommentiert | `FirstTurnFactorTests` rot (3/3) |
| Werbung wieder mit Umbruch `(Cursor + delta + count) % count` | `AdvertisingArrowTests` rot (1/3) |
| Neuauswertung bei neuem Drücken in `ArrowPair.Update` entfernt | rot (1/17 aus ArrowPair/Werbung/Stadion) |

**Fundstellen nachgelesen** (`ndisasm -b16`, Dateiadressen):

- **Pfeile.** Stub 0x120:0x3E, Argumente in C-Reihenfolge:
  - Werbung 0x2BA5F ff.: (138, 183, 138, 192, 0).
  - Stadion 0x3DD76 ff.: (192, 189, 202, 189, 0); 0x3E0A9 ff.: (192, 210, 202, 210, 0).
  - Eintritt: neu = alt − Rückgabe, nur bei 9 < neu < 31 (0x3DDB3–0x3DDC5). Das entspricht `Clamp(10, 30)` bei Schritt ±1.
  - Werbung: Index klemmt bei 9 bzw. 4 (Fernsehen, Fläche 8) (0x2BE1C–0x2BE38). `Companies` hat 10 Einträge, `Stations` 5.
- **Wettbewerbscode.** 0x50283 prüft [0xD0C2] auf 7 oder 8 (0x5030D–0x5031B), sonst Sprung ans Ende. Alle Setzstellen in em.exe:
  1 (0x31AB7), 3, 6 (0x35E7C), 7 (0x35EC4), 8, 10, 13. 8 ist die WM-Gruppe (Länder, eigener Pfad). Die Aussage
  „nur Ligaspiele der Vereine“ stimmt.
- **Bau-Tabelle.** Texte „(keine Baut{tigkeiten)“ und „(kein Bau...)“ (DGROUP, `{` = ä), „ (Noch “ und „ Wochen“ stehen in
  em.exe. Farbe 31 − 30 · [bp+0xA] bei 0x3D5FB und 0x3D737.
- **Umbruchbreite.** 158 = Linie 80…237 von 0x4C246. Der Umbruch selbst ist richtig als `[APPROX]` markiert. Die Breite 158 stand
  schon am Base im Code, das Delta ändert nur den Kommentar.

**Pixel.**

- **Hinweiszeile G7.** Remake gerendert: neues Spiel, Stadion, Maus auf (20 | 216) = Zeile Gastronomie. Gegen `file01_3_stadion`:
  Bereich (205…319 | 196…207), Begrenzung beider Bilder (213 | 201)–(304 | 205), Textmaske identisch (0 abweichende Pixel).
- **R-K2.** Die RGB-Werte (243, 0, 0), (81, 97, 16) und (48, 65, 195) kommen in `file13_14_saisonende_uebersicht` und
  `file16_8_saisonende` vor.

**Annahmen (nicht geprüft):**

- Der Bildmaßstab der DOSBox-Aufnahmen (Schritt 2 ab (1 | 26)) ist aus Rand und Titelleiste abgeleitet. Er passt pixelgenau zur
  Hinweiszeile.
- Die Grundgrafik der Stadionpfeile in R13IU-01 habe ich nur aus den Bildern erschlossen, nicht aus dem Aufbau 0x3D7F8.

**Akzeptierte `[APPROX]`:**

- Bremse der Werbepfeile.
- Neuauswertung in `ArrowPair` bei neuem Drücken.
- Blättern aus dem Vertragsmodus (Cursor −1) beginnt am Ende.
- Umbruch in `SaveLoadScreen.Wrap`.
- Laufende Baustelle in der Stufenwahl.
