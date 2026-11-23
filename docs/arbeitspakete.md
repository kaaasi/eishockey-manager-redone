# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 24 – Abweichungsliste vor v1.0, P3-Rest, Konzept Aufräumen (Plan 23.11.2026, Entscheidungen offen)

Runde 23 abgeschlossen am 23.11.2026: [archiv/runde-23.md](archiv/runde-23.md). Stand: Restliste
[analyse/restliste-v1.md](analyse/restliste-v1.md) **V = 0, X = 0**, D 26, K 224, E 323. Offen: P3 #978, #980; #936; Vormerkungen
unten. Keine neue DOSBox-Sitzung von `runde-22` aufgenommen (Referenz-Repo `main` `7412a50`), D 26 bleibt bis zur Aufnahme.
**Ziel der Runde:** Der Nutzer bekommt die Abweichungsliste (R20-D3-Zusatz) in einer Form, in der er je Thema „bleibt“ oder
„ändern“ entscheiden kann; die P3 sind erledigt; für #936 liegt ein Konzept mit den Nutzerentscheidungen vor. Umsetzung der
Änderungswünsche aus der Liste in Runde 25, Aufräumen danach (letzter Schritt vor v1.0), dann Abschlussprüfung.
**Entscheidungen offen** (Optionen im Plan-PR): R24-D1 Form der Abweichungsliste, R24-D2 P3 #978/#980, R24-D3 Aufräumen #936.
Spuren erst nach Go.

**Regeln wie Runde 23** (Archiv): Base = aktueller `main` beim Start, ein Branch `paket/<spur>`, ein PR, höchstens drei
Fortsetzungen; kein `markerliste.py --write`; keine geplanten Aufgaben aus Läufen (R15-E4). Bei Unklarheit „wie im Original“,
sonst Leitstelle bündelt. Entwickler und Erstreviews sind Claude-Sitzungen. **Kein Gesamtreview** (keine Änderung am
Spielverhalten in dieser Runde; nur Doku und ein Test).

**Folge (Vorschlag):** alle Spuren parallel in Welle 1, Dateien überschneiden sich nicht; Integration in der Reihenfolge der
Fertigstellung.

| Spur | Welle | Inhalt | Modell | Grund |
|---|---|---|---|---|
| `abweichungsliste` | 1 | R24-D1: Abweichungsliste verdichtet als Entscheidungsvorlage | Opus 5.5 | Einordnung „sichtbar für Spieler“ und Aufwand braucht Code- und em.exe-Kenntnis |
| `p3-rest` | 1 | R24-D2: #978, #980 | Sonnet 5.5 | einfache Testergänzung, Wortlaut, Kommentar |
| `aufraeum-konzept` | 1 | R24-D3: Konzept #936 ohne Umbau | Opus 5.5 | Architektur, Migrationen, Risiken |

Erstreviews: `abweichungsliste` und `aufraeum-konzept` Sonnet 5.5 (reine Doku), `p3-rest` Sonnet 5.5. Fixläufe im Modell der Spur.
Plan-Review Sonnet 5.5.

### Spur `abweichungsliste` – Entscheidungsvorlage vor v1.0 (Welle 1, R24-D1)

- **Teile:** 1. `python3 tools/reverse-engineering/markerliste.py --abweichungen` auf aktuellem `main` (nur lesen) und die
  Ausgabe in Themen bündeln (gleiche Funktion bzw. gleicher Bildschirm, Doppelzeilen wie A768/A769 zusammen): je Thema IDs,
  Abweichung in einem Satz, **sichtbar für den Spieler** ja/nein, Aufwand S/M/L für „wie im Original“, Vorschlag „bleibt“ oder
  „ändern“ mit Grund; E aus Nutzerentscheidungen (N1, N2, R15-E3 usw.) mit Verweis, Vorschlag „bleibt“. Oben eine Übersicht je
  Bereich (Anzahl Themen, davon sichtbar, davon Vorschlag „ändern“); ganz oben die Themen mit Vorschlag „ändern“. D-Zeilen
  getrennt (warten auf DOSBox). Die Vollliste als Anhang derselben Datei (generiert, unverändert).
- **Dateien:** `docs/analyse/abweichungen-v1.md` (neu). **Prüfung:** entfällt (nur Doku); Probe im PR: Anzahl IDs der
  Vollliste = Anzahl IDs in den Themen. **Erstreview:** Sonnet 5.5 (Stichprobe 20 Themen gegen Code/Fundstelle).
- **Status:** wartet auf Go.

### Spur `p3-rest` – P3 aus Runde 23 (Welle 1, R24-D2)

- **Teile:** 1. #978: Fall Europapokalfeld (`[bp+0xA] = 8`, Datei 0x5494C–0x5499E) in `SpielwertungVorbereitungTests.cs`
  ergänzen, Rückmutation im PR; 2. #980 P3-1 (`restliste-v1.md:52`, außerhalb des generierten Blocks: A899 statt A964), P3-2 (Abschnittsverweise A962, A966), P3-3 (Kommentar
  `MatchEngine.Managed.cs:110` mit Bedingung +0x0A ≠ 0, Datei 0x54918–0x54927), P3-4 nur bestätigen (PR-Text, kein Code).
  Fixes #978, #980.
- **Dateien:** `tests/EishockeyManager.Core.Tests/SpielwertungVorbereitungTests.cs`, `Core/MatchEngine.Managed.cs` (nur Kommentar), `docs/analyse/restliste-v1.md`
  (nur Zeilen 1–173) und die Doku-Stellen aus P3-2. **Prüfung:** `tools/pruefen.sh --schnell`. **Erstreview:** Sonnet 5.5. **Status:** wartet auf Go.

### Spur `aufraeum-konzept` – Konzept für #936 ohne Umbau (Welle 1, R24-D3)

- **Teile:** 1. Bestandsaufnahme aller Teile des Repos nach #936 (`src/` aller Projekte, `tests/`, `tools/`, `docs/`): Dateien
  nach Spur/Runde benannt, Zielnamen nach Thema, Migrationen (Anzahl, Bündelung vs. alte Remake-Stände nicht mehr laden),
  verwaiste Skripte, veraltete Doku, Verdacht auf doppelte Logik und tote Pfade (mit Fundstelle); 2. Zielstruktur und Zuschnitt
  der Umbau-Spuren (Verhalten unverändert, alle drei Prüfläufe), Reihenfolge, Risiken, offene Nutzerentscheidungen mit 3–5
  Optionen. **Keine** Datei verschieben oder ändern außer der Konzeptdatei.
- **Dateien:** `docs/analyse/aufraeumen-v1.md` (neu). **Prüfung:** entfällt (nur Doku). **Erstreview:** Sonnet 5.5.
- **Status:** wartet auf Go.

**Modellwahl (Nutzer 17.11.2026):** je Lauf möglichst wenig Tokens für ein brauchbares Ergebnis – Haiku, Sonnet, Opus; Fable nur
in Ausnahmefällen. **Läufe melden ihr Ende** per Push-Benachrichtigung (Nutzer 22.11.2026, `prozess/anhang.md` F).

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen

- **DOSBox-Aufnahme `runde-22` Sitzungen 2–6** und **Sitzung 7** (neue D aus Runde 22: A671, A713/A949, A896, A899, A910, A912,
  A918, A945; Liste nach R23-D3 in `vorlagen/dosbox-aufnahme.md`): Aufnahme nach Lust, Auswertung in der Runde danach.
- **Abschlussprüfung:** 24 X-Funktionszeilen sind im Generator ausgeblendet (#976) – dort bestätigen; P3 #978, #980.
- **Letzter Schritt vor v1.0 (Nutzer 17.11.2026, #936):** Aufräumen des gesamten Repos, danach nur noch die Abschlussprüfung.
- **Testlücke Gesamtreview #848:** Speichern/Laden mitten in einer angehaltenen Live-Paarung (über die Oberfläche nicht
  erreichbar; prüfen, sobald ein Speicherweg im Live-Spiel dazukommt).
- **Vor v1.0 (R20-D3-Zusatz, Nutzer 15.11.2026):** Liste aller noch abweichenden Bereiche und Funktionen an den Nutzer
  (`python3 tools/reverse-engineering/markerliste.py --abweichungen`), der entscheidet, was noch geändert wird.
- **Mit v1.0 und dem bitgenauen Zufallsmodus (N6 = A, Nutzer 16.11.2026):** eigene Zufallsquellen der Darstellung (A061, A271,
  A274, A438, A452) erneut prüfen.
- **Nach v1.0:** Einstellungen aus R15-E3, bitgenauer Zufallsmodus (`anforderungen/zusatzfunktionen.md` C), Sprachen und
  Währungen (N1 = B, `anforderungen/zusatzfunktionen.md` A 2).

## Paket `hok-format` – Original-Spielstände `*.HOK`

Branch: `paket/hok-format` · Status: Analyse gemergt (#176, Review #178): Verschlüsselung, Prüfsummen, Kopf und Blockfolge
belegt, `emtool hok dump`, Doku `docs/formate/spielstand-original.md` (offene Liste dort). Entscheidung Nutzer:
Sicher-/Ladesperre des Originals nicht übernehmen. Weiter in Runde 7 mit `hok-patch` (Spielstände fürs Original);
Import/Export ins Remake danach.

- Nachtrag (#197, Branch `paket/hok-format-2`): Kaderplätze, Spielerpool, Training und Taktik an FILE05 belegt;
  Kaderausgabe in `emtool hok dump`. Spielstände FILE01–05 und `KAPUTT.HOK` im Referenz-Repo `original/spielstaende/`.

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: PR 1–7 gemergt (Runde 3); Restarbeiten in Runde 4 gemergt; PR 8 in Runde 5 gemergt (`paarungen-liga`, #128). Regeln und Belege: [wettbewerbe.md](wettbewerbe.md)

| PR | Inhalt | Stand |
|---|---|---|
| 1 | DEB-Pokal: Auslosung, Heimrecht, „best of three“, Abrechnung, Bildschirm „DEB-Pokal Auslosung“ | gemergt (Runde 3, #54) |
| 2 | Europapokal (Gruppe + Endrunde), Bildschirm Gruppen/Endrunde | gemergt (Runde 3, #54) |
| 3 | WM (2 Gruppen + K.-o.), Bildschirm Gruppe/Tabelle WM | gemergt (Runde 3, #54) |
| 4 | Stärken (Ligen, Länderteams) | gemergt (Runde 3, #54) |
| 5 | Statistiken: Ewige Tabelle (Totale), Früher | gemergt (Runde 3, #54) |
| 6 | Bundestrainer: WM-Kader, „WM Vorbereitung“ (Freundschaftsspiele), Etat; WM: Werte Deutschlands neu ausgewürfelt, Länder 20-mal getauscht | gemergt (Runde 3, #54) |
| 7 | Spielmodus „nur Weltkugel“: Spielvorbereitung, Vereinssaison überspringen, Menü des Bundestrainers | gemergt (#87 über #89) |
| 8 | Paarungen Liga | gemergt (Runde 5, #128); Pixelvergleich Runde 6 (`pixel-wettbewerbe`) |
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | Runde 8 (`statistiken`, `nationalteam`) |

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: Format, Dialog und automatisches Speichern gemergt (Runde 3, [archiv/runde-3.md](archiv/runde-3.md)); #80 in Runde 4 behoben (#105)

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`) und Export per Vorlage (`hok-format`) – wartet auf Original-Spielstände des Nutzers.

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen auf den Ereignissen aus `spielberechnung` (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
