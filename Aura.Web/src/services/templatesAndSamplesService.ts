/**
 * Service for managing video templates, sample projects, and example prompts
 *
 * This service provides users with starter templates and examples during
 * the first-run wizard to help them get started quickly.
 */

export interface VideoTemplate {
  id: string;
  name: string;
  description: string;
  category: TemplateCategory;
  thumbnail?: string;
  duration: number; // in seconds
  difficulty: 'beginner' | 'intermediate' | 'advanced';
  promptExample: string;
  tags: string[];
  estimatedTime: string; // e.g., "2-3 minutes"
}

export type TemplateCategory =
  | 'youtube'
  | 'social-media'
  | 'educational'
  | 'marketing'
  | 'tutorial'
  | 'storytelling'
  | 'news'
  | 'entertainment';

export interface SampleProject {
  id: string;
  name: string;
  description: string;
  template: VideoTemplate;
  prompt: string;
  expectedOutput: string;
  learningPoints: string[];
}

export interface ExamplePrompt {
  id: string;
  title: string;
  prompt: string;
  category: TemplateCategory;
  tags: string[];
  expectedDuration: number;
  tips: string[];
}

/**
 * Get all available video templates
 */
export function getVideoTemplates(): VideoTemplate[] {
  return [
    {
      id: 'youtube-tutorial',
      name: 'YouTube Tutorial',
      description:
        'Create educational tutorial videos for YouTube with clear structure and engaging visuals',
      category: 'tutorial',
      duration: 180, // 3 minutes
      difficulty: 'beginner',
      promptExample:
        'Create a tutorial video about how to make homemade pasta, including ingredients, step-by-step instructions, and cooking tips',
      tags: ['youtube', 'education', 'tutorial', 'how-to'],
      estimatedTime: '2-3 minutes',
    },
    {
      id: 'social-shorts',
      name: 'Social Media Shorts',
      description:
        'Quick, engaging short-form content perfect for TikTok, Instagram Reels, and YouTube Shorts',
      category: 'social-media',
      duration: 60, // 1 minute
      difficulty: 'beginner',
      promptExample: 'Create a 60-second video about 5 productivity hacks for remote workers',
      tags: ['social', 'shorts', 'tiktok', 'reels', 'quick'],
      estimatedTime: '1-2 minutes',
    },
    {
      id: 'product-demo',
      name: 'Product Demonstration',
      description: 'Showcase product features and benefits with professional marketing style',
      category: 'marketing',
      duration: 120, // 2 minutes
      difficulty: 'intermediate',
      promptExample:
        'Create a product demo video for a new smart home device, highlighting its key features and ease of use',
      tags: ['marketing', 'product', 'demo', 'commercial'],
      estimatedTime: '3-4 minutes',
    },
    {
      id: 'educational-explainer',
      name: 'Educational Explainer',
      description: 'Explain complex topics in simple, visual terms',
      category: 'educational',
      duration: 240, // 4 minutes
      difficulty: 'intermediate',
      promptExample:
        'Explain how photosynthesis works in plants, using simple analogies and visual examples',
      tags: ['education', 'explainer', 'science', 'learning'],
      estimatedTime: '3-5 minutes',
    },
    {
      id: 'news-summary',
      name: 'News Summary',
      description: 'Quick news roundup or topic summary in journalistic style',
      category: 'news',
      duration: 150, // 2.5 minutes
      difficulty: 'intermediate',
      promptExample:
        'Create a news summary video about recent developments in renewable energy technology',
      tags: ['news', 'current-events', 'journalism', 'informative'],
      estimatedTime: '2-3 minutes',
    },
    {
      id: 'story-narrative',
      name: 'Story & Narrative',
      description: 'Tell compelling stories with narrative structure and emotional impact',
      category: 'storytelling',
      duration: 300, // 5 minutes
      difficulty: 'advanced',
      promptExample:
        'Tell the inspiring story of how a small startup became a successful company through innovation and perseverance',
      tags: ['storytelling', 'narrative', 'emotional', 'journey'],
      estimatedTime: '5-7 minutes',
    },
    {
      id: 'list-compilation',
      name: 'Top 10 List',
      description: 'Countdown or list-based content that keeps viewers engaged',
      category: 'entertainment',
      duration: 180, // 3 minutes
      difficulty: 'beginner',
      promptExample: 'Create a video listing the top 10 hidden features in popular smartphones',
      tags: ['list', 'top-10', 'countdown', 'entertainment'],
      estimatedTime: '2-3 minutes',
    },
    {
      id: 'comparison-video',
      name: 'Comparison & Review',
      description: 'Compare two or more options to help viewers make informed decisions',
      category: 'educational',
      duration: 210, // 3.5 minutes
      difficulty: 'intermediate',
      promptExample:
        'Compare electric cars vs hybrid cars, covering cost, performance, and environmental impact',
      tags: ['comparison', 'review', 'vs', 'analysis'],
      estimatedTime: '3-4 minutes',
    },
  ];
}

/**
 * Get sample projects to help users get started
 */
export function getSampleProjects(): SampleProject[] {
  const templates = getVideoTemplates();

  return [
    {
      id: 'sample-coffee-tutorial',
      name: 'How to Make Perfect Coffee',
      description: 'A beginner-friendly tutorial demonstrating the sample project workflow',
      template: templates.find((t) => t.id === 'youtube-tutorial')!,
      prompt:
        'Create a 3-minute tutorial video about how to make the perfect cup of coffee at home. Include equipment needed, step-by-step brewing instructions, and tips for choosing beans.',
      expectedOutput:
        '3-minute educational video with introduction, main content sections, and conclusion',
      learningPoints: [
        'How to write effective prompts',
        'Understanding video structure',
        'Working with tutorial format',
        'Basic editing and pacing',
      ],
    },
    {
      id: 'sample-productivity-short',
      name: '5 Morning Routine Hacks',
      description: 'Quick social media content example',
      template: templates.find((t) => t.id === 'social-shorts')!,
      prompt:
        'Create a 60-second video about 5 morning routine hacks to start your day productively',
      expectedOutput: 'Fast-paced 1-minute video with quick transitions between tips',
      learningPoints: [
        'Creating engaging short-form content',
        'Pacing for social media',
        'Hook and retention strategies',
        'Quick tips format',
      ],
    },
    {
      id: 'sample-space-explainer',
      name: 'Why Is the Sky Blue?',
      description: 'Educational explainer video sample',
      template: templates.find((t) => t.id === 'educational-explainer')!,
      prompt:
        'Explain why the sky appears blue during the day in a way that a 10-year-old could understand. Use simple analogies and avoid complex scientific jargon.',
      expectedOutput: '2-3 minute explainer with clear visual descriptions and simple language',
      learningPoints: [
        'Simplifying complex topics',
        'Using analogies effectively',
        'Educational content structure',
        'Balancing information and engagement',
      ],
    },
  ];
}

/**
 * Get example prompts categorized by use case
 */
export function getExamplePrompts(): ExamplePrompt[] {
  return [
    {
      id: 'prompt-cooking-basic',
      title: 'Basic Cooking Tutorial',
      prompt:
        'Create a video showing how to make scrambled eggs, including ingredient prep, cooking technique, and plating tips',
      category: 'tutorial',
      tags: ['cooking', 'food', 'beginner', 'how-to'],
      expectedDuration: 120,
      tips: [
        'Be specific about ingredients and measurements',
        'Break down steps clearly',
        'Include safety tips where relevant',
      ],
    },
    {
      id: 'prompt-tech-review',
      title: 'Tech Product Review',
      prompt:
        'Review the latest smartphone, covering design, performance, camera quality, battery life, and value for money',
      category: 'educational',
      tags: ['tech', 'review', 'gadgets', 'analysis'],
      expectedDuration: 240,
      tips: [
        'Structure reviews with clear sections',
        'Be objective and balanced',
        'Include pros and cons',
        'Mention target audience',
      ],
    },
    {
      id: 'prompt-travel-vlog',
      title: 'Travel Destination Guide',
      prompt:
        'Create a travel guide video for Paris, highlighting top attractions, local food, transportation tips, and budget recommendations',
      category: 'entertainment',
      tags: ['travel', 'guide', 'tourism', 'lifestyle'],
      expectedDuration: 300,
      tips: [
        'Include practical information',
        'Mention best times to visit',
        'Add local cultural insights',
        'Consider different budget levels',
      ],
    },
    {
      id: 'prompt-science-explainer',
      title: 'Science Concept Explanation',
      prompt:
        'Explain how solar panels convert sunlight into electricity, using simple language and everyday analogies',
      category: 'educational',
      tags: ['science', 'technology', 'energy', 'explainer'],
      expectedDuration: 180,
      tips: [
        'Start with the basics',
        'Use relatable comparisons',
        'Avoid jargon or explain technical terms',
        'Visual descriptions help understanding',
      ],
    },
    {
      id: 'prompt-business-tips',
      title: 'Business Advice Video',
      prompt:
        'Share 7 essential tips for entrepreneurs starting their first business, covering planning, funding, marketing, and growth',
      category: 'educational',
      tags: ['business', 'entrepreneurship', 'tips', 'advice'],
      expectedDuration: 240,
      tips: [
        'Number your tips clearly',
        'Provide actionable advice',
        'Include real-world examples',
        'Balance optimism with realism',
      ],
    },
    {
      id: 'prompt-fitness-routine',
      title: 'Fitness Workout Guide',
      prompt:
        'Create a 15-minute home workout routine for beginners, with no equipment needed. Include warmup, exercises, and cooldown',
      category: 'tutorial',
      tags: ['fitness', 'health', 'workout', 'exercise'],
      expectedDuration: 180,
      tips: [
        'Emphasize proper form',
        'Include modifications for different levels',
        'Add safety warnings',
        'Mention rest periods',
      ],
    },
    {
      id: 'prompt-history-lesson',
      title: 'Historical Event Summary',
      prompt: 'Summarize the key events and significance of the moon landing in 1969',
      category: 'educational',
      tags: ['history', 'space', 'education', 'documentary'],
      expectedDuration: 240,
      tips: [
        'Provide context and background',
        'Highlight key moments',
        'Explain historical impact',
        'Keep it engaging, not just facts',
      ],
    },
    {
      id: 'prompt-art-tutorial',
      title: 'Art Technique Tutorial',
      prompt:
        'Teach beginners how to draw a realistic eye, step by step, with shading and detail techniques',
      category: 'tutorial',
      tags: ['art', 'drawing', 'creative', 'how-to'],
      expectedDuration: 300,
      tips: [
        'Break down into simple shapes first',
        'Explain each technique clearly',
        'Show common mistakes to avoid',
        'Encourage practice',
      ],
    },
  ];
}

/**
 * Get templates by category
 */
export function getTemplatesByCategory(category: TemplateCategory): VideoTemplate[] {
  return getVideoTemplates().filter((t) => t.category === category);
}

/**
 * Get templates by difficulty
 */
export function getTemplatesByDifficulty(
  difficulty: 'beginner' | 'intermediate' | 'advanced'
): VideoTemplate[] {
  return getVideoTemplates().filter((t) => t.difficulty === difficulty);
}

/**
 * Get recommended templates for first-time users
 */
export function getBeginnerTemplates(): VideoTemplate[] {
  return getVideoTemplates()
    .filter((t) => t.difficulty === 'beginner')
    .slice(0, 3);
}

/**
 * Search templates by tags or keywords
 */
export function searchTemplates(query: string): VideoTemplate[] {
  const lowerQuery = query.toLowerCase();
  return getVideoTemplates().filter(
    (t) =>
      t.name.toLowerCase().includes(lowerQuery) ||
      t.description.toLowerCase().includes(lowerQuery) ||
      t.tags.some((tag) => tag.toLowerCase().includes(lowerQuery))
  );
}

/**
 * Create a project from a template
 */
export interface CreateProjectFromTemplateRequest {
  templateId: string;
  customPrompt?: string;
  projectName?: string;
}

export async function createProjectFromTemplate(
  request: CreateProjectFromTemplateRequest
): Promise<{ success: boolean; projectId?: string; message: string }> {
  try {
    const response = await fetch('/api/templates/create-project', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(`Failed to create project: ${response.statusText}`);
    }

    const data = await response.json();
    return {
      success: true,
      projectId: data.projectId,
      message: 'Project created successfully from template',
    };
  } catch (error) {
    return {
      success: false,
      message: error instanceof Error ? error.message : 'Failed to create project',
    };
  }
}

/**
 * Get tutorial videos/guides
 */
export interface TutorialGuide {
  id: string;
  title: string;
  description: string;
  videoUrl?: string;
  steps: TutorialStep[];
  estimatedTime: string;
  difficulty: 'beginner' | 'intermediate' | 'advanced';
}

export interface TutorialStep {
  title: string;
  description: string;
  imageUrl?: string;
  duration?: string;
}

export function getTutorialGuides(): TutorialGuide[] {
  return [
    {
      id: 'getting-started',
      title: 'Getting Started with Aura',
      description: 'Learn the basics of creating your first video with Aura Video Studio',
      estimatedTime: '10 minutes',
      difficulty: 'beginner',
      steps: [
        {
          title: 'Write Your Prompt',
          description:
            'Start by describing the video you want to create. Be specific about the topic, style, and duration.',
          duration: '2 min',
        },
        {
          title: 'Choose Your Settings',
          description: 'Select video resolution, aspect ratio, and any advanced options you need.',
          duration: '1 min',
        },
        {
          title: 'Generate Script',
          description:
            'Aura will generate a script based on your prompt. Review and edit as needed.',
          duration: '3 min',
        },
        {
          title: 'Review & Render',
          description: 'Preview the timeline, make adjustments, then render your final video.',
          duration: '4 min',
        },
      ],
    },
    {
      id: 'advanced-prompting',
      title: 'Advanced Prompting Techniques',
      description: 'Master the art of writing effective prompts to get better results',
      estimatedTime: '15 minutes',
      difficulty: 'intermediate',
      steps: [
        {
          title: 'Be Specific',
          description:
            'Include details about tone, pace, target audience, and key points to cover.',
          duration: '3 min',
        },
        {
          title: 'Structure Your Request',
          description: 'Break down your video into sections (intro, main points, conclusion).',
          duration: '4 min',
        },
        {
          title: 'Use Examples',
          description: 'Reference styles or examples of videos similar to what you want.',
          duration: '4 min',
        },
        {
          title: 'Iterate and Refine',
          description: 'Learn how to adjust your prompt based on the generated results.',
          duration: '4 min',
        },
      ],
    },
    {
      id: 'customization',
      title: 'Customizing Your Videos',
      description: 'Learn how to customize generated videos with your own branding and style',
      estimatedTime: '20 minutes',
      difficulty: 'advanced',
      steps: [
        {
          title: 'Add Custom Assets',
          description:
            'Import your own images, videos, and audio files to personalize your content.',
          duration: '5 min',
        },
        {
          title: 'Adjust Timing',
          description: 'Fine-tune clip durations and transitions in the timeline editor.',
          duration: '5 min',
        },
        {
          title: 'Apply Branding',
          description: 'Add logos, custom colors, and consistent visual elements.',
          duration: '5 min',
        },
        {
          title: 'Export Settings',
          description: 'Choose the right export settings for your platform and audience.',
          duration: '5 min',
        },
      ],
    },
  ];
}
