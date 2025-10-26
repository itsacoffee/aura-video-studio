/**
 * Chroma Key Effect Component
 * Advanced green screen / blue screen controls
 */

import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Label,
  Slider,
  Button,
  Card,
} from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';
import { AppliedEffect } from '../../types/effects';

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
  },
  controlGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  colorPicker: {
    width: '100%',
    height: '40px',
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
  },
  presetButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    flexWrap: 'wrap',
  },
  section: {
    padding: tokens.spacingVerticalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  sectionTitle: {
    fontWeight: tokens.fontWeightSemibold,
    marginBottom: tokens.spacingVerticalS,
  },
});

interface ChromaKeyEffectProps {
  effect: AppliedEffect;
  onUpdate: (effect: AppliedEffect) => void;
  onRemove: () => void;
}

export function ChromaKeyEffect({ effect, onUpdate, onRemove }: ChromaKeyEffectProps) {
  const styles = useStyles();

  const [keyColor, setKeyColor] = useState<string>(
    (effect.parameters.keyColor as string) || '#00ff00'
  );
  const [similarity, setSimilarity] = useState<number>(
    (effect.parameters.similarity as number) || 40
  );
  const [smoothness, setSmoothness] = useState<number>(
    (effect.parameters.smoothness as number) || 8
  );
  const [spillSuppression, setSpillSuppression] = useState<number>(
    (effect.parameters.spillSuppression as number) || 15
  );
  const [edgeThickness, setEdgeThickness] = useState<number>(
    (effect.parameters.edgeThickness as number) || 0
  );
  const [edgeFeather, setEdgeFeather] = useState<number>(
    (effect.parameters.edgeFeather as number) || 1
  );
  const [choke, setChoke] = useState<number>((effect.parameters.choke as number) || 0);
  const [matteCleanup, setMatteCleanup] = useState<number>(
    (effect.parameters.matteCleanup as number) || 0
  );

  const updateEffect = (updates: Record<string, string | number | boolean>) => {
    onUpdate({
      ...effect,
      parameters: {
        ...effect.parameters,
        ...updates,
      },
    });
  };

  const applyPreset = (presetName: string) => {
    const presets: Record<string, Record<string, string | number | boolean>> = {
      studio: {
        keyColor: '#00ff00',
        similarity: 40,
        smoothness: 8,
        spillSuppression: 15,
        edgeThickness: 0,
        edgeFeather: 1,
        choke: 0,
        matteCleanup: 10,
      },
      natural: {
        keyColor: '#00ff00',
        similarity: 50,
        smoothness: 12,
        spillSuppression: 20,
        edgeThickness: 0.5,
        edgeFeather: 2,
        choke: 0.5,
        matteCleanup: 15,
      },
      lowLight: {
        keyColor: '#00ff00',
        similarity: 60,
        smoothness: 15,
        spillSuppression: 25,
        edgeThickness: 1,
        edgeFeather: 3,
        choke: 1,
        matteCleanup: 20,
      },
      uneven: {
        keyColor: '#00ff00',
        similarity: 55,
        smoothness: 18,
        spillSuppression: 30,
        edgeThickness: 0,
        edgeFeather: 2.5,
        choke: 0,
        matteCleanup: 25,
      },
      blueScreen: {
        keyColor: '#0000ff',
        similarity: 40,
        smoothness: 8,
        spillSuppression: 15,
        edgeThickness: 0,
        edgeFeather: 1,
        choke: 0,
        matteCleanup: 10,
      },
    };

    const preset = presets[presetName];
    if (preset) {
      setKeyColor((preset.keyColor as string) || keyColor);
      setSimilarity((preset.similarity as number) || similarity);
      setSmoothness((preset.smoothness as number) || smoothness);
      setSpillSuppression((preset.spillSuppression as number) || spillSuppression);
      setEdgeThickness((preset.edgeThickness as number) || edgeThickness);
      setEdgeFeather((preset.edgeFeather as number) || edgeFeather);
      setChoke((preset.choke as number) || choke);
      setMatteCleanup((preset.matteCleanup as number) || matteCleanup);
      updateEffect(preset);
    }
  };

  return (
    <Card className={styles.container}>
      <div className={styles.header}>
        <Label size="large" weight="semibold">
          Chroma Key
        </Label>
        <Button appearance="subtle" icon={<Dismiss24Regular />} onClick={onRemove} />
      </div>

      {/* Presets */}
      <div className={styles.controlGroup}>
        <Label>Quick Presets</Label>
        <div className={styles.presetButtons}>
          <Button size="small" onClick={() => applyPreset('studio')}>
            Studio
          </Button>
          <Button size="small" onClick={() => applyPreset('natural')}>
            Natural Light
          </Button>
          <Button size="small" onClick={() => applyPreset('lowLight')}>
            Low Light
          </Button>
          <Button size="small" onClick={() => applyPreset('uneven')}>
            Uneven
          </Button>
          <Button size="small" onClick={() => applyPreset('blueScreen')}>
            Blue Screen
          </Button>
        </div>
      </div>

      {/* Key Color */}
      <div className={styles.controlGroup}>
        <Label>Key Color</Label>
        <input
          type="color"
          value={keyColor}
          onChange={(e) => {
            setKeyColor(e.target.value);
            updateEffect({ keyColor: e.target.value });
          }}
          className={styles.colorPicker}
        />
        <div className={styles.presetButtons}>
          <Button
            size="small"
            onClick={() => {
              setKeyColor('#00ff00');
              updateEffect({ keyColor: '#00ff00' });
            }}
          >
            Green
          </Button>
          <Button
            size="small"
            onClick={() => {
              setKeyColor('#0000ff');
              updateEffect({ keyColor: '#0000ff' });
            }}
          >
            Blue
          </Button>
        </div>
      </div>

      {/* Basic Controls */}
      <div className={styles.section}>
        <div className={styles.sectionTitle}>Basic Controls</div>

        <div className={styles.controlGroup}>
          <Label>Similarity: {similarity.toFixed(1)}</Label>
          <Slider
            min={0}
            max={100}
            step={0.1}
            value={similarity}
            onChange={(_, data) => {
              setSimilarity(data.value);
              updateEffect({ similarity: data.value });
            }}
          />
        </div>

        <div className={styles.controlGroup}>
          <Label>Smoothness: {smoothness.toFixed(1)}</Label>
          <Slider
            min={0}
            max={100}
            step={0.1}
            value={smoothness}
            onChange={(_, data) => {
              setSmoothness(data.value);
              updateEffect({ smoothness: data.value });
            }}
          />
        </div>

        <div className={styles.controlGroup}>
          <Label>Spill Suppression: {spillSuppression.toFixed(0)}</Label>
          <Slider
            min={0}
            max={100}
            step={1}
            value={spillSuppression}
            onChange={(_, data) => {
              setSpillSuppression(data.value);
              updateEffect({ spillSuppression: data.value });
            }}
          />
        </div>
      </div>

      {/* Edge Refinement */}
      <div className={styles.section}>
        <div className={styles.sectionTitle}>Edge Refinement</div>

        <div className={styles.controlGroup}>
          <Label>Edge Thickness: {edgeThickness.toFixed(1)}</Label>
          <Slider
            min={-10}
            max={10}
            step={0.1}
            value={edgeThickness}
            onChange={(_, data) => {
              setEdgeThickness(data.value);
              updateEffect({ edgeThickness: data.value });
            }}
          />
        </div>

        <div className={styles.controlGroup}>
          <Label>Edge Feather: {edgeFeather.toFixed(1)}</Label>
          <Slider
            min={0}
            max={50}
            step={0.1}
            value={edgeFeather}
            onChange={(_, data) => {
              setEdgeFeather(data.value);
              updateEffect({ edgeFeather: data.value });
            }}
          />
        </div>

        <div className={styles.controlGroup}>
          <Label>Choke: {choke.toFixed(1)}</Label>
          <Slider
            min={-10}
            max={10}
            step={0.1}
            value={choke}
            onChange={(_, data) => {
              setChoke(data.value);
              updateEffect({ choke: data.value });
            }}
          />
        </div>

        <div className={styles.controlGroup}>
          <Label>Matte Cleanup: {matteCleanup.toFixed(0)}</Label>
          <Slider
            min={0}
            max={100}
            step={1}
            value={matteCleanup}
            onChange={(_, data) => {
              setMatteCleanup(data.value);
              updateEffect({ matteCleanup: data.value });
            }}
          />
        </div>
      </div>
    </Card>
  );
}
