# Review PR #915 – Abschluss Runde 21 (Doku-Prüfung)

Geprüft: Base `3d8eb0e`, Head `b07c050` (Branch `prozess/abschluss-runde-21`). Reviewer: Claude Haiku 5.5, geplanter Lauf,
eigene Sitzung, an #915 nicht beteiligt. Reine Doku-Prüfung ohne Build und ohne CI (Auftrag). Gelesen über die Git-Objekte
(`git show`), kein Checkout des Heads: „nur Codelesen“ für die Doku.

## 1. Merge-Empfehlung

**Ja für `b07c050`.** Der Runde-21-Abschnitt ist vollständig archiviert, Ergebnis und Mergenummern stimmen mit GitHub überein,
die sechs erwarteten Vormerkungen sind offen. Die beiden P3-Punkte sind kein Merge-Hindernis.

## 2. Befunde

- **P3-1 (Beleg):** `docs/archiv/runde-21.md:34` – die alte Überschrift „## Aktuelle Runde: Runde 21 …“ steht unter dem neuen
  Titel „# Archiv: Runde 21 …“. Der Archivkopf sagt, nur die Überschrift sei geändert. Probe:
  `git show b07c050:docs/archiv/runde-21.md | grep -n "Aktuelle Runde"` → Zeile 34. Vorschlag: Zeile streichen.
- **P3-2 (Verdacht, ohne Beleg):** „Modellwahl (Nutzer 17.11.2026)“ im Kopf Runde 22 (`docs/arbeitspakete.md`) und im Archiv
  (Entscheidungen). Außerhalb dieses Commits habe ich keine Quelle gefunden. Geprüft: Issues #896, #897 (inkl. Kommentare),
  #899, #908 und die Reviews vom 16.–17.11.; nicht geprüft: alle Kommentare der Spur-PRs #889–#913.

## 3. Ausgeführte Prüfungen

- `git diff --stat 3d8eb0e b07c050`: 2 Dateien, 198 Zeilen ergänzt, 155 entfernt (`arbeitspakete.md`, `archiv/runde-21.md`);
  keine Änderung an Code oder Tests.
- **Gegenbeispiel Archivprobe (bestanden):** Python-Vergleich der nichtleeren Zeilen des Abschnitts „Aktuelle Runde: Runde 21“
  aus `git show 3d8eb0e:docs/arbeitspakete.md` (bis „### Zurückgestellt“, 129 Zeilen) gegen `archiv/runde-21.md`: 0 fehlend,
  der Block ist zusammenhängend enthalten. Zurückgestellt und Vormerkungen fehlen im Archiv bewusst.
- **Erwartet offen (bestätigt):** GitHub-Issues #896, #897, #899, #908, #912, #914 offen; einziger offener PR ist #915.
- **Archiv gegen GitHub:** Paare PR/Erstreview bzw. Merge-Datum für #891/#898, #892/#900, #893/#901, #894/#907, #895/#903,
  #904/#906, #905/#909, #910/#911 und Gesamtreview #913 („Ja für e5cf5e3“, `docs/reviews/2026-11-17-runde-21-gesamt.md`) stimmen.
- **Vormerkungen gegen Issues:** #896 (R21-E2 = A, Fundstellen 0x32AA3/0x38402/0x376F0), #897 (Finanzübersicht 3/4 und 6/7,
  Pokal-Gegentore, `ChangeList`, 0x07462, Pool 179, `SlotSeed`, A059), #899 (A093, A206, A059), #908 (Reihentausch, 0x952C)
  decken sich mit dem Text.
- **A093:** `docs/analyse/re-vor-dosbox.md:21` klassifiziert „D → V (M)“; die Streichung aus „Zurückgestellt“ ist richtig.
- **Entscheidungen Modelle:** Sonnet 5.5 für `pruefung-karte` (`docs/reviews/2026-11-17-pruefung-karte-r21.md`), Fable 5.2 für
  `teamwert-aufstellung` und `zufallsstrom` (Reviews 16.11./17.11.) stimmen mit dem Archiv überein. Die alten Statuszeilen
  nennen weiter Opus 5.5; das erklärt der Archivkopf (Stand vor Merge).

## Akzeptierte `[APPROX]` und Annahmen

- Keine; reine Doku. Nicht geprüft: Build, Tests, Originaldaten (Auftrag). Kommentare der Spur-PRs nur stichprobenartig.
