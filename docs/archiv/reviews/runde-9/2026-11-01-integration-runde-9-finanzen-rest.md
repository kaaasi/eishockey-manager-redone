# Review: PR #320 – Integration Runde 9 (finanzen-rest)

- PR: #320 · Base `3bf25094a274ec160d241a228071f3558c0dd562` · Head `5729e695aa6f9fd7f7a0a9c0ffefc921727e1098`
- Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext), 01.11.2026
- Checkout: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer; Base = `merge-base` mit `origin/main`.
  Ausführung am nachgewiesenen Checkout (nicht nur Codelesen).

## 1. Merge-Empfehlung

**Nein für Head `5729e69`, solange F1 (P2) offen ist; ja, sobald F1 behoben oder vom Nutzer ausdrücklich
zurückgestellt ist** – sonst ohne Einwand. Build und alle drei Läufe grün,
Jugendteams (Prüfpunkt 1) und Bandenwerbung (Prüfpunkt 2) stimmen mit den nachgeprüften Fundstellen in `em.exe` überein,
alle acht Gegenproben werden rot. Der Dateidialog (Prüfpunkt 3) verändert beim Öffnen und beim Rechtsklick keinen
Spielzustand. Allerdings ist das Rechtsklick-Verhalten als `[ORIGINAL]` gekennzeichnet und weicht vom Original ab
(F1, P2). Es betrifft nur den Dialogzustand und lässt sich zurückbauen. Vor dem Merge F1 beheben oder vom Nutzer
ausdrücklich zurückstellen lassen (Merge-Prüfung §6 Nr. 5); kleinste Behebung: Markierung auf `[APPROX]` und
Doku-Text berichtigen (dann Nachprüfung nur dieses Deltas).

## 2. Befunde

### F1 (P2) – Rechtsklick auf einen Spielstand: Original zeigt die Spielstand-Info, übernimmt aber keinen Namen

[`SaveLoadScreen.cs` Z. 26 und 217–233](https://github.com/kaaasi/eishockey-manager-redone/blob/5729e695aa6f9fd7f7a0a9c0ffefc921727e1098/src/EishockeyManager.UI/Screens/SaveLoad/SaveLoadScreen.cs#L217-L233),
Test `RightClickOnEntry_TakesNameButDoesNotLeaveOrLoad`.

Belegt (ndisasm, Datei 0x5E5ED–0x5E667): Bei Taste 3 baut das Original den Pfad `Verzeichnis + "\" + Eintrag` in
einem **lokalen** Puffer `[bp-0x7c]`, ruft damit Datei 0x5B661 auf und setzt `[0x5A16] = 0`. Das Namensfeld
`[bp+6]:[bp+8]` wird in diesem Zweig nicht angefasst – nur der Linksklick-Zweig (0x5E66A ff.) vergleicht und kopiert mit
`0:0x1adb`/`0:0x1b0b` dorthin. 0x5B661 öffnet die Datei (`0x50:0x17`), liest den Kopf, prüft die Kennung (DGROUP
0x55DF) und zeigt sonst „Kein / Eishockey Manager / Spielstand. / !!!“ (DGROUP 0x1441–0x144D, `0x120:0x4d`); bei einem
gültigen Stand zeichnet er ein Infofeld (Text `0x30:0x885` bei x 0x48–0x105, y 0x51, Linie `0x30:0x995` (0x4D|0x53)–(0xFE|0x53),
weitere Zeilen y 0x5A/0x4C/0x85/0xD4; „Pfad: …“ über DGROUP 0x5618). Der Rechtsklick ist im Original also
„Spielstand ansehen“ (vgl. `OriginalSaveManagerHeader` in `Formats/OriginalSaveGame.cs`: Start-Liga, Name, Verein).

Gegenbeispiel: Laden öffnen (Feld leer), Rechtsklick auf `RECHTS`, Enter → Remake lädt `RECHTS`; im Original stünde
das Feld weiter leer (Enter lädt nichts), stattdessen wäre die Kopfinfo des Spielstands sichtbar.
Vorschlag: Rechtsklick auf einen Eintrag ohne Namensübernahme (Dialog bleibt offen) und das Infofeld als eigene
Teilaufgabe; bis dahin `[APPROX]` statt `[ORIGINAL]` und den Text in Z. 26 berichtigen. Test entsprechend umdrehen.

### F2 (P3) – Startliga der Jugendteams aus der Vereinsliga statt aus `ManagerSetup.StartLeague`

[`GameState.Manager.cs` Z. 56](https://github.com/kaaasi/eishockey-manager-redone/blob/5729e695aa6f9fd7f7a0a9c0ffefc921727e1098/src/EishockeyManager.Core/GameState.Manager.cs#L56).
Datei 0x29BBB–0x29BC1 schreibt bestätigt `[0x11A]` (Start-Liga) in das Ligabyte, 0x29BAA den Platz 5. Das Remake hat den
Wert als `ManagerSetup.StartLeague` (Vereinswahl in `NewGame.cs` Z. 100) und nimmt stattdessen die Liga des Vereins beim
**ersten Zugriff** auf `FinanceData` (lazy). Das ist als `[APPROX]` gekennzeichnet und zu Spielbeginn gleichwertig, solange
der erste Zugriff vor dem ersten Saisonwechsel liegt (in der Mehrsaisonprobe der Fall). Optional: Startliga aus
`Setup.Managers[…].StartLeague` lesen, dann entfällt das `[APPROX]`. Keine Merge-Bedingung.

### Ohne Befund geprüft

- **Jugendteams 0x3A24B (Prüfpunkt 1):** nachgeprüft per ndisasm: Klick nur bei `[0x5A16] == 1`; Zeile y > 167 + 24·Team
  und < +11 (0x3A65F–0x3A6B0); Schritt `500 · 2^Team` (0x3A6B7–0x3A6C9, LXMUL); Pfeil rechts x > 0xBE und Förderung < `10000 · 2^Team`
  (0x3A713–0x3A751), Pfeil links x < 0x79 und Förderung ≠ 0 (0x3A755–0x3A777); Balken 0x78 < x < 0xBF mit
  `⌊⌊(x−120)·2^Team·10000/70⌋/Schritt⌋·Schritt` (0x3A798–0x3A7F1) – deckt sich mit `YouthTeam.BarFunding`,
  `StepYouthFunding` und `YouthScreen.Update`. Startplatz 5 (= 6.) und Ligabyte = Start-Liga (0x29BAA/0x29BC1), Satz
  DGROUP 0xA078 + 0x15·Manager. Teamfarben DGROUP 0x4ED5 = 18, 15, 23 und Kästchenfarben 0x4EAE =
  {18,17,16}{15,13,12}{23,22,8} bestätigt. Buchung: `SettleYouth` bucht ausschließlich über `GameState.Book(…, Jugend, …)`,
  Mehrsaisonprobe `FourSeasonChanges_FundingIsBookedAndPlacesStayValid` (passiv/aktiv, 4 Saisonwechsel, ≥ 28
  Monatsbuchungen, Plätze 1–12/Liga 1–3, deterministisch) läuft grün und wird bei entfernter Buchung rot.
- **Bandenwerbung 0x2A9AD (Prüfpunkt 2):** Konstanten im Disassembly bestätigt (x 0x9C/0xBF/0x113, Grundlinien
  0xB3/0xBD/0xC5/0xCD, Text `0x30:0x885` mit Farbe 0 und Schatten 0xFF, Logo `0x30:0x16D`, leere Fläche `0x30:0x800`);
  Bandenzeile nur für Flächen < 7 (0x2ACBE); „Angebot“ (Modus ≠ 0, DGROUP 0x11A5) bzw. „Prämie“ (Modus 0, 0x11A9) bestätigt.
- **Dateidialog (Prüfpunkt 3):** Linksklick vergleicht das Feld mit dem Eintrag (`0:0x1adb`), bei Gleichheit Sprung
  nach 0x5E7A4 (Ende des Dialogs = Ausführen), sonst Kopie des Namens ins Feld (0x5E6A5) – wie im Remake. Rechtsklick
  außerhalb eines Eintrags bricht ab (0x5E79A). Öffnen und Rechtsklick ändern keinen Spielzustand (nur `_name`/`_selected`);
  Tippen setzt `_selected` zurück, ein erneuter Klick nach Änderung des Feldes führt also nicht ungewollt aus.
- Erwartet offen, bestätigt (in `docs/arbeitspakete.md`/`docs/finanzen-rekonstruktion.md` eingetragen): Wirkung der
  Förderung 0x39DEC/0x3A13B (`SettleYouth` `[APPROX]`), „Kündbar“ beim Aufstieg (0x2713F), Kursregler-Marke nach dem
  Loslassen, Dateidialog-`[APPROX]` (Speichern-Bild, ein Ordner statt Laufwerken).

## 3. Ausgeführte Prüfungen

```
git rev-parse HEAD                 # 5729e695aa6f9fd7f7a0a9c0ffefc921727e1098, status leer
git merge-base pr320 origin/main   # 3bf25094a274ec160d241a228071f3558c0dd562
tools/claude/setup-workspace.sh ~/referenz/original/gamedata && source ~/claude-tools/env.sh   # dotnet 10.0.402
tools/pruefen.sh                   # alle drei Läufe
ndisasm -b16 -e <Datei-Offset> gamedata/em.exe   # 0x29B90, 0x2A9AD, 0x3A630, 0x3A67F, 0x3A798, 0x5B661, 0x5E5C0, 0x5E68F, 0x5E790
```

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1450 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1204 | 0 | 222 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1204 | 246 | 0 |

Commit 5729e69, SDK 10.0.402, Ergebnis: bestanden.

**Gegenproben** (je eine Mutation, gezielter Testlauf, danach zurückgesetzt; Baum danach wieder sauber):

| Mutation | Test | Ergebnis |
|---|---|---|
| Förderschritt ohne `2^Team` | `FinanceYouth*` | rot (2) |
| Buchung „Jugend“ entfernt | `FourSeasonChanges…` | rot (1) |
| `StartRank = 12` | `FinanceYouthOriginalTests` | rot (2) |
| Pfeil rechts ohne Deckelprüfung | `YouthOriginal*` | rot (1) |
| Rechtsklick bricht immer ab | `SaveLoadScreenTests` | rot (1) |
| gleicher Name führt nicht aus | `SaveLoadScreenTests` | rot (1) |
| immer „Angebot“ statt „Prämie“ | `AdvertisingPanel*` | rot (1) |
| Klick auf Fläche setzt Blättern nicht zurück | `AdvertisingPanel*` | rot (1) |

Kein Pixelvergleich gegen Videobilder wiederholt (die NCC-Werte in `docs/finanzen-rekonstruktion.md` nicht nachgeprüft);
Kursregler (#196) nur gelesen, nicht gegen das Disassembly nachgemessen.

**Akzeptierte `[APPROX]`:** `SettleYouth` (Wirkung der Förderung), Rabatt der Talentsuche je 1.000 DM, Startliga aus der
Vereinsliga (F2), Durchsichtigkeit der Fahnen (Farbe 4), Abstand „n.Platz“–„m.Liga“, Pfeil-Trefferflächen der Werbung und
fehlende gedrückte Darstellung, Regler-Streifen bleibt stehen, Dateidialog ohne Laufwerke/Speichern-Bild.
**Nicht akzeptiert:** Rechtsklick-`[ORIGINAL]` im Dateidialog (F1).

**Annahmen:** Die Deutung von 0x5B661 als Infoanzeige des Spielstands stützt sich auf Dateizugriff, Kennungsprüfung,
Fehlermeldung und Textausgaben; die einzelnen Infozeilen sind nicht entschlüsselt. Für F1 reicht, dass das Namensfeld im
Rechtsklick-Zweig nicht geschrieben wird.
