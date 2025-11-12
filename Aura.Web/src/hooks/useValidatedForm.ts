/**
 * Reusable form validation hook using react-hook-form + zod
 * Provides type-safe form validation with inline error display
 */

import { zodResolver } from '@hookform/resolvers/zod';
import { useForm, UseFormProps, UseFormReturn, FieldValues } from 'react-hook-form';
import { ZodSchema } from 'zod';

export interface UseValidatedFormOptions<T extends FieldValues>
  extends Omit<UseFormProps<T>, 'resolver'> {
  schema: ZodSchema<T>;
  onValidSubmit?: (data: T) => void | Promise<void>;
}

/**
 * Hook for creating forms with zod validation
 *
 * @example
 * ```tsx
 * const schema = z.object({
 *   email: z.string().email('Invalid email address'),
 *   password: z.string().min(8, 'Password must be at least 8 characters'),
 * });
 *
 * function LoginForm() {
 *   const { register, handleSubmit, formState: { errors, isSubmitting } } = useValidatedForm({
 *     schema,
 *     onValidSubmit: async (data) => {
 *       await login(data);
 *     },
 *   });
 *
 *   return (
 *     <form onSubmit={handleSubmit}>
 *       <input {...register('email')} />
 *       {errors.email && <span>{errors.email.message}</span>}
 *       <button type="submit" disabled={isSubmitting}>Submit</button>
 *     </form>
 *   );
 * }
 * ```
 */
export function useValidatedForm<T extends FieldValues>({
  schema,
  onValidSubmit,
  ...options
}: UseValidatedFormOptions<T>): UseFormReturn<T> {
  const form = useForm<T>({
    ...options,
    resolver: zodResolver(schema),
  });

  const originalHandleSubmit = form.handleSubmit;

  // Wrap handleSubmit to call onValidSubmit if provided
  if (onValidSubmit) {
    form.handleSubmit = (onValid, onInvalid) => {
      return originalHandleSubmit(async (data) => {
        await onValidSubmit(data);
        if (onValid) {
          await onValid(data);
        }
      }, onInvalid);
    };
  }

  return form;
}
