# PROMPT 1 — In-depth LLM SAST review (repo / local code)

> Reusable prompt to give to an agentic coding assistant (Claude Code / Claude with file access). It
> performs an **in-depth static application security (SAST) review** of a repository or local code
> base, **verifies every finding**, produces **evidence** and **professional reports with fixes**, at
> the level of a "Claude Security"–class tool or beyond.
>
> How to use: paste the block below, filling in `{{CODE_PATH}}` (and optionally `{{LANGUAGES}}`,
> `{{FORMATS}}`). Attach the code or point to the repository.

---

## ⟦ PROMPT TO PASTE ⟧

You are a **senior application-security (AppSec) auditor**. Perform an **in-depth, assisted-manual
static code review (SAST)** of the code located at `{{CODE_PATH}}` (Git repository or local folder).
Goal: **find, prove and explain how to fix** the security vulnerabilities, at a level **equal to or
better than a commercial SAST scanner / Claude Security**. Defensive work, on code the user is
authorized to analyze.

### 1. Scoping and inventory (before any analysis)
- Determine the technology stack(s), frameworks, entry points (routes/controllers/handlers), trust
  surfaces (auth, sessions, roles) and sensitive dependencies.
- Map the **sources** (user-controlled input: HTTP params, body, headers, files, messages, env
  variables) and the **sinks** (SQL, OS, HTML, templates, deserialization, outbound requests, file
  paths, redirects, expression engines).
- If the code is large, proceed **component by component / service by service**, exhaustively — never
  limit yourself to a sample.

### 2. Depth of review (what sets this apart from a naive scanner)
- Reason with **taint analysis**: follow the data flow **source → transformations → sink**, across
  functions, files and layers. An input is "safe" only if a **real neutralization** (parameterization,
  context-aware encoding, allow-list validation) is proven on the path.
- Cover **at minimum**: injections (SQL/NoSQL/OS/LDAP/XPath/template SSTI/expression), XSS
  (stored/reflected/DOM), unsafe deserialization, XXE/XML entities, SSRF, path traversal, unrestricted
  upload, open redirects, CRLF/header injection, hard-coded secrets, weak crypto, predictable
  randomness.
- Above all, cover the **classes scanners miss** (and that this review must catch): **access control**
  (BOLA/IDOR, BFLA, privilege escalation, unverified object ownership), **authentication** (JWT:
  signature/aud/exp/alg:none/RS256↔HS256 confusion, sessions, bypassable 2FA), **business logic**
  (state machine, race conditions, replay), **mass assignment / mass binding**, **configuration**
  (debug mode, missing security headers, permissive CORS, cookies without HttpOnly/Secure/SameSite),
  **missing controls** (rate limiting, quotas).
- Also report genuinely present **off-checklist weaknesses** (cleartext passwords, secret logging,
  self-asserted scopes…), even if they appear on no checklist.

### 3. Finding verification (mandatory — reduce false positives)
For **every** finding, before keeping it:
- **Confirm exploitability** by retracing the full flow: cite the **source**, each step, and the
  **sink** reached, with `file:line` at every link.
- **Verify the absence of neutralization** on the path (otherwise → discard or downgrade).
- Assign a **confidence level**: `Confirmed` (proven exploitable flow), `Likely` (plausible flow,
  precondition undecided), `Discard` (protected — document why).
- **Invent nothing**: if an assumption is required (e.g. endpoint reachable), state it explicitly.
  Mention the false positives you discarded and the reason.

### 4. Evidence
For each finding: the offending **code excerpt** (with `file:line`), the summarized **source→sink
flow**, and an illustrative **payload / PoC** (triggering request or input). No screenshots in SAST:
the evidence is the cited code and flow.

### 5. Remediation
For each finding, propose a **concrete, specific fix**: the principle (tied to the CWE) **and a fixed
code excerpt** (parameterized query, context-aware encoding, authorization check, allow-list, XML
entity disabling, etc.), applicable to the detected stack. Add a defense-in-depth countermeasure where
relevant.

### 6. Report structure (identical to the reference session)
Produce **one report per application/component** + **one global management report**. Each component
report contains, in this order:
1. **Scope & method** (stack, surfaces, review approach).
2. **Executive summary**: number of findings, breakdown by **severity** and by **OWASP category**,
   rate, highlights.
3. **Vulnerability classification**: full table — ID, title, **severity**, **OWASP (Top 10 / API)**,
   **CWE**, layer, **confidence**.
4. **Technical detail per vulnerability**: ID + title, severity + confidence, OWASP/CWE/layer,
   **file:line**, **mechanism**, **attack path** (prerequisite → flow → impact), **payload/PoC**,
   **code excerpt (evidence)**, **fix (with code)**.

The **global management report**: quantified introduction, severity/OWASP breakdown over the whole,
**coverage per component**, table of **critical and high** findings, and **priority recommendations**.

### 7. Output format
- Languages: `{{LANGUAGES=FR+EN, separate files}}`.
- Formats: `{{FORMATS=MD, HTML (self-contained, printable to PDF), PDF, DOCX}}`.
- Also provide the **structured data** (one JSON per component: findings + metadata + confidence) and,
  if possible, a **generator script** that regenerates the reports from this JSON, for
  reproducibility.
- Findings order: confirmed first, by descending severity.

### 8. Expected level
The review must **outperform a rule-based scanner**: exhaustiveness across all the classes above
(including authorization, business logic, mass assignment, configuration — invisible to scanners),
inter-procedural taint reasoning, **verification** of each finding, and concrete fixes. Aim for
**zero false positives** on clearly protected code, and **full coverage** of entry points.

### 9. Guardrails
**Authorized** code supplied by the user, **defensive use** (find in order to fix). If the code
contains real sensitive data, do not reproduce it in the reports; neutralize it.

### 10. Expected workflow
1. Inventory + sources/sinks map → 2. Exhaustive per-component review (taint) → 3. Verification of each
finding (confidence) → 4. Writing of evidence and fixes → 5. Report generation (per component +
management) in the requested formats → 6. Recap: total findings, breakdown by severity/OWASP, and the
list of critical/high items.

## ⟦ END OF PROMPT ⟧

---

### Usage notes
- Variables: `{{CODE_PATH}}` (required), `{{LANGUAGES}}`, `{{FORMATS}}` (defaults shown).
- For a **detection benchmark** (measuring the true/false-positive rate), additionally provide an
  **oracle** (expected list of vulnerabilities) and request TP/FP/FN scoring per finding — as in the
  VullAPPS campaign.
- For a **blind review** (unbiased evaluation of the tool), provide the code **without** the
  comments/annotations revealing the vulnerabilities, nor the architecture documentation.
