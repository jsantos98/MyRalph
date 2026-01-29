# React/TypeScript Patterns Reference

Comprehensive reference of React patterns and best practices for the Senior Frontend Developer.

## Table of Contents

1. [Component Patterns](#component-patterns)
2. [State Management](#state-management)
3. [Data Fetching](#data-fetching)
4. [Form Handling](#form-handling)
5. [Performance](#performance)
6. [Testing](#testing)
7. [Styling](#styling)
8. [TypeScript Patterns](#typescript-patterns)

---

## Component Patterns

### Compound Components

```typescript
// Dialog compound component pattern
interface DialogContextValue {
  isOpen: boolean;
  open: () => void;
  close: () => void;
}

const DialogContext = createContext<DialogContextValue | null>(null);

export function Dialog({ children }: { children: ReactNode }) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <DialogContext.Provider value={{ isOpen, open: () => setIsOpen(true), close: () => setIsOpen(false) }}>
      {children}
    </DialogContext.Provider>
  );
}

export function DialogTrigger({ children }: { children: ReactNode }) {
  const { open } = useDialogContext();
  return <button onClick={open}>{children}</button>;
}

export function DialogContent({ children, title }: { children: ReactNode; title: string }) {
  const { isOpen, close } = useDialogContext();

  if (!isOpen) return null;

  return createPortal(
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="bg-white rounded-lg shadow-xl p-6 max-w-md">
        <h2 className="text-xl font-bold mb-4">{title}</h2>
        {children}
      </div>
    </div>,
    document.body
  );
}

// Usage
<Dialog>
  <DialogTrigger>Open Dialog</DialogTrigger>
  <DialogContent title="Confirm Action">
    <p>Are you sure?</p>
  </DialogContent>
</Dialog>
```

### Render Props

```typescript
interface MouseTrackerProps {
  render: (position: { x: number; y: number }) => ReactNode;
}

export function MouseTracker({ render }: MouseTrackerProps) {
  const [position, setPosition] = useState({ x: 0, y: 0 });

  const handleMouseMove = (e: MouseEvent) => {
    setPosition({ x: e.clientX, y: e.clientY });
  };

  useEffect(() => {
    window.addEventListener('mousemove', handleMouseMove);
    return () => window.removeEventListener('mousemove', handleMouseMove);
  }, []);

  return <>{render(position)}</>;
}

// Usage
<MouseTracker render={({ x, y }) => <p>Mouse: {x}, {y}</p>} />
```

### Higher-Order Components

```typescript
function withLoading<P extends object>(
  Component: ComponentType<P>,
  loadingSelector: (state: RootState) => boolean
) {
  return function WithLoading(props: P) {
    const isLoading = useAppSelector(loadingSelector);

    if (isLoading) {
      return <LoadingSpinner />;
    }

    return <Component {...props} />;
  };
}

// Usage
const UserProfileWithLoading = withLoading(UserProfile, (state) => state.user.isLoading);
```

---

## State Management

### Zustand Store Pattern

```typescript
// stores/userStore.ts
interface UserState {
  user: User | null;
  isLoading: boolean;
  error: string | null;
  fetchUser: (id: string) => Promise<void>;
  clearUser: () => void;
}

export const useUserStore = create<UserState>((set, get) => ({
  user: null,
  isLoading: false,
  error: null,

  fetchUser: async (id: string) => {
    set({ isLoading: true, error: null });
    try {
      const user = await usersApi.get(id);
      set({ user, isLoading: false });
    } catch (error) {
      set({
        error: error instanceof Error ? error.message : 'Failed to fetch user',
        isLoading: false
      });
    }
  },

  clearUser: () => set({ user: null, error: null }),
}));
```

### Context API Pattern

```typescript
// contexts/AuthContext.tsx
interface AuthContextValue {
  user: User | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);

  const login = useCallback(async (email: string, password: string) => {
    const response = await authApi.login({ email, password });
    setUser(response.user);
    localStorage.setItem('token', response.token);
  }, []);

  const logout = useCallback(() => {
    setUser(null);
    localStorage.removeItem('token');
  }, []);

  const value = useMemo(
    () => ({ user, login, logout }),
    [user, login, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
```

---

## Data Fetching

### React Query Patterns

```typescript
// hooks/useUsers.ts
export function useUsers(filters?: UserFilters) {
  return useQuery({
    queryKey: ['users', filters],
    queryFn: () => usersApi.list(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes (formerly cacheTime)
  });
}

// hooks/useUser.ts
export function useUser(id: string, enabled = true) {
  return useQuery({
    queryKey: ['user', id],
    queryFn: () => usersApi.get(id),
    enabled: enabled && !!id,
  });
}

// Infinite query for pagination
export function useInfiniteUsers() {
  return useInfiniteQuery({
    queryKey: ['users', 'infinite'],
    queryFn: ({ pageParam = 0 }) =>
      usersApi.list({ page: pageParam, limit: 20 }),
    initialPageParam: 0,
    getNextPageParam: (lastPage, allPages) => {
      if (lastPage.length < 20) return undefined;
      return allPages.length;
    },
  });
}
```

### Optimistic Updates

```typescript
export function useUpdateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<User> }) =>
      usersApi.update(id, data),

    onMutate: async ({ id, data }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['user', id] });

      // Snapshot previous value
      const previousUser = queryClient.getQueryData<User>(['user', id]);

      // Optimistically update
      queryClient.setQueryData<User>(['user', id], (old) =>
        old ? { ...old, ...data } : old
      );

      return { previousUser };
    },

    onError: (err, variables, context) => {
      // Rollback on error
      queryClient.setQueryData(['user', variables.id], context?.previousUser);
    },

    onSettled: (data, error, variables) => {
      // Refetch after error or success
      queryClient.invalidateQueries({ queryKey: ['user', variables.id] });
    },
  });
}
```

---

## Form Handling

### React Hook Form + Zod

```typescript
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const userSchema = z.object({
  name: z.string().min(2),
  email: z.string().email(),
  age: z.number().min(18).max(120),
});

type UserFormData = z.infer<typeof userSchema>;

function UserForm() {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<UserFormData>({
    resolver: zodResolver(userSchema),
  });

  const onSubmit = async (data: UserFormData) => {
    await usersApi.create(data);
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <input {...register('name')} />
      {errors.name && <span>{errors.name.message}</span>}

      <input type="email" {...register('email')} />
      {errors.email && <span>{errors.email.message}</span>}

      <input type="number" {...register('age', { valueAsNumber: true })} />
      {errors.age && <span>{errors.age.message}</span>}

      <button disabled={isSubmitting}>Submit</button>
    </form>
  );
}
```

### Dynamic Fields

```typescript
function DynamicForm() {
  const { fields, append, remove } = useFieldArray({
    control,
    name: 'items',
  });

  return (
    <div>
      {fields.map((field, index) => (
        <div key={field.id}>
          <input {...register(`items.${index}.name`)} />
          <button type="button" onClick={() => remove(index)}>
            Remove
          </button>
        </div>
      ))}
      <button type="button" onClick={() => append({ name: '' })}>
        Add Item
      </button>
    </div>
  );
}
```

---

## Performance

### Memoization

```typescript
// ✅ Memo for expensive renders
const ExpensiveList = memo(({ items }: { items: Item[] }) => {
  return (
    <div>
      {items.map((item) => (
        <Item key={item.id} item={item} />
      ))}
    </div>
  );
}, (prev, next) => {
  // Custom comparison
  return prev.items.length === next.items.length &&
    prev.items.every((item, i) => item.id === next.items[i]?.id);
});

// ✅ useMemo for expensive calculations
function DataTable({ data }: { data: Data[] }) {
  const sorted = useMemo(() => {
    return data.sort((a, b) => a.name.localeCompare(b.name));
  }, [data]);

  const total = useMemo(() => {
    return data.reduce((sum, item) => sum + item.value, 0);
  }, [data]);

  return <div>{/* ... */}</div>;
}

// ✅ useCallback for stable references
function Parent() {
  const handleClick = useCallback((id: string) => {
    console.log('Clicked', id);
  }, []); // Empty deps = stable forever

  return <Child onClick={handleClick} />;
}
```

### Code Splitting

```typescript
import { lazy, Suspense } from 'react';

// Lazy load pages
const HomePage = lazy(() => import('@/pages/HomePage'));
const DashboardPage = lazy(() => import('@/pages/DashboardPage'));
const SettingsPage = lazy(() => import('@/pages/SettingsPage'));

function App() {
  return (
    <Suspense fallback={<PageSkeleton />}>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Routes>
    </Suspense>
  );
}
```

### Virtual Scrolling

```typescript
import { useVirtualizer } from '@tanstack/react-virtual';

function VirtualList({ items }: { items: Item[] }) {
  const parentRef = useRef<HTMLDivElement>(null);

  const virtualizer = useVirtualizer({
    count: items.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 50, // Estimated item height
  });

  return (
    <div ref={parentRef} style={{ height: '400px', overflow: 'auto' }}>
      <div style={{ height: `${virtualizer.getTotalSize()}px` }}>
        {virtualizer.getVirtualItems().map((virtualItem) => (
          <div
            key={virtualItem.key}
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              width: '100%',
              transform: `translateY(${virtualItem.start}px)`,
            }}
          >
            {items[virtualItem.index].name}
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## Testing

### Component Testing

```typescript
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

describe('UserCard', () => {
  it('renders user name', () => {
    render(<UserCard user={{ id: '1', name: 'John' }} />);
    expect(screen.getByText('John')).toBeInTheDocument();
  });

  it('calls onEdit when edit button clicked', async () => {
    const onEdit = vi.fn();
    render(<UserCard user={{ id: '1', name: 'John' }} onEdit={onEdit} />);

    await userEvent.click(screen.getByRole('button', { name: /edit/i }));
    expect(onEdit).toHaveBeenCalled();
  });
});
```

### Hook Testing

```typescript
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

describe('useUsers', () => {
  it('fetches users', async () => {
    const queryClient = new QueryClient();

    const { result } = renderHook(() => useUsers(), {
      wrapper: ({ children }) => (
        <QueryClientProvider client={queryClient}>
          {children}
        </QueryClientProvider>
      ),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
  });
});
```

---

## Styling

### Tailwind Patterns

```typescript
// ✅ Use cn() utility for conditional classes
import { cn } from '@/lib/utils';

function Button({ variant = 'primary', className, ...props }: ButtonProps) {
  return (
    <button
      className={cn(
        'px-4 py-2 rounded font-medium',
        variant === 'primary' && 'bg-blue-600 text-white hover:bg-blue-700',
        variant === 'secondary' && 'bg-gray-200 text-gray-900 hover:bg-gray-300',
        variant === 'danger' && 'bg-red-600 text-white hover:bg-red-700',
        className
      )}
      {...props}
    />
  );
}
```

### CSS-in-JS (if needed)

```typescript
const styledCard = cva(
  'bg-white rounded-lg shadow p-4',
  {
    variants: {
      size: {
        sm: 'p-2',
        md: 'p-4',
        lg: 'p-6',
      },
      elevation: {
        none: 'shadow-none',
        sm: 'shadow-sm',
        md: 'shadow',
        lg: 'shadow-lg',
      }
    },
    defaultVariants: {
      size: 'md',
      elevation: 'md',
    }
  }
);
```

---

## TypeScript Patterns

### Generic Components

```typescript
interface TableProps<T> {
  data: T[];
  columns: Column<T>[];
  keyExtractor: (item: T) => string;
}

function Table<T>({ data, columns, keyExtractor }: TableProps<T>) {
  return (
    <table>
      <thead>
        <tr>
          {columns.map((col) => (
            <th key={col.key}>{col.header}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {data.map((item) => (
          <tr key={keyExtractor(item)}>
            {columns.map((col) => (
              <td key={col.key}>{col.render(item)}</td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  );
}

// Usage
interface User {
  id: string;
  name: string;
  email: string;
}

const columns: Column<User>[] = [
  { key: 'name', header: 'Name', render: (user) => user.name },
  { key: 'email', header: 'Email', render: (user) => user.email },
];

<Table
  data={users}
  columns={columns}
  keyExtractor={(user) => user.id}
/>
```

### Polymorphic Components

```typescript
import { ComponentProps, ReactElement } from 'react';

type AsProps<T extends React.ElementType> = {
  as?: T;
};

type Props<T extends React.ElementType> = AsProps<T> &
  Omit<ComponentProps<T>, 'as'> & {
    children: ReactNode;
  };

export function Box<T extends React.ElementType = 'div'>({
  as,
  children,
  ...props
}: Props<T>): ReactElement {
  const Component = as || 'div';
  return <Component {...props}>{children}</Component>;
}

// Usage
<Box as="button" onClick={handleClick}>
  Click me
</Box>

<Box as="a" href="/about">
  About
</Box>
```

### Utility Types

```typescript
// Make specific props optional
type OptionalProps<T, K extends keyof T> = Omit<T, K> & Partial<Pick<T, K>>;

// Usage
interface ButtonProps {
  variant: 'primary' | 'secondary';
  size: 'sm' | 'md' | 'lg';
  onClick: () => void;
}

type BaseButtonProps = OptionalProps<ButtonProps, 'onClick'>;

// Make specific props required
type RequiredProps<T, K extends keyof T> = Omit<T, K> & Required<Pick<T, K>>;

// Extract props from component
type ButtonProps = React.ComponentProps<typeof Button>;
```

---

## Key Principles

1. **Composition over inheritance** - Use composition patterns
2. **Unidirectional data flow** - Props down, events up
3. **Single responsibility** - Components should do one thing well
4. **Type safety** - Leverage TypeScript for better DX
5. **Performance first** - Use memo, useMemo, useCallback appropriately
6. **Accessibility** - WCAG 2.1 AA compliance minimum
7. **Test behavior** - Test what users see and do, not implementation
8. **Responsive design** - Mobile-first approach
