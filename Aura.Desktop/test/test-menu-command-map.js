/**
 * Unit Tests for Menu Command Map
 * 
 * Tests the MenuCommandMap module including:
 * 1. Command registry with all required commands
 * 2. Zod schema validation for payloads
 * 3. Context availability checking
 * 4. Command metadata retrieval
 */

const assert = require('assert');
const {
  MENU_COMMANDS,
  CommandContext,
  validateCommandPayload,
  getCommandMetadata,
  isCommandAvailableInContext,
  getAllCommandIds,
  getCommandsByCategory
} = require('../electron/menu-command-map');

console.log('='.repeat(60));
console.log('Testing Menu Command Map');
console.log('='.repeat(60));

// Test 1: Verify MENU_COMMANDS structure
console.log('\n1. Testing MENU_COMMANDS structure...');
assert.ok(typeof MENU_COMMANDS === 'object', 'MENU_COMMANDS should be an object');
assert.ok(Object.keys(MENU_COMMANDS).length > 0, 'MENU_COMMANDS should not be empty');

// Check that all commands have required properties
Object.entries(MENU_COMMANDS).forEach(([key, command]) => {
  assert.ok(command.id, `Command ${key} should have an id`);
  assert.ok(command.label, `Command ${key} should have a label`);
  assert.ok(command.category, `Command ${key} should have a category`);
  assert.ok(command.schema, `Command ${key} should have a schema`);
  assert.ok(Array.isArray(command.contexts), `Command ${key} should have contexts array`);
  assert.ok(command.description, `Command ${key} should have a description`);
});
console.log(`✓ All ${Object.keys(MENU_COMMANDS).length} commands have required properties`);

// Test 2: Verify CommandContext values
console.log('\n2. Testing CommandContext enum...');
const expectedContexts = ['global', 'project', 'timeline', 'media', 'settings', 'help'];
expectedContexts.forEach(context => {
  assert.ok(
    Object.values(CommandContext).includes(context),
    `CommandContext should include '${context}'`
  );
});
console.log(`✓ All ${expectedContexts.length} contexts are defined`);

// Test 3: Test validateCommandPayload with valid payloads
console.log('\n3. Testing validateCommandPayload with valid payloads...');

// Test with empty payload (most commands)
const newProjectResult = validateCommandPayload('menu:newProject', {});
assert.strictEqual(newProjectResult.success, true, 'Empty payload should validate for NEW_PROJECT');
assert.ok(newProjectResult.command, 'Validation result should include command metadata');
assert.strictEqual(newProjectResult.command.label, 'New Project', 'Should include correct label');

// Test with data payload (OpenRecentProject)
const openRecentResult = validateCommandPayload('menu:openRecentProject', { path: '/path/to/project' });
assert.strictEqual(openRecentResult.success, true, 'Valid path should validate for OPEN_RECENT_PROJECT');
assert.strictEqual(openRecentResult.data.path, '/path/to/project', 'Should preserve path in validated data');

console.log('✓ Valid payloads validate successfully');

// Test 4: Test validateCommandPayload with invalid payloads
console.log('\n4. Testing validateCommandPayload with invalid payloads...');

// Test with missing required field
const invalidRecentResult = validateCommandPayload('menu:openRecentProject', {});
assert.strictEqual(invalidRecentResult.success, false, 'Empty payload should fail for OPEN_RECENT_PROJECT');
assert.ok(invalidRecentResult.error, 'Should include error message');
assert.ok(invalidRecentResult.issues, 'Should include validation issues');

// Test with unknown command
const unknownResult = validateCommandPayload('menu:unknown', {});
assert.strictEqual(unknownResult.success, false, 'Unknown command should fail validation');
assert.ok(unknownResult.error.includes('Unknown command'), 'Error should mention unknown command');

console.log('✓ Invalid payloads properly rejected');

// Test 5: Test getCommandMetadata
console.log('\n5. Testing getCommandMetadata...');

const newProjectMetadata = getCommandMetadata('menu:newProject');
assert.ok(newProjectMetadata, 'Should return metadata for valid command');
assert.strictEqual(newProjectMetadata.label, 'New Project', 'Should return correct label');
assert.strictEqual(newProjectMetadata.category, 'File', 'Should return correct category');

const unknownMetadata = getCommandMetadata('menu:unknown');
assert.strictEqual(unknownMetadata, null, 'Should return null for unknown command');

console.log('✓ Command metadata retrieval works correctly');

// Test 6: Test isCommandAvailableInContext
console.log('\n6. Testing isCommandAvailableInContext...');

// Global commands should be available everywhere
assert.strictEqual(
  isCommandAvailableInContext('menu:newProject', CommandContext.GLOBAL),
  true,
  'Global command should be available in global context'
);
assert.strictEqual(
  isCommandAvailableInContext('menu:newProject', CommandContext.TIMELINE),
  true,
  'Global command should be available in timeline context'
);

// Context-specific commands
assert.strictEqual(
  isCommandAvailableInContext('menu:saveProject', CommandContext.PROJECT_LOADED),
  true,
  'Save should be available when project is loaded'
);
assert.strictEqual(
  isCommandAvailableInContext('menu:saveProject', CommandContext.GLOBAL),
  false,
  'Save should not be available in global context (no project)'
);

console.log('✓ Context availability checking works correctly');

// Test 7: Test getAllCommandIds
console.log('\n7. Testing getAllCommandIds...');

const allIds = getAllCommandIds();
assert.ok(Array.isArray(allIds), 'Should return an array');
assert.ok(allIds.length > 0, 'Should return non-empty array');
assert.strictEqual(allIds.length, Object.keys(MENU_COMMANDS).length, 'Should return all command IDs');

// Verify all IDs start with 'menu:'
allIds.forEach(id => {
  assert.ok(id.startsWith('menu:'), `Command ID '${id}' should start with 'menu:'`);
});

console.log(`✓ getAllCommandIds returns all ${allIds.length} command IDs`);

// Test 8: Test getCommandsByCategory
console.log('\n8. Testing getCommandsByCategory...');

const fileCommands = getCommandsByCategory('File');
assert.ok(Array.isArray(fileCommands), 'Should return an array');
assert.ok(fileCommands.length > 0, 'File category should have commands');
fileCommands.forEach(cmd => {
  assert.strictEqual(cmd.category, 'File', 'All commands should be in File category');
});

const helpCommands = getCommandsByCategory('Help');
assert.ok(helpCommands.length > 0, 'Help category should have commands');
helpCommands.forEach(cmd => {
  assert.strictEqual(cmd.category, 'Help', 'All commands should be in Help category');
});

console.log('✓ getCommandsByCategory works correctly');

// Test 9: Verify all command IDs match existing channels
console.log('\n9. Testing command IDs match existing menu event channels...');

const { MENU_EVENT_CHANNELS } = require('../electron/menu-event-types');
const commandIds = getAllCommandIds();

// Every command ID should be in MENU_EVENT_CHANNELS
commandIds.forEach(id => {
  assert.ok(
    MENU_EVENT_CHANNELS.includes(id),
    `Command ID '${id}' should be in MENU_EVENT_CHANNELS`
  );
});

// Every menu event channel should have a command
MENU_EVENT_CHANNELS.forEach(channel => {
  assert.ok(
    commandIds.includes(channel),
    `Menu event channel '${channel}' should have a command definition`
  );
});

console.log('✓ Command IDs and menu event channels are in sync');

// Test 10: Verify keyboard shortcuts are defined correctly
console.log('\n10. Testing keyboard shortcuts...');

let shortcutsCount = 0;
Object.values(MENU_COMMANDS).forEach(command => {
  if (command.accelerator) {
    shortcutsCount++;
    assert.ok(
      typeof command.accelerator === 'string',
      `Accelerator for '${command.id}' should be a string`
    );
  }
});

console.log(`✓ ${shortcutsCount} commands have keyboard shortcuts defined`);

// Test 11: Test schema validation with edge cases
console.log('\n11. Testing schema validation with edge cases...');

// Test with extra fields (should be allowed)
const extraFieldsResult = validateCommandPayload('menu:newProject', { extraField: 'value' });
assert.strictEqual(extraFieldsResult.success, true, 'Extra fields should be allowed');

// Test with empty string for required field
const emptyStringResult = validateCommandPayload('menu:openRecentProject', { path: '' });
assert.strictEqual(emptyStringResult.success, false, 'Empty string should fail min length validation');

// Test with correct type
const validPathResult = validateCommandPayload('menu:openRecentProject', { path: 'valid/path' });
assert.strictEqual(validPathResult.success, true, 'Valid path string should validate');

console.log('✓ Schema validation handles edge cases correctly');

// Test 12: Verify categories are consistent
console.log('\n12. Testing category consistency...');

// Get actual categories from commands
const actualCategories = new Set(Object.values(MENU_COMMANDS).map(cmd => cmd.category));

// Verify we have the expected main categories (not all may be present)
const possibleCategories = ['File', 'Edit', 'View', 'Tools', 'Help'];
actualCategories.forEach(category => {
  assert.ok(
    possibleCategories.includes(category),
    `Category '${category}' should be in expected categories list`
  );
});

// Verify each category has at least one command
actualCategories.forEach(category => {
  const commandsInCategory = getCommandsByCategory(category);
  assert.ok(
    commandsInCategory.length > 0,
    `Category '${category}' should have at least one command`
  );
});

console.log(`✓ All ${actualCategories.size} categories are valid and have commands`);

console.log('\n' + '='.repeat(60));
console.log('ALL MENU COMMAND MAP TESTS PASSED ✓');
console.log('='.repeat(60));
console.log(`Total commands: ${Object.keys(MENU_COMMANDS).length}`);
console.log(`Total contexts: ${Object.values(CommandContext).length}`);
console.log(`Commands with shortcuts: ${shortcutsCount}`);
console.log(`Categories: ${Array.from(actualCategories).join(', ')}`);
console.log('='.repeat(60));
