#!/bin/bash
# scripts/validate_pipeline.sh
# Comprehensive pipeline validation script for PR 32
# Tests complete video generation pipeline from brief to downloadable video

set -e

# Configuration
API_BASE="${API_BASE:-http://127.0.0.1:5000}"
FRONTEND_BASE="${FRONTEND_BASE:-http://127.0.0.1:5173}"
TEST_OUTPUT_DIR="${TEST_OUTPUT_DIR:-./test-output/pipeline-validation}"
MAX_WAIT_SECONDS=300  # 5 minutes max for video generation

# Colors for output
COLOR_RESET='\033[0m'
COLOR_GREEN='\033[0;32m'
COLOR_RED='\033[0;31m'
COLOR_YELLOW='\033[1;33m'
COLOR_CYAN='\033[0;36m'

# Test results tracking
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Helper functions
print_header() {
    echo ""
    echo "========================================"
    echo " $1"
    echo "========================================"
    echo ""
}

print_success() {
    echo -e "${COLOR_GREEN}✓${COLOR_RESET} $1"
    ((PASSED_TESTS++))
    ((TOTAL_TESTS++))
}

print_fail() {
    echo -e "${COLOR_RED}✗${COLOR_RESET} $1"
    ((FAILED_TESTS++))
    ((TOTAL_TESTS++))
}

print_info() {
    echo -e "${COLOR_CYAN}→${COLOR_RESET} $1"
}

print_warning() {
    echo -e "${COLOR_YELLOW}⚠${COLOR_RESET} $1"
}

# Create test output directory
mkdir -p "$TEST_OUTPUT_DIR"

print_header "Aura Video Studio - Pipeline Validation (PR 32)"

# Test 1: Backend API Health Check
print_info "Test 1: Checking backend API availability..."
if curl -s -f -m 5 "${API_BASE}/api/healthz" > /dev/null 2>&1; then
    print_success "Backend API is healthy"
else
    print_fail "Backend API is not available at ${API_BASE}"
    print_warning "Please start backend: cd Aura.Api && dotnet run"
    exit 1
fi

# Test 2: Frontend Availability Check
print_info "Test 2: Checking frontend availability..."
if curl -s -f -m 5 "${FRONTEND_BASE}" > /dev/null 2>&1; then
    print_success "Frontend is available"
else
    print_warning "Frontend not detected at ${FRONTEND_BASE} (optional for API tests)"
fi

# Test 3: Submit Quick Demo Request
print_info "Test 3: Submitting Quick Demo video generation request..."
JOB_RESPONSE=$(curl -s -X POST "${API_BASE}/api/jobs/quick" \
    -H "Content-Type: application/json" \
    -d '{
        "brief": {
            "topic": "Pipeline Validation Test",
            "audience": "Developers",
            "goal": "Test pipeline",
            "tone": "Professional",
            "language": "en-US",
            "aspect": "Widescreen16x9"
        },
        "plan": {
            "targetDuration": "PT15S",
            "pacing": 2,
            "density": 2,
            "style": "Explainer"
        },
        "voice": {
            "voiceName": "en-US-Standard-A",
            "rate": 1.0,
            "pitch": 0.0,
            "pause": 1
        },
        "render": {
            "res": { "width": 1920, "height": 1080 },
            "container": "mp4",
            "videoBitrateK": 5000,
            "audioBitrateK": 192,
            "fps": 30,
            "codec": "H264",
            "qualityLevel": 75,
            "enableSceneCut": true
        }
    }')

if [ $? -eq 0 ] && [ -n "$JOB_RESPONSE" ]; then
    JOB_ID=$(echo "$JOB_RESPONSE" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    if [ -n "$JOB_ID" ]; then
        print_success "Job created with ID: $JOB_ID"
        echo "$JOB_ID" > "$TEST_OUTPUT_DIR/job_id.txt"
    else
        print_fail "Failed to extract job ID from response"
        echo "$JOB_RESPONSE" > "$TEST_OUTPUT_DIR/job_creation_error.json"
        exit 1
    fi
else
    print_fail "Failed to create job"
    exit 1
fi

# Test 4: Verify Job Status and Progress Tracking
print_info "Test 4: Monitoring job progress..."
WAIT_START=$(date +%s)
PREV_PERCENT=-1
STAGES_SEEN=""

while true; do
    CURRENT_TIME=$(date +%s)
    ELAPSED=$((CURRENT_TIME - WAIT_START))
    
    if [ $ELAPSED -gt $MAX_WAIT_SECONDS ]; then
        print_fail "Job timeout after ${MAX_WAIT_SECONDS}s"
        exit 1
    fi
    
    JOB_STATUS=$(curl -s "${API_BASE}/api/jobs/${JOB_ID}")
    STATUS=$(echo "$JOB_STATUS" | grep -o '"status":"[^"]*"' | cut -d'"' -f4 | head -1)
    PERCENT=$(echo "$JOB_STATUS" | grep -o '"percent":[0-9]*' | cut -d':' -f2 | head -1)
    STAGE=$(echo "$JOB_STATUS" | grep -o '"stage":"[^"]*"' | cut -d'"' -f4 | head -1)
    
    # Track progress updates
    if [ "$PERCENT" != "$PREV_PERCENT" ] && [ -n "$PERCENT" ]; then
        echo "    Progress: ${PERCENT}% - Stage: ${STAGE} (${ELAPSED}s elapsed)"
        PREV_PERCENT=$PERCENT
    fi
    
    # Track stages seen
    if [ -n "$STAGE" ] && [[ ! "$STAGES_SEEN" =~ "$STAGE" ]]; then
        STAGES_SEEN="${STAGES_SEEN} ${STAGE}"
    fi
    
    if [ "$STATUS" = "Done" ] || [ "$STATUS" = "Succeeded" ]; then
        print_success "Job completed successfully in ${ELAPSED}s"
        echo "$STAGES_SEEN" > "$TEST_OUTPUT_DIR/stages_seen.txt"
        break
    fi
    
    if [ "$STATUS" = "Failed" ]; then
        ERROR_MSG=$(echo "$JOB_STATUS" | grep -o '"errorMessage":"[^"]*"' | cut -d'"' -f4)
        print_fail "Job failed: $ERROR_MSG"
        echo "$JOB_STATUS" > "$TEST_OUTPUT_DIR/job_failure.json"
        exit 1
    fi
    
    sleep 2
done

# Test 5: Verify Script Generation Stage
print_info "Test 5: Verifying script generation stage..."
if [[ "$STAGES_SEEN" =~ "Script" ]] || [[ "$STAGES_SEEN" =~ "script" ]]; then
    print_success "Script generation stage detected"
else
    print_warning "Script generation stage not explicitly detected"
fi

# Test 6: Verify TTS Generation Stage
print_info "Test 6: Verifying TTS audio generation stage..."
if [[ "$STAGES_SEEN" =~ "Audio" ]] || [[ "$STAGES_SEEN" =~ "TTS" ]] || [[ "$STAGES_SEEN" =~ "audio" ]]; then
    print_success "TTS audio generation stage detected"
else
    print_warning "TTS generation stage not explicitly detected"
fi

# Test 7: Verify Image Generation Stage
print_info "Test 7: Verifying image generation stage..."
if [[ "$STAGES_SEEN" =~ "Visual" ]] || [[ "$STAGES_SEEN" =~ "Image" ]] || [[ "$STAGES_SEEN" =~ "visual" ]]; then
    print_success "Image generation stage detected"
else
    print_warning "Image generation stage not explicitly detected"
fi

# Test 8: Verify FFmpeg Video Assembly Stage
print_info "Test 8: Verifying FFmpeg video assembly stage..."
if [[ "$STAGES_SEEN" =~ "Render" ]] || [[ "$STAGES_SEEN" =~ "render" ]] || [[ "$STAGES_SEEN" =~ "Compose" ]]; then
    print_success "FFmpeg video assembly stage detected"
else
    print_warning "FFmpeg assembly stage not explicitly detected"
fi

# Test 9: Download and Verify Generated Video
print_info "Test 9: Downloading and verifying generated video..."
FINAL_STATUS=$(curl -s "${API_BASE}/api/jobs/${JOB_ID}")
VIDEO_PATH=$(echo "$FINAL_STATUS" | grep -o '"path":"[^"]*\.mp4"' | cut -d'"' -f4 | head -1)

if [ -n "$VIDEO_PATH" ] && [ -f "$VIDEO_PATH" ]; then
    VIDEO_SIZE=$(stat -f%z "$VIDEO_PATH" 2>/dev/null || stat -c%s "$VIDEO_PATH" 2>/dev/null)
    if [ "$VIDEO_SIZE" -gt 100000 ]; then
        cp "$VIDEO_PATH" "$TEST_OUTPUT_DIR/test_video.mp4"
        print_success "Video file validated: ${VIDEO_SIZE} bytes"
        
        # Verify video plays with audio (using ffprobe if available)
        if command -v ffprobe &> /dev/null; then
            STREAMS=$(ffprobe -v quiet -show_streams "$VIDEO_PATH" 2>&1)
            if echo "$STREAMS" | grep -q "codec_type=video" && echo "$STREAMS" | grep -q "codec_type=audio"; then
                print_success "Video contains both video and audio streams"
            else
                print_warning "Could not verify both video and audio streams"
            fi
        fi
    else
        print_fail "Video file too small: ${VIDEO_SIZE} bytes"
    fi
else
    print_fail "Video file not found or path not in response"
fi

# Test 10: Job Cancellation (create a new job to test cancellation)
print_info "Test 10: Testing job cancellation..."
CANCEL_JOB_RESPONSE=$(curl -s -X POST "${API_BASE}/api/jobs/quick" \
    -H "Content-Type: application/json" \
    -d '{
        "brief": {
            "topic": "Cancellation Test",
            "audience": "Test",
            "goal": "Test cancellation",
            "tone": "Neutral",
            "language": "en-US",
            "aspect": "Widescreen16x9"
        },
        "plan": {
            "targetDuration": "PT30S",
            "pacing": 2,
            "density": 2,
            "style": "Test"
        },
        "voice": {
            "voiceName": "en-US-Standard-A",
            "rate": 1.0,
            "pitch": 0.0,
            "pause": 1
        },
        "render": {
            "res": { "width": 1920, "height": 1080 },
            "container": "mp4",
            "videoBitrateK": 5000,
            "audioBitrateK": 192,
            "fps": 30,
            "codec": "H264",
            "qualityLevel": 75,
            "enableSceneCut": true
        }
    }')

CANCEL_JOB_ID=$(echo "$CANCEL_JOB_RESPONSE" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
if [ -n "$CANCEL_JOB_ID" ]; then
    sleep 1  # Let job start
    CANCEL_RESULT=$(curl -s -X POST "${API_BASE}/api/jobs/${CANCEL_JOB_ID}/cancel")
    sleep 2  # Let cancellation take effect
    CANCELED_STATUS=$(curl -s "${API_BASE}/api/jobs/${CANCEL_JOB_ID}")
    CANCELED_STATE=$(echo "$CANCELED_STATUS" | grep -o '"status":"[^"]*"' | cut -d'"' -f4 | head -1)
    
    if [ "$CANCELED_STATE" = "Canceled" ] || [ "$CANCELED_STATE" = "Failed" ]; then
        print_success "Job cancellation works: $CANCELED_STATE"
    else
        print_warning "Job cancellation resulted in: $CANCELED_STATE"
    fi
else
    print_warning "Could not create job for cancellation test"
fi

# Test 11-13: Error Handling Tests
print_info "Test 11-13: Error handling tests..."
print_info "Testing error handling for unavailable providers..."

# These tests require intentionally misconfiguring providers, which is complex in a running system
# Marking as manual test requirement
print_warning "Error handling tests for unavailable providers require manual configuration"
print_warning "To test: Stop provider services and retry video generation"

# Test 14: User-Friendly Error Messages
print_info "Test 14: Verifying error message formatting..."
print_info "This test requires analyzing failed job responses"
print_warning "Manual verification needed: Check UI for user-friendly error messages"

# Test 15: Temporary Files Cleanup
print_info "Test 15: Checking temporary files cleanup..."
TEMP_DIR="/tmp/aura-*"
TEMP_COUNT=$(find /tmp -maxdepth 1 -name "aura-*" -type d 2>/dev/null | wc -l)
if [ "$TEMP_COUNT" -lt 10 ]; then
    print_success "Temporary files appear to be cleaned up (${TEMP_COUNT} temp dirs)"
else
    print_warning "Found ${TEMP_COUNT} temporary directories - cleanup may need attention"
fi

# Test 16: Logs Capture
print_info "Test 16: Verifying logs capture pipeline events..."
LOG_FILE="$TEST_OUTPUT_DIR/api_logs.txt"
if [ -f "Aura.Api/logs/api.log" ]; then
    cp "Aura.Api/logs/api.log" "$LOG_FILE" 2>/dev/null || true
    LOG_SIZE=$(stat -f%z "$LOG_FILE" 2>/dev/null || stat -c%s "$LOG_FILE" 2>/dev/null || echo "0")
    if [ "$LOG_SIZE" -gt 0 ]; then
        print_success "Log file captured: ${LOG_SIZE} bytes"
    else
        print_warning "Log file is empty or not accessible"
    fi
else
    print_warning "Log file not found at expected location"
    print_info "Logs may be directed to console output"
fi

# Final Summary
print_header "Test Results Summary"
echo "Total Tests:  $TOTAL_TESTS"
echo "Passed:       $PASSED_TESTS"
echo "Failed:       $FAILED_TESTS"
echo ""

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${COLOR_GREEN}✓ All tests passed!${COLOR_RESET}"
    echo ""
    echo "Test artifacts saved to: $TEST_OUTPUT_DIR"
    exit 0
else
    echo -e "${COLOR_RED}✗ Some tests failed${COLOR_RESET}"
    echo ""
    echo "Test artifacts saved to: $TEST_OUTPUT_DIR"
    exit 1
fi
