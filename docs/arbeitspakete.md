# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [CLAUDE.md](../CLAUDE.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.

**Koordination:** Der Haupt-Chat verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku.

## Aktuelle Runde: Runde 2 – Stabilisierung (ab 26.10.2026)

Grundlage: unabhängiges Folgereview [reviews/2026-10-26-folgereview.md](reviews/2026-10-26-folgereview.md) zu Commit `e1ac97e`.
Die Befunde R2-02, R2-06, R2-07, die verkürzten Stärkewerte (A01) und die Levelgrenze wurden vom Koordinations-Chat
mit den Originaldaten nachvollzogen. **Erst stabilisieren, dann ausbauen:** `wettbewerbe` und das endgültige
Speicherformat starten erst nach dieser Runde.

### Spuren und Reihenfolge

```
Welle A (parallel):  fundament ─┐   wirtschaft ─┐   mehrmanager   sound (unabhängig)
                                │               ▼
Welle B:                        └──────────► lebenszyklus   (braucht die Spielerfabrik aus wirtschaft)
Danach (Runde 3):    wettbewerbe, speichern, Kalibrierung Finanzen
```

| Spur | Branch | Befunde | Inhalt | Verbindliche Abnahme (Regressionstest) |
|---|---|---|---|---|
| **fundament** | `paket/fundament` | R2-02, R2-05, A07, A08, A09 | UI-Einstiege schreiben keine Zeit fort (alle `CatchUp…`-Aufrufe aus Bildschirmen entfernen); Tagesablauf festlegen (Reihenfolge „Plan ändern → Training“); Originaldaten-Tests als echtes **Skip** (z. B. `Xunit.SkippableFact`), Pflichtlauf `EM_REQUIRE_GAMEDATA=1` scheitert ohne Daten; CI-Bericht getrennt nach synthetisch / Originaldaten / übersprungen; `ScreenDriver.PlayMatchDay()` wirft bei Zeitüberschreitung; Kacheltest unterscheidet Platzhalter und echte Bildschirme; `global.json` und feste Paketversionen | Bei pausiertem Tag alle Ansichten öffnen/schließen → Geld, Spielerwerte, Markt, Zufallszustand unverändert; danach jeder Tag genau einmal verarbeitet. CI zeigt Skips sichtbar. |
| **wirtschaft** | `paket/wirtschaft` | R2-01, R2-03, Restpunkte | Gehälter und Versicherung aus den echten Verträgen (`Contracts.MonthlyCosts`), Ersatzformel nur ausdrücklich begrenzt; **eine** Spielerfabrik für Spielstart, Transfermarkt und Jugend; **eine** Talentsuche (Jugend-„Spion“ und Transfer-Knopf rufen dieselbe Logik); Saisonprämien über `Book` mit eigener Kategorie; Heimspiele in Play-Offs/Aufstiegsrunde abrechnen (gemeinsame Spielabrechnung) | Vertrag ändern → über `Continue()` nächsten Monat erreichen → genau die neuen Gehälter/Versicherungen im Finanzbuch. Jeder Zugangspfad liefert Spieler mit Alter, eindeutiger Rückennummer, gültigen Werten und Vertrag. Jede Zahlung genau einmal im Finanzbuch. |
| **mehrmanager** | `paket/mehrmanager` | R2-06, R2-07, A01, Level | Hot-Seat: Bereitschaftsphase (Lampen), jeder aktive Manager bekommt seine Runde, Tag wird erst nach Bestätigung aller genau einmal simuliert, Entlassene ausgeschlossen; exklusive Vereinsvergabe (feste Wahlen zuerst reservieren, dann Zufall); alle drei Stärkewerte der Datenbank erhalten (Bedeutung getrennt dokumentieren); Setup-Level nach Originalbytes `5, 3, 2, 2` (DGROUP 0x11C) prüfen und Levelumfang festlegen | 2–4 Manager: jeder kommt je Tag genau einmal dran, Simulation genau einmal; Tests mit gemischten Wahlarten, mehreren Reihenfolgen und Seeds → jeder Manager exklusiver Verein; Stärketripel vollständig. |
| **lebenszyklus** | `paket/lebenszyklus` | R2-04, R2-08, R2-09 | Saisonwechsel als gemeinsame Transaktion: Altern, Vertragslaufzeit/-ende, Leihende (ursprünglichen Verein speichern), offene Angebote, Aufstellung, Markt; Fristen über Saisongrenzen (Saison + Tag); Transfergebote nur einmal wirksam (Gültigkeit, Eigentum, offener Zustand) | Saisonwechsel aktualisiert den Kader vollständig; Anfrage kurz vor dem Wechsel ist nach Fristende nicht mehr annehmbar; zweimal angenommenes Gebot hat keine zweite Wirkung, zurückgezogenes Listing wird abgelehnt. |
| **sound** | `paket/sound` | – | wie unten beschrieben; darf keine Dateien der Stabilisierungsspuren ändern | siehe unten |

**Dateigrenzen in Welle A** (damit sich die Spuren nicht überschneiden):
`fundament` → `UI/Screens/**` (nur CatchUp-Aufrufe), `UI/Testing/**`, `tests/EishockeyManager.UI.Tests/**`, `.github/**`, `global.json`, `*.csproj` (Versionen) ·
`wirtschaft` → `Core/Finance*.cs`, `Core/GameState.Manager.cs`, `Core/Player*.cs` (Fabrik), `Core/Transfer.Talent.cs`, Prämie in `SeasonFlow.cs` ·
`mehrmanager` → `Core/NewGame.cs`, `Core/GameSetup.cs`, `Formats/ClubDatabase.cs`, `UI/Screens/SetupScreen.cs`, `UI/Screens/MainMenuScreen.cs` (Zugwechsel), `Continue()`-Einstieg in `SeasonFlow.cs`.
Berührungen außerhalb der eigenen Grenzen im PR begründen. `GameState.Integration.cs` ändert nur der Koordinations-Chat oder die Spur mit Absprache im PR.

### Pflichtangaben in jedem PR dieser Runde

1. Base- und Head-SHA
2. Reproduzierter Fehler **vorher** (Befundnummer, kurzer Ablauf, beobachteter Wert)
3. Regressionstest (Name, was er prüft) – muss vor dem Fix rot gewesen sein
4. Tatsächlich ausgeführte Tests: Anzahl synthetisch / mit Originaldaten / übersprungen
5. Restlücken und neue `[APPROX]`-Stellen

### Ablauf je PR

Spur-Chat liefert PR → Koordinations-Chat prüft gegen `main` und die anderen offenen PRs (Integrationstest mit Originaldaten)
→ **unabhängiges Review (ChatGPT) mit festem Base-/Head-SHA**: zuerst die reproduzierten Gegenbeispiele erneut ausführen,
dann Aufrufer und neue Tests prüfen → Merge durch den Koordinations-Chat. Review-Ergebnisse unter `docs/reviews/`.

### Grundsätze aus dem Review

- Keine Finanz-Neukalibrierung, bevor Gehälter, Versicherungen, Prämien und Nachsaisonspiele korrekt gebucht werden.
- „Geld über `Book`“ gilt für echte Einnahmen und Ausgaben. Neutrale Umbuchungen (Bank ein/aus, Sparbuch, Aktienkauf/-verkauf
  zum Kurswert, Kredite zwischen Managern) bleiben getrennt und werden nicht als Aufwand gebucht.
- Testanzahl ist keine Abnahme. Pixelparität und Balance werden getrennt abgenommen.

### Spur `wirtschaft` – Status

Branches je PR: `paket/wirtschaft-<punkt>` (Spurbranch `paket/wirtschaft`). Reihenfolge laut Auftrag; PR 2 baut auf PR 1 auf.

| PR | Inhalt | Status |
|---|---|---|
| 1 – Spielerfabrik | `Core/Player.Factory.cs`: eine Erzeugung für Spielstart, Transfermarkt, Talentsuche (Transfer und Jugend); `PlayerFactory.Join` (Nummer, Vertrag) und `Validate` (Invarianten) für `lebenszyklus` | im Review |
| 2 – eine Talentsuche | Jugend-„Spion“ und Transfer-Knopf auf eine Logik | offen |
| 3 – Gehälter/Versicherung aus Verträgen | `Contracts.MonthlyCosts` statt Pauschalformel | offen |
| 4 – Prämien, Nachsaison-Heimspiele | Prämien über `Book`, gemeinsame Spielabrechnung | offen |

---

## Runde 1 (abgeschlossen am 26.10.2026)

Abhängigkeiten damals: `saison` → `wettbewerbe`; `manager` und `mannschaft` erweitern Club/Player/GameState in eigenen Dateien.
Ergebnis und Integration siehe Abschnitt „Integration Runde 1“ weiter unten.

---

## Paket `saison` – Saisonablauf über mehrere Jahre

Branch: `paket/saison` · Status: **Runde 1 gemergt** (PRs #6, #8, #10). Details und offene `[APPROX]`-Punkte: [saison.md](saison.md).
Für `wettbewerbe`: DEB-Pokal, Europapokal und WM über `ISeasonCompetition` in `GameState.Competitions` anmelden.

- Hauptrunde → Playoffs → Relegation (laut Video: die zwei Ersten der Oberliga und die zwei Letzten der 2. Liga spielen
  in einer Gruppe je zweimal gegeneinander, die beiden Ersten spielen in der 2. Liga) → Auf-/Abstieg → neue Saison.
- Bildschirm „Saisonende“ (Weltmeister, DEB-Pokal, Deutscher Meister, Europapokal, Auf-/Absteiger je Liga),
  Meisterfoto (kb\16 u. a.), Spielende nach gewählter Dauer, Entlassung bei zu hohen Schulden (siehe `bildschirme.md`).
- Zeitleiste ab „und tschüß…“ inkl. spielfreier Tage, „Keine besonderen Ereignisse…“.
- Eigene Dateien: `Core/Season*.cs`, `Core/Playoffs*.cs`, `UI/Screens/SeasonEnd*.cs`, `UI/Screens/MatchDayScreen.cs`;
  in `MainMenuScreen.cs` nur die Methode `Continue_`.
- Abnahme: Test spielt 3 Saisons mit Auf-/Abstieg durch; Bildschirmfotos-Ablauf `saison`; Vergleich mit Video
  (UI-Video ab ca. 14:00–15:40, Gameplay-Video an Saisonenden).

## Paket `manager` – Finanzen, Werbung, Stadion, Jugend, Posteingang

Branch: `paket/manager` · Status: **Runde 1 gemergt** (PRs #3, #5, #9, #11, #13)

- [x] PR 1: Finanzmodell im Core (Finanzbuch, Bank, Sparbuch, Aktien, Spieltagsabrechnung, Stadion-Grundwerte) –
  Abrechnung über `GameState.SettleFinances` (Aufruf in `PlayNextMatchDay`); Tageswechsel und Saisonwechsel laufen seit der
  Integration über die Zeitleiste (`GameState.Integration.cs`)
- [x] PR 2: Bildschirm Finanzen (kb\152) mit Finanzbuch – `UI/Screens/Manager/FinanceScreen.cs`, Ablauf `finanzen`;
  Bedienung (Überfahren wählt, [+]/[−] = Betrag, ◄/► = buchen) und Finanzbuch-Layout `[APPROX]`, da im Video nichts gebucht wird
- [x] PR 3: Werbung (kb\151, kb\11) – `UI/Screens/Manager/AdvertisingScreen.cs`, `Core/FinanceSponsors.cs`, Ablauf `werbung`;
  Firmen, Sender und Firmen-Infos aus em.exe; Angebotshöhen `[APPROX]` (kalibriert auf das UI-Video), Verlängerung folgt mit PR 5
- [x] PR 4: Stadion (kb\14, Ausbauten kb\8) – `UI/Screens/Manager/StadiumScreen.cs`, Ablauf `stadion`; Kosten der ersten
  Ausbaustufen aus dem Video, Lage von Kino/Imbiß/Kneipe `[APPROX]`, Meldung „Stadion-Ausbau abgeschlossen“ folgt mit PR 5
- [x] PR 5: Jugend, Neuer (Posteingang), Nachrichtenleiste – `UI/Screens/Manager/YouthScreen.cs`, `InboxScreen.cs`,
  `NewsBar.cs`, `Core/FinanceYouth.cs`, `Core/FinanceInbox.cs`, Abläufe `jugend` und `posteingang`; Jugendteams und
  leere Nachrichtenleiste am Video vermessen, Meldungstexte aus em.exe (Stadion-Ausbau, Werbepartner mit
  Verlängerungsoption, Talentsuche); Kreditanfragen unter Managern über den Posteingang. `[APPROX]`: Wirkung der
  Förderung, Ort/Preis/Rabatt der Talentsuche („Spion“: Klick ins Fenster „Jugendteams“), Posteingang-Layout
- Offen: Bedeutung der Kachel „Neuer“ (Symbol ist ein Managerkopf, im Video nicht geöffnet) – vorerst Posteingang;
  Talente bekommen Alter/Stärken, sobald das Paket „mannschaft“ Spielerwerte hat

- Finanzen (kb\152): Konto, Kredit, Sparbuch, vier Aktien mit Kursverlauf; Finanzbuch.
- Werbung (kb\151, Logos kb\11): Bande, Eisfläche, Trikot, Angebote annehmen/ablehnen.
- Stadion (kb\14): Ausbaustufen, Sitz-/Stehplätze, Eintritt.
- Jugend (Talentsuche), „Neuer“ = Posteingang, Nachrichtenleiste unten im Hauptmenü (kb\6).
- Einnahmen/Ausgaben je Spieltag (Zuschauer, Eintritt, Gehälter, Sponsoren) – Größenordnungen aus dem 75-Minuten-Video.
- Eigene Dateien: `Core/Finance*.cs`, `Core/GameState.Manager.cs`, `UI/Screens/Manager/*`.
- Abnahme: alle fünf Kacheln öffnen echte Bildschirme; Kontostand ändert sich über eine Saison plausibel.

## Paket `mannschaft` – Kader und Spieler

Branch: `paket/mannschaft` · Status: **Runde 1 gemergt** (PRs #1, #2, #4, #7, #12, #14)

- ✔ Spielerwerte, Kader aller Vereine, Aufstellung (Core), Mannschaftsstärke → `Club.MatchStrength` in `MatchEngine`
- ✔ Bildschirme Aufstellung, Taktik, Verträge (mit Vertragsverhandlung) und Transfer (Kauf, Leihe, Verkauf, Talentsuche)
- ✔ Training: Bildschirm kb\5, Kosten, Standardplan und Wirkung aus em.exe (`Squad.Training.cs`)
- Training und Transfermarkt laufen seit der Integration über die Zeitleiste (`GameState.Integration.cs`);
  Trainingskosten, Transfers und Talentsuche werden im Finanzbuch gebucht

- Spielerwerte (siehe Spielerstruktur in `reverse-engineering.md`: Talent, Alter, Technik, Kondition, Form, Rolle …),
  Kader aller Vereine aus der Datenbank, Transfermarkt.
- Aufstellung (4 Blöcke + Torhüter), Taktik (Einsatz/Angriff je Spielstand, Linienwechsel), Verträge mit Verhandlung,
  Transfer (Kauf/Leihe), Training (kb\5: 7 Tage × 2 Blöcke × 2 Einheiten).
- Eigene Dateien: `Core/Player*.cs`, `Core/Squad*.cs`, `Core/Transfer*.cs`, `UI/Screens/Team/*`.
- Abnahme: alle fünf Kacheln funktionieren; Aufstellung beeinflusst die Mannschaftsstärke (Schnittstelle zu `MatchEngine`
  mit dem Paket `saison` abstimmen).

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: **Runde 3** – startet nach der Stabilisierung (gemeinsame Spiel-/Finanzabrechnung und Stärketripel nötig); Anschluss über `ISeasonCompetition`

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `sound` – Sound, Musik, Einstellungen

Branch: `paket/sound` · Status: **Runde 2, Welle A** (unabhängig, keine Dateien der Stabilisierungsspuren ändern)

- VOC-Wiedergabe (kb\237–241), CMF-Musik (kb\248) über OPL2-Emulation, Einbindung in Desktop (MonoGame `DynamicSoundEffectInstance`).
- Einstellungen-Bildschirm (Torszenen, Lautstärken, Musik im Spiel, Vollbild).
- Eigene Dateien: `Formats/Voc*.cs`, `Formats/Cmf*.cs`, `src/EishockeyManager.Audio/*` (neues Projekt), `UI/Screens/Settings*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: **Runde 3** – endgültiges Schema erst nach Kaderlebenszyklus, Fristen und Transaktionssicherheit; muss Zufallszustand, Objektidentität und offene Vorgänge enthalten

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`).

## Integration Runde 1 (26.10.2026) – Ergebnis und offene Punkte

Die drei Pakete waren einzeln grün, zusammen schlugen 8 Tests fehl: `manager` und `mannschaft` kannten die Zeitleiste
von `saison` noch nicht. Behoben in `src/EishockeyManager.Core/GameState.Integration.cs` (Anschlusspunkte
`OnDayManager`, `OnNewSeasonManager`, `OnNewSeasonTeam`) und Tests, die Zeit über `TestSeason` (= `Continue()`) vorspulen.

**Regel ab jetzt:** Zeit vergeht nur über `GameState.Continue()`. Wer etwas je Tag/Woche/Saison braucht, hängt sich in
`GameState.Integration.cs` ein (bzw. ruft von dort die eigene Methode auf) – keine eigenen „CatchUp“-Zähler mehr.
Geld immer über `GameState.Book(manager, kategorie, betrag)` buchen, nicht `Money` direkt ändern.

Offene Punkte (stehen jetzt als Spuren in Runde 2):

| Paket | Punkt |
|---|---|
| `manager` + `mannschaft` | **Talentsuche doppelt**: „Spion“ im Jugend-Fenster (`FinanceYouth.cs`) und Knopf „Talent“ im Transfer (`Transfer.Talent.cs`). Im Original eine Funktion → zusammenführen (Vorschlag: Logik in `FinanceYouth`, Transfer-Knopf ruft sie auf). |
| `manager` | Heimspiele in Play-Offs, Aufstiegsrunde und Pokalen bringen noch keine Zuschauereinnahmen (`SettleFinances` nur für Ligaspiele). |
| `manager` | Kalibrierung: Ein passiver Manager in der Oberliga rutscht in drei Saisons auf ca. −0,5 Mio. DM (echte Daten). Mit dem 75-Minuten-Video abgleichen. |
| `saison` | Saisonprämien (`SeasonFlow.cs`) direkt auf `Money` – über `Book(…)` mit eigener Kategorie buchen (Finanzbuch). |
| `mannschaft` | Saisonwechsel für den Kader (Altern, Vertragsende, neue Talente) – Anschluss `OnNewSeasonTeam` in `GameState.Integration.cs`. |
| `mannschaft` | `CatchUpTraining()`/`TransferMarket.CatchUp()` in den Bildschirmen sind jetzt überflüssig (laufen über die Zeitleiste) – entfernen. |

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
