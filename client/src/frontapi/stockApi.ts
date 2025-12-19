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

export type StockItemSummary = {
    stockItemId: number;
    catalogItemId: number;
    unit: string;
    qtyOnHand: number;
};

export type StockItemDetail = StockItemSummary & {
    averageUnitCost: number;
};

export type StockMovement = {
    stockMovementId: number;
    stockItemId: number;
    type: string;
    delta: number;
    unitCostSnapshot: number | null;
    orderLineId: number | null;
    at: string;
    note: string | null;
};

export async function listStockItems(businessId: number, catalogItemId?: number): Promise<StockItemSummary[]> {
    const qs = new URLSearchParams();
    if (catalogItemId) qs.set("catalogItemId", String(catalogItemId));

    const url = qs.toString()
        ? `${API_URL}/businesses/${businessId}/stock-items?${qs}`
        : `${API_URL}/businesses/${businessId}/stock-items`;

    const res = await fetch(url, { headers: authHeaders() });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function getStockItem(businessId: number, stockItemId: number): Promise<StockItemDetail> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/stock-items/${stockItemId}`, {
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function createStockItem(
    businessId: number,
    body: {
        catalogItemId: number;
        unit: string;
        initialQtyOnHand?: number | null;
        initialAverageUnitCost?: number | null;
    }
): Promise<StockItemDetail> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/stock-items`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function updateStockItemUnit(
    businessId: number,
    stockItemId: number,
    unit: string
): Promise<StockItemDetail> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/stock-items/${stockItemId}`, {
        method: "PUT",
        headers: authHeaders(),
        body: JSON.stringify({ unit }),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function listStockMovements(
    businessId: number,
    stockItemId: number,
    params?: { type?: string; dateFrom?: string; dateTo?: string }
): Promise<StockMovement[]> {
    const qs = new URLSearchParams();
    if (params?.type) qs.set("type", params.type);
    if (params?.dateFrom) qs.set("dateFrom", params.dateFrom);
    if (params?.dateTo) qs.set("dateTo", params.dateTo);

    const url = qs.toString()
        ? `${API_URL}/businesses/${businessId}/stock-items/${stockItemId}/movements?${qs}`
        : `${API_URL}/businesses/${businessId}/stock-items/${stockItemId}/movements`;

    const res = await fetch(url, { headers: authHeaders() });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function createStockMovement(
    businessId: number,
    stockItemId: number,
    body: {
        type: string;
        delta: number;
        unitCostSnapshot?: number | null;
        at?: string | null;
        note?: string | null;
    }
): Promise<StockMovement> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/stock-items/${stockItemId}/movements`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}


