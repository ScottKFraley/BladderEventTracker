import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardComponent } from './dashboard.component';
import { Store } from '@ngrx/store';
import { AuthService } from '../auth/auth.service';
import { ConfigService } from '../services/config.service';
import { of } from 'rxjs';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let mockStore: jasmine.SpyObj<Store>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockConfigService: jasmine.SpyObj<ConfigService>;

  beforeEach(async () => {
    mockStore = jasmine.createSpyObj('Store', ['dispatch', 'select']);
    mockAuthService = jasmine.createSpyObj('AuthService', ['getCurrentUserId']);
    mockConfigService = jasmine.createSpyObj('ConfigService', ['getDaysPrevious']);

    mockStore.select.and.returnValue(of([]));
    mockAuthService.getCurrentUserId.and.returnValue('test-user-id');
    mockConfigService.getDaysPrevious.and.returnValue(of(7));

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        { provide: Store, useValue: mockStore },
        { provide: AuthService, useValue: mockAuthService },
        { provide: ConfigService, useValue: mockConfigService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load tracking logs on init', () => {
    fixture.detectChanges();
    
    expect(mockConfigService.getDaysPrevious).toHaveBeenCalled();
    expect(mockAuthService.getCurrentUserId).toHaveBeenCalled();
    expect(mockStore.dispatch).toHaveBeenCalled();
  });

  // Add more tests as needed
});
