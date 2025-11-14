/**
 * Enhanced Menu Command Handler for Preload
 * 
 * Provides validated, correlation-tracked menu command handling for the preload script.
 * This module wraps the raw IPC communication with:
 * - Payload validation using MenuCommandMap schemas
 * - Correlation ID generation for request tracking
 * - Structured logging for debugging
 * - Error reporting with context
 */

const { validateCommandPayload, getCommandMetadata } = require('./menu-command-map');

/**
 * Generate a unique correlation ID for tracking
 * 
 * @returns {string} Correlation ID in format: cmd_timestamp_random
 */
function generateCorrelationId() {
  const timestamp = Date.now();
  const random = Math.random().toString(36).substring(2, 8);
  return `cmd_${timestamp}_${random}`;
}

/**
 * Create enhanced menu command listener with validation
 * 
 * @param {object} ipcRenderer - Electron ipcRenderer instance
 * @param {string} channel - Menu event channel
 * @param {function} userCallback - User's callback function
 * @returns {function} Unsubscribe function
 */
function createValidatedMenuListener(ipcRenderer, channel, userCallback) {
  if (!channel.startsWith('menu:')) {
    throw new Error(`Invalid menu channel: ${channel}. Must start with 'menu:'`);
  }
  
  if (typeof userCallback !== 'function') {
    throw new TypeError(`Callback must be a function, received ${typeof userCallback}`);
  }
  
  // Create wrapper that adds validation and correlation tracking
  const validatedHandler = (event, payload = {}) => {
    const correlationId = generateCorrelationId();
    const startTime = Date.now();
    
    const logContext = {
      correlationId,
      channel,
      timestamp: new Date().toISOString(),
      payload: payload && Object.keys(payload).length > 0 ? payload : undefined
    };
    
    console.log('[Preload:MenuCommand] Received command', logContext);
    
    try {
      // Validate payload against schema
      const validation = validateCommandPayload(channel, payload);
      
      if (!validation.success) {
        console.error('[Preload:MenuCommand] Validation failed', {
          ...logContext,
          error: validation.error,
          issues: validation.issues
        });
        
        // Still call the user callback but with validation error context
        userCallback({
          ...payload,
          _validationError: validation.error,
          _validationIssues: validation.issues,
          _correlationId: correlationId
        });
        return;
      }
      
      const commandMetadata = getCommandMetadata(channel);
      
      // Create enhanced payload with metadata
      const enhancedPayload = {
        ...validation.data,
        _correlationId: correlationId,
        _command: commandMetadata ? {
          label: commandMetadata.label,
          category: commandMetadata.category,
          description: commandMetadata.description
        } : undefined,
        _timestamp: new Date().toISOString()
      };
      
      console.log('[Preload:MenuCommand] Validation passed, dispatching to renderer', {
        correlationId,
        channel,
        command: commandMetadata?.label
      });
      
      // Call user callback with enhanced payload
      const result = userCallback(enhancedPayload);
      
      // If callback returns a promise, track completion
      if (result && typeof result.then === 'function') {
        result.then(() => {
          const duration = Date.now() - startTime;
          console.log('[Preload:MenuCommand] Command completed successfully', {
            correlationId,
            channel,
            duration: `${duration}ms`
          });
        }).catch((error) => {
          const duration = Date.now() - startTime;
          console.error('[Preload:MenuCommand] Command failed', {
            correlationId,
            channel,
            duration: `${duration}ms`,
            error: {
              message: error.message,
              stack: error.stack
            }
          });
        });
      } else {
        const duration = Date.now() - startTime;
        console.log('[Preload:MenuCommand] Command completed (sync)', {
          correlationId,
          channel,
          duration: `${duration}ms`
        });
      }
    } catch (error) {
      const duration = Date.now() - startTime;
      console.error('[Preload:MenuCommand] Unexpected error in command handler', {
        correlationId,
        channel,
        duration: `${duration}ms`,
        error: {
          message: error.message,
          stack: error.stack,
          name: error.name
        }
      });
      
      // Still try to call user callback with error context
      try {
        userCallback({
          ...payload,
          _error: error.message,
          _correlationId: correlationId
        });
      } catch (callbackError) {
        console.error('[Preload:MenuCommand] User callback also threw error', {
          correlationId,
          error: callbackError.message
        });
      }
    }
  };
  
  // Register the validated handler
  ipcRenderer.on(channel, validatedHandler);
  
  // Return unsubscribe function
  return () => {
    console.log('[Preload:MenuCommand] Unsubscribing from channel', { channel });
    ipcRenderer.removeListener(channel, validatedHandler);
  };
}

/**
 * Create a menu API object with validated command handlers
 * 
 * @param {object} ipcRenderer - Electron ipcRenderer instance
 * @returns {object} Menu API with validated command handlers
 */
function createValidatedMenuAPI(ipcRenderer) {
  /**
   * Create a validated listener for a specific menu command
   * 
   * @param {string} channel - Menu event channel
   * @returns {function} Function that accepts callback and returns unsubscribe function
   */
  function createCommandListener(channel) {
    return (callback) => createValidatedMenuListener(ipcRenderer, channel, callback);
  }
  
  return {
    onNewProject: createCommandListener('menu:newProject'),
    onOpenProject: createCommandListener('menu:openProject'),
    onOpenRecentProject: createCommandListener('menu:openRecentProject'),
    onSaveProject: createCommandListener('menu:saveProject'),
    onSaveProjectAs: createCommandListener('menu:saveProjectAs'),
    onImportVideo: createCommandListener('menu:importVideo'),
    onImportAudio: createCommandListener('menu:importAudio'),
    onImportImages: createCommandListener('menu:importImages'),
    onImportDocument: createCommandListener('menu:importDocument'),
    onExportVideo: createCommandListener('menu:exportVideo'),
    onExportTimeline: createCommandListener('menu:exportTimeline'),
    onFind: createCommandListener('menu:find'),
    onOpenPreferences: createCommandListener('menu:openPreferences'),
    onOpenProviderSettings: createCommandListener('menu:openProviderSettings'),
    onOpenFFmpegConfig: createCommandListener('menu:openFFmpegConfig'),
    onClearCache: createCommandListener('menu:clearCache'),
    onViewLogs: createCommandListener('menu:viewLogs'),
    onRunDiagnostics: createCommandListener('menu:runDiagnostics'),
    onOpenGettingStarted: createCommandListener('menu:openGettingStarted'),
    onShowKeyboardShortcuts: createCommandListener('menu:showKeyboardShortcuts'),
    onCheckForUpdates: createCommandListener('menu:checkForUpdates')
  };
}

module.exports = {
  createValidatedMenuAPI,
  createValidatedMenuListener,
  generateCorrelationId
};
