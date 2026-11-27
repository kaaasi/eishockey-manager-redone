# Review: Abschluss Runde 11 (PR #475)

Geprüft: Base `121c99344a30130a41a055781ab3358d9f8e804e`, Head `31543d2fa49068d8f8eeede2457aa13f7c0339a1` (Tree `140f3a51740ad6e7fc362a765f8ba0844d813a36`).
Reviewer: Claude (Sonnet 5.5), frische Sitzung ohne Mitarbeit am PR. Ausführung am nachgewiesenen Checkout:
Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer; Originaldaten aus dem Referenz-Repo (`SHA256SUMS` geprüft, ok).

## 1. Merge-Empfehlung

**Ja für genau diesen Head** (Ausführung am Checkout, Lauf `tools/pruefen.sh --schnell`; die drei Läufe waren nicht verlangt und
nicht nötig, weil keine Test-Infrastruktur geändert ist). Der PR ändert in `src/` und `tests/` nur zwei Kommentare, das Archiv
ist vollständig, beide Befunde R11T3-01/02 sind behoben und die Funktionskarte ist reproduzierbar. Offen bleibt ein P3
(Vollständigkeit der Kandidatenliste), das den Merge nicht hindert.

## 2. Befunde

| ID | Prio | Fundstelle | Befund |
|---|---|---|---|
| R11A-01 | P3 | `docs/arbeitspakete.md` „Kandidaten Runde 12“ | „Serien der Pokal-/Play-off-Spiele“ (erwartet offen in Review #468) fehlt in der Liste |

**R11A-01 (P3).** Review #468 (`reviews/2026-11-02-integration-wettbewerbe-spiel.md`, „Erwartet offen“, Z. 164) nennt neben U8
und A184/A174 auch „Serien der Pokal-/Play-off-Spiele“. Gegenbeispiel: `grep -n -i "serie" docs/arbeitspakete.md
docs/archiv/runde-11.md` liefert keinen Treffer zu diesem Punkt; im Code ruft nur die Liga (`GameState.cs:108`) und das Länderspiel
(`CupsNational.TeamInfo.cs:189`) `CountSeries` auf, Pokal und Play-offs nicht. `docs/reverse-engineering.md` Z. 780 führt die Lücke
als `[APPROX]`, aber nicht als Kandidat. Vorschlag: eine Zeile in „Kandidaten Runde 12“ (mit Verweis auf Z. 780 und `StatisticsClub.cs:272`).
Alle übrigen offenen Punkte der Reviews #463/#464, #468/#471, #469, #470, #473, #474 stehen in der Liste oder sind laut Code
bzw. Archiv erledigt (KT-01 bis KT-05, KTN-01, MU-01 bis MU-03, R11M-01 bis R11M-03, R11I-01 bis R11I-03, WS-01/03/05, K2).

Anmerkung (kein Befund): Zeile A129 im Stand-Vermerk („Energie `random(80, 120)` (die 80 ist der Gehaltsfaktor …)“) ist knapp
formuliert; Code (`FinanceTalent.cs`: `Energy = random(80,120)`, `SalaryPercent` 80) stimmt mit beiden Aussagen überein.

## 3. Geprüft

### Gegenbeispiele

| ID | Ablauf | Ergebnis |
|---|---|---|
| R11T3-01 | `FinanceStadium.cs:359`; Bytes bei Datei `0xC037` in `em.exe`: `55 8bec 83ec02 56 57 8b7606 8b7e08 3bfe 7d08 …` = `push bp; mov bp,sp; …; cmp di,si; jge` (Grenzentausch bei b < a) | **behoben**: Kommentar „Datei 0xC037 (0x38:0x847 in finanzen-rekonstruktion.md)“; `inventur.md` Segmenttabelle ebenso. Marker `[ORIGINAL]` bleibt zu Recht |
| R11T3-02 | `inventur.md` Z. 747/748 (F066/F067) gegen `Playoffs.cs` Z. 54–66 | **behoben**: „Heimtausch/Siege [ORIGINAL]; Spiel 1 beim besser Platzierten [APPROX]“, im Code genau so markiert (0x31505, 0x315E9) |
| Funktionskarte | `python3 tools/reverse-engineering/funktionskarte.py gamedata/em.exe --repo . --write` am Head | **kein Diff** (`git status --porcelain` leer); Zahlen in `re-abdeckung.md` (88 / 1 / 11 %, 4712 Zuordnungen) stimmen |

### Besonders prüfen

1. **Archiv.** Zeilenweiser Vergleich von `git show 121c993:docs/arbeitspakete.md` (Abschnitt Runde 11, Z. 11–261) mit
   `docs/archiv/runde-11.md`: alle Zeilen enthalten; abweichend nur die erklärten Änderungen (Überschrift, Ergebnis-Absatz, relative
   Links, nachgeführte Status der sieben Spuren, WS-02 „entschieden“ statt „vorläufig“, neue Abschnitte „Entscheidungen
   Rundenende“ und „Funktionskarte“). „Nicht in Runde 11“ und „Zurückgestellt“ (Z. 262–281) stehen unverändert in `arbeitspakete.md`.
   Alle relativen Links beider Dateien lösen auf (Skript, keine Fehlstelle).
2. **Inventur „Stand Runde 11“.** 84 Zeilen mit Zusatz (davon 83 neu laut PR); Stichprobe 12 Zeilen gegen Code und PR-Texte:
   A024, A084, A090 (bleiben `[APPROX]`, nicht als Originaltreue dargestellt), A048, A118–A132 (Talentsuche: `Player.Factory.cs`
   `ApplyScoutedTalentValues` Alter 18–21, Form 45–55, Technik/Kondition `s−5…s+1`; `FinanceTalent.cs` Energie 80–120, Gehalt 80 %; für
   Computervereine ausdrücklich `[APPROX]`), A349 (nur Kommentar), A003/A011/A014/A015/A019/A022/A025/A030–A032 (Aussagen decken
   sich mit den PR-Texten #420/#435). Keine Originaltreue behauptet, die nicht belegt ist. Nicht übertragene Zeilen (A016, A273,
   A302, A334) sind im Vermerk offen benannt.
3. **Kandidatenliste.** Siehe R11A-01; sonst vollständig (E2–E5, WS-04, A184/A174, SW-02/05/06, R11G-01..06, U4/U5/U8/U9/U15,
   UM-2/3/4, `FinanceStadium.Strength`, WS-05-Beobachtung, #474-Prüfbereiche, P3 Nr. 2/4/6, `livespiel`).
   `git diff --stat 121c993 HEAD -- src tests`: zwei Dateien, je eine Kommentarzeile (`FinanceStadium.cs:359`,
   `FinanceCalibrationTests.cs:293`); kein Code, kein Test geändert.

### `tools/pruefen.sh --schnell` am Head

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1982 | 0 | 0 |

Build Release mit Warnungen als Fehler; SDK 10.0.402; Ergebnis: bestanden. Lauf ohne Originaldaten übernimmt die CI (nicht selbst ausgeführt).

### Akzeptierte `[APPROX]` und Annahmen

Keine neuen `[APPROX]` im PR. Unverändert akzeptiert: die in der Inventur als „bleibt“ vermerkten (A024, A084, A090,
Computervereine bei A118–A132). Annahme: die PR-Zuordnungen (#417 bis #454) in den Stand-Vermerken wurden nur stichprobenartig
gegen Code, nicht gegen jeden PR-Text geprüft; die übrigen rund 70 Zeilen sind nicht einzeln nachvollzogen.
