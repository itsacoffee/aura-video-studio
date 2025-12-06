/**
 * Tests for layout token updates for tighter gutters
 */

import { describe, it, expect } from 'vitest';
import { pageLayout, panelLayout, container } from '../layout';

describe('Layout token improvements', () => {
  it('should have tighter sidebar width for expanded state', () => {
    expect(panelLayout.sidebarWidth).toBe('232px');
  });

  it('should have appropriate collapsed sidebar width', () => {
    expect(panelLayout.sidebarWidthCollapsed).toBe('72px');
  });

  it('should use responsive padding with clamp for better space utilization', () => {
    expect(pageLayout.pagePadding).toBe('clamp(16px, 3vw, 28px)');
  });

  it('should have 1440px max content width for 16:9 desktop optimization', () => {
    expect(pageLayout.maxContentWidth).toBe('1440px');
  });

  it('should have 1440px wide container max-width', () => {
    expect(container.wideMaxWidth).toBe('1440px');
  });
});
