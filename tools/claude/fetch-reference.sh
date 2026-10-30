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
# Ohne Angabe: oberflaeche und uebersicht. Ein veralteter vorhandener Klon wird nicht verändert (Rückgabe 3, Hinweis). Die Bilder niemals in dieses Repo kopieren.
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
  # Vorhandener Klon (RV2-01 #140, RV2N-01 #144, RV2N2-01 #147, RV2N2-02 #148, RV2N3-01 #151): Das Skript verändert
  # dessen Arbeitsverzeichnis nie durch Aktualisieren. Es prüft nur, ob das Ziel die Wurzel eines Klons ist, dessen
  # origin genau das Referenz-Repo auf GitHub ist, holt main nach .git und bricht ab, wenn der Klon nicht auf diesem
  # Stand ist. Dann den Ordner selbst löschen (eigene Dateien darin vorher sichern) und das Skript erneut aufrufen.
  top="$(git -C "$TARGET" rev-parse --show-toplevel 2>/dev/null || true)"
  url="$(git -C "$TARGET" remote get-url origin 2>/dev/null || true)"
  case "$url" in
    https://github.com/kaaasi/eishockey-manager-referenz|https://github.com/kaaasi/eishockey-manager-referenz.git|\
    git@github.com:kaaasi/eishockey-manager-referenz.git) ;;
    *) url="" ;;
  esac
  if [ -z "$url" ] || [ "$top" != "$(cd "$TARGET" && pwd -P)" ]; then
    echo "Abbruch: $TARGET ist kein Klon von https://github.com/kaaasi/eishockey-manager-referenz." >&2
    exit 2
  fi
  git -C "$TARGET" fetch -q --depth 1 --filter=blob:none origin main
  have="$(git -C "$TARGET" rev-parse HEAD)"
  want="$(git -C "$TARGET" rev-parse FETCH_HEAD)"
  if [ "$have" != "$want" ]; then
    echo "Abbruch: $TARGET steht auf ${have:0:7}, das Referenz-Repo auf ${want:0:7}. Ordner löschen (eigene Dateien" \
      "darin vorher sichern) und erneut aufrufen; das Skript klont dann neu." >&2
    exit 3
  fi
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
