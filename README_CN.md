# Trarizon.Functional

提供基本的monads，以及type union的生成器

- 提供`Optional<T>`和`Result<T, TError>`，支持`ref struct`
- 提供`TypeUnionAttribute`，基于源生成器生成type union，支持`ref struct`以及指针

## Monads

内置`Optional<T>`和`Result<T, TError>`，两者提供了类似的API用于操作。
`RefOptional<T>`和`RefResult<T, TError>`为两者的`ref struct`版本。

如果语言规范允许，`ref struct`版本的类型基本拥有非`ref struct`版本的所有方法。

#### 创建

使用`Optional`、`RefOptional`、`Result`、`RefResult`类创建值。
对于C# 14及以上版本，`Optional`、`Result`也可用于创建`ref struct`版本

|方法|描述|
|---|---|
`Optional.Of<T>()`|创建`Some(T)`
`Optional.None`|创建`None`
`Result.Success<T>()`|创建`Success(T)`
`Result.Failure<TError>()`|创建`Failure(TError)`

`Optional.None` 返回的类型为`Optional.NoneBuilder`，该类型可隐式转换为`Optional<T>`与`RefOptional<T>`，因此以下使用方式是合法的。
``` csharp
Optional<int> optNumber = Optional.None;
var func = (bool condition) => condition ? Optional.Of(1) : Optional.None;
```

如果隐式转换失效或需要具体类型的`None`，可使用`Optional<T>.None`或`Build`方法
``` csharp
var none = Optional<int>.None; // 返回Optional<int>
var none = Optional.None.Build<int>(); // 返回Optional<int>
```

`Result`的API原理与`Optional`类似。

此外还提供了其他方法用于创建值，均位于`Optional`类中。

#### 方法

转换方法基于C#习惯命名，如无必要不使用函数式命名。

方法普遍有多个重载，以及根据不同命名后缀提供差异操作，比如`Select` vs `SelectError`。

方法|描述|
|---|---|
|`Select`|map|
|`Where`|filter|
|`Bind`|flatMap|
|`Tap`|不改变值，仅用于副作用|
|`Zip`|合并两个monad，返回一个新的monad|
|`Swap`|交换Success和Failure|
|`Match`|根据monad的状态执行不同的操作|
|`Cast`|同Linq的Cast，将monad转换为指定类型|
|`OfType`|同Linq的OfType，筛选出指定类型的monad|
|`GetValueOrThrow`<br/>`GetValueOrDefault`<br/>`TryGetVaue`<br/>`GetValueRefOrDefaultRef`|获取值

- `Select`，`Bind`支持`ref struct`，会自动根据函数返回值切换`ref struct`版与非`ref struct`版。
- 提供了`SelectMany`方法同`Bind`，但已隐藏，仅用于linq表达式语法使用。

#### 转换

关于`Optional<T>`、`Result<T, TError>`及其`ref struct`版本，以及`Nullable<T>`、`Task<T>`、`ValueTask<T>`、`IEnumerable<T>`等类型复合类型的转换方法。

下表中`Monad<T>`代表`Optional<T>`或`Result<T, TError>`，`Task<T>`代表`Task<T>`或`ValueTask<T>`。

|类型|方法|
|---|---|
|`Monad<T>` <-> `RefMonad<T>`| `AsRef`<br/>`AsDeref`<br/>implicit cast|
|`Optional<T>` <-> `Nullable<T>`|`ToNullable`<br/>`Optional.OfNotNull<T>(T?)`|
|`Optional<T>` <-> `Result<T, TError>`|`ToResult`<br/>`ToOptional`|
|`Optional<Result<T, TError>>` <-> `Result<Optional<T>, TError>`|`Transpose`|
|`Monad<Task<T>>` -> `Task<Monad<T>>`|`Transpose`|
|`IEnumerable<Monad<T>>` -> `IEnumerable<T>`|`WhereValues`<br/>`WhereErrors`|

## Type Union

使用`Trarizon.Library.Functional.Unions.TypeUnionAttribute`标记类型以生成type union。

- 支持`ref struct`，含有`ref struct`时union类型会标记为`ref struct`。
- 支持指针类型
- 支持`void`但没什么用

``` csharp
[TypeUnion(
    typeof(void),
    typeof(string),
    typeof(int),
    typeof(JsonElement),
    typeof(ReadOnlySpan<char>),
    typeof(float),
    typeof(void*), typeof(int*))]
partial struct MyUnion;
```

<details>
<summary>生成代码（简化）</summary>

``` csharp
[StructLayout(LayoutKind.Auto)]
ref partial struct MyUnion 
{
    readonly uint _flag;
    readonly object _obj;
    readonly JsonElement _managed0;
    readonly ReadOnlySpan<char> _managed1;
    readonly _Unmanageds _unmanaged;

    public bool IsNull => _flag == 0;

    public static MyUnion Void => default(MyUnion) with { _flag = 1 };

    public MyUnion(string value) => (_flag, _obj) = (2, value);
    public MyUnion(int value) => (_flag, _unmanaged._0) = (3, value);
    // ...

    public static implicit operator MyUnion(string value) => new(value);
    // ...

    public static explicit operator string(MyUnion value) => value.As<string>();
    // ...

    public T? As<T>() where T : allows ref struct
    {
        return _flag switch
        {
            2 => _obj is T t ? t : default(T),
            3 => _unmanaged._0 is T t ? t : default(T),
            4 => _managed0 is T t ? t : default(T),
            // ...
            _ => default(T)
        }
    }

    public T? AsExactly<T>() where T : allows ref struct
    {
        if (typeof(T) == typeof(string)) return _flag == 2 ? Unsafe.As<object, T>(ref _obj) : default(T);
        if (typeof(T) == typeof(int)) return _flag == 3 ? Unsafe.As<object, T>(ref _unmanaged._0) : default(T);
        // ...
        return default(T);
    }

    public bool Is<T>() where T : allows ref struct
        => /* ... */;

    public bool IsExactly(Type type)
        => /* ... */;

    public bool IsExactly<T>() where T : allows ref struct
        => /* ... */;

    [StructLayout(LayoutKind.Explicit)]
    struct _Unmanageds // 如果含有ref struct，则该结构体也会是ref struct
    {
        [FieldOffset(0)] public readonly int _0;
        [FieldOffset(0)] public readonly float _1;
        [FieldOffset(0)] public readonly nint _ptr; // 所有指针共用该字段
    }
}
```

</details>

<details>
<summary>完整生成代码（删除了`[GeneratedCode]`特性与注释）</summary>

``` csharp
[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]
ref partial struct MyUnion
{
    private readonly uint __um_flag;
    private readonly object __um_obj;
    private readonly global::System.Text.Json.JsonElement __um_managed0;
    private readonly global::System.ReadOnlySpan<char> __um_managed1;
    private readonly __ut_Unmanaged __um_unmanaged;
    
    public bool IsNull { get { return this.__um_flag == 0u; } }
    
    public static global::MyUnion Void
    {
        get
        {
            global::MyUnion value = default(global::MyUnion);
            global::System.Runtime.CompilerServices.Unsafe.AsRef(in value.__um_flag) = 1u;
            return value;
        }
    }
    
    public MyUnion(string value)
    {
        this.__um_flag = 2u;
        this.__um_obj = value;
    }
    
    public MyUnion(int value)
    {
        this.__um_flag = 3u;
        this.__um_unmanaged._1 = value;
    }
    
    public MyUnion(global::System.Text.Json.JsonElement value)
    {
        this.__um_flag = 4u;
        this.__um_managed0 = value;
    }
    
    public MyUnion(global::System.ReadOnlySpan<char> value)
    {
        this.__um_flag = 5u;
        this.__um_managed1 = value;
    }
    
    public MyUnion(float value)
    {
        this.__um_flag = 6u;
        this.__um_unmanaged._2 = value;
    }
    
    public unsafe MyUnion(void* value)
    {
        this.__um_flag = 7u;
        this.__um_unmanaged._ptr = (global::System.IntPtr)value;
    }
    
    public unsafe MyUnion(int* value)
    {
        this.__um_flag = 8u;
        this.__um_unmanaged._ptr = (global::System.IntPtr)value;
    }
    
    #region Cast
    
    public static implicit operator global::MyUnion(string value) => new global::MyUnion(value);
    
    public static implicit operator global::MyUnion(int value) => new global::MyUnion(value);
    
    public static implicit operator global::MyUnion(global::System.Text.Json.JsonElement value) => new global::MyUnion(value);
    
    public static implicit operator global::MyUnion(global::System.ReadOnlySpan<char> value) => new global::MyUnion(value);
    
    public static implicit operator global::MyUnion(float value) => new global::MyUnion(value);
    
    public static unsafe implicit operator global::MyUnion(void* value) => new global::MyUnion(value);
    
    public static unsafe implicit operator global::MyUnion(int* value) => new global::MyUnion(value);
    
    public static explicit operator string(global::MyUnion value)
    {
        if (value.__um_flag != 2u)
            throw new InvalidCastException($"Unable to cast MyUnion to string");
        return global::System.Runtime.CompilerServices.Unsafe.As<object, string>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<object>(in value.__um_obj));
    }
    
    public static explicit operator int(global::MyUnion value)
    {
        if (value.__um_flag != 3u)
            throw new InvalidCastException($"Unable to cast MyUnion to int");
        return value.__um_unmanaged._1;
    }
    
    public static explicit operator global::System.Text.Json.JsonElement(global::MyUnion value)
    {
        if (value.__um_flag != 4u)
            throw new InvalidCastException($"Unable to cast MyUnion to System.Text.Json.JsonElement");
        return value.__um_managed0;
    }
    
    public static explicit operator global::System.ReadOnlySpan<char>(global::MyUnion value)
    {
        if (value.__um_flag != 5u)
            throw new InvalidCastException($"Unable to cast MyUnion to System.ReadOnlySpan<char>");
        return value.__um_managed1;
    }
    
    public static explicit operator float(global::MyUnion value)
    {
        if (value.__um_flag != 6u)
            throw new InvalidCastException($"Unable to cast MyUnion to float");
        return value.__um_unmanaged._2;
    }
    
    public static unsafe explicit operator void*(global::MyUnion value)
    {
        if (value.__um_flag != 7u)
            throw new InvalidCastException($"Unable to cast MyUnion to void*");
        return (void*)value.__um_unmanaged._ptr;
    }
    
    public static unsafe explicit operator int*(global::MyUnion value)
    {
        if (value.__um_flag != 8u)
            throw new InvalidCastException($"Unable to cast MyUnion to int*");
        return (int*)value.__um_unmanaged._ptr;
    }
    
    #endregion
    
    #region As
    
    public readonly T? As<T>() where T : allows ref struct
    {
        switch (this.__um_flag)
        {
            case 2u:
            {
                if (typeof(T) == typeof(string))
                    return global::System.Runtime.CompilerServices.Unsafe.As<object, T>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<object>(in this.__um_obj));
                if (global::System.Runtime.CompilerServices.Unsafe.As<object, string>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<object>(in this.__um_obj)) is T typed)
                    return typed;
                break;
            }
            case 3u:
            {
                if (typeof(T) == typeof(int))
                    return global::System.Runtime.CompilerServices.Unsafe.As<int, T>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<int>(in this.__um_unmanaged._1));
                if (this.__um_unmanaged._1 is T typed)
                    return typed;
                break;
            }
            case 4u:
            {
                if (typeof(T) == typeof(global::System.Text.Json.JsonElement))
                    return global::System.Runtime.CompilerServices.Unsafe.As<global::System.Text.Json.JsonElement, T>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::System.Text.Json.JsonElement>(in this.__um_managed0));
                if (this.__um_managed0 is T typed)
                    return typed;
                break;
            }
            case 5u:
            {
                if (typeof(T) == typeof(global::System.ReadOnlySpan<char>))
                    return global::System.Runtime.CompilerServices.Unsafe.As<global::System.ReadOnlySpan<char>, T>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::System.ReadOnlySpan<char>>(in this.__um_managed1));
                break;
            }
            case 6u:
            {
                if (typeof(T) == typeof(float))
                    return global::System.Runtime.CompilerServices.Unsafe.As<float, T>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<float>(in this.__um_unmanaged._2));
                if (this.__um_unmanaged._2 is T typed)
                    return typed;
                break;
            }
        }
        return default(T);
    }
    
    public readonly T? AsExactly<T>() where T : allows ref struct
    {
        if (typeof(T) == typeof(string))
        {
            if (this.__um_flag == 2u)
                return global::System.Runtime.CompilerServices.Unsafe.As<object, T>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<object>(in this.__um_obj));
        }
        if (typeof(T) == typeof(int))
        {
            if (this.__um_flag == 3u)
                return global::System.Runtime.CompilerServices.Unsafe.As<int, T>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<int>(in this.__um_unmanaged._1));
        }
        if (typeof(T) == typeof(global::System.Text.Json.JsonElement))
        {
            if (this.__um_flag == 4u)
                return global::System.Runtime.CompilerServices.Unsafe.As<global::System.Text.Json.JsonElement, T>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::System.Text.Json.JsonElement>(in this.__um_managed0));
        }
        if (typeof(T) == typeof(global::System.ReadOnlySpan<char>))
        {
            if (this.__um_flag == 5u)
                return global::System.Runtime.CompilerServices.Unsafe.As<global::System.ReadOnlySpan<char>, T>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::System.ReadOnlySpan<char>>(in this.__um_managed1));
        }
        if (typeof(T) == typeof(float))
        {
            if (this.__um_flag == 6u)
                return global::System.Runtime.CompilerServices.Unsafe.As<float, T>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<float>(in this.__um_unmanaged._2));
        }
        return default(T);
    }
    
    public unsafe readonly T* AsPointer<T>() where T : allows ref struct
    {
        if (typeof(T*) == typeof(int*))
        {
            if (this.__um_flag == 8u)
                return (T*)this.__um_unmanaged._ptr;
        }
        return default(T*);
    }
    
    public unsafe readonly void* AsVoidPointer()
    {
        if (this.__um_flag == 7u)
            return (void*)this.__um_unmanaged._ptr;
        return default(void*);
    }
    
    #endregion
    
    #region Is
    
    public readonly bool Is<T>() where T : allows ref struct
    {
        switch (this.__um_flag)
        {
            case 2u:
            {
                if (typeof(T) == typeof(string))
                    return true;
                return global::System.Runtime.CompilerServices.Unsafe.As<object, string>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef<object>(in this.__um_obj)) is T;
            }
            case 3u:
            {
                if (typeof(T) == typeof(int))
                    return true;
                return this.__um_unmanaged._1 is T;
            }
            case 4u:
            {
                if (typeof(T) == typeof(global::System.Text.Json.JsonElement))
                    return true;
                return this.__um_managed0 is T;
            }
            case 5u:
            {
                if (typeof(T) == typeof(global::System.ReadOnlySpan<char>))
                    return true;
                return false;
            }
            case 6u:
            {
                if (typeof(T) == typeof(float))
                    return true;
                return this.__um_unmanaged._2 is T;
            }
        }
        return false;
    }
    
    public readonly bool IsExactly(global::System.Type type)
    {
        if (typeof(void) == type)
            return this.__um_flag == 1u;
        if (typeof(string) == type)
            return this.__um_flag == 2u;
        if (typeof(int) == type)
            return this.__um_flag == 3u;
        if (typeof(global::System.Text.Json.JsonElement) == type)
            return this.__um_flag == 4u;
        if (typeof(global::System.ReadOnlySpan<char>) == type)
            return this.__um_flag == 5u;
        if (typeof(float) == type)
            return this.__um_flag == 6u;
        if (typeof(void*) == type)
            return this.__um_flag == 7u;
        if (typeof(int*) == type)
            return this.__um_flag == 8u;
        return false;
    }
    
    public readonly bool IsExactly<T>() where T : allows ref struct
    {
        if (typeof(T) == typeof(string))
            return this.__um_flag == 2u;
        if (typeof(T) == typeof(int))
            return this.__um_flag == 3u;
        if (typeof(T) == typeof(global::System.Text.Json.JsonElement))
            return this.__um_flag == 4u;
        if (typeof(T) == typeof(global::System.ReadOnlySpan<char>))
            return this.__um_flag == 5u;
        if (typeof(T) == typeof(float))
            return this.__um_flag == 6u;
        return false;
    }
    
    public readonly bool IsVoid()
    {
        return this.__um_flag == 1u;
    }
    
    public unsafe readonly bool IsPointer<T>() where T : allows ref struct
    {
        if (typeof(T*) == typeof(int*))
            return this.__um_flag == 8u;
        return false;
    }
    
    public unsafe readonly bool IsVoidPointer()
    {
        return this.__um_flag == 7u;
    }
    
    #endregion
    
    [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]
    private struct __ut_Unmanaged
    {
        [global::System.Runtime.InteropServices.FieldOffset(0)]
        public int _1;
        [global::System.Runtime.InteropServices.FieldOffset(0)]
        public float _2;
        [global::System.Runtime.InteropServices.FieldOffset(0)]
        public global::System.IntPtr _ptr;
    }
}
```

</details>

- 生成的类型的排列固定为`LayoutKind.Auto`
- 所有引用类型会重叠；所有非托管值类型及指针会重叠；所有托管值类型不会重叠。

### 选项

- `GenerateDangerousMembers`：默认值为`false`。生成一系列DangerousGetValueRef私有方法，提供根据类型对字段的直接访问。
  - 不设为true也可以访问，因为字段本身就是private可读的。方法只是提供一个更便捷的接口。
- `AlwaysGenerateSeparateMethodsForRefStruct`：即使运行时支持`allows ref struct`，也会为`ref struct` 生成单独的`As`与`Is`方法。

#### 方法

|方法|描述|
|---|---|
|implicit cast|将类型转换为union类型|
|explicit cast|将union类型转换为指定类型|
|`ctor(T)`<br/>`Void`|构造函数，`void`由静态属性`Void`提供|
|`IsNull`|所有类型都存在该属性，用于判定`default`值|
|`As<T>()`|获取指定类型的实例。该方法会通过`as`检测其基类与接口|
|`AsExactly<T>()`|获取指定类型的实例。该方法直接检测确定类型，不检测其基类与接口|
|`Is<T>()`|检查实例是否为指定类型，会检测其基类与接口|
|`IsExactly(Type)`|检查实例是否为指定类型|
|`IsExactly<T>()`|检查实例是否为指定类型，不检测其基类与接口|

- 对于低版本(.NET 9.0以下)不支持`allows ref struct`方法时，会生成单独的`As`与`Is`方法用于检测该类型。
  - 例：`As_ReadOnlySpan_char()`、`Is_ReadOnlySpan_char()`
  - 该功能可由属性控制始终开启
- 存在void类型时，会生成`IsVoid()`方法
- 存在指针类型时，会生成`AsPointer<T>()`，`AsVoidPointer()`, `IsPointer<T>()`、`IsVoidPointer()`方法
  - 对于ref struct指针，同样会生成单独的`AsPointer_ReadOnlySpan_char()`方法
- 由于C#不支持接口的类型转换，因此不生成接口类型的隐式转换与显式转换方法。
