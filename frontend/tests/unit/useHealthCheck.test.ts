/**
 * Unit tests for useHealthCheck hook
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useHealthCheck, getHealthStatusProps } from '../../src/hooks/useHealthCheck';
import * as api from '../../src/services/api';

// Mock the API module
vi.mock('../../src/services/api', () => ({
  healthApi: {
    getHealthStatus: vi.fn(),
  },
}));

describe('useHealthCheck', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
  });

  describe('Initial State', () => {
    it('starts with loading state', () => {
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

      const { result } = renderHook(() => useHealthCheck());

      expect(result.current.isLoading).toBe(true);
      expect(result.current.health).toBeNull();
      expect(result.current.error).toBeNull();
    });

    it('fetches health status on mount', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
        version: '1.0.0',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      renderHook(() => useHealthCheck());

      // Wait for the async operation to complete using fake timers
      await act(async () => {
        await vi.runAllTimersAsync();
      });

      expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(1);
    });
  });

  describe('Successful Health Check', () => {
    it('returns health data when API call succeeds', async () => {
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

      const { result } = renderHook(() => useHealthCheck());

      await act(async () => {
        await vi.runAllTimersAsync();
      });

      expect(result.current.health).toEqual(mockHealthData);
      expect(result.current.error).toBeNull();
    });

    it('sets loading to false after successful fetch', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      const { result } = renderHook(() => useHealthCheck());

      const initialLoading = result.current.isLoading;
      expect(initialLoading).toBe(true);

      await act(async () => {
        await vi.runAllTimersAsync();
      });

      expect(result.current.isLoading).toBe(false);
    });

    it('does not set error on successful fetch', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      const { result } = renderHook(() => useHealthCheck());

      await act(async () => {
        await vi.runAllTimersAsync();
      });

      expect(result.current.error).toBeNull();
    });
  });

  describe('Failed Health Check', () => {
    it('sets error state when API call fails', async () => {
      const mockError = new Error('Network error');
      vi.mocked(api.healthApi.getHealthStatus).mockRejectedValue(mockError);

      const { result } = renderHook(() => useHealthCheck());

      await act(async () => {
        await vi.runAllTimersAsync();
      });

      expect(result.current.error).toBe('Network error');
    });

    it('sets unhealthy status when API call fails', async () => {
      const mockError = new Error('Network error');
      vi.mocked(api.healthApi.getHealthStatus).mockRejectedValue(mockError);

      const { result } = renderHook(() => useHealthCheck());

      await act(async () => {
        await vi.runAllTimersAsync();
      });

      expect(result.current.health).toEqual({
        status: 'unhealthy',
        timestamp: expect.any(String),
        error: 'Network error',
      });
    });

    it('sets loading to false after failed fetch', async () => {
      vi.mocked(api.healthApi.getHealthStatus).mockRejectedValue(
        new Error('API Error')
      );

      const { result } = renderHook(() => useHealthCheck());

      await act(async () => {
        await vi.runAllTimersAsync();
      });

      expect(result.current.isLoading).toBe(false);
    });

    it('handles unknown errors', async () => {
      vi.mocked(api.healthApi.getHealthStatus).mockRejectedValue('Unknown error');

      const { result } = renderHook(() => useHealthCheck());

      await act(async () => {
        await vi.runAllTimersAsync();
      });

      expect(result.current.error).toBe('Unknown error occurred');
    });
  });

  describe('Refetch Functionality', () => {
    it('provides refetch function', () => {
      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue({
        status: 'healthy',
        timestamp: '2024-01-15T10:30:00Z',
      });

      const { result } = renderHook(() => useHealthCheck());

      expect(result.current.refetch).toBeDefined();
      expect(typeof result.current.refetch).toBe('function');
    });

    it('refetches health status when refetch is called', async () => {
      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue({
        status: 'healthy',
        timestamp: '2024-01-15T10:30:00Z',
      });

      const { result } = renderHook(() => useHealthCheck());

      await act(async () => {
        await vi.runAllTimersAsync();
      });

      expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(1);

      await act(async () => {
        result.current.refetch();
        await vi.runAllTimersAsync();
      });

      expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(2);
    });

    it('updates health data after refetch', async () => {
      const initialData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      const updatedData = {
        status: 'degraded' as const,
        timestamp: '2024-01-15T10:31:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus)
        .mockResolvedValueOnce(initialData)
        .mockResolvedValueOnce(updatedData);

      const { result } = renderHook(() => useHealthCheck());

      await act(async () => {
        await vi.runAllTimersAsync();
      });

      expect(result.current.health?.status).toBe('healthy');

      await act(async () => {
        result.current.refetch();
        await vi.runAllTimersAsync();
      });

      expect(result.current.health?.status).toBe('degraded');
    });
  });

  describe('Polling Functionality', () => {
    it('sets up polling when pollInterval is provided', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      renderHook(() => useHealthCheck(5000));

      // Wait for initial fetch (and first poll due to timer behavior)
      await act(async () => {
        await vi.runOnlyPendingTimersAsync();
      });

      // Should be 2: initial fetch + first poll triggered immediately
      const initialCallCount = vi.mocked(api.healthApi.getHealthStatus).mock.calls.length;
      expect(initialCallCount).toBeGreaterThanOrEqual(1);

      // Advance time by poll interval to trigger another poll
      await act(async () => {
        vi.advanceTimersByTimeAsync(5000);
      });

      // Should have at least one more call
      expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(initialCallCount + 1);
    });

    it('continues polling at specified interval', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      renderHook(() => useHealthCheck(3000));

      // Wait for initial fetch
      await act(async () => {
        await vi.runOnlyPendingTimersAsync();
      });

      const callCountAfterFirst = vi.mocked(api.healthApi.getHealthStatus).mock.calls.length;
      expect(callCountAfterFirst).toBeGreaterThanOrEqual(1);

      // Advance time by first interval
      await act(async () => {
        vi.advanceTimersByTimeAsync(3000);
      });

      expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(callCountAfterFirst + 1);

      // Advance time by second interval
      await act(async () => {
        vi.advanceTimersByTimeAsync(3000);
      });

      expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(callCountAfterFirst + 2);
    });

    it('does not poll when pollInterval is 0', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      renderHook(() => useHealthCheck(0));

      // Wait for initial fetch
      await act(async () => {
        await vi.runOnlyPendingTimersAsync();
      });

      const initialCallCount = vi.mocked(api.healthApi.getHealthStatus).mock.calls.length;
      expect(initialCallCount).toBe(1);

      // Advance time significantly
      act(() => {
        vi.advanceTimersByTime(10000);
      });

      // Should still be 1 since no polling is set up
      expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(1);
    });

    it('does not poll when pollInterval is not provided', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      renderHook(() => useHealthCheck());

      // Wait for initial fetch
      await act(async () => {
        await vi.runOnlyPendingTimersAsync();
      });

      const initialCallCount = vi.mocked(api.healthApi.getHealthStatus).mock.calls.length;
      expect(initialCallCount).toBe(1);

      // Advance time significantly
      act(() => {
        vi.advanceTimersByTime(10000);
      });

      // Should still be 1 since no polling is set up
      expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(1);
    });

    it('cleans up polling interval on unmount', async () => {
      const mockHealthData = {
        status: 'healthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
      };

      vi.mocked(api.healthApi.getHealthStatus).mockResolvedValue(mockHealthData);

      const { unmount } = renderHook(() => useHealthCheck(5000));

      // Wait for initial fetch
      await act(async () => {
        await vi.runOnlyPendingTimersAsync();
      });

      const callCountBeforeUnmount = vi.mocked(api.healthApi.getHealthStatus).mock.calls.length;
      expect(callCountBeforeUnmount).toBeGreaterThanOrEqual(1);

      // Unmount the hook
      unmount();

      // Advance time past poll interval
      act(() => {
        vi.advanceTimersByTime(5000);
      });

      // Should be the same count since cleanup removed the interval
      expect(api.healthApi.getHealthStatus).toHaveBeenCalledTimes(callCountBeforeUnmount);
    });
  });

  describe('getHealthStatusProps Utility', () => {
    it('returns correct props for healthy status', () => {
      const props = getHealthStatusProps('healthy');

      expect(props).toEqual({
        color: 'text-green-600',
        bgColor: 'bg-green-100',
        label: 'Healthy',
      });
    });

    it('returns correct props for unhealthy status', () => {
      const props = getHealthStatusProps('unhealthy');

      expect(props).toEqual({
        color: 'text-red-600',
        bgColor: 'bg-red-100',
        label: 'Unhealthy',
      });
    });

    it('returns correct props for degraded status', () => {
      const props = getHealthStatusProps('degraded');

      expect(props).toEqual({
        color: 'text-yellow-600',
        bgColor: 'bg-yellow-100',
        label: 'Degraded',
      });
    });

    it('returns default props for unknown status', () => {
      const props = getHealthStatusProps('unknown' as any);

      expect(props).toEqual({
        color: 'text-gray-600',
        bgColor: 'bg-gray-100',
        label: 'Unknown',
      });
    });
  });

  describe('Timestamp Generation', () => {
    it('generates current timestamp on error', async () => {
      vi.mocked(api.healthApi.getHealthStatus).mockRejectedValue(
        new Error('API Error')
      );

      const { result } = renderHook(() => useHealthCheck());

      await act(async () => {
        await vi.runAllTimersAsync();
      });

      const timestamp = result.current.health?.timestamp;
      expect(timestamp).toBeDefined();

      // Verify it's a valid ISO timestamp
      const date = new Date(timestamp!);
      expect(date.toISOString()).toBe(timestamp);
    });
  });
});
