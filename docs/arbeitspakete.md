# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [CLAUDE.md](../CLAUDE.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.

**Koordination:** Der Haupt-Chat verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku.

## Aktuelle Runde: Runde 3 – Kader, Wettbewerbe, Finanzen (ab 26.10.2026)

Basis: `main` nach Integration Runde 2 (PR #35 und Nachprüfung #37). Regeln, Pflichtangaben und der Review-Ablauf
mit ChatGPT gelten weiter (siehe Runde 2). **Neu verbindlich:** Jede Spur, die Saisonwechsel, Kader oder Geld berührt,
liefert eine **Mehrsaisonprobe** (mindestens 4 Saisonwechsel, passiver und aktiver Manager) als Test.

```
Welle A (parallel):  kader   wettbewerbe   finanzen
Welle B:             speichern (endgültiges Schema, nachdem Welle A die Daten festgelegt hat)
```

| Spur | Branch | Befunde | Inhalt | Verbindliche Abnahme (Regressionstest) |
|---|---|---|---|---|
| **kader** | `paket/kader` | R2-11 (P1), R2-12 (P2) | Vertragsende ankündigen (Posteingang/Vertragsbildschirm, Texte aus em.exe suchen) und Verlängerung ermöglichen; Kader-KI der Computervereine: Verlängerung, Neuzugänge aus dem Pool und aus Talenten, Ruhestand; ersetzt die `[APPROX]`-Überbrückung aus Welle B und begrenzt den Pool | Passiver Manager wird vor Ablauf gewarnt; nach 6 Saisonwechseln hat jeder Computerverein einen spielfähigen Kader (Mindestbesetzung je Position) mit plausibler Altersverteilung; Pool begrenzt; kein Spieler verloren oder doppelt |
| **wettbewerbe** | `paket/wettbewerbe` | – | wie im Paketabschnitt unten: DEB-Pokal, Europapokal, WM, Freundschaftsspiele, Stärken- und Statistikbildschirme; Anschluss über `ISeasonCompetition`, Heimspiele über die gemeinsame Spielabrechnung | Jeder Wettbewerb läuft über `Continue()` vollständig durch; jedes Heimspiel genau einmal abgerechnet; Bildschirme zustandsrein; Pixelvergleich mit den Referenzbildern |
| **finanzen** | `paket/finanzen` | Kalibrierung | Prämien, Zuschauer, Werbung, Stadion mit dem 75-Minuten-Video und em.exe abgleichen (Hinweis unten: passiver Oberliga-Manager deutlich im Plus) | Mehrsaisonprobe Oberliga und Bundesliga: Kontoverlauf passiver Manager innerhalb einer im PR begründeten Spanne; jede belegte Formel `[ORIGINAL]`, Rest `[APPROX]` |

**Dateigrenzen in Welle A:**
`kader` → `Core/Player.Contract.cs`, `Core/Player.Factory.cs`, `Core/Transfer*.cs`, `Core/FinanceInbox.cs`, Verträge-Bildschirm ·
`wettbewerbe` → `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/**`, `UI/Screens/Statistics/**`, Anmeldung in `SeasonFlow.cs`/`GameState.Integration.cs` nur mit Begründung ·
`finanzen` → `Core/Finance*.cs` außer `FinanceInbox.cs`, Prämien in `SeasonFlow.cs`, `UI/Screens/Finance/**`.
Berührungen außerhalb der eigenen Grenzen im PR begründen.

### Status Spur `kader`

Stand: implementiert, lokale Abnahme grün; Review und Merge stehen aus (Branch `paket/kader`).

- [x] **Belege aus Original und Video:** em.exe enthält die Posteingangstexte „$ bietet Ihnen an, $^Jahre zu verlängern.“,
  „$ kündigt an, daß er seinen Vertrag^nicht mehr verlängern wird.“, „$ gibt das Eishockeyspielen im Alter von^# Jahren
  endgültig auf.“ und die Verhandlungsmeldungen „ist im Moment / nicht verhandlungs- / bereit.“, „wird sich mit /
  Ablauf des Vertrages / zur Ruhe setzen.“. Im 75-Minuten-Video erscheinen 16 Ankündigungen von Spielern im letzten
  Vertragsjahr, verteilt vom 13. September bis 30. März (15 Angebote: 12 × „von 1 auf 2“, 2 × „auf 3“, 1 × „auf 4“; 1 Absage); verlängert wird im
  Bildschirm „Verträge“. Die Vertragsanzeige sinkt beim Saisonwechsel um ein Jahr (14.4. „2 Jahre“ → 10.9. „1 Jahr“).
- [x] **R2-11:** Jeder Spieler eines Manager-Vereins im letzten Vertragsjahr meldet sich genau einmal je Saison im
  Posteingang, spätestens am 30. März (`GameState.ContractNoticeDeadline`): Angebot „von 1 auf n Jahre“, Absage oder
  (ab 33) Ruhestand. Absage/Ruhestand sperren die Verhandlung mit den Originalmeldungen; nach einem Angebot verlängert
  der Manager im Bildschirm „Verträge“. Ruheständler verlassen den Kader mit „… gibt das Eishockeyspielen … auf.“
- [x] **R2-12:** Kader-KI der Computervereine beim Saisonwechsel ersetzt die Überbrückung „verlängern immer“:
  Ruhestand nach Alter (33 → 20 % … 37 → 100 %), auslaufende Verträge zu 75 % um 1–3 Jahre verlängert, sonst in den Pool;
  Auffüllen je Position auf mindestens 2/8/10 und höchstens die Größe vor dem Wechsel (max. 25) aus dem Pool (bis 32 Jahre,
  passend zur Vereinsstärke) oder mit Talenten aus der Spielerfabrik; Pool höchstens 60 (`TransferMarket.MaxFreeAgents`),
  Überzählige (die ältesten) beenden die Laufbahn. Laufbahnenden in `GameState.RetiredPlayers`. Eigene Zufallsquelle
  `ContractRandom`, damit Spielergebnisse, Markt und Finanzen ihre Folgen behalten.
- [x] Mehrsaisonprobe `SquadLifecycleTests` (6 Wechsel, passiv und aktiv, zwei synthetische Seeds und Originaldaten Verein 42):
  Mindestbesetzung je Computerkader, keine Spieler verloren oder doppelt (Kader + Pool + Laufbahnende), Pool begrenzt,
  Alter 17–36, Ø 23–29. Originaldaten passiv nach 6 Wechseln: 1.322 Computerspieler, Alter 17–36, Ø 26,1 (vorher 25–37, Ø 30,9),
  446 Neuzugänge, 408 Laufbahnenden, Pool 49.
- Offen/Folgearbeit: Spur `speichern` muss `Notice`, `NoticeYears`, `RetiredPlayers` und den Zustand von `ContractRandom` sichern.
  Passive Manager verlieren den Kader weiterhin (jetzt angekündigt) – das entspricht dem Video, ist aber spielerisch hart.
  Nachrichtenleiste (`NewsBar.cs`, nicht Spur `kader`): Das Video zeigt Kopfzeile = Datum, darunter zwei Textzeilen.

## Runde 2 – Stabilisierung (abgeschlossen am 26.10.2026)

Ergebnis: Welle A gemergt mit #31 (Review [pr31](reviews/2026-10-26-pr31-review.md)), Welle B mit #35
(Review [pr35](reviews/2026-10-26-pr35-review.md), Nachprüfung [pr35-nachpruefung](reviews/2026-10-26-pr35-nachpruefung.md):
Freigabe für `9bd8fea`). Der P3-Befund PR35-N01 (UI-Test prüfte die Meldung nicht) ist nach dem Merge behoben.
Offen in Runde 3: R2-11, R2-12.

### Ursprünglicher Plan Runde 2 (ab 26.10.2026)

Grundlage: unabhängiges Folgereview [reviews/2026-10-26-folgereview.md](reviews/2026-10-26-folgereview.md) zu Commit `e1ac97e`.
Die Befunde R2-02, R2-06, R2-07, die verkürzten Stärkewerte (A01) und die Levelgrenze wurden vom Koordinations-Chat
mit den Originaldaten nachvollzogen. **Erst stabilisieren, dann ausbauen:** `wettbewerbe` und das endgültige
Speicherformat starten erst nach dieser Runde.

### Spuren und Reihenfolge

```
Welle A (parallel):  fundament ─┐   wirtschaft ─┐   mehrmanager   sound (unabhängig)
                                │               ▼
Welle B:                        └──────────► lebenszyklus   (braucht die Spielerfabrik aus wirtschaft)
Danach (Runde 3):    wettbewerbe, speichern, Kalibrierung Finanzen
```

| Spur | Branch | Befunde | Inhalt | Verbindliche Abnahme (Regressionstest) |
|---|---|---|---|---|
| **fundament** | `paket/fundament` | R2-02, R2-05, A07, A08, A09 | UI-Einstiege schreiben keine Zeit fort (alle `CatchUp…`-Aufrufe aus Bildschirmen entfernen); Tagesablauf festlegen (Reihenfolge „Plan ändern → Training“); Originaldaten-Tests als echtes **Skip** (z. B. `Xunit.SkippableFact`), Pflichtlauf `EM_REQUIRE_GAMEDATA=1` scheitert ohne Daten; CI-Bericht getrennt nach synthetisch / Originaldaten / übersprungen; `ScreenDriver.PlayMatchDay()` wirft bei Zeitüberschreitung; Kacheltest unterscheidet Platzhalter und echte Bildschirme; `global.json` und feste Paketversionen | Bei pausiertem Tag alle Ansichten öffnen/schließen → Geld, Spielerwerte, Markt, Zufallszustand unverändert; danach jeder Tag genau einmal verarbeitet. CI zeigt Skips sichtbar. |
| **wirtschaft** | `paket/wirtschaft` | R2-01, R2-03, Restpunkte | Gehälter und Versicherung aus den echten Verträgen (`Contracts.MonthlyCosts`), Ersatzformel nur ausdrücklich begrenzt; **eine** Spielerfabrik für Spielstart, Transfermarkt und Jugend; **eine** Talentsuche (Jugend-„Spion“ und Transfer-Knopf rufen dieselbe Logik); Saisonprämien über `Book` mit eigener Kategorie; Heimspiele in Play-Offs/Aufstiegsrunde abrechnen (gemeinsame Spielabrechnung) | Vertrag ändern → über `Continue()` nächsten Monat erreichen → genau die neuen Gehälter/Versicherungen im Finanzbuch. Jeder Zugangspfad liefert Spieler mit Alter, eindeutiger Rückennummer, gültigen Werten und Vertrag. Jede Zahlung genau einmal im Finanzbuch. |
| **mehrmanager** | `paket/mehrmanager` | R2-06, R2-07, A01, Level | Hot-Seat: Bereitschaftsphase (Lampen), jeder aktive Manager bekommt seine Runde, Tag wird erst nach Bestätigung aller genau einmal simuliert, Entlassene ausgeschlossen; exklusive Vereinsvergabe (feste Wahlen zuerst reservieren, dann Zufall); alle drei Stärkewerte der Datenbank erhalten (Bedeutung getrennt dokumentieren); Setup-Level nach Originalbytes `5, 3, 2, 2` (DGROUP 0x11C) prüfen und Levelumfang festlegen | 2–4 Manager: jeder kommt je Tag genau einmal dran, Simulation genau einmal; Tests mit gemischten Wahlarten, mehreren Reihenfolgen und Seeds → jeder Manager exklusiver Verein; Stärketripel vollständig. |
| **lebenszyklus** | `paket/lebenszyklus` | R2-04, R2-08, R2-09 | Saisonwechsel als gemeinsame Transaktion: Altern, Vertragslaufzeit/-ende, Leihende (ursprünglichen Verein speichern), offene Angebote, Aufstellung, Markt; Fristen über Saisongrenzen (Saison + Tag); Transfergebote nur einmal wirksam (Gültigkeit, Eigentum, offener Zustand) | Saisonwechsel aktualisiert den Kader vollständig; Anfrage kurz vor dem Wechsel ist nach Fristende nicht mehr annehmbar; zweimal angenommenes Gebot hat keine zweite Wirkung, zurückgezogenes Listing wird abgelehnt. |
| **sound** | `paket/sound` | – | wie unten beschrieben; darf keine Dateien der Stabilisierungsspuren ändern | siehe unten |

**Dateigrenzen in Welle A** (damit sich die Spuren nicht überschneiden):
`fundament` → `UI/Screens/**` (nur CatchUp-Aufrufe), `UI/Testing/**`, `tests/EishockeyManager.UI.Tests/**`, `.github/**`, `global.json`, `*.csproj` (Versionen) ·
`wirtschaft` → `Core/Finance*.cs`, `Core/GameState.Manager.cs`, `Core/Player*.cs` (Fabrik), `Core/Transfer.Talent.cs`, Prämie in `SeasonFlow.cs` ·
`mehrmanager` → `Core/NewGame.cs`, `Core/GameSetup.cs`, `Formats/ClubDatabase.cs`, `UI/Screens/SetupScreen.cs`, `UI/Screens/MainMenuScreen.cs` (Zugwechsel), `Continue()`-Einstieg in `SeasonFlow.cs`.
Berührungen außerhalb der eigenen Grenzen im PR begründen. `GameState.Integration.cs` ändert nur der Koordinations-Chat oder die Spur mit Absprache im PR.

### Status Spur `fundament`

- [x] PR 1 – Testgrundlagen (PR offen) (R2-05, A07, A08): Originaldaten-Tests mit `[GameDataFact]` (echtes Skip ohne Daten,
  Pflichtlauf `EM_REQUIRE_GAMEDATA=1` scheitert), Trait `Category=Originaldaten`, CI-Bericht getrennt nach
  synthetisch / Originaldaten / übersprungen (`.github/scripts/testbericht.py`), `ScreenDriver.PlayMatchDay()` wirft
  `TimeoutException`, Kacheltest trennt Platzhalter und echte Bildschirme (Liste `MenuEntryTests.DocumentedPlaceholders`)
- [x] PR 2 – Passive Bildschirme und Tagesablauf (R2-02, PR offen): alle `CatchUp…`-Aufrufe aus den Bildschirmen entfernt,
  Tagesablauf in [tagesablauf.md](tagesablauf.md), Abnahmetests `PassiveScreenTests` (Zustandsabdruck, Zwillingsspiel).
  **Neuer Folgebefund R2-02b** (Spur `wirtschaft`): erstes Öffnen von „Werbung“ verbraucht den Finanz-Zufall –
  strenger Test liegt sichtbar übersprungen bereit
- [ ] PR 3 – Werkzeugversionen festschreiben (A09)

### Pflichtangaben in jedem PR dieser Runde

1. Base- und Head-SHA
2. Reproduzierter Fehler **vorher** (Befundnummer, kurzer Ablauf, beobachteter Wert)
3. Regressionstest (Name, was er prüft) – muss vor dem Fix rot gewesen sein
4. Tatsächlich ausgeführte Tests: Anzahl synthetisch / mit Originaldaten / übersprungen
5. Restlücken und neue `[APPROX]`-Stellen

### Ablauf je PR

Spur-Chat liefert PR → Koordinations-Chat prüft gegen `main` und die anderen offenen PRs (Integrationstest mit Originaldaten)
→ **unabhängiges Review (ChatGPT) mit festem Base-/Head-SHA**: zuerst die reproduzierten Gegenbeispiele erneut ausführen,
dann Aufrufer und neue Tests prüfen → Merge durch den Koordinations-Chat. Review-Ergebnisse unter `docs/reviews/`.

**Review durch ChatGPT:** Der Koordinations-Chat formuliert die Review-Anfrage immer als fertigen, kopierbaren Text
(PR, Base-/Head-SHA, Gegenbeispiele, erwartet offene Befunde, gewünschte Bewertung). ChatGPT legt das Ergebnis als
Markdown-Datei `docs/reviews/JJJJ-MM-TT-<thema>.md` auf einem eigenen Branch `review/<thema>` ab und öffnet dazu
einen PR, der **nur diese Datei** enthält. Ohne Schreibzugriff stellt ChatGPT die Datei zum Herunterladen bereit,
dann legt der Koordinations-Chat sie ab. Der Review-PR wird zusammen mit dem geprüften PR gemergt.

### Grundsätze aus dem Review

- Keine Finanz-Neukalibrierung, bevor Gehälter, Versicherungen, Prämien und Nachsaisonspiele korrekt gebucht werden.
- „Geld über `Book`“ gilt für echte Einnahmen und Ausgaben. Neutrale Umbuchungen (Bank ein/aus, Sparbuch, Aktienkauf/-verkauf
  zum Kurswert, Kredite zwischen Managern) bleiben getrennt und werden nicht als Aufwand gebucht.
- Testanzahl ist keine Abnahme. Pixelparität und Balance werden getrennt abgenommen.

### Spur `wirtschaft` – Status

Branches je PR: `paket/wirtschaft-<punkt>` (Spurbranch `paket/wirtschaft`). Reihenfolge laut Auftrag; PR 2 baut auf PR 1 auf.

| PR | Inhalt | Status |
|---|---|---|
| 1 – Spielerfabrik | `Core/Player.Factory.cs`: eine Erzeugung für Spielstart, Transfermarkt, Talentsuche (Transfer und Jugend); `PlayerFactory.Join` (Nummer, Vertrag) und `Validate` (Invarianten) für `lebenszyklus` | im Review (#16) |
| 2 – eine Talentsuche | Jugend-„Spion“ und Transfer-Knopf rufen `GameState.StartTalentSearch` (Preis 90.000 DM laut Video − Rabatt, 4 Wochen, Ergebnis als Nachricht, neues Talent aus der Fabrik); offene Entscheidungen im PR | im Review (#17, nach #16) |
| 3 – Gehälter/Versicherung aus Verträgen | Vertragsgehälter, Versicherung getrennt gebucht; Pauschale nur für Vereine ohne Kader | im Review (#19) |
| 4 – Prämien, Nachsaison-Heimspiele | Kategorie „Prämien“ über `Book`; `SettleHomeMatch` für Liga, Play-Offs, Aufstiegsrunde | im Review (#21) |

Hinweis für Runde 3 (Kalibrierung): Mit #19 und #21 steigt der Kontostand eines passiven Oberliga-Managers deutlich
(Originaldaten, SC RIESSERSEE, eine Saison: 109.460 → 579.160 DM, Gehälter −666.400 → −196.700 DM).

### Status Spur `mehrmanager`

| Punkt | PR | Stand |
|---|---|---|
| R2-07 exklusive Vereinsvergabe | #18 | offen, wartet auf Review |
| R2-06 Hot-Seat (Bereitschaftsphase, Lampen, Tag genau einmal) | #20 | offen, wartet auf Review |
| A01 drei Stärkewerte je Verein | #22 | offen, wartet auf Review |
| Levelgrenze: Level 0–5, Regler begrenzt statt Umlauf (DGROUP 0x11C, 0x4B3E) | siehe PR „Setup-Level“ | offen, wartet auf Review |

Neue Befunde aus dem Disassembly (Details in [reverse-engineering.md](reverse-engineering.md#spiel-level)):
Kadergröße der Manager-Vereine hängt im Original an der **Start-Liga**, nicht am Level; Level, Start-Liga und
Start-Stärke sind im Original **globale** Setup-Werte (Remake: je Manager); Regel „eine Saison nur mit Start-Liga 0“
ist noch nicht übernommen.

---

### Integration Runde 2, Welle A (26.10.2026)

PRs #16–#30 (fundament, wirtschaft, mehrmanager, sound) probeweise zusammengeführt: ein Textkonflikt in diesem Dokument,
sonst konfliktfrei. Build ohne Warnungen; mit `EM_REQUIRE_GAMEDATA=1` und Originaldaten 327 Tests bestanden, 1 sichtbar
übersprungen (R2-02b, siehe unten); alle 16 Bildschirmfoto-Abläufe laufen durch.

**Integrationsfehler (behoben im Integrations-Commit):** Die Tests aus `mehrmanager` und `sound` waren gegen den alten
Stand geschrieben und nutzten noch das Muster „ohne Daten still `return`“ bzw. `FlowTests.CreateDriver()`, das seit
`fundament` ohne Daten scheitert. Folge: CI-Schritt „Test (synthetisch)“ rot, und 16 Originaldaten-Tests wären ohne
Daten wieder still „bestanden“ (R2-05). Alle auf `[GameDataFact]` / neues `[GameDataTheory]` umgestellt.
Ohne Daten jetzt: 281 bestanden, 42 sichtbar übersprungen; Pflichtlauf ohne Daten scheitert (46 Fälle); mit Daten 327 bestanden, 1 übersprungen (R2-02b).
**Regel:** Tests mit Originaldaten nur noch mit `[GameDataFact]`/`[GameDataTheory]` und `TestGameData.Load()`.

Befunde im Gesamtstand vom Koordinations-Chat nachgestellt (eigene Probe gegen die gebauten Assemblies):

| Befund | Ergebnis im Gesamtstand |
|---|---|
| R2-01 Gehälter | behoben – Gehälter und Versicherung im Monat entsprechen genau der Vertragssumme nach Gehaltsänderung |
| R2-02 Ansichten | behoben – Bildschirme schreiben keine Zeit mehr fort; **R2-02b offen**: erstes Öffnen von „Werbung“ würfelt Angebote aus (FinanceSponsors.cs) |
| R2-03 Talente | behoben – nach Talentsuche und 40 Weiter-Schritten kein ungültiger Spieler in irgendeinem Kader |
| R2-05 Tests | behoben – Originaldaten-Tests werden ohne Daten sichtbar übersprungen, Pflichtlauf scheitert ohne Daten |
| R2-06 Hot-Seat | behoben – zwei Manager wechseln sich ab, je Runde genau ein Spieltag |
| R2-07 Vereine | behoben – 200 Seeds mit Zufall + fester Wahl: keine Doppelbelegung |
| A01 Stärketripel | behoben – alle drei Werte erhalten (Bedeutung weiter offen) |
| Level | umgesetzt – Level 0–5 (LevelCount 6) |
| R2-04, R2-08, R2-09 | erwartungsgemäß offen → Welle B (`lebenszyklus`) |

**Review und Merge:** ChatGPT-Review [reviews/2026-10-26-pr31-review.md](reviews/2026-10-26-pr31-review.md) am Head
`9bddf6f`: **Freigabe für Welle A**, keine neuen P0–P2-Befunde. Gemergt als `fce0760` (PR #31, damit auch #16–#30).
Neu bzw. präzisiert:

| Befund | Inhalt | Zuständig |
|---|---|---|
| R2-02b | Erstes Öffnen von „Werbung“ würfelt Angebote aus, die Folgeentwicklung weicht ab | `ansichten` |
| R2-02c | Weitere Initialisierungen beim Öffnen: „Finanzen“ erzeugt den Aktienmarkt, „Training“ den Standardplan (Folgezustand nach 8 Schritten gleich, aber nicht zustandsrein) | `ansichten` |
| R2-10 (P3) | `SeasonPaymentTests` (Heimspiel „genau einmal“) zählt nur positive Schritte je `Continue()`; eine zweite `SettleHomeMatch()`-Buchung derselben Begegnung (+60.170 DM) bliebe unentdeckt | `ansichten` |
| R2-09 präzisiert | `PlayerFactory.Join()` verhindert die doppelte Spielerreferenz, aber zweimaliges Annehmen bucht weiterhin 400.000 → 604.000 → 808.000 DM | `lebenszyklus` |

## Runde 2, Welle B (ab 26.10.2026)

Zwei Spuren parallel, Basis `main` = `fce0760`:

| Spur | Branch | Befunde | Inhalt | Verbindliche Abnahme (Regressionstest) |
|---|---|---|---|---|
| **lebenszyklus** | `paket/lebenszyklus` | R2-04, R2-08, R2-09 | wie in der Tabelle oben; zusätzlich die Aufstellungsbereinigung beim Öffnen in den Saisonwechsel/Tagesablauf verlegen | Spieler altert, Vertrag läuft ab bzw. verkürzt sich, Leihe endet beim ursprünglichen Verein; Anfrage von Tag 231 ist in der Folgesaison nicht mehr annehmbar; zweites Annehmen ändert weder Kontostand noch Kader |
| **ansichten** | `paket/ansichten` | R2-02b, R2-02c, R2-10 | Werbeangebote, Aktienmarkt und Standard-Trainingsplan werden im Spielablauf (Spielstart bzw. Tageshook) erzeugt, nicht beim Öffnen; Heimspieltest prüft Buchungen **je Begegnung** | Ab Spielstart alle Kacheln öffnen/schließen → vollständiger Zustandsabdruck (inkl. Zufallszustand) unverändert, Skip R2-02b entfällt; doppelte `SettleHomeMatch()` lässt den Test rot werden |

### Status Spur `ansichten`

- [x] R2-02b/c: Spielstart-Hook `InitializeManagerSystems()` nach der Kaderanlage erzeugt Börse, Werbeangebote und
  Trainingspläne für alle Manager, ohne einen Tag zu verarbeiten. `Ads()` würfelt beim Lesen nichts aus;
  `TrainingScreen.Enter()` liest den vorhandenen Plan. Monatsangebote bleiben im bestehenden Finanz-Tagesablauf.
- [x] Fünf Regressionen vor dem Fix rot: die drei Kacheln einzeln ab Spielstart, alle Kacheln gemeinsam und der
  ausdrücklich gezeichnete Special-Plan. Alle Kacheltests schließen Werbung ein, ohne vorbereitende Testeingriffe;
  der R2-02b-Skip entfällt. Vollständiger Zustandsabdruck einschließlich Zufallszustand sofort und nach Tag 40 gleich.
- [x] R2-10: Eintritt und Gastronomie entsprechen nach jedem Weiter-Schritt exakt den ausgetragenen Begegnungen
  (kontrollierte Kapazitäten und Preise, drei Seeds, Play-Offs und Aufstiegsrunde separat nachgewiesen).
  Mutationsprobe mit zweitem `SettleHomeMatch()` derselben Begegnung: alter Test grün, neuer Test rot
  (77 DM erwartet, 154 DM gebucht); Mutation anschließend entfernt.
- Validierung: Build ohne Warnungen/Fehler; 281 synthetische und 51 Originaldaten-Tests bestanden, 0 übersprungen.
  Ohne Originaldaten: 281 bestanden, 44 sichtbar übersprungen (Theories ohne Daten nicht expandiert).
- Restgrenze: Aufstellungsbereinigung und Kaderlebenszyklus gehören zu `lebenszyklus`; R2-10 sichert den Test ab,
  führt keine allgemeine Idempotenz von `SettleHomeMatch()` ein. Keine neuen `[APPROX]`-Regeln.

**Dateigrenzen in Welle B:**
`lebenszyklus` → `Core/Player.Contract.cs`, `Core/Player.Factory.cs`, `Core/Transfer.cs`, `Core/FinanceInbox.cs`, `Core/Squad.Lineup.cs`,
Saisonwechsel in `Core/SeasonFlow.cs`, `OnNewSeason…` in `GameState.Integration.cs` (im PR begründen) ·
`ansichten` → `Core/FinanceSponsors.cs`, `Core/FinanceStocks.cs`, `Core/Squad.Training.cs`, die öffnenden Bildschirme in `UI/Screens/**`,
`OnDay…`/Spielstart-Hook in `GameState.Integration.cs` (im PR begründen), `tests/EishockeyManager.Core.Tests/SeasonPaymentTests.cs`.
Danach Integration Welle B mit ChatGPT-Review, anschließend Runde 3 (`wettbewerbe`, `speichern`, Finanzkalibrierung).

### Status Spur `lebenszyklus` – Welle B

Stand: implementiert, lokale Abnahme grün; unabhängiges Review und Merge stehen aus.

- [x] R2-04: Saisonwechsel aktualisiert Alter und Restvertrag, beendet Verträge und führt Leihspieler zum
  gespeicherten Eigentümer zurück (Stammvertrag inklusive Gehalt/Versicherung erhalten). Vorprüfung vor jeder
  Saisonmutation; jeder Spieler altert einmal, Rückennummern werden erst nach allen Abgängen vergeben.
  Offene Marktangebote/Gebote werden verworfen, Markt und Aufstellungen neu abgeglichen.
- [x] R2-08: Nachrichten speichern Saison und Tag; Kreditanfragen verfallen nach sieben Simulationstagen auch
  über den Wechsel. `Answer()` prüft die Frist zusätzlich am pausierten, noch unverarbeiteten Tag.
- [x] R2-09: Nur ein tatsächlich offenes Gebot auf ein aktuelles Listing mit passendem Eigentümer darf buchen;
  Wiederholung, Rückzug, Ablehnung, wertgleiche Nachbildung und Eigentümerverlust bleiben ohne Wirkung.
- [x] Aufstellung: Bereinigung über `Continue()` und Saisonwechsel; Öffnen/Schließen verändert nichts.
- Tests vor Fix: 9 synthetische Regressionen und 1 Originaldaten-UI-Regression rot (Alter 27 statt 28,
  Leihe bleibt aktiv, Anfrage von Tag 231 bleibt offen, doppeltes Gebot 400.000 → 666.000 → 932.000 DM).
  Danach zusätzliche Grenzfälle und Originaldaten-Saisonwechsel ergänzt.
- Lokale Abnahme: Build ohne Warnungen/Fehler; **294 synthetisch + 48 mit Originaldaten bestanden,
  1 sichtbar übersprungen** (bekanntes R2-02b, Spur `ansichten`).
- Begrenzte Berührungen: `GameState.Integration.cs` ausschließlich `OnNewSeasonTeam`; `SeasonFlow.ProcessDay`
  ruft die Aufstellungsbereinigung auf; `LineupScreen` verliert nur die beiden mutierenden Aufrufe.
- Neue `[APPROX]`-Regel: ausgelaufene Verträge wechseln ablösefrei in einen erhaltenen Pool vertragsloser
  Spieler; die sichtbare Marktliste bleibt begrenzt. Die genaue DOS-Regel ist noch nicht rekonstruiert.
  Automatische Vertragsverlängerung/Kaderergänzung durch Computer-Transfers, neue Saison-Talente
  und altersbedingter Ruhestand sind weiterhin offen; ohne Verlängerungen können Kader leer werden.
  Alte Leihflags ohne gespeicherten Ursprung werden vor dem Saisonwechsel abgewiesen, nicht einem Verein zugeordnet.

### Integration Runde 2, Welle B (26.10.2026)

PRs #33 (`ansichten`) und #34 (`lebenszyklus`) zusammengeführt, beide auf Basis `a4c4294`: konfliktfrei.
Build ohne Warnungen.

**Integrationsfehler (behoben im Integrations-Commit):** Mit dem Vertragsablauf aus #34 waren **ab der dritten Saison
alle Kader leer**. Jeder Startvertrag läuft zwei Jahre, und Computervereine verlängern nie. Probe mit passivem Manager:
Am Ende der ersten Saison haben alle 36 Vereine mit Kader 20 Spieler, nach dem zweiten Saisonwechsel haben alle
48 wählbaren Vereine 0 Spieler, und 720 Spieler liegen im Pool vertragsloser Spieler. Überbrückung `[APPROX]` in
`Player.Contract.cs`: Computervereine verlängern auslaufende Verträge um `StartContractYears` mit gleichem Gehalt.
Regressionstest `MultiSeasonSquadTests` (vor der Überbrückung rot): Computerkader bleiben über drei Saisonwechsel
vollständig, jeder Spieler altert um 3 Jahre und hat einen gültigen Vertrag.

| Befund | Ergebnis im Gesamtstand |
|---|---|
| R2-02b/c Ansichten | behoben – Werbung, Börse und Trainingsplan entstehen beim Spielstart; strenger Kacheltest ohne Skip grün |
| R2-10 Heimspieltest | behoben – exakte Beträge je Begegnung; doppelte Abrechnung wird rot (laut PR mutationsgeprüft) |
| R2-04 Saisonwechsel | behoben – Altern, Vertragsablauf, Leihende zum Stammverein, Markt neu |
| R2-08 Fristen | behoben – Saison + Tag; Anfrage von Tag 231 ist in der Folgesaison nicht mehr annehmbar |
| R2-09 Transfergebote | behoben – zweites Annehmen ohne Buchung und ohne Kaderänderung |
| **R2-11 (neu, offen)** | Ein passiver Manager verliert zu Beginn der dritten Saison seinen gesamten Kader (alle Verträge laufen gleichzeitig aus), ohne Vorwarnung im Posteingang. Ob und wie das Original warnt oder verlängert, ist nicht rekonstruiert → Runde 3 (Kader-KI/Verträge) |
| R2-12 (neu, offen) | Keine Neuzugänge, Talente oder Ruhestand bei Computervereinen; der Pool vertragsloser Spieler wächst und altert unbegrenzt → Runde 3 |

**Review 1** ([reviews/2026-10-26-pr35-review.md](reviews/2026-10-26-pr35-review.md), Head `c4fca70`): keine Freigabe wegen
zwei P2-Befunden; die ursprünglichen Gegenbeispiele und die Mehrsaisonprobe (4 Wechsel) wurden bestätigt.
R2-11 bekommt die Priorität P1 und R2-12 die Priorität P2, beide für Runde 3. Behoben im zweiten Integrations-Commit:

| Befund | Behebung | Regressionstest (vorher rot) |
|---|---|---|
| PR35-01 (P2) Leihrückgabe in vollen Computerverein: Saisonwechsel bricht nach Teilmutation ab, Spieler verloren | Vorprüfung der Kapazität entfällt; beim Rückführen gilt: Stammverein voll (99 Nummern) → Spieler wird vertragslos (`[APPROX]`). Vorprüfung und Ausführung können nicht mehr auseinanderlaufen | `WaveBReviewRegressionTests.PR3501_…` |
| PR35-02 (P2) Ungültig gewordenes Gebot bleibt offen und erscheint nach „Annehmen“ kommentarlos erneut | `TransferMarket.Accept` liefert `TransferResult`; ein offenes, nicht mehr ausführbares Gebot wird verworfen (`[APPROX]`); die Ansicht zeigt „Der Transfer findet nicht statt.“ | `WaveBReviewRegressionTests.PR3502_…` (Core), `WaveBReviewScreenTests.PR3502_…` (echte Knöpfe) |

Tests: mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) 351 bestanden, 0 übersprungen; ohne Daten 297 bestanden,
47 sichtbar übersprungen; Pflichtlauf ohne Daten scheitert (54 Fälle). Alle Bildschirmfoto-Abläufe laufen durch.

## Runde 1 (abgeschlossen am 26.10.2026)

Abhängigkeiten damals: `saison` → `wettbewerbe`; `manager` und `mannschaft` erweitern Club/Player/GameState in eigenen Dateien.
Ergebnis und Integration siehe Abschnitt „Integration Runde 1“ weiter unten.

---

## Paket `saison` – Saisonablauf über mehrere Jahre

Branch: `paket/saison` · Status: **Runde 1 gemergt** (PRs #6, #8, #10). Details und offene `[APPROX]`-Punkte: [saison.md](saison.md).
Für `wettbewerbe`: DEB-Pokal, Europapokal und WM über `ISeasonCompetition` in `GameState.Competitions` anmelden.

- Hauptrunde → Playoffs → Relegation (laut Video: die zwei Ersten der Oberliga und die zwei Letzten der 2. Liga spielen
  in einer Gruppe je zweimal gegeneinander, die beiden Ersten spielen in der 2. Liga) → Auf-/Abstieg → neue Saison.
- Bildschirm „Saisonende“ (Weltmeister, DEB-Pokal, Deutscher Meister, Europapokal, Auf-/Absteiger je Liga),
  Meisterfoto (kb\16 u. a.), Spielende nach gewählter Dauer, Entlassung bei zu hohen Schulden (siehe `bildschirme.md`).
- Zeitleiste ab „und tschüß…“ inkl. spielfreier Tage, „Keine besonderen Ereignisse…“.
- Eigene Dateien: `Core/Season*.cs`, `Core/Playoffs*.cs`, `UI/Screens/SeasonEnd*.cs`, `UI/Screens/MatchDayScreen.cs`;
  in `MainMenuScreen.cs` nur die Methode `Continue_`.
- Abnahme: Test spielt 3 Saisons mit Auf-/Abstieg durch; Bildschirmfotos-Ablauf `saison`; Vergleich mit Video
  (UI-Video ab ca. 14:00–15:40, Gameplay-Video an Saisonenden).

## Paket `manager` – Finanzen, Werbung, Stadion, Jugend, Posteingang

Branch: `paket/manager` · Status: **Runde 1 gemergt** (PRs #3, #5, #9, #11, #13)

- [x] PR 1: Finanzmodell im Core (Finanzbuch, Bank, Sparbuch, Aktien, Spieltagsabrechnung, Stadion-Grundwerte) –
  Abrechnung über `GameState.SettleFinances` (Aufruf in `PlayNextMatchDay`); Tageswechsel und Saisonwechsel laufen seit der
  Integration über die Zeitleiste (`GameState.Integration.cs`)
- [x] PR 2: Bildschirm Finanzen (kb\152) mit Finanzbuch – `UI/Screens/Manager/FinanceScreen.cs`, Ablauf `finanzen`;
  Bedienung (Überfahren wählt, [+]/[−] = Betrag, ◄/► = buchen) und Finanzbuch-Layout `[APPROX]`, da im Video nichts gebucht wird
- [x] PR 3: Werbung (kb\151, kb\11) – `UI/Screens/Manager/AdvertisingScreen.cs`, `Core/FinanceSponsors.cs`, Ablauf `werbung`;
  Firmen, Sender und Firmen-Infos aus em.exe; Angebotshöhen `[APPROX]` (kalibriert auf das UI-Video), Verlängerung folgt mit PR 5
- [x] PR 4: Stadion (kb\14, Ausbauten kb\8) – `UI/Screens/Manager/StadiumScreen.cs`, Ablauf `stadion`; Kosten der ersten
  Ausbaustufen aus dem Video, Lage von Kino/Imbiß/Kneipe `[APPROX]`, Meldung „Stadion-Ausbau abgeschlossen“ folgt mit PR 5
- [x] PR 5: Jugend, Neuer (Posteingang), Nachrichtenleiste – `UI/Screens/Manager/YouthScreen.cs`, `InboxScreen.cs`,
  `NewsBar.cs`, `Core/FinanceYouth.cs`, `Core/FinanceInbox.cs`, Abläufe `jugend` und `posteingang`; Jugendteams und
  leere Nachrichtenleiste am Video vermessen, Meldungstexte aus em.exe (Stadion-Ausbau, Werbepartner mit
  Verlängerungsoption, Talentsuche); Kreditanfragen unter Managern über den Posteingang. `[APPROX]`: Wirkung der
  Förderung, Ort/Preis/Rabatt der Talentsuche („Spion“: Klick ins Fenster „Jugendteams“), Posteingang-Layout
- Offen: Bedeutung der Kachel „Neuer“ (Symbol ist ein Managerkopf, im Video nicht geöffnet) – vorerst Posteingang;
  Talente bekommen Alter/Stärken, sobald das Paket „mannschaft“ Spielerwerte hat

- Finanzen (kb\152): Konto, Kredit, Sparbuch, vier Aktien mit Kursverlauf; Finanzbuch.
- Werbung (kb\151, Logos kb\11): Bande, Eisfläche, Trikot, Angebote annehmen/ablehnen.
- Stadion (kb\14): Ausbaustufen, Sitz-/Stehplätze, Eintritt.
- Jugend (Talentsuche), „Neuer“ = Posteingang, Nachrichtenleiste unten im Hauptmenü (kb\6).
- Einnahmen/Ausgaben je Spieltag (Zuschauer, Eintritt, Gehälter, Sponsoren) – Größenordnungen aus dem 75-Minuten-Video.
- Eigene Dateien: `Core/Finance*.cs`, `Core/GameState.Manager.cs`, `UI/Screens/Manager/*`.
- Abnahme: alle fünf Kacheln öffnen echte Bildschirme; Kontostand ändert sich über eine Saison plausibel.

## Paket `mannschaft` – Kader und Spieler

Branch: `paket/mannschaft` · Status: **Runde 1 gemergt** (PRs #1, #2, #4, #7, #12, #14)

- ✔ Spielerwerte, Kader aller Vereine, Aufstellung (Core), Mannschaftsstärke → `Club.MatchStrength` in `MatchEngine`
- ✔ Bildschirme Aufstellung, Taktik, Verträge (mit Vertragsverhandlung) und Transfer (Kauf, Leihe, Verkauf, Talentsuche)
- ✔ Training: Bildschirm kb\5, Kosten, Standardplan und Wirkung aus em.exe (`Squad.Training.cs`)
- Training und Transfermarkt laufen seit der Integration über die Zeitleiste (`GameState.Integration.cs`);
  Trainingskosten, Transfers und Talentsuche werden im Finanzbuch gebucht

- Spielerwerte (siehe Spielerstruktur in `reverse-engineering.md`: Talent, Alter, Technik, Kondition, Form, Rolle …),
  Kader aller Vereine aus der Datenbank, Transfermarkt.
- Aufstellung (4 Blöcke + Torhüter), Taktik (Einsatz/Angriff je Spielstand, Linienwechsel), Verträge mit Verhandlung,
  Transfer (Kauf/Leihe), Training (kb\5: 7 Tage × 2 Blöcke × 2 Einheiten).
- Eigene Dateien: `Core/Player*.cs`, `Core/Squad*.cs`, `Core/Transfer*.cs`, `UI/Screens/Team/*`.
- Abnahme: alle fünf Kacheln funktionieren; Aufstellung beeinflusst die Mannschaftsstärke (Schnittstelle zu `MatchEngine`
  mit dem Paket `saison` abstimmen).

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: **Runde 3** – startet nach der Stabilisierung (gemeinsame Spiel-/Finanzabrechnung und Stärketripel nötig); Anschluss über `ISeasonCompetition`

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `sound` – Sound, Musik, Einstellungen

Branch: `paket/sound` · Status: **Runde 2, Welle A** (unabhängig, keine Dateien der Stabilisierungsspuren ändern)

- VOC-Wiedergabe (kb\237–241), CMF-Musik (kb\248) über OPL2-Emulation, Einbindung in Desktop (MonoGame `DynamicSoundEffectInstance`).
- Einstellungen-Bildschirm (Torszenen, Lautstärken, Musik im Spiel, Vollbild).
- Eigene Dateien: `Formats/Voc*.cs`, `Formats/Cmf*.cs`, `src/EishockeyManager.Audio/*` (neues Projekt), `UI/Screens/Settings*`.

Fortschritt:
- [x] VOC-Lader (`VocFile`, alle Blocktypen 1–9 ohne Kompression), `PcmSound`, `WavWriter`, `emtool export-sound` – Branch `paket/sound-voc`
- [x] CMF-Lader (`CmfFile`), OPL2-Emulation (`Opl2`, eigener Code), Wiedergabe nach dem FM-Treiber in em.exe (`CmfPlayer`) – Branch `paket/sound-cmf`
- [x] Projekt `EishockeyManager.Audio` (Mischer, ein Effektkanal wie CT-VOICE, Musik in Schleife), Desktop über `DynamicSoundEffectInstance`, F8 = Ton aus; Effektzuordnung aus dem Disassembly belegt – Branch `paket/sound-audio`
- [x] Bildschirm Einstellungen (Kachel „Gemischtes“): Lautstärken, Musik auch im Spiel, Torszenen, Vollbild; gespeichert in `einstellungen.json` im Benutzerprofil – Branch `paket/sound-einstellungen`

Offen für später: Effekte in den Torszenen auslösen (sobald es sie gibt; UI braucht dann eine Referenz auf Audio), Einstellungen „Cheats“ und „Automatisch speichern“ (gehören zu `speichern`), Einstellungen-Eintrag im Titelmenü, Musikstart/-stopp des Originals aus dem Disassembly.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: **Runde 3** – endgültiges Schema erst nach Kaderlebenszyklus, Fristen und Transaktionssicherheit; muss Zufallszustand, Objektidentität und offene Vorgänge enthalten

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`).

## Integration Runde 1 (26.10.2026) – Ergebnis und offene Punkte

Die drei Pakete waren einzeln grün, zusammen schlugen 8 Tests fehl: `manager` und `mannschaft` kannten die Zeitleiste
von `saison` noch nicht. Behoben in `src/EishockeyManager.Core/GameState.Integration.cs` (Anschlusspunkte
`OnDayManager`, `OnNewSeasonManager`, `OnNewSeasonTeam`) und Tests, die Zeit über `TestSeason` (= `Continue()`) vorspulen.

**Regel ab jetzt:** Zeit vergeht nur über `GameState.Continue()`. Wer etwas je Tag/Woche/Saison braucht, hängt sich in
`GameState.Integration.cs` ein (bzw. ruft von dort die eigene Methode auf) – keine eigenen „CatchUp“-Zähler mehr.
Geld immer über `GameState.Book(manager, kategorie, betrag)` buchen, nicht `Money` direkt ändern.

Offene Punkte (stehen jetzt als Spuren in Runde 2):

| Paket | Punkt |
|---|---|
| `manager` + `mannschaft` | **Talentsuche doppelt**: „Spion“ im Jugend-Fenster (`FinanceYouth.cs`) und Knopf „Talent“ im Transfer (`Transfer.Talent.cs`). Im Original eine Funktion → zusammenführen (Vorschlag: Logik in `FinanceYouth`, Transfer-Knopf ruft sie auf). |
| `manager` | Heimspiele in Play-Offs, Aufstiegsrunde und Pokalen bringen noch keine Zuschauereinnahmen (`SettleFinances` nur für Ligaspiele). |
| `manager` | Kalibrierung: Ein passiver Manager in der Oberliga rutscht in drei Saisons auf ca. −0,5 Mio. DM (echte Daten). Mit dem 75-Minuten-Video abgleichen. |
| `saison` | Saisonprämien (`SeasonFlow.cs`) direkt auf `Money` – über `Book(…)` mit eigener Kategorie buchen (Finanzbuch). |
| `mannschaft` | Saisonwechsel für den Kader (Altern, Vertragsende, neue Talente) – Anschluss `OnNewSeasonTeam` in `GameState.Integration.cs`. |
| `mannschaft` | `CatchUpTraining()`/`TransferMarket.CatchUp()` in den Bildschirmen sind jetzt überflüssig (laufen über die Zeitleiste) – entfernen. |

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
