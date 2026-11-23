# Nachprüfung Review PR #965 (re-reste) – RR-01

Reviewer: Claude Code (Opus 5.5), eigene Sitzung ohne Implementierungskontext, geplanter Lauf. Nachprüfung von
`docs/reviews/2026-11-23-re-reste.md` (bisher geprüfter Head `d6d5560`), nur Delta `d6d5560..54e7f5b` und RR-01 (Label
`beleg-strittig`). Geprüft: Head `54e7f5bda6cb841d807b72cd395decbe224a67bd`, Base `a9e7d1b` (= `main`), Checkout
`git rev-parse HEAD` = Head, `git status --porcelain` leer, SDK 10.0.402, Originaldaten aus `kaaasi/eishockey-manager-referenz`.
Rebase: `git range-diff` der drei alten Commits gegen `a9e7d1b..54e7f5b~1` patch-gleich (`=`); eigene Änderung nur `54e7f5b`.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Die strittige Frage RR-01 entscheidet die Lesung zugunsten des Fixlaufs: Im schnellen Weg des
DEB-Pokals bekommen **nur die ersten zwei gerechneten Paarungen eines Termins** (mit noch offener Paarung dahinter) den Zusatzwurf
bei ungleichem Ergebnis – nicht „jede Paarung, hinter der eine offene aussteht“. Die Aussage des Erstreviews („bei jedem Aufruf
wieder erkannt, 0:0 → 30:0 → 60:0 …“) war falsch: Bei 60:0 greift keiner der beiden Vergleiche. `MatchEngine.DebCupExtraRoll`
(`DebCupExtraRollPairings = 2`) stimmt mit einer Nachbildung der Originalschleife in 24 000 Zufallsterminen überein. RR-01 ist
damit behoben, das Label `beleg-strittig` erledigt.

## 2. Befunde

Keine neuen. **RR-01: behoben, Lesart Fixlauf bestätigt.** Beleg (`ndisasm -b16 -o <a> -e <a> em.exe`, selbst gelesen):
- 0x3731F läuft je Zeile (Maske `[bp+8]`, DEB = 1) über die Plätze `CFF8[Termin]` … Ende `[bp-3]` (erstes 5CFB = 0xFF,
  0x36BF6–0x36C17) in Schritt 2 (0x377A3–0x377B3); Paarungen mit D014-Byte ≥ 0x64 (Serie entschieden) werden weder gerechnet
  noch markiert (0x373D6/0x373F0 → 0x377A3). Je gerechneter Paarung: Ergebnis überschreibt 5C4B/5C4C (0x376AE/0x376C5), dann
  Aufruf 0x32AA3 (0x376E1, Argumente Termin, 0, Ende, Maske).
- 0x32AA3, Zeile 0: Start `CFF8[Termin]` (0x32AED), alle Plätze bis Ende (0x32DFD–0x32E0D). Treffer bei Heim = Gast (0x32B23)
  oder Heim − 30 = Gast (0x32B3F `add ax,0xffe2`, 16 Bit; 0x32B58). Übersprungen nur, wenn **beide** D014-Bytes > 0x63
  (0x32B71, 0x32B88 → `jmp 0x32DFD`). Treffer: Rückgabe 1 (0x32B92), Heimbyte + 30 (0x32B9D–0x32BAA), dann **weiter** mit dem
  nächsten Platz (0x32C73–0x32C7C → 0x32DFD bzw. 0x32DE3, das in 0x32DFD mündet) – kein Abbruch, alle passenden Plätze werden in
  einem Aufruf markiert.
- Folge: Eine offene Paarung steht zu Terminbeginn auf 0:0 (Rücksetzen 0x34C94–0x34CAA nach jeder Wertung, Wertung erst nach
  dem Termin in der Schleife 0x34B9B). Aufruf nach Paarung 1: 0:0 → 30:0 (Heim = Gast); nach Paarung 2: 30:0 → 60:0
  (Heim − 30 = Gast); nach Paarung 3: 60:0 ≠ 0 und 60 − 30 ≠ 0 → kein Treffer. Gerechnete Paarungen treffen nie (nach dem Wurf
  gilt Heim + x ≠ Gast + y, gespeichert mit + 30 bzw. + 60), außer die eben gerechnete bei eigenem Gleichstand.
- **Ausführbarer Beleg** (Python, liest `em.exe` und prüft die Bytes aller oben genannten Stellen, bildet die Schleife nach;
  nicht committet): `python3 -I rr01_loop.py ~/referenz/original/gamedata/em.exe`

  ```
  Bytes ok
  3 Paarungen, alle ungleich 3:1: Wurf bei Paarung [0, 1]
  3 Paarungen, Paarung 1 eigener Gleichstand: Wurf bei Paarung [0, 1]
  4 Paarungen, alle ungleich 3:1: Wurf bei Paarung [0, 1]
  4 Paarungen, Paarung 1 eigener Gleichstand: Wurf bei Paarung [0, 1]
  5 Paarungen, Paarung 1 entschieden: [0, 2]
  5 Paarungen, alle ungleich: [0, 1]
  Vergleich Nachbildung vs. Remake-Regel: 24000 Termine (1-8 Paarungen, Gleichstaende, entschiedene), Abweichungen: 0
  ```

  Die Lesart des Erstreviews ergäbe bei 4 Paarungen `[0, 1, 2]`. Die Remake-Regel im Skript ist
  `MatchEngine.DebCupExtraRoll` (höchstens zwei schon gerechnete, offene Paarung dahinter, entschiedene zählen nicht) plus
  eigener Gleichstand aus `FastCupResultWithTie`.
- Videobild: `folge13_02545` (DEB-Pokalspiele 29.9., Runde 1 mit Ergebnissen) ist ein Managertag (Berliner SC hervorgehoben),
  unterscheidet die Lesarten also nicht. Ein Bild eines Mehrpaarungstermins ohne Managerverein ist weiter nicht bekannt; der
  Beleg ist die Lesung oben.
- Nicht geprüft (außerhalb RR-01, keine Folge für die Empfehlung): die Bemerkung in `MatchEngine.DebCupFastPath.cs`, eine Paarung
  der Minutenschleife zähle als gerechnet (0x37211) – an Managertagen läuft der Termin im Original über den Minutenweg.

Erwartet offen, bestätigt:
- **A876/A917 bleiben K:** 0x55850–0x5585C (`cmp al,[0x386]; jc`), 0x558A5 Rücksetzer unverändert; Delta berührt sie nicht.
- **WM-Endrunde/Europapokal in 0x32AA3 nicht gelesen:** 0x32AF4 (`cmp [bp+6],1`) / 0x32C7F (`cmp [bp-1],3`) leiten in den
  eigenen Zweig; Restlücke unverändert.

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh --schnell` auf dem Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3850 | 0 | 0 |

Gegenbeispiele und Mutationen (temporäre Kopie, `dotnet test tests/EishockeyManager.Core.Tests --filter …ReResteTests`):

| Probe | Ergebnis |
|---|---|
| Probe des Erstreviews: `FastCupResultWithTie(…, knockOut: true, debCup: true, debExtraRoll: true)`, Saat 0 … 1999, jedes ungleiche Ergebnis muss würfeln | **grün**, 1728 von 1728 (vorher rot, Actual 0) |
| Altes Verhalten (kein Wurf bei `h != g`, `FromReport`-Zweig aus, `DebCupExtraRoll` = false) | **rot**: alle drei neuen Tests (`FastPath_DebCupExtraRoll_OnClearResult`, `FromReport_DebCupExtraRoll_ClearResultCarriesExtraGoals_OneMark`, `DebCupExtraRoll_FirstTwoPairingsBeforeAnOpenOne`) |
| Lesart Erstreview (`DebCupExtraRollPairings = int.MaxValue`) | rot: `DebCupExtraRoll_FirstTwoPairingsBeforeAnOpenOne` |
| nur erste Paarung (`DebCupExtraRollPairings = 1`) | rot: `DebCupExtraRoll_FirstTwoPairingsBeforeAnOpenOne` |

Akzeptierte `[APPROX]`: keine neuen. Annahmen: Ein Termin ist im Original der Abschnitt `CFF8[Termin]` bis zum ersten
5CFB = 0xFF (wie im Erstreview und im Remake angenommen); Freilos-Paarungen (5CFC = 0xFF) im DEB-Pokal nicht betrachtet.
