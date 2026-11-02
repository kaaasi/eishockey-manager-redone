# Nachprüfung Review PR #411 – Plan Runde 11

- **PR:** #411 · **Base:** `e06ee33d5e2bde0065404caad870f41cda7911a3` · **Head:** `e76a5c137fd64850c8c45bbb7eefcf5884884356`
- **Vorher geprüft:** `16e884f4e80c695872ed29bc0c2c7fdae3f40949` (Review `docs/reviews/2026-11-02-plan-runde-11.md`, PR #413)
- **Delta:** `git range-diff` → `16e884f = c26651e` (Plan-Commit unverändert auf neuen `main` umgesetzt); inhaltlich nur
  Commit `e76a5c1` (`docs/arbeitspakete.md`, +17/−11).
- **Reviewer:** Claude Opus 5.5, geplanter Lauf (frische Sitzung, nicht an der Umsetzung beteiligt)
- **Checkout:** Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer; Originaldaten aus
  `kaaasi/eishockey-manager-referenz` (`git ls-remote … HEAD` → `94bb25e`).

Links: `https://github.com/kaaasi/eishockey-manager-redone/blob/e76a5c137fd64850c8c45bbb7eefcf5884884356/<pfad>#L<n>`
(kurz `AP` = `docs/arbeitspakete.md` am Head).

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** R11P-01 bis R11P-05 sind behoben, die Änderung erzeugt keine neue Überschneidung der
Dateigrenzen, #357 steht genau einmal (bei `kader-transfer`). Einziger neuer Befund R11N-01 (P3): die eine Minute vor dem
Commit angelegte Issue #414 fehlt im Plan; sie benennt ihre Spur selbst (`marker-werkzeug` Teil 1) und kann beim Merge
oder mit dem Start der Spur nachgetragen werden.

## 2. Befunde

### Frühere Befunde

| ID | Stand | Beleg am Head |
|---|---|---|
| R11P-01 (P2) | **behoben** | [AP L49–51](https://github.com/kaaasi/eishockey-manager-redone/blob/e76a5c137fd64850c8c45bbb7eefcf5884884356/docs/arbeitspakete.md#L49): `Core/Season.cs` allein `saisonwechsel`, Ausnahme `ui-meldungen` nur Feld `Months`. `grep -rln -i startorder src` → nur `Core/Season.cs` (`StartOrder` L201/L244, `saisonwechsel` Teil 4 [L109](https://github.com/kaaasi/eishockey-manager-redone/blob/e76a5c137fd64850c8c45bbb7eefcf5884884356/docs/arbeitspakete.md#L109)); `Months` ist ein einzelnes `private static readonly`-Feld ([Season.cs L90](https://github.com/kaaasi/eishockey-manager-redone/blob/e76a5c137fd64850c8c45bbb7eefcf5884884356/src/EishockeyManager.Core/Season.cs#L90)), Monatsnamen (`ui-meldungen` Teil 3, A157) brauchen nur dieses. Die alte Zuweisung „`Core/Season.cs` (Monatstabelle)“ an `ui-meldungen` ist entfernt. |
| R11P-02 (P2) | **behoben** | [AP L56–60](https://github.com/kaaasi/eishockey-manager-redone/blob/e76a5c137fd64850c8c45bbb7eefcf5884884356/docs/arbeitspakete.md#L56): `DrawRankCurve` (einzige Fundstelle `MainMenuScreen.cs` L284/L285/L318) wird in eine eigene Datei ausgelagert; in `MainMenuScreen.cs` nur Aufruf und versteckter Klick (Infofile), „sonst nichts“. Verträgt sich mit L62 („nur minimal und begründet“); keine andere Spur nennt `MainMenuScreen.cs`. |
| R11P-03 (P3) | **behoben** | `kader-transfer` Teil 2 [L120–122](https://github.com/kaaasi/eishockey-manager-redone/blob/e76a5c137fd64850c8c45bbb7eefcf5884884356/docs/arbeitspakete.md#L120): Anzeige in `UI/Screens/Statistics/PlayerStatisticsScreen.cs` (Datei vorhanden, gehört `wettbewerbe-spiel`) als Anschluss für die Leitstelle. |
| R11P-04 (P3) | **behoben** | `geld` Teil 6 [L91–94](https://github.com/kaaasi/eishockey-manager-redone/blob/e76a5c137fd64850c8c45bbb7eefcf5884884356/docs/arbeitspakete.md#L91): Knopf in `UI/Screens/Team/TransferScreen.cs` (vorhanden, gehört `kader-transfer`) als Anschluss, Bildschirmfoto-Abnahme nach der Integration. |
| R11P-05 (P3) | **behoben** | #357 nur noch bei `kader-transfer` Teil 6 [L130–131](https://github.com/kaaasi/eishockey-manager-redone/blob/e76a5c137fd64850c8c45bbb7eefcf5884884356/docs/arbeitspakete.md#L130) (Test `renamedClashes` in `TransferVertraegeKomplettTeil5Tests.cs` L343 ✓), nicht mehr bei `saisonwechsel` Teil 4. Teil 5 [L128–129](https://github.com/kaaasi/eishockey-manager-redone/blob/e76a5c137fd64850c8c45bbb7eefcf5884884356/docs/arbeitspakete.md#L128) nennt F002–F004, F013–F015; Inventur: F002 054D9h, F003 05521h, F004 057E3h, F013 07C7Eh, F014 07EF1h, F015 07F6Bh (wirkungslos) ✓. |

**Erwartet offen (bestätigt):** #349 offen, unter „Zurückgestellt“ ✓.
**Zusatz:** Der Satz zu #398 unter „Nicht in Runde 11“ ist entfernt; #398 ist geschlossen (2026-11-02T07:44:40Z) ✓.

### R11N-01 (P3) – offene Issue #414 keiner Spur zugeordnet

Gegenbeispiel 1b des Erstreviews erneut: `gh api 'repos/kaaasi/eishockey-manager-redone/issues?state=open' --paginate`
ohne PRs → 19 offen: #166, #327, #329, #342, #344, #347, #348, #349, #352, #353, #354, #357, #358, #362, #363, #364,
#401, #406, **#414**. Alle außer #414 stehen genau einmal im Abschnitt „Aktuelle Runde“
(`grep -o '#[0-9]\{3\}' | sort | uniq -c`: jede 1×). #414 („R-RW: P3-Befunde Review RE-Werkzeuge“, angelegt 07:52:34Z,
Commit 07:53:15Z) nennt selbst „Runde 11: Spur `marker-werkzeug`, Teil 1“; dort
([AP L170–171](https://github.com/kaaasi/eishockey-manager-redone/blob/e76a5c137fd64850c8c45bbb7eefcf5884884356/docs/arbeitspakete.md#L170))
stehen nur #401 und #406. Dateigrenze passt (`tools/reverse-engineering/*` allein `marker-werkzeug`).
**Vorschlag:** in Teil 1 „; Befunde R-1 bis R-4, R-6 zu `disasm.py`/README (#414)“ ergänzen – sonst sieht die Spur
die Issue nicht, weil `spur-start.md` vom Plan ausgeht.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `git range-diff 16e884f~1..16e884f c26651e~1..c26651e` | `=` (unverändert) |
| `git diff --stat 16e884f e76a5c1 -- src tests tools .github` | nur `main`-Stand (#409/#410: `disasm.py`, README), nicht Teil des PR |
| R11P-01: `grep -rln -i startorder src`; `grep -n 'Months\|StartOrder' Core/Season.cs`; `ls Core \| grep -i season` | nur `Season.cs`; `Season*.cs` alle bei `saisonwechsel`, `Transfer.SeasonEnd.cs` über `Transfer*.cs` bei `kader-transfer` – keine Doppelzuweisung |
| R11P-02: `grep -rln DrawRankCurve src` | nur `MainMenuScreen.cs` |
| R11P-03/04: Dateien `PlayerStatisticsScreen.cs`, `TransferScreen.cs` | vorhanden, Eigentümer laut Zuständigkeit `wettbewerbe-spiel` bzw. `kader-transfer` → Anschluss korrekt |
| R11P-05: `#357` im Rundenabschnitt; Inventur F002–F004/F013–F015 | 1× (`kader-transfer`); Adressen ✓ |
| Dateigrenzen nach der Änderung: Zuständigkeit L45–64 gegen die Teile aller Spuren | keine neue Überschneidung; einzige Dateien mit zwei Spuren sind die benannten Ausnahmen (`Player.Factory.cs`, `Player.Attributes.cs`, `Season.cs` `Months`, `MainMenuScreen.cs`) |
| Funktionskarte: `funktionskarte.py em.exe --csv` am Head und an der Base | 679 Funktionen, **0** mit `arbeitspakete` als Fundstelle; CSV Head = Base (`cmp`) ✓ |
| Gegenprobe: temporär `(3D7F8h/3D316h)` → `(Datei 0x3D7F8, Datei 0x3D316)` | 2 Treffer → Prüfung wirkt; Datei wiederhergestellt (`git status --porcelain` leer) |
| Offene Issues (siehe R11N-01) | 18 von 19 zugeordnet |
| `tools/pruefen.sh` (alle drei Läufe), SDK 10.0.402 | Tabelle unten |

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1631 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1351 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1351 | 280 | 0 |

Commit `e76a5c137fd64850c8c45bbb7eefcf5884884356`, Ergebnis: bestanden (Doku-PR, kein Code berührt).

**Akzeptierte `[APPROX]`:** keine (Plan ohne Code). **Annahmen:** „nur das Feld `Months`“ deckt Monatsnamen (A157) ab;
falls der Videoabgleich auch Wochentage (`Weekdays`) ändern lässt, braucht es eine erweiterte Ausnahme.
