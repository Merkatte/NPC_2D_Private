#!/usr/bin/env sh
set -eu

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$script_dir"
exec dotnet run --project "$script_dir/Tools/NpcHarness/NpcHarness.csproj" -- "$@"
