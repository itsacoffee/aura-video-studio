#!/usr/bin/env node
/**
 * Update version in version.json
 * Usage: node update-version.js [version]
 * 
 * If no version is provided, it will increment the patch version.
 * Supports: major, minor, patch, or explicit version like 1.2.3
 */

const fs = require('fs');
const path = require('path');

function parseVersion(version) {
  const match = version.match(/^(\d+)\.(\d+)\.(\d+)$/);
  if (!match) {
    throw new Error(`Invalid version format: ${version}. Expected format: X.Y.Z`);
  }
  return {
    major: parseInt(match[1], 10),
    minor: parseInt(match[2], 10),
    patch: parseInt(match[3], 10)
  };
}

function formatVersion(parts) {
  return `${parts.major}.${parts.minor}.${parts.patch}`;
}

function incrementVersion(currentVersion, increment) {
  const parts = parseVersion(currentVersion);

  switch (increment) {
    case 'major':
      parts.major++;
      parts.minor = 0;
      parts.patch = 0;
      break;
    case 'minor':
      parts.minor++;
      parts.patch = 0;
      break;
    case 'patch':
    default:
      parts.patch++;
      break;
  }

  return formatVersion(parts);
}

function updateVersion(newVersionOrIncrement) {
  const versionFilePath = path.join(__dirname, '../../version.json');

  let versionData;
  try {
    const content = fs.readFileSync(versionFilePath, 'utf-8');
    versionData = JSON.parse(content);
  } catch (error) {
    console.error('Error reading version.json:', error.message);
    process.exit(1);
  }

  const currentVersion = versionData.version || '1.0.0';
  let newVersion;

  if (['major', 'minor', 'patch'].includes(newVersionOrIncrement)) {
    newVersion = incrementVersion(currentVersion, newVersionOrIncrement);
    console.log(`Incrementing ${newVersionOrIncrement} version: ${currentVersion} → ${newVersion}`);
  } else {
    try {
      parseVersion(newVersionOrIncrement);
      newVersion = newVersionOrIncrement;
      console.log(`Setting explicit version: ${currentVersion} → ${newVersion}`);
    } catch (error) {
      console.error(error.message);
      console.log('Usage: node update-version.js [major|minor|patch|X.Y.Z]');
      process.exit(1);
    }
  }

  versionData.version = newVersion;
  versionData.semanticVersion = newVersion;
  versionData.informationalVersion = newVersion;
  versionData.buildDate = new Date().toISOString().split('T')[0];

  fs.writeFileSync(versionFilePath, JSON.stringify(versionData, null, 2) + '\n');
  console.log(`Updated version.json: ${newVersion}`);
  console.log(`Build date: ${versionData.buildDate}`);

  return newVersion;
}

function main() {
  const args = process.argv.slice(2);
  const versionArg = args[0] || 'patch';

  updateVersion(versionArg);
}

if (require.main === module) {
  main();
}

module.exports = { updateVersion, parseVersion, incrementVersion };
