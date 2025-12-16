import { useEffect, useMemo, useState } from "react";
import "./BeautyPayments.css";
import {
    listPaymentsForBusiness,
    refundPayment,
    type PaymentHistoryItem,
} from "../../../frontapi/paymentApi";
import { getUserFromToken } from "../../../utils/auth";
import { logout } from "../../../frontapi/authApi";

function formatMoney(cents: number, currency: string) {
    const amount = (Number(cents) || 0) / 100;
    const cur = (currency || "EUR").toUpperCase();
    return `${amount.toFixed(2)} ${cur}`;
}

export default function BeautyPayments() {
    const user = getUserFromToken();
    const role = (user?.role ?? "").toLowerCase();

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [payments, setPayments] = useState<PaymentHistoryItem[]>([]);
    const [queryOrderId, setQueryOrderId] = useState<string>("");

    const [busyRefundId, setBusyRefundId] = useState<number | null>(null);

    const canRefund = role === "owner" || role === "manager";

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
        const raw = queryOrderId.trim();
        if (!raw) return payments;

        const id = Number(raw);
        if (!Number.isFinite(id) || id <= 0) return payments;

        return payments.filter((p) => p.orderId === id);
    }, [payments, queryOrderId]);

    const totalCents = (p: PaymentHistoryItem) =>
        (Number(p.amountCents) || 0) + (Number(p.tipCents) || 0);

    const doRefund = async (p: PaymentHistoryItem) => {
        if (!canRefund) return;
        if (busyRefundId) return;

        if (p.status !== "Success") {
            setError("Only successful payments can be refunded.");
            return;
        }

        const ok = window.confirm(
            `Refund payment #${p.paymentId} for order #${p.orderId} (${formatMoney(
                totalCents(p),
                p.currency
            )})?`
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

            <div className="payments-list">
                {loading ? (
                    <div className="no-payments">Loading payments…</div>
                ) : filtered.length > 0 ? (
                    filtered.map((p) => (
                        <div key={p.paymentId} className="payment-card">
                            <div className="payment-main">
                                <div className="payment-amount">
                                    {formatMoney(totalCents(p), p.currency)}
                                </div>
                                <div className="payment-status">{p.status}</div>
                            </div>

                            <div className="payment-details">
                                <div>Order #{p.orderId}</div>
                                <div>Method: {p.method}</div>

                                <div className="muted">
                                    Base: {formatMoney(p.amountCents, p.currency)} • Tip:{" "}
                                    {formatMoney(p.tipCents ?? 0, p.currency)}
                                </div>

                                <div className="muted">
                                    Created: {new Date(p.createdAt).toLocaleString()}
                                </div>
                                {p.completedAt && (
                                    <div className="muted">
                                        Completed: {new Date(p.completedAt).toLocaleString()}
                                    </div>
                                )}
                            </div>

                            {canRefund && (
                                <div style={{ marginTop: 10 }}>
                                    <button
                                        className="btn"
                                        disabled={busyRefundId === p.paymentId || p.status !== "Success"}
                                        onClick={() => doRefund(p)}
                                        title={p.status !== "Success" ? "Only Success payments can be refunded" : ""}
                                    >
                                        {busyRefundId === p.paymentId ? "Refunding…" : "Refund"}
                                    </button>
                                </div>
                            )}
                        </div>
                    ))
                ) : (
                    <div className="no-payments">No payments found</div>
                )}
            </div>
        </div>
    );
}
