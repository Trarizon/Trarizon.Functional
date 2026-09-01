namespace Trarizon.Library.Functional.Generators.Internals;

interface IParseInfo
{
    ExceptionInfo? Exception { get; set; }
}

readonly record struct ExceptionInfo(
    string Message,
    string StackTrace
);

static class ParseInfoExt
{
    extension<T>(T self) where T : IParseInfo, new()
    {
        public static T FromException(Exception exception)
        {
            return new T { Exception = new ExceptionInfo(exception.Message, exception.StackTrace) };
        }

        public bool IsSuccess => self.Exception is null;
    }
}