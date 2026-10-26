# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [CLAUDE.md](../CLAUDE.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.

**Koordination:** Der Haupt-Chat verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku.

## Abhängigkeiten

```
saison ──────────────► wettbewerbe (Playoffs/Pokal brauchen den Saisonablauf)
manager ─┐
mannschaft ─┴─► (beide erweitern Club/Player/GameState in eigenen Dateien)
sound, speichern: unabhängig
```

Empfohlener Start: **saison**, **manager**, **mannschaft** parallel. **wettbewerbe**, **sound**, **speichern** danach.

---

## Paket `saison` – Saisonablauf über mehrere Jahre

Branch: `paket/saison` · Status: offen

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

Branch: `paket/manager` · Status: fertig (PRs 1–5 offen)

- [x] PR 1: Finanzmodell im Core (Finanzbuch, Bank, Sparbuch, Aktien, Spieltagsabrechnung, Stadion-Grundwerte) –
  Abrechnung über `GameState.SettleFinances` (Aufruf in `PlayNextMatchDay`), Saisonwechsel über `StartFinanceSeason()`
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

Branch: `paket/mannschaft` · Status: offen

- Spielerwerte (siehe Spielerstruktur in `reverse-engineering.md`: Talent, Alter, Technik, Kondition, Form, Rolle …),
  Kader aller Vereine aus der Datenbank, Transfermarkt.
- Aufstellung (4 Blöcke + Torhüter), Taktik (Einsatz/Angriff je Spielstand, Linienwechsel), Verträge mit Verhandlung,
  Transfer (Kauf/Leihe), Training (kb\5: 7 Tage × 2 Blöcke × 2 Einheiten).
- Eigene Dateien: `Core/Player*.cs`, `Core/Squad*.cs`, `Core/Transfer*.cs`, `UI/Screens/Team/*`.
- Abnahme: alle fünf Kacheln funktionieren; Aufstellung beeinflusst die Mannschaftsstärke (Schnittstelle zu `MatchEngine`
  mit dem Paket `saison` abstimmen).

## Paket `wettbewerbe` – Paarungen, Stärken, Statistiken, Pokale

Branch: `paket/wettbewerbe` · Status: wartet auf `saison`

- Paarungen (Liga, Playoffs, DEB-Pokal, Europapokal, WM), Freundschaftsspiele.
- Stärken (Ligen, Länderteams: Gesamt/Abwehr/Sturm, Smileys), Statistiken (Totale, Früher, Verein, Spieler, Fairplay, WM).
- DEB-Pokal (32 Teams), Europapokal (Gruppe + Endrunde), WM (2 Gruppen + K.-o.).
- Eigene Dateien: `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/*`, `UI/Screens/Statistics/*`.

## Paket `sound` – Sound, Musik, Einstellungen

Branch: `paket/sound` · Status: offen (unabhängig)

- VOC-Wiedergabe (kb\237–241), CMF-Musik (kb\248) über OPL2-Emulation, Einbindung in Desktop (MonoGame `DynamicSoundEffectInstance`).
- Einstellungen-Bildschirm (Torszenen, Lautstärken, Musik im Spiel, Vollbild).
- Eigene Dateien: `Formats/Voc*.cs`, `Formats/Cmf*.cs`, `src/EishockeyManager.Audio/*` (neues Projekt), `UI/Screens/Settings*`.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: wartet auf `manager` und `mannschaft` (Datenmodell)

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`).

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
