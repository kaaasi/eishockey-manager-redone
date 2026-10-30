# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 7 – Spielberechnung, Startwerte, DOSBox-Befunde (ab 30.10.2026)

Runde 6 (Werbung, Heimspiel-Statistik, Saisonende-Status, Pixelvergleiche): [archiv/runde-6.md](archiv/runde-6.md);
ältere Runden in [archiv/](archiv/).
**Ziel der Runde (Entscheidung Nutzer, 30.10.2026):** die echte Spielberechnung des Originals mit Ereignissen je Spieler
(Grundlage für Einzelprämien und Statistiken), die Startwerte aus den Runde-6-Funden, die DOSBox-Befunde und Werkzeuge
für Original-Spielstände. **Torszenen, Anzeigetafel und Reporter bleiben bis zum Schluss ausgeklammert** (`livespiel`).
**Vorlauf erledigt** (Sammelreview #178): #167 (`heimspiel-statistik` Teil 2 mit #165, Schema 9), #175 (DOSBox-Befunde),
#176 (`hok-format` Analyse) gemergt; welche Spur wann startet, regelt „Start gestaffelt“ unten.
**Base für alle Spuren:** aktueller `main`; tatsächlichen Base-SHA in den Draft-PR. Weiterhin verbindlich: Mehrsaisonprobe
bei Saisonwechsel, Kader oder Geld; Schema mit Migration für jeden neuen Zustand (Leitstelle nummeriert in der
Integration); geänderte Abdrücke je Fall begründen (FN-01, #139); Befund-Issues mit `Fixes #<nr>`. **Entwickler sind
Claude-Sitzungen; ChatGPT nur für Reviews.**

```
Runde 7 (parallel):  spielberechnung   kader-start   boerse-start   weltkugel-dosbox   menue-befunde   hok-patch
Integration:         kader-start → boerse-start → weltkugel-dosbox → menue-befunde, hok-patch → spielberechnung zuletzt
```

**Start gestaffelt** (Entscheidung Nutzer, 30.10.2026): `menue-befunde` und `boerse-start` sofort; `kader-start` und
`spielberechnung` nach Merge von #167; `weltkugel-dosbox` nach #175; `hok-patch` nach #176.

**Zuständigkeit für gemeinsame Dateien (R7V-177-01, #180):**
- `Core/CupsNational*.cs` (auch `PlayFriendly` in `CupsNational.GameState.cs`) gehört **allein** `weltkugel-dosbox`.
  `spielberechnung` ändert dort nichts: `MatchEngine.Play(Club, Club, Random)` bleibt mit unveränderter Signatur als
  Einstieg „nur Ergebnis“ erhalten; die neue Berechnung mit Ereignissen bekommt eine eigene Methode. Freundschafts- und
  WM-Spiele des Nationalteams stellt die Leitstelle in der Integration auf die neue Methode um (nach beiden Spuren).
- Bestehende Referenz- und Mehrsaisontests (`RngTwinProbeTests`, `GameModeTests.Mode4_ReferenceValues_Unchanged`,
  `FinanceCalibrationTests`, `DebCupTests`/`EuropeanCupTests`/`WorldCupTests` Mehrsaisonproben,
  `SaveLoadScreenTests`, `SeasonUiTests`, `DebCupDrawFlowTests`, Schemaabdruck in `SaveGameTests`): nur Sollwerte
  ändern, und nur von Spuren, die Ergebnisse, Kader, Geld oder Zufallsverbrauch verändern (`kader-start`, `boerse-start`,
  `weltkugel-dosbox`, `spielberechnung`), je Fall begründet. Konflikte löst die Leitstelle in der Integrationsfolge oben;
  `spielberechnung` ermittelt ihre Sollwerte auf dem integrierten Stand neu.
- Neue Tests jeder Spur in eigenen Dateien (`<Spurname>Tests.cs` bzw. nach Befund), keine fremden Testdateien sonst.
- `menue-befunde` ändert `MainMenuScreen.cs` höchstens minimal (Leiste); `weltkugel-dosbox` besitzt `TileLocks.cs` und
  `NationalMenu.cs`.

### Spur `spielberechnung` – Spielberechnung des Originals mit Ereignissen je Spieler (ohne Anzeige)

- **Ziel:** `MatchEngine` (`[APPROX]`, Poisson aus der Stärkedifferenz) durch die Berechnung des Originals ersetzen
  (Overlay 5E73, `docs/reverse-engineering.md`): Ergebnis **und** Ereignisse je Spieler – Tore, Vorlagen, 2-/5-Minuten-Strafen,
  Torhüterminuten und Gegentore, Verletzungen, soweit das Original sie im Spiel erzeugt. Keine Anzeigetafel, kein
  Reporter, keine Torszenen.
- **Schritt 1 (vor dem Code, im PR belegen):** Klären, ob das Original die Ereignisse unabhängig von der Anzeige
  berechnet (Einstellungen „Torszenen aus“ und „WM-Spiele“/Ligen ohne Live-Spiel, `docs/dosbox-befunde.md`; Aufrufer im
  Spieltagsablauf 0x36B5D). Ist die Berechnung an die Anzeige gekoppelt, wird der Rechenteil trotzdem herausgelöst und
  ohne Darstellung nachgebaut – gleiche Ergebnisse und Ereignisse; die Anzeige folgt in `livespiel` (Entscheidung Nutzer,
  30.10.2026).
- **Belege:** em.exe Overlay 5E73; Ranglisten 0xC8:0x20 (Torhüter, Datei 0x25E30) und 0xE0:0x20 (Fair-Play, 0x2A1E0)
  aus #135 zeigen, welche Felder je Spieler gezählt werden; Spielerfelder `Goals`, `Assists`, `Penalties2/5`
  (`Core/Player.Attributes.cs`) gibt es schon, sie werden nie hochgezählt.
- **Abnahme:** (1) Formel mit Fundstellen, `[ORIGINAL]` bzw. `[APPROX]` je Teil; (2) Tests: Ereignisse je Spiel konsistent
  mit dem Ergebnis (Summe Tore = Tore der Spieler, Vorlagen ≤ 2 je Tor, Strafen nur Feldspieler …), Tor-/Strafverteilung
  über eine Saison in den Größenordnungen der Videos (Tore je Spiel, Strafminuten); (3) Spielwertung 0:2 aus #167 und
  Penaltyschießen bleiben erhalten; (4) Mehrsaisonprobe (4 Wechsel, passiv und aktiv, Modus 1 und 4) mit begründeter
  Änderung **jedes** Referenzwerts; (5) Spielstand: Schema/Migration für neue Zähler; (6) `docs/reverse-engineering.md`
  korrigieren (der Abschnitt „Spielsimulation“ beschreibt eine Schrittsimulation, die es im Code nicht gibt).
- **Nicht in dieser Spur:** Einzelprämien (#135) und die Statistikbildschirme Spieler/Verein/Fairplay/WM – Folgespuren in
  Runde 8 auf den neuen Zählern.
- **Dateigrenzen:** `Core/Season.cs` (nur `MatchEngine` herauslösen), neue Dateien `Core/MatchEngine*.cs`,
  `Core/Player.Attributes.cs` (Zähler), Aufrufer in `GameState.cs`, `Playoffs.cs`, `CupsDeb.cs`, `CupsEurope.cs`,
  `CupsGroup.cs`, `CupsWorld.cs` nur für den Wechsel auf die neue Methode – **nicht** `CupsNational*.cs` (siehe oben),
  `Core/SaveGame.Migration*.cs` (neue Datei), `docs/reverse-engineering.md`, neues `docs/spielberechnung.md`, Tests.
- **Modell:** Claude Opus 5.5 (Review: GPT-6 Astra).
- **Status:** offen.

### Spur `kader-start` – Startwerte der Kaderspieler (#168)

- **Ziel:** #168: Kondition/Technik zu Spielbeginn wie im Original (`folge01_00528`/`_00550`: 19–23, Remake 29–34).
  Herkunft der Basis rekonstruieren (3810:4D3B Teamwerte, Datenbankwert, Zufall, Spiel-Level) und
  `SquadSetup.BaseValue` (`[APPROX]` 20 + Stärke/2) ersetzen; den Widerspruch zum UI-Video (Werte um 29–33) erklären.
- **Abnahme:** (1) roter Test mit den Werten aus `folge01` (Berliner SC, Level 5), danach grün; (2) Gegenprobe gegen das
  UI-Video bzw. die Eisbären-Reihe; (3) Mehrsaisonprobe mit begründeten Abdruckänderungen; (4) Schema nur bei neuem Zustand.
- **Dateigrenzen:** `Core/Squad.Setup.cs`, `Core/NewGame.cs` (minimal), `docs/reverse-engineering.md` (eigener Abschnitt), Tests.
- **Modell:** Claude Opus 5.5.
- **Status:** offen.

### Spur `boerse-start` – Börse zu Spielbeginn (#169)

- **Ziel:** #169: Startkurse und Startverlauf über die ganze Breite wie `folge01_00508`–`_00510` (98/96/134/126 DM), Regel
  für den anfänglichen Bildausschnitt aus em.exe statt Mitte aus höchstem und niedrigstem Kurs.
- **Abnahme:** (1) roter Test (Kurse, Verlauf, Ausschnitt), danach grün; (2) Pixelvergleich `folge01_00510` mit Kennzahl
  wie in Runde 6; (3) Bildschirm zustandsrein; (4) Mehrsaisonprobe, falls der Zufallsverbrauch sich ändert.
- **Dateigrenzen:** `Core/FinanceStocks.cs`, `UI/Screens/Manager/FinanceScreen.cs`, `Tools/Scenarios/` (eigene Datei),
  `docs/finanzen-rekonstruktion.md` (eigener Abschnitt), Tests.
- **Modell:** Claude Opus 5.5.
- **Status:** offen.

### Spur `weltkugel-dosbox` – Befunde aus den DOSBox-Aufnahmen „nur Weltkugel“ (#175)

- **Ziel:** die Folgeaufgaben aus `docs/dosbox-befunde.md` (nach Merge von #175) umsetzen: Kachel „Highscore“ vor der
  Ernennung offen (Sperrtabelle DGROUP 0x5159 vs. 0xD0BA klären); Fenster „WM-Endrundenspiele – Die Paarungen stehen
  noch nicht fest…“ mit Knopf „Gruppe“ statt `[APPROX]`; Wirkung der Freundschaftsspiele (kein Zeitfortschritt,
  Deutschland +3 Kondition/+3 Technik/+5 Form nach drei Spielen, Energie der Eingesetzten 46, Kosten 100–120 Tsd. DM);
  Taktik des Nationalteams zu Beginn (DOSBox: alle Werte 0) gegen die Startwerte aus `taktik-verein` (Runde 5): in em.exe
  klären, wann das Original die Startwerte setzt (z. B. erst bei der Ernennung oder nur im Modus 4), und nur Belegtes
  umsetzen, mit Test je Modus (Entscheidung Nutzer, 30.10.2026). Dazu den falschen Kommentar zu 0x289A3 in
  `NationalMenu.cs` berichtigen und den nie gemergten Commit `71b27de` vom Branch `paket/wettbewerbe-weltkugel-2`
  übernehmen (Ablauf `nurfahne`, Abschnitt „Kachelsperren im Modus 2 gegen das Karriere-Video“ in `docs/wettbewerbe.md`;
  den Nachtrag in `arbeitspakete.md` nicht, die Bilderliste mit den DOSBox-Bildern abgleichen).
- **Abnahme:** je Punkt roter Test mit dem DOSBox-Bild als Beleg (Bildname im Test), danach grün, oder begründet als
  Befund offen lassen; Mehrsaisonprobe Modus 2 und 4; `docs/wettbewerbe.md` nachgeführt.
- **Dateigrenzen:** `UI/Screens/TileLocks.cs`, `UI/Screens/Competitions/NationalMenu.cs`, `WorldCupKnockoutScreen.cs`,
  `Tools/Scenarios/Wettbewerbe.cs` (Ablauf `nurfahne`),
  `FriendliesScreen.cs`, `Core/CupsNational*.cs`, `docs/wettbewerbe.md`, `docs/dosbox-befunde.md` (Status), Tests.
- **Modell:** Claude Opus 5.5.
- **Status:** Teil 1 in #189 (Review ausstehend): „Highscore“ im Modus 1 offen, Endrunden-Fenster, 71b27de übernommen,
  Regressionstests Freundschaftsspiele, Energie 70 beim ersten Freundschaftsspiel (`[APPROX]` 0x3BB51). Ausgelagert:
  Taktik-Startwerte #192, Länderwerte aus der Aufstellung #193, Paarungsansicht WM #194, Modus-1-Halt #195; Abzug je
  Spiel bei `spielberechnung`.

### Spur `menue-befunde` – Nachrichtenleiste (#170) und Handbuchfrage des Originals

- **Ziel:** (1) #170: Nachrichtenleiste im Hauptmenü nur mit Nachricht (Bedingung des Originals aus em.exe; Widerspruch
  zu `oberflaeche_1510` klären). (2) **Kopierschutz** (Entscheidung Nutzer, 30.10.2026): Das Original stellt sporadisch
  eine Frage zum Handbuch; das Remake übernimmt sie **nicht**. Fundstelle in em.exe (Auslöser, Text, Ablauf) belegen,
  in `docs/portierung.md` als bewusste Abweichung festhalten, damit keine Spur sie mitportiert, und einen Test, dass
  Spielstart und die ersten Spieltage ohne Abfrage durchlaufen.
- **Abnahme:** (1) roter Test für die Leiste, Pixelvergleich `eisbaeren_f24_00040` und `folge01_00708`; (2) Fundstelle und
  Doku der Handbuchfrage, Test grün; Bildschirme zustandsrein.
- **Dateigrenzen:** `UI/Screens/Manager/NewsBar.cs`, `MainMenuScreen.cs` (minimal, begründet), `docs/portierung.md`,
  `docs/reverse-engineering.md` (Abschnitt Kopierschutz), Tests.
- **Modell:** Claude Sonnet 5.5.
- **Status:** offen.

### Spur `hok-patch` – Spielstände für das Original erzeugen (Nutzeranforderung)

- **Ziel:** nach Merge von #176 `emtool hok patch <vorlage> <ziel> feld=wert …`: Vorlage lesen, nur belegte Felder ändern
  (Konto, Eintritt, Stadion, Datum/Zähler, Regler), Prüfsummen neu, neue Spielstand-ID, Kopf und Füllbyte übernehmen –
  damit der Nutzer im Original gezielt Situationen herstellen kann (Analyse in DOSBox). Import/Export ins Remake folgt
  später.
- **Abnahme:** (1) Tests mit synthetischen Bytes (Prüfsummen, ID, nur genannte Felder geändert, unbekannte Felder
  abgewiesen); (2) mit Originaldaten: gepatchter FILE01 entschlüsselt mit gültigen Prüfsummen; (3) Abnahme durch den
  Nutzer: Stand lädt im Original und zeigt die Werte (Bild).
- **Dateigrenzen:** `src/EishockeyManager.Formats/OriginalSaveGame*.cs`, `src/EishockeyManager.Tools/Hok*.cs`,
  `docs/formate/spielstand-original.md`, Tests. Keine Spielstände ins Code-Repo.
- **Modell:** Claude Sonnet 5.5.
- **Status:** offen (#176 gemergt).

### Zurückgestellt (Entscheidung Nutzer bzw. fehlende Grundlage)

- Einzelprämien Topscorer, bester Torhüter, Fair-Play (#135), Statistiken Spieler, Verein, Fairplay, WM-Statistik:
  warten auf `spielberechnung` (Tore, Vorlagen, Strafen je Spieler; Korrektur Nutzer 30.10.2026: nicht auf die Torszenen).
- #166 Oberliga-Austausch (400.000 DM): erst, wenn der Austausch der Oberliga samt Managervereinen belegt ist.
- Import/Export von Original-Spielständen ins Remake: nach `hok-format` (Analyse, #176) und `hok-patch`.
- Highscore am Spielende, Saisonende im Modus „nur Weltkugel“: in keinem Video – Kandidat für eine DOSBox-Aufnahme
  mit einem per `hok-patch` vorbereiteten Spielstand.
- Inaktive Branches (Entscheidung Nutzer, 30.10.2026: prüfen, dann löschen): `paket/speichern-ids` ist vollständig in
  `main` → löschen; `paket/wettbewerbe-weltkugel-2` hat einen ungemergten Commit `71b27de` → erst nach Übernahme durch
  `weltkugel-dosbox` löschen. Löschen nur in der Weboberfläche (aus den Sitzungen gesperrt).

## Paket `hok-format` – Original-Spielstände `*.HOK`

Branch: `paket/hok-format` · Status: Analyse gemergt (#176, Review #178): Verschlüsselung, Prüfsummen, Kopf und Blockfolge
belegt, `emtool hok dump`, Doku `docs/formate/spielstand-original.md` (offene Liste dort). Entscheidung Nutzer:
Sicher-/Ladesperre des Originals nicht übernehmen. Weiter in Runde 7 mit `hok-patch` (Spielstände fürs Original);
Import/Export ins Remake danach.

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
| – | Statistiken Spieler, Verein, Fairplay, WM-Statistik | **wartet auf `spielberechnung`** (Tore, Vorlagen, Strafen je Spieler) |

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
