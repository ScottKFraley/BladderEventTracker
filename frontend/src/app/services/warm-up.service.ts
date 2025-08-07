import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, timeout, catchError, map, TimeoutError } from 'rxjs';
import { ApiEndpointsService } from './api-endpoints.service';

@Injectable({
  providedIn: 'root'
})
export class WarmUpService {
  private readonly WARM_UP_TIMEOUT = 4 * 60 * 1000; // 4 minutes in milliseconds

  constructor(
    private http: HttpClient,
    private apiEndpoints: ApiEndpointsService
  ) {}

  /**
   * Calls the warm-up endpoint to initialize backend services
   * @returns Observable<boolean> indicating success (true) or failure (false)
   */
  warmUpServices(): Observable<boolean> {
    const warmUpEndpoint = `${this.apiEndpoints.getEndpointBase()}/warmup`;
    
    return this.http.get(warmUpEndpoint, { 
      // No authentication required for warm-up endpoint
      observe: 'response'
    }).pipe(
      timeout(this.WARM_UP_TIMEOUT),
      map(response => {
        // 204 No Content indicates successful warm-up
        return response.status === 204;
      }),
      catchError((error: HttpErrorResponse) => {
        console.error('Warm-up service error:', error);
        
        // Always return false for any error to indicate failure
        // but don't throw to allow graceful error handling in components
        return throwError(() => ({
          success: false,
          error: this.getErrorMessage(error)
        }));
      })
    );
  }

  /**
   * Gets a human-readable error message based on the HTTP error
   */
  private getErrorMessage(error: HttpErrorResponse): string {
    // Check for timeout scenarios
    if (error.status === 0 || 
        error.error instanceof TimeoutError || 
        error.status === 408) {
      return 'Service warm-up timed out. Please try again.';
    }
    
    switch (error.status) {
      case 0:
        return 'Unable to connect to services. Please check your connection.';
      case 404:
        return 'Warm-up service not found.';
      case 500:
        return 'Server error during warm-up. Please try again.';
      case 503:
        return 'Services are currently unavailable. Please try again later.';
      default:
        return `Service warm-up failed (${error.status}). Please try again.`;
    }
  }
}