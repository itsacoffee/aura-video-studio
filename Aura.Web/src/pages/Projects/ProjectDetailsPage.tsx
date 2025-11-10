import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { projectManagementApi, ProjectDetails, ProjectVersion } from '../../api/projectManagement';
import {
  ArrowLeft,
  Edit,
  Trash2,
  Copy,
  Download,
  Upload,
  Clock,
  Folder,
  Film,
  FileText,
  History,
  Settings,
  Play,
  AlertCircle,
  CheckCircle,
  XCircle,
  Loader2,
  Tag,
  Calendar,
  User,
} from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';
import { Button } from '../../components/ui/Button';

export function ProjectDetailsPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'overview' | 'scenes' | 'assets' | 'versions'>('overview');

  // Fetch project details
  const { data: project, isLoading, error } = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => projectManagementApi.getProject(projectId!),
    enabled: !!projectId,
  });

  // Fetch version history
  const { data: versionsData } = useQuery({
    queryKey: ['projectVersions', projectId],
    queryFn: () => projectManagementApi.getProjectVersions(projectId!),
    enabled: !!projectId && activeTab === 'versions',
  });

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: () => projectManagementApi.deleteProject(projectId!),
    onSuccess: () => {
      navigate('/projects');
    },
  });

  // Duplicate mutation
  const duplicateMutation = useMutation({
    mutationFn: () => projectManagementApi.duplicateProject(projectId!),
    onSuccess: (data) => {
      navigate(`/projects/${data.id}`);
    },
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <Loader2 className="w-8 h-8 animate-spin text-blue-500" />
      </div>
    );
  }

  if (error || !project) {
    return (
      <div className="flex flex-col items-center justify-center h-screen">
        <AlertCircle className="w-16 h-16 text-red-500 mb-4" />
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
          Project Not Found
        </h2>
        <p className="text-gray-500 dark:text-gray-400 mb-4">
          The project you're looking for doesn't exist or has been deleted.
        </p>
        <Button onClick={() => navigate('/projects')}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Projects
        </Button>
      </div>
    );
  }

  const statusIcons = {
    Draft: <FileText className="w-5 h-5" />,
    InProgress: <Loader2 className="w-5 h-5 animate-spin" />,
    Completed: <CheckCircle className="w-5 h-5" />,
    Failed: <XCircle className="w-5 h-5" />,
    Cancelled: <XCircle className="w-5 h-5" />,
  };

  const statusColors = {
    Draft: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
    InProgress: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
    Completed: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300',
    Failed: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300',
    Cancelled: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
  };

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      {/* Header */}
      <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
        <div className="max-w-7xl mx-auto px-6 py-6">
          <div className="flex items-center justify-between mb-4">
            <Link
              to="/projects"
              className="flex items-center text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back to Projects
            </Link>

            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => duplicateMutation.mutate()}
                disabled={duplicateMutation.isPending}
              >
                <Copy className="w-4 h-4 mr-2" />
                Duplicate
              </Button>
              {project.outputFilePath && (
                <Button variant="outline" size="sm">
                  <Download className="w-4 h-4 mr-2" />
                  Export
                </Button>
              )}
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  if (confirm('Are you sure you want to delete this project?')) {
                    deleteMutation.mutate();
                  }
                }}
                disabled={deleteMutation.isPending}
                className="text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
              >
                <Trash2 className="w-4 h-4 mr-2" />
                Delete
              </Button>
              <Button size="sm">
                <Edit className="w-4 h-4 mr-2" />
                Edit Project
              </Button>
            </div>
          </div>

          <div className="flex items-start justify-between">
            <div className="flex-1">
              <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
                {project.title}
              </h1>
              {project.description && (
                <p className="text-gray-600 dark:text-gray-400 mb-4">{project.description}</p>
              )}

              <div className="flex flex-wrap items-center gap-4 text-sm">
                <div
                  className={`inline-flex items-center gap-2 px-3 py-1.5 rounded-full font-medium ${statusColors[project.status]}`}
                >
                  {statusIcons[project.status]}
                  {project.status}
                </div>

                {project.category && (
                  <div className="flex items-center gap-2 text-gray-600 dark:text-gray-400">
                    <Folder className="w-4 h-4" />
                    {project.category}
                  </div>
                )}

                {project.durationSeconds && (
                  <div className="flex items-center gap-2 text-gray-600 dark:text-gray-400">
                    <Clock className="w-4 h-4" />
                    {Math.floor(project.durationSeconds / 60)}:{String(Math.floor(project.durationSeconds % 60)).padStart(2, '0')}
                  </div>
                )}

                {project.sceneCount > 0 && (
                  <div className="flex items-center gap-2 text-gray-600 dark:text-gray-400">
                    <Film className="w-4 h-4" />
                    {project.sceneCount} scenes
                  </div>
                )}

                <div className="flex items-center gap-2 text-gray-600 dark:text-gray-400">
                  <Calendar className="w-4 h-4" />
                  Updated {formatDistanceToNow(new Date(project.updatedAt), { addSuffix: true })}
                </div>

                {project.createdBy && (
                  <div className="flex items-center gap-2 text-gray-600 dark:text-gray-400">
                    <User className="w-4 h-4" />
                    {project.createdBy}
                  </div>
                )}
              </div>

              {/* Tags */}
              {project.tags && project.tags.length > 0 && (
                <div className="flex flex-wrap gap-2 mt-4">
                  {project.tags.map((tag, index) => (
                    <span
                      key={index}
                      className="inline-flex items-center gap-1 px-2 py-1 text-xs bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 rounded-full"
                    >
                      <Tag className="w-3 h-3" />
                      {tag}
                    </span>
                  ))}
                </div>
              )}
            </div>

            {/* Thumbnail */}
            {project.thumbnailPath && (
              <div className="ml-6 w-64 aspect-video bg-gray-100 dark:bg-gray-800 rounded-lg overflow-hidden flex-shrink-0">
                <img
                  src={project.thumbnailPath}
                  alt={project.title}
                  className="w-full h-full object-cover"
                />
              </div>
            )}
          </div>

          {/* Progress bar for in-progress projects */}
          {project.status === 'InProgress' && project.progressPercent > 0 && (
            <div className="mt-4">
              <div className="flex items-center justify-between text-sm text-gray-600 dark:text-gray-400 mb-2">
                <span>Progress</span>
                <span>{project.progressPercent}%</span>
              </div>
              <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                <div
                  className="h-full bg-blue-500 transition-all duration-300"
                  style={{ width: `${project.progressPercent}%` }}
                />
              </div>
            </div>
          )}
        </div>

        {/* Tabs */}
        <div className="max-w-7xl mx-auto px-6">
          <div className="flex gap-6 border-b border-gray-200 dark:border-gray-700">
            {[
              { id: 'overview', label: 'Overview', icon: FileText },
              { id: 'scenes', label: `Scenes (${project.scenes.length})`, icon: Film },
              { id: 'assets', label: `Assets (${project.assets.length})`, icon: Folder },
              { id: 'versions', label: 'Version History', icon: History },
            ].map((tab) => {
              const Icon = tab.icon;
              return (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id as any)}
                  className={`flex items-center gap-2 px-1 py-3 border-b-2 font-medium text-sm transition-colors ${
                    activeTab === tab.id
                      ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                      : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'
                  }`}
                >
                  <Icon className="w-4 h-4" />
                  {tab.label}
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-6 py-8">
        {activeTab === 'overview' && (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Main info */}
            <div className="lg:col-span-2 space-y-6">
              {/* Generation Parameters */}
              <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                  Generation Parameters
                </h2>
                <div className="space-y-3">
                  {project.briefJson && (
                    <div>
                      <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                        Brief
                      </h3>
                      <pre className="text-xs bg-gray-50 dark:bg-gray-900 p-3 rounded overflow-x-auto">
                        {JSON.stringify(JSON.parse(project.briefJson), null, 2)}
                      </pre>
                    </div>
                  )}
                </div>
              </div>

              {/* Checkpoints */}
              {project.checkpoints.length > 0 && (
                <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                  <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                    Generation Checkpoints
                  </h2>
                  <div className="space-y-3">
                    {project.checkpoints.map((checkpoint) => (
                      <div
                        key={checkpoint.id}
                        className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-900 rounded-lg"
                      >
                        <div>
                          <div className="font-medium text-gray-900 dark:text-white">
                            {checkpoint.stageName}
                          </div>
                          <div className="text-sm text-gray-500 dark:text-gray-400">
                            {checkpoint.completedScenes}/{checkpoint.totalScenes} scenes completed
                          </div>
                        </div>
                        <div className="text-xs text-gray-500 dark:text-gray-400">
                          {formatDistanceToNow(new Date(checkpoint.checkpointTime), { addSuffix: true })}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Error message for failed projects */}
              {project.status === 'Failed' && project.errorMessage && (
                <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-6">
                  <div className="flex items-start gap-3">
                    <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400 flex-shrink-0 mt-0.5" />
                    <div>
                      <h3 className="font-medium text-red-900 dark:text-red-100 mb-1">
                        Generation Failed
                      </h3>
                      <p className="text-sm text-red-700 dark:text-red-300">
                        {project.errorMessage}
                      </p>
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Sidebar */}
            <div className="space-y-6">
              {/* Project info */}
              <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                  Project Info
                </h2>
                <dl className="space-y-3 text-sm">
                  <div>
                    <dt className="text-gray-500 dark:text-gray-400">Created</dt>
                    <dd className="text-gray-900 dark:text-white font-medium">
                      {new Date(project.createdAt).toLocaleString()}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-gray-500 dark:text-gray-400">Last Updated</dt>
                    <dd className="text-gray-900 dark:text-white font-medium">
                      {new Date(project.updatedAt).toLocaleString()}
                    </dd>
                  </div>
                  {project.completedAt && (
                    <div>
                      <dt className="text-gray-500 dark:text-gray-400">Completed</dt>
                      <dd className="text-gray-900 dark:text-white font-medium">
                        {new Date(project.completedAt).toLocaleString()}
                      </dd>
                    </div>
                  )}
                  {project.lastAutoSaveAt && (
                    <div>
                      <dt className="text-gray-500 dark:text-gray-400">Last Auto-Save</dt>
                      <dd className="text-gray-900 dark:text-white font-medium">
                        {new Date(project.lastAutoSaveAt).toLocaleString()}
                      </dd>
                    </div>
                  )}
                  {project.templateId && (
                    <div>
                      <dt className="text-gray-500 dark:text-gray-400">Template</dt>
                      <dd className="text-gray-900 dark:text-white font-medium">
                        {project.templateId}
                      </dd>
                    </div>
                  )}
                  {project.jobId && (
                    <div>
                      <dt className="text-gray-500 dark:text-gray-400">Job ID</dt>
                      <dd className="text-gray-900 dark:text-white font-mono text-xs">
                        {project.jobId}
                      </dd>
                    </div>
                  )}
                </dl>
              </div>

              {/* Output file */}
              {project.outputFilePath && (
                <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                  <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                    Output
                  </h2>
                  <Button className="w-full" size="sm">
                    <Play className="w-4 h-4 mr-2" />
                    View Video
                  </Button>
                  <Button variant="outline" className="w-full mt-2" size="sm">
                    <Download className="w-4 h-4 mr-2" />
                    Download
                  </Button>
                </div>
              )}
            </div>
          </div>
        )}

        {activeTab === 'scenes' && (
          <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
            <div className="p-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                Scenes
              </h2>
              {project.scenes.length === 0 ? (
                <p className="text-gray-500 dark:text-gray-400">No scenes yet</p>
              ) : (
                <div className="space-y-4">
                  {project.scenes.map((scene) => (
                    <div
                      key={scene.id}
                      className="p-4 bg-gray-50 dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700"
                    >
                      <div className="flex items-start justify-between mb-2">
                        <div className="flex items-center gap-2">
                          <span className="inline-flex items-center justify-center w-6 h-6 rounded-full bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 text-xs font-medium">
                            {scene.sceneIndex + 1}
                          </span>
                          <span className="font-medium text-gray-900 dark:text-white">
                            Scene {scene.sceneIndex + 1}
                          </span>
                        </div>
                        <div className="flex items-center gap-2 text-xs">
                          <Clock className="w-3 h-3" />
                          {scene.durationSeconds}s
                        </div>
                      </div>
                      <p className="text-sm text-gray-600 dark:text-gray-400 mb-2">
                        {scene.scriptText}
                      </p>
                      <div className="flex items-center gap-4 text-xs text-gray-500 dark:text-gray-400">
                        {scene.audioFilePath && (
                          <span className="flex items-center gap-1">
                            🎵 Audio ready
                          </span>
                        )}
                        {scene.imageFilePath && (
                          <span className="flex items-center gap-1">
                            🖼️ Image ready
                          </span>
                        )}
                        {scene.isCompleted && (
                          <span className="flex items-center gap-1 text-green-600 dark:text-green-400">
                            <CheckCircle className="w-3 h-3" />
                            Completed
                          </span>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}

        {activeTab === 'assets' && (
          <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
            <div className="p-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                Assets
              </h2>
              {project.assets.length === 0 ? (
                <p className="text-gray-500 dark:text-gray-400">No assets yet</p>
              ) : (
                <div className="space-y-2">
                  {project.assets.map((asset) => (
                    <div
                      key={asset.id}
                      className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-900 rounded-lg"
                    >
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-gray-200 dark:bg-gray-700 rounded flex items-center justify-center">
                          <Folder className="w-5 h-5 text-gray-500 dark:text-gray-400" />
                        </div>
                        <div>
                          <div className="font-medium text-gray-900 dark:text-white text-sm">
                            {asset.assetType}
                          </div>
                          <div className="text-xs text-gray-500 dark:text-gray-400">
                            {(asset.fileSizeBytes / 1024).toFixed(1)} KB
                            {asset.mimeType && ` · ${asset.mimeType}`}
                          </div>
                        </div>
                      </div>
                      <div className="text-xs text-gray-500 dark:text-gray-400">
                        {formatDistanceToNow(new Date(asset.createdAt), { addSuffix: true })}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}

        {activeTab === 'versions' && (
          <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
            <div className="p-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                Version History
              </h2>
              {!versionsData || versionsData.versions.length === 0 ? (
                <p className="text-gray-500 dark:text-gray-400">No version history yet</p>
              ) : (
                <div className="space-y-3">
                  {versionsData.versions.map((version: ProjectVersion) => (
                    <div
                      key={version.id}
                      className="p-4 bg-gray-50 dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700"
                    >
                      <div className="flex items-start justify-between mb-2">
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-medium text-gray-900 dark:text-white">
                              Version {version.versionNumber}
                            </span>
                            {version.isMarkedImportant && (
                              <span className="px-2 py-0.5 text-xs bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300 rounded">
                                Important
                              </span>
                            )}
                            <span className="px-2 py-0.5 text-xs bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-300 rounded">
                              {version.versionType}
                            </span>
                          </div>
                          {version.name && (
                            <div className="text-sm text-gray-900 dark:text-white mt-1">
                              {version.name}
                            </div>
                          )}
                          {version.description && (
                            <div className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                              {version.description}
                            </div>
                          )}
                        </div>
                        <div className="text-xs text-gray-500 dark:text-gray-400 text-right">
                          {formatDistanceToNow(new Date(version.createdAt), { addSuffix: true })}
                          {version.createdBy && (
                            <div className="mt-1">by {version.createdBy}</div>
                          )}
                        </div>
                      </div>
                      <div className="flex items-center gap-4 text-xs text-gray-500 dark:text-gray-400">
                        <span>{(version.storageSizeBytes / 1024).toFixed(1)} KB</span>
                        {version.trigger && <span>Trigger: {version.trigger}</span>}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
