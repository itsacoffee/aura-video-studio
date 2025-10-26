module.exports = {
  root: true,
  env: { browser: true, es2020: true, node: true },
  extends: [
    'eslint:recommended',
    'plugin:@typescript-eslint/recommended',
    'plugin:react/recommended',
    'plugin:react/jsx-runtime',
    'plugin:react-hooks/recommended',
    'plugin:jsx-a11y/recommended',
  ],
  ignorePatterns: ['dist', '.eslintrc.cjs', 'node_modules', 'build', 'coverage', '*.config.js', '*.config.ts'],
  parser: '@typescript-eslint/parser',
  parserOptions: {
    ecmaVersion: 'latest',
    sourceType: 'module',
    ecmaFeatures: {
      jsx: true,
    },
  },
  plugins: ['react-refresh', 'jsx-a11y', 'import', 'sonarjs', 'security'],
  settings: {
    react: {
      version: 'detect',
    },
  },
  rules: {
    // React rules
    'react-refresh/only-export-components': [
      'warn',
      { allowConstantExport: true },
    ],
    'react/prop-types': 'off',
    'react-hooks/exhaustive-deps': 'warn',
    'react/no-unescaped-entities': 'error',
    
    // TypeScript rules
    '@typescript-eslint/no-explicit-any': 'warn',
    '@typescript-eslint/no-unused-vars': ['warn', { 
      argsIgnorePattern: '^_',
      varsIgnorePattern: '^_',
    }],
    
    // Console rules
    'no-console': ['warn', { allow: ['warn', 'error', 'info'] }],
    
    // Accessibility rules
    'jsx-a11y/click-events-have-key-events': 'warn',
    'jsx-a11y/no-static-element-interactions': 'warn',
    'jsx-a11y/media-has-caption': 'warn',
    
    // Import rules
    'import/order': ['warn', {
      'groups': ['builtin', 'external', 'internal', 'parent', 'sibling', 'index'],
      'newlines-between': 'never',
      'alphabetize': { 'order': 'asc', 'caseInsensitive': true }
    }],
    'import/no-duplicates': 'warn',
    
    // SonarJS complexity rules (manually enabled)
    'sonarjs/cognitive-complexity': ['warn', 20],
    'sonarjs/no-duplicate-string': 'off', // Too strict for React components
    'sonarjs/no-identical-functions': 'warn',
    'sonarjs/prefer-immediate-return': 'warn',
    
    // Security rules (manually enabled)
    'security/detect-unsafe-regex': 'error',
    'security/detect-buffer-noassert': 'error',
    'security/detect-child-process': 'warn',
    'security/detect-disable-mustache-escape': 'error',
    'security/detect-eval-with-expression': 'error',
    'security/detect-no-csrf-before-method-override': 'error',
    'security/detect-non-literal-fs-filename': 'off', // Too many false positives
    'security/detect-non-literal-require': 'warn',
    'security/detect-object-injection': 'off', // Too many false positives
    'security/detect-possible-timing-attacks': 'warn',
    'security/detect-pseudoRandomBytes': 'error',
  },
};
