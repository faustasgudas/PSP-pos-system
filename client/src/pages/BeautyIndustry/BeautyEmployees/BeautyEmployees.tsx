import { useState } from "react";
import "./BeautyEmployees.css";
import type { EmployeeSummaryResponse } from "../../../types/api";
import * as employeeService from "../../../services/employeeService";

interface BeautyEmployeesProps {
    employees: EmployeeSummaryResponse[];
    businessId: number;
    onRefresh: () => void;
}

export default function BeautyEmployees({ employees, businessId, onRefresh }: BeautyEmployeesProps) {
    const [showModal, setShowModal] = useState(false);
    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [role, setRole] = useState("Staff");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

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
                        <div key={emp.employeeId} className="employee-card">
                            <div>
                                <div className="employee-name">{emp.name}</div>
                                <div className="employee-role">{emp.role}</div>
                                <div className="employee-email">{emp.email}</div>
                                <div className="employee-status">Status: {emp.status}</div>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="no-employees">No employees found</div>
                )}
            </div>

            {/* ✅ ADD EMPLOYEE MODAL */}
            {showModal && (
                <div className="modal-overlay" onClick={() => setShowModal(false)}>
                    <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                        <h3 className="modal-title">Add Employee</h3>

                        {error && (
                            <div style={{ color: "red", marginBottom: "1rem" }}>
                                {error}
                            </div>
                        )}

                        <form
                            onSubmit={async (e) => {
                                e.preventDefault();
                                setIsSubmitting(true);
                                setError(null);
                                try {
                                    await employeeService.createEmployee(businessId, {
                                        name,
                                        email,
                                        password,
                                        role,
                                    });
                                    setName("");
                                    setEmail("");
                                    setPassword("");
                                    setRole("Staff");
                                    setShowModal(false);
                                    onRefresh();
                                } catch (err) {
                                    setError(err instanceof Error ? err.message : "Failed to create employee");
                                } finally {
                                    setIsSubmitting(false);
                                }
                            }}
                        >
                            <div className="modal-form">
                                <div className="modal-field">
                                    <label>Name *</label>
                                    <input
                                        type="text"
                                        value={name}
                                        onChange={(e) => setName(e.target.value)}
                                        required
                                    />
                                </div>

                                <div className="modal-field">
                                    <label>Email *</label>
                                    <input
                                        type="email"
                                        value={email}
                                        onChange={(e) => setEmail(e.target.value)}
                                        required
                                    />
                                </div>

                                <div className="modal-field">
                                    <label>Password *</label>
                                    <input
                                        type="password"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        required
                                    />
                                </div>

                                <div className="modal-field">
                                    <label>Role *</label>
                                    <select
                                        value={role}
                                        onChange={(e) => setRole(e.target.value)}
                                        required
                                    >
                                        <option value="Staff">Staff</option>
                                        <option value="Manager">Manager</option>
                                        <option value="Owner">Owner</option>
                                    </select>
                                </div>
                            </div>

                            <div className="modal-actions">
                                <button
                                    type="button"
                                    className="btn btn-secondary"
                                    onClick={() => setShowModal(false)}
                                    disabled={isSubmitting}
                                >
                                    Cancel
                                </button>
                                <button
                                    type="submit"
                                    className="btn btn-success"
                                    disabled={isSubmitting}
                                >
                                    {isSubmitting ? "Saving..." : "Save Employee"}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
}
