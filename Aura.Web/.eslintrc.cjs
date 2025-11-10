// This file is deprecated and replaced by eslint.config.js for ESLint 9+
// Keeping this file for backwards compatibility and IDE support
// To use ESLint 9, run: npm run lint

module.exports = {
  root: true,
  env: { browser: true, es2020: true, node: true },
  extends: [
    'eslint:recommended',
  ],
  ignorePatterns: ['dist', '.eslintrc.cjs', 'node_modules', 'build', 'coverage', '*.config.js', '*.config.ts'],
};
