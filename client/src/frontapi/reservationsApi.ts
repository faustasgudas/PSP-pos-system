const API_URL = "http://localhost:5269/api";

function authHeaders() {
    return {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
    };
}

async function readErrorMessage(res: Response): Promise<string> {
    const contentType = res.headers.get("content-type") ?? "";

    try {
        if (contentType.includes("application/json")) {
            const data: any = await res.json();
            if (typeof data?.error === "string") return data.error;
            if (typeof data?.message === "string") return data.message;
            if (typeof data?.details === "string") return data.details;

            if (data?.errors && typeof data.errors === "object") {
                const firstKey = Object.keys(data.errors)[0];
                const firstVal = data.errors[firstKey];
                if (Array.isArray(firstVal) && firstVal[0]) return String(firstVal[0]);
            }

            return JSON.stringify(data);
        }

        const txt = await res.text();
        return txt || `HTTP ${res.status}`;
    } catch {
        return `HTTP ${res.status}`;
    }
}

export type ReservationStatus = "Booked" | "Cancelled" | "Completed" | string;

export type ReservationSummary = {
    reservationId: number;
    businessId: number;
    employeeId: number;
    catalogItemId: number;
    appointmentStart: string;
    plannedDurationMin: number;
    status: ReservationStatus;
};

export type ReservationDetail = ReservationSummary & {
    bookedAt: string;
    notes: string | null;
    tableOrArea: string | null;
    orderId: number | null;
};

export async function listReservations(
    businessId: number,
    params?: {
        status?: string;
        dateFrom?: string; // ISO
        dateTo?: string; // ISO
        employeeId?: number;
        catalogItemId?: number;
    }
): Promise<ReservationSummary[]> {
    const qs = new URLSearchParams();
    if (params?.status) qs.set("status", params.status);
    if (params?.dateFrom) qs.set("dateFrom", params.dateFrom);
    if (params?.dateTo) qs.set("dateTo", params.dateTo);
    if (params?.employeeId) qs.set("employeeId", String(params.employeeId));
    if (params?.catalogItemId) qs.set("catalogItemId", String(params.catalogItemId));

    const url = qs.toString()
        ? `${API_URL}/businesses/${businessId}/reservations?${qs}`
        : `${API_URL}/businesses/${businessId}/reservations`;

    const res = await fetch(url, { headers: authHeaders() });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function getReservation(businessId: number, reservationId: number): Promise<ReservationDetail> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/reservations/${reservationId}`, {
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function createReservation(
    businessId: number,
    body: {
        catalogItemId: number;
        employeeId?: number | null;
        appointmentStart: string; // ISO
        notes?: string | null;
        tableOrArea?: string | null;
    }
): Promise<ReservationDetail> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/reservations`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function updateReservation(
    businessId: number,
    reservationId: number,
    body: {
        employeeId?: number | null;
        catalogItemId?: number | null;
        appointmentStart?: string | null;
        notes?: string | null;
        tableOrArea?: string | null;
        status?: ReservationStatus | null;
    }
): Promise<ReservationDetail> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/reservations/${reservationId}`, {
        method: "PATCH",
        headers: authHeaders(),
        body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function cancelReservation(businessId: number, reservationId: number): Promise<ReservationDetail> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/reservations/${reservationId}`, {
        method: "DELETE",
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}


