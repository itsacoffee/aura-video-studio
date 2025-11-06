import {
  Card,
  Text,
  Title3,
  Button,
  Spinner,
  Badge,
  Table,
  TableHeader,
  TableRow,
  TableHeaderCell,
  TableBody,
  TableCell,
  makeStyles,
  tokens,
  Field,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import { Checkmark24Regular, Warning24Regular, Info24Regular } from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import type { FC } from 'react';
import apiClient from '../../../services/api/apiClient';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  form: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'flex-end',
  },
  table: {
    width: '100%',
  },
  validRow: {
    backgroundColor: tokens.colorPaletteGreenBackground2,
  },
  invalidRow: {
    backgroundColor: tokens.colorPaletteRedBackground2,
  },
  statusIcon: {
    verticalAlign: 'middle',
    marginRight: tokens.spacingHorizontalS,
  },
  fallbackInfo: {
    marginTop: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusSmall,
  },
});

interface VoiceValidationResult {
  language: string;
  provider: string;
  voiceName: string;
  isValid: boolean;
  errorMessage?: string;
  matchedVoice?: {
    voiceName: string;
    voiceId: string;
    gender: string;
    style: string;
    quality: string;
  };
  fallbackSuggestion?: {
    voiceName: string;
    gender: string;
    quality: string;
  };
}

export const VoiceMappingValidator: FC = () => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [targetLanguage, setTargetLanguage] = useState('en');
  const [provider, setProvider] = useState('ElevenLabs');
  const [voiceName, setVoiceName] = useState('Rachel');
  const [results, setResults] = useState<VoiceValidationResult[]>([]);

  const validateVoice = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiClient.post<{ validation: VoiceValidationResult }>(
        '/api/localization/validate-voice',
        {
          targetLanguage,
          provider,
          voiceName,
        }
      );

      setResults([response.data.validation]);
    } catch (error: unknown) {
      console.error('Voice validation failed', error);
      setResults([
        {
          language: targetLanguage,
          provider,
          voiceName,
          isValid: false,
          errorMessage: error instanceof Error ? error.message : 'Validation failed',
        },
      ]);
    } finally {
      setLoading(false);
    }
  }, [targetLanguage, provider, voiceName]);

  const validateMultiple = useCallback(async () => {
    setLoading(true);

    const testCases = [
      { language: 'en', provider: 'ElevenLabs', voice: 'Rachel' },
      { language: 'es', provider: 'ElevenLabs', voice: 'Diego' },
      { language: 'ar', provider: 'ElevenLabs', voice: 'Ahmed' },
      { language: 'he', provider: 'ElevenLabs', voice: 'David' },
      { language: 'en', provider: 'WindowsSAPI', voice: 'Microsoft David Desktop' },
    ];

    const validationResults: VoiceValidationResult[] = [];

    for (const testCase of testCases) {
      try {
        const response = await apiClient.post<{ validation: VoiceValidationResult }>(
          '/api/localization/validate-voice',
          {
            targetLanguage: testCase.language,
            provider: testCase.provider,
            voiceName: testCase.voice,
          }
        );
        validationResults.push(response.data.validation);
      } catch (error: unknown) {
        validationResults.push({
          language: testCase.language,
          provider: testCase.provider,
          voiceName: testCase.voice,
          isValid: false,
          errorMessage: error instanceof Error ? error.message : 'Validation failed',
        });
      }
    }

    setResults(validationResults);
    setLoading(false);
  }, []);

  return (
    <Card className={styles.container}>
      <div>
        <Title3>Voice Mapping Validation</Title3>
        <Text>Validate voice availability across TTS providers and languages</Text>
      </div>

      <div className={styles.form}>
        <Field label="Target Language">
          <Dropdown
            value={targetLanguage}
            onOptionSelect={(_, data) => setTargetLanguage(data.optionValue as string)}
          >
            <Option value="en">English</Option>
            <Option value="es">Spanish</Option>
            <Option value="fr">French</Option>
            <Option value="de">German</Option>
            <Option value="ar">Arabic (RTL)</Option>
            <Option value="he">Hebrew (RTL)</Option>
            <Option value="ja">Japanese</Option>
            <Option value="zh">Chinese</Option>
          </Dropdown>
        </Field>

        <Field label="Provider">
          <Dropdown
            value={provider}
            onOptionSelect={(_, data) => setProvider(data.optionValue as string)}
          >
            <Option value="ElevenLabs">ElevenLabs</Option>
            <Option value="PlayHT">PlayHT</Option>
            <Option value="WindowsSAPI">Windows SAPI</Option>
            <Option value="Piper">Piper</Option>
            <Option value="Azure">Azure</Option>
          </Dropdown>
        </Field>

        <Field label="Voice Name">
          <Dropdown
            value={voiceName}
            onOptionSelect={(_, data) => setVoiceName(data.optionValue as string)}
          >
            <Option value="Rachel">Rachel</Option>
            <Option value="Josh">Josh</Option>
            <Option value="Diego">Diego</Option>
            <Option value="Ahmed">Ahmed</Option>
            <Option value="David">David</Option>
          </Dropdown>
        </Field>

        <Button appearance="primary" onClick={validateVoice} disabled={loading}>
          {loading ? <Spinner size="tiny" /> : 'Validate'}
        </Button>

        <Button onClick={validateMultiple} disabled={loading}>
          Test Multiple
        </Button>
      </div>

      {results.length > 0 && (
        <Table className={styles.table}>
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Status</TableHeaderCell>
              <TableHeaderCell>Language</TableHeaderCell>
              <TableHeaderCell>Provider</TableHeaderCell>
              <TableHeaderCell>Voice</TableHeaderCell>
              <TableHeaderCell>Details</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {results.map((result, index) => (
              <TableRow
                key={index}
                className={result.isValid ? styles.validRow : styles.invalidRow}
              >
                <TableCell>
                  {result.isValid ? (
                    <>
                      <Checkmark24Regular
                        className={styles.statusIcon}
                        style={{ color: tokens.colorPaletteGreenForeground1 }}
                      />
                      <Badge appearance="tint" color="success">
                        Valid
                      </Badge>
                    </>
                  ) : (
                    <>
                      <Warning24Regular
                        className={styles.statusIcon}
                        style={{ color: tokens.colorPaletteRedForeground1 }}
                      />
                      <Badge appearance="tint" color="danger">
                        Invalid
                      </Badge>
                    </>
                  )}
                </TableCell>
                <TableCell>{result.language}</TableCell>
                <TableCell>{result.provider}</TableCell>
                <TableCell>{result.voiceName}</TableCell>
                <TableCell>
                  {result.isValid && result.matchedVoice && (
                    <div>
                      <Text size={200}>
                        {result.matchedVoice.gender} • {result.matchedVoice.style} •{' '}
                        {result.matchedVoice.quality}
                      </Text>
                    </div>
                  )}
                  {!result.isValid && (
                    <div>
                      <Text size={200}>{result.errorMessage}</Text>
                      {result.fallbackSuggestion && (
                        <div className={styles.fallbackInfo}>
                          <Info24Regular
                            style={{
                              verticalAlign: 'middle',
                              marginRight: tokens.spacingHorizontalXS,
                            }}
                          />
                          <Text size={200} weight="semibold">
                            Fallback: {result.fallbackSuggestion.voiceName}
                          </Text>
                          <Text size={100} style={{ display: 'block', marginTop: '4px' }}>
                            {result.fallbackSuggestion.gender} • {result.fallbackSuggestion.quality}
                          </Text>
                        </div>
                      )}
                    </div>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Card>
  );
};
