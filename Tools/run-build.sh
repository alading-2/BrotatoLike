#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

Tools/run-dataos-snapshot.sh
dotnet build BrotatoLike.slnx
