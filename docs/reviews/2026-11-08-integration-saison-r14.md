# Review: Integration Runde 14, Spur `saison` (PR #652)

Base `b7e1eb279c78d65a93a109d54f7fa696ba814301` · Head `194f387e60fd0220f570c5867c2d1a55b21f5c0b` · Reviewer: Claude (geplanter Lauf,
frische Sitzung ohne Implementierungskontext) · 08.11.2026 · Checkout: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer.
Vollprüfung des ganzen Deltas (15 Dateien, +348/−23), Codelesen **und** Ausführung am nachgewiesenen Checkout mit Originaldaten.

## 1. Merge-Empfehlung

**Nein** für diesen Head. #591, #592 und #600-Rest sind in Ordnung und per Gegenprobe belegt. #349 erfüllt die Kernanforderung
nicht: Mit „Ganzer Tag“ an rechnen an einem Pokaltag mit Managerverein die meisten Paarungen weiter den schnellen Weg (R14S-01, P1),
und die Tests merken das nicht (R14S-02). Bei „aus“ ist das Verhalten gleich Base (Gegenbeispiel 3), Schema 39 → 40 funktioniert.

## 2. Befunde

### R14S-01 (P1) – Merker „Managerverein spielt“ wird je Paarung neu ausgewertet und kippt mitten im Spieltag

[`MatchEngine.Live.cs` Z. 89](https://github.com/kaaasi/eishockey-manager-redone/blob/194f387e60fd0220f570c5867c2d1a55b21f5c0b/src/EishockeyManager.Core/MatchEngine.Live.cs#L89) ruft je Paarung
`game.ManagerPlaysOn(game.CurrentDay)` ([`SeasonFlow.cs` Z. 120](https://github.com/kaaasi/eishockey-manager-redone/blob/194f387e60fd0220f570c5867c2d1a55b21f5c0b/src/EishockeyManager.Core/SeasonFlow.cs#L120)). Das fragt
`MatchOn`, und `MatchOn` hängt vom Fortschritt ab (Pokalrunde, `PlayedMatchDays`, `series.NextPairing`, `PlayedRounds`).
Sobald das Spiel des Managers gerechnet ist, liefert es `null`. Alle Paarungen danach nehmen wieder den schnellen Weg 0x3731F.

Im Original wird der Merker **einmal** gebildet (Datei 0x36FEA–0x3700B: `[bp-5]` aus DGROUP 0xA729+m, m = 0…5) und bei 0x37028
geprüft. Die Sätze werden in 0x32AA3 gelöscht, in der Spielvorbereitung 0x545A6/0x547C7 gesetzt und erst nach dem Tag in 0x54FF3
zurückgesetzt. Selbst nachgelesen (`disasm.py`).

**Gegenbeispiel** (Szenario des PR-Tests `ManagerPlaysCupDay_…`: Seed 5, Köln (5), DEB-Pokal-Schalter aus, „Ganzer Tag“ an).
Zur Messung wurde eine Kopie instrumentiert: Merker zu Tagesbeginn in `ProcessDay` gegen den Wert je Paarung. Am Tag 12 (DEB-Pokal)
gibt es 16 Pokalpaarungen. **13** davon sehen `ManagerPlaysOn = false` und rechnen schnell, z. B. 32-23, 14-6, 22-0.
Über 4 Saisons (6 Vereine/Seeds, nur Continue) kippt der Merker bei 54–77 von rund 780 Pokal-/Play-off-/Aufstiegsrundenaufrufen,
z. B. DEB-Pokal Tag 12/14/40/42/47/96, Aufstiegsrunde Tag 149/153/163/168/188/193, Play-offs Tag 153. Ergebnis: Mit Einstellung an
weder Original noch Remake. Welche Paarungen betroffen sind, hängt nur von der Reihenfolge ab, in der gerechnet wird.

**Vorschlag:** Den Merker einmal je Tag vor den ersten Spielen in `ProcessDay` bilden (Feld oder Parameter, nicht gespeichert, da
innerhalb eines Tages). `PlayWithEvents` liest nur diesen Wert. Regressionstest: Am Pokaltag mit Managerverein laufen bei Einstellung an
**alle** Paarungen ohne Manager durch `OriginalMinutes.Play`, z. B. mit einem Zähler oder Testhaken. Rot am Head.

### R14S-02 (P2) – Tests zu #349 sind gegen R14S-01 und gegen „Merker ignoriert“ blind

- [`ManagerPlaysCupDay_OptionOn_AllPairingsUseMinuteLoop_RandomDiffers`](https://github.com/kaaasi/eishockey-manager-redone/blob/194f387e60fd0220f570c5867c2d1a55b21f5c0b/tests/EishockeyManager.Core.Tests/SaisonManagerTagTests.cs#L58)
  prüft nur `Assert.NotEqual(randomOff, randomOn)` (Z. 69). Schon eine Paarung im Minutenweg macht ihn grün. So bleibt der Test am
  Head grün, obwohl 13 von 16 Paarungen schnell rechnen. Der Name verspricht „AllPairings“.
- [`NoManagerInvolved_OptionDoesNothing_SameRandom`](https://github.com/kaaasi/eishockey-manager-redone/blob/194f387e60fd0220f570c5867c2d1a55b21f5c0b/tests/EishockeyManager.Core.Tests/SaisonManagerTagTests.cs#L73)
  bleibt grün unter der Mutation `if (game.Options.ManagerDayMinuteLoop)`, die die Bedingung „Managerverein spielt“ weglässt (Tabelle,
  M4). Der Test bricht beim ersten Nicht-Ligatag des Managers ab. Ein Pokaltag **ohne** Manager mit eingeschalteter Option kommt
  in seinem Fenster nicht vor.
- **Vorschlag:** Den ersten Test auf „alle Paarungen“ schärfen (siehe R14S-01). Für den zweiten einen Tag mit Pokal/Aufstiegsrunde
  wählen, an dem kein Managerverein spielt, z. B. einen Manager der ausgeschieden ist, und die Gleichheit genau dort prüfen.

### R14S-03 (P3) – „auch die WM-Endrunde“ ist als `[ORIGINAL]` stärker als belegt

[`MatchEngine.Live.cs` Z. 82](https://github.com/kaaasi/eishockey-manager-redone/blob/194f387e60fd0220f570c5867c2d1a55b21f5c0b/src/EishockeyManager.Core/MatchEngine.Live.cs#L82) und `GameOptions.cs` Z. 74–81:
Der Sprung 0x37028 liegt vor den Codeprüfungen (nachgelesen). Dass die WM-Endrunde (Code 10, 0x31017) 0x36B5D überhaupt erreicht, ist
laut Kommentar Z. 78–79 selbst nur aus dem Aufrufmuster geschlossen (R9S-07, #358). Die Einstellung schaltet die WM-K.-o.-Spiele um
(`minuteLoop = true` überschreibt den festen schnellen Weg). Dieser Teil sollte als `[APPROX]` mit Verweis auf R9S-07 gekennzeichnet sein.
Kein Verhaltensfehler bei „aus“.

### R14S-04 (P3) – Dateigrenzen: `SaveGame.cs` und neue Migrationsdatei

`SaveGame.cs` (Schema 40, Eintrag `[39]`) und `SaveGame.Migration.Saison4.cs` stehen nicht in der Liste der Spur `saison`. Das ist
die übliche Folge eines neuen Zustandsfelds (Schemaregel, Nummer von der Leitstelle). Im PR begründet, akzeptiert. Alle übrigen
Dateien liegen innerhalb der Grenzen von Teil 4. `MatchEngine*` ist nur an der Auswahl der Minutenrechnung geändert,
`ManagerPlaysOn` nur auf `internal` gestellt.

### Ohne Befund

- **#591:** Die Gegenprobe ist wirksam. Mutation „Folge nicht übernehmen, neu bilden und mischen“ (M1): der neue Test ist rot (2/2).
  Mit derselben Mutation und **ohne** die neue Zeile `game.Random.Next(1_000_000)` ist er grün (M1-alt). Das belegt, dass der
  alte Test blind war.
- **#592:** Der Rückfall auf die Indexfolge stimmt. Am Base liefert `FairPlayPremiumWinner` `null` (M2 rot). Bei umgekehrter Indexfolge
  ist der Gleichstandsfall rot (M2b). `[APPROX]` begründet, Fundstelle 0x27184–0x2725D im Fensterkommentar.
- **#600-Rest / A112:** Datei 0x29C78–0x29CF7 nachgelesen: `random(0x7C0[l], 0x7C0[l]+0x7B1[l]−1)` über die Plätze der Start-Liga,
  neu gezogen bei Platz eines anderen Managers oder wenn 0x45CD0 ≠ 0, dann `0x36169(Vereins-Nr. des Managers, r, 0x80)`.
  Der Kommentar in `OutsidePlaces` stimmt. Die Paarung bei mehreren Managern ist als `[APPROX]` gekennzeichnet: Mutation „Paarung
  umgekehrt“ (M13) bleibt grün, das ist so gewollt und dokumentiert.
- **AGENTS.md §4:** Zeit vergeht nur über `Continue()`. Der Optionsbildschirm ändert die Einstellung nur per Klick, nicht beim Öffnen.
  `Book` ist nicht berührt.

## 3. Ausgeführte Prüfungen

`tools/claude/setup-workspace.sh`, SDK 10.0.402, Originaldaten aus `kaaasi/eishockey-manager-referenz`, `tools/pruefen.sh` (alle
drei Läufe) am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2288 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1922 | 0 | 328 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1923 | 365 | 0 |

Ergebnis: bestanden.

**Gegenproben** (Mutation in einer Kopie, `dotnet test --filter` mit Originaldaten):

| Nr. | Test | Mutation | Ergebnis |
|---|---|---|---|
| M1 | `MultiSeason_NextSlotsFormedOn29April_…` (2) | Übernahme in `AdvanceLeagueSlots` abgeschaltet | rot 2/2 ✔ |
| M1-alt | dito ohne neue Zufallszeile | wie M1 | grün (alter Test blind) ✔ |
| M2 | `FairPlayWinner_WithoutFormedSlots_…` | `NextLeagueOrder` → `NextLeagueSlots` (Base) | rot ✔ |
| M2b | dito | Rückfall absteigend | rot ✔ |
| M3 | `ManagerPlaysCupDay_OptionOn_…` | Zeile 89 `if (false)` | rot ✔ |
| M4 | `NoManagerInvolved_OptionDoesNothing_…` | Bedingung „Manager spielt“ entfernt | **grün ✘** (R14S-02) |
| M5 | `Default_IsOff_AndSaveRoundTripKeepsIt` | Vorgabe `= true` | rot ✔ |
| M6 | `Schema39Save_Migrates_OptionOff` | Migration setzt `true` | rot ✔ |
| M6b | dito | Eintrag `[39]` entfernt | rot ✔ |
| – | `MultiSeason_FourSeasonChanges_BothSettings` (4) | M3/M4/M5/M6 | grün: Rauchtest ohne Sollwert, keine wirksame Gegenprobe |
| M8 | `ManagerDaySwitch_TogglesOption` (UI) | Umschalten entfernt | rot ✔ |
| M10 | `StartSwap_*` (neu 5) | Tausch in `OutsidePlaces` entfernt | rot ✔ |
| M11 | dito | Verdrängte nur aus Oberliga (alter Kommentar) | rot (Bundesliga/2. Liga) ✔ |
| M12 | `StartSwap_TwoChosen…MultiSeason` (2) | `ApplyOberligaPairs` entfernt | rot 2/2 ✔ |
| M13 | dito | Paarung umgekehrt | grün, gewollt (`[APPROX]`) |

**Sollwerte und Fingerabdruck:** `ExpectedSchemaFingerprint` `E3546517EB3E0A02`. Neu berechnet ohne die eine Zeile
`GameOptions.ManagerDayMinuteLoop: …` ergibt sich der alte Wert `8F2FBF53475D516B`. Der neue Wert geht also allein auf das neue Feld
zurück. Neue Saaten (5, 9, 3, 4, 21, 7, 12) sind Testszenarien ohne Sollwert. Keine geänderten Fingerabdrücke von Zufallsfolgen
(`RngTwinProbeTests` unverändert und grün).

**Gegenbeispiele des Aufrufs:**

1. Siehe Tabelle.
2. Fingerabdruck auf die Regel zurückgeführt (oben).
3. Pokaltag mit Managerverein, Einstellung an: Der Zufallsverbrauch weicht ab, aber **nicht alle** Paarungen laufen durch die
   Minutenschleife (13/16 schnell, R14S-01). Einstellung aus: Seed 5 / Köln, 2 Saisons passiv und aktiv. Zufallszustand und
   Abschlusstabellen nach jeder Saison sind gleich Base (gleicher Prüfwert der Ausgaben an Base und Head).
4. Spielstand nach 15 Zügen auf Schema 39 zurückgesetzt, das Feld entfernt, geladen: Einstellung aus, wieder gespeichert mit Schema 40.
   Danach 4 Saisonwechsel bei passiv/aktiv × aus/an mit zwei Managern (5, 30): ohne Fehler, Satzfolgen vollständig, Einstellung
   übersteht jeden Roundtrip.

**em.exe selbst nachgelesen** (`tools/reverse-engineering/disasm.py`): 0x36FB0–0x37090 (Merker, Sprung 0x37028, Codeprüfungen
0x3702E–0x37085), Querverweise DGROUP 0xA729–0xA72E (0x32ABD, 0x36FF9, 0x38BC3, 0x545A6, 0x547C7, 0x54FF3), 0x29C70–0x29D00 (Starttausch).

**Erwartet offen, bestätigt:** Inventur A112 und #349 → `pruefung-karte` (Welle 2; Vermerk in `docs/arbeitspakete.md`
„Integration Runde 14“). Platz des Schalters „Ganzer Tag“ im Optionsbildschirm `[APPROX]` (kein Original-Schalter, 0x2CD1C):
akzeptiert.

**Akzeptierte `[APPROX]`:** Rückfall der Fair-Play-Folge auf die Indexfolge (#592). Paarung gewählter/verdrängter Verein nach Index
bei mehreren Managern (A112). Platz des Zusatzschalters.
**Nicht akzeptiert:** `[ORIGINAL]` an der Merkerzeile, solange R14S-01 besteht. Teil WM-Endrunde (R14S-03).

**Annahmen:** Dass 0xA729+m zwischen Spielvorbereitung und 0x54FF3 durchgehend gesetzt bleibt, folgt aus den Querverweisen und R9S-01.
Der Ablauf zwischen den Funktionen wurde nicht in DOSBox beobachtet.
