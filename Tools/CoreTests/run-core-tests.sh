#!/usr/bin/env bash
# Compiles BugReport.Core at Unity's language level and runs its NUnit tests
# without Unity. Requires the .NET 8 SDK. Usage: Tools/CoreTests/run-core-tests.sh
set -euo pipefail
cd "$(dirname "$0")"
dotnet test Tests/BugReport.Core.Tests.csproj --nologo -v quiet "$@"
