import { useEffect, useMemo, useState } from "react";
import "./BeautyPayments.css";
import { listPaymentsForBusiness, refundPayment, type PaymentHistoryItem } from "../../../frontapi/paymentApi";
import { getUserFromToken } from "../../../utils/auth";
import { logout } from "../../../frontapi/authApi";

function formatMoney(cents: number, currency: string) {
    const amount = (Number(cents) || 0) / 100;
    const cur = (currency || "EUR").toUpperCase();
    return `${amount.toFixed(2)} ${cur}`;
}

export default function BeautyPayments() {
    const user = getUserFromToken();
    const role = user?.role ?? "";

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [payments, setPayments] = useState<PaymentHistoryItem[]>([]);
    const [queryOrderId, setQueryOrderId] = useState<string>("");

    const [busyRefundId, setBusyRefundId] = useState<number | null>(null);

    const canRefund = role === "Owner" || role === "Manager";

    const authProblem =
        (error ?? "").toLowerCase().includes("unauthorized") ||
        (error ?? "").toLowerCase().includes("forbid") ||
        (error ?? "").includes("401") ||
        (error ?? "").includes("403");

    const load = async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await listPaymentsForBusiness();
            setPayments(Array.isArray(data) ? data : []);
        } catch (e: any) {
            setError(e?.message || "Failed to load payments");
            setPayments([]);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        load();
    }, []);

    const filtered = useMemo(() => {
        const id = queryOrderId.trim() ? Number(queryOrderId) : null;
        if (!id) return payments;
        return payments.filter((p) => p.orderId === id);
    }, [payments, queryOrderId]);

    const doRefund = async (p: PaymentHistoryItem) => {
        if (!canRefund) return;
        if (busyRefundId) return;
        if (p.status !== "Success") {
            setError("Only successful payments can be refunded.");
            return;
        }

        const ok = window.confirm(
            `Refund payment #${p.paymentId} for order #${p.orderId} (${formatMoney(p.amountCents, p.currency)})?`
        );
        if (!ok) return;

        setBusyRefundId(p.paymentId);
        setError(null);
        try {
            await refundPayment(p.paymentId);
            await load();
        } catch (e: any) {
            setError(e?.message || "Refund failed");
        } finally {
            setBusyRefundId(null);
        }
    };

    return (
        <div className="payments-container">
            <div className="action-bar">
                <h2 className="section-title">Payments</h2>
                <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
                    <input
                        className="dropdown"
                        placeholder="Filter by Order ID"
                        inputMode="numeric"
                        value={queryOrderId}
                        onChange={(e) => setQueryOrderId(e.target.value)}
                        disabled={loading}
                    />
                    <button className="btn" onClick={load} disabled={loading}>
                        {loading ? "Refreshing…" : "Refresh"}
                    </button>
                </div>
            </div>

            {error && (
                <div style={{ margin: "10px 0" }} className="no-payments">
                    <div>{error}</div>
                    {authProblem && (
                        <div style={{ marginTop: 10 }}>
                            <button
                                className="btn"
                                onClick={() => {
                                    logout();
                                    window.location.reload();
                                }}
                            >
                                Log out
                            </button>
                        </div>
                    )}
                </div>
            )}

            <div className="payments-table-wrap">
                <table className="payments-table">
                    <thead>
                        <tr>
                            <th>Payment</th>
                            <th>Order</th>
                            <th>Method</th>
                            <th>Status</th>
                            <th>Created</th>
                            <th>Completed</th>
                            <th className="right">Amount</th>
                            <th className="right">Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {loading && (
                            <tr>
                                <td colSpan={8}>
                    <div className="no-payments">Loading payments…</div>
                                </td>
                            </tr>
                        )}

                        {!loading && filtered.length === 0 && (
                            <tr>
                                <td colSpan={8}>
                                    <div className="no-payments">No payments found</div>
                                </td>
                            </tr>
                        )}

                        {!loading &&
                            filtered.map((p) => {
                                const statusLower = String(p.status || "").toLowerCase();
                                return (
                                    <tr key={p.paymentId} className="payments-row">
                                        <td className="name-cell">#{p.paymentId}</td>
                                        <td className="muted">#{p.orderId}</td>
                                        <td className="muted">{p.method}</td>
                                        <td>
                                            <span className={`payment-status-badge status-${statusLower}`}>{p.status}</span>
                                        </td>
                                        <td className="muted">{new Date(p.createdAt).toLocaleString()}</td>
                                        <td className="muted">{p.completedAt ? new Date(p.completedAt).toLocaleString() : "—"}</td>
                                        <td className="right" style={{ fontWeight: 700 }}>
                                            {formatMoney(p.amountCents, p.currency)}
                                        </td>
                                        <td className="right">
                                            {canRefund ? (
                                    <button
                                                    className="btn btn-sm"
                                        disabled={busyRefundId === p.paymentId || p.status !== "Success"}
                                        onClick={() => doRefund(p)}
                                        title={p.status !== "Success" ? "Only Success payments can be refunded" : ""}
                                    >
                                        {busyRefundId === p.paymentId ? "Refunding…" : "Refund"}
                                    </button>
                ) : (
                                                <span className="muted">—</span>
                )}
                                        </td>
                                    </tr>
                                );
                            })}
                    </tbody>
                </table>
            </div>
        </div>
    );
}
