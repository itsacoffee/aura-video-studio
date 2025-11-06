#!/usr/bin/env node
/**
 * Generate release notes from conventional commits
 * Usage: node generate-release-notes.js [from-tag] [to-tag]
 */

const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

function execCommand(command) {
  try {
    return execSync(command, { encoding: 'utf-8' }).trim();
  } catch (error) {
    console.error(`Error executing: ${command}`);
    console.error(error.message);
    return '';
  }
}

function getCommitsSinceTag(fromTag, toTag = 'HEAD', limit = 100) {
  const command = fromTag 
    ? `git log ${fromTag}..${toTag} --pretty=format:"%H|||%s|||%an|||%aI|||%b" --no-merges`
    : `git log ${toTag} --pretty=format:"%H|||%s|||%an|||%aI|||%b" --no-merges -n ${limit}`;
  
  const output = execCommand(command);
  if (!output) return [];

  const commits = [];
  const lines = output.split('\n');
  let currentCommit = null;
  
  for (const line of lines) {
    if (line.includes('|||')) {
      const parts = line.split('|||');
      if (parts.length >= 4) {
        if (currentCommit) {
          commits.push(currentCommit);
        }
        const [hash, subject, author, date] = parts;
        currentCommit = { 
          hash, 
          subject: subject || '', 
          author: author || 'Unknown', 
          email: '', 
          date,
          body: parts.slice(4).join('|||') || ''
        };
      }
    } else if (currentCommit && line.trim()) {
      currentCommit.body += '\n' + line;
    }
  }
  
  if (currentCommit) {
    commits.push(currentCommit);
  }
  
  return commits.filter(commit => commit !== null);
}

function parseConventionalCommit(subject, body) {
  if (!subject) {
    return { type: 'other', scope: null, description: '', breaking: false };
  }

  const conventionalPattern = /^(feat|fix|docs|style|refactor|perf|test|chore|build|ci|revert)(\(.+\))?: (.+)$/;
  const match = subject.match(conventionalPattern);

  if (match) {
    const [, type, scope, description] = match;
    const breaking = subject.includes('!') || body.includes('BREAKING CHANGE');
    return { type, scope: scope?.replace(/[()]/g, ''), description, breaking };
  }

  return { type: 'other', scope: null, description: subject, breaking: false };
}

function categorizeCommits(commits) {
  const categories = {
    breaking: [],
    features: [],
    fixes: [],
    performance: [],
    documentation: [],
    refactoring: [],
    testing: [],
    chore: [],
    other: []
  };

  commits.forEach(commit => {
    const parsed = parseConventionalCommit(commit.subject, commit.body);
    const entry = {
      ...commit,
      ...parsed
    };

    if (parsed.breaking) {
      categories.breaking.push(entry);
    } else {
      switch (parsed.type) {
        case 'feat':
          categories.features.push(entry);
          break;
        case 'fix':
          categories.fixes.push(entry);
          break;
        case 'perf':
          categories.performance.push(entry);
          break;
        case 'docs':
          categories.documentation.push(entry);
          break;
        case 'refactor':
          categories.refactoring.push(entry);
          break;
        case 'test':
          categories.testing.push(entry);
          break;
        case 'chore':
        case 'build':
        case 'ci':
          categories.chore.push(entry);
          break;
        default:
          categories.other.push(entry);
      }
    }
  });

  return categories;
}

function formatReleaseNotes(version, categories, fromTag, toTag) {
  const lines = [];
  
  lines.push(`# Release ${version}`);
  lines.push('');
  lines.push(`**Release Date:** ${new Date().toISOString().split('T')[0]}`);
  lines.push('');

  if (fromTag) {
    lines.push(`**Commits:** ${fromTag}...${toTag}`);
  } else {
    lines.push(`**Commits:** ${toTag}`);
  }
  lines.push('');

  if (categories.breaking.length > 0) {
    lines.push('## ⚠️ BREAKING CHANGES');
    lines.push('');
    categories.breaking.forEach(commit => {
      const scope = commit.scope ? `**${commit.scope}:** ` : '';
      lines.push(`- ${scope}${commit.description} ([${commit.hash.substring(0, 7)}](../../commit/${commit.hash}))`);
      if (commit.body.includes('BREAKING CHANGE')) {
        const breakingLine = commit.body.split('\n').find(l => l.includes('BREAKING CHANGE'));
        if (breakingLine) {
          lines.push(`  - ${breakingLine.replace('BREAKING CHANGE:', '').trim()}`);
        }
      }
    });
    lines.push('');
  }

  if (categories.features.length > 0) {
    lines.push('## 🚀 Features');
    lines.push('');
    categories.features.forEach(commit => {
      const scope = commit.scope ? `**${commit.scope}:** ` : '';
      lines.push(`- ${scope}${commit.description} ([${commit.hash.substring(0, 7)}](../../commit/${commit.hash}))`);
    });
    lines.push('');
  }

  if (categories.fixes.length > 0) {
    lines.push('## 🐛 Bug Fixes');
    lines.push('');
    categories.fixes.forEach(commit => {
      const scope = commit.scope ? `**${commit.scope}:** ` : '';
      lines.push(`- ${scope}${commit.description} ([${commit.hash.substring(0, 7)}](../../commit/${commit.hash}))`);
    });
    lines.push('');
  }

  if (categories.performance.length > 0) {
    lines.push('## ⚡ Performance Improvements');
    lines.push('');
    categories.performance.forEach(commit => {
      const scope = commit.scope ? `**${commit.scope}:** ` : '';
      lines.push(`- ${scope}${commit.description} ([${commit.hash.substring(0, 7)}](../../commit/${commit.hash}))`);
    });
    lines.push('');
  }

  if (categories.documentation.length > 0) {
    lines.push('## 📚 Documentation');
    lines.push('');
    categories.documentation.forEach(commit => {
      const scope = commit.scope ? `**${commit.scope}:** ` : '';
      lines.push(`- ${scope}${commit.description} ([${commit.hash.substring(0, 7)}](../../commit/${commit.hash}))`);
    });
    lines.push('');
  }

  if (categories.refactoring.length > 0) {
    lines.push('## ♻️ Code Refactoring');
    lines.push('');
    categories.refactoring.forEach(commit => {
      const scope = commit.scope ? `**${commit.scope}:** ` : '';
      lines.push(`- ${scope}${commit.description} ([${commit.hash.substring(0, 7)}](../../commit/${commit.hash}))`);
    });
    lines.push('');
  }

  const totalCommits = Object.values(categories).reduce((sum, cat) => sum + cat.length, 0);
  const contributors = [...new Set(Object.values(categories).flat().map(c => c.author))];

  lines.push('---');
  lines.push('');
  lines.push('## 📊 Statistics');
  lines.push('');
  lines.push(`- **Total Commits:** ${totalCommits}`);
  lines.push(`- **Contributors:** ${contributors.length}`);
  if (categories.breaking.length > 0) {
    lines.push(`- **Breaking Changes:** ${categories.breaking.length}`);
  }
  lines.push(`- **Features:** ${categories.features.length}`);
  lines.push(`- **Bug Fixes:** ${categories.fixes.length}`);
  lines.push('');

  if (contributors.length > 0) {
    lines.push('## 👥 Contributors');
    lines.push('');
    contributors.forEach(contributor => {
      lines.push(`- ${contributor}`);
    });
    lines.push('');
  }

  return lines.join('\n');
}

function getVersion() {
  try {
    const versionFile = path.join(__dirname, '../../version.json');
    if (fs.existsSync(versionFile)) {
      const versionData = JSON.parse(fs.readFileSync(versionFile, 'utf-8'));
      return versionData.version || '1.0.0';
    }
  } catch (error) {
    console.error('Error reading version.json:', error.message);
  }
  return '1.0.0';
}

function main() {
  const args = process.argv.slice(2);
  const fromTag = args[0] || null;
  const toTag = args[1] || 'HEAD';

  console.log('Generating release notes...');
  if (fromTag) {
    console.log(`From: ${fromTag} to: ${toTag}`);
  } else {
    console.log(`Latest commits (no tag specified)`);
  }

  const commits = getCommitsSinceTag(fromTag, toTag);
  console.log(`Found ${commits.length} commits`);

  if (commits.length === 0) {
    console.log('No commits found. Exiting.');
    process.exit(0);
  }

  const categories = categorizeCommits(commits);
  const version = getVersion();
  const releaseNotes = formatReleaseNotes(version, categories, fromTag, toTag);

  const outputPath = path.join(__dirname, '../../RELEASE_NOTES.md');
  fs.writeFileSync(outputPath, releaseNotes);

  console.log(`Release notes generated: ${outputPath}`);
  console.log('');
  console.log(releaseNotes);
}

if (require.main === module) {
  main();
}

module.exports = { getCommitsSinceTag, categorizeCommits, formatReleaseNotes, parseConventionalCommit };
