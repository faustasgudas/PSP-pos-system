import "./CateringSettings.css";

export default function CateringSettings() {
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
                        <select defaultValue="LT">
                            <option value="LT">Lithuania</option>
                        </select>
                    </div>

                    <div className="settings-field">
                        <label>Tax Calculation</label>
                        <select defaultValue="PerLine">
                            <option value="PerLine">Round tax per line</option>
                            <option value="PerOrder">Round tax per order</option>
                        </select>
                    </div>

                    <div className="settings-field">
                        <label>Price Includes Tax</label>
                        <select defaultValue="true">
                            <option value="true">Yes</option>
                            <option value="false">No</option>
                        </select>
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