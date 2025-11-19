import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Field,
  Input,
  Card,
  Spinner,
} from '@fluentui/react-components';
import { Play24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import { useState, useRef } from 'react';
import { useNotifications } from '../../components/Notifications/Toasts';
import { LlmProviderSelector } from '../../components/Streaming/LlmProviderSelector';
import { StreamingMetrics } from '../../components/Streaming/StreamingMetrics';
import {
  streamGeneration,
  type StreamingScriptEvent,
  type StreamInitEvent,
  type StreamChunkEvent,
  type StreamCompleteEvent,
  type StreamErrorEvent,
} from '../../services/api/ollamaService';

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
    margin: '0 auto',
    padding: tokens.spacingVerticalXXL,
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
  },
  section: {
    marginBottom: tokens.spacingVerticalL,
  },
  formSection: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalL,
    marginBottom: tokens.spacingVerticalL,
  },
  formColumn: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  outputSection: {
    marginTop: tokens.spacingVerticalL,
  },
  outputCard: {
    padding: tokens.spacingVerticalL,
    minHeight: '300px',
    maxHeight: '600px',
    overflowY: 'auto',
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase300,
    lineHeight: '1.6',
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  errorCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteRedBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
});

export function StreamingScriptDemo() {
  const styles = useStyles();
  const { showSuccessToast, showFailureToast } = useNotifications();

  const [topic, setTopic] = useState('The future of AI in video production');
  const [audience, setAudience] = useState('Content creators and video editors');
  const [duration, setDuration] = useState('60');
  const [provider, setProvider] = useState<string | undefined>(undefined);

  const [isStreaming, setIsStreaming] = useState(false);
  const [outputText, setOutputText] = useState('');
  const [error, setError] = useState<string | null>(null);

  const [initEvent, setInitEvent] = useState<StreamInitEvent | undefined>();
  const [currentChunk, setCurrentChunk] = useState<StreamChunkEvent | undefined>();
  const [completeEvent, setCompleteEvent] = useState<StreamCompleteEvent | undefined>();

  const abortControllerRef = useRef<AbortController | null>(null);

  const handleStart = async () => {
    if (!topic.trim()) {
      showFailureToast({ title: 'Validation Error', message: 'Please enter a topic' });
      return;
    }

    setIsStreaming(true);
    setOutputText('');
    setError(null);
    setInitEvent(undefined);
    setCurrentChunk(undefined);
    setCompleteEvent(undefined);

    const abortController = new AbortController();
    abortControllerRef.current = abortController;

    try {
      await streamGeneration(
        {
          topic: topic.trim(),
          audience: audience.trim() || undefined,
          targetDurationSeconds: parseInt(duration) || 60,
          preferredProvider: provider,
        },
        (event: StreamingScriptEvent) => {
          switch (event.eventType) {
            case 'init': {
              setInitEvent(event);
              break;
            }

            case 'chunk': {
              setCurrentChunk(event);
              setOutputText(event.accumulatedContent);
              break;
            }

            case 'complete': {
              setCompleteEvent(event);
              setOutputText(event.accumulatedContent);
              setIsStreaming(false);
              showSuccessToast({
                title: 'Generation Complete',
                message: `Generated ${event.tokenCount} tokens`,
              });
              break;
            }

            case 'error': {
              const errorEvent = event as StreamErrorEvent;
              setError(errorEvent.errorMessage);
              setIsStreaming(false);
              showFailureToast({
                title: 'Generation Error',
                message: errorEvent.errorMessage,
              });
              break;
            }
          }
        },
        abortController.signal
      );
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      if (errorObj.name !== 'AbortError') {
        setError(errorObj.message);
        showFailureToast({
          title: 'Generation Failed',
          message: errorObj.message,
        });
      }
      setIsStreaming(false);
    } finally {
      abortControllerRef.current = null;
    }
  };

  const handleCancel = () => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
      setIsStreaming(false);
      showSuccessToast({ title: 'Cancelled', message: 'Generation cancelled by user' });
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title2>Streaming Script Generation Demo</Title2>
        <Text>
          Test the unified streaming interface with all LLM providers (OpenAI, Anthropic, Gemini,
          Azure, Ollama)
        </Text>
      </div>

      <Card className={styles.section}>
        <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>Configuration</Title3>

        <div className={styles.formSection}>
          <div className={styles.formColumn}>
            <Field label="Video Topic" required>
              <Input
                value={topic}
                onChange={(e) => setTopic(e.target.value)}
                placeholder="e.g., How to create engaging videos"
                disabled={isStreaming}
              />
            </Field>

            <Field label="Target Audience">
              <Input
                value={audience}
                onChange={(e) => setAudience(e.target.value)}
                placeholder="e.g., Beginners, Professionals"
                disabled={isStreaming}
              />
            </Field>

            <Field label="Duration (seconds)">
              <Input
                type="number"
                value={duration}
                onChange={(e) => setDuration(e.target.value)}
                min="15"
                max="300"
                disabled={isStreaming}
              />
            </Field>
          </div>

          <div className={styles.formColumn}>
            <LlmProviderSelector value={provider} onChange={setProvider} showAutoOption={true} />
          </div>
        </div>

        <div className={styles.actions}>
          <Button
            appearance="primary"
            icon={<Play24Regular />}
            onClick={handleStart}
            disabled={isStreaming}
          >
            Start Streaming
          </Button>
          {isStreaming && (
            <Button icon={<Dismiss24Regular />} onClick={handleCancel}>
              Cancel
            </Button>
          )}
        </div>
      </Card>

      <div className={styles.outputSection}>
        <StreamingMetrics
          initEvent={initEvent}
          currentChunk={currentChunk}
          completeEvent={completeEvent}
          isStreaming={isStreaming}
        />

        <Card className={styles.section}>
          <Title3 style={{ marginBottom: tokens.spacingVerticalM }}>Generated Script</Title3>
          <Card className={styles.outputCard}>
            {isStreaming && !outputText && (
              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
              >
                <Spinner size="tiny" />
                <Text>Waiting for stream to start...</Text>
              </div>
            )}
            {outputText || (
              <Text style={{ color: tokens.colorNeutralForeground3 }}>
                Click &quot;Start Streaming&quot; to generate a script
              </Text>
            )}
            {outputText}
          </Card>
        </Card>

        {error && (
          <Card className={styles.errorCard}>
            <Text weight="semibold" style={{ color: tokens.colorPaletteRedForeground1 }}>
              Error: {error}
            </Text>
          </Card>
        )}
      </div>
    </div>
  );
}
