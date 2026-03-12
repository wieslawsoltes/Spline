---
title: Docs Site
description: Local and CI workflows for the Lunet documentation site.
---

# Docs Site

The repository documentation site is built with Lunet and lives under `site/`.

## Local commands

```bash
dotnet tool restore
bash ./build-docs.sh
```

To validate the generated output with repository-specific assertions:

```bash
bash ./check-docs.sh
```

To run a local dev server with watch mode:

```bash
bash ./serve-docs.sh
```

You can override the bind address and port with `DOCS_HOST` and `DOCS_PORT`.

## Deployment

The `docs.yml` GitHub Actions workflow builds the site and deploys
`site/.lunet/build/www` to GitHub Pages.
