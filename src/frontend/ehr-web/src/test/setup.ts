// Vitest global setup: extends `expect` with jest-dom matchers (toBeInTheDocument, …) and
// clears the DOM/mocks between tests so cases stay isolated.
import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

afterEach(() => {
  cleanup();
});
