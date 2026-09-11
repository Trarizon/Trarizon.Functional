# Design-TypeUnion

用户通过标记`[TypeUnion]`来定义一个类型联合。

```csharp
[TypeUnion(typeof(int), typeof(string))]
partial struct MyTypeUnion;

[TypeUnion<int, string>]
partial struct MyTypeUnion2;
```

目前type union生成器的设计目的是尽可能重叠能重叠的字段以减少堆分配，因此只支持`struct`。

## 支持特性

- 提供完整的类型支持（包括`ref struct`，`void`，指针(由于函数指针不支持`typeof()`，这个没办法)）
- 提供接口支持用户自定扩展方法（`DangerousGetValueRef<T>()`）
- 支持共享接口，Union自动实现所有variant都有实现的接口
- 提供元信息支持，能够在运行时不经过反射获取类型的变体信息

## 生成的代码

若含有`ref struct`变体，则生成类型也为`ref struct`。

### 成员命名

所有私有成员以双下划线开头，命名逻辑为：
- 成员：`__um_xxx`, **u**nion **m**ember
- 类型：`__ut_Xxx`, **u**nion **t**ype

union类型标记为`StructLayout(LayoutKind.Auto)`以支持CLR重排字段

代码内部使用`byte __um_flag`字段标记类型，因C#无法避免`default`值，字段为0时表示null

所有引用类型使用单个`object __um_obj`字段存储；所有非托管类型使用`__ut_Unmanaged __um_unmanaged`字段存储；托管值类型平铺`RealType __um_managedX`字段。

#### `__un_Unmanaged`

标记为`[StructLayout(LayoutKind.Explicit)]`，重叠存储所有非托管类型的字段。

字段命名为`_x`，x为数字索引，仅用于避免重复，无意义。

若存在指针变体，则使用`nint _ptr`字段存储所有指针。

### 公开成员

成员|注释
:-:|:--
`IsNull`|判定是否为`null`（_flag == 0）
`ctor()`|为所有字段提供构造函数
`Void`|存在`void`变体时，该属性用于构造`void`实例
`implicit cast()`|提供所有变体到union的隐式转换运算符（若允许）
`explicit cast()`|提供union到所有变体的显式转换运算符（若允许）
`As<T>()`|提供C#`as`语义的转换
`AsExactly<T>()`|判断union是否为指定变体(判断_flag值)，若是则返回该变体类型值
`Is<T>()`|提供C#`is`语义的判断
`IsExactly<T>()`|判断union是否为指定变体(判断_flag值)，若是则返回true
`Is<T>(out T)`|提供C#`is`语义的判断，若为指定变体则返回true并赋值给out参数
`IsExactly<T>(out T)`|判断union是否为指定变体(判断_flag值)，若是则返回true并赋值给out参数

对于泛型成员如`Is<T>()`，由于有些类型无法作为泛型参数传入，因此使用其他方式提供访问，以`Is<T>()`为例：

- 对于`void`，提供`IsVoid()`方法，用于判断是否为`void`变体，`As`不存在该方法
- 对于`ref struct`，例`ReadOnlySpan<char>`，提供`Is_ReadOnlySpan_char()`方法。.NET 9及以上版本支持`ref struct`作为泛型参数，因此不生成该方法。但依然可以通过`TypeUnionAttribute`的属性手动指定生成
- 对于`void*`，提供`IsVoidPointer()`方法，用于判断是否为`void*`变体
  - 对于多级`void`指针，例`void**`，提供`IsVoidPointer2()`方法，用于判断是否为`void**`变体
- 对于指针`T*`，提供`IsPointer<T>()`方法与`IsPointer_ReadOnlySpan_char()`等方法
  - 对于多级指针，提供`IsPointer2<T>()`方法

### 接口

生成的type union类型默认实现`ITypeUnion<TSelf>`与`IEquatable<TSelf>`接口。

`ITypeUnion<TSelf>`设计为生成器专用接口，不允许用户手动实现。

可选`ShareInterfaces`参数用于指定是否自动实现所有variant都有实现的接口。

### 元信息

对于.NET 7及以上版本，通过`static abstract interface member`提供元信息。

对于所有版本，提供静态类型`TypeUnionMetadata<T>`来获取类型的变体信息，对于低版本.NET，该类型通过反射获取并缓存用户相关数据实现。

### `DangerousGetValueRef<T>()`

设置`GenerateDangerousMembers = true`时生成，该方法定义为私有方法，用于在已知变体类型时提供直接访问对变体字段的方式。该方法归类为unsafe方法，不提供安全检查。

该方法同样为无法作为泛型参数的类型提供了变体。

### 共享接口

设置`ShareInterfaces = UnionShareInterfaceOption.Explicit`时生成。使union实现所有variant都实现的接口，转发到对应变体的接口实现成员。

- 目前所有接口成员实现为explicit
- 目前不支持含有抽象静态成员的接口（这个不应该，应该支持。目前是因为TSelf会导致一些奇怪的问题）
