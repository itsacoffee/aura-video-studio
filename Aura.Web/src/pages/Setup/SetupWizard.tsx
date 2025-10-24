import { useState, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Title1,
  Title2,
  Title3,
  Text,
  Button,
  Card,
  Badge,
  Spinner,
  Input,
  ProgressBar,
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  ChevronRight24Regular,
  ChevronLeft24Regular,
  Dismiss24Regular,
  Warning24Regular,
  Info24Regular,
  CheckmarkCircle24Filled,
} from '@fluentui/react-icons';
import { useNavigate } from 'react-router-dom';
import { API_BASE_URL } from '../../config/api';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    padding: tokens.spacingVerticalXXL,
    maxWidth: '900px',
    margin: '0 auto',
  },
  header: {
    textAlign: 'center',
    marginBottom: tokens.spacingVerticalXXL,
  },
  content: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    overflowY: 'auto',
  },
  footer: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: tokens.spacingVerticalXXL,
    paddingTop: tokens.spacingVerticalL,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  steps: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  step: {
    width: '60px',
    height: '4px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: '2px',
  },
  stepActive: {
    backgroundColor: tokens.colorBrandBackground,
  },
  tierCard: {
    cursor: 'pointer',
    padding: tokens.spacingVerticalL,
    transition: 'all 0.2s',
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  tierCardSelected: {
    border: `2px solid ${tokens.colorBrandBackground}`,
  },
  checkList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  checkItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
  },
  apiKeyInput: {
    width: '100%',
    marginBottom: tokens.spacingVerticalM,
  },
});

type Tier = 'free' | 'balanced' | 'pro';

interface DependencyStatus {
  ffmpegInstalled: boolean;
  ffmpegVersion?: string;
  piperTtsInstalled: boolean;
  piperTtsPath?: string;
  ollamaInstalled: boolean;
  ollamaVersion?: string;
  nvidiaDriversInstalled: boolean;
  nvidiaDriverVersion?: string;
  diskSpaceGB: number;
  internetConnected: boolean;
  ffmpegInstallationRequired: boolean;
  piperTtsInstallationRequired: boolean;
  ollamaInstallationRequired: boolean;
}

interface InstallProgress {
  percentage: number;
  status: string;
  currentFile: string;
  bytesDownloaded: number;
  totalBytes: number;
}

export function SetupWizard() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [step, setStep] = useState(0);
  const [tier, setTier] = useState<Tier>('free');
  const [depStatus, setDepStatus] = useState<DependencyStatus | null>(null);
  const [isDetecting, setIsDetecting] = useState(false);
  const [isInstalling, setIsInstalling] = useState(false);
  const [installProgress, setInstallProgress] = useState<InstallProgress | null>(null);
  const [apiKeys, setApiKeys] = useState({
    openai: '',
    gemini: '',
    elevenlabs: '',
    playht: '',
  });

  const totalSteps = tier === 'pro' ? 6 : 5;

  useEffect(() => {
    // Check if rerun mode
    const params = new URLSearchParams(window.location.search);
    if (params.get('rerun') === 'true') {
      // Load existing config
      loadExistingConfig();
    }
  }, []);

  const loadExistingConfig = async () => {
    // In a real implementation, would load from config endpoint
  };

  const detectDependencies = async () => {
    setIsDetecting(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/setup/detect`);
      const data = await response.json();
      setDepStatus(data);
    } catch (error) {
      console.error('Failed to detect dependencies:', error);
    } finally {
      setIsDetecting(false);
    }
  };

  const installAll = async () => {
    setIsInstalling(true);
    setInstallProgress({
      percentage: 0,
      status: 'Starting installation...',
      currentFile: '',
      bytesDownloaded: 0,
      totalBytes: 0,
    });

    try {
      const eventSource = new EventSource(`${API_BASE_URL}/api/setup/install/all`);

      eventSource.addEventListener('progress', (event) => {
        const progress = JSON.parse(event.data);
        setInstallProgress(progress);
      });

      eventSource.addEventListener('complete', (event) => {
        JSON.parse(event.data);
        setIsInstalling(false);
        eventSource.close();
        // Re-detect after installation
        detectDependencies();
      });

      eventSource.addEventListener('error', (event) => {
        console.error('Installation error:', event);
        setIsInstalling(false);
        eventSource.close();
      });
    } catch (error) {
      console.error('Failed to install:', error);
      setIsInstalling(false);
    }
  };

  const saveConfig = async () => {
    try {
      await fetch(`${API_BASE_URL}/api/setup/save-config`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          tier,
          setupCompleted: true,
          setupVersion: '1.0',
          apiKeys: tier === 'pro' ? apiKeys : null,
        }),
      });
    } catch (error) {
      console.error('Failed to save config:', error);
    }
  };

  const handleNext = async () => {
    if (step === 2) {
      // Detect dependencies when entering step 3
      await detectDependencies();
    }
    if (step === totalSteps - 1) {
      // Save config and complete
      await saveConfig();
      navigate('/');
    } else {
      setStep(step + 1);
    }
  };

  const handleBack = () => {
    if (step > 0) {
      setStep(step - 1);
    }
  };

  const renderStep0 = () => (
    <>
      <Title2>Welcome to Aura Video Studio!</Title2>
      <Text>
        Create professional videos with AI-powered script generation, text-to-speech, and automated
        video composition.
      </Text>
      <Card>
        <Title3>System Requirements</Title3>
        <ul>
          <li>2GB free disk space (minimum)</li>
          <li>4GB RAM (recommended)</li>
          <li>2 CPU cores (minimum)</li>
        </ul>
        <Text>Operating System: {navigator.platform}</Text>
      </Card>
    </>
  );

  const renderStep1 = () => (
    <>
      <Title2>Choose Your Tier</Title2>
      <Text>Select the features you want to use:</Text>

      <Card
        className={`${styles.tierCard} ${tier === 'free' ? styles.tierCardSelected : ''}`}
        onClick={() => setTier('free')}
      >
        <Title3>🆓 Free Tier</Title3>
        <Text>
          <strong>No AI Required</strong>
          <ul>
            <li>Rule-based script generation</li>
            <li>Windows built-in TTS</li>
            <li>Stock images from Pexels/Unsplash</li>
            <li>FFmpeg only (auto-install available)</li>
          </ul>
          <Badge appearance="filled" color="success">
            0 GB disk space
          </Badge>
          <Badge appearance="outline">Fast processing</Badge>
        </Text>
      </Card>

      <Card
        className={`${styles.tierCard} ${tier === 'balanced' ? styles.tierCardSelected : ''}`}
        onClick={() => setTier('balanced')}
      >
        <Title3>⚖️ Balanced Tier (Local AI)</Title3>
        <Text>
          <strong>Privacy-Focused Local AI</strong>
          <ul>
            <li>Ollama for local LLM (Llama 3)</li>
            <li>Piper TTS for natural voices</li>
            <li>Stock images</li>
            <li>FFmpeg (auto-install available)</li>
          </ul>
          <Badge appearance="filled" color="warning">
            ~8 GB disk space
          </Badge>
          <Badge appearance="outline">Medium processing</Badge>
        </Text>
      </Card>

      <Card
        className={`${styles.tierCard} ${tier === 'pro' ? styles.tierCardSelected : ''}`}
        onClick={() => setTier('pro')}
      >
        <Title3>⭐ Pro Tier (Cloud AI)</Title3>
        <Text>
          <strong>Best Quality with Cloud APIs</strong>
          <ul>
            <li>OpenAI/Gemini for advanced LLM</li>
            <li>ElevenLabs/PlayHT for premium TTS</li>
            <li>Stable Diffusion for custom images</li>
            <li>FFmpeg (auto-install available)</li>
          </ul>
          <Badge appearance="filled" color="danger">
            API keys required
          </Badge>
          <Badge appearance="outline">Fastest processing</Badge>
        </Text>
      </Card>
    </>
  );

  const renderStep2 = () => (
    <>
      <Title2>Dependency Check</Title2>
      {isDetecting ? (
        <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}>
          <Spinner label="Detecting dependencies..." />
        </div>
      ) : depStatus ? (
        <div className={styles.checkList}>
          <div className={styles.checkItem}>
            {depStatus.ffmpegInstalled ? (
              <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
            ) : (
              <Dismiss24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
            )}
            <div style={{ flex: 1 }}>
              <Text weight="semibold">FFmpeg (Required)</Text>
              <Text size={200}>
                {depStatus.ffmpegInstalled
                  ? `Installed: ${depStatus.ffmpegVersion}`
                  : 'Not installed'}
              </Text>
            </div>
          </div>

          {tier === 'balanced' && (
            <>
              <div className={styles.checkItem}>
                {depStatus.piperTtsInstalled ? (
                  <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
                ) : (
                  <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />
                )}
                <div style={{ flex: 1 }}>
                  <Text weight="semibold">Piper TTS (Recommended)</Text>
                  <Text size={200}>
                    {depStatus.piperTtsInstalled
                      ? `Installed: ${depStatus.piperTtsPath}`
                      : 'Not installed'}
                  </Text>
                </div>
              </div>

              <div className={styles.checkItem}>
                {depStatus.ollamaInstalled ? (
                  <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
                ) : (
                  <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />
                )}
                <div style={{ flex: 1 }}>
                  <Text weight="semibold">Ollama (Recommended)</Text>
                  <Text size={200}>
                    {depStatus.ollamaInstalled
                      ? `Installed: ${depStatus.ollamaVersion}`
                      : 'Not installed - please install separately from ollama.ai'}
                  </Text>
                </div>
              </div>
            </>
          )}

          <div className={styles.checkItem}>
            <Info24Regular style={{ color: tokens.colorBrandForeground1 }} />
            <div style={{ flex: 1 }}>
              <Text weight="semibold">Disk Space</Text>
              <Text size={200}>{depStatus.diskSpaceGB.toFixed(2)} GB available</Text>
            </div>
          </div>

          <div className={styles.checkItem}>
            {depStatus.internetConnected ? (
              <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />
            ) : (
              <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />
            )}
            <div style={{ flex: 1 }}>
              <Text weight="semibold">Internet Connection</Text>
              <Text size={200}>{depStatus.internetConnected ? 'Connected' : 'Not connected'}</Text>
            </div>
          </div>
        </div>
      ) : (
        <Button appearance="primary" onClick={detectDependencies}>
          Start Detection
        </Button>
      )}

      {depStatus &&
        (depStatus.ffmpegInstallationRequired ||
          (tier === 'balanced' && depStatus.piperTtsInstallationRequired)) && (
          <Button appearance="primary" onClick={installAll} disabled={isInstalling}>
            {isInstalling ? 'Installing...' : 'Install All Missing Dependencies'}
          </Button>
        )}
    </>
  );

  const renderStep3 = () => (
    <>
      <Title2>Installation Progress</Title2>
      {isInstalling && installProgress ? (
        <div>
          <ProgressBar value={installProgress.percentage / 100} />
          <Text>{installProgress.status}</Text>
          {installProgress.currentFile && (
            <Text size={200}>File: {installProgress.currentFile}</Text>
          )}
          {installProgress.totalBytes > 0 && (
            <Text size={200}>
              {(installProgress.bytesDownloaded / 1024 / 1024).toFixed(2)} MB /{' '}
              {(installProgress.totalBytes / 1024 / 1024).toFixed(2)} MB
            </Text>
          )}
        </div>
      ) : (
        <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}>
          <CheckmarkCircle24Filled
            style={{ fontSize: '64px', color: tokens.colorPaletteGreenForeground1 }}
          />
          <Title3>Installation Complete!</Title3>
          <Text>All required dependencies have been installed successfully.</Text>
        </div>
      )}
    </>
  );

  const renderStep4Pro = () => (
    <>
      <Title2>API Keys (Optional)</Title2>
      <Text>
        Enter your API keys for cloud services. You can skip this and add them later in Settings.
      </Text>

      <div className={styles.apiKeyInput}>
        <Text weight="semibold">OpenAI API Key</Text>
        <Input
          type="password"
          value={apiKeys.openai}
          onChange={(e) => setApiKeys({ ...apiKeys, openai: e.target.value })}
          placeholder="sk-..."
        />
      </div>

      <div className={styles.apiKeyInput}>
        <Text weight="semibold">Gemini API Key</Text>
        <Input
          type="password"
          value={apiKeys.gemini}
          onChange={(e) => setApiKeys({ ...apiKeys, gemini: e.target.value })}
          placeholder="AI..."
        />
      </div>

      <div className={styles.apiKeyInput}>
        <Text weight="semibold">ElevenLabs API Key</Text>
        <Input
          type="password"
          value={apiKeys.elevenlabs}
          onChange={(e) => setApiKeys({ ...apiKeys, elevenlabs: e.target.value })}
          placeholder="..."
        />
      </div>

      <div className={styles.apiKeyInput}>
        <Text weight="semibold">PlayHT API Key</Text>
        <Input
          type="password"
          value={apiKeys.playht}
          onChange={(e) => setApiKeys({ ...apiKeys, playht: e.target.value })}
          placeholder="..."
        />
      </div>
    </>
  );

  const renderStep5 = () => (
    <>
      <div style={{ textAlign: 'center', padding: tokens.spacingVerticalXXL }}>
        <CheckmarkCircle24Filled
          style={{ fontSize: '64px', color: tokens.colorPaletteGreenForeground1 }}
        />
        <Title2>Setup Complete!</Title2>
        <Text>You&apos;re all set to start creating videos with Aura Video Studio.</Text>

        {depStatus && (
          <Card style={{ marginTop: tokens.spacingVerticalL, textAlign: 'left' }}>
            <Title3>Installed Components</Title3>
            <ul>
              {depStatus.ffmpegInstalled && <li>FFmpeg {depStatus.ffmpegVersion}</li>}
              {depStatus.piperTtsInstalled && <li>Piper TTS</li>}
              {depStatus.ollamaInstalled && <li>Ollama {depStatus.ollamaVersion}</li>}
            </ul>
          </Card>
        )}

        <div
          style={{
            marginTop: tokens.spacingVerticalXL,
            display: 'flex',
            gap: tokens.spacingHorizontalM,
            justifyContent: 'center',
          }}
        >
          <Button appearance="primary" onClick={() => navigate('/create')}>
            Create Your First Video
          </Button>
          <Button onClick={() => navigate('/')}>Go to Dashboard</Button>
        </div>
      </div>
    </>
  );

  const renderCurrentStep = () => {
    if (tier === 'pro') {
      switch (step) {
        case 0:
          return renderStep0();
        case 1:
          return renderStep1();
        case 2:
          return renderStep2();
        case 3:
          return renderStep3();
        case 4:
          return renderStep4Pro();
        case 5:
          return renderStep5();
        default:
          return null;
      }
    } else {
      switch (step) {
        case 0:
          return renderStep0();
        case 1:
          return renderStep1();
        case 2:
          return renderStep2();
        case 3:
          return renderStep3();
        case 4:
          return renderStep5();
        default:
          return null;
      }
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>Aura Video Studio Setup</Title1>
        <div className={styles.steps}>
          {Array.from({ length: totalSteps }).map((_, i) => (
            <div key={i} className={`${styles.step} ${i <= step ? styles.stepActive : ''}`} />
          ))}
        </div>
      </div>

      <div className={styles.content}>{renderCurrentStep()}</div>

      <div className={styles.footer}>
        <Button icon={<ChevronLeft24Regular />} onClick={handleBack} disabled={step === 0}>
          Back
        </Button>
        <Text>
          {step + 1} / {totalSteps}
        </Text>
        <Button
          appearance="primary"
          icon={step === totalSteps - 1 ? <Checkmark24Regular /> : <ChevronRight24Regular />}
          iconPosition="after"
          onClick={handleNext}
          disabled={(step === 2 && !depStatus) || (step === 3 && isInstalling)}
        >
          {step === totalSteps - 1 ? 'Finish' : 'Next'}
        </Button>
      </div>
    </div>
  );
}
