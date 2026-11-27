# Nachprüfung: Integration Runde 7

## 1. Merge-Empfehlung

**Ja für Head `6811477acae21e8f0e0e430de3ccac825feb63a2` von PR #198 im vereinbarten Umfang.**
R7-02, R7-03 und R7-04 sind behoben und durch erneut ausgeführte Gegenbeispiele sowie gezielte
Mutationen abgesichert. Im geprüften Delta kein neuer blockierender Befund. Die Meisterhäufung
ist **nicht beseitigt**; die akzeptierte Annäherung der Managervereine bleibt eine Grenze der Freigabe,
kein Nachweis originalgetreuer Balance.

- Integrations-Base: `15d07333f1bfd2bf2fe39a400c26a834b31d7183`.
- Nachprüfung ausschließlich des Deltas `eafbbae1b8e35c4a164ab17c773d53be6d86c107..6811477acae21e8f0e0e430de3ccac825feb63a2`
  und der ausdrücklich genannten Befunde; Ausgangsbericht: [Erstreview](2026-10-30-integration-runde-7.md).
- Reviewer: ChatGPT/Codex; keine Mitarbeit an der Implementierung der Korrekturen.
- **Ausführung am vollständig nachgewiesenen Quellbaum, nicht nur Codelesen.**
  Den verifizierten alten Tree um die 15 geänderten Dateien mit festem neuem Head-Ref aus dem
  GitHub-Connector ergänzt. Vollständiger Git-Tree einschließlich Pfadbestand und Modi:
  `77aae847d2b80f27154e541761afcb6013d227ae`, identisch mit dem Tree des Zielcommits.
  Vor und nach der Prüfung verifiziert. Die lokale HEAD-Referenz bleibt technisch auf dem Base;
  die Ausgabe „mit nicht eingecheckten Änderungen“ des Skripts beschreibt diese Rekonstruktion.
- Review-Branch von aktuellem `main` (`521e4d9e1c29f9cc7da0f337150bfb8f6e3a396a`, enthält
  inzwischen den Erstbericht). Veröffentlicht wird ausschließlich diese Datei.

## 2. Befunde

### R7-02 / #200 · bisher P2 · behoben

[MatchEngine.Original.cs:133](https://github.com/kaaasi/eishockey-manager-redone/blob/6811477acae21e8f0e0e430de3ccac825feb63a2/src/EishockeyManager.Core/MatchEngine.Original.cs#L133):
Beide Seiten ziehen jetzt inklusive `lrandom(Angriff, 7)`. Die Originalstellen
0x9F92–0xA019 erneut disassembliert: Obergrenze 7, untere Grenze Angriffsbitfeld,
danach Ergebnis minus 5 und Begrenzung auf mindestens 0.

Unabhängige Wiederholung des alten Gegenbeispiels mit einem protokollierenden `Random`:
`FullResult(new MatchSide(150, AttackLevel:7), new MatchSide(150, AttackLevel:3), false, false, rng)`
liefert als erste inklusive Grenzen **(7,7), (3,7)**. Rückmutation beider Aufrufe auf
`Rnd(0, Angriff)`: alle **vier** Fälle der neuen `IntegrationRunde7ReviewTests` rot.
Die Zusatztheorie rechnet die Bonusverteilung teilweise selbst aus; ihr produktiver Schutz
besteht vor allem in der Prüfung der tatsächlich angefragten Grenzen.

### R7-03 / #201 · bisher P2 · behoben

[MatchEngine.Live.cs:178](https://github.com/kaaasi/eishockey-manager-redone/blob/6811477acae21e8f0e0e430de3ccac825feb63a2/src/EishockeyManager.Core/MatchEngine.Live.cs#L178):
Die eigene Nationalmannschaft wird vor dem Abbruch für leere `RatedNation.Players`
an `coach.Lineup()` angeschlossen. Die gemeinsame Verbuchung erhält die Spielerobjekte
des Nationalkaders; Spielwertungen zählen weiterhin nicht.

Altes Gegenbeispiel erneut: `GameModeTests.Create(7,false,true,2)`, `Continue()`,
`AutoNominate()`, `PlayFriendly(52,true)`. Ergebnis 2:5; Kadersummen jetzt
**2 Tore, 2 Vorlagen, 2 Zweiminutenstrafen, 60 Torhüterminuten, 16 Länderspiele**.
Zusätzlich besteht `WorldCup_CreditsTheNationalLineup` nach automatischer Nominierung
ohne vorheriges Freundschaftsspiel mit mindestens 60 Torhüterminuten.
Die beiden neuen Nationalzähler-Tests sind grün; Deaktivieren allein des neuen
Nationalmannschaftszweigs macht **beide rot**.

### R7-04 / #202 · bisher P3 · behoben

[IntegrationRunde7EnergyTests.cs:13](https://github.com/kaaasi/eishockey-manager-redone/blob/6811477acae21e8f0e0e430de3ccac825feb63a2/tests/EishockeyManager.UI.Tests/IntegrationRunde7EnergyTests.cs#L13):
Unabhängiges Literal 70 und Vergleich mit der Schreibinstruktion in `em.exe`.
Originalstelle 0x3BB51 erneut geprüft: schreibt 0x46.
Ausschließlich `NationalCoach.FriendlyEnergy = 70` auf `71` mutiert:
**beide neuen Tests rot**. Damit besteht die frühere gemeinsame Konstantenabhängigkeit
nicht mehr. #200–#202 stehen auf GitHub noch offen; diese Nachprüfung bewertet die Korrekturen
am genannten Head und schließt keine Issues.

### Präzisierte Tests und neue Sollwerte

- **FinanceCalibrationTests – Prüfziel erhalten.**
  [Zeile 61](https://github.com/kaaasi/eishockey-manager-redone/blob/6811477acae21e8f0e0e430de3ccac825feb63a2/tests/EishockeyManager.Core.Tests/FinanceCalibrationTests.cs#L61):
  Das Pokalfinale kann nach den manipulierten Ligatiteln noch einen Pokalsieger setzen.
  Dessen Prämie wird aus Saisonprotokoll und Buchhaltung abgezogen; beide Werte werden weiterhin
  gegen die unabhängigen Aufstiegs-/Tabellenerwartungen geprüft. Mutation Oberliga-Aufstiegsprämie
  300000 → 300001: **2 rot, 5 grün**. Die Pokalprämie selbst ist zusätzlich durch
  `OriginalPremiumAmounts_MatchExecutableInstructions` und
  `FinanceNachpruefungTests.SeasonPremium_CupWinnerAndPromotionOnly` abgesichert.
- **WeltkugelDosboxTests – Prüfziel erhalten, enger begrenzt.**
  [Zeile 342](https://github.com/kaaasi/eishockey-manager-redone/blob/6811477acae21e8f0e0e430de3ccac825feb63a2/tests/EishockeyManager.UI.Tests/WeltkugelDosboxTests.cs#L342):
  Vier Saisonwechsel, mindestens eine Ernennung und mindestens eine Saison ohne Ernennung
  im Vollmodus; jede tatsächlich gespielte erste Begegnung setzt alle Kaderenergien von 55 auf 70.
  Weltmodus bleibt exakt vier, Vereinsmodus exakt null. 1–3 ist kein Nachweis einer bestimmten
  Ernennungszahl oder der Auswahlregel; diese wird separat in `NationalCoachMode4Tests` geprüft.
  Gegenprobe: Zuweisung der Energie auf 55 festgesetzt → **Voll- und Weltmodus rot, Vereinsmodus grün**.
- **IntegrationRunde7Tests – Konsistenzziel erhalten.**
  [Zeile 77](https://github.com/kaaasi/eishockey-manager-redone/blob/6811477acae21e8f0e0e430de3ccac825feb63a2/tests/EishockeyManager.UI.Tests/IntegrationRunde7Tests.cs#L77):
  Spielertore ≤ Mannschaftstore entspricht der vorhandenen Zuordnung nach 0x5028;
  Gleichheit war wegen nicht zugeordneter Treffer kein zulässiges allgemeines Soll.
  Dieser Einzeltest allein akzeptiert auch null Spielertore. Positive Zuordnung schützen
  zusätzlich die geskriptete Torschützenprobe, die 4000-Spiele-Verteilungsprobe und
  `RegularSeason_PlayerCountersConsistentWithResults` mit Untergrenze 60 % je Verein.
  Gegenprobe: Torschützenzuwachs von 1 auf 100 erhöht → **der geänderte UI-Test rot**.

Die geänderten Referenzabdrücke wurden ausgeführt und die Änderungen den beiden Produktionskorrekturen
zugeordnet: andere Angriffsziehungen verändern Ergebnisse und Folgeereignisse; die neue
Nationalstatistik verbraucht zusätzliche Zufallszahlen. **Kein schrittweiser Zwillingsvergleich
und keine unabhängige Herleitung jedes neuen Abdrucks** erfolgt. Die Abdrücke sichern
Wiederholbarkeit des Remakes, nicht Gleichlauf mit DOS. Die Empfehlung stützt sich deshalb auf
die konkreten Originalstellen, Gegenbeispiele und Invarianten.

### Meisterverteilung nach R7-02

Identische Zusatzprobe wie im Erstbericht: Originaldaten, aktiver Managerverein 42,
Seeds 1–12, je vier Saisonwechsel über `RngTwinProbeTests.Run(game, true, changes:4)`,
Titel vor jedem Wechsel aus `History[^1]`.

| Meister | Vorher (48 Saisons) | Jetzt (48 Saisons) |
|---|---:|---:|
| Düsseldorf (2) | 17 | 21 |
| Köln (5) | 12 | 11 |
| Krefeld (6) | 12 | 8 |
| Hedos München (9) | 4 | 4 |
| Mannheim (8) | 2 | 2 |
| Preussen Berlin (1) | 0 | 1 |
| Kaufbeuren (4) | 1 | 1 |

Seed 1 ergibt jetzt **2,2,2,2**, Seed 2 **2,5,5,5**, Seed 7 **5,5,5,6**.
Es gibt weiterhin mehrjährige Dominanz. Düsseldorf steigt von 35,4 % auf 43,8 % der Titel;
das ist eine kleine deterministische Stichprobe, kein signifikanter Nachweis einer
Verschlechterung. Eine allgemeine Entwarnung zur Balance lässt sich aus dem geänderten
synthetischen Modus-4-Abdruck nicht ableiten. Dessen Indexfolge ist 1,13,0,24,0;
Index 0 ist ein Verein, kein fehlender Meister (die Striche in `docs/arbeitspakete.md`
sind dafür missverständlich). Kein zusätzlicher funktionaler P2-Befund:
Die korrigierte Originalformel ist belegt; die unveränderte Teamwertabbildung bleibt
der bereits akzeptierte offene Teil.

## 3. Ausgeführte Prüfungen und akzeptierte Grenzen

### Ausführung

.NET SDK **10.0.402**, Release, Build mit Warnungen als Fehler.
Originaldaten aus der unveränderten privaten Referenz; SHA-256 von `em.exe`:
`f41eb08aa3bc3119bd83c44856ccc550d7a450d6ee55e0447378700fefb2294b`.
Offline-Restore aus vorhandenem Paketcache; `NuGetAudit=false` nur für die Umgebung.
Testhost-Sockets mit genehmigter Ausführungsfreigabe.

```sh
export PATH=/workspace/.dotnet:$PATH
export NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=1
dotnet restore EishockeyManager.slnx --source /home/agent/.nuget/packages \
  --disable-parallel -p:NuGetAudit=false -m:1
bash tools/pruefen.sh
```

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1050 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 873 | 0 | 161 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 873 | 177 | 0 |

Ergebnis des Skripts: **bestanden (Exit 0)**. Alle 177 Fehler des Pflichtlaufs wurden vom Skript als fehlende Originaldaten bestätigt.

Zusatzprobe in separater Kopie des verifizierten Trees, mit temporärer Testklasse
`ReviewR7Tests` (nicht veröffentlicht): alte Grenzzug- und Freundschaftsspiel-Gegenbeispiele,
zwölf Seeds der Meisterprobe und diagnostisch die Startstärkeprobe; zusammen mit den neuen
Grenz- und Nationalzähler-Tests **10 bestanden**.

```sh
dotnet test tests/EishockeyManager.Core.Tests -c Release --no-restore \
  --filter 'FullyQualifiedName~ReviewR7Tests|FullyQualifiedName~IntegrationRunde7NationalCountersTests|FullyQualifiedName~IntegrationRunde7ReviewTests' \
  --logger 'console;verbosity=detailed'
```

Mutationen ausschließlich in dieser separaten Kopie, jeweils anschließend zurückgesetzt.
Jeweils `dotnet test tests/EishockeyManager.<Core|UI>.Tests -c Release --no-restore
--filter FullyQualifiedName~<oben genannter Test>`. Keine Produktionsänderung am geprüften
Tree oder im Review-PR. Die Vollprüfungen enthalten diese Zusatztests und Mutationen nicht.

### Erwartet offen / akzeptierte `[APPROX]`

- #192–#196 am 31.10.2026 per GitHub gelesen: weiterhin offen; im Delta keine Umsetzung.
  Nationaltaktik-Startzustand, Länderwerte nach Freundschaftsspielen, WM-Gruppen-Paarungsansicht,
  erster Halt im Weltmodus und Kursverlaufsregler bleiben zurückgestellt.
- Managervereine: Auswahl 0x386D1 / 0x378C5, Reihenwerte 0x98F8–0x9C63,
  Energie 0x87F7 und Reihenwechsel 0x8EF2 bleiben Annäherungen; ebenso die zugehörige
  Taktik-/Einsatzzuschlags- und Teamwertabbildung. Der neue Nationalzähleranschluss verwendet
  diese bestehende approximative Ereigniszuordnung.
- Freundschaftsspiele wie WM-Gruppenspiele; Energie 70 erst beim ersten Freundschaftsspiel.
  Die übrigen im Erstbericht akzeptierten Grenzen sind durch dieses Delta nicht aufgehoben.

### Nicht nachgewiesen / nicht erneut geprüft

Keine DOS-Zwillingsausführung, keine originalgetreue Meisterwahrscheinlichkeit, keine
vollständige Erklärung jedes geänderten Referenzwerts. R7-01, Schema-Migrationen und FILE01-Patch
wurden in dieser gezielten Nachprüfung nicht noch einmal als eigene Gegenbeispiele ausgeführt;
die vorhandenen Tests liefen in der Vollprüfung mit. Die Nachprüfung ersetzt nicht die
unveränderten Teile des Erstberichts.
