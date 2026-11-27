# Review: PR #197 – Nachtrag hok-format-2 (Delta `21c36a3..caa29a3`)

- PR: [#197](https://github.com/kaaasi/eishockey-manager-redone/pull/197) · geprüfter Head `caa29a3a942481216d3e01d949c4ffd84e83da6a`
- Umfang: nur Delta `21c36a3..caa29a3` (`8c2e077` FILE09, `caa29a3` Kaderspalten E/2m/5m/TO/Versicherung); `21c36a3` ist über #198 bereits in `main`.
- Reviewer: Claude (frische Sitzung ohne Implementierungskontext, geplanter Lauf), 31.10.2026.
- Checkout nachgewiesen: Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer; lokaler Merge mit `main` `3228411` → `d4faf62` (nicht gepusht).

## 1. Merge-Empfehlung

**Ja** für Head `caa29a3`. Der PR lässt sich konfliktfrei auf `main` (`3228411`) setzen; der Diff gegenüber `main` besteht
genau aus den vier Dateien des Deltas. Alle neuen Spalten sind in em.exe nachvollziehbar belegt und stimmen an FILE05 in
25 von 25 Zeilen mit dem Bild überein; FILE09 entspricht README und Bildern. `tools/pruefen.sh --schnell` auf dem
zusammengeführten Stand grün. Die Befunde sind P3 und blockieren nicht.

## 2. Befunde

- **P3 – Fundstelle/Offset der Versicherung in Doku und Code uneinheitlich.**
  [`spielstand-original.md` Z. 222](https://github.com/kaaasi/eishockey-manager-redone/blob/caa29a3a942481216d3e01d949c4ffd84e83da6a/docs/formate/spielstand-original.md#L222)
  nennt `+0x30` und em.exe `0x4239A`; der Code ([`OriginalSaveGame.cs` Z. 29](https://github.com/kaaasi/eishockey-manager-redone/blob/caa29a3a942481216d3e01d949c4ffd84e83da6a/src/EishockeyManager.Formats/OriginalSaveGame.cs#L29))
  richtig `+0x30/+0x32` und `0x42410`. Belegt: Datei `0x42410` = cs:`0x2430` `mov ax,[es:bx+0x30]; or ax,[es:bx+0x32]; jz` →
  Text DGROUP `0x5119` „(v)“; `0x4239A` = cs:`0x23BA` ist der Beginn der Spalte V.DAUER („kein V.“ bei `+0x0B` = 0, Text `0x510E`).
  Vorschlag: Doku-Zeile auf „`+0x30`–`+0x33` (4 Byte)“ und beide Fundstellen ergänzen.
- **P3 – Testlücken der Synthetik.** Der Test setzt nur `+0x03` (nicht `+0x04`/`+0x05`) und prüft `Insured` nur als `false`.
  Gegenbeispiel: Mutation `Goals = slot[0x03]` (ohne Summe) oder `Insured` nur aus `+0x30` (2 Byte) bliebe grün.
  Je ein Wert in `+0x04`, `+0x05` und `+0x32` würde das abdecken. (Die Mutationen 2m/5m vertauscht, E ohne `·10/15`,
  TO aus `+0x22`, `Insured` immer wahr werden rot – siehe 3.)
- **P3 – `IsGoalkeeper` ohne Markierung.** [`OriginalSaveGame.cs` Z. 38](https://github.com/kaaasi/eishockey-manager-redone/blob/caa29a3a942481216d3e01d949c4ffd84e83da6a/src/EishockeyManager.Formats/OriginalSaveGame.cs#L38)
  `RoleValue == 0`: Das Original entscheidet „-“ in TO über einen Far-Call mit Poolindex `+0x0F` (cs:`0x21F6`–`0x2206`);
  die Funktion selbst wurde nicht nachgelesen. Ergebnis stimmt an FILE05 (HEISS, CISSEWSKI „-“) und FILE09 (je 2 TW).
  `[APPROX]` oder Fundstelle ergänzen (Regel 2.7).
- Kosmetik: In der Kaderplatz-Tabelle stehen `+0x00`/`+0x01` hinter `+0x1C` (Reihenfolge nicht nach Offset).

## 3. Ausgeführte Prüfungen

**em.exe** (`~/referenz/original/gamedata/em.exe`, `ndisasm -b16`, Segment ab Datei `0x3FFE0`):

| Spalte | Sprungtabelle cs:`0x258E` | Befund |
|---|---|---|
| 12 TO | `0x21F6` (Datei `0x421D6`) | Prüfung Poolindex `+0x0F`, sonst `+0x03 + +0x04 + +0x05` ✔ |
| 13 2m 5m | `0x223D` (Datei `0x4221D`) | erst `+0x01`, Trenner `0x5103`, dann `+0x00` ✔ |
| 15 E | `0x2355` (Datei `0x42335`) | `cmp [+0x13],0x82` → Farbe 0x10; `+0x13 · 10 / 15` ✔ |
| 16 V.DAUER | `0x23BA` (Datei `0x4239A`) | `+0x0B` = 0 → „kein V.“; „(v)“ bei `+0x30|+0x32` ≠ 0 (`0x2430`) ✔ |

Spalten 0–11, 14, 17 der Tabelle nur auf Vorhandensein gesehen, nicht im Einzelnen nachgelesen.

**Gegenbeispiele** (`emtool hok dump` am Head, Spielstände aus `original/spielstaende/` des Referenz-Repos):

- FILE05–FILE09: beide Prüfsummen gültig.
- FILE05 gegen `file05_2_aufstellung.png`: TO, 2m, 5m, E in allen 25 Zeilen gleich (z. B. KÜHN 0/2/1/70, SIKORA 1/0/1/33,
  LIEBSCH 2/0/0/80, Torhüter „-“). Kein „(v)“ in FILE05–09 (wie Doku: kein Fall).
- FILE07: DAFFNER SP 2, Vertrag 1 J., Preis 400.000 DM (unverändert gegenüber geprüftem Stand).
- FILE09: zwei Kopf-Manager „Spieler 1“/SC PREUSSEN BERLIN und „Spieler 2“/EC HEDOS MÜNCHEN, Level 5, Dauer 0,
  Start-Stärke 2, Spielmodus 4, Manager am Zug 1, Spielende 1994, Konto je 400.000 DM (Bild `file09_3`: 400.000 DM),
  zwei Kader à 25. Stimmt mit README und Bildern. Trainingsplan „alle = 1“ nicht eigenständig nachgerechnet.

**Gegenprobe Test** (`OriginalSaveGameTests`, temporäre Mutation, danach zurückgesetzt): 2m/5m vertauscht → rot;
E ohne Skalierung → rot; TO aus `+0x22` → rot; `Insured` immer wahr → rot.

**Regeln:** Delta ändert nur `.cs`/`.md` (keine Originaldateien, keine Bilder); neuer Test rein synthetisch.

**`tools/pruefen.sh --schnell`** auf lokalem Merge `d4faf62` (= `main` `3228411` + Head), SDK 10.0.402:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1077 | 0 | 0 |

**Erwartet offen – bestätigt:** übrige offene Felder in `spielstand-original.md` (Kaderplatz `+0x02`, `+0x09`,
`+0x0C`–`+0x0E`, `+0x14`, `+0x16`–`+0x1B`, `+0x20`, `+0x21`, `+0x23`–`+0x27`; Bedeutung `+0x22`; Zuordnung der
Wettbewerbe zu `+0x06`–`+0x08`/`+0x03`–`+0x05`; Vereinsdatensatz, Kaufregel, Saisonwechsel, DEB-Pokal, Ablauf nach
Mogelguru) sind als offen geführt, keine falsche Originaltreue behauptet.

**Annahmen (nicht geprüft):** Torhüter-Gegentore in `+0x03`–`+0x05` (in Doku als „vermutlich“ markiert);
`+0x22` = `+0x03` in allen Ständen nicht nachgerechnet.
