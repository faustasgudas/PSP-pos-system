import "./CateringPayments.css";

interface Payment {
    id: number;
    reservationId: number;
    amount: { amount: number; currency: string };
    method: string;
    status: string;
}

interface CateringPaymentsProps {
    payments: Payment[];
}

export default function CateringPayments({ payments }: CateringPaymentsProps) {
    return (
        <div className="payments-container">
            <div className="action-bar">
                <h2 className="section-title">Payments</h2>
            </div>
            <div className="payments-list">
                {payments.length > 0 ? (
                    payments.map(payment => (
                        <div key={payment.id} className="payment-card">
                            <div className="payment-main">
                                <div className="payment-amount">
                                    {payment.amount.amount} {payment.amount.currency}
                                </div>
                                <div className="payment-status">
                                    {payment.status}
                                </div>
                            </div>
                            <div className="payment-details">
                                <div>Reservation #{payment.reservationId}</div>
                                <div>Method: {payment.method}</div>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="no-payments">No payments found</div>
                )}
            </div>
        </div>
    )
}