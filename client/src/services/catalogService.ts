import { apiRequest } from '../config/api';
import type {
    CatalogItemSummaryResponse,
    CatalogItemDetailResponse,
    CreateCatalogItemRequest,
    UpdateCatalogItemRequest,
} from '../types/api';

export const getCatalogItems = async (
    businessId: number,
    params?: {
        type?: string;
        status?: string;
        code?: string;
    }
): Promise<CatalogItemSummaryResponse[]> => {
    const queryParams = new URLSearchParams();
    if (params?.type) queryParams.append('type', params.type);
    if (params?.status) queryParams.append('status', params.status);
    if (params?.code) queryParams.append('code', params.code);

    const query = queryParams.toString();
    const endpoint = `/api/businesses/${businessId}/catalog-items${query ? `?${query}` : ''}`;
    
    return apiRequest<CatalogItemSummaryResponse[]>(endpoint, {
        method: 'GET',
    });
};

export const getCatalogItem = async (
    businessId: number,
    catalogItemId: number
): Promise<CatalogItemDetailResponse> => {
    return apiRequest<CatalogItemDetailResponse>(
        `/api/businesses/${businessId}/catalog-items/${catalogItemId}`,
        {
            method: 'GET',
        }
    );
};

export const createCatalogItem = async (
    businessId: number,
    data: CreateCatalogItemRequest
): Promise<CatalogItemDetailResponse> => {
    return apiRequest<CatalogItemDetailResponse>(
        `/api/businesses/${businessId}/catalog-items`,
        {
            method: 'POST',
            body: JSON.stringify(data),
        }
    );
};

export const updateCatalogItem = async (
    businessId: number,
    catalogItemId: number,
    data: UpdateCatalogItemRequest
): Promise<CatalogItemDetailResponse> => {
    return apiRequest<CatalogItemDetailResponse>(
        `/api/businesses/${businessId}/catalog-items/${catalogItemId}`,
        {
            method: 'PUT',
            body: JSON.stringify(data),
        }
    );
};

export const archiveCatalogItem = async (
    businessId: number,
    catalogItemId: number
): Promise<void> => {
    return apiRequest<void>(
        `/api/businesses/${businessId}/catalog-items/${catalogItemId}/archive`,
        {
            method: 'POST',
        }
    );
};
