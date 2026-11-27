# Review: Funktionskarte von em.exe (#210)

- PR: #210 · Base `7b484eabe99e208a4c7279850daae3cbf0ec4b36` · Head `b907edea61862ee015adec1e6630bca003ad6c8e`
- Umfang: `tools/reverse-engineering/funktionskarte.py`, `docs/re-abdeckung.md`, `docs/reverse-engineering.md`,
  Zeile `kb\195` in `docs/anforderungen/bildschirme.md`, `tools/reverse-engineering/README.md`, `.gitignore`
- Reviewer: Claude (frische Sitzung ohne Implementierungskontext, geplanter Lauf), 31.10.2026
- Checkout: Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer. Ausführung am nachgewiesenen Checkout
  mit `em.exe` aus `kaaasi/eishockey-manager-referenz` (SDK 10.0.402, `ndisasm` vorhanden).

## 1. Merge-Empfehlung

**Ja, für genau diesen Head.** Das Skript reproduziert `docs/re-abdeckung.md` bytegenau (auch nach Merge mit dem
aktuellen `main` 52dad67, konfliktfrei), die drei Gegenbeispiele stimmen, die 10 Stichproben für Grenzen/Overlay-Zuordnung
und die 10 für „umgesetzt“-Treffer sind korrekt, keine Originaldaten im Diff. Offen nur P3-Punkte.

## 2. Befunde

### RA-01 (P3) – Aufruferzahl von 0x49D36 widersprüchlich

[`re-abdeckung.md` Z. 123](https://github.com/kaaasi/eishockey-manager-redone/blob/b907edea61862ee015adec1e6630bca003ad6c8e/docs/re-abdeckung.md#L123)
nennt „13 Aufrufer“, die erzeugte Tabelle (Z. 369) und die CSV nennen 12 (0x041AC, 0x06D7F, 0x07169, 0x26F20, 0x28165,
0x3F833, 0x42CB1, 0x465F1, 0x494EF, 0x49A3B, 0x4A1DB, 0x5D04F). Handtext auf 12 korrigieren oder „Aufrufstellen“ sagen.

### RA-02 (P3) – Laufzeitbibliothek: Einsprung 0x02327 nicht als eigene Funktion erkannt

0x02293 (Basis 0x0, 151 B) endet ohne `ret` auf `pop bx; push cs; push bx` (0x02327) und fällt in 0x0232A
(`cmp cl,0x10; … shl dx,cl … retf`, Borland-Langschiebehelfer). 0x02327 ist der Nah-Einsprung (Near→Far-Umsetzer) und
gehört nicht zu 0x02293. Reproduktion: `ndisasm -b16 -e 0x2293 -o 0x2293 em.exe | sed -n '/00002327/,+4p'`.
Ohne Auswirkung auf die Abdeckung, weil Systemsegmente nicht zählen; die Prüfung „348 von 364 enden auf ret/retf/jmp“
bezieht sich ausdrücklich nur auf den Spielcode.

### RA-03 (P3) – Falsch beschrifteter Kommentar bleibt in `src/`, Skript braucht deshalb eine Ausnahmeliste

[`funktionskarte.py` Z. 418](https://github.com/kaaasi/eishockey-manager-redone/blob/b907edea61862ee015adec1e6630bca003ad6c8e/tools/reverse-engineering/funktionskarte.py#L418)
(`DGROUP_MISLABELLED`) fängt `SeasonEndSummaryScreen.cs` Z. 31 „Symbol 5 (Datei 0x4B2E)“ ab. Geprüft: DGROUP 0x4B2E =
2, 1, 0, 5, 4, 6, 8 – der Kommentar ist falsch, die Ausnahme richtig. Gegenprobe: Ausnahmeliste leeren → 0x041AC springt
von „unbekannt“ auf „genähert“. Vorschlag: Kommentar in einem kleinen `fix/`-PR auf „DGROUP 0x4B2E“ ändern, dann den
Eintrag entfernen (die Spur durfte `src/` nicht ändern; kein Blocker).

### Prüfpunkte aus dem Auftrag

1. **Funktionsgrenzen und Overlay-Zuordnung (10 Stichproben, Zufall seed 210):** 0x255C0, 0x0219E, 0x2A5F5, 0x0C7AA,
   0x02293, 0x5D04F, 0x13208, 0x1080E, 0x0282A, 0x575C3. Unabhängig nachgerechnet: Segmenttabelle 0x163C0 (12
   Basissegmente mit Größe ≠ 0, 17 Stubs), Basis = 0x1E00 + Seg·16 + Off, Overlay = 0x255C0 + Stub+4 + Off,
   Stub-Einträge `CD 3F` mit passendem Offset (z. B. 0x118:0x70 → 0x3D36 → 0x49D36, Overlay 0x118 ab 0x46000, Größe
   0x4DE2). Neun beginnen mit `55 8B EC` bzw. am Segmentanfang und enden auf `ret`/`retf`/`retf 8`, das nächste Byte ist ein
   Prolog; Ausnahme 0x02293 → RA-02.
2. **„umgesetzt“-Treffer (10 Stichproben, seed 7):** 0x32E23, 0x5D04F, 0x0B7FA (`99F:000A`), 0x35691, 0x42CB1
   (0x42E2A), 0x06CA6, 0x08365, 0x54C9F (z. B. 0x5569F, 0x55A86, 0x55CD3), 0x3CABF (0x3CAD5), 0x0939A – jede Fundstelle
   ist ein Dateioffset bzw. Seg:Off innerhalb der Funktion und steht unter `[ORIGINAL]`. Zusätzlich alle
   „umgesetzt“-Fundstellen ohne Kennwort durchgesehen (z. B. 0x4DB6, 0x874D, 0xC097): Es sind exakte Funktionsanfänge mit
   Prolog im Code-Kontext, kein DGROUP-Zufall gefunden. 0x54C9F (6,6 KB) ist grob gekörnt – in „Grenzen“ benannt.
3. **20 größte Lücken:** Benennungen gegen Texte, Bilder und Aufrufer der CSV geprüft (z. B. 0x3901B „Penaltyschiessen“,
   „n.P.“ ← 0x32AA3; 0x2A9AD „JWE Service GmbH“, „Kündbar.“ ← 0x2B0C0; 0x4F78D Tabellenkopf, kb\33; 0x5DBE5 „.HOK“,
   „VERZEICHNIS:“; 0x41683 „Rückennummer“, „IM TEAM/RESERVE“). Alle plausibel; Nr. 7 und 18 (ohne Texte) sind als
   Vermutung gekennzeichnet. Die `kb\195`-Korrektur hält: einziger Laderaufruf mit Konstante 0xC3 im ganzen Code ist
   0x5D98B (in 0x5D90D, `mov al,0xc3; push ax; call 0x38:0x98d`).
4. **Keine Originaldaten:** Diff nur Text (6 Dateien, keine Binärbytes), CSV nicht im Repo, `__pycache__/` ignoriert.
   Enthalten sind kurze Textauszüge aus em.exe (längster ≈ 47 Zeichen) als Hinweise – wie in bestehenden Docs üblich und
   in der README offen benannt.

### Erwartet offen (bestätigt)

- Wortliste `DGROUP_WORDS` für den DGROUP-Kontext ist heuristisch. Gegenprobe: Liste unwirksam gemacht → 0x05CA0 wird
  „dokumentiert“ statt „unbekannt“ (die übrigen Regeln verhindern „umgesetzt“).
- Echte Fundstellen ohne Kennwort gehen verloren (Beispiel `Tor (0xA88C ff.)` in „Grenzen“ dokumentiert).

## 3. Ausgeführte Prüfungen

| Befehl | Ergebnis |
|---|---|
| `python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --write --csv <scratch>` | 1,3 s; `git status` danach leer → eingecheckter Block identisch; 678 Funktionen (504 unbekannt, 121 umgesetzt, 48 dokumentiert, 5 genähert) |
| Gegenbeispiele 0x05CA0 / 0x041AC / 0x49D36 | unbekannt / unbekannt / genähert ✔ |
| Merge mit `origin/main` 52dad67 + `--write` | konfliktfrei, Block unverändert |
| Mutation `DGROUP_MISLABELLED` leer | 0x041AC → genähert (Regel wirksam) |
| Mutation `DGROUP_WORDS` unwirksam | 0x05CA0 → dokumentiert (Regel wirksam) |
| Eigenes Prüfskript (Segmenttabelle, Stubs, `ndisasm` je Stichprobe) | siehe Prüfpunkte 1–2 |
| `tools/pruefen.sh --schnell` | siehe Tabelle |

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1071 | 0 | 0 |

Commit: b907edea61862ee015adec1e6630bca003ad6c8e · SDK 10.0.402 · Ergebnis: bestanden. Nur Doku/Tools, daher kein
Dreifachlauf (Auftrag).

**Akzeptierte `[APPROX]`/Heuristiken:** Funktionsende = nächster Anfang; „umgesetzt“ als Obergrenze wegen Körnung;
Bildnummern nur als Konstante vor dem Lader; DGROUP-Kontext per Wortliste. Alle im Dokument unter „Grenzen“ benannt.
**Annahmen:** Die Zuordnung der alten Ghidra-Segmentnamen zu Stub-Selektoren (`GHIDRA_OVERLAY`, nur Beschriftung in
Klammern) wurde nicht unabhängig gegengeprüft; die Dateioffsets hängen davon nicht ab.
