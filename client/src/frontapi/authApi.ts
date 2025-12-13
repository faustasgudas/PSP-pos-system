import axios from "axios";

const API_URL = "https://localhost:44317/api";

export interface LoginResponse {
    token: string;
    businessType: string;
}

export async function login(email: string, password: string): Promise<LoginResponse> {
    const response = await fetch(`${API_URL}/auth/login`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ email, password }),
    });

    if (!response.ok) {
        throw new Error("Login failed");
    }

    return response.json();
}
