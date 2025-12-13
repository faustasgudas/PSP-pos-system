import { apiRequest } from '../config/api';
import type { StockItemSummaryResponse } from '../types/api';

export const getStockItems = async (): Promise<StockItemSummaryResponse[]> => {
    return apiRequest<StockItemSummaryResponse[]>('/api/stock-items', {
        method: 'GET',
    });
};
