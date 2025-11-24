import { makeStyles, Button } from '@fluentui/react-components';
import {
  ChevronDown20Regular,
  ChevronUp20Regular,
  ChevronLeft20Regular,
  ChevronRight20Regular,
} from '@fluentui/react-icons';
import '../../styles/video-editor-theme.css';

const useStyles = makeStyles({
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: 'var(--editor-space-sm) var(--editor-space-md)',
    backgroundColor: 'var(--editor-panel-header-bg)',
    borderBottom: `1px solid var(--editor-panel-border)`,
    minHeight: '36px',
    transition: 'background-color var(--editor-transition-fast)',
    '&:hover': {
      backgroundColor: 'var(--editor-panel-hover)',
    },
  },
  title: {
    fontSize: 'var(--editor-font-size-base)',
    fontWeight: 'var(--editor-font-weight-semibold)',
    color: 'var(--editor-text-primary)',
    letterSpacing: '0.2px',
    textTransform: 'none',
    whiteSpace: 'nowrap',
    overflow: 'visible',
    textOverflow: 'clip',
    flex: 1,
    minWidth: 0,
    marginRight: 'var(--editor-space-md)',
  },
  collapseButton: {
    minWidth: '28px',
    padding: 'var(--editor-space-xs)',
    color: 'var(--editor-text-secondary)',
    transition: 'all var(--editor-transition-fast)',
    '&:hover': {
      color: 'var(--editor-text-primary)',
      backgroundColor: 'var(--editor-panel-active)',
    },
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
    <div className={`${styles.header} aura-editor-panel__header`}>
      <span className={`${styles.title} aura-editor-panel__header-title`} title={title}>
        {title}
      </span>
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
