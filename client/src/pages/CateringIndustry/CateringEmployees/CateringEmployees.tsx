import { useEffect, useState } from "react";
import "./CateringEmployees.css";
import "../../BeautyIndustry/BeautyEmployees/BeautyEmployees.css";

import { getUserFromToken } from "../../../utils/auth";
import { BeautySelect } from "../../../components/ui/BeautySelect";

type EmployeeRole = "Owner" | "Manager" | "Staff";

interface Employee {
    employeeId: number;
    name: string;
    email: string;
    role: EmployeeRole;
    status: "Active" | "Inactive" | string;
}

export default function CateringEmployees() {
    const user = getUserFromToken();
    const role = (user?.role ?? "") as EmployeeRole | "";

    const token = localStorage.getItem("token");
    const businessId = localStorage.getItem("businessId");

    // 🚫 Staff must never see employees at all
    if (role === "Staff") return null;

    const [employees, setEmployees] = useState<Employee[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [showAddModal, setShowAddModal] = useState(false);
    const [employeeToDeactivate, setEmployeeToDeactivate] = useState<Employee | null>(null);
    const [employeeToEdit, setEmployeeToEdit] = useState<Employee | null>(null);

    // Add form fields
    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [newRole, setNewRole] = useState<"Staff" | "Manager">("Staff");

    // Edit form fields
    const [editName, setEditName] = useState("");
    const [editEmail, setEditEmail] = useState("");
    const [editRole, setEditRole] = useState<EmployeeRole>("Staff");

    const fetchEmployees = async () => {
        setLoading(true);
        setError(null);
        try {
            const res = await fetch(`http://localhost:5269/api/businesses/${businessId}/employees`, {
                headers: { Authorization: `Bearer ${token}` },
            });
            if (!res.ok) throw new Error("Failed to load employees");
            const data = await res.json();
            setEmployees(Array.isArray(data) ? data : []);
        } catch (e: any) {
            setEmployees([]);
            setError(e?.message || "Failed to load employees");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        void fetchEmployees();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const handleAddEmployee = async () => {
        setError(null);
        try {
            const res = await fetch(`http://localhost:5269/api/businesses/${businessId}/employees`, {
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
            });

            if (!res.ok) throw new Error("Failed to add employee");

            setShowAddModal(false);
            setName("");
            setEmail("");
            setPassword("");
            setNewRole("Staff");
            await fetchEmployees();
        } catch (e: any) {
            setError(e?.message || "Failed to add employee");
        }
    };

    const handleDeactivate = async () => {
        if (!employeeToDeactivate) return;

        setError(null);
        try {
            const res = await fetch(
                `http://localhost:5269/api/businesses/${businessId}/employees/${employeeToDeactivate.employeeId}/deactivate`,
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        Authorization: `Bearer ${token}`,
                    },
                    body: JSON.stringify({ reason: "" }),
                }
            );
            if (!res.ok) throw new Error("Failed to deactivate employee");

            setEmployeeToDeactivate(null);
            await fetchEmployees();
        } catch (e: any) {
            setError(e?.message || "Failed to deactivate employee");
        }
    };

    const openEditModal = (emp: Employee) => {
        setEmployeeToEdit(emp);
        setEditName(emp.name);
        setEditEmail(emp.email);
        setEditRole(emp.role as EmployeeRole);
    };

    const handleEditEmployee = async () => {
        if (!employeeToEdit) return;

        setError(null);
        try {
            const res = await fetch(
                `http://localhost:5269/api/businesses/${businessId}/employees/${employeeToEdit.employeeId}`,
                {
                    method: "PATCH",
                    headers: {
                        "Content-Type": "application/json",
                        Authorization: `Bearer ${token}`,
                    },
                    body: JSON.stringify({
                        name: editName,
                        email: editEmail,
                        role: editRole,
                    }),
                }
            );
            if (!res.ok) throw new Error("Failed to update employee");

            setEmployeeToEdit(null);
            await fetchEmployees();
        } catch (e: any) {
            setError(e?.message || "Failed to update employee");
        }
    };

    const sortedEmployees = employees
        .filter((e) => e.status === "Active")
        .sort((a, b) => {
            const order = { Owner: 0, Manager: 1, Staff: 2 } as const;
            return (order[a.role as EmployeeRole] ?? 99) - (order[b.role as EmployeeRole] ?? 99);
        });

    if (loading) return <div className="loading-message">Loading employees…</div>;

    return (
        <div className="employees-container">
            <div className="employees-header">
                <h2>Employees</h2>

                {(role === "Owner" || role === "Manager") && (
                    <button className="btn btn-primary" onClick={() => setShowAddModal(true)}>
                        ➕ Add Employee
                    </button>
                )}
            </div>

            {error && (
                <div style={{ marginBottom: 12, color: "#b01d1d" }}>
                    {error}
                </div>
            )}

            <div className="employees-table-wrap">
                <table className="employees-table">
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Email</th>
                            <th>Role</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {sortedEmployees.map((emp) => {
                            const isSelf = emp.email === user?.email;
                            const canEdit =
                                role === "Owner" || (role === "Manager" && emp.role === "Staff");
                            const canDeactivate =
                                !isSelf && (role === "Owner" || (role === "Manager" && emp.role === "Staff"));

                            return (
                                <tr key={emp.employeeId}>
                                    <td className="name-cell">
                                        {emp.name}
                                        {isSelf && <span className="you-badge">You</span>}
                                    </td>
                                    <td>{emp.email}</td>
                                    <td>
                                        <span className={`role-badge role-${String(emp.role).toLowerCase()}`}>
                                            {emp.role}
                                        </span>
                                    </td>
                                    <td className="actions-cell">
                                        {canEdit && !isSelf && (
                                            <button className="btn btn-sm" onClick={() => openEditModal(emp)}>
                                                ✏️ Edit
                                            </button>
                                        )}
                                        {canDeactivate && (
                                            <button className="btn btn-sm btn-danger" onClick={() => setEmployeeToDeactivate(emp)}>
                                                Deactivate
                                            </button>
                                        )}
                                        {isSelf && <span className="muted">—</span>}
                                    </td>
                                </tr>
                            );
                        })}
                    </tbody>
                </table>
            </div>

            {showAddModal && (
                <div className="modal-backdrop" onClick={() => setShowAddModal(false)}>
                    <div className="modal" onClick={(e) => e.stopPropagation()}>
                        <h3>Add Employee</h3>

                        <div className="form-field">
                            <label>Name</label>
                            <input value={name} onChange={(e) => setName(e.target.value)} />
                        </div>

                        <div className="form-field">
                            <label>Email</label>
                            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
                        </div>

                        <div className="form-field">
                            <label>Password</label>
                            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
                        </div>

                        <div className="form-field">
                            <label>Role</label>
                            <BeautySelect
                                value={newRole}
                                onChange={(v) => setNewRole(v as any)}
                                options={[
                                    { value: "Staff", label: "Staff" },
                                    ...(role === "Owner" ? [{ value: "Manager", label: "Manager" }] : []),
                                ]}
                            />
                        </div>

                        <div className="modal-actions">
                            <button className="btn" onClick={() => setShowAddModal(false)}>
                                Cancel
                            </button>
                            <button className="btn btn-primary" onClick={handleAddEmployee}>
                                Add Employee
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {employeeToEdit && (
                <div className="modal-backdrop" onClick={() => setEmployeeToEdit(null)}>
                    <div className="modal" onClick={(e) => e.stopPropagation()}>
                        <h3>Edit Employee</h3>

                        <div className="form-field">
                            <label>Name</label>
                            <input value={editName} onChange={(e) => setEditName(e.target.value)} />
                        </div>

                        <div className="form-field">
                            <label>Email</label>
                            <input type="email" value={editEmail} onChange={(e) => setEditEmail(e.target.value)} />
                        </div>

                        <div className="form-field">
                            <label>Role</label>
                            <BeautySelect
                                value={editRole}
                                onChange={(v) => setEditRole(v as any)}
                                disabled={employeeToEdit.role === "Owner"}
                                options={[
                                    { value: "Staff", label: "Staff" },
                                    ...(role === "Owner" ? [{ value: "Manager", label: "Manager" }] : []),
                                    ...(employeeToEdit.role === "Owner" ? [{ value: "Owner", label: "Owner" }] : []),
                                ]}
                            />
                        </div>

                        <div className="modal-actions">
                            <button className="btn" onClick={() => setEmployeeToEdit(null)}>
                                Cancel
                            </button>
                            <button className="btn btn-primary" onClick={handleEditEmployee}>
                                Save Changes
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {employeeToDeactivate && (
                <div className="modal-backdrop" onClick={() => setEmployeeToDeactivate(null)}>
                    <div className="modal" onClick={(e) => e.stopPropagation()}>
                        <h3>Deactivate Employee</h3>
                        <p>
                            Are you sure you want to deactivate <strong>{employeeToDeactivate.name}</strong>?
                        </p>

                        <div className="modal-actions">
                            <button className="btn" onClick={() => setEmployeeToDeactivate(null)}>
                                Cancel
                            </button>
                            <button className="btn btn-danger" onClick={handleDeactivate}>
                                Deactivate
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}