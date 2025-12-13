import { apiRequest } from '../config/api';
import type {
    ReservationSummaryResponse,
    ReservationDetailResponse,
    CreateReservationRequest,
    UpdateReservationRequest,
} from '../types/api';

export const getReservations = async (
    businessId: number,
    params?: {
        status?: string;
        dateFrom?: string;
        dateTo?: string;
        employeeId?: number;
        catalogItemId?: number;
    }
): Promise<ReservationSummaryResponse[]> => {
    const queryParams = new URLSearchParams();
    if (params?.status) queryParams.append('status', params.status);
    if (params?.dateFrom) queryParams.append('dateFrom', params.dateFrom);
    if (params?.dateTo) queryParams.append('dateTo', params.dateTo);
    if (params?.employeeId) queryParams.append('employeeId', params.employeeId.toString());
    if (params?.catalogItemId) queryParams.append('catalogItemId', params.catalogItemId.toString());

    const query = queryParams.toString();
    const endpoint = `/api/businesses/${businessId}/reservations${query ? `?${query}` : ''}`;
    
    return apiRequest<ReservationSummaryResponse[]>(endpoint, {
        method: 'GET',
    });
};

export const getReservation = async (
    businessId: number,
    reservationId: number
): Promise<ReservationDetailResponse> => {
    return apiRequest<ReservationDetailResponse>(
        `/api/businesses/${businessId}/reservations/${reservationId}`,
        {
            method: 'GET',
        }
    );
};

export const createReservation = async (
    businessId: number,
    data: CreateReservationRequest
): Promise<ReservationDetailResponse> => {
    return apiRequest<ReservationDetailResponse>(
        `/api/businesses/${businessId}/reservations`,
        {
            method: 'POST',
            body: JSON.stringify(data),
        }
    );
};

export const updateReservation = async (
    businessId: number,
    reservationId: number,
    data: UpdateReservationRequest
): Promise<ReservationDetailResponse> => {
    return apiRequest<ReservationDetailResponse>(
        `/api/businesses/${businessId}/reservations/${reservationId}`,
        {
            method: 'PATCH',
            body: JSON.stringify(data),
        }
    );
};

export const cancelReservation = async (
    businessId: number,
    reservationId: number
): Promise<ReservationDetailResponse> => {
    return apiRequest<ReservationDetailResponse>(
        `/api/businesses/${businessId}/reservations/${reservationId}`,
        {
            method: 'DELETE',
        }
    );
};
