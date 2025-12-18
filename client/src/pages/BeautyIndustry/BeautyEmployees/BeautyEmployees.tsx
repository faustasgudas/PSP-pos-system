import { useEffect, useState } from "react";
import "./BeautyEmployees.css";
import { getUserFromToken } from "../../../utils/auth";
import { BeautySelect } from "../../../components/ui/BeautySelect";

interface Employee {
    employeeId: number;
    name: string;
    email: string;
    role: "Owner" | "Manager" | "Staff";
    status: "Active" | "Inactive";
}

export default function BeautyEmployees() {
    const user = getUserFromToken();
    const role = user?.role;
    const token = localStorage.getItem("token");
    const businessId = localStorage.getItem("businessId");

    // 🚫 Staff must never see employees at all
    if (role === "Staff") return null;

    const [employees, setEmployees] = useState<Employee[]>([]);
    const [loading, setLoading] = useState(true);

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
    const [editRole, setEditRole] = useState<"Staff" | "Manager" | "Owner">("Staff");

    const fetchEmployees = async () => {
        try {
            const res = await fetch(
                `http://localhost:5269/api/businesses/${businessId}/employees`,
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
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchEmployees();
    }, []);

    const handleAddEmployee = async () => {
        await fetch(
            `http://localhost:5269/api/businesses/${businessId}/employees`,
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

        setShowAddModal(false);
        setName("");
        setEmail("");
        setPassword("");
        setNewRole("Staff");
        fetchEmployees();
    };

    const handleDeactivate = async () => {
        if (!employeeToDeactivate) return;

        await fetch(
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

        setEmployeeToDeactivate(null);
        fetchEmployees();
    };

    const openEditModal = (emp: Employee) => {
        setEmployeeToEdit(emp);
        setEditName(emp.name);
        setEditEmail(emp.email);
        setEditRole(emp.role);
    };

    const handleEditEmployee = async () => {
        if (!employeeToEdit) return;

        await fetch(
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

        setEmployeeToEdit(null);
        fetchEmployees();
    };

    const sortedEmployees = employees
        .filter(e => e.status === "Active")
        .sort((a, b) => {
            const order = { Owner: 0, Manager: 1, Staff: 2 };
            return order[a.role] - order[b.role];
        });

    if (loading) return <div className="loading-message">Loading employees…</div>;

    return (
        <div className="employees-container">
            <div className="employees-header">
                <h2>Employees</h2>

                {(role === "Owner" || role === "Manager") && (
                    <button
                        className="btn btn-primary"
                        onClick={() => setShowAddModal(true)}
                    >
                        ➕ Add Employee
                    </button>
                )}
            </div>

            {/* Employee Table */}
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
                        {sortedEmployees.map(emp => {
                            const isSelf = emp.email === user?.email;
                            const canEdit = 
                                role === "Owner" || 
                                (role === "Manager" && emp.role === "Staff");
                            const canDeactivate =
                                !isSelf &&
                                (role === "Owner" ||
                                    (role === "Manager" && emp.role === "Staff"));

                            return (
                                <tr key={emp.employeeId}>
                                    <td className="name-cell">
                                        {emp.name}
                                        {isSelf && <span className="you-badge">You</span>}
                                    </td>
                                    <td>{emp.email}</td>
                                    <td>
                                        <span className={`role-badge role-${emp.role.toLowerCase()}`}>
                                            {emp.role}
                                        </span>
                                    </td>
                                    <td className="actions-cell">
                                        {canEdit && !isSelf && (
                                            <button
                                                className="btn btn-sm"
                                                onClick={() => openEditModal(emp)}
                                            >
                                                ✏️ Edit
                                            </button>
                                        )}
                                        {canDeactivate && (
                                            <button
                                                className="btn btn-sm btn-danger"
                                                onClick={() => setEmployeeToDeactivate(emp)}
                                            >
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

            {/* ADD EMPLOYEE MODAL */}
            {showAddModal && (
                <div className="modal-backdrop" onClick={() => setShowAddModal(false)}>
                    <div className="modal" onClick={e => e.stopPropagation()}>
                        <h3>Add Employee</h3>

                        <div className="form-field">
                            <label>Name</label>
                            <input
                                placeholder="e.g. John Smith"
                                value={name}
                                onChange={e => setName(e.target.value)}
                            />
                        </div>
                        
                        <div className="form-field">
                            <label>Email</label>
                            <input
                                placeholder="e.g. name@company.com"
                                type="email"
                                value={email}
                                onChange={e => setEmail(e.target.value)}
                            />
                        </div>
                        
                        <div className="form-field">
                            <label>Password</label>
                            <input
                                placeholder="Set a temporary password"
                                type="password"
                                value={password}
                                onChange={e => setPassword(e.target.value)}
                            />
                        </div>

                        <div className="form-field">
                            <label>Role</label>
                            <BeautySelect
                                value={newRole}
                                onChange={(v) => setNewRole(v as any)}
                                placeholder="Select role"
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

            {/* EDIT EMPLOYEE MODAL */}
            {employeeToEdit && (
                <div className="modal-backdrop" onClick={() => setEmployeeToEdit(null)}>
                    <div className="modal" onClick={e => e.stopPropagation()}>
                        <h3>Edit Employee</h3>

                        <div className="form-field">
                            <label>Name</label>
                            <input
                                placeholder="e.g. John Smith"
                                value={editName}
                                onChange={e => setEditName(e.target.value)}
                            />
                        </div>
                        
                        <div className="form-field">
                            <label>Email</label>
                            <input
                                type="email"
                                placeholder="e.g. name@company.com"
                                value={editEmail}
                                onChange={e => setEditEmail(e.target.value)}
                            />
                        </div>

                        <div className="form-field">
                            <label>Role</label>
                            <BeautySelect
                                value={editRole}
                                onChange={(v) => setEditRole(v as any)}
                                disabled={employeeToEdit.role === "Owner"}
                                placeholder="Select role"
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

            {/* DEACTIVATE MODAL */}
            {employeeToDeactivate && (
                <div className="modal-backdrop" onClick={() => setEmployeeToDeactivate(null)}>
                    <div className="modal" onClick={e => e.stopPropagation()}>
                        <h3>Deactivate Employee</h3>
                        <p>
                            Are you sure you want to deactivate{" "}
                            <strong>{employeeToDeactivate.name}</strong>?
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
