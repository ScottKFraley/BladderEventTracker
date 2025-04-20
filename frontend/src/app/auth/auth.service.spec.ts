import { TestBed, discardPeriodicTasks, fakeAsync, flush, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterModule } from '@angular/router';
import { AuthService, LoginDto, AuthResponse } from './auth.service';
import { Router } from '@angular/router';
import { Component } from '@angular/core';
import { TOKEN_REFRESH_THRESHOLD } from './auth.config';
import { ApiEndpointsService } from '../services/api-endpoints.service';


@Component({
    template: ''
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
            base: '/api/auth',
            login: '/api/auth/login',
            refresh: '/api/auth/token'
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
                    { path: 'login', component: {} as any },
                    { path: 'dashboard', component: MockDashboardComponent }
                ])
            ],
            declarations: [MockDashboardComponent],
            providers: [
                AuthService,
                { provide: TOKEN_REFRESH_THRESHOLD, useValue: 1000 },
                { provide: ApiEndpointsService, useValue: mockApiEndpoints }
            ]
        });

        service = TestBed.inject(AuthService);
        httpMock = TestBed.inject(HttpTestingController);
        router = TestBed.inject(Router);
        apiEndpoints = TestBed.inject(ApiEndpointsService);

        // Clear localStorage before each test
        localStorage.clear();
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('login', () => {

        const mockResponse: AuthResponse = {
            token: 'mock-jwt-token'
        };

        it('should store token and update auth status on successful login', () => {
            // Arrange
            const navigateSpy = spyOn(router, 'navigate');

            // Act
            service.login(mockCredentials).subscribe({
                next: (response) => {
                    expect(response).toEqual(mockResponse);
                    expect(localStorage.getItem('auth_token')).toBe(mockResponse.token);
                    expect(localStorage.getItem('auth_token_expiry')).toBeTruthy();
                }
            });

            // Assert
            const req = httpMock.expectOne('/api/auth/login');
            expect(req.request.method).toBe('POST');
            expect(req.request.body).toEqual(mockCredentials);

            req.flush(mockResponse);
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
            const req = httpMock.expectOne('/api/auth/login');
            req.flush('Invalid credentials', { status: 401, statusText: 'Unauthorized' });
        });
    });

    describe('logout', () => {
        it('should clear storage and navigate to login', () => {
            // Arrange
            const navigateSpy = spyOn(router, 'navigate');
            localStorage.setItem('auth_token', 'test-token');
            localStorage.setItem('auth_token_expiry', '123456');

            // Act
            service.logout();

            // Assert
            expect(localStorage.getItem('auth_token')).toBeNull();
            expect(localStorage.getItem('auth_token_expiry')).toBeNull();
            expect(navigateSpy).toHaveBeenCalledWith(['/login']);
        });
    });

    describe('token management', () => {
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

    afterEach(() => {
        service.stopRefreshTimer();
        localStorage.clear();
        httpMock.verify();
    });
});
