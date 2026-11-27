# Nachprüfung: Integration Runde 4 (#106)

## 1. Merge-Empfehlung

**Ja für `af252a23f6bd94472f99c5af73fd3ef20498fe92` gegen Base `cbc650c349e7f151abc5a75d8af705ef780cdf0d`, im ausdrücklich reduzierten Umfang.** Die vom Nutzer verlangte Rücknahme der DEB-Pokaltabelle zum Platzhalter ist umgesetzt; der falsche Originalbeleg ist korrigiert. Die Übersicht „DEB-Pokalspiele“ wird damit nicht abgenommen. R4-01 (#108) und R4-02 (#109) bleiben wie beauftragt offen.

ChatGPT, unabhängiger Reviewer ohne Beteiligung an der Implementierung. Ausschließlich Delta `c30b2c235e088fd8f2f805dfcdefedf4de6192ee..af252a23f6bd94472f99c5af73fd3ef20498fe92` (ein Commit, sechs Dateien) und die genannten Befunde nachgeprüft. Grundlage: [Erstreview](2026-10-29-integration-runde-4.md). Keine erneute Gesamtbewertung der unveränderten Spuren, keine Änderungen am geprüften PR.

## 2. Befunde

**Keine neuen Befunde im Delta.**

- **R4-01, P2 / #108:** inhaltlich weiterhin offen und vom Nutzer zurückgestellt. [CupTableMenuEntries.cs](https://github.com/kaaasi/eishockey-manager-redone/blob/af252a23f6bd94472f99c5af73fd3ef20498fe92/src/EishockeyManager.UI/Screens/Competitions/CupTableMenuEntries.cs#L3) registriert ausschließlich „Tabelle Europapokal“. „Tabelle DEB-Pokal“ fällt auf den bestehenden `PlaceholderScreen` zurück und zeigt keine Auslosung mehr; vor und nach dem ersten Pokaltermin ausgeführt. Die noch fehlende Ergebnisübersicht und die bewusst weiterhin approximative Paarungskachel „DEB-Pokal“ sind in [wettbewerbe.md](https://github.com/kaaasi/eishockey-manager-redone/blob/af252a23f6bd94472f99c5af73fd3ef20498fe92/docs/wettbewerbe.md#L250) korrekt unterschieden. [arbeitspakete.md](https://github.com/kaaasi/eishockey-manager-redone/blob/af252a23f6bd94472f99c5af73fd3ef20498fe92/docs/arbeitspakete.md#L96) hält Nutzerentscheidung und Folgearbeit in `tabellen-highscore` Teil 2 fest. Kein Anspruch, R4-01 sei fachlich behoben.
- **R4-02, P3 / #109:** unverändert offen; Hinweis „(Neuer)“ bei vier Managern bleibt Teil 2 zugeordnet. `TileLocks.cs`, `NewsBar.cs` und `InboxScreen.cs` sind im Delta unverändert. Die im Erstreview belegte Zugriffsmöglichkeit über die Nachrichtenleiste wird durch das Delta nicht berührt.
- **Highscore:** weiterhin offen wie im Erstreview, keine neue Umsetzung im Delta.

## 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

### Prüfstand und Begrenzung des Deltas

Aufbau unter `/tmp/em-review-followup` aus dem bereits nachgewiesenen alten Head und den sechs mit festem neuem Ref abgerufenen Dateien. Alle **298 Blobs und Dateimodi** gegen den vollständigen GitHub-Tree geprüft; resultierender Tree **`9cbfc3dd1e35982955645654ed12c1b03963f701`**, identisch mit dem Tree des neuen Head-Commits. Seine einzige Parent-SHA ist der bisher geprüfte Head. Für die Prüfung keine Quell- oder Standardtestdateien dieses Stands verändert; Zusatzprobe und Mutation ausschließlich unter `/tmp/em-follow-probes`.

Die sechs Änderungen betreffen zwei Dokumente, den Bildschirmfoto-Ablauf `TabellenHighscore.cs`, `CupTableMenuEntries.cs` und zwei Testdateien. **Einzige Änderung am ausführbaren UI-Produktcode:** Entfernung der DEB-Registrierung; die Europapokal-Registrierung ist identisch. Nationalteam, Taktik, Sperren, Speichern und Core sind bytegleich zum alten Head. Das Tools-Szenario entfernt nur die beiden DEB-Auslosungsaufnahmen; die Europapokal-Aufnahmen bleiben erhalten. Die beiden Tests mit der falschen DEB-Sollanzeige entfallen; der Registrierungstest verlangt nun die fehlende DEB-Registrierung und den Eintrag in der Platzhalterliste.

### Bedienprobe, Gegenprobe und Originalbeleg

`ReviewR4FollowupTests.DebTableIsPlaceholderBeforeAndAfterPlayedCupDate`, zusätzliche Originaldatenprobe in der separaten Kopie:

1. Neues Spiel; „Tabelle DEB-Pokal“ öffnen → exakt `PlaceholderScreen`, kein `DebCupDrawScreen`; rendern und schließen → `StateFingerprint` unverändert.
2. Paarungen → „DEB-Pokal“ öffnet weiterhin `DebCupDrawScreen`; „Tabelle Europapokal“ weiterhin `EuropeanCupScreen`.
3. Über `PlayMatchDay()` bis zu vorhandenen `DebCup.Matches` spielen, weiterhin Runde 0; dieselben Prüfungen wiederholen.

Ein grüner Fall. Befehl: `dotnet test tests/EishockeyManager.UI.Tests -c Release --filter FullyQualifiedName~ReviewR4FollowupTests`. Gegenprobe: in dieser Testkopie allein die alte `CupTableMenuEntries.cs` einsetzen; Filter `FullyQualifiedName~ReviewR4FollowupTests|FullyQualifiedName~Tiles_AreRegistered_WithoutPlaceholder` → beide Fälle rot (falscher Bildschirm bzw. unerwartete Registrierung). Anschließend Datei zurückgesetzt. Keine Mutation am nachgewiesenen Prüfstand.

`em.exe` aus dem prüfsummengültigen Originalbestand des Erstreviews erneut gelesen (SHA-256 `f41eb08aa3bc3119bd83c44856ccc550d7a450d6ee55e0447378700fefb2294b`): Segmenttabelle `0x163C0+0xE8` → Stubsegment `0x1490`, Datei `0x16700`; Stub-Dword `+4 = 0x9F70`, Eintrag `+0x75` enthält `CD 3F F3 38 00`. **`0x255C0 + 0x9F70 + 0x38F3 = 0x32E23`**. Titelzeiger DGROUP `0x0CD0` → `14FD:2AE5` → Datei `0x198B5`, Text **„DEB-Pokalspiele“**. Kommentar, korrigierter Testbeleg und Dokumentation stimmen damit überein; `0x33B83` wird zutreffend als andere Funktion unterschieden.

Bildschirmfoto-Abläufe am neuen Stand:

```sh
dotnet run --project src/EishockeyManager.Tools -c Release --no-build -- \
  screens gamedata /tmp/em-follow-screens tabellen-pokale nationalteam-2
```

Zwei Europapokalbilder und drei Nationalteambilder erfolgreich erzeugt; **alle fünf bytegleich** mit den jeweiligen Aufnahmen am alten Head aus dem Erstreview. Kein erneuter Pixelvergleich gegen die unveränderten Original-Videobilder erforderlich. Keine Originalmedien committed.

### Standardprüfung

SDK `10.0.402` gemäß `global.json`; vorhandene Originaldateien und Paketcache der ersten Prüfung wiederverwendet. Offline-Restore aus `/home/agent/.nuget/packages` mit `--disable-parallel -p:NuGetAudit=false -m:1`. Unverändertes Skript mit den für VSTest freigegebenen lokalen TCP-Sockets selbst ausgeführt:

```sh
export DOTNET_ROOT=/workspace/.dotnet
export PATH="$DOTNET_ROOT:$PATH"
NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=1 tools/pruefen.sh
```

Die Rekonstruktion besitzt kein ursprüngliches Head-Commitobjekt für die Abschlusszeile des Skripts; die Head-Bindung erfolgt über den vollständigen Tree-Nachweis oben. NuGet Audit nur für diesen Offline-Lauf deaktiviert, keine Projektänderung. Die folgende Tabelle stammt aus der eigenen Ausführung, nicht aus dem Nachweis der Leitstelle.

Release-Build mit Warnungen als Fehler bestanden; Skript-Exit **0**. Alle vier Testprojekte liefern Ergebnisse; die TRX-Auswertung bestätigt im Pflichtlauf ausschließlich Fehler wegen fehlender Originaldaten.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 693 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 585 | 0 | 93 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 585 | 108 | 0 |

Gegenüber dem Erstreview jeweils zwei Originaldatenfälle weniger: genau die entfernten Tests, die die falsche DEB-Auslosungsansicht verlangten. Abschließender Vergleich der geprüften Quelldateien gegen den nachgewiesenen Index ohne Abweichung.

### Akzeptierte [APPROX] und offene Arbeit

Keine neuen fachlichen Näherungen im Delta. Die im Erstreview akzeptierten `[APPROX]` der unveränderten Funktionen bleiben bestehen, insbesondere die Paarungskachel „DEB-Pokal“ als Auslosungsansicht und das Öffnen des Europapokals bei Gruppe 1. Der neue Platzhalter wird ausdrücklich als fehlende Funktion behandelt. R4-01/#108, R4-02/#109 und Highscore Teil 2 sind offene Folgearbeit gemäß Nutzerentscheidung; weder ihre Umsetzung noch eine vollständige Abnahme von `tabellen-highscore` wird behauptet.
