# Gesamtreview Runde 18

## 1. Merge-Empfehlung

**Ja** für `f6679b8c62d2652f99fe00ca120cf5e0c0bffde4` (Base `5367ba84614e9c57aefc85f0cab348aaebc6a1fd`). Die gezielten Gegenbeispiele und der Schnelllauf mit Originaldaten bestehen; bei den untersuchten Übergängen ergab sich kein neuer nachgewiesener Produktfehler. Die unten genannten Grenzen bleiben bestehen. Empfehlung gilt für diesen Head, nicht für spätere Änderungen.

## 2. Befunde

Keine neuen nachgewiesenen Produktfehler P0–P3.

**Testlücke:** Speichern/Laden mitten in einer angehaltenen Live-Paarung wurde nicht als fortsetzbarer Spielstand geprüft. Der reguläre Live- und Eingriffsweg bietet keinen Speichern/Laden-Dialog; `LiveMatchDay` und die vorbereiteten Zuschauer sind temporär. Geprüft wurden dagegen Spielstände vor und nach dem Spieltag sowie nach Saisonwechseln. Kein Nachweis für einen vom Nutzer erreichbaren Defekt.

Erwartet offen bestätigt: #730 Spieltage 2/3 und A093, A053, #826, #838 (⅓ s/Minute `[APPROX]`), kein Platztausch neuer Manager in die Start-Liga, Torszenen in Runde 19. Doku-Nachzug bleibt: Inventur A318–A320, `anforderungen/bildschirme.md` Z. 67/100 und `re-livespiel.md` §4 Punkt 5. Diese Punkte werden nicht als neue Befunde gezählt.

## 3. Ausgeführte Prüfungen

- Sauberer Checkout: `git rev-parse HEAD` = angegebener Head, `git status --porcelain` leer vor Prüfung; SDK 10.0.402. Originaldaten/Spielstände aus lokalem Referenz-Repo, `sha256sum -c --quiet SHA256SUMS` erfolgreich.
- #773/#804/#845-Kern: `dotnet test tests/EishockeyManager.Core.Tests --filter 'FullyQualifiedName~AbgleichDosboxPenaltyTests|FullyQualifiedName~AbgleichDosboxExportTests|FullyQualifiedName~AddManagerTests'` → 32 bestanden, 0 fehlgeschlagen. #804 umfasst FILE03/16A/44/53 und vier Saisonwechsel mit HOK-Rundläufen.
- #834/#845: `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.UI.Tests --filter 'FullyQualifiedName~NutzerBefundeMatchDayTests|FullyQualifiedName~AddManagerScreenTests|FullyQualifiedName~NutzerBefundeAdvertisingTests'` → 11 bestanden. #832: gleicher Befehl mit Filter `FullyQualifiedName~PlayoffBanner_BeforeFirstLiveMatch` → 1 bestanden.
- Risikoorientiertes Lesen des Deltas und der Aufrufer: Semaphore-Übergabe und `CurrentManager`, Minutenschritt und Anzeigezufall, Zuschauerabrechnung, Managerliste nach Laden, HOK-Felder und Wettbewerbsschritte. Schnelllauf enthält u. a. Gleichheit mit/ohne Live-Anzeige, Eingriff, Zuschauerabrechnung und Mehrsaisonproben.
- Nicht eigenständig ausgeführt: interaktiver DOSBox-/Pixelvergleich und vollständige Inventur; Originaldaten wurden in den Tests genutzt. Keine flächendeckenden Mutationen oder Einzelrückführungen.

Akzeptierte `[APPROX]`: Paarungen im Live-Anschluss nacheinander statt minutenweise verschränkt; Zuschauer-Vorbereitung nicht serialisiert; #838 Zeitwert und die oben genannten offenen Punkte. Annahme: Speicherung ist über die normalen UI-Wege nur außerhalb der laufenden Paarung erreichbar.

`tools/pruefen.sh --schnell` (Release-Build mit Warnungen als Fehler erfolgreich):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3249 | 0 | 0 |
