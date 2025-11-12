import { X, Search, Book, Keyboard, HelpCircle, ExternalLink, ChevronRight } from 'lucide-react';
import React, { useState } from 'react';

interface HelpSection {
  id: string;
  title: string;
  icon: React.ReactNode;
  content: HelpItem[];
}

interface HelpItem {
  id: string;
  title: string;
  description: string;
  link?: string;
}

const helpSections: HelpSection[] = [
  {
    id: 'getting-started',
    title: 'Getting Started',
    icon: <Book className="w-5 h-5" />,
    content: [
      {
        id: 'first-video',
        title: 'Create Your First Video',
        description: 'Learn how to generate your first AI-powered video in minutes',
        link: '/docs/getting-started/QUICK_START.md'
      },
      {
        id: 'onboarding',
        title: 'First Run Setup',
        description: 'Configure Aura for your workflow with the onboarding wizard',
        link: '/docs/getting-started/FIRST_RUN_GUIDE.md'
      },
      {
        id: 'providers',
        title: 'Provider Configuration',
        description: 'Set up AI providers and API keys for script, voice, and images',
        link: '/docs/user-guide/USER_MANUAL.md#provider-configuration'
      }
    ]
  },
  {
    id: 'features',
    title: 'Features',
    icon: <HelpCircle className="w-5 h-5" />,
    content: [
      {
        id: 'script-generation',
        title: 'Script Generation',
        description: 'Use AI to create compelling video scripts from your ideas',
        link: '/docs/user-guide/USER_MANUAL.md#script-generation'
      },
      {
        id: 'text-to-speech',
        title: 'Text-to-Speech',
        description: 'Convert scripts to natural-sounding voiceovers',
        link: '/docs/user-guide/USER_MANUAL.md#voice-generation-text-to-speech'
      },
      {
        id: 'timeline-editor',
        title: 'Timeline Editor',
        description: 'Edit your video with professional timeline controls',
        link: '/docs/features/TIMELINE.md'
      },
      {
        id: 'export',
        title: 'Video Export',
        description: 'Render and export videos in multiple formats',
        link: '/docs/user-guide/USER_MANUAL.md#export'
      }
    ]
  },
  {
    id: 'troubleshooting',
    title: 'Troubleshooting',
    icon: <HelpCircle className="w-5 h-5" />,
    content: [
      {
        id: 'common-issues',
        title: 'Common Issues',
        description: 'Quick fixes for frequently encountered problems',
        link: '/docs/troubleshooting/Troubleshooting.md'
      },
      {
        id: 'faq',
        title: 'Frequently Asked Questions',
        description: 'Answers to the most common questions about Aura',
        link: '/docs/user-guide/FAQ.md'
      },
      {
        id: 'diagnostics',
        title: 'Generate Diagnostic Bundle',
        description: 'Create a support bundle for troubleshooting',
        link: '/settings/diagnostics'
      }
    ]
  },
  {
    id: 'shortcuts',
    title: 'Keyboard Shortcuts',
    icon: <Keyboard className="w-5 h-5" />,
    content: [
      {
        id: 'global-shortcuts',
        title: 'Global Shortcuts',
        description: 'Keyboard shortcuts that work throughout the app',
        link: '#shortcuts-modal'
      },
      {
        id: 'timeline-shortcuts',
        title: 'Timeline Shortcuts',
        description: 'Speed up editing with timeline keyboard shortcuts',
        link: '#shortcuts-modal'
      }
    ]
  }
];

interface HelpPanelProps {
  isOpen: boolean;
  onClose: () => void;
  onOpenShortcuts: () => void;
}

export const HelpPanel: React.FC<HelpPanelProps> = ({ isOpen, onClose, onOpenShortcuts }) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedSection, setSelectedSection] = useState<string | null>(null);

  if (!isOpen) return null;

  const filteredSections = helpSections.map(section => ({
    ...section,
    content: section.content.filter(item =>
      item.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      item.description.toLowerCase().includes(searchQuery.toLowerCase())
    )
  })).filter(section => section.content.length > 0);

  const handleItemClick = (link: string) => {
    if (link === '#shortcuts-modal') {
      onOpenShortcuts();
      return;
    }

    if (link.startsWith('/settings')) {
      window.location.href = link;
      return;
    }

    if (link.startsWith('/docs')) {
      window.open(`https://github.com/aura-video-studio/aura/blob/main${link}`, '_blank');
      return;
    }

    window.open(link, '_blank');
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="relative w-full max-w-4xl h-[80vh] bg-white dark:bg-gray-900 rounded-lg shadow-2xl overflow-hidden flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-200 dark:border-gray-700">
          <div className="flex items-center gap-3">
            <HelpCircle className="w-6 h-6 text-blue-600 dark:text-blue-400" />
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
              Help Center
            </h2>
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
            aria-label="Close help panel"
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
              placeholder="Search help articles..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-white"
            />
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6">
          {searchQuery && filteredSections.length === 0 ? (
            <div className="text-center py-12">
              <HelpCircle className="w-16 h-16 mx-auto text-gray-400 mb-4" />
              <p className="text-gray-600 dark:text-gray-400">
                No help articles found for "{searchQuery}"
              </p>
            </div>
          ) : (
            <div className="space-y-6">
              {filteredSections.map(section => (
                <div key={section.id} className="space-y-3">
                  <div className="flex items-center gap-2 text-gray-900 dark:text-white">
                    {section.icon}
                    <h3 className="text-lg font-semibold">{section.title}</h3>
                  </div>
                  <div className="grid gap-3">
                    {section.content.map(item => (
                      <button
                        key={item.id}
                        onClick={() => handleItemClick(item.link || '#')}
                        className="text-left p-4 rounded-lg border border-gray-200 dark:border-gray-700 hover:border-blue-500 dark:hover:border-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-all group"
                      >
                        <div className="flex items-start justify-between gap-4">
                          <div className="flex-1">
                            <h4 className="font-medium text-gray-900 dark:text-white mb-1 group-hover:text-blue-600 dark:group-hover:text-blue-400">
                              {item.title}
                            </h4>
                            <p className="text-sm text-gray-600 dark:text-gray-400">
                              {item.description}
                            </p>
                          </div>
                          <ChevronRight className="w-5 h-5 text-gray-400 group-hover:text-blue-600 dark:group-hover:text-blue-400 flex-shrink-0 mt-1" />
                        </div>
                      </button>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-6 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
          <div className="flex items-center justify-between">
            <p className="text-sm text-gray-600 dark:text-gray-400">
              Need more help? Check our documentation
            </p>
            <a
              href="https://github.com/aura-video-studio/aura/tree/main/docs"
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors"
            >
              <span>View Documentation</span>
              <ExternalLink className="w-4 h-4" />
            </a>
          </div>
        </div>
      </div>
    </div>
  );
};
