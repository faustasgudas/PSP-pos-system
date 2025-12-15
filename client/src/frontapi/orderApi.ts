const API_URL = "http://localhost:5269/api";

function authHeaders() {
    return {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("token")}`,
    };
}

/**
 * Creates an empty order.
 * Backend requires employeeId in body.
 */
export async function createOrder(employeeId: number) {
    const res = await fetch(`${API_URL}/orders`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({
            employeeId,
            reservationId: null,
            tableOrArea: null,
        }),
    });

    if (!res.ok) {
        throw new Error(await res.text());
    }

    return res.json(); // OrderDetailResponse (contains orderId)
}
