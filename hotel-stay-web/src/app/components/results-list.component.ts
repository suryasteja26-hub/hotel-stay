import { Component, EventEmitter, Input, Output, computed, signal } from '@angular/core';
import { HotelRoomOption } from '../models/hotel.models';
import { RoomOptionCardComponent } from './room-option-card.component';

type SortDir = 'asc' | 'desc';

@Component({
  selector: 'app-results-list',
  standalone: true,
  imports: [RoomOptionCardComponent],
  template: `
    <div class="results">
      <div class="results__bar">
        <span>{{ options.length }} room{{ options.length === 1 ? '' : 's' }} found</span>
        <label class="results__sort">
          Sort by total price
          <select [value]="sortDir()" (change)="onSortChange($event)">
            <option value="asc">Lowest first</option>
            <option value="desc">Highest first</option>
          </select>
        </label>
      </div>

      <div class="results__grid">
        @for (option of sortedOptions(); track option.id) {
          <app-room-option-card [option]="option" (select)="select.emit($event)" />
        }
      </div>
    </div>
  `,
})
export class ResultsListComponent {
  private readonly _options = signal<HotelRoomOption[]>([]);
  readonly sortDir = signal<SortDir>('asc');

  @Input({ required: true })
  set options(value: HotelRoomOption[]) {
    this._options.set(value ?? []);
  }
  get options(): HotelRoomOption[] {
    return this._options();
  }

  @Output() select = new EventEmitter<HotelRoomOption>();

  readonly sortedOptions = computed(() => {
    const dir = this.sortDir() === 'asc' ? 1 : -1;
    return [...this._options()].sort((a, b) => (a.totalPrice - b.totalPrice) * dir);
  });

  onSortChange(event: Event): void {
    this.sortDir.set((event.target as HTMLSelectElement).value as SortDir);
  }
}
