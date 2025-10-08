import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Text,
  Button,
  Card,
} from '@fluentui/react-components';
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
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXXL,
  },
});

export function DashboardPage() {
  const styles = useStyles();
  const navigate = useNavigate();

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Project Dashboard</Title1>
        <Button 
          appearance="primary"
          icon={<Add24Regular />}
          onClick={() => navigate('/create')}
        >
          New Project
        </Button>
      </div>

      <Card className={styles.emptyState}>
        <Title2>No projects yet</Title2>
        <Text>Create your first video project to get started</Text>
        <div style={{ marginTop: tokens.spacingVerticalL }}>
          <Button 
            appearance="primary"
            onClick={() => navigate('/create')}
          >
            Create Project
          </Button>
        </div>
      </Card>
    </div>
  );
}
