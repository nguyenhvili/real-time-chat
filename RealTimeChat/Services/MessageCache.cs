using Microsoft.Extensions.Options;
using RealTimeChat.Database.Models;
using RealTimeChat.Server.Configuration;

namespace RealTimeChat.Server.Services;

public class MessageCache(IOptions<ChatSettings> settings)
{
    private readonly int _maxMessages = settings.Value.MaxCachedMessages;
    private readonly LinkedList<MessageEntity> _messages = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public bool IsInitialized { get; private set; }

    public IReadOnlyList<MessageEntity> GetMessages()
    {
        _lock.EnterReadLock();
        try
        {
            return [.. _messages];
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Initialize(IEnumerable<MessageEntity> messages)
    {
        _lock.EnterWriteLock();
        try
        {
            _messages.Clear();
            foreach (var m in messages)
                _messages.AddLast(m);
            IsInitialized = true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void AddMessage(MessageEntity message)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_messages.Count == _maxMessages)
                _messages.RemoveFirst();
            _messages.AddLast(message);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
