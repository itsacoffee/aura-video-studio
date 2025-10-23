import {
  makeStyles,
  tokens,
  Input,
  Textarea,
  Button,
  Label,
  Slider,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import { Delete24Regular, Add24Regular, Image24Regular } from '@fluentui/react-icons';
import type { TimelineScene, TimelineAsset } from '../../types/timeline';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalM,
    height: '100%',
    overflow: 'auto',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  row: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
  assetList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  assetItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  assetItemSelected: {
    backgroundColor: tokens.colorBrandBackground2,
    '&:hover': {
      backgroundColor: tokens.colorBrandBackground2Hover,
    },
  },
  thumbnail: {
    width: '48px',
    height: '48px',
    objectFit: 'cover',
    borderRadius: tokens.borderRadiusSmall,
  },
  assetInfo: {
    flex: 1,
    overflow: 'hidden',
  },
  placeholder: {
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
    padding: tokens.spacingVerticalL,
  },
});

interface ScenePropertiesPanelProps {
  scene?: TimelineScene;
  selectedAssetId?: string;
  onUpdateScene?: (updates: Partial<TimelineScene>) => void;
  onUpdateAsset?: (assetId: string, updates: Partial<TimelineAsset>) => void;
  onDeleteAsset?: (assetId: string) => void;
  onImportAsset?: () => void;
  onSelectAsset?: (assetId: string) => void;
  onDuplicateScene?: () => void;
  onDeleteScene?: () => void;
}

export function ScenePropertiesPanel({
  scene,
  selectedAssetId,
  onUpdateScene,
  onUpdateAsset,
  onDeleteAsset,
  onImportAsset,
  onSelectAsset,
  onDuplicateScene,
  onDeleteScene,
}: ScenePropertiesPanelProps) {
  const styles = useStyles();

  if (!scene) {
    return (
      <div className={styles.container}>
        <div className={styles.placeholder}>Select a scene to view its properties</div>
      </div>
    );
  }

  const selectedAsset = scene.visualAssets.find((a) => a.id === selectedAssetId);

  return (
    <div className={styles.container}>
      <div className={styles.section}>
        <h3>Scene Properties</h3>

        <div className={styles.field}>
          <Label>Scene {scene.index + 1}</Label>
          <Input
            value={scene.heading}
            onChange={(_, data) => onUpdateScene?.({ heading: data.value })}
            placeholder="Scene heading"
          />
        </div>

        <div className={styles.field}>
          <Label>Script</Label>
          <Textarea
            value={scene.script}
            onChange={(_, data) => onUpdateScene?.({ script: data.value })}
            placeholder="Scene script"
            rows={4}
          />
        </div>

        <div className={styles.field}>
          <Label>Duration (seconds)</Label>
          <Input
            type="number"
            value={scene.duration.toString()}
            onChange={(_, data) => {
              const duration = parseFloat(data.value);
              if (!isNaN(duration) && duration > 0) {
                onUpdateScene?.({ duration });
              }
            }}
            step={0.1}
            min={0.1}
          />
        </div>

        <div className={styles.field}>
          <Label>Transition</Label>
          <Dropdown
            value={scene.transitionType}
            onOptionSelect={(_, data) =>
              onUpdateScene?.({ transitionType: data.optionValue as string })
            }
          >
            <Option value="None">None</Option>
            <Option value="Fade">Fade</Option>
            <Option value="Slide">Slide</Option>
            <Option value="Wipe">Wipe</Option>
          </Dropdown>
        </div>

        {scene.transitionType !== 'None' && (
          <div className={styles.field}>
            <Label>Transition Duration (seconds)</Label>
            <Input
              type="number"
              value={(scene.transitionDuration || 0.5).toString()}
              onChange={(_, data) => {
                const duration = parseFloat(data.value);
                if (!isNaN(duration) && duration > 0) {
                  onUpdateScene?.({ transitionDuration: duration });
                }
              }}
              step={0.1}
              min={0.1}
            />
          </div>
        )}

        <div className={styles.row}>
          <Button appearance="secondary" onClick={onDuplicateScene}>
            Duplicate Scene
          </Button>
          <Button appearance="secondary" icon={<Delete24Regular />} onClick={onDeleteScene}>
            Delete
          </Button>
        </div>
      </div>

      <div className={styles.section}>
        <div className={styles.row}>
          <h3>Visual Assets</h3>
          <Button appearance="primary" icon={<Add24Regular />} onClick={onImportAsset}>
            Import Asset
          </Button>
        </div>

        {scene.visualAssets.length === 0 ? (
          <div className={styles.placeholder}>No assets in this scene</div>
        ) : (
          <div className={styles.assetList}>
            {scene.visualAssets.map((asset) => (
              <div
                key={asset.id}
                className={`${styles.assetItem} ${
                  asset.id === selectedAssetId ? styles.assetItemSelected : ''
                }`}
                onClick={() => onSelectAsset?.(asset.id)}
              >
                <Image24Regular />
                <div className={styles.assetInfo}>
                  <div>{asset.type}</div>
                  <div
                    style={{
                      fontSize: tokens.fontSizeBase200,
                      color: tokens.colorNeutralForeground3,
                    }}
                  >
                    Z: {asset.zIndex}, Opacity: {Math.round(asset.opacity * 100)}%
                  </div>
                </div>
                <Button
                  appearance="subtle"
                  icon={<Delete24Regular />}
                  onClick={(e) => {
                    e.stopPropagation();
                    onDeleteAsset?.(asset.id);
                  }}
                />
              </div>
            ))}
          </div>
        )}
      </div>

      {selectedAsset && (
        <div className={styles.section}>
          <h3>Asset Properties</h3>

          <div className={styles.field}>
            <Label>Position X (%)</Label>
            <Slider
              value={selectedAsset.position.x}
              min={0}
              max={100}
              step={1}
              onChange={(_, data) =>
                onUpdateAsset?.(selectedAsset.id, {
                  position: { ...selectedAsset.position, x: data.value },
                })
              }
            />
          </div>

          <div className={styles.field}>
            <Label>Position Y (%)</Label>
            <Slider
              value={selectedAsset.position.y}
              min={0}
              max={100}
              step={1}
              onChange={(_, data) =>
                onUpdateAsset?.(selectedAsset.id, {
                  position: { ...selectedAsset.position, y: data.value },
                })
              }
            />
          </div>

          <div className={styles.field}>
            <Label>Width (%)</Label>
            <Slider
              value={selectedAsset.position.width}
              min={1}
              max={100}
              step={1}
              onChange={(_, data) =>
                onUpdateAsset?.(selectedAsset.id, {
                  position: { ...selectedAsset.position, width: data.value },
                })
              }
            />
          </div>

          <div className={styles.field}>
            <Label>Height (%)</Label>
            <Slider
              value={selectedAsset.position.height}
              min={1}
              max={100}
              step={1}
              onChange={(_, data) =>
                onUpdateAsset?.(selectedAsset.id, {
                  position: { ...selectedAsset.position, height: data.value },
                })
              }
            />
          </div>

          <div className={styles.field}>
            <Label>Opacity</Label>
            <Slider
              value={selectedAsset.opacity}
              min={0}
              max={1}
              step={0.01}
              onChange={(_, data) =>
                onUpdateAsset?.(selectedAsset.id, {
                  opacity: data.value,
                })
              }
            />
          </div>

          <div className={styles.field}>
            <Label>Z-Index (layering)</Label>
            <Input
              type="number"
              value={selectedAsset.zIndex.toString()}
              onChange={(_, data) => {
                const zIndex = parseInt(data.value, 10);
                if (!isNaN(zIndex)) {
                  onUpdateAsset?.(selectedAsset.id, { zIndex });
                }
              }}
            />
          </div>

          {selectedAsset.effects && (
            <>
              <div className={styles.field}>
                <Label>Brightness</Label>
                <Slider
                  value={selectedAsset.effects.brightness}
                  min={0}
                  max={2}
                  step={0.1}
                  onChange={(_, data) =>
                    onUpdateAsset?.(selectedAsset.id, {
                      effects: { ...selectedAsset.effects!, brightness: data.value },
                    })
                  }
                />
              </div>

              <div className={styles.field}>
                <Label>Contrast</Label>
                <Slider
                  value={selectedAsset.effects.contrast}
                  min={0}
                  max={2}
                  step={0.1}
                  onChange={(_, data) =>
                    onUpdateAsset?.(selectedAsset.id, {
                      effects: { ...selectedAsset.effects!, contrast: data.value },
                    })
                  }
                />
              </div>

              <div className={styles.field}>
                <Label>Saturation</Label>
                <Slider
                  value={selectedAsset.effects.saturation}
                  min={0}
                  max={2}
                  step={0.1}
                  onChange={(_, data) =>
                    onUpdateAsset?.(selectedAsset.id, {
                      effects: { ...selectedAsset.effects!, saturation: data.value },
                    })
                  }
                />
              </div>
            </>
          )}
        </div>
      )}
    </div>
  );
}
