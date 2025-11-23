import {
  makeStyles,
  tokens,
  Text,
  Field,
  Input,
  Button,
  Divider,
  Accordion,
  AccordionItem,
  AccordionHeader,
  AccordionPanel,
  Slider,
  Switch,
  Tooltip,
  Select,
} from '@fluentui/react-components';
import {
  Delete24Regular,
  ArrowUpRegular,
  ArrowDownRegular,
  EyeRegular,
  EyeOffRegular,
  DismissRegular,
  AddRegular,
  Info24Regular,
} from '@fluentui/react-icons';
import React, { useState } from 'react';
import { AppliedEffect, EFFECT_DEFINITIONS } from '../../types/effects';
import { TooltipContent, TooltipWithLink } from '../Tooltips';
import { TextOverlaysPanel } from './TextOverlaysPanel';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    overflow: 'auto',
  },
  header: {
    padding: tokens.spacingVerticalL,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  title: {
    fontSize: tokens.fontSizeBase500,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  content: {
    padding: `${tokens.spacingVerticalL} ${tokens.spacingVerticalXL}`,
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
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
    marginBottom: tokens.spacingVerticalXS,
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
  effectsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  effectItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  effectHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  effectName: {
    flex: 1,
    fontSize: tokens.fontSizeBase400,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
    whiteSpace: 'nowrap',
    overflow: 'visible',
    textOverflow: 'clip',
  },
  effectControls: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  effectParameters: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  dropZone: {
    padding: tokens.spacingVerticalL,
    border: `2px dashed ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
  },
  dropZoneActive: {
    backgroundColor: tokens.colorBrandBackground2,
    color: tokens.colorBrandForeground1,
  },
});

interface ClipProperties {
  id: string;
  label: string;
  startTime: number;
  duration: number;
  type: 'video' | 'audio' | 'image';
  prompt?: string;
  effects?: AppliedEffect[];
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
  const [isDragOver, setIsDragOver] = useState(false);

  const handleEffectDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragOver(true);
  };

  const handleEffectDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragOver(false);
  };

  const handleEffectDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragOver(false);

    try {
      const data = e.dataTransfer.getData('application/json');
      if (!data) return;

      const dropData = JSON.parse(data);
      if (dropData.type === 'effect') {
        const effectDef = EFFECT_DEFINITIONS.find((e) => e.type === dropData.effectType);
        if (!effectDef) return;

        // Create new effect with default parameters
        const newEffect: AppliedEffect = {
          id: `effect-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`,
          effectType: effectDef.type,
          enabled: true,
          parameters: effectDef.parameters.reduce(
            (acc, param) => {
              acc[param.name] = param.defaultValue;
              return acc;
            },
            {} as Record<string, number | boolean | string>
          ),
        };

        const currentEffects = selectedClip?.effects || [];
        onUpdateClip?.({ effects: [...currentEffects, newEffect] });
      }
    } catch (error) {
      console.error('Failed to parse dropped effect:', error);
    }
  };

  const handleToggleEffect = (index: number) => {
    if (!selectedClip?.effects) return;
    const updatedEffects = [...selectedClip.effects];
    updatedEffects[index] = { ...updatedEffects[index], enabled: !updatedEffects[index].enabled };
    onUpdateClip?.({ effects: updatedEffects });
  };

  const handleMoveEffect = (index: number, direction: number) => {
    if (!selectedClip?.effects) return;
    const newIndex = index + direction;
    if (newIndex < 0 || newIndex >= selectedClip.effects.length) return;

    const updatedEffects = [...selectedClip.effects];
    [updatedEffects[index], updatedEffects[newIndex]] = [
      updatedEffects[newIndex],
      updatedEffects[index],
    ];
    onUpdateClip?.({ effects: updatedEffects });
  };

  const handleRemoveEffect = (index: number) => {
    if (!selectedClip?.effects) return;
    const updatedEffects = selectedClip.effects.filter((_, i) => i !== index);
    onUpdateClip?.({ effects: updatedEffects });
  };

  const handleUpdateEffectParameter = (
    effectIndex: number,
    paramName: string,
    value: number | boolean | string
  ) => {
    if (!selectedClip?.effects) return;
    const updatedEffects = [...selectedClip.effects];
    updatedEffects[effectIndex] = {
      ...updatedEffects[effectIndex],
      parameters: {
        ...updatedEffects[effectIndex].parameters,
        [paramName]: value,
      },
    };
    onUpdateClip?.({ effects: updatedEffects });
  };

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
                    onChange={(_, data) =>
                      onUpdateClip?.({ startTime: parseFloat(data.value) || 0 })
                    }
                  />
                </Field>
                <Field label="Duration (s)">
                  <Input
                    type="number"
                    value={selectedClip.duration.toString()}
                    onChange={(_, data) =>
                      onUpdateClip?.({ duration: parseFloat(data.value) || 0 })
                    }
                  />
                </Field>
              </div>
            </AccordionPanel>
          </AccordionItem>

          {(selectedClip.type === 'video' || selectedClip.type === 'image') && (
            <AccordionItem value="transform">
              <AccordionHeader>
                <div style={{ display: 'flex', alignItems: 'center' }}>
                  Transform
                  <Tooltip
                    content={<TooltipWithLink content={TooltipContent.editorTransform} />}
                    relationship="label"
                  >
                    <Info24Regular style={{ marginLeft: tokens.spacingHorizontalXXS }} />
                  </Tooltip>
                </div>
              </AccordionHeader>
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
                          transform: {
                            ...selectedClip.transform,
                            scale: parseFloat(data.value) / 100 || 1,
                          },
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
                          transform: {
                            ...selectedClip.transform,
                            rotation: parseFloat(data.value) || 0,
                          },
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

          <AccordionItem value="effects">
            <AccordionHeader>Effects ({selectedClip.effects?.length || 0})</AccordionHeader>
            <AccordionPanel>
              <div className={styles.section}>
                {/* Drop zone for effects */}
                <div
                  className={`${styles.dropZone} ${isDragOver ? styles.dropZoneActive : ''}`}
                  onDragOver={handleEffectDragOver}
                  onDragLeave={handleEffectDragLeave}
                  onDrop={handleEffectDrop}
                >
                  <AddRegular />
                  <Text>Drag effects here from Effects Library</Text>
                </div>

                {/* Applied effects list */}
                {selectedClip.effects && selectedClip.effects.length > 0 && (
                  <div className={styles.effectsList}>
                    {selectedClip.effects.map((effect, index) => (
                      <div key={effect.id} className={styles.effectItem}>
                        <div className={styles.effectHeader}>
                          <Text className={styles.effectName}>
                            {EFFECT_DEFINITIONS.find((e) => e.type === effect.effectType)?.name ||
                              effect.effectType}
                          </Text>
                          <div className={styles.effectControls}>
                            <Tooltip
                              content={effect.enabled ? 'Disable effect' : 'Enable effect'}
                              relationship="label"
                            >
                              <Button
                                appearance="subtle"
                                size="small"
                                icon={effect.enabled ? <EyeRegular /> : <EyeOffRegular />}
                                onClick={() => handleToggleEffect(index)}
                              />
                            </Tooltip>
                            <Tooltip content="Move up" relationship="label">
                              <Button
                                appearance="subtle"
                                size="small"
                                icon={<ArrowUpRegular />}
                                onClick={() => handleMoveEffect(index, -1)}
                                disabled={index === 0}
                              />
                            </Tooltip>
                            <Tooltip content="Move down" relationship="label">
                              <Button
                                appearance="subtle"
                                size="small"
                                icon={<ArrowDownRegular />}
                                onClick={() => handleMoveEffect(index, 1)}
                                disabled={index === selectedClip.effects!.length - 1}
                              />
                            </Tooltip>
                            <Tooltip content="Remove effect" relationship="label">
                              <Button
                                appearance="subtle"
                                size="small"
                                icon={<DismissRegular />}
                                onClick={() => handleRemoveEffect(index)}
                              />
                            </Tooltip>
                          </div>
                        </div>

                        {/* Effect parameters */}
                        {effect.enabled && (
                          <div className={styles.effectParameters}>
                            {EFFECT_DEFINITIONS.find(
                              (e) => e.type === effect.effectType
                            )?.parameters.map((param) => (
                              <Field key={param.name} label={param.label}>
                                {param.type === 'number' ? (
                                  <div
                                    style={{
                                      display: 'flex',
                                      gap: tokens.spacingHorizontalS,
                                      alignItems: 'center',
                                    }}
                                  >
                                    <Slider
                                      min={param.min}
                                      max={param.max}
                                      step={param.step}
                                      value={effect.parameters[param.name] as number}
                                      onChange={(_, data) =>
                                        handleUpdateEffectParameter(index, param.name, data.value)
                                      }
                                      style={{ flex: 1 }}
                                    />
                                    <Input
                                      type="number"
                                      value={(effect.parameters[param.name] as number).toString()}
                                      onChange={(_, data) =>
                                        handleUpdateEffectParameter(
                                          index,
                                          param.name,
                                          parseFloat(data.value) || 0
                                        )
                                      }
                                      style={{ width: '80px' }}
                                    />
                                  </div>
                                ) : param.type === 'boolean' ? (
                                  <Switch
                                    checked={effect.parameters[param.name] as boolean}
                                    onChange={(_, data) =>
                                      handleUpdateEffectParameter(index, param.name, data.checked)
                                    }
                                  />
                                ) : param.type === 'select' ? (
                                  <Select
                                    value={effect.parameters[param.name] as string}
                                    onChange={(_, data) =>
                                      handleUpdateEffectParameter(index, param.name, data.value)
                                    }
                                  >
                                    {param.options?.map((opt) => (
                                      <option key={opt.value} value={opt.value}>
                                        {opt.label}
                                      </option>
                                    ))}
                                  </Select>
                                ) : (
                                  <Input
                                    value={effect.parameters[param.name] as string}
                                    onChange={(_, data) =>
                                      handleUpdateEffectParameter(index, param.name, data.value)
                                    }
                                  />
                                )}
                              </Field>
                            ))}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </AccordionPanel>
          </AccordionItem>

          <TextOverlaysPanel />
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
