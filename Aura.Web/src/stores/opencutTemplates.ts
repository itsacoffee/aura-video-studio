/**
 * OpenCut Templates Store
 *
 * Manages video project templates including built-in templates and user-created templates.
 * Provides functionality for saving, loading, and sharing templates with pre-configured
 * tracks, clips, effects, and styling.
 */

import { create } from 'zustand';
import { persist } from 'zustand/middleware';

/** Track type in template */
export type TemplateTrackType = 'video' | 'audio' | 'image' | 'text';

/** Track configuration in a template */
export interface TemplateTrack {
  id: string;
  type: TemplateTrackType;
  name: string;
  order: number;
  height: number;
  muted: boolean;
  solo: boolean;
  locked: boolean;
  visible: boolean;
}

/** Clip placeholder in a template */
export interface TemplateClip {
  id: string;
  trackId: string;
  type: TemplateTrackType;
  name: string;
  startTime: number;
  duration: number;
  isPlaceholder: boolean;
}

/** Effect reference in a template */
export interface TemplateEffect {
  id: string;
  effectId: string;
  clipId: string;
  parameters: Record<string, string | number | boolean>;
}

/** Transition reference in a template */
export interface TemplateTransition {
  id: string;
  transitionId: string;
  fromClipId: string;
  toClipId: string;
  duration: number;
}

/** Marker in a template */
export interface TemplateMarker {
  id: string;
  time: number;
  type: 'standard' | 'chapter' | 'todo' | 'beat';
  color: string;
  name: string;
}

/** Complete template data structure */
export interface TemplateData {
  tracks: TemplateTrack[];
  clips: TemplateClip[];
  effects: TemplateEffect[];
  transitions: TemplateTransition[];
  markers: TemplateMarker[];
}

/** Template definition */
export interface Template {
  id: string;
  name: string;
  description: string;
  thumbnail: string;
  category: string;
  tags: string[];
  aspectRatio: string;
  duration: number;
  data: TemplateData;
  createdAt: string;
  updatedAt: string;
  isBuiltin?: boolean;
}

function generateId(): string {
  return `template-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/** Built-in templates */
export const BUILTIN_TEMPLATES: Template[] = [
  {
    id: 'social-vertical',
    name: 'Vertical Social Video',
    description: 'Perfect for mobile-first platforms. 9:16 aspect ratio with text-safe zones.',
    thumbnail: '/templates/vertical-social.png',
    category: 'Social Media',
    tags: ['vertical', 'mobile', 'short-form'],
    aspectRatio: '9:16',
    duration: 60,
    data: {
      tracks: [
        {
          id: 't1',
          type: 'video',
          name: 'Main Video',
          order: 0,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't2',
          type: 'audio',
          name: 'Music',
          order: 1,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't3',
          type: 'text',
          name: 'Captions',
          order: 2,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
      ],
      clips: [],
      effects: [],
      transitions: [],
      markers: [],
    },
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    isBuiltin: true,
  },
  {
    id: 'youtube-landscape',
    name: 'Landscape Video',
    description: 'Standard 16:9 format for desktop viewing. Includes intro and outro placeholders.',
    thumbnail: '/templates/youtube-landscape.png',
    category: 'Social Media',
    tags: ['landscape', 'desktop', 'long-form'],
    aspectRatio: '16:9',
    duration: 600,
    data: {
      tracks: [
        {
          id: 't1',
          type: 'video',
          name: 'B-Roll',
          order: 0,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't2',
          type: 'video',
          name: 'Main Video',
          order: 1,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't3',
          type: 'audio',
          name: 'Voiceover',
          order: 2,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't4',
          type: 'audio',
          name: 'Music',
          order: 3,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't5',
          type: 'text',
          name: 'Lower Thirds',
          order: 4,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
      ],
      clips: [],
      effects: [],
      transitions: [],
      markers: [
        { id: 'm1', time: 0, type: 'chapter', color: 'green', name: 'Intro' },
        { id: 'm2', time: 30, type: 'chapter', color: 'blue', name: 'Main Content' },
      ],
    },
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    isBuiltin: true,
  },
  {
    id: 'square-promo',
    name: 'Square Promo',
    description: 'Square format ideal for feed posts. Bold text overlays.',
    thumbnail: '/templates/square-promo.png',
    category: 'Social Media',
    tags: ['square', 'promo', 'feed'],
    aspectRatio: '1:1',
    duration: 30,
    data: {
      tracks: [
        {
          id: 't1',
          type: 'video',
          name: 'Background',
          order: 0,
          height: 56,
          muted: true,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't2',
          type: 'text',
          name: 'Headlines',
          order: 1,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't3',
          type: 'audio',
          name: 'Music',
          order: 2,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
      ],
      clips: [],
      effects: [],
      transitions: [],
      markers: [],
    },
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    isBuiltin: true,
  },
  {
    id: 'presentation',
    name: 'Presentation',
    description: 'Professional 16:9 layout for presentations and tutorials.',
    thumbnail: '/templates/presentation.png',
    category: 'Business',
    tags: ['presentation', 'tutorial', 'professional'],
    aspectRatio: '16:9',
    duration: 300,
    data: {
      tracks: [
        {
          id: 't1',
          type: 'video',
          name: 'Screen Recording',
          order: 0,
          height: 56,
          muted: true,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't2',
          type: 'video',
          name: 'Webcam',
          order: 1,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't3',
          type: 'audio',
          name: 'Voiceover',
          order: 2,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't4',
          type: 'text',
          name: 'Annotations',
          order: 3,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
      ],
      clips: [],
      effects: [],
      transitions: [],
      markers: [
        { id: 'm1', time: 0, type: 'chapter', color: 'blue', name: 'Introduction' },
        { id: 'm2', time: 60, type: 'chapter', color: 'green', name: 'Demo' },
        { id: 'm3', time: 240, type: 'chapter', color: 'purple', name: 'Summary' },
      ],
    },
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    isBuiltin: true,
  },
  {
    id: 'podcast-video',
    name: 'Video Podcast',
    description: 'Layout for video podcasts with multiple speakers.',
    thumbnail: '/templates/podcast-video.png',
    category: 'Entertainment',
    tags: ['podcast', 'interview', 'multi-camera'],
    aspectRatio: '16:9',
    duration: 1800,
    data: {
      tracks: [
        {
          id: 't1',
          type: 'video',
          name: 'Host',
          order: 0,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't2',
          type: 'video',
          name: 'Guest',
          order: 1,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't3',
          type: 'audio',
          name: 'Audio Mix',
          order: 2,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
        {
          id: 't4',
          type: 'text',
          name: 'Lower Thirds',
          order: 3,
          height: 56,
          muted: false,
          solo: false,
          locked: false,
          visible: true,
        },
      ],
      clips: [],
      effects: [],
      transitions: [],
      markers: [],
    },
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    isBuiltin: true,
  },
];

/** Templates store state */
interface TemplatesState {
  builtinTemplates: Template[];
  userTemplates: Template[];
  selectedTemplateId: string | null;
}

/** Templates store actions */
interface TemplatesActions {
  /** Create a new user template */
  createTemplate: (
    name: string,
    description: string,
    data: TemplateData,
    aspectRatio: string,
    category?: string,
    tags?: string[]
  ) => string;

  /** Update an existing user template */
  updateTemplate: (templateId: string, updates: Partial<Template>) => void;

  /** Delete a user template */
  deleteTemplate: (templateId: string) => void;

  /** Duplicate a template (creates a user copy) */
  duplicateTemplate: (templateId: string) => string;

  /** Get template data for applying to a project */
  applyTemplate: (templateId: string) => TemplateData | null;

  /** Select a template by ID */
  selectTemplate: (templateId: string | null) => void;

  /** Get a template by ID */
  getTemplate: (templateId: string) => Template | undefined;

  /** Get all templates in a category */
  getTemplatesByCategory: (category: string) => Template[];

  /** Search templates by name, description, or tags */
  searchTemplates: (query: string) => Template[];

  /** Get all unique categories */
  getCategories: () => string[];

  /** Export a template as JSON string */
  exportTemplate: (templateId: string) => string;

  /** Import a template from JSON string */
  importTemplate: (jsonString: string) => string | null;

  /** Get all templates (builtin + user) */
  getAllTemplates: () => Template[];
}

export type OpenCutTemplatesStore = TemplatesState & TemplatesActions;

export const useTemplatesStore = create<OpenCutTemplatesStore>()(
  persist(
    (set, get) => ({
      builtinTemplates: BUILTIN_TEMPLATES,
      userTemplates: [],
      selectedTemplateId: null,

      createTemplate: (name, description, data, aspectRatio, category = 'Custom', tags = []) => {
        const id = generateId();
        const now = new Date().toISOString();
        const template: Template = {
          id,
          name,
          description,
          thumbnail: '',
          category,
          tags,
          aspectRatio,
          duration: 0,
          data,
          createdAt: now,
          updatedAt: now,
          isBuiltin: false,
        };
        set((state) => ({ userTemplates: [...state.userTemplates, template] }));
        return id;
      },

      updateTemplate: (templateId, updates) => {
        set((state) => ({
          userTemplates: state.userTemplates.map((t) =>
            t.id === templateId ? { ...t, ...updates, updatedAt: new Date().toISOString() } : t
          ),
        }));
      },

      deleteTemplate: (templateId) => {
        set((state) => ({
          userTemplates: state.userTemplates.filter((t) => t.id !== templateId),
          selectedTemplateId:
            state.selectedTemplateId === templateId ? null : state.selectedTemplateId,
        }));
      },

      duplicateTemplate: (templateId) => {
        const template = get().getTemplate(templateId);
        if (!template) return '';

        return get().createTemplate(
          `${template.name} (Copy)`,
          template.description,
          JSON.parse(JSON.stringify(template.data)) as TemplateData,
          template.aspectRatio,
          'Custom',
          [...template.tags]
        );
      },

      applyTemplate: (templateId) => {
        const template = get().getTemplate(templateId);
        return template ? (JSON.parse(JSON.stringify(template.data)) as TemplateData) : null;
      },

      selectTemplate: (templateId) => set({ selectedTemplateId: templateId }),

      getTemplate: (templateId) => {
        const all = [...get().builtinTemplates, ...get().userTemplates];
        return all.find((t) => t.id === templateId);
      },

      getTemplatesByCategory: (category) => {
        const all = [...get().builtinTemplates, ...get().userTemplates];
        return all.filter((t) => t.category === category);
      },

      searchTemplates: (query) => {
        const lower = query.toLowerCase();
        const all = [...get().builtinTemplates, ...get().userTemplates];
        return all.filter(
          (t) =>
            t.name.toLowerCase().includes(lower) ||
            t.description.toLowerCase().includes(lower) ||
            t.tags.some((tag) => tag.toLowerCase().includes(lower))
        );
      },

      getCategories: () => {
        const all = [...get().builtinTemplates, ...get().userTemplates];
        const categories = new Set(all.map((t) => t.category));
        return Array.from(categories).sort();
      },

      exportTemplate: (templateId) => {
        const template = get().getTemplate(templateId);
        return template ? JSON.stringify(template, null, 2) : '';
      },

      importTemplate: (jsonString) => {
        try {
          const template = JSON.parse(jsonString) as Template;
          const id = generateId();
          const now = new Date().toISOString();
          const imported: Template = {
            ...template,
            id,
            category: 'Imported',
            createdAt: now,
            updatedAt: now,
            isBuiltin: false,
          };
          set((state) => ({ userTemplates: [...state.userTemplates, imported] }));
          return id;
        } catch {
          return null;
        }
      },

      getAllTemplates: () => {
        return [...get().builtinTemplates, ...get().userTemplates];
      },
    }),
    {
      name: 'opencut-templates',
      partialize: (state) => ({
        userTemplates: state.userTemplates,
        selectedTemplateId: state.selectedTemplateId,
      }),
    }
  )
);
