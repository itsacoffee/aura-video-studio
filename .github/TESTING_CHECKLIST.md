# Testing Checklist

Use this checklist when adding new features or making changes to ensure quality.

## Pre-Development

- [ ] Review existing tests for similar functionality
- [ ] Identify what needs to be tested
- [ ] Plan test scenarios (happy path, edge cases, errors)
- [ ] Consider test data requirements

## During Development

### Unit Tests

- [ ] Write tests for new functions/methods
- [ ] Test happy path scenarios
- [ ] Test edge cases
- [ ] Test error conditions
- [ ] Use descriptive test names
- [ ] Follow existing test patterns
- [ ] Use test data builders where appropriate

### Integration Tests

- [ ] Test API endpoints
- [ ] Test database operations
- [ ] Test external service integrations
- [ ] Test message queue operations
- [ ] Use test containers for isolation

### E2E Tests

- [ ] Test critical user journeys affected by changes
- [ ] Use Page Object Model
- [ ] Add appropriate waits and timeouts
- [ ] Handle async operations correctly
- [ ] Test error handling UI

## Before Committing

### Local Verification

- [ ] Run all tests locally: `dotnet test && npm test`
- [ ] Check test coverage: `./scripts/test/coverage-analysis.sh`
- [ ] Verify coverage meets 80% threshold
- [ ] Run linters: `npm run lint && dotnet format`
- [ ] Fix any linting errors

### Test Quality

- [ ] All new tests pass
- [ ] No tests skipped without justification
- [ ] Tests are isolated (don't depend on each other)
- [ ] Tests clean up after themselves
- [ ] Mock external dependencies appropriately
- [ ] Test data is realistic but not real

### Performance

- [ ] Tests run in reasonable time (< 5s for unit tests)
- [ ] No unnecessary delays or sleeps
- [ ] Parallel execution not blocked
- [ ] No resource leaks

## Before Creating PR

### Documentation

- [ ] Update test documentation if patterns changed
- [ ] Add comments for complex test scenarios
- [ ] Document any test data requirements
- [ ] Update README if test setup changed

### CI/CD

- [ ] Verify all CI checks pass
- [ ] Review test results in CI
- [ ] Check coverage reports
- [ ] Address any flaky tests

## Code Review Checklist

### Test Coverage

- [ ] New code has tests
- [ ] Coverage meets or exceeds 80%
- [ ] Critical paths are tested
- [ ] Edge cases are covered

### Test Quality

- [ ] Tests are clear and understandable
- [ ] Test names describe what they test
- [ ] Assertions are specific and meaningful
- [ ] No duplicate test code (DRY principle)
- [ ] Proper use of setup/teardown

### Test Maintainability

- [ ] Tests will be easy to update
- [ ] Test data is manageable
- [ ] Mocks are not overly complex
- [ ] Tests follow project conventions

## Post-Merge

### Monitoring

- [ ] Monitor CI for flaky tests
- [ ] Check coverage trends
- [ ] Review performance benchmarks
- [ ] Address any issues promptly

### Maintenance

- [ ] Update tests when requirements change
- [ ] Refactor tests when code refactored
- [ ] Remove obsolete tests
- [ ] Keep test utilities up to date

## Release Checklist

### Pre-Release

- [ ] All test suites pass
- [ ] E2E tests pass in staging environment
- [ ] Performance tests meet benchmarks
- [ ] No known flaky tests
- [ ] Coverage reports reviewed

### Post-Release

- [ ] Monitor production metrics
- [ ] Verify no regressions
- [ ] Update baseline performance metrics
- [ ] Document any test infrastructure changes

## Test Categories

### Must Test

- [ ] Business logic
- [ ] API endpoints
- [ ] Database operations
- [ ] Authentication/Authorization
- [ ] Error handling
- [ ] Critical user journeys

### Should Test

- [ ] Utility functions
- [ ] UI components
- [ ] State management
- [ ] Validation logic
- [ ] Integration points

### Nice to Test

- [ ] Edge cases
- [ ] Performance characteristics
- [ ] Accessibility features
- [ ] Responsive behavior

## Anti-Patterns to Avoid

- [ ] ❌ Tests that test framework behavior
- [ ] ❌ Tests with no assertions
- [ ] ❌ Tests that depend on execution order
- [ ] ❌ Tests with hardcoded timeouts
- [ ] ❌ Tests that modify global state
- [ ] ❌ Tests that are overly complex
- [ ] ❌ Tests that test implementation details
- [ ] ❌ Flaky tests that pass/fail randomly

## Quality Metrics

### Current Status

- Backend Coverage: [ ]% (Target: 80%)
- Frontend Coverage: [ ]% (Target: 80%)
- Integration Coverage: [ ]% (Target: 80%)
- Flaky Test Rate: [ ]% (Target: < 2%)
- Test Execution Time: [ ] minutes (Target: < 10m)

### Goals

- [ ] Maintain 80% coverage
- [ ] Zero flaky tests
- [ ] Fast test execution (< 10 minutes)
- [ ] 100% critical path coverage
- [ ] Clear, maintainable tests

## Resources

- [Testing Quick Start](../TESTING_QUICK_START.md)
- [Test Coverage Strategy](../docs/testing/TEST_COVERAGE_STRATEGY.md)
- [E2E Testing Guide](../E2E_TESTING_GUIDE.md)
- [Test Writing Guide](../docs/testing/TEST_WRITING_GUIDE.md)
- [Mocking Guide](../docs/testing/MOCKING_GUIDE.md)

---

**Remember**: Quality is not an act, it is a habit. Write tests as you write code, not after!
