import { useState } from 'react';
import "./CateringEmployees.css";

interface Employee {
    id: number;
    name: string;
    role: string;
}

interface CateringEmployeesProps {
    employees: Employee[];
}

export default function CateringEmployees({ employees }: CateringEmployeesProps) {
    const [showModal, setShowModal] = useState(false);
    
    return (
        <div className="employees-container">
            <div className="action-bar">
                <h2 className="section-title">Employees</h2>
                <button className="btn btn-primary" onClick={() => setShowModal(true)}>
                    ➕ Add Employee
                </button>
            </div>
            <div className="employees-list">
                {employees.length > 0 ? (
                    employees.map(emp => (
                        <div key={emp.id} className="employee-card">
                            <div>
                                <div className="employee-name">{emp.name}</div>
                                <div className="employee-role">{emp.role}</div>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="no-employees">No employees found</div>
                )}
            </div>

            {showModal && (
                <div className="modal-overlay">
                    <div className="modal-card">
                        <h3 className="modal-title">Add Employee</h3>
                        <div className="modal-form">
                            <div className="modal-field">
                                <label>Name</label>
                                <input type="text" />
                            </div>
                            <div className="modal-field">
                                <label>Role</label>
                                <input type="text" />
                            </div>
                        </div>
                        <div className="modal-actions">
                            <button
                                className="btn btn-secondary"
                                onClick={() => setShowModal(false)}
                            >
                                Cancel
                            </button>
                            <button className="btn btn-success">
                                Save Employee
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}