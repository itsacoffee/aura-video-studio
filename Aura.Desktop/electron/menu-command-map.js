/**
 * Menu Command Map - Central Registry for Menu Commands
 * 
 * This module defines all menu commands with:
 * - Command IDs (matching event channels)
 * - Payload schemas (Zod validation)
 * - Allowed contexts (which views/states support this command)
 * - Command metadata (labels, descriptions, keyboard shortcuts)
 * 
 * This ensures type-safe, validated communication from main → preload → renderer
 */

const { z } = require('zod');

/**
 * Context types where commands can be executed
 */
const CommandContext = {
  GLOBAL: 'global',              // Available everywhere
  PROJECT_LOADED: 'project',     // Requires active project
  TIMELINE: 'timeline',          // Available in timeline view
  MEDIA_LIBRARY: 'media',        // Available in media library
  SETTINGS: 'settings',          // Available in settings view
  HELP: 'help'                   // Available in help/documentation views
};

/**
 * Schema for OpenRecentProject command payload
 */
const OpenRecentProjectSchema = z.object({
  path: z.string().min(1, 'Project path is required'),
  name: z.string().optional()
});

/**
 * Default empty payload schema (for commands with no parameters)
 */
const EmptyPayloadSchema = z.object({}).optional();

/**
 * Complete Menu Command Map
 * 
 * Each command has:
 * - id: Unique command identifier (matches IPC channel)
 * - label: Human-readable label
 * - category: Menu category (File, Edit, View, Tools, Help)
 * - schema: Zod schema for payload validation
 * - contexts: Array of contexts where command is available
 * - accelerator: Keyboard shortcut (optional)
 * - description: Detailed description for logging/debugging
 */
const MENU_COMMANDS = {
  // File Menu Commands
  NEW_PROJECT: {
    id: 'menu:newProject',
    label: 'New Project',
    category: 'File',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.GLOBAL],
    accelerator: 'CmdOrCtrl+N',
    description: 'Create a new video project'
  },
  
  OPEN_PROJECT: {
    id: 'menu:openProject',
    label: 'Open Project',
    category: 'File',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.GLOBAL],
    accelerator: 'CmdOrCtrl+O',
    description: 'Open an existing project from file system'
  },
  
  OPEN_RECENT_PROJECT: {
    id: 'menu:openRecentProject',
    label: 'Open Recent Project',
    category: 'File',
    schema: OpenRecentProjectSchema,
    contexts: [CommandContext.GLOBAL],
    description: 'Open a recently used project'
  },
  
  SAVE_PROJECT: {
    id: 'menu:saveProject',
    label: 'Save',
    category: 'File',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.PROJECT_LOADED],
    accelerator: 'CmdOrCtrl+S',
    description: 'Save current project'
  },
  
  SAVE_PROJECT_AS: {
    id: 'menu:saveProjectAs',
    label: 'Save As',
    category: 'File',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.PROJECT_LOADED],
    accelerator: 'CmdOrCtrl+Shift+S',
    description: 'Save current project to a new location'
  },
  
  IMPORT_VIDEO: {
    id: 'menu:importVideo',
    label: 'Import Video',
    category: 'File',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.PROJECT_LOADED, CommandContext.MEDIA_LIBRARY],
    description: 'Import video files into media library'
  },
  
  IMPORT_AUDIO: {
    id: 'menu:importAudio',
    label: 'Import Audio',
    category: 'File',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.PROJECT_LOADED, CommandContext.MEDIA_LIBRARY],
    description: 'Import audio files into media library'
  },
  
  IMPORT_IMAGES: {
    id: 'menu:importImages',
    label: 'Import Images',
    category: 'File',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.PROJECT_LOADED, CommandContext.MEDIA_LIBRARY],
    description: 'Import image files into media library'
  },
  
  IMPORT_DOCUMENT: {
    id: 'menu:importDocument',
    label: 'Import Document',
    category: 'File',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.PROJECT_LOADED],
    description: 'Import document for script generation'
  },
  
  EXPORT_VIDEO: {
    id: 'menu:exportVideo',
    label: 'Export Video',
    category: 'File',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.PROJECT_LOADED, CommandContext.TIMELINE],
    accelerator: 'CmdOrCtrl+E',
    description: 'Export video to file'
  },
  
  EXPORT_TIMELINE: {
    id: 'menu:exportTimeline',
    label: 'Export Timeline',
    category: 'File',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.PROJECT_LOADED, CommandContext.TIMELINE],
    description: 'Export timeline as JSON or other format'
  },
  
  // Edit Menu Commands
  FIND: {
    id: 'menu:find',
    label: 'Find',
    category: 'Edit',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.GLOBAL],
    accelerator: 'CmdOrCtrl+F',
    description: 'Open find/search dialog'
  },
  
  OPEN_PREFERENCES: {
    id: 'menu:openPreferences',
    label: 'Preferences',
    category: 'Edit',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.GLOBAL],
    accelerator: process.platform === 'darwin' ? 'Cmd+,' : 'CmdOrCtrl+,',
    description: 'Open application preferences'
  },
  
  // Tools Menu Commands
  OPEN_PROVIDER_SETTINGS: {
    id: 'menu:openProviderSettings',
    label: 'Provider Settings',
    category: 'Tools',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.GLOBAL],
    description: 'Configure AI provider settings (OpenAI, Anthropic, etc.)'
  },
  
  OPEN_FFMPEG_CONFIG: {
    id: 'menu:openFFmpegConfig',
    label: 'FFmpeg Configuration',
    category: 'Tools',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.GLOBAL],
    description: 'Configure FFmpeg settings and hardware acceleration'
  },
  
  CLEAR_CACHE: {
    id: 'menu:clearCache',
    label: 'Clear Cache',
    category: 'Tools',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.GLOBAL],
    description: 'Clear application cache and temporary files'
  },
  
  VIEW_LOGS: {
    id: 'menu:viewLogs',
    label: 'View Logs',
    category: 'Tools',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.GLOBAL],
    description: 'Open application logs viewer'
  },
  
  RUN_DIAGNOSTICS: {
    id: 'menu:runDiagnostics',
    label: 'Run Diagnostics',
    category: 'Tools',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.GLOBAL],
    description: 'Run system diagnostics to check configuration'
  },
  
  // Help Menu Commands
  OPEN_GETTING_STARTED: {
    id: 'menu:openGettingStarted',
    label: 'Getting Started Guide',
    category: 'Help',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.GLOBAL],
    description: 'Open getting started guide for new users'
  },
  
  SHOW_KEYBOARD_SHORTCUTS: {
    id: 'menu:showKeyboardShortcuts',
    label: 'Keyboard Shortcuts',
    category: 'Help',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.GLOBAL],
    description: 'Show available keyboard shortcuts'
  },
  
  CHECK_FOR_UPDATES: {
    id: 'menu:checkForUpdates',
    label: 'Check for Updates',
    category: 'Help',
    schema: EmptyPayloadSchema,
    contexts: [CommandContext.GLOBAL],
    description: 'Check for application updates'
  }
};

/**
 * Validate command payload against its schema
 * 
 * @param {string} commandId - Command ID to validate
 * @param {object} payload - Payload to validate
 * @returns {{ success: boolean, data?: object, error?: string, issues?: array }}
 */
function validateCommandPayload(commandId, payload = {}) {
  const command = Object.values(MENU_COMMANDS).find(cmd => cmd.id === commandId);
  
  if (!command) {
    return {
      success: false,
      error: `Unknown command: ${commandId}`,
      commandId
    };
  }
  
  try {
    const result = command.schema.safeParse(payload);
    
    if (result.success) {
      return {
        success: true,
        data: result.data,
        commandId,
        command: {
          label: command.label,
          category: command.category,
          description: command.description
        }
      };
    } else {
      return {
        success: false,
        error: 'Payload validation failed',
        issues: result.error.issues,
        commandId,
        command: {
          label: command.label,
          category: command.category
        }
      };
    }
  } catch (error) {
    return {
      success: false,
      error: `Validation error: ${error.message}`,
      commandId
    };
  }
}

/**
 * Get command metadata by ID
 * 
 * @param {string} commandId - Command ID
 * @returns {object|null} Command metadata or null if not found
 */
function getCommandMetadata(commandId) {
  const command = Object.values(MENU_COMMANDS).find(cmd => cmd.id === commandId);
  return command || null;
}

/**
 * Check if command is available in given context
 * 
 * @param {string} commandId - Command ID
 * @param {string} currentContext - Current application context
 * @returns {boolean} True if command is available in context
 */
function isCommandAvailableInContext(commandId, currentContext) {
  const command = Object.values(MENU_COMMANDS).find(cmd => cmd.id === commandId);
  
  if (!command) {
    return false;
  }
  
  // GLOBAL commands are available everywhere
  if (command.contexts.includes(CommandContext.GLOBAL)) {
    return true;
  }
  
  // Check if current context matches any allowed context
  return command.contexts.includes(currentContext);
}

/**
 * Get all command IDs (for validation/registration)
 * 
 * @returns {string[]} Array of all command IDs
 */
function getAllCommandIds() {
  return Object.values(MENU_COMMANDS).map(cmd => cmd.id);
}

/**
 * Get commands by category
 * 
 * @param {string} category - Category name (File, Edit, View, Tools, Help)
 * @returns {object[]} Array of commands in that category
 */
function getCommandsByCategory(category) {
  return Object.values(MENU_COMMANDS).filter(cmd => cmd.category === category);
}

module.exports = {
  MENU_COMMANDS,
  CommandContext,
  validateCommandPayload,
  getCommandMetadata,
  isCommandAvailableInContext,
  getAllCommandIds,
  getCommandsByCategory
};
