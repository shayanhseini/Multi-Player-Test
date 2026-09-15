using System;

[Flags]
public enum PlayerRole
{
    None = 0,
    Presenter = 1 << 0,
    Guest = 1 << 1
}