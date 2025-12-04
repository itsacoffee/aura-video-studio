import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { GenerationCostEstimate } from '../GenerationCostEstimate';

// Mock apiClient
vi.mock('@/services/api/apiClient', () => ({
  default: {
    post: vi.fn(),
  },
}));

import apiClient from '@/services/api/apiClient';

describe('GenerationCostEstimate', () => {
  const mockApiClient = vi.mocked(apiClient);

  const defaultProps = {
    estimatedScriptLength: 1000,
    sceneCount: 5,
    llmProvider: 'Ollama',
    llmModel: 'llama3',
    ttsProvider: 'Piper',
    imageProvider: 'Placeholder',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('shows loading state initially', () => {
    mockApiClient.post.mockImplementation(() => new Promise(() => {})); // Never resolves

    render(<GenerationCostEstimate {...defaultProps} />);

    expect(screen.getByText(/calculating estimated costs/i)).toBeInTheDocument();
  });

  it('displays free generation badge for free providers', async () => {
    mockApiClient.post.mockResolvedValue({
      data: {
        llmCost: 0,
        ttsCost: 0,
        imageCost: 0,
        totalCost: 0,
        currency: 'USD',
        breakdown: [
          {
            name: 'Script Generation',
            provider: 'Ollama',
            cost: 0,
            isFree: true,
            units: 500,
            unitType: 'tokens',
          },
          {
            name: 'Text-to-Speech',
            provider: 'Piper',
            cost: 0,
            isFree: true,
            units: 1000,
            unitType: 'characters',
          },
        ],
        isFreeGeneration: true,
        confidence: 'high',
      },
    });

    render(<GenerationCostEstimate {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByText(/free generation/i)).toBeInTheDocument();
    });
  });

  it('displays cost breakdown items', async () => {
    mockApiClient.post.mockResolvedValue({
      data: {
        llmCost: 0.001,
        ttsCost: 0.05,
        imageCost: 0,
        totalCost: 0.051,
        currency: 'USD',
        breakdown: [
          {
            name: 'Script Generation',
            provider: 'OpenAI (gpt-4o-mini)',
            cost: 0.001,
            isFree: false,
            units: 500,
            unitType: 'tokens',
          },
          {
            name: 'Text-to-Speech',
            provider: 'ElevenLabs',
            cost: 0.05,
            isFree: false,
            units: 1000,
            unitType: 'characters',
          },
        ],
        isFreeGeneration: false,
        confidence: 'high',
      },
    });

    render(
      <GenerationCostEstimate
        {...defaultProps}
        llmProvider="OpenAI"
        llmModel="gpt-4o-mini"
        ttsProvider="ElevenLabs"
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Script Generation')).toBeInTheDocument();
      expect(screen.getByText('Text-to-Speech')).toBeInTheDocument();
    });
  });

  it('shows total estimated cost', async () => {
    mockApiClient.post.mockResolvedValue({
      data: {
        llmCost: 0.001,
        ttsCost: 0.05,
        imageCost: 0.1,
        totalCost: 0.151,
        currency: 'USD',
        breakdown: [
          {
            name: 'Script Generation',
            provider: 'OpenAI',
            cost: 0.001,
            isFree: false,
            units: 500,
            unitType: 'tokens',
          },
          {
            name: 'Text-to-Speech',
            provider: 'ElevenLabs',
            cost: 0.05,
            isFree: false,
            units: 1000,
            unitType: 'characters',
          },
          {
            name: 'Image Generation',
            provider: 'DALL-E',
            cost: 0.1,
            isFree: false,
            units: 5,
            unitType: 'images',
          },
        ],
        isFreeGeneration: false,
        confidence: 'high',
      },
    });

    render(
      <GenerationCostEstimate
        {...defaultProps}
        llmProvider="OpenAI"
        llmModel="gpt-4o-mini"
        ttsProvider="ElevenLabs"
        imageProvider="DALL-E"
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Total Estimate')).toBeInTheDocument();
      expect(screen.getByText('$0.1510')).toBeInTheDocument();
    });
  });

  it('shows budget warning when over budget', async () => {
    mockApiClient.post.mockResolvedValue({
      data: {
        llmCost: 5.0,
        ttsCost: 0,
        imageCost: 0,
        totalCost: 5.0,
        currency: 'USD',
        breakdown: [
          {
            name: 'Script Generation',
            provider: 'OpenAI',
            cost: 5.0,
            isFree: false,
            units: 50000,
            unitType: 'tokens',
          },
        ],
        isFreeGeneration: false,
        confidence: 'high',
        budgetCheck: {
          isWithinBudget: false,
          shouldBlock: false,
          warnings: ['Would exceed monthly budget of $10.00'],
          currentMonthlyCost: 8.0,
          estimatedNewTotal: 13.0,
        },
      },
    });

    render(<GenerationCostEstimate {...defaultProps} llmProvider="OpenAI" llmModel="gpt-4" />);

    await waitFor(() => {
      expect(screen.getByText('Budget Warning')).toBeInTheDocument();
      expect(screen.getByText(/Would exceed monthly budget/i)).toBeInTheDocument();
    });
  });

  it('calls onCostEstimated callback with estimate', async () => {
    const mockEstimate = {
      llmCost: 0,
      ttsCost: 0,
      imageCost: 0,
      totalCost: 0,
      currency: 'USD',
      breakdown: [],
      isFreeGeneration: true,
      confidence: 'high' as const,
    };

    mockApiClient.post.mockResolvedValue({ data: mockEstimate });

    const onCostEstimated = vi.fn();

    render(<GenerationCostEstimate {...defaultProps} onCostEstimated={onCostEstimated} />);

    await waitFor(() => {
      expect(onCostEstimated).toHaveBeenCalledWith(mockEstimate);
    });
  });

  it('displays error message on API failure', async () => {
    mockApiClient.post.mockRejectedValue(new Error('Network error'));

    render(<GenerationCostEstimate {...defaultProps} />);

    await waitFor(() => {
      expect(screen.getByText(/could not estimate costs/i)).toBeInTheDocument();
    });
  });

  it('displays "Free (local)" for free providers in breakdown', async () => {
    mockApiClient.post.mockResolvedValue({
      data: {
        llmCost: 0,
        ttsCost: 0,
        imageCost: 0,
        totalCost: 0,
        currency: 'USD',
        breakdown: [
          {
            name: 'Script Generation',
            provider: 'Ollama',
            cost: 0,
            isFree: true,
            units: 500,
            unitType: 'tokens',
          },
          {
            name: 'Text-to-Speech',
            provider: 'Piper',
            cost: 0,
            isFree: true,
            units: 1000,
            unitType: 'characters',
          },
        ],
        isFreeGeneration: true,
        confidence: 'high',
      },
    });

    render(<GenerationCostEstimate {...defaultProps} />);

    await waitFor(() => {
      const freeLabels = screen.getAllByText(/free \(local\)/i);
      expect(freeLabels.length).toBeGreaterThan(0);
    });
  });
});
