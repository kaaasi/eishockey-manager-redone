# Review PR #866 – Spur `abgleich-dosbox-19` (Erstreview)

Reviewer: Claude Code (Claude Opus 5.5), geplanter Lauf, Sitzung ohne Implementierungskontext · Base `2b000ac` (= `main`) ·
Head `476f2a3d5a48509f5582e40830053fe905eaa2f0` (`git rev-parse HEAD` gleich, `git status --porcelain` leer) · 15.11.2026

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Alle drei Gegenbeispiele reproduziert, Rückschaltproben rot, alle drei Läufe von
`tools/pruefen.sh` grün, CI `build` auf dem Head grün. Der Fix #847 ist an em.exe nachgelesen (Datei 0x2A06C–0x2A073:
`push 0; push [0xA70E]; call 0x28:0x146C` direkt nach der Aufstellung 0x108:0x4D – derselbe Stub wie im Tageslauf
0x54FB8–0x54FBF, dort mit Schalter 1) und von vier bestehenden Mehrsaisonproben durchlaufen. Nur P3.

**Zu „Besonders prüfen“:**

- **#730 „im Bereich“ schließen: getragen.** Die fünf Läufe FILE66–70 sind verschiedene Spielstände (md5 paarweise
  verschieden) desselben Starts wie die Remake-Saaten (FILE50) – der Vergleich ist sauber. Das Kriterium aus §4.4 A (Mittel um
  49 → kleinere Rate; um 60 → Zufall) war vor der Messung festgelegt, Ergebnis 58,4. Rechnet man den Lauf heraus, der die
  Hypothese „kleinere Rate“ ausgelöst hat (FILE02→50→61→63, Auswahlfehler), bleiben FILE02→03, FILE02→51, FILE50→66…70:
  513 gegen 532,8 ± 20,6 (Zahlen aus §4.2/§4.5), z ≈ −1,0. Die Gegenprobe Rate · ¾ wird rot (s. u.). **Grenze der Aussage:**
  Rate · 0,9 bleibt grün (Remake 39,3 gegen Original 45,4, Grenze ±6,5); ein Restunterschied unter etwa 10 % ist nicht
  ausgeschlossen. Interpoliert zwischen · 1 (47,8) und · 0,9 (39,3) liegt das Original bei etwa · 0,97. Für „im Bereich“
  genügt das, mit dem Rest-Absatz in §4.5 ist es offen dokumentiert.
- **Pflichteinträge #730 in den Sägezahn: ja, aber mit Drift-Wächter (P3-2).** ZIMMERMANN (FILE16A→17) liegt nicht nur bei
  den sechs Prüfsaaten daneben, sondern bei **39 von 40** Saaten (Probe unten, nur Saat 28 kippt wie das Original auf 50).
  Das Original kippt, das Remake bleibt bei 97–122 – also das Original *mehr* Belastung. Das ist die Gegenrichtung zur
  geprüften Hypothese „kleinere Rate“ und widerlegt das Schließen nicht; es ist ein einzelner Phasenwert über eine Saison
  (bei vielen Spielern und Paaren ist ein Ausreißer mit p ≈ 1/40 erwartbar). Ein geschlossener Befund sollte aber nicht
  als „Befund #730 (Restursache offen)“ im Code stehen bleiben. Empfehlung: ZIMMERMANN in `Expected` nur noch
  `StrainSawtooth`, dafür `[InlineData("FILE16A", "FILE17", "14 SV BAYREUTH", "ZIMMERMANN", 90, StrainSawtoothMax, 6)]` in
  `Strain730_DeviatesAtMostSeeds`. Der Test heißt dann besser `Strain_DeviatesAtMostSeeds_DriftGuard` und bekommt den Text
  „ehemals #730, im Bereich geschlossen R19“. Die Wächterwirkung (Abdriften → rot) bleibt so erhalten. Das kann der nächste
  Lauf der Spur oder `pruefung-karte` übernehmen, es muss nicht vor dem Merge passieren.
- **Fix #847 mit Mehrsaisonprobe: erfüllt.** Keine neue Probe nötig. Die vier bestehenden Mehrsaisonproben
  `AddManagerTests.AddManager_MultiSeason_FourSeasonChanges_PassiveAndActive`, `…AfterLoad_MultiSeason_OriginalData…` und
  `NeuerManagerTests.AddManager_WithSwap_MultiSeason_…` (synthetisch und mit Originaldaten; passiv und aktiv; je vier
  Saisonwechsel) laufen nachweislich durch den neuen Zweig (Probe M1b) und sind am Head grün. Die PR-Angabe „Mehrsaisonprobe –“
  ist damit zu streng formuliert; sachlich ist sie abgedeckt.
- **Fremder Test `NeuerManagerTests` (R15-E2 = C): akzeptiert.** Die Teamwerte von Platz r werden für den Platzvergleich
  vorübergehend zurückgesetzt, danach geprüft (DefenseCondition, AttackForm). Der Rest der Platzinvariante bleibt
  unverändert. Rückschaltprobe M1 → rot. Geprüft werden nur zwei der sechs Felder; die vollständige Prüfung leistet
  `File64ToFile65_SwapLikeOriginal`. Dort erfüllt auch die Originalseite selbst „Teamwerte = Aufstellung“, und das ist der
  stärkste Beleg des Fixes.
- **Linientests 1000 Saaten / 0,1 %: akzeptiert.** Laufzeit File61 15 s, File63 ≈ 16 s, zusammen 33 s mit File50 (gegenüber
  17 min für die Vollprüfung). Begründung tragfähig: Bei 32 neuen Kennzahlen × 2 Seiten wäre 1 % je Seite zu eng. Die Ausgabe
  zeigt „Gast Abwehr R2“ mit 3 von 1000 ≥ und „Heim getrennt“ mit 5 von 1000 ≥. Die Gegenprobe `OnIceMinutes > 5` macht
  File50 und File61 rot, File63 bleibt grün; File63 trennt diese Mutation also nicht. Das ist hinnehmbar, weil die Regel
  von File50/61 gehalten wird.

**Erwartet offen – bestätigt:** A093 (laut Bericht §4.4 und §4.5 spielt in `runde-19` ohne Klick kein Spieler außerhalb
seiner Reihe; nicht belegbar). Neues P3 „Reihe auf dem Eis bei Spielbeginn“: in den Testdaten belegt (`File61[3]` Köln Sturm
beginnt mit `222`, Abwehr `File61[2]` mit `000`), `[APPROX]` an `ChangeLines` vorhanden; Issue durch die Leitstelle.

## 2. Befunde

- **P3-1 Build-Artefakt committet:**
  [`docs/analyse/belastung-staerke/__pycache__/hok.cpython-313.pyc`](https://github.com/kaaasi/eishockey-manager-redone/blob/476f2a3d5a48509f5582e40830053fe905eaa2f0/docs/analyse/belastung-staerke/__pycache__/hok.cpython-313.pyc)
  (Commit `7a1dc78`). Beleg: `git diff --stat 2b000ac 476f2a3 | grep pyc` → 1 Datei, 2644 Byte; `grep -n pycache .gitignore`
  → kein Treffer. Fix: Datei löschen, `__pycache__/` in `.gitignore`.
- **P3-2 Pflichteintrag eines geschlossenen Befunds:**
  [`AbgleichOriginalPaareTests.cs` Z. 120](https://github.com/kaaasi/eishockey-manager-redone/blob/476f2a3d5a48509f5582e40830053fe905eaa2f0/tests/EishockeyManager.Core.Tests/AbgleichOriginalPaareTests.cs#L120).
  Dort steht weiter `StrainOf(… "ZIMMERMANN")` mit Kategorie `Strain = 730` und dem Text „Befund #730 (Restursache offen)“
  (Z. 72), obwohl #730 mit diesem PR geschlossen ist. Beleg (Probe ZIMMERMANN, Abschnitt 3): 39/40 Saaten außerhalb ±20.
  Vorschlag wie in Abschnitt 1. Das betrifft nur Kennzeichnung und Wächter, keine Abweichung im Spiel.
- **P3-3 Doppelte Rechnung:**
  [`GameState.AddManager.cs` Z. 175](https://github.com/kaaasi/eishockey-manager-redone/blob/476f2a3d5a48509f5582e40830053fe905eaa2f0/src/EishockeyManager.Core/GameState.AddManager.cs#L175)
  wiederholt den Rumpf von `GameState.UpdateManagerTeamValues` (`GameState.DailyRun.cs` Z. 236–245) wörtlich. Beleg:
  `diff` der beiden Blöcke ist bis auf die Schleife leer. Vorschlag: `UpdateManagerTeamValues` ruft
  `WriteLineupTeamValues(club)` je Manager auf, damit eine spätere Korrektur nicht nur eine der beiden Stellen trifft. Das
  „wie dort“ für „Teamwerte noch nicht gezogen → nichts“ verweist auf ein `[APPROX]` von `UpdateManagerTeamValues`; die
  neue Methode sollte es selbst als `[APPROX]` kennzeichnen (Datei 0x2A073 ruft ohne Bedingung).
- **P3-4 Uneinheitliche Bezugsbasis im Bericht:**
  [`abgleich-dosbox.md` Z. 331](https://github.com/kaaasi/eishockey-manager-redone/blob/476f2a3d5a48509f5582e40830053fe905eaa2f0/docs/analyse/abgleich-dosbox.md#L331).
  „714,5 ± 24,2“ setzt für FILE02→63 die Eiszeit-Simulation an (181,8), für die übrigen Paare `hits.py`. Mit `hits.py`
  durchgehend (173,6) ergibt sich 706,4 ± 24,4, z ≈ −2,2 statt −2,6. Beleg: Summe der Tabellenwerte §4.2/§4.5. Die Aussage
  ändert sich nicht. Ebenso nennt `AbgleichDosboxStrainHitsTests` in den Remarks die `hits.py`-Werte mit Erholung
  (64/58/57/60/53). Der Test misst und gibt ohne Erholung aus (51/45/44/47/40, Mittel 45,4 gegen 47,8 ± 6,6). Ein Satz
  dazu fehlt.

Keine P0–P2.

## 3. Ausgeführte Prüfungen

Umgebung: `tools/claude/fetch-reference.sh original`, `setup-workspace.sh`, SDK 10.0.402, 2 Kerne. Spielstände FILE50–70 aus
`~/referenz/original/spielstaende`.

`tools/pruefen.sh` (alle drei Läufe, 17 min 9 s, Ergebnis „bestanden“, Commit 476f2a3):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3364 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2425 | 0 | 571 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2426 | 938 | 0 |

Gegenbeispiele und Mutationen (Kopie per `git worktree`, `dotnet test … --filter`):

| Probe | Ergebnis |
|---|---|
| #730 `File50_FiveRuns_StrainHitsMeanWithinRemakeSpread` am Head | grün: Original 51, 45, 44, 47, 40 (45,4); Remake 47,8 ± 6,6; Grenze ±6,4 |
| #730 Mutation `DrainEnergy`: Treffer nur bei `random.Next(4) != 0` (Rate · ¾) | **rot**: Remake 30,0 ± 5,8, Grenze ±5,6 |
| #730 Mutation Rate · 0,9 (`Next(10) != 0`) | grün (39,3 gegen 45,4, Grenze ±6,5) – Trennschärfe, s. Abschnitt 1 |
| #847 M1: Aufruf `WriteLineupTeamValues(club)` auskommentiert | **rot**: `File64ToFile65_SwapLikeOriginal` (Hannover 75,71,71,75,54,47 statt Aufstellung 81,80,80,80,53,55) und `NeuerManagerTests.AddManager_BundesligaStart_…` |
| #847 M1b: `throw` in AddManager, wenn Teamwerte gezogen | rot in 4 Mehrsaisonproben (Abdeckung des Zweigs, s. o.) |
| #838 `FramesPerMinute = 3` | **rot**: `AbgleichDosboxEchtzeitTests.RunningDay_WithinMeasuredNoPauseRange` (2 Pausentests unabhängig davon grün) |
| Linien `OnIceMinutes[part] > 5` statt `> 3` | **rot**: File50, File61; File63 grün |
| em.exe `ndisasm -b16 -e 0x2A030 -o 0x2A030` / `-e 0x54F9C` | Aufruf 0x28:0x146C(m, 0) nach 0x108:0x4D; im Tageslauf derselbe Stub mit (m, 1) |
| ZIMMERMANN: `emtool abgleich FILE16A FILE17 --seed 1…40 --alle` | 39/40 „außerhalb ±20“ (Original 50, Remake 97–113 bei Saaten 1–3/29), nur Saat 28 = 50 |
| md5 FILE66…70 | fünf verschiedene Stände |
| CI Check-Runs Head | `build` success |

Nachgerechnet: LiveClock (0,8 · 15 + 0,2 · 10) / 18,2 ≈ 0,77 s; 15,9/60 bzw. 20/60 = 0,27–0,33 s je Minute; `RunningSeconds`
(60 · 20 + 21)/60 = 20,35 s im Testbereich 15,4–20,5.

**Akzeptierte `[APPROX]`:** Reihe aus dem Vorspiel bei Spielbeginn (`ChangeLines`, Wirkung ≤ 3 Minuten, neues P3); #838
1/3 s je Minute mit Messwert (CPU-abhängig); Teamwerte vor dem ersten Spiel nicht vorziehen (wie `UpdateManagerTeamValues`,
s. P3-3). Die Umstellung des Markers an `ChangeLines` auf `[ORIGINAL]` ist durch File50/61/63 gedeckt.

**Annahmen (nicht geprüft):** Die Reihenfolgen in `File61`/`File63` und die Siebensegment-Lesung habe ich nicht gegen die
Bilder `file61_*`/`file63_*` nachgezählt (Bilder nicht geladen). Die Remake-Verteilungen von `hits.py` (140/400 Saaten) habe
ich nicht neu gerechnet, nur über den Abnahmetest (30 Saaten). Ebenso ungeprüft: der vorgeschlagene P2 „Live-Bildschirm ohne
Rechenzeit“ (Spur `torszenen`).
