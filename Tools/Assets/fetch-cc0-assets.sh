#!/usr/bin/env bash
# Downloads the third-party CC0/OFL assets listed in CREDITS.md from the authors' official
# GitHub repositories (pinned commits), copies exactly the files the game uses into
# Assets/ThirdParty/, and verifies every file against Tools/Assets/cc0-assets.sha256.
#
# Run from anywhere:  bash Tools/Assets/fetch-cc0-assets.sh
# Then commit:        git add Assets/ThirdParty && git commit -m "Add CC0 asset files" && git push
# (Git LFS stores the binaries; run `git lfs install` once if you haven't.)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
DEST="$ROOT/Assets/ThirdParty"
SUMS="$ROOT/Tools/Assets/cc0-assets.sha256"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

fetch() { # url commit dir
  echo "Fetching $1 @ ${2:0:10}"
  git init -q "$TMP/$3"
  git -C "$TMP/$3" fetch -q --depth 1 "$1" "$2"
  git -C "$TMP/$3" -c advice.detachedHead=false checkout -q FETCH_HEAD
}

fetch https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0 672074b73ba276876a19e8816ecdc5241817ab47 adventurers
fetch https://github.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0 b0ca9bd96a8072ab36a3a5464f00ed1e06a16d07 dungeon
fetch https://github.com/KenneyNL/Starter-Kit-3D-Platformer 3fa8a04b1c01ab23db43123d4ce814a34c3fc7f0 platformer
fetch https://github.com/KenneyNL/Starter-Kit-FPS 185fd2326d74a5cf858cffc616f87cf9696f9cc0 fps

KA="$TMP/adventurers/addons/kaykit_character_pack_adventures"
KD="$TMP/dungeon/addons/kaykit_dungeon_remastered/Assets"

mkdir -p "$DEST/KayKit_Adventurers" "$DEST/KayKit_DungeonRemastered/Models" "$DEST/Kenney_StarterKitSounds" "$DEST/Fonts/LilitaOne"
cp "$KA/Characters/fbx/Knight.fbx" "$KA/Textures/knight_texture.png" "$DEST/KayKit_Adventurers/"
cp "$KD"/fbx/*.fbx "$DEST/KayKit_DungeonRemastered/Models/"
cp "$KD/texture/dungeon_texture.png" "$DEST/KayKit_DungeonRemastered/"
cp "$TMP"/platformer/sounds/*.ogg "$DEST/Kenney_StarterKitSounds/"
for f in "$TMP"/fps/sounds/*.ogg; do
  b="$(basename "$f")"
  if [ -e "$TMP/platformer/sounds/$b" ]; then cp "$f" "$DEST/Kenney_StarterKitSounds/fps_$b"; else cp "$f" "$DEST/Kenney_StarterKitSounds/$b"; fi
done
cp "$TMP/platformer/fonts/lilita_one_regular.ttf" "$DEST/Fonts/LilitaOne/LilitaOne-Regular.ttf"

echo "Verifying checksums..."
cd "$DEST"
if command -v sha256sum >/dev/null 2>&1; then sha256sum --quiet -c "$SUMS"; else shasum -a 256 --quiet -c "$SUMS"; fi
echo "OK: $(wc -l < "$SUMS" | tr -d ' ') files in Assets/ThirdParty match the recorded checksums."
