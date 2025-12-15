const API_URL = "https://localhost:44317/api";

export async function createPayment(
    orderId: number,
    giftCardCode?: string
) {
    const res = await fetch(`${API_URL}/payments`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${localStorage.getItem("token")}`,
        },
        body: JSON.stringify({
            orderId,
            currency: "EUR",
            giftCardCode: giftCardCode ?? null,
        }),
    });

    if (!res.ok) {
        const text = await res.text();
        throw new Error(`Payment failed: ${text}`);
    }

    return res.json();
}
