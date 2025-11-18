/**
 * Projects Store
 * Zustand store for managing project state with persistence and optimistic updates
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import type { Project } from '../services/api/projectsApi';

export interface ProjectsState {
  // Projects cache
  projects: Map<string, Project>;
  projectsList: string[]; // Ordered list of project IDs

  // UI state
  selectedProjectId: string | null;
  isLoading: boolean;
  isSyncing: boolean;

  // Filters and sorting
  filters: {
    status?: Project['status'];
    search?: string;
    tags?: string[];
  };
  sortBy: 'createdAt' | 'updatedAt' | 'name';
  sortOrder: 'asc' | 'desc';

  // Pagination
  currentPage: number;
  pageSize: number;
  totalProjects: number;

  // Recent projects for quick access
  recentProjects: string[]; // Project IDs
  maxRecentProjects: number;

  // Draft auto-save
  draftProject: Partial<Project> | null;

  // Actions - Project management
  addProject: (project: Project) => void;
  updateProject: (id: string, updates: Partial<Project>) => void;
  removeProject: (id: string) => void;
  setProjects: (projects: Project[]) => void;
  clearProjects: () => void;

  // Actions - Selection and navigation
  selectProject: (id: string | null) => void;
  addToRecent: (id: string) => void;

  // Actions - Filters and sorting
  setFilters: (filters: Partial<ProjectsState['filters']>) => void;
  clearFilters: () => void;
  setSortBy: (sortBy: ProjectsState['sortBy']) => void;
  setSortOrder: (sortOrder: ProjectsState['sortOrder']) => void;

  // Actions - Pagination
  setPage: (page: number) => void;
  setPageSize: (pageSize: number) => void;
  setTotalProjects: (total: number) => void;

  // Actions - Loading states
  setLoading: (isLoading: boolean) => void;
  setSyncing: (isSyncing: boolean) => void;

  // Actions - Draft management
  saveDraft: (draft: Partial<Project>) => void;
  clearDraft: () => void;

  // Getters
  getProject: (id: string) => Project | undefined;
  getFilteredProjects: () => Project[];
  getSortedProjects: () => Project[];
}

export const useProjectsStore = create<ProjectsState>()(
  persist(
    (set, get) => ({
      // Initial state
      projects: new Map(),
      projectsList: [],
      selectedProjectId: null,
      isLoading: false,
      isSyncing: false,

      filters: {},
      sortBy: 'updatedAt',
      sortOrder: 'desc',

      currentPage: 1,
      pageSize: 10,
      totalProjects: 0,

      recentProjects: [],
      maxRecentProjects: 10,

      draftProject: null,

      // Add a project to the store
      addProject: (project) => {
        set((state) => {
          const newProjects = new Map(state.projects);
          newProjects.set(project.id, project);

          return {
            projects: newProjects,
            projectsList: [project.id, ...state.projectsList.filter((id) => id !== project.id)],
          };
        });
      },

      // Update a project
      updateProject: (id, updates) => {
        set((state) => {
          const project = state.projects.get(id);
          if (!project) return state;

          const updatedProject = {
            ...project,
            ...updates,
            updatedAt: new Date().toISOString(),
          };

          const newProjects = new Map(state.projects);
          newProjects.set(id, updatedProject);

          return { projects: newProjects };
        });
      },

      // Remove a project
      removeProject: (id) => {
        set((state) => {
          const newProjects = new Map(state.projects);
          newProjects.delete(id);

          return {
            projects: newProjects,
            projectsList: state.projectsList.filter((projectId) => projectId !== id),
            selectedProjectId: state.selectedProjectId === id ? null : state.selectedProjectId,
            recentProjects: state.recentProjects.filter((projectId) => projectId !== id),
          };
        });
      },

      // Set multiple projects (bulk update)
      setProjects: (projects) => {
        const projectsMap = new Map(projects.map((p) => [p.id, p]));
        const projectsList = projects.map((p) => p.id);

        set({
          projects: projectsMap,
          projectsList,
        });
      },

      // Clear all projects
      clearProjects: () => {
        set({
          projects: new Map(),
          projectsList: [],
          selectedProjectId: null,
          totalProjects: 0,
        });
      },

      // Select a project
      selectProject: (id) => {
        set({ selectedProjectId: id });
        if (id) {
          get().addToRecent(id);
        }
      },

      // Add project to recent list
      addToRecent: (id) => {
        set((state) => {
          const recentProjects = [
            id,
            ...state.recentProjects.filter((projectId) => projectId !== id),
          ].slice(0, state.maxRecentProjects);

          return { recentProjects };
        });
      },

      // Set filters
      setFilters: (filters) => {
        set((state) => ({
          filters: { ...state.filters, ...filters },
          currentPage: 1, // Reset to first page when filters change
        }));
      },

      // Clear all filters
      clearFilters: () => {
        set({ filters: {}, currentPage: 1 });
      },

      // Set sort by field
      setSortBy: (sortBy) => {
        set({ sortBy });
      },

      // Set sort order
      setSortOrder: (sortOrder) => {
        set({ sortOrder });
      },

      // Set current page
      setPage: (page) => {
        set({ currentPage: page });
      },

      // Set page size
      setPageSize: (pageSize) => {
        set({ pageSize, currentPage: 1 }); // Reset to first page
      },

      // Set total projects count
      setTotalProjects: (total) => {
        set({ totalProjects: total });
      },

      // Set loading state
      setLoading: (isLoading) => {
        set({ isLoading });
      },

      // Set syncing state
      setSyncing: (isSyncing) => {
        set({ isSyncing });
      },

      // Save draft project
      saveDraft: (draft) => {
        set({ draftProject: draft });
      },

      // Clear draft
      clearDraft: () => {
        set({ draftProject: null });
      },

      // Get a specific project
      getProject: (id) => {
        return get().projects.get(id);
      },

      // Get filtered projects
      getFilteredProjects: () => {
        const { projects, filters } = get();
        let filtered = Array.from(projects.values());

        if (filters.status) {
          filtered = filtered.filter((p) => p.status === filters.status);
        }

        if (filters.search) {
          const searchLower = filters.search.toLowerCase();
          filtered = filtered.filter(
            (p) =>
              p.name.toLowerCase().includes(searchLower) ||
              p.description?.toLowerCase().includes(searchLower) ||
              p.brief.topic.toLowerCase().includes(searchLower)
          );
        }

        if (filters.tags && filters.tags.length > 0) {
          filtered = filtered.filter(
            (p) => p.tags && filters.tags!.some((tag) => p.tags!.includes(tag))
          );
        }

        return filtered;
      },

      // Get sorted projects
      getSortedProjects: () => {
        const { sortBy, sortOrder } = get();
        const filtered = get().getFilteredProjects();

        return filtered.sort((a, b) => {
          let comparison = 0;

          switch (sortBy) {
            case 'name':
              comparison = a.name.localeCompare(b.name);
              break;
            case 'createdAt':
              comparison = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
              break;
            case 'updatedAt':
              comparison = new Date(a.updatedAt).getTime() - new Date(b.updatedAt).getTime();
              break;
          }

          return sortOrder === 'asc' ? comparison : -comparison;
        });
      },
    }),
    {
      name: 'projects-store',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        recentProjects: state.recentProjects,
        filters: state.filters,
        sortBy: state.sortBy,
        sortOrder: state.sortOrder,
        pageSize: state.pageSize,
        draftProject: state.draftProject,
        // Don't persist projects - they should be fetched from server
      }),
    }
  )
);
