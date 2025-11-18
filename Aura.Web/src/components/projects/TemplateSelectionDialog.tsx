import { useQuery, useMutation } from '@tanstack/react-query';
import { X, Sparkles } from 'lucide-react';
import { useState } from 'react';
import { projectManagementApi, Template } from '../../api/projectManagement';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';

interface TemplateSelectionDialogProps {
  onClose: () => void;
  onSelected: () => void;
}

export function TemplateSelectionDialog({ onClose, onSelected }: TemplateSelectionDialogProps) {
  const [selectedTemplate, setSelectedTemplate] = useState<Template | null>(null);
  const [projectName, setProjectName] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['templates'],
    queryFn: () => projectManagementApi.getTemplates(),
  });

  const createFromTemplateMutation = useMutation({
    mutationFn: (templateId: string) =>
      projectManagementApi.createProjectFromTemplate(templateId, projectName || undefined),
    onSuccess: () => {
      onSelected();
    },
  });

  const handleSelectTemplate = (template: Template) => {
    setSelectedTemplate(template);
    setProjectName(template.name);
  };

  const handleCreate = () => {
    if (!selectedTemplate) return;
    createFromTemplateMutation.mutate(selectedTemplate.id);
  };

  const templates = data?.templates || [];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl w-full max-w-4xl mx-4 max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-200 dark:border-gray-700">
          <div>
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
              Choose a Template
            </h2>
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
              Start your project with a pre-built template
            </p>
          </div>
          <button
            onClick={onClose}
            className="p-1 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-auto p-6">
          {isLoading ? (
            <div className="flex items-center justify-center h-64">
              <div className="text-gray-500">Loading templates...</div>
            </div>
          ) : templates.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-64 text-center">
              <Sparkles className="w-16 h-16 text-gray-300 dark:text-gray-600 mb-4" />
              <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                No templates available
              </h3>
              <p className="text-gray-500 dark:text-gray-400">
                System templates will be seeded automatically
              </p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {templates.map((template) => (
                <button
                  key={template.id}
                  onClick={() => handleSelectTemplate(template)}
                  className={`text-left p-4 rounded-lg border-2 transition-all hover:shadow-md ${
                    selectedTemplate?.id === template.id
                      ? 'border-blue-500 dark:border-blue-400 bg-blue-50 dark:bg-blue-900/20'
                      : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'
                  }`}
                >
                  {/* Preview image placeholder */}
                  <div className="aspect-video bg-gradient-to-br from-blue-500 to-purple-600 rounded-lg mb-3 flex items-center justify-center">
                    <Sparkles className="w-8 h-8 text-white/50" />
                  </div>

                  <h3 className="font-semibold text-gray-900 dark:text-white mb-1">
                    {template.name}
                  </h3>
                  <p className="text-sm text-gray-500 dark:text-gray-400 line-clamp-2 mb-2">
                    {template.description}
                  </p>

                  <div className="flex items-center justify-between text-xs">
                    <span className="text-gray-500 dark:text-gray-400">{template.category}</span>
                    {template.isSystemTemplate && (
                      <span className="px-2 py-0.5 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 rounded-full">
                        Official
                      </span>
                    )}
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Footer with project name input */}
        {selectedTemplate && (
          <div className="border-t border-gray-200 dark:border-gray-700 p-6 space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Project Name
              </label>
              <Input
                type="text"
                value={projectName}
                onChange={(e) => setProjectName(e.target.value)}
                placeholder="Enter project name..."
              />
            </div>

            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={onClose}>
                Cancel
              </Button>
              <Button
                onClick={handleCreate}
                disabled={!projectName.trim() || createFromTemplateMutation.isPending}
              >
                {createFromTemplateMutation.isPending ? 'Creating...' : 'Create Project'}
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
