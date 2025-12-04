/**
 * TemplateConfigurator - Form for configuring template variables
 * Displays template structure preview and allows users to customize before generating
 */

import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Card,
  Field,
  Input,
  Textarea,
  SpinButton,
  Dropdown,
  Option,
  Badge,
  Spinner,
} from '@fluentui/react-components';
import {
  ArrowLeft24Regular,
  Play24Regular,
  Eye24Regular,
  Checkmark24Regular,
} from '@fluentui/react-icons';
import React, { useState, useMemo, useCallback } from 'react';
import { apiUrl } from '../../config/api';
import type {
  VideoTemplate,
  TemplatedBrief,
  ScriptPreviewResponse,
  ValidationResult,
} from '../../types/videoTemplates';
import { formatDuration, getCategoryColor, TemplateIconMap } from '../../types/videoTemplates';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingHorizontalL,
    maxWidth: '900px',
    margin: '0 auto',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  templateInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  iconContainer: {
    width: '48px',
    height: '48px',
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: '24px',
    color: 'white',
  },
  section: {
    padding: tokens.spacingVerticalL,
  },
  sectionTitle: {
    marginBottom: tokens.spacingVerticalM,
  },
  fieldGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  structurePreview: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  sectionItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  sectionNumber: {
    width: '24px',
    height: '24px',
    borderRadius: '50%',
    backgroundColor: tokens.colorBrandBackground,
    color: 'white',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
  },
  sectionInfo: {
    flex: 1,
  },
  sectionName: {
    fontWeight: tokens.fontWeightSemibold,
  },
  sectionPurpose: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  sectionDuration: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  estimatedDuration: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalL,
  },
  previewSection: {
    marginTop: tokens.spacingVerticalL,
  },
  previewContent: {
    backgroundColor: tokens.colorNeutralBackground2,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    maxHeight: '400px',
    overflow: 'auto',
    whiteSpace: 'pre-wrap',
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
  },
  errorMessage: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalXS,
  },
  validationErrors: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    marginTop: tokens.spacingVerticalM,
  },
});

interface TemplateConfiguratorProps {
  template: VideoTemplate;
  onBack: () => void;
  onGenerate: (brief: TemplatedBrief) => void;
}

export function TemplateConfigurator({ template, onBack, onGenerate }: TemplateConfiguratorProps) {
  const styles = useStyles();
  const [variableValues, setVariableValues] = useState<Record<string, string>>(() => {
    // Initialize with default values
    const defaults: Record<string, string> = {};
    template.variables.forEach((v) => {
      if (v.defaultValue) {
        defaults[v.name] = v.defaultValue;
      }
    });
    return defaults;
  });
  const [loading, setLoading] = useState(false);
  const [previewing, setPreviewing] = useState(false);
  const [preview, setPreview] = useState<ScriptPreviewResponse | null>(null);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);

  // Calculate estimated duration based on template and variable values
  const estimatedDuration = useMemo(() => {
    let totalSeconds = 0;

    template.structure.sections.forEach((section) => {
      if (section.isRepeatable && section.repeatCountVariable) {
        const count = parseInt(variableValues[section.repeatCountVariable] || '1', 10);
        totalSeconds += section.suggestedDurationSeconds * count;
      } else {
        totalSeconds += section.suggestedDurationSeconds;
      }
    });

    return totalSeconds;
  }, [template, variableValues]);

  // Handle variable change
  const handleVariableChange = useCallback((name: string, value: string) => {
    setVariableValues((prev) => ({
      ...prev,
      [name]: value,
    }));
    setValidationErrors([]);
  }, []);

  // Validate variables
  const validateVariables = useCallback(async (): Promise<boolean> => {
    try {
      const response = await fetch(apiUrl(`/api/video-templates/${template.id}/validate`), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ variableValues }),
      });

      const result: ValidationResult = await response.json();
      setValidationErrors(result.errors);
      return result.isValid;
    } catch {
      setValidationErrors(['Failed to validate variables']);
      return false;
    }
  }, [template.id, variableValues]);

  // Handle preview
  const handlePreview = useCallback(async () => {
    const isValid = await validateVariables();
    if (!isValid) return;

    setPreviewing(true);
    try {
      const response = await fetch(apiUrl(`/api/video-templates/${template.id}/preview-script`), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ variableValues }),
      });

      if (!response.ok) {
        throw new Error('Failed to generate preview');
      }

      const result: ScriptPreviewResponse = await response.json();
      setPreview(result);
    } catch (err) {
      setValidationErrors([err instanceof Error ? err.message : 'Failed to generate preview']);
    } finally {
      setPreviewing(false);
    }
  }, [template.id, variableValues, validateVariables]);

  // Handle generate
  const handleGenerate = useCallback(async () => {
    const isValid = await validateVariables();
    if (!isValid) return;

    setLoading(true);
    try {
      const response = await fetch(apiUrl(`/api/video-templates/${template.id}/apply`), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ variableValues }),
      });

      if (!response.ok) {
        throw new Error('Failed to apply template');
      }

      const result: TemplatedBrief = await response.json();
      onGenerate(result);
    } catch (err) {
      setValidationErrors([err instanceof Error ? err.message : 'Failed to apply template']);
    } finally {
      setLoading(false);
    }
  }, [template.id, variableValues, validateVariables, onGenerate]);

  // Render variable input based on type
  const renderVariableInput = (variable: (typeof template.variables)[0]) => {
    const value = variableValues[variable.name] || '';

    switch (variable.type) {
      case 'Number':
        return (
          <SpinButton
            value={parseInt(value, 10) || variable.minValue || 1}
            min={variable.minValue || 1}
            max={variable.maxValue || 100}
            onChange={(_, data) => handleVariableChange(variable.name, String(data.value || 1))}
          />
        );

      case 'Selection':
        return (
          <Dropdown
            value={value}
            onOptionSelect={(_, data) =>
              handleVariableChange(variable.name, data.optionValue as string)
            }
          >
            {variable.options?.map((opt) => (
              <Option key={opt} value={opt}>
                {opt}
              </Option>
            ))}
          </Dropdown>
        );

      case 'LongText':
        return (
          <Textarea
            value={value}
            onChange={(_, data) => handleVariableChange(variable.name, data.value)}
            placeholder={variable.placeholder || undefined}
            resize="vertical"
            style={{ minHeight: '100px' }}
          />
        );

      case 'Text':
      default:
        return (
          <Input
            value={value}
            onChange={(_, data) => handleVariableChange(variable.name, data.value)}
            placeholder={variable.placeholder || undefined}
          />
        );
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Button icon={<ArrowLeft24Regular />} appearance="subtle" onClick={onBack}>
          Back
        </Button>
        <Title2>Configure Template</Title2>
      </div>

      {/* Template Info */}
      <Card className={styles.section}>
        <div className={styles.templateInfo}>
          <div
            className={styles.iconContainer}
            style={{
              backgroundColor:
                template.thumbnail?.accentColor || getCategoryColor(template.category),
            }}
          >
            {template.thumbnail ? TemplateIconMap[template.thumbnail.iconName] || '📄' : '📄'}
          </div>
          <div>
            <Title3>{template.name}</Title3>
            <Text size={300}>{template.description}</Text>
          </div>
        </div>

        <div className={styles.estimatedDuration}>
          <Text weight="semibold">Estimated Duration:</Text>
          <Badge appearance="tint" color="brand">
            {formatDuration(estimatedDuration)}
          </Badge>
          <Text>•</Text>
          <Text>{template.structure.sections.length} sections</Text>
        </div>
      </Card>

      {/* Variables Form */}
      <Card className={styles.section}>
        <Title3 className={styles.sectionTitle}>Customize Your Video</Title3>
        <div className={styles.fieldGroup}>
          {template.variables.map((variable) => (
            <Field
              key={variable.name}
              label={variable.displayName}
              required={variable.isRequired}
              hint={variable.placeholder || undefined}
            >
              {renderVariableInput(variable)}
            </Field>
          ))}
        </div>

        {validationErrors.length > 0 && (
          <div className={styles.validationErrors}>
            {validationErrors.map((error, i) => (
              <Text key={i} className={styles.errorMessage}>
                ⚠️ {error}
              </Text>
            ))}
          </div>
        )}
      </Card>

      {/* Structure Preview */}
      <Card className={styles.section}>
        <Title3 className={styles.sectionTitle}>Video Structure</Title3>
        <div className={styles.structurePreview}>
          {template.structure.sections.map((section, index) => (
            <div key={index} className={styles.sectionItem}>
              <div className={styles.sectionNumber}>{index + 1}</div>
              <div className={styles.sectionInfo}>
                <Text className={styles.sectionName}>
                  {section.name}
                  {section.isOptional && (
                    <Badge size="small" appearance="outline" style={{ marginLeft: '8px' }}>
                      Optional
                    </Badge>
                  )}
                  {section.isRepeatable && (
                    <Badge
                      size="small"
                      appearance="tint"
                      color="brand"
                      style={{ marginLeft: '8px' }}
                    >
                      ×{variableValues[section.repeatCountVariable || ''] || '?'}
                    </Badge>
                  )}
                </Text>
                <Text className={styles.sectionPurpose}>{section.purpose}</Text>
              </div>
              <Text className={styles.sectionDuration}>
                {formatDuration(section.suggestedDurationSeconds)}
              </Text>
            </div>
          ))}
        </div>
      </Card>

      {/* Script Preview */}
      {preview && (
        <Card className={styles.previewSection}>
          <Title3 className={styles.sectionTitle}>
            <Checkmark24Regular /> Script Preview
          </Title3>
          <div className={styles.previewContent}>{preview.script}</div>
        </Card>
      )}

      {/* Actions */}
      <div className={styles.actions}>
        <Button
          icon={previewing ? <Spinner size="tiny" /> : <Eye24Regular />}
          appearance="secondary"
          onClick={handlePreview}
          disabled={previewing || loading}
        >
          {previewing ? 'Generating...' : 'Preview Script'}
        </Button>
        <Button
          icon={loading ? <Spinner size="tiny" /> : <Play24Regular />}
          appearance="primary"
          onClick={handleGenerate}
          disabled={loading || previewing}
        >
          {loading ? 'Generating...' : 'Generate Video'}
        </Button>
      </div>
    </div>
  );
}

export default TemplateConfigurator;
