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
  Tooltip,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
} from '@fluentui/react-components';
import {
  Play24Regular,
  Settings24Regular,
  Rocket24Regular,
  Lightbulb24Regular,
  Checkmark24Regular,
  Warning24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { ConfigurationModal } from '../components/ConfigurationModal';
import { ConfigurationStatusCard } from '../components/ConfigurationStatusCard';
import { SystemCheckCard } from '../components/SystemCheckCard';
import { TooltipContent, TooltipWithLink } from '../components/Tooltips';
import { apiUrl } from '../config/api';
import { configurationStatusService } from '../services/configurationStatusService';
import type { ConfigurationStatus } from '../services/configurationStatusService';
import { hasCompletedFirstRun, getLocalFirstRunStatus } from '../services/firstRunService';
import type { HardwareCapabilities } from '../types';

const useStyles = makeStyles({
  container: {
    maxWidth: '1200px',
    margin: '0 auto',
    padding: tokens.spacingVerticalM,
  },
  hero: {
    textAlign: 'center',
    marginBottom: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalL,
    borderRadius: tokens.borderRadiusLarge,
    background: `linear-gradient(135deg, ${tokens.colorNeutralBackground2} 0%, ${tokens.colorNeutralBackground1} 100%)`,
    boxShadow: '0 4px 12px rgba(0, 0, 0, 0.1)',
  },
  title: {
    marginBottom: tokens.spacingVerticalS,
    display: 'block',
    background: `linear-gradient(135deg, ${tokens.colorBrandForeground1} 0%, ${tokens.colorPalettePurpleForeground2} 100%)`,
    WebkitBackgroundClip: 'text',
    WebkitTextFillColor: 'transparent',
    backgroundClip: 'text',
  },
  subtitle: {
    color: tokens.colorNeutralForeground2,
    marginBottom: tokens.spacingVerticalM,
    display: 'block',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'center',
    marginTop: tokens.spacingVerticalM,
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
    gap: tokens.spacingVerticalM,
    marginTop: tokens.spacingVerticalL,
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
    marginBottom: tokens.spacingVerticalL,
  },
  setupBannerActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
    justifyContent: 'center',
    flexWrap: 'wrap',
  },
  setupBannerList: {
    marginTop: tokens.spacingVerticalM,
    marginBottom: tokens.spacingVerticalM,
    paddingLeft: tokens.spacingHorizontalXL,
    textAlign: 'left',
    maxWidth: '600px',
    margin: '0 auto',
  },
  readyBanner: {
    marginBottom: tokens.spacingVerticalL,
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
  const location = useLocation();
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [capabilities, setCapabilities] = useState<HardwareCapabilities | null>(null);
  const [loading, setLoading] = useState(true);
  const [_isFirstRun, setIsFirstRun] = useState(false);
  const [_checkingFirstRun, setCheckingFirstRun] = useState(true);
  const [isCheckingSetup, setIsCheckingSetup] = useState(true);
  const [configStatus, setConfigStatus] = useState<ConfigurationStatus | null>(null);
  const [showConfigModal, setShowConfigModal] = useState(false);
  const [loadingConfig, setLoadingConfig] = useState(true);

  useEffect(() => {
    // Check first-run status and redirect to setup if not complete
    const checkSetupStatus = async () => {
      setIsCheckingSetup(true);
      try {
        const completed = await hasCompletedFirstRun();
        if (!completed) {
          // Redirect to setup wizard if first-run not completed
          navigate('/setup', { replace: true });
          return;
        }
        setIsFirstRun(!completed);
        setIsCheckingSetup(false);
      } catch (error) {
        console.error('Failed to check setup status:', error);
        // Retry after 2 seconds if backend is temporarily unavailable
        setTimeout(checkSetupStatus, 2000);
      } finally {
        setCheckingFirstRun(false);
      }
    };

    checkSetupStatus();
  }, [navigate]);

  useEffect(() => {
    // Load configuration status - force refresh on mount to get latest status
    const loadConfigStatus = async () => {
      try {
        // Force refresh to ensure we get the latest status after setup completion
        const status = await configurationStatusService.getStatus(true);
        setConfigStatus(status);
        console.info('[WelcomePage] Configuration status loaded:', {
          isConfigured: status.isConfigured,
          checks: status.checks,
        });
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

  // Refresh configuration status when page becomes visible (e.g., returning from setup wizard)
  useEffect(() => {
    const handleVisibilityChange = async () => {
      if (document.visibilityState === 'visible' && !isCheckingSetup) {
        console.info('[WelcomePage] Page became visible, refreshing configuration status');
        try {
          const status = await configurationStatusService.getStatus(true);
          setConfigStatus(status);
          console.info('[WelcomePage] Configuration status refreshed:', {
            isConfigured: status.isConfigured,
            checks: status.checks,
          });
        } catch (error) {
          console.error('[WelcomePage] Error refreshing configuration status:', error);
        }
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => document.removeEventListener('visibilitychange', handleVisibilityChange);
  }, [isCheckingSetup]);

  // Refresh configuration status when navigating to this page (e.g., after completing setup)
  useEffect(() => {
    if (location.pathname === '/welcome' || location.pathname === '/') {
      console.info('[WelcomePage] Welcome page mounted/visited, refreshing configuration status');
      const refreshStatus = async () => {
        try {
          const status = await configurationStatusService.getStatus(true);
          setConfigStatus(status);
          setLoadingConfig(false);
          console.info('[WelcomePage] Configuration status refreshed on navigation:', {
            isConfigured: status.isConfigured,
            checks: status.checks,
            hasCompletedWizard: getLocalFirstRunStatus(),
          });
        } catch (error) {
          console.error('[WelcomePage] Error refreshing configuration status on navigation:', error);
          setLoadingConfig(false);
        }
      };
      refreshStatus();
    }
  }, [location.pathname]);

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
    // Force a complete refresh of configuration status after setup completion
    console.info('[WelcomePage] Setup completed, refreshing configuration status...');
    
    // Clear any cached status
    await configurationStatusService.resetConfiguration();
    
    // Wait a moment for backend to update
    await new Promise((resolve) => setTimeout(resolve, 500));
    
    // Get fresh status with force refresh
    const status = await configurationStatusService.getStatus(true);
    setConfigStatus(status);
    
    console.info('[WelcomePage] Configuration status after setup:', {
      isConfigured: status.isConfigured,
      checks: status.checks,
    });
    
    // If still not configured, wait a bit longer and retry (backend might need time to update)
    if (!status.isConfigured) {
      console.warn('[WelcomePage] Status still shows not configured, retrying after delay...');
      await new Promise((resolve) => setTimeout(resolve, 1000));
      const retryStatus = await configurationStatusService.getStatus(true);
      setConfigStatus(retryStatus);
      console.info('[WelcomePage] Configuration status after retry:', {
        isConfigured: retryStatus.isConfigured,
        checks: retryStatus.checks,
      });
    }
  };

  const isSystemReady = !loadingConfig && configStatus?.isConfigured;
  const needsSetup = !loadingConfig && !configStatus?.isConfigured;

  // Show loading spinner while checking setup status
  if (isCheckingSetup) {
    return (
      <div
        style={{
          height: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Spinner size="large" label="Checking setup status..." />
      </div>
    );
  }

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
          <MessageBar intent="warning" icon={<Warning24Regular />}>
            <MessageBarBody>
              <MessageBarTitle>Setup Required</MessageBarTitle>
              <Text>
                Complete the quick setup to start creating videos. Configure AI providers, install
                FFmpeg, and set up your workspace.
              </Text>
            </MessageBarBody>
          </MessageBar>
        </div>
      )}

      {/* Ready Banner - Show if configured */}
      {isSystemReady && (
        <div className={styles.readyBanner}>
          <MessageBar intent="success" icon={<Checkmark24Regular />}>
            <MessageBarBody>
              <MessageBarTitle>System Ready!</MessageBarTitle>
              <Text>Your system is configured and ready to create videos. All checks passed!</Text>
            </MessageBarBody>
          </MessageBar>
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
                  <Text
                    weight="semibold"
                    style={{ display: 'block', marginBottom: tokens.spacingVerticalS }}
                  >
                    Setup Required
                  </Text>
                  <Text>
                    You must complete the configuration wizard before creating videos. Click
                    &quot;Quick Setup&quot; above to get started.
                  </Text>
                </div>
              }
              relationship="description"
            >
              <Button appearance="primary" size="large" icon={<Play24Regular />} disabled={true}>
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
            appearance={needsSetup ? 'primary' : 'secondary'}
          >
            {needsSetup ? 'Setup Wizard' : 'Reconfigure'}
          </Button>
        </div>
      </div>

      {/* System Summary - Compact overview */}
      {isSystemReady && (
        <Card className={styles.summaryCard}>
          <CardHeader
            header={<Title2>System Overview</Title2>}
            description="Quick status of your system"
          />
          <div className={styles.summaryGrid}>
            <div className={styles.summaryItem}>
              <Text weight="semibold">API Status</Text>
              <Text size={300}>
                {loading ? (
                  <Spinner size="tiny" />
                ) : health ? (
                  <>
                    {health.status}
                    <Badge
                      appearance="filled"
                      color="success"
                      style={{ marginLeft: tokens.spacingHorizontalXS }}
                    >
                      Online
                    </Badge>
                  </>
                ) : (
                  <>
                    Unavailable
                    <Badge
                      appearance="filled"
                      color="danger"
                      style={{ marginLeft: tokens.spacingHorizontalXS }}
                    >
                      Offline
                    </Badge>
                  </>
                )}
              </Text>
            </div>
            <div className={styles.summaryItem}>
              <Text weight="semibold">Hardware</Text>
              <Text size={300}>
                {loading ? (
                  <Spinner size="tiny" />
                ) : capabilities ? (
                  <>
                    Tier {capabilities.tier} • {capabilities.cpu.threads} threads •{' '}
                    {capabilities.ram.gb}GB RAM
                  </>
                ) : (
                  'Detection failed'
                )}
              </Text>
            </div>
            <div className={styles.summaryItem}>
              <Text weight="semibold">AI Providers</Text>
              <Text size={300}>
                {configStatus.details.configuredProviders.length > 0
                  ? configStatus.details.configuredProviders.join(', ')
                  : 'None configured'}
              </Text>
            </div>
            <div className={styles.summaryItem}>
              <Text weight="semibold">FFmpeg</Text>
              <Text size={300}>
                {configStatus.details.ffmpegVersion
                  ? `v${configStatus.details.ffmpegVersion}`
                  : 'Not detected'}
              </Text>
            </div>
            {capabilities?.gpu && (
              <div className={styles.summaryItem}>
                <Text weight="semibold">GPU</Text>
                <Text size={300}>
                  {capabilities.gpu.model} ({capabilities.gpu.vramGB}GB)
                  {capabilities.enableNVENC && ' • NVENC'}
                </Text>
              </div>
            )}
            {configStatus.details.diskSpaceAvailable && (
              <div className={styles.summaryItem}>
                <Text weight="semibold">Disk Space</Text>
                <Text size={300}>
                  {configStatus.details.diskSpaceAvailable.toFixed(1)} GB available
                </Text>
              </div>
            )}
          </div>
        </Card>
      )}
    </div>
  );
}
