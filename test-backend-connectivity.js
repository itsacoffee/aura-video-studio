/**
 * Backend Connectivity Diagnostic Script
 * Run this to diagnose backend connection issues in portable builds
 * 
 * Usage: node test-backend-connectivity.js
 */

const http = require('http');
const { spawn, exec } = require('child_process');
const path = require('path');
const fs = require('fs');

const BACKEND_URL = process.env.AURA_BACKEND_URL || 'http://127.0.0.1:5005';
const BACKEND_HOST = '127.0.0.1';
const BACKEND_PORT = 5005;

console.log('='.repeat(70));
console.log('BACKEND CONNECTIVITY DIAGNOSTIC');
console.log('='.repeat(70));
console.log(`Target Backend URL: ${BACKEND_URL}`);
console.log(`Expected Host: ${BACKEND_HOST}`);
console.log(`Expected Port: ${BACKEND_PORT}`);
console.log('');

// Test 1: Check if port is open
async function testPortConnectivity() {
  console.log('[TEST 1] Testing Port Connectivity...');
  
  return new Promise((resolve) => {
    const socket = require('net').createConnection({ port: BACKEND_PORT, host: BACKEND_HOST }, () => {
      console.log('  ✓ Port is open and accepting connections');
      socket.end();
      resolve(true);
    });

    socket.on('error', (err) => {
      console.log(`  ✗ Port is not accessible: ${err.message}`);
      resolve(false);
    });

    setTimeout(() => {
      socket.destroy();
      console.log('  ✗ Connection timeout');
      resolve(false);
    }, 3000);
  });
}

// Test 2: Check what's using the port (Windows)
async function testPortOwner() {
  console.log('\n[TEST 2] Checking Port Owner...');
  
  if (process.platform !== 'win32') {
    console.log('  ⊘ Skipped (Windows only)');
    return;
  }

  return new Promise((resolve) => {
    exec(`netstat -ano | findstr :${BACKEND_PORT}`, (error, stdout, stderr) => {
      if (error || !stdout) {
        console.log('  ⊘ No process found on port');
        resolve();
        return;
      }

      console.log('  Port Usage:');
      const lines = stdout.trim().split('\n');
      lines.forEach(line => {
        console.log(`    ${line.trim()}`);
        
        const pidMatch = line.match(/LISTENING\s+(\d+)/);
        if (pidMatch) {
          const pid = pidMatch[1];
          exec(`tasklist /FI "PID eq ${pid}" /FO CSV /NH`, (err2, stdout2) => {
            if (!err2 && stdout2) {
              const processName = stdout2.split(',')[0]?.replace(/"/g, '');
              console.log(`    └─ Process: ${processName} (PID: ${pid})`);
            }
          });
        }
      });
      
      resolve();
    });
  });
}

// Test 3: HTTP Health Check
async function testHttpHealth() {
  console.log('\n[TEST 3] Testing HTTP Health Endpoint...');
  
  const healthEndpoints = [
    '/healthz/simple',
    '/health/live',
    '/health/ready',
    '/health',
    '/api/health'
  ];

  for (const endpoint of healthEndpoints) {
    const url = `http://${BACKEND_HOST}:${BACKEND_PORT}${endpoint}`;
    
    try {
      const result = await makeHttpRequest(url);
      console.log(`  ✓ ${endpoint} - Status: ${result.statusCode}`);
      if (result.data) {
        console.log(`    Response: ${result.data.substring(0, 100)}`);
      }
      return true;
    } catch (error) {
      console.log(`  ✗ ${endpoint} - ${error.message}`);
    }
  }
  
  return false;
}

// Test 4: Check backend executable
async function testBackendExecutable() {
  console.log('\n[TEST 4] Checking Backend Executable...');
  
  const possiblePaths = [
    path.join(process.cwd(), 'dist', 'backend', 'Aura.Api.exe'),
    path.join(process.cwd(), 'Aura.Api', 'bin', 'Release', 'net8.0', 'win-x64', 'publish', 'Aura.Api.exe'),
    path.join(process.cwd(), 'Aura.Api', 'bin', 'Debug', 'net8.0', 'Aura.Api.exe'),
  ];

  for (const exePath of possiblePaths) {
    if (fs.existsSync(exePath)) {
      const stats = fs.statSync(exePath);
      console.log(`  ✓ Found: ${exePath}`);
      console.log(`    Size: ${(stats.size / 1024 / 1024).toFixed(2)} MB`);
      console.log(`    Modified: ${stats.mtime.toISOString()}`);
      return exePath;
    }
  }
  
  console.log('  ✗ Backend executable not found in expected locations');
  return null;
}

// Test 5: Check .NET Runtime
async function testDotnetRuntime() {
  console.log('\n[TEST 5] Checking .NET Runtime...');
  
  return new Promise((resolve) => {
    exec('dotnet --version', { timeout: 5000 }, (error, stdout, stderr) => {
      if (error) {
        console.log(`  ✗ .NET runtime not found: ${error.message}`);
        resolve(false);
        return;
      }

      const version = stdout.trim();
      console.log(`  ✓ .NET version: ${version}`);
      
      const versionMatch = version.match(/^(\d+)\.(\d+)/);
      if (versionMatch) {
        const majorVersion = parseInt(versionMatch[1], 10);
        if (majorVersion >= 8) {
          console.log(`  ✓ Version is compatible (>= 8.0)`);
          resolve(true);
        } else {
          console.log(`  ✗ Version too old (need >= 8.0)`);
          resolve(false);
        }
      } else {
        console.log(`  ⚠ Could not parse version`);
        resolve(false);
      }
    });
  });
}

// Test 6: Check ASPNETCORE_URLS environment variable
async function testEnvironmentVariables() {
  console.log('\n[TEST 6] Checking Environment Variables...');
  
  const vars = {
    'ASPNETCORE_URLS': process.env.ASPNETCORE_URLS,
    'AURA_BACKEND_URL': process.env.AURA_BACKEND_URL,
    'AURA_API_URL': process.env.AURA_API_URL,
    'DOTNET_ENVIRONMENT': process.env.DOTNET_ENVIRONMENT,
  };

  let hasIssues = false;
  for (const [key, value] of Object.entries(vars)) {
    if (value) {
      console.log(`  ✓ ${key}=${value}`);
    } else {
      console.log(`  ⊘ ${key} not set`);
    }
  }

  return !hasIssues;
}

// Helper function to make HTTP requests
function makeHttpRequest(url, timeout = 3000) {
  return new Promise((resolve, reject) => {
    const urlParts = new URL(url);
    
    const req = http.get({
      hostname: urlParts.hostname,
      port: urlParts.port,
      path: urlParts.pathname,
      timeout: timeout,
    }, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        resolve({ statusCode: res.statusCode, data });
      });
    });

    req.on('error', reject);
    req.on('timeout', () => {
      req.destroy();
      reject(new Error('Request timeout'));
    });
  });
}

// Run all tests
async function runDiagnostics() {
  try {
    const portOpen = await testPortConnectivity();
    await testPortOwner();
    const httpWorks = await testHttpHealth();
    const backendPath = await testBackendExecutable();
    const dotnetWorks = await testDotnetRuntime();
    await testEnvironmentVariables();

    console.log('\n' + '='.repeat(70));
    console.log('DIAGNOSTIC SUMMARY');
    console.log('='.repeat(70));
    console.log(`Port Connectivity:    ${portOpen ? '✓ PASS' : '✗ FAIL'}`);
    console.log(`HTTP Health Check:    ${httpWorks ? '✓ PASS' : '✗ FAIL'}`);
    console.log(`Backend Executable:   ${backendPath ? '✓ FOUND' : '✗ NOT FOUND'}`);
    console.log(`.NET Runtime:         ${dotnetWorks ? '✓ OK' : '✗ ISSUE'}`);
    console.log('');

    if (!portOpen) {
      console.log('⚠ ISSUE: Backend port is not open');
      console.log('  Possible causes:');
      console.log('  1. Backend process is not running');
      console.log('  2. Backend failed to start');
      console.log('  3. Port is blocked by firewall');
      console.log('  4. Backend is listening on wrong port');
    }

    if (!httpWorks) {
      console.log('⚠ ISSUE: Backend HTTP server is not responding');
      console.log('  Possible causes:');
      console.log('  1. Backend started but crashed');
      console.log('  2. Backend is binding to wrong address');
      console.log('  3. Backend is taking too long to start');
    }

    if (!backendPath) {
      console.log('⚠ ISSUE: Backend executable not found');
      console.log('  Possible causes:');
      console.log('  1. Build incomplete or failed');
      console.log('  2. Files not extracted properly (portable)');
      console.log('  3. Running from wrong directory');
    }

    if (!dotnetWorks) {
      console.log('⚠ ISSUE: .NET runtime issue');
      console.log('  Possible causes:');
      console.log('  1. .NET 8.0 not installed');
      console.log('  2. .NET not in PATH');
      console.log('  3. Corrupted .NET installation');
    }

    console.log('='.repeat(70));
  } catch (error) {
    console.error('\n✗ Diagnostic failed:', error.message);
    process.exit(1);
  }
}

// Run diagnostics
runDiagnostics();

