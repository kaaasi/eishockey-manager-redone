# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

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

### Status Spur `finanzen`

- Teil 1 auf `paket/finanzen`: Prämien nach Ereignissen statt geschätzter Platz-2/3-Zahlungen;
  Meister 500.000 DM statt zusätzlich 1 Mio. DM. Belege und verbleibende Zuordnungsannahmen stehen
  in `Core/FinancePremiums.cs`. `FinanceInbox.cs` und die Kaderlogik bleiben unberührt.
- Regression `FinanceCalibrationTests.Premium_DependsOnTitlesAndPromotion_NotEstimatedTableAwards`:
  gegen unveränderten Produktcode von `cc26bde` sechs von sieben Fällen rot; danach sieben grün.
  Ein Originaldaten-Test prüft die Konto-Buchungsbytes in `em.exe` an den dokumentierten Dateioffsets.
- Validierung Teil 1: frischer Gesamtbuild ohne Warnungen/Fehler; 304 synthetische und 59
  Originaldaten-Fälle bestanden, 0 übersprungen. Ohne Originaldaten 304 bestanden und 49 sichtbar
  übersprungen (Theories werden ohne Daten nicht in sämtliche Datenzeilen expandiert).
- Mehrsaisonprobe: Start Oberliga (ESC WOLFSBURG) und Bundesliga (ESV KAUFBEUREN), Seeds 3/17/41,
  je passiv und aktiv, vier tatsächliche Saisonwechsel über `Continue()`/`StartNextSeason()`.
  Aktiv: Verträge regulär verlängern, Auto-Aufstellung, angebotene Werbung annehmen, Imbiss bauen.
  Keine Geldzufuhr. Konto gegen Finanzbuch nach jedem Schritt, Vorjahresbuch nach jedem Wechsel geprüft.
  Einzelkategorien, Start-/Endkonto, Liga und Kadergröße stehen im xUnit-Testoutput.
- Gemessene Endkonten nach vier Wechseln: passiv Oberliga −14.330 bis 140.970 DM,
  passiv Start-Bundesliga 3.715.170 bis 4.384.490 DM; aktiv Oberliga 2.131.369 bis 2.178.451 DM,
  aktiv Start-Bundesliga 11.098.224 bis 12.680.139 DM. Ab-/Aufstieg bleibt möglich.
- **Noch keine fachliche Gesamtabnahme der Finanzbalance.** Die im Test markierten Schranken
  (passiv −1,5 bis 3 Mio. / 7 Mio. DM, aktiv bis 15 Mio. DM) sind technische Alarmgrenzen des
  aktuellen Näherungsmodells, keine aus dem Original rekonstruierte Sollspanne.
  Die Oberliga-Grenze erlaubt saisonale Verlust-/Gewinnschwankungen unterhalb einer Aufstiegsprämie;
  Bundesliga berücksichtigt die höhere Zuschauerbasis und mehrere zusätzliche Heimspiele.
  Der bekannte passive Kaderverlust ab Saison 3 und die dann einsetzende Gehaltspauschale verzerren
  die Balance weiter; diese Probe ersetzt die Abnahme der Spur `kader` nicht.
- Referenzstichproben der Karriere gelesen: `karriere_01000.png` (Konto 132.775 DM,
  Einnahmen/Monat 62.747 DM, Ausgaben 120.370 DM), `karriere_01100.png` (172.799 / 63.370 /
  120.370 DM; Zuschauerschnitt 3.049, bereinigt 2.703, Rekord 4.500), `karriere_02000.png`
  (Rushware 13.899 DM/Monat, Einnahmen 109.450, Ausgaben 156.171 DM), `karriere_03450.png`
  (Konto 680.742 DM, Eintritt 12 DM, 2.000 Sitz-/5.000 Stehplätze, Gastronomie OHNE).
  Das ist eine aktiv gespielte Karriere mit Transfers, Werbung und Ausbauten, kein passiver Kontrolllauf.
- Offen für Teil 2: Zuschauer-/Preisfunktion, Gastronomieertrag, laufender Stadionunterhalt und
  Werbeangebotsformel bis zu den Original-Buchungen rekonstruieren. Bisherige Formeln bleiben
  ausdrücklich `[APPROX]`; insbesondere erklären die erheblich höheren Original-Monatsausgaben
  noch keine einzelne belegte Ersatzformel. Keine frei erfundene Ausgabenpauschale zur Erzeugung
  eines gewünschten Minus eingeführt. Die genaue Zuordnung der 100.000-DM-Einzelprämien bleibt `[APPROX]`.

### Integration Runde 3, Welle A (28.10.2026)

Branch `integration/runde-3a` von `main` `cc26bde`, Leitstelle Claude Code. Zusammengeführt in dieser Reihenfolge:
#39 (kader), #40 → #43 → #44 → #48 → #49 → #51 (wettbewerbe, gestapelt), #41 (finanzen, **Teil 1**: Prämien und
Messbasis; der Rest folgt als Spur `finanzen-2`).

- Konflikte: `GameState.Integration.cs` (#39 × #49) – Vertragsankündigung und Saisonstatistik sind unabhängig, beide
  Aufrufe bleiben im Tagesabschluss; `docs/arbeitspakete.md` (#39 × #41) – beide Statusabschnitte übernommen.
- Integrationsfehler 1: Build mit Warnungen als Fehler scheiterte an CS8600/CS8602 im Ablauf `nationalteam`
  (`Scenarios/Wettbewerbe.cs`, aus #51). Behoben ohne Verhaltensänderung.
- Integrationsfehler 2: `SeasonPaymentTests.SeasonPremium_IsBookedOnce_InOwnCategory` rot („Keine Prämie gezahlt“):
  #41 zahlt keine Tabellenprämie mehr, und mit den Wettbewerben gewinnt in Seeds 1–3 kein Manager-Verein einen Titel
  (Meister jeweils Verein 1). Test prüft jetzt mindestens drei Seeds und weitere, bis eine Prämie gebucht wurde.
- Mehrsaisonprobe auf dem Integrationsstand (alle Wettbewerbe, Kader-KI und Prämien gemeinsam über `Continue()`):
  `SquadLifecycleTests` (6 Wechsel) und `FinanceCalibrationTests` (4 Wechsel, 12 Läufe) grün. Endkonten nach vier
  Wechseln: passiv Oberliga 145.010–271.200 DM, passiv Bundesliga 4.074.640–4.718.800 DM, aktiv Oberliga
  2.137.396–2.267.913 DM, aktiv Bundesliga 12.343.492–13.001.751 DM (innerhalb der Alarmgrenzen aus #41).
- Bildschirmfoto-Abläufe `wettbewerbe europapokal wm staerken statistik nationalteam vertraege posteingang finanzen
  saison saison_aufstieg`: 64 Bilder ohne Fehler; alle Wettbewerbsbilder bytegleich mit dem Stand von #51.
  Abweichungen zu #39/#41 nur durch zusätzliche Pokalspiele (Datum der ersten Nachricht, Eintritt, Kontostand).
- Restlücken (für Review und Welle B):
  - Passive Manager haben ab Saison 3 einen leeren Kader (angekündigte Vertragsenden, #39) und zahlen dann die alte
    Gehaltspauschale (−1.092.000 DM/Saison in der Bundesliga) → `finanzen-2`.
  - `AppointNationalCoach` bucht den Etat des Bundestrainers als Einnahme „Prämien“ (#51) – Beleg prüfen → `finanzen-2`.
  - `FinishSeason` ordnet jeden Titel außer Meister/Pokal als `EuropeanCupWinner` ein; der WM-Titel trifft nur
    Nationen und damit nie einen Manager-Verein, die Zuordnung sollte aber ausdrücklich sein → `finanzen-2`.
  - Prozess-PRs #45/#46/#50 sind noch nicht auf `main`: `tools/pruefen.sh` und `EM_SKIP_GAMEDATA` fehlen dort. Lauf 2/3
    wurden deshalb mit entfernten Originaldaten ausgeführt (Skript vom Branch `prozess/sparsam`, Lauf 1 identisch).

## Runde 3, Welle B (ab Merge der Integration Welle A)

```
Welle B (parallel):  speichern   finanzen-2
```

Base für beide Spuren: `main` nach Merge des Integrations-PR Welle A (SHA trägt die Spur in ihren Draft-PR ein).

| Spur | Branch | Befunde | Inhalt | Verbindliche Abnahme (Regressionstest) |
|---|---|---|---|---|
| **speichern** | `paket/speichern` | Paket `speichern` | Eigenes Spielstandformat mit Versionsnummer für den **gesamten** Zustand: Vereine, Spieler (stabile IDs statt Objektverweisen: Kader, Aufstellung, Markt, Leihen, WM-Kader, `RetiredPlayers`), Manager, Finanzbuch/Stadion/Aktien/Kredite, Posteingang mit Fristen, Vertragsankündigungen (`Notice`, `NoticeYears`), Wettbewerbe (DEB-Pokal, Europapokal, WM, Bundestrainer, Etat), Statistik („Ewige Tabelle“, „Früher“), Saisonphase und Kalender; **Zustand aller Zufallsquellen** (`Random`, `FinanceRandom`, `CupRandom`, `ContractRandom`) – `System.Random` ist nicht serialisierbar, daher eigener Generator, der die bisherige Zahlenfolge für denselben Seed beibehält. Laden/Sichern-Dialog wie im Original; Einstellung „Automatisch speichern“. Import `*.HOK` nicht in dieser Spur. | Rundlauf an beliebigen Tagen (vor/nach Pokalrunde, WM, Saisonwechsel) ergibt denselben Zustandsabdruck; Zwillingsspiel: gespeichert + geladen + 4 Saisonwechsel = ununterbrochen gespielt (Konten, Tabellen, Kader, Titel identisch); Vollständigkeitstest, der jede neue öffentliche Zustandseigenschaft ohne Sicherung meldet; fremde/alte Version wird mit Meldung abgelehnt; Öffnen des Dialogs ändert nichts |
| **finanzen-2** | `paket/finanzen-2` | Rest aus #41 | Zuschauer-/Eintrittsfunktion, Gastronomieertrag, laufender Stadionunterhalt und Werbeangebotsformel bis zu den Original-Buchungen (em.exe) rekonstruieren; Zuordnung der 100.000-DM-Einzelprämien; Gehaltspauschale bei leerem Kader prüfen; Etat des Bundestrainers als Einnahme belegen oder korrigieren; Titelzuordnung in `FinishSeason` ausdrücklich machen | Mehrsaisonprobe (4 Wechsel, Oberliga und Bundesliga, passiv und aktiv): Kontoverlauf passiver Manager in einer **aus Original oder Video begründeten** Sollspanne statt technischer Alarmgrenzen; je Formel roter Regressionstest gegen `[ORIGINAL]`-Wert; verbleibende Näherungen `[APPROX]` |

**Dateigrenzen in Welle B:**
`speichern` → `Core/Save*.cs`, `Core/Rng*.cs` (neuer Generator), `UI/Screens/SaveLoad/**`, Ablauf in
`Tools/Scenarios/Speichern.cs`; Umstellung der Felder `Random`/`FinanceRandom`/`CupRandom`/`ContractRandom` und
stabile Spieler-IDs in den jeweiligen `partial`-Dateien nur minimal und begründet ·
`finanzen-2` → `Core/Finance*.cs` außer `FinanceInbox.cs`, Prämien- und Titelzeilen in `SeasonFlow.cs`,
`Core/CupsNational.GameState.cs` nur für die Etatbuchung, `UI/Screens/Manager/Finance*.cs`, `StadiumScreen.cs`, `AdvertisingScreen.cs`.
**Abhängigkeit:** Neuer Zustand aus `finanzen-2` muss gesichert werden. Wer zuerst gemergt wird, ist egal; der
Vollständigkeitstest aus `speichern` macht fehlende Felder sichtbar, der zweite PR ergänzt sie.

## Runde 2 – Stabilisierung (abgeschlossen am 26.10.2026)

Ergebnis: Welle A gemergt mit #31 (Review [pr31](reviews/2026-10-26-pr31-review.md)), Welle B mit #35
(Review [pr35](reviews/2026-10-26-pr35-review.md), Nachprüfung [pr35-nachpruefung](reviews/2026-10-26-pr35-nachpruefung.md):
Freigabe für `9bd8fea`). Der P3-Befund PR35-N01 (UI-Test prüfte die Meldung nicht) ist nach dem Merge behoben.
Offen in Runde 3: R2-11, R2-12.

Plan, Spurstatus und Integrationsberichte von Runde 2: [archiv/runden-1-2.md](archiv/runden-1-2.md).

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

Branch: `paket/wettbewerbe` · Status: **Runde 3, Welle A** – PR 1–6 (DEB-Pokal, Europapokal, WM, Stärken, Ewige Tabelle/Früher, Bundestrainer) eingereicht (Review ausstehend); als Nächstes der Spielmodus „nur Weltkugel“ (Bundestrainer-Ablauf), dann Paarungen Liga. Regeln und Belege: [wettbewerbe.md](wettbewerbe.md)

| PR | Inhalt | Stand |
|---|---|---|
| 1 | DEB-Pokal: Auslosung, Heimrecht, „best of three“, Abrechnung, Bildschirm „DEB-Pokal Auslosung“ | eingereicht |
| 2 | Europapokal (Gruppe + Endrunde), Bildschirm Gruppen/Endrunde | eingereicht (baut auf PR 1 auf) |
| 3 | WM (2 Gruppen + K.-o.), Bildschirm Gruppe/Tabelle WM | eingereicht (baut auf PR 2 auf) |
| 4 | Stärken (Ligen, Länderteams) | eingereicht (baut auf PR 3 auf) |
| 5 | Statistiken: Ewige Tabelle (Totale), Früher | eingereicht (baut auf PR 4 auf) |
| 6 | Bundestrainer: WM-Kader, „WM Vorbereitung“ (Freundschaftsspiele), Etat; WM: Werte Deutschlands neu ausgewürfelt, Länder 20-mal getauscht | eingereicht (baut auf PR 5 auf) |
| 7 | Spielmodus „nur Weltkugel“: Spielvorbereitung, Vereinssaison überspringen, Menü des Bundestrainers | offen, berührt gemeinsame Dateien (siehe `wettbewerbe.md`) |
| 8 | Paarungen Liga (kein Videobeleg) | offen |
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | **wartet auf `livespiel`** (Tore, Vorlagen, Strafen je Spieler) |

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

## Integration Runde 1

Ergebnis und offene Punkte: [archiv/runden-1-2.md](archiv/runden-1-2.md). Die Regeln daraus stehen in `AGENTS.md`.

## Pilot: Markerbericht (Prozess, Übergabe zwischen Agenten)

Ziel und Bewertung: `docs/prozess/2026-10-27-audit-validierung.md` §5. Start nach Merge von #45 und #50.

| Feld | Inhalt |
|---|---|
| Ziel | `tools/markerbericht.py` zählt `[ORIGINAL]`/`[APPROX]` je Datei und Projekt in `src/`, gibt eine Markdown-Tabelle aus; CI hängt sie an die Zusammenfassung an |
| Branch / Base | `paket/markerbericht` von aktuellem `main` |
| Bearbeiter | ChatGPT; nach dem ersten Commit Übergabe an eine **neue** ChatGPT-Sitzung, die das Paket beendet |
| Dateigrenzen | `tools/markerbericht.py` (neu), ein Schritt am Ende des Jobs `build` in `.github/workflows/build.yml` |
| Abnahme | Zahlen gleich `grep -rho "\[APPROX\]\|\[ORIGINAL\]" src \| sort \| uniq -c`; Ausgabe sortiert und deterministisch; keine neuen Abhängigkeiten; CI grün; PR nach Vorlage |
| Review | Claude Code (Leitstelle) |
| Status | offen |

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
