# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 3 – Kader, Wettbewerbe, Finanzen (ab 26.10.2026)

Basis: `main` nach Integration Runde 2 (PR #35 und Nachprüfung #37). Runde 2 und die Pakete `mannschaft`/`sound`:
[archiv/runden-1-2.md](archiv/runden-1-2.md). Regeln und Ablauf: [AGENTS.md](../AGENTS.md), [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md). **Neu verbindlich:** Jede Spur, die Saisonwechsel, Kader oder Geld berührt,
liefert eine **Mehrsaisonprobe** (mindestens 4 Saisonwechsel, passiver und aktiver Manager) als Test.

```
Welle A (parallel):  kader   wettbewerbe   finanzen          → integriert mit #54 (main 5f8e02a)
Welle B (parallel):  speichern   finanzen-2   markerbericht   → abgeschlossen 29.10.2026 (main 51c110a)
Welle C (parallel):  speichern-dialog   wettbewerbe-weltkugel
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
- **Ergebnis (28.10.2026):** #54 gemergt mit Head `3e44a84` → `main` `5f8e02a`. Review
  [integration-runde-3a](reviews/2026-10-28-integration-runde-3a.md) (R3A-01 P2, R3A-02/03 P3), Nachprüfung
  [integration-runde-3a-nachpruefung](reviews/2026-10-28-integration-runde-3a-nachpruefung.md): alle drei behoben,
  Freigabe für `3e44a84`. Die Prozess-PRs #45/#46/#50/#52 sind vor #54 gemergt; `tools/pruefen.sh` und
  `EM_SKIP_GAMEDATA` stehen damit auf `main`. Welle A ist abgeschlossen; die übrigen Restlücken gehen an Welle B
  (`finanzen-2`, `speichern`).

## Runde 3, Welle B (ab 28.10.2026)

```
Welle B (parallel):  speichern   finanzen-2   markerbericht (Pilot, ChatGPT)
```

**Base für alle drei Spuren:** `main` `5f8e02a` (Integration Welle A, #54). Branch vom dann aktuellen `main`; den
tatsächlichen Base-SHA trägt die Spur in ihren Draft-PR ein. `speichern` und `finanzen-2` berühren Kader oder Geld →
Mehrsaisonprobe Pflicht. Kleine PRs je Teil (`paket/<spur>-<teil>`), jeder Teil mit eigenem rotem Regressionstest.

### Spur `speichern` – Spielstand mit Schemaversion (G11)

- **Ziel:** eigenes Spielstandformat für den **gesamten** Zustand, robust gegen Saisonwechsel und spätere
  Schemaänderungen (Audit G11, [audit-validierung](prozess/2026-10-27-audit-validierung.md), Maßnahme M6).
- **Branch / Base:** `paket/speichern[-<teil>]` von `main` (≥ `5f8e02a`). Vorschlag Teile: `-rng` (Generator),
  `-ids` (stabile IDs), `-format` (Schema, Roundtrip), `-dialog` (Laden/Sichern, „Automatisch speichern“).
- **Inhalt:**
  - **Schemaversion:** jede Datei beginnt mit Kennung und ganzzahliger Schemaversion; Laden prüft beides vor dem
    Lesen. Fremde Datei oder unbekannte (höhere) Version → Meldung, kein Teilzustand. Migrationsstelle je Version
    vorgesehen (zunächst leer). Format ist textbasiert oder binär nach Wahl der Spur, im PR begründet und in
    `docs/formate/` beschrieben.
  - **Stabile IDs:** jeder Spieler bekommt eine dauerhafte ID (bei Spielstart, Talent, Pool-Zugang vergeben, nie
    wiederverwendet). Alle Verweise werden als ID gesichert statt als Objektverweis: Kader, Aufstellung/Blöcke, Markt
    und Pool, Leihen (inkl. ursprünglicher Verein), WM-Kader des Bundestrainers, `RetiredPlayers`, Posteingang mit
    Spielerbezug. Vereine und Manager über ihren Index bzw. eine eigene ID.
  - **Umfang:** Vereine, Spieler, Manager, Finanzbuch/Stadion/Aktien/Kredite/Werbung, Posteingang mit Fristen,
    Vertragsankündigungen (`Notice`, `NoticeYears`), Wettbewerbe (DEB-Pokal, Europapokal, WM, Bundestrainer, Etat),
    Statistik („Ewige Tabelle“, „Früher“), Saisonphase und Kalender.
  - **Zufallszustand:** `Random`, `FinanceRandom`, `CupRandom`, `ContractRandom` auf einen eigenen, serialisierbaren
    Generator umstellen, der für denselben Seed **dieselbe Zahlenfolge** wie `System.Random` liefert (sonst ändern sich
    alle Mehrsaison-Referenzwerte). Die verzögerte Erzeugung (`??=` mit Saison-Seed) bleibt im Verhalten gleich und
    wird mitgesichert (erzeugt / noch nicht erzeugt).
  - Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname); Einstellung „Automatisch speichern“.
    Import `*.HOK` **nicht** in dieser Spur.
- **Abnahme (Regressionstests):**
  1. **Generator:** für mehrere Seeds sind die ersten 10.000 Werte von `Next()`, `Next(a,b)`, `NextDouble()` gleich
     `System.Random`; Zustand sichern → laden → Folge setzt sich identisch fort.
  2. **IDs:** nach 4 Saisonwechseln (passiv und aktiv) sind alle IDs über Kader, Pool, Leihen und `RetiredPlayers`
     eindeutig, kein Verweis zeigt ins Leere; eine ID wird nie neu vergeben.
  3. **Roundtrip:** sichern → laden → sichern ergibt bytegleiche Dateien bzw. denselben Zustandsabdruck, an
     beliebigen Tagen (vor/nach Pokalrunde, während WM, unmittelbar vor und nach dem Saisonwechsel, mit offener
     Frist im Posteingang).
  4. **Mehrsaison-Roundtrip (Zwillingsspiel):** gespeichert + geladen + 4 Saisonwechsel über `Continue()` =
     ununterbrochen gespielt (Konten, Finanzbuch, Tabellen, Kader, Titel, Statistik identisch); passiv und aktiv,
     synthetisch und mit Originaldaten.
  5. **Vollständigkeit:** ein Test meldet jede öffentliche Zustandseigenschaft von `GameState` und den
     Zustandsklassen, die weder gesichert noch ausdrücklich ausgenommen ist.
  6. **Version:** fremde Datei und höhere Schemaversion werden mit Meldung abgelehnt, der laufende Zustand bleibt
     unverändert. Öffnen und Abbrechen des Dialogs ändert nichts (zustandsrein).
- **Dateigrenzen:** `Core/Save*.cs`, `Core/Rng*.cs` (neu), `Core/Player.Identity.cs` (neu, ID-Vergabe),
  `UI/Screens/SaveLoad/**`, `Tools/Scenarios/Speichern.cs`, `docs/formate/spielstand.md`. Minimal und im PR begründet:
  Umstellung der Zufallsfelder in `GameState.cs`, `GameState.Manager.cs`, `Cups.GameState.cs`,
  `Transfer.Squads.cs`; ID-Vergabe in `NewGame.cs`/`Player.Factory.cs`; „Automatisch speichern“ in
  `UI/Screens/Settings.cs`/`SettingsScreen.cs`.

### Status Spur `speichern`

Stand 29.10.2026: Teile `-rng` (#68), `-ids` (#70) und `-format` (#71, Schema mit Version, Roundtrip, Zwillingsprobe)
**gemergt**; Abnahme 1–5 erfüllt. Reviews [speichern-68-70-71](reviews/2026-10-28-speichern-68-70-71.md) und
[speichern-71-nachpruefung](reviews/2026-10-28-speichern-71-nachpruefung.md). Schema 2 kam mit `finanzen-2` (#67).

- Offen: Teil `-dialog` (Abnahme 6, Welle C); #80 (Reihenfolge von Wörterbüchern nach Remove/Add, P3) vom Nutzer
  zurückgestellt.

### Spur `finanzen-2` – Restkalibrierung (Restlücken aus #41)

- **Ziel:** die in #41 als `[APPROX]` offen gelassenen Buchungspfade aus em.exe und Video rekonstruieren, sodass der
  passive Kontoverlauf in einer **belegten** Sollspanne liegt statt in technischen Alarmgrenzen.
- **Branch / Base:** `paket/finanzen-2[-<teil>]` von `main` (≥ `5f8e02a`). Vorschlag Teile: `-zuschauer`
  (Zuschauer und Eintritt), `-stadion` (Unterhalt, Gastronomie), `-werbung`, `-ausgaben` (Monatsausgaben, Abnahme).
- **Inhalt:**
  - **Zuschauer und Eintritt:** Zuschauerfunktion (Liga, Stärke, Tabellenplatz, Preis, Kapazität Sitz/Steh) und
    Eintrittserlös je Heimspiel bis zu den Original-Buchungen. Bezugswerte u. a. `karriere_01100.png`
    (Schnitt 3.049, bereinigt 2.703, Rekord 4.500), `karriere_03450.png` (Eintritt 12 DM, 2.000/5.000 Plätze).
  - **Werbung:** Formel der Werbeangebote (Betrag, Laufzeit, Häufigkeit) und laufende Einnahmen
    (Bezug `karriere_02000.png`: Rushware 13.899 DM/Monat).
  - **Stadionunterhalt und Gastronomie:** laufender Unterhalt je Ausbaustufe, Gastronomieertrag.
  - **Zu niedrige Monatsausgaben:** Original 120.370 DM/Monat (`karriere_01000/01100`) bzw. 156.171 DM
    (`karriere_02000`) gegen das Modell; fehlende Ausgabenposten in em.exe finden und je Posten belegen. **Keine
    erfundene Pauschale**, um ein gewünschtes Minus zu erzeugen.
  - Nebenpunkte aus Integration A: Gehaltspauschale bei leerem Kader (passiver Manager ab Saison 3,
    −1.092.000 DM/Saison Bundesliga) prüfen; Etat des Bundestrainers als Einnahme „Prämien“ belegen oder korrigieren;
    Titelzuordnung in `FinishSeason` ausdrücklich machen (WM-Titel nicht als `EuropeanCupWinner`); Zuordnung der
    100.000-DM-Einzelprämien.
- **Abnahme (Regressionstests):**
  1. Je rekonstruierter Formel ein Test gegen den `[ORIGINAL]`-Wert (em.exe-Konstante oder Referenzbild), gegen den
     unveränderten Code von `5f8e02a` rot; verbleibende Näherungen ausdrücklich `[APPROX]` mit Begründung.
  2. Monatsübersicht für die Lage der Referenzbilder (Liga, Stadion, Werbung nachgestellt): Einnahmen und Ausgaben je
     Monat innerhalb einer im PR begründeten Toleranz zu den Bildwerten.
  3. Mehrsaisonprobe (4 Wechsel, Oberliga und Bundesliga, passiv und aktiv, mehrere Seeds): passiver Kontoverlauf in
     einer **aus Original oder Video begründeten** Sollspanne; die Alarmgrenzen aus #41 werden ersetzt.
  4. Jede Buchung genau einmal im Finanzbuch; Konto = Summe des Finanzbuchs nach jedem Schritt.
- **Dateigrenzen:** `Core/Finance*.cs` außer `FinanceInbox.cs`, Prämien- und Titelzeilen in `SeasonFlow.cs`,
  `Core/CupsNational.GameState.cs` nur für die Etatbuchung, `UI/Screens/Manager/Finance*.cs`, `StadiumScreen.cs`,
  `AdvertisingScreen.cs`, Tests `FinanceCalibrationTests` und neue `Finance*Tests`.

**Abhängigkeit `speichern` ↔ `finanzen-2`:** Neuer Zustand aus `finanzen-2` (z. B. Zuschauerrekord, Werbeverträge)
muss gesichert werden. Wer zuerst gemergt wird, ist egal: Der Vollständigkeitstest aus `speichern` meldet fehlende
Felder, der zweite PR ergänzt sie. Ändert `speichern` den Generator, müssen die Zahlenfolgen gleich bleiben – sonst
verschieben sich die Referenzwerte aus `finanzen-2`.

### Status Spur `finanzen-2`

Stand 29.10.2026: **gemergt** (PR #67, Head `9f51e14` → `main` `32101ab`). Review
[finanzen-2-67](reviews/2026-10-28-finanzen-2-67.md): F67-01…04 (#75–#78) behoben; Nachprüfung
[finanzen-2-67-nachpruefung](reviews/2026-10-28-finanzen-2-67-nachpruefung.md) ohne Codebefunde, die dort blockierten
drei Testläufe hat die Leitstelle auf dem Head nachgeholt (614/0/0 · 530/0/69 · Pflichtlauf wie gewollt). Belege in
[finanzen-rekonstruktion.md](finanzen-rekonstruktion.md) (Disassembly em.exe + Karriere-/UI-Video).

- [x] **Monatsabschluss** `[ORIGINAL]` (0x3E657/0x3E811/0x3F02C): Grundeinnahmen 5 × Spieler × Teamwerte; Grundausgaben
  130.000 − 10.000 × (5 − Level) − 20 × Ligawert (+ Bundesliga-/2.-Liga-Terme); Unterhalt je Ausbaustufe und Platz;
  Zinsen täglich aufs Zinskonto (Sparbuch 1 %, Kredit 4 %, Manager 2,5 %/2 %, Überziehung 10 %); Kostenstufe = Liga der
  Saison. Keine Gehaltspauschale mehr bei leerem Kader. Startstadion und Eintritt je Liga (0x29F2E); Startkonto
  400.000 + 50.000 × (5 − Level) DM wird angewendet (0x29BD5, #78).
- [x] **Zuschauer** `[ORIGINAL]` (0x3F185): Stärken, Tabellenplätze, Ligen, Ausbaustufen, Level, Preisfaktor,
  Wettbewerbsbonus (DEB-Pokal 1, Play-Off 1, Europapokal 2). Eintritt = Zuschauer × Preis; **keine Gastronomie-Einnahme**
  (Kategorie entfernt). `[APPROX]`: Tabellenplatz vor dem 1. Spieltag, random(a, b) mit a > b, Vereine ohne Liga.
- [x] **Werbung** `[ORIGINAL]` (0x3FB47): Beträge aus Bewertung × Art × Partner, Laufzeit 12/24 Monate, Option 1/8,
  Angebote am 1. und zur Monatsmitte (Test erzwingt den Zusatztermin, #77), Partnerwahrscheinlichkeiten; Verlängerungsfrage
  nur mit Option. Bewertung (0x3F833): Pokalterm 32 · (Titel · (n + 1) − 2) nur bei ausgelostem DEB-Pokal, n = Runde
  ([0x5CF3], #75); `[APPROX]` für drei Titelzähler, `[0x7AF]` und Spielerwerte.
- [x] **Nebenpunkte:** Bundestrainer-Etat und Freundschaftsspiele über das eigene Konto des Nationalteams
  (0x554A6/0x3BFC4, `GameState.NationalAccount`), nicht übers Vereinskonto; Hauptrunden-Erster 100.000 DM in **jeder**
  Liga (0xE0:0x20); WM-Titel ausdrücklich ohne Vereinsprämie; die zwei weiteren 100.000-DM-Kennungen bleiben `[APPROX]`.
- [x] Mehrsaisonprobe `FinanceCalibrationTests.FourSeasonChanges_*` (Oberliga und Bundesliga, passiv und aktiv, Seeds
  3/17/41, Originaldaten und synthetisch): Eintritt je Heimspiel innerhalb [50 × Preis, Kapazität × Preis]; Sponsoren
  höchstens 7 × (5 · Spieler · 396 / 3 + Vertragssumme); Prämien nur als Summe der em.exe-Einzelprämien; passiv
  Unterhalt/Gehälter exakt aus den em.exe-Konstanten, übrige Kategorien 0, Saisonsaldo im geschlossenen Korridor;
  Konto = Startkonto + Finanzbuch (#76). Endkonten nach vier Wechseln (Originaldaten): passiv Oberliga
  774.708–1.137.872 DM (Tiefstand 171.112), passiv Bundesliga 4.845.672–4.999.230 DM, aktiv Oberliga
  2.784.332–3.008.131 DM, aktiv Bundesliga 14.037.273–14.194.070 DM.
- [x] Spielstand: Schema 2 mit Migration 1 → 2 für `NationalAccount`, `CostLeague`, Zins-Akkumulatoren, `OptionMonths`
  und das Finanzbuch ohne „Gastronomie“ (`SaveGame.Migration1.cs`, `FinanceSaveGameTests`).
- Restlücken: Karriere-Video ESC Wolfsburg zeigt zu Spielbeginn 87.370 DM Monatsausgaben, die Formel 73.977 DM
  (UI-Video Berliner SC passt auf 100 DM); passive Manager mit leerem Kader ab Saison 3 (Spur `kader`) verzerren
  Eintritt und Gehälter weiter; Werbepartner-Bewertung ohne die unbekannten Titelzähler; Zeitpunkt der
  DEB-Pokal-Auslosung im Original nicht geprüft (Remake: Saisonbeginn); Originaltreue der übrigen `[ORIGINAL]`-Fundstellen
  noch nicht unabhängig nachdisassembliert (Review §3).

### Spur `markerbericht` – Pilot Übergabe (ChatGPT)

Ziel und Bewertung: [audit-validierung](prozess/2026-10-27-audit-validierung.md) §5. Voraussetzungen (#45, #46, #50)
sind auf `main`.

| Feld | Inhalt |
|---|---|
| Ziel | `tools/markerbericht.py` zählt `[ORIGINAL]`/`[APPROX]` je Datei und Projekt in `src/`, gibt eine Markdown-Tabelle aus; CI hängt sie an die Zusammenfassung an |
| Branch / Base | `paket/markerbericht` von `main` (≥ `5f8e02a`) |
| Bearbeiter | ChatGPT; nach dem ersten Commit Übergabekommentar nach `docs/handoffs/VORLAGE.md`, eine **neue** ChatGPT-Sitzung übernimmt und beendet das Paket |
| Dateigrenzen | `tools/markerbericht.py` (neu), ein Schritt am Ende des Jobs `build` in `.github/workflows/build.yml` |
| Abnahme | Zahlen gleich `grep -rho "\[APPROX\]\|\[ORIGINAL\]" src \| sort \| uniq -c`; Ausgabe sortiert und deterministisch; keine neuen Abhängigkeiten; CI grün; PR nach Vorlage mit `tools/pruefen.sh`-Tabelle und Trailer |
| Review | Claude Code (Leitstelle, andere Sitzung als jede Umsetzung) |
| Status | **abgeschlossen** 28.10.2026: PR #69 gemergt (Übergabe ChatGPT → Claude Code vollständig), Review [markerbericht-69](reviews/2026-10-28-markerbericht-69.md) ohne Befunde |

## Runde 3, Welle C (ab 29.10.2026)

**Base:** `main` `51c110a` (Welle B abgeschlossen). Zwei unabhängige Spuren, je ein Branch und Draft-PR, kleine PRs.
Danach Abschluss von Runde 3 (Archiv, offene Restlücken nach Runde 4).

### Spur `speichern-dialog` – Laden und Sichern im Spiel (Abnahme 6 aus `speichern`)

- **Ziel:** Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname) auf dem gemergten Format;
  Einstellung „Automatisch speichern“.
- **Belege:** Grafiken und Texte aus em.exe, Referenzbilder des Dialogs, soweit im Video vorhanden (im Kapitelindex
  `docs/referenz-videos.md` bisher nicht verzeichnet – zuerst suchen); Zeitpunkt und Dateiname des automatischen
  Speicherns belegen oder `[APPROX]`. Ohne Videobild: Pixelvergleich entfällt, im PR begründen.
- **Abnahme:** (1) Öffnen und Abbrechen ändert keinen Zustand (Zustandsabdruck vorher = nachher); (2) Sichern → Laden
  über den Dialog ergibt denselben Zustandsabdruck; fremde Datei und höhere Version zeigen die Meldung, laufender
  Zustand unverändert; (3) „Automatisch speichern“ schreibt zum belegten Zeitpunkt genau eine Datei, nie während
  `Continue()` mitten im Tag; (4) Bildschirmfoto-Ablauf `speichern`, Pixelvergleich zum Referenzbild, falls vorhanden.
- **Dateigrenzen:** `UI/Screens/SaveLoad/**`, `Tools/Scenarios/Speichern.cs`, `docs/formate/spielstand.md`;
  minimal und begründet: Anmeldung über `[MenuEntry]` (kein Eingriff in `MainMenuScreen.cs`), „Laden“ im
  Titel-/Startbildschirm, `UI/Screens/Settings.cs`/`SettingsScreen.cs`, Aufruf des automatischen Speicherns im
  Tagesabschluss (`GameState.Integration.cs`). `Core/SaveGame*.cs` nur für eine nötige API, kein Formatwechsel.
- Nicht enthalten: Import `*.HOK`, #80.

### Status Spur `speichern-dialog`

Stand 29.10.2026: umgesetzt, im Review (Branch `paket/speichern-dialog`, Base `f642ef8`, Bearbeiter Claude Code).
Dialog „Laden“/„Sichern“ (Kacheln, Notizbuch der Spielvorbereitung), Einstellung „Automatisch speichern“ (Standard an),
automatisches Speichern nach dem Zähler aus `em.exe`. Tests: `SaveLoadScreenTests` (Abnahme 1–3, Mehrsaisonprobe),
Bildschirmfoto-Ablauf `speichern` (Abnahme 4). Belege und `[APPROX]`: `docs/formate/spielstand.md`, Teil `-dialog`.
Offen: Laufwerksknöpfe ohne Wirkung, „.“/„..“-Einträge fehlen, Hot-Seat-Zählung nur über Code belegt (kein Test).

### Spur `wettbewerbe-weltkugel` – Spielmodus „nur Weltkugel“ (Paket `wettbewerbe`, PR 7)

- **Ziel:** wie in `docs/wettbewerbe.md` („Noch offen“): Auswahl des Spielmodus in der Spielvorbereitung (Fahne/Weltkugel,
  DGROUP 0xD0B9), Überspringen der Vereinssaison bis zum ersten WM-Termin, Ernennung zum Bundestrainer zu jeder Saison,
  Hauptmenü des Bundestrainers mit Kachelfreischaltung; macht „WM Vorbereitung“ und WM-Kader erreichbar.
- **Abnahme:** (1) Modus 1/2/4 aus der Spielvorbereitung wie im Original, Test gegen die Bitwerte; (2) im Modus
  „nur Weltkugel“ läuft eine Saison über `Continue()` ohne Liga- und Vereinsbuchungen bis zur WM und durch sie hindurch;
  (3) Mehrsaisonprobe 4 Wechsel im Modus 1 (Ernennung jede Saison, Nationalkonto = Summe seiner Buchungen) und
  unveränderte Referenzwerte im Modus 4; (4) Bildschirmfoto-Abläufe Spielvorbereitung und Bundestrainer-Menü mit
  Pixelvergleich; Bildschirme zustandsrein.
- **Dateigrenzen:** `Core/Cups*.cs` (inkl. `CupsNational*.cs`), `UI/Screens/Competitions/**`, `Tools/Scenarios/Wettbewerbe.cs`;
  gemeinsame Dateien laut `wettbewerbe.md` minimal und begründet: `SetupScreen.cs`, `MenuTiles.cs`/`MainMenuScreen.cs`
  (Freischaltung), `SeasonFlow.cs`, `GameState.Integration.cs`. Spielstand: neuer Zustand mit Schemaversion + Migration.
- **Abhängigkeit:** berührt `GameState.Integration.cs` wie `speichern-dialog` – getrennte Aufrufe, Konflikt beim Zweiten
  per Rebase lösen.

### Status Spur `wettbewerbe-weltkugel`

Stand: implementiert, lokale Abnahme grün; Review und Merge stehen aus (PR #87, Branch `paket/wettbewerbe-weltkugel`).

- [x] (1) Spielmodus aus Fahne/Weltkugel wie im Original (Bits → 1/2/4, Fahne nur mit einem Manager, ohne Modus kein
  Start); Tests `GameModeTests.Mode_*`, `WorldOnlyMenuTests.Setup_Buttons_GiveOriginalModes`.
- [x] (2) Modus 1: Sprung zum ersten WM-Termin über `Continue()`, Ernennung, WM vollständig, keine Liga, keine
  Vereinsbuchungen (`WorldOnly_SeasonRunsThroughWorldCup_WithoutLeagueOrClubBookings`).
- [x] (3) Mehrsaisonprobe Modus 1, 4 Wechsel, passiv/aktiv (Ernennung jede Saison, Nationalkonto = Summe der
  Buchungen); Referenzwerte Modus 4 unverändert (`Mode4_ReferenceValues_Unchanged`, aufgenommen auf Base f642ef8).
- [x] (4) Bildschirmfoto-Ablauf `weltkugel`; Pixelvergleich der Modusknöpfe mit dem Video (2:13–2:30), Menü des
  Bundestrainers ohne Videobild; Bildschirme zustandsrein (`WorldOnly_CoachMenu_LocksAndRoutes_AndOpeningChangesNothing`).
- Spielstand Schema 3 mit Migration (Fahne, Nationalbuch). Belege und `[APPROX]`: [wettbewerbe.md](wettbewerbe.md).
- Offen: Aufstellung/Taktik des Nationalteams (Platzhalter), Bildschirme „Tabelle DEB-Pokal“/„Tabelle Europapokal“ (Kacheln wie im Original, Platzhalter),
  weitere dynamische Sperren des Originals (Werbung, Neuer, Highscore), Bundestrainer im Modus 4.

### Zurückgestellt (Entscheidung Nutzer)

- Paket `wettbewerbe` PR 8 „Paarungen Liga“: kein Bildschirm im Referenzvideo nachgewiesen.
- `finanzen`: Monatsausgaben ESC Wolfsburg (87.370 statt 73.977 DM) und unabhängige Nachdisassembly der übrigen
  `[ORIGINAL]`-Fundstellen.

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
| 7 | Spielmodus „nur Weltkugel“: Spielvorbereitung, Vereinssaison überspringen, Menü des Bundestrainers | eingereicht (#87, Spur `wettbewerbe-weltkugel`) |
| 8 | Paarungen Liga (kein Videobeleg) | offen |
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | **wartet auf `livespiel`** (Tore, Vorlagen, Strafen je Spieler) |

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: **Runde 3, Welle B** – Plan und Abnahme im Abschnitt „Spur `speichern`“ oben

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`).

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
