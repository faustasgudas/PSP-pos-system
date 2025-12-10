import { useState } from 'react';
import "../../../App.css";
import "./CateringInventory.css";

function CateringInventory() {
    return (
        <div className="content-box" id="inventory">
            <div className="action-bar">
                <h2 className="section-title">Inventory Management</h2>
                <button className="btn btn-primary">
                    <span>➕</span> Add Inventory Item
                </button>
            </div>
            <div className="inventory" id="inventory-grid">
                {/* todo - add inventory grid */}
            </div>
        </div>
    )
}

export default CateringInventory;