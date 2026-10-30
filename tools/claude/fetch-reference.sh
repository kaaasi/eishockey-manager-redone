#!/usr/bin/env bash
# Lädt Referenzbilder aus dem privaten Repo kaaasi/eishockey-manager-referenz nach ~/referenz
# (nur die genannten Ordner). Aufruf:
#   tools/claude/fetch-reference.sh [oberflaeche] [karriere] [bundestrainer] [folge01 … | folgen]
#     [eisbaeren_f01 … | eisbaeren] [duell_s2a … | duell] [speedrun_t1 speedrun_t2 | speedrun] [uebersicht] [original]
# `bundestrainer` = WM-Video im Modus „nur Weltkugel“ (docs/referenz-videos.md).
# `folge01` … `folge29` = Reihe „Eishockey Manager EM“ (entdoppelt, ausgedünnt); `folgen` = alle 29 (ca. 0,4 GB).
# Release videos-roh-2: `eisbaeren_f01` … `eisbaeren_f65` (ohne f41), `duell_s2a` … `duell_s6d`, `speedrun_t1`/`_t2`;
# Gruppen `eisbaeren` (ca. 1,3 GB), `duell` (ca. 0,3 GB), `speedrun` (ca. 0,1 GB).
# `original` = DOS-Originaldateien (docs/originaldateien.md).
# Ohne Angabe: oberflaeche und uebersicht. Die Bilder niemals in dieses Repo kopieren.
set -euo pipefail
TARGET="${REFERENZ:-$HOME/referenz}"
FOLDERS=("$@")
[ ${#FOLDERS[@]} -eq 0 ] && FOLDERS=(oberflaeche uebersicht)
EXPANDED=()
for f in "${FOLDERS[@]}"; do
  case "$f" in
    folgen) for n in $(seq -w 1 29); do EXPANDED+=("folge$n"); done ;;
    eisbaeren) for n in $(seq -w 1 65); do [ "$n" = 41 ] || EXPANDED+=("eisbaeren_f$n"); done ;;
    duell) for t in s2a s2b s2c s4a s4b s4c s4d s6a s6b s6c s6d; do EXPANDED+=("duell_$t"); done ;;
    speedrun) EXPANDED+=(speedrun_t1 speedrun_t2) ;;
    *) EXPANDED+=("$f") ;;
  esac
done
FOLDERS=("${EXPANDED[@]}")

if [ ! -d "$TARGET/.git" ]; then
  git clone -q --depth 1 --filter=blob:none --sparse https://github.com/kaaasi/eishockey-manager-referenz "$TARGET"
fi
git -C "$TARGET" sparse-checkout add "${FOLDERS[@]}"
for f in "${FOLDERS[@]}"; do
  echo "$f: $(ls "$TARGET/$f" | wc -l) Dateien in $TARGET/$f"
done
