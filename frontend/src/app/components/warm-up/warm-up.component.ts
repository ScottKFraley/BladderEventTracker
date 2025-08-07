import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { WarmUpService } from '../../services/warm-up.service';
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-warm-up',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './warm-up.component.html',
  styleUrls: ['./warm-up.component.sass']
})
export class WarmUpComponent implements OnInit, OnDestroy {
  isLoading = true;
  errorMessage = '';
  showRetry = false;
  progressMessage = 'Warming up services...';
  
  private subscription = new Subscription();

  constructor(
    private warmUpService: WarmUpService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.startWarmUp();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  private startWarmUp(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.showRetry = false;
    this.progressMessage = 'Warming up services...';

    const warmUpSub = this.warmUpService.warmUpServices().subscribe({
      next: (success) => {
        if (success) {
          this.progressMessage = 'Services ready! Redirecting...';
          this.handleSuccessfulWarmUp();
        } else {
          this.handleWarmUpError('Service warm-up completed but returned unexpected response.');
        }
      },
      error: (error) => {
        this.handleWarmUpError(error.error || 'An unexpected error occurred during warm-up.');
      }
    });

    this.subscription.add(warmUpSub);
  }

  private handleSuccessfulWarmUp(): void {
    // Check if user is already authenticated
    const authSub = this.authService.isAuthenticated().subscribe({
      next: (isAuthenticated) => {
        setTimeout(() => {
          if (isAuthenticated) {
            // User is authenticated, redirect to dashboard
            this.router.navigate(['/dashboard']);
          } else {
            // User is not authenticated, redirect to login
            this.router.navigate(['/login']);
          }
        }, 1000); // Brief delay to show success message
      },
      error: () => {
        // If authentication check fails, default to login
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1000);
      }
    });

    this.subscription.add(authSub);
  }

  private handleWarmUpError(message: string): void {
    this.isLoading = false;
    this.errorMessage = message;
    this.showRetry = true;
    this.progressMessage = '';
  }

  onRetry(): void {
    this.startWarmUp();
  }

  onSkip(): void {
    // Skip warm-up and proceed to authentication check
    const authSub = this.authService.isAuthenticated().subscribe({
      next: (isAuthenticated) => {
        if (isAuthenticated) {
          this.router.navigate(['/dashboard']);
        } else {
          this.router.navigate(['/login']);
        }
      },
      error: () => {
        this.router.navigate(['/login']);
      }
    });

    this.subscription.add(authSub);
  }
}