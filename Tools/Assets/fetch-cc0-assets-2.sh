#!/usr/bin/env bash
# Second asset batch (KayKit Skeletons, Space Base, Prototype Bits, Halloween Bits, Medieval Hexagon).
# Reads Tools/Assets/cc0-manifest-2.tsv (repo, pinned commit, source path, destination, SHA-256),
# downloads each repository at its pinned commit, copies the listed files into Assets/ThirdParty/
# and verifies every checksum. Licence files are already committed.
#
#   bash Tools/Assets/fetch-cc0-assets-2.sh      (about 110 MB download; prints progress per repository)
#   git add Assets/ThirdParty && git commit -m "Add CC0 asset files (batch 2)" && git push
#
# If the files are already in your checkout (git lfs pull), you do not need to run this.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
DEST="$ROOT/Assets/ThirdParty"
MANIFEST="$ROOT/Tools/Assets/cc0-manifest-2.tsv"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

sha_check() { if command -v sha256sum >/dev/null 2>&1; then sha256sum "$1" | cut -d' ' -f1; else shasum -a 256 "$1" | cut -d' ' -f1; fi; }

n=0
while IFS=$'\t' read -r repo commit src dst sum; do
  key="${repo##*/}"
  if [ ! -d "$TMP/$key/.git" ]; then
    echo "Fetching $key @ ${commit:0:10} (progress below)"
    git init -q "$TMP/$key"
    git -C "$TMP/$key" fetch --depth 1 "$repo" "$commit" 2>&1 | tail -1 || true
    git -C "$TMP/$key" -c advice.detachedHead=false checkout -q FETCH_HEAD
  fi
  mkdir -p "$(dirname "$DEST/$dst")"
  cp "$TMP/$key/$src" "$DEST/$dst"
  actual="$(sha_check "$DEST/$dst")"
  if [ "$actual" != "$sum" ]; then echo "CHECKSUM MISMATCH: $dst" >&2; exit 1; fi
  n=$((n+1))
done < "$MANIFEST"
echo "OK: $n files in Assets/ThirdParty match the manifest."
