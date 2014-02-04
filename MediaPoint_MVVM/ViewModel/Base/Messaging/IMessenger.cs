using System;

namespace MediaPoint.MVVM.Messaging
{
    /// <summary>
    /// The Messenger is a class allowing objects to exchange messages.
    /// </summary>
    public interface IMessenger
    {
        /// <summary>
        /// Registers a recipient for a type of message TMessage. The action
        /// parameter will be executed when a corresponding message is sent.
        /// <para>Registering a recipient does not create a hard reference to it,
        /// so if this recipient is deleted, no memory leak is caused.</para>
        /// </summary>
        /// <typeparam name="TMessage">The type of message that the recipient registers
        /// for.</typeparam>
        /// <param name="recipient">The recipient that will receive the messages.</param>
        /// <param name="action">The action that will be executed when a message
        /// of type TMessage is sent.</param>
        void Register<TMessage>(object recipient, Action<TMessage> action);

        /// <summary>
        /// Sends a message to registered recipients. The message will
        /// reach all recipients that registered for this message type
        /// using one of the Register methods.
        /// </summary>
        /// <typeparam name="TMessage">The type of message that will be sent.</typeparam>
        /// <param name="message">The message to send to registered recipients.</param>
        void Send<TMessage>(TMessage message);

        /// <summary>
        /// Sends a message to registered recipients. The message will
        /// reach only recipients that registered for this message type
        /// using one of the Register methods, and that are
        /// of the targetType.
        /// </summary>
        /// <typeparam name="TMessage">The type of message that will be sent.</typeparam>
        /// <typeparam name="TTarget">The type of recipients that will receive
        /// the message. The message won't be sent to recipients of another type.</typeparam>
        /// <param name="message">The message to send to registered recipients.</param>
        void Send<TMessage, TTarget>(TMessage message);

        /// <summary>
        /// Unregisters a messager recipient completely. After this method
        /// is executed, the recipient will not receive any messages anymore.
        /// </summary>
        /// <param name="recipient">The recipient that must be unregistered.</param>
        void Unregister(object recipient);

    }
}