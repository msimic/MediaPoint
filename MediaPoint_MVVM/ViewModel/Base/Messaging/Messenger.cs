using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MediaPoint.MVVM.Helpers;
using System.Diagnostics;

namespace MediaPoint.MVVM.Messaging
{
    /// <summary>
    /// The Messenger is a class allowing objects to exchange messages.
    /// </summary>
    public class Messenger : IMessenger
    {
        private static readonly object _creationLock = new object();
        private static Messenger _defaultInstance;
        private readonly object _registerLock = new object();
        private Dictionary<Type, List<WeakAction>> _recipientsAction;

        /// <summary>
        /// Gets the Messenger's default instance, allowing
        /// to register and send messages in a static manner.
        /// </summary>
        public static Messenger Default
        {
            get
            {
                if (_defaultInstance == null)
                {
                    lock (_creationLock)
                    {
                        if (_defaultInstance == null)
                        {
#if DEBUG
                            _defaultInstance = new LoggingMessenger();
#else
                            _defaultInstance = new Messenger();
#endif
                        }
                    }
                }

                return _defaultInstance;
            }
        }

        #region IMessenger Members

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
        [DebuggerStepThrough]
        public virtual void Register<TMessage>(
            object recipient,
            Action<TMessage> action)
        {
            lock (_registerLock)
            {
                Type messageType = typeof(TMessage);

                Dictionary<Type, List<WeakAction>> recipients;

                if (_recipientsAction == null)
                {
                    _recipientsAction = new Dictionary<Type, List<WeakAction>>();
                }

                recipients = _recipientsAction;

                lock (recipients)
                {
                    List<WeakAction> list;

                    if (!recipients.ContainsKey(messageType))
                    {
                        list = new List<WeakAction>();
                        recipients.Add(messageType, list);
                    }
                    else
                    {
                        list = recipients[messageType];
                    }

                    var weakAction = new WeakAction<TMessage>(recipient, action);
                    list.Add(weakAction);
                }
            }

            Cleanup();
        }

        /// <summary>
        /// Sends a message to registered recipients. The message will
        /// reach all recipients that registered for this message type
        /// using one of the Register methods.
        /// </summary>
        /// <typeparam name="TMessage">The type of message that will be sent.</typeparam>
        /// <param name="message">The message to send to registered recipients.</param>
        [DebuggerStepThrough]
        public virtual void Send<TMessage>(TMessage message)
        {
            SendToTargetOrType(message, null);
        }

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
        [DebuggerStepThrough]         
        public virtual void Send<TMessage, TTarget>(TMessage message)
        {
            SendToTargetOrType(message, typeof(TTarget));
        }

        /// <summary>
        /// Unregisters a messager recipient completely. After this method
        /// is executed, the recipient will not receive any messages anymore.
        /// </summary>
        /// <param name="recipient">The recipient that must be unregistered.</param>
        [DebuggerStepThrough]
        public virtual void Unregister(object recipient)
        {
            UnregisterFromLists(recipient, _recipientsAction);
        }

        #endregion

        /// <summary>
        /// Provides a way to override the Messenger.Default instance with
        /// a custom instance, for example for unit testing purposes.
        /// </summary>
        /// <param name="newMessenger">The instance that will be used as Messenger.Default.</param>
        public static void OverrideDefault(Messenger newMessenger)
        {
            _defaultInstance = newMessenger;
        }

        /// <summary>
        /// Sets the Messenger's default (static) instance to null.
        /// </summary>
        public static void Reset()
        {
            _defaultInstance = null;
        }

        private static void CleanupList(IDictionary<Type, List<WeakAction>> lists)
        {
            if (lists == null)
            {
                return;
            }

            lock (lists)
            {
                var listsToRemove = new List<Type>();
                foreach (var list in lists)
                {
                    var recipientsToRemove = new List<WeakAction>();
                    foreach (WeakAction item in list.Value)
                    {
                        if (item == null
                            || !item.IsAlive)
                        {
                            recipientsToRemove.Add(item);
                        }
                    }

                    foreach (WeakAction recipient in recipientsToRemove)
                    {
                        list.Value.Remove(recipient);
                    }

                    if (list.Value.Count == 0)
                    {
                        listsToRemove.Add(list.Key);
                    }
                }

                foreach (Type key in listsToRemove)
                {
                    lists.Remove(key);
                }
            }
        }

        protected virtual void SendToList<TMessage>(
            TMessage message,
            IEnumerable<WeakAction> list,
            Type messageTargetType)
        {
            if (list != null)
            {
                // Clone to protect from people registering in a "receive message" method
                List<WeakAction> listClone = list.Take(list.Count()).ToList();

                foreach (WeakAction item in listClone)
                {
                    var executeAction = item as IObjectAction;

                    if (executeAction != null
                        && item.IsAlive
                        && item.Target != null
                        && (messageTargetType == null
                            || item.Target.GetType() == messageTargetType))
                    {
                        executeAction.Execute(message);
                    }
                }
            }
        }

        private static void UnregisterFromLists(object recipient, Dictionary<Type, List<WeakAction>> lists)
        {
            if (recipient == null
                || lists == null
                || lists.Count == 0)
            {
                return;
            }

            lock (lists)
            {
                foreach (Type messageType in lists.Keys)
                {
                    foreach (WeakAction item in lists[messageType])
                    {
                        WeakAction weakAction = item;

                        if (weakAction != null
                            && recipient == weakAction.Target)
                        {
                            weakAction.MarkForDeletion();
                        }
                    }
                }
            }
        }

        private static void UnregisterFromLists<TMessage>(
            object recipient,
            object token,
            Action<TMessage> action,
            Dictionary<Type, List<WeakAction>> lists)
        {
            Type messageType = typeof(TMessage);

            if (recipient == null
                || lists == null
                || lists.Count == 0
                || !lists.ContainsKey(messageType))
            {
                return;
            }

            lock (lists)
            {
                foreach (WeakAction item in lists[messageType])
                {
                    var weakActionCasted = item as WeakAction<TMessage>;

                    if (weakActionCasted != null
                        && recipient == weakActionCasted.Target
                        && (action == null
                            || action == weakActionCasted.Action))
                    {
                        item.MarkForDeletion();
                    }
                }
            }
        }

        private void Cleanup()
        {
            CleanupList(_recipientsAction);
        }

        [DebuggerStepThrough]                 
        public void SendToTargetOrType<TMessage>(TMessage message, Type messageTargetType)
        {
            Type messageType = message.GetType();

            if (_recipientsAction != null)
            {
                if (_recipientsAction.ContainsKey(messageType))
                {
                    List<WeakAction> list = null;

                    lock (_recipientsAction)
                    {
                        list = _recipientsAction[messageType].Take(_recipientsAction[messageType].Count()).ToList();
                    }

                    SendToList(message, list, messageTargetType);
                }
            }

            Cleanup();
        }

    }
}