import {
  makeStyles,
  tokens,
  Title1,
  Text,
} from '@fluentui/react-components';
import { TimelineView } from '../components/Timeline/TimelineView';
import { OverlayPanel } from '../components/Overlays/OverlayPanel';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    overflow: 'hidden',
  },
  header: {
    padding: tokens.spacingVerticalXXL,
    paddingBottom: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
  content: {
    flex: 1,
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    padding: tokens.spacingVerticalL,
    overflow: 'hidden',
  },
  timeline: {
    flex: 3,
    overflow: 'hidden',
  },
  sidebar: {
    flex: 1,
    minWidth: '300px',
    maxWidth: '400px',
    overflow: 'auto',
  },
});

export function TimelinePage() {
  const styles = useStyles();

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Timeline Editor</Title1>
        <Text className={styles.subtitle}>
          Edit clips, add overlays, and create chapter markers
        </Text>
      </div>

      <div className={styles.content}>
        <div className={styles.timeline}>
          <TimelineView />
        </div>
        <div className={styles.sidebar}>
          <OverlayPanel />
        </div>
      </div>
    </div>
  );
}
