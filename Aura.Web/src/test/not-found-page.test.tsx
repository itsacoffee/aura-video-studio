import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { NotFoundPage } from '../pages/NotFoundPage';

describe('NotFoundPage', () => {
  it('should render 404 error page', () => {
    render(
      <BrowserRouter>
        <FluentProvider theme={webLightTheme}>
          <NotFoundPage />
        </FluentProvider>
      </BrowserRouter>
    );

    expect(screen.getByText('404')).toBeDefined();
    expect(screen.getByText('Page Not Found')).toBeDefined();
  });

  it('should have navigation buttons', () => {
    render(
      <BrowserRouter>
        <FluentProvider theme={webLightTheme}>
          <NotFoundPage />
        </FluentProvider>
      </BrowserRouter>
    );

    expect(screen.getByText('Go to Home')).toBeDefined();
    expect(screen.getByText('Go Back')).toBeDefined();
  });
});
