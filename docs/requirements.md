# PayLink — Requirements Specification (Phase 1)

**Repository Path:** `/docs/requirements.md`  
**Authors:** Youssef Hassib & Ali Saied 
**Declared Technology Stack:** [e.g.,.NET] + [e.g., PostgreSQL]  

---

## 1. Problem Summary
PayLink provides back-office services to a large number of end-customers and agents, enabling payment of a variety of bills, primarily electricity, water, mobile airtime and internet, removing the need to stand in long queues at an agent. All these payments would be made against available funds on the customer's wallet in EGP, through mobile apps or various agent terminal solutions. PayLink charges varying levels of fees for this service and shares 40% of this with the relevant agent.

It is vital that all amounts are correctly accounted for, so all wallet and other relevant balances will be changed only by reference to a further set of unalterable decimal-based records. A number of mechanisms are required, including pre-holding funds, idempotency, detection of fraudulent behaviour, and appropriate states for payments at all times, to avoid over-charging and negative balances. In addition, many different types of interfaces to billers will need to be supported (REST, SOAP, response requested, files), robustly handled in case of delays, failures and other problems, reconciled on a daily basis, and made as resilient as possible to changes at billers.

---

## 2. Stakeholders

| Stakeholder | Role / Type | Primary Goals & Interactions |
| :--- | :--- | :--- |
| **Basic Customer** | End User | Registers an account, tops up digital wallet, checks utility bills, and pays up to 5,000 EGP/day with instant receipts. |
| **Verified Customer** | End User | Upgrades verification level to unlock higher daily payment limits (up to 30,000 EGP/day). |
| **Agent (Shop Terminal)** | Business Partner | Collects cash from walk-in customers, executes bill payments via terminal, and earns a 40% commission on service fees. |
| **Utility Billers** | External Systems | Receive payment requests and return statuses (ElectroCo via REST JSON, AquaCo via SOAP XML, TopUpNet via Async REST + Callback, NetCo via Batch CSV). |
| **System Administrator** | Internal Operator | Manages active/disabled billers, configures fees, limits, and active promotion windows without code redeployment, and views daily financial totals. |
| **Support Employee** | Internal Operator | Searches payment audit trails by reference, investigates reconciliation discrepancies, blocks/unblocks suspicious accounts, and approves refund requests. |
| **Financial Regulators** | External Oversight | Require strict financial accuracy (exact decimals, immutable ledger entries, full audit trails) and zero customer fund loss. |

---

## 3. Functional Requirements (User Stories)

### Part A: Accounts, Wallets, Catalog & Core Payment Lifecycle (`FR-1` to `FR-14`)

#### FR-1: Customer Registration
* **Priority:** Must
* **User Story:** As a new customer, I want to register an account on PayLink so that I can hold funds in a digital wallet and pay utility bills.
* **Acceptance Criteria:**
  * **Given** a visitor provides valid unique registration details,
  * **When** they submit the registration request,
  * **Then** the system creates a new customer account with a `Basic` verification level, initializes a wallet with `0.00 EGP` available and `0.00 EGP` held balances, and records an audit log entry.

#### FR-2: Account Verification Upgrade
* **Priority:** Should
* **User Story:** As a basic customer, I want to upgrade my account to verified status so that my daily payment limit increases from 5,000 EGP to 30,000 EGP.
* **Acceptance Criteria:**
  * **Given** an active customer account currently at the `Basic` verification level,
  * **When** the customer's identity verification is approved,
  * **Then** the account's verification level updates to `Verified`, raising their daily payment limit to `30,000.00 EGP`.

#### FR-3: View Wallet Balances & Transaction History
* **Priority:** Must
* **User Story:** As a customer, I want to view my available balance, held balance, and transaction history so that I can track my money accurately.
* **Acceptance Criteria:**
  * **Given** an authenticated customer with existing wallet ledger entries,
  * **When** they request their wallet summary and history,
  * **Then** the system returns their exact `available_balance` and `held_balance` computed from the ledger alongside a chronological list of transactions.

#### FR-4: Wallet Top-Up
* **Priority:** Must
* **User Story:** As a customer, I want to top up my wallet via a simulated card or agent cash deposit so that I have funds available to pay bills.
* **Acceptance Criteria:**
  * **Given** an active customer wallet and a valid positive top-up amount,
  * **When** the top-up transaction succeeds,
  * **Then** a credit ledger entry is appended, increasing the wallet's `available_balance` by the exact decimal amount.

#### FR-5: Immutable Ledger-Based Balance Calculation
* **Priority:** Must
* **User Story:** As a system architect, I want every balance change to be recorded as an immutable ledger entry so that wallet balances are never overwritten directly and remain fully auditable.
* **Acceptance Criteria:**
  * **Given** any financial event (top-up, hold, capture, release, fee, commission, or refund),
  * **When** the event is processed,
  * **Then** an immutable ledger entry is inserted with exact decimal precision, and the wallet balance reflects the sum of its ledger entries without direct balance overwrites.

#### FR-6: Biller & Service Management (Admin)
* **Priority:** Must
* **User Story:** As an admin, I want to add, edit, and disable billers and services so that the catalog stays up to date without code changes.
* **Acceptance Criteria:**
  * **Given** an authenticated admin user,
  * **When** they create, update, or disable a biller or service in the catalog,
  * **Then** the catalog updates immediately, disabled billers reject new payment attempts, and an audit log entry is recorded.

#### FR-7: Browse Active Billers & Required Fields
* **Priority:** Must
* **User Story:** As a customer, I want to view the list of active billers and the input fields each requires so that I know what information to enter before paying.
* **Acceptance Criteria:**
  * **Given** active billers configured in the Biller Catalog,
  * **When** a customer requests the biller list,
  * **Then** the system returns all active billers along with their service types, required input fields (e.g., meter number, phone number), and min/max payment limits.

#### FR-8: Bill Inquiry & Fee Preview
* **Priority:** Must
* **User Story:** As a customer, I want to run a bill inquiry before paying so that I can see the exact bill amount, PayLink service fee, and total cost.
* **Acceptance Criteria:**
  * **Given** a valid meter or account identifier for an active live biller,
  * **When** the customer submits a bill inquiry,
  * **Then** PayLink queries the biller and displays the due amount, the calculated service fee (accounting for any active promotion), and the exact total (`amount + fee`).

#### FR-9: Pay Bill from Wallet
* **Priority:** Must
* **User Story:** As a customer, I want to pay a utility bill using my wallet balance so that I can settle my bills digitally.
* **Acceptance Criteria:**
  * **Given** an authenticated customer with sufficient `available_balance` who passes all fraud and limit checks,
  * **When** they submit a bill payment request,
  * **Then** PayLink places a hold on `amount + fee`, dispatches the payment to the biller, captures the hold upon confirmation, and transitions the payment to `Completed`.

#### FR-10: Request Idempotency
* **Priority:** Must
* **User Story:** As a customer on an unstable mobile network, I want duplicate payment requests with the same idempotency key to return the original result so that I am never charged twice.
* **Acceptance Criteria:**
  * **Given** a payment request already processed or in progress with idempotency key `K`,
  * **When** the client sends a second request with the same idempotency key `K` and identical payload,
  * **Then** the system does not create a new payment or hold funds again, and instead returns the original payment status and response.

#### FR-11: Strict Payment State Lifecycle Enforcement
* **Priority:** Must
* **User Story:** As a system engineer, I want every payment to follow only the allowed state transitions (`Initiated -> FundsHeld -> SentToBiller -> Completed/Failed/Unknown -> Refunded`) so that invalid state jumps cannot corrupt funds.
* **Acceptance Criteria:**
  * **Given** a payment in any current state (including terminal states `Completed`, `Rejected`, `Refunded`),
  * **When** an event attempts an undefined transition (such as `Unknown -> Refunded` directly without transitioning to `Failed` first),
  * **Then** the state machine rejects the transition and preserves the current valid state.

#### FR-12: Pre-Biller Fund Hold & Failure Release
* **Priority:** Must
* **User Story:** As a customer, I want my funds to be held before the biller is called and automatically released if the biller refuses the payment so that I never lose money on failed transactions.
* **Acceptance Criteria:**
  * **Given** a payment in `Initiated` state that passes validation and has sufficient funds,
  * **When** the payment moves to `FundsHeld`, `total = amount + fee` moves from `available_balance` to `held_balance` before calling the biller; **And When** the biller refuses the payment (`Failed`),
  * **Then** the held funds are released back to `available_balance` and the payment transitions to `Refunded`.

#### FR-13: Biller Timeout Handling & Unknown Retry Schedule
* **Priority:** Must
* **User Story:** As a customer, I want a payment that times out after 10 seconds to enter `Unknown` status with funds still held and automatically re-query the biller so that I am neither double-charged nor refunded prematurely.
* **Acceptance Criteria:**
  * **Given** a payment in `SentToBiller` state where the live biller does not respond within `10 seconds`,
  * **When** the timeout triggers,
  * **Then** the payment transitions to `Unknown`, funds remain in `held_balance`, the customer is notified it is still processing, and the system schedules up to 3 status re-queries at `1 minute`, `5 minutes`, and `15 minutes` before deferring to nightly reconciliation.

#### FR-14: Asynchronous Callback Handling & Deduplication (TopUpNet)
- **Priority:** Must
- **User Story:** As the PayLink backend, I want to accept asynchronous callbacks from TopUpNet and ignore duplicate callbacks so that delayed mobile top-ups complete exactly once.
- **Acceptance Criteria:**
  - **Given** a TopUpNet payment awaiting asynchronous confirmation,
  - **When** TopUpNet invokes the callback URL with `"success"` (or `"failed"`),
  - **Then** the payment transitions to `Completed` (or `Failed` -> `Refunded`) once; **And When** TopUpNet sends a duplicate callback for the same reference, the system logs and ignores it without altering balances.

---

### Part B: Multi-Biller Support, Fees, Fraud, Operations & Reconciliation (`FR-15` to `FR-28`)

#### FR-15: Multi-Biller Protocol Integration
- **Priority:** Must
- **User Story:** As a system architect, I want PayLink to integrate with at least three billers using distinct protocols (ElectroCo via REST JSON, AquaCo via SOAP XML, TopUpNet via asynchronous REST callbacks, and optional NetCo via batch CSV) through isolated adapters so that adding or updating a biller never changes core payment logic.
- **Acceptance Criteria:**
  - **Given** active biller configurations for ElectroCo, AquaCo, and TopUpNet,
  - **When** payments or inquiries are routed to each biller,
  - **Then** the corresponding adapter translates the request/response format, enforces AquaCo's rule rejecting payments above the billed amount, and isolates protocol-specific changes (such as ElectroCo v2) from all other modules.

#### FR-16: Dynamic Fee Calculation & Promotion Period
- **Priority:** Must
- **User Story:** As a customer, I want PayLink to calculate service fees accurately based on biller rules and active promotions so that I am charged the exact expected fee.
- **Acceptance Criteria:**
  - **Given** a payment request for a specific biller at a given timestamp,
  - **When** the fee engine computes the service fee,
  - **Then** it applies `2.50 EGP` fixed for electricity/water, `1.5%` (minimum `1.00 EGP`) for mobile top-up, `2.00 EGP` up to `200.00 EGP` or `4.00 EGP` above `200.00 EGP` for internet, and `0.00 EGP` if the biller is currently within an active promotion window.

#### FR-17: Agent-Assisted Payments & Commission Credit
- **Priority:** Should
- **User Story:** As a PayLink shop agent, I want to pay utility bills on behalf of walk-in customers and earn a 40% commission on the service fee so that I am compensated for operating a payment terminal.
- **Acceptance Criteria:**
  - **Given** an authenticated agent initiating a bill payment on behalf of a customer,
  - **When** the payment transitions to `Completed`,
  - **Then** the system credits the agent's wallet with an immutable ledger entry equal to `40%` of the collected service fee (`0.00 EGP` during a zero-fee promotion).

#### FR-18: Single-Payment & Daily Limit Enforcement
- **Priority:** Must
- **User Story:** As a risk controller, I want payments outside single-transaction bounds or exceeding the account's daily spending limit to be rejected immediately so that financial exposure is bounded.
- **Acceptance Criteria:**
  - **Given** a customer initiating a payment,
  - **When** the bill amount is below `5.00 EGP` or above `10,000.00 EGP`, or causes the customer's daily cumulative payments to exceed `5,000.00 EGP` (`Basic`) or `30,000.00 EGP` (`Verified`),
  - **Then** the payment transitions immediately from `Initiated` to `Rejected` with a clear error reason and no funds are held.

#### FR-19: Velocity Fraud Rule (10-Minute Auto-Block)
- **Priority:** Should
- **User Story:** As a security engineer, I want an account that attempts more than 5 payments within 60 seconds to be automatically blocked for 10 minutes so that automated abuse or credential stuffing is stopped.
- **Acceptance Criteria:**
  - **Given** an account that has already initiated 5 payments within a rolling 60-second window,
  - **When** a 6th payment request is submitted within that same 60-second window,
  - **Then** the 6th payment is `Rejected`, the account is automatically blocked for `10 minutes`, and any new payment attempts during the block window are rejected until the 10-minute timer expires.

#### FR-20: Support Payment Search & Full History
- **Priority:** Must
- **User Story:** As a support employee, I want to search for any payment by its unique reference and inspect its full state history and ledger entries so that I can assist customers with payment issues.
- **Acceptance Criteria:**
  - **Given** an authenticated support employee and a valid payment reference,
  - **When** they query the payment lookup endpoint,
  - **Then** the system returns the payment metadata, all chronological state transitions with timestamps, biller responses, and associated ledger entries.

#### FR-21: Support Account Block & Unblock
- **Priority:** Should
- **User Story:** As a support employee, I want to manually block and unblock customer or agent accounts so that fraudulent or compromised accounts can be frozen and restored after review.
- **Acceptance Criteria:**
  - **Given** an authenticated support employee and a target user account,
  - **When** the support employee executes a block (or unblock) action with a reason,
  - **Then** the account status updates immediately, blocked accounts cannot initiate new payments, and an audit log entry records who performed the action, when, and why.

#### FR-22: Completion & Failure Notifications (SMS & Push)
- **Priority:** Must
- **User Story:** As a customer, I want to receive both an SMS and a push notification whenever my payment completes or fails so that I have immediate confirmation of my bill status.
- **Acceptance Criteria:**
  - **Given** a payment that transitions to `Completed` or `Failed` (`Refunded`),
  - **When** the notification module processes the payment outcome event,
  - **Then** the system dispatches an SMS receipt (or failure explanation) and a push notification to the customer without blocking the core payment transaction.

#### FR-23: Notification Retry & Undelivered Marking
- **Priority:** Should
- **User Story:** As a system operator, I want failed SMS or email notifications to be retried up to 3 times and then marked undelivered without rolling back the payment so that transient messaging outages do not corrupt completed financial transactions.
- **Acceptance Criteria:**
  - **Given** a `Completed` payment whose SMS or email notification fails to send due to provider unavailability,
  - **When** the notification delivery worker handles the failure,
  - **Then** the payment remains `Completed`, and the notification is retried up to `3 times` before being marked as `Undelivered` in the notification log.

#### FR-24: Runtime Configuration of Fees & Limits (Admin)
- **Priority:** Should
- **User Story:** As an admin, I want to update service fee rules, daily/single payment limits, and promotional zero-fee periods via configuration or admin endpoints without redeploying the application so that business rules can adapt rapidly.
- **Acceptance Criteria:**
  - **Given** an authenticated admin updating a fee rule, limit threshold, or promotion date range,
  - **When** the configuration change is saved,
  - **Then** subsequent fee calculations and limit validations use the updated values immediately without requiring service restart, and the change is written to the audit log.

#### FR-25: Comprehensive Financial & Admin Audit Log
- **Priority:** Must
- **User Story:** As a compliance auditor, I want every money movement and administrative configuration change recorded in an immutable audit log so that every system action is traceable.
- **Acceptance Criteria:**
  - **Given** any wallet ledger movement or admin/support action (such as biller edits, config changes, account blocks, or refund approvals),
  - **When** the operation executes,
  - **Then** an immutable audit record is persisted capturing the actor (`who`), timestamp (`when`), action type, target entity, and reason/details (`why`).

#### FR-26: Daily Settlement Reconciliation Report
- **Priority:** Should
- **User Story:** As a finance operator, I want the system to compare daily internal payment records against each biller's settlement file and generate a discrepancy report so that mismatched statuses are flagged for review.
- **Acceptance Criteria:**
  - **Given** a daily biller settlement file listing confirmed payments and amounts,
  - **When** the nightly reconciliation job runs against PayLink's payment records,
  - **Then** any `Unknown` payment present in the file is finalized to `Completed`, any `Unknown` payment absent from the file is marked `Failed` and refunded, and any discrepancy (such as a payment marked `Failed` in PayLink but listed as paid in the settlement file) is flagged in the reconciliation report for support review.

#### FR-27: Post-Completion Refund Request & Approval
- **Priority:** Could
- **User Story:** As a customer, I want to request a refund for a completed payment and have a support employee approve it so that erroneous or disputed completed payments can be reversed safely.
- **Acceptance Criteria:**
  - **Given** a payment in `Completed` state and a submitted customer refund request,
  - **When** an authenticated support employee approves the refund request,
  - **Then** the system creates compensating credit ledger entries returning the funds to the payer's `available_balance` and logs the approval in the audit trail.

#### FR-28: Admin Daily Financial & Failure Totals Dashboard
- **Priority:** Could
- **User Story:** As an admin, I want to view daily aggregated totals for completed payments, service fees earned, agent commissions paid, and failures per biller so that I can monitor business health.
- **Acceptance Criteria:**
  - **Given** processed transactions across one or more billers for a target date,
  - **When** an authenticated admin requests the daily summary report,
  - **Then** the system returns accurate decimal totals for payment volume, fees earned, commissions paid, and failure counts grouped by biller.

---

## 4. Non-Functional Requirements (Measurable)

| ID | Quality Attribute | Requirement Description | Measurable Target & Verification Method |
| :--- | :--- | :--- | :--- |
| **NFR-1** | **Correctness & Concurrency** | Concurrent payment requests from the same wallet and duplicate idempotency requests must never overdraw funds or double-charge. | **0 double charges** and **0 negative balances** across automated concurrency and idempotency integration tests (Hard Cases #1 & #2). |
| **NFR-2** | **Performance (Throughput)** | The payment processing pipeline must sustain high transaction volume during peak billing days. | **>= 100 payments/second** sustained for **1 minute** against mock billers without errors or timeouts. |
| **NFR-3** | **Resilience & Fault Isolation** | Latency, timeouts, or outages from one biller (or the SMS provider) must not block payments to other billers or fail completed transactions. | **100% availability** for healthy billers when one mock biller is down/slow, and **0 failed payments** when the SMS mock fails (Hard Cases #3, #4, #5). |
| **NFR-4** | **Modifiability** | Onboarding a new biller or absorbing a biller API version change must be isolated to the integration layer and configuration. | **0 files modified** in core domain modules (`Accounts`, `Payment Processing`, `Fees`, `Notifications`) when adding a new biller or applying ElectroCo v2. |
| **NFR-5** | **Auditability & Precision** | All financial calculations must use exact decimal arithmetic, and every balance change or admin action must be logged. | **100% of money movements** have a corresponding ledger + audit record (`who`, `when`, `why`), and **0 floating-point types** used for currency. |
| **NFR-6** | **Testability** | Core business rules, state transitions, and fee/fraud logic must be thoroughly covered by automated tests in CI. | **>= 70% line coverage** on domain logic reported automatically in GitHub Actions on every push. |

---

## 5. Ambiguities & Decisions Log

| # | Ambiguity / Question Identified | Options Considered | Final Team Decision | Impacted Modules / Diagrams |
| :--- | :--- | :--- | :--- | :--- |
| **1** | **Daily Limit Calculation Scope:** Does the 5,000 / 30,000 EGP daily limit include the service fee, count `FundsHeld`/`Unknown` payments, and reset at midnight? | (A) Bill amount only vs. Total (`amount + fee`)<br>(B) Midnight Cairo time vs. Rolling 24h | Applies strictly to the principal bill amount (excluding service fee); counts `FundsHeld`, `SentToBiller`, `Unknown`, and `Completed` payments within the calendar day (`00:00–23:59 EET`). | Fraud Checks, Payment Processing, ER Diagram |
| **2** | **Single Payment Limit Scope (5–10,000 EGP):** Do min/max limits apply to the bill amount or `bill + fee`? | (A) Bill amount only<br>(B) Total charged (`bill + fee`) | Applies strictly to the principal bill amount before adding PayLink's service fee. | Biller Catalog, Fraud Checks |
| **3** | **Agent Wallet & Commission Mechanics (`FR-17`):** Which wallet is debited when an agent pays for a customer, and what is the commission during a 0-EGP promotion? | (A) Agent's own pre-funded wallet is debited (cash collected at shop)<br>(B) Customer's wallet is debited | **Option A:** Agent's pre-funded wallet is held and debited for `amount + fee`, and credited `40%` of the fee upon `Completed`. During a 0-EGP promotion, commission is `0.00 EGP` (no commission ledger entry created). | Accounts & Wallet, Fees & Commissions, ER & Sequence Diagrams |
| **4** | **Fee Refund Policy (`FR-27` vs. Failed Payments):** When a `Completed` payment is refunded after support approval, is the service fee refunded too? | (A) Refund bill amount + fee (claw back agent commission)<br>(B) Refund bill amount only (fee non-refundable once service delivered) | **Option B:** For post-completion support-approved refunds (`FR-27`), only the principal bill amount is refunded; the service fee is non-refundable once the biller confirmed delivery. (For pre-completion failures `Failed -> Refunded`, the entire held `amount + fee` is released.) | Accounts & Wallet, Payment Processing, Refund Sequence Diagram |
| **5** | **Partial vs. Exact Bill Payments:** Can customers pay less than the inquired bill amount for ElectroCo/AquaCo? | (A) Exact inquired amount required for utility bills; custom amount allowed for TopUpNet<br>(B) Partial payments allowed | **Option A:** Utility bills (ElectroCo, AquaCo) require paying the exact inquired bill amount; mobile top-ups (TopUpNet) allow custom amounts within `[5.00, 10,000.00] EGP`. | Biller Integration, Payment Processing |
| **6** | **TopUpNet Timeout vs. Async Callback:** Does the 10-second timeout apply only to TopUpNet's initial `"accepted"` response, or does the payment enter `Unknown` until the callback arrives? | (A) 10s timeout applies to initial HTTP `"accepted"` ACK; payment stays `SentToBiller` awaiting callback<br>(B) Transitions to `Unknown` if callback takes >10s | **Option A:** The 10-second HTTP timeout applies to TopUpNet's initial synchronous `"accepted"` response. Once `"accepted"` is received, the payment remains in `SentToBiller` awaiting the async callback (and falls back to scheduled status re-queries if no callback arrives within 1 minute). | Payment Processing, Biller Integration, State Diagram |
| **7** | **Velocity Rule (`>5 payments in 60s`) Edge Cases:** Do `Rejected` attempts count toward the 5-payment threshold, and does the 10-minute block auto-expire? | (A) Count all initiated attempts; auto-expire block after 10 minutes<br>(B) Count only non-rejected attempts | **Option A:** All payment initiation attempts within a rolling 60-second window count toward the 5-payment threshold; the 6th attempt triggers an automatic 10-minute block that expires automatically after 10 minutes. | Fraud Checks, Accounts & Wallet |
| **8** | **Overlapping Promotions (`FR-24`):** How does the system guarantee only *one* biller has a 0-EGP fee during a promotion period? | (A) Reject admin creation/update if date ranges overlap an active promotion<br>(B) Latest promotion overrides | **Option A:** The admin configuration endpoint validates promotion date intervals and rejects any new or updated promotion whose `[start_date, end_date]` overlaps an existing promotion. | Fees & Commissions, Admin API |
| **9** | **Idempotency Key Scope & Payload Mismatch (`FR-10`):** Is the key scoped per customer, and what happens if the same key is sent with a different amount/meter? | (A) Scoped per account; return `HTTP 409 Conflict` if request payload differs from original<br>(B) Globally unique key | **Option A:** Idempotency keys are scoped per payer account (`account_id + idempotency_key`). Reusing a key with an identical payload returns the original response; reusing it with a modified payload returns `HTTP 409 Conflict`. | Payment Processing, API Contract |
| **10** | **Blocked Account Scope (`FR-21`):** When an account is blocked, can in-flight `SentToBiller` callbacks/retries and wallet top-ups still complete? | (A) Block only new payment initiations; allow in-flight callbacks/reconciliation to settle<br>(B) Freeze all wallet activity | **Option A:** Blocking an account rejects all new payment initiations and wallet top-ups, while allowing already in-flight `SentToBiller`/`Unknown` payments to settle via callbacks, retries, or reconciliation so funds are not stranded. | Accounts & Wallet, Payment Processing |
| **11** | **NetCo Batch Integration Scope (`FR-15`):** Since NetCo is marked optional in `FR-15`, is it included in Phase 2 implementation? | (A) Implement ElectroCo, AquaCo, and TopUpNet; model NetCo in catalog/docs as future extension<br>(B) Implement all four | **Option A:** Core implementation targets the three live billers (ElectroCo, AquaCo, TopUpNet), while NetCo is modeled in the Biller Catalog and adapter architecture to demonstrate zero-core-change extensibility. | Biller Catalog, Biller Integration |