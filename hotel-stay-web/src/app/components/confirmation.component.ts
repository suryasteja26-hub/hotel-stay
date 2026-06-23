import { CurrencyPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import {
  CANCELLATION_POLICY_LABELS,
  ReservationResponse,
} from '../models/hotel.models';

@Component({
  selector: 'app-confirmation',
  standalone: true,
  imports: [CurrencyPipe],
  template: `
    <div class="card confirmation">
      <div class="confirmation__check" aria-hidden="true">✓</div>
      <h2>Reservation confirmed</h2>

      <p class="confirmation__reference">
        Reference <strong>{{ reservation.reference }}</strong>
      </p>

      <dl class="confirmation__details">
        <div><dt>Provider</dt><dd>{{ reservation.provider }}</dd></div>
        <div><dt>Destination</dt><dd>{{ reservation.destination }}</dd></div>
        <div><dt>Room type</dt><dd>{{ reservation.roomType }}</dd></div>
        <div><dt>Guest</dt><dd>{{ reservation.guestName }}</dd></div>
        <div><dt>Total price</dt><dd>{{ reservation.totalPrice | currency: 'GBP' }}</dd></div>
        <div><dt>Cancellation</dt><dd>{{ policyLabel }}</dd></div>
      </dl>

      <button type="button" (click)="newSearch.emit()">Start a new search</button>
    </div>
  `,
})
export class ConfirmationComponent {
  @Input({ required: true }) reservation!: ReservationResponse;
  @Output() newSearch = new EventEmitter<void>();

  get policyLabel(): string {
    return CANCELLATION_POLICY_LABELS[this.reservation.cancellationPolicy];
  }
}
