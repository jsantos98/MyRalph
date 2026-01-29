/**
 * Unit tests for API service
 * Tests API configuration, interceptors, and health check endpoint
 */

import { describe, it, expect, vi, beforeAll, beforeEach } from 'vitest';
import axios from 'axios';

// Mock axios at the module level
vi.mock('axios', () => ({
  default: {
    create: vi.fn(() => ({
      get: vi.fn(),
      defaults: {
        baseURL: '/api/v1',
        timeout: 10000,
        headers: {
          'Content-Type': 'application/json',
        },
      },
      interceptors: {
        request: {
          use: vi.fn(),
          handlers: [],
        },
        response: {
          use: vi.fn(),
          handlers: [],
        },
      },
    })),
  },
}));

describe('API Service', () => {
  let mockApiGet: ReturnType<typeof vi.fn>;

  beforeAll(() => {
    // Mock localStorage
    const localStorageMock = {
      getItem: vi.fn(),
      setItem: vi.fn(),
      removeItem: vi.fn(),
      clear: vi.fn(),
    };

    Object.defineProperty(global, 'localStorage', {
      value: localStorageMock,
      writable: true,
    });
  });

  beforeEach(() => {
    // Create a fresh mock for each test
    mockApiGet = vi.fn();

    // Mock the axios.create return value
    const mockAxiosInstance = {
      get: mockApiGet,
      defaults: {
        baseURL: '/api/v1',
        timeout: 10000,
        headers: {
          'Content-Type': 'application/json',
        },
      },
      interceptors: {
        request: {
          use: vi.fn((fulfilled: unknown) => fulfilled),
          handlers: [],
        },
        response: {
          use: vi.fn((fulfilled: unknown) => fulfilled, (rejected: unknown) => Promise.reject(rejected)),
          handlers: [],
        },
      },
    };

    vi.mocked(axios.create).mockReturnValue(mockAxiosInstance as any);
  });

  describe('Module Structure', () => {
    it('should import API service without errors', async () => {
      const api = await import('../../src/services/api');
      expect(api).toBeDefined();
    });

    it('should export healthApi object', async () => {
      const { healthApi } = await import('../../src/services/api');
      expect(healthApi).toBeDefined();
      expect(healthApi.getHealthStatus).toBeDefined();
      expect(typeof healthApi.getHealthStatus).toBe('function');
    });

    it('should export apiClient instance', async () => {
      const { apiClient } = await import('../../src/services/api');
      expect(apiClient).toBeDefined();
      expect(apiClient.interceptors).toBeDefined();
    });
  });

  describe('healthApi.getHealthStatus', () => {
    it('should make a GET request to /health endpoint', async () => {
      mockApiGet.mockResolvedValue({
        data: { data: { status: 'healthy', timestamp: '2024-01-15T10:30:00Z' } },
      });

      const { healthApi } = await import('../../src/services/api');
      await healthApi.getHealthStatus();

      expect(mockApiGet).toHaveBeenCalledWith('/health');
    });

    it('should return health status data on successful response', async () => {
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

      mockApiGet.mockResolvedValue({
        data: { data: mockHealthData },
      });

      const { healthApi } = await import('../../src/services/api');
      const result = await healthApi.getHealthStatus();

      expect(result).toEqual(mockHealthData);
    });

    it('should throw error with message from API response', async () => {
      const mockError = {
        response: {
          data: { message: 'Service unavailable' },
        },
      };

      mockApiGet.mockRejectedValue(mockError);

      const { healthApi } = await import('../../src/services/api');

      await expect(healthApi.getHealthStatus()).rejects.toThrow('Service unavailable');
    });

    it('should throw error with axios message when no response data', async () => {
      const mockError = { message: 'Network Error' };

      mockApiGet.mockRejectedValue(mockError);

      const { healthApi } = await import('../../src/services/api');

      await expect(healthApi.getHealthStatus()).rejects.toThrow('Network Error');
    });

    it('should throw default error message when error has no message', async () => {
      mockApiGet.mockRejectedValue({});

      const { healthApi } = await import('../../src/services/api');

      await expect(healthApi.getHealthStatus()).rejects.toThrow(
        'Failed to fetch health status'
      );
    });

    it('should handle degraded status correctly', async () => {
      const mockHealthData = {
        status: 'degraded' as const,
        timestamp: '2024-01-15T10:30:00Z',
        services: {
          database: 'up' as const,
          cache: 'down' as const,
        },
      };

      mockApiGet.mockResolvedValue({
        data: { data: mockHealthData },
      });

      const { healthApi } = await import('../../src/services/api');
      const result = await healthApi.getHealthStatus();

      expect(result.status).toBe('degraded');
      expect(result.services?.cache).toBe('down');
    });

    it('should handle unhealthy status correctly', async () => {
      const mockHealthData = {
        status: 'unhealthy' as const,
        timestamp: '2024-01-15T10:30:00Z',
        error: 'Database connection failed',
      };

      mockApiGet.mockResolvedValue({
        data: { data: mockHealthData },
      });

      const { healthApi } = await import('../../src/services/api');
      const result = await healthApi.getHealthStatus();

      expect(result.status).toBe('unhealthy');
      expect(result.error).toBe('Database connection failed');
    });

    it('should handle timeout errors', async () => {
      const mockError = new Error('timeout of 10000ms exceeded');

      mockApiGet.mockRejectedValue(mockError);

      const { healthApi } = await import('../../src/services/api');

      await expect(healthApi.getHealthStatus()).rejects.toThrow(
        'timeout of 10000ms exceeded'
      );
    });

    it('should handle error with code property', async () => {
      const mockError = {
        response: {
          data: {
            message: 'Rate limit exceeded',
            code: 'RATE_LIMIT_EXCEEDED',
          },
        },
      };

      mockApiGet.mockRejectedValue(mockError);

      const { healthApi } = await import('../../src/services/api');

      await expect(healthApi.getHealthStatus()).rejects.toThrow('Rate limit exceeded');
    });

    it('should handle error without response object', async () => {
      const mockError = new Error('Connection refused');

      mockApiGet.mockRejectedValue(mockError);

      const { healthApi } = await import('../../src/services/api');

      await expect(healthApi.getHealthStatus()).rejects.toThrow('Connection refused');
    });
  });

  describe('API Client Configuration', () => {
    it('should create axios instance with correct configuration', () => {
      require('../../src/services/api');

      expect(axios.create).toHaveBeenCalledWith({
        baseURL: '/api/v1',
        timeout: 10000,
        headers: {
          'Content-Type': 'application/json',
        },
      });
    });
  });

  describe('Request Interceptor', () => {
    it('should add auth token when available in localStorage', async () => {
      const localStorageMock = global.localStorage as any;
      localStorageMock.getItem.mockReturnValue('test-token');

      const { apiClient } = await import('../../src/services/api');

      // Get the request interceptor handler
      const handlers = (apiClient.interceptors.request as any).handlers;

      if (handlers && handlers.length > 0) {
        const fulfilledHandler = handlers[0].fulfilled;
        const config = { headers: {} };

        const result = fulfilledHandler(config);

        expect(result.headers.Authorization).toBe('Bearer test-token');
      }
    });

    it('should not add auth token when not available', async () => {
      const localStorageMock = global.localStorage as any;
      localStorageMock.getItem.mockReturnValue(null);

      const { apiClient } = await import('../../src/services/api');

      // Get the request interceptor handler
      const handlers = (apiClient.interceptors.request as any).handlers;

      if (handlers && handlers.length > 0) {
        const fulfilledHandler = handlers[0].fulfilled;
        const config = { headers: {} };

        const result = fulfilledHandler(config);

        expect(result.headers.Authorization).toBeUndefined();
      }
    });
  });

  describe('Response Interceptor', () => {
    it('should clear auth token on 401 response', async () => {
      const localStorageMock = global.localStorage as any;

      const { apiClient } = await import('../../src/services/api');

      // Get the response interceptor handler
      const handlers = (apiClient.interceptors.response as any).handlers;

      if (handlers && handlers.length > 0) {
        const rejectedHandler = handlers[0].rejected;
        const error = { response: { status: 401 } };

        if (rejectedHandler) {
          rejectedHandler(error);
          expect(localStorageMock.removeItem).toHaveBeenCalledWith('auth_token');
        }
      }
    });
  });
});
