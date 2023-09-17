using System;

namespace WPF_Desktop.Shared
{
    public interface IMessenger
    {
        void Send<TMessage>(TMessage message);

        void Subscribe<TMessage>(object subscriber, Action<object> message);

        void Unsubscribe<TMessage>(object subscriber);
    }
}