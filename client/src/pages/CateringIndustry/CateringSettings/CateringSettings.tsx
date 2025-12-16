import { useState } from "react";
import "./CateringSettings.css";
import { BeautySelect } from "../../../components/ui/BeautySelect";

export default function CateringSettings() {
    const [country, setCountry] = useState("LT");
    const [taxCalc, setTaxCalc] = useState("PerLine");
    const [priceIncludesTax, setPriceIncludesTax] = useState("true");

    return (
        <div className="settings-container">
            <div className="action-bar">
                <h2 className="section-title">Business Configuration</h2>
                <button 
                    className="btn btn-secondary">
                    Back to Dashboard
                </button>
            </div>

            <div className="settings-panel">
                <h3 className="settings-card-title">Business Settings</h3>

                <div className="settings-form-grid">
                    <div className="settings-field">
                        <label>Business Name</label>
                        <input type="text" defaultValue="Catering" />
                    </div>

                    <div className="settings-field">
                        <label>Country</label>
                        <BeautySelect
                            value={country}
                            onChange={setCountry}
                            options={[{ value: "LT", label: "Lithuania" }]}
                        />
                    </div>

                    <div className="settings-field">
                        <label>Tax Calculation</label>
                        <BeautySelect
                            value={taxCalc}
                            onChange={setTaxCalc}
                            options={[
                                { value: "PerLine", label: "Round tax per line" },
                                { value: "PerOrder", label: "Round tax per order" },
                            ]}
                        />
                    </div>

                    <div className="settings-field">
                        <label>Price Includes Tax</label>
                        <BeautySelect
                            value={priceIncludesTax}
                            onChange={setPriceIncludesTax}
                            options={[
                                { value: "true", label: "Yes" },
                                { value: "false", label: "No" },
                            ]}
                        />
                    </div>
                </div>

                <div className="settings-actions">
                    <button className="btn btn-success">
                        Save Settings
                    </button>
                </div>
            </div>
        </div>
    );
}