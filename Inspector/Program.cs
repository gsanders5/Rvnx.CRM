using System;
using System.Linq;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var asm = Assembly.Load("FileTypeChecker.Web");
            Console.WriteLine("Assembly Loaded: " + asm.FullName);
            foreach (var type in asm.GetTypes())
            {
                if (type.IsPublic)
                {
                    Console.WriteLine($"Type: {type.FullName}");
                    foreach (var method in type.GetMethods())
                    {
                        if (method.Name == "AddFileTypesValidation")
                        {
                            Console.WriteLine($"  FOUND Method: {method.Name} in {type.FullName}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }
    }
}
