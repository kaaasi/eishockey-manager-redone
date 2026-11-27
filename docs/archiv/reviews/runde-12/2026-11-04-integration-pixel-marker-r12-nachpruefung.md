# Nachprüfung: Integration pixel-marker, Runde 12 (PR #545)

Base `abdc378f96cb15d19956cb39cfdd8ecc521bfc94` · Head `84721d76db49f721df0d1a10304d24f9e3e10e9d` · Delta `f1e6dd3..84721d7`
(Korrektur #551) · Vorheriges Review: `docs/reviews/2026-11-04-integration-pixel-marker-r12.md` (#546).
Reviewer: Claude Code (geplanter Lauf, Claude Opus 5.5), eigene Sitzung, ohne Mitarbeit an #545 oder #551.

## 1. Merge-Empfehlung

**Ja für `84721d7`.** Die vier Befunde R12PM-01 bis R12PM-04 sind behoben. Alle drei Läufe von `tools/pruefen.sh` sind am
exakten Head bestanden. Neu ist nur ein P3-Befund: Die Inventur ist für drei weitere Lane-PRs dieser Runde nicht
nachgezogen, und der Hinweis in `inventur.md:269` behauptet Vollständigkeit. Das ist nur Doku, ändert keinen Code und keinen
Test und blockiert den Merge nicht. Es sollte beim Rundenabschluss behoben werden.

## 2. Befunde

**Behoben (Delta geprüft):**

- **R12PM-01** ist behoben. [`MatchDayScreen.cs:22`](https://github.com/kaaasi/eishockey-manager-redone/blob/84721d76db49f721df0d1a10304d24f9e3e10e9d/src/EishockeyManager.UI/Screens/MatchDayScreen.cs#L22)
  setzt jetzt `FramesFor(60)` = 66 Bilder, mit Begründung über 0x130EE / 3. Der Test prüft `InRange(60, 72)` und `Equal(66)`.
  - Nachgezogen sind `re-rest.md` §E (Z. 190), K3 und die Inventurzeile F077.
  - Nicht nachgezogen ist [`re-rest.md:192`](https://github.com/kaaasi/eishockey-manager-redone/blob/84721d76db49f721df0d1a10304d24f9e3e10e9d/docs/analyse/re-rest.md#L192)
    „`MatchDayScreen.BannerFrames` = 75“. Das beschreibt aber den Remake-Stand zur Zeit der Analyse und ist kein Befund.
- **R12PM-02** ist behoben, soweit es die damals genannten Zeilen betrifft. Alle 18 Zeilen tragen genau einmal „Stand Runde 12“ mit PR-Nummer
  und Integration:
  - A013, A016, A070, A089, A174, A184, A193, A305, A310, A327, A341, A369
  - F040, F106, F108, F121, F122, F135

  Der Hinweis in Z. 269 ist ergänzt. Die Zuordnung von A174 zu #495 → #508 ist richtig. Das alte Review hatte „#526“ geschrieben,
  das war falsch. Rest siehe R12PMN-01.
- **R12PM-03** ist behoben. `SetupScreen.ClubAt` trennt jetzt zwei Aussagen:
  - `[ORIGINAL]` für die Seitenzuordnung und das Blättern (0x28030/0x28E4B).
  - `[APPROX]` für die Reihenfolge innerhalb der Seite, mit Verweis auf die Videobilder.

  A347 und F058 stehen auf offen (P3), `re-rest.md` hat einen Nachtrag, und Stufe 2 in Z. 168 nennt R-K5 als teilweise erledigt.
- **R12PM-04** ist behoben. A309 lautet jetzt „erledigt (stimmte bereits, re-rest §D; … nur das Trikot geändert, R12PM-04)“.

**Neu:**

- **P3 – R12PMN-01: Drei Lane-PRs der Runde 12 nennen weitere Inventurzeilen, die nicht übertragen sind.**
  [`inventur.md:269`](https://github.com/kaaasi/eishockey-manager-redone/blob/84721d76db49f721df0d1a10304d24f9e3e10e9d/docs/analyse/inventur.md#L269)
  sagt, für `saisonwechsel` sei nur A193 betroffen, und „geld (#505) und kader-transfer (#526) nennen keine weiteren
  Zeilen“. Die Integrations-PRs selbst nennen tatsächlich keine (`#505`/`#526`: keine A/F-Kennung im Text). Die darin
  integrierten Lane-PRs nennen aber welche. Stichprobe über die Texte aller Lane-PRs der Runde (#481–#515, #524/#525/#527):
  - **#510** (`saisonwechsel` Teil 3, gemergt, integriert #519) nennt A194: „`FairPlayTable` sortiert über diese Folge
    (A194 `[APPROX]` → `[ORIGINAL]`)“, S-K6 Entscheidung A.
    - [A194](https://github.com/kaaasi/eishockey-manager-redone/blob/84721d76db49f721df0d1a10304d24f9e3e10e9d/docs/analyse/inventur.md#L473)
      steht ohne Stand auf P3.
    - Z. 173 „S-K6, A194 – nur Analyse“ ist ebenfalls unverändert.
    - Das ist die einzige echte Statusänderung unter den fehlenden Zeilen. Sie berührt auch den Hinweis zu R12PM-03
      („evtl. S-K6“).
  - **#487** (`geld` Teil 2, integriert #505) nennt A035. #487 hat die Zuschauerstärke (`SpectatorRules.Strength`,
    0x4D316) auf die Vereinsfelder umgestellt. Die Zeile A035 nennt nur den Stand von Runde 11 (Monatseinnahme).
  - **#490** (`geld` Teil 3, nur Doku) nennt die Pixelprobe G7 (A324, A326, F091, F092) und G8 (F155–F157). Das Ergebnis
    ist teilweise: linke Tabelle exakt, rechter Block +1 px offen. Die Zeilen bleiben richtig offen, ein Stand fehlt aber.
  - Gegenbeispiel: `grep '^| A194 |' docs/analyse/inventur.md` am Head enthält kein „Stand Runde 12“. Die Texte von #510,
    #487 und #490 nennen die Kennungen in der Kopfzeile „Befunde:“.
  - Korrektur: A194, A035, die G7-Zeilen und die G8-Zeilen in derselben Form nachtragen und Z. 269 berichtigen. Bei A194
    auch Z. 173 und die Kandidatenzeile S-K6 (Z. 249).

**Erwartet offen, bestätigt:**

- Die Reihenfolge der Vereinsauswahl innerhalb der Seite (S-K6/A194, Runde 13) ist offen. Am Head ist sie in Code und
  Inventur als `[APPROX]` bzw. offen geführt.
- Der Hinweis zu `NewsBar.Shown` gilt weiter. Das Delta berührt `NewsBar.cs` nicht.

## 3. Ausgeführte Prüfungen

Prüfstand:

- Klon mit `git rev-parse HEAD` = `84721d76…` und leerem `git status --porcelain`.
- SDK 10.0.402.
- Originaldateien aus `kaaasi/eishockey-manager-referenz` (`git ls-remote … HEAD` erreichbar).
- Alter Head `f1e6dd3` als eigener Worktree.

`tools/pruefen.sh` (alle drei Läufe) am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2159 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1818 | 0 | 305 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1819 | 340 | 0 |

Ergebnis: bestanden.

**Gegenbeispiele:**

- **R12PM-01:** `PixelMarkerTests.cs` vom Head in den alten Head `f1e6dd3` kopiert.
  - Dort ist `Banner_TextOnBaseline126_ShadowOneRowBelow_20Ticks` rot: `Assert.InRange() Failure … Actual: 198`.
  - Am Head ist der Test grün (Lauf mit Originaldaten). Der rote Lauf am alten Stand dient zugleich als Gegenprobe gegen
    `FramesFor(180)`.
- **R12PM-02:** Je Kennung `grep '^| <id> |' docs/analyse/inventur.md | grep -c 'Stand Runde 12'` ausgeführt. Ergebnis: 1 für
  alle 18 Zeilen.
  - `funktionskarte.py gamedata/em.exe --repo . --write` lief in einem frischen Worktree des Heads zweimal. Danach war
    `git status --porcelain` beide Male leer.
- **R12PM-03 und R12PM-04:** Code und Doku im Delta gelesen (siehe §2). Die Videomessung aus #546 ist nicht wiederholt, weil
  der Code der Reihenfolge unverändert ist und nur der Marker gewechselt hat.
- **Stichprobe Integrationen:** Texte von #505, #508, #519, #526 und #535 sowie aller zugehörigen Lane-PRs per REST nach
  A/F-Kennungen durchsucht und gegen die Inventur geprüft. Ergebnis siehe R12PMN-01.
  - A174, A184 und A013 (#508), A193 (#519) sowie A016, A341, F122 und F135 (#535) passen in Inhalt und PR-Zuordnung zu den
    Lane-PRs.
  - `Player.Strain` (A184) ist im Code vorhanden.

**Akzeptierte `[APPROX]`:** Die Reihenfolge innerhalb der Seite der Vereinsauswahl ist mit Videobeleg begründet (`SetupScreen.ClubAt`).

**Annahmen (nicht geprüft):**

- Die Division durch 3 in 0x130EE ist aus #546 übernommen und nicht erneut disassembliert.
- Für #490 ist nur der PR-Text geprüft, nicht die Pixelprobe selbst.
