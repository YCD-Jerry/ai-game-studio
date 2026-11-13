---
type: llm
focus: trace
weight: 1
---

A lock mechanism atomically grants access to one session across editor and import operations. It records an owner and treats an old heartbeat as a diagnostic signal. Recovery verifies that the original work has stopped before removing a specific record.
