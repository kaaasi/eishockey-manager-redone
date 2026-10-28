# Ablage der DOS-Originaldateien

Die vom Nutzer bereitgestellte DOS-Version liegt im privaten Repository
[`kaaasi/eishockey-manager-referenz`](https://github.com/kaaasi/eishockey-manager-referenz).
Zugriff benötigt Leserechte auf dieses Repository, auch in einer neuen Chat- oder Arbeitsumgebung.

| Inhalt | Pfad im Referenz-Repository |
|---|---|
| Unverändertes hochgeladenes Archiv | `original/Eishockey-Manager_DOS_DE.zip` |
| Vollständig entpackte Dateien | `original/gamedata/` (unter anderem `em.exe`, `kb/`, `tore/`) |
| SHA-256-Prüfsummen für ZIP und entpackte Dateien | `original/SHA256SUMS` |
| Herkunft und weitere Hinweise | `original/README.md` |

Nachgewiesener Ablagestand:
[`709992cad106f9f26fc32158d84e3e9fb47f36cf`](https://github.com/kaaasi/eishockey-manager-referenz/tree/709992cad106f9f26fc32158d84e3e9fb47f36cf/original),
angelegt über [Referenz-PR #1](https://github.com/kaaasi/eishockey-manager-referenz/pull/1).
Der folgende Abruf verwendet diesen festen Stand und funktioniert auch, solange dessen Übernahme nach `main`
noch aussteht. ZIP und alle 166 entpackten Dateien wurden beim Ablegen auf unveränderte Inhalte geprüft.

## Laden in einer neuen Umgebung

Beispiel für einen neuen lokalen Klon unter `/workspace/shared/`:

```bash
mkdir -p /workspace/shared
git clone --depth 1 --filter=blob:none --sparse \
  https://github.com/kaaasi/eishockey-manager-referenz.git \
  /workspace/shared/eishockey-manager-referenz
git -C /workspace/shared/eishockey-manager-referenz fetch --depth 1 origin \
  709992cad106f9f26fc32158d84e3e9fb47f36cf
git -C /workspace/shared/eishockey-manager-referenz checkout --detach \
  709992cad106f9f26fc32158d84e3e9fb47f36cf
git -C /workspace/shared/eishockey-manager-referenz sparse-checkout set original
```

Einen bereits vorhandenen Referenz-Klon weiterverwenden, statt ihn zu überschreiben. Vor einem Wechsel des
lokalen Stands dortige Änderungen prüfen. `sparse-checkout set` legt die Auswahl neu fest; benötigte
Videoreferenz-Ordner gegebenenfalls zusätzlich angeben.

Die Ablage in GitHub ist dauerhaft; Dateien in `/workspace/shared/` gehören dagegen zur jeweiligen
Umgebungsinstanz. Neue Chats erhalten sie erst durch diesen Abruf oder ein entsprechend eingerichtetes
Umgebungs-Setup. Die Setup-Konfiguration wird durch diese Dokumentation nicht automatisch geändert.

## Im Remake verwenden

Erwartet werden `gamedata/em.exe`, `gamedata/kb/0.vga` und `gamedata/tore/anzahl` im Stamm des
Code-Repositories. Den Inhalt aus `original/gamedata/` dorthin kopieren oder einen Symlink anlegen.
Wenn dort noch kein `gamedata` existiert, beispielsweise:

```bash
ln -s /workspace/shared/eishockey-manager-referenz/original/gamedata \
  /workspace/eishockey-manager-redone/gamedata
```

Weitere Worktrees können denselben Bestand über eigene `gamedata`-Symlinks nutzen. Alternativ nimmt
`tools/claude/setup-workspace.sh` den bereits geladenen Datenordner als Argument entgegen und verlinkt ihn;
dieses Skript lädt die Originaldaten nicht selbst.

Prüfsummen im Referenz-Klon kontrollieren:

```bash
cd /workspace/shared/eishockey-manager-referenz/original
sha256sum -c SHA256SUMS
```

SHA-256 der unveränderten ZIP:
`142fc78e9b286cb47f36baa450f5af311ac0902525564d0c30ab63697ca321f4`.

Die Originaldateien bleiben im privaten Referenz-Repository bzw. lokal im ignorierten `gamedata/`.
In dieses Code-Repository werden nur Beschreibungen und Links übernommen.
