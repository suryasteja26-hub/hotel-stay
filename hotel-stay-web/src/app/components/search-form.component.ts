import { Component, EventEmitter, Output, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { HotelSearchRequest, ROOM_TYPES, RoomType } from '../models/hotel.models';

// Cross-field validator: checkOut must be strictly after checkIn.
function checkOutAfterCheckIn(group: AbstractControl): ValidationErrors | null {
  const checkIn = group.get('checkIn')?.value;
  const checkOut = group.get('checkOut')?.value;
  if (!checkIn || !checkOut) {
    return null;
  }
  return checkOut > checkIn ? null : { checkOutBeforeCheckIn: true };
}

@Component({
  selector: 'app-search-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <form class="card search-form" [formGroup]="form" (ngSubmit)="onSubmit()">
      <div class="field">
        <label for="destination">Destination</label>
        <input id="destination" type="text" formControlName="destination"
               placeholder="e.g. London, Paris" />
      </div>

      <div class="field">
        <label for="checkIn">Check-in</label>
        <input id="checkIn" type="date" formControlName="checkIn" />
      </div>

      <div class="field">
        <label for="checkOut">Check-out</label>
        <input id="checkOut" type="date" formControlName="checkOut" />
      </div>

      <div class="field">
        <label for="roomType">Room type (optional)</label>
        <select id="roomType" formControlName="roomType">
          <option value="">Any</option>
          @for (type of roomTypes; track type) {
            <option [value]="type">{{ type }}</option>
          }
        </select>
      </div>

      <div class="field field--action">
        <button type="submit" [disabled]="form.invalid">Search</button>
      </div>

      @if (form.errors?.['checkOutBeforeCheckIn'] && form.get('checkOut')?.touched) {
        <p class="field-error">Check-out must be after check-in.</p>
      }
    </form>
  `,
})
export class SearchFormComponent {
  @Output() search = new EventEmitter<HotelSearchRequest>();

  private readonly fb = inject(FormBuilder);

  readonly roomTypes = ROOM_TYPES;

  readonly form = this.fb.group(
    {
      destination: ['', Validators.required],
      checkIn: ['', Validators.required],
      checkOut: ['', Validators.required],
      roomType: [''],
    },
    { validators: checkOutAfterCheckIn },
  );

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.search.emit({
      destination: value.destination!.trim(),
      checkIn: value.checkIn!,
      checkOut: value.checkOut!,
      roomType: (value.roomType || undefined) as RoomType | undefined,
    });
  }
}
