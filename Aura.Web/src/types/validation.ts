/**
 * Validation schema types for forms
 */

/**
 * Validation result from a field or form validation
 */
export interface ValidationResult {
  isValid: boolean;
  error?: string;
  errors?: Record<string, string>;
}

/**
 * Field validation state for real-time validation
 */
export interface FieldValidation {
  isValid: boolean;
  error?: string;
  isValidating?: boolean;
  touched?: boolean;
}

/**
 * Form validation state
 */
export interface FormValidationState<T> {
  values: T;
  errors: Partial<Record<keyof T, string>>;
  touched: Partial<Record<keyof T, boolean>>;
  isValid: boolean;
  isValidating: boolean;
  isSubmitting: boolean;
}

/**
 * Validation rule definition
 */
export interface ValidationRule<T = any> {
  validator: (value: T) => boolean | Promise<boolean>;
  message: string;
}

/**
 * Field validation configuration
 */
export interface FieldValidationConfig<T = any> {
  required?: boolean | string;
  minLength?: { value: number; message?: string };
  maxLength?: { value: number; message?: string };
  pattern?: { value: RegExp; message?: string };
  min?: { value: number; message?: string };
  max?: { value: number; message?: string };
  validate?: ValidationRule<T> | Record<string, ValidationRule<T>>;
  debounceMs?: number;
}

/**
 * Form schema configuration
 */
export type FormSchemaConfig<T extends Record<string, any>> = {
  [K in keyof T]?: FieldValidationConfig<T[K]>;
};
