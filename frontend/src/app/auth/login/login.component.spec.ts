import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { Component } from '@angular/core';
import { LoginComponent } from './login.component';
import { AuthService } from '../auth.service';
import { of, throwError } from 'rxjs';
import { Router } from '@angular/router';

@Component({
  template: '<div>Mock Dashboard</div>'
})
class MockDashboardComponent { }

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['login']);
    
    await TestBed.configureTestingModule({
      imports: [
        ReactiveFormsModule,
        RouterTestingModule.withRoutes([
          { path: 'dashboard', component: MockDashboardComponent }
        ]),
        LoginComponent
      ],
      declarations: [MockDashboardComponent],
      providers: [
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    router = TestBed.inject(Router);
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('form validation', () => {
    it('should be invalid when empty', () => {
      expect(component.loginForm.valid).toBeFalsy();
    });

    it('should validate username requirements', () => {
      const usernameControl = component.loginForm.controls['username'];
      
      usernameControl.setValue('');
      expect(usernameControl.hasError('required')).toBeTruthy();

      usernameControl.setValue('ab');
      expect(usernameControl.hasError('minlength')).toBeTruthy();

      usernameControl.setValue('a'.repeat(51));
      expect(usernameControl.hasError('maxlength')).toBeTruthy();

      usernameControl.setValue('validuser');
      expect(usernameControl.valid).toBeTruthy();
    });

    it('should validate password is required', () => {
      const passwordControl = component.loginForm.controls['password'];
      
      passwordControl.setValue('');
      expect(passwordControl.hasError('required')).toBeTruthy();

      passwordControl.setValue('password123');
      expect(passwordControl.valid).toBeTruthy();
    });
  });

  describe('onSubmit', () => {
    it('should call auth service and navigate on successful login', () => {
      // Arrange
      const navigateSpy = spyOn(router, 'navigate');
      authService.login.and.returnValue(of({ token: 'mock-token' }));
      component.loginForm.setValue({
        username: 'testuser',
        password: 'testpass'
      });

      // Act
      component.onSubmit();

      // Assert
      expect(authService.login).toHaveBeenCalledWith({
        username: 'testuser',
        password: 'testpass'
      });
      expect(navigateSpy).toHaveBeenCalledWith(['/dashboard']);
      expect(component.isLoading).toBeFalse();
      expect(component.errorMessage).toBe('');
    });

    it('should handle login error correctly', () => {
      // Arrange
      authService.login.and.returnValue(throwError(() => 'Invalid credentials'));
      component.loginForm.setValue({
        username: 'testuser',
        password: 'wrongpass'
      });

      // Act
      component.onSubmit();

      // Assert
      expect(component.errorMessage).toBe('Invalid credentials');
      expect(component.isLoading).toBeFalse();
    });

    it('should not call login if form is invalid', () => {
      // Act
      component.onSubmit();

      // Assert
      expect(authService.login).not.toHaveBeenCalled();
    });
  });

  describe('loading state', () => {
    it('should show loading state during login attempt', () => {
      // Arrange
      authService.login.and.returnValue(of({ token: 'mock-token' }));
      component.loginForm.setValue({
        username: 'testuser',
        password: 'testpass'
      });

      // Act
      component.onSubmit();

      // Assert
      expect(component.isLoading).toBeFalse(); // Should be false after completion
    });
  });
});
