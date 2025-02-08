import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AuthService, LoginDto, AuthResponse } from './auth.service';
import { Router } from '@angular/router';
import { Component } from '@angular/core';

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
        TestBed.configureTestingModule({
            imports: [
                HttpClientTestingModule,
                RouterTestingModule.withRoutes([
                    { path: 'login', component: {} as any },
                    { path: 'dashboard', component: MockDashboardComponent }  // Use the mock component
                ])
            ],
            declarations: [MockDashboardComponent],  // Declare the mock component
            providers: [AuthService]
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

        it('should setup token expiry timer on successful auth', (done) => {
            // Arrange
            const mockResponse: AuthResponse = { token: 'new-token' };
            jasmine.clock().install();  // Add this

            // Act
            service.login(mockCredentials).subscribe(() => {
                // Fast-forward time to just before token refresh
                jasmine.clock().tick(100);  // Replace setTimeout with this
                const refreshReq = httpMock.expectOne('/api/auth/token');
                expect(refreshReq.request.method).toBe('POST');
                refreshReq.flush({ token: 'refreshed-token' });
                jasmine.clock().uninstall();  // Add this
                done();
            });

            // Handle initial login request
            const loginReq = httpMock.expectOne('/api/auth/login');
            loginReq.flush(mockResponse);
        });
    });
});
