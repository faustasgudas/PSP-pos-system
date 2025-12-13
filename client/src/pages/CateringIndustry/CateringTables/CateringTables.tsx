import { useState } from 'react';
import "../../../App.css";
import "./CateringTables.css";

interface Table {
    id: number;
    seats: number;
    status: string;
}

function CateringTables() {
    return (
        <div className="content-box" id="tables">
            <div className="action-bar">
                <h2 className="section-title">Table Management</h2>
                <button className="btn btn-primary">
                    <span>➕</span> Add Table
                </button>
            </div>
            <div className="tables-grid" id="tables-grid">
                {/* todo - add tables grid */}
            </div>
        </div>
    )
}

export default CateringTables;