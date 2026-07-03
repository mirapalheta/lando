import tseslint from "typescript-eslint";
import prettierConfig from "eslint-config-prettier";

export default tseslint.config(
  // Base TypeScript + strict rules
  ...tseslint.configs.strictTypeChecked,
  ...tseslint.configs.stylisticTypeChecked,

  // Project-wide parser options
  {
    languageOptions: {
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
  },

  // Rule overrides
  {
    rules: {
      // Prefer explicit return types on exported functions for readability
      "@typescript-eslint/explicit-module-boundary-types": "warn",
      // Async functions that never await can return void — allow it
      "@typescript-eslint/require-await": "warn",
      // Allow underscore-prefixed names for intentionally unused params
      "@typescript-eslint/no-unused-vars": [
        "error",
        { argsIgnorePattern: "^_", varsIgnorePattern: "^_" },
      ],
      // Console is fine in a Lambda — it goes to CloudWatch
      "no-console": "off",
    },
  },

  // Disable all rules that conflict with Prettier formatting
  prettierConfig,

  // Ignore generated / vendored paths
  {
    ignores: ["dist/", "node_modules/", "*.js"],
  },
);
