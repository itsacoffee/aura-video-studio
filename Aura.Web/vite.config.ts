import { defineConfig, Plugin } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';
import { visualizer } from 'rollup-plugin-visualizer';
import viteCompression from 'vite-plugin-compression';
import fs from 'fs';

/**
 * Asset Verification Plugin
 * Verifies that critical assets are copied to dist and logs their status
 */
function assetVerificationPlugin(): Plugin {
  return {
    name: 'asset-verification',
    enforce: 'post',
    closeBundle() {
      console.log('\n📁 Asset Verification Report:\n');

      const distPath = path.resolve(__dirname, 'dist');
      const criticalAssets = [
        'favicon.ico',
        'favicon-16x16.png',
        'favicon-32x32.png',
        'logo256.png',
        'logo512.png',
        'vite.svg',
      ];

      const criticalDirs = ['assets', 'workspaces'];

      let hasErrors = false;

      // Check critical files
      for (const asset of criticalAssets) {
        const assetPath = path.join(distPath, asset);
        if (fs.existsSync(assetPath)) {
          const stats = fs.statSync(assetPath);
          console.log(`✓ ${asset} (${(stats.size / 1024).toFixed(2)} KB)`);
        } else {
          console.error(`✗ Missing: ${asset}`);
          hasErrors = true;
        }
      }

      // Check critical directories
      for (const dir of criticalDirs) {
        const dirPath = path.join(distPath, dir);
        if (fs.existsSync(dirPath) && fs.statSync(dirPath).isDirectory()) {
          const files = fs.readdirSync(dirPath);
          console.log(`✓ ${dir}/ (${files.length} items)`);
        } else {
          console.error(`✗ Missing directory: ${dir}/`);
          hasErrors = true;
        }
      }

      if (hasErrors) {
        console.error('\n⚠️  Some critical assets are missing!\n');
      } else {
        console.log('\n✓ All critical assets verified\n');
      }
    },
  };
}

/**
 * Performance Budget Plugin
 * Warns when bundle sizes exceed configured limits
 */
function performanceBudgetPlugin(): Plugin {
  // Performance budgets in KB - updated to realistic values after optimization
  const budgets = {
    'react-vendor': 200,
    'fluentui-components': 600, // Large UI library, acceptable size
    'fluentui-icons': 200,
    'ffmpeg-vendor': 500,
    'audio-vendor': 100,
    'animation-vendor': 150, // framer-motion
    'charts-vendor': 300, // recharts and d3
    'utils-vendor': 150, // fuse.js, date-fns
    'forms-vendor': 100, // react-hook-form, zod
    'http-vendor': 50, // axios
    vendor: 500, // Remaining vendor libraries
    total: 3300, // Total bundle size budget - realistic for feature-rich app
  };

  return {
    name: 'performance-budget',
    enforce: 'post',
    writeBundle(options, bundle) {
      const chunks: { [key: string]: number } = {};
      let totalSize = 0;

      // Calculate chunk sizes
      Object.entries(bundle).forEach(([fileName, chunk]) => {
        if (chunk.type === 'chunk' && 'code' in chunk) {
          const size = chunk.code.length / 1024; // Convert to KB
          totalSize += size;

          // Extract chunk name
          const chunkName = fileName.replace(/^assets\//, '').replace(/-[a-f0-9]+\.js$/, '');
          chunks[chunkName] = (chunks[chunkName] || 0) + size;
        }
      });

      // Check budgets
      console.log('\n📊 Performance Budget Report:\n');
      let hasViolations = false;

      Object.entries(chunks).forEach(([name, size]) => {
        const budget = budgets[name];
        if (budget && size > budget) {
          console.warn(`⚠️  ${name}: ${size.toFixed(2)}KB exceeds budget of ${budget}KB`);
          hasViolations = true;
        } else if (budget) {
          console.log(`✅ ${name}: ${size.toFixed(2)}KB (budget: ${budget}KB)`);
        }
      });

      // Check total size
      if (totalSize > budgets.total) {
        console.warn(
          `⚠️  Total bundle size: ${totalSize.toFixed(2)}KB exceeds budget of ${budgets.total}KB`
        );
        hasViolations = true;
      } else {
        console.log(`✅ Total bundle size: ${totalSize.toFixed(2)}KB (budget: ${budgets.total}KB)`);
      }

      if (hasViolations) {
        console.warn(
          '\n⚠️  Performance budget violations detected! Consider code splitting or removing unused dependencies.\n'
        );
      } else {
        console.log('\n✅ All performance budgets met!\n');
      }
    },
  };
}

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  const isProduction = mode === 'production';

  return {
    plugins: [
      react(),
      // Generate bundle analysis report
      visualizer({
        filename: './dist/stats.html',
        open: false,
        gzipSize: true,
        brotliSize: true,
      }),
      // Compress assets with gzip and brotli
      viteCompression({
        algorithm: 'gzip',
        ext: '.gz',
        threshold: 1024, // Only compress files larger than 1KB
      }),
      viteCompression({
        algorithm: 'brotliCompress',
        ext: '.br',
        threshold: 1024,
      }),
      // Performance budget plugin
      performanceBudgetPlugin(),
      // Asset verification plugin
      assetVerificationPlugin(),
    ],
    // Use relative base path for Electron compatibility (file:// protocol)
    // This ensures all paths in index.html are relative (./assets/...) instead of absolute (/assets/...)
    base: './',
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
      },
    },
    server: {
      port: 5173,
      open: true,
      proxy: {
        '/api': {
          target: 'http://127.0.0.1:5005',
          changeOrigin: true,
        },
      },
    },
    build: {
      outDir: 'dist',
      // Only generate source maps in development or as hidden source maps in production
      sourcemap: isProduction ? 'hidden' : true,
      // Increase warning limit to realistic threshold after manual chunk optimization
      // Large chunks are intentionally split for optimal loading performance
      chunkSizeWarningLimit: 800,
      emptyOutDir: true,
      assetsDir: 'assets',
      // Target Electron's Chrome version (Electron 32 uses Chrome 128)
      target: 'chrome128',
      // Explicitly ensure assets are copied from public directory
      copyPublicDir: true,
      // Disable CSS code splitting for simpler Electron loading
      cssCodeSplit: false,
      // Use esbuild minification for better compatibility with Electron
      // terser was causing "Cannot access 'e' before initialization" errors
      minify: 'esbuild',
      // Keep console logs in production for debugging Electron app
      // terserOptions: {
      //   compress: {
      //     drop_console: isProduction,
      //     drop_debugger: isProduction,
      //     pure_funcs: isProduction ? ['console.log', 'console.info', 'console.debug'] : [],
      //     passes: 2,
      //   },
      //   mangle: {
      //     safari10: true,
      //   },
      // },
      rollupOptions: {
        output: {
          // Use ES module format for better Electron compatibility
          format: 'es',
          entryFileNames: 'assets/[name]-[hash].js',
          chunkFileNames: 'assets/[name]-[hash].js',
          assetFileNames: 'assets/[name]-[hash].[ext]',
          // Improved code splitting strategy with route-based chunks
          manualChunks: (id) => {
            // FFmpeg - large library, keep separate
            if (id.includes('@ffmpeg')) {
              return 'ffmpeg-vendor';
            }
            // Wavesurfer - audio visualization
            if (id.includes('wavesurfer')) {
              return 'audio-vendor';
            }
            // React core - separate chunk
            if (id.includes('node_modules/react/') || id.includes('node_modules/react-dom/')) {
              return 'react-vendor';
            }
            // Fluent UI Components - separate from icons to avoid circular dependencies
            // This includes all @fluentui/react-* packages except react-icons
            if (id.includes('@fluentui/react-') && !id.includes('@fluentui/react-icons')) {
              return 'fluentui-components';
            }
            // Fluent UI Icons - separate chunk
            if (id.includes('@fluentui/react-icons')) {
              return 'fluentui-icons';
            }
            // Router - separate chunk
            if (id.includes('react-router')) {
              return 'router-vendor';
            }
            // State management
            if (id.includes('zustand') || id.includes('@tanstack/react-query')) {
              return 'state-vendor';
            }
            // Animation libraries - separate chunk for framer-motion
            if (id.includes('framer-motion')) {
              return 'animation-vendor';
            }
            // Chart/visualization libraries
            if (id.includes('recharts') || id.includes('d3-')) {
              return 'charts-vendor';
            }
            // Search and utility libraries
            if (id.includes('fuse.js') || id.includes('date-fns')) {
              return 'utils-vendor';
            }
            // Form handling
            if (id.includes('react-hook-form') || id.includes('@hookform/') || id.includes('zod')) {
              return 'forms-vendor';
            }
            // HTTP client and related
            if (id.includes('axios')) {
              return 'http-vendor';
            }
            // All other node_modules go to vendor chunk
            if (id.includes('node_modules')) {
              return 'vendor';
            }
          },
        },
        treeshake: {
          moduleSideEffects: 'no-external', // Enable aggressive tree shaking
          propertyReadSideEffects: false,
          tryCatchDeoptimization: false,
        },
      },
      // Asset optimization
      assetsInlineLimit: 4096, // Inline assets smaller than 4KB as base64
      // Optimize dependencies
      commonjsOptions: {
        include: [/node_modules/],
        extensions: ['.js', '.cjs'],
      },
      // Performance optimizations
      reportCompressedSize: isProduction,
    },
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
      include: [
        'src/**/*.test.{ts,tsx}',
        'tests/smoke/**/*.test.ts',
        'tests/integration/**/*.test.{ts,tsx}',
      ],
      exclude: ['tests/e2e/**', 'node_modules/**'],
      coverage: {
        provider: 'v8',
        reporter: ['text', 'json', 'html'],
        include: ['src/**/*.{ts,tsx}'],
        exclude: ['src/**/*.test.{ts,tsx}', 'src/test/**'],
        thresholds: {
          lines: 70,
          branches: 70,
          statements: 70,
          perFile: true,
        },
        all: false,
      },
    },
  };
});
