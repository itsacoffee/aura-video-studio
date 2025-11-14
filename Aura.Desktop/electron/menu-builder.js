/**
 * Menu Builder Module
 * Creates application menu for Windows/Mac/Linux
 */

const { Menu, shell, dialog } = require('electron');
const path = require('path');
const { validateCommandPayload, getCommandMetadata } = require('./menu-command-map');

class MenuBuilder {
  constructor(app, windowManager, appConfig, isDev) {
    this.app = app;
    this.windowManager = windowManager;
    this.appConfig = appConfig;
    this.isDev = isDev;
  }

  /**
   * Build and set application menu
   */
  buildMenu() {
    const template = [
      this._buildFileMenu(),
      this._buildEditMenu(),
      this._buildViewMenu(),
      this._buildToolsMenu(),
      this._buildHelpMenu()
    ];

    // On macOS, add app menu as first item
    if (process.platform === 'darwin') {
      template.unshift(this._buildAppMenu());
    }

    const menu = Menu.buildFromTemplate(template);
    Menu.setApplicationMenu(menu);
    
    return menu;
  }

  /**
   * Build app menu (macOS only)
   */
  _buildAppMenu() {
    return {
      label: this.app.getName(),
      submenu: [
        {
          label: `About ${this.app.getName()}`,
          click: () => this._showAbout()
        },
        { type: 'separator' },
        {
          label: 'Preferences',
          accelerator: 'Cmd+,',
          click: () => this._openPreferences()
        },
        { type: 'separator' },
        {
          label: 'Services',
          role: 'services'
        },
        { type: 'separator' },
        {
          label: `Hide ${this.app.getName()}`,
          accelerator: 'Cmd+H',
          role: 'hide'
        },
        {
          label: 'Hide Others',
          accelerator: 'Cmd+Alt+H',
          role: 'hideOthers'
        },
        {
          label: 'Show All',
          role: 'unhide'
        },
        { type: 'separator' },
        {
          label: 'Quit',
          accelerator: 'Cmd+Q',
          click: () => this.app.quit()
        }
      ]
    };
  }

  /**
   * Build File menu
   */
  _buildFileMenu() {
    const submenu = [
      {
        label: 'New Project',
        accelerator: 'CmdOrCtrl+N',
        click: () => this._newProject()
      },
      {
        label: 'Open Project...',
        accelerator: 'CmdOrCtrl+O',
        click: () => this._openProject()
      },
      {
        label: 'Open Recent',
        submenu: this._buildRecentProjectsMenu()
      },
      { type: 'separator' },
      {
        label: 'Save',
        accelerator: 'CmdOrCtrl+S',
        click: () => this._saveProject()
      },
      {
        label: 'Save As...',
        accelerator: 'CmdOrCtrl+Shift+S',
        click: () => this._saveProjectAs()
      },
      { type: 'separator' },
      {
        label: 'Import...',
        submenu: [
          {
            label: 'Import Video...',
            click: () => this._importVideo()
          },
          {
            label: 'Import Audio...',
            click: () => this._importAudio()
          },
          {
            label: 'Import Images...',
            click: () => this._importImages()
          },
          {
            label: 'Import Document...',
            click: () => this._importDocument()
          }
        ]
      },
      {
        label: 'Export...',
        submenu: [
          {
            label: 'Export Video...',
            accelerator: 'CmdOrCtrl+E',
            click: () => this._exportVideo()
          },
          {
            label: 'Export Timeline...',
            click: () => this._exportTimeline()
          }
        ]
      },
      { type: 'separator' }
    ];

    // Add Exit for Windows/Linux (macOS uses Quit in app menu)
    if (process.platform !== 'darwin') {
      submenu.push({
        label: 'Exit',
        accelerator: 'Alt+F4',
        click: () => this.app.quit()
      });
    } else {
      submenu.push({
        label: 'Close Window',
        accelerator: 'Cmd+W',
        role: 'close'
      });
    }

    return {
      label: 'File',
      submenu
    };
  }

  /**
   * Build Edit menu
   */
  _buildEditMenu() {
    return {
      label: 'Edit',
      submenu: [
        {
          label: 'Undo',
          accelerator: 'CmdOrCtrl+Z',
          role: 'undo'
        },
        {
          label: 'Redo',
          accelerator: process.platform === 'darwin' ? 'Cmd+Shift+Z' : 'CmdOrCtrl+Y',
          role: 'redo'
        },
        { type: 'separator' },
        {
          label: 'Cut',
          accelerator: 'CmdOrCtrl+X',
          role: 'cut'
        },
        {
          label: 'Copy',
          accelerator: 'CmdOrCtrl+C',
          role: 'copy'
        },
        {
          label: 'Paste',
          accelerator: 'CmdOrCtrl+V',
          role: 'paste'
        },
        {
          label: 'Select All',
          accelerator: 'CmdOrCtrl+A',
          role: 'selectAll'
        },
        { type: 'separator' },
        {
          label: 'Find',
          accelerator: 'CmdOrCtrl+F',
          click: () => this._find()
        },
        { type: 'separator' },
        {
          label: 'Preferences',
          accelerator: process.platform === 'darwin' ? 'Cmd+,' : 'CmdOrCtrl+,',
          click: () => this._openPreferences()
        }
      ]
    };
  }

  /**
   * Build View menu
   */
  _buildViewMenu() {
    return {
      label: 'View',
      submenu: [
        {
          label: 'Reload',
          accelerator: 'CmdOrCtrl+R',
          click: () => {
            const window = this.windowManager.getMainWindow();
            if (window) window.reload();
          }
        },
        {
          label: 'Force Reload',
          accelerator: 'CmdOrCtrl+Shift+R',
          click: () => {
            const window = this.windowManager.getMainWindow();
            if (window) window.webContents.reloadIgnoringCache();
          }
        },
        {
          label: 'Toggle Developer Tools',
          accelerator: process.platform === 'darwin' ? 'Alt+Cmd+I' : 'Ctrl+Shift+I',
          click: () => {
            const window = this.windowManager.getMainWindow();
            if (window) window.webContents.toggleDevTools();
          }
        },
        { type: 'separator' },
        {
          label: 'Actual Size',
          accelerator: 'CmdOrCtrl+0',
          click: () => {
            const window = this.windowManager.getMainWindow();
            if (window) window.webContents.setZoomLevel(0);
          }
        },
        {
          label: 'Zoom In',
          accelerator: 'CmdOrCtrl+Plus',
          click: () => {
            const window = this.windowManager.getMainWindow();
            if (window) {
              const zoomLevel = window.webContents.getZoomLevel();
              window.webContents.setZoomLevel(zoomLevel + 0.5);
            }
          }
        },
        {
          label: 'Zoom Out',
          accelerator: 'CmdOrCtrl+-',
          click: () => {
            const window = this.windowManager.getMainWindow();
            if (window) {
              const zoomLevel = window.webContents.getZoomLevel();
              window.webContents.setZoomLevel(zoomLevel - 0.5);
            }
          }
        },
        { type: 'separator' },
        {
          label: 'Toggle Full Screen',
          accelerator: process.platform === 'darwin' ? 'Ctrl+Cmd+F' : 'F11',
          click: () => {
            const window = this.windowManager.getMainWindow();
            if (window) {
              window.setFullScreen(!window.isFullScreen());
            }
          }
        }
      ]
    };
  }

  /**
   * Build Tools menu
   */
  _buildToolsMenu() {
    return {
      label: 'Tools',
      submenu: [
        {
          label: 'Provider Settings',
          click: () => this._openProviderSettings()
        },
        {
          label: 'FFmpeg Configuration',
          click: () => this._openFFmpegConfig()
        },
        { type: 'separator' },
        {
          label: 'Clear Cache',
          click: () => this._clearCache()
        },
        {
          label: 'Reset Settings',
          click: () => this._resetSettings()
        },
        { type: 'separator' },
        {
          label: 'View Logs',
          click: () => this._viewLogs()
        },
        {
          label: 'Open Logs Folder',
          click: () => this._openLogsFolder()
        },
        { type: 'separator' },
        {
          label: 'Run Diagnostics',
          click: () => this._runDiagnostics()
        }
      ]
    };
  }

  /**
   * Build Help menu
   */
  _buildHelpMenu() {
    return {
      label: 'Help',
      submenu: [
        {
          label: 'Documentation',
          click: () => shell.openExternal('https://github.com/coffee285/aura-video-studio/wiki')
        },
        {
          label: 'Getting Started Guide',
          click: () => this._openGettingStarted()
        },
        {
          label: 'Keyboard Shortcuts',
          click: () => this._showKeyboardShortcuts()
        },
        { type: 'separator' },
        {
          label: 'Report Issue',
          click: () => shell.openExternal('https://github.com/coffee285/aura-video-studio/issues/new')
        },
        {
          label: 'View on GitHub',
          click: () => shell.openExternal('https://github.com/coffee285/aura-video-studio')
        },
        { type: 'separator' },
        {
          label: 'Check for Updates',
          click: () => this._checkForUpdates()
        },
        { type: 'separator' },
        {
          label: `About ${this.app.getName()}`,
          click: () => this._showAbout()
        }
      ]
    };
  }

  /**
   * Build recent projects submenu
   */
  _buildRecentProjectsMenu() {
    const recentProjects = this.appConfig.getRecentProjects();
    
    if (recentProjects.length === 0) {
      return [
        {
          label: 'No Recent Projects',
          enabled: false
        }
      ];
    }

    const items = recentProjects.slice(0, 10).map(project => ({
      label: project.name,
      click: () => this._openRecentProject(project.path)
    }));

    items.push(
      { type: 'separator' },
      {
        label: 'Clear Recent Projects',
        click: () => {
          this.appConfig.clearRecentProjects();
          this.buildMenu(); // Rebuild menu
        }
      }
    );

    return items;
  }

  // Menu action handlers
  _newProject() {
    this._sendToRenderer('menu:newProject');
  }

  _openProject() {
    this._sendToRenderer('menu:openProject');
  }

  _openRecentProject(projectPath) {
    this._sendToRenderer('menu:openRecentProject', { path: projectPath });
  }

  _saveProject() {
    this._sendToRenderer('menu:saveProject');
  }

  _saveProjectAs() {
    this._sendToRenderer('menu:saveProjectAs');
  }

  _importVideo() {
    this._sendToRenderer('menu:importVideo');
  }

  _importAudio() {
    this._sendToRenderer('menu:importAudio');
  }

  _importImages() {
    this._sendToRenderer('menu:importImages');
  }

  _importDocument() {
    this._sendToRenderer('menu:importDocument');
  }

  _exportVideo() {
    this._sendToRenderer('menu:exportVideo');
  }

  _exportTimeline() {
    this._sendToRenderer('menu:exportTimeline');
  }

  _find() {
    this._sendToRenderer('menu:find');
  }

  _openPreferences() {
    this._sendToRenderer('menu:openPreferences');
  }

  _openProviderSettings() {
    this._sendToRenderer('menu:openProviderSettings');
  }

  _openFFmpegConfig() {
    this._sendToRenderer('menu:openFFmpegConfig');
  }

  _clearCache() {
    dialog.showMessageBox(this.windowManager.getMainWindow(), {
      type: 'question',
      title: 'Clear Cache',
      message: 'Are you sure you want to clear the cache?',
      detail: 'This will remove all cached data and may require re-downloading some content.',
      buttons: ['Clear Cache', 'Cancel'],
      defaultId: 0,
      cancelId: 1
    }).then(result => {
      if (result.response === 0) {
        this._sendToRenderer('menu:clearCache');
      }
    });
  }

  _resetSettings() {
    dialog.showMessageBox(this.windowManager.getMainWindow(), {
      type: 'warning',
      title: 'Reset Settings',
      message: 'Are you sure you want to reset all settings to defaults?',
      detail: 'This action cannot be undone.',
      buttons: ['Reset Settings', 'Cancel'],
      defaultId: 1,
      cancelId: 1
    }).then(result => {
      if (result.response === 0) {
        this.appConfig.reset();
        this.app.relaunch();
        this.app.exit();
      }
    });
  }

  _viewLogs() {
    this._sendToRenderer('menu:viewLogs');
  }

  _openLogsFolder() {
    const logsPath = path.join(this.app.getPath('userData'), 'logs');
    shell.openPath(logsPath);
  }

  _runDiagnostics() {
    this._sendToRenderer('menu:runDiagnostics');
  }

  _openGettingStarted() {
    this._sendToRenderer('menu:openGettingStarted');
  }

  _showKeyboardShortcuts() {
    this._sendToRenderer('menu:showKeyboardShortcuts');
  }

  _checkForUpdates() {
    this._sendToRenderer('menu:checkForUpdates');
  }

  _showAbout() {
    dialog.showMessageBox(this.windowManager.getMainWindow(), {
      type: 'info',
      title: `About ${this.app.getName()}`,
      message: this.app.getName(),
      detail: `Version: ${this.app.getVersion()}\n` +
              `Electron: ${process.versions.electron}\n` +
              `Chrome: ${process.versions.chrome}\n` +
              `Node: ${process.versions.node}\n` +
              `Platform: ${process.platform} ${process.arch}\n\n` +
              `AI-Powered Video Generation Studio\n` +
              `Copyright © 2025 Coffee285`,
      buttons: ['OK']
    });
  }

  /**
   * Send menu action to renderer process with validation and logging
   */
  _sendToRenderer(channel, data = {}) {
    const window = this.windowManager.getMainWindow();
    if (!window || window.isDestroyed()) {
      console.warn('[MenuBuilder] Cannot send command: window not available', { channel });
      return;
    }
    
    // Generate correlation ID for tracking
    const correlationId = `cmd_${Date.now()}_${Math.random().toString(36).substring(2, 8)}`;
    
    // Validate command payload
    const validation = validateCommandPayload(channel, data);
    
    if (!validation.success) {
      console.error('[MenuBuilder] Invalid command payload', {
        correlationId,
        channel,
        error: validation.error,
        issues: validation.issues,
        payload: data
      });
      // Send anyway but include validation error context
      window.webContents.send(channel, {
        ...data,
        _validationError: validation.error,
        _correlationId: correlationId
      });
      return;
    }
    
    const commandMetadata = getCommandMetadata(channel);
    
    console.log('[MenuBuilder] Sending command to renderer', {
      correlationId,
      channel,
      command: commandMetadata ? commandMetadata.label : 'Unknown',
      category: commandMetadata ? commandMetadata.category : 'Unknown',
      hasPayload: Object.keys(data).length > 0
    });
    
    // Send validated command with correlation ID
    window.webContents.send(channel, {
      ...validation.data,
      _correlationId: correlationId,
      _timestamp: new Date().toISOString()
    });
  }
}

module.exports = MenuBuilder;
