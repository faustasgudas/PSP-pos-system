import { useState } from "react";
import "./BeautyNewBooking.css";
import * as reservationService from "../../../services/reservationService";
import type { CatalogItemSummaryResponse } from "../../../types/api";
import type { EmployeeSummaryResponse } from "../../../types/api";

interface BeautyNewBookingProps {
    goBack: () => void;
    services: CatalogItemSummaryResponse[];
    employees: EmployeeSummaryResponse[];
    businessId: number;
    onBookingCreated: () => void;
}

export default function BeautyNewBooking({
    goBack,
    services,
    employees,
    businessId,
    onBookingCreated,
}: BeautyNewBookingProps) {
    const [selectedServiceId, setSelectedServiceId] = useState<number | "">("");
    const [selectedEmployeeId, setSelectedEmployeeId] = useState<number | "">("");
    const [date, setDate] = useState("");
    const [startTime, setStartTime] = useState("");
    const [duration, setDuration] = useState(60);
    const [notes, setNotes] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        
        if (!selectedServiceId || !date || !startTime) {
            setError("Please fill in all required fields");
            return;
        }

        setIsSubmitting(true);
        setError(null);

        try {
            // Combine date and time - ensure we're working in local timezone
            // Create date string in format: YYYY-MM-DDTHH:mm (local time)
            const dateTimeString = `${date}T${startTime}`;
            const appointmentStart = new Date(dateTimeString);
            
            // Validate the date was parsed correctly
            if (isNaN(appointmentStart.getTime())) {
                throw new Error('Invalid date or time selected');
            }
            
            const appointmentEnd = new Date(appointmentStart.getTime() + duration * 60000);

            await reservationService.createReservation(businessId, {
                catalogItemId: Number(selectedServiceId),
                employeeId: selectedEmployeeId ? Number(selectedEmployeeId) : undefined,
                appointmentStart: appointmentStart.toISOString(),
                appointmentEnd: appointmentEnd.toISOString(),
                plannedDurationMin: duration,
                notes: notes || undefined,
            });

            onBookingCreated();
        } catch (err) {
            setError(err instanceof Error ? err.message : "Failed to create booking");
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="new-booking-container">
            <div className="action-bar">
                <h2 className="section-title">New Booking</h2>
                <button className="btn btn-secondary" onClick={goBack}>
                    Back to Reservations
                </button>
            </div>

            {error && (
                <div style={{ color: "red", padding: "1rem", marginBottom: "1rem" }}>
                    {error}
                </div>
            )}

            <form onSubmit={handleSubmit}>
                <div className="new-booking-card">
                    <div className="new-booking-grid">
                        <div className="nb-field">
                            <label>Service *</label>
                            <select
                                value={selectedServiceId}
                                onChange={(e) => setSelectedServiceId(e.target.value ? Number(e.target.value) : "")}
                                required
                            >
                                <option value="">Select service</option>
                                {services.map(service => (
                                    <option key={service.catalogItemId} value={service.catalogItemId}>
                                        {service.name} - €{service.basePrice.toFixed(2)}
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div className="nb-field">
                            <label>Employee</label>
                            <select
                                value={selectedEmployeeId}
                                onChange={(e) => setSelectedEmployeeId(e.target.value ? Number(e.target.value) : "")}
                            >
                                <option value="">Select employee (optional)</option>
                                {employees.map(employee => (
                                    <option key={employee.employeeId} value={employee.employeeId}>
                                        {employee.name} ({employee.role})
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div className="nb-field">
                            <label>Date *</label>
                            <input
                                type="date"
                                value={date}
                                onChange={(e) => setDate(e.target.value)}
                                required
                            />
                        </div>

                        <div className="nb-field">
                            <label>Start Time *</label>
                            <input
                                type="time"
                                value={startTime}
                                onChange={(e) => setStartTime(e.target.value)}
                                required
                            />
                        </div>

                        <div className="nb-field">
                            <label>Duration (minutes) *</label>
                            <input
                                type="number"
                                value={duration}
                                onChange={(e) => setDuration(Number(e.target.value))}
                                min="15"
                                step="15"
                                required
                            />
                        </div>

                        <div className="nb-field full">
                            <label>Notes</label>
                            <textarea
                                rows={3}
                                value={notes}
                                onChange={(e) => setNotes(e.target.value)}
                            />
                        </div>
                    </div>

                    <div className="new-booking-actions">
                        <button
                            type="submit"
                            className="btn btn-primary"
                            disabled={isSubmitting}
                        >
                            {isSubmitting ? "Saving..." : "Save Booking"}
                        </button>
                    </div>
                </div>
            </form>
        </div>
    );
}
