/**
 * EffectStack Component
 *
 * Displays the stack of applied effects for a selected clip.
 * Supports reordering, enabling/disabling, and removing effects.
 */

import { makeStyles, tokens, Text, Button, Tooltip, Switch } from '@fluentui/react-components';
import {
  Delete24Regular,
  ReOrder24Regular,
  Copy24Regular,
  ChevronDown24Regular,
  ChevronRight24Regular,
  Color24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import type { FC } from 'react';
import { useOpenCutEffectsStore, type AppliedEffect } from '../../../stores/opencutEffects';
import { openCutTokens } from '../../../styles/designTokens';
import { EffectControls } from './EffectControls';

export interface EffectStackProps {
  clipId: string;
  className?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalXS,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    color: tokens.colorNeutralForeground2,
  },
  headerIcon: {
    fontSize: '16px',
    color: tokens.colorNeutralForeground3,
  },
  effectItem: {
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
  },
  effectItemSelected: {
    outline: `2px solid ${tokens.colorBrandStroke1}`,
    outlineOffset: '-2px',
  },
  effectItemDisabled: {
    opacity: 0.5,
  },
  effectHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalS}`,
    cursor: 'pointer',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
    },
  },
  effectHeaderLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    flex: 1,
    minWidth: 0,
  },
  dragHandle: {
    cursor: 'grab',
    color: tokens.colorNeutralForeground3,
    ':active': {
      cursor: 'grabbing',
    },
  },
  expandIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '16px',
  },
  effectName: {
    flex: 1,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  effectActions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  actionButton: {
    minWidth: '24px',
    minHeight: '24px',
    padding: '2px',
  },
  effectContent: {
    borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalL,
    color: tokens.colorNeutralForeground3,
  },
  effectCount: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    marginLeft: tokens.spacingHorizontalXS,
  },
});

interface EffectItemProps {
  effect: AppliedEffect;
  isExpanded: boolean;
  isSelected: boolean;
  onToggleExpand: () => void;
  onSelect: () => void;
}

const EffectItem: FC<EffectItemProps> = ({
  effect,
  isExpanded,
  isSelected,
  onToggleExpand,
  onSelect,
}) => {
  const styles = useStyles();
  const effectsStore = useOpenCutEffectsStore();
  const definition = effectsStore.getEffectDefinition(effect.effectId);

  const handleToggleEnabled = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      effectsStore.toggleEffectEnabled(effect.id);
    },
    [effectsStore, effect.id]
  );

  const handleDuplicate = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      effectsStore.duplicateEffect(effect.id);
    },
    [effectsStore, effect.id]
  );

  const handleRemove = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      effectsStore.removeEffect(effect.id);
    },
    [effectsStore, effect.id]
  );

  const handleHeaderClick = useCallback(() => {
    onSelect();
    onToggleExpand();
  }, [onSelect, onToggleExpand]);

  if (!definition) {
    return null;
  }

  return (
    <div
      className={`${styles.effectItem} ${isSelected ? styles.effectItemSelected : ''} ${!effect.enabled ? styles.effectItemDisabled : ''}`}
    >
      <div
        className={styles.effectHeader}
        onClick={handleHeaderClick}
        role="button"
        tabIndex={0}
        aria-expanded={isExpanded}
        aria-label={`${definition.name} effect`}
      >
        <div className={styles.effectHeaderLeft}>
          <ReOrder24Regular className={styles.dragHandle} />
          {isExpanded ? (
            <ChevronDown24Regular className={styles.expandIcon} />
          ) : (
            <ChevronRight24Regular className={styles.expandIcon} />
          )}
          <Text size={200} className={styles.effectName}>
            {definition.name}
          </Text>
        </div>
        <div className={styles.effectActions}>
          <Tooltip
            content={effect.enabled ? 'Disable effect' : 'Enable effect'}
            relationship="label"
          >
            <Switch
              checked={effect.enabled}
              onChange={() => effectsStore.toggleEffectEnabled(effect.id)}
              onClick={(e) => e.stopPropagation()}
              aria-label={effect.enabled ? 'Disable effect' : 'Enable effect'}
            />
          </Tooltip>
          <Tooltip content="Duplicate effect" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<Copy24Regular />}
              className={styles.actionButton}
              onClick={handleDuplicate}
              aria-label="Duplicate effect"
            />
          </Tooltip>
          <Tooltip content="Remove effect" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<Delete24Regular />}
              className={styles.actionButton}
              onClick={handleRemove}
              aria-label="Remove effect"
            />
          </Tooltip>
        </div>
      </div>
      {isExpanded && (
        <div className={styles.effectContent}>
          <EffectControls effect={effect} />
        </div>
      )}
    </div>
  );
};

export const EffectStack: FC<EffectStackProps> = ({ clipId, className }) => {
  const styles = useStyles();
  const effectsStore = useOpenCutEffectsStore();
  const [expandedEffectId, setExpandedEffectId] = useState<string | null>(null);

  const effects = effectsStore.getEffectsForClip(clipId);
  const selectedEffectId = effectsStore.selectedEffectId;

  const handleToggleExpand = useCallback((effectId: string) => {
    setExpandedEffectId((prev) => (prev === effectId ? null : effectId));
  }, []);

  const handleSelectEffect = useCallback(
    (effectId: string) => {
      effectsStore.selectEffect(effectId);
    },
    [effectsStore]
  );

  if (effects.length === 0) {
    return (
      <div className={className}>
        <div className={styles.header}>
          <div className={styles.headerTitle}>
            <Color24Regular className={styles.headerIcon} />
            <Text weight="semibold" size={200}>
              Effects
            </Text>
          </div>
        </div>
        <div className={styles.emptyState}>
          <Text size={200}>No effects applied</Text>
          <Text size={100} block style={{ marginTop: openCutTokens.spacing.xs }}>
            Double-click an effect to apply it
          </Text>
        </div>
      </div>
    );
  }

  return (
    <div className={className}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Color24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={200}>
            Effects
          </Text>
          <Text className={styles.effectCount}>({effects.length})</Text>
        </div>
      </div>
      <div className={styles.container}>
        {effects.map((effect) => (
          <EffectItem
            key={effect.id}
            effect={effect}
            isExpanded={expandedEffectId === effect.id}
            isSelected={selectedEffectId === effect.id}
            onToggleExpand={() => handleToggleExpand(effect.id)}
            onSelect={() => handleSelectEffect(effect.id)}
          />
        ))}
      </div>
    </div>
  );
};

export default EffectStack;
