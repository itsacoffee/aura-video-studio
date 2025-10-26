/**
 * Layer Parenting Component
 * Manage hierarchical relationships between layers
 */

import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Button,
  Label,
  Card,
  Select,
  Divider,
} from '@fluentui/react-components';
import {
  ArrowUp24Regular,
  ArrowDown24Regular,
  Delete24Regular,
  Link24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalL,
  },
  layerList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  layerItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusSmall,
    borderLeft: `4px solid transparent`,
  },
  layerItemChild: {
    marginLeft: tokens.spacingHorizontalXXL,
    borderLeftColor: tokens.colorBrandBackground,
  },
  layerInfo: {
    flex: 1,
  },
  controls: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  parentingControls: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
});

export interface Layer {
  id: string;
  name: string;
  type: 'video' | 'audio' | 'shape' | 'text' | 'image';
  parentId: string | null;
}

interface LayerParentingProps {
  layers: Layer[];
  onLayerParent?: (layerId: string, parentId: string | null) => void;
  onLayerReorder?: (layerId: string, direction: 'up' | 'down') => void;
}

export function LayerParenting({
  layers,
  onLayerParent,
  onLayerReorder,
}: LayerParentingProps) {
  const styles = useStyles();
  const [selectedLayerId, setSelectedLayerId] = useState<string | null>(null);

  const selectedLayer = layers.find((l) => l.id === selectedLayerId);

  const getLayerHierarchy = () => {
    // Build hierarchy structure
    const rootLayers = layers.filter((l) => !l.parentId);
    const result: Array<{ layer: Layer; depth: number }> = [];

    const addChildren = (parentId: string, depth: number) => {
      const children = layers.filter((l) => l.parentId === parentId);
      children.forEach((child) => {
        result.push({ layer: child, depth });
        addChildren(child.id, depth + 1);
      });
    };

    rootLayers.forEach((layer) => {
      result.push({ layer, depth: 0 });
      addChildren(layer.id, 1);
    });

    return result;
  };

  const handleSetParent = (parentId: string | null) => {
    if (selectedLayerId) {
      // Prevent circular dependencies
      if (parentId && isDescendant(parentId, selectedLayerId)) {
        return;
      }
      onLayerParent?.(selectedLayerId, parentId);
    }
  };

  const isDescendant = (layerId: string, potentialDescendantId: string): boolean => {
    const descendants = layers.filter((l) => l.parentId === layerId);
    if (descendants.some((d) => d.id === potentialDescendantId)) {
      return true;
    }
    return descendants.some((d) => isDescendant(d.id, potentialDescendantId));
  };

  const getAvailableParents = () => {
    if (!selectedLayerId) return [];
    return layers.filter(
      (l) => l.id !== selectedLayerId && !isDescendant(selectedLayerId, l.id)
    );
  };

  const hierarchy = getLayerHierarchy();

  return (
    <div className={styles.container}>
      <Card>
        <div style={{ padding: tokens.spacingVerticalM }}>
          <Label weight="semibold">Layer Hierarchy</Label>
          <Divider />
          <p style={{ fontSize: tokens.fontSizeBase200, color: tokens.colorNeutralForeground2 }}>
            Create parent-child relationships between layers. Child layers inherit transformations
            from their parents.
          </p>
        </div>

        <div className={styles.layerList}>
          {hierarchy.map(({ layer, depth }) => (
            <div
              key={layer.id}
              className={`${styles.layerItem} ${depth > 0 ? styles.layerItemChild : ''}`}
              style={{ marginLeft: `${depth * 32}px` }}
            >
              <div className={styles.layerInfo}>
                <Label weight={selectedLayerId === layer.id ? 'semibold' : 'regular'}>
                  {layer.name}
                </Label>
                <Label size="small" style={{ color: tokens.colorNeutralForeground3 }}>
                  {layer.type}
                  {layer.parentId && ` • Parent: ${layers.find((l) => l.id === layer.parentId)?.name}`}
                </Label>
              </div>

              <div className={styles.controls}>
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<ArrowUp24Regular />}
                  onClick={() => onLayerReorder?.(layer.id, 'up')}
                  aria-label="Move up"
                />
                <Button
                  appearance="subtle"
                  size="small"
                  icon={<ArrowDown24Regular />}
                  onClick={() => onLayerReorder?.(layer.id, 'down')}
                  aria-label="Move down"
                />
                <Button
                  appearance={selectedLayerId === layer.id ? 'primary' : 'subtle'}
                  size="small"
                  icon={<Link24Regular />}
                  onClick={() => setSelectedLayerId(layer.id)}
                  aria-label="Set parent"
                />
              </div>
            </div>
          ))}
        </div>
      </Card>

      {selectedLayer && (
        <Card>
          <div className={styles.parentingControls}>
            <Label weight="semibold">Parent Layer: {selectedLayer.name}</Label>
            <Divider />

            <div>
              <Label>Parent</Label>
              <Select
                value={selectedLayer.parentId || 'none'}
                onChange={(_, data) => {
                  const newParentId = data.value === 'none' ? null : data.value;
                  handleSetParent(newParentId);
                }}
              >
                <option value="none">No Parent</option>
                {getAvailableParents().map((layer) => (
                  <option key={layer.id} value={layer.id}>
                    {layer.name}
                  </option>
                ))}
              </Select>
            </div>

            {selectedLayer.parentId && (
              <Button
                appearance="secondary"
                icon={<Delete24Regular />}
                onClick={() => handleSetParent(null)}
              >
                Remove Parent
              </Button>
            )}
          </div>
        </Card>
      )}
    </div>
  );
}
