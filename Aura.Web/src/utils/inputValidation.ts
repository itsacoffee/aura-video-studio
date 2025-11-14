/**
 * Input Validation Utilities
 * Validates user input to prevent errors
 */

export interface ValidationResult {
  isValid: boolean;
  error?: string;
  warning?: string;
}

/**
 * Validates video title
 */
export function validateVideoTitle(title: string): ValidationResult {
  if (!title || title.trim().length === 0) {
    return {
      isValid: false,
      error: 'Title is required',
    };
  }

  if (title.length > 200) {
    return {
      isValid: false,
      error: 'Title must be 200 characters or less',
    };
  }

  // Check for invalid filename characters (for output file)
  // eslint-disable-next-line no-control-regex
  const invalidChars = /[<>:"/\\|?*\x00-\x1F]/;
  if (invalidChars.test(title)) {
    return {
      isValid: false,
      error: 'Title contains invalid characters (<>:"/\\|?*)',
    };
  }

  return { isValid: true };
}

/**
 * Validates video description/prompt
 */
export function validateVideoDescription(description: string, maxLength = 5000): ValidationResult {
  if (!description || description.trim().length === 0) {
    return {
      isValid: false,
      error: 'Description is required',
    };
  }

  if (description.length < 10) {
    return {
      isValid: false,
      error: 'Description must be at least 10 characters',
    };
  }

  if (description.length > maxLength) {
    return {
      isValid: false,
      error: `Description must be ${maxLength} characters or less`,
    };
  }

  return { isValid: true };
}

/**
 * Validates API key format
 */
export function validateApiKey(keyName: string, keyValue: string): ValidationResult {
  if (!keyValue || keyValue.trim().length === 0) {
    return {
      isValid: false,
      error: `${keyName} is required`,
    };
  }

  // Remove whitespace
  const trimmed = keyValue.trim();

  // Check for whitespace in middle
  if (trimmed !== keyValue) {
    return {
      isValid: false,
      error: 'API key should not have leading or trailing whitespace',
    };
  }

  if (keyValue.includes(' ') || keyValue.includes('\n') || keyValue.includes('\t')) {
    return {
      isValid: false,
      error: 'API key should not contain spaces or line breaks',
    };
  }

  // Minimum length check
  if (keyValue.length < 10) {
    return {
      isValid: false,
      error: 'API key appears too short (should be at least 10 characters)',
    };
  }

  // Check for "Bearer" prefix (common mistake)
  if (keyValue.toLowerCase().startsWith('bearer ')) {
    return {
      isValid: false,
      error: 'Remove "Bearer " prefix from API key',
    };
  }

  // Provider-specific validation
  if (keyName.toLowerCase().includes('openai') && !keyValue.startsWith('sk-')) {
    return {
      isValid: true,
      warning: 'OpenAI API keys typically start with "sk-". Are you sure this is correct?',
    };
  }

  if (keyName.toLowerCase().includes('anthropic') && !keyValue.startsWith('sk-ant-')) {
    return {
      isValid: true,
      warning: 'Anthropic API keys typically start with "sk-ant-". Are you sure this is correct?',
    };
  }

  return { isValid: true };
}

/**
 * Validates video duration
 */
export function validateDuration(duration: number): ValidationResult {
  if (duration <= 0) {
    return {
      isValid: false,
      error: 'Duration must be greater than 0',
    };
  }

  if (duration > 600) {
    // 10 minutes
    return {
      isValid: false,
      error: 'Duration must be 10 minutes (600 seconds) or less',
    };
  }

  if (duration > 180) {
    // Warn for long videos
    return {
      isValid: true,
      warning: 'Videos longer than 3 minutes may take significant time to generate',
    };
  }

  return { isValid: true };
}

/**
 * Validates file size
 */
export function validateFileSize(sizeBytes: number, maxSizeMB = 100): ValidationResult {
  const sizeMB = sizeBytes / (1024 * 1024);

  if (sizeMB > maxSizeMB) {
    return {
      isValid: false,
      error: `File size (${sizeMB.toFixed(1)}MB) exceeds maximum of ${maxSizeMB}MB`,
    };
  }

  if (sizeMB > maxSizeMB * 0.8) {
    // Warn at 80% of limit
    return {
      isValid: true,
      warning: `File size (${sizeMB.toFixed(1)}MB) is close to the limit of ${maxSizeMB}MB`,
    };
  }

  return { isValid: true };
}

/**
 * Validates image resolution
 */
export function validateImageResolution(width: number, height: number): ValidationResult {
  if (width <= 0 || height <= 0) {
    return {
      isValid: false,
      error: 'Width and height must be greater than 0',
    };
  }

  if (width > 4096 || height > 4096) {
    return {
      isValid: false,
      error: 'Resolution cannot exceed 4096x4096',
    };
  }

  // Check aspect ratio
  const aspectRatio = width / height;
  if (aspectRatio < 0.25 || aspectRatio > 4) {
    return {
      isValid: true,
      warning: 'Unusual aspect ratio detected. This may cause rendering issues.',
    };
  }

  // Warn for large resolutions
  const pixels = width * height;
  if (pixels > 2073600) {
    // > 1920x1080
    return {
      isValid: true,
      warning: 'High resolution may increase rendering time and memory usage',
    };
  }

  return { isValid: true };
}

/**
 * Validates URL
 */
export function validateUrl(url: string): ValidationResult {
  if (!url || url.trim().length === 0) {
    return {
      isValid: false,
      error: 'URL is required',
    };
  }

  try {
    const parsed = new URL(url);

    if (!['http:', 'https:'].includes(parsed.protocol)) {
      return {
        isValid: false,
        error: 'URL must use HTTP or HTTPS protocol',
      };
    }

    return { isValid: true };
  } catch {
    return {
      isValid: false,
      error: 'Invalid URL format',
    };
  }
}

/**
 * Validates email address
 */
export function validateEmail(email: string): ValidationResult {
  if (!email || email.trim().length === 0) {
    return {
      isValid: false,
      error: 'Email is required',
    };
  }

  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!emailRegex.test(email)) {
    return {
      isValid: false,
      error: 'Invalid email format',
    };
  }

  return { isValid: true };
}

/**
 * Validates numeric input within a range
 */
export function validateNumber(
  value: number,
  min?: number,
  max?: number,
  fieldName = 'Value'
): ValidationResult {
  if (isNaN(value)) {
    return {
      isValid: false,
      error: `${fieldName} must be a valid number`,
    };
  }

  if (min !== undefined && value < min) {
    return {
      isValid: false,
      error: `${fieldName} must be at least ${min}`,
    };
  }

  if (max !== undefined && value > max) {
    return {
      isValid: false,
      error: `${fieldName} must be at most ${max}`,
    };
  }

  return { isValid: true };
}

/**
 * Validates array has required number of items
 */
export function validateArrayLength<T>(
  array: T[],
  min?: number,
  max?: number,
  itemName = 'items'
): ValidationResult {
  if (min !== undefined && array.length < min) {
    return {
      isValid: false,
      error: `At least ${min} ${itemName} required (currently ${array.length})`,
    };
  }

  if (max !== undefined && array.length > max) {
    return {
      isValid: false,
      error: `At most ${max} ${itemName} allowed (currently ${array.length})`,
    };
  }

  return { isValid: true };
}

/**
 * Combines multiple validation results
 */
export function combineValidations(...results: ValidationResult[]): ValidationResult {
  const errors = results.filter((r) => !r.isValid).map((r) => r.error!);
  const warnings = results.filter((r) => r.isValid && r.warning).map((r) => r.warning!);

  if (errors.length > 0) {
    return {
      isValid: false,
      error: errors.join('; '),
    };
  }

  if (warnings.length > 0) {
    return {
      isValid: true,
      warning: warnings.join('; '),
    };
  }

  return { isValid: true };
}
