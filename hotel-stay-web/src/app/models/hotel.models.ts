// Mirrors the backend contracts. Backend enums are serialized as strings,
// so string unions map 1:1 with no conversion.

export type RoomType = 'Standard' | 'Deluxe' | 'Suite';

export type DocumentType = 'Passport' | 'NationalId';

export type CancellationPolicy =
  | 'FreeCancellation48Hours'
  | 'Flexible24Hours'
  | 'NonRefundable';

export const ROOM_TYPES: RoomType[] = ['Standard', 'Deluxe', 'Suite'];

export interface HotelSearchRequest {
  destination: string;
  checkIn: string; // yyyy-MM-dd
  checkOut: string; // yyyy-MM-dd
  roomType?: RoomType;
}

// A single normalized, bookable offer (matches backend HotelOffer).
export interface HotelOffer {
  providerId: string;
  hotelId: string;
  hotelName: string;
  city: string;
  roomType: RoomType;
  pricePerNight: number;
  currency: string;
  availableRooms: number | null;
  description: string | null;
  cancellationPolicy: CancellationPolicy;
  amenities: string[];
  starRating: number | null;
}

// Search response wrapper (matches backend SearchResponse).
export interface SearchResponse {
  destination: string;
  checkIn: string;
  checkOut: string;
  nights: number;
  results: HotelOffer[];
}

export interface GuestRequest {
  fullName: string;
  documentType: DocumentType;
  documentNumber: string;
}

export interface ReserveRequest {
  providerId: string;
  hotelId: string;
  hotelName: string;
  city: string;
  roomType: RoomType;
  pricePerNight: number;
  currency: string;
  checkIn: string;
  checkOut: string;
  cancellationPolicy: CancellationPolicy;
  guest: GuestRequest;
}

export interface ReservationGuest {
  fullName: string;
  documentType: DocumentType;
}

// Confirmed reservation (matches backend Reservation response shape).
export interface Reservation {
  reference: string;
  providerId: string;
  hotelId: string;
  hotelName: string;
  city: string;
  roomType: RoomType;
  pricePerNight: number;
  currency: string;
  checkIn: string;
  checkOut: string;
  nights: number;
  totalPrice: number;
  cancellationPolicy: CancellationPolicy | null;
  guest: ReservationGuest;
  createdAt: string;
}

// Consistent error envelope returned by the API on non-2xx responses.
export interface ApiError {
  status: number;
  error: string;
  message: string;
}

// Human-friendly labels for cancellation policies.
export const CANCELLATION_POLICY_LABELS: Record<CancellationPolicy, string> = {
  FreeCancellation48Hours: 'Free cancellation (48h)',
  Flexible24Hours: 'Flexible (24h)',
  NonRefundable: 'Non-refundable',
};
