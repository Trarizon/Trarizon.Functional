using Trarizon.Library.Functional.Unions;

Console.WriteLine("Hello, World!");

[TypeUnion(typeof(int),typeof(string))]
partial struct U
{

}