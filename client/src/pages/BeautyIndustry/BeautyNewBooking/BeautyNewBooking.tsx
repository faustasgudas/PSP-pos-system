import "./BeautyNewBooking.css";

export default function BeautyNewBooking({
                                             goBack,
                                         }: {
    goBack: () => void;
}) {
    return (
        <div className="new-booking-container">
            <div className="action-bar">
                <h2 className="section-title">New Booking</h2>
                <button className="btn btn-secondary" onClick={goBack}>
                    Back to Reservations
                </button>
            </div>

            <div className="new-booking-card">
                <div className="new-booking-grid">
                    <div className="nb-field">
                        <label>Client Name</label>
                        <input type="text" />
                    </div>

                    <div className="nb-field">
                        <label>Phone</label>
                        <input type="text" />
                    </div>

                    <div className="nb-field">
                        <label>Service</label>
                        <select>
                            <option>Select service</option>
                        </select>
                    </div>

                    <div className="nb-field">
                        <label>Employee</label>
                        <select>
                            <option>Select employee</option>
                        </select>
                    </div>

                    <div className="nb-field">
                        <label>Date</label>
                        <input type="date" />
                    </div>

                    <div className="nb-field">
                        <label>Time</label>
                        <input type="time" />
                    </div>

                    <div className="nb-field full">
                        <label>Notes</label>
                        <textarea rows={3} />
                    </div>
                </div>

                <div className="new-booking-actions">
                    <button className="btn btn-primary">
                        Save Booking
                    </button>
                </div>
            </div>
        </div>
    );
}
