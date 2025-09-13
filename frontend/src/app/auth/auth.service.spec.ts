import { TestBed, discardPeriodicTasks, fakeAsync, flush, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AuthService, LoginDto, AuthResponse } from './auth.service';
import { Router } from '@angular/router';
import { Component } from '@angular/core';
import { TOKEN_REFRESH_THRESHOLD } from './auth.config';
import { ApiEndpointsService } from '../services/api-endpoints.service';
import { ApplicationInsightsService } from '../services/application-insights.service';
import { MockApplicationInsightsService } from '../services/application-insights.service.mock';

@Component({
    selector: 'app-mock-dashboard',
    template: '<div>Mock Dashboard</div>'
})
class MockDashboardComponent { }

describe('AuthService', () => {
    let service: AuthService;
    let httpMock: HttpTestingController;
    let router: Router;
    let apiEndpoints: ApiEndpointsService;

    // Mock API endpoints
    const mockApiEndpoints = {
        getAuthEndpoints: () => ({
            base: '/api/v1/auth',
            login: '/api/v1/auth/login',
            token: '/api/v1/auth/token',
            refresh: '/api/v1/auth/refresh',
            revoke: '/api/v1/auth/revoke',
            revokeAll: '/api/v1/auth/revoke-all'
        })
    };

    const mockCredentials: LoginDto = {
        username: 'testuser',
        password: 'testpass'
    };

    beforeEach(() => {
        jasmine.DEFAULT_TIMEOUT_INTERVAL = 20000;
        TestBed.configureTestingModule({
            imports: [
                HttpClientTestingModule,
                RouterModule.forRoot([
                    { path: 'login', component: MockDashboardComponent },
                    { path: 'dashboard', component: MockDashboardComponent },
                    { path: '', redirectTo: '/login', pathMatch: 'full' }
                ], { useHash: true })
            ],
            declarations: [MockDashboardComponent],
            providers: [
                AuthService,
                { provide: TOKEN_REFRESH_THRESHOLD, useValue: 1000 },
                { provide: ApiEndpointsService, useValue: mockApiEndpoints },
                { provide: ApplicationInsightsService, useClass: MockApplicationInsightsService }
            ]
        });

        service = TestBed.inject(AuthService);
        httpMock = TestBed.inject(HttpTestingController);
        router = TestBed.inject(Router);
        apiEndpoints = TestBed.inject(ApiEndpointsService);

        // Clear localStorage before each test (still needed for some tests)
        localStorage.clear();
    });

    afterEach(() => {
        // Handle any pending refresh token requests from service initialization
        const pendingRequests = httpMock.match(() => true);
        pendingRequests.forEach(req => {
            if (req.request.url.includes('/refresh') && !req.cancelled) {
                req.flush('', { status: 401, statusText: 'Unauthorized' });
            }
        });
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('login', () => {

        const mockResponse: AuthResponse = {
            token: 'mock-jwt-token'
        };

        it('should extract token expiry from JWT and update auth status on successful login', () => {
            // Arrange
            const navigateSpy = spyOn(router, 'navigate');
            // Create a mock JWT token with 30-day expiry
            const mockJwtPayload = {
                exp: Math.floor(Date.now() / 1000) + (30 * 24 * 60 * 60), // 30 days from now
                'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': 'test-user-id'
            };
            const mockJwtToken = 'header.' + btoa(JSON.stringify(mockJwtPayload)) + '.signature';
            const mockResponseWithJwt = { token: mockJwtToken };

            // Act
            service.login(mockCredentials).subscribe({
                next: (response) => {
                    expect(response).toEqual(mockResponseWithJwt);
                    // With cookie-based auth, tokens are not stored in localStorage
                    expect(localStorage.getItem('auth_token')).toBeNull();
                    expect(localStorage.getItem('auth_token_expiry')).toBeNull();
                }
            });

            // Assert
            const req = httpMock.expectOne('/api/v1/auth/login');
            expect(req.request.method).toBe('POST');
            expect(req.request.body).toEqual(mockCredentials);
            expect(req.request.withCredentials).toBe(true); // Should include cookies

            req.flush(mockResponseWithJwt);
        });

        it('should handle login error correctly', () => {
            // Act
            service.login(mockCredentials).subscribe({
                error: (error) => {
                    expect(error).toBe('Invalid credentials');
                    expect(localStorage.getItem('auth_token')).toBeNull();
                }
            });

            // Assert
            const req = httpMock.expectOne('/api/v1/auth/login');
            expect(req.request.withCredentials).toBe(true); // Should include cookies
            req.flush('Invalid credentials', { status: 401, statusText: 'Unauthorized' });
        });
    });

    describe('logout', () => {
        it('should clear auth state and navigate to login', () => {
            // Arrange
            const navigateSpy = spyOn(router, 'navigate');
            
            // Act
            service.logout();

            // Assert
            // With cookie-based auth, no localStorage clearing is needed
            // Cookies are cleared by backend endpoints
            expect(navigateSpy).toHaveBeenCalledWith(['/login']);
        });
    });

    xdescribe('token management', () => {
        it('should setup token expiry timer on successful auth', fakeAsync(() => {
            // Arrange
            const mockResponse: AuthResponse = { token: 'new-token' };

            // Act - Login
            service.login(mockCredentials).subscribe();
            const loginReq = httpMock.expectOne(apiEndpoints.getAuthEndpoints().login);
            loginReq.flush(mockResponse);

            // Get the expiry time that was set
            const expiry = parseInt(localStorage.getItem('auth_token_expiry') || '0', 10);
            const timeUntilRefresh = expiry - Date.now() - 1000;

            // Fast-forward to just before refresh
            tick(timeUntilRefresh);

            // Handle the refresh token request
            const refreshReq = httpMock.expectOne(apiEndpoints.getAuthEndpoints().refresh);
            expect(refreshReq.request.method).toBe('POST');
            refreshReq.flush({ token: 'refreshed-token' });

            tick();

            expect(localStorage.getItem('auth_token')).toBe('refreshed-token');

            service.stopRefreshTimer();
            flush();
            discardPeriodicTasks();
        }));
    });

    describe('new token management methods', () => {
        xit('should call refresh endpoint and handle successful response', () => {
            const mockResponse: AuthResponse = { token: 'new-refresh-token' };

            service.refreshToken().subscribe({
                next: (response) => {
                    expect(response).toEqual(mockResponse);
                    expect(localStorage.getItem('auth_token')).toBe(mockResponse.token);
                }
            });

            const req = httpMock.expectOne('/api/v1/auth/refresh');
            expect(req.request.method).toBe('POST');
            expect(req.request.withCredentials).toBe(true);
            req.flush(mockResponse);
        });

        xit('should handle refresh token error', () => {
            service.refreshToken().subscribe({
                error: (error) => {
                    expect(error).toBe('Invalid credentials');
                }
            });

            const req = httpMock.expectOne('/api/v1/auth/refresh');
            req.flush('Invalid credentials', { status: 401, statusText: 'Unauthorized' });
        });

        xit('should call revoke endpoint and logout on success', () => {
            const navigateSpy = spyOn(router, 'navigate');
            localStorage.setItem('auth_token', 'test-token');

            service.revokeToken().subscribe({
                next: (response) => {
                    expect(localStorage.getItem('auth_token')).toBeNull();
                    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
                }
            });

            const req = httpMock.expectOne('/api/v1/auth/revoke');
            expect(req.request.method).toBe('POST');
            expect(req.request.withCredentials).toBe(true);
            req.flush({});
        });

        xit('should call revoke-all endpoint and logout on success', () => {
            const navigateSpy = spyOn(router, 'navigate');
            localStorage.setItem('auth_token', 'test-token');

            service.revokeAllTokens().subscribe({
                next: (response) => {
                    expect(localStorage.getItem('auth_token')).toBeNull();
                    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
                }
            });

            const req = httpMock.expectOne('/api/v1/auth/revoke-all');
            expect(req.request.method).toBe('POST');
            expect(req.request.withCredentials).toBe(true);
            req.flush({});
        });

        xit('should handle revoke token error', () => {
            service.revokeToken().subscribe({
                error: (error) => {
                    expect(error).toBe('Server error, please try again later');
                }
            });

            const req = httpMock.expectOne('/api/v1/auth/revoke');
            req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
        });
    });

    xdescribe('startup authentication checks', () => {
        it('should attempt refresh token login when no valid token exists', () => {
            localStorage.clear();

            // Create a new service instance to trigger checkAuthStatus
            const newService = new (service.constructor as any)(
                TestBed.inject(HttpClient),
                router,
                apiEndpoints,
                1000
            );

            // Should attempt refresh token call
            const req = httpMock.expectOne('/api/v1/auth/refresh');
            expect(req.request.method).toBe('POST');
            expect(req.request.withCredentials).toBe(true);

            // Simulate failed refresh (no valid refresh token)
            req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

            newService.stopRefreshTimer();
        });

        it('should successfully authenticate on startup with valid refresh token', () => {
            localStorage.clear();
            const mockResponse: AuthResponse = { token: 'startup-token' };

            // Create a new service instance to trigger checkAuthStatus
            const newService = new (service.constructor as any)(
                TestBed.inject(HttpClient),
                router,
                apiEndpoints,
                1000
            );

            // Should attempt refresh token call
            const req = httpMock.expectOne('/api/v1/auth/refresh');
            req.flush(mockResponse);

            expect(localStorage.getItem('auth_token')).toBe('startup-token');

            newService.stopRefreshTimer();
        });
    });

    xdescribe('login with credentials', () => {
        xit('should include withCredentials in login request', () => {
            const mockResponse: AuthResponse = { token: 'login-token' };

            service.login(mockCredentials).subscribe();

            const req = httpMock.expectOne('/api/v1/auth/login');
            expect(req.request.withCredentials).toBe(true);
            req.flush(mockResponse);
        });
    });

    afterEach(() => {
        if (service && typeof service.stopRefreshTimer === 'function') {
            service.stopRefreshTimer();
        }
        localStorage.clear();
        // Handle any remaining requests before verify
        const pendingRequests = httpMock.match(() => true);
        pendingRequests.forEach(req => {
            if (req.request.url.includes('/refresh') && !req.cancelled) {
                req.flush('', { status: 401, statusText: 'Unauthorized' });
            }
        });
        httpMock.verify();
    });
});
