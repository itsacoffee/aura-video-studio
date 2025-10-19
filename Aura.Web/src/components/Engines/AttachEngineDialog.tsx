import { useState } from 'react';
import { apiUrl } from '../../../config/api';
import {
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Field,
  Input,
  Textarea,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { LinkAdd24Regular } from '@fluentui/react-icons';
import { useEnginesStore } from '../../state/engines';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
});

interface AttachEngineDialogProps {
  engineId: string;
  engineName: string;
}

export function AttachEngineDialog({ engineId, engineName }: AttachEngineDialogProps) {
  const styles = useStyles();
  const [open, setOpen] = useState(false);
  const [installPath, setInstallPath] = useState('');
  const [executablePath, setExecutablePath] = useState('');
  const [port, setPort] = useState('');
  const [healthCheckUrl, setHealthCheckUrl] = useState('');
  const [notes, setNotes] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { attachEngine } = useEnginesStore();

  const handleSubmit = async () => {
    setError(null);
    
    if (!installPath.trim()) {
      setError('Install path is required');
      return;
    }

    setIsSubmitting(true);
    try {
      await attachEngine({
        engineId,
        installPath: installPath.trim(),
        executablePath: executablePath.trim() || undefined,
        port: port ? parseInt(port) : undefined,
        healthCheckUrl: healthCheckUrl.trim() || undefined,
        notes: notes.trim() || undefined,
      });
      
      // Reset form and close
      setInstallPath('');
      setExecutablePath('');
      setPort('');
      setHealthCheckUrl('');
      setNotes('');
      setOpen(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to attach engine');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => setOpen(data.open)}>
      <DialogTrigger disableButtonEnhancement>
        <Button appearance="secondary" icon={<LinkAdd24Regular />}>
          Attach Existing Install
        </Button>
      </DialogTrigger>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Attach Existing {engineName} Installation</DialogTitle>
          <DialogContent className={styles.content}>
            <Field label="Install Path" required hint="Absolute path to the installation directory">
              <Input
                value={installPath}
                onChange={(e) => setInstallPath(e.target.value)}
                placeholder="e.g., C:\Tools\sd-webui or /opt/stable-diffusion-webui"
              />
            </Field>

            <Field label="Executable Path (optional)" hint="Path to the main executable or start script">
              <Input
                value={executablePath}
                onChange={(e) => setExecutablePath(e.target.value)}
                placeholder="e.g., webui.bat or python main.py"
              />
            </Field>

            <Field label="Port (optional)" hint="Web UI port number">
              <Input
                type="number"
                value={port}
                onChange={(e) => setPort(e.target.value)}
                placeholder="e.g., 7860"
              />
            </Field>

            <Field label="Health Check URL (optional)" hint="URL endpoint for health checks">
              <Input
                value={healthCheckUrl}
                onChange={(e) => setHealthCheckUrl(e.target.value)}
                placeholder="e.g., http://localhost:7860/health"
              />
            </Field>

            <Field label="Notes (optional)" hint="Any notes about this installation">
              <Textarea
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="e.g., Custom installation with specific models"
                rows={3}
              />
            </Field>

            {error && (
              <div style={{ color: tokens.colorPaletteRedForeground1 }}>
                {error}
              </div>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={() => setOpen(false)} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button appearance="primary" onClick={handleSubmit} disabled={isSubmitting}>
              {isSubmitting ? 'Attaching...' : 'Attach'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
