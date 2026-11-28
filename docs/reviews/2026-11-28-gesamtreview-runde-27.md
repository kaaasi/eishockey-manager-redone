# Gesamtreview Runde 27 · Aufräumen #936

## 1. Merge-Empfehlung

**Ja für `8fa9aafa5a7c70f81d3bf5cfa7ba496dfe44b613`** gegenüber `6506d623f75df9b3613cf9230c6c983e295e5eed`.
Die feste Partie durchläuft auf beiden Ständen vier Saisonwechsel mit identischem Zustand. Der Lader zieht die beschlossene
Grenze bei Schema 65. Die gezielten Proben zu Markt, Wettbewerben, Finanzen und Bedienung ergaben keinen neuen Produktfehler.

## 2. Befunde

**Produktfehler:** keine nachgewiesen.

**P3 · Testlücke bei #1070:** Der Rauchtest erkennt den vollständigen Verlust des zweiten Vertragsbilds nicht.
In einer separaten Head-Kopie nur `d.Save("02_saisonende_karriere_03555");` entfernt. Mit Originaldaten bleibt
`dotnet test tests/EishockeyManager.UI.Tests -c Release --filter 'DisplayName~Scenario_RunsAndWritesImages&DisplayName~transfer-vertrag-kauf-saisonende'`
grün (1/1), weil das Kaufbild bereits existiert. Im unveränderten Head entstehen beide Bilder. Die Lücke betrifft die
Vollständigkeit der Bildserie. [Rauchtest, Z. 24](https://github.com/kaaasi/eishockey-manager-redone/blob/8fa9aaf/tests/EishockeyManager.UI.Tests/ScenarioSmokeTests.cs#L24) · [zweites Bild, Z. 84](https://github.com/kaaasi/eishockey-manager-redone/blob/8fa9aaf/src/EishockeyManager.Tools/Scenarios/TransferVertragKaufSaisonende.cs#L84)

## 3. Ausgeführte Prüfungen und offene Punkte

`git rev-parse HEAD` und `git status --porcelain` vor der Prüfung bestätigten den sauberen Head. SDK 10.0.402,
Originaldaten und HOK-Stände aus dem Referenz-Repo. Release-Build mit Warnungen als Fehler.
`NuGetAudit=false DOTNET_PROCESSOR_COUNT=3 tools/pruefen.sh --schnell` mit lokalem Paketcache (erster Build wegen
unerreichbarem NuGet-Auditdienst mit NU1900 abgebrochen).

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4084 | 0 | 0 |

- **Zustandsabdruck:** Den C#-Block aus der Beschreibung von #1059 unverändert nach `tests/EishockeyManager.UI.Tests/`
  in zwei separaten Worktrees kopiert. Jeweils `EM_REQUIRE_GAMEDATA=1 EM_HARNESS_OUT=<ziel> dotnet test tests/EishockeyManager.UI.Tests -c Release --filter Zz_MigrationHarness`
  → 2/2. Saat 1993, passiv ein Manager, aktiv zwei Manager mit Laden nach jedem Wechsel.
  `diff -qr <base-ziel> <head-ziel>` leer für acht Abdrücke und zwei Logs, ohne ausgeblendete Felder.
  Passiv von `427150EB075036B9` bis `1FD1AA5409D04D4E`, aktiv von `D9D7F4C45B5A87CB` bis `BBCD733F833309A6`.
  Jede Schema-65-Rundreise meldet „gleich“ nach der im Harness vorgesehenen Normalisierung der Sammlungstypen.
  Der Harness setzt das Managervermögen vor jedem Weiterschalten auf 10 Mio. DM.
- **Ladegrenze und Pool:** `SaveGameTests.Schema64_CanNoLongerBeLoaded` prüft denselben vollständigen Stand mit Schema 64 und 65.
  Zusatzprobe mit `SaveGame.Load` auf JSON aus `kennung=SaveGame.Identifier` und `schema=n` für jedes n von 1 bis 64:
  jeweils `WrongVersion` und exakt „Spielstände mit Schema n können nicht mehr geladen werden.“ A221/A222/A392 an
  Poolaufbau und Marktlisten verfolgt: Spielstart und HOK-Import liefern einen Pool, freigestellte Spieler landen in
  `_freeAgents`. Seltene bereits früher migrierte 65er-Stände ohne Listenwechsel deckt die feste Partie nicht ab.
- **Markt und Saisonwechsel:** `OfferExpiryTests.MarketBid_Expires_WhileListingStays` geht über `Continue()`. Der einzige Aufruf von `AdvanceBids` würfelt Angebote
  des Verkäufers. Der Tagesablauf würfelt den Verfall je Markt-Platz. `python3 tools/reverse-engineering/abweichungen/t03_markt.py gamedata/em.exe`
  zeigt den Wurf auf allen vier vorbereiteten Plätzen und Verfall auf Platz 0. `t03_saisonwechsel.py` mit derselben EXE
  zeigt Wanderung vor Ligamischung ohne Schreibzugriff auf Tageszähler oder Tagesindex. `SeasonEndObserver` ist threadlokal,
  wird nur im Test gesetzt und im `finally` gelöscht. Der Test vergleicht den Zufallszustand nach den Länderwürfen.
- **Wettbewerbe/Kader:** Bei 3:3 lieferte `KnockoutMatch.FromReport` für beide Werte von `ShootoutHomeWins` den jeweiligen
  Sieger, bei fehlendem Wert die vorgesehene Ausnahme. Minutenweg und Schnellweg setzen den Sieger bei K.-o.-Gleichstand,
  kampflose Spiele enden 2:0 oder 0:2. `c07_marken.py gamedata/em.exe` bestätigt u. a. eine Marke nach Verlängerungstor.
  Im Diff K8 dieselben Angriffs-/Abwehrwerte, K10 dieselbe Heim-/Gastbilanz, K12 weiterhin Grenzen 1 (Play-off) und 2
  (DEB-Pokal). Bei K19 unterscheiden Aufstellung und Spielvorbereitung weiterhin alle von aktiven Managern.
- **Finanzen/U10:** `emtool hok patch FILE01.HOK <ziel> --id 17 konto=-123456 eintritt=41 sitz=1234 steh=5678` und
  `emtool hok dump <ziel>` auf Base und Head. `cmp` für HOK (47.949 Byte) und Dump jeweils gleich, Prüfsummen gültig.
  Beide Dumps lesen −123.456 DM, 41 DM Eintritt und 1.234/5.678 Plätze zurück.
- **Bedienung/U1 und #1070:** Alle 79 Rauchtest-Fälle bestanden. Mit `emtool screens gamedata <ziel> transfer-vertrag pixel-verein` sind alle neun PNGs
  zwischen Base und Head bytegleich, darunter Vertragsdialog, Transferliste, Finanzen und Aufstellung. Auf Base brechen
  `pixel-verein-bundestrainer` („Kein Zug des Bundestrainers erreicht.“) und `transfer-vertrag-kauf-saisonende`
  („Verhandlung am Saisonende fehlt“) ab. Auf Head liefern die gleichen Aufrufe fünf bzw. zwei Bilder.
  Im Zielbild des Vertragsablaufs stehen wie in `karriere_03555` A.ONNEN, 27 Jahre, 1 Jahr und 6.480 DM.
  Das Bundestrainerbild öffnet wie `eisbaeren_f24_00040` das deutsche Menü mit Deutschlandflagge.
- **Relative Links:** Alle versionierten Markdown-Dateien (373 → 382) je Datei mit `pandoc -f gfm -t json` gelesen.
  Relative Link-/Bildziele gegen `git ls-files` und dessen Verzeichnisse aufgelöst, HTML-Verweise einbezogen,
  Markdown-Abschnittsverweise gegen gerenderte Überschriften geprüft. 286 → 290 relative Pfadverweise,
  92 → 54 ungelöste Vorkommen. Nach Zuordnung verschobener Quelldateien mit `git diff -M --name-status` kein neuer Bruch.
  Alle 54 verbliebenen stammen aus dem Archiv.

**Erwartet offen / akzeptierte [APPROX]:** #1075 (Über-/Unterzahl-Doku) und #1078 (alte Fundorte und Testname) auf GitHub
offen mit P3. K1 bleibt: Vereinsaufstellung verwaltet die Reserve über Spieler-IDs, die Nationalaufstellung über Objekte.
Die CSV der Restliste führt auf Base und Head dieselben 29 IDs in Klasse D, Auswertung in `abgleich-dosbox-22b`.
A1035 setzt für den Bildschirmablauf am ersten WM-Tag den Bundestrainer ein. Diese Setzung ist auf das Szenario beschränkt.

**Prüfgrenze:** Vergleich einzelner Zielbilder, kein vollständiger Pixelabgleich. Die separate Funktionskarte der gesamten
EXE ließ sich ohne `ndisasm` nicht neu erzeugen. Die drei genannten gezielten Proben liefen mit der Original-EXE.
