using Trarizon.Library.Functional;

namespace Trarizon.Run;

internal class Functional_Chains
{
    public void Optionals()
    {
        _ = Optional.Some("str")
            .Select(i => i.ToString())  // val -> val
            .Select(o => o.AsSpan())    // val -> ref
            .Select(o => o[..1])        // ref -> ref
            .Select(x => x.ToString()); // ref -> val

        _ = Optional.Some("str")
            .Bind(x => Optional.Some(x.ToString()))  // val -> val
            .Bind(x => Optional.Some(x.AsSpan()))    // val -> ref
            .Bind(x => Optional.Some(x[..1]))        // ref -> ref
            .Bind(x => Optional.Some(x.ToString())); // ref -> val

        _ = Optional.Some("str".AsSpan())
            .SelectMany(x => Optional.Some(x.ToString()), (x, y) => $"{x}{y}")             // ref - val - val
            .SelectMany(x => Optional.Some(x.AsSpan()), (x, y) => $"{x}{y}")               // val - ref - val
            .SelectMany(x => Optional.Some(x.ToString()), (x, y) => $"{x}{y}".AsSpan())    // val - val - ref
            .SelectMany(x => Optional.Some(x.ToString()), (x, y) => $"{x}{y}".AsSpan())    // ref - val - ref
            .SelectMany(x => Optional.Some(x[..1]), (x, y) => $"{x}{y}".ToString())        // ref - ref - val
            .SelectMany(x => Optional.Some(x.AsSpan()), (x, y) => $"{x}{y}".AsSpan());     // val - ref - ref

        _ = Optional.Some("str".AsSpan()).Zip(Optional.Some("ing"), (x, y) => $"{x}{y}");    // ref + val = val
        _ = Optional.Some("str").Zip(Optional.Some("ing".AsSpan()), (x, y) => $"{x}{y}");    // val + ref = val
        _ = Optional.Some("str").Zip(Optional.Some("ing"), (x, y) => $"{x}{y}".AsSpan());    // val + val = ref
        _ = Optional.Some("str".AsSpan()).Zip(Optional.Some("ing"), (x, y) => $"{x}{y}".AsSpan()); // ref + val = ref
        _ = Optional.Some("str").Zip(Optional.Some("ing".AsSpan()), (x, y) => $"{x}{y}".AsSpan()); // val + ref = ref
        _ = Optional.Some("str".AsSpan()).Zip(Optional.Some("ing".AsSpan()), (x, y) => $"{x}{y}"); // ref + ref = val
        _ = Optional.Some("str".AsSpan()).Zip(Optional.Some("ing".AsSpan()), (x, y) => $"{x}{y}"); // ref + ref = ref

    }

    public void Results()
    {
        _ = Result.Success("str").Build<int>()
            .Select(i => i.ToString())  // val -> val
            .Select(o => o.AsSpan())    // val -> ref
            .Select(o => o[..1])        // ref -> ref
            .Select(x => x.ToString()); // ref -> val

        _ = Result.Success("str".AsSpan());
    }
}
