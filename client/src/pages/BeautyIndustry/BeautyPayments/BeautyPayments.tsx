import "./BeautyPayments.css";
import type { OrderSummaryResponse } from "../../../types/api";

interface BeautyPaymentsProps {
    orders: OrderSummaryResponse[];
    onRefresh: () => void;
}

export default function BeautyPayments({ orders, onRefresh }: BeautyPaymentsProps) {
    return (
        <div className="payments-container">
            <div className="action-bar">
                <h2 className="section-title">Orders & Payments</h2>
            </div>

            <div className="payments-list">
                {orders.length > 0 ? (
                    orders.map(order => (
                        <div key={order.orderId} className="payment-card">
                            <div className="payment-main">
                                <div className="payment-amount">
                                    Order #{order.orderId}
                                </div>
                                <div className="payment-status">
                                    {order.status}
                                </div>
                            </div>

                            <div className="payment-details">
                                {order.reservationId && <div>Reservation #{order.reservationId}</div>}
                                <div>Created: {new Date(order.createdAt).toLocaleString()}</div>
                                {order.closedAt && <div>Closed: {new Date(order.closedAt).toLocaleString()}</div>}
                                {order.tableOrArea && <div>Table/Area: {order.tableOrArea}</div>}
                                {order.tipAmount > 0 && <div>Tip: €{order.tipAmount.toFixed(2)}</div>}
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="no-payments">No orders found</div>
                )}
            </div>
        </div>
    );
}
