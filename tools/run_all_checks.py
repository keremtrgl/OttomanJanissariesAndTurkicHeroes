#!/usr/bin/env python3
"""
run_all_checks.py - Single entry point for this mod's two independent test/check systems.

Source/SeljukEmpire.Tests/ (xUnit, 43 tests as of v1.8.1) verifies the REACTIVE TACTICAL AI's
own decision logic (TacticalSituationAssessor - deliberately engine-independent, zero
TaleWorlds.* references, so it runs without the game installed). tools/verify_mod.py verifies
everything else (XML validity, id collisions, localization coverage, save-compatible id
ordering, troop/item balance signals, etc.). Until now these had to be run and read
separately, with no single "is the mod actually clean" answer - this wraps both under one
command and one pass/fail.

Usage:
    python tools/run_all_checks.py                       # both suites, auto-detects game install
    python tools/run_all_checks.py --game-path "C:\\...\\Mount & Blade II Bannerlord"
    python tools/run_all_checks.py --quick                # verify_mod.py: skip game-install-dependent checks
    python tools/run_all_checks.py --skip-dotnet-test      # verify_mod.py only (e.g. dotnet not installed)
    python tools/run_all_checks.py --skip-verify-mod       # dotnet test only

Exit code 0 only if both suites pass (or the ones that ran did); non-zero otherwise, so this
is safe to use as a single pre-commit/CI gate in place of calling verify_mod.py directly.
"""

import argparse
import shutil
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
TESTS_PROJECT = REPO_ROOT / "Source" / "SeljukEmpire.Tests" / "SeljukEmpire.Tests.csproj"
VERIFY_MOD = REPO_ROOT / "tools" / "verify_mod.py"

# subprocess.run() inherits this process's stdout handle directly, so a child's output reaches
# the terminal as soon as it's written - but our own print() calls sit in Python's stdout
# buffer until it's flushed. Without line-buffering, every section banner below prints out of
# order (all of them AFTER both tools' actual output, instead of each one right before its own
# tool runs) whenever stdout isn't a live terminal (a pipe, a CI log, a captured run) -
# confirmed by running this script and watching the banners land at the very end instead of
# interleaved.
sys.stdout.reconfigure(line_buffering=True)


def run_dotnet_test():
    print("=" * 70)
    print("dotnet test - Source/SeljukEmpire.Tests (reactive tactical AI logic)")
    print("=" * 70)

    dotnet = shutil.which("dotnet")
    if dotnet is None:
        print("SKIPPED: 'dotnet' not found on PATH.")
        return None  # not a failure - just unavailable on this machine

    if not TESTS_PROJECT.exists():
        print(f"SKIPPED: {TESTS_PROJECT} not found (older checkout?).")
        return None

    result = subprocess.run(
        [dotnet, "test", str(TESTS_PROJECT), "--nologo"],
        cwd=str(REPO_ROOT),
    )
    return result.returncode == 0


def run_verify_mod(extra_args):
    print()
    print("=" * 70)
    print("verify_mod.py - content integrity (18 checks)")
    print("=" * 70)

    python = sys.executable or "python3"
    result = subprocess.run([python, str(VERIFY_MOD)] + extra_args, cwd=str(REPO_ROOT))
    return result.returncode == 0


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--skip-dotnet-test", action="store_true", help="Only run verify_mod.py.")
    parser.add_argument("--skip-verify-mod", action="store_true", help="Only run dotnet test.")
    # Everything else (--game-path, --quick, --json, --check-workshop-conflicts, --update-baseline)
    # is forwarded to verify_mod.py as-is, so this script's own --help stays the source of truth
    # for its two flags above without having to duplicate verify_mod.py's whole argument list.
    args, forwarded = parser.parse_known_args()

    test_result = None
    verify_result = None

    if not args.skip_dotnet_test:
        test_result = run_dotnet_test()

    if not args.skip_verify_mod:
        verify_result = run_verify_mod(forwarded)

    print()
    print("=" * 70)
    print("SUMMARY")
    print("=" * 70)

    def fmt(name, result):
        if result is None:
            return f"  {name}: SKIPPED"
        return f"  {name}: {'PASS' if result else 'FAIL'}"

    print(fmt("dotnet test  ", test_result))
    print(fmt("verify_mod.py", verify_result))

    failed = (test_result is False) or (verify_result is False)
    if failed:
        print()
        print("run_all_checks: FAILED - see output above.")
        return 1

    print()
    print("run_all_checks: all checks that ran passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
