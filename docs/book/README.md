# Reference documentation — PDF source of truth

The **Mirth Connect user guide** is the sole requirements source for SmartConnect traceability.

## Canonical file (Git LFS)

Place the integration platform user guide at this **exact** path:

`docs/book/mirth-connect-user-guide.pdf`

### One-time setup

1. Install [Git LFS](https://git-lfs.com/) and run `git lfs install` once per machine.
2. Tracking for this path is already declared in [`.gitattributes`](../../.gitattributes) (`filter=lfs`).
3. Copy your licensed PDF to `docs/book/mirth-connect-user-guide.pdf`, then `git add` and commit. Do **not** keep alternate copies for traceability; TOC extraction and the matrix read **only** this path.

### Git LFS on the canonical remote (CI)

GitHub Actions materializes LFS objects only when the PDF is committed as an LFS object and the workflow uses `lfs: true` (already set in `.github/workflows/smartconnect-pdf-sot.yml`).

After cloning or before opening a PR:

1. `git lfs install` (once per machine).
2. Confirm tracking: `git lfs ls-files` must list `docs/book/mirth-connect-user-guide.pdf`.
3. Confirm the working tree file is **not** a pointer stub: `wc -c docs/book/mirth-connect-user-guide.pdf` should show a large size (megabytes), not a few hundred bytes. If it is tiny, run `git lfs pull`.
4. Run the same checks as CI: `./scripts/verify-smartconnect-pdf-sot.sh` from the repository root.
5. Push your branch; resolve any workflow failures by keeping `guide-toc.json` and `guide-traceability.md` in sync with the PDF (run `extract_pdf_toc.py` then `generate_traceability_md.py` after PDF changes).

**Licensing:** only commit the PDF if your license allows storing it in this repository.

### Machine-readable TOC

After the PDF is present, generate the outline index:

```bash
python3 -m pip install -r ../../tools/smartconnect/requirements.txt
python3 ../../tools/smartconnect/extract_pdf_toc.py
python3 ../../tools/smartconnect/generate_traceability_md.py
```

Outputs:

- `docs/book/guide-toc.json` — outline nodes: `id`, `title`, `level`, `page`.
- `docs/smartconnect/guide-traceability.md` — regenerated table (merge manual columns via `docs/smartconnect/traceability-overrides.json`).

See [`tools/smartconnect/README.md`](../../tools/smartconnect/README.md) for dependencies and CI behavior.
