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
} from '@fluentui/react-components';
import {
  Checkmark24Regular,
  ChevronRight24Regular,
  ChevronLeft24Regular,
  Play24Regular,
  Settings24Regular,
  VideoClip24Regular,
} from '@fluentui/react-icons';
import { useNavigate } from 'react-router-dom';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
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
  stepCompleted: {
    backgroundColor: tokens.colorPaletteGreenBackground2,
  },
  modeCard: {
    cursor: 'pointer',
    padding: tokens.spacingVerticalL,
    transition: 'all 0.2s',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  hardwareInfo: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: tokens.spacingVerticalM,
  },
  installList: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: tokens.spacingVerticalS,
  },
  validationItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
  },
  successCard: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
  },
});

type WizardMode = 'free' | 'local' | 'pro';

interface HardwareInfo {
  gpu?: string;
  vram?: number;
  canRunSD: boolean;
  recommendation: string;
}

interface InstallItem {
  id: string;
  name: string;
  required: boolean;
  installed: boolean;
  installing: boolean;
}

export function FirstRunWizard() {
  const styles = useStyles();
  const navigate = useNavigate();
  const [step, setStep] = useState(0);
  const [mode, setMode] = useState<WizardMode>('free');
  const [hardware, setHardware] = useState<HardwareInfo | null>(null);
  const [detectingHardware, setDetectingHardware] = useState(false);
  const [installItems, setInstallItems] = useState<InstallItem[]>([
    { id: 'ffmpeg', name: 'FFmpeg (Video encoding)', required: true, installed: false, installing: false },
    { id: 'ollama', name: 'Ollama (Local AI)', required: false, installed: false, installing: false },
    { id: 'stable-diffusion', name: 'Stable Diffusion WebUI', required: false, installed: false, installing: false },
  ]);
  const [validating, setValidating] = useState(false);
  const [validationComplete, setValidationComplete] = useState(false);

  const totalSteps = 4;

  useEffect(() => {
    // Check if this is truly first run
    const hasSeenOnboarding = localStorage.getItem('hasSeenOnboarding');
    if (hasSeenOnboarding === 'true') {
      // User has already seen onboarding, redirect to home
      navigate('/');
    }
  }, [navigate]);

  const detectHardware = async () => {
    setDetectingHardware(true);
    try {
      // Call hardware detection API
      const response = await fetch('/api/hardware/probe');
      if (response.ok) {
        const data = await response.json();
        
        // Parse hardware info
        const gpuInfo = data.gpu || 'Unknown GPU';
        const vramGB = data.vramGB || 0;
        const canRunSD = data.enableLocalDiffusion || false;
        
        let recommendation = '';
        if (canRunSD) {
          recommendation = `Your ${gpuInfo} with ${vramGB}GB VRAM can run Stable Diffusion locally!`;
        } else {
          recommendation = `Your system doesn't meet requirements for local Stable Diffusion. We recommend using Stock images or Pro cloud providers.`;
        }
        
        setHardware({
          gpu: gpuInfo,
          vram: vramGB,
          canRunSD,
          recommendation,
        });
      }
    } catch (error) {
      console.error('Hardware detection failed:', error);
      setHardware({
        canRunSD: false,
        recommendation: 'Could not detect hardware. We recommend starting with Free-only mode using Stock images.',
      });
    }
    setDetectingHardware(false);
  };

  const installItem = async (itemId: string) => {
    setInstallItems(prev => prev.map(item =>
      item.id === itemId ? { ...item, installing: true } : item
    ));

    // Simulate installation (in real app, this would call the download API)
    await new Promise(resolve => setTimeout(resolve, 2000));

    setInstallItems(prev => prev.map(item =>
      item.id === itemId ? { ...item, installing: false, installed: true } : item
    ));
  };

  const runValidation = async () => {
    setValidating(true);
    
    // Run preflight check
    try {
      const profileMap: Record<WizardMode, string> = {
        free: 'Free-Only',
        local: 'Balanced Mix',
        pro: 'Pro-Max',
      };
      
      const response = await fetch(`/api/preflight?profile=${profileMap[mode]}`);
      if (response.ok) {
        const report = await response.json();
        setValidationComplete(report.ok);
      }
    } catch (error) {
      console.error('Validation failed:', error);
    }
    
    setValidating(false);
  };

  const completeOnboarding = () => {
    localStorage.setItem('hasSeenOnboarding', 'true');
    navigate('/create');
  };

  const handleNext = async () => {
    if (step === 1 && !hardware) {
      // Detect hardware before moving to step 2
      await detectHardware();
    }
    
    if (step === 2) {
      // Install required items
      const requiredItems = installItems.filter(item => item.required && !item.installed);
      for (const item of requiredItems) {
        await installItem(item.id);
      }
    }
    
    if (step === 3) {
      // Run validation
      await runValidation();
    }
    
    if (step < totalSteps - 1) {
      setStep(step + 1);
    }
  };

  const handleBack = () => {
    if (step > 0) {
      setStep(step - 1);
    }
  };

  const handleSkip = () => {
    localStorage.setItem('hasSeenOnboarding', 'true');
    navigate('/');
  };

  const renderStep0 = () => (
    <>
      <Title2>Welcome to Aura Video Studio!</Title2>
      <Text>Let's get you set up in just a few steps. Choose your preferred mode:</Text>
      
      <div style={{ display: 'flex', flexDirection: 'column', gap: tokens.spacingVerticalM }}>
        <Card
          className={styles.modeCard}
          onClick={() => setMode('free')}
          style={mode === 'free' ? { 
            borderColor: tokens.colorBrandBackground, 
            borderWidth: '2px',
            borderStyle: 'solid'
          } : {}}
        >
          <Title3>🆓 Free-Only Mode</Title3>
          <Text>
            Uses free, always-available providers:
            <ul>
              <li>Rule-based script generation</li>
              <li>Windows built-in text-to-speech</li>
              <li>Stock images from Pexels/Unsplash</li>
            </ul>
            Best for: Getting started quickly with zero setup
          </Text>
        </Card>

        <Card
          className={styles.modeCard}
          onClick={() => setMode('local')}
          style={mode === 'local' ? { 
            borderColor: tokens.colorBrandBackground, 
            borderWidth: '2px',
            borderStyle: 'solid'
          } : {}}
        >
          <Title3>💻 Local Mode</Title3>
          <Text>
            Uses local AI engines for privacy and offline work:
            <ul>
              <li>Ollama for script generation</li>
              <li>Local Piper/Mimic3 TTS</li>
              <li>Stable Diffusion for visuals (requires NVIDIA GPU)</li>
            </ul>
            Best for: Privacy-conscious users with capable hardware
          </Text>
        </Card>

        <Card
          className={styles.modeCard}
          onClick={() => setMode('pro')}
          style={mode === 'pro' ? { 
            borderColor: tokens.colorBrandBackground, 
            borderWidth: '2px',
            borderStyle: 'solid'
          } : {}}
        >
          <Title3>⭐ Pro Mode</Title3>
          <Text>
            Uses premium cloud APIs for best quality:
            <ul>
              <li>OpenAI GPT-4 for scripts</li>
              <li>ElevenLabs for voices</li>
              <li>Stability AI/Runway for visuals</li>
            </ul>
            Best for: Professional content creators (requires API keys)
          </Text>
        </Card>
      </div>
    </>
  );

  const renderStep1 = () => (
    <>
      <Title2>Hardware Detection</Title2>
      
      {detectingHardware ? (
        <Card>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
            <Spinner size="small" />
            <Text>Detecting your hardware capabilities...</Text>
          </div>
        </Card>
      ) : hardware ? (
        <div className={styles.hardwareInfo}>
          <Card>
            <Title3>System Information</Title3>
            {hardware.gpu && <Text>GPU: {hardware.gpu}</Text>}
            {hardware.vram && <Text>VRAM: {hardware.vram}GB</Text>}
            <Text style={{ marginTop: tokens.spacingVerticalM }}>
              <strong>Recommendation:</strong> {hardware.recommendation}
            </Text>
          </Card>

          {!hardware.canRunSD && mode === 'local' && (
            <Card>
              <Badge appearance="filled" color="warning">⚠ Note</Badge>
              <Text style={{ marginTop: tokens.spacingVerticalS }}>
                Your system doesn't meet the requirements for local Stable Diffusion. 
                We'll use Stock images as a fallback, or you can add cloud Pro providers later.
              </Text>
            </Card>
          )}
        </div>
      ) : (
        <Card>
          <Text>Click Next to detect your hardware...</Text>
        </Card>
      )}
    </>
  );

  const renderStep2 = () => (
    <>
      <Title2>Install Required Components</Title2>
      <Text>We'll help you install the necessary tools for your chosen mode.</Text>

      <div className={styles.installList}>
        {installItems.map(item => (
          <Card key={item.id} className={styles.validationItem}>
            <div style={{ width: '24px' }}>
              {item.installed ? (
                <Checkmark24Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
              ) : item.installing ? (
                <Spinner size="tiny" />
              ) : null}
            </div>
            <div style={{ flex: 1 }}>
              <Text weight="semibold">{item.name}</Text>
              {item.required && <Badge size="small" color="danger">Required</Badge>}
            </div>
            {!item.installed && !item.installing && (
              <Button
                size="small"
                appearance="primary"
                onClick={() => installItem(item.id)}
              >
                Install
              </Button>
            )}
          </Card>
        ))}
      </div>

      <Card>
        <Text>
          💡 Tip: You can always install additional engines later from the Downloads page.
        </Text>
      </Card>
    </>
  );

  const renderStep3 = () => (
    <>
      <Title2>Validation & Demo</Title2>
      
      {validating ? (
        <Card>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
            <Spinner size="small" />
            <Text>Running preflight checks...</Text>
          </div>
        </Card>
      ) : validationComplete ? (
        <div className={styles.successCard}>
          <Checkmark24Regular style={{ fontSize: '64px', color: tokens.colorPaletteGreenForeground1 }} />
          <Title1 style={{ marginTop: tokens.spacingVerticalL }}>All Set!</Title1>
          <Text style={{ marginTop: tokens.spacingVerticalM }}>
            Your system is ready to create amazing videos. Let's create your first project!
          </Text>
          <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, justifyContent: 'center', marginTop: tokens.spacingVerticalXL }}>
            <Button
              appearance="primary"
              size="large"
              icon={<VideoClip24Regular />}
              onClick={completeOnboarding}
            >
              Create My First Video
            </Button>
            <Button
              appearance="secondary"
              size="large"
              icon={<Settings24Regular />}
              onClick={() => {
                localStorage.setItem('hasSeenOnboarding', 'true');
                navigate('/settings');
              }}
            >
              Go to Settings
            </Button>
          </div>
        </div>
      ) : (
        <Card>
          <Text>Click Next to validate your setup...</Text>
        </Card>
      )}
    </>
  );

  const renderStepContent = () => {
    switch (step) {
      case 0:
        return renderStep0();
      case 1:
        return renderStep1();
      case 2:
        return renderStep2();
      case 3:
        return renderStep3();
      default:
        return null;
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title1>First-Run Setup</Title1>
        <div className={styles.steps}>
          {Array.from({ length: totalSteps }).map((_, i) => (
            <div
              key={i}
              className={`${styles.step} ${i === step ? styles.stepActive : ''} ${i < step ? styles.stepCompleted : ''}`}
            />
          ))}
        </div>
      </div>

      <div className={styles.content}>
        {renderStepContent()}
      </div>

      {!validationComplete && (
        <div className={styles.footer}>
          <Button
            appearance="subtle"
            onClick={handleSkip}
          >
            Skip Setup
          </Button>

          <div style={{ display: 'flex', gap: tokens.spacingHorizontalM }}>
            {step > 0 && (
              <Button
                appearance="secondary"
                icon={<ChevronLeft24Regular />}
                onClick={handleBack}
                disabled={validating || detectingHardware}
              >
                Back
              </Button>
            )}
            <Button
              appearance="primary"
              icon={step < totalSteps - 1 ? <ChevronRight24Regular /> : <Play24Regular />}
              iconPosition="after"
              onClick={handleNext}
              disabled={validating || detectingHardware}
            >
              {step < totalSteps - 1 ? 'Next' : 'Validate'}
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
