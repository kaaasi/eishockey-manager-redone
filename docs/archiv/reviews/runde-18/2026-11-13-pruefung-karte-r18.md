# Review PR #813 – Spur pruefung-karte Teil 1 (Runde 18): #803, #805

Reviewer: Claude Code (Claude Opus 5.5), geplanter Lauf, eigene Sitzung ohne Implementierungskontext.
Geprüfter Stand: Base `5367ba84614e9c57aefc85f0cab348aaebc6a1fd`, Head `d88fa218e49fe5d1bcd47c076242209a6d2a4845`
(`git rev-parse HEAD` = Head, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Beide Formeln sind im Disassembly nachgelesen und stimmen (Hinweiszeile vorzeichenlos
+0x13 · 10 / 15; vierter Balken (100 − +0x13) · 94 / 100 als 32-Bit-Rechnung, Begrenzung 1…94). Der Börsentest fängt
r = 1/2/4 (selbst wiederholt), r = 3 bleibt grün. Der fremde Test ist nach R15-E2 = C korrekt angepasst (Rückschaltprobe
selbst wiederholt). Alle drei Läufe von `tools/pruefen.sh` grün. Offen nur P3 (Testlücken, Fundstellenschärfe).

## 2. Befunde

**P3-1 Zeichenweg des Erschöpfungsbalkens ungetestet.**
[`PlayerInfoPopup.cs` Z. 83](https://github.com/kaaasi/eishockey-manager-redone/blob/d88fa218e49fe5d1bcd47c076242209a6d2a4845/src/EishockeyManager.UI/Screens/Team/PlayerInfoPopup.cs#L83):
Probe – in `BarPixelLengths` den vierten Eintrag auf die alte Formel
`PlayerDataWindow.BarPixelsFromPercent(Math.Clamp(p.Strain - 50, 0, 100))` zurückgesetzt, dann
`EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.UI.Tests` → **714/714 grün**. Die neuen Tests prüfen nur den
Helfer `ExhaustionBarPixels`, nicht, dass `Draw` ihn verwendet. Vorschlag (Teil 2 oder Folge): Test über
`BarPixelLengths(p)[3]` oder Pixelprobe der Zeigerspalte im gerendeten Fenster.

**P3-2 Balkentest unterscheidet Divisor 100 nicht von 99.**
[`PruefungKarteRunde18Tests.cs` Z. 46](https://github.com/kaaasi/eishockey-manager-redone/blob/d88fa218e49fe5d1bcd47c076242209a6d2a4845/tests/EishockeyManager.UI.Tests/PruefungKarteRunde18Tests.cs#L46):
Mutation `(100 - p.Strain) * 94 / 99` (Z. 80 von `PlayerInfoPopup.cs`) → `InfoPopup_ExhaustionBar_FollowsOriginal`
**6/6 grün** (50→47, 60→37, 80→18, 98→1 gleich). Unterscheidender Fall z. B. Belastung 66: /100 → 31, /99 → 32.
Vorschlag: `[InlineData(66, 31)]` ergänzen.

**P3-3 Fundstelle „Aufruf 0xA5B“ ungenau.** `PlayerInfoPopup.cs` Z. 77 und Testkommentar Z. 40: Bei Versatz 0xA5B steht
`mov ax,di` (x-Lage + 0x6C, vierter Balken), der Aufruf der Balkenroutine 0xAC9 liegt bei **0xA63**
(`ndisasm -b16 -e $((0x41AC+0xA51)) -o 0xA51 em.exe`). Inhaltlich ohne Folgen; beim Nachziehen in Teil 2 korrigieren.

**Erwartet offen (bestätigt):** Inventurzeile A370 (`docs/analyse/inventur.md`) und `docs/reverse-engineering.md` Z. 525/554
(Belastung noch als `[APPROX]`) sind am Head unverändert → Teil 2 (`paket/pruefung-karte-2`), im Status der Spur vermerkt.

Keine P0–P2.

## 3. Ausgeführte Prüfungen

**Disassembly (`ndisasm -b16`, em.exe aus `original/gamedata`):**

- Hinweiszeile Datei 0x41B0A `cmp word [bp+0xe],0xe9` (nur Aufstellung), 0x41B29 `mov al,[es:bx+0x13]`, 0x41B2D `mov ah,0`
  (vorzeichenlos, 150 bleibt 150), `imul 10`, `idiv 15` → identisch mit `TrainingRules.ExhaustionDisplay` (Spalte E). ✔
- Spielerdaten 0x041AC + 0xA1C–0xA4C: langes 100 gepusht; 0xA26 Byte +0x13 vorzeichenlos, 32-Bit `100 − Wert`
  (`sub`/`sbb`), `· 0x5E` (0x3CA), `/ 100` (0x47C); Ergebnis als Längenparameter `[bp+0xe]` der Balkenroutine 0xAC9
  (x = di + 0x6C → vierter Balken „Erschöpfung“), dort 0xBBA–0xBC7 auf 1…0x5E begrenzt. Negative Werte (Belastung > 100)
  ergeben ≤ 0 → 1 px; Remake gleich (C#-Division schneidet wie `idiv` zur Null hin). ✔
- Börse 0x57854–0x5787A: `Basis · 3` (0x3CA) `/ 10000` (0x483), aufaddiert → r = 3 je 10.000 und Tag wie `FinanceStocks.cs` Z. 102. ✔
- Referenzbild `oberflaeche_1115` (HANFT, Transferliste): Zeiger am linken Rand. Mit der neuen Formel = Belastung ≥ 99;
  mit der alten wäre links = frisch. Belastung des Spielers unbekannt → Bild belegt die Formel nicht (wie im PR); Beleg ist
  das Disassembly.

**Mutationsproben (temporärer Worktree, je `dotnet test … --filter`):**

| Probe | Ergebnis |
|---|---|
| #803 r = 1 (`FinanceStocks.cs` Z. 102) | rot, Anteil 55 |
| #803 r = 2 | rot, Anteil 58 |
| #803 r = 4 | rot, Median 0,3297 (Anteil 66 liegt noch im Fenster, nur der Median fängt) |
| #803 r = 3 (Original) | grün |
| Rückschaltprobe `PlayerHintLine.cs` auf Base | `Hint_Trend_ListsExhaustionOnlyInLineup` rot (erwartet 53, ist 20); `Hint_Exhaustion_IsStrainTimesTenFifteenths` rot (z. B. 87 ↔ 54) |
| Balken /99 statt /100 | grün → P3-2 |
| `BarPixelLengths[3]` alte Formel | UI-Tests 714/714 grün → P3-1 |

Grenzen #803: aus dem Original belegt ist nur die Rate r = 3; die Fenster (60–66 %, 0,19–0,28) sind am Remake-Modell
gemessen (Saaten 1–1000, deterministisch) und trennen r = 3 nachweislich von 2 und 4 – als Testfenster akzeptiert.

**Fremder Test (R15-E2 = C):** `PlayerInfoWindowTests.Hint_Trend_ListsExhaustionOnlyInLineup` 20 → 53; Energy 70 =
Strain 150 − 70 = 80 (`Player.Attributes.cs` Z. 88/89), 80 · 10 / 15 = 53; Begründung mit Fundstelle 0x41B29 und
Rückschaltprobe im PR, selbst bestätigt. **Dateigrenzen:** Produktcode nur `PlayerHintLine.cs`/`PlayerInfoPopup.cs` (Spur
laut Plan), eigene Testdateien R17/R18, ein fremder Test wie oben, `docs/arbeitspakete.md` nur im eigenen Statusabschnitt.
Keine gemeinsamen Dateien. Weitere Aufrufer von `BarValues` (jetzt 3 Werte) gibt es nicht (`grep -rn BarValues src tests`).

**`tools/pruefen.sh` (alle drei Läufe, SDK 10.0.402, Commit d88fa21):**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3055 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2252 | 0 | 516 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2253 | 802 | 0 |

Ergebnis: bestanden.

**Akzeptierte `[APPROX]`:** keine neuen; zwei entfallen zugunsten `[ORIGINAL]` (Hinweiszeile, Balken). **Annahmen:** keine
über das Genannte hinaus; der Tendenzbalken (im PR als Beobachtung) ist nicht Gegenstand dieses Reviews.
