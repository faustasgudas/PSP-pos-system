import { useState } from 'react';
import "../../../App.css"
import "./CateringEmployees.css";

interface Employee {
    id: number;
    name: string;
    role: string;
}

function CateringEmployees() {
    return (
        <div className="conent-box" id="employees">
            <div className="action-bar">
                <h2 className="section-title">Employees</h2>
                <button className="btn btn-primary">
                    ➕ Add Employee
                </button>
            </div>
            <div className="employee-list">
                {/* todo - add employee list */}
            </div>
        </div>
    )
}

export default CateringEmployees;