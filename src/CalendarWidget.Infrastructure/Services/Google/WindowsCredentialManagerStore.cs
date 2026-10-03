using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace CalendarWidget.Infrastructure.Services.Google;

/// <summary>Stores encrypted secrets in the current Windows user's Credential Manager vault.</summary>
public sealed class WindowsCredentialManagerStore : IGoogleCredentialStore
{
    private const uint GenericCredential = 1;
    private const uint PersistLocalMachine = 2;

    public Task<string?> ReadAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureWindows();
        string target = GetTargetName(key);
        if (!CredRead(target, GenericCredential, 0, out IntPtr credentialPointer))
        {
            int error = Marshal.GetLastWin32Error();
            if (error == 1168)
                return Task.FromResult<string?>(null);
            throw new Win32Exception(error, "Could not read the Google credential from Windows Credential Manager.");
        }

        try
        {
            NativeCredential credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
            return Task.FromResult<string?>(Marshal.PtrToStringUni(
                credential.CredentialBlob, checked((int)credential.CredentialBlobSize / sizeof(char))));
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    public Task WriteAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureWindows();
        IntPtr blob = Marshal.StringToCoTaskMemUni(value);
        try
        {
            NativeCredential credential = new()
            {
                Type = GenericCredential,
                TargetName = GetTargetName(key),
                CredentialBlobSize = checked((uint)(Encoding.Unicode.GetByteCount(value))),
                CredentialBlob = blob,
                Persist = PersistLocalMachine,
                UserName = "Desktop Calendar",
            };
            if (!CredWrite(ref credential, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not save the Google credential in Windows Credential Manager.");
            return Task.CompletedTask;
        }
        finally
        {
            Marshal.ZeroFreeCoTaskMemUnicode(blob);
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureWindows();
        if (!CredDelete(GetTargetName(key), GenericCredential, 0) && Marshal.GetLastWin32Error() != 1168)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not remove the Google credential from Windows Credential Manager.");
        return Task.CompletedTask;
    }

    private static string GetTargetName(string key)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return $"DesktopCalendar.Google.{Convert.ToHexString(digest)}";
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Google credential storage requires Windows Credential Manager.");
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite(ref NativeCredential credential, uint flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = false)]
    private static extern void CredFree(IntPtr buffer);
}
