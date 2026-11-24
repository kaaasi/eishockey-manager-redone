# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 25 – Planung offen (neue Leitstellen-Sitzung, `docs/vorlagen/leitstelle-start.md`)

Runde 24 abgeschlossen am 24.11.2026: [archiv/runde-24.md](archiv/runde-24.md). Stand: Restliste
[analyse/restliste-v1.md](analyse/restliste-v1.md) **V = 0, X = 0**, D 26, K 224, E 323.
**Auftrag Runde 25 (Entscheidungen Nutzer 24.11.2026):** Abweichungen umsetzen nach
[analyse/abweichungen-v1.md](analyse/abweichungen-v1.md) **Abschnitt 0** (15 Themen aus Abschnitt 1, dazu B31, B11, K07, S05, S07,
T03, S01; Prüfungen B16, F06, F08; aus dem Aufräumkonzept K2, K9, K11, K13). Je Thema „wie im Original“, Beleg aus em.exe vor der
Änderung, wo die Liste das verlangt (B12, B13, B16, B28, B29, S02). Klassen je Thema: [analyse/abweichungen-bewertung-claude.md](analyse/abweichungen-bewertung-claude.md).
**Danach:** Aufräumen #936 nach [analyse/aufraeumen-v1.md](analyse/aufraeumen-v1.md) (A1–A6 entschieden, mit #978, #980), dann
Abschlussprüfung. **Modellwahl (Nutzer 17.11.2026):** je Lauf möglichst wenig Tokens für ein brauchbares Ergebnis – Haiku, Sonnet,
Opus; Fable nur in Ausnahmefällen. **Läufe melden ihr Ende** per Push-Benachrichtigung (Nutzer 22.11.2026, `prozess/anhang.md` F).

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

### Vormerkungen

- **DOSBox-Aufnahme `runde-22` Sitzungen 2–6** und **Sitzung 7** (neue D aus Runde 22: A671, A713/A949, A896, A899, A910, A912,
  A918, A945; Liste nach R23-D3 in `vorlagen/dosbox-aufnahme.md`): Aufnahme nach Lust, Auswertung in der Runde danach.
- **Abschlussprüfung:** 24 X-Funktionszeilen sind im Generator ausgeblendet (#976) – dort bestätigen. P3 #978, #980 im Aufräumen
  (R24-D2 = B).
- **Vor v1.0 (R20-D3-Zusatz):** Abweichungsliste vorgelegt und entschieden (24.11.2026, Runde 24).
- **Letzter Schritt vor v1.0 (Nutzer 17.11.2026, #936):** Aufräumen des gesamten Repos, danach nur noch die Abschlussprüfung.
- **Testlücke Gesamtreview #848:** Speichern/Laden mitten in einer angehaltenen Live-Paarung (über die Oberfläche nicht
  erreichbar; prüfen, sobald ein Speicherweg im Live-Spiel dazukommt).
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
