/**
 * OpenCut Motion Graphics Store
 *
 * Manages motion graphics assets and applied instances for the OpenCut editor.
 * Includes a curated library of professionally designed lower thirds, callouts,
 * social media elements, animated titles, and shape animations.
 */

import { create } from 'zustand';
import type {
  MotionGraphicAsset,
  AppliedGraphic,
  MotionGraphicsState,
  MotionGraphicsActions,
  GraphicLayer,
  TextLayerProperties,
  ShapeLayerProperties,
  LayerTransform,
} from '../types/motionGraphics';

function generateId(): string {
  return `graphic-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/** Default transform for layers */
const defaultTransform: LayerTransform = {
  x: 50,
  y: 50,
  scaleX: 100,
  scaleY: 100,
  rotation: 0,
  opacity: 100,
  anchorX: 50,
  anchorY: 50,
};

/** Default text properties */
const defaultTextProps: TextLayerProperties = {
  content: 'Text',
  fontFamily: 'Inter, system-ui, sans-serif',
  fontSize: 24,
  fontWeight: 600,
  fontStyle: 'normal',
  textAlign: 'left',
  lineHeight: 1.2,
  letterSpacing: 0,
  color: '#ffffff',
};

/** Default shape properties */
const defaultShapeProps: ShapeLayerProperties = {
  shapeType: 'rectangle',
  width: 100,
  height: 40,
  cornerRadius: 4,
  fillColor: '#3B82F6',
};

/**
 * Create a text layer
 */
function createTextLayer(
  id: string,
  name: string,
  transform: Partial<LayerTransform>,
  textProps: Partial<TextLayerProperties>
): GraphicLayer {
  return {
    id,
    name,
    type: 'text',
    transform: { ...defaultTransform, ...transform },
    blendMode: 'normal',
    visible: true,
    zIndex: 1,
    textProperties: { ...defaultTextProps, ...textProps },
    entryAnimation: {
      style: 'fade',
      duration: 0.3,
      easing: 'easeOut',
    },
    exitAnimation: {
      style: 'fade',
      duration: 0.3,
      easing: 'easeIn',
    },
  };
}

/**
 * Create a shape layer
 */
function createShapeLayer(
  id: string,
  name: string,
  transform: Partial<LayerTransform>,
  shapeProps: Partial<ShapeLayerProperties>
): GraphicLayer {
  return {
    id,
    name,
    type: 'shape',
    transform: { ...defaultTransform, ...transform },
    blendMode: 'normal',
    visible: true,
    zIndex: 0,
    shapeProperties: { ...defaultShapeProps, ...shapeProps },
    entryAnimation: {
      style: 'slide',
      duration: 0.4,
      easing: 'easeOut',
      direction: 'left',
    },
    exitAnimation: {
      style: 'slide',
      duration: 0.3,
      easing: 'easeIn',
      direction: 'left',
    },
  };
}

/**
 * Built-in motion graphics library
 */
export const BUILTIN_GRAPHICS: MotionGraphicAsset[] = [
  // ============ LOWER THIRDS ============
  {
    id: 'lower-third-editorial-minimal',
    name: 'Editorial Minimal',
    description:
      'Clean, minimalist lower third with elegant line reveal animation. Perfect for documentaries and interviews.',
    category: 'lower-thirds',
    tags: ['minimal', 'clean', 'documentary', 'interview', 'professional'],
    duration: 5,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createTextLayer(
        'name',
        'Name',
        { x: 10, y: 85, anchorX: 0, anchorY: 100 },
        {
          content: 'John Smith',
          fontSize: 28,
          fontWeight: 700,
          color: '#ffffff',
        }
      ),
      createTextLayer(
        'title',
        'Title',
        { x: 10, y: 90, anchorX: 0, anchorY: 100 },
        {
          content: 'Creative Director',
          fontSize: 16,
          fontWeight: 400,
          color: '#a1a1aa',
        }
      ),
      createShapeLayer(
        'line',
        'Accent Line',
        { x: 10, y: 82, anchorX: 0, anchorY: 100 },
        {
          shapeType: 'rectangle',
          width: 40,
          height: 2,
          fillColor: '#3B82F6',
          cornerRadius: 1,
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'name',
          label: 'Name',
          type: 'text',
          defaultValue: 'John Smith',
          category: 'text',
          maxLength: 50,
          placeholder: 'Enter name',
        },
        {
          id: 'title',
          label: 'Title',
          type: 'text',
          defaultValue: 'Creative Director',
          category: 'text',
          maxLength: 60,
          placeholder: 'Enter title',
        },
        {
          id: 'accentColor',
          label: 'Accent Color',
          type: 'color',
          defaultValue: '#3B82F6',
          category: 'colors',
        },
        {
          id: 'textColor',
          label: 'Text Color',
          type: 'color',
          defaultValue: '#ffffff',
          category: 'colors',
        },
      ],
    },
    defaultValues: {
      name: 'John Smith',
      title: 'Creative Director',
      accentColor: '#3B82F6',
      textColor: '#ffffff',
    },
    colorSchemes: ['dark', 'light'],
  },
  {
    id: 'lower-third-glass-morphism',
    name: 'Glass Morphism',
    description: 'Modern frosted glass effect with vibrant gradient accents and backdrop blur.',
    category: 'lower-thirds',
    tags: ['modern', 'glass', 'gradient', 'blur', 'trendy'],
    duration: 5,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createShapeLayer(
        'background',
        'Glass Background',
        { x: 10, y: 80, anchorX: 0, anchorY: 100 },
        {
          shapeType: 'rectangle',
          width: 300,
          height: 80,
          cornerRadius: 12,
          fillColor: 'rgba(255, 255, 255, 0.1)',
        }
      ),
      createShapeLayer(
        'gradient-accent',
        'Gradient Accent',
        { x: 10, y: 80, anchorX: 0, anchorY: 100 },
        {
          shapeType: 'rectangle',
          width: 4,
          height: 80,
          cornerRadius: 2,
          fillGradient: {
            type: 'linear',
            angle: 180,
            stops: [
              { offset: 0, color: '#8B5CF6' },
              { offset: 1, color: '#EC4899' },
            ],
          },
        }
      ),
      createTextLayer(
        'name',
        'Name',
        { x: 12, y: 85, anchorX: 0, anchorY: 100 },
        {
          content: 'Sarah Johnson',
          fontSize: 24,
          fontWeight: 700,
          color: '#ffffff',
        }
      ),
      createTextLayer(
        'title',
        'Title',
        { x: 12, y: 90, anchorX: 0, anchorY: 100 },
        {
          content: 'Product Designer',
          fontSize: 14,
          fontWeight: 400,
          color: 'rgba(255, 255, 255, 0.7)',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'name',
          label: 'Name',
          type: 'text',
          defaultValue: 'Sarah Johnson',
          category: 'text',
          maxLength: 50,
        },
        {
          id: 'title',
          label: 'Title',
          type: 'text',
          defaultValue: 'Product Designer',
          category: 'text',
          maxLength: 60,
        },
        {
          id: 'gradientStart',
          label: 'Gradient Start',
          type: 'color',
          defaultValue: '#8B5CF6',
          category: 'colors',
        },
        {
          id: 'gradientEnd',
          label: 'Gradient End',
          type: 'color',
          defaultValue: '#EC4899',
          category: 'colors',
        },
      ],
    },
    defaultValues: {
      name: 'Sarah Johnson',
      title: 'Product Designer',
      gradientStart: '#8B5CF6',
      gradientEnd: '#EC4899',
    },
    colorSchemes: ['dark'],
  },
  {
    id: 'lower-third-broadcast-news',
    name: 'Broadcast News',
    description:
      'Professional broadcast-style lower third with animated ticker. Perfect for news, sports, and live events.',
    category: 'lower-thirds',
    tags: ['broadcast', 'news', 'sports', 'live', 'professional', 'ticker'],
    duration: 6,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createShapeLayer(
        'main-bar',
        'Main Bar',
        { x: 0, y: 88, anchorX: 0, anchorY: 100 },
        {
          shapeType: 'rectangle',
          width: 100,
          height: 12,
          fillColor: '#1E3A8A',
          cornerRadius: 0,
        }
      ),
      createShapeLayer(
        'accent-bar',
        'Accent Bar',
        { x: 0, y: 85, anchorX: 0, anchorY: 100 },
        {
          shapeType: 'rectangle',
          width: 35,
          height: 15,
          fillColor: '#DC2626',
          cornerRadius: 0,
        }
      ),
      createTextLayer(
        'name',
        'Name',
        { x: 2, y: 87, anchorX: 0, anchorY: 100 },
        {
          content: 'BREAKING NEWS',
          fontSize: 18,
          fontWeight: 800,
          color: '#ffffff',
          textTransform: 'uppercase',
        }
      ),
      createTextLayer(
        'headline',
        'Headline',
        { x: 37, y: 92, anchorX: 0, anchorY: 100 },
        {
          content: 'Major story developing as reports come in from the scene',
          fontSize: 14,
          fontWeight: 600,
          color: '#ffffff',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'label',
          label: 'Label',
          type: 'text',
          defaultValue: 'BREAKING NEWS',
          category: 'text',
          maxLength: 30,
        },
        {
          id: 'headline',
          label: 'Headline',
          type: 'text',
          defaultValue: 'Major story developing',
          category: 'text',
          maxLength: 100,
        },
        {
          id: 'labelColor',
          label: 'Label Background',
          type: 'color',
          defaultValue: '#DC2626',
          category: 'colors',
        },
        {
          id: 'barColor',
          label: 'Bar Color',
          type: 'color',
          defaultValue: '#1E3A8A',
          category: 'colors',
        },
      ],
    },
    defaultValues: {
      label: 'BREAKING NEWS',
      headline: 'Major story developing as reports come in from the scene',
      labelColor: '#DC2626',
      barColor: '#1E3A8A',
    },
    colorSchemes: ['dark'],
  },
  {
    id: 'lower-third-cinematic-elegant',
    name: 'Cinematic Elegant',
    description:
      'Film-inspired lower third with sophisticated serif typography and delicate underline animation.',
    category: 'lower-thirds',
    tags: ['cinematic', 'film', 'elegant', 'serif', 'luxury', 'wedding'],
    duration: 5,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createTextLayer(
        'name',
        'Name',
        { x: 10, y: 85, anchorX: 0, anchorY: 100 },
        {
          content: 'Elizabeth Montgomery',
          fontSize: 32,
          fontWeight: 400,
          fontFamily: 'Georgia, Times New Roman, serif',
          color: '#ffffff',
          letterSpacing: 2,
        }
      ),
      createTextLayer(
        'title',
        'Title',
        { x: 10, y: 91, anchorX: 0, anchorY: 100 },
        {
          content: 'Executive Producer',
          fontSize: 14,
          fontWeight: 400,
          fontFamily: 'Georgia, Times New Roman, serif',
          color: '#a1a1aa',
          letterSpacing: 4,
          textTransform: 'uppercase',
        }
      ),
      createShapeLayer(
        'underline',
        'Underline',
        { x: 10, y: 87, anchorX: 0, anchorY: 100 },
        {
          shapeType: 'rectangle',
          width: 60,
          height: 1,
          fillColor: '#D4AF37',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'name',
          label: 'Name',
          type: 'text',
          defaultValue: 'Elizabeth Montgomery',
          category: 'text',
          maxLength: 40,
        },
        {
          id: 'title',
          label: 'Title',
          type: 'text',
          defaultValue: 'Executive Producer',
          category: 'text',
          maxLength: 50,
        },
        {
          id: 'accentColor',
          label: 'Accent Color',
          type: 'color',
          defaultValue: '#D4AF37',
          category: 'colors',
        },
      ],
    },
    defaultValues: {
      name: 'Elizabeth Montgomery',
      title: 'Executive Producer',
      accentColor: '#D4AF37',
    },
    colorSchemes: ['dark', 'light'],
  },
  {
    id: 'lower-third-tech-startup',
    name: 'Tech Startup',
    description:
      'Dynamic lower third with animated gradient border. Perfect for tech presentations and product demos.',
    category: 'lower-thirds',
    tags: ['tech', 'startup', 'modern', 'gradient', 'product', 'demo'],
    duration: 5,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createShapeLayer(
        'border-glow',
        'Border Glow',
        { x: 10, y: 80, anchorX: 0, anchorY: 100 },
        {
          shapeType: 'rectangle',
          width: 280,
          height: 70,
          cornerRadius: 8,
          strokeColor: '#10B981',
          strokeWidth: 2,
          fillColor: 'rgba(16, 185, 129, 0.1)',
        }
      ),
      createTextLayer(
        'name',
        'Name',
        { x: 12, y: 85, anchorX: 0, anchorY: 100 },
        {
          content: 'Alex Chen',
          fontSize: 22,
          fontWeight: 700,
          color: '#ffffff',
        }
      ),
      createTextLayer(
        'title',
        'Title',
        { x: 12, y: 90, anchorX: 0, anchorY: 100 },
        {
          content: 'CTO & Co-Founder',
          fontSize: 13,
          fontWeight: 500,
          color: '#10B981',
        }
      ),
      createTextLayer(
        'company',
        'Company',
        { x: 12, y: 94, anchorX: 0, anchorY: 100 },
        {
          content: 'TechCorp Inc.',
          fontSize: 11,
          fontWeight: 400,
          color: '#6B7280',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'name',
          label: 'Name',
          type: 'text',
          defaultValue: 'Alex Chen',
          category: 'text',
          maxLength: 40,
        },
        {
          id: 'title',
          label: 'Title',
          type: 'text',
          defaultValue: 'CTO & Co-Founder',
          category: 'text',
          maxLength: 50,
        },
        {
          id: 'company',
          label: 'Company',
          type: 'text',
          defaultValue: 'TechCorp Inc.',
          category: 'text',
          maxLength: 40,
        },
        {
          id: 'accentColor',
          label: 'Accent Color',
          type: 'color',
          defaultValue: '#10B981',
          category: 'colors',
        },
      ],
    },
    defaultValues: {
      name: 'Alex Chen',
      title: 'CTO & Co-Founder',
      company: 'TechCorp Inc.',
      accentColor: '#10B981',
    },
    colorSchemes: ['dark'],
  },

  // ============ CALLOUTS ============
  {
    id: 'callout-focus-circle',
    name: 'Focus Circle',
    description:
      'Animated circular highlight with expanding ring and pulse effect for drawing attention.',
    category: 'callouts',
    tags: ['circle', 'highlight', 'focus', 'attention', 'pulse', 'tutorial'],
    duration: 4,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createShapeLayer(
        'outer-ring',
        'Outer Ring',
        { x: 50, y: 50 },
        {
          shapeType: 'circle',
          radius: 40,
          strokeColor: '#3B82F6',
          strokeWidth: 3,
          fillColor: 'transparent',
        }
      ),
      createShapeLayer(
        'inner-ring',
        'Inner Ring',
        { x: 50, y: 50 },
        {
          shapeType: 'circle',
          radius: 25,
          strokeColor: '#3B82F6',
          strokeWidth: 2,
          fillColor: 'rgba(59, 130, 246, 0.2)',
        }
      ),
      createTextLayer(
        'label',
        'Label',
        { x: 50, y: 65 },
        {
          content: 'Click Here',
          fontSize: 14,
          fontWeight: 600,
          color: '#3B82F6',
          textAlign: 'center',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'label',
          label: 'Label Text',
          type: 'text',
          defaultValue: 'Click Here',
          category: 'text',
          maxLength: 30,
        },
        { id: 'color', label: 'Color', type: 'color', defaultValue: '#3B82F6', category: 'colors' },
        {
          id: 'showLabel',
          label: 'Show Label',
          type: 'toggle',
          defaultValue: true,
          category: 'layout',
        },
      ],
    },
    defaultValues: {
      label: 'Click Here',
      color: '#3B82F6',
      showLabel: true,
    },
  },
  {
    id: 'callout-arrow-pointer',
    name: 'Arrow Pointer',
    description:
      'Sleek animated arrow with optional text label. Perfect for tutorials and walkthroughs.',
    category: 'callouts',
    tags: ['arrow', 'pointer', 'tutorial', 'guide', 'direction'],
    duration: 4,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createShapeLayer(
        'arrow',
        'Arrow',
        { x: 50, y: 50, rotation: -45 },
        {
          shapeType: 'path',
          pathData: 'M0,0 L20,10 L0,20 L5,10 Z',
          fillColor: '#EF4444',
        }
      ),
      createShapeLayer(
        'line',
        'Line',
        { x: 50, y: 50 },
        {
          shapeType: 'rectangle',
          width: 60,
          height: 3,
          cornerRadius: 2,
          fillColor: '#EF4444',
        }
      ),
      createTextLayer(
        'label',
        'Label',
        { x: 50, y: 60 },
        {
          content: 'Important',
          fontSize: 14,
          fontWeight: 600,
          color: '#EF4444',
          textAlign: 'center',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'label',
          label: 'Label Text',
          type: 'text',
          defaultValue: 'Important',
          category: 'text',
          maxLength: 30,
        },
        { id: 'color', label: 'Color', type: 'color', defaultValue: '#EF4444', category: 'colors' },
        {
          id: 'direction',
          label: 'Direction',
          type: 'select',
          defaultValue: 'right',
          category: 'layout',
          options: [
            { value: 'left', label: 'Left' },
            { value: 'right', label: 'Right' },
            { value: 'up', label: 'Up' },
            { value: 'down', label: 'Down' },
          ],
        },
      ],
    },
    defaultValues: {
      label: 'Important',
      color: '#EF4444',
      direction: 'right',
    },
  },
  {
    id: 'callout-tooltip-box',
    name: 'Tooltip Box',
    description: 'Elegant tooltip-style callout with pointer tail and customizable content.',
    category: 'callouts',
    tags: ['tooltip', 'box', 'info', 'hint', 'help'],
    duration: 4,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createShapeLayer(
        'box',
        'Box',
        { x: 50, y: 50 },
        {
          shapeType: 'rectangle',
          width: 200,
          height: 60,
          cornerRadius: 8,
          fillColor: '#1F2937',
          strokeColor: '#374151',
          strokeWidth: 1,
        }
      ),
      createShapeLayer(
        'pointer',
        'Pointer',
        { x: 50, y: 70 },
        {
          shapeType: 'polygon',
          fillColor: '#1F2937',
          points: [
            { x: -8, y: 0 },
            { x: 8, y: 0 },
            { x: 0, y: 10 },
          ],
        }
      ),
      createTextLayer(
        'text',
        'Text',
        { x: 50, y: 50 },
        {
          content: 'Helpful tip goes here',
          fontSize: 14,
          fontWeight: 400,
          color: '#ffffff',
          textAlign: 'center',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'text',
          label: 'Tooltip Text',
          type: 'text',
          defaultValue: 'Helpful tip goes here',
          category: 'text',
          maxLength: 100,
        },
        {
          id: 'bgColor',
          label: 'Background',
          type: 'color',
          defaultValue: '#1F2937',
          category: 'colors',
        },
        {
          id: 'textColor',
          label: 'Text Color',
          type: 'color',
          defaultValue: '#ffffff',
          category: 'colors',
        },
      ],
    },
    defaultValues: {
      text: 'Helpful tip goes here',
      bgColor: '#1F2937',
      textColor: '#ffffff',
    },
  },

  // ============ SOCIAL MEDIA ============
  {
    id: 'social-subscribe-button',
    name: 'Subscribe Button',
    description: 'Eye-catching subscribe button with bell notification and click animation.',
    category: 'social',
    tags: ['subscribe', 'youtube', 'button', 'bell', 'notification'],
    duration: 3,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createShapeLayer(
        'button',
        'Button',
        { x: 50, y: 50 },
        {
          shapeType: 'rectangle',
          width: 140,
          height: 45,
          cornerRadius: 4,
          fillColor: '#EF4444',
        }
      ),
      createTextLayer(
        'text',
        'Text',
        { x: 50, y: 50 },
        {
          content: 'SUBSCRIBE',
          fontSize: 16,
          fontWeight: 700,
          color: '#ffffff',
          textAlign: 'center',
          letterSpacing: 1,
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'text',
          label: 'Button Text',
          type: 'text',
          defaultValue: 'SUBSCRIBE',
          category: 'text',
          maxLength: 20,
        },
        {
          id: 'buttonColor',
          label: 'Button Color',
          type: 'color',
          defaultValue: '#EF4444',
          category: 'colors',
        },
        {
          id: 'showBell',
          label: 'Show Bell Icon',
          type: 'toggle',
          defaultValue: true,
          category: 'layout',
        },
      ],
    },
    defaultValues: {
      text: 'SUBSCRIBE',
      buttonColor: '#EF4444',
      showBell: true,
    },
  },
  {
    id: 'social-like-heart',
    name: 'Like Heart',
    description: 'Animated heart with particle burst effect for engagement moments.',
    category: 'social',
    tags: ['like', 'heart', 'love', 'engagement', 'particles'],
    duration: 2,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createShapeLayer(
        'heart',
        'Heart',
        { x: 50, y: 50 },
        {
          shapeType: 'path',
          pathData:
            'M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z',
          fillColor: '#EF4444',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'color',
          label: 'Heart Color',
          type: 'color',
          defaultValue: '#EF4444',
          category: 'colors',
        },
        {
          id: 'showParticles',
          label: 'Show Particles',
          type: 'toggle',
          defaultValue: true,
          category: 'animation',
        },
      ],
    },
    defaultValues: {
      color: '#EF4444',
      showParticles: true,
    },
  },
  {
    id: 'social-follow-badge',
    name: 'Follow Me Badge',
    description: 'Stylish badge supporting multiple platforms with animated entry.',
    category: 'social',
    tags: ['follow', 'badge', 'social', 'instagram', 'twitter', 'tiktok'],
    duration: 4,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createShapeLayer(
        'badge',
        'Badge',
        { x: 50, y: 50 },
        {
          shapeType: 'rectangle',
          width: 180,
          height: 50,
          cornerRadius: 25,
          fillColor: '#1DA1F2',
        }
      ),
      createTextLayer(
        'handle',
        'Handle',
        { x: 50, y: 50 },
        {
          content: '@username',
          fontSize: 18,
          fontWeight: 600,
          color: '#ffffff',
          textAlign: 'center',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'handle',
          label: 'Username/Handle',
          type: 'text',
          defaultValue: '@username',
          category: 'text',
          maxLength: 30,
        },
        {
          id: 'platform',
          label: 'Platform',
          type: 'select',
          defaultValue: 'twitter',
          category: 'layout',
          options: [
            { value: 'twitter', label: 'Twitter / X' },
            { value: 'instagram', label: 'Instagram' },
            { value: 'tiktok', label: 'TikTok' },
            { value: 'youtube', label: 'YouTube' },
            { value: 'linkedin', label: 'LinkedIn' },
          ],
        },
        {
          id: 'badgeColor',
          label: 'Badge Color',
          type: 'color',
          defaultValue: '#1DA1F2',
          category: 'colors',
        },
      ],
    },
    defaultValues: {
      handle: '@username',
      platform: 'twitter',
      badgeColor: '#1DA1F2',
    },
  },

  // ============ ANIMATED TITLES ============
  {
    id: 'title-cinematic-reveal',
    name: 'Cinematic Reveal',
    description:
      'Dramatic title reveal with lens flare and light leak effects. Perfect for trailers and intros.',
    category: 'titles',
    tags: ['cinematic', 'reveal', 'epic', 'trailer', 'intro', 'dramatic'],
    duration: 4,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createTextLayer(
        'main',
        'Main Title',
        { x: 50, y: 50 },
        {
          content: 'EPIC TITLE',
          fontSize: 72,
          fontWeight: 800,
          color: '#ffffff',
          textAlign: 'center',
          letterSpacing: 8,
          textTransform: 'uppercase',
        }
      ),
      createTextLayer(
        'subtitle',
        'Subtitle',
        { x: 50, y: 60 },
        {
          content: 'A Compelling Subtitle',
          fontSize: 18,
          fontWeight: 400,
          color: '#a1a1aa',
          textAlign: 'center',
          letterSpacing: 4,
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'title',
          label: 'Main Title',
          type: 'text',
          defaultValue: 'EPIC TITLE',
          category: 'text',
          maxLength: 30,
        },
        {
          id: 'subtitle',
          label: 'Subtitle',
          type: 'text',
          defaultValue: 'A Compelling Subtitle',
          category: 'text',
          maxLength: 50,
        },
        {
          id: 'titleColor',
          label: 'Title Color',
          type: 'color',
          defaultValue: '#ffffff',
          category: 'colors',
        },
        {
          id: 'showFlare',
          label: 'Show Lens Flare',
          type: 'toggle',
          defaultValue: true,
          category: 'animation',
        },
      ],
    },
    defaultValues: {
      title: 'EPIC TITLE',
      subtitle: 'A Compelling Subtitle',
      titleColor: '#ffffff',
      showFlare: true,
    },
  },
  {
    id: 'title-glitch-distort',
    name: 'Glitch Distort',
    description:
      'Edgy title with RGB split and digital distortion effects. Great for tech and gaming content.',
    category: 'titles',
    tags: ['glitch', 'tech', 'gaming', 'digital', 'distortion', 'cyber'],
    duration: 3,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createTextLayer(
        'main',
        'Main Title',
        { x: 50, y: 50 },
        {
          content: 'GLITCH',
          fontSize: 80,
          fontWeight: 900,
          color: '#ffffff',
          textAlign: 'center',
          letterSpacing: 4,
          textTransform: 'uppercase',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'title',
          label: 'Title Text',
          type: 'text',
          defaultValue: 'GLITCH',
          category: 'text',
          maxLength: 20,
        },
        {
          id: 'color',
          label: 'Text Color',
          type: 'color',
          defaultValue: '#ffffff',
          category: 'colors',
        },
        {
          id: 'glitchIntensity',
          label: 'Glitch Intensity',
          type: 'slider',
          defaultValue: 50,
          category: 'animation',
          min: 0,
          max: 100,
        },
      ],
    },
    defaultValues: {
      title: 'GLITCH',
      color: '#ffffff',
      glitchIntensity: 50,
    },
  },
  {
    id: 'title-elegant-serif',
    name: 'Elegant Serif',
    description:
      'Sophisticated title with graceful fade and particle effects. Ideal for weddings and luxury brands.',
    category: 'titles',
    tags: ['elegant', 'serif', 'wedding', 'luxury', 'particles', 'romantic'],
    duration: 5,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createTextLayer(
        'main',
        'Main Title',
        { x: 50, y: 45 },
        {
          content: 'Beautiful Moments',
          fontSize: 56,
          fontWeight: 400,
          fontFamily: 'Georgia, Times New Roman, serif',
          color: '#ffffff',
          textAlign: 'center',
          letterSpacing: 2,
        }
      ),
      createTextLayer(
        'date',
        'Date',
        { x: 50, y: 55 },
        {
          content: 'December 14, 2024',
          fontSize: 20,
          fontWeight: 300,
          fontFamily: 'Georgia, Times New Roman, serif',
          color: '#D4AF37',
          textAlign: 'center',
          letterSpacing: 4,
        }
      ),
      createShapeLayer(
        'line-left',
        'Line Left',
        { x: 30, y: 50 },
        {
          shapeType: 'rectangle',
          width: 80,
          height: 1,
          fillColor: '#D4AF37',
        }
      ),
      createShapeLayer(
        'line-right',
        'Line Right',
        { x: 70, y: 50 },
        {
          shapeType: 'rectangle',
          width: 80,
          height: 1,
          fillColor: '#D4AF37',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'title',
          label: 'Main Title',
          type: 'text',
          defaultValue: 'Beautiful Moments',
          category: 'text',
          maxLength: 40,
        },
        {
          id: 'date',
          label: 'Date/Subtitle',
          type: 'text',
          defaultValue: 'December 14, 2024',
          category: 'text',
          maxLength: 30,
        },
        {
          id: 'accentColor',
          label: 'Accent Color',
          type: 'color',
          defaultValue: '#D4AF37',
          category: 'colors',
        },
        {
          id: 'showParticles',
          label: 'Show Particles',
          type: 'toggle',
          defaultValue: true,
          category: 'animation',
        },
      ],
    },
    defaultValues: {
      title: 'Beautiful Moments',
      date: 'December 14, 2024',
      accentColor: '#D4AF37',
      showParticles: true,
    },
  },

  // ============ SHAPES ============
  {
    id: 'shape-line-reveal',
    name: 'Line Reveal',
    description: 'Animated line drawing with customizable direction and timing.',
    category: 'shapes',
    tags: ['line', 'reveal', 'draw', 'animation', 'minimal'],
    duration: 2,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createShapeLayer(
        'line',
        'Line',
        { x: 50, y: 50 },
        {
          shapeType: 'rectangle',
          width: 200,
          height: 3,
          cornerRadius: 2,
          fillColor: '#3B82F6',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'color',
          label: 'Line Color',
          type: 'color',
          defaultValue: '#3B82F6',
          category: 'colors',
        },
        {
          id: 'thickness',
          label: 'Thickness',
          type: 'slider',
          defaultValue: 3,
          category: 'layout',
          min: 1,
          max: 10,
        },
        {
          id: 'direction',
          label: 'Direction',
          type: 'select',
          defaultValue: 'horizontal',
          category: 'layout',
          options: [
            { value: 'horizontal', label: 'Horizontal' },
            { value: 'vertical', label: 'Vertical' },
            { value: 'diagonal', label: 'Diagonal' },
          ],
        },
      ],
    },
    defaultValues: {
      color: '#3B82F6',
      thickness: 3,
      direction: 'horizontal',
    },
  },
  {
    id: 'shape-circle-burst',
    name: 'Circle Burst',
    description: 'Expanding circles with staggered animation for dynamic emphasis.',
    category: 'shapes',
    tags: ['circle', 'burst', 'expand', 'emphasis', 'dynamic'],
    duration: 2,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createShapeLayer(
        'circle-1',
        'Circle 1',
        { x: 50, y: 50 },
        {
          shapeType: 'circle',
          radius: 30,
          strokeColor: '#8B5CF6',
          strokeWidth: 2,
          fillColor: 'transparent',
        }
      ),
      createShapeLayer(
        'circle-2',
        'Circle 2',
        { x: 50, y: 50 },
        {
          shapeType: 'circle',
          radius: 50,
          strokeColor: '#8B5CF6',
          strokeWidth: 2,
          fillColor: 'transparent',
        }
      ),
      createShapeLayer(
        'circle-3',
        'Circle 3',
        { x: 50, y: 50 },
        {
          shapeType: 'circle',
          radius: 70,
          strokeColor: '#8B5CF6',
          strokeWidth: 2,
          fillColor: 'transparent',
        }
      ),
    ],
    customization: {
      fields: [
        { id: 'color', label: 'Color', type: 'color', defaultValue: '#8B5CF6', category: 'colors' },
        {
          id: 'rings',
          label: 'Number of Rings',
          type: 'slider',
          defaultValue: 3,
          category: 'layout',
          min: 1,
          max: 5,
        },
      ],
    },
    defaultValues: {
      color: '#8B5CF6',
      rings: 3,
    },
  },
  {
    id: 'shape-geometric-pattern',
    name: 'Geometric Pattern',
    description: 'Animated geometric shapes with loop options for backgrounds and transitions.',
    category: 'shapes',
    tags: ['geometric', 'pattern', 'loop', 'background', 'abstract'],
    duration: 3,
    isPremium: false,
    version: '1.0.0',
    layers: [
      createShapeLayer(
        'triangle',
        'Triangle',
        { x: 30, y: 40 },
        {
          shapeType: 'polygon',
          fillColor: '#EC4899',
          points: [
            { x: 0, y: 20 },
            { x: 20, y: 20 },
            { x: 10, y: 0 },
          ],
        }
      ),
      createShapeLayer(
        'square',
        'Square',
        { x: 50, y: 50, rotation: 45 },
        {
          shapeType: 'rectangle',
          width: 30,
          height: 30,
          fillColor: '#3B82F6',
        }
      ),
      createShapeLayer(
        'circle',
        'Circle',
        { x: 70, y: 60 },
        {
          shapeType: 'circle',
          radius: 15,
          fillColor: '#10B981',
        }
      ),
    ],
    customization: {
      fields: [
        {
          id: 'color1',
          label: 'Color 1',
          type: 'color',
          defaultValue: '#EC4899',
          category: 'colors',
        },
        {
          id: 'color2',
          label: 'Color 2',
          type: 'color',
          defaultValue: '#3B82F6',
          category: 'colors',
        },
        {
          id: 'color3',
          label: 'Color 3',
          type: 'color',
          defaultValue: '#10B981',
          category: 'colors',
        },
        {
          id: 'loop',
          label: 'Loop Animation',
          type: 'toggle',
          defaultValue: true,
          category: 'animation',
        },
      ],
    },
    defaultValues: {
      color1: '#EC4899',
      color2: '#3B82F6',
      color3: '#10B981',
      loop: true,
    },
  },
];

/**
 * Motion graphics store type
 */
export type MotionGraphicsStore = MotionGraphicsState & MotionGraphicsActions;

/**
 * Create the motion graphics store
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
    const graphic: AppliedGraphic = {
      id,
      assetId,
      trackId,
      startTime,
      duration: asset.duration,
      customValues: { ...asset.defaultValues },
      locked: false,
      name: asset.name,
    };

    set((state) => ({
      applied: [...state.applied, graphic],
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
        g.id === graphicId ? { ...g, customValues: { ...g.customValues, [key]: value } } : g
      ),
    }));
  },

  duplicateGraphic: (graphicId) => {
    const graphic = get().applied.find((g) => g.id === graphicId);
    if (!graphic) return null;

    const id = generateId();
    const newGraphic: AppliedGraphic = {
      ...graphic,
      id,
      startTime: graphic.startTime + graphic.duration,
      name: `${graphic.name || 'Graphic'} (copy)`,
    };

    set((state) => ({
      applied: [...state.applied, newGraphic],
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
        a.tags.some((t) => t.toLowerCase().includes(lowerQuery))
    );
  },
}));

export default useMotionGraphicsStore;
