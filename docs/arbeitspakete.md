# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 8 – Funktionskarte: Spielerwert, Transfers, Tabellen, Statistiken (ab 31.10.2026)

Runde 7 (Spielberechnung, Startwerte, DOSBox-Befunde): [archiv/runde-7.md](archiv/runde-7.md); ältere Runden in
[archiv/](archiv/).
**Ziel der Runde (Leitstelle nach `docs/re-abdeckung.md`, Auftrag Nutzer 31.10.2026):** die größten unbekannten bzw.
genäherten Teile von em.exe, die das Spiel bestimmen, aus dem Code belegen und umsetzen – Spielerwert, Transfers und
Verträge, Tabellen, Spielstärke der Managervereine – sowie die Statistiken und Einzelprämien auf den Zählern aus
`spielberechnung`. **Torszenen, Anzeigetafel und Reporter bleiben bis zum Schluss ausgeklammert** (`livespiel`,
Funktionskarte Nr. 14/16).
**Base für alle Spuren:** aktueller `main`; tatsächlichen Base-SHA in den Draft-PR. Weiterhin verbindlich: Mehrsaisonprobe
(4 Wechsel, passiv und aktiv) bei Saisonwechsel, Kader oder Geld; Schema mit Migration in eigener Datei für jeden neuen
Zustand (die Leitstelle nummeriert in der Integration); geänderte Abdrücke je Fall begründen (FN-01); Befund-Issues mit
`Fixes #<nr>`; jede Fundstelle in `[ORIGINAL]`/`[APPROX]` und – wo die Funktionskarte sie als „unbekannt“ führt – im
eigenen Abschnitt von `docs/reverse-engineering.md` nachgetragen (die Karte erzeugt die Leitstelle nach der Integration
neu). **Entwickler und Reviews sind Claude-Sitzungen** (geplante Läufe, Entscheidung Nutzer 31.10.2026).

```
Runde 8 (parallel):  spielerwert  tabellen-original  spielstaerke-manager  statistiken  nationalteam  optionen-kalender  befunde-p3
nach spielerwert:    transfer-original  spielerinfo
Vorab:               spielerwert (einzeln reviewt und gemergt)
Integration:         befunde-p3 → optionen-kalender → spielerinfo → transfer-original → tabellen-original
                     → nationalteam → statistiken → spielstaerke-manager zuletzt
```

**Start gestaffelt:** alle Spuren sofort außer `transfer-original` und `spielerinfo`; diese beiden starten nach dem Merge
von `spielerwert` (0x49D36 wird von 0x465F1, 0x4A1DB und 0x041AC gerufen). `spielerwert` wird deshalb einzeln vorab
reviewt und gemergt und prüft die Aufruferzahl von 0x49D36 (#225) selbst, weil `befunde-p3` erst später integriert wird.

**Zuständigkeit für gemeinsame Dateien:**
- `Core/MatchEngine*.cs` und `Core/Squad.Strength.cs` gehören **allein** `spielstaerke-manager`. Andere Spuren rufen die
  Spielberechnung nur auf und ändern keine Aufrufer-Signaturen. Dazu gehört auch die Abbildung Aufstellung → Team-/Reihenwerte
  für die Länderwerte Deutschlands (#193) samt Anzeige `UI/Screens/Statistics/StrengthScreen.cs`.
- `Core/Squad.Tactics.cs` ist geteilt: `nationalteam` nur Startwert und Anzeige unter „AUTOM.“ (`Shown*`) samt Zustand
  „Werte des letzten Spiels“ für das Nationalteam (#192); `spielstaerke-manager` nur `EffortStrengthBonus`, `Effective*`
  und `Auto*` (0x974B, 0x939A). Vereins-Startwerte und Vereinsanzeige bleiben unverändert. Integration: `nationalteam`
  vor `spielstaerke-manager`.
- Schema: jede Spur mit neuem Zustand legt ihre Migration als **neue Datei** `Core/SaveGame.Migration.<Spur>.cs` an;
  `SchemaVersion` und den Eintrag in `Migrations` (`Core/SaveGame.cs`) trägt die Leitstelle in der Integration ein.
- `Core/Season.cs`: nur `tabellen-original` (Tabelle und Sortierung); `Playoffs.cs`/`CupsGroup.cs` nur deren `Table`.
- `Core/Transfer*.cs`, `Core/Player.Contract.cs`: nur `transfer-original`. `spielerwert` liefert den Wert in einer neuen
  Datei und stellt nur Highscore und Sponsorbewertung um; den Einsatz in Transfers macht `transfer-original`.
- `Core/FinancePremiums.cs` und `UI/Screens/SeasonEnd*.cs`: nur `statistiken`.
- `Core/CupsNational*.cs`, `Core/CupsWorld.cs`, `UI/Screens/Competitions/National*.cs`, `WorldCup*.cs`,
  `FriendliesScreen.cs`, `UI/Screens/TileLocks.cs`: nur `nationalteam`.
- `MainMenuScreen.cs`: nur `befunde-p3`, minimal und begründet. Neue Bildschirme melden sich über `[MenuEntry]` an.
- Aufrufe eines Fensters aus fremden Bildschirmen (z. B. Spielerdaten-Fenster aus Transferliste oder Ranglisten) baut die
  Leitstelle in der Integration ein; die Spur nennt die Stelle im PR. Einzige Ausnahme: `spielerinfo` darf den Aufruf in
  `LineupScreen.cs` selbst setzen.
- Referenz- und Mehrsaisontests (`RngTwinProbeTests`, `GameModeTests.Mode4_ReferenceValues_Unchanged`,
  `FinanceCalibrationTests`, Pokal-Mehrsaisonproben, `SaveLoadScreenTests`, `SeasonUiTests`, `DebCupDrawFlowTests`,
  Schemaabdruck in `SaveGameTests`): Sollwerte nur von Spuren ändern, die Ergebnisse, Kader, Geld oder Zufallsverbrauch
  verändern, je Fall begründet; die Leitstelle ermittelt sie auf dem integrierten Stand neu.
- Neue Tests in eigenen Dateien (`<Spurname>Tests.cs`); gemeinsame Testhelfer nur additiv. `docs/reverse-engineering.md`:
  jede Spur nur in einem eigenen Abschnitt.

### Spur `spielerwert` – Wert eines Spielers nach em.exe 0x49D36

- **Ziel:** die Formel aus 0x49D36 (Ov 0x118 + 0x3D36, Einsprung `0x118:0x70`, 1189 B) belegen – Eingänge (Stärke,
  Alter, Position, Kondition/Technik/Form, Vertrag …), Rundung, Ausgabeeinheit – und als einzige Quelle des Spielerwerts
  einführen. Highscore (Vermögen 0x5D04F, bisher „Kaufpreis · 3“) und Sponsorbewertung 0x3F833 darauf umstellen. Den
  Einsatz im Saisonende 0x26F20 und in der Aufstellung 0x42CB1 nur einordnen und im PR nennen.
- **Belege:** `docs/re-abdeckung.md` Abschnitt „Spielerwert 0x49D36“; Emulatorprobe wie bei 0x5D04F (`docs/highscore.md`);
  Aufruferzahl nach #225 prüfen.
- **Abnahme:** (1) roter Test mit Werten aus der Emulatorprobe bzw. einem Original-Spielstand (`emtool hok dump`), danach
  grün; (2) `[ORIGINAL]` je Formelteil; (3) Mehrsaisonprobe (die Sponsorbewertung ändert Geld; Highscore nur Anzeige) mit begründeten
  Abdruckänderungen; (4) kein neuer Spielzustand ohne Schema.
- **Dateigrenzen:** neue Datei `Core/Player.Value.cs`, `Core/Highscore.cs`, `Core/FinanceSponsorRating.cs`,
  `Core/SaveGame.Migration.<Spur>.cs` (neue Datei, nur bei neuem Zustand), `docs/reverse-engineering.md` (eigener Abschnitt),
  `docs/highscore.md`, Tests.
- **Modell:** Claude Opus 5.5.
- **Status:** gemergt (PR #239, Review #247, 31.10.2026): Formel 0x49D36 im Emulator belegt (2.000 Fälle),
  `PlayerValue` in `Core/Player.Value.cs`; Highscore und Werbebewertung umgestellt; Aufrufer (#225 RA-01): 12 Funktionen,
  21 Aufrufstellen (`docs/reverse-engineering.md`).
  Restlücken aus Review #247 (P3): SW-01 Test für den Zufallsverbrauch der Werbebewertung bei Spielern mit Angebot;
  SW-02 Benennung +0x1C/+0x1E/+0x20 (laut `spielstand-original.md` Spiele beim jetzigen Verein bzw. offen) → `statistiken`;
  SW-03 Verweis `PlayerValueTests` → `SpielerwertTests`; Goldtest aus Original-Spielständen → `transfer-original`.

### Spur `transfer-original` – Transferliste, Ablöse und Vertragsverhandlung nach em.exe (Ov 0x118)

- **Ziel:** die Transferregeln des Originals statt der Kalibrierung an sechs Videopreisen: Transferliste 0x465F1
  (Anzeige, Kaufen, Leihen, Angebot; „DM (K)/(L)“ 0x46000), Ablöse/Angebot 0x491F9, 0x49A3B, Vertragsverhandlung 0x4A1DB
  (Gehalt, Dauer, Versicherung) – auf dem Spielerwert aus `spielerwert`. Was an Computervereinen und Spielerpool
  (`Transfer.Squads.cs`) nicht aus Ov 0x118 belegbar ist, bleibt `[APPROX]` und wird im PR aufgelistet.
- **Belege:** `docs/re-abdeckung.md` Nr. 1 und 4; Videos der Reihen `karriere`/`eisbaeren` (Fundlisten
  `docs/referenz-videos.md`); Kaderplätze und Spielerpool an FILE05 (`docs/formate/spielstand-original.md`).
- **Abnahme:** (1) rote Tests je Regel (Preis, Leihgebühr, Annahme eines Angebots, Gehaltsforderung, Laufzeit), danach
  grün; (2) Gegenprobe an den sechs Videopreisen; (3) Pixelvergleich Transferliste und Verhandlungsfenster mit Kennzahl;
  (4) Bildschirme zustandsrein; (5) Mehrsaisonprobe (Kader und Geld); (6) Schema nur bei neuem Zustand.
- **Dateigrenzen:** `Core/Transfer*.cs`, `Core/Player.Contract.cs`, `Core/SaveGame.Migration.<Spur>.cs` (neue Datei, nur bei
  neuem Zustand), `UI/Screens/Team/TransferScreen.cs`,
  `TransferDialog.cs`, `ContractsScreen.cs`, `Tools/Scenarios/` (eigene Datei), neues `docs/transfer.md`,
  `docs/reverse-engineering.md` (eigener Abschnitt), Tests.
- **Modell:** Claude Opus 5.5.
- **Status:** Teil 1 (Preis, Annahme, Unfair-Grenze, Angebote für eigene Spieler) in #248; Teil 2 Vertragsverhandlung
  0x4A1DB, Sperre nach Ablehnung, Pixelvergleich offen (siehe `docs/transfer.md`).

### Spur `spielerinfo` – Spielerinfo- und Spielerdaten-Fenster (0x41683, 0x041AC)

- **Ziel:** die beiden Fenster nach em.exe: Spielerinfo 0x41683 (Rückennummer, Tendenz, Verletzung, „IM TEAM/RESERVE“) und
  Spielerdaten 0x041AC (kb\10; „GELIEHEN“, „Kaufpreis“, „Alter:“, „Unterzahl/Überzahl“, Spielerwert). Zuerst am Video
  zuordnen, welches Fenster wo erscheint (Aufstellung 0x3FFE0/0x40364, Ranglisten 0x25E30, Transferliste 0x465F1).
- **Abnahme:** (1) Zuordnung mit Bildnamen im PR; (2) Pixelvergleich je Fenster mit Kennzahl; (3) roter Test je Feld
  (Text und Wert), danach grün; (4) Fenster zustandsrein; (5) Aufrufstellen aus fremden Bildschirmen im PR für die
  Integration benannt. `PlayerInfoPopup.Show(Player, long)` bleibt als Aufruf stabil (genutzt von `TransferScreen.cs`,
  Spur `transfer-original`); eine Umbenennung oder Teilung stellt die Leitstelle in der Integration um.
- **Dateigrenzen:** `UI/Screens/Team/PlayerInfoPopup.cs`, neue Datei `UI/Screens/Team/PlayerDataWindow.cs`,
  `UI/Screens/Team/LineupScreen.cs` (nur Aufruf), `Tools/Scenarios/` (eigene Datei), `docs/reverse-engineering.md`
  (eigener Abschnitt), Tests.
- **Modell:** Claude Sonnet 5.5.
- **Status:** Umsetzung fertig, Review offen (Claude Code, PR #249, Base `421d96f`); Beleg und Restlücken in `docs/reverse-engineering.md`, Abschnitt „Spur `spielerinfo`“. Offen: Aufruf des Fensters aus Transferliste (Marktwert über `PlayerDataContext`), Ranglisten und Aufstellung (Leitstelle, Integration).

### Spur `tabellen-original` – Tabellenberechnung und -ansicht (0x4F78D, 0x50283, 0x50B45)

- **Ziel:** Reihenfolge und Darstellung der Tabellen wie im Original: Berechnung/Sortierung 0x50283 (belegt von Pokalen und
  Nationalteam sowie 0x50B45 gerufen; ob die Liga sie nutzt, zuerst klären), Tabellenreihenfolge Gesamt/Heim/Auswärts 0x50B45 (bisher `[APPROX]`), Ansicht 0x4F78D
  („PL. TD VEREIN … BILANZ S-U-N“). Gleichstandsregeln für Liga, Play-off-Setzung, Europapokal- und WM-Gruppen.
- **Abnahme:** (1) rote Tests für jede Gleichstandsregel (konstruierte Tabellen), danach grün; (2) Pixelvergleich der
  Tabellenansicht mit Kennzahl; (3) Mehrsaisonprobe – Platzierungen bestimmen Auf-/Abstieg und Prämien – mit begründeten
  Abdruckänderungen; (4) Bildschirme zustandsrein.
- **Dateigrenzen:** `Core/Season.cs` (nur Tabelle/Sortierung), `Core/Playoffs.cs` und `Core/CupsGroup.cs` (nur `Table`),
  `Core/SaveGame.Migration.<Spur>.cs` (neue Datei, nur bei neuem Zustand),
  `UI/Screens/TableScreen.cs`, `UI/Screens/Competitions/CupTableMenuEntries.cs`, `Tools/Scenarios/` (eigene Datei),
  `docs/saison.md` (eigener Abschnitt), `docs/reverse-engineering.md` (eigener Abschnitt), Tests.
- **Modell:** Claude Opus 5.5.
- **Status:** Teil 1 (Ligatabelle: Sortierung 0x50B45, Bilanz 0x50283, Ansicht Heim/Auswärts) in PR #240; offen: Pokal-/WM-Gruppen
  und Play-off-Setzung, Pixelvergleich der Ansicht (Referenzbilder in diesem Lauf nicht erreichbar).

### Spur `spielstaerke-manager` – Spielstärke der Managervereine und Restteile der Spielberechnung (#204, #179, #193)

- **Ziel:** die in `spielberechnung` offen gebliebenen Teile (`docs/spielberechnung.md` §8, §11): Managerteil 0x386D1,
  0x378C5, Teamwertabbildung 0x98F8–0x9C63, 0x87F7, 0x8EF2; Verletzungen im Spiel. Damit #204 (mehrjährige
  Meisterdominanz, R7N-01) klären: Abbildung, fehlende Streuung des Laders 0x2EF1B (`MatchEngine.Live.cs`) oder echter
  Befund. Dazu #179 (Kommentar `ForfeitsMatch`) und #193: Länderwerte Deutschlands aus der Aufstellung nach
  Freundschaftsspielen (0x30365; Kandidat Datei 0x952C/Reihenwert 0x977F, `docs/dosbox-befunde.md`), mit Anzeige im
  Stärkevergleich. Den Aufruf nach Freundschaftsspielen in `Core/CupsNational*.cs` nennt die Spur im PR; die Leitstelle
  schließt ihn in der Integration an.
- **Abnahme:** (1) roter Test je belegtem Teil, danach grün; (2) Saisonverteilung (Meister über ≥ 8 Saisons, Tore je
  Spiel, Strafminuten) gegen die Videos bzw. DOSBox-Befunde, Ergebnis im PR; (3) Mehrsaisonprobe Modus 1, 2 und 4, passiv und
  aktiv, jede Referenzwertänderung begründet; (4) Schema nur bei neuem Zustand.
- **Dateigrenzen:** `Core/MatchEngine*.cs`, `Core/Squad.Strength.cs`, `Core/StadiumStatistics.cs` (nur #179),
  `UI/Screens/Statistics/StrengthScreen.cs` (nur #193), `Core/Squad.Tactics.cs` (nur `EffortStrengthBonus`,
  `Effective*`, `Auto*`),
  `Core/SaveGame.Migration.<Spur>.cs` (neue Datei), `docs/spielberechnung.md`, Tests.
- **Modell:** Claude Opus 5.5.
- **Status:** Teil 1 in PR #241 (Claude Code): Lader 0x2EF1B, Tagesform 0x4F527, Meisterverteilung (#204), #179. Offen: Managerteil 0x386D1/0x378C5/0x98F8/0x87F7/0x8EF2, Verletzungen, #193, Videovergleich.

### Spur `statistiken` – Statistiken Spieler, Fairplay, Verein und Einzelprämien (#135, #222)

- **Ziel:** die Kacheln „Spieler“, „Fairplay“, „Verein“ unter Statistiken auf den Zählern aus `spielberechnung`:
  Ranglisten 0x25E30 (Ov 0xC8, Topscorer/Torhüter) und Fair-Play 0x2A1E0 (Ov 0xE0); „Verein“ zuerst am Video zuordnen
  (Kandidat Vereins-/Gegnerinfo 0x4D385 mit Spielplan 0x05CA0, „Höchster H-Sieg“, „Noch keine Eintragung“). Einzelprämien
  Topscorer, bester Torhüter, Fair-Play im Saisonende 0x26F20 (#135) über `GameState.Book`. Dazu #222.
- **Abnahme:** (1) roter Test je Rangliste (Reihenfolge, Gleichstand, Mindestspiele) und je Prämie, danach grün;
  (2) Pixelvergleich je Bildschirm mit Kennzahl; (3) Bildschirme zustandsrein; (4) Mehrsaisonprobe (Geld) mit begründeten
  Abdruckänderungen; (5) Schema nur bei neuem Zustand (z. B. Höchstwerte je Verein).
- **Dateigrenzen:** neue Dateien `Core/StatisticsPlayers.cs`, `Core/StatisticsClub.cs`,
  `UI/Screens/Statistics/PlayerStatisticsScreen.cs`, `FairplayScreen.cs`, `ClubStatisticsScreen.cs`,
  `Core/FinancePremiums.cs`, `UI/Screens/SeasonEnd*.cs`, `Core/SaveGame.Migration.<Spur>.cs` (neue Datei),
  `Tools/Scenarios/` (eigene Datei), `docs/wettbewerbe.md` (eigener Abschnitt), `docs/reverse-engineering.md` (eigener
  Abschnitt), Tests.
- **Modell:** Claude Opus 5.5.
- **Status:** PR #242 (Claude Code, geplanter Lauf 31.10.2026): Spieler (0x25E30), Fairplay (0x2A1E0), Einzelprämien
  (#135) und #222 umgesetzt, Schema vorläufig 11 → 12. Offen: „Verein“ = 0x2D5AE (Menüsprung belegt, nicht 0x4D385),
  Pixelvergleich am Video (Referenzbilder nicht erreichbar).

### Spur `nationalteam` – WM-Statistik, WM-Paarungen und Befunde Bundestrainer (#192, #194, #195)

- **Ziel:** WM-Statistik 0x3ADE1 (kb\12, Kachel „WM-Statistik“); Paarungsansicht WM/Europapokal „Gruppe … Spieltag“,
  „Endrunde“ 0x2FC2F (#194); Taktik des Nationalteams ohne Startwert (#192, mit Migration für laufende Spielstände);
  erster Halt im Modus 1 nach „Los jetzt!“ (#195, Halt-Bedingung in 0x44E22).
- **Abnahme:** je Punkt roter Test mit DOSBox- bzw. Videobild als Beleg (Bildname im Test), danach grün, oder begründet
  als Befund offen; Pixelvergleich WM-Statistik und Paarungsansicht; Bildschirme zustandsrein; Mehrsaisonprobe Modus 1
  und 4 (Modus 2 als Gegenprobe); `docs/wettbewerbe.md`, `docs/dosbox-befunde.md` nachgeführt.
- **Dateigrenzen:** `Core/CupsNational*.cs`, `Core/CupsWorld.cs`, `Core/Squad.Tactics.cs` (nur Startwert und
  `Shown*` des Nationalteams, siehe Zuständigkeiten), `Core/SaveGame.Migration.<Spur>.cs` (neue Datei), `UI/Screens/Competitions/National*.cs`, `WorldCup*.cs`,
  `FriendliesScreen.cs`, neue Dateien `UI/Screens/Competitions/WorldCupStatisticsScreen.cs`,
  `CupPairingsScreen.cs`, `UI/Screens/TileLocks.cs`, `Tools/Scenarios/` (eigene Datei), `docs/wettbewerbe.md` (eigener
  Abschnitt), `docs/dosbox-befunde.md`, Tests. #193 gehört zu `spielstaerke-manager`.
- **Modell:** Claude Opus 5.5.
- **Status:** in Arbeit (Claude Code). Teil `taktik` (#192): fertig zur Review, PR #243. Offen: #195, #194, WM-Statistik (ohne Referenzbilder in dieser Sitzung nicht prüfbar, siehe Übergabe in #243).

### Spur `optionen-kalender` – Spielmodus-Optionen und Kalender (0x2CD1C, 0x43E21; #228)

- **Ziel:** (1) Optionen 0x2CD1C („Neuer Spielmodus:“, Drittelpause, Torszenen, WM-Spiele, Länderspiele,
  Hintergrundgrafik) mit ihrer Wirkung belegen und dokumentieren; im Einstellungsbildschirm nur Darstellung und
  Geräteeinstellungen (`GameSettings`) umsetzen. Optionen mit Wirkung auf den Spielstand (Spielmodus, WM-Spiele,
  Länderspiele) zeigt der Bildschirm nur lesend an; ihre Umsetzung in Core samt Migration ist Folgepaket
  `optionen-wirkung` (Runde 9). Unbenutzte Optionen (z. B. „Zeitung“ 0xEB8) dokumentieren. (2) Kalender: 0x43E21
  mit 0x43B1F, 0x43C38, 0x445E0 („Vor dem ersten Spieltag.“) und 0x36169 einordnen; einen Bildschirm nur bauen, wenn er am
  Video belegt ist. (3) #228.
- **Abnahme:** (1) je Option Wirkung mit Fundstelle dokumentiert; roter Test je umgesetzter Geräteeinstellung und für die
  lesende Anzeige der Spielstand-Optionen, danach grün; (2) Pixelvergleich Einstellungen und ggf. Kalender;
  (3) Bildschirme zustandsrein; (4) `docs/tagesablauf.md` bzw. `docs/saison.md` (eigener Abschnitt) nachgeführt.
- **Dateigrenzen:** `UI/Screens/SettingsScreen.cs`, `UI/Screens/Settings.cs` (Einstellungen und `SettingsSync`), neue Datei
  `UI/Screens/CalendarScreen.cs`, `Tools/Scenarios/` (eigene Datei), `docs/tagesablauf.md`, `docs/saison.md` (eigener
  Abschnitt), `docs/reverse-engineering.md` (eigener Abschnitt), Tests. Änderungen am Tagesablauf (`SeasonFlow.cs`) nur
  als Vorschlag im PR.
- **Modell:** Claude Sonnet 5.5.
- **Status:** offen.

### Spur `befunde-p3` – Kleinbefunde Hauptmenü, hok-format, Funktionskarte (#216, #217, #225, #230)

- **Ziel:** #216 Klickzone Managerkopf im Hauptmenü (0x4549D–0x454B3); #217 Bundestrainer-Warnung (< 10 Spieler) und „Sie
  können nicht mehr weiterspielen.“ im Logo-Zweig; #225 Aufruferzahl 0x49D36 und Einsprung 0x02327 in der Funktionskarte;
  #230 hok-format (Versicherungs-Fundstelle, Testlücken, `IsGoalkeeper` ohne Markierung). Dabei in `docs/re-abdeckung.md`
  die Nummer der Lücke zu #194 berichtigen (Text „Nr. 10“, Tabelle Nr. 11).
- **Abnahme:** je Issue roter Test (bzw. bei #225 Prüfung im Skript), danach grün; Bildschirme zustandsrein.
- **Dateigrenzen:** `UI/Screens/MainMenuScreen.cs` (minimal, begründet), `Formats/OriginalSaveGame*.cs`, `Tools/Hok*.cs`,
  `docs/formate/spielstand-original.md`, `tools/reverse-engineering/funktionskarte.py`, `docs/re-abdeckung.md`, Tests.
- **Modell:** Claude Sonnet 5.5.
- **Status:** umgesetzt im Draft-PR #244 (#216, #217, #225, #230); Review offen. #217: „Sie können nicht mehr weiterspielen.“ gehört zur Sicher-/Ladesperre und bleibt entfallen (Entscheidung Nutzer).

### Integration Runde 8 (Leitstelle)

Branch `integration/runde-8` von `main` 421d96f (mit `spielerwert`), zusammengeführt in Planfolge: `befunde-p3` (#244),
`tabellen-original` (#240), `nationalteam` (#243), `statistiken` (#242), `spielstaerke-manager` (#241); danach (Auftrag
Nutzer 31.10.) `spielerinfo` (#249), `transfer-original` (#248), `optionen-kalender` (#245, Teil 1: Belege). Diese drei
änderten keine Referenzwerte.
- **Schema:** 11 → 12 Taktik des Nationalteams, 12 → 13 Strafminuten je Verein, 13 → 14 Teamwerte des Laders.
- **Referenzwerte** neu auf dem integrierten Stand (RngTwinProbe, Mode4, Zähler je Saison); Seeds Börse 7 → 8 und Werbung
  aktiv 15 → 16 (Zufallsproben, Prüfziel gleich), Begründung in den Tests.
- **Für die Leitstelle nach der Integration** (aus den PRs): Funktionskarte in `docs/re-abdeckung.md` neu erzeugen; Aufruf
  des Spielerdaten-Fensters aus `PlayerStatisticsScreen` und Rangliste (nach `spielerinfo`); `docs/anforderungen/bildschirme.md`
  Z. 103 (Bilanz: acht Spiele, Heim klein) und die Vermutungen zur Sortierung 0x5CCB nach Heim/Auswärts (`CupsNationalMode.cs`,
  `SeasonFlow.cs`, `docs/saison.md`, `docs/wettbewerbe.md`) berichtigen; veralteter Hinweis „Nicht umgesetzt …“ in
  `docs/wettbewerbe.md` („Statistiken über Saisons“); Lader der Teamwerte ggf. nach `NewGame.Create` verlegen (#241);
  Aufrufstellen des Spielerdaten-Fensters umstellen (#249: `TransferScreen` auf `Show(player, price, PlayerDataContext…)`
  und `PlayerHintLine`, `ContractsScreen`/`NationalLineupScreen`, Öffnen aus Aufstellung und Ranglisten).
- **Offene Teile der Spuren** (Folgebranches): `tabellen-original` Teil 2 (Gruppen-, Play-off-Tabellen, Pixelvergleich);
  `spielstaerke-manager` Teil 2 (Managerteil, Verletzungen, #193, Videovergleich #204); `statistiken` Bildschirm „Verein“
  (0x2D5AE); `nationalteam` #194, #195 (Ursache gefunden, siehe #243), WM-Statistik; `optionen-kalender` Teil 2
  (Einstellungsbildschirm, Kalender, #228); `transfer-original` Teil 2 (Vertragsverhandlung 0x4A1DB, Gehalt 0x49A3B,
  Kaufablauf 0x494EF, Sperre nach Ablehnung, unaufgeforderte Angebote, Pixelvergleich).

- **Review** `docs/reviews/2026-10-31-integration-runde-8.md`: R8-D01 (P2, #253) in der Integration behoben. P3 als
  Restlücken: R8-A01 Spielstände aus den Spurbranches `statistiken`/`spielstaerke-manager` (vorläufige Schemanummer) laden
  nicht; R8-A02 Seed-Proben hängen am Zufall (robustere Prüfung statt Seed-Wechsel); R8-C01 sieben ältere Core-Tests kaufen
  zum Listenpreis und sind nur wegen des Seeds grün; R8-C02 Reihenfolge Geld/Unfair beim Gebot an einen anderen Manager;
  R8-B01 Tabellentest ohne Regel „weniger Spiele“; R8-B02 Strafminuten im Ligaspiel nur indirekt getestet; R8-B03
  Heimspiel-Zweig der Nationalteam-Taktik ungetestet; R8-B04 freie Nationalteam-Taktik behält Modus des Vorgängers; F46-01,
  W51-01 (Doku-PRs #246, #251).

### Runde 8, Welle 3 – Folgeteile der Spuren (ab 31.10.2026, Auftrag Nutzer)

**Base:** aktueller `main` (mit Integration #250); tatsächlichen Base-SHA in den Draft-PR. Alle Regeln der Runde gelten
weiter (Mehrsaisonprobe, Schema mit Migration in neuer Datei `Core/SaveGame.Migration.<Spur>.cs`; ein **vorläufiger**
Eintrag in `SchemaVersion`/`Migrations` in `Core/SaveGame.cs` ist erlaubt, damit der Branch grün ist – die Leitstelle
nummeriert in der Integration). Das Referenz-Repo ist in geplanten Läufen erreichbar: Pixelvergleiche und
`OriginalSavePatcherTests` gehören zur Abnahme. Die Folgearbeiten der Leitstelle, die Spurdateien berühren, gehen in die
Spur, der die Datei gehört (Spalte „übernimmt“); die Leitstelle baut keine Aufrufe in diese Dateien ein.

```
Welle 3 (parallel):  tabellen-original-2  spielstaerke-manager-2  statistiken-verein  nationalteam-2
                     transfer-original-vertrag  optionen-kalender-2   (+ Leitstelle: leitstelle-r8)
Integration:         optionen-kalender-2 → tabellen-original-2 → statistiken-verein → nationalteam-2
                     → transfer-original-vertrag → spielstaerke-manager-2 zuletzt
```

| Spur (Branch `paket/<spur>`) | Ziel | Dateigrenzen (zusätzlich zur Spur aus Welle 1) | übernimmt | Modell |
|---|---|---|---|---|
| `tabellen-original-2` | Gruppentabellen (`CupsGroup.Table`, Europapokal/WM) und `Playoffs.Table` nach 0x50283/0x50B45 (Aufrufer 0x30365, 0x30852, 0x30E7B, 0x315E9, 0x34B76), Pixelvergleich Tabellenansicht mit Kennzahl, Bilanz Heim klein am Video | wie `tabellen-original`; `docs/wettbewerbe.md` nur Z. ~331 (Sortierung 0x5CCB) | Doku-Berichtigung Sortierung nach Heim/Auswärts in `docs/saison.md`, `docs/wettbewerbe.md`; R8-B01 | Opus 5.5 |
| `spielstaerke-manager-2` | Managerteil 0x386D1, 0x378C5, Reihenwerte 0x98F8–0x9C63, 0x974B, 0x87F7, 0x8EF2; Verletzungen im Spiel; #193 Länderwerte (Anschluss nach Freundschaftsspielen nennt die Spur, die Leitstelle schließt an); Videovergleich #204 | wie `spielstaerke-manager`; `Core/NewGame.cs` minimal (Lader beim Spielstart, optional) | Lader nach `NewGame.Create` | Opus 5.5 |
| `statistiken-verein` | Bildschirm „Verein“ 0x2D5AE (Texte XOR 0xD9/0xC9) zuordnen, umsetzen, Pixelvergleich aller drei Statistik-Bildschirme | wie `statistiken`; `UI/Screens/Statistics/PlayerStatisticsScreen.cs` | Aufruf Spielerdaten-Fenster aus den Ranglisten (0x26B13 → 0x041AC, `PlayerDataContext.Create`); veralteter Hinweis „Nicht umgesetzt …“ in `docs/wettbewerbe.md`; SW-02 (Benennung +0x1C/+0x1E/+0x20); R8-B02 | Opus 5.5 |
| `nationalteam-2` | #195 (Ursache in #243), #194 Paarungsansicht WM/Europapokal (`CupPairingsScreen.cs`), WM-Statistik 0x3ADE1 (`WorldCupStatisticsScreen.cs`), Pixelvergleich DOSBox/Video | wie `nationalteam`; `UI/Screens/SetupScreen.cs` minimal (#195) | Aufruf Spielerdaten-Fenster aus `NationalLineupScreen.cs`; Kommentar Sortierung in `CupsNationalMode.cs`; R8-B03, R8-B04 | Opus 5.5 |
| `transfer-original-vertrag` | Vertragsverhandlung 0x4A1DB, Gehalt 0x49A3B, Kaufablauf 0x494EF, Sperre nach Ablehnung (Pool +0x1A), unaufgeforderte Angebote (+0x09 Bit 0x40), Pixelvergleich Transferliste und Verhandlungsfenster | wie `transfer-original` | Aufrufe Spielerdaten-Fenster und `PlayerHintLine` in `TransferScreen.cs`, `ContractsScreen.cs`; R8-C01 (Tests zum Listenpreis), R8-C02; SW-01 | Opus 5.5 |
| `optionen-kalender-2` | Einstellungsbildschirm (Geräteeinstellungen, lesende Anzeige der Spielstand-Optionen), Kalender 0x43E21 mit 0x43B1F, 0x43C38, 0x445E0, 0x36169 (Bildschirm nur mit Videobeleg), #228 | wie `optionen-kalender` | – | Sonnet 5.5 |
| `leitstelle-r8` (Leitstelle, Branch `prozess/leitstelle-r8`) | Funktionskarte `docs/re-abdeckung.md` neu erzeugen; `docs/anforderungen/bildschirme.md` Z. 103 (Bilanz acht Spiele, Heim klein); Kommentar zur Sortierung in `Core/SeasonFlow.cs`; R8-A02 (robustere Zufallsproben statt Seed-Wechsel in `BoerseStartTests`, `FinanceWerbungTests`); SW-03 | `docs/re-abdeckung.md`, `docs/anforderungen/bildschirme.md`, `Core/SeasonFlow.cs` (nur Kommentar), die genannten Tests, `tools/reverse-engineering/` | – | Leitstelle |

**Abnahme je Spur** wie in Welle 1 (roter Test je Regel, Pixelvergleich mit Kennzahl, Bildschirme zustandsrein,
Mehrsaisonprobe bei Kader/Geld/Saisonwechsel, `[ORIGINAL]`/`[APPROX]`, Abschnitt in `docs/reverse-engineering.md`).
R8-A01 (Spielstände aus Spurbranches) wird nicht behoben: Spurbranches sind keine Ausgabestände.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- `livespiel` (Torszenen, Anzeigetafel, Reporter; Nr. 14/16): letzter Schritt vor v1.0 (Entscheidung Nutzer).
- `penalty-original` (0x3901B, Penaltyschießen nach Verlängerung): Runde 9.
- `optionen-wirkung` (Spielstand-Optionen aus 0x2CD1C in Core, Abgrenzung zu `nationalteam` für `CupsNationalMode.cs`):
  Runde 9, auf den Belegen von `optionen-kalender`.
- #196 Regler Ausschnitt/Maßstab des Kursverlaufs: eigenes Paket, Runde 9.
- #166 Oberliga-Austausch (400.000 DM): erst, wenn der Austausch der Oberliga samt Managervereinen belegt ist.
- Kleinere Lücken der Funktionskarte (Jugendteams 0x3A24B, Bandenwerbung 0x2A9AD, Dateidialog 0x5DBE5, Versicherung
  0x07089): Fundstellen beim nächsten Paket des jeweiligen Bereichs nachtragen.
- Import/Export von Original-Spielständen ins Remake; Highscore am Spielende und Saisonende im Modus „nur Weltkugel“
  (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).


## Paket `hok-format` – Original-Spielstände `*.HOK`

Branch: `paket/hok-format` · Status: Analyse gemergt (#176, Review #178): Verschlüsselung, Prüfsummen, Kopf und Blockfolge
belegt, `emtool hok dump`, Doku `docs/formate/spielstand-original.md` (offene Liste dort). Entscheidung Nutzer:
Sicher-/Ladesperre des Originals nicht übernehmen. Weiter in Runde 7 mit `hok-patch` (Spielstände fürs Original);
Import/Export ins Remake danach.

- Nachtrag (#197, Branch `paket/hok-format-2`): Kaderplätze, Spielerpool, Training und Taktik an FILE05 belegt;
  Kaderausgabe in `emtool hok dump`. Spielstände FILE01–05 und `KAPUTT.HOK` im Referenz-Repo `original/spielstaende/`.

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: PR 1–7 gemergt (Runde 3); Restarbeiten in Runde 4 gemergt; PR 8 in Runde 5 gemergt (`paarungen-liga`, #128). Regeln und Belege: [wettbewerbe.md](wettbewerbe.md)

| PR | Inhalt | Stand |
|---|---|---|
| 1 | DEB-Pokal: Auslosung, Heimrecht, „best of three“, Abrechnung, Bildschirm „DEB-Pokal Auslosung“ | gemergt (Runde 3, #54) |
| 2 | Europapokal (Gruppe + Endrunde), Bildschirm Gruppen/Endrunde | gemergt (Runde 3, #54) |
| 3 | WM (2 Gruppen + K.-o.), Bildschirm Gruppe/Tabelle WM | gemergt (Runde 3, #54) |
| 4 | Stärken (Ligen, Länderteams) | gemergt (Runde 3, #54) |
| 5 | Statistiken: Ewige Tabelle (Totale), Früher | gemergt (Runde 3, #54) |
| 6 | Bundestrainer: WM-Kader, „WM Vorbereitung“ (Freundschaftsspiele), Etat; WM: Werte Deutschlands neu ausgewürfelt, Länder 20-mal getauscht | gemergt (Runde 3, #54) |
| 7 | Spielmodus „nur Weltkugel“: Spielvorbereitung, Vereinssaison überspringen, Menü des Bundestrainers | gemergt (#87 über #89) |
| 8 | Paarungen Liga | gemergt (Runde 5, #128); Pixelvergleich Runde 6 (`pixel-wettbewerbe`) |
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | Runde 8 (`statistiken`, `nationalteam`) |

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: Format, Dialog und automatisches Speichern gemergt (Runde 3, [archiv/runde-3.md](archiv/runde-3.md)); #80 in Runde 4 behoben (#105)

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`) und Export per Vorlage (`hok-format`) – wartet auf Original-Spielstände des Nutzers.

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen auf den Ereignissen aus `spielberechnung` (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
