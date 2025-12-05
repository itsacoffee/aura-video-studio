import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Title3,
  Text,
  Button,
  Dropdown,
  Option,
  Slider,
  Card,
  Field,
  Spinner,
  Badge,
  Checkbox,
  Tooltip,
  Textarea,
} from '@fluentui/react-components';
import {
  Play24Regular,
  Lightbulb24Regular,
  Checkmark24Regular,
  ChevronDown24Regular,
  ChevronUp24Regular,
  DocumentBulletList24Regular,
  Compose24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { ValidatedInput } from '../components/forms/ValidatedInput';
import { JobRecoveryBanner } from '../components/JobRecoveryBanner';
import { useNotifications } from '../components/Notifications/Toasts';
import { PreflightOverrideDialog } from '../components/PreflightOverrideDialog';
import { PreflightPanel } from '../components/PreflightPanel';
import { ImageProviderSelector } from '../components/Providers/ImageProviderSelector';
import { TemplateGallery, TemplateConfigurator } from '../components/VideoTemplates';
import { apiUrl } from '../config/api';
import { useDisableWhenOffline } from '../hooks/useDisableWhenOffline';
import { useFormValidation } from '../hooks/useFormValidation';
import { keyboardShortcutManager } from '../services/keyboardShortcutManager';
import { generateVideo } from '../services/api/videoApi';
import { useActivity } from '../state/activityContext';
import type { PreflightReport } from '../state/providers';
import { container, spacing, gaps, formLayout } from '../themes/layout';
import type { Brief, PlanSpec, PlannerRecommendations } from '../types';
import type { VideoTemplate, TemplatedBrief } from '../types/videoTemplates';
import { normalizeEnumsForApi, validateAndWarnEnums } from '../utils/enumNormalizer';

/**
 * Format duration in minutes to HH:MM:SS string
 */
function formatDuration(minutes: number): string {
  const totalSeconds = Math.floor(minutes * 60);
  const hrs = Math.floor(totalSeconds / 3600);
  const mins = Math.floor((totalSeconds % 3600) / 60);
  const secs = totalSeconds % 60;
  return (
    String(hrs).padStart(2, '0') +
    ':' +
    String(mins).padStart(2, '0') +
    ':' +
    String(secs).padStart(2, '0')
  );
}

const useStyles = makeStyles({
  container: {
    maxWidth: container.formMaxWidth,
    margin: '0 auto',
  },
  header: {
    marginBottom: spacing.xxl,
    display: 'flex',
    flexDirection: 'column',
    gap: spacing.sm,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase400,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: spacing.lg,
  },
  section: {
    padding: spacing.xl,
  },
  sectionTitle: {
    marginBottom: spacing.sm,
  },
  sectionDescription: {
    marginBottom: spacing.lg,
  },
  actions: {
    display: 'flex',
    gap: gaps.standard,
    justifyContent: 'flex-end',
    marginTop: spacing.xl,
  },
  fieldGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: formLayout.fieldGap,
  },
  advancedHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    cursor: 'pointer',
    paddingTop: spacing.md,
    paddingBottom: spacing.md,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
    marginTop: spacing.lg,
  },
  advancedContent: {
    paddingTop: spacing.md,
  },
  modeSelection: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
    gap: spacing.lg,
    marginTop: spacing.lg,
  },
  modeCard: {
    padding: spacing.xl,
    cursor: 'pointer',
    transition: 'transform 0.2s ease, box-shadow 0.2s ease',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow16,
    },
  },
  modeCardHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: spacing.md,
    marginBottom: spacing.md,
  },
  modeIcon: {
    width: '48px',
    height: '48px',
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: '24px',
  },
  modeTitle: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase500,
  },
  modeDescription: {
    color: tokens.colorNeutralForeground2,
    fontSize: tokens.fontSizeBase300,
  },
});

// Creation modes
type CreateMode = 'select' | 'template' | 'scratch';

export function CreatePage() {
  const styles = useStyles();
  const navigate = useNavigate();
  const { addActivity, updateActivity } = useActivity();
  const isOfflineDisabled = useDisableWhenOffline();
  const { showSuccessToast, showFailureToast } = useNotifications();

  // Template mode state
  const [createMode, setCreateMode] = useState<CreateMode>('select');
  const [selectedTemplate, setSelectedTemplate] = useState<VideoTemplate | null>(null);

  const [currentStep, setCurrentStep] = useState(1);
  const [brief, setBrief] = useState<Partial<Brief>>({
    topic: '',
    audience: 'General',
    goal: 'Inform',
    tone: 'Informative',
    language: 'en-US',
    aspect: 'Widescreen16x9',
  });

  const [planSpec, setPlanSpec] = useState<Partial<PlanSpec>>({
    targetDurationMinutes: 3.0,
    pacing: 'Conversational',
    density: 'Balanced',
    style: 'Standard',
  });

  // Form validation state
  const briefValidationSchema = z.object({
    topic: z
      .string()
      .min(3, 'Topic must be at least 3 characters')
      .max(100, 'Topic must be no more than 100 characters'),
  });

  const {
    values: briefValues,
    errors: briefErrors,
    isFormValid: isBriefValid,
    setValue: setBriefValue,
  } = useFormValidation({
    schema: briefValidationSchema,
    initialValues: { topic: brief.topic || '' },
    debounceMs: 500,
  });

  // Update brief when validated values change
  useEffect(() => {
    if (briefValues.topic !== brief.topic) {
      setBrief((prevBrief) => ({ ...prevBrief, topic: briefValues.topic }));
    }
  }, [briefValues.topic, brief.topic]);

  const [generating, setGenerating] = useState(false);
  const [loadingRecommendations, setLoadingRecommendations] = useState(false);
  const [recommendations, setRecommendations] = useState<PlannerRecommendations | null>(null);
  const [showRecommendations, setShowRecommendations] = useState(false);
  const [showAdvancedOptions, setShowAdvancedOptions] = useState(false);

  // Preflight state
  const [selectedProfile, setSelectedProfile] = useState('Free-Only');
  const [preflightReport, setPreflightReport] = useState<PreflightReport | null>(null);
  const [isRunningPreflight, setIsRunningPreflight] = useState(false);
  const [overridePreflightGate, setOverridePreflightGate] = useState(false);

  // Image provider state
  const [selectedImageProvider, setSelectedImageProvider] = useState<string | null>(null);

  // Override dialog state
  const [showOverrideDialog, setShowOverrideDialog] = useState(false);

  const handleGetRecommendations = async () => {
    setLoadingRecommendations(true);
    try {
      const response = await fetch('/api/planner/recommendations', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          topic: brief.topic,
          audience: brief.audience,
          goal: brief.goal,
          tone: brief.tone,
          language: brief.language,
          aspect: brief.aspect,
          targetDurationMinutes: planSpec.targetDurationMinutes,
          pacing: planSpec.pacing,
          density: planSpec.density,
          style: planSpec.style,
        }),
      });

      if (response.ok) {
        const data = await response.json();
        setRecommendations(data.recommendations);
        setShowRecommendations(true);
      } else {
        alert('Failed to get recommendations');
      }
    } catch (error) {
      console.error('Error getting recommendations:', error);
      alert('Error getting recommendations');
    } finally {
      setLoadingRecommendations(false);
    }
  };

  const applyAllRecommendations = () => {
    if (!recommendations) return;
    // Apply recommendations that map to planSpec
    // Additional fields can be mapped as needed
    alert('Recommendations applied successfully!');
  };

  const handleRunPreflight = async () => {
    setIsRunningPreflight(true);
    try {
      const response = await fetch(`/api/preflight?profile=${encodeURIComponent(selectedProfile)}`);

      if (response.ok) {
        const data = await response.json();
        setPreflightReport(data);
      } else {
        alert('Failed to run preflight check');
      }
    } catch (error) {
      console.error('Error running preflight check:', error);
      alert('Error running preflight check');
    } finally {
      setIsRunningPreflight(false);
    }
  };

  const handleApplySafeDefaults = async () => {
    try {
      const response = await fetch(apiUrl('/api/preflight/safe-defaults'));

      if (response.ok) {
        // Apply safe defaults
        setSelectedProfile('Free-Only');

        // Re-run preflight check
        await handleRunPreflight();

        alert('Applied safe defaults: Free-Only mode.');
      } else {
        alert('Failed to get safe defaults');
      }
    } catch (error) {
      console.error('Error applying safe defaults:', error);
      alert('Error applying safe defaults');
    }
  };

  const handleGenerate = useCallback(async () => {
    // Validate required fields before submission
    const validationErrors: string[] = [];

    if (!brief.topic || brief.topic.trim().length < 3) {
      validationErrors.push('Topic must be at least 3 characters');
    }

    if (!planSpec.targetDurationMinutes || planSpec.targetDurationMinutes < 0.5) {
      validationErrors.push('Duration must be at least 30 seconds');
    }

    if (validationErrors.length > 0) {
      showFailureToast({
        title: 'Validation Error',
        message: validationErrors.join('. '),
      });
      return;
    }

    setGenerating(true);

    const activityId = addActivity({
      type: 'video-generation',
      title: 'Generating Video',
      message: 'Creating video about "' + brief.topic + '"',
      canCancel: true,
      canRetry: true,
      metadata: { topic: brief.topic },
    });

    updateActivity(activityId, { status: 'running', progress: 0 });

    try {
      // Validate and warn about legacy enum values
      validateAndWarnEnums(brief, planSpec);

      // Normalize enums to canonical values before sending to API
      const { brief: normalizedBrief, planSpec: normalizedPlanSpec } = normalizeEnumsForApi(
        brief,
        planSpec
      );

      // Build typed request
      const request = {
        brief: {
          topic: normalizedBrief.topic || '',
          audience: normalizedBrief.audience || 'General',
          goal: normalizedBrief.goal || 'Inform',
          tone: normalizedBrief.tone || 'Informative',
          language: normalizedBrief.language || 'en-US',
          aspect: normalizedBrief.aspect || 'Widescreen16x9',
        },
        planSpec: {
          targetDuration: formatDuration(normalizedPlanSpec.targetDurationMinutes || 3),
          pacing: normalizedPlanSpec.pacing || 'Conversational',
          density: normalizedPlanSpec.density || 'Balanced',
          style: normalizedPlanSpec.style || 'Standard',
        },
        voiceSpec: {
          voiceName: 'en-US-Standard-A',
          rate: 1.0,
          pitch: 0.0,
          pause: 'Medium',
        },
        renderSpec: {
          res: '1920x1080',
          container: 'mp4',
          videoBitrateK: 5000,
          audioBitrateK: 192,
          fps: 30,
          codec: 'H264',
          qualityLevel: '75',
          enableSceneCut: true,
        },
      };

      updateActivity(activityId, { progress: 10, message: 'Sending request to server...' });

      // Use typed API client with built-in retry and error handling
      const response = await generateVideo(request, {
        timeout: 60000,
      });

      // Store job ID in session storage for recovery
      sessionStorage.setItem('lastJobId', response.jobId);
      sessionStorage.setItem('lastJobTopic', brief.topic || '');

      updateActivity(activityId, {
        status: 'completed',
        progress: 100,
        message: 'Video generation job created: ' + response.jobId,
        metadata: { jobId: response.jobId },
      });

      showSuccessToast({
        title: 'Video Generation Started',
        message: 'Job ID: ' + response.jobId + '. You can track progress in the jobs panel.',
      });

      navigate('/jobs');
    } catch (error: unknown) {
      console.error('Error creating video generation job:', error);

      // Extract user-friendly error message
      let errorMessage = 'Unknown error occurred';
      let errorDetails: string | undefined;

      if (error instanceof Error) {
        errorMessage = error.message;

        if (error.message.includes('network') || error.message.includes('fetch')) {
          errorMessage = 'Unable to connect to server. Please check your connection.';
        } else if (error.message.includes('timeout')) {
          errorMessage = 'Request timed out. The server may be busy.';
        } else if (error.message.includes('401') || error.message.includes('unauthorized')) {
          errorMessage = 'Authentication required. Please refresh the page.';
        }
      }

      // Check for API error response
      if (error && typeof error === 'object' && 'response' in error) {
        const apiError = error as {
          response?: { data?: { detail?: string; errors?: string[] } };
        };
        if (apiError.response?.data?.detail) {
          errorMessage = apiError.response.data.detail;
        }
        if (apiError.response?.data?.errors) {
          errorDetails = apiError.response.data.errors.join(', ');
        }
      }

      updateActivity(activityId, {
        status: 'failed',
        message: 'Error starting video generation',
        error: errorMessage,
      });

      showFailureToast({
        title: 'Video Generation Error',
        message: errorMessage,
        errorDetails: errorDetails,
      });
    } finally {
      setGenerating(false);
    }
  }, [brief, planSpec, addActivity, updateActivity, showSuccessToast, showFailureToast, navigate]);

  // Handle preflight override toggle - show dialog for confirmation
  const handleOverrideToggle = useCallback(
    (checked: boolean) => {
      if (checked && preflightReport && !preflightReport.ok) {
        setShowOverrideDialog(true);
      } else {
        setOverridePreflightGate(checked);
      }
    },
    [preflightReport]
  );

  // Register Create workflow shortcuts
  useEffect(() => {
    // Set the active context
    keyboardShortcutManager.setActiveContext('create');

    // Register create-specific shortcuts
    keyboardShortcutManager.registerMultiple([
      {
        id: 'create-next-step',
        keys: 'Ctrl+Enter',
        description: 'Next step',
        context: 'create',
        handler: () => {
          if (currentStep < 3) {
            if (currentStep === 1 && !brief.topic) {
              return; // Don't proceed if topic is empty
            }
            setCurrentStep(currentStep + 1);
          } else if (
            currentStep === 3 &&
            preflightReport &&
            (preflightReport.ok || overridePreflightGate)
          ) {
            handleGenerate();
          }
        },
      },
      {
        id: 'create-previous-step',
        keys: 'Ctrl+Shift+Enter',
        description: 'Previous step',
        context: 'create',
        handler: () => {
          if (currentStep > 1) {
            setCurrentStep(currentStep - 1);
          }
        },
      },
      {
        id: 'create-cancel',
        keys: 'Escape',
        description: 'Cancel/Go back',
        context: 'create',
        handler: () => {
          if (currentStep > 1) {
            setCurrentStep(currentStep - 1);
          } else {
            navigate('/');
          }
        },
      },
    ]);

    // Clean up on unmount
    return () => {
      keyboardShortcutManager.unregisterContext('create');
    };
  }, [currentStep, brief.topic, preflightReport, overridePreflightGate, navigate, handleGenerate]);

  // Handle template selection
  const handleSelectTemplate = useCallback((template: VideoTemplate) => {
    setSelectedTemplate(template);
  }, []);

  // Handle template generation
  const handleTemplateGenerate = useCallback(
    async (templatedBrief: TemplatedBrief) => {
      // Convert templated brief to format expected by the job API
      const combinedScript = templatedBrief.sections
        .map((s) => `## ${s.name}\n\n${s.content}`)
        .join('\n\n');

      // Set brief values from template result
      setBrief({
        topic: templatedBrief.brief.topic,
        audience: templatedBrief.brief.audience || 'General',
        goal: templatedBrief.brief.goal || 'Inform',
        tone: templatedBrief.brief.tone,
        language: templatedBrief.brief.language,
        aspect: templatedBrief.brief.aspect as Brief['aspect'],
        scriptGuidance: combinedScript,
      });

      setPlanSpec({
        targetDurationMinutes: templatedBrief.planSpec.targetDurationSeconds / 60,
        pacing: templatedBrief.planSpec.pacing as PlanSpec['pacing'],
        density: templatedBrief.planSpec.density as PlanSpec['density'],
        style: templatedBrief.planSpec.style,
      });

      // Switch to scratch mode at step 3 (review)
      setCreateMode('scratch');
      setCurrentStep(3);

      showSuccessToast({
        title: 'Template Applied',
        message: 'Your brief has been generated from the template. Review and generate your video.',
      });
    },
    [showSuccessToast]
  );

  // If in template selection mode
  if (createMode === 'template' && !selectedTemplate) {
    return (
      <TemplateGallery
        onSelectTemplate={handleSelectTemplate}
        onBack={() => setCreateMode('select')}
      />
    );
  }

  // If configuring a template
  if (createMode === 'template' && selectedTemplate) {
    return (
      <TemplateConfigurator
        template={selectedTemplate}
        onBack={() => setSelectedTemplate(null)}
        onGenerate={handleTemplateGenerate}
      />
    );
  }

  // Mode selection screen
  if (createMode === 'select') {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Title1>Create Video</Title1>
          <Text className={styles.subtitle}>Choose how you want to get started</Text>
        </div>

        <div className={styles.modeSelection}>
          <Card className={styles.modeCard} onClick={() => setCreateMode('template')}>
            <div className={styles.modeCardHeader}>
              <div
                className={styles.modeIcon}
                style={{ backgroundColor: tokens.colorBrandBackground, color: 'white' }}
              >
                <DocumentBulletList24Regular />
              </div>
              <div>
                <Text className={styles.modeTitle}>Start with Template</Text>
                <Badge appearance="tint" color="success" style={{ marginLeft: '8px' }}>
                  Recommended
                </Badge>
              </div>
            </div>
            <Text className={styles.modeDescription}>
              Choose from pre-built video structures like Explainers, Listicles, Tutorials, and
              more. Perfect for following proven formats that engage viewers.
            </Text>
          </Card>

          <Card className={styles.modeCard} onClick={() => setCreateMode('scratch')}>
            <div className={styles.modeCardHeader}>
              <div
                className={styles.modeIcon}
                style={{ backgroundColor: tokens.colorNeutralBackground3 }}
              >
                <Compose24Regular />
              </div>
              <Text className={styles.modeTitle}>Start from Scratch</Text>
            </div>
            <Text className={styles.modeDescription}>
              Build your video with complete creative freedom. Define your own topic, audience, and
              structure without any template constraints.
            </Text>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Create Video</Title1>
        <Text className={styles.subtitle}>Step {currentStep} of 3</Text>
      </div>

      <JobRecoveryBanner />

      <div className={styles.form}>
        {currentStep === 1 && (
          <Card className={styles.section}>
            <Title2 className={styles.sectionTitle}>Brief</Title2>
            <Text size={200} className={styles.sectionDescription}>
              Define the core details of your video
            </Text>
            <div className={styles.fieldGroup}>
              <ValidatedInput
                label="Topic"
                required
                value={briefValues.topic || ''}
                onChange={(value) => setBriefValue('topic', value)}
                error={briefErrors.topic?.error}
                isValid={briefErrors.topic?.isValid}
                isValidating={briefErrors.topic?.isValidating}
                placeholder="e.g., Introduction to Machine Learning"
                hint="Enter a topic between 3 and 100 characters"
              />

              <Field label="Audience" hint="Who is this video for?">
                <Dropdown
                  value={brief.audience}
                  onOptionSelect={(_, data) => setBrief({ ...brief, audience: data.optionText })}
                >
                  <Option>General</Option>
                  <Option>Beginners</Option>
                  <Option>Advanced</Option>
                  <Option>Professionals</Option>
                </Dropdown>
              </Field>

              <Field label="Tone" hint="What style should the video have?">
                <Dropdown
                  value={brief.tone}
                  onOptionSelect={(_, data) => setBrief({ ...brief, tone: data.optionText })}
                >
                  <Option>Informative</Option>
                  <Option>Casual</Option>
                  <Option>Professional</Option>
                  <Option>Energetic</Option>
                </Dropdown>
              </Field>

              <Field label="Aspect Ratio" hint="Video dimensions">
                <Dropdown
                  value={brief.aspect}
                  onOptionSelect={(_, data) =>
                    setBrief({ ...brief, aspect: data.optionValue as Brief['aspect'] })
                  }
                >
                  <Option value="Widescreen16x9">16:9 Widescreen</Option>
                  <Option value="Vertical9x16">9:16 Vertical</Option>
                  <Option value="Square1x1">1:1 Square</Option>
                </Dropdown>
              </Field>
            </div>
          </Card>
        )}

        {currentStep === 2 && (
          <>
            <Card className={styles.section}>
              <Title2 className={styles.sectionTitle}>Length and Pacing</Title2>
              <Text size={200} className={styles.sectionDescription}>
                Configure the duration and pacing of your video
              </Text>
              <div className={styles.fieldGroup}>
                <Field
                  label={`Duration: ${planSpec.targetDurationMinutes} minutes`}
                  hint="Recommended: 0.5 to 20 minutes. Shorter videos have higher engagement."
                >
                  <Slider
                    min={0.5}
                    max={20}
                    step={0.5}
                    value={planSpec.targetDurationMinutes}
                    onChange={(_, data) =>
                      setPlanSpec({ ...planSpec, targetDurationMinutes: data.value })
                    }
                  />
                </Field>

                <Field label="Pacing" hint="How fast should the narration be?">
                  <Dropdown
                    value={planSpec.pacing}
                    onOptionSelect={(_, data) =>
                      setPlanSpec({ ...planSpec, pacing: data.optionText as PlanSpec['pacing'] })
                    }
                  >
                    <Option>Chill</Option>
                    <Option>Conversational</Option>
                    <Option>Fast</Option>
                  </Dropdown>
                </Field>

                <Field label="Density" hint="How much content per minute?">
                  <Dropdown
                    value={planSpec.density}
                    onOptionSelect={(_, data) =>
                      setPlanSpec({ ...planSpec, density: data.optionText as PlanSpec['density'] })
                    }
                  >
                    <Option>Sparse</Option>
                    <Option>Balanced</Option>
                    <Option>Dense</Option>
                  </Dropdown>
                </Field>

                {/* Advanced Options - Script Guidance */}
                <div
                  className={styles.advancedHeader}
                  onClick={() => setShowAdvancedOptions(!showAdvancedOptions)}
                  role="button"
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      setShowAdvancedOptions(!showAdvancedOptions);
                    }
                  }}
                >
                  <Text weight="semibold">Advanced Options</Text>
                  {showAdvancedOptions ? <ChevronUp24Regular /> : <ChevronDown24Regular />}
                </div>

                {showAdvancedOptions && (
                  <div className={styles.advancedContent}>
                    <Field
                      label="Script Guidance"
                      hint="Optional: Provide specific instructions to guide the AI in generating your script. For example: 'Include a personal anecdote about learning to code', 'Focus on practical examples', 'Use a storytelling approach', or 'Emphasize beginner-friendly explanations'."
                    >
                      <Textarea
                        value={brief.scriptGuidance || ''}
                        onChange={(_, data) => setBrief({ ...brief, scriptGuidance: data.value })}
                        placeholder="Example: Start with an attention-grabbing statistic about the topic. Include 3 practical tips that viewers can apply immediately. End with a thought-provoking question."
                        resize="vertical"
                        style={{ minHeight: '100px' }}
                      />
                    </Field>
                    <Text
                      size={200}
                      style={{
                        color: tokens.colorNeutralForeground3,
                        marginTop: tokens.spacingVerticalS,
                      }}
                    >
                      Tip: Be specific about what you want included or how you want the content
                      structured. The AI will incorporate your guidance into the script generation.
                    </Text>
                  </div>
                )}

                <div style={{ marginTop: tokens.spacingVerticalL }}>
                  <Button
                    appearance="secondary"
                    icon={<Lightbulb24Regular />}
                    onClick={handleGetRecommendations}
                    disabled={loadingRecommendations || !brief.topic}
                  >
                    {loadingRecommendations ? (
                      <>
                        <Spinner size="tiny" /> Getting Recommendations...
                      </>
                    ) : (
                      'Get Recommendations'
                    )}
                  </Button>
                </div>
              </div>
            </Card>

            {/* Image Provider Selection */}
            <Card className={styles.section}>
              <Title2 className={styles.sectionTitle}>Visual Content</Title2>
              <Text size={200} className={styles.sectionDescription}>
                Choose where to source images for your video
              </Text>
              <ImageProviderSelector
                selectedProvider={selectedImageProvider}
                onProviderSelect={setSelectedImageProvider}
              />
            </Card>

            {showRecommendations && recommendations && (
              <Card
                className={styles.section}
                style={{ backgroundColor: tokens.colorNeutralBackground2 }}
              >
                <div
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    marginBottom: tokens.spacingVerticalM,
                  }}
                >
                  <Title3>AI Recommendations</Title3>
                  <Button
                    appearance="primary"
                    icon={<Checkmark24Regular />}
                    onClick={applyAllRecommendations}
                  >
                    Apply All
                  </Button>
                </div>

                <div
                  style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalL }}
                >
                  <div>
                    <Text weight="semibold">Scene Count:</Text>{' '}
                    <Badge appearance="tint" color="informative">
                      {recommendations.sceneCount} scenes
                    </Badge>
                  </div>

                  <div>
                    <Text weight="semibold">Shots per Scene:</Text>{' '}
                    <Badge appearance="tint" color="informative">
                      {recommendations.shotsPerScene} shots
                    </Badge>
                  </div>

                  <div>
                    <Text weight="semibold">B-Roll:</Text>{' '}
                    <Badge appearance="tint" color="informative">
                      {recommendations.bRollPercentage}%
                    </Badge>
                  </div>

                  <div>
                    <Text weight="semibold">Overlay Density:</Text>{' '}
                    <Badge appearance="tint" color="informative">
                      {recommendations.overlayDensity} overlays/scene
                    </Badge>
                  </div>

                  <div>
                    <Text weight="semibold">Reading Level:</Text>{' '}
                    <Badge appearance="tint" color="informative">
                      Grade {recommendations.readingLevel}
                    </Badge>
                  </div>

                  <div>
                    <Text weight="semibold">Voice:</Text>{' '}
                    <Text>
                      Rate: {recommendations.voice.rate}x, Pitch: {recommendations.voice.pitch}x,
                      Style: {recommendations.voice.style}
                    </Text>
                  </div>

                  <div>
                    <Text weight="semibold">Music:</Text>{' '}
                    <Text>
                      {recommendations.music.genre} - {recommendations.music.tempo}
                    </Text>
                  </div>

                  <div>
                    <Text weight="semibold">Captions:</Text>{' '}
                    <Text>
                      {recommendations.captions.position}, {recommendations.captions.fontSize}
                    </Text>
                  </div>

                  <div>
                    <Text weight="semibold">SEO Title:</Text>{' '}
                    <Text>{recommendations.seo.title}</Text>
                  </div>

                  <div>
                    <Text weight="semibold">Tags:</Text>{' '}
                    <Text>{recommendations.seo.tags.join(', ')}</Text>
                  </div>

                  <div>
                    <Text weight="semibold">Thumbnail Prompt:</Text>
                    <div
                      style={{
                        marginTop: tokens.spacingVerticalXS,
                        padding: tokens.spacingVerticalM,
                        backgroundColor: tokens.colorNeutralBackground1,
                        borderRadius: tokens.borderRadiusMedium,
                      }}
                    >
                      <Text size={200}>{recommendations.thumbnailPrompt}</Text>
                    </div>
                  </div>

                  <div>
                    <Text weight="semibold">Outline:</Text>
                    <div
                      style={{
                        marginTop: tokens.spacingVerticalXS,
                        padding: tokens.spacingVerticalM,
                        backgroundColor: tokens.colorNeutralBackground1,
                        borderRadius: tokens.borderRadiusMedium,
                        whiteSpace: 'pre-wrap',
                      }}
                    >
                      <Text size={200} style={{ fontFamily: 'monospace' }}>
                        {recommendations.outline}
                      </Text>
                    </div>
                  </div>
                </div>
              </Card>
            )}
          </>
        )}

        {currentStep === 3 && (
          <>
            <Card className={styles.section}>
              <Title2>Review Settings</Title2>
              <Text size={200} style={{ marginBottom: tokens.spacingVerticalL }}>
                Review your settings before generating your video
              </Text>
              <div
                style={{
                  marginTop: tokens.spacingVerticalL,
                  display: 'flex',
                  flexDirection: 'column',
                  gap: tokens.spacingVerticalM,
                }}
              >
                <div>
                  <Text weight="semibold">Topic:</Text> <Text>{brief.topic}</Text>
                </div>
                <div>
                  <Text weight="semibold">Audience:</Text> <Text>{brief.audience}</Text>
                </div>
                <div>
                  <Text weight="semibold">Duration:</Text>{' '}
                  <Text>{planSpec.targetDurationMinutes} minutes</Text>
                </div>
                <div>
                  <Text weight="semibold">Pacing:</Text> <Text>{planSpec.pacing}</Text>
                </div>
                <div>
                  <Text weight="semibold">Density:</Text> <Text>{planSpec.density}</Text>
                </div>
                <div>
                  <Text weight="semibold">Aspect:</Text> <Text>{brief.aspect}</Text>
                </div>
                <div>
                  <Text weight="semibold">Image Provider:</Text>{' '}
                  <Text>{selectedImageProvider || 'Default'}</Text>
                </div>
              </div>
            </Card>

            <Card className={styles.section}>
              <div style={{ marginBottom: tokens.spacingVerticalL }}>
                <Field label="Profile">
                  <Dropdown
                    value={selectedProfile}
                    onOptionSelect={(_, data) => {
                      setSelectedProfile(data.optionValue as string);
                      setPreflightReport(null); // Clear report when profile changes
                    }}
                  >
                    <Option value="Free-Only">Free-Only (Ollama + Windows TTS + Stock)</Option>
                    <Option value="Balanced Mix">Balanced Mix (Pro with fallbacks)</Option>
                    <Option value="Pro-Max">Pro-Max (OpenAI + ElevenLabs + Cloud)</Option>
                  </Dropdown>
                </Field>
              </div>

              <PreflightPanel
                profile={selectedProfile}
                report={preflightReport}
                isRunning={isRunningPreflight}
                onRunPreflight={handleRunPreflight}
                onApplySafeDefaults={handleApplySafeDefaults}
              />

              {preflightReport && !preflightReport.ok && (
                <div style={{ marginTop: tokens.spacingVerticalL }}>
                  <Checkbox
                    checked={overridePreflightGate}
                    onChange={(_, data) => handleOverrideToggle(data.checked === true)}
                    label={
                      <Tooltip
                        content="Some preflight checks failed, but you can still proceed at your own risk"
                        relationship="label"
                      >
                        <Text>Override and proceed anyway</Text>
                      </Tooltip>
                    }
                  />
                </div>
              )}
            </Card>
          </>
        )}

        <div className={styles.actions}>
          {currentStep > 1 && (
            <Button onClick={() => setCurrentStep(currentStep - 1)}>Previous</Button>
          )}
          {currentStep < 3 ? (
            <Tooltip
              content={
                currentStep === 1 && !isBriefValid
                  ? 'Please enter a valid topic (3-100 characters)'
                  : ''
              }
              relationship="label"
            >
              <Button
                appearance="primary"
                onClick={() => setCurrentStep(currentStep + 1)}
                disabled={currentStep === 1 && !isBriefValid}
              >
                Next
              </Button>
            </Tooltip>
          ) : (
            <Tooltip
              content={
                !preflightReport
                  ? 'Run preflight check before generating'
                  : !preflightReport.ok && !overridePreflightGate
                    ? 'Preflight checks failed. Enable override to proceed anyway.'
                    : ''
              }
              relationship="label"
            >
              <Button
                appearance="primary"
                icon={<Play24Regular />}
                onClick={handleGenerate}
                disabled={
                  generating ||
                  !preflightReport ||
                  (!preflightReport.ok && !overridePreflightGate) ||
                  isOfflineDisabled
                }
                aria-label={
                  isOfflineDisabled ? 'Generate Video (Backend offline)' : 'Generate Video'
                }
              >
                {generating ? 'Generating...' : 'Generate Video'}
              </Button>
            </Tooltip>
          )}
        </div>
      </div>

      <PreflightOverrideDialog
        open={showOverrideDialog}
        onOpenChange={setShowOverrideDialog}
        errors={
          preflightReport?.stages
            .filter((stage) => stage.status === 'fail')
            .map((stage) => stage.message) || []
        }
        onConfirm={() => {
          setOverridePreflightGate(true);
          setShowOverrideDialog(false);
        }}
      />
    </div>
  );
}
