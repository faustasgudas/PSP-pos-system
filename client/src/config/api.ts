// API Configuration
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5269';

export const getAuthToken = (): string | null => {
    return localStorage.getItem('authToken');
};

export const setAuthToken = (token: string): void => {
    localStorage.setItem('authToken', token);
};

export const removeAuthToken = (): void => {
    localStorage.removeItem('authToken');
};

export const getBusinessId = (): number | null => {
    const id = localStorage.getItem('businessId');
    return id ? parseInt(id, 10) : null;
};

export const setBusinessId = (id: number): void => {
    localStorage.setItem('businessId', id.toString());
};

export const removeBusinessId = (): void => {
    localStorage.removeItem('businessId');
};

// API Client with authentication
export async function apiRequest<T>(
    endpoint: string,
    options: RequestInit = {}
): Promise<T> {
    const token = getAuthToken();
    const url = `${API_BASE_URL}${endpoint}`;

    const headers: HeadersInit = {
        'Content-Type': 'application/json',
        ...options.headers,
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(url, {
        ...options,
        headers,
    });

    if (!response.ok) {
        // Handle 401 Unauthorized - token expired or invalid
        if (response.status === 401) {
            // Clear auth data
            removeAuthToken();
            removeBusinessId();
            localStorage.removeItem('businessType');
            // Redirect to login by throwing a specific error
            throw new Error('Session expired. Please login again.');
        }

        let errorMessage = `HTTP error! status: ${response.status}`;
        try {
            const errorData = await response.json();
            errorMessage = errorData.error || errorData.message || errorMessage;
        } catch {
            // If response is not JSON, use status text
            errorMessage = response.statusText || errorMessage;
        }
        throw new Error(errorMessage);
    }

    // Handle 204 No Content
    if (response.status === 204) {
        return {} as T;
    }

    return response.json();
}
