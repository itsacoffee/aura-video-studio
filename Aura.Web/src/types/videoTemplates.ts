/**
 * TypeScript types for Video Structure Templates (Script-to-Video Templates)
 * These templates define pre-built video formats like Explainer, Listicle, Comparison, etc.
 */

/**
 * Represents a pre-built video structure template.
 */
export interface VideoTemplate {
  id: string;
  name: string;
  description: string;
  category: string;
  structure: TemplateStructure;
  variables: TemplateVariable[];
  thumbnail: TemplateThumbnail | null;
  metadata: TemplateMetadata;
}

/**
 * Defines the structure of a video template with sections and timing.
 */
export interface TemplateStructure {
  sections: TemplateSection[];
  estimatedDurationSeconds: number;
  recommendedSceneCount: number;
}

/**
 * Represents a section within a video template.
 */
export interface TemplateSection {
  name: string;
  purpose: string;
  type: SectionType;
  suggestedDurationSeconds: number;
  promptTemplate: string;
  isOptional: boolean;
  exampleContent: string[] | null;
  isRepeatable: boolean;
  repeatCountVariable: string | null;
}

/**
 * Types of sections that can appear in a video template.
 */
export type SectionType =
  | 'Hook'
  | 'Introduction'
  | 'MainPoint'
  | 'Transition'
  | 'Example'
  | 'CallToAction'
  | 'Conclusion'
  | 'NumberedItem'
  | 'Comparison'
  | 'Problem'
  | 'Solution'
  | 'Testimonial'
  | 'Setup'
  | 'RisingAction'
  | 'Climax'
  | 'Resolution'
  | 'Lesson'
  | 'Overview'
  | 'Prerequisites'
  | 'Step'
  | 'CommonMistakes'
  | 'Summary'
  | 'Attention'
  | 'Interest'
  | 'Desire'
  | 'Action'
  | 'Recap'
  | 'Verdict'
  | 'OptionA'
  | 'OptionB';

/**
 * Represents a variable that users can customize in a template.
 */
export interface TemplateVariable {
  name: string;
  displayName: string;
  type: VariableType;
  defaultValue: string | null;
  placeholder: string | null;
  isRequired: boolean;
  options: string[] | null;
  minValue: number | null;
  maxValue: number | null;
}

/**
 * Types of variables supported in templates.
 */
export type VariableType = 'Text' | 'Number' | 'Selection' | 'MultiSelection' | 'LongText';

/**
 * Thumbnail configuration for template display.
 */
export interface TemplateThumbnail {
  iconName: string;
  accentColor: string;
}

/**
 * Metadata about a template for filtering and recommendations.
 */
export interface TemplateMetadata {
  recommendedAudiences: string[];
  recommendedTones: string[];
  supportedAspects: string[];
  minDurationSeconds: number;
  maxDurationSeconds: number;
  tags: string[];
}

/**
 * Request to apply a template with variable values.
 */
export interface ApplyTemplateRequest {
  variableValues: Record<string, string>;
  language?: string;
  aspect?: string;
  pacing?: string;
  density?: string;
}

/**
 * Result of applying a template with variable values.
 */
export interface TemplatedBrief {
  brief: TemplateBriefResult;
  planSpec: TemplatePlanSpecResult;
  sections: GeneratedSection[];
  sourceTemplateId: string;
}

/**
 * Brief from templated brief result.
 */
export interface TemplateBriefResult {
  topic: string;
  audience: string | null;
  goal: string | null;
  tone: string;
  language: string;
  aspect: string;
}

/**
 * PlanSpec from templated brief result.
 */
export interface TemplatePlanSpecResult {
  targetDurationSeconds: number;
  pacing: string;
  density: string;
  style: string;
  targetSceneCount: number | null;
}

/**
 * A generated section with content from template application.
 */
export interface GeneratedSection {
  name: string;
  content: string;
  suggestedDurationSeconds: number;
  type: string;
}

/**
 * Response containing a preview of the generated script.
 */
export interface ScriptPreviewResponse {
  script: string;
  sections: GeneratedSection[];
  estimatedDurationSeconds: number;
  sceneCount: number;
}

/**
 * Result of variable validation.
 */
export interface ValidationResult {
  isValid: boolean;
  errors: string[];
}

/**
 * Template category for filtering.
 */
export type VideoTemplateCategory = 'Educational' | 'Entertainment' | 'Reviews' | 'Marketing';

/**
 * Icon mapping for template thumbnails.
 */
export const TemplateIconMap: Record<string, string> = {
  Lightbulb: '💡',
  NumberList: '📝',
  ScaleBalance: '⚖️',
  BookOpen: '📖',
  GraduationCap: '🎓',
  ShoppingBag: '🛍️',
};

/**
 * Helper to format duration from seconds.
 */
export function formatDuration(seconds: number): string {
  if (seconds < 60) {
    return `${seconds}s`;
  }
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  if (remainingSeconds === 0) {
    return `${minutes}min`;
  }
  return `${minutes}min ${remainingSeconds}s`;
}

/**
 * Helper to get category color.
 */
export function getCategoryColor(category: string): string {
  switch (category) {
    case 'Educational':
      return '#4CAF50';
    case 'Entertainment':
      return '#FF9800';
    case 'Reviews':
      return '#2196F3';
    case 'Marketing':
      return '#E91E63';
    default:
      return '#9E9E9E';
  }
}
