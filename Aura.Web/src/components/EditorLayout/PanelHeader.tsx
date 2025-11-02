import { makeStyles, tokens, Button } from '@fluentui/react-components';
import {
  ChevronDown20Regular,
  ChevronUp20Regular,
  ChevronLeft20Regular,
  ChevronRight20Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    backgroundColor: tokens.colorNeutralBackground2,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    minHeight: '36px',
  },
  title: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  collapseButton: {
    minWidth: '28px',
    padding: tokens.spacingHorizontalXS,
  },
});

interface PanelHeaderProps {
  title: string;
  isCollapsed: boolean;
  onToggleCollapse: () => void;
  orientation?: 'horizontal' | 'vertical';
}

export function PanelHeader({
  title,
  isCollapsed,
  onToggleCollapse,
  orientation = 'vertical',
}: PanelHeaderProps) {
  const styles = useStyles();

  const getIcon = () => {
    if (orientation === 'horizontal') {
      return isCollapsed ? <ChevronDown20Regular /> : <ChevronUp20Regular />;
    }
    return isCollapsed ? <ChevronRight20Regular /> : <ChevronLeft20Regular />;
  };

  return (
    <div className={styles.header}>
      <span className={styles.title}>{title}</span>
      <Button
        appearance="subtle"
        icon={getIcon()}
        onClick={onToggleCollapse}
        className={styles.collapseButton}
        aria-label={isCollapsed ? `Expand ${title}` : `Collapse ${title}`}
      />
    </div>
  );
}
