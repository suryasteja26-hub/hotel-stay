import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  HotelRoomOption,
  HotelSearchRequest,
  ReservationResponse,
  ReserveRoomRequest,
} from '../models/hotel.models';

@Injectable({ providedIn: 'root' })
export class HotelApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  search(request: HotelSearchRequest): Observable<HotelRoomOption[]> {
    let params = new HttpParams()
      .set('destination', request.destination)
      .set('checkIn', request.checkIn)
      .set('checkOut', request.checkOut);

    if (request.roomType) {
      params = params.set('roomType', request.roomType);
    }

    return this.http.get<HotelRoomOption[]>(`${this.baseUrl}/hotels/search`, { params });
  }

  reserve(request: ReserveRoomRequest): Observable<ReservationResponse> {
    return this.http.post<ReservationResponse>(`${this.baseUrl}/hotels/reserve`, request);
  }

  getReservation(reference: string): Observable<ReservationResponse> {
    return this.http.get<ReservationResponse>(
      `${this.baseUrl}/hotels/reservation/${encodeURIComponent(reference)}`,
    );
  }
}
