import { makeStyles, tokens, Title1, Title2, Text, Button, Card } from '@fluentui/react-components';
import { Add24Regular } from '@fluentui/react-icons';
import { useNavigate } from 'react-router-dom';

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalXXL,
    flexWrap: 'wrap',
    gap: tokens.spacingVerticalM,
  },
  headerContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXXL,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: tokens.spacingVerticalL,
  },
});

export function DashboardPage() {
  const styles = useStyles();
  const navigate = useNavigate();

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <Title1>Project Dashboard</Title1>
          <Text className={styles.subtitle}>Manage all your video projects in one place</Text>
        </div>
        <Button
          appearance="primary"
          icon={<Add24Regular />}
          onClick={() => navigate('/create')}
          size="large"
        >
          New Project
        </Button>
      </div>

      <Card className={styles.emptyState}>
        <Title2>No projects yet</Title2>
        <Text>Create your first video project to get started with Aura Studio</Text>
        <Button appearance="primary" onClick={() => navigate('/create')} size="large">
          Create Your First Project
        </Button>
      </Card>
    </div>
  );
}
