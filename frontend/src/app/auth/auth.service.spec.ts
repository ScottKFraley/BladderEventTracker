import { TestBed, discardPeriodicTasks, fakeAsync, flush, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterModule } from '@angular/router';
import { AuthService, LoginDto, AuthResponse } from './auth.service';
import { Router } from '@angular/router';
import { Component } from '@angular/core';
import { TOKEN_REFRESH_THRESHOLD } from './auth.config';


@Component({
    template: ''
})
class MockDashboardComponent { }

describe('AuthService', () => {
    let service: AuthService;
    let httpMock: HttpTestingController;
    let router: Router;

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
                { provide: TOKEN_REFRESH_THRESHOLD, useValue: 1000 } // Add this line
            ]
        });

        service = TestBed.inject(AuthService);
        httpMock = TestBed.inject(HttpTestingController);
        router = TestBed.inject(Router);

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
        const mockCredentials: LoginDto = {
            username: 'testuser',
            password: 'testpass'
        };
    
        it('should setup token expiry timer on successful auth', fakeAsync(() => {
            // Arrange
            const mockResponse: AuthResponse = { token: 'new-token' };
            
            // Act - Login
            service.login(mockCredentials).subscribe();
            const loginReq = httpMock.expectOne(`${service['AUTH_API']}/login`);
            loginReq.flush(mockResponse);
    
            // Get the expiry time that was set
            const expiry = parseInt(localStorage.getItem(service['TOKEN_EXPIRY_KEY']) || '0', 10);
            const timeUntilRefresh = expiry - Date.now() - 1000; // Using the 1000ms threshold
    
            // Fast-forward to just before refresh
            tick(timeUntilRefresh);
    
            // Handle the refresh token request
            const refreshReq = httpMock.expectOne(`${service['AUTH_API']}/token`);
            expect(refreshReq.request.method).toBe('POST');
            refreshReq.flush({ token: 'refreshed-token' });
    
            // Allow any pending operations to complete
            tick();
    
            // Assertions
            expect(localStorage.getItem(service['TOKEN_KEY'])).toBe('refreshed-token');
    
            // Clean up
            service.stopRefreshTimer();
            flush();
            discardPeriodicTasks();
        }));
    
        afterEach(() => {
            service.stopRefreshTimer();
            localStorage.clear();
            httpMock.verify();
        });
    });
    
    // describe('token management', () => {
    //     const mockCredentials: LoginDto = {
    //         username: 'testuser',
    //         password: 'testpass'
    //     };

    //     it('should setup token expiry timer on successful auth', fakeAsync(() => {
    //         // Arrange
    //         const mockResponse: AuthResponse = { token: 'new-token' };

    //         // Act
    //         service.login(mockCredentials).subscribe();

    //         // Handle initial login request
    //         const loginReq = httpMock.expectOne('/api/auth/login');
    //         loginReq.flush(mockResponse);

    //         // Get the expiry time that was set
    //         const expiryTime = parseInt(localStorage.getItem('auth_token_expiry') || '0', 10);
    //         const now = new Date().getTime();
    //         const timeUntilRefresh = (expiryTime - now) - service['TOKEN_REFRESH_THRESHOLD'];

    //         // Fast-forward time to when the refresh should happen
    //         tick(timeUntilRefresh);

    //         // Handle the refresh request
    //         const refreshReq = httpMock.expectOne('/api/auth/token');
    //         expect(refreshReq.request.method).toBe('POST');
    //         refreshReq.flush({ token: 'refreshed-token' });

    //         // Complete all pending asynchronous operations
    //         tick();

    //         // Verify the token was updated
    //         expect(localStorage.getItem('auth_token')).toBe('refreshed-token');

    //         // Clean up any remaining timers and complete the test
    //         service.stopRefreshTimer();
    //         discardPeriodicTasks();
    //         flush();
    //     }));

    //     afterEach(() => {
    //         // Clean up timers after each test
    //         service.stopRefreshTimer();
    //     });
    // });

});
