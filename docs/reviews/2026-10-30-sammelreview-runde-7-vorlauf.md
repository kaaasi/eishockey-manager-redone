# Unabhängiges Sammelreview – Runde 7, Vorlauf

Reviewer: ChatGPT, frische Sitzung ohne Implementierungskontext, 30.10.2026.
Einrichtung einmal für alle vier PRs; Prüfgegenstand jeweils ausschließlich Base → angegebener Head.
Die Empfehlungen gelten vorbehaltlich der allgemeinen Merge-Prüfung aus `docs/prozess/zusammenarbeit.md` §6.

Gemeinsame Einrichtung: SDK **10.0.402**, regulärer Restore und Release-Build mit Warnungen als Fehler.
Der erste Fetch im Sandbox-Kontext scheiterte am Proxy; mit regulär genehmigter Ausführung gelangen Fetch und
Sparse-Klon des Referenz-Repos. Keine Checkout-Rekonstruktion erforderlich. Vier getrennte Detached-Worktrees:
`git rev-parse HEAD` jeweils gleich dem unten genannten SHA, `git status --porcelain` vor und nach den
Standardprüfungen leer. Referenz-Repo: `4890073fbaedc749908c90977bf6f3336841f980`, nur `original/` und `dosbox/`.
`sha256sum -c --quiet SHA256SUMS` erfolgreich; FILE01–03 stimmen mit den SHA-256-Werten ihres README überein.
Originaldaten liegen außerhalb des Code-Repos und sind über ignoriertes `gamedata/` verlinkt.

Zusatztests, Vergleichsmessungen und Mutationen ausschließlich in separaten temporären Worktrees; sie sind
Zusatzdiagnosen und verändern keinen der vier nachgewiesenen Prüfstände. Lokale Protokolle: `/workspace/r7-logs/`.
Keine Testläufe für die Dokumentations-/Plan-PRs #175/#177. Keine Änderungen an den geprüften PRs.

## PR #167 – Weniger als neun Spieler und Spielwertung

Base: `a50a799917e971ac102eecd80c796fd772e6d2ba`  
Head: `16c535a33ac3b3aac80556b44bfba6cc0967e05c` (vollständigen SHA aus PR #167 aufgelöst).

### 1. Merge-Empfehlung

**Ja** für diesen Head. Die angeforderten Gegenbeispiele, Migration 8 → 9 und alle drei Standardläufe
bestehen. Kein neuer P0/P1/P2-Befund. Die Mehrsaisonänderungen haben nachvollziehbare fachliche Ursachen;
die drei passiven RNG-Abdrücke wurden unabhängig gegen die Base nachgemessen.

### 2. Befunde

- **R7V-167-01 · P3 – Kommentar widerspricht dem Speicherformat.**
  [StadiumStatistics.cs:192](https://github.com/kaaasi/eishockey-manager-redone/blob/16c535a33ac3b3aac80556b44bfba6cc0967e05c/src/EishockeyManager.Core/StadiumStatistics.cs#L192)
  nennt `ForfeitsMatch` „Nicht im Spielstand“. Tatsächlich wird es serialisiert; genau deshalb erhöht der PR
  das Schema und ergänzt Migration 8 → 9. Reproduktion: `SchemaEightSave_IsMigrated_ForfeitFlagFalse`
  entfernt das Feld aus jedem serialisierten Verein und prüft dessen Wiederherstellung beim Laden.
  Kommentar an die tatsächliche Persistenz anpassen; keine Funktionsblockade.

### 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

Befehle: `dotnet test tests/EishockeyManager.Core.Tests -c Release --filter FullyQualifiedName~HeimspielStatistikTests`
(**21 bestanden**), danach unverändert `tools/pruefen.sh`.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 909 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 755 | 0 | 139 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 755 | 154 | 0 |

Gesamtergebnis des Skripts: bestanden. Die 154 Pflichtfehlschläge sind vom Skript als fehlende Originaldaten
verifiziert; keine Abstürze. Die unterschiedliche Summe ohne Daten folgt der Discovery datenabhängiger Theorien.

- Acht Aufgestellte: direkte Heim-/Auswärtsabrechnung prüft den Kontodelta −200.000 DM und die Meldung.
  Ligaspiele ergeben zuhause 0:2, auswärts 2:0; kein Eintritt, keine neue Heimstatistik. Pokalspiel ohne Penaltyschießen.
- Unabhängige Zusatzprobe über vier Spieltage, einmal acht Spieler und einmal neun mit einem für 20 Spiele
  Verletzten: beide Heim-/Auswärtsrichtungen, exakte Strafsumme je Ligaspiel, Eintritt und Statistik null.
  Neun gesunde Spieler werden normal berechnet. Verletzte werden vor der Markierung aus der Aufstellung entfernt.
- `MarkShortHandedClubs` steht nach Aufstellungsbereinigung und Entlassungsprüfung, vor Liga, Play-Offs und
  Wettbewerben. Die geprüften Aufrufer führen zum gemeinsamen `MatchEngine.Play`; dessen frühe Rückgabe
  verhindert Ergebniszufallszüge und ein Unentschieden mit anschließendem Penaltyschießen.
- Migration: Feld bei Schema 8 ergänzen, Sortierung, Schemaabdruck und Fortsetzung nach Laden geprüft.
  Die Standardtests decken auch bestehende Migrationsketten und Roundtrips ab.
- Mutationen in separater Kopie: Strafe 1 statt 200.000 (3 Tests rot), Entlassungsprüfung entfernt (1 rot),
  Mindestzahl 10 (2 rot), Verfügbarkeitsfilter entfernt (1 rot), Heim-Wertung deaktiviert (2 rot),
  Migrationsfeld entfernt (1 rot). Jeder Lauf scheitert an Assertions, nicht am Build.
- Original: Abrechnung ab `em.exe 0x54420`, Heimprüfung `0x54589`, Eintritt `0x5464A` und Gastprüfung
  `0x547AA` per Disassembly gegengelesen; Originalbyte-Tests grün. Die Managerreihenfolge der Abrechnung
  ist beibehalten: bei spielfähigem Heimmanager vor einem nicht spielfähigen Gastmanager kann der Heimmanager
  bereits Eintritt erhalten. „Kein Eintritt“ ist hier keine neue globale Sperre für jeden anderen Manager.

**Mehrsaisonbegründungen:** alle geänderten Testdateien gegen die Tabelle in
`docs/finanzen-rekonstruktion.md` abgeglichen. Kaderhalten in DEB-/Europa-/WM-Proben erhält deren
Abrechnungsgegenstand; in UI-/Autosave-Proben verhindert es das vorzeitige Spielende. Die Finanzprobe
verlängert erst nach dem letzten Monatsabschluss und begrenzt die nun möglichen Überziehungszinsen.
`Mode4_ReferenceValues_Unchanged` ändert bereits 1994 das Gehalt, ab 1995 Kader und Folgeergebnisse;
die UI-Pokalreferenz ändert nur 1995/1996. Diese Begründungen passen zu den jeweiligen Änderungen,
alle betroffenen Tests laufen im Standardlauf. Für diese beiden Referenzen wurde kein zusätzlicher
vollständiger Zustandsvergleich je Schritt erstellt.

Die sechs RNG-Proben wurden dagegen zusätzlich auf Base und Head mit identischer Beobachtung nach jedem
Schritt über vier Saisonwechsel ausgeführt (Konto, Kaderzahl, Strafen, eigene Ergebnisse, vier RNG-Zustände):

| Fall | Base-Abdruck → Head-Abdruck | unabhängig beobachtete erste Abweichung |
|---|---|---|
| synthetisch passiv, Seed 3 | `29CDCDB29ABE8389` → `33A4F52EC2360DE0` | 1995/Tag 3: 200.000 DM Strafe, 2:0 statt 4:6; Hauptquelle anders |
| synthetisch passiv, Seed 11 | `8D792630DA011B2B` → `FC797F99C7C8B35C` | 1995/Tag 3: Strafe statt Eintritt, 0:2 statt 4:6; Haupt- und Finanzquelle anders |
| Originaldaten passiv | `4D65B4082DBAA198` → `74ED95088678F3B6` | 1995/Tag 3: 200.000 DM Strafe, 2:0 statt 7:3; Hauptquelle anders |
| drei aktive Fälle | `9541EF4978491142`, `7CB3FC8E58DC44CC`, `9FAAA0D4276178C3` jeweils unverändert | keine Abweichung in beobachteten Feldern oder vier RNG-Zuständen |

Die Kaderzahlen bleiben in den passiven Vergleichspaaren gleich; Ursache ist deren leere Aufstellung,
nicht ein neu eingeführter Kaderverlust. Finanzquelle erstmals verschieden an Tag 17/3/7,
Pokalquelle an Tag 14/17/14. Die Dokumentation zur Vertragsquelle ist als mögliche Folge zu lesen:
im passiven Originaldatenfall blieb sie in dieser Vergleichsmessung gleich.

**Akzeptierte [APPROX]:** beide nicht spielfähig → 0:2, Meldung im Posteingang, Buchung unter „Strafen“,
Verletzte/Gesperrte zählen nicht, entlassene Manager ohne Strafe. Die Originalregel für Verfügbarkeit und
das Verhalten passiver Originalmanager sind damit nicht zusätzlich bewiesen. Beide nicht spielfähig und
ausbleibender Zufallsverbrauch wurden in einer separaten Gegenprobe geprüft.

## PR #176 – Original-Spielstände lesen

Base: `a50a799917e971ac102eecd80c796fd772e6d2ba`  
Head: `22d95f8fd530f44c332bb46221c3f62336366947`.

### 1. Merge-Empfehlung

**Ja** für diesen Head. FILE01–03 werden richtig entschlüsselt, beide Prüfsummen geprüft und die sichtbaren
Werte richtig ausgegeben. Beide gezielt beschädigten Prüfsummen werden erkannt. Alle drei Standardläufe bestanden.

### 2. Befunde

Keine neuen P0–P3-Befunde im geprüften Umfang.

### 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

Zuerst Tools-Release-Build und je Datei
`dotnet src/EishockeyManager.Tools/bin/Release/net10.0/emtool.dll hok dump <FILE01–03.HOK>`,
danach `tools/pruefen.sh`.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 907 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 753 | 0 | 139 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 753 | 154 | 0 |

| Originaldatei | Prüfsumme 0x8F09 / 0xBB4C | Datum | Konto | Eintritt |
|---|---|---|---:|---:|
| FILE01 | FF / E9, beide gültig | 10.09.1993 | 500.000 DM | 13 DM |
| FILE02 | D0 / BA, beide gültig | 10.09.1993 | 500.000 DM | 17 DM |
| FILE03 | D9 / DD, beide gültig | 20.09.1993 | 737.354 DM | 17 DM |

Abgleich mit Originalbildern `file01_1_setup`, `file01_2_hauptmenue`, `file01_3_stadion`,
`file02_1_stadion_eintritt17`, `file03_7_hauptmenue_20sept`: Manager/Verein, Regler,
Datum, Konto, Eintritt, 3.000 Sitz-/4.500 Stehplätze und Stadionstufen stimmen.
Die 18 Einzelspielergebnisse wurden nicht nochmals vollständig gegen alle Paarungsbilder geprüft.

Je eine externe Kopie von FILE01 mit XOR 1 an 0x8F09 bzw. 0xBB4C:
genau die veränderte Prüfsumme „FALSCH“, die andere gültig, Prozess-Exit **3**.
Das Werkzeug gibt Diagnosewerte weiterhin aus; es importiert keinen Spielzustand.

`objdump -D -b binary -m i8086` auf den extern gehaltenen Ausschnitt
`em.exe 0x5A894–0x5AA35`: Schreiben summiert Klartext, XOR mit Schlüssel, Addition des Schlüssels,
Schritt D7; Lesen subtrahiert und XORt, summiert entschlüsseltes Byte. Das entspricht dem Leser.
Beide fortlaufenden Prüfsummen an drei Originaldateien bestätigen Startwert und Behandlung der Prüfsummenbytes.

Fünf unabhängige Mutationen: Blocklänge, inverse Verschlüsselung, Summenstart, Gültigkeitsvergleich,
Kennungsprüfung – jeweils zugehöriger neuer Test rot. `ZeroBytes_EncryptToDoubleKeyStepping` prüft
nur den synthetischen Testdatenerzeuger, nicht den produktiven Leser; dafür keine sinnvolle Lesermutation.
Diff und getrackte Pfade geprüft: keine Originaldateien neu im Code-Repo, Tests ausschließlich synthetisch.

**Akzeptierte Grenzen:** offene Block-/Felddeutungen und fehlende Varianten (mehrere Manager, Weltkugel,
Saisonwechsel/Bundestrainer) bleiben ausdrücklich in `docs/formate/spielstand-original.md`.
Text der originalen Prüfsummenfehlermeldung bleibt offen. Sicher-/Ladesperre wird gemäß Nutzerentscheidung
nicht übernommen. Kein Import/Export und keine Behauptung vollständiger Formatdeutung.

## PR #175 – DOSBox-Befunde

Base: `a50a799917e971ac102eecd80c796fd772e6d2ba`  
Head: `620ec0d520910c0b85d5282832888001a2e3d84a`.

### 1. Merge-Empfehlung

**Ja** für diesen Head als Dokumentationsänderung. Die angeforderten Bildstichproben stimmen;
die Änderung am Fetch-Skript betrifft ausschließlich Aufrufkommentar und Erklärung.

### 2. Befunde

Keine neuen P0–P3-Befunde im geprüften Umfang.

### 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

Keine Testläufe gemäß Auftrag. Diff aller vier Dateien und Originalbilder direkt geprüft:

- 07: Highscore offen, fünf andere Managerkacheln gesperrt.
- 24: „WM-Endrundenspiele“, „Die Paarungen stehen noch nicht fest…“, Knopf „Gruppe“.
- 26: sichtbare Taktikwerte null, AUTOM./MANUELL/Automatisch wie beschrieben.
- 38/39: 3:1 Schweden, 1:3 Finnland, 14:1 Österreich.
- 40: unverändert 14. April, Konto 260.000 DM; Mannschaftskacheln wie beschrieben.
- 41–43 gegen 27–29: Gesamt 88/90/52 → 91/93/57, Abwehr 88/90/53 → 91/93/58,
  Sturm 89/90/52 → 92/93/57; jeweils +3/+3/+5, übrige Länderwerte unverändert.
- Zusätzlich 06 und 08: Halt im Hauptmenü am 14. April und WM-Menü vor der Kaderauswahl sichtbar.

Die zwei entfernten [APPROX]-Markierungen in `docs/wettbewerbe.md` sind hinsichtlich Halt und
vorhandenem Bundestrainermenü nachvollziehbar. Die Sperre von Mannschaft/Stärken vor Ernennung beruht
ausdrücklich auf der in `docs/dosbox-befunde.md` gekennzeichneten Nutzerbeobachtung, nicht auf einem
separaten Sperrbild. Zeitpunkt/Auslöser der Ernennung ist durch die Bilder nicht bewiesen.

`fetch-reference.sh`: nur Dokumentation des bereits durch den allgemeinen Ordnerfall unterstützten
Arguments `dosbox`; keine Änderung der Aktualisierungs-, Sparse-Checkout- oder Schutzlogik.

**Erwartet offen bestätigt:** Ernennung, Reihe Manager danach, Bedeutung der Torszenen-Stufe 3.
Fensterfotos belegen Inhalt, keinen exakten Pixelvergleich. Die Energie-Näherung bleibt markiert.

## PR #177 – Plan Runde 7

Base: `a50a799917e971ac102eecd80c796fd772e6d2ba`  
Head: `8546faf355c186168807726cf38ba6442e8d60fa`.

### 1. Merge-Empfehlung

**Nein** für diesen Head: R7V-177-01 muss vor Freigabe des parallelen Plans geklärt werden.
Das Archiv übernimmt den alten Rundeninhalt unverändert; die sechs Spuren haben weitgehend konkrete
Abnahmen, aber keine vollständig überschneidungsfreien Dateizuständigkeiten.

### 2. Befunde

- **R7V-177-01 · P2 – Spielberechnung und Weltkugel bearbeiten dieselbe produktive Datei.**
  [arbeitspakete.md:60](https://github.com/kaaasi/eishockey-manager-redone/blob/8546faf355c186168807726cf38ba6442e8d60fa/docs/arbeitspakete.md#L60)
  erlaubt `spielberechnung` Signaturänderungen in `Cups*.cs`;
  [Zeile 103](https://github.com/kaaasi/eishockey-manager-redone/blob/8546faf355c186168807726cf38ba6442e8d60fa/docs/arbeitspakete.md#L103)
  erlaubt `weltkugel-dosbox` `Core/CupsNational*.cs`.
  Konkretes Gegenbeispiel: `CupsNational.GameState.cs:164` ruft in `PlayFriendly` den zu ersetzenden
  `MatchEngine.Play` auf; dieselbe Methode berechnet Kosten, Ergebnis und Wertebonus, die die Weltkugel-Spur
  ändern soll. Nach Merge der Vorlauf-PRs dürfen beide Spuren parallel starten. Die allgemeine Integration
  „Spielberechnung zuletzt“ weist die gemeinsame Methode keinem eindeutigen Bearbeiter zu.
  Abhilfe: konkrete Übergabe/Reihenfolge für diese Datei oder Signaturanpassung hier allein durch eine Spur,
  mit expliziter Ausnahme für die andere. Gemeinsame Tests/Referenzdateien ebenfalls konkret zuordnen;
  pauschal „Tests“ grenzt deren Besitz nicht ab.
- **R7V-177-02 · P3 – Archivlinks zeigen nach dem Verschieben ins Leere.**
  [runde-6.md:11](https://github.com/kaaasi/eishockey-manager-redone/blob/8546faf355c186168807726cf38ba6442e8d60fa/docs/archiv/runde-6.md#L11):
  `(archiv/runde-5.md)` wird relativ zu `docs/archiv/` als
  `docs/archiv/archiv/runde-5.md` aufgelöst; auch `(archiv/)` ist dort falsch.
  Gegenprobe: diese Ziele existieren nicht, `docs/archiv/runde-5.md` dagegen schon.
  Links an den neuen Ablageort anpassen, ohne den historischen Inhalt umzuschreiben.

### 3. Ausgeführte Prüfungen und akzeptierte [APPROX]

Keine Testläufe gemäß Auftrag. Vollständiger Diff der zwei Dateien, sechs Spurabschnitte und betroffene
Aufrufer gelesen. Textvergleich: Base-`arbeitspakete.md` ab „Runde 5 (Auslosung …)“ bis vor
„Zurückgestellt“ ist gleich dem entsprechenden Hauptblock in `archiv/runde-6.md` (abschließende Leerzeilen
ignoriert). Hinzu kommen Archivüberschrift, Herkunft und Ergebniszusammenfassung; historische Statuszeilen
wurden nicht still aktualisiert.

Abnahmekriterien: rote/grüne Startwerte-, Menü- und HOK-Tests sowie Nutzerabnahme des gepatchten Stands
sind prüfbar; Spielereignis-Konsistenz, Erhalt von 0:2 und Migration ebenfalls. Für „Größenordnungen der
Videos“ und Pixelvergleich „mit Kennzahl wie in Runde 6“ müssen die Implementierungs-PRs konkrete
Referenzen, Messgrößen und akzeptierte Abweichungen festhalten. Das ist keine vorweggenommene
Originaltreue-Freigabe. Die Weltkugel-Abnahme darf offene Punkte nur begründet als Befund stehen lassen.

Keine neue [APPROX]-Implementierung. Torszenen/Reporter bleiben zurückgestellt, HOK-Abnahme im Original
bleibt ein späterer Nutzerschritt. Die spätere Staffelungsregel ist spezifischer als der davor stehende
pauschale Satz, alle Spuren starteten erst nach allen drei Vorlauf-Merges; diesen Satz redaktionell angleichen.
