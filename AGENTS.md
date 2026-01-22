# Agent


## General F# guidelines

- Prefer shortened lambdas to full declaration (`_.Method()` over `fun x -> x.Method()`)
- Prefer appending new code to the end of the file
- Use `Task.FromResult()` instead of `task { return ... }`
- Omit parentheses for single-argument functions

## Unit testing guidelines

- Prefer class `type` instead of module for unit tests
- Do not add `Tests` suffix to types with unit tests
- Use `Moq` library to mock interfaces
- Declare and initialize mocks as `type` fields
- Use assertion checks from `FsUnit` and `FsUnit.xUnit` libraries
- Setup mocks before entering `task` computational expression
- Prefer `VerifyAll` and `VerifyNoOtherCalls` method calls on `IMock` instead of `Verify` method
- Do not add `Mock` suffix to mock variable names
- Prefer `ReturnsAsync` method for setting up mock returns
- Do not call `|> ignore` in case if mocks is set up inside type `member`