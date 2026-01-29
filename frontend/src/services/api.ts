/**
 * API service for FelizesTracker backend
 * Handles all HTTP communication with the backend API
 */

import axios, { type AxiosError, type AxiosInstance } from 'axios';
import type {
  HealthStatus,
  HealthCheckResponse,
  HealthCheckError,
} from '../types/health';

// API base URL - uses proxy in development
const API_BASE_URL = import.meta.env.VITE_API_URL || '/api/v1';

/**
 * Create and configure axios instance
 */
const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Request interceptor to add auth token (if available)
 */
apiClient.interceptors.request.use(
  (config) => {
    // Add auth token if stored securely
    const token = localStorage.getItem('auth_token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

/**
 * Response interceptor for error handling
 */
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    // Handle common errors
    if (error.response?.status === 401) {
      // Unauthorized - clear token
      localStorage.removeItem('auth_token');
    }
    return Promise.reject(error);
  }
);

/**
 * Health check API methods
 */
export const healthApi = {
  /**
   * Get system health status
   * @returns Promise with health status data
   */
  async getHealthStatus(): Promise<HealthStatus> {
    try {
      const response = await apiClient.get<HealthCheckResponse>('/health');
      return response.data.data;
    } catch (error) {
      const axiosError = error as AxiosError<HealthCheckError>;
      throw new Error(
        axiosError.response?.data?.message ||
          axiosError.message ||
          'Failed to fetch health status'
      );
    }
  },
};

/**
 * Default export of the API client
 * Use this for making custom requests
 */
export default apiClient;
