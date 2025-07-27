# Add EntityFramework Core Providers

EntityFramework Core supports [many different database providers](https://learn.microsoft.com/en-us/ef/core/providers).
NetPad has support for a few already.

To add support for a new EntityFramework Core provider to NetPad, mimic the changes introduced
in [this PR](https://github.com/tareqimbasher/NetPad/pull/247).

### Test it

After the code changes are in place, test it:

1. Run NetPad and create a new database connection using the new provider that you added.
2. Create a new script and use the newly created connection.
3. Write a LINQ query selecting data from the database and run the script.

> If you're having issues, and need help, please reach out via Discord.