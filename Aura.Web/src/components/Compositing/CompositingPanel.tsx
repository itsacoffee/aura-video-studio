/**
 * Compositing Panel
 * Integrated panel combining chroma key, layer compositing, and motion tracking
 */

import { makeStyles, tokens, Tab, TabList, Card } from '@fluentui/react-components';
import { useState } from 'react';
import { TrackingPath } from '../../services/motionTrackingService';
import { AppliedEffect } from '../../types/effects';
import { ChromaKeyEffect } from '../Effects/ChromaKeyEffect';
import { LayerStack, VideoLayer } from './LayerStack';
import { MattePreview } from './MattePreview';
import { MotionTracking } from './MotionTracking';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    height: '100%',
  },
  tabContent: {
    flex: 1,
    overflow: 'auto',
  },
});

interface CompositingPanelProps {
  onEffectUpdate?: (effect: AppliedEffect) => void;
  onEffectRemove?: () => void;
  chromaKeyEffect?: AppliedEffect;
}

export function CompositingPanel({
  onEffectUpdate,
  onEffectRemove,
  chromaKeyEffect,
}: CompositingPanelProps) {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<string>('chroma-key');

  // Layer management state
  const [layers, setLayers] = useState<VideoLayer[]>([
    {
      id: 'layer-1',
      name: 'Background',
      visible: true,
      blendMode: 'normal',
      opacity: 100,
      position: { x: 0, y: 0 },
      scale: { x: 1, y: 1 },
      rotation: 0,
    },
  ]);
  const [selectedLayerId, setSelectedLayerId] = useState<string | null>(null);

  // Motion tracking state
  const [trackingPaths, setTrackingPaths] = useState<Record<string, TrackingPath>>({});
  const [isTracking, setIsTracking] = useState(false);

  const handleStartTracking = (name: string) => {
    // In a real implementation, this would start the tracking process
    // eslint-disable-next-line no-console
    console.log('Start tracking:', name);
    setIsTracking(true);
  };

  const handleStopTracking = (pathId: string) => {
    // eslint-disable-next-line no-console
    console.log('Stop tracking:', pathId);
    setIsTracking(false);
  };

  const handleRemoveTracking = (pathId: string) => {
    const newPaths = { ...trackingPaths };
    delete newPaths[pathId];
    setTrackingPaths(newPaths);
  };

  return (
    <Card className={styles.container}>
      <TabList
        selectedValue={selectedTab}
        onTabSelect={(_, data) => setSelectedTab(data.value as string)}
      >
        <Tab value="chroma-key">Chroma Key</Tab>
        <Tab value="layers">Layers</Tab>
        <Tab value="preview">Preview</Tab>
        <Tab value="tracking">Motion Tracking</Tab>
      </TabList>

      <div className={styles.tabContent}>
        {selectedTab === 'chroma-key' && chromaKeyEffect && onEffectUpdate && onEffectRemove && (
          <ChromaKeyEffect
            effect={chromaKeyEffect}
            onUpdate={onEffectUpdate}
            onRemove={onEffectRemove}
          />
        )}

        {selectedTab === 'layers' && (
          <LayerStack
            layers={layers}
            selectedLayerId={selectedLayerId}
            onLayersChange={setLayers}
            onSelectLayer={setSelectedLayerId}
          />
        )}

        {selectedTab === 'preview' && (
          <MattePreview
            sourceCanvas={undefined}
            matteCanvas={undefined}
            compositeCanvas={undefined}
            backgroundCanvas={undefined}
          />
        )}

        {selectedTab === 'tracking' && (
          <MotionTracking
            trackingPaths={trackingPaths}
            onStartTracking={handleStartTracking}
            onStopTracking={handleStopTracking}
            onRemoveTracking={handleRemoveTracking}
            isTracking={isTracking}
          />
        )}
      </div>
    </Card>
  );
}
