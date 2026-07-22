# -*- coding: utf-8 -*-
"""
Utility script to build the solution and run all tests.

Usage:
    python3 aider_buildcmd.py

The script assumes that the repository root contains a .sln file.
It will:
    1. Restore NuGet packages.
    2. Build the solution.
    3. Run `dotnet test` for the test projects.

If any step fails, the script exits with a non‑zero status code.
"""

import subprocess
import sys
from pathlib import Path


def run_cmd(command: list[str], cwd: Path | None = None) -> None:
    """Run a command, streaming its output to the console."""
    try:
        result = subprocess.run(
            command,
            cwd=cwd,
            check=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
        )
        print(result.stdout)
    except subprocess.CalledProcessError as exc:
        print(exc.stdout)
        sys.exit(exc.returncode)


def find_solution(root: Path) -> Path:
    """Locate the first *.sln file in the given directory."""
    sln_files = list(root.glob("*.sln"))
    if not sln_files:
        print("Error: No solution (.sln) file found in the repository root.", file=sys.stderr)
        sys.exit(1)
    return sln_files[0]


def main() -> None:
    repo_root = Path(__file__).resolve().parent

    # 1. Restore packages
    print("Restoring NuGet packages...")
    run_cmd(["dotnet", "restore"], cwd=repo_root)

    # 2. Build the solution
    sln_path = find_solution(repo_root)
    print(f"Building solution: {sln_path.name}")
    run_cmd(["dotnet", "build", str(sln_path), "-c", "Release", "--no-restore"], cwd=repo_root)

    # 3. Run tests
    print("Running tests...")
    run_cmd(["dotnet", "test", str(sln_path), "-c", "Release", "--no-build"], cwd=repo_root)

    print("\nAll steps completed successfully.")


if __name__ == "__main__":
    main()
