import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DashboardTotalsComponent } from './dashboard-totals.component';

describe('DashboardTotalsComponent', () => {
  let component: DashboardTotalsComponent;
  let fixture: ComponentFixture<DashboardTotalsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardTotalsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(DashboardTotalsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
