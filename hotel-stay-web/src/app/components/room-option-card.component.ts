import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import {
  CANCELLATION_POLICY_LABELS,
  HotelOffer,
} from '../models/hotel.models';

@Component({
  selector: 'app-room-option-card',
  standalone: true,
  imports: [CurrencyPipe],
  template: `
    <div class="card room-card">
      <div class="room-card__head">
        <span class="badge" [class.badge--premier]="offer.providerId === 'PremierStays'"
              [class.badge--budget]="offer.providerId === 'BudgetNests'">
          {{ offer.providerId }}
        </span>
        @if (offer.starRating !== null) {
          <span class="room-card__stars">{{ stars }}</span>
        }
      </div>

      <h3 class="room-card__title">{{ offer.hotelName }} — {{ offer.roomType }}</h3>
      <p class="room-card__city">{{ offer.city }}</p>

      <dl class="room-card__details">
        <div>
          <dt>Per night</dt>
          <dd>{{ offer.pricePerNight | currency: offer.currency }}</dd>
        </div>
        <div>
          <dt>Total ({{ nights }} night{{ nights === 1 ? '' : 's' }})</dt>
          <dd class="room-card__total">{{ totalPrice | currency: offer.currency }}</dd>
        </div>
        <div>
          <dt>Cancellation</dt>
          <dd>{{ policyLabel }}</dd>
        </div>
      </dl>

      @if (offer.amenities.length) {
        <ul class="room-card__amenities">
          @for (amenity of offer.amenities; track amenity) {
            <li>{{ amenity }}</li>
          }
        </ul>
      }

      <button type="button" (click)="select.emit(offer)">Reserve this room</button>
    </div>
  `,
})
export class RoomOptionCardComponent {
  @Input({ required: true }) offer!: HotelOffer;
  @Input({ required: true }) nights!: number;
  @Output() select = new EventEmitter<HotelOffer>();

  get totalPrice(): number {
    return this.offer.pricePerNight * this.nights;
  }

  get policyLabel(): string {
    return CANCELLATION_POLICY_LABELS[this.offer.cancellationPolicy];
  }

  get stars(): string {
    const rating = this.offer.starRating ?? 0;
    return '★'.repeat(rating) + '☆'.repeat(5 - rating);
  }
}
