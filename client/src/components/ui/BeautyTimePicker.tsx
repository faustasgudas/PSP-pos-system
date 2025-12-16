import { useEffect, useMemo, useRef, useState } from "react";

function pad2(n: number) {
    return String(n).padStart(2, "0");
}

function buildTimes(stepMin = 15, fromHour = 8, toHour = 20) {
    const out: string[] = [];
    for (let h = fromHour; h <= toHour; h++) {
        for (let m = 0; m < 60; m += stepMin) {
            if (h === toHour && m > 0) continue;
            out.push(`${pad2(h)}:${pad2(m)}`);
        }
    }
    return out;
}

export function BeautyTimePicker(props: {
    label?: string;
    value: string; // HH:mm
    onChange: (hhmm: string) => void;
    disabled?: boolean;
    placeholder?: string;
}) {
    const { label, value, onChange, disabled, placeholder = "Select time" } = props;
    const [open, setOpen] = useState(false);
    const rootRef = useRef<HTMLDivElement | null>(null);

    useEffect(() => {
        const onDocMouseDown = (e: MouseEvent) => {
            const el = rootRef.current;
            if (!el) return;
            if (!el.contains(e.target as Node)) setOpen(false);
        };
        document.addEventListener("mousedown", onDocMouseDown);
        return () => document.removeEventListener("mousedown", onDocMouseDown);
    }, []);

    const times = useMemo(() => buildTimes(15, 8, 20), []);

    return (
        <div className={`bpicker ${disabled ? "is-disabled" : ""}`} ref={rootRef}>
            {label && <div className="bpicker__label">{label}</div>}

            <button
                type="button"
                className={`bpicker__trigger ${open ? "is-open" : ""} ${value ? "has-value" : ""}`}
                onClick={() => !disabled && setOpen((v) => !v)}
                disabled={disabled}
            >
                <div className="bpicker__value">
                    {value ? value : placeholder}
                </div>
                <span className="bpicker__icon" aria-hidden="true">🕒</span>
            </button>

            {open && !disabled && (
                <div className="bpicker__popover">
                    <div className="btime__title">Choose a time</div>
                    <div className="btime__grid">
                        {times.map((t) => (
                            <button
                                key={t}
                                type="button"
                                className={`btime__slot ${t === value ? "is-selected" : ""}`}
                                onClick={() => {
                                    onChange(t);
                                    setOpen(false);
                                }}
                            >
                                {t}
                            </button>
                        ))}
                    </div>
                    <div className="bcal__footer">
                        <button type="button" className="bcal__btn" onClick={() => setOpen(false)}>
                            Close
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}

