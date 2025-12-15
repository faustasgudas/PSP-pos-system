import "../../../App.css";
import "./CateringTables.css";

interface Table {
    id: number;
    seats: number;
    status: string;
}

interface CateringTablesProps {
    tables: Table[];
}

export default function CateringTables({tables}: CateringTablesProps) {
    return (
        <div className="tables-container">
            <div className="action-bar">
                <h2 className="section-title">Table Management</h2>
                <div className="muted">{tables.length} table(s)</div>
                <button className="btn btn-primary">
                    <span>➕</span> Add Table
                </button>
            </div>
            <div className="tables-grid" id="tables-grid">
                {tables.length === 0 ? (
                    <div className="muted">No tables loaded in this screen yet.</div>
                ) : (
                    tables.map((t) => (
                        <div key={t.id} className="card" style={{ padding: 12, textAlign: "left" }}>
                            <strong>Table #{t.id}</strong>
                            <div className="muted">Seats: {t.seats}</div>
                            <div className="muted">Status: {t.status}</div>
                        </div>
                    ))
                )}
            </div>
        </div>
    )
}