using System;

namespace ShareShot.Interfaces
{
    public interface IKeyboardHook : IDisposable
    {
        event EventHandler? KeyPressed;
    }
} 