
# Reasons for Deviating from the Original Design

### 1. Gift cards were not defined

The original design document did not specify a **GiftCard entity** or describe how gift cards should be stored, audited. Due to the lack of a clear data model and business rules, implementing gift cards would be ambiguous and unreliable.

### 2. Missing auditability and snapshots

The design did not account for **immutable snapshots** of prices, taxes, or discounts. In real-world POS systems, historical accuracy is critical, and values must be preserved as they were at the time of transaction.

### 3. Employee entity created a bottleneck

The original model over-centralized business logic around the **Employee** entity, which reduced flexibility for order and reservation management.

### Because we used different data model we did not use their yamal.

## Reasons for Deviating from our YAMAL:

Our YAML defines order modification via a single PATCH /orders/{id} endpoint, we split this into explicit endpoints for adding/updating/removing lines because each operation has critical side-effects (inventory movements, tax/discount snapshots) that must be validated deterministically. We also implemented a dedicated “move order lines” endpoint to support POS-style bill splitting, which is not defined in the YAML contract.



#Score for the design document: 5.5 / 10

##Why:

-  Data model exists, but not deep enough (missing cardinalities, snapshot/audit rules, delete vs archive).

-  No package diagram → architecture/module boundaries unclear (harder to map to code structure).

-  wireframes.

-  Endpoint behavior is underwritten → lacks error cases.


Overall: reads like a high-level overview, not a “buildable spec.”
