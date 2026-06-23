import { Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import {
  HotelRoomOption,
  HotelSearchRequest,
  ReservationResponse,
  ReserveRoomRequest,
} from '../models/hotel.models';
import { HotelApiService } from '../services/hotel-api.service';
import { SearchFormComponent } from './search-form.component';
import { ResultsListComponent } from './results-list.component';
import { ReservationFormComponent } from './reservation-form.component';
import { ConfirmationComponent } from './confirmation.component';
import { StateBannerComponent } from './state-banner.component';

type ViewState = 'idle' | 'loading' | 'results' | 'empty' | 'error' | 'confirmed';

@Component({
  selector: 'app-search-page',
  standalone: true,
  imports: [
    SearchFormComponent,
    ResultsListComponent,
    ReservationFormComponent,
    ConfirmationComponent,
    StateBannerComponent,
  ],
  template: `
    <section class="search-page">
      @if (state() !== 'confirmed') {
        <app-search-form (search)="onSearch($event)" />
      }

      @switch (state()) {
        @case ('loading') {
          <app-state-banner state="loading" />
        }
        @case ('empty') {
          <app-state-banner state="empty" />
        }
        @case ('error') {
          <app-state-banner state="error" [message]="errorMessage()" />
        }
        @case ('results') {
          @if (selectedOption(); as option) {
            <app-reservation-form
              [selectedOption]="option"
              [checkIn]="lastSearch()!.checkIn"
              [checkOut]="lastSearch()!.checkOut"
              [submitting]="reserving()"
              (reserve)="onReserve($event)"
              (cancel)="clearSelection()" />
          } @else {
            <app-results-list [options]="results()" (select)="onSelect($event)" />
          }
        }
        @case ('confirmed') {
          <app-confirmation [reservation]="reservation()!" (newSearch)="reset()" />
        }
      }
    </section>
  `,
})
export class SearchPageComponent {
  private readonly api = inject(HotelApiService);

  readonly state = signal<ViewState>('idle');
  readonly results = signal<HotelRoomOption[]>([]);
  readonly selectedOption = signal<HotelRoomOption | null>(null);
  readonly reservation = signal<ReservationResponse | null>(null);
  readonly lastSearch = signal<HotelSearchRequest | null>(null);
  readonly reserving = signal(false);
  readonly errorMessage = signal<string | undefined>(undefined);

  onSearch(request: HotelSearchRequest): void {
    this.lastSearch.set(request);
    this.selectedOption.set(null);
    this.reservation.set(null);
    this.state.set('loading');

    this.api.search(request).subscribe({
      next: (options) => {
        this.results.set(options);
        this.state.set(options.length ? 'results' : 'empty');
      },
      error: (err: HttpErrorResponse) => this.failWith(err),
    });
  }

  onSelect(option: HotelRoomOption): void {
    this.selectedOption.set(option);
  }

  clearSelection(): void {
    this.selectedOption.set(null);
  }

  onReserve(request: ReserveRoomRequest): void {
    this.reserving.set(true);
    this.errorMessage.set(undefined);

    this.api.reserve(request).subscribe({
      next: (reservation) => {
        this.reserving.set(false);
        this.reservation.set(reservation);
        this.state.set('confirmed');
      },
      error: (err: HttpErrorResponse) => {
        this.reserving.set(false);
        this.failWith(err);
      },
    });
  }

  reset(): void {
    this.state.set('idle');
    this.results.set([]);
    this.selectedOption.set(null);
    this.reservation.set(null);
    this.errorMessage.set(undefined);
  }

  private failWith(err: HttpErrorResponse): void {
    // Surface the API's { message } when present (e.g. 422 document mismatch, 400 validation).
    this.errorMessage.set(err.error?.message ?? err.message ?? 'Unexpected error.');
    this.state.set('error');
  }
}
