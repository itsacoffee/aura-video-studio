import { makeStyles, tokens, Text, Field, Input, Button, Divider } from '@fluentui/react-components';
import { Delete24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    overflow: 'auto',
  },
  header: {
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  title: {
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
  },
  content: {
    padding: tokens.spacingVerticalL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground2,
  },
  emptyState: {
    padding: tokens.spacingVerticalXXL,
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
  },
  actions: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
});

interface ClipProperties {
  id: string;
  label: string;
  startTime: number;
  duration: number;
  type: 'video' | 'audio' | 'image';
  prompt?: string;
  effects?: string[];
}

interface PropertiesPanelProps {
  selectedClip?: ClipProperties | null;
  onUpdateClip?: (updates: Partial<ClipProperties>) => void;
  onDeleteClip?: () => void;
}

export function PropertiesPanel({
  selectedClip,
  onUpdateClip,
  onDeleteClip,
}: PropertiesPanelProps) {
  const styles = useStyles();

  if (!selectedClip) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Text className={styles.title}>Properties</Text>
        </div>
        <div className={styles.emptyState}>
          <Text>Select a clip to view its properties</Text>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text className={styles.title}>Clip Properties</Text>
      </div>

      <div className={styles.content}>
        <div className={styles.section}>
          <Text className={styles.sectionTitle}>Basic Info</Text>
          <Field label="Label">
            <Input
              value={selectedClip.label}
              onChange={(_, data) => onUpdateClip?.({ label: data.value })}
            />
          </Field>
          <Field label="Type">
            <Input value={selectedClip.type} disabled />
          </Field>
          <Field label="Start Time (s)">
            <Input
              type="number"
              value={selectedClip.startTime.toString()}
              onChange={(_, data) => onUpdateClip?.({ startTime: parseFloat(data.value) || 0 })}
            />
          </Field>
          <Field label="Duration (s)">
            <Input
              type="number"
              value={selectedClip.duration.toString()}
              onChange={(_, data) => onUpdateClip?.({ duration: parseFloat(data.value) || 0 })}
            />
          </Field>
        </div>

        {selectedClip.prompt && (
          <>
            <Divider />
            <div className={styles.section}>
              <Text className={styles.sectionTitle}>Generation Details</Text>
              <Field label="Prompt">
                <Input value={selectedClip.prompt} disabled />
              </Field>
            </div>
          </>
        )}

        {selectedClip.effects && selectedClip.effects.length > 0 && (
          <>
            <Divider />
            <div className={styles.section}>
              <Text className={styles.sectionTitle}>Effects</Text>
              {selectedClip.effects.map((effect, index) => (
                <Text key={index}>{effect}</Text>
              ))}
            </div>
          </>
        )}

        <Divider />
        <div className={styles.actions}>
          <Button
            appearance="subtle"
            icon={<Delete24Regular />}
            onClick={onDeleteClip}
            style={{ color: tokens.colorPaletteRedForeground1 }}
          >
            Delete Clip
          </Button>
        </div>
      </div>
    </div>
  );
}
