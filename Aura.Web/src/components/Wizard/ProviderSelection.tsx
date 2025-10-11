import {
  makeStyles,
  tokens,
  Title3,
  Text,
  Dropdown,
  Option,
  Field,
  Tooltip,
  Card,
} from '@fluentui/react-components';
import { Info24Regular } from '@fluentui/react-icons';
import type { PerStageProviderSelection } from '../../state/providers';
import {
  ScriptProviders,
  TtsProviders,
  VisualsProviders,
  UploadProviders,
} from '../../state/providers';

const useStyles = makeStyles({
  section: {
    padding: tokens.spacingVerticalL,
    marginBottom: tokens.spacingVerticalM,
  },
  fieldGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  infoIcon: {
    marginLeft: tokens.spacingHorizontalXS,
    color: tokens.colorBrandForeground1,
    cursor: 'help',
  },
});

interface ProviderSelectionProps {
  selection: PerStageProviderSelection;
  onSelectionChange: (selection: PerStageProviderSelection) => void;
}

export function ProviderSelection({ selection, onSelectionChange }: ProviderSelectionProps) {
  const styles = useStyles();

  const updateSelection = (stage: keyof PerStageProviderSelection, value: string) => {
    onSelectionChange({
      ...selection,
      [stage]: value,
    });
  };

  return (
    <Card className={styles.section}>
      <Title3>
        Provider Selection (Per-Stage)
        <Tooltip content="Choose which provider to use for each stage of video generation" relationship="label">
          <Info24Regular className={styles.infoIcon} />
        </Tooltip>
      </Title3>
      <Text size={200} style={{ marginBottom: tokens.spacingVerticalM }}>
        Override the profile defaults by selecting specific providers for each stage
      </Text>
      
      <div className={styles.fieldGroup}>
        <Field
          label={
            <div style={{ display: 'flex', alignItems: 'center' }}>
              Script LLM Provider
              <Tooltip content="Which AI model to use for script generation" relationship="label">
                <Info24Regular className={styles.infoIcon} />
              </Tooltip>
            </div>
          }
        >
          <Dropdown
            value={selection.script || 'Auto'}
            onOptionSelect={(_, data) => updateSelection('script', data.optionValue as string)}
          >
            <Option value="Auto">Auto (Use Profile Default)</Option>
            {ScriptProviders.map((provider) => (
              <Option key={provider.value} value={provider.value}>
                {provider.label}
              </Option>
            ))}
          </Dropdown>
        </Field>

        <Field
          label={
            <div style={{ display: 'flex', alignItems: 'center' }}>
              TTS Provider
              <Tooltip content="Which text-to-speech engine to use for voice narration" relationship="label">
                <Info24Regular className={styles.infoIcon} />
              </Tooltip>
            </div>
          }
        >
          <Dropdown
            value={selection.tts || 'Auto'}
            onOptionSelect={(_, data) => updateSelection('tts', data.optionValue as string)}
          >
            <Option value="Auto">Auto (Use Profile Default)</Option>
            {TtsProviders.map((provider) => (
              <Option key={provider.value} value={provider.value}>
                {provider.label}
              </Option>
            ))}
          </Dropdown>
        </Field>

        <Field
          label={
            <div style={{ display: 'flex', alignItems: 'center' }}>
              Visuals Provider
              <Tooltip content="Which image provider to use for generating/sourcing visuals" relationship="label">
                <Info24Regular className={styles.infoIcon} />
              </Tooltip>
            </div>
          }
        >
          <Dropdown
            value={selection.visuals || 'Auto'}
            onOptionSelect={(_, data) => updateSelection('visuals', data.optionValue as string)}
          >
            <Option value="Auto">Auto (Use Profile Default)</Option>
            {VisualsProviders.map((provider) => (
              <Option key={provider.value} value={provider.value}>
                {provider.label}
              </Option>
            ))}
          </Dropdown>
        </Field>

        <Field
          label={
            <div style={{ display: 'flex', alignItems: 'center' }}>
              Upload Provider
              <Tooltip content="Whether to automatically upload the finished video" relationship="label">
                <Info24Regular className={styles.infoIcon} />
              </Tooltip>
            </div>
          }
        >
          <Dropdown
            value={selection.upload || 'Auto'}
            onOptionSelect={(_, data) => updateSelection('upload', data.optionValue as string)}
          >
            <Option value="Auto">Auto (Use Profile Default)</Option>
            {UploadProviders.map((provider) => (
              <Option key={provider.value} value={provider.value}>
                {provider.label}
              </Option>
            ))}
          </Dropdown>
        </Field>
      </div>
    </Card>
  );
}
