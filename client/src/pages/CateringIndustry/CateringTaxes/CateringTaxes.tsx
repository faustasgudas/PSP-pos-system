import { useState } from 'react';
import "../../../App.css";
import "./CateringTaxes.css";

function CateringTaxes() {
    return (
        <div className="content-box" id="taxes">
            <div className="action-bar">
                <h2 className="section-title">Tax Configuration</h2>
                <button className="btn btn-primary">
                    <span>➕</span> New Tax Rule
                </button>
            </div>
            <div className="tax-rules-grid" id="tax-rules-grid">
                {/* todo - add tax rules grid */}
            </div>
        </div>
    )
}

export default CateringTaxes;