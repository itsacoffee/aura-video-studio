/**
 * Example Form Component with Validation
 * Demonstrates react-hook-form + zod validation with error display
 */

import {
  Button,
  Input,
  makeStyles,
  shorthands,
  Spinner,
  Text,
  tokens,
} from '@fluentui/react-components';
import { useState } from 'react';
import { z } from 'zod';
import { useValidatedForm } from '../../hooks/useValidatedForm';
import { FormField } from '../forms/FormField';

const useStyles = makeStyles({
  form: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalL),
    maxWidth: '500px',
    ...shorthands.padding(tokens.spacingVerticalL),
  },
  actions: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalS),
  },
  success: {
    color: tokens.colorPaletteGreenForeground1,
    ...shorthands.padding(tokens.spacingVerticalS),
  },
});

// Define validation schema using zod
const videoFormSchema = z.object({
  title: z
    .string()
    .min(1, 'Title is required')
    .min(3, 'Title must be at least 3 characters')
    .max(100, 'Title must be less than 100 characters'),
  description: z
    .string()
    .min(10, 'Description must be at least 10 characters')
    .max(500, 'Description must be less than 500 characters')
    .optional()
    .or(z.literal('')),
  duration: z
    .number({ invalid_type_error: 'Duration must be a number' })
    .min(10, 'Duration must be at least 10 seconds')
    .max(600, 'Duration must be less than 10 minutes'),
  apiKey: z
    .string()
    .min(1, 'API key is required')
    .regex(/^[A-Za-z0-9_-]+$/, 'API key contains invalid characters'),
});

type VideoFormData = z.infer<typeof videoFormSchema>;

interface ExampleValidatedFormProps {
  onSubmit?: (data: VideoFormData) => Promise<void>;
}

export function ExampleValidatedForm({ onSubmit }: ExampleValidatedFormProps) {
  const styles = useStyles();
  const [submitSuccess, setSubmitSuccess] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useValidatedForm<VideoFormData>({
    schema: videoFormSchema,
    defaultValues: {
      title: '',
      description: '',
      duration: 60,
      apiKey: '',
    },
    onValidSubmit: async (data) => {
      setSubmitSuccess(false);

      // Simulate async submission
      if (onSubmit) {
        await onSubmit(data);
      } else {
        await new Promise((resolve) => setTimeout(resolve, 1000));
      }

      setSubmitSuccess(true);

      // Reset form after success
      setTimeout(() => {
        setSubmitSuccess(false);
      }, 3000);
    },
  });

  return (
    <form className={styles.form} onSubmit={handleSubmit(() => {})}>
      <Text size={600} weight="semibold">
        Create Video
      </Text>

      <FormField
        label="Video Title"
        error={errors.title}
        required
        helpText="Give your video a descriptive title"
      >
        <Input
          {...register('title')}
          placeholder="Enter video title"
          aria-invalid={!!errors.title}
          disabled={isSubmitting}
        />
      </FormField>

      <FormField
        label="Description"
        error={errors.description}
        helpText="Optional description of your video"
      >
        <Input
          {...register('description')}
          placeholder="Enter description"
          aria-invalid={!!errors.description}
          disabled={isSubmitting}
        />
      </FormField>

      <FormField
        label="Duration (seconds)"
        error={errors.duration}
        required
        helpText="Video duration in seconds (10-600)"
      >
        <Input
          {...register('duration', { valueAsNumber: true })}
          type="number"
          placeholder="60"
          aria-invalid={!!errors.duration}
          disabled={isSubmitting}
        />
      </FormField>

      <FormField label="API Key" error={errors.apiKey} required helpText="Your provider API key">
        <Input
          {...register('apiKey')}
          type="password"
          placeholder="Enter API key"
          aria-invalid={!!errors.apiKey}
          disabled={isSubmitting}
        />
      </FormField>

      {submitSuccess && (
        <div className={styles.success}>
          <Text weight="semibold">✓ Form submitted successfully!</Text>
        </div>
      )}

      <div className={styles.actions}>
        <Button
          type="submit"
          appearance="primary"
          disabled={isSubmitting}
          icon={isSubmitting ? <Spinner size="tiny" /> : undefined}
        >
          {isSubmitting ? 'Submitting...' : 'Submit'}
        </Button>
        <Button
          type="button"
          appearance="secondary"
          onClick={() => reset()}
          disabled={isSubmitting}
        >
          Reset
        </Button>
      </div>
    </form>
  );
}
