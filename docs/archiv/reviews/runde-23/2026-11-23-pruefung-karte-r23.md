# Review PR #976 (Spur pruefung-karte, Runde 23)

Geprüft: Base `4ae1735`, Head `894f301` (`git rev-parse HEAD` = Head, `git status --porcelain` leer; Mit-Daten-Lauf mit Originaldateien aus dem Referenz-Repo). Reviewer: Claude (Sonnet 5.5), frische Sitzung, geplanter Lauf.

## 1. Merge-Empfehlung

**Ja** für Head `894f301`. Keine P0–P2. Vier P3 (Wortlaut/Verweis), kein Blocker.

## 2. Befunde

- **P3-1 – falsche ID für die belegte Marke.** `docs/analyse/restliste-v1.md:52` und `docs/arbeitspakete.md:129` nennen „A964 (alt: ‚Zählt 0x54910 über +0x15?‘)“. Der entfallene Marker ist **A899** (D, `MatchEngine.Managed.cs:110`, Wortlaut „Ob 0x54910 … nicht gelesen“; steht im Abschnitt „entfallen“, Zeile 1220). **A964** ist ein anderer, neuer Marker (`MatchGate.cs:167`, Strafenzähler, K). Beleg: `git grep -n A964 origin/main` = 0 Treffer; `git grep -n "0x54910" origin/main -- docs/analyse` = A899. Der Inhalt stimmt, nur die ID im Text. Die CSV-Zeile A899 (D, „nicht gelesen“) ist veraltet, aber wirkungslos (ID entfallen).
- **P3-2 – Abschnittsverweise in zwei Klassenzeilen.** A962 nennt „abgleich-dosbox-22 §2 (A057)“, A057 steht in §3; A966 nennt „§6 (A614)“, A614 steht in §5 (A629 ist §6). Beleg: `grep -n "^## " docs/analyse/abgleich-dosbox-22.md`.
- **P3-3 – A964-Kommentar verkürzt die Bedingung.** `MatchEngine.Managed.cs:110`: „zählt jeden Platz mit +0x15 ≠ 0“. Disassembly Datei 0x54918–0x54927 (`ndisasm -b 16 -o 0x54900 -e 0x54900 em.exe`): `cmp [es:bx+0xa],0 / jz` **und** `cmp [es:bx+0x15],0 / jz` – es zählt nur ein belegter Platz (+0x0A ≠ 0) mit +0x15 ≠ 0. Die Aussage `[ORIGINAL]` trägt trotzdem (der sechste Platz hat +0x15 = 1), nur die Bedingung ist unvollständig.
- **P3-4 – #969-Prüfung im Lauf mit Daten nur lokal wirksam.** `tools/pruefen-selbsttest.sh` führt die Zeile „Hänger gekennzeichnet (Lauf mit Daten)“ nur aus, wenn `gamedata/kb` da ist (`[ -d … ] && zeile …`). In der CI (ohne Daten) bleibt die Mutation „Vermerk der Zeile ‚Mit Originaldaten‘ entfernt“ unbemerkt; der Lauf ohne Daten ist dort abgedeckt. Kein Fehler, aber im PR-Text nicht erwähnt.

## 3. Ausgeführte Prüfungen

**Gegenbeispiele (erneut ausgeführt):**
- #969: `tools/pruefen.sh` Zeile 60 (`|| vermerk=" – **FEHLER: unvollständig**"` → `|| vermerk=""`) → `tools/pruefen-selbsttest.sh`: `FEHLER: Tabelle 'haenger': Hänger gekennzeichnet (Lauf mit Daten)`, Rückgabe 1. Gleiches für Zeile 74 (Lauf ohne Daten) → rot. Ohne Mutation 15 ok, 0 Fehler.
- #974: `Highscore.cs:385` `: club.StoredTableRank` → `: 0` → `Highscore_BeforeFirstMatchDay_UsesStoredRankOfPreviousSeason` rot (1 failed). `OriginalImport.Rest.cs:201` Vertragsnummer `c[4]` → `-1` → `Import_Sponsoring_SameTermsOtherNumber_StaysTwoContracts` rot (1 failed, mit Originaldaten). Beide Dateien danach wiederhergestellt.

**Besonders prüfen:**
1. Generatoren auf Head: `markerliste.py --repo . --write` und `funktionskarte.py --repo . --write gamedata/em.exe` → `git status --porcelain` leer (idempotent). Zusammenfassung V 0, X 0, unklassifiziert 0; Zählung 228 Dateien/573 Marker = `grep -rc '\[APPROX\]' src/`-Gegenprobe. CSV: 24 F-Zeilen Klasse X (im Generator ausgeblendet, Zahl im Text). A961–A973 je genau eine Zeile in der Markerliste, Beleg je Zeile vorhanden und gegen `abgleich-dosbox-22.md`/`re-reste-23.md` gelesen (Verweise: P3-2). A964-Aussage `[ORIGINAL]` Datei 0x54918 am Disassembly bestätigt (P3-1, P3-3).
2. #954: Wortlaut A537 (`GameState.SeasonEndTurn.cs`: „umgesetzt“ statt `[ORIGINAL]` ohne Fundstelle) und A665 (`SeasonFlow.Zwangsabstieg.cs`: Token vor der Remake-Aussage entfernt) entsprechen dem Befund; Generatorlauf ohne Rest.
3. Test-Infrastruktur: `tools/pruefen.sh` (alle drei Läufe, Build mit Warnungen als Fehler), SDK 10.0.402, Ergebnis „bestanden“:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3852 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2716 | 0 | 626 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2717 | 1135 | 0 |

`tools/pruefen-selbsttest.sh`: 15 ok, 0 Fehler. Zahlen = PR-Beschreibung.

**Erwartet offen, bestätigt:** Klassen A961–A973 alle K (CSV); 24 X-Funktionszeilen im Generator ausgeblendet (Abschlussprüfung).

**Akzeptierte `[APPROX]`:** keine neuen; A961–A973 als K (kosmetisch/ohne Spielwirkung) akzeptiert. **Annahmen (nicht geprüft):** die Beobachtungen aus den DOSBox-Berichten (A965 „an keinem Stand mehr als ein Manager mit Bankeintrag“, A967) nicht neu erhoben; Mehrsaisonprobe entfällt (nur Tests/Kommentare, Diff gelesen).
