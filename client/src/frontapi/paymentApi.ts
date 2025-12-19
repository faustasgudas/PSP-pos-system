const API_URL = "http://localhost:5269/api";

async function readErrorMessage(res: Response): Promise<string> {
    const contentType = res.headers.get("content-type") ?? "";

    try {
        if (contentType.includes("application/json")) {
            const data: any = await res.json();

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

function authHeaders() {
    return {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
    };
}

export type PaymentResponse = {
    paymentId: number;
    paidByGiftCard: number;
    remainingForStripe: number;
    stripeUrl?: string | null;
    stripeSessionId?: string | null;
};

export type PaymentHistoryItem = {
    paymentId: number;
    amountCents: number;
    tipCents: number;
    currency: string;
    method: string;
    createdAt: string;
    completedAt: string | null;
    status: string;
    stripeSessionId: string | null;
    giftCardId: number | null;
    employeeId: number | null;
    businessId: number;
    orderId: number;
    giftCardPlannedCents: number;
};


export async function createPayment(
    orderId: number,
    opts?: {
        currency?: string;
        giftCardCode?: string;
        giftCardAmountCents?: number | null;
    }
): Promise<PaymentResponse> {
    const res = await fetch(`${API_URL}/payments`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({
            orderId,
            currency: opts?.currency ?? "EUR",
            giftCardCode: opts?.giftCardCode ?? null,
            giftCardAmountCents: opts?.giftCardAmountCents ?? null,
        }),
    });

    if (!res.ok) {
        throw new Error(await readErrorMessage(res));
    }

    return res.json();
}

export async function listPaymentsForBusiness(): Promise<PaymentHistoryItem[]> {
    const res = await fetch(`${API_URL}/payments/history`, {
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function listPaymentsForOrder(orderId: number): Promise<PaymentHistoryItem[]> {
    const res = await fetch(`${API_URL}/payments/orders/${orderId}`, {
        headers: authHeaders(),
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}

export async function refundPayment(paymentId: number): Promise<{ message: string }> {
    const res = await fetch(`${API_URL}/payments/${paymentId}/refund`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({})
    });
    if (!res.ok) throw new Error(await readErrorMessage(res));
    return res.json();
}
