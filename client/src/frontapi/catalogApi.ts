const API_URL = "http://localhost:5269/api";

function authHeaders() {
    const token = localStorage.getItem("token");
    return {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
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

/* 🔹 THIS WAS MISSING OR NOT EXPORTED BEFORE */
export type CatalogItem = {
    catalogItemId: number;
    name: string;
    status: "Draft" | "Active" | "Archived" | string;
    basePrice: number;
    code: string;
    type: string;
    businessId: number;
    taxClass: string;
    defaultDurationMin?: number; // present on detail response; optional on list
};

/* 🔹 ONLY ACTIVE SERVICES */
export async function getActiveServices(
    businessId: number
): Promise<CatalogItem[]> {
    const res = await fetch(
        `${API_URL}/businesses/${businessId}/catalog-items?type=Service`,
        {
            headers: authHeaders(),
        }
    );

    if (!res.ok) {
        throw new Error(await readErrorMessage(res));
    }

    const data: CatalogItem[] = await res.json();

    return data.filter((s) => s.status === "Active");
}

export async function listCatalogItems(
    businessId: number,
    params?: { type?: string; status?: string; code?: string }
): Promise<CatalogItem[]> {
    const qs = new URLSearchParams();
    if (params?.type) qs.set("type", params.type);
    if (params?.status) qs.set("status", params.status);
    if (params?.code) qs.set("code", params.code);

    const url = qs.toString()
        ? `${API_URL}/businesses/${businessId}/catalog-items?${qs}`
        : `${API_URL}/businesses/${businessId}/catalog-items`;

    const res = await fetch(url, { headers: authHeaders() });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function getCatalogItemById(businessId: number, catalogItemId: number): Promise<CatalogItem> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/catalog-items/${catalogItemId}`, {
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function createCatalogItem(
    businessId: number,
    body: {
        name: string;
        code: string;
        type: "Product" | "Service" | string;
        basePrice: number;
        status?: string;
        defaultDurationMin?: number;
        taxClass: string;
    }
): Promise<CatalogItem> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/catalog-items`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function updateCatalogItem(
    businessId: number,
    catalogItemId: number,
    body: {
        name?: string;
        code?: string;
        type?: string;
        basePrice?: number;
        status?: string;
        defaultDurationMin?: number;
        taxClass?: string;
    }
): Promise<CatalogItem> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/catalog-items/${catalogItemId}`, {
        method: "PUT",
        headers: authHeaders(),
        body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function archiveCatalogItem(businessId: number, catalogItemId: number): Promise<void> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/catalog-items/${catalogItemId}/archive`, {
        method: "POST",
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
}
