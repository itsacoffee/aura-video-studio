import { makeStyles, tokens, Title1, Body1, Button } from '@fluentui/react-components';
import { Home24Regular, ArrowLeft24Regular } from '@fluentui/react-icons';
import { useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { loggingService } from '../services/loggingService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '60vh',
    padding: tokens.spacingVerticalXXXL,
    textAlign: 'center',
  },
  errorCode: {
    fontSize: '120px',
    fontWeight: 'bold',
    color: tokens.colorNeutralForeground3,
    lineHeight: '1',
    marginBottom: tokens.spacingVerticalL,
  },
  title: {
    marginBottom: tokens.spacingVerticalM,
  },
  message: {
    color: tokens.colorNeutralForeground2,
    marginBottom: tokens.spacingVerticalXXL,
    maxWidth: '500px',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
    justifyContent: 'center',
  },
});

export function NotFoundPage() {
  const styles = useStyles();
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    loggingService.warn(
      `404 Page Not Found: User attempted to access non-existent route`,
      'NotFoundPage',
      'mount',
      {
        attemptedPath: location.pathname,
        search: location.search,
        hash: location.hash,
        state: location.state,
      }
    );
  }, [location]);

  const handleGoHome = () => {
    navigate('/');
  };

  const handleGoBack = () => {
    navigate(-1);
  };

  return (
    <div className={styles.container}>
      <div className={styles.errorCode}>404</div>
      <Title1 className={styles.title}>Page Not Found</Title1>
      <Body1 className={styles.message}>
        The page you&apos;re looking for doesn&apos;t exist or has been moved. Please check the URL
        or navigate back to a known page.
      </Body1>
      <div className={styles.actions}>
        <Button appearance="primary" icon={<Home24Regular />} onClick={handleGoHome} size="large">
          Go to Home
        </Button>
        <Button
          appearance="secondary"
          icon={<ArrowLeft24Regular />}
          onClick={handleGoBack}
          size="large"
        >
          Go Back
        </Button>
      </div>
    </div>
  );
}
