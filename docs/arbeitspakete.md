# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 10 – Sonderrunde Analyse em.exe (ab 01.11.2026)

Runde 9 (Bereiche abschließen): [archiv/runde-9.md](archiv/runde-9.md); ältere Runden in [archiv/](archiv/). **Ziel der
Runde (Wunsch Nutzer 01.11.2026):** die noch unbekannten bzw. genäherten Teile von em.exe nach
[re-abdeckung.md](re-abdeckung.md) aus dem Code belegen und so beschreiben, dass Runde 11 und `livespiel` sie umsetzen
können. Stand der Karte auf `main` am 01.11.2026 (nicht committet): Spielcode 82 % umgesetzt, 2 % genähert, 7 %
dokumentiert, 10 % unbekannt.

### Regeln der Runde

- **Nur Doku, keine Code-Änderung** (`src/`, `tests/`, `tools/` bleiben unberührt). Hilfsskripte und Ausgaben
  (Disassembly, CSV, Spielstände) bleiben im Arbeitsbereich bzw. Scratch, nie im Repo; Auszüge aus dem Original
  höchstens als kurze Befehlszeilen mit Adresse.
- **Ein Lauf je Spur, keine Selbstfortsetzung.** Ein PR je Spur (Branch `paket/<spur>`, Base `main`). Reicht die Zeit
  nicht, Draft mit Übergabe und Label `spur-wartet` (Abschnitt „Teile und Fortsetzung“ in `spur-start.md`, Schritt 4
  sinngemäß); die Leitstelle legt nach Entscheidung des Nutzers einen einzelnen Folgeteil an.
- **Dateigrenzen:** je Spur genau eine neue Datei `docs/analyse/<spur>.md` (die Funktionskarte zählt Fundstellen aus
  `docs/**/*.md`). `docs/reverse-engineering.md`, `docs/re-abdeckung.md` und `docs/arbeitspakete.md` (außer dem eigenen
  Statuspunkt) ändert nur die Leitstelle in der Integration.
- **Inhalt je Fundstelle:** Dateioffset (Schreibweise nach `re-abdeckung.md`, Abschnitt „Fundstellen“, damit die Karte
  sie erkennt), Aufrufer und Aufgerufene, Daten (DGROUP-Variablen, Strukturfelder), Regel in Worten bzw. Pseudocode,
  Beleg (Disassembly, Spielstand, DOSBox-Bild des Nutzers), Abgleich mit dem Remake (Datei, bisherige `[APPROX]`-Stelle)
  und Umsetzungsvorschlag für Runde 11 mit Priorität P1–P3.
- **Abnahme je Spur:** alle genannten Adressen zugeordnet oder begründet offen; Abschnitt „Kandidaten Runde 11“ mit je
  einem Eintrag pro Umsetzung (Ziel, Dateien, Testidee); Selbstprüfung mit `funktionskarte.py` (ohne `--write`): die
  eigenen Adressen stehen nicht mehr auf „unbekannt“. Tests entfallen (reiner Doku-PR); `emtool` darf zur Belegerzeugung
  gebaut werden (`tools/claude/setup-workspace.sh`).
- **Leitstelle danach:** eine Integration `integration/runde-10` gegen `main` (alle Spuren, kein Stapel), Karte mit
  `--write` neu, Verweise in `reverse-engineering.md`, ein Claude-Review, Issues für Runde 11 aus den Kandidaten.

### Spur `re-geld` – Geld, Börse, Stadion

- **Adressen:** Stadionbau 0x3D45F; Börse/Konto 0x57ECF, 0x581C2, 0x57CFB, 0x58409; Konto Transfermarkt 0x463CC;
  Grundwert 0x48E62; Jugendförderung 0x39DEC (genähert, Restlücke `finanzen-rest`: Wirkung 0x39DEC/0x3A13B).
- **Abgleich:** `FinanceStocks.cs`, `FinanceYouth.cs`, Stadion- und Kontobuchungen über `GameState.Book`,
  `docs/finanzen-rekonstruktion.md`.
- **Modell:** Claude Opus 5.5. **Status:** wartet auf Go.

### Spur `re-meldungen` – Meldungen, Tagesablauf, Fenster

- **Adressen:** Meldungen 0x4C246, 0x4BA58 (genähert); Infofile 0x432EC; Kurve 0x445E0 (Maßstab `[APPROX]`); Tagesablauf
  0x07C7E, 0x4F325; Fehler/Bildlader 0x25B4A, 0x25764; Fenster Basis 0x902/0x99F.
- **Abgleich:** `docs/tagesablauf.md`, `GameState.Integration.cs`, Meldungs- und Fensterbausteine in `Widgets.cs`.
- **Modell:** Claude Opus 5.5. **Status:** wartet auf Go.

### Spur `re-saisonwechsel` – Manager ohne Liga (#166)

- **Adressen:** 0x562C6 und Umfeld (Saisonwechsel eines Managers ohne Liga, Oberliga-Austausch samt Managervereinen).
- **Beleg im Original:** Spielstand mit `emtool hok patch` (`docs/formate/spielstand-original.md`) aus einer Vorlage im
  Referenz-Repo `original/spielstaende/`; im PR stehen Befehlszeile, Vorlage und eine DOSBox-Anleitung (was spielen, was
  festhalten). Den Spielstand selbst erzeugt die Leitstelle und gibt ihn dem Nutzer; der Nutzer spielt und liefert
  Bilder ins Referenz-Repo. Die Auswertung der Bilder ist ein Folgeteil nach Entscheidung des Nutzers.
- **Abgleich:** `SeasonFlow.cs`, `docs/saison.md`, `[APPROX]` aus #307. **Modell:** Claude Opus 5.5. **Status:** wartet
  auf Go.

### Spur `re-livespiel` – Livespiel Ov 0x130 (nur Analyse)

- **Adressen:** Overlay 0x130, u. a. 0x5353C, 0x52BD6, 0x56EDD (Torszenen, Anzeigetafel, Reporter; Funktionskarte Nr.
  14/16).
- **Ziel:** Grundlage für `livespiel` (letzter Schritt vor v1.0): Ablauf, Ereignisse aus der Spielberechnung, Grafik-
  und Textquellen, Zeitsteuerung. Keine Umsetzung.
- **Modell:** Claude Fable 5.1; blockieren die Schutzmaßnahmen die Analyse, meldet der Lauf das im PR (`spur-wartet`)
  und die Leitstelle startet die Spur nach Freigabe des Nutzers mit Claude Opus 5.5 neu. **Status:** wartet auf Go.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- `livespiel` (Torszenen, Anzeigetafel, Reporter; Nr. 14/16): Umsetzung als letzter Schritt vor v1.0; Analyse in
  `re-livespiel`.
- Runde 11 (normale Runde, Entscheidung Nutzer 01.11.2026): Kandidaten aus Runde 10; P3-Befunde der Integration Runde 9
  – #327, #329, #342, #344, #347, #348, #354, #358, #362, #363, #364; Robustheit der Mehrsaisonproben (#352, #353,
  #357).
- #349 Spieltag mit Managerverein ganz in der Minutenrechnung: Remake-Verhalten bleibt; Original als Einstellung nach
  v1.0 (`docs/anforderungen/zusatzfunktionen.md`, Abschnitt C).
- Import/Export von Original-Spielständen ins Remake; Highscore am Spielende und Saisonende im Modus „nur Weltkugel“
  (Kandidat für eine DOSBox-Aufnahme mit `hok-patch`).
- R8-A01 (Spielstände aus Spurbranches laden nicht): Spurbranches sind keine Ausgabestände.

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
