/**
 * Health status types for the FelizesTracker application
 */

export type HealthStatusType = 'healthy' | 'unhealthy' | 'degraded';

export interface HealthStatus {
  status: HealthStatusType;
  timestamp: string;
  version?: string;
  uptime?: number;
  services?: {
    database: 'up' | 'down';
    cache?: 'up' | 'down';
  };
  error?: string;
}

export interface HealthCheckResponse {
  data: HealthStatus;
}

export interface HealthCheckError {
  message: string;
  code?: string;
}
