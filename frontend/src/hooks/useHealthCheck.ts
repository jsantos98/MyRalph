/**
 * Custom hook for health check data fetching
 * Manages health status state and polling
 */

import { useEffect, useState } from 'react';
import { healthApi } from '../services/api';
import type { HealthStatus, HealthStatusType } from '../types/health';

export interface UseHealthCheckResult {
  health: HealthStatus | null;
  isLoading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

/**
 * Custom hook to fetch and manage health check status
 * @param pollInterval - Optional polling interval in milliseconds (default: disabled)
 * @returns Health check state and refetch function
 */
export function useHealthCheck(pollInterval?: number): UseHealthCheckResult {
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchHealth = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const healthData = await healthApi.getHealthStatus();
      setHealth(healthData);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'Unknown error occurred';
      setError(errorMessage);
      // Set unhealthy status on error
      setHealth({
        status: 'unhealthy',
        timestamp: new Date().toISOString(),
        error: errorMessage,
      });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    // Fetch health status on mount
    fetchHealth();

    // Set up polling if interval is provided
    if (pollInterval && pollInterval > 0) {
      const intervalId = setInterval(fetchHealth, pollInterval);
      return () => clearInterval(intervalId);
    }
  }, [pollInterval]);

  return {
    health,
    isLoading,
    error,
    refetch: fetchHealth,
  };
}

/**
 * Utility function to get health status display properties
 */
export function getHealthStatusProps(
  status: HealthStatusType
): {
  color: string;
  bgColor: string;
  label: string;
} {
  switch (status) {
    case 'healthy':
      return {
        color: 'text-green-600',
        bgColor: 'bg-green-100',
        label: 'Healthy',
      };
    case 'unhealthy':
      return {
        color: 'text-red-600',
        bgColor: 'bg-red-100',
        label: 'Unhealthy',
      };
    case 'degraded':
      return {
        color: 'text-yellow-600',
        bgColor: 'bg-yellow-100',
        label: 'Degraded',
      };
    default:
      return {
        color: 'text-gray-600',
        bgColor: 'bg-gray-100',
        label: 'Unknown',
      };
  }
}
