#!/usr/bin/env node

/**
 * Code Quality Report Generator
 * Generates a comprehensive report of code quality metrics
 */

import { execSync } from 'child_process';
import { writeFileSync } from 'fs';

function runCommand(command, options = {}) {
  try {
    return execSync(command, { encoding: 'utf8', ...options });
  } catch (error) {
    return error.stdout || error.message;
  }
}

function generateReport() {
  console.log('🔍 Generating Code Quality Report...\n');

  const report = {
    timestamp: new Date().toISOString(),
    sections: []
  };

  // ESLint Analysis
  console.log('Running ESLint...');
  const eslintOutput = runCommand('npm run lint -- --format json', { stdio: 'pipe' });
  let eslintResults = { errors: 0, warnings: 0, total: 0 };
  try {
    const eslintData = JSON.parse(eslintOutput);
    eslintResults.errors = eslintData.reduce((sum, file) => sum + file.errorCount, 0);
    eslintResults.warnings = eslintData.reduce((sum, file) => sum + file.warningCount, 0);
    eslintResults.total = eslintResults.errors + eslintResults.warnings;
  } catch (e) {
    console.log('Could not parse ESLint JSON output');
  }

  report.sections.push({
    title: 'ESLint Analysis',
    data: eslintResults,
    status: eslintResults.errors === 0 ? '✅' : '❌'
  });

  // TypeScript Type Check
  console.log('Running TypeScript type check...');
  const tscOutput = runCommand('npm run type-check');
  const tscErrors = tscOutput.includes('error TS') ? tscOutput.split('\n').filter(line => line.includes('error TS')).length : 0;

  report.sections.push({
    title: 'TypeScript Type Check',
    data: { errors: tscErrors },
    status: tscErrors === 0 ? '✅' : '❌'
  });

  // Prettier Format Check
  console.log('Running Prettier format check...');
  const prettierOutput = runCommand('npm run format:check');
  const prettierIssues = prettierOutput.includes('Code style issues found') ? 1 : 0;

  report.sections.push({
    title: 'Prettier Format Check',
    data: { issues: prettierIssues },
    status: prettierIssues === 0 ? '✅' : '❌'
  });

  // Stylelint Check
  console.log('Running Stylelint...');
  const stylelintOutput = runCommand('npm run lint:css -- --formatter json', { stdio: 'pipe' });
  let stylelintResults = { errors: 0, warnings: 0 };
  try {
    const stylelintData = JSON.parse(stylelintOutput);
    stylelintResults.errors = stylelintData.reduce((sum, file) => sum + file.errored, 0);
    stylelintResults.warnings = stylelintData.reduce((sum, file) => sum + file.warnings.length, 0);
  } catch (e) {
    console.log('Could not parse Stylelint JSON output');
  }

  report.sections.push({
    title: 'Stylelint Analysis',
    data: stylelintResults,
    status: stylelintResults.errors === 0 ? '✅' : '❌'
  });

  // NPM Audit
  console.log('Running npm audit...');
  const auditOutput = runCommand('npm audit --json', { stdio: 'pipe' });
  let auditResults = { vulnerabilities: 0, critical: 0, high: 0, moderate: 0, low: 0 };
  try {
    const auditData = JSON.parse(auditOutput);
    if (auditData.metadata && auditData.metadata.vulnerabilities) {
      const vulns = auditData.metadata.vulnerabilities;
      auditResults.critical = vulns.critical || 0;
      auditResults.high = vulns.high || 0;
      auditResults.moderate = vulns.moderate || 0;
      auditResults.low = vulns.low || 0;
      auditResults.vulnerabilities = auditResults.critical + auditResults.high + auditResults.moderate + auditResults.low;
    }
  } catch (e) {
    console.log('Could not parse npm audit JSON output');
  }

  report.sections.push({
    title: 'NPM Security Audit',
    data: auditResults,
    status: auditResults.critical === 0 && auditResults.high === 0 ? '✅' : '⚠️'
  });

  // Generate Report
  const reportText = generateReportText(report);
  console.log('\n' + reportText);

  // Write to file
  const reportFile = 'CODE_QUALITY_REPORT.md';
  writeFileSync(reportFile, reportText);
  console.log(`\n📊 Report saved to ${reportFile}`);

  // Return exit code based on critical issues
  const hasCriticalIssues = eslintResults.errors > 0 || tscErrors > 0 || auditResults.critical > 0;
  process.exit(hasCriticalIssues ? 1 : 0);
}

function generateReportText(report) {
  let text = '# Code Quality Report\n\n';
  text += `Generated: ${report.timestamp}\n\n`;
  text += '## Summary\n\n';

  for (const section of report.sections) {
    text += `### ${section.status} ${section.title}\n\n`;
    for (const [key, value] of Object.entries(section.data)) {
      text += `- **${key}**: ${value}\n`;
    }
    text += '\n';
  }

  text += '## Recommendations\n\n';
  
  const eslintSection = report.sections.find(s => s.title === 'ESLint Analysis');
  if (eslintSection && eslintSection.data.total > 0) {
    text += '- Run `npm run lint:fix` to auto-fix ESLint issues\n';
  }

  const prettierSection = report.sections.find(s => s.title === 'Prettier Format Check');
  if (prettierSection && prettierSection.data.issues > 0) {
    text += '- Run `npm run format` to auto-format code\n';
  }

  const stylelintSection = report.sections.find(s => s.title === 'Stylelint Analysis');
  if (stylelintSection && stylelintSection.data.errors > 0) {
    text += '- Run `npm run lint:css:fix` to auto-fix CSS issues\n';
  }

  const auditSection = report.sections.find(s => s.title === 'NPM Security Audit');
  if (auditSection && auditSection.data.vulnerabilities > 0) {
    text += '- Run `npm audit fix` to fix dependency vulnerabilities\n';
  }

  return text;
}

generateReport();
