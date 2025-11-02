import {
  makeStyles,
  tokens,
  Card,
  Text,
  Input,
  Dropdown,
  Option,
  Button,
  Field,
  Slider,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
} from '@fluentui/react-components';
import { Add24Regular, Delete24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import { useTimelineStore, TextOverlay } from '../../state/timeline';

const useStyles = makeStyles({
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  overlayList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    maxHeight: '200px',
    overflowY: 'auto',
  },
  overlayItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalS,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    cursor: 'pointer',
    transition: 'background-color 0.2s ease',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground2,
    },
  },
  overlayItemSelected: {
    backgroundColor: tokens.colorBrandBackground2,
  },
  overlayInfo: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  row: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
  field: {
    flex: 1,
  },
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
  emptyState: {
    padding: tokens.spacingVerticalL,
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
  },
});

export function TextOverlaysPanel() {
  const styles = useStyles();
  const {
    overlays,
    selectedOverlayId,
    currentTime,
    setSelectedOverlayId,
    addOverlay,
    updateOverlay,
    removeOverlay,
  } = useTimelineStore();

  const [editingOverlay, setEditingOverlay] = useState<Partial<TextOverlay> | null>(null);

  const selectedOverlay = overlays.find((o) => o.id === selectedOverlayId) || editingOverlay;

  const handleSelectOverlay = (overlayId: string) => {
    setSelectedOverlayId(overlayId);
    const overlay = overlays.find((o) => o.id === overlayId);
    if (overlay) {
      setEditingOverlay({ ...overlay });
    }
  };

  const handleAddOverlay = (type: 'title' | 'lowerThird' | 'callout') => {
    const overlayDefaults: Record<
      'title' | 'lowerThird' | 'callout',
      Omit<TextOverlay, 'id' | 'type' | 'inTime' | 'outTime' | 'x' | 'y'>
    > = {
      title: {
        text: 'Title Text',
        alignment: 'topCenter',
        fontSize: 72,
        fontColor: 'white',
        backgroundColor: 'black',
        backgroundOpacity: 0.7,
        borderWidth: 2,
        borderColor: 'white',
      },
      lowerThird: {
        text: 'Name',
        alignment: 'bottomLeft',
        fontSize: 36,
        fontColor: 'white',
        backgroundColor: '#000080',
        backgroundOpacity: 0.85,
        borderWidth: 0,
        borderColor: undefined,
      },
      callout: {
        text: 'Important!',
        alignment: 'middleRight',
        fontSize: 48,
        fontColor: 'yellow',
        backgroundColor: 'black',
        backgroundOpacity: 0.8,
        borderWidth: 3,
        borderColor: 'yellow',
      },
    };

    const newOverlay: TextOverlay = {
      id: `overlay_${Date.now()}`,
      type,
      inTime: currentTime,
      outTime: currentTime + 3,
      x: 0,
      y: 0,
      ...overlayDefaults[type],
    };

    addOverlay(newOverlay);
    setSelectedOverlayId(newOverlay.id);
    setEditingOverlay({ ...newOverlay });
  };

  const handleUpdateField = (field: keyof TextOverlay, value: unknown) => {
    if (!editingOverlay) return;
    const updated = { ...editingOverlay, [field]: value };
    setEditingOverlay(updated);
  };

  const handleSave = () => {
    if (!editingOverlay || !editingOverlay.id) return;
    updateOverlay(editingOverlay as TextOverlay);
  };

  const handleDelete = (overlayId: string) => {
    removeOverlay(overlayId);
    if (selectedOverlayId === overlayId) {
      setSelectedOverlayId(null);
      setEditingOverlay(null);
    }
  };

  return (
    <AccordionItem value="textOverlays">
      <AccordionHeader>Text Overlays ({overlays.length})</AccordionHeader>
      <AccordionPanel>
        <div className={styles.section}>
          <div className={styles.buttonGroup}>
            <Button size="small" icon={<Add24Regular />} onClick={() => handleAddOverlay('title')}>
              Title
            </Button>
            <Button
              size="small"
              icon={<Add24Regular />}
              onClick={() => handleAddOverlay('lowerThird')}
            >
              Lower Third
            </Button>
            <Button
              size="small"
              icon={<Add24Regular />}
              onClick={() => handleAddOverlay('callout')}
            >
              Callout
            </Button>
          </div>

          {overlays.length > 0 ? (
            <div className={styles.overlayList}>
              {overlays.map((overlay) => (
                <div
                  key={overlay.id}
                  className={`${styles.overlayItem} ${overlay.id === selectedOverlayId ? styles.overlayItemSelected : ''}`}
                  role="button"
                  tabIndex={0}
                  onClick={() => handleSelectOverlay(overlay.id)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      handleSelectOverlay(overlay.id);
                    }
                  }}
                >
                  <div className={styles.overlayInfo}>
                    <Text weight="semibold" size={200}>
                      {overlay.text}
                    </Text>
                    <Text size={100}>
                      {overlay.type} • {overlay.inTime.toFixed(1)}s - {overlay.outTime.toFixed(1)}s
                    </Text>
                  </div>
                  <Button
                    size="small"
                    icon={<Delete24Regular />}
                    appearance="subtle"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleDelete(overlay.id);
                    }}
                  />
                </div>
              ))}
            </div>
          ) : (
            <div className={styles.emptyState}>
              <Text size={200}>No text overlays yet. Add one to get started.</Text>
            </div>
          )}

          {selectedOverlay && editingOverlay && (
            <Card>
              <div className={styles.form}>
                <Field label="Text">
                  <Input
                    value={editingOverlay.text || ''}
                    onChange={(_, data) => handleUpdateField('text', data.value)}
                  />
                </Field>

                <div className={styles.row}>
                  <Field label="In Time (s)" className={styles.field}>
                    <Input
                      type="number"
                      value={editingOverlay.inTime?.toString() || '0'}
                      onChange={(_, data) =>
                        handleUpdateField('inTime', parseFloat(data.value) || 0)
                      }
                    />
                  </Field>
                  <Field label="Out Time (s)" className={styles.field}>
                    <Input
                      type="number"
                      value={editingOverlay.outTime?.toString() || '0'}
                      onChange={(_, data) =>
                        handleUpdateField('outTime', parseFloat(data.value) || 0)
                      }
                    />
                  </Field>
                </div>

                <Field label="Alignment">
                  <Dropdown
                    value={editingOverlay.alignment || 'topCenter'}
                    onOptionSelect={(_, data) => handleUpdateField('alignment', data.optionValue)}
                  >
                    <Option value="topLeft">Top Left</Option>
                    <Option value="topCenter">Top Center</Option>
                    <Option value="topRight">Top Right</Option>
                    <Option value="middleLeft">Middle Left</Option>
                    <Option value="middleCenter">Middle Center</Option>
                    <Option value="middleRight">Middle Right</Option>
                    <Option value="bottomLeft">Bottom Left</Option>
                    <Option value="bottomCenter">Bottom Center</Option>
                    <Option value="bottomRight">Bottom Right</Option>
                  </Dropdown>
                </Field>

                <Field label={`Font Size: ${editingOverlay.fontSize || 48}px`}>
                  <Slider
                    value={editingOverlay.fontSize || 48}
                    min={12}
                    max={120}
                    onChange={(_, data) => handleUpdateField('fontSize', data.value)}
                  />
                </Field>

                <div className={styles.row}>
                  <Field label="Font Color" className={styles.field}>
                    <Input
                      value={editingOverlay.fontColor || 'white'}
                      onChange={(_, data) => handleUpdateField('fontColor', data.value)}
                    />
                  </Field>
                  <Field label="Background" className={styles.field}>
                    <Input
                      value={editingOverlay.backgroundColor || ''}
                      onChange={(_, data) => handleUpdateField('backgroundColor', data.value)}
                      placeholder="Optional"
                    />
                  </Field>
                </div>

                <Field
                  label={`Background Opacity: ${((editingOverlay.backgroundOpacity || 0.8) * 100).toFixed(0)}%`}
                >
                  <Slider
                    value={editingOverlay.backgroundOpacity || 0.8}
                    min={0}
                    max={1}
                    step={0.1}
                    onChange={(_, data) => handleUpdateField('backgroundOpacity', data.value)}
                  />
                </Field>

                <div className={styles.row}>
                  <Field label="Border Width" className={styles.field}>
                    <Input
                      type="number"
                      value={editingOverlay.borderWidth?.toString() || '0'}
                      onChange={(_, data) =>
                        handleUpdateField('borderWidth', parseInt(data.value, 10) || 0)
                      }
                    />
                  </Field>
                  <Field label="Border Color" className={styles.field}>
                    <Input
                      value={editingOverlay.borderColor || ''}
                      onChange={(_, data) => handleUpdateField('borderColor', data.value)}
                      placeholder="Optional"
                    />
                  </Field>
                </div>

                <Button appearance="primary" onClick={handleSave}>
                  Save Changes
                </Button>
              </div>
            </Card>
          )}
        </div>
      </AccordionPanel>
    </AccordionItem>
  );
}
