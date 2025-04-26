// services/api-endpoints.service.ts
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiEndpointsService {
  private readonly baseUrl = '/api/v1';

  // Tracker endpoints
  private readonly trackerEndpoints = {
    base: `${this.baseUrl}/tracker`,
    getByDaysAndUser: (days: number, userId: string) => 
      `${this.baseUrl}/tracker/${days}/${userId}`,
  };

  // Auth endpoints
  private readonly authEndpoints = {
    base: `${this.baseUrl}/auth`,
    login: `${this.baseUrl}/auth/login`,
    refresh: `${this.baseUrl}/auth/token`,
  };

  getByDaysAndUserEndpoint(days: number, userId: string) {
    return this.trackerEndpoints.getByDaysAndUser(days, userId);
  }

  getTrackerEndpoints() {
    return this.trackerEndpoints;
  }

  getAuthEndpoints() {
    return this.authEndpoints;
  }

  getEndpointBase() {
    return this.baseUrl;
  }
}
