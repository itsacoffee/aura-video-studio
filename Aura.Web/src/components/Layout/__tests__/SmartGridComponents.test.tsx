/**
 * Tests for Smart Grid Components
 */

import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AutoGrid } from '../AutoGrid';
import { MasonryGrid } from '../MasonryGrid';
import { ResponsiveDataGrid } from '../ResponsiveDataGrid';
import { SmartGrid } from '../SmartGrid';

// Mock useDisplayEnvironment hook
vi.mock('../../../hooks/useDisplayEnvironment', () => ({
  useDisplayEnvironment: vi.fn(() => ({
    viewportWidth: 1200,
    viewportHeight: 800,
    sizeClass: 'regular',
    densityClass: 'standard',
  })),
}));

describe('SmartGrid component', () => {
  it('should render children', () => {
    render(
      <SmartGrid data-testid="smart-grid">
        <div>Item 1</div>
        <div>Item 2</div>
        <div>Item 3</div>
      </SmartGrid>
    );

    expect(screen.getByTestId('smart-grid')).toBeInTheDocument();
    expect(screen.getByText('Item 1')).toBeInTheDocument();
    expect(screen.getByText('Item 2')).toBeInTheDocument();
    expect(screen.getByText('Item 3')).toBeInTheDocument();
  });

  it('should apply different item types', () => {
    const { rerender } = render(
      <SmartGrid itemType="card" data-testid="smart-grid">
        <div>Content</div>
      </SmartGrid>
    );

    expect(screen.getByTestId('smart-grid')).toBeInTheDocument();

    rerender(
      <SmartGrid itemType="thumbnail" data-testid="smart-grid">
        <div>Content</div>
      </SmartGrid>
    );
    expect(screen.getByTestId('smart-grid')).toBeInTheDocument();

    rerender(
      <SmartGrid itemType="tile" data-testid="smart-grid">
        <div>Content</div>
      </SmartGrid>
    );
    expect(screen.getByTestId('smart-grid')).toBeInTheDocument();

    rerender(
      <SmartGrid itemType="list-item" data-testid="smart-grid">
        <div>Content</div>
      </SmartGrid>
    );
    expect(screen.getByTestId('smart-grid')).toBeInTheDocument();
  });

  it('should apply gap styles', () => {
    const { rerender } = render(
      <SmartGrid gap="tight" data-testid="smart-grid">
        <div>Content</div>
      </SmartGrid>
    );

    expect(screen.getByTestId('smart-grid')).toBeInTheDocument();

    rerender(
      <SmartGrid gap="normal" data-testid="smart-grid">
        <div>Content</div>
      </SmartGrid>
    );
    expect(screen.getByTestId('smart-grid')).toBeInTheDocument();

    rerender(
      <SmartGrid gap="wide" data-testid="smart-grid">
        <div>Content</div>
      </SmartGrid>
    );
    expect(screen.getByTestId('smart-grid')).toBeInTheDocument();
  });

  it('should accept custom class names', () => {
    render(
      <SmartGrid className="custom-class" data-testid="smart-grid">
        <div>Content</div>
      </SmartGrid>
    );

    expect(screen.getByTestId('smart-grid')).toHaveClass('custom-class');
  });

  it('should set data-columns attribute', () => {
    render(
      <SmartGrid data-testid="smart-grid">
        <div>Content</div>
      </SmartGrid>
    );

    expect(screen.getByTestId('smart-grid')).toHaveAttribute('data-columns');
  });
});

describe('MasonryGrid component', () => {
  beforeEach(() => {
    // Mock offsetWidth and offsetHeight for masonry calculations
    Object.defineProperty(HTMLElement.prototype, 'offsetWidth', {
      configurable: true,
      value: 900,
    });
    Object.defineProperty(HTMLElement.prototype, 'offsetHeight', {
      configurable: true,
      value: 100,
    });
  });

  it('should render children', () => {
    render(
      <MasonryGrid data-testid="masonry-grid">
        <div>Item 1</div>
        <div>Item 2</div>
        <div>Item 3</div>
      </MasonryGrid>
    );

    expect(screen.getByTestId('masonry-grid')).toBeInTheDocument();
    expect(screen.getByText('Item 1')).toBeInTheDocument();
    expect(screen.getByText('Item 2')).toBeInTheDocument();
    expect(screen.getByText('Item 3')).toBeInTheDocument();
  });

  it('should accept custom column width', () => {
    render(
      <MasonryGrid columnWidth={200} data-testid="masonry-grid">
        <div>Content</div>
      </MasonryGrid>
    );

    expect(screen.getByTestId('masonry-grid')).toBeInTheDocument();
  });

  it('should accept custom gap', () => {
    render(
      <MasonryGrid gap={24} data-testid="masonry-grid">
        <div>Content</div>
      </MasonryGrid>
    );

    expect(screen.getByTestId('masonry-grid')).toBeInTheDocument();
  });

  it('should accept custom class names', () => {
    render(
      <MasonryGrid className="custom-class" data-testid="masonry-grid">
        <div>Content</div>
      </MasonryGrid>
    );

    expect(screen.getByTestId('masonry-grid')).toHaveClass('custom-class');
  });
});

describe('ResponsiveDataGrid component', () => {
  interface TestData {
    id: number;
    name: string;
    email: string;
  }

  const testData: TestData[] = [
    { id: 1, name: 'Alice', email: 'alice@example.com' },
    { id: 2, name: 'Bob', email: 'bob@example.com' },
  ];

  const columns = [
    { key: 'name' as const, header: 'Name', priority: 1 },
    { key: 'email' as const, header: 'Email', priority: 2 },
  ];

  it('should render table view on larger screens', () => {
    render(
      <ResponsiveDataGrid
        data={testData}
        columns={columns}
        breakpoint={768}
        data-testid="data-grid"
      />
    );

    expect(screen.getByTestId('data-grid')).toBeInTheDocument();
    expect(screen.getByText('Name')).toBeInTheDocument();
    expect(screen.getByText('Email')).toBeInTheDocument();
    expect(screen.getByText('Alice')).toBeInTheDocument();
    expect(screen.getByText('Bob')).toBeInTheDocument();
  });

  it('should render column headers', () => {
    render(<ResponsiveDataGrid data={testData} columns={columns} data-testid="data-grid" />);

    const headers = screen.getAllByRole('columnheader');
    expect(headers).toHaveLength(2);
    expect(headers[0]).toHaveTextContent('Name');
    expect(headers[1]).toHaveTextContent('Email');
  });

  it('should render data rows', () => {
    render(<ResponsiveDataGrid data={testData} columns={columns} data-testid="data-grid" />);

    const rows = screen.getAllByRole('row');
    // 1 header row + 2 data rows
    expect(rows).toHaveLength(3);
  });

  it('should accept custom class names', () => {
    render(
      <ResponsiveDataGrid
        data={testData}
        columns={columns}
        className="custom-class"
        data-testid="data-grid"
      />
    );

    expect(screen.getByTestId('data-grid')).toHaveClass('custom-class');
  });

  it('should support custom render function', () => {
    const columnsWithRender = [
      {
        key: 'name' as const,
        header: 'Name',
        render: (value: unknown) => <strong>{String(value)}</strong>,
      },
      { key: 'email' as const, header: 'Email' },
    ];

    render(
      <ResponsiveDataGrid data={testData} columns={columnsWithRender} data-testid="data-grid" />
    );

    const strongElements = screen.getAllByText('Alice')[0].closest('strong');
    expect(strongElements).toBeInTheDocument();
  });

  it('should support custom getRowKey function', () => {
    render(
      <ResponsiveDataGrid
        data={testData}
        columns={columns}
        getRowKey={(item) => item.id}
        data-testid="data-grid"
      />
    );

    const rows = screen.getAllByRole('row');
    expect(rows).toHaveLength(3);
  });

  it('should use id field as key when available', () => {
    render(<ResponsiveDataGrid data={testData} columns={columns} data-testid="data-grid" />);

    // If data has id field, it should be used without getRowKey
    const rows = screen.getAllByRole('row');
    expect(rows).toHaveLength(3);
  });
});

describe('AutoGrid component', () => {
  it('should render children', () => {
    render(
      <AutoGrid data-testid="auto-grid">
        <div>Item 1</div>
        <div>Item 2</div>
        <div>Item 3</div>
      </AutoGrid>
    );

    expect(screen.getByTestId('auto-grid')).toBeInTheDocument();
    expect(screen.getByText('Item 1')).toBeInTheDocument();
    expect(screen.getByText('Item 2')).toBeInTheDocument();
    expect(screen.getByText('Item 3')).toBeInTheDocument();
  });

  it('should apply grid display style', () => {
    render(
      <AutoGrid data-testid="auto-grid">
        <div>Content</div>
      </AutoGrid>
    );

    expect(screen.getByTestId('auto-grid')).toHaveStyle({ display: 'grid' });
  });

  it('should apply gap styles', () => {
    const { rerender } = render(
      <AutoGrid gap="tight" data-testid="auto-grid">
        <div>Content</div>
      </AutoGrid>
    );

    expect(screen.getByTestId('auto-grid')).toBeInTheDocument();

    rerender(
      <AutoGrid gap="normal" data-testid="auto-grid">
        <div>Content</div>
      </AutoGrid>
    );
    expect(screen.getByTestId('auto-grid')).toBeInTheDocument();

    rerender(
      <AutoGrid gap="wide" data-testid="auto-grid">
        <div>Content</div>
      </AutoGrid>
    );
    expect(screen.getByTestId('auto-grid')).toBeInTheDocument();
  });

  it('should accept custom min item width', () => {
    render(
      <AutoGrid minItemWidth={300} data-testid="auto-grid">
        <div>Content</div>
      </AutoGrid>
    );

    expect(screen.getByTestId('auto-grid')).toBeInTheDocument();
  });

  it('should accept custom max item width', () => {
    render(
      <AutoGrid maxItemWidth={400} data-testid="auto-grid">
        <div>Content</div>
      </AutoGrid>
    );

    expect(screen.getByTestId('auto-grid')).toBeInTheDocument();
  });

  it('should accept custom class names', () => {
    render(
      <AutoGrid className="custom-class" data-testid="auto-grid">
        <div>Content</div>
      </AutoGrid>
    );

    expect(screen.getByTestId('auto-grid')).toHaveClass('custom-class');
  });

  it('should accept inline styles', () => {
    render(
      <AutoGrid style={{ backgroundColor: 'red' }} data-testid="auto-grid">
        <div>Content</div>
      </AutoGrid>
    );

    const element = screen.getByTestId('auto-grid');
    // The style prop is applied but may be merged with component styles
    expect(element).toBeInTheDocument();
    expect(element.style.backgroundColor).toBe('red');
  });
});
