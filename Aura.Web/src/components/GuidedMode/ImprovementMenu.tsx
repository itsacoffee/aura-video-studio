import {
  makeStyles,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Button,
} from '@fluentui/react-components';
import {
  TextGrammarSettings24Regular,
  PeopleAudience24Regular,
  ArrowMinimize24Regular,
  ArrowMaximize24Regular,
  Sparkle24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';

const useStyles = makeStyles({
  menuButton: {
    minWidth: '150px',
  },
});

export interface ImprovementMenuProps {
  onImproveClarity: () => void;
  onAdaptForAudience: () => void;
  onShorten: () => void;
  onExpand: () => void;
}

export const ImprovementMenu: FC<ImprovementMenuProps> = ({
  onImproveClarity,
  onAdaptForAudience,
  onShorten,
  onExpand,
}) => {
  const styles = useStyles();

  return (
    <Menu>
      <MenuTrigger>
        <Button appearance="primary" icon={<Sparkle24Regular />} className={styles.menuButton}>
          Improve
        </Button>
      </MenuTrigger>

      <MenuPopover>
        <MenuList>
          <MenuItem icon={<TextGrammarSettings24Regular />} onClick={onImproveClarity}>
            Improve Clarity
          </MenuItem>
          <MenuItem icon={<PeopleAudience24Regular />} onClick={onAdaptForAudience}>
            Adapt for Audience
          </MenuItem>
          <MenuItem icon={<ArrowMinimize24Regular />} onClick={onShorten}>
            Shorten
          </MenuItem>
          <MenuItem icon={<ArrowMaximize24Regular />} onClick={onExpand}>
            Expand
          </MenuItem>
        </MenuList>
      </MenuPopover>
    </Menu>
  );
};
