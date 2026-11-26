# Review: Plan Spur `live-seiten-rest` (PR #1038)

Prüfer: Claude Code (geplanter Lauf, Sonnet 5.5, frische Sitzung). Base `9d9ae39`, Head `0559ec4` (nur `docs/arbeitspakete.md`, +19). Nur Codelesen
der Git-Objekte (`git diff 9d9ae39 0559ec4`, `git show`); reine Doku, `tools/pruefen.sh` entfällt, kein SDK.

## 1. Merge-Empfehlung

**Ja** für Head `0559ec4`. Die Fundstellen stimmen mit der Übergabe in #1032 überein, Dateigrenzen gegen `torszene-dauer` Teil 2 sind
disjunkt, die Entscheidungen decken sich mit den Kommentaren. Nur P3, alle mit der nächsten Plan-Änderung behebbar.

## 2. Befunde

- **P3-1 Doppelte Zuweisung zweier Dateien.** Der Absatz „Entscheidungen während der Runde“ (Zeile 26–30) erweitert die Dateigrenzen von
  `live-verschraenkt` um `PromotionRoundScreen.cs` und `EuropeanCupScreen.cs`; dieselben Dateien stehen in `live-seiten-rest`. Die
  Leitstelle (#1032, 26.11.) hat nur `LeaguePairingsScreen.cs`, `PromotionRoundScreen.cs`, `EuropeanCupScreen.cs` für Teil 3b erweitert; `live-verschraenkt`
  hat ihre Fortsetzungen verbraucht. Beleg: `git show 0559ec4:docs/arbeitspakete.md | sed -n 26,30p` und `sed -n 113,125p`. Vorschlag: im Absatz sagen, dass die zwei
  Dateien an `live-seiten-rest` gehen.
- **P3-2 Spurtabelle und Ablauf nicht nachgezogen.** Die Tabelle „Spur | Welle | Themen | Modell“ (Zeile 50–55) und der Absatz „Folge (R26-D1 = B)“
  (Zeile 46–48) nennen `live-seiten-rest` nicht; Welle 1b steht nur im Spurkopf. Beleg: `grep -n "live-seiten-rest"` ergibt nur Zeile 113–125. Ein
  Leser der Tabelle startet die Spur nicht.
- **P3-3 „löst PW-04“ ohne Dateien dafür.** Der Befund steht in `docs/wettbewerbe.md:634` und `PixelWettbewerbeTests.cs:106` (Befund PW-04);
  beide fehlen unter „Dateien“. Entweder aufnehmen (minimal) oder „PW-04 in der Übergabe melden“ schreiben.
- **P3-4 „keine gemeinsamen Dateien außer `live-spieltag.md`“ ungenau.** `torszene-dauer` Teil 2 schreibt in `torszene-dauer.md`
  (Zeile 100–102), nicht in `live-spieltag.md`; die Überschneidung ist also leer. Harmlos, aber die Aussage weglassen oder „keine“ schreiben.
- **P3-5 Unverifiziert (Verdacht).** Terminzahl „min(DGROUP 0x7AF, 6 bzw. − 6) + (Minute ∈ {0, 60}) − 1“ ist wörtlich aus der Übergabe
  übernommen; „6 bzw. − 6“ ist mehrdeutig. Nicht gegen `em.exe` geprüft (keine Originaldaten in dieser Sitzung); gehört in den Beleg von Teil 1.

Ohne Befund: Fundstellen 0x31CD5, 0x3AB43–0x3ABCE, 0x32294, 0x32515, 0x3183C und Merker [bp+0xA] gleich der Übergabe vom
26.11.2026 00:45 und `live-spieltag.md` §10 (Zeile 229) im Branch von #1032. `LivePageScreens.Create` wird von `LiveMatchScreen.cs:281` aufgerufen,
`LivePageKind.PromotionRound` und `EuropeanCupTable` bestehen schon in `Core/MatchEngine.LivePages.cs`; `LiveMatchScreen.cs` und Core-Datei
brauchen also keine Änderung, die Dateiliste genügt (`LiveMatchScreen.cs` bleibt `torszene-dauer`). R26-E1 passt zum Status von `tagesabschluss` Teil 2;
T7/F076 passt zu den Leitstellen-Kommentaren in #1033 (25.11.) und #1032 (26.11.).

## 3. Ausgeführte Prüfungen

- `git diff --stat 9d9ae39 0559ec4`: 1 Datei, 19 Zeilen; `mergeable_state` clean, keine PR-Kommentare.
- Gegenlesen: PR-Kommentare #1032 (alle 11) und #1033 (2), PR-Text #1032, Plan auf Head, `5d491e5` (Head #1032) für Aufrufer.
- Nicht ausgeführt: `tools/pruefen.sh` (Doku), Skripte, Videovergleich. `[APPROX]`: keine neuen in diesem PR; PW-04 bleibt `[APPROX]` bis zur Spur.
