import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import {
  CANCELLATION_POLICY_LABELS,
  HotelRoomOption,
} from '../models/hotel.models';

@Component({
  selector: 'app-room-option-card',
  standalone: true,
  imports: [CurrencyPipe],
  template: `
    <div class="card room-card">
      <div class="room-card__head">
        <span class="badge" [class.badge--premier]="option.provider === 'PremierStays'"
              [class.badge--budget]="option.provider === 'BudgetNests'">
          {{ option.provider }}
        </span>
        <span class="room-card__stars">{{ stars }}</span>
      </div>

      <h3 class="room-card__title">{{ option.roomType }}</h3>

      <dl class="room-card__details">
        <div>
          <dt>Per night</dt>
          <dd>{{ option.perNightRate | currency: 'GBP' }}</dd>
        </div>
        <div>
          <dt>Total ({{ option.nights }} night{{ option.nights === 1 ? '' : 's' }})</dt>
          <dd class="room-card__total">{{ option.totalPrice | currency: 'GBP' }}</dd>
        </div>
        <div>
          <dt>Cancellation</dt>
          <dd>{{ policyLabel }}</dd>
        </div>
      </dl>

      @if (option.amenities.length) {
        <ul class="room-card__amenities">
          @for (amenity of option.amenities; track amenity) {
            <li>{{ amenity }}</li>
          }
        </ul>
      }

      <button type="button" (click)="select.emit(option)">Reserve this room</button>
    </div>
  `,
})
export class RoomOptionCardComponent {
  @Input({ required: true }) option!: HotelRoomOption;
  @Output() select = new EventEmitter<HotelRoomOption>();

  get policyLabel(): string {
    return CANCELLATION_POLICY_LABELS[this.option.cancellationPolicy];
  }

  get stars(): string {
    return '★'.repeat(this.option.starRating) + '☆'.repeat(5 - this.option.starRating);
  }
}
