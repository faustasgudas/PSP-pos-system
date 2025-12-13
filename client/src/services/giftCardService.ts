import { apiRequest } from '../config/api';
import type { GiftCardResponse, CreateGiftCardRequest } from '../types/api';

export const getGiftCards = async (): Promise<GiftCardResponse[]> => {
    return apiRequest<GiftCardResponse[]>('/api/gift-cards', {
        method: 'GET',
    });
};

export const createGiftCard = async (data: CreateGiftCardRequest): Promise<GiftCardResponse> => {
    return apiRequest<GiftCardResponse>('/api/gift-cards', {
        method: 'POST',
        body: JSON.stringify(data),
    });
};
