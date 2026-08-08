using System;
using System.Collections.Generic;
using System.Text;

namespace Trarizon.Library.Functional.Unions;

internal interface ITypeUnion
{
    bool IsNull { get; }
    T? As<T>();
}
