# Review: Integration Runde 4, Teil 2 (PR #112)

PR [#112](https://github.com/kaaasi/eishockey-manager-redone/pull/112) · Base `caf970020c8e9830fc1690aefb7e929711b1c15e` ·
Head `c3e04154cecadcc32e24bb62f0c6f258da97101a` · Reviewer: Claude (Opus 5.5), neue Sitzung ohne Beteiligung an #111/#112.

## 1. Merge-Empfehlung

**Ja für `c3e04154cecadcc32e24bb62f0c6f258da97101a` gegen `caf970020c8e9830fc1690aefb7e929711b1c15e`** (Base = aktueller `main`).
R4-01 und R4-02 sind am Head behoben (Gegenbeispiele erneut ausgeführt, siehe 3). Schlüssel, Einfügen, Punkte und Sperre
der Bestenliste stimmen mit em.exe überein (eigenes Lesen der Disassembly). Öffnen der Kachel „Highscore“ ist zustandsrein
einschließlich `FinanceRandom`, die Eintragung beim Saisonwechsel lässt den Spielstand über vier Saisonwechsel
bitgleich, Tests schreiben nicht in den Benutzerordner. Alle drei Standardläufe grün. Offen bleiben nur P3-Befunde
(Titel 1 Pixel versetzt, Testlücke Schlüssel, Doku/Restlücken); keiner blockiert.

**Erwartet offen, bestätigt** (`[APPROX]` in Code und [highscore.md](https://github.com/kaaasi/eishockey-manager-redone/blob/c3e04154cecadcc32e24bb62f0c6f258da97101a/docs/highscore.md#L25-L30)
bzw. [DebCupGamesScreen.cs](https://github.com/kaaasi/eishockey-manager-redone/blob/c3e04154cecadcc32e24bb62f0c6f258da97101a/src/EishockeyManager.UI/Screens/Competitions/DebCupGamesScreen.cs#L107-L113)):
Spielerwerte 0x49D36 (Kaufpreis × 3/2), Europa-Stufe auf Pokalbyte 1 / Pokalbytes 2–3 / Meister-Code 0x35, Penalty statt
Verlängerung (ein Punkt hinter dem Ergebnis). Keine Abnahme dieser Teile.

## 2. Befunde

### H2-01 · P3 · Titel „Highscore“ 1 Pixel zu weit links; „pixelgenau geprüft“ trifft für den Titel nicht zu

[HighscoreScreen.cs:163](https://github.com/kaaasi/eishockey-manager-redone/blob/c3e04154cecadcc32e24bb62f0c6f258da97101a/src/EishockeyManager.UI/Screens/Statistics/HighscoreScreen.cs#L163),
[highscore.md:15–16](https://github.com/kaaasi/eishockey-manager-redone/blob/c3e04154cecadcc32e24bb62f0c6f258da97101a/docs/highscore.md#L15-L16).

Ablauf `highscore` (`01_highscore_video`) gegen `oberflaeche_0844`/`0848`/`0852`/`0855`: Zeilen passen (horizontaler
Versatz −0,15 px ≈ Videoversatz −0,25), der Titel nicht. Ausdehnung der Titelpixel (Helligkeit > 600, y 3–17):
Remake x = 116 … 201, Video x = 117 … 202 in allen vier Bildern. Mittlere Abweichung im Titelbereich (Remake mit
Unschärfe 0,5) 31,3 bei Versatz −0,25 gegen 10,9 bei +0,75. Die Parameter des Originalaufrufs stimmen
(`0x5D9B8–0x5D9D6`: 0x30:0x885 mit x 0 … 0x140, Grundlinie 0xF, Farbe 0xE, Schatten 1); die Abweichung liegt in der
Rundung von `CompetitionWidgets.Centered` für diese Textbreite. Ein automatischer Pixeltest fehlt (nur Konstanten in
`Layout_AsInOriginal`). Erforderlich: Rundung gegen das Video korrigieren oder als `[APPROX]` vermerken und die Aussage
in `highscore.md` einschränken.

**Zur Frage (1), Referenzbild:** `oberflaeche_0840` zeigt „Jugendteams“, nicht die Bestenliste (die Abnahme in
`arbeitspakete.md` nennt das falsche Bild). 0841–0843 sind Übergänge. 0844–0855 zeigen dieselbe Liste und unterscheiden sich
von 0848 nur durch den Mauszeiger (mittlere Abweichung ≤ 2,6, 0849–0852 ≤ 0,22). Der Vergleich gegen 0848 ist daher
ausreichend.

### H2-02 · P3 · Schlüssel: Bedingung „Dauer = eine Saison **und** Startliga = Bundesliga“ ungetestet

[HighscoreTests.cs:101–113](https://github.com/kaaasi/eishockey-manager-redone/blob/c3e04154cecadcc32e24bb62f0c6f258da97101a/tests/EishockeyManager.Core.Tests/HighscoreTests.cs#L101-L113).
Code stimmt mit 0x289A3–0x289C5 überein. Gegenprobe: `!(duration == 0 && league == 0)` → `!(duration == 0)` lässt alle
35 Core-Tests grün. Fehlender Fall z. B. `(4, EineSaison, ZweiteLiga, OberesDrittel) → null`.

### H2-03 · P3 · Auslosung im Spiel nicht mehr erreichbar; Dateigrenzen-Ausnahmen unvollständig aufgeführt

[DebCupDrawScreen.cs:103](https://github.com/kaaasi/eishockey-manager-redone/blob/c3e04154cecadcc32e24bb62f0c6f258da97101a/src/EishockeyManager.UI/Screens/Competitions/DebCupDrawScreen.cs#L103),
[arbeitspakete.md:115](https://github.com/kaaasi/eishockey-manager-redone/blob/c3e04154cecadcc32e24bb62f0c6f258da97101a/docs/arbeitspakete.md#L115).

Seit die Paarungs-Kachel „DEB-Pokal“ die Übersicht öffnet (originalgetreu, gleicher Stub), erzeugt kein Spielpfad mehr
`DebCupDrawScreen`; nur die Bildschirmfoto-Abläufe nutzen ihn, `CupDrawStep` hat keinen UI-Verbraucher. `wettbewerbe.md`
nennt das „offen“, die Restlücken der Integration in `arbeitspakete.md` nicht. Dort bitte nachtragen (Live-Auslosung im
Tagesablauf). Außer den beiden genannten Ausnahmen ändert der PR weitere fremde Dateien, jede minimal und sachlich
begründet, aber nicht in der Ausnahmenliste: `Manager/NewsBar.cs` (R4-02), `SeasonEndSummaryScreen.cs` (eine Zeile,
Eintragung beim Saisonwechsel, einziger Aufrufer von `StartNextSeason`), `Tools/Scenarios/Wettbewerbe.cs`,
`docs/wettbewerbe.md`, Tests `DebCupScreenTests`, `FlowTests`, `SeasonUiTests`. Die beiden genannten Ausnahmen geprüft und
**akzeptiert**: `DebCupDrawScreen.cs` ändert nur das Ziel des `[MenuEntry]` (Beleg 0x45897 → Stub 0xE8:0x75 wie die
Tabellen-Kachel). `FinanceSponsorRating.cs` hat einen optionalen Parameter. Der einzige bisherige Aufrufer
(`FinanceSponsors.cs:181`) übergibt nichts, sein Verhalten ist unverändert.

### H2-04 · P3 · Nicht dokumentierte Abweichungen der Punkteformel

[Highscore.cs:307](https://github.com/kaaasi/eishockey-manager-redone/blob/c3e04154cecadcc32e24bb62f0c6f258da97101a/src/EishockeyManager.Core/Highscore.cs#L307),
[Highscore.cs:346–353](https://github.com/kaaasi/eishockey-manager-redone/blob/c3e04154cecadcc32e24bb62f0c6f258da97101a/src/EishockeyManager.Core/Highscore.cs#L346-L353),
[highscore.md:21](https://github.com/kaaasi/eishockey-manager-redone/blob/c3e04154cecadcc32e24bb62f0c6f258da97101a/docs/highscore.md#L21).
Das Modell `Points` stimmt mit 0x5D04F überein. `Inputs`/`Entry` weichen jedoch an drei Stellen ab, ohne Eintrag in
der `[APPROX]`-Liste von `highscore.md`:

1. Saisonzahl: Das Original zählt den globalen Zähler DGROUP 0x3BD (≤ 50, Teiler +1, `0x5D3A3`), das Remake die eigene
   Verlaufslänge des Managers. Das wirkt nur bei entlassenen Managern.
2. Platzierungsterm vor dem ersten Spieltag: 0 statt des Platzbytes des Vereins (nur im Code als `[APPROX]`).
3. Titelzähler aus dem Verlauf statt der Bytes Manager + 0x15–0x17 (`0x5D545–0x5D56D`). Gesamtpunkte werden begrenzt,
   im Original laufen sie als 16-Bit-Wert über.

Wortlaut „Europa nur bei DEB-Sieger bzw. ≥ 8“: Der Zweig „≥ 8“ ist nach der Maskierung `& 7` (0x5D41D) unerreichbar. Der
Code ist richtig, die Doku sollte „nur bei DEB-Sieger“ sagen.

## 3. Ausgeführte Prüfungen

**Stand:** frischer Klon, `git fetch origin c3e0415…` → `git rev-parse HEAD` = `c3e04154cecadcc32e24bb62f0c6f258da97101a`.
`main` = Base, CI-Check `build` auf dem Head `success`. Originaldaten aus `kaaasi/eishockey-manager-referenz`
(`fetch-reference.sh original`), SDK/Pakete per `setup-workspace.sh`, `dotnet --version` 10.0.402. Ausführung am
nachgewiesenen Checkout, nicht nur Codelesen. Proben und Mutationen nur in einem getrennten `git worktree` (nicht committet).

`tools/pruefen.sh` (alle drei Läufe):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 750 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 633 | 0 | 102 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 633 | 117 | 0 |

Commit `c3e04154…`, SDK 10.0.402, Ergebnis: bestanden. Die Differenz 750 ≠ 633 + 102 entsteht, weil übersprungene
Theorien als ein Fall zählen; dasselbe Muster zeigte das Review Runde 4.

**Gegenbeispiele (eigene Proben am Head):**
- **R4-01:** Neues Spiel → „Tabelle DEB-Pokal“ und Paarungen „DEB-Pokal“ öffnen beide `DebCupGamesScreen`; nach dem
  ersten gespielten Pokaltermin (`CurrentRound` = 0) unterscheidet sich das Bild in 5344 Pixeln, je Paarung genau ein
  Ergebnis, Tore links/rechts passend zu `tie.Games` mit vertauschten Seiten. `StateFingerprint` vor/nach jedem Öffnen
  gleich. **Behoben.**
- **R4-02:** Vier Manager, `RequestLoan(1, 0, 100)`, `NewsBar.Draw`: rote Schrift beginnt bei x = 260 = Breite von
  „(Klicken)“ (bei „(Neuer)“ wäre es 265). Klick 100|215 im Hauptmenü öffnet `InboxScreen`. Der Hinweis nennt jetzt den
  tatsächlichen Zugang. **Behoben** (Wortlaut `[APPROX]`).

**Besonders prüfen:**
1. **em.exe:** Schlüssel `0x28A26–0x28A69`: 0 → keine Liste, Knopfbits 2 → 0x7530, sonst
   `[0x118] + 4·[0x119] + 32·[0x11A] + 128·[0x11B] − 2`. Bedingung bei `0x2899D–0x289CC`: Level > 2 und (Stärke = 1 oder
   Dauer = Liga = 0); Enum-Werte des Remakes passen. Sperre `0x44FCE`: Kachel gesperrt bei DGROUP 0x256 = 0. Einfügen
   `0x5D73D`: Gleichheit von Name **und** Verein (0x1ADB), erster Platz mit weniger Punkten (`jl`), bei Treffer
   Überschreiben an dessen Stelle, sonst Nachrücken per `movmem` (Quelle vor Ziel), Kennung aus DGROUP 0x2C4 bei +54,
   danach Austauschsortierung. Punkte `0x5D04F–0x5D4C1`: Bewertung·100; Stadion Preis/1000·Stufe (Low-Byte); Plätze
   32-Bit-Produkt mit **vorzeichenloser** Division (`0x0:0x483` setzt cx = 1, `0x47C` vorzeichenbehaftet); Vermögen
   inklusive Forderungen, Schulden und halber Spielerwerte, Obergrenze 0x2FAF080, /2000; Platz 25·max(0, 36 − Platz −
   Versatz − 1); Pokalbytes 1–6 → 150·v; Verlauf wie im Modell; Summe /20. Eintrag `0x5D52E–0x5D60B`: +8/+6/+10 je
   Titel, mindestens 1. Aufrufer `0x45773` (Kachel), `0x56647` (Saisonwechsel, je Manager; trägt jedes Mal alle ein –
   wegen „gleicher Name ersetzt“ gleichwertig zu einmal). Den Emulator-Goldtest konnte ich nicht nachrechnen, die Skripte
   liegen nicht im Repo; die Formelprüfung beruht auf statischem Lesen. Pixel siehe H2-01.
2. **Zustandsreinheit:** Kachel „Highscore“ nach sechs Spieltagen geöffnet: `StateFingerprint` des Spiels und
   gesondert von `FinanceRandom` unverändert. Vier Saisonwechsel über die Oberfläche (Leertaste) mit und ohne die Zeile
   `HighscoreStore.Record()`: SHA-256 des Fingerprints identisch (`08AC7238…52EA7`). Benutzerordner:
   `HighscoreStore.Directory` ist nur gesetzt, wenn der Spielstandordner gleich `SaveContext.UserDirectory` ist; das setzt
   nur `Desktop/SettingsSync.cs`. Nach allen drei Läufen und den Abläufen `highscore` und `tabellen-pokale` gibt es im
   Dateisystem weder einen Ordner `Bestenliste` noch eine Datei `h.*`. `List_IsPersisted_AsOriginalFile` nutzt einen
   Temp-Ordner und löscht ihn.
3. **Dateigrenzen:** siehe H2-03.

**Gegenproben (Mutation in der Kopie → Test rot):** eigener Zufallsstrom entfernt (1 rot,
`Open_EntersManagers_WithoutChangingGameState`); Hinweis fest „(Neuer)“ (1); „Tabelle DEB-Pokal“ → Auslosung (3);
Paarungs-Kachel → Auslosung (1); Ersetzen gleicher Name entfernt (1); Sitzplatzprodukt ohne Überlauf (11); „− 2“ im
Schlüssel entfernt (3); Liga-Bedingung des Schlüssels (**0, überlebt** → H2-02); Seitentausch nach Termin 1 entfernt
(2); Sperre „Highscore“ entfernt (1); Eintragung beim Saisonwechsel entfernt (1); Europa-Sperre ohne Maske (2);
Bestenliste am Spielende entfernt (1).

**Bildschirmfotos:** `emtool screens gamedata … highscore tabellen-pokale` ohne Fehler.

### Akzeptierte `[APPROX]` (getrennt von Annahmen)

Spielerwerte als Kaufpreis × 3/2; Bewertung mit festem Zufallsstrom `new Random(Vereinsindex)`; Europa-Stufe und
Pokalbytes 2–3, Meister-Code 0x80 | 0x35; Kennung = Sitzungsstempel; Level/Liga/Stärke des ersten Managers;
Weltkugel-Wertung (Weltmeistertitel + 1, Titelspalte ausgeblendet); Speicherort „Bestenliste“ neben den Spielständen;
Penalty-Punkt als einzelnes Pixel; Hinweis „(Klicken)“; jede Taste führt zurück.

**Annahmen (nicht geprüft):** Seitentausch und Rahmenformel der Übersicht „DEB-Pokalspiele“ laut Emulation des Autors
(nur Code und Tests gelesen, Disassembly 0x34DCD ff. nicht nachvollzogen); Herkunft DGROUP 0x2C4.
