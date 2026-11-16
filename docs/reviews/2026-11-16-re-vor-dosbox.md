# Review: PR #891 – Spur `re-vor-dosbox` (Erstreview)

Base `40f67fcabb0a7b0649f3771d9f00a4ce8d186bea` · Head `b5217751898596d36ec91489112803a13df84b47` · Reviewer: Claude Opus 5.5
(geplanter Lauf, frische Sitzung ohne Implementierungskontext) · 16.11.2026

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Die Stichprobe gegen em.exe (sieben V, zwei X, A002 als K, A059/A480 als E) trägt Fundstelle und
Lesart; alle Gegenbeispiele mit FILE13/14, FILE20/21 und FILE71–74 sind nachgerechnet und stimmen byte-genau. Der Diff bleibt
in der Abgrenzung (nur Doku, genau 23 CSV-Zeilen, kein Generatorlauf). Offen bleiben nur die erwarteten Nutzerentscheidungen
(A059, A480) und drei P3-Präzisierungen für die umsetzenden Spuren, die keine Klasse ändern.

## 2. Befunde

Kein P0–P2.

**P3-1 (Präzisierung A093, für `kaderplatz-werte`)** –
[re-vor-dosbox.md:21](https://github.com/kaaasi/eishockey-manager-redone/blob/b5217751898596d36ec91489112803a13df84b47/docs/analyse/re-vor-dosbox.md#L21).
Datei 0x09A72–0x09A8A: `al = [+0x02]; ah = 0; dec ax; cmp ax, Reihe` → bei +0x02 = 0 ist ax = 0xFFFF ≠ Reihe, der Abzug gilt
also auch für jeden Spieler **ohne** eingespielte Reihe (in FILE71/72 für alle). Verglichen mit 12 wird `si` =
[+0x11] · [bp−0xA] / 100 (Datei 0x09A54–0x09A63), also der schon verrechnete Wert, nicht die rohe Technik. Die Formel der
Tabelle deckt das, die CSV-Abweichung („außerhalb der eingespielten Reihe“) sagt es nicht ausdrücklich – in der Spur als
Testfall mit +0x02 = 0 aufnehmen. Beleg: `disasm.py em.exe 0x09A40 0x09A95`.

**P3-2 (Präzisierung A206)** –
[re-vor-dosbox.md:23](https://github.com/kaaasi/eishockey-manager-redone/blob/b5217751898596d36ec91489112803a13df84b47/docs/analyse/re-vor-dosbox.md#L23).
Die Schleife um Datei 0x5613E läuft nur für Liga 0–2 (`cmp [bp−1], 3` Datei 0x563A4), die Rücksetzung 0x565E0 nur für Liga
3–7 (Start 3 Datei 0x565D3, `cmp 8` Datei 0x565E8). „je Liga auf 1 … erst danach auf 0“ stimmt so nicht; für die Managerligen
0–2 bleibt der Rest im Fenster 0x26F20 dennoch 100 − 100 · 1 / Spieltage (98), die Klasse V ist richtig. Die umsetzende Spur
sollte den Wert von DGROUP 0x7A9 + Liga 0–2 nach dem Fenster selbst nachlesen. Beleg: `disasm.py em.exe 0x56130 0x56150`,
`0x563A0 0x563B4`, `0x565C0 0x565F0`.

**P3-3 Verdacht (A059, erwartet offen)** – Lesart bestätigt (siehe 3.), aber nur über direkte Adressierung: Byte-Suche nach
`42 97` findet nur die drei Leser 0x55D33/48/5A, nach `41 97` die Schreiber 0x29907, 0x3428E, 0x34335, 0x34682 (= 1, Index 0),
0x34D8F (Index 0, nur DGROUP 0xDD39 = 0), 0x4374D und die Leser 0x55C89, 0x5D353. Ein Schreiber über Fernzeiger auf den
Managersatz (DGROUP 0x965B + 0xE7) ist so nicht auszuschließen; die Probe der Spur (79 Stände, immer 0x1E) stützt die Lesart
empirisch. Folgebefund (Titelbit 0x20, Zähler) ist wie angegeben Verdacht.

**Erwartet offen, bestätigt:** A059 und A480 als E mit N2 = B sind Vorschläge, die Entscheidung des Nutzers steht aus (CSV-Feld
„Beleg“ sagt das). Folgebefund zu A059 bleibt Verdacht.

## 3. Ausgeführte Prüfungen

Checkout: `git rev-parse HEAD` = `b5217751…`, `git status --porcelain` leer. SDK 10.0.402. Originaldateien und Spielstände aus
`kaaasi/eishockey-manager-referenz` (`11c3095`, `ls-remote` erreichbar). Disassembly mit `tools/reverse-engineering/disasm.py`
(Ausgaben nur im Scratch).

**Abgrenzung:** `git diff --stat 40f67fc b521775` → nur `docs/analyse/re-vor-dosbox.md`, `restliste-v1-klassen.csv`
(46 Zeilen = 23 −/23 +, genau die 22 „RE zuerst“ und A093), `restliste-v1.md`, `arbeitspakete.md`; `-- src tests tools` leer.
CSV: 690 Zeilen, alle 8 Felder; Klassen danach D 22 (genau die Sammelliste in `restliste-v1.md`), V 100, X 87, K 183, E 297.
Der erzeugte Block ist unverändert (kein `markerliste.py --write`), wie in `restliste-v1.md` vermerkt.

**Gegenbeispiele (eigener Lauf `emtool hok dump`, Bytes ausgewertet):**

| Probe | Ergebnis |
|---|---|
| A080 FILE71/72/73 `far:9657` EC HEDOS MÜNCHEN +0x18–0x1D | `44 46 45 45 37 30` → `45 46 45 46 37 30` → `4D 46 4B 45 3C 30` (= 68 70 69 69 55 48 / 69 70 69 70 55 48 / 77 70 75 69 60 48) ✓ |
| A093 FILE71/72 `5DC3` +0x02 | beide Manager alle 0 ✓ |
| A093 FILE73 | Berlin Plätze 1, 4, 6, 19 (+0x02 = 1, 2, 3, 2 = Reihe), München 14 (1) ✓ |
| A093 FILE74 (Zweig von FILE71, nicht Folge von FILE73) | München 2, 14, 18 (1, 1, 3 = Reihe), Berlin keiner ✓ |
| A698 FILE13 → FILE14 `8257` | TEV MIESBACH Platzfeld +0x31 = 0, Punkte +0x4C: 0 → 15 ✓; KREFELDER EV Platzfeld 11, 1 Punkt (12 − 11, passt zu `file14_3`) ✓ |
| A671 FILE20/21 `far:9657` +0x16 | FILE20 Verein 7 DEUTSCHLAND = 1, FILE21 Verein 5 = 1; Konto Satz 0 FILE21 −4.590.982 Drittel = −1.530.327 DM ✓; Schreiber 0x562D8/0x562DE = 0x124F80 Drittel = 400.000 DM ✓ |

**Stichprobe gegen em.exe (Fundstelle und Lesart selbst nachvollzogen):**

- **A093 (V, Zufallsverbrauch):** 0x544C0 (Aufrufer 0x36B5D), Schleife Datei 0x548A8–0x549CB über `[bp−3] < [bp−2]`
  (Kaderzahl aus 0x4BE17) je Managerverein: `push 4; push 0; call 0xC037` (Datei 0x548CC–0x548D3) = random(0, 4) **je
  Kaderplatz, unabhängig vom Ergebnis**; bei 0: +0x02 = +0x15 & 0x7F (0x548DE–0x548E7). Leser 0x09A65 (0xED8 < 2, [bp+0xC] ≠ 0). ✓
- **A080 (V):** 0x952C schreibt Vereinsfelder 0x18/0x1A/0x1C (+1 je Teil) = Summe / 3 (Datei 0x096FD–0x09779); Aufruf im
  Tageslauf Datei 0x54FBF; 0x977F liest 0x18–0x1D (0x097FD, 0x0981E, 0x0983D) nach der Prüfung 0x52B91 (0x097E6). Zuordnung
  zu `teamwert-aufstellung` statt Folge des Laders ist schlüssig. ✓
- **A206 (V):** Rest = 100 − 100 · [0x7A9 + Liga] / [0x7B6 + Liga] (Datei 0x4A851–0x4A8A6, Langarithmetik 0x21CA/0x227C) ✓; Rest siehe P3-2.
- **A698 (V):** Punkte = di + 12 − [0x8288 + 0x50 · Verein] (Datei 0x566E4–0x566FA), Zuschlag 3 bei Platzfeld 0 (0x56783–0x56793) ✓, Probe oben.
- **A101 (V):** Bit 7 Vereinsfeld 0x34 (Datei 0x390CB, 0x390FA); random(0, 10), Treffer bei > 6 (Datei 0x39429–0x3943A) ✓.
- **A671 (V):** Probe und Schreiber oben ✓.
- **A038 (X):** Zeigertabelle DGROUP 0x10A1 (Datei 0x16DD0 + 0x10A1) ausgelesen: Firma 1 „Computer Center Holstein“ + 4 Zeilen,
  Firma 9 „KrOn Software“ + 4 Zeilen; Farbe 7 − 6 · (Zeile ≠ 0) (Datei 0x2BD8F–0x2BDA6) – gleich `FinanceSponsors.cs:40–53` ✓.
- **A694 (X):** Sperre 0x35BFD (+0x0A = 0, +0x09 |= 1, +0x0D), Managerzug 0x07612–0x0761C, Zählung nur +0x15 ≠ 0 und +0x0A ≠ 0
  (0x38978–0x38986) ✓.
- **A801 (X, Zusatz):** 0x563AF ruft 0x5667A direkt nach der Ligaschleife, 0x5667A ruft als erstes 0x4C747 (0x56682) ✓.
- **A002 (K):** einziger Aufrufer von 0x431CB ist Datei 0x4391F (Funktionskopf „Aufrufer 0x437d5“); Treiber: `cmp al, 0x2F` →
  0x15F08 (Datei 0x15DB0–0x15DB4) setzt Kennzeichen [0x1E1] = 0; Rohbytes `E6 59` nur bei 0x1427F, 0x14350, 0x14359 (Treiber) –
  kein Leser im Spielcode (`--xref 0x59E6` leer). K ist vertretbar (ohne Spielwert); `AudioMixer.cs:39` `loop: true` ✓.
- **A059 (E-Vorschlag):** Leser 0x55D31 (≠ 0), 0x55D46 (≠ 0x1E), 0x55D58; Schreiber 0x29905, 0x3428C, 0x4374B setzen 0x1E,
  0x34D8D nur Index 0 bei DGROUP 0xDD39 = 0 ✓ (Grenze P3-3).
- **A480 (E-Vorschlag):** gültig = (Kopf und Verein beide gesetzt oder beide 0xFF), ungültig nur zusätzlich „kein Name, aber
  beides“ (Datei 0x2961C–0x29682); 0x296EC schreibt +0x16 bei Verein 0xFF → Offset 255 · 0x3D + 0x16 = 15 577 ✓.
- Remake-Stellen geprüft: `MatchEngine.Live.cs:266–270`, `:414`, `MatchEngine.Managed.cs:189`, `Transfer.Contract.cs:144`
  (`SeasonMatchDays + 1`), `StatisticsHistory.cs:152–155`, `Player.Competitions.cs:23`, `AudioMixer.cs:39` – Beschreibung zutreffend.

**`tools/pruefen.sh --schnell`** (reine Analyse, laut Auftrag):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3373 | 0 | 0 |

Commit `b5217751898596d36ec91489112803a13df84b47`, SDK 10.0.402, Ergebnis bestanden. Gegenproben durch Mutation entfallen
(keine neuen Tests).

**Akzeptierte `[APPROX]`:** keine neuen (kein Code). **Annahmen (nicht geprüft):** A421 Aufstiegsgruppe nach dem letzten
Termin, A455 Inhalt von 0x48E28 für Manager 5 (beide in „Grenzen“ der Spur offen); A492, A494, A497, A500, A553, A628, A670,
A672, A425 nicht selbst nachgelesen (Stichprobe).
