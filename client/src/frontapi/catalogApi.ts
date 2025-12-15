const API_URL = "https://localhost:44317/api";

function authHeaders() {
    const token = localStorage.getItem("token");
    return {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
    };
}

/* 🔹 THIS WAS MISSING OR NOT EXPORTED BEFORE */
export type CatalogItem = {
    catalogItemId: number;
    name: string;
    status: "Active" | "Archived";
    basePrice: number;  // ← Changed from object to number
    code: string;
    type: string;
    businessId: number;
    taxClass: string;
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
        throw new Error("Failed to fetch services");
    }

    const data: CatalogItem[] = await res.json();

    return data.filter((s) => s.status === "Active");
}
