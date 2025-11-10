import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Title3,
  Text,
  Button,
  Card,
  CardHeader,
  Spinner,
  Badge,
  Tooltip,
} from '@fluentui/react-components';
import { Play24Regular, Settings24Regular, Rocket24Regular, Lightbulb24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { ConfigurationModal } from '../components/ConfigurationModal';
import { ConfigurationStatusCard } from '../components/ConfigurationStatusCard';
import { FirstRunDiagnostics } from '../components/FirstRunDiagnostics';
import { SystemCheckCard } from '../components/SystemCheckCard';
import { TooltipContent, TooltipWithLink } from '../components/Tooltips';
import { apiUrl } from '../config/api';
import { hasCompletedFirstRun } from '../services/firstRunService';
import { configurationStatusService } from '../services/configurationStatusService';
import type { ConfigurationStatus } from '../services/configurationStatusService';
import type { HardwareCapabilities } from '../types';

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
  firstTimeCallout: {
    marginBottom: tokens.spacingVerticalXXXL,
    padding: tokens.spacingVerticalXXL,
    borderRadius: tokens.borderRadiusLarge,
    background: `linear-gradient(135deg, ${tokens.colorNeutralBackground2} 0%, ${tokens.colorNeutralBackground1} 100%)`,
    boxShadow: tokens.shadow16,
    textAlign: 'center',
    border: `2px solid ${tokens.colorBrandStroke1}`,
  },
  calloutTitle: {
    marginBottom: tokens.spacingVerticalL,
    fontSize: '28px',
    fontWeight: tokens.fontWeightBold,
    color: tokens.colorNeutralForeground1,
  },
  calloutText: {
    fontSize: '16px',
    color: tokens.colorNeutralForeground2,
    maxWidth: '600px',
    margin: '0 auto',
    lineHeight: '1.6',
    marginBottom: tokens.spacingVerticalXL,
  },
  primaryOnboardingButton: {
    fontSize: '18px',
    padding: '16px 48px',
    height: 'auto',
    minHeight: '56px',
    fontWeight: tokens.fontWeightSemibold,
  },
  reconfigureCallout: {
    marginBottom: tokens.spacingVerticalXXL,
    padding: tokens.spacingVerticalXL,
    borderRadius: tokens.borderRadiusLarge,
    background: `linear-gradient(135deg, ${tokens.colorNeutralBackground2} 0%, ${tokens.colorNeutralBackground1} 100%)`,
    boxShadow: tokens.shadow8,
    textAlign: 'center',
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  reconfigureTitle: {
    marginBottom: tokens.spacingVerticalM,
    fontSize: '20px',
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  reconfigureText: {
    fontSize: '14px',
    color: tokens.colorNeutralForeground2,
    maxWidth: '500px',
    margin: '0 auto',
    lineHeight: '1.5',
    marginBottom: tokens.spacingVerticalL,
  },
  secondaryOnboardingButton: {
    fontSize: '16px',
    padding: '12px 32px',
    height: 'auto',
    minHeight: '48px',
    fontWeight: tokens.fontWeightSemibold,
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
  setupBanner: {
    padding: tokens.spacingVerticalXXL,
    marginBottom: tokens.spacingVerticalXXL,
    borderRadius: tokens.borderRadiusLarge,
    background: `linear-gradient(135deg, ${tokens.colorPaletteRedBackground1} 0%, ${tokens.colorPaletteOrangeBackground1} 100%)`,
    border: `3px solid ${tokens.colorPaletteRedBorder2}`,
    textAlign: 'center',
    animation: 'pulseGlow 2s ease-in-out infinite',
  },
  '@keyframes pulseGlow': {
    '0%, 100%': {
      boxShadow: `0 0 20px ${tokens.colorPaletteRedBorder1}`,
    },
    '50%': {
      boxShadow: `0 0 40px ${tokens.colorPaletteRedBorder2}`,
    },
  },
  setupBannerTitle: {
    fontSize: '32px',
    fontWeight: tokens.fontWeightBold,
    marginBottom: tokens.spacingVerticalM,
    color: tokens.colorPaletteRedForeground1,
  },
  setupBannerText: {
    fontSize: '18px',
    marginBottom: tokens.spacingVerticalXL,
    maxWidth: '700px',
    margin: '0 auto',
    lineHeight: '1.6',
  },
  quickSetupButton: {
    fontSize: '20px',
    padding: '20px 48px',
    height: 'auto',
    minHeight: '64px',
    fontWeight: tokens.fontWeightBold,
    animation: 'buttonPulse 2s ease-in-out infinite',
  },
  '@keyframes buttonPulse': {
    '0%, 100%': {
      transform: 'scale(1)',
    },
    '50%': {
      transform: 'scale(1.05)',
    },
  },
  readyBanner: {
    padding: tokens.spacingVerticalXL,
    marginBottom: tokens.spacingVerticalXXL,
    borderRadius: tokens.borderRadiusLarge,
    background: `linear-gradient(135deg, ${tokens.colorPaletteGreenBackground1} 0%, ${tokens.colorPaletteBlueBorderActive} 100%)`,
    border: `2px solid ${tokens.colorPaletteGreenBorder1}`,
    textAlign: 'center',
  },
  readyBannerTitle: {
    fontSize: '28px',
    fontWeight: tokens.fontWeightBold,
    marginBottom: tokens.spacingVerticalS,
    color: tokens.colorPaletteGreenForeground1,
  },
  disabledButtonTooltip: {
    padding: tokens.spacingVerticalM,
    maxWidth: '300px',
  },
  summaryCard: {
    padding: tokens.spacingVerticalL,
    marginTop: tokens.spacingVerticalL,
  },
  summaryGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
    gap: tokens.spacingVerticalM,
  },
  summaryItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
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
  const [isFirstRun, setIsFirstRun] = useState(false);
  const [checkingFirstRun, setCheckingFirstRun] = useState(true);
  const [configStatus, setConfigStatus] = useState<ConfigurationStatus | null>(null);
  const [showConfigModal, setShowConfigModal] = useState(false);
  const [loadingConfig, setLoadingConfig] = useState(true);

  useEffect(() => {
    // Check first-run status
    const checkFirstRunStatus = async () => {
      try {
        const completed = await hasCompletedFirstRun();
        setIsFirstRun(!completed);
      } catch (error) {
        console.error('Error checking first-run status:', error);
        setIsFirstRun(false);
      } finally {
        setCheckingFirstRun(false);
      }
    };

    checkFirstRunStatus();
  }, []);

  useEffect(() => {
    // Load configuration status
    const loadConfigStatus = async () => {
      try {
        const status = await configurationStatusService.getStatus();
        setConfigStatus(status);
      } catch (error) {
        console.error('Error loading configuration status:', error);
      } finally {
        setLoadingConfig(false);
      }
    };

    loadConfigStatus();

    // Subscribe to configuration changes
    const unsubscribe = configurationStatusService.subscribe(setConfigStatus);
    return () => unsubscribe();
  }, []);

  useEffect(() => {
    // Fetch system info on mount
    const fetchHealthData = async () => {
      try {
        const response = await fetch(apiUrl('/api/healthz'));
        if (response.ok) {
          return await response.json();
        }
        console.warn('Health check failed with status:', response.status);
        return null;
      } catch (err) {
        console.error('Failed to fetch health data:', err);
        return null;
      }
    };

    const fetchCapabilitiesData = async () => {
      try {
        const response = await fetch(apiUrl('/api/capabilities'));
        if (response.ok) {
          return await response.json();
        }
        console.warn('Capabilities check failed with status:', response.status);
        return null;
      } catch (err) {
        console.error('Failed to fetch capabilities data:', err);
        return null;
      }
    };

    Promise.all([fetchHealthData(), fetchCapabilitiesData()])
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

  const handleOpenConfigModal = () => {
    setShowConfigModal(true);
  };

  const handleCloseConfigModal = () => {
    setShowConfigModal(false);
    // Refresh configuration status after modal closes
    configurationStatusService.getStatus(true).then(setConfigStatus);
  };

  const handleConfigComplete = async () => {
    // Refresh all statuses
    await configurationStatusService.markConfigured();
    const status = await configurationStatusService.getStatus(true);
    setConfigStatus(status);
  };

  const isSystemReady = !loadingConfig && configStatus?.isConfigured;
  const needsSetup = !loadingConfig && !configStatus?.isConfigured;

  return (
    <div className={styles.container}>
      {/* Configuration Modal */}
      <ConfigurationModal
        open={showConfigModal}
        onClose={handleCloseConfigModal}
        onComplete={handleConfigComplete}
        allowDismiss={!needsSetup}
      />

      {/* Setup Required Banner - Show if not configured */}
      {needsSetup && (
        <div className={styles.setupBanner}>
          <div className={styles.setupBannerTitle}>
            ⚠️ SETUP REQUIRED ⚠️
          </div>
          <Text className={styles.setupBannerText}>
            <strong>Configuration is incomplete.</strong> You must complete the setup wizard before you can create videos.
            <br />
            <br />
            ✓ Configure AI providers for script generation
            <br />
            ✓ Install FFmpeg for video rendering
            <br />
            ✓ Set up your workspace for saving projects
            <br />
            <br />
            This quick 3-5 minute setup will get you ready to create amazing videos!
          </Text>
          <Button
            appearance="primary"
            size="large"
            className={styles.quickSetupButton}
            icon={<Rocket24Regular />}
            onClick={handleOpenConfigModal}
          >
            🚀 Quick Setup - Start Now
          </Button>
        </div>
      )}

      {/* Ready Banner - Show if configured */}
      {isSystemReady && (
        <div className={styles.readyBanner}>
          <div className={styles.readyBannerTitle}>
            ✅ System Ready!
          </div>
          <Text style={{ fontSize: '16px', marginTop: tokens.spacingVerticalS }}>
            Your system is configured and ready to create videos. All checks passed!
          </Text>
        </div>
      )}

      {/* Configuration Status Card */}
      {!loadingConfig && (
        <ConfigurationStatusCard
          onConfigure={handleOpenConfigModal}
          showConfigureButton={true}
          autoRefresh={true}
        />
      )}

      {/* System Health Check */}
      <SystemCheckCard autoRetry={true} retryInterval={30000} />

      <div className={styles.hero}>
        <Title1 className={styles.title}>Welcome to Aura Video Studio</Title1>
        <Text size={500} className={styles.subtitle}>
          Create professional YouTube videos with AI-powered automation
        </Text>

        <div className={styles.actions}>
          {needsSetup ? (
            <Tooltip
              content={
                <div className={styles.disabledButtonTooltip}>
                  <Text weight="semibold" style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}>
                    Setup Required
                  </Text>
                  <Text>
                    You must complete the configuration wizard before creating videos. 
                    Click &quot;Quick Setup&quot; above to get started.
                  </Text>
                </div>
              }
              relationship="description"
            >
              <Button
                appearance="primary"
                size="large"
                icon={<Play24Regular />}
                disabled={true}
              >
                Create Video
              </Button>
            </Tooltip>
          ) : (
            <Tooltip
              content={<TooltipWithLink content={TooltipContent.welcomeCreateNew} />}
              relationship="description"
            >
              <Button
                appearance="primary"
                size="large"
                icon={<Play24Regular />}
                onClick={() => navigate('/create')}
              >
                Create Video
              </Button>
            </Tooltip>
          )}
          <Tooltip
            content={<TooltipWithLink content={TooltipContent.welcomeSettings} />}
            relationship="description"
          >
            <Button size="large" icon={<Settings24Regular />} onClick={() => navigate('/settings')}>
              Settings
            </Button>
          </Tooltip>
          <Button 
            size="large" 
            icon={<Lightbulb24Regular />} 
            onClick={handleOpenConfigModal}
            appearance={needsSetup ? "primary" : "secondary"}
          >
            {needsSetup ? "Setup Wizard" : "Reconfigure"}
          </Button>
        </div>
      </div>

      {/* Configuration Summary - Show when ready */}
      {isSystemReady && configStatus && (
        <Card className={styles.summaryCard}>
          <CardHeader
            header={<Title2>Configuration Summary</Title2>}
            description="Your current system configuration"
          />
          <div className={styles.summaryGrid}>
            <div className={styles.summaryItem}>
              <Text weight="semibold">AI Providers</Text>
              <Text size={300}>
                {configStatus.details.configuredProviders.length > 0
                  ? configStatus.details.configuredProviders.join(', ')
                  : 'None'}
              </Text>
            </div>
            <div className={styles.summaryItem}>
              <Text weight="semibold">FFmpeg</Text>
              <Text size={300}>
                {configStatus.details.ffmpegVersion
                  ? `Version ${configStatus.details.ffmpegVersion}`
                  : 'Not detected'}
              </Text>
            </div>
            <div className={styles.summaryItem}>
              <Text weight="semibold">GPU</Text>
              <Text size={300}>
                {configStatus.details.gpuAvailable ? 'Available' : 'Not available'}
              </Text>
            </div>
            <div className={styles.summaryItem}>
              <Text weight="semibold">Disk Space</Text>
              <Text size={300}>
                {configStatus.details.diskSpaceAvailable
                  ? `${configStatus.details.diskSpaceAvailable.toFixed(1)} GB available`
                  : 'Unknown'}
              </Text>
            </div>
          </div>
        </Card>
      )}

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
