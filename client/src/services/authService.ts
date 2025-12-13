import { apiRequest, getAuthToken } from '../config/api';
import type { LoginRequest, LoginResponse, RegisterBusinessRequest, RegisterBusinessResponse, BusinessResponse } from '../types/api';

export const login = async (email: string, password: string): Promise<LoginResponse> => {
    const request: LoginRequest = { email, password };
    return apiRequest<LoginResponse>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify(request),
    });
};

export const registerBusiness = async (data: RegisterBusinessRequest): Promise<RegisterBusinessResponse> => {
    return apiRequest<RegisterBusinessResponse>('/api/auth/register-business', {
        method: 'POST',
        body: JSON.stringify(data),
    });
};

export const getCurrentBusiness = async (): Promise<BusinessResponse[]> => {
    // Token is automatically included by apiRequest from localStorage
    return apiRequest<BusinessResponse[]>('/api/businesses', {
        method: 'GET',
    });
};
