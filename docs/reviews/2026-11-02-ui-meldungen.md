# Review: Spur `ui-meldungen` (#421, #426, #430)

Unabhängiges Review (geplanter Lauf, Claude Opus 5.5, ohne Implementierungskontext) nach `docs/vorlagen/review-anfrage.md`.

| PR | Base | Head |
|---|---|---|
| #421 Teil 1 | `7332a0fd04628e1a63acde30d9131ecb2f2d2afb` | `adb1815ed3af751f8dd9ba941de97547905f7224` |
| #426 Teil 2 | `adb1815ed3af751f8dd9ba941de97547905f7224` | `bcb2458a2007d95546b9cf3a31cd7c8cfea02e11` |
| #430 Teil 3 | `bcb2458a2007d95546b9cf3a31cd7c8cfea02e11` | `a65f3e88c6e59b3260c190367a002cfd75ecfa9a` |

Geprüft am Klon (`git rev-parse HEAD` = Head, `git status --porcelain` leer), Build und Tests selbst ausgeführt, nicht nur Codelesen.

## 1. Merge-Empfehlung

- **#421 @ `adb1815`: ja.** Baustein 0x4C246/0x0C0B8 stimmt im Pixelvergleich mit dem Video (Versatz 0 | 0), alle 11 gezielten
  Mutationen werden rot, `--schnell` grün. Die Änderung an `MainMenuScreen.cs` (Bundestrainer-Warnung) liegt außerhalb der
  Ausnahme R11P-02, ist aber im PR benannt und begründet (R11U-01, P3 – Entscheidung Leitstelle).
- **#426 @ `bcb2458`: ja.** Kurve und Achsen lagegenau an `oberflaeche_0842` und `folge06_00000`, Infofile zustandsrein,
  alle 12 Mutationen rot, `--schnell` grün. `MainMenuScreen.cs` nur Aufruf, Achsen und versteckter Klick (innerhalb R11P-02).
- **#430 @ `a65f3e8`: nein (nur wegen R11U-02).** Inhalt korrekt (Monatstabelle an `em.exe` nachgeprüft, S-K5 per Mutation
  belegt, alle drei Läufe grün), aber der Status steht im Abschnitt der Spur `marker-werkzeug` und überschreibt deren
  Status. Nach Korrektur dieser einen Zeile ohne weitere Prüfung mergebar (nur Doku).

Reihenfolge: #421 → #426 → #430 (Stapel; Base von #426/#430 sind die Spur-Branches, vor dem Merge auf `main` umstellen).

## 2. Befunde

### R11U-02 · P2 · Status in fremdem Abschnitt (#430)

[`docs/arbeitspakete.md:183` @ a65f3e8](https://github.com/kaaasi/eishockey-manager-redone/blob/a65f3e88c6e59b3260c190367a002cfd75ecfa9a/docs/arbeitspakete.md#L183):
Commit `a65f3e8` setzt „Status: Teil 1 #421, Teil 2 #426, Teil 3 … fertig, Review offen“ in den Abschnitt
`### Spur marker-werkzeug` (Zeilen 169 ff.; deren Status war „–“). Der eigene Abschnitt (Zeile 165) sagt weiterhin
„Teil 1 in Arbeit“. Verstößt gegen AGENTS.md §6 („Status nur im eigenen Abschnitt“) und verfälscht den Stand von
`marker-werkzeug`. Gegenbeispiel: `git show a65f3e8:docs/arbeitspakete.md | sed -n 169,184p`.
Behebung: Zeile 183 auf „**Status:** –“ zurück, Text in Zeile 165 übernehmen.

### R11U-01 · P3 · `MainMenuScreen.cs` außerhalb der Ausnahme R11P-02 (#421)

[`MainMenuScreen.cs:54/121–126` @ adb1815](https://github.com/kaaasi/eishockey-manager-redone/blob/adb1815ed3af751f8dd9ba941de97547905f7224/src/EishockeyManager.UI/Screens/MainMenuScreen.cs#L54):
R11P-02 erlaubt in dieser Datei nur Auslagern von `DrawRankCurve`, Kurvenaufruf und versteckten Klick („sonst nichts“) und
sagt „Aufrufer in fremden Dateien stellt `ui-meldungen` nicht um“. Teil 1 stellt die Bundestrainer-Warnung (A298) dort
auf `ShortMessage` um (Property, Update, Draw) und ändert dafür `Testing/ScreenDriver.cs`, `MainMenuBefundeP3Tests`,
`WettbewerbeNationalteamTests`. Der Auftrag der Spur nennt die Bundestrainer-Warnung ausdrücklich, der PR begründet die
Änderung, sie ist klein und getestet (Mutation M06, Fingerabdruck in `MainMenuBefundeP3Tests`). Kein Fehler im Verhalten –
die Leitstelle entscheidet, ob sie die Ausnahme nachträglich deckt oder die Umstellung in die Integration verschiebt.
Dasselbe gilt sinngemäß für `QuitQuestion.cs` (in `re-meldungen.md` K1 als umzustellen genannt, nicht in R11P-02).

### R11U-03 · P3 · #430 nennt `Season.cs` nicht unter „Gemeinsame Dateien“

PR-Text #430: „Gemeinsame Dateien: Keine“, obwohl `Core/Season.cs` (Ausnahme R11P-01, nur `Months`) geändert ist. Diff
geprüft: nur das Feld `Months` und dessen Kommentar – inhaltlich innerhalb der Ausnahme; nur die PR-Angabe fehlt.

### R11U-04 · P3 · Infofile: leere Schlussseite des Originals fehlt

[`InfoFileScreen.cs:158` @ a65f3e8](https://github.com/kaaasi/eishockey-manager-redone/blob/a65f3e88c6e59b3260c190367a002cfd75ecfa9a/src/EishockeyManager.UI/Screens/InfoFileScreen.cs#L158):
Nach `re-meldungen.md` §D Schritt 4 folgt auf jedes Seitenende ein neuer Seitenkopf mit „TASTE!“; endet die Datei direkt
nach einem „#“ oder genau nach der 27. Zeile, zeigt das Original also noch eine leere Seite. `Layout` lässt sie weg
(`glyphs.Count > 0 || pages.Count == 0`). Gegenbeispiel: `InfoFileScreen.Layout("A#"u8, _ => 5).Count` = 1, nach Regel 2.
Das Original-`infofile` (1153 Byte, kein „#“, 34 Zeilen) ist nicht betroffen; nur für fremde Dateien. Mit `[APPROX]`
vermerken oder angleichen.

### R11U-05 · P3 · Zustandsreinheit des Infofiles nur über `DateText` getestet

[`InfoFileAndRankCurveTests.cs:150` @ a65f3e8](https://github.com/kaaasi/eishockey-manager-redone/blob/a65f3e88c6e59b3260c190367a002cfd75ecfa9a/tests/EishockeyManager.UI.Tests/InfoFileAndRankCurveTests.cs#L150)
vergleicht nur das Datum. Mit einem temporären Test (nicht committet) per `StateFingerprint.Of` nachgeprüft: Öffnen,
Blättern und Zurück lassen den Fingerabdruck unverändert – Verhalten korrekt, nur der Test ist schwach. Empfehlung:
`StateFingerprint` statt `DateText`.

### Hinweis außerhalb des Diffs (kein Befund dieser PRs)

Schon auf `main` (`7332a0f`) ändert bloßes Zeichnen des Hauptmenüs nach zwei Spieltagen (5 × `Frame()`) bzw. ein Klick
auf (10 | 10) den `StateFingerprint` (ein bool-Feld wird `True`, vermutlich Gelesen-Markierung der Nachrichtenleiste).
Gleiches Ergebnis an `a65f3e8`. Zur Einordnung durch die Leitstelle (ggf. gewollt).

## 3. Ausgeführte Prüfungen

### Builds und Tests

| Head | Befehl | mit Daten | ohne Daten | Pflichtlauf | Ergebnis |
|---|---|---|---|---|---|
| `a65f3e8` | `tools/pruefen.sh` | 1664 / 0 / 0 | 1376 / 0 / 259 | 1376 / 288 / 0 (scheitert wie gewollt) | bestanden |
| `bcb2458` | `tools/pruefen.sh --schnell` | 1663 / 0 / 0 | – | – | bestanden |
| `adb1815` | `tools/pruefen.sh --schnell` | 1646 / 0 / 0 | – | – | bestanden |

(bestanden / fehlgeschlagen / übersprungen; SDK 10.0.402, Originaldaten aus `kaaasi/eishockey-manager-referenz`.)

### Gegenbeispiele: Bildschirmfoto gegen Referenzbild

Abläufe `meldungen` und `infofile-kurve` (`dotnet run --project src/EishockeyManager.Tools -- screens gamedata …`) an
`a65f3e8`; Vergleich RGB, Kennzahl MAD (mittlere größte Kanalabweichung) und Anteil Pixel mit Abweichung < 40, bester
Versatz im Bereich ±2 (Mauszeiger ausgenommen):

| Baustein | Referenz | Bereich | bester Versatz | MAD | Anteil < 40 |
|---|---|---|---|---:|---:|
| M-K1 Frage 0x4C246, ganzes Fenster | `oberflaeche_0346` | 75…242 × 107…182 | 0 \| 0 | 17,8 | 83,7 % |
| – Symbolfeld links / rechts | 〃 | 83 / 212 \| 112, 22 × 22 | 0 \| 0 | 13,4 / 9,4 | 91,1 / 98,3 % |
| – Überschrift | 〃 | 105…209 × 115…127 | 0 \| 0 | 11,5 | 90,6 % |
| – Knöpfe | 〃 | 94…223 × 164…177 | 0 \| 0 | 17,4 | 94,3 % |
| – Textzeilen | 〃 | 100…219 × 135…161 | 0 \| 0 | 26,6 | 72,0 % |
| Tabellenkurve 0x445E0, vor dem 1. Spieltag | `oberflaeche_0842` | 2…107 × 40…73 | 0 \| 0 | 5,7 | 98,8 % |
| Tabellenkurve, Achsenlage | `folge06_00000`, `oberflaeche_0842` | Spalte x 5, Zeile y 70 | dunkelste Spalte/Zeile in beiden Bildern und im Remake: x 5 / y 70 | – | – |
| Tabellenkurve, Kurvenende und Platz bei 35 Spieltagen | `folge06_00000` | y 51 (Platz 3), y 69 (Platz 12) | Video: Ende x 74 voll, x 75 halb; Zahl ab x 76/77 · Remake: Ende 75, Zahl 76 | – | im [APPROX] ±1 px |

Die Textzeilen fallen nur durch die Unschärfe des Videos ab (Seitenvergleich vergrößert angesehen: gleiche Lage, gleiche
Glyphen). Im Bild `folge06_00000` ist das Video heller (andere Helligkeit des Mitschnitts), deshalb dort Lage statt Farbwert.
**M-K2 Kurzmeldung 0x0C0B8** und **Infofile 0x432EC/0x43596**: kein Referenzbild vorhanden (im Video nicht gezeigt);
Bildschirmfotos `05_kurzmeldung`, `03_/04_infofile_seite1/2` gerendert und angesehen (Kurzmeldung = Baustein M-K1 ohne Knöpfe;
Infofile zwei Seiten, Kopf, Linie y 9, „TASTE!“). Seitenzahl (2) und Dateigröße (1153 Byte) am Original geprüft.
Monatstabelle direkt in `em.exe` nachgelesen: Datei 0x1907C „Januar … Juli, August, Sept., Okt., Nov., Dez.“, kein „Aug.“.

### Gegenproben (gezielte Mutation in einer temporären Kopie, je Lauf mit Originaldaten)

Alle 25 rot (Test schlägt fehl), keine überlebt:

| Nr. | Mutation | roter Test |
|---|---|---|
| M01 | Symbolfeld x 83 → 84 | `Question_SymbolFields_AtOriginalPosition` |
| M02 | Rechtsklick wie Linksklick | `Question_OnlyLeftClickOnAButtonCloses`, `Message_AnyLeftClickCloses_ButNotRightClickOrKeys` |
| M03 | Grundlinie 141 → 142 | `Baselines_FollowTheOriginalTable` |
| M04 | Knopfregel `‖` → `&&` | `Buttons_OnlyWithLine3Or4_AndTexts` |
| M05 | „Auto-“ 45 → 75 | `ShortMessage_DurationFollowsMode` |
| M06 | Kurzmeldung schließt bei Klick | `ShortMessage_ClosesByItself_AndIgnoresInput` |
| M07 | „einlegen.“ entfernt | `ShortMessage_MissingSecondLine_ShowsEinlegen` |
| M08 / M11 | Linie unten 158 → 157 / oben 99 → 100 | `Dialog_LinesAndButtons_AtOriginalPosition` |
| M09 | Meldungsfarbe 17 → 14 | `Message_SymbolFields_UseColors16And17` |
| M10 | Knöpfe immer | `MessageWithoutButtons_DrawsNoButtonGraphics` |
| M12 | Schritt 1 → 2 entfernt | `Step_IsFloorOf85DividedByPlayedDays_OneBecomesTwo` |
| M13 | Linienanfang X + 3 → X + 2 | `Points_StartAtX6_AndEndAtPlayedTimesStep` |
| M14 | y 49 → 50 | `Y_TwoPixelsPerRank_FromRow47` |
| M15 | waagrechte Achse 96 → 97 | `DrawAxes_AtOriginalPosition_AlsoBeforeFirstMatchDay` |
| M16 | eigene Kurve weiß | `Curve_InMainMenu_UsesStepAndYellowForOwnClub` |
| M17 | Seitenende 230 → 235 | `Layout_27LinesPerPage_FirstBaseline19_LastBaseline227` |
| M18 | Tab-Grenze 285 → 290 | `Layout_Tab_JumpsToNextMultipleOf20_ButNotBeyond285` |
| M19 | „#“ ohne Wirkung | `Layout_HashEndsPage_AndLineWrapsAfter310` |
| M20 | Ö (0xD6) nicht umgesetzt | `Translate_Latin1Umlauts_MapToGameCharacters_OemUmlautsStay` |
| M21 | Rechtsklick öffnet Infofile | `Trigger_OnlyLeftClickOnPixel319_0` |
| M22 | Blättern um 2 Seiten | `ClickOnHiddenField_OpensInfoFile_PagesThenBack_WithoutChangingGame` |
| M23 | Kopflinie y 9 → 10 | `InfoFilePage_Draws_HeaderLine_AndPrompt` |
| M24 | „August“ → „Aug.“ | `SeasonCalendarTests.MonthTable_MatchesOriginal` |
| M25 | S-K5-Zweig in `SeasonEndScreen` entfernt | `SeasonUiTests.GameEnds_AfterChosenDuration` |

### Besonders geprüft

- **Dateigrenzen:** `Season.cs` nur `Months` (eingehalten). `MainMenuScreen.cs`: #426 innerhalb R11P-02; #421 außerhalb
  (R11U-01). Weitere fremde/gemeinsame Dateien: #421 `QuitQuestion.cs`, `Testing/ScreenDriver.cs`, zwei Testdateien
  (Anpassung an die API, im PR genannt); #430 `SeasonEndScreen.cs` (Spurbereich `SeasonEnd*`), `re-saisonwechsel.md`
  (nur Remake-Satz), `SaveLoadScreenTests.cs` (nur Kommentar #342). `DebCupDrawLive.cs`, `AdvertisingScreen.cs`,
  `TransferDialog.cs`, `TeamUi.cs` unverändert.
- **Zustandsreinheit:** Infofile öffnen/blättern/zurück per `StateFingerprint` unverändert (temporärer Test). Bundestrainer-
  Warnung: Fingerabdruck in `MainMenuBefundeP3Tests` vorhanden und grün. `InfoFileScreen` liest nur die Datei im Konstruktor.

### Erwartet offen – bestätigt

- Vereinsinfo 0x4D385: `RankCurve` wird nur aus `MainMenuScreen.cs` aufgerufen; `TeamInfoWindow.cs` unverändert, nicht angeschlossen.
- A157: `Months` jetzt `[ORIGINAL]` aus der Tabelle (in `em.exe` nachgelesen), ausdrücklich ohne Videobeleg (Saison zeigt keinen August).
- S-K5: Sprung zum Spielende `[ORIGINAL]` 0x5641A; ob das Titelträger-Fenster davor erscheint, `[APPROX]` ohne Videobeleg.
- Aufrufer in fremden Dateien als Liste im PR #421 (DebCupDrawLive, AdvertisingScreen, TransferDialog, `TeamUi.MessageBox`
  mit Nutzern), nicht umgestellt.

### Akzeptierte `[APPROX]`

Knopflage 94/164 statt 95/165 (Video 3:46 belegt −1 px); Takt 18,2 Hz / 60 Bilder je s; Überschrift der Bundestrainer-
Warnung = Name des aktiven Managers; Escape/rechte Taste in `QuitQuestion` als Komfort (A334 offen); Lage der Platzzahl ±1 px
(am Video bestätigt) und Abstand 7 px bei nahen Plätzen; Infofile-Schatten, rechte Maustaste blättert, ohne
Speicherprüfung; Titelträger-Fenster in der letzten Saison.

### Annahmen (nicht geprüft)

Disassembly-Fundstellen (0x4C246, 0x0C0B8, 0x445E0, 0x432EC, 0x5641A) nicht selbst nachdisassembliert; geprüft wurden
nur die Folgen am Bild, an `em.exe`-Daten (Monatstabelle) und am `infofile`.
