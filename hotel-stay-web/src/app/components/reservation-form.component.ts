import { CurrencyPipe } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import {
  DocumentType,
  HotelOffer,
  ReserveRequest,
} from '../models/hotel.models';
import { DestinationRulesService } from '../services/destination-rules.service';

@Component({
  selector: 'app-reservation-form',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe],
  template: `
    <form class="card reservation-form" [formGroup]="form" (ngSubmit)="onSubmit()">
      <h3>Reserve: {{ offer.hotelName }} — {{ offer.roomType }} ({{ offer.providerId }})</h3>
      <p class="reservation-form__summary">
        {{ offer.city }} · {{ checkIn }} → {{ checkOut }} ·
        {{ totalPrice | currency: offer.currency }} total
      </p>

      <div class="field">
        <label for="guestName">Guest name</label>
        <input id="guestName" type="text" formControlName="guestName" />
      </div>

      <div class="field">
        <label for="documentType">Document type</label>
        <select id="documentType" formControlName="documentType">
          <option value="Passport">Passport</option>
          <option value="NationalId">National ID</option>
        </select>
      </div>

      <div class="field">
        <label for="documentNumber">Document number</label>
        <input id="documentNumber" type="text" formControlName="documentNumber" />
      </div>

      @if (form.errors?.['documentNotAccepted'] && form.get('documentType')?.touched) {
        <p class="field-error">
          {{ isInternational ? 'International destinations require a Passport.'
                             : 'This destination does not accept the selected document.' }}
        </p>
      }

      <div class="field field--action">
        <button type="submit" [disabled]="form.invalid || submitting">
          {{ submitting ? 'Reserving…' : 'Confirm reservation' }}
        </button>
        <button type="button" class="btn-secondary" (click)="cancel.emit()">Back to results</button>
      </div>
    </form>
  `,
})
export class ReservationFormComponent implements OnInit {
  @Input({ required: true }) offer!: HotelOffer;
  @Input({ required: true }) checkIn!: string;
  @Input({ required: true }) checkOut!: string;
  @Input({ required: true }) nights!: number;
  @Input() submitting = false;
  @Output() reserve = new EventEmitter<ReserveRequest>();
  @Output() cancel = new EventEmitter<void>();

  private readonly fb = inject(FormBuilder);
  private readonly rules = inject(DestinationRulesService);

  readonly form = this.fb.group(
    {
      guestName: ['', [Validators.required, Validators.minLength(2)]],
      documentType: ['Passport' as DocumentType, Validators.required],
      documentNumber: ['', [Validators.required, Validators.minLength(3)]],
    },
    { validators: (group) => this.documentAcceptedValidator(group) },
  );

  ngOnInit(): void {
    // offer is now bound; re-run the cross-field validator against it.
    this.form.updateValueAndValidity();
  }

  get totalPrice(): number {
    return this.offer.pricePerNight * this.nights;
  }

  get isInternational(): boolean {
    return this.rules.isInternational(this.offer.city);
  }

  private documentAcceptedValidator(group: AbstractControl): ValidationErrors | null {
    // Runs once at construction before @Input bindings are applied, so guard
    // against offer not being set yet.
    if (!this.offer) {
      return null;
    }
    const documentType = group.get('documentType')?.value as DocumentType;
    if (!documentType) {
      return null;
    }
    return this.rules.isDocumentAccepted(this.offer.city, documentType)
      ? null
      : { documentNotAccepted: true };
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const offer = this.offer;

    this.reserve.emit({
      providerId: offer.providerId,
      hotelId: offer.hotelId,
      hotelName: offer.hotelName,
      city: offer.city,
      roomType: offer.roomType,
      pricePerNight: offer.pricePerNight,
      currency: offer.currency,
      checkIn: this.checkIn,
      checkOut: this.checkOut,
      cancellationPolicy: offer.cancellationPolicy,
      guest: {
        fullName: value.guestName!.trim(),
        documentType: value.documentType!,
        documentNumber: value.documentNumber!.trim(),
      },
    });
  }
}
