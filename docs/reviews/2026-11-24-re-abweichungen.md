# Review PR #998 (Spur re-abweichungen: Belege der Runde 25 aus em.exe)

Reviewer: Claude Code (Opus 5.5), eigene Sitzung ohne Implementierungskontext, geplanter Lauf. Geprüft: Head
`c50b71b49da47a8a6b3b5a8ea936e497a9432d28` gegen Base `ae50e16` (= aktueller `main`, Merge-Base gleich), Checkout
`git rev-parse HEAD` = Head, `git status --porcelain` leer. Diff: nur `docs/analyse/re-abweichungen.md`, eine Statuszeile in
`docs/arbeitspakete.md` und 24 Python-Dateien unter `tools/reverse-engineering/abweichungen/`; nichts unter `src/` oder `tests/`.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head, wegen eines offenen P2 (RA-01). Bei F06 lässt die Vorgabe eine belegte Abweichung im Geld stehen.
Das Original verteilt die Banden immer ab Feld 0. Das Remake verteilt sie ab dem angeklickten Feld, und die Vorgabe sagt
„Überschreiben wie bisher“. Sonst ist der Stand sauber:

- Alle 22 Skripte laufen fehlerfrei und geben wörtlich aus, was im Bericht steht.
- Der Interpreter rechnet die geprüften Befehle richtig.
- Kein Stub verfälscht ein Urteil (Prüfung 2).
- Die Stichprobe aus Teil 3 deckt sich mit Disassembly und Remake-Code.

Die Korrektur ist klein: ein Satz und ein roter Test in der F06-Vorgabe. Danach genügt die Delta-Prüfung der Leitstelle (nur P2/P3).

## 2. Befunde

**RA-01 (P2) – F06: Die Vorgabe übernimmt die Reihenfolge der Felder des Remakes, das Original fängt immer bei Feld 0 an.**
[`re-abweichungen.md:556–566`](https://github.com/kaaasi/eishockey-manager-redone/blob/c50b71b49da47a8a6b3b5a8ea936e497a9432d28/docs/analyse/re-abweichungen.md#L556-L566)
stellt richtig fest: Das Original legt jede Bande in das „erste freie oder kündbare Feld (Feld 0 … 6 der Reihe nach)“. Die
Vorgabe sagt dagegen „Überschreiben wie bisher feldweise“. Das Remake fängt aber beim angeklickten Feld an und läuft im Kreis
weiter:
[`FinanceSponsors.cs:341`](https://github.com/kaaasi/eishockey-manager-redone/blob/c50b71b49da47a8a6b3b5a8ea936e497a9432d28/src/EishockeyManager.Core/FinanceSponsors.cs#L341),
`free.Where(b => b >= slot.Index).Concat(free.Where(b => b < slot.Index))`.

- **Beleg im Disassembly.** Befehl: `python3 tools/reverse-engineering/disasm.py em.exe 0x2BA90 0x2BB90`. In der Schleife je
  Bande setzt Datei 0x2BAD9 `mov word [bp-0x2a],0x0`. Dann sucht 0x2BADE–0x2BB6D die Felder 0 … 6 ab: frei bei +0 = 0, kündbar
  bei +6 Bit 0x80. Das angeklickte Feld liest die Schleife nicht; es wird nur vorher in 0x2BAC3 verglichen.
- **Beleg im Ablauf.** `f06_bande.py` zeigt die Annahme einer Bande: Das kündbare Feld 0 wird überschrieben, obwohl Feld 3 frei
  ist. Ausgabe: `1 Bande(n): [0: 900 Nr 3] [1: 300 Nr 1 k] [2: 500 Nr 2] [3: -] …`.
- **Folge im Remake.** Ein Klick auf Feld 3 belegt dort Feld 3. Der kündbare Vertrag behält beide Felder. Die Einnahme ist
  dann 300 + 300 + 500 + 900 statt 900 + 300 + 500.
- **Fix in der Vorgabe (`saison-geld`).**
  - Annahme je Bande: das erste freie oder kündbare Feld ab Feld 0, ohne Rücksicht auf das angeklickte Feld.
  - Roter Test mit den Feldern wie oben: Klick auf Feld 3, Angebot über eine Bande → Feld 0 neu, Feld 3 frei.
  - Ob das Original für den Klick ein freies Feld verlangt, steht in 0x2BA9D–0x2BAD1; der Fixlauf soll das lesen und angeben.

**RA-02 (P3) – A972: Die Bedingung der Abrechnung ist verkürzt.**
[`re-abweichungen.md:666–667`](https://github.com/kaaasi/eishockey-manager-redone/blob/c50b71b49da47a8a6b3b5a8ea936e497a9432d28/docs/analyse/re-abweichungen.md#L666-L667)
sagt: „Abrechnung zählt jeden Platz mit +0x15 ≠ 0, Datei 0x54918–0x54927“. Die Stelle prüft zuerst
`cmp byte [es:bx+0xa],0` (0x54918, bei 0 Sprung nach 0x549A2) und danach erst `+0x15` (0x54922). Gezählt wird also nur bei
+0x0A ≠ 0 **und** +0x15 ≠ 0.

- Folge für die Vorgabe K07 (4), Test (d): Der Test muss zusätzlich prüfen, dass der sechste Feldspieler eingesetzt bleibt
  (+0x0A).
- Das Urteil ändert sich nicht.

## 3. Ausgeführte Prüfungen

- **Zugriff.** `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD` → `7412a50…`. Originaldateien geladen
  mit `tools/claude/fetch-reference.sh original`: 934 Dateien, `em.exe` 387 968 Byte.
- **`tools/pruefen.sh` entfällt**, wie im Aufruf festgelegt: kein Code unter `src/`. Ein SDK ist in dieser Sitzung nicht
  eingerichtet.
- **Gegenbeispiele.** Alle 22 Skripte liefen mit `python3 <skript>.py ~/referenz/original/gamedata/em.exe`, alle mit
  Rückgabe 0. `hauptmenue.py` ist nur die gemeinsame Hilfsdatei und gibt nichts aus.
  - Abgleich der Zeilen per Skript: Jede Ausgabezeile steht wörtlich in einem Codeblock des Berichts, und umgekehrt.
  - `k07_sechster` steht im Bericht eingerückt in einer Liste, sonst gleich.
  - `f08_sommerpause` ist im Bericht absichtlich auf den ersten Tag gekürzt (`…`). Die Tage mit 0x3B7 = 8 und 9 laufen wie
    angegeben ohne Stadionbau.
- **(1) Interpreter.**
  - Eigener Test mit `nasm` (Code in die Region von 0x0C037 kopiert, dann `cpu.call`): `adc`, `sbb dx,dx`, `cwd` mit `idiv`
    bei negativem Dividenden (−7/2 = −3 Rest −1), `sar`, `rcl` mit CF, `jl`/`ja` bei Vorzeichengrenze, `cbw`, `mul`, `div`,
    `xchg`, `neg`. Alle Ergebnisse richtig.
  - Durchsicht von `alu`, `cond`, `grp3`, `grp45`. `inc`/`dec` lassen CF unverändert. `imul` 8/16 Bit setzt CF/OF richtig.
- **(1) Stubs.** Zum Beleg hat ein Patch von `enter_target` in einer temporären Kopie alle Stubs und Hooks mitgeschrieben,
  während die 22 Skripte liefen. Ergebnis: 81 verschiedene Ziele.
  - **Pascal-Aufrufkonvention:** Im ganzen Code von em.exe enden 14 Funktionen mit `ret`/`retf imm`. Keine davon wird
    ersetzt. Ein Stub, der die Argumente auf dem Stapel lässt, ist darum ausgeschlossen; alle ersetzten Aufrufe räumen beim
    Aufrufer ab (`add sp`/`pop cx`).
  - **`push cs; call near`:** Bei allen ersetzten Zielen passt die Erkennung „fern“ zur Rückkehr der Zielfunktion
    (`ret` gegen `retf`). Keine Abweichung.
  - **Rückgabe 0 der Stubs:** Stichprobe am Stadionbau 0x3D3EA–0x3D447: Die Stubs 0x0390B und 0x0389C sind Textfunktionen,
    der Aufrufer räumt mit `add sp,8` ab, und ihr Ergebnis geht in keine Entscheidung ein. Die Hooks für Zufall 0x0C037,
    Chance 0x09F5C und Schuss 0x0A484 sind in den Skripten offen angegeben.
- **(2) Stichprobe der Urteile.**
  - **K2:** `Club.ClickPlayerInMatch` prüft `PenaltyBoxMinutes` (`Squad.Lineup.InMatch.cs:46`), `NationalCoach` prüft es
    nicht. Bestätigt.
  - **K07:**
    - A936: 0x426C4–0x426E4 liest `[bx+0x375]` mit bx = 2 · Manager.
    - A939: 0x548D3–0x548E7 ruft `random(0, 4)`; bei 0 setzt es +0x02 = +0x15 & 0x7F, für jeden Platz ohne Blick auf
      +0x0A. Der Verletzte spielt also seine alte Reihe ein.
    - A971: Das Remake leitet den Wert ab, `SixthSkaterOpen` (`Squad.Lineup.cs:116`).
    - A807 (Rechtsklick in der Namensspalte): Das Remake fängt ihn vorher ab und öffnet das Spielerdatenfenster
      (`LineupScreen.cs:149`). Keine Abweichung, richtig.
  - **C07:** Bestätigt.
    - Verlängerungstor: Heim 34 bei Endstand 4:3, also eine Marke. Penaltyschießen: Heim 66 bei Endstand 6:5, also zwei Marken.
    - Remake: 2 Marken bei Verlängerung (`Cups.Tiebreak.cs:69`), 1 beim Penaltyschießen; `MarksFromReport` gibt immer
      `TieMarks`.
  - **T03:**
    - A829: Ablauf der Würfe je Platz wie im Bericht.
    - A661, A843: Reihenfolge 0x36169 → 0x5667A → 0x09D1B → 0x35EDD ohne Schreibzugriff auf die Tageszähler.
    - Remake: Der Zug liegt in `FinishClubSeason` (`SeasonFlow.cs:391`). Die Wiederholung bei gleichem Zug ist im Remake
      schon vorhanden.
  - **F08:**
    - A512: Elf Würfe 0x3ED19–0x3ED48 ohne Prüfung des Merkers. Das Remake würfelt nur bei gesetztem Merker
      (`FinanceStadium.cs:544`).
    - A526: `RunSummerBreak` geht je Tag durch alle Manager nacheinander.
    - Beide sind im Remake schon mit `[APPROX]` gekennzeichnet; das Urteil „bestätigt“ ist richtig.
    - A509: Restwoche 0 → Stufe = Ziel ohne Prüfung (0x3D370–0x3D39A).
  - **K13:** Die Tabelle DGROUP 0x3B1 lautet 32, 16, 8, 4, 2, 1. Das deckt die Regel „2 · (32 ≫ Runde)“. `DrawStartLeaguePlace`
    sperrt Satz 0 und die Europapokal-Plätze unabhängig von der Runde. Bestätigt.
  - **F06:** Siehe RA-01. Die Einnahme ist gleich (`FinanceSponsors.cs:116` zählt einmal je Vertrag).
- **(3) Vorgaben.**
  - Je Thema stehen Regel, Fundstellen und Erwartungswerte da und sind testbar. Beispiele:
    B17: 42 und 87; B28: 25/25/14 Pixel; C07: 4:3 mit einer Marke, 6:5 mit zwei Marken.
  - Ausnahmen: F06 (RA-01) und die Ergänzung zu K07 (d) (RA-02). Die Mehrsaisonprobe ist bei T03 und F06 gefordert.
- **Erwartet offen, bestätigt.** Diese Lesarten sind im Bericht gekennzeichnet:
  - B28 „Grenzen des Belegs“ (Bytebedeutung).
  - B29/A700 „Bild, Lesart“.
  - B17: Land des Bundestrainers nicht nachgebildet.
  - Die K07-Seite des Remakes ist nur aus dem Code beschrieben.
- **Keine Originaldaten im Diff.** Es stehen dort nur Adressen, Größen und kurze Textauszüge (Meldungen). Die Skripte lesen
  `em.exe` zur Laufzeit.
- **`[APPROX]`:** Der PR bringt keine neuen; die bestehenden bei A512 und A526 sind durch die Belege gedeckt.
