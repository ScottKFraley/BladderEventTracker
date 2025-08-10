// login.component.ts
import { Component, OnDestroy } from '@angular/core';
import { FormGroup, FormControl, Validators, FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { CommonModule } from '@angular/common';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [NgbModule, ReactiveFormsModule, CommonModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.sass']
})
export class LoginComponent implements OnDestroy {
  loginForm: FormGroup;
  errorMessage: string = '';
  isLoading: boolean = false;
  loadingMessage: string = 'Signing in...';
  showAdvancedLoading: boolean = false;
  private loadingTimer: any;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      username: new FormControl('', [
        Validators.required,
        Validators.minLength(3),
        Validators.maxLength(50),
      ]),
      password: new FormControl('', [Validators.required])
    });
  }

  // Validation methods

  isUsernameDirtyAndHasErrors(): boolean {
    const usernameControl = this.loginForm.get('username');
    return usernameControl?.errors != null && usernameControl?.dirty;
  }

  isUsernameRequired(): boolean {
    const usernameControl = this.loginForm.get('username');
    return usernameControl?.errors != null && usernameControl?.hasError('required');
  }

  // @if (loginForm.get('password').errors && loginForm.get('password').dirty) {
  isPasswordDirtyAndHasErrors(): boolean {
    const passwordControl = this.loginForm.get('password');
    return passwordControl?.errors != null && passwordControl?.dirty;
  }

  onSubmit(): void {
    if (this.loginForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      this.loadingMessage = 'Signing in...';
      this.showAdvancedLoading = false;
      
      // Show advanced loading message after 10 seconds
      this.loadingTimer = setTimeout(() => {
        if (this.isLoading) {
          this.loadingMessage = 'Initializing database connection...';
          this.showAdvancedLoading = true;
        }
      }, 10000);

      this.authService.login(this.loginForm.value).subscribe({
        next: () => {
          console.log('Login successful, navigating to dashboard');
          this.clearLoadingTimer();
          this.router.navigate(['/dashboard']); // or your desired route
        },
        error: (error: string) => {
          console.error('Login error in component:', {
            error: error,
            formValue: this.loginForm.value.username, // Don't log password
            userAgent: navigator.userAgent,
            timestamp: new Date().toISOString()
          });
          
          // Enhanced error messaging for timeouts
          if (error.includes('timed out') || error.includes('timeout')) {
            this.errorMessage = 'Login is taking longer than usual. This may be due to database initialization on Azure. Please try again in a moment.';
          } else {
            this.errorMessage = error;
          }
          
          this.clearLoadingTimer();
          this.isLoading = false;
          this.showAdvancedLoading = false;
        },
        complete: () => {
          this.clearLoadingTimer();
          this.isLoading = false;
          this.showAdvancedLoading = false;
        }
      });
    }
  }
  
  private clearLoadingTimer(): void {
    if (this.loadingTimer) {
      clearTimeout(this.loadingTimer);
      this.loadingTimer = null;
    }
  }
  
  ngOnDestroy(): void {
    this.clearLoadingTimer();
  }
}

