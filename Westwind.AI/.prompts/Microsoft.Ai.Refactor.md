# Refactor Library to use Microsoft.AI Extensions

Need to refactor this library to use Microsoft.AI extensions instead of the raw Http interfaces used in the existing classes, but maintaining the existing configuration architecture.

API changes to the outward facing interfaces should be minimal as to cause minimal API breakage.

Any changes should be made in light of a follow-up update to support Agentic framework integration - the chat interfaces eventually should also support agentic behavior to perform tasks using tools/mcp etc. 

Create a plan and let me review. If unsure of changes ask for clarification.