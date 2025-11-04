import { makeStyles, tokens, Button, Text, Badge, Card } from '@fluentui/react-components';
import { LockClosed24Regular, LockOpen24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import type { FC } from 'react';
import type { LockedSectionDto } from '../../types/api-v1';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  lockedSectionsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  lockedSection: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusSmall,
    borderLeft: `3px solid ${tokens.colorBrandBackground}`,
  },
  sectionInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    flex: 1,
  },
  sectionContent: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  sectionText: {
    fontSize: tokens.fontSizeBase300,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  sectionReason: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalL,
    color: tokens.colorNeutralForeground3,
  },
});

export interface ScriptLockingControlsProps {
  lockedSections: LockedSectionDto[];
  onUnlock: (sectionIndex: number) => void;
}

export const ScriptLockingControls: FC<ScriptLockingControlsProps> = ({
  lockedSections,
  onUnlock,
}) => {
  const styles = useStyles();

  return (
    <div className={styles.container}>
      <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
        <LockClosed24Regular />
        <Text weight="semibold">Locked Sections</Text>
        <Badge appearance="tint" color="informative">
          {lockedSections.length}
        </Badge>
      </div>

      {lockedSections.length === 0 ? (
        <div className={styles.emptyState}>
          <LockOpen24Regular />
          <Text>No sections locked. Select text to lock sections during regeneration.</Text>
        </div>
      ) : (
        <div className={styles.lockedSectionsList}>
          {lockedSections.map((section, index) => (
            <Card key={index} className={styles.lockedSection}>
              <div className={styles.sectionInfo}>
                <LockClosed24Regular />
                <div className={styles.sectionContent}>
                  <Text className={styles.sectionText}>
                    Lines {section.startIndex + 1} - {section.endIndex + 1}
                  </Text>
                  <Text className={styles.sectionReason}>{section.reason || 'Locked by user'}</Text>
                </div>
              </div>
              <Button
                appearance="subtle"
                icon={<Dismiss24Regular />}
                onClick={() => onUnlock(index)}
                aria-label="Unlock section"
              />
            </Card>
          ))}
        </div>
      )}
    </div>
  );
};
