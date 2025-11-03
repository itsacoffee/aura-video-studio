/**
 * Default values for user preference objects
 */

import type { CustomAudienceProfile, ContentFilteringPolicy } from '../state/userPreferences';

/**
 * Creates a default custom audience profile with all required fields
 */
export function createDefaultProfile(
  name: string,
  description: string = '',
  minAge: number = 18,
  maxAge: number = 65,
  formalityLevel: number = 5,
  educationLevel: string = 'College'
): Omit<CustomAudienceProfile, 'id' | 'createdAt' | 'updatedAt'> {
  return {
    name,
    description,
    baseProfileId: undefined,
    isCustom: true,
    minAge,
    maxAge,
    educationLevel,
    educationLevelDescription: undefined,
    culturalSensitivities: [],
    topicsToAvoid: [],
    topicsToEmphasize: [],
    vocabularyLevel: 5,
    sentenceStructurePreference: 'Balanced',
    readingLevel: 10,
    violenceThreshold: 5,
    profanityThreshold: 5,
    sexualContentThreshold: 5,
    controversialTopicsThreshold: 5,
    humorStyle: 'Neutral',
    sarcasmLevel: 3,
    jokeTypes: [],
    culturalHumorPreferences: [],
    formalityLevel,
    attentionSpanSeconds: 120,
    pacingPreference: 'Medium',
    informationDensity: 5,
    technicalDepthTolerance: 5,
    jargonAcceptability: 5,
    familiarTechnicalTerms: [],
    emotionalTone: 'Neutral',
    emotionalIntensity: 5,
    ctaAggressiveness: 5,
    ctaStyle: 'Encouraging',
    brandVoiceGuidelines: undefined,
    brandToneKeywords: [],
    brandPersonality: undefined,
    tags: [],
    isFavorite: false,
    usageCount: 0,
    lastUsedAt: undefined,
  };
}

/**
 * Creates a default content filtering policy with all required fields
 */
export function createDefaultPolicy(
  name: string,
  description: string = '',
  filteringEnabled: boolean = true,
  profanityFilter: string = 'Moderate',
  violenceThreshold: number = 5,
  blockGraphicContent: boolean = false
): Omit<ContentFilteringPolicy, 'id' | 'createdAt' | 'updatedAt'> {
  return {
    name,
    description,
    filteringEnabled,
    allowOverrideAll: false,
    profanityFilter,
    customBannedWords: [],
    customAllowedWords: [],
    violenceThreshold,
    blockGraphicContent,
    sexualContentThreshold: 5,
    blockExplicitContent: false,
    bannedTopics: [],
    allowedControversialTopics: [],
    politicalContent: 'Allow',
    politicalContentGuidelines: undefined,
    religiousContent: 'Allow',
    religiousContentGuidelines: undefined,
    substanceReferences: 'Allow',
    blockHateSpeech: true,
    hateSpeechExceptions: [],
    copyrightPolicy: 'Strict',
    blockedConcepts: [],
    allowedConcepts: [],
    blockedPeople: [],
    allowedPeople: [],
    blockedBrands: [],
    allowedBrands: [],
    isDefault: false,
    usageCount: 0,
    lastUsedAt: undefined,
  };
}
