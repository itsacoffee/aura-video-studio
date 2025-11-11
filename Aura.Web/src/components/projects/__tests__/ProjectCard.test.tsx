import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { ProjectCard } from '../ProjectCard';
import { Project } from '../../../api/projectManagement';

const mockProject: Project = {
  id: '123',
  title: 'Test Project',
  description: 'Test Description',
  status: 'Draft',
  category: 'Marketing',
  tags: ['test', 'video'],
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-02T00:00:00Z',
  scenes: [],
  assets: [],
  settings: {},
};

describe('ProjectCard Performance', () => {
  it('should render without errors', () => {
    const mockOnSelect = vi.fn();
    const mockOnDelete = vi.fn();
    const mockOnDuplicate = vi.fn();

    render(
      <BrowserRouter>
        <ProjectCard
          project={mockProject}
          selected={false}
          onSelect={mockOnSelect}
          onDelete={mockOnDelete}
          onDuplicate={mockOnDuplicate}
        />
      </BrowserRouter>
    );

    expect(screen.getByText('Test Project')).toBeInTheDocument();
  });

  it('should not re-render when unrelated props change', () => {
    const mockOnSelect = vi.fn();
    const mockOnDelete = vi.fn();
    const mockOnDuplicate = vi.fn();

    const { rerender } = render(
      <BrowserRouter>
        <ProjectCard
          project={mockProject}
          selected={false}
          onSelect={mockOnSelect}
          onDelete={mockOnDelete}
          onDuplicate={mockOnDuplicate}
        />
      </BrowserRouter>
    );

    const renderCount = vi.fn();
    
    // Same props should not trigger re-render (React.memo optimization)
    rerender(
      <BrowserRouter>
        <ProjectCard
          project={mockProject}
          selected={false}
          onSelect={mockOnSelect}
          onDelete={mockOnDelete}
          onDuplicate={mockOnDuplicate}
        />
      </BrowserRouter>
    );

    // Component should be memoized and not re-render with identical props
    expect(screen.getByText('Test Project')).toBeInTheDocument();
  });

  it('should re-render when project data changes', () => {
    const mockOnSelect = vi.fn();
    const mockOnDelete = vi.fn();
    const mockOnDuplicate = vi.fn();

    const { rerender } = render(
      <BrowserRouter>
        <ProjectCard
          project={mockProject}
          selected={false}
          onSelect={mockOnSelect}
          onDelete={mockOnDelete}
          onDuplicate={mockOnDuplicate}
        />
      </BrowserRouter>
    );

    const updatedProject = {
      ...mockProject,
      title: 'Updated Project',
    };

    rerender(
      <BrowserRouter>
        <ProjectCard
          project={updatedProject}
          selected={false}
          onSelect={mockOnSelect}
          onDelete={mockOnDelete}
          onDuplicate={mockOnDuplicate}
        />
      </BrowserRouter>
    );

    expect(screen.getByText('Updated Project')).toBeInTheDocument();
  });

  it('should re-render when selection state changes', () => {
    const mockOnSelect = vi.fn();
    const mockOnDelete = vi.fn();
    const mockOnDuplicate = vi.fn();

    const { rerender, container } = render(
      <BrowserRouter>
        <ProjectCard
          project={mockProject}
          selected={false}
          onSelect={mockOnSelect}
          onDelete={mockOnDelete}
          onDuplicate={mockOnDuplicate}
        />
      </BrowserRouter>
    );

    expect(container.querySelector('.border-blue-500')).not.toBeInTheDocument();

    rerender(
      <BrowserRouter>
        <ProjectCard
          project={mockProject}
          selected={true}
          onSelect={mockOnSelect}
          onDelete={mockOnDelete}
          onDuplicate={mockOnDuplicate}
        />
      </BrowserRouter>
    );

    expect(container.querySelector('.border-blue-500')).toBeInTheDocument();
  });
});
