using System;
using System.Threading.Tasks;

namespace NetPad.Utilities;

public static class Try
{
    public static bool Run(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static async Task<bool> RunAsync(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static T Run<T>(Func<T> action, T valueOnError = default(T))
    {
        try
        {
            return action();
        }
        catch (Exception)
        {
            return valueOnError;
        }
    }

    public static async Task<T> RunAsync<T>(Func<Task<T>> action, T valueOnError = default(T))
    {
        try
        {
            return await action();
        }
        catch (Exception)
        {
            return valueOnError;
        }
    }

    public static T Run<T>(Func<T> action, Func<T> valueOnErrorFunc)
    {
        try
        {
            return action();
        }
        catch (Exception)
        {
            return valueOnErrorFunc();
        }
    }

    public static async Task<T> RunAsync<T>(Func<Task<T>> action, Func<T> valueOnErrorFunc)
    {
        try
        {
            return await action();
        }
        catch (Exception)
        {
            return valueOnErrorFunc();
        }
    }
}
