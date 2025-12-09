/**
 * Tests for Panel System components
 */

import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { CollapsiblePanel } from '../CollapsiblePanel';
import { PanelDivider } from '../PanelDivider';
import { PanelSystemProvider, usePanelSystem, usePanel } from '../PanelSystem';
import { ResizablePanel } from '../ResizablePanel';

// Mock useDisplayEnvironment hook
vi.mock('../../../hooks/useDisplayEnvironment', () => ({
  useDisplayEnvironment: () => ({
    viewportWidth: 1920,
    viewportHeight: 1080,
    sizeClass: 'expanded',
    panelLayout: 'three-panel',
  }),
}));

describe('PanelSystemProvider', () => {
  it('provides panel context to children', () => {
    let contextValue: ReturnType<typeof usePanelSystem> | null = null;

    function TestConsumer() {
      contextValue = usePanelSystem();
      return <div>Test</div>;
    }

    render(
      <PanelSystemProvider>
        <TestConsumer />
      </PanelSystemProvider>
    );

    expect(contextValue).not.toBeNull();
    expect(contextValue?.panels).toBeInstanceOf(Map);
    expect(typeof contextValue?.registerPanel).toBe('function');
    expect(typeof contextValue?.togglePanel).toBe('function');
  });

  it('throws error when used outside provider', () => {
    function TestComponent() {
      usePanelSystem();
      return <div>Test</div>;
    }

    // Suppress console.error for this test
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

    expect(() => render(<TestComponent />)).toThrow(
      'usePanelSystem must be used within PanelSystemProvider'
    );

    consoleSpy.mockRestore();
  });
});

describe('usePanel hook', () => {
  it('returns panel state and controls', () => {
    let panelState: ReturnType<typeof usePanel> | null = null;

    function TestComponent() {
      panelState = usePanel('sidebar');
      return <div>Test</div>;
    }

    render(
      <PanelSystemProvider>
        <TestComponent />
      </PanelSystemProvider>
    );

    expect(panelState).not.toBeNull();
    expect(typeof panelState?.isVisible).toBe('boolean');
    expect(typeof panelState?.isCollapsed).toBe('boolean');
    expect(typeof panelState?.width).toBe('number');
    expect(typeof panelState?.toggle).toBe('function');
    expect(typeof panelState?.collapse).toBe('function');
    expect(typeof panelState?.expand).toBe('function');
    expect(typeof panelState?.setWidth).toBe('function');
  });
});

describe('ResizablePanel', () => {
  it('renders with correct initial width', () => {
    render(
      <ResizablePanel
        id="test-panel"
        minWidth={200}
        maxWidth={400}
        defaultWidth={280}
        position="left"
      >
        <div data-testid="content">Content</div>
      </ResizablePanel>
    );

    const panel = document.querySelector('[data-panel-id="test-panel"]');
    expect(panel).toBeInTheDocument();
    expect(panel).toHaveStyle({ width: '280px' });
    expect(screen.getByTestId('content')).toBeInTheDocument();
  });

  it('has accessible resize handle', () => {
    render(
      <ResizablePanel
        id="test-panel"
        minWidth={200}
        maxWidth={400}
        defaultWidth={280}
        position="left"
      >
        <div>Content</div>
      </ResizablePanel>
    );

    const handle = screen.getByRole('separator');
    expect(handle).toBeInTheDocument();
    expect(handle).toHaveAttribute('aria-label', 'Resize test-panel panel');
    expect(handle).toHaveAttribute('aria-orientation', 'vertical');
  });

  it('handles keyboard navigation for accessibility', () => {
    const onResize = vi.fn();
    render(
      <ResizablePanel
        id="test-panel"
        minWidth={200}
        maxWidth={400}
        defaultWidth={280}
        position="left"
        onResize={onResize}
      >
        <div>Content</div>
      </ResizablePanel>
    );

    const handle = screen.getByRole('separator');
    handle.focus();

    // Arrow right should increase width for left panel
    fireEvent.keyDown(handle, { key: 'ArrowRight' });
    expect(onResize).toHaveBeenCalled();
  });
});

describe('CollapsiblePanel', () => {
  it('renders in expanded state by default', () => {
    render(
      <CollapsiblePanel id="test-collapse" expandedWidth={280} collapsedWidth={48} position="left">
        <div data-testid="content">Content</div>
      </CollapsiblePanel>
    );

    const panel = document.querySelector('[data-panel-id="test-collapse"]');
    expect(panel).toBeInTheDocument();
    expect(panel).toHaveStyle({ width: '280px' });
    expect(panel).toHaveAttribute('data-collapsed', 'false');
  });

  it('toggles collapsed state when button clicked', () => {
    const onCollapsedChange = vi.fn();
    render(
      <CollapsiblePanel
        id="test-collapse"
        expandedWidth={280}
        collapsedWidth={48}
        position="left"
        onCollapsedChange={onCollapsedChange}
      >
        <div>Content</div>
      </CollapsiblePanel>
    );

    const collapseButton = screen.getByRole('button', { name: /collapse/i });
    fireEvent.click(collapseButton);

    expect(onCollapsedChange).toHaveBeenCalledWith(true);
  });

  it('shows collapsed content when collapsed', () => {
    render(
      <CollapsiblePanel
        id="test-collapse"
        expandedWidth={280}
        collapsedWidth={48}
        position="left"
        collapsed={true}
        collapsedContent={<div data-testid="collapsed-content">Icons</div>}
      >
        <div data-testid="full-content">Full Content</div>
      </CollapsiblePanel>
    );

    expect(screen.getByTestId('collapsed-content')).toBeInTheDocument();
  });

  it('supports controlled collapsed state', () => {
    const { rerender } = render(
      <CollapsiblePanel
        id="test-collapse"
        expandedWidth={280}
        collapsedWidth={48}
        position="left"
        collapsed={false}
      >
        <div>Content</div>
      </CollapsiblePanel>
    );

    let panel = document.querySelector('[data-panel-id="test-collapse"]');
    expect(panel).toHaveStyle({ width: '280px' });

    rerender(
      <CollapsiblePanel
        id="test-collapse"
        expandedWidth={280}
        collapsedWidth={48}
        position="left"
        collapsed={true}
      >
        <div>Content</div>
      </CollapsiblePanel>
    );

    panel = document.querySelector('[data-panel-id="test-collapse"]');
    expect(panel).toHaveStyle({ width: '48px' });
  });
});

describe('PanelDivider', () => {
  it('renders with correct orientation', () => {
    render(<PanelDivider id="test-divider" orientation="vertical" />);

    const divider = screen.getByRole('separator');
    expect(divider).toBeInTheDocument();
    expect(divider).toHaveAttribute('aria-orientation', 'vertical');
  });

  it('handles keyboard resize for vertical divider', () => {
    const onResize = vi.fn();
    render(<PanelDivider id="test-divider" orientation="vertical" onResize={onResize} />);

    const divider = screen.getByRole('separator');
    divider.focus();

    fireEvent.keyDown(divider, { key: 'ArrowRight' });
    expect(onResize).toHaveBeenCalledWith(10);

    fireEvent.keyDown(divider, { key: 'ArrowLeft' });
    expect(onResize).toHaveBeenCalledWith(-10);
  });

  it('handles keyboard resize for horizontal divider', () => {
    const onResize = vi.fn();
    render(<PanelDivider id="test-divider" orientation="horizontal" onResize={onResize} />);

    const divider = screen.getByRole('separator');
    divider.focus();

    fireEvent.keyDown(divider, { key: 'ArrowDown' });
    expect(onResize).toHaveBeenCalledWith(10);

    fireEvent.keyDown(divider, { key: 'ArrowUp' });
    expect(onResize).toHaveBeenCalledWith(-10);
  });

  it('uses larger step with shift key', () => {
    const onResize = vi.fn();
    render(<PanelDivider id="test-divider" orientation="vertical" onResize={onResize} />);

    const divider = screen.getByRole('separator');
    divider.focus();

    fireEvent.keyDown(divider, { key: 'ArrowRight', shiftKey: true });
    expect(onResize).toHaveBeenCalledWith(50);
  });

  it('calls onResizeStart and onResizeEnd callbacks', async () => {
    const onResizeStart = vi.fn();
    const onResizeEnd = vi.fn();
    render(
      <PanelDivider
        id="test-divider"
        orientation="vertical"
        onResizeStart={onResizeStart}
        onResizeEnd={onResizeEnd}
      />
    );

    const divider = screen.getByRole('separator');

    // Simulate mouse down followed by mouse up
    fireEvent.mouseDown(divider, { clientX: 100 });
    expect(onResizeStart).toHaveBeenCalled();

    // Mouse up ends the resize - need to dispatch on document where listener is attached
    fireEvent.mouseUp(document);
    expect(onResizeEnd).toHaveBeenCalled();
  });

  it('has custom aria label when provided', () => {
    render(<PanelDivider id="test-divider" orientation="vertical" ariaLabel="Resize sidebar" />);

    const divider = screen.getByRole('separator', { name: 'Resize sidebar' });
    expect(divider).toBeInTheDocument();
  });
});
