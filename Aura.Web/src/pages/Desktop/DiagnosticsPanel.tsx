/**
 * Desktop Diagnostics Panel
 * 
 * Comprehensive system diagnostics for troubleshooting:
 * - System requirements validation
 * - Dependency checks (FFmpeg, Ollama, .NET)
 * - Provider connectivity tests
 * - Performance metrics
 * - Log viewer
 */

import {
  makeStyles,
  tokens,
  Title2,
  Title3,
  Text,
  Button,
  Card,
  CardHeader,
  Spinner,
  Badge,
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
  Tooltip,
  Link,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Filled,
  ErrorCircle24Regular,
  Warning24Regular,
  Info24Regular,
  ArrowSync24Regular,
  Copy24Regular,
  FolderOpen24Regular,
  Clipboard24Regular,
} from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import { useNotifications } from '../../components/Notifications/Toasts';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1200px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXL,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  section: {
    marginBottom: tokens.spacingVerticalXL,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(350px, 1fr))',
    gap: tokens.spacingHorizontalL,
  },
  card: {
    padding: tokens.spacingVerticalL,
  },
  statusRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalS,
  },
  logViewer: {
    backgroundColor: tokens.colorNeutralBackground3,
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: '12px',
    maxHeight: '400px',
    overflow: 'auto',
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-all',
  },
  actionButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalM,
  },
});

interface DiagnosticResult {
  name: string;
  status: 'pass' | 'warning' | 'fail' | 'checking';
  message?: string;
  details?: string;
  action?: {
    label: string;
    onClick: () => void;
  };
}

interface SystemInfo {
  platform: string;
  architecture: string;
  osDescription: string;
  frameworkDescription: string;
  paths: Record<string, string>;
}

export function DiagnosticsPanel() {
  const styles = useStyles();
  const { showSuccess, showError, showInfo } = useNotifications();
  
  const [isChecking, setIsChecking] = useState(false);
  const [systemInfo, setSystemInfo] = useState<SystemInfo | null>(null);
  const [requirements, setRequirements] = useState<DiagnosticResult[]>([]);
  const [dependencies, setDependencies] = useState<DiagnosticResult[]>([]);
  const [providers, setProviders] = useState<DiagnosticResult[]>([]);
  const [logs, setLogs] = useState<string>('');

  const isElectron = typeof window !== 'undefined' && 
    (window as any).electron?.platform?.isElectron;

  useEffect(() => {
    runDiagnostics();
  }, []);

  const runDiagnostics = async () => {
    setIsChecking(true);
    
    try {
      // Run all diagnostics in parallel
      await Promise.all([
        checkSystemInfo(),
        checkRequirements(),
        checkDependencies(),
        checkProviders(),
      ]);
      
      showSuccess('Diagnostics complete');
    } catch (error) {
      showError('Failed to run diagnostics');
      console.error(error);
    } finally {
      setIsChecking(false);
    }
  };

  const checkSystemInfo = async () => {
    try {
      const response = await fetch('/api/setup/system-info');
      const data = await response.json();
      setSystemInfo(data);
    } catch (error) {
      console.error('Failed to get system info:', error);
    }
  };

  const checkRequirements = async () => {
    try {
      const response = await fetch('/api/setup/validate-requirements');
      const data = await response.json();
      
      setRequirements(
        data.requirements.map((req: any) => ({
          name: req.name,
          status: req.status,
          message: `${req.detected} (${req.required} required)`,
          details: req.message,
        }))
      );
    } catch (error) {
      console.error('Failed to check requirements:', error);
      setRequirements([
        {
          name: 'System Requirements',
          status: 'fail',
          message: 'Failed to check system requirements',
        },
      ]);
    }
  };

  const checkDependencies = async () => {
    const results: DiagnosticResult[] = [];
    
    // Check FFmpeg
    try {
      const response = await fetch('/api/health/ffmpeg');
      const data = await response.json();
      results.push({
        name: 'FFmpeg',
        status: data.isAvailable ? 'pass' : 'fail',
        message: data.isAvailable 
          ? `Version ${data.version} at ${data.path}` 
          : 'Not found',
        action: !data.isAvailable ? {
          label: 'Install FFmpeg',
          onClick: () => window.location.hash = '#/setup',
        } : undefined,
      });
    } catch {
      results.push({
        name: 'FFmpeg',
        status: 'fail',
        message: 'Failed to check FFmpeg status',
      });
    }
    
    // Check Ollama
    try {
      const response = await fetch('/api/setup/ollama-status');
      const data = await response.json();
      results.push({
        name: 'Ollama',
        status: data.available ? 'pass' : 'warning',
        message: data.available ? 'Available at localhost:11434' : 'Not running',
        details: data.available ? undefined : 'Optional: Install Ollama to run AI models locally',
      });
    } catch {
      results.push({
        name: 'Ollama',
        status: 'warning',
        message: 'Not detected',
      });
    }
    
    // Check .NET Backend
    try {
      const response = await fetch('/api/health');
      results.push({
        name: '.NET Backend',
        status: response.ok ? 'pass' : 'fail',
        message: response.ok ? 'Running' : 'Not responding',
      });
    } catch {
      results.push({
        name: '.NET Backend',
        status: 'fail',
        message: 'Cannot connect to backend',
      });
    }
    
    setDependencies(results);
  };

  const checkProviders = async () => {
    const results: DiagnosticResult[] = [];
    
    // Check configured providers
    try {
      const response = await fetch('/api/settings');
      const settings = await response.json();
      
      // Check if any LLM provider is configured
      const llmProviders = ['OpenAI', 'Anthropic', 'Google', 'Ollama'];
      const configuredLlm = llmProviders.some(p => 
        settings[`${p.toLowerCase()}ApiKey`] || settings[`${p.toLowerCase()}Enabled`]
      );
      
      results.push({
        name: 'LLM Provider',
        status: configuredLlm ? 'pass' : 'warning',
        message: configuredLlm ? 'Configured' : 'No provider configured',
        action: !configuredLlm ? {
          label: 'Configure Provider',
          onClick: () => window.location.hash = '#/settings',
        } : undefined,
      });
      
      // Check TTS configuration
      results.push({
        name: 'TTS Provider',
        status: settings.ttsProvider ? 'pass' : 'warning',
        message: settings.ttsProvider ? `Using ${settings.ttsProvider}` : 'Not configured',
      });
      
    } catch (error) {
      results.push({
        name: 'Provider Configuration',
        status: 'fail',
        message: 'Failed to check provider configuration',
      });
    }
    
    setProviders(results);
  };

  const copyDiagnostics = async () => {
    const diagnosticsText = `
Aura Video Studio Diagnostics Report
Generated: ${new Date().toISOString()}

=== System Information ===
Platform: ${systemInfo?.platform}
Architecture: ${systemInfo?.architecture}
OS: ${systemInfo?.osDescription}
Framework: ${systemInfo?.frameworkDescription}

=== System Requirements ===
${requirements.map(r => `${r.name}: ${r.status.toUpperCase()} - ${r.message}`).join('\n')}

=== Dependencies ===
${dependencies.map(d => `${d.name}: ${d.status.toUpperCase()} - ${d.message}`).join('\n')}

=== Providers ===
${providers.map(p => `${p.name}: ${p.status.toUpperCase()} - ${p.message}`).join('\n')}

=== Logs ===
${logs || 'No logs available'}
    `.trim();
    
    try {
      await navigator.clipboard.writeText(diagnosticsText);
      showSuccess('Diagnostics copied to clipboard');
    } catch (error) {
      showError('Failed to copy diagnostics');
    }
  };

  const openLogsFolder = async () => {
    if (!isElectron) {
      showInfo('Logs folder is only accessible in the desktop app');
      return;
    }
    
    try {
      const electron = (window as any).electron;
      const paths = await electron.app.getPaths();
      await electron.shell.openPath(paths.userData + '/logs');
    } catch (error) {
      showError('Failed to open logs folder');
    }
  };

  const renderStatusIcon = (status: DiagnosticResult['status']) => {
    switch (status) {
      case 'pass':
        return <CheckmarkCircle24Filled style={{ color: tokens.colorPaletteGreenForeground1 }} />;
      case 'warning':
        return <Warning24Regular style={{ color: tokens.colorPaletteYellowForeground1 }} />;
      case 'fail':
        return <ErrorCircle24Regular style={{ color: tokens.colorPaletteRedForeground1 }} />;
      case 'checking':
        return <Spinner size="tiny" />;
    }
  };

  const renderDiagnosticCard = (title: string, items: DiagnosticResult[]) => (
    <Card className={styles.card}>
      <CardHeader header={<Title3>{title}</Title3>} />
      
      {items.map((item, index) => (
        <div key={index} className={styles.statusRow}>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS, flex: 1 }}>
            {renderStatusIcon(item.status)}
            <div>
              <Text weight="semibold">{item.name}</Text>
              <Text size={300} style={{ display: 'block', color: tokens.colorNeutralForeground3 }}>
                {item.message}
              </Text>
              {item.details && (
                <Text size={200} style={{ display: 'block', color: tokens.colorNeutralForeground4 }}>
                  {item.details}
                </Text>
              )}
            </div>
          </div>
          
          {item.action && (
            <Button size="small" onClick={item.action.onClick}>
              {item.action.label}
            </Button>
          )}
        </div>
      ))}
    </Card>
  );

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div>
          <Title2>System Diagnostics</Title2>
          <Text>Comprehensive system and dependency checks</Text>
        </div>
        
        <div className={styles.actionButtons}>
          <Button
            icon={<ArrowSync24Regular />}
            onClick={runDiagnostics}
            disabled={isChecking}
          >
            {isChecking ? 'Checking...' : 'Refresh'}
          </Button>
          <Button
            icon={<Copy24Regular />}
            onClick={copyDiagnostics}
          >
            Copy Report
          </Button>
          {isElectron && (
            <Button
              icon={<FolderOpen24Regular />}
              onClick={openLogsFolder}
            >
              Open Logs
            </Button>
          )}
        </div>
      </div>

      {/* System Information */}
      {systemInfo && (
        <div className={styles.section}>
          <Card className={styles.card}>
            <CardHeader header={<Title3>System Information</Title3>} />
            <div style={{ display: 'grid', gridTemplateColumns: '200px 1fr', gap: tokens.spacingVerticalS }}>
              <Text weight="semibold">Platform:</Text>
              <Text>{systemInfo.platform}</Text>
              
              <Text weight="semibold">Architecture:</Text>
              <Text>{systemInfo.architecture}</Text>
              
              <Text weight="semibold">OS Version:</Text>
              <Text>{systemInfo.osDescription}</Text>
              
              <Text weight="semibold">.NET Version:</Text>
              <Text>{systemInfo.frameworkDescription}</Text>
            </div>
          </Card>
        </div>
      )}

      {/* Diagnostic Results */}
      <div className={styles.section}>
        <div className={styles.grid}>
          {renderDiagnosticCard('System Requirements', requirements)}
          {renderDiagnosticCard('Dependencies', dependencies)}
          {renderDiagnosticCard('Provider Configuration', providers)}
        </div>
      </div>

      {/* Detailed Logs */}
      <div className={styles.section}>
        <Card className={styles.card}>
          <CardHeader header={<Title3>Recent Logs</Title3>} />
          <Accordion collapsible>
            <AccordionItem value="logs">
              <AccordionHeader>View Application Logs</AccordionHeader>
              <AccordionPanel>
                <div className={styles.logViewer}>
                  {logs || 'No logs available. Check the logs folder for detailed logs.'}
                </div>
              </AccordionPanel>
            </AccordionItem>
          </Accordion>
        </Card>
      </div>

      {/* Help Resources */}
      <div className={styles.section}>
        <Card className={styles.card}>
          <CardHeader header={<Title3>Need Help?</Title3>} />
          <Text>
            If you're experiencing issues, try these resources:
          </Text>
          <ul>
            <li>
              <Link href="https://docs.aura-video-studio.com/troubleshooting" target="_blank">
                Troubleshooting Guide
              </Link>
            </li>
            <li>
              <Link href="https://github.com/coffee285/aura-video-studio/issues" target="_blank">
                Report an Issue
              </Link>
            </li>
            <li>
              <Link href="https://discord.gg/aura-video-studio" target="_blank">
                Community Discord
              </Link>
            </li>
          </ul>
        </Card>
      </div>
    </div>
  );
}
