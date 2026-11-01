# Archiv: Runde 10 – Sonderrunde Analyse em.exe (01.–02.11.2026)

Aus `docs/arbeitspakete.md` ausgelagert am 02.11.2026, damit Agenten beim Einstieg nur die aktuelle Runde lesen.
Inhalt unverändert bis auf die Überschrift, den ergänzten Absatz „Ergebnis“ und ohne den Abschnitt „Zurückgestellt“ (in
Runde 11 übernommen). Ergebnisse: `docs/analyse/`; Reviews: `docs/reviews/` (Dateien vom 01.11.2026). Nur bei Bedarf lesen.

**Ergebnis:** Sechs Analyse-Spuren, nur Doku: `re-geld` (#375), `re-meldungen` (#376), `re-saisonwechsel` (#377, DOSBox-Beleg
#389), `re-livespiel` (#378, Fable 5.1), `re-rest` (#381) und `inventur` (#395); Integration #380 (Review #390). Plan #366
(Reviews #367, #373), Nachträge #379 (Review #383) und #394 (Review #396); Leitstellen-Zugriff #382 (Reviews #384, #385);
Abschluss mit den P3-Befunden #386, #391–#393, #398, #399, #401–#405. **Funktionskarte Spielcode:** 82 % umgesetzt, 2 %
genähert, 16 % dokumentiert, unbekannt nur 0x575C3 (vorher 10 %). Grundlage für Runde 11: `docs/analyse/inventur.md`
(priorisierte Liste).


Runde 9 (Bereiche abschließen): [archiv/runde-9.md](runde-9.md); ältere Runden in [archiv/](./).
**Ziel der Runde (Wunsch Nutzer 01.11.2026):** die noch unbekannten bzw. genäherten Teile von em.exe nach
[re-abdeckung.md](../re-abdeckung.md) aus dem Code belegen und so beschreiben, dass Runde 11 und `livespiel` sie umsetzen
können. Base-Stand der Karte (`main` 29f3062, nicht committet): Spielcode 82 % umgesetzt, 2 % genähert, 7 %
dokumentiert, 10 % unbekannt.
**Schreibweise in diesem Abschnitt:** Adressen sind Dateioffsets mit Suffix `h` (`3D45Fh` statt Präfix `0x`), damit
die Funktionskarte den Plan nicht als Fundstelle zählt (R10P-01, #368). In `docs/analyse/` gilt die normale Schreibweise.

### Regeln der Runde

- **Nur Doku, keine Code-Änderung** (`src/`, `tests/`, `tools/` bleiben unberührt). Hilfsskripte und Ausgaben
  (Disassembly, CSV, Spielstände) bleiben im Arbeitsbereich bzw. Scratch, nie im Repo; Auszüge aus dem Original
  höchstens als kurze Befehlszeilen mit Adresse.
- **Ein Lauf je Spur, keine Teile, keine Selbstfortsetzung.** Ein PR je Spur (Branch `paket/<spur>`, Base `main`).
  Reicht die Zeit nicht: Draft mit Übergabe und Label `spur-wartet`; die Leitstelle legt nach Entscheidung des Nutzers
  einen weiteren Lauf an.
- **Startaufruf** (R10P-02, #369; geplanter Lauf, wörtlich; Zugriffsabsatz aus `docs/vorlagen/spur-start.md`
  unverändert):

  > Repo kaaasi/eishockey-manager-redone. Arbeite nach `docs/vorlagen/spur-start.md`. Spur: `<name>`. Agent: Claude
  > Code. Unbeaufsichtigt, keine Rückfragen. Runde 10 nach `docs/arbeitspakete.md`, „Regeln der Runde“: ein Lauf, keine
  > Teile, keine Fortsetzung anlegen; Branch `paket/<name>`; statt `tools/pruefen.sh --schnell` die Selbstprüfung nach
  > „Abnahme je Spur“; bei Zeitmangel Draft mit Übergabe und Label `spur-wartet`. Modell der Spur: `<modell>`.
  > `<Zugriffsabsatz „Zugriff zuerst prüfen: …“ aus spur-start.md, Kurzaufruf „Geplanter Lauf mit Teilen“, wörtlich>`

  Es gilt der Hauptablauf von `spur-start.md`; der Abschnitt „Teile und Fortsetzung“ entfällt, Schritt 5 (Rebase vor dem
  Push) bleibt.
- **Dateigrenzen:** je Spur genau eine neue Datei `docs/analyse/<spur>.md` und der eigene Statuspunkt hier.
  `docs/reverse-engineering.md` und `docs/re-abdeckung.md` ändert nur die Leitstelle in der Integration.
- **Inhalt je Fundstelle:** Dateioffset (Schreibweise nach `re-abdeckung.md`, Abschnitt „Fundstellen“, damit die Karte
  sie erkennt), Aufrufer und Aufgerufene, Daten (DGROUP-Variablen, Strukturfelder), Regel in Worten bzw. Pseudocode,
  Beleg (Disassembly, Spielstand, DOSBox-Bild des Nutzers), Abgleich mit dem Remake (Datei, bisherige `[APPROX]`-Stelle)
  und Umsetzungsvorschlag für Runde 11 mit Priorität P1–P3.
- **Abnahme je Spur** (R10P-01, R10P-03): jede Adresse der Spur ist in `docs/analyse/<spur>.md` zugeordnet oder
  begründet offen. Selbstprüfung: `python3 tools/reverse-engineering/funktionskarte.py <em.exe> --csv
  <scratch>/karte.csv` (ohne `--write`); für jede Funktion der Spur nennt die Spalte `fundstellen` die eigene
  Analyse-Datei. Adressen innerhalb einer Funktion (kein Funktionsanfang) und genäherte Funktionen bleiben in der Karte
  unverändert – dort genügt die Fundstelle in der Analyse-Datei. Abschnitt „Kandidaten Runde 11“ mit je einem Eintrag
  pro Umsetzung (Ziel, Dateien, Testidee). Tests entfallen (reiner Doku-PR); `emtool` darf zur Belegerzeugung gebaut
  werden (`tools/claude/setup-workspace.sh`).
- **Leitstelle danach:** eine Integration `integration/runde-10` gegen `main` (alle Spuren, kein Stapel), Karte mit
  `--write` neu, Verweise in `reverse-engineering.md`, ein Claude-Review, Issues für Runde 11 aus den Kandidaten.

### Spur `re-geld` – Geld, Börse, Stadion

- **Adressen:** Stadionbau 3D45Fh; Börse/Konto 57ECFh, 581C2h, 57CFBh, 58409h; Konto Transfermarkt 463CCh; Grundwert
  48E62h; Jugendförderung 39DECh und 3A13Bh (genähert, Restlücke `finanzen-rest`).
- **Abgleich:** `FinanceStocks.cs`, `FinanceYouth.cs`, Stadion- und Kontobuchungen über `GameState.Book`,
  `docs/finanzen-rekonstruktion.md`.
- **Modell:** Claude Opus 5.5. **Status:** Analyse fertig, PR #375 (`docs/analyse/re-geld.md`, Kandidaten G1–G8;
  48E62h ist Taktik, nicht Geld).

### Spur `re-meldungen` – Meldungen, Tagesablauf, Fenster

- **Adressen:** Meldungen 4C246h, 4BA58h (genähert); Infofile 432ECh; Kurve 445E0h (Maßstab `[APPROX]`); Tagesablauf
  07C7Eh, 4F325h; Fehler/Bildlader 25B4Ah, 25764h; Fensterbasis – alle unbekannten Funktionen der Basissegmente 902h und
  99Fh (Base-Stand 16): 0AF8Dh, 0B019h, 0B0E6h, 0B447h, 0B7B5h, 0B830h, 0B8C2h, 0B8FEh, 0BE71h, 0BECDh, 0BEFAh, 0BF64h,
  0C0B8h, 0C5CEh, 0C729h, 0C789h.
- **Abgleich:** `docs/tagesablauf.md`, `GameState.Integration.cs`, Meldungs- und Fensterbausteine in `Widgets.cs`.
- **Modell:** Claude Opus 5.5. **Status:** Analyse fertig, PR #376 (`docs/analyse/re-meldungen.md`, 24 von 24 Funktionen
  mit Fundstelle, Kandidaten K1–K10).

### Spur `re-saisonwechsel` – Manager ohne Liga (#166)

- **Adressen:** Saisonende 54C9Fh (in der Karte „umgesetzt“) mit der Stelle 562C6h (innerhalb der Funktion:
  Saisonwechsel eines Managers ohne Liga, Oberliga-Austausch samt Managervereinen).
- **Beleg im Original:** Spielstand mit `emtool hok patch` (`docs/formate/spielstand-original.md`) aus einer Vorlage im
  Referenz-Repo `original/spielstaende/`; im PR stehen Befehlszeile, Vorlage und eine DOSBox-Anleitung (was spielen, was
  festhalten). Den Spielstand selbst erzeugt die Leitstelle und gibt ihn dem Nutzer; der Nutzer spielt und liefert
  Bilder ins Referenz-Repo. Die Auswertung der Bilder ist ein weiterer Lauf nach Entscheidung des Nutzers.
- **Abgleich:** `SeasonFlow.cs`, `docs/saison.md`, `[APPROX]` aus #307. **Modell:** Claude Opus 5.5. **Status:** Analyse
  fertig (#377, `docs/analyse/re-saisonwechsel.md`): Managervereine verlassen die Oberliga nie (zweiter Tausch hebt den ersten
  auf), Konto 400.000 DM, beide Pfeile. DOSBox-Beleg: `FILE12`–`FILE14` und Bilder im Referenz-Repo
  (kaaasi/eishockey-manager-referenz#7). **Auswertung** (zweiter Lauf, `paket/re-saisonwechsel-2`): alle Aussagen bestätigt
  (Krefelder EV bleibt, ein Nachrücker, Konto 400.000 DM, beide Pfeile), dazu Ewige Tabelle (K4 → P2) und Darstellung im
  Saisonende-Fenster; neu K6 (Umordnung der Vereinssätze je Liga).

### Spur `re-livespiel` – Livespiel Ov 130h (nur Analyse)

- **Adressen** (R10P-04, #371): die unbekannten Funktionen von Ov 130h (Base-Stand 19): 51F3Dh, 51FEAh, 52190h, 5224Eh,
  522ECh, 52382h, 52430h, 52BD6h, 53234h, 53307h, 533D0h, 53453h, 534BCh, 53508h, 5353Ch, 53BF9h, 5434Ch, 56EDDh,
  570E8h. Umgesetzte Funktionen des Overlays (Spielberechnung, Saisonende 54C9Fh) nur lesen; 54C9Fh/562C6h gehören zu
  `re-saisonwechsel`.
- **Ziel:** Grundlage für `livespiel` (Torszenen, Anzeigetafel, Reporter; Funktionskarte Nr. 14/16; letzter Schritt vor
  v1.0): Ablauf, Ereignisse aus der Spielberechnung, Grafik- und Textquellen, Zeitsteuerung. Keine Umsetzung.
- **Modell:** Claude Fable 5.1; blockieren die Schutzmaßnahmen die Analyse, meldet der Lauf das im PR (`spur-wartet`)
  und die Leitstelle startet die Spur nach Freigabe des Nutzers mit Claude Opus 5.5 neu. **Status:** Analyse fertig
  (PR #378, `docs/analyse/re-livespiel.md`): alle 19 Adressen zugeordnet, Kandidaten L1–L8 für Runde 11; keine
  Schutzmaßnahme hat blockiert. Offen: Eingriffsmenü 38D96h (Ov F0h) und `tore\`-Format (Kandidat L7).

### Spur `re-rest` – übrige unbekannte Funktionen (Nachtrag, Go Nutzer 01.11.2026)

- **Adressen:** die nach `re-geld`, `re-meldungen` und `re-saisonwechsel` noch unbekannten Funktionen des Spielcodes
  außer Ov 130h und 575C3h (siehe „Zurückgestellt“, R10R-01 #387); Stand: drei Spurbranches auf 664ff18,
  53 Funktionen, etwa 7 KB:
  Basis 238h: 064C9h; Basis 46Eh: 06C9Ch, 06CDFh, 06FA5h (Talentsuche); Basis 62Ch: 0AD7Ah;
  Ov C0h: 255C0h, 255FFh, 2592Fh, 25A0Eh, 25AF9h, 25B31h; Ov D8h: 28030h, 280DAh, 28111h;
  Ov E0h: 2A5F5h, 2A6A2h, 2B008h; Ov E8h: 3183Ch, 34872h, 34B3Eh, 34F7Dh, 34FF9h;
  Ov F0h: 35CBEh, 35D5Dh, 36146h, 367B1h, 369C7h, 36AAAh, 37A4Ah; Ov F8h: 3CA71h, 3CCEBh; Ov 108h: 40290h;
  Ov 110h: 431CBh, 432C4h, 435CDh, 436DDh, 45CD0h; Ov 118h: 47BCDh, 47CA0h;
  Ov 120h: 4B0F0h, 4B101h, 4B127h, 4B270h, 4B2BAh, 4B9CDh, 4BD7Eh, 4C6B6h, 4CCB4h; Ov 138h: 59D71h;
  Ov 140h: 5A820h, 5AA34h, 5AC9Bh, 5E8B0h.
- **Vorgehen:** je Funktion Aufrufer lesen (meist schon umgesetzt) und die Funktion dem Remake zuordnen; kleine Hilfen
  dürfen zusammengefasst beschrieben werden. Analysen der anderen Spuren (`docs/analyse/`, auch auf den offenen
  Spurbranches) nur lesen. Nach einer Adresse kein Doppelpunkt (sonst erkennt die Karte sie nicht; Hinweis in
  `reviews/2026-11-01-plan-runde-10-nachpruefung.md`, R10R-02 #388).
- **Modell:** Claude Opus 5.5. **Status:** Analyse fertig (PR #381, `docs/analyse/re-rest.md`): 53 von 53 Funktionen
  zugeordnet, Kandidaten K1–K7 (K1 Talentsuche P2); Übergaben an `livespiel`: 3183Ch, 35CBEh, 40290h.

### Spur `inventur` – Annahmen und Umsetzungslücken (Nachtrag, Go Nutzer 01.11.2026)

- **Ziel:** Grundlage für die Planung von Runde 11. Statt einzelner Kandidaten eine vollständige, priorisierte Liste
  aller Stellen, an denen das Remake nicht aus em.exe belegt ist. Stand `main` 2d0b081 (nach Integration #380).
- **Umfang:**
  1. **Alle `[APPROX]`-Stellen in `src/`** (Stand 2d0b081: 382 in 128 Dateien). Je Stelle eine Einordnung: (a) jetzt aus einer
     Analyse bzw. `reverse-engineering.md` belegbar (Fundstelle nennen), (b) bewusste Erweiterung oder Entscheidung des
     Nutzers (Quelle nennen), (c) nur mit DOSBox-Spielstand oder Video belegbar (Vorschlag für den Beleg), (d) unklar.
     Gleichartige Stellen einer Datei dürfen in einer Zeile zusammengefasst werden.
  2. **Funktionen mit Status „dokumentiert“** im Spielcode laut Funktionskarte (Stand 2d0b081: 159, etwa 43 KB) und die
     **genäherten** (7): je Funktion „fehlt im Remake“, „vorhanden ohne `[ORIGINAL]`“ (Datei nennen) oder „nicht nötig“
     (Begründung, z. B. durch .NET ersetzt).
  3. **Kandidaten** aus `docs/analyse/*.md` (G1–G8, K1–K10, K1–K6, L1–L8, K1–K7) den Punkten aus 1 und 2 zuordnen.
  4. **Systemsegmente:** kurz einschätzen, welche Teile für die Originaltreue zählen (Grafik, Timer, FM-Treiber).
- **Ergebnis:** `docs/analyse/inventur.md` mit Übersicht (Zahlen je Gruppe), den Tabellen zu 1 und 2 und einer
  **priorisierten Liste für Runde 11** (P1–P3, je Eintrag Ziel, betroffene Dateien, Beleg, Testidee, Vorschlag für die
  Spur). Keine Code-Änderung; Auszählungen per Skript im Scratch.
- **Abnahme:** jede `[APPROX]`-Stelle und jede Funktion aus 2 ist genau einer Gruppe zugeordnet (Gegenprobe mit
  `grep -rc` bzw. `funktionskarte.py --csv`); Zahlen der Übersicht stimmen mit der Gegenprobe überein.
- **Modell:** Claude Opus 5.5. **Status:** fertig (PR #395, `docs/analyse/inventur.md`; Review #400, Befunde R10N-01 bis
  -05 im Abschluss-PR behoben, R10N-06 für Runde 11).
