import {
  makeStyles,
  tokens,
  Title1,
  Text,
} from '@fluentui/react-components';
import { RenderPanel } from '../components/RenderPanel';

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
});

export function RenderPage() {
  const styles = useStyles();

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Render & Export</Title1>
        <Text className={styles.subtitle}>
          Configure render settings and manage your video export queue
        </Text>
      </div>

      <RenderPanel />
    </div>
  );
}
