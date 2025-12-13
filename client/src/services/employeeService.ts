import { apiRequest } from '../config/api';
import type {
    EmployeeSummaryResponse,
    CreateEmployeeRequest,
    UpdateEmployeeRequest,
    DeactivateEmployeeRequest,
} from '../types/api';

export const getEmployees = async (
    businessId: number,
    params?: {
        role?: string;
        status?: string;
    }
): Promise<EmployeeSummaryResponse[]> => {
    const queryParams = new URLSearchParams();
    if (params?.role) queryParams.append('role', params.role);
    if (params?.status) queryParams.append('status', params.status);

    const query = queryParams.toString();
    const endpoint = `/api/businesses/${businessId}/employees${query ? `?${query}` : ''}`;
    
    return apiRequest<EmployeeSummaryResponse[]>(endpoint, {
        method: 'GET',
    });
};

export const createEmployee = async (
    businessId: number,
    data: CreateEmployeeRequest
): Promise<EmployeeSummaryResponse> => {
    return apiRequest<EmployeeSummaryResponse>(
        `/api/businesses/${businessId}/employees`,
        {
            method: 'POST',
            body: JSON.stringify(data),
        }
    );
};

export const updateEmployee = async (
    businessId: number,
    employeeId: number,
    data: UpdateEmployeeRequest
): Promise<EmployeeSummaryResponse> => {
    return apiRequest<EmployeeSummaryResponse>(
        `/api/businesses/${businessId}/employees/${employeeId}`,
        {
            method: 'PUT',
            body: JSON.stringify(data),
        }
    );
};

export const deactivateEmployee = async (
    businessId: number,
    employeeId: number,
    data: DeactivateEmployeeRequest
): Promise<EmployeeSummaryResponse> => {
    return apiRequest<EmployeeSummaryResponse>(
        `/api/businesses/${businessId}/employees/${employeeId}/deactivate`,
        {
            method: 'POST',
            body: JSON.stringify(data),
        }
    );
};
