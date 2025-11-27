/**
 * Context Menu Builder Service
 *
 * Builds native Electron context menus based on type and data.
 * Each menu type has its own dedicated builder method.
 */

const { Menu } = require('electron');

class ContextMenuBuilder {
  /**
   * Create a new ContextMenuBuilder instance.
   * @param {object} logger - Logger instance for debugging
   */
  constructor(logger) {
    this.logger = logger || console;
  }

  /**
   * Build a context menu for the specified type.
   * @param {string} type - The context menu type
   * @param {object} data - Menu data specific to the type
   * @param {object} callbacks - Callback functions for menu actions
   * @returns {Electron.Menu} The built Electron menu
   */
  build(type, data, callbacks) {
    this.logger.debug('Building context menu', { type, data });

    switch (type) {
      case 'timeline-clip':
        return this.buildTimelineClipMenu(data, callbacks);
      case 'timeline-track':
        return this.buildTimelineTrackMenu(data, callbacks);
      case 'timeline-empty':
        return this.buildTimelineEmptyMenu(data, callbacks);
      case 'media-asset':
        return this.buildMediaAssetMenu(data, callbacks);
      case 'ai-script':
        return this.buildAIScriptMenu(data, callbacks);
      case 'job-queue':
        return this.buildJobQueueMenu(data, callbacks);
      case 'preview-window':
        return this.buildPreviewWindowMenu(data, callbacks);
      case 'ai-provider':
        return this.buildAIProviderMenu(data, callbacks);
      default:
        this.logger.warn('Unknown context menu type', { type });
        return Menu.buildFromTemplate([
          { label: 'Unknown menu type', enabled: false }
        ]);
    }
  }

  /**
   * Build context menu for timeline clips.
   * @param {object} data - Timeline clip data
   * @param {object} callbacks - Callback functions
   * @returns {Electron.Menu}
   */
  buildTimelineClipMenu(data, callbacks) {
    const template = [
      {
        label: 'Cut',
        accelerator: 'CmdOrCtrl+X',
        click: () => callbacks.onCut?.(data)
      },
      {
        label: 'Copy',
        accelerator: 'CmdOrCtrl+C',
        click: () => callbacks.onCopy?.(data)
      },
      {
        label: 'Paste',
        accelerator: 'CmdOrCtrl+V',
        click: () => callbacks.onPaste?.(data),
        enabled: data.hasClipboardData || false
      },
      {
        label: 'Duplicate',
        accelerator: 'CmdOrCtrl+D',
        click: () => callbacks.onDuplicate?.(data)
      },
      { type: 'separator' },
      {
        label: 'Split at Playhead',
        accelerator: 'S',
        click: () => callbacks.onSplit?.(data)
      },
      {
        label: 'Delete',
        accelerator: 'Delete',
        click: () => callbacks.onDelete?.(data)
      },
      {
        label: 'Ripple Delete',
        accelerator: 'Shift+Delete',
        click: () => callbacks.onRippleDelete?.(data)
      },
      { type: 'separator' },
      {
        label: 'Properties',
        accelerator: 'CmdOrCtrl+I',
        click: () => callbacks.onProperties?.(data)
      }
    ];
    return Menu.buildFromTemplate(template);
  }

  /**
   * Build context menu for timeline tracks.
   * @param {object} data - Timeline track data
   * @param {object} callbacks - Callback functions
   * @returns {Electron.Menu}
   */
  buildTimelineTrackMenu(data, callbacks) {
    const template = [
      {
        label: 'Add Track Above',
        click: () => callbacks.onAddTrack?.(data, 'above')
      },
      {
        label: 'Add Track Below',
        click: () => callbacks.onAddTrack?.(data, 'below')
      },
      { type: 'separator' },
      {
        label: 'Lock Track',
        type: 'checkbox',
        checked: data.isLocked || false,
        click: () => callbacks.onToggleLock?.(data)
      },
      {
        label: 'Mute Track',
        type: 'checkbox',
        checked: data.isMuted || false,
        click: () => callbacks.onToggleMute?.(data)
      },
      {
        label: 'Solo Track',
        type: 'checkbox',
        checked: data.isSolo || false,
        click: () => callbacks.onToggleSolo?.(data)
      },
      { type: 'separator' },
      {
        label: 'Rename Track',
        click: () => callbacks.onRename?.(data)
      },
      {
        label: 'Delete Track',
        click: () => callbacks.onDelete?.(data),
        enabled: data.totalTracks > 1
      }
    ];
    return Menu.buildFromTemplate(template);
  }

  /**
   * Build context menu for empty timeline area.
   * @param {object} data - Empty timeline area data
   * @param {object} callbacks - Callback functions
   * @returns {Electron.Menu}
   */
  buildTimelineEmptyMenu(data, callbacks) {
    const template = [
      {
        label: 'Paste',
        accelerator: 'CmdOrCtrl+V',
        click: () => callbacks.onPaste?.(data),
        enabled: data.hasClipboardData || false
      },
      { type: 'separator' },
      {
        label: 'Add Marker',
        accelerator: 'M',
        click: () => callbacks.onAddMarker?.(data)
      },
      {
        label: 'Select All Clips',
        accelerator: 'CmdOrCtrl+A',
        click: () => callbacks.onSelectAll?.(data)
      }
    ];
    return Menu.buildFromTemplate(template);
  }

  /**
   * Build context menu for media assets.
   * @param {object} data - Media asset data
   * @param {object} callbacks - Callback functions
   * @returns {Electron.Menu}
   */
  buildMediaAssetMenu(data, callbacks) {
    const template = [
      {
        label: 'Add to Timeline',
        click: () => callbacks.onAddToTimeline?.(data)
      },
      {
        label: 'Preview',
        click: () => callbacks.onPreview?.(data)
      },
      { type: 'separator' },
      {
        label: 'Rename',
        accelerator: 'F2',
        click: () => callbacks.onRename?.(data)
      },
      {
        label: 'Add to Favorites',
        type: 'checkbox',
        checked: data.isFavorite || false,
        click: () => callbacks.onToggleFavorite?.(data)
      },
      { type: 'separator' },
      {
        label: 'Reveal in File Explorer',
        accelerator: 'CmdOrCtrl+R',
        click: () => callbacks.onRevealInOS?.(data)
      },
      {
        label: 'Properties',
        click: () => callbacks.onProperties?.(data)
      },
      { type: 'separator' },
      {
        label: 'Delete from Library',
        click: () => callbacks.onDelete?.(data)
      }
    ];
    return Menu.buildFromTemplate(template);
  }

  /**
   * Build context menu for AI script scenes.
   * @param {object} data - AI script data
   * @param {object} callbacks - Callback functions
   * @returns {Electron.Menu}
   */
  buildAIScriptMenu(data, callbacks) {
    const template = [
      {
        label: 'Regenerate This Scene',
        click: () => callbacks.onRegenerate?.(data)
      },
      {
        label: 'Expand Section',
        click: () => callbacks.onExpand?.(data)
      },
      {
        label: 'Shorten Section',
        click: () => callbacks.onShorten?.(data)
      },
      { type: 'separator' },
      {
        label: 'Generate B-Roll Suggestions',
        click: () => callbacks.onGenerateBRoll?.(data)
      },
      {
        label: 'Copy Text',
        accelerator: 'CmdOrCtrl+C',
        click: () => callbacks.onCopyText?.(data)
      }
    ];
    return Menu.buildFromTemplate(template);
  }

  /**
   * Build context menu for job queue items.
   * @param {object} data - Job queue data
   * @param {object} callbacks - Callback functions
   * @returns {Electron.Menu}
   */
  buildJobQueueMenu(data, callbacks) {
    const isPausable = data.status === 'running';
    const isResumable = data.status === 'paused';
    const isCancelable = ['running', 'paused', 'queued'].includes(data.status);
    const isRetryable = data.status === 'failed';
    const isCompleted = data.status === 'completed';
    const hasOutput = isCompleted && Boolean(data.outputPath);

    const template = [
      {
        label: 'Pause Job',
        click: () => callbacks.onPause?.(data),
        enabled: isPausable
      },
      {
        label: 'Resume Job',
        click: () => callbacks.onResume?.(data),
        enabled: isResumable
      },
      {
        label: 'Cancel Job',
        click: () => callbacks.onCancel?.(data),
        enabled: isCancelable
      },
      { type: 'separator' },
      {
        label: 'View Logs',
        click: () => callbacks.onViewLogs?.(data)
      },
      {
        label: 'Retry Job',
        click: () => callbacks.onRetry?.(data),
        enabled: isRetryable
      },
      { type: 'separator' },
      {
        label: 'Open Output File',
        click: () => callbacks.onOpenOutput?.(data),
        enabled: hasOutput
      },
      {
        label: 'Reveal Output in Explorer',
        click: () => callbacks.onRevealOutput?.(data),
        enabled: hasOutput
      }
    ];
    return Menu.buildFromTemplate(template);
  }

  /**
   * Build context menu for preview window.
   * @param {object} data - Preview window data
   * @param {object} callbacks - Callback functions
   * @returns {Electron.Menu}
   */
  buildPreviewWindowMenu(data, callbacks) {
    const template = [
      {
        label: data.isPlaying ? 'Pause' : 'Play',
        accelerator: 'Space',
        click: () => callbacks.onTogglePlayback?.(data)
      },
      { type: 'separator' },
      {
        label: 'Add Marker at Current Frame',
        accelerator: 'M',
        click: () => callbacks.onAddMarker?.(data)
      },
      {
        label: 'Export Frame as Image',
        click: () => callbacks.onExportFrame?.(data)
      },
      { type: 'separator' },
      {
        label: 'Zoom',
        submenu: [
          {
            label: 'Fit to Window',
            click: () => callbacks.onSetZoom?.(data, 'fit')
          },
          {
            label: '50%',
            click: () => callbacks.onSetZoom?.(data, 0.5)
          },
          {
            label: '100%',
            click: () => callbacks.onSetZoom?.(data, 1.0)
          },
          {
            label: '200%',
            click: () => callbacks.onSetZoom?.(data, 2.0)
          }
        ]
      }
    ];
    return Menu.buildFromTemplate(template);
  }

  /**
   * Build context menu for AI providers.
   * @param {object} data - AI provider data
   * @param {object} callbacks - Callback functions
   * @returns {Electron.Menu}
   */
  buildAIProviderMenu(data, callbacks) {
    const template = [
      {
        label: 'Test Connection',
        click: () => callbacks.onTestConnection?.(data)
      },
      {
        label: 'View Usage Stats',
        click: () => callbacks.onViewStats?.(data)
      },
      { type: 'separator' },
      {
        label: 'Set as Default',
        type: 'checkbox',
        checked: data.isDefault || false,
        click: () => callbacks.onSetDefault?.(data)
      },
      {
        label: 'Configure',
        click: () => callbacks.onConfigure?.(data)
      }
    ];
    return Menu.buildFromTemplate(template);
  }
}

module.exports = { ContextMenuBuilder };
