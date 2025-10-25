import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'
import { visualizer } from 'rollup-plugin-visualizer'
import viteCompression from 'vite-plugin-compression'

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  const isProduction = mode === 'production'
  
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
    ],
    // Use relative base path for production to work when served from Aura.Api
    base: '/',
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
        }
      }
    },
    build: {
      outDir: 'dist',
      // Only generate source maps in development or as hidden source maps in production
      sourcemap: isProduction ? 'hidden' : true,
      // Increase warning limit but maintain awareness of large chunks
      chunkSizeWarningLimit: 600,
      emptyOutDir: true,
      assetsDir: 'assets',
      // Enable minification with terser for better compression
      minify: 'terser',
      terserOptions: {
        compress: {
          drop_console: isProduction, // Remove console.logs in production
          drop_debugger: isProduction, // Remove debugger statements in production
          pure_funcs: isProduction ? ['console.log', 'console.info', 'console.debug'] : [],
        },
      },
      rollupOptions: {
        output: {
          entryFileNames: 'assets/[name]-[hash].js',
          chunkFileNames: 'assets/[name]-[hash].js',
          assetFileNames: 'assets/[name]-[hash].[ext]',
          // Improved code splitting strategy
          manualChunks: (id) => {
            // Core React libraries
            if (id.includes('node_modules/react') || 
                id.includes('node_modules/react-dom') || 
                id.includes('node_modules/react-router')) {
              return 'react-vendor';
            }
            // Fluent UI components - split into smaller chunks
            if (id.includes('@fluentui/react-components')) {
              return 'fluent-components';
            }
            if (id.includes('@fluentui/react-icons')) {
              return 'fluent-icons';
            }
            // State management
            if (id.includes('node_modules/zustand')) {
              return 'state-vendor';
            }
            // Form libraries
            if (id.includes('node_modules/react-hook-form') || 
                id.includes('node_modules/zod')) {
              return 'form-vendor';
            }
            // HTTP client
            if (id.includes('node_modules/axios')) {
              return 'http-vendor';
            }
            // FFmpeg - large library, keep separate
            if (id.includes('@ffmpeg')) {
              return 'ffmpeg-vendor';
            }
            // Wavesurfer - audio visualization
            if (id.includes('wavesurfer')) {
              return 'audio-vendor';
            }
            // All other node_modules
            if (id.includes('node_modules')) {
              return 'vendor';
            }
          },
        },
      },
      // Asset optimization
      assetsInlineLimit: 4096, // Inline assets smaller than 4KB as base64
      cssCodeSplit: true, // Split CSS into separate files per chunk
    },
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
      include: ['src/**/*.test.{ts,tsx}'],
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
          perFile: true
        },
        all: false,
      }
    }
  }
})
