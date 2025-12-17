import { useEffect, useState } from 'react';
import "./CateringEmployees.css";
import { getUserFromToken } from "../../../utils/auth";

interface Employee {
    employeeId: number;
    name: string;
    email: string;
    role: "Owner" | "Manager" | "Staff";
    status: "Active" | "Inactive";
}

export default function CateringEmployees() {
    const user = getUserFromToken();
    const role = user?.role;
    const token = localStorage.getItem("token");
    const businessId = localStorage.getItem("businessId");
    
    const [employees, setEmployees] = useState<Employee[]>([]);
    const [loading, setLoading] = useState(true);
    
    const [showModal, setShowModal] = useState(false);
    const [employeeToDeactivate, setEmployeeToDeactivate] = useState<Employee | null>(null);
    
    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [newRole, setNewRole] = useState<"Staff" | "Manager">("Staff");
    
    const fetchEmployees = async () => {
        try {
            const res = await fetch(
                `https://localhost:44317/api/businesses/${businessId}/employees`,
                {
                    headers: {
                        Authorization: `Bearer ${token}`,
                    },
                }
            );
            if (!res.ok) throw new Error();
            const data = await res.json();
            setEmployees(data);
        } catch {
            setEmployees([]);
        } finally {
            setLoading(true);
        }
    };
    
    useEffect(() => {
        fetchEmployees();
    }, []);
    
    const handleAddEmployee = async () => {
        await fetch(
            `https://localhost:44317/api/businesses/${businessId}/employees`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({
                    name,
                    email,
                    password,
                    role: newRole,
                }),
            }
        );
        setShowModal(false);
        setName("");
        setEmail("");
        setPassword("");
        setNewRole("Staff");
        fetchEmployees();
    };
    
    const handleDeactivate = async () => {
        if (!employeeToDeactivate) return;
        await fetch(
            `https://localhost:44317/api/businesses/${businessId}/employees/${employeeToDeactivate.employeeId}/deactivate`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({ reason: "" }),
            }
        );
        setEmployeeToDeactivate(null);
        fetchEmployees();
    };
    
    const sortedEmployees = employees
        .filter(e => e.status === "Active")
        .sort((a, b) => {
            const order = { Owner: 0, Manager: 1, Staff: 2 };
            return order[a.role] - order[b.role];
        });
    
    if (loading) return <div>Loading employees…</div>;
    
    return (
        <div className="employees-container">
            <div className="employees-header">
                <h2>Employees</h2>

                {(role === "Owner" || role === "Manager") && (
                    <button
                        className="btn btn-primary"
                        onClick={() => setShowModal(true)}
                    >
                        ➕ Add Employee
                    </button>
                )}
            </div>
            <div className="employee-grid">
                {sortedEmployees.map(emp => {
                    const isSelf = emp.email === user?.email;
                    const canDeactivate =
                        !isSelf &&
                        (role === "Owner" ||
                            (role === "Manager" && emp.role === "Staff"));
                    return (
                        <div key={emp.employeeId} className="employee-card">
                            <div className="employee-name">
                                {emp.name}
                                {isSelf && <span className="you-badge">You</span>}
                            </div>
                            <div className="employee-email">{emp.email}</div>
                            <div className="employee-role">{emp.role}</div>

                            {canDeactivate && (
                                <button
                                    className="btn btn-danger"
                                    onClick={() => setEmployeeToDeactivate(emp)}
                                >
                                    Deactivate
                                </button>
                            )}
                        </div>
                    );
                })}
            </div>

            {showModal && (
                <div className="modal-backdrop">
                    <div className="modal">
                        <h3>Add Employee</h3>
                        <input
                            placeholder="Name"
                            value={name}
                            onChange={e => setName(e.target.value)}
                        />
                        <input
                            placeholder="Email"
                            value={email}
                            onChange={e => setEmail(e.target.value)}
                        />
                        <input
                            placeholder="Password"
                            type="password"
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                        />
                        <select
                            value={newRole}
                            onChange={e => setNewRole(e.target.value as any)}
                        >
                            <option value="Staff">Staff</option>
                            {role === "Owner" && (
                                <option value="Manager">Manager</option>
                            )}
                        </select>
                        <div className="modal-actions">
                            <button
                                className="btn"
                                onClick={() => setShowModal(false)}
                            >
                                Cancel
                            </button>
                            <button
                                className="btn btn-primary"
                                onClick={handleAddEmployee}
                            >
                                Add
                            </button>
                        </div>
                    </div>
                </div>
            )}
            {employeeToDeactivate && (
                <div className="modal-backdrop">
                    <div className="modal">
                        <h3>Deactivate Employee</h3>
                        <p>
                            Are you sure you want to deactivate{" "}
                            <strong>{employeeToDeactivate.name}</strong>?
                        </p>
                        <div className="modal-actions">
                            <button
                                className="btn"
                                onClick={() => setEmployeeToDeactivate(null)}
                            >
                                Cancel    
                            </button>
                            <button
                                className="btn btn-danger"
                                onClick={handleDeactivate}
                            >
                                Deactivate    
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}