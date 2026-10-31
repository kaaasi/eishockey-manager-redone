# Unabhängiges Review: Integration Runde 7

## 1. Merge-Empfehlung

**Nein für Head `eafbbae1b8e35c4a164ab17c773d53be6d86c107` von PR #198.**
R7-02 und R7-03 sind offene P2-Befunde: Die Angriffstaktik verwendet andere Zufallsgrenzen als
`em.exe`; Nationalmannschaftsspiele erreichen die neuen Spielerzähler nicht. R7-04 ist eine
P3-Testlücke. R7-01 ist behoben und durch Gegenproben abgesichert. Die ausdrücklich
zurückgestellten Punkte werden hier nicht erneut zur Merge-Bedingung gemacht.

- Base: `15d07333f1bfd2bf2fe39a400c26a834b31d7183`.
- Reviewer: ChatGPT/Codex, frische Sitzung ohne Mitarbeit an der Implementierung.
- Prüfung begonnen am 30.10.2026, abgeschlossen am 31.10.2026; Dateiname gemäß Review-Auftrag.
- **Ausgeführt am vollständig verifizierten Inhalt des Heads, nicht nur Codelesen.**
  Git-Fetch scheiterte am nicht erreichbaren Umgebungsproxy. Rekonstruktion aus lokalem Base-Checkout
  und 70 Dateien mit festem Head-Ref über den GitHub-Connector; vollständiger rekursiver Tree,
  Dateimodi und Pfadbestand berücksichtigt. `git add -A && git write-tree` ergab vor und nach
  den Prüfungen **`7d100c8042bcc09762192c2b7296a31f806f5a4c`**, identisch mit dem Tree des
  Zielcommits aus der Git-Daten-API. Kein ungestagtes Quelldelta. Die lokale HEAD-Referenz blieb
  beim Base-Commit; die entsprechende Commit-Zeile des Prüfskripts ist deshalb kein Nachweis
  des Zielcommits. Diesen liefert der vollständige Tree-Vergleich.
- `main` und PR-Head bei Abschluss unverändert. Der Review-Branch basiert auf `main`;
  nur diese Datei wird veröffentlicht.

## 2. Befunde

### Spur `kader-start` (#187)

Keine neue funktionale Abweichung im geprüften Delta. Die neun Tabellenwerte
**87/81/70, 68/55/42, 39/31/21** stimmen mit DGROUP 0x4B42 in der Originaldatei überein.
Die Liga-/Startstärke-Auswahl und die Würfelbereiche der Initialisierung wurden gelesen,
die zugehörigen Tests ausgeführt. Mutation 87 → 86 lässt zwei der 19 Kaderstart-Fälle scheitern.

Die Wirkung auf die Spielstärke bleibt begrenzt durch die akzeptierte Abbildung der Managervereine:
Bei fest gewähltem Verein 2 und Seed 7 ergeben alle drei Startstärken `TeamValue = 92`,
obwohl die Aufstellungsbewertungen 68,07 / 64,44 / 57,81 betragen. Jede Startaufstellung wird
als eigener `ReferenceRating` abgezogen. Das ist eine konkrete Folge des bestehenden
`[APPROX]`-Modells, kein hier neu erhobener Fehler der Startwerttabelle.

### Spur `boerse-start` (#188)

Keine neue funktionale Abweichung im geprüften Delta. Tagesrechnung mit `em.exe`
0x5784A–0x57C63 abgeglichen: Basiswachstum, Tendenzwechsel, große/kleine Ereignisse,
Rauschen, Rückzug zum vorher ermittelten Zielkurs, Aktienteilung und Wortablage.
Die 90 vorbereitenden Tage, die Migration und die Kauf-/Verkauf-Aufrufer sind getestet;
90 → 89 lässt zwei der 14 Börsenfälle scheitern. Der einmalige Börsenabschluss nach allen
Managern entspricht der dokumentierten Nutzerentscheidung. Der fehlende Chartregler #196 bleibt offen.

### Spur `weltkugel-dosbox` (#189)

Kachelsperren/Highscore, Vor-Endrundenfenster, Kosten, fehlender Zeitfortschritt und der
Zeitpunkt des Energiesetzens sind durch die vorhandenen Tests abgedeckt. Die noch fehlende
Erreichbarkeit des Vor-Endrundenfensters ist #194, kein neuer Befund.

**R7-04 · P3 – Energietests sichern den belegten Zahlenwert nicht unabhängig ab.**
[WeltkugelDosboxTests.cs:210](https://github.com/kaaasi/eishockey-manager-redone/blob/eafbbae1b8e35c4a164ab17c773d53be6d86c107/tests/EishockeyManager.UI.Tests/WeltkugelDosboxTests.cs#L210),
ebenso Zeilen 266 und 329: Erwartungswert ist dieselbe Konstante wie im Produktcode.
Gegenprobe in separater Kopie: ausschließlich `NationalCoach.FriendlyEnergy = 70` auf
`71` ändern, dann die Klasse `WeltkugelDosboxTests` ausführen: **14 bestanden, 0 fehlgeschlagen**.
Die Originalstelle 0x3BB51 setzt dagegen 0x46 = 70. Einen unabhängigen Literal-/Originaldatenbeleg
für 70 ergänzen; der akzeptierte Zeitpunkt „erst beim ersten Freundschaftsspiel“ bleibt davon unberührt.

### Spur `menue-befunde` (#190)

Keine neue funktionale Abweichung im geprüften Delta. Nachrichtenleiste ohne/mit ungelesener
Nachricht und nach Rückkehr aus dem Posteingang geprüft; Mutation „ohne Sichtbarkeitsbedingung
zeichnen“ lässt zwei der fünf Fälle scheitern. Das Weglassen der Handbuchfrage entspricht
der dokumentierten Entscheidung. Das Nachrichtenkennzeichen bleibt die gekennzeichnete
Annäherung über ungelesene Nachrichten.

### Spur `hok-patch` (#191)

Kein neuer Befund. Den angeforderten **CLI-Aufruf mit FILE01** erneut ausgeführt:

```sh
dotnet run --project src/EishockeyManager.Tools -c Release --no-restore -- \
  hok patch /workspace/work/spielstaende/FILE01.HOK /workspace/work/FILE04.HOK \
  konto=2000000 eintritt=20 datum=20.09.1993 zaehler=11 spieltag=4
```

Exit 0, 47.949 Byte; beide Prüfsummen gültig. Rücklesen: 20.09.1993, Spieltag 4,
Konto 6.000.000 Drittel-DM = 2.000.000 DM, Eintritt 20 DM, weiterhin 3.000 Sitz-/4.500 Stehplätze,
neue ID −14839. FILE01–03 bestehen den byteidentischen Encode/Decode-Rundlauf; der Eintritt-17-Patch
entspricht dem Klartext von FILE02. Die Tests prüfen auch die begrenzten geänderten Blöcke.
Prüfsummenmutation lässt sechs der 25 Patcher-Fälle scheitern.

**Grenze:** Kein neuer DOSBox-Ladevorgang und kein Weiterlaufen eines Spieltags in DOSBox.
Die dokumentierte Nutzerabnahme des Ladens wird nicht als eigene Ausführung ausgegeben.

### Spur `hok-format`, Nachtrag (#197)

Kein neuer Befund im geprüften Delta. Decoder und Feldzuordnung mit FILE05–08 erneut gelesen:
MENDE 80/82/60, zwei Vertragsjahre und 12.100 DM Gehalt; Wechsel BRANDL → DOBRYZINSKI in Reihe 3;
DAFFNER nach Kauf mit einem Vertragsjahr und 400.000 DM gespeichertem Gebot.
Kontostände FILE05/07/08: 737.354 / 557.354 / 1.438.354 DM.
Der neue synthetische Feldtest besteht; Änderung Vertragsfeld +0x0B → +0x0C macht ihn rot.
Die Referenzbilder wurden nicht erneut pixelweise verglichen.

### Spur `spielberechnung` (#186)

**R7-02 · P2 – Falsche Grenzen des Angriffszufalls verändern Torbudget und Taktikwirkung.**
[MatchEngine.Original.cs:132](https://github.com/kaaasi/eishockey-manager-redone/blob/eafbbae1b8e35c4a164ab17c773d53be6d86c107/src/EishockeyManager.Core/MatchEngine.Original.cs#L132)
und Zeile 133 ziehen `Rnd(0, AttackLevel)`. Der unabhängige Disassembly-Abgleich
von 0x9F92–0xA019 mit der Parameterverwendung der aufgerufenen Funktion 0xBFD5 belegt
**`lrandom(AttackLevel, 7)`**: 7 ist die obere, das Angriffsbitfeld die untere Grenze.
Anschließend wird 5 abgezogen und bei 0 abgeschnitten. Bei Angriff 7 ist der Bonus im Original
daher immer 2; im Head ist er in sechs von acht möglichen Ziehungen 0.
Auch [docs/spielberechnung.md, Abschnitt 3](https://github.com/kaaasi/eishockey-manager-redone/blob/eafbbae1b8e35c4a164ab17c773d53be6d86c107/docs/spielberechnung.md#3-torbudget-und-torchancen-0x9f5c-originalmatch)
übernimmt die falschen Grenzen.

Reproduzierbare Gegenprobe: `Random.Next(min,max)` überschreiben, die angeforderten
inklusiven Grenzen `(min,max-1)` protokollieren und jeweils `min` zurückgeben. Aufruf:

```csharp
OriginalMatch.FullResult(
    new MatchSide(150, AttackLevel: 7),
    new MatchSide(150, AttackLevel: 3), false, false, rng);
```

Die ersten beiden Aufrufe sind **(0,7), (0,3)**, erwartet **(7,7), (3,7)**.
Der daraus gebaute Test ist am Head rot. Nur die beiden Grenzen in der temporären Kopie
korrigiert → derselbe Test grün. Beide Wege, `FullResult` und `MinuteChances`, benutzen
dieses Budget. Korrektur, unabhängige Bereichstests und erneute Ermittlung betroffener
Referenzwerte erforderlich.

**R7-03 · P2 – Nationalmannschaft erreicht die neuen Ereignis-/Spielerzähler nicht.**
[MatchEngine.Live.cs:175](https://github.com/kaaasi/eishockey-manager-redone/blob/eafbbae1b8e35c4a164ab17c773d53be6d86c107/src/EishockeyManager.Core/MatchEngine.Live.cs#L175)
bricht bei leerem `Club.Players` ab. Die WM- und Freundschaftsspiel-Aufrufer übergeben
`RatedNation`, das nur Index, Name und Stärke enthält
([CupsNational.GameState.cs:189](https://github.com/kaaasi/eishockey-manager-redone/blob/eafbbae1b8e35c4a164ab17c773d53be6d86c107/src/EishockeyManager.Core/CupsNational.GameState.cs#L189)).
Der vorhandene `NationalCoach.Squad` und dessen Aufstellung werden nicht angebunden.
Die Erkennung als gemanagtes Team verhindert das nicht.

Gegenprobe mit vorhandenen Testhelfern:

```csharp
var game = GameModeTests.Create(7, false, true, 2);
game.Continue();
game.AutoNominate();
Assert.Equal(FriendlyResult.Played, game.PlayFriendly(52, home: true));
Assert.True(game.NationalCoach!.Squad.Sum(m => m.Player.GoalieMinutes) > 0);
```

Ergebnis am Head: Heim 56 gegen Gast 52 **2:5**, 16 gezählte Länderspiele,
aber Kadersummen **Tore 0, Vorlagen 0, Zweiminutenstrafen 0, Torhüterminuten 0**;
die letzte Assertion scheitert. Das ist keine bloß angenäherte Wahl von Torschütze/Strafspieler:
Die Zählung entfällt vollständig. DOSBox-Befund 45 dokumentiert gefüllte Tore/Strafminuten
nach Freundschaftsspielen; die neue Berechnung enthält bereits Torhüter- und Ereigniszähler.
Die Nationalaufstellung muss beim Anschluss berücksichtigt werden, mit Regression für
Freundschaftsspiel und WM. Die akzeptierte WM-Gruppenspiel-Zuordnung ist davon unabhängig.

**Spielstärke/Meisterhäufung:** Zusätzliche Probe mit Originaldaten, Managerverein 42,
aktiv gemäß `RngTwinProbeTests.Run`, Seeds 1–12, je vier Saisonwechsel:
48 Meistertitel verteilen sich auf Düsseldorf (Index 2) 17, Krefeld 12, Köln 12,
Hedos München 4, Mannheim 2, Kaufbeuren 1. Keine universelle Bindung an einen einzigen Meister;
die beiden festen Referenzseeds sind kein Balancebeweis. Die synthetische Datenbank gibt
den Vereinen 0/12/24 bereits Stärke 100. Die fehlende Streuung der Teamwerte und die
Manager-Abbildung bleiben erkennbare Grenzen. Nach R7-02 sind die Verteilungen erneut zu messen;
eine Übereinstimmung der Meisterwahrscheinlichkeiten mit dem DOS-Spiel ist hier nicht belegt.

### Integrationscommits der Leitstelle

**R7-01 behoben:** Schema 10 mit Torhüterzählern und altem `BasePrice`-Modell lädt,
Kurse werden migriert, Zähler bleiben erhalten. Die echte Form für Schema 9 mit altem
Kursmodell und entfernten Torhüterfeldern durchläuft 9 → 10 → 11 und ist danach stabil
speicher-/ladbar. Beide Tests bestanden. Entfernen des Nachholaufrufs in `MigrateFrom10`
macht R7-01 rot; bedingungsloses Nullsetzen der Torhüterzähler ebenfalls.

Der Freundschaftsspiel-Anschluss erbt R7-03. Die neuen sechs RNG-Abdrücke, Modus-4-Referenz,
Anschluss-Abdrücke, UI-Referenz und der Schemaabdruck sind im vollständigen Lauf reproduziert.
Beim UI-Test bleibt auch „mit/ohne Anzeige“ gleich. Die geänderten Seeds und Suchgrenzen in
Lifecycle-/Prämien-/Werbetests wurden gelesen; ihre fachlichen Prüfziele bleiben vorhanden.

**Aussagegrenze der Sollwerte:** Sie belegen den jetzigen deterministischen Verlauf.
Es wurde auch in diesem Review kein schrittweiser Zwillingsvergleich der Integration gegen
`spielberechnung` allein durchgeführt. Die globale Begründung „Zusammenwirken der Spuren“
isoliert keine erste Abweichung. Insbesondere bestehen sämtliche Referenzen trotz R7-02 und
R7-03; sie sind deshalb kein unabhängiger Nachweis der Originaltreue. Keine Freigabe allein
aufgrund der aktualisierten Sollwerte oder der Testanzahl.

## 3. Ausgeführte Prüfungen und akzeptierte Annäherungen

### Ausführung am verifizierten Head-Inhalt

SDK im Prüfverzeichnis: **10.0.402**. Restore aus vorhandenem vollständigem Paketcache:

```sh
export PATH=/workspace/.dotnet:$PATH
dotnet restore EishockeyManager.slnx --source /home/agent/.nuget/packages \
  --disable-parallel -p:NuGetAudit=false -m:1
NuGetAudit=false UseSharedCompilation=false DOTNET_PROCESSOR_COUNT=1 tools/pruefen.sh
```

Release-Build mit Warnungen als Fehler und alle drei Läufe abgeschlossen, Skriptergebnis
**bestanden**. NuGet-Audit für diesen Offline-Lauf deaktiviert, keine Repository-Konfiguration geändert.
Der erste gezielte VSTest-Start war wegen lokaler Sockets blockiert und zählt nicht als Test.
Nach erteilter Umgebungsfreigabe liefen VSTest und das unveränderte Prüfskript regulär.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1042 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 866 | 0 | 160 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 866 | 176 | 0 |

Der Pflichtlauf wurde vom Skript anhand der TRX-Fehlermeldungen als Scheitern wegen fehlender
Originaldaten bestätigt. Unterschiedliche Fallzahlen entstehen auch durch die Theorie-Ermittlung
beim Überspringen; die Tabelle ist die tatsächliche Skriptausgabe.

Weitere ausgeführte Prüfungen:

- Zuerst `dotnet test tests/EishockeyManager.Core.Tests -c Release --no-restore --filter FullyQualifiedName~IntegrationRunde7MigrationTests`:
  2 bestanden. Anschließend der oben dokumentierte FILE01-CLI-Aufruf.
- Originalarchiv und FILE01–03 per Connector geladen und Git-Blob-Hashes gegen die Remote-Angaben
  geprüft. `em.exe` SHA-256:
  `f41eb08aa3bc3119bd83c44856ccc550d7a450d6ee55e0447378700fefb2294b`.
- Unabhängiger Binärabgleich mit `objdump -D -b binary -m i8086 --start-address=… --stop-address=… em.exe`:
  Angriff/Torbudget 0x9F5C–0xA35B, Zufallsfunktion 0xBFD5–0xC036,
  Stärke 0x368B1–0x369C6, Computer-Reihenwerte 0x97F3–0x98F7,
  Schwächewurf 0xA57F–0xA5AB, Börse 0x5784A–0x57C63, Energie 0x3BB51;
  Tabellen DGROUP 0x4B42, 0x4948, 0x4E99 direkt gelesen.
  Kein vollständiger Maschinenzustands-/Zufallsfolgen-Zwillingslauf gegen DOS.
- Alle 70 geänderten Pfade anhand des Vergleichs erfasst; zentrale Produktdeltas, betroffene
  Aufrufer und neue Tests gelesen. FILE05–08 zusätzlich durch `emtool hok dump` geprüft.
- Zusätzliche Review-Tests und Mutationen nur in separater temporärer Kopie, mit regulärem
  `dotnet test`. Keine Änderung am geprüften Produktcode veröffentlicht.

| Gegenprobe in temporärer Kopie | Ergebnis (bestanden / fehlgeschlagen) |
|---|---:|
| Börsenschritt aus Migration 10 entfernt | 1 / 1 |
| Torhüterzähler in Migration 10 immer auf 0 | 1 / 1 |
| Startbasis 87 → 86, Klasse KaderStartTests | 17 / 2 |
| Börsenstart 90 → 89 Tage, Klasse BoerseStartTests | 12 / 2 |
| R7-02: unabhängige Grenzenprüfung am Head → mit zwei korrigierten Grenzen | 0 / 1 → 1 / 0 |
| Encoder-Prüfsumme +1, Klasse OriginalSavePatcherTests | 19 / 6 |
| Vertragsfeld +0x0B → +0x0C, neuer Formatfeldtest | 0 / 1 |
| Startenergie 70 → 71, Klasse WeltkugelDosboxTests | 14 / 0 (R7-04) |
| Nachrichtenleiste bedingungslos zeichnen, Klasse MenueBefundeTests | 3 / 2 |
| Nationalmannschaft: Torhüterminuten nach Freundschaftsspiel (R7-03) | 0 / 1 |

Die Mutationen sind repräsentative Gegenproben; nicht jeder neue Einzeltest wurde separat
mutiert. R7-03 wurde nicht repariert; kein Rot→Grün-Nachweis dafür behauptet.

### Erwartet offen / akzeptierte `[APPROX]`

- #192–#196 sind weiterhin offen und inhaltlich bestätigt: Nationaltaktik-Startzustand,
  Länderwerte nach Freundschaftsspielen, WM-Gruppen-Paarungsansicht, erster Halt im Modus 1,
  Kursverlaufsregler. Keine erneute Ausarbeitung dieser zurückgestellten Aufgaben.
- Managervereine: 0x386D1, 0x378C5, 0x98F8–0x9C63, 0x87F7 und 0x8EF2 bleiben die benannten
  Annäherungen für Auswahl, Reihenwerte, Energie und Reihenwechsel. Die zugehörige
  Taktik-/Einsatzzuschlagsannäherung und Teamwerte ohne Originalstreuung sind dokumentiert.
- Freundschaftsspiele wie WM-Gruppenspiele und Energie 70 erst vor dem ersten Freundschaftsspiel
  bleiben akzeptiert. R7-03 betrifft den fehlenden Zähleranschluss, R7-04 die unabhängige Absicherung
  des Zahlenwerts, nicht diese Entscheidungen.
- Weitere deklarierte Grenzen: Ersatz beider Original-Zufallsquellen durch Remake-`Random`,
  schneller Pokalweg ohne Anzeige, Darstellung des Penaltysiegers, Statistikzuordnung der
  Computervereine, saisonales Rücksetzen der Zähler, nicht angeschlossenes `OpenNet`,
  nicht rekonstruierte Spielverletzungen; beim HOK-Patcher ID-Auswahl und enge Werteobergrenzen.
- Nicht als nachgewiesen behandelt: exakte Balance gegenüber DOS, vollständige Erklärung
  jedes neuen Referenzabdrucks, neuer DOSBox-Ladenachweis oder erneuter Pixelvergleich.
