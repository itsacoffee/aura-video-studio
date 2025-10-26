import {
  Add20Regular as Plus,
  Delete20Regular as Trash2,
  Open20Regular as ExternalLink,
  Copy20Regular as Copy,
  Checkmark20Regular as Check,
} from '@fluentui/react-icons';
import { useState } from 'react';

interface Source {
  sourceId: string;
  name: string;
  url: string;
  type: string;
  credibilityScore: number;
  publishedDate?: string;
  author?: string;
}

interface SourceCitationEditorProps {
  sources: Source[];
  onSourcesChange?: (sources: Source[]) => void;
}

export const SourceCitationEditor: React.FC<SourceCitationEditorProps> = ({
  sources: initialSources,
  onSourcesChange,
}) => {
  const [sources, setSources] = useState<Source[]>(initialSources);
  const [citationFormat, setCitationFormat] = useState<'APA' | 'MLA' | 'Chicago' | 'Harvard'>(
    'APA'
  );
  const [citations, setCitations] = useState<string[]>([]);
  const [isCopied, setIsCopied] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);

  const handleAddSource = () => {
    const newSource: Source = {
      sourceId: `source-${Date.now()}`,
      name: '',
      url: '',
      type: 'Other',
      credibilityScore: 0.7,
      publishedDate: new Date().toISOString(),
      author: '',
    };

    const updated = [...sources, newSource];
    setSources(updated);
    onSourcesChange?.(updated);
  };

  const handleRemoveSource = (sourceId: string) => {
    const updated = sources.filter((s) => s.sourceId !== sourceId);
    setSources(updated);
    onSourcesChange?.(updated);
  };

  const handleUpdateSource = (sourceId: string, field: keyof Source, value: string | number) => {
    const updated = sources.map((s) => (s.sourceId === sourceId ? { ...s, [field]: value } : s));
    setSources(updated);
    onSourcesChange?.(updated);
  };

  const handleGenerateCitations = async () => {
    setIsGenerating(true);

    try {
      const response = await fetch('/api/verification/citations', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          sources,
          format: citationFormat,
        }),
      });

      if (!response.ok) {
        throw new Error('Failed to generate citations');
      }

      const data = await response.json();
      setCitations(data.citations);
    } catch (err) {
      console.error('Error generating citations:', err);
    } finally {
      setIsGenerating(false);
    }
  };

  const handleCopyCitations = () => {
    const text = citations.join('\n\n');
    navigator.clipboard.writeText(text);
    setIsCopied(true);
    setTimeout(() => setIsCopied(false), 2000);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">Source Citations</h3>
        <button
          onClick={handleAddSource}
          className="flex items-center gap-2 px-3 py-1.5 bg-blue-600 text-white rounded-md hover:bg-blue-700 text-sm"
        >
          <Plus className="h-4 w-4" />
          Add Source
        </button>
      </div>

      {/* Source List */}
      <div className="space-y-3">
        {sources.map((source) => (
          <div key={source.sourceId} className="bg-gray-50 border rounded-lg p-4">
            <div className="flex items-start justify-between gap-4 mb-3">
              <div className="flex-1 grid grid-cols-2 gap-3">
                <div>
                  <label
                    htmlFor={`source-name-${source.sourceId}`}
                    className="block text-xs font-medium text-gray-700 mb-1"
                  >
                    Source Name *
                  </label>
                  <input
                    id={`source-name-${source.sourceId}`}
                    type="text"
                    value={source.name}
                    onChange={(e) => handleUpdateSource(source.sourceId, 'name', e.target.value)}
                    className="w-full px-3 py-1.5 border rounded-md text-sm"
                    placeholder="e.g., Scientific Journal"
                  />
                </div>

                <div>
                  <label
                    htmlFor={`source-author-${source.sourceId}`}
                    className="block text-xs font-medium text-gray-700 mb-1"
                  >
                    Author
                  </label>
                  <input
                    id={`source-author-${source.sourceId}`}
                    type="text"
                    value={source.author || ''}
                    onChange={(e) => handleUpdateSource(source.sourceId, 'author', e.target.value)}
                    className="w-full px-3 py-1.5 border rounded-md text-sm"
                    placeholder="e.g., Dr. Smith"
                  />
                </div>

                <div className="col-span-2">
                  <label
                    htmlFor={`source-url-${source.sourceId}`}
                    className="block text-xs font-medium text-gray-700 mb-1"
                  >
                    URL *
                  </label>
                  <input
                    id={`source-url-${source.sourceId}`}
                    type="url"
                    value={source.url}
                    onChange={(e) => handleUpdateSource(source.sourceId, 'url', e.target.value)}
                    className="w-full px-3 py-1.5 border rounded-md text-sm"
                    placeholder="https://example.com/article"
                  />
                </div>

                <div>
                  <label
                    htmlFor={`source-type-${source.sourceId}`}
                    className="block text-xs font-medium text-gray-700 mb-1"
                  >
                    Type
                  </label>
                  <select
                    id={`source-type-${source.sourceId}`}
                    value={source.type}
                    onChange={(e) => handleUpdateSource(source.sourceId, 'type', e.target.value)}
                    className="w-full px-3 py-1.5 border rounded-md text-sm"
                  >
                    <option value="AcademicJournal">Academic Journal</option>
                    <option value="NewsOrganization">News Organization</option>
                    <option value="Government">Government</option>
                    <option value="Wikipedia">Wikipedia</option>
                    <option value="Expert">Expert</option>
                    <option value="Organization">Organization</option>
                    <option value="Other">Other</option>
                  </select>
                </div>

                <div>
                  <label
                    htmlFor={`source-date-${source.sourceId}`}
                    className="block text-xs font-medium text-gray-700 mb-1"
                  >
                    Published Date
                  </label>
                  <input
                    id={`source-date-${source.sourceId}`}
                    type="date"
                    value={source.publishedDate ? source.publishedDate.split('T')[0] : ''}
                    onChange={(e) =>
                      handleUpdateSource(
                        source.sourceId,
                        'publishedDate',
                        e.target.value + 'T00:00:00Z'
                      )
                    }
                    className="w-full px-3 py-1.5 border rounded-md text-sm"
                  />
                </div>
              </div>

              <button
                onClick={() => handleRemoveSource(source.sourceId)}
                className="p-2 text-red-600 hover:bg-red-50 rounded-md"
                title="Remove source"
              >
                <Trash2 className="h-4 w-4" />
              </button>
            </div>

            {source.url && (
              <a
                href={source.url}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-1 text-xs text-blue-600 hover:text-blue-800"
              >
                <ExternalLink className="h-3 w-3" />
                View source
              </a>
            )}
          </div>
        ))}

        {sources.length === 0 && (
          <div className="text-center py-8 text-gray-500 text-sm">
            No sources added yet. Click &quot;Add Source&quot; to start.
          </div>
        )}
      </div>

      {/* Citation Generator */}
      {sources.length > 0 && (
        <div className="border-t pt-6">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-4">
              <label htmlFor="citation-format" className="text-sm font-medium text-gray-700">
                Citation Format:
              </label>
              <select
                id="citation-format"
                value={citationFormat}
                onChange={(e) =>
                  setCitationFormat(e.target.value as 'APA' | 'MLA' | 'Chicago' | 'Harvard')
                }
                className="px-3 py-1.5 border rounded-md text-sm"
              >
                <option value="APA">APA</option>
                <option value="MLA">MLA</option>
                <option value="Chicago">Chicago</option>
                <option value="Harvard">Harvard</option>
              </select>
            </div>

            <button
              onClick={handleGenerateCitations}
              disabled={isGenerating}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 text-sm"
            >
              {isGenerating ? 'Generating...' : 'Generate Citations'}
            </button>
          </div>

          {/* Generated Citations */}
          {citations.length > 0 && (
            <div className="bg-gray-50 border rounded-lg p-4">
              <div className="flex items-center justify-between mb-3">
                <h4 className="font-medium text-sm">Generated Citations ({citationFormat})</h4>
                <button
                  onClick={handleCopyCitations}
                  className="flex items-center gap-2 px-3 py-1.5 bg-white border rounded-md hover:bg-gray-50 text-sm"
                >
                  {isCopied ? (
                    <>
                      <Check className="h-4 w-4 text-green-600" />
                      Copied!
                    </>
                  ) : (
                    <>
                      <Copy className="h-4 w-4" />
                      Copy All
                    </>
                  )}
                </button>
              </div>

              <div className="space-y-3">
                {citations.map((citation, idx) => (
                  <div key={idx} className="bg-white border rounded p-3">
                    <p className="text-sm text-gray-800">{citation}</p>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
