import {
  makeStyles,
  tokens,
  Button,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
} from '@fluentui/react-components';
import {
  QuestionCircle24Regular,
  Sparkle24Regular,
  TextGrammarSettings24Regular,
  PeopleAudience24Regular,
  ArrowMinimize24Regular,
  ArrowMaximize24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import { useGuidedModeActions } from '../../hooks/useGuidedModeActions';
import { useGuidedMode } from '../../state/guidedMode';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
  },
});

export interface ArtifactActionsProps {
  artifactType: string;
  artifactId: string;
  content: string;
  onUpdate?: (newContent: string) => void;
  targetAudience?: string;
}

/**
 * Action buttons for artifacts: Explain and Improve
 * Provides quick access to guided mode features
 */
export const ArtifactActions: FC<ArtifactActionsProps> = ({
  artifactType,
  artifactId,
  content,
  onUpdate,
  targetAudience,
}) => {
  const styles = useStyles();
  const { config } = useGuidedMode();
  const { explainArtifact, improveArtifact } = useGuidedModeActions(artifactType);

  if (!config.enabled) {
    return null;
  }

  const handleExplain = () => {
    void explainArtifact(content);
  };

  const handleImprove = (action: string) => {
    void improveArtifact(content, action, artifactId, targetAudience, (newContent) => {
      if (onUpdate) {
        onUpdate(newContent);
      }
    });
  };

  return (
    <div className={styles.container}>
      <Button appearance="subtle" icon={<QuestionCircle24Regular />} onClick={handleExplain}>
        Explain this
      </Button>

      <Menu>
        <MenuTrigger>
          <Button appearance="subtle" icon={<Sparkle24Regular />}>
            Improve
          </Button>
        </MenuTrigger>

        <MenuPopover>
          <MenuList>
            <MenuItem
              icon={<TextGrammarSettings24Regular />}
              onClick={() => handleImprove('improve clarity')}
            >
              Improve Clarity
            </MenuItem>
            <MenuItem
              icon={<PeopleAudience24Regular />}
              onClick={() => handleImprove('adapt for audience')}
            >
              Adapt for Audience
            </MenuItem>
            <MenuItem icon={<ArrowMinimize24Regular />} onClick={() => handleImprove('shorten')}>
              Shorten
            </MenuItem>
            <MenuItem icon={<ArrowMaximize24Regular />} onClick={() => handleImprove('expand')}>
              Expand
            </MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>
    </div>
  );
};
