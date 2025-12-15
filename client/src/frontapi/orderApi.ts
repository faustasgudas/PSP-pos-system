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

            // Our API sometimes returns { message, details } or { error }
            if (typeof data?.error === "string") return data.error;
            if (typeof data?.message === "string") return data.message;
            if (typeof data?.details === "string") return data.details;

            // ASP.NET ValidationProblemDetails: { errors: { field: ["msg"] } }
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

export type OrderStatus = "Open" | "Closed" | "Cancelled" | string;

export type OrderSummary = {
    orderId: number;
    businessId: number;
    employeeId: number;
    reservationId: number | null;
    status: OrderStatus;
    tableOrArea: string | null;
    createdAt: string;
    closedAt: string | null;
    tipAmount: number;
    discountId: number | null;
};

export type OrderLine = {
    orderLineId: number;
    orderId: number;
    businessId: number;
    catalogItemId: number;
    discountId: number | null;
    qty: number;

    itemNameSnapshot: string;
    unitPriceSnapshot: number;

    catalogTypeSnapshot: string;
    unitDiscountSnapshot: string | null;
    taxClassSnapshot: string;
    taxRateSnapshotPct: number;

    performedAt: string;
    performedByEmployeeId: number | null;
};

export type OrderDetail = {
    orderId: number;
    businessId: number;
    employeeId: number;
    reservationId: number | null;
    status: OrderStatus;
    tableOrArea: string | null;
    createdAt: string;
    closedAt: string | null;
    tipAmount: number;
    discountId: number | null;
    orderDiscountSnapshot: string | null;
    lines: OrderLine[];
};

export type MoveOrderLinesRequest = {
    targetOrderId: number;
    lines: Array<{ orderLineId: number; qty: number }>;
};

/**
 * Creates an empty order.
 * Backend requires employeeId in body.
 */
export async function createOrder(
    employeeId: number,
    opts?: { reservationId?: number | null; tableOrArea?: string | null }
): Promise<OrderDetail> {
    const res = await fetch(`${API_URL}/orders`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({
            employeeId,
            reservationId: opts?.reservationId ?? null,
            tableOrArea: opts?.tableOrArea ?? null,
        }),
    });

    if (!res.ok) {
        throw new Error(await readErrorMessage(res));
    }

    return res.json(); // OrderDetailResponse (contains orderId)
}

export async function listMyOrders(): Promise<OrderSummary[]> {
    const res = await fetch(`${API_URL}/orders/mine`, { headers: authHeaders() });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function listAllOrders(params?: {
    status?: string;
    from?: string; // ISO
    to?: string; // ISO
}): Promise<OrderSummary[]> {
    const qs = new URLSearchParams();
    if (params?.status) qs.set("status", params.status);
    if (params?.from) qs.set("from", params.from);
    if (params?.to) qs.set("to", params.to);

    const url = qs.toString() ? `${API_URL}/orders?${qs}` : `${API_URL}/orders`;
    const res = await fetch(url, { headers: authHeaders() });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function getOrder(orderId: number): Promise<OrderDetail> {
    const res = await fetch(`${API_URL}/orders/${orderId}`, { headers: authHeaders() });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function addOrderLine(orderId: number, catalogItemId: number, qty: number): Promise<OrderLine> {
    const res = await fetch(`${API_URL}/orders/${orderId}/lines`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ catalogItemId, qty }),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function updateOrderLine(
    orderId: number,
    orderLineId: number,
    qty: number,
    discountId?: number | null
): Promise<OrderLine> {
    const res = await fetch(`${API_URL}/orders/${orderId}/lines/${orderLineId}`, {
        method: "PUT",
        headers: authHeaders(),
        body: JSON.stringify({ qty, discountId: discountId ?? null }),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function removeOrderLine(orderId: number, orderLineId: number): Promise<void> {
    const res = await fetch(`${API_URL}/orders/${orderId}/lines/${orderLineId}`, {
        method: "DELETE",
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
}

export async function moveOrderLines(fromOrderId: number, body: MoveOrderLinesRequest): Promise<void> {
    const res = await fetch(`${API_URL}/orders/${fromOrderId}/move-lines`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
}

export async function closeOrder(orderId: number): Promise<OrderDetail> {
    const res = await fetch(`${API_URL}/orders/${orderId}/close`, {
        method: "POST",
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function cancelOrder(orderId: number, employeeId: number, reason?: string): Promise<OrderDetail> {
    const res = await fetch(`${API_URL}/orders/${orderId}/cancel`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ employeeId, reason: reason ?? null }),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function refundOrder(orderId: number, employeeId: number, reason?: string): Promise<OrderDetail> {
    const res = await fetch(`${API_URL}/orders/${orderId}/refund`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ employeeId, reason: reason ?? null }),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function reopenOrder(orderId: number): Promise<OrderDetail> {
    const res = await fetch(`${API_URL}/orders/${orderId}/reopen`, {
        method: "POST",
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}
