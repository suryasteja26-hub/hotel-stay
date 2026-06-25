import { Component, EventEmitter, Input, Output, computed, signal } from '@angular/core';
import { HotelOffer } from '../models/hotel.models';
import { RoomOptionCardComponent } from './room-option-card.component';

type SortDir = 'asc' | 'desc';

@Component({
  selector: 'app-results-list',
  standalone: true,
  imports: [RoomOptionCardComponent],
  template: `
    <div class="results">
      <div class="results__bar">
        <span>{{ offers.length }} room{{ offers.length === 1 ? '' : 's' }} found</span>
        <label class="results__sort">
          Sort by total price
          <select [value]="sortDir()" (change)="onSortChange($event)">
            <option value="asc">Lowest first</option>
            <option value="desc">Highest first</option>
          </select>
        </label>
      </div>

      <div class="results__grid">
        @for (offer of sortedOffers(); track trackOffer(offer)) {
          <app-room-option-card [offer]="offer" [nights]="nights" (select)="select.emit($event)" />
        }
      </div>
    </div>
  `,
})
export class ResultsListComponent {
  private readonly _offers = signal<HotelOffer[]>([]);
  readonly sortDir = signal<SortDir>('asc');

  @Input({ required: true })
  set offers(value: HotelOffer[]) {
    this._offers.set(value ?? []);
  }
  get offers(): HotelOffer[] {
    return this._offers();
  }

  // Nights is constant across a result set, so sorting by pricePerNight is
  // equivalent to sorting by total — kept explicit for clarity.
  @Input({ required: true }) nights = 0;

  @Output() select = new EventEmitter<HotelOffer>();

  readonly sortedOffers = computed(() => {
    const dir = this.sortDir() === 'asc' ? 1 : -1;
    return [...this._offers()].sort((a, b) => (a.pricePerNight - b.pricePerNight) * dir);
  });

  trackOffer(offer: HotelOffer): string {
    return `${offer.providerId}-${offer.hotelId}-${offer.roomType}`;
  }

  onSortChange(event: Event): void {
    this.sortDir.set((event.target as HTMLSelectElement).value as SortDir);
  }
}
