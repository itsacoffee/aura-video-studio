import { defineConfig, mergeConfig } from 'vitest/config';
import baseConfig from './vite.config';

/**
 * Vitest configuration with comprehensive coverage settings
 * This configuration extends the base vite.config.ts with test-specific settings
 */
export default mergeConfig(
  baseConfig,
  defineConfig({
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
      
      // Test file patterns
      include: [
        'src/**/*.test.{ts,tsx}',
        'tests/smoke/**/*.test.ts',
        'tests/integration/**/*.test.ts',
      ],
      exclude: [
        'tests/e2e/**',
        'node_modules/**',
        'dist/**',
        '.cache/**',
      ],

      // Coverage configuration
      coverage: {
        enabled: true,
        provider: 'v8',
        
        // Output formats
        reporter: [
          'text',
          'text-summary',
          'json',
          'json-summary',
          'html',
          'lcov',
          'cobertura',
        ],
        
        // Output directory
        reportsDirectory: './coverage',
        
        // Files to include in coverage
        include: [
          'src/**/*.{ts,tsx}',
        ],
        
        // Files to exclude from coverage
        exclude: [
          'src/**/*.test.{ts,tsx}',
          'src/test/**',
          'src/**/*.d.ts',
          'src/vite-env.d.ts',
          'src/main.tsx',
          'src/**/*.stories.{ts,tsx}',
          'src/**/types.ts',
          'src/**/constants.ts',
          'src/**/index.ts',
          '**/*.config.{ts,js}',
          '**/node_modules/**',
        ],
        
        // Coverage thresholds - enforced at 80%
        thresholds: {
          lines: 80,
          branches: 80,
          functions: 80,
          statements: 80,
          perFile: false, // Apply globally, not per-file
        },
        
        // Additional settings
        all: true, // Include all source files, even untested ones
        clean: true, // Clean coverage results before running
        cleanOnRerun: true,
        reportOnFailure: true, // Generate report even if tests fail
        skipFull: false, // Don't skip files with 100% coverage
      },

      // Test execution settings
      pool: 'threads',
      poolOptions: {
        threads: {
          minThreads: 1,
          maxThreads: 4,
        },
      },
      
      // Timeouts
      testTimeout: 10000,
      hookTimeout: 10000,
      teardownTimeout: 10000,
      
      // Retry configuration for flaky tests
      retry: 0, // Don't retry by default, catch flaky tests
      
      // Test isolation
      isolate: true,
      
      // Reporters
      reporters: ['default', 'html', 'json'],
      
      // Output
      outputFile: {
        json: './test-results/vitest-results.json',
        html: './test-results/index.html',
      },
      
      // Silent console output in tests
      silent: false,
      
      // UI settings (for vitest --ui)
      ui: {
        port: 51204,
        open: false,
      },
      
      // Watch settings
      watch: false, // Disable watch mode for CI
      
      // Environment variables for tests
      env: {
        NODE_ENV: 'test',
      },
    },
  })
);
