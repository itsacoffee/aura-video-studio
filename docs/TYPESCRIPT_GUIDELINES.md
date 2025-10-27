# TypeScript Guidelines for Aura Video Studio

## Overview

This project uses TypeScript in **strict mode** to ensure type safety and catch errors at compile time. All code must pass TypeScript compilation with zero errors before merging.

## Strict Mode Configuration

Our `tsconfig.json` is configured with:
```json
{
  "compilerOptions": {
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true
  }
}
```

## Type Safety Best Practices

### 1. No Implicit Any

❌ **Bad:**
```typescript
function processData(data) {  // Implicitly 'any'
  return data.value;
}
```

✅ **Good:**
```typescript
function processData(data: { value: string }): string {
  return data.value;
}
```

### 2. Proper Null/Undefined Handling

❌ **Bad:**
```typescript
const message = warning.suggestion;  // Could be undefined
```

✅ **Good:**
```typescript
const message = warning.suggestion ?? 'No suggestion';
// or
const message = warning.suggestion || 'No suggestion';
```

### 3. useCallback Dependencies

❌ **Bad:**
```typescript
useEffect(() => {
  loadData();
}, [loadData]);  // loadData not defined yet!

const loadData = useCallback(async () => {
  // ...
}, []);
```

✅ **Good:**
```typescript
const loadData = useCallback(async () => {
  // ...
}, []);

useEffect(() => {
  loadData();
}, [loadData]);
```

### 4. Type Assertions

Use type assertions sparingly and only when you have more information than TypeScript:

❌ **Bad:**
```typescript
const data = await response.json() as any;  // Too permissive
```

✅ **Good:**
```typescript
interface ExpectedResponse {
  name: string;
  value: number;
}

const data = await response.json() as ExpectedResponse;
```

### 5. Interface Definitions

Always define interfaces for complex objects:

❌ **Bad:**
```typescript
const [state, setState] = useState<Record<string, unknown>>({});
```

✅ **Good:**
```typescript
interface ComponentState {
  isInstalled: boolean;
  isInstalling: boolean;
  error?: string;
}

const [state, setState] = useState<ComponentState>({
  isInstalled: false,
  isInstalling: false,
});
```

### 6. Function Return Types

Always specify return types for functions:

❌ **Bad:**
```typescript
async function fetchData() {
  const response = await fetch('/api/data');
  return response.json();
}
```

✅ **Good:**
```typescript
async function fetchData(): Promise<DataResponse> {
  const response = await fetch('/api/data');
  return response.json() as DataResponse;
}
```

### 7. React Component Props

Always define prop interfaces:

❌ **Bad:**
```typescript
export function MyComponent({ title, onClose }) {
  return <div>{title}</div>;
}
```

✅ **Good:**
```typescript
interface MyComponentProps {
  title: string;
  onClose?: () => void;
}

export function MyComponent({ title, onClose }: MyComponentProps) {
  return <div>{title}</div>;
}
```

### 8. Event Handlers

Use proper event types:

❌ **Bad:**
```typescript
const handleClick = (e) => {  // Implicit any
  e.preventDefault();
};
```

✅ **Good:**
```typescript
const handleClick = (e: React.MouseEvent<HTMLButtonElement>) => {
  e.preventDefault();
};
```

### 9. Unknown vs Any

Prefer `unknown` over `any` when dealing with truly unknown data:

❌ **Bad:**
```typescript
function processApiResponse(data: any) {
  return data.value;  // No type checking
}
```

✅ **Good:**
```typescript
function processApiResponse(data: unknown) {
  if (isValidResponse(data)) {
    return data.value;  // Type guard ensures safety
  }
  throw new Error('Invalid response');
}

function isValidResponse(data: unknown): data is { value: string } {
  return typeof data === 'object' && data !== null && 'value' in data;
}
```

### 10. Optional Chaining and Nullish Coalescing

Use modern TypeScript operators for safer code:

✅ **Good:**
```typescript
// Optional chaining
const port = config?.server?.port;

// Nullish coalescing (only null/undefined, not falsy)
const timeout = config.timeout ?? 5000;

// Combined
const url = config?.api?.url ?? 'http://localhost:3000';
```

## Common Patterns

### API Response Handling

```typescript
interface ApiResponse<T> {
  data: T;
  status: number;
  message?: string;
}

async function fetchFromApi<T>(endpoint: string): Promise<ApiResponse<T>> {
  const response = await fetch(endpoint);
  if (!response.ok) {
    throw new Error(`API error: ${response.statusText}`);
  }
  return response.json() as Promise<ApiResponse<T>>;
}
```

### State Management with Zustand

```typescript
interface UserState {
  user: User | null;
  isLoading: boolean;
  error: string | null;
  
  setUser: (user: User | null) => void;
  fetchUser: (id: string) => Promise<void>;
}

export const useUserStore = create<UserState>((set) => ({
  user: null,
  isLoading: false,
  error: null,
  
  setUser: (user) => set({ user }),
  
  fetchUser: async (id) => {
    set({ isLoading: true, error: null });
    try {
      const user = await api.getUser(id);
      set({ user, isLoading: false });
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Unknown error',
        isLoading: false 
      });
    }
  },
}));
```

## Type Checking Workflow

### Pre-Commit

Type-check is automatically run as part of the pre-commit hook via lint-staged.

### Manual Check

```bash
npm run type-check
```

### Build

The production build includes type-checking:

```bash
npm run build:prod
```

## Common Type Errors and Solutions

### Error: Property does not exist on type

**Problem:**
```typescript
const value = response.someProperty;  // Error if not in interface
```

**Solution:**
```typescript
// Add property to interface
interface Response {
  someProperty: string;
}

// Or use type guard
if ('someProperty' in response) {
  const value = response.someProperty;
}
```

### Error: Type 'X' is not assignable to type 'Y'

**Problem:**
```typescript
const status: 'active' | 'inactive' = userInput;  // Error
```

**Solution:**
```typescript
// Validate and narrow type
if (userInput === 'active' || userInput === 'inactive') {
  const status: 'active' | 'inactive' = userInput;
}
```

### Error: Object is possibly 'undefined'

**Problem:**
```typescript
const length = array.length;  // Error if array could be undefined
```

**Solution:**
```typescript
const length = array?.length ?? 0;
// or
if (array) {
  const length = array.length;
}
```

## Resources

- [TypeScript Handbook](https://www.typescriptlang.org/docs/handbook/intro.html)
- [React TypeScript Cheatsheet](https://react-typescript-cheatsheet.netlify.app/)
- [TypeScript ESLint Rules](https://typescript-eslint.io/rules/)

## Contributing

Before submitting a PR:

1. Run `npm run type-check` to ensure zero TypeScript errors
2. Run `npm run lint` to check for linting issues
3. Run `npm test` to ensure tests pass
4. Run `npm run build:prod` to verify production build works

Type errors in CI/CD will block merging. Fix all errors before requesting review.
