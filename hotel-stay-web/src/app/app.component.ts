import { Component } from '@angular/core';
import { SearchPageComponent } from './components/search-page.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [SearchPageComponent],
  template: `
    <header class="app-header">
      <h1>Hotel Stay Availability</h1>
      <p>Search across PremierStays and BudgetNests in one place.</p>
    </header>
    <main class="app-main">
      <app-search-page />
    </main>
  `,
  styleUrl: './app.component.scss',
})
export class AppComponent {}
