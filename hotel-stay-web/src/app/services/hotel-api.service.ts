import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  HotelSearchRequest,
  Reservation,
  ReserveRequest,
  SearchResponse,
} from '../models/hotel.models';

@Injectable({ providedIn: 'root' })
export class HotelApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  search(request: HotelSearchRequest): Observable<SearchResponse> {
    let params = new HttpParams()
      .set('destination', request.destination)
      .set('checkIn', request.checkIn)
      .set('checkOut', request.checkOut);

    if (request.roomType) {
      params = params.set('roomType', request.roomType);
    }

    return this.http.get<SearchResponse>(`${this.baseUrl}/hotels/search`, { params });
  }

  reserve(request: ReserveRequest): Observable<Reservation> {
    return this.http.post<Reservation>(`${this.baseUrl}/hotels/reserve`, request);
  }

  getReservation(reference: string): Observable<Reservation> {
    return this.http.get<Reservation>(
      `${this.baseUrl}/hotels/reservation/${encodeURIComponent(reference)}`,
    );
  }
}
