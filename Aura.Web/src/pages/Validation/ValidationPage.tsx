import {
  Button,
  Card,
  Text,
  Title1,
  Title2,
  Title3,
  Spinner,
  makeStyles,
  tokens,
  Field,
  Input,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import { CheckmarkCircle24Regular, ErrorCircle24Regular } from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import { ErrorState } from '../../components/Loading';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1400px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  headerIcon: {
    fontSize: '32px',
    color: tokens.colorBrandForeground1,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalS,
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  toolCard: {
    padding: tokens.spacingVerticalXL,
  },
  toolHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalL,
  },
  toolIcon: {
    fontSize: '24px',
    color: tokens.colorBrandForeground1,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    maxWidth: '600px',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
  resultsSection: {
    marginTop: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  validationStatus: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalM,
  },
  validIcon: {
    color: tokens.colorPaletteGreenForeground1,
    fontSize: '32px',
  },
  invalidIcon: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: '32px',
  },
  issuesList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalM,
  },
  issueItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusSmall,
  },
});

interface ValidationResult {
  isValid: boolean;
  issues: string[];
  issueCount: number;
}

const ValidationPage: React.FC = () => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [topic, setTopic] = useState('');
  const [audience, setAudience] = useState('');
  const [goal, setGoal] = useState('');
  const [tone, setTone] = useState('Informative');
  const [language, setLanguage] = useState('en-US');
  const [durationMinutes, setDurationMinutes] = useState('1.0');
  const [validationResult, setValidationResult] = useState<ValidationResult | null>(null);

  const handleValidate = useCallback(async () => {
    setLoading(true);
    setError(null);
    setValidationResult(null);

    try {
      const response = await fetch('/api/validation/brief', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          topic,
          audience,
          goal,
          tone,
          language,
          durationMinutes: parseFloat(durationMinutes),
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Validation failed');
      }

      const data = await response.json();
      setValidationResult(data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [topic, audience, goal, tone, language, durationMinutes]);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <CheckmarkCircle24Regular className={styles.headerIcon} />
        <div>
          <Title1>Brief Validation</Title1>
          <Text className={styles.subtitle}>
            Validate your video brief and plan specification before starting generation
          </Text>
        </div>
      </div>

      <div className={styles.content}>
        {error && <ErrorState message={error} />}

        <Card className={styles.toolCard}>
          <div className={styles.toolHeader}>
            <CheckmarkCircle24Regular className={styles.toolIcon} />
            <Title2>Validate Video Brief</Title2>
          </div>
          <div className={styles.form}>
            <Field label="Topic" required>
              <Input
                value={topic}
                onChange={(_, data) => setTopic(data.value)}
                placeholder="AI in Healthcare"
              />
            </Field>
            <Field label="Audience">
              <Input
                value={audience}
                onChange={(_, data) => setAudience(data.value)}
                placeholder="Healthcare professionals"
              />
            </Field>
            <Field label="Goal">
              <Input
                value={goal}
                onChange={(_, data) => setGoal(data.value)}
                placeholder="Educate and inform"
              />
            </Field>
            <Field label="Tone">
              <Dropdown
                value={tone}
                onOptionSelect={(_, data) => setTone(data.optionText || 'Informative')}
              >
                <Option>Informative</Option>
                <Option>Casual</Option>
                <Option>Professional</Option>
                <Option>Energetic</Option>
                <Option>Serious</Option>
              </Dropdown>
            </Field>
            <Field label="Language">
              <Dropdown
                value={language}
                onOptionSelect={(_, data) => setLanguage(data.optionText || 'en-US')}
              >
                <Option>en-US</Option>
                <Option>es-ES</Option>
                <Option>fr-FR</Option>
                <Option>de-DE</Option>
                <Option>it-IT</Option>
                <Option>pt-BR</Option>
              </Dropdown>
            </Field>
            <Field label="Target Duration (minutes)">
              <Input
                type="number"
                value={durationMinutes}
                onChange={(_, data) => setDurationMinutes(data.value)}
                placeholder="1.0"
                step="0.5"
              />
            </Field>
            <div className={styles.actions}>
              <Button appearance="primary" onClick={handleValidate} disabled={loading || !topic}>
                {loading ? <Spinner size="tiny" /> : 'Validate Brief'}
              </Button>
            </div>
          </div>
          {validationResult && (
            <div className={styles.resultsSection}>
              <div className={styles.validationStatus}>
                {validationResult.isValid ? (
                  <>
                    <CheckmarkCircle24Regular className={styles.validIcon} />
                    <Title3>Brief is Valid</Title3>
                  </>
                ) : (
                  <>
                    <ErrorCircle24Regular className={styles.invalidIcon} />
                    <Title3>Brief has {validationResult.issueCount} Issue(s)</Title3>
                  </>
                )}
              </div>
              {!validationResult.isValid && validationResult.issues.length > 0 && (
                <div className={styles.issuesList}>
                  <Text weight="semibold">Issues to resolve:</Text>
                  {validationResult.issues.map((issue, i) => (
                    <div key={i} className={styles.issueItem}>
                      <ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
                      <Text>{issue}</Text>
                    </div>
                  ))}
                </div>
              )}
              {validationResult.isValid && (
                <Text>Your brief is ready for video generation. All requirements are met.</Text>
              )}
            </div>
          )}
        </Card>
      </div>
    </div>
  );
};

export default ValidationPage;
