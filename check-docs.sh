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
test -f "${DOC_ROOT}/articles/index.html"
test -f "${DOC_ROOT}/articles/getting-started/index.html"
test -f "${DOC_ROOT}/articles/getting-started/installation/index.html"
test -f "${DOC_ROOT}/articles/getting-started/quick-start/index.html"
test -f "${DOC_ROOT}/articles/concepts/index.html"
test -f "${DOC_ROOT}/articles/concepts/library-architecture/index.html"
test -f "${DOC_ROOT}/articles/concepts/solver-and-curvature/index.html"
test -f "${DOC_ROOT}/articles/concepts/rendering-and-hit-testing/index.html"
test -f "${DOC_ROOT}/articles/guides/index.html"
test -f "${DOC_ROOT}/articles/guides/using-spline-core/index.html"
test -f "${DOC_ROOT}/articles/guides/demospline-workflow/index.html"
test -f "${DOC_ROOT}/articles/guides/json-format/index.html"
test -f "${DOC_ROOT}/articles/guides/editing-workflow/index.html"
test -f "${DOC_ROOT}/articles/guides/tuner-window/index.html"
test -f "${DOC_ROOT}/articles/advanced/index.html"
test -f "${DOC_ROOT}/articles/advanced/freehand-tracing/index.html"
test -f "${DOC_ROOT}/articles/advanced/curve-grid-and-tuning/index.html"
test -f "${DOC_ROOT}/articles/reference/index.html"
test -f "${DOC_ROOT}/articles/reference/packages/index.html"
test -f "${DOC_ROOT}/articles/reference/project-structure/index.html"
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

if find "${DOC_ROOT}/articles" -name '*.md' -print -quit | grep -q .; then
    echo "Generated docs still contain raw .md article outputs."
    find "${DOC_ROOT}/articles" -name '*.md' -print
    exit 1
fi

if ! search_generated_fixed 'Spline.Core' "${DOC_ROOT}/index.html" >/dev/null; then
    echo "Generated docs home page is missing Spline.Core content."
    exit 1
fi
