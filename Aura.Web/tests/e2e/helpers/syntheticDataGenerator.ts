/**
 * Synthetic test data generator for E2E tests
 * Generates realistic test data for briefs, scripts, and edge cases
 */

export interface SyntheticBrief {
  topic: string;
  audience: string;
  goal: string;
  tone: string;
  language?: string;
  duration?: number;
}

export interface SyntheticScript {
  lines: Array<{
    text: string;
    duration: number;
    scene?: string;
  }>;
  totalDuration: number;
  sceneCount: number;
}

export interface SyntheticJobConfig {
  jobId: string;
  brief: SyntheticBrief;
  script?: SyntheticScript;
  providers: {
    script: string;
    tts: string;
    visuals: string;
  };
  expectedDuration: number;
}

const TOPICS = [
  'Getting Started with AI Video Creation',
  'Best Practices for Content Marketing',
  'Introduction to Machine Learning',
  'Digital Photography Fundamentals',
  'Healthy Cooking Tips and Tricks',
  'Time Management for Professionals',
  'Understanding Climate Change',
  'Modern Web Development',
  'Financial Planning Basics',
  'Effective Communication Skills',
];

const AUDIENCES = [
  'Beginners',
  'Professionals',
  'Students',
  'General Public',
  'Technical Users',
  'Business Owners',
  'Hobbyists',
  'Educators',
  'Researchers',
  'Content Creators',
];

const GOALS = [
  'Tutorial',
  'Explainer',
  'Showcase',
  'Demonstration',
  'Education',
  'Marketing',
  'Training',
  'Introduction',
  'Overview',
  'Guide',
];

const TONES = [
  'Friendly',
  'Professional',
  'Casual',
  'Formal',
  'Enthusiastic',
  'Informative',
  'Conversational',
  'Technical',
  'Inspiring',
  'Educational',
];

const SCENE_DESCRIPTIONS = [
  'Opening scene with title card',
  'Main content introduction',
  'Detailed explanation with visuals',
  'Step-by-step demonstration',
  'Key points summary',
  'Real-world examples',
  'Best practices overview',
  'Common mistakes to avoid',
  'Tips and recommendations',
  'Conclusion and call-to-action',
];

/**
 * Generate a random synthetic brief
 */
export function generateSyntheticBrief(seed?: number): SyntheticBrief {
  const random = seed !== undefined ? seededRandom(seed) : Math.random;

  return {
    topic: TOPICS[Math.floor(random() * TOPICS.length)],
    audience: AUDIENCES[Math.floor(random() * AUDIENCES.length)],
    goal: GOALS[Math.floor(random() * GOALS.length)],
    tone: TONES[Math.floor(random() * TONES.length)],
    language: 'English',
    duration: 15 + Math.floor(random() * 46),
  };
}

/**
 * Generate a synthetic script from a brief
 */
export function generateSyntheticScript(brief: SyntheticBrief, seed?: number): SyntheticScript {
  const random = seed !== undefined ? seededRandom(seed) : Math.random;
  const targetDuration = brief.duration || 30;
  const sceneCount = Math.max(3, Math.floor(targetDuration / 10));

  const lines: SyntheticScript['lines'] = [];
  let totalDuration = 0;

  for (let i = 0; i < sceneCount; i++) {
    const sceneDuration = targetDuration / sceneCount;
    const text = generateSceneText(brief, i, sceneCount, seed);

    lines.push({
      text,
      duration: sceneDuration,
      scene: SCENE_DESCRIPTIONS[i % SCENE_DESCRIPTIONS.length],
    });

    totalDuration += sceneDuration;
  }

  return {
    lines,
    totalDuration,
    sceneCount,
  };
}

/**
 * Generate realistic scene text
 */
function generateSceneText(
  brief: SyntheticBrief,
  sceneIndex: number,
  totalScenes: number,
  seed?: number
): string {
  const random = seed !== undefined ? seededRandom(seed + sceneIndex) : Math.random;

  if (sceneIndex === 0) {
    return `Welcome to this ${brief.tone.toLowerCase()} guide about ${brief.topic.toLowerCase()}. This is designed for ${brief.audience.toLowerCase()}.`;
  } else if (sceneIndex === totalScenes - 1) {
    return `Thank you for watching this ${brief.goal.toLowerCase()} on ${brief.topic.toLowerCase()}. We hope you found it helpful.`;
  } else {
    const templates = [
      `In this section, we'll explore ${brief.topic.toLowerCase()} in detail.`,
      `Let's dive deeper into the key concepts of ${brief.topic.toLowerCase()}.`,
      `Here are some important points about ${brief.topic.toLowerCase()} that you should know.`,
      `Now, let's look at practical applications of ${brief.topic.toLowerCase()}.`,
      `Understanding ${brief.topic.toLowerCase()} requires attention to these fundamentals.`,
    ];

    return templates[Math.floor(random() * templates.length)];
  }
}

/**
 * Generate complete job configuration with synthetic data
 */
export function generateSyntheticJobConfig(seed?: number): SyntheticJobConfig {
  const random = seed !== undefined ? seededRandom(seed) : Math.random;
  const brief = generateSyntheticBrief(seed);
  const script = generateSyntheticScript(brief, seed);

  const providers = [
    { script: 'RuleBased', tts: 'WindowsTTS', visuals: 'Stock' },
    { script: 'Ollama', tts: 'Piper', visuals: 'StableDiffusion' },
    { script: 'OpenAI', tts: 'ElevenLabs', visuals: 'DALLE' },
  ];

  const selectedProvider = providers[Math.floor(random() * providers.length)];

  return {
    jobId: `synthetic-${Date.now()}-${Math.floor(random() * 10000)}`,
    brief,
    script,
    providers: selectedProvider,
    expectedDuration: script.totalDuration,
  };
}

/**
 * Generate edge case scenarios for testing
 */
export function generateEdgeCaseScenarios(): SyntheticJobConfig[] {
  return [
    {
      jobId: 'edge-very-short',
      brief: {
        topic: 'Quick Tip',
        audience: 'Everyone',
        goal: 'Tutorial',
        tone: 'Casual',
        duration: 5,
      },
      providers: {
        script: 'RuleBased',
        tts: 'WindowsTTS',
        visuals: 'Stock',
      },
      expectedDuration: 5,
    },
    {
      jobId: 'edge-very-long',
      brief: {
        topic: 'Comprehensive Course',
        audience: 'Advanced Learners',
        goal: 'Training',
        tone: 'Professional',
        duration: 300,
      },
      providers: {
        script: 'OpenAI',
        tts: 'ElevenLabs',
        visuals: 'StableDiffusion',
      },
      expectedDuration: 300,
    },
    {
      jobId: 'edge-special-chars',
      brief: {
        topic: 'C++ & Python: A Comparison!',
        audience: 'Developers (All Levels)',
        goal: 'Explainer',
        tone: 'Technical',
        duration: 30,
      },
      providers: {
        script: 'RuleBased',
        tts: 'WindowsTTS',
        visuals: 'Stock',
      },
      expectedDuration: 30,
    },
    {
      jobId: 'edge-unicode',
      brief: {
        topic: '日本語でビデオを作成',
        audience: '日本人ユーザー',
        goal: 'チュートリアル',
        tone: 'フレンドリー',
        language: 'Japanese',
        duration: 30,
      },
      providers: {
        script: 'RuleBased',
        tts: 'WindowsTTS',
        visuals: 'Stock',
      },
      expectedDuration: 30,
    },
    {
      jobId: 'edge-empty-topic',
      brief: {
        topic: '',
        audience: 'General',
        goal: 'Test',
        tone: 'Neutral',
        duration: 10,
      },
      providers: {
        script: 'RuleBased',
        tts: 'WindowsTTS',
        visuals: 'Stock',
      },
      expectedDuration: 10,
    },
    {
      jobId: 'edge-max-length-topic',
      brief: {
        topic: 'A'.repeat(500),
        audience: 'Test Audience',
        goal: 'Test Goal',
        tone: 'Test Tone',
        duration: 30,
      },
      providers: {
        script: 'RuleBased',
        tts: 'WindowsTTS',
        visuals: 'Stock',
      },
      expectedDuration: 30,
    },
  ];
}

/**
 * Generate batch of synthetic jobs for concurrent testing
 */
export function generateBatchJobs(count: number, seed?: number): SyntheticJobConfig[] {
  const jobs: SyntheticJobConfig[] = [];

  for (let i = 0; i < count; i++) {
    const jobSeed = seed !== undefined ? seed + i : undefined;
    jobs.push(generateSyntheticJobConfig(jobSeed));
  }

  return jobs;
}

/**
 * Seeded random number generator for reproducible tests
 */
function seededRandom(seed: number): () => number {
  let state = seed;

  return () => {
    state = (state * 9301 + 49297) % 233280;
    return state / 233280;
  };
}

/**
 * Generate realistic progress updates for a job
 */
export function generateProgressUpdates(
  jobId: string,
  phases: string[]
): Array<{
  event: string;
  data: unknown;
}> {
  const updates: Array<{ event: string; data: unknown }> = [];
  const progressPerPhase = 100 / phases.length;

  phases.forEach((phase, index) => {
    const baseProgress = index * progressPerPhase;

    updates.push({
      event: 'job-status',
      data: {
        id: jobId,
        status: 'Running',
        stage: phase,
        progress: Math.floor(baseProgress),
      },
    });

    for (let i = 0; i < 3; i++) {
      updates.push({
        event: 'step-progress',
        data: {
          step: phase.toLowerCase(),
          progress: Math.floor(baseProgress + (i + 1) * (progressPerPhase / 4)),
          message: `Processing ${phase.toLowerCase()} - step ${i + 1}`,
        },
      });
    }
  });

  updates.push({
    event: 'job-completed',
    data: {
      id: jobId,
      status: 'Done',
      progress: 100,
    },
  });

  return updates;
}

/**
 * Generate mock SSE event stream
 */
export function generateSSEEventStream(updates: Array<{ event: string; data: unknown }>): string {
  return updates
    .map((update, index) => {
      const dataStr = typeof update.data === 'string' ? update.data : JSON.stringify(update.data);
      return `id: event-${index}\nevent: ${update.event}\ndata: ${dataStr}\n\n`;
    })
    .join('');
}
