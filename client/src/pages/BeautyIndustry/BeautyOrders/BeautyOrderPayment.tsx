import { useState } from "react";
import { createPayment } from "../../../frontapi/paymentApi";

export default function BeautyOrderPayment(props?: {
    orderId: number;
    onBack?: () => void;
}) {
    const orderIdFromProps = props?.orderId;
    const params = new URLSearchParams(window.location.search);
    const orderIdFromQuery = Number(params.get("orderId"));
    const orderId = Number(orderIdFromProps ?? orderIdFromQuery);

    const [giftCardCode, setGiftCardCode] = useState("");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    if (!orderId) {
        return <div className="page">No order found</div>;
    }

    const pay = async () => {
        try {
            setLoading(true);
            setError(null);
            const result = await createPayment(
                orderId,
                giftCardCode || undefined
            );

            if (result.stripeUrl) {
                window.location.href = result.stripeUrl;
            } else {
                window.location.href = "/payment-success";
            }
        } catch (e) {
            console.error(e);
            setError((e as any)?.message || "Payment failed");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="page">
            <h2>Payment</h2>

            {props?.onBack && (
                <button className="btn" onClick={props.onBack} disabled={loading}>
                    ← Back
                </button>
            )}

            {error && (
                <div
                    style={{
                        background: "rgba(214, 40, 40, 0.1)",
                        border: "1px solid rgba(214, 40, 40, 0.3)",
                        color: "#b01d1d",
                        borderRadius: 12,
                        padding: "10px 12px",
                        marginTop: 10,
                        marginBottom: 10,
                    }}
                >
                    {error}
                </div>
            )}

            <input
                placeholder="Gift card code (optional)"
                value={giftCardCode}
                onChange={e => setGiftCardCode(e.target.value)}
                disabled={loading}
            />

            <button className="btn btn-primary" onClick={pay} disabled={loading}>
                {loading ? "Processing…" : "Pay"}
            </button>
        </div>
    );
}
