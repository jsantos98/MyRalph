/**
 * Landing Page Component for FelizesTracker
 * Displays application name and system health status
 * Uses compound component pattern for flexibility
 */

import { useHealthCheck, getHealthStatusProps } from '../hooks/useHealthCheck';
import type { HealthStatus } from '../types/health';
import './LandingPage.css';

export interface LandingPageProps {
  /**
   * Polling interval for health checks in milliseconds
   * Set to 0 to disable polling
   */
  pollInterval?: number;
}

/**
 * Main Landing Page Component
 */
export function LandingPage({ pollInterval = 0 }: LandingPageProps) {
  const { health, isLoading, error, refetch } = useHealthCheck(pollInterval);

  return (
    <div className="landing-page">
      <LandingPage.Header />
      <LandingPage.Main>
        <LandingPage.HealthIndicator
          health={health}
          isLoading={isLoading}
          error={error}
          onRefresh={refetch}
        />
      </LandingPage.Main>
    </div>
  );
}

/**
 * Header Component - Displays FelizesTracker branding
 */
LandingPage.Header = function Header() {
  return (
    <header className="landing-header">
      <div className="landing-header__container">
        <h1 className="landing-header__title">FelizesTracker</h1>
        <p className="landing-header__subtitle">
          Track your feliz journey with confidence
        </p>
      </div>
    </header>
  );
};

/**
 * Main Content Area
 */
LandingPage.Main = function Main({ children }: { children: React.ReactNode }) {
  return <main className="landing-main">{children}</main>;
};

/**
 * Health Indicator Component - Shows system health status
 */
LandingPage.HealthIndicator = function HealthIndicator({
  health,
  isLoading,
  error,
  onRefresh,
}: {
  health: HealthStatus | null;
  isLoading: boolean;
  error: string | null;
  onRefresh: () => Promise<void>;
}) {
  if (isLoading) {
    return (
      <div className="health-indicator health-indicator--loading">
        <div className="health-indicator__spinner" />
        <span className="health-indicator__label">Checking system status...</span>
      </div>
    );
  }

  if (error || !health) {
    return (
      <div className="health-indicator health-indicator--error">
        <div className="health-indicator__icon health-indicator__icon--error" />
        <div className="health-indicator__content">
          <span className="health-indicator__label">Unable to check status</span>
          <span className="health-indicator__error">{error || 'Unknown error'}</span>
        </div>
        <button
          className="health-indicator__refresh"
          onClick={onRefresh}
          aria-label="Retry health check"
        >
          Retry
        </button>
      </div>
    );
  }

  const statusProps = getHealthStatusProps(health.status);

  return (
    <div
      className={`health-indicator ${statusProps.bgColor}`}
      role="status"
      aria-live="polite"
    >
      <div
        className={`health-indicator__icon health-indicator__icon--${health.status}`}
      />
      <div className="health-indicator__content">
        <span className={`health-indicator__label ${statusProps.color}`}>
          {statusProps.label}
        </span>
        {health.version && (
          <span className="health-indicator__version">
            Version: {health.version}
          </span>
        )}
        {health.uptime && (
          <span className="health-indicator__uptime">
            Uptime: {Math.floor(health.uptime / 60)} minutes
          </span>
        )}
        {health.services && (
          <div className="health-indicator__services">
            <span
              className={`health-indicator__service ${
                health.services.database === 'up' ? 'text-green-600' : 'text-red-600'
              }`}
            >
              Database: {health.services.database}
            </span>
            {health.services.cache && (
              <span
                className={`health-indicator__service ${
                  health.services.cache === 'up' ? 'text-green-600' : 'text-red-600'
                }`}
              >
                Cache: {health.services.cache}
              </span>
            )}
          </div>
        )}
      </div>
      <button
        className="health-indicator__refresh"
        onClick={onRefresh}
        aria-label="Refresh health status"
      >
        ↻
      </button>
    </div>
  );
};
