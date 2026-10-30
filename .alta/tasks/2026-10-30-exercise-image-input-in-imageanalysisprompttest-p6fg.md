---
title: "Exercise image input in ImageAnalysisPromptTest"
kind: gap
status: pending
created: 2026-10-30
summary: "The image-analysis test comments out its ImageData message and only sends a text prompt, so it cannot detect regressions in image-content mapping."
---

## Why
`Westwind.AI.Tests/TextCompletions/TextChatCompletionTests.cs:105-112` comments out the `ImageData` message; the test's only request message is text, despite its name and documentation.

## What to do
Use the copied `Images/ArlingtonBackLoop.png` fixture in the test request and ensure the test checks a nonempty response from a vision-capable configured model.

## Where
`Westwind.AI.Tests/TextCompletions/TextChatCompletionTests.cs`, `ImageAnalysisPromptTest`.
