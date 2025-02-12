// login.component.ts
import { Component } from '@angular/core';
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
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  loginForm: FormGroup;
  errorMessage: string = '';
  isLoading: boolean = false;

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

      this.authService.login(this.loginForm.value).subscribe({
        next: () => {
          this.router.navigate(['/dashboard']); // or your desired route
        },
        error: (error: string) => {
          this.errorMessage = error;
          this.isLoading = false;
        },
        complete: () => {
          this.isLoading = false;
        }
      });
    }
  }
}

