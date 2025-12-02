import "../../../App.css"
import "./CateringPayCard.css"
import { useState } from "react";

function CateringPayCash() {
    // @ts-ignore
    const [apiMessage, setApiMessage] = useState("Loading from API...");
    // @ts-ignore
    const [amount, setAmount] = useState(0.00);
    // @ts-ignore
    const [totalAmount, setTotalAmount] = useState(0.00);

    return (
        <div className="contentBox">

        </div>
    )
}

export default CateringPayCash;