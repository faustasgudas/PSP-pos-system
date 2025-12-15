import { useState } from "react";
import { createPayment } from "../../../frontapi/paymentApi";

export default function BeautyOrderPayment() {
    const params = new URLSearchParams(window.location.search);
    const orderId = Number(params.get("orderId"));

    const [giftCardCode, setGiftCardCode] = useState("");

    if (!orderId) {
        return <div className="page">No order found</div>;
    }

    const pay = async () => {
        try {
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
            alert("Payment failed");
        }
    };

    return (
        <div className="page">
            <h2>Payment</h2>

            <input
                placeholder="Gift card code (optional)"
                value={giftCardCode}
                onChange={e => setGiftCardCode(e.target.value)}
            />

            <button className="btn btn-primary" onClick={pay}>
                Pay
            </button>
        </div>
    );
}
