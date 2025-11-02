import { z } from 'zod';

/**
 * Zod schemas for validating API responses and providing defaults
 */

// Project schemas
export const ProjectListItemSchema = z.object({
  id: z.string(),
  name: z.string(),
  description: z.string().optional().nullable(),
  thumbnail: z.string().optional().nullable(),
  lastModifiedAt: z.string().or(z.date()),
  duration: z.number().default(0),
  clipCount: z.number().default(0),
});

export const ProjectListSchema = z.array(ProjectListItemSchema).default([]);

export type ValidatedProjectListItem = z.infer<typeof ProjectListItemSchema>;

// Asset schemas
export const AssetTagSchema = z.object({
  name: z.string(),
  confidence: z.number().optional(),
});

export const AssetMetadataSchema = z
  .object({
    width: z.number().optional(),
    height: z.number().optional(),
    duration: z.string().optional(),
    fileSizeBytes: z.number().optional(),
    format: z.string().optional(),
    codec: z.string().optional(),
    bitrate: z.number().optional(),
    sampleRate: z.number().optional(),
    extra: z.record(z.string()).optional(),
  })
  .default({});

export const AssetSchema = z.object({
  id: z.string(),
  title: z.string(),
  description: z.string().optional().nullable(),
  type: z.enum(['Image', 'Video', 'Audio']),
  source: z.string(),
  filePath: z.string(),
  thumbnailPath: z.string().optional().nullable(),
  tags: z.array(AssetTagSchema).default([]),
  metadata: AssetMetadataSchema,
  dateAdded: z.string(),
  dateModified: z.string(),
  usageCount: z.number().default(0),
  collections: z.array(z.string()).default([]),
  dominantColor: z.string().optional(),
});

export const AssetSearchResultSchema = z.object({
  assets: z.array(AssetSchema).default([]),
  totalCount: z.number().default(0),
  page: z.number().default(1),
  pageSize: z.number().default(50),
});

export type ValidatedAsset = z.infer<typeof AssetSchema>;
export type ValidatedAssetSearchResult = z.infer<typeof AssetSearchResultSchema>;

// Content Planning schemas
export const TrendDataPointSchema = z.object({
  timestamp: z.string(),
  value: z.number(),
  additionalData: z.record(z.unknown()).optional(),
});

export const TrendDataSchema = z.object({
  id: z.string(),
  topic: z.string(),
  category: z.string(),
  platform: z.string(),
  trendScore: z.number(),
  direction: z.enum(['Rising', 'Stable', 'Declining']),
  analyzedAt: z.string(),
  dataPoints: z.array(TrendDataPointSchema).default([]),
  metrics: z.record(z.unknown()).default({}),
});

export const PlatformTrendsResponseSchema = z.object({
  success: z.boolean().default(true),
  trends: z.array(TrendDataSchema).default([]),
  platform: z.string(),
  category: z.string().optional().nullable(),
});

export const TopicSuggestionSchema = z.object({
  id: z.string(),
  topic: z.string(),
  description: z.string(),
  category: z.string(),
  relevanceScore: z.number(),
  trendScore: z.number(),
  predictedEngagement: z.number(),
  keywords: z.array(z.string()).default([]),
  recommendedPlatforms: z.array(z.string()).default([]),
  generatedAt: z.string(),
  metadata: z.record(z.unknown()).default({}),
});

export const TopicSuggestionResponseSchema = z.object({
  suggestions: z.array(TopicSuggestionSchema).default([]),
  generatedAt: z.string(),
  totalCount: z.number().default(0),
});

export const DemographicsSchema = z.object({
  ageDistribution: z.record(z.number()).default({}),
  genderDistribution: z.record(z.number()).default({}),
  locationDistribution: z.record(z.number()).default({}),
});

export const AudienceInsightSchema = z.object({
  id: z.string(),
  platform: z.string(),
  demographics: DemographicsSchema,
  topInterests: z.array(z.string()).default([]),
  preferredContentTypes: z.array(z.string()).default([]),
  engagementRate: z.number().default(0),
  bestPostingTimes: z.record(z.number()).default({}),
  analyzedAt: z.string(),
});

export type ValidatedTrendData = z.infer<typeof TrendDataSchema>;
export type ValidatedTopicSuggestion = z.infer<typeof TopicSuggestionSchema>;
export type ValidatedAudienceInsight = z.infer<typeof AudienceInsightSchema>;

/**
 * Helper function to safely parse API responses with zod
 */
export function parseApiResponse<T>(
  schema: z.ZodSchema<T>,
  data: unknown,
  fallback?: T
): { success: true; data: T } | { success: false; error: Error } {
  try {
    const parsed = schema.parse(data);
    return { success: true, data: parsed };
  } catch (error) {
    if (error instanceof z.ZodError) {
      const errorMessage = `API response validation failed: ${error.errors.map((e) => `${e.path.join('.')}: ${e.message}`).join(', ')}`;

      // If fallback is provided, use it
      if (fallback !== undefined) {
        return { success: true, data: fallback };
      }

      return { success: false, error: new Error(errorMessage) };
    }
    return { success: false, error: error instanceof Error ? error : new Error(String(error)) };
  }
}

/**
 * Helper to safely parse with default fallback
 */
export function parseWithDefault<T>(schema: z.ZodSchema<T>, data: unknown): T {
  const result = schema.safeParse(data);
  if (result.success) {
    return result.data;
  }

  // Return the default from the schema if it has one
  const defaultResult = schema.safeParse(undefined);
  if (defaultResult.success) {
    return defaultResult.data;
  }

  throw new Error('Schema has no default and data is invalid');
}
