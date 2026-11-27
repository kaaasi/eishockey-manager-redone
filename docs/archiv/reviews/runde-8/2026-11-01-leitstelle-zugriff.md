# Review PR #382 – Leitstelle: Lese- und Schreibzugriff auf beide Repos

**Reviewer:** Claude Code (geplanter Lauf, frische Sitzung ohne Implementierungskontext) · **Datum:** 01.11.2026
**Base:** `664ff18f8675c12d63c5f6ad4075309d5975b829` · **Head geprüft:** `307cb763edaf4877325323f14fda2db00b738100`
(Head bei Start, = `origin/main`-Basis; Merge-Base = Base)

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Reiner Doku-PR (2 Dateien, nur `.md`, +7/−1). Die Vorgabe des Nutzers vom 01.11.2026
(„Leitstelle hat immer Lese- und Schreibzugriff auf beide Repos“) steht vollständig und widerspruchsfrei in
`docs/vorlagen/leitstelle-start.md` Schritt 0 (beide Repos, access `push`, Meldung bei Ablehnung) und Schritt 7
(verweist auf Schritt 0, „beide push“; die frühere Angabe „referenz read“ ist entfernt) sowie in `CLAUDE.md`
(„GitHub aus der Sitzung“). Der Zugriffsabsatz der Spuren in `docs/vorlagen/spur-start.md` (Z. 13–17, access `read`)
ist unverändert. Die Befunde unten sind P3 und blockieren nicht. Kein Testnachweis nötig (keine Code-, Test- oder
CI-Änderung); Build/Tests wurden nicht ausgeführt.

## 2. Befunde

- **P3-1 – `CLAUDE.md` Einrichtungsabschnitt nennt weiter nur `read`.**
  [CLAUDE.md Z. 16 und 31](https://github.com/kaaasi/eishockey-manager-redone/blob/307cb763edaf4877325323f14fda2db00b738100/CLAUDE.md#L16-L31)
  („Umgebung einrichten (jede neue Sitzung)“) sagen „per `add_repo` (access `read`) einbinden“. Für Spuren richtig; eine
  Leitstelle, die `CLAUDE.md` von oben abarbeitet, bindet so zuerst nur lesend ein und muss den neuen Punkt in Z. 45–47
  erst finden. Kein echter Widerspruch (Z. 45 ist spezieller, `leitstelle-start.md` Schritt 0 kommt zuerst), aber ein
  Halbsatz „(Leitstelle: `push`, siehe unten)“ an Z. 16/31 würde das Fehlgreifen ausschließen.
- **P3-2 – Geerbtes Schreibrecht für geplante Läufe ohne Nutzungsregel.**
  Folge von Schritt 7 (von der PR-Beschreibung benannt): Spur- und Reviewläufe erben künftig `push` auf
  `eishockey-manager-referenz`. Die Regel „nur lesen“ steht nur in `AGENTS.md` §3 (Tabelle „Referenzbilder … (nur lesen)“);
  `spur-start.md` regelt nur den Rückfallweg (`add_repo` read). Ein Satz in `spur-start.md` oder `AGENTS.md` §3, dass
  Spuren und Reviews ins Referenz-Repo nicht schreiben (Ablage dort nur durch Leitstelle/Nutzer, per PR `daten/…`), würde
  die Absicht ausdrücklich machen. Gegenbeispiel: keins im Repo – nur eine Lücke in der Regelung, kein Fehlverhalten.
- **P3-3 – Satzbau Schritt 7.**
  [leitstelle-start.md Z. 28–29](https://github.com/kaaasi/eishockey-manager-redone/blob/307cb763edaf4877325323f14fda2db00b738100/docs/vorlagen/leitstelle-start.md#L28-L29):
  „Deshalb vor dem ersten `create_trigger` der Sitzung beide Repos nach Schritt 0 eingebunden haben (beide push) und mit
  … prüfen“ – verständlich, aber holprig; z. B. „Deshalb müssen vor dem ersten `create_trigger` beide Repos nach
  Schritt 0 eingebunden sein (beide push); mit … prüfen.“

Geprüft ohne Befund: `CLAUDE.md` Z. 48–50 (Geplante Läufe anlegen) passt zu Schritt 7; Branch-Konvention `daten/…`
existiert im Referenz-Repo (`daten/originaldateien`, `daten/spielstaende*`); `git diff --check` sauber; keine weiteren
Fundstellen mit `add_repo`/`access` außerhalb von Archiv und Reviews.

## 3. Ausgeführte Prüfungen

| Prüfung | Ergebnis |
|---|---|
| `git fetch origin pull/382/head`, `git rev-parse HEAD` | `307cb763…`, `git status --porcelain` leer |
| `git merge-base HEAD origin/main` | `664ff18…` = Base = aktueller `main` |
| `git diff --stat 664ff18..HEAD`, `--name-only` | `CLAUDE.md`, `docs/vorlagen/leitstelle-start.md`; nur `.md` |
| `git diff --check` | sauber |
| `grep -rn "add_repo\|access .read\|nur lesen"` (ohne Archiv/Reviews) | Fundstellen s. o. |
| CI am Head (Check-Runs) | `build` success, `publish` skipped |
| `tools/pruefen.sh` | **nicht ausgeführt** – reiner Doku-PR, keine Code-/Test-/CI-Dateien |

Akzeptierte `[APPROX]`: keine (Doku).
Annahmen: Dass `add_repo` den Wert `push` für `access` annimmt, konnte nicht geprüft werden (Werkzeug in diesem Lauf
nicht verfügbar, auch nicht per `ToolSearch`); übernommen aus der Vorgabe des Nutzers und der PR-Beschreibung.
