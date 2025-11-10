/**
 * Projects Store Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { useProjectsStore } from '../projectsStore';
import type { Project } from '../../services/api/projectsApi';

const createMockProject = (id: string, overrides: Partial<Project> = {}): Project => ({
  id,
  name: `Project ${id}`,
  brief: {
    topic: 'Test',
    audience: 'General',
    goal: 'Inform',
    tone: 'Informative',
    language: 'en-US',
    aspect: 'Widescreen16x9',
  },
  planSpec: {
    targetDuration: '00:03:00',
    pacing: 'Conversational',
    density: 'Balanced',
    style: 'Standard',
  },
  status: 'draft',
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  ...overrides,
});

describe('ProjectsStore', () => {
  beforeEach(() => {
    // Reset store before each test
    useProjectsStore.setState({
      projects: new Map(),
      projectsList: [],
      selectedProjectId: null,
      filters: {},
      recentProjects: [],
    });
  });

  it('should add a project', () => {
    const { addProject } = useProjectsStore.getState();
    const project = createMockProject('1');

    addProject(project);

    const state = useProjectsStore.getState();
    expect(state.projects.size).toBe(1);
    expect(state.projectsList.length).toBe(1);
    expect(state.projects.get('1')).toEqual(project);
  });

  it('should update a project', () => {
    const { addProject, updateProject } = useProjectsStore.getState();
    const project = createMockProject('1');

    addProject(project);
    updateProject('1', { name: 'Updated Name', status: 'in-progress' });

    const state = useProjectsStore.getState();
    const updatedProject = state.projects.get('1');
    
    expect(updatedProject?.name).toBe('Updated Name');
    expect(updatedProject?.status).toBe('in-progress');
  });

  it('should remove a project', () => {
    const { addProject, removeProject } = useProjectsStore.getState();
    const project = createMockProject('1');

    addProject(project);
    removeProject('1');

    const state = useProjectsStore.getState();
    expect(state.projects.size).toBe(0);
    expect(state.projectsList.length).toBe(0);
  });

  it('should select a project and add to recent', () => {
    const { addProject, selectProject } = useProjectsStore.getState();
    const project = createMockProject('1');

    addProject(project);
    selectProject('1');

    const state = useProjectsStore.getState();
    expect(state.selectedProjectId).toBe('1');
    expect(state.recentProjects).toContain('1');
  });

  it('should filter projects by status', () => {
    const { addProject, setFilters, getFilteredProjects } = useProjectsStore.getState();

    addProject(createMockProject('1', { status: 'draft' }));
    addProject(createMockProject('2', { status: 'in-progress' }));
    addProject(createMockProject('3', { status: 'completed' }));

    setFilters({ status: 'draft' });

    const filtered = getFilteredProjects();
    expect(filtered.length).toBe(1);
    expect(filtered[0].id).toBe('1');
  });

  it('should filter projects by search', () => {
    const { addProject, setFilters, getFilteredProjects } = useProjectsStore.getState();

    addProject(createMockProject('1', { name: 'React Tutorial' }));
    addProject(createMockProject('2', { name: 'Vue Guide' }));
    addProject(createMockProject('3', { name: 'React Advanced' }));

    setFilters({ search: 'React' });

    const filtered = getFilteredProjects();
    expect(filtered.length).toBe(2);
  });

  it('should sort projects', () => {
    const { addProject, setSortBy, setSortOrder, getSortedProjects } = useProjectsStore.getState();

    addProject(createMockProject('1', { name: 'Charlie', createdAt: '2024-01-01' }));
    addProject(createMockProject('2', { name: 'Alice', createdAt: '2024-01-03' }));
    addProject(createMockProject('3', { name: 'Bob', createdAt: '2024-01-02' }));

    setSortBy('name');
    setSortOrder('asc');

    const sorted = getSortedProjects();
    expect(sorted[0].name).toBe('Alice');
    expect(sorted[1].name).toBe('Bob');
    expect(sorted[2].name).toBe('Charlie');
  });

  it('should maintain recent projects list with max limit', () => {
    const { addProject, selectProject, maxRecentProjects } = useProjectsStore.getState();

    // Add more projects than max recent limit
    for (let i = 0; i < maxRecentProjects + 5; i++) {
      const project = createMockProject(`${i}`);
      addProject(project);
      selectProject(`${i}`);
    }

    const state = useProjectsStore.getState();
    expect(state.recentProjects.length).toBe(maxRecentProjects);
  });

  it('should save and clear draft', () => {
    const { saveDraft, clearDraft } = useProjectsStore.getState();

    const draft = { name: 'Draft Project', description: 'In progress' };
    saveDraft(draft);

    let state = useProjectsStore.getState();
    expect(state.draftProject).toEqual(draft);

    clearDraft();

    state = useProjectsStore.getState();
    expect(state.draftProject).toBeNull();
  });
});
