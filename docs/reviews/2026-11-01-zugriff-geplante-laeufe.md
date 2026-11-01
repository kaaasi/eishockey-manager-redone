# Review PR #296 – Zugriff geplanter Läufe auf das Referenz-Repo; #289/#290

- **PR:** #296 · **Base:** `48c027e161a740904eae69aba53323d96bd5f258` · **Head:** `6b430f3e0a1f9ff73e5b293a683509c51e0c135b`
- **Reviewer:** Claude Code (geplanter Lauf, eigene Sitzung; der Head-Commit stammt aus einer anderen Sitzung)
- **Art:** nur Codelesen (reiner Doku-PR, Auftrag), kein Build, keine Tests

## 1. Merge-Empfehlung

**Ja für genau diesen Head (nur Codelesen).** R9T-01 und R9T-02 sind umgesetzt. Der Zugriffsabsatz ist ohne Chatverlauf
ausführbar und führt bei fehlendem Zugriff zu „blockiert“, nicht zu roten Tests. Neu sind vier P3 (Z96-01 bis -04). Sie
blockieren den Merge nicht. Z96-01 und Z96-02 sollten vor dem nächsten geplanten Spurlauf nachgezogen werden.

**Beleg aus diesem Lauf (zugleich Beleg für #296):**

- `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD` gelang **sofort**:
  `0bc38fba0e3e7d0c41b122e8a3087a933a88ea13 HEAD`. Der Lauf hatte beide Repos schon im Sitzungsumfang (von der anlegenden
  Sitzung geerbt). Ein `add_repo` war nicht nötig.
- `add_repo` stand in diesem Lauf **nicht** bereit. `ToolSearch` mit `select:mcp__claude-code-remote__add_repo` ergab
  „No matching deferred tools found“. Eine Stichwortsuche `add_repo list_repos repository` fand nur `mcp__github__*`-Werkzeuge.
  Die Ausweichstufe des Zugriffsabsatzes ist also hier nicht ausführbar. Tragend ist allein Schritt 7 der Leitstelle. Siehe Z96-01.

## 2. Befunde

### Gegenbeispiele erneut ausgeführt

- **R9T-01 – erledigt.** [`arbeitspakete.md` Z. 50–53](https://github.com/kaaasi/eishockey-manager-redone/blob/6b430f3e0a1f9ff73e5b293a683509c51e0c135b/docs/arbeitspakete.md#L50-L53)
  erlaubt nur noch das An- und Abmelden in `StartNextSeason` und den Moduswechsel. Es steht dort „**keine** Filter in
  `MatchOn`/`ProcessDay`“. `grep -n 'MatchOn\|ProcessDay\|Filter'` findet am Head in `arbeitspakete.md` nur noch diese
  Verbotszeile. Der Gedankenablauf „Schalter am Tag *d* aus → Pokal ohne Termine, `Winner` null“ ist damit nicht mehr zulässig.
  Gegengeprüft am Head:
  - „vor `StartSeason`“ passt zu `SeasonFlow.cs` Z. 398–399 (`foreach (var competition in Competitions) competition.StartSeason(this)`).
    Ein dort neu angemeldeter Wettbewerb wird mit ausgelost.
  - „Reihenfolge DEB-Pokal → Europapokal → WM“ entspricht `Cups.GameState.cs` Z. 35–40 (`InitializeCompetitions`).
  - Hinweis für die Spur (kein Befund): Beim Wiederanmelden muss die Position in der Liste stimmen (`Insert`, nicht `Add`).
    Der Europapokal setzt außerdem `ClubCompetitions` voraus (Z. 37).
- **R9T-02 – erledigt, mit Rest Z96-03.**
  - Punkt 1: [`spur-start.md` Z. 47–49](https://github.com/kaaasi/eishockey-manager-redone/blob/6b430f3e0a1f9ff73e5b293a683509c51e0c135b/docs/vorlagen/spur-start.md#L47-L49)
    lautet jetzt „nichts am Branch ändern, nur Label `spur-wartet` und Hinweiskommentar – ohne Übergabe und ohne `Bearbeiter: frei`
    – keine Fortsetzung“. Das entspricht dem Vorschlag. Der Verweis auf Schritt 4 ist entfallen.
  - Punkt 2: Das Kriterium lautet „**nach** dessen letztem Bearbeiter- oder Übernahme-Eintrag“. Erledigt.
  - Punkt 3: Schritt 3 (Z. 27–29) nimmt nur die PR-Prüfung und die Wahl von Branch und Base aus. Der Draft-PR mit `Bearbeiter:`
    bleibt Pflicht. Schritt 5 (Z. 32–33) verweist für das Rebase-Ziel auf Abschnitt Schritt 2. Abschnitt Schritt 2 (Z. 51)
    verweist zurück auf „Hauptablauf Schritt 5“. Der Verweis ist eindeutig und nicht mehr zirkulär im Wortlaut.

### Besonders geprüft

**Zugriffsabsatz ohne Chatverlauf ausführbar?** Ja. Er nennt den Befehl, den Werkzeugnamen, das Repo, das Zugriffsrecht und
das Abbruchkriterium wörtlich. Die Ausweichstufe (`add_repo` per `ToolSearch`) war in diesem Lauf nicht vorhanden (siehe 1.).
Dann greift der letzte Halbsatz („bleibt es unerreichbar, sofort blockiert melden …, keine Code-Arbeit“). Das ist sicher.

**Blockiert statt rote Tests?** Ja, soweit die Prüfung vor der Einrichtung läuft. Ohne `gamedata/kb` würde `tools/pruefen.sh`
den Lauf mit Originaldaten auslassen (Z. 42–54) und nicht rot werden. Die eigentliche Gefahr wäre also ein *grüner* Teil ohne
Originaldaten und ohne Referenzbilder. Der Absatz verhindert das, weil er vor jeder Code-Arbeit abbricht. Offen ist nur, wohin
die Meldung geht (Z96-02).

**Widerspruchsfreiheit `spur-start.md` Schritt 3/5, „Teile und Fortsetzung“, `leitstelle-start.md` Schritt 7:** im Kern
widerspruchsfrei. Reste stehen in Z96-03 und Z96-04.

### Z96-01 (P3) – Ausweichstufe `add_repo` per `ToolSearch` im Lauf nicht vorhanden

Stelle: [`spur-start.md` Z. 13–15](https://github.com/kaaasi/eishockey-manager-redone/blob/6b430f3e0a1f9ff73e5b293a683509c51e0c135b/docs/vorlagen/spur-start.md#L13-L15),
[`CLAUDE.md` Z. 45–47](https://github.com/kaaasi/eishockey-manager-redone/blob/6b430f3e0a1f9ff73e5b293a683509c51e0c135b/CLAUDE.md#L45-L47).

Der Leitstellenkommentar in #291 nennt als Ursache: „`add_repo` ist in geplanten Läufen ein nachzuladendes Werkzeug“. Das ist
in diesem Lauf nicht bestätigt.

Gegenbeispiel: In einem geplanten Lauf ergibt `ToolSearch` mit `select:mcp__claude-code-remote__add_repo` „No matching
deferred tools found“. Das ist reproduziert in diesem Lauf, siehe 1.

Folge: Der Ablauf bleibt sicher (→ blockiert). Die Doku weckt aber die Erwartung, der Lauf könne sich selbst helfen.
`leitstelle-start.md` Schritt 7 sagt richtig „nicht verlässlich“.

**Vorschlag:** In `CLAUDE.md` Z. 46–47 „In einem Lauf `add_repo` bei Bedarf erst per `ToolSearch` laden“ ersetzen durch
„im Lauf meist nicht verfügbar; maßgeblich ist das Einbinden durch die anlegende Sitzung“. Im Zugriffsabsatz die Fehlermeldung
von `ToolSearch` ausdrücklich in die Blockademeldung aufnehmen.

### Z96-02 (P3) – Ort der Blockademeldung beim Start ungeregelt

Stelle: [`spur-start.md` Z. 15](https://github.com/kaaasi/eishockey-manager-redone/blob/6b430f3e0a1f9ff73e5b293a683509c51e0c135b/docs/vorlagen/spur-start.md#L15)
mit [Z. 56–58](https://github.com/kaaasi/eishockey-manager-redone/blob/6b430f3e0a1f9ff73e5b293a683509c51e0c135b/docs/vorlagen/spur-start.md#L56-L58)
und [`leitstelle-start.md` Z. 24–29](https://github.com/kaaasi/eishockey-manager-redone/blob/6b430f3e0a1f9ff73e5b293a683509c51e0c135b/docs/vorlagen/leitstelle-start.md#L24-L29).

„Sofort blockiert melden“ sagt nicht, wo. Abschnitt Schritt 4 setzt einen Draft-PR des Teils voraus. Beim Start von Teil 1
gibt es noch keinen. Der Zugriffsabsatz steht vor Schritt 3 und verbietet Code-Arbeit. Ein PR ohne Commit wird von GitHub
abgelehnt.

Gegenbeispiel:

1. Die Leitstelle legt den Lauf für Teil 1 ohne Referenz-Repo im Sitzungsumfang an.
2. `ls-remote` scheitert, `add_repo` fehlt.
3. Der Agent meldet „blockiert“ nur in der Antwort des unbeaufsichtigten Laufs.
4. Kein PR und kein Label `spur-wartet` entstehen. Die Leitstelle sieht nur PRs mit `spur-wartet` durch (Abschnitt Schritt 6).
   Die Spur gilt weiter als „offen“.

Nebenbei: Auch #291 und #295 tragen am Prüfzeitpunkt kein Label `spur-wartet`, obwohl #291 „blockiert“ meldet.

**Vorschlag:** Im Zugriffsabsatz ergänzen: „Meldung: Benachrichtigung an den Nutzer; gibt es einen PR der Spur, dort
Kommentar und Label `spur-wartet`; sonst keinen PR anlegen.“

### Z96-03 (P3) – Rest R9T-02: Hauptablauf Schritt 7 verlangt die Übergabe auch im Wartefall

Stelle: [`spur-start.md` Z. 36–38](https://github.com/kaaasi/eishockey-manager-redone/blob/6b430f3e0a1f9ff73e5b293a683509c51e0c135b/docs/vorlagen/spur-start.md#L36-L38)
gegen Z. 47–49. Ebenso `AGENTS.md` §6 „Übergabe immer“.

Schritt 7 gilt „immer zum Schluss“ und schreibt `Bearbeiter: frei` vor. Abschnitt Schritt 1 verbietet genau das, nimmt Schritt 7
aber nicht aus. Außerdem sagt Abschnitt Schritt 1 nicht, ob Lauf B danach endet. B könnte einen weiteren Teil beginnen und auf
den Branch von A stapeln.

Gegenbeispiel:

1. Lauf B erkennt Lauf A als Bearbeiter von `paket/<spur>-2`.
2. B setzt Label und Hinweis und beginnt Teil 3 von `paket/<spur>-2`.
3. Zum Schluss schreibt B nach Schritt 7 eine Übergabe mit `Bearbeiter: frei`.

**Vorschlag:** In Abschnitt Schritt 1 anhängen: „… und den Lauf beenden; der Hinweiskommentar ersetzt die Übergabe
(Hauptablauf Schritt 7 entfällt).“

### Z96-04 (P3) – Kleinere Unschärfen

- [`spur-start.md` Z. 17](https://github.com/kaaasi/eishockey-manager-redone/blob/6b430f3e0a1f9ff73e5b293a683509c51e0c135b/docs/vorlagen/spur-start.md#L17)
  verlangt den Absatz „in **jedem** Startaufruf … (… Review) wörtlich“.
  [`review-anfrage.md` Z. 14–15](https://github.com/kaaasi/eishockey-manager-redone/blob/6b430f3e0a1f9ff73e5b293a683509c51e0c135b/docs/vorlagen/review-anfrage.md#L14-L15)
  verlangt ihn nur, „wenn Originaldaten, Spielstände oder Bilder nötig sind“. „Keine Code-Arbeit“ passt für Reviews nicht
  wörtlich. Dieser Startaufruf hat den Absatz zudem abgewandelt. Vorschlag: eine Regel, z. B. „bei Reviews nur, wenn
  Daten nötig; ‚keine Code-Arbeit‘ gilt dort als ‚kein Review‘“.
- [`leitstelle-start.md` Z. 28–29](https://github.com/kaaasi/eishockey-manager-redone/blob/6b430f3e0a1f9ff73e5b293a683509c51e0c135b/docs/vorlagen/leitstelle-start.md#L28-L29):
  „Modell … mit `update_trigger` setzen … bzw. laut Plan“. Das Werkzeug erlaubt eine Modelländerung nur auf ausdrücklichen
  Wunsch eines Menschen. Vorschlag: „nur auf ausdrücklichen Wunsch des Nutzers (z. B. vom Nutzer bestätigter Plan)“.

## 3. Ausgeführte Prüfungen

- Zugriff: `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD` → erreichbar ohne `add_repo`.
  `ToolSearch` `select:mcp__claude-code-remote__add_repo` → nicht vorhanden. Code-Repo: Zugriff über den vorhandenen Klon
  und `gh api` (REST).
- `git fetch` von Base und Head. `git merge-base origin/main 6b430f3e` = Base. `git diff --stat` (5 Dateien, +31/−10). Ganzes
  Delta gelesen.
- `spur-start.md` und `leitstelle-start.md` am Head ganz gelesen. `arbeitspakete.md` Z. 45–55 und 100–125 gelesen.
- Code zu R9T-01: `SeasonFlow.cs` Z. 365–401 und `Cups.GameState.cs` Z. 1–62. `git grep` nach `Competitions.Add`.
- `tools/claude/fetch-reference.sh` (Klonweg) und `tools/pruefen.sh` Z. 42–68 (Verhalten ohne `gamedata/`).
- PR #291, #295 und #296 per REST (Status, Labels). Übergabe und Leitstellenkommentar in #291.
- Kein Build und keine Tests (Doku-PR, Auftrag). Damit gibt es keinen Testnachweis nach `zusammenarbeit.md` §6 Nr. 4.
- **Akzeptierte `[APPROX]`:** keine neuen. Unverändert gilt „Wirkung ab dem nächsten Saisonstart“ (`arbeitspakete.md` Z. 118).
