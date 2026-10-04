using Horse_BackEnd.Data;

try
{
    switch (args)
    {
        case ["backup-sqlite", var database, var uploads, var keys, var destination, "--offline"]:
            SqliteRecoveryBundle.Create(database, uploads, keys, destination); break;
        case ["verify-sqlite", var bundle]: SqliteRecoveryBundle.Verify(bundle); break;
        case ["restore-sqlite", var bundle, var destination]: SqliteRecoveryBundle.Restore(bundle, destination); break;
        default:
            Console.Error.WriteLine("Stop all API/worker processes before backup.\nbackup-sqlite <database> <uploads> <keys> <new-bundle> --offline\nverify-sqlite <bundle>\nrestore-sqlite <bundle> <new-destination>");
            return 2;
    }
    Console.WriteLine("Recovery operation completed."); return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"Recovery operation failed ({error.GetType().Name}): {error.Message}"); return 1;
}
