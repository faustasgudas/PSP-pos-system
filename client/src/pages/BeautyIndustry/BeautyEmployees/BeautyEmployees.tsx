import "./BeautyEmployees.css";

interface Employee {
    id: number;
    name: string;
    role: string;
}

interface BeautyEmployeesProps {
    employees: Employee[];
}

export default function BeautyEmployees({ employees }: BeautyEmployeesProps) {
    return (
        <div className="employees-container">
            <div className="action-bar">
                <h2 className="section-title">Employees</h2>
                <button className="btn btn-primary">
                    <span>➕</span> Add Employee
                </button>
            </div>

            <div className="employees-list">
                {employees.length > 0 ? (
                    employees.map(e => (
                        <div key={e.id} className="employee-card">
                            <div className="employee-header">
                                <div className="employee-avatar">
                                    {e.name[0].toUpperCase()}
                                </div>

                                <div>
                                    <div className="employee-name">{e.name}</div>
                                    <div className="employee-role">{e.role}</div>
                                </div>
                            </div>

                            <div className="employee-actions">
                                <button className="btn-small">Edit</button>
                                <button className="btn-small btn-danger">Remove</button>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="no-employees">No employees found</div>
                )}
            </div>
        </div>
    );
}
