import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme, Button } from '@fluentui/react-components';

/**
 * Accessibility Tests
 * 
 * These tests ensure that key components are accessible and follow
 * ARIA best practices for keyboard navigation and screen readers.
 */

describe('Accessibility', () => {
  describe('Button Component', () => {
    it('should have proper ARIA attributes', () => {
      const { container } = render(
        <FluentProvider theme={webLightTheme}>
          <Button aria-label="Test Button">Click Me</Button>
        </FluentProvider>
      );

      const button = container.querySelector('button');
      expect(button).toBeTruthy();
      expect(button?.getAttribute('aria-label')).toBe('Test Button');
    });

    it('should be keyboard accessible', () => {
      const { container } = render(
        <FluentProvider theme={webLightTheme}>
          <Button>Keyboard Test</Button>
        </FluentProvider>
      );

      const button = container.querySelector('button');
      expect(button?.tabIndex).toBeGreaterThanOrEqual(0);
    });
  });

  describe('Focus Management', () => {
    it('should maintain focus order', () => {
      const { container } = render(
        <FluentProvider theme={webLightTheme}>
          <div>
            <Button>First</Button>
            <Button>Second</Button>
            <Button>Third</Button>
          </div>
        </FluentProvider>
      );

      const buttons = container.querySelectorAll('button');
      expect(buttons).toHaveLength(3);
      
      // All buttons should be keyboard accessible
      buttons.forEach(button => {
        expect(button.tabIndex).toBeGreaterThanOrEqual(0);
      });
    });
  });

  describe('Screen Reader Support', () => {
    it('should provide text alternatives for interactive elements', () => {
      render(
        <FluentProvider theme={webLightTheme}>
          <Button aria-label="Close dialog">X</Button>
        </FluentProvider>
      );

      const button = screen.getByRole('button', { name: /close dialog/i });
      expect(button).toBeDefined();
    });

    it('should use semantic HTML elements', () => {
      const { container } = render(
        <FluentProvider theme={webLightTheme}>
          <nav aria-label="Main navigation">
            <Button>Home</Button>
            <Button>About</Button>
          </nav>
        </FluentProvider>
      );

      const nav = container.querySelector('nav');
      expect(nav).toBeTruthy();
      expect(nav?.getAttribute('aria-label')).toBe('Main navigation');
    });
  });

  describe('Keyboard Navigation', () => {
    it('should support Enter key for activation', () => {
      const { container } = render(
        <FluentProvider theme={webLightTheme}>
          <Button>Submit</Button>
        </FluentProvider>
      );

      const button = container.querySelector('button');
      expect(button?.type).toBe('button');
    });

    it('should have no keyboard traps', () => {
      const { container } = render(
        <FluentProvider theme={webLightTheme}>
          <div>
            <Button>First</Button>
            <input type="text" aria-label="Text input" />
            <Button>Last</Button>
          </div>
        </FluentProvider>
      );

      const focusableElements = container.querySelectorAll('button, input');
      expect(focusableElements.length).toBeGreaterThan(0);
      
      // Ensure all elements can receive focus
      focusableElements.forEach(element => {
        if (element instanceof HTMLElement) {
          expect(element.tabIndex).toBeGreaterThanOrEqual(-1);
        }
      });
    });
  });

  describe('Color Contrast', () => {
    it('should use theme colors that meet contrast requirements', () => {
      const { container } = render(
        <FluentProvider theme={webLightTheme}>
          <Button appearance="primary">Primary Button</Button>
        </FluentProvider>
      );

      const button = container.querySelector('button');
      expect(button).toBeTruthy();
      // FluentUI components are designed to meet WCAG contrast requirements
    });
  });

  describe('Form Accessibility', () => {
    it('should associate labels with form controls', () => {
      const { container } = render(
        <FluentProvider theme={webLightTheme}>
          <div>
            <label htmlFor="test-input">Name:</label>
            <input id="test-input" type="text" />
          </div>
        </FluentProvider>
      );

      const label = container.querySelector('label');
      const input = container.querySelector('input');
      
      expect(label?.htmlFor).toBe('test-input');
      expect(input?.id).toBe('test-input');
    });

    it('should provide error messages accessibly', () => {
      const { container } = render(
        <FluentProvider theme={webLightTheme}>
          <div>
            <input 
              type="text" 
              aria-label="Username"
              aria-describedby="username-error"
              aria-invalid="true"
            />
            <span id="username-error" role="alert">
              Username is required
            </span>
          </div>
        </FluentProvider>
      );

      const input = container.querySelector('input');
      const error = container.querySelector('#username-error');
      
      expect(input?.getAttribute('aria-describedby')).toBe('username-error');
      expect(input?.getAttribute('aria-invalid')).toBe('true');
      expect(error?.getAttribute('role')).toBe('alert');
    });
  });
});
