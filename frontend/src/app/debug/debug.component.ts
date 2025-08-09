import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MobileDebugService } from '../services/mobile-debug.service';
import { AuthService } from '../auth/auth.service';
import { TrackingLogService } from '../services/tracking-log.service';
import { Subscription, interval } from 'rxjs';

@Component({
  selector: 'app-debug',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './debug.component.html',
  styleUrls: ['./debug.component.sass']
})
export class DebugComponent implements OnInit, OnDestroy {
  debugInfo: any = {};
  lastError: any = null;
  logs: string[] = [];
  isTestingConnection = false;
  connectionTestResult: any = null;
  
  private subscription = new Subscription();
  private originalConsoleError = console.error;
  private originalConsoleLog = console.log;

  constructor(
    private mobileDebug: MobileDebugService,
    private authService: AuthService,
    private trackingLogService: TrackingLogService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.refreshDebugInfo();
    
    // Intercept console messages to display on page
    this.interceptConsole();
    
    // Auto-refresh debug info every 5 seconds
    const refreshSub = interval(5000).subscribe(() => {
      this.refreshDebugInfo();
    });
    
    this.subscription.add(refreshSub);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    
    // Restore original console methods
    console.error = this.originalConsoleError;
    console.log = this.originalConsoleLog;
  }

  private interceptConsole(): void {
    console.error = (...args: any[]) => {
      this.originalConsoleError.apply(console, args);
      this.addLog('ERROR: ' + args.map(arg => 
        typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
      ).join(' '));
    };

    console.log = (...args: any[]) => {
      this.originalConsoleLog.apply(console, args);
      this.addLog('LOG: ' + args.map(arg => 
        typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
      ).join(' '));
    };
  }

  private addLog(message: string): void {
    const timestamp = new Date().toLocaleTimeString();
    this.logs.unshift(`[${timestamp}] ${message}`);
    
    // Keep only last 50 log entries
    if (this.logs.length > 50) {
      this.logs = this.logs.slice(0, 50);
    }
  }

  refreshDebugInfo(): void {
    this.debugInfo = this.mobileDebug.getDebugInfo();
    this.lastError = this.mobileDebug.getLastError();
  }

  clearLogs(): void {
    this.logs = [];
    this.addLog('Logs cleared');
  }

  testConnection(): void {
    this.isTestingConnection = true;
    this.connectionTestResult = null;
    this.addLog('Starting connection test...');
    
    this.mobileDebug.testConnection();
    
    // Also test our API endpoints
    const userId = this.authService.getCurrentUserId();
    if (userId) {
      this.trackingLogService.getTrackingLogs(2, userId).subscribe({
        next: (logs) => {
          this.connectionTestResult = {
            success: true,
            message: `API test successful - received ${logs.length} tracking logs`,
            timestamp: new Date().toISOString()
          };
          this.addLog(`API test successful - ${logs.length} logs received`);
          this.isTestingConnection = false;
        },
        error: (error) => {
          this.connectionTestResult = {
            success: false,
            message: error,
            timestamp: new Date().toISOString()
          };
          this.addLog(`API test failed: ${error}`);
          this.isTestingConnection = false;
        }
      });
    } else {
      this.connectionTestResult = {
        success: false,
        message: 'No user ID available for API test',
        timestamp: new Date().toISOString()
      };
      this.isTestingConnection = false;
    }
  }

  testLogin(): void {
    this.addLog('Testing login (using dummy credentials)...');
    
    this.authService.login({ username: 'test', password: 'test' }).subscribe({
      next: () => {
        this.addLog('Login test: Unexpected success');
      },
      error: (error) => {
        this.addLog(`Login test error (expected): ${error}`);
      }
    });
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  copyDebugInfo(): void {
    const debugData = {
      timestamp: new Date().toISOString(),
      lastError: this.lastError,
      debugInfo: this.debugInfo,
      logs: this.logs.slice(0, 10), // Last 10 logs
      connectionTest: this.connectionTestResult
    };

    const text = JSON.stringify(debugData, null, 2);
    
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(() => {
        this.addLog('Debug info copied to clipboard');
      }).catch(() => {
        this.showTextToCopy(text);
      });
    } else {
      this.showTextToCopy(text);
    }
  }

  private showTextToCopy(text: string): void {
    // Create a textarea element to show the text for manual copying
    const textarea = document.createElement('textarea');
    textarea.value = text;
    textarea.style.position = 'absolute';
    textarea.style.top = '0';
    textarea.style.left = '0';
    textarea.style.width = '100%';
    textarea.style.height = '200px';
    textarea.style.zIndex = '1000';
    document.body.appendChild(textarea);
    textarea.select();
    
    this.addLog('Debug info displayed in textarea - please copy manually');
    
    // Remove after 10 seconds
    setTimeout(() => {
      if (document.body.contains(textarea)) {
        document.body.removeChild(textarea);
      }
    }, 10000);
  }
}