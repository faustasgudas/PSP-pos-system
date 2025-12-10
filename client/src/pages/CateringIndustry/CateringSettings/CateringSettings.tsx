import { useState } from 'react';
import "../../../App.css";
import "./CateringDashboard.css";

function CateringSettings() {
    return (
        <div className="content-box" id="settings">
            <div className="action-bar">
                <h2 className="section-title">Business Configuration</h2>
                <button className="btn btn-secondary">Back to Dashboard</button>
            </div>
            <div className="card">
                <h3>Business Settings</h3>
                <div className="form-grid">
                    <div className="form-group">
                        <label className="form-label">Business Name</label>
                        <input type="text" className="form-input" value="SuperApp"></input>
                    </div>
                    <div className="form-group">
                        <label className="form-label">Business Type</label>
                        <select className="form-select" id="business-type">
                            <option value="catering">Catering</option>
                            <option value="beauty">Beauty</option>
                        </select>
                    </div>
                    <div className="form-group">
                        <label className="form-label">Country</label>
                        <select className="form-select" id="country">
                            <option value="lt">Lithuania</option>
                        </select>
                    </div>
                    <div className="form-group">
                        <label className="form-label">Tax Calculation</label>
                        <select className="form-select" id="tax-calculation">
                            <option value="per-line">Round tax per line</option>
                            <option value="per-order">Round tax per order</option>
                        </select>
                    </div>
                    <div className="form-group">
                        <label className="form-label">Price Includes Tax</label>
                        <select className="form-select" id="price-incl-tax">
                            <option value="true">Yes</option>
                            <option value="false">No</option>
                        </select>
                    </div>
                </div>
                <div>
                    <button className="btn btn-success">Save Settings</button>
                </div>
            </div>
        </div>
    )
}

export default CateringSettings;