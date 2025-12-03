import {
  Card,
  CardHeader,
  Text,
  Dropdown,
  Option,
  Badge,
  makeStyles,
  tokens,
  Spinner,
  Button,
} from '@fluentui/react-components';
import { PersonRegular, MicRegular } from '@fluentui/react-icons';
import { useEffect } from 'react';
import type { FC } from 'react';
import { useVoiceStore } from '@/stores/voiceStore';
import type { DetectedCharacter, VoiceDescriptor } from '@/stores/voiceStore';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  headerIcon: {
    fontSize: '24px',
    color: tokens.colorBrandForeground1,
  },
  characterList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  characterCard: {
    padding: tokens.spacingVerticalM,
  },
  characterRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalL,
    flexWrap: 'wrap',
  },
  characterInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    flex: 1,
    minWidth: '200px',
  },
  characterName: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  metadata: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  voiceDropdown: {
    minWidth: '200px',
  },
  loadingContainer: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXL,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXL,
    color: tokens.colorNeutralForeground3,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
});

interface CharacterVoiceAssignmentProps {
  characters: DetectedCharacter[];
  onAssignmentChange?: (assignments: Record<string, string>) => void;
  onAssignVoices?: () => Promise<void>;
}

export const CharacterVoiceAssignment: FC<CharacterVoiceAssignmentProps> = ({
  characters,
  onAssignmentChange,
  onAssignVoices,
}) => {
  const styles = useStyles();

  const {
    availableVoices,
    clonedVoices,
    isLoadingVoices,
    manualAssignments,
    setManualAssignment,
    loadAvailableVoices,
    loadClonedVoices,
  } = useVoiceStore();

  // Load voices on mount
  useEffect(() => {
    loadAvailableVoices();
    loadClonedVoices();
  }, [loadAvailableVoices, loadClonedVoices]);

  // Combine available and cloned voices
  const allVoices: VoiceDescriptor[] = [
    ...availableVoices,
    ...clonedVoices.map((cv) => ({
      id: cv.id,
      name: cv.name,
      provider: cv.provider,
      locale: 'en-US',
      gender: 'Neutral' as const,
      isCloned: true,
    })),
  ];

  const handleVoiceSelect = (characterName: string, voiceId: string) => {
    setManualAssignment(characterName, voiceId);
    onAssignmentChange?.({ ...manualAssignments, [characterName]: voiceId });
  };

  const getSuggestedVoiceBadgeColor = (
    voiceType: string
  ): 'brand' | 'informative' | 'subtle' => {
    if (voiceType.includes('female')) return 'brand';
    if (voiceType.includes('male')) return 'informative';
    return 'subtle';
  };

  if (characters.length === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <span className={styles.headerIcon}>🎭</span>
          <Text weight="semibold" size={400}>
            Assign Voices to Characters
          </Text>
        </div>
        <div className={styles.emptyState}>
          <Text>No characters detected. Analyze a script first.</Text>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.headerIcon}>🎭</span>
        <Text weight="semibold" size={400}>
          Assign Voices to Characters
        </Text>
      </div>

      {isLoadingVoices ? (
        <div className={styles.loadingContainer}>
          <Spinner label="Loading voices..." />
        </div>
      ) : (
        <div className={styles.characterList}>
          {characters.map((character) => (
            <Card key={character.name} className={styles.characterCard}>
              <CardHeader
                header={
                  <div className={styles.characterRow}>
                    <div className={styles.characterInfo}>
                      <div className={styles.characterName}>
                        <PersonRegular />
                        <Text weight="semibold">{character.name}</Text>
                      </div>
                      <div className={styles.metadata}>
                        <Badge appearance="outline" size="small">
                          {character.lineCount}{' '}
                          {character.lineCount === 1 ? 'line' : 'lines'}
                        </Badge>
                        <Badge
                          appearance="tint"
                          size="small"
                          color={getSuggestedVoiceBadgeColor(
                            character.suggestedVoiceType
                          )}
                        >
                          Suggested: {character.suggestedVoiceType}
                        </Badge>
                      </div>
                    </div>

                    <Dropdown
                      className={styles.voiceDropdown}
                      placeholder="Select a voice"
                      value={
                        manualAssignments[character.name]
                          ? allVoices.find(
                              (v) => v.id === manualAssignments[character.name]
                            )?.name || ''
                          : ''
                      }
                      selectedOptions={
                        manualAssignments[character.name]
                          ? [manualAssignments[character.name]]
                          : []
                      }
                      onOptionSelect={(_, data) => {
                        if (data.optionValue) {
                          handleVoiceSelect(character.name, data.optionValue);
                        }
                      }}
                    >
                      <Option value="">Auto-assign</Option>
                      {allVoices.map((voice) => (
                        <Option key={voice.id} value={voice.id}>
                          <div
                            style={{
                              display: 'flex',
                              alignItems: 'center',
                              gap: '8px',
                            }}
                          >
                            <MicRegular />
                            {voice.name}
                            {voice.isCloned && (
                              <Badge appearance="tint" size="small" color="brand">
                                Cloned
                              </Badge>
                            )}
                          </div>
                        </Option>
                      ))}
                    </Dropdown>
                  </div>
                }
              />
            </Card>
          ))}
        </div>
      )}

      {onAssignVoices && (
        <div className={styles.actions}>
          <Button appearance="primary" onClick={onAssignVoices}>
            Apply Voice Assignments
          </Button>
        </div>
      )}
    </div>
  );
};
