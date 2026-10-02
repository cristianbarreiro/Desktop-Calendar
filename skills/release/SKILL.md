---
name: release
description: Release preparation, packaging, versioning, and deployment verification for Desktop Calendar.
---

# Release Decision Aid

Use only for an explicitly requested release or packaging task. An ordinary code fix does not require publishing or changing version metadata.

## Prepare

- Verify intended version, runtime, distribution type, and existing release workflow before packaging.
- The local entry point is `scripts/build_installer.ps1`; `scripts/package.ps1` owns the packaging implementation and is reused by the release workflow. Use `README.md` for invocation options and outputs rather than duplicating them here.
- Select validation and clean scope deliberately; inspect working-tree status, artifact contents, checksums, and the manifest for the exact commit.
- Keep user databases and development artifacts out of release packages.
- Update release documentation to match what was built and validated.

Creating a tag, publishing artifacts, or changing the release version requires an explicit release request. Keep implementation, local validation, and published-release state distinct. Never report a package or release as verified unless it was produced and checked.
