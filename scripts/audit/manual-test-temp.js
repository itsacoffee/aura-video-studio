#!/usr/bin/env node

// Test scenarios for placeholder scanner

// SCENARIO 1: String literals should NOT match
const message = "// TODO: This is shown in the UI";
const warning = '// FIXME: user error message';
const template = `// HACK: interpolated ${value}`;

// SCENARIO 2: Real comments SHOULD match
// TODO: Implement this feature

// SCENARIO 3: Suppressed line should NOT match
// TODO: Justified exception // placeholder-scan: ignore-line

// SCENARIO 4: Block suppression
/* placeholder-scan: ignore-start */
// TODO: Legacy code, documented
// FIXME: Known issue, tracked in #123
/* placeholder-scan: ignore-end */

// SCENARIO 5: After block, should match again
// WIP: Active development
