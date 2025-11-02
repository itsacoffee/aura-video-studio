/**
 * Custom Template Builder Component
 * Form for creating and editing custom video templates with script structure,
 * video settings, LLM configuration, and visual preferences
 */

import {
  makeStyles,
  tokens,
  Text,
  Title3,
  Button,
  Input,
  Textarea,
  Field,
  Spinner,
  MessageBar,
  MessageBarBody,
  Card,
  Label,
  Divider,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
  Dropdown,
  Option,
  SpinButton,
  Checkbox,
} from '@fluentui/react-components';
import { Add24Regular, Delete24Regular, Save24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import type {
  CreateCustomTemplateRequest,
  UpdateCustomTemplateRequest,
  CustomVideoTemplate,
  ScriptSection,
} from '../../types/templates';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    maxWidth: '1200px',
    margin: '0 auto',
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  formRow: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  scriptSection: {
    padding: tokens.spacingVerticalM,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  sectionHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  messageBar: {
    marginBottom: tokens.spacingVerticalM,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalXL,
  },
});

export interface CustomTemplateBuilderProps {
  initialTemplate?: CustomVideoTemplate;
  onSave: (template: CreateCustomTemplateRequest | UpdateCustomTemplateRequest) => Promise<void>;
  onCancel: () => void;
}

export function CustomTemplateBuilder({
  initialTemplate,
  onSave,
  onCancel,
}: CustomTemplateBuilderProps) {
  const styles = useStyles();
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Basic information
  const [name, setName] = useState(initialTemplate?.name || '');
  const [description, setDescription] = useState(initialTemplate?.description || '');
  const [category, setCategory] = useState(initialTemplate?.category || '');
  const [tags, setTags] = useState(initialTemplate?.tags.join(', ') || '');

  // Script structure
  const [scriptSections, setScriptSections] = useState<ScriptSection[]>(
    initialTemplate?.scriptStructure.sections || [
      {
        id: crypto.randomUUID(),
        name: 'Intro',
        description: 'Introduction to the video',
        order: 0,
        isRequired: true,
        isOptional: false,
        tone: 'excited',
        style: 'engaging',
        minDuration: 5,
        maxDuration: 15,
      },
    ]
  );

  // Video structure
  const [typicalDuration, setTypicalDuration] = useState(
    initialTemplate?.videoStructure.typicalDuration || 60
  );
  const [pacing, setPacing] = useState(initialTemplate?.videoStructure.pacing || 'medium');
  const [sceneCount, setSceneCount] = useState(initialTemplate?.videoStructure.sceneCount || 5);
  const [transitionStyle, setTransitionStyle] = useState(
    initialTemplate?.videoStructure.transitionStyle || 'smooth'
  );
  const [useBRoll, setUseBRoll] = useState(initialTemplate?.videoStructure.useBRoll ?? true);
  const [musicStyle, setMusicStyle] = useState(
    initialTemplate?.videoStructure.musicStyle || 'background'
  );
  const [musicVolume, setMusicVolume] = useState(
    initialTemplate?.videoStructure.musicVolume || 0.3
  );

  // LLM configuration
  const [defaultTemperature, setDefaultTemperature] = useState(
    initialTemplate?.llmPipeline.defaultTemperature || 0.7
  );
  const [defaultMaxTokens, setDefaultMaxTokens] = useState(
    initialTemplate?.llmPipeline.defaultMaxTokens || 500
  );
  const [defaultModel, setDefaultModel] = useState(
    initialTemplate?.llmPipeline.defaultModel || 'gpt-4'
  );
  const [keywordsToEmphasize, setKeywordsToEmphasize] = useState(
    initialTemplate?.llmPipeline.keywordsToEmphasize.join(', ') || ''
  );
  const [keywordsToAvoid, setKeywordsToAvoid] = useState(
    initialTemplate?.llmPipeline.keywordsToAvoid.join(', ') || ''
  );

  // Visual preferences
  const [colorScheme, setColorScheme] = useState(
    initialTemplate?.visualPrefs.colorScheme || 'vibrant'
  );
  const [textOverlayStyle, setTextOverlayStyle] = useState(
    initialTemplate?.visualPrefs.textOverlayStyle || 'modern'
  );
  const [transitionPreference, setTransitionPreference] = useState(
    initialTemplate?.visualPrefs.transitionPreference || 'crossfade'
  );

  const handleAddSection = () => {
    setScriptSections([
      ...scriptSections,
      {
        id: crypto.randomUUID(),
        name: '',
        description: '',
        order: scriptSections.length,
        isRequired: false,
        isOptional: true,
        tone: 'neutral',
        style: 'informative',
        minDuration: 10,
        maxDuration: 30,
      },
    ]);
  };

  const handleRemoveSection = (id: string) => {
    setScriptSections(scriptSections.filter((s) => s.id !== id));
  };

  const handleSectionChange = (id: string, field: keyof ScriptSection, value: unknown) => {
    setScriptSections(scriptSections.map((s) => (s.id === id ? { ...s, [field]: value } : s)));
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setError(null);

      if (!name.trim()) {
        setError('Template name is required');
        return;
      }

      if (!category.trim()) {
        setError('Category is required');
        return;
      }

      const templateData: CreateCustomTemplateRequest = {
        name: name.trim(),
        description: description.trim(),
        category: category.trim(),
        tags: tags
          .split(',')
          .map((t) => t.trim())
          .filter((t) => t.length > 0),
        scriptStructure: {
          sections: scriptSections,
        },
        videoStructure: {
          typicalDuration,
          pacing,
          sceneCount,
          transitionStyle,
          useBRoll,
          musicStyle,
          musicVolume,
        },
        llmPipeline: {
          sectionPrompts: [],
          defaultTemperature,
          defaultMaxTokens,
          defaultModel,
          keywordsToEmphasize: keywordsToEmphasize
            .split(',')
            .map((k) => k.trim())
            .filter((k) => k.length > 0),
          keywordsToAvoid: keywordsToAvoid
            .split(',')
            .map((k) => k.trim())
            .filter((k) => k.length > 0),
        },
        visualPrefs: {
          imageGenerationPromptTemplate: '',
          colorScheme,
          aestheticGuidelines: [],
          textOverlayStyle,
          transitionPreference,
          customStyles: {},
        },
      };

      await onSave(templateData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save template');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className={styles.container}>
      <Title3>{initialTemplate ? 'Edit Custom Template' : 'Create Custom Template'}</Title3>

      {error && (
        <MessageBar intent="error" className={styles.messageBar}>
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      <Accordion multiple collapsible defaultOpenItems={['basic', 'script']}>
        {/* Basic Information */}
        <AccordionItem value="basic">
          <AccordionHeader>Basic Information</AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <Field label="Template Name" required>
                <Input value={name} onChange={(_, data) => setName(data.value)} />
              </Field>

              <Field label="Description">
                <Textarea
                  value={description}
                  onChange={(_, data) => setDescription(data.value)}
                  rows={3}
                />
              </Field>

              <div className={styles.formRow}>
                <Field label="Category" required>
                  <Input value={category} onChange={(_, data) => setCategory(data.value)} />
                </Field>

                <Field label="Tags (comma-separated)">
                  <Input value={tags} onChange={(_, data) => setTags(data.value)} />
                </Field>
              </div>
            </div>
          </AccordionPanel>
        </AccordionItem>

        {/* Script Structure */}
        <AccordionItem value="script">
          <AccordionHeader>Script Structure</AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <div
                style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
              >
                <Label>Script Sections</Label>
                <Button
                  appearance="secondary"
                  icon={<Add24Regular />}
                  onClick={handleAddSection}
                  size="small"
                >
                  Add Section
                </Button>
              </div>

              {scriptSections.map((section, index) => (
                <Card key={section.id} className={styles.scriptSection}>
                  <div className={styles.sectionHeader}>
                    <Text weight="semibold">Section {index + 1}</Text>
                    {scriptSections.length > 1 && (
                      <Button
                        appearance="subtle"
                        icon={<Delete24Regular />}
                        onClick={() => handleRemoveSection(section.id)}
                        size="small"
                      />
                    )}
                  </div>

                  <div className={styles.formRow}>
                    <Field label="Name">
                      <Input
                        value={section.name}
                        onChange={(_, data) => handleSectionChange(section.id, 'name', data.value)}
                      />
                    </Field>

                    <Field label="Tone">
                      <Dropdown
                        value={section.tone}
                        onOptionSelect={(_, data) =>
                          handleSectionChange(section.id, 'tone', data.optionValue)
                        }
                      >
                        <Option value="neutral">Neutral</Option>
                        <Option value="excited">Excited</Option>
                        <Option value="serious">Serious</Option>
                        <Option value="humorous">Humorous</Option>
                        <Option value="informative">Informative</Option>
                      </Dropdown>
                    </Field>
                  </div>

                  <Field label="Description">
                    <Textarea
                      value={section.description}
                      onChange={(_, data) =>
                        handleSectionChange(section.id, 'description', data.value)
                      }
                      rows={2}
                    />
                  </Field>

                  <div className={styles.formRow}>
                    <Field label="Min Duration (seconds)">
                      <SpinButton
                        value={section.minDuration}
                        onChange={(_, data) =>
                          handleSectionChange(section.id, 'minDuration', data.value || 5)
                        }
                        min={1}
                        max={300}
                      />
                    </Field>

                    <Field label="Max Duration (seconds)">
                      <SpinButton
                        value={section.maxDuration}
                        onChange={(_, data) =>
                          handleSectionChange(section.id, 'maxDuration', data.value || 30)
                        }
                        min={1}
                        max={600}
                      />
                    </Field>
                  </div>

                  <Checkbox
                    label="Required Section"
                    checked={section.isRequired}
                    onChange={(_, data) =>
                      handleSectionChange(section.id, 'isRequired', data.checked)
                    }
                  />
                </Card>
              ))}
            </div>
          </AccordionPanel>
        </AccordionItem>

        {/* Video Structure */}
        <AccordionItem value="video">
          <AccordionHeader>Video Structure</AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <div className={styles.formRow}>
                <Field label="Typical Duration (seconds)">
                  <SpinButton
                    value={typicalDuration}
                    onChange={(_, data) => setTypicalDuration(data.value || 60)}
                    min={10}
                    max={3600}
                  />
                </Field>

                <Field label="Scene Count">
                  <SpinButton
                    value={sceneCount}
                    onChange={(_, data) => setSceneCount(data.value || 5)}
                    min={1}
                    max={50}
                  />
                </Field>
              </div>

              <div className={styles.formRow}>
                <Field label="Pacing">
                  <Dropdown
                    value={pacing}
                    onOptionSelect={(_, data) => setPacing(data.optionValue || 'medium')}
                  >
                    <Option value="slow">Slow</Option>
                    <Option value="medium">Medium</Option>
                    <Option value="fast">Fast</Option>
                  </Dropdown>
                </Field>

                <Field label="Transition Style">
                  <Dropdown
                    value={transitionStyle}
                    onOptionSelect={(_, data) => setTransitionStyle(data.optionValue || 'smooth')}
                  >
                    <Option value="smooth">Smooth</Option>
                    <Option value="cut">Cut</Option>
                    <Option value="fade">Fade</Option>
                    <Option value="slide">Slide</Option>
                  </Dropdown>
                </Field>
              </div>

              <div className={styles.formRow}>
                <Field label="Music Style">
                  <Input value={musicStyle} onChange={(_, data) => setMusicStyle(data.value)} />
                </Field>

                <Field label="Music Volume">
                  <SpinButton
                    value={musicVolume}
                    onChange={(_, data) => setMusicVolume(data.value || 0.3)}
                    min={0}
                    max={1}
                    step={0.1}
                  />
                </Field>
              </div>

              <Checkbox
                label="Use B-Roll Footage"
                checked={useBRoll}
                onChange={(_, data) => setUseBRoll(data.checked === true)}
              />
            </div>
          </AccordionPanel>
        </AccordionItem>

        {/* LLM Configuration */}
        <AccordionItem value="llm">
          <AccordionHeader>LLM Configuration</AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <div className={styles.formRow}>
                <Field label="Default Model">
                  <Dropdown
                    value={defaultModel}
                    onOptionSelect={(_, data) => setDefaultModel(data.optionValue || 'gpt-4')}
                  >
                    <Option value="gpt-4">GPT-4</Option>
                    <Option value="gpt-3.5-turbo">GPT-3.5 Turbo</Option>
                    <Option value="claude-3">Claude 3</Option>
                  </Dropdown>
                </Field>

                <Field label="Temperature">
                  <SpinButton
                    value={defaultTemperature}
                    onChange={(_, data) => setDefaultTemperature(data.value || 0.7)}
                    min={0}
                    max={2}
                    step={0.1}
                  />
                </Field>
              </div>

              <Field label="Max Tokens">
                <SpinButton
                  value={defaultMaxTokens}
                  onChange={(_, data) => setDefaultMaxTokens(data.value || 500)}
                  min={50}
                  max={4000}
                  step={50}
                />
              </Field>

              <Field label="Keywords to Emphasize (comma-separated)">
                <Input
                  value={keywordsToEmphasize}
                  onChange={(_, data) => setKeywordsToEmphasize(data.value)}
                />
              </Field>

              <Field label="Keywords to Avoid (comma-separated)">
                <Input
                  value={keywordsToAvoid}
                  onChange={(_, data) => setKeywordsToAvoid(data.value)}
                />
              </Field>
            </div>
          </AccordionPanel>
        </AccordionItem>

        {/* Visual Preferences */}
        <AccordionItem value="visual">
          <AccordionHeader>Visual Preferences</AccordionHeader>
          <AccordionPanel>
            <div className={styles.section}>
              <div className={styles.formRow}>
                <Field label="Color Scheme">
                  <Dropdown
                    value={colorScheme}
                    onOptionSelect={(_, data) => setColorScheme(data.optionValue || 'vibrant')}
                  >
                    <Option value="vibrant">Vibrant</Option>
                    <Option value="muted">Muted</Option>
                    <Option value="pastel">Pastel</Option>
                    <Option value="dark">Dark</Option>
                    <Option value="light">Light</Option>
                  </Dropdown>
                </Field>

                <Field label="Text Overlay Style">
                  <Dropdown
                    value={textOverlayStyle}
                    onOptionSelect={(_, data) => setTextOverlayStyle(data.optionValue || 'modern')}
                  >
                    <Option value="modern">Modern</Option>
                    <Option value="classic">Classic</Option>
                    <Option value="bold">Bold</Option>
                    <Option value="minimal">Minimal</Option>
                  </Dropdown>
                </Field>
              </div>

              <Field label="Transition Preference">
                <Dropdown
                  value={transitionPreference}
                  onOptionSelect={(_, data) =>
                    setTransitionPreference(data.optionValue || 'crossfade')
                  }
                >
                  <Option value="crossfade">Crossfade</Option>
                  <Option value="dissolve">Dissolve</Option>
                  <Option value="wipe">Wipe</Option>
                  <Option value="zoom">Zoom</Option>
                </Dropdown>
              </Field>
            </div>
          </AccordionPanel>
        </AccordionItem>
      </Accordion>

      <Divider />

      <div className={styles.actions}>
        <Button appearance="secondary" onClick={onCancel} disabled={saving}>
          Cancel
        </Button>
        <Button
          appearance="primary"
          icon={saving ? <Spinner size="tiny" /> : <Save24Regular />}
          onClick={handleSave}
          disabled={saving}
        >
          {saving ? 'Saving...' : 'Save Template'}
        </Button>
      </div>
    </div>
  );
}
