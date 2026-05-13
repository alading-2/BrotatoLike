#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
workspace_root="$(cd "$repo_root/../.." && pwd)"
framework_project="$workspace_root/SkilmeAI/GameOS/SkilmeAI.GameOS.csproj"
cd "$repo_root"

Tools/run-dataos-snapshot.sh
SkilmeAIGameOSProject="$framework_project" dotnet build BrotatoLike.csproj
