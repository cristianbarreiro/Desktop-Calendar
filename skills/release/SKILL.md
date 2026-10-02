---
name: release
description: Release preparation, packaging, versioning, and deployment verification for Desktop Calendar.
---

# Release Decision Aid

Use only for an explicitly requested release or packaging task. An ordinary code fix does not require publishing or changing version metadata.

## Prepare

- Verify intended version, runtime, distribution type, and existing release workflow before packaging.
- Run Release build/tests and format checks; inspect working-tree status and generated package contents.
- Keep user databases and development artifacts out of release packages.
- Update release documentation to match what was built and validated.

Creating a tag, publishing artifacts, or changing the release version requires an explicit release request. Never report a package or release as verified unless it was produced and checked.
