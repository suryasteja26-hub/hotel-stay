import { Component, Input } from '@angular/core';

// Reusable loading / empty / error display.
@Component({
  selector: 'app-state-banner',
  standalone: true,
  template: `
    @switch (state) {
      @case ('loading') {
        <div class="banner banner--loading">
          <span class="spinner" aria-hidden="true"></span>
          <span>Searching for rooms…</span>
        </div>
      }
      @case ('empty') {
        <div class="banner banner--empty">
          <strong>No rooms found</strong>
          <span>Try different dates or another destination.</span>
        </div>
      }
      @case ('error') {
        <div class="banner banner--error" role="alert">
          <strong>Something went wrong</strong>
          <span>{{ message || 'Please try again.' }}</span>
        </div>
      }
    }
  `,
})
export class StateBannerComponent {
  @Input() state: 'loading' | 'empty' | 'error' = 'loading';
  @Input() message?: string;
}
