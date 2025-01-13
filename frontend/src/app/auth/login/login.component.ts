import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators, FormBuilder } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';


@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {

  loginForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.loginForm = this.fb.group({
      username: new FormControl('',
        [Validators.required,
        Validators.minLength(3),
        Validators.maxLength(50),
          //Validators.pattern('^[a-zA-Z0-9]+$')
        ]),
      password: new FormControl('', [Validators.required])
    });
  }

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
    // Access the form values
    const loginData = this.loginForm.value;

    // Send login data to your backend API
    // ... 
  }

}
