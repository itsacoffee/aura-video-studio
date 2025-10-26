/**
 * Custom hook for real-time form validation with debouncing
 */

import { useState, useCallback, useEffect, useRef } from 'react';
import { z } from 'zod';

export interface ValidationState {
  isValid: boolean;
  error?: string;
  isValidating?: boolean;
}

export interface FieldValidationState {
  [fieldName: string]: ValidationState;
}

export interface UseFormValidationOptions<T> {
  schema: z.ZodType<T>;
  initialValues?: Partial<T>;
  debounceMs?: number;
  validateOnMount?: boolean;
  onValidationChange?: (isValid: boolean) => void;
}

export interface UseFormValidationReturn<T> {
  values: Partial<T>;
  errors: FieldValidationState;
  isFormValid: boolean;
  isValidating: boolean;
  setValue: (field: keyof T, value: T[keyof T]) => void;
  setValues: (values: Partial<T>) => void;
  validateField: (field: keyof T) => Promise<ValidationState>;
  validateForm: () => Promise<boolean>;
  resetValidation: () => void;
  resetForm: (newValues?: Partial<T>) => void;
}

/**
 * Hook for managing form validation with real-time feedback
 *
 * @example
 * ```tsx
 * const { values, errors, isFormValid, setValue } = useFormValidation({
 *   schema: z.object({
 *     email: z.string().email(),
 *     password: z.string().min(8),
 *   }),
 *   initialValues: { email: '', password: '' },
 *   debounceMs: 300,
 * });
 * ```
 */
export function useFormValidation<T extends Record<string, unknown>>({
  schema,
  initialValues = {} as Partial<T>,
  debounceMs = 300,
  validateOnMount = false,
  onValidationChange,
}: UseFormValidationOptions<T>): UseFormValidationReturn<T> {
  const [values, setValuesState] = useState<Partial<T>>(initialValues);
  const [errors, setErrors] = useState<FieldValidationState>({});
  const [isFormValid, setIsFormValid] = useState(false);
  const [isValidating, setIsValidating] = useState(false);

  // Debounce timers for each field
  const debounceTimers = useRef<{ [key: string]: ReturnType<typeof setTimeout> }>({});
  const validationCache = useRef<{ [key: string]: ValidationState }>({});
  const isMounted = useRef(true);

  useEffect(() => {
    isMounted.current = true;
    return () => {
      isMounted.current = false;
      // Clear all debounce timers on unmount
      Object.values(debounceTimers.current).forEach(clearTimeout);
    };
  }, []);

  // Validate a single field
  const validateField = useCallback(
    async (field: keyof T): Promise<ValidationState> => {
      // Check if schema is an object schema
      if (!(schema instanceof z.ZodObject)) {
        return { isValid: true };
      }

      const fieldValue = values[field];
      const fieldSchema = schema.shape[field as string];

      if (!fieldSchema) {
        return { isValid: true };
      }

      try {
        await fieldSchema.parseAsync(fieldValue);
        return { isValid: true };
      } catch (err) {
        if (err instanceof z.ZodError) {
          return {
            isValid: false,
            error: err.errors[0]?.message || 'Validation failed',
          };
        }
        return { isValid: false, error: 'Validation error' };
      }
    },
    [schema, values]
  );

  // Validate entire form
  const validateForm = useCallback(async (): Promise<boolean> => {
    setIsValidating(true);
    try {
      await schema.parseAsync(values);
      setIsFormValid(true);
      setErrors({});
      onValidationChange?.(true);
      return true;
    } catch (err) {
      if (err instanceof z.ZodError) {
        const fieldErrors: FieldValidationState = {};
        err.errors.forEach((error) => {
          const fieldName = error.path[0]?.toString();
          if (fieldName) {
            fieldErrors[fieldName] = {
              isValid: false,
              error: error.message,
            };
          }
        });
        setErrors(fieldErrors);
      }
      setIsFormValid(false);
      onValidationChange?.(false);
      return false;
    } finally {
      if (isMounted.current) {
        setIsValidating(false);
      }
    }
  }, [schema, values, onValidationChange]);

  // Set a single field value with debounced validation
  const setValue = useCallback(
    (field: keyof T, value: T[keyof T]) => {
      setValuesState((prev) => ({ ...prev, [field]: value }));

      // Clear existing timer for this field
      if (debounceTimers.current[field as string]) {
        clearTimeout(debounceTimers.current[field as string]);
      }

      // Set field as validating
      setErrors((prev) => ({
        ...prev,
        [field as string]: { isValid: prev[field as string]?.isValid ?? true, isValidating: true },
      }));

      // Set debounced validation
      debounceTimers.current[field as string] = setTimeout(async () => {
        const validationResult = await validateField(field);
        validationCache.current[field as string] = validationResult;

        if (isMounted.current) {
          setErrors((prev) => ({
            ...prev,
            [field as string]: { ...validationResult, isValidating: false },
          }));

          // Check if entire form is valid
          const allFieldsValid = Object.keys(values).every(
            (key) => validationCache.current[key]?.isValid !== false
          );
          setIsFormValid(allFieldsValid);
        }
      }, debounceMs);
    },
    [validateField, debounceMs, values]
  );

  // Set multiple values at once
  const setValues = useCallback((newValues: Partial<T>) => {
    setValuesState((prev) => ({ ...prev, ...newValues }));
  }, []);

  // Reset validation state
  const resetValidation = useCallback(() => {
    setErrors({});
    setIsFormValid(false);
    validationCache.current = {};
  }, []);

  // Reset form to initial or new values
  const resetForm = useCallback(
    (newValues?: Partial<T>) => {
      setValuesState(newValues || initialValues);
      resetValidation();
    },
    [initialValues, resetValidation]
  );

  // Validate on mount if requested
  useEffect(() => {
    if (validateOnMount) {
      validateForm();
    }
  }, [validateOnMount]); // Only run on mount

  return {
    values,
    errors,
    isFormValid,
    isValidating,
    setValue,
    setValues,
    validateField,
    validateForm,
    resetValidation,
    resetForm,
  };
}
