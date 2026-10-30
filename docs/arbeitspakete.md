# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 6 – Restbefunde und Pixelvergleiche mit den neuen Referenzbildern (ab 30.10.2026)

Runde 5 (Auslosung, Paarungen Liga, Taktik, Modus 4, Finanz-Nachprüfung, Referenzvideos): [archiv/runde-5.md](archiv/runde-5.md);
ältere Runden in [archiv/](archiv/).
**Ziel der Runde (Entscheidung Nutzer, 30.10.2026):** die offenen Befunde aus Runde 5 schließen und die Bildschirme, die
bisher ohne Videobeleg gebaut wurden, gegen die neuen Referenzreihen vermessen (`docs/referenz-videos.md`, Fundlisten;
laden mit `tools/claude/fetch-reference.sh <reihe>`).
**Base für alle Spuren:** aktueller `main` (≥ diesem Plan); den tatsächlichen Base-SHA trägt die Spur in ihren Draft-PR ein.
Weiterhin verbindlich: Mehrsaisonprobe für jede Spur, die Saisonwechsel, Kader oder Geld berührt; Spielstand-Schema mit
Version und Migration für jeden neuen Zustand; Befund-Issues mit `Fixes #<nr>` schließen. **Entwickler sind Claude-Sitzungen;
ChatGPT nur für Reviews** (Entscheidung Nutzer, 29.10.2026).

```
Runde 6 (parallel):  finanzen-werbung   heimspiel-statistik   saisonende-status
                     pixel-wettbewerbe  pixel-verein
```

Gemeinsame Berührungen: `finanzen-werbung` und `heimspiel-statistik` arbeiten beide in `Core/Finance*.cs`, aber in
getrennten Dateien (siehe Dateigrenzen). `saisonende-status` und `heimspiel-statistik` können sich in `Core/SeasonFlow.cs`
treffen (der Zweite rebased). `pixel-wettbewerbe` und `pixel-verein` teilen sich `UI/Screens/Competitions/` nur über
`NationalMenu.cs`, das allein `pixel-verein` ändert. Neue Schemaversionen: Die Leitstelle nummeriert in der Integration
fortlaufend (wie Runde 5, Migration 4 → 5 → 6).

### Spur `finanzen-werbung` – Werbebewertung, Werbeflächen, Monatsletzter (#132, #133, #134)

- **Ziel:** die P2-Befunde der unabhängigen Finanz-Nachprüfung NP-01 (#132) und NP-02 (#133) umsetzen, zusammen mit
  NP-03 (#134, Monatsabschluss am Monatsletzten), weil die Werbebuchung daran hängt.
- **Belege:** die Issues und `docs/finanzen-rekonstruktion.md`, Abschnitt „Unabhängige Nachprüfung“ (em.exe 0x3F833 ff.,
  DGROUP 0x4F05, 0x2B56A, 0x2A051, 0x3EE6B, 0x3EF52, 0x3F02C); Referenzbilder Werbebildschirm und Vereinsstatistik
  „Einnahmen/Ausgaben / Monat“ (`speedrun_t1_00456`, `folge01_00522`).
- **Abnahme:** (1) je Regressionsbeispiel aus #132/#133/#134 ein roter Test, danach grün; (2) Werbebewertung, Faktoren,
  Angebotsbetrag, Erneuerungs- und Buchungstage wie em.exe, Vertragsende mit und ohne Option wie beschrieben;
  (3) Mehrsaisonprobe (4 Wechsel, passiv und aktiv) mit begründeter Änderung jedes Referenzwerts (Ursache je Fall wie
  bei FN-01, #139); (4) Spielstand: Schema/Migration nur bei neuem Zustand.
- **Dateigrenzen:** `Core/FinanceSponsors.cs`, `Core/FinanceSponsorRating.cs`, `Core/FinanceInbox.cs`, `Core/FinanceMonthly.cs`,
  `Core/GameState.Manager.cs` (Tag des Monatsabschlusses), `UI/Screens/Manager/AdvertisingScreen.cs`, `Core/SaveGame.Migration*.cs`
  (neue Datei), `docs/finanzen-rekonstruktion.md`, Tests `Finance*`.
- **Modell:** Claude Opus 5.5.
- **Status:** offen.

### Spur `heimspiel-statistik` – Zuschauerstatistik, „Weniger als neun Spieler“, Spielbedeutung (#136)

- **Ziel:** die Restlücken aus NP-05 (#136) schließen: Zuschauerstatistik je Heimspiel (Verlauf, Summe, Rekord,
  Minuskulisse mit Gegner, Zähler), Abzug 200.000 DM bei weniger als neun Spielern, Spielbedeutung 0–2 der
  Zuschauerformel; den Nebenbefund Oberliga-Austausch (0x562C6) prüfen und nur bei Beleg umsetzen.
- **Belege:** #136 (em.exe 0x54664, 0x54420, [0xD0C2], 0x562C6); Karriere-Video (Saison-Schnitt, Rekord, Minuskulisse,
  `docs/finanzen-rekonstruktion.md`); Vereinsstatistik in den neuen Reihen.
- **Abnahme:** (1) roter Test je Punkt; (2) Statistikwerte gegen das Karriere-Video (Saison 1: Schnitt 3.049,
  Minuskulisse 1.046, Rekord 4.500); (3) Mehrsaisonprobe mit begründeten Änderungen; (4) Schema/Migration für die neue
  Statistik. Der Bildschirm „Statistik Verein“ fehlt im Remake noch (wartet auf `livespiel`); die Werte stehen bis
  dahin nur im Core bereit.
- **Dateigrenzen:** `Core/FinanceStadium.cs`, neue Datei `Core/StadiumStatistics*.cs`, Aufruf in `Core/GameState.Integration.cs`
  bzw. `Core/SeasonFlow.cs` (minimal, begründet), `Core/SaveGame.Migration*.cs` (neue Datei),
  `docs/finanzen-rekonstruktion.md` (eigener Abschnitt), Tests.
- **Modell:** Claude Opus 5.5.
- **Status:** offen.

### Spur `saisonende-status` – Statusbit 0x40 und Highscore-Schlüssel „nur Weltkugel“ (#125, #126)

- **Ziel:** #125 (Statusbit 0x40 „unter den ersten fünf der 1. Liga“ am 13./29. April, Löschen des Statusbytes) und
  #126 (Schlüssel 0x7530 im Original unerreichbar → im Modus „nur Weltkugel“ keine Bestenliste, Kachel gesperrt).
- **Belege:** die Issues (em.exe 0x553AF–0x553F2, 0x34769, 0x289A3–0x28A3A); Video `bundestrainer` (Kachel „Highscore“).
- **Abnahme:** (1) je Issue ein roter Test; (2) Statusbit wirkt dort, wo das Original es liest (Fundstellen suchen und
  im PR nennen), sonst nur gespeichert; (3) Modus „nur Weltkugel“: keine Bestenliste, Spielende ohne Liste – Kachel-
  und Spielende-Tests angepasst; (4) Mehrsaisonprobe Modus 1 und 4; (5) Schema/Migration nur bei neuem Zustand.
- **Dateigrenzen:** `Core/SeasonRecord.cs`, `Core/SeasonFlow.cs` (minimal), `Core/Highscore.cs`, `UI/Screens/Statistics/HighscoreScreen.cs`,
  `UI/Screens/TileLocks.cs`, `docs/highscore.md`, `docs/saison.md`, Tests.
- **Modell:** Claude Sonnet 5.5.
- **Status:** zur Prüfung, PR #160 (Claude Code, Base `f0938fa`). #125 umgesetzt (Symbol 8 im Fenster „Saisonende“, Bit 0x40 für jeden Manager unter den ersten fünf der 1. Liga, nicht im Modus 2). **#126 trifft nicht zu:** der Schlüssel 0x7530 ist im Original erreichbar (Vergleich bei 0x2899D, vom Issue übersehen); Remake war richtig, keine Codeänderung, nur Test und Doku (`docs/highscore.md`). Das Issue gehört nach Prüfung geschlossen, nicht über `Fixes`.

### Spur `pixel-wettbewerbe` – Wettbewerbsbildschirme gegen die neuen Referenzbilder

- **Ziel:** die Bildschirme, die bisher nur aus em.exe gebaut wurden, mit den neuen Bildern vermessen und Abweichungen
  beheben oder begründen.
- **Vorlagen** (`docs/referenz-videos.md`): Übersicht „DEB-Pokalspiele“ mit Ergebnissen `folge13_02545`, ohne Ergebnisse
  `duell_s2a_01217`, Achtelfinale `duell_s6a_03100`, Endspiel `eisbaeren_f60_02758`; Live-Auslosung (Frage `folge01_00217`,
  `duell_s2a_02651`; aufgedeckt `eisbaeren_f30_01719`, `eisbaeren_f52_03358`); Paarungen Liga (Oberliga `duell_s2a_01138`,
  `folge06_01256`; Zweite Liga `eisbaeren_f14_01518`, `folge17_02406`; Bundesliga `eisbaeren_f40_01833`, `folge21_02149`);
  Europapokal mit Ergebnissen und Endrunde (`folge04_01241`–`01247`, `eisbaeren_f51_03346`, `eisbaeren_f58_01323`);
  WM-Endrunde „WM-Endrundenspiele“ (`eisbaeren_f35_00646`, `eisbaeren_f44_01330`).
- **Abnahme:** (1) je Bildschirm ein Bildschirmfoto-Ablauf mit nachgestelltem Zustand (Mannschaften, Ergebnisse, Datum wie
  im Bild, soweit über den Spielstand herstellbar) und ein Pixelvergleich mit Kennzahl wie bei den Modusknöpfen in
  `wettbewerbe.md` (Mauszeiger und Verdeckung maskiert); (2) Abweichungen mit rotem Test behoben oder als `[APPROX]`
  begründet; bisherige `[APPROX]` der Bildschirme, die das Bild nun belegt, in `[ORIGINAL]` mit Bildname umwandeln;
  (3) Bildschirme bleiben zustandsrein; (4) `docs/wettbewerbe.md` aktualisiert.
- **Dateigrenzen:** `UI/Screens/Competitions/**` außer `NationalMenu.cs`, `Tools/Scenarios/` (eigene Datei), Tests, `docs/wettbewerbe.md`.
- **Modell:** Claude Opus 5.5.
- **Status:** umgesetzt, Review ausstehend (PR #162). Alle Vorlagen vermessen (Abläufe `pixel-debpokal`, `pixel-auslosung`,
  `pixel-paarungen`, `pixel-europapokal`, `pixel-wm`); Befunde PW-01 – PW-05 behoben (Rundenname erste Runde, Großschrift der
  DEB-Gruppen, Europapokal-Ergebnisse und -Termine, „WM-Endrundenspiele“). Kennzahlen: `docs/wettbewerbe.md`, Abschnitt
  „Pixelvergleich Runde 6“.

### Spur `pixel-verein` – Vereins- und Modus-4-Bildschirme gegen die neuen Referenzbilder

- **Ziel:** wie `pixel-wettbewerbe`, für Verein, Kachelreihen und Bundestrainer im Modus 4.
- **Vorlagen:** Vereinstaktik (`folge10_00300`–`00328`, `duell_s2a_02108`, `eisbaeren_f16_03225`), Vereinsaufstellung
  (`folge01_00528`–`00550`), Finanzen (Bank) zu Spielbeginn (`folge01_00508`–`00510`), Kachelreihen Statistiken/Tabellen/Gemischtes (`folge01_00526`, `folge03_00018`, `folge01_00600`, `duell_s2a_01203`,
  `duell_s2a_00135`), Spielmodus „Liga + Weltkugel“ mit offenen WM-Kacheln (`eisbaeren_f01_01430`, `eisbaeren_f01_01431`
  und die Eisbären-Reihe), Spielvorbereitung mit Kopfwahl (`speedrun_t1_00056`, `eisbaeren_f01_00204`).
- **Abnahme:** wie `pixel-wettbewerbe` (1)–(3); Modus 4: Kachelsperren und Menü des Bundestrainers aus #123 gegen die
  Eisbären-Reihe, Abweichungen als Befund mit Test; Doku in `docs/wettbewerbe.md` (Abschnitt Modus 4) bzw.
  `docs/anforderungen/bildschirme.md`.
- **Dateigrenzen:** `UI/Screens/Team/**`, `UI/Screens/Manager/**` außer `AdvertisingScreen.cs`, `UI/Screens/Statistics/**` außer
  `HighscoreScreen.cs`, `UI/Screens/Competitions/NationalMenu.cs`, `UI/Screens/SetupScreen.cs`; `MenuTiles.cs`/`MainMenuScreen.cs`
  nur minimal und begründet; `Tools/Scenarios/` (eigene Datei), Tests.
- **Modell:** Claude Opus 5.5.
- **Status:** offen.

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- Einzelprämien Topscorer, bester Torhüter, Fair-Play (#135), Statistiken Spieler, Verein, Fairplay, WM-Statistik:
  warten auf `livespiel` (Tore, Vorlagen, Strafen je Spieler).
- `hok-format` (Original-Spielstände `*.HOK`: Import, Export per Vorlage): wartet auf Original-Spielstände des Nutzers
  (`original/spielstaende/` im Referenz-Repo).
- Highscore am Spielende, Saisonende im Modus „nur Weltkugel“: in keinem Video (Kandidat für `hok-format`).
- Inaktive Branches `paket/speichern-ids`, `paket/wettbewerbe-weltkugel-2` (ohne PR): Löschen nur nach Entscheidung des Nutzers.

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
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | **wartet auf `livespiel`** (Tore, Vorlagen, Strafen je Spieler) |

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: Format, Dialog und automatisches Speichern gemergt (Runde 3, [archiv/runde-3.md](archiv/runde-3.md)); #80 in Runde 4 behoben (#105)

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`) und Export per Vorlage (`hok-format`) – wartet auf Original-Spielstände des Nutzers.

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
