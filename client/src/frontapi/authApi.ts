// client/src/frontapi/authApi.ts

export type LoginResponse = {
    token: string;
    businessType: string;
};

const API_BASE = "http://localhost:5269";

async function readErrorMessage(res: Response): Promise<string> {
    const contentType = res.headers.get("content-type") ?? "";

    try {
        if (contentType.includes("application/json")) {
            const data: any = await res.json();

            if (typeof data?.error === "string") return data.error;
            if (typeof data?.message === "string") return data.message;

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

export async function login(email: string, password: string): Promise<LoginResponse> {
    try {
        const res = await fetch(`${API_BASE}/api/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email: email.trim(), password }),
        });

        if (!res.ok) {
            const msg = await readErrorMessage(res);
            throw new Error(msg || "Login failed");
        }

        const data = (await res.json()) as LoginResponse;

        if (!data?.token) throw new Error("Login response missing token");
        if (!data?.businessType) throw new Error("Login response missing businessType");

        return data;
    } catch (err: any) {
        // fetch() network error (backend down / wrong port / CORS)
        if (err?.name === "TypeError") {
            throw new Error("Cannot reach backend (is API running on http://localhost:5269 ?)");
        }
        throw err;
    }
}

export function logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("businessId");
    localStorage.removeItem("employeeId");
    localStorage.removeItem("role");
    localStorage.removeItem("businessType");
}
