using Application.Auth;

if (args.Length == 0)
{
    Console.Error.WriteLine("Uso: PasswordHashGen <password>");
    return 1;
}

Console.WriteLine(PasswordHasher.Hash(args[0]));
return 0;
