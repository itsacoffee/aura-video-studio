import { useQuery } from '@tanstack/react-query';
import { formatDistanceToNow } from 'date-fns';
import { ArrowLeft, Clock, Folder, Tag, Download, Play, Edit } from 'lucide-react';
import { useParams, Link } from 'react-router-dom';
import { projectManagementApi } from '../api/projectManagement';
import { Button } from '../components/ui/Button';

export function ProjectDetailsPage() {
  const { projectId } = useParams<{ projectId: string }>();

  const { data: project, isLoading } = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => projectManagementApi.getProject(projectId!),
    enabled: !!projectId,
  });

  const { data: versionsData } = useQuery({
    queryKey: ['projectVersions', projectId],
    queryFn: () => projectManagementApi.getProjectVersions(projectId!),
    enabled: !!projectId,
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-gray-500">Loading project...</div>
      </div>
    );
  }

  if (!project) {
    return (
      <div className="flex flex-col items-center justify-center h-screen">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
          Project Not Found
        </h1>
        <Link to="/projects">
          <Button>
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back to Projects
          </Button>
        </Link>
      </div>
    );
  }

  const statusColors = {
    Draft: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
    InProgress: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
    Completed: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300',
    Failed: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300',
    Cancelled: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
  };

  const versions = versionsData?.versions || [];

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      {/* Header */}
      <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between mb-4">
            <Link
              to="/projects"
              className="flex items-center text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back to Projects
            </Link>

            <div className="flex items-center gap-2">
              {project.outputFilePath && (
                <Button variant="outline" size="sm">
                  <Download className="w-4 h-4 mr-2" />
                  Download
                </Button>
              )}
              {project.status === 'Completed' && (
                <Button size="sm">
                  <Play className="w-4 h-4 mr-2" />
                  Preview
                </Button>
              )}
              <Button size="sm">
                <Edit className="w-4 h-4 mr-2" />
                Edit
              </Button>
            </div>
          </div>

          <div className="flex items-start gap-6">
            {/* Thumbnail */}
            <div className="w-48 h-27 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex-shrink-0" />

            {/* Info */}
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-3 mb-2">
                <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
                  {project.title}
                </h1>
                <span
                  className={`px-3 py-1 text-sm font-medium rounded-full ${
                    statusColors[project.status]
                  }`}
                >
                  {project.status}
                </span>
              </div>

              {project.description && (
                <p className="text-gray-600 dark:text-gray-400 mb-4">{project.description}</p>
              )}

              {/* Metadata */}
              <div className="flex flex-wrap gap-4 text-sm text-gray-500 dark:text-gray-400">
                {project.category && (
                  <div className="flex items-center gap-1">
                    <Folder className="w-4 h-4" />
                    {project.category}
                  </div>
                )}
                {project.durationSeconds && (
                  <div className="flex items-center gap-1">
                    <Clock className="w-4 h-4" />
                    {Math.floor(project.durationSeconds / 60)}:
                    {String(Math.floor(project.durationSeconds % 60)).padStart(2, '0')}
                  </div>
                )}
                <span>
                  Created {formatDistanceToNow(new Date(project.createdAt), { addSuffix: true })}
                </span>
                <span>
                  Updated {formatDistanceToNow(new Date(project.updatedAt), { addSuffix: true })}
                </span>
              </div>

              {/* Tags */}
              {project.tags && project.tags.length > 0 && (
                <div className="flex flex-wrap gap-2 mt-3">
                  {project.tags.map((tag, index) => (
                    <span
                      key={index}
                      className="flex items-center gap-1 px-2 py-1 text-xs bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 rounded"
                    >
                      <Tag className="w-3 h-3" />
                      {tag}
                    </span>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-6 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main content */}
          <div className="lg:col-span-2 space-y-6">
            {/* Scenes */}
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                Scenes ({project.scenes.length})
              </h2>
              <div className="space-y-3">
                {project.scenes.length === 0 ? (
                  <p className="text-gray-500 dark:text-gray-400 text-center py-8">
                    No scenes yet
                  </p>
                ) : (
                  project.scenes.map((scene) => (
                    <div
                      key={scene.id}
                      className="p-4 bg-gray-50 dark:bg-gray-900/50 rounded-lg"
                    >
                      <div className="flex items-start justify-between mb-2">
                        <span className="text-sm font-medium text-gray-900 dark:text-white">
                          Scene {scene.sceneIndex + 1}
                        </span>
                        <span className="text-xs text-gray-500 dark:text-gray-400">
                          {scene.durationSeconds.toFixed(1)}s
                        </span>
                      </div>
                      <p className="text-sm text-gray-600 dark:text-gray-400">
                        {scene.scriptText}
                      </p>
                    </div>
                  ))
                )}
              </div>
            </div>

            {/* Version History */}
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                Version History ({versions.length})
              </h2>
              <div className="space-y-3">
                {versions.length === 0 ? (
                  <p className="text-gray-500 dark:text-gray-400 text-center py-8">
                    No versions saved yet
                  </p>
                ) : (
                  versions.map((version) => (
                    <div
                      key={version.id}
                      className="flex items-start gap-3 p-3 bg-gray-50 dark:bg-gray-900/50 rounded-lg"
                    >
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="font-medium text-gray-900 dark:text-white">
                            Version {version.versionNumber}
                          </span>
                          {version.name && (
                            <span className="text-sm text-gray-500 dark:text-gray-400">
                              - {version.name}
                            </span>
                          )}
                        </div>
                        {version.description && (
                          <p className="text-sm text-gray-600 dark:text-gray-400 mb-2">
                            {version.description}
                          </p>
                        )}
                        <div className="text-xs text-gray-500 dark:text-gray-400">
                          {formatDistanceToNow(new Date(version.createdAt), {
                            addSuffix: true,
                          })}
                          {version.createdBy && ` by ${version.createdBy}`}
                        </div>
                      </div>
                      <Button variant="outline" size="sm">
                        Restore
                      </Button>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Assets */}
            <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                Assets ({project.assets.length})
              </h2>
              <div className="space-y-2">
                {project.assets.length === 0 ? (
                  <p className="text-gray-500 dark:text-gray-400 text-center py-4 text-sm">
                    No assets
                  </p>
                ) : (
                  project.assets.map((asset) => (
                    <div
                      key={asset.id}
                      className="flex items-center justify-between p-2 bg-gray-50 dark:bg-gray-900/50 rounded text-sm"
                    >
                      <span className="text-gray-900 dark:text-white truncate">
                        {asset.assetType}
                      </span>
                      <span className="text-xs text-gray-500 dark:text-gray-400">
                        {(asset.fileSizeBytes / 1024 / 1024).toFixed(2)} MB
                      </span>
                    </div>
                  ))
                )}
              </div>
            </div>

            {/* Progress */}
            {project.status === 'InProgress' && (
              <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                  Progress
                </h2>
                <div className="space-y-3">
                  <div>
                    <div className="flex items-center justify-between text-sm mb-2">
                      <span className="text-gray-600 dark:text-gray-400">Overall</span>
                      <span className="font-medium text-gray-900 dark:text-white">
                        {project.progressPercent}%
                      </span>
                    </div>
                    <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                      <div
                        className="h-full bg-blue-500 transition-all duration-300"
                        style={{ width: `${project.progressPercent}%` }}
                      />
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
