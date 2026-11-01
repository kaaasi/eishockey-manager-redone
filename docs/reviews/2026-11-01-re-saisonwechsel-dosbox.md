# Review PR #389 – re-saisonwechsel: Auswertung DOSBox-Beleg FILE12–FILE14

Geprüft: Head `7a0831f49029999be4423c19bb64be5f95e748dc`, Merge-Base `94893d3244cb6dcd643d711973731c5792f9c7ce`
(main inzwischen `2d0b081`, `git merge-tree` konfliktfrei). Reviewer: Claude Opus 5.5, geplanter Lauf, Sitzung ohne
Implementierungskontext (PR-Commit stammt aus einer anderen Sitzung). Referenz-Repo `kaaasi/eishockey-manager-referenz`
auf `fcfc6104` (enthält #7).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Reiner Doku-PR (nur `docs/analyse/re-saisonwechsel.md`, `docs/arbeitspakete.md`, keine Bilder,
keine Binär- oder Originalauszüge). Jede Aussage „bestätigt/geklärt“ der neuen Tabelle hält gegen Bild **und** Dump; der
Spielstand liefert für die Ewige Tabelle sogar einen zweiten, unabhängigen Beleg (P3-01 unten). Die Annahme „Konto nach
Fenster 500.000 DM“ ist im Disassembly gedeckt und im PR ausreichend vorsichtig formuliert. Die drei Befunde sind P3
(Formulierung), kein Merge-Hindernis.

Erwartet offen, bestätigt: **K3** (Zwangsabstieg) und **K5** (letzte Saison ohne Fenster) bleiben ohne Beleg und `[APPROX]`;
**K6** ist reine Analyse und offen.

## 2. Befunde

**P3-01 – Ewige Tabelle: Platz-Schreibweise uneinheitlich** ([re-saisonwechsel.md, Abschnitt 4 und K4](https://github.com/kaaasi/eishockey-manager-redone/blob/7a0831f49029999be4423c19bb64be5f95e748dc/docs/analyse/re-saisonwechsel.md)).
Datei 0x566E4–0x566FA rechnet `20·(Index<12) + 20·(Index<24) + 12 − Platzfeld` (Tabellensatz DGROUP 0x8288 + 0x50·Index),
das Platzfeld ist **0-basiert** (Meister Mannheim: 52 = 40 + 12 − 0). Abschnitt 4 schreibt „40 + 12 − Platz“, die neue Tabelle
„40 + 12 − 1“ für Platz 2, K4 „40 + 11“ für Platz 2. Wer Abschnitt 4 mit dem angezeigten Platz (1-basiert) liest, kommt auf 50
statt 51. Vorschlag: in Abschnitt 4 „Platzfeld (0 = Erster)“ bzw. „13 − Platz“ schreiben. Das Ergebnis selbst ist richtig und
zusätzlich belegt: In FILE14 steht Weißwasser in der Platzliste DGROUP 0x5CCB auf Bundesliga-Slot 10 (vorher Rosenheim) und
Rosenheim auf Zweitliga-Slot 1 (vorher Weißwasser) – also Weißwasser Platz 2 der 2. Liga, Rosenheim Platz 11 der Bundesliga;
die übrigen Bundesligisten in `file14_3` ergeben exakt 52 − Slot.

**P3-02 – „Lauf folgte der Anleitung bis auf den DEB-Pokal“ stimmt nicht ganz.** FILE12 hat laut Dump Regler Level **0**
und Start-Stärke **0**; die Anleitung verlangt Level 3 und Start-Stärke Abstiegskampf (= 2, vgl. FILE09). Für die Aussagen ohne
Bedeutung (Konto wird fest gesetzt, Letzter wurde über weniger als neun Spieler erzwungen), aber als Abweichung zu nennen.

**P3-03 – Konto „nicht gebucht“: Begründung aus dem Bild zu stark.** „Ein so runder Wert aus einer Buchung ist
ausgeschlossen“ belegt das Setzen, nicht das Fehlen einer Finanzbuch-Buchung. „Nicht gebucht“ stützt sich auf das
Disassembly (Datei 0x562D8/0x562DE: zwei `mov` direkt auf das Konto, kein Buchungsaufruf) – so formulieren.

Hinweise außerhalb des PR (keine Befunde):

- `docs/finanzen-rekonstruktion.md` Zeile 194 nennt `0xE0:0x20(0)` noch „Erster der Hauptrunde“, Zeile 242 „Fair-Play“. Der
  Beleg entscheidet für Fair-Play: Krefelder EV war Letzter der Hauptrunde und bekam trotzdem Zelle 13 (Datei 0x2721D
  `mov al,0xd`, nur in diesem Zweig). Zeile 194 bei Gelegenheit berichtigen.
- README im Referenz-Repo („Prämie für Platz 12“): Hinweis an die Leitstelle wie im PR; nichts im Referenz-Repo geändert.

## 3. Ausgeführte Prüfungen

Checkout: `git rev-parse HEAD` = `7a0831f49029999be4423c19bb64be5f95e748dc`, `git status --porcelain` leer.

**Gegenbeispiele (Dump):** `emtool hok dump FILE1{2,3,4}.HOK 965B far:9657 5CCB` (SHA-256 wie README), ausgewertet mit
eigenem Skript (Manager +0x14 Verein, +0xEB Liga-Stufe, +0xF3 Status, +0x18F Konto; Verein +0x16):

| | FILE13 (31.01.1994) | FILE14 (10.09.1994) |
|---|---|---|
| Verein / Liga-Stufe | 27 Krefelder EV / 2 | 24 Krefelder EV / 2 |
| Statusbyte | 0x00 | **0x03** |
| Konto | 1.950.000 Drittel = 650.000 DM | 551.363 Drittel = 183.787 DM |
| +0x16 ≠ 0 | nur Verein 27 | nur Verein 24 |
| Oberliga (Platzliste) | … Wedemark (35) … Krefelder (27) | Landsberg … Miesbach (32), Krefelder (24) |
| Index 40 | TEV Miesbach | ESC Wedemark |
| 42–47 | unverändert | unverändert |

**Bilder** (alle Namen der Auswertung existieren in `original/spielstaende/bilder/` und wurden angesehen): `file13_7`
(11. Wedemark, 12. Krefelder 0:6), `file13_13` (14.04., −491.221 DM), `file13_14` (Oberliga: Landsberg rot, Krefelder und
Miesbach grün; Wedemark fehlt), `file13_15` (400.000 DM im Kopf; Pfeil hoch, Pfeil runter, Torhüter-Symbol; „12. Platz der
Oberliga“, 100.000 DM), `file14_1` (183.787 DM, Gegner EV Landsberg), `file14_2` (gegenüber `file13_7` nur Bad Nauheim →
Landsberg, Wedemark → Miesbach), `file14_3` (Weißwasser 51, Krefelder 1 P.). Alle neun Zeilen der Tabelle „Auswertung“ stimmen.

**Disassembly** (`ndisasm -b 16` auf `em.exe`): 0x562C6–0x562F2 (Gruppe ≥ 2: Konto = 0x00124F80 = 1.200.000 Drittel, dann
Tausch); 0x5645D Aufruf `0xD0:0x20`; in 0x26F20 schreiben nur Prämienzweige das Konto, jeweils mit gleichem Betrag auch
den Prämienzähler DGROUP 0xE289 + 4·Manager, der als „Erhaltene Prämien“ angezeigt wird (0x27382). Zelle 13 nur nach
`0xE0:0x20(0)` (0x27213), +0x493E0 = 300.000 Drittel. Ewige Tabelle 0x5667A–0x567BF schreibt kein Konto. Damit Konto nach
dem Fenster 400.000 + 100.000 = 500.000 DM, sofern `0x28:0x1C5B`, `0xF0:0x43`, `0xE8:0x8E` (zwischen Setzen und Fenster) das
Konto nicht ändern – **nicht geprüft**; der PR formuliert das bereits als „rechnerisch“ und „nicht geprüft“.

**Build und Tests** (`tools/pruefen.sh`, alle drei Läufe, SDK 10.0.402):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1631 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1351 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1351 | 280 | 0 |

Commit `7a0831f49029999be4423c19bb64be5f95e748dc`, Ergebnis: bestanden.

CI des PR: `build` grün. Gegenproben durch Mutation entfallen (keine neuen Tests, reiner Doku-PR).

**Akzeptierte `[APPROX]`:** keine neuen. **Annahmen:** Das Saisonende-Fenster liest die Namen über die Auf-/Absteigerlisten
(Erklärung des PR, mit dem Bild und dem Doppeltausch stimmig, Lesecode nicht nachgeprüft).
