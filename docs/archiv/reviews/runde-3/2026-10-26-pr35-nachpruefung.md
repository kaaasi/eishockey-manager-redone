# Nachprüfung des unabhängigen Reviews zu PR #35

- **Repository:** `kaaasi/eishockey-manager-redone`
- **PR:** https://github.com/kaaasi/eishockey-manager-redone/pull/35
- **Datum:** 26.10.2026
- **PR-Base:** `a4c4294dbc7afd67388b4f976338ee6281e73c41`
- **Bisher geprüfter Head:** `c4fca70ce5dc035df317a74615b388c15c053538`
- **Neu geprüfter Head:** `9bd8fea5907f8719f92e2ff091f2c87b1a3f6258`
- **Gegenstand:** zusätzlicher Commit `c4fca70..9bd8fea` mit sechs geänderten/neuen Dateien; Nachprüfung der beiden P2-Befunde aus [2026-10-26-pr35-review.md](2026-10-26-pr35-review.md).
- **Dokumentationsbranch:** `review/pr35-nachpruefung`, Basis `main` bei `05223eb787858c7ff3a6c02438f7d615840fb4d3`.
- Keine Änderungen am Produktcode, am Integrationsbranch oder an PR #35. Nur dieser Nachprüfungsbericht wird als eigener Dokumentations-PR abgelegt.

## Merge-Empfehlung

**Ja für Head `9bd8fea5907f8719f92e2ff091f2c87b1a3f6258`, im abgegrenzten Umfang von Runde 2/Welle B.**

**PR35-01 und PR35-02 sind behoben.** Die unveränderten bisherigen Gegenproben reproduzieren ihre Fehler nicht mehr. Ergänzende Proben bestätigen den Erhalt aller Spieler, eindeutige Rückennummern, konsistente Vertragslosenbestände sowie die Isolation gültiger Gebote und die Wiederholungsabwehr.

Keine neuen P0-/P1-/P2-Befunde. Es bleibt eine neue, **nicht mergeblockierende P3-Testlücke** zur Absicherung der Fehlermeldung. Die Freigabe beruht auf den eigenen Gegen- und Invariantenproben; die Testzahlen sind ergänzende technische Evidenz.

Diese Empfehlung ersetzt die negative Empfehlung des vorherigen Reviews **nur für den neuen Head**. R2-11 und R2-12 bleiben bekannte offene Runde-3-Arbeit.

## 1. Eigene Reviewergebnisse

### 1.1 PR35-01: Leihrückgabe an vollen Stammverein

**Status: behoben.**

Die bisherige Gegenprobe wurde als unveränderte Reviewerprobe gegen die neu gebauten Assemblies ausgeführt:

1. Originaldaten, Seed 3, Managerverein 42, Saisonende über `Continue()`.
2. Ersten Marktspieler mit einem Jahr Stammvertrag regulär ausleihen.
3. Stammverein EC KASSEL auf 99 Spieler mit eindeutigen Rückennummern auffüllen, seine Verträge auf 1 setzen.
4. `StartNextSeason()` aufrufen.

**Beobachtet:**

| Eigenschaft | Alter Head | Neuer Head |
|---|---|---|
| Ausnahme bei Rückgabe | Keine freie Rückennummer | Keine Ausnahme |
| Saison | bereits 1994 trotz Fehler | 1994, Übergang abgeschlossen |
| Spieler | weder Kader noch Pool | genau einmal im vertragslosen Pool |
| Alter | 30 → 31 | 30 → 31 |
| Leihstatus | gelöscht, Spieler verloren | `OnLoan=false`, `Loan=null` |
| Vertrag/Gehalt/Versicherung | keine konsistente Rückgabe | Vertrag 0, Gehalt 0, nicht versichert |

Die Kapazitätsauflösung erfolgt jetzt in [Player.Contract.cs:101](https://github.com/kaaasi/eishockey-manager-redone/blob/9bd8fea5907f8719f92e2ff091f2c87b1a3f6258/src/EishockeyManager.Core/Player.Contract.cs#L101). Die bisher widersprüchliche Kapazitätsvorprüfung entfällt; die übrige Validierung ungültiger Eigentums-/Leihzustände bleibt bestehen.

### 1.2 Zusätzliche Spieler- und Poolinvarianten

Gezielte Grenzfälle mit vollständig initialisierten Spielern:

| Stammkader vor Rückgaben | Leih-Rückkehrer | Stammkader danach | Neue Vertragslose wegen Überfüllung |
|---:|---:|---:|---:|
| 98 | 1 | 99 | 0 |
| 99 | 1 | 99 | 1 |
| 98 | 2 | 99 | 1 |

Die Ein-Leihspieler-Fälle verwenden den regulären Marktkauf als Leihe. Für den zusätzlichen Zwei-Rückkehrer-Grenzfall wurde die zweite gültige Leihvereinbarung gezielt im Testzustand über die Spielerfabrik angelegt; dies ist kein behaupteter zusätzlicher UI-Ablauf.

Nach jedem Übergang unabhängig geprüft:

- Die Menge der Spielerobjekte vor und nach dem Übergang ist identisch.
- Die Summe aus Kader- und Vertragslosenbeständen entspricht der Zahl der eindeutigen Spieler: kein Verlust und keine doppelte Zuordnung, auch nicht zwischen Kader und Pool.
- Alle ursprünglich vorhandenen Spieler altern genau einmal.
- In jedem Kader sind die Rückennummern eindeutig und liegen zwischen 1 und 99.
- Aufgestellte Spieler gehören weiterhin zu ihrem jeweiligen Kader.
- Vertragslose haben Vertrag 0, Gehalt 0, keine Versicherung und keine offene Leihvereinbarung.
- Der überzählige Spieler kann als ablösefreier Spieler regulär wieder verpflichtet werden: genau einmal im neuen Kader, aus dem Pool entfernt, neuer Vertrag 2 Jahre und positives Gehalt.

Der Pool wird beim Saisonwechsel mit neuen Listing-Identitäten aufgebaut. In den geprüften Grenzfällen entstanden weder doppelte Pool-Einträge noch verwaiste Spieler. Die neue Auflösung ist damit für die geprüften gültigen Ausgangszustände konsistent.

Die bestehende Gegenprobe `OnLoan=true` ohne gespeicherten Stammverein wirft weiterhin **vor jeder Mutation**; der vollständige Zustandsabdruck bleibt nach dieser Ausnahme unverändert. Die Auflösung der Kapazitätsgrenze wurde also nicht durch ein allgemeines Verschlucken von Validierungsfehlern erkauft.

### 1.3 PR35-02: ungültig gewordenes Verkaufsgebot

**Status: behoben.**

Die ursprüngliche Probe wurde unverändert ausgeführt: drei reguläre Verteidigerverkäufe reduzieren acht auf fünf; zwei weitere echte Gebote werden erzeugt; nach dem ersten Verkauf wird das zweite über die UI angenommen.

**Ergebnis:** Geld und Kader bleiben beim ungültigen zweiten Gebot unverändert, das Gebot wird entfernt und nicht erneut vorgelegt. Nach einem weiteren Klick ist weiterhin kein wiederkehrender Gebotsdialog vorhanden.

Zusätzlich wurden **beide** Gebote derselben Zwei-Verteidiger-Konstellation über die echten Bestätigungsknöpfe der Transferansicht bearbeitet:

- Erstes Gebot erfolgreich angenommen.
- Zweites Gebot nach Erreichen von vier Verteidigern abgelehnt.
- Fehlermeldung sichtbar.
- Nach Schließen der Meldung kein Gebotsdialog mehr.

Am unveränderten Head wurde außerdem der tatsächliche Meldungsinhalt über den Zustand der `MessageBox` geprüft: **„Der Transfer findet nicht statt.“**

Fundstellen: [Transfer.cs:308](https://github.com/kaaasi/eishockey-manager-redone/blob/9bd8fea5907f8719f92e2ff091f2c87b1a3f6258/src/EishockeyManager.Core/Transfer.cs#L308) und [TransferScreen.cs:169](https://github.com/kaaasi/eishockey-manager-redone/blob/9bd8fea5907f8719f92e2ff091f2c87b1a3f6258/src/EishockeyManager.UI/Screens/Team/TransferScreen.cs#L169).

### 1.4 Gültige Gebote und Wiederholungsabwehr

Eine zusätzliche Probe enthielt gleichzeitig zwei Verteidigergebote und ein weiterhin gültiges Stürmergebot:

1. Erstes Verteidigergebot annehmen; zweites dadurch nicht mehr ausführbar.
2. Wertgleiche Kopie des gültigen Stürmergebots an `Accept()` übergeben: `Impossible`, vollständiger Zustandsabdruck unverändert, echtes Gebot bleibt offen.
3. Ungültiges zweites Verteidigergebot über die UI bestätigen: nur dieses Gebot wird entfernt; Stürmergebot bleibt erhalten und wird als nächster Dialog vorbereitet.
4. Fehlermeldung schließen und Stürmergebot über die UI annehmen: genau dessen Betrag wird gutgeschrieben, Spieler wechselt korrekt.
5. Dasselbe akzeptierte Stürmergebot erneut annehmen: `Impossible`, vollständiger Zustandsabdruck unverändert.

Die neue Entfernung arbeitet mit **Referenzidentität** und trifft in dieser Probe keine gültigen anderen Gebote. R2-09 bleibt wirksam. Auch die bisherigen Regressionen für zurückgezogene, abgelehnte, gefälschte und eigentumsseitig veraltete Gebote bestehen im gesamten Testlauf.

### 1.5 Vier Saisonwechsel, passiver Manager

Die bisherige Mehrsaisonprobe wurde erneut ausgeführt: Originaldaten, Seed 3, Managerverein 42, einmalig 100 Mio. DM zur Vermeidung einer Entlassung, danach keine Vertragsverlängerung und kein Transfer.

| Wechsel | Neue Saison | Managerkader | Computer-Spieler | Größe der 58 Computerkader | Vertragslose | Alter der Kaderspieler | Computerverträge |
|---:|---:|---:|---:|---|---:|---|---|
| 1 | 1994 | 20 | 1.322 | 20–25 | 0 | 20–32 | 1 |
| 2 | 1995 | 0 | 1.322 | 20–25 | 20 | 21–33 | 2 |
| 3 | 1996 | 0 | 1.322 | 20–25 | 20 | 22–34 | 1 |
| 4 | 1997 | 0 | 1.322 | 20–25 | 20 | 23–35 | 2 |

Die Computerzahlen enthalten auch Reservevereine/Nationalteams mit Kader.

Nach jedem Wechsel bleiben **1.342 eindeutige Spieler** in Kadern plus Pool erhalten; Alter erhöht sich für jeden Spieler genau um die Anzahl der Wechsel, Kaderspieler haben positive Verträge und Vertragslose Vertrag 0. Der Pool altert nach dem zweiten Wechsel weiter von 21–33 auf 23–35 Jahre; er wächst im rein passiven Lauf nach dem einmaligen Abgang nicht weiter.

- **R2-11 bleibt offen, P1 für Runde 3:** vollständiger Managerkaderverlust zu Beginn der dritten Saison ohne Vorwarnung.
- **R2-12 bleibt offen, P2 für Runde 3:** keine Kader-KI für Neuzugänge, Talente und Ruhestand.

Diese bekannten Einschränkungen waren bereits abgegrenzt und sind keine neuen Rückschritte des zusätzlichen Commits.

### 1.6 Neuer Befund PR35-N01 · P3 · UI-Test prüft die behauptete Fehlermeldung nicht

**Fundstelle:** [WaveBReviewScreenTests.cs:43](https://github.com/kaaasi/eishockey-manager-redone/blob/9bd8fea5907f8719f92e2ff091f2c87b1a3f6258/tests/EishockeyManager.UI.Tests/WaveBReviewScreenTests.cs#L43).

Der Test `PR3502_SecondBidBecameInvalid_ShowsMessageAndDoesNotReopenSameBid` prüft Dialogende, Gebotsentfernung, Geld und Mindestbesetzung. Er prüft weder Sichtbarkeit noch Inhalt der Meldung.

**Reproduzierbares Gegenbeispiel:** In einer separaten temporären Quellkopie ausschließlich den folgenden Meldungszweig aus `TransferScreen` entfernen, sonst unverändert lassen:

```csharp
if (accepted != TransferResult.Accepted)
    _message.Show(TransferMarket.Message(accepted));
```

Dann mit Originaldaten ausführen:

```bash
EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.UI.Tests/EishockeyManager.UI.Tests.csproj \
  -c Release --filter "FullyQualifiedName~WaveBReviewScreenTests.PR3502_"
```

**Eigenes Mutationsergebnis:** **1 bestanden, 0 fehlgeschlagen, 0 übersprungen**, obwohl die Fehlermeldung nicht mehr angezeigt wird.

**Empfehlung:** Die Regression um eine Prüfung der sichtbaren Rückmeldung und möglichst ihres erwarteten Textes erweitern. Dieser P3-Befund betrifft die Absicherung, nicht das aktuelle Produktverhalten: Die richtige Meldung am unveränderten Head wurde in der eigenen Zusatzprobe ausdrücklich bestätigt. **Nicht mergeblockierend.**

## 2. Tatsächlich ausgeführte technische Prüfungen

### Quellstand und Lesearbeit

- PR-Metadaten abgeglichen; tatsächlicher Head ist `9bd8fea5907f8719f92e2ff091f2c87b1a3f6258`.
- Vorherigen Bericht direkt von `main` gelesen.
- Den vollständigen Diff `c4fca70..9bd8fea` gelesen: **ein Commit, sechs Dateien** (Dokumentation, drei Produktdateien, zwei neue Testdateien).
- Rückgabepfad der Leihen, Spielerfabrik/Nummernvergabe, Pool-/Listing-Neuaufbau, `Accept()`, Transfer-UI und beide neuen Regressionstestklassen geprüft.
- Vollständigen neuen Head als **182 Git-Blobs** bezogen, Hashes verifiziert und nach den Prüfungen erneut auf unveränderte Quellinhalte geprüft.
- Ursprünglichen Reviewer-Probecode unverändert kopiert; lediglich die Projektreferenzen auf den neuen Quellstand umgestellt. Zusätzliche Grenz- und UI-Proben separat ausgeführt.
- Keine ursprünglichen DOS-Dateien oder temporären Prüfprogramme ins Repository aufgenommen.

### Build und Tests

.NET SDK **10.0.402**, Release-Build der gesamten Solution: **0 Warnungen, 0 Fehler**.

| Eigener Lauf am unveränderten Head | Bestanden | Fehlgeschlagen | Übersprungen |
|---|---:|---:|---:|
| Originaldaten, `EM_REQUIRE_GAMEDATA=1` | 351 | 0 | 0 |
| Ohne Originaldaten | 297 | 0 | 47 |
| Pflichtlauf ohne Daten, `Category=Originaldaten` | 0 | 54 | 0 |

Diese Ergebnisse bestätigen die gemeldeten Zahlen. Unterschiedliche Gesamtfallzahlen bei fehlenden Daten hängen unter anderem mit der nicht expandierten Discovery übersprungener Theory-Datenzeilen zusammen.

Standardbefehle wurden mit lokalem SDK sowie `-m:1 -nr:false` ausgeführt:

```bash
dotnet restore EishockeyManager.slnx
dotnet build EishockeyManager.slnx -c Release --no-restore
dotnet test EishockeyManager.slnx -c Release --no-build
EM_REQUIRE_GAMEDATA=1 dotnet test EishockeyManager.slnx -c Release --no-build
EM_REQUIRE_GAMEDATA=1 dotnet test EishockeyManager.slnx -c Release --no-build --filter "Category=Originaldaten"
```

Läufe ohne bzw. mit Originaldaten waren getrennt. Die Mutation zu PR35-N01 erfolgte ausschließlich in einer separaten temporären Kopie. Sie verändert weder den geprüften Head noch den Integrationsbranch.

[CI-Lauf 40387368700](https://github.com/kaaasi/eishockey-manager-redone/actions/runs/40387368700): erfolgreich; Jobstatus, Schritte und Joblogs gelesen.

**Prüfgrenzen:** Kein neuer Desktop-Hörtest, Pixelvergleich oder vollständiger Screenshot-Szenariolauf. Die Prüfung konzentriert sich auf den zusätzlichen Commit, die beiden vorherigen Befunde und die explizit angefragten Invarianten. Aus einer erfolgreichen Grenzfallbehandlung wird keine allgemeine Garantie abgeleitet, dass beliebig beschädigte Spielzustände repariert werden.

## 3. Akzeptierte [APPROX]-Abweichungen

Für diesen Merge akzeptiert:

1. **Voller Stammverein → vertragslos.** Ein gültiger Stammvertrag kann bei Rückgabe wegen ausgeschöpfter Rückennummern beendet werden. Das ist eine explizite Produktregel ohne Originalbeleg. Sie ist als befristete Auflösung vertretbar, weil der Spieler in den geprüften Fällen erhalten, eindeutig zugeordnet und erneut verpflichtbar bleibt.
2. **Offenes, nicht mehr ausführbares Gebot → verwerfen.** Das Originalverhalten ist unbekannt. Die Regel vermeidet den wiederkehrenden Dialog, verändert weder Geld noch Kader und lässt andere gültige Gebote bestehen.
3. Die bereits akzeptierte **automatische Verlängerung auslaufender Computerverträge** bleibt eine Überbrückung bis zur Kader-KI. Ihre normale Mehrsaisonwirkung wurde erneut bestätigt.

Bei mehreren Rückkehrern und nur einem freien Platz wird in der bestehenden Verarbeitungsreihenfolge ein Spieler aufgenommen und der andere vertragslos. Eine sportlich oder historisch belegte Auswahlstrategie wird damit nicht behauptet.

R2-11/R2-12 bleiben offene Funktionslücken; die Freigabe ist keine vollständige fachliche Abnahme mehrjähriger Karrieren. Der neue P3-Testbefund kann unabhängig vom Merge nachgezogen werden.
