import { makeStyles, tokens, Text, Field, Input, Button, Divider, Accordion, AccordionItem, AccordionHeader, AccordionPanel } from '@fluentui/react-components';
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
    padding: `${tokens.spacingVerticalM} ${tokens.spacingVerticalL}`,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
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
  transform?: {
    x?: number;
    y?: number;
    scale?: number;
    rotation?: number;
  };
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
        <Accordion collapsible defaultOpenItems={['basic', 'transform']}>
          <AccordionItem value="basic">
            <AccordionHeader>Basic Info</AccordionHeader>
            <AccordionPanel>
              <div className={styles.section}>
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
            </AccordionPanel>
          </AccordionItem>

          {(selectedClip.type === 'video' || selectedClip.type === 'image') && (
            <AccordionItem value="transform">
              <AccordionHeader>Transform</AccordionHeader>
              <AccordionPanel>
                <div className={styles.section}>
                  <Field label="Position X">
                    <Input
                      type="number"
                      value={(selectedClip.transform?.x || 0).toString()}
                      onChange={(_, data) =>
                        onUpdateClip?.({
                          transform: { ...selectedClip.transform, x: parseFloat(data.value) || 0 },
                        })
                      }
                    />
                  </Field>
                  <Field label="Position Y">
                    <Input
                      type="number"
                      value={(selectedClip.transform?.y || 0).toString()}
                      onChange={(_, data) =>
                        onUpdateClip?.({
                          transform: { ...selectedClip.transform, y: parseFloat(data.value) || 0 },
                        })
                      }
                    />
                  </Field>
                  <Field label="Scale (%)">
                    <Input
                      type="number"
                      value={((selectedClip.transform?.scale || 1) * 100).toString()}
                      onChange={(_, data) =>
                        onUpdateClip?.({
                          transform: { ...selectedClip.transform, scale: parseFloat(data.value) / 100 || 1 },
                        })
                      }
                    />
                  </Field>
                  <Field label="Rotation (deg)">
                    <Input
                      type="number"
                      value={(selectedClip.transform?.rotation || 0).toString()}
                      onChange={(_, data) =>
                        onUpdateClip?.({
                          transform: { ...selectedClip.transform, rotation: parseFloat(data.value) || 0 },
                        })
                      }
                    />
                  </Field>
                </div>
              </AccordionPanel>
            </AccordionItem>
          )}

          {selectedClip.prompt && (
            <AccordionItem value="generation">
              <AccordionHeader>Generation Details</AccordionHeader>
              <AccordionPanel>
                <div className={styles.section}>
                  <Field label="Prompt">
                    <Input value={selectedClip.prompt} disabled />
                  </Field>
                </div>
              </AccordionPanel>
            </AccordionItem>
          )}

          {selectedClip.effects && selectedClip.effects.length > 0 && (
            <AccordionItem value="effects">
              <AccordionHeader>Effects</AccordionHeader>
              <AccordionPanel>
                <div className={styles.section}>
                  {selectedClip.effects.map((effect, index) => (
                    <Text key={index}>{effect}</Text>
                  ))}
                </div>
              </AccordionPanel>
            </AccordionItem>
          )}
        </Accordion>

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
