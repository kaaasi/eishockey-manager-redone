#!/usr/bin/env bash
# Lädt Referenzbilder aus dem privaten Repo kaaasi/eishockey-manager-referenz nach ~/referenz
# (nur die genannten Ordner). Aufruf: tools/claude/fetch-reference.sh [oberflaeche] [karriere] [uebersicht]
# Ohne Angabe: oberflaeche und uebersicht. Die Bilder niemals in dieses Repo kopieren.
set -euo pipefail
TARGET="${REFERENZ:-$HOME/referenz}"
FOLDERS=("$@")
[ ${#FOLDERS[@]} -eq 0 ] && FOLDERS=(oberflaeche uebersicht)

if [ ! -d "$TARGET/.git" ]; then
  git clone -q --depth 1 --filter=blob:none --sparse https://github.com/kaaasi/eishockey-manager-referenz "$TARGET"
fi
git -C "$TARGET" sparse-checkout add "${FOLDERS[@]}"
for f in "${FOLDERS[@]}"; do
  echo "$f: $(ls "$TARGET/$f" | wc -l) Dateien in $TARGET/$f"
done
