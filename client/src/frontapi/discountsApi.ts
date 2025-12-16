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

export type DiscountType = "Percent" | "Amount" | string;
export type DiscountScope = "Order" | "Line" | string;

export type DiscountSummary = {
    discountId: number;
    businessId: number;
    code: string;
    type: DiscountType;
    scope: DiscountScope;
    value: number;
    startsAt: string; // ISO
    endsAt: string; // ISO
    status: string;
};

export type DiscountEligibility = {
    discountId: number;
    catalogItemId: number;
};

export type DiscountDetail = DiscountSummary & {
    eligibilities: DiscountEligibility[];
};

export type CreateDiscountRequest = {
    code: string;
    type: "Percent" | "Amount";
    scope: "Order" | "Line";
    value: number;
    startsAt: string; // ISO
    endsAt: string; // ISO
    status?: string | null;
};

export type UpdateDiscountRequest = Partial<CreateDiscountRequest>;

export async function listDiscounts(): Promise<DiscountSummary[]> {
    const res = await fetch(`${API_URL}/discounts`, { headers: authHeaders() });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function getDiscount(discountId: number): Promise<DiscountDetail> {
    const res = await fetch(`${API_URL}/discounts/${discountId}`, { headers: authHeaders() });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function createDiscount(body: CreateDiscountRequest): Promise<DiscountDetail> {
    const res = await fetch(`${API_URL}/discounts`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function updateDiscount(discountId: number, body: UpdateDiscountRequest): Promise<DiscountDetail> {
    const res = await fetch(`${API_URL}/discounts/${discountId}`, {
        method: "PUT",
        headers: authHeaders(),
        body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function deleteDiscount(discountId: number): Promise<void> {
    const res = await fetch(`${API_URL}/discounts/${discountId}`, {
        method: "DELETE",
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
}


export async function addEligibility(
    discountId: number,
    catalogItemId: number
) {
    const res = await fetch(
        `${API_URL}/discounts/${discountId}/eligibilities`,
        {
            method: "POST",
            headers: authHeaders(),
            body: JSON.stringify({ catalogItemId }),
        }
    );

    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}
