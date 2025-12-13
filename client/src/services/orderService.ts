import { apiRequest } from '../config/api';
import type { OrderSummaryResponse } from '../types/api';

export const getOrders = async (params?: {
    status?: string;
    from?: string;
    to?: string;
}): Promise<OrderSummaryResponse[]> => {
    const queryParams = new URLSearchParams();
    if (params?.status) queryParams.append('status', params.status);
    if (params?.from) queryParams.append('from', params.from);
    if (params?.to) queryParams.append('to', params.to);

    const query = queryParams.toString();
    const endpoint = `/api/orders${query ? `?${query}` : ''}`;
    
    return apiRequest<OrderSummaryResponse[]>(endpoint, {
        method: 'GET',
    });
};

export const getMyOrders = async (): Promise<OrderSummaryResponse[]> => {
    return apiRequest<OrderSummaryResponse[]>('/api/orders/mine', {
        method: 'GET',
    });
};
