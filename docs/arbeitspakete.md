# Arbeitspakete

Die Portierung ist in Pakete aufgeteilt, die parallel in eigenen Chats bearbeitet werden können.
Jedes Paket hat einen Branch `paket/<name>`, eigene Dateien und klare Abnahmekriterien.
Regeln und Einrichtung: [AGENTS.md](../AGENTS.md). Den Status pflegt jedes Paket nur in seinem eigenen Abschnitt.
Abgeschlossene Runden stehen in [archiv/](archiv/) und müssen beim Einstieg nicht gelesen werden.

**Koordination:** Die Leitstelle verteilt Pakete, prüft und mergt Pull Requests und pflegt Fahrplan und Doku
(Rollen: [prozess/zusammenarbeit.md](prozess/zusammenarbeit.md)).

## Aktuelle Runde: Runde 3 – Kader, Wettbewerbe, Finanzen (ab 26.10.2026)

Basis: `main` nach Integration Runde 2 (PR #35 und Nachprüfung #37). Regeln, Pflichtangaben und der Review-Ablauf
mit ChatGPT gelten weiter (siehe Runde 2). **Neu verbindlich:** Jede Spur, die Saisonwechsel, Kader oder Geld berührt,
liefert eine **Mehrsaisonprobe** (mindestens 4 Saisonwechsel, passiver und aktiver Manager) als Test.

```
Welle A (parallel):  kader   wettbewerbe   finanzen
Welle B:             speichern (endgültiges Schema, nachdem Welle A die Daten festgelegt hat)
```

| Spur | Branch | Befunde | Inhalt | Verbindliche Abnahme (Regressionstest) |
|---|---|---|---|---|
| **kader** | `paket/kader` | R2-11 (P1), R2-12 (P2) | Vertragsende ankündigen (Posteingang/Vertragsbildschirm, Texte aus em.exe suchen) und Verlängerung ermöglichen; Kader-KI der Computervereine: Verlängerung, Neuzugänge aus dem Pool und aus Talenten, Ruhestand; ersetzt die `[APPROX]`-Überbrückung aus Welle B und begrenzt den Pool | Passiver Manager wird vor Ablauf gewarnt; nach 6 Saisonwechseln hat jeder Computerverein einen spielfähigen Kader (Mindestbesetzung je Position) mit plausibler Altersverteilung; Pool begrenzt; kein Spieler verloren oder doppelt |
| **wettbewerbe** | `paket/wettbewerbe` | – | wie im Paketabschnitt unten: DEB-Pokal, Europapokal, WM, Freundschaftsspiele, Stärken- und Statistikbildschirme; Anschluss über `ISeasonCompetition`, Heimspiele über die gemeinsame Spielabrechnung | Jeder Wettbewerb läuft über `Continue()` vollständig durch; jedes Heimspiel genau einmal abgerechnet; Bildschirme zustandsrein; Pixelvergleich mit den Referenzbildern |
| **finanzen** | `paket/finanzen` | Kalibrierung | Prämien, Zuschauer, Werbung, Stadion mit dem 75-Minuten-Video und em.exe abgleichen (Hinweis unten: passiver Oberliga-Manager deutlich im Plus) | Mehrsaisonprobe Oberliga und Bundesliga: Kontoverlauf passiver Manager innerhalb einer im PR begründeten Spanne; jede belegte Formel `[ORIGINAL]`, Rest `[APPROX]` |

**Dateigrenzen in Welle A:**
`kader` → `Core/Player.Contract.cs`, `Core/Player.Factory.cs`, `Core/Transfer*.cs`, `Core/FinanceInbox.cs`, Verträge-Bildschirm ·
`wettbewerbe` → `Core/Cups*.cs`, `Core/Statistics*.cs`, `UI/Screens/Competitions/**`, `UI/Screens/Statistics/**`, Anmeldung in `SeasonFlow.cs`/`GameState.Integration.cs` nur mit Begründung ·
`finanzen` → `Core/Finance*.cs` außer `FinanceInbox.cs`, Prämien in `SeasonFlow.cs`, `UI/Screens/Finance/**`.
Berührungen außerhalb der eigenen Grenzen im PR begründen.

## Runde 2 – Stabilisierung (abgeschlossen am 26.10.2026)

Ergebnis: Welle A gemergt mit #31 (Review [pr31](reviews/2026-10-26-pr31-review.md)), Welle B mit #35
(Review [pr35](reviews/2026-10-26-pr35-review.md), Nachprüfung [pr35-nachpruefung](reviews/2026-10-26-pr35-nachpruefung.md):
Freigabe für `9bd8fea`). Der P3-Befund PR35-N01 (UI-Test prüfte die Meldung nicht) ist nach dem Merge behoben.
Offen in Runde 3: R2-11, R2-12.

Plan, Spurstatus und Integrationsberichte von Runde 2: [archiv/runden-1-2.md](archiv/runden-1-2.md).

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

Fortschritt:
- [x] VOC-Lader (`VocFile`, alle Blocktypen 1–9 ohne Kompression), `PcmSound`, `WavWriter`, `emtool export-sound` – Branch `paket/sound-voc`
- [x] CMF-Lader (`CmfFile`), OPL2-Emulation (`Opl2`, eigener Code), Wiedergabe nach dem FM-Treiber in em.exe (`CmfPlayer`) – Branch `paket/sound-cmf`
- [x] Projekt `EishockeyManager.Audio` (Mischer, ein Effektkanal wie CT-VOICE, Musik in Schleife), Desktop über `DynamicSoundEffectInstance`, F8 = Ton aus; Effektzuordnung aus dem Disassembly belegt – Branch `paket/sound-audio`
- [x] Bildschirm Einstellungen (Kachel „Gemischtes“): Lautstärken, Musik auch im Spiel, Torszenen, Vollbild; gespeichert in `einstellungen.json` im Benutzerprofil – Branch `paket/sound-einstellungen`

Offen für später: Effekte in den Torszenen auslösen (sobald es sie gibt; UI braucht dann eine Referenz auf Audio), Einstellungen „Cheats“ und „Automatisch speichern“ (gehören zu `speichern`), Einstellungen-Eintrag im Titelmenü, Musikstart/-stopp des Originals aus dem Disassembly.

## Paket `speichern` – Laden und Sichern

Branch: `paket/speichern` · Status: **Runde 3** – endgültiges Schema erst nach Kaderlebenszyklus, Fristen und Transaktionssicherheit; muss Zufallszustand, Objektidentität und offene Vorgänge enthalten

- Eigenes Spielstandformat, Laden/Sichern-Dialog wie im Original (Dateiliste, Verzeichnis, Dateiname).
- Optional: Import von Original-Spielständen `*.HOK` (Verschlüsselung in `dateiformate.md`).

## Integration Runde 1

Ergebnis und offene Punkte: [archiv/runden-1-2.md](archiv/runden-1-2.md). Die Regeln daraus stehen in `AGENTS.md`.

## Später (nach v0.5)

- `livespiel` – Anzeigetafel, Reporter, Torszenen (optional abschaltbar, letzter Schritt vor v1.0)
- Zusatzfunktionen nach `docs/anforderungen/zusatzfunktionen.md`
