# Deployment Validation Checklist

Use this checklist to ensure all components are properly integrated and working before deploying to production.

## Pre-Deployment Checks

### 1. Build and Compilation ✓
- [ ] Solution builds without errors
  ```bash
  dotnet build --configuration Release
  ```
- [ ] All projects compile successfully
- [ ] No critical warnings in build output

### 2. Unit Tests ✓
- [ ] All unit tests pass
  ```bash
  dotnet test --filter "FullyQualifiedName!~Integration&FullyQualifiedName!~E2E"
  ```
- [ ] Test coverage meets requirements (target: >80%)
- [ ] No skipped tests without documentation

### 3. Smoke Tests ✓
- [ ] All smoke tests pass
  ```bash
  dotnet test --filter "FullyQualifiedName~Smoke"
  ```
- [ ] System initializes without errors
- [ ] Core services are accessible
- [ ] RuleBased provider works offline

### 4. Integration Tests ✓
- [ ] All integration tests pass
  ```bash
  dotnet test --filter "FullyQualifiedName~Integration"
  ```
- [ ] AI Quality System tests pass
- [ ] LLM Provider integration tests pass
- [ ] End-to-end workflow tests pass
- [ ] Content quality pipeline tests pass
- [ ] Multi-provider consistency tests pass

### 5. Performance Benchmarks ✓
- [ ] Performance tests meet thresholds
  ```bash
  dotnet test --filter "FullyQualifiedName~Performance"
  ```
- [ ] Prompt generation < 50ms
- [ ] Quality analysis < 5 seconds
- [ ] Memory usage within limits

## System Health Validation

### 6. Health Check API ✓
- [ ] Health endpoint responds
  ```bash
  curl http://localhost:5000/api/diagnostics/health
  ```
- [ ] All health checks report healthy status
- [ ] Response time < 3 seconds

### 7. Configuration Validation ✓
- [ ] Configuration endpoint accessible
  ```bash
  curl http://localhost:5000/api/diagnostics/configuration
  ```
- [ ] All required services configured
- [ ] LLM providers properly registered
- [ ] Content advisor initialized

### 8. System Integrity ✓
- [ ] System integrity validation passes
  ```bash
  POST /api/diagnostics/run-integration-tests
  ```
- [ ] All components properly wired
- [ ] Dependencies correctly injected
- [ ] Prompt templates functional

## LLM Provider Validation

### 9. Local Provider (RuleBased) ✓
- [ ] RuleBased provider accessible
- [ ] Generates valid scripts
- [ ] Works without external dependencies
- [ ] Performance acceptable

### 10. External Providers (if configured) ⚠️
Test each configured provider:
- [ ] OpenAI provider works (if configured)
  ```bash
  POST /api/diagnostics/test-provider
  {"providerName": "OpenAI"}
  ```
- [ ] Azure OpenAI works (if configured)
- [ ] Ollama works (if configured)
- [ ] Gemini works (if configured)

## Quality System Validation

### 11. EnhancedPromptTemplates ✓
- [ ] Script generation prompts valid
- [ ] Visual selection prompts valid
- [ ] Quality validation prompts valid
- [ ] All tones supported
- [ ] Prompt generation performant

### 12. IntelligentContentAdvisor ✓
- [ ] Quality analysis functional
- [ ] Detects low-quality content
- [ ] Approves high-quality content
- [ ] Provides actionable feedback
- [ ] Quality threshold (75) enforced

### 13. Quality Pipeline ✓
- [ ] Analysis → regeneration loop works
- [ ] Quality scores consistent
- [ ] Poor content prevented from progressing
- [ ] Suggestions are actionable

## API Endpoints Validation

### 14. Core Endpoints ✓
- [ ] All API endpoints respond
- [ ] Authentication working (if required)
- [ ] CORS configured correctly
- [ ] Rate limiting active (if configured)

### 15. Diagnostics Endpoints ✓
- [ ] GET /api/diagnostics/health ✓
- [ ] POST /api/diagnostics/test-provider ✓
- [ ] POST /api/diagnostics/test-quality-analysis ✓
- [ ] GET /api/diagnostics/configuration ✓
- [ ] POST /api/diagnostics/run-integration-tests ✓

## Security Checks

### 16. API Keys and Secrets ✓
- [ ] No API keys in source code
- [ ] Secrets properly configured
- [ ] Environment variables set
- [ ] Key rotation documented

### 17. Dependencies ✓
- [ ] No known vulnerabilities
  ```bash
  dotnet list package --vulnerable
  ```
- [ ] All packages up to date
- [ ] License compliance verified

## Documentation

### 18. Documentation Complete ✓
- [ ] Integration Testing Guide available
- [ ] Troubleshooting Guide available
- [ ] API documentation updated
- [ ] Deployment instructions current

### 19. Change Log ✓
- [ ] CHANGELOG.md updated
- [ ] Breaking changes documented
- [ ] Migration guide (if needed)

## Environment-Specific Checks

### 20. Development Environment ✓
- [ ] Local development works
- [ ] Hot reload functional
- [ ] Debug mode available

### 21. Staging Environment ⚠️
- [ ] Staging deployment successful
- [ ] Integration tests pass on staging
- [ ] Performance acceptable on staging
- [ ] Load testing completed

### 22. Production Environment ⚠️
- [ ] Production deployment plan reviewed
- [ ] Rollback procedure documented
- [ ] Monitoring configured
- [ ] Alerts set up

## Post-Deployment Verification

### 23. Immediate Verification ✓
Within 5 minutes of deployment:
- [ ] Application starts successfully
- [ ] Health checks pass
- [ ] No critical errors in logs
- [ ] Key endpoints responding

### 24. Smoke Test Verification ✓
Within 15 minutes of deployment:
- [ ] Run smoke tests against production
  ```bash
  dotnet test --filter "FullyQualifiedName~Smoke" --logger "console;verbosity=normal"
  ```
- [ ] All smoke tests pass
- [ ] Response times acceptable

### 25. Integration Test Verification ⚠️
Within 1 hour of deployment:
- [ ] Run integration tests (if safe for production)
- [ ] Monitor error rates
- [ ] Check performance metrics
- [ ] Verify quality system functioning

### 26. Monitoring and Alerts ✓
- [ ] Application logs flowing
- [ ] Metrics being collected
- [ ] Alerts triggering correctly
- [ ] Dashboard accessible

## Rollback Criteria

Rollback if any of these occur:
- [ ] Critical security vulnerability discovered
- [ ] Application won't start
- [ ] Health checks consistently failing
- [ ] Error rate > 5%
- [ ] Response time > 2x normal
- [ ] Data corruption detected
- [ ] Critical feature broken

## Sign-Off

- [ ] Developer sign-off
  - Name: ________________
  - Date: ________________
  
- [ ] QA sign-off
  - Name: ________________
  - Date: ________________
  
- [ ] DevOps sign-off
  - Name: ________________
  - Date: ________________

## Quick Commands Reference

### Pre-Deployment
```bash
# Full test suite
dotnet test

# Smoke tests only
dotnet test --filter "FullyQualifiedName~Smoke"

# Integration tests only
dotnet test --filter "FullyQualifiedName~Integration"

# Performance tests
dotnet test --filter "FullyQualifiedName~Performance"
```

### Health Checks
```bash
# System health
curl http://localhost:5000/api/diagnostics/health

# Configuration status
curl http://localhost:5000/api/diagnostics/configuration

# Run all diagnostics
curl -X POST http://localhost:5000/api/diagnostics/run-integration-tests
```

### Provider Testing
```bash
# Test specific provider
curl -X POST http://localhost:5000/api/diagnostics/test-provider \
  -H "Content-Type: application/json" \
  -d '{"providerName":"RuleBased"}'

# Test quality analysis
curl -X POST http://localhost:5000/api/diagnostics/test-quality-analysis \
  -H "Content-Type: application/json" \
  -d '{"script":"Test script","topic":"Test","tone":"informative"}'
```

## Notes

- ✓ = Must complete before deployment
- ⚠️ = Complete if applicable to environment
- All automated tests should be run as part of CI/CD pipeline
- Manual verification steps should be documented with results
- Keep this checklist updated as system evolves

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-10-24 | Initial deployment checklist |

## Additional Resources

- [Integration Testing Guide](./INTEGRATION_TESTING_GUIDE.md)
- Troubleshooting Guide (archived)
- AI Quality Implementation Summary (archived)
