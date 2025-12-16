const API_URL = "http://localhost:5269/api";

function authHeaders() {
    const token = localStorage.getItem("token");
    return {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token ?? ""}`,
    };
}

async function readErrorMessage(res: Response): Promise<string> {
    const contentType = res.headers.get("content-type") ?? "";

    try {
        if (contentType.includes("application/json")) {
            const data: any = await res.json();

            if (typeof data?.error === "string") return data.error;
            if (typeof data?.message === "string") return data.message;
            if (typeof data?.details === "string" && data.details) return data.details;

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

export type GiftCardResponse = {
    giftCardId: number;
    code: string;
    balance: number; // minor units (e.g. cents)
    status: string; // "Active" | "Inactive" | ...
    expiresAt: string | null;
    issuedAt: string; // ISO string
};

export type CreateGiftCardRequest = {
    code: string;
    balance: number; // minor units (e.g. cents)
    expiresAt?: string | null;
};

export type UpdateBalanceRequest = {
    amount: number; // minor units (e.g. cents)
};

export type RedeemRequest = {
    amount: number; // minor units (e.g. cents)
};

export type RedeemResponse = {
    charged: number;
    remainingBalance: number;
};

export async function listGiftCards(
    businessId: number,
    opts?: { status?: string; code?: string }
): Promise<GiftCardResponse[]> {
    const params = new URLSearchParams();
    if (opts?.status) params.set("status", opts.status);
    if (opts?.code) params.set("code", opts.code);

    const res = await fetch(
        `${API_URL}/businesses/${businessId}/giftcards${params.toString() ? `?${params.toString()}` : ""}`,
        { headers: authHeaders() }
    );

    if (!res.ok) {
        throw new Error(await readErrorMessage(res));
    }

    return res.json();
}

export async function getGiftCardById(
    businessId: number,
    id: number
): Promise<GiftCardResponse> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/giftcards/${id}`, {
        headers: authHeaders(),
    });

    if (!res.ok) {
        throw new Error(await readErrorMessage(res));
    }

    return res.json();
}

export async function getGiftCardByCode(
    businessId: number,
    code: string
): Promise<GiftCardResponse> {
    const safeCode = encodeURIComponent(code.trim());
    const res = await fetch(`${API_URL}/businesses/${businessId}/giftcards/code/${safeCode}`, {
        headers: authHeaders(),
    });

    if (!res.ok) {
        throw new Error(await readErrorMessage(res));
    }

    return res.json();
}

export async function createGiftCard(
    businessId: number,
    req: CreateGiftCardRequest
): Promise<GiftCardResponse> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/giftcards`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({
            code: req.code,
            balance: req.balance,
            expiresAt: req.expiresAt ?? null,
        }),
    });

    if (!res.ok) {
        throw new Error(await readErrorMessage(res));
    }

    return res.json();
}

export async function topUpGiftCard(
    businessId: number,
    id: number,
    amount: number
): Promise<void> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/giftcards/${id}/balance`, {
        method: "PATCH",
        headers: authHeaders(),
        body: JSON.stringify({ amount } satisfies UpdateBalanceRequest),
    });

    if (!res.ok) {
        throw new Error(await readErrorMessage(res));
    }
}

export async function redeemGiftCard(
    businessId: number,
    id: number,
    amount: number
): Promise<RedeemResponse> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/giftcards/${id}/transactions`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({ amount } satisfies RedeemRequest),
    });

    if (!res.ok) {
        throw new Error(await readErrorMessage(res));
    }

    return res.json();
}

export async function deactivateGiftCard(
    businessId: number,
    id: number
): Promise<void> {
    const res = await fetch(`${API_URL}/businesses/${businessId}/giftcards/${id}/deactivate`, {
        method: "POST",
        headers: authHeaders(),
    });

    if (!res.ok) {
        throw new Error(await readErrorMessage(res));
    }
}


