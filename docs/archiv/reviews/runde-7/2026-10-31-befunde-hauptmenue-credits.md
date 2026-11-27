# Review: Befunde 31.10. – Hauptmenü beenden/Tag weiter, Bankauswahl, Credits, ohne Leertaste (#211)

- PR: #211 · Base `ae4d56bbfd54025d3b391a6832421241ea65b96a` · Head `4841f2545042356315231e27230abf1183b05d26`
- Umfang: #208 (`hauptmenue-bank`), #209 (`credits`), Leitstellen-Commit `4841f25` (Leertaste entfernt)
- Reviewer: Claude (frische Sitzung ohne Implementierungskontext, geplanter Lauf), 31.10.2026
- Checkout: Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer; `origin/main` = Base. Build und
  Tests am nachgewiesenen Checkout ausgeführt, Originaldaten aus `kaaasi/eishockey-manager-referenz`.

## 1. Merge-Empfehlung

**Ja, für genau diesen Head.** Alle drei Läufe grün, alle Gegenbeispiele reproduziert und durch Tests abgesichert
(Mutationen werden rot), die vier Fundstellen stimmen mit `em.exe` überein. Offen sind nur P3-Punkte (Testlücke bei
der Bankauswahl, Präzisierung eines Kommentars, eine nicht nachgebaute Klickzone), die einen Merge nicht blockieren.

## 2. Befunde

### HB-01 (P3) – Bankauswahl: Einschränkungen auf Logospalte und linke Taste ohne Test

[`FinanceScreen.cs` Z. 74](https://github.com/kaaasi/eishockey-manager-redone/blob/4841f2545042356315231e27230abf1183b05d26/src/EishockeyManager.UI/Screens/Manager/FinanceScreen.cs#L74),
[Z. 208](https://github.com/kaaasi/eishockey-manager-redone/blob/4841f2545042356315231e27230abf1183b05d26/src/EishockeyManager.UI/Screens/Manager/FinanceScreen.cs#L208).
Code ist richtig (Original 0x59B2D: nur `0x5A16 == 1`; 0x59BF2: `x < 47`), aber kein Test hält das fest:

- Mutation `StockSelectArea(i)` → `StockRow(i)` (Infofeld wählt die Aktie): alle Tests grün.
- Mutation `input.LeftClick && SelectAt` → `(input.LeftClick || input.RightClick) && SelectAt`: alle Tests grün.

Vorschlag: In `BoerseStartScreenTests`/`FinanceScreenTests` einen Klick auf das Infofeld (z. B. x 60) und einen
Rechtsklick auf die Logospalte ergänzen, die Auswahl und Ausschnitt nicht ändern dürfen.

### HB-02 (P3) – Kommentar zu DGROUP 0xA70E ungenau

[`QuitQuestion.cs` Z. 11](https://github.com/kaaasi/eishockey-manager-redone/blob/4841f2545042356315231e27230abf1183b05d26/src/EishockeyManager.UI/Screens/QuitQuestion.cs#L11):
„Überschrift = Name des Managers am Zug (DGROUP 0xA70E)“. 0xA70E ist ein **Byte** (0x45C61 `mov al,[0xa70e]` als
erstes Argument von 0x120:0x7A; 0x454C2 `cmp byte [0xa70e],5` für den Bundestrainer) – also der Index des Managers am
Zug, nicht der Name. Dass 0x120:0x7A daraus den Namen als Überschrift zeichnet, ist durch das Nutzerbild belegt, nicht
durch die Disassembly. Vorschlag: „Index des Managers am Zug (Byte DGROUP 0xA70E), Überschrift dessen Name (Nutzerbild)“.

### HB-03 (P3) – Klickzone Trainerkopf im Logo-Zweig nicht dokumentiert

Hauptmenüschleife 0x4549D–0x454B3: im selben y-Bereich (33 < y < 72) ruft ein Klick mit 232 < x < 272 (Kasten mit dem
Managerkopf) `0x120:0x48` mit Argument 0xFF auf, danach geht es mit der Logo-Prüfung weiter. Im Remake weder nachgebaut
noch in `MainMenuScreen.ContinueLogo` oder `docs/reverse-engineering.md` erwähnt. Wirkung von 0x120:0x48 nicht
geprüft. Vorschlag: als Restlücke in `docs/arbeitspakete.md` aufnehmen.

### Hinweis (kein Befund) – Leertaste als „beliebige Taste“

Die Entfernung betrifft wie verlangt Hauptmenü, Spieltag und Auslosungsfrage. Bildschirme, die auf *jede* Taste
reagieren (`input.Keys.Count > 0`: Titel, Saisonende, Meldungen in `TeamUi.cs` Z. 218, Werbung, Stadion …), schließen
weiterhin auch mit der Leertaste. Das entspricht „beliebige Taste“ und ist kein Widerspruch zur Entscheidung; der
Commit-Titel „überall entfernt“ ist nur so zu lesen.

## 3. Ausgeführte Prüfungen

### `tools/pruefen.sh` (alle drei Läufe, Head `4841f25`, SDK 10.0.402)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1071 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 873 | 0 | 179 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 873 | 198 | 0 |

Ergebnis: bestanden. CI auf dem Head: `build` success.

### Gegenbeispiele (mit Originaldaten)

| Gegenbeispiel | Nachweis | Ergebnis |
|---|---|---|
| Klick „und tschüß…“ → Sicherheitsfrage, kein Tag, Zustand unverändert | `MainMenuQuitTests.QuitTile_AsksInsteadOfPlayingDay`; Ablauf `screens … und-tschuess` (Bild angesehen: Überschrift Managername, drei Zeilen, „Leider ja“/„Kein Gedanke“) | ok |
| „Kein Gedanke“ zurück, Logo bei offener Frage wirkungslos | `QuitQuestion_KeinGedanke_ReturnsToMenuUnchanged` | ok |
| „Leider ja“ beendet | `QuitQuestion_LeiderJa_ExitsProgram` (Mutation Ja↔Nein → rot) | ok |
| Klick aufs Vereinslogo → nächster Tag, auch mit offener Unterkachel | `LogoClick_StartsDay`, `LogoClick_WorksWithSmallTilesOpen`, Ränder `ClickBesideLogo_DoesNothing` (Mutation Rect um 1 px → rot) | ok |
| Logo im Hot-Seat gibt den Zug weiter | `HotSeatUiTests` (jetzt über `NextDay`) | ok |
| Leertaste Hauptmenü / Spieltag / Auslosungsfrage ohne Wirkung | `Space_InMainMenu_DoesNothing`, `NoSpaceKeyTests.*`; je Mutation „Leertaste wieder an“ → genau der zugehörige Test rot | ok |
| Bank: Überfahren wählt nichts, Klick auf Logospalte setzt Ausschnitt | `Hover_KeepsOffset_ClickSetsItImmediately`, `BankAndPartner_SelectedOnlyByClick` (Mutation „Auswahl beim Überfahren“ → beide rot; Bankfeld verkleinert → rot) | ok (Lücke HB-01) |
| Klick auf Titel → Credits mit „Remake / Jörg Klinge“ | `CreditsScreenTests` (Mutation Titelhöhe 0x16 → rot); Ablauf `screens … credits` (Bild angesehen) | ok |

### Fundstellen gegen `em.exe` (ndisasm, DGROUP = Datei 0x16DD0)

- **0x45C33**: Zeiger DGROUP 0x12E5/0x12E9/0x12ED → „Sie haben also wirklich“, „etwas besseres zu tun ?“, „Sind Sie
  sicher ??“; 0x1319/0x131D → „Leider ja“, „Kein Gedanke“; Aufruf 0x120:0x7A, bei `ax ≠ 0` Freigaben und
  `0xC0:0x61(0x63)` (Programmende), sonst `retf`. Aufruf aus 0x45551–0x45559 bei Kachel 7, danach `jmp 0x535B`. Bestätigt
  (Präzisierung HB-02).
- **0x45483–0x4551E**: `si` = y, `[bp-4]` = x; 33 < y < 72 und 274 < x < 313 → Bundestrainer-Prüfung (0x454C2),
  `[0x176]`-Meldung (0x454F5), sonst `[0x5A16]` sichern und `jmp 0x592F` (weiter). `ContinueLogo` (275, 34, 38, 38)
  deckt genau 275–312 × 34–71. y < 0x15 → `0xF8:0x20` (Credits), x ohne Prüfung. Vorher 0x4544E: nur Ereignis 1 oder 3.
  Bestätigt (zusätzlich HB-03).
- **0x59B2D**: `cmp word [0x5A16],1`, sonst `jmp 0x59D42`. Mitmanager 117 + 71k < x < 174 + 71k, y < 54, Kasten belegt
  (`[0x553B+k] ≠ 0xFF`); Bank x < 106, y < 54; Aktie x < 47, y > 101, i = (y − 102) / 34; Versatz Kurs − 0x82
  (0x59C6B–0x59C79). Alle Klickfelder im Code stimmen.
- **0x399D0**: Funktionsbeginn (`push bp`), Puffer 216 × 153 (0xD8 × 0x99), Bild 0x0F (kb\15) laden, Hintergrund ab
  (0x33 | 0x3D) sichern, Bild 216 × 151 (0xD8 × 0x97) nach (51 | 61), Warten `0x38:0x118`. Bestätigt.

### Prüfziele nach Umstellung Leertaste → `Advance`/Enter

Alle früheren `Press(Key.Space)` in Tests und Abläufen sind durch `Advance()` (Hauptmenü: Logo-Klick, sonst Enter) bzw.
`NextDay()` ersetzt. Außerhalb von Hauptmenü, Spieltag und Auslosungsfrage hatte die Leertaste vor der Änderung keine
eigene Belegung (`git grep Key.Space ae4d56b -- src`); Enter wirkt in Spieltag (`skip`) und Frage („Aber klar“) wie
vorher die Leertaste, in Aufdeckphase und „beliebige Taste“-Bildschirmen ebenso. Damit prüfen die umgestellten Tests
dieselben Ziele; verloren ging keines. Die frühere Leertasten-Wirkung ist jetzt negativ abgesichert (`NoSpaceKeyTests`,
`Space_InMainMenu_DoesNothing`).

### Akzeptierte `[APPROX]`

- „?“-Felder der Sicherheitsfrage wie in den übrigen Fragen (Nutzerbild zeigt sie gelb, Remake grün) – erwartet offen, bestätigt.
- Escape und Rechtsklick = „Kein Gedanke“ (Tastenbelegung 0x120:0x7A ungeprüft) – erwartet offen, bestätigt.
- Tasten schließen die Credits nicht (Original wartet in 0x38:0x118 nur auf Mausereignis 1/3) – bestätigt.
- Logo nur mit linker Taste; Original nimmt auch Ereignis 3 – als `[APPROX]` markiert, akzeptiert.
- Fehlende Bundestrainer-Warnung und „Sie können nicht mehr weiterspielen.“ im Logo-Zweig – erwartet offen, bestätigt
  (im Code dokumentiert).

### Annahmen

- Ereignis 1/3 in DGROUP 0x5A16 als linke/rechte Taste gedeutet wie im Repo (nicht selbst nachgeprüft).
- Segmenttabellenzuordnung Overlay 0xF8 → Datei 0x399D0 aus `docs/reverse-engineering.md` übernommen; der Code an der
  Stelle passt zum beschriebenen Ablauf.
