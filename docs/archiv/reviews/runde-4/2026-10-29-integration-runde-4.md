# Unabhängiges Review: Integration Runde 4 (#106)

## 1. Merge-Empfehlung

**Nein für `c30b2c235e088fd8f2f805dfcdefedf4de6192ee` gegen `cbc650c349e7f151abc5a75d8af705ef780cdf0d`.** R4-01 (P2): „Tabelle DEB-Pokal“ öffnet die Auslosung statt der aus em.exe belegten Spiel-/Ergebnisübersicht; die Abnahme von #104 Teil 1 ist damit nicht erfüllt. Alle drei Standardläufe und die echte Schema-3-Migration sind grün, ersetzen aber diesen fachlichen Nachweis nicht. R4-02 ist ein nicht blockierender Bedienungshinweis.

Reviewer: ChatGPT, neue Sitzung ohne Beteiligung an den Implementierungen. Erstprüfung der drei Spuren anhand ihrer Diffs und des integrierten Codes; keine Änderung am geprüften PR. Compare der jeweiligen Spur-Heads gegen den Integrations-Head bestätigt die Übernahme; `NationalMenu.cs` enthält beide Änderungen an `Open` bzw. `IsLocked`.

| Spur / geprüfte Umsetzung | Abnahme aus Runde 4 |
|---|---|
| #103 `38f0e70b5cf7193a5d627a197f0e23f4d95c95a3` | (1) Erreichbarkeit, zustandsreines Öffnen, Aufstellung/Taktik und Wirkung bis ins nächste WM-Spiel bestätigt; (2) Roundtrip und echte Migration bestätigt; (3) Pixelvergleich 00056/00058/00059 nachgemessen, Einschränkungen unten; (4) vier Saisonwechsel aktiv/passiv durch Standardlauf bestätigt. |
| #104 `65446d5f3c07c12a727accf7046afdb5956573d5` | **Teil 1:** (1) Europapokal und zustandsreines Öffnen bestätigt, DEB-Pokal fachlich nicht abgenommen (R4-01); (3) Sperren Werbung/Neuer gegen em.exe bestätigt, R4-02 beachten; (4) Szenario erfolgreich ausgeführt. Highscore einschließlich seiner Sperre ist ausdrücklich ausgenommen. |
| #105 `f4ef3abbe1064df9f49e27b6ae01df1a9186941b` | Gegenbeispiel erneut ausgeführt; stabile Verarbeitung statt Erhaltung der Sammlungshistorie ist die ausdrücklich erlaubte Lösung. Spielverlaufs- und Roundtrip-Proben grün; Rücknahme der Sortierung macht drei von vier neuen Fällen rot. Keine eigene Formatänderung dieser Spur. |

**Erwartet offen, bestätigt:** Highscore (`tabellen-highscore`, Teil 2: Bestenliste, Eintrag/Sortierung, dauerhafte Speicherung, Sperre und Pixelvergleich). Keine Abnahme dieses Teils durch dieses Review.

## 2. Befunde

### R4-01 · P2 · DEB-Pokaltabelle verweist auf Auslosung statt Spiel-/Ergebnisübersicht

[CupTableMenuEntries.cs:10](https://github.com/kaaasi/eishockey-manager-redone/blob/c30b2c235e088fd8f2f805dfcdefedf4de6192ee/src/EishockeyManager.UI/Screens/Competitions/CupTableMenuEntries.cs#L10), [Registrierung:23](https://github.com/kaaasi/eishockey-manager-redone/blob/c30b2c235e088fd8f2f805dfcdefedf4de6192ee/src/EishockeyManager.UI/Screens/Competitions/CupTableMenuEntries.cs#L23), [verengte Testabnahme](https://github.com/kaaasi/eishockey-manager-redone/blob/c30b2c235e088fd8f2f805dfcdefedf4de6192ee/tests/EishockeyManager.UI.Tests/CupTableTileTests.cs#L45).

Der neue `[ORIGINAL]`-Beleg setzt `call 0xE8:0x75 (0,0)` mit dem Bildschirm „DEB-Pokal Auslosung“ bei Datei `0x33B83` gleich. Die unabhängige Stub-Auflösung ergibt jedoch **`0x32E23`**:

```text
Segmenttabelleneintrag Datei 0x163C0 + 0xE8 → Stubsegment 0x1490
Stub Datei 0x16700, Dword bei +4 = 0x9F70
Stub-Eintrag +0x75 = CD 3F F3 38 00 → Funktionsoffset 0x38F3
0x255C0 + 0x9F70 + 0x38F3 = 0x32E23
```

Diese Funktion lädt für Argument 0 bei `0x32F06–0x32F13` den Titel über DGROUP `0x0CD0` → `14FD:2AE5` → **„DEB-Pokalspiele“** (Datei `0x198B5`). Sie zeichnet Gegner und Ergebnisse: DEB-Zweig `0x33819–0x33865` liest Ergebnispaare aus DGROUP `0xD012/0xD013` mit Spiel-/Paarungsindex; die folgende Schleife formatiert und zeichnet die Ergebnisse. `0x33B83` ist eine andere Funktion. Dass die Tabellen- und Paarungskachel denselben Originalaufruf benutzen, stimmt; die bisherige Paarungskachel des Remakes ist aber selbst nur eine als `[APPROX]` dokumentierte Auslosungsansicht. Ihre Wiederverwendung belegt keine originalgetreue Tabelle.

**Reproduktion am Head:** neues Spiel → „Tabelle DEB-Pokal“ vor dem ersten Termin rendern → zurück → über `PlayMatchDay()` bis zu ersten gespeicherten `DebCup.Matches` spielen (weiter `CurrentRound == 0`) → Kachel erneut öffnen, Maus identisch positionieren. Die beiden Oberflächen sind **pixelgleich**, obwohl Pokalergebnisse vorliegen: `DebCupDrawScreen` liest ausschließlich `DebCup.Draw`, keine Spiele, Tore oder Serienstände. Der rote Zusatztest `ReviewDebTableTests.PlayedDebCupResultsShouldAppearInTable` prüft genau diese fehlende Änderung. Der neue Standardtest prüft dagegen nur, ob nach der nächsten Auslosung die Zahl der Plätze sinkt, und bleibt grün.

Erforderlich: die aus dem aufgelösten Originalaufruf rekonstruierte Spiel-/Ergebnisübersicht für die Kachel bereitstellen, vorhandene `CupTie.Games`/Serienstände anzeigen und einen Test nach einem tatsächlich gespielten Pokaltermin ergänzen. Belegkommentar und `wettbewerbe.md` korrigieren. Das ist Teil der zugesagten Pokaltabellen-Abnahme, keine zusätzlich geforderte Highscore-Arbeit.

### R4-02 · P3 · Fragen verweisen bei vier Managern auf die gesperrte Kachel „Neuer“

[Neue Sperre, TileLocks.cs:25](https://github.com/kaaasi/eishockey-manager-redone/blob/c30b2c235e088fd8f2f805dfcdefedf4de6192ee/src/EishockeyManager.UI/Screens/TileLocks.cs#L25), [Hinweis, NewsBar.cs:81](https://github.com/kaaasi/eishockey-manager-redone/blob/c30b2c235e088fd8f2f805dfcdefedf4de6192ee/src/EishockeyManager.UI/Screens/Manager/NewsBar.cs#L81), [bisherige Zuordnung zum Posteingang](https://github.com/kaaasi/eishockey-manager-redone/blob/c30b2c235e088fd8f2f805dfcdefedf4de6192ee/src/EishockeyManager.UI/Screens/Manager/InboxScreen.cs#L98).

Reproduktion: Spiel mit vier Managern; `game.RequestLoan(1, 0, 100)` erzeugt eine Frage für Manager 0. Die Nachrichtenleiste zeigt „(Neuer)“, diese Kachel trägt nun aber das Schloss und nimmt keinen Klick an. Mit weniger Managern öffnet sie weiterhin den Posteingang. Die Sperrbedingung selbst ist aus em.exe korrekt übernommen; sie trifft auf die schon vorher als `[APPROX]` markierte Umdeutung der Kachel zum Posteingang. Der Konflikt ist bereits in #104 beschrieben, aber im Integrationsumfang noch nicht aufgelöst.

**Begrenzung nachgeprüft:** Ein Klick direkt auf die Nachricht (z. B. 100|215 bei nicht gewählter großer Kachel) öffnet den Posteingang auch mit vier Managern; die Kreditanfrage lässt sich dort beantworten. Deshalb keine behauptete Unerreichbarkeit oder verlorene Finanzfunktion, sondern P3. Den Hinweis auf den tatsächlich verfügbaren Nachrichtenzugang ändern; die spätere Originalfunktion „neuer Manager“ von der Posteingangs-Erweiterung trennen.

## 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

### Prüfstand, Umgebung und Standardläufe

Git-Fetch scheiterte sofort an `Failed to connect to proxy port 8080`; SDK und vollständiger Paketcache waren bereits vorhanden. Dateien über den GitHub-Connector mit festem Ref geladen. Rekonstruktion aus lokalem `b4db57e` plus vollständigem Compare: **alle 298 Blobs und Dateimodi geprüft**, resultierender Git-Tree **`98feb8269a38bcfec7b962002bd59b8989bde873`**, identisch mit dem Tree des Zielcommits. Auch die Base wurde vollständig rekonstruiert: 286 Blobs, Tree `47d086032393c92a483d9b8246442f16c1189d66`. Die sieben Änderungen zwischen lokalem Ausgangsstand und Base betreffen ausschließlich Doku. Keine bloße Prüfung eines ähnlich aussehenden Checkouts.

Unveränderter Produkt-/Testcode unter `/tmp/em-review-head`; zusätzliche Tests und Mutationen ausschließlich in getrennten Kopien. Die Abschlusszeile des Prüfskripts kann in der Rekonstruktion ohne ursprüngliches Commitobjekt keinen gültigen Head ausgeben; die Zuordnung erfolgt über den vollständigen Tree-Nachweis, nicht diese Zeile. Abschließender Dateivergleich gegen den nachgewiesenen Index ohne Abweichung.

```sh
export DOTNET_ROOT=/workspace/.dotnet
export PATH="$DOTNET_ROOT:$PATH"
dotnet --version # 10.0.402, entspricht global.json
dotnet restore EishockeyManager.slnx --source /home/agent/.nuget/packages \
  --disable-parallel -p:NuGetAudit=false -m:1
NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=1 tools/pruefen.sh
```

VSTest mit freigegebenen lokalen TCP-Sockets ausgeführt (im Sandbox-Vorcheck `Operation not permitted`). NuGet Audit nur für diese Offline-Ausführung deaktiviert; keine Projektänderung. Release-Build mit Warnungen als Fehler bestanden; Skript-Exit 0. TRX-Auswertung des Pflichtlaufs bestätigt ausschließlich Fehler wegen fehlender Originaldaten.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 695 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 585 | 0 | 95 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 585 | 110 | 0 |

Die Zahlen sind selbst ausgeführt, nicht aus #106 übernommen. Enthalten sind insbesondere `NationalLineupTests`, `NationalLineupScreenTests`, `CupTableTileTests`, `TileLockTests`, `SaveGameOrderTests`, `IntegrationRunde4Tests` sowie die bestehenden Zwillings-/Mehrsaisonproben.

### Gegenbeispiele und zusätzliche Ausführung

- **S71-02 unverändert nachgestellt:** `TestGames.Create()` → ersten `Schedules`-Eintrag entfernen → sichern/laden → denselben Eintrag auf beiden Seiten wieder einfügen → Schlüsselarrays vergleichen. Weiterhin rot: `[Bundesliga, ZweiteLiga, Oberliga]` gegenüber `[ZweiteLiga, Oberliga, Bundesliga]`. Das wird vom neuen Vertrag ausdrücklich nicht mehr versprochen. Anschließende Spielverarbeitung wird nun unabhängig davon sortiert; die vier `SaveGameOrderTests` sind grün. Ohne `Schedules.OrderBy(s => s.Key)` werden die Fälle für Entnahme 0/1 und der explizite Ligareihenfolgetest rot. Kein weiterhin bestehender Spielverlaufsfehler aus dem alten Schlüsselvergleich abgeleitet.
- **Echte Migration 3 → 4:** In `/tmp/em-review-base` mit dem unveränderten Schema-3-Writer von Base `cbc650c` vier Dateien erzeugt: synthetische/originale Datenbank × vor Ernennung/nach Ernennung und Auto-Nominierung; Modus 1, Seed 7. Kein nachträgliches Heruntersetzen einer Schema-4-Datei. Schema 3 und eigener Rückladeerfolg vor dem Transfer geprüft. Danach mit dem Head geladen: vier grüne Fälle, leere Formation, niemand eingesetzt, Auto-Taktik, identische Spielerreferenzen und wieder bytegleicher Schema-4-Roundtrip. Vollständigen alten JSON-Zustand nach Wegnahme allein der drei neuen Coach-Felder und kanonischer Objektnummerierung verglichen: in allen vier Fällen gleich, ebenso nach einem weiteren `Continue()` auf beiden Produktständen. SHA-256 der echten Originaldaten-Dateien: ohne Coach `1e6de2b79024e0168df84c5173293b531a0a28bb95458eb5c5bc1d9f70e37b79`, mit Coach `d6623f696f85705d64432a6091c806a8ccc9e32889be53320a70f348cc870325`.
- **Wirkung im echten nächsten WM-Spiel:** Seeds 7/19, zwei per Save/Load identische Spiele mit Auto-Aufstellung; im Zwilling alle Feldspielerplätze leeren, Torhüter belassen. Stärke sinkt; nach `Continue()` gleiche Gegner/Heimrechte und unterschiedliche tatsächlich gespeicherte WM-Ergebnisse. Zwei grüne Zusatzfälle. Aufruferkette `WorldCup.PlayDay` → `RatedNation` → `NationalMatchStrength` zusätzlich gelesen.
- **DEB-Ergebnisanzeige zu R4-01:** rote UI-Probe vor/nach dem ersten gespielten Pokaltermin; unveränderte Pixel trotz vorhandener Ergebnisse (Ablauf oben).
- **Vier-Manager-Gegenprobe zu R4-02:** eine grüne UI-Probe mit echter Kreditanfrage, gesperrtem Kachelzugang und erfolgreicher Antwort über die Nachrichtenleiste.

Zusatzbefehle: `dotnet test tests/EishockeyManager.Core.Tests -c Release --filter 'FullyQualifiedName~ReviewProbeTests|FullyQualifiedName~SaveGameOrderTests'` (1 rot wie oben, 4 grün); Filter `ReviewSchema3WriterTests` und `ReviewMigrationTests` je 4 grün, `ReviewWorldCupTests` 2 grün; UI-Filter `ReviewFourManagerTests` 1 grün. Diese Zusatztests gehören nicht zum unveränderten Standardlauf.

### Mutationsgegenproben

15 kompilierende Einzelmutationen in `/tmp/em-review-mutations`, jeweils eigener Testprozess mit lokalen Projektverweisen; anschließend Quelldateien zurückgesetzt. `dotnet test tests/EishockeyManager.{Core,UI}.Tests -c Release --filter <betroffene Klassen>`; keine Mutation im unveränderten Standardlauf. Auswahl der nachgewiesenen Reaktionen:

| Mutation | Rote Fälle |
|---|---|
| Ligastabilisierung entfernen | 3/4 `SaveGameOrderTests` |
| Formation ignorieren / Taktikbonus entfernen | Aufstellungs-Stärkeprobe / Taktikprobe und beide aktiven Mehrsaisonfälle |
| Entlassenen nicht bereinigen / Grenze auf 29 erhöhen / Verletzte spielen lassen | Kaderänderungsprobe / Eingesetzt-Regeln / Verletztenprobe |
| Migration 3 überspringen / sämtliche Schema-3-Dateien ablehnen | Coach-Migration / beide Migrationsproben einschließlich ohne Coach |
| `Formation` beim Schreiben auslassen | Roundtrip und Coach-Migration |
| Farbe 17 statt 16 / Aufstellung auf Platzhalter routen | Bildmerkmale / beide betroffenen UI-Bedienproben |
| Werbungssperre / Neuer-Sperre entfernen | jeweils passende Bedingungstests, zusätzlich übrige Kacheln bzw. Klickprobe |
| „Sichern“ im Coach-Menü sperren | beide `IntegrationRunde4Tests` |
| Beide Pokaltabellen-Registrierungen entfernen | alle fünf `CupTableTileTests` |

Diese Gegenproben zeigen die Empfindlichkeit der vorhandenen Tests für die jeweiligen Mutationen. Sie belegen nicht die fachliche Richtigkeit ihrer Sollwerte: gerade die DEB-Tests erkennen eine fehlende Registrierung, verlangen aber den falschen Bildschirm (R4-01). Auch der Integrationstest akzeptiert beim bloßen erneuten Öffnen jeden Typ außer `MainMenuScreen`; die separaten Nationalteam-UI-Tests erkennen den Platzhalter.

### Originalbelege und Pixelvergleich

DOS-ZIP aus `kaaasi/eishockey-manager-referenz`, Stand `fa6dc57481702694f7157fad2cc90fc3ee04444a`, außerhalb des Repos entpackt. ZIP-SHA-256 `142fc78e9b286cb47f36baa450f5af311ac0902525564d0c30ab63697ca321f4`; sämtliche entpackten Dateien gegen `original/SHA256SUMS` ohne Abweichung geprüft, insbesondere em.exe `f41eb08aa3bc3119bd83c44856ccc550d7a450d6ee55e0447378700fefb2294b`.

Unabhängige Disassembly mit `objdump -D -b binary -m i8086 -M intel --adjust-vma=<Dateioffset>` auf passenden lokalen Bytebereichen; DGROUP-Daten bei Datei `0x16DD0 + Adresse` und Overlay-Stubs/Verteilertabellen zusätzlich ausgewertet:

| Aussage | Nachgeprüfter Beleg in em.exe |
|---|---|
| Nationalteam nutzt gemeinsame Aufstellungs-/Taktikweiterleitung | Datei `0x48E28`, Fernaufruf `0x108:0x2A`, Taktikzweig `0x47F2B`; Aufstellungsziel `0x40364` über Overlay-Stubs aufgelöst. |
| Eingesetzt, Grenze und Bedienung | `0x40E4D`: Vergleich `0x16`; `0x415B2`: Kaderfeld `+0x0A = 1`; `0x40CF8/0x40D0D`: Platz und Einsatz löschen; `0x40F45–0x40F59`: Reservist herausnehmen; `0x411FE–0x41208`: Rechtsklick setzt ein. `0x41D84`: Textfarbe `0x10`. |
| Originalaufrufe der Pokaltabellen | Sprungtabellen `0x45A5F/0x45A6B/0x45A99`; `0x4584A → 0x45897`: `push 0; push 0; call 0xE8:0x75` → **`0x32E23`, DEB-Pokalspiele, nicht Auslosung** (R4-01); `0x4584C–0x45862`: `call 0xF8:0x3E` → `0x3A88A` mit Seitenspeicher. DGROUP `0x5123` und `0x5126` jeweils 3. |
| Kachelsperren | `0x45057`: Managerzahl `[0x386] == 4`, schreibt `0x515D`; `0x45079`: Endjahr `[0x5A14] == 1994`, schreibt `0x515A`; Initialisierung um `0x2987C`. |
| Abweichende Taktik-Vorgaben tatsächlich vorhanden | DGROUP `0x4911` enthält `0,0,25,20,30,0,…`; `0x4920` enthält `0,4,5,6,7,4`. Die sichtbaren Videozahlen sind damit keine frei erfundene Sollvorgabe; ihre vollständige Bedeutung ist weiter offen. |

`dotnet run --project src/EishockeyManager.Tools -c Release --no-build -- screens gamedata /tmp/em-screens nationalteam-2` und gleicher Aufruf mit `tabellen-pokale`: drei bzw. vier Bilder erfolgreich. Keine Originalmedien im Review-Commit.

Pixelvergleich gegen die drei separat geladenen Referenzbilder dieses Referenzstands: 320×240, PIL-Grauwerte, mittlere absolute Differenz / Anteil mit Differenz >40. Maske unten links `x<48,y≥170`, zusätzlich Mausbereich bei 00056 `x=156…181,y=216…239`, bei 00058/59 `x=207…229,y=64…88`. Pegelanpassung separat als lineare Kleinste-Quadrate-Anpassung `Video ≈ a·Render+b`; keine Behauptung bytegleicher Pixel. 00059 wird gegen denselben gerenderten Grundtaktik-Bildschirm verglichen.

| Referenz / Bereich | roh | pegelbereinigt |
|---|---|---|
| 00056 ganz | 18,93 / 11,18 % | 15,41 / 7,72 % |
| 00056 ohne Listeninhalt (`x=5…234,y=27…206` zusätzlich maskiert) | 15,39 / 5,27 % | 11,35 / 4,23 % |
| 00056 rechts (`x≥241`) | 13,35 / 3,06 % | 8,23 / 2,16 % |
| 00058 ganz | 14,20 / 4,10 % | 9,53 / 4,50 % |
| 00059 ganz | 14,21 / 4,06 % | 9,51 / 4,64 % |

Bester ganzzahliger Versatz jeweils **0|0**, Suche ±2 Pixel. Pegelfaktoren für ganze Bilder 0,661/0,842/0,842, Offsets 52,71/28,67/28,58. Die wesentlichen Kennzahlen aus `wettbewerbe.md` sind nachvollziehbar; kleinere Prozentabweichungen bei Taktik hängen an der expliziten Maske. Andere Spielerdaten, Schrift-/Videopegel und die bekannten Taktikfelder bleiben sichtbar verschieden. Für Pokaltabellen fehlt ein Videobild; dort wurde der Originalaufruf statt eines Pixelnachweises geprüft – dabei entstand R4-01.

### Akzeptierte [APPROX] und Grenzen

Für diesen Integrationsumfang akzeptiert, **keine Originaltreue zugesagt**: Auto-Aufstellung samt Auffüllen bis 22, Verdrängter bleibt Reserve, vereinfachte Auswahl statt Spielerinfo, Stärke aus Torhüter/Reihen 1–3 und vereinsgleicher Taktikzuschlag, Automatik bei ganz leerer Formation; Grundtaktik statt der sichtbar abweichenden Originalvorgaben; Europapokal öffnet Gruppe 1 statt die zuletzt gemerkte Seite. Die in `wettbewerbe.md` benannten Abweichungen der Vereinsaufstellung (Farbe/22er-Grenze) sind nicht durch diese Integration behoben. Die bestehenden Näherungen des WM-/Spielmodells bleiben Näherungen.

Davon getrennt: Die Bedeutung sämtlicher Original-Taktikwerte und die exakte Wirkung aller Taktikmodi auf die Originalsimulation wurden nicht rekonstruiert. Der Nachweis unterschiedlicher Remake-WM-Ergebnisse belegt die Verdrahtung, keine Originalformel. Highscore bleibt wie beauftragt offen. Keine Annahme über eine fehlende GitHub-Schreibberechtigung aus dem lokalen Proxyfehler abgeleitet.
