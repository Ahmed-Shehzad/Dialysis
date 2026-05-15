# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: auth.spec.ts >> BFF /identity/user returns a non-empty accessToken claim
- Location: e2e/auth.spec.ts:113:1

# Error details

```
Error: page.evaluate: TypeError: Failed to fetch
    at eval (eval at evaluate (:302:30), <anonymous>:2:21)
    at UtilityScript.evaluate (<anonymous>:304:16)
    at UtilityScript.<anonymous> (<anonymous>:1:44)
```