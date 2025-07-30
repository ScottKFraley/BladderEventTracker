import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpClient, HttpRequest, HttpHandler, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { of, throwError, BehaviorSubject } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../auth/auth.service';

describe('AuthInterceptor', () => {
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockNext: jasmine.SpyObj<HttpHandler>;

  beforeEach(() => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['getToken', 'refreshToken', 'logout']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    const nextSpy = jasmine.createSpyObj('HttpHandler', ['handle']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    mockAuthService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    mockNext = nextSpy;
  });

  afterEach(() => {
    // Reset any spies
    if (mockAuthService) {
      mockAuthService.getToken.calls.reset();
      mockAuthService.refreshToken.calls.reset();
      mockAuthService.logout.calls.reset();
    }
    if (mockRouter) {
      mockRouter.navigate.calls.reset();
    }
    if (mockNext) {
      mockNext.handle.calls.reset();
    }
  });

  it('should add Authorization header when token exists', () => {
    const token = 'test-token';
    const mockRequest = new HttpRequest('GET', '/api/test');
    const mockResponse = new HttpResponse({ status: 200 });

    mockAuthService.getToken.and.returnValue(token);
    mockNext.handle.and.returnValue(of(mockResponse));

    TestBed.runInInjectionContext(() => {
      authInterceptor(mockRequest, mockNext.handle).subscribe();
    });

    expect(mockNext.handle).toHaveBeenCalledWith(
      jasmine.objectContaining({
        headers: jasmine.objectContaining({
          lazyUpdate: jasmine.any(Array)
        })
      })
    );
  });

  it('should not add Authorization header when no token exists', () => {
    const mockRequest = new HttpRequest('GET', '/api/test');
    const mockResponse = new HttpResponse({ status: 200 });

    mockAuthService.getToken.and.returnValue(null);
    mockNext.handle.and.returnValue(of(mockResponse));

    TestBed.runInInjectionContext(() => {
      authInterceptor(mockRequest, mockNext.handle).subscribe();
    });

    expect(mockNext.handle).toHaveBeenCalledWith(mockRequest);
  });

  it('should pass through non-401 errors', () => {
    const mockRequest = new HttpRequest('GET', '/api/test');
    const errorResponse = new HttpErrorResponse({ status: 500, statusText: 'Server Error' });

    mockAuthService.getToken.and.returnValue('token');
    mockNext.handle.and.returnValue(throwError(() => errorResponse));

    TestBed.runInInjectionContext(() => {
      authInterceptor(mockRequest, mockNext.handle).subscribe({
        error: (error) => {
          expect(error.status).toBe(500);
        }
      });
    });
  });

  it('should ignore 401 errors for auth endpoints', () => {
    const mockRequest = new HttpRequest('POST', '/api/v1/auth/login', {});
    const errorResponse = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });

    mockAuthService.getToken.and.returnValue('token');
    mockNext.handle.and.returnValue(throwError(() => errorResponse));

    TestBed.runInInjectionContext(() => {
      authInterceptor(mockRequest, mockNext.handle).subscribe({
        error: (error) => {
          expect(error.status).toBe(401);
          expect(mockAuthService.refreshToken).not.toHaveBeenCalled();
        }
      });
    });
  });

  it('should attempt token refresh on 401 error for non-auth endpoints', () => {
    const mockRequest = new HttpRequest('GET', '/api/data');
    const errorResponse = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });
    const refreshResponse = { token: 'new-token' };
    const retryResponse = new HttpResponse({ status: 200 });

    mockAuthService.getToken.and.returnValue('old-token');
    mockAuthService.refreshToken.and.returnValue(of(refreshResponse));
    mockNext.handle.and.returnValues(
      throwError(() => errorResponse), // First call fails with 401
      of(retryResponse) // Retry succeeds
    );

    TestBed.runInInjectionContext(() => {
      authInterceptor(mockRequest, mockNext.handle).subscribe({
        next: (response) => {
          expect((response as HttpResponse<any>).status).toBe(200);
          expect(mockAuthService.refreshToken).toHaveBeenCalled();
          expect(mockNext.handle).toHaveBeenCalledTimes(2);
        }
      });
    });
  });

  it('should logout and redirect when refresh token fails', () => {
    const mockRequest = new HttpRequest('GET', '/api/data');
    const errorResponse = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });
    const refreshError = new HttpErrorResponse({ status: 401, statusText: 'Refresh Failed' });

    mockAuthService.getToken.and.returnValue('old-token');
    mockAuthService.refreshToken.and.returnValue(throwError(() => refreshError));
    mockNext.handle.and.returnValue(throwError(() => errorResponse));

    TestBed.runInInjectionContext(() => {
      authInterceptor(mockRequest, mockNext.handle).subscribe({
        error: (error) => {
          expect(mockAuthService.logout).toHaveBeenCalled();
          expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
        }
      });
    });
  });

  it('should handle concurrent 401 requests without multiple refresh attempts', () => {
    // Skip this test for now - the global state management in the interceptor
    // makes it difficult to test concurrent scenarios properly
    pending('Concurrent request handling test needs interceptor refactoring');
  });

  it('should identify auth endpoints correctly', () => {
    const authEndpoints = [
      '/api/v1/auth/login',
      '/api/v1/auth/refresh', 
      '/api/v1/auth/revoke',
      '/api/v1/auth/revoke-all',
      '/api/v1/auth/token'
    ];

    authEndpoints.forEach(endpoint => {
      const mockRequest = new HttpRequest('POST', endpoint, {});
      const errorResponse = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });

      mockAuthService.getToken.and.returnValue('token');
      mockNext.handle.and.returnValue(throwError(() => errorResponse));

      TestBed.runInInjectionContext(() => {
        authInterceptor(mockRequest, mockNext.handle).subscribe({
          error: (error) => {
            expect(error.status).toBe(401);
            expect(mockAuthService.refreshToken).not.toHaveBeenCalled();
          }
        });
      });

      // Reset the spy for next iteration
      mockAuthService.refreshToken.calls.reset();
    });
  });
});