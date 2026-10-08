#!/usr/bin/env python3
"""Checks DECISIONS.md: entries numbered 1, 2, 3... in order, no duplicates or gaps, no merge markers.
Usage: python3 tools/check-decisions.py   (exit code 1 on any problem)"""
import re, sys, pathlib

text = pathlib.Path(__file__).resolve().parent.parent.joinpath("DECISIONS.md").read_text()
problems = []
for marker in ("<<<<<<<", "=======", ">>>>>>>"):
    for n, line in enumerate(text.splitlines(), 1):
        if line.startswith(marker):
            problems.append(f"line {n}: leftover merge marker {marker}")

numbers = [(int(m.group(1)), m.group(2)) for m in re.finditer(r"^### (\d+)\. (.*)$", text, re.M)]
seen = {}
for i, (num, title) in enumerate(numbers, 1):
    if num in seen:
        problems.append(f"duplicate number {num}: '{seen[num]}' and '{title}'")
    seen.setdefault(num, title)
    if num != i:
        problems.append(f"entry {i} in the file is numbered {num}: '{title}'")
        break

titles = [t for _, t in numbers]
for t in {t for t in titles if titles.count(t) > 1}:
    problems.append(f"same decision appears twice: '{t}'")

if problems:
    print("DECISIONS.md problems:\n  " + "\n  ".join(problems))
    sys.exit(1)
print(f"DECISIONS.md OK: {len(numbers)} decisions, numbered 1 to {len(numbers)} in order")
