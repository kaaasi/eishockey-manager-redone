#!/usr/bin/env bash
# Lädt Referenzbilder aus dem privaten Repo kaaasi/eishockey-manager-referenz nach ~/referenz
# (nur die genannten Ordner). Aufruf:
#   tools/claude/fetch-reference.sh [oberflaeche] [karriere] [bundestrainer] [folge01 … folge29 | folgen] [uebersicht] [original]
# `bundestrainer` = WM-Video im Modus „nur Weltkugel“ (docs/referenz-videos.md).
# `folge01` … `folge29` = Reihe „Eishockey Manager EM“ (entdoppelt, je 50–100 MB); `folgen` = alle 29 (ca. 2,5 GB).
# `original` = DOS-Originaldateien (docs/originaldateien.md).
# Ohne Angabe: oberflaeche und uebersicht. Die Bilder niemals in dieses Repo kopieren.
set -euo pipefail
TARGET="${REFERENZ:-$HOME/referenz}"
FOLDERS=("$@")
[ ${#FOLDERS[@]} -eq 0 ] && FOLDERS=(oberflaeche uebersicht)
EXPANDED=()
for f in "${FOLDERS[@]}"; do
  if [ "$f" = folgen ]; then
    for n in $(seq -w 1 29); do EXPANDED+=("folge$n"); done
  else
    EXPANDED+=("$f")
  fi
done
FOLDERS=("${EXPANDED[@]}")

if [ ! -d "$TARGET/.git" ]; then
  git clone -q --depth 1 --filter=blob:none --sparse https://github.com/kaaasi/eishockey-manager-referenz "$TARGET"
fi
git -C "$TARGET" sparse-checkout add "${FOLDERS[@]}"
for f in "${FOLDERS[@]}"; do
  echo "$f: $(ls "$TARGET/$f" | wc -l) Dateien in $TARGET/$f"
done
