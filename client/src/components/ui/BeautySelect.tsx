import { useEffect, useMemo, useRef, useState } from "react";

export type BeautySelectOption = {
    value: string;
    label: string;
    subLabel?: string;
    disabled?: boolean;
};

export function BeautySelect(props: {
    label?: string;
    placeholder?: string;
    value: string;
    options: BeautySelectOption[];
    disabled?: boolean;
    onChange: (nextValue: string) => void;
}) {
    const { label, placeholder = "Select…", value, options, disabled, onChange } = props;
    const [open, setOpen] = useState(false);
    const rootRef = useRef<HTMLDivElement | null>(null);

    const selected = useMemo(() => options.find((o) => o.value === value) ?? null, [options, value]);

    useEffect(() => {
        const onDocMouseDown = (e: MouseEvent) => {
            const el = rootRef.current;
            if (!el) return;
            if (!el.contains(e.target as Node)) setOpen(false);
        };
        document.addEventListener("mousedown", onDocMouseDown);
        return () => document.removeEventListener("mousedown", onDocMouseDown);
    }, []);

    return (
        <div className={`bselect ${disabled ? "is-disabled" : ""}`} ref={rootRef}>
            {label && <div className="bselect__label">{label}</div>}

            <button
                type="button"
                className={`bselect__trigger ${open ? "is-open" : ""} ${selected ? "has-value" : ""}`}
                onClick={() => !disabled && setOpen((v) => !v)}
                disabled={disabled}
                aria-haspopup="listbox"
                aria-expanded={open}
            >
                <div className="bselect__value">
                    <div className="bselect__value-main">
                        {selected ? selected.label : placeholder}
                    </div>
                    {selected?.subLabel && (
                        <div className="bselect__value-sub">{selected.subLabel}</div>
                    )}
                </div>
                <span className="bselect__chev" aria-hidden="true">▾</span>
            </button>

            {open && !disabled && (
                <div className="bselect__menu" role="listbox">
                    {options.map((opt) => {
                        const isSelected = opt.value === value;
                        return (
                            <button
                                key={opt.value}
                                type="button"
                                role="option"
                                aria-selected={isSelected}
                                className={`bselect__option ${isSelected ? "is-selected" : ""}`}
                                disabled={opt.disabled}
                                onClick={() => {
                                    onChange(opt.value);
                                    setOpen(false);
                                }}
                            >
                                <div className="bselect__option-main">{opt.label}</div>
                                {opt.subLabel && <div className="bselect__option-sub">{opt.subLabel}</div>}
                            </button>
                        );
                    })}
                </div>
            )}
        </div>
    );
}

