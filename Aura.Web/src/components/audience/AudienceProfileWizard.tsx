import {
  Button,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Field,
  Input,
  Dropdown,
  Option,
  Textarea,
  Checkbox,
  ProgressBar,
  Card,
  CardHeader,
  Text,
  tokens,
} from '@fluentui/react-components';
import { PersonRegular, SaveRegular, DismissRegular } from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import type { FC } from 'react';
import type { AudienceProfileDto } from '../../types/api-v1';
import {
  EducationLevels,
  ExpertiseLevels,
  TechnicalComfortLevels,
  LearningStyles,
} from '../../types/api-v1';

interface AudienceProfileWizardProps {
  open: boolean;
  onClose: () => void;
  onSave: (profile: AudienceProfileDto) => void;
  initialProfile?: AudienceProfileDto | null;
  templates?: AudienceProfileDto[];
}

type WizardStep = 'template' | 'demographics' | 'psychographics' | 'preferences' | 'review';

const AudienceProfileWizard: FC<AudienceProfileWizardProps> = ({
  open,
  onClose,
  onSave,
  initialProfile,
  templates = [],
}) => {
  const [currentStep, setCurrentStep] = useState<WizardStep>('template');
  const [profile, setProfile] = useState<Partial<AudienceProfileDto>>(
    initialProfile || {
      name: '',
      description: '',
      interests: [],
      painPoints: [],
      motivations: [],
      tags: [],
      isTemplate: false,
      version: 1,
    }
  );

  const steps: WizardStep[] = [
    'template',
    'demographics',
    'psychographics',
    'preferences',
    'review',
  ];
  const currentStepIndex = steps.indexOf(currentStep);
  const progress = ((currentStepIndex + 1) / steps.length) * 100;

  const handleNext = useCallback(() => {
    const nextIndex = currentStepIndex + 1;
    if (nextIndex < steps.length) {
      setCurrentStep(steps[nextIndex]);
    }
  }, [currentStepIndex, steps]);

  const handlePrevious = useCallback(() => {
    const prevIndex = currentStepIndex - 1;
    if (prevIndex >= 0) {
      setCurrentStep(steps[prevIndex]);
    }
  }, [currentStepIndex, steps]);

  const handleSave = useCallback(() => {
    const fullProfile: AudienceProfileDto = {
      id: profile.id || null,
      name: profile.name || 'Untitled Profile',
      description: profile.description || null,
      ageRange: profile.ageRange || null,
      educationLevel: profile.educationLevel || null,
      profession: profile.profession || null,
      industry: profile.industry || null,
      expertiseLevel: profile.expertiseLevel || null,
      incomeBracket: profile.incomeBracket || null,
      geographicRegion: profile.geographicRegion || null,
      languageFluency: profile.languageFluency || null,
      interests: profile.interests || [],
      painPoints: profile.painPoints || [],
      motivations: profile.motivations || [],
      culturalBackground: profile.culturalBackground || null,
      preferredLearningStyle: profile.preferredLearningStyle || null,
      attentionSpan: profile.attentionSpan || null,
      technicalComfort: profile.technicalComfort || null,
      accessibilityNeeds: profile.accessibilityNeeds || null,
      isTemplate: profile.isTemplate || false,
      tags: profile.tags || [],
      version: profile.version || 1,
      createdAt: profile.createdAt || null,
      updatedAt: profile.updatedAt || null,
    };
    onSave(fullProfile);
  }, [profile, onSave]);

  const handleTemplateSelect = useCallback(
    (template: AudienceProfileDto) => {
      setProfile({
        ...template,
        id: null,
        name: `${template.name} (Copy)`,
        isTemplate: false,
      });
      handleNext();
    },
    [handleNext]
  );

  const updateProfile = useCallback((updates: Partial<AudienceProfileDto>) => {
    setProfile((prev) => ({ ...prev, ...updates }));
  }, []);

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface style={{ maxWidth: '800px', minHeight: '600px' }}>
        <DialogTitle>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <PersonRegular fontSize={24} />
            Create Audience Profile
          </div>
        </DialogTitle>
        <DialogBody>
          <DialogContent>
            <div style={{ marginBottom: '16px' }}>
              <ProgressBar value={progress} />
              <Text size={200} style={{ marginTop: '4px', display: 'block' }}>
                Step {currentStepIndex + 1} of {steps.length}
              </Text>
            </div>

            {currentStep === 'template' && (
              <TemplateStep
                templates={templates}
                onSelect={handleTemplateSelect}
                onSkip={handleNext}
              />
            )}

            {currentStep === 'demographics' && (
              <DemographicsStep profile={profile} updateProfile={updateProfile} />
            )}

            {currentStep === 'psychographics' && (
              <PsychographicsStep profile={profile} updateProfile={updateProfile} />
            )}

            {currentStep === 'preferences' && (
              <PreferencesStep profile={profile} updateProfile={updateProfile} />
            )}

            {currentStep === 'review' && <ReviewStep profile={profile} />}
          </DialogContent>
        </DialogBody>
        <DialogActions>
          <Button appearance="secondary" onClick={onClose} icon={<DismissRegular />}>
            Cancel
          </Button>
          {currentStepIndex > 0 && (
            <Button appearance="secondary" onClick={handlePrevious}>
              Previous
            </Button>
          )}
          {currentStepIndex < steps.length - 1 && (
            <Button appearance="primary" onClick={handleNext}>
              Next
            </Button>
          )}
          {currentStepIndex === steps.length - 1 && (
            <Button appearance="primary" onClick={handleSave} icon={<SaveRegular />}>
              Save Profile
            </Button>
          )}
        </DialogActions>
      </DialogSurface>
    </Dialog>
  );
};

const TemplateStep: FC<{
  templates: AudienceProfileDto[];
  onSelect: (template: AudienceProfileDto) => void;
  onSkip: () => void;
}> = ({ templates, onSelect, onSkip }) => (
  <div>
    <Text size={500} weight="semibold" style={{ display: 'block', marginBottom: '16px' }}>
      Start with a Template
    </Text>
    <Text
      size={300}
      style={{ display: 'block', marginBottom: '24px', color: tokens.colorNeutralForeground3 }}
    >
      Choose a preset template to get started quickly, or create from scratch
    </Text>
    <div
      style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
        gap: '16px',
      }}
    >
      {templates.map((template) => (
        <Card key={template.id} onClick={() => onSelect(template)} style={{ cursor: 'pointer' }}>
          <CardHeader
            header={<Text weight="semibold">{template.name}</Text>}
            description={<Text size={200}>{template.description}</Text>}
          />
        </Card>
      ))}
      <Card onClick={onSkip} style={{ cursor: 'pointer' }}>
        <CardHeader
          header={<Text weight="semibold">Start from Scratch</Text>}
          description={<Text size={200}>Create a custom profile</Text>}
        />
      </Card>
    </div>
  </div>
);

const DemographicsStep: FC<{
  profile: Partial<AudienceProfileDto>;
  updateProfile: (updates: Partial<AudienceProfileDto>) => void;
}> = ({ profile, updateProfile }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
    <Text size={500} weight="semibold">
      Demographics
    </Text>

    <Field label="Profile Name" required>
      <Input
        value={profile.name || ''}
        onChange={(_, data) => updateProfile({ name: data.value })}
        placeholder="e.g., Tech Professionals"
      />
    </Field>

    <Field label="Description">
      <Textarea
        value={profile.description || ''}
        onChange={(_, data) => updateProfile({ description: data.value })}
        placeholder="Brief description of this audience..."
        rows={3}
      />
    </Field>

    <Field label="Education Level">
      <Dropdown
        value={profile.educationLevel || ''}
        onOptionSelect={(_, data) => updateProfile({ educationLevel: data.optionValue || null })}
        placeholder="Select education level"
      >
        {EducationLevels.map((level) => (
          <Option key={level} value={level}>
            {level}
          </Option>
        ))}
      </Dropdown>
    </Field>

    <Field label="Expertise Level">
      <Dropdown
        value={profile.expertiseLevel || ''}
        onOptionSelect={(_, data) => updateProfile({ expertiseLevel: data.optionValue || null })}
        placeholder="Select expertise level"
      >
        {ExpertiseLevels.map((level) => (
          <Option key={level} value={level}>
            {level}
          </Option>
        ))}
      </Dropdown>
    </Field>

    <Field label="Profession">
      <Input
        value={profile.profession || ''}
        onChange={(_, data) => updateProfile({ profession: data.value })}
        placeholder="e.g., Software Developer"
      />
    </Field>

    <Field label="Industry">
      <Input
        value={profile.industry || ''}
        onChange={(_, data) => updateProfile({ industry: data.value })}
        placeholder="e.g., Technology"
      />
    </Field>
  </div>
);

const PsychographicsStep: FC<{
  profile: Partial<AudienceProfileDto>;
  updateProfile: (updates: Partial<AudienceProfileDto>) => void;
}> = ({ profile, updateProfile }) => {
  const [currentInterest, setCurrentInterest] = useState('');
  const [currentPainPoint, setCurrentPainPoint] = useState('');
  const [currentMotivation, setCurrentMotivation] = useState('');

  const addItem = useCallback(
    (field: 'interests' | 'painPoints' | 'motivations', value: string) => {
      if (!value.trim()) return;
      const current = profile[field] || [];
      updateProfile({ [field]: [...current, value.trim()] });
    },
    [profile, updateProfile]
  );

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      <Text size={500} weight="semibold">
        Psychographics
      </Text>

      <Field label="Interests">
        <div style={{ display: 'flex', gap: '8px' }}>
          <Input
            value={currentInterest}
            onChange={(_, data) => setCurrentInterest(data.value)}
            placeholder="Add an interest..."
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                addItem('interests', currentInterest);
                setCurrentInterest('');
              }
            }}
          />
          <Button
            onClick={() => {
              addItem('interests', currentInterest);
              setCurrentInterest('');
            }}
          >
            Add
          </Button>
        </div>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginTop: '8px' }}>
          {(profile.interests || []).map((interest, index) => (
            <div
              key={index}
              style={{
                padding: '4px 8px',
                background: tokens.colorNeutralBackground3,
                borderRadius: '4px',
                fontSize: '14px',
              }}
            >
              {interest}
            </div>
          ))}
        </div>
      </Field>

      <Field label="Pain Points (max 500 chars each)">
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          <Textarea
            value={currentPainPoint}
            onChange={(_, data) => setCurrentPainPoint(data.value)}
            placeholder="Describe a challenge or pain point..."
            rows={2}
            maxLength={500}
          />
          <Button
            size="small"
            onClick={() => {
              addItem('painPoints', currentPainPoint);
              setCurrentPainPoint('');
            }}
          >
            Add Pain Point
          </Button>
        </div>
        {(profile.painPoints || []).map((painPoint, index) => (
          <Text key={index} size={200} style={{ display: 'block', marginTop: '8px' }}>
            • {painPoint}
          </Text>
        ))}
      </Field>

      <Field label="Motivations (max 500 chars each)">
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          <Textarea
            value={currentMotivation}
            onChange={(_, data) => setCurrentMotivation(data.value)}
            placeholder="What motivates this audience..."
            rows={2}
            maxLength={500}
          />
          <Button
            size="small"
            onClick={() => {
              addItem('motivations', currentMotivation);
              setCurrentMotivation('');
            }}
          >
            Add Motivation
          </Button>
        </div>
        {(profile.motivations || []).map((motivation, index) => (
          <Text key={index} size={200} style={{ display: 'block', marginTop: '8px' }}>
            • {motivation}
          </Text>
        ))}
      </Field>
    </div>
  );
};

const PreferencesStep: FC<{
  profile: Partial<AudienceProfileDto>;
  updateProfile: (updates: Partial<AudienceProfileDto>) => void;
}> = ({ profile, updateProfile }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
    <Text size={500} weight="semibold">
      Learning Preferences
    </Text>

    <Field label="Technical Comfort Level">
      <Dropdown
        value={profile.technicalComfort || ''}
        onOptionSelect={(_, data) => updateProfile({ technicalComfort: data.optionValue || null })}
        placeholder="Select technical comfort"
      >
        {TechnicalComfortLevels.map((level) => (
          <Option key={level} value={level}>
            {level}
          </Option>
        ))}
      </Dropdown>
    </Field>

    <Field label="Preferred Learning Style">
      <Dropdown
        value={profile.preferredLearningStyle || ''}
        onOptionSelect={(_, data) =>
          updateProfile({ preferredLearningStyle: data.optionValue || null })
        }
        placeholder="Select learning style"
      >
        {LearningStyles.map((style) => (
          <Option key={style} value={style}>
            {style}
          </Option>
        ))}
      </Dropdown>
    </Field>

    <Field label="Accessibility Needs">
      <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
        <Checkbox
          checked={profile.accessibilityNeeds?.requiresCaptions || false}
          onChange={(_, data) =>
            updateProfile({
              accessibilityNeeds: {
                ...(profile.accessibilityNeeds || {
                  requiresCaptions: false,
                  requiresAudioDescriptions: false,
                  requiresHighContrast: false,
                  requiresSimplifiedLanguage: false,
                  requiresLargeText: false,
                }),
                requiresCaptions: !!data.checked,
              },
            })
          }
          label="Requires Captions"
        />
        <Checkbox
          checked={profile.accessibilityNeeds?.requiresLargeText || false}
          onChange={(_, data) =>
            updateProfile({
              accessibilityNeeds: {
                ...(profile.accessibilityNeeds || {
                  requiresCaptions: false,
                  requiresAudioDescriptions: false,
                  requiresHighContrast: false,
                  requiresSimplifiedLanguage: false,
                  requiresLargeText: false,
                }),
                requiresLargeText: !!data.checked,
              },
            })
          }
          label="Requires Large Text"
        />
        <Checkbox
          checked={profile.accessibilityNeeds?.requiresSimplifiedLanguage || false}
          onChange={(_, data) =>
            updateProfile({
              accessibilityNeeds: {
                ...(profile.accessibilityNeeds || {
                  requiresCaptions: false,
                  requiresAudioDescriptions: false,
                  requiresHighContrast: false,
                  requiresSimplifiedLanguage: false,
                  requiresLargeText: false,
                }),
                requiresSimplifiedLanguage: !!data.checked,
              },
            })
          }
          label="Requires Simplified Language"
        />
      </div>
    </Field>
  </div>
);

const ReviewStep: FC<{ profile: Partial<AudienceProfileDto> }> = ({ profile }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
    <Text size={500} weight="semibold">
      Review Profile
    </Text>

    <div style={{ display: 'grid', gridTemplateColumns: '150px 1fr', gap: '12px' }}>
      <Text weight="semibold">Name:</Text>
      <Text>{profile.name || 'N/A'}</Text>

      <Text weight="semibold">Education:</Text>
      <Text>{profile.educationLevel || 'N/A'}</Text>

      <Text weight="semibold">Expertise:</Text>
      <Text>{profile.expertiseLevel || 'N/A'}</Text>

      <Text weight="semibold">Technical Comfort:</Text>
      <Text>{profile.technicalComfort || 'N/A'}</Text>

      <Text weight="semibold">Interests:</Text>
      <Text>{(profile.interests || []).join(', ') || 'None'}</Text>

      <Text weight="semibold">Pain Points:</Text>
      <div>
        {(profile.painPoints || []).map((point, index) => (
          <Text key={index} size={200} style={{ display: 'block', marginBottom: '4px' }}>
            • {point}
          </Text>
        ))}
      </div>

      <Text weight="semibold">Motivations:</Text>
      <div>
        {(profile.motivations || []).map((motivation, index) => (
          <Text key={index} size={200} style={{ display: 'block', marginBottom: '4px' }}>
            • {motivation}
          </Text>
        ))}
      </div>
    </div>
  </div>
);

export default AudienceProfileWizard;
