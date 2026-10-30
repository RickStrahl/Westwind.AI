---
title: "Fix GenerateRejectsInvalidReferenceTest failure for null base image"
kind: problem
status: later
created: 2026-10-30
summary: "ImageGenerationRequestTests.GenerateRejectsInvalidReferenceTest(null, \"not found\") fails: a null BaseImages entry produces the error \"Missing image file in BaseImageFiles.\" which does not contain the expected \"not found\"."
---

## Why
`dotnet test --filter FullyQualifiedName~RequestTests` fails on `GenerateRejectsInvalidReferenceTest (null,"not found")` in `Westwind.AI.Tests/Images/ImageGenerationRequestTests.cs:150`. The test expects the error to contain "not found", but `OpenAiImageGeneration.cs:96` sets "Missing image file in BaseImageFiles." for a null entry (the "not found" wording is only at line 134 for an existing-but-missing path). Unrelated to the reasoning-effort change; fails before and after it.

## What to do
Align the error message and the test: either make the null-entry error read like the missing-file one ("Reference image file not found") or adjust the DataRow expectation to the actual message.

## Where
- `Westwind.AI/Images/OpenAiImageGeneration.cs` (lines ~96 and ~134)
- `Westwind.AI.Tests/Images/ImageGenerationRequestTests.cs` (`GenerateRejectsInvalidReferenceTest`)
