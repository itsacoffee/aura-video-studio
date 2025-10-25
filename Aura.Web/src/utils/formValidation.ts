/**
 * Form validation utilities using Zod
 */

import { z } from 'zod';

/**
 * Common validation schemas
 */
export const validationSchemas = {
  email: z.string().email('Invalid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  url: z.string().url('Invalid URL'),
  positiveNumber: z.number().positive('Must be a positive number'),
  nonEmptyString: z.string().min(1, 'This field is required'),
  optionalString: z.string().optional(),
};

/**
 * Validation helpers
 */
export const validators = {
  required: (message = 'This field is required') => z.string().min(1, message),

  minLength: (length: number, message?: string) =>
    z.string().min(length, message || `Must be at least ${length} characters`),

  maxLength: (length: number, message?: string) =>
    z.string().max(length, message || `Must be no more than ${length} characters`),

  email: (message = 'Invalid email address') => z.string().email(message),

  url: (message = 'Invalid URL') => z.string().url(message),

  number: (message = 'Must be a number') => z.number({ invalid_type_error: message }),

  positiveNumber: (message = 'Must be a positive number') => z.number().positive(message),

  range: (min: number, max: number, message?: string) =>
    z
      .number()
      .min(min, message || `Must be at least ${min}`)
      .max(max, message || `Must be no more than ${max}`),

  phone: (message = 'Invalid phone number') =>
    z.string().regex(/^[+]?[(]?[0-9]{1,4}[)]?[-\s.]?[(]?[0-9]{1,4}[)]?[-\s.]?[0-9]{1,9}$/, message),

  alphanumeric: (message = 'Must contain only letters and numbers') =>
    z.string().regex(/^[a-zA-Z0-9]+$/, message),
};

/**
 * Custom validation functions
 */
export function createFormSchema<T extends z.ZodRawShape>(shape: T) {
  return z.object(shape);
}

export function validateField<T>(
  schema: z.ZodType<T>,
  value: unknown
): { valid: boolean; error?: string } {
  try {
    schema.parse(value);
    return { valid: true };
  } catch (err) {
    if (err instanceof z.ZodError) {
      return { valid: false, error: err.errors[0]?.message };
    }
    return { valid: false, error: 'Validation failed' };
  }
}

/**
 * File validation
 */
export function validateFile(
  file: File,
  options: {
    maxSize?: number; // in bytes
    allowedTypes?: string[];
  }
): { valid: boolean; error?: string } {
  if (options.maxSize && file.size > options.maxSize) {
    const sizeMB = (options.maxSize / (1024 * 1024)).toFixed(2);
    return { valid: false, error: `File size must be less than ${sizeMB}MB` };
  }

  if (options.allowedTypes && !options.allowedTypes.includes(file.type)) {
    return { valid: false, error: `File type must be one of: ${options.allowedTypes.join(', ')}` };
  }

  return { valid: true };
}

/**
 * Form submission helper
 */
export async function handleFormSubmit<T>(
  data: T,
  submitFn: (data: T) => Promise<void>,
  options?: {
    onSuccess?: () => void;
    onError?: (error: Error) => void;
  }
): Promise<{ success: boolean; error?: Error }> {
  try {
    await submitFn(data);
    options?.onSuccess?.();
    return { success: true };
  } catch (error) {
    const err = error instanceof Error ? error : new Error('Submission failed');
    options?.onError?.(err);
    return { success: false, error: err };
  }
}

/**
 * Brief validation schema
 */
export const briefValidationSchema = z.object({
  topic: z.string().min(3, 'Topic must be at least 3 characters'),
  audience: z.string().optional(),
  goal: z.string().optional(),
  tone: z.string().optional(),
  language: z.string().optional(),
  durationMinutes: z
    .number()
    .min(10 / 60, 'Duration must be at least 10 seconds (0.17 minutes)')
    .max(30, 'Duration must be no more than 30 minutes'),
});

/**
 * Validates a brief validation request before sending to API
 */
export function validateBriefRequest(request: {
  topic?: string;
  audience?: string;
  goal?: string;
  tone?: string;
  language?: string;
  durationMinutes?: number;
}): { valid: boolean; errors: string[] } {
  const result = briefValidationSchema.safeParse(request);

  if (result.success) {
    return { valid: true, errors: [] };
  }

  const errors = result.error.errors.map(
    (err) => `${err.path.join('.')}: ${err.message}`
  );

  return { valid: false, errors };
}
