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
    public class LoggingMessenger : Messenger
    {
        [DebuggerStepThrough]
        public override void Register<TMessage>(object recipient, Action<TMessage> action)
        {
            Trace.TraceInformation(String.Format("{0} registered to messages of type {1}", recipient.GetType().Name, typeof(TMessage).Name));
            base.Register<TMessage>(recipient, action);
        }

        [DebuggerStepThrough]
        public override void Send<TMessage>(TMessage message)
        {
            StackTrace st = new StackTrace(true);
            var sf = st.GetFrame(1);
            Type t = sf.GetMethod().DeclaringType;
            Trace.TraceInformation(String.Format("{0} sent a message of type {1}. (file: {2}, line: {3})", t.Name.ToString(), typeof(TMessage).Name, sf.GetFileName(), sf.GetFileLineNumber()));
            base.Send<TMessage>(message);
        }

        [DebuggerStepThrough]
        public override void Send<TMessage, TTarget>(TMessage message)
        {
            StackTrace st = new StackTrace(true);
            var sf = st.GetFrame(1);
            Type t = sf.GetMethod().DeclaringType;
            Trace.TraceInformation(String.Format("{0} sent a message of type {1}. (file: {2}, line: {3})", t.Name.ToString(), typeof(TMessage).Name, sf.GetFileName(), sf.GetFileLineNumber()));
            base.Send<TMessage, TTarget>(message);
        }

        [DebuggerStepThrough]
        public override void Unregister(object recipient)
        {
            Trace.TraceInformation(String.Format("{0} ({1}) unregisters from messages", recipient.GetType().Name, recipient.GetHashCode()));
            base.Unregister(recipient);
        }

        [DebuggerStepThrough]
        protected override void SendToList<TMessage>(
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

                    if ((executeAction != null) &&
                        (item.IsAlive) &&
                        (item.Target != null) &&
                        ((messageTargetType == null) ||
                         (item.Target.GetType() == messageTargetType)))
                    {
						Object target = ((System.Delegate)executeAction.GetObject()).Target;
						if (target != null)
						{
							Trace.TraceInformation(String.Format("{0} ({1}) shall receive a message of type {2}", target.GetType().Name, target.GetHashCode(), typeof(TMessage).Name));
							executeAction.Execute(message);
						}
                    }
                }
            }
        }        
    }
}