import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { listPaymentsForBusiness, type PaymentHistoryItem } from "../../../frontapi/paymentApi";
import { useNavigate } from "react-router-dom";


export default function PaymentSuccess() {
    const [params] = useSearchParams();
    const navigate = useNavigate();
    const sessionId = params.get("sessionId");

    const [payments, setPayments] = useState<PaymentHistoryItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [polling, setPolling] = useState(true);

    useEffect(() => {
        if (!sessionId) {
            setError("Missing sessionId");
            setLoading(false);
            return;
        }

        let timer: number | undefined;

        const load = async () => {
            try {
                const data = await listPaymentsForBusiness();
                setPayments(Array.isArray(data) ? data : []);
                setLoading(false);
            } catch (e: any) {
                setError(e?.message ?? "Failed to load payments");
                setLoading(false);
            }
        };

        load();
        timer = window.setInterval(load, 2000);

        return () => {
            if (timer) window.clearInterval(timer);
        };
    }, [sessionId]);

    const payment = useMemo(
        () => payments.find((p) => p.stripeSessionId === sessionId),
        [payments, sessionId]
    );

    useEffect(() => {
        if (!payment) return;
        if (payment.status === "Success" || payment.status === "Cancelled") {
            setPolling(false);
        }
    }, [payment]);

    if (loading) return <div style={styles.box}>Processing payment…</div>;
    if (error) return <div style={{ ...styles.box, color: "red" }}>{error}</div>;
    if (!payment) return <div style={styles.box}>Waiting for payment confirmation…</div>;

    if (payment.status === "Success") {
        return (
            <div style={styles.box}>
                <h2>✅ Payment successful</h2>
                <div>Order #{payment.orderId}</div>
                <div>
                    Amount: {(payment.amountCents / 100).toFixed(2)} {payment.currency}
                </div>

                <button
                    style={styles.btn}
                    onClick={() => navigate("/", { replace: true })}
                >
                    Return to main screen
                </button>
            </div>
        );
    }

    if (payment.status === "Cancelled") {
        return (
            <div style={{ ...styles.box, color: "#b01d1d" }}>
                <h2>❌ Payment cancelled</h2>
                <div>Order #{payment.orderId}</div>
            </div>
        );
    }

    return (
        <div style={styles.box}>
            Processing payment…
            {polling && <div className="muted">Waiting for Stripe webhook…</div>}
        </div>
    );
}

const styles: Record<string, React.CSSProperties> = {
    box: {
        maxWidth: 420,
        margin: "80px auto",
        padding: 24,
        borderRadius: 12,
        background: "#fff",
        boxShadow: "0 10px 30px rgba(0,0,0,.15)",
        textAlign: "center",
        fontSize: 16,
    },
    btn: {
        marginTop: 16,
        padding: "10px 14px",
        borderRadius: 10,
        border: "1px solid rgba(0,0,0,0.12)",
        background: "#111827",
        color: "#fff",
        cursor: "pointer",
    },
};
