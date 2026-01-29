/**
 * Unit tests for LandingPage component
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { LandingPage } from '../../src/pages/LandingPage';
import * as api from '../../src/services/api';

// Mock the API module
vi.mock('../../src/services/api', () => ({
  healthApi: {
    getHealthStatus: vi.fn(),
  },
}));

describe('LandingPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Mock localStorage
    vi.spyOn(storageMock, 'getItem').mockReturnValue(null);
    vi.spyOn(storageMock, 'setItem').mockImplementation(() => {});
  });

  const storageMock = {
    getItem: vi.fn(),
    setItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn(),
  };

  Object.defineProperty(global, 'localStorage', {
    value: storageMock,
  });

  describe('Component Rendering', () => {
    it('renders "FelizesTracker" as the main title', async () => {
      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue({
        status: 'healthy',
        timestamp: '2024-01-15T10:30:00Z',
        version: '1.0.0',
      });

      render(<LandingPage />);

      expect(screen.getByText('FelizesTracker')).toBeInTheDocument();
    });

    it('renders subtitle text', async () => {
      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue({
        status: 'healthy',
        timestamp: '2024-01-15T10:30:00Z',
      });

      render(<LandingPage />);

      expect(
        screen.getByText('Track your feliz journey with confidence')
      ).toBeInTheDocument();
    });

    it('applies correct CSS class names', async () => {
      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue({
        status: 'healthy',
        timestamp: '2024-01-15T10:30:00Z',
      });

      const { container } = render(<LandingPage />);

      expect(container.querySelector('.landing-page')).toBeInTheDocument();
      expect(container.querySelector('.landing-header')).toBeInTheDocument();
      expect(container.querySelector('.landing-main')).toBeInTheDocument();
    });
  });

  describe('Health Status - Healthy State', () => {
    it('displays healthy status when API returns healthy', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
        version: '1.0.0',
        uptime: 3600,
        services: {
          database: 'up' as const,
          cache: 'up' as const,
        },
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      render(<LandingPage />);

      await waitFor(() => {
        expect(screen.getByText('Healthy')).toBeInTheDocument();
      });
    });

    it('displays version information when available', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
        version: '1.0.0',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      render(<LandingPage />);

      await waitFor(() => {
        expect(screen.getByText(/Version: 1.0.0/)).toBeInTheDocument();
      });
    });

    it('displays uptime information when available', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
        uptime: 3600,
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      render(<LandingPage />);

      await waitFor(() => {
        expect(screen.getByText(/Uptime: 60 minutes/)).toBeInTheDocument();
      });
    });

    it('displays service status when available', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
        services: {
          database: 'up' as const,
          cache: 'up' as const,
        },
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      render(<LandingPage />);

      await waitFor(() => {
        expect(screen.getByText(/Database: up/)).toBeInTheDocument();
        expect(screen.getByText(/Cache: up/)).toBeInTheDocument();
      });
    });
  });

  describe('Health Status - Unhealthy State', () => {
    it('displays unhealthy status when API returns unhealthy', async () => {
      const mockHealthData = {
        status: 'unhealthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
        error: 'Database connection failed',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      render(<LandingPage />);

      await waitFor(() => {
        expect(screen.getByText('Unhealthy')).toBeInTheDocument();
      });
    });
  });

  describe('Health Status - Degraded State', () => {
    it('displays degraded status when API returns degraded', async () => {
      const mockHealthData = {
        status: 'degraded' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      render(<LandingPage />);

      await waitFor(() => {
        expect(screen.getByText('Degraded')).toBeInTheDocument();
      });
    });
  });

  describe('Loading State', () => {
    it('displays loading indicator while fetching', () => {
      vi.mocked(api.healthApi.getHealthStatus).mockImplementation(
        () =>
          new Promise((resolve) => {
            setTimeout(() => {
              resolve({
                status: 'healthy',
                timestamp: '2024-01-15T10:30:00Z',
              });
            }, 100);
          })
      );

      render(<LandingPage />);

      expect(screen.getByText('Checking system status...')).toBeInTheDocument();
    });
  });

  describe('Error State', () => {
    it('displays error message when API call fails', async () => {
      vi.mocked(api.healthApi.getHealthStatus).mockRejectedValue(
        new Error('Network error')
      );

      render(<LandingPage />);

      await waitFor(() => {
        expect(screen.getByText('Unable to check status')).toBeInTheDocument();
        expect(screen.getByText('Network error')).toBeInTheDocument();
      });
    });

    it('displays retry button when error occurs', async () => {
      vi.mocked(api.healthApi.getHealthStatus).mockRejectedValue(
        new Error('Network error')
      );

      render(<LandingPage />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
      });
    });

    it('retries health check when retry button is clicked', async () => {
      vi.mocked(api.healthApi.getHealthStatus)
        .mockRejectedValueOnce(new Error('Network error'))
        .mockResolvedValueOnce({
          status: 'healthy',
          timestamp: '2024-01-15T10:30:00Z',
        });

      render(<LandingPage />);

      await waitFor(() => {
        expect(screen.getByText('Unable to check status')).toBeInTheDocument();
      });

      const retryButton = screen.getByRole('button', { name: /retry/i });
      await userEvent.click(retryButton);

      await waitFor(() => {
        expect(screen.getByText('Healthy')).toBeInTheDocument();
      });
    });
  });

  describe('Refresh Functionality', () => {
    it('displays refresh button when health status is loaded', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      render(<LandingPage />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /refresh health status/i })).toBeInTheDocument();
      });
    });

    it('refetches health status when refresh button is clicked', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      render(<LandingPage />);

      await waitFor(() => {
        expect(screen.getByText('Healthy')).toBeInTheDocument();
      });

      expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(1);

      const refreshButton = screen.getByRole('button', { name: /refresh health status/i });
      await userEvent.click(refreshButton);

      await waitFor(() => {
        expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(2);
      });
    });
  });

  describe('Polling', () => {
    it('sets up polling when pollInterval is provided', async () => {
      vi.useFakeTimers();

      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      render(<LandingPage pollInterval={5000} />);

      await waitFor(() => {
        expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(1);
      });

      vi.advanceTimersByTime(5000);

      await waitFor(() => {
        expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(2);
      });

      vi.useRealTimers();
    });

    it('does not poll when pollInterval is 0', async () => {
      vi.useFakeTimers();

      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      render(<LandingPage pollInterval={0} />);

      await waitFor(() => {
        expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(1);
      });

      vi.advanceTimersByTime(5000);

      expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(1);

      vi.useRealTimers();
    });
  });

  describe('Accessibility', () => {
    it('has proper ARIA labels for status indicators', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      render(<LandingPage />);

      await waitFor(() => {
        const statusRegion = screen.getByRole('status');
        expect(statusRegion).toBeInTheDocument();
        expect(statusRegion).toHaveAttribute('aria-live', 'polite');
      });
    });

    it('has accessible button labels', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      render(<LandingPage />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /refresh health status/i })).toBeInTheDocument();
      });
    });
  });
});
