# End-to-End Demo Plan: Business Hours Assistant

This document outlines a concise, testable end-to-end demonstration of the Meta AI-powered local business assistant.

---

## 1. Prerequisites

- **Node.js**: v20 or higher (`node -v`)
- **Dependencies**: Installed via `npm install` (zero runtime dependencies, devDependencies only).
- **Network**: Internet access is **not required** for the default demo (offline mock mode operates deterministically).

---

## 2. Automated Test Verification

Before running manual queries, verify that the complete test suite passes:

```bash
npm test
```

Expected output:
- 7 test suites passed (domain, application, and adapters).
- 41 tests passed, 0 failures.

---

## 3. Interactive Server Demo

### Step 1: Start the Assistant Server

Run the development server in a terminal:

```bash
npm run dev
```

Output should confirm:
```
[Assistant Server] Running at http://127.0.0.1:3000
[Assistant Server] Meta Model: muse-spark-1.3 (API Key: (not set))
[Assistant Server] Running in offline mode with MockMetaModelAdapter (zero external dependencies).
```

---

### Step 2: Health Check

In a second terminal, verify the server is responding:

```bash
curl http://127.0.0.1:3000/health
```

Expected response:
```json
{
  "status": "ok",
  "service": "MyVirtualCommunity AI Hackathon Assistant",
  "metaModelConfigured": false,
  "model": "muse-spark-1.3",
  "timestamp": "..."
}
```

---

### Step 3: Scenario A — Business is Open (Normal Operating Hours)

**Context:**
- Business: `restaurant:wing-spot` (Wing Spot Gómez Morín)
- Simulated Time: Monday, 2026-10-26 at 14:00 local (20:00 UTC)
- Schedule: 09:00 to 18:00, Mon–Sat

**Request:**
```bash
curl -X POST http://127.0.0.1:3000/api/assistant/query \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "restaurant:wing-spot",
    "userQuery": "¿A qué hora cierran hoy?",
    "evaluatedAt": "2026-10-26T20:00:00.000Z"
  }'
```

**Expected Result:**
- `status`: `"OPEN"`
- `isOpen`: `true`
- `admissionPermitted`: `true`
- `answer`: Confirms Wing Spot is open, closing at 18:00, with last entry at 17:45.

---

### Step 4: Scenario B — Business is Closing Soon (Cutoff Window Reached)

**Context:**
- Same business, evaluated at 17:50 local (23:50 UTC).
- Since closing time is 18:00 and cutoff is 15 minutes before close (17:45), the cutoff has passed.

**Request:**
```bash
curl -X POST http://127.0.0.1:3000/api/assistant/query \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "restaurant:wing-spot",
    "userQuery": "¿Todavía alcanzo a entrar a cenar?",
    "evaluatedAt": "2026-10-26T23:50:00.000Z"
  }'
```

**Expected Result:**
- `status`: `"CLOSING_SOON"`
- `isOpen`: `true`
- `admissionPermitted`: `false`
- `answer`: Informs the customer that the restaurant is closing soon at 18:00 and no new admissions are accepted.

---

### Step 5: Scenario C — Business is Closed (Non-Operating Day)

**Context:**
- Evaluated on Sunday, 2026-10-25 at 14:00 local (20:00 UTC).
- Wing Spot does not operate on Sundays.

**Request:**
```bash
curl -X POST http://127.0.0.1:3000/api/assistant/query \
  -H "Content-Type: application/json" \
  -d '{
    "businessId": "restaurant:wing-spot",
    "userQuery": "¿Están abiertos hoy domingo?",
    "evaluatedAt": "2026-10-25T20:00:00.000Z"
  }'
```

**Expected Result:**
- `status`: `"CLOSED"`
- `isOpen`: `false`
- `admissionPermitted`: `false`
- `answer`: Explains that Wing Spot is closed on Sundays and lists regular opening days.

---

## 4. Live Meta Model API Smoke Test (Optional)

When a developer possesses a valid Meta Model API key and wishes to test the live cloud service:

1. Create a `.env` file from `.env.example`:
   ```bash
   cp .env.example .env
   ```
2. Set `MODEL_API_KEY`:
   ```env
   MODEL_API_KEY=your_meta_model_api_key_here
   ```
3. Run the dedicated smoke test command:
   ```bash
   npm run test:meta-smoke
   ```

*(Note: Never run this command without configuring credentials, and never commit `.env`.)*
