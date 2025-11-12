import { X, Keyboard, Search } from 'lucide-react';
import React, { useState } from 'react';

interface ShortcutGroup {
  category: string;
  shortcuts: Shortcut[];
}

interface Shortcut {
  keys: string[];
  description: string;
  context?: string;
}

const shortcutGroups: ShortcutGroup[] = [
  {
    category: 'Global',
    shortcuts: [
      { keys: ['Ctrl', 'K'], description: 'Open Command Palette' },
      { keys: ['Ctrl', 'S'], description: 'Save Current Project' },
      { keys: ['Ctrl', 'Z'], description: 'Undo' },
      { keys: ['Ctrl', 'Shift', 'Z'], description: 'Redo' },
      { keys: ['Ctrl', 'Y'], description: 'Redo (Alternative)' },
      { keys: ['F1'], description: 'Open Help' },
      { keys: ['Ctrl', ','], description: 'Open Settings' },
      { keys: ['Ctrl', 'N'], description: 'New Project' },
      { keys: ['Ctrl', 'O'], description: 'Open Project' },
      { keys: ['Esc'], description: 'Close Modal / Cancel' }
    ]
  },
  {
    category: 'Timeline Editor',
    shortcuts: [
      { keys: ['Space'], description: 'Play / Pause', context: 'Also K key' },
      { keys: ['K'], description: 'Toggle Play / Pause' },
      { keys: ['J'], description: 'Play Backward / Decrease Speed' },
      { keys: ['L'], description: 'Play Forward / Increase Speed' },
      { keys: ['Home'], description: 'Jump to Start' },
      { keys: ['End'], description: 'Jump to End' },
      { keys: ['I'], description: 'Set In Point' },
      { keys: ['O'], description: 'Set Out Point' },
      { keys: ['←'], description: 'Previous Frame' },
      { keys: ['→'], description: 'Next Frame' },
      { keys: ['↑'], description: 'Previous Scene' },
      { keys: ['↓'], description: 'Next Scene' },
      { keys: ['Shift', 'Delete'], description: 'Ripple Delete (Remove and Close Gap)' },
      { keys: ['Delete'], description: 'Delete Selected' },
      { keys: ['Ctrl', 'D'], description: 'Duplicate Selected' },
      { keys: ['C'], description: 'Razor Tool (Cut)' },
      { keys: ['V'], description: 'Selection Tool' },
      { keys: ['M'], description: 'Add Marker' },
      { keys: ['Ctrl', '+'], description: 'Zoom In Timeline' },
      { keys: ['Ctrl', '-'], description: 'Zoom Out Timeline' }
    ]
  },
  {
    category: 'Script Editor',
    shortcuts: [
      { keys: ['Ctrl', 'B'], description: 'Bold Text' },
      { keys: ['Ctrl', 'I'], description: 'Italic Text' },
      { keys: ['Ctrl', 'U'], description: 'Underline Text' },
      { keys: ['Ctrl', 'F'], description: 'Find' },
      { keys: ['Ctrl', 'H'], description: 'Find and Replace' },
      { keys: ['Tab'], description: 'Indent' },
      { keys: ['Shift', 'Tab'], description: 'Outdent' }
    ]
  },
  {
    category: 'View Controls',
    shortcuts: [
      { keys: ['Ctrl', '1'], description: 'Toggle Sidebar' },
      { keys: ['Ctrl', '2'], description: 'Toggle Properties Panel' },
      { keys: ['Ctrl', '3'], description: 'Toggle Timeline' },
      { keys: ['F'], description: 'Fit to Window' },
      { keys: ['Ctrl', '0'], description: 'Reset Zoom' }
    ]
  },
  {
    category: 'Navigation',
    shortcuts: [
      { keys: ['Ctrl', 'Shift', 'W'], description: 'Go to Welcome Page' },
      { keys: ['Ctrl', 'Shift', 'C'], description: 'Go to Create Page' },
      { keys: ['Ctrl', 'Shift', 'E'], description: 'Go to Editor' },
      { keys: ['Ctrl', 'Shift', 'X'], description: 'Go to Export' },
      { keys: ['Ctrl', 'Shift', 'S'], description: 'Go to Settings' }
    ]
  }
];

interface KeyboardShortcutsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const KeyboardShortcutsModal: React.FC<KeyboardShortcutsModalProps> = ({ isOpen, onClose }) => {
  const [searchQuery, setSearchQuery] = useState('');

  if (!isOpen) return null;

  const filteredGroups = shortcutGroups.map(group => ({
    ...group,
    shortcuts: group.shortcuts.filter(shortcut =>
      shortcut.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
      shortcut.keys.some(key => key.toLowerCase().includes(searchQuery.toLowerCase()))
    )
  })).filter(group => group.shortcuts.length > 0);

  const renderKey = (key: string) => (
    <kbd className="px-2 py-1 text-xs font-semibold text-gray-800 dark:text-gray-200 bg-gray-100 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded shadow-sm">
      {key}
    </kbd>
  );

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="relative w-full max-w-5xl h-[85vh] bg-white dark:bg-gray-900 rounded-lg shadow-2xl overflow-hidden flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-200 dark:border-gray-700">
          <div className="flex items-center gap-3">
            <Keyboard className="w-6 h-6 text-blue-600 dark:text-blue-400" />
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
              Keyboard Shortcuts
            </h2>
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
            aria-label="Close shortcuts modal"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Search Bar */}
        <div className="p-6 border-b border-gray-200 dark:border-gray-700">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
            <input
              type="text"
              placeholder="Search shortcuts..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-white"
            />
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6">
          {filteredGroups.length === 0 ? (
            <div className="text-center py-12">
              <Keyboard className="w-16 h-16 mx-auto text-gray-400 mb-4" />
              <p className="text-gray-600 dark:text-gray-400">
                No shortcuts found for "{searchQuery}"
              </p>
            </div>
          ) : (
            <div className="space-y-8">
              {filteredGroups.map(group => (
                <div key={group.category} className="space-y-3">
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-white border-b border-gray-200 dark:border-gray-700 pb-2">
                    {group.category}
                  </h3>
                  <div className="space-y-2">
                    {group.shortcuts.map((shortcut, index) => (
                      <div
                        key={index}
                        className="flex items-center justify-between py-3 px-4 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
                      >
                        <div className="flex-1">
                          <p className="text-gray-900 dark:text-white font-medium">
                            {shortcut.description}
                          </p>
                          {shortcut.context && (
                            <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                              {shortcut.context}
                            </p>
                          )}
                        </div>
                        <div className="flex items-center gap-1">
                          {shortcut.keys.map((key, keyIndex) => (
                            <React.Fragment key={keyIndex}>
                              {keyIndex > 0 && (
                                <span className="text-gray-400 mx-1">+</span>
                              )}
                              {renderKey(key)}
                            </React.Fragment>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-6 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
          <p className="text-sm text-gray-600 dark:text-gray-400 text-center">
            <span className="font-medium">Tip:</span> Press <kbd className="px-2 py-1 text-xs font-semibold text-gray-800 dark:text-gray-200 bg-gray-100 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded">F1</kbd> anytime to open help, or <kbd className="px-2 py-1 text-xs font-semibold text-gray-800 dark:text-gray-200 bg-gray-100 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded">Ctrl+K</kbd> for the command palette
          </p>
        </div>
      </div>
    </div>
  );
};
