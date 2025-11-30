import type { NextConfig } from "next";
import { withBotId } from "botid/next/config";

const nextConfig: NextConfig = {
  compiler: {
    removeConsole: process.env.NODE_ENV === "production",
  },
  reactStrictMode: true,
  productionBrowserSourceMaps: true,
  output: "standalone",
  // Disable TypeScript build errors for offline builds where type resolution may fail
  // due to npm workspace module resolution differences from bun
  typescript: {
    ignoreBuildErrors: true,
  },
  // Disable ESLint during build for faster builds
  eslint: {
    ignoreDuringBuilds: true,
  },
  images: {
    // Disable image optimization during build since it requires network access
    unoptimized: process.env.NEXT_PUBLIC_SKIP_IMAGE_OPTIMIZATION === "true",
    remotePatterns: [
      {
        protocol: "https",
        hostname: "plus.unsplash.com",
      },
      {
        protocol: "https",
        hostname: "images.unsplash.com",
      },
      {
        protocol: "https",
        hostname: "images.marblecms.com",
      },
      {
        protocol: "https",
        hostname: "lh3.googleusercontent.com",
      },
      {
        protocol: "https",
        hostname: "avatars.githubusercontent.com",
      },
      {
        protocol: "https",
        hostname: "api.iconify.design",
      },
      {
        protocol: "https",
        hostname: "api.simplesvg.com",
      },
      {
        protocol: "https",
        hostname: "api.unisvg.com",
      },
    ],
  },
  // Configure headers for embedding in Aura Video Studio
  async headers() {
    // Check if running embedded in Aura
    const isEmbedded = process.env.NEXT_PUBLIC_AURA_EMBEDDED === "true";
    
    return [
      {
        // Apply to all routes
        source: "/:path*",
        headers: [
          // Allow embedding in iframe when running in Aura
          {
            key: "X-Frame-Options",
            value: isEmbedded ? "ALLOWALL" : "SAMEORIGIN",
          },
          {
            key: "Content-Security-Policy",
            value: isEmbedded
              ? "frame-ancestors *"
              : "frame-ancestors 'self'",
          },
        ],
      },
      {
        // API routes get CORS headers
        source: "/api/:path*",
        headers: [
          {
            key: "Access-Control-Allow-Origin",
            value: "*",
          },
          {
            key: "Access-Control-Allow-Methods",
            value: "GET, POST, PUT, DELETE, OPTIONS",
          },
          {
            key: "Access-Control-Allow-Headers",
            value: "Content-Type, Authorization",
          },
        ],
      },
    ];
  },
};

export default withBotId(nextConfig);
