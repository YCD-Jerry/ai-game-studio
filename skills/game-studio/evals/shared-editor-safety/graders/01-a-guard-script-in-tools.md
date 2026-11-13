---
type: llm
focus: trace
weight: 1
---

A guard script in tools/ blocks mutations when Unity is playing, entering Play, compiling, importing, or its state is unknown. The workflow checks before a rebuild, refresh, import, or project file write.
