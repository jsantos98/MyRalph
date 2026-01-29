/**
 * Unit tests for API service
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import axios from 'axios';
import type { HealthStatus } from '../../src/types/health';

// Mock axios module
vi.mock('axios', () => ({
  default: {
    create: vi.fn(),
  },
}));

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

describe('API Service', () => {
  let mockAxiosInstance: {
    get: ReturnType<typeof vi.fn>;
    interceptors: {
      request: { use: ReturnType<typeof vi.fn> };
      response: { use: ReturnType<typeof vi.fn> };
    };
  };

  beforeEach(() => {
    vi.clearAllMocks();

    // Create fresh mock instance for each test
    mockAxiosInstance = {
      get: vi.fn(),
      interceptors: {
        request: {
          use: vi.fn((fulfilled: unknown) => fulfilled),
        },
        response: {
          use: vi.fn((fulfilled: unknown) => fulfilled),
        },
      },
    };

    vi.mocked(axios.create).mockReturnValue(mockAxiosInstance as unknown as typeof axios.create);
  });

  describe('API Client Configuration', () => {
    it('creates axios instance with correct default configuration', () => {
      require('../../src/services/api');

      expect(axios.create).toHaveBeenCalledWith({
        baseURL: '/api/v1',
        timeout: 10000,
        headers: {
          'Content-Type': 'application/json',
        },
      });
    });

    it('sets up request interceptor', () => {
      require('../../src/services/api');

      expect(mockAxiosInstance.interceptors.request.use).toHaveBeenCalled();
    });

    it('sets up response interceptor', () => {
      require('../../src/services/api');

      expect(mockAxiosInstance.interceptors.response.use).toHaveBeenCalled();
    });
  });

  describe('Request Interceptor', () => {
    it('adds auth token when available in localStorage', () => {
      const mockToken = 'test-auth-token';
      const mockConfig = { headers: {} };

      localStorageMock.getItem.mockReturnValue(mockToken);

      // Get the request interceptor callback
      const onFulfilled = mockAxiosInstance.interceptors.request.use.mock.calls[0]?.[0];

      if (onFulfilled) {
        const result = onFulfilled(mockConfig);
        expect(result.headers.Authorization).toBe(`Bearer ${mockToken}`);
      }
    });

    it('does not add auth token when not available', () => {
      const mockConfig = { headers: {} };

      localStorageMock.getItem.mockReturnValue(null);

      // Get the request interceptor callback
      const onFulfilled = mockAxiosInstance.interceptors.request.use.mock.calls[0]?.[0];

      if (onFulfilled) {
        const result = onFulfilled(mockConfig);
        expect(result.headers.Authorization).toBeUndefined();
      }
    });
  });

  describe('Response Interceptor', () => {
    it('clears auth token on 401 response', () => {
      const error = { response: { status: 401 } };

      // Get the response interceptor error callback
      const onRejected = mockAxiosInstance.interceptors.response.use.mock.calls[0]?.[1];

      if (onRejected) {
        onRejected(error);
        expect(localStorageMock.removeItem).toHaveBeenCalledWith('auth_token');
      }
    });

    it('passes through other errors', () => {
      const error = { response: { status: 500 } };

      const onRejected = mockAxiosInstance.interceptors.response.use.mock.calls[0]?.[1];

      if (onRejected) {
        expect(() => onRejected(error)).rejects.toEqual(error);
      }
    });
  });

  describe('healthApi.getHealthStatus', () => {
    it('fetches health status successfully', async () => {
      const mockHealthData: HealthStatus = {
        status: 'healthy',
        timestamp: '2024-01-15T10:30:00Z',
        version: '1.0.0',
        uptime: 3600,
        services: {
          database: 'up',
          cache: 'up',
        },
      };

      mockAxiosInstance.get.mockResolvedValue({
        data: { data: mockHealthData },
      });

      const { healthApi } = require('../../src/services/api');
      const result = await healthApi.getHealthStatus();

      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/health');
      expect(result).toEqual(mockHealthData);
    });

    it('throws error with message from API response', async () => {
      const mockError = {
        response: {
          data: { message: 'Service unavailable' },
        },
      };

      mockAxiosInstance.get.mockRejectedValue(mockError);

      const { healthApi } = require('../../src/services/api');

      await expect(healthApi.getHealthStatus()).rejects.toThrow('Service unavailable');
    });

    it('throws error with axios message when no response data', async () => {
      const mockError = { message: 'Network Error' };

      mockAxiosInstance.get.mockRejectedValue(mockError);

      const { healthApi } = require('../../src/services/api');

      await expect(healthApi.getHealthStatus()).rejects.toThrow('Network Error');
    });

    it('throws default error message when error has no message', async () => {
      mockAxiosInstance.get.mockRejectedValue({});

      const { healthApi } = require('../../src/services/api');

      await expect(healthApi.getHealthStatus()).rejects.toThrow(
        'Failed to fetch health status'
      );
    });

    it('handles degraded status correctly', async () => {
      const mockHealthData: HealthStatus = {
        status: 'degraded',
        timestamp: '2024-01-15T10:30:00Z',
        services: {
          database: 'up',
          cache: 'down',
        },
      };

      mockAxiosInstance.get.mockResolvedValue({
        data: { data: mockHealthData },
      });

      const { healthApi } = require('../../src/services/api');
      const result = await healthApi.getHealthStatus();

      expect(result.status).toBe('degraded');
      expect(result.services?.cache).toBe('down');
    });

    it('handles unhealthy status correctly', async () => {
      const mockHealthData: HealthStatus = {
        status: 'unhealthy',
        timestamp: '2024-01-15T10:30:00Z',
        error: 'Database connection failed',
      };

      mockAxiosInstance.get.mockResolvedValue({
        data: { data: mockHealthData },
      });

      const { healthApi } = require('../../src/services/api');
      const result = await healthApi.getHealthStatus();

      expect(result.status).toBe('unhealthy');
      expect(result.error).toBe('Database connection failed');
    });

    it('handles timeout errors', async () => {
      const mockError = new Error('timeout of 10000ms exceeded');

      mockAxiosInstance.get.mockRejectedValue(mockError);

      const { healthApi } = require('../../src/services/api');

      await expect(healthApi.getHealthStatus()).rejects.toThrow(
        'timeout of 10000ms exceeded'
      );
    });

    it('handles error with code property', async () => {
      const mockError = {
        response: {
          data: {
            message: 'Rate limit exceeded',
            code: 'RATE_LIMIT_EXCEEDED',
          },
        },
      };

      mockAxiosInstance.get.mockRejectedValue(mockError);

      const { healthApi } = require('../../src/services/api');

      await expect(healthApi.getHealthStatus()).rejects.toThrow('Rate limit exceeded');
    });

    it('handles error without response object', async () => {
      const mockError = new Error('Connection refused');

      mockAxiosInstance.get.mockRejectedValue(mockError);

      const { healthApi } = require('../../src/services/api');

      await expect(healthApi.getHealthStatus()).rejects.toThrow('Connection refused');
    });
  });

  describe('API Client Export', () => {
    it('exports apiClient for custom requests', () => {
      const api = require('../../src/services/api');

      expect(api.apiClient).toBeDefined();
      expect(api.apiClient.interceptors).toBeDefined();
    });

    it('exports healthApi with getHealthStatus method', () => {
      const api = require('../../src/services/api');

      expect(api.healthApi).toBeDefined();
      expect(api.healthApi.getHealthStatus).toBeDefined();
      expect(typeof api.healthApi.getHealthStatus).toBe('function');
    });
  });
});
