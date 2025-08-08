export class MockApplicationInsightsService {
    trackEvent(name: string, properties?: any, measurements?: any): void { }
    trackPageView(name?: string, uri?: string): void { }
    trackException(exception: Error, properties?: any): void { }
    trackMetric(name: string, average: number, properties?: any): void { }
    trackDependencyData(dependency: any): void { }
    trackLogin(username: string, success: boolean, duration?: number, errorMessage?: string): void { }
    trackLogout(username: string): void { }
    trackTokenRefresh(success: boolean, duration?: number, errorMessage?: string): void { }
    trackNavigation(from: string, to: string, duration?: number): void { }
    trackAPICall(endpoint: string, method: string, duration: number, success: boolean, statusCode?: number, errorMessage?: string): void { }
    trackFormSubmission(formName: string, success: boolean, validationErrors?: string[]): void { }
    trackPerformanceMetric(name: string, value: number, properties?: any): void { }
    setAuthenticatedUser(userId: string, accountId?: string): void { }
    clearAuthenticatedUser(): void { }
    addTelemetryInitializer(telemetryInitializer: (envelope: any) => boolean | void): void { }
    flush(): void { }
}
