using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CalendarWidget.Infrastructure.Services;

/// <summary>
/// File-based JSON persistence implementation of <see cref="ISettingsRepository"/>.
/// </summary>
public sealed class FileSettingsRepository : ISettingsRepository, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly Action<ILogger, Exception?> LogValidationFailed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(1, "ValidationFailed"), "Settings file validation failed. Falling back to default settings.");

    private static readonly Action<ILogger, Exception?> LogMalformedJson =
        LoggerMessage.Define(LogLevel.Warning, new EventId(2, "MalformedJson"), "Corrupt or malformed settings JSON file. Falling back to default settings.");

    private static readonly Action<ILogger, string, Exception?> LogReadError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3, "ReadError"), "Unexpected error reading settings file from {FilePath}.");

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _filePath;
    private readonly ILogger<FileSettingsRepository> _logger;
    private readonly SemaphoreSlim _fileLock;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSettingsRepository"/> class.
    /// </summary>
    /// <param name="filePath">Target path to the settings.json file.</param>
    /// <param name="logger">Logger instance.</param>
    public FileSettingsRepository(string filePath, ILogger<FileSettingsRepository> logger)
    {
        _filePath = filePath;
        _logger = logger;

        string normalizedPath = Path.GetFullPath(filePath);
        _fileLock = FileLocks.GetOrAdd(normalizedPath, _ => new SemaphoreSlim(1, 1));
    }

    /// <inheritdoc />
    public async Task<UserSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
            {
                return new UserSettings();
            }

            string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new UserSettings();
            }

            UserSettings? settings = JsonSerializer.Deserialize<UserSettings>(json, SerializerOptions);
            if (settings is null)
            {
                return new UserSettings();
            }

            settings.Validate();
            return settings;
        }
        catch (DomainValidationException ex)
        {
            LogValidationFailed(_logger, ex);
            return new UserSettings();
        }
        catch (JsonException ex)
        {
            LogMalformedJson(_logger, ex);
            try
            {
                if (File.Exists(_filePath))
                {
                    string backupPath = $"{_filePath}.corrupted.{DateTime.UtcNow:yyyyMMddHHmmss}";
                    File.Move(_filePath, backupPath, overwrite: true);
                }
            }
            catch
            {
                // Silently ignore backup errors to ensure fallback default settings are returned.
            }

            return new UserSettings();
        }
        catch (Exception ex)
        {
            LogReadError(_logger, _filePath, ex);
            return new UserSettings();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(settings, SerializerOptions);
            string directory = Path.GetDirectoryName(_filePath) ?? Directory.GetCurrentDirectory();
            string tempFile = Path.Combine(directory, $"{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.tmp");
            string backupFile = Path.Combine(directory, $"{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.bak");

            await File.WriteAllTextAsync(tempFile, json, cancellationToken);
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Move(_filePath, backupFile);
                }

                File.Move(tempFile, _filePath);

                if (File.Exists(backupFile))
                {
                    File.Delete(backupFile);
                }
            }
            catch
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                if (File.Exists(backupFile) && !File.Exists(_filePath))
                {
                    File.Move(backupFile, _filePath);
                }

                throw;
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // This repository coordinates file-path-level synchronization across all instances.
        // Disposing the shared semaphore here would invalidate in-flight writers created by
        // other repository instances that point to the same settings file.
    }
}
