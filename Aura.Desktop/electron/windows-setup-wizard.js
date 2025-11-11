/**
 * Windows-specific First-Run Setup Wizard
 * Handles Windows-specific checks and configuration on first run
 */

const { dialog, shell } = require('electron');
const { spawn, exec } = require('child_process');
const path = require('path');
const fs = require('fs');
const os = require('os');

class WindowsSetupWizard {
  constructor(app, windowManager, appConfig) {
    this.app = app;
    this.windowManager = windowManager;
    this.appConfig = appConfig;
    this.isWindows = process.platform === 'win32';
    this.setupResults = {
      dotnetInstalled: false,
      dotnetVersion: null,
      firewallConfigured: false,
      dataPathConfigured: false,
      shortcutsCreated: false,
      errors: []
    };
  }

  /**
   * Run the complete Windows setup wizard
   */
  async runSetup() {
    if (!this.isWindows) {
      console.log('Not running on Windows, skipping Windows-specific setup');
      return { success: true, skipped: true };
    }

    console.log('='.repeat(60));
    console.log('Windows First-Run Setup Wizard');
    console.log('='.repeat(60));

    try {
      // Step 1: Check .NET 8 Runtime
      console.log('Step 1: Checking .NET 8 Runtime...');
      const dotnetCheck = await this.checkDotNetRuntime();
      this.setupResults.dotnetInstalled = dotnetCheck.installed;
      this.setupResults.dotnetVersion = dotnetCheck.version;

      if (!dotnetCheck.installed) {
        const shouldInstall = await this.promptDotNetInstallation(dotnetCheck);
        if (shouldInstall) {
          await this.openDotNetDownloadPage();
        } else {
          // User declined, add warning but continue
          this.setupResults.errors.push('User declined .NET 8 Runtime installation');
        }
      }

      // Step 2: Configure Data Paths
      console.log('Step 2: Configuring data paths...');
      const dataPathResult = await this.configureDataPaths();
      this.setupResults.dataPathConfigured = dataPathResult.success;

      // Step 3: Check Windows Firewall
      console.log('Step 3: Checking Windows Firewall...');
      const firewallResult = await this.checkWindowsFirewall();
      this.setupResults.firewallConfigured = firewallResult.configured;

      if (!firewallResult.configured) {
        await this.promptFirewallConfiguration(firewallResult);
      }

      // Step 4: Create Desktop Shortcuts (if not created by installer)
      console.log('Step 4: Verifying shortcuts...');
      const shortcutResult = await this.verifyShortcuts();
      this.setupResults.shortcutsCreated = shortcutResult.created;

      // Step 5: Verify Windows Compatibility
      console.log('Step 5: Checking Windows compatibility...');
      await this.checkWindowsCompatibility();

      console.log('='.repeat(60));
      console.log('Windows Setup Wizard Complete');
      console.log('='.repeat(60));
      console.log('Results:', JSON.stringify(this.setupResults, null, 2));

      // Store setup completion
      this.appConfig.set('windowsSetupComplete', true);
      this.appConfig.set('windowsSetupResults', this.setupResults);

      return {
        success: true,
        results: this.setupResults
      };

    } catch (error) {
      console.error('Windows setup wizard error:', error);
      this.setupResults.errors.push(error.message);
      
      return {
        success: false,
        error: error.message,
        results: this.setupResults
      };
    }
  }

  /**
   * Check if .NET 8 Runtime is installed
   */
  async checkDotNetRuntime() {
    return new Promise((resolve) => {
      exec('dotnet --list-runtimes', (error, stdout, stderr) => {
        if (error) {
          console.log('.NET runtime check error:', error.message);
          resolve({
            installed: false,
            version: null,
            error: 'dotnet command not found'
          });
          return;
        }

        const runtimes = stdout.toString();
        console.log('Installed .NET runtimes:\n', runtimes);

        // Check for .NET 8 Runtime (Microsoft.AspNetCore.App or Microsoft.NETCore.App)
        const aspNetCore8 = runtimes.match(/Microsoft\.AspNetCore\.App 8\.(\d+)\.(\d+)/);
        const netCore8 = runtimes.match(/Microsoft\.NETCore\.App 8\.(\d+)\.(\d+)/);

        if (aspNetCore8 || netCore8) {
          const version = aspNetCore8 ? aspNetCore8[0] : netCore8[0];
          console.log('✓ .NET 8 Runtime found:', version);
          resolve({
            installed: true,
            version: version,
            aspNetCoreFound: !!aspNetCore8,
            netCoreFound: !!netCore8
          });
        } else {
          console.log('✗ .NET 8 Runtime not found');
          resolve({
            installed: false,
            version: null,
            availableRuntimes: runtimes
          });
        }
      });
    });
  }

  /**
   * Prompt user to install .NET Runtime
   */
  async promptDotNetInstallation(dotnetCheck) {
    const mainWindow = this.windowManager.getMainWindow();
    
    const result = await dialog.showMessageBox(mainWindow, {
      type: 'warning',
      title: '.NET 8 Runtime Required',
      message: '.NET 8 Runtime is required to run Aura Video Studio',
      detail: 
        'Aura Video Studio requires the .NET 8 Runtime to function properly.\n\n' +
        'Without it, the application backend will not start and you will not be able to generate videos.\n\n' +
        'Would you like to download and install it now? This will open the Microsoft download page in your browser.',
      buttons: ['Download .NET 8', 'Skip (Not Recommended)', 'Cancel'],
      defaultId: 0,
      cancelId: 2,
      noLink: true
    });

    return result.response === 0;
  }

  /**
   * Open .NET download page
   */
  async openDotNetDownloadPage() {
    const downloadUrl = 'https://dotnet.microsoft.com/download/dotnet/8.0';
    console.log('Opening .NET download page:', downloadUrl);
    
    await shell.openExternal(downloadUrl);
    
    const mainWindow = this.windowManager.getMainWindow();
    await dialog.showMessageBox(mainWindow, {
      type: 'info',
      title: 'Install .NET 8 Runtime',
      message: 'Please install .NET 8 Runtime and restart Aura Video Studio',
      detail: 
        'The download page has been opened in your browser.\n\n' +
        'Please download and install:\n' +
        '• .NET 8 Runtime (recommended)\n' +
        '• Or ASP.NET Core 8 Runtime\n\n' +
        'After installation, restart Aura Video Studio.',
      buttons: ['OK']
    });
  }

  /**
   * Configure Windows-specific data paths
   */
  async configureDataPaths() {
    try {
      const paths = {
        // Use proper Windows AppData locations
        userData: this.app.getPath('userData'), // %LOCALAPPDATA%\aura-video-studio
        roaming: this.app.getPath('appData'),   // %APPDATA%
        local: this.app.getPath('userData'),     // %LOCALAPPDATA%\aura-video-studio
        documents: path.join(this.app.getPath('documents'), 'Aura Video Studio'),
        videos: path.join(this.app.getPath('videos'), 'Aura Studio'),
        logs: path.join(this.app.getPath('userData'), 'logs'),
        cache: path.join(this.app.getPath('userData'), 'cache'),
        temp: path.join(this.app.getPath('temp'), 'aura-video-studio')
      };

      console.log('Configured Windows data paths:');
      console.log('  User Data:', paths.userData);
      console.log('  Documents:', paths.documents);
      console.log('  Videos:', paths.videos);
      console.log('  Logs:', paths.logs);
      console.log('  Cache:', paths.cache);
      console.log('  Temp:', paths.temp);

      // Create necessary directories
      for (const [key, dirPath] of Object.entries(paths)) {
        if (!fs.existsSync(dirPath)) {
          fs.mkdirSync(dirPath, { recursive: true });
          console.log(`  ✓ Created ${key} directory:`, dirPath);
        }
      }

      // Store paths in config for later use
      this.appConfig.set('windowsPaths', paths);

      return {
        success: true,
        paths
      };

    } catch (error) {
      console.error('Error configuring data paths:', error);
      return {
        success: false,
        error: error.message
      };
    }
  }

  /**
   * Check Windows Firewall configuration
   */
  async checkWindowsFirewall() {
    return new Promise((resolve) => {
      const appPath = this.app.getPath('exe');
      const command = `netsh advfirewall firewall show rule name="Aura Video Studio"`;

      exec(command, (error, stdout, stderr) => {
        if (error || stdout.includes('No rules match')) {
          console.log('Windows Firewall rule not found');
          resolve({
            configured: false,
            needsConfiguration: true,
            appPath
          });
        } else {
          console.log('✓ Windows Firewall rule exists');
          resolve({
            configured: true,
            needsConfiguration: false,
            ruleDetails: stdout
          });
        }
      });
    });
  }

  /**
   * Prompt user to configure Windows Firewall
   */
  async promptFirewallConfiguration(firewallResult) {
    const mainWindow = this.windowManager.getMainWindow();
    
    const result = await dialog.showMessageBox(mainWindow, {
      type: 'info',
      title: 'Windows Firewall Configuration',
      message: 'Windows Firewall may block Aura Video Studio',
      detail: 
        'Aura Video Studio runs a local web server for its backend.\n\n' +
        'Windows Firewall may block this server and prevent the application from working correctly.\n\n' +
        'The installer should have added a firewall rule automatically.\n' +
        'If you experience connection issues, please add Aura Video Studio to your firewall exceptions manually.\n\n' +
        'Would you like to view firewall configuration instructions?',
      buttons: ['View Instructions', 'Skip'],
      defaultId: 0,
      cancelId: 1
    });

    if (result.response === 0) {
      await this.showFirewallInstructions(firewallResult.appPath);
    }
  }

  /**
   * Show firewall configuration instructions
   */
  async showFirewallInstructions(appPath) {
    const mainWindow = this.windowManager.getMainWindow();
    
    const command = `netsh advfirewall firewall add rule name="Aura Video Studio" dir=in action=allow program="${appPath}" enable=yes profile=any`;
    
    await dialog.showMessageBox(mainWindow, {
      type: 'info',
      title: 'Windows Firewall Configuration',
      message: 'To add Aura Video Studio to Windows Firewall:',
      detail: 
        'Option 1: Automatic (Requires Administrator)\n' +
        'Run the following command in an Administrator Command Prompt:\n\n' +
        `${command}\n\n` +
        'Option 2: Manual\n' +
        '1. Open Windows Security\n' +
        '2. Go to Firewall & network protection\n' +
        '3. Click "Allow an app through firewall"\n' +
        '4. Click "Change settings" (requires admin)\n' +
        '5. Click "Allow another app..."\n' +
        '6. Browse and select: ' + appPath + '\n' +
        '7. Click "Add"\n\n' +
        'The command has been copied to your clipboard.',
      buttons: ['OK']
    });

    // Try to copy command to clipboard (requires electron clipboard)
    try {
      const { clipboard } = require('electron');
      clipboard.writeText(command);
      console.log('Firewall command copied to clipboard');
    } catch (error) {
      console.error('Failed to copy to clipboard:', error);
    }
  }

  /**
   * Verify shortcuts are created
   */
  async verifyShortcuts() {
    try {
      const desktopPath = this.app.getPath('desktop');
      const startMenuPath = path.join(
        this.app.getPath('appData'),
        'Microsoft\\Windows\\Start Menu\\Programs'
      );

      const desktopShortcut = path.join(desktopPath, 'Aura Video Studio.lnk');
      const startMenuShortcut = path.join(startMenuPath, 'Aura Video Studio.lnk');

      const desktopExists = fs.existsSync(desktopShortcut);
      const startMenuExists = fs.existsSync(startMenuShortcut);

      console.log('Desktop shortcut exists:', desktopExists);
      console.log('Start menu shortcut exists:', startMenuExists);

      return {
        created: desktopExists || startMenuExists,
        desktop: desktopExists,
        startMenu: startMenuExists
      };

    } catch (error) {
      console.error('Error verifying shortcuts:', error);
      return {
        created: false,
        error: error.message
      };
    }
  }

  /**
   * Check Windows version compatibility
   */
  async checkWindowsCompatibility() {
    try {
      const release = os.release();
      const version = os.version();
      
      console.log('Windows version:', release);
      console.log('OS version:', version);

      // Parse Windows version (Windows 10 = 10.0, Windows 11 = 10.0.22000+)
      const versionParts = release.split('.');
      const majorVersion = parseInt(versionParts[0]);
      const buildNumber = versionParts[2] ? parseInt(versionParts[2]) : 0;

      let windowsVersion = 'Unknown';
      let compatible = true;
      let recommendation = '';

      if (majorVersion >= 10) {
        if (buildNumber >= 22000) {
          windowsVersion = 'Windows 11';
          compatible = true;
          recommendation = 'Fully compatible';
        } else {
          windowsVersion = 'Windows 10';
          compatible = true;
          recommendation = 'Compatible, but Windows 11 recommended for best experience';
        }
      } else {
        windowsVersion = `Windows ${majorVersion}`;
        compatible = false;
        recommendation = 'Windows 10 or later required';
      }

      console.log(`Detected: ${windowsVersion}`);
      console.log(`Compatible: ${compatible}`);
      console.log(`Recommendation: ${recommendation}`);

      this.appConfig.set('windowsVersion', {
        version: windowsVersion,
        release,
        buildNumber,
        compatible,
        recommendation
      });

      if (!compatible) {
        const mainWindow = this.windowManager.getMainWindow();
        await dialog.showMessageBox(mainWindow, {
          type: 'warning',
          title: 'Windows Version Warning',
          message: `${windowsVersion} detected`,
          detail: 
            'Aura Video Studio is designed for Windows 10 or later.\n\n' +
            'Your version of Windows may not be fully supported.\n\n' +
            'You may experience compatibility issues or reduced functionality.\n\n' +
            'We recommend upgrading to Windows 10 or Windows 11 for the best experience.',
          buttons: ['I Understand']
        });
      }

      return {
        windowsVersion,
        compatible,
        recommendation
      };

    } catch (error) {
      console.error('Error checking Windows compatibility:', error);
      return {
        windowsVersion: 'Unknown',
        compatible: true,
        error: error.message
      };
    }
  }

  /**
   * Get setup results
   */
  getResults() {
    return this.setupResults;
  }

  /**
   * Check if setup has been completed before
   */
  isSetupComplete() {
    return this.appConfig.get('windowsSetupComplete', false);
  }

  /**
   * Run quick compatibility check (for subsequent launches)
   */
  async quickCheck() {
    if (!this.isWindows) {
      return { compatible: true, issues: [] };
    }

    const issues = [];

    // Quick .NET check
    const dotnetCheck = await this.checkDotNetRuntime();
    if (!dotnetCheck.installed) {
      issues.push({
        type: 'error',
        message: '.NET 8 Runtime not found',
        action: 'Install .NET 8 Runtime from https://dotnet.microsoft.com/download/dotnet/8.0'
      });
    }

    return {
      compatible: issues.length === 0,
      issues
    };
  }
}

module.exports = WindowsSetupWizard;
