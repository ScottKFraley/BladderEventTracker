export interface ApiError {
  statusCode: number;
  message: string;
  details?: string;
  timestamp: string;
  correlationId: string;
  endpoint: string;
  errorCode?: string;
  userFriendlyMessage?: string;
  retryAfter?: number;
  supportReference?: string;
}

export interface AuthenticationError extends ApiError {
  authenticationState: 'expired' | 'invalid' | 'missing' | 'server_error' | 'timeout';
  tokenExpiration?: string;
  refreshTokenAvailable: boolean;
  suggestedAction: 'retry' | 'relogin' | 'contact_support' | 'wait';
}

export interface ErrorContext {
  userAgent: string;
  url: string;
  method: string;
  requestHeaders: Record<string, string>;
  responseHeaders?: Record<string, string>;
  networkConnection: string;
  timestamp: string;
  userId?: string;
  sessionId?: string;
}

export interface ErrorHistory {
  errors: (ApiError | AuthenticationError)[];
  lastOccurrence: string;
  totalCount: number;
  errorsByType: Record<string, number>;
}

export interface DebugInfo {
  user: {
    username: string | null;
    isAuthenticated: boolean;
    tokenExpiration: string | null;
    userId: string | null;
    roles: string[];
  };
  authentication: {
    hasAccessToken: boolean;
    hasRefreshToken: boolean;
    lastAuthAttempt: string | null;
    authErrors: AuthenticationError[];
    tokenRefreshCount: number;
    lastSuccessfulAuth: string | null;
  };
  logs: {
    entries: LogEntry[];
    lastFetched: string;
    totalEntries: number;
  };
  system: {
    userAgent: string;
    connectionType: string;
    timestamp: string;
    localStorage: Record<string, any>;
    sessionStorage: Record<string, any>;
    cookieInfo: CookieDebugInfo;
    networkStatus: 'online' | 'offline' | 'unstable';
    performance: PerformanceMetrics;
  };
  errorHistory: ErrorHistory;
}

export interface LogEntry {
  timestamp: string;
  level: 'debug' | 'info' | 'warn' | 'error';
  source: 'frontend' | 'backend' | 'nginx' | 'azure';
  message: string;
  details?: any;
  correlationId?: string;
  userId?: string;
}

export interface CookieDebugInfo {
  authCookiePresent: boolean;
  authCookieExpiry?: string;
  refreshCookiePresent: boolean;
  refreshCookieExpiry?: string;
  sameSite: string;
  secure: boolean;
  httpOnly: boolean;
}

export interface PerformanceMetrics {
  pageLoadTime: number;
  firstContentfulPaint: number;
  largestContentfulPaint: number;
  apiResponseTimes: Record<string, number>;
  memoryUsage?: number;
  connectionSpeed?: 'slow-2g' | '2g' | '3g' | '4g' | 'unknown';
}

export enum ErrorSeverity {
  LOW = 'low',
  MEDIUM = 'medium',
  HIGH = 'high',
  CRITICAL = 'critical'
}

export interface ErrorPatternAlert {
  pattern: string;
  occurrences: number;
  timeWindow: string;
  severity: ErrorSeverity;
  suggestedAction: string;
}