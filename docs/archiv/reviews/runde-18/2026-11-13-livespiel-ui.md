# Review PR #811 – Spur `livespiel-ui` (Runde 18, B1 und B4)

Geprüfter Head: `22396dcaad498e9f97cc757b7f68ec5f0619b475` · Base: `5367ba84614e9c57aefc85f0cab348aaebc6a1fd` · Reviewer: Claude Opus 5.5
(geplanter Lauf, frische Sitzung ohne Implementierungskontext). Checkout nachgewiesen: `git rev-parse HEAD` = Head,
`git status --porcelain` leer.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head – Merge laut Plan erst nach `livespiel-kern` (noch nicht auf `main`). Alle fünf Bausteine und
„Spiele laufen“ stimmen mit der Disassembly überein (Argumentreihenfolge, eingeschlossene Endpunkte, Byte-Arithmetik, Farben
aus DGROUP), der Pixelvergleich mit `karriere_02315` bestätigt Versatz (0 | 0) und die Korrektur der Abnahme (Minute 08).
Keine Kern-Änderung, Dateigrenzen eingehalten, `DrawRunning` zeichnet nur. Befunde nur P3.

Erwartet offen (bestätigt): B2, B3 (Welle 2, Spur `livespiel-ui-2`) – keine Live-Anzeige im Diff, `LiveWidgets` nur von
`MatchDayScreen` benutzt.

## 2. Befunde

**P3-1 `[APPROX]` „0x50:0xBD3 lässt Farbe 0 durch“ ist per em.exe belegbar** –
[LiveWidgets.cs Z. 117–121](https://github.com/kaaasi/eishockey-manager-redone/blob/22396dcaad498e9f97cc757b7f68ec5f0619b475/src/EishockeyManager.UI/LiveWidgets.cs#L117-L121).
Beleg (`ndisasm -b16`, Datei-Adressen, Header 0x1E00): 0x50:0xBD3 = Datei 0x13B23; sind weder Quelle noch Ziel 0xA000:0
(Arbeitszelle kb\38 → Arbeitszelle), springt sie nach 0x13C0B und ruft `0xBBF:0x3893` = Datei 0x11283. Dort Zweig
Speicher → Speicher (0x112B4–0x112C4 nicht 0xA000) mit der Pixelschleife
```
0001134C  mov al,[si] / or al,al / jz 0x1355 / mov [es:di],al
```
→ Farbe 0 wird nicht geschrieben, die Zelle bleibt stehen. Damit ist der zweite Satz des `[APPROX]` ein `[ORIGINAL]`
(Fundstelle 0x1134C); der Video-Schluss (Inneres der „0“ ≈ Farbe 3: Video ≈ (87–94, 116–123, 123–130) vs. Remake (97,130,146), nicht schwarz)
passt dazu. Der erste Satz (Stil 0 aus dem Ziel statt aus Hintergrundpuffer 0xC12B) bleibt `[APPROX]` und ist akzeptiert.
Vorschlag: bei Gelegenheit (z. B. in `livespiel-ui-2`) Kommentar umstellen; kein Merge-Hindernis.

**P3-2 Füllfarbe der Ziffernzelle ungeschützt** –
[SpieleLaufenOriginalTests.cs Z. 63–68](https://github.com/kaaasi/eishockey-manager-redone/blob/22396dcaad498e9f97cc757b7f68ec5f0619b475/tests/EishockeyManager.UI.Tests/SpieleLaufenOriginalTests.cs#L63-L68).
Der Test erwartet `LiveWidgets.DigitCellFill` statt der Zahl 3; Mutation `DigitCellFill = 3 → 2` bleibt grün (Gegenprobe M2,
19/19 bestanden). Vorschlag: im Erwartungswert `(byte)3` mit Fundstelle 0x53245 einsetzen.

**P3-3 Abnahmezeile der Spur** – [arbeitspakete.md Z. 86](https://github.com/kaaasi/eishockey-manager-redone/blob/22396dcaad498e9f97cc757b7f68ec5f0619b475/docs/arbeitspakete.md#L86)
nennt weiter „zwei volle Balken, dritter 32 px“; die Korrektur steht nur im Status und in `re-livespiel.md` §13. Die Korrektur
selbst ist richtig (siehe 3.). Die Leitstelle sollte die Abnahmezeile beim Integrieren anpassen (Datei der Leitstelle).

**Geprüft ohne Befund:**
- 0x51E90: Balken `0x533D0(9, 0xBE+2+8k, 0x4F, (sbyte)Rest·4)`, Rest −= 20 mit `jnl` → 0; Ziffern `idiv 10` bei x 0x60/0x6A,
  y 0xC4, Stil = [bp+6]. 0x533D0: Wert ≤ max, Rechteck 0x11 (x|y)–(x+max|y+1) nur bei max > Wert, Linien (x|y+k)–(x+Wert|y+k)
  aus DGROUP 0x5427. 0x53307: Linien (x+2+k) von y+max bis y+max−Wert−1 (0x5425), danach 0x12 von y bis y+max−Wert.
  DGROUP 0x5425–0x5428 (Datei 0x1C1F5) = `18 13 18 13`. 0x53453: an kb\38 (0x4C|0xA) 4×4 → (x+1|y+2), Maske 0xC0; aus 0x10
  (x|y+1)–(x+4|y+5). 0x53234: Stil ≠ 0 Zelle Farbe 3, Endkopie Maske 0xC0 – im Remake gleichwertig, weil kb\38 im
  Ziffernbereich nur die Indizes 0, 1, 30, 254 enthält (Probe-Test, kein 0xC0).
- 0x37B82–0x37C5C: Text 0x30:0x885(0, 0x13F, 0x50, 0x1E, Schatten 0); Rahmen 0x30:0x787 (3|183)–(123|218) Farbe 0; Füllung
  0x30:0x800 (4|184)–(122|217) Farbe 3; Uhr 0x130:0x20 mit Stil 1, wenn [bp+0x10] = 0 (nur bei [0x5A55] = 0). `ProgressBox`
  = (3, 183, 121, 36) passt.
- B4: 0x51F3D-Texte (Datei 0x51F91/0x51FB8) unverändert im bestehenden `DrawGameEnd`; Szenario `live-spielende` liefert
  schwarzes Bild mit „Das wars...“/„Carpe diem !“. Setzt `Level = 1` nur im Werkzeug-Szenario.
- Dateigrenzen: geändert nur `UI/LiveWidgets.cs` (neu), `Screens/MatchDayScreen.cs`, `Tools/Scenarios/Live.cs` (neu), zwei
  Testdateien, eigener Abschnitt `re-livespiel.md` §13 und eigener Status. Kein `src/EishockeyManager.Core`, keine gemeinsame
  Datei. `DrawRunning` ist statisch und reines Zeichnen.

## 3. Ausgeführte Prüfungen

- Einrichtung: `tools/claude/fetch-reference.sh original karriere`, `tools/claude/setup-workspace.sh`, `dotnet --version` =
  10.0.402.
- `tools/pruefen.sh` (alle drei Läufe, Erstreview):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3063 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2254 | 0 | 522 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2255 | 808 | 0 |

  Commit 22396dc, SDK 10.0.402, Ergebnis: bestanden.
- Pixelvergleich selbst wiederholt: `emtool screens gamedata <ziel> live-bausteine`, Bild `01_spiele_laufen_minute_08` gegen
  `karriere_02315` (RGB, mittlere absolute Abweichung, Versatz −2…2): Text (95,55)–(225,85) 12,9 bei (0|0), nächster 24,4;
  Kasten 12,3 bei (0|0), nächster 15,8; Ziffern 10,6 bei (0|0), nächster 18,0; Balken 15,3 bei (0|0) und (1|0) gleichauf
  (waagerechte Linien, waagerecht unempfindlich); ganzes Bild 8,6. Video Zeile 192: gelb bis x 41, ab x 42 rot; Zeilen 200 und
  208 durchgehend rot → **Minute 08**, Korrektur der Abnahme bestätigt.
- Gegenproben (Mutation im Arbeitsbaum, danach `git checkout -- src`; Filter `SpieleLaufenOriginalTests|LiveWidgetsTests`,
  `EM_REQUIRE_GAMEDATA=1`):

| Mutation | Ergebnis |
|---|---|
| M1 `BarColors` vertauscht | rot (8) |
| M2 `DigitCellFill` 3 → 2 | **grün** (P3-2) |
| M3 senkrechter Balken ohne −1 | rot (2) |
| M4 Uhr Rest − 19 statt − 20 | rot (1) |
| M5 Schatten 0 → 53 | rot (1) |
| M6 `LampOff` 0x10 → 0x11 | rot (1) |

**Akzeptierte `[APPROX]`:** 0x53234 Stil 0 aus dem Ziel statt Hintergrundpuffer (Bildschirme zeichnen den Grund je Bild neu);
Ablauf „Spiele laufen“ (Bilder je Minute, Vorspultaste) bis A2 in `livespiel-kern`.
**Annahmen (nicht geprüft):** Gleichwertigkeit von `CompetitionWidgets.Centered` mit 0x0B6D9 (aus früherer Runde übernommen);
kein Videobild des Spielendes.
