import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ActivityProvider } from '../../../state/activityContext';
import { GlobalStatusFooter } from '../GlobalStatusFooter';

describe('GlobalStatusFooter', () => {
  it('should not render when there are no activities', () => {
    const { container } = render(
      <FluentProvider theme={webLightTheme}>
        <ActivityProvider>
          <GlobalStatusFooter />
        </ActivityProvider>
      </FluentProvider>
    );

    // Footer should not be rendered when there are no activities
    expect(container.querySelector('.footer')).toBeNull();
  });

  it('should render when activities exist', () => {
    const { container } = render(
      <FluentProvider theme={webLightTheme}>
        <ActivityProvider>
          <GlobalStatusFooter />
        </ActivityProvider>
      </FluentProvider>
    );

    // Initially no activities, so no footer
    expect(container.querySelector('Activity Status')).toBeNull();
  });
});
