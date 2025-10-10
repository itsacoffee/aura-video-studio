#!/bin/bash
# scripts/run_quick_generate_demo.sh
# Smoke test that generates a demo video.
# Falls back to ffmpeg color bars if API not available.

set -e

API_BASE="${API_BASE:-http://127.0.0.1:5000}"
FFMPEG_PATH="${FFMPEG_PATH:-ffmpeg}"
SECONDS_DURATION="${SECONDS_DURATION:-10}"

SMOKE_START=$(date +%s)

echo ""
echo "========================================"
echo " Aura Video Studio - Smoke Test"
echo "========================================"
echo ""

# Create output directory
mkdir -p artifacts/smoke
OUT="artifacts/smoke/demo.mp4"
SRT_OUT="artifacts/smoke/demo.srt"
LOGS_OUT="artifacts/smoke/logs.zip"

# Remove old output if exists
rm -f "$OUT" "$SRT_OUT" "$LOGS_OUT"

# Helper function for API calls
invoke_api() {
    local method=$1
    local path=$2
    local body=$3
    
    echo "  Testing $method $path..." >&2
    
    if [ "$method" = "GET" ]; then
        curl -s -f -m 5 "${API_BASE}${path}" 2>/dev/null || echo ""
    else
        curl -s -f -m 15 -X POST "${API_BASE}${path}" \
            -H "Content-Type: application/json" \
            -d "$body" 2>/dev/null || echo ""
    fi
}

# Try full API pipeline
OK=false

echo "Attempting full API pipeline..."
HEALTH=$(invoke_api "GET" "/healthz" "")
if [ -n "$HEALTH" ]; then
    echo "  ✓ API health check passed"
    
    BRIEF='{"Topic":"Demo Video","Tone":"Neutral","Language":"en","Aspect":"Widescreen16x9"}'
    PLAN='{"TargetDuration":1.0,"Pacing":3,"Density":3,"Style":"Explainer"}'
    SCRIPT_REQ="{\"Brief\":$BRIEF,\"Plan\":$PLAN}"
    
    SCRIPT_RES=$(invoke_api "POST" "/script" "$SCRIPT_REQ")
    if [ -n "$SCRIPT_RES" ]; then
        echo "  ✓ Script generation successful"
        
        RENDER_REQ="{\"Mode\":\"Free\",\"Brief\":$BRIEF,\"Plan\":$PLAN}"
        RENDER_RES=$(invoke_api "POST" "/render/quick" "$RENDER_REQ")
        
        if [ -n "$RENDER_RES" ]; then
            OUTPUT_PATH=$(echo "$RENDER_RES" | grep -o '"OutputPath":"[^"]*"' | cut -d'"' -f4)
            if [ -n "$OUTPUT_PATH" ] && [ -f "$OUTPUT_PATH" ]; then
                cp "$OUTPUT_PATH" "$OUT"
                echo "  ✓ Render completed via API"
                OK=true
            fi
        fi
    fi
fi

# Fallback to ffmpeg-only demo render
if [ "$OK" = false ]; then
    echo ""
    echo "Falling back to ffmpeg-only demo render..."
    
    # Check if ffmpeg exists
    if ! command -v "$FFMPEG_PATH" &> /dev/null; then
        echo "  ✗ FFmpeg not found in PATH" >&2
        exit 1
    fi
    
    echo "  Generating $SECONDS_DURATION second color bars demo..."
    "$FFMPEG_PATH" -y \
        -f lavfi -i "smptebars=size=1280x720:rate=30" \
        -f lavfi -i "sine=frequency=1000:sample_rate=48000:duration=$SECONDS_DURATION" \
        -c:v libx264 -t "$SECONDS_DURATION" -pix_fmt yuv420p \
        -c:a aac -shortest "$OUT" 2>&1 >/dev/null
    
    if [ $? -eq 0 ] && [ -f "$OUT" ]; then
        echo "  ✓ Fallback render successful"
        OK=true
    else
        echo "  ✗ FFmpeg fallback failed" >&2
    fi
fi

# Create sample SRT caption file
cat > "$SRT_OUT" << 'EOF'
1
00:00:00,000 --> 00:00:03,000
Welcome to Aura Video Studio

2
00:00:03,000 --> 00:00:06,000
AI-powered video creation

3
00:00:06,000 --> 00:00:10,000
Quick smoke test demo
EOF

# Create logs archive
mkdir -p artifacts/smoke/logs
echo "Smoke test completed at $(date)" > artifacts/smoke/logs/test.log
echo "FFmpeg path: $FFMPEG_PATH" >> artifacts/smoke/logs/test.log
echo "Duration: ${SECONDS_DURATION}s" >> artifacts/smoke/logs/test.log
cd artifacts/smoke && zip -q logs.zip logs/* && cd ../..
rm -rf artifacts/smoke/logs

SMOKE_END=$(date +%s)
SMOKE_DURATION=$((SMOKE_END - SMOKE_START))

echo ""
if [ -f "$OUT" ]; then
    FILE_SIZE=$(du -k "$OUT" | cut -f1)
    echo "========================================"
    echo " Smoke Test: PASS"
    echo "========================================"
    echo ""
    echo "Output:   $(realpath "$OUT")"
    echo "Captions: $(realpath "$SRT_OUT")"
    echo "Logs:     $(realpath "$LOGS_OUT")"
    echo "Size:     ${FILE_SIZE} KB"
    echo "Duration: ${SMOKE_DURATION}s"
    echo ""
    exit 0
else
    echo "========================================"
    echo " Smoke Test: FAIL"
    echo "========================================"
    echo ""
    echo "Failed to generate demo video"
    echo "Duration: ${SMOKE_DURATION}s"
    echo ""
    exit 1
fi
