import { useState } from 'react';
import "../../../App.css";
import "./CateringPayments.css";

function CateringPayments() {
    return (
        <div className="content-box" id="payments">
            <div className="action-bar">
                <h2 className="section-title">Payment Management</h2>
                <button className="btn btn-primary">
                    <span>📅</span> View Reservations
                </button>
            </div>
            <div className="unpaid-orders" id="unpaid-orders">
                {/* todo - add list of unpaid orders */}
            </div>
        </div>
    )
}

export default CateringPayments;