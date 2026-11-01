# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 9 – Bereiche abschließen (ab 01.11.2026)

Runde 8 (Spielerwert, Transfers, Tabellen, Statistiken, Spielstärke der Managervereine): [archiv/runde-8.md](archiv/runde-8.md);
ältere Runden in [archiv/](archiv/).
**Ziel der Runde (Auftrag Nutzer 01.11.2026):** weniger, aber größere Spuren, die je einen Bereich abschließen – alle offenen
Teile und Restlücken aus Runde 8 sind genau einer Spur zugeordnet. **Torszenen, Anzeigetafel und Reporter bleiben
ausgeklammert** (`livespiel`). **Runde 10 ist eine Sonderrunde** (Wunsch Nutzer 01.11.2026): Analyse der noch unbekannten
Teile von em.exe nach `docs/re-abdeckung.md` – Planung nach Abschluss dieser Runde.
**Base für alle Spuren:** aktueller `main`; tatsächlichen Base-SHA in den Draft-PR. Weiterhin verbindlich: Mehrsaisonprobe
(4 Wechsel, passiv und aktiv) bei Saisonwechsel, Kader oder Geld; Schema mit Migration in neuer Datei
`Core/SaveGame.Migration.<Spur>.cs` (vorläufiger Eintrag in `Core/SaveGame.cs` erlaubt, die Leitstelle nummeriert);
geänderte Abdrücke je Fall begründen; Befund-Issues mit `Fixes #<nr>`; jede Fundstelle `[ORIGINAL]`/`[APPROX]` und im
eigenen Abschnitt von `docs/reverse-engineering.md`; Pixelvergleiche und `OriginalSavePatcherTests` über das Referenz-Repo
(in geplanten Läufen erreichbar). **Entwickler und Reviews sind Claude-Sitzungen** (geplante Läufe).

### Ablauf: Teile und Selbstfortsetzung (Entscheidung Nutzer 01.11.2026)

- Jede Spur hat eine **geordnete Liste von Teilen**. Ein Lauf arbeitet die Teile der Reihe nach ab; jeder fertige Teil ist
  ein eigener PR (Branch `paket/<spur>-<n>`) mit Übergabe. Stapelregel, PR-Base, Rebase und Fortsetzung regelt verbindlich
  [vorlagen/spur-start.md](vorlagen/spur-start.md), Abschnitt „Teile und Fortsetzung“.
- **Selbstfortsetzung:** Bleiben Teile offen, legt der Lauf vor dem Ende eine einmalige geplante Aufgabe an (Ablauf und
  Prompt-Format in `spur-start.md`). **Höchstens drei Fortsetzungen je Spur**; danach oder bei einem blockierten Teil
  entscheidet der Nutzer (PR-Label `spur-wartet`). Kein Lauf startet eine fremde Spur. Die Erlaubnis steht im Startaufruf
  der Leitstelle.
- Ein Teil darf nur mit grünem `tools/pruefen.sh --schnell` übergeben werden. Werden `BoerseStartTests`/`FinanceWerbungTests`
  durch geänderten Zufallsverbrauch rot, nennt die Spur das im PR (blockiert nicht); diese Dateien ändert nur die Leitstelle.
- Die Leitstelle integriert **je Spur am Ende** (alle Teile), nicht nach jedem Teil. Integrationsfolge: `finanzen-rest` →
  `wettbewerbe-komplett` → `optionen-wirkung` (kleine Abfragen in `Season.cs`/`SeasonFlow.cs` nach den großen Änderungen)
  → `transfer-vertraege-komplett` → `spielberechnung-komplett` zuletzt.

### Zuständigkeit für gemeinsame Dateien

- `Core/MatchEngine*.cs`, `Core/Squad.Strength.cs`, `Core/Squad.Tactics.cs`, `Core/StadiumStatistics.cs`,
  `Core/StatisticsStrength.cs`, `Core/StatisticsClub.cs`, `Core/StatisticsPlayers.cs`, `UI/Screens/Statistics/*` außer
  `HighscoreScreen.cs`: allein `spielberechnung-komplett`.
- `Core/Transfer*.cs`, `Core/Player.Contract.cs`, `Core/Player.Value.cs`, `UI/Screens/Team/TransferScreen.cs`,
  `TransferDialog.cs`, `ContractsScreen.cs`, `UI/Screens/SeasonEnd*.cs`: allein `transfer-vertraege-komplett`.
- `Core/Season.cs`, `Core/Playoffs.cs`, `Core/CupsGroup.cs`, `Core/Cups*.cs` außer `CupsNationalMode.cs`, `Core/SeasonFlow.cs`,
  `Core/SeasonRecord.cs`, `Core/StatisticsHistory.cs`, `UI/Screens/TableScreen.cs`, `UI/Screens/Competitions/*`: allein
  `wettbewerbe-komplett`. **Ausnahme:** `optionen-wirkung` darf in `SeasonFlow.cs` nur einfügen (ohne Signaturänderung, im PR
  benannt): An-/Abmelden der Pokale nach den Spielstand-Schaltern in `StartNextSeason` (vor `StartSeason`, Reihenfolge
  DEB-Pokal → Europapokal → WM) und den Moduswechsel; **keine** Filter in `MatchOn`/`ProcessDay` (#289). `Season.cs` (statische Tagestabelle) und `Cups*.cs` ändert sie nicht; wäre das
  nötig, gilt der Teil als blockiert (`spur-start.md`, Teile und Fortsetzung, Schritt 4).
- `Core/CupsNationalMode.cs`, `Core/GameSetup.cs`, neue Datei `Core/GameOptions.cs`, `UI/Screens/SettingsScreen.cs`,
  `Settings.cs`, `CalendarScreen.cs`, `SetupScreen.cs`: allein `optionen-wirkung`.
- `Core/FinanceYouth.cs`, `Core/FinanceSponsors.cs`, `Core/FinanceStocks.cs`, `UI/Screens/Manager/*`,
  `UI/Screens/SaveLoad/*`: allein `finanzen-rest`.
- `Core/GameState.Integration.cs`, `MainMenuScreen.cs`, `MenuTiles.cs`, `SaveGame.cs` (außer vorläufiger Schemaeintrag):
  nur minimal und begründet; Anschlüsse in fremde Dateien nennt die Spur im PR, die Leitstelle setzt sie in der Integration.
- Referenz- und Mehrsaisontests: Sollwerte nur von Spuren ändern, die Ergebnisse, Kader, Geld oder Zufallsverbrauch
  verändern, je Fall begründet; die Leitstelle ermittelt sie auf dem integrierten Stand neu. Neue Tests in eigenen Dateien.

### Spur `spielberechnung-komplett` – Spielberechnung abschließen (#204)

- **Teile (in dieser Reihenfolge):**
  1. **Spielstärke der Managervereine gegen das Original** (R8W3I-15, #204): Messung Spielerwerte ↔ Datenbankstärke gegen
     Videos (Reihen `folgen`/`karriere`, Saisonenden) und DOSBox-Befunde; Ursache der Meisterdominanz bzw. -streuung im
     Original (Play-off-Format, Schreibzugriffe 0x570E/0x49723). Ergebnis: belegte Abbildung oder Befund; danach
     `PlayAtDatabaseStrength` in Tests nur noch, wo begründet.
  2. **Strafen in der Managerpaarung** (R8W3I-03 bis -05): `PenalizedPlayer`, Rückgabe 100, Managerwert für Strafen;
     **Strafzüge 0x378C5 für das Nationalteam** in `LiveTeamOf` (R8W3I-06, ändert den Zufallsverbrauch der Länderspiele);
     Verdacht 0x952C → 0x9567 (automatische Aufstellung vor den Reihenwerten, `[APPROX]` in `PlayFriendly`) klären;
     Test `Forfeit_LeagueMatch_UpdatesDayForm` (Teil von R8W3I-10).
  3. **Verletzungen im Spiel** (0x38B8F, Ereignis 4 in 0x130:0x7F) und **Torschütze** 0x386D1.
  4. **Über-/Unterzahl-Zähler** an die Minutenrechnung (aus `statistiken-verein`), **Länderwerte (#193) im Stärkevergleich**.
  5. **Penaltyschießen** nach Verlängerung 0x3901B (`penalty-original`).
- **Abnahme:** je Teil roter Test mit Fundstelle, danach grün; Saisonverteilung (Meister ≥ 8 Saisons, Tore, Strafminuten)
  gegen Videos im PR; Mehrsaisonprobe Modus 1, 2, 4 passiv/aktiv; Referenzwerte begründet.
- **Dateigrenzen:** siehe Zuständigkeit; `docs/spielberechnung.md`, `docs/dosbox-befunde.md` (eigener Abschnitt), Tests.
- **Modell:** Claude Opus 5.5. **Status:** Teile 1–5 fertig (#295 → #302 → #309 → #311 → #312); Integration zuletzt.

### Spur `transfer-vertraege-komplett` – Transfers und Verträge abschließen

- **Teile:**
  1. **Kauf mit anschließender Verhandlung** 0x4786A und **Verhandlung am Saisonende** 0x27D05 (Erreichbarkeit im UI klären).
  2. **Start-/Wechselgehalt** = Wert(Gehalt), **Versicherung** (Prämie, Dialog R8W3I-07, Fundstelle 0x07089),
     **Gehalt in ganzen DM** (R8W3I-08), **Sperre beim Saisonwechsel** (R8W3I-09).
  3. **Unaufgeforderte Angebote** (+0x09 Bit 0x40), **Listenwechsel** 0x494EF, Knopfdarstellung.
  4. **Testlücken** R8W3I-10 (ohne `Forfeit_LeagueMatch_UpdatesDayForm` → spielberechnung Teil 2 und Menüweg Modus 1 →
     wettbewerbe Teil 3), R8W3I-16 (Level 5, gleiche Laufzeit, altes Gehalt), SW-01; W51-02 (Aussage „hängt nur von Gebot und
     Listenpreis ab“ in der Doku präzisieren); Pixelvergleich Transferliste und Verhandlungsfenster mit Kennzahl.
- **Abnahme:** je Regel roter Test, danach grün; Goldtest aus FILE03/05/07 erweitert; Mehrsaisonprobe (Kader, Geld);
  Bildschirme zustandsrein.
- **Dateigrenzen:** siehe Zuständigkeit; `docs/transfer.md`, Tests.
- **Folgeteil (Entscheidungen Nutzer 01.11.2026, eigener Lauf ohne Fortsetzung, Go 01.11.2026):**
  5. **Listenwechsel 0x494EF auf das Original umbauen:** Pool von 200 Spielern (Computervereine ohne Spieler) statt Zug aus den
     Kadern der Computervereine (Beschreibung `docs/transfer.md`, #308); **Ablöse bei Markt-Angeboten** (Bit 0x80) nach 0x49332:
     gutgeschrieben wird Angebot − Angebot / 7 · Restlaufzeit statt des vollen Angebots. Geänderte Referenzwerte je Fall
     begründen; Mehrsaisonprobe (Kader, Geld). Branch `paket/transfer-vertraege-komplett-5`, Base `paket/transfer-vertraege-komplett-4`
     (#310), solange #310 offen ist.
- **Modell:** Claude Opus 5.5. **Status:** Teile 1–4 fertig (#294 → #301 → #305 → #308 → #310); Teil 5 läuft.

### Spur `wettbewerbe-komplett` – Tabellen, Pokale, Nationalteam abschließen

- **Teile:**
  1. **Tabellen:** Startordnung ab Saison 2, Abzug in der Titelzahl 0x4F986, Pixelvergleich Tabellen- und Gruppenansicht;
     Verdacht Tore modulo 30 in 0x30852 (0x30906/0x30916) gegen die Gruppentabellen des Remakes prüfen.
  2. **Nationalteam:** Gegner-Fenster 0x128:0x2A (Klick auf Flaggen), Schreibstelle der WM-Platzierungen (DGROUP 0x8257 +
     0x34); Test für den Grenzwert 10 der Bundestrainer-Warnung (R8-D02; `MainMenuScreen.cs` nur bei rotem Test, minimal).
  3. **Mehrsaisonproben robust:** Mode4-Probe mit schrumpfendem Kader (Manager 0 nicht spielfähig) und weitere Proben, deren
     Vorbedingung am Zufall hängt – ohne Seed-Wechsel (wie R8-A02); Menüweg Modus 1 in `WorldOnlyMenuTests` (Teil von
     R8W3I-10).
  4. **#166 Oberliga-Austausch** nur, wenn der Austausch der Oberliga samt Managervereinen belegt wird; sonst Beleglage im PR.
- **Abnahme:** je Teil roter Test bzw. Pixelvergleich mit Kennzahl; Mehrsaisonprobe Modus 1, 2, 4; Bildschirme zustandsrein.
- **Dateigrenzen:** siehe Zuständigkeit; `docs/saison.md`, `docs/wettbewerbe.md` (eigene Abschnitte), Tests.
- **Modell:** Claude Opus 5.5. **Status:** Teile 1–4 fertig (#293 → #299 → #306 → #307; Teil 4 nur Beleglage, #166 → Runde 10).

### Spur `optionen-wirkung` – Spielstand-Optionen, Einstellungen, Kalender

- **Teile:**
  1. **Optionen mit Spielwirkung** aus 0x2CD1C in Core (Zustand in `Core/GameOptions.cs` samt Migration; Index 9
     Moduswechsel, Index 10–12 DEB-Pokal, Europapokal, WM-Spiele) – auf den Belegen von `optionen-kalender`
     (`docs/reverse-engineering.md`). Die Wirkung wird in `SeasonFlow.cs` angeschlossen (Ausnahme unter „Zuständigkeit“) und
     gilt **ab dem nächsten Saisonstart** (`[APPROX]`, solange 0x2CD1C nichts anderes belegt; kein halber Pokal mitten in
     der Saison). Zuerst R8-D03 klären (Anfangswert Index 9 vs. 10, `reverse-engineering.md` „Index 9 = 1“).
  2. **Einstellungsbildschirm** nach Original-Layout (Bild 48) mit Pixelvergleich, Optionen schaltbar statt nur lesend.
  3. **Kalender** 0x43E21/0x43B1F/0x43C38/0x36169 – Bildschirm nur mit Videobeleg; 0x445E0 (Infoleiste „Vor dem ersten
     Spieltag.“) ist im Remake vorhanden und wird nur abgeglichen.
- **Abnahme:** roter Test je Option mit Wirkung; Mehrsaisonprobe mit Moduswechsel; Pixelvergleich; Bildschirme zustandsrein.
- **Dateigrenzen:** siehe Zuständigkeit; `docs/tagesablauf.md`, Tests.
- **Folgeteil (Entscheidung Nutzer 01.11.2026, eigener Lauf ohne Fortsetzung, Go 01.11.2026):**
  4. **Torszenen-Schalter zusammenlegen:** Geräteeinstellung „Torszenen zeigen“ (Remake) und Original-Schalter im Spielstand
     (`GameOptions.LiveMode`, #298) werden ein Schalter; Optionsbildschirm des Originals bleibt die Bedienstelle, die
     Geräteeinstellung entfällt bzw. wird daraus abgeleitet; Migration alter Spielstände/Einstellungen; Pixelvergleich Bild
     48–51 unverändert. Branch `paket/optionen-wirkung-4`, Base `paket/optionen-wirkung-3` (#303), solange #303 offen ist.
- **Modell:** Claude Opus 5.5 (Core-Logik), Teil 4 Claude Sonnet 5.5. **Status:** Teile 1–3 fertig (#292 → #298 → #303);
  Teil 4 läuft.

### Spur `finanzen-rest` – Restliche Finanz- und Managerbildschirme

- **Teile:**
  1. **Jugendteams** 0x3A24B (Funktionskarte) gegen `FinanceYouth.cs`/`YouthScreen.cs`.
  2. **Bandenwerbung** 0x2A9AD gegen `FinanceSponsors.cs`/`AdvertisingScreen.cs`.
  3. **#196 Regler Ausschnitt/Maßstab des Kursverlaufs** (`FinanceScreen.cs`).
  4. **Dateidialog** 0x5DBE5 (`SaveLoadScreen.cs`) gegen das Original.
- **Abnahme:** je Teil Zuordnung mit Fundstelle, roter Test, Pixelvergleich mit Kennzahl; Mehrsaisonprobe bei Geld.
- **Dateigrenzen:** siehe Zuständigkeit; `docs/finanzen-rekonstruktion.md` (eigener Abschnitt), Tests.
- **Modell:** Claude Sonnet 5.5. **Status:** Teile 1–4 fertig (#291 → #300 → #304 → #313); Integration läuft.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- `livespiel` (Torszenen, Anzeigetafel, Reporter; Nr. 14/16): letzter Schritt vor v1.0 (Entscheidung Nutzer).
- Runde 10: Sonderrunde Analyse der unbekannten Teile von em.exe (Wunsch Nutzer 01.11.2026).
- Nach Runde 10 (Runde 11, Entscheidung Nutzer 01.11.2026): P3-Befunde der Integration Runde 9 – #327, #329, #342, #344,
  #347, #348, #354; Robustheit der Mehrsaisonproben (#352, #353).
- #349 Spieltag mit Managerverein ganz in der Minutenrechnung: Remake-Verhalten bleibt; Original als Einstellung nach v1.0
  (`docs/anforderungen/zusatzfunktionen.md`, Abschnitt C).
- #166 Saison eines Managers ohne Liga bzw. Oberliga-Austausch samt Managervereinen (Entscheidung Nutzer 01.11.2026, #307):
  nicht in Runde 9, `[APPROX]` bleibt; in Runde 10 analysieren und mit einem `hok-patch`-Spielstand in DOSBox belegen.
- Import/Export von Original-Spielständen ins Remake; Highscore am Spielende und Saisonende im Modus „nur Weltkugel“
  (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.
- Entfallen bzw. erledigt (R9P-03): R8-D04 (Sperre in „Minuten“, akzeptiertes `[APPROX]`), R8-D05 (Dateigrenzen ohne
  Begründung in Runde 8 – Regel 2.5 gilt weiter, keine Nacharbeit), R8-A03 (in #263), W51-03 (Leerzeile, im Plan-PR #275 behoben).

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
