export function getUserFromToken(): { email: string; role: string } | null {
    const token = localStorage.getItem("token");
    if (!token) return null;

    try {
        const payload = JSON.parse(atob(token.split(".")[1]));

        return {
            email: payload.email,
            role: payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"],
        };
    } catch {
        return null;
    }
}
