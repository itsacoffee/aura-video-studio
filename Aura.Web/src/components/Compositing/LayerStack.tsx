/**
 * Layer Stack Component
 * Multi-layer compositing with blend modes and transform controls
 */

import {
  makeStyles,
  tokens,
  Button,
  Label,
  Slider,
  Dropdown,
  Option,
  Card,
} from '@fluentui/react-components';
import {
  ArrowUp24Regular,
  ArrowDown24Regular,
  Delete24Regular,
  Eye24Regular,
  EyeOff24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  layerList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  layer: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  layerSelected: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1Selected,
  },
  layerInfo: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  layerControls: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  transformControls: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalM,
  },
  controlGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
});

export interface VideoLayer {
  id: string;
  name: string;
  visible: boolean;
  blendMode: 'normal' | 'multiply' | 'screen' | 'overlay' | 'add';
  opacity: number;
  position: { x: number; y: number };
  scale: { x: number; y: number };
  rotation: number;
}

interface LayerStackProps {
  layers: VideoLayer[];
  selectedLayerId: string | null;
  onLayersChange: (layers: VideoLayer[]) => void;
  onSelectLayer: (layerId: string | null) => void;
}

export function LayerStack({
  layers,
  selectedLayerId,
  onLayersChange,
  onSelectLayer,
}: LayerStackProps) {
  const styles = useStyles();

  const selectedLayer = layers.find((l) => l.id === selectedLayerId);

  const updateLayer = (layerId: string, updates: Partial<VideoLayer>) => {
    onLayersChange(
      layers.map((layer) => (layer.id === layerId ? { ...layer, ...updates } : layer))
    );
  };

  const moveLayer = (layerId: string, direction: 'up' | 'down') => {
    const index = layers.findIndex((l) => l.id === layerId);
    if (index === -1) return;

    const newIndex = direction === 'up' ? index - 1 : index + 1;
    if (newIndex < 0 || newIndex >= layers.length) return;

    const newLayers = [...layers];
    [newLayers[index], newLayers[newIndex]] = [newLayers[newIndex], newLayers[index]];
    onLayersChange(newLayers);
  };

  const deleteLayer = (layerId: string) => {
    onLayersChange(layers.filter((l) => l.id !== layerId));
    if (selectedLayerId === layerId) {
      onSelectLayer(null);
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Label size="large" weight="semibold">
          Layer Stack
        </Label>
        <Button
          size="small"
          onClick={() => {
            const newLayer: VideoLayer = {
              id: `layer-${Date.now()}`,
              name: `Layer ${layers.length + 1}`,
              visible: true,
              blendMode: 'normal',
              opacity: 100,
              position: { x: 0, y: 0 },
              scale: { x: 1, y: 1 },
              rotation: 0,
            };
            onLayersChange([...layers, newLayer]);
          }}
        >
          Add Layer
        </Button>
      </div>

      <div className={styles.layerList}>
        {layers.map((layer, index) => (
          <div
            key={layer.id}
            className={`${styles.layer} ${selectedLayerId === layer.id ? styles.layerSelected : ''}`}
            onClick={() => onSelectLayer(layer.id)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                onSelectLayer(layer.id);
              }
            }}
            role="button"
            tabIndex={0}
          >
            <div className={styles.layerInfo}>
              <Label weight="semibold">{layer.name}</Label>
              <Label size="small">
                {layer.blendMode} • {layer.opacity}% opacity
              </Label>
            </div>

            <div className={styles.layerControls}>
              <Button
                size="small"
                appearance="subtle"
                icon={layer.visible ? <Eye24Regular /> : <EyeOff24Regular />}
                onClick={(e) => {
                  e.stopPropagation();
                  updateLayer(layer.id, { visible: !layer.visible });
                }}
              />
              <Button
                size="small"
                appearance="subtle"
                icon={<ArrowUp24Regular />}
                disabled={index === 0}
                onClick={(e) => {
                  e.stopPropagation();
                  moveLayer(layer.id, 'up');
                }}
              />
              <Button
                size="small"
                appearance="subtle"
                icon={<ArrowDown24Regular />}
                disabled={index === layers.length - 1}
                onClick={(e) => {
                  e.stopPropagation();
                  moveLayer(layer.id, 'down');
                }}
              />
              <Button
                size="small"
                appearance="subtle"
                icon={<Delete24Regular />}
                onClick={(e) => {
                  e.stopPropagation();
                  deleteLayer(layer.id);
                }}
              />
            </div>
          </div>
        ))}
      </div>

      {selectedLayer && (
        <Card>
          <div className={styles.container}>
            <Label size="large" weight="semibold">
              Layer Properties
            </Label>

            {/* Blend Mode */}
            <div className={styles.controlGroup}>
              <Label>Blend Mode</Label>
              <Dropdown
                value={selectedLayer.blendMode}
                onOptionSelect={(_, data) => {
                  updateLayer(selectedLayer.id, {
                    blendMode: data.optionValue as VideoLayer['blendMode'],
                  });
                }}
              >
                <Option value="normal">Normal</Option>
                <Option value="multiply">Multiply</Option>
                <Option value="screen">Screen</Option>
                <Option value="overlay">Overlay</Option>
                <Option value="add">Add</Option>
              </Dropdown>
            </div>

            {/* Opacity */}
            <div className={styles.controlGroup}>
              <Label>Opacity: {selectedLayer.opacity}%</Label>
              <Slider
                min={0}
                max={100}
                value={selectedLayer.opacity}
                onChange={(_, data) => {
                  updateLayer(selectedLayer.id, { opacity: data.value });
                }}
              />
            </div>

            {/* Transform Controls */}
            <div className={styles.transformControls}>
              <div className={styles.controlGroup}>
                <Label>Position X: {selectedLayer.position.x}</Label>
                <Slider
                  min={-1000}
                  max={1000}
                  value={selectedLayer.position.x}
                  onChange={(_, data) => {
                    updateLayer(selectedLayer.id, {
                      position: { ...selectedLayer.position, x: data.value },
                    });
                  }}
                />
              </div>

              <div className={styles.controlGroup}>
                <Label>Position Y: {selectedLayer.position.y}</Label>
                <Slider
                  min={-1000}
                  max={1000}
                  value={selectedLayer.position.y}
                  onChange={(_, data) => {
                    updateLayer(selectedLayer.id, {
                      position: { ...selectedLayer.position, y: data.value },
                    });
                  }}
                />
              </div>

              <div className={styles.controlGroup}>
                <Label>Scale X: {selectedLayer.scale.x.toFixed(2)}</Label>
                <Slider
                  min={0.1}
                  max={5}
                  step={0.01}
                  value={selectedLayer.scale.x}
                  onChange={(_, data) => {
                    updateLayer(selectedLayer.id, {
                      scale: { ...selectedLayer.scale, x: data.value },
                    });
                  }}
                />
              </div>

              <div className={styles.controlGroup}>
                <Label>Scale Y: {selectedLayer.scale.y.toFixed(2)}</Label>
                <Slider
                  min={0.1}
                  max={5}
                  step={0.01}
                  value={selectedLayer.scale.y}
                  onChange={(_, data) => {
                    updateLayer(selectedLayer.id, {
                      scale: { ...selectedLayer.scale, y: data.value },
                    });
                  }}
                />
              </div>

              <div className={styles.controlGroup}>
                <Label>Rotation: {selectedLayer.rotation}°</Label>
                <Slider
                  min={-360}
                  max={360}
                  value={selectedLayer.rotation}
                  onChange={(_, data) => {
                    updateLayer(selectedLayer.id, { rotation: data.value });
                  }}
                />
              </div>
            </div>
          </div>
        </Card>
      )}
    </div>
  );
}
