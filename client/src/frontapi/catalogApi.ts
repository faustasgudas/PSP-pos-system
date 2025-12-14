const API_URL = "https://localhost:44317/api";

function authHeaders() {
    const token = localStorage.getItem("token");
    return {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
    };
}

export async function deactivateService(
    businessId: number,
    catalogItemId: number
) {
    const res = await fetch(
        `${API_URL}/businesses/${businessId}/catalog-items/${catalogItemId}/archive`,
        {
            method: "POST",
            headers: authHeaders(),
        }
    );

    if (!res.ok) throw new Error("Failed to deactivate service");
}

export async function updateService(
    businessId: number,
    catalogItemId: number,
    payload: { name: string; basePrice: number }
) {
    const res = await fetch(
        `${API_URL}/businesses/${businessId}/catalog-items/${catalogItemId}`,
        {
            method: "PUT",
            headers: authHeaders(),
            body: JSON.stringify(payload),
        }
    );

    if (!res.ok) throw new Error("Failed to update service");

    return res.json();
}

export async function createService(
    businessId: number,
    payload: { name: string; basePrice: number }
) {
    const res = await fetch(
        `${API_URL}/businesses/${businessId}/catalog-items`,
        {
            method: "POST",
            headers: authHeaders(),
            body: JSON.stringify({
                ...payload,
                type: "Service",
                status: "Active",
                code: payload.name.toUpperCase().replace(/\s+/g, "_"),
                taxClass: "Service",
                defaultDurationMin: 0,
            }),
        }
    );

    if (!res.ok) throw new Error("Failed to create service");

    return res.json();
}
