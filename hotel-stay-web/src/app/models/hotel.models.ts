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

export interface HotelRoomOption {
  id: string;
  provider: string;
  destination: string;
  roomType: RoomType;
  perNightRate: number;
  totalPrice: number;
  nights: number;
  cancellationPolicy: CancellationPolicy;
  amenities: string[];
  starRating: number;
}

export interface ReserveRoomRequest {
  roomId: string;
  provider: string;
  destination: string;
  checkIn: string;
  checkOut: string;
  roomType: RoomType;
  totalPrice: number;
  guestName: string;
  documentType: DocumentType;
  documentNumber: string;
  cancellationPolicy: CancellationPolicy;
}

export interface ReservationResponse {
  reference: string;
  guestName: string;
  provider: string;
  destination: string;
  checkIn: string;
  checkOut: string;
  roomType: RoomType;
  totalPrice: number;
  cancellationPolicy: CancellationPolicy;
  documentType: DocumentType;
  documentNumber: string;
}

// Human-friendly labels for cancellation policies.
export const CANCELLATION_POLICY_LABELS: Record<CancellationPolicy, string> = {
  FreeCancellation48Hours: 'Free cancellation (48h)',
  Flexible24Hours: 'Flexible (24h)',
  NonRefundable: 'Non-refundable',
};
