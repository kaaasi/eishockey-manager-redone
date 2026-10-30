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
else
  # Vorhandenen Klon auf den aktuellen Stand von main bringen (RV2-01, #140). Die Historie des Referenz-Repos wurde
  # am 30.10.2026 neu geschrieben, daher kein Fast-forward, sondern hart auf den geholten Stand – aber nur, wenn das
  # Ziel sicher der Referenz-Klon ist und keine lokalen Änderungen hat (RV2N-01, #144).
  top="$(git -C "$TARGET" rev-parse --show-toplevel 2>/dev/null || true)"
  url="$(git -C "$TARGET" remote get-url origin 2>/dev/null || true)"
  if [ "$top" != "$(cd "$TARGET" && pwd -P)" ] || [[ ! "$url" =~ kaaasi/eishockey-manager-referenz(\.git)?/?$ ]]; then
    echo "Abbruch: $TARGET ist kein Klon von kaaasi/eishockey-manager-referenz (origin: ${url:-keiner})." >&2
    exit 2
  fi
  if [ -n "$(git -C "$TARGET" status --porcelain --untracked-files=no)" ]; then
    echo "Abbruch: $TARGET hat lokale Änderungen; bitte sichern oder verwerfen, dann erneut aufrufen." >&2
    exit 2
  fi
  git -C "$TARGET" fetch -q --depth 1 --filter=blob:none origin main
  git -C "$TARGET" reset -q --hard FETCH_HEAD
fi
git -C "$TARGET" sparse-checkout add "${FOLDERS[@]}"
missing=0
for f in "${FOLDERS[@]}"; do
  if [ -d "$TARGET/$f" ]; then
    echo "$f: $(find "$TARGET/$f" -type f | wc -l) Dateien in $TARGET/$f"
  else
    echo "$f: fehlt im Referenz-Repo (Name prüfen, docs/referenz-videos.md)" >&2
    missing=1
  fi
done
exit "$missing"
