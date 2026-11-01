# Review: Integration Runde 9 – optionen-wirkung (PR #335)

- **Geprüft:** Head `4f8cbc3f3e9a5746eb7716ba2ec85559202e7a9d`, nur Delta Base `6504333bd2185bbe1369936197d845ca27cc42e5`..Head
  (gestapelt auf #322/#320). Base ist Vorfahr des Heads.
- **Reviewer:** Claude Opus 5.5, geplanter Lauf, neue Sitzung ohne Implementierungskontext.
- **Checkout:** Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer; Originaldaten aus
  `kaaasi/eishockey-manager-referenz` (`ls-remote` erfolgreich). Ausführung am nachgewiesenen Checkout, nicht nur Codelesen.

## 1. Merge-Empfehlung

**Ja für genau diesen Head, sobald R9O-01 (P2, reine Testlücke) behoben oder vom Nutzer zurückgestellt ist.** Kein
Funktionsfehler gefunden: Das Gegenbeispiel (Infoleiste ≠ Gegner-Fenster) ist am Merge-Stand `05a2070` rot und am Head
grün; der zusammengelegte Schreiber verhält sich byteweise wie der alte `EnterHeadToHead`; Migration 18 → 19 und Abdruck
stimmen; Optionsbildschirm unverändert. Aber der eigentliche Anschluss der Ligaspiele ist durch keinen Test gesichert.

## 2. Befunde

### R9O-01 (P2) – Ligaspiel-Anschluss `RecordMeeting` ungetestet; Mehrsaisonprobe zählt Länderspiele mit

[`GameState.cs:109`](https://github.com/kaaasi/eishockey-manager-redone/blob/4f8cbc3f3e9a5746eb7716ba2ec85559202e7a9d/src/EishockeyManager.Core/GameState.cs#L109),
[`MeetingHistoryTests.cs:101`](https://github.com/kaaasi/eishockey-manager-redone/blob/4f8cbc3f3e9a5746eb7716ba2ec85559202e7a9d/tests/EishockeyManager.Core.Tests/MeetingHistoryTests.cs#L101)

- Gegenbeispiel: Zeile 109 (`RecordMeeting(home, away, hg, ag);`) entfernen → **alle 949 Core-Tests grün**. Die
  Infoleiste bliebe im echten Spiel dauerhaft leer, ohne dass ein Test es merkt.
- Ursache: `Meetings_SurviveFourSeasonChanges_AndSaveGame` zählt alle belegten Bytes der Tabelle von Manager 0. Seit der
  Zusammenlegung landen dort auch die Länderspiele Deutschlands (`EnterNationalResult` → Platz 0). Ohne Ligaanschluss
  stehen nach einer Saison 0 Vereins- und 5 Ländereinträge in der Tabelle → `counts[0] > 0` hält trotzdem. Die übrigen Tests
  rufen `RecordMeeting` direkt auf.
- Behebung (Vorschlag): In der Mehrsaisonprobe nur Gegner `< MeetingHistory.FirstNation` zählen und/oder einen Test über
  `TestSeason.PlayMatchDay` ergänzen, der `MeetingEntries(X)` und `HeadToHead(0, X)` gegen das Ergebnis in `Results`
  prüft (wie das Gegenbeispiel unten). Beide Varianten sind mit der Mutation rot (geprüft).

### R9O-02 (P3) – Bundestrainer-Zweige ohne Test

[`MeetingHistory.cs:149`](https://github.com/kaaasi/eishockey-manager-redone/blob/4f8cbc3f3e9a5746eb7716ba2ec85559202e7a9d/src/EishockeyManager.Core/MeetingHistory.cs#L149),
[`MeetingHistory.cs:165`](https://github.com/kaaasi/eishockey-manager-redone/blob/4f8cbc3f3e9a5746eb7716ba2ec85559202e7a9d/src/EishockeyManager.Core/MeetingHistory.cs#L165)

- Mutation `TableOwner`: Bundestrainer-Nation → `null` statt Platz 0: grün. Mutation `MeetingEntries`:
  `NationalCoachMenu` ignorieren (immer `ActiveManager`): grün (Filter MeetingHistory/GegnerFenster/GameOptions).
- Der erste Zweig ist derzeit unerreichbar (nur Ligaspiele rufen `RecordMeeting`, Länder spielen keine Liga); er wird erst
  mit dem Anschluss der WM-/Pokalspiele relevant. Der zweite ist bei mehreren Managern im Bundestrainermenü erreichbar.
  Test nachziehen, spätestens mit dem Anschluss der übrigen Wettbewerbe.

### Besonders geprüft (ohne Befund)

1. **Zusammenführung DGROUP 0xAB7B:** `MeetingHistory.RecordAt` ist für Länder und Vereine gleichwertig zum entfernten
   `TeamRecords.EnterHeadToHead` (Wert `(hi<<4)+lo & 0xFF` = `(byte)(lo + (hi<<4))`; Länder: Seite 0, dann 1, voll → gerade
   Plätze vor, Platz 1 → 8, ungerade vor, neu auf 9; Vereine je Seite, voll → vor). Mutation des Länder-Übertrags wird von
   `GegnerFensterTests.HeadToHead_NationListIsAFifoOfTen` und `MeetingHistoryTests.Record_NationalTeams_…` gefangen.
   **Durch den Wegfall von `Manager.MeetingTable` geht nichts verloren:** `GameSetup.MaxManagers` = 4 < `HeadToHeadSlots` = 6
   (kein Überlauf von `MeetingTable(i)`), Manager werden nach Spielbeginn nie entfernt (Platz = Managernummer bleibt
   stabil), `_headToHead` ist ab Schema 18 leer angelegt und wird mitgesichert. `Manager.MeetingTable` gab es nur auf dem
   Zwischenstand `05a2070` (nie auf `main`, `main` steht auf Schema 17). Kein Doppeleintrag: Ligaspiele schreiben nur über
   `RecordMeeting`, Länderspiele nur über `EnterNationalResult`.
2. **Migration 18 → 19 und Abdruck:** Fehlende `Setup.Options` → Anfangswerte von `new GameOptions()` (DEB-Pokal an,
   Rest aus, Geschwindigkeit 70), vorhandene bleiben; Renumber wie die übrigen Migrationen. Mutation `ShowDebCup = false`
   in der Migration → `Migration_FromSchema18_UsesOriginalInitialValues` rot. Abdruck `5F82E8AF4D0C593A` bei Schema 19 grün.
3. **Torszenen-Schalter (#321):** `GameSettings.GoalScenes` wurde schon am Base nur vom Einstellungsbildschirm gelesen,
   das Live-Spiel liest weder Alt- noch Neuwert (`LiveMode`-Anschluss erwartet offen). Alte Datei mit `"GoalScenes": false`
   lädt, Feld fällt beim Speichern weg (Test `GoalScenesDeviceSetting_IsGone_OldFieldIsDroppedOnSave`). Ablauf `optionen`
   gerendert: Bilder 48, 49, 50, 51a, 51 am Head **byteidentisch** zum Spurstand `b1a15eb`; Sichtvergleich mit
   `dosbox/nur-weltkugel/48` und `51` stimmt bis auf die bekannte `[APPROX]`-Zeile „RAM: …“ und den Level des Szenarios.

### Erwartet offen – bestätigt

- Minutenweg-Anschluss: `GameOptions.CupMatchUsesMinuteLoop` hat außerhalb von `GameOptions.cs` keinen Aufrufer; `LiveMode`
  liest nur der Optionsbildschirm.
- Begegnungen/Serien aus Pokalen und Play-offs: `RecordMeeting` nur in `PlayNextMatchDay` (`[APPROX]` im Code).
- Kurvenmaßstab 0x445E0: weiter `[APPROX]`, in `docs/arbeitspakete.md` als Restlücke geführt.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `tools/pruefen.sh` (alle drei Läufe) am Head | bestanden |
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1521 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1255 bestanden, 0 fehlgeschlagen, 237 übersprungen |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1255 bestanden, 266 fehlgeschlagen |
| SDK | 10.0.402 |

**Gegenbeispiel** (temporärer Test, nicht committet): neues Testspiel, `TestSeason.PlayMatchDay`, Ligaspiel des Managers
gegen X aus `Results`; Erwartung `HeadToHead(0, X)[Seite] = eigene + 16 · Gegentore` und `MeetingEntries(X)` = genau ein
Eintrag „H/A eigene:Gegentore“. `05a2070`: **rot** (Gegner-Fenster leer); Head `4f8cbc3`: **grün**.

**Mutationen** (temporäre Worktree-Kopie des Heads, Core-Tests):

| Mutation | Ergebnis |
|---|---|
| `MeetingTable` auf eigenes Feld statt `_headToHead` | rot (3 Tests, u. a. `MeetingTable_And_HeadToHead_AreOneTable`) |
| Migration `ShowDebCup = false` | rot (`Migration_FromSchema18_UsesOriginalInitialValues`) |
| Länder-Übertrag Platz 1 → 8 in `RecordAt` entfernt | rot (2 Tests) |
| `RecordMeeting` in `PlayNextMatchDay` entfernt | **grün → R9O-01** |
| `TableOwner`: Bundestrainer → `null` | **grün → R9O-02** |
| `MeetingEntries` ohne Bundestrainermenü | **grün → R9O-02** |

Bildschirmfotos: `emtool screens gamedata <ziel> optionen` an Head und `b1a15eb`, Dateivergleich mit `cmp`.

**Akzeptierte `[APPROX]`:** fehlende RAM-Zeile im Optionsbildschirm; Ergebnisse der laufenden Saison werden bei der
Migration nicht nachgetragen (Tabelle leer ab Schema 18); nur Ligaspiele in der Begegnungstabelle; Länder ohne Manager,
Kopf-an-Kopf nur für Deutschland (aus #322, nicht Teil dieses Deltas).

**Annahmen (nicht geprüft):** Disassembly-Fundstellen 0x4F5A8/0x50283/0x43E21/0x4E072 nicht erneut nachgelesen; die
Gleichwertigkeit des Schreibers ist am Code und an den Tests belegt, nicht an em.exe.
