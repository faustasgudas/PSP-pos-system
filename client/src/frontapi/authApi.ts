const API_URL = "https://localhost:44317/api";

function parseJwt(token: string) {
    const base64Url = token.split(".")[1];
    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    return JSON.parse(atob(base64));
}

export async function login(email: string, password: string) {
    const res = await fetch(`${API_URL}/auth/login`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ email, password }),
    });

    if (!res.ok) {
        throw new Error("Invalid email or password");
    }

    const data = await res.json();
    const decoded = parseJwt(data.token);

    // ✅ store EVERYTHING auth-related here
    localStorage.setItem("token", data.token);
    localStorage.setItem("businessType", data.businessType);
    localStorage.setItem("businessId", decoded.businessId);
    localStorage.setItem("employeeId", decoded.employeeId);
    localStorage.setItem("role", decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]);

    return data;
}
