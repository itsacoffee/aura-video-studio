const { spawnSync } = require('child_process');
const path = require('path');

const isWindows = process.platform === 'win32';

if (!isWindows) {
  console.log('[ffmpeg] Skipping bundled FFmpeg download (Windows-only).');
  process.exit(0);
}

const scriptPath = path.join(__dirname, 'ensure-ffmpeg.ps1');
const result = spawnSync('powershell.exe', ['-ExecutionPolicy', 'Bypass', '-File', scriptPath], {
  stdio: 'inherit',
});

if (result.error) {
  console.error('[ffmpeg] Failed to execute ensure-ffmpeg.ps1:', result.error.message);
  process.exit(result.status ?? 1);
}

process.exit(result.status ?? 0);

