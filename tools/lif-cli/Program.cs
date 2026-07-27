using Lif.Cli;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: lif-cli <command> [args]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  seed-validate <package-dir>   Validate a curriculum seed package (doc 18 §4).");
    return 2;
}

switch (args[0])
{
    case "seed-validate":
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: lif-cli seed-validate <package-dir>");
            return 2;
        }

        return SeedValidator.Run(args[1]);
    }

    default:
        Console.Error.WriteLine($"Unknown command '{args[0]}'.");
        return 2;
}
