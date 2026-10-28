# Ablage der DOS-Originaldateien

Die vom Nutzer bereitgestellte DOS-Version liegt im privaten Repository
[`kaaasi/eishockey-manager-referenz`](https://github.com/kaaasi/eishockey-manager-referenz) auf `main`.
Zugriff benötigt Leserechte auf dieses Repository, auch in einer neuen Chat- oder Arbeitsumgebung.

| Inhalt | Pfad im Referenz-Repository |
|---|---|
| Unverändertes hochgeladenes Archiv | `original/Eishockey-Manager_DOS_DE.zip` |
| Vollständig entpackte Dateien | `original/gamedata/` (unter anderem `em.exe`, `kb/`, `tore/`) |
| SHA-256-Prüfsummen für ZIP und entpackte Dateien | `original/SHA256SUMS` |
| Herkunft und weitere Hinweise | `original/README.md` |

Abgelegt mit Commit `709992c` über [Referenz-PR #1](https://github.com/kaaasi/eishockey-manager-referenz/pull/1)
(gemergt). Nachweis des Inhalts sind die Prüfsummen, nicht ein fester Commit.

## Laden in einer neuen Umgebung

**Nur `original/` abrufen.** Das Referenz-Repository enthält außerdem die Videobilder (rund 650 MB); ein
vollständiger Klon lädt alles. Ein Sparse-Klon lädt nur die gewählten Ordner:

```bash
git clone --depth 1 --filter=blob:none --sparse \
  https://github.com/kaaasi/eishockey-manager-referenz.git ~/referenz
git -C ~/referenz sparse-checkout add original
```

Dasselbe erledigt `tools/claude/fetch-reference.sh original` (Ziel `~/referenz`, änderbar über `REFERENZ=<pfad>`).
Ein vorhandener Klon wird weiterverwendet; `sparse-checkout add` ergänzt die Auswahl, ohne bereits geladene
Bildordner zu entfernen.

Prüfsummen kontrollieren:

```bash
(cd ~/referenz/original && sha256sum -c --quiet SHA256SUMS) && echo OK
```

SHA-256 der unveränderten ZIP: `142fc78e9b286cb47f36baa450f5af311ac0902525564d0c30ab63697ca321f4`.

Der lokale Klon gehört zur jeweiligen Umgebung; neue Chats holen ihn mit diesem Abruf erneut.

## Im Remake verwenden

Erwartet werden `gamedata/em.exe`, `gamedata/kb/0.vga` und `gamedata/tore/anzahl` im Stamm des
Code-Repositories. `tools/claude/setup-workspace.sh ~/referenz/original/gamedata` verlinkt den Ordner (und richtet
SDK und Pakete ein); ohne das Skript genügt ein Symlink oder eine Kopie:

```bash
ln -s ~/referenz/original/gamedata gamedata
```

## Ohne Zugriff auf das Referenz-Repository

Rückfallweg: eigene Kopie der DOS-Version, z. B. die ZIP in den Projektdateien des Nutzers, in einen eigenen Ordner
entpacken und diesen nach `gamedata/` verlinken. Fehlt auch das, beim Nutzer anfragen.

Die Originaldateien bleiben im privaten Referenz-Repository bzw. lokal im ignorierten `gamedata/`.
In dieses Code-Repository werden nur Beschreibungen und Links übernommen.
