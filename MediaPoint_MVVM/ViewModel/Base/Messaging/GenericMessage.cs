namespace MediaPoint.MVVM.Messaging
{
    /// <summary>
    /// Passes a generic value (Content) to a recipient.
    /// </summary>
    /// <typeparam name="T">The type of the Content property.</typeparam>
    public class GenericMessage<T> : MessageBase
    {
        /// <summary>
        /// Initializes a new instance of the GenericMessage class.
        /// </summary>
        /// <param name="content">The message content.</param>
        public GenericMessage(T content)
        {
            Content = content;
        }

        /// <summary>
        /// Initializes a new instance of the GenericMessage class.
        /// </summary>
        /// <param name="sender">The message's sender.</param>
        /// <param name="content">The message content.</param>
        public GenericMessage(object sender, T content)
            : base(sender)
        {
            Content = content;
        }

        /// <summary>
        /// Initializes a new instance of the GenericMessage class.
        /// </summary>
        /// <param name="sender">The message's sender.</param>
        /// <param name="target">The message's intended target. This parameter can be used
        /// to give an indication as to whom the message was intended for. Of course
        /// this is only an indication, amd may be null.</param>
        /// <param name="content">The message content.</param>
        public GenericMessage(object sender, object target, T content)
            : base(sender, target)
        {
            Content = content;
        }

        /// <summary>
        /// Gets or sets the message's content.
        /// </summary>
        public T Content
        {
            get;
            protected set;
        }
    }

    /// <summary>
    /// Passes three generic values (Content1, Content2, Content3) to a recipient.
    /// </summary>
    /// <typeparam name="T">The type of the Content1 property.</typeparam>
    /// <typeparam name="T2">The type of the Content2 property.</typeparam>
    /// <typeparam name="T3">The type of the Content3 property.</typeparam>
    public class GenericMessage<T, T2, T3> : MessageBase
    {
        /// <summary>
        /// Initializes a new instance of the GenericMessage class.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <param name="content2">The message content.</param>
        public GenericMessage(T content, T2 content2, T3 content3)
        {
            Content1 = content;
            Content2 = content2;
            Content3 = content3;
        }

        /// <summary>
        /// Initializes a new instance of the GenericMessage class.
        /// </summary>
        /// <param name="sender">The message's sender.</param>
        /// <param name="content">The message content1.</param>
        /// <param name="content2">The message content2.</param>
        /// <param name="content3">The message content3.</param>
        public GenericMessage(object sender, T content, T2 content2, T3 content3)
            : base(sender)
        {
            Content1 = content;
            Content2 = content2;
            Content3 = content3;
        }

        /// <summary>
        /// Initializes a new instance of the GenericMessage class.
        /// </summary>
        /// <param name="sender">The message's sender.</param>
        /// <param name="target">The message's intended target. This parameter can be used
        /// to give an indication as to whom the message was intended for. Of course
        /// this is only an indication, amd may be null.</param>
        /// <param name="content">The message content1.</param>
        /// <param name="content2">The message content2.</param>
        /// <param name="content3">The message content3.</param>
        public GenericMessage(object sender, object target, T content, T2 content2, T3 content3)
            : base(sender, target)
        {
            Content1 = content;
            Content2 = content2;
            Content3 = content3;
        }

        /// <summary>
        /// Gets or sets the message's content.
        /// </summary>
        public T Content1
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the message's content.
        /// </summary>
        public T2 Content2
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the message's content.
        /// </summary>
        public T3 Content3
        {
            get;
            protected set;
        }

    }

    /// <summary>
    /// Passes two generic values (Content1, Content2) to a recipient.
    /// </summary>
    /// <typeparam name="T">The type of the Content1 property.</typeparam>
    /// /// <typeparam name="T2">The type of the Content2 property.</typeparam>
    public class GenericMessage<T, T2> : MessageBase
    {
        /// <summary>
        /// Initializes a new instance of the GenericMessage class.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <param name="content2">The message content.</param>
        public GenericMessage(T content, T2 content2)
        {
            Content1 = content;
            Content2 = content2;
        }

        /// <summary>
        /// Initializes a new instance of the GenericMessage class.
        /// </summary>
        /// <param name="sender">The message's sender.</param>
        /// <param name="content">The message content1.</param>
        /// <param name="content2">The message content2.</param>
        public GenericMessage(object sender, T content, T2 content2)
            : base(sender)
        {
            Content1 = content;
            Content2 = content2;
        }

        /// <summary>
        /// Initializes a new instance of the GenericMessage class.
        /// </summary>
        /// <param name="sender">The message's sender.</param>
        /// <param name="target">The message's intended target. This parameter can be used
        /// to give an indication as to whom the message was intended for. Of course
        /// this is only an indication, amd may be null.</param>
        /// <param name="content">The message content1.</param>
        /// <param name="content2">The message content2.</param>
        public GenericMessage(object sender, object target, T content, T2 content2)
            : base(sender, target)
        {
            Content1 = content;
            Content2 = content2;
        }

        /// <summary>
        /// Gets or sets the message's content.
        /// </summary>
        public T Content1
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the message's content.
        /// </summary>
        public T2 Content2
        {
            get;
            protected set;
        }

    }
}