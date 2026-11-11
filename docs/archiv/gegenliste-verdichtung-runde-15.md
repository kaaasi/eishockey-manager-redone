# Gegenliste zur Verdichtung der Prozessdoku (Spur `prozess-kern`, Runde 15)

Prüfhilfe für das Review von PR #722; nach dem Merge entbehrlich (die Leitstelle verschiebt sie mit der Runde ins Archiv).
Alte Fassung = `main` @ `7ea49a5`. Jede Regel der alten Fassung → Fundstelle neu (Datei, Abschnitt/Punkt). „Wortlaut“ = Text
unverändert übernommen, „gekürzt“ = gleiche Regel, kürzere Formulierung. Neu sind nur die Klärungen aus #712 (letzter Abschnitt).
**Archiviert beim Abschluss Runde 15 (#724):** Zeichenzahl neu gemessen 43.092 statt 43.057 (N1, −24 % bleibt); nicht verzeichnet
war der Eintrag „DOSBox-Aufnahme“ in `AGENTS.md` §8, Zeile „Kurzaufrufe“ (N3, nur Auffindbarkeit).
Abkürzungen: A = `AGENTS.md`, Z = `docs/prozess/zusammenarbeit.md`, H = `docs/prozess/anhang.md`, S = `spur-start.md`,
R = `review-anfrage.md`, L = `leitstelle-start.md`, V = `docs/handoffs/VORLAGE.md`, C = `CLAUDE.md`.

## Zeilenzählung

| Datei | vorher | nachher |
|---|---:|---:|
| `AGENTS.md` | 163 | 144 |
| `docs/prozess/zusammenarbeit.md` | 182 | 128 |
| `docs/vorlagen/spur-start.md` | 95 | 84 |
| `docs/vorlagen/review-anfrage.md` | 93 | 45 |
| `docs/vorlagen/leitstelle-start.md` | 53 | 38 |
| `docs/handoffs/VORLAGE.md` | 48 | 46 |
| `CLAUDE.md` | 73 | 58 |
| `docs/prozess/anhang.md` (neu, nur bei Bedarf) | – | 109 |
| **Pflichtlektüre Spur** (A + Z §2–4 + S + V) | 361 | 305 |
| **Pflichtlektüre Review** (A + Z §5–6 + R) | 334 | 261 |
| **Leitstelle** (A + Z + L + H; dazu S, R, V) | 398 (+236) | 419 (+175) |

Unverändert und nicht Pflichtlektüre: `umgebung.md` (69), `vorlagen/dosbox-aufnahme.md` (131). Zeichen der sieben Kerndateien:
56.823 → 43.057 (−24 %).

## AGENTS.md

| Alt | Regel | Neu |
|---|---|---|
| Kopf | gilt für alle Agenten und Menschen, kein Chatverlauf, Werkzeughinweise getrennt, kein Widerspruch; Verweise Zusammenarbeit, Arbeitspakete | A Kopf (gekürzt, + Verweis H) |
| §1 | Ziel, Sprache | A §1 (gekürzt) |
| §2 Nr. 1–7 | harte Regeln | A §2 Nr. 1–7 (gekürzt; Nr. 3 ohne „siehe Zusammenarbeit“) |
| §3 Tabelle | SDK, NuGet, Originaldaten, Referenzbilder | A §3 Tabelle (gekürzt) |
| §3 | „Prüfen, ob alles da ist“ | A §3 |
| §3 „Eingeschränkte Arbeitsumgebungen“ | `umgebung.md` lesen; blockiert ≠ Ergebnis; Proxy, Sandbox, Freigaben nicht umgehen | A §3 (Absatz statt Unterabschnitt) |
| §4 | Schichten, Projekte, `partial`, eigene Datei je Paket | A §4 (gekürzt) |
| §4 Architekturregeln | `Continue()`, `Book()`, Bildschirme ohne Zustandsänderung inkl. `OnMainMenu`/E4 = B, `MenuEntry`, Szenarien | A §4 „Verbindliche Architekturregeln“ (Wortlaut bzw. gekürzt) |
| §4 Originaltreue 1–5 | Vorlage, Vermessen, Farben, Markieren, Prüfen | A §4 „Originaltreue“ (1)–(5) als Absatz |
| §5 | `pruefen.sh`, `--schnell`, drei Läufe nur bei …, Selbsttest, gezielte Tests, `[GameDataFact]`, Zeitleiste, Mehrsaisonprobe, CI ohne Daten | A §5 (gekürzt) |
| §6 | Branchnamen, Rebase + Prüfung vor Push, Commits/Trailer, kleine PRs/Vorlage/REST, Status, Übergabe, Merge nur Leitstelle/Nutzer | A §6 (gekürzt) |
| §7 | knapp antworten, Kurzaufrufe, gezielt lesen, lange Inhalte ins Repo, Tests nach Bedarf | A §7 (gekürzt; Anhang bei Bedarf ergänzt) |
| §8 | Wo was steht | A §8 (+ Zeile Anhang; GitHub-Free-Grenzen → H) |

## docs/prozess/zusammenarbeit.md

| Alt | Regel | Neu |
|---|---|---|
| Kopf | Ziel: Übernahme nur anhand GitHub | Z Kopf (+ Verweis H) |
| §1 Tabelle | Rollen, Aufgaben, Verbote | Z §1 (Spalte „Wer“ in Spalte „Rolle“) |
| §1 | aktuelle Leitstelle, je Runde frische Sitzung, Standard-Reviewer, Wechsel per PR | Z §1 |
| §2 Liste | Übernahmefähigkeit | Z §2 (als Satz) |
| §2 Nr. 1 | lesen | Z §2 → S Ablauf 1 |
| §2 Nr. 2 | Bearbeiter ohne Übergabe → nicht anfangen, Nutzer fragen | Z §2, S Ablauf 3 |
| §2 Nr. 3 | einrichten, `git log -1` vergleichen, `--schnell`, erst dann weiter | Z §2 |
| §2 Nr. 4 | neuer Branch, Draft-PR (Titel, Bearbeiter, Base) = Sperre | Z §2, S Ablauf 3 |
| §2 Nr. 5 | fertig: Vorlage, Ready, Leitstelle informieren | Z §2, S Ablauf 6 |
| §3 | Übergabe jeder Sitzung, Anlässe, vorher bei absehbarem Abbruch, Inhalt (5 Punkte) | Z §3 Abs. 1 (Inhalt als Satz), V |
| §3 | Länge 15 Zeilen + Tabelle, verlinken | Z §3, V |
| §3 | ein PR je Spur (Runde 15) | Z §3 Abs. 2; Klammer „bis Runde 14 gestapelt …“ entfällt (historisch, R719-02) |
| §3 | Kommentar statt Datei; ohne PR `docs/handoffs/…` | Z §3 Abs. 2 |
| §3 | Abbruch ohne Kommentar → letzter Stand, Übernahmekommentar | Z §3 Abs. 2, V |
| §4 | ein Branch – ein Bearbeiter, `paket/<spur>-<teil>` bei Gleichzeitigkeit, Fortsetzungen nacheinander | Z §4 |
| §4 | kein `--force` fremd, `--force-with-lease` eigen; Dateigrenzen; Statusabschnitt | Z §4 |
| §5 | Kurzaufruf, Reviewer ≠ Umsetzer, Ablage, Nachprüfung bei geändertem Head | Z §5 Spiegelstriche 1–4 (Ausnahme ergänzt: #712/N2) |
| §5 Kette 1–3 | Erstreview, Fixlauf, Nachprüfung | Z §5 Punkte 1–3 |
| §5 Kette 4 | nur P2/P3 → Delta-Prüfung Leitstelle, Vermerk | Z §5 Punkt 4 (+ Rebase-Fall aus §6 Nr. 1, #712/N2) |
| §5 Kette 5–7 | P3 als Issue, Gesamtreview + Abschlussprüfung, Befund nur mit Beleg, `beleg-strittig` | Z §5 Punkte 5–7 |
| §5 Kette 8 | Fixlauf legt Nachprüfung an (Modell, 5 Min., `list_triggers`), Labels setzen/entfernen, Netz, Leitstelle wartet/merged/legt Fixläufe an, Freigabe abgelehnt | Z §5 Punkt 8 mit Labeltabelle; Netz → H D |
| §5 Kette 9 | Länge 120 Zeilen | Z §5 Punkt 9 |
| §5 | Modellwahl | Z §5 „Modellwahl“ |
| §5 | Befunde als Issues | Z §5 letzter Spiegelstrich |
| §6 Kopf | GitHub Free erzwingt nichts | Z §6 Kopf (Details H B) |
| §6 Nr. 1–8 | Merge-Prüfung | Z §6 Nr. 1–8 (gekürzt; Nr. 2 nennt das Vorgehen aus L Schritt 2) |
| §7 Nr. 1–5 + Schluss | Vertretung | H A |
| §8 Tabelle + Schluss | GitHub Free; Pflichtangaben wegen gemeinsamen Kontos | H B; Schlusssatz Z §8 |

## docs/vorlagen/spur-start.md

| Alt | Regel | Neu |
|---|---|---|
| Kurzaufrufe | Kurzaufruf, „Geplanter Lauf mit Teilen“ | S (Wortlaut) |
| nach Kurzaufruf | Zugriffsabsatz wörtlich in jedem Startaufruf; Reviews nur wenn Daten nötig; Voraussetzung Leitstelle; Schreibrecht Referenz-Repo nie nutzen; fehlende Daten im PR melden | S (gekürzt; Voraussetzung → H F) |
| Folgeteil | einzelner Folgeteil ohne Fortsetzung, R9F-02 | S |
| Fixlauf | Auftrag, Nachprüfung anlegen, `list_triggers`, Modell prüfen, sonst `leitstelle-delta`, `review-nein` entfernen, `beleg-strittig` | S „Fixlauf“ (+ Fixlauf ohne Befund, #712/N3) |
| Ablauf 1–8 | lesen, einrichten, PR prüfen/anlegen, harte Regeln, Rebase + `--schnell`, fertig, Übergabe, Antwort | S Ablauf 1–8 (Schritt 3/5 ohne Sonderverweis auf „PR-Base“/„Rebase-Ziel“, R719-02; Übergabeinhalt → Z §3, V) |
| Teile 1–7 | Stand, ein Branch/ein PR, Teil abschließen, blockiert, Fortsetzung anlegen, Kettenende, keine fremde Spur | S Teile 1–7 (gekürzt) |

## docs/vorlagen/review-anfrage.md

| Alt | Regel | Neu |
|---|---|---|
| Kurzaufruf | Kurzaufruf | R (Wortlaut) |
| Zusätze | geplanter Lauf, Nachprüfung | R (gekürzt) |
| Ablauf 1 | lesen, Abbruch bei Mitarbeit | R Ablauf 1 |
| Ablauf 2 | SDK, `dotnet --version`; Connector-Rekonstruktion, Tree-SHA, unverifizierter Stand, nur Codelesen dokumentieren | R Ablauf 2 (Kern) + H E (Rekonstruktion) |
| Ablauf 2 | genauer Head (`rev-parse`, `status`), selbst bauen, Läufe je Reviewart, PR-Angaben prüfen | R Ablauf 2 (+ Prüflauf Gesamtreview, #712/P3-3) |
| Ablauf 3 | Gegenbeispiele, Diff, Aufrufer, Gegenprobe je Test | R Ablauf 3 (+ Hinweis Gesamtreview, #712/P3-3) |
| Ablauf 4 | drei Teile, 120 Zeilen, Beleg, nur Codelesen | R Ablauf 4 |
| Ablauf 5–7 | Ablage, Label, Antwort | R Ablauf 5–7 |
| Gesamtreview | Ziel, Nachprüfung nur P0/P1, Einsatzabsatz, Abschlussprüfung | H C (Verweis „Aufrufe für ChatGPT/Codex“ → H D) |
| Codex 1–4 | Klon, ohne Netz, Head-Branch, Ablage neu, keine Netzbefehle, Zugriffsabsatz nur Claude | H D (Netz vereinheitlicht, #712/N-02) |

## docs/vorlagen/leitstelle-start.md

| Alt | Regel | Neu |
|---|---|---|
| Kurzaufruf | Kurzaufruf | L (Wortlaut) |
| 0–6 | Zugriff, lesen, Integration, Review anfordern, Mergen, Planen, Übergabe | L 0–6 (gekürzt; Schritt 1 + H) |
| 7 | geplante Läufe: Repos der anlegenden Sitzung, Vorbedingung, Zugriffsabsatz, Modell, `spur-wartet`, R9F-03, Draft aufheben | L 7 → H F |
| 8–9 | Antwort, Sitzung je Runde | L 8–9 |
| 10 | Warten über Labels | L 10 (Kurzform) + H F |

## docs/handoffs/VORLAGE.md, CLAUDE.md

| Alt | Regel | Neu |
|---|---|---|
| V Kopf | Kommentar, ohne PR Datei, 15 Zeilen, nicht Ausgeführtes weglassen | V Kopf (gekürzt) |
| V Vorlagen | Übergabe, Übernahme | V (Wortlaut) |
| C Einrichtung, GitHub (REST, Draft, Endpunkte, Releases, Merge, Leitstelle `push`, Trailer) | | C (Wortlaut bzw. gekürzt) |
| C „Geplante Läufe anlegen“ | Repos der anlegenden Sitzung, `add_repo` im Lauf meist nicht da | C (Kurzform) + H F |
| C „Git im flachen Klon“ | Fetch-Regeln, Tracking | H G |
| C „Ende geplanter Läufe“ | `list_triggers`, Codex mit Netz, Titel/Head prüfen | H F, H D |
| C „Auf geplante Läufe warten“ | Hintergrundschleife, Intervall, Obergrenze, Neustart | H F |
| C Rolle | Leitstelle | C (Wortlaut) |

## Klärungen aus #712 (neu, je mit Verweis)

| ID | Klärung | Fundstelle |
|---|---|---|
| R-06 | Leitstelle merged „nach §6“ | Z §5 Punkt 8 |
| N-01 | `spur-wartet` mit Wartepunkt beim Nutzer: Leitstelle entfernt das Label, sobald sie vorgelegt und im PR vermerkt hat | Z §5 Punkt 8 (Tabelle), H F |
| N-02 | Codex: Vorgabe „mit Netzzugang“; ohne Netz kein Label, Leitstelle wartet auf `review/*` | H D (C und Z verweisen dorthin) |
| P3-1, P3-2 | E14-3/„Vollprüfung“ als abgelöst gekennzeichnet (Zeilen stehen seit dem Rundenabschluss in `docs/archiv/runde-14.md`) | `archiv/runde-14.md` (zwei Halbsätze) |
| P3-3 | Gesamtreview: Prüflauf `--schnell`; Schritt 3 „Gesamtreview: siehe Anhang C“ | R Ablauf 2 und 3, H C |
| N2 (#721) | Nachprüfungs-Ausnahme in §5 nennt den Rebase-Fall aus §6 Nr. 1 | Z §5 Spiegelstrich 4 und Punkt 4 |
| N3 (#721) | Fixlauf ohne Befund (Rebase/Planpflicht) setzt `leitstelle-delta`; Startaufruf dafür | Z §5 Punkt 8 (Tabelle), S „Fixlauf“, L 2 |
| R719-02 | Reste Stapelmodell: Klammer „bis Runde 14 gestapelt“ und Sonderverweise „PR-Base“/„Rebase-Ziel“ entfernt | Z §3, S Ablauf 3 und 5 |
