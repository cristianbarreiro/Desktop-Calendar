---
name: documentation
description: Standards and guidelines for keeping project documentation synchronized with code changes.
---

# Documentation Skill

## Purpose
Ensure that whenever architectural, domain, or UI behaviors change, documentation is updated immediately as part of the Definition of Done.

## When to Use
- Implementing a new feature.
- Changing an architectural pattern or data boundary.
- Resolving technical debt or discovering a new constraint.

## Mapping Matrix
| Change Type | Documents to Update |
|-------------|---------------------|
| New Feature | `docs/product/requirements.md`, `docs/product/feature-map.md` |
| Architectural Shift | `docs/adr/`, `docs/architecture/overview.md` |
| UI/Theme Update | `docs/ui/design-system.md`, `docs/ui/interaction-rules.md` |
| New Domain Rule | `knowledge/domain.md` |
| Sprint Progress | `docs/project-state.md`, `docs/current-sprint.md`, `docs/development-log.md` |

## Validation
- Review updated markdown files for clarity, correct relative links, and absence of outdated claims.
