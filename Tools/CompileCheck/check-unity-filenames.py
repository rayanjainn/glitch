#!/usr/bin/env python3
"""Fails if a MonoBehaviour/ScriptableObject subclass is not declared in <ClassName>.cs.
Unity silently drops such components from scenes on save. Run: python3 Tools/CompileCheck/check-unity-filenames.py"""
import glob, os, re, sys

root = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets')
decls = {}
for f in glob.glob(os.path.join(root, '**', '*.cs'), recursive=True):
    for m in re.finditer(r'class\s+(\w+)\s*(?:<[^>]*>)?\s*:\s*([\w\.]+)', open(f, encoding='utf-8').read()):
        decls[m.group(1)] = (m.group(2).split('.')[-1], f)

UNITY = {'MonoBehaviour', 'ScriptableObject', 'Behaviour', 'Component'}

def is_unity(name, seen=()):
    base = decls.get(name, (None,))[0]
    if base in UNITY:
        return True
    return base in decls and base not in seen and is_unity(base, seen + (name,))

bad = [(c, f) for c, (_, f) in decls.items() if is_unity(c) and os.path.basename(f) != c + '.cs']
for c, f in bad:
    print('MISMATCH: class %s is in %s' % (c, os.path.relpath(f)))
print('OK' if not bad else '%d mismatch(es)' % len(bad))
sys.exit(1 if bad else 0)
