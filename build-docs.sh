#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOCK_DIR="${SCRIPT_DIR}/site/.lunet/.build-lock"

cleanup() {
    rmdir "${LOCK_DIR}" 2>/dev/null || true
}

trap cleanup EXIT

cd "${SCRIPT_DIR}"
mkdir -p "${SCRIPT_DIR}/site/.lunet"
while ! mkdir "${LOCK_DIR}" 2>/dev/null; do
    sleep 1
done

dotnet tool restore
rm -rf "${SCRIPT_DIR}/site/.lunet/build"

cd "${SCRIPT_DIR}/site"
dotnet tool run lunet --stacktrace build
