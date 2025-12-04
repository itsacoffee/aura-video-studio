/**
 * Tests for Stack, Cluster, and Region layout components
 */

import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { Cluster } from '../Cluster';
import { Region } from '../Region';
import { Stack } from '../Stack';

describe('Stack component', () => {
  it('should render children with stack class', () => {
    render(
      <Stack data-testid="stack">
        <div>Item 1</div>
        <div>Item 2</div>
      </Stack>
    );

    const stack = screen.getByTestId('stack');
    expect(stack).toHaveClass('stack');
    expect(stack).toHaveClass('stack-md'); // default
  });

  it('should apply correct spacing class for each size', () => {
    const { rerender } = render(
      <Stack space="xs" data-testid="stack">
        <div>Content</div>
      </Stack>
    );

    expect(screen.getByTestId('stack')).toHaveClass('stack-xs');

    rerender(
      <Stack space="sm" data-testid="stack">
        <div>Content</div>
      </Stack>
    );
    expect(screen.getByTestId('stack')).toHaveClass('stack-sm');

    rerender(
      <Stack space="lg" data-testid="stack">
        <div>Content</div>
      </Stack>
    );
    expect(screen.getByTestId('stack')).toHaveClass('stack-lg');

    rerender(
      <Stack space="xl" data-testid="stack">
        <div>Content</div>
      </Stack>
    );
    expect(screen.getByTestId('stack')).toHaveClass('stack-xl');
  });

  it('should render as different HTML elements', () => {
    const { rerender } = render(
      <Stack as="section" data-testid="stack">
        <div>Content</div>
      </Stack>
    );

    expect(screen.getByTestId('stack').tagName).toBe('SECTION');

    rerender(
      <Stack as="article" data-testid="stack">
        <div>Content</div>
      </Stack>
    );
    expect(screen.getByTestId('stack').tagName).toBe('ARTICLE');
  });

  it('should accept custom class names', () => {
    render(
      <Stack className="custom-class" data-testid="stack">
        <div>Content</div>
      </Stack>
    );

    expect(screen.getByTestId('stack')).toHaveClass('custom-class');
    expect(screen.getByTestId('stack')).toHaveClass('stack');
  });

  it('should apply custom spacing via CSS variable', () => {
    render(
      <Stack customSpace="var(--space-6)" data-testid="stack">
        <div>Content</div>
      </Stack>
    );

    const stack = screen.getByTestId('stack');
    expect(stack).toHaveStyle({ '--stack-space': 'var(--space-6)' });
  });
});

describe('Cluster component', () => {
  it('should render children with cluster class', () => {
    render(
      <Cluster data-testid="cluster">
        <span>Item 1</span>
        <span>Item 2</span>
      </Cluster>
    );

    const cluster = screen.getByTestId('cluster');
    expect(cluster).toHaveClass('cluster');
    expect(cluster).toHaveClass('cluster-md'); // default
  });

  it('should apply correct spacing class for each size', () => {
    const { rerender } = render(
      <Cluster space="xs" data-testid="cluster">
        <span>Content</span>
      </Cluster>
    );

    expect(screen.getByTestId('cluster')).toHaveClass('cluster-xs');

    rerender(
      <Cluster space="sm" data-testid="cluster">
        <span>Content</span>
      </Cluster>
    );
    expect(screen.getByTestId('cluster')).toHaveClass('cluster-sm');

    rerender(
      <Cluster space="lg" data-testid="cluster">
        <span>Content</span>
      </Cluster>
    );
    expect(screen.getByTestId('cluster')).toHaveClass('cluster-lg');
  });

  it('should apply custom alignment', () => {
    render(
      <Cluster align="baseline" data-testid="cluster">
        <span>Content</span>
      </Cluster>
    );

    expect(screen.getByTestId('cluster')).toHaveStyle({ alignItems: 'baseline' });
  });

  it('should apply custom justification', () => {
    render(
      <Cluster justify="space-between" data-testid="cluster">
        <span>Content</span>
      </Cluster>
    );

    expect(screen.getByTestId('cluster')).toHaveStyle({ justifyContent: 'space-between' });
  });

  it('should render as different HTML elements', () => {
    const { rerender } = render(
      <Cluster as="nav" data-testid="cluster">
        <span>Content</span>
      </Cluster>
    );

    expect(screen.getByTestId('cluster').tagName).toBe('NAV');

    rerender(
      <Cluster as="ul" data-testid="cluster">
        <li>Content</li>
      </Cluster>
    );
    expect(screen.getByTestId('cluster').tagName).toBe('UL');
  });

  it('should accept custom class names', () => {
    render(
      <Cluster className="custom-class" data-testid="cluster">
        <span>Content</span>
      </Cluster>
    );

    expect(screen.getByTestId('cluster')).toHaveClass('custom-class');
    expect(screen.getByTestId('cluster')).toHaveClass('cluster');
  });

  it('should apply custom spacing via CSS variable', () => {
    render(
      <Cluster customSpace="var(--space-4)" data-testid="cluster">
        <span>Content</span>
      </Cluster>
    );

    const cluster = screen.getByTestId('cluster');
    expect(cluster).toHaveStyle({ '--cluster-space': 'var(--space-4)' });
  });
});

describe('Region component', () => {
  it('should render children with region class', () => {
    render(
      <Region data-testid="region">
        <div>Content</div>
      </Region>
    );

    const region = screen.getByTestId('region');
    expect(region).toHaveClass('region');
  });

  it('should apply correct spacing class for each size', () => {
    const { rerender } = render(
      <Region space="sm" data-testid="region">
        <div>Content</div>
      </Region>
    );

    expect(screen.getByTestId('region')).toHaveClass('region-sm');

    rerender(
      <Region space="lg" data-testid="region">
        <div>Content</div>
      </Region>
    );
    expect(screen.getByTestId('region')).toHaveClass('region-lg');

    rerender(
      <Region space="xl" data-testid="region">
        <div>Content</div>
      </Region>
    );
    expect(screen.getByTestId('region')).toHaveClass('region-xl');

    // md is default and doesn't add a class
    rerender(
      <Region space="md" data-testid="region">
        <div>Content</div>
      </Region>
    );
    expect(screen.getByTestId('region')).toHaveClass('region');
    expect(screen.getByTestId('region')).not.toHaveClass('region-md');
  });

  it('should render as different HTML elements', () => {
    const { rerender } = render(
      <Region as="section" data-testid="region">
        <div>Content</div>
      </Region>
    );

    expect(screen.getByTestId('region').tagName).toBe('SECTION');

    rerender(
      <Region as="article" data-testid="region">
        <div>Content</div>
      </Region>
    );
    expect(screen.getByTestId('region').tagName).toBe('ARTICLE');

    rerender(
      <Region as="main" data-testid="region">
        <div>Content</div>
      </Region>
    );
    expect(screen.getByTestId('region').tagName).toBe('MAIN');
  });

  it('should accept custom class names', () => {
    render(
      <Region className="custom-class" data-testid="region">
        <div>Content</div>
      </Region>
    );

    expect(screen.getByTestId('region')).toHaveClass('custom-class');
    expect(screen.getByTestId('region')).toHaveClass('region');
  });

  it('should apply custom spacing via CSS variable', () => {
    render(
      <Region customSpace="var(--space-8)" data-testid="region">
        <div>Content</div>
      </Region>
    );

    const region = screen.getByTestId('region');
    expect(region).toHaveStyle({ '--region-space': 'var(--space-8)' });
  });

  it('should support aria attributes for accessibility', () => {
    render(
      <Region as="section" aria-labelledby="section-heading" data-testid="region">
        <h2 id="section-heading">Title</h2>
        <p>Content</p>
      </Region>
    );

    expect(screen.getByTestId('region')).toHaveAttribute('aria-labelledby', 'section-heading');
  });
});
