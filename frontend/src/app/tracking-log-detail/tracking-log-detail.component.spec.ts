import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TrackingLogDetailComponent } from './tracking-log-detail.component';

describe('TrackingLogDetailComponent', () => {
  let component: TrackingLogDetailComponent;
  let fixture: ComponentFixture<TrackingLogDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TrackingLogDetailComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(TrackingLogDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
