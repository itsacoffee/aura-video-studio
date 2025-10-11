#!/usr/bin/env node

/**
 * Generate TypeScript types from API OpenAPI spec
 * 
 * This script:
 * 1. Starts the API server temporarily
 * 2. Fetches the OpenAPI JSON from /swagger/v1/swagger.json
 * 3. Runs openapi-typescript to generate TS types
 * 4. Saves the result to Aura.Web/src/types/api-v1.ts
 * 
 * Usage:
 *   node scripts/contract/generate-api-v1-types.js
 *   npm run generate:api-types (if added to package.json)
 */

const { spawn, exec } = require('child_process');
const fs = require('fs');
const path = require('path');
const http = require('http');

const REPO_ROOT = path.resolve(__dirname, '../..');
const API_PROJECT = path.join(REPO_ROOT, 'Aura.Api');
const OUTPUT_FILE = path.join(REPO_ROOT, 'Aura.Web/src/types/api-v1.ts');
const SWAGGER_URL = 'http://localhost:5000/swagger/v1/swagger.json';
const API_PORT = 5000;

console.log('🚀 Generating API V1 TypeScript types from OpenAPI spec...\n');

// Check if openapi-typescript is available
exec('npx openapi-typescript --version', (error) => {
  if (error) {
    console.error('❌ openapi-typescript not found. Installing...');
    exec('npm install -g openapi-typescript', (installError) => {
      if (installError) {
        console.error('❌ Failed to install openapi-typescript:', installError);
        process.exit(1);
      }
      console.log('✅ openapi-typescript installed\n');
      startGeneration();
    });
  } else {
    startGeneration();
  }
});

function startGeneration() {
  console.log(`1️⃣  Starting API server on port ${API_PORT}...`);
  
  // Start the API server
  const apiProcess = spawn('dotnet', ['run', '--no-build', '--urls', `http://localhost:${API_PORT}`], {
    cwd: API_PROJECT,
    env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development' }
  });

  let serverReady = false;
  let outputBuffer = '';

  apiProcess.stdout.on('data', (data) => {
    const output = data.toString();
    outputBuffer += output;
    
    if (output.includes('Now listening on:') || output.includes(`http://localhost:${API_PORT}`)) {
      serverReady = true;
      console.log('✅ API server is running\n');
      
      // Wait a bit for full initialization
      setTimeout(() => fetchAndGenerate(apiProcess), 2000);
    }
  });

  apiProcess.stderr.on('data', (data) => {
    // Log warnings/errors but don't stop
    console.error('API stderr:', data.toString());
  });

  // Timeout if server doesn't start in 30 seconds
  setTimeout(() => {
    if (!serverReady) {
      console.error('❌ API server failed to start within 30 seconds');
      apiProcess.kill();
      process.exit(1);
    }
  }, 30000);
}

function fetchAndGenerate(apiProcess) {
  console.log('2️⃣  Fetching OpenAPI spec from', SWAGGER_URL);
  
  http.get(SWAGGER_URL, (res) => {
    if (res.statusCode !== 200) {
      console.error(`❌ Failed to fetch OpenAPI spec: HTTP ${res.statusCode}`);
      apiProcess.kill();
      process.exit(1);
    }

    let data = '';
    res.on('data', (chunk) => {
      data += chunk;
    });

    res.on('end', () => {
      console.log('✅ OpenAPI spec fetched\n');
      
      // Save temp file
      const tempFile = path.join(__dirname, 'openapi.json');
      fs.writeFileSync(tempFile, data);
      
      console.log('3️⃣  Generating TypeScript types with openapi-typescript...');
      
      // Generate TypeScript types
      exec(`npx openapi-typescript "${tempFile}" --output "${OUTPUT_FILE}"`, (error, stdout, stderr) => {
        // Clean up
        fs.unlinkSync(tempFile);
        apiProcess.kill();
        
        if (error) {
          console.error('❌ Failed to generate TypeScript types:', error);
          console.error(stderr);
          process.exit(1);
        }
        
        console.log('✅ TypeScript types generated\n');
        
        // Add header comment
        addHeaderComment();
      });
    });
  }).on('error', (err) => {
    console.error('❌ Error fetching OpenAPI spec:', err);
    apiProcess.kill();
    process.exit(1);
  });
}

function addHeaderComment() {
  console.log('4️⃣  Adding header comment...');
  
  const header = `/**
 * AUTO-GENERATED - DO NOT EDIT
 * 
 * API V1 Type Definitions
 * Generated from OpenAPI spec at ${SWAGGER_URL}
 * 
 * To regenerate:
 *   node scripts/contract/generate-api-v1-types.js
 * 
 * Last generated: ${new Date().toISOString()}
 */

`;

  const content = fs.readFileSync(OUTPUT_FILE, 'utf8');
  fs.writeFileSync(OUTPUT_FILE, header + content);
  
  console.log('✅ Header added\n');
  console.log(`🎉 Done! TypeScript types saved to ${OUTPUT_FILE}`);
}
