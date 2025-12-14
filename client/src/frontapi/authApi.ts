// client/src/frontapi/authApi.ts

export type LoginResponse = {
    token: string;
    businessType: string;
};

const API_BASE = "/api";

async function readErrorMessage(res: Response): Promise<string> {
    // Backend might return JSON or plain text. Handle both safely.
    const contentType = res.headers.get("content-type") ?? "";
    try {
        if (contentType.includes("application/json")) {
            const data = await res.json();
            // common patterns: { error: "..."} or validation errors
            if (typeof data?.error === "string") return data.error;
            return JSON.stringify(data);
        }
        return await res.text();
    } catch {
        return "Request failed.";
    }
}

export async function login(email: string, password: string): Promise<LoginResponse> {
    const res = await fetch(`${API_BASE}/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
    });

    if (!res.ok) {
        const msg = await readErrorMessage(res);
        throw new Error(msg || "Invalid email or password");
    }

    const data = (await res.json()) as LoginResponse;

    if (!data?.token) {
        throw new Error("Login response missing token");
    }

    return data;
}

export function logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("businessId");
    localStorage.removeItem("employeeId");
    localStorage.removeItem("role");
    localStorage.removeItem("businessType");
}
