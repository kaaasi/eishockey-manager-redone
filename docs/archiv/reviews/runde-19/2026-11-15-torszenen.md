# Review PR #856 – Spur `torszenen` (Runde 19, Spur C, L7)

Erstreview, unabhängige Sitzung (keine Mitarbeit am PR), unbeaufsichtigter Lauf. Base `6b64baa0d0ba17fd1a75de034ed64120a9ff6dee`,
Head `e09ed1726bc4e16a80867924536a21d75bcfb9d1` (Checkout nachgewiesen: `git rev-parse HEAD` = Head, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Alle drei Läufe von `tools/pruefen.sh` grün, die Gegenbeispiele C2/C3 und das Szenario
`live-torszene` lassen sich nachstellen; der Bildschirm ändert keinen Spielzustand (Spielstand einschließlich Zufallsquellen
gleich dem Lauf ohne Anzeige, auch mit Darstellung 2 und „w“). Die Befunde sind P3 (zwei Testlücken, ein nicht umgesetzter
Sonderfall des Dauer-Eisfelds, ein Szenariobild einen Takt zu früh) und blockieren nicht.

## 2. Befunde

**P3-1 Testlücke Tausenderpunkt der Zuschauerzeile.** [GoalSceneCall.cs:80](https://github.com/kaaasi/eishockey-manager-redone/blob/e09ed1726bc4e16a80867924536a21d75bcfb9d1/src/EishockeyManager.UI/Screens/Live/GoalSceneCall.cs#L80)
Mutation `Number(spectators, 1, false)` → `true` bleibt grün (`dotnet test tests/EishockeyManager.UI.Tests --filter
FullyQualifiedName~GoalScene`: 18/18 bestanden). `Spectators_AsInOriginal` prüft nur „0 ZUSCHAUER“ und zwei Breiten. Das Verhalten
selbst stimmt (Probe Heimspiel Tag 2: „1161 ZUSCHAUER“, Stufe 1); vorgeschlagen: Zuschauerzeile eines vorbereiteten Heimspiels
mit Zahl ≥ 1000 prüfen.

**P3-2 Testlücke Spielstand innerhalb einer Minute.** [LiveMatchScreen.cs:306](https://github.com/kaaasi/eishockey-manager-redone/blob/e09ed1726bc4e16a80867924536a21d75bcfb9d1/src/EishockeyManager.UI/Screens/Live/LiveMatchScreen.cs#L306)
Mutation `score = …` → `score = score;` bleibt grün (`--filter FullyQualifiedName~GoalSceneCall`: 6/6), weil am ersten Spieltag
keine zwei Chancen in derselben Minute nach einem Tor liegen. Das Verhalten stimmt (Probe Tag 2, Minute 16: Tor Heim bei 2:1,
danach Chance Gast mit Kopfzeile 3:1). Vorschlag: Kopfzeilenprobe über den zweiten Spieltag oder einen synthetischen Fall.

**P3-3 Hallenstufe 2 bei mehreren Managern im Dauer-Eisfeld nicht umgesetzt.** [GoalSceneRink.cs:63](https://github.com/kaaasi/eishockey-manager-redone/blob/e09ed1726bc4e16a80867924536a21d75bcfb9d1/src/EishockeyManager.Formats/GoalSceneRink.cs#L63)
nennt die Regel (Datei 0x568A9–0x568B6: `cmp byte [0x386],1; jna; cmp byte [bp+0x20],0; jz; mov byte [bp+0x22],2`), aber
[GoalSceneCall.cs:99](https://github.com/kaaasi/eishockey-manager-redone/blob/e09ed1726bc4e16a80867924536a21d75bcfb9d1/src/EishockeyManager.UI/Screens/Live/GoalSceneCall.cs#L99)
übergibt bei Darstellung 2 immer den Füllgrad; Codesuche `rg "0x386" src` findet keinen Anschluss in Torszene/Abspieler.
Disassembly: Modus 2 springt bei Datei 0x5680A am Laden vorbei, die Regel greift im Original also beim Aufbau in Modus 1 aus dem
Spieltagsablauf – fällt damit unter das akzeptierte `[APPROX]` „Dauer-Eisfeld nicht nachgebaut“. Vorschlag: im `[APPROX]`
von `GoalSceneCall` mit nennen statt nur im Kommentar von `MapGraphic`.

**P3-4 Szenariobild `02_szene_ende` einen Takt zu früh.** [Live.cs:113–115](https://github.com/kaaasi/eishockey-manager-redone/blob/e09ed1726bc4e16a80867924536a21d75bcfb9d1/src/EishockeyManager.Tools/Scenarios/Live.cs#L113-L115)
speichert bei `Animation.Finished`; die Torzahl steigt erst im nächsten Takt (`GoalSceneScreen.Tick`). Ausgeführt
(`dotnet run --project src/EishockeyManager.Tools -- screens gamedata <scratch> live-torszene`): Tor Heim in Minute 4, Bild
`02_szene_ende` zeigt noch 0 : 0, entgegen dem Szenariokommentar „Kopfzeile nach dem Tor“. Abspieler selbst richtig
(`Scene7at_SoundsAndEnd`: 0 : 1 am Ende). Vorschlag: bis `Header` geändert bzw. bis Pause 6 läuft weiterbilden.

## 3. Ausgeführte Prüfungen

**Gegenbeispiele**

- **C2** (Szene `7.at`, 33 Takte, Eisbären Berlin – EC Ratingen, eigene Probe außerhalb der Tests, Vergleich mit
  `eisbaeren_f01_01150`): Eis Zeilen 0–199 mittlere Abweichung 15,0 je Pixel (Median 4, 92,4 % < 40), beste Verschiebung (0 | 0),
  für Stufe 0, 2, 4 gleich; Kopfzeile: 928 von 934 hellen Videopixeln ab x 40 vom Remake getroffen, „ZUSCHAUER“ beginnt mit
  „4335 “ an derselben Stelle wie im Video; Sichtprüfung Seite an Seite unauffällig. Entzerrung Summe < 6 → XOR 0xFFF: Mutation
  `< 6` → `<= 6` rot (1 Test). Ton je Bild = Byte 3: `Scene7at_SoundsAndEnd` grün; Modus 2 ohne Pause 6: Mutation (Pause auch
  in Modus 2) rot.
- **C3** Spieltag mit Managerpaarung: `GoalSceneCallTests` grün (Darstellung 1 und 2 gleich `Continue()` ohne Anzeige, Save
  einschließlich Zufallsquellen). Zusätzliche Proben: zweiter Spieltag (Heimspiel) mit Darstellung 1 gleich Referenz; „w“ statt
  „r“ mit Darstellung 2, einmal und dreifach, gleich Referenz, je Chance genau 1 bzw. 3 Abläufe derselben Szene (R19-E1 = A).
  Gegenprobe Spielzustand: zusätzlicher Zug `game.Random.Next()` in `GoalSceneCall.Request` → 3 Tests rot. Höchstzahl:
  `plays >= 3` → `> 3` rot.
- **Szenario `live-torszene`** läuft durch (5 Bilder: Szene, Ende, Pause 20 mit Live-Bildschirm, Wiederholung, Reporter „TOR“).

**Besonders geprüft**

- **Kein Spielzustand durch den Bildschirm:** `GoalSceneScreen` liest nur den Auftrag, Ton über `GameAudio.Current`;
  `GoalSceneCall` liest `HomeGate`/`Finance` (für aktive Manager schon angelegt); Saves gleich (s. o.).
- **Figurtafel/Eisfläche C1** gegen em.exe: Datei 0x128BF (`48h:4ECFh`) x − 0x50, y − 0x60, Größe 5 bzw. 0x20; Datei
  0x129E7–0x12A50: < 2 eingebaut (`cs:0x54`/`cs:0x6D`, Zeile 5), ≤ 0x29 `cs:0x44` − 2, ≤ 0x51 `cs:0x48` − 0x2A, ≤ 0x5D `cs:0x4C`
  − 0x52, Index · 0x400 – deckt sich mit `GoalSceneFigures.Resolve`. Das Original vergleicht vorzeichenbehaftet (Figur ≥ 0x80 →
  eingebaute Figur 1) und überspringt Objekte bei (0 | 0); beides ohne Wirkung: Daten nur 0 … 93 (`AllSceneFigures_HaveImage`),
  (0 | 0) liegt außerhalb jedes Sichtfensters.
- **„Pause 6“** Datei 0x56E14–0x56E29: `mov al,[bp+0x20]; or ax,ax; jnz` um `50h:19Eh(6)` herum → nur Modus 0. Aufgelöst.
- **Fremder Test R15-E2:** Rückschaltprobe (Eintrag `GoalSceneScreen` aus `NotConstructible` entfernt) → 3 Fälle rot, bestätigt.
- **Verweis UI → Audio:** `EishockeyManager.Audio` verweist nur auf `Formats`, kein Zyklus; Ausgabe über die vorhandene
  Schnittstelle `IGameAudio`, Tests ohne Gerät grün.

**`tools/pruefen.sh` (alle drei Läufe, Head e09ed17, SDK 10.0.402)**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3295 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2412 | 0 | 564 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2413 | 882 | 0 |

**Akzeptierte `[APPROX]`** (bestätigt, wie erwartet offen): Wiederholung ohne neuen Zug (R19-E1 = A); Speichermangel-Zweig
entfällt; Dauer-Eisfeld zwischen den Szenen nicht nachgebaut (einschließlich P3-3); Rücksprung bei weniger als 4 fitten Spielern
(`120h:66h`) nicht nachgebaut; Bildtakt 18,2 Hz auf 60 Hz verteilt. Außerdem: „Ton an“ als Stummschaltung; Taste = erstes
Zeichen während Szene/Pause 20 (im Original auch früher getippte Zeichen aus dem Tastaturpuffer).

**Annahmen (nicht geprüft):** Hallenstufen-Karten `kb\242–245` gegen Video (im Ausschnitt ohne Ränge nicht unterscheidbar);
Modus 1/3 aus dem Spieltagsablauf (nicht Teil des PR).
