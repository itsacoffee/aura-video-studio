/**
 * Enhanced Routes Configuration with Metadata and Guards
 * Extends config/routes.ts with guard support and metadata
 */

import { dependencyChecker } from '../services/dependencyChecker';
import { hasCompletedFirstRun } from '../services/firstRunService';
import type { RouteMetadata } from '../services/navigationService';

/**
 * Guard: Check if first-run setup is completed
 */
export async function firstRunGuard(): Promise<boolean> {
  try {
    return await hasCompletedFirstRun();
  } catch (error) {
    console.error('First-run guard check failed:', error);
    return false;
  }
}

/**
 * Guard: Check if FFmpeg is available
 */
export async function ffmpegGuard(): Promise<boolean> {
  try {
    const status = await dependencyChecker.checkFFmpeg();
    return status.installed;
  } catch (error) {
    console.error('FFmpeg guard check failed:', error);
    return false;
  }
}

/**
 * Guard: Check if settings are configured
 */
export async function settingsGuard(): Promise<boolean> {
  try {
    return localStorage.getItem('openai_api_key') !== null;
  } catch (error) {
    console.error('Settings guard check failed:', error);
    return false;
  }
}

/**
 * Enhanced route metadata with guards
 */
export const ROUTE_METADATA_ENHANCED: RouteMetadata[] = [
  {
    path: '/',
    title: 'Welcome',
    description: 'Getting started with Aura Video Studio',
  },
  {
    path: '/setup',
    title: 'Setup',
    description: 'First-run setup wizard',
  },
  {
    path: '/dashboard',
    title: 'Dashboard',
    description: 'Overview of your projects and recent activity',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/ideation',
    title: 'Ideation',
    description: 'AI-powered brainstorming and concept generation',
    requiresFirstRun: true,
    requiresSettings: true,
    guards: [firstRunGuard, settingsGuard],
  },
  {
    path: '/trending',
    title: 'Trending Topics',
    description: 'Explore trending topics and content ideas',
    requiresFirstRun: true,
    requiresSettings: true,
    guards: [firstRunGuard, settingsGuard],
  },
  {
    path: '/create',
    title: 'Create',
    description: 'Create a new video project',
    requiresFirstRun: true,
    requiresFFmpeg: true,
    requiresSettings: true,
    guards: [firstRunGuard, ffmpegGuard, settingsGuard],
  },
  {
    path: '/create/advanced',
    title: 'Advanced Create',
    description: 'Advanced video creation wizard',
    requiresFirstRun: true,
    requiresFFmpeg: true,
    requiresSettings: true,
    guards: [firstRunGuard, ffmpegGuard, settingsGuard],
  },
  {
    path: '/create/legacy',
    title: 'Legacy Create',
    description: 'Legacy video creation interface',
    requiresFirstRun: true,
    requiresFFmpeg: true,
    requiresSettings: true,
    guards: [firstRunGuard, ffmpegGuard, settingsGuard],
  },
  {
    path: '/editor',
    title: 'Video Editor',
    description: 'Edit your video timeline',
    requiresFirstRun: true,
    requiresFFmpeg: true,
    guards: [firstRunGuard, ffmpegGuard],
  },
  {
    path: '/editor/:jobId',
    title: 'Timeline Editor',
    description: 'Edit specific video timeline',
    requiresFirstRun: true,
    requiresFFmpeg: true,
    guards: [firstRunGuard, ffmpegGuard],
  },
  {
    path: '/projects',
    title: 'Projects',
    description: 'Manage your video projects',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/assets',
    title: 'Asset Library',
    description: 'Manage media assets and stock content',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/rag',
    title: 'Document Management',
    description: 'Manage RAG documents and knowledge base',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/render',
    title: 'Render',
    description: 'Render and export videos',
    requiresFirstRun: true,
    requiresFFmpeg: true,
    guards: [firstRunGuard, ffmpegGuard],
  },
  {
    path: '/settings',
    title: 'Settings',
    description: 'Configure application settings',
  },
  {
    path: '/logs',
    title: 'Logs',
    description: 'View application logs',
  },
  {
    path: '/diagnostics',
    title: 'Diagnostics',
    description: 'System diagnostics and troubleshooting',
  },
  {
    path: '/health',
    title: 'System Health',
    description: 'View system health diagnostics',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/jobs',
    title: 'Recent Jobs',
    description: 'View recent video generation jobs',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/templates',
    title: 'Templates',
    description: 'Browse video templates',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/pacing',
    title: 'Pacing Analyzer',
    description: 'Analyze video pacing and timing',
    requiresFirstRun: true,
    requiresFFmpeg: true,
    guards: [firstRunGuard, ffmpegGuard],
  },
  {
    path: '/platform',
    title: 'Platform Optimizer',
    description: 'Optimize videos for different platforms',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/quality',
    title: 'Quality Dashboard',
    description: 'Video quality analysis and metrics',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/content-planning',
    title: 'Content Planning',
    description: 'Plan and schedule content',
    requiresFirstRun: true,
    requiresSettings: true,
    guards: [firstRunGuard, settingsGuard],
  },
  {
    path: '/downloads',
    title: 'Program Dependencies',
    description: 'Download and manage program dependencies',
  },
  {
    path: '/ai-editing',
    title: 'AI Editing',
    description: 'AI-powered video editing',
    requiresFirstRun: true,
    requiresFFmpeg: true,
    requiresSettings: true,
    guards: [firstRunGuard, ffmpegGuard, settingsGuard],
  },
  {
    path: '/aesthetics',
    title: 'Visual Aesthetics',
    description: 'Visual style and aesthetics controls',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/localization',
    title: 'Localization',
    description: 'Manage translations and localization',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/prompt-management',
    title: 'Prompt Management',
    description: 'Manage AI prompts and templates',
    requiresFirstRun: true,
    requiresSettings: true,
    guards: [firstRunGuard, settingsGuard],
  },
  {
    path: '/voice-enhancement',
    title: 'Voice Enhancement',
    description: 'Enhance and process voice audio',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/performance-analytics',
    title: 'Performance Analytics',
    description: 'Analyze system and video performance',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/usage-analytics',
    title: 'Usage Analytics',
    description: 'Track application usage and metrics',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/ml-lab',
    title: 'ML Lab',
    description: 'Machine learning experiments and training',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/quality-validation',
    title: 'Quality Validation',
    description: 'Validate video quality and output',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/validation',
    title: 'Brief Validation',
    description: 'Validate video briefs and requirements',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/verification',
    title: 'Content Verification',
    description: 'Verify content for quality and compliance',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
  {
    path: '/admin',
    title: 'Admin Dashboard',
    description: 'Administration and system management',
    requiresFirstRun: true,
    guards: [firstRunGuard],
  },
];
