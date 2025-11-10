import { useState, useEffect, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Search,
  Grid3x3,
  List,
  Plus,
  Filter,
  Trash2,
  Copy,
  Download,
  Upload,
  LayoutGrid,
  LayoutList,
  RefreshCw,
} from 'lucide-react';
import { projectManagementApi, Project } from '../../api/projectManagement';
import { ProjectCard } from './ProjectCard';
import { ProjectListItem } from './ProjectListItem';
import { ProjectFilters } from './ProjectFilters';
import { CreateProjectDialog } from './CreateProjectDialog';
import { TemplateSelectionDialog } from './TemplateSelectionDialog';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';

type ViewMode = 'grid' | 'list';

export function ProjectManagement() {
  const queryClient = useQueryClient();
  const [viewMode, setViewMode] = useState<ViewMode>('grid');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedProjects, setSelectedProjects] = useState<Set<string>>(new Set());
  const [showFilters, setShowFilters] = useState(false);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [showTemplateDialog, setShowTemplateDialog] = useState(false);

  // Filter state
  const [filters, setFilters] = useState({
    status: '',
    category: '',
    tags: '',
    sortBy: 'UpdatedAt',
    ascending: false,
    page: 1,
    pageSize: 20,
  });

  // Fetch projects
  const { data, isLoading, refetch } = useQuery({
    queryKey: ['projects', searchQuery, filters],
    queryFn: () =>
      projectManagementApi.getProjects({
        search: searchQuery || undefined,
        ...filters,
        tags: filters.tags || undefined,
      }),
  });

  // Fetch statistics
  const { data: stats } = useQuery({
    queryKey: ['projectStatistics'],
    queryFn: () => projectManagementApi.getStatistics(),
  });

  // Delete project mutation
  const deleteMutation = useMutation({
    mutationFn: (projectId: string) => projectManagementApi.deleteProject(projectId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      queryClient.invalidateQueries({ queryKey: ['projectStatistics'] });
    },
  });

  // Bulk delete mutation
  const bulkDeleteMutation = useMutation({
    mutationFn: (projectIds: string[]) =>
      projectManagementApi.bulkDeleteProjects(projectIds),
    onSuccess: () => {
      setSelectedProjects(new Set());
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      queryClient.invalidateQueries({ queryKey: ['projectStatistics'] });
    },
  });

  // Duplicate project mutation
  const duplicateMutation = useMutation({
    mutationFn: (projectId: string) => projectManagementApi.duplicateProject(projectId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
  });

  const handleSelectProject = (projectId: string, selected: boolean) => {
    const newSelected = new Set(selectedProjects);
    if (selected) {
      newSelected.add(projectId);
    } else {
      newSelected.delete(projectId);
    }
    setSelectedProjects(newSelected);
  };

  const handleSelectAll = () => {
    if (data?.projects) {
      if (selectedProjects.size === data.projects.length) {
        setSelectedProjects(new Set());
      } else {
        setSelectedProjects(new Set(data.projects.map((p) => p.id)));
      }
    }
  };

  const handleBulkDelete = async () => {
    if (selectedProjects.size === 0) return;
    if (!confirm(`Delete ${selectedProjects.size} project(s)?`)) return;

    await bulkDeleteMutation.mutateAsync(Array.from(selectedProjects));
  };

  const handleDeleteProject = async (projectId: string) => {
    if (!confirm('Delete this project?')) return;
    await deleteMutation.mutateAsync(projectId);
  };

  const handleDuplicateProject = async (projectId: string) => {
    await duplicateMutation.mutateAsync(projectId);
  };

  const handleFilterChange = (newFilters: Partial<typeof filters>) => {
    setFilters({ ...filters, ...newFilters, page: 1 });
  };

  const handlePageChange = (page: number) => {
    setFilters({ ...filters, page });
  };

  const projects = data?.projects || [];
  const pagination = data?.pagination;

  return (
    <div className="h-full flex flex-col bg-gray-50 dark:bg-gray-900">
      {/* Header */}
      <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
        <div className="px-6 py-4">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Projects</h1>
              {stats && (
                <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                  {stats.totalProjects} total · {stats.draftProjects} drafts ·{' '}
                  {stats.inProgressProjects} in progress · {stats.completedProjects} completed
                </p>
              )}
            </div>

            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => refetch()}
                disabled={isLoading}
              >
                <RefreshCw className="w-4 h-4 mr-2" />
                Refresh
              </Button>
              <Button variant="outline" size="sm" onClick={() => setShowTemplateDialog(true)}>
                <LayoutGrid className="w-4 h-4 mr-2" />
                From Template
              </Button>
              <Button size="sm" onClick={() => setShowCreateDialog(true)}>
                <Plus className="w-4 h-4 mr-2" />
                New Project
              </Button>
            </div>
          </div>

          {/* Search and filters */}
          <div className="flex items-center gap-3">
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <Input
                type="text"
                placeholder="Search projects..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10"
              />
            </div>

            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowFilters(!showFilters)}
              className={showFilters ? 'bg-blue-50 dark:bg-blue-900/20' : ''}
            >
              <Filter className="w-4 h-4 mr-2" />
              Filters
            </Button>

            <div className="flex items-center gap-1 border border-gray-300 dark:border-gray-600 rounded-lg p-1">
              <button
                onClick={() => setViewMode('grid')}
                className={`p-1.5 rounded ${
                  viewMode === 'grid'
                    ? 'bg-blue-500 text-white'
                    : 'text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'
                }`}
              >
                <LayoutGrid className="w-4 h-4" />
              </button>
              <button
                onClick={() => setViewMode('list')}
                className={`p-1.5 rounded ${
                  viewMode === 'list'
                    ? 'bg-blue-500 text-white'
                    : 'text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'
                }`}
              >
                <LayoutList className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* Bulk actions */}
          {selectedProjects.size > 0 && (
            <div className="mt-3 flex items-center gap-2 p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
              <span className="text-sm font-medium text-blue-900 dark:text-blue-100">
                {selectedProjects.size} selected
              </span>
              <div className="flex-1" />
              <Button
                variant="outline"
                size="sm"
                onClick={handleBulkDelete}
                disabled={bulkDeleteMutation.isPending}
              >
                <Trash2 className="w-4 h-4 mr-2" />
                Delete
              </Button>
              <Button variant="outline" size="sm" onClick={() => setSelectedProjects(new Set())}>
                Clear
              </Button>
            </div>
          )}
        </div>

        {/* Filters panel */}
        {showFilters && (
          <div className="border-t border-gray-200 dark:border-gray-700">
            <ProjectFilters filters={filters} onFilterChange={handleFilterChange} />
          </div>
        )}
      </div>

      {/* Content */}
      <div className="flex-1 overflow-auto p-6">
        {isLoading ? (
          <div className="flex items-center justify-center h-64">
            <div className="text-gray-500">Loading projects...</div>
          </div>
        ) : projects.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-64 text-center">
            <LayoutGrid className="w-16 h-16 text-gray-300 dark:text-gray-600 mb-4" />
            <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
              No projects yet
            </h3>
            <p className="text-gray-500 dark:text-gray-400 mb-4">
              Create your first project or start from a template
            </p>
            <div className="flex gap-2">
              <Button onClick={() => setShowCreateDialog(true)}>
                <Plus className="w-4 h-4 mr-2" />
                New Project
              </Button>
              <Button variant="outline" onClick={() => setShowTemplateDialog(true)}>
                <LayoutGrid className="w-4 h-4 mr-2" />
                Browse Templates
              </Button>
            </div>
          </div>
        ) : viewMode === 'grid' ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {projects.map((project) => (
              <ProjectCard
                key={project.id}
                project={project}
                selected={selectedProjects.has(project.id)}
                onSelect={(selected) => handleSelectProject(project.id, selected)}
                onDelete={() => handleDeleteProject(project.id)}
                onDuplicate={() => handleDuplicateProject(project.id)}
              />
            ))}
          </div>
        ) : (
          <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50 dark:bg-gray-900/50 border-b border-gray-200 dark:border-gray-700">
                  <tr>
                    <th className="px-4 py-3 text-left">
                      <input
                        type="checkbox"
                        checked={
                          projects.length > 0 && selectedProjects.size === projects.length
                        }
                        onChange={handleSelectAll}
                        className="rounded border-gray-300 dark:border-gray-600"
                      />
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Project
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Category
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Updated
                    </th>
                    <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                  {projects.map((project) => (
                    <ProjectListItem
                      key={project.id}
                      project={project}
                      selected={selectedProjects.has(project.id)}
                      onSelect={(selected) => handleSelectProject(project.id, selected)}
                      onDelete={() => handleDeleteProject(project.id)}
                      onDuplicate={() => handleDuplicateProject(project.id)}
                    />
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* Pagination */}
        {pagination && pagination.totalPages > 1 && (
          <div className="mt-6 flex items-center justify-between">
            <div className="text-sm text-gray-500 dark:text-gray-400">
              Showing {(pagination.page - 1) * pagination.pageSize + 1} to{' '}
              {Math.min(pagination.page * pagination.pageSize, pagination.totalCount)} of{' '}
              {pagination.totalCount} projects
            </div>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => handlePageChange(filters.page - 1)}
                disabled={!pagination.hasPreviousPage}
              >
                Previous
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handlePageChange(filters.page + 1)}
                disabled={!pagination.hasNextPage}
              >
                Next
              </Button>
            </div>
          </div>
        )}
      </div>

      {/* Dialogs */}
      {showCreateDialog && (
        <CreateProjectDialog
          onClose={() => setShowCreateDialog(false)}
          onCreated={() => {
            setShowCreateDialog(false);
            queryClient.invalidateQueries({ queryKey: ['projects'] });
          }}
        />
      )}

      {showTemplateDialog && (
        <TemplateSelectionDialog
          onClose={() => setShowTemplateDialog(false)}
          onSelected={() => {
            setShowTemplateDialog(false);
            queryClient.invalidateQueries({ queryKey: ['projects'] });
          }}
        />
      )}
    </div>
  );
}
