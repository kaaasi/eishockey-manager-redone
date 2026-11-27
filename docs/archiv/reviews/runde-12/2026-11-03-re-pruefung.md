# Unabhängiges Review PR #480 (Spur `re-pruefung`)

Base `af907c5af899c58424de2d843b60952a8c76b460` · Head `1bd70a8b24041bd8de3c949b83bfb528a59c12e0` · Reviewer: Claude Opus 5.5,
geplanter Lauf, neue Sitzung ohne Implementierungsbeteiligung. Geändert: `docs/analyse/re-runde-12.md` (neu),
`docs/arbeitspakete.md` (eigener Statuspunkt). Nur Doku.

## 1. Merge-Empfehlung

**Nein für `1bd70a8`**, nach Korrektur von R12RP-01 bis R12RP-03 ja (reine Textänderungen in `re-runde-12.md`, Nachprüfung
nur des Deltas). Die Kernaussagen zu E2, E4 und E5/S-K1 sind am Disassembly nachvollzogen und stimmen (Abschnitt 3). Blockierend
ist R12RP-01: eine falsch notierte Adresse erzeugt in der Funktionskarte eine falsche Fundstelle (Abnahmekriterium der Spur).

Erwartet offen und bestätigt: S-K6 umsetzen ja/nein (Zeile „S-K6“ der Umsetzung, „Entscheidung Leitstelle“) und die Ausnahme
in `AGENTS.md` §4 für E4 (Zeile „E4“). Beide sind als Leitstellen-Entscheidung gekennzeichnet; kein Befund.

## 2. Befunde

**R12RP-01 (P2) – falsche Fundstelle in der Funktionskarte.**
[`re-runde-12.md:29`](https://github.com/kaaasi/eishockey-manager-redone/blob/1bd70a8b24041bd8de3c949b83bfb528a59c12e0/docs/analyse/re-runde-12.md#L29)
schreibt „DGROUP 0x1255 → Datei 0x40AE, 0x1251 → 0x409E“. 0x40AE/0x409E sind DGROUP-Versätze der Texte („Spieler ist“,
„noch gesperrt !“; Datei 0x1AE7E/0x1AE6E), keine Dateioffsets. Gegenbeispiel: `funktionskarte.py … --csv <scratch>` am Head →
Funktion **0x03FB7** (Basis 0x0, 337 B) steht auf „dokumentiert“ mit der **einzigen** Fundstelle `docs/analyse/re-runde-12.md:29`;
am Base ist sie unbekannt. Sie ist die einzige Funktion, die nur durch diese Datei belegt ist (die übrigen 35 Treffer haben
weitere Fundstellen). Behebung: „DGROUP 0x40AE“/„DGROUP 0x409E“ schreiben (oder ohne Präfix „Datei“), dann Selbstprüfung
wiederholen (erwartet: 0x03FB7 unbekannt, Gesamtzahl „dokumentiert“ 171).

**R12RP-02 (P2) – E4: Liste „Alle Schreibzugriffe“ unvollständig, Zufallsaufruf fehlt.**
[`re-runde-12.md:60–73`](https://github.com/kaaasi/eishockey-manager-redone/blob/1bd70a8b24041bd8de3c949b83bfb528a59c12e0/docs/analyse/re-runde-12.md#L60-L73)
nennt sich vollständig, es fehlen aber (`disasm.py 0x44E22 0x45AA7`, alle `mov/inc … [DGROUP]`):
- **Datei 0x44E3F/0x44E46: DGROUP 0xE1A4 = random(0x55, 0x73)** (0x0C037) am Funktionsanfang, also je Managerzug – Spielzustand
  und Zufall beim Anzeigen; Leser ist die Vertragsverhandlung 0x4A832 (im Remake `ContractTurnFactor`,
  `Transfer.Contract.cs:34`, dort schon als abgeleiteter Wert umgesetzt).
- Datei 0x44EB1: DGROUP 0xA70D = Manager am Zug; Datei 0x44EB4: 0xE1A6 = 0xFF (vor 0x44A48); Datei 0x450C3 und 0x45957:
  0xA70E = 0xA70D (Rücksetzen vor 0x426BC bzw. nach der Rückkehr).
Für die vorgeschlagene §4-Ausnahme ist mindestens 0xE1A4 zu nennen (oder ausdrücklich als im Remake abgeleitet auszunehmen).

**R12RP-03 (P2) – E2: Umsetzung weist eine fremde Datei zu.**
[`re-runde-12.md:184`](https://github.com/kaaasi/eishockey-manager-redone/blob/1bd70a8b24041bd8de3c949b83bfb528a59c12e0/docs/analyse/re-runde-12.md#L184)
nennt für `kader-transfer` Teil 4 „ggf. `Core/Squad.Lineup.cs`“. Diese Datei gehört nach der Zuständigkeit Runde 11
(`archiv/runde-11.md`, von Runde 12 übernommen) allein `wettbewerbe-spiel`; `kader-transfer` hat sie nicht in den Dateigrenzen.
Behebung: „Anschluss melden“ wie in den Zeilen E4/S-K6, oder Zuständigkeit durch die Leitstelle.

**R12RP-04 (P3) – E2: Umfang über `kader-transfer` Teil 4 hinaus.** Teil 4 lautet „Text angleichen“; die Umsetzung verlangt
zusätzlich das Sperren des Feldklicks (0x40B37), die vorgeschaltete Automatik-Meldung und „Keine Reihe / ausgewählt !“ (letzteres
nur „prüfen“). Fachlich belegt (Abschnitt 3), aber die Leitstelle sollte den Teil ausdrücklich erweitern.

**R12RP-05 (P3) – E4: Ungenauigkeiten in der Tabelle.**
- Zeile 0x45996/0x4599C „nach Rückkehr“: Die Rückkehr springt schon bei Datei 0x45961/0x4596C nach 0x44FBD; 0x4596F–0x4599C
  läuft nur beim Verlassen des Menüs ([bp−7] ≠ 0, Zugende).
- Zeile 0x4527C: „Manager < 0x386“ ist ein Vergleich mit dem **Wert** DGROUP [0x386] (`cmp al,[0x386]`, Datei 0x45273).
- Umsetzung E4 nennt `GameState.Notify` als Setzer; der Ablageweg im Remake ist `FinanceInbox.Post` (Notify ist nur die
  Info-Variante). Die Neuaufstellung bei jeder Rückkehr berührt `GameState.HotSeat.cs` (`BeginManagerTurn`), die in der Spalte
  „Dateien“ fehlt.

**R12RP-06 (P3) – Restpunkte „prüfen“.** E2-Tabelle (Automatik, Keine Reihe) und UM-4 0x414C6/0x5C559 enden mit „– prüfen“
statt einem Befund zum Remake. Zulässig als Testidee, sollte in der Umsetzung aber dem ausführenden Teil zugewiesen sein
(ist es: `kader-transfer` Teil 4, `ui-meldungen` Teil 3).

## 3. Ausgeführte Prüfungen

Checkout: `git fetch origin pull/480/head`, `git rev-parse HEAD` = `1bd70a8b…`, `git status --porcelain` leer; Merge-Base = Base.
Originaldateien aus `kaaasi/eishockey-manager-referenz` (`git ls-remote … HEAD` erreichbar, `fetch-reference.sh original`).

**Disassembly (je Punkt am Original nachvollzogen):**
- **E2** (`disasm.py 0x40364 0x41540`): Datei 0x40B37–0x40B55 `cmp [es:bx+0xd],0` / `test [es:bx+0x9],3`, beide ≠ 0 →
  0x0C0B8(„Spieler ist“, „noch gesperrt !“) über die Zeiger DGROUP 0x1255/0x1251 (Inhalte geprüft), dann `jmp 0x40D7B`. Kaderplatz
  `[0xA70E]·0x618 + Zeile·0x34 + 0x5DC3` bestätigt; Automatik 0x375 + 2·Manager vorher gelöscht mit „Auto-Aufstellung / ist
  deaktiviert !“ (0x1249/0x124D). Listenweg Datei 0x40E01–0x40E27: s ∈ {1, 2}, Zeile 2 = [0xB84 + 4·(2 − s)] → „Verletzt.“/„Gesperrt.“,
  ohne +0x0D; Reihenfolge 22 Mann (0x4BE17 Art 3 = 0x16) → nur Linksklick ([0x5A16] = 1), +0x15 = 0: „Keine Reihe / ausgewählt !“
  ([bp−9] = 0xFF) → „Der darf da / nicht rein !“ (Torhüter außerhalb Reihe 4 bzw. Feldspieler in Reihe 4); Rückgabe 1 in [bp−1]
  bricht bei 0x40F02 ab. Alles wie beschrieben.
- **E4** (`disasm.py 0x44E22 0x45AA7`): einziger Aufrufer 0x54C9F; Einstiege 0x44E6D (Sprünge 0x453FB, 0x4576D, 0x4590F) und
  0x44FBD (0x45961, 0x4596C); 0x450C6 ruft 0x426BC unbedingt bei jedem Durchlauf ab 0x44FBD, 0x426BC kehrt ohne Automatik sofort
  zurück ([0x375 + 2·Manager] − 1 = 0xFF → 0x42832). `--xref 0x5A50 0x5A54`: genau 0x4527C (Leser), 0x45289 (= 0), 0x4B5E6 (= 1).
  Autosave-Zähler 0x5148, 0x1DB nur bei Spieltag 0x2C und 0x1DB = 2 bestätigt. Abweichungen siehe R12RP-02/05.
- **E5/S-K1** (`disasm.py 0x54C9F 0x56800`): Datei 0x56296 Feld +0x16, 0x5629D–0x562B3 Bit 0x04 (Basis `bx−0x6A61` = 0x959F),
  0x562C1 `or 2`, 0x562C6 `cmp [bp−1],2; jc`, 0x562D8/0x562DE `mov word` 0x0012/0x4F80 = 0x124F80 auf 0x963D/0x963B, danach
  0x36169; keine Kontoabfrage im Pfad. `--xref 0x963B 0x963D`: nur diese zwei Schreiber. Remake `SeasonFlow.cs:323–326` setzt
  `OberligaKeptMoney` ohne Kontoprüfung – Aussage stimmt.
- **S-K6** (Stichprobe): Aufruf 0x35EDD bei Datei 0x5644E nach 0x09D1B, vor 0x346D5/0x26F20; Schleifengrenzen `cmp …,0x14`
  (Länder, 0x35FC9) und `cmp …,0x37` (55, 0x3612D), Platzliste 0x5CCB gelesen und geschrieben. FILE13/FILE14-Vergleich nicht
  wiederholt.
- **UM-4/UM-2:** nur 0x40EF7 (Bedingung [bp−0xE] aus 0x404E8–0x404FD) nachgeprüft; übrige Zeilen gelesen, nicht nachgerechnet.

**Umsetzung ↔ Spuren:** Jede Zeile genau einer Spur/Teil zugeordnet und mit `docs/arbeitspakete.md` stimmig (E2 → `kader-transfer`
4, E4 → `ui-meldungen` 4, E5 und S-K6 → `saisonwechsel` 3, UM-4 → `ui-meldungen` 3, UM-2 → `ui-meldungen` 2); Ziel, Dateien,
Testidee je Zeile vorhanden. Dateigrenzen siehe R12RP-03/05.

**Selbstprüfung** `python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --repo . --csv <scratch>`
ohne `--write`: 679 Funktionen, 4811 Zuordnungen, Spielcode umgesetzt 238 / genähert 4 / dokumentiert 121 / unbekannt 1,
`re-runde-12.md` Fundstelle in 36 Funktionen – wie angegeben; `git status` danach leer. Abweichung im Nicht-Spielcode: R12RP-01.

**`tools/pruefen.sh`** (alle drei Läufe, SDK 10.0.402, Commit `1bd70a8`), Ergebnis „bestanden“:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1982 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1670 | 0 | 280 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1671 | 311 | 0 |

Keine Gegenproben: PR ohne Code und Tests.

**Akzeptierte `[APPROX]`:** keine neuen. **Annahmen** (nicht geprüft): Umrechnung 1.200.000 → 400.000 DM (Drittel-DM nach
`re-saisonwechsel.md`); Deutung der Bilder in UM-2 (vom Bericht selbst als ungeprüft markiert).
