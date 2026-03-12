#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

search_generated_regex() {
    local pattern="$1"
    shift

    if command -v rg >/dev/null 2>&1; then
        rg -n -g '*.html' -e "$pattern" "$@"
    else
        grep -R -n -E --include='*.html' "$pattern" "$@"
    fi
}

search_generated_fixed() {
    local text="$1"
    shift

    if command -v rg >/dev/null 2>&1; then
        rg -n -F "$text" "$@"
    else
        grep -R -n -F "$text" "$@"
    fi
}

"${SCRIPT_DIR}/build-docs.sh"

DOC_ROOT="${SCRIPT_DIR}/site/.lunet/build/www"

test -f "${DOC_ROOT}/index.html"
test -f "${DOC_ROOT}/api/index.html"
test -f "${DOC_ROOT}/api/Spline/index.html"
test -f "${DOC_ROOT}/api/Spline.Spline/index.html"
test -f "${DOC_ROOT}/api/Spline.Vec2/index.html"
test -f "${DOC_ROOT}/articles/index.html"
test -f "${DOC_ROOT}/articles/getting-started/index.html"
test -f "${DOC_ROOT}/articles/getting-started/overview/index.html"
test -f "${DOC_ROOT}/articles/getting-started/installation/index.html"
test -f "${DOC_ROOT}/articles/getting-started/quick-start/index.html"
test -f "${DOC_ROOT}/articles/getting-started/verification/index.html"
test -f "${DOC_ROOT}/articles/concepts/index.html"
test -f "${DOC_ROOT}/articles/concepts/library-architecture/index.html"
test -f "${DOC_ROOT}/articles/concepts/geometry-primitives/index.html"
test -f "${DOC_ROOT}/articles/concepts/curve-families-and-parameterization/index.html"
test -f "${DOC_ROOT}/articles/concepts/solver-and-curvature/index.html"
test -f "${DOC_ROOT}/articles/concepts/rendering-and-hit-testing/index.html"
test -f "${DOC_ROOT}/articles/guides/index.html"
test -f "${DOC_ROOT}/articles/guides/using-spline/index.html"
test -f "${DOC_ROOT}/articles/guides/demospline-workflow/index.html"
test -f "${DOC_ROOT}/articles/guides/rendering-and-svg/index.html"
test -f "${DOC_ROOT}/articles/guides/hit-testing-and-editing/index.html"
test -f "${DOC_ROOT}/articles/guides/curve-grid-and-serialization/index.html"
test -f "${DOC_ROOT}/articles/guides/json-format/index.html"
test -f "${DOC_ROOT}/articles/guides/editing-workflow/index.html"
test -f "${DOC_ROOT}/articles/guides/tuner-window/index.html"
test -f "${DOC_ROOT}/articles/guides/migration-from-spline-core/index.html"
test -f "${DOC_ROOT}/articles/advanced/index.html"
test -f "${DOC_ROOT}/articles/advanced/freehand-tracing/index.html"
test -f "${DOC_ROOT}/articles/advanced/curve-grid-and-tuning/index.html"
test -f "${DOC_ROOT}/articles/advanced/numerical-behavior/index.html"
test -f "${DOC_ROOT}/articles/advanced/testing-and-parity/index.html"
test -f "${DOC_ROOT}/articles/reference/index.html"
test -f "${DOC_ROOT}/articles/reference/package-and-assembly/index.html"
test -f "${DOC_ROOT}/articles/reference/packages/index.html"
test -f "${DOC_ROOT}/articles/reference/project-structure/index.html"
test -f "${DOC_ROOT}/articles/reference/api-coverage-index/index.html"
test -f "${DOC_ROOT}/articles/reference/testing-and-validation/index.html"
test -f "${DOC_ROOT}/articles/reference/ci-and-releases/index.html"
test -f "${DOC_ROOT}/articles/reference/docs-site/index.html"

if search_generated_regex 'href="[^"]*\.md([?#"][^"]*)?"' "${DOC_ROOT}" | grep -vE 'href="https?://' >/dev/null; then
    echo "Generated docs contain raw .md links."
    exit 1
fi

if search_generated_regex 'href="[^"]*/readme([?#"][^"]*)?"' "${DOC_ROOT}" >/dev/null; then
    echo "Generated docs contain /readme routes instead of directory routes."
    exit 1
fi

if search_generated_regex 'href="[^"]*/api/index\.md([?#"][^"]*)?"' "${DOC_ROOT}" >/dev/null; then
    echo "Generated docs contain stale /api/index.md links."
    exit 1
fi

if find "${DOC_ROOT}/articles" -name '*.md' -print -quit | grep -q .; then
    echo "Generated docs still contain raw .md article outputs."
    find "${DOC_ROOT}/articles" -name '*.md' -print
    exit 1
fi

if ! search_generated_fixed 'Package ID' "${DOC_ROOT}/articles/reference/package-and-assembly/index.html" >/dev/null; then
    echo "Generated docs package reference page is missing package metadata content."
    exit 1
fi

if ! search_generated_fixed 'Namespace' "${DOC_ROOT}/api/Spline/index.html" >/dev/null; then
    echo "Generated API namespace page is missing namespace content."
    exit 1
fi

if ! search_generated_fixed '/Spline/css/lite.css' "${DOC_ROOT}/api/Spline/index.html" >/dev/null; then
    echo "Generated API namespace page is missing CSS bundle links."
    exit 1
fi

if ! search_generated_fixed '/Spline/js/lite-defer.js' "${DOC_ROOT}/api/Spline/index.html" >/dev/null; then
    echo "Generated API namespace page is missing JS bundle links."
    exit 1
fi
