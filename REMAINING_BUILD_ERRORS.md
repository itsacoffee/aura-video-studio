# Remaining Build Errors - Future Work

This document tracks the remaining ~33 build errors that are **pre-existing** and **unrelated** to the PR #198 API/model mismatch issues.

## Status
- ✅ **Original PR #198 Issues**: All ~20 API/model mismatch errors have been fixed
- ⚠️ **Pre-existing Errors**: ~33 errors remain (existed before PR #198)
- These errors are present in both Windows and Linux builds
- They are not related to the Electron installer or recent architectural changes

## Remaining Error Categories

### 1. SecurityServicesExtensions.cs (2 errors)
**File**: `Aura.Api/Startup/SecurityServicesExtensions.cs`

**Issue 1 - Line 104**: Missing `CookiePolicyOptions` type
```
error CS0234: The type or namespace name 'CookiePolicyOptions' does not exist in the namespace 'Microsoft.AspNetCore.Http'
```
**Possible Fix**: 
- Update ASP.NET Core package version
- Or remove/replace cookie policy configuration

**Issue 2 - Line 147**: Missing `UseHttpsEnforcement` extension method
```
error CS1061: 'IApplicationBuilder' does not contain a definition for 'UseHttpsEnforcement'
```
**Possible Fix**:
- Implement custom middleware
- Or remove if not needed

### 2. ResilienceServicesExtensions.cs (2 errors)
**File**: `Aura.Api/Startup/ResilienceServicesExtensions.cs`

**Issue - Lines 55 & 84**: `IHttpClientBuilder.AddResiliencePipeline` method signature mismatch
```
error CS1929: 'IHttpClientBuilder' does not contain a definition for 'AddResiliencePipeline'
```
**Possible Fix**:
- Update Polly/Microsoft.Extensions.Http.Resilience packages
- Check API changes in newer versions
- May need to adjust resilience pipeline configuration syntax

### 3. SystemRequirementsController.cs (~25 errors)
**File**: `Aura.Api/Controllers/SystemRequirementsController.cs`

**Issue - Multiple lines**: Anonymous object property access issues
```
error CS1061: 'object' does not contain a definition for 'compatible'/'status'/etc.
```
**Possible Fix**:
- Define proper DTOs/record types instead of anonymous objects
- Use explicit typing in controller returns
- Refactor to use strongly-typed responses

### 4. Aura.App/Aura.App.csproj (Multiple errors)
**Issue**: XAML compiler errors on Linux
```
error MSB3073: XamlCompiler.exe exited with code 126
```
**Note**: This is expected on Linux as it's a Windows-specific WinUI3 project
**Fix**: Not needed - project should only build on Windows

## Recommended Approach for Future PRs

### High Priority
1. **SystemRequirementsController**: Refactor to use strongly-typed DTOs
   - Create proper response models
   - Replace anonymous objects
   - Impact: ~25 errors

### Medium Priority
2. **ResilienceServicesExtensions**: Update Polly integration
   - Review package versions
   - Update to current API syntax
   - Impact: 2 errors

3. **SecurityServicesExtensions**: Review security middleware
   - Implement or remove UseHttpsEnforcement
   - Fix CookiePolicyOptions reference
   - Impact: 2 errors

### Low Priority (Expected Behavior)
4. **Aura.App XAML Errors**: These are expected on Linux builds
   - WinUI3 project only builds on Windows
   - Can be ignored for cross-platform builds

## Testing Recommendations

Before addressing these errors:
1. Confirm they exist on a clean Windows build
2. Check if they're flagged by CI/CD
3. Determine if they're blocking actual functionality
4. Some may be warnings treated as errors that could be downgraded

## Notes

- These errors were present before PR #198 and the Electron installer work
- They don't affect the core video generation pipeline
- They may be in optional/administrative features
- Consider whether the affected features are actively used before investing time

## Related Issues

- Consider creating separate GitHub issues for each category
- Link to this document from those issues
- Track progress independently from the PR #198 work
