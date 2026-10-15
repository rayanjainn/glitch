#!/usr/bin/env python3
"""
Developer tool: builds Tools/Assets/cc0-manifest-2.tsv from local clones of the source repositories.
(The fetch script only needs the manifest; run this when the asset selection changes.)

Usage: python3 Tools/Assets/make-manifest.py <clones-dir>
Each row: repo-url <TAB> commit <TAB> source-path <TAB> dest-under-Assets/ThirdParty <TAB> sha256
"""
import hashlib
import os
import subprocess
import sys

CLONES = sys.argv[1]
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "cc0-manifest-2.tsv")

PACKS = [
    # (clone dir, github repo, dest folder, [(source dir under addons/<x>/, glob-ish filter, dest subfolder)], texture(s))
    ("kaykit-character-pack-skeletons-1.0", "KayKit-Game-Assets/KayKit-Character-Pack-Skeletons-1.0", "KayKit_Skeletons",
     [("Characters/fbx", lambda n: n in ("Skeleton_Warrior.fbx", "Skeleton_Minion.fbx", "Skeleton_Mage.fbx", "Skeleton_Rogue.fbx"), ""),
      ("Assets/fbx", lambda n: n.endswith(".fbx"), "Weapons")],
     [("Textures/skeleton_texture.png", "skeleton_texture.png")]),
    ("kaykit-space-base-bits-1.0", "KayKit-Game-Assets/KayKit-Space-Base-Bits-1.0", "KayKit_SpaceBase",
     [("Assets/fbx", lambda n: n.endswith(".fbx"), "Models")],
     [("Assets/fbx/spacebits_texture.png", "spacebits_texture.png")]),
    ("kaykit-prototype-bits-1.0", "KayKit-Game-Assets/KayKit-Prototype-Bits-1.0", "KayKit_PrototypeBits",
     [("Assets/fbx", lambda n: n.endswith(".fbx"), "Models")],
     [("Assets/fbx/prototypebits_texture.png", "prototypebits_texture.png")]),
    ("kaykit-halloween-bits-1.0", "KayKit-Game-Assets/KayKit-Halloween-Bits-1.0", "KayKit_HalloweenBits",
     [("Assets/fbx", lambda n: n.endswith(".fbx"), "Models")],
     [("Assets/fbx/halloweenbits_texture.png", "halloweenbits_texture.png")]),
    ("kaykit-medieval-hexagon-pack-1.0", "KayKit-Game-Assets/KayKit-Medieval-Hexagon-Pack-1.0", "KayKit_MedievalHexagon",
     [("Assets/fbx/decoration/nature", lambda n: n.endswith(".fbx"), "Models"),
      ("Assets/fbx/decoration/props", lambda n: n.endswith(".fbx"), "Models"),
      ("Assets/fbx/buildings/neutral", lambda n: n.endswith(".fbx"), "Models"),
      ("Assets/fbx/buildings/blue", lambda n: n.endswith(".fbx"), "Models"),
      ("Assets/fbx/tiles/base", lambda n: n.endswith(".fbx"), "Models")],
     [("Assets/fbx/buildings/blue/hexagons_medieval.png", "hexagons_medieval.png")]),
]

def sha(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()

rows = []
seen = set()
for clone, repo, dest, groups, textures in PACKS:
    root = os.path.join(CLONES, clone)
    commit = subprocess.check_output(["git", "-C", root, "rev-parse", "HEAD"], text=True).strip()
    addon = os.path.join(root, "addons", os.listdir(os.path.join(root, "addons"))[0])
    for sub, keep, dest_sub in groups:
        folder = os.path.join(addon, sub)
        for name in sorted(os.listdir(folder)):
            if not keep(name):
                continue
            d = (dest + "/" + dest_sub + "/" + name).replace("//", "/")
            if d in seen:
                raise SystemExit("duplicate destination " + d)
            seen.add(d)
            src = os.path.relpath(os.path.join(folder, name), root)
            rows.append((f"https://github.com/{repo}", commit, src, d, sha(os.path.join(folder, name))))
    for src_rel, name in textures:
        full = os.path.join(addon, src_rel)
        rows.append((f"https://github.com/{repo}", commit, os.path.relpath(full, root), dest + "/" + name, sha(full)))

with open(OUT, "w", newline="\n") as f:
    for r in rows:
        f.write("\t".join(r) + "\n")
print(len(rows), "files ->", OUT)
