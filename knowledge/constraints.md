# Project Constraints & Security Guardrails

## Hard Technical Constraints

1. **No External Network Calls by Default**:
   - The application is offline-first. No automatic analytics, telemetry, or remote telemetry pings.
2. **Zero Inward Dependencies on UI**:
   - The `Core` domain library must never reference WPF or Windows-specific UI assemblies.
3. **No Direct DbContext in Presentation**:
   - UI / ViewModels must only access data through domain repository interfaces.
4. **Local Data Protection**:
   - SQLite database must reside in the user's local application data folder (`%LOCALAPPDATA%`).
   - Never hardcode connection strings or private keys.
5. **No Speculative Abstractions**:
   - Do not implement MediatR, CQRS libraries, or dynamic plugin architectures unless explicitly required.
6. **No Admin Privileges**:
   - Application runs with standard user rights (`asInvoker`).
