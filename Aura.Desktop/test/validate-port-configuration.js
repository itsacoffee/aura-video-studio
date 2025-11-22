/**
 * Port Configuration Validation Test
 * 
 * Validates that all components are configured to use the same port (5005).
 * This prevents the "Backend Server Not Reachable" error caused by port mismatches.
 * 
 * Run: node test/validate-port-configuration.js
 */

const path = require('path');
const fs = require('fs');

console.log('='.repeat(80));
console.log('PORT CONFIGURATION VALIDATION TEST');
console.log('='.repeat(80));
console.log('');

let allTestsPassed = true;
const issues = [];

/**
 * Test 1: Validate Electron Network Contract
 */
console.log('Test 1: Validating Electron Network Contract...');
try {
  const networkContractPath = path.join(__dirname, '..', 'electron', 'network-contract.js');
  const networkContractContent = fs.readFileSync(networkContractPath, 'utf8');
  
  // Check for correct port 5005
  const devUrlMatch = networkContractContent.match(/DEFAULT_DEV_BACKEND_URL[^"]*"([^"]+)"/);
  const prodUrlMatch = networkContractContent.match(/DEFAULT_PROD_BACKEND_URL[^"]*"([^"]+)"/);
  
  if (devUrlMatch) {
    const devUrl = devUrlMatch[1];
    console.log(`  ✓ Development URL: ${devUrl}`);
    
    if (!devUrl.includes(':5005')) {
      issues.push('❌ Development URL does not use port 5005');
      allTestsPassed = false;
    } else {
      console.log('  ✓ Development port is correct (5005)');
    }
  } else {
    issues.push('❌ Could not find DEFAULT_DEV_BACKEND_URL in network-contract.js');
    allTestsPassed = false;
  }
  
  if (prodUrlMatch) {
    const prodUrl = prodUrlMatch[1];
    console.log(`  ✓ Production URL: ${prodUrl}`);
    
    if (!prodUrl.includes(':5005')) {
      issues.push('❌ Production URL does not use port 5005');
      allTestsPassed = false;
    } else {
      console.log('  ✓ Production port is correct (5005)');
    }
  } else {
    issues.push('❌ Could not find DEFAULT_PROD_BACKEND_URL in network-contract.js');
    allTestsPassed = false;
  }
} catch (error) {
  issues.push(`❌ Error reading network-contract.js: ${error.message}`);
  allTestsPassed = false;
}

console.log('');

/**
 * Test 2: Validate Backend Configuration
 */
console.log('Test 2: Validating Backend Configuration...');
try {
  const appsettingsPath = path.join(__dirname, '..', '..', 'Aura.Api', 'appsettings.json');
  const appsettingsContent = fs.readFileSync(appsettingsPath, 'utf8');
  // Remove BOM if present
  const cleanContent = appsettingsContent.replace(/^\uFEFF/, '');
  const appsettings = JSON.parse(cleanContent);
  
  if (appsettings.Urls) {
    console.log(`  ✓ Backend configured URL: ${appsettings.Urls}`);
    
    if (!appsettings.Urls.includes(':5005')) {
      issues.push('❌ Backend Urls setting does not use port 5005');
      allTestsPassed = false;
    } else {
      console.log('  ✓ Backend port is correct (5005)');
    }
  } else {
    console.log('  ⚠ No Urls setting found in appsettings.json (will use launchSettings.json or ASPNETCORE_URLS)');
  }
  
  // Check launchSettings.json
  const launchSettingsPath = path.join(__dirname, '..', '..', 'Aura.Api', 'Properties', 'launchSettings.json');
  if (fs.existsSync(launchSettingsPath)) {
    const launchSettingsContent = fs.readFileSync(launchSettingsPath, 'utf8');
    // Remove BOM if present
    const cleanLaunchContent = launchSettingsContent.replace(/^\uFEFF/, '');
    const launchSettings = JSON.parse(cleanLaunchContent);
    
    if (launchSettings.profiles) {
      Object.keys(launchSettings.profiles).forEach(profileName => {
        const profile = launchSettings.profiles[profileName];
        if (profile.applicationUrl) {
          console.log(`  ✓ Launch profile "${profileName}": ${profile.applicationUrl}`);
          
          if (!profile.applicationUrl.includes(':5005')) {
            console.log(`  ⚠ Launch profile "${profileName}" does not include port 5005`);
          }
        }
      });
    }
  }
} catch (error) {
  issues.push(`❌ Error reading backend configuration: ${error.message}`);
  allTestsPassed = false;
}

console.log('');

/**
 * Test 3: Validate Frontend Configuration
 */
console.log('Test 3: Validating Frontend Configuration...');
try {
  const envDevPath = path.join(__dirname, '..', '..', 'Aura.Web', '.env.development');
  
  if (fs.existsSync(envDevPath)) {
    const envDevContent = fs.readFileSync(envDevPath, 'utf8');
    const apiBaseUrlMatch = envDevContent.match(/VITE_API_BASE_URL=(.+)/);
    
    if (apiBaseUrlMatch) {
      const apiBaseUrl = apiBaseUrlMatch[1].trim();
      console.log(`  ✓ Frontend development API URL: ${apiBaseUrl}`);
      
      if (!apiBaseUrl.includes(':5005')) {
        issues.push('❌ Frontend .env.development does not use port 5005');
        allTestsPassed = false;
      } else {
        console.log('  ✓ Frontend port is correct (5005)');
      }
    } else {
      issues.push('❌ Could not find VITE_API_BASE_URL in .env.development');
      allTestsPassed = false;
    }
  } else {
    console.log('  ⚠ .env.development not found (OK for Electron, uses IPC bridge)');
  }
} catch (error) {
  issues.push(`❌ Error reading frontend configuration: ${error.message}`);
  allTestsPassed = false;
}

console.log('');
console.log('='.repeat(80));

if (allTestsPassed && issues.length === 0) {
  console.log('✅ ALL TESTS PASSED - Port configuration is consistent (5005)');
  console.log('='.repeat(80));
  process.exit(0);
} else {
  console.log('❌ TESTS FAILED - Port configuration issues detected:');
  console.log('='.repeat(80));
  issues.forEach(issue => console.log(issue));
  console.log('');
  console.log('RECOMMENDED FIX:');
  console.log('  1. Ensure network-contract.js uses port 5005 for both dev and prod');
  console.log('  2. Ensure Aura.Api/appsettings.json Urls setting includes port 5005');
  console.log('  3. Ensure Aura.Web/.env.development uses port 5005');
  console.log('='.repeat(80));
  process.exit(1);
}
