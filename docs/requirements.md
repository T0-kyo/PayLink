# PayLink — Requirements Specification (Phase 1)

**Repository Path:** `/docs/requirements.md`  
**Authors:** Youssef Hassib & Ali Saied 
**Declared Technology Stack:** [e.g.,.NET] + [e.g., PostgreSQL]  

---

## 1. Problem Summary
*(Author: Youssef — Write 2–3 paragraphs in your own words summarizing PayLink's purpose, how customers and shop agents pay utility bills via digital wallets, why ledger-backed holds and strict state transitions prevent double charges or lost funds, and how the system handles unreliable third-party biller protocols and daily settlement reconciliation.)*
PayLink is simply the guy in between the customer and the billers?

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
*All 28 requirements from Section 3.2 of the brief are retained and prioritized using MoSCoW (`Must`, `Should`, `Could`).*

### Part A: Accounts, Wallets, Catalog & Core Payment Lifecycle (`FR-1` to `FR-14`)
*(Author: Youssef)*

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
* **Priority:** Must
* **User Story:** As the PayLink backend, I want to accept asynchronous callbacks from TopUpNet and ignore duplicate callbacks so that delayed mobile top-ups complete exactly once.
* **Acceptance Criteria:**
  * **Given** a Top