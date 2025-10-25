import { useState, useEffect } from 'react';
import { apiUrl } from '../config/api';
import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Text,
  Button,
  Card,
  CardHeader,
  Spinner,
  Badge,
} from '@fluentui/react-components';
import { useNavigate } from 'react-router-dom';
import { Play24Regular, Settings24Regular } from '@fluentui/react-icons';
import type { HardwareCapabilities } from '../types';
import { SystemCheckCard } from '../components/SystemCheckCard';
import { FirstRunDiagnostics } from '../components/FirstRunDiagnostics';

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
    margin: '0 auto',
  },
  hero: {
    textAlign: 'center',
    marginBottom: tokens.spacingVerticalXXXL,
    padding: tokens.spacingVerticalXXL,
    borderRadius: tokens.borderRadiusLarge,
    background: `linear-gradient(135deg, ${tokens.colorNeutralBackground2} 0%, ${tokens.colorNeutralBackground1} 100%)`,
    boxShadow: '0 4px 12px rgba(0, 0, 0, 0.1)',
  },
  title: {
    marginBottom: tokens.spacingVerticalM,
    display: 'block',
    background: `linear-gradient(135deg, ${tokens.colorBrandForeground1} 0%, ${tokens.colorPalettePurpleForeground2} 100%)`,
    WebkitBackgroundClip: 'text',
    WebkitTextFillColor: 'transparent',
    backgroundClip: 'text',
  },
  subtitle: {
    color: tokens.colorNeutralForeground2,
    marginBottom: tokens.spacingVerticalXL,
    display: 'block',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'center',
    marginTop: tokens.spacingVerticalXL,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalL,
    marginTop: tokens.spacingVerticalXXL,
  },
  card: {
    height: '100%',
    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
    cursor: 'default',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: '0 8px 20px rgba(0, 0, 0, 0.15)',
    },
  },
  statusBadge: {
    marginLeft: tokens.spacingHorizontalS,
  },
  cardContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
});

interface HealthStatus {
  status: string;
  timestamp: string;
}

export function WelcomePage() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [capabilities, setCapabilities] = useState<HardwareCapabilities | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Fetch system info on mount
    Promise.all([
      fetch(apiUrl('/api/healthz')).then((res) => res.json()),
      fetch(apiUrl('/api/capabilities')).then((res) => res.json()),
    ])
      .then(([healthData, capData]) => {
        setHealth(healthData);
        setCapabilities(capData);
        setLoading(false);
      })
      .catch((err) => {
        console.error('Failed to fetch system info:', err);
        setLoading(false);
      });
  }, []);

  return (
    <div className={styles.container}>
      {/* First-Run Diagnostics */}
      <FirstRunDiagnostics
        autoRun={true}
        onNeedsSetup={() => {
          // Optionally redirect to onboarding if critical issues found
        }}
      />

      {/* System Health Check */}
      <SystemCheckCard autoRetry={true} retryInterval={30000} />

      <div className={styles.hero}>
        <Title1 className={styles.title}>Welcome to Aura Video Studio</Title1>
        <Text size={500} className={styles.subtitle}>
          Create professional YouTube videos with AI-powered automation
        </Text>

        <div className={styles.actions}>
          <Button
            appearance="primary"
            size="large"
            icon={<Play24Regular />}
            onClick={() => navigate('/create')}
          >
            Create Video
          </Button>
          <Button size="large" icon={<Settings24Regular />} onClick={() => navigate('/settings')}>
            Settings
          </Button>
          <Button size="large" onClick={() => navigate('/onboarding')}>
            Run Onboarding
          </Button>
        </div>
      </div>

      <div className={styles.grid}>
        <Card className={styles.card}>
          <CardHeader
            header={<Title2>System Status</Title2>}
            description={
              loading ? (
                <Spinner size="small" label="Checking..." />
              ) : health ? (
                <Text>
                  API is {health.status}
                  <Badge appearance="filled" color="success" className={styles.statusBadge}>
                    Online
                  </Badge>
                </Text>
              ) : (
                <Text>
                  API unavailable
                  <Badge appearance="filled" color="danger" className={styles.statusBadge}>
                    Offline
                  </Badge>
                </Text>
              )
            }
          />
        </Card>

        <Card className={styles.card}>
          <CardHeader
            header={<Title2>Hardware</Title2>}
            description={
              loading ? (
                <Spinner size="small" label="Detecting..." />
              ) : capabilities ? (
                <div className={styles.cardContent}>
                  <Text>Tier: {capabilities.tier}</Text>
                  <Text>CPU: {capabilities.cpu.threads} threads</Text>
                  <Text>RAM: {capabilities.ram.gb} GB</Text>
                  {capabilities.gpu && (
                    <Text>
                      GPU: {capabilities.gpu.model} ({capabilities.gpu.vramGB} GB)
                    </Text>
                  )}
                </div>
              ) : (
                <Text>Hardware detection failed</Text>
              )
            }
          />
        </Card>

        <Card className={styles.card}>
          <CardHeader
            header={<Title2>Features Available</Title2>}
            description={
              loading ? (
                <Spinner size="small" />
              ) : capabilities ? (
                <div className={styles.cardContent}>
                  <Text>NVENC: {capabilities.enableNVENC ? '✓ Yes' : '✗ No'}</Text>
                  <Text>Stable Diffusion: {capabilities.enableSD ? '✓ Yes' : '✗ No'}</Text>
                  <Text>Offline Mode: {capabilities.offlineOnly ? '✓ Active' : '✗ Inactive'}</Text>
                </div>
              ) : (
                <Text>No capability data</Text>
              )
            }
          />
        </Card>
      </div>
    </div>
  );
}
