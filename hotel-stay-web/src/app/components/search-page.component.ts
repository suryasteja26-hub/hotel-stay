import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import {
  HotelOffer,
  HotelSearchRequest,
  Reservation,
  ReserveRequest,
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
          @if (selectedOffer(); as offer) {
            <app-reservation-form
              [offer]="offer"
              [checkIn]="lastSearch()!.checkIn"
              [checkOut]="lastSearch()!.checkOut"
              [nights]="nights()"
              [submitting]="reserving()"
              (reserve)="onReserve($event)"
              (cancel)="clearSelection()" />
          } @else {
            <app-results-list
              [offers]="results()"
              [nights]="nights()"
              (select)="onSelect($event)" />
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
  readonly results = signal<HotelOffer[]>([]);
  readonly nights = signal(0);
  readonly selectedOffer = signal<HotelOffer | null>(null);
  readonly reservation = signal<Reservation | null>(null);
  readonly lastSearch = signal<HotelSearchRequest | null>(null);
  readonly reserving = signal(false);
  readonly errorMessage = signal<string | undefined>(undefined);

  onSearch(request: HotelSearchRequest): void {
    this.lastSearch.set(request);
    this.selectedOffer.set(null);
    this.reservation.set(null);
    this.state.set('loading');

    this.api.search(request).subscribe({
      next: (response) => {
        this.results.set(response.results);
        this.nights.set(response.nights);
        this.state.set(response.results.length ? 'results' : 'empty');
      },
      error: (err: HttpErrorResponse) => this.failWith(err),
    });
  }

  onSelect(offer: HotelOffer): void {
    this.selectedOffer.set(offer);
  }

  clearSelection(): void {
    this.selectedOffer.set(null);
  }

  onReserve(request: ReserveRequest): void {
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
    this.nights.set(0);
    this.selectedOffer.set(null);
    this.reservation.set(null);
    this.errorMessage.set(undefined);
  }

  private failWith(err: HttpErrorResponse): void {
    // Surface the API's { status, error, message } envelope when present.
    this.errorMessage.set(err.error?.message ?? err.message ?? 'Unexpected error.');
    this.state.set('error');
  }
}
