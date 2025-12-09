/**
 * OpenCut Motion Graphics Store
 *
 * Manages motion graphics assets and applied graphics on the timeline.
 * Includes a curated library of professionally designed lower thirds,
 * callouts, social media elements, animated titles, and shape animations.
 */

import { create } from 'zustand';
import type {
  MotionGraphicAsset,
  AppliedGraphic,
  GraphicCategory,
  GraphicLayer,
  CustomizationSchema,
} from '../types/motionGraphics';

/**
 * Generate a unique ID for graphics
 */
function generateId(): string {
  return `graphic-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/**
 * Create a default text layer
 */
function createTextLayer(
  id: string,
  name: string,
  content: string,
  options: {
    x?: number;
    y?: number;
    fontSize?: number;
    fontWeight?: number;
    color?: string;
    opacity?: number;
  } = {}
): GraphicLayer {
  return {
    id,
    type: 'text',
    name,
    transform: {
      x: options.x ?? 50,
      y: options.y ?? 50,
      scaleX: 1,
      scaleY: 1,
      rotation: 0,
      anchorX: 50,
      anchorY: 50,
      opacity: options.opacity ?? 1,
    },
    blendMode: 'normal',
    animations: [],
    textProperties: {
      content,
      fontFamily: 'Inter, system-ui, sans-serif',
      fontSize: options.fontSize ?? 24,
      fontWeight: options.fontWeight ?? 400,
      fontStyle: 'normal',
      color: options.color ?? '#FFFFFF',
      textAlign: 'left',
      lineHeight: 1.4,
      letterSpacing: 0,
    },
  };
}

/**
 * Create a default shape layer
 */
function createShapeLayer(
  id: string,
  name: string,
  options: {
    x?: number;
    y?: number;
    width?: number;
    height?: number;
    fill?: string;
    cornerRadius?: number;
    opacity?: number;
  } = {}
): GraphicLayer {
  return {
    id,
    type: 'shape',
    name,
    transform: {
      x: options.x ?? 50,
      y: options.y ?? 50,
      scaleX: 1,
      scaleY: 1,
      rotation: 0,
      anchorX: 50,
      anchorY: 50,
      opacity: options.opacity ?? 1,
    },
    blendMode: 'normal',
    animations: [],
    shapeProperties: {
      shape: 'rectangle',
      width: options.width ?? 100,
      height: options.height ?? 50,
      fill: options.fill ?? '#000000',
      cornerRadius: options.cornerRadius ?? 0,
    },
  };
}

/**
 * Create a default customization schema
 */
function createCustomizationSchema(
  options: Partial<CustomizationSchema> = {}
): CustomizationSchema {
  return {
    text: options.text ?? [],
    colors: options.colors ?? [],
    layout: options.layout ?? [],
    animation: options.animation ?? [],
    advanced: options.advanced ?? [],
  };
}

/**
 * Built-in motion graphics library
 */
export const BUILTIN_GRAPHICS: MotionGraphicAsset[] = [
  // ============================================================================
  // LOWER THIRDS (5 variants)
  // ============================================================================
  {
    id: 'lt-editorial-minimal',
    name: 'Editorial Minimal',
    description:
      'Clean, minimalist lower third with elegant line reveal animation. Perfect for documentaries and interviews.',
    category: 'lower-thirds',
    tags: ['minimal', 'clean', 'documentary', 'interview', 'professional'],
    duration: 5,
    minDuration: 2,
    maxDuration: 30,
    layers: [
      createShapeLayer('lt-minimal-line', 'Accent Line', {
        x: 5,
        y: 85,
        width: 60,
        height: 2,
        fill: '#3B82F6',
      }),
      createTextLayer('lt-minimal-name', 'Name', 'John Smith', {
        x: 5,
        y: 80,
        fontSize: 32,
        fontWeight: 600,
        color: '#FFFFFF',
      }),
      createTextLayer('lt-minimal-title', 'Title', 'Creative Director', {
        x: 5,
        y: 88,
        fontSize: 18,
        fontWeight: 400,
        color: 'rgba(255,255,255,0.8)',
      }),
    ],
    customization: createCustomizationSchema({
      text: [
        {
          id: 'name',
          label: 'Name',
          layerId: 'lt-minimal-name',
          defaultValue: 'John Smith',
          maxLength: 50,
          placeholder: 'Enter name',
        },
        {
          id: 'title',
          label: 'Title',
          layerId: 'lt-minimal-title',
          defaultValue: 'Creative Director',
          maxLength: 60,
          placeholder: 'Enter title or role',
        },
      ],
      colors: [
        {
          id: 'accentColor',
          label: 'Accent Color',
          layerIds: ['lt-minimal-line'],
          property: 'fill',
          defaultValue: '#3B82F6',
          presets: ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899'],
        },
        {
          id: 'textColor',
          label: 'Text Color',
          layerIds: ['lt-minimal-name', 'lt-minimal-title'],
          property: 'color',
          defaultValue: '#FFFFFF',
          presets: ['#FFFFFF', '#000000', '#F3F4F6', '#1F2937'],
        },
      ],
      animation: [
        {
          id: 'entryDuration',
          label: 'Entry Duration',
          property: 'duration',
          defaultValue: 0.5,
          min: 0.1,
          max: 2,
        },
        {
          id: 'exitDuration',
          label: 'Exit Duration',
          property: 'duration',
          defaultValue: 0.4,
          min: 0.1,
          max: 2,
        },
      ],
    }),
    responsiveBreakpoints: [
      { aspectRatio: '16:9' },
      { aspectRatio: '9:16', fontScale: 0.8, transforms: { 'lt-minimal-name': { y: 70 } } },
      { aspectRatio: '1:1', fontScale: 0.9 },
    ],
    colorScheme: 'auto',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'lt-glass-morphism',
    name: 'Glass Morphism',
    description:
      'Modern frosted glass effect with vibrant gradient accents and backdrop blur. Contemporary and eye-catching.',
    category: 'lower-thirds',
    tags: ['modern', 'glass', 'blur', 'gradient', 'trendy'],
    duration: 5,
    minDuration: 2,
    maxDuration: 30,
    layers: [
      createShapeLayer('lt-glass-bg', 'Glass Background', {
        x: 5,
        y: 78,
        width: 300,
        height: 80,
        fill: 'rgba(255,255,255,0.1)',
        cornerRadius: 16,
        opacity: 0.9,
      }),
      createShapeLayer('lt-glass-accent', 'Gradient Accent', {
        x: 5,
        y: 78,
        width: 4,
        height: 80,
        fill: '#8B5CF6',
        cornerRadius: 2,
      }),
      createTextLayer('lt-glass-name', 'Name', 'Sarah Johnson', {
        x: 8,
        y: 82,
        fontSize: 28,
        fontWeight: 600,
        color: '#FFFFFF',
      }),
      createTextLayer('lt-glass-title', 'Title', 'UX Designer • TechCorp', {
        x: 8,
        y: 90,
        fontSize: 16,
        fontWeight: 400,
        color: 'rgba(255,255,255,0.7)',
      }),
    ],
    customization: createCustomizationSchema({
      text: [
        {
          id: 'name',
          label: 'Name',
          layerId: 'lt-glass-name',
          defaultValue: 'Sarah Johnson',
          maxLength: 50,
        },
        {
          id: 'title',
          label: 'Title',
          layerId: 'lt-glass-title',
          defaultValue: 'UX Designer • TechCorp',
          maxLength: 60,
        },
      ],
      colors: [
        {
          id: 'accentColor',
          label: 'Accent Color',
          layerIds: ['lt-glass-accent'],
          property: 'fill',
          defaultValue: '#8B5CF6',
          presets: ['#8B5CF6', '#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#EC4899'],
          allowGradient: true,
        },
      ],
      advanced: [
        {
          id: 'blurAmount',
          label: 'Blur Amount',
          type: 'slider',
          defaultValue: 20,
          min: 0,
          max: 50,
          step: 1,
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }, { aspectRatio: '9:16', fontScale: 0.85 }],
    colorScheme: 'dark',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'lt-broadcast-news',
    name: 'Broadcast News',
    description:
      'Professional broadcast-style lower third with animated ticker. Ideal for news, sports, and live events.',
    category: 'lower-thirds',
    tags: ['broadcast', 'news', 'sports', 'live', 'professional', 'ticker'],
    duration: 6,
    minDuration: 3,
    maxDuration: 60,
    layers: [
      createShapeLayer('lt-news-bar', 'Main Bar', {
        x: 0,
        y: 82,
        width: 100,
        height: 50,
        fill: '#1E3A8A',
      }),
      createShapeLayer('lt-news-accent', 'Accent Bar', {
        x: 0,
        y: 80,
        width: 100,
        height: 4,
        fill: '#EF4444',
      }),
      createTextLayer('lt-news-name', 'Name', 'BREAKING NEWS', {
        x: 2,
        y: 85,
        fontSize: 24,
        fontWeight: 700,
        color: '#FFFFFF',
      }),
      createTextLayer('lt-news-headline', 'Headline', 'Major developments in todays top story', {
        x: 2,
        y: 92,
        fontSize: 18,
        fontWeight: 400,
        color: 'rgba(255,255,255,0.9)',
      }),
    ],
    customization: createCustomizationSchema({
      text: [
        {
          id: 'name',
          label: 'Label',
          layerId: 'lt-news-name',
          defaultValue: 'BREAKING NEWS',
          maxLength: 30,
        },
        {
          id: 'headline',
          label: 'Headline',
          layerId: 'lt-news-headline',
          defaultValue: 'Major developments in todays top story',
          maxLength: 100,
        },
      ],
      colors: [
        {
          id: 'barColor',
          label: 'Bar Color',
          layerIds: ['lt-news-bar'],
          property: 'fill',
          defaultValue: '#1E3A8A',
          presets: ['#1E3A8A', '#991B1B', '#166534', '#1E40AF', '#7C2D12'],
        },
        {
          id: 'accentColor',
          label: 'Accent Color',
          layerIds: ['lt-news-accent'],
          property: 'fill',
          defaultValue: '#EF4444',
          presets: ['#EF4444', '#F59E0B', '#10B981', '#3B82F6'],
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }],
    colorScheme: 'dark',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'lt-cinematic-elegant',
    name: 'Cinematic Elegant',
    description:
      'Film-inspired design with sophisticated serif typography and delicate underline animation. Perfect for weddings and luxury content.',
    category: 'lower-thirds',
    tags: ['cinematic', 'elegant', 'wedding', 'luxury', 'film', 'serif'],
    duration: 5,
    minDuration: 2,
    maxDuration: 30,
    layers: [
      createTextLayer('lt-cine-name', 'Name', 'Elizabeth & James', {
        x: 50,
        y: 82,
        fontSize: 36,
        fontWeight: 400,
        color: '#FFFFFF',
      }),
      createShapeLayer('lt-cine-line', 'Decorative Line', {
        x: 35,
        y: 87,
        width: 120,
        height: 1,
        fill: 'rgba(255,255,255,0.5)',
      }),
      createTextLayer('lt-cine-subtitle', 'Subtitle', 'June 15, 2024 • Napa Valley', {
        x: 50,
        y: 90,
        fontSize: 16,
        fontWeight: 300,
        color: 'rgba(255,255,255,0.8)',
      }),
    ],
    customization: createCustomizationSchema({
      text: [
        {
          id: 'name',
          label: 'Names',
          layerId: 'lt-cine-name',
          defaultValue: 'Elizabeth & James',
          maxLength: 50,
        },
        {
          id: 'subtitle',
          label: 'Subtitle',
          layerId: 'lt-cine-subtitle',
          defaultValue: 'June 15, 2024 • Napa Valley',
          maxLength: 60,
        },
      ],
      colors: [
        {
          id: 'textColor',
          label: 'Text Color',
          layerIds: ['lt-cine-name', 'lt-cine-subtitle'],
          property: 'color',
          defaultValue: '#FFFFFF',
          presets: ['#FFFFFF', '#F5F5DC', '#D4AF37', '#F8F8FF'],
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }, { aspectRatio: '9:16', fontScale: 0.85 }],
    colorScheme: 'auto',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'lt-tech-startup',
    name: 'Tech Startup',
    description:
      'Dynamic design with animated gradient border for tech presentations and product demos. Modern and impactful.',
    category: 'lower-thirds',
    tags: ['tech', 'startup', 'modern', 'gradient', 'product', 'demo'],
    duration: 5,
    minDuration: 2,
    maxDuration: 30,
    layers: [
      createShapeLayer('lt-tech-bg', 'Background', {
        x: 3,
        y: 78,
        width: 320,
        height: 85,
        fill: 'rgba(0,0,0,0.8)',
        cornerRadius: 12,
      }),
      createShapeLayer('lt-tech-border', 'Gradient Border', {
        x: 3,
        y: 78,
        width: 320,
        height: 85,
        fill: 'transparent',
        cornerRadius: 12,
      }),
      createTextLayer('lt-tech-name', 'Name', 'Alex Chen', {
        x: 6,
        y: 82,
        fontSize: 28,
        fontWeight: 600,
        color: '#FFFFFF',
      }),
      createTextLayer('lt-tech-role', 'Role', 'CEO & Co-Founder', {
        x: 6,
        y: 89,
        fontSize: 16,
        fontWeight: 400,
        color: '#10B981',
      }),
    ],
    customization: createCustomizationSchema({
      text: [
        {
          id: 'name',
          label: 'Name',
          layerId: 'lt-tech-name',
          defaultValue: 'Alex Chen',
          maxLength: 40,
        },
        {
          id: 'role',
          label: 'Role',
          layerId: 'lt-tech-role',
          defaultValue: 'CEO & Co-Founder',
          maxLength: 50,
        },
      ],
      colors: [
        {
          id: 'accentColor',
          label: 'Accent Color',
          layerIds: ['lt-tech-role'],
          property: 'color',
          defaultValue: '#10B981',
          presets: ['#10B981', '#3B82F6', '#8B5CF6', '#F59E0B', '#EC4899'],
          allowGradient: true,
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }],
    colorScheme: 'dark',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },

  // ============================================================================
  // CALLOUTS (3 variants)
  // ============================================================================
  {
    id: 'co-focus-circle',
    name: 'Focus Circle',
    description:
      'Animated circular highlight with expanding ring and pulse effect. Perfect for drawing attention to specific areas.',
    category: 'callouts',
    tags: ['circle', 'highlight', 'pulse', 'focus', 'attention'],
    duration: 4,
    minDuration: 1,
    maxDuration: 20,
    layers: [
      createShapeLayer('co-circle-outer', 'Outer Ring', {
        x: 50,
        y: 50,
        width: 80,
        height: 80,
        fill: 'transparent',
        cornerRadius: 40,
      }),
      createShapeLayer('co-circle-inner', 'Inner Ring', {
        x: 50,
        y: 50,
        width: 60,
        height: 60,
        fill: 'transparent',
        cornerRadius: 30,
      }),
    ],
    customization: createCustomizationSchema({
      colors: [
        {
          id: 'ringColor',
          label: 'Ring Color',
          layerIds: ['co-circle-outer', 'co-circle-inner'],
          property: 'strokeColor',
          defaultValue: '#EF4444',
          presets: ['#EF4444', '#F59E0B', '#10B981', '#3B82F6', '#8B5CF6'],
        },
      ],
      layout: [
        {
          id: 'size',
          label: 'Size',
          property: 'size',
          defaultValue: 80,
          min: 40,
          max: 200,
          step: 10,
          unit: 'px',
        },
      ],
      advanced: [
        {
          id: 'pulseSpeed',
          label: 'Pulse Speed',
          type: 'slider',
          defaultValue: 1,
          min: 0.5,
          max: 3,
          step: 0.1,
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }],
    colorScheme: 'auto',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'co-arrow-pointer',
    name: 'Arrow Pointer',
    description:
      'Sleek animated arrow with optional text label. Ideal for tutorials and educational content.',
    category: 'callouts',
    tags: ['arrow', 'pointer', 'tutorial', 'education', 'guide'],
    duration: 4,
    minDuration: 1,
    maxDuration: 20,
    layers: [
      createShapeLayer('co-arrow-line', 'Arrow Line', {
        x: 30,
        y: 50,
        width: 100,
        height: 4,
        fill: '#3B82F6',
        cornerRadius: 2,
      }),
      createShapeLayer('co-arrow-head', 'Arrow Head', {
        x: 45,
        y: 50,
        width: 16,
        height: 16,
        fill: '#3B82F6',
      }),
      createTextLayer('co-arrow-label', 'Label', 'Click here', {
        x: 10,
        y: 45,
        fontSize: 18,
        fontWeight: 500,
        color: '#FFFFFF',
      }),
    ],
    customization: createCustomizationSchema({
      text: [
        {
          id: 'label',
          label: 'Label Text',
          layerId: 'co-arrow-label',
          defaultValue: 'Click here',
          maxLength: 40,
        },
      ],
      colors: [
        {
          id: 'arrowColor',
          label: 'Arrow Color',
          layerIds: ['co-arrow-line', 'co-arrow-head'],
          property: 'fill',
          defaultValue: '#3B82F6',
          presets: ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6'],
        },
      ],
      advanced: [
        {
          id: 'showLabel',
          label: 'Show Label',
          type: 'toggle',
          defaultValue: true,
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }],
    colorScheme: 'auto',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'co-tooltip-box',
    name: 'Tooltip Box',
    description:
      'Elegant tooltip-style callout with pointer tail and customizable content. Great for annotations.',
    category: 'callouts',
    tags: ['tooltip', 'annotation', 'info', 'box', 'note'],
    duration: 4,
    minDuration: 1,
    maxDuration: 20,
    layers: [
      createShapeLayer('co-tooltip-bg', 'Tooltip Background', {
        x: 50,
        y: 40,
        width: 200,
        height: 60,
        fill: 'rgba(0,0,0,0.9)',
        cornerRadius: 8,
      }),
      createShapeLayer('co-tooltip-pointer', 'Pointer', {
        x: 50,
        y: 55,
        width: 16,
        height: 16,
        fill: 'rgba(0,0,0,0.9)',
      }),
      createTextLayer('co-tooltip-text', 'Text', 'This is an important feature', {
        x: 50,
        y: 42,
        fontSize: 16,
        fontWeight: 400,
        color: '#FFFFFF',
      }),
    ],
    customization: createCustomizationSchema({
      text: [
        {
          id: 'text',
          label: 'Tooltip Text',
          layerId: 'co-tooltip-text',
          defaultValue: 'This is an important feature',
          maxLength: 100,
        },
      ],
      colors: [
        {
          id: 'bgColor',
          label: 'Background',
          layerIds: ['co-tooltip-bg', 'co-tooltip-pointer'],
          property: 'fill',
          defaultValue: 'rgba(0,0,0,0.9)',
          presets: [
            'rgba(0,0,0,0.9)',
            'rgba(59,130,246,0.95)',
            'rgba(16,185,129,0.95)',
            'rgba(239,68,68,0.95)',
          ],
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }],
    colorScheme: 'auto',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },

  // ============================================================================
  // SOCIAL MEDIA ELEMENTS (3 variants)
  // ============================================================================
  {
    id: 'sm-subscribe-button',
    name: 'Subscribe Button',
    description:
      'Eye-catching subscribe button with bell notification and click animation. Perfect for YouTube-style CTAs.',
    category: 'social',
    tags: ['subscribe', 'youtube', 'button', 'cta', 'notification', 'bell'],
    duration: 3,
    minDuration: 2,
    maxDuration: 15,
    layers: [
      createShapeLayer('sm-sub-button', 'Button Background', {
        x: 50,
        y: 50,
        width: 180,
        height: 50,
        fill: '#FF0000',
        cornerRadius: 25,
      }),
      createTextLayer('sm-sub-text', 'Button Text', 'SUBSCRIBE', {
        x: 50,
        y: 50,
        fontSize: 18,
        fontWeight: 700,
        color: '#FFFFFF',
      }),
    ],
    customization: createCustomizationSchema({
      text: [
        {
          id: 'buttonText',
          label: 'Button Text',
          layerId: 'sm-sub-text',
          defaultValue: 'SUBSCRIBE',
          maxLength: 20,
        },
      ],
      colors: [
        {
          id: 'buttonColor',
          label: 'Button Color',
          layerIds: ['sm-sub-button'],
          property: 'fill',
          defaultValue: '#FF0000',
          presets: ['#FF0000', '#3B82F6', '#10B981', '#8B5CF6', '#EC4899'],
        },
      ],
      advanced: [
        {
          id: 'showBell',
          label: 'Show Bell Icon',
          type: 'toggle',
          defaultValue: true,
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }, { aspectRatio: '9:16', fontScale: 0.9 }],
    colorScheme: 'auto',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'sm-like-heart',
    name: 'Like Heart',
    description:
      'Animated heart with particle burst effect. Perfect for engagement and reaction animations.',
    category: 'social',
    tags: ['like', 'heart', 'reaction', 'love', 'particles', 'instagram'],
    duration: 2,
    minDuration: 1,
    maxDuration: 10,
    layers: [
      createShapeLayer('sm-heart-shape', 'Heart', {
        x: 50,
        y: 50,
        width: 60,
        height: 60,
        fill: '#EF4444',
      }),
    ],
    customization: createCustomizationSchema({
      colors: [
        {
          id: 'heartColor',
          label: 'Heart Color',
          layerIds: ['sm-heart-shape'],
          property: 'fill',
          defaultValue: '#EF4444',
          presets: ['#EF4444', '#EC4899', '#8B5CF6', '#F59E0B'],
        },
      ],
      layout: [
        {
          id: 'size',
          label: 'Size',
          property: 'size',
          defaultValue: 60,
          min: 30,
          max: 150,
          step: 10,
          unit: 'px',
        },
      ],
      advanced: [
        {
          id: 'showParticles',
          label: 'Show Particles',
          type: 'toggle',
          defaultValue: true,
        },
        {
          id: 'particleCount',
          label: 'Particle Count',
          type: 'slider',
          defaultValue: 12,
          min: 4,
          max: 24,
          step: 2,
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }],
    colorScheme: 'auto',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'sm-follow-badge',
    name: 'Follow Me Badge',
    description:
      'Stylish social media badge supporting multiple platforms with animated entry. Customizable for any platform.',
    category: 'social',
    tags: ['follow', 'social', 'badge', 'instagram', 'twitter', 'tiktok', 'handle'],
    duration: 4,
    minDuration: 2,
    maxDuration: 20,
    layers: [
      createShapeLayer('sm-badge-bg', 'Badge Background', {
        x: 50,
        y: 50,
        width: 220,
        height: 55,
        fill: 'rgba(0,0,0,0.85)',
        cornerRadius: 28,
      }),
      createShapeLayer('sm-badge-icon', 'Platform Icon', {
        x: 22,
        y: 50,
        width: 36,
        height: 36,
        fill: '#E1306C',
        cornerRadius: 18,
      }),
      createTextLayer('sm-badge-handle', 'Handle', '@yourhandle', {
        x: 58,
        y: 50,
        fontSize: 20,
        fontWeight: 600,
        color: '#FFFFFF',
      }),
    ],
    customization: createCustomizationSchema({
      text: [
        {
          id: 'handle',
          label: 'Handle',
          layerId: 'sm-badge-handle',
          defaultValue: '@yourhandle',
          maxLength: 30,
          placeholder: '@username',
        },
      ],
      colors: [
        {
          id: 'platformColor',
          label: 'Platform Color',
          layerIds: ['sm-badge-icon'],
          property: 'fill',
          defaultValue: '#E1306C',
          presets: ['#E1306C', '#1DA1F2', '#000000', '#FF0050', '#0077B5'],
        },
      ],
      advanced: [
        {
          id: 'platform',
          label: 'Platform',
          type: 'select',
          defaultValue: 'instagram',
          options: [
            { label: 'Instagram', value: 'instagram' },
            { label: 'Twitter/X', value: 'twitter' },
            { label: 'TikTok', value: 'tiktok' },
            { label: 'LinkedIn', value: 'linkedin' },
            { label: 'YouTube', value: 'youtube' },
          ],
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }, { aspectRatio: '9:16', fontScale: 0.85 }],
    colorScheme: 'auto',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },

  // ============================================================================
  // ANIMATED TITLES (3 variants)
  // ============================================================================
  {
    id: 'tt-cinematic-reveal',
    name: 'Cinematic Reveal',
    description:
      'Dramatic title with lens flare and light leak effects. Perfect for trailers, intros, and cinematic content.',
    category: 'titles',
    tags: ['cinematic', 'dramatic', 'trailer', 'intro', 'lens-flare', 'epic'],
    duration: 4,
    minDuration: 2,
    maxDuration: 15,
    layers: [
      createTextLayer('tt-cine-main', 'Main Title', 'EPIC JOURNEY', {
        x: 50,
        y: 50,
        fontSize: 72,
        fontWeight: 700,
        color: '#FFFFFF',
      }),
      createTextLayer('tt-cine-sub', 'Subtitle', 'A Story of Adventure', {
        x: 50,
        y: 62,
        fontSize: 24,
        fontWeight: 300,
        color: 'rgba(255,255,255,0.8)',
      }),
    ],
    customization: createCustomizationSchema({
      text: [
        {
          id: 'mainTitle',
          label: 'Main Title',
          layerId: 'tt-cine-main',
          defaultValue: 'EPIC JOURNEY',
          maxLength: 30,
        },
        {
          id: 'subtitle',
          label: 'Subtitle',
          layerId: 'tt-cine-sub',
          defaultValue: 'A Story of Adventure',
          maxLength: 50,
        },
      ],
      colors: [
        {
          id: 'textColor',
          label: 'Text Color',
          layerIds: ['tt-cine-main', 'tt-cine-sub'],
          property: 'color',
          defaultValue: '#FFFFFF',
          presets: ['#FFFFFF', '#FFD700', '#F5F5DC'],
        },
      ],
      advanced: [
        {
          id: 'showLensFlare',
          label: 'Show Lens Flare',
          type: 'toggle',
          defaultValue: true,
        },
        {
          id: 'flareIntensity',
          label: 'Flare Intensity',
          type: 'slider',
          defaultValue: 0.7,
          min: 0,
          max: 1,
          step: 0.1,
        },
      ],
    }),
    responsiveBreakpoints: [
      { aspectRatio: '16:9' },
      { aspectRatio: '21:9' },
      { aspectRatio: '9:16', fontScale: 0.6 },
    ],
    colorScheme: 'dark',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'tt-glitch-distort',
    name: 'Glitch Distort',
    description:
      'Edgy title with RGB split and digital distortion effects. Ideal for tech, gaming, and cyberpunk content.',
    category: 'titles',
    tags: ['glitch', 'tech', 'gaming', 'cyberpunk', 'distort', 'rgb'],
    duration: 3,
    minDuration: 1,
    maxDuration: 10,
    layers: [
      createTextLayer('tt-glitch-text', 'Glitch Text', 'OVERRIDE', {
        x: 50,
        y: 50,
        fontSize: 80,
        fontWeight: 900,
        color: '#FFFFFF',
      }),
    ],
    customization: createCustomizationSchema({
      text: [
        {
          id: 'title',
          label: 'Title',
          layerId: 'tt-glitch-text',
          defaultValue: 'OVERRIDE',
          maxLength: 20,
        },
      ],
      colors: [
        {
          id: 'textColor',
          label: 'Text Color',
          layerIds: ['tt-glitch-text'],
          property: 'color',
          defaultValue: '#FFFFFF',
          presets: ['#FFFFFF', '#00FF00', '#FF00FF', '#00FFFF'],
        },
      ],
      advanced: [
        {
          id: 'glitchIntensity',
          label: 'Glitch Intensity',
          type: 'slider',
          defaultValue: 0.5,
          min: 0,
          max: 1,
          step: 0.1,
        },
        {
          id: 'rgbSplit',
          label: 'RGB Split Amount',
          type: 'slider',
          defaultValue: 5,
          min: 0,
          max: 20,
          step: 1,
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }],
    colorScheme: 'dark',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'tt-elegant-serif',
    name: 'Elegant Serif',
    description:
      'Sophisticated title with graceful fade and subtle particle effects. Perfect for weddings, luxury brands, and formal events.',
    category: 'titles',
    tags: ['elegant', 'serif', 'wedding', 'luxury', 'formal', 'sophisticated'],
    duration: 4,
    minDuration: 2,
    maxDuration: 15,
    layers: [
      createTextLayer('tt-elegant-main', 'Main Title', 'Timeless Elegance', {
        x: 50,
        y: 45,
        fontSize: 56,
        fontWeight: 400,
        color: '#FFFFFF',
      }),
      createShapeLayer('tt-elegant-divider', 'Divider', {
        x: 35,
        y: 55,
        width: 120,
        height: 1,
        fill: 'rgba(255,255,255,0.4)',
      }),
      createTextLayer('tt-elegant-sub', 'Subtitle', 'A Celebration of Love', {
        x: 50,
        y: 60,
        fontSize: 20,
        fontWeight: 300,
        color: 'rgba(255,255,255,0.9)',
      }),
    ],
    customization: createCustomizationSchema({
      text: [
        {
          id: 'mainTitle',
          label: 'Main Title',
          layerId: 'tt-elegant-main',
          defaultValue: 'Timeless Elegance',
          maxLength: 40,
        },
        {
          id: 'subtitle',
          label: 'Subtitle',
          layerId: 'tt-elegant-sub',
          defaultValue: 'A Celebration of Love',
          maxLength: 50,
        },
      ],
      colors: [
        {
          id: 'textColor',
          label: 'Text Color',
          layerIds: ['tt-elegant-main', 'tt-elegant-sub'],
          property: 'color',
          defaultValue: '#FFFFFF',
          presets: ['#FFFFFF', '#F5F5DC', '#D4AF37', '#FDF5E6'],
        },
      ],
      advanced: [
        {
          id: 'showParticles',
          label: 'Show Particles',
          type: 'toggle',
          defaultValue: true,
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }, { aspectRatio: '9:16', fontScale: 0.7 }],
    colorScheme: 'auto',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },

  // ============================================================================
  // SHAPE ANIMATIONS (3 variants)
  // ============================================================================
  {
    id: 'sh-line-reveal',
    name: 'Line Reveal',
    description:
      'Animated line drawing with customizable direction and timing. Great for separators and accents.',
    category: 'shapes',
    tags: ['line', 'reveal', 'draw', 'separator', 'accent', 'minimal'],
    duration: 2,
    minDuration: 0.5,
    maxDuration: 10,
    layers: [
      createShapeLayer('sh-line-main', 'Line', {
        x: 20,
        y: 50,
        width: 200,
        height: 2,
        fill: '#FFFFFF',
        cornerRadius: 1,
      }),
    ],
    customization: createCustomizationSchema({
      colors: [
        {
          id: 'lineColor',
          label: 'Line Color',
          layerIds: ['sh-line-main'],
          property: 'fill',
          defaultValue: '#FFFFFF',
          presets: ['#FFFFFF', '#3B82F6', '#10B981', '#F59E0B', '#EF4444'],
          allowGradient: true,
        },
      ],
      layout: [
        {
          id: 'width',
          label: 'Width',
          property: 'size',
          defaultValue: 200,
          min: 50,
          max: 500,
          step: 10,
          unit: 'px',
        },
        {
          id: 'thickness',
          label: 'Thickness',
          property: 'size',
          defaultValue: 2,
          min: 1,
          max: 10,
          step: 1,
          unit: 'px',
        },
      ],
      advanced: [
        {
          id: 'direction',
          label: 'Direction',
          type: 'select',
          defaultValue: 'left-to-right',
          options: [
            { label: 'Left to Right', value: 'left-to-right' },
            { label: 'Right to Left', value: 'right-to-left' },
            { label: 'Center Out', value: 'center-out' },
          ],
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }],
    colorScheme: 'auto',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'sh-circle-burst',
    name: 'Circle Burst',
    description:
      'Expanding circles with staggered animation. Perfect for emphasis and attention-grabbing moments.',
    category: 'shapes',
    tags: ['circle', 'burst', 'expand', 'rings', 'emphasis', 'attention'],
    duration: 2,
    minDuration: 0.5,
    maxDuration: 10,
    layers: [
      createShapeLayer('sh-burst-ring1', 'Ring 1', {
        x: 50,
        y: 50,
        width: 40,
        height: 40,
        fill: 'transparent',
        cornerRadius: 20,
      }),
      createShapeLayer('sh-burst-ring2', 'Ring 2', {
        x: 50,
        y: 50,
        width: 60,
        height: 60,
        fill: 'transparent',
        cornerRadius: 30,
      }),
      createShapeLayer('sh-burst-ring3', 'Ring 3', {
        x: 50,
        y: 50,
        width: 80,
        height: 80,
        fill: 'transparent',
        cornerRadius: 40,
      }),
    ],
    customization: createCustomizationSchema({
      colors: [
        {
          id: 'ringColor',
          label: 'Ring Color',
          layerIds: ['sh-burst-ring1', 'sh-burst-ring2', 'sh-burst-ring3'],
          property: 'strokeColor',
          defaultValue: '#3B82F6',
          presets: ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6'],
        },
      ],
      advanced: [
        {
          id: 'ringCount',
          label: 'Number of Rings',
          type: 'slider',
          defaultValue: 3,
          min: 2,
          max: 6,
          step: 1,
        },
        {
          id: 'staggerDelay',
          label: 'Stagger Delay',
          type: 'slider',
          defaultValue: 0.1,
          min: 0.05,
          max: 0.5,
          step: 0.05,
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }],
    colorScheme: 'auto',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'sh-geometric-pattern',
    name: 'Geometric Pattern',
    description:
      'Animated geometric shapes with loop options. Modern and versatile for backgrounds and transitions.',
    category: 'shapes',
    tags: ['geometric', 'pattern', 'loop', 'background', 'abstract', 'modern'],
    duration: 3,
    minDuration: 1,
    maxDuration: 30,
    layers: [
      createShapeLayer('sh-geo-square1', 'Square 1', {
        x: 30,
        y: 40,
        width: 50,
        height: 50,
        fill: 'rgba(59,130,246,0.3)',
        cornerRadius: 4,
      }),
      createShapeLayer('sh-geo-square2', 'Square 2', {
        x: 50,
        y: 50,
        width: 40,
        height: 40,
        fill: 'rgba(139,92,246,0.3)',
        cornerRadius: 4,
      }),
      createShapeLayer('sh-geo-circle', 'Circle', {
        x: 70,
        y: 35,
        width: 35,
        height: 35,
        fill: 'rgba(16,185,129,0.3)',
        cornerRadius: 17.5,
      }),
    ],
    customization: createCustomizationSchema({
      colors: [
        {
          id: 'color1',
          label: 'Color 1',
          layerIds: ['sh-geo-square1'],
          property: 'fill',
          defaultValue: 'rgba(59,130,246,0.3)',
          presets: ['rgba(59,130,246,0.3)', 'rgba(16,185,129,0.3)', 'rgba(245,158,11,0.3)'],
        },
        {
          id: 'color2',
          label: 'Color 2',
          layerIds: ['sh-geo-square2'],
          property: 'fill',
          defaultValue: 'rgba(139,92,246,0.3)',
          presets: ['rgba(139,92,246,0.3)', 'rgba(236,72,153,0.3)', 'rgba(239,68,68,0.3)'],
        },
        {
          id: 'color3',
          label: 'Color 3',
          layerIds: ['sh-geo-circle'],
          property: 'fill',
          defaultValue: 'rgba(16,185,129,0.3)',
          presets: ['rgba(16,185,129,0.3)', 'rgba(59,130,246,0.3)', 'rgba(245,158,11,0.3)'],
        },
      ],
      advanced: [
        {
          id: 'loop',
          label: 'Loop Animation',
          type: 'toggle',
          defaultValue: true,
        },
        {
          id: 'speed',
          label: 'Animation Speed',
          type: 'slider',
          defaultValue: 1,
          min: 0.5,
          max: 2,
          step: 0.1,
        },
      ],
    }),
    responsiveBreakpoints: [{ aspectRatio: '16:9' }],
    colorScheme: 'auto',
    isPremium: false,
    version: '1.0.0',
    createdAt: new Date().toISOString(),
  },
];

// ============================================================================
// Store State & Actions
// ============================================================================

/** Motion graphics store state */
interface MotionGraphicsState {
  /** All available motion graphic assets */
  assets: MotionGraphicAsset[];
  /** Applied graphics on the timeline */
  applied: AppliedGraphic[];
  /** Currently selected graphic ID (applied instance) */
  selectedGraphicId: string | null;
  /** Current search query */
  searchQuery: string;
  /** Current filter category */
  filterCategory: GraphicCategory | 'all';
  /** Asset ID currently being previewed */
  previewingAssetId: string | null;
}

/** Motion graphics store actions */
interface MotionGraphicsActions {
  /** Add a graphic to the timeline */
  addGraphic: (assetId: string, trackId: string, startTime: number) => string;
  /** Remove a graphic from the timeline */
  removeGraphic: (graphicId: string) => void;
  /** Update an applied graphic's properties */
  updateGraphic: (graphicId: string, updates: Partial<AppliedGraphic>) => void;
  /** Update a single customization value */
  updateGraphicValue: (graphicId: string, key: string, value: string | number | boolean) => void;
  /** Duplicate a graphic with offset timing */
  duplicateGraphic: (graphicId: string) => string;
  /** Select a graphic for editing */
  selectGraphic: (graphicId: string | null) => void;
  /** Set search query */
  setSearchQuery: (query: string) => void;
  /** Set filter category */
  setFilterCategory: (category: GraphicCategory | 'all') => void;
  /** Set previewing asset */
  setPreviewingAsset: (assetId: string | null) => void;
  /** Get asset by ID */
  getAsset: (assetId: string) => MotionGraphicAsset | undefined;
  /** Get assets by category */
  getAssetsByCategory: (category: GraphicCategory) => MotionGraphicAsset[];
  /** Get graphics on a specific track */
  getGraphicsForTrack: (trackId: string) => AppliedGraphic[];
  /** Get graphics in time range */
  getGraphicsInRange: (startTime: number, endTime: number) => AppliedGraphic[];
  /** Search assets by query */
  searchAssets: (query: string) => MotionGraphicAsset[];
  /** Get currently selected graphic */
  getSelectedGraphic: () => AppliedGraphic | undefined;
  /** Get filtered assets based on current state */
  getFilteredAssets: () => MotionGraphicAsset[];
}

export type MotionGraphicsStore = MotionGraphicsState & MotionGraphicsActions;

/**
 * Motion Graphics Zustand Store
 */
export const useMotionGraphicsStore = create<MotionGraphicsStore>((set, get) => ({
  // Initial state
  assets: BUILTIN_GRAPHICS,
  applied: [],
  selectedGraphicId: null,
  searchQuery: '',
  filterCategory: 'all',
  previewingAssetId: null,

  // Actions
  addGraphic: (assetId, trackId, startTime) => {
    const asset = get().getAsset(assetId);
    if (!asset) return '';

    const id = generateId();
    const defaultValues: Record<string, string | number | boolean> = {};

    // Initialize default values from customization schema
    asset.customization.text.forEach((field) => {
      defaultValues[field.id] = field.defaultValue;
    });
    asset.customization.colors.forEach((field) => {
      defaultValues[field.id] = field.defaultValue;
    });
    asset.customization.layout.forEach((field) => {
      defaultValues[field.id] = field.defaultValue;
    });
    asset.customization.animation.forEach((field) => {
      defaultValues[field.id] = field.defaultValue;
    });
    asset.customization.advanced.forEach((field) => {
      defaultValues[field.id] = field.defaultValue;
    });

    const appliedGraphic: AppliedGraphic = {
      id,
      assetId,
      trackId,
      startTime,
      duration: asset.duration,
      customValues: defaultValues,
      positionX: 50,
      positionY: 50,
      scale: 1,
      opacity: 1,
      lockAspectRatio: true,
    };

    set((state) => ({
      applied: [...state.applied, appliedGraphic],
      selectedGraphicId: id,
    }));

    return id;
  },

  removeGraphic: (graphicId) => {
    set((state) => ({
      applied: state.applied.filter((g) => g.id !== graphicId),
      selectedGraphicId: state.selectedGraphicId === graphicId ? null : state.selectedGraphicId,
    }));
  },

  updateGraphic: (graphicId, updates) => {
    set((state) => ({
      applied: state.applied.map((g) => (g.id === graphicId ? { ...g, ...updates } : g)),
    }));
  },

  updateGraphicValue: (graphicId, key, value) => {
    set((state) => ({
      applied: state.applied.map((g) =>
        g.id === graphicId
          ? {
              ...g,
              customValues: { ...g.customValues, [key]: value },
            }
          : g
      ),
    }));
  },

  duplicateGraphic: (graphicId) => {
    const graphic = get().applied.find((g) => g.id === graphicId);
    if (!graphic) return '';

    const id = generateId();
    const duplicated: AppliedGraphic = {
      ...graphic,
      id,
      startTime: graphic.startTime + graphic.duration + 0.5, // Offset by duration + 0.5s
      customValues: { ...graphic.customValues },
    };

    set((state) => ({
      applied: [...state.applied, duplicated],
      selectedGraphicId: id,
    }));

    return id;
  },

  selectGraphic: (graphicId) => {
    set({ selectedGraphicId: graphicId });
  },

  setSearchQuery: (query) => {
    set({ searchQuery: query });
  },

  setFilterCategory: (category) => {
    set({ filterCategory: category });
  },

  setPreviewingAsset: (assetId) => {
    set({ previewingAssetId: assetId });
  },

  getAsset: (assetId) => {
    return get().assets.find((a) => a.id === assetId);
  },

  getAssetsByCategory: (category) => {
    return get().assets.filter((a) => a.category === category);
  },

  getGraphicsForTrack: (trackId) => {
    return get().applied.filter((g) => g.trackId === trackId);
  },

  getGraphicsInRange: (startTime, endTime) => {
    return get().applied.filter((g) => {
      const graphicEnd = g.startTime + g.duration;
      return g.startTime < endTime && graphicEnd > startTime;
    });
  },

  searchAssets: (query) => {
    const lowerQuery = query.toLowerCase();
    return get().assets.filter(
      (a) =>
        a.name.toLowerCase().includes(lowerQuery) ||
        a.description.toLowerCase().includes(lowerQuery) ||
        a.tags.some((tag) => tag.toLowerCase().includes(lowerQuery))
    );
  },

  getSelectedGraphic: () => {
    const { applied, selectedGraphicId } = get();
    return applied.find((g) => g.id === selectedGraphicId);
  },

  getFilteredAssets: () => {
    const { assets, searchQuery, filterCategory } = get();

    let filtered = assets;

    // Filter by category
    if (filterCategory !== 'all') {
      filtered = filtered.filter((a) => a.category === filterCategory);
    }

    // Filter by search query
    if (searchQuery) {
      const lowerQuery = searchQuery.toLowerCase();
      filtered = filtered.filter(
        (a) =>
          a.name.toLowerCase().includes(lowerQuery) ||
          a.description.toLowerCase().includes(lowerQuery) ||
          a.tags.some((tag) => tag.toLowerCase().includes(lowerQuery))
      );
    }

    return filtered;
  },
}));

export default useMotionGraphicsStore;
