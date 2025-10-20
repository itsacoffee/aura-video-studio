# Portable Distribution - End-to-End Test Checklist

## Overview
This checklist consolidates all test requirements for the portable distribution and provides a comprehensive verification framework. Use this as the master checklist before releasing a portable build.

## Test Environment Requirements

### Hardware Requirements
- [ ] Windows 10 (64-bit) VM or physical machine
- [ ] Windows 11 (64-bit) VM or physical machine
- [ ] Minimum 4 GB RAM
- [ ] 10 GB free disk space
- [ ] Optional: NVIDIA GPU for Stable Diffusion testing

### Software Requirements
- [ ] No .NET Runtime pre-installed (test self-contained)
- [ ] No FFmpeg pre-installed
- [ ] Web browser (Edge, Chrome, or Firefox)
- [ ] No development tools installed

---

## 1. Build Verification Tests

### Build Script Functionality
- [ ] `build-portable.ps1` executes without errors
- [ ] .NET projects build successfully (Core, Providers, API)
- [ ] Web UI builds successfully
- [ ] Self-contained runtime included in API output
- [ ] Web UI copied to `Api\wwwroot\` correctly
- [ ] Launcher script (`start_portable.cmd`) created
- [ ] Version.json generated with correct metadata
- [ ] Checksums.txt generated for all files
- [ ] SBOM (sbom.json) generated
- [ ] Attributions.txt created
- [ ] ZIP archive created successfully
- [ ] ZIP checksum (SHA-256) generated

### Build Artifacts Verification
- [ ] Portable ZIP size is reasonable (< 150 MB without FFmpeg)
- [ ] Extracted folder structure matches specification
- [ ] All required folders present (Api, Tools, AuraData, etc.)
- [ ] README.md present at root
- [ ] version.json contains correct version info
- [ ] No debug files or PDB files (Release build)

**Expected Folder Structure:**
```
AuraVideoStudio_Portable_x64/
├── Api/
│   ├── Aura.Api.exe
│   ├── wwwroot/
│   └── [runtime files]
├── Tools/              (empty initially)
├── AuraData/
│   └── README.txt
├── Logs/               (empty, created on first run)
├── Projects/           (empty initially)
├── Downloads/          (empty initially)
├── ffmpeg/             (optional, if pre-bundled)
├── start_portable.cmd
├── README.md
├── version.json
├── checksums.txt
├── sbom.json
├── attributions.txt
└── LICENSE
```

---

## 2. Quick Demo Verification (QuickDemoVerification.md)

### Extraction and First Launch
- [ ] ZIP extracts without errors
- [ ] All files and folders present
- [ ] No corrupt files (verify checksums)
- [ ] `start_portable.cmd` launches API successfully
- [ ] API health check passes (within 10 seconds)
- [ ] Browser opens automatically to http://127.0.0.1:5005
- [ ] Web UI loads without 404 errors
- [ ] No console errors in browser DevTools

### First-Run Wizard
- [ ] Welcome screen displays correctly
- [ ] System status shows hardware detection
- [ ] CPU detected and displayed
- [ ] RAM detected and displayed
- [ ] GPU detected (if available) or "No GPU" shown
- [ ] FFmpeg shows "Not Installed" initially
- [ ] First-Time Setup or Get Started button present

### FFmpeg Setup (Quick Path)
- [ ] Download Center accessible
- [ ] FFmpeg card shows "Not Installed"
- [ ] "Install" button available
- [ ] Click Install starts download
- [ ] Progress bar shows download percentage
- [ ] Download speed and ETA displayed
- [ ] Download completes (1-3 minutes)
- [ ] Extraction automatic
- [ ] FFmpeg card updates to "Installed"
- [ ] Version displayed correctly
- [ ] Path shown: `Tools\ffmpeg\bin\ffmpeg.exe`

### First Video Generation
- [ ] Navigate to Create page
- [ ] Wizard Step 1 loads
- [ ] Default values present
- [ ] Enter topic: "Test Video"
- [ ] Click "Next" to Step 2
- [ ] Step 2 loads with defaults
- [ ] Click "Next" to Step 3
- [ ] Step 3 loads with profile options
- [ ] Select "Free-Only" profile
- [ ] Click "Run Preflight Check"
- [ ] Preflight passes with Free providers
- [ ] "Generate Video" button enabled
- [ ] Click "Generate Video"
- [ ] Generation progress shown
- [ ] All stages complete (5 stages)
- [ ] Video file created in Projects folder
- [ ] Video file is playable
- [ ] Audio narration present (Windows SAPI)
- [ ] Duration matches request (~1 minute)
- [ ] Resolution is 1920x1080 (or selected)

### Persistence and Restart
- [ ] Close API server
- [ ] Restart via `start_portable.cmd`
- [ ] API starts successfully
- [ ] FFmpeg still shows "Installed"
- [ ] Generated video still in Projects folder
- [ ] Settings preserved
- [ ] No errors in logs

**Quick Demo Time Budget:** 5-10 minutes total

---

## 3. Wizard End-to-End Tests (WizardEndToEndTests.md)

### Navigation Tests (3 tests)
- [ ] Test 1.1: Forward Navigation
- [ ] Test 1.2: Backward Navigation
- [ ] Test 1.3: Step Indicator Navigation

### Default Values Tests (3 tests)
- [ ] Test 2.1: Step 1 Defaults
- [ ] Test 2.2: Step 2 Defaults
- [ ] Test 2.3: Brand Kit Defaults

### Persistence Tests (3 tests)
- [ ] Test 3.1: Persistence Across Refresh
- [ ] Test 3.2: Persistence Across Browser Restart
- [ ] Test 3.3: Independent Setting Storage

### Profile Selection Tests (4 tests)
- [ ] Test 4.1: Free-Only Profile
- [ ] Test 4.2: Pro-Basic Profile
- [ ] Test 4.3: Pro-Max Profile
- [ ] Test 4.4: Custom Profile

### Preflight Check Tests (4 tests)
- [ ] Test 5.1: Passing Preflight
- [ ] Test 5.2: Failing Preflight
- [ ] Test 5.3: Preflight Override
- [ ] Test 5.4: Preflight Re-run

### Validation Tests (4 tests)
- [ ] Test 6.1: Required Field Validation
- [ ] Test 6.2: Field Format Validation
- [ ] Test 6.3: Conditional Validation
- [ ] Test 6.4: Real-time Validation

### Error Handling Tests (4 tests)
- [ ] Test 7.1: Network Error During Generation
- [ ] Test 7.2: API Error Response
- [ ] Test 7.3: Insufficient Disk Space
- [ ] Test 7.4: Timeout Handling

### Configuration Save Tests (3 tests)
- [ ] Test 8.1: Save to Profile
- [ ] Test 8.2: Export Configuration
- [ ] Test 8.3: Settings Migration

**Wizard Tests Total:** 28 tests

---

## 4. Error Path Tests (ErrorPathTests.md)

### Network Error Scenarios (4 tests)
- [ ] Test 1.1: Network Disconnection During Download
- [ ] Test 1.2: API Endpoint Unreachable
- [ ] Test 1.3: Timeout During API Call
- [ ] Test 1.4: DNS Resolution Failure

### File System Error Scenarios (5 tests)
- [ ] Test 2.1: Insufficient Disk Space
- [ ] Test 2.2: File Already Exists
- [ ] Test 2.3: Read-Only File System
- [ ] Test 2.4: File Path Too Long
- [ ] Test 2.5: File Locked by Another Process

### Insufficient Permissions Scenarios (3 tests)
- [ ] Test 3.1: No Admin Rights for System Dependency
- [ ] Test 3.2: Antivirus Blocking Executable
- [ ] Test 3.3: Firewall Blocking Network Access

### Invalid Input Scenarios (4 tests)
- [ ] Test 4.1: Corrupted Input File
- [ ] Test 4.2: Unsupported File Format
- [ ] Test 4.3: Invalid API Key Format
- [ ] Test 4.4: Invalid URL Format

### Dependency Failure Scenarios (4 tests)
- [ ] Test 5.1: FFmpeg Executable Corrupted
- [ ] Test 5.2: Missing FFmpeg Dependencies (DLL)
- [ ] Test 5.3: API Service Unavailable (500 Error)
- [ ] Test 5.4: Rate Limit Exceeded

### Resource Exhaustion Scenarios (3 tests)
- [ ] Test 6.1: Out of Memory During Generation
- [ ] Test 6.2: GPU Memory Exceeded
- [ ] Test 6.3: CPU Overheating/Throttling

### Recovery and Retry Scenarios (4 tests)
- [ ] Test 7.1: Resume Interrupted Download
- [ ] Test 7.2: Retry Failed Generation
- [ ] Test 7.3: Recover from Application Crash
- [ ] Test 7.4: Auto-Save and Restore

**Error Path Tests Total:** 27 tests

---

## 5. FFmpeg Detection Tests (FFMPEG_DETECTION_TEST_PLAN.md)

### Detection Scenarios (4 tests)
- [ ] Test 1.1: No FFmpeg Detected (Clean System)
- [ ] Test 1.2: FFmpeg in System PATH
- [ ] Test 1.3: FFmpeg in Portable Folder (Pre-bundled)
- [ ] Test 1.4: FFmpeg in Tools Folder (Downloaded)

### Installation Methods (2 tests)
- [ ] Test 2.1: Download and Install via UI
- [ ] Test 2.2: Resume Interrupted Download

### Attachment Methods (3 tests)
- [ ] Test 3.1: Manual Copy + Rescan Workflow
- [ ] Test 3.2: Attach Existing via Absolute Path
- [ ] Test 3.3: Attach via Directory Path

### Priority and Fallback (2 tests)
- [ ] Test 4.1: Detection Priority Order
- [ ] Test 4.2: Fallback When Primary Not Found

### API Endpoint Tests (3 tests)
- [ ] Test 5.1: POST /api/downloads/ffmpeg/rescan
- [ ] Test 5.2: POST /api/downloads/ffmpeg/attach
- [ ] Test 5.3: GET /api/downloads/ffmpeg/status

**FFmpeg Tests Total:** 14 tests

---

## 6. AI Orchestration Tests (AIOrchestrationTests.md)

### Multi-Component Generation Tests (4 tests)
- [ ] Test 1.1: Full AI Pipeline (Pro-Max Profile)
- [ ] Test 1.2: Parallel Component Processing
- [ ] Test 1.3: Adaptive Quality Adjustment
- [ ] Test 1.4: Long-Form Content Generation (10+ min)

### Resource Optimization Tests (4 tests)
- [ ] Test 2.1: CPU Utilization Optimization
- [ ] Test 2.2: GPU Memory Management
- [ ] Test 2.3: Network Bandwidth Optimization
- [ ] Test 2.4: Disk I/O Optimization

### Component Failure Recovery Tests (4 tests)
- [ ] Test 3.1: Script Generation Failure Recovery
- [ ] Test 3.2: TTS Synthesis Failure Recovery
- [ ] Test 3.3: Visual Generation Failure Recovery
- [ ] Test 3.4: Cascade Failure Handling

### Quality and Performance Tests (3 tests)
- [ ] Test 4.1: Output Quality Comparison (Free vs Pro-Basic vs Pro-Max)
- [ ] Test 4.2: Performance Benchmark (All profiles x durations)
- [ ] Test 4.3: Scalability Testing (Multiple generations)

### System Integration Tests (3 tests)
- [ ] Test 5.1: End-to-End Pipeline Integration
- [ ] Test 5.2: Provider Ecosystem Integration
- [ ] Test 5.3: External Service Integration

**AI Orchestration Tests Total:** 18 tests

---

## 7. Security and CodeQL Checks

### Security Validation
- [ ] Run CodeQL security scan
- [ ] No critical vulnerabilities detected
- [ ] No high-severity issues in changed code
- [ ] All security alerts reviewed and addressed
- [ ] Security summary documented

### Code Quality
- [ ] No hardcoded secrets or API keys
- [ ] Input validation present for all user inputs
- [ ] Path traversal vulnerabilities checked
- [ ] SQL injection risks (if applicable)
- [ ] XSS vulnerabilities reviewed

---

## 8. Documentation Verification

### User Documentation
- [ ] README.md accurate and up-to-date
- [ ] PORTABLE.md describes portable structure correctly
- [ ] BUILD_AND_RUN.md has correct build instructions
- [ ] QuickDemoVerification.md is complete
- [ ] All screenshots current (if applicable)

### Test Documentation
- [ ] WizardEndToEndTests.md complete
- [ ] ErrorPathTests.md comprehensive
- [ ] AIOrchestrationTests.md detailed
- [ ] FFMPEG_DETECTION_TEST_PLAN.md updated
- [ ] All test procedures verified

### Technical Documentation
- [ ] version.json schema documented
- [ ] Auto-update mechanism described
- [ ] Dependency bundling options explained
- [ ] API endpoint changes documented

---

## 9. Cross-Platform Considerations

### Windows 10 Testing
- [ ] Tested on clean Windows 10 VM
- [ ] All features work correctly
- [ ] No Windows 10-specific issues
- [ ] Compatible with Windows Defender

### Windows 11 Testing
- [ ] Tested on clean Windows 11 VM
- [ ] All features work correctly
- [ ] Compatible with Windows 11 security features
- [ ] Runs without compatibility mode

### Edge Cases
- [ ] Long paths (>260 characters) handled
- [ ] Special characters in paths handled
- [ ] Non-English Windows locale tested
- [ ] Limited user account tested (non-admin)

---

## 10. Performance Benchmarks

### Generation Performance
Record actual times:

| Profile | Duration | Expected Time | Actual Time | Pass? |
|---------|----------|---------------|-------------|-------|
| Free | 1 min | 30-60s | ___s | ☐ |
| Free | 3 min | 90-150s | ___s | ☐ |
| Free | 5 min | 150-300s | ___s | ☐ |
| Pro-Basic | 1 min | 60-120s | ___s | ☐ |
| Pro-Basic | 3 min | 180-300s | ___s | ☐ |
| Pro-Max | 1 min | 120-180s | ___s | ☐ |
| Pro-Max | 3 min | 300-600s | ___s | ☐ |

### Resource Usage
Record peak usage:

| Metric | Free Profile | Pro-Basic | Pro-Max | Acceptable? |
|--------|--------------|-----------|---------|-------------|
| CPU Peak | ___% | ___% | ___% | < 95% |
| RAM Peak | ___ GB | ___ GB | ___ GB | < 80% available |
| GPU VRAM | N/A | N/A | ___ GB | < 95% |
| Disk I/O | ___ MB/s | ___ MB/s | ___ MB/s | No bottleneck |

---

## 11. Integration Testing

### Full Workflow Integration
- [ ] Welcome → Setup → Create → Generate → View (complete flow)
- [ ] Settings → API Keys → Provider Selection → Generate
- [ ] Download Center → Install Deps → Configure → Generate
- [ ] Error → Recovery → Retry → Success

### Data Persistence
- [ ] Settings persist across restarts
- [ ] Generated videos preserved
- [ ] Dependency installations remembered
- [ ] Wizard state saves automatically
- [ ] Logs accumulate correctly

### System Interactions
- [ ] File associations work (if applicable)
- [ ] Windows Explorer integration
- [ ] Default browser opens correctly
- [ ] System tray integration (if applicable)

---

## 12. Release Readiness Checklist

### Pre-Release
- [ ] All critical tests passed
- [ ] No known critical bugs
- [ ] Performance within acceptable ranges
- [ ] Documentation complete and accurate
- [ ] Build artifacts verified
- [ ] Checksums validated

### Release Package
- [ ] Portable ZIP created
- [ ] ZIP checksum (SHA-256) generated
- [ ] SBOM included
- [ ] Attributions complete
- [ ] License file present
- [ ] version.json correct

### Post-Release Verification
- [ ] Test download from release location
- [ ] Extract and verify on fresh system
- [ ] Quick Demo passes on clean install
- [ ] No unexpected issues reported

---

## Test Summary

### Overall Statistics
```
Total Test Count: 110+ tests

Build Verification:       [ ] / 12 tests
Quick Demo:              [ ] / 10 steps
Wizard End-to-End:       [ ] / 28 tests
Error Path:              [ ] / 27 tests
FFmpeg Detection:        [ ] / 14 tests
AI Orchestration:        [ ] / 18 tests
Security & CodeQL:       [ ] / 5 checks
Documentation:           [ ] / 15 items
Cross-Platform:          [ ] / 8 tests
Performance:             [ ] / 7 benchmarks
Integration:             [ ] / 10 tests

---
TOTAL PASSED:            [ ] / 154 items
PASS RATE:               ____%
```

### Critical Issues Found
```
1. [Issue description]
   Severity: [Critical/High/Medium/Low]
   Status: [Open/In Progress/Resolved]
   
2. ...
```

### Test Environment Details
```
Test Date: [DATE]
Tester: [NAME]
Build Version: [VERSION from version.json]
Test Environment:
  - OS: Windows [10/11]
  - Type: [VM/Physical]
  - RAM: [X] GB
  - CPU: [Model]
  - GPU: [Model or "Integrated"]
```

### Sign-Off

**Test Lead:** _________________ Date: _______

**QA Engineer:** _________________ Date: _______

**Release Manager:** _________________ Date: _______

---

## Final Approval

- [ ] All critical tests passed (100%)
- [ ] All high-priority tests passed (>95%)
- [ ] Medium/low priority tests acceptable (>80%)
- [ ] No critical bugs remain
- [ ] Documentation complete
- [ ] Security scan clear
- [ ] Performance acceptable
- [ ] **APPROVED FOR RELEASE**

---

**Document Version:** 1.0  
**Last Updated:** 2025-10-20  
**Maintained By:** Aura Video Studio Team
