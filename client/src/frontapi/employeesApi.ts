const API_URL = "http://localhost:5269/api";

function authHeaders() {
    const token = localStorage.getItem("token");
    if (!token) throw new Error("No token");

    return {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
    };
}

export async function fetchEmployees(businessId: number) {
    const res = await fetch(
        `${API_URL}/businesses/${businessId}/employees`,
        { headers: authHeaders() }
    );

    if (!res.ok) throw new Error("Failed to fetch employees");
    return res.json();
}

export async function createEmployee(businessId: number, body: any) {
    const res = await fetch(
        `${API_URL}/businesses/${businessId}/employees`,
        {
            method: "POST",
            headers: authHeaders(),
            body: JSON.stringify(body),
        }
    );

    if (!res.ok) throw new Error("Failed to create employee");
    return res.json();
}

export async function deactivateEmployee(businessId: number, employeeId: number) {
    const res = await fetch(
        `${API_URL}/businesses/${businessId}/employees/${employeeId}/deactivate`,
        {
            method: "POST",
            headers: authHeaders(),
        }
    );

    if (!res.ok) throw new Error("Failed to deactivate employee");
}
