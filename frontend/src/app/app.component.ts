import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { NavbarComponent } from './navbar/navbar.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    NgbModule,
    RouterOutlet,
    NavbarComponent
  ],
  templateUrl: 'app.component.html',
  styleUrls: [ /* './app.component.sass' */ ],
})
export class AppComponent {
  title = 'Bladder Event Tracker';
}
