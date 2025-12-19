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

        const text = await res.text();
        return text || `HTTP ${res.status}`;
    } catch {
        return `HTTP ${res.status}`;
    }
}

export interface Business {
    businessId: number;
    name: string;
    address: string;
    phone: string;
    email: string;
    countryCode: string;
    priceIncludesTax: boolean;
    businessStatus: string;
    businessType: string;
}

export async function getBusiness(businessId: number): Promise<Business> {
    const res = await fetch(`${API_URL}/businesses/${businessId}`, {
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function updateBusiness(
    businessId: number,
    data: {
        name: string;
        address: string;
        phone: string;
        email: string;
        countryCode: string;
        priceIncludesTax: boolean;
        businessType: string;
    }
): Promise<Business> {
    const res = await fetch(`${API_URL}/businesses/${businessId}`, {
        method: "PUT",
        headers: authHeaders(),
        body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}
