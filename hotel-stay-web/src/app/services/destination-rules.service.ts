import { Injectable } from '@angular/core';
import { DocumentType } from '../models/hotel.models';

// Client-side mirror of the backend destination rules, for instant UX feedback.
// The backend remains the source of truth (returns 422 on document mismatch).
@Injectable({ providedIn: 'root' })
export class DestinationRulesService {
  private readonly domestic = ['london', 'manchester'];
  private readonly international = ['paris', 'new york', 'tokyo'];

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
