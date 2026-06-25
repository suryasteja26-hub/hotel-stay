import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DocumentType } from '../models/hotel.models';
import { environment } from '../../environments/environment';

// Client-side mirror of the backend destination rules, for instant UX feedback.
// The backend remains the source of truth (returns 422 on document mismatch).
@Injectable({ providedIn: 'root' })
export class DestinationRulesService {
  private http = inject(HttpClient);

  // Fallback lists used until the API is reachable.
  private domestic: string[] = ['london', 'manchester'];
  private international: string[] = ['paris', 'new york', 'tokyo'];

  constructor() {
    this.loadFromApi();
  }

  private loadFromApi(): void {
    const url = `${environment.apiBaseUrl}/hotels/destinations`;
    this.http.get<{ domestic: string[]; international: string[] }>(url).subscribe({
      next: (data) => {
        if (data?.domestic?.length) {
          this.domestic = data.domestic.map((d) => d.trim().toLowerCase());
        }
        if (data?.international?.length) {
          this.international = data.international.map((d) => d.trim().toLowerCase());
        }
      },
      error: () => {
        // Keep fallback lists on error — API may be unavailable in local dev.
      },
    });
  }

  isDomestic(city: string): boolean {
    return this.domestic.includes(this.normalize(city));
  }

  isInternational(city: string): boolean {
    return this.international.includes(this.normalize(city));
  }

  isKnown(city: string): boolean {
    return this.isDomestic(city) || this.isInternational(city);
  }

  acceptedDocuments(city: string): DocumentType[] {
    if (this.isInternational(city)) {
      return ['Passport'];
    }
    if (this.isDomestic(city)) {
      return ['NationalId', 'Passport'];
    }
    return [];
  }

  isDocumentAccepted(city: string, documentType: DocumentType): boolean {
    return this.acceptedDocuments(city).includes(documentType);
  }

  private normalize(city: string): string {
    return (city ?? '').trim().toLowerCase();
  }
}
