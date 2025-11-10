# 001. Monorepo Structure

Date: 2024-01-15
Status: Accepted

## Context

Aura Video Studio consists of multiple components: backend API (C#), web frontend (TypeScript/React), CLI tool, desktop app (WinUI), and shared core libraries. We needed to decide how to organize these components in version control.

Options considered were:
1. Multiple repositories (one per component)
2. Monorepo with all components together
3. Hybrid approach with separate repos for frontend/backend

Key factors influencing this decision:
- Ease of coordinated changes across frontend and backend
- Shared versioning and release management
- Developer onboarding experience
- CI/CD complexity
- Code sharing between components

## Decision

We will use a **monorepo structure** with all components in a single repository, organized by project:

```
/
├── Aura.Api/         # ASP.NET Core backend
├── Aura.Core/        # Shared domain logic
├── Aura.Providers/   # Provider implementations
├── Aura.Web/         # React frontend
├── Aura.Cli/         # CLI tool
├── Aura.App/         # WinUI desktop app
├── Aura.Tests/       # Shared tests
├── docs/             # Documentation
└── scripts/          # Build and deployment scripts
```

## Consequences

### Positive Consequences

- **Atomic commits**: Changes affecting multiple components can be committed together
- **Shared tooling**: Single CI/CD pipeline, shared scripts and configurations
- **Consistent versioning**: All components released together with matching versions
- **Easier refactoring**: Cross-component refactoring is straightforward
- **Simplified dependency management**: Internal dependencies are clear and co-located
- **Better discoverability**: Developers can see the entire system in one place
- **Reduced overhead**: No need to manage multiple repositories

### Negative Consequences

- **Larger repository size**: Clone time and disk space requirements increase
- **Potential for unrelated changes**: Commits may mix unrelated component changes
- **Build complexity**: Need to handle multiple build systems (.NET, Node.js)
- **Access control**: Cannot easily restrict access to specific components
- **Git history**: More commits and larger history to navigate

## Alternatives Considered

### Alternative 1: Multiple Repositories

**Description:** Separate repositories for backend, frontend, CLI, and desktop app.

**Pros:**
- Smaller, focused repositories
- Independent versioning per component
- Clearer component boundaries
- Better access control

**Cons:**
- Complex cross-repo changes requiring multiple PRs
- Difficult to maintain version compatibility
- Duplicated CI/CD configuration
- More overhead managing multiple repos
- Harder for new developers to understand system architecture

**Why Rejected:** The complexity of coordinating changes across repositories outweighed the benefits of separation. Most changes in Aura require modifications to both frontend and backend.

### Alternative 2: Hybrid Approach

**Description:** Backend components in one repo, frontend in another.

**Pros:**
- Some separation of concerns
- Different teams could own different repos
- Smaller than full monorepo

**Cons:**
- Still requires coordinating changes across repos
- API contract changes affect both repos
- Version synchronization issues
- Only partial benefit of separation

**Why Rejected:** Provides neither the full benefits of a monorepo nor the full separation of multiple repos. The worst of both worlds for this project's needs.

## References

- [Monorepo: Advantages and Disadvantages](https://www.atlassian.com/git/tutorials/monorepos)
- [Why Google Stores Billions of Lines of Code in a Single Repository](https://research.google/pubs/pub45424/)
- [Monorepos: Please don't!](https://medium.com/@mattklein123/monorepos-please-dont-e9a279be011b)

## Notes

This decision aligns with the project's goal of being beginner-friendly and easy to contribute to. A single repository reduces the cognitive overhead for new contributors and makes it easier to understand how components interact.

The monorepo structure is particularly beneficial for Aura because:
- The API contract between frontend and backend changes frequently during development
- Features often span multiple components (e.g., new video generation feature touches API, frontend, and core logic)
- The project is developed primarily by a small team where everyone works across the stack
