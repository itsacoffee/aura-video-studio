/**
 * Fallback language list for offline/error scenarios
 * This provides a basic set of languages when the API is unavailable
 */

import type { LanguageInfoDto } from '../types/api-v1';

/**
 * Core languages that are always available as a fallback
 * Covers the most commonly used languages worldwide
 */
export const fallbackLanguages: LanguageInfoDto[] = [
  // English variants
  {
    code: 'en',
    name: 'English',
    nativeName: 'English',
    region: 'Global',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.0,
  },
  {
    code: 'en-US',
    name: 'English (US)',
    nativeName: 'English (United States)',
    region: 'North America',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.0,
  },
  {
    code: 'en-GB',
    name: 'English (UK)',
    nativeName: 'English (United Kingdom)',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.0,
  },

  // Spanish variants
  {
    code: 'es',
    name: 'Spanish',
    nativeName: 'Español',
    region: 'Global',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.15,
  },
  {
    code: 'es-ES',
    name: 'Spanish (Spain)',
    nativeName: 'Español (España)',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 1.15,
  },
  {
    code: 'es-MX',
    name: 'Spanish (Mexico)',
    nativeName: 'Español (México)',
    region: 'Latin America',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.15,
  },

  // French variants
  {
    code: 'fr',
    name: 'French',
    nativeName: 'Français',
    region: 'Global',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 1.15,
  },
  {
    code: 'fr-FR',
    name: 'French (France)',
    nativeName: 'Français (France)',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 1.15,
  },
  {
    code: 'fr-CA',
    name: 'French (Canada)',
    nativeName: 'Français (Canada)',
    region: 'North America',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.15,
  },

  // Portuguese variants
  {
    code: 'pt',
    name: 'Portuguese',
    nativeName: 'Português',
    region: 'Global',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.15,
  },
  {
    code: 'pt-BR',
    name: 'Portuguese (Brazil)',
    nativeName: 'Português (Brasil)',
    region: 'Latin America',
    isRightToLeft: false,
    defaultFormality: 'Informal',
    typicalExpansionFactor: 1.15,
  },
  {
    code: 'pt-PT',
    name: 'Portuguese (Portugal)',
    nativeName: 'Português (Portugal)',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 1.15,
  },

  // German
  {
    code: 'de',
    name: 'German',
    nativeName: 'Deutsch',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 1.3,
  },
  {
    code: 'de-DE',
    name: 'German (Germany)',
    nativeName: 'Deutsch (Deutschland)',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 1.3,
  },

  // Italian
  {
    code: 'it',
    name: 'Italian',
    nativeName: 'Italiano',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.2,
  },

  // Dutch
  {
    code: 'nl',
    name: 'Dutch',
    nativeName: 'Nederlands',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.1,
  },

  // Russian
  {
    code: 'ru',
    name: 'Russian',
    nativeName: 'Русский',
    region: 'Eastern Europe',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 1.15,
  },

  // Polish
  {
    code: 'pl',
    name: 'Polish',
    nativeName: 'Polski',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 1.2,
  },

  // Chinese
  {
    code: 'zh',
    name: 'Chinese (Simplified)',
    nativeName: '简体中文',
    region: 'Asia',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 0.7,
  },
  {
    code: 'zh-CN',
    name: 'Chinese (Mainland)',
    nativeName: '简体中文 (中国)',
    region: 'Asia',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 0.7,
  },
  {
    code: 'zh-TW',
    name: 'Chinese (Traditional)',
    nativeName: '繁體中文 (台灣)',
    region: 'Asia',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 0.7,
  },

  // Japanese
  {
    code: 'ja',
    name: 'Japanese',
    nativeName: '日本語',
    region: 'Asia',
    isRightToLeft: false,
    defaultFormality: 'VeryFormal',
    typicalExpansionFactor: 0.8,
  },

  // Korean
  {
    code: 'ko',
    name: 'Korean',
    nativeName: '한국어',
    region: 'Asia',
    isRightToLeft: false,
    defaultFormality: 'VeryFormal',
    typicalExpansionFactor: 0.9,
  },

  // Arabic
  {
    code: 'ar',
    name: 'Arabic',
    nativeName: 'العربية',
    region: 'Middle East',
    isRightToLeft: true,
    defaultFormality: 'VeryFormal',
    typicalExpansionFactor: 1.25,
  },
  {
    code: 'ar-SA',
    name: 'Arabic (Saudi Arabia)',
    nativeName: 'العربية (السعودية)',
    region: 'Middle East',
    isRightToLeft: true,
    defaultFormality: 'VeryFormal',
    typicalExpansionFactor: 1.25,
  },

  // Hebrew
  {
    code: 'he',
    name: 'Hebrew',
    nativeName: 'עברית',
    region: 'Middle East',
    isRightToLeft: true,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.1,
  },

  // Hindi
  {
    code: 'hi',
    name: 'Hindi',
    nativeName: 'हिन्दी',
    region: 'Asia',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 1.15,
  },

  // Vietnamese
  {
    code: 'vi',
    name: 'Vietnamese',
    nativeName: 'Tiếng Việt',
    region: 'Asia',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 1.0,
  },

  // Thai
  {
    code: 'th',
    name: 'Thai',
    nativeName: 'ไทย',
    region: 'Asia',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 1.0,
  },

  // Turkish
  {
    code: 'tr',
    name: 'Turkish',
    nativeName: 'Türkçe',
    region: 'Middle East',
    isRightToLeft: false,
    defaultFormality: 'Formal',
    typicalExpansionFactor: 1.15,
  },

  // Indonesian
  {
    code: 'id',
    name: 'Indonesian',
    nativeName: 'Bahasa Indonesia',
    region: 'Asia',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.1,
  },

  // Swedish
  {
    code: 'sv',
    name: 'Swedish',
    nativeName: 'Svenska',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.1,
  },

  // Norwegian
  {
    code: 'no',
    name: 'Norwegian',
    nativeName: 'Norsk',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.1,
  },

  // Danish
  {
    code: 'da',
    name: 'Danish',
    nativeName: 'Dansk',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.1,
  },

  // Finnish
  {
    code: 'fi',
    name: 'Finnish',
    nativeName: 'Suomi',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.2,
  },

  // Greek
  {
    code: 'el',
    name: 'Greek',
    nativeName: 'Ελληνικά',
    region: 'Europe',
    isRightToLeft: false,
    defaultFormality: 'Neutral',
    typicalExpansionFactor: 1.2,
  },
];
