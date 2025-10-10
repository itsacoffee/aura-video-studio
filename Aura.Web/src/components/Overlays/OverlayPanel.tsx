import { useState } from 'react';
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
} from '@fluentui/react-components';
import {
  Add24Regular,
  Delete24Regular,
} from '@fluentui/react-icons';
import { useTimelineStore, TextOverlay } from '../../state/timeline';

const useStyles = makeStyles({
  panel: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  overlayList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  overlayItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    cursor: 'pointer',
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
});

export function OverlayPanel() {
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
    const newOverlay: TextOverlay = {
      id: `overlay_${Date.now()}`,
      type,
      text: type === 'title' ? 'Title Text' : type === 'lowerThird' ? 'Name' : 'Important!',
      inTime: currentTime,
      outTime: currentTime + 3,
      alignment: type === 'title' ? 'topCenter' : type === 'lowerThird' ? 'bottomLeft' : 'middleRight',
      x: 0,
      y: 0,
      fontSize: type === 'title' ? 72 : type === 'lowerThird' ? 36 : 48,
      fontColor: type === 'callout' ? 'yellow' : 'white',
      backgroundColor: type === 'title' ? 'black' : type === 'lowerThird' ? '#000080' : 'black',
      backgroundOpacity: type === 'title' ? 0.7 : type === 'lowerThird' ? 0.85 : 0.8,
      borderWidth: type === 'title' ? 2 : type === 'callout' ? 3 : 0,
      borderColor: type === 'title' ? 'white' : type === 'callout' ? 'yellow' : undefined,
    };

    addOverlay(newOverlay);
    setSelectedOverlayId(newOverlay.id);
    setEditingOverlay({ ...newOverlay });
  };

  const handleUpdateField = (field: keyof TextOverlay, value: any) => {
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
    <div className={styles.panel}>
      <Card>
        <div className={styles.section}>
          <Text size={500} weight="semibold">
            Text Overlays
          </Text>
          <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
            <Button icon={<Add24Regular />} onClick={() => handleAddOverlay('title')}>
              Title
            </Button>
            <Button icon={<Add24Regular />} onClick={() => handleAddOverlay('lowerThird')}>
              Lower Third
            </Button>
            <Button icon={<Add24Regular />} onClick={() => handleAddOverlay('callout')}>
              Callout
            </Button>
          </div>

          <div className={styles.overlayList}>
            {overlays.map((overlay) => (
              <div
                key={overlay.id}
                className={`${styles.overlayItem} ${overlay.id === selectedOverlayId ? styles.overlayItemSelected : ''}`}
                onClick={() => handleSelectOverlay(overlay.id)}
              >
                <div className={styles.overlayInfo}>
                  <Text weight="semibold">{overlay.text}</Text>
                  <Text size={200}>
                    {overlay.type} • {overlay.inTime.toFixed(1)}s - {overlay.outTime.toFixed(1)}s
                  </Text>
                </div>
                <Button
                  icon={<Delete24Regular />}
                  appearance="subtle"
                  onClick={(e) => {
                    e.stopPropagation();
                    handleDelete(overlay.id);
                  }}
                />
              </div>
            ))}
            {overlays.length === 0 && (
              <Text size={300} style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL, color: tokens.colorNeutralForeground3 }}>
                No overlays yet. Add one to get started.
              </Text>
            )}
          </div>
        </div>
      </Card>

      {selectedOverlay && editingOverlay && (
        <Card>
          <div className={styles.section}>
            <Text size={500} weight="semibold">
              Edit Overlay
            </Text>
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
                    onChange={(_, data) => handleUpdateField('inTime', parseFloat(data.value) || 0)}
                  />
                </Field>
                <Field label="Out Time (s)" className={styles.field}>
                  <Input
                    type="number"
                    value={editingOverlay.outTime?.toString() || '0'}
                    onChange={(_, data) => handleUpdateField('outTime', parseFloat(data.value) || 0)}
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

              <div className={styles.row}>
                <Field label="Font Size" className={styles.field}>
                  <Slider
                    value={editingOverlay.fontSize || 48}
                    min={12}
                    max={120}
                    onChange={(_, data) => handleUpdateField('fontSize', data.value)}
                  />
                </Field>
              </div>

              <div className={styles.row}>
                <Field label="Font Color" className={styles.field}>
                  <Input
                    value={editingOverlay.fontColor || 'white'}
                    onChange={(_, data) => handleUpdateField('fontColor', data.value)}
                  />
                </Field>
                <Field label="Background Color" className={styles.field}>
                  <Input
                    value={editingOverlay.backgroundColor || ''}
                    onChange={(_, data) => handleUpdateField('backgroundColor', data.value)}
                    placeholder="Optional"
                  />
                </Field>
              </div>

              <Field label="Background Opacity">
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
                    onChange={(_, data) => handleUpdateField('borderWidth', parseInt(data.value, 10) || 0)}
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
          </div>
        </Card>
      )}
    </div>
  );
}
