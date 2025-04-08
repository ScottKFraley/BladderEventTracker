// app/admin/admin-config/admin-config.component.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConfigService } from '../../services/config.service';

@Component({
  selector: 'app-admin-config',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-config.component.html',
  styleUrl: './admin-config.component.sass'
})
export class AdminConfigComponent {
  private configService = inject(ConfigService);
  
  currentDays = signal<number>(2);
  saveStatus = signal<string>('');

  constructor() {
    // Initialize with current value
    this.configService.getDaysPrevious().subscribe(days => {
      this.currentDays.set(days);
    });
  }

  updateDays(days: number) {
    if (days >= 1 && days <= 30) {
      this.configService.setDaysPrevious(days);
      this.saveStatus.set('success');
      setTimeout(() => this.saveStatus.set(''), 2000);
    } else {
      this.saveStatus.set('error');
    }
  }
}
