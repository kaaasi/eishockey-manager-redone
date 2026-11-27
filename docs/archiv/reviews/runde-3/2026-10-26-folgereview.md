# Eishockey Manager Redone – Folgereview nach Integration Runde 1

**Datum:** 26.10.2026 · **Repository:** `kaaasi/eishockey-manager-redone` · ausschließlich lesende Prüfung

**Basis:** `c5c2eb7adbc2eb5afc12c65a7f07f95c13d33877` (Audit vom 25.10.)

**Geprüfter neuer Stand:** `e1ac97ec8d0ec2a324b6d0ef32a6a96767650086` auf `main`

## Bewertung

**Ein erheblicher und sinnvoll strukturierter Funktionsfortschritt, aber noch keine abgeschlossene fachliche Integration.** Mannschaft, Finanzen und Saisonablauf sind jetzt tatsächlich verbunden. Der nächste Schritt sollte eine gezielte Stabilisierungsrunde sein, bevor weitere Wettbewerbe oder das endgültige Speicherformat darauf aufbauen.

Besonders positiv sind der nun direkt aus der DOS-EXE übernommene Kalender, die belegte Trainingstabelle und der Übergang zu echten Paket-PRs mit einer gemeinsamen Integrationsrunde. Die Leitstelle hat mehrere offene Schnittstellen bereits selbst dokumentiert. Zusätzlich wurden in diesem Review Fehler reproduziert, die bisher nicht ausreichend durch Tests abgesichert sind.

**Empfehlung:** Als Entwicklungszwischenstand weiterverwenden; noch keine Freigabe als stabiler Mehrsaison-/Mehrmanagerstand oder als originalgetreue Wirtschaftssimulation. Keine Notwendigkeit, die Architektur neu aufzusetzen.

## 1. Was konkret geprüft und ausgeführt wurde

- Vergleich der beiden festen Commits: **21 neue erreichbare Commits, 78 geänderte oder neue Dateien**.
- Aktuelle Implementierungen und Tests insbesondere für Finanzen, Training, Talentsuche, Transfers, Saisonwechsel und UI-Einstiege; dazu Änderungen an Organisation und Dokumentation.
- **15 zusammengeführte PRs**, darunter Integrations-PR #15; aktueller Build mit Testprotokoll und drei Publish-Jobs.
- Das veröffentlichte **Linux-Artefakt des geprüften Commits** heruntergeladen und dessen unveränderte Core-/UI-/Formats-Assemblies ausgeführt.
- Mit frisch aus der gelieferten ZIP entpackten und vor/nach dem Lauf abgeglichenen Originaldateien **alle 15 Screenshot-Szenarien erfolgreich ausgeführt: 97 PNGs**. Stichproben der Darstellung wurden angesehen. Kein Vergleich mit Originalvideos, da diese weiterhin nicht vorliegen.
- **Zehn gezielte Laufzeitproben** gegen die veröffentlichten C#-Assemblies, unter anderem zu Gehaltsabrechnung, Ansichtswechsel, Talentdaten, Mehrmanagerbetrieb, Vereinswahl, Saisonwechsel, Angebotswiederholung und Nachrichtenfristen. Die Assemblies wurden über Python/.NET geladen; es handelt sich nicht bloß um eine Nachbildung der Formeln in Python.
- Kalender und Trainingstabelle direkt mit den Originalbytes verglichen.

Die komplette xUnit-Suite wurde hier nicht neu gebaut oder erneut ausgeführt; dafür fehlt weiterhin ein .NET-SDK. Die Suite-Ergebnisse unten stammen aus dem aktuellen CI-Protokoll. Die eigenen Laufzeitprüfungen sind davon unabhängige zusätzliche Belege.

**Repository unverändert:** keine Commits, Branches, Kommentare, formellen Reviews, Workflowstarts, Merges oder Einstellungen verändert. Alle Laufzeitproben arbeiteten mit lokalen Kopien und Spielzuständen im Speicher. Auch die frisch entpackten Originaldateien waren nach den Szenarien bytegleich zur ZIP.

## 2. Nachweisbarer Fortschritt

| Bereich | Neuer Stand | Einordnung |
|---|---|---|
| Mannschaft | Spielerattribute, Kaderinitialisierung, Aufstellung, Taktik, Verträge, Training, Transfers und zugehörige Screens | Wesentlich mehr als der frühere Oberflächenprototyp |
| Spielstärke | `MatchEngine` verwendet nun `Club.MatchStrength` aus Aufstellung, Energie und Einsatz | Früher fehlender Anschluss ist vorhanden; Formel bleibt `[APPROX]` |
| Saison | Playoffs, Aufstiegsrunden, Auf-/Abstieg, Saisonende, gewählte Spieldauer, Historie, Entlassung | Mehrjährige Abläufe sind umgesetzt, nicht nur geplant |
| Finanzen | Finanzbuch, Bank, Aktien, Werbung, Stadion, Jugend und Posteingang | Breiter Funktionsumfang; reale Gehaltsverträge sind noch nicht angebunden |
| Integration | `GameState.Integration.cs` verbindet Tages- und Saisonereignisse | Richtiger Ansatz; UI-Nachholsimulation und einige Inselmodelle bestehen weiter |
| Prozess | Paket-PRs und gemeinsamer Integrations-PR; acht Integrationsfehler laut PR-Beschreibung behoben | Deutliche Verbesserung gegenüber rein linearen Direktcommits |
| Tests | CI meldet 162 Fälle gegenüber vorher 50 | Mehr Core-Tests; UI-Erfolg weiterhin durch fehlende Daten verzerrt |

### Originalbelege, die ich selbst bestätigen konnte

**Kalender:** Die 232 Bytes der neuen `SeasonCalendar`-Tabelle stimmen **vollständig und ohne Abweichung** mit `em.exe` ab Dateioffset `0x175F9` überein: 44 Ligatermine, 15 Pokal-, 12 Europa-, 15 Playoff- und 8 WM-Termine sowie zwei Endmarkierungen. Die frühere Vereinfachung auf einen regelmäßigen Sonntag-/Mittwochskalender wurde damit erheblich verbessert. Die Benennung der Wettbewerbsbits und die spezielle Wochentagsanzeige am Saisonende sind damit allein noch nicht vollständig bewiesen.

**Training:** Die 108 Bytes der Belastungstabelle in `Squad.Training.cs` stimmen mit DGROUP `0x4861` der Original-EXE überein. Das belegt die Tabelle, nicht automatisch jede Zeile der nachgebauten Wirkungsfunktion.

**Schwierigkeitsgrenze:** Die Originalbytes an DGROUP `0x11C` lauten `5, 3, 2, 2`. Die neue Doku nennt bereits Level bis 5; `ManagerSetup.LevelCount` steht jedoch weiterhin auf 4. Die Oberfläche bietet deshalb nur 0–3 an. Die neu berücksichtigten Trainingsfälle für Level 4/5 sind im regulären Setup nicht erreichbar.

## 3. Priorisierte Befunde

P1 bedeutet hier: vor Abnahme der betroffenen Spielfunktion beziehungsweise vor weiterem Ausbau ihrer Datenverträge beheben. P2 bedeutet: im nächsten Integrationsschritt absichern. Bereits von Claude dokumentierte Restarbeiten werden ausdrücklich als solche gekennzeichnet.

### R2-01 · P1 · Vertragliches Gehalt und Versicherung wirken nicht auf die Abrechnung

**Fundstellen:** `Core/Finance.cs:91`, `Core/GameState.Manager.cs:67,131`, `Core/Player.Contract.cs:83`.

Die Monatsabrechnung nutzt weiterhin

`max(Kadergröße, 20) × (1.400 + 80 × Vereinsstärke)`.

`Contracts.MonthlyCosts(club)` summiert dagegen die tatsächlichen Einzelgehälter und Versicherungen, wird aber von der Finanzabrechnung nicht verwendet.

**Eigene Laufzeitprobe mit Originaldaten:** Die vertraglichen Monatskosten eines Kaders steigen durch Gehaltserhöhung und Versicherung von **33.500 auf 143.690 DM**. Die von der Finanzlogik angesetzten Gehälter bleiben unverändert bei **175.200 DM**.

**Folge:** Der Vertragsbildschirm bietet wirtschaftliche Entscheidungen an, deren Kosten ignoriert werden. Die bisherige Finanzkalibrierung kann deshalb keine belastbare Aussage über Spielbalance liefern.

**Empfehlung/Abnahme:** Für reale Kader die Vertragssumme verwenden; Versicherung bewusst getrennt buchen oder dokumentiert einbeziehen. Einen eventuellen synthetischen Fallback ausdrücklich begrenzen. Integrationstest: Vertrag ändern, über `Continue()` den nächsten Monat erreichen, genau die neuen Gehälter und Versicherungsbeiträge im Finanzbuch und Vermögen nachweisen. Die vorhandenen Tests vergleichen derzeit größtenteils die Abrechnung mit derselben pauschalen Formel und können diesen Anschlussfehler nicht erkennen.

### R2-02 · P1 · Öffnen einer Mannschaftsansicht rechnet Training und Geld vorzeitig ab

**Fundstellen:** `UI/Screens/Team/LineupScreen.cs:52`, `TransferScreen.cs:63–66`, `TrainingScreen.cs:67`; `Core/Squad.Training.cs:421`; `Core/GameState.Integration.cs:10–19`.

Die zentrale Zeitleiste ruft Training auf, doch die Bildschirme rufen zusätzlich weiterhin `CatchUpTraining()` auf. Das ist keine bloß überflüssige zweite Abfrage: `Continue()` hält bereits am Tag des nächsten Spiels an, bevor dieser Tag verarbeitet wurde. Ein Bildschirm holt das Training dieses Tages deshalb vor der nächsten Weiter-Aktion nach.

**Eigene Probe über den tatsächlichen `ScreenDriver` und `LineupScreen`:** Ein Oberligamanager hat am Tag 3 **400.000 DM**, Training verarbeitet bis Tag 2. Nach Öffnen von „Aufstellung“ steht das Datum weiterhin auf Tag 3, der Kontostand aber auf **393.800 DM** und der Trainingszähler auf Tag 3. Der reine Ansichtswechsel löst also eine **6.200-DM-Buchung** aus.

**Folge:** Der Zustand hängt davon ab, welche Ansichten vor der eigentlichen Tagesverarbeitung geöffnet wurden. Insbesondere werden Trainingspläne unter Umständen mit dem alten Stand angewandt, bevor der Nutzer sie im Trainingsfenster ändern kann. Simulation und Anzeige sind nicht sauber getrennt.

**Bereits von Claude erkannt:** `docs/arbeitspakete.md` nennt die CatchUp-Aufrufe in den Bildschirmen als zu entfernen. Die Laufzeitprobe bestätigt, dass dieser Punkt jetzt Funktionsrelevanz hat.

**Empfehlung/Abnahme:** Zeitereignisse ausschließlich an der festgelegten Stelle der Tagesverarbeitung auslösen; UI-Einstiege dürfen keine Zeit nachholen. Test: bei pausiertem Tag alle Ansichten öffnen/schließen – Spielerwerte, Zufallszustand, Geld und Markt müssen unverändert bleiben. Danach einen Tag genau einmal abwickeln. Die Reihenfolge „Plan ändern → Training“ ausdrücklich festlegen.

### R2-03 · P1 · Die Jugendtalentsuche erzeugt unvollständige Spieler

**Fundstellen:** `Core/FinanceYouth.cs:149,160–170`; daneben `Core/Transfer.Talent.cs`.

Der Jugendpfad fügt lediglich `new Player { Name, Role }` hinzu. Nach Einführung der Spielerattribute reicht das nicht mehr aus. Der Transferpfad hat dagegen eine eigene umfangreiche Initialisierung. Die doppelte Talentsuche ist bereits dokumentiert, ihre unterschiedlichen Dateninvarianten sind aber ein unmittelbarer Fehler.

**Eigene Laufzeitprobe:** Reguläre Talentsuche starten und die Saison über `Continue()` bis zum Ergebnis fortsetzen. Bei Erfolg entsteht ein Spieler mit **Alter 0, Rückennummer 0, Kondition 0, Technik 0, Form 0, Energie 0, Vertragsdauer 0 und Gehalt 0**.

**Folge:** Ein scheinbar erfolgreich gewonnenes Talent ist kein regulär initialisierter Spieler. Aufstellung, Kosten, Transfers und spätere Speicherung verarbeiten dadurch ungültige oder sachlich unvollständige Daten.

**Empfehlung/Abnahme:** Zentrale Spielerfabrik für Jugend, Transfermarkt und Spielstart; ein gemeinsamer Suchauftrag mit einheitlichen Preisen, Grenzen und Ergebniszuständen. Test jedes erfolgreichen Zugangspfads auf Alter, eindeutige Rückennummer, gültige Werte und Vertrag. Unterschiedliche Suchdauern dürfen nur als bewusst bestätigte Produktentscheidung bestehen bleiben.

### R2-04 · P1 · Saisonwechsel aktualisiert Spieler und Leihen nicht

**Fundstellen:** `Core/GameState.Integration.cs:25–30`, `Core/SeasonFlow.cs:339–371`, `Core/Player.Contract.cs:15–16`.

Der Teamanschluss am Saisonwechsel setzt derzeit nur den Transfermarktzähler zurück. Altern, Vertragsverkürzung/-ende und Leihende fehlen. Dies ist von Claude ausdrücklich als offene Arbeit markiert; es sollte entsprechend nicht als abgeschlossene Mehrsaisonsimulation gelten.

**Eigene Laufzeitprobe:** Ein 27-jähriger Spieler mit Restvertrag 1 und `OnLoan=true` verbleibt nach Abschluss und Neustart der Saison **27 Jahre alt, mit Restvertrag 1 und weiterhin ausgeliehen im selben Kader**.

**Empfehlung/Abnahme:** Saisonübergang als gemeinsame Transaktion definieren: Alter, Laufzeit, Ablauf, Leihen, offene Angebote, Aufstellung und Markt konsistent aktualisieren. Für Leihen gegebenenfalls ursprünglichen Eigentümer/vereinbarten Rückkehrort explizit speichern – ein boolesches Flag allein bildet nicht jeden gewünschten Leihvertrag ab. Erst diesen Vertrag stabilisieren, dann das Speicherformat festlegen.

### R2-05 · P1 · 19 UI-Fälle werden in CI als bestanden gemeldet, obwohl sie nicht prüfen

**Fundstellen:** `tests/EishockeyManager.UI.Tests/FlowTests.cs` sowie die neuen UI-Testklassen; unveränderter Buildworkflow.

Der aktuelle CI-Lauf meldet **110 Core + 31 Formats + 21 UI = 162 bestandene Fälle**. Von den 21 UI-Fällen benötigen **19** Originaldaten. Ohne diese Dateien verlassen sie den Test direkt oder über die Hilfsmethode `Open()` erfolgreich. Nur die beiden datenfreien Registrierungsprüfungen laufen tatsächlich.

Die frühere Auditlücke A02 ist somit weiterhin vorhanden und betrifft inzwischen erheblich mehr Fälle. „162 Tests grün“ darf nicht als „162 fachliche Testabläufe ausgeführt“ gelesen werden.

**Empfehlung/Abnahme:** Echte Skip-Meldung für optionale Originaldatenläufe; ein ausdrücklich angeforderter Integrationslauf muss bei fehlenden Dateien scheitern. Ergebnisse getrennt veröffentlichen: synthetische Tests, ausgeführte Originaldaten-Tests, übersprungene Fälle. Den echten Originaldatenlauf kontrolliert lokal/privat ausführen, ohne Spieldateien ins Repository aufzunehmen.

**Abgrenzung:** Meine eigenen 15 erfolgreichen Screenshot-Szenarien sind ein zusätzlicher Ausführungsbeleg. Sie ersetzen weder die 19 xUnit-Testkörper noch einen Bildvergleich mit dem Original.

### R2-06 · P1 · Regulärer Hot-Seat-Zugwechsel fehlt weiterhin

**Fundstellen:** `Core/GameState.cs:77`, `Core/SeasonFlow.cs` und `UI/Screens/MainMenuScreen.cs`.

Es gibt jetzt eine Umschaltung des aktiven Managers nach einer Entlassung. Eine reguläre Bedien-/Bereitschaftsphase aller Manager vor der Tagesverarbeitung fehlt weiterhin.

**Eigene Laufzeitprobe:** Zwei Manager, fünfmal `Continue()`: aktive Managerindizes **0,0,0,0,0,0**; die Saison ist trotzdem um fünf Ligaspieltage fortgeschritten.

**Folge:** Mitmanager-Kredite und mehrere angelegte Manager sind technisch vorhanden, der zweite Manager erhält im normalen Bedienfluss aber keine reguläre eigene Runde.

**Empfehlung/Abnahme:** Bereitschaftsphase mit 2–4 aktiven Managern und exakt einmaliger Simulation pro gemeinsam bestätigtem Tag. Entlassene Manager ausschließen. Bis dahin Hot-Seat sichtbar als unvollständig behandeln.

### R2-07 · P1 · Feste und zufällige Vereinszuordnung kann weiterhin doppelt belegen

**Fundstelle:** `Core/NewGame.cs:35–53,77–86` (alter Befund A04).

**Eigene Probe mit der Originaldatenbank:** Seed **0**, erster Manager zufällig aus dem Bundesliga-Mittelfeld, zweiter Manager fest auf Verein **4** → beide Manager erhalten Verein **4**.

**Folge:** Die gleiche Mannschaft wird zwei Managern zugeordnet; Gehälter, Heimspielerlöse und Mannschaftsentscheidungen können dadurch inkonsistent werden. Die neue Kaderinitialisierung gruppiert Manager zwar nach Verein, verhindert die Doppelbelegung aber nicht.

**Empfehlung/Abnahme:** Feste Wahlen aller Manager zuerst reservieren und validieren, danach Zufallsauswahl/Verdrängung durchführen. Tests mit gemischten Wahlarten, mehreren Reihenfolgen und Seeds; pro Manager genau ein exklusiver Verein.

### R2-08 · P2 · Nachrichtenfristen überleben den Saisonwechsel falsch

**Fundstellen:** `Core/FinanceInbox.cs:106–110`, `Core/GameState.Manager.cs:97–103`.

Nachrichten speichern nur einen Saisontag. Beim Saisonwechsel wird `FinanceDay` auf −1 zurückgesetzt, die Inbox bleibt erhalten. Die Differenz `FinanceDay - Message.Day` wird damit negativ.

**Eigene Laufzeitprobe:** Kreditanfrage an Tag **231** stellen, nächste Saison starten und acht Weiter-Schritte ausführen. Bei Tag **28** der neuen Saison ist die nach sieben Tagen verfallende Anfrage noch immer eine offene Frage.

**Prüfgrenze:** Die Anfrage an Tag 231 wurde über die öffentliche Core-API erzeugt. Dass sie in diesem Moment auch über den aktuellen Finanzbildschirm erstellt werden kann, wurde nicht nachgewiesen. Die Fristenlogik ist dennoch an der Saison-/Persistenzgrenze inkonsistent.

**Empfehlung/Abnahme:** Eindeutige fortlaufende Zeit oder Saison+Tag mit wohldefinierter Fristsemantik verwenden. Alternativ Anfragen am Saisonwechsel ausdrücklich erledigen. Test einer Anfrage unmittelbar vor dem Wechsel und ihrer Annahme nach Fristende.

### R2-09 · P2 · Wiederverwendung eines Transfergebots bucht erneut und dupliziert den Spieler

**Fundstelle:** `Core/Transfer.cs:272–277` und `Move()`.

`Accept()` prüft nicht, ob das Gebot noch offen beziehungsweise das Listing noch vorhanden ist. Nach Entfernen des Angebots kann dasselbe Objekt erneut angenommen werden.

**Eigene Laufzeitprobe:** Angebot über **204.000 DM** zweimal annehmen → Kontostand **400.000 → 604.000 → 808.000 DM**; derselbe Spieler ist anschließend **zweimal als identische Objektreferenz** im Käuferkader enthalten.

**Einordnung:** Dies wurde über die Core-API reproduziert. Ein entsprechender doppelter Klick im aktuellen UI ist damit nicht nachgewiesen. Der Befund betrifft Transaktionssicherheit und wird spätestens bei Speichern, Wiederaufnahme oder Netzwerk besonders relevant.

**Empfehlung/Abnahme:** Gültigkeit, Eigentum und offenen Zustand vor der Buchung prüfen; genau einmal ausführen oder Wiederholung ohne Wirkung ablehnen. Den Spielerwechsel und beide Geldseiten als eine Operation behandeln. Negativtests für zweimaliges Annehmen, zurückgezogenes Listing und veraltetes Gebot.

## 4. Bereits bekannte Restarbeiten richtig priorisieren

Claude führt mehrere Punkte transparent selbst als offen auf. Diese sollten nicht als überraschend neue Auditfehler bewertet werden, sind aber vor einer fachlichen Abnahme relevant:

- **Keine Heimspieleinnahmen in Playoffs/Aufstiegsrunden/Pokalen.** `SettleFinances()` wird derzeit nur vom Ligapfad aus erreicht. Zusätzliche erfolgreiche Spiele werden finanziell nicht entsprechend abgebildet. Vor Balancekalibrierung die Spielabrechnung vereinheitlichen.
- **Saisonprämien umgehen das Finanzbuch.** Direkte Änderung von `Money` in `SeasonFlow.cs:312`. Eine eigene Prämienkategorie fehlt. Monatliche Finanztests vor der Saisonprämie decken diesen Bruch nicht ab.
- **Doppelte Talentsuche und UI-CatchUp.** Jetzt durch Laufzeitproben als konkrete Inkonsistenzen bestätigt, nicht nur Wartungsarbeit.
- **Spieleralter, Verträge und Leihen.** Bekannte Funktionslücke, die einen stabilen mehrjährigen Spielstand verhindert.
- **Finanzkalibrierung.** Erst nach korrekter Gehalts-, Versicherungs-, Nachsaison- und Prämienabrechnung sinnvoll. Ein passiver Oberligaverein mit negativem Kontoverlauf ist derzeit kein isolierter Beleg für zu geringe Einnahmen; auf der Ausgabenseite wird noch eine Ersatzformel verwendet.

## 5. Status der zwölf Befunde aus dem ersten Audit

| Alter Befund | Status im neuen Stand |
|---|---|
| A01: Stärketripel der 86 Europapokalgegner verkürzt | **Offen.** `ClubDatabase` ist unverändert. Vor `wettbewerbe` alle Rohwerte erhalten; Bedeutung separat rekonstruieren. |
| A02: UI-Tests ohne Originaldaten als Passed | **Offen, größere Reichweite.** Jetzt 19 UI-Fälle. |
| A03: Hot-Seat-Zugwechsel | **Offen.** Wechsel nur bei Entlassung ergänzt; reguläre Runde fehlt. |
| A04: Zufalls-/Festwahl kollidiert | **Offen, jetzt im veröffentlichten Code reproduziert.** |
| A05: Alte Evidenz und Ist-Stand vermischt | **Teilweise verbessert.** Neue Kalender-/Trainingsoffsets und Overlay-Mapping helfen. Alte Behauptungen über 10-Sekunden-Simulation bleiben unzutreffend; Spielsimulation ist weiterhin Poisson. |
| A06: Unzureichende Paketverträge | **Deutlich verbessert, nicht abgeschlossen.** Tages-/Saisonhooks und MatchStrength sind vorhanden; Gehalt, Talente, Zustandsfristen und UI-Nachholung sind konkrete Restkonflikte. |
| A07: Kacheltest lässt Platzhalter bestehen | **Teilweise verbessert.** Viele echte Screens und spezifische Tests ergänzt. Der generische Kacheltest unterscheidet weiterhin nicht zwischen Feature und Platzhalter. |
| A08: ScreenDriver-Timeout ohne Fehler | **Offen.** `PlayMatchDay()` endet nach 1.000 Versuchen weiter still. |
| A09: Nicht festgelegte Toolchain/Pakete | **Offen.** SDK-/MonoGame-Wildcards und überschreibbares Werkzeugrelease bleiben. |
| A10: Parsergrenzen | **Offen.** Formats-Code ist unverändert. Keine neue Fehlfunktion mit frischen gültigen Originaldateien nachgewiesen. |
| A11: Cross-Publish ohne Zielsystemstart | **Offen, zusätzlicher Teilbeleg.** Linux-Headless-Werkzeug lief hier erfolgreich. Das bestätigt keinen Windows-/macOS-Fensterstart. |
| A12: Wiederholte Tabellenberechnung im Draw | **Offen.** Gegenwärtig nachrangig gegenüber fachlicher Korrektheit. |

Zusätzlich: Level 4/5 sind trotz neuer Originalbelege weiterhin nicht im Setup auswählbar. Als kleine separate Originalitätskorrektur einplanen, inklusive Kader-/Trainingsverhalten für diese Level.

## 6. Entwicklungsprozess und CI

Die neue Integrationsorganisation ist ein Fortschritt: Die drei Pakete waren einzeln grün, die Leitstelle stellte beim Zusammenführen acht fehlschlagende Tests fest und ergänzte daraufhin zentrale Anschlusspunkte. Das ist genau der Bereich, in dem unabhängige Gegenprüfung jetzt den größten Nutzen hat.

Der finale Stand ist durch [Build 40295709378](https://github.com/kaaasi/eishockey-manager-redone/actions/runs/40295709378) abgesichert: Build erfolgreich, 0 Buildwarnungen/-fehler, drei Publish-Jobs erfolgreich. Der getestete Umfang ist aber wie oben beschrieben zu interpretieren. Line-/Branch-Coverage wird weiterhin nicht gemessen.

[PR #15](https://github.com/kaaasi/eishockey-manager-redone/pull/15) dokumentiert lokale 162 Tests und 15 Bildabläufe. Meine 15 Szenarien sind ein unabhängiger positiver Teilbeleg. Die Behauptung des lokalen vollständigen xUnit-Laufs habe ich nicht selbst wiederholt. Zum Abrufzeitpunkt sind keine formellen Reviews an PR #15 vorhanden; die Prüfungen im Claude-Hauptchat sind dadurch nicht widerlegt, aber nicht als getrennte GitHub-Reviewentscheidung nachvollziehbar.

Die neuen Regeln „Zeit nur über Continue“ und „Geld über Book“ sind nützlich, werden aber noch nicht durch Tests oder vollständige Migration erzwungen. Bei der Geldregel sollten echte Einnahmen/Ausgaben von neutralen Umbuchungen unterschieden werden: Bankeinzahlungen und Aktienkäufe sind nicht automatisch Aufwand. Nicht pauschal jede bestehende `Money`-Änderung durch eine Aufwandsbuchung ersetzen.

## 7. Konkrete Empfehlungen an die Leitstelle

### Korrekturrunde zuerst in vier kleinen PRs

| PR | Umfang | Verbindliche Abnahme |
|---|---|---|
| **1 – Tests und passive Ansichten** | R2-02/R2-05, CatchUp aus Screens entfernen, Tageszeitpunkt eindeutig festlegen, ScreenDriver-Timeout scharf machen | 19 Originaldatenfälle sichtbar korrekt ausgeführt/übersprungen; Ansichtswechsel verändert keinen Simulationszustand; jeder Tag genau einmal |
| **2 – Mannschaft und Finanzen verbinden** | R2-01, gemeinsame Talentinitialisierung R2-03, Saisonprämien buchen, Nachsaisonspiele abrechnen | Gehaltsänderung verändert echte Monatskosten; kein Talent mit Nullattributen; jede Zahlung nachvollziehbar und einmalig |
| **3 – Saison-/Transaktionsgrenzen** | R2-04/R2-08/R2-09: Spielerlebenszyklus, Leihende, Fristen, veraltete Angebote | Saisonwechsel aktualisiert Kader vollständig; abgelaufene Anfrage nicht mehr annehmbar; doppeltes Gebot ohne zweite Wirkung |
| **4 – Mehrmanager und Originaldaten** | R2-06/R2-07, drei Stärkewerte erhalten, Setup-Level prüfen | Alle Manager erhalten ihre Runde; exklusive Vereine; Tripel vollständig; Levelumfang entspricht beschlossenem Originalverhalten |

Die PRs nach fachlicher Abhängigkeit schneiden, nicht allein nach Dateizuständigkeit. Zwei Implementierer sollten beispielsweise nicht gleichzeitig konkurrierende Talentfabriken oder alternative Gehaltsregeln ergänzen.

### Erst danach weitere Pakete

- `wettbewerbe`: nach gemeinsamer Spiel-/Finanzabrechnung und Erhalt der drei Stärkewerte starten; Regeln und Ersatzannahmen getrennt halten.
- `speichern`: Persistenzkonzept kann jetzt vorbereitet werden. Das endgültige Schema und ein „Fortsetzen verhält sich identisch“-Versprechen erst nach Kaderlebenszyklus, Fristen und Transaktionssicherheit festlegen. Zufallszustand, Objektidentität und offene Vorgänge einschließen.
- `sound`: fachlich weitgehend unabhängig, darf parallel weitergehen, sofern es die Stabilisierungsarbeit nicht in denselben Dateien behindert.
- Balance und Pixelparität: getrennte Abnahmen. Ein vollständiger Screenshotexport beweist noch keine Pixelidentität zum DOS-Spiel.

### ChatGPT im nächsten Schritt einsetzen

Claude implementiert jeweils einen der vier kleinen PRs. Danach einen festen Base-/Head-SHA an ChatGPT zur lesenden Nachprüfung geben. Schwerpunkt: zuerst die konkret reproduzierten Gegenbeispiele erneut ausführen, danach betroffene Aufrufer und neue Tests prüfen. Keine pauschale Freigabe anhand der Testanzahl. Reviewempfehlungen, technische Prüfergebnisse und akzeptierte `[APPROX]`-Abweichungen getrennt dokumentieren.

## 8. Kopierbarer Auftrag an Claude

```text
Bitte zunächst eine Stabilisierungsrunde vor weiteren Wettbewerben oder dem
endgültigen Speicherformat durchführen. Grundlage ist das unabhängige
Folgereview zum Commit e1ac97ec8d0ec2a324b6d0ef32a6a96767650086.

Bitte die Befunde selbst nachvollziehen und in kleinen PRs bearbeiten:
1. UI darf beim Öffnen keine Zeit, Training oder Geld fortschreiben.
   Originaldaten-Tests ohne Daten dürfen nicht als Passed erscheinen.
2. Gehalts-/Versicherungsabrechnung an die tatsächlichen Verträge anbinden.
   Beide Talentpfade über eine vollständige gemeinsame Spielerinitialisierung
   führen; Saisonprämien und Nachsaisonspiele konsistent abrechnen.
3. Saisonwechsel für Alter, Verträge und Leihen umsetzen; Fristen über
   Saisongrenzen stabilisieren; veraltete Transfergebote ohne Wirkung ablehnen.
4. Hot-Seat-Runden und exklusive Vereinsvergabe absichern. Alle drei
   Original-Stärkewerte erhalten und den Setup-Levelumfang prüfen.

Für jeden PR bitte angeben: Base-/Head-SHA, reproduzierter Vorherfehler,
konkreter Regressionstest, tatsächlich ausgeführte Tests und Restlücken.
Die vorhandene Architektur beibehalten. Keine reine Finanz-Neukalibrierung,
bevor die falsche Gehaltsabrechnung und fehlende Buchungen korrigiert sind.
Danach erneut unabhängig reviewen lassen.
```

## 9. Quellen und Belege

Alle Codezeilen beziehen sich auf den geprüften Head `e1ac97ec8d0ec2a324b6d0ef32a6a96767650086`.

- [Änderungsvergleich zum Erstaudit](https://github.com/kaaasi/eishockey-manager-redone/compare/c5c2eb7adbc2eb5afc12c65a7f07f95c13d33877...e1ac97ec8d0ec2a324b6d0ef32a6a96767650086)
- [Integration und offene Punkte](https://github.com/kaaasi/eishockey-manager-redone/blob/e1ac97ec8d0ec2a324b6d0ef32a6a96767650086/docs/arbeitspakete.md)
- [Zentrale Tages-/Saisonanschlüsse](https://github.com/kaaasi/eishockey-manager-redone/blob/e1ac97ec8d0ec2a324b6d0ef32a6a96767650086/src/EishockeyManager.Core/GameState.Integration.cs)
- [Pauschale Gehaltsformel](https://github.com/kaaasi/eishockey-manager-redone/blob/e1ac97ec8d0ec2a324b6d0ef32a6a96767650086/src/EishockeyManager.Core/Finance.cs#L91)
- [Tatsächliche Vertragssummen](https://github.com/kaaasi/eishockey-manager-redone/blob/e1ac97ec8d0ec2a324b6d0ef32a6a96767650086/src/EishockeyManager.Core/Player.Contract.cs#L83)
- [Training bei UI-Einstieg](https://github.com/kaaasi/eishockey-manager-redone/blob/e1ac97ec8d0ec2a324b6d0ef32a6a96767650086/src/EishockeyManager.UI/Screens/Team/LineupScreen.cs#L52)
- [Jugendspielerinitialisierung](https://github.com/kaaasi/eishockey-manager-redone/blob/e1ac97ec8d0ec2a324b6d0ef32a6a96767650086/src/EishockeyManager.Core/FinanceYouth.cs#L160)
- [Nachrichtenfristen](https://github.com/kaaasi/eishockey-manager-redone/blob/e1ac97ec8d0ec2a324b6d0ef32a6a96767650086/src/EishockeyManager.Core/FinanceInbox.cs#L106)
- [Gebot annehmen](https://github.com/kaaasi/eishockey-manager-redone/blob/e1ac97ec8d0ec2a324b6d0ef32a6a96767650086/src/EishockeyManager.Core/Transfer.cs#L272)
- [Saisonwechsel](https://github.com/kaaasi/eishockey-manager-redone/blob/e1ac97ec8d0ec2a324b6d0ef32a6a96767650086/src/EishockeyManager.Core/SeasonFlow.cs#L339)
- [CI-Build-/Testprotokoll](https://github.com/kaaasi/eishockey-manager-redone/actions/runs/40295709378/job/121481131887)

Laufzeitartefakt: `EishockeyManager-linux-x64-e1ac97ec8d0ec2a324b6d0ef32a6a96767650086`, Artifact-ID `12638755112`. Für die Proben wurden die veröffentlichten Assemblies daraus verwendet, ohne deren Produktcode zu ändern.

Originalreferenz: DOS V1.00 vom 10.05.1993; EXE-SHA-256 `f41eb08aa3bc3119bd83c44856ccc550d7a450d6ee55e0447378700fefb2294b`. Keine eigenständige Behauptung vollständiger DOS-Regelparität, kein neuer Vergleich gegen nicht vorliegende Originalvideos.
