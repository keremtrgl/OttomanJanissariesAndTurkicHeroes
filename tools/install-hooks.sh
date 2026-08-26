#!/bin/sh
# One-time setup: point git at this repo's tracked hooks directory
# (.githooks/) so tools/verify_mod.py runs automatically before every
# commit. Git never reads hooks from a tracked path on its own - .git/hooks/
# is local-only and not committed - so this needs to run once per clone.
set -e
REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"
git config core.hooksPath .githooks
chmod +x .githooks/pre-commit 2>/dev/null || true
echo "Installed: git will now run .githooks/pre-commit (tools/verify_mod.py) before every commit."
