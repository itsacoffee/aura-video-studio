# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records (ADRs) for Aura Video Studio. ADRs document significant architectural decisions made during the development of the project.

## What is an ADR?

An Architecture Decision Record (ADR) is a document that captures an important architectural decision made along with its context and consequences.

## ADR Format

Each ADR follows this structure:

```markdown
# [Number]. [Title]

Date: YYYY-MM-DD
Status: [Proposed | Accepted | Deprecated | Superseded]

## Context

What is the issue that we're seeing that is motivating this decision or change?

## Decision

What is the change that we're proposing and/or doing?

## Consequences

What becomes easier or more difficult to do because of this change?

### Positive Consequences

- Benefit 1
- Benefit 2

### Negative Consequences

- Drawback 1
- Drawback 2

## Alternatives Considered

What other options were considered?

## References

- Related documentation
- External resources
```

## Index of ADRs

### Accepted

- [ADR-001](./001-monorepo-structure.md) - Monorepo Structure
- [ADR-002](./002-aspnet-core-backend.md) - ASP.NET Core Backend
- ADR-003 - React + TypeScript Frontend
- ADR-004 - SQLite Database
- ADR-005 - Provider Abstraction Pattern
- [ADR-006](./006-server-sent-events.md) - Server-Sent Events for Real-Time Updates
- ADR-007 - Structured Logging with Serilog
- ADR-008 - Guided and Advanced Modes
- [ADR-009](./009-secrets-encryption.md) - Secrets Encryption Strategy
- ADR-010 - FFmpeg Integration

### Deprecated

None

### Superseded

None

## Creating a New ADR

1. Copy the template: `docs/adr/template.md`
2. Number it sequentially (e.g., `011-your-decision.md`)
3. Fill in all sections
4. Update this README with the new ADR
5. Submit as part of your PR

## When to Create an ADR

Create an ADR when:

- Making a significant architectural decision
- Choosing between multiple viable technical approaches
- Introducing a new technology or framework
- Changing a fundamental system design
- Making a decision that will be hard to reverse later

Don't create an ADR for:

- Minor implementation details
- Obvious or trivial choices
- Temporary workarounds
- Bug fixes (unless they require architectural changes)

## Reviewing ADRs

ADRs should be:

- Reviewed by the team before implementation
- Clear and concise
- Focused on the "why" not just the "what"
- Updated if the decision changes

## Additional Resources

- [Documenting Architecture Decisions](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
- [ADR GitHub Organization](https://adr.github.io/)
- [ADR Tools](https://github.com/npryce/adr-tools)
