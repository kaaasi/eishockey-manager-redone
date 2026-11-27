# Review #904 – Spur teamwert-aufstellung (Erstreview)

Geprüft: Head `1fb880143d506e75f31f90d788ec0f6eca1dc5dd`, Base `f3490bdc7f7eb920dc585a74790d4232061e3162` (= `main`); Checkout
`git rev-parse HEAD` = Head, `git status --porcelain` leer; SDK 10.0.402. Reviewer: Claude Opus 5.5, eigene Sitzung (geplanter
Lauf), ohne Implementierungskontext (die Spur lief mit Claude Fable 5.2).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Die Lesart von em.exe stimmt am Binärcode. Alle drei Läufe von `tools/pruefen.sh` sind grün. Mit
`MatchEngine.Live.cs` und `Squad.Strength.cs` von der Base werden 14 von 15 neuen Fällen rot, wie im PR angegeben. Die fremden
Tests ändern nur Abdrücke, Saaten oder den Zeitpunkt einer Probe. Kein Sollwert aus einem Originalstand und kein Abgleich-Paar ist
neu eingetragen. Die Abweichung der Vereinsfelder bei FILE72/73 (P3-1) liegt im Import, nicht in dieser Spur. Es gibt nur P3.

## 2. Befunde

Keine P0–P2.

**Lesart (1) bestätigt** (`ndisasm -b16`, Segmenttabelle Datei 0x163C0):
- 0x977F, Datei 0x097CF–0x097F3: Vereinsfeld 0x16 ≠ 0 → bei `[bp+0xc] = 0` sofort Spieler (0x98F8), sonst `call 0x14E1:0x4D`
  (Liste) ≠ 0 → Spieler, sonst Felder 0x18/0x1A/0x1C + Teil (+4 − 4 · Reihe, Datei 0x097FD–0x0984F).
- 0x368B1 ruft `0x28:0x16BF` (= 0x977F) mit dem 4. Argument **1** (Datei 0x368D8). Die Liste wird also immer geprüft.
- 0x52B91 vergleicht DGROUP 0x233/0x234 mit dem Verein (5 Paare).
- Schreiber von 0x233: nur 0x36B99 und 0x392BF (beide 0xFF) sowie 0x5359B (Live-Bildschirm).
- 0x36D12/0x36D49 rufen `0x28:0x2A5` (= 0x8365) für Heim und Gast nach dem Leeren bei 0x36B8E–0x36BBF.
- 0x952C schreibt 0x18/0x1A/0x1C + Teil (Datei 0x09721–0x09771). Der Tageslauf ruft ihn bei Datei 0x54FBF (`push 1`, `0x28:0x146C`).

Die Gegner-Automatik folgt also dem Feldwert des Managervereins. Das prüft `A080_Preparation_OpponentComparesFieldStrengthOfManagerClub`.

**P3-1 – A080 FILE71/72/73: Die Felder 0x18–0x1D sind nur bei FILE71 nachgebildet. Ursache ist der Import, nicht diese Spur.**
Probe (temporärer Test, `HokCompare.Import` Saat 1, EC HEDOS MÜNCHEN, Reihenfolge Kond. A/S, Technik A/S, Form A/S):

| Stand | Original | Remake: Import + `UpdateManagerTeamValues` | Remake nach `PlayTo` |
|---|---|---|---|
| FILE71 | 68 70 69 69 55 48 | 68 70 69 69 55 48 ✓ | – |
| FILE72 | 69 70 69 70 55 48 | 68 70 69 69 55 48 | – |
| FILE73 | 77 70 75 69 60 48 | – | 69 69 69 69 55 48 |

Ursache bei FILE72: `5DC3` Satz 1 trägt den Tausch (Plätze 7/8 `01`→`02`/`02`→`01`, 20/22 `82`↔`81`). 0x376 + 2 · 1 bleibt aber
1 (`0375` = `1 1 0 1`). Der Import setzt deshalb `AutoLineupEnabled` und stellt neu auf, also ohne den Tausch:
LUTZ bleibt Abwehr Block 0 (`OriginalImport.Rest.cs:252`, #733). Im Original bleibt der Tausch bis FILE73 bestehen: LUTZ 2A,
`0375` = `1 1 0 0`. Außerdem ändert das Original die Felder schon von FILE71 auf FILE72, ohne zu spielen. 0x952C läuft also auch
beim Verlassen der Aufstellung (0x41506–0x4151A: `push 0, [0xA70E]`, `call 0x28:0x146C`). Im Remake ist das nur für den
Bundestrainer angeschlossen (`OnNationalLineupLeft`).

Folgen für den PR:
- Der Fall `FILE72→FILE73` des Regressionstests rechnet mit der Aufstellung vor dem Tausch. Er bleibt nur grün, weil die Zeile der
  Automatik bei |Δ| < 25 gleich ausfällt.
- Der Satz „an Tagen mit Ereignis … nicht bei jeder Umstellung“ ist nicht ganz richtig:
  [Squad.Strength.cs:88](https://github.com/kaaasi/eishockey-manager-redone/blob/1fb880143d506e75f31f90d788ec0f6eca1dc5dd/src/EishockeyManager.Core/Squad.Strength.cs#L88)
  und [TeamwertAufstellungTests.cs:139](https://github.com/kaaasi/eishockey-manager-redone/blob/1fb880143d506e75f31f90d788ec0f6eca1dc5dd/tests/EishockeyManager.Core.Tests/TeamwertAufstellungTests.cs#L139).
  Vor jedem Spiel schreibt der Tageslauf neu, deshalb wirkt das im Spiel nicht.

Empfehlung: ein Issue für die Leitstelle (Import 0x375/0x376 gegen den Tausch in FILE72, 0x952C beim Verlassen der
Vereinsaufstellung). Der Kommentar wird in `pruefung-karte` berichtigt.

**P3-2 – Die Übernahme von Einsatz und Angriff der Vorbereitung ist nur synthetisch gesichert.**
[MatchEngine.Live.cs:204–205](https://github.com/kaaasi/eishockey-manager-redone/blob/1fb880143d506e75f31f90d788ec0f6eca1dc5dd/src/EishockeyManager.Core/MatchEngine.Live.cs#L204-L205):
Mutation (beide `CopyPreparation`-Aufrufe auskommentiert) → von `TeamwertAufstellungTests` ist nur
`A080_Preparation_OpponentComparesFieldStrengthOfManagerClub` rot (14/15 grün). Die Originalpaare prüfen nur die Zeile im Vereinssatz
(`RecordAutoLine` am `Club`), nicht Einsatz und Angriff des `LiveTeam`. Das ist kein Fehler, aber eine Lücke. Zur Abhilfe genügt
eine Zeile im Paartest: `match.HomeTeam.Effort == r[0x26]`.

**P3-3 – Testname.** [SpielstaerkeManagerTests.cs:107](https://github.com/kaaasi/eishockey-manager-redone/blob/1fb880143d506e75f31f90d788ec0f6eca1dc5dd/tests/EishockeyManager.Core.Tests/SpielstaerkeManagerTests.cs#L107):
`LiveTeam_ManagedClub_OnlyDayForm` prüft jetzt Felder mit Abweichung und Form, nicht „nur Tagesform“. Nur kosmetisch.

**Erwartet offen, bestätigt:**
- Das veraltete `[APPROX]` in `MatchEngine.Preparation.cs:22–24` („das Remake nimmt in der Managerpaarung die Spieler“)
  widerspricht jetzt `PrepareFromClubFields`.
- Die Markerzeilen gehen an `pruefung-karte`.
- Pool-Beobachtung beim vierten Saisonwechsel: `AbgleichFixPoolTests` Saat 1 → 2; Saat 1 kippt mit altem und neuem Code am vierten
  Wechsel, im PR als Restlücke gemeldet.
- `zusehen` (#894) und `zufallsstrom` (#905) sind nicht auf `main`. Das `[APPROX]` „jedes Managerspiel ist Managerpaarung“
  ([MatchEngine.Live.cs:293](https://github.com/kaaasi/eishockey-manager-redone/blob/1fb880143d506e75f31f90d788ec0f6eca1dc5dd/src/EishockeyManager.Core/MatchEngine.Live.cs#L293))
  gehört zu A541. Die Integration prüft die Leitstelle.

**Fremde Tests (2), Rückschaltprobe:** `MatchEngine.Live.cs` und `Squad.Strength.cs` von f3490bd, neue Tests von Head.
- Rot (Abdrücke): `RngTwinProbeTests` (6), `SpielberechnungAnschlussTests` (2), `JugendTeamsTests` (2), `GameModeTests.Mode4…`,
  `DebCupDrawFlowTests`.
- Rot (Sollwert der Spur): `SpielstaerkeManagerTests.LiveTeam_ManagedClub_OnlyDayForm`, `WettbewerbeSpiel12Teil3Tests.ManagedClub…`.
- Rot (Saat): `PruefungKarteRunde17UiTests`.
- Grün (Probe hing an der Saat bzw. am Zeitpunkt): `WorldCupTests`, `AbgleichFixPoolTests`, `SpielberechnungKomplett3Tests`,
  `LivespielUi2Tests`.

Das deckt sich mit der Tabelle im PR. Jede Datei hat einen R21-D2-Kommentar mit Fundstelle. Kein Test auf Originalstände
(`Abgleich*Paare`, `HokCompare`-Werte) ist geändert.

**Dateigrenzen (3):**
- `src/` nur `MatchEngine.Live.cs`: Aufruf der Vorbereitung, `PrepareFromClubFields`/`CopyPreparation`, `LiveTeamOf` Felder,
  `ManagedClubValue`. Das sind A080/A544.
- `src/` sonst nur `Squad.Strength.cs` (A174).
- `GameState.DailyRun.cs` ist unverändert, der Schreiber war schon angeschlossen.
- Doku: nur der eigene Status in `docs/arbeitspakete.md`.

## 3. Ausgeführte Prüfungen

- `tools/pruefen.sh` (alle drei Läufe) auf dem Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3511 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2445 | 0 | 580 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2446 | 1065 | 0 |

- Rückschaltprobe Core (`dotnet test … --filter` auf die zehn betroffenen Klassen): 27 rot, 55 grün. Darunter
  `TeamwertAufstellungTests` 14/15 rot; nur `FILE39→FILE40` ist grün, wie im PR angegeben. Die Mehrsaisonprobe (passiv und aktiv)
  ist mit altem Code rot und mit neuem grün.
- Rückschaltprobe UI: `PruefungKarteRunde17UiTests` und `DebCupDrawFlowTests` sind rot, `LivespielUi2Tests` ist grün.
- Mutation P3-2: siehe oben. Probe P3-1: temporärer Test im verworfenen Worktree, nicht committet.
- Disassembly: `ndisasm -b16 -e <Datei> -o <Datei> em.exe` an 0x977F, 0x97CF, 0x368B1, 0x36B8E, 0x36D00, 0x39280, 0x52B91, 0x9721
  und 0x54FA0. Die Ziele der Fernaufrufe sind über die Segmenttabelle aufgelöst (Eintrag 5 → Basis 0x62C, also 0x977F, 0x8365 und
  0x952C). Alle zwölf Aufrufer von 0x952C sind per Bytesuche `9A 6C 14 28 00` gefunden.

**Akzeptierte `[APPROX]`:**
- `TeamStrength.PlayerValue` dient nur noch dem Auffüllen der Aufstellung.
- `TeamStrength.FieldValue` ist eine Zahl aus vier Feldern, nur für Vergleiche ohne 0x8365.
- `TeamStrength.MatchStrength` gehört zum alten Weg `MatchEngine.Play` und hat keinen Aufrufer im Spielablauf. Dort ist der Faktor
  2 entfallen, weil `Rating` jetzt im Maßstab der Felder rechnet.
- `LiveTeamOf`: Jedes Managerspiel gilt als Managerpaarung (A541).

**Annahme:** Ziel des Overlay-Stubs `0x14E1:0x4D` = 0x52B91, nicht über den Stub-Kopf nachgerechnet (Inhalt passt).
