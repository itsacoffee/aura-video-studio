#!/usr/bin/env node

/**
 * Wait for API to be healthy before starting the web app
 * This ensures a smooth development experience by preventing
 * connection errors when the API is still starting up
 */

const http = require('http');

const API_HEALTH_URL = process.env.VITE_API_HEALTH_CHECK_URL || 'http://localhost:5005/health/live';
const MAX_WAIT_TIME_MS = parseInt(process.env.VITE_API_HEALTH_CHECK_TIMEOUT || '60000', 10);
const CHECK_INTERVAL_MS = 2000;

let elapsedTime = 0;

console.log('🔍 Waiting for API to be healthy...');
console.log(`   URL: ${API_HEALTH_URL}`);
console.log(`   Max wait time: ${MAX_WAIT_TIME_MS / 1000}s`);
console.log('');

/**
 * Check if the API health endpoint responds successfully
 */
function checkApiHealth() {
  return new Promise((resolve) => {
    const url = new URL(API_HEALTH_URL);
    
    const options = {
      hostname: url.hostname,
      port: url.port || 80,
      path: url.pathname,
      method: 'GET',
      timeout: 5000
    };

    const req = http.request(options, (res) => {
      if (res.statusCode === 200) {
        resolve(true);
      } else {
        resolve(false);
      }
    });

    req.on('error', () => {
      resolve(false);
    });

    req.on('timeout', () => {
      req.destroy();
      resolve(false);
    });

    req.end();
  });
}

/**
 * Wait for the API to be healthy with periodic checks
 */
async function waitForApi() {
  while (elapsedTime < MAX_WAIT_TIME_MS) {
    const isHealthy = await checkApiHealth();
    
    if (isHealthy) {
      console.log('✅ API is healthy and ready!');
      console.log('');
      return true;
    }

    // Show progress
    const secondsElapsed = Math.floor(elapsedTime / 1000);
    const maxSeconds = Math.floor(MAX_WAIT_TIME_MS / 1000);
    process.stdout.write(`\r⏳ Waiting... ${secondsElapsed}/${maxSeconds}s`);

    // Wait before next check
    await new Promise(resolve => setTimeout(resolve, CHECK_INTERVAL_MS));
    elapsedTime += CHECK_INTERVAL_MS;
  }

  console.log('\n');
  console.log('❌ API did not become healthy within the timeout period');
  console.log('');
  console.log('Troubleshooting steps:');
  console.log('  1. Check if the API container is running: docker-compose ps');
  console.log('  2. Check API logs: docker-compose logs api');
  console.log('  3. Try accessing the health endpoint manually: curl http://localhost:5005/health/live');
  console.log('  4. Ensure no port conflicts exist on port 5005');
  console.log('');
  return false;
}

// Run the health check
waitForApi().then(success => {
  if (!success) {
    console.log('⚠️  Starting anyway - API may not be ready');
    console.log('');
  }
  // Always exit successfully to allow the dev server to start
  // This prevents blocking developers who want to work on frontend without backend
  process.exit(0);
}).catch(error => {
  console.error('Error checking API health:', error);
  // Exit successfully even on error to not block development
  process.exit(0);
});
