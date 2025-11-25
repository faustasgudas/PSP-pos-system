import "../../../App.css"

function BeautyCheckout(){
    
    return(
        <div className="content-box">
            <div className="top-bar">
                <h1 className="title">SuperApp</h1>
            </div>
            <div className="checkout">
                <h2 className="checkout-text">Checkout</h2>
                <p>Select payment method:</p>
                <select
                    id = "payment-method"
                    className="payment-method-dropdown"
                >
                    <option value="">Select payment method</option>
                    <option value="cash">Cash</option>
                    <option value="card">Card</option>
                    <option value="giftcard">Gift card</option>
                </select>
            </div>
        </div>
    )
}

export default BeautyCheckout;