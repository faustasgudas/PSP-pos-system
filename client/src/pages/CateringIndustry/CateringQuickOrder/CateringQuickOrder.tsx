import { useState } from 'react';
import "../../../App.css";
import "./CateringQuickOrder.css";

function CateringQuickOrder() {
    return (
        <div className="content-box" id="quick-order">
            <div className="action-bar">
                <h2 className="section-title">Quick Order</h2>
                <button className="btn btn-secondary">Back to Dashboard</button>
            </div>
            <div className="card">
                <h3>Select Products</h3>
                <div className="product-grid" id="product-grid">
                    {/* todo - add product grid */}
                </div>
                <div className="selected-products" id="selected-products">
                    <h4>Selected Products</h4>
                    <div className="selected-product-grid" id="selected-product-grid">
                        {/* todo - add selected products */}
                    </div>
                    <div className="total">
                        Total: €<span id="order-total">0.00</span>
                    </div>
                </div>
                <div>
                    <button className="btn btn-secondary">Clear</button>
                    <button className="btn btn-success">Process Payment</button>
                </div>
            </div>
        </div>
    )
}

export default CateringQuickOrder;