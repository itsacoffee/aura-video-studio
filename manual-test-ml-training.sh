#!/bin/bash
set -e

# Manual test script for ML training backend
# Tests: happy path, insufficient data, invalid frames, revert

API_URL="${AURA_API_URL:-http://localhost:5005}"
USER_ID="test-user"

echo "=== ML Training Backend Manual Test Script ==="
echo "API URL: $API_URL"
echo ""

# Test 1: Upload valid annotations (happy path)
echo "Test 1: Uploading valid annotations..."
curl -X POST "$API_URL/api/ml/annotations/upload" \
  -H "Content-Type: application/json" \
  -d '{
    "annotations": [
      { "framePath": "frame001.jpg", "rating": 0.9 },
      { "framePath": "frame002.jpg", "rating": 0.7 },
      { "framePath": "frame003.jpg", "rating": 0.5 },
      { "framePath": "frame004.jpg", "rating": 0.8 },
      { "framePath": "frame005.jpg", "rating": 0.6 }
    ]
  }'
echo ""
echo ""

# Test 2: Get annotation stats
echo "Test 2: Getting annotation statistics..."
curl -X GET "$API_URL/api/ml/annotations/stats"
echo ""
echo ""

# Test 3: Start training job (happy path)
echo "Test 3: Starting training job..."
TRAINING_RESPONSE=$(curl -s -X POST "$API_URL/api/ml/train/frame-importance" \
  -H "Content-Type: application/json" \
  -d '{ "modelName": "test-model" }')
echo "$TRAINING_RESPONSE"
JOB_ID=$(echo "$TRAINING_RESPONSE" | grep -o '"jobId":"[^"]*"' | cut -d'"' -f4)
echo "Job ID: $JOB_ID"
echo ""

# Test 4: Monitor training progress
echo "Test 4: Monitoring training progress..."
for i in {1..10}; do
  echo "Check $i/10..."
  STATUS=$(curl -s -X GET "$API_URL/api/ml/train/$JOB_ID/status")
  echo "$STATUS"

  STATE=$(echo "$STATUS" | grep -o '"state":"[^"]*"' | cut -d'"' -f4)
  if [ "$STATE" == "Completed" ] || [ "$STATE" == "Failed" ]; then
    echo "Training $STATE"
    break
  fi

  sleep 2
done
echo ""

# Test 5: Test with insufficient data (new user)
echo "Test 5: Testing with insufficient data..."
curl -X POST "$API_URL/api/ml/train/frame-importance" \
  -H "Content-Type: application/json" \
  -H "X-User-Id: no-data-user" \
  -d '{ "modelName": "test-model" }'
echo ""
echo ""

# Test 6: Test with invalid rating
echo "Test 6: Testing with invalid rating (should fail)..."
curl -X POST "$API_URL/api/ml/annotations/upload" \
  -H "Content-Type: application/json" \
  -d '{
    "annotations": [
      { "framePath": "invalid.jpg", "rating": 1.5 }
    ]
  }'
echo ""
echo ""

# Test 7: Test with empty frame path (should fail)
echo "Test 7: Testing with empty frame path (should fail)..."
curl -X POST "$API_URL/api/ml/annotations/upload" \
  -H "Content-Type: application/json" \
  -d '{
    "annotations": [
      { "framePath": "", "rating": 0.5 }
    ]
  }'
echo ""
echo ""

# Test 8: Revert to default model
echo "Test 8: Reverting to default model..."
curl -X POST "$API_URL/api/ml/model/revert"
echo ""
echo ""

# Test 9: Cancel a job
echo "Test 9: Starting and cancelling a job..."
CANCEL_RESPONSE=$(curl -s -X POST "$API_URL/api/ml/train/frame-importance" \
  -H "Content-Type: application/json" \
  -d '{ "modelName": "cancel-test" }')
CANCEL_JOB_ID=$(echo "$CANCEL_RESPONSE" | grep -o '"jobId":"[^"]*"' | cut -d'"' -f4)
echo "Cancel Job ID: $CANCEL_JOB_ID"

sleep 1

curl -X POST "$API_URL/api/ml/train/$CANCEL_JOB_ID/cancel"
echo ""
echo ""

# Test 10: Check cancelled job status
echo "Test 10: Checking cancelled job status..."
curl -X GET "$API_URL/api/ml/train/$CANCEL_JOB_ID/status"
echo ""
echo ""

echo "=== All manual tests completed ==="
