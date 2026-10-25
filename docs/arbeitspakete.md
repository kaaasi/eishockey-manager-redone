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

Branch: `paket/saison` · Status: **umgesetzt, in Prüfung** – PRs #6 (Kalender), #8 (Ablauf), UI-PR (gestapelt:
`paket/saison` → `paket/saison-ablauf` → `paket/saison-ui`). Details und offene `[APPROX]`-Punkte: [saison.md](saison.md).
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

Branch: `paket/manager` · Status: offen

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
